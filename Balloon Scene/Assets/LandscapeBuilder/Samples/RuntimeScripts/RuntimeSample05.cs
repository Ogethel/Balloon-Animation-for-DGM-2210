using UnityEngine;
using System.Collections;
using System.Collections.Generic; // System.Collections.Generic is needed for list functionality
using LandscapeBuilder;

/// <summary>
/// Example script that creates a landscape entirely at runtime
/// It creates the topography and textures the landscape
/// It uses the new Layer-based topography generation type.
/// NOTE: Ensure Project Settings, Color Space = Linear
/// There is no error checking for different sized terrains.
/// RECOMMEND: Start with RuntimeSample07 or newer, then work backwards to find what you need.
/// </summary>
public class RuntimeSample05 : MonoBehaviour
{
    #region Public variables and properties
    public Vector2 landscapeSize = Vector2.one * 4000f;
    public float terrainWidth = 1000f;
    public float terrainHeight = 1000f;

    // References to texture files
    // Ideally should be 4 or 8 textures so shader can do 1 or 2 passes
    public Texture2D texture1;
    public Texture2D texture1NM;
    public Texture2D texture2;
    public Texture2D texture2NM;
    public Texture2D texture3;
    public Texture2D texture3NM;
    public Texture2D texture4;
    public Texture2D texture4NM;
    public Texture2D texture5;
    public Texture2D texture5NM;

    // References to tree types
    public Transform treePrefab1;
    public Transform treePrefab2;

    // Stencil Layer input images
    public Texture2D stencilLayerTex1;

    public float waterHeight = 398f;

    // Reference to prefabs
    public GameObject rockPrefab;

    // Reference to paths
    public Material path1meshMaterial;

    #endregion

    #region Private variables
    private LBLandscape landscape = null;
    #endregion

    // Use this for initialization
    void Awake()
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
            Debug.Log("Cannot find LBLandscape script attached to Runtime gameobject");
            return;
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

        // Update the size
        landscape.size = landscapeSize;
        // Create the terrains
        int terrainNumber = 0;
        #endregion

        #region Create Terrains
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
                // Set terrain settings (depending on your situtation, you may need to set more or less than I have in this example)
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

                // Set up the terrain collider
                TerrainCollider newTerrainCol = terrainObj.AddComponent<TerrainCollider>();
                newTerrainCol.terrainData = newTerrainData;
            }
        }

        landscape.SetLandscapeTerrains(true);
        #endregion

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

        #region Create a Stencil
        // Add a new Stencil to the (empty) landscape
        LBStencil lbStencil = LBStencil.CreateStencilInScene(landscape, landscape.gameObject);
        if (lbStencil != null)
        {
            lbStencil.name += "01";

            /// Import PNG files into stencil layers (at runtime) and add as filters.
            lbStencil.layerImportMethod = LBStencil.LayerImportMethod.Grayscale;
            if (lbStencil.ImportTexture("Hills Layer 1", stencilLayerTex1, true))
            {
                // Remove the default first layer
                if (lbStencil.stencilLayerList.Count > 1) { lbStencil.stencilLayerList.RemoveAt(0); }
            }
        }
        #endregion

        #region Define the Topography
        // NOTE: This sample uses a topography mask. This is not a requirement

        // Create the distance to centre mask using an animation curve
        AnimationCurve distanceToCentreMask = new AnimationCurve();
        distanceToCentreMask.AddKey(0.00f, 0.04f);
        distanceToCentreMask.AddKey(0.30f, 0.03f);
        distanceToCentreMask.AddKey(0.78f, 1.00f);
        distanceToCentreMask.AddKey(1.00f, 1.00f);
        Keyframe[] distanceToCentreMaskKeys = distanceToCentreMask.keys;
        distanceToCentreMaskKeys[0].inTangent = 0.00f;
        distanceToCentreMaskKeys[0].outTangent = 0.00f;
        distanceToCentreMaskKeys[1].inTangent = -0.02f;
        distanceToCentreMaskKeys[1].outTangent = -0.02f;
        distanceToCentreMaskKeys[2].inTangent = 0.01f;
        distanceToCentreMaskKeys[2].outTangent = 0.01f;
        distanceToCentreMaskKeys[3].inTangent = 0.00f;
        distanceToCentreMaskKeys[3].outTangent = 0.00f;
        distanceToCentreMask = new AnimationCurve(distanceToCentreMaskKeys);

        // Assign the topography mask to the LBLandscape instance
        landscape.distanceToCentreMask = distanceToCentreMask;
        landscape.topographyMaskMode = LBLandscape.MaskMode.DistanceToCentre;

        // Create the Topography Layers
        // You can mix and match Perlin and Image layers
        landscape.topographyLayersList = new List<LBLayer>();
        if (landscape.topographyLayersList != null)
        {
            // NOTE
            // If you have used the LB Editor to first build a test landscape, the Topography layer
            // can be scripted out and pasted into your runtime script here.

            // Layer Code generated from Landscape Builder at 7:59 AM on ....
            AnimationCurve additiveCurve = new AnimationCurve();

            AnimationCurve subtractiveCurve = new AnimationCurve();

            List<LBLayerFilter> filters = new List<LBLayerFilter>();
            // Add LayerFilter code here
            List<AnimationCurve> imageCurveModifiers = new List<AnimationCurve>();

            List<AnimationCurve> outputCurveModifiers = new List<AnimationCurve>();
            AnimationCurve OutputCurve1 = new AnimationCurve();
            OutputCurve1.AddKey(0.00f, 0.00f);
            OutputCurve1.AddKey(0.50f, 0.06f);
            OutputCurve1.AddKey(1.00f, 1.00f);
            Keyframe[] OutputCurve1Keys = OutputCurve1.keys;
            OutputCurve1Keys[0].inTangent = 0.00f;
            OutputCurve1Keys[0].outTangent = 0.00f;
            OutputCurve1Keys[1].inTangent = 0.50f;
            OutputCurve1Keys[1].outTangent = 0.50f;
            OutputCurve1Keys[2].inTangent = 4.00f;
            OutputCurve1Keys[2].outTangent = 4.00f;
            OutputCurve1 = new AnimationCurve(OutputCurve1Keys);

            outputCurveModifiers.Add(OutputCurve1);

            List<AnimationCurve> perOctaveCurveModifiers = new List<AnimationCurve>();
            AnimationCurve PerOctaveCurve1 = new AnimationCurve();
            PerOctaveCurve1.AddKey(0.00f, 0.00f);
            PerOctaveCurve1.AddKey(0.05f, 0.10f);
            PerOctaveCurve1.AddKey(0.45f, 0.90f);
            PerOctaveCurve1.AddKey(0.50f, 1.00f);
            PerOctaveCurve1.AddKey(0.55f, 0.90f);
            PerOctaveCurve1.AddKey(0.95f, 0.10f);
            PerOctaveCurve1.AddKey(1.00f, 0.00f);
            Keyframe[] PerOctaveCurve1Keys = PerOctaveCurve1.keys;
            PerOctaveCurve1Keys[0].inTangent = 0.00f;
            PerOctaveCurve1Keys[0].outTangent = 0.00f;
            PerOctaveCurve1Keys[1].inTangent = 2.00f;
            PerOctaveCurve1Keys[1].outTangent = 2.00f;
            PerOctaveCurve1Keys[2].inTangent = 2.00f;
            PerOctaveCurve1Keys[2].outTangent = 2.00f;
            PerOctaveCurve1Keys[3].inTangent = 0.00f;
            PerOctaveCurve1Keys[3].outTangent = 0.00f;
            PerOctaveCurve1Keys[4].inTangent = -2.00f;
            PerOctaveCurve1Keys[4].outTangent = -2.00f;
            PerOctaveCurve1Keys[5].inTangent = -2.00f;
            PerOctaveCurve1Keys[5].outTangent = -2.00f;
            PerOctaveCurve1Keys[6].inTangent = 0.00f;
            PerOctaveCurve1Keys[6].outTangent = 0.00f;
            PerOctaveCurve1 = new AnimationCurve(PerOctaveCurve1Keys);

            perOctaveCurveModifiers.Add(PerOctaveCurve1);

            AnimationCurve mapPathBlendCurve = new AnimationCurve();
            mapPathBlendCurve.AddKey(0.00f, 0.00f);
            mapPathBlendCurve.AddKey(1.00f, 1.00f);
            Keyframe[] mapPathBlendCurveKeys = mapPathBlendCurve.keys;
            mapPathBlendCurveKeys[0].inTangent = 0.00f;
            mapPathBlendCurveKeys[0].outTangent = 0.00f;
            mapPathBlendCurveKeys[1].inTangent = 0.00f;
            mapPathBlendCurveKeys[1].outTangent = 0.00f;
            mapPathBlendCurve = new AnimationCurve(mapPathBlendCurveKeys);

            AnimationCurve mapPathHeightCurve = new AnimationCurve();
            mapPathHeightCurve.AddKey(0.00f, 1.00f);
            mapPathHeightCurve.AddKey(1.00f, 1.00f);
            Keyframe[] mapPathHeightCurveKeys = mapPathHeightCurve.keys;
            mapPathHeightCurveKeys[0].inTangent = 0.00f;
            mapPathHeightCurveKeys[0].outTangent = 0.00f;
            mapPathHeightCurveKeys[1].inTangent = 0.00f;
            mapPathHeightCurveKeys[1].outTangent = 0.00f;
            mapPathHeightCurve = new AnimationCurve(mapPathHeightCurveKeys);

            LBLayer lbBaseLayer01 = new LBLayer();
            if (lbBaseLayer01 != null)
            {
                lbBaseLayer01.type = LBLayer.LayerType.PerlinBase;
                lbBaseLayer01.preset = LBLayer.LayerPreset.MountainPeaksBase;
                lbBaseLayer01.layerTypeMode = LBLayer.LayerTypeMode.Add;
                lbBaseLayer01.noiseTileSize = 1000;
                lbBaseLayer01.noiseOffsetX = 0;
                lbBaseLayer01.noiseOffsetZ = 0;
                lbBaseLayer01.octaves = 7;
                lbBaseLayer01.downscaling = 1;
                lbBaseLayer01.lacunarity = 1.75f;
                lbBaseLayer01.gain = 0.475f;
                lbBaseLayer01.additiveAmount = 0.5f;
                lbBaseLayer01.subtractiveAmount = 0.2f;
                lbBaseLayer01.additiveCurve = additiveCurve;
                lbBaseLayer01.subtractiveCurve = subtractiveCurve;
                lbBaseLayer01.removeBaseNoise = true;
                lbBaseLayer01.addMinHeight = false;
                lbBaseLayer01.addHeight = 0f;
                lbBaseLayer01.restrictArea = false;
                lbBaseLayer01.areaRect = new Rect(0, 0, 1000, 1000);
                lbBaseLayer01.interpolationSmoothing = 0;
                //lbBaseLayer01.heightmapImage = heightmapImage;
                lbBaseLayer01.imageHeightScale = 1f;
                lbBaseLayer01.imageCurveModifiers = imageCurveModifiers;
                lbBaseLayer01.filters = filters;
                lbBaseLayer01.isDisabled = false;
                lbBaseLayer01.showLayer = false;
                lbBaseLayer01.showAdvancedSettings = false;
                lbBaseLayer01.showCurvesAndFilters = true;
                lbBaseLayer01.showAreaHighlighter = false;
                lbBaseLayer01.detailSmoothRate = 0f;
                lbBaseLayer01.areaBlendRate = 0.5f;
                lbBaseLayer01.downscaling = 1;
                lbBaseLayer01.warpAmount = 0f;
                lbBaseLayer01.warpOctaves = 1;
                lbBaseLayer01.outputCurveModifiers = outputCurveModifiers;
                lbBaseLayer01.perOctaveCurveModifiers = perOctaveCurveModifiers;
                lbBaseLayer01.heightScale = 0.75f;
                lbBaseLayer01.minHeight = 0f;
                lbBaseLayer01.imageSource = LBLayer.LayerImageSource.Default;
                lbBaseLayer01.imageRepairHoles = false;
                lbBaseLayer01.threshholdRepairHoles = 0f;
                lbBaseLayer01.mapPathBlendCurve = mapPathBlendCurve;
                lbBaseLayer01.mapPathHeightCurve = mapPathHeightCurve;
                lbBaseLayer01.mapPathAddInvert = false;
                lbBaseLayer01.floorOffsetY = 0f;
                // Add LBMapPath code here
                // lbBaseLayer01.lbPath = lbPath;
                // lbBaseLayer01.lbMapPath = lbMapPath;

                // NOTE Add the new layer to the landscape meta-data
                landscape.topographyLayersList.Add(lbBaseLayer01);
            }
        }

        #endregion

        // Create the terrain topographies
        landscape.ApplyTopography(false, true);

        // NOTE
        // If you have used the LB Editor to first build a test path in a landscape, the path points can be scripted
        // out and pasted into your runtime script

        #region LBMapPath Map Path
        // LBMapPath generated from Landscape Builder at 3:10 PM on Sunday, October 01, 2017
        LBMapPath lbMapPath = LBMapPath.CreateMapPath(landscape, landscape.gameObject);
        if (lbMapPath != null)
        {
            lbMapPath.name = "Map Path";
            if (lbMapPath.lbPath != null)
            {
                lbMapPath.lbPath.pathName = "Map Path";
                lbMapPath.mapResolution = 1;
                lbMapPath.lbPath.blendStart = true;
                lbMapPath.lbPath.blendEnd = true;
                lbMapPath.lbPath.pathResolution = 2f;
                lbMapPath.lbPath.closedCircuit = false;
                lbMapPath.lbPath.edgeBlendWidth = 0f;
                lbMapPath.lbPath.removeCentre = false;
                lbMapPath.lbPath.leftBorderWidth = 0f;
                lbMapPath.lbPath.rightBorderWidth = 0f;
                lbMapPath.lbPath.snapToTerrain = true;
                lbMapPath.lbPath.heightAboveTerrain = 0f;
                lbMapPath.lbPath.zoomOnFind = true;
                lbMapPath.lbPath.findZoomDistance = 50f;
                // Mesh options
                lbMapPath.lbPath.isMeshLandscapeUV = false;
                lbMapPath.lbPath.meshUVTileScale = new Vector2(1, 1);
                lbMapPath.lbPath.meshYOffset = 0.08f;
                lbMapPath.lbPath.meshEdgeSnapToTerrain = true;
                lbMapPath.lbPath.meshSnapType = LBPath.MeshSnapType.BothEdges;
                lbMapPath.lbPath.meshIsDoubleSided = false;
                lbMapPath.lbPath.meshIncludeEdges = false;
                lbMapPath.lbPath.meshIncludeWater = false;
                lbMapPath.lbPath.meshSaveToProjectFolder = false;
                lbMapPath.meshMaterial = path1meshMaterial;
                // Path Points
                lbMapPath.lbPath.positionList.Add(new Vector3(631.6624f, 23.52699f, 825.2574f));
                lbMapPath.lbPath.positionList.Add(new Vector3(615.8184f, 9.507171f, 763.9471f));
                lbMapPath.lbPath.positionList.Add(new Vector3(592.5421f, 4.510101f, 721.7316f));
                lbMapPath.lbPath.positionList.Add(new Vector3(600.6003f, 2.589149f, 679.3673f));
                lbMapPath.lbPath.positionList.Add(new Vector3(620.8919f, 2.475228f, 658.526f));
                lbMapPath.lbPath.positionList.Add(new Vector3(599.6022f, 2.838308f, 605.2884f));
                lbMapPath.lbPath.positionList.Add(new Vector3(575.0403f, 2.714737f, 555.6046f));
                lbMapPath.lbPath.positionList.Add(new Vector3(560.4745f, 2.838308f, 472.1874f));
                lbMapPath.lbPath.positionList.Add(new Vector3(570.2177f, 3.09073f, 444.6886f));
                lbMapPath.lbPath.positionList.Add(new Vector3(570.2177f, 3.299063f, 387.1046f));
                lbMapPath.lbPath.positionList.Add(new Vector3(600.3734f, 4.613719f, 311.1418f));
                lbMapPath.lbPath.positionList.Add(new Vector3(610.5876f, 6.345493f, 293.1662f));
                lbMapPath.lbPath.widthList.Add(14f);
                lbMapPath.lbPath.widthList.Add(14f);
                lbMapPath.lbPath.widthList.Add(14f);
                lbMapPath.lbPath.widthList.Add(14f);
                lbMapPath.lbPath.widthList.Add(14f);
                lbMapPath.lbPath.widthList.Add(11.32066f);
                lbMapPath.lbPath.widthList.Add(14f);
                lbMapPath.lbPath.widthList.Add(14f);
                lbMapPath.lbPath.widthList.Add(14f);
                lbMapPath.lbPath.widthList.Add(14f);
                lbMapPath.lbPath.widthList.Add(14f);
                lbMapPath.lbPath.widthList.Add(14f);
                lbMapPath.lbPath.positionListLeftEdge.Add(new Vector3(638.2852f, 23.52699f, 823.5834f));
                lbMapPath.lbPath.positionListLeftEdge.Add(new Vector3(622.2536f, 9.507171f, 761.4589f));
                lbMapPath.lbPath.positionListLeftEdge.Add(new Vector3(599.409f, 4.510101f, 720.4888f));
                lbMapPath.lbPath.positionListLeftEdge.Add(new Vector3(606.8383f, 2.589149f, 682.5374f));
                lbMapPath.lbPath.positionListLeftEdge.Add(new Vector3(627.8883f, 2.475228f, 658.7487f));
                lbMapPath.lbPath.positionListLeftEdge.Add(new Vector3(604.7557f, 2.838308f, 602.9471f));
                lbMapPath.lbPath.positionListLeftEdge.Add(new Vector3(581.7473f, 2.714737f, 553.6004f));
                lbMapPath.lbPath.positionListLeftEdge.Add(new Vector3(567.473f, 2.838308f, 472.0406f));
                lbMapPath.lbPath.positionListLeftEdge.Add(new Vector3(577.1524f, 3.09073f, 445.6413f));
                lbMapPath.lbPath.positionListLeftEdge.Add(new Vector3(577.0537f, 3.299063f, 388.6086f));
                lbMapPath.lbPath.positionListLeftEdge.Add(new Vector3(606.7752f, 4.613719f, 313.9633f));
                lbMapPath.lbPath.positionListLeftEdge.Add(new Vector3(616.454f, 6.345493f, 296.91f));
                lbMapPath.lbPath.positionListRightEdge.Add(new Vector3(625.0396f, 23.52699f, 826.9315f));
                lbMapPath.lbPath.positionListRightEdge.Add(new Vector3(609.3831f, 9.507171f, 766.4353f));
                lbMapPath.lbPath.positionListRightEdge.Add(new Vector3(585.6751f, 4.510101f, 722.9744f));
                lbMapPath.lbPath.positionListRightEdge.Add(new Vector3(594.3624f, 2.589149f, 676.1973f));
                lbMapPath.lbPath.positionListRightEdge.Add(new Vector3(613.8955f, 2.475228f, 658.3033f));
                lbMapPath.lbPath.positionListRightEdge.Add(new Vector3(594.4488f, 2.838308f, 607.6296f));
                lbMapPath.lbPath.positionListRightEdge.Add(new Vector3(568.3334f, 2.714737f, 557.6088f));
                lbMapPath.lbPath.positionListRightEdge.Add(new Vector3(553.4761f, 2.838308f, 472.3342f));
                lbMapPath.lbPath.positionListRightEdge.Add(new Vector3(563.2829f, 3.09073f, 443.736f));
                lbMapPath.lbPath.positionListRightEdge.Add(new Vector3(563.3816f, 3.299063f, 385.6005f));
                lbMapPath.lbPath.positionListRightEdge.Add(new Vector3(593.9716f, 4.613719f, 308.3203f));
                lbMapPath.lbPath.positionListRightEdge.Add(new Vector3(604.7213f, 6.345493f, 289.4224f));
                lbMapPath.minPathWidth = lbMapPath.lbPath.GetMinWidth();
                lbMapPath.lbPath.RefreshPathHeights(landscape);
                // Create Mesh for Path
                if (lbMapPath.lbPath.CreateMeshFromPath(landscape))
                {
                    Vector3 meshPosition = new Vector3(0f, lbMapPath.lbPath.meshYOffset, 0f);
                    Transform meshTransform = LBMeshOperations.AddMeshToScene(lbMapPath.lbPath.lbMesh, meshPosition, lbMapPath.lbPath.pathName + " Mesh", lbMapPath.transform, lbMapPath.meshMaterial, true, true);
                    if (meshTransform != null) { }
                }
            }
        }
        #endregion

        // Create a list of LBTerrainTexture objects
        // These contain the textures and normal maps but also the rules for applying them to the terrain
        landscape.terrainTexturesList = new List<LBTerrainTexture>();

        // Populate the list by creating temporary LBTerrainTexture objects and adjusting their settings,
        // then adding each one into the list

        // Update the textures
        LBTerrainTexture tempTerrainTexture = new LBTerrainTexture();
        if (tempTerrainTexture != null)
        {
            // Forest Floor
            tempTerrainTexture.texture = texture1;
            tempTerrainTexture.normalMap = texture1NM;
            tempTerrainTexture.tileSize = Vector2.one * 15f;
            tempTerrainTexture.useNoise = false;
            tempTerrainTexture.texturingMode = LBTerrainTexture.TexturingMode.ConstantInfluence;
            tempTerrainTexture.strength = 0.04f;
            landscape.terrainTexturesList.Add(tempTerrainTexture);

            // Rock Layered
            tempTerrainTexture = new LBTerrainTexture();
            tempTerrainTexture.texture = texture2;
            tempTerrainTexture.normalMap = texture2NM;
            tempTerrainTexture.tileSize = Vector2.one * 100f;
            tempTerrainTexture.texturingMode = LBTerrainTexture.TexturingMode.HeightAndInclination;
            tempTerrainTexture.minHeight = 400f / terrainHeight;
            tempTerrainTexture.maxHeight = 1000f / terrainHeight;
            tempTerrainTexture.minInclination = 30f;
            tempTerrainTexture.maxInclination = 90f;
            tempTerrainTexture.useNoise = true;
            tempTerrainTexture.noiseTileSize = 100f;
            tempTerrainTexture.strength = 1f;
            landscape.terrainTexturesList.Add(tempTerrainTexture);

            // Rock1
            tempTerrainTexture = new LBTerrainTexture();
            tempTerrainTexture.texture = texture3;
            tempTerrainTexture.normalMap = texture3NM;
            tempTerrainTexture.tileSize = Vector2.one * 100f;
            tempTerrainTexture.texturingMode = LBTerrainTexture.TexturingMode.HeightAndInclination;
            tempTerrainTexture.minHeight = 10f / terrainHeight;
            tempTerrainTexture.maxHeight = 1000f / terrainHeight;
            tempTerrainTexture.minInclination = 30f;
            tempTerrainTexture.maxInclination = 60f;
            tempTerrainTexture.useNoise = true;
            tempTerrainTexture.noiseTileSize = 100f;
            tempTerrainTexture.strength = 1f;
            landscape.terrainTexturesList.Add(tempTerrainTexture);

            // CliffBlue
            tempTerrainTexture = new LBTerrainTexture();
            tempTerrainTexture.texture = texture4;
            tempTerrainTexture.normalMap = texture4NM;
            tempTerrainTexture.tileSize = Vector2.one * 100f;
            tempTerrainTexture.texturingMode = LBTerrainTexture.TexturingMode.Height;
            tempTerrainTexture.minHeight = 100f / terrainHeight;
            tempTerrainTexture.maxHeight = 1000f / terrainHeight;
            tempTerrainTexture.useNoise = true;
            tempTerrainTexture.noiseTileSize = 10f;
            tempTerrainTexture.strength = 0.5f;
            landscape.terrainTexturesList.Add(tempTerrainTexture);

            // Good Dirt - FUTURE generate Map at runtime from slightly wider path and texture edges
            //tempTerrainTexture = new LBTerrainTexture();
            //tempTerrainTexture.texture = texture5;
            //tempTerrainTexture.normalMap = texture5NM;
            //tempTerrainTexture.tileSize = Vector2.one * 10f;
            //tempTerrainTexture.texturingMode = LBTerrainTexture.TexturingMode.Map;
            //tempTerrainTexture.map = ....
            //tempTerrainTexture.mapIsPath = true;
            //tempTerrainTexture.useAdvancedMapTolerance = false;
            //tempTerrainTexture.mapInverse = false;
            //tempTerrainTexture.useNoise = false;
            //tempTerrainTexture.strength = 1f;
            //landscape.terrainTexturesList.Add(tempTerrainTexture);

            // Texture the terrains
            landscape.ApplyTextures(true, true);
        }

        // Add a tree type
        LBTerrainTree terrainTree1 = new LBTerrainTree();
        if (terrainTree1 != null && treePrefab1 != null)
        {
            // Set tree type options as required. See LBTerrainTree class constructor for default settings
            terrainTree1.prefab = treePrefab1.gameObject;
            terrainTree1.maxTreesPerSqrKm = 140;
            terrainTree1.minScale = 2f;
            terrainTree1.maxScale = 3f;
            terrainTree1.treeScalingMode = LBTerrainTree.TreeScalingMode.RandomScaling;
            terrainTree1.lockWidthToHeight = true;
            terrainTree1.treePlacingMode = LBTerrainTree.TreePlacingMode.HeightAndInclination;
            terrainTree1.minInclination = 0f;
            terrainTree1.maxInclination = 30f;
            terrainTree1.minHeight = 0f;
            terrainTree1.maxHeight = 200f / terrainHeight;
            terrainTree1.useNoise = false;

            //// Create a new filter, which will be used for the Stencil Layer
            //// This demonstrates how to apply a stencil layer to the placement of trees
            //LBFilter lbFilterTree = LBFilter.CreateFilter(lbStencil, "Hills Layer 1", true);

            //if (lbFilterTree != null)
            //{
            //    // If the list of filters isn't defined, create an empty list 
            //    if (terrainTree1.filterList == null) { terrainTree1.filterList = new List<LBFilter>(); }

            //    // Add the Stencil Layer to the list of Tree Filters
            //    terrainTree1.filterList.Add(lbFilterTree);
            //}

            // Add the tree type configuration to the landscape
            landscape.terrainTreesList.Add(terrainTree1);
        }

        LBTerrainTree terrainTree2 = new LBTerrainTree();
        if (terrainTree2 != null && treePrefab1 != null)
        {
            // Set tree type options as required. See LBTerrainTree class constructor for default settings
            terrainTree2.prefab = treePrefab2.gameObject;
            terrainTree2.maxTreesPerSqrKm = 140;
            terrainTree2.minScale = 2f;
            terrainTree2.maxScale = 3f;
            terrainTree2.treeScalingMode = LBTerrainTree.TreeScalingMode.RandomScaling;
            terrainTree2.lockWidthToHeight = true;
            terrainTree2.treePlacingMode = LBTerrainTree.TreePlacingMode.HeightAndInclination;
            terrainTree2.minInclination = 0f;
            terrainTree2.maxInclination = 30f;
            terrainTree2.minHeight = 0f;
            terrainTree2.maxHeight = 200f / terrainHeight;
            terrainTree2.useNoise = false;

            // Add the tree type configuration to the landscape
            landscape.terrainTreesList.Add(terrainTree2);
        }

        // Populate the landscape with the trees
        landscape.treesHaveColliders = true;
        landscape.treePlacementSpeed = LBTerrainTree.TreePlacementSpeed.FastPlacement;
        landscape.ApplyTrees(true, true);

        // Add some rock prefabs to the landscape
        LBLandscapeMesh lbLandscapeMesh = new LBLandscapeMesh();
        if (lbLandscapeMesh != null && rockPrefab != null)
        {
            lbLandscapeMesh.usePrefab = true;
            lbLandscapeMesh.prefab = rockPrefab;
            // Set mesh options as required. See LBLandscapeMesh class constructor for default settings
            lbLandscapeMesh.maxMeshes = 50;
            lbLandscapeMesh.meshPlacingMode = LBLandscapeMesh.MeshPlacingMode.ConstantInfluence;
            // Sink the rocks into the ground by 0.5m
            lbLandscapeMesh.offset = new Vector3(0f, -0.5f, 0f);
            // Group the rocks together
            //lbLandscapeMesh.isClustered = true;
            //lbLandscapeMesh.clusterDensity = 0.1f;
            //lbLandscapeMesh.clusterDistance = 10f;

            landscape.landscapeMeshList.Add(lbLandscapeMesh);

            landscape.meshPlacementSpeed = LBLandscapeMesh.MeshPlacementSpeed.FastPlacement;
            //landscape.ApplyMeshes(true, true);
        }

        // Get the first Camera Animator in the scene, snap the camera path to the new terrain,
        // and start moving the camera along the camera path.
        LBCameraAnimator lbCameraAnimator = LBCameraAnimator.GetFirstCameraAnimatorInLandscape(landscape);
        if (lbCameraAnimator == null) { Debug.LogWarning("GetFirstCameraAnimatorInLandscape returned null"); }
        else
        {
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
                lbPath.heightAboveTerrain = 5f;
                lbPath.snapToTerrain = true;
                lbPath.RefreshPathHeights(landscape);

                // Start the camera moving from the start of the path.
                lbCameraAnimator.BeginAnimation(true, 0f);
            }
        }

        // Display the total time taken to generate the landscape (usually for debugging purposes)
        Debug.Log("Time taken to generate landscape: " + (Time.realtimeSinceStartup - generationStartTime).ToString("00.00") + " seconds.");
    }
}
