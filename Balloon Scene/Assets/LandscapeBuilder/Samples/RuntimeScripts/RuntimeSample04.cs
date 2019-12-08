using UnityEngine;
using System.Collections.Generic;
using LandscapeBuilder;

/// <summary>
/// Example script that creates a landscape entirely at runtime
/// It creates the topography and textures the landscape
/// It uses the new Layer-based topography generation type.
/// Water is added to the scene at Runtime.
/// NOTE: This sample creates 1 or more terrains with size of w:2000 l:2000 h:2000.
/// There is no error checking for different sized terrains.
/// RECOMMEND: Start with RuntimeSample07 or newer, then work backwards to find what you need.
/// </summary>
public class RuntimeSample04 : MonoBehaviour
{
    #region Public variables and properties
    // References to texture files
    public Texture2D grassHillTexture;
    public Texture2D grassHillNormalMap;
    public Texture2D rockLayeredTexture;
    public Texture2D rockLayeredNormalMap;
    public Vector2 landscapeSize = Vector2.one * 4000f;

    // References to tree
    public Transform treePrefab;

    // Stencil Layer input images
    public Texture2D stencilLayerTex1;

    public bool IsMaskingOn = false;
    public Transform waterPrefab;
    public float waterHeight = 398f;

    // Reference to prefabs
    public GameObject rockPrefab;
    #endregion

    #region Private variables
    private LBLandscape landscape = null;
    private float terrainHeight = 0f;
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

        #endregion

        #region Create the terrains
        int terrainNumber = 0;

        for (float tx = 0f; tx < landscapeSize.x - 1f; tx += 2000f)
        {
            for (float ty = 0f; ty < landscapeSize.y - 1f; ty += 2000f)
            {
                // Create a new gameobject
                GameObject terrainObj = new GameObject("Runtime Terrain " + (terrainNumber++).ToString("000"));

                // Correctly parent and position the terrain
                terrainObj.transform.parent = this.transform;
                terrainObj.transform.localPosition = new Vector3(tx, 0f, ty);

                // Add a terrain component
                Terrain newTerrain = terrainObj.AddComponent<Terrain>();

                // Set terrain settings (depending on your situtation, you may need to set more or less than I have in this example)
                newTerrain.heightmapPixelError = 1;
                newTerrain.basemapDistance = 5000f;
                newTerrain.treeDistance = 5000f;
                newTerrain.treeBillboardDistance = 100f;
                newTerrain.detailObjectDistance = 150f;
                newTerrain.treeCrossFadeLength = 25f;

                // Set terrain data settings (same as above comment)
                TerrainData newTerrainData = new TerrainData();

                // One thing to note here is that modfiying the heightmap resolution not only clears all terrain height data,
                // it also scales up or down the size of the terrain. So you should always set the heightmap resolution
                // BEFORE you set the terrain size
                newTerrainData.heightmapResolution = 513;
                newTerrainData.size = Vector3.one * 2000f;
                newTerrainData.SetDetailResolution(1024, 16);
                newTerrain.terrainData = newTerrainData;

                // Set up the terrain collider
                TerrainCollider newTerrainCol = terrainObj.AddComponent<TerrainCollider>();
                newTerrainCol.terrainData = newTerrainData;
            }
        }
        #endregion

        landscape.SetLandscapeTerrains(true);
        terrainHeight = landscape.GetLandscapeTerrainHeight();

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

        #region Add a new Stencil to the (empty) landscape
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

        // NOTE: This sample uses a topography mask. This is not a requirement

        // Create the distance to centre mask using an animation curve
        AnimationCurve distanceToCentreMask = new AnimationCurve();
        int keyInt = distanceToCentreMask.AddKey(0f, 1f);
        keyInt = distanceToCentreMask.AddKey(0.529f, 0.959f);
        keyInt = distanceToCentreMask.AddKey(1f, 0f);
        Keyframe[] curveKeys = distanceToCentreMask.keys;
        curveKeys[0].inTangent = 0f;
        curveKeys[0].outTangent = 0f;
        curveKeys[1].inTangent = -0.25f;
        curveKeys[1].outTangent = -0.25f;
        curveKeys[2].inTangent = 0f;
        curveKeys[2].outTangent = 0f;
        distanceToCentreMask = new AnimationCurve(curveKeys);

        // Assign the topography mask to the LBLandscape instance
        landscape.distanceToCentreMask = distanceToCentreMask;
        if (IsMaskingOn) { landscape.topographyMaskMode = LBLandscape.MaskMode.DistanceToCentre; }
        else { landscape.topographyMaskMode = LBLandscape.MaskMode.None; }
        landscape.maskWarpAmount = 0f;
        landscape.maskNoiseTileSize = 10000f;
        landscape.maskNoiseCurveModifier = AnimationCurve.Linear(0f, 0.5f, 1f, 1f);
        landscape.maskNoiseOffsetX = 0f;
        landscape.maskNoiseOffsetY = 0f;

        // Avoid warning of keyInt not being used.
        if (keyInt == 0) { }

        // Create the Topography Layers
        // You can mix and match Perlin and Image layers
        landscape.topographyLayersList = new List<LBLayer>();
        if (landscape.topographyLayersList != null)
        {
            // Add one or more Base layers
            LBLayer lbBaseLayer1 = new LBLayer();
            if (lbBaseLayer1 != null)
            {
                lbBaseLayer1 = LBLayer.SetLayerFromPreset(LBLayer.LayerPreset.DesertFloorBase);
                landscape.topographyLayersList.Add(lbBaseLayer1);
            }

            // Add one or more Additive layers
            LBLayer lbAdditiveLayer1 = new LBLayer();
            if (lbAdditiveLayer1 != null)
            {
                // You can manually configure a layer, or use a preset then modify it.
                lbAdditiveLayer1 = LBLayer.SetLayerFromPreset(LBLayer.LayerPreset.RollingHillsBase);
                // If using using a different type of preset, must set the type after applying preset
                lbAdditiveLayer1.type = LBLayer.LayerType.PerlinAdditive;
                // Optionally override the preset settings
                //lbAdditiveLayer1.noiseTileSize = 5000f;
                //lbAdditiveLayer1.octaves = 8;
                //lbAdditiveLayer1.lacunarity = 1.92f;
                //lbAdditiveLayer1.gain = 0.45f;
                //lbAdditiveLayer1.warpAmount = 0;
                lbAdditiveLayer1.removeBaseNoise = true;
                lbAdditiveLayer1.heightScale = 0.35f;
                lbAdditiveLayer1.additiveAmount = 0.95f;
                lbAdditiveLayer1.additiveCurve = LBLayer.CreateAdditiveCurve(lbAdditiveLayer1.additiveAmount);
                //lbAdditiveLayer1.perOctaveCurveModifierPresets = new List<LBCurve.CurvePreset>();
                //lbAdditiveLayer1.perOctaveCurveModifierPresets.Add(LBCurve.CurvePreset.DoubleRidged);

                // Create a new topography layer filter, which will be used for the Stencil Layer
                LBLayerFilter lbLayerFilter = LBLayerFilter.CreateLayerFilter(lbStencil, "Hills Layer 1", true);

                if (lbLayerFilter != null)
                {
                    // If the list of filters isn't defined, create an empty list 
                    if (lbAdditiveLayer1.filters == null) { lbAdditiveLayer1.filters = new List<LBLayerFilter>(); }

                    // Add the Stencil Layer to the list of Topography LayerFilters
                    lbAdditiveLayer1.filters.Add(lbLayerFilter);
                }

                landscape.topographyLayersList.Add(lbAdditiveLayer1);
            }

            // Add a detail layer
            LBLayer lbDetailLayer1 = new LBLayer();
            if (lbDetailLayer1 != null)
            {
                lbDetailLayer1 = LBLayer.SetLayerFromPreset(LBLayer.LayerPreset.HillsDetail);
                landscape.topographyLayersList.Add(lbDetailLayer1);
            }
        }

        // Create the terrain topographies
        landscape.ApplyTopography(false, true);

        // Get the first Camera Animator in the scene, snap the camera path to the new terrain,
        // and start moving the camera along the camera path.
        LBCameraAnimator lbCameraAnimator = LBCameraAnimator.GetFirstCameraAnimatorInLandscape(landscape);
        if (lbCameraAnimator == null) { Debug.LogWarning("GetFirstCameraAnimatorInLandscape returned null"); }
        else
        {
            // Get the LBPath instance which contains the points along the camera path
            LBPath lbPath = lbCameraAnimator.cameraPath.lbPath;
            if (lbPath == null) { Debug.LogWarning("Could not find the camera path instance for the animator"); }
            else
            {
                // Optionally update the path points to match the terrain
                lbPath.heightAboveTerrain = 15f;
                lbPath.snapToTerrain = true;
                lbPath.RefreshPathHeights(landscape);

                // Start the camera moving from the start of the path.
                lbCameraAnimator.BeginAnimation(true, 0f);
            }
        }

        // Add some water the scene
        int numberOfMeshes = 0;
        Vector2 waterSize = new Vector2(landscape.size.x * 2f, landscape.size.y * 2f);
        // The primary water body is placed in the centre of the landscape
        Vector3 waterPosition = landscape.start + (0.5f * new Vector3(landscape.size.x, 0f, landscape.size.y));

        // Populate the paramaters to pass to AddWaterToScene()
        LBWaterParameters lbWaterParms = new LBWaterParameters();
        lbWaterParms.landscape = landscape;
        lbWaterParms.landscapeGameObject = landscape.gameObject;
        lbWaterParms.waterPosition = waterPosition;
        lbWaterParms.waterSize = waterSize;
        lbWaterParms.waterIsPrimary = true;
        lbWaterParms.waterHeight = waterHeight;
        lbWaterParms.waterPrefab = waterPrefab;
        lbWaterParms.keepPrefabAspectRatio = true;
        lbWaterParms.waterResizingMode = LBWater.WaterResizingMode.StandardAssets;
        lbWaterParms.waterMaxMeshThreshold = 5000;
        lbWaterParms.waterMainCamera = Camera.main;
        lbWaterParms.waterCausticsPrefabList = null;
        lbWaterParms.isRiver = false;
        lbWaterParms.lbLighting = GameObject.FindObjectOfType<LBLighting>();

        LBWater addedWater = LBWaterOperations.AddWaterToScene(lbWaterParms, ref numberOfMeshes);

        if (addedWater == null)
        {
            Debug.LogWarning("Could not add water to scene");
        }

        // Create a list of LBTerrainTexture objects
        // These contain the textures and normal maps but also the rules for applying them to the terrain
        landscape.terrainTexturesList = new List<LBTerrainTexture>();

        // Populate the list by creating temporary LBTerrainTexture objects and adjusting their settings,
        // then adding each one into the list

        // Grass Hill texture
        LBTerrainTexture tempTerrainTexture = new LBTerrainTexture();
        if (tempTerrainTexture != null)
        {
            tempTerrainTexture.texture = grassHillTexture;
            tempTerrainTexture.normalMap = grassHillNormalMap;

            tempTerrainTexture.tileSize = Vector2.one * 25f;
            tempTerrainTexture.minInclination = 0f;
            tempTerrainTexture.maxInclination = 45f;
            tempTerrainTexture.useNoise = true;
            tempTerrainTexture.noiseTileSize = 100f;
            tempTerrainTexture.texturingMode = LBTerrainTexture.TexturingMode.Inclination;
            landscape.terrainTexturesList.Add(tempTerrainTexture);

            // Rock Layered texture
            tempTerrainTexture = new LBTerrainTexture();
            tempTerrainTexture.texture = rockLayeredTexture;
            tempTerrainTexture.normalMap = rockLayeredNormalMap;
            tempTerrainTexture.tileSize = Vector2.one * 100f;
            tempTerrainTexture.minInclination = 30f;
            tempTerrainTexture.maxInclination = 90f;
            tempTerrainTexture.useNoise = true;
            tempTerrainTexture.noiseTileSize = 100f;
            tempTerrainTexture.texturingMode = LBTerrainTexture.TexturingMode.Inclination;
            landscape.terrainTexturesList.Add(tempTerrainTexture);

            // Texture the terrains
            landscape.ApplyTextures(true, true);
        }

        // Add a tree type
        LBTerrainTree tempTerrainTree = new LBTerrainTree();
        if (tempTerrainTree != null && treePrefab != null)
        {
            // Set tree type options as required. See LBTerrainTree class constructor for default settings
            tempTerrainTree.prefab = treePrefab.gameObject;
            tempTerrainTree.maxTreesPerSqrKm = 100;
            tempTerrainTree.treePlacingMode = LBTerrainTree.TreePlacingMode.HeightAndInclination;
            tempTerrainTree.minInclination = 0f;
            tempTerrainTree.maxInclination = 30f;
            tempTerrainTree.minHeight = (waterHeight + 2f) / terrainHeight;

            // Create a new filter, which will be used for the Stencil Layer
            // This demonstrates how to apply a stencil layer to the placement of trees
            LBFilter lbFilterTree = LBFilter.CreateFilter(lbStencil, "Hills Layer 1", true);

            if (lbFilterTree != null)
            {
                // If the list of filters isn't defined, create an empty list 
                if (tempTerrainTree.filterList == null) { tempTerrainTree.filterList = new List<LBFilter>(); }

                // Add the Stencil Layer to the list of Tree Filters
                tempTerrainTree.filterList.Add(lbFilterTree);
            }

            // Add the tree type configuration to the landscape
            landscape.terrainTreesList.Add(tempTerrainTree);

            // Populate the landscape with the trees
            landscape.ApplyTrees(true, true);
        }

        // Add some rock prefabs to the landscape
        LBLandscapeMesh lbLandscapeMesh = new LBLandscapeMesh();
        if (lbLandscapeMesh != null && rockPrefab != null)
        {
            lbLandscapeMesh.usePrefab = true;
            lbLandscapeMesh.prefab = rockPrefab;
            // Set mesh options as required. See LBLandscapeMesh class constructor for default settings
            lbLandscapeMesh.maxMeshes = 100;
            // Sink the rocks into the ground by 0.5m
            lbLandscapeMesh.offset = new Vector3(0f, -0.5f, 0f);
            // Place the rocks near the water's edge
            lbLandscapeMesh.meshPlacingMode = LBLandscapeMesh.MeshPlacingMode.HeightAndInclination;
            lbLandscapeMesh.minHeight = (waterHeight + 1f) / terrainHeight;
            lbLandscapeMesh.maxHeight = lbLandscapeMesh.minHeight + (10f / terrainHeight);
            lbLandscapeMesh.maxInclination = 12.5f;
            lbLandscapeMesh.useNoise = true;
            // Group the rocks together
            lbLandscapeMesh.isClustered = true;
            lbLandscapeMesh.clusterDensity = 0.1f;
            lbLandscapeMesh.clusterDistance = 10f;
            //lbLandscapeMesh.minProximity = 10f;

            landscape.landscapeMeshList.Add(lbLandscapeMesh);

            landscape.ApplyMeshes(true, true);
        }

        // Display the total time taken to generate the landscape (usually for debugging purposes)
        Debug.Log("Time taken to generate landscape: " + (Time.realtimeSinceStartup - generationStartTime).ToString("00.00") + " seconds.");
    }
}

