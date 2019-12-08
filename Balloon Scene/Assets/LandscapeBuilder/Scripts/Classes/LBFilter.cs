// Landscape Builder. Copyright (c) 2016-2019 SCSM Pty Ltd. All rights reserved.
using UnityEngine;
using System.Collections.Generic;

namespace LandscapeBuilder
{
    [System.Serializable]
    public class LBFilter
    {
        // Filter class - added in version 1.2.1
        // NOTE: CANNOT be used in a LBTerrainTexture class as this
        // would create a circular link because LBFilter contains:
        //  public LBTerrainTexture terrainTexture
        // To apply a filter to a Texture use LBTextureFilter

        #region Enumerations

        // Added Biome LB 2.2.1. Currently only used with Vegetation Studio Pro
        public enum FilterType
        {
            Texture = 0,
            Area = 1,
            Biome = 3,
            Proximity = 10,
            StencilLayer = 15
        }

        // Not all filter types are available for everything
        // So define limited enums as required
        public enum FilterTypesForTrees
        {
            Texture = 0,
            Area = 1,
            StencilLayer = 15
        }

        public enum FilterTypesForGrass
        {
            Texture = 0,
            Area = 1,
            StencilLayer = 15
        }

        public enum FilterTypesForMeshes
        {
            Texture = 0,
            Area = 1,
            Proximity = 10,
            StencilLayer = 15
        }

        public enum FilterTypesForGroups
        {
            Biome = 3,
            StencilLayer = 15
        }

        public enum FilterMode
        {
            AND = 0,
            OR = 1,
            NOT = 2
        }

        /// <summary>
        /// Used as a subset of FilterMode
        /// </summary>
        public enum FilterModeAndNot
        {
            AND = 0,
            NOT = 2
        }

        #endregion

        #region Public Variables and Properties
        public FilterType filterType;
        public FilterMode filterMode;

        // Restrict area members
        // Added 1.3.0 Beta 6j
        public Rect areaRect;
        public bool showAreaHighlighter;

        // Added 1.3.1 Beta 9b
        // Proximity filter - Don't place near other object types
        // Use Unity Layers and Unity Tags
        // Store the Full Unity Layers mask as there is no way to verify
        // the restricted Unity Layers list is the same one that was exists when the filter
        // is applied at runtime.
        public LayerMask layerMask;
        public float minProximity;
        public string filterByTag;
        public static string FilterByAllTags = "{ All Tags }";

        /// <summary>
        /// This temporarily stores all the positions of objects within the landscape
        /// that match the proximity filter. The distance to these is compare with
        /// minProxity when placing Meshes/Prefabs in the scene.
        /// </summary>
        [System.NonSerialized] public List<Vector3> objectPositionProximityCacheList;
        [System.NonSerialized] public float minProximitySquared;

        // A Texture filter "may" contain multiple textures but although serializing
        // a List<LBTerrainTexture> within a List<LBFilter> was tested successfully,
        // it lead to layout issues in the editor and the need for more buttons. 
        public LBTerrainTexture terrainTexture;

        // Added 1.4.1 Beta 1b
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

        // Added 2.2.1 used by Vegetation Studio Pro to indentify
        // the biome (vegetation package) in index of ones available
        public LBBiome lbBiome;

        #endregion

        #region Constructors
        // Class constructors
        public LBFilter()
        {
            // defaults
            this.filterType = FilterType.Area;
            this.filterMode = FilterMode.AND;
            this.areaRect = new Rect(0f, 0f, 1000f, 1000f);
            showAreaHighlighter = false;
            minProximity = 10f;
            layerMask = 0;
            filterByTag = LBFilter.FilterByAllTags;
            this.lbStencilGUID = string.Empty;
            this.lbStencilLayerGUID = string.Empty;
            lbBiome = null;
        }

        public LBFilter(FilterType filterType, FilterMode filterMode)
        {
            this.filterType = filterType;
            this.filterMode = filterMode;

            // defaults
            this.areaRect = new Rect(0f, 0f, 1000f, 1000f);
            showAreaHighlighter = false;
            minProximity = 10f;
            layerMask = 0;
            filterByTag = LBFilter.FilterByAllTags;
            this.lbStencilGUID = string.Empty;
            this.lbStencilLayerGUID = string.Empty;
            lbBiome = null;
        }

        /// <summary>
        /// Constructor to create a clone of a LBFilter instance
        /// </summary>
        /// <param name="lbFilter"></param>
        public LBFilter(LBFilter lbFilter)
        {
            this.filterType = lbFilter.filterType;
            this.filterMode = lbFilter.filterMode;
            this.areaRect = lbFilter.areaRect;
            this.showAreaHighlighter = lbFilter.showAreaHighlighter;
            this.minProximity = lbFilter.minProximity;
            this.layerMask = lbFilter.layerMask;
            this.filterByTag = lbFilter.filterByTag;
            this.terrainTexture = new LBTerrainTexture(lbFilter.terrainTexture);
            this.lbStencilGUID = lbFilter.lbStencilGUID;
            this.lbStencilLayerGUID = lbFilter.lbStencilLayerGUID;
            this.lbBiome = lbFilter.lbBiome;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns the worldspace location of the filter AreaRect
        /// NOTE: The areaRect is offset from 0,0 in the current landscape
        /// </summary>
        /// <param name="landscapePosition"></param>
        /// <returns></returns>
        public Rect GetWorldSpaceAreaRect(Vector3 landscapePosition)
        {
            return new Rect(landscapePosition.x + areaRect.xMin, landscapePosition.z + areaRect.yMin, areaRect.width, areaRect.height);
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Determine if a List of LBFilter contains one with a given FilterType 
        /// </summary>
        /// <param name="lbFilterList"></param>
        /// <param name="filterType"></param>
        /// <returns></returns>
        public static bool Contains(List<LBFilter> lbFilterList, FilterType filterType)
        {
            bool containsFilterType = false;

            if (lbFilterList != null)
            {
                // Find the first occurence of the filterType in the list. If it is not found
                // FindIndex will return -1. 
                containsFilterType = (lbFilterList.FindIndex(lb => lb.filterType == filterType) >= 0);
            }

            return containsFilterType;
        }

        /// <summary>
        /// Copies or duplicates a list of LBFilters
        /// </summary>
        /// <param name="lbFilterList"></param>
        /// <returns></returns>
        public static List<LBFilter> CopyList(List<LBFilter> lbFilterList)
        {
            List<LBFilter> duplicateList = new List<LBFilter>();

            if (lbFilterList != null && duplicateList != null)
            {
                foreach (LBFilter lbFilter in lbFilterList)
                {
                    if (lbFilter != null)
                    {
                        duplicateList.Add(new LBFilter(lbFilter));
                    }
                }
            }

            return duplicateList;
        }

        /// <summary>
        /// Find the first LBTerrainTexture that has the same Texture2D image as the Texture filter.
        /// Update the filter. NOTE: If there are multiple uses of the same texture, this may sometimes
        /// get the wrong texture. The only way to correct this is to fully populate the LBFilter will
        /// all the LBTerrainTexture attributes (like Normalmap, smoothness, metallic etc).
        /// </summary>
        /// <param name="lbFilterList"></param>
        /// <param name="lbTerrainTextureList"></param>
        public static void UpdateTextures(List<LBFilter> lbFilterList, List<LBTerrainTexture> lbTerrainTextureList, bool showErrors)
        {
            if (lbTerrainTextureList == null)
            {
                if (showErrors) { Debug.LogWarning("ERROR LBFilter.UpdateTextures - List of available textures cannot be null"); }
            }
            else if (lbTerrainTextureList.Count < 1)
            {
                if (showErrors) { Debug.Log("INFO LBFilter.UpdateTextures - List of available textures is empty"); }
            }
            else if (lbFilterList != null)
            {
                for (int f = 0; f < lbFilterList.Count; f++)
                {
                    LBFilter lbFilter = lbFilterList[f];
                    if (lbFilter != null)
                    {
                        if (lbFilter.filterType == FilterType.Texture)
                        {
                            // Find the first matching texture using only the Texture2D image. NOTE: this may be incorrect for
                            // landscapes with the same Texture2D used multiple times in the Texturing tab.
                            int textureIndex = lbTerrainTextureList.FindIndex(ftx => ftx.texture == lbFilter.terrainTexture.texture);
                            if (textureIndex >= 0)
                            {
                                //Debug.Log("UpdateTextures Found: " + lbFilter.terrainTexture.texture.name);
                                lbFilter.terrainTexture = new LBTerrainTexture(lbTerrainTextureList[textureIndex]);
                            }
                            else
                            {
                                if (showErrors) { Debug.LogWarning("ERROR LBFilter.UpdateTextures - could not find a match for " + lbFilter.terrainTexture.texture.name); }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Create an LBFilter for Stencil Layer using the LBStencil instance and the name of the layer
        /// </summary>
        /// <param name="lbStencil"></param>
        /// <param name="layerName"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static LBFilter CreateFilter(LBStencil lbStencil, string layerName, bool showErrors)
        {
            LBFilter lbFilter = null;

            if (lbStencil == null) { if (showErrors) { Debug.LogWarning("LBFilter.CreateFilter - stencil is null"); } }
            else if (string.IsNullOrEmpty(layerName)) { if (showErrors) { Debug.LogWarning("LBFilter.CreateFilter - layerName is an empty string"); } }
            else
            {
                lbFilter = new LBFilter(LBFilter.FilterType.StencilLayer, FilterMode.AND);
                if (lbFilter == null) { if (showErrors) { Debug.LogWarning("LBFilter.CreateFilter - could not create LBFilter for " + lbStencil.stencilName + "." + layerName); } }
                else
                {
                    lbFilter.lbStencilGUID = lbStencil.GUID;

                    // Find the new stencil layer
                    LBStencilLayer lbStencilLayer = lbStencil.GetStencilLayerByName(layerName);
                    if (lbStencilLayer != null)
                    {
                        lbFilter.lbStencilLayerGUID = lbStencilLayer.GUID;
                    }
                    else { lbFilter = null; }
                }
            }

            return lbFilter;
        }

        /// <summary>
        /// Create an LBFilter for Stencil Layer using the LBStencil instance GUID and the GUID of the stencil layer
        /// </summary>
        /// <param name="lbStencilGUID"></param>
        /// <param name="lbStencilLayerGUID"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static LBFilter CreateFilter(string lbStencilGUID, string lbStencilLayerGUID, bool showErrors)
        {
            LBFilter lbFilter = null;

            if (string.IsNullOrEmpty(lbStencilGUID)) { if (showErrors) { Debug.LogWarning("LBFilter.CreateFilter - lbStencilGUID is an empty string"); } }
            else if (string.IsNullOrEmpty(lbStencilLayerGUID)) { if (showErrors) { Debug.LogWarning("LBFilter.CreateFilter - lbStencilLayerGUID is an empty string"); } }
            else
            {
                lbFilter = new LBFilter(FilterType.StencilLayer, FilterMode.AND);
                if (lbFilter == null) { if (showErrors) { Debug.LogWarning("LBFilter.CreateFilter - could not create LBFilter"); } }
                else
                {
                    lbFilter.lbStencilGUID = lbStencilGUID;
                    lbFilter.lbStencilLayerGUID = lbStencilLayerGUID;
                }
            }

            return lbFilter;
        }

        /// <summary>
        /// Check all the Stencil filters in a list, and see if the given normalised point (0-1.0f) has
        /// been filtered out.
        /// NOTE: lbFilter.lbStencilLayer.layerArray must be pre-populated before calling this method
        /// for each of the filters.
        /// </summary>
        /// <param name="lbFilterList"></param>
        /// <param name="stencilLayerPosN"></param>
        /// <returns></returns>
        public static bool IsPointFilteredByStencils(List<LBFilter> lbFilterList, Vector2 stencilLayerPosN)
        {
            bool isFiltered = false;

            if (lbFilterList != null)
            {
                int arrayPointX = 0, arrayPointZ = 0;
                int stencilLayerResolution = 0;
                int stencilLayerPixel = 0;
                int hashValue = 0;
                LB_Extension.XXHash xxHash = new LB_Extension.XXHash(37761);

                foreach (LBFilter lbFilter in lbFilterList)
                {
                    // The layerArray should have been pre-populated for StencilLayer filters
                    // Check for lbFilter.lbStencilLayer != null in case user deleted the Stencil Layer in the scene
                    if (lbFilter.filterType == LBFilter.FilterType.StencilLayer && lbFilter.lbStencilLayer != null && lbFilter.lbStencilLayer.layerArray != null)
                    {
                        // Get the value of the Stencil layer at this point in the terrain
                        stencilLayerResolution = (int)lbFilter.lbStencilLayer.layerResolution;
                        // Get the point within the stencil layer USHORT array
                        arrayPointX = Mathf.RoundToInt(stencilLayerPosN.x * (stencilLayerResolution - 1f));
                        arrayPointZ = Mathf.RoundToInt((1f - stencilLayerPosN.y) * (stencilLayerResolution - 1f));

                        // Ensure it it's outside the range of the USHORT array
                        arrayPointX = Mathf.Clamp(arrayPointX, 0, stencilLayerResolution - 1);
                        arrayPointZ = Mathf.Clamp(arrayPointZ, 0, stencilLayerResolution - 1);

                        /// TODO - might be able to improve group stencil layer blend using a curve
                        stencilLayerPixel = lbFilter.lbStencilLayer.layerArray[arrayPointX, arrayPointZ];

                        if (lbFilter.filterMode == LBFilter.FilterMode.AND)
                        {
                            // Randomise the chance of placement based on stencil layer pixel value
                            if (stencilLayerPixel < 1) { isFiltered = true; break; }
                            else
                            {
                                // The hash, this case, takes the point in the stencil as input data
                                hashValue = xxHash.Range(0, 65535, arrayPointX, arrayPointZ);

                                if (hashValue > stencilLayerPixel) { isFiltered = true; break; }
                            }
                        }
                        // Assume NOT
                        else
                        {
                            // Randomise the chance of placement based on stencil layer pixel value
                            if (stencilLayerPixel > 65534) { isFiltered = true; break; }
                            else
                            {
                                // The hash, this case, takes the point in the stencil as input data
                                hashValue = xxHash.Range(0, 65535, arrayPointX, arrayPointZ);

                                if (hashValue < stencilLayerPixel) { isFiltered = true; break; }
                            }
                        }
                    }
                }
            }

            return isFiltered;
        }

        #endregion
    }
}