using System.Collections.Generic;
using UnityEngine;
using LandscapeBuilder;

/// <summary>
/// Example script that creates a landscape entirely at runtime.
/// Stencil, Topography, Texturing, and Groups code was creating using
/// the (S) scripting buttons from an existing landscape.
/// NOTE: Ensure Project Settings, Color Space = Linear.
/// There is no error checking for different sized terrains.
/// MANAUL STEPS REQUIRED
/// 1. In the Application.dataPath folder, create a Heightmaps folder. In the Editor the Heightmaps
///    folder should be created in the Project pane. For Standalone PC Builds, it should be added to
///    the [project]_Data folder.
/// 2. Create a duplicate of LandscapeBuilder/Modifiers/Mountains/DeathValleyDryMountain.raw
/// 3. Copy the .raw file into the new Heightmaps folder.
/// 4. Ensure the file in Heightmaps folder is the same name as the original.
/// </summary>
[RequireComponent(typeof(LBLandscape))]
public class RuntimeSample06 : MonoBehaviour
{
    #region Public Variables
    public Vector2 landscapeSize = Vector2.one * 1000f;
    public float terrainWidth = 500f;
    public float terrainHeight = 500f;
    public bool showErrors = false;

    public bool useGPUTopography = false;
    public bool useGPUTexturing = true;
    public bool useGPUGrass = true;
    public bool useGPUPath = false;

    // Stencil Layer input images
    public Texture2D lbStRTStencil6RTValleyFloor6Tex = null;
    public Texture2D lbStRTStencil6PathwayTex = null;
    public Texture2D lbStRTStencil6GrassLayerTex = null;

    [Header("ValleyWalk")]
    public Material meshMatpathValleyWalk;

    // Texturing inputs
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

    [Header("Grass1")]
    public Texture2D textureGrs1;
    public GameObject meshPrefabGrs1;
    public Texture2D mapGrs1;

    [Header("Group1")]
    public GameObject Group001_Member001prefab;

    [Header("Group2")]
    public GameObject Group002_Member002prefab;

    #endregion

    #region Private variables
    private LBLandscape landscape = null;
    #endregion

    // Use this for initialization
    void Awake ()
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
            Debug.LogWarning("Cannot find LBLandscape script attached to Runtime gameobject");
            return;
        }
        else if (landscape.IsGPUAccelerationAvailable())
        {
            landscape.useGPUGrass = useGPUGrass;
            landscape.useGPUTexturing = useGPUTexturing;

            // In this demo script we're not using GPU on topography or path
            // However, in your runtime script you could have it enabled.
            landscape.useGPUTopography = useGPUTopography;
            landscape.useGPUPath = useGPUPath;
        }
        else
        {
            #if UNITY_EDITOR
            if (useGPUTopography || useGPUTexturing || useGPUGrass || useGPUPath)
            {
                Debug.LogWarning("Sorry, your hardware does not support GPU acceleration");
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

        #region Create Terrains

        // Update the size
        landscape.size = landscapeSize;
        // Create the terrains
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
                // Set terrain settings (depending on your situtation, you may need to adjust the following values)
                newTerrain.heightmapPixelError = 5f;
                newTerrain.basemapDistance = 1000f;
                newTerrain.treeDistance = 750f;
                newTerrain.treeBillboardDistance = 100f;
                newTerrain.detailObjectDistance = 200f;
                newTerrain.treeCrossFadeLength = 5f;
                // Set terrain data settings (same as above comment)
                TerrainData newTerrainData = new TerrainData();
                newTerrainData.heightmapResolution = 513;
                newTerrainData.size = new Vector3(terrainWidth, terrainHeight, terrainWidth);
                newTerrainData.SetDetailResolution(1024, 16);
                newTerrain.terrainData = newTerrainData;

                #if UNITY_2018_3_OR_NEWER
                newTerrain.drawInstanced = false;  // Currently doesn't work on a new terrain at runtime
                newTerrain.groupingID = 100; // Default is 0
                newTerrain.allowAutoConnect = true;
                #endif

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

        // Stencil Code generated from Landscape Builder 2 at 7:59 AM on Tuesday, January 16, 2018
        // Assumes a LBLandscape object called landscape has already been defined.
        // Stencil Layer textures can be created with the SaveMaps feature in the Stencil tool.
        #region Stencil
        // Add a new Stencil to the (empty) landscape

        LBStencil lbStRTStencil6 = LBStencil.CreateStencilInScene(landscape, landscape.gameObject);
        if (lbStRTStencil6 != null)
        {
            lbStRTStencil6.stencilName = "RTStencil6";
            lbStRTStencil6.name = "RTStencil6";
            lbStRTStencil6.GUID = "4a2cf5b9-cf52-49d5-9e52-1836e724a747";
            /// Import PNG files into stencil layers (at runtime) and add as filters.
            lbStRTStencil6.layerImportMethod = LBStencil.LayerImportMethod.Alpha;
            // If importing LB Map Textures into Stencil Layers, set this to true
            lbStRTStencil6.isFlipImportTopBottom = true;

            // Remove the default first layer
            if (lbStRTStencil6.stencilLayerList.Count > 0) { lbStRTStencil6.stencilLayerList.RemoveAt(0); }

            // Start Stencil Layers
            if (lbStRTStencil6.ImportTexture("RTValleyFloor6", lbStRTStencil6RTValleyFloor6Tex, false))
            {
                int slIdx = lbStRTStencil6.stencilLayerList.Count - 1;
                lbStRTStencil6.stencilLayerList[slIdx].GUID = "ad3f6656-55b9-419a-9101-2f5cc61746e7";
                lbStRTStencil6.stencilLayerList[slIdx].layerResolution = LBStencil.LayerResolution._1024x1024;
                lbStRTStencil6.stencilLayerList[slIdx].colourInEditor = new Color(0.606f, 1f, 0.597f, 1f);
            }
            if (lbStRTStencil6.ImportTexture("Pathway", lbStRTStencil6PathwayTex, false))
            {
                int slIdx = lbStRTStencil6.stencilLayerList.Count - 1;
                lbStRTStencil6.stencilLayerList[slIdx].GUID = "f4fff2dd-9357-47fb-8bdb-8673f8ada58d";
                lbStRTStencil6.stencilLayerList[slIdx].layerResolution = LBStencil.LayerResolution._1024x1024;
                lbStRTStencil6.stencilLayerList[slIdx].colourInEditor = new Color(1f, 1f, 1f, 1f);
            }
            if (lbStRTStencil6.ImportTexture("GrassLayer", lbStRTStencil6GrassLayerTex, false))
            {
                int slIdx = lbStRTStencil6.stencilLayerList.Count - 1;
                lbStRTStencil6.stencilLayerList[slIdx].GUID = "97a5c286-6ed0-4f69-bdc3-f430a4d03a68";
                lbStRTStencil6.stencilLayerList[slIdx].layerResolution = LBStencil.LayerResolution._512x512;
                lbStRTStencil6.stencilLayerList[slIdx].colourInEditor = new Color(0f, 1f, 0f, 1f);
            }
            // End Stencil Layers
        }
        #endregion

        // If you have used the LB Editor to first build a test path in a landscape, the path points can be scripted
        // out and pasted into your runtime script
        #region lbMapPathValleyWalk - place before Topography
        // Valley Walk generated from Landscape Builder 2 at 12:34 PM on Thursday, January 18, 2018
        LBMapPath lbMapPathValleyWalk = LBMapPath.CreateMapPath(landscape, landscape.gameObject);
        if (lbMapPathValleyWalk != null)
        {
            lbMapPathValleyWalk.name = "Valley Walk";
            if (lbMapPathValleyWalk.lbPath != null)
            {
                lbMapPathValleyWalk.lbPath.pathName = "Valley Walk";
                lbMapPathValleyWalk.lbPath.showPathInScene = false;
                lbMapPathValleyWalk.mapResolution = 1;
                lbMapPathValleyWalk.lbPath.blendStart = true;
                lbMapPathValleyWalk.lbPath.blendEnd = true;
                lbMapPathValleyWalk.lbPath.pathResolution = 2f;
                lbMapPathValleyWalk.lbPath.closedCircuit = false;
                lbMapPathValleyWalk.lbPath.edgeBlendWidth = 2f;
                lbMapPathValleyWalk.lbPath.removeCentre = false;
                lbMapPathValleyWalk.lbPath.leftBorderWidth = 0f;
                lbMapPathValleyWalk.lbPath.rightBorderWidth = 0f;
                lbMapPathValleyWalk.lbPath.snapToTerrain = true;
                lbMapPathValleyWalk.lbPath.heightAboveTerrain = 1f;
                lbMapPathValleyWalk.lbPath.zoomOnFind = true;
                lbMapPathValleyWalk.lbPath.findZoomDistance = 100f;
                // Mesh options
                lbMapPathValleyWalk.lbPath.isMeshLandscapeUV = false;
                lbMapPathValleyWalk.lbPath.meshUVTileScale = new Vector2(1, 1);
                lbMapPathValleyWalk.lbPath.meshYOffset = 0.05f;
                lbMapPathValleyWalk.lbPath.meshEdgeSnapToTerrain = true;
                lbMapPathValleyWalk.lbPath.meshSnapType = LBPath.MeshSnapType.BothEdges;
                lbMapPathValleyWalk.lbPath.meshIsDoubleSided = false;
                lbMapPathValleyWalk.lbPath.meshIncludeEdges = false;
                lbMapPathValleyWalk.lbPath.meshIncludeWater = false;
                lbMapPathValleyWalk.lbPath.meshSaveToProjectFolder = false;
                lbMapPathValleyWalk.meshMaterial = meshMatpathValleyWalk;
                // Path Points
                lbMapPathValleyWalk.lbPath.positionList.Add(new Vector3(754.1235f, 101.7484f, 58.17023f));
                lbMapPathValleyWalk.lbPath.positionList.Add(new Vector3(751.0826f, 101.1902f, 139.7238f));
                lbMapPathValleyWalk.lbPath.positionList.Add(new Vector3(749.0591f, 102.6063f, 201.8718f));
                lbMapPathValleyWalk.lbPath.positionList.Add(new Vector3(749.5621f, 103.5861f, 241.327f));
                lbMapPathValleyWalk.lbPath.positionList.Add(new Vector3(750.9412f, 105.1134f, 269.1842f));
                lbMapPathValleyWalk.lbPath.positionList.Add(new Vector3(762.8666f, 109.1425f, 363.3736f));
                lbMapPathValleyWalk.lbPath.positionList.Add(new Vector3(788.0256f, 111.6812f, 425.491f));
                lbMapPathValleyWalk.lbPath.positionList.Add(new Vector3(856.0757f, 114.8563f, 505.8546f));
                lbMapPathValleyWalk.lbPath.positionList.Add(new Vector3(944.9757f, 123.8846f, 594.6475f));
                lbMapPathValleyWalk.lbPath.widthList.Add(14f);
                lbMapPathValleyWalk.lbPath.widthList.Add(14f);
                lbMapPathValleyWalk.lbPath.widthList.Add(14f);
                lbMapPathValleyWalk.lbPath.widthList.Add(14f);
                lbMapPathValleyWalk.lbPath.widthList.Add(14f);
                lbMapPathValleyWalk.lbPath.widthList.Add(14f);
                lbMapPathValleyWalk.lbPath.widthList.Add(14f);
                lbMapPathValleyWalk.lbPath.widthList.Add(14f);
                lbMapPathValleyWalk.lbPath.widthList.Add(14f);
                lbMapPathValleyWalk.lbPath.positionListLeftEdge.Add(new Vector3(747.1285f, 101.7484f, 57.9088f));
                lbMapPathValleyWalk.lbPath.positionListLeftEdge.Add(new Vector3(744.0872f, 101.1902f, 139.4762f));
                lbMapPathValleyWalk.lbPath.positionListLeftEdge.Add(new Vector3(742.0618f, 102.6063f, 201.7714f));
                lbMapPathValleyWalk.lbPath.positionListLeftEdge.Add(new Vector3(742.5693f, 103.5861f, 241.4977f));
                lbMapPathValleyWalk.lbPath.positionListLeftEdge.Add(new Vector3(743.988f, 105.1134f, 269.9245f));
                lbMapPathValleyWalk.lbPath.positionListLeftEdge.Add(new Vector3(756.058f, 109.1425f, 364.9739f));
                lbMapPathValleyWalk.lbPath.positionListLeftEdge.Add(new Vector3(782.1679f, 111.6812f, 429.3163f));
                lbMapPathValleyWalk.lbPath.positionListLeftEdge.Add(new Vector3(850.955f, 114.8563f, 510.6129f));
                lbMapPathValleyWalk.lbPath.positionListLeftEdge.Add(new Vector3(940.0475f, 123.8846f, 599.593f));
                lbMapPathValleyWalk.lbPath.positionListRightEdge.Add(new Vector3(761.1184f, 101.7484f, 58.43166f));
                lbMapPathValleyWalk.lbPath.positionListRightEdge.Add(new Vector3(758.0781f, 101.1902f, 139.9713f));
                lbMapPathValleyWalk.lbPath.positionListRightEdge.Add(new Vector3(756.0564f, 102.6063f, 201.9722f));
                lbMapPathValleyWalk.lbPath.positionListRightEdge.Add(new Vector3(756.555f, 103.5861f, 241.1563f));
                lbMapPathValleyWalk.lbPath.positionListRightEdge.Add(new Vector3(757.8945f, 105.1134f, 268.4438f));
                lbMapPathValleyWalk.lbPath.positionListRightEdge.Add(new Vector3(769.6752f, 109.1425f, 361.7733f));
                lbMapPathValleyWalk.lbPath.positionListRightEdge.Add(new Vector3(793.8832f, 111.6812f, 421.6656f));
                lbMapPathValleyWalk.lbPath.positionListRightEdge.Add(new Vector3(861.1965f, 114.8563f, 501.0962f));
                lbMapPathValleyWalk.lbPath.positionListRightEdge.Add(new Vector3(949.9039f, 123.8846f, 589.702f));
                lbMapPathValleyWalk.minPathWidth = lbMapPathValleyWalk.lbPath.GetMinWidth();
            }
        }
        #endregion

        // Layer Code generated from Landscape Builder at 8:28 AM on Tuesday, January 16, 2018
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
            lbBaseLayer1.additiveAmount = 0f;
            lbBaseLayer1.subtractiveAmount = 0.2f;
            lbBaseLayer1.additiveCurve = additiveCurveLyr1;
            lbBaseLayer1.subtractiveCurve = subtractiveCurveLyr1;
            lbBaseLayer1.removeBaseNoise = true;
            lbBaseLayer1.addMinHeight = false;
            lbBaseLayer1.addHeight = 0f;
            lbBaseLayer1.restrictArea = false;
            lbBaseLayer1.areaRect = new Rect(1000, 1000, 2001, 2001);
            lbBaseLayer1.interpolationSmoothing = 0;
            // lbBaseLayer1.heightmapImage = heightmapImageLyr1;
            lbBaseLayer1.imageHeightScale = 0.25f;
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
            lbBaseLayer1.floorOffsetY = 0f;
            lbBaseLayer1.mapPathBlendCurve = mapPathBlendCurveLyr1;
            lbBaseLayer1.mapPathHeightCurve = mapPathHeightCurveLyr1;
            lbBaseLayer1.mapPathAddInvert = false;
            // Add LBMapPath code here
            // lbBaseLayer1.lbPath = lbPath; 
            // lbBaseLayer1.lbMapPath = lbMapPath; 

            // MANAUL STEPS REQUIRED
            // 1. In the Application.dataPath folder create a Heightmaps folder.
            // 2. If using a LB modifier, create a duplicate of the .raw file.
            // 3. Copy the .raw file into the new Heightmaps folder.
            // 4. Ensure the file in Heightmaps folder is the same name as the original.
            LBRaw lbRawLyr1 = LBRaw.ImportHeightmapRAW(Application.dataPath + "/Heightmaps/DeathValleyDryMountain.RAW", false, false);
            if (lbRawLyr1 != null)
            {
                lbBaseLayer1.modifierRAWFile = lbRawLyr1;
            }
            lbBaseLayer1.modifierMode = LBLayer.LayerModifierMode.Set;
            lbBaseLayer1.modifierAddInvert = false;
            lbBaseLayer1.modifierUseBlending = false;
            lbBaseLayer1.areaRectRotation = 0f;
            lbBaseLayer1.modifierLandformCategory = LBModifierOperations.ModifierLandformCategory.Mountains;
            lbBaseLayer1.modifierSourceFileType = LBRaw.SourceFileType.RAW;
            // Currently runtime does not support water with Modifier Layers - contact support or post in our Unity forum if you need this feature
            lbBaseLayer1.modifierUseWater = false;
            lbBaseLayer1.modifierWaterIsMeshLandscapeUV = false;
            lbBaseLayer1.modifierWaterMeshUVTileScale = new Vector2(1f, 1f);
            // NOTE Add the new layer to the landscape meta-data
            landscape.topographyLayersList.Add(lbBaseLayer1);
        }
        #endregion

        #region Topography Layer2
        AnimationCurve additiveCurveLyr2 = new AnimationCurve();

        AnimationCurve subtractiveCurveLyr2 = new AnimationCurve();

        List<LBLayerFilter> filtersLyr2 = new List<LBLayerFilter>();
        // Add LayerFilter code here
        List<AnimationCurve> imageCurveModifiersLyr2 = new List<AnimationCurve>();

        List<AnimationCurve> outputCurveModifiersLyr2 = new List<AnimationCurve>();

        List<LBCurve.CurvePreset> outputCurveModifierPresetsLyr2 = new List<LBCurve.CurvePreset>();

        List<AnimationCurve> perOctaveCurveModifiersLyr2 = new List<AnimationCurve>();
        AnimationCurve perOctaveCurveLyr21 = new AnimationCurve();
        perOctaveCurveLyr21.AddKey(0.00f, 0.50f);
        perOctaveCurveLyr21.AddKey(0.05f, 0.40f);
        perOctaveCurveLyr21.AddKey(0.20f, 0.10f);
        perOctaveCurveLyr21.AddKey(0.25f, 0.00f);
        perOctaveCurveLyr21.AddKey(0.30f, 0.20f);
        perOctaveCurveLyr21.AddKey(0.45f, 0.80f);
        perOctaveCurveLyr21.AddKey(0.50f, 1.00f);
        perOctaveCurveLyr21.AddKey(0.55f, 0.80f);
        perOctaveCurveLyr21.AddKey(0.70f, 0.20f);
        perOctaveCurveLyr21.AddKey(0.75f, 0.00f);
        perOctaveCurveLyr21.AddKey(0.80f, 0.10f);
        perOctaveCurveLyr21.AddKey(0.95f, 0.40f);
        perOctaveCurveLyr21.AddKey(1.00f, 0.50f);
        Keyframe[] perOctaveCurveLyr21Keys = perOctaveCurveLyr21.keys;
        perOctaveCurveLyr21Keys[0].inTangent = 0.00f;
        perOctaveCurveLyr21Keys[0].outTangent = 0.00f;
        perOctaveCurveLyr21Keys[1].inTangent = -2.00f;
        perOctaveCurveLyr21Keys[1].outTangent = -2.00f;
        perOctaveCurveLyr21Keys[2].inTangent = -2.00f;
        perOctaveCurveLyr21Keys[2].outTangent = -2.00f;
        perOctaveCurveLyr21Keys[3].inTangent = 0.00f;
        perOctaveCurveLyr21Keys[3].outTangent = 0.00f;
        perOctaveCurveLyr21Keys[4].inTangent = 4.00f;
        perOctaveCurveLyr21Keys[4].outTangent = 4.00f;
        perOctaveCurveLyr21Keys[5].inTangent = 4.00f;
        perOctaveCurveLyr21Keys[5].outTangent = 4.00f;
        perOctaveCurveLyr21Keys[6].inTangent = 0.00f;
        perOctaveCurveLyr21Keys[6].outTangent = 0.00f;
        perOctaveCurveLyr21Keys[7].inTangent = -4.00f;
        perOctaveCurveLyr21Keys[7].outTangent = -4.00f;
        perOctaveCurveLyr21Keys[8].inTangent = -4.00f;
        perOctaveCurveLyr21Keys[8].outTangent = -4.00f;
        perOctaveCurveLyr21Keys[9].inTangent = 0.00f;
        perOctaveCurveLyr21Keys[9].outTangent = 0.00f;
        perOctaveCurveLyr21Keys[10].inTangent = 2.00f;
        perOctaveCurveLyr21Keys[10].outTangent = 2.00f;
        perOctaveCurveLyr21Keys[11].inTangent = 2.00f;
        perOctaveCurveLyr21Keys[11].outTangent = 2.00f;
        perOctaveCurveLyr21Keys[12].inTangent = 0.00f;
        perOctaveCurveLyr21Keys[12].outTangent = 0.00f;
        perOctaveCurveLyr21 = new AnimationCurve(perOctaveCurveLyr21Keys);

        perOctaveCurveModifiersLyr2.Add(perOctaveCurveLyr21);

        List<LBCurve.CurvePreset> perOctaveCurveModifierPresetsLyr2 = new List<LBCurve.CurvePreset>();

        AnimationCurve mapPathBlendCurveLyr2 = new AnimationCurve();
        mapPathBlendCurveLyr2.AddKey(0.00f, 0.00f);
        mapPathBlendCurveLyr2.AddKey(0.05f, 1.00f);
        mapPathBlendCurveLyr2.AddKey(1.00f, 1.00f);
        Keyframe[] mapPathBlendCurveLyr2Keys = mapPathBlendCurveLyr2.keys;
        mapPathBlendCurveLyr2Keys[0].inTangent = 21.60f;
        mapPathBlendCurveLyr2Keys[0].outTangent = 21.60f;
        mapPathBlendCurveLyr2Keys[1].inTangent = -0.06f;
        mapPathBlendCurveLyr2Keys[1].outTangent = -0.06f;
        mapPathBlendCurveLyr2Keys[2].inTangent = 0.00f;
        mapPathBlendCurveLyr2Keys[2].outTangent = 0.00f;
        mapPathBlendCurveLyr2 = new AnimationCurve(mapPathBlendCurveLyr2Keys);

        AnimationCurve mapPathHeightCurveLyr2 = new AnimationCurve();
        mapPathHeightCurveLyr2.AddKey(0.00f, 1.00f);
        mapPathHeightCurveLyr2.AddKey(1.00f, 1.00f);
        Keyframe[] mapPathHeightCurveLyr2Keys = mapPathHeightCurveLyr2.keys;
        mapPathHeightCurveLyr2Keys[0].inTangent = 0.00f;
        mapPathHeightCurveLyr2Keys[0].outTangent = 0.00f;
        mapPathHeightCurveLyr2Keys[1].inTangent = 0.00f;
        mapPathHeightCurveLyr2Keys[1].outTangent = 0.00f;
        mapPathHeightCurveLyr2 = new AnimationCurve(mapPathHeightCurveLyr2Keys);

        LBLayer lbBaseLayer2 = new LBLayer();
        if (lbBaseLayer2 != null)
        {
            lbBaseLayer2.type = LBLayer.LayerType.MapPath;
            lbBaseLayer2.preset = LBLayer.LayerPreset.MountainRangeBase;
            lbBaseLayer2.layerTypeMode = LBLayer.LayerTypeMode.Flatten;
            lbBaseLayer2.noiseTileSize = 5000;
            lbBaseLayer2.noiseOffsetX = 0;
            lbBaseLayer2.noiseOffsetZ = 0;
            lbBaseLayer2.octaves = 3;
            lbBaseLayer2.downscaling = 1;
            lbBaseLayer2.lacunarity = 1.95f;
            lbBaseLayer2.gain = 0.4f;
            lbBaseLayer2.additiveAmount = 0.5f;
            lbBaseLayer2.subtractiveAmount = 0.2f;
            lbBaseLayer2.additiveCurve = additiveCurveLyr2;
            lbBaseLayer2.subtractiveCurve = subtractiveCurveLyr2;
            lbBaseLayer2.removeBaseNoise = true;
            lbBaseLayer2.addMinHeight = false;
            lbBaseLayer2.addHeight = 0f;
            lbBaseLayer2.restrictArea = false;
            lbBaseLayer2.areaRect = new Rect(0, 0, 1000, 1000);
            lbBaseLayer2.interpolationSmoothing = 0;
            // lbBaseLayer2.heightmapImage = heightmapImageLyr2;
            lbBaseLayer2.imageHeightScale = 1f;
            lbBaseLayer2.imageCurveModifiers = imageCurveModifiersLyr2;
            lbBaseLayer2.filters = filtersLyr2;
            lbBaseLayer2.isDisabled = false;
            lbBaseLayer2.showLayer = true;
            lbBaseLayer2.showAdvancedSettings = false;
            lbBaseLayer2.showCurvesAndFilters = false;
            lbBaseLayer2.showAreaHighlighter = false;
            lbBaseLayer2.detailSmoothRate = 0f;
            lbBaseLayer2.areaBlendRate = 0.5f;
            lbBaseLayer2.downscaling = 1;
            lbBaseLayer2.warpAmount = 0f;
            lbBaseLayer2.warpOctaves = 1;
            lbBaseLayer2.outputCurveModifiers = outputCurveModifiersLyr2;
            lbBaseLayer2.outputCurveModifierPresets = outputCurveModifierPresetsLyr2;
            lbBaseLayer2.perOctaveCurveModifiers = perOctaveCurveModifiersLyr2;
            lbBaseLayer2.perOctaveCurveModifierPresets = perOctaveCurveModifierPresetsLyr2;
            lbBaseLayer2.heightScale = 1E-06f;
            lbBaseLayer2.minHeight = 0f;
            lbBaseLayer2.imageSource = LBLayer.LayerImageSource.Default;
            lbBaseLayer2.imageRepairHoles = false;
            lbBaseLayer2.threshholdRepairHoles = 0f;
            lbBaseLayer2.floorOffsetY = 0f;
            lbBaseLayer2.mapPathBlendCurve = mapPathBlendCurveLyr2;
            lbBaseLayer2.mapPathHeightCurve = mapPathHeightCurveLyr2;
            lbBaseLayer2.mapPathAddInvert = false;
            lbBaseLayer2.lbPath = lbMapPathValleyWalk.lbPath;
            lbBaseLayer2.lbMapPath = lbMapPathValleyWalk;
            lbBaseLayer2.modifierMode = LBLayer.LayerModifierMode.Set;
            lbBaseLayer2.modifierAddInvert = false;
            lbBaseLayer2.modifierUseBlending = false;
            lbBaseLayer2.areaRectRotation = 0f;
            lbBaseLayer2.modifierLandformCategory = LBModifierOperations.ModifierLandformCategory.Custom;
            lbBaseLayer2.modifierSourceFileType = LBRaw.SourceFileType.RAW;
            // Currently runtime does not support water with Modifier Layers - contact support or post in our Unity forum if you need this feature 
            lbBaseLayer2.modifierUseWater = false;
            lbBaseLayer2.modifierWaterIsMeshLandscapeUV = false;
            lbBaseLayer2.modifierWaterMeshUVTileScale = new Vector2(1f, 1f);
            // NOTE Add the new layer to the landscape meta-data
            landscape.topographyLayersList.Add(lbBaseLayer2);
        }
        #endregion

        // Create the terrain topographies
        landscape.ApplyTopography(false, showErrors);

        #region lbMapPathValleyWalk - place after Topography generation
        if (lbMapPathValleyWalk != null)
        {
            lbMapPathValleyWalk.lbPath.RefreshPathHeights(landscape);
            // Create Mesh for Path
            if (lbMapPathValleyWalk.lbPath.CreateMeshFromPath(landscape))
            {
                Vector3 meshPosition = new Vector3(0f, lbMapPathValleyWalk.lbPath.meshYOffset, 0f);
                Transform meshTransform = LBMeshOperations.AddMeshToScene(lbMapPathValleyWalk.lbPath.lbMesh, meshPosition, lbMapPathValleyWalk.lbPath.pathName + " Mesh", lbMapPathValleyWalk.transform, lbMapPathValleyWalk.meshMaterial, true, false);
                if (meshTransform != null) { }
            }
        }
        #endregion

        // Texture Code generated from Landscape Builder 2 at 8:51 AM on Tuesday, January 16, 2018
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
            lbTerrainTexture1.maxHeight = 1f;
            lbTerrainTexture1.minInclination = 0f;
            lbTerrainTexture1.maxInclination = 30f;
            lbTerrainTexture1.strength = 0.01f;
            lbTerrainTexture1.texturingMode = LBTerrainTexture.TexturingMode.Height;
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
            lbTerrainTexture1.GUID = "e6af109a-3b4d-4453-9098-f4059dfe601a";
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
            lbTerrainTexture2.minHeight = 0f;
            lbTerrainTexture2.maxHeight = 0.3f;
            lbTerrainTexture2.minInclination = 0f;
            lbTerrainTexture2.maxInclination = 30f;
            lbTerrainTexture2.strength = 1f;
            lbTerrainTexture2.texturingMode = LBTerrainTexture.TexturingMode.HeightAndInclination;
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
            lbTerrainTexture2.showTexture = true;
            lbTerrainTexture2.noiseOffset = 41.59176f;
            lbTerrainTexture2.GUID = "481aea37-2abf-4b90-8ccc-a31c7ee389ac";
            lbTerrainTexture2.filterList = new List<LBTextureFilter>();
            LBTextureFilter lbFilterTex2 = null;
            lbFilterTex2 = LBTextureFilter.CreateFilter("4a2cf5b9-cf52-49d5-9e52-1836e724a747", "ad3f6656-55b9-419a-9101-2f5cc61746e7", false);
            if (lbFilterTex2 != null)
            {
                lbFilterTex2.filterMode = LBTextureFilter.FilterMode.AND;
                lbTerrainTexture2.filterList.Add(lbFilterTex2);
            }

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
            lbTerrainTexture3.showTexture = true;
            lbTerrainTexture3.noiseOffset = 32.63931f;
            lbTerrainTexture3.GUID = "ced2b8a9-0185-445a-8c25-59a3f1043599";
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
            lbTerrainTexture4.textureName = "GoodDirt";
            lbTerrainTexture4.normalMapName = "GoodDirtNM";
            lbTerrainTexture4.tileSize = new Vector2(25, 25);
            lbTerrainTexture4.smoothness = 0f;
            lbTerrainTexture4.metallic = 0f;
            lbTerrainTexture4.minHeight = 0.25f;
            lbTerrainTexture4.maxHeight = 0.75f;
            lbTerrainTexture4.minInclination = 0f;
            lbTerrainTexture4.maxInclination = 30f;
            lbTerrainTexture4.strength = 0.5f;
            lbTerrainTexture4.texturingMode = LBTerrainTexture.TexturingMode.ConstantInfluence;
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
            lbTerrainTexture4.GUID = "7723477c-76c5-4cd4-8eca-6e70ff0df0af";
            lbTerrainTexture4.filterList = new List<LBTextureFilter>();
            LBTextureFilter lbFilterTex4 = null;
            lbFilterTex4 = LBTextureFilter.CreateFilter("4a2cf5b9-cf52-49d5-9e52-1836e724a747", "f4fff2dd-9357-47fb-8bdb-8673f8ada58d", false);
            if (lbFilterTex4 != null)
            {
                lbFilterTex4.filterMode = LBTextureFilter.FilterMode.AND;
                lbTerrainTexture4.filterList.Add(lbFilterTex4);
            }

            lbTerrainTexture4.lbTerrainDataList = null;
            // NOTE Add the new Texture to the landscape meta-data
            landscape.terrainTexturesList.Add(lbTerrainTexture4);
        }
        #endregion

        // Texture the terrains
        landscape.ApplyTextures(true, showErrors);

        // Tree Code generated from Landscape Builder 2 at 10:56 AM on Tuesday, January 16, 2018
        #region LBTerrainTree1
        LBTerrainTree lbTerrainTree1 = new LBTerrainTree();
        if (lbTerrainTree1 != null)
        {
            lbTerrainTree1.maxTreesPerSqrKm = 250;
            lbTerrainTree1.bendFactor = 0.5f;
            lbTerrainTree1.prefab = prefabTree1;
            lbTerrainTree1.minScale = 0.7f;
            lbTerrainTree1.maxScale = 1f;
            lbTerrainTree1.treeScalingMode = LBTerrainTree.TreeScalingMode.RandomScaling;
            lbTerrainTree1.lockWidthToHeight = false;
            lbTerrainTree1.minProximity = 5f;
            lbTerrainTree1.minHeight = 0.5f;
            lbTerrainTree1.maxHeight = 0.75f;
            lbTerrainTree1.minInclination = 0f;
            lbTerrainTree1.maxInclination = 30f;
            lbTerrainTree1.treePlacingMode = LBTerrainTree.TreePlacingMode.Inclination;
            lbTerrainTree1.map = mapTree1;
            lbTerrainTree1.mapColour = new Color(1f, 0.9215686f, 0.01568628f, 1f);
            lbTerrainTree1.mapTolerance = 1;
            lbTerrainTree1.mapInverse = false;
            lbTerrainTree1.useNoise = false;
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
            lbTerrainTree1.filterList = new List<LBFilter>();
            LBFilter lbFilterTree1 = null;
            lbFilterTree1 = LBFilter.CreateFilter("4a2cf5b9-cf52-49d5-9e52-1836e724a747", "ad3f6656-55b9-419a-9101-2f5cc61746e7", false);
            if (lbFilterTree1 != null)
            {
                lbFilterTree1.filterMode = LBFilter.FilterMode.AND;
                lbTerrainTree1.filterList.Add(lbFilterTree1);
            }

            lbFilterTree1 = LBFilter.CreateFilter("4a2cf5b9-cf52-49d5-9e52-1836e724a747", "f4fff2dd-9357-47fb-8bdb-8673f8ada58d", false);
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

        // Paste code generated from Grass Tab items here
        #region LBTerrainGrass1
        LBTerrainGrass lbTerrainGrass1 = new LBTerrainGrass();
        if (lbTerrainGrass1 != null)
        {
            lbTerrainGrass1.texture = textureGrs1;
            lbTerrainGrass1.textureName = "GrassFrond01AlbedoAlpha";
            lbTerrainGrass1.minHeight = 0.5f;
            lbTerrainGrass1.maxHeight = 1f;
            lbTerrainGrass1.minWidth = 0.5f;
            lbTerrainGrass1.maxWidth = 1f;
            lbTerrainGrass1.healthyColour = new Color(0.2627451f, 0.9764706f, 0.1647059f, 1f);
            lbTerrainGrass1.dryColour = new Color(0.8039216f, 0.7372549f, 0.1019608f, 1f);
            lbTerrainGrass1.noiseSpread = 0.1f;
            lbTerrainGrass1.minPopulatedHeight = 0.5f;
            lbTerrainGrass1.maxPopulatedHeight = 1f;
            lbTerrainGrass1.minInclination = 0f;
            lbTerrainGrass1.maxInclination = 20f;
            lbTerrainGrass1.influence = 0.5f;
            lbTerrainGrass1.minDensity = 0;
            lbTerrainGrass1.density = 5;
            lbTerrainGrass1.detailRenderMode = DetailRenderMode.GrassBillboard;
            lbTerrainGrass1.grassPlacingMode = LBTerrainGrass.GrassPlacingMode.Inclination;
            lbTerrainGrass1.grassPatchFadingMode = LBTerrainGrass.GrassPatchFadingMode.DontFade;
            lbTerrainGrass1.map = mapGrs1;
            lbTerrainGrass1.mapColour = new Color(0f, 1f, 0f, 1f);
            lbTerrainGrass1.mapTolerance = 1;
            lbTerrainGrass1.mapInverse = false;
            lbTerrainGrass1.mapToleranceBlendCurve = LBMap.GetDefaultToleranceBlendCurve;
            lbTerrainGrass1.mapIsPath = false;
            lbTerrainGrass1.isDisabled = false;
            lbTerrainGrass1.showGrass = true;
            lbTerrainGrass1.useNoise = true;
            lbTerrainGrass1.noiseTileSize = 5f;
            lbTerrainGrass1.grassPlacementCutoff = 0.5f;
            lbTerrainGrass1.noiseOctaves = 5;
            lbTerrainGrass1.useMeshPrefab = false;
            lbTerrainGrass1.meshPrefab = meshPrefabGrs1;
            lbTerrainGrass1.meshPrefabName = "";
            lbTerrainGrass1.GUID = "3eda92ae-f491-49c1-bf48-8e56c38b3efc";
            lbTerrainGrass1.filterList = new List<LBFilter>();
            LBFilter lbFilterGrs1 = null;
            lbFilterGrs1 = LBFilter.CreateFilter("4a2cf5b9-cf52-49d5-9e52-1836e724a747", "97a5c286-6ed0-4f69-bdc3-f430a4d03a68", false);
            if (lbFilterGrs1 != null)
            {
                lbFilterGrs1.filterMode = LBFilter.FilterMode.AND;
                lbTerrainGrass1.filterList.Add(lbFilterGrs1);
            }

            lbTerrainGrass1.lbTerrainDataList = null;
            // NOTE Add the new Grass to the landscape meta-data
            landscape.terrainGrassList.Add(lbTerrainGrass1);
        }
        #endregion

        // Add Grass to the terrains
        landscape.ApplyGrass(true, true);

        // Paste code generated from Groups Tab items here
        #region Group1 [Rocks Group]
        LBGroup lbGroup1 = new LBGroup();
        if (lbGroup1 != null)
        {
            #region Group1-level variables
            lbGroup1.groupName = "Rocks Group";
            lbGroup1.lbGroupType = LBGroup.LBGroupType.Uniform;
            lbGroup1.maxGroupSqrKm = 10;
            lbGroup1.isDisabled = false;
            lbGroup1.showInEditor = false;
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
            LBFilter lbFilterGrp1 = null;
            lbFilterGrp1 = LBFilter.CreateFilter("4a2cf5b9-cf52-49d5-9e52-1836e724a747", "97a5c286-6ed0-4f69-bdc3-f430a4d03a68", false);
            if (lbFilterGrp1 != null)
            {
                lbFilterGrp1.filterMode = LBFilter.FilterMode.AND;
                lbGroup1.filterList.Add(lbFilterGrp1);
            }

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
            #region Group1 Member1 Rock1
            LBGroupMember group1_lbGroupMember1 = new LBGroupMember();
            if (group1_lbGroupMember1 != null)
            {
                group1_lbGroupMember1.isDisabled = false;
                group1_lbGroupMember1.showInEditor = true;
                group1_lbGroupMember1.showtabInEditor = 0;
                group1_lbGroupMember1.GUID = "db5640cf-64a5-4047-8c9f-1808fb20f2e7";
                group1_lbGroupMember1.lbMemberType = LBGroupMember.LBMemberType.Prefab;
                group1_lbGroupMember1.isGroupOverride = false;
                group1_lbGroupMember1.minScale = 1f;
                group1_lbGroupMember1.maxScale = 1f;
                group1_lbGroupMember1.minHeight = 0f;
                group1_lbGroupMember1.maxHeight = 1f;
                group1_lbGroupMember1.minInclination = 0f;
                group1_lbGroupMember1.maxInclination = 90f;
                group1_lbGroupMember1.prefab = Group001_Member001prefab;
                group1_lbGroupMember1.prefabName = "Rock1";
                group1_lbGroupMember1.showPrefabPreview = false;
                group1_lbGroupMember1.isKeepPrefabConnection = false;
                group1_lbGroupMember1.isCombineMesh = false;
                group1_lbGroupMember1.isRemoveEmptyGameObjects = true;
                group1_lbGroupMember1.isRemoveAnimator = true;
                group1_lbGroupMember1.isCreateCollider = false;
                group1_lbGroupMember1.maxPrefabSqrKm = 24;
                group1_lbGroupMember1.maxPrefabPerGroup = 10000;
                group1_lbGroupMember1.isPlacedInCentre = false;
                group1_lbGroupMember1.showXYZSettings = false;
                group1_lbGroupMember1.modelOffsetX = 0f;
                group1_lbGroupMember1.modelOffsetY = 0f;
                group1_lbGroupMember1.modelOffsetZ = 0f;
                group1_lbGroupMember1.minOffsetX = 0f;
                group1_lbGroupMember1.minOffsetZ = 0f;
                group1_lbGroupMember1.minOffsetY = -2f;
                group1_lbGroupMember1.maxOffsetY = -1f;
                group1_lbGroupMember1.randomiseOffsetY = true;
                group1_lbGroupMember1.rotationType = LBGroupMember.LBRotationType.WorldSpace;
                group1_lbGroupMember1.randomiseRotationY = true;
                group1_lbGroupMember1.startRotationY = 0f;
                group1_lbGroupMember1.endRotationY = 359.9f;
                group1_lbGroupMember1.randomiseRotationXZ = false;
                group1_lbGroupMember1.rotationX = 0f;
                group1_lbGroupMember1.rotationZ = 0f;
                group1_lbGroupMember1.endRotationX = 0f;
                group1_lbGroupMember1.endRotationZ = 0f;
                group1_lbGroupMember1.useNoise = false;
                group1_lbGroupMember1.noiseOffset = 272.3504f;
                group1_lbGroupMember1.noiseTileSize = 500f;
                group1_lbGroupMember1.noisePlacementCutoff = 0.65f;
                group1_lbGroupMember1.proximityExtent = 10f;
                group1_lbGroupMember1.removeGrassBlendDist = 0.5f;
                group1_lbGroupMember1.minGrassProximity = 0f;
                group1_lbGroupMember1.isRemoveTree = false;
                group1_lbGroupMember1.minTreeProximity = 2f;
                group1_lbGroupMember1.isTerrainAligned = false;
                group1_lbGroupMember1.isTerrainFlattened = false;
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
            }
            lbGroup1.groupMemberList.Add(group1_lbGroupMember1);
            #endregion
            // End Group Members

            // NOTE Add the new Group to the landscape meta-data
            landscape.lbGroupList.Add(lbGroup1);
        }
        #endregion

        #region Group2 [Posts Group]
        LBGroup lbGroup2 = new LBGroup();
        if (lbGroup2 != null)
        {
            #region Group2-level variables
            lbGroup2.groupName = "Posts Group";
            lbGroup2.lbGroupType = LBGroup.LBGroupType.Uniform;
            lbGroup2.maxGroupSqrKm = 10;
            lbGroup2.isDisabled = false;
            lbGroup2.showInEditor = true;
            lbGroup2.showGroupDefaultsInEditor = true;
            lbGroup2.showGroupOptionsInEditor = true;
            lbGroup2.showGroupMembersInEditor = true;
            lbGroup2.showGroupDesigner = false;
            lbGroup2.showGroupsInScene = false;
            lbGroup2.showtabInEditor = 0;
            lbGroup2.isMemberListExpanded = true;
            // Clearing Group variables
            lbGroup2.minClearingRadius = 100f;
            lbGroup2.maxClearingRadius = 100f;
            lbGroup2.startClearingRotationY = 0f;
            lbGroup2.endClearingRotationY = 359.9f;
            lbGroup2.isRemoveExistingGrass = true;
            lbGroup2.removeExistingGrassBlendDist = 0.5f;
            lbGroup2.isRemoveExistingTrees = true;
            // Proximity variables
            lbGroup2.proximityExtent = 10f;
            // Default values per group
            lbGroup2.minScale = 1f;
            lbGroup2.maxScale = 1f;
            lbGroup2.minHeight = 0f;
            lbGroup2.maxHeight = 1f;
            lbGroup2.minInclination = 0f;
            lbGroup2.maxInclination = 90f;
            // Group flatten terrain variables
            lbGroup2.isTerrainFlattened = false;
            lbGroup2.flattenHeightOffset = 0f;
            lbGroup2.flattenBlendRate = 0.5f;
            #endregion

            #region Group2 Zones
            // Start Group2-Level Zones
            // End Group2-Level Zones
            #endregion

            #region Group2 Filters
            lbGroup2.filterList = new List<LBFilter>();
            #endregion

            #region Group2 Textures
            // Only apply to Procedural and Manual Clearing groups
            lbGroup2.textureList = new List<LBGroupTexture>();
            #endregion

            #region Group2 Grass
            // Only apply to Procedural and Manual Clearing groups
            lbGroup2.grassList = new List<LBGroupGrass>();
            #endregion

            // Start Group Members
            #region Group2 Member1 
            LBGroupMember group2_lbGroupMember1 = new LBGroupMember();
            if (group2_lbGroupMember1 != null)
            {
                group2_lbGroupMember1.isDisabled = false;
                group2_lbGroupMember1.showInEditor = true;
                group2_lbGroupMember1.showtabInEditor = 3;
                group2_lbGroupMember1.GUID = "bf588c83-7c51-41b1-b52a-9952a09082cb";
                group2_lbGroupMember1.lbMemberType = LBGroupMember.LBMemberType.ObjPath;
                group2_lbGroupMember1.isGroupOverride = false;
                group2_lbGroupMember1.minScale = 1f;
                group2_lbGroupMember1.maxScale = 1f;
                group2_lbGroupMember1.minHeight = 0f;
                group2_lbGroupMember1.maxHeight = 1f;
                group2_lbGroupMember1.minInclination = 0f;
                group2_lbGroupMember1.maxInclination = 90f;
                group2_lbGroupMember1.prefab = null;
                group2_lbGroupMember1.prefabName = "";
                group2_lbGroupMember1.showPrefabPreview = false;
                group2_lbGroupMember1.isKeepPrefabConnection = false;
                group2_lbGroupMember1.isCombineMesh = false;
                group2_lbGroupMember1.isRemoveEmptyGameObjects = true;
                group2_lbGroupMember1.isRemoveAnimator = true;
                group2_lbGroupMember1.isCreateCollider = false;
                group2_lbGroupMember1.maxPrefabSqrKm = 10;
                group2_lbGroupMember1.maxPrefabPerGroup = 10000;
                group2_lbGroupMember1.isPlacedInCentre = false;
                group2_lbGroupMember1.showXYZSettings = false;
                group2_lbGroupMember1.modelOffsetX = 0f;
                group2_lbGroupMember1.modelOffsetY = 0f;
                group2_lbGroupMember1.modelOffsetZ = 0f;
                group2_lbGroupMember1.minOffsetX = 0f;
                group2_lbGroupMember1.minOffsetZ = 0f;
                group2_lbGroupMember1.minOffsetY = 0f;
                group2_lbGroupMember1.maxOffsetY = 0f;
                group2_lbGroupMember1.randomiseOffsetY = false;
                group2_lbGroupMember1.rotationType = LBGroupMember.LBRotationType.WorldSpace;
                group2_lbGroupMember1.randomiseRotationY = true;
                group2_lbGroupMember1.startRotationY = 0f;
                group2_lbGroupMember1.endRotationY = 359.9f;
                group2_lbGroupMember1.randomiseRotationXZ = false;
                group2_lbGroupMember1.rotationX = 0f;
                group2_lbGroupMember1.rotationZ = 0f;
                group2_lbGroupMember1.endRotationX = 0f;
                group2_lbGroupMember1.endRotationZ = 0f;
                group2_lbGroupMember1.useNoise = false;
                group2_lbGroupMember1.noiseOffset = 207.9302f;
                group2_lbGroupMember1.noiseTileSize = 500f;
                group2_lbGroupMember1.noisePlacementCutoff = 0.491f;
                group2_lbGroupMember1.proximityExtent = 10f;
                group2_lbGroupMember1.removeGrassBlendDist = 0.5f;
                group2_lbGroupMember1.minGrassProximity = 0f;
                group2_lbGroupMember1.isRemoveTree = true;
                group2_lbGroupMember1.minTreeProximity = 10f;
                group2_lbGroupMember1.isTerrainAligned = false;
                group2_lbGroupMember1.isTerrainFlattened = false;
                group2_lbGroupMember1.flattenDistance = 2f;
                group2_lbGroupMember1.flattenHeightOffset = 0f;
                group2_lbGroupMember1.flattenBlendRate = 0.5f;

                // Start Member-Level Zones references for group2_lbGroupMember1

                // End Member-Level Zones references for group2_lbGroupMember1

                group2_lbGroupMember1.isZoneEdgeFillTop = false;
                group2_lbGroupMember1.isZoneEdgeFillBottom = false;
                group2_lbGroupMember1.isZoneEdgeFillLeft = false;
                group2_lbGroupMember1.isZoneEdgeFillRight = false;
                group2_lbGroupMember1.zoneEdgeFillDistance = 1f;
                group2_lbGroupMember1.isPathOnly = false;
                group2_lbGroupMember1.lbObjectOrientation = LBObjPath.LBObjectOrientation.PathSpace;

                // Start Object Path settings for [Posts Object Path]
                group2_lbGroupMember1.lbObjPath = new LBObjPath();
                if (group2_lbGroupMember1.lbObjPath != null)
                {
                    // LBPath settings
                    group2_lbGroupMember1.lbObjPath.pathName = "Posts Object Path";
                    group2_lbGroupMember1.lbObjPath.showPathInScene = false;
                    group2_lbGroupMember1.lbObjPath.blendStart = true;
                    group2_lbGroupMember1.lbObjPath.blendEnd = true;
                    group2_lbGroupMember1.lbObjPath.pathResolution = 5f;
                    group2_lbGroupMember1.lbObjPath.closedCircuit = false;
                    group2_lbGroupMember1.lbObjPath.edgeBlendWidth = 5f;
                    group2_lbGroupMember1.lbObjPath.removeCentre = false;
                    group2_lbGroupMember1.lbObjPath.leftBorderWidth = 0f;
                    group2_lbGroupMember1.lbObjPath.rightBorderWidth = 0f;
                    group2_lbGroupMember1.lbObjPath.snapToTerrain = true;
                    group2_lbGroupMember1.lbObjPath.heightAboveTerrain = 2f;
                    group2_lbGroupMember1.lbObjPath.zoomOnFind = false;
                    group2_lbGroupMember1.lbObjPath.findZoomDistance = 50f;
                    // LBPath Mesh options (future use)
                    group2_lbGroupMember1.lbObjPath.isMeshLandscapeUV = false;
                    group2_lbGroupMember1.lbObjPath.meshUVTileScale = new Vector2(1f, 1f);
                    group2_lbGroupMember1.lbObjPath.meshYOffset = 0f;
                    group2_lbGroupMember1.lbObjPath.meshEdgeSnapToTerrain = false;
                    group2_lbGroupMember1.lbObjPath.meshSnapType = LBPath.MeshSnapType.BothEdges;
                    group2_lbGroupMember1.lbObjPath.meshIsDoubleSided = false;
                    group2_lbGroupMember1.lbObjPath.meshIncludeEdges = true;
                    group2_lbGroupMember1.lbObjPath.meshIncludeWater = false;
                    group2_lbGroupMember1.lbObjPath.meshSaveToProjectFolder = false;
                    // Path Points
                    group2_lbGroupMember1.lbObjPath.positionList.Add(new Vector3(723.7068f, 102.8855f, 64.82976f));
                    group2_lbGroupMember1.lbObjPath.positionList.Add(new Vector3(740.547f, 102.4963f, 69.91248f));
                    group2_lbGroupMember1.lbObjPath.positionList.Add(new Vector3(737.4777f, 102.747f, 84.75021f));
                    group2_lbGroupMember1.lbObjPath.positionList.Add(new Vector3(735.99f, 102.1416f, 99.257f));
                    group2_lbGroupMember1.lbObjPath.positionList.Add(new Vector3(735.8358f, 102.7111f, 114.5583f));
                    // LBObjPath settings
                    group2_lbGroupMember1.lbObjPath.layoutMethod = LBObjPath.LayoutMethod.Spacing;
                    group2_lbGroupMember1.lbObjPath.selectionMethod = LBObjPath.SelectionMethod.Alternating;
                    group2_lbGroupMember1.lbObjPath.spacingDistance = 1f;
                    group2_lbGroupMember1.lbObjPath.maxMainPrefabs = 5;
                    group2_lbGroupMember1.lbObjPath.isLastObjSnappedToEnd = true;
                    group2_lbGroupMember1.lbObjPath.pathPointList = new List<LBPathPoint>();
                    group2_lbGroupMember1.lbObjPath.mainObjPrefabList = new List<LBObjPrefab>();
                    // ObjPath Points
                    LBPathPoint group2_lbGroupMember1PathPoint = null;
                    group2_lbGroupMember1PathPoint = new LBPathPoint();
                    group2_lbGroupMember1PathPoint.GUID = "8fa4d1e6-7511-4bf9-9a79-9680b4dbf923";
                    group2_lbGroupMember1PathPoint.showInEditor = false;
                    group2_lbGroupMember1PathPoint.rotationZ = 0f;
                    group2_lbGroupMember1.lbObjPath.pathPointList.Add(group2_lbGroupMember1PathPoint);
                    group2_lbGroupMember1PathPoint = null;
                    group2_lbGroupMember1PathPoint = new LBPathPoint();
                    group2_lbGroupMember1PathPoint.GUID = "8fa4d1e6-7511-4bf9-9a79-9680b4dbf923";
                    group2_lbGroupMember1PathPoint.showInEditor = false;
                    group2_lbGroupMember1PathPoint.rotationZ = 0f;
                    group2_lbGroupMember1.lbObjPath.pathPointList.Add(group2_lbGroupMember1PathPoint);
                    group2_lbGroupMember1PathPoint = null;
                    group2_lbGroupMember1PathPoint = new LBPathPoint();
                    group2_lbGroupMember1PathPoint.GUID = "4bb6ba10-f056-43ae-95f9-a96b074c43a4";
                    group2_lbGroupMember1PathPoint.showInEditor = false;
                    group2_lbGroupMember1PathPoint.rotationZ = 10f;
                    group2_lbGroupMember1.lbObjPath.pathPointList.Add(group2_lbGroupMember1PathPoint);
                    group2_lbGroupMember1PathPoint = null;
                    group2_lbGroupMember1PathPoint = new LBPathPoint();
                    group2_lbGroupMember1PathPoint.GUID = "2b6df6fc-6328-4384-b113-f1d4e8b17bd5";
                    group2_lbGroupMember1PathPoint.showInEditor = false;
                    group2_lbGroupMember1PathPoint.rotationZ = 0f;
                    group2_lbGroupMember1.lbObjPath.pathPointList.Add(group2_lbGroupMember1PathPoint);
                    group2_lbGroupMember1PathPoint = null;
                    group2_lbGroupMember1PathPoint = new LBPathPoint();
                    group2_lbGroupMember1PathPoint.GUID = "77522d75-db91-41fe-be73-591f9f0521ee";
                    group2_lbGroupMember1PathPoint.showInEditor = false;
                    group2_lbGroupMember1PathPoint.rotationZ = 0f;
                    group2_lbGroupMember1.lbObjPath.pathPointList.Add(group2_lbGroupMember1PathPoint);
                    group2_lbGroupMember1PathPoint = null;
                    // Main ObjPrefabs
                    LBObjPrefab group2_lbGroupMember1ObjPathPrefab = null;
                    group2_lbGroupMember1ObjPathPrefab = new LBObjPrefab();
                    group2_lbGroupMember1ObjPathPrefab.groupMemberGUID = "040601b0-046d-4d7b-8a5d-1414f5895ee5";
                    group2_lbGroupMember1.lbObjPath.mainObjPrefabList.Add(group2_lbGroupMember1ObjPathPrefab);
                    group2_lbGroupMember1ObjPathPrefab = null;
                    // Start ObjPrefab
                    group2_lbGroupMember1.lbObjPath.startObjPrefab = new LBObjPrefab();
                    group2_lbGroupMember1.lbObjPath.startObjPrefab.groupMemberGUID = "";
                    // End ObjPrefab
                    group2_lbGroupMember1.lbObjPath.endObjPrefab = new LBObjPrefab();
                    group2_lbGroupMember1.lbObjPath.endObjPrefab.groupMemberGUID = "";
                }

                // End Object Path settings for [Posts Object Path]
            }
            lbGroup2.groupMemberList.Add(group2_lbGroupMember1);
            #endregion

            #region Group2 Member2 LB_PostWood01_LQ_512
            LBGroupMember group2_lbGroupMember2 = new LBGroupMember();
            if (group2_lbGroupMember2 != null)
            {
                group2_lbGroupMember2.isDisabled = false;
                group2_lbGroupMember2.showInEditor = true;
                group2_lbGroupMember2.showtabInEditor = 0;
                group2_lbGroupMember2.GUID = "040601b0-046d-4d7b-8a5d-1414f5895ee5";
                group2_lbGroupMember2.lbMemberType = LBGroupMember.LBMemberType.Prefab;
                group2_lbGroupMember2.isGroupOverride = false;
                group2_lbGroupMember2.minScale = 1f;
                group2_lbGroupMember2.maxScale = 1f;
                group2_lbGroupMember2.minHeight = 0f;
                group2_lbGroupMember2.maxHeight = 1f;
                group2_lbGroupMember2.minInclination = 0f;
                group2_lbGroupMember2.maxInclination = 90f;
                group2_lbGroupMember2.prefab = Group002_Member002prefab;
                group2_lbGroupMember2.prefabName = "LB_PostWood01_LQ_512";
                group2_lbGroupMember2.showPrefabPreview = false;
                group2_lbGroupMember2.isKeepPrefabConnection = false;
                group2_lbGroupMember2.isCombineMesh = false;
                group2_lbGroupMember2.isRemoveEmptyGameObjects = true;
                group2_lbGroupMember2.isRemoveAnimator = true;
                group2_lbGroupMember2.isCreateCollider = false;
                group2_lbGroupMember2.maxPrefabSqrKm = 10;
                group2_lbGroupMember2.maxPrefabPerGroup = 10000;
                group2_lbGroupMember2.isPlacedInCentre = false;
                group2_lbGroupMember2.showXYZSettings = false;
                group2_lbGroupMember2.modelOffsetX = 0f;
                group2_lbGroupMember2.modelOffsetY = 0f;
                group2_lbGroupMember2.modelOffsetZ = 0f;
                group2_lbGroupMember2.minOffsetX = 0f;
                group2_lbGroupMember2.minOffsetZ = 0f;
                group2_lbGroupMember2.minOffsetY = 0f;
                group2_lbGroupMember2.maxOffsetY = 0f;
                group2_lbGroupMember2.randomiseOffsetY = false;
                group2_lbGroupMember2.rotationType = LBGroupMember.LBRotationType.WorldSpace;
                group2_lbGroupMember2.randomiseRotationY = false;
                group2_lbGroupMember2.startRotationY = 0f;
                group2_lbGroupMember2.endRotationY = 359.9f;
                group2_lbGroupMember2.randomiseRotationXZ = false;
                group2_lbGroupMember2.rotationX = 0f;
                group2_lbGroupMember2.rotationZ = 0f;
                group2_lbGroupMember2.endRotationX = 0f;
                group2_lbGroupMember2.endRotationZ = 0f;
                group2_lbGroupMember2.useNoise = true;
                group2_lbGroupMember2.noiseOffset = 0f;
                group2_lbGroupMember2.noiseTileSize = 500f;
                group2_lbGroupMember2.noisePlacementCutoff = 0.65f;
                group2_lbGroupMember2.proximityExtent = 1f;
                group2_lbGroupMember2.removeGrassBlendDist = 0.5f;
                group2_lbGroupMember2.minGrassProximity = 0f;
                group2_lbGroupMember2.isRemoveTree = true;
                group2_lbGroupMember2.minTreeProximity = 10f;
                group2_lbGroupMember2.isTerrainAligned = false;
                group2_lbGroupMember2.isTerrainFlattened = false;
                group2_lbGroupMember2.flattenDistance = 2f;
                group2_lbGroupMember2.flattenHeightOffset = 0f;
                group2_lbGroupMember2.flattenBlendRate = 0.5f;

                // Start Member-Level Zones references for group2_lbGroupMember2

                // End Member-Level Zones references for group2_lbGroupMember2

                group2_lbGroupMember2.isZoneEdgeFillTop = false;
                group2_lbGroupMember2.isZoneEdgeFillBottom = false;
                group2_lbGroupMember2.isZoneEdgeFillLeft = false;
                group2_lbGroupMember2.isZoneEdgeFillRight = false;
                group2_lbGroupMember2.zoneEdgeFillDistance = 1f;
                group2_lbGroupMember2.isPathOnly = true;
                group2_lbGroupMember2.lbObjectOrientation = LBObjPath.LBObjectOrientation.PathSpace;
            }
            lbGroup2.groupMemberList.Add(group2_lbGroupMember2);
            #endregion

            #region Group2 Member3 
            LBGroupMember group2_lbGroupMember3 = new LBGroupMember();
            if (group2_lbGroupMember3 != null)
            {
                group2_lbGroupMember3.isDisabled = false;
                group2_lbGroupMember3.showInEditor = true;
                group2_lbGroupMember3.showtabInEditor = 3;
                group2_lbGroupMember3.GUID = "06a97ba5-8770-4e9d-8033-4fc9e56a7f6a";
                group2_lbGroupMember3.lbMemberType = LBGroupMember.LBMemberType.ObjPath;
                group2_lbGroupMember3.isGroupOverride = false;
                group2_lbGroupMember3.minScale = 1f;
                group2_lbGroupMember3.maxScale = 1f;
                group2_lbGroupMember3.minHeight = 0f;
                group2_lbGroupMember3.maxHeight = 1f;
                group2_lbGroupMember3.minInclination = 0f;
                group2_lbGroupMember3.maxInclination = 90f;
                group2_lbGroupMember3.prefab = null;
                group2_lbGroupMember3.prefabName = "";
                group2_lbGroupMember3.showPrefabPreview = false;
                group2_lbGroupMember3.isKeepPrefabConnection = false;
                group2_lbGroupMember3.isCombineMesh = false;
                group2_lbGroupMember3.isRemoveEmptyGameObjects = true;
                group2_lbGroupMember3.isRemoveAnimator = true;
                group2_lbGroupMember3.isCreateCollider = false;
                group2_lbGroupMember3.maxPrefabSqrKm = 10;
                group2_lbGroupMember3.maxPrefabPerGroup = 10000;
                group2_lbGroupMember3.isPlacedInCentre = false;
                group2_lbGroupMember3.showXYZSettings = false;
                group2_lbGroupMember3.modelOffsetX = 0f;
                group2_lbGroupMember3.modelOffsetY = 0f;
                group2_lbGroupMember3.modelOffsetZ = 0f;
                group2_lbGroupMember3.minOffsetX = 0f;
                group2_lbGroupMember3.minOffsetZ = 0f;
                group2_lbGroupMember3.minOffsetY = 0f;
                group2_lbGroupMember3.maxOffsetY = 0f;
                group2_lbGroupMember3.randomiseOffsetY = false;
                group2_lbGroupMember3.rotationType = LBGroupMember.LBRotationType.WorldSpace;
                group2_lbGroupMember3.randomiseRotationY = true;
                group2_lbGroupMember3.startRotationY = 0f;
                group2_lbGroupMember3.endRotationY = 359.9f;
                group2_lbGroupMember3.randomiseRotationXZ = false;
                group2_lbGroupMember3.rotationX = 0f;
                group2_lbGroupMember3.rotationZ = 0f;
                group2_lbGroupMember3.endRotationX = 0f;
                group2_lbGroupMember3.endRotationZ = 0f;
                group2_lbGroupMember3.useNoise = false;
                group2_lbGroupMember3.noiseOffset = 207.9302f;
                group2_lbGroupMember3.noiseTileSize = 500f;
                group2_lbGroupMember3.noisePlacementCutoff = 1f;
                group2_lbGroupMember3.proximityExtent = 10f;
                group2_lbGroupMember3.removeGrassBlendDist = 0.5f;
                group2_lbGroupMember3.minGrassProximity = 0f;
                group2_lbGroupMember3.isRemoveTree = true;
                group2_lbGroupMember3.minTreeProximity = 10f;
                group2_lbGroupMember3.isTerrainAligned = false;
                group2_lbGroupMember3.isTerrainFlattened = false;
                group2_lbGroupMember3.flattenDistance = 2f;
                group2_lbGroupMember3.flattenHeightOffset = 0f;
                group2_lbGroupMember3.flattenBlendRate = 0.5f;

                // Start Member-Level Zones references for group2_lbGroupMember3

                // End Member-Level Zones references for group2_lbGroupMember3

                group2_lbGroupMember3.isZoneEdgeFillTop = false;
                group2_lbGroupMember3.isZoneEdgeFillBottom = false;
                group2_lbGroupMember3.isZoneEdgeFillLeft = false;
                group2_lbGroupMember3.isZoneEdgeFillRight = false;
                group2_lbGroupMember3.zoneEdgeFillDistance = 1f;
                group2_lbGroupMember3.isPathOnly = false;
                group2_lbGroupMember3.lbObjectOrientation = LBObjPath.LBObjectOrientation.PathSpace;

                // Start Object Path settings for [Posts Group objpath 03]
                group2_lbGroupMember3.lbObjPath = new LBObjPath();
                if (group2_lbGroupMember3.lbObjPath != null)
                {
                    // LBPath settings
                    group2_lbGroupMember3.lbObjPath.pathName = "Posts Group objpath 03";
                    group2_lbGroupMember3.lbObjPath.showPathInScene = false;
                    group2_lbGroupMember3.lbObjPath.blendStart = true;
                    group2_lbGroupMember3.lbObjPath.blendEnd = true;
                    group2_lbGroupMember3.lbObjPath.pathResolution = 5f;
                    group2_lbGroupMember3.lbObjPath.closedCircuit = false;
                    group2_lbGroupMember3.lbObjPath.edgeBlendWidth = 5f;
                    group2_lbGroupMember3.lbObjPath.removeCentre = false;
                    group2_lbGroupMember3.lbObjPath.leftBorderWidth = 0f;
                    group2_lbGroupMember3.lbObjPath.rightBorderWidth = 0f;
                    group2_lbGroupMember3.lbObjPath.snapToTerrain = true;
                    group2_lbGroupMember3.lbObjPath.heightAboveTerrain = 2f;
                    group2_lbGroupMember3.lbObjPath.zoomOnFind = false;
                    group2_lbGroupMember3.lbObjPath.findZoomDistance = 50f;
                    // LBPath Mesh options (future use)
                    group2_lbGroupMember3.lbObjPath.isMeshLandscapeUV = false;
                    group2_lbGroupMember3.lbObjPath.meshUVTileScale = new Vector2(1f, 1f);
                    group2_lbGroupMember3.lbObjPath.meshYOffset = 0f;
                    group2_lbGroupMember3.lbObjPath.meshEdgeSnapToTerrain = false;
                    group2_lbGroupMember3.lbObjPath.meshSnapType = LBPath.MeshSnapType.BothEdges;
                    group2_lbGroupMember3.lbObjPath.meshIsDoubleSided = false;
                    group2_lbGroupMember3.lbObjPath.meshIncludeEdges = true;
                    group2_lbGroupMember3.lbObjPath.meshIncludeWater = false;
                    group2_lbGroupMember3.lbObjPath.meshSaveToProjectFolder = false;
                    // Path Points
                    group2_lbGroupMember3.lbObjPath.positionList.Add(new Vector3(769.2106f, 102.6073f, 86.10138f));
                    group2_lbGroupMember3.lbObjPath.positionList.Add(new Vector3(772.8463f, 102.6012f, 107.22f));
                    group2_lbGroupMember3.lbObjPath.positionList.Add(new Vector3(779.0529f, 102.8491f, 132.4362f));
                    // LBObjPath settings
                    group2_lbGroupMember3.lbObjPath.layoutMethod = LBObjPath.LayoutMethod.ExactQty;
                    group2_lbGroupMember3.lbObjPath.selectionMethod = LBObjPath.SelectionMethod.Alternating;
                    group2_lbGroupMember3.lbObjPath.spacingDistance = 10f;
                    group2_lbGroupMember3.lbObjPath.maxMainPrefabs = 5;
                    group2_lbGroupMember3.lbObjPath.isLastObjSnappedToEnd = false;
                    group2_lbGroupMember3.lbObjPath.pathPointList = new List<LBPathPoint>();
                    group2_lbGroupMember3.lbObjPath.mainObjPrefabList = new List<LBObjPrefab>();
                    // ObjPath Points
                    LBPathPoint group2_lbGroupMember3PathPoint = null;
                    group2_lbGroupMember3PathPoint = new LBPathPoint();
                    group2_lbGroupMember3PathPoint.GUID = "6c2fad9f-91b1-4c75-9308-91e3fdc23f7f";
                    group2_lbGroupMember3PathPoint.showInEditor = false;
                    group2_lbGroupMember3PathPoint.rotationZ = 0f;
                    group2_lbGroupMember3.lbObjPath.pathPointList.Add(group2_lbGroupMember3PathPoint);
                    group2_lbGroupMember3PathPoint = null;
                    group2_lbGroupMember3PathPoint = new LBPathPoint();
                    group2_lbGroupMember3PathPoint.GUID = "1f6426af-da74-4759-824c-6dbd46bbb5e6";
                    group2_lbGroupMember3PathPoint.showInEditor = false;
                    group2_lbGroupMember3PathPoint.rotationZ = 0f;
                    group2_lbGroupMember3.lbObjPath.pathPointList.Add(group2_lbGroupMember3PathPoint);
                    group2_lbGroupMember3PathPoint = null;
                    group2_lbGroupMember3PathPoint = new LBPathPoint();
                    group2_lbGroupMember3PathPoint.GUID = "2671e138-b78b-4f05-84e4-d7689d5d9591";
                    group2_lbGroupMember3PathPoint.showInEditor = false;
                    group2_lbGroupMember3PathPoint.rotationZ = 0f;
                    group2_lbGroupMember3.lbObjPath.pathPointList.Add(group2_lbGroupMember3PathPoint);
                    group2_lbGroupMember3PathPoint = null;
                    // Main ObjPrefabs
                    LBObjPrefab group2_lbGroupMember3ObjPathPrefab = null;
                    group2_lbGroupMember3ObjPathPrefab = new LBObjPrefab();
                    group2_lbGroupMember3ObjPathPrefab.groupMemberGUID = "040601b0-046d-4d7b-8a5d-1414f5895ee5";
                    group2_lbGroupMember3.lbObjPath.mainObjPrefabList.Add(group2_lbGroupMember3ObjPathPrefab);
                    group2_lbGroupMember3ObjPathPrefab = null;
                    // Start ObjPrefab
                    group2_lbGroupMember3.lbObjPath.startObjPrefab = new LBObjPrefab();
                    group2_lbGroupMember3.lbObjPath.startObjPrefab.groupMemberGUID = "";
                    // End ObjPrefab
                    group2_lbGroupMember3.lbObjPath.endObjPrefab = new LBObjPrefab();
                    group2_lbGroupMember3.lbObjPath.endObjPrefab.groupMemberGUID = "";
                }

                // End Object Path settings for [Posts Group objpath 03]
            }
            lbGroup2.groupMemberList.Add(group2_lbGroupMember3);
            #endregion
            // End Group Members

            // NOTE Add the new Group to the landscape meta-data
            landscape.lbGroupList.Add(lbGroup2);
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
            // Find the first MapPath in the landscape (typically this would be hardcoded rather
            // than performing a (slow) GetComponentInChildren.
            LBMapPath lbMapPath = landscape.GetComponentInChildren<LBMapPath>();
            if (lbMapPath != null)
            {
                if (!lbCameraAnimator.cameraPath.ImportMapPathPoints(lbMapPath))
                {
                    Debug.LogWarning("Could not make camera animator travel along new Map Path.");
                }
            }

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
