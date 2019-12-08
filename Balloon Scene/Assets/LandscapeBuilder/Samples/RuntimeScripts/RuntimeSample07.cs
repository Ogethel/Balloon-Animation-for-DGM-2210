using System.Collections.Generic;
using UnityEngine;
using LandscapeBuilder;

/// <summary>
/// Example script that creates a landscape entirely at runtime.
/// GPU is used where possible.
/// Stencil, Topography, Texturing, and Groups code was creating using
/// the (S) scripting buttons from an existing landscape.
/// Includes an Group Object Path example.
/// NOTE: Ensure Project Settings, Color Space = Linear.
/// There is no error checking for different sized terrains.
/// MANAUL STEPS REQUIRED
/// 1. In the Application.dataPath folder, create a Heightmaps folder. In the Editor the Heightmaps
///    folder should be created in the Project pane. For Standalone PC Builds, it should be added to
///    the [project]_Data folder.
/// 2. Create a duplicate of LandscapeBuilder/Modifiers/Valleys/LauterbrunnenValleyCH.raw
/// 3. Copy the .raw file into the new Heightmaps folder.
/// 4. Ensure the file in Heightmaps folder is the same name as the original.
/// </summary>
[RequireComponent(typeof(LBLandscape))]
public class RuntimeSample07 : MonoBehaviour
{
    #region Public Variables
    public Vector2 landscapeSize = Vector2.one * 2000f;
    public float terrainWidth = 1000f;
    public float terrainHeight = 2000f;

    public bool showErrors = false;
    public bool useGPUTopography = true;
    public bool useGPUTexturing = true;
    public bool useGPUGrass = true;
    public bool useGPUPath = true;

    // Stencil Layer input images
    public Texture2D lbStRTStencil7StencilLayerPathTex = null;

    [Header("Texture1")]
    public Texture2D textureTex1;
    public Texture2D normalMapTex1;
    public Texture2D heightMapTex1;
    public Texture2D mapTex1;

    [Header("Texture2")]
    public Texture2D textureTex2;
    public Texture2D normalMapTex2;
    public Texture2D heightMapTex2;
    public Texture2D mapTex2;

    [Header("Texture3")]
    public Texture2D textureTex3;
    public Texture2D normalMapTex3;
    public Texture2D heightMapTex3;
    public Texture2D mapTex3;

    [Header("Texture4")]
    public Texture2D textureTex4;
    public Texture2D normalMapTex4;
    public Texture2D heightMapTex4;
    public Texture2D mapTex4;

    [Header("Tree1")]
    public GameObject prefabTree1;
    public Texture2D mapTree1;

    [Header("Group1")]
    public Material Group001_Member001surfaceMeshMat;
    public GameObject Group001_Member002prefab;
    public GameObject Group001_Member003prefab;


    [Header("CameraPath")]
    // This is the GroupMember GUID for the Object Path and can be located within the script created by the Group. This
    // assumes that a Group with an Object Path member was first created in the editor and then scripted out with the S
    // button.
    public string objPathGUID;

    #endregion

    #region Private variables
    private LBLandscape landscape = null;
    private List<LBLayer> topographyLayers;
    #endregion

    private void Awake()
    {
        #region Initialise
        // This line just gets the starting time of the generation so that the total generation time
        // can be recorded and displayed
        float generationStartTime = Time.realtimeSinceStartup;

        RuntimeSampleHelper.RemoveDefaultCamera();
        RuntimeSampleHelper.RemoveDefaultLight();

        // We're using some old trees in this demo, which don't like anti-aliasing
        QualitySettings.antiAliasing = 0;

        // Get a link to the LBLandscape script
        landscape = this.GetComponent<LBLandscape>();

	    if (landscape == null)
	    {
		    Debug.Log("Could not add LBLandscape script to gameobject at Runtime");
		    return;
	    }

	    else if (landscape.IsGPUAccelerationAvailable())
	    {
		    landscape.useGPUGrass = useGPUGrass;
		    landscape.useGPUTexturing = useGPUTexturing;
		    landscape.useGPUTopography = useGPUTopography;
		    landscape.useGPUPath = useGPUPath;
	    }
	    else
	    {
		    #if UNITY_EDITOR
		    if (useGPUTopography || useGPUTexturing || useGPUGrass || useGPUPath)
		    {
			    Debug.Log("Sorry, your hardware does not support GPU acceleration");
		    }
		    #endif
		    landscape.useGPUTopography = false;
		    landscape.useGPUTexturing = false;
		    landscape.useGPUGrass = false;
		    landscape.useGPUPath = false;
	    }

        // Check to see if Universal Render Pipeline is installed in the project
        bool isURP = LBLandscape.IsURP(false);

        // Check to see if Light Weight Render Pipeline is installed in this project
        bool isLWRP = !isURP && LBLandscape.IsLWRP(false);

        // Check to see if High Definition Render Pipeline is installed in this project
        bool isHDRP = !isURP && !isLWRP && LBLandscape.IsHDRP(false);

        #if UNITY_2019_2_OR_NEWER
        bool is201920Plus = true;
        #else
        bool is201920Plus = false;
        #endif

        #endregion

        // Update the size
        landscape.size = landscapeSize;

        #region Create the terrains
        int terrainNumber = 0;

        for (float tx = 0f; tx < landscapeSize.x - 1f; tx += terrainWidth)
        {
            for (float ty = 0f; ty < landscapeSize.y - 1f; ty += terrainWidth)
            {
                // Create a new gameobject
                GameObject terrainObj = new GameObject("RuntimeTerrain" + (terrainNumber++).ToString("000"));
                // Create a new gameobject
                // Correctly parent and position the terrain
                terrainObj.transform.parent = this.transform;
                terrainObj.transform.localPosition = new Vector3(tx, 0f, ty);
                // Add a terrain component
                Terrain newTerrain = terrainObj.AddComponent<Terrain>();
                // Set terrain settings)
                newTerrain.heightmapPixelError = 1f;
                newTerrain.basemapDistance = 1500f;
                newTerrain.treeDistance = 10000f;
                newTerrain.treeBillboardDistance = 200f;
                newTerrain.detailObjectDistance = 200f;
                newTerrain.treeCrossFadeLength = 5f;
                #if UNITY_2018_3_OR_NEWER
                newTerrain.drawInstanced = false;  // Currently doesn't work on a new terrain at runtime
                newTerrain.groupingID = 100; // Default is 0
                newTerrain.allowAutoConnect = true;
                #endif
                // Set terrain data settings
                TerrainData newTerrainData = new TerrainData();

                newTerrainData.heightmapResolution = 513;
                newTerrainData.size = new Vector3(terrainWidth, terrainHeight, terrainWidth);
                newTerrainData.SetDetailResolution(1024, 16);
                newTerrain.terrainData = newTerrainData;

                // Set up the terrain collider
                TerrainCollider newTerrainCol = terrainObj.AddComponent<TerrainCollider>();
                newTerrainCol.terrainData = newTerrainData;

            }
        }
        #endregion

        landscape.SetLandscapeTerrains(true);

        landscape.SetTerrainNeighbours(false);

        #region Set the terrain material
        int numTerrains = landscape.landscapeTerrains == null ? 0 : landscape.landscapeTerrains.Length;

        // Check for URP/LWRP/HDRP or do we need to create a default material for U2019.2.0 or newer
        if (isURP || isLWRP || isHDRP || is201920Plus)
        {
            float pixelError = 0f;
            Terrain terrain = null;
            LBLandscape.TerrainMaterialType terrainMaterialType = isURP ? LBLandscape.TerrainMaterialType.URP : (isLWRP ? LBLandscape.TerrainMaterialType.LWRP : (terrainMaterialType = isHDRP ? LBLandscape.TerrainMaterialType.HDRP : LBLandscape.TerrainMaterialType.BuiltInStandard));

            for (int tIdx = 0; tIdx < numTerrains; tIdx++)
            {
                terrain = landscape.landscapeTerrains[tIdx];
                landscape.SetTerrainMaterial(terrain, tIdx, (tIdx == numTerrains - 1), terrain.terrainData.size.x, ref pixelError, terrainMaterialType);
            }
        }
        #endregion

        // Add any stencil code here

        #region Stencil RTStencil7
        // Add a new Stencil to the (empty) landscape
        LBStencil lbStRTStencil7 = LBStencil.CreateStencilInScene(landscape, landscape.gameObject);
        if (lbStRTStencil7 != null)
        {
            lbStRTStencil7.stencilName = "RTStencil7";
            lbStRTStencil7.name = "RTStencil7";
            lbStRTStencil7.GUID = "137beacb-3551-4948-b026-fad31d958fe2";
            /// Import PNG files into stencil layers (at runtime) and add as filters.
            lbStRTStencil7.layerImportSource = LBStencil.LayerImportSource.PNG;
            lbStRTStencil7.layerImportMethod = LBStencil.LayerImportMethod.Alpha;
            // If importing LB Map Textures into Stencil Layers, set this to true
            lbStRTStencil7.isFlipImportTopBottom = true;
            lbStRTStencil7.isCreateMapAsRGBA = false;
            lbStRTStencil7.isCreateMapPerTerrain = false;

            // Remove the default first layer
            if (lbStRTStencil7.stencilLayerList.Count > 0) { lbStRTStencil7.stencilLayerList.RemoveAt(0); }

            // Start Stencil Layers
            if (lbStRTStencil7.ImportTexture("StencilLayerPath", lbStRTStencil7StencilLayerPathTex, false))
            {
                int slIdx = lbStRTStencil7.stencilLayerList.Count - 1;
                lbStRTStencil7.stencilLayerList[slIdx].GUID = "ec3eeff6-e331-42fb-b209-d04e335ec184";
                lbStRTStencil7.stencilLayerList[slIdx].layerResolution = LBStencil.LayerResolution._1024x1024;
                lbStRTStencil7.stencilLayerList[slIdx].colourInEditor = new Color(1f, 1f, 1f, 1f);
            }
            // End Stencil Layers
        }
        #endregion

        //  Paste code generated from Topography Tab layers here 

        // Layer Code generated from Landscape Builder at 8:43 PM on Friday, February 01, 2019
        #region Topography Layer1
        AnimationCurve additiveCurveLyr1 = new AnimationCurve();

        AnimationCurve subtractiveCurveLyr1 = new AnimationCurve();

        List<LBLayerFilter> filtersLyr1 = new List<LBLayerFilter>();
        // Add LayerFilter code here
        List<AnimationCurve> imageCurveModifiersLyr1 = new List<AnimationCurve>();

        List<AnimationCurve> outputCurveModifiersLyr1 = new List<AnimationCurve>();

        List<LBCurve.CurvePreset> outputCurveModifierPresetsLyr1 = new List<LBCurve.CurvePreset>();

        List<AnimationCurve> perOctaveCurveModifiersLyr1 = new List<AnimationCurve>();
        AnimationCurve perOctaveCurveLyr11 = new AnimationCurve();
        perOctaveCurveLyr11.AddKey(0.00f, 0.50f);
        perOctaveCurveLyr11.AddKey(0.05f, 0.40f);
        perOctaveCurveLyr11.AddKey(0.20f, 0.10f);
        perOctaveCurveLyr11.AddKey(0.25f, 0.00f);
        perOctaveCurveLyr11.AddKey(0.30f, 0.20f);
        perOctaveCurveLyr11.AddKey(0.45f, 0.80f);
        perOctaveCurveLyr11.AddKey(0.50f, 1.00f);
        perOctaveCurveLyr11.AddKey(0.55f, 0.80f);
        perOctaveCurveLyr11.AddKey(0.70f, 0.20f);
        perOctaveCurveLyr11.AddKey(0.75f, 0.00f);
        perOctaveCurveLyr11.AddKey(0.80f, 0.10f);
        perOctaveCurveLyr11.AddKey(0.95f, 0.40f);
        perOctaveCurveLyr11.AddKey(1.00f, 0.50f);
        Keyframe[] perOctaveCurveLyr11Keys = perOctaveCurveLyr11.keys;
        perOctaveCurveLyr11Keys[0].inTangent = 0.00f;
        perOctaveCurveLyr11Keys[0].outTangent = 0.00f;
        perOctaveCurveLyr11Keys[1].inTangent = -2.00f;
        perOctaveCurveLyr11Keys[1].outTangent = -2.00f;
        perOctaveCurveLyr11Keys[2].inTangent = -2.00f;
        perOctaveCurveLyr11Keys[2].outTangent = -2.00f;
        perOctaveCurveLyr11Keys[3].inTangent = 0.00f;
        perOctaveCurveLyr11Keys[3].outTangent = 0.00f;
        perOctaveCurveLyr11Keys[4].inTangent = 4.00f;
        perOctaveCurveLyr11Keys[4].outTangent = 4.00f;
        perOctaveCurveLyr11Keys[5].inTangent = 4.00f;
        perOctaveCurveLyr11Keys[5].outTangent = 4.00f;
        perOctaveCurveLyr11Keys[6].inTangent = 0.00f;
        perOctaveCurveLyr11Keys[6].outTangent = 0.00f;
        perOctaveCurveLyr11Keys[7].inTangent = -4.00f;
        perOctaveCurveLyr11Keys[7].outTangent = -4.00f;
        perOctaveCurveLyr11Keys[8].inTangent = -4.00f;
        perOctaveCurveLyr11Keys[8].outTangent = -4.00f;
        perOctaveCurveLyr11Keys[9].inTangent = 0.00f;
        perOctaveCurveLyr11Keys[9].outTangent = 0.00f;
        perOctaveCurveLyr11Keys[10].inTangent = 2.00f;
        perOctaveCurveLyr11Keys[10].outTangent = 2.00f;
        perOctaveCurveLyr11Keys[11].inTangent = 2.00f;
        perOctaveCurveLyr11Keys[11].outTangent = 2.00f;
        perOctaveCurveLyr11Keys[12].inTangent = 0.00f;
        perOctaveCurveLyr11Keys[12].outTangent = 0.00f;
        perOctaveCurveLyr11 = new AnimationCurve(perOctaveCurveLyr11Keys);

        perOctaveCurveModifiersLyr1.Add(perOctaveCurveLyr11);

        List<LBCurve.CurvePreset> perOctaveCurveModifierPresetsLyr1 = new List<LBCurve.CurvePreset>();

        AnimationCurve mapPathBlendCurveLyr1 = new AnimationCurve();
        mapPathBlendCurveLyr1.AddKey(0.00f, 0.00f);
        mapPathBlendCurveLyr1.AddKey(1.00f, 1.00f);
        Keyframe[] mapPathBlendCurveLyr1Keys = mapPathBlendCurveLyr1.keys;
        mapPathBlendCurveLyr1Keys[0].inTangent = 0.00f;
        mapPathBlendCurveLyr1Keys[0].outTangent = 0.00f;
        mapPathBlendCurveLyr1Keys[1].inTangent = 0.00f;
        mapPathBlendCurveLyr1Keys[1].outTangent = 0.00f;
        mapPathBlendCurveLyr1 = new AnimationCurve(mapPathBlendCurveLyr1Keys);

        AnimationCurve mapPathHeightCurveLyr1 = new AnimationCurve();
        mapPathHeightCurveLyr1.AddKey(0.00f, 1.00f);
        mapPathHeightCurveLyr1.AddKey(1.00f, 1.00f);
        Keyframe[] mapPathHeightCurveLyr1Keys = mapPathHeightCurveLyr1.keys;
        mapPathHeightCurveLyr1Keys[0].inTangent = 0.00f;
        mapPathHeightCurveLyr1Keys[0].outTangent = 0.00f;
        mapPathHeightCurveLyr1Keys[1].inTangent = 0.00f;
        mapPathHeightCurveLyr1Keys[1].outTangent = 0.00f;
        mapPathHeightCurveLyr1 = new AnimationCurve(mapPathHeightCurveLyr1Keys);

        LBLayer lbBaseLayer1 = new LBLayer();
        if (lbBaseLayer1 != null)
        {
            lbBaseLayer1.type = LBLayer.LayerType.ImageModifier;
            lbBaseLayer1.preset = LBLayer.LayerPreset.MountainRangeBase;
            lbBaseLayer1.layerTypeMode = LBLayer.LayerTypeMode.Add;
            lbBaseLayer1.noiseTileSize = 5000;
            lbBaseLayer1.noiseOffsetX = 0;
            lbBaseLayer1.noiseOffsetZ = 0;
            lbBaseLayer1.octaves = 3;
            lbBaseLayer1.downscaling = 1;
            lbBaseLayer1.lacunarity = 1.95f;
            lbBaseLayer1.gain = 0.4f;
            lbBaseLayer1.additiveAmount = 0.5f;
            lbBaseLayer1.subtractiveAmount = 0.2f;
            lbBaseLayer1.additiveCurve = additiveCurveLyr1;
            lbBaseLayer1.subtractiveCurve = subtractiveCurveLyr1;
            lbBaseLayer1.removeBaseNoise = true;
            lbBaseLayer1.addMinHeight = false;
            lbBaseLayer1.addHeight = 0f;
            lbBaseLayer1.restrictArea = false;
            lbBaseLayer1.areaRect = new Rect(1000, 1000, 2000, 2000);
            lbBaseLayer1.interpolationSmoothing = 0;
            // lbBaseLayer1.heightmapImage = heightmapImageLyr1;
            lbBaseLayer1.imageHeightScale = 0.125f;
            lbBaseLayer1.imageCurveModifiers = imageCurveModifiersLyr1;
            lbBaseLayer1.filters = filtersLyr1;
            lbBaseLayer1.isDisabled = false;
            lbBaseLayer1.showLayer = true;
            lbBaseLayer1.showAdvancedSettings = false;
            lbBaseLayer1.showCurvesAndFilters = false;
            lbBaseLayer1.showAreaHighlighter = false;
            lbBaseLayer1.detailSmoothRate = 0f;
            lbBaseLayer1.areaBlendRate = 0.5f;
            lbBaseLayer1.downscaling = 1;
            lbBaseLayer1.warpAmount = 0f;
            lbBaseLayer1.warpOctaves = 1;
            lbBaseLayer1.outputCurveModifiers = outputCurveModifiersLyr1;
            lbBaseLayer1.outputCurveModifierPresets = outputCurveModifierPresetsLyr1;
            lbBaseLayer1.perOctaveCurveModifiers = perOctaveCurveModifiersLyr1;
            lbBaseLayer1.perOctaveCurveModifierPresets = perOctaveCurveModifierPresetsLyr1;
            lbBaseLayer1.heightScale = 0.75f;
            lbBaseLayer1.minHeight = 0f;
            lbBaseLayer1.imageSource = LBLayer.LayerImageSource.Default;
            lbBaseLayer1.imageRepairHoles = false;
            lbBaseLayer1.threshholdRepairHoles = 0f;
            lbBaseLayer1.normaliseImage = true;
            lbBaseLayer1.isBelowSeaLevelDataIncluded = false;
            lbBaseLayer1.floorOffsetY = 0f;
            lbBaseLayer1.mapPathBlendCurve = mapPathBlendCurveLyr1;
            lbBaseLayer1.mapPathHeightCurve = mapPathHeightCurveLyr1;
            lbBaseLayer1.mapPathAddInvert = false;

            // MANAUL STEPS REQUIRED
            // 1. In the Application.dataPath folder create a Heightmaps folder.
            // 2. If using a LB modifier, create a duplicate of the .raw file.
            // 3. Copy the .raw file into the new Heightmaps folder.
            // 4. Ensure the file in Heightmaps folder is the same name as the original.
            LBRaw lbRawLyr1 = LBRaw.ImportHeightmapRAW(Application.dataPath + "/Heightmaps/LauterbrunnenValleyCH.RAW", false, false);
            if (lbRawLyr1 != null)
            {
                lbBaseLayer1.modifierRAWFile = lbRawLyr1;
            }
            lbBaseLayer1.modifierMode = LBLayer.LayerModifierMode.Set;
            lbBaseLayer1.modifierAddInvert = false;
            lbBaseLayer1.modifierUseBlending = false;
            lbBaseLayer1.areaRectRotation = 0f;
            lbBaseLayer1.modifierLandformCategory = LBModifierOperations.ModifierLandformCategory.Valleys;
            lbBaseLayer1.modifierSourceFileType = LBRaw.SourceFileType.RAW;
            // Currently runtime does not support water with Modifier Layers - contact support or post in our Unity forum if you need this feature 
            lbBaseLayer1.modifierUseWater = false;
            lbBaseLayer1.modifierWaterIsMeshLandscapeUV = false;
            lbBaseLayer1.modifierWaterMeshUVTileScale = new Vector2(1f, 1f);
            // NOTE Add the new layer to the landscape meta-data
            landscape.topographyLayersList.Add(lbBaseLayer1);
        }
        #endregion

        // Create the terrain topographies
        landscape.ApplyTopography(false, showErrors);

        // Paste code generated from Trees Tab items here

        #region LBTerrainTree1
        LBTerrainTree lbTerrainTree1 = new LBTerrainTree();
        if (lbTerrainTree1 != null)
        {
            lbTerrainTree1.maxTreesPerSqrKm = 250;
            lbTerrainTree1.bendFactor = 0.5f;
            lbTerrainTree1.prefab = prefabTree1;
            lbTerrainTree1.minScale = 1f;
            lbTerrainTree1.maxScale = 3f;
            lbTerrainTree1.treeScalingMode = LBTerrainTree.TreeScalingMode.ScaleByTerrainHeight;
            lbTerrainTree1.lockWidthToHeight = true;
            lbTerrainTree1.minProximity = 10f;
            lbTerrainTree1.minHeight = 0f;
            lbTerrainTree1.maxHeight = 0.5f;
            lbTerrainTree1.minInclination = 0f;
            lbTerrainTree1.maxInclination = 30f;
            lbTerrainTree1.treePlacingMode = LBTerrainTree.TreePlacingMode.HeightAndInclination;
            lbTerrainTree1.isCurvatureConcave = false;
            lbTerrainTree1.curvatureDistance = 5f;
            lbTerrainTree1.curvatureMinHeightDiff = 1f;
            lbTerrainTree1.map = mapTree1;
            lbTerrainTree1.mapColour = new Color(1f, 0.9215686f, 0.01568628f, 1f);
            lbTerrainTree1.mapTolerance = 1;
            lbTerrainTree1.mapInverse = false;
            lbTerrainTree1.useNoise = true;
            lbTerrainTree1.noiseTileSize = 500f;
            lbTerrainTree1.noiseOffset = 207.9302f;
            lbTerrainTree1.treePlacementCutoff = 0.65f;
            lbTerrainTree1.mapToleranceBlendCurve = LBMap.GetDefaultToleranceBlendCurve;
            lbTerrainTree1.mapIsPath = false;
            lbTerrainTree1.isDisabled = false;
            lbTerrainTree1.offsetY = 0f;
            lbTerrainTree1.showTree = true;
            lbTerrainTree1.isTinted = true;
            lbTerrainTree1.maxTintStrength = 0.2f;
            lbTerrainTree1.tintColour = new Color(0f, 1f, 0f, 1f);
            lbTerrainTree1.prefabName = "";
            lbTerrainTree1.showPrefabPreview = false;
            lbTerrainTree1.GUID = "65715bda-04d5-4916-9ee6-d9f4051f200b";
            lbTerrainTree1.filterList = new List<LBFilter>();
            LBFilter lbFilterTree1 = null;
            lbFilterTree1 = LBFilter.CreateFilter("137beacb-3551-4948-b026-fad31d958fe2", "ec3eeff6-e331-42fb-b209-d04e335ec184", false);
            if (lbFilterTree1 != null)
            {
                lbFilterTree1.filterMode = LBFilter.FilterMode.NOT;
                lbTerrainTree1.filterList.Add(lbFilterTree1);
            }

            lbTerrainTree1.lbTerrainDataList = null;
            // NOTE Add the new Tree to the landscape meta-data
            landscape.terrainTreesList.Add(lbTerrainTree1);
        }
        #endregion

        // Add Trees to the terrains
        #region Add Trees to Terrains
        landscape.treesHaveColliders = true;
        landscape.treePlacementSpeed = LBTerrainTree.TreePlacementSpeed.FastPlacement;
        landscape.ApplyTrees(true, showErrors);
        #endregion

        // Paste code generated from Texturing Tab items here

        #region LBTerrainTexture1
        AnimationCurve mapToleranceBlendCurveTex1 = new AnimationCurve();
        mapToleranceBlendCurveTex1.AddKey(0.00f, 0.00f);
        mapToleranceBlendCurveTex1.AddKey(0.50f, 0.13f);
        mapToleranceBlendCurveTex1.AddKey(1.00f, 1.00f);
        Keyframe[] mapToleranceBlendCurveTex1Keys = mapToleranceBlendCurveTex1.keys;
        mapToleranceBlendCurveTex1Keys[0].inTangent = 0.00f;
        mapToleranceBlendCurveTex1Keys[0].outTangent = 0.00f;
        mapToleranceBlendCurveTex1Keys[1].inTangent = 0.75f;
        mapToleranceBlendCurveTex1Keys[1].outTangent = 0.75f;
        mapToleranceBlendCurveTex1Keys[2].inTangent = 3.00f;
        mapToleranceBlendCurveTex1Keys[2].outTangent = 3.00f;
        mapToleranceBlendCurveTex1 = new AnimationCurve(mapToleranceBlendCurveTex1Keys);

        LBTerrainTexture lbTerrainTexture1 = new LBTerrainTexture();
        if (lbTerrainTexture1 != null)
        {
            lbTerrainTexture1.texture = textureTex1;
            lbTerrainTexture1.normalMap = normalMapTex1;
            lbTerrainTexture1.heightMap = heightMapTex1;
            lbTerrainTexture1.textureName = "";
            lbTerrainTexture1.normalMapName = "";
            lbTerrainTexture1.tileSize = new Vector2(25, 25);
            lbTerrainTexture1.smoothness = 0f;
            lbTerrainTexture1.metallic = 0f;
            lbTerrainTexture1.minHeight = 0f;
            lbTerrainTexture1.maxHeight = 0.1875f;
            lbTerrainTexture1.minInclination = 0f;
            lbTerrainTexture1.maxInclination = 30f;
            lbTerrainTexture1.strength = 0.01f;
            lbTerrainTexture1.texturingMode = LBTerrainTexture.TexturingMode.Height;
            lbTerrainTexture1.isCurvatureConcave = false;
            lbTerrainTexture1.curvatureDistance = 5f;
            lbTerrainTexture1.curvatureMinHeightDiff = 1f;
            lbTerrainTexture1.map = mapTex1;
            lbTerrainTexture1.mapColour = new Color(1f, 0f, 0f, 1f);
            lbTerrainTexture1.isDisabled = false;
            lbTerrainTexture1.mapTolerance = 1;
            lbTerrainTexture1.useNoise = true;
            lbTerrainTexture1.noiseTileSize = 100f;
            lbTerrainTexture1.isMinimalBlendingEnabled = false;
            lbTerrainTexture1.mapInverse = false;
            lbTerrainTexture1.useAdvancedMapTolerance = false;
            lbTerrainTexture1.mapToleranceRed = 0;
            lbTerrainTexture1.mapToleranceGreen = 0;
            lbTerrainTexture1.mapToleranceBlue = 0;
            lbTerrainTexture1.mapToleranceAlpha = 0;
            lbTerrainTexture1.mapWeightRed = 1f;
            lbTerrainTexture1.mapWeightGreen = 1f;
            lbTerrainTexture1.mapWeightBlue = 1f;
            lbTerrainTexture1.mapWeightAlpha = 1f;
            lbTerrainTexture1.mapToleranceBlendCurvePreset = LBCurve.BlendCurvePreset.Cubed;
            lbTerrainTexture1.mapToleranceBlendCurve = mapToleranceBlendCurveTex1;
            lbTerrainTexture1.mapIsPath = false;
            lbTerrainTexture1.isTinted = false;
            lbTerrainTexture1.tintColour = new Color(0f, 0f, 0f, 0f);
            lbTerrainTexture1.tintStrength = 0.5f;
            lbTerrainTexture1.tintedTexture = null;
            lbTerrainTexture1.isRotated = false;
            lbTerrainTexture1.rotationAngle = 0f;
            lbTerrainTexture1.rotatedTexture = null;
            lbTerrainTexture1.showTexture = true;
            lbTerrainTexture1.noiseOffset = 41.58604f;
            lbTerrainTexture1.GUID = "fc668161-6496-49e2-8dd2-41acf1db4236";
            lbTerrainTexture1.filterList = new List<LBTextureFilter>();
            lbTerrainTexture1.lbTerrainDataList = null;
            // NOTE Add the new Texture to the landscape meta-data
            landscape.terrainTexturesList.Add(lbTerrainTexture1);
        }
        #endregion

        #region LBTerrainTexture2
        AnimationCurve mapToleranceBlendCurveTex2 = new AnimationCurve();
        mapToleranceBlendCurveTex2.AddKey(0.00f, 0.00f);
        mapToleranceBlendCurveTex2.AddKey(0.50f, 0.13f);
        mapToleranceBlendCurveTex2.AddKey(1.00f, 1.00f);
        Keyframe[] mapToleranceBlendCurveTex2Keys = mapToleranceBlendCurveTex2.keys;
        mapToleranceBlendCurveTex2Keys[0].inTangent = 0.00f;
        mapToleranceBlendCurveTex2Keys[0].outTangent = 0.00f;
        mapToleranceBlendCurveTex2Keys[1].inTangent = 0.75f;
        mapToleranceBlendCurveTex2Keys[1].outTangent = 0.75f;
        mapToleranceBlendCurveTex2Keys[2].inTangent = 3.00f;
        mapToleranceBlendCurveTex2Keys[2].outTangent = 3.00f;
        mapToleranceBlendCurveTex2 = new AnimationCurve(mapToleranceBlendCurveTex2Keys);

        LBTerrainTexture lbTerrainTexture2 = new LBTerrainTexture();
        if (lbTerrainTexture2 != null)
        {
            lbTerrainTexture2.texture = textureTex2;
            lbTerrainTexture2.normalMap = normalMapTex2;
            lbTerrainTexture2.heightMap = heightMapTex2;
            lbTerrainTexture2.textureName = "";
            lbTerrainTexture2.normalMapName = "";
            lbTerrainTexture2.tileSize = new Vector2(25, 25);
            lbTerrainTexture2.smoothness = 0f;
            lbTerrainTexture2.metallic = 0f;
            lbTerrainTexture2.minHeight = 0.175f;
            lbTerrainTexture2.maxHeight = 1f;
            lbTerrainTexture2.minInclination = 0f;
            lbTerrainTexture2.maxInclination = 30f;
            lbTerrainTexture2.strength = 1f;
            lbTerrainTexture2.texturingMode = LBTerrainTexture.TexturingMode.HeightAndInclination;
            lbTerrainTexture2.isCurvatureConcave = false;
            lbTerrainTexture2.curvatureDistance = 5f;
            lbTerrainTexture2.curvatureMinHeightDiff = 1f;
            lbTerrainTexture2.map = mapTex2;
            lbTerrainTexture2.mapColour = new Color(1f, 0f, 0f, 1f);
            lbTerrainTexture2.isDisabled = false;
            lbTerrainTexture2.mapTolerance = 1;
            lbTerrainTexture2.useNoise = true;
            lbTerrainTexture2.noiseTileSize = 100f;
            lbTerrainTexture2.isMinimalBlendingEnabled = false;
            lbTerrainTexture2.mapInverse = false;
            lbTerrainTexture2.useAdvancedMapTolerance = false;
            lbTerrainTexture2.mapToleranceRed = 0;
            lbTerrainTexture2.mapToleranceGreen = 0;
            lbTerrainTexture2.mapToleranceBlue = 0;
            lbTerrainTexture2.mapToleranceAlpha = 0;
            lbTerrainTexture2.mapWeightRed = 1f;
            lbTerrainTexture2.mapWeightGreen = 1f;
            lbTerrainTexture2.mapWeightBlue = 1f;
            lbTerrainTexture2.mapWeightAlpha = 1f;
            lbTerrainTexture2.mapToleranceBlendCurvePreset = LBCurve.BlendCurvePreset.Cubed;
            lbTerrainTexture2.mapToleranceBlendCurve = mapToleranceBlendCurveTex2;
            lbTerrainTexture2.mapIsPath = false;
            lbTerrainTexture2.isTinted = false;
            lbTerrainTexture2.tintColour = new Color(0f, 0f, 0f, 0f);
            lbTerrainTexture2.tintStrength = 0.5f;
            lbTerrainTexture2.tintedTexture = null;
            lbTerrainTexture2.isRotated = false;
            lbTerrainTexture2.rotationAngle = 0f;
            lbTerrainTexture2.rotatedTexture = null;
            lbTerrainTexture2.showTexture = false;
            lbTerrainTexture2.noiseOffset = 41.59176f;
            lbTerrainTexture2.GUID = "354b1627-8753-4dda-9c42-685d74415ef3";
            lbTerrainTexture2.filterList = new List<LBTextureFilter>();
            lbTerrainTexture2.lbTerrainDataList = null;
            // NOTE Add the new Texture to the landscape meta-data
            landscape.terrainTexturesList.Add(lbTerrainTexture2);
        }
        #endregion

        #region LBTerrainTexture3
        AnimationCurve mapToleranceBlendCurveTex3 = new AnimationCurve();
        mapToleranceBlendCurveTex3.AddKey(0.00f, 0.00f);
        mapToleranceBlendCurveTex3.AddKey(0.50f, 0.13f);
        mapToleranceBlendCurveTex3.AddKey(1.00f, 1.00f);
        Keyframe[] mapToleranceBlendCurveTex3Keys = mapToleranceBlendCurveTex3.keys;
        mapToleranceBlendCurveTex3Keys[0].inTangent = 0.00f;
        mapToleranceBlendCurveTex3Keys[0].outTangent = 0.00f;
        mapToleranceBlendCurveTex3Keys[1].inTangent = 0.75f;
        mapToleranceBlendCurveTex3Keys[1].outTangent = 0.75f;
        mapToleranceBlendCurveTex3Keys[2].inTangent = 3.00f;
        mapToleranceBlendCurveTex3Keys[2].outTangent = 3.00f;
        mapToleranceBlendCurveTex3 = new AnimationCurve(mapToleranceBlendCurveTex3Keys);

        LBTerrainTexture lbTerrainTexture3 = new LBTerrainTexture();
        if (lbTerrainTexture3 != null)
        {
            lbTerrainTexture3.texture = textureTex3;
            lbTerrainTexture3.normalMap = normalMapTex3;
            lbTerrainTexture3.heightMap = heightMapTex3;
            lbTerrainTexture3.textureName = "";
            lbTerrainTexture3.normalMapName = "";
            lbTerrainTexture3.tileSize = new Vector2(25, 25);
            lbTerrainTexture3.smoothness = 0f;
            lbTerrainTexture3.metallic = 0f;
            lbTerrainTexture3.minHeight = 0.25f;
            lbTerrainTexture3.maxHeight = 0.75f;
            lbTerrainTexture3.minInclination = 29f;
            lbTerrainTexture3.maxInclination = 90f;
            lbTerrainTexture3.strength = 1f;
            lbTerrainTexture3.texturingMode = LBTerrainTexture.TexturingMode.Inclination;
            lbTerrainTexture3.isCurvatureConcave = false;
            lbTerrainTexture3.curvatureDistance = 5f;
            lbTerrainTexture3.curvatureMinHeightDiff = 1f;
            lbTerrainTexture3.map = mapTex3;
            lbTerrainTexture3.mapColour = new Color(1f, 0f, 0f, 1f);
            lbTerrainTexture3.isDisabled = false;
            lbTerrainTexture3.mapTolerance = 1;
            lbTerrainTexture3.useNoise = true;
            lbTerrainTexture3.noiseTileSize = 100f;
            lbTerrainTexture3.isMinimalBlendingEnabled = false;
            lbTerrainTexture3.mapInverse = false;
            lbTerrainTexture3.useAdvancedMapTolerance = false;
            lbTerrainTexture3.mapToleranceRed = 0;
            lbTerrainTexture3.mapToleranceGreen = 0;
            lbTerrainTexture3.mapToleranceBlue = 0;
            lbTerrainTexture3.mapToleranceAlpha = 0;
            lbTerrainTexture3.mapWeightRed = 1f;
            lbTerrainTexture3.mapWeightGreen = 1f;
            lbTerrainTexture3.mapWeightBlue = 1f;
            lbTerrainTexture3.mapWeightAlpha = 1f;
            lbTerrainTexture3.mapToleranceBlendCurvePreset = LBCurve.BlendCurvePreset.Cubed;
            lbTerrainTexture3.mapToleranceBlendCurve = mapToleranceBlendCurveTex3;
            lbTerrainTexture3.mapIsPath = false;
            lbTerrainTexture3.isTinted = false;
            lbTerrainTexture3.tintColour = new Color(0f, 0f, 0f, 0f);
            lbTerrainTexture3.tintStrength = 0.5f;
            lbTerrainTexture3.tintedTexture = null;
            lbTerrainTexture3.isRotated = false;
            lbTerrainTexture3.rotationAngle = 0f;
            lbTerrainTexture3.rotatedTexture = null;
            lbTerrainTexture3.showTexture = false;
            lbTerrainTexture3.noiseOffset = 32.63931f;
            lbTerrainTexture3.GUID = "c6ca133e-0fcb-40b9-8d04-d8236c423f9a";
            lbTerrainTexture3.filterList = new List<LBTextureFilter>();
            lbTerrainTexture3.lbTerrainDataList = null;
            // NOTE Add the new Texture to the landscape meta-data
            landscape.terrainTexturesList.Add(lbTerrainTexture3);
        }
        #endregion

        #region LBTerrainTexture4
        AnimationCurve mapToleranceBlendCurveTex4 = new AnimationCurve();
        mapToleranceBlendCurveTex4.AddKey(0.00f, 0.00f);
        mapToleranceBlendCurveTex4.AddKey(0.50f, 0.13f);
        mapToleranceBlendCurveTex4.AddKey(1.00f, 1.00f);
        Keyframe[] mapToleranceBlendCurveTex4Keys = mapToleranceBlendCurveTex4.keys;
        mapToleranceBlendCurveTex4Keys[0].inTangent = 0.00f;
        mapToleranceBlendCurveTex4Keys[0].outTangent = 0.00f;
        mapToleranceBlendCurveTex4Keys[1].inTangent = 0.75f;
        mapToleranceBlendCurveTex4Keys[1].outTangent = 0.75f;
        mapToleranceBlendCurveTex4Keys[2].inTangent = 3.00f;
        mapToleranceBlendCurveTex4Keys[2].outTangent = 3.00f;
        mapToleranceBlendCurveTex4 = new AnimationCurve(mapToleranceBlendCurveTex4Keys);

        LBTerrainTexture lbTerrainTexture4 = new LBTerrainTexture();
        if (lbTerrainTexture4 != null)
        {
            lbTerrainTexture4.texture = textureTex4;
            lbTerrainTexture4.normalMap = normalMapTex4;
            lbTerrainTexture4.heightMap = heightMapTex4;
            lbTerrainTexture4.textureName = "Rocky Dirt";
            lbTerrainTexture4.normalMapName = "";
            lbTerrainTexture4.tileSize = new Vector2(25, 25);
            lbTerrainTexture4.smoothness = 0f;
            lbTerrainTexture4.metallic = 0f;
            lbTerrainTexture4.minHeight = 0.25f;
            lbTerrainTexture4.maxHeight = 0.75f;
            lbTerrainTexture4.minInclination = 0f;
            lbTerrainTexture4.maxInclination = 30f;
            lbTerrainTexture4.strength = 0f;
            lbTerrainTexture4.texturingMode = LBTerrainTexture.TexturingMode.ConstantInfluence;
            lbTerrainTexture4.isCurvatureConcave = false;
            lbTerrainTexture4.curvatureDistance = 5f;
            lbTerrainTexture4.curvatureMinHeightDiff = 1f;
            lbTerrainTexture4.map = mapTex4;
            lbTerrainTexture4.mapColour = new Color(1f, 0f, 0f, 1f);
            lbTerrainTexture4.isDisabled = false;
            lbTerrainTexture4.mapTolerance = 1;
            lbTerrainTexture4.useNoise = false;
            lbTerrainTexture4.noiseTileSize = 100f;
            lbTerrainTexture4.isMinimalBlendingEnabled = false;
            lbTerrainTexture4.mapInverse = false;
            lbTerrainTexture4.useAdvancedMapTolerance = false;
            lbTerrainTexture4.mapToleranceRed = 0;
            lbTerrainTexture4.mapToleranceGreen = 0;
            lbTerrainTexture4.mapToleranceBlue = 0;
            lbTerrainTexture4.mapToleranceAlpha = 0;
            lbTerrainTexture4.mapWeightRed = 1f;
            lbTerrainTexture4.mapWeightGreen = 1f;
            lbTerrainTexture4.mapWeightBlue = 1f;
            lbTerrainTexture4.mapWeightAlpha = 1f;
            lbTerrainTexture4.mapToleranceBlendCurvePreset = LBCurve.BlendCurvePreset.Cubed;
            lbTerrainTexture4.mapToleranceBlendCurve = mapToleranceBlendCurveTex4;
            lbTerrainTexture4.mapIsPath = false;
            lbTerrainTexture4.isTinted = false;
            lbTerrainTexture4.tintColour = new Color(0f, 0f, 0f, 0f);
            lbTerrainTexture4.tintStrength = 0.5f;
            lbTerrainTexture4.tintedTexture = null;
            lbTerrainTexture4.isRotated = false;
            lbTerrainTexture4.rotationAngle = 0f;
            lbTerrainTexture4.rotatedTexture = null;
            lbTerrainTexture4.showTexture = false;
            lbTerrainTexture4.noiseOffset = 23.3493f;
            lbTerrainTexture4.GUID = "4ff54e18-18b3-4778-9f10-f3c8f13f1fd5";
            lbTerrainTexture4.filterList = new List<LBTextureFilter>();
            lbTerrainTexture4.lbTerrainDataList = null;
            // NOTE Add the new Texture to the landscape meta-data
            landscape.terrainTexturesList.Add(lbTerrainTexture4);
        }
        #endregion

        // Texture the terrains
        landscape.ApplyTextures(true, showErrors);

        // Paste code generated from Grass Tab items here

        // Add Grass to the terrains
        landscape.ApplyGrass(true, true);


        // Paste code generated from Groups Tab items here

        #region Group1 [path group]
        LBGroup lbGroup1 = new LBGroup();
        if (lbGroup1 != null)
        {
            #region Group1-level variables
            lbGroup1.groupName = "path group";
            lbGroup1.lbGroupType = LBGroup.LBGroupType.Uniform;
            lbGroup1.maxGroupSqrKm = 10;
            lbGroup1.isDisabled = false;
            lbGroup1.showInEditor = true;
            lbGroup1.showGroupDefaultsInEditor = true;
            lbGroup1.showGroupOptionsInEditor = true;
            lbGroup1.showGroupMembersInEditor = true;
            lbGroup1.showGroupDesigner = false;
            lbGroup1.showGroupsInScene = false;
            lbGroup1.showtabInEditor = 0;
            lbGroup1.isMemberListExpanded = true;
            // Clearing Group variables
            lbGroup1.minClearingRadius = 100f;
            lbGroup1.maxClearingRadius = 100f;
            lbGroup1.startClearingRotationY = 0f;
            lbGroup1.endClearingRotationY = 359.9f;
            lbGroup1.isRemoveExistingGrass = true;
            lbGroup1.removeExistingGrassBlendDist = 0.5f;
            lbGroup1.isRemoveExistingTrees = true;
            // Proximity variables
            lbGroup1.proximityExtent = 10f;
            // Default values per group
            lbGroup1.minScale = 1f;
            lbGroup1.maxScale = 1f;
            lbGroup1.minHeight = 0f;
            lbGroup1.maxHeight = 1f;
            lbGroup1.minInclination = 0f;
            lbGroup1.maxInclination = 90f;
            // Group flatten terrain variables
            lbGroup1.isTerrainFlattened = false;
            lbGroup1.flattenHeightOffset = 0f;
            lbGroup1.flattenBlendRate = 0.5f;
            #endregion

            #region Group1 Zones
            // Start Group1-Level Zones
            // End Group1-Level Zones
            #endregion

            #region Group1 Filters
            lbGroup1.filterList = new List<LBFilter>();
            #endregion

            #region Group1 Textures
            // Only apply to Procedural and Manual Clearing groups
            lbGroup1.textureList = new List<LBGroupTexture>();
            #endregion

            #region Group1 Grass
            // Only apply to Procedural and Manual Clearing groups
            lbGroup1.grassList = new List<LBGroupGrass>();
            #endregion

            // Start Group Members
            #region Group1 Member1 
            LBGroupMember group1_lbGroupMember1 = new LBGroupMember();
            if (group1_lbGroupMember1 != null)
            {
                group1_lbGroupMember1.isDisabled = false;
                group1_lbGroupMember1.showInEditor = true;
                group1_lbGroupMember1.showtabInEditor = 1;
                group1_lbGroupMember1.GUID = "e511ea98-2e5a-4e1c-b251-05b438d4c3ae";
                group1_lbGroupMember1.lbMemberType = LBGroupMember.LBMemberType.ObjPath;
                group1_lbGroupMember1.isGroupOverride = false;
                group1_lbGroupMember1.minScale = 1f;
                group1_lbGroupMember1.maxScale = 1f;
                group1_lbGroupMember1.minHeight = 0f;
                group1_lbGroupMember1.maxHeight = 1f;
                group1_lbGroupMember1.minInclination = 0f;
                group1_lbGroupMember1.maxInclination = 90f;
                group1_lbGroupMember1.prefab = null;
                group1_lbGroupMember1.prefabName = "";
                group1_lbGroupMember1.showPrefabPreview = false;
                group1_lbGroupMember1.isKeepPrefabConnection = false;
                group1_lbGroupMember1.isCombineMesh = false;
                group1_lbGroupMember1.isRemoveEmptyGameObjects = true;
                group1_lbGroupMember1.isRemoveAnimator = true;
                group1_lbGroupMember1.isCreateCollider = false;
                group1_lbGroupMember1.maxPrefabSqrKm = 10;
                group1_lbGroupMember1.maxPrefabPerGroup = 10000;
                group1_lbGroupMember1.isPlacedInCentre = false;
                group1_lbGroupMember1.showXYZSettings = false;
                group1_lbGroupMember1.modelOffsetX = 0f;
                group1_lbGroupMember1.modelOffsetY = 0f;
                group1_lbGroupMember1.modelOffsetZ = 0f;
                group1_lbGroupMember1.minOffsetX = 0f;
                group1_lbGroupMember1.minOffsetZ = 0f;
                group1_lbGroupMember1.minOffsetY = 0f;
                group1_lbGroupMember1.maxOffsetY = 0f;
                group1_lbGroupMember1.randomiseOffsetY = false;
                group1_lbGroupMember1.rotationType = LBGroupMember.LBRotationType.WorldSpace;
                group1_lbGroupMember1.randomiseRotationY = true;
                group1_lbGroupMember1.startRotationY = 0f;
                group1_lbGroupMember1.endRotationY = 359.9f;
                group1_lbGroupMember1.randomiseRotationXZ = false;
                group1_lbGroupMember1.rotationX = 0f;
                group1_lbGroupMember1.rotationZ = 0f;
                group1_lbGroupMember1.endRotationX = 0f;
                group1_lbGroupMember1.endRotationZ = 0f;
                group1_lbGroupMember1.isLockTilt = false;
                group1_lbGroupMember1.useNoise = false;
                group1_lbGroupMember1.noiseOffset = 207.9302f;
                group1_lbGroupMember1.noiseTileSize = 500f;
                group1_lbGroupMember1.noisePlacementCutoff = 1f;
                group1_lbGroupMember1.proximityExtent = 10f;
                group1_lbGroupMember1.removeGrassBlendDist = 0.5f;
                group1_lbGroupMember1.minGrassProximity = 0f;
                group1_lbGroupMember1.isRemoveTree = true;
                group1_lbGroupMember1.minTreeProximity = 10f;
                group1_lbGroupMember1.isTerrainAligned = false;
                group1_lbGroupMember1.isTerrainFlattened = true;
                group1_lbGroupMember1.flattenDistance = 2f;
                group1_lbGroupMember1.flattenHeightOffset = 0f;
                group1_lbGroupMember1.flattenBlendRate = 0.5f;

                // Start Member-Level Zones references for group1_lbGroupMember1

                // End Member-Level Zones references for group1_lbGroupMember1

                group1_lbGroupMember1.isZoneEdgeFillTop = false;
                group1_lbGroupMember1.isZoneEdgeFillBottom = false;
                group1_lbGroupMember1.isZoneEdgeFillLeft = false;
                group1_lbGroupMember1.isZoneEdgeFillRight = false;
                group1_lbGroupMember1.zoneEdgeFillDistance = 1f;
                group1_lbGroupMember1.isPathOnly = false;
                group1_lbGroupMember1.lbObjectOrientation = LBObjPath.LBObjectOrientation.PathSpace;

                #region Start Object Path settings for [path group objpath 01]
                group1_lbGroupMember1.lbObjPath = new LBObjPath();
                if (group1_lbGroupMember1.lbObjPath != null)
                {
                    // LBPath settings
                    group1_lbGroupMember1.lbObjPath.pathName = "path group objpath 01";
                    group1_lbGroupMember1.lbObjPath.showPathInScene = false;
                    group1_lbGroupMember1.lbObjPath.blendStart = true;
                    group1_lbGroupMember1.lbObjPath.blendEnd = true;
                    group1_lbGroupMember1.lbObjPath.pathResolution = 2f;
                    group1_lbGroupMember1.lbObjPath.closedCircuit = false;
                    group1_lbGroupMember1.lbObjPath.edgeBlendWidth = 13f;
                    group1_lbGroupMember1.lbObjPath.removeCentre = false;
                    group1_lbGroupMember1.lbObjPath.leftBorderWidth = 1.5f;
                    group1_lbGroupMember1.lbObjPath.rightBorderWidth = 1.5f;
                    group1_lbGroupMember1.lbObjPath.snapToTerrain = false;
                    group1_lbGroupMember1.lbObjPath.heightAboveTerrain = 2f;
                    group1_lbGroupMember1.lbObjPath.zoomOnFind = true;
                    group1_lbGroupMember1.lbObjPath.findZoomDistance = 50f;
                    // LBPath Surface options
                    group1_lbGroupMember1.lbObjPath.isMeshLandscapeUV = false;
                    group1_lbGroupMember1.lbObjPath.meshUVTileScale = new Vector2(1f, 1f);
                    group1_lbGroupMember1.lbObjPath.meshYOffset = 0.03f;
                    group1_lbGroupMember1.lbObjPath.meshEdgeSnapToTerrain = false;
                    group1_lbGroupMember1.lbObjPath.meshSnapType = LBPath.MeshSnapType.BothEdges;
                    group1_lbGroupMember1.lbObjPath.meshIsDoubleSided = false;
                    group1_lbGroupMember1.lbObjPath.meshIncludeEdges = true;
                    group1_lbGroupMember1.lbObjPath.meshIncludeWater = false;
                    group1_lbGroupMember1.lbObjPath.meshSaveToProjectFolder = false;
                    // Path Points
                    group1_lbGroupMember1.lbObjPath.positionList.Add(new Vector3(817.4839f, 21.46869f, 84.36831f));
                    group1_lbGroupMember1.lbObjPath.positionList.Add(new Vector3(786.0258f, 16.40092f, 292.0215f));
                    group1_lbGroupMember1.lbObjPath.positionList.Add(new Vector3(855.6399f, 14.42382f, 471.945f));
                    group1_lbGroupMember1.lbObjPath.positionList.Add(new Vector3(958.4627f, 12.43765f, 681.1046f));
                    group1_lbGroupMember1.lbObjPath.positionList.Add(new Vector3(1009.286f, 11.21687f, 894.7774f));
                    group1_lbGroupMember1.lbObjPath.positionList.Add(new Vector3(994.5861f, 9.14256f, 1236.36f));
                    group1_lbGroupMember1.lbObjPath.positionList.Add(new Vector3(930.4923f, 9.863279f, 1551.112f));
                    group1_lbGroupMember1.lbObjPath.widthList.Add(19.996f);
                    group1_lbGroupMember1.lbObjPath.widthList.Add(20f);
                    group1_lbGroupMember1.lbObjPath.widthList.Add(20f);
                    group1_lbGroupMember1.lbObjPath.widthList.Add(20f);
                    group1_lbGroupMember1.lbObjPath.widthList.Add(20f);
                    group1_lbGroupMember1.lbObjPath.widthList.Add(20f);
                    group1_lbGroupMember1.lbObjPath.widthList.Add(19.996f);
                    group1_lbGroupMember1.lbObjPath.positionListLeftEdge.Add(new Vector3(807.6017f, 21.46869f, 82.85145f));
                    group1_lbGroupMember1.lbObjPath.positionListLeftEdge.Add(new Vector3(776.0804f, 16.40092f, 293.0654f));
                    group1_lbGroupMember1.lbObjPath.positionListLeftEdge.Add(new Vector3(846.5087f, 14.42382f, 476.0219f));
                    group1_lbGroupMember1.lbObjPath.positionListLeftEdge.Add(new Vector3(949.0643f, 12.43765f, 684.5208f));
                    group1_lbGroupMember1.lbObjPath.positionListLeftEdge.Add(new Vector3(999.3066f, 11.21687f, 895.416f));
                    group1_lbGroupMember1.lbObjPath.positionListLeftEdge.Add(new Vector3(984.6592f, 9.14256f, 1235.153f));
                    group1_lbGroupMember1.lbObjPath.positionListLeftEdge.Add(new Vector3(920.696f, 9.863279f, 1549.114f));
                    group1_lbGroupMember1.lbObjPath.positionListRightEdge.Add(new Vector3(827.3662f, 21.46869f, 85.88517f));
                    group1_lbGroupMember1.lbObjPath.positionListRightEdge.Add(new Vector3(795.9711f, 16.40092f, 290.9775f));
                    group1_lbGroupMember1.lbObjPath.positionListRightEdge.Add(new Vector3(864.7711f, 14.42382f, 467.8681f));
                    group1_lbGroupMember1.lbObjPath.positionListRightEdge.Add(new Vector3(967.8611f, 12.43765f, 677.6884f));
                    group1_lbGroupMember1.lbObjPath.positionListRightEdge.Add(new Vector3(1019.266f, 11.21687f, 894.1389f));
                    group1_lbGroupMember1.lbObjPath.positionListRightEdge.Add(new Vector3(1004.513f, 9.14256f, 1237.568f));
                    group1_lbGroupMember1.lbObjPath.positionListRightEdge.Add(new Vector3(940.2886f, 9.863279f, 1553.11f));
                    group1_lbGroupMember1.lbObjPath.minPathWidth = group1_lbGroupMember1.lbObjPath.GetMinWidth();
                    // LBObjPath settings
                    group1_lbGroupMember1.lbObjPath.useWidth = true;
                    group1_lbGroupMember1.lbObjPath.useSurfaceMesh = true;
                    group1_lbGroupMember1.lbObjPath.layoutMethod = LBObjPath.LayoutMethod.Spacing;
                    group1_lbGroupMember1.lbObjPath.selectionMethod = LBObjPath.SelectionMethod.Alternating;
                    group1_lbGroupMember1.lbObjPath.spacingDistance = 7f;
                    group1_lbGroupMember1.lbObjPath.maxMainPrefabs = 5;
                    group1_lbGroupMember1.lbObjPath.isLastObjSnappedToEnd = true;
                    group1_lbGroupMember1.lbObjPath.surroundSmoothing = 0f;
                    group1_lbGroupMember1.lbObjPath.coreTextureGUID = "4ff54e18-18b3-4778-9f10-f3c8f13f1fd5";
                    group1_lbGroupMember1.lbObjPath.coreTextureNoiseTileSize = 0f;
                    group1_lbGroupMember1.lbObjPath.coreTextureStrength = 0.179f;
                    group1_lbGroupMember1.lbObjPath.surroundTextureGUID = "";
                    group1_lbGroupMember1.lbObjPath.surroundTextureNoiseTileSize = 0f;
                    group1_lbGroupMember1.lbObjPath.surroundTextureStrength = 1f;
                    group1_lbGroupMember1.lbObjPath.surfaceMeshMaterial = Group001_Member001surfaceMeshMat;
                    // ObjPath Curves
                    AnimationCurve grp1_gmbr1profileHeightCurve = new AnimationCurve();
                    grp1_gmbr1profileHeightCurve.AddKey(0.00f, 1.00f);
                    grp1_gmbr1profileHeightCurve.AddKey(1.00f, 1.00f);
                    Keyframe[] grp1_gmbr1profileHeightCurveKeys = grp1_gmbr1profileHeightCurve.keys;
                    grp1_gmbr1profileHeightCurveKeys[0].inTangent = 0.00f;
                    grp1_gmbr1profileHeightCurveKeys[0].outTangent = 0.00f;
                    grp1_gmbr1profileHeightCurveKeys[1].inTangent = 0.00f;
                    grp1_gmbr1profileHeightCurveKeys[1].outTangent = 0.00f;
                    grp1_gmbr1profileHeightCurve = new AnimationCurve(grp1_gmbr1profileHeightCurveKeys);

                    AnimationCurve grp1_gmbr1surroundBlendCurve = new AnimationCurve();
                    grp1_gmbr1surroundBlendCurve.AddKey(0.00f, 0.00f);
                    grp1_gmbr1surroundBlendCurve.AddKey(1.00f, 1.00f);
                    Keyframe[] grp1_gmbr1surroundBlendCurveKeys = grp1_gmbr1surroundBlendCurve.keys;
                    grp1_gmbr1surroundBlendCurveKeys[0].inTangent = 0.00f;
                    grp1_gmbr1surroundBlendCurveKeys[0].outTangent = 0.00f;
                    grp1_gmbr1surroundBlendCurveKeys[1].inTangent = 0.00f;
                    grp1_gmbr1surroundBlendCurveKeys[1].outTangent = 0.00f;
                    grp1_gmbr1surroundBlendCurve = new AnimationCurve(grp1_gmbr1surroundBlendCurveKeys);

                    group1_lbGroupMember1.lbObjPath.profileHeightCurve = grp1_gmbr1profileHeightCurve;
                    group1_lbGroupMember1.lbObjPath.surroundBlendCurve = grp1_gmbr1surroundBlendCurve;

                    group1_lbGroupMember1.lbObjPath.pathPointList = new List<LBPathPoint>();
                    group1_lbGroupMember1.lbObjPath.mainObjPrefabList = new List<LBObjPrefab>();
                    // ObjPath Points
                    LBPathPoint group1_lbGroupMember1PathPoint = null;
                    group1_lbGroupMember1PathPoint = new LBPathPoint();
                    group1_lbGroupMember1PathPoint.GUID = "c1ea4aef-3ce0-417e-ab16-7bc7f2692d51";
                    group1_lbGroupMember1PathPoint.showInEditor = false;
                    group1_lbGroupMember1PathPoint.rotationZ = 0f;
                    group1_lbGroupMember1.lbObjPath.pathPointList.Add(group1_lbGroupMember1PathPoint);
                    group1_lbGroupMember1PathPoint = null;
                    group1_lbGroupMember1PathPoint = new LBPathPoint();
                    group1_lbGroupMember1PathPoint.GUID = "7d57f9dd-4d3f-445a-8f51-af45a17a36a5";
                    group1_lbGroupMember1PathPoint.showInEditor = false;
                    group1_lbGroupMember1PathPoint.rotationZ = 0f;
                    group1_lbGroupMember1.lbObjPath.pathPointList.Add(group1_lbGroupMember1PathPoint);
                    group1_lbGroupMember1PathPoint = null;
                    group1_lbGroupMember1PathPoint = new LBPathPoint();
                    group1_lbGroupMember1PathPoint.GUID = "4bb052de-6c15-4199-bc18-2c79a89a5aa1";
                    group1_lbGroupMember1PathPoint.showInEditor = false;
                    group1_lbGroupMember1PathPoint.rotationZ = 0f;
                    group1_lbGroupMember1.lbObjPath.pathPointList.Add(group1_lbGroupMember1PathPoint);
                    group1_lbGroupMember1PathPoint = null;
                    group1_lbGroupMember1PathPoint = new LBPathPoint();
                    group1_lbGroupMember1PathPoint.GUID = "195f1f37-bbad-4caf-ba7d-c303086b0c6a";
                    group1_lbGroupMember1PathPoint.showInEditor = false;
                    group1_lbGroupMember1PathPoint.rotationZ = 0f;
                    group1_lbGroupMember1.lbObjPath.pathPointList.Add(group1_lbGroupMember1PathPoint);
                    group1_lbGroupMember1PathPoint = null;
                    group1_lbGroupMember1PathPoint = new LBPathPoint();
                    group1_lbGroupMember1PathPoint.GUID = "489b28c9-365f-4223-b4bb-74269c9bfd1c";
                    group1_lbGroupMember1PathPoint.showInEditor = false;
                    group1_lbGroupMember1PathPoint.rotationZ = 0f;
                    group1_lbGroupMember1.lbObjPath.pathPointList.Add(group1_lbGroupMember1PathPoint);
                    group1_lbGroupMember1PathPoint = null;
                    group1_lbGroupMember1PathPoint = new LBPathPoint();
                    group1_lbGroupMember1PathPoint.GUID = "57426bc1-6a84-421b-bb1f-daebb51d9c01";
                    group1_lbGroupMember1PathPoint.showInEditor = false;
                    group1_lbGroupMember1PathPoint.rotationZ = 0f;
                    group1_lbGroupMember1.lbObjPath.pathPointList.Add(group1_lbGroupMember1PathPoint);
                    group1_lbGroupMember1PathPoint = null;
                    group1_lbGroupMember1PathPoint = new LBPathPoint();
                    group1_lbGroupMember1PathPoint.GUID = "e4cf9f6a-b806-44f4-944a-83b1cb56c773";
                    group1_lbGroupMember1PathPoint.showInEditor = false;
                    group1_lbGroupMember1PathPoint.rotationZ = 0f;
                    group1_lbGroupMember1.lbObjPath.pathPointList.Add(group1_lbGroupMember1PathPoint);
                    group1_lbGroupMember1PathPoint = null;
                    // Start ObjPrefab
                    group1_lbGroupMember1.lbObjPath.startObjPrefab = new LBObjPrefab();
                    group1_lbGroupMember1.lbObjPath.startObjPrefab.groupMemberGUID = "";
                    // End ObjPrefab
                    group1_lbGroupMember1.lbObjPath.endObjPrefab = new LBObjPrefab();
                    group1_lbGroupMember1.lbObjPath.endObjPrefab.groupMemberGUID = "";
                    group1_lbGroupMember1.lbObjPath.lbObjPathSeriesList = new List<LBObjPathSeries>();

                    #region ObjPathSeries
                    LBObjPathSeries group1_lbGroupMember1ObjPathSeries = null;

                    #region Series 1
                    group1_lbGroupMember1ObjPathSeries = new LBObjPathSeries();
                    group1_lbGroupMember1ObjPathSeries.seriesGUID = "2520a5d2-abf5-4963-aede-1f9d6012e053";
                    group1_lbGroupMember1ObjPathSeries.seriesName = "posts by roadside";
                    group1_lbGroupMember1ObjPathSeries.showInEditor = false;
                    group1_lbGroupMember1ObjPathSeries.layoutMethod = LBObjPath.LayoutMethod.Spacing;
                    group1_lbGroupMember1ObjPathSeries.selectionMethod = LBObjPath.SelectionMethod.Alternating;
                    group1_lbGroupMember1ObjPathSeries.mainObjPrefabList = new List<LBObjPrefab>();
                    // Series Main ObjPrefabs
                    LBObjPrefab group1_lbGroupMember1ObjPathPrefab0 = null;
                    group1_lbGroupMember1ObjPathPrefab0 = new LBObjPrefab();
                    group1_lbGroupMember1ObjPathPrefab0.groupMemberGUID = "f39d69fe-b08f-4cfe-bb11-fb8e8a3cbf35";
                    group1_lbGroupMember1ObjPathSeries.mainObjPrefabList.Add(group1_lbGroupMember1ObjPathPrefab0);
                    group1_lbGroupMember1ObjPathPrefab0 = null;
                    // Series Start ObjPrefab
                    group1_lbGroupMember1ObjPathSeries.startObjPrefab = new LBObjPrefab();
                    group1_lbGroupMember1ObjPathSeries.startObjPrefab.groupMemberGUID = "";
                    // Series End ObjPrefab
                    group1_lbGroupMember1ObjPathSeries.endObjPrefab = new LBObjPrefab();
                    group1_lbGroupMember1ObjPathSeries.endObjPrefab.groupMemberGUID = "";
                    group1_lbGroupMember1ObjPathSeries.spacingDistance = 7f;
                    group1_lbGroupMember1ObjPathSeries.maxMainPrefabs = 5;
                    group1_lbGroupMember1ObjPathSeries.isLastObjSnappedToEnd = false;
                    group1_lbGroupMember1ObjPathSeries.sparcePlacementCutoff = 1f;
                    group1_lbGroupMember1ObjPathSeries.pathSpline = LBObjPath.PathSpline.RightEdge;
                    group1_lbGroupMember1ObjPathSeries.prefabOffsetZ = 0f;
                    group1_lbGroupMember1ObjPathSeries.prefabStartOffset = 15f;
                    group1_lbGroupMember1ObjPathSeries.prefabEndOffset = 11f;
                    group1_lbGroupMember1ObjPathSeries.use3DDistance = false;
                    group1_lbGroupMember1ObjPathSeries.useNonOffsetDistance = false;
                    group1_lbGroupMember1.lbObjPath.lbObjPathSeriesList.Add(group1_lbGroupMember1ObjPathSeries);
                    group1_lbGroupMember1ObjPathSeries = null;
                    #endregion Series

                    #region Series 2
                    group1_lbGroupMember1ObjPathSeries = new LBObjPathSeries();
                    group1_lbGroupMember1ObjPathSeries.seriesGUID = "450323fd-1ec9-4457-b7f2-1cf4d650da3f";
                    group1_lbGroupMember1ObjPathSeries.seriesName = "rocks by roadside";
                    group1_lbGroupMember1ObjPathSeries.showInEditor = false;
                    group1_lbGroupMember1ObjPathSeries.layoutMethod = LBObjPath.LayoutMethod.Spacing;
                    group1_lbGroupMember1ObjPathSeries.selectionMethod = LBObjPath.SelectionMethod.Alternating;
                    group1_lbGroupMember1ObjPathSeries.mainObjPrefabList = new List<LBObjPrefab>();
                    // Series Main ObjPrefabs
                    LBObjPrefab group1_lbGroupMember1ObjPathPrefab1 = null;
                    group1_lbGroupMember1ObjPathPrefab1 = new LBObjPrefab();
                    group1_lbGroupMember1ObjPathPrefab1.groupMemberGUID = "4a284844-1282-418f-8ca1-9e38894ec61a";
                    group1_lbGroupMember1ObjPathSeries.mainObjPrefabList.Add(group1_lbGroupMember1ObjPathPrefab1);
                    group1_lbGroupMember1ObjPathPrefab1 = null;
                    // Series Start ObjPrefab
                    group1_lbGroupMember1ObjPathSeries.startObjPrefab = new LBObjPrefab();
                    group1_lbGroupMember1ObjPathSeries.startObjPrefab.groupMemberGUID = "";
                    // Series End ObjPrefab
                    group1_lbGroupMember1ObjPathSeries.endObjPrefab = new LBObjPrefab();
                    group1_lbGroupMember1ObjPathSeries.endObjPrefab.groupMemberGUID = "";
                    group1_lbGroupMember1ObjPathSeries.spacingDistance = 69.3f;
                    group1_lbGroupMember1ObjPathSeries.maxMainPrefabs = 5;
                    group1_lbGroupMember1ObjPathSeries.isLastObjSnappedToEnd = false;
                    group1_lbGroupMember1ObjPathSeries.sparcePlacementCutoff = 0.453f;
                    group1_lbGroupMember1ObjPathSeries.pathSpline = LBObjPath.PathSpline.RightEdge;
                    group1_lbGroupMember1ObjPathSeries.prefabOffsetZ = 7.5f;
                    group1_lbGroupMember1ObjPathSeries.prefabStartOffset = 55.9f;
                    group1_lbGroupMember1ObjPathSeries.prefabEndOffset = 0f;
                    group1_lbGroupMember1ObjPathSeries.use3DDistance = false;
                    group1_lbGroupMember1ObjPathSeries.useNonOffsetDistance = false;
                    group1_lbGroupMember1.lbObjPath.lbObjPathSeriesList.Add(group1_lbGroupMember1ObjPathSeries);
                    group1_lbGroupMember1ObjPathSeries = null;
                    #endregion Series

                    #endregion ObjPathSeries

                }
                #endregion End Object Path settings for [path group objpath 01]
            }
            lbGroup1.groupMemberList.Add(group1_lbGroupMember1);
            #endregion

            #region Group1 Member2 LB_PostWood01_LQ_512
            LBGroupMember group1_lbGroupMember2 = new LBGroupMember();
            if (group1_lbGroupMember2 != null)
            {
                group1_lbGroupMember2.isDisabled = false;
                group1_lbGroupMember2.showInEditor = false;
                group1_lbGroupMember2.showtabInEditor = 0;
                group1_lbGroupMember2.GUID = "f39d69fe-b08f-4cfe-bb11-fb8e8a3cbf35";
                group1_lbGroupMember2.lbMemberType = LBGroupMember.LBMemberType.Prefab;
                group1_lbGroupMember2.isGroupOverride = false;
                group1_lbGroupMember2.minScale = 1f;
                group1_lbGroupMember2.maxScale = 1f;
                group1_lbGroupMember2.minHeight = 0f;
                group1_lbGroupMember2.maxHeight = 1f;
                group1_lbGroupMember2.minInclination = 0f;
                group1_lbGroupMember2.maxInclination = 90f;
                group1_lbGroupMember2.prefab = Group001_Member002prefab;
                group1_lbGroupMember2.prefabName = "LB_PostWood01_LQ_512";
                group1_lbGroupMember2.showPrefabPreview = false;
                group1_lbGroupMember2.isKeepPrefabConnection = false;
                group1_lbGroupMember2.isCombineMesh = false;
                group1_lbGroupMember2.isRemoveEmptyGameObjects = true;
                group1_lbGroupMember2.isRemoveAnimator = true;
                group1_lbGroupMember2.isCreateCollider = false;
                group1_lbGroupMember2.maxPrefabSqrKm = 10;
                group1_lbGroupMember2.maxPrefabPerGroup = 10000;
                group1_lbGroupMember2.isPlacedInCentre = false;
                group1_lbGroupMember2.showXYZSettings = false;
                group1_lbGroupMember2.modelOffsetX = 0f;
                group1_lbGroupMember2.modelOffsetY = 0f;
                group1_lbGroupMember2.modelOffsetZ = 0f;
                group1_lbGroupMember2.minOffsetX = 0f;
                group1_lbGroupMember2.minOffsetZ = 0f;
                group1_lbGroupMember2.minOffsetY = 0f;
                group1_lbGroupMember2.maxOffsetY = 0f;
                group1_lbGroupMember2.randomiseOffsetY = false;
                group1_lbGroupMember2.rotationType = LBGroupMember.LBRotationType.WorldSpace;
                group1_lbGroupMember2.randomiseRotationY = false;
                group1_lbGroupMember2.startRotationY = 0f;
                group1_lbGroupMember2.endRotationY = 359.9f;
                group1_lbGroupMember2.randomiseRotationXZ = false;
                group1_lbGroupMember2.rotationX = 0f;
                group1_lbGroupMember2.rotationZ = 0f;
                group1_lbGroupMember2.endRotationX = 0f;
                group1_lbGroupMember2.endRotationZ = 0f;
                group1_lbGroupMember2.isLockTilt = false;
                group1_lbGroupMember2.useNoise = false;
                group1_lbGroupMember2.noiseOffset = 0f;
                group1_lbGroupMember2.noiseTileSize = 500f;
                group1_lbGroupMember2.noisePlacementCutoff = 0.65f;
                group1_lbGroupMember2.proximityExtent = 0.078f;
                group1_lbGroupMember2.removeGrassBlendDist = 0.5f;
                group1_lbGroupMember2.minGrassProximity = 0f;
                group1_lbGroupMember2.isRemoveTree = true;
                group1_lbGroupMember2.minTreeProximity = 10f;
                group1_lbGroupMember2.isTerrainAligned = false;
                group1_lbGroupMember2.isTerrainFlattened = false;
                group1_lbGroupMember2.flattenDistance = 2f;
                group1_lbGroupMember2.flattenHeightOffset = 0f;
                group1_lbGroupMember2.flattenBlendRate = 0.5f;

                // Start Member-Level Zones references for group1_lbGroupMember2

                // End Member-Level Zones references for group1_lbGroupMember2

                group1_lbGroupMember2.isZoneEdgeFillTop = false;
                group1_lbGroupMember2.isZoneEdgeFillBottom = false;
                group1_lbGroupMember2.isZoneEdgeFillLeft = false;
                group1_lbGroupMember2.isZoneEdgeFillRight = false;
                group1_lbGroupMember2.zoneEdgeFillDistance = 1f;
                group1_lbGroupMember2.isPathOnly = true;
                group1_lbGroupMember2.lbObjectOrientation = LBObjPath.LBObjectOrientation.PathSpace;
            }
            lbGroup1.groupMemberList.Add(group1_lbGroupMember2);
            #endregion

            #region Group1 Member3 Rock1
            LBGroupMember group1_lbGroupMember3 = new LBGroupMember();
            if (group1_lbGroupMember3 != null)
            {
                group1_lbGroupMember3.isDisabled = false;
                group1_lbGroupMember3.showInEditor = true;
                group1_lbGroupMember3.showtabInEditor = 1;
                group1_lbGroupMember3.GUID = "4a284844-1282-418f-8ca1-9e38894ec61a";
                group1_lbGroupMember3.lbMemberType = LBGroupMember.LBMemberType.Prefab;
                group1_lbGroupMember3.isGroupOverride = false;
                group1_lbGroupMember3.minScale = 1f;
                group1_lbGroupMember3.maxScale = 1f;
                group1_lbGroupMember3.minHeight = 0f;
                group1_lbGroupMember3.maxHeight = 1f;
                group1_lbGroupMember3.minInclination = 0f;
                group1_lbGroupMember3.maxInclination = 90f;
                group1_lbGroupMember3.prefab = Group001_Member003prefab;
                group1_lbGroupMember3.prefabName = "Rock1";
                group1_lbGroupMember3.showPrefabPreview = false;
                group1_lbGroupMember3.isKeepPrefabConnection = false;
                group1_lbGroupMember3.isCombineMesh = false;
                group1_lbGroupMember3.isRemoveEmptyGameObjects = true;
                group1_lbGroupMember3.isRemoveAnimator = true;
                group1_lbGroupMember3.isCreateCollider = false;
                group1_lbGroupMember3.maxPrefabSqrKm = 10;
                group1_lbGroupMember3.maxPrefabPerGroup = 10000;
                group1_lbGroupMember3.isPlacedInCentre = false;
                group1_lbGroupMember3.showXYZSettings = false;
                group1_lbGroupMember3.modelOffsetX = 0f;
                group1_lbGroupMember3.modelOffsetY = 0f;
                group1_lbGroupMember3.modelOffsetZ = 0f;
                group1_lbGroupMember3.minOffsetX = 0f;
                group1_lbGroupMember3.minOffsetZ = 0f;
                group1_lbGroupMember3.minOffsetY = -2.199f;
                group1_lbGroupMember3.maxOffsetY = -0.171f;
                group1_lbGroupMember3.randomiseOffsetY = true;
                group1_lbGroupMember3.rotationType = LBGroupMember.LBRotationType.WorldSpace;
                group1_lbGroupMember3.randomiseRotationY = true;
                group1_lbGroupMember3.startRotationY = 0f;
                group1_lbGroupMember3.endRotationY = 359.9f;
                group1_lbGroupMember3.randomiseRotationXZ = false;
                group1_lbGroupMember3.rotationX = 0f;
                group1_lbGroupMember3.rotationZ = 0f;
                group1_lbGroupMember3.isLockTilt = false;
                group1_lbGroupMember3.useNoise = false;
                group1_lbGroupMember3.noiseOffset = 0f;
                group1_lbGroupMember3.noiseTileSize = 500f;
                group1_lbGroupMember3.noisePlacementCutoff = 0.65f;
                group1_lbGroupMember3.proximityExtent = 1.464f;
                group1_lbGroupMember3.removeGrassBlendDist = 0.5f;
                group1_lbGroupMember3.minGrassProximity = 0f;
                group1_lbGroupMember3.isRemoveTree = true;
                group1_lbGroupMember3.minTreeProximity = 10f;
                group1_lbGroupMember3.isTerrainAligned = false;
                group1_lbGroupMember3.isTerrainFlattened = false;
                group1_lbGroupMember3.flattenDistance = 2f;
                group1_lbGroupMember3.flattenHeightOffset = 0f;
                group1_lbGroupMember3.flattenBlendRate = 0.5f;

                // Start Member-Level Zones references for group1_lbGroupMember3

                // End Member-Level Zones references for group1_lbGroupMember3

                group1_lbGroupMember3.isZoneEdgeFillTop = false;
                group1_lbGroupMember3.isZoneEdgeFillBottom = false;
                group1_lbGroupMember3.isZoneEdgeFillLeft = false;
                group1_lbGroupMember3.isZoneEdgeFillRight = false;
                group1_lbGroupMember3.zoneEdgeFillDistance = 1f;
                group1_lbGroupMember3.isPathOnly = true;
                group1_lbGroupMember3.lbObjectOrientation = LBObjPath.LBObjectOrientation.PathSpace;
            }
            lbGroup1.groupMemberList.Add(group1_lbGroupMember3);
            #endregion
            // End Group Members

            // NOTE Add the new Group to the landscape meta-data
            landscape.lbGroupList.Add(lbGroup1);
        }
        #endregion

        landscape.ApplyGroups(false, false);

        #region Camera Animator
        // Get the first Camera Animator in the scene, snap the camera path to the new terrain,
        // and start moving the camera along the camera path.
        LBCameraAnimator lbCameraAnimator = LBCameraAnimator.GetFirstCameraAnimatorInLandscape(landscape);
        if (lbCameraAnimator == null) { Debug.LogWarning("GetFirstCameraAnimatorInLandscape returned null"); }
        else
        {
            if (!string.IsNullOrEmpty(objPathGUID))
            {
                LBObjPath lbObjPath = LBGroup.GetObjectPath(landscape.lbGroupList, objPathGUID);

                if (lbObjPath != null)
                {
                    if (!lbCameraAnimator.cameraPath.ImportPathPoints(lbObjPath))
                    {
                        Debug.LogWarning("Could not make camera animator travel along new Group Object Path.");
                    }
                }
                else if (showErrors) { Debug.LogWarning("Could not make camera animator travel along new Group Object Path because the GUID supplied did not match an existing Group Object Path"); }
            }
            else if (showErrors) { Debug.LogWarning("Could not make camera animator travel along new Group Object Path because the GUID is set in the CameraPath script public variables"); }

            // Get the LBPath instance which contains the points along the camera path
            LBPath lbPath = lbCameraAnimator.cameraPath.lbPath;
            if (lbPath == null) { Debug.LogWarning("Could not find the camera path instance for the animator"); }
            else
            {
                // Optionally update the path points to match the terrain
                lbPath.heightAboveTerrain = 7f;
                lbPath.snapToTerrain = true;
                lbPath.RefreshPathHeights(landscape);

                // Start the camera moving from the start of the path.
                lbCameraAnimator.BeginAnimation(true, 0f);
            }
        }
        #endregion

        // Display the total time taken to generate the landscape (usually for debugging purposes)
        Debug.Log("Time taken to generate landscape: " + (Time.realtimeSinceStartup - generationStartTime).ToString("00.00") + " seconds.");
    }
}
