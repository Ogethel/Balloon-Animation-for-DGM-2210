using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LandscapeBuilder
{
    [System.Serializable]
    public class LBGroupGrass
    {
        #region Public Variables

        /// <summary>
        /// Reference to a LBTerrainGrass instance
        /// </summary>
        public string lbTerrainGrassGUID;
        public bool isWholeGroup;

        public bool showInEditor;

        /// <summary>
        /// The minimum distance over which the grass is blended with the surroundings outside the clearing.
        /// Normalised to the radius of the group. If value = 0, there is no blending
        /// and grass stops at the edge of the clearing. If > 0 it blends the blenddist
        /// x Radius outwards from the group clearing edge.
        /// </summary>
        public float minBlendDist;

        /// <summary>
        /// Zone edge blend in metres
        /// </summary>
        public float edgeBlendDist;

        // min,max density of a grass patch
        public int minDensity;
        public int maxDensity;
        public bool useNoise;
        public float noiseTileSize;
        public float noiseOffset;
        public int noiseOctaves;
        public float grassPlacementCutoff;

        public List<string> zoneGUIDList;

        #endregion

        #region Constructors

        // Basic class constructor
        public LBGroupGrass()
        {
            SetClassDefaults();
        }

        // Clone constructor
        public LBGroupGrass(LBGroupGrass lbGroupGrass)
        {
            if (lbGroupGrass == null) { SetClassDefaults(); }
            else
            {
                lbTerrainGrassGUID = lbGroupGrass.lbTerrainGrassGUID;
                isWholeGroup = lbGroupGrass.isWholeGroup;
                showInEditor = lbGroupGrass.showInEditor;
                minBlendDist = lbGroupGrass.minBlendDist;
                edgeBlendDist = lbGroupGrass.edgeBlendDist;
                minDensity = lbGroupGrass.minDensity;
                maxDensity = lbGroupGrass.maxDensity;
                useNoise = lbGroupGrass.useNoise;
                noiseTileSize = lbGroupGrass.noiseTileSize;
                noiseOffset = lbGroupGrass.noiseOffset;
                noiseOctaves = lbGroupGrass.noiseOctaves;
                grassPlacementCutoff = lbGroupGrass.grassPlacementCutoff;

                if (lbGroupGrass.zoneGUIDList == null) { zoneGUIDList = new List<string>(); }
                else { zoneGUIDList = new List<string>(lbGroupGrass.zoneGUIDList); }
            }
        }

        #endregion

        #region Private non-static Methods

        /// <summary>
        ///  Set the default values for a new LBGroupGrass class instance
        /// </summary>
        private void SetClassDefaults()
        {
            lbTerrainGrassGUID = string.Empty;
            isWholeGroup = true;
            showInEditor = true;
            minBlendDist = 0.1f;
            edgeBlendDist = 2f;
            minDensity = 0;
            maxDensity = 1;
            useNoise = false;
            noiseTileSize = 10f;
            noiseOctaves = 5;
            noiseOffset = 1f;  // Gets set at placement time
            grassPlacementCutoff = 0.5f;

            zoneGUIDList = new List<string>();
        }

        #endregion
    }
}