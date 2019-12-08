#define _LBIMPORT_TIFF_DEBUG_MODE
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using BitMiracle.LibTiff.Classic;

namespace LandscapeBuilder
{
    /// <summary>
    /// Methods in this class would usually be placed in LBImport and LBTerrainData,
    /// however we have decided to place all of the LibTIFF 3rd party library under
    /// an Editor folder (projectname.CSharp.Editor) so it is not easily accessible
    /// from projectname.CSharp. We have LibTIFF in the Editor folder so that all the
    /// classes are automatically excluded at runtime. There may be a better way to
    /// do this...
    /// </summary>
    public class LBImportTIFF : MonoBehaviour
    {
        #region Import GeoTIFF heightmap data to a Topography Layer

        /// <summary>
        /// Import a GeoTIFF file from disk into a Topography Layer
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="rawFileList"></param>
        /// <param name="isMacRAWFormat"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static bool ImportGeoTIFFHeightmap(LBLandscape landscape, string filePath, bool showErrors)
        {
            bool isSuccessful = false;
            string methodName = "LBImportTIFF.ImportGeoTIFFHeightmap";

            if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscape is null. Please report"); } }
            else if (landscape.topographyLayersList == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - topographyLayers is not defined. Please report"); } }
            else if (string.IsNullOrEmpty(filePath)) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - filePath is not defined. Please report."); } }
            else if (landscape.landscapeTerrains == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscapeTerrains is not defined. Please report"); } }
            else if (landscape.landscapeTerrains.Length < 1) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - No terrains in landscape"); } }
            else if (landscape.topographyLayersList.Exists(lyr => lyr.type == LBLayer.LayerType.UnityTerrains)) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - Multiple Unity Terrain Layers are not supported in the same landscape"); } }
            else
            {
                float generationStartTime = Time.realtimeSinceStartup;
                int numTerrains = landscape.landscapeTerrains.Length;

                List<LBTerrainData> lbTerrainDataList = new List<LBTerrainData>();

                // Track the min/max heights for all the terrains
                ushort sampleMinHeight = ushort.MaxValue;
                ushort sampleMaxHeight = ushort.MinValue;

                for (int index = 0; index < numTerrains; index++)
                {
                    //Debug.Log("INFO: " + methodName + " importing TIFF for terrain: " + landscape.landscapeTerrains[index].name + " using file: " + filePath);

                    // Import TIFF region for the current terrain
                    LBTerrainData lbTerrainData = ImportHeightmapTIFF(landscape, landscape.landscapeTerrains[index], filePath, ref sampleMinHeight, ref sampleMaxHeight, showErrors);

                    // Don't continue if at least one terrain cannot be imported
                    if (lbTerrainData == null) { break; }
                    else
                    {
                        lbTerrainDataList.Add(lbTerrainData);
                    }

                    // Only create a new layer if all terrains were imported
                    if (lbTerrainDataList.Count == numTerrains)
                    {
                        LBLayer lbLayer = new LBLayer();
                        if (lbLayer != null)
                        {
                            lbLayer.type = LBLayer.LayerType.UnityTerrains;
                            lbLayer.lbTerrainDataList = lbTerrainDataList;
                            lbLayer.isCheckHeightRangeDiff = true;
                            lbLayer.heightScale = 1.0f;
                            lbLayer.normaliseImage = false;

                            // Insert at the top of the list
                            landscape.topographyLayersList.Insert(0, lbLayer);
                        }

                        if (landscape.showTiming)
                        {
                            Debug.Log("Time taken to import TIFF heightmaps: " + (Time.realtimeSinceStartup - generationStartTime).ToString("00.00") + " seconds.");
                        }

                        isSuccessful = true;
                    }
                }
            }

            return isSuccessful;
        }

        /// <summary>
        /// Load a GeoTIFF file from disk into rawHeightData for the given terrain.
        /// Currently we load the whole TIFF file and find the pixels inside the file
        /// for each pixel in the terrain. Potentially it would be more efficient to just
        /// scan the data that applies to the current terrain, howbeit more complex.
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="terrain"></param>
        /// <param name="filePath"></param>
        /// <param name="minHeight"></param>
        /// <param name="maxHeight"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static LBTerrainData ImportHeightmapTIFF(LBLandscape landscape, Terrain terrain, string filePath, ref ushort minHeight, ref ushort maxHeight, bool showErrors)
        {
            LBTerrainData lbTerrainData = null;

            string methodName = "LBImportTIFF.ImportHeightmapTIFF";

            if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscape is null"); } }
            else if (terrain == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - terrain is null in " + landscape.name); } }
            else if (terrain.terrainData == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - terrain.terrainData is null for " + landscape.name + "." + terrain.name); } }
            else if (string.IsNullOrEmpty(filePath)) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - RAW file is not available for " + landscape.name + "." + terrain.name); } }
            else if (!System.IO.File.Exists(filePath)) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - RAW file does not exist: " + filePath); } }
            {
#if LBIMPORT_TIFF_DEBUG_MODE
            Debug.Log("INFO: " + methodName + " importing TIFF for terrain: " + terrain.name + " using file: " + filePath);
#endif

                Tiff tiff = null;
                try
                {
                    // open the geotiff file in read-only mode (the only other option is write mode - the library does not support rw mode)
                    tiff = Tiff.Open(filePath, "r");
                    if (tiff == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " could not open TIFF file: " + filePath); } }
                    else if (tiff.IsTiled()) { if (showErrors) { Debug.LogWarning("INFO: " + methodName + " Landscape Builder currently does not support TIFF files that contain tiles"); } }
                    else if (tiff.NumberOfDirectories() < (short)1) { if (showErrors) { Debug.LogWarning("INFO: " + methodName + " the TIFF file does not appear to contain any image data."); } }
                    else
                    {
                        // We assume there is only 1 "directory" within the TIFF (as they can support multiple images or directories within a single file)
                        // Just get the first "directory" by using [0].
                        int bitsPerSample = tiff.GetField(TiffTag.BITSPERSAMPLE)[0].ToInt();
                        int imgWidth = tiff.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
                        int imgLength = tiff.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
                        int scanlineSize = tiff.ScanlineSize();
                        int sampleFormat = tiff.GetFieldDefaulted(TiffTag.SAMPLEFORMAT)[0].ToInt();
                        int samplesPerPixel = tiff.GetFieldDefaulted(TiffTag.SAMPLESPERPIXEL)[0].ToInt();

#if LBIMPORT_TIFF_DEBUG_MODE

                    int numTiles = tiff.GetField(TiffTag.TILEBYTECOUNTS).Length;
                    int rowsPerStrip = tiff.GetField(TiffTag.ROWSPERSTRIP)[0].ToInt();
                    int imgCompression = tiff.GetField(TiffTag.COMPRESSION)[0].ToInt();

                    Debug.Log("INFO: " + methodName + " samplesPerPixel:" + samplesPerPixel + " bitsPerSample:" + bitsPerSample + " imgWidth:" + imgWidth + " imgLength:" + imgLength +
                              " Num Tiles: " + numTiles + " rowsPerStrip:" + rowsPerStrip + " imgCompression: " + Enum.GetName(typeof(Compression), imgCompression) +
                                " SampleFormat: " + Enum.GetName(typeof(SampleFormat), sampleFormat));

#endif

                        // NOTE: Orientation seems to be obsolete and almost always returns TOPLEFT

                        if (imgWidth < 1 || imgLength < 1) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - the TIFF image size is invalid (" + imgWidth + "," + imgLength + ") in " + filePath); } }
                        else if (bitsPerSample != 8 && bitsPerSample != 16 && bitsPerSample != 32) { if (showErrors) { Debug.LogWarning("INFO: " + methodName + " - Landscape Builder currently only supports 8, 16 or 32bit TIFF files"); } }
                        else if (samplesPerPixel != 1) { if (showErrors) { Debug.LogWarning("INFO: " + methodName + " - Landscape Builder currently only supports GeoTIFF files with 1 sample per pixel. Try using a Image Layer for RGB or RGBA TIFF files."); } }
                        else if (scanlineSize == 0) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " the line size in the TIFF file is zero for file: " + filePath); } }
                        else if (scanlineSize != imgWidth * (bitsPerSample / 8)) { if (showErrors) { Debug.LogWarning("INFO: " + methodName + " the TIFF data scan line size (" + scanlineSize + ") does not seem to match the width and bits per sample: " + imgWidth + "pixels * (" + bitsPerSample + "bits/8)"); } }
                        else
                        {
                            byte[] scanline = new byte[scanlineSize];
                            if (scanline == null) { if (showErrors) { Debug.LogWarning("INFO: " + methodName + " could not reserve memory to scan TIFF data lines."); } }
                            else
                            {
                                // Attempt to allocate enough space for the whole image
                                int tiffImageBufferSize = imgWidth * imgLength * (bitsPerSample / 8);
                                byte[] tiffImageBuffer = new byte[tiffImageBufferSize];

                                if (tiffImageBuffer == null) { if (showErrors) { Debug.LogWarning("INFO: " + methodName + " could not reserve memory to hold the TIFF data."); } }
                                else
                                {
                                    int tiffImageOffset = 0;

                                    // There should be one line for each pixel the TIFF image is in length.
                                    for (int sl = 0; sl < imgLength; sl++)
                                    {
                                        if (tiff.ReadScanline(scanline, sl))
                                        {
                                            // Copy the line into the image buffer
                                            Buffer.BlockCopy(scanline, 0, tiffImageBuffer, tiffImageOffset, scanlineSize);
                                            tiffImageOffset += scanlineSize;
                                        }
                                        else
                                        {
                                            if (showErrors) { Debug.LogWarning("INFO: " + methodName + " ReadScanline failed at offset " + tiffImageOffset); }
                                            break;
                                        }
                                    }

                                    // Make sure we read in all the data from the first image in the TIFF file
                                    if ((tiffImageOffset / scanlineSize) == imgLength)
                                    {
                                        lbTerrainData = new LBTerrainData();
                                        if (lbTerrainData == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not create LBTerrainData instance for " + landscape.name + "." + terrain.name); } }
                                        else
                                        {
                                            TerrainData tData = terrain.terrainData;
                                            int heightmapResolution = tData.heightmapResolution;

                                            // terrainData name and the Terrain object name may be different if created outside LB
                                            // We want the terrainData name, as that is what will be used in LBLandscapeTerrain.HeightmapFromLayers(..).
                                            lbTerrainData.sourceTerrainName = terrain.name;
                                            lbTerrainData.sourceTerrainDataName = tData.name;
                                            lbTerrainData.rawHeightData = null;
                                            lbTerrainData.dataSourceName = System.IO.Path.GetFileName(filePath);
                                            lbTerrainData.rawSourceWidth = imgWidth;
                                            lbTerrainData.rawSourceLength = imgLength;

                                            // RAW data is stored as 16-bit, consisting of a little and big endian (8bit + 8bit)
                                            lbTerrainData.rawHeightData = new byte[heightmapResolution * heightmapResolution * 2];
                                            byte[] rawHeightDataPixel = new byte[2];

                                            if (lbTerrainData.rawHeightData == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not create rawHeightData array for " + landscape.name + "." + terrain.name); } }
                                            else
                                            {
                                                int rawHeightDataLength = lbTerrainData.rawHeightData.Length;

                                                // Assume GeoTIFF is in Little Endian (Windows) format. THIS MIGHT BE WRONG...
                                                bool isMac = false;

                                                int xHeightmap = 0, zHeightmap = 0;
                                                float xPos = 0f, zPos = 0f, xPosN = 0f, zPosN = 0f;
                                                Vector3 terrainWorldPosition = terrain.transform.position;
                                                Vector3 landscapePosition = landscape.gameObject.transform.position;
                                                Vector3 heightmapScale = tData.heightmapScale;
                                                Vector2 imgCoords = Vector2.zero;

                                                ushort sample16bit = 0;
                                                int sample32bitInt = 0;
                                                float sample32bitF = 0f;

                                                ushort sampleMinValue = ushort.MaxValue;
                                                ushort sampleMaxValue = ushort.MinValue;

                                                // LBTerrainData.rawHeightData is stored as 16bit (2 x 8bit)
                                                // We need to convert our TIFF data into this format
                                                for (int byteIndex = 0; byteIndex < rawHeightDataLength - 1; byteIndex += 2)
                                                {
                                                    // Get the point in the heightmap (assume the rawHeightData matches the terrain heightmap resolution)
                                                    // There are twice as many samples in the rawHeightData (16bit) as in the terrain heightmap (single float per sample)
                                                    zHeightmap = (int)((byteIndex / 2) / heightmapResolution);
                                                    xHeightmap = (byteIndex / 2) % heightmapResolution;

                                                    // Get world position of heightmap point
                                                    xPos = (heightmapScale.x * xHeightmap) + terrainWorldPosition.x - landscapePosition.x;
                                                    zPos = (heightmapScale.z * zHeightmap) + terrainWorldPosition.z - landscapePosition.z;

                                                    // Get normalised position in landscape (0.0-1.0)
                                                    xPosN = xPos / landscape.size.x;
                                                    zPosN = zPos / landscape.size.y;

                                                    // Get the position in the TIFF data
                                                    // z-axis is flipped because TIFF image typically is 0,0 is topleft, but heightmap data has 0,0 at bottomleft.
                                                    imgCoords = new Vector2(xPosN * (imgWidth - 1), (1f - zPosN) * (imgLength - 1));

                                                    // Get the position in the imported TIFF buffer
                                                    tiffImageOffset = ((int)imgCoords.y * scanlineSize) + (int)imgCoords.x * (bitsPerSample / 8);

                                                    // Reset destination pixel data
                                                    rawHeightDataPixel[0] = 0;
                                                    rawHeightDataPixel[1] = 0;

                                                    // Convert each pixel to a 16bit sample
                                                    if (bitsPerSample == 8)
                                                    {
                                                        // upsize the sample to 16 bits
                                                        sample16bit = (ushort)(tiffImageBuffer[tiffImageOffset] * 255);
                                                    }
                                                    else if (bitsPerSample == 16)
                                                    {
                                                        // This has been validated against the VTBuild application
                                                        sample16bit = (ushort)(tiffImageBuffer[tiffImageOffset + 1] << 8 | tiffImageBuffer[tiffImageOffset]);
                                                        //rawHeightDataPixel[0] = tiffImageBuffer[tiffImageOffset];
                                                        //rawHeightDataPixel[1] = tiffImageBuffer[tiffImageOffset+1];
                                                    }
                                                    else if (bitsPerSample == 32)
                                                    {
                                                        if ((SampleFormat)sampleFormat == SampleFormat.IEEEFP)
                                                        {
                                                            sample32bitF = BitConverter.ToSingle(tiffImageBuffer, tiffImageOffset);

                                                            // Convert to float (single), then cast to ushort. The float data seems to be heights in metres, rather than 0-1 as expected.
                                                            //sample16bit = (ushort)(BitConverter.ToSingle(tiffImageBuffer, tiffImageOffset));

                                                            // GMRT data includes -ve values (below sea level) and +ve values.
                                                            // -ve values are offset from 64K then can be fixed in editor by ticking Include Below Sealevel and clicking Fix button.
                                                            if (sample32bitF < 0f) { sample16bit = (ushort)(65535f + sample32bitF); }
                                                            else { sample16bit = (ushort)sample32bitF; }

                                                        }
                                                        else
                                                        {
                                                            // Assume INT but could be UINT...
                                                            sample32bitInt = tiffImageBuffer[tiffImageOffset + 3] << 24 | tiffImageBuffer[tiffImageOffset + 2] << 16 | tiffImageBuffer[tiffImageOffset + 1] << 8 | tiffImageBuffer[tiffImageOffset];

                                                            // Downsize to 16 bit - assume values 0 -> (2^32 -1). Convert to 1 -> (2^16 - 1)
                                                            sample16bit = (ushort)(((sample32bitInt + 1) / 65536) - 1);
                                                        }
                                                    }

                                                    // keep track of the min/max values in the input TIFF data
                                                    if (sample16bit > sampleMaxValue) { sampleMaxValue = sample16bit; }
                                                    if (sample16bit < sampleMinValue) { sampleMinValue = sample16bit; }

                                                    rawHeightDataPixel[0] = System.Convert.ToByte(sample16bit & 255);
                                                    rawHeightDataPixel[1] = System.Convert.ToByte(sample16bit >> 8);

                                                    // LB stores RAW data in little endian (Windows) format, which is more suited to Intel processors.
                                                    if (isMac)
                                                    {
                                                        // Mac: big endian
                                                        lbTerrainData.rawHeightData[byteIndex + 1] = rawHeightDataPixel[0];
                                                        lbTerrainData.rawHeightData[byteIndex] = rawHeightDataPixel[1];
                                                    }
                                                    else
                                                    {
                                                        // Windows: little endian
                                                        lbTerrainData.rawHeightData[byteIndex] = rawHeightDataPixel[0];
                                                        lbTerrainData.rawHeightData[byteIndex + 1] = rawHeightDataPixel[1];
                                                    }
                                                }

                                                if (sampleMinValue < minHeight) { minHeight = sampleMinValue; }
                                                if (sampleMaxValue > maxHeight) { maxHeight = sampleMaxValue; }

                                                if (sampleMinValue == ushort.MaxValue) { sampleMinValue = ushort.MinValue; }

                                                lbTerrainData.rawMinHeight = sampleMinValue;
                                                lbTerrainData.rawMaxHeight = sampleMaxValue;

#if LBIMPORT_TIFF_DEBUG_MODE
                                            //Debug.Log("INFO: " + methodName + " TIFF min: " + (sampleMinValue < ushort.MaxValue ? sampleMinValue : (ushort)0).ToString() + " max: " + (sampleMaxValue > ushort.MinValue ? sampleMaxValue : (ushort)0).ToString() + " for " + terrain.name);
#endif
                                            }
                                        }
                                    }

                                    tiffImageBuffer = null;
                                }

                                scanline = null;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " could not process TIFF file. " + ex.Message); }
                    lbTerrainData = null;
                }
                finally
                {
                    // Clean up.
                    if (tiff != null) { tiff.Close(); tiff.Dispose(); tiff = null; }
                }

            }
            return lbTerrainData;
        }

        #endregion

        #region Repair GeoTIFF data

        /// <summary>
        /// Fix or repair holes in GeoTIFF data. Typically used when GeoTIFF data
        /// has values > 65000 (which in metres is very unlikely)
        /// Also used with Include Below Sea Level with OpenTopo GMRT data.
        /// See notes in ImportHeightmapTIFF() under 32bit SampleFormat.IEEEFP.
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="lbLayer"></param>
        /// <param name="showErrors"></param>
        public static void FixGeoTIFFHeightmap(LBLandscape landscape, LBLayer lbLayer, bool showErrors)
        {
            string methodName = "LBImportTIFF.FixGeoTIFFHeightmap";

            if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscape is null. Please report"); } }
            else if (landscape.topographyLayersList == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - topographyLayers is not defined. Please report"); } }
            else if (lbLayer == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - lbLayer is not defined. Please report"); } }
            else if (lbLayer.type != LBLayer.LayerType.UnityTerrains ) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - lbLayer is not of type: UnityTerrains. Please report"); } }
            else if (lbLayer.lbTerrainDataList == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - lbLayer.lbTerrainDataList is not defined. Please report"); } }
            else if (lbLayer.lbTerrainDataList.Count < 1) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - lbLayer.lbTerrainDataList does not contain any data. Please report"); } }
            {
                int numTerrainData = (lbLayer.lbTerrainDataList == null ? 0 : lbLayer.lbTerrainDataList.Count);
                LBTerrainData lbTerrainData = null;

                ushort upperClip = 65000;

                // GRMT data adjustment variables. New sea-level to be 10,000
                ushort landCeiling = 65335 - 9999; // The maximum assumed height land values can be

                // We may need to use LBTerrainData.GetLandscapeScopedHeightmap() to fix holes
                // across terrain boundaries

                for (int lbTDataIdx = 0; lbTDataIdx < numTerrainData; lbTDataIdx++)
                {
                    lbTerrainData = lbLayer.lbTerrainDataList[lbTDataIdx];

                    if (lbTerrainData != null && lbTerrainData.HasRAWHeightData())
                    {
                        // Raw data is stored as a 1-dimensional USHORT byte array
                        // To fix holes we 
                        int heightmapResolution = lbTerrainData.RawHeightResolution;
                        int byteIndex, byteIndexNearby;
                        ushort heightUShort, heightUShortNearby;

                        ushort sampleMinValue = ushort.MaxValue;
                        ushort sampleMaxValue = ushort.MinValue;

                        for (int z = 0; z < heightmapResolution; z++)
                        {
                            for (int x = 0; x < heightmapResolution; x++)
                            {
                                byteIndex = z * (heightmapResolution * 2) + (x * 2);

                                // LB stores RAW data in little endian (Windows) format, which is more suited to Intel processors.
                                heightUShort = (ushort)((lbTerrainData.rawHeightData[byteIndex + 1] << 8) | lbTerrainData.rawHeightData[byteIndex]);

                                // Global Multi-Resolution Topography (GMRT) data include +ve and -ve data.
                                // As we only import into ushort, the -ve values become values close to 64K
                                if (lbLayer.isBelowSeaLevelDataIncluded)
                                {
                                    // WORKAROUD - z==0 and z = max have incorrect values.
                                    // Attempt to fix bottom and top edges with some GMRT datasets (not sure why this happens yet...)
                                    //if ((z == 0 || z == (heightmapResolution - 1)) && heightUShort == 0u)
                                    if ((z == 0) && heightUShort == 0)
                                    {
                                        ulong total = 0;
                                        int pixels = 0;
                                        ushort pixelAvg = 0;
                                        // Get matrix of nearby pixels
                                        for (int pz = -3; pz < 4; pz++)
                                        {
                                            // Ignore pixels we're trying to fix
                                            if (pz == 0) { continue; }

                                            // Ensure the pixel is within the data area
                                            if (z + pz < 0 || z + pz > heightmapResolution - 1) { continue; }

                                            for (int px = -3; px < 4; px++)
                                            {
                                                // Ignore original value and ensure the pixel is within the data area
                                                if ((pz == 0 && px == 0) || (x + px < 0 || x + px > heightmapResolution - 1)) { continue; }

                                                byteIndexNearby = (z + pz) * (heightmapResolution * 2) + ((x + px) * 2);
                                                heightUShortNearby = (ushort)((lbTerrainData.rawHeightData[byteIndexNearby + 1] << 8) | lbTerrainData.rawHeightData[byteIndexNearby]);

                                                // Only include pixels that are not being adjusted
                                                if (heightUShortNearby > 0)
                                                {
                                                    pixels++;
                                                    total += heightUShortNearby;
                                                }
                                            }
                                        }

                                        if (pixels > 0)
                                        {
                                            pixelAvg = Convert.ToUInt16((total / (ulong)pixels));
                                            if (pixelAvg > 0) { heightUShort = pixelAvg; }
                                        }
                                    }

                                    // Raise land values by 10,000
                                    if (heightUShort < landCeiling) { heightUShort += 10000; }
                                    else
                                    {
                                        // The -ve (below sea level) values have become height +ve numbers during the import
                                        // process. Move these original -ve number back under the new artifical sea level.
                                        heightUShort = (ushort)(10000u - (65535u - (uint)heightUShort));
                                    }

                                    // Convert ushort 0-65535 back to 2-bytes
                                    // LB stores RAW data in little endian (Windows) format, which is more suited to Intel processors.
                                    lbTerrainData.rawHeightData[byteIndex] = System.Convert.ToByte(heightUShort & 255);
                                    lbTerrainData.rawHeightData[byteIndex + 1] = System.Convert.ToByte(heightUShort >> 8);

                                    // keep track of the min/max values in the input TIFF data
                                    if (heightUShort > sampleMaxValue) { sampleMaxValue = heightUShort; }
                                    if (heightUShort < sampleMinValue) { sampleMinValue = heightUShort; }
                                }
                                else
                                {
                                    // Is this an extreme value?
                                    if (heightUShort >= upperClip)
                                    {
                                        ulong total = 0;
                                        int pixels = 0;
                                        ushort pixelAvg = 0;
                                        // Get matrix of nearby pixels
                                        for (int pz = -3; pz < 4; pz++)
                                        {
                                            // Ensure the pixel is within the data area
                                            if (z + pz < 0 || z + pz > heightmapResolution - 1) { continue; }

                                            for (int px = -3; px < 4; px++)
                                            {
                                                // Ignore original value and ensure the pixel is within the data area
                                                if ((pz == 0 && px == 0) || (x + px < 0 || x + px > heightmapResolution - 1)) { continue; }

                                                // Ensure the pixel is within the data area
                                                //if (x + px < 0 || x + px > heightmapResolution - 1) { continue; }
                                                byteIndexNearby = (z + pz) * (heightmapResolution * 2) + ((x + px) * 2);
                                                heightUShortNearby = (ushort)((lbTerrainData.rawHeightData[byteIndexNearby + 1] << 8) | lbTerrainData.rawHeightData[byteIndexNearby]);

                                                // Only include pixels that are not being clipped. Adjacent pixels may also need to be clipped, so we need
                                                // to not include them for averaging purposes, else the new pixel updated below will be much higher than the surrounding terrain.
                                                if (heightUShortNearby < upperClip)
                                                {
                                                    pixels++;
                                                    total += heightUShortNearby;
                                                }
                                            }
                                        }

                                        if (pixels > 0)
                                        {
                                            pixelAvg = Convert.ToUInt16((total / (ulong)pixels));
                                            if (pixelAvg > 0)
                                            {
                                                // Convert ushort 0-65535 back to 2-bytes
                                                // LB stores RAW data in little endian (Windows) format, which is more suited to Intel processors.
                                                lbTerrainData.rawHeightData[byteIndex] = System.Convert.ToByte(pixelAvg & 255);
                                                lbTerrainData.rawHeightData[byteIndex + 1] = System.Convert.ToByte(pixelAvg >> 8);
                                                //Debug.Log("x,z " + x + "," + z + " avg: " + pixelAvg);
                                            }
                                        }
                                        else
                                        {
                                            // keep track of the min/max values in the input TIFF data
                                            if (heightUShort > sampleMaxValue) { sampleMaxValue = heightUShort; }
                                            if (heightUShort < sampleMinValue) { sampleMinValue = heightUShort; }
                                        }
                                    }
                                    else // Value is not extreme so update min/max values
                                    {
                                        // keep track of the min/max values in the input TIFF data
                                        if (heightUShort > sampleMaxValue) { sampleMaxValue = heightUShort; }
                                        if (heightUShort < sampleMinValue) { sampleMinValue = heightUShort; }
                                    }
                                }
                            }
                        }

                        // Need to update max height
                        lbTerrainData.rawMinHeight = sampleMinValue;
                        lbTerrainData.rawMaxHeight = sampleMaxValue;
                    }
                }
            }
        }

        #endregion
    }
}