 using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LandscapeBuilder
{
    [System.Serializable]
    public class LBRaw
    {
        #region Enumerations

        public enum SourceFileType
        {
            RAW = 0,
            PNG = 1
        }

        #endregion

        #region Variables and Properties

        public string dataSourceName;           // The RAW File name

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

        public SourceFileType sourceFileType;

        #endregion

        #region Constructors

        public LBRaw()
        {
            SetClassDefaults();
        }

        /// <summary>
        /// Clone constructor
        /// </summary>
        /// <param name="lbRaw"></param>
        public LBRaw(LBRaw lbRaw)
        {
            if (lbRaw == null) { SetClassDefaults(); }
            else
            {
                if (string.IsNullOrEmpty(this.dataSourceName)) { this.dataSourceName = "unknown"; }
                else { this.dataSourceName = lbRaw.dataSourceName; }

                // Heightmap data
                if (lbRaw.rawHeightData == null)
                {
                    this.rawHeightData = null;
                    this.rawMinHeight = 0;
                    this.rawMaxHeight = 0;
                    this.rawSourceWidth = 0;
                    this.rawSourceLength = 0;
                    this.sourceFileType = SourceFileType.RAW;
                }
                else
                {
                    // A shallow copy (clone) works here because it is a primative/immutable type
                    // This should create a true copy rather than a reference to the input array
                    this.rawHeightData = lbRaw.rawHeightData.Clone() as byte[];

                    this.rawMinHeight = lbRaw.rawMinHeight;
                    this.rawMaxHeight = lbRaw.rawMaxHeight;
                    this.rawSourceWidth = lbRaw.rawSourceWidth;
                    this.rawSourceLength = lbRaw.rawSourceLength;

                    this.sourceFileType = lbRaw.sourceFileType;
                }
            }
        }

        #endregion

        #region Public Non-Static Methods

        /// <summary>
        /// Set the default values for a new LBRaw class instance
        /// </summary>
        private void SetClassDefaults()
        {
            this.rawHeightData = null;
            this.dataSourceName = "unknown";
            this.rawMinHeight = 0;
            this.rawMaxHeight = 0;
            this.rawSourceWidth = 0;
            this.rawSourceLength = 0;
            sourceFileType = SourceFileType.RAW;
        }

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
        /// Returns an averaged height from normalised coords xCoord,yCoord.
        /// This function finds the four heightmap points enclosing the coordinates and
        /// blends between them depending on how close the coordinates are to each point
        /// </summary>
        /// <param name="xCoord"></param>
        /// <param name="yCoord"></param>
        /// <returns></returns>
        public ushort SampleHeightAtCoordinates(float xCoord, float yCoord)
        {
            // Get texture-space coordinates from 0-1 coordinates
            float scaledXCoord = Mathf.Clamp(xCoord * (rawSourceWidth - 1), 0f, (float)rawSourceWidth - 1f);
            float scaledYCoord = Mathf.Clamp(yCoord * (rawSourceLength - 1), 0f, (float)rawSourceLength - 1f);
            int minScaledXCoord = (int)scaledXCoord;
            int minScaledYCoord = (int)scaledYCoord;

            if (minScaledXCoord > rawSourceWidth - 2) { minScaledXCoord = rawSourceWidth - 2; }
            if (minScaledYCoord > rawSourceLength - 2) { minScaledYCoord = rawSourceLength - 2; }

            // Get bottom-left height
            int byteIndex = minScaledYCoord * (rawSourceWidth * 2) + (minScaledXCoord * 2);
            // LB stores RAW data in little endian (Windows) format, which is more suited to Intel processors.
            ushort heightBL = (ushort)((rawHeightData[byteIndex + 1] << 8) | rawHeightData[byteIndex]);

            // Get bottom-right height
            byteIndex = minScaledYCoord * (rawSourceWidth * 2) + ((minScaledXCoord + 1) * 2);
            // LB stores RAW data in little endian (Windows) format, which is more suited to Intel processors.
            ushort heightBR = (ushort)((rawHeightData[byteIndex + 1] << 8) | rawHeightData[byteIndex]);

            // Get top-left height
            byteIndex = (minScaledYCoord + 1) * (rawSourceWidth * 2) + (minScaledXCoord * 2);

            // LB stores RAW data in little endian (Windows) format, which is more suited to Intel processors.
            ushort heightTL = (ushort)((rawHeightData[byteIndex + 1] << 8) | rawHeightData[byteIndex]);

            // Get top-right height
            byteIndex = (minScaledYCoord + 1) * (rawSourceWidth * 2) + ((minScaledXCoord + 1) * 2);
            // LB stores RAW data in little endian (Windows) format, which is more suited to Intel processors.

            ushort heightTR = (ushort)((rawHeightData[byteIndex + 1] << 8) | rawHeightData[byteIndex]);

            // Blend the heights of the various cells together
            float horizontalBlendFactor = scaledXCoord - minScaledXCoord;
            float verticalBlendFactor = scaledYCoord - minScaledYCoord;
            ushort bottomHeights = (ushort)(((1f - horizontalBlendFactor) * heightBL) + (horizontalBlendFactor * heightBR));
            ushort topHeights = (ushort)(((1f - horizontalBlendFactor) * heightTL) + (horizontalBlendFactor * heightTR));
            return (ushort)(((1f - verticalBlendFactor) * bottomHeights) + (verticalBlendFactor * topHeights));
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
                    rawDataInt1D[hIdx] = (ushort)((rawHeightData[(hIdx * 2) + 1] << 8) | rawHeightData[hIdx*2]);
                }
            }
        }

        /// <summary>
        /// Creates a preview mesh from the RAW height map data, with each vert offset by vertOffset
        /// NOTE: RecalculateTangents is only available in U5.6+
        /// </summary>
        /// <param name="vertOffset"></param>
        /// <param name="maxMeshResolution"></param>
        /// <returns></returns>
        public Mesh CreatePreviewMesh(Vector3 vertOffset, int maxMeshResolution = 129)
        {
            if (rawHeightData == null) { return null; }
            else if (rawHeightData.Length < 2) { return null; }
            else
            {
                Mesh previewMesh = new Mesh();

                //string methodName = "LBRaw.CreatePreviewMesh";

                // Declare outside loops for less garbage collection
                Vector3 vertPosition;
                int byteIndex = 0;
                int vertCount = 0;
                ushort heightUShort;

                int meshWidth = rawSourceWidth;
                int meshLength = rawSourceLength;
                if (meshWidth > maxMeshResolution) { meshWidth = maxMeshResolution; }
                if (meshLength > maxMeshResolution) { meshLength = maxMeshResolution; }

                int pixelScalingX = (int)((rawSourceWidth - 1) / (meshWidth - 1));
                int pixelScalingZ = (int)((rawSourceWidth - 1) / (meshWidth - 1));

                // Initialise mesh lists
                List<Vector3> verts = new List<Vector3>();
                List<Vector2> uvs = new List<Vector2>();
                List<int> triangles = new List<int>();

                // Loop through all the x,z heightmap coordinates in this chunk of the terrain
                // Triangle numbers are zero based (triX,triZ) while the position in the terrain
                // is offset by the starting location of the chunk.
                for (int x = 0; x < rawSourceWidth; x += pixelScalingX)
                {
                    for (int z = 0; z < rawSourceLength; z += pixelScalingZ)
                    {
                        // Create vert for top right of quad

                        // Get the index of the byte for this x,z position
                        byteIndex = z * (rawSourceWidth * 2) + (x * 2);

                        // LB stores RAW data in little endian (Windows) format, which is more suited to Intel processors.
                        heightUShort = (ushort)((rawHeightData[byteIndex + 1] << 8) | rawHeightData[byteIndex]);

                        // Heights are stored as [height,width] and are 0.0 - 1.0f.

                        // Create the vert as a 0-1 position
                        // Normalise the RAW Pixel - converting it to a range of 0 to 1
                        vertPosition = new Vector3((float)x / (float)(rawSourceWidth - 1), LBLandscapeTerrain.Normalise(heightUShort, rawMinHeight, rawMaxHeight), (float)z / (float)(rawSourceLength - 1));
                        verts.Add(vertPosition + vertOffset);

                        // Generic uvs (simply 0-1 coordinates of vert position)
                        uvs.Add(new Vector2(vertPosition.x, vertPosition.z));

                        // Add the two triangles for the quad
                        // Not required if on left or bottom edges of the mesh
                        if (x < rawSourceWidth - 1 && z < rawSourceLength - 1)
                        {
                            // Bottom (left) triangle
                            // Bottom left of quad
                            triangles.Add(vertCount);
                            // Bottom right of quad
                            triangles.Add(vertCount + 1);
                            // Top right of quad
                            triangles.Add(vertCount + meshWidth + 1);

                            // Top (right) triangle
                            // Top left of quad
                            triangles.Add(vertCount + meshWidth);
                            // Bottom left of quad
                            triangles.Add(vertCount);
                            // Top right of quad
                            triangles.Add(vertCount + meshWidth + 1);
                        }

                        // Increment the vert count
                        vertCount++;
                    }
                }

                //string methodName = "LBRaw.CreatePreviewMesh";
                //Debug.Log("INFO: " + methodName + " Mesh verts:" + verts.Count + " tris:" + triangles.Count);

                // Set mesh data
                previewMesh.vertices = verts.ToArray();
                previewMesh.uv = uvs.ToArray();
                previewMesh.triangles = triangles.ToArray();
                previewMesh.RecalculateNormals();
#if UNITY_5_6_OR_NEWER
                previewMesh.RecalculateTangents();
#endif
                previewMesh.name = dataSourceName + " preview mesh";

                // Return the generated mesh
                return previewMesh;
            }
        }

        /// <summary>
        /// Validate the RAWHeightData.
        /// </summary>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public bool ValidateRAWHeightData(bool showErrors)
        {
            bool isValid = false;
            string methodName = "LBRaw.ValidateRAWHeightData";

            if (!HasRAWHeightData()) { if (showErrors) Debug.LogWarning("ERROR: " + methodName + " - no raw height data to validate"); }
            else
            {
                isValid = true;
            }

            return isValid;
        }

        #endregion

        #region Public Static Heigthmap Methods

        /// <summary>
        /// Does the list of LBRaw contain any RAW Height data?
        /// </summary>
        /// <param name="lbRawList"></param>
        /// <returns></returns>
        public static bool HasRAWHeightData(List<LBRaw> lbRawList)
        {
            bool isSuccessful = false;

            if (lbRawList != null)
            {
                if (lbRawList.Count > 0)
                {
                    for (int td = 0; td < lbRawList.Count; td++)
                    {
                        if (lbRawList[td] != null)
                        {
                            if (lbRawList[td].HasRAWHeightData()) { isSuccessful = true; break; }
                        }
                    }
                }
            }

            return isSuccessful;
        }

        /// <summary>
        /// Load a RAW file from disk into rawHeightData (which is stored in LB as 16-bit little endian [windows] RAW)
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="isMac"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static LBRaw ImportHeightmapRAW(string filePath, bool isMac, bool showErrors)
        {
            LBRaw lbRaw = null;

            string methodName = "LBRaw.ImportHeightmapRAW";

            // Perform basic validation
            if (string.IsNullOrEmpty(filePath)) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - RAW file is not available"); } }
            else if (!System.IO.File.Exists(filePath)) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - RAW file does not exist: " + filePath); } }
            else if (!System.IO.Path.HasExtension(filePath)) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - the filename does not have the raw extension. File: " + filePath); } }
            else if (System.IO.Path.GetExtension(filePath).ToLower() != ".raw") { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - the filename is not of type raw. File: " + filePath); } }
            else
            {
                byte[] rawData = null;

                // RAW data is stored as 16-bit, consisting of a little and big endian (8bit + 8bit)
                try
                {
                    rawData = System.IO.File.ReadAllBytes(filePath);
                }
                catch (System.Exception ex)
                {
                    if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not read RAW file " + ex.Message); }
                }

                if (rawData == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not get heights from file: " + filePath); } }
                else if (rawData.Length < 3) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - RAW data file is empty or invalid: " + filePath); } }
                else
                {
                    lbRaw = new LBRaw();
                    if (lbRaw == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not create LBRaw instance"); } }
                    else
                    {
                        // This assumes that the RAW file is square
                        lbRaw.rawHeightData = null;
                        lbRaw.dataSourceName = System.IO.Path.GetFileName(filePath);
                        lbRaw.rawSourceWidth = Mathf.RoundToInt(Mathf.Sqrt(rawData.Length / 2));
                        lbRaw.rawSourceLength = Mathf.RoundToInt(Mathf.Sqrt(rawData.Length / 2));
                        lbRaw.sourceFileType = SourceFileType.RAW;

                        // RAW data is stored as 16-bit, consisting of a little and big endian (8bit + 8bit)
                        lbRaw.rawHeightData = new byte[lbRaw.rawSourceWidth * lbRaw.rawSourceLength * 2];

                        if (lbRaw.rawHeightData == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not create rawHeightData array"); } }
                        else
                        {
                            int rawHeightDataLength = lbRaw.rawHeightData.Length;

                            ushort sampleMinValue = ushort.MaxValue;
                            ushort sampleMaxValue = ushort.MinValue;
                            ushort sample16bit = 0;

                            for (int byteIndex = 0; byteIndex < rawHeightDataLength - 1; byteIndex += 2)
                            {
                                // LB stores RAW data in little endian (Windows) format, which is more suited to Intel processors.
                                if (isMac)
                                {
                                    // Mac: big endian
                                    lbRaw.rawHeightData[byteIndex + 1] = rawData[byteIndex];
                                    lbRaw.rawHeightData[byteIndex] = rawData[byteIndex + 1];
                                    sample16bit = (ushort)(rawData[byteIndex] << 8 | rawData[byteIndex + 1]);
                                }
                                else
                                {
                                    // Windows: little endian
                                    lbRaw.rawHeightData[byteIndex + 1] = rawData[byteIndex + 1];
                                    lbRaw.rawHeightData[byteIndex] = rawData[byteIndex];
                                    sample16bit = (ushort)(rawData[byteIndex + 1] << 8 | rawData[byteIndex]);
                                }

                                // keep track of the min/max values in the input RAW data
                                if (sample16bit > sampleMaxValue) { sampleMaxValue = sample16bit; }
                                if (sample16bit < sampleMinValue) { sampleMinValue = sample16bit; }
                            }

                            if (sampleMinValue == ushort.MaxValue) { sampleMinValue = ushort.MinValue; }

                            lbRaw.rawMinHeight = sampleMinValue;
                            lbRaw.rawMaxHeight = sampleMaxValue;
                        }
                    }
                }
            }

            return lbRaw;
        }

        /// <summary>
        /// Load a PNG file from disk into rawHeightData (which is stored in LB as 16-bit little endian [windows] RAW)
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static LBRaw ImportHeightmapPNG(string filePath, bool showErrors)
        {
            LBRaw lbRaw = null;

            string methodName = "LBRaw.ImportHeightmapPNG";

            // Perform basic validation
            if (string.IsNullOrEmpty(filePath)) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - PNG file is not available"); } }
            else if (!System.IO.File.Exists(filePath)) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - PNG file does not exist: " + filePath); } }
            else if (!System.IO.Path.HasExtension(filePath)) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - the filename does not have the raw extension. File: " + filePath); } }
            else if (System.IO.Path.GetExtension(filePath).ToLower() != ".png") { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - the filename is not of type png. File: " + filePath); } }
            else
            {
                byte[] pngData = null;

                // RAW data is stored as 16-bit, consisting of a little and big endian (8bit + 8bit)
                try
                {
                    pngData = System.IO.File.ReadAllBytes(filePath);
                }
                catch (System.Exception ex)
                {
                    if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not read RAW file " + ex.Message); }
                }

                if (pngData == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not get heights from file: " + filePath); } }
                else if (pngData.Length < 3) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - PNG data file is empty or invalid: " + filePath); } }
                else
                {
                    lbRaw = new LBRaw();
                    if (lbRaw == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not create LBRaw instance"); } }
                    else
                    {
                        // This assumes that the RAW file is square
                        lbRaw.rawHeightData = null;
                        lbRaw.dataSourceName = System.IO.Path.GetFileName(filePath);
                        lbRaw.sourceFileType = SourceFileType.PNG;

                        // Create a very small texture. Texture2D.LoadImage will auto-resize it.
                        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);

                        if (texture == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not create Texture2D"); } }
                        else
                        {
                            // Load the PNG raw data and auto-resize the texture
                            texture.LoadImage(pngData, false);

                            // Is the width 2 ^ n where n is an integer
                            float log2 = Mathf.Log(texture.width, 2);
                            if (log2 - (int)log2 < 0.0001f)
                            {
                                // Make the width (2 ^ n) + 1
                                lbRaw.rawSourceWidth = texture.width + 1;
                            }
                            else
                            {
                                // Scale down to the nearest 2 ^ n width where n is an integer - then add 1
                                lbRaw.rawSourceWidth = ((int)Mathf.Pow(2f, (int)log2)) + 1;
                            }

                            // Assume this is a square image                     
                            lbRaw.rawSourceLength = lbRaw.rawSourceWidth;

                            //Debug.Log("PNG tex size: " + texture.width + "x" + texture.height + " resize to " + lbRaw.rawSourceWidth + "x" + lbRaw.rawSourceWidth);

                            // resize to a square texture with width = 2^n + 1
                            LBTextureOperations.TexturePointScale(texture, lbRaw.rawSourceWidth, lbRaw.rawSourceLength);

                            // Get the pixel colours from the base texture (mipmap = 0)
                            Color[] colours = texture.GetPixels();

                            if (colours == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not get pixels from imported PNG file"); } }
                            else
                            {
                                // TEST CODE
                                //LBEditorHelper.SaveMapTexture(texture, filePath + "_2.png", 2048);

                                // RAW data is stored as 16-bit, consisting of a little and big endian (8bit + 8bit)
                                lbRaw.rawHeightData = new byte[lbRaw.rawSourceWidth * lbRaw.rawSourceLength * 2];

                                if (lbRaw.rawHeightData == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not create rawHeightData array"); } }
                                else
                                {
                                    int rawHeightDataLength = lbRaw.rawHeightData.Length;

                                    ushort sampleMinValue = ushort.MaxValue;
                                    ushort sampleMaxValue = ushort.MinValue;
                                    ushort sample16bit = 0;

                                    for (int byteIndex = 0; byteIndex < rawHeightDataLength - 1; byteIndex += 2)
                                    {
                                        //pixel = colours[byteIndex / 2].grayscale;
                                        sample16bit = (ushort)(colours[byteIndex / 2].grayscale * 65535f);

                                        // LB stores RAW data in little endian (Windows) format, which is more suited to Intel processors.
                                        // Windows: little endian
                                        lbRaw.rawHeightData[byteIndex + 1] = System.Convert.ToByte(sample16bit >> 8);
                                        lbRaw.rawHeightData[byteIndex] = System.Convert.ToByte(sample16bit & 255);

                                        // keep track of the min/max values in the input RAW data
                                        if (sample16bit > sampleMaxValue) { sampleMaxValue = sample16bit; }
                                        if (sample16bit < sampleMinValue) { sampleMinValue = sample16bit; }
                                    }

                                    if (sampleMinValue == ushort.MaxValue) { sampleMinValue = ushort.MinValue; }

                                    lbRaw.rawMinHeight = sampleMinValue;
                                    lbRaw.rawMaxHeight = sampleMaxValue;

                                    //Debug.Log("PNG RAW min/max: " + sampleMinValue + "," + sampleMaxValue);
                                }
                            }
                        }
                    }
                }
            }

            return lbRaw;
        }

        #endregion
    }
}