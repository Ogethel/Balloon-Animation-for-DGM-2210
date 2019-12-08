using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace LandscapeBuilder
{
    [System.Serializable]
    public class LBWaterCaustics
    {
        // Landscape Builder Water Caustic Class
        // Used to store a list of Caustic objects for water components
        // A list is stored in the LBWater class which are children of
        // a LBLandscape class in the scene
        // First used with AQUAS Water Set v1.2.2
        // Added in LB version 1.2.1 Beta 9a

        public string waterCausticPrefabName;
        public string waterCausticPrefabPath;
        // This is the primary, caustics instance. It doesn't refer to a primary water body - which is different
        public bool isPrimaryWaterCaustics;

        // AQUAS requires separate copies of the material for each body of water
        public string waterCausticsMaterialPath;

        // Constructor
        public LBWaterCaustics()
        {
            this.waterCausticPrefabName = string.Empty;
            this.waterCausticPrefabPath = string.Empty;
            this.isPrimaryWaterCaustics = false;
            this.waterCausticsMaterialPath = string.Empty;
        }
    }
}