using UnityEngine;
using System.Collections.Generic;
using LandscapeBuilder;

public class RuntimeSample01 : MonoBehaviour
{
    // WARNING: For Unity 2019.2+ use RuntimeSample03-07.
    // Example script that creates a landscape entirely at runtime
    // It creates the topography and textures the landscape
    // This basic example doesn't create a LBLandscape script instance in the scene
    // For examples that do that - see RuntimeSample03-07.
    // RECOMMEND: Start with RuntimeSample07 or newer, then work backwards to find what you need.

    // References to texture files
    public Texture2D grassHillTexture;
    public Texture2D grassHillNormalMap;
    public Texture2D rockLayeredTexture;
    public Texture2D rockLayeredNormalMap;
    public Texture2D rock1Texture;
    public Texture2D rock1NormalMap;
    public Texture2D botCliffTexture;
    public Texture2D botCliffNormalMap;
    public Vector2 landscapeSize = Vector2.one * 4000f;
    private List<Terrain> terrainsList;

    // Use this for initialization
    void Awake()
    {
        // This line just gets the starting time of the generation so that the total generation time
        // can be recorded and displayed
        float generationStartTime = Time.realtimeSinceStartup;

        RuntimeSampleHelper.RemoveDefaultCamera();

        // Create the terrains and store references to them
        terrainsList = new List<Terrain>();

        for (float tx = 0f; tx < landscapeSize.x - 1f; tx += 2000f)
        {
            for (float ty = 0f; ty < landscapeSize.y - 1f; ty += 2000f)
            {
                // Create a new gameobject
                GameObject terrainObj = new GameObject("Runtime Terrain");

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
                newTerrain.terrainData = newTerrainData;

                // Set up the terrain collider
                TerrainCollider newTerrainCol = terrainObj.AddComponent<TerrainCollider>();
                newTerrainCol.terrainData = newTerrainData;

                // Add the terrain to the list of terrains
                terrainsList.Add(newTerrain);
            }
        }

        // Create the terrain topographies
        for (int t = 0; t < terrainsList.Count; t++)
        {
            // Use the LBLandscapeTerrain.PerlinNoiseHeightmap function for value based perlin noise
            // This example is using the values from the Mountain Range preset

            terrainsList[t].terrainData = LBLandscapeTerrain.PerlinNoiseHeightmap(terrainsList[t].terrainData, terrainsList[t].transform.position,
            7, 5000f, Vector2.zero, 2f, 1.95f, 0.49f, true, false, 0, 0f, 0, LBCurve.SetCurveFromPreset(LBCurve.CurvePreset.None), 1f);
        }

        // Create a list of LBTerrainTexture objects
        // These contain the textures and normal maps but also the rules for applying them to the terrain

        List<LBTerrainTexture> terrainTexturesList = new List<LBTerrainTexture>();

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
        terrainTexturesList.Add(tempTerrainTexture);

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
        terrainTexturesList.Add(tempTerrainTexture);

        // Rock1 texture
        tempTerrainTexture = new LBTerrainTexture();
        tempTerrainTexture.texture = rock1Texture;
        tempTerrainTexture.normalMap = rock1NormalMap;
        tempTerrainTexture.tileSize = Vector2.one * 100f;
        tempTerrainTexture.minInclination = 30f;
        tempTerrainTexture.maxInclination = 60f;
        tempTerrainTexture.useNoise = true;
        tempTerrainTexture.noiseTileSize = 100f;
        tempTerrainTexture.texturingMode = LBTerrainTexture.TexturingMode.Inclination;
        terrainTexturesList.Add(tempTerrainTexture);

        // Bot Cliff texture
        tempTerrainTexture = new LBTerrainTexture();
        tempTerrainTexture.texture = botCliffTexture;
        tempTerrainTexture.normalMap = botCliffNormalMap;
        tempTerrainTexture.tileSize = Vector2.one * 100f;
        tempTerrainTexture.strength = 0.01f;
        tempTerrainTexture.useNoise = false;
        tempTerrainTexture.noiseTileSize = 100f;
        tempTerrainTexture.texturingMode = LBTerrainTexture.TexturingMode.ConstantInfluence;
        terrainTexturesList.Add(tempTerrainTexture);

        // Texture the terrains
        for (int t = 0; t < terrainsList.Count; t++)
        {
            // Use the LBLandscapeTerrain.TextureTerrain function for texturing the terrain
            terrainsList[t].terrainData = LBLandscapeTerrain.TextureTerrain(terrainsList[t].terrainData, terrainTexturesList,
             terrainsList[t].transform.position, landscapeSize, this.transform.position, false, null);
        }

        // Display the total time taken to generate the landscape (usually for debugging purposes)
        Debug.Log("Time taken to generate landscape: " + (Time.realtimeSinceStartup - generationStartTime).ToString("00.00") + " seconds.");
    }
}
