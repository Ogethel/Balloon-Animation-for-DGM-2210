// Landscape Builder. Copyright (c) 2016-2019 SCSM Pty Ltd. All rights reserved.
using UnityEngine;

namespace LandscapeBuilder
{
    /// <summary>
    /// Simple script to find the first active landscape in a scene and update the terrain neighbours
    /// to avoid terrain LOD issues. Add it as a component to any gameobject in the scene.
    /// </summary>
    public class LBSetNeighbours : MonoBehaviour
    {
        void Start()
        {
            LBLandscape landscape = FindObjectOfType<LBLandscape>();
            if (landscape != null) { landscape.SetLandscapeTerrains(true); landscape.SetTerrainNeighbours(); }
        }
    }
}