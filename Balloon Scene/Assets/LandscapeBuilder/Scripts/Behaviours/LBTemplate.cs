// Landscape Builder. Copyright (c) 2016-2019 SCSM Pty Ltd. All rights reserved.
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace LandscapeBuilder
{
    [System.Serializable]
    public class LBTemplate : MonoBehaviour
    {
        #region Enumeration
        // This is a copy from LandscapeBuilderWindow.cs
        public enum TerrainMaterialType
        {
            BuiltInStandard = 0,
            BuiltInLegacyDiffuse = 1,
            BuiltInLegacySpecular = 2,
            LBStandard = 3,
            MegaSplat = 7,
            MicroSplat = 8,
            ReliefTerrainPack = 10,
            LWRP = 11,
            HDRP = 12,
            Custom = 20
        }

        public enum MaskMode
        {
            None = 0,
            DistanceToCentre = 1,
            Noise = 2
        }
        #endregion

        // This is the installed version of LB when the template was created
        [HideInInspector] public string LBVersion = "1.0";

        // This lets us edit the landscape name in the template while in debug mode
        public string landscapeName;
        [HideInInspector] public bool isLBLightingIncluded = false;
        [HideInInspector] public bool useLegacyNoiseOffset = true;

        #region Terrain Settings
        [HideInInspector] public int heightmapResolution = 513;
        [HideInInspector] public float terrainWidth = 2000f;
        [HideInInspector] public float terrainHeight = 2000f;
        [HideInInspector] public float pixelError = 1f;
        [HideInInspector] public float baseMapDistance = 1500f;
        [HideInInspector] public int baseTextureResolution = 512;
        [HideInInspector] public int alphaMapResolution = 512;
        [HideInInspector] public int landscapeLayerIndex = 0; // "Default" Unity inbuild layer [0..31]
        [HideInInspector] public float treeDistance = 10000f;
        [HideInInspector] public float treeBillboardDistance = 200f;
        [HideInInspector] public float detailDistance = 200f;
        [HideInInspector] public float detailDensity = 1f;
        [HideInInspector] public int detailResolution = 1024;
        [HideInInspector] public float treeFadeDistance = 5f;
        [HideInInspector] public bool showTerrainGrassWindSettings = false;
        [HideInInspector] public float grassWindSpeed = 0.5f;
        [HideInInspector] public float grassWindRippleSize = 0.5f;
        [HideInInspector] public float grassWindBending = 0.5f;
        [HideInInspector] public Color grassWindTint = Color.white;
        [HideInInspector] public TerrainMaterialType terrainMaterialType = TerrainMaterialType.BuiltInStandard;
        [HideInInspector] public Color terrainLegacySpecular = new Color(0.5f, 0.5f, 0.5f, 1f);
        [HideInInspector] public float terrainLegacyShininess = 0.1f;
        [HideInInspector] public bool useTerrainDrawInstanced = true; // New in U2018.3
        [HideInInspector] public bool useTerrainPerPixelNormals = false; // New in U2018.3 with LWRP 4.0.1+
        [HideInInspector] public int terrainGroupingID = 100; // New in U2018.3
        [HideInInspector] public bool terrainAutoConnect = true; // New in U2018.3
        #endregion

        #region Landscape class lists
        // The lists are visible in debug mode
        public List<LBLayer> topographyLayersList;
        public List<LBTerrainTexture> terrainTexturesList;
        public List<LBTerrainTree> terrainTreesList;
        public List<LBTerrainGrass> terrainGrassList;
        public List<LBLandscapeMesh> landscapeMeshList;
        public List<LBGroup> lbGroupList;
        public List<LBWater> landscapeWaterList;
        [HideInInspector] public Vector2 size = Vector2.zero;
        [HideInInspector] public Vector3 start = Vector3.zero;
        #endregion

        #region Topography Mask (one per landscape)
        [HideInInspector] public MaskMode topographyMaskMode = MaskMode.None;
        [HideInInspector] public AnimationCurve distanceToCentreMask = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        [HideInInspector] public float maskWarpAmount = 0f;
        [HideInInspector] public float maskNoiseTileSize = 2000f;
        [HideInInspector] public float maskNoiseOffsetX = 0f;
        [HideInInspector] public float maskNoiseOffsetY = 0f;
        [HideInInspector] public AnimationCurve maskNoiseCurveModifier = AnimationCurve.Linear(0f, 0.5f, 1f, 1f);
        #endregion

        [HideInInspector] public Material LBStandardTerrainMaterial;

        // The version this landscape was last updated with
        [HideInInspector] public string LastUpdatedVersion = "1.0";

        // The Unity version this template was created with
        [HideInInspector] public string templateUnityVersion = "Unknown";

        #region Final Pass

        [HideInInspector] public bool useFinalPassSmoothing = false;
        [HideInInspector] public int finalPassSmoothingIterations = 1;
        [HideInInspector] public int finalPassPixelRange = 1;
        [HideInInspector] public string fPassSmoothStencilGUID = string.Empty;
        [HideInInspector] public string fPassSmoothStencilLayerGUID = string.Empty;
        [HideInInspector] public LBFilter.FilterMode fPassSmoothFilterMode = LBFilter.FilterMode.AND;

        [HideInInspector] public LBLandscape.ThermalErosionPreset thermalErosionPreset = LBLandscape.ThermalErosionPreset.SoftEarthSlippage;
        [HideInInspector] public bool useThermalErosion = false;
        [HideInInspector] public int thermalErosionIterations = 50;
        [HideInInspector] public float thermalErosionTalusAngle = 45f;
        [HideInInspector] public float thermalErosionStrength = 0.2f;
        [HideInInspector] public string fPassThErosionStencilGUID = string.Empty;
        [HideInInspector] public string fPassThErosionStencilLayerGUID = string.Empty;
        [HideInInspector] public LBFilter.FilterMode fPassThErosionFilterMode = LBFilter.FilterMode.AND;

        #endregion

        #region Tree Settings
        // Remember Tree settings for landscape
        [HideInInspector] public LBTerrainTree.TreePlacementSpeed treePlacementSpeed = LBTerrainTree.TreePlacementSpeed.FastPlacement;
        [HideInInspector] public bool treesHaveColliders = false;
        #endregion

        #region LBLighting
        [HideInInspector] public Light sun;
        [HideInInspector] public float sunIntensity;
        [HideInInspector] public float yAxisRotation;

        [HideInInspector] public LBLighting.LightingMode lightingMode;
        [HideInInspector] public LBLighting.SetupMode setupMode;
        [HideInInspector] public LBLighting.EnvAmbientSource envAmbientSource;

        [HideInInspector] public LBLighting.SkyboxType skyboxType;
        [HideInInspector] public List<LBSkybox> skyboxesList;
        [HideInInspector] public Material proceduralSkybox;
        [HideInInspector] public Material blendedSkybox;

        // Dynamic lighting variables
        [HideInInspector] public float dayLength;
        [HideInInspector] public bool realtimeTerrainLighting;
        [HideInInspector] public LBLighting.ReflectionProbeUpdateMode reflProbesUpdateMode;
        [HideInInspector] public float lightingUpdateInterval;

        [HideInInspector] public float startTimeOfDay;
        [HideInInspector] public float sunriseTime;
        [HideInInspector] public float sunsetTime;

        // Ambient Light and Night Setttings
        // dayAmbientLight and nightAmbientLight are also used for ambient Sky colour for environment gradient mode
        [HideInInspector] public Color dayAmbientLight;
        [HideInInspector] public Color nightAmbientLight;
        [HideInInspector] public Color dayAmbientGndLight;
        [HideInInspector] public Color nightAmbientGndLight;
        [HideInInspector] public Color dayAmbientHznLight;
        [HideInInspector] public Color nightAmbientHznLight;

        #if UNITY_2018_1_OR_NEWER
        [HideInInspector] [ColorUsageAttribute(true, true)] public Color dayAmbientLightHDR;
        [HideInInspector] [ColorUsageAttribute(true, true)] public Color nightAmbientLightHDR;
        [HideInInspector] [ColorUsageAttribute(true, true)] public Color dayAmbientGndLightHDR;
        [HideInInspector] [ColorUsageAttribute(true, true)] public Color nightAmbientGndLightHDR;
        [HideInInspector] [ColorUsageAttribute(true, true)] public Color dayAmbientHznLightHDR;
        [HideInInspector] [ColorUsageAttribute(true, true)] public Color nightAmbientHznLightHDR;
        #else
        [HideInInspector] [ColorUsageAttribute(true, true, 0f, 8f, 0.125f, 3f)] public Color dayAmbientLightHDR;
        [HideInInspector] [ColorUsageAttribute(true, true, 0f, 8f, 0.125f, 3f)] public Color nightAmbientLightHDR;
        [HideInInspector] [ColorUsageAttribute(true, true, 0f, 8f, 0.125f, 3f)] public Color dayAmbientGndLightHDR;
        [HideInInspector] [ColorUsageAttribute(true, true, 0f, 8f, 0.125f, 3f)] public Color nightAmbientGndLightHDR;
        [HideInInspector] [ColorUsageAttribute(true, true, 0f, 8f, 0.125f, 3f)] public Color dayAmbientHznLightHDR;
        [HideInInspector] [ColorUsageAttribute(true, true, 0f, 8f, 0.125f, 3f)] public Color nightAmbientHznLightHDR;
        #endif

        [HideInInspector] public Color dayFogColour;
        [HideInInspector] public Color nightFogColour;

        [HideInInspector] public float dayFogDensity;
        [HideInInspector] public float nightFogDensity;

        // Advanced setup variables
        [HideInInspector] public AnimationCurve sunIntensityCurve;
        [HideInInspector] public Gradient ambientLightGradient;
        [HideInInspector] public Gradient ambientHznLightGradient;
        [HideInInspector] public Gradient ambientGndLightGradient;
        [HideInInspector] public Gradient fogColourGradient;
        [HideInInspector] public AnimationCurve fogDensityCurve;
        [HideInInspector] public float minFogDensity;
        [HideInInspector] public float maxFogDensity;
        [HideInInspector] public AnimationCurve moonIntensityCurve;
        [HideInInspector] public AnimationCurve starVisibilityCurve;

        // Celestials variables
        [HideInInspector] public bool useCelestials;
        [HideInInspector] public LBCelestials celestials;
        [HideInInspector] public Camera mainCamera;
        [HideInInspector] public int numberOfStars;
        [HideInInspector] public float starSize;

        [HideInInspector] public bool useMoon;
        [HideInInspector] public Light moon;
        [HideInInspector] public float moonIntensity;
        [HideInInspector] public float moonYAxisRotation;
        [HideInInspector] public float moonSize;

        // Weather variables
        // Don't store the list of cameras as they may change
        [HideInInspector] public bool useWeather;
        [HideInInspector] public bool isHDREnabled;
        [HideInInspector] public Transform rainParticleSystemPrefab;
        [HideInInspector] public Transform hailParticleSystemPrefab;
        [HideInInspector] public Transform snowParticleSystemPrefab;
        [HideInInspector] public bool useClouds;
        [HideInInspector] public bool use3DNoise;
        [HideInInspector] public LBImageFX.CloudStyle cloudStyle;
        [HideInInspector] public LBImageFX.CloudQualityLevel cloudsQualityLevel;
        [HideInInspector] public float cloudsDetailAmount;
        [HideInInspector] public Color cloudsUpperColourDay;
        [HideInInspector] public Color cloudsLowerColourDay;
        [HideInInspector] public Color cloudsUpperColourNight;
        [HideInInspector] public Color cloudsLowerColourNight;
        #if UNITY_2018_1_OR_NEWER
        [HideInInspector] [ColorUsageAttribute(true, true)] public Color cloudsUpperColourDayHDR;
        [HideInInspector] [ColorUsageAttribute(true, true)] public Color cloudsLowerColourDayHDR;
        [HideInInspector] [ColorUsageAttribute(true, true)] public Color cloudsUpperColourNightHDR;
        [HideInInspector] [ColorUsageAttribute(true, true)] public Color cloudsLowerColourNightHDR;
        #else
        [HideInInspector] [ColorUsageAttribute(true, true, 0f, 8f, 0.125f, 3f)] public Color cloudsUpperColourDayHDR;
        [HideInInspector] [ColorUsageAttribute(true, true, 0f, 8f, 0.125f, 3f)] public Color cloudsLowerColourDayHDR;
        [HideInInspector] [ColorUsageAttribute(true, true, 0f, 8f, 0.125f, 3f)] public Color cloudsUpperColourNightHDR;
        [HideInInspector] [ColorUsageAttribute(true, true, 0f, 8f, 0.125f, 3f)] public Color cloudsLowerColourNightHDR;
        #endif
        [HideInInspector] public List<LBWeatherState> weatherStatesList;
        [HideInInspector] public int startWeatherState;
        [HideInInspector] public float minWeatherTransitionDuration;
        [HideInInspector] public float maxWeatherTransitionDuration;
        [HideInInspector] public bool allowAutomaticTransitions;
        [HideInInspector] public bool randomiseWindDirection;
        [HideInInspector] public bool useWindZone;
        [HideInInspector] public List<LBWindZone> lbWindZoneList;

        // Screen clock variables
        [HideInInspector] public bool useScreenClock;
        [HideInInspector] public bool useScreenClockSeconds;
        [HideInInspector] public Canvas lightingCanvas;
        [HideInInspector] public RectTransform screenClockPanel;
        [HideInInspector] public UnityEngine.UI.Text screenClockText;
        [HideInInspector] public Color screenClockTextColour;

        // Screen Fader variables
        [HideInInspector] public bool fadeInOnWake;
        [HideInInspector] public bool fadeOutOnWake;
        [HideInInspector] public float fadeInDuration;
        [HideInInspector] public float fadeOutDuration;

        // Show/Hide variables
        [HideInInspector] public bool showSkyboxSettings;
        [HideInInspector] public bool showGeneralLightingSettings;
        [HideInInspector] public bool showAmbientAndFogSettings;
        [HideInInspector] public bool showCelestialsSettings;
        [HideInInspector] public bool showWeatherSettings;
        [HideInInspector] public bool showWeatherCameraSettings;
        [HideInInspector] public bool showWeatherCloudSettings;
        [HideInInspector] public bool showWeatherStateSettings = false;
        [HideInInspector] public bool showClockSettings;
        [HideInInspector] public bool showScreenFadeSettings;
        #endregion

        #region Paths
        //[HideInInspector] do not hide - allow to show in debug mode
        public List<LBPath> lbPathList;
        #endregion

        #region Stencils
        // Visible in debug mode
        public List<LBTemplateStencil> lbTemplateStencilList;
        #endregion

        #region Group Settings
        [HideInInspector] public bool autoRefreshGroupDesigner = true;

        #endregion

        #region Vegetation Studio
        [HideInInspector] public bool useVegetationSystem = false;
        [HideInInspector] public bool useVegetationSystemTextures = false;
        #endregion

        #region Landscape Extension
        [HideInInspector] public bool useLandscapeExtension = false;
        public LBLandscapeExtension lbLandscapeExtension;
        #endregion

        #region GPU Acceleration
        [HideInInspector] public bool useGPUTexturing = false;
        [HideInInspector] public bool useGPUTopography = false;
        [HideInInspector] public bool useGPUGrass = false;
        [HideInInspector] public bool useGPUPath = false;

        #endregion

        #region Undo
        [HideInInspector] public bool isUndoTopographyDisabled = false;
        #endregion

        #region TemplateEditor Options
        [HideInInspector] public bool isPopulateLandscape = false;
        #endregion

        #region Debug Mode
        [HideInInspector] public bool debugMode = false;
        #endregion

        #region Constructors
        // Basic class constructor
        public LBTemplate()
        {
            skyboxesList = new List<LBSkybox>();
            lbPathList = new List<LBPath>();
            lbTemplateStencilList = new List<LBTemplateStencil>();
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Create a new landscape using the original size of the landscape used to make the LBTemplate
        /// </summary>
        /// <param name="landscapeName"></param>
        /// <returns></returns>
        public LBLandscape CreateLandscapeFromTemplate(string landscapeName)
        {
            LBLandscape landscape = null;

            Vector3 startPosition = Vector3.zero;
            Vector2 landscapeSize = this.size;

            GameObject landscapeGameObject = new GameObject(landscapeName);
            landscapeGameObject.transform.position = startPosition;
            int index = 0;
            //int numberOfTerrains = Mathf.FloorToInt((landscapeSize.x / terrainWidth) * (landscapeSize.y / terrainWidth));
            for (float xF = 0f; xF < landscapeSize.x; xF += terrainWidth)
            {
                for (float yF = 0f; yF < landscapeSize.y; yF += terrainWidth)
                {
                    GameObject newTerrainObj = new GameObject("Landscape Terrain " + index.ToString("0000"));
                    newTerrainObj.transform.position = new Vector3(xF, 0f, yF) + startPosition;
                    newTerrainObj.transform.parent = landscapeGameObject.transform;
                    newTerrainObj.layer = landscapeLayerIndex;
                    Terrain newTerrain = newTerrainObj.AddComponent<Terrain>();
                    newTerrain.heightmapPixelError = pixelError;
                    newTerrain.basemapDistance = baseMapDistance;
                    newTerrain.treeDistance = treeDistance;
                    newTerrain.treeBillboardDistance = treeBillboardDistance;
                    newTerrain.detailObjectDistance = detailDistance;
                    newTerrain.detailObjectDensity = detailDensity;
                    newTerrain.treeCrossFadeLength = treeFadeDistance;
                    newTerrain.name = "LandscapeTerrain" + index.ToString("0000");
                    TerrainCollider newTerrainCol = newTerrainObj.AddComponent<TerrainCollider>();
                    TerrainData newTerrainData = new TerrainData();
                    newTerrainData.heightmapResolution = heightmapResolution;
                    newTerrainData.size = new Vector3(terrainWidth, terrainHeight, terrainWidth);
                    newTerrainData.SetDetailResolution(1024, 16);
                    newTerrainData.name = "LandscapeTerrain" + index.ToString("0000");
                    newTerrain.terrainData = newTerrainData;
                    newTerrainCol.terrainData = newTerrainData;
                    index++;

                    //Debug.Log("Creating " + index.ToString() + " of " + numberOfTerrains.ToString() + " terrains");
                }
            }

            if (landscapeGameObject != null)
            {
                landscape = landscapeGameObject.AddComponent<LBLandscape>();
                if (landscape != null)
                {
                    landscape.size = landscapeSize;
                    landscape.start = startPosition;
                    landscape.SetTerrainNeighbours();
                    landscape.LastUpdatedVersion = LastUpdatedVersion;

                    //Debug.Log("Major Version: " + landscape.GetLastUpdateMajorVersion);
                    //Debug.Log("Minor Version: " + landscape.GetLastUpdateMinorVersion);
                    //Debug.Log("Patch Version: " + landscape.GetLastUpdatePatchVersion);

                    if (landscape.GetLastUpdateMajorVersion == 1 && (landscape.GetLastUpdateMinorVersion < 4 || (landscape.GetLastUpdateMinorVersion == 4 && landscape.GetLastUpdatePatchVersion < 2)))
                    {
                        landscape.useLegacyNoiseOffset = true;
                    }
                    else { landscape.useLegacyNoiseOffset = useLegacyNoiseOffset; }
                }
                else { Debug.LogWarning("LBTemplate.CreateLandscapeFromTemplate - could not add LBLandscape component."); }
            }

            return landscape;
        }

        /// <summary>
        /// Apply this template to the landscape provided. It will overwrite any current data within the landscape.
        /// NOTE: Does not apply the topography, textures, grass, trees, paths etc.
        /// Only supports LBWater.WaterResizingMode.StandardAssets
        /// Only applies one waterPrefab for all water bodies in scene
        /// Includes ApplyPathsToScene()
        /// WARNING: Currently won't apply Textures to TerrainData BEFORE attempting to apply LBFilter textures for trees, grass or meshes.
        /// This has been fixed in LandscapeBuilderWindow (Apply Template button) by first applying Topography and Textures. Not so easy to do here...
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public bool ApplyTemplateToLandscape(LBLandscape landscape, bool ignoreStartPosition, bool updateTerrainHeight, Transform waterPrefab, bool showErrors)
        {
            bool isSuccess = false;

            if (landscape == null)
            {
                if (showErrors) { Debug.LogWarning("LBTemplate.ApplyTemplateToLandscape - landscape cannot be null"); }
            }
            else
            {
                GameObject landscapeGameObject = landscape.gameObject;

                if (landscapeGameObject == null)
                {
                    if (showErrors) { Debug.LogWarning("LBTemplate.ApplyTemplateToLandscape - landscape script does not appear to have a gameobject."); }
                }
                else
                {
                    // Apply landscape settings (don't resize)
                    if (ignoreStartPosition) { landscape.start = landscapeGameObject.transform.position; }
                    else { landscape.start = this.start; }
                    landscapeGameObject.transform.position = landscape.start;

                    //Debug.Log("LBTemplate landscape Position: " + landscapeGameObject.transform.position);

                    // Remove any existing water from the landscape
                    List<LBWaterItem> existingWater = new List<LBWaterItem>(landscapeGameObject.GetComponentsInChildren<LBWaterItem>());
                    if (existingWater != null)
                    {
                        for (int w = existingWater.Count; w > 0; w--)
                        {
                            DestroyImmediate(existingWater[w - 1].gameObject);
                        }

                        // Remove any reflection cameras from scene
                        LBWaterOperations.RemoveWaterReflectionCamera("Main Camera");
                        LBWaterOperations.RemoveWaterReflectionCamera("Animation Camera");
                        LBWaterOperations.RemoveWaterReflectionCamera("Celestials Camera");
                        LBWaterOperations.RemoveWaterReflectionCamera("SceneCamera");
                    }

                    RemoveWindZones(landscape);

                    // Apply the lists
                    // NOTE: This ONLY works if there is one landscape in the scene using this list contents
                    landscape.topographyLayersList = new List<LBLayer>();
                    landscape.terrainTexturesList = new List<LBTerrainTexture>();
                    landscape.terrainTreesList = new List<LBTerrainTree>();
                    landscape.terrainGrassList = new List<LBTerrainGrass>();
                    landscape.landscapeMeshList = new List<LBLandscapeMesh>();
                    landscape.lbGroupList = new List<LBGroup>();
                    // If we add water here from the template, duplicates occur when water
                    // is added after terrain data is updated below.
                    //landscape.landscapeWaterList = new List<LBWater>(this.landscapeWaterList);
                    landscape.landscapeWaterList = new List<LBWater>();

                    ApplyStencilsToScene(landscape, true, showErrors);

                    landscape.SetLandscapeTerrains(true);

                    // Apply the lists
                    // Rather than add the template lists to a new List, we need to add clones or copies
                    // of each class instance just in case there are other landscapes in the same scene that
                    // are using the same Templates. For example, the following code could allow 2 landscapes
                    // in the same scene to contain the same instances of LBLayer which would result in an
                    // alteration in one landscape topographyLayersList altering that layer in another landscape.
                    // e.g. landscape.topographyLayersList = new List<LBLayer>(this.topographyLayersList);
                    if (this.topographyLayersList != null)
                    {
                        foreach (LBLayer lbLayer in this.topographyLayersList)
                        {
                            landscape.topographyLayersList.Add(new LBLayer(lbLayer));

                            //Debug.Log("[DEBUG] " + lbLayer.layerTypeMode + " additiveAmount " + lbLayer.additiveAmount);
                        }

                        this.UpdateTopographyLayerLBTerrainData(landscape, true);
                    }

                    ApplyPathsToScene(landscape, true, true, ignoreStartPosition, true);

                    // Apply Topography Mask - there is one per landscape
                    landscape.topographyMaskMode = (LBLandscape.MaskMode)this.topographyMaskMode;
                    landscape.distanceToCentreMask = new AnimationCurve(this.distanceToCentreMask.keys);
                    landscape.maskWarpAmount = this.maskWarpAmount;
                    landscape.maskNoiseTileSize = this.maskNoiseTileSize;
                    landscape.maskNoiseOffsetX = this.maskNoiseOffsetX;
                    landscape.maskNoiseOffsetY = this.maskNoiseOffsetY;
                    landscape.maskNoiseCurveModifier = new AnimationCurve(this.maskNoiseCurveModifier.keys);

                    int numTerrains = landscape.landscapeTerrains == null ? 0 : landscape.landscapeTerrains.Length;

                    // Apply terrain settings to the terrains
                    if (numTerrains > 0)
                    {
                        Vector3 terrainSize = new Vector3(2000f, 2000f, 2000f);

                        terrainSize = landscape.landscapeTerrains[0].terrainData.size;
                        // Override the current terrain height
                        if (updateTerrainHeight) { terrainSize.y = this.terrainHeight; }

                        int terrainIndex = 0;
                        foreach (Terrain terrain in landscape.landscapeTerrains)
                        {
                            if (terrain != null)
                            {
                                // Must set the resolution first because Unity will resize the terrain
                                terrain.terrainData.heightmapResolution = this.heightmapResolution;
                                // Must then set the size back to original
                                terrain.terrainData.size = terrainSize;

                                terrain.heightmapPixelError = this.pixelError;
                                terrain.basemapDistance = this.baseMapDistance;
                                terrain.treeDistance = this.treeDistance;
                                terrain.treeBillboardDistance = this.treeBillboardDistance;
                                terrain.detailObjectDistance = detailDistance;
                                terrain.detailObjectDensity = detailDensity;
                                terrain.treeCrossFadeLength = treeFadeDistance;
                                terrain.terrainData.baseMapResolution = this.baseTextureResolution;
                                terrain.terrainData.SetDetailResolution(1024, 16);
                                terrain.gameObject.layer = this.landscapeLayerIndex;
                                terrain.terrainData.wavingGrassSpeed = this.grassWindSpeed;
                                terrain.terrainData.wavingGrassAmount = this.grassWindRippleSize;
                                terrain.terrainData.wavingGrassStrength = this.grassWindBending;
                                terrain.terrainData.wavingGrassTint = this.grassWindTint;
                                #if !UNITY_2019_2_OR_NEWER
                                terrain.legacySpecular = this.terrainLegacySpecular;
                                terrain.legacyShininess = this.terrainLegacyShininess;
                                #endif
                                #if UNITY_2018_3_OR_NEWER
                                // Currently doesn't work when set on a new terrain in a build
                                terrain.drawInstanced = Application.isEditor ? this.useTerrainDrawInstanced : false;
                                #endif
                                
                                 //Debug.Log("terrain mat type: " + this.terrainMaterialType.ToString());
                                landscape.SetTerrainMaterial(terrain, terrainIndex, (terrainIndex == numTerrains - 1), terrainSize.x, ref this.pixelError, (LBLandscape.TerrainMaterialType)this.terrainMaterialType);

                                terrainIndex++;
                            }
                        }

                        // Calculate offset and scale compared to template source landscape
                        Vector3 landscapeChangeOffset = GetLandscapeChangeOffset(landscape, ignoreStartPosition);
                        Vector3 landscapeChangeScale = GetLandscapeChangeScale(landscape, terrainSize.y);

                        if (this.landscapeWaterList != null)
                        {
                            foreach (LBWater lbWater in this.landscapeWaterList)
                            {
                                // To be unique in the scene, we may need to update the GUID
                                landscape.landscapeWaterList.Add(new LBWater(lbWater, false));
                            }
                        }

                        if (this.terrainTexturesList != null)
                        {
                            foreach (LBTerrainTexture lbTexture in this.terrainTexturesList)
                            {
                                LBTerrainTexture importedLBTerrainTexture = new LBTerrainTexture(lbTexture);
                                if (importedLBTerrainTexture != null)
                                {
                                    #if UNITY_EDITOR
                                    // Re-apply Tinting and texture rotation if they are enabled
                                    if (importedLBTerrainTexture.isTinted && importedLBTerrainTexture.texture != null)
                                    {
                                        LBTextureOperations.SetTextureAttributes(importedLBTerrainTexture.texture, UnityEditor.TextureImporterCompression.Uncompressed, FilterMode.Trilinear, true, 0, true);
                                        importedLBTerrainTexture.tintedTexture = LBTextureOperations.TintTexture(importedLBTerrainTexture.texture, importedLBTerrainTexture.tintColour, importedLBTerrainTexture.tintStrength);
                                    }
                                    if (importedLBTerrainTexture.isRotated && importedLBTerrainTexture.normalMap == null && importedLBTerrainTexture.texture != null)
                                    {
                                        LBTextureOperations.SetTextureAttributes(importedLBTerrainTexture.texture, UnityEditor.TextureImporterCompression.Uncompressed, FilterMode.Trilinear, true, 0, true);
                                        importedLBTerrainTexture.rotatedTexture = LBTextureOperations.RotateTexture(importedLBTerrainTexture.texture, importedLBTerrainTexture.rotationAngle);
                                    }
                                    #endif

                                    landscape.terrainTexturesList.Add(importedLBTerrainTexture);
                                }

                                //landscape.terrainTexturesList.Add(new LBTerrainTexture(lbTexture));
                            }

                            this.UpdateTextureLBTerrainData(landscape, true);
                        }

                        if (this.terrainTreesList != null)
                        {
                            foreach (LBTerrainTree lbTerrainTree in this.terrainTreesList)
                            {
                                // Find matching texture for filters in landscape and update
                                //LBFilter.UpdateTextures(lbTerrainTree.filterList, TerrainTexturesAvailableList, true);

                                landscape.terrainTreesList.Add(new LBTerrainTree(lbTerrainTree));
                            }

                            this.UpdateTreesLBTerrainData(landscape, true);
                        }

                        if (this.terrainGrassList != null)
                        {
                            foreach (LBTerrainGrass lbTerrainGrass in this.terrainGrassList)
                            {
                                // Find matching texture for filters in landscape and update
                                //LBFilter.UpdateTextures(lbTerrainGrass.filterList, TerrainTexturesAvailableList, true);

                                landscape.terrainGrassList.Add(new LBTerrainGrass(lbTerrainGrass));
                            }
                            this.UpdateGrassLBTerrainData(landscape, true);
                        }

                        if (this.landscapeMeshList != null)
                        {
                            foreach (LBLandscapeMesh lbLandscapeMesh in this.landscapeMeshList)
                            {
                                // Find matching texture for filters in landscape and update
                                //LBFilter.UpdateTextures(lbLandscapeMesh.filterList, TerrainTexturesAvailableList, true);

                                landscape.landscapeMeshList.Add(new LBLandscapeMesh(lbLandscapeMesh));
                            }
                        }

                        if (this.lbGroupList != null)
                        {
                            // Clone lbGroupList specific fields - use ConvertAll to do a deep copy
                            // NOTE: Even after doing a ConvertAll, the group members in the scene are still linked to the template prefab.
                            // Changes to either the template prefab (via Debug option) or in the LB Editor will change both.
                            if (lbGroupList != null) { landscape.lbGroupList = lbGroupList.ConvertAll(grp => new LBGroup(grp)); }

                            //foreach (LBGroup lbGroup in this.lbGroupList)
                            //{
                            //    landscape.lbGroupList.Add(new LBGroup(lbGroup));
                            //}

                            LBGroup.LandscapeDimensionLocationChanged(landscape.lbGroupList, landscapeChangeOffset, landscapeChangeScale);
                        }

                        // Final Pass variables
                        landscape.useFinalPassSmoothing = useFinalPassSmoothing;
                        landscape.finalPassSmoothingIterations = finalPassSmoothingIterations;
                        landscape.finalPassPixelRange = finalPassPixelRange;
                        landscape.fPassSmoothStencilGUID = fPassSmoothStencilGUID;
                        landscape.fPassSmoothStencilLayerGUID = fPassSmoothStencilLayerGUID;
                        landscape.fPassSmoothFilterMode = fPassSmoothFilterMode;

                        landscape.thermalErosionPreset = thermalErosionPreset;
                        landscape.useThermalErosion = useThermalErosion;
                        landscape.thermalErosionIterations = thermalErosionIterations;
                        landscape.thermalErosionTalusAngle = thermalErosionTalusAngle;
                        landscape.thermalErosionStrength = thermalErosionStrength;
                        landscape.fPassThErosionStencilGUID = fPassThErosionStencilGUID;
                        landscape.fPassThErosionStencilLayerGUID = fPassThErosionStencilLayerGUID;
                        landscape.fPassThErosionFilterMode = fPassThErosionFilterMode;

                        // Vegetation System integration
                        landscape.useVegetationSystem = useVegetationSystem;
                        landscape.useVegetationSystemTextures = useVegetationSystemTextures;

                        // Landscape Extension
                        landscape.useLandscapeExtension = useLandscapeExtension;
                        if (lbLandscapeExtension != null) { landscape.lbLandscapeExtension = new LBLandscapeExtension(lbLandscapeExtension); }
                        else { landscape.lbLandscapeExtension = new LBLandscapeExtension(); }

                        // GPU acceleration
                        landscape.useGPUTexturing = useGPUTexturing;
                        landscape.useGPUTopography = useGPUTopography;
                        landscape.useGPUGrass = useGPUGrass;
                        landscape.useGPUPath = useGPUPath;

                        // Apply the water from the Template - only supports LBWater.WaterResizingMode.StandardAssets
                        int lbWaterCount = landscape.landscapeWaterList.Count;
                        Vector2 waterSizeAdjusted = Vector2.zero;

                        for (int waterIndex = 0; waterIndex < lbWaterCount; waterIndex++)
                        {
                            LBWater lbWater = landscape.landscapeWaterList[waterIndex];

                            if (lbWater != null)
                            {

                                // Add some water the scene
                                int numberOfMeshes = 0;

                                // Adjust Water position taking into consideration difference between original and target landscape sizes
                                Vector3 waterPosition = new Vector3(lbWater.waterPosition.x * landscape.size.x / this.size.x, 0f, lbWater.waterPosition.z * landscape.size.y / this.size.y);

                                if (ignoreStartPosition)
                                {
                                    // Consider offset and scaling
                                    waterPosition.x -= ((this.start.x - landscape.start.x) * (landscape.size.x / this.size.x));
                                    waterPosition.z -= ((this.start.z - landscape.start.z) * (landscape.size.y / this.size.y));
                                }

                                // Adjust Water size taking into consideration difference between original and target landscape sizes
                                waterSizeAdjusted = new Vector2(lbWater.waterSize.x * landscape.size.x / this.size.x, lbWater.waterSize.x * landscape.size.x / this.size.x);

                                // Populate the paramaters to pass to AddWaterToScene()
                                LBWaterParameters lbWaterParms = new LBWaterParameters();
                                lbWaterParms.landscape = landscape;
                                lbWaterParms.landscapeGameObject = landscape.gameObject;

                                lbWaterParms.waterPosition = waterPosition;
                                lbWaterParms.waterSize = waterSizeAdjusted;

                                lbWaterParms.waterIsPrimary = true;
                                lbWaterParms.waterHeight = lbWater.waterLevel;
                                lbWaterParms.waterPrefab = waterPrefab;
                                lbWaterParms.keepPrefabAspectRatio = true;
                                lbWaterParms.waterResizingMode = LBWater.WaterResizingMode.StandardAssets;
                                lbWaterParms.waterMaxMeshThreshold = 5000;
                                lbWaterParms.waterMainCamera = Camera.main;
                                lbWaterParms.waterCausticsPrefabList = null;
                                lbWaterParms.isRiver = false;
                                lbWaterParms.lbLighting = GameObject.FindObjectOfType<LBLighting>();
                                // NOTE: Currently doesn't enable AQUAS Under Water FX
                                lbWater.isUnderWaterFXEnabled = false;

                                LBWater addedWater = LBWaterOperations.AddWaterToScene(lbWaterParms, ref numberOfMeshes);
                                if (addedWater == null) { if (showErrors) { Debug.LogWarning("LBTemplate.ApplyTemplateToLandscape - could not add water to the scene."); } }
                            }
                        }

                        landscape.useVegetationSystem = false;

                        // Currently enabling Vegetation Studio system from a template seems to have issues.
                        // Needs more testing...

                        //#if VEGETATION_STUDIO && UNITY_EDITOR                       
                        //if (!LBIntegration.VegetationStudioEnable(landscape, landscape.useVegetationSystem, true))
                        //{
                        //    Debug.LogWarning("LBTemplate - Could not modifiy landscape with Vegetation Studio attributes. Please Report");
                        //}
                        //#else
                        //landscape.useVegetationSystem = false;
                        //#endif

                        isSuccess = true;
                    }
                }
            }

            return isSuccess;
        }

        /// <summary>
        ///  Copy all the LBLighting public settings to the template
        ///  If there is a weather windzone, add a single LBWindZone instance
        ///  to the lbTemplate.lbWindZoneList
        /// </summary>
        /// <param name="lbLighting"></param>
        public void AddLBLightingSettings(LBLighting lbLighting)
        {
            if (lbLighting == null) { return; }

            sun = lbLighting.sun;
            sunIntensity = lbLighting.sunIntensity;
            yAxisRotation = lbLighting.yAxisRotation;
            lightingMode = lbLighting.lightingMode;
            setupMode = lbLighting.setupMode;
            envAmbientSource = lbLighting.envAmbientSource;
            skyboxType = lbLighting.skyboxType;
            skyboxesList.AddRange(lbLighting.skyboxesList);
            proceduralSkybox = lbLighting.proceduralSkybox;
            blendedSkybox = lbLighting.blendedSkybox;

            // Dynamic lighting variables
            dayLength = lbLighting.dayLength;
            realtimeTerrainLighting = lbLighting.realtimeTerrainLighting;
            reflProbesUpdateMode = lbLighting.reflProbesUpdateMode;
            lightingUpdateInterval = lbLighting.lightingUpdateInterval;

            startTimeOfDay = lbLighting.startTimeOfDay;
            sunriseTime = lbLighting.sunriseTime;
            sunsetTime = lbLighting.sunsetTime;

            // Ambient Light and Night Setttings
            dayAmbientLight = lbLighting.dayAmbientLight;
            nightAmbientLight = lbLighting.nightAmbientLight;
            dayAmbientLightHDR = lbLighting.dayAmbientLightHDR;
            nightAmbientLightHDR = lbLighting.nightAmbientLightHDR;

            dayAmbientGndLight = lbLighting.dayAmbientGndLight;
            nightAmbientGndLight = lbLighting.nightAmbientGndLight;
            dayAmbientGndLightHDR = lbLighting.dayAmbientGndLightHDR;
            nightAmbientGndLightHDR = lbLighting.nightAmbientGndLightHDR;

            dayAmbientHznLight = lbLighting.dayAmbientHznLight;
            nightAmbientHznLight = lbLighting.nightAmbientHznLight;
            dayAmbientHznLightHDR = lbLighting.dayAmbientHznLightHDR;
            nightAmbientHznLightHDR = lbLighting.nightAmbientHznLightHDR;

            dayFogColour = lbLighting.dayFogColour;
            nightFogColour = lbLighting.nightFogColour;
            dayFogDensity = lbLighting.dayFogDensity;
            nightFogDensity = lbLighting.nightFogDensity;

            // Advanced setup variables
            sunIntensityCurve = lbLighting.sunIntensityCurve;
            // Ensure gradients aren't null before copying to template
            lbLighting.InitialiseGradients();
            ambientLightGradient = lbLighting.ambientLightGradient;
            ambientHznLightGradient = lbLighting.ambientHznLightGradient;
            ambientGndLightGradient = lbLighting.ambientGndLightGradient;
            fogColourGradient = lbLighting.fogColourGradient;
            fogDensityCurve = lbLighting.fogDensityCurve;
            minFogDensity = lbLighting.minFogDensity;
            maxFogDensity = lbLighting.maxFogDensity;
            moonIntensityCurve = lbLighting.moonIntensityCurve;
            starVisibilityCurve = lbLighting.starVisibilityCurve;

            // Celestials variables
            useCelestials = lbLighting.useCelestials;
            celestials = lbLighting.celestials;
            mainCamera = lbLighting.mainCamera;
            numberOfStars = lbLighting.numberOfStars;
            starSize = lbLighting.starSize;

            useMoon = lbLighting.useMoon;
            moon = lbLighting.moon;
            moonIntensity = lbLighting.moonIntensity;
            moonYAxisRotation = lbLighting.moonYAxisRotation;
            moonSize = lbLighting.moonSize;

            // Weather variables
            useWeather = lbLighting.useWeather;
            isHDREnabled = lbLighting.isHDREnabled;
            rainParticleSystemPrefab = lbLighting.rainParticleSystemPrefab;
            hailParticleSystemPrefab = lbLighting.hailParticleSystemPrefab;
            snowParticleSystemPrefab = lbLighting.snowParticleSystemPrefab;
            useClouds = lbLighting.useClouds;
            use3DNoise = lbLighting.use3DNoise;
            cloudStyle = lbLighting.cloudStyle;
            cloudsQualityLevel = lbLighting.cloudsQualityLevel;
            cloudsDetailAmount = lbLighting.cloudsDetailAmount;
            cloudsUpperColourDay = lbLighting.cloudsUpperColourDay;
            cloudsLowerColourDay = lbLighting.cloudsLowerColourDay;
            cloudsUpperColourNight = lbLighting.cloudsUpperColourNight;
            cloudsLowerColourNight = lbLighting.cloudsLowerColourNight;
            cloudsUpperColourDayHDR = lbLighting.cloudsUpperColourDayHDR;
            cloudsLowerColourDayHDR = lbLighting.cloudsLowerColourDayHDR;
            cloudsUpperColourNightHDR = lbLighting.cloudsUpperColourNightHDR;
            cloudsLowerColourNightHDR = lbLighting.cloudsLowerColourNightHDR;
            weatherStatesList = new List<LBWeatherState>(lbLighting.weatherStatesList);
            startWeatherState = lbLighting.startWeatherState;
            minWeatherTransitionDuration = lbLighting.minWeatherTransitionDuration;
            maxWeatherTransitionDuration = lbLighting.maxWeatherTransitionDuration;
            allowAutomaticTransitions = lbLighting.allowAutomaticTransitions;
            randomiseWindDirection = lbLighting.randomiseWindDirection;
            useWindZone = lbLighting.useWindZone;
            if (lbWindZoneList == null) { lbWindZoneList = new List<LBWindZone>(); }
            if (lbWindZoneList != null && lbLighting.weatherWindZone != null)
            {
                // Create a serializable LBWindZone class instance and populate it with the values
                // from the WindZone.
                LBWindZone lbWindZone = new LBWindZone();
                if (lbWindZone != null)
                {
                    lbWindZone.mode = lbLighting.weatherWindZone.mode;
                    lbWindZone.radius = lbLighting.weatherWindZone.radius;
                    lbWindZone.windMain = lbLighting.weatherWindZone.windMain;
                    lbWindZone.windPulseMagnitude = lbLighting.weatherWindZone.windPulseMagnitude;
                    lbWindZone.windPulseFrequency = lbLighting.weatherWindZone.windPulseFrequency;
                    lbWindZone.isWeatherFXWindZone = true;
                    lbWindZoneList.Add(lbWindZone);
                }
            }

            // Screen clock variables
            useScreenClock = lbLighting.useScreenClock;
            useScreenClockSeconds = lbLighting.useScreenClockSeconds;
            lightingCanvas = lbLighting.lightingCanvas;
            screenClockPanel = lbLighting.screenClockPanel;
            screenClockText = lbLighting.screenClockText;
            screenClockTextColour = lbLighting.screenClockTextColour;

            // Screen Fader variables
            fadeInOnWake = lbLighting.fadeInOnWake;
            fadeOutOnWake = lbLighting.fadeOutOnWake;
            fadeInDuration = lbLighting.fadeInDuration;
            fadeOutDuration = lbLighting.fadeOutDuration;

            // Show/Hide variables
            showSkyboxSettings = lbLighting.showSkyboxSettings;
            showGeneralLightingSettings = lbLighting.showGeneralLightingSettings;
            showAmbientAndFogSettings = lbLighting.showAmbientAndFogSettings;
            showCelestialsSettings = lbLighting.showCelestialsSettings;
            showWeatherSettings = lbLighting.showWeatherSettings;
            showWeatherCameraSettings = lbLighting.showWeatherCameraSettings;
            showWeatherCloudSettings = lbLighting.showWeatherCloudSettings;
            showWeatherStateSettings = lbLighting.showWeatherStateSettings;
            showClockSettings = lbLighting.showClockSettings;
            showScreenFadeSettings = lbLighting.showScreenFadeSettings;
        }

        /// <summary>
        /// Apply all the template lighting settings to a LBLighting script instance
        /// </summary>
        /// <param name="lbLighting"></param>
        public void ApplyLBLightingSettings(LBLandscape lbLandscape, ref LBLighting lbLighting, Camera camera, bool resetTimeToStartOfDay)
        {
            if (lbLighting == null) { return; }

            // Find the lights in LBLighting
            GameObject lightingObject = lbLighting.gameObject;
            Light[] lights = lightingObject.GetComponentsInChildren<Light>(true);
            if (lights != null)
            {
                foreach (Light light in lights)
                {
                    if (light.name == "Sun Light") { lbLighting.sun = light; }
                    else if (light.name == "Moon Light") { lbLighting.moon = light; }
                }
            }

            lbLighting.sunIntensity = sunIntensity;
            lbLighting.yAxisRotation = yAxisRotation;
            lbLighting.lightingMode = lightingMode;
            lbLighting.setupMode = setupMode;
            lbLighting.envAmbientSource = envAmbientSource;
            lbLighting.skyboxType = skyboxType;
            if (lbLighting.skyboxesList == null) { lbLighting.skyboxesList = new List<LBSkybox>(); }
            lbLighting.skyboxesList.AddRange(skyboxesList);
            lbLighting.proceduralSkybox = proceduralSkybox;
            lbLighting.blendedSkybox = blendedSkybox;

            // Dynamic lighting variables
            lbLighting.dayLength = dayLength;
            lbLighting.realtimeTerrainLighting = realtimeTerrainLighting;
            lbLighting.reflProbesUpdateMode = reflProbesUpdateMode;
            lbLighting.lightingUpdateInterval = lightingUpdateInterval;

            lbLighting.startTimeOfDay = startTimeOfDay;
            lbLighting.sunriseTime = sunriseTime;
            lbLighting.sunsetTime = sunsetTime;

            // Ambient Light and Night Setttings
            lbLighting.dayAmbientLight = dayAmbientLight;
            lbLighting.nightAmbientLight = nightAmbientLight;
            lbLighting.dayAmbientLightHDR = dayAmbientLightHDR;
            lbLighting.nightAmbientLightHDR = nightAmbientLightHDR;

            lbLighting.dayAmbientGndLight = dayAmbientGndLight;
            lbLighting.nightAmbientGndLight = nightAmbientGndLight;
            lbLighting.dayAmbientGndLightHDR = dayAmbientGndLightHDR;
            lbLighting.nightAmbientGndLightHDR = nightAmbientGndLightHDR;

            lbLighting.dayAmbientHznLight = dayAmbientHznLight;
            lbLighting.nightAmbientHznLight = nightAmbientHznLight;
            lbLighting.dayAmbientHznLightHDR = dayAmbientHznLightHDR;
            lbLighting.nightAmbientHznLightHDR = nightAmbientHznLightHDR;

            lbLighting.dayFogColour = dayFogColour;
            lbLighting.nightFogColour = nightFogColour;
            lbLighting.dayFogDensity = dayFogDensity;
            lbLighting.nightFogDensity = nightFogDensity;

            // Advanced setup variables
            lbLighting.sunIntensityCurve = sunIntensityCurve;
            lbLighting.ambientLightGradient = ambientLightGradient;
            lbLighting.ambientHznLightGradient = ambientHznLightGradient;
            lbLighting.ambientGndLightGradient = ambientGndLightGradient;
            lbLighting.fogColourGradient = fogColourGradient;
            lbLighting.fogDensityCurve = fogDensityCurve;
            lbLighting.minFogDensity = minFogDensity;
            lbLighting.maxFogDensity = maxFogDensity;
            lbLighting.moonIntensityCurve = moonIntensityCurve;
            lbLighting.starVisibilityCurve = starVisibilityCurve;

            // Celestials variables
            lbLighting.useCelestials = useCelestials;
            lbLighting.celestials = celestials;
            lbLighting.mainCamera = camera;
            lbLighting.numberOfStars = numberOfStars;
            lbLighting.starSize = starSize;

            lbLighting.useMoon = useMoon;
            lbLighting.moonIntensity = moonIntensity;
            lbLighting.moonYAxisRotation = moonYAxisRotation;
            lbLighting.moonSize = moonSize;

            // Weather variables
            lbLighting.useWeather = useWeather;
            lbLighting.isHDREnabled = isHDREnabled;
            lbLighting.rainParticleSystemPrefab = rainParticleSystemPrefab;
            lbLighting.hailParticleSystemPrefab = hailParticleSystemPrefab;
            lbLighting.snowParticleSystemPrefab = snowParticleSystemPrefab;
            lbLighting.useClouds = useClouds;
            lbLighting.use3DNoise = use3DNoise;
            lbLighting.cloudStyle = cloudStyle;
            lbLighting.cloudsQualityLevel = cloudsQualityLevel;
            lbLighting.cloudsDetailAmount = cloudsDetailAmount;
            lbLighting.cloudsUpperColourDay = cloudsUpperColourDay;
            lbLighting.cloudsLowerColourDay = cloudsLowerColourDay;
            lbLighting.cloudsUpperColourNight = cloudsUpperColourNight;
            lbLighting.cloudsLowerColourNight = cloudsLowerColourNight;
            lbLighting.cloudsUpperColourDayHDR = cloudsUpperColourDayHDR;
            lbLighting.cloudsLowerColourDayHDR = cloudsLowerColourDayHDR;
            lbLighting.cloudsUpperColourNightHDR = cloudsUpperColourNightHDR;
            lbLighting.cloudsLowerColourNightHDR = cloudsLowerColourNightHDR;
            lbLighting.weatherStatesList = new List<LBWeatherState>(weatherStatesList);
            lbLighting.startWeatherState = startWeatherState;
            lbLighting.minWeatherTransitionDuration = minWeatherTransitionDuration;
            lbLighting.maxWeatherTransitionDuration = maxWeatherTransitionDuration;
            lbLighting.randomiseWindDirection = randomiseWindDirection;
            lbLighting.useWindZone = useWindZone;
            //lbLighting.weatherWindZone = weatherWindZone;
            lbLighting.RefreshWeatherCameraList(true, true);
            if (lbLighting.useWindZone)
            {
                if (lbWindZoneList != null)
                {
                    // Find the first 
                    foreach (LBWindZone lbWindZone in lbWindZoneList)
                    {
                        if (lbWindZone != null)
                        {
                            if (lbWindZone.isWeatherFXWindZone)
                            {
                                lbLighting.weatherWindZone = AddWindZone(lbLandscape, lbWindZone);
                                break;
                            }
                        }
                    }
                }
            }

            // Screen clock variables
            lbLighting.useScreenClock = useScreenClock;
            lbLighting.useScreenClockSeconds = useScreenClockSeconds;
            lbLighting.lightingCanvas = lightingCanvas;
            lbLighting.screenClockPanel = screenClockPanel;
            lbLighting.screenClockText = screenClockText;
            lbLighting.screenClockTextColour = screenClockTextColour;

            // Screen Fader variables
            lbLighting.fadeInOnWake = fadeInOnWake;
            lbLighting.fadeOutOnWake = fadeOutOnWake;
            lbLighting.fadeInDuration = fadeInDuration;
            lbLighting.fadeOutDuration = fadeOutDuration;

            // Show/Hide variables
            lbLighting.showSkyboxSettings = showSkyboxSettings;
            lbLighting.showGeneralLightingSettings = showGeneralLightingSettings;
            lbLighting.showAmbientAndFogSettings = showAmbientAndFogSettings;
            lbLighting.showCelestialsSettings = showCelestialsSettings;
            lbLighting.showWeatherSettings = showWeatherSettings;
            lbLighting.showWeatherCameraSettings = showWeatherCameraSettings;
            lbLighting.showWeatherCloudSettings = showWeatherCloudSettings;
            lbLighting.showWeatherStateSettings = showWeatherStateSettings;
            lbLighting.showClockSettings = showClockSettings;
            lbLighting.showScreenFadeSettings = showScreenFadeSettings;

            if (resetTimeToStartOfDay) { lbLighting.ResetToStartOfDay(); }

            lbLighting.Initialise();
        }

        /// <summary>
        /// Add a WindZone under a landscape
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="lbWindZone"></param>
        public WindZone AddWindZone(LBLandscape landscape, LBWindZone lbWindZone)
        {
            WindZone windZone = null;

            if (landscape == null) { Debug.LogWarning("LBTemplate.AddWindZone - the landscape cannot be null"); }
            else if (lbWindZone == null) { Debug.LogWarning("LBTemplate.AddWindZone - lbWindZone cannot be null"); }
            else
            {
                GameObject windZoneGameObject = new GameObject("Landscape Wind Zone");
                if (landscape.gameObject != null)
                {
                    windZoneGameObject.transform.parent = landscape.gameObject.transform;
                    windZoneGameObject.name = landscape.gameObject.name + " Wind Zone";
                }
                windZone = windZoneGameObject.AddComponent<WindZone>();
                if (windZone != null)
                {
                    windZone.mode = lbWindZone.mode;
                    windZone.radius = lbWindZone.radius;
                    windZone.windMain = lbWindZone.windMain;
                    windZone.windPulseMagnitude = lbWindZone.windPulseMagnitude;
                    windZone.windPulseFrequency = lbWindZone.windPulseFrequency;
                    windZone.windTurbulence = lbWindZone.windTurbulence;
                }
            }
            return windZone;
        }

        /// <summary>
        /// Remove all Wind Zones from a landscape
        /// </summary>
        /// <param name="landscape"></param>
        public void RemoveWindZones(LBLandscape landscape)
        {
            if (landscape == null) { Debug.LogWarning("LBTemplate.RemoveWindZones - the landscape cannot be null"); }
            else
            {
                WindZone[] windZones = landscape.gameObject.GetComponentsInChildren<WindZone>(true);
                if (windZones != null)
                {
                    for (int w = windZones.Length; w > 0; w--)
                    {
                        DestroyImmediate(windZones[w - 1].gameObject);
                    }
                }
            }
        }

        /// <summary>
        /// Create new paths in the scene based on the LBTemplate
        /// Adds it under the landscape gameobject.
        /// If vertices etc. data exists for a MapPath, set rebuildMesh to add mesh to scene
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="isRemoveExisting"></param>
        /// <param name="addCameraAnimators"></param>
        /// <param name="ignoreTemplateStartPosition"></param>
        /// <param name="rebuildMesh"></param>
        public void ApplyPathsToScene(LBLandscape landscape, bool isRemoveExisting, bool addCameraAnimators, bool ignoreTemplateStartPosition, bool rebuildMesh = false)
        {
            if (landscape == null) { Debug.LogError("LBTemplate.ApplyPathsToScene - landscape is not defined"); }
            else if (lbPathList == null) { Debug.LogError("LBTemplate.ApplyPathsToScene - lbPathList is not defined"); }
            else
            {
                // If required, remove existing paths
                if (isRemoveExisting)
                {
                    LBCameraPath.RemoveCameraPathsFromScene(landscape);
                    LBMapPath.RemoveMapPathsFromLandscape(landscape);
                }

                bool isFirstCameraPath = true;
                Vector3 landscapeChangeOffset = Vector3.zero;
                Vector3 landscapeChangeScale = Vector3.one;

                if (lbPathList.Count > 0)
                {
                    landscapeChangeScale = GetLandscapeChangeScale(landscape, terrainHeight);
                    landscapeChangeOffset = GetLandscapeChangeOffset(landscape, ignoreTemplateStartPosition);

                    // Restore the list of Paths (supports camera and map paths)
                    foreach (LBPath lbPath in lbPathList)
                    {
                        if (lbPath != null)
                        {
                            if (lbPath.pathType == LBPath.PathType.CameraPath)
                            {
                                LBCameraPath lbCameraPath = LBCameraPath.CreateCameraPath(landscape, landscape.gameObject);
                                if (lbCameraPath == null) { Debug.LogError("LBTemplate.ApplyPathsToScene - CreateCameraPath failed"); }
                                else if (lbCameraPath.gameObject == null) { Debug.LogError("LBTemplate.ApplyPathsToScene - CameraPath GameObject is null"); }
                                else if (lbCameraPath.lbPath == null) { Debug.LogError("LBTemplate.ApplyPathsToScene - CreateCameraPath did not create the lbPath"); }
                                else if (lbCameraPath.lbPath.positionList == null) { Debug.LogError("LBTemplate.ApplyPathsToScene - lbPath.positionList is not defined"); }
                                else if (lbCameraPath.lbPath.rotationList == null) { Debug.LogError("LBTemplate.ApplyPathsToScene - lbPath.rotationList is not defined"); }
                                {
                                    // Configure the CameraPath
                                    lbCameraPath.lbPath.pathName = lbPath.pathName;
                                    lbCameraPath.lbPath.positionList.AddRange(lbPath.positionList);
                                    lbCameraPath.lbPath.rotationList.AddRange(lbPath.rotationList);
                                    lbCameraPath.lbPath.pathResolution = lbPath.pathResolution;
                                    lbCameraPath.lbPath.closedCircuit = lbPath.closedCircuit;

                                    if (addCameraAnimators)
                                    {
                                        // Add a camera animator for each CameraPath - don't enable Start On Wake (there may be more than one path)
                                        LBCameraAnimator lbCameraAnimator = LBCameraAnimator.CreateCameraAnimator(landscape, "Camera Animation", false);
                                        if (lbCameraAnimator != null)
                                        {
                                            lbCameraAnimator.cameraPath = lbCameraPath;
                                        }
                                    }
                                    else if (isFirstCameraPath)
                                    {
                                        // Attempt to hook up the first active Camera Animator with the first CameraPath
                                        LBCameraAnimator lbCameraAnimator = landscape.GetComponentInChildren<LBCameraAnimator>();
                                        if (lbCameraAnimator != null)
                                        {
                                            lbCameraAnimator.cameraPath = lbCameraPath;
                                        }
                                    }
                                    lbCameraPath.lbPath.MovePoints(landscapeChangeOffset, landscapeChangeScale);
                                }
                                isFirstCameraPath = false;
                            }
                            else if (lbPath.pathType == LBPath.PathType.MapPath)
                            {
                                LBMapPath lbMapPath = LBMapPath.CreateMapPath(landscape, landscape.gameObject);
                                if (lbMapPath == null) { Debug.LogError("LBTemplate.ApplyPathsToScene - CreateMapPath failed"); }
                                else if (lbMapPath.gameObject == null) { Debug.LogError("LBTemplate.ApplyPathsToScene - MapPath GameObject is null"); }
                                else if (lbMapPath.lbPath == null) { Debug.LogError("LBTemplate.ApplyPathsToScene - CreateMapPath did not create the lbPath"); }
                                else if (lbMapPath.lbPath.positionList == null) { Debug.LogError("LBTemplate.ApplyPathsToScene - lbPath.positionList is not defined"); }
                                else if (lbMapPath.lbPath.rotationList == null) { Debug.LogError("LBTemplate.ApplyPathsToScene - lbPath.rotationList is not defined"); }
                                else if (lbMapPath.lbPath.widthList == null) { Debug.LogError("LBTemplate.ApplyPathsToScene - lbPath.widthList is not defined"); }
                                {
                                    // Configure the map path
                                    lbMapPath.lbPath = new LBPath(lbPath);
                                    lbMapPath.gameObject.name = lbMapPath.lbPath.pathName;

                                    float originalSplineLength = lbMapPath.lbPath.splineLength;

                                    lbMapPath.lbPath.MovePoints(landscapeChangeOffset, landscapeChangeScale);
                                    lbMapPath.minPathWidth = lbMapPath.lbPath.GetMinWidth();

                                    if (lbPath.meshTempMaterial != null)
                                    {
                                        //Debug.Log("INFO: ApplyPathsToScene - transferring mesh material:" + lbPath.meshTempMaterial.name);
                                        lbMapPath.meshMaterial = new Material(lbPath.meshTempMaterial);
                                        lbMapPath.lbPath.meshTempMaterial = null;
                                        //if (lbMapPath.meshMaterial != null) { Debug.Log("INFO: ApplyPathsToScene - MapPath meshMat1:" + lbMapPath.meshMaterial.name); }
                                    }

                                    if (rebuildMesh && lbMapPath.lbPath.lbMesh != null)
                                    {
                                        // Validate mesh data, create the mesh, then add it as a gameobject in the scene
                                        if (lbMapPath.lbPath.lbMesh.IsMeshDataValid())
                                        {
                                            lbMapPath.lbPath.lbMesh.MoveVerts(landscapeChangeOffset, landscapeChangeScale);

                                            if (lbMapPath.lbPath.RebuildMesh())
                                            {
                                                Vector3 meshPosition = new Vector3(0f, lbMapPath.lbPath.meshYOffset, 0f);

                                                Transform meshTransform = LBMeshOperations.AddMeshToScene(lbMapPath.lbPath.lbMesh, meshPosition, lbMapPath.lbPath.pathName + " Mesh", lbMapPath.transform, lbMapPath.meshMaterial, true, true);

                                                if (meshTransform == null)
                                                {
                                                    Debug.LogWarning("ERROR: LBTemplate.ApplyPathsToScene could not add mesh to " + lbPath.pathName);
                                                }
                                            }
                                            else { Debug.LogWarning("ERROR: LBTemplate.ApplyPathsToScene could not rebuild mesh on " + lbPath.pathName); }
                                        }
                                    }

                                    // Check if this path is used in a Topography Layer Type of MapPath
                                    foreach (LBLayer lbLayer in landscape.topographyLayersList)
                                    {
                                        if (lbLayer.type == LBLayer.LayerType.MapPath)
                                        {
                                            // Can't use lbPath.Equals(...) because LBPath contents may be different
                                            if (lbMapPath.lbPath.pathName == lbLayer.lbPath.pathName &&
                                                lbMapPath.lbPath.positionList.Count == lbLayer.lbPath.positionList.Count &&
                                                originalSplineLength == lbLayer.lbPath.splineLength)
                                            {
                                                // Restore the link between the layer and the LBMapPath
                                                lbLayer.lbMapPath = lbMapPath;
                                                // Update the Layer to use the path in the scene
                                                lbLayer.lbPath = lbMapPath.lbPath;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Apply Stencils from the template to the scene as children of the landscape
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="isRemoveExisting"></param>
        /// <param name="showErrors"></param>
        public void ApplyStencilsToScene(LBLandscape landscape, bool isRemoveExisting, bool showErrors)
        {
            string methodName = "LBTemplate.ApplyStencilsToScene";

            if (landscape == null) { Debug.LogWarning("ERROR: " + methodName + " - landscape is not defined"); }
            else if (lbTemplateStencilList == null) { Debug.LogWarning("ERROR: " + methodName + " - lbStencilList is not defined"); }
            else
            {
                // Remove any existing LBStencil child objects of the landscape gameobject
                if (isRemoveExisting) { LBStencil.RemoveStencilsFromLandscape(landscape, showErrors); }

                if (lbTemplateStencilList.Count > 0)
                {
                    foreach (LBTemplateStencil lbTemplateStencil in lbTemplateStencilList)
                    {
                        if (!LBStencil.CreateStencilInScene(landscape, lbTemplateStencil, showErrors))
                        {
                            string stencilName = lbTemplateStencil != null ? lbTemplateStencil.stencilName : "(unknown)";
                            if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not apply Stencil: " + stencilName); }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Update the Topography Layer LBTerrainData list to match the destination landscape.
        /// FUTURE: To support different source/destination terrain resolutions
        /// FUTURE: To support different number of source/destination terrains
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="showErrors"></param>
        public void UpdateTopographyLayerLBTerrainData(LBLandscape landscape, bool showErrors)
        {
            string methodName = "LBTemplate.UpdateTopographyLayerLBTerrainData";
            string sourceTerrainName = string.Empty;
            string destTerrainName = string.Empty;
            string sourceTerrainDataName = string.Empty;
            string destTerrainDataName = string.Empty;

            if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscape is not defined. PLEASE REPORT"); } }
            else if (landscape.landscapeTerrains == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - terrains are not defined in " + landscape.name + ". PLEASE REPORT"); } }
            else if (landscape.topographyLayersList == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - topographyLayersList is not defined in " + landscape.name + ". PLEASE REPORT"); } }
            else
            {
                int numDestTerrains = landscape.landscapeTerrains.Length;

                for (int layerIdx = 0; layerIdx < topographyLayersList.Count; layerIdx++)
                {
                    if (topographyLayersList[layerIdx].type == LBLayer.LayerType.UnityTerrains)
                    {
                        if (topographyLayersList[layerIdx].lbTerrainDataList == null)
                        {
                            if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - lbTerrainDataList is not defined for Layer " + (layerIdx + 1).ToString() + " (LayerType: UnityTerrains) in Landscape: " + landscape.name + ". PLEASE REPORT"); }
                        }
                        else if (topographyLayersList[layerIdx].lbTerrainDataList.Count < 1)
                        {
                            if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - lbTerrainDataList is empty for Layer " + (layerIdx + 1).ToString() + " (LayerType: UnityTerrains) in Landscape: " + landscape.name + ". PLEASE REPORT"); }
                        }
                        // Currently, the number of source/destination terrains must match
                        else if (topographyLayersList[layerIdx].lbTerrainDataList.Count != numDestTerrains)
                        {
                            if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - the number of source (" + topographyLayersList[layerIdx].lbTerrainDataList.Count + ") and destination (" + numDestTerrains + ") terrains do not match for Layer " + (layerIdx + 1).ToString() + " (LayerType: UnityTerrains) in Landscape: " + landscape.name + ". PLEASE REPORT"); }
                        }
                        else
                        {
                            // Assume terrains are arranged in the same order under the parent gameobject
                            for (int t = 0; t < numDestTerrains; t++)
                            {
                                sourceTerrainName = topographyLayersList[layerIdx].lbTerrainDataList[t].sourceTerrainName;
                                destTerrainName = landscape.landscapeTerrains[t].name;

                                // If the source terrain name is different from the destination terrain name, update it.
                                if (sourceTerrainName != destTerrainName)
                                {
                                    //Debug.Log("INFO " + methodName + " updating terrain name. Source: " + sourceTerrainName + " Destination: " + destTerrainName + " for Layer " + (layerIdx + 1).ToString());
                                    landscape.topographyLayersList[layerIdx].lbTerrainDataList[t].sourceTerrainName = destTerrainName;
                                }

                                TerrainData tData = landscape.landscapeTerrains[t].terrainData;
                                if (tData == null) { Debug.LogWarning("ERROR " + methodName + " terrain data for " + destTerrainName + " is null"); }
                                {
                                    sourceTerrainDataName = topographyLayersList[layerIdx].lbTerrainDataList[t].sourceTerrainDataName;
                                    destTerrainDataName = tData.name;

                                    if (sourceTerrainDataName != destTerrainDataName)
                                    {
                                        //Debug.Log("INFO " + methodName + " updating terrain data name. Source: " + sourceTerrainDataName + " Destination: " + destTerrainDataName + " for Layer " + (layerIdx + 1).ToString());
                                        landscape.topographyLayersList[layerIdx].lbTerrainDataList[t].sourceTerrainDataName = destTerrainDataName;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Update the Texture type LBTerrainData list to match the destination landscape.
        /// FUTURE: To support different source/destination terrain resolutions
        /// FUTURE: To support different number of source/destination terrains
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="showErrors"></param>
        public void UpdateTextureLBTerrainData(LBLandscape landscape, bool showErrors)
        {
            string methodName = "LBTemplate.UpdateTextureLBTerrainData";
            string sourceTerrainName = string.Empty;
            string destTerrainName = string.Empty;
            string sourceTerrainDataName = string.Empty;
            string destTerrainDataName = string.Empty;

            if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscape is not defined. PLEASE REPORT"); } }
            else if (landscape.landscapeTerrains == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - terrains are not defined in " + landscape.name + ". PLEASE REPORT"); } }
            else if (landscape.terrainTexturesList == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - terrainTexturesList is not defined in " + landscape.name + ". PLEASE REPORT"); } }
            else
            {
                int numDestTerrains = landscape.landscapeTerrains.Length;

                for (int textureTypeIdx = 0; textureTypeIdx < terrainTexturesList.Count; textureTypeIdx++)
                {
                    if (terrainTexturesList[textureTypeIdx].texturingMode == LBTerrainTexture.TexturingMode.Imported)
                    {
                        if (terrainTexturesList[textureTypeIdx].lbTerrainDataList == null)
                        {
                            if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - lbTerrainDataList is not defined for Texture Type " + (textureTypeIdx + 1).ToString() + " (TexturingMode.Imported) in Landscape: " + landscape.name + ". PLEASE REPORT"); }
                        }
                        else if (terrainTexturesList[textureTypeIdx].lbTerrainDataList.Count < 1)
                        {
                            if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - lbTerrainDataList is empty for Texture Type " + (textureTypeIdx + 1).ToString() + " (TexturingMode.Imported) in Landscape: " + landscape.name + ". PLEASE REPORT"); }
                        }
                        // Currently, the number of source/destination terrains must match
                        else if (terrainTexturesList[textureTypeIdx].lbTerrainDataList.Count != numDestTerrains)
                        {
                            if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - the number of source (" + terrainTexturesList[textureTypeIdx].lbTerrainDataList.Count + ") and destination (" + numDestTerrains + ") terrains do not match for Texture Type " + (textureTypeIdx + 1).ToString() + " (TexturingMode.Imported) in Landscape: " + landscape.name + ". PLEASE REPORT"); }
                        }
                        else
                        {
                            // Assume terrains are arranged in the same order under the parent gameobject
                            for (int t = 0; t < numDestTerrains; t++)
                            {
                                sourceTerrainName = terrainTexturesList[textureTypeIdx].lbTerrainDataList[t].sourceTerrainName;
                                destTerrainName = landscape.landscapeTerrains[t].name;

                                // If the source terrain name is different from the destination terrain name, update it.
                                if (sourceTerrainName != destTerrainName)
                                {
                                    //Debug.Log("INFO " + methodName + " updating terrain name. Source: " + sourceTerrainName + " Destination: " + destTerrainName + " for Texture Type " + (textureTypeIdx + 1).ToString());
                                    landscape.terrainTexturesList[textureTypeIdx].lbTerrainDataList[t].sourceTerrainName = destTerrainName;
                                }

                                TerrainData tData = landscape.landscapeTerrains[t].terrainData;
                                if (tData == null) { Debug.LogWarning("ERROR " + methodName + " terrain data for " + destTerrainName + " is null"); }
                                {
                                    sourceTerrainDataName = terrainTexturesList[textureTypeIdx].lbTerrainDataList[t].sourceTerrainDataName;
                                    destTerrainDataName = tData.name;

                                    if (sourceTerrainDataName != destTerrainDataName)
                                    {
                                        //Debug.Log("INFO " + methodName + " updating terrain data name. Source: " + sourceTerrainDataName + " Destination: " + destTerrainDataName + " for Texture Type " + (textureTypeIdx + 1).ToString());
                                        landscape.terrainTexturesList[textureTypeIdx].lbTerrainDataList[t].sourceTerrainDataName = destTerrainDataName;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Update the Tree type LBTerrainData list to match the destination landscape.
        /// FUTURE: To support different source/destination terrain resolutions
        /// FUTURE: To support different number of source/destination terrains
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="showErrors"></param>
        public void UpdateTreesLBTerrainData(LBLandscape landscape, bool showErrors)
        {
            string methodName = "LBTemplate.UpdateTreesLBTerrainData";
            string sourceTerrainName = string.Empty;
            string destTerrainName = string.Empty;
            string sourceTerrainDataName = string.Empty;
            string destTerrainDataName = string.Empty;

            if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscape is not defined. PLEASE REPORT"); } }
            else if (landscape.landscapeTerrains == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - terrains are not defined in " + landscape.name + ". PLEASE REPORT"); } }
            else if (landscape.terrainTreesList == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - terrainTreesList is not defined in " + landscape.name + ". PLEASE REPORT"); } }
            else
            {
                int numDestTerrains = landscape.landscapeTerrains.Length;

                for (int treeTypeIdx = 0; treeTypeIdx < terrainTreesList.Count; treeTypeIdx++)
                {
                    if (terrainTreesList[treeTypeIdx].treePlacingMode == LBTerrainTree.TreePlacingMode.Imported)
                    {
                        if (terrainTreesList[treeTypeIdx].lbTerrainDataList == null)
                        {
                            if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - lbTerrainDataList is not defined for Tree Type " + (treeTypeIdx + 1).ToString() + " (TreePlacingMode.Imported) in Landscape: " + landscape.name + ". PLEASE REPORT"); }
                        }
                        else if (terrainTreesList[treeTypeIdx].lbTerrainDataList.Count < 1)
                        {
                            if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - lbTerrainDataList is empty for Tree Type " + (treeTypeIdx + 1).ToString() + " (TreePlacingMode.Imported) in Landscape: " + landscape.name + ". PLEASE REPORT"); }
                        }
                        // Currently, the number of source/destination terrains must match
                        else if (terrainTreesList[treeTypeIdx].lbTerrainDataList.Count != numDestTerrains)
                        {
                            if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - the number of source (" + terrainTreesList[treeTypeIdx].lbTerrainDataList.Count + ") and destination (" + numDestTerrains + ") terrains do not match for Tree Type " + (treeTypeIdx + 1).ToString() + " (TreePlacingMode.Imported) in Landscape: " + landscape.name + ". PLEASE REPORT"); }
                        }
                        else
                        {
                            // Assume terrains are arranged in the same order under the parent gameobject
                            for (int t = 0; t < numDestTerrains; t++)
                            {
                                sourceTerrainName = terrainTreesList[treeTypeIdx].lbTerrainDataList[t].sourceTerrainName;
                                destTerrainName = landscape.landscapeTerrains[t].name;

                                // If the source terrain name is different from the destination terrain name, update it.
                                if (sourceTerrainName != destTerrainName)
                                {
                                    //Debug.Log("INFO " + methodName + " updating terrain name. Source: " + sourceTerrainName + " Destination: " + destTerrainName + " for Tree Type " + (treeTypeIdx + 1).ToString());
                                    landscape.terrainTreesList[treeTypeIdx].lbTerrainDataList[t].sourceTerrainName = destTerrainName;
                                }

                                TerrainData tData = landscape.landscapeTerrains[t].terrainData;
                                if (tData == null) { Debug.LogWarning("ERROR " + methodName + " terrain data for " + destTerrainName + " is null"); }
                                {
                                    sourceTerrainDataName = terrainTreesList[treeTypeIdx].lbTerrainDataList[t].sourceTerrainDataName;
                                    destTerrainDataName = tData.name;

                                    if (sourceTerrainDataName != destTerrainDataName)
                                    {
                                        //Debug.Log("INFO " + methodName + " updating terrain data name. Source: " + sourceTerrainDataName + " Destination: " + destTerrainDataName + " for Tree Type " + (treeTypeIdx + 1).ToString());
                                        landscape.terrainTreesList[treeTypeIdx].lbTerrainDataList[t].sourceTerrainDataName = destTerrainDataName;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Update the Grass type LBTerrainData list to match the destination landscape.
        /// FUTURE: To support different source/destination terrain resolutions
        /// FUTURE: To support different number of source/destination terrains
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="showErrors"></param>
        public void UpdateGrassLBTerrainData(LBLandscape landscape, bool showErrors)
        {
            string methodName = "LBTemplate.UpdateGrassLBTerrainData";
            string sourceTerrainName = string.Empty;
            string destTerrainName = string.Empty;
            string sourceTerrainDataName = string.Empty;
            string destTerrainDataName = string.Empty;

            if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscape is not defined. PLEASE REPORT"); } }
            else if (landscape.landscapeTerrains == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - terrains are not defined in " + landscape.name + ". PLEASE REPORT"); } }
            else if (landscape.terrainGrassList == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - terrainGrassList is not defined in " + landscape.name + ". PLEASE REPORT"); } }
            else
            {
                int numDestTerrains = landscape.landscapeTerrains.Length;

                for (int grassTypeIdx = 0; grassTypeIdx < terrainGrassList.Count; grassTypeIdx++)
                {
                    if (terrainGrassList[grassTypeIdx].grassPlacingMode == LBTerrainGrass.GrassPlacingMode.Imported)
                    {
                        if (terrainGrassList[grassTypeIdx].lbTerrainDataList == null)
                        {
                            if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - lbTerrainDataList is not defined for Grass Type " + (grassTypeIdx + 1).ToString() + " (GrassPlacingMode.Imported) in Landscape: " + landscape.name + ". PLEASE REPORT"); }
                        }
                        else if (terrainGrassList[grassTypeIdx].lbTerrainDataList.Count < 1)
                        {
                            if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - lbTerrainDataList is empty for Grass Type " + (grassTypeIdx + 1).ToString() + " (GrassPlacingMode.Imported) in Landscape: " + landscape.name + ". PLEASE REPORT"); }
                        }
                        // Currently, the number of source/destination terrains must match
                        else if (terrainGrassList[grassTypeIdx].lbTerrainDataList.Count != numDestTerrains)
                        {
                            if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - the number of source (" + terrainGrassList[grassTypeIdx].lbTerrainDataList.Count + ") and destination (" + numDestTerrains + ") terrains do not match for Grass Type " + (grassTypeIdx + 1).ToString() + " (GrassPlacingMode.Imported) in Landscape: " + landscape.name + ". PLEASE REPORT"); }
                        }
                        else
                        {
                            // Assume terrains are arranged in the same order under the parent gameobject
                            for (int t = 0; t < numDestTerrains; t++)
                            {
                                sourceTerrainName = terrainGrassList[grassTypeIdx].lbTerrainDataList[t].sourceTerrainName;
                                destTerrainName = landscape.landscapeTerrains[t].name;

                                // If the source terrain name is different from the destination terrain name, update it.
                                if (sourceTerrainName != destTerrainName)
                                {
                                    //Debug.Log("INFO " + methodName + " updating terrain name. Source: " + sourceTerrainName + " Destination: " + destTerrainName + " for Grass Type " + (grassTypeIdx + 1).ToString());
                                    landscape.terrainGrassList[grassTypeIdx].lbTerrainDataList[t].sourceTerrainName = destTerrainName;
                                }

                                TerrainData tData = landscape.landscapeTerrains[t].terrainData;
                                if (tData == null) { Debug.LogWarning("ERROR " + methodName + " terrain data for " + destTerrainName + " is null"); }
                                {
                                    sourceTerrainDataName = terrainGrassList[grassTypeIdx].lbTerrainDataList[t].sourceTerrainDataName;
                                    destTerrainDataName = tData.name;

                                    if (sourceTerrainDataName != destTerrainDataName)
                                    {
                                        //Debug.Log("INFO " + methodName + " updating terrain data name. Source: " + sourceTerrainDataName + " Destination: " + destTerrainDataName + " for Grass Type " + (grassTypeIdx + 1).ToString());
                                        landscape.terrainGrassList[grassTypeIdx].lbTerrainDataList[t].sourceTerrainDataName = destTerrainDataName;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Calculate offset compared to the template and source landscape
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="ignoreStartPosition"></param>
        /// <returns></returns>
        public Vector3 GetLandscapeChangeOffset(LBLandscape landscape, bool ignoreStartPosition)
        {
            Vector3 changeOffset = Vector3.zero;

            if (landscape != null)
            {
                // If new landscape will be at x,y,z we need to offset by diff of the original landscape position from the Template and the current landscape position
                // Get the offset difference between the new (landscape) and the template (this.start)
                Vector3 offsetDiff = landscape.start - this.start;

                if (ignoreStartPosition) { changeOffset = offsetDiff; }
                // If we don't ignore the original source landscape start position, then things should have the same offset (without scaling) as the original.
            }

            return changeOffset;
        }

        /// <summary>
        /// Get the amount items or locations need to be scaled up or down based on the different
        /// sizes of the destination landscape and the source landscape (from the template).
        /// NOTE: Assumes the landscape terrain data has already been updated
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="newHeight"></param>
        /// <returns></returns>
        public Vector3 GetLandscapeChangeScale(LBLandscape landscape, float newHeight)
        {
            Vector3 changeScale = Vector3.one;

            if (landscape != null)
            {
                // Calculate the amount to scale things in the x,y and z axis
                changeScale.x = landscape.size.x / this.size.x;
                changeScale.z = landscape.size.y / this.size.y;
                changeScale.y = landscape.GetLandscapeTerrainHeight() / (newHeight == 0 ? 2000f : newHeight);  // Avoid div by 0
            }
            return changeScale;
        }

        #endregion
    }
}