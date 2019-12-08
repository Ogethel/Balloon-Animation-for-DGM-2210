// Landscape Builder. Copyright (c) 2016-2019 SCSM Pty Ltd. All rights reserved.
using UnityEngine;

namespace LandscapeBuilder
{
    [System.Serializable]
    public class LBBiome
    {
        #region Public Variables
        // Added 2.2.1 used by Vegetation Studio Pro to indentify
        // the biome (vegetation package) in index of ones available
        public int biomeIndex;
        public float minBlendDist;

        #endregion

        #region Constructors

        // Basic class constructor
        public LBBiome()
        {
            SetClassDefaults();
        }

        // Clone constructor
        public LBBiome(LBBiome lbBiome)
        {
            if (lbBiome == null) { SetClassDefaults(); }
            else
            {
                biomeIndex = lbBiome.biomeIndex;
                minBlendDist = lbBiome.minBlendDist;
            }
        }

        #endregion

        #region Non-Static Private Methods

        private void SetClassDefaults()
        {
            biomeIndex = 0;
            minBlendDist = 1f;
        }

        #endregion
    }
}