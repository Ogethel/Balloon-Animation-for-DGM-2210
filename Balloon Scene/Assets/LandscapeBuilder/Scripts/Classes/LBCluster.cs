using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace LandscapeBuilder
{
    /// <summary>
    /// LBCluster is a group of placement position attributes typically
    /// used for placing objects like meshes or prefabs in small groups or
    /// clusters near each other in a landscape.
    /// See LBLandscapeTerrain.PopulateTerrainWithMeshes(..)
    /// </summary>
    public class LBCluster
    {
        #region Public Variables and Properties
        public List<Vector3> positionList;
        public List<Vector3> scaleList;

        #endregion

        #region Constructors

        public LBCluster()
        {
            positionList = new List<Vector3>();
            scaleList = new List<Vector3>();
        }

        #endregion

        public void PopulateCluster(Vector3 position, Rect boundary, float minScale, float maxScale, float density, float resolution)
        {
            // Empty the cluster
            if (positionList == null) { positionList = new List<Vector3>(); }
            else { positionList.Clear(); }

            if (scaleList == null) { scaleList = new List<Vector3>(); }
            else { scaleList.Clear(); }

            Vector3 origin = new Vector3(position.x - (boundary.width / 2f), position.y, position.z - (boundary.height / 2f));

            float noiseOffset = UnityEngine.Random.Range(0f, boundary.width);
            Vector2 noiseCoords = Vector2.zero;
            float clusterNoiseTileSize = boundary.width / 5f;

            // Convert values from float to int to avoid a float for(;;) loop
            int resolutionInt = Mathf.FloorToInt(resolution * 1000f);
            int widthInt = Mathf.FloorToInt(boundary.width * 1000);
            int lengthInt = Mathf.FloorToInt(boundary.height * 1000);

            float xF = 0f, zF = 0f;

            for (int x = 0; x < widthInt; x += resolutionInt)
            {
                for (int z = 0; z < lengthInt; z += resolutionInt)
                {
                    // Convert back to floats
                    xF = x / 1000f;
                    zF = z / 1000f;

                    // Get a perlin noise value for this point in the cluster
                    noiseCoords.x = xF + noiseOffset;
                    noiseCoords.y = zF + noiseOffset;
                    float noiseValue = Mathf.Abs(LBNoise.PerlinFractalNoise(noiseCoords.x / clusterNoiseTileSize, noiseCoords.y / clusterNoiseTileSize, 5) - 0.5f) * 4f;
                    // If the noise value is less than density don't create the position
                    if (noiseValue < density)
                    {
                        positionList.Add(new Vector3(origin.x + xF, origin.y, origin.z + zF));
                        scaleList.Add(Vector3.one * UnityEngine.Random.Range(minScale, maxScale));
                    }
                }
            }
        }
    }
}