// Landscape Builder. Copyright (c) 2016-2019 SCSM Pty Ltd. All rights reserved.
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace LandscapeBuilder
{
    public class LBImport : MonoBehaviour
    {
        #region Public variables and properties


        #endregion

        #region Import Landscape

        /// <summary>
        /// If LB has been added to a group of terrains that were not built with Landscape Builder,
        /// some settings may need to be updated.
        /// Pass landscape by reference, in case it needs to be replaced with another instance when
        /// a terrain is reparented.
        /// </summary>
        /// <param name="landscapeToImport"></param>
        /// <param name="LB_Version"></param>
        /// <returns></returns>
        public static bool ImportLandscape(ref LBLandscape landscapeToImport, string LB_Version = "")
        {
            // First added in v1.2.0
            bool isSuccessful = false;
            bool terrainsAllSquare = true;
            bool terrainsAllSameSize = true;
            bool isLandscapeSquare = false;
            bool isLandscapeRectangle = false;
            Vector2 newLandscapeSize = Vector2.zero;
            bool isOkToContinue = true;

            Vector3 terrainSize = Vector3.zero;
            string importWarning = "cannot be imported into the current version of Landscape Builder";

            if (landscapeToImport == null)
            {
                Debug.LogError("LBUpdate ImportLandscape - no landscape to import");
            }
            else
            {
                GameObject landscapeGameObject = landscapeToImport.gameObject;
                if (landscapeGameObject == null)
                {
                    Debug.LogError("LBUpdate ImportLandscape - cannot find LB Landscape script parent gameobject");
                }
                else
                {
                    Terrain[] landscapeTerrains = landscapeGameObject.GetComponentsInChildren<Terrain>();
                    if (landscapeTerrains == null)
                    {
                        Debug.LogError("LBUpdate ImportLandscape - no terrains are children of the " + landscapeGameObject.name + " gameobject");
                    }
                    else if (landscapeTerrains.Length == 0)
                    {
                        Debug.LogError("LBUpdate ImportLandscape - no terrains are children of the " + landscapeGameObject.name + " gameobject");
                    }
                    else if (landscapeGameObject.transform.parent != null)
                    {
                        #if UNITY_EDITOR
                        string parentMsg = landscapeGameObject.name + " gameobject is not a root gameobject in the scene. Seek help in manual, on the Unity forum or on our Discord channel.";
                        UnityEditor.EditorUtility.DisplayDialog("Import Landscape", parentMsg, "Got it!");
                        #endif

                        Debug.LogError("LBUpdate ImportLandscape - " + landscapeGameObject.name + " gameobject is not a root gameobject in the scene.");
                    }
                    else
                    {
                        isOkToContinue = true;

                        #if UNITY_EDITOR
                        // Did user attach LB Landscape directly to a single terrain? Only check in editor
                        if (landscapeGameObject.GetComponent<Terrain>() != null && landscapeTerrains.Length == 1)
                        {
                            // WARNING: This doesn't copy across any landscape meta-data. It currently just creates a new landscape on the correct gameobject

                            string reParentMsg = "The " + landscapeGameObject.name + " gameobject should be a child of the landscape.\n\n" +
                                                 "If unsure seek help in manual, on the Unity forum or on our Discord channel.\n\n" +
                                                 "Do you want to automatically fix this?";
                            isOkToContinue = UnityEditor.EditorUtility.DisplayDialog("IMPORTANT!! Make terrain child of Landscape", reParentMsg, "YES, FIX NOW", "CANCEL");

                            if (isOkToContinue)
                            {
                                // Create a new parent gameobject for the terrain
                                GameObject newParentGameObject = new GameObject(landscapeGameObject.name + "(ImportedLandscape)");
                                newParentGameObject.transform.position = Vector3.zero;
                                landscapeGameObject.transform.SetParent(newParentGameObject.transform);
                                LBLandscape newLandscape = newParentGameObject.AddComponent<LBLandscape>();
                                DestroyImmediate(landscapeToImport);
                                landscapeGameObject = newParentGameObject;
                                landscapeTerrains = landscapeGameObject.GetComponentsInChildren<Terrain>();
                                landscapeToImport = newLandscape;
                                // We need to update again, because we destroyed the original. Do this one silently.
                                LBUpdate.LandscapeUpdate(landscapeToImport.LastUpdatedVersion, LB_Version, ref landscapeToImport, true);
                            }
                        }
                        #endif

                        #if UNITY_EDITOR
                        // Assume that if this in at runtime outside editor, person who wrote code knows what they are doing...
                        if (isOkToContinue && landscapeGameObject.transform.position != Vector3.zero)
                        {
                            string nonZeroMsg = "WARNING!! The parent gameobject of the terrain(s) is not at 0,0,0. If you created a new parent gameobject, this should be at 0,0,0." +
                                                "\n\nCancel if unsure and seek help in manual, on the Unity forum or on our Discord channel.";
                            isOkToContinue = UnityEditor.EditorUtility.DisplayDialog(landscapeGameObject.name + " position", nonZeroMsg, "Continue", "CANCEL");

                            // NOTE: If it is already parented to a terrain, resetting the parent to 0,0,0 will change the position of other objects on the terrain, which may break stuff.
                        }
                        #endif

                        // Loop through all the terrains that are children of the gameobject hosting the LBLandscape script
                        for (int t = 0; isOkToContinue && t < landscapeTerrains.Length; t++)
                        {
                            TerrainData tData = landscapeTerrains[t].terrainData;

                            // Each terrain must be a square
                            if (Mathf.RoundToInt(tData.size.x) != Mathf.RoundToInt(tData.size.z))
                            {
                                terrainsAllSquare = false;
                                Debug.LogError("LBUpdate ImportLandscape - " + landscapeTerrains[t].name + " is not square and " + importWarning);
                            }

                            if (t == 0)
                            {
                                // Record the size of the first terrain and compare everything else with that
                                terrainSize = tData.size;

                                // Check to see if the terrain data is being imported from a Project Folder (rather than the scene)
                                #if UNITY_EDITOR
                                string tDataFolder = LBEditorHelper.GetAssetFolder(tData);
                                #else
                                string tDataFolder = string.Empty;
                                #endif
                                if (string.IsNullOrEmpty(tDataFolder)) { landscapeToImport.useProjectForTerrainData = false; }
                                else
                                {
                                    landscapeToImport.useProjectForTerrainData = true;
                                    if (tDataFolder.StartsWith("Assets/")) { tDataFolder = tDataFolder.Substring(7); }
                                    // Check if in the root folder
                                    else if (tDataFolder.StartsWith("Assets")) { tDataFolder = string.Empty; }
                                }
                                landscapeToImport.terrainDataFolder = tDataFolder;
                            }
                            else
                            {
                                // Check that all heights are the same (do this independently from the x,z values so we know if height is a problem
                                if (Mathf.RoundToInt(tData.size.y) != Mathf.RoundToInt(terrainSize.y))
                                {
                                    terrainsAllSameSize = false;
                                    Debug.LogError("LBUpdate ImportLandscape - " + landscapeTerrains[t].name + " is not the same height as " + landscapeTerrains[0].name + " and " + importWarning);
                                }
                                // Compare the sizes
                                else if (tData.size != terrainSize)
                                {
                                    terrainsAllSameSize = false;
                                    Debug.LogError("LBUpdate ImportLandscape - " + landscapeTerrains[t].name + " is not the same size as " + landscapeTerrains[0].name + " and " + importWarning);
                                }
                            }
                        }

                        if (isOkToContinue && terrainsAllSquare && terrainsAllSameSize && landscapeTerrains.Length > 0)
                        {

                            terrainSize = landscapeTerrains[0].terrainData.size;

                            if (landscapeTerrains.Length == 1)
                            {
                                isLandscapeSquare = true;
                                newLandscapeSize = new Vector2(terrainSize.x, terrainSize.z);

                                // Set landscape parent gameobject to the offset (if any) of the terrain
                                // This is required so that stencils work corectly
                                landscapeGameObject.transform.position = landscapeTerrains[0].transform.position;
                                landscapeToImport.start = landscapeGameObject.transform.position;

                                // Move terrain and any other objects back to their original positions
                                foreach (Transform childTrfrm in landscapeGameObject.transform)
                                {
                                    childTrfrm.position -= landscapeToImport.start;
                                }
                            }
                            else
                            {
                                // Get the bounds of the landscape in worldspace
                                Rect WorldBounds = new Rect();
                                WorldBounds = LBLandscapeTerrain.GetLandscapeWorldBounds(landscapeTerrains);

                                // Are the sides divisible by the terrain width? (All terrains must be square)
                                float numberOfTerrainsWide = WorldBounds.width / terrainSize.x;
                                float numberOfTerrainsLong = WorldBounds.height / terrainSize.x;

                                int numberOfTerrainsWideInt = Mathf.RoundToInt(numberOfTerrainsWide);
                                int numberOfTerrainsLongInt = Mathf.RoundToInt(numberOfTerrainsLong);

                                if ((numberOfTerrainsWide - (float)numberOfTerrainsWideInt > 0.0001f) || (numberOfTerrainsLong - (float)numberOfTerrainsLongInt > 0.0001f))
                                {
                                    Debug.Log(landscapeGameObject.name + " is not a contiguous square or rectangular landscape. Put your terrains into separate landscape gameobjects");
                                }
                                // Do we have the expected number of terrains
                                else if ((numberOfTerrainsWideInt * numberOfTerrainsLongInt) != landscapeTerrains.Length)
                                {
                                    Debug.Log(landscapeGameObject.name + " is not a square or rectangular landscape. Put your terrains into separate landscape gameobjects");
                                }
                                else
                                {
                                    // Landscape appears to have correct dimensions (have not considered rotation)
                                    isLandscapeSquare = (numberOfTerrainsWide == numberOfTerrainsLongInt);
                                    isLandscapeRectangle = !isLandscapeSquare;
                                    newLandscapeSize = new Vector2(terrainSize.x * numberOfTerrainsWide, terrainSize.z * numberOfTerrainsLongInt);
                                    landscapeToImport.start.x = WorldBounds.min.x;
                                    landscapeToImport.start.y = landscapeTerrains[0].transform.position.y;
                                    landscapeToImport.start.z = WorldBounds.min.y;

                                    // Set landscape parent gameobject to the offset (if any) of the terrains
                                    // This is required so that stencils work corectly
                                    bool changeParentGO = landscapeToImport.start != landscapeGameObject.transform.position;

                                    if (changeParentGO)
                                    {
                                        landscapeGameObject.transform.position = landscapeToImport.start;

                                        // Move terrains and any other objects back to their original positions
                                        foreach (Transform childTrfrm in landscapeGameObject.transform)
                                        {
                                            childTrfrm.position -= landscapeToImport.start;
                                        }
                                    }
                                }
                            }

                            if (isLandscapeRectangle || isLandscapeSquare)
                            {
                                landscapeToImport.topographyLayersList = new List<LBLayer>();
                                ImportTerrainHeightmaps(landscapeToImport, true);
                                if (landscapeToImport.topographyLayersList.Count > 0)
                                {
                                    // Turn off the check if this is an imported landscape, because the terrain
                                    // height should already have been set.
                                    landscapeToImport.topographyLayersList[0].isCheckHeightRangeDiff = false;
                                    landscapeToImport.topographyLayersList[0].layerName = "Imported terrain(s)";
                                }
                            }

                            if (isLandscapeRectangle || isLandscapeSquare)
                            {

                                // Import Textures
                                if (landscapeToImport.terrainTexturesList != null)
                                {
                                    // Remove existing textures - although there shouldn't be any
                                    // unless a previous import failed
                                    landscapeToImport.terrainTexturesList.Clear();
                                    // Release any memory
                                    landscapeToImport.terrainTexturesList.TrimExcess();
                                }
                                else
                                {
                                    landscapeToImport.terrainTexturesList = new List<LBTerrainTexture>();
                                }

                                // Look for existing Textures in the terrains
                                ImportTextures(landscapeToImport, true);

                                // Import Trees
                                if (landscapeToImport.terrainTreesList != null)
                                {
                                    // Remove existing trees - although there shouldn't be any
                                    // unless a previous import failed
                                    landscapeToImport.terrainTreesList.Clear();
                                    // Release any memory
                                    landscapeToImport.terrainTreesList.TrimExcess();
                                }
                                else
                                {
                                    landscapeToImport.terrainTreesList = new List<LBTerrainTree>();
                                }

                                ImportTrees(landscapeToImport, true);

                                // Import Grass
                                if (landscapeToImport.terrainGrassList != null)
                                {
                                    // Remove existing grass - although there shouldn't be any
                                    // unless a previous import failed
                                    landscapeToImport.terrainGrassList.Clear();
                                    // Release any memory
                                    landscapeToImport.terrainGrassList.TrimExcess();
                                }
                                else
                                {
                                    landscapeToImport.terrainGrassList = new List<LBTerrainGrass>();
                                }

                                // Look for existing Grass in the terrains
                                ImportGrass(landscapeToImport, true);

                                isSuccessful = true;
                            }
                        }
                    }

                    if (isOkToContinue && isSuccessful)
                    {
                        // The last thing we need to do is update the landscape size
                        // This is how LB knows it has been successfully imported or created in LB
                        landscapeToImport.size = newLandscapeSize;
                        Debug.Log("LBUpdate ImportLandscape - " + landscapeGameObject.name + " imported successfully");
                    }
                    else
                    {
                        Debug.Log("LBUpdate ImportLandscape - " + landscapeGameObject.name + " Imported failed!");
                    }
                }
            }
            return isSuccessful;
        }
        #endregion

        #region Import Existing Terrain Heightmaps to a Topography Layer

        /// <summary>
        /// Import terrain data into a new Topography Layer
        /// </summary>
        /// <param name="showErrors"></param>
        public static void ImportTerrainHeightmaps(LBLandscape landscape, bool showErrors)
        {
            string methodName = "LBImport.ImportTerrainHeightmaps";

            if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscape is null. Please report"); } }
            else if (landscape.topographyLayersList == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - topographyLayers is not defined. Please report"); } }
            else if (landscape.landscapeTerrains == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscapeTerrains is not defined. Please report"); } }
            else if (landscape.landscapeTerrains.Length < 1) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - No terrains in landscape"); } }
            else if (landscape.topographyLayersList.Exists(lyr => lyr.type == LBLayer.LayerType.UnityTerrains)) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - Multiple Unity Terrain Layers are not supported in the same landscape"); } }
            else
            {
                float generationStartTime = Time.realtimeSinceStartup;

                List<LBTerrainData> lbTerrainDataList = new List<LBTerrainData>();

                for (int index = 0; index < landscape.landscapeTerrains.Length; index++)
                {
                    LBTerrainData lbTerrainData = LBTerrainData.ImportHeightmap(landscape, landscape.landscapeTerrains[index], showErrors);

                    // Don't continue if at least one terrain cannot be imported
                    if (lbTerrainData == null) { break; }
                    else
                    {
                        lbTerrainDataList.Add(lbTerrainData);
                    }
                }

                // Only create a new layer if all terrains were imported
                if (lbTerrainDataList.Count == landscape.landscapeTerrains.Length)
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
                        Debug.Log("Time taken to import terrain heightmaps: " + (Time.realtimeSinceStartup - generationStartTime).ToString("00.00") + " seconds.");
                    }
                }
            }
        }

        #endregion

        #region Import RAW heightmap data to a Topography Layer

        /// <summary>
        /// Import RAW heightmap files from disk into a topography layer.
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="rawFileList"></param>
        /// <param name="isMacRAWFormat"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static bool ImportRAWHeightmaps(LBLandscape landscape, List<string> rawFileList, bool isMacRAWFormat, bool showErrors)
        {
            bool isSuccessful = false;
            string methodName = "LBImport.ImportRAWHeightmaps";

            if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscape is null. Please report"); } }
            else if (landscape.topographyLayersList == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - topographyLayers is not defined. Please report"); } }
            else if (rawFileList == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - rawFileList is not defined. Please report."); } }
            else if (rawFileList.Count < 1) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - there are no RAW files to import"); } }
            else if (landscape.landscapeTerrains == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscapeTerrains is not defined. Please report"); } }
            else if (landscape.landscapeTerrains.Length < 1) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - No terrains in landscape"); } }
            else if (landscape.topographyLayersList.Exists(lyr => lyr.type == LBLayer.LayerType.UnityTerrains)) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - Multiple Unity Terrain Layers are not supported in the same landscape"); } }
            else
            {
                float generationStartTime = Time.realtimeSinceStartup;
                string rawFileName = string.Empty;
                int numTerrains = landscape.landscapeTerrains.Length;

                List<LBTerrainData> lbTerrainDataList = new List<LBTerrainData>();

                for (int index = 0; index < numTerrains; index++)
                {
                    rawFileName = string.Empty;
                    if (rawFileList.Count > index)
                    {
                        rawFileName = rawFileList[index];
                    }

                    //Debug.Log("INFO: " + methodName + " importing file: " + rawFileName);

                    LBTerrainData lbTerrainData = LBTerrainData.ImportHeightmapRAW(landscape, landscape.landscapeTerrains[index], rawFileName, isMacRAWFormat, showErrors);

                    // Don't continue if at least one terrain cannot be imported
                    if (lbTerrainData == null) { break; }
                    else
                    {
                        lbTerrainDataList.Add(lbTerrainData);
                    }
                }

                // Only create a new layer if all terrains were imported
                if (lbTerrainDataList.Count == numTerrains)
                {
                    LBLayer lbLayer = new LBLayer();
                    if (lbLayer != null)
                    {
                        lbLayer.type = LBLayer.LayerType.UnityTerrains;
                        lbLayer.lbTerrainDataList = lbTerrainDataList;
                        lbLayer.heightScale = 1.0f;
                        lbLayer.normaliseImage = false;
                        lbLayer.isCheckHeightRangeDiff = false;

                        // Insert at the top of the list
                        landscape.topographyLayersList.Insert(0, lbLayer);
                    }

                    if (landscape.showTiming)
                    {
                        Debug.Log("Time taken to import RAW heightmaps: " + (Time.realtimeSinceStartup - generationStartTime).ToString("00.00") + " seconds.");
                    }

                    isSuccessful = true;
                }
            }
            return isSuccessful;
        }

        /// <summary>
        /// Import a RAW heightmap file from disk into a topography layer and store in RAW format.
        /// Sometimes we may wish to override the Advanced tab setting of Show Timing.
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="lbLayer"></param>
        /// <param name="rawFileName"></param>
        /// <param name="isMacRAWFormat"></param>
        /// <param name="neverShowTiming"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static bool ImportRAWHeightmapToLayer(LBLandscape landscape, LBLayer lbLayer, string rawFileName, bool isMacRAWFormat, bool neverShowTiming, bool showErrors)
        {
            bool isSuccessful = false;
            string methodName = "LBImport.ImportRAWHeightmapToLayer";

            if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscape is null. Please report"); } }
            else if (landscape.topographyLayersList == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - topographyLayers is not defined. Please report"); } }
            else if (landscape.landscapeTerrains == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscapeTerrains is not defined. Please report"); } }
            else if (landscape.landscapeTerrains.Length < 1) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - No terrains in landscape"); } }
            else if (lbLayer == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - layer lbLayer is not defined"); } }
            else
            {
                float generationStartTime = Time.realtimeSinceStartup;

                // Import RAW file data from specified path
                LBRaw lbRaw = LBRaw.ImportHeightmapRAW(rawFileName, isMacRAWFormat, showErrors);
                if (lbRaw != null)
                {
                    // Only write to layer if the data is not null
                    lbLayer.modifierRAWFile = lbRaw;

                    if (!neverShowTiming && landscape.showTiming)
                    {
                        Debug.Log("Time taken to import RAW heightmap: " + (Time.realtimeSinceStartup - generationStartTime).ToString("00.00") + " seconds.");
                    }

                    isSuccessful = true;
                }
            }
            return isSuccessful;
        }

        /// <summary>
        /// Import a PNG heightmap file from disk into a topography layer and store in RAW format.
        /// Sometimes we may wish to override the Advanced tab setting of Show Timing
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="lbLayer"></param>
        /// <param name="pngFileName"></param>
        /// <param name="neverShowTiming"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static bool ImportPNGHeightmapToLayer(LBLandscape landscape, LBLayer lbLayer, string pngFileName, bool neverShowTiming, bool showErrors)
        {
            bool isSuccessful = false;
            string methodName = "LBImport.ImportPNGHeightmapToLayer";

            if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscape is null. Please report"); } }
            else if (landscape.topographyLayersList == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - topographyLayers is not defined. Please report"); } }
            else if (landscape.landscapeTerrains == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscapeTerrains is not defined. Please report"); } }
            else if (landscape.landscapeTerrains.Length < 1) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - No terrains in landscape"); } }
            else if (lbLayer == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - layer lbLayer is not defined"); } }
            else
            {
                float generationStartTime = Time.realtimeSinceStartup;

                // Import PNG file data from specified path and store it as RAW
                LBRaw lbRaw = LBRaw.ImportHeightmapPNG(pngFileName, showErrors);
                if (lbRaw != null)
                {
                    // Only write to layer if the data is not null
                    lbLayer.modifierRAWFile = lbRaw;

                    if (!neverShowTiming && landscape.showTiming)
                    {
                        Debug.Log("Time taken to import RAW heightmap: " + (Time.realtimeSinceStartup - generationStartTime).ToString("00.00") + " seconds.");
                    }

                    isSuccessful = true;
                }
            }
            return isSuccessful;
        }

        #endregion

        #region Import Textures

        /// <summary>
        /// Import texture types and splatmaps from Unity Terrains which have typically not been procedurally generated by LB.
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="showErrors"></param>
        public static void ImportTextures(LBLandscape landscape, bool showErrors)
        {
            string methodName = "LBImport.ImportTextures";

            if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscape is null. Please report"); } }
            else if (landscape.terrainTexturesList == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - terrainTexturesList is not defined. Please report"); } }
            else if (landscape.landscapeTerrains == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscapeTerrains is not defined. Please report"); } }
            else if (landscape.landscapeTerrains.Length < 1) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - No terrains in landscape"); } }
            else
            {
                float generationStartTime = Time.realtimeSinceStartup;
                int index = 0, tIdx = 0, insertAt = 0;

                List<LBTerrainTexture> importedTerrainTextureList = new List<LBTerrainTexture>();

                // Import the texture SplatPrototypes and create a new LBTerrainTexture instances with the texturingmode = Imported
                for (tIdx = 0; tIdx < landscape.landscapeTerrains.Length; tIdx++)
                {
                    List<LBTerrainTexture> terrainTextureList = ImportTextureTypes(landscape, landscape.landscapeTerrains[tIdx], showErrors);

                    if (terrainTextureList != null)
                    {
                        if (terrainTextureList.Count > 0)
                        {
                            //Debug.Log("INFO: " + methodName + " - add textures: " + terrainTextureList.Count);                           

                            // Keep a record of the Imported texture types
                            importedTerrainTextureList.AddRange(terrainTextureList);
                            // Add non-duplicate texture types to the landscape at the top of the texture type list
                            landscape.terrainTexturesList.InsertRange(insertAt, terrainTextureList);
                            insertAt += terrainTextureList.Count;
                        }
                    }
                }

                // Add a list of LBTerrainData to each Texture Type. This will ensure we have the correct number of LBTerrainData
                // instances for each Texture type.
                for (index = 0; index < importedTerrainTextureList.Count; index++)
                {
                    LBTerrainTexture lbTerrainTexture = importedTerrainTextureList[index];
                    if (lbTerrainTexture != null)
                    {
                        if (lbTerrainTexture.lbTerrainDataList == null) { lbTerrainTexture.lbTerrainDataList = new List<LBTerrainData>(); }
                        for (tIdx = 0; tIdx < landscape.landscapeTerrains.Length; tIdx++)
                        {
                            //Debug.Log("INFO: " + methodName +" adding LBTerrainData " + landscape.landscapeTerrains[tIdx].name + " for " + lbTerrainTexture.texture.name);

                            // The terrain data will not initially have any texture splatmap data
                            lbTerrainTexture.lbTerrainDataList.Add(new LBTerrainData(landscape.landscapeTerrains[tIdx].name));
                        }
                    }
                }

                if (landscape.showTiming)
                {
                    Debug.Log("Time taken to import all texture types: " + (Time.realtimeSinceStartup - generationStartTime).ToString("00.000") + " seconds.");
                }

                for (tIdx = 0; tIdx < landscape.landscapeTerrains.Length; tIdx++)
                {
                    if (!LBTerrainData.ImportTextures(landscape, landscape.landscapeTerrains[tIdx], tIdx, importedTerrainTextureList, showErrors))
                    {
                        // Don't continue if at least one terrain cannot be imported
                        break;
                    }
                }

                if (landscape.showTiming)
                {
                    Debug.Log("Total time taken to import textures: " + (Time.realtimeSinceStartup - generationStartTime).ToString("00.000") + " seconds.");
                }
            }
        }

        /// <summary>
        /// Import unique Unity terrain splatPrototypes into a LBLandscape
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="terrain"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        private static List<LBTerrainTexture> ImportTextureTypes(LBLandscape landscape, Terrain terrain, bool showErrors)
        {
            List<LBTerrainTexture> terrainTextureList = null;

            string methodName = "LBImport.ImportTextureTypes";

            if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscape is null"); } }
            else if (terrain == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - terrain is null in " + landscape.name); } }
            else if (terrain.terrainData == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - terrain.terrainData is null for " + landscape.name + "." + terrain.name); } }
            else
            {
                if (landscape.terrainTexturesList == null) { landscape.terrainTexturesList = new List<LBTerrainTexture>(); }

                TerrainData tData = terrain.terrainData;

                if (landscape.terrainTexturesList != null)
                {
                    // Get the texture types for this terrain
                    #if UNITY_2018_3_OR_NEWER
                    terrainTextureList = LBTerrainTexture.ToLBTerrainTextureList(tData.terrainLayers);
                    #else
                    terrainTextureList = LBTerrainTexture.ToLBTerrainTextureList(tData.splatPrototypes);
                    #endif

                    if (terrainTextureList != null)
                    {
                        //Debug.Log(methodName + " found types " + terrainTextureList.Count);

                        if (terrainTextureList.Count > 0)
                        {
                            // Look for textures that already exist in other terrains already processed
                            // Loop backwards so that we can remove matching textures
                            for (int d = terrainTextureList.Count - 1; d >= 0; d--)
                            {
                                LBTerrainTexture lbTerrainTexture = terrainTextureList[d];

                                if (lbTerrainTexture != null)
                                {
                                    LBTerrainTexture existingTexture = landscape.terrainTexturesList.Find(tx => tx.texture == lbTerrainTexture.texture &&
                                                                                                                tx.normalMap == lbTerrainTexture.normalMap &&
                                                                                                                tx.smoothness == lbTerrainTexture.smoothness &&
                                                                                                                tx.metallic == lbTerrainTexture.metallic &&
                                                                                                                tx.tileSize == lbTerrainTexture.tileSize &&
                                                                                                                tx.texturingMode == LBTerrainTexture.TexturingMode.Imported);
                                    // Did we find a match?
                                    if (existingTexture != null)
                                    {
                                        // No need to add this as it was in another terrain
                                        terrainTextureList.Remove(lbTerrainTexture);
                                    }
                                }
                            }

                            // Set these new texture types to Imported.
                            for (int t = 0; t < terrainTextureList.Count; t++)
                            {
                                terrainTextureList[t].texturingMode = LBTerrainTexture.TexturingMode.Imported;
                                terrainTextureList[t].useNoise = false;
                            }
                        }
                    }
                }
            }
            return terrainTextureList;
        }

        #endregion

        #region Import Trees

        /// <summary>
        /// Import trees from Unity Terrains which have typically not been procedurally generated by LB.
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="showErrors"></param>
        public static void ImportTrees(LBLandscape landscape, bool showErrors)
        {
            string methodName = "LBImport.ImportTrees";

            if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscape is null. Please report"); } }
            else if (landscape.terrainTreesList == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - terrainTreesList is not defined. Please report"); } }
            else if (landscape.landscapeTerrains == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscapeTerrains is not defined. Please report"); } }
            else if (landscape.landscapeTerrains.Length < 1) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - No terrains in landscape"); } }
            else
            {
                float generationStartTime = Time.realtimeSinceStartup;
                int index = 0, tIdx = 0, insertAt = 0;
                int numTerrains = landscape.landscapeTerrains.Length;

                List<LBTerrainTree> importedTerrainTreeList = new List<LBTerrainTree>();

                // Import the tree prototypes and create new LBTreeType instances with the placementmode = Imported
                for (tIdx = 0; tIdx < numTerrains; tIdx++)
                {
                    List<LBTerrainTree> terrainTreeList = ImportTreeTypes(landscape, landscape.landscapeTerrains[tIdx], showErrors);

                    if (terrainTreeList != null)
                    {
                        if (terrainTreeList.Count > 0)
                        {
                            // Keep a record of the Imported tree prototypes
                            importedTerrainTreeList.AddRange(terrainTreeList);
                            // Add non-duplicate tree types to the landscape at the top of the tree type list
                            landscape.terrainTreesList.InsertRange(insertAt, terrainTreeList);
                            insertAt += terrainTreeList.Count;
                        }
                    }
                }

                int numImportedTerrainTreesTypes = importedTerrainTreeList == null ? 0 : importedTerrainTreeList.Count;

                // Add a list of LBTerrainData to each Tree Type. This will ensure we have the correct number of LBTerrainData
                // instances for each tree type.
                for (index = 0; index < numImportedTerrainTreesTypes; index++)
                {
                    LBTerrainTree lbTerrainTree = importedTerrainTreeList[index];
                    if (lbTerrainTree != null)
                    {
                        if (lbTerrainTree.lbTerrainDataList == null) { lbTerrainTree.lbTerrainDataList = new List<LBTerrainData>(); }
                        for (tIdx = 0; tIdx < numTerrains; tIdx++)
                        {
                            //Debug.Log("[DEBUG] INFO: " + methodName + " adding LBTerrainData " + landscape.landscapeTerrains[tIdx].name + " for " + lbTerrainTree.prefab.name);

                            // The terrain data will not initially have any tree instances
                            lbTerrainTree.lbTerrainDataList.Add(new LBTerrainData(landscape.landscapeTerrains[tIdx].name));
                        }
                    }
                }

                if (landscape.showTiming)
                {
                    Debug.Log("Time taken to import all tree types: " + (Time.realtimeSinceStartup - generationStartTime).ToString("00.000") + " seconds.");
                }

                if (numImportedTerrainTreesTypes > 0)
                {
                    for (tIdx = 0; tIdx < numTerrains; tIdx++)
                    {
                        if (!LBTerrainData.ImportTrees(landscape, landscape.landscapeTerrains[tIdx], tIdx, importedTerrainTreeList, showErrors))
                        {
                            // Don't continue if at least one terrain cannot be imported
                            break;
                        }
                    }
                }
                else
                {
                    Debug.Log("INFO: " + methodName + " no unique terrain tree types to import.");
                }

                if (landscape.showTiming)
                {
                    Debug.Log("Total time taken to import trees: " + (Time.realtimeSinceStartup - generationStartTime).ToString("00.000") + " seconds.");
                }
            }
        }

        /// <summary>
        /// Import unique Unity Terrain tree prototypes 
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="terrain"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        private static List<LBTerrainTree> ImportTreeTypes(LBLandscape landscape, Terrain terrain, bool showErrors)
        {
            List<LBTerrainTree> terrainTreeList = null;

            string methodName = "LBImport.ImportTreeTypes";

            if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscape is null"); } }
            else if (terrain == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - terrain is null in " + landscape.name); } }
            else if (terrain.terrainData == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - terrain.terrainData is null for " + landscape.name + "." + terrain.name); } }
            else
            {
                if (landscape.terrainTreesList == null) { landscape.terrainTreesList = new List<LBTerrainTree>(); }

                TerrainData tData = terrain.terrainData;

                if (landscape.terrainTreesList != null)
                {
                    // Get the tree types for this terrain
                    terrainTreeList = LBTerrainTree.ToLBTerrainTreeList(tData.treePrototypes);
                    if (terrainTreeList != null)
                    {
                        //Debug.Log(methodName + " found prototypes " + terrainTreeList.Count);

                        if (terrainTreeList.Count > 0)
                        {
                            // Look for trees that already exist in this landscape or in other terrains already processed
                            // Only non-disabled trees with a treePlacingMode of Imported are consider.
                            // Loop backwards so that we can remove matching trees
                            for (int d = terrainTreeList.Count - 1; d >= 0; d--)
                            {
                                LBTerrainTree lbTerrainTree = terrainTreeList[d];

                                if (lbTerrainTree != null)
                                {
                                    LBTerrainTree exitingTree = landscape.terrainTreesList.Find(tr => tr.prefab == lbTerrainTree.prefab &&
                                                                                                      tr.bendFactor == lbTerrainTree.bendFactor &&
                                                                                                      !tr.isDisabled &&
                                                                                                      tr.treePlacingMode == LBTerrainTree.TreePlacingMode.Imported);
                                    // Did we find a match?
                                    if (exitingTree != null)
                                    {
                                        // No need to add this as it was in another terrain
                                        terrainTreeList.Remove(lbTerrainTree);
                                    }
                                }
                            }
                            // Set these new trees to Imported.
                            for (int t = 0; t < terrainTreeList.Count; t++)
                            {
                                terrainTreeList[t].treePlacingMode = LBTerrainTree.TreePlacingMode.Imported;
                                terrainTreeList[t].minScale = 1;
                                terrainTreeList[t].maxScale = 1;
                                terrainTreeList[t].minProximity = 0f;
                                terrainTreeList[t].useNoise = false;
                            }
                        }
                    }
                }
            }
            return terrainTreeList;
        }


        #endregion

        #region Import Grass

        /// <summary>
        /// Import grass from Unity Terrains which have typically not been procedurally generated by LB.
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="showErrors"></param>
        public static void ImportGrass(LBLandscape landscape, bool showErrors)
        {
            if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR: LBImport.ImportGrass - landscape is null. Please report"); } }
            else if (landscape.terrainGrassList == null) { if (showErrors) { Debug.LogWarning("ERROR: LBImport.ImportGrass - terrainGrassList is not defined. Please report"); } }
            else if (landscape.landscapeTerrains == null) { if (showErrors) { Debug.LogWarning("ERROR: LBImport.ImportGrass - landscapeTerrains is not defined. Please report"); } }
            else if (landscape.landscapeTerrains.Length < 1) { if (showErrors) { Debug.LogWarning("ERROR: LBImport.ImportGrass - No terrains in landscape"); } }
            else
            {
                float generationStartTime = Time.realtimeSinceStartup;
                int index = 0, tIdx = 0, insertAt = 0;

                List<LBTerrainGrass> importedTerrainGrassList = new List<LBTerrainGrass>();

                // Import the grass prototypes and create new LBGrassType instances with the placementmode = Imported
                for (tIdx = 0; tIdx < landscape.landscapeTerrains.Length; tIdx++)
                {
                    List<LBTerrainGrass> terrainGrassList = ImportGrassTypes(landscape, landscape.landscapeTerrains[tIdx], showErrors);

                    if (terrainGrassList != null)
                    {
                        if (terrainGrassList.Count > 0)
                        {
                            // Keep a record of the Imported grass types
                            importedTerrainGrassList.AddRange(terrainGrassList);
                            // Add non-duplicate grass types to the landscape at the top of the grass type list
                            landscape.terrainGrassList.InsertRange(insertAt, terrainGrassList);
                            insertAt += terrainGrassList.Count;
                        }
                    }
                }

                int numImportedTerrainGrassTypes = importedTerrainGrassList == null ? 0 : importedTerrainGrassList.Count;

                // Add a list of LBTerrainData to each Grass Type. This will ensure we have the correct number of LBTerrainData
                // instances for each grass type.
                for (index = 0; index < numImportedTerrainGrassTypes; index++)
                {
                    LBTerrainGrass lbTerrainGrass = importedTerrainGrassList[index];
                    if (lbTerrainGrass != null)
                    {
                        if (lbTerrainGrass.lbTerrainDataList == null) { lbTerrainGrass.lbTerrainDataList = new List<LBTerrainData>(); }
                        for (tIdx = 0; tIdx < landscape.landscapeTerrains.Length; tIdx++)
                        {
                            //Debug.Log("INFO LBImport.ImportGrass adding LBTerrainData " + landscape.landscapeTerrains[tIdx].name + " for " + lbTerrainGrass.texture.name);

                            // The terrain data will not initially have any grass data
                            lbTerrainGrass.lbTerrainDataList.Add(new LBTerrainData(landscape.landscapeTerrains[tIdx].name));
                        }
                    }
                }

                if (landscape.showTiming)
                {
                    Debug.Log("Time taken to import all grass types: " + (Time.realtimeSinceStartup - generationStartTime).ToString("00.000") + " seconds.");
                }

                if (numImportedTerrainGrassTypes > 0)
                {
                    // Populate the LBTerrainData for each terrain.
                    for (tIdx = 0; tIdx < landscape.landscapeTerrains.Length; tIdx++)
                    {
                        if (!LBTerrainData.ImportGrass(landscape, landscape.landscapeTerrains[tIdx], tIdx, importedTerrainGrassList, showErrors))
                        {
                            // Don't continue if at least one terrain cannot be imported
                            break;
                        }
                    }
                }
                else
                {
                    Debug.Log("INFO: LBImport.ImportGrass no terrain grass types to import.");
                }

                if (landscape.showTiming)
                {
                    Debug.Log("Total time taken to import grass: " + (Time.realtimeSinceStartup - generationStartTime).ToString("00.000") + " seconds.");
                }
            }
        }


        /// <summary>
        /// Import unique Unity Terrain grass types
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="terrain"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        private static List<LBTerrainGrass> ImportGrassTypes(LBLandscape landscape, Terrain terrain, bool showErrors)
        {
            List<LBTerrainGrass> terrainGrassList = null;

            string methodName = "LBImport.ImportGrassTypes";

            if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscape is null"); } }
            else if (terrain == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - terrain is null in " + landscape.name); } }
            else if (terrain.terrainData == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - terrain.terrainData is null for " + landscape.name + "." + terrain.name); } }
            else
            {
                if (landscape.terrainGrassList == null) { landscape.terrainGrassList = new List<LBTerrainGrass>(); }

                TerrainData tData = terrain.terrainData;

                if (landscape.terrainGrassList != null)
                {
                    // Get the grass types for this terrain


                    terrainGrassList = LBTerrainGrass.ToLBTerrainGrassList(tData.detailPrototypes);
                    if (terrainGrassList != null)
                    {
                        //Debug.Log(methodName + " found types " + terrainGrassList.Count);

                        if (terrainGrassList.Count > 0)
                        {
                            // Look for grass that already exist in other terrains already processed
                            // Loop backwards so that we can remove matching grass types
                            for (int d = terrainGrassList.Count - 1; d >= 0; d--)
                            {
                                LBTerrainGrass lbTerrainGrass = terrainGrassList[d];

                                if (lbTerrainGrass != null)
                                {
                                    LBTerrainGrass exitingGrass = landscape.terrainGrassList.Find(gr => gr.texture == lbTerrainGrass.texture &&
                                                                                                        gr.minHeight == lbTerrainGrass.minHeight &&
                                                                                                        gr.maxHeight == lbTerrainGrass.maxHeight &&
                                                                                                        gr.minWidth == lbTerrainGrass.minWidth &&
                                                                                                        gr.maxWidth == lbTerrainGrass.maxWidth &&
                                                                                                        gr.dryColour == lbTerrainGrass.dryColour &&
                                                                                                        gr.healthyColour == lbTerrainGrass.healthyColour &&
                                                                                                        gr.noiseSpread == lbTerrainGrass.noiseSpread &&
                                                                                                        gr.detailRenderMode == lbTerrainGrass.detailRenderMode &&
                                                                                                        gr.grassPlacingMode == LBTerrainGrass.GrassPlacingMode.Imported);
                                    // Did we find a match?
                                    if (exitingGrass != null)
                                    {
                                        // No need to add this as it was in another terrain
                                        terrainGrassList.Remove(lbTerrainGrass);
                                    }
                                }
                            }

                            // Set these new grass to Imported.
                            for (int t = 0; t < terrainGrassList.Count; t++)
                            {
                                terrainGrassList[t].grassPlacingMode = LBTerrainGrass.GrassPlacingMode.Imported;
                                terrainGrassList[t].useNoise = false;
                                // Min/Max Density will be the defaults (0-5).
                            }
                        }
                    }
                }
            }
            return terrainGrassList;
        }

        #endregion

        #region Public Static Methods



        #endregion

    }
}