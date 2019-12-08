using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace LandscapeBuilder
{
    [System.Serializable]
    public class LBTextureFilter
    {
        // Filter class - added in version 1.3.0
        // Overcomes a limitation with LBFilter which cannont be a
        // public member of LBTextureFilter because it contains
        // a public member of LBTextureFilter itelf.

        #region Enumerators
        public enum FilterType
        {
            Area = 0,
            StencilLayer = 15
        }

        public enum FilterMode
        {
            AND = 0,
            OR = 1,
            NOT = 2
        }
        #endregion

        #region Public variables and properties
        public FilterType filterType;
        public FilterMode filterMode;

        // Restrict area members
        public Rect areaRect;
        public bool showAreaHighlighter;

        // Added 1.4.1 Beta 1c
        public string lbStencilGUID;
        public string lbStencilLayerGUID;

        // Temp storage for display in LandscapeBuilderWindow Editor and LBLandscapeTerrain.PopulateTerrainWithMeshes(),
        // PopulateTerrainWithGrass(), PopulateTerrainWithTrees()
        [System.NonSerialized] public LBStencil lbStencil;

        // Temp storage for use in LBLandscapeTerrain.PopulateTerrainWithMeshes(),
        // PopulateTerrainWithGrass(), PopulateTerrainWithTrees()
        [System.NonSerialized] public LBStencilLayer lbStencilLayer;

        // Cached for Compute Shader used in LBLandscapeTerrain.TextureTerrain()
        [System.NonSerialized] public int stencilLayerResolution;

        #endregion

        #region Constructors
        // Class constructors
        public LBTextureFilter()
        {
            // Default values
            this.filterType = FilterType.Area;
            // LB 2.2.1 changed default from OR to AND. With a single filter,
            // by default typically want to filter to only area defined.
            this.filterMode = FilterMode.AND;
            this.areaRect = new Rect(0f, 0f, 1000f, 1000f);
            showAreaHighlighter = false;
            this.lbStencilGUID = string.Empty;
            this.lbStencilLayerGUID = string.Empty;
        }

        public LBTextureFilter(FilterType filterType, FilterMode filterMode)
        {
            this.filterType = filterType;
            this.filterMode = filterMode;

            // Defaults
            this.areaRect = new Rect(0f, 0f, 1000f, 1000f);
            showAreaHighlighter = false;
            this.lbStencilGUID = string.Empty;
            this.lbStencilLayerGUID = string.Empty;
        }
        #endregion

        #region Public Static Methods
        /// <summary>
        /// Determine if a List of LBTextureFilter contains one with a given FilterType 
        /// </summary>
        /// <param name="lbTextureFilterList"></param>
        /// <param name="filterType"></param>
        /// <returns></returns>
        public static bool Contains(List<LBTextureFilter> lbTextureFilterList, FilterType filterType)
        {
            bool containsFilterType = false;

            if (lbTextureFilterList != null)
            {
                // Find the first occurence of the filterType in the list. If it is not found
                // FindIndex will return -1. 
                containsFilterType = (lbTextureFilterList.FindIndex(lb => lb.filterType == filterType) >= 0);
            }

            return containsFilterType;
        }

        /// <summary>
        /// Create an LBTextureFilter for Stencil Layer using the LBStencil instance GUID and the GUID of the stencil layer
        /// </summary>
        /// <param name="lbStencilGUID"></param>
        /// <param name="lbStencilLayerGUID"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static LBTextureFilter CreateFilter(string lbStencilGUID, string lbStencilLayerGUID, bool showErrors)
        {
            LBTextureFilter lbFilter = null;

            if (string.IsNullOrEmpty(lbStencilGUID)) { if (showErrors) { Debug.LogWarning("LBTextureFilter.CreateFilter - lbStencilGUID is an empty string"); } }
            else if (string.IsNullOrEmpty(lbStencilLayerGUID)) { if (showErrors) { Debug.LogWarning("LBTextureFilter.CreateFilter - lbStencilLayerGUID is an empty string"); } }
            else
            {
                lbFilter = new LBTextureFilter(FilterType.StencilLayer, FilterMode.AND);
                if (lbFilter == null) { if (showErrors) { Debug.LogWarning("LBTextureFilter.CreateFilter - could not create LBTextureFilter"); } }
                else
                {
                    lbFilter.lbStencilGUID = lbStencilGUID;
                    lbFilter.lbStencilLayerGUID = lbStencilLayerGUID;
                }
            }

            return lbFilter;
        }

        #endregion
    }
}