using UnityEngine;
using System.Collections.Generic;
using LandscapeBuilder;

public class RuntimeSample03 : MonoBehaviour
{
    // Example script that creates a landscape entirely at runtime
    // It creates the topography and textures the landscape
    // It uses the new Layer-based topography generation type first
    // available in version 1.3.0
    // RECOMMEND: Start with RuntimeSample07 or newer, then work backwards to find what you need.

    // References to texture files
    public Texture2D grassHillTexture;
    public Texture2D grassHillNormalMap;
    public Texture2D rockLayeredTexture;
    public Texture2D rockLayeredNormalMap;
    public Vector2 landscapeSize = Vector2.one * 4000f;

    public bool IsMaskingOn = false;
    public LBLandscape landscape = null;

    private List<Terrain> terrainsList;

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

        #endregion

        #region Create the terrains and store references to them
        terrainsList = new List<Terrain>();
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

                // One thing to note here is that modfiying the heightmap resolution not only clears all terrai height data,
                // it also scales up or down the size of the terrain. So you should always set the heightmap resolution
                // BEFORE you set the terrain size
                newTerrainData.heightmapResolution = 513;
                newTerrainData.size = Vector3.one * 2000f;
                newTerrainData.SetDetailResolution(1024, 16);
                newTerrain.terrainData = newTerrainData;

                // Set up the terrain collider
                TerrainCollider newTerrainCol = terrainObj.AddComponent<TerrainCollider>();
                newTerrainCol.terrainData = newTerrainData;

                // Add the terrain to the list of terrains
                terrainsList.Add(newTerrain);
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

        // Set the topography noise variables
        float maskWarpAmount = 0f;
        float maskNoiseTileSize = 10000f;
        float maskNoiseOffsetX = 0f;
        float maskNoiseOffsetY = 0f;

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

        AnimationCurve maskNoiseCurveModifier = AnimationCurve.Linear(0f, 0.5f, 1f, 1f);

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
                lbAdditiveLayer1 = LBLayer.SetLayerFromPreset(LBLayer.LayerPreset.MountainRangeComplexBase);
                // If using using a different type of preset, must set the type after applying preset
                lbAdditiveLayer1.type = LBLayer.LayerType.PerlinAdditive;
                // Optionally override the preset settings
                lbAdditiveLayer1.noiseTileSize = 5000f;
                lbAdditiveLayer1.octaves = 8;
                lbAdditiveLayer1.lacunarity = 1.92f;
                lbAdditiveLayer1.gain = 0.45f;
                lbAdditiveLayer1.warpAmount = 0;
                lbAdditiveLayer1.removeBaseNoise = true;
                lbAdditiveLayer1.heightScale = 1f;
                lbAdditiveLayer1.additiveAmount = 0.75f;
                lbAdditiveLayer1.additiveCurve = LBLayer.CreateAdditiveCurve(lbAdditiveLayer1.additiveAmount);
                lbAdditiveLayer1.perOctaveCurveModifierPresets = new List<LBCurve.CurvePreset>();
                lbAdditiveLayer1.perOctaveCurveModifierPresets.Add(LBCurve.CurvePreset.DoubleRidged);
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
        for (int t = 0; t < terrainsList.Count && landscape.topographyLayersList != null; t++)
        {
            // Add the topography layers            
            terrainsList[t].terrainData = LBLandscapeTerrain.HeightmapFromLayers(landscape, terrainsList[t].terrainData,
                                            terrainsList[t].transform.position, landscapeSize, landscape.transform.position, landscape.topographyLayersList);


            if (IsMaskingOn)
            {
                // Example of applying a mask to the terrain topography
                terrainsList[t].terrainData = LBLandscapeTerrain.MaskedHeightmap(terrainsList[t].terrainData,
                                                                terrainsList[t].transform.position, landscapeSize, transform.position,
                                                                1, distanceToCentreMask, maskWarpAmount, maskNoiseTileSize,
                                                                new Vector2(maskNoiseOffsetX, maskNoiseOffsetY), maskNoiseCurveModifier);
            }
        }

        // Create a list of LBTerrainTexture objects
        // These contain the textures and normal maps but also the rules for applying them to the terrain

        landscape.terrainTexturesList = new List<LBTerrainTexture>();

        // Populate the list by creating temporary LBTerrainTexture objects and adjusting their settings,
        // then adding each one into the list

        // Grass Hill texture
        LBTerrainTexture tempTerrainTexture = new LBTerrainTexture();
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
        for (int t = 0; t < terrainsList.Count; t++)
        {
            // Use the LBLandscapeTerrain.TextureTerrain function for texturing the terrain
            terrainsList[t].terrainData = LBLandscapeTerrain.TextureTerrain(terrainsList[t].terrainData, landscape.terrainTexturesList,
             terrainsList[t].transform.position, landscapeSize, this.transform.position, false, landscape);
        }

        // Display the total time taken to generate the landscape (usually for debugging purposes)
        Debug.Log("Time taken to generate landscape: " + (Time.realtimeSinceStartup - generationStartTime).ToString("00.00") + " seconds.");
    }
}
