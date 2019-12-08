using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LandscapeBuilder
{
    [System.Serializable]
    public class LBGroupTexture
    {
        #region Public Variables

        /// <summary>
        /// Reference to a LBTerrainTexture instance
        /// </summary>
        public string lbTerrainTextureGUID;
        public bool isWholeGroup;
        public bool showInEditor;

        /// <summary>
        /// The minimum distance over which the texture is blended with the surroundings outside the clearing.
        /// Normalised to the radius of the group. If value = 0, there is no blending
        /// and the texture stops at the edge of the clearing. If > 0 it blends the blenddist
        /// x Radius outwards from the group clearing edge.
        /// </summary>
        public float minBlendDist;

        /// <summary>
        /// Zone edge blend in metres
        /// </summary>
        public float edgeBlendDist;

        public float strength;
        public bool useNoise;
        public float noiseTileSize;
        public float noiseOffset;

        public List<string> zoneGUIDList;

        #endregion

        #region Constructors

        // Basic class constructor
        public LBGroupTexture()
        {
            SetClassDefaults();
        }

        // Clone constructor
        public LBGroupTexture(LBGroupTexture lbGroupTexture)
        {
            if (lbGroupTexture == null) { SetClassDefaults(); }
            else
            {
                lbTerrainTextureGUID = lbGroupTexture.lbTerrainTextureGUID;
                isWholeGroup = lbGroupTexture.isWholeGroup;
                showInEditor = lbGroupTexture.showInEditor;
                minBlendDist = lbGroupTexture.minBlendDist;
                edgeBlendDist = lbGroupTexture.edgeBlendDist;
                strength = lbGroupTexture.strength;
                useNoise = lbGroupTexture.useNoise;
                noiseTileSize = lbGroupTexture.noiseTileSize;
                noiseOffset = lbGroupTexture.noiseOffset;

                if (lbGroupTexture.zoneGUIDList == null) { zoneGUIDList = new List<string>(); }
                else { zoneGUIDList = new List<string>(lbGroupTexture.zoneGUIDList); }
            }
        }

        #endregion

        #region Private non-static Methods

        /// <summary>
        ///  Set the default values for a new LBGroupTexture class instance
        /// </summary>
        private void SetClassDefaults()
        {
            lbTerrainTextureGUID = string.Empty;
            isWholeGroup = true;
            showInEditor = true;
            minBlendDist = 0.1f;
            edgeBlendDist = 2f;
            strength = 1f;
            useNoise = false;
            noiseTileSize = 100f;

            zoneGUIDList = new List<string>();
        }

        #endregion
    }
}