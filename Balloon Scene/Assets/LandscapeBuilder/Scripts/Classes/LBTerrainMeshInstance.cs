using UnityEngine;
using System.Collections;

namespace LandscapeBuilder
{
    /// <summary>
    /// Stores placed normalised mesh position in terrain, minProximity and minProximity Squared pre-calc'd values
    /// </summary>
    public class LBTerrainMeshInstance
    {
        // Normalised mesh position in terrain
        public Vector3 position;
        public float minProximity;
        public float minProximityNormalisedSquared { get; private set; }

        public LBTerrainMeshInstance(Vector3 meshPosition, float meshMinProximity, float meshMinProximitySquared)
        {
            position = meshPosition;
            minProximity = meshMinProximity;
            minProximityNormalisedSquared = meshMinProximitySquared;
        }
    }
}