// Landscape Builder. Copyright (c) 2016-2019 SCSM Pty Ltd. All rights reserved.
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace LandscapeBuilder
{
    /// <summary>
    /// Used for storing data imported from terrains typically not procedurally generated with Landscape Builder.
    /// A single instance of LBTerrainData can be used to store data for a single terrain for a LB type like
    /// LBLayer, LBTerrainTexture, LBTerrainGrass, LBTerrainTree. Only the data required is populated. For example,
    /// if we're importing heightmap data we'd populate rawHeightData. If we're importing texture data (alphamaps),
    /// we'd populate only the textureAlphaMapData.
    /// </summary>
    [System.Serializable]
    public class LBTerrainData
    {
        #region Enumerations

        #endregion

        #region Variables and Properties

        public string sourceTerrainName;
        public string sourceTerrainDataName;
        public string dataSourceName;           // e.g. Unity Terrain Data, a tiff file name, a RAW File name

        // HEIGHTMAP variables

        /// <summary>
        /// RAW data is stored as 16-bit, consisting of a little and big endian (8bit + 8bit).
        /// LB stores RAW data in little endian (Windows) format, which is more suited to Intel processors. 
        /// </summary>
        public byte[] rawHeightData;

        public int RawHeightResolution { get { if (rawHeightData == null) { return 0; } else { return (int)(Mathf.Sqrt(rawHeightData.Length / 2)); } } }

        // RAW heightmap data can have a range 0-65535
        public ushort rawMinHeight;
        public ushort rawMaxHeight;

        // The source data width and length (e.g. dimensions of tiff file)
        public int rawSourceWidth;
        public int rawSourceLength;

        // TEXTURE variables
        // Alphamaps are read and updated in Unity as a 3D array of splat values [x,y,t].
        // The third dimension corresponds to the number or position of the splatmap texture (SplatPrototype).
        // 
        public float[] textureAlphaMapData;
        public int TextureSplatResolution { get { if (textureAlphaMapData == null) { return 0; } else { return (int)Mathf.Sqrt(textureAlphaMapData.Length); } } }

        // TREE variables
        public List<LBTerrainTreeInstanceExt> lbTerrainTreeInstanceExtList;

        // GRASS variables
        // Grass is stored within a y,x map of integers within a terrain (int [,] detailDensityMap)
        // LB stores this imported data as single dimensional, serializable array.
        public int[] grassDetailDensityData;
        public int GrassDetailResolution { get { if (grassDetailDensityData == null) { return 0; } else { return (int)Mathf.Sqrt(grassDetailDensityData.Length); } } }

        #endregion

        #region Constructors

        public LBTerrainData()
        {
            this.sourceTerrainName = "unknown terrain";
            this.sourceTerrainDataName = "unknown terrain data name";
            this.rawHeightData = null;
            this.lbTerrainTreeInstanceExtList = new List<LBTerrainTreeInstanceExt>();
            this.grassDetailDensityData = null;
            this.textureAlphaMapData = null;
            this.dataSourceName = "unknown";
            this.rawMinHeight = 0;
            this.rawMaxHeight = 0;
            this.rawSourceWidth = 0;
            this.rawSourceLength = 0;
        }

        public LBTerrainData(string TerrainName)
        {
            this.sourceTerrainName = TerrainName;
            this.sourceTerrainDataName = "unknown terrain data name";
            this.rawHeightData = null;
            this.lbTerrainTreeInstanceExtList = new List<LBTerrainTreeInstanceExt>();
            this.grassDetailDensityData = null;
            this.textureAlphaMapData = null;
            this.dataSourceName = "unknown";
            this.rawMinHeight = 0;
            this.rawMaxHeight = 0;
            this.rawSourceWidth = 0;
            this.rawSourceLength = 0;
        }

        /// <summary>
        /// Clone constructor
        /// </summary>
        /// <param name="lbTerrainData"></param>
        public LBTerrainData(LBTerrainData lbTerrainData)
        {
            if (lbTerrainData == null)
            {
                // Default values
                this.sourceTerrainName = "unknown terrain";
                this.sourceTerrainDataName = "unknown terrain data name";
                this.rawHeightData = null;
                this.grassDetailDensityData = null;
                this.textureAlphaMapData = null;
                this.dataSourceName = "unknown";
                this.rawMinHeight = 0;
                this.rawMaxHeight = 0;
                this.rawSourceWidth = 0;
                this.rawSourceLength = 0;
            }
            else
            {
                this.sourceTerrainName = lbTerrainData.sourceTerrainName;
                this.sourceTerrainDataName = lbTerrainData.sourceTerrainDataName;

                if (string.IsNullOrEmpty(this.dataSourceName)) { this.dataSourceName = "unknown"; }
                else { this.dataSourceName = lbTerrainData.dataSourceName; }

                // Heightmap data
                if (lbTerrainData.rawHeightData == null)
                {
                    this.rawHeightData = null;
                    this.rawMinHeight = 0;
                    this.rawMaxHeight = 0;
                    this.rawSourceWidth = 0;
                    this.rawSourceLength = 0;
                }
                else
                {
                    // A shallow copy (clone) works here because it is a primative/immutable type
                    // This should create a true copy rather than a reference to the input array
                    this.rawHeightData = lbTerrainData.rawHeightData.Clone() as byte[];

                    this.rawMinHeight = lbTerrainData.rawMinHeight;
                    this.rawMaxHeight = lbTerrainData.rawMaxHeight;
                    this.rawSourceWidth = lbTerrainData.rawSourceWidth;
                    this.rawSourceLength = lbTerrainData.rawSourceLength;
                }

                // Texture / splatmap data
                if (lbTerrainData.textureAlphaMapData == null) { textureAlphaMapData = null; }
                else
                {
                    // A shallow copy (clone) works here because it is a primative/immutable type
                    // This should create a true copy rather than a reference to the input array
                    this.textureAlphaMapData = lbTerrainData.textureAlphaMapData.Clone() as float[];
                }

                // Tree instance data
                if (lbTerrainData.lbTerrainTreeInstanceExtList == null) { lbTerrainTreeInstanceExtList = new List<LBTerrainTreeInstanceExt>(); }
                else { lbTerrainTreeInstanceExtList = new List<LBTerrainTreeInstanceExt>(lbTerrainData.lbTerrainTreeInstanceExtList); }

                // Grass data
                if (lbTerrainData.grassDetailDensityData == null) { grassDetailDensityData = null; }
                else
                {
                    // A shallow copy (clone) works here because it is a primative/immutable type
                    // This should create a true copy rather than a reference to the input array
                    this.grassDetailDensityData = lbTerrainData.grassDetailDensityData.Clone() as int[];
                }
            }
        }

        #endregion

        #region Public Non-Static Methods

        /// <summary>
        /// Is there any RAW Height map data?
        /// </summary>
        /// <returns></returns>
        public bool HasRAWHeightData()
        {
            if (rawHeightData == null) { return false; }
            else if (rawHeightData.Length < 2) { return false; }
            else { return true; }
        }

        /// <summary>
        /// Validate the RAWHeightData. Compare it with the
        /// first terrain in the landscape.
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public bool ValidateRAWHeightData(LBLandscape landscape, bool showErrors)
        {
            bool isValid = false;

            if (landscape == null) { if (showErrors) Debug.LogWarning("ERROR: LBTerrainData.ValidateRAWHeightData - landscape is null"); }
            else if (string.IsNullOrEmpty(sourceTerrainName)) { if (showErrors) Debug.LogWarning("ERROR: LBTerrainData.ValidateRAWHeightData - sourceTerrainName is not available"); }
            else if (!HasRAWHeightData()) { if (showErrors) Debug.LogWarning("ERROR: LBTerrainData.ValidateRAWHeightData - no raw height data to validate"); }
            else if (landscape.landscapeTerrains == null) { if (showErrors) { Debug.LogWarning("ERROR: LBTerrainData.ValidateRAWHeightData - no terrains in landscape. Did you call landscape.landscape.SetLandscapeTerrains(true)?"); } }
            else if (landscape.landscapeTerrains.Length < 1) { if (showErrors) { Debug.LogWarning("ERROR: LBTerrainData.ValidateRAWHeightData - no terrains in landscape. You need at least 1 terrain under the Landscape gameobject"); } }
            else
            {
                // Get a reference to the first terrain in the landscape (they should all have the same terrain settings)
                Terrain terrain = landscape.landscapeTerrains[0];
                if (terrain == null) { if (showErrors) { Debug.LogWarning("ERROR: LBTerrainData.ValidateRAWHeightData - the first terrain in the landscape is null"); } }
                else if (terrain.terrainData == null) { if (showErrors) { Debug.LogWarning("ERROR: LBTerrainData.ValidateRAWHeightData - the first terrain terrainData in the landscape is null"); } }
                else
                {
                    //int rawResolution = RawHeightResolution;

                    if (terrain.terrainData.heightmapResolution != RawHeightResolution) { if (showErrors) { Debug.LogWarning("ERROR: LBTerrainData.ValidateRAWHeightData - Height Resolutions mismatch (Landscape: " + terrain.terrainData.heightmapResolution + " RawHeightData: " + RawHeightResolution + ")"); } }
                    else
                    {
                        isValid = true;
                    }
                }
            }

            return isValid;
        }

        /// <summary>
        /// Validate the Grass Detail data. Compare it with the first terrain in the landscape.
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public bool ValidateGrassDetailData(LBLandscape landscape, bool showErrors)
        {
            bool isValid = false;

            string methodName = "LBTerrainData.ValidateGrassDetailData";

            int grassDetailResolution = GrassDetailResolution;

            if (landscape == null) { if (showErrors) Debug.LogWarning("ERROR: " + methodName + " - landscape is null"); }
            else if (string.IsNullOrEmpty(sourceTerrainName)) { if (showErrors) Debug.LogWarning("ERROR: " + methodName + " - sourceTerrainName is not available"); }
            else if (grassDetailResolution < 1) { if (showErrors) Debug.LogWarning("ERROR: " + methodName + " - no grass detail data to validate"); }
            else if (landscape.landscapeTerrains == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - no terrains in landscape. Did you call landscape.landscape.SetLandscapeTerrains(true)?"); } }
            else if (landscape.landscapeTerrains.Length < 1) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - no terrains in landscape. You need at least 1 terrain under the Landscape gameobject"); } }
            else
            {
                // Get a reference to the first terrain in the landscape (they should all have the same terrain settings)
                Terrain terrain = landscape.landscapeTerrains[0];
                if (terrain == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - the first terrain in the landscape is null"); } }
                else if (terrain.terrainData == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - the first terrain terrainData in the landscape is null"); } }
                else if (terrain.terrainData.detailResolution != grassDetailResolution) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - Grass Detail Resolutions mismatch (Landscape: " + terrain.terrainData.detailResolution + " GrassDetailData: " + grassDetailResolution + ")"); } }
                else
                {
                    isValid = true;
                }
            }

            return isValid;
        }

        /// <summary>
        /// Validate the Texture Alphamap/Splatmap data. Compare it with the first terrain in the landscape.
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="allowResolutionMismatch"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public bool ValidateTextureAlphamapData(LBLandscape landscape, bool allowResolutionMismatch, bool showErrors)
        {
            bool isValid = false;

            string methodName = "LBTerrainData.ValidateTextureAlphamapData";

            int textureAlphaResolution = TextureSplatResolution;

            if (landscape == null) { if (showErrors) Debug.LogWarning("ERROR: " + methodName + " - landscape is null"); }
            else if (string.IsNullOrEmpty(sourceTerrainName)) { if (showErrors) Debug.LogWarning("ERROR: " + methodName + " - sourceTerrainName is not available"); }
            else if (textureAlphaResolution < 1) { if (showErrors) Debug.LogWarning("ERROR: " + methodName + " - no texture alphamap data to validate"); }
            else if (landscape.landscapeTerrains == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - no terrains in landscape. Did you call landscape.landscape.SetLandscapeTerrains(true)?"); } }
            else if (landscape.landscapeTerrains.Length < 1) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - no terrains in landscape. You need at least 1 terrain under the Landscape gameobject"); } }
            else
            {
                // Get a reference to the first terrain in the landscape (they should all have the same terrain settings)
                Terrain terrain = landscape.landscapeTerrains[0];
                if (terrain == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - the first terrain in the landscape is null"); } }
                else if (terrain.terrainData == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - the first terrain terrainData in the landscape is null"); } }
                else if (!allowResolutionMismatch && terrain.terrainData.alphamapResolution != textureAlphaResolution) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - Texture Alphamap Resolutions mismatch (Landscape: " + terrain.terrainData.alphamapResolution + " TextureAlphamapData: " + textureAlphaResolution + ")"); } }
                else
                {
                    isValid = true;
                }
            }

            return isValid;
        }


        /// <summary>
        /// Get the rawHeightData as an uint array. Pass in an empty array
        /// of the correct length (half the length of the byte array).
        /// </summary>
        /// <param name="rawDataInt1D"></param>
        public void ConvertRawHeightDataToInt(ref uint[] rawDataInt1D)
        {
            int rawData1DSize = (rawDataInt1D == null ? 0 : rawDataInt1D.Length);

            // LB stores RAW data in little endian (Windows) format. 2 bytes = 1 ushort 
            int rawHeightSize = (rawHeightData == null ? 0 : rawHeightData.Length / 2);

            if (rawData1DSize == rawHeightSize)
            {
                for (int hIdx = 0; hIdx < rawHeightSize; hIdx++)
                {
                    rawDataInt1D[hIdx] = (ushort)((rawHeightData[(hIdx * 2) + 1] << 8) | rawHeightData[hIdx * 2]);
                }
            }
        }

        /// <summary>
        /// Get the rawHeightData as an float array. Pass in an empty array
        /// of the correct length (half the length of the byte array).
        /// </summary>
        /// <param name="rawDataInt1D"></param>
        public void ConvertRawHeightDataToFloat(ref float[] rawDataInt1D)
        {
            int rawData1DSize = (rawDataInt1D == null ? 0 : rawDataInt1D.Length);

            // LB stores RAW data in little endian (Windows) format. 2 bytes = 1 ushort 
            int rawHeightSize = (rawHeightData == null ? 0 : rawHeightData.Length / 2);

            if (rawData1DSize == rawHeightSize)
            {
                // Increment the byte count within the for-loop to save hIdx * 2 multiplication
                for (int hIdx = 0, hIdx2 = 0; hIdx < rawHeightSize; hIdx++, hIdx2 += 2)
                {
                    rawDataInt1D[hIdx] = (rawHeightData[(hIdx2) + 1] << 8) | rawHeightData[hIdx2];
                    //rawDataInt1D[hIdx] = (rawHeightData[(hIdx * 2) + 1] << 8) | rawHeightData[hIdx * 2];
                    //rawDataInt1D[hIdx] = (float)(ushort)((rawHeightData[(hIdx * 2) + 1] << 8) | rawHeightData[hIdx * 2]);
                }
            }
        }

        #endregion

        #region Public Static Heigthmap Methods

        /// <summary>
        /// Does the list of LBTerrainData contain any RAW Height data?
        /// </summary>
        /// <param name="lbTerrainDataList"></param>
        /// <returns></returns>
        public static bool HasRAWHeightData(List<LBTerrainData> lbTerrainDataList)
        {
            bool isSuccessful = false;

            if (lbTerrainDataList != null)
            {
                if (lbTerrainDataList.Count > 0)
                {
                    for (int td = 0; td < lbTerrainDataList.Count; td++)
                    {
                        if (lbTerrainDataList[td] != null)
                        {
                            if (lbTerrainDataList[td].HasRAWHeightData()) { isSuccessful = true; break; }
                        }
                    }
                }
            }

            return isSuccessful;
        }

        /// <summary>
        /// Assuming all the RAWHeightData has the same resolution in the list of LBTerrainData,
        /// get the height resolution of the first one in the list.
        /// </summary>
        /// <param name="lbTerrainDataList"></param>
        /// <returns></returns>
        public static int GetRAWHeightResolution(List<LBTerrainData> lbTerrainDataList)
        {
            if (lbTerrainDataList == null) { return 0; }
            else if (lbTerrainDataList[0] == null) { return 0; }
            else { return lbTerrainDataList[0].RawHeightResolution; }
        }

        /// <summary>
        /// Get the total number of heightmap 'pixels' from all the LBTerrainData in the list supplied.
        /// Apart from 2 sqrts, should be pretty fast, although it does need to get the
        /// heightmap resolution from the first LBTerrainData.
        /// NOTE: Assumes each LBTerrainData stores data for one terrain and that they list
        /// was created in the same order as the terrains in landscape.landscapeTerrains.
        /// </summary>
        /// <param name="lbTerrainDataList"></param>
        /// <param name="isIncludeInnerEdges"></param>
        /// <returns></returns>
        public static int GetTotalHeightmapSize(List<LBTerrainData> lbTerrainDataList, bool isIncludeInnerEdges)
        {
            if (lbTerrainDataList == null) { return 0; }
            else if (lbTerrainDataList.Count < 1) { return 0; }
            else
            {
                int numTerrains = lbTerrainDataList.Count;
                int numTerrainsWide = Mathf.RoundToInt(Mathf.Sqrt(numTerrains));
                int heightmapResolution = GetRAWHeightResolution(lbTerrainDataList);

                if (numTerrainsWide == 1) { return heightmapResolution * heightmapResolution; }
                else if (isIncludeInnerEdges) { return heightmapResolution * heightmapResolution * numTerrains; }
                else
                {
                    return ((numTerrainsWide * (heightmapResolution - 1)) + 1) * ((numTerrainsWide * (heightmapResolution - 1)) + 1);
                }
            }
        }

        /// <summary>
        /// Get the name of the source data type. If it was an imported GeoTIFF file,
        /// return the TIFF filename
        /// </summary>
        /// <param name="lbTerrainDataList"></param>
        /// <returns></returns>
        public static string GetDataSourceDisplayName(List<LBTerrainData> lbTerrainDataList)
        {
            if (lbTerrainDataList == null) { return "None"; }
            else if (lbTerrainDataList.Count < 1) { return "None"; }
            else if (lbTerrainDataList[0] == null) { return "None"; }
            else
            {
                string displayName = lbTerrainDataList[0].dataSourceName;

                if (lbTerrainDataList.Count > 1 && !displayName.ToLower().Contains(".tif")) { displayName += "..."; }

                return displayName;
            }
        }

        /// <summary>
        /// Optimised for displaying the the min/max heights within the editor for the rawHeightData
        /// </summary>
        /// <param name="lbTerrainDataList"></param>
        /// <returns></returns>
        public static string GetRAWMinMaxHeightDisplay(List<LBTerrainData> lbTerrainDataList)
        {
            if (lbTerrainDataList == null) { return "N/A"; }
            else if (lbTerrainDataList.Count < 1) { return "N/A"; }
            else
            {
                ushort minHeight = ushort.MaxValue, maxHeight = ushort.MinValue;
                for (int i = 0; i < lbTerrainDataList.Count; i++)
                {
                    if (lbTerrainDataList[i].rawHeightData != null)
                    {
                        if (lbTerrainDataList[i].rawMinHeight < minHeight) { minHeight = lbTerrainDataList[i].rawMinHeight; }
                        if (lbTerrainDataList[i].rawMaxHeight > maxHeight) { maxHeight = lbTerrainDataList[i].rawMaxHeight; }
                    }
                }
                return (minHeight < ushort.MaxValue ? minHeight : (ushort)0).ToString("0") + ", " + (maxHeight > ushort.MinValue ? maxHeight : (ushort)0).ToString("0");
            }
        }

        /// <summary>
        /// Get the min/max RAW data heights for all the LBTerrainData in the list. Populate the minHeight and maxHeight parms
        /// and pass back to the caller. This is more efficient than having 2 methods that loop through the same list.
        /// </summary>
        /// <param name="lbTerrainDataList"></param>
        /// <param name="minHeight"></param>
        /// <param name="maxHeight"></param>
        /// <returns></returns>
        public static bool GetRAWMinMaxHeights(List<LBTerrainData> lbTerrainDataList, ref ushort minHeight, ref ushort maxHeight)
        {
            if (lbTerrainDataList == null) { return false; }
            else if (lbTerrainDataList.Count < 1) { return false; }
            else
            {
                minHeight = ushort.MaxValue;
                maxHeight = ushort.MinValue;
                for (int i = 0; i < lbTerrainDataList.Count; i++)
                {
                    if (lbTerrainDataList[i].rawHeightData != null)
                    {
                        if (lbTerrainDataList[i].rawMinHeight < minHeight) { minHeight = lbTerrainDataList[i].rawMinHeight; }
                        if (lbTerrainDataList[i].rawMaxHeight > maxHeight) { maxHeight = lbTerrainDataList[i].rawMaxHeight; }
                    }
                }
                if (minHeight == ushort.MaxValue) { minHeight = (ushort)0; }
                if (maxHeight == ushort.MinValue) { maxHeight = (ushort)0; }
                return true;
            }
        }

        /// <summary>
        /// Get the source heightmap dimensions of the first LBTerrainData instance in the list
        /// </summary>
        /// <param name="lbTerrainDataList"></param>
        /// <returns></returns>
        public static string GetDataSourceDimensionsDisplay(List<LBTerrainData> lbTerrainDataList)
        {
            if (lbTerrainDataList == null) { return "N/A"; }
            else if (lbTerrainDataList.Count < 1) { return "N/A"; }
            else if (lbTerrainDataList[0] == null) { return "N/A"; }
            else
            {
                return lbTerrainDataList[0].rawSourceWidth + ", " + lbTerrainDataList[0].rawSourceLength;
            }
        }

        /// <summary>
        /// Get all LBTerrainData heightmaps, and populate a single 1-dimensional array supplied by the calling method. The array must be created
        /// before calling this method. This method is much more efficient than returning a float array e.g. float[] flt = MyMethod(..) as it
        /// doesn't require a lot of garbage collection (GC). The float[] parmeter in this method is passed as a reference which
        /// does not require GC.
        /// NOTE: Assumes each LBTerrainData stores data for one terrain and that they list
        /// was created in the same order as the terrains in landscape.landscapeTerrains.
        /// USAGE:
        ///   int landscapeHeightsSize = landscape.GetTotalHeightmapSize(false);
        ///   System.GC.Collect();
        ///   float[] landscapeScopedHeightmap = new float[landscapeHeightsSize];
        ///   if (GetLandscapeScopedHeightmap(landscape, lbTerrainDataList, landscapeScopedHeightmap, false, showErrors)) { .... }
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="lbTerrainDataList"></param>
        /// <param name="landscapeScopedHeightmap"></param>
        /// <param name="isIncludeInnerEdges"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static bool GetLandscapeScopedHeightmap(LBLandscape landscape, List<LBTerrainData> lbTerrainDataList, float[] landscapeScopedHeightmap, bool isIncludeInnerEdges, bool showErrors)
        {
            bool isSuccessful = false;
            string methodName = "LBTerrainData.GetLandscapeScopedHeightmap";

            // Perform some basic validation
            if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR " + methodName + " - landscape cannot be null"); } }
            else if (lbTerrainDataList == null) { if (showErrors) { Debug.LogWarning("ERROR " + methodName + " - lbTerrainDataList is null. Please Report."); } }
            else if (lbTerrainDataList.Count < 1) { if (showErrors) { Debug.LogWarning("ERROR " + methodName + " - no LBTerrainData in lbTerrainDataList. Please Report."); } }
            else if (landscapeScopedHeightmap == null) { if (showErrors) { Debug.LogWarning("ERROR " + methodName + " - the landscape-scoped height map array is null. Please Report."); } }
            else
            {
                int numTerrains = lbTerrainDataList.Count;
                int rawHeightmapResolution = LBTerrainData.GetRAWHeightResolution(lbTerrainDataList);
                int landscapeHeightsSize = LBTerrainData.GetTotalHeightmapSize(lbTerrainDataList, isIncludeInnerEdges);

                // By default LB creates terrains left to right, bottom to top (0,0 is bottom left corner)
                int terrainRow = 0;
                int terrainCol = 0;
                int numTerrainsWide = (int)Mathf.Sqrt(numTerrains);
                int rowPixelWidth = isIncludeInnerEdges ? (numTerrainsWide * rawHeightmapResolution) : (numTerrainsWide * (rawHeightmapResolution - 1)) + 1;

                // WARNING: If not including inner edges:
                //     colPixelWidth is terrainHeightmapResolution - 1 on all but right-edge terrains (i.e. terrainCol = numTerrainsWide-1)
                //     colPixelLength is terrainHeightmapResolution - 1 on all but top-edge terrains (i.e. terrainRow = numTerrainsWide-1)
                int colPixelWidth = 0;  // The variable number of pixels to be used for the current column (terrain) width
                int colPixelLength = 0; // The variable number of pixels to be used for the current column (terrain) length

                // TopEdge and LeftEdge terrains also includes a terrain in a landscape with only one terrain.
                int colPixelWidthRightEdge = rawHeightmapResolution;
                int colPixelLengthTopEdge = rawHeightmapResolution;

                // Inner terrains exclude a terrain in a landscape with only one terrain.
                int colPixelWidthInner = isIncludeInnerEdges ? rawHeightmapResolution : rawHeightmapResolution - 1;
                int colPixelLengthInner = isIncludeInnerEdges ? rawHeightmapResolution : rawHeightmapResolution - 1;

                // Validate the size of the landscapeScopedHeightmap
                if (landscapeScopedHeightmap.Length != landscapeHeightsSize) { if (showErrors) { Debug.LogWarning("ERROR " + methodName + " - the landscape-scoped height map array is the incorrect size. Array size: " + landscapeScopedHeightmap.Length + " - should be: " + landscapeHeightsSize + ".  Please Report."); } }
                else
                {
                    // The start of the line in the landscapeScopedHeightmap
                    int zOffset = 0;

                    // Set default values when including inner edges
                    if (isIncludeInnerEdges || numTerrains == 1)
                    {
                        colPixelWidth = colPixelWidthRightEdge;
                        colPixelLength = colPixelLengthTopEdge;
                    }

                    // Copy the data from each terrain into the array
                    for (int terrainIdx = 0; terrainIdx < numTerrains; terrainIdx++)
                    {
                        // Copy data from this rawHeightData into the correct position in array
                        LBTerrainData lbTerrainData = lbTerrainDataList[terrainIdx];
                        byte[] rawHeightData = lbTerrainData.rawHeightData;

                        terrainRow = terrainIdx % numTerrainsWide;
                        terrainCol = terrainIdx / numTerrainsWide;

                        if (!isIncludeInnerEdges && numTerrains > 1)
                        {
                            if (terrainCol == numTerrainsWide - 1) { colPixelWidth = colPixelWidthRightEdge; }
                            else { colPixelWidth = colPixelWidthInner; }

                            if (terrainRow == numTerrainsWide - 1) { colPixelLength = colPixelLengthTopEdge; }
                            else { colPixelLength = colPixelLengthInner; }
                        }

                        int previousRowsOffset = 0;

                        // Determine the offset for each previous row (if there are any)
                        for (int row = 0; row < terrainRow; row++)
                        {
                            if (row == numTerrainsWide - 1) { previousRowsOffset += (colPixelLengthTopEdge * rowPixelWidth); }
                            else { previousRowsOffset += (colPixelLengthInner * rowPixelWidth); }
                        }

                        for (int z = 0; z < colPixelLength; z++)
                        {
                            // previous full terrain rows (if any)
                            // + previous full rows of pixels in the current row of terrains (if z > 0)
                            // + pixels in current row of pixels in terrains (columns) to the left of the current terrain (these will always be INNER terrains)  
                            zOffset = previousRowsOffset + (z * rowPixelWidth) + (terrainCol * colPixelWidthInner);

                            //Debug.Log(" INFO z:" + z + " previousRowsOffset:" + previousRowsOffset + " + (terrainCol * colPixelWidth) + " + (terrainCol * colPixelWidth) + " + (z * rowPixelWidth) + " + (z * rowPixelWidth) + " = " + zOffset + " landscapeScopedHeightmap[" +(zOffset) + ".." + (zOffset+colPixelWidth - 1) + "] = heightMap[" + z + ", 0.." + (colPixelWidth-1) + "]");

                            // NOTE: Should use System.Buffer.BlockCopy
                            for (int px = 0; px < colPixelWidth; px++) { landscapeScopedHeightmap[zOffset + px] = rawHeightData[(z * rawHeightmapResolution) + px]; }
                        }
                    }
                    isSuccessful = true;
                }
            }

            return isSuccessful;
        }

        /// <summary>
        /// Export a single terrain to a RAW file on disk
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="terrain"></param>
        /// <param name="folderPath"></param>
        /// <param name="fileName"></param>
        /// <param name="isMac"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static bool ExportHeightmapRaw(LBLandscape landscape, Terrain terrain, string folderPath, string fileName, bool isMac, bool showErrors)
        {
            bool isSuccessful = false;

            string methodName = "LBTerrainData.ExportHeightmapRaw";

            if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscape is null"); } }
            else if (terrain == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - terrain is null"); } }
            else if (string.IsNullOrEmpty(folderPath)) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - folderPath is an empty string"); } }
            else if (string.IsNullOrEmpty(fileName)) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - fileName is an empty string"); } }
            else if (!System.IO.Directory.Exists(folderPath)) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - " + folderPath + " does not exist"); } }
            else
            {
                int byteIndex = 0;
                string filePath = folderPath + "/" + fileName;

                // Append the extension if it doesn't exist
                if (string.IsNullOrEmpty(System.IO.Path.GetExtension(filePath))) { filePath += ".raw"; }

                TerrainData tData = terrain.terrainData;

                int heightmapResolution = tData.heightmapResolution;

                float[,] heightMap = tData.GetHeights(0, 0, heightmapResolution, heightmapResolution);

                // RAW data is stored as 16-bit, consisting of a little and big endian (8bit + 8bit)
                byte[] rawData = new byte[heightmapResolution * heightmapResolution * 2];

                if (rawData == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not create rawData array"); } }
                else
                {
                    //Debug.Log("INFO: LBTerrainData.ExportHeightmapRaw - exporting..." + terrain.name);

                    ushort heightUShort = 0;

                    for (int x = 0; x < heightmapResolution; x++)
                    {
                        for (int y = 0; y < heightmapResolution; y++)
                        {
                            byteIndex = y * (heightmapResolution * 2) + (x * 2);

                            // Heights are stored as [height,width] and are 0.0 - 1.0f.
                            heightUShort = (ushort)Mathf.RoundToInt(heightMap[y, x] * 65535f);

                            if (isMac)
                            {
                                // Mac: big endian
                                rawData[byteIndex] = System.Convert.ToByte(heightUShort >> 8);
                                rawData[byteIndex + 1] = System.Convert.ToByte(heightUShort & 255);
                            }
                            else
                            {
                                // Windows: little endian
                                rawData[byteIndex + 1] = System.Convert.ToByte(heightUShort >> 8);
                                rawData[byteIndex] = System.Convert.ToByte(heightUShort & 255);
                            }
                        }
                    }
                    try
                    {
                        // This will overwrite any existing file
                        System.IO.File.WriteAllBytes(filePath, rawData);
                        isSuccessful = true;
                    }
                    catch (System.IO.IOException ex)
                    {
                        if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not write file " + ex.Message); }
                    }
                    rawData = null;
                }
            }
            return isSuccessful;
        }

        /// <summary>
        /// Load a RAW file from disk into rawHeightData for the given terrain
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="terrain"></param>
        /// <param name="filePath"></param>
        /// <param name="isMac"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static LBTerrainData ImportHeightmapRAW(LBLandscape landscape, Terrain terrain, string filePath, bool isMac, bool showErrors)
        {
            LBTerrainData lbTerrainData = null;

            string methodName = "LBTerrainData.ImportHeightmapRAW";

            if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscape is null"); } }
            else if (terrain == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - terrain is null in " + landscape.name); } }
            else if (terrain.terrainData == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - terrain.terrainData is null for " + landscape.name + "." + terrain.name); } }
            else if (string.IsNullOrEmpty(filePath)) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - RAW file is not available for " + landscape.name + "." + terrain.name); } }
            else if (!System.IO.File.Exists(filePath)) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - RAW file does not exist: " + filePath); } }
            else
            {
                byte[] rawData = null;

                TerrainData tData = terrain.terrainData;

                int heightmapResolution = tData.heightmapResolution;

                // RAW data is stored as 16-bit, consisting of a little and big endian (8bit + 8bit)
                try
                {
                    rawData = System.IO.File.ReadAllBytes(filePath);
                }
                catch (System.Exception ex)
                {
                    if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not read RAW file " + ex.Message); }
                }

                if (rawData == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not get heights from terrain data for " + landscape.name + "." + terrain.name + " from file: " + filePath); } }
                else if (rawData.Length < 3) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - RAW data file for " + landscape.name + "." + terrain.name + " is empty or invalid: " + filePath); } }
                else if (Mathf.Sqrt(rawData.Length / 2) != heightmapResolution) { if (showErrors) { Debug.LogWarning("WARNING: " + methodName + " - The RAW data file resolution (" + (Mathf.Sqrt(rawData.Length / 2)).ToString() + ") does not match the terrain heightmap resolution (" + heightmapResolution + ") " + landscape.name + "." + terrain.name + " file: " + filePath); } }
                else
                {
                    lbTerrainData = new LBTerrainData();
                    if (lbTerrainData == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not create LBTerrainData instance for " + landscape.name + "." + terrain.name); } }
                    else
                    {
                        // terrainData name and the Terrain object name may be different if created outside LB
                        // We want the terrainData name, as that is what will be used in LBLandscapeTerrain.HeightmapFromLayers(..).
                        lbTerrainData.sourceTerrainName = terrain.name;
                        lbTerrainData.sourceTerrainDataName = tData.name;
                        lbTerrainData.rawHeightData = null;
                        lbTerrainData.dataSourceName = System.IO.Path.GetFileName(filePath);
                        lbTerrainData.rawSourceWidth = heightmapResolution;
                        lbTerrainData.rawSourceLength = heightmapResolution;

                        // RAW data is stored as 16-bit, consisting of a little and big endian (8bit + 8bit)
                        lbTerrainData.rawHeightData = new byte[heightmapResolution * heightmapResolution * 2];

                        if (lbTerrainData.rawHeightData == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not create rawHeightData array for " + landscape.name + "." + terrain.name); } }
                        else
                        {
                            int rawHeightDataLength = lbTerrainData.rawHeightData.Length;

                            ushort sampleMinValue = ushort.MaxValue;
                            ushort sampleMaxValue = ushort.MinValue;
                            ushort sample16bit = 0;

                            float scaledHeight = landscape.GetLandscapeTerrainHeight() / 65535f;

                            for (int byteIndex = 0; byteIndex < rawHeightDataLength - 1; byteIndex += 2)
                            {
                                // LB stores RAW data in little endian (Windows) format, which is more suited to Intel processors.
                                if (isMac)
                                {
                                    // Mac: big endian
                                    lbTerrainData.rawHeightData[byteIndex + 1] = rawData[byteIndex];
                                    lbTerrainData.rawHeightData[byteIndex] = rawData[byteIndex + 1];
                                    //sample16bit = (ushort)(rawData[byteIndex] << 8 | rawData[byteIndex + 1]);
                                }
                                else
                                {
                                    // Windows: little endian
                                    lbTerrainData.rawHeightData[byteIndex + 1] = rawData[byteIndex + 1];
                                    lbTerrainData.rawHeightData[byteIndex] = rawData[byteIndex];
                                    //sample16bit = (ushort)(rawData[byteIndex + 1] << 8 | rawData[byteIndex]);
                                }

                                sample16bit = (ushort)(rawData[byteIndex + 1] << 8 | rawData[byteIndex]);

                                // RAW data can range from 0 to 65535. There is no way to determine the actual heights in the source system.
                                // So apply the current heights of the landscape
                                sample16bit = (ushort)Mathf.RoundToInt(sample16bit * scaledHeight);

                                // keep track of the min/max values in the input RAW data
                                if (sample16bit > sampleMaxValue) { sampleMaxValue = sample16bit; }
                                if (sample16bit < sampleMinValue) { sampleMinValue = sample16bit; }
                            }

                            if (sampleMinValue == ushort.MaxValue) { sampleMinValue = ushort.MinValue; }

                            lbTerrainData.rawMinHeight = sampleMinValue;
                            lbTerrainData.rawMaxHeight = sampleMaxValue;
                        }
                    }
                }
            }

            return lbTerrainData;
        }

        /// <summary>
        /// Import Unity Terrain height data into a new LBTerrainData class instance
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="terrain"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static LBTerrainData ImportHeightmap(LBLandscape landscape, Terrain terrain, bool showErrors)
        {
            LBTerrainData lbTerrainData = null;

            string methodName = "LBTerrainData.ImportHeightmap";

            if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscape is null"); } }
            else if (terrain == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - terrain is null in " + landscape.name); } }
            else if (terrain.terrainData == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - terrain.terrainData is null for " + landscape.name + "." + terrain.name); } }
            else
            {
                TerrainData tData = terrain.terrainData;

                int heightmapResolution = tData.heightmapResolution;

                // Load the terrain heightmap data from the Unity Terrain as normalised values (0.0-1.0f)
                float[,] heightMap = tData.GetHeights(0, 0, heightmapResolution, heightmapResolution);

                if (heightMap == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not get heights from terrain data for " + landscape.name + "." + terrain.name); } }
                else
                {
                    lbTerrainData = new LBTerrainData();
                    if (lbTerrainData == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not create LBTerrainData instance for " + landscape.name + "." + terrain.name); } }
                    else
                    {
                        // terrainData name and the Terrain object name may be different if created outside LB
                        // We want the terrainData name, as that is what will be used in LBLandscapeTerrain.HeightmapFromLayers(..).
                        lbTerrainData.sourceTerrainName = terrain.name;
                        lbTerrainData.sourceTerrainDataName = tData.name;
                        lbTerrainData.rawHeightData = null;
                        lbTerrainData.dataSourceName = "Unity Terrain";
                        lbTerrainData.rawSourceWidth = heightmapResolution;
                        lbTerrainData.rawSourceLength = heightmapResolution;

                        // RAW data is stored as 16-bit, consisting of a little and big endian (8bit + 8bit)
                        lbTerrainData.rawHeightData = new byte[heightmapResolution * heightmapResolution * 2];

                        if (lbTerrainData.rawHeightData == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not create rawHeightData array for " + landscape.name + "." + terrain.name); } }
                        else
                        {
                            ushort heightUShort = 0;
                            int byteIndex = 0;

                            ushort sampleMinValue = ushort.MaxValue;
                            ushort sampleMaxValue = ushort.MinValue;

                            for (int x = 0; x < heightmapResolution; x++)
                            {
                                for (int y = 0; y < heightmapResolution; y++)
                                {
                                    byteIndex = y * (heightmapResolution * 2) + (x * 2);

                                    // Heights are stored as [height,width] and are 0.0 - 1.0f.
                                    heightUShort = (ushort)Mathf.RoundToInt(heightMap[y, x] * 65535f);

                                    // LB stores RAW data in little endian (Windows) format, which is more suited to Intel processors.
                                    lbTerrainData.rawHeightData[byteIndex + 1] = System.Convert.ToByte(heightUShort >> 8);
                                    lbTerrainData.rawHeightData[byteIndex] = System.Convert.ToByte(heightUShort & 255);

                                    // keep track of the min/max values in the input RAW data
                                    if (heightUShort > sampleMaxValue) { sampleMaxValue = heightUShort; }
                                    if (heightUShort < sampleMinValue) { sampleMinValue = heightUShort; }
                                }
                            }

                            if (sampleMinValue == ushort.MaxValue) { sampleMinValue = ushort.MinValue; }

                            // Convert to terrain heights
                            lbTerrainData.rawMinHeight = (ushort)Mathf.RoundToInt((sampleMinValue / 65535f) * tData.size.y);
                            lbTerrainData.rawMaxHeight = (ushort)Mathf.RoundToInt((sampleMaxValue / 65535f) * tData.size.y);
                        }
                    }
                }
            }

            return lbTerrainData;
        }

        #endregion

        #region Public Static Texturing Methods

        /// <summary>
        /// Import a Unity Terrain texture alphamaps (splatmaps) in LBTerrainData for each texture type.
        /// If any one texture succeeds, the method will return true.
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="terrain"></param>
        /// <param name="terrainIndex"></param>
        /// <param name="importedTerrainTexureList"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static bool ImportTextures(LBLandscape landscape, Terrain terrain, int terrainIndex, List<LBTerrainTexture> importedTerrainTexureList, bool showErrors)
        {
            bool isSuccessful = false;

            string methodName = "LBTerrainData.ImportTextures";

            if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscape is null"); } }
            else if (importedTerrainTexureList == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - list of imported terrain texture types is null."); } }
            else if (importedTerrainTexureList.Count < 1) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - no imported terrain texture types provided."); } }
            else if (terrain == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - terrain is null in " + landscape.name); } }
            else if (terrain.terrainData == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - terrain.terrainData is null for " + landscape.name + "." + terrain.name); } }
            {
                TerrainData tData = terrain.terrainData;

                int alphamapWidth = tData.alphamapWidth;
                int alphamapHeight = tData.alphamapHeight;

                // Get the texture types for this terrain
                #if UNITY_2018_3_OR_NEWER
                List<LBTerrainTexture> terrainTextureList = LBTerrainTexture.ToLBTerrainTextureList(tData.terrainLayers);
                #else
                List<LBTerrainTexture> terrainTextureList = LBTerrainTexture.ToLBTerrainTextureList(tData.splatPrototypes);
                #endif

                int numTerrainTextures = terrainTextureList == null ? 0 : terrainTextureList.Count;

                if (numTerrainTextures > 0)
                {
                    // Unity only provides a way of loading all the splatmaps - we can't just load one at a time
                    float[,,] alphaMaps = tData.GetAlphamaps(0, 0, alphamapWidth, alphamapHeight);

                    if (alphaMaps == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not allocate alphaMaps when getting terrain data for " + landscape.name + "." + terrain.name); } }
                    else
                    {
                        // Loop through all the texture types (SplatProtoTypes) in this terrain
                        for (int txTIdx = 0; txTIdx < numTerrainTextures; txTIdx++)
                        {
                            LBTerrainTexture lbTerrainTexture = terrainTextureList[txTIdx];

                            // Match this with one of the imported terrain texture types (if we can)
                            LBTerrainTexture lbTerrainTextureImported = importedTerrainTexureList.Find(tx => tx.texture == lbTerrainTexture.texture &&
                                                                                                             tx.normalMap == lbTerrainTexture.normalMap &&
                                                                                                             tx.smoothness == lbTerrainTexture.smoothness &&
                                                                                                             tx.metallic == lbTerrainTexture.metallic &&
                                                                                                             tx.tileSize == lbTerrainTexture.tileSize);

                            // If we found a match, get the terrain alphamap for this texture type within this terrain
                            if (lbTerrainTextureImported != null)
                            {
                                if (lbTerrainTextureImported.texture == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - grass texture is null in " + landscape.name + "." + terrain.name + ". Please Report"); } }
                                else if (lbTerrainTextureImported.lbTerrainDataList == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - lbTerrainDataList is not defined for " + lbTerrainTextureImported.texture.name + " in " + landscape.name + "." + terrain.name); } }
                                else if (lbTerrainTextureImported.lbTerrainDataList.Count <= terrainIndex) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - lbTerrainData is not defined for " + lbTerrainTextureImported.texture.name + " in " + landscape.name + "." + terrain.name); } }
                                else if (lbTerrainTextureImported.lbTerrainDataList[terrainIndex] == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - lbTerrainDataList[" + terrainIndex + "] is not defined for " + lbTerrainTextureImported.texture.name + " in " + landscape.name + "." + terrain.name); } }
                                else
                                {
                                    // terrainData name and the Terrain object name may be different if created outside LB
                                    // We want the terrainData name, as that is what will be used in LBLandscapeTerrain.PopulateTerrainWithTextures(..).
                                    lbTerrainTextureImported.lbTerrainDataList[terrainIndex].sourceTerrainName = terrain.name;
                                    lbTerrainTextureImported.lbTerrainDataList[terrainIndex].sourceTerrainDataName = tData.name;
                                    lbTerrainTextureImported.lbTerrainDataList[terrainIndex].textureAlphaMapData = null;

                                    // Texture alphamap data is stored within a x,y map of floats within a terrain (float [,,] alphamap)
                                    // LB is storing each (splat)alphamap as a single dimensional, serializable array.
                                    lbTerrainTextureImported.lbTerrainDataList[terrainIndex].textureAlphaMapData = new float[alphamapWidth * alphamapHeight];

                                    if (lbTerrainTextureImported.lbTerrainDataList[terrainIndex].textureAlphaMapData == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not create textureAlphaMapData array for " + landscape.name + "." + terrain.name); } }
                                    else
                                    {
                                        //Debug.Log("INFO: " + methodName + " - lbTerrainDataList[" + terrainIndex + "] found for " + lbTerrainTextureImported.texture.name + " in " + landscape.name + "." + terrain.name);

                                        for (int x = 0; x < alphamapWidth; x++)
                                        {
                                            for (int y = 0; y < alphamapHeight; y++)
                                            {
                                                lbTerrainTextureImported.lbTerrainDataList[terrainIndex].textureAlphaMapData[y * alphamapWidth + x] = alphaMaps[x, y, txTIdx];
                                            }
                                        }
                                        isSuccessful = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return isSuccessful;
        }

        /// <summary>
        /// Copy the 1D imported texture data into an array of Texture2Ds for use in a compute shader.
        /// Pass the isTerrainAlphaDataValid array in as a reference so as to not (hopefully) impact GC.
        /// </summary>
        /// <param name="texture2DArray"></param>
        /// <param name="lbTerrainDataList"></param>
        /// <param name="isTerrainAlphaDataValid"></param>
        /// <returns></returns>
        public static bool PopulateTexture2DArray(ref Texture2DArray texture2DArray, List<LBTerrainData> lbTerrainDataList, ref bool[] isTerrainAlphaDataValid)
        {
            bool isSuccessful = false;

            // The Texture2DArray only contains enough textures to hold all the imported texturing data.
            int num2DTextures = texture2DArray == null ? 0 : texture2DArray.depth;

            // One LBTerrainData instance is created for each enabled Texture from the LB Texturing tab.
            // This may include non-Imported textures.
            int numLBTerrainData = lbTerrainDataList == null ? 0 : lbTerrainDataList.Count;

            // Currently use 1 texture for each terrain texture data
            if (num2DTextures > 0 && num2DTextures <= numLBTerrainData)
            {
                int texWidth = texture2DArray.width;
                int texHeight = texture2DArray.height;
                int texArrayIdx = 0;
                for (int txIdx = 0; txIdx < numLBTerrainData; txIdx++)
                {
                    // Skip over Texture data that doesn't contain any valid alpha/splatmap data.
                    // This would include non-imported textures
                    if (!isTerrainAlphaDataValid[txIdx]) { continue; }

                    Color[] colours = texture2DArray.GetPixels(texArrayIdx);

                    // Populate colour data
                    for (int y = 0; y < texHeight; y++)
                    {
                        for (int x = 0; x < texWidth; x++)
                        {
                            colours[y * texWidth + x].r = lbTerrainDataList[txIdx].textureAlphaMapData[y * texWidth + x];
                        }
                    }

                    texture2DArray.SetPixels(colours, texArrayIdx);
                    texture2DArray.Apply(false, false);
                    texArrayIdx++;
                }

                isSuccessful = true;

                //Debug.Log("[DEBUG] numLBTerrainData: " + numLBTerrainData + " num2DTex: " + num2DTextures);
            }

            return isSuccessful;
        }

        #endregion

        #region Public Static Trees Methods

        /// <summary>
        /// Import Unity Terrain tree instance data as a List of LBTerrainData for each tree type.
        /// If any one tree type succeeds, the method will return true.
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="terrain"></param>
        /// <param name="terrainIndex"></param>
        /// <param name="importedTerrainTreeList"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static bool ImportTrees(LBLandscape landscape, Terrain terrain, int terrainIndex, List<LBTerrainTree> importedTerrainTreeList, bool showErrors)
        {
            bool isSuccessful = false;

            string methodName = "LBTerrainData.ImportTrees";

            if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscape is null"); } }
            else if (importedTerrainTreeList == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - list of imported terrain tree types is null."); } }
            else if (importedTerrainTreeList.Count < 1) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - no imported terrain tree types provided."); } }
            else if (terrain == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - terrain is null in " + landscape.name); } }
            else if (terrain.terrainData == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - terrain.terrainData is null for " + landscape.name + "." + terrain.name); } }
            else
            {
                TerrainData tData = terrain.terrainData;
                //int prototypeIndex = -1;

                // Get a list of the tree prototypes from the terrain
                List<LBTerrainTree> terrainTreeList = LBTerrainTree.ToLBTerrainTreeList(tData.treePrototypes);
                if (terrainTreeList != null && tData.treeInstances != null)
                {
                    List<LBTerrainTreeInstanceExt> lbTerrainTreeInstanceExtList = null;

                    if (terrainTreeList.Count > 0)
                    {
                        // Loop through all the tree prototypes in this terrain
                        for (int ttIdx = 0; ttIdx < terrainTreeList.Count; ttIdx++)
                        {
                            LBTerrainTree lbTerrainTree = terrainTreeList[ttIdx];

                            // Match this with one of the imported terrain tree types (if we can)
                            LBTerrainTree lbTerrainTreeImported = importedTerrainTreeList.Find(tt => tt.prefab == lbTerrainTree.prefab && tt.bendFactor == lbTerrainTree.bendFactor);

                            // If we found a match, get the terrain tree instances for this tree type within this terrain
                            if (lbTerrainTreeImported != null)
                            {
                                if (lbTerrainTreeImported.prefab == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - tree prefab is null in " + landscape.name + "." + terrain.name + ". Please Report"); } }
                                else if (lbTerrainTreeImported.lbTerrainDataList == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - lbTerrainDataList is not defined for " + lbTerrainTreeImported.prefab.name + " in " + landscape.name + "." + terrain.name); } }
                                else if (lbTerrainTreeImported.lbTerrainDataList.Count <= terrainIndex) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - lbTerrainData is not defined for " + lbTerrainTreeImported.prefab.name + " in " + landscape.name + "." + terrain.name); } }
                                else
                                {
                                    //Debug.Log("Matched " + lbTerrainTreeImported.prefab.name);

                                    // Get all the tree instances in this terrain for this tree type
                                    lbTerrainTreeInstanceExtList = LBTerrainTreeInstanceExt.ToLBTerrainTreeInstanceExtList(tData.treeInstances, ttIdx);

                                    // If some terrains have no Unity trees, we still want an empty list to avoid errors when populating the landscape
                                    if (lbTerrainTreeInstanceExtList == null) { lbTerrainTreeInstanceExtList = new List<LBTerrainTreeInstanceExt>(); }
                                    if (lbTerrainTreeInstanceExtList != null)
                                    {
                                        //Debug.Log("[DEBUG] INFO Found " + lbTerrainTreeInstanceExtList.Count + " " + lbTerrainTreeImported.prefab.name + " trees in " + landscape.name + "." + terrain.name);

                                        lbTerrainTreeImported.lbTerrainDataList[terrainIndex].sourceTerrainName = terrain.name;
                                        lbTerrainTreeImported.lbTerrainDataList[terrainIndex].sourceTerrainDataName = tData.name;
                                        lbTerrainTreeImported.lbTerrainDataList[terrainIndex].lbTerrainTreeInstanceExtList = lbTerrainTreeInstanceExtList;

                                        isSuccessful = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return isSuccessful;
        }

        #endregion

        #region Public Static Grass Methods

        /// <summary>
        /// Import a Unity Terrain grass detail density map in LBTerrainData for each grass type.
        /// If any one grass type succeeds, the method will return true.
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="terrain"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static bool ImportGrass(LBLandscape landscape, Terrain terrain, int terrainIndex, List<LBTerrainGrass> importedTerrainGrassList, bool showErrors)
        {
            bool isSuccessful = false;

            string methodName = "LBTerrainData.ImportGrass";

            if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscape is null"); } }
            else if (importedTerrainGrassList == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - list of imported terrain grass types is null."); } }
            else if (importedTerrainGrassList.Count < 1) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - no imported terrain grass types provided."); } }
            else if (terrain == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - terrain is null in " + landscape.name); } }
            else if (terrain.terrainData == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - terrain.terrainData is null for " + landscape.name + "." + terrain.name); } }
            {
                TerrainData tData = terrain.terrainData;

                int detailWidth = tData.detailWidth;
                int detailHeight = tData.detailHeight;

                // Get a list of the grass prototypes from the terrain
                List<LBTerrainGrass> terrainGrassList = LBTerrainGrass.ToLBTerrainGrassList(tData.detailPrototypes);
                if (terrainGrassList != null)
                {
                    // Loop through all the grass prototypes in this terrain
                    for (int gtIdx = 0; gtIdx < terrainGrassList.Count; gtIdx++)
                    {
                        LBTerrainGrass lbTerrainGrass = terrainGrassList[gtIdx];

                        // Match this with one of the imported terrain grass types (if we can)
                        LBTerrainGrass lbTerrainGrassImported = importedTerrainGrassList.Find(gr => gr.texture == lbTerrainGrass.texture &&
                                                                                                    gr.useMeshPrefab == lbTerrainGrass.useMeshPrefab &&
                                                                                                    (!gr.useMeshPrefab ? true : gr.meshPrefab == null ? lbTerrainGrass == null : (lbTerrainGrass == null ? false : gr.meshPrefab.name == lbTerrainGrass.meshPrefab.name)) &&
                                                                                                    gr.meshPrefab == lbTerrainGrass.meshPrefab &&
                                                                                                    gr.minHeight == lbTerrainGrass.minHeight &&
                                                                                                    gr.maxHeight == lbTerrainGrass.maxHeight &&
                                                                                                    gr.minWidth == lbTerrainGrass.minWidth &&
                                                                                                    gr.maxWidth == lbTerrainGrass.maxWidth &&
                                                                                                    gr.dryColour == lbTerrainGrass.dryColour &&
                                                                                                    gr.healthyColour == lbTerrainGrass.healthyColour &&
                                                                                                    gr.noiseSpread == lbTerrainGrass.noiseSpread &&
                                                                                                    gr.detailRenderMode == lbTerrainGrass.detailRenderMode);

                        // If we found a match, get the terrain grass details for this grass type within this terrain
                        if (lbTerrainGrassImported != null)
                        {
                            if (lbTerrainGrassImported.texture == null && !lbTerrainGrassImported.useMeshPrefab) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - grass texture is null in " + landscape.name + "." + terrain.name + ". Please Report"); } }
                            else if (lbTerrainGrassImported.lbTerrainDataList == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - lbTerrainDataList is not defined for " + lbTerrainGrassImported.texture.name + " in " + landscape.name + "." + terrain.name); } }
                            else if (lbTerrainGrassImported.lbTerrainDataList.Count <= terrainIndex) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - lbTerrainData is not defined for " + lbTerrainGrassImported.texture.name + " in " + landscape.name + "." + terrain.name); } }
                            else if (lbTerrainGrassImported.lbTerrainDataList[terrainIndex] == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - lbTerrainDataList[" + terrainIndex + "] is not defined for " + lbTerrainGrassImported.texture.name + " in " + landscape.name + "." + terrain.name); } }
                            else
                            {
                                int[,] detailDensityMap = tData.GetDetailLayer(0, 0, detailWidth, detailHeight, gtIdx); // new int[detailWidth, detailHeight];

                                if (detailDensityMap == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not allocate detailDensityMap when getting terrain data for " + landscape.name + "." + terrain.name); } }
                                else
                                {
                                    // terrainData name and the Terrain object name may be different if created outside LB
                                    // We want the terrainData name, as that is what will be used in LBLandscapeTerrain.PopulateTerrainWithGrass(..).
                                    lbTerrainGrassImported.lbTerrainDataList[terrainIndex].sourceTerrainName = terrain.name;
                                    lbTerrainGrassImported.lbTerrainDataList[terrainIndex].sourceTerrainDataName = tData.name;
                                    lbTerrainGrassImported.lbTerrainDataList[terrainIndex].grassDetailDensityData = null;

                                    // Grass is stored within a y,x map of integers within a terrain (int [,] detailDensityMap)
                                    // LB is storing it as a single dimensional, serializable array.
                                    lbTerrainGrassImported.lbTerrainDataList[terrainIndex].grassDetailDensityData = new int[detailWidth * detailHeight];

                                    if (lbTerrainGrassImported.lbTerrainDataList[terrainIndex].grassDetailDensityData == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not create grassDetailDensityData array for " + landscape.name + "." + terrain.name); } }
                                    else
                                    {
                                        //Debug.Log("INFO: " + methodName + " - lbTerrainDataList[" + terrainIndex + "] found for " + lbTerrainGrassImported.texture.name + " in " + landscape.name + "." + terrain.name);

                                        for (int x = 0; x < detailWidth; x++)
                                        {
                                            for (int y = 0; y < detailHeight; y++)
                                            {
                                                lbTerrainGrassImported.lbTerrainDataList[terrainIndex].grassDetailDensityData[y * detailWidth + x] = detailDensityMap[x, y];
                                            }
                                        }
                                    }

                                    isSuccessful = true;
                                }
                            }
                        }
                    }
                }
            }

            return isSuccessful;
        }

        /// <summary>
        /// Get the Grass (detail) resolution for the supplied terrainData
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="tData"></param>
        /// <param name="lbTerrainDataList"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static int GetGrassDetailResolution(LBLandscape landscape, TerrainData tData, List<LBTerrainData> lbTerrainDataList, bool showErrors)
        {
            string methodName = "LBTerrainData.GetGrassDetailResolution";
            // LB default
            int detailResolution = 1024;

            if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscape is null"); } }
            else if (lbTerrainDataList == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - lbTerrainDataList is null"); } }
            else if (tData == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - terrainData is null for " + landscape.name); } }
            else
            {
                LBTerrainData lbTerrainData = lbTerrainDataList.Find(td => td.sourceTerrainDataName == tData.name);
                if (lbTerrainData != null) { detailResolution = lbTerrainData.GrassDetailResolution; }
            }

            return detailResolution;
        }

        #endregion

        #region Private Methods


        #endregion
    }
}