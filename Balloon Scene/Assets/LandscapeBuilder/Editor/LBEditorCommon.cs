// Landscape Builder. Copyright (c) 2016-2019 SCSM Pty Ltd. All rights reserved.
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace LandscapeBuilder
{
    /// <summary>
    /// Common Editor class for Landscape Builder
    /// </summary>
    public class LBEditorCommon
    {
        #region Public static Variables and properties

        public static string LBVersion { get { return "2.2.3"; } }
        public static string LBBetaVersion { get { return ""; } }

        public static LBLandscape currentLandscape;
        #endregion

        #region Common Editor methods - Terrains

        /// <summary>
        /// Per Pixel Normals is a feature in U2018.3 with LWRP 4.0.1 or newer.
        /// It also requires Draw Instanced to be enabled on the terrain (new in U2018.3).
        /// If users want different per-pixel normals for different landscapes, they should set
        /// terrain type to custom, and use a custom material.
        /// </summary>
        /// <param name="lbLandscape"></param>
        /// <returns></returns>
        public static bool GetTerrainPerPixelNormals(LBLandscape lbLandscape)
        {
            bool isPerPixelNormalsEnabled = false;

            #if UNITY_2018_3_OR_NEWER
            if (lbLandscape != null)
            {
                LBLandscape.TerrainMaterialType _terrainMaterialType = lbLandscape.GetTerrainMaterialType();

                if (_terrainMaterialType == LBLandscape.TerrainMaterialType.URP)
                {
                    #if UNITY_2019_3_OR_NEWER
                    // Universal Render Pipeline requires U2019.3+
                    if (lbLandscape.GetLandscapeTerrainDrawInstanced())
                    {
                        // Check the custom material on the first terrain
                        Material _tCustomMat = lbLandscape.GetLandscapeTerrainCustomMaterial();

                        if (_tCustomMat != null && _tCustomMat.shader.name.Contains("Universal Render Pipeline/Terrain/Lit"))
                        {
                            if (_tCustomMat.HasProperty("_EnableInstancedPerPixelNormal"))
                            {
                                isPerPixelNormalsEnabled = (_tCustomMat.GetFloat("_EnableInstancedPerPixelNormal") > 0f);
                            }
                            else
                            {
                                Debug.LogWarning("URP material property _EnableInstancedPerPixelNormal is not available. Do you have URP 7.1.2 or newer in your project? Check Package Manager");
                            }
                        }
                    }
                    #endif
                }
                else if (_terrainMaterialType == LBLandscape.TerrainMaterialType.LWRP)
                {
                    if (lbLandscape.GetLandscapeTerrainDrawInstanced())
                    {
                        // Check the custom material on the first terrain
                        Material _tCustomMat = lbLandscape.GetLandscapeTerrainCustomMaterial();

                        // The shader path changed in LWRP 4.0 so make sure we have the correct version
                        if (_tCustomMat != null && _tCustomMat.shader.name.Contains("Lightweight Render Pipeline/Terrain/Lit"))
                        {
                            if (_tCustomMat.HasProperty("_TERRAIN_INSTANCED_PERPIXEL_NORMAL"))
                            {
                                isPerPixelNormalsEnabled = (_tCustomMat.GetFloat("_TERRAIN_INSTANCED_PERPIXEL_NORMAL") > 0f);
                            }
                            else
                            {
                                Debug.LogWarning("LWRP material property _TERRAIN_INSTANCED_PERPIXEL_NORMAL is not available. Do you have LWRP 4.0.1 or newer in your project? Check Package Manager");
                            }
                        }
                    }
                }
            }
            #endif

            return isPerPixelNormalsEnabled;
        }


        #endregion

        #region Common Editor methods - Templates

        public static bool SaveTemplate
        (
            LBLandscape landscape,
            string LBEditorVersion,
            string landscapeTemplateName,
            ref bool isSceneSaveRequired,
            bool createTemplatePackage,
            bool addMapTexturesToTemplatePackage,
            bool addLayerHeightmapTexturesToTemplatePackage,
            bool addLBLightingToTemplate,
            bool addPathsToTemplate,
            bool addStencilsToTemplate,
            bool addPathMeshMaterialsToTemplate
        )
        {
            bool isSuccessful = false;

            string landscapeName = landscape.name;

            // Create a new gameobject in the scene hierarchy that we can turn into a prefab
            GameObject newTemplateObj = new GameObject(landscapeName + " template");
            if (newTemplateObj == null) { Debug.LogWarning("Landscape Builder - could not create temporary gameobject for " + landscapeName + " template"); }
            else
            {
                LBTemplate lbTemplate = newTemplateObj.AddComponent<LBTemplate>();
                if (lbTemplate == null)
                {
                    Debug.LogWarning("Landscape Builder - could not add LBTemplate component " + landscapeName + " template");
                    GameObject.DestroyImmediate(newTemplateObj);
                }
                else
                {
                    // This is the installed version of LB when the template was created
                    lbTemplate.LBVersion = LBEditorVersion;
                    // This is the version of the landscape when the template was created
                    lbTemplate.LastUpdatedVersion = landscape.LastUpdatedVersion;

                    // The version of Unity that created this template
                    lbTemplate.templateUnityVersion = Application.unityVersion;

                    lbTemplate.useLegacyNoiseOffset = landscape.useLegacyNoiseOffset;

                    // Name of the landscape game object
                    lbTemplate.landscapeName = landscapeName;
                    lbTemplate.size = landscape.size;
                    lbTemplate.start = landscape.start;

                    // Landscape terrain settings
                    Vector3 terrainSize3D = landscape.GetLandscapeTerrainSize();
                    lbTemplate.heightmapResolution = landscape.GetLandscapeTerrainHeightmapResolution();
                    lbTemplate.terrainWidth = terrainSize3D.x;
                    lbTemplate.terrainHeight = terrainSize3D.y;
                    lbTemplate.pixelError = landscape.GetLandscapeTerrainPixelError();
                    lbTemplate.baseMapDistance = landscape.GetLandscapeTerrainBaseMapDist();
                    lbTemplate.alphaMapResolution = landscape.GetLandscapeTerrainAlphaMapResolution();
                    lbTemplate.baseTextureResolution = landscape.GetLandscapeTerrainBaseTextureResolution();
                    lbTemplate.treeDistance = landscape.GetLandscapeTerrainTreeDistance();
                    lbTemplate.treeBillboardDistance = landscape.GetLandscapeTerrainTreeBillboardStart();
                    lbTemplate.detailDistance = landscape.GetLandscapeTerrainDetailDistance();
                    lbTemplate.detailDensity = landscape.GetLandscapeTerrainDetailDensity();
                    lbTemplate.detailResolution = landscape.GetLandscapeTerrainDetailResolution();
                    lbTemplate.treeFadeDistance = landscape.GetLandscapeTerrainTreeFadeLength();
                    lbTemplate.grassWindSpeed = landscape.GetLandscapeTerrainGrassWindSpeed();
                    lbTemplate.grassWindRippleSize = landscape.GetLandscapeTerrainGrassWindRippleSize();
                    lbTemplate.grassWindBending = landscape.GetLandscapeTerrainGrassWindBending();
                    lbTemplate.grassWindTint = landscape.GetLandscapeTerrainGrassWindTint();
                    lbTemplate.terrainLegacySpecular = landscape.GetLandscapeTerrainLegacySpecular();
                    lbTemplate.terrainLegacyShininess = landscape.GetLandscapeTerrainLegacyShininess();
                    lbTemplate.useTerrainDrawInstanced = landscape.GetLandscapeTerrainDrawInstanced();
                    lbTemplate.useTerrainPerPixelNormals = GetTerrainPerPixelNormals(landscape);
                    lbTemplate.terrainGroupingID = landscape.GetLandscapeTerrainGroupingID();
                    lbTemplate.terrainAutoConnect = landscape.GetLandscapeTerrainAutoConnect();

                    // Synchronize with a duplicate enum in lbTemplate
                    lbTemplate.terrainMaterialType = (LBTemplate.TerrainMaterialType)landscape.GetTerrainMaterialType();

                    // Check if any lists are null
                    if (landscape.topographyLayersList == null) { landscape.topographyLayersList = new List<LBLayer>(); }
                    if (landscape.terrainTexturesList == null) { landscape.terrainTexturesList = new List<LBTerrainTexture>(); }
                    if (landscape.terrainTreesList == null) { landscape.terrainTreesList = new List<LBTerrainTree>(); }
                    if (landscape.terrainGrassList == null) { landscape.terrainGrassList = new List<LBTerrainGrass>(); }
                    if (landscape.lbGroupList == null) { landscape.lbGroupList = new List<LBGroup>(); }
                    if (landscape.landscapeMeshList == null) { landscape.landscapeMeshList = new List<LBLandscapeMesh>(); }
                    if (landscape.landscapeWaterList == null) { landscape.landscapeWaterList = new List<LBWater>(); }

                    // Update texture names to help detect missing textures when importing templates into another project
                    for (int txIdx = 0; txIdx < landscape.terrainTexturesList.Count; txIdx++)
                    {
                        // Only attempt to update the textureName if it is null or an empty string.
                        // This avoids loosing details about an already missing texture
                        if (string.IsNullOrEmpty(landscape.terrainTexturesList[txIdx].textureName))
                        {
                            if (landscape.terrainTexturesList[txIdx].texture != null)
                            {
                                landscape.terrainTexturesList[txIdx].textureName = landscape.terrainTexturesList[txIdx].texture.name;
                                isSceneSaveRequired = true;
                            }
                        }

                        // Only attempt to update the normalMapName if it is null or an empty string.
                        // This avoids loosing details about an already missing normalMap
                        if (string.IsNullOrEmpty(landscape.terrainTexturesList[txIdx].normalMapName))
                        {
                            if (landscape.terrainTexturesList[txIdx].normalMap != null)
                            {
                                landscape.terrainTexturesList[txIdx].normalMapName = landscape.terrainTexturesList[txIdx].normalMap.name;
                                isSceneSaveRequired = true;
                            }
                        }
                    }

                    // Update Grass texture names to help detect missing textures when importing templates into another project
                    for (int grIdx = 0; grIdx < landscape.terrainGrassList.Count; grIdx++)
                    {
                        // Only attempt to update the grass textureName if it is null or an empty string.
                        // This avoids loosing details about an already missing grass texture
                        if (string.IsNullOrEmpty(landscape.terrainGrassList[grIdx].textureName))
                        {
                            if (landscape.terrainGrassList[grIdx].texture != null)
                            {
                                landscape.terrainGrassList[grIdx].textureName = landscape.terrainGrassList[grIdx].texture.name;
                                isSceneSaveRequired = true;
                            }
                        }

                        // Only attempt to update the grass meshPrefName if it is null or an empty string.
                        // This avoids loosing details about an already missing grass mesh prefab
                        if (string.IsNullOrEmpty(landscape.terrainGrassList[grIdx].meshPrefabName))
                        {
                            if (landscape.terrainGrassList[grIdx].meshPrefab != null)
                            {
                                landscape.terrainGrassList[grIdx].meshPrefabName = landscape.terrainGrassList[grIdx].meshPrefab.name;
                                isSceneSaveRequired = true;
                            }
                        }
                    }

                    // Update Tree prefab names to help detect missing prefabs when importing templates into another project
                    for (int trIdx = 0; trIdx < landscape.terrainTreesList.Count; trIdx++)
                    {
                        // Only attempt to update the tree prefabName if it is null or an empty string.
                        // This avoids loosing details about an already missing tree prefab
                        if (string.IsNullOrEmpty(landscape.terrainTreesList[trIdx].prefabName))
                        {
                            if (landscape.terrainTreesList[trIdx].prefab != null)
                            {
                                landscape.terrainTreesList[trIdx].prefabName = landscape.terrainTreesList[trIdx].prefab.name;
                                isSceneSaveRequired = true;
                            }
                        }
                    }

                    // Update Mesh prefab names to help detect missing prefabs when importing templates into another project
                    for (int meshIdx = 0; meshIdx < landscape.landscapeMeshList.Count; meshIdx++)
                    {
                        // Only attempt to update the mesh prefabName if it is null or an empty string.
                        // This avoids loosing details about an already missing mesh prefab
                        if (string.IsNullOrEmpty(landscape.landscapeMeshList[meshIdx].prefabName))
                        {
                            if (landscape.landscapeMeshList[meshIdx].prefab != null)
                            {
                                landscape.landscapeMeshList[meshIdx].prefabName = landscape.landscapeMeshList[meshIdx].prefab.name;
                                isSceneSaveRequired = true;
                            }
                        }
                    }

                    int numGroups = landscape.lbGroupList == null ? 0 : landscape.lbGroupList.Count;

                    // Update Group Member prefab names to help detect missing prefabs when importing templates into another project
                    for (int grpIdx = 0; grpIdx < numGroups; grpIdx++)
                    {
                        if (landscape.lbGroupList[grpIdx].groupMemberList != null)
                        {
                            List<LBGroupMember> groupMemberList = landscape.lbGroupList[grpIdx].groupMemberList;
                            for (int grpMbrIdx = 0; grpMbrIdx < groupMemberList.Count; grpMbrIdx++)
                            {
                                // Only attempt to update the prefab prefabName if it is null or an empty string.
                                // This avoids loosing details about an already missing prefab
                                if (string.IsNullOrEmpty(groupMemberList[grpMbrIdx].prefabName))
                                {
                                    if (groupMemberList[grpMbrIdx].prefab != null)
                                    {
                                        groupMemberList[grpMbrIdx].prefabName = groupMemberList[grpMbrIdx].prefab.name;
                                        isSceneSaveRequired = true;
                                    }
                                }
                            }
                        }
                    }

                    // Landscape class lists - perform deep copy (#SMS 2.0.6 Beta 6f)
                    // Attempts to avoid issue where a template prefab is created (and later deleted) which could delete data
                    // from the landscape meta-data in the scene.
                    lbTemplate.topographyLayersList = landscape.topographyLayersList.ConvertAll(lyr => new LBLayer(lyr));
                    lbTemplate.terrainTexturesList = landscape.terrainTexturesList.ConvertAll(tt => new LBTerrainTexture(tt));
                    lbTemplate.terrainTreesList = landscape.terrainTreesList.ConvertAll(tt => new LBTerrainTree(tt));
                    lbTemplate.terrainGrassList = landscape.terrainGrassList.ConvertAll(tg => new LBTerrainGrass(tg));
                    lbTemplate.lbGroupList = landscape.lbGroupList.ConvertAll(grp => new LBGroup(grp));
                    lbTemplate.landscapeMeshList = landscape.landscapeMeshList.ConvertAll(msh => new LBLandscapeMesh(msh));
                    lbTemplate.landscapeWaterList = landscape.landscapeWaterList.ConvertAll(wtr => new LBWater(wtr, false));

                    // Check for surface and/or base mesh materials in Group Object Paths
                    for (int grpIdx = 0; grpIdx < numGroups; grpIdx++)
                    {
                        int numGrpMembers = landscape.lbGroupList[grpIdx].groupMemberList == null ? 0 : landscape.lbGroupList[grpIdx].groupMemberList.Count;

                        for (int grpMbrIdx = 0; grpMbrIdx < numGrpMembers; grpMbrIdx++)
                        {
                            LBGroupMember _lbGroupMember = landscape.lbGroupList[grpIdx].groupMemberList[grpMbrIdx];
                            // A deep copy will attempt create a new instance of the material using our LBGroupMember clone constructor
                            // In this case we need to reference the actual material in the project folder.
                            // If the Object Path was a duplicate of an existing Object Path, then this may also fail.
                            if (_lbGroupMember != null && _lbGroupMember.lbMemberType == LBGroupMember.LBMemberType.ObjPath && _lbGroupMember.lbObjPath != null)
                            {
                                lbTemplate.lbGroupList[grpIdx].groupMemberList[grpMbrIdx].lbObjPath.surfaceMeshMaterial = _lbGroupMember.lbObjPath.surfaceMeshMaterial;
                                lbTemplate.lbGroupList[grpIdx].groupMemberList[grpMbrIdx].lbObjPath.baseMeshMaterial = _lbGroupMember.lbObjPath.baseMeshMaterial;
                            }
                        }
                    }

                    // Final Pass varibles
                    lbTemplate.useFinalPassSmoothing = landscape.useFinalPassSmoothing;
                    lbTemplate.finalPassSmoothingIterations = landscape.finalPassSmoothingIterations;
                    lbTemplate.finalPassPixelRange = landscape.finalPassPixelRange;
                    lbTemplate.fPassSmoothStencilGUID = landscape.fPassSmoothStencilGUID;
                    lbTemplate.fPassSmoothStencilLayerGUID = landscape.fPassSmoothStencilLayerGUID;
                    lbTemplate.fPassSmoothFilterMode = landscape.fPassSmoothFilterMode;

                    lbTemplate.thermalErosionPreset = landscape.thermalErosionPreset;
                    lbTemplate.useThermalErosion = landscape.useThermalErosion;
                    lbTemplate.thermalErosionIterations = landscape.thermalErosionIterations;
                    lbTemplate.thermalErosionTalusAngle = landscape.thermalErosionTalusAngle;
                    lbTemplate.thermalErosionStrength = landscape.thermalErosionStrength;
                    lbTemplate.fPassThErosionStencilGUID = landscape.fPassThErosionStencilGUID;
                    lbTemplate.fPassThErosionStencilLayerGUID = landscape.fPassThErosionStencilLayerGUID;
                    lbTemplate.fPassThErosionFilterMode = landscape.fPassThErosionFilterMode;

                    // Group Settings
                    lbTemplate.autoRefreshGroupDesigner = landscape.autoRefreshGroupDesigner;

                    // Vegetation Integration variables
                    lbTemplate.useVegetationSystem = landscape.useVegetationSystem;
                    lbTemplate.useVegetationSystemTextures = landscape.useVegetationSystemTextures;

                    // Landscape Extension
                    lbTemplate.useLandscapeExtension = landscape.useLandscapeExtension;
                    if (landscape.lbLandscapeExtension != null) { lbTemplate.lbLandscapeExtension = new LBLandscapeExtension(landscape.lbLandscapeExtension); }
                    else { lbTemplate.lbLandscapeExtension = new LBLandscapeExtension(); }

                    // GPU acceleration
                    lbTemplate.useGPUTexturing = landscape.useGPUTexturing;
                    lbTemplate.useGPUTopography = landscape.useGPUTopography;
                    lbTemplate.useGPUGrass = landscape.useGPUGrass;
                    lbTemplate.useGPUPath = landscape.useGPUPath;

                    // Undo override
                    lbTemplate.isUndoTopographyDisabled = landscape.isUndoTopographyDisabled;

                    // Topography Mask - there is one per landscape
                    lbTemplate.topographyMaskMode = (LBTemplate.MaskMode)landscape.topographyMaskMode;
                    lbTemplate.distanceToCentreMask = landscape.distanceToCentreMask;
                    lbTemplate.maskWarpAmount = landscape.maskWarpAmount;
                    lbTemplate.maskNoiseTileSize = landscape.maskNoiseTileSize;
                    lbTemplate.maskNoiseOffsetX = landscape.maskNoiseOffsetX;
                    lbTemplate.maskNoiseOffsetY = landscape.maskNoiseOffsetY;
                    lbTemplate.maskNoiseCurveModifier = landscape.maskNoiseCurveModifier;

                    // Does user wish to include LBLighting?
                    if (addLBLightingToTemplate)
                    {
                        // Only include LBLighting if it is in the scene
                        LBLighting lbLighting = GameObject.FindObjectOfType<LBLighting>();
                        if (lbLighting == null)
                        {
                            lbTemplate.isLBLightingIncluded = false;
                            Debug.LogWarning("Landscape Builder - Export Template - No LBLighting found in the scene");
                        }
                        else
                        {
                            lbTemplate.isLBLightingIncluded = true;
                            lbTemplate.AddLBLightingSettings(lbLighting);
                        }
                    }
                    else { lbTemplate.isLBLightingIncluded = false; }

                    // Create an empty list of Asset Paths for the mesh materials
                    List<string> lbMapPathMaterialAssetPathList = new List<string>();

                    if (addPathsToTemplate)
                    {
                        // Find any Camera Paths in this landscape (changed from all in scene in 1.3.2 Beta 9b)
                        List<LBCameraPath> lbCameraPathList = LBCameraPath.GetCameraPathsInLandscape(landscape);
                        if (lbCameraPathList != null)
                        {
                            // Get the LBPath from each LBCameraPath and add it to the LBTemplate instance
                            foreach (LBCameraPath lbCameraPath in lbCameraPathList)
                            {
                                if (lbCameraPath.lbPath != null)
                                {
                                    // Update the path name before adding to the template
                                    if (lbCameraPath.gameObject != null)
                                    {
                                        lbCameraPath.lbPath.pathName = lbCameraPath.gameObject.name;
                                        isSceneSaveRequired = true;
                                    }
                                    lbTemplate.lbPathList.Add(lbCameraPath.lbPath);
                                }
                            }
                        }

                        // Find all MapPaths in the this landscape
                        List<LBMapPath> lbMapPathList = LBMapPath.GetMapPathsInLandscape(landscape);
                        if (lbMapPathList != null)
                        {
                            // Get the LBPath from each LBMapPath and add it to the LBTemplate instance
                            foreach (LBMapPath lbMapPath in lbMapPathList)
                            {
                                if (lbMapPath.lbPath != null)
                                {
                                    // Update the path name before adding to the template
                                    if (lbMapPath.gameObject != null)
                                    {
                                        lbMapPath.lbPath.pathName = lbMapPath.gameObject.name;

                                        // Check for a mesh
                                        if (lbMapPath.lbPath.lbMesh != null && addPathMeshMaterialsToTemplate)
                                        {
                                            lbMapPath.lbPath.meshTempMaterial = lbMapPath.meshMaterial;

                                            // If also creating a package, remember the asset path for this mesh material
                                            if (createTemplatePackage)
                                            {
                                                string mapPathMatAssetPath = UnityEditor.AssetDatabase.GetAssetPath(lbMapPath.meshMaterial);
                                                if (!string.IsNullOrEmpty(mapPathMatAssetPath))
                                                {
                                                    lbMapPathMaterialAssetPathList.Add(mapPathMatAssetPath);
                                                }
                                            }
                                        }
                                        else { lbMapPath.lbPath.meshTempMaterial = null; }

                                        isSceneSaveRequired = true;
                                    }
                                    lbTemplate.lbPathList.Add(lbMapPath.lbPath);
                                }
                            }
                        }
                    }

                    if (addStencilsToTemplate)
                    {
                        List<LBStencil> lbStencilList = LBStencil.GetStencilsInLandscape(landscape, true);
                        if (lbTemplate.lbTemplateStencilList == null) { lbTemplate.lbTemplateStencilList = new List<LBTemplateStencil>(); }

                        if (lbTemplate.lbTemplateStencilList != null && lbStencilList != null)
                        {
                            // Create a LBTemplateStencil for each Stencil in the landscape, and add it to the Template.
                            // NOTE Does not copy the USHORT layerArray or render textures in the Stencil Layers as we want to keep
                            // the Template as small as possible.
                            foreach (LBStencil lbStencil in lbStencilList)
                            {
                                if (lbStencil != null)
                                {
                                    LBTemplateStencil lbTemplateStencil = new LBTemplateStencil(lbStencil);
                                    if (lbTemplateStencil != null) { lbTemplate.lbTemplateStencilList.Add(lbTemplateStencil); }
                                }
                            }
                        }
                    }

                    // Create template folders if they don't already exist
                    LBEditorHelper.CheckFolder("Assets/LandscapeBuilder/Templates");

                    if (!Directory.Exists("LandscapeBuilder/TemplatePackages"))
                    {
                        Directory.CreateDirectory("LandscapeBuilder/TemplatePackages");
                    }

                    // Create a prefab
                    bool continueToSave = false;
                    string templateFullPath = Application.dataPath + "/LandscapeBuilder/Templates/" + landscapeTemplateName + ".prefab";
                    string templateAssetPath = "Assets/LandscapeBuilder/Templates/" + landscapeTemplateName + ".prefab";
                    if (File.Exists(templateFullPath))
                    {
                        if (EditorUtility.DisplayDialog("Template Already Exists", "Are you sure you want to save the template? The currently existing" +
                                                " template will be lost.", "Overwrite", "Cancel"))
                        {
                            if (!AssetDatabase.DeleteAsset(templateAssetPath))
                            {
                                Debug.LogWarning("Landscape Builder - could not overwrite Assets/LandscapeBuilder/Templates/" + landscapeTemplateName + " for " + landscapeName + " template");
                            }
                            else { continueToSave = true; }
                        }
                    }
                    else { continueToSave = true; }

                    if (continueToSave)
                    {
                        #if UNITY_2018_3_OR_NEWER
                        GameObject templatePrefabGO = PrefabUtility.SaveAsPrefabAsset(newTemplateObj, templateAssetPath);
                        #else
                        GameObject templatePrefabGO = PrefabUtility.CreatePrefab(templateAssetPath, newTemplateObj);
                        #endif

                        if (templatePrefabGO != null && createTemplatePackage)
                        {
                            // Always include the template prefab (this is meta-data only)
                            List<string> templateAssetPaths = new List<string>();
                            templateAssetPaths.Add(templateAssetPath);

                            // Although technically users could include 3rd party textures in a LBMap texture, they are MUCH more likely to use
                            // map textures generated from within Landscape Builder.
                            if (addMapTexturesToTemplatePackage)
                            {
                                List<string> uniqueMapTexPaths = LBLandscapeOperations.GetMapTextureAssetPathsFromLandscape(landscape, true);
                                if (uniqueMapTexPaths != null)
                                {
                                    if (uniqueMapTexPaths.Count > 0) { templateAssetPaths.AddRange(uniqueMapTexPaths); }
                                }
                            }

                            if (addLayerHeightmapTexturesToTemplatePackage)
                            {
                                // Get a list of the image-based topography layers with a heightmap texture
                                List<LBLayer> layerList = landscape.topographyLayersList.FindAll(lyr => lyr.heightmapImage != null && (lyr.type == LBLayer.LayerType.ImageBase || lyr.type == LBLayer.LayerType.ImageAdditive || lyr.type == LBLayer.LayerType.ImageSubtractive || lyr.type == LBLayer.LayerType.ImageDetail));
                                if (layerList != null)
                                {
                                    //Debug.Log("Save Template found " + layerList.Count + " image layers with a heightmap");
                                    for (int lyrIdx = 0; lyrIdx < layerList.Count; lyrIdx++)
                                    {
                                        // Get the path in the asset db to the image-based layer heightmap texture
                                        string heightmapPath = UnityEditor.AssetDatabase.GetAssetPath(layerList[lyrIdx].heightmapImage);
                                        if (!string.IsNullOrEmpty(heightmapPath)) { templateAssetPaths.Add(heightmapPath); }
                                    }
                                }
                            }

                            // Add the list of map path mesh materials we add earlier.
                            if (addPathMeshMaterialsToTemplate && lbMapPathMaterialAssetPathList.Count > 0)
                            {
                                templateAssetPaths.AddRange(lbMapPathMaterialAssetPathList);
                            }

                            AssetDatabase.ExportPackage(templateAssetPaths.ToArray(), "LandscapeBuilder/TemplatePackages/" + landscapeTemplateName + ".unitypackage", ExportPackageOptions.Interactive);
                        }
                        LBEditorHelper.HighlightItemInProjectWindow(templateAssetPath);
                    }

                    // Cleanup scene hierarchy
                    GameObject.DestroyImmediate(newTemplateObj);
                }
            }

            return isSuccessful;
        }

        

        #endregion
    }
}
