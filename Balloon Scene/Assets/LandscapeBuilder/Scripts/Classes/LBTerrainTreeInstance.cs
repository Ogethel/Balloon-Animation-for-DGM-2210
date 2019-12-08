using UnityEngine;
using System.Collections;

namespace LandscapeBuilder
{
    /// <summary>
    /// Stores placed normalised tree position in terrain, minProximity and minProximity Squared pre-calc'd values
    /// </summary>
    [System.Serializable]
    public class LBTerrainTreeInstance
    {
        #region Public variables and Properties
        // Normalised tree position in terrain
        public Vector3 position;
        public float minProximity;
        public float minProximityNormalisedSquared { get; private set; }
        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor for serialization. If you need to set minProximityNormalisedSquared outside this
        /// class, call the LBTerrainTreeInstance(treePosition, treeMinProximity, treeMinProximitySquared) constructor.
        /// </summary>
        public LBTerrainTreeInstance()
        {
            position = Vector3.zero;
            minProximity = 100f;
            minProximityNormalisedSquared = 0;
        }

        public LBTerrainTreeInstance(Vector3 treePosition, float treeMinProximity, float treeMinProximitySquared)
        {
            position = treePosition;
            minProximity = treeMinProximity;
            minProximityNormalisedSquared = treeMinProximitySquared;
        }
        #endregion
    }
}