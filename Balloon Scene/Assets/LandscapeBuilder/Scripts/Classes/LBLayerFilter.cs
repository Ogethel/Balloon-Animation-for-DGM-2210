using UnityEngine;
using System.Collections;

namespace LandscapeBuilder
{
    [System.Serializable]
    public class LBLayerFilter
    {
        // Class containing topography layer filter info

        #region Enumerations

        // Filter types
        public enum LayerFilterType
        {
            Height = 0,
            Inclination = 1,
            Map = 10,
            StencilLayer = 15
        }

        // Subset of LayerFilterType for Additive and Subtractive layers
        public enum LayerFilterTypeForAddSubLayers
        {
            Map = 10,
            StencilLayer = 15
        }

        #endregion

        #region Public Variables

        // The type of filter this is
        public LayerFilterType type;

        // Height filter variables
        public float minHeight;
        public float maxHeight;
        public AnimationCurve heightCurve;
        public LBCurve.FilterCurvePreset heightCurvePreset;

        // Steepness filter variables
        public float minInclination;
        public float maxInclination;
        public AnimationCurve inclinationCurve;
        public LBCurve.FilterCurvePreset inclinationCurvePreset;

        // Added 1.3.1 Beta 2a
        // Map filter variables
        public Texture2D map;
        public Color mapColour;
        public int mapTolerance;
        public bool mapInverse;
        // Added 1.3.2 Beta 5f
        public AnimationCurve mapToleranceBlendCurve;
        public float smoothRate;

        // Added 1.4.0 Beta 10h
        public string lbStencilGUID;
        public string lbStencilLayerGUID;

        // Temp storage for display in LandscapeBuilderWindow Editor and LBLandscapeTerrain.HeightmapFromLayers()
        [System.NonSerialized] public LBStencil lbStencil;

        // Temp storage for use in LBLandscapeTerrain.HeightmapFromLayers()
        [System.NonSerialized] public LBStencilLayer lbStencilLayer;

        // Cached for Compute Shader used in LBLandscapeTerrain.HeightmapFromLayers()
        [System.NonSerialized] public int stencilLayerResolution;

        #endregion

        #region Constructors

        // Class constructor
        public LBLayerFilter()
        {
            this.type = LayerFilterType.Height;
            this.minHeight = 0f;
            this.maxHeight = 0.5f;
            this.heightCurve = LBCurve.SetCurveFromPreset(LBCurve.FilterCurvePreset.WideRange);
            this.heightCurvePreset = LBCurve.FilterCurvePreset.WideRange;
            this.minInclination = 0f;
            this.maxInclination = 30f;
            this.inclinationCurve = LBCurve.SetCurveFromPreset(LBCurve.FilterCurvePreset.WideRange);
            this.inclinationCurvePreset = LBCurve.FilterCurvePreset.WideRange;
            this.map = null; // new Texture2D(1,1);
            this.mapColour = Color.white;
            this.mapTolerance = 0;
            this.mapInverse = false;
            this.mapToleranceBlendCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            this.smoothRate = 0f;
            this.lbStencilGUID = string.Empty;
            this.lbStencilLayerGUID = string.Empty;
        }

        /// <summary>
        /// Constructor to create a clone of a LBLayerFilter instance
        /// </summary>
        /// <param name="lbLayerFilter"></param>
        public LBLayerFilter(LBLayerFilter lbLayerFilter)
        {
            this.type = lbLayerFilter.type;

            // Height filter variables
            this.minHeight = lbLayerFilter.minHeight;
            this.maxHeight = lbLayerFilter.maxHeight;
            this.heightCurve = new AnimationCurve(lbLayerFilter.heightCurve.keys);
            this.heightCurvePreset = lbLayerFilter.heightCurvePreset;

            this.minInclination = lbLayerFilter.minInclination;
            this.maxInclination = lbLayerFilter.maxInclination;
            this.inclinationCurve = new AnimationCurve(lbLayerFilter.inclinationCurve.keys);
            this.inclinationCurvePreset = lbLayerFilter.inclinationCurvePreset;

            if (lbLayerFilter.map == null) { this.map = null; }
            else { this.map = Texture2D.Instantiate(lbLayerFilter.map); }
            this.mapColour = lbLayerFilter.mapColour;
            this.mapTolerance = lbLayerFilter.mapTolerance;
            this.mapInverse = lbLayerFilter.mapInverse;

            this.mapToleranceBlendCurve = new AnimationCurve(lbLayerFilter.mapToleranceBlendCurve.keys);
            this.smoothRate = lbLayerFilter.smoothRate;

            // Added 1.4.0 Beta 10h
            this.lbStencilGUID = lbLayerFilter.lbStencilGUID;
            this.lbStencilLayerGUID = lbLayerFilter.lbStencilLayerGUID;
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Create an LBLayerFilter for Stencil Layer using the LBStencil instance and the name of the layer
        /// </summary>
        /// <param name="lbStencil"></param>
        /// <param name="layerName"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static LBLayerFilter CreateLayerFilter(LBStencil lbStencil, string layerName, bool showErrors)
        {
            LBLayerFilter lbLayerFilter = null;

            if (lbStencil == null) { if (showErrors) { Debug.LogWarning("LBLayerFilter.CreateLayerFilter - stencil is null"); } }
            else if (string.IsNullOrEmpty(layerName)) { if (showErrors) { Debug.LogWarning("LBLayerFilter.CreateLayerFilter - layerName is an empty string"); } }
            else
            {
                lbLayerFilter = new LBLayerFilter();
                if (lbLayerFilter == null) { if (showErrors) { Debug.LogWarning("LBLayerFilter.CreateLayerFilter - could not create LBLayerFilter for " + lbStencil.stencilName + "." + layerName); } }
                else
                {
                    lbLayerFilter.type = LBLayerFilter.LayerFilterType.StencilLayer;
                    lbLayerFilter.lbStencilGUID = lbStencil.GUID;

                    // Find the new stencil layer
                    LBStencilLayer lbStencilLayer = lbStencil.GetStencilLayerByName(layerName);
                    if (lbStencilLayer != null)
                    {
                        lbLayerFilter.lbStencilLayerGUID = lbStencilLayer.GUID;
                    }
                    else { lbLayerFilter = null; }
                }
            }

            return lbLayerFilter;
        }

        #endregion
    }
}