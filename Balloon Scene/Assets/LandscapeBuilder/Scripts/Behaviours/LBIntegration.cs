using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
#if VEGETATION_STUDIO_PRO
using AwesomeTechnologies.VegetationStudio;
using AwesomeTechnologies.VegetationSystem;
using AwesomeTechnologies.Vegetation.PersistentStorage;
#endif

namespace LandscapeBuilder
{
    public class LBIntegration : MonoBehaviour
    {
        #region EasyRoads3D Integration

        /// <summary>
        /// Check to see if EasyRoads3D version 3.x is installed in the current project
        /// </summary>
        /// <returns></returns>
        public static bool isEasyRoads3DInstalled(bool showErrors = true)
        {
            bool isInstalled = false;

            System.Type erRoadNetworkType = null;

            try
            {
                erRoadNetworkType = System.Type.GetType("EasyRoads3Dv3.ERRoadNetwork, EasyRoads3Dv3, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", true, true);
                if (erRoadNetworkType != null) { isInstalled = true; }
            }
            catch
            {
                if (showErrors) { Debug.LogWarning("LBIntegration.isEasyRoads3DInstalled: it appears that EasyRoads3D version 3.x is not installed in this project."); }
            }

            return isInstalled;
        }

        #if LB_EDITOR_ER3

        /// <summary>
        /// Populate a supplied (and not null) list of roads from an EasyRoads network
        /// </summary>
        /// <param name="roadList"></param>
        /// <param name="showErrors"></param>
        public static void GetERRoadList(List<LBRoad> roadList, bool showErrors)
        {
            string methodName = "LBIntegration.GetERRoadList";

            if (roadList == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " the input roadlist cannot be null"); } }
            else
            {
                EasyRoads3Dv3.ERRoadNetwork eRRoadNetwork = new EasyRoads3Dv3.ERRoadNetwork();

                if (eRRoadNetwork != null)
                {
                    EasyRoads3Dv3.ERRoad[] erRoads = eRRoadNetwork.GetRoads();
                    int numRoads = erRoads == null ? 0 : erRoads.Length;
                    LBRoad lbRoad = null;
                    EasyRoads3Dv3.ERRoad erRoad = null;
                    EasyRoads3Dv3.ERRoadType erRoadType = null;

                    // Loop through the ERRoad[] array
                    for (int r = 0; r < numRoads; r++)
                    {
                        lbRoad = new LBRoad();
                        erRoad = erRoads[r];

                        if (lbRoad != null && erRoad != null)
                        {
                            // Populate the LBRoad class instance with data from EasyRoads
                            lbRoad.roadName = erRoad.GetName();
                            lbRoad.roadLength = erRoad.GetLength();
                            lbRoad.roadWidth = erRoad.GetWidth();
                            erRoadType = erRoad.GetRoadType();
                            lbRoad.roadTypeDesc = erRoadType == null ? "unknown" : erRoadType.roadTypeName;

                            //Debug.Log("[DEBUG] " + lbRoad.roadName + " [" + lbRoad.roadTypeDesc + "]");

                            // Add the road to the list that will be returned
                            roadList.Add(lbRoad);
                        }
                    }
                }
            }
        }


        #endif

        /// <summary>
        /// Get a list of roads from an EasyRoads network using reflection
        /// </summary>
        /// <returns></returns>
        public static List<LBRoad> GetERRoadList()
        {
            List<LBRoad> roadList = new List<LBRoad>();
            LBRoad lbRoad = null;

            System.Type erRoadNetworkType = null;
            System.Type erRoadType = null;

            try
            {
                erRoadNetworkType = System.Type.GetType("EasyRoads3Dv3.ERRoadNetwork, EasyRoads3Dv3, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", true, true);
                erRoadType = System.Type.GetType("EasyRoads3Dv3.ERRoad, EasyRoads3Dv3, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", true, true);

                if (erRoadNetworkType == null)
                {
                    Debug.LogError("LBIntegration.GetERRoadList: could not reference EasyRoads3Dv3.ERRoadNetwork class");
                }
                else if (erRoadType == null)
                {
                    Debug.LogError("LBIntegration.GetERRoadList: could not reference EasyRoads3Dv3.ERRoad class");
                }
                else
                {
                    // Create instance of the API Type to be used so that we can call methods
                    // We can't use FindObjectOfType because ERRoadNetwork is not derived from UnityEngine.Object
                    // We "could" do the following but that's not part of the ER3Dv3 API
                    // var erModularRoads = EasyRoads3D_network.GetComponentsInChildren(erModularRoadType);
                    var erRoadNetworkObj = Activator.CreateInstance(erRoadNetworkType);

                    if (erRoadNetworkObj == null) { Debug.LogError("LBIntegration.GetERRoadList: could not access EasyRoads Road Network API"); }
                    else
                    {
                        // Get an array of the roads
                        var erRoads = erRoadNetworkType.InvokeMember("GetRoads", System.Reflection.BindingFlags.InvokeMethod, null, erRoadNetworkObj, new object[] { });
                        if (erRoads != null)
                        {
                            // Loop through the ERRoad[] array
                            for (int r = 0; r < ((object[])erRoads).Length; r++)
                            {
                                // Get a reference to the ERRoad class instance
                                var erRoad = ((object[])erRoads)[r];
                                if (erRoad != null)
                                {
                                    lbRoad = new LBRoad();
                                    if (lbRoad != null)
                                    {
                                        // Populate the LBRoad class instance with data from EasyRoads
                                        lbRoad.roadName = (string)erRoadType.InvokeMember("GetName", System.Reflection.BindingFlags.InvokeMethod, null, erRoad, new object[] { });
                                        lbRoad.roadLength = (float)erRoadType.InvokeMember("GetLength", System.Reflection.BindingFlags.InvokeMethod, null, erRoad, new object[] { });
                                        lbRoad.roadWidth = (float)erRoadType.InvokeMember("GetWidth", System.Reflection.BindingFlags.InvokeMethod, null, erRoad, new object[] { });

                                        // Add the road to the list that will be returned
                                        roadList.Add(lbRoad);

                                        //Debug.Log("Road: " + lbRoad.roadName + " length: " + lbRoad.roadLength.ToString() + " width: " + lbRoad.roadWidth.ToString());
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                Debug.LogWarning("LBIntegration.GetERRoadList: it appears that EasyRoads3D version 3.x is not installed in this project.");
            }

            return roadList;
        }

        /// <summary>
        /// Retrieve the spline points from an EasyRoads3D v3 road given a splinetype.
        /// A filterSize > 0 will only return the nth spline point. This can be useful for things like the Camera Animator
        /// which doesn't require points so closely spaced.
        /// </summary>
        /// <param name="lbRoad"></param>
        /// <param name="splineType"></param>
        /// <param name="filterSize"></param>
        /// <returns></returns>
        public static Vector3[] GetERRoadSplinePoints(LBRoad lbRoad, LBRoad.SplineType splineType, int filterSize)
        {
            Vector3[] splinePoints = null;
            List<Vector3> splinePointFilteredList = new List<Vector3>();
            string methodName = string.Empty;

            System.Type erRoadNetworkType = null;
            System.Type erRoadType = null;

            try
            {
                erRoadNetworkType = System.Type.GetType("EasyRoads3Dv3.ERRoadNetwork, EasyRoads3Dv3, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", true, true);
                erRoadType = System.Type.GetType("EasyRoads3Dv3.ERRoad, EasyRoads3Dv3, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", true, true);

                if (erRoadNetworkType == null)
                {
                    Debug.LogWarning("LBIntegration.GetERRoadSplinePoints: could not reference EasyRoads3Dv3.ERRoadNetwork class");
                }
                else if (erRoadType == null)
                {
                    Debug.LogWarning("LBIntegration.GetERRoadSplinePoints: could not reference EasyRoads3Dv3.ERRoad class");
                }
                else
                {
                    // Create instance of the API Type to be used so that we can call methods
                    var erRoadNetworkObj = Activator.CreateInstance(erRoadNetworkType);

                    if (erRoadNetworkObj == null) { Debug.LogError("LBIntegration.GetERRoadSplinePoints: could not access EasyRoads Road Network API"); }
                    else
                    {
                        // Find the first road with this roadName
                        var erRoad = erRoadNetworkType.InvokeMember("GetRoadByName", System.Reflection.BindingFlags.InvokeMethod, null, erRoadNetworkObj, new object[] { lbRoad.roadName });
                        if (erRoad != null)
                        {
                            // Which method will be called?
                            if (splineType == LBRoad.SplineType.CentreSpline) { methodName = "GetSplinePointsCenter"; }
                            else if (splineType == LBRoad.SplineType.LeftSpline) { methodName = "GetSplinePointsLeftSide"; }
                            else if (splineType == LBRoad.SplineType.RightSpline) { methodName = "GetSplinePointsRightSide"; }
                            else if (splineType == LBRoad.SplineType.MarkerSpline) { methodName = "GetMarkerPositions"; }
                            else { methodName = "GetSplinePointsCenter"; }

                            // Get the spline points by passing in the correct method (function) name
                            splinePoints = (Vector3[])erRoadType.InvokeMember(methodName, System.Reflection.BindingFlags.InvokeMethod, null, erRoad, new object[] { });

                            if (filterSize > 0 && splinePoints != null)
                            {
                                if (splinePoints.Length > 2)
                                {
                                    // Always add the first point
                                    splinePointFilteredList.Add(splinePoints[0]);

                                    // Examine the second until second-last points, incrementing by filterSize
                                    for (int i = 1; i < splinePoints.Length - 1; i += filterSize)
                                    {
                                        // Only add ever filterSize'd item
                                        splinePointFilteredList.Add(splinePoints[i]);
                                    }

                                    // Always add the last point
                                    splinePointFilteredList.Add(splinePoints[splinePoints.Length - 1]);

                                    splinePoints = splinePointFilteredList.ToArray();
                                }
                            }
                        }
                        else
                        {
                            Debug.LogWarning("LBIntegration.GetERRoadSplinePoints - missing road: " + lbRoad.roadName);
                        }
                    }
                }
            }
            catch
            {
                Debug.LogError("LBIntegration.GetERRoadSplinePoints: sorry, something went wrong with EasyRoads3Dv3 integration for road: " + lbRoad.roadName);
            }

            return splinePoints;
        }

        #endregion

        #region Vegetation Studio

        /// <summary>
        /// Determine if Vegetation Studio is installed in the project
        /// </summary>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static bool IsVegetationStudioInstalled(bool showErrors)
        {
            #if VEGETATION_STUDIO
            return true;
            #else
            return false;
            #endif
        }

        /// <summary>
        /// Vegetation Studio VegetationSourceID for Landscape Builder
        /// </summary>
        public static byte GetVegetationStudioSourceID { get { return 17; } }

        /// <summary>
        /// Add Vegetation Studio scripts to the landscape.
        /// USAGE: landscape.SetLandscapeTerrains(true);
        /// if (LBIntegration.VegetationStudioEnable(landscape, landscape.useVegetationSystem, true)) { .. }
        /// Currently this is EDITOR ONLY
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="isEnabled"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static bool VegetationStudioEnable(LBLandscape landscape, bool isEnabled, bool showErrors)
        {
            bool isSuccessful = false;
            string methodName = "LBIntegration.VegetationStudioEnable";

            if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscape cannot be null"); } }
            else if (!IsVegetationStudioInstalled(true)) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - Vegetation Studio 1.1.0.3 or newer does not seem to be installed in the project."); } }
            else if (landscape.landscapeTerrains == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscape.landscapeTerrains cannot be null"); } }
            {
                #if VEGETATION_STUDIO
                
                int numTerrains = (landscape.landscapeTerrains == null ? 0 : landscape.landscapeTerrains.Length);

                // Users can specify 0, 1 or multiple cameras to use with Vegetation Studio. If none are specified, auto select
                // camera will be used and 1 vegetation system per terrain will be created.
                if (landscape.vegetationStudioCameraList == null) { landscape.vegetationStudioCameraList = new List<Camera>(); }
                int numCameras = (landscape.vegetationStudioCameraList == null ? 0 : landscape.vegetationStudioCameraList.Count);
                int minNumCameras = (numCameras == 0 ? 1 : numCameras);

                // Re-use a list so we don't incur GC overhead
                List<AwesomeTechnologies.VegetationSystem> vegetationSystemList = new List<AwesomeTechnologies.VegetationSystem>(minNumCameras);
                Terrain terrain;

                if (isEnabled)
                {
                    // If Vegetation Studio doesn't exist in the scene, add it.
                    AwesomeTechnologies.VegetationStudio.VegetationStudioManager vegetationStudioManager = FindObjectOfType<AwesomeTechnologies.VegetationStudio.VegetationStudioManager>();
                    if (vegetationStudioManager == null)
                    {
                        GameObject go = new GameObject { name = "VegetationStudio" };
                        go.AddComponent<AwesomeTechnologies.VegetationStudio.VegetationStudioManager>();
                    }

                    #if UNITY_EDITOR
                    bool vegetationSystemAdded = false;
                    #endif

                    AwesomeTechnologies.VegetationSystem vegSystem = null;
                    AwesomeTechnologies.VegetationPackage vegetationPackage = null;

                    // TODO Check to see if Vegetation Package and/or Persistent Storage packages already exist in the project.


                    // TODO Prompt user to overwrite or cancel


                    #region Add Veg Studio scripts to the terrains
                    for (int tIdx = 0; tIdx < numTerrains; tIdx++)
                    {
                        terrain = landscape.landscapeTerrains[tIdx];

                        // Does the VegetationSystem script already exist?
                        terrain.GetComponentsInChildren(vegetationSystemList);
                        int numVS = (vegetationSystemList == null ? 0 : vegetationSystemList.Count);

                        if (vegetationSystemList != null && numVS < minNumCameras)
                        {
                            // Create the number required, skipping over any existing Vegetation Systems
                            for (int vsIdx = numVS; vsIdx < minNumCameras; vsIdx++)
                            {
                                GameObject vegetationSystemGameObject = new GameObject { name = "VegetationSystem" };
                                vegSystem = vegetationSystemGameObject.AddComponent<AwesomeTechnologies.VegetationSystem>();
                                vegetationSystemGameObject.AddComponent<AwesomeTechnologies.TerrainSystem>();
                                vegetationSystemGameObject.AddComponent<AwesomeTechnologies.Billboards.BillboardSystem>();
                                vegetationSystemGameObject.AddComponent<AwesomeTechnologies.Colliders.ColliderSystem>();
                                vegetationSystemGameObject.AddComponent<AwesomeTechnologies.Vegetation.PersistentStorage.PersistentVegetationStorage>();

                                vegSystem.transform.SetParent(landscape.landscapeTerrains[tIdx].transform);
                                vegSystem.AutoselectTerrain = false;
                                vegSystem.currentTerrain = landscape.landscapeTerrains[tIdx];

                                #if UNITY_EDITOR
                                vegetationSystemAdded = true;
                                #endif
                            }
                        }
                        vegetationSystemList.Clear();
                    }
                    #endregion

                    #region Add Vegetation Package and import textures, trees and grass

                    #if UNITY_EDITOR

                    // Check to see if a VegetationPackage has already been added
                    vegetationPackage = LBEditorHelper.GetAsset<AwesomeTechnologies.VegetationPackage>(landscape.name + " ");

                    //if (vegetationPackage != null) { Debug.Log("INFO: Found existing VegetationPackage: " + vegetationPackage.PackageName); }

                    // Create a new vegetation system package in the project
                    if (vegetationSystemAdded)
                    { 
                        // Do we need to create a new vegetation package?
                        if (vegetationPackage == null)
                        {
                            vegetationPackage = LBEditorHelper.CreateAsset<AwesomeTechnologies.VegetationPackage>(landscape.name + " ", false);
                        }

                        if (vegetationPackage == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not create VegetationPackage in project folder"); } }
                        else
                        {                            
                            vegetationPackage.PackageName = landscape.name;                      
                        
                            VegetationStudioImportTextures(landscape, showErrors);
                            VegetationStudioImportTrees(landscape, showErrors);
                            VegetationStudioImportGrass(landscape, showErrors);
                        }
                    }
                    #endif
                    #endregion

                    #region Add Vegetation Package to terrain scripts and wake them up

                    for (int tIdx = 0; tIdx < numTerrains; tIdx++)
                    {
                        terrain = landscape.landscapeTerrains[tIdx];
                        terrain.GetComponentsInChildren(vegetationSystemList);
                        int numVS = (vegetationSystemList == null ? 0 : vegetationSystemList.Count);
                        for (int vsIdx = 0; vsIdx < numVS; vsIdx++)
                        {
                            vegSystem = vegetationSystemList[vsIdx];

                            if (vegSystem != null)
                            {
                                if (vegetationPackage != null)
                                {
                                    int numVegetationPackages = (vegSystem.VegetationPackageList == null ? 0 : vegSystem.VegetationPackageList.Count);

                                    if (numVegetationPackages == 0)
                                    {
                                        vegSystem.VegetationPackageList.Add(vegetationPackage);
                                    }

                                    // THIS CAUSES ISSUES
                                    //vegSystem.SetVegetationPackage(0, true);
                                }

                                // Check to see if we need to assign a specific camera
                                if (vsIdx < numCameras)
                                {
                                    vegSystem.AutoselectCamera = false;
                                    vegSystem.SetCamera(landscape.vegetationStudioCameraList[vsIdx]);
                                }
                                else { vegSystem.AutoselectCamera = true; }

                                // Wake up the Vegetation System on this terrain
                                vegSystem.SetSleepMode(false);
                            }
                        }
                    }

                    #endregion

                    isSuccessful = true;
                }
                else
                {
                    #region Remove any Veg Studio scripts from the terrains

                    bool vsRemoved = false;

                    for (int tIdx = 0; tIdx < numTerrains; tIdx++)
                    {
                        terrain = landscape.landscapeTerrains[tIdx];

                        // Does the VegetationSystem script exist?
                        terrain.GetComponentsInChildren(vegetationSystemList);
                        int numVS = (vegetationSystemList == null ? 0 : vegetationSystemList.Count);
                        for (int vsIdx = numVS - 1; vsIdx >= 0; vsIdx--)
                        {
                            DestroyImmediate(vegetationSystemList[vsIdx].gameObject);
                            vsRemoved = true;
                        }
                        if (vegetationSystemList != null) { vegetationSystemList.Clear(); }

                        // Restore default Pixel Error, Tree and Detail Distances
                        // only where VSys was previously applied.
                        if (vsRemoved)
                        {
                            terrain.heightmapPixelError = 1f;
                            terrain.detailObjectDistance = 200f;
                            terrain.treeDistance = 10000f;
                            terrain.treeBillboardDistance = 200f;
                            terrain.drawTreesAndFoliage = true;
                        }
                    }
                    isSuccessful = true;
                    #endregion
                }

                #endif
            }

            return isSuccessful;
        }

        #if VEGETATION_STUDIO

        /// <summary>
        /// Get an existing Vegetation Studio Vegetation Package, or create one if it does not already exist.
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="isExisting"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static AwesomeTechnologies.VegetationPackage GetVegetationPackage(LBLandscape landscape, ref bool isExisting, bool showErrors)
        {
            AwesomeTechnologies.VegetationPackage vegetationPackage = null;

            #if UNITY_EDITOR
            vegetationPackage = LBEditorHelper.GetAsset<AwesomeTechnologies.VegetationPackage>(landscape.name + " ");

            if (vegetationPackage == null)
            {
                vegetationPackage = LBEditorHelper.CreateAsset<AwesomeTechnologies.VegetationPackage>(landscape.name + " ", false);
            }
            else
            {
                isExisting = true;
            }
            #endif

            return vegetationPackage;
        }

        /// <summary>
        /// Get an existing Vegetation Studio Persistent Storage Package, or create one if it does not already exist.
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="terrainIdx"></param>
        /// <param name="isExisting"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static AwesomeTechnologies.Vegetation.PersistentStorage.PersistentVegetationStoragePackage GetVegetationPersistentStoragePackage(LBLandscape landscape, int terrainIdx, ref bool isExisting, bool showErrors)
        {
            AwesomeTechnologies.Vegetation.PersistentStorage.PersistentVegetationStoragePackage persistentVegetationStoragePackage = null;

            #if UNITY_EDITOR
            persistentVegetationStoragePackage = LBEditorHelper.GetAsset<AwesomeTechnologies.Vegetation.PersistentStorage.PersistentVegetationStoragePackage>(landscape.name + (terrainIdx).ToString("0000") + " ");

            if (persistentVegetationStoragePackage == null)
            {
                // Create a storage package in the asset project folder and name it [landscape name][terrain number]
                persistentVegetationStoragePackage = LBEditorHelper.CreateAsset<AwesomeTechnologies.Vegetation.PersistentStorage.PersistentVegetationStoragePackage>(landscape.name + (terrainIdx).ToString("0000") + " ", false);
            }
            else
            {
                isExisting = true;
            }
            #endif

            return persistentVegetationStoragePackage;
        }

        /// <summary>
        /// Start (or stop/sleep) the Vegetation Studio VegetationSystem scripts attached to each terrain in a landscape
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="isStarted"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static bool VegetationStudioStartVegetationSystems(LBLandscape landscape, bool isStarted, bool showErrors)
        {
            bool isSuccessful = false;
            string methodName = "LBIntegration.VegetationStudioStartVegetationSystems";

            if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscape cannot be null"); } }
            else if (!IsVegetationStudioInstalled(true)) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - Vegetation Studio 1.1.0.3 or newer does not seem to be installed in the project."); } }
            else if (landscape.landscapeTerrains == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscape.landscapeTerrains cannot be null"); } }
            else
            {
                int numTerrains = (landscape.landscapeTerrains == null ? 0 : landscape.landscapeTerrains.Length);

                AwesomeTechnologies.VegetationSystem vegetationSystem = null;
                Terrain terrain;

                // Users can specify 0, 1 or multiple cameras to use with Vegetation Studio. If none are specified, auto select
                // camera will be used and 1 vegetation system per terrain will be created.
                if (landscape.vegetationStudioCameraList == null) { landscape.vegetationStudioCameraList = new List<Camera>(); }
                int numCameras = (landscape.vegetationStudioCameraList == null ? 0 : landscape.vegetationStudioCameraList.Count);
                int minNumCameras = (numCameras == 0 ? 1 : numCameras);

                // Re-use a list so we don't incur GC overhead
                List<AwesomeTechnologies.VegetationSystem> vegetationSystemList = new List<AwesomeTechnologies.VegetationSystem>(minNumCameras);

                for (int tIdx = 0; tIdx < numTerrains; tIdx++)
                {
                    terrain = landscape.landscapeTerrains[tIdx];
                    terrain.GetComponentsInChildren(vegetationSystemList);
                    int numVS = (vegetationSystemList == null ? 0 : vegetationSystemList.Count);
                    for (int vsIdx = 0; vsIdx < numVS; vsIdx++)
                    {
                        vegetationSystem = vegetationSystemList[vsIdx];

                        if (vegetationSystem != null)
                        {
                            // Wake up the Vegetation System on this terrain
                            vegetationSystem.SetSleepMode(!isStarted);
                        }
                    }
                }
                isSuccessful = true;
            }

            return isSuccessful;
        }

        /// <summary>
        /// Import the Unity Terrain textures into Vegetation Studio.
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static bool VegetationStudioImportTextures(LBLandscape landscape, bool showErrors)
        {
            bool isSuccessful = false;
            bool isExistingVegetationPackage = false;

            string methodName = "LBIntegration.VegetationStudioImportTextures";
            AwesomeTechnologies.VegetationStudio.VegetationStudioManager vegetationStudioManager = FindObjectOfType<AwesomeTechnologies.VegetationStudio.VegetationStudioManager>();

            if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscape cannot be null"); } }
            else if (!IsVegetationStudioInstalled(true)) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - Vegetation Studio 1.1.0.3 or newer does not seem to be installed in the project."); } }
            else if (vegetationStudioManager == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not find Vegetation Studio Manager in the scene."); } }
            else if (landscape.landscapeTerrains == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscape.landscapeTerrains cannot be null"); } }
            else if (!VegetationStudioStartVegetationSystems(landscape, true, showErrors)) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not wake up vegetation systems"); } }
            else
            {
                //int numTerrains = (landscape.landscapeTerrains == null ? 0 : landscape.landscapeTerrains.Length);

                // Re-use a list so we don't incur GC overhead
                //List<AwesomeTechnologies.VegetationSystem> vegetationSystemList = new List<AwesomeTechnologies.VegetationSystem>(1);
                LBTerrainTexture lbTerrainTexture;

                AwesomeTechnologies.VegetationPackage vegetationPackage = GetVegetationPackage(landscape, ref isExistingVegetationPackage, showErrors);

                if (vegetationPackage != null)
                {
                    // Textures will be added to the package but not applied to the terrain.
                    // Currently we want Texturing Tab, and Groups Tab to control textures.
                    // They can still be used in Vegetation Studio rules.
                    vegetationPackage.UseTerrainTextures = landscape.useVegetationSystemTextures;

                    // Ensure the height curves use the correct values. Auto will normalise the min/max heights in the terrains.
                    vegetationPackage.AutomaticMaxCurveHeight = false;
                    vegetationPackage.MaxCurveHeight = landscape.GetLandscapeTerrainHeight();

                    #region Copy Textures from LB to Vegetation Studio

                    List<LBTerrainTexture> enabledTextureTerrainList = landscape.terrainTexturesList.FindAll(tx => !tx.isDisabled);

                    // How many enabled textures do we have in the landscape?
                    int numLBTextures = (enabledTextureTerrainList == null ? 0 : enabledTextureTerrainList.Count);

                    // Vegetation Studio expects 0, 4, 8, 12, or 16 textures.
                    int base4Textures = (numLBTextures == 0 ? 0 : (int)Math.Ceiling(numLBTextures / 4f)) * 4;

                    vegetationPackage.TerrainTextureCount = base4Textures;

                    // Pre-populate with some default textures (this will fill any unused slots)
                    vegetationPackage.LoadDefaultTextures();

                    // Pre-populate with some default texture settings
                    vegetationPackage.SetupTerrainTextureSettings();

                    int numTexInfos = (vegetationPackage.TerrainTextureList == null ? 0 : vegetationPackage.TerrainTextureList.Count);
                    int numTexSettings = (vegetationPackage.TerrainTextureSettingsList == null ? 0 : vegetationPackage.TerrainTextureSettingsList.Count);

                    AwesomeTechnologies.TerrainTextureSettings terrainTextureSettings;
                    AwesomeTechnologies.Common.Interfaces.TerrainTextureInfo terrainTextureInfo;

                    float minHeight = 0f, maxHeight = 1f;
                    float minInclination = 0f, maxInclination = 1f;

                    // Load the textures from current landscape
                    for (int txIdx = 0; txIdx < base4Textures; txIdx++)
                    {
                        // Process the textures from the landscape
                        if (txIdx < numLBTextures)
                        {
                            lbTerrainTexture = enabledTextureTerrainList[txIdx];

                            if (lbTerrainTexture != null)
                            {
                                // Update default Vegetation Studio texture with the one from the landscape
                                if (numTexInfos > txIdx && lbTerrainTexture.texture != null)
                                {
                                    terrainTextureInfo = vegetationPackage.TerrainTextureList[txIdx];
                                    terrainTextureInfo.TileSize = lbTerrainTexture.tileSize;
                                    terrainTextureInfo.Texture = lbTerrainTexture.texture;
                                    terrainTextureInfo.TextureNormals = lbTerrainTexture.normalMap;
                                    terrainTextureInfo.TextureHeightMap = lbTerrainTexture.heightMap;
                                    terrainTextureInfo.TextureOcclusion = null;
                                }

                                if (numTexSettings > txIdx)
                                {
                                    // Update default Vegetation Studio texture settings with the one from the landscape
                                    terrainTextureSettings = vegetationPackage.TerrainTextureSettingsList[txIdx];
                                    terrainTextureSettings.TextureUseNoise = lbTerrainTexture.useNoise;
                                    terrainTextureSettings.TextureNoiseScale = lbTerrainTexture.noiseTileSize;
                                    terrainTextureSettings.TextureWeight = lbTerrainTexture.strength;

                                    if (lbTerrainTexture.texturingMode == LBTerrainTexture.TexturingMode.HeightAndInclination || lbTerrainTexture.texturingMode == LBTerrainTexture.TexturingMode.HeightInclinationMap)
                                    {
                                        minHeight = lbTerrainTexture.minHeight;
                                        maxHeight = lbTerrainTexture.maxHeight;
                                        minInclination = lbTerrainTexture.minInclination;
                                        maxInclination = lbTerrainTexture.maxInclination;
                                    }
                                    else if (lbTerrainTexture.texturingMode == LBTerrainTexture.TexturingMode.Height)
                                    {
                                        minHeight = lbTerrainTexture.minHeight;
                                        maxHeight = lbTerrainTexture.maxHeight;
                                        minInclination = 0f;
                                        maxInclination = 1f;
                                    }
                                    else if (lbTerrainTexture.texturingMode == LBTerrainTexture.TexturingMode.Inclination)
                                    {
                                        minHeight = 0f;
                                        maxHeight = 1f;
                                        minInclination = lbTerrainTexture.minInclination;
                                        maxInclination = lbTerrainTexture.maxInclination;
                                    }
                                    else
                                    {
                                        minHeight = 0f;
                                        maxHeight = 1f;
                                        minInclination = 0f;
                                        maxInclination = 1f;
                                    }

                                    // Vertical axis is amount and horizontal height where 1 is max. Currently there is no blend curve
                                    // in the integration, so amount is 0.0 or 1.0
                                    AnimationCurve heightCurve = new AnimationCurve();
                                    if (minHeight > 0f) { heightCurve.AddKey(0f, 0f); heightCurve.AddKey(minHeight-0.001f, 0f); }
                                    heightCurve.AddKey(minHeight, 1f);
                                    heightCurve.AddKey(maxHeight, 1f);
                                    if (maxHeight < 1f) { heightCurve.AddKey(1f, 0f); heightCurve.AddKey(maxHeight + 0.001f, 0f); }
                                    terrainTextureSettings.TextureHeightCurve = heightCurve;

                                    // Vertical axis is amount and horizontal steepness where 1 is max. Currently there is no blend curve
                                    // in the integration, so amount is 0.0 or 1.0
                                    AnimationCurve slopeCurve = new AnimationCurve();
                                    if (minInclination > 0f) { slopeCurve.AddKey(0f, 0f); slopeCurve.AddKey((minInclination / 90f) - 0.001f, 0f); }
                                    slopeCurve.AddKey(minInclination / 90f, 1f);
                                    slopeCurve.AddKey(maxInclination / 90f, 1f);
                                    if (maxInclination < 90f) { slopeCurve.AddKey(1f, 0f); slopeCurve.AddKey((maxInclination / 90f) + 0.001f, 0f); }
                                    terrainTextureSettings.TextureAngleCurve = slopeCurve;
                                }
                            }
                        }
                        // Process extra textures added by Vegetation Studio
                        // Set their weight to 0 so they are ignored.
                        else
                        {
                            if (numTexInfos > txIdx)
                            {
                                terrainTextureInfo = vegetationPackage.TerrainTextureList[txIdx];
                                terrainTextureInfo.TileSize = Vector2.one * 10f;
                            }

                            if (numTexSettings > txIdx)
                            {
                                terrainTextureSettings = vegetationPackage.TerrainTextureSettingsList[txIdx];
                                terrainTextureSettings.TextureWeight = 0f;
                            }
                        }
                    }
                    #endregion
                }
            }

            return isSuccessful;
        }

        /// <summary>
        /// Import the Unity Terrain trees into Vegetation Studio.
        /// Remove any existing trees.
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static bool VegetationStudioImportTrees(LBLandscape landscape, bool showErrors)
        {
            bool isSuccessful = false;
            bool isExistingVegetationPackage = false;

            string methodName = "LBIntegration.VegetationStudioImportTrees";
            AwesomeTechnologies.VegetationStudio.VegetationStudioManager vegetationStudioManager = FindObjectOfType<AwesomeTechnologies.VegetationStudio.VegetationStudioManager>();

            if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscape cannot be null"); } }
            else if (!IsVegetationStudioInstalled(true)) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - Vegetation Studio 1.1.0.3 or newer does not seem to be installed in the project."); } }
            else if (vegetationStudioManager == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not find Vegetation Studio Manager in the scene."); } }
            else if (landscape.landscapeTerrains == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscape.landscapeTerrains cannot be null"); } }
            else
            {
                AwesomeTechnologies.VegetationItemInfo vegetationItemInfo = null;
                // Users can specify 0, 1 or multiple cameras to use with Vegetation Studio.
                if (landscape.vegetationStudioCameraList == null) { landscape.vegetationStudioCameraList = new List<Camera>(); }
                int numCameras = (landscape.vegetationStudioCameraList == null ? 0 : landscape.vegetationStudioCameraList.Count);
                int minNumCameras = (numCameras == 0 ? 1 : numCameras);

                #if UNITY_EDITOR
                int numTerrains = (landscape.landscapeTerrains == null ? 0 : landscape.landscapeTerrains.Length);
                AwesomeTechnologies.VegetationSystem vegetationSystem = null;

                // Re-use a list so we don't incur GC overhead
                List<AwesomeTechnologies.VegetationSystem> vegetationSystemList = new List<AwesomeTechnologies.VegetationSystem>(minNumCameras);
                List<AwesomeTechnologies.Vegetation.PersistentStorage.PersistentVegetationStorage> persistentStorageList = new List<AwesomeTechnologies.Vegetation.PersistentStorage.PersistentVegetationStorage>(minNumCameras);
                Terrain terrain;
                TerrainData terrainData;
                #endif

                LBTerrainTree lbTerrainTree;

                AwesomeTechnologies.VegetationPackage vegetationPackage = GetVegetationPackage(landscape, ref isExistingVegetationPackage, showErrors);

                if (vegetationPackage != null)
                {
                    #region Add/Replace Tree types in vegetation package
                    List<LBTerrainTree> enabledTreeTerrainList = landscape.terrainTreesList.FindAll(tr => !tr.isDisabled && tr.prefab != null);

                    // How many enabled tree types do we have in the landscape?
                    int numLBTree = (enabledTreeTerrainList == null ? 0 : enabledTreeTerrainList.Count);

                    // Check all the existing tree types in Vegetation Studio
                    // Remove any that aren't currently configured in the LB Grass tab
                    // Remove any grasses that useMesh or prefab/texture names has changed
                    int numVegPkg = (vegetationPackage.VegetationInfoList == null ? 0 : vegetationPackage.VegetationInfoList.Count);
                    bool isRemoveVegInfo = false;

                    for (int vpkIdx = numVegPkg - 1; vpkIdx >= 0; vpkIdx--)
                    {
                        isRemoveVegInfo = false;
                        vegetationItemInfo = vegetationPackage.VegetationInfoList[vpkIdx];

                        //Debug.Log("VegInfo: " + vegetationItemInfo.Name + " " + vegetationItemInfo.VegetationItemID);

                        // Only process Trees
                        if (vegetationItemInfo.VegetationType == AwesomeTechnologies.VegetationType.Tree)
                        {
                            // Is this item in the LB Grass tab list?
                            lbTerrainTree = enabledTreeTerrainList.Find(gr => gr.GUID == vegetationItemInfo.VegetationItemID);
                            if (lbTerrainTree != null)
                            {
                                //Debug.Log("Found Tree " + vegetationItemInfo.Name + " " + lbTerrainTree.GUID + " " + " prefabname: " + lbTerrainTree.prefab.name);

                                // Has prefab changed?
                                if (vegetationItemInfo.Name != lbTerrainTree.prefab.name) { isRemoveVegInfo = true; }
                            }
                            else { isRemoveVegInfo = true; }

                            if (isRemoveVegInfo)
                            {
                                //Debug.Log("Removing Vegetation Info: " + vegetationItemInfo.Name + " " + vegetationItemInfo.VegetationItemID);
                                vegetationPackage.VegetationInfoList.Remove(vegetationItemInfo);
                            }
                        }
                    }

                    for (int tIdx = 0; tIdx < numLBTree; tIdx++)
                    {
                        lbTerrainTree = enabledTreeTerrainList[tIdx];

                        // Do we need to update an existing grass or add a new one?
                        vegetationItemInfo = vegetationPackage.GetVegetationInfo(lbTerrainTree.GUID);

                        if (vegetationItemInfo == null)
                        {
                            // Add the tree to the vegetation package. Supply our own GUID for easy reference later.
                            vegetationPackage.AddVegetationItem(lbTerrainTree.prefab, AwesomeTechnologies.VegetationType.Tree, false, lbTerrainTree.GUID);
                        }
                    }

                    #endregion

                    #region Add Persistant Storage Packages

                    #if UNITY_EDITOR

                    // TODO find existing packages

                    AwesomeTechnologies.Vegetation.PersistentStorage.PersistentVegetationStoragePackage persistentVegetationStoragePackage = null;
                    AwesomeTechnologies.Vegetation.PersistentStorage.PersistentVegetationStorage persistentVegetationStorage = null;

                    List<TreeInstance> treeInstanceList = new List<TreeInstance>();
                    List<TreeInstance> treeInstanceSubsetList = new List<TreeInstance>();
                    Vector3 terrainSize;
                    Vector3 terrainPosition;
                    Vector3 treePosition = Vector3.zero;

                    List<LBTerrainTree> protoTypeTreesList;
                    LBTerrainTree lbTerrainTreeMatch;
                    int numTreePrototypes = 0;
                    int numTreesInTerrain = 0;
                    int numTreesForPrototype = 0;

                    bool isExistingPersistentStoragePackage = false;

                    byte vegetationSourceID = GetVegetationStudioSourceID;

                    // We need 1 persistent storage package per terrain
                    for (int tIdx = 0; tIdx < numTerrains; tIdx++)
                    {
                        terrain = landscape.landscapeTerrains[tIdx];
                        terrainPosition = terrain.transform.position;
                        terrain.GetComponentsInChildren(vegetationSystemList);
                        terrain.GetComponentsInChildren(persistentStorageList);
                        terrainData = terrain.terrainData;
                        terrainSize = terrainData.size;

                        // Get the tree prototypes for the first terrain. Assumes all terrains have the same
                        // trees that were created with LB.
                        protoTypeTreesList = LBTerrainTree.ToLBTerrainTreeList(terrainData.treePrototypes);
                        numTreePrototypes = (protoTypeTreesList == null ? 0 : protoTypeTreesList.Count);

                        int numVS = (vegetationSystemList == null ? 0 : vegetationSystemList.Count);

                        if (persistentStorageList != null && persistentStorageList.Count > 0 && numVS > 0 && terrainData != null)
                        {
                            persistentVegetationStoragePackage = null;
                            isExistingPersistentStoragePackage = false;

                            // Loop through all the Vegetation Systems for this terrain. One for each camera.
                            for (int vsIdx = 0; vsIdx < numVS; vsIdx++)
                            {
                                vegetationSystem = vegetationSystemList[vsIdx];

                                #region wake VS and add Vegetation Package for this camera.
                                if (vegetationSystem != null)
                                {
                                    if (vegetationPackage != null)
                                    {
                                        int numVegetationPackages = (vegetationSystem.VegetationPackageList == null ? 0 : vegetationSystem.VegetationPackageList.Count);

                                        if (numVegetationPackages == 0)
                                        {
                                            vegetationSystem.VegetationPackageList.Add(vegetationPackage);
                                        }

                                        // Wake up the Vegetation System on this terrain
                                        vegetationSystem.SetSleepMode(false);

                                        // Doesn't seem to do much
                                        //vegetationSystem.SetVegetationPackage(0, true);
                                    }

                                    // Doesn't seem to do much
                                    //vegetationSystem.RefreshVegetationPackage();
                                }
                                #endregion

                                #region GET and Set Persistent Package Storage (per terrain per camera)
                                persistentVegetationStorage = persistentStorageList[vsIdx];

                                UnityEditor.Selection.activeObject = null;                                

                                // Get the existing persistent storage package, or create a new one and name it [landscape name][terrain number]
                                persistentVegetationStoragePackage = GetVegetationPersistentStoragePackage(landscape, tIdx, ref isExistingPersistentStoragePackage, showErrors);

                                if (persistentVegetationStoragePackage == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not create PersistentVegetationStoragePackage in project folder"); } }
                                {
                                    persistentVegetationStorage.SetPersistentVegetationStoragePackage(persistentVegetationStoragePackage);
                                }

                                #endregion
                            }

                            #region Populate the Persistent Storage Package (1 per Terrain)
                            if (persistentVegetationStoragePackage == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not create PersistentVegetationStoragePackage in project folder"); } }
                            else
                            {
                                if (isExistingPersistentStoragePackage)
                                {
                                    // TODO - Remove existing tree instances

                                    // For now delete everything...
                                    persistentVegetationStorage.InitializePersistentStorage();

                                    //AwesomeTechnologies.Vegetation.PersistentStorage.PersistentVegetationInfo persistentVegetationInfo
                                    //persistentVegetationStorage.RemoveVegetationItemInstances("", vegetationSourceID);

                                    //vegetationStudioManager
                                }
                                else { persistentVegetationStorage.InitializePersistentStorage(); }

                                #region Import Tree instances

                                numTreesInTerrain = terrainData.treeInstanceCount;

                                if (numTreesInTerrain > 0) { treeInstanceList.AddRange(terrainData.treeInstances); }

                                // Loop through all the tree prototypes in the terrain.
                                for (int tpIdx = 0; tpIdx < numTreePrototypes; tpIdx++)
                                {
                                    lbTerrainTree = protoTypeTreesList[tpIdx];

                                    // Find the first matching prototype in the LBTerrainTree list in the landscape (if any)
                                    lbTerrainTreeMatch = landscape.terrainTreesList.Find(tp => !tp.isDisabled && tp.prefab != null && tp.prefab == lbTerrainTree.prefab && tp.bendFactor == lbTerrainTree.bendFactor);

                                    if (lbTerrainTreeMatch == null) { Debug.Log("INFO: " + methodName + " skipping tree prototype " + (lbTerrainTree.prefab == null ? "no prefab" : lbTerrainTree.prefab.name) + " because it is not in the Landscape Builder Tree tab list"); }
                                    else
                                    {
                                        // Get tree instances for this prototype which is in the Landscape Tree types list
                                        treeInstanceSubsetList.AddRange(treeInstanceList.FindAll(ti => ti.prototypeIndex == tpIdx));
                                        numTreesForPrototype = treeInstanceSubsetList.Count;

                                        //Debug.Log("terrain: " + terrain.name + " prototype: " + protoTypeTreesList[tpIdx].prefab.name + " trees: " + numTreesForPrototype);

                                        // Process all the trees for this treeprototype in the current terrain
                                        for (int trIdx = 0; trIdx < numTreesForPrototype; trIdx++)
                                        {
                                            TreeInstance treeInstance = treeInstanceSubsetList[trIdx];

                                            treePosition.x = treeInstance.position.x * terrainSize.x;
                                            treePosition.y = treeInstance.position.y * terrainSize.y;
                                            treePosition.z = treeInstance.position.z * terrainSize.z;
                                            treePosition += terrainPosition;

                                            persistentVegetationStorage.AddVegetationItemInstance
                                            ( lbTerrainTreeMatch.GUID, treePosition,
                                              new Vector3(treeInstance.widthScale, treeInstance.heightScale, treeInstance.widthScale),
                                              Quaternion.Euler(0, treeInstance.rotation * Mathf.Rad2Deg, 0),true, vegetationSourceID
                                            );
                                        }
                                    }

                                    treeInstanceSubsetList.Clear();
                                }

                                #endregion
                            }
                            #endregion
                        }
                        treeInstanceList.Clear();
                        persistentStorageList.Clear();
                    }
                    #endif

                    #endregion

                    //SetVegetationPackage(VegetationPackageIndex, true);
                    isSuccessful = true;
                }
            }

            return isSuccessful;
        }

        /// <summary>
        /// Import the Unity Terrain grass into Vegetation Studio.
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static bool VegetationStudioImportGrass(LBLandscape landscape, bool showErrors)
        {
            bool isSuccessful = false;
            bool isExistingVegetationPackage = false;

            string methodName = "LBIntegration.VegetationStudioImportGrass";
            AwesomeTechnologies.VegetationStudio.VegetationStudioManager vegetationStudioManager = FindObjectOfType<AwesomeTechnologies.VegetationStudio.VegetationStudioManager>();

            if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscape cannot be null"); } }
            else if (!IsVegetationStudioInstalled(true)) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - Vegetation Studio 1.1.0.3 or newer does not seem to be installed in the project."); } }
            else if (vegetationStudioManager == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not find Vegetation Studio Manager in the scene."); } }
            else if (!VegetationStudioStartVegetationSystems(landscape, true, showErrors)) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not wake up vegetation systems"); } }
            else if (landscape.landscapeTerrains == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscape.landscapeTerrains cannot be null"); } }
            else
            {
                int numTerrains = (landscape.landscapeTerrains == null ? 0 : landscape.landscapeTerrains.Length);
                // Users can specify 0, 1 or multiple cameras to use with Vegetation Studio.
                if (landscape.vegetationStudioCameraList == null) { landscape.vegetationStudioCameraList = new List<Camera>(); }
                int numCameras = (landscape.vegetationStudioCameraList == null ? 0 : landscape.vegetationStudioCameraList.Count);
                int minNumCameras = (numCameras == 0 ? 1 : numCameras);

                AwesomeTechnologies.VegetationSystem vegetationSystem = null;
                AwesomeTechnologies.VegetationItemInfo vegetationItemInfo = null;

                // Re-use a list so we don't incur GC overhead
                List<AwesomeTechnologies.VegetationSystem> vegetationSystemList = new List<AwesomeTechnologies.VegetationSystem>(minNumCameras);
                LBTerrainGrass lbTerrainGrass;
                Terrain terrain;
                
                AwesomeTechnologies.VegetationPackage vegetationPackage = GetVegetationPackage(landscape, ref isExistingVegetationPackage, showErrors);

                if (vegetationPackage != null)
                {
                    #region Add/Replace Grass types in vegetation package
                    List<LBTerrainGrass> enabledGrassTerrainList = landscape.terrainGrassList.FindAll(gr => !gr.isDisabled && (gr.texture != null || (gr.useMeshPrefab && gr.meshPrefab != null)));

                    // How many enabled grass types do we have in the landscape?
                    int numLBGrass = (enabledGrassTerrainList == null ? 0 : enabledGrassTerrainList.Count);

                    // Check all the existing grass types in Vegetation Studio
                    // Remove any that aren't currently configured in the LB Grass tab
                    // Remove any grasses that useMesh or prefab/texture names has changed
                    int numVegPkg = (vegetationPackage.VegetationInfoList == null ? 0 : vegetationPackage.VegetationInfoList.Count);
                    bool isRemoveVegInfo = false;

                    for (int vpkIdx = numVegPkg - 1; vpkIdx >= 0; vpkIdx--)
                    {
                        isRemoveVegInfo = false;
                        vegetationItemInfo = vegetationPackage.VegetationInfoList[vpkIdx];

                        //Debug.Log("VegInfo: " + vegetationItemInfo.Name + " " + vegetationItemInfo.VegetationItemID);

                        // Only process Grass
                        if (vegetationItemInfo.VegetationType == AwesomeTechnologies.VegetationType.Grass)
                        {
                            // Is this item in the LB Grass tab list?
                            lbTerrainGrass = enabledGrassTerrainList.Find(gr => gr.GUID == vegetationItemInfo.VegetationItemID);
                            if (lbTerrainGrass != null)
                            {
                                //Debug.Log("Found Grass " + vegetationItemInfo.Name + " " + lbTerrainGrass.GUID + " " + vegetationItemInfo.PrefabType + " txtname: " + lbTerrainGrass.textureName);

                                // Has useMesh or prefab/texture changed?
                                if (vegetationItemInfo.PrefabType == AwesomeTechnologies.VegetationPrefabType.Mesh && (!lbTerrainGrass.useMeshPrefab || vegetationItemInfo.Name != lbTerrainGrass.meshPrefab.name) ||
                                    vegetationItemInfo.PrefabType == AwesomeTechnologies.VegetationPrefabType.Texture && (lbTerrainGrass.useMeshPrefab || vegetationItemInfo.Name != lbTerrainGrass.texture.name)
                                    )
                                {
                                    isRemoveVegInfo = true;
                                }
                            }
                            else { isRemoveVegInfo = true; }

                            if (isRemoveVegInfo)
                            {
                                //Debug.Log("Removing Vegetation Info: " + vegetationItemInfo.Name + " " + vegetationItemInfo.VegetationItemID);
                                vegetationPackage.VegetationInfoList.Remove(vegetationItemInfo);
                            }
                        }
                    }

                    float terrainHeight = landscape.GetLandscapeTerrainHeight();

                    for (int grIdx = 0; grIdx < numLBGrass; grIdx++)
                    {
                        lbTerrainGrass = enabledGrassTerrainList[grIdx];

                        // Do we need to update an existing grass or add a new one?
                        vegetationItemInfo = vegetationPackage.GetVegetationInfo(lbTerrainGrass.GUID);

                        if (vegetationItemInfo == null)
                        {
                            if (lbTerrainGrass.useMeshPrefab)
                            {
                                // Add the grass to the vegetation package. Supply our own GUID for easy reference later.
                                vegetationPackage.AddVegetationItem(lbTerrainGrass.meshPrefab, AwesomeTechnologies.VegetationType.Grass, false, lbTerrainGrass.GUID);
                            }
                            else
                            {
                                vegetationPackage.AddVegetationItem(lbTerrainGrass.texture, AwesomeTechnologies.VegetationType.Grass, false, lbTerrainGrass.GUID);
                            }

                            // Retrieve the new grass item we just added
                            vegetationItemInfo = vegetationPackage.GetVegetationInfo(lbTerrainGrass.GUID);
                        }

                        // Update basic grass information
                        if (vegetationItemInfo != null)
                        {
                            vegetationItemInfo.YScale = lbTerrainGrass.maxWidth;
                            vegetationItemInfo.EnableRuntimeSpawn = true;

                            vegetationItemInfo.IncludeDetailLayer = grIdx;
                            vegetationItemInfo.UseIncludeDetailMaskRules = true;
                            vegetationItemInfo.UsePerlinMask = lbTerrainGrass.useNoise;
                            vegetationItemInfo.PerlinCutoff = lbTerrainGrass.grassPlacementCutoff;
                            vegetationItemInfo.PerlinScale = lbTerrainGrass.noiseTileSize;

                            vegetationItemInfo.VegetationScaleType = AwesomeTechnologies.VegetationScaleType.Simple;
                            vegetationItemInfo.MinScale = lbTerrainGrass.minHeight;
                            vegetationItemInfo.MaxScale = lbTerrainGrass.maxHeight;

                            // Get the min/max Height and min/max Inclination (slope or steepness)
                            float minHeight = 0f, maxHeight = terrainHeight;
                            float minInclination = 0f, maxInclination = 90f;

                            if (lbTerrainGrass.grassPlacingMode == LBTerrainGrass.GrassPlacingMode.HeightAndInclination || lbTerrainGrass.grassPlacingMode == LBTerrainGrass.GrassPlacingMode.HeightInclinationMap)
                            {
                                minHeight = lbTerrainGrass.minPopulatedHeight * terrainHeight;
                                maxHeight = lbTerrainGrass.maxPopulatedHeight * terrainHeight;
                                minInclination = lbTerrainGrass.minInclination;
                                maxInclination = lbTerrainGrass.maxInclination;
                            }
                            else if (lbTerrainGrass.grassPlacingMode == LBTerrainGrass.GrassPlacingMode.Height)
                            {
                                minHeight = lbTerrainGrass.minPopulatedHeight * terrainHeight;
                                maxHeight = lbTerrainGrass.maxPopulatedHeight * terrainHeight;
                                minInclination = 0f;
                                maxInclination = 90f;
                            }
                            else if (lbTerrainGrass.grassPlacingMode == LBTerrainGrass.GrassPlacingMode.Inclination)
                            {
                                minHeight = 0f;
                                maxHeight = terrainHeight;
                                minInclination = lbTerrainGrass.minInclination;
                                maxInclination = lbTerrainGrass.maxInclination;
                            }
                            else
                            {
                                minHeight = 0f;
                                maxHeight = terrainHeight;
                                minInclination = 0f;
                                maxInclination = 90f;
                            }

                            vegetationItemInfo.VegetationHeightType = AwesomeTechnologies.VegetationHeightType.Simple;
                            vegetationItemInfo.MinimumHeight = minHeight;
                            vegetationItemInfo.MaximumHeight = maxHeight;

                            vegetationItemInfo.VegetationSteepnessType = AwesomeTechnologies.VegetationSteepnessType.Simple;
                            vegetationItemInfo.MinimumSteepness = minInclination;
                            vegetationItemInfo.MaximumSteepness = maxInclination;
                        }
                    }

                    // If there is grass in the Grass Tab, instruct Vegetation Studio to load it.
                    for (int tIdx = 0; tIdx < numTerrains; tIdx++)
                    {
                        terrain = landscape.landscapeTerrains[tIdx];
                        terrain.GetComponentsInChildren(vegetationSystemList);
                        int numVS = (vegetationSystemList == null ? 0 : vegetationSystemList.Count);

                        // Loop through all the Vegetation Systems for this terrain. One for each camera.
                        for (int vsIdx = 0; vsIdx < numVS; vsIdx++)
                        {
                            vegetationSystem = vegetationSystemList[vsIdx];
                            vegetationSystem.LoadUnityTerrainDetails = (numLBGrass > 0);
                        }
                    }

                    #endregion

                    isSuccessful = true;
                }
            }

            return isSuccessful;
        }

        /// <summary>
        /// Set the camera to be used for Vegetation Studio on each terrain.
        /// To auto-select the camera (the default), pass in null for the camera parameter.
        /// Auto-select in Vegetation Studio will pick the first camera with the MainCamera Unity tag.
        /// cameraIndex is the non-zero when there are multiple cameras used at the same time. This will
        /// mean there are multiple VegetationSystems per terrain.
        /// Currently called from LBCameraAnimator.cs
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="camera"></param>
        /// <param name="cameraIndex"></param>
        /// <param name="showErrors"></param>
        public static void VegetationStudioSetCamera(LBLandscape landscape, Camera camera, int cameraIndex, bool showErrors)
        {
            string methodName = "LBIntegration.VegetationStudioSetCameras";
            AwesomeTechnologies.VegetationStudio.VegetationStudioManager vegetationStudioManager = FindObjectOfType<AwesomeTechnologies.VegetationStudio.VegetationStudioManager>();

            if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscape cannot be null"); } }
            else if (!IsVegetationStudioInstalled(true)) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - Vegetation Studio 1.1.0.3 or newer does not seem to be installed in the project."); } }
            else if (vegetationStudioManager == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not find Vegetation Studio Manager in the scene."); } }
            //else if (!VegetationStudioStartVegetationSystems(landscape, true, showErrors)) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not wake up vegetation systems"); } }
            else if (landscape.landscapeTerrains == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscape.landscapeTerrains cannot be null"); } }
            else
            {
                int numTerrains = (landscape.landscapeTerrains == null ? 0 : landscape.landscapeTerrains.Length);

                AwesomeTechnologies.VegetationSystem vegetationSystem = null;

                // Re-use a list so we don't incur GC overhead (typically there will be 1 or two cameras for single or multi-user)
                List<AwesomeTechnologies.VegetationSystem> vegetationSystemList = new List<AwesomeTechnologies.VegetationSystem>(2);

                Terrain terrain;

                for (int tIdx = 0; tIdx < numTerrains; tIdx++)
                {
                    terrain = landscape.landscapeTerrains[tIdx];
                    terrain.GetComponentsInChildren(vegetationSystemList);

                    int numVS = (vegetationSystemList == null ? 0 : vegetationSystemList.Count);

                    if (cameraIndex < numVS)
                    {
                        vegetationSystem = vegetationSystemList[cameraIndex];

                        // If camera is not define, set to auto select the camera (e.g. camera tagged MainCamera)
                        if (camera == null) { vegetationSystem.AutoselectCamera = true; }
                        else
                        {
                            vegetationSystem.AutoselectCamera = false;
                            vegetationSystem.SetCamera(camera);
                        }

                        // Wake up the Vegetation System on this terrain
                        vegetationSystem.SetSleepMode(false);
                    }
                }
            }
        }

        #endif

        #endregion

        #region Vegetation Studio Pro

        #if VEGETATION_STUDIO_PRO && UNITY_EDITOR
        private static readonly string vsProStorageDataFolder = "PersistentVegetationStorageData";
        #endif

        /// <summary>
        /// Determine if Vegetation Studio Pro is installed in the project
        /// </summary>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static bool IsVegetationStudioProInstalled()
        {
            #if VEGETATION_STUDIO_PRO
            return true;
            #else
            return false;
            #endif
        }

        /// <summary>
        /// Add Vegetation Studio scripts to the landscape.
        /// USAGE: landscape.SetLandscapeTerrains(true);
        /// if (LBIntegration.VegetationStudioProEnable(landscape, landscape.useVegetationSystem, true)) { .. }
        /// Currently this is EDITOR ONLY
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="isEnabled"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static bool VegetationStudioProEnable(LBLandscape landscape, bool isEnabled, bool showErrors)
        {
            bool isSuccessful = false;
            string methodName = "LBIntegration.VegetationStudioProEnable";

            if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscape cannot be null"); } }
            else if (!IsVegetationStudioProInstalled()) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - Vegetation Studio Pro 0.8.0.0 or newer does not seem to be installed in the project."); } }
            else if (landscape.landscapeTerrains == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscape.landscapeTerrains cannot be null"); } }
            else
            {
                #if VEGETATION_STUDIO_PRO
                
                Terrain terrain = null;
                #region Enable VSPro
                if (isEnabled)
                {
                    try
                    {
                        // Does the manager already exist in the scene?
                        VegetationStudioManager vegetationStudioManager = FindObjectOfType<VegetationStudioManager>();
                        if (vegetationStudioManager == null)
                        {
                            // The manager isn't in the scene so attempt to add manager, vspro object, and auto-add terrains
                            // Note: This will only work with a single landscape in the scene.
                            #if UNITY_EDITOR
                            LBEditorHelper.CallMenu("Window/Awesome Technologies/Add Vegetation Studio Pro to scene");
                            isSuccessful = true;
                            #endif
                        }
                        else
                        {
                            // Vegetation Studio Manager exists, so find the child VegetationSystemPro object
                            VegetationSystemPro vsPro = vegetationStudioManager.GetComponentInChildren<VegetationSystemPro>();
                            if (vsPro == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not get type VegetationSystemPro. PLEASE REPORT"); } }
                            else
                            {
                                int numTerrains = (landscape.landscapeTerrains == null ? 0 : landscape.landscapeTerrains.Length);

                                // Add each of the terrains to VS Pro
                                for (int tIdx = 0; tIdx < numTerrains; tIdx++)
                                {
                                    terrain = landscape.landscapeTerrains[tIdx];
                                    if (terrain != null)
                                    {
                                        // Add the VSPro UnityTerrain script to the terrain if it doesn't already exist
                                        Component vsUnityTerrain = terrain.GetComponent<UnityTerrain>();
                                        if (vsUnityTerrain == null)
                                        {
                                            terrain.gameObject.AddComponent<UnityTerrain>();
                                        }
                                        terrain.drawTreesAndFoliage = false;
                                        vsPro.AddTerrain(terrain.gameObject);
                                    }
                                }

                                if (numTerrains > 0)
                                {
                                    // Check to see if Vegetation Package already exists in the project, else create a new one
                                    bool isExistingVegetationPackage = false;
                                    // NOTE: Use a vegetation package for LB, rather than assume an existing one in VSPro created outside LB can be modified.
                                    VegetationPackagePro vegetationPackagePro = GetVegetationPackagePro(landscape, ref isExistingVegetationPackage, showErrors);
                                    if (vegetationPackagePro != null)
                                    {
                                        vegetationPackagePro.PackageName = "LB " + landscape.name;
                                        vsPro.AddVegetationPackage(vegetationPackagePro);
                                        #if UNITY_EDITOR
                                        UnityEditor.EditorUtility.SetDirty(vegetationPackagePro);

                                        #endif
                                    }

                                    // Add the persistent storage component if it doesn't already exist
                                    PersistentVegetationStorage persistentVegetationStorage = vsPro.GetComponent<PersistentVegetationStorage>();
                                    if (persistentVegetationStorage == null)
                                    {
                                        persistentVegetationStorage = vsPro.gameObject.AddComponent<PersistentVegetationStorage>();
                                    }

                                    if (persistentVegetationStorage == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not get find PersistentVegetationStorage component. PLEASE REPORT"); } }
                                    else
                                    {                                      
                                        // If there is no existing package, attempt to find it in the project or create a new one
                                        if (persistentVegetationStorage.PersistentVegetationStoragePackage == null)
                                        {
                                            bool isExistingStoragePackage = false;
                                            PersistentVegetationStoragePackage persistentVegetationStoragePackage = GetVegetationPersistentStoragePackage(landscape, ref isExistingStoragePackage, showErrors);

                                            persistentVegetationStorage.SetPersistentVegetationStoragePackage(persistentVegetationStoragePackage);

                                            //if (!isExistingStoragePackage) { persistentVegetationStorage.InitializePersistentStorage(); }
                                            persistentVegetationStorage.InitializePersistentStorage();
                                        }

                                        if (vegetationPackagePro != null)
                                        {
                                            VegetationStudioProImportTextures(landscape, showErrors);
                                            VegetationStudioProImportTrees(landscape, showErrors);
                                            VegetationStudioProImportGrass(landscape, showErrors);
                                        }

                                        isSuccessful = true;
                                    }
                                }
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not add Vegetation Studio Pro to scene. " + ex.Message); }
                    }
                }
                #endregion

                #region Disable VSPro
                else
                {
                    #region Remove any Veg Studio scripts from the terrains
                    AwesomeTechnologies.VegetationSystem.VegetationSystemPro vsPro = FindObjectOfType<AwesomeTechnologies.VegetationSystem.VegetationSystemPro>();
                    if (vsPro == null)
                    {
                        // New scenes where VS Pro hasn't yet been enabled will not have a VS Pro script in the scene.
                        // If the users has deleted it, there is not much we can do without raising a false error for new scenes.
                        isSuccessful = true;
                        //if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not get type VegetationSystemPro. PLEASE REPORT"); }
                    }
                    else
                    {
                        int numTerrains = (landscape.landscapeTerrains == null ? 0 : landscape.landscapeTerrains.Length);

                        bool vsRemoved = false;

                        for (int tIdx = 0; tIdx < numTerrains; tIdx++)
                        {
                            terrain = landscape.landscapeTerrains[tIdx];
                            if (terrain != null)
                            {
                                vsPro.RemoveTerrain(terrain.gameObject);
                                Component vsUnityTerrain = terrain.GetComponent<AwesomeTechnologies.VegetationSystem.UnityTerrain>();
                                if (vsUnityTerrain != null)
                                {
                                    #if UNITY_EDITOR
                                    DestroyImmediate(vsUnityTerrain);
                                    #else
                                    Destroy(vsUnityTerrain);
                                    #endif
                                    vsRemoved = true;
                                }

                                // Restore default Pixel Error, Tree and Detail Distances
                                // only where VSys was previously applied.
                                if (vsRemoved)
                                {
                                    terrain.heightmapPixelError = 1f;
                                    terrain.basemapDistance = 1500f;
                                    terrain.detailObjectDistance = 200f;
                                    terrain.treeDistance = 10000f;
                                    terrain.treeBillboardDistance = 200f;
                                    terrain.drawTreesAndFoliage = true;
                                }
                            }
                        }

                        // Remove, but don't delete, the Vegetation Package (Biome)
                        if (vsRemoved)
                        {
                            VegetationPackagePro vegetationPackage = vsPro.VegetationPackageProList.Find(vp => vp.PackageName.StartsWith("LB " + landscape.name));
                            if (vegetationPackage != null)
                            {
                                vsPro.RemoveVegetationPackage(vegetationPackage);
                            }
                        }

                        isSuccessful = true;
                    }

                    #endregion
                }
                #endregion

                #endif
            }

            return isSuccessful;
        }

#if VEGETATION_STUDIO_PRO

        /// <summary>
        /// Get an existing Vegetation Studio Pro Persistent Storage Package, or create one if it does not already exist.
        /// Vegetation Studio Pro only
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="isExisting"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static PersistentVegetationStoragePackage GetVegetationPersistentStoragePackage(LBLandscape landscape, ref bool isExisting, bool showErrors)
        {
            PersistentVegetationStoragePackage persistentVegetationStoragePackage = null;
            isExisting = false;

#if UNITY_EDITOR

            LBEditorHelper.CheckFolder("Assets/" + vsProStorageDataFolder);

            string packagePath = vsProStorageDataFolder + "/" + landscape.name + "_" + UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name + " ";

            persistentVegetationStoragePackage = LBEditorHelper.GetAsset<PersistentVegetationStoragePackage>(packagePath);

            if (persistentVegetationStoragePackage == null)
            {
                // Create a storage package in the asset project folder and name it [landscape name][sceneName]
                persistentVegetationStoragePackage = LBEditorHelper.CreateAsset<PersistentVegetationStoragePackage>(packagePath, false);
            }
            else
            {
                isExisting = true;
            }
#endif

            return persistentVegetationStoragePackage;
        }

        /// <summary>
        /// Get a reference to the VegetationSystemPro script in the scene
        /// </summary>
        /// <returns></returns>
        public static VegetationSystemPro GetVegetationSystemPro()
        {
            VegetationStudioManager vegetationStudioManager = FindObjectOfType<VegetationStudioManager>();
            if (vegetationStudioManager != null)
            {
                // Vegetation Studio Manager exists, so find the child VegetationSystemPro object
                return vegetationStudioManager.GetComponentInChildren<VegetationSystemPro>();
            }
            else { return null; }
        }

        /// <summary>
        /// Get an existing Vegetation Studio Pro Vegetation Package, or create one if it does not already exist.
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="isExisting"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static VegetationPackagePro GetVegetationPackagePro(LBLandscape landscape, ref bool isExisting, bool showErrors)
        {
            VegetationPackagePro vegetationPackage = null;

#if UNITY_EDITOR
            vegetationPackage = LBEditorHelper.GetAsset<VegetationPackagePro>(landscape.name + " ");

            if (vegetationPackage == null)
            {
                vegetationPackage = LBEditorHelper.CreateAsset<VegetationPackagePro>(landscape.name + " ", false);
            }
            else
            {
                isExisting = true;
            }
#endif

            return vegetationPackage;
        }

        /// <summary>
        /// Import the Unity Terrain textures into Vegetation Studio Pro.
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static bool VegetationStudioProImportTextures(LBLandscape landscape, bool showErrors)
        {
            bool isSuccessful = false;

            string methodName = "LBIntegration.VegetationStudioImportTextures";
            VegetationStudioManager vegetationStudioManager = FindObjectOfType<VegetationStudioManager>();

            if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscape cannot be null"); } }
            else if (!IsVegetationStudioProInstalled()) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - Vegetation Studio Pro 0.8.0.0 or newer does not seem to be installed in the project."); } }
            else if (vegetationStudioManager == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not find Vegetation Studio Manager in the scene."); } }
            else if (landscape.landscapeTerrains == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscape.landscapeTerrains cannot be null"); } }
            else
            {
                // Vegetation Studio Manager exists, so find the child VegetationSystemPro object
                VegetationSystemPro vsPro = vegetationStudioManager.GetComponentInChildren<VegetationSystemPro>();
                if (vsPro == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not get type VegetationSystemPro. PLEASE REPORT"); } }
                else
                {
                    // Get the persistent storage package from the component (script)
                    PersistentVegetationStorage persistentVegetationStorage = vsPro.GetComponent<PersistentVegetationStorage>();
                    if (persistentVegetationStorage == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not get PersistentVegetationStorage component. PLEASE REPORT"); } }
                    else
                    {
                        PersistentVegetationStoragePackage pvstoragePackage = persistentVegetationStorage.PersistentVegetationStoragePackage;
                        if (pvstoragePackage != null)
                        {
                            // Get the Vegetation Package (Biome) for this landscape used by LB.
                            VegetationPackagePro vegetationPackage = vsPro.VegetationPackageProList.Find(vp => vp.PackageName.StartsWith("LB " + landscape.name));

                            if (vegetationPackage == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not find VegetationPackage (Biome) called " + "LB " + landscape.name); } }
                            else
                            {
                                // Textures will be added to the package but not applied to the terrain.
                                // Currently we want Texturing Tab, and Groups Tab to control textures.
                                // They can still be used in Vegetation Studio Pro rules.
                                bool autoSplatEnabled = landscape.useVegetationSystemTextures;

                                // Ensure the height curves use the correct values. Auto will normalise the min/max heights in the terrains.
                                //vegetationPackage.AutomaticMaxCurveHeight = false;
                                //vegetationPackage.MaxCurveHeight = landscape.GetLandscapeTerrainHeight();

        #region Copy Textures from LB to Vegetation Studio Pro

                                List<LBTerrainTexture> enabledTextureTerrainList = landscape.terrainTexturesList.FindAll(tx => !tx.isDisabled);

                                // How many enabled textures do we have in the landscape?
                                int numLBTextures = (enabledTextureTerrainList == null ? 0 : enabledTextureTerrainList.Count);

                                // Vegetation Studio Pro expects 0, 4, 8, 12, or 16 textures.
                                int base4Textures = (numLBTextures == 0 ? 0 : (int)Math.Ceiling(numLBTextures / 4f)) * 4;

                                vegetationPackage.TerrainTextureCount = base4Textures;

                                // Pre-populate with some default textures (this will fill any unused slots)
                                vegetationPackage.LoadDefaultTextures();

                                // Pre-populate with some default texture settings
                                vegetationPackage.SetupTerrainTextureSettings();

                                int numTexInfos = (vegetationPackage.TerrainTextureList == null ? 0 : vegetationPackage.TerrainTextureList.Count);
                                int numTexSettings = (vegetationPackage.TerrainTextureSettingsList == null ? 0 : vegetationPackage.TerrainTextureSettingsList.Count);

                                TerrainTextureSettings terrainTextureSettings;
                                TerrainTextureInfo terrainTextureInfo;
                                LBTerrainTexture lbTerrainTexture = null;

                                float minHeight = 0f, maxHeight = 1f;
                                float minInclination = 0f, maxInclination = 1f;

        #region Load textures from current landscape
                                for (int txIdx = 0; txIdx < base4Textures; txIdx++)
                                {
                                    if (txIdx < numLBTextures)
                                    {
                                        lbTerrainTexture = enabledTextureTerrainList[txIdx];

                                        if (lbTerrainTexture != null)
                                        {
                                            // Update default Vegetation Studio Pro texture with the one from the landscape
                                            if (numTexInfos > txIdx && lbTerrainTexture.texture != null)
                                            {
                                                terrainTextureInfo = vegetationPackage.TerrainTextureList[txIdx];
                                                terrainTextureInfo.TileSize = lbTerrainTexture.tileSize;
                                                if (lbTerrainTexture.isTinted && lbTerrainTexture.tintedTexture != null)
                                                {
                                                    terrainTextureInfo.Texture = lbTerrainTexture.tintedTexture;
                                                }
                                                else { terrainTextureInfo.Texture = lbTerrainTexture.texture; }
                                                terrainTextureInfo.TextureNormals = lbTerrainTexture.normalMap;
                                                terrainTextureInfo.TextureHeightMap = lbTerrainTexture.heightMap;
                                                terrainTextureInfo.TextureOcclusion = null;
                                            }

                                            if (numTexSettings > txIdx)
                                            {
                                                // Update default Vegetation Studio texture settings with the one from the landscape
                                                terrainTextureSettings = vegetationPackage.TerrainTextureSettingsList[txIdx];
                                                terrainTextureSettings.UseNoise = lbTerrainTexture.useNoise;
                                                terrainTextureSettings.NoiseScale = lbTerrainTexture.noiseTileSize;
                                                terrainTextureSettings.TextureWeight = lbTerrainTexture.strength;

                                                // Enabled this texture for Auto Splat generation (OFF by default in LB)
                                                terrainTextureSettings.Enabled = autoSplatEnabled;

                                                if (lbTerrainTexture.texturingMode == LBTerrainTexture.TexturingMode.HeightAndInclination ||
                                                    lbTerrainTexture.texturingMode == LBTerrainTexture.TexturingMode.HeightInclinationCurvature ||
                                                    lbTerrainTexture.texturingMode == LBTerrainTexture.TexturingMode.HeightInclinationMap)
                                                {
                                                    minHeight = lbTerrainTexture.minHeight;
                                                    maxHeight = lbTerrainTexture.maxHeight;
                                                    minInclination = lbTerrainTexture.minInclination;
                                                    maxInclination = lbTerrainTexture.maxInclination;
                                                }
                                                else if (lbTerrainTexture.texturingMode == LBTerrainTexture.TexturingMode.Height)
                                                {
                                                    minHeight = lbTerrainTexture.minHeight;
                                                    maxHeight = lbTerrainTexture.maxHeight;
                                                    minInclination = 0f;
                                                    maxInclination = 1f;
                                                }
                                                else if (lbTerrainTexture.texturingMode == LBTerrainTexture.TexturingMode.Inclination)
                                                {
                                                    minHeight = 0f;
                                                    maxHeight = 1f;
                                                    minInclination = lbTerrainTexture.minInclination;
                                                    maxInclination = lbTerrainTexture.maxInclination;
                                                }
                                                else
                                                {
                                                    minHeight = 0f;
                                                    maxHeight = 1f;
                                                    minInclination = 0f;
                                                    maxInclination = 1f;
                                                }

                                                // Vertical axis is amount and horizontal height where 1 is max. Currently there is no blend curve
                                                // in the integration, so amount is 0.0 or 1.0
                                                AnimationCurve heightCurve = new AnimationCurve();
                                                if (minHeight > 0f) { heightCurve.AddKey(0f, 0f); heightCurve.AddKey(minHeight - 0.001f, 0f); }
                                                heightCurve.AddKey(minHeight, 1f);
                                                heightCurve.AddKey(maxHeight, 1f);
                                                if (maxHeight < 1f) { heightCurve.AddKey(1f, 0f); heightCurve.AddKey(maxHeight + 0.001f, 0f); }
                                                terrainTextureSettings.TextureHeightCurve = heightCurve;

                                                // Vertical axis is amount and horizontal steepness where 1 is max. Currently there is no blend curve
                                                // in the integration, so amount is 0.0 or 1.0
                                                AnimationCurve slopeCurve = new AnimationCurve();
                                                if (minInclination > 0f) { slopeCurve.AddKey(0f, 0f); slopeCurve.AddKey((minInclination / 90f) - 0.001f, 0f); }
                                                slopeCurve.AddKey(minInclination / 90f, 1f);
                                                slopeCurve.AddKey(maxInclination / 90f, 1f);
                                                if (maxInclination < 90f) { slopeCurve.AddKey(1f, 0f); slopeCurve.AddKey((maxInclination / 90f) + 0.001f, 0f); }
                                                terrainTextureSettings.TextureSteepnessCurve = slopeCurve;

                                                // Apply curvature rules
                                                if (lbTerrainTexture.texturingMode == LBTerrainTexture.TexturingMode.HeightInclinationCurvature)
                                                {
                                                    if (lbTerrainTexture.isCurvatureConcave) { terrainTextureSettings.ConcaveEnable = true; terrainTextureSettings.ConvexEnable = false; }
                                                    else { terrainTextureSettings.ConcaveEnable = false; terrainTextureSettings.ConvexEnable = true; }
                                                    terrainTextureSettings.ConcaveMode = ConcaveMode.Blended;
                                                    terrainTextureSettings.ConcaveMinHeightDifference = lbTerrainTexture.curvatureMinHeightDiff;
                                                    terrainTextureSettings.ConcaveAverage = true;
                                                    terrainTextureSettings.ConcaveDistance = lbTerrainTexture.curvatureDistance;
                                                }
                                                else
                                                {
                                                    terrainTextureSettings.ConcaveEnable = false;
                                                    terrainTextureSettings.ConvexEnable = false;
                                                }
                                            }
                                        }
                                    }
                                    // Process extra textures added by Vegetation Studio
                                    // Set their weight to 0 so they are ignored.
                                    else
                                    {
                                        if (numTexInfos > txIdx)
                                        {
                                            terrainTextureInfo = vegetationPackage.TerrainTextureList[txIdx];
                                            terrainTextureInfo.TileSize = Vector2.one * 10f;
                                        }

                                        if (numTexSettings > txIdx)
                                        {
                                            terrainTextureSettings = vegetationPackage.TerrainTextureSettingsList[txIdx];
                                            terrainTextureSettings.TextureWeight = 0f;
                                        }
                                    }
                                }
        #endregion

        #endregion

                                isSuccessful = true;
                            }
                        }
                    }
                }
            }

            return isSuccessful;
        }

        /// <summary>
        /// Import the Unity Terrain grass into Vegetation Studio Pro.
        /// Cannot use persistent storage...
        /// Cannot make use of Maps and Stencils for placement....
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static bool VegetationStudioProImportGrass(LBLandscape landscape, bool showErrors)
        {
            bool isSuccessful = false;

            string methodName = "LBIntegration.VegetationStudioImportGrass";
            VegetationStudioManager vegetationStudioManager = FindObjectOfType<VegetationStudioManager>();

            if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscape cannot be null"); } }
            else if (!IsVegetationStudioProInstalled()) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - Vegetation Studio Pro 0.8.0.0 or newer does not seem to be installed in the project."); } }
            else if (vegetationStudioManager == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not find Vegetation Studio Manager in the scene."); } }
            else if (landscape.landscapeTerrains == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscape.landscapeTerrains cannot be null"); } }
            else
            {
                // Vegetation Studio Manager exists, so find the child VegetationSystemPro object
                VegetationSystemPro vsPro = vegetationStudioManager.GetComponentInChildren<VegetationSystemPro>();
                if (vsPro == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not get type VegetationSystemPro. PLEASE REPORT"); } }
                else
                {
                    // Get the persistent storage package from the component (script)
                    PersistentVegetationStorage persistentVegetationStorage = vsPro.GetComponent<PersistentVegetationStorage>();
                    if (persistentVegetationStorage == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not get PersistentVegetationStorage component. PLEASE REPORT"); } }
                    else
                    {
                        PersistentVegetationStoragePackage pvstoragePackage = persistentVegetationStorage.PersistentVegetationStoragePackage;
                        if (pvstoragePackage != null)
                        {
                            // Get the Vegetation Package (Biome) for this landscape used by LB.
                            VegetationPackagePro vegetationPackage = vsPro.VegetationPackageProList.Find(vp => vp.PackageName.StartsWith("LB " + landscape.name));

                            if (vegetationPackage == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not find VegetationPackage (Biome) called " + "LB " + landscape.name); } }
                            else
                            {
                                VegetationItemInfoPro vegetationItemInfoPro = null;
                                int numVegInfos = (vegetationPackage.VegetationInfoList == null ? 0 : vegetationPackage.VegetationInfoList.Count);
                                byte vegetationSourceID = GetVegetationStudioSourceID;

        #region Remove Existing Grass instances

                                // Loop through all the Veg Item Infos. These could be tree types or grass types etc. Similar to LBTerrainTree, LBTerrainGrass etc
                                for (int vInfoIdx = 0; vInfoIdx < numVegInfos; vInfoIdx++)
                                {
                                    vegetationItemInfoPro = vegetationPackage.VegetationInfoList[vInfoIdx];
                                    if (vegetationItemInfoPro != null && vegetationItemInfoPro.VegetationType == VegetationType.Grass)
                                    {
                                        pvstoragePackage.RemoveVegetationItemInstances(vegetationItemInfoPro.VegetationItemID, vegetationSourceID);
                                    }
                                }

        #endregion

                                LBTerrainGrass lbTerrainGrass;

        #region Add/Replace Grass types in vegetation package

                                List<LBTerrainGrass> enabledGrassTerrainList = landscape.terrainGrassList.FindAll(gr => !gr.isDisabled && (gr.texture != null || (gr.useMeshPrefab && gr.meshPrefab != null)));

                                // How many enabled grass types do we have in the landscape?
                                int numLBGrass = (enabledGrassTerrainList == null ? 0 : enabledGrassTerrainList.Count);

                                // Check all the existing grass types in Vegetation Studio
                                // Remove any that aren't currently configured in the LB Grass tab
                                // Remove any grasses that useMesh or prefab/texture names has changed
                                bool isRemoveVegInfo = false;

                                for (int vpkIdx = numVegInfos - 1; vpkIdx >= 0; vpkIdx--)
                                {
                                    isRemoveVegInfo = false;
                                    vegetationItemInfoPro = vegetationPackage.VegetationInfoList[vpkIdx];

                                    //Debug.Log("[DEBUG] VegInfo: " + vegetationItemInfoPro.Name + " " + vegetationItemInfoPro.VegetationItemID);

                                    // Only process Grass
                                    if (vegetationItemInfoPro.VegetationType == VegetationType.Grass)
                                    {
                                        // Is this item in the LB Grass tab list?
                                        lbTerrainGrass = enabledGrassTerrainList.Find(gr => gr.GUID == vegetationItemInfoPro.VegetationItemID);
                                        if (lbTerrainGrass != null)
                                        {
                                            //Debug.Log("[DEBUG] Found Grass " + vegetationItemInfoPro.Name + " " + lbTerrainGrass.GUID + " " + vegetationItemInfoPro.PrefabType + " txtname: " + lbTerrainGrass.textureName);

                                            // Has useMesh or prefab/texture changed?
                                            if (vegetationItemInfoPro.PrefabType == VegetationPrefabType.Mesh && (!lbTerrainGrass.useMeshPrefab || vegetationItemInfoPro.Name != lbTerrainGrass.meshPrefab.name) ||
                                                vegetationItemInfoPro.PrefabType == VegetationPrefabType.Texture && (lbTerrainGrass.useMeshPrefab || vegetationItemInfoPro.Name != lbTerrainGrass.texture.name)
                                                )
                                            {
                                                isRemoveVegInfo = true;
                                            }
                                        }
                                        else { isRemoveVegInfo = true; }

                                        if (isRemoveVegInfo)
                                        {
                                            //Debug.Log("[DEBUG] Removing Vegetation Info: " + vegetationItemInfoPro.Name + " " + vegetationItemInfoPro.VegetationItemID);
                                            vegetationPackage.VegetationInfoList.Remove(vegetationItemInfoPro);
                                        }
                                    }
                                }

                                float terrainHeight = landscape.GetLandscapeTerrainHeight();
                                LBFilter lbFilter = null;
                                TerrainTextureRule terrainTextureRule = null;

                                List<LBTerrainTexture> enabledTextureTerrainList = landscape.terrainTexturesList.FindAll(tx => !tx.isDisabled && tx.texture != null);
                                int numLBTexs = (enabledTextureTerrainList == null ? 0 : enabledTextureTerrainList.Count);
                                int numTexInfos = (vegetationPackage.TerrainTextureList == null ? 0 : vegetationPackage.TerrainTextureList.Count);

                                for (int grIdx = 0; grIdx < numLBGrass; grIdx++)
                                {
                                    lbTerrainGrass = enabledGrassTerrainList[grIdx];

                                    // Do we need to update an existing grass or add a new one?
                                    vegetationItemInfoPro = vegetationPackage.GetVegetationInfo(lbTerrainGrass.GUID);

        #region Add the Grass Type to VS Pro as an VegetationItem
                                    if (vegetationItemInfoPro == null)
                                    {
                                        if (lbTerrainGrass.useMeshPrefab)
                                        {
                                            // Add the grass to the vegetation package. Supply our own GUID for easy reference later.
                                            vegetationPackage.AddVegetationItem(lbTerrainGrass.meshPrefab, VegetationType.Grass, false, lbTerrainGrass.GUID);
                                        }
                                        else
                                        {
                                            vegetationPackage.AddVegetationItem(lbTerrainGrass.texture, VegetationType.Grass, false, lbTerrainGrass.GUID);
                                        }

                                        // Retrieve the new grass item we just added
                                        vegetationItemInfoPro = vegetationPackage.GetVegetationInfo(lbTerrainGrass.GUID);
                                    }
        #endregion

        #region Update basic grass information
                                    if (vegetationItemInfoPro != null)
                                    {
                                        vegetationItemInfoPro.YScale = lbTerrainGrass.maxWidth;

                                        // Enable - cannot use persistent storage data for detail density
                                        vegetationItemInfoPro.EnableRuntimeSpawn = true;

                                        //vegetationItemInfoPro.IncludeDetailLayer = grIdx;
                                        //vegetationItemInfoPro.UseIncludeDetailMaskRules = true;

                                        // DistanceFallOff set to 1.0 per instance. For grass it is auto calculated by VS Pro to 0.4 and 1.0.
                                        // It is designed to allow some of the grass patches to have a shorter visible distance to get a view point dependent reduction
                                        // of grass in the distance.  For 1st person games where you see the terrain from ground level the default setting will make
                                        // sure all grass patches have at least 0.4 for all of them to show the first 40% and then a linear scale to only a few at 1 (100%)
                                        // It only applies if vegetationItemInfoPro.UseDistanceFalloff.
                                        vegetationItemInfoPro.UseDistanceFalloff = true;

                                        // LB Grass density can be from 1 (min density) to 15 (max density) - and is given as a range which is randomised per patch.
                                        // LB Grass density does support a range between 0 and 15, but VSP only has a single value.
                                        // VSP doesn't have a similar concept - instead it has SampleDistance.
                                        // SampleDistance can be from 0.4 (max density) to 50 (min density)

                                        // 50 gives a very low density. Change this to 2 metres.
                                        // SampleDistance = ((1f - (((float)lbTerrainGrass.density - 1f) / 14f)) * (50f - 0.4f)) + 0.4f;
                                        vegetationItemInfoPro.SampleDistance = ((1f - (((float)lbTerrainGrass.density - 1f) / 14f)) * (2f - 0.4f)) + 0.4f;

                                        vegetationItemInfoPro.UseNoiseCutoff = lbTerrainGrass.useNoise;
                                        vegetationItemInfoPro.NoiseCutoffValue = 1f - lbTerrainGrass.grassPlacementCutoff;  // in VSP 0 is no cutoff
                                        vegetationItemInfoPro.NoiseCutoffInverse = false;
                                        
                                        vegetationItemInfoPro.UseNoiseDensity = lbTerrainGrass.useNoise;
                                        vegetationItemInfoPro.NoiseDensityScale = lbTerrainGrass.noiseTileSize;

                                        // Scaling (currently VSP does not support separate width scale)
                                        vegetationItemInfoPro.UseNoiseScaleRule = false;
                                        vegetationItemInfoPro.MinScale = lbTerrainGrass.minHeight;
                                        vegetationItemInfoPro.MaxScale = lbTerrainGrass.maxHeight;
                                        vegetationItemInfoPro.ScaleMultiplier.y = 1f;

                                        // Scale the width
                                        vegetationItemInfoPro.ScaleMultiplier.x = (1f / lbTerrainGrass.minHeight) * lbTerrainGrass.minWidth;
                                        vegetationItemInfoPro.ScaleMultiplier.z = (1f / lbTerrainGrass.maxHeight) * lbTerrainGrass.maxWidth;

                                        // Potentially set "TintColor1", "Dry color tint" and "TintColor2", "healty color tint"
                                        //vegetationItemInfoPro.ShaderControllerSettings.

                                        // Get the min/max Height and min/max Inclination (slope or steepness)
                                        float minHeight = 0f, maxHeight = terrainHeight;
                                        float minInclination = 0f, maxInclination = 90f;

                                        if (lbTerrainGrass.grassPlacingMode == LBTerrainGrass.GrassPlacingMode.HeightAndInclination ||
                                            lbTerrainGrass.grassPlacingMode == LBTerrainGrass.GrassPlacingMode.HeightInclinationCurvature ||
                                            lbTerrainGrass.grassPlacingMode == LBTerrainGrass.GrassPlacingMode.HeightInclinationMap)
                                        {
                                            minHeight = lbTerrainGrass.minPopulatedHeight * terrainHeight;
                                            maxHeight = lbTerrainGrass.maxPopulatedHeight * terrainHeight;
                                            minInclination = lbTerrainGrass.minInclination;
                                            maxInclination = lbTerrainGrass.maxInclination;
                                        }
                                        else if (lbTerrainGrass.grassPlacingMode == LBTerrainGrass.GrassPlacingMode.Height)
                                        {
                                            minHeight = lbTerrainGrass.minPopulatedHeight * terrainHeight;
                                            maxHeight = lbTerrainGrass.maxPopulatedHeight * terrainHeight;
                                            minInclination = 0f;
                                            maxInclination = 90f;
                                        }
                                        else if (lbTerrainGrass.grassPlacingMode == LBTerrainGrass.GrassPlacingMode.Inclination)
                                        {
                                            minHeight = 0f;
                                            maxHeight = terrainHeight;
                                            minInclination = lbTerrainGrass.minInclination;
                                            maxInclination = lbTerrainGrass.maxInclination;
                                        }
                                        else
                                        {
                                            minHeight = 0f;
                                            maxHeight = terrainHeight;
                                            minInclination = 0f;
                                            maxInclination = 90f;
                                        }

                                        vegetationItemInfoPro.UseHeightRule = true;
                                        vegetationItemInfoPro.UseSteepnessRule = true;

                                        vegetationItemInfoPro.UseAdvancedHeightRule = false;
                                        vegetationItemInfoPro.MinHeight = minHeight;
                                        vegetationItemInfoPro.MaxHeight = maxHeight;

                                        vegetationItemInfoPro.UseAdvancedSteepnessRule = false;
                                        vegetationItemInfoPro.MinSteepness = minInclination;
                                        vegetationItemInfoPro.MaxSteepness = maxInclination;

                                        vegetationItemInfoPro.UseConcaveLocationRule = (lbTerrainGrass.grassPlacingMode == LBTerrainGrass.GrassPlacingMode.HeightInclinationCurvature);
                                        vegetationItemInfoPro.ConcaveLoactionAverage = true;
                                        vegetationItemInfoPro.ConcaveLoactionDistance = lbTerrainGrass.curvatureDistance;
                                        vegetationItemInfoPro.ConcaveLoactionMinHeightDifference= lbTerrainGrass.curvatureMinHeightDiff;
                                    }
        #endregion

                                    // NOTE Area and Stencil filters are not supported
        #region Apply Texture Filters using VS Pro Terrain Texture Rules

                                    int numFilters = (lbTerrainGrass.filterList == null ? 0 : lbTerrainGrass.filterList.Count);

                                    // Reset filters
                                    vegetationItemInfoPro.UseTerrainTextureExcludeRules = false;
                                    vegetationItemInfoPro.UseTerrainTextureIncludeRules = false;
                                    vegetationItemInfoPro.TerrainTextureExcludeRuleList.Clear();
                                    vegetationItemInfoPro.TerrainTextureIncludeRuleList.Clear();

                                    for (int fIdx = 0; fIdx < numFilters; fIdx++)
                                    {
                                        lbFilter = lbTerrainGrass.filterList[fIdx];
                                        if (lbFilter != null && lbFilter.filterType == LBFilter.FilterType.Texture)
                                        {
                                            int textureIndex = -1;

                                            // Look up the closest matching texture
                                            if (numTexInfos > 0)
                                            {
                                                if (numTexInfos >= numLBTexs)
                                                {
                                                    //Debug.Log("Checking filter for grass " + lbTerrainGrass.texture + " " + lbFilter.terrainTexture.texture.name);

                                                    // First try to find a match in LB's Textures
                                                    textureIndex = enabledTextureTerrainList.FindIndex(ftx => ftx.texture == lbFilter.terrainTexture.texture &&
                                                                                                       ftx.normalMap == lbFilter.terrainTexture.normalMap &&
                                                                                                       ftx.metallic == lbFilter.terrainTexture.metallic &&
                                                                                                       ftx.smoothness == lbFilter.terrainTexture.smoothness &&
                                                                                                       ftx.tileSize == lbFilter.terrainTexture.tileSize);
                                                    // Then try to match that texture with the one in the same position in VS Pro
                                                    if (textureIndex >= 0)
                                                    {
                                                        

                                                        TerrainTextureInfo textureInfo = vegetationPackage.TerrainTextureList[textureIndex];
                                                        LBTerrainTexture lbTerrainTexture = enabledTextureTerrainList[textureIndex];

                                                        // Check if it looks like a match
                                                        if (!(textureInfo != null && textureInfo.Texture != null && textureInfo.Texture.name == lbTerrainTexture.texture.name))
                                                        {
                                                            // The texture in LB Texturing tab, cannot be located in VS Pro. It is likely
                                                            // that LB Texturing hasn't been applied yet after VS Pro was enabled for this landscape.
                                                            textureIndex = -1;
                                                        }

                                                        //Debug.Log("  Found " + lbFilter.terrainTexture.texture.name + " textureIndex: " + textureIndex);
                                                    }
                                                }
                                                
                                                // If we didn't find a match try a find a likely candidate in VS Pro's existing Textures
                                                if (textureIndex < 0)
                                                {
                                                    textureIndex = vegetationPackage.TerrainTextureList.FindIndex(ftx => ftx.Texture == lbFilter.terrainTexture.texture &&
                                                                                                                  ftx.TextureNormals == lbFilter.terrainTexture.normalMap &&
                                                                                                                  ftx.TileSize == lbFilter.terrainTexture.tileSize);
                                                }
                                            }

                                            // If we found a likely texture in VS Pro for this LB Texture filter,
                                            // create a rule.
                                            if (textureIndex >= 0)
                                            {
                                                if (lbFilter.filterMode == LBFilter.FilterMode.NOT)
                                                {
                                                    vegetationItemInfoPro.UseTerrainTextureExcludeRules = true;
                                                    terrainTextureRule = new TerrainTextureRule();
                                                    terrainTextureRule.TextureIndex = textureIndex;
                                                    terrainTextureRule.MinimumValue = 0.1f;
                                                    terrainTextureRule.MaximumValue = 1.0f;
                                                    vegetationItemInfoPro.TerrainTextureExcludeRuleList.Add(terrainTextureRule);
                                                }
                                                // Currently all VSP rules are OR. But we'll include LB 'AND' filters too as LB only supports AND + NOT Texture filters
                                                else
                                                {
                                                    vegetationItemInfoPro.UseTerrainTextureIncludeRules = true;
                                                    terrainTextureRule = new TerrainTextureRule();
                                                    terrainTextureRule.TextureIndex = textureIndex;
                                                    terrainTextureRule.MinimumValue = 0.5f;
                                                    terrainTextureRule.MaximumValue = 1.0f;
                                                    vegetationItemInfoPro.TerrainTextureIncludeRuleList.Add(terrainTextureRule);
                                                }
                                            }
                                        }
                                    }

        #endregion
                                }

        #endregion

                                // CURRENTLY NOT IMPLEMENTED - not sure if we can
        #region Add Grass from Terrains to Persistant Storage Package (EDITOR ONLY)

                                //int numTerrains = (landscape.landscapeTerrains == null ? 0 : landscape.landscapeTerrains.Length);

                                //Terrain terrain = null;
                                //TerrainData tData = null;
                                //List<LBTerrainGrass> protoTypeGrassList = new List<LBTerrainGrass>();
                                //int numDetailPrototypes = 0;

                                //// If there is grass in the Grass Tab, instruct Vegetation Studio to load it.
                                //for (int tIdx = 0; tIdx < numTerrains; tIdx++)
                                //{
                                //    terrain = landscape.landscapeTerrains[tIdx];

                                //    if (terrain != null)
                                //    {
                                //        tData = terrain.terrainData;
                                //        if (tData != null)
                                //        {
                                //            // Get the detail prototypes for the terrain.
                                //            LBTerrainGrass.ToLBTerrainGrassList(tData.detailPrototypes, protoTypeGrassList);
                                //            numDetailPrototypes = (protoTypeGrassList == null ? 0 : protoTypeGrassList.Count);
                                //        }
                                //    }
                                //}

        #endregion

                                vsPro.RefreshVegetationSystem();
                                isSuccessful = true;
                            }
                        }
                    }
                }
            }

            return isSuccessful;
        }

        /// <summary>
        /// Import the Unity Terrain trees into Vegetation Studio Pro.
        /// Remove any existing trees.
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static bool VegetationStudioProImportTrees(LBLandscape landscape, bool showErrors)
        {
            bool isSuccessful = false;

            string methodName = "LBIntegration.VegetationStudioImportTrees";
            VegetationStudioManager vegetationStudioManager = FindObjectOfType<VegetationStudioManager>();

            if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscape cannot be null"); } }
            else if (!IsVegetationStudioProInstalled()) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - Vegetation Studio Pro 0.8.0.0 or newer does not seem to be installed in the project."); } }
            else if (vegetationStudioManager == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not find Vegetation Studio Manager in the scene."); } }
            else if (landscape.landscapeTerrains == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscape.landscapeTerrains cannot be null"); } }
            else
            {
                // Vegetation Studio Manager exists, so find the child VegetationSystemPro object
                VegetationSystemPro vsPro = vegetationStudioManager.GetComponentInChildren<VegetationSystemPro>();
                if (vsPro == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not get type VegetationSystemPro. PLEASE REPORT"); } }
                else
                {
                    // Get the persistent storage package from the component (script)
                    PersistentVegetationStorage persistentVegetationStorage = vsPro.GetComponent<PersistentVegetationStorage>();
                    if (persistentVegetationStorage == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not get PersistentVegetationStorage component. PLEASE REPORT"); } }
                    else
                    {
                        PersistentVegetationStoragePackage pvstoragePackage = persistentVegetationStorage.PersistentVegetationStoragePackage;
                        if (pvstoragePackage != null)
                        {
                            // Get the Vegetation Package (Biome) for this landscape used by LB.
                            VegetationPackagePro vegetationPackage = vsPro.VegetationPackageProList.Find(vp => vp.PackageName.StartsWith("LB " + landscape.name));

                            if (vegetationPackage == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not find VegetationPackage (Biome) called " + "LB " + landscape.name); } }
                            else
                            {
                                LBTerrainTree lbTerrainTree;

                                VegetationItemInfoPro vegetationItemInfoPro = null;
                                int numVegInfos = (vegetationPackage.VegetationInfoList == null ? 0 : vegetationPackage.VegetationInfoList.Count);
                                byte vegetationSourceID = GetVegetationStudioSourceID;

        #region Remove Existing Tree instances

                                // Loop through all the Veg Item Infos. These could be tree types or grass types etc. Similar to LBTerrainTree, LBTerrainGrass etc
                                for (int vInfoIdx = 0; vInfoIdx < numVegInfos; vInfoIdx++)
                                {
                                    vegetationItemInfoPro = vegetationPackage.VegetationInfoList[vInfoIdx];
                                    if (vegetationItemInfoPro != null && vegetationItemInfoPro.VegetationType == VegetationType.Tree)
                                    {
                                        pvstoragePackage.RemoveVegetationItemInstances(vegetationItemInfoPro.VegetationItemID, vegetationSourceID);
                                    }
                                }

        #endregion

        #region Add/Replace Tree types in vegetation package
                                List<LBTerrainTree> enabledTreeTerrainList = landscape.terrainTreesList.FindAll(tr => !tr.isDisabled && tr.prefab != null);

                                // How many enabled tree types do we have in the landscape?
                                int numLBTree = (enabledTreeTerrainList == null ? 0 : enabledTreeTerrainList.Count);

                                // Check all the existing tree types in Vegetation Studio
                                // Remove any that aren't currently configured in the LB Tree tab
                                // Remove any trees that useMesh or prefab/texture names has changed
                                bool isRemoveVegInfo = false;


                                for (int vInfoIdx = numVegInfos - 1; vInfoIdx >= 0; vInfoIdx--)
                                {
                                    isRemoveVegInfo = false;
                                    vegetationItemInfoPro = vegetationPackage.VegetationInfoList[vInfoIdx];

                                    //Debug.Log("[DEBUG] VegInfo: " + vegetationItemInfoPro.Name + " " + vegetationItemInfoPro.VegetationItemID);

                                    // Only process Trees
                                    if (vegetationItemInfoPro.VegetationType == VegetationType.Tree)
                                    {
                                        // Is this item in the LB Tree tab list?
                                        lbTerrainTree = enabledTreeTerrainList.Find(gr => gr.GUID == vegetationItemInfoPro.VegetationItemID);
                                        if (lbTerrainTree != null)
                                        {
                                            //Debug.Log("[DEBUG] Found Tree " + vegetationItemInfoPro.Name + " " + lbTerrainTree.GUID + " " + " prefabname: " + lbTerrainTree.prefab.name);

                                            // Has prefab changed?
                                            if (vegetationItemInfoPro.Name != lbTerrainTree.prefab.name) { isRemoveVegInfo = true; }
                                        }
                                        else { isRemoveVegInfo = true; }

                                        if (isRemoveVegInfo)
                                        {
                                            //Debug.Log("Removing Vegetation Info: " + vegetationItemInfoPro.Name + " " + vegetationItemInfoPro.VegetationItemID);
                                            vegetationPackage.VegetationInfoList.Remove(vegetationItemInfoPro);
                                        }
                                    }
                                }

                                // Add Tree Types to Vegetation Package (Biome)
                                for (int tIdx = 0; tIdx < numLBTree; tIdx++)
                                {
                                    lbTerrainTree = enabledTreeTerrainList[tIdx];

                                    // Do we need to update an existing tree or add a new one?
                                    vegetationItemInfoPro = vegetationPackage.GetVegetationInfo(lbTerrainTree.GUID);

                                    if (vegetationItemInfoPro == null)
                                    {
                                        // Add the tree to the vegetation package. Supply our own GUID for easy reference later.
                                        vegetationPackage.AddVegetationItem(lbTerrainTree.prefab, VegetationType.Tree, false, lbTerrainTree.GUID);
                                    }
                                }

        #endregion

        #region Add Trees from Terrains to Persistant Storage Package (EDITOR ONLY)

                                int numTerrains = (landscape.landscapeTerrains == null ? 0 : landscape.landscapeTerrains.Length);
                                int numTreesInTerrain = 0;
                                int numTreePrototypes = 0;
                                int numTreesForPrototype = 0;
                                Terrain terrain = null;
                                TerrainData tData = null;
                                Vector3 terrainPosition = Vector3.zero;
                                Vector3 terrainSize = Vector3.zero;
                                Vector3 treePosition = Vector3.zero;
                                List<TreeInstance> treeInstanceList = new List<TreeInstance>();
                                List<TreeInstance> treeInstanceSubsetList = new List<TreeInstance>();
                                List<LBTerrainTree> protoTypeTreesList = new List<LBTerrainTree>();
                                LBTerrainTree lbTerrainTreeMatch = null;

                                for (int trnIdx = 0; trnIdx < numTerrains; trnIdx++)
                                {
                                    terrain = landscape.landscapeTerrains[trnIdx];
                                    if (terrain != null)
                                    {
                                        tData = terrain.terrainData;
                                        if (tData != null)
                                        {
                                            terrainPosition = terrain.transform.position;
                                            terrainSize = tData.size;

                                            // Get the tree prototypes for the first terrain. Assumes all terrains have the same
                                            // trees that were created with LB.
                                            LBTerrainTree.ToLBTerrainTreeList(tData.treePrototypes, protoTypeTreesList);
                                            numTreePrototypes = (protoTypeTreesList == null ? 0 : protoTypeTreesList.Count);
                                            numTreesInTerrain = tData.treeInstanceCount;

                                            if (numTreesInTerrain > 0) { treeInstanceList.AddRange(tData.treeInstances); }

                                            // Loop through all the tree prototypes in the terrain.
                                            for (int tpIdx = 0; tpIdx < numTreePrototypes; tpIdx++)
                                            {
                                                lbTerrainTree = protoTypeTreesList[tpIdx];

                                                // Find the first matching prototype in the LBTerrainTree list in the landscape (if any)
                                                lbTerrainTreeMatch = landscape.terrainTreesList.Find(tp => !tp.isDisabled && tp.prefab != null && tp.prefab == lbTerrainTree.prefab && tp.bendFactor == lbTerrainTree.bendFactor);

                                                if (lbTerrainTreeMatch == null) { Debug.Log("INFO: " + methodName + " skipping tree prototype " + (lbTerrainTree.prefab == null ? "no prefab" : lbTerrainTree.prefab.name) + " because it is not in the Landscape Builder Tree tab list"); }
                                                else
                                                {
                                                    // Get tree instances for this prototype which is in the Landscape Tree types list
                                                    treeInstanceSubsetList.AddRange(treeInstanceList.FindAll(ti => ti.prototypeIndex == tpIdx));
                                                    numTreesForPrototype = treeInstanceSubsetList.Count;

                                                    //Debug.Log("[DEBUG] terrain: " + terrain.name + " prototype: " + protoTypeTreesList[tpIdx].prefab.name + " trees: " + numTreesForPrototype);

                                                    // Process all the trees for this treeprototype in the current terrain
                                                    for (int trIdx = 0; trIdx < numTreesForPrototype; trIdx++)
                                                    {
                                                        TreeInstance treeInstance = treeInstanceSubsetList[trIdx];

                                                        treePosition.x = treeInstance.position.x * terrainSize.x;
                                                        treePosition.y = treeInstance.position.y * terrainSize.y;
                                                        treePosition.z = treeInstance.position.z * terrainSize.z;
                                                        treePosition += terrainPosition;

                                                        persistentVegetationStorage.AddVegetationItemInstance
                                                        (lbTerrainTreeMatch.GUID, treePosition,
                                                          new Vector3(treeInstance.widthScale, treeInstance.heightScale, treeInstance.widthScale),
                                                          Quaternion.Euler(0, treeInstance.rotation * Mathf.Rad2Deg, 0), true, vegetationSourceID, 1.0f
                                                        );
                                                    }
                                                }

                                                treeInstanceSubsetList.Clear();
                                            }
                                        }
                                    }

                                    // Clear lists
                                    treeInstanceList.Clear();
                                }

        #endregion

#if UNITY_EDITOR
                                UnityEditor.EditorUtility.SetDirty(vegetationPackage);
#endif
                                vsPro.RefreshVegetationSystem();

                                isSuccessful = true;
                            }
                        }
                    }
                }
            }

            return isSuccessful;
        }

        /// <summary>
        /// When the topography changes, the vegetation must be refreshed.
        /// </summary>
        /// <param name="showErrors"></param>
        public static void VegetationStudioProRefresh(bool showErrors)
        {
            string methodName = "LBIntegration.VegetationStudioProRefresh";
            VegetationStudioManager vegetationStudioManager = FindObjectOfType<VegetationStudioManager>();

            if (!IsVegetationStudioProInstalled()) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - Vegetation Studio Pro 1.1.0.0 or newer does not seem to be installed in the project."); } }
            else if (vegetationStudioManager == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not find Vegetation Studio Manager in the scene."); } }
            else
            {
                // Vegetation Studio Manager exists, so find the child VegetationSystemPro object
                VegetationSystemPro vsPro = vegetationStudioManager.GetComponentInChildren<VegetationSystemPro>();
                if (vsPro == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not get type VegetationSystemPro. PLEASE REPORT"); } }
                else
                {
                    vsPro.ClearCache();
                    vsPro.RefreshTerrainHeightmap();
#if UNITY_EDITOR
                    UnityEditor.SceneView.RepaintAll();
#endif
                }
            }
        }

        public static AwesomeTechnologies.VegetationSystem.Biomes.BiomeMaskArea VegetationStudioProCreateBiomeMaskArea
        (VegetationSystemPro vsPro, string maskName, Transform parentTrfrm, LBBiome lbBiome)
        {
            AwesomeTechnologies.VegetationSystem.Biomes.BiomeMaskArea biomeMaskArea = null;

            GameObject vspBiomeMaskGameObject = new GameObject(maskName);
            if (vspBiomeMaskGameObject != null)
            {
                vspBiomeMaskGameObject.transform.SetParent(parentTrfrm);
                LBPrefabItem lbPrefabItem = vspBiomeMaskGameObject.AddComponent<LBPrefabItem>();
                if (lbPrefabItem != null)
                {
                    lbPrefabItem.prefabItemType = LBPrefabItem.PrefabItemType.VSProBiomeMaskArea;
                    biomeMaskArea = vspBiomeMaskGameObject.AddComponent<AwesomeTechnologies.VegetationSystem.Biomes.BiomeMaskArea>();
                    if (biomeMaskArea != null)
                    {
                        // Set default values
                        biomeMaskArea.MaskName = maskName;
                        biomeMaskArea.ShowHandles = false;
                        if (lbBiome != null) { biomeMaskArea.BlendDistance = lbBiome.minBlendDist; }

                        // Find the BiomeType which the enumeration of different biome "labels" or "types". Multiple Biomes (packages)
                        // can have the same BiomeType.
                        if (vsPro != null)
                        {
                            int numBiomes = vsPro.VegetationPackageProList == null ? 0 : vsPro.VegetationPackageProList.Count;
                            if (lbBiome != null && lbBiome.biomeIndex >= 0 && lbBiome.biomeIndex < numBiomes) { biomeMaskArea.BiomeType = vsPro.VegetationPackageProList[lbBiome.biomeIndex].BiomeType; }
                            else { biomeMaskArea.BiomeType = BiomeType.Default; }
                        }
                    }
                }
            }

            return biomeMaskArea;
        }

        public static AwesomeTechnologies.VegetationMaskArea VegetationStudioProCreateVegMaskArea(string maskName, Transform parentTrfrm)
        {
            AwesomeTechnologies.VegetationMaskArea vegMaskArea = null;

            GameObject vspVegMaskGameObject = new GameObject(maskName);
            if (vspVegMaskGameObject != null)
            {
                vspVegMaskGameObject.transform.SetParent(parentTrfrm);
                LBPrefabItem lbPrefabItem = vspVegMaskGameObject.AddComponent<LBPrefabItem>();
                if (lbPrefabItem != null)
                {
                    lbPrefabItem.prefabItemType = LBPrefabItem.PrefabItemType.VSProVegMaskArea;
                    vegMaskArea = vspVegMaskGameObject.AddComponent<AwesomeTechnologies.VegetationMaskArea>();
                    if (vegMaskArea != null)
                    {
                        // Set default values
                        vegMaskArea.MaskName = maskName;
                        vegMaskArea.ShowHandles = false;
                    }
                }
            }

            return vegMaskArea;
        }

        /// <summary>
        /// Get a list of all packages (biomes) available in the scene for display in a popup (dropdown)
        /// </summary>
        /// <param name="vsPro"></param>
        /// <returns></returns>
        public static string[] VegetationStudioProGetPackageList(VegetationSystemPro vsPro)
        {
            if (vsPro == null || vsPro.VegetationPackageProList == null || vsPro.VegetationPackageProList.Count == 0) { return new string[] { "None" }; }
            else
            {
                string[] packageNameList = new string[vsPro.VegetationPackageProList.Count];
                for (int i = 0; i < vsPro.VegetationPackageProList.Count; i++)
                {
                    if (vsPro.VegetationPackageProList[i])
                    {
                        packageNameList[i] = (i + 1).ToString() + " " +
                                             vsPro.VegetationPackageProList[i].PackageName + " (" + vsPro.VegetationPackageProList[i].BiomeType.ToString() + ")";
                    }
                    else
                    {
                        packageNameList[i] = "Not found";
                    }
                }
                return packageNameList;
            }
        }

#endif

        #endregion

        #region AQUAS Integration

        /// <summary>
        /// Check to see if AQUAS Water Set or AQUAS Lite is installed in the project
        /// </summary>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static bool isAQUASInstalled(bool showErrors)
        {
            bool isInstalled = false;

            System.Type aquasCameraType = null;

            try
            {
                aquasCameraType = System.Type.GetType("AQUAS_Camera, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", true, true);
                if (aquasCameraType != null) { isInstalled = true; }
                aquasCameraType = null;
            }
            catch
            {
                if (showErrors) { Debug.LogWarning("LBIntegration.isAQUASInstalled: it appears that AQUAS is not installed in this project."); }
            }

            return isInstalled;
        }

        /// <summary>
        /// Checks to see if AQUAS River feature is installed in the project. This was new in AQUAS v1.3
        /// NOTE: This only checks for an Editor script so will always return false at runtime
        /// </summary>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static bool isAQUASRiverInstalled(bool showErrors)
        {
            bool isInstalled = false;

            System.Type aquasRiverType = null;

            try
            {
                aquasRiverType = System.Type.GetType("AQUAS_RiverSetup, Assembly-CSharp-Editor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", true, true);
                if (aquasRiverType != null) { isInstalled = true; }
                aquasRiverType = null;
            }
            catch
            {
                if (showErrors) { Debug.LogWarning("LBIntegration.isAQUASRiverInstalled: it appears that AQUAS v1.3 or newer is not installed in this project."); }
            }

            return isInstalled;
        }

        /// <summary>
        /// Add AQUAS Water to the scene
        /// Typically called from LBWaterOperations.AddWaterToScene(LBWaterParameters lbWaterParms, ref int numberOfMeshesToCreate)
        /// </summary>
        /// <param name="lbWaterParms"></param>
        /// <param name="lbWater"></param>
        /// <param name="sizeX"></param>
        /// <param name="sizeZ"></param>
        public static void AddAQUASWaterToScene(LBWaterParameters lbWaterParms, LBWater lbWater, float sizeX, float sizeZ)
        {
            bool isRiverInstalled = isAQUASRiverInstalled(false);

            if (lbWaterParms == null) { Debug.LogWarning("LBIntegration.AddAQUASWaterToScene - lbWaterParms cannot be null"); }
            else if (lbWaterParms.waterTransform == null) { Debug.LogWarning("LBIntegration.AddAQUASWaterToScene - water transform cannot be null"); }
            else if (lbWaterParms.waterTransform.gameObject == null) { Debug.LogWarning("LBIntegration.AddAQUASWaterToScene - water gameobject cannot be null"); }
            {
                Renderer renderer = lbWaterParms.waterTransform.GetComponent<Renderer>();
                if (renderer == null) { Debug.LogWarning("LBIntegration.AddAQUASWaterToScene - water gameobject is missing the rendered"); }
                else
                {
                    // With AQUAS we always want to keep the aspect ratio
                    float scaleXYZ = Mathf.Max(lbWaterParms.waterSize.x / sizeX, lbWaterParms.waterSize.y / sizeZ);

                    lbWaterParms.waterTransform.localScale = scaleXYZ * Vector3.one;

                    if (isRiverInstalled && lbWaterParms.isRiver)
                    {
                        DestroyImmediate(lbWaterParms.waterTransform.GetComponent<MeshCollider>());
                        lbWaterParms.waterTransform.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        lbWaterParms.waterTransform.GetComponent<Renderer>().material = lbWaterParms.riverMaterial;
                    }

                    if (lbWaterParms.waterMainCamera == null)
                    {
                        Debug.LogWarning("LBIntegration - could not add AQUAS_Camera script to MainCamera.");
                    }
                    else
                    {
                        System.Type aquasCamerType = null;
                        System.Type aquasReflectionType = null;
                        System.Type aquasRenderQueueEditorType = null;
                        try
                        {
                            if (isRiverInstalled && lbWaterParms.isRiver)
                            {
                                aquasReflectionType = System.Type.GetType("AQUAS_Reflection, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", true, true);
                                aquasRenderQueueEditorType = System.Type.GetType("AQUAS_RenderQueueEditor, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", true, true);

                                if (aquasReflectionType != null) { lbWaterParms.waterTransform.gameObject.AddComponent(aquasReflectionType); }
                                if (aquasRenderQueueEditorType != null) { lbWaterParms.waterTransform.gameObject.AddComponent(aquasRenderQueueEditorType); }
                            }

                            // Attempt to find the AQUAS_Camera.cs script in the project
                            aquasCamerType = System.Type.GetType("AQUAS_Camera, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", true, true);
                            if (aquasCamerType == null)
                            {
                                Debug.LogError("AQUAS camera script could not be found in the project.");
                            }
                            else
                            {
                                // Check to see if the AQUAS_Camera script is already attached to the main camera
                                var aquasCameraComponent = lbWaterParms.waterMainCamera.gameObject.GetComponent(aquasCamerType);
                                // Only add the script if it isn't already attached.
                                if (aquasCameraComponent == null)
                                {
                                    lbWaterParms.waterMainCamera.gameObject.AddComponent(aquasCamerType);
                                }

                                // TODO investigate why LB Lighting Reflection Probe doesn't seem to be updating
                                //Check if Tenkoku is in the scene already
                                if (GameObject.Find("Tenkoku DynamicSky") == null)
                                {
                                    // AQUAS needs a reflection probe
                                    GameObject refProbe = new GameObject("Reflection Probe");
                                    refProbe.transform.position = lbWaterParms.waterPosition;
                                    refProbe.AddComponent<ReflectionProbe>();
                                    refProbe.GetComponent<ReflectionProbe>().intensity = 0.3f;
                                    refProbe.transform.SetParent(lbWaterParms.waterTransform);
                                    // Reset the local position so that the probe doesn't appear under the water
                                    refProbe.transform.localPosition = Vector3.zero;

                                    renderer.sharedMaterial.SetFloat("_EnableCustomFog", 1f);
                                    renderer.sharedMaterial.SetFloat("_Specular", 0.5f);
                                }
                                else
                                {
                                    renderer.sharedMaterial.SetFloat("_EnableCustomFog", 0f);
                                    renderer.sharedMaterial.SetFloat("_Specular", 1f);
                                }

                                // Add the caustic prefabs
                                if (lbWaterParms.waterCausticsPrefabList != null)
                                {
                                    if (lbWaterParms.waterCausticsPrefabList.Count > 0)
                                    {
                                        if (lbWaterParms.waterCausticsPrefabList[0] == null)
                                        {
                                            Debug.LogWarning("LBIntegration - could not add Primary Caustics prefab to water in scene");
                                        }
                                        else
                                        {
                                            Transform primaryCaustics = Instantiate(lbWaterParms.waterCausticsPrefabList[0]);
                                            primaryCaustics.position = lbWaterParms.waterTransform.position;
                                            primaryCaustics.name = "PrimaryCausticsProjector";

                                            Projector primaryProjector = primaryCaustics.GetComponent<Projector>();
                                            if (primaryProjector == null)
                                            {
                                                Debug.LogWarning("LBIntegration - Projector component is missing on Primary Caustics prefab");
                                            }
                                            else
                                            {
                                                primaryProjector.orthographicSize = (lbWater.waterSize.x / 2f);
                                                primaryCaustics.SetParent(lbWaterParms.waterTransform);

                                                // Create a copy of the material and store it in the scene
                                                primaryProjector.material = new Material(primaryProjector.material);
                                            }
                                        }
                                    }

                                    if (lbWaterParms.waterCausticsPrefabList.Count > 1)
                                    {
                                        if (lbWaterParms.waterCausticsPrefabList[1] == null)
                                        {
                                            Debug.LogWarning("LBIntegration - could not add Secondary Caustics prefab to water in scene");
                                        }
                                        else
                                        {
                                            Transform secondaryCaustics = Instantiate(lbWaterParms.waterCausticsPrefabList[1]);
                                            secondaryCaustics.position = lbWaterParms.waterTransform.position;
                                            secondaryCaustics.name = "SecondaryCausticsProjector";

                                            Projector secondaryProjector = secondaryCaustics.GetComponent<Projector>();
                                            if (secondaryProjector == null)
                                            {
                                                Debug.LogWarning("LBIntegration - Projector component is missing on Secondary Caustics prefab");
                                            }
                                            else
                                            {
                                                secondaryProjector.orthographicSize = (lbWater.waterSize.x / 2f);
                                                secondaryCaustics.SetParent(lbWaterParms.waterTransform);

                                                // Create a copy of the material and store it in the scene
                                                secondaryProjector.material = new Material(secondaryProjector.material);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning("LBIntegration. Something went wrong with AQUAS. Import AQUAS to use this feature. LB supports version 1.2.2 and newer" + ex.Message.ToString());
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Add the AQUAS scripts to the mesh transform.
        /// Typically called from LBWaterOperations.AddWaterToMesh()
        /// </summary>
        /// <param name="lbWaterParms"></param>
        /// <param name="lbWater"></param>
        public static bool AddAQUASWaterToMesh(LBWaterParameters lbWaterParms, LBWater lbWater)
        {
            bool isSuccess = false;
            bool isRiverInstalled = isAQUASRiverInstalled(false);

            // Basic Validation
            if (lbWaterParms == null) { Debug.LogWarning("LBIntegration.AddAQUASWaterToMesh - lbWaterParms cannot be null"); }
            else if (lbWaterParms.waterTransform == null) { Debug.LogWarning("LBIntegration.AddAQUASWaterToMesh - lbWaterParms.waterTransform cannot be null"); }
            else if (!isRiverInstalled)
            {
                Debug.LogWarning("LBIntegration.AddAQUASWaterToMesh requires AQUAS 1.3 or newer. Import AQUAS 1.3 or newer to use this feature.");
            }
            else
            {
                Renderer renderer = lbWaterParms.waterTransform.GetComponent<Renderer>();
                if (renderer == null) { Debug.LogWarning("LBIntegration.AddAQUASWaterToMesh - " + lbWaterParms.waterTransform.name + " is missing the rendered"); }
                else
                {
                    System.Type aquasReflectionType = null;
                    System.Type aquasRenderQueueEditorType = null;

                    try
                    {
                        if (isRiverInstalled && lbWaterParms.isRiver)
                        {
                            aquasReflectionType = System.Type.GetType("AQUAS_Reflection, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", true, true);
                            aquasRenderQueueEditorType = System.Type.GetType("AQUAS_RenderQueueEditor, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", true, true);

                            // Check to see if the AQUAS_Relection script is already attached to the mesh transform
                            var aquasReflectionComponent = lbWaterParms.waterTransform.GetComponent(aquasReflectionType);
                            // Only add the script if it isn't already attached.
                            if (aquasReflectionType != null && aquasReflectionComponent == null)
                            {
                                lbWaterParms.waterTransform.gameObject.AddComponent(aquasReflectionType);
                            }

                            // Check to see if the AQUAS_Render Queue Editor script is already attached to the mesh transform
                            var aquasRenderQueueEditorComponent = lbWaterParms.waterTransform.GetComponent(aquasRenderQueueEditorType);
                            // Only add the script if it isn't already attached.
                            if (aquasRenderQueueEditorType != null && aquasRenderQueueEditorComponent == null)
                            {
                                lbWaterParms.waterTransform.gameObject.AddComponent(aquasRenderQueueEditorType);
                            }

                            // Remove any mesh colliders from the river surface mesh
                            DestroyImmediate(lbWaterParms.waterTransform.GetComponent<MeshCollider>());
                            // The AQUAS River should not cast shadows
                            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                            //Check if Tenkoku is in the scene already
                            if (GameObject.Find("Tenkoku DynamicSky") == null)
                            {
                                GameObject refProbeGO;

                                // Create a new reflection probe Gameobject if it doesn't already exist
                                Transform refProbeTrfm = lbWaterParms.waterTransform.Find("Reflection Probe");
                                if (refProbeTrfm == null) { refProbeGO = new GameObject("Reflection Probe"); }
                                else { refProbeGO = refProbeTrfm.gameObject; }

                                // AQUAS needs a reflection probe
                                ReflectionProbe refProbe = refProbeGO.GetComponent<ReflectionProbe>();
                                if (refProbe == null) { refProbe = refProbeGO.AddComponent<ReflectionProbe>(); }

                                refProbeGO.transform.position = lbWaterParms.reflectionProbePosition;
                                refProbe.intensity = 0.3f;
                                refProbeGO.transform.SetParent(lbWaterParms.waterTransform);
                                // Reset the local position so that the probe doesn't appear under the water
                                //refProbe.transform.localPosition = Vector3.zero;

                                renderer.sharedMaterial.SetFloat("_EnableCustomFog", 1f);
                                renderer.sharedMaterial.SetFloat("_Specular", 0.5f);
                            }
                            else
                            {
                                renderer.sharedMaterial.SetFloat("_EnableCustomFog", 0f);
                                renderer.sharedMaterial.SetFloat("_Specular", 1f);
                            }
                        }

                        isSuccess = true;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning("LBIntegration. Something went wrong with AQUAS. Import AQUAS to use this feature. LB supports version 1.3 and newer" + ex.Message.ToString());
                    }
                }
            }
            return isSuccess;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Create the AQUAS Reference image that will be used to paint the flow map
        /// with the external tool. It is saved to the Assets\RiverReferences folder
        /// </summary>
        /// <param name="showErrors"></param>
        public static void AQUASCreateReferenceImage(GameObject cameraGameObject, GameObject waterGameObject, bool showErrors)
        {
            System.Type aquasRiverSetupType = null;

            try
            {
                aquasRiverSetupType = System.Type.GetType("AQUAS_RiverSetup, Assembly-CSharp-Editor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", true, true);
                if (aquasRiverSetupType != null)
                {
                    var windowRiverSetup = UnityEditor.EditorWindow.GetWindow(aquasRiverSetupType, false);

                    if (windowRiverSetup != null)
                    {
                        // Set the required public members
                        aquasRiverSetupType.InvokeMember("camera", System.Reflection.BindingFlags.SetField, null, windowRiverSetup, new object[] { cameraGameObject });
                        aquasRiverSetupType.InvokeMember("waterPlane", System.Reflection.BindingFlags.SetField, null, windowRiverSetup, new object[] { waterGameObject });

                        aquasRiverSetupType.InvokeMember("CreateRiverReference", System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.NonPublic, null, windowRiverSetup, new object[] { });
                        // Close the RiverSetup window after the image was saved to the Assets\RiverReferences folder
                        windowRiverSetup.Close();
                    }
                }
                aquasRiverSetupType = null;
            }
            catch (Exception ex)
            {
                if (showErrors) { Debug.LogWarning("LBIntegration.AQUASCreateReferenceImage - Sorry, something went wrong. " + ex.Message); }
            }
        }
#endif

        /// <summary>
        /// Attempt to add the AQUAS Camera script to a gameobject
        /// </summary>
        /// <param name="gameObj"></param>
        /// <param name="showErrors"></param>
        /// <param name="isSceneSaveRequired"></param>
        public static void AddAQUASCameraScript(GameObject gameObj, bool showErrors, ref bool isSceneSaveRequired)
        {
            if (gameObj == null)
            {
                if (showErrors) { Debug.LogError("LBIntegration.AddAQUASCameraScript - gameobject not defined"); }
            }
            else if (isAQUASInstalled(showErrors))
            {
                Camera camara = gameObj.GetComponent<Camera>();
                if (camara == null)
                {
                    if (showErrors) { Debug.Log("LBIntegration.AddAQUASCameraScript - no camera attached to " + gameObj.name); }
                }
                else
                {
                    // Get a refence to the AQUAS Camera type
                    System.Type aquasCamerType = System.Type.GetType("AQUAS_Camera, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", true, true);
                    if (aquasCamerType != null)
                    {
                        // Check to see if the AQUAS_Camera script is already attached to this gameobject
                        var aquasCameraComponent = gameObj.GetComponent(aquasCamerType);
                        // Only add the script if it isn't already attached.
                        if (aquasCameraComponent == null)
                        {
                            gameObj.AddComponent(aquasCamerType);
                            isSceneSaveRequired = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Remove the AQUAS_Camera script from the gameobject.
        /// WARNING: When a component attached to the same custom inspector is removed, Unity may attempt to redraw it after it has been removed.
        ///          To avoid this, either call EditorGUIUtility.ExitGUI() immediately after this method or move the component further up the
        ///          list before the custom inspector script. The error raised is:
        ///          MissingReferenceException: The object of type 'AQUAS_Camera' has been destroyed but you are still trying to access it.
        /// </summary>
        /// <param name="gameObj"></param>
        /// <param name="showErrors"></param>
        /// <param name="isSceneSaveRequired"></param>
        public static void RemoveAQUASCameraScript(GameObject gameObj, bool showErrors, ref bool isSceneSaveRequired)
        {
            if (gameObj == null)
            {
                if (showErrors) { Debug.LogError("LBIntegration.RemoveAQUASCameraScript - gameobject not defined"); }
            }
            else if (isAQUASInstalled(showErrors))
            {
                // Get a refence to the AQUAS Camera type
                System.Type aquasCamerType = System.Type.GetType("AQUAS_Camera, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", true, true);
                if (aquasCamerType != null)
                {
                    // Check to see if the AQUAS_Camera script attached to this gameobject
                    var aquasCameraComponent = gameObj.GetComponent(aquasCamerType);
                    // Only attempt to remove if it is attached.
                    if (aquasCameraComponent != null)
                    {
                        DestroyImmediate(aquasCameraComponent);
                        isSceneSaveRequired = true;
                    }
                    else
                    {
                        if (showErrors) { Debug.LogError("LBIntegration.RemoveAQUASCameraScript - no component to remove"); }
                    }
                }
            }
        }

        /// <summary>
        /// Cleanup and remove any previous scripts and components required by AQUAS attached to a water gameobject
        /// NOTE: This does not remove AQUAS scripts from cameras. For that see:
        /// LBIntegration.RemoveAQUASCameraScript()
        /// </summary>
        /// <param name="gameObj"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static bool RemoveAQUASComponents(GameObject gameObj, bool showErrors)
        {
            bool isSuccessful = false;

            if (gameObj == null)
            {
                if (showErrors) { Debug.LogError("LBIntegration.RemoveAQUASCameraScript - gameobject not defined"); }
            }
            else if (isAQUASInstalled(showErrors))
            {
                System.Type aquasReflectionType = null;
                System.Type aquasRenderQueueEditorType = null;

                try
                {
                    aquasReflectionType = System.Type.GetType("AQUAS_Reflection, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", true, true);
                    aquasRenderQueueEditorType = System.Type.GetType("AQUAS_RenderQueueEditor, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", true, true);

                    // If there is a child reflection probe, remove it
                    Transform refProbeTrfm = gameObj.transform.Find("Reflection Probe");
                    if (refProbeTrfm != null) { DestroyImmediate(refProbeTrfm.gameObject); }

                    if (aquasRenderQueueEditorType != null)
                    {
                        // Check to see if the AQUAS_Render Queue Editor script is already attached to the mesh transform
                        var aquasRenderQueueEditorComponent = gameObj.GetComponent(aquasRenderQueueEditorType);

                        // Remove it, if it is attached.
                        if (aquasRenderQueueEditorComponent != null) { DestroyImmediate(aquasRenderQueueEditorComponent); }
                    }

                    if (aquasReflectionType != null)
                    {
                        // Check to see if the AQUAS_Relection script is already attached to the mesh transform
                        var aquasReflectionComponent = gameObj.GetComponent(aquasReflectionType);

                        // Remove it, if it is attached.
                        if (aquasReflectionComponent != null) { DestroyImmediate(aquasReflectionComponent); }
                    }

                    isSuccessful = true;
                }
                catch
                {
                    if (showErrors) { Debug.LogWarning("LBIntegration.RemoveAQUASComponents: it appears that AQUAS 1.3 or newer is not installed in this project."); }
                }
            }
            return isSuccessful;
        }

        /// <summary>
        /// Add or remove the AQUAS Underwater FX from camera
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="waterGameObj"></param>
        /// <param name="cameraGameObj"></param>
        /// <param name="isEnable"></param>
        /// <param name="underWaterFXPrefab"></param>
        /// <param name="showErrors"></param>
        public static void AQUASEnableUnderwaterFX(LBLandscape landscape, GameObject waterGameObj, Vector2 waterSize, float waterLevel,
                                                   GameObject cameraGameObj, bool isEnable, Transform underWaterFXPrefab, bool showErrors)
        {
            if (cameraGameObj == null)
            {
                if (showErrors) { Debug.LogWarning("LBIntegration.AQUASEnableUnderwaterFX - gameobject not defined"); }
            }
            else if (cameraGameObj.GetComponent<Camera>() == null)
            {
                if (showErrors) { Debug.LogWarning("LBIntegration.AQUASEnableUnderwaterFX - there is no camera attached to " + cameraGameObj.name); }
            }
            else if (isEnable && underWaterFXPrefab == null)
            {
                if (showErrors) { Debug.LogWarning("LBIntegration.AQUASEnableUnderwaterFX - underWaterFXPrefab is null"); }
            }
           else if (isAQUASInstalled(showErrors))
            {
                // AQUAS v1.5 (ish) introduced PostProcessingStack (v1 or v2) for underwater fx.
                bool usePostProcessing = System.IO.File.Exists("Assets/AQUAS/Post Processing/AQUAS_Underwater.asset");

                System.Type bloomType = null;
                System.Type blurOptimizedType = null;
                System.Type vigAndChromAbType = null;
                System.Type noiseAndGrainType = null;
                System.Type sunShaftsType = null;
                System.Type globalFogType = null;

                #if AQUAS_PRESENT && UNITY_EDITOR

                #if UNITY_POST_PROCESSING_STACK_V1
                //var pp = LBEditorHelper.GetAsset<UnityEngine.PostProcessing.PostProcessingProfile>("AQUAS/Post Processing/", "AQUAS_Underwater");
                System.Type ppBehaviourType = typeof(UnityEngine.PostProcessing.PostProcessingBehaviour);
                #elif UNITY_POST_PROCESSING_STACK_V2
                System.Type ppPostProcessLayerType = typeof(UnityEngine.Rendering.PostProcessing.PostProcessLayer);
                System.Type ppPostProcessResourcesType = typeof(UnityEngine.Rendering.PostProcessing.PostProcessResources);
                System.Type ppPostProcessVolumeType = typeof(UnityEngine.Rendering.PostProcessing.PostProcessVolume);
                #endif

                #endif

                if (!usePostProcessing)
                {
                    // Check that all the effects are installed in the project
                    bloomType = GetClassTypeFromFullName("UnityStandardAssets.ImageEffects.Bloom, Assembly-CSharp-firstpass, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", false);
                    blurOptimizedType = GetClassTypeFromFullName("UnityStandardAssets.ImageEffects.BlurOptimized, Assembly-CSharp-firstpass, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", false);
                    vigAndChromAbType = GetClassTypeFromFullName("UnityStandardAssets.ImageEffects.VignetteAndChromaticAberration, Assembly-CSharp-firstpass, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", false);
                    noiseAndGrainType = GetClassTypeFromFullName("UnityStandardAssets.ImageEffects.NoiseAndGrain, Assembly-CSharp-firstpass, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", false);
                    sunShaftsType = GetClassTypeFromFullName("UnityStandardAssets.ImageEffects.SunShafts, Assembly-CSharp-firstpass, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", false);
                    globalFogType = GetClassTypeFromFullName("UnityStandardAssets.ImageEffects.GlobalFog, Assembly-CSharp-firstpass, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", false);

                    // Raise a warning if any of the effects are not installed
                    if (bloomType == null) { if (showErrors) { Debug.LogWarning("Unity Standard Asset ImageEffects Bloom needs to be installed in this project"); } }
                    if (blurOptimizedType == null) { if (showErrors) { Debug.LogWarning("Unity Standard Asset ImageEffects Blur Optimized needs to be installed in this project"); } }
                    if (vigAndChromAbType == null) { if (showErrors) { Debug.LogWarning("Unity Standard Asset ImageEffects Vignette And Chromatic Aberration needs to be installed in this project"); } }
                    if (noiseAndGrainType == null) { if (showErrors) { Debug.LogWarning("Unity Standard Asset ImageEffects Noise And Grain needs to be installed in this project"); } }
                    if (sunShaftsType == null) { if (showErrors) { Debug.LogWarning("Unity Standard Asset ImageEffects Sun Shafts needs to be installed in this project"); } }
                    if (globalFogType == null) { if (showErrors) { Debug.LogWarning("Unity Standard Asset ImageEffects Global Fog needs to be installed in this project"); } }
                }

                System.Type aquasLensFXType = GetClassTypeFromFullName("AQUAS_LensEffects, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", false);

                if (aquasLensFXType == null) { if (showErrors) { Debug.LogWarning("AQUAS_LensEffects was not found. Is AQUAS Water Set (not AQUAS-Lite) installed in this project?"); } }

                if (isEnable)
                {
                    if (aquasLensFXType != null)
                    {
                        try
                        {
                            #region PostProcessing AQUAS 1.5+
                            if (usePostProcessing)
                            {
                                #if AQUAS_PRESENT && UNITY_EDITOR

                                #if UNITY_POST_PROCESSING_STACK_V1
                                if (cameraGameObj.GetComponent(ppBehaviourType) == null)
                                cameraGameObj.AddComponent(ppBehaviourType);
                                #elif UNITY_POST_PROCESSING_STACK_V2

                                if (cameraGameObj.GetComponent(ppPostProcessLayerType) == null)
                                {
                                    cameraGameObj.AddComponent(ppPostProcessLayerType);

                                    UnityEngine.Rendering.PostProcessing.PostProcessResources ppResources;

                                    ppResources = (UnityEngine.Rendering.PostProcessing.PostProcessResources)UnityEditor.AssetDatabase.LoadAssetAtPath("Assets/PostProcessing-2/PostProcessing/PostProcessResources.asset", ppPostProcessResourcesType);

                                    if (ppResources == null)
                                    {
                                        ppResources = (UnityEngine.Rendering.PostProcessing.PostProcessResources)UnityEditor.AssetDatabase.LoadAssetAtPath("Assets/PostProcessing/PostProcessResources.asset", ppPostProcessResourcesType);
                                        if (ppResources == null)
                                        {
                                            ppResources = (UnityEngine.Rendering.PostProcessing.PostProcessResources)UnityEditor.AssetDatabase.LoadAssetAtPath("Assets/PostProcessing-2/PostProcessResources.asset", ppPostProcessResourcesType);
                                            
                                            if (ppResources == null)
                                            {
                                                ppResources = (UnityEngine.Rendering.PostProcessing.PostProcessResources)UnityEditor.AssetDatabase.LoadAssetAtPath("Packages/Post-processing/PostProcessing/PostProcessResources.asset", ppPostProcessResourcesType);

                                                if (ppResources == null && showErrors)
                                                {
                                                    Debug.LogWarning("ERROR: Could not locate Post Process Resource file. Please make sure your post processing folder is at the top level of the assets folder and named either 'PostProcessing' or 'PostProcessing-2'. The file should be named 'PostProcessResources'. See AQUAS manual for more details.");
                                                }
                                            }
                                        }
                                    }

                                    if (ppResources != null)
                                    {
                                        cameraGameObj.GetComponent<UnityEngine.Rendering.PostProcessing.PostProcessLayer>().Init(ppResources);
                                    }
                                    cameraGameObj.GetComponent<UnityEngine.Rendering.PostProcessing.PostProcessLayer>().volumeLayer = LayerMask.NameToLayer("Everything");
                                }

                                if (cameraGameObj.GetComponent(ppPostProcessVolumeType) == null)
                                {
                                    cameraGameObj.AddComponent(ppPostProcessVolumeType);
                                    cameraGameObj.GetComponent<UnityEngine.Rendering.PostProcessing.PostProcessVolume>().isGlobal = true;
                                }
                                #endif

                                #endif
                            }
                            #endregion

                            #region Pre-Post Processing - AQUAS 1.4 and earlier
                            else if (bloomType != null && blurOptimizedType != null && vigAndChromAbType != null && noiseAndGrainType != null && sunShaftsType != null && globalFogType != null)
                            {
                                // The component may already exist. So first add it, then configure
                                if (cameraGameObj.GetComponent(bloomType) == null) { cameraGameObj.AddComponent(bloomType); }
                                var bloomFX = cameraGameObj.GetComponent(bloomType);
                                if (bloomFX == null) { if (showErrors) { Debug.LogError("LBIntegration.AQUASEnableUnderwaterFX - could not add bloom FX to " + cameraGameObj.name); } }
                                else
                                {
                                    bloomType.InvokeMember("enabled", System.Reflection.BindingFlags.SetProperty, null, bloomFX, new object[] { false });
                                    bloomType.InvokeMember("bloomIntensity", System.Reflection.BindingFlags.SetField, null, bloomFX, new object[] { 0.4f });
                                    bloomType.InvokeMember("bloomThreshold", System.Reflection.BindingFlags.SetField, null, bloomFX, new object[] { 0.5f });
                                    bloomType.InvokeMember("bloomBlurIterations", System.Reflection.BindingFlags.SetField, null, bloomFX, new object[] { 1 });
                                }

                                if (cameraGameObj.GetComponent(blurOptimizedType) == null) { cameraGameObj.AddComponent(blurOptimizedType); }
                                var blurOptimizedFX = cameraGameObj.GetComponent(blurOptimizedType);
                                if (blurOptimizedFX == null) { if (showErrors) { Debug.LogError("LBIntegration.AQUASEnableUnderwaterFX - could not add blur optimized FX to " + cameraGameObj.name); } }
                                else
                                {
                                    blurOptimizedType.InvokeMember("enabled", System.Reflection.BindingFlags.SetProperty, null, blurOptimizedFX, new object[] { false });
                                    blurOptimizedType.InvokeMember("downsample", System.Reflection.BindingFlags.SetField, null, blurOptimizedFX, new object[] { 0 });
                                    blurOptimizedType.InvokeMember("blurSize", System.Reflection.BindingFlags.SetField, null, blurOptimizedFX, new object[] { 1.5f });
                                    blurOptimizedType.InvokeMember("blurIterations", System.Reflection.BindingFlags.SetField, null, blurOptimizedFX, new object[] { 2 });
                                }

                                if (cameraGameObj.GetComponent(vigAndChromAbType) == null) { cameraGameObj.AddComponent(vigAndChromAbType); }
                                var vigAndChromAbFX = cameraGameObj.GetComponent(vigAndChromAbType);
                                if (vigAndChromAbFX == null) { if (showErrors) { Debug.LogError("LBIntegration.AQUASEnableUnderwaterFX - could not add Vignette And Chromatic Aberration FX to " + cameraGameObj.name); } }
                                else
                                {
                                    vigAndChromAbType.InvokeMember("enabled", System.Reflection.BindingFlags.SetProperty, null, vigAndChromAbFX, new object[] { false });
                                    vigAndChromAbType.InvokeMember("intensity", System.Reflection.BindingFlags.SetField, null, vigAndChromAbFX, new object[] { 0f });
                                    vigAndChromAbType.InvokeMember("blur", System.Reflection.BindingFlags.SetField, null, vigAndChromAbFX, new object[] { 0.54f });
                                    vigAndChromAbType.InvokeMember("blurDistance", System.Reflection.BindingFlags.SetField, null, vigAndChromAbFX, new object[] { 1f });
                                    vigAndChromAbType.InvokeMember("chromaticAberration", System.Reflection.BindingFlags.SetField, null, vigAndChromAbFX, new object[] { 0f });
                                }

                                if (cameraGameObj.GetComponent(noiseAndGrainType) == null) { cameraGameObj.AddComponent(noiseAndGrainType); }
                                var noiseAndGrainFX = cameraGameObj.GetComponent(noiseAndGrainType);
                                if (noiseAndGrainFX == null) { if (showErrors) { Debug.LogError("LBIntegration.AQUASEnableUnderwaterFX - could not add Noise And Grain FX to " + cameraGameObj.name); } }
                                else
                                {
                                    noiseAndGrainType.InvokeMember("enabled", System.Reflection.BindingFlags.SetProperty, null, noiseAndGrainFX, new object[] { false });
                                    noiseAndGrainType.InvokeMember("dx11Grain", System.Reflection.BindingFlags.SetField, null, noiseAndGrainFX, new object[] { false });
                                    noiseAndGrainType.InvokeMember("monochrome", System.Reflection.BindingFlags.SetField, null, noiseAndGrainFX, new object[] { false });
                                    noiseAndGrainType.InvokeMember("intensityMultiplier", System.Reflection.BindingFlags.SetField, null, noiseAndGrainFX, new object[] { 1.86f });
                                    noiseAndGrainType.InvokeMember("generalIntensity", System.Reflection.BindingFlags.SetField, null, noiseAndGrainFX, new object[] { 0f });
                                    noiseAndGrainType.InvokeMember("blackIntensity", System.Reflection.BindingFlags.SetField, null, noiseAndGrainFX, new object[] { 1f });
                                    noiseAndGrainType.InvokeMember("whiteIntensity", System.Reflection.BindingFlags.SetField, null, noiseAndGrainFX, new object[] { 1f });
                                    noiseAndGrainType.InvokeMember("midGrey", System.Reflection.BindingFlags.SetField, null, noiseAndGrainFX, new object[] { 0.2f });
                                    noiseAndGrainType.InvokeMember("softness", System.Reflection.BindingFlags.SetField, null, noiseAndGrainFX, new object[] { 0.276f });
                                    noiseAndGrainType.InvokeMember("tiling", System.Reflection.BindingFlags.SetField, null, noiseAndGrainFX, new object[] { new Vector3(512, 512, 512) });
                                }

                                if (cameraGameObj.GetComponent(sunShaftsType) == null) { cameraGameObj.AddComponent(sunShaftsType); }
                                var sunShaftsFX = cameraGameObj.GetComponent(sunShaftsType);
                                if (sunShaftsFX == null) { if (showErrors) { Debug.LogError("LBIntegration.AQUASEnableUnderwaterFX - could not add Sun Shafts FX to " + cameraGameObj.name); } }
                                else
                                {
                                    sunShaftsType.InvokeMember("enabled", System.Reflection.BindingFlags.SetProperty, null, sunShaftsFX, new object[] { false });
                                    sunShaftsType.InvokeMember("sunShaftIntensity", System.Reflection.BindingFlags.SetField, null, sunShaftsFX, new object[] { 0.6f });
                                    sunShaftsType.InvokeMember("sunShaftBlurRadius", System.Reflection.BindingFlags.SetField, null, sunShaftsFX, new object[] { 1f });
                                    sunShaftsType.InvokeMember("radialBlurIterations", System.Reflection.BindingFlags.SetField, null, sunShaftsFX, new object[] { 3 });
                                    sunShaftsType.InvokeMember("maxRadius", System.Reflection.BindingFlags.SetField, null, sunShaftsFX, new object[] { 0.1f });
                                }

                                if (cameraGameObj.GetComponent(globalFogType) == null) { cameraGameObj.AddComponent(globalFogType); }
                                var globalFogFX = cameraGameObj.GetComponent(globalFogType);
                                if (globalFogFX == null) { if (showErrors) { Debug.LogError("LBIntegration.AQUASEnableUnderwaterFX - could not add Global Fog FX to " + cameraGameObj.name); } }
                                else
                                {
                                    globalFogType.InvokeMember("enabled", System.Reflection.BindingFlags.SetProperty, null, globalFogFX, new object[] { false });
                                }
                            }
                            #endregion

                            if (cameraGameObj.GetComponent<AudioSource>() == null) { cameraGameObj.AddComponent<AudioSource>(); }
                            AudioSource audioSource = cameraGameObj.GetComponent<AudioSource>();
                            if (audioSource == null) { if (showErrors) { Debug.LogError("LBIntegration.AQUASEnableUnderwaterFX - could not add an AudioSource to " + cameraGameObj.name); } }
                            else
                            {
                                audioSource.GetType().InvokeMember("enabled", System.Reflection.BindingFlags.SetProperty, null, audioSource, new object[] { true });
                            }

                            // Add the underwater FX prefab to the camera if it doesn't already exist
                            Transform underwaterTransform = null;
                            if (cameraGameObj.GetComponentInChildren(aquasLensFXType) == null)
                            {
                                underwaterTransform = Instantiate(underWaterFXPrefab);
                                if (underwaterTransform == null)
                                {
                                    Debug.LogWarning("LBIntegration.AQUASEnableUnderwaterFX - could not instantiate prefab: " + underWaterFXPrefab.name);
                                }
                                else
                                {
                                    if (usePostProcessing)
                                    {
                                        #if UNITY_POST_PROCESSING_STACK_V2 && AQUAS_PRESENT
                                        underwaterTransform.GetComponent<AQUAS_LensEffects>().underWaterParameters.underwaterProfile = (UnityEngine.Rendering.PostProcessing.PostProcessProfile)UnityEditor.AssetDatabase.LoadAssetAtPath("Assets/AQUAS/Post Processing/AQUAS_Underwater_v2.asset", typeof(UnityEngine.Rendering.PostProcessing.PostProcessProfile));
                                        underwaterTransform.GetComponent<AQUAS_LensEffects>().underWaterParameters.defaultProfile = (UnityEngine.Rendering.PostProcessing.PostProcessProfile)UnityEditor.AssetDatabase.LoadAssetAtPath("Assets/AQUAS/Post Processing/DefaultPostProcessing_v2.asset", typeof(UnityEngine.Rendering.PostProcessing.PostProcessProfile));
                                        #endif
                                    }
                                    underwaterTransform.name = "UnderWaterCameraEffects";
                                    underwaterTransform.SetParent(cameraGameObj.transform);
                                    underwaterTransform.localPosition = new Vector3(0, 0, 0);
                                    underwaterTransform.localEulerAngles = new Vector3(0, 0, 0);
                                }
                            }

                            if (underwaterTransform != null)
                            {
                                var aquasLensFX = underwaterTransform.GetComponent(aquasLensFXType);
                                if (aquasLensFX == null) { if (showErrors) { Debug.LogWarning("LBIntegration.AQUASEnableUnderwaterFX - could not add AQUAS_LensEffects to " + underwaterTransform.name + " which is a child of " + cameraGameObj.name); } }
                                else
                                {
                                    // Lens Effects FX component uses classes to hold groups of parameters
                                    var gameObjects = aquasLensFXType.InvokeMember("gameObjects", System.Reflection.BindingFlags.GetField, null, aquasLensFX, new object[] { });
                                    if (gameObjects != null)
                                    {
                                        gameObjects.GetType().InvokeMember("useSquaredPlanes", System.Reflection.BindingFlags.SetField, null, gameObjects, new object[] { true });
                                        gameObjects.GetType().InvokeMember("mainCamera", System.Reflection.BindingFlags.SetField, null, gameObjects, new object[] { cameraGameObj });

                                        // waterPlanes is a list in the AQUAS_Parameters.GameObjects class
                                        List<GameObject> waterPlanesList = new List<GameObject>();
                                        waterPlanesList.Add(waterGameObj);

                                        gameObjects.GetType().InvokeMember("waterPlanes", System.Reflection.BindingFlags.SetField, null, gameObjects, new object[] { waterPlanesList });
                                    }
                                }
                            }

                            // We have tested that the camera is already attached, so go ahead and set the farClipPlane.
                            cameraGameObj.GetComponent<Camera>().farClipPlane = landscape.size.x;

                            AQUASUpdateBorders(landscape, waterGameObj, waterSize, waterLevel, showErrors);
                        }
                        catch (Exception ex)
                        {
                            if (showErrors) { Debug.LogWarning("LBIntegration.AQUASEnableUnderwaterFX - sorry, something went wrong. " + ex.Message); }
                        }
                    }
                }
                else
                {
                    // Remove any components previously added to the camera
                    if (bloomType != null) { var bloomFX = cameraGameObj.GetComponent(bloomType); if (bloomFX != null) { DestroyImmediate(bloomFX); } }
                    if (blurOptimizedType != null) { var blurOptimizedFX = cameraGameObj.GetComponent(blurOptimizedType); if (blurOptimizedFX != null) { DestroyImmediate(blurOptimizedFX); } }
                    if (vigAndChromAbType != null) { var vigAndChromAbFX = cameraGameObj.GetComponent(vigAndChromAbType); if (vigAndChromAbFX != null) { DestroyImmediate(vigAndChromAbFX); } }
                    if (noiseAndGrainType != null) { var noiseAndGrainFX = cameraGameObj.GetComponent(noiseAndGrainType); if (noiseAndGrainFX != null) { DestroyImmediate(noiseAndGrainFX); } }
                    if (sunShaftsType != null) { var sunShaftsFX = cameraGameObj.GetComponent(sunShaftsType); if (sunShaftsFX != null) { DestroyImmediate(sunShaftsFX); } }
                    if (globalFogType != null) { var globalFogFX = cameraGameObj.GetComponent(globalFogType); if (globalFogFX != null) { DestroyImmediate(globalFogFX); } }

                    // Remove AQUAS_LensEffects from the camera
                    var aquasLensFX = cameraGameObj.GetComponentInChildren(aquasLensFXType);
                    if (aquasLensFX != null) { DestroyImmediate(aquasLensFX.gameObject); }

                    // Just disable the audio source as it may be used for something else on this camera
                    AudioSource audioSource = cameraGameObj.GetComponent<AudioSource>(); if (audioSource != null) { audioSource.enabled = false; }

                    if (usePostProcessing)
                    {
                        #if AQUAS_PRESENT && UNITY_EDITOR

                        #if UNITY_POST_PROCESSING_STACK_V1
                        Component ppBehaviour = cameraGameObj.GetComponent(ppBehaviourType);
                        if (ppBehaviour != null) { GameObject.DestroyImmediate(ppBehaviour); }
                        #elif UNITY_POST_PROCESSING_STACK_V2
                        Component ppPostLayer = cameraGameObj.GetComponent(ppPostProcessLayerType);
                        if (ppPostLayer != null) { GameObject.DestroyImmediate(ppPostLayer); }
                        Component ppPostVolume = cameraGameObj.GetComponent(ppPostProcessVolumeType);
                        if (ppPostVolume != null) { GameObject.DestroyImmediate(ppPostVolume); }
                        #endif

                        #endif
                    }

                    // Find the child object "Borders" under the water gameobject.
                    Transform borders = waterGameObj.transform.Find("Borders");
                    if (borders != null) { DestroyImmediate(borders.gameObject); }
                }
            }
        }

        public static void AQUASUpdateWaterPlanes(GameObject cameraGameObj, GameObject waterGameObj, bool showErrors)
        {
            System.Type aquasLensFXType = GetClassTypeFromFullName("AQUAS_LensEffects, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", false);

            if (aquasLensFXType == null) { if (showErrors) { Debug.LogWarning("AQUAS_LensEffects was not found. Is AQUAS Water Set (not AQUAS-Lite) installed in this project?"); } }

            if (aquasLensFXType != null)
            {
                var aquasLensFX = cameraGameObj.GetComponentInChildren(aquasLensFXType);
                if (aquasLensFX == null) { if (showErrors) { Debug.LogWarning("LBIntegration.AQUASUpdateWaterPlanes - could find UnderWaterCameraEffects AQUAS_LensEffects component which is a child of " + cameraGameObj.name); } }

                if (aquasLensFX != null)
                {
                    // Lens Effects FX component uses classes to hold groups of parameters
                    var gameObjects = aquasLensFXType.InvokeMember("gameObjects", System.Reflection.BindingFlags.GetField, null, aquasLensFX, new object[] { });
                    if (gameObjects != null)
                    {
                        // waterPlanes is a list in the AQUAS_Parameters.GameObjects class
                        List<GameObject> waterPlanesList = new List<GameObject>();
                        waterPlanesList.Add(waterGameObj);

                        gameObjects.GetType().InvokeMember("waterPlanes", System.Reflection.BindingFlags.SetField, null, gameObjects, new object[] { waterPlanesList });
                    }
                }
            }
        }


        /// <summary>
        /// Add or update AQUAS borders used for Underwater Effects
        /// This is also required whenever the water level is changed
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="waterGameObj"></param>
        /// <param name="waterSize"></param>
        /// <param name="waterLevel"></param>
        /// <param name="showErrors"></param>
        public static void AQUASUpdateBorders(LBLandscape landscape, GameObject waterGameObj, Vector2 waterSize, float waterLevel, bool showErrors)
        {
            // Create the Borders if they don't already exist
            Transform borders = waterGameObj.transform.Find("Borders");
            Vector3 waterPosition = waterGameObj.transform.position;
            if (borders == null)
            {
                GameObject bordersGameObj = new GameObject();
                borders = bordersGameObj.transform;
                borders.name = "Borders";

                GameObject borderLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
                borderLeft.name = "Left Border";
                borderLeft.transform.localScale = new Vector3(waterSize.x, waterLevel, 0.1f);
                borderLeft.transform.position = new Vector3(waterPosition.x, waterLevel - (waterLevel / 2), waterPosition.z + waterSize.x / 2);

                GameObject borderRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
                borderRight.name = "Right Border";
                borderRight.transform.localScale = new Vector3(waterSize.x, waterLevel, 0.1f);
                borderRight.transform.position = new Vector3(waterPosition.x, waterLevel - (waterLevel / 2), waterPosition.z - waterSize.x / 2);

                GameObject borderTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
                borderTop.name = "Top Border";
                borderTop.transform.localScale = new Vector3(0.1f, waterLevel, waterSize.x);
                borderTop.transform.position = new Vector3(waterPosition.x + waterSize.x / 2, waterLevel - (waterLevel / 2), waterPosition.z);

                GameObject borderBottom = GameObject.CreatePrimitive(PrimitiveType.Cube);
                borderBottom.name = "Bottom Border";
                borderBottom.transform.localScale = new Vector3(0.1f, waterLevel, waterSize.x);
                borderBottom.transform.position = new Vector3(waterPosition.x - waterSize.x / 2, waterLevel - (waterLevel / 2), waterPosition.z);

                borderLeft.transform.SetParent(borders.transform);
                borderRight.transform.SetParent(borders.transform);
                borderTop.transform.SetParent(borders.transform);
                borderBottom.transform.SetParent(borders.transform);

                borders.transform.SetParent(waterGameObj.transform);
            }
            else
            {
                // Update the borders
                Transform borderLeft = borders.Find("Left Border");
                if (borderLeft != null)
                {
                    borderLeft.transform.localScale = new Vector3(waterSize.x, waterLevel, 0.1f);
                    borderLeft.transform.position = new Vector3(waterPosition.x, waterLevel - (waterLevel / 2), waterPosition.z + waterSize.x / 2);
                }
                Transform borderRight = borders.Find("Right Border");
                if (borderLeft != null)
                {
                    borderRight.transform.localScale = new Vector3(waterSize.x, waterLevel, 0.1f);
                    borderRight.transform.position = new Vector3(waterPosition.x, waterLevel - (waterLevel / 2), waterPosition.z - waterSize.x / 2);
                }
                Transform borderTop = borders.Find("Top Border");
                if (borderTop != null)
                {
                    borderTop.transform.localScale = new Vector3(0.1f, waterLevel, waterSize.x);
                    borderTop.transform.position = new Vector3(waterPosition.x + waterSize.x / 2, waterLevel - (waterLevel / 2), waterPosition.z);
                }
                Transform borderBottom = borders.Find("Bottom Border");
                if (borderBottom != null)
                {
                    borderBottom.transform.localScale = new Vector3(0.1f, waterLevel, waterSize.x);
                    borderBottom.transform.position = new Vector3(waterPosition.x - waterSize.x / 2, waterLevel - (waterLevel / 2), waterPosition.z);
                }
            }
        }


        #endregion

        #region River Auto Material Integration

        public static bool IsRiverAutoMaterialInstalled(bool showErrors)
        {
            bool isInstalled = false;

            Shader shader = Shader.Find("NatureManufacture Shaders/Water/Water River");
            if (shader == null)
            {
                // Try the older shader
                shader = Shader.Find("NatureManufacture Shaders/Water River");
            }

            if (shader != null) { isInstalled = true; }
            else { if (showErrors) { Debug.LogWarning("LBIntegration.IsRiverAutoMaterialInstalled - River Auto Material does not seem to be installed in this project"); } }

            return isInstalled;
        }

        #endregion

        #region Calm Water Integration

        /// <summary>
        /// Is Calm Water asset installed in the project?
        /// </summary>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static bool IsCalmWaterInstalled(bool showErrors)
        {
            bool isInstalled = false;

            System.Type mirrorReflectionType = null;

            try
            {
                mirrorReflectionType = System.Type.GetType("MirrorReflection, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", true, true);
                if (mirrorReflectionType != null) { isInstalled = true; }
                mirrorReflectionType = null;
            }
            catch
            {
                if (showErrors) { Debug.LogWarning("LBIntegration.IsCalmWaterInstalled: it appears that Calm Water 1.5.9 or newer is not installed in this project."); }
            }

            return isInstalled;
        }

        /// <summary>
        /// Add the Calm Water material and scripts to the river mesh in the scene
        /// Set GameObject to Water Layer to prevent Calm Water from crashing Unity when
        /// Mirror_Reflection script is added. Requires Calm Water 1.5.9 or newer
        /// </summary>
        /// <param name="lbWaterParms"></param>
        /// <param name="lbWater"></param>
        /// <returns></returns>
        public static bool AddCalmWaterToMesh(LBWaterParameters lbWaterParms, LBWater lbWater, bool showErrors)
        {
            bool isSuccess = false;
            bool isCalmWaterInstalled = IsCalmWaterInstalled(false);

            // Basic Validation
            if (lbWaterParms == null) { Debug.LogWarning("LBIntegration.AddCalmWaterToMesh - lbWaterParms cannot be null"); }
            else if (lbWaterParms.waterTransform == null) { Debug.LogWarning("LBIntegration.AddCalmWaterToMesh - lbWaterParms.waterTransform cannot be null"); }
            else if (!isCalmWaterInstalled)
            {
                Debug.LogWarning("LBIntegration.AddCalmWaterToMesh requires Calm Water 1.5.9 or newer. Import Calm Water 1.5.9 or newer to use this feature.");
            }
            else
            {
                lbWaterParms.waterTransform.gameObject.layer = LayerMask.NameToLayer("Water");

                Renderer renderer = lbWaterParms.waterTransform.GetComponent<Renderer>();
                if (renderer == null) { Debug.LogWarning("LBIntegration.AddCalmWaterToMesh - " + lbWaterParms.waterTransform.name + " is missing the rendered"); }
                else
                {
                    // Remove any mesh colliders from the river surface mesh
                    DestroyImmediate(lbWaterParms.waterTransform.GetComponent<MeshCollider>());
                    // Calm Water should not cast shadows
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                    isSuccess = EnableCalmWaterComponents(lbWaterParms.waterTransform, true, showErrors);
                }
            }

            return isSuccess;
        }

        /// <summary>
        /// Add the Calm Water material and scripts to a primary or secondary body of water in the scene
        /// Set GameObject to Water Layer to prevent Calm Water from crashing Unity when
        /// Mirror_Reflection script is added. Requires Calm Water 1.5.9 or newer
        /// </summary>
        /// <param name="lbWaterParms"></param>
        /// <param name="lbWater"></param>
        /// <param name="sizeX"></param>
        /// <param name="sizeZ"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static bool AddCalmWaterToScene(LBWaterParameters lbWaterParms, LBWater lbWater, float sizeX, float sizeZ, bool showErrors)
        {
            bool isSuccess = false;
            bool isCalmWaterInstalled = IsCalmWaterInstalled(false); // Don't show errors twice

            // Basic Validation
            if (lbWaterParms == null) { Debug.LogWarning("LBIntegration.AddCalmWaterToScene - lbWaterParms cannot be null"); }
            else if (lbWaterParms.waterTransform == null) { Debug.LogWarning("LBIntegration.AddCalmWaterToScene - lbWaterParms.waterTransform cannot be null"); }
            else if (!isCalmWaterInstalled)
            {
                if (showErrors) { Debug.LogWarning("LBIntegration.AddCalmWaterToScene requires Calm Water 1.5.9 or newer. Import Calm Water 1.5.9 or newer to use this feature."); }
            }
            else
            {
                // Create in Water layer to prevent Calm Water mirror_reflection script from crashing Unity
                lbWaterParms.waterTransform.gameObject.layer = LayerMask.NameToLayer("Water");

                // Resize the plane and kept the same aspect ratio
                float scaleXYZ = Mathf.Max(lbWaterParms.waterSize.x / sizeX, lbWaterParms.waterSize.y / sizeZ);
                lbWaterParms.waterTransform.localScale = scaleXYZ * Vector3.one;

                Renderer renderer = lbWaterParms.waterTransform.GetComponent<Renderer>();
                if (renderer == null) { Debug.LogWarning("LBIntegration.AddCalmWaterToScene - " + lbWaterParms.waterTransform.name + " is missing the rendered"); }
                else
                {
                    // Remove any mesh colliders from the river surface mesh
                    DestroyImmediate(lbWaterParms.waterTransform.GetComponent<MeshCollider>());
                    // Calm Water should not cast shadows
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                    isSuccess = EnableCalmWaterComponents(lbWaterParms.waterTransform, true, showErrors);

                    // Add the caustic prefabs
                    if (isSuccess && lbWaterParms.waterCausticsPrefabList != null)
                    {
                        if (lbWaterParms.waterCausticsPrefabList.Count > 0)
                        {
                            if (lbWaterParms.waterCausticsPrefabList[0] == null)
                            {
                                if (showErrors) { Debug.LogWarning("LBIntegration.AddCalmWaterToScene - could not add Caustics prefab to water in scene"); }
                            }
                            else
                            {
                                Transform primaryCaustics = Instantiate(lbWaterParms.waterCausticsPrefabList[0]);
                                primaryCaustics.position = lbWaterParms.waterTransform.position;
                                primaryCaustics.name = "CausticsProjector";

                                Projector primaryProjector = primaryCaustics.GetComponent<Projector>();
                                if (primaryProjector == null)
                                {
                                    if (showErrors) { Debug.LogWarning("LBIntegration.AddCalmWaterToScene - Projector component is missing on Primary Caustics prefab"); }
                                }
                                else
                                {
                                    primaryProjector.orthographicSize = (lbWater.waterSize.x / 2f);
                                    primaryCaustics.SetParent(lbWaterParms.waterTransform);

                                    // Create a copy of the material and store it in the scene
                                    primaryProjector.material = new Material(primaryProjector.material);
                                }
                            }
                        }
                    }
                }
            }

            return isSuccess;
        }

        /// <summary>
        /// Adds or removes Calm Water components / scripts to a GameObject
        /// NOTE: MirrorReflection crashes Unity in Calm Water 1.5.8 and earlier
        /// Customers need to upgrade to Calm Water 1.5.9 or newer
        /// </summary>
        /// <param name="waterTransform"></param>
        /// <param name="isEnabled"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static bool EnableCalmWaterComponents(Transform waterTransform, bool isEnabled, bool showErrors)
        {
            bool isSuccessful = false;

            System.Type mirrorReflectionType = null;

            try
            {
                mirrorReflectionType = System.Type.GetType("MirrorReflection, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", true, true);
                if (mirrorReflectionType != null)
                {
                    // Check to see if the Calm Water MirrorReflection script is already attached to the transform
                    var mirrorReflectionScript = waterTransform.GetComponent(mirrorReflectionType);
                    // Only add the script if it isn't already attached.
                    if (mirrorReflectionScript == null && isEnabled)
                    {
                        // By default in Calm Water 1.5.8, mirror_reflection layer mask = Everything which will crash Unity (should be fixed in Calm Water 1.5.9)
                        mirrorReflectionScript = waterTransform.gameObject.AddComponent(mirrorReflectionType);

                        if (mirrorReflectionScript != null)
                        {
                            System.Reflection.FieldInfo layerMaskField = mirrorReflectionType.GetField("reflectionMask");
                            LayerMask layerMask = (LayerMask)layerMaskField.GetValue(mirrorReflectionScript);

                            // Set all bits on (Everything), except Water and LB Celestials (25 by default, can be changed in LBCelestrials)
                            layerMask.value = -1 & ~(1 << LayerMask.NameToLayer("Water")) & ~(1 << LBCelestials.celestialsUnityLayer);
                            layerMaskField.SetValue(mirrorReflectionScript, layerMask);
                        }
                    }
                    else if (mirrorReflectionScript != null && !isEnabled)
                    {
                        // remove it.
                        DestroyImmediate(mirrorReflectionScript);
                    }
                    isSuccessful = true;
                }
            }
            catch
            {
                if (showErrors) { Debug.LogWarning("LBIntegration.EnableCalmWaterComponents: it appears that Calm Water 1.5.9 or newer is not installed in this project."); }
            }

            return isSuccessful;
        }

        #endregion

        #region Relief Terrain Pack Integration

        /// <summary>
        /// Is Relief Terrain Pack installed in this project?
        /// </summary>
        /// <returns></returns>
        public static bool isRTPInstalled(bool showErrors)
        {
            bool isInstalled = false;

            System.Type rtpReliefTerrainType = null;

            try
            {
                rtpReliefTerrainType = System.Type.GetType("ReliefTerrain, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", true, true);
                if (rtpReliefTerrainType != null) { isInstalled = true; }
            }
            catch
            {
                if (showErrors)
                {
                    Debug.LogWarning("LBIntegration.isRTPInstalled: it appears that Relief Terrain Pack 3.3 or newer is not installed in this project.");
                }
            }

            return isInstalled;
        }

        /// <summary>
        /// Enable or disable Relief Terrain Pack material/shader on the landscape
        /// Should call isRTPInstalled() and RTPValidation() before calling this method.
        /// Currently support RTP v3.3d
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="terrain"></param>
        /// <param name="isEnabled"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static bool RTPEnable(LBLandscape landscape, Terrain terrain, bool isEnabled, bool isFirstTerrain, bool isLastTerrain, bool showErrors)
        {
            bool isSuccessful = false;

            // Validate input
            if (landscape == null)
            {
                if (showErrors) { Debug.LogError("LBIntegration.RTPEnable: no valid landscape found."); }
            }
            else if (terrain == null)
            {
                if (showErrors) { Debug.LogError("LBIntegration.RTPEnable: no valid terrain found"); }
            }
            else if (landscape.terrainTexturesList == null)
            {
                if (showErrors) { Debug.LogError("LBIntegration.RTPEnable: terrainTexturesList cannot be null"); }
            }
            else if (isEnabled && landscape.rtpUseTessellation && terrain.terrainData == null)
            {
                if (showErrors) { Debug.LogError("LBIntegration.RTPEnable: terrainData cannot be null when RTP Use Tessellation is enabled"); }
            }
            else
            {
                System.Type rtpReliefTerrainType = null;
                System.Type rtpGlobalSettingsHolderType = null;
                System.Type rtpLODmanagerType = null;
#if UNITY_EDITOR
                System.Type rtpLODmanagerEditorType = null;
#endif

                try
                {
                    rtpReliefTerrainType = System.Type.GetType("ReliefTerrain, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", true, true);
                    rtpGlobalSettingsHolderType = System.Type.GetType("ReliefTerrainGlobalSettingsHolder, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", true, true);
                    rtpLODmanagerType = System.Type.GetType("RTP_LODmanager, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", true, true);
#if UNITY_EDITOR
                    rtpLODmanagerEditorType = System.Type.GetType("RTP_LODmanagerEditor, Assembly-CSharp-Editor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", true, true);
#endif

                    if (rtpReliefTerrainType != null && rtpGlobalSettingsHolderType != null && rtpLODmanagerType != null)
                    {
                        // Get the ReliefTerrain script that should be attached to the terrain
                        var rtpReliefTerrain = terrain.GetComponent(rtpReliefTerrainType);

                        // If the ReliefTerrain script is not attached and we are enabling RTP
                        // we will need to attach it to this terrain.
                        if (rtpReliefTerrain == null && isEnabled)
                        {
                            var rtpReliefTerrainAdded = terrain.gameObject.AddComponent(rtpReliefTerrainType);

                            if (landscape.rtpUseTessellation && rtpReliefTerrainAdded != null)
                            {
                                RTPPopulateTessellationMap(terrain, rtpReliefTerrainAdded, showErrors);
                            }

                            // Only update the last terrain. The values will propogate to the other terrains when
                            // RefreshAll() is called.
                            if (isLastTerrain)
                            {
                                if (rtpReliefTerrainAdded != null)
                                {
                                    // Remove disabled texture from the list. Create new list so we don't affect original in LBLandscape
                                    List<LBTerrainTexture> terrainTexturesListNoDisabled = new List<LBTerrainTexture>(landscape.terrainTexturesList);
                                    for (int i = terrainTexturesListNoDisabled.Count - 1; i >= 0; i--)
                                    {
                                        if (terrainTexturesListNoDisabled[i].isDisabled) { terrainTexturesListNoDisabled.Remove(terrainTexturesListNoDisabled[i]); }
                                    }

                                    // After adding at least one Relief Terrain script, the LODmanager should be in the scene
                                    var rtpLODmanager = GameObject.FindObjectOfType(rtpLODmanagerType);

                                    // RTP global settings
                                    if (rtpLODmanager == null) { Debug.LogError("LBIntegration.RTPEnable - could not find RTP LOD Manager in scene"); }
                                    else
                                    {
#if UNITY_EDITOR
                                        // If in the editor, disable selection. If the LODManager Editor is selected, it will override some properties as it will
                                        // read them from the shader when de-selected next time. To prvent this, set dont_sync to true so that when user selects
                                        // it to compile shaders it will retain the settings.
                                        UnityEditor.Selection.activeObject = rtpLODmanager;
#endif

                                        // RTP_4LAYERS_MODE - If there are 4 or less textures, don't use 8 layer (texture) in first pass shader (else we'll see
                                        // a warning in RTP script on each terrain).
                                        bool is4LayerMode = (terrainTexturesListNoDisabled.Count < 5);

                                        rtpLODmanagerType.InvokeMember("dont_sync", System.Reflection.BindingFlags.SetField, null, rtpLODmanager, new object[] { true });

                                        // Set RTP global settings - Only do this on one terrain
                                        rtpLODmanagerType.InvokeMember("RTP_CUT_HOLES", System.Reflection.BindingFlags.SetField, null, rtpLODmanager, new object[] { false });

                                        // Are we using 4 or 8 textures in first shader pass?
                                        rtpLODmanagerType.InvokeMember("RTP_4LAYERS_MODE", System.Reflection.BindingFlags.SetField, null, rtpLODmanager, new object[] { is4LayerMode });

                                        // Set LOD to Parallax (POM)
                                        rtpLODmanagerType.InvokeMember("MAX_LOD_FIRST", System.Reflection.BindingFlags.SetField, null, rtpLODmanager, new object[] { 0 });
                                        rtpLODmanagerType.InvokeMember("MAX_LOD_FIRST_PLUS4", System.Reflection.BindingFlags.SetField, null, rtpLODmanager, new object[] { 0 });
                                        rtpLODmanagerType.InvokeMember("MAX_LOD_ADD", System.Reflection.BindingFlags.SetField, null, rtpLODmanager, new object[] { 0 });

                                        // Appear to need to set some fields in ADD Pass layer before FIRST else FIRST just gets reset to what ADD is to start with.
                                        rtpLODmanagerType.InvokeMember("RTP_SUPER_DETAIL_ADD", System.Reflection.BindingFlags.SetField, null, rtpLODmanager, new object[] { true });
                                        rtpLODmanagerType.InvokeMember("RTP_SUPER_DETAIL_FIRST", System.Reflection.BindingFlags.SetField, null, rtpLODmanager, new object[] { true });
                                        rtpLODmanagerType.InvokeMember("RTP_TESSELLATION", System.Reflection.BindingFlags.SetField, null, rtpLODmanager, new object[] { landscape.rtpUseTessellation });
                                        // Currently there is an edge terrain issue with bicubic filering with the edges of multiple terrains - turn it off for now.
                                        rtpLODmanagerType.InvokeMember("RTP_HEIGHTMAP_SAMPLE_BICUBIC", System.Reflection.BindingFlags.SetField, null, rtpLODmanager, new object[] { false });
                                    }

                                    var globalSettingsHolder = rtpReliefTerrainType.InvokeMember("globalSettingsHolder", System.Reflection.BindingFlags.GetField, null, rtpReliefTerrainAdded, new object[] { });
                                    if (globalSettingsHolder != null)
                                    {
                                        // Get normalmap and heightmap array field so we can fill them.
                                        Texture2D[] Bumps = (Texture2D[])rtpGlobalSettingsHolderType.InvokeMember("Bumps", System.Reflection.BindingFlags.GetField, null, globalSettingsHolder, new object[] { });
                                        Texture2D[] Heights = (Texture2D[])rtpGlobalSettingsHolderType.InvokeMember("Heights", System.Reflection.BindingFlags.GetField, null, globalSettingsHolder, new object[] { });

                                        if (Bumps != null && Heights != null)
                                        {
                                            if (terrainTexturesListNoDisabled != null)
                                            {
                                                int numTextures = terrainTexturesListNoDisabled.Count;
                                                if (numTextures > 0)
                                                {
                                                    // RTP uses a single tile size - select the first one
                                                    Vector2 tileSize = terrainTexturesListNoDisabled[0].tileSize;
                                                    // ReliefTranform stores the tileSize and the tileOffset as two Vector2s in a Vector4.
                                                    Vector4 reliefTransform = new Vector4(tileSize.x, tileSize.y, 0f, 0f);
                                                    rtpGlobalSettingsHolderType.InvokeMember("ReliefTransform", System.Reflection.BindingFlags.SetField, null, globalSettingsHolder, new object[] { reliefTransform });

                                                    float nearDistanceStart = 5f, mipsLevelBias = 0f;
                                                    if (landscape.rtpUseTessellation)
                                                    {
                                                        nearDistanceStart = 50f;
                                                        mipsLevelBias = -0.5f;
                                                    }
                                                    rtpGlobalSettingsHolderType.InvokeMember("distance_start", System.Reflection.BindingFlags.SetField, null, globalSettingsHolder, new object[] { nearDistanceStart });
                                                    rtpGlobalSettingsHolderType.InvokeMember("RTP_MIP_BIAS", System.Reflection.BindingFlags.SetField, null, globalSettingsHolder, new object[] { mipsLevelBias });

                                                    #region Process normal maps
                                                    for (int nm = 0; nm < numTextures; nm++)
                                                    {
                                                        // Set the Normal Map for this LBTexture
                                                        Bumps[nm] = terrainTexturesListNoDisabled[nm].normalMap;

                                                        // To CombineNormals, all normalMaps must be readable
                                                        #if UNITY_EDITOR
                                                        LBTextureOperations.EnableReadable(Bumps[nm], true, false);
                                                        #endif

                                                        float grey = 128f / 255f;

                                                        // Combine in pairs to show up in RTP Combined Textures, Normal Maps section of RTP script
                                                        // Check for odd numbers
                                                        if (nm % 2 == 1)
                                                        {
                                                            // If they are both null, there is nothing to combine
                                                            if (Bumps[nm - 1] != null || Bumps[nm] != null)
                                                            {
                                                                Texture2D texture1, texture2;
                                                                // If the previous normal map was null, create a grey normal map
                                                                // for this texture so that it can be combined with the next one
                                                                if (Bumps[nm - 1] == null)
                                                                {
                                                                    texture1 = LBTextureOperations.CreateTexture(Bumps[nm].width, Bumps[nm].height, new Color(grey, grey, grey, grey));
                                                                    texture2 = Bumps[nm];
                                                                }
                                                                else if (Bumps[nm] == null)
                                                                {
                                                                    texture1 = Bumps[nm - 1];
                                                                    texture2 = LBTextureOperations.CreateTexture(Bumps[nm - 1].width, Bumps[nm - 1].height, new Color(grey, grey, grey, grey));
                                                                }
                                                                else
                                                                {
                                                                    texture1 = Bumps[nm - 1];
                                                                    texture2 = Bumps[nm];
                                                                }

                                                                // RTP 3.3d CombineNormals() is private. So check of Public and Private method of same name.
                                                                Texture2D combinedBump = (Texture2D)rtpGlobalSettingsHolderType.InvokeMember("CombineNormals", System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public, null, globalSettingsHolder, new object[] { texture1, texture2 });

                                                                // Set the combined bump fields named Bump01/23/45/67/89/AB
                                                                string bumpFieldName = "Bump" + (nm - 1).ToString() + nm.ToString();
                                                                if (nm == 11) { bumpFieldName = "BumpAB"; }

                                                                // RTP expects 4, 8 or 12 textures
                                                                if (nm < 12)
                                                                {
                                                                    rtpGlobalSettingsHolderType.InvokeMember(bumpFieldName, System.Reflection.BindingFlags.SetField, null, globalSettingsHolder, new object[] { combinedBump });
                                                                }
                                                            }
                                                        }
                                                        else if (nm == numTextures - 1 && Bumps[nm] != null)
                                                        {
                                                            // If there is an uneven number of textures, we need to combine the last one with a grey texture.

                                                            Texture2D greyTexture2D = LBTextureOperations.CreateTexture(Bumps[nm].width, Bumps[nm].height, new Color(grey, grey, grey, grey));

                                                            Texture2D combinedBump = (Texture2D)rtpGlobalSettingsHolderType.InvokeMember("CombineNormals", System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public, null, globalSettingsHolder, new object[] { Bumps[nm], greyTexture2D });

                                                            // Set the combined bump fields named Bump01/23/45/67/89/AB
                                                            string bumpFieldName = "Bump" + nm.ToString() + (nm + 1).ToString();
                                                            if (nm == 10) { bumpFieldName = "BumpAB"; }

                                                            // RTP expects 4, 8 or 12 textures
                                                            // The last even number is 10 (which is the eleventh texture). The Grey texture will be the 12th slot
                                                            if (nm < 11)
                                                            {
                                                                rtpGlobalSettingsHolderType.InvokeMember(bumpFieldName, System.Reflection.BindingFlags.SetField, null, globalSettingsHolder, new object[] { combinedBump });
                                                            }
                                                        }
                                                    }
                                                    #endregion

                                                    #region Process heightmaps
                                                    // Get the minimum width of the heightMaps
                                                    int minWidth = 100000;
                                                    for (int hm = 0; hm < numTextures; hm++)
                                                    {
                                                        if (Heights[hm] != null) { if (Heights[hm].width < minWidth) minWidth = Heights[hm].width; }
                                                    }
                                                    if (minWidth == 100000) { minWidth = 256; }

                                                    for (int hm = 0; hm < numTextures; hm++)
                                                    {
                                                        // Set the Height Map for this LBTexture
                                                        Heights[hm] = terrainTexturesListNoDisabled[hm].heightMap;

                                                        // To CombineHeights, all heightMaps must be readable
#if UNITY_EDITOR
                                                        LBTextureOperations.EnableReadable(Heights[hm], true, false);
#endif

                                                        // If the heightMap is empty, create one with a solid color at max height
                                                        if (Heights[hm] == null)
                                                        {
                                                            Heights[hm] = LBTextureOperations.CreateTexture(minWidth, minWidth, new Color(1f, 1f, 1f, 1f));
                                                        }
                                                    }

                                                    // Heights need to be filled in blocks of 4 (i.e. 4, 8, 12)
                                                    int numPasses = 1;

                                                    // RTP (and Unity) currently doesn't handle more than 12 textures
                                                    if (numTextures > 8) { numPasses = 3; }
                                                    else if (numTextures > 4) { numPasses = 2; }

                                                    // Create the combined heightmaps
                                                    for (int pass = 0; pass < numPasses; pass++)
                                                    {
                                                        List<Texture2D> heightMapsToCombine = new List<Texture2D>();
                                                        Texture2D combinedHeightmap = new Texture2D(minWidth, minWidth, TextureFormat.ARGB32, true);
                                                        string combinedFieldName = "HeightMap";
                                                        if (combinedHeightmap != null)
                                                        {
                                                            for (int hm = pass * 4; hm < pass * 4 + 4; hm++)
                                                            {
                                                                // Is this in the current block but beyond the number of Textures in the landscape
                                                                if (hm > numTextures - 1)
                                                                {
                                                                    heightMapsToCombine.Add(LBTextureOperations.CreateTexture(minWidth, minWidth, new Color(1f, 1f, 1f, 1f)));
                                                                }
                                                                else { heightMapsToCombine.Add(Heights[hm]); }
                                                            }

                                                            // Use the Red channel as the source of the height data from each texture
                                                            byte[] colsR = LBTextureOperations.GetTextureColourChannel(heightMapsToCombine[0], 'R');
                                                            byte[] colsG = LBTextureOperations.GetTextureColourChannel(heightMapsToCombine[1], 'R');
                                                            byte[] colsB = LBTextureOperations.GetTextureColourChannel(heightMapsToCombine[2], 'R');
                                                            byte[] colsA = LBTextureOperations.GetTextureColourChannel(heightMapsToCombine[3], 'R');
                                                            Color32[] pixelColours = combinedHeightmap.GetPixels32();
                                                            for (int i = 0; i < pixelColours.Length; i++)
                                                            {
                                                                pixelColours[i].r = colsR[i];
                                                                pixelColours[i].g = colsG[i];
                                                                pixelColours[i].b = colsB[i];
                                                                pixelColours[i].a = colsA[i];
                                                            }
                                                            combinedHeightmap.SetPixels32(pixelColours);

                                                            if (pass == 0) { combinedFieldName = "HeightMap"; }         // Heights 0-3
                                                            else if (pass == 1) { combinedFieldName = "HeightMap2"; }   // Heights 4-7
                                                            else if (pass == 2) { combinedFieldName = "HeightMap3"; }   // Heights 8-11

                                                            if (pass < 3)
                                                            {
                                                                rtpGlobalSettingsHolderType.InvokeMember(combinedFieldName, System.Reflection.BindingFlags.SetField, null, globalSettingsHolder, new object[] { combinedHeightmap });
                                                            }
                                                        }
                                                    }

                                                    #endregion
                                                }
                                            }

                                            if (landscape.rtpUseTessellation)
                                            {
                                                // Tessellation Substeps (Close and Far)
                                                rtpGlobalSettingsHolderType.InvokeMember("_TessSubdivisions", System.Reflection.BindingFlags.SetField, null, globalSettingsHolder, new object[] { 6 });
                                                rtpGlobalSettingsHolderType.InvokeMember("_TessSubdivisionsFar", System.Reflection.BindingFlags.SetField, null, globalSettingsHolder, new object[] { 20 });
                                            }

                                            // Should now combine in pairs to show up in RTP Combined Textures, Normal Maps section of RTP script
                                            rtpGlobalSettingsHolderType.InvokeMember("RefreshAll", System.Reflection.BindingFlags.InvokeMethod, null, globalSettingsHolder, new object[] { });

#if UNITY_EDITOR
                                            if (rtpLODmanagerEditorType != null)
                                            {
                                                // If in the editor, attempt to locate the RPT LOB Manager Editor script
                                                var editor = Resources.FindObjectsOfTypeAll(rtpLODmanagerEditorType);
                                                if (editor != null)
                                                {
                                                    if (editor.Length >= 0)
                                                    {
                                                        // Compile the shaders using the new settings
                                                        rtpLODmanagerEditorType.InvokeMember("RefreshFeatures", System.Reflection.BindingFlags.InvokeMethod, null, editor[0], new object[] { });
                                                    }
                                                }
                                            }
#endif
                                        }
                                    }
                                }
                            }
                        }
                        // If it is attached and we're disabling RTP we need to remove it
                        else if (rtpReliefTerrain != null && !isEnabled)
                        {
                            DestroyImmediate(rtpReliefTerrain);
                        }

                        isSuccessful = true;
                    }
                }
                catch (Exception ex)
                {
                    if (showErrors) { Debug.LogWarning("LBIntegration.RTPEnable: something went wrong with Relief Terrain Pack Integration. " + ex.Message); }
                }
            }

            return isSuccessful;
        }


        /// <summary>
        /// Update RTP textures and associated settings for the landscape.
        /// NOTE: There are issues switching from 4 to 5+ and 5+ to 4 or less
        /// textures. This is due to RTP_4LAYERS_MODE not being set correctly.
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="showErrors"></param>
        public static void RTPUpdateTextures(LBLandscape landscape, bool showErrors)
        {
            string methodName = "LBIntegration.RTPUpdateTextures";

            // Validate input
            if (landscape == null)
            {
                if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " no valid landscape found. PLEASE REPORT"); }
            }
            else if (landscape.landscapeTerrains == null)
            {
                if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " no terrains found. PLEASE REPORT"); }
            }
            else if (landscape.terrainTexturesList == null)
            {
                if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " terrainTexturesList cannot be null. PLEASE REPORT"); }
            }
            else
            {
                System.Type rtpReliefTerrainType = null;
                System.Type rtpGlobalSettingsHolderType = null;
                System.Type rtpLODmanagerType = null;
                #if UNITY_EDITOR
                System.Type rtpLODmanagerEditorType = null;
                #endif

                List<LBTerrainTexture> enabledTextureTerrainList = landscape.terrainTexturesList.FindAll(tx => !tx.isDisabled);

                // Only update the last terrain. The values will propogate to the other terrains when RefreshAll is called
                Terrain terrain = landscape.landscapeTerrains[landscape.landscapeTerrains.Length - 1];

                // How many enabled textures do we have in the landscape?
                int numLBTextures = (enabledTextureTerrainList == null ? 0 : enabledTextureTerrainList.Count);

                // RTP expects 0, 4, 8 or 12 textures.
                //int base4Textures = (numLBTextures == 0 ? 0 : (int)Math.Ceiling(numLBTextures / 4f)) * 4;

                try
                {
                    rtpReliefTerrainType = System.Type.GetType("ReliefTerrain, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", true, true);
                    rtpGlobalSettingsHolderType = System.Type.GetType("ReliefTerrainGlobalSettingsHolder, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", true, true);
                    rtpLODmanagerType = System.Type.GetType("RTP_LODmanager, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", true, true);
                    #if UNITY_EDITOR
                    rtpLODmanagerEditorType = System.Type.GetType("RTP_LODmanagerEditor, Assembly-CSharp-Editor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", true, true);
                    #endif

                    if (rtpReliefTerrainType != null && rtpGlobalSettingsHolderType != null && rtpLODmanagerType != null)
                    {

                        // Get the ReliefTerrain script that should be attached to the terrain
                        var rtpReliefTerrain = terrain.GetComponent(rtpReliefTerrainType);

                        if (rtpReliefTerrain == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " could not find RTP Relief Terrain script on terrain"); } }
                        else
                        {
                            // After adding at least one Relief Terrain script, the LODmanager should be in the scene
                            var rtpLODmanager = GameObject.FindObjectOfType(rtpLODmanagerType);

                            // RTP global settings
                            if (rtpLODmanager == null) { Debug.LogWarning("ERROR: " + methodName + " - could not find RTP LOD Manager in scene"); }
                            else
                            {
                                #if UNITY_EDITOR
                                // If in the editor, disable selection. If the LODManager Editor is selected, it will override some properties as it will
                                // read them from the shader when de-selected next time. To prvent this, set dont_sync to true so that when user selects
                                // it to compile shaders it will retain the settings.
                                UnityEditor.Selection.activeObject = rtpLODmanager;
                                #endif

                                // RTP_4LAYERS_MODE - If there are 4 or less textures, don't use 8 layer (texture) in first pass shader (else we'll see
                                // a warning in RTP script on each terrain).
                                bool is4LayerMode = numLBTextures < 5;

                                rtpLODmanagerType.InvokeMember("dont_sync", System.Reflection.BindingFlags.SetField, null, rtpLODmanager, new object[] { true });

                                // Are we using 4 or 8 textures in first shader pass?
                                if (is4LayerMode)
                                {
                                    // If it was previous on (5+ textures), turn it off if no longer required.
                                    // Don't enable it before the 5+ textures have been copied to RTP or there will be problems
                                    rtpLODmanagerType.InvokeMember("RTP_4LAYERS_MODE", System.Reflection.BindingFlags.SetField, null, rtpLODmanager, new object[] { is4LayerMode });
                                }

                                #if UNITY_EDITOR
                                bool is4LayerModePrev = (bool)rtpLODmanagerType.InvokeMember("RTP_4LAYERS_MODE", System.Reflection.BindingFlags.GetField, null, rtpLODmanager, new object[] { });
                                //Debug.Log("[DEBUG] is4LayerModePrev: " + is4LayerModePrev);

                                // If we've change from > 4 textures to < 5 we need to refresh features before refreshing the textures.
                                if (is4LayerMode != is4LayerModePrev && !is4LayerMode && rtpLODmanagerEditorType != null)
                                {
                                    // If in the editor, attempt to locate the RPT LOB Manager Editor script
                                    var editor = Resources.FindObjectsOfTypeAll(rtpLODmanagerEditorType);
                                    if (editor != null)
                                    {
                                        if (editor.Length >= 0)
                                        {
                                            // Compile the shaders using the new settings
                                            rtpLODmanagerEditorType.InvokeMember("RefreshFeatures", System.Reflection.BindingFlags.InvokeMethod, null, editor[0], new object[] { });
                                            UnityEditor.Selection.activeObject = rtpLODmanager;
                                        }
                                    }
                                }
                                #endif

                                var globalSettingsHolder = rtpReliefTerrainType.InvokeMember("globalSettingsHolder", System.Reflection.BindingFlags.GetField, null, rtpReliefTerrain, new object[] { });

                                if (globalSettingsHolder == null) { Debug.LogWarning("ERROR: " + methodName + " - globalSettingsHolder not found. PLEASE REPORT"); }
                                else
                                {
                                    // Set textures, normalmap and heightmap array field so we can update them.
                                    Texture2D[] splats = new Texture2D[numLBTextures];
                                    Texture2D[] Bumps = new Texture2D[numLBTextures];
                                    Texture2D[] Heights = new Texture2D[numLBTextures];

                                    if (numLBTextures == 0)
                                    {
                                        // Probably should clear RTP...
                                        rtpGlobalSettingsHolderType.InvokeMember("splats", System.Reflection.BindingFlags.SetField, null, globalSettingsHolder, new object[] { splats });
                                        // Update the Normalmap textures in RTP
                                        rtpGlobalSettingsHolderType.InvokeMember("Bumps", System.Reflection.BindingFlags.SetField, null, globalSettingsHolder, new object[] { Bumps });
                                        // Update the Height textures in RTP
                                        rtpGlobalSettingsHolderType.InvokeMember("Heights", System.Reflection.BindingFlags.SetField, null, globalSettingsHolder, new object[] { Heights });
                                    }
                                    else
                                    {
                                        // RTP uses a single tile size - select the first one
                                        Vector2 tileSize = enabledTextureTerrainList[0].tileSize;
                                        // ReliefTranform stores the tileSize and the tileOffset as two Vector2s in a Vector4.
                                        Vector4 reliefTransform = new Vector4(tileSize.x, tileSize.y, 0f, 0f);
                                        rtpGlobalSettingsHolderType.InvokeMember("ReliefTransform", System.Reflection.BindingFlags.SetField, null, globalSettingsHolder, new object[] { reliefTransform });

                                        #region Set splats (Textures)
                                        for (int tIdx = 0; tIdx < numLBTextures; tIdx++)
                                        {
                                            splats[tIdx] = enabledTextureTerrainList[tIdx].texture;
                                        }
                                        rtpGlobalSettingsHolderType.InvokeMember("splats", System.Reflection.BindingFlags.SetField, null, globalSettingsHolder, new object[] { splats });
                                        #endregion

                                        #region Process normal maps
                                        for (int nm = 0; nm < numLBTextures; nm++)
                                        {
                                            // Set the Normal Map for this LBTexture
                                            Bumps[nm] = enabledTextureTerrainList[nm].normalMap;

                                            // To CombineNormals, all normalMaps must be readable
                                            #if UNITY_EDITOR
                                            if (Bumps[nm] != null) {LBTextureOperations.EnableReadable(Bumps[nm], true, false); }
                                            #endif

                                            float grey = 128f / 255f;

                                            // Combine in pairs to show up in RTP Combined Textures, Normal Maps section of RTP script
                                            // Check for odd numbers
                                            if (nm % 2 == 1)
                                            {
                                                // If they are both null, there is nothing to combine
                                                if (Bumps[nm - 1] != null || Bumps[nm] != null)
                                                {
                                                    Texture2D texture1, texture2;
                                                    // If the previous normal map was null, create a grey normal map
                                                    // for this texture so that it can be combined with the next one
                                                    if (Bumps[nm - 1] == null)
                                                    {
                                                        texture1 = LBTextureOperations.CreateTexture(Bumps[nm].width, Bumps[nm].height, new Color(grey, grey, grey, grey));
                                                        texture2 = Bumps[nm];
                                                    }
                                                    else if (Bumps[nm] == null)
                                                    {
                                                        texture1 = Bumps[nm - 1];
                                                        texture2 = LBTextureOperations.CreateTexture(Bumps[nm - 1].width, Bumps[nm - 1].height, new Color(grey, grey, grey, grey));
                                                    }
                                                    else
                                                    {
                                                        texture1 = Bumps[nm - 1];
                                                        texture2 = Bumps[nm];
                                                    }

                                                    // RTP 3.3d CombineNormals() is private. So check of Public and Private method of same name.
                                                    Texture2D combinedBump = (Texture2D)rtpGlobalSettingsHolderType.InvokeMember("CombineNormals", System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public, null, globalSettingsHolder, new object[] { texture1, texture2 });

                                                    // Set the combined bump fields named Bump01/23/45/67/89/AB
                                                    string bumpFieldName = "Bump" + (nm - 1).ToString() + nm.ToString();
                                                    if (nm == 11) { bumpFieldName = "BumpAB"; }

                                                    // RTP expects 4, 8 or 12 textures
                                                    if (nm < 12)
                                                    {
                                                        rtpGlobalSettingsHolderType.InvokeMember(bumpFieldName, System.Reflection.BindingFlags.SetField, null, globalSettingsHolder, new object[] { combinedBump });
                                                    }
                                                }
                                            }
                                            else if (nm == numLBTextures - 1 && Bumps[nm] != null)
                                            {
                                                // If there is an uneven number of textures, we need to combine the last one with a grey texture.

                                                Texture2D greyTexture2D = LBTextureOperations.CreateTexture(Bumps[nm].width, Bumps[nm].height, new Color(grey, grey, grey, grey));

                                                Texture2D combinedBump = (Texture2D)rtpGlobalSettingsHolderType.InvokeMember("CombineNormals", System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public, null, globalSettingsHolder, new object[] { Bumps[nm], greyTexture2D });

                                                // Set the combined bump fields named Bump01/23/45/67/89/AB
                                                string bumpFieldName = "Bump" + nm.ToString() + (nm + 1).ToString();
                                                if (nm == 10) { bumpFieldName = "BumpAB"; }

                                                // RTP expects 4, 8 or 12 textures
                                                // The last even number is 10 (which is the eleventh texture). The Grey texture will be the 12th slot
                                                if (nm < 11)
                                                {
                                                    rtpGlobalSettingsHolderType.InvokeMember(bumpFieldName, System.Reflection.BindingFlags.SetField, null, globalSettingsHolder, new object[] { combinedBump });
                                                }
                                            }
                                        }

                                        // Update the Normalmap textures in RTP
                                        rtpGlobalSettingsHolderType.InvokeMember("Bumps", System.Reflection.BindingFlags.SetField, null, globalSettingsHolder, new object[] { Bumps });

                                        #endregion

                                        #region Process heightmaps
                                        // Get the minimum width of the heightMaps
                                        int minWidth = 100000;
                                        for (int hm = 0; hm < numLBTextures; hm++)
                                        {
                                            if (Heights[hm] != null) { if (Heights[hm].width < minWidth) minWidth = Heights[hm].width; }
                                        }
                                        if (minWidth == 100000) { minWidth = 256; }

                                        for (int hm = 0; hm < numLBTextures; hm++)
                                        {
                                            // Set the Height Map for this LBTexture
                                            Heights[hm] = enabledTextureTerrainList[hm].heightMap;

                                            // To CombineHeights, all heightMaps must be readable
                                            #if UNITY_EDITOR
                                            LBTextureOperations.EnableReadable(Heights[hm], true, false);
                                            #endif

                                            // If the heightMap is empty, create one with a solid color at max height
                                            if (Heights[hm] == null)
                                            {
                                                Heights[hm] = LBTextureOperations.CreateTexture(minWidth, minWidth, new Color(1f, 1f, 1f, 1f));
                                            }
                                        }

                                        // Update the Height textures in RTP
                                        rtpGlobalSettingsHolderType.InvokeMember("Heights", System.Reflection.BindingFlags.SetField, null, globalSettingsHolder, new object[] { Heights });

                                        // Heights need to be filled in blocks of 4 (i.e. 4, 8, 12)
                                        int numPasses = 1;

                                        // RTP (and Unity) currently doesn't handle more than 12 textures
                                        if (numLBTextures > 8) { numPasses = 3; }
                                        else if (numLBTextures > 4) { numPasses = 2; }

                                        // Create the combined heightmaps
                                        for (int pass = 0; pass < numPasses; pass++)
                                        {
                                            List<Texture2D> heightMapsToCombine = new List<Texture2D>();
                                            Texture2D combinedHeightmap = new Texture2D(minWidth, minWidth, TextureFormat.ARGB32, true);
                                            string combinedFieldName = "HeightMap";
                                            if (combinedHeightmap != null)
                                            {
                                                for (int hm = pass * 4; hm < pass * 4 + 4; hm++)
                                                {
                                                    // Is this in the current block but beyond the number of Textures in the landscape
                                                    if (hm > numLBTextures - 1)
                                                    {
                                                        heightMapsToCombine.Add(LBTextureOperations.CreateTexture(minWidth, minWidth, new Color(1f, 1f, 1f, 1f)));
                                                    }
                                                    else { heightMapsToCombine.Add(Heights[hm]); }
                                                }

                                                // Use the Red channel as the source of the height data from each texture
                                                byte[] colsR = LBTextureOperations.GetTextureColourChannel(heightMapsToCombine[0], 'R');
                                                byte[] colsG = LBTextureOperations.GetTextureColourChannel(heightMapsToCombine[1], 'R');
                                                byte[] colsB = LBTextureOperations.GetTextureColourChannel(heightMapsToCombine[2], 'R');
                                                byte[] colsA = LBTextureOperations.GetTextureColourChannel(heightMapsToCombine[3], 'R');
                                                Color32[] pixelColours = combinedHeightmap.GetPixels32();
                                                for (int i = 0; i < pixelColours.Length; i++)
                                                {
                                                    pixelColours[i].r = colsR[i];
                                                    pixelColours[i].g = colsG[i];
                                                    pixelColours[i].b = colsB[i];
                                                    pixelColours[i].a = colsA[i];
                                                }
                                                combinedHeightmap.SetPixels32(pixelColours);

                                                if (pass == 0) { combinedFieldName = "HeightMap"; }         // Heights 0-3
                                                else if (pass == 1) { combinedFieldName = "HeightMap2"; }   // Heights 4-7
                                                else if (pass == 2) { combinedFieldName = "HeightMap3"; }   // Heights 8-11

                                                if (pass < 3)
                                                {
                                                    rtpGlobalSettingsHolderType.InvokeMember(combinedFieldName, System.Reflection.BindingFlags.SetField, null, globalSettingsHolder, new object[] { combinedHeightmap });
                                                }
                                            }
                                        }

                                        #endregion
                                    }

                                    //#if UNITY_EDITOR
                                    //// If we've change from > 4 textures to < 5 we need to refresh features before refreshing the textures.
                                    //if (is4LayerMode != is4LayerModePrev && !is4LayerMode && rtpLODmanagerEditorType != null)
                                    //{
                                    //    // If in the editor, attempt to locate the RPT LOB Manager Editor script
                                    //    var editor = Resources.FindObjectsOfTypeAll(rtpLODmanagerEditorType);
                                    //    if (editor != null)
                                    //    {
                                    //        if (editor.Length >= 0)
                                    //        {
                                    //            // Compile the shaders using the new settings
                                    //            rtpLODmanagerEditorType.InvokeMember("RefreshFeatures", System.Reflection.BindingFlags.InvokeMethod, null, editor[0], new object[] { });
                                    //        }
                                    //    }
                                    //}
                                    //#endif
                                    rtpLODmanagerType.InvokeMember("RTP_4LAYERS_MODE", System.Reflection.BindingFlags.SetField, null, rtpLODmanager, new object[] { is4LayerMode });

                                    // Refresh so Combined Textures, Normal Maps etc show up in RTP script
                                    rtpGlobalSettingsHolderType.InvokeMember("RefreshAll", System.Reflection.BindingFlags.InvokeMethod, null, globalSettingsHolder, new object[] { });

                                }
                            }
                        }


                    }

                }

                catch (Exception ex)
                {
                    if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " something went wrong with RTP Integration. Please try again. " + ex.Message); }
                }
            }
        }

        /// <summary>
        /// Tessellation is based on the heightmap of each terrain. In the ReliefTerrain editor, under Settings,
        /// on the Global Maps tab, you can find configuration for Tessellation height & normal map settings
        /// </summary>
        /// <param name="terrain"></param>
        /// <param name="rtpReliefTerrainAdded"></param>
        /// <param name="showErrors"></param>
        private static void RTPPopulateTessellationMap(Terrain terrain, Component rtpReliefTerrainAdded, bool showErrors)
        {
            if (terrain == null)
            {
                if (showErrors) { Debug.LogWarning("LBIntegration.RTPPopulateTessellationMap: terrain cannot be null"); }
            }
            else
            {
                System.Type rtpReliefTerrainType = null;

                try
                {
                    rtpReliefTerrainType = System.Type.GetType("ReliefTerrain, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", true, true);

                    if (rtpReliefTerrainType != null)
                    {
                        // Create the heightmap texture from the terrain data
                        Texture2D tessellationHeightmap = LBLandscapeTerrain.ExportImageBasedHeightmapSingleTerrain(terrain.terrainData, true);
                        if (tessellationHeightmap == null)
                        {
                            if (showErrors) { Debug.LogWarning("LBIntegration.RTPEnable: could not create tessellation heightmap from terrain"); }
                        }
                        else
                        {
                            // Copy the terrain heightmap normals texture into the NormalGlobal texture
                            rtpReliefTerrainType.InvokeMember("NormalGlobal", System.Reflection.BindingFlags.SetField, null, rtpReliefTerrainAdded, new object[] { tessellationHeightmap });
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (showErrors) { Debug.LogWarning("LBIntegration.RTPPopulateTessellationMap: something went wrong with Relief Terrain Pack Integration. " + ex.Message); }
                }
            }
        }

        /// <summary>
        /// Call this if the topography has changed and RTP Tessellation is enabled.
        /// This is required when tessellation is enabled because RTP takes into consideration the heights of the terrain
        /// in the shader. It gets this data from the tessellation texture.
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="landscapeTerrains"></param>
        /// <param name="showErrors"></param>
        public static void RTPUpdateTessellationMaps(LBLandscape landscape, Terrain[] landscapeTerrains, bool showErrors)
        {
            // Validate input
            if (landscape == null)
            {
                if (showErrors) { Debug.LogError("LBIntegration.RTPUpdateTessellationMaps: no valid landscape found."); }
            }
            else if (!landscape.rtpUseTessellation)
            {
                // Do nothing - no need to continue
            }
            else if (landscapeTerrains == null)
            {
                if (showErrors) { Debug.LogError("LBIntegration.RTPUpdateTessellationMaps: terrains cannot be null"); }
            }
            else if (landscapeTerrains.Length > 0)
            {
                System.Type rtpReliefTerrainType = null;

                try
                {
                    rtpReliefTerrainType = System.Type.GetType("ReliefTerrain, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", true, true);

                    if (rtpReliefTerrainType != null)
                    {
                        foreach (Terrain terrain in landscapeTerrains)
                        {
                            if (terrain == null) { continue; }
                            var rtpReliefTerrain = terrain.gameObject.GetComponent(rtpReliefTerrainType);

                            if (rtpReliefTerrain != null)
                            {
                                RTPPopulateTessellationMap(terrain, rtpReliefTerrain, showErrors);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (showErrors) { Debug.LogWarning("LBIntegration.RTPUpdateTessellationMaps: something went wrong with Relief Terrain Pack Integration. " + ex.Message); }
                }
            }
        }

        /// <summary>
        /// Call this to validate the current landscape before attempting to enable RTP
        /// Currently checks to see if heightmap textures are compatible with Tessellation
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static bool RTPValidation(LBLandscape landscape, bool showErrors)
        {
            bool isSuccessful = false;
            bool isValidateHeightmaps = true;

            // Validate input
            if (landscape == null)
            {
                Debug.LogError("LBIntegration.RTPValidation: no valid landscape found.");
            }
            else if (landscape.terrainTexturesList == null)
            {
                Debug.LogError("LBIntegration.RTPValidation: terrainTexturesList cannot be null");
            }
            else
            {
                // Make sure all heightmaps are the same size
                if (landscape.rtpUseTessellation)
                {
                    // Remove disabled texture from the list. Create new list so we don't affect original in LBLandscape
                    List<LBTerrainTexture> terrainTexturesListNoDisabled = new List<LBTerrainTexture>(landscape.terrainTexturesList);
                    for (int i = terrainTexturesListNoDisabled.Count - 1; i >= 0; i--)
                    {
                        if (terrainTexturesListNoDisabled[i].isDisabled) { terrainTexturesListNoDisabled.Remove(terrainTexturesListNoDisabled[i]); }
                    }

                    if (terrainTexturesListNoDisabled.Count > 1)
                    {
                        Vector2 heightMapSize = Vector2.zero;
                        foreach (LBTerrainTexture lbTerrainTexture in terrainTexturesListNoDisabled)
                        {
                            if (lbTerrainTexture.heightMap != null)
                            {
                                // If this is the first heightmap set the heightMapSize
                                if (heightMapSize == Vector2.zero) { heightMapSize.x = lbTerrainTexture.heightMap.width; heightMapSize.y = lbTerrainTexture.heightMap.height; }
                                // Compare this heightMap size with the 
                                else if (lbTerrainTexture.heightMap.width != heightMapSize.x || lbTerrainTexture.heightMap.height != heightMapSize.y)
                                {
                                    if (showErrors) { Debug.Log("LBIntegration.RTPValidation - all heightmaps need to be the same size"); }
                                    isValidateHeightmaps = false; break;
                                }
                            }
                        }
                    }
                    isSuccessful = isValidateHeightmaps;
                }
                else { isSuccessful = true; }
            }

            return isSuccessful;
        }

        #endregion

        #region MegaSplat Integration

        /// <summary>
        /// Is MegaSplat installed in this project?
        /// </summary>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static bool IsMegaSplatInstalled(bool showErrors)
        {
            bool isInstalled = false;

            #if __MEGASPLAT__
            isInstalled = true;
            #endif

            return isInstalled;
        }

        /// <summary>
        /// Get the installed version of MegaSplat
        /// </summary>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static string MegaSplatGetVersion(bool showErrors)
        {
            string version = string.Empty;

#if UNITY_EDITOR

            System.Type SplatArrayShaderGUIType = null;

            try
            {
                SplatArrayShaderGUIType = System.Type.GetType("SplatArrayShaderGUI, Assembly-CSharp-Editor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", true, true);

                if (SplatArrayShaderGUIType == null)
                {
                    if (showErrors) { Debug.LogWarning("LBIntegration.MegaSplatGetVersion - could not find SplatArrayShaderGUI editor class. Please Report."); }
                }
                else
                {
                    version = (string)SplatArrayShaderGUIType.GetField("MegaSplatVersion").GetValue(null);
                }
            }
            catch (Exception ex)
            {
                if (showErrors) { Debug.LogWarning("LBIntegration.MegaSplatGetVersion something has gone wrong (MegaSplat 1.73+ required). Please report. " + ex.Message); }
            }

#endif
            return version;
        }

        /// <summary>
        /// Add (or remove) MegaSplat material to a terrain within a landscape
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="terrain"></param>
        /// <param name="isEnabled"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static bool MegaSplatEnable(LBLandscape landscape, Terrain terrain, Material megaSplatMaterial, bool isEnabled, bool showErrors)
        {
            bool isSuccessful = false;
            string methodName = "LBIntegration.MegaSplatEnable";

            // Basic validation
            if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscape cannot be null"); } }
            else if (terrain == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - terrain cannot be null"); } }
            else if (!IsMegaSplatInstalled(true)) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - MegaSplat 1.73 or newer does not seem to be installed in the project."); } }
            else if (isEnabled)
            {
                //Debug.Log("MegaSplatEnable - enabling");
                if (megaSplatMaterial != null)
                {
                    #if !UNITY_2019_2_OR_NEWER
                    terrain.materialType = Terrain.MaterialType.Custom;
                    #endif
                    terrain.materialTemplate = megaSplatMaterial;
                    landscape.terrainCustomMaterial = megaSplatMaterial;
                    isSuccessful = true;
                }
                else { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscape.terrainCustomMaterial cannot be null. Please Report"); } }
            }
            else
            {
                // De-select the custom material
                landscape.terrainCustomMaterial = null;

                // if there is a MegaSplat terrain manager attached to the terrain, attempt to remove it
                System.Type terrainManagerType = MegaSplatTerrainManagerType(true);
                if (terrainManagerType != null)
                {
                    Component terrainMgrComponent = terrain.gameObject.GetComponent(terrainManagerType);
                    if (terrainMgrComponent != null) { DestroyImmediate(terrainMgrComponent); }
                }
                isSuccessful = true;
            }

            return isSuccessful;
        }

        /// <summary>
        /// Get the System.Type of a MegaSplat TextureArrayConfig class
        /// </summary>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static System.Type MegaSplatGetTextureArrayType(bool showErrors)
        {
            System.Type TextureArrayConfigType = null;

#if UNITY_EDITOR

            try
            {
                TextureArrayConfigType = System.Type.GetType("JBooth.MegaSplat.TextureArrayConfig, Assembly-CSharp-Editor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", true, true);

                if (TextureArrayConfigType == null)
                {
                    if (showErrors) { Debug.LogWarning("LBIntegration.MegaSplatGetTextureArrayType - could not find TextureArrayConfig class. Please Report."); }
                }
            }
            catch (Exception ex)
            {
                if (showErrors) { Debug.LogWarning("LBIntegration.MegaSplatGetTextureArrayType something has gone wrong (MegaSplat 1.73+ required). Please report. " + ex.Message); }
            }

#endif
            return TextureArrayConfigType;
        }

        /// <summary>
        /// Get the System.Type of a MegaSplat TerrainToMegaSplatConfig Class
        /// </summary>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static System.Type MegaSplatGetTerrainToMegaSplatConfigType(bool showErrors)
        {
            System.Type TerrainToMegaSplatConfigType = null;

#if UNITY_EDITOR

            try
            {
                TerrainToMegaSplatConfigType = System.Type.GetType("JBooth.MegaSplat.MegaSplatGetTerrainToMegaSplatConfigType, Assembly-CSharp-Editor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", true, true);

                if (TerrainToMegaSplatConfigType == null)
                {
                    if (showErrors) { Debug.LogWarning("LBIntegration.MegaSplatGetTerrainToMegaSplatConfigType - could not find TerrainToMegaSplatConfig class. Please Report."); }
                }
            }
            catch (Exception ex)
            {
                if (showErrors) { Debug.LogWarning("LBIntegration.MegaSplatGetTerrainToMegaSplatConfigType something has gone wrong (MegaSplat 1.14.1+ required). Please report. " + ex.Message); }
            }

#endif
            return TerrainToMegaSplatConfigType;
        }

        /// <summary>
        /// Get the System.Type of a MegaSplat MegaSplatTerrainManager Class
        /// </summary>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static System.Type MegaSplatTerrainManagerType(bool showErrors)
        {
            System.Type MegaSplatTerrainManagerType = null;

#if UNITY_EDITOR

            string methodName = "LBIntegration.MegaSplatTerrainManagerType";

            try
            {
                MegaSplatTerrainManagerType = System.Type.GetType("MegaSplatTerrainManager, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", true, true);

                if (MegaSplatTerrainManagerType == null)
                {
                    if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not find TextureArrayConfig class. Please Report."); }
                }
            }
            catch (Exception ex)
            {
                if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " something has gone wrong (MegaSplat 1.73+ required). Please report. " + ex.Message); }
            }

#endif
            return MegaSplatTerrainManagerType;
        }

        /// <summary>
        /// Get the MegaSplat Material name for a landscape
        /// There are two filename formats, one for a Unity terrain material and the other for a mesh terrain material
        /// NOTE: Assumes landscape names are unique in the project
        /// </summary>
        /// <param name="landscapeName"></param>
        /// <param name="isMeshMaterial"></param>
        /// <param name="includeExtension"></param>
        /// <returns></returns>
        public static string MegaSplatGetMaterialName(string landscapeName, bool isMeshMaterial, bool includeExtension)
        {
            return landscapeName + (isMeshMaterial ? "_Mesh" : "") + "_MegaSplatMaterial" + (includeExtension ? ".mat" : "");
        }

        /// <summary>
        /// Get the MegaSplat Shader name for a landscape
        /// There are two filename formats, one for a Unity terrain shader and the other for a mesh terrain shader
        /// NOTE: Assumes landscape names are unique in the project
        /// </summary>
        /// <param name="landscapeName"></param>
        /// <param name="isMeshShader"></param>
        /// <returns></returns>
        public static string MegaSplatGetShaderName(string landscapeName, bool isMeshShader)
        {
            return landscapeName + (isMeshShader ? "_Mesh" : "") + "_MegaSplat";
        }

        /// <summary>
        /// Get the MegaSplat Texture Array Config file name for a landscape
        /// </summary>
        /// <param name="landscapeName"></param>
        /// <param name="includeExtension"></param>
        /// <returns></returns>
        public static string MegaSplatGetTextureArrayConfigName(string landscapeName, bool includeExtension)
        {
            return landscapeName + "_MegaSplat_tarray_config" + (includeExtension ? ".asset" : "");
        }

        #region MegaSplat Only
        #if __MEGASPLAT__

        /// <summary>
        /// Get the TerrainToSplatConfig config file name for a landscape
        /// </summary>
        /// <param name="landscapeName"></param>
        /// <param name="includeExtension"></param>
        /// <returns></returns>
        public static string MegaSplatGetTerrainToMegaSplatConfigName(string landscapeName, bool includeExtension)
        {
            return landscapeName + "_TerrainToSplatConfig" + (includeExtension ? ".asset" : "");
        }

        /// <summary>
        /// Get a MegaSplat Texture Array Config asset for a landscape
        /// NOTE: Assumes landscape names are unique in the project
        /// EDITOR-ONLY, else always returns NULL
        /// </summary>
        /// <param name="landscapeName"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static UnityEngine.Object MegaSplatGetTextureArrayConfig(string landscapeName, bool showErrors)
        {
            UnityEngine.Object objTextureArrayConfig = null;

#if UNITY_EDITOR
            System.Type TextureArrayConfigType = MegaSplatGetTextureArrayType(showErrors);

            if (TextureArrayConfigType != null)
            {
                string pathTextureArrayConfig = "Assets/LandscapeBuilder/Materials/" + MegaSplatGetTextureArrayConfigName(landscapeName, true);

                objTextureArrayConfig = UnityEditor.AssetDatabase.LoadAssetAtPath(pathTextureArrayConfig, TextureArrayConfigType);

                if (objTextureArrayConfig == null && showErrors)
                {
                    Debug.LogWarning("LBIntegration.MegaSplatGetTextureArrayConfig - could not find config file at " + pathTextureArrayConfig);
                }
            }
#endif

            return objTextureArrayConfig;
        }

        public static UnityEngine.Object MegaSplatGetTerrainToMegaSplatConfig(string landscapeName, bool showErrors)
        {
            UnityEngine.Object objTextureToMegaSplatConfig = null;

#if UNITY_EDITOR
            System.Type TextureArrayConfigType = MegaSplatGetTextureArrayType(showErrors);

            if (TextureArrayConfigType != null)
            {
                string pathTextureArrayConfig = "Assets/LandscapeBuilder/Materials/" + MegaSplatGetTextureArrayConfigName(landscapeName, true);

                objTextureToMegaSplatConfig = UnityEditor.AssetDatabase.LoadAssetAtPath(pathTextureArrayConfig, TextureArrayConfigType);

                if (objTextureToMegaSplatConfig == null && showErrors)
                {
                    Debug.LogWarning("LBIntegration.MegaSplatGetTextureArrayConfig - could not find config file at " + pathTextureArrayConfig);
                }
            }
#endif

            return objTextureToMegaSplatConfig;
        }

        /// <summary>
        /// Get the MegaSplat material for this landscape
        /// There are two types, one for a Unity terrain material and the other for a mesh terrain material
        /// EDITOR ONLY - at runtime always returns null
        /// </summary>
        /// <param name="landscapeName"></param>
        /// <param name="isMeshMaterial"></param>
        /// <returns></returns>
        public static Material MegaSplatGetMaterial(string landscapeName, bool isMeshMaterial)
        {
#if UNITY_EDITOR
            string materialName = MegaSplatGetMaterialName(landscapeName, isMeshMaterial, true);

            return (Material)UnityEditor.AssetDatabase.LoadAssetAtPath("Assets/LandscapeBuilder/Materials/" + materialName, typeof(Material));
#else
            return null;
#endif
        }

        #endif
        #endregion

        /// <summary>
        /// Creates or uses a custom material and shader for MegaSplat.
        /// EDITOR-ONLY as MegaSplat shader creation only works in the editor
        /// Assumes the landscape name is unique in the project
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="autoClosePainter"></param>
        /// <param name="isMeshShader"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static Material MegaSplatConfigureMaterial(LBLandscape landscape, bool autoClosePainter, bool isMeshShader, bool showErrors)
        {
            Material mat = null;

            #region MegaSplat Only
            #if __MEGASPLAT__

            string methodName = "LBIntegration.MegaSplatConfigureMaterial";

            if (IsMegaSplatInstalled(showErrors))
            {
                System.Type SplatArrayShaderGUIType = null;
                System.Type TerrainPainterWindowType = null;

                try
                {
                    SplatArrayShaderGUIType = System.Type.GetType("SplatArrayShaderGUI, Assembly-CSharp-Editor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", true, true);
                    TerrainPainterWindowType = System.Type.GetType("JBooth.TerrainPainter.TerrainPainterWindow, Assembly-CSharp-Editor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", true, true);

                    if (SplatArrayShaderGUIType == null)
                    {
                        if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not find SplatArrayShaderGUI editor class. Please Report."); }
                    }
                    if (TerrainPainterWindowType == null)
                    {
                        if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not find TerrainPainterWindowType editor class. Please Report."); }
                    }
                    else
                    {
                        #if UNITY_EDITOR

                        Material terrainMat = null;
                        Shader shader = null;
                        bool isNewMaterialRequired = true;

                        // Check to see if the material already exists
                        string materialName = MegaSplatGetMaterialName(landscape.name, isMeshShader, false);
                        terrainMat = (Material)UnityEditor.AssetDatabase.LoadAssetAtPath("Assets/LandscapeBuilder/Materials/" + materialName, typeof(Material));
                        isNewMaterialRequired = (terrainMat == null);

                        // Check to see if the shader already exists
                        string shaderName = MegaSplatGetShaderName(landscape.name, isMeshShader);
                        shader = (Shader)UnityEditor.AssetDatabase.LoadAssetAtPath("Assets/LandscapeBuilder/Materials/" + shaderName + ".shader", typeof(Shader));
                        bool isNewShaderRequired = (shader == null);

                        if (isNewShaderRequired)
                        {
                            // MegaSplat will create the shader in the selected Project folder
                            UnityEditor.Selection.activeObject = null;
                            LBEditorHelper.SelectFolder("Assets/LandscapeBuilder/Materials", true);

                            shader = (Shader)SplatArrayShaderGUIType.InvokeMember("NewShader", System.Reflection.BindingFlags.InvokeMethod, null, null, new object[] { });

                            // This will fail if the asset already exists
                            UnityEditor.AssetDatabase.RenameAsset("Assets/LandscapeBuilder/Materials/MegaSplat.shader", shaderName);
                        }

                        if (shader == null)
                        {
                            if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not create MegaSplat shader. Please Report."); }
                        }
                        else
                        {
                            if (isNewMaterialRequired) { terrainMat = new Material(shader); }

                            if (terrainMat != null)
                            {
                                string terrainMaterialPath = "Assets/LandscapeBuilder/Materials/" + materialName;

                                if (isNewMaterialRequired)
                                {
                                    terrainMat.name = materialName;

                                    UnityEditor.AssetDatabase.CreateAsset(terrainMat, terrainMaterialPath + ".mat");
                                    UnityEditor.AssetDatabase.Refresh();
                                }

                                if (isMeshShader)
                                {
                                    terrainMat.DisableKeyword("_TERRAIN");
                                }
                                else
                                {
                                    terrainMat.EnableKeyword("_TERRAIN");
                                }

                                // For some reason the shaderFeatures of "_TERRAIN" doesn't work on inital compile - it seems to get created as Mesh first.
                                // Enabling the keyword, AFTER the first Compile seems to work with no errors in MegaSplat 1.57
                                //string[] shaderFeatures = { "" };

                                // MegaSplat v1.73 - this now works fine
                                string[] shaderFeatures = { (isMeshShader ? "" : "_TERRAIN") };

                                // Version 0.996 had 3 parms. Version 1.14 adds a fourth parm Material existingMat = null
                                string shader_output = (string)SplatArrayShaderGUIType.InvokeMember("Compile", System.Reflection.BindingFlags.InvokeMethod, null, null, new object[] { shaderFeatures, shaderName, null, terrainMat });

                                if (!string.IsNullOrEmpty(shader_output))
                                {
                                    // Added LB 2.0.6 Beta 7j
                                    // Write the new shader script back to the shader asset
                                    string shaderPath = UnityEditor.AssetDatabase.GetAssetPath(terrainMat.shader);
                                    if (!string.IsNullOrEmpty(shaderPath))
                                    {
                                        System.IO.File.WriteAllText(shaderPath, shader_output);
                                        UnityEditor.EditorUtility.SetDirty(terrainMat);
                                        UnityEditor.AssetDatabase.Refresh();
                                    }

                                    if (isMeshShader)
                                    {
                                        // This should be handled automatically above
                                        //terrainMat.DisableKeyword("_TERRAIN");
                                    }
                                    else
                                    {
                                        //Debug.Log("[DEBUG] shader output: " + shader_output);

                                        // Apply the material to each of the terrains in the landscape
                                        Terrain[] landscapeTerrains = landscape.GetComponentsInChildren<Terrain>();
                                        if (landscapeTerrains != null)
                                        {
                                            for (int t = 0; t < landscapeTerrains.Length; t++)
                                            {
                                                landscapeTerrains[t].materialTemplate = terrainMat;
                                            }

                                            // Temporarily open the MegaSplat Terrain Paint window so we can create PNG files (if required)
                                            var windowTerrainPainter = UnityEditor.EditorWindow.GetWindow(TerrainPainterWindowType, false);
                                            if (windowTerrainPainter != null)
                                            {
                                                // Loop through all the terrains in this landscape
                                                for (int t = 0; t < landscapeTerrains.Length; t++)
                                                {
                                                    string _texturePrefix = landscape.name + "_" + landscapeTerrains[t].name;

                                                    // If required, create a texture that MegaSplat can populate for each terrain
                                                    TerrainPainterWindowType.InvokeMember("CreateTexture", System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public, null, windowTerrainPainter, new object[] { landscapeTerrains[t], _texturePrefix, null });
                                                }

                                                if (autoClosePainter) { windowTerrainPainter.Close(); }

                                                // Set focus back to LB Editor Window
                                                LBEditorHelper.SetFocusLBW();
                                            }
                                        }
                                    }

                                    LBEditorHelper.HighlightItemInProjectWindow(terrainMaterialPath + ".mat", true);
                                    //Debug.Log("Shader output: " + shader_output);
                                    mat = terrainMat;
                                }
                            }
                        }
                    #else
                    if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - MegaSplat currently does not support setup outside the Editor"); }
                    #endif
                    }
                }
                catch (Exception ex)
                {
                    if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " something has gone wrong. Please report. " + ex.Message); }
                }
            }
            #endif
            #endregion

            return mat;
        }

        /// <summary>
        /// Attempt to recompile MegaSplat shader with certain features
        /// USAGE:
        /// string[] shaderFeatures = { "_TERRAIN" };
        /// bool isSuccessful = MegaSplatCompiler(landscape, shaderFeatures, false, true);
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="shaderFeatures"></param>
        /// <param name="isMeshShader"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static bool MegaSplatCompileShader(LBLandscape landscape, string[] shaderFeatures, bool isMeshShader, bool showErrors)
        {
            bool isSuccessful = false;

            #region MegaSplat Only
            #if __MEGASPLAT__

            if (IsMegaSplatInstalled(showErrors))
            {
                System.Type SplatArrayShaderGUIType = null;

                try
                {
                    SplatArrayShaderGUIType = System.Type.GetType("SplatArrayShaderGUI, Assembly-CSharp-Editor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", true, true);

                    if (SplatArrayShaderGUIType == null)
                    {
                        if (showErrors) { Debug.LogWarning("LBIntegration.MegaSplatCompileShader - could not find SplatArrayShaderGUI editor class. Please Report."); }
                    }
                    else
                    {
                        #if UNITY_EDITOR

                        Material terrainMat = null;
                        Shader shader = null;

                        // Check to see if the material exists
                        string materialName = MegaSplatGetMaterialName(landscape.name, isMeshShader, false);
                        terrainMat = (Material)UnityEditor.AssetDatabase.LoadAssetAtPath("Assets/LandscapeBuilder/Materials/" + materialName, typeof(Material));

                        // Check to see if the shader exists
                        string shaderName = MegaSplatGetShaderName(landscape.name, isMeshShader);
                        shader = (Shader)UnityEditor.AssetDatabase.LoadAssetAtPath("Assets/LandscapeBuilder/Materials/" + shaderName + ".shader", typeof(Shader));

                        if (shader == null)
                        {
                            if (showErrors) { Debug.LogWarning("LBIntegration.MegaSplatCompileShader - could not find MegaSplat shader. Please Report."); }
                        }
                        else
                        {
                            if (terrainMat != null)
                            {
                                string shader_output = (string)SplatArrayShaderGUIType.InvokeMember("Compile", System.Reflection.BindingFlags.InvokeMethod, null, null, new object[] { shaderFeatures, shaderName, null });

                                if (!string.IsNullOrEmpty(shader_output))
                                {
                                    // Added LB 2.0.6 Beta 7j
                                    // Write the new shader script back to the shader asset
                                    string shaderPath = UnityEditor.AssetDatabase.GetAssetPath(terrainMat.shader);
                                    if (!string.IsNullOrEmpty(shaderPath))
                                    {
                                        System.IO.File.WriteAllText(shaderPath, shader_output);
                                        UnityEditor.EditorUtility.SetDirty(terrainMat);
                                        UnityEditor.AssetDatabase.Refresh();
                                    }
                                }

                                isSuccessful = !string.IsNullOrEmpty(shader_output);
                            }
                        }
                        #else
                        if (showErrors) { Debug.LogWarning("LBIntegration.MegaSplatCompileShader - MegaSplat currently does not support setup outside the Editor"); }
                        #endif
                    }
                }
                catch (Exception ex)
                {
                    if (showErrors) { Debug.LogWarning("LBIntegration.MegaSplatCompileShader something has gone wrong. Please report. " + ex.Message); }
                }
            }

            #endif
            #endregion

            return isSuccessful;
        }

        #region MegaSplat Only
        #if __MEGASPLAT__
        /// <summary>
        /// Create a new MegaSplat Texture Array in the same Project folder as the MegaSplat shader and material for the landscape
        /// EDITOR-ONLY as MegaSplat does not support runtime generation
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="terrainMaterialType"></param>
        /// <param name="showErrors"></param>
        public static void MegaSplatCreateTextureArray(LBLandscape landscape, LBLandscape.TerrainMaterialType terrainMaterialType, bool showErrors)
        {
            // Basic validation
            if (landscape == null) { if (showErrors) { Debug.LogWarning("LBIntegration.MegaSplatCreateTextureArray - landscape cannot be null"); } }
            else if (!IsMegaSplatInstalled(true)) { if (showErrors) { Debug.LogWarning("LBIntegration.MegaSplatCreateTextureArray - MegaSplat 1.73 or newer does not seem to be installed in the project."); } }
            else if (terrainMaterialType != LBLandscape.TerrainMaterialType.MegaSplat) { if (showErrors) { Debug.LogWarning("LBIntegration.MegaSplatCreateTextureArray - The Landscape Terrain Settings needs to have a Material Type of Mega Splat. Please set and try again."); } }
            else if (landscape.terrainCustomMaterial == null) { if (showErrors) { Debug.LogWarning("LBIntegration.MegaSplatCreateTextureArray - the landscape does not appear to have a MegaSplat terrain material. Has it been initialised in the Landscape Terrain Settings?"); } }
            else
            {
                #if UNITY_EDITOR

                // Does the array config already exist?
                string materialsFolder = "Assets/LandscapeBuilder/Materials";

                UnityEngine.Object obj = UnityEditor.AssetDatabase.LoadMainAssetAtPath(materialsFolder + "/" + landscape.name + "_MegaSplat_tarray_config.asset");

                if (obj != null)
                {
                    // Select the existing Texture Array Config
                    UnityEditor.Selection.activeObject = obj;
                    //Debug.Log("CFG: " + obj.GetType().FullName);
                }
                else
                {
                    // The Texture Array Config doesn't exist, so create one
                    LBEditorHelper.SelectObjectInProjectWindow(landscape.terrainCustomMaterial, false);

                    // Create a new empty Texture Array
                    LBEditorHelper.CallMenu("Assets/Create/MegaSplat/Texture Array Config");

                    obj = UnityEditor.Selection.activeObject;

                    if (obj != null)
                    {
                        // WORKAROUND: Take focus away from the editable Texture Array Config in Project window
                        //LBEditorHelper.SelectObjectInProjectWindow(landscape.terrainCustomMaterial, true);
                        LBEditorHelper.SetFocusLBW();
                        // Now rename the new asset
                        if (obj.name.StartsWith("New Texture Array Config"))
                        {
                            string textureArrayPath = UnityEditor.AssetDatabase.GetAssetPath(obj);

                            if (!string.IsNullOrEmpty(textureArrayPath))
                            {
                                UnityEditor.AssetDatabase.RenameAsset(textureArrayPath, landscape.name + "_MegaSplat_tarray_config");

                                // Reselect the new Texture Array Config
                                UnityEditor.Selection.activeObject = obj;
                            }
                        }
                    }
                }

                #endif
            }
        }

        /// <summary>
        /// Create a new TerrainToMegaSplatConfig file in the same Project folder as the MegaSplat shader and material for the landscape
        /// EDITOR-ONLY as MegaSplat does not support runtime generation
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="terrainMaterialType"></param>
        /// <param name="showErrors"></param>
        public static UnityEngine.Object MegaSplatCreateTerrainToMegaSplatConfig(LBLandscape landscape, LBLandscape.TerrainMaterialType terrainMaterialType, UnityEngine.Object objTextureArrayConfig, bool showErrors)
        {
            UnityEngine.Object obj = null;

            // Basic validation
            if (landscape == null) { if (showErrors) { Debug.LogWarning("LBIntegration.MegaSplatCreateTerrainToMegaSplatConfig - landscape cannot be null"); } }
            else if (!IsMegaSplatInstalled(true)) { if (showErrors) { Debug.LogWarning("LBIntegration.MegaSplatCreateTerrainToMegaSplatConfig - MegaSplat 1.14 or newer does not seem to be installed in the project."); } }
            else if (terrainMaterialType != LBLandscape.TerrainMaterialType.MegaSplat) { if (showErrors) { Debug.LogWarning("LBIntegration.MegaSplatCreateTerrainToMegaSplatConfig - The Landscape Terrain Settings needs to have a Material Type of Mega Splat. Please set and try again."); } }
            else if (landscape.terrainCustomMaterial == null) { if (showErrors) { Debug.LogWarning("LBIntegration.MegaSplatCreateTerrainToMegaSplatConfig - the landscape does not appear to have a MegaSplat terrain material. Has it been initialised in the Landscape Terrain Settings?"); } }
            else
            {
                #if UNITY_EDITOR

                // Does the config file already exist?
                string terrainToMegaSplatConfigName = MegaSplatGetTerrainToMegaSplatConfigName(landscape.name, false);

                obj = UnityEditor.AssetDatabase.LoadMainAssetAtPath(LBSetup.materialsFolder + "/" + terrainToMegaSplatConfigName + ".asset");

                if (obj != null)
                {
                    // Select the existing TerrainToMegaSplatConfig
                    UnityEditor.Selection.activeObject = obj;
                    //Debug.Log("CFG: " + obj.GetType().FullName);
                }
                else
                {
                    // The TerrainToMegaSplatConfig file doesn't exist, so create one
                    LBEditorHelper.CallMenu("Assets/Create/MegaSplat/Terrain To MegaSplat Config");

                    obj = UnityEditor.Selection.activeObject;

                    if (obj != null)
                    {
                        // WORKAROUND: Take focus away from the editable TerrainToMegaSplatConfig in Project window
                        LBEditorHelper.SetFocusLBW();
                        // Now rename the new asset
                        if (obj.name.StartsWith("TerrainToSplatConfig"))
                        {
                            string terrainToMegaSplatConfigPath = UnityEditor.AssetDatabase.GetAssetPath(obj);

                            if (!string.IsNullOrEmpty(terrainToMegaSplatConfigPath))
                            {
                                UnityEditor.AssetDatabase.RenameAsset(terrainToMegaSplatConfigPath, terrainToMegaSplatConfigName);

                                // Reselect the new TerrainToMegaSplatConfig Config
                                UnityEditor.Selection.activeObject = obj;
                            }
                        }
                    }
                }

                #endif
            }

            return obj;
        }

        /// <summary>
        /// Replace an existing TextureArrayConfig for a landscape, by selecting an existing TextureArray in the project.
        /// Assumes the Config file is same name as TextureArray without the following extensions:
        /// _tarray, _diff_tarray, _normSAO_tarray
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="newTexture2DArray"></param>
        /// <param name="showErrors"></param>
        public static bool MegaSplatReplaceTextureArray(LBLandscape landscape, Texture2DArray newTexture2DArray, bool showErrors)
        {
            bool isSuccessful = false;

            // Basic validation
            if (landscape == null) { if (showErrors) { Debug.LogWarning("LBIntegration.MegaSplatReplaceTextureArray - landscape cannot be null"); } }
            else if (!IsMegaSplatInstalled(true)) { if (showErrors) { Debug.LogWarning("LBIntegration.MegaSplatReplaceTextureArray - MegaSplat 1.73 or newer does not seem to be installed in the project."); } }
            else if (newTexture2DArray == null) { if (showErrors) { Debug.LogWarning("LBIntegration.MegaSplatReplaceTextureArray - no Texture2D Array selected"); } }
            else if (landscape.terrainCustomMaterial == null) { if (showErrors) { Debug.LogWarning("LBIntegration.MegaSplatReplaceTextureArray - the landscape does not appear to have a MegaSplat terrain material. Has it been initialised in the Landscape Terrain Settings?"); } }
            else
            {
                #if UNITY_EDITOR

                // Check if a matching config exists for the Texture2DArray
                string pathNewTexture2DArray = UnityEditor.AssetDatabase.GetAssetPath(newTexture2DArray);
                string pathNewTexture2DConfig = pathNewTexture2DArray.Replace("_tarray.asset", ".asset");

                System.Type TextureArrayConfigType = MegaSplatGetTextureArrayType(true);
                string materialsFolder = "Assets/LandscapeBuilder/Materials";

                bool isConfigFound = false;

                isConfigFound = (UnityEditor.AssetDatabase.LoadAssetAtPath(pathNewTexture2DConfig, TextureArrayConfigType) != null);

                // Try other alternative naming of the config file
                if (!isConfigFound)
                {
                    // Try example from MegaSplat 1.73
                    pathNewTexture2DConfig = pathNewTexture2DArray.Replace("_diff_tarray.asset", ".asset");
                    isConfigFound = (UnityEditor.AssetDatabase.LoadAssetAtPath(pathNewTexture2DConfig, TextureArrayConfigType) != null);
                    if (!isConfigFound)
                    {
                        // Try another example from MegaSplat 1.73
                        pathNewTexture2DConfig = pathNewTexture2DArray.Replace("_normSAO_tarray.asset", ".asset");
                        isConfigFound = (UnityEditor.AssetDatabase.LoadAssetAtPath(pathNewTexture2DConfig, TextureArrayConfigType) != null);
                    }
                }

                if (!isConfigFound) { if (showErrors) { Debug.LogWarning("LBIntegration.MegaSplatReplaceTextureArray - could not locate matching Texture2D Array Config"); } }
                else
                {
                    string targetTArrayConfigPath = materialsFolder + "/" + MegaSplatGetTextureArrayConfigName(landscape.name, true);

                    // Check if a config already exists for this Landscape
                    UnityEngine.Object objExistingConfig = UnityEditor.AssetDatabase.LoadAssetAtPath(targetTArrayConfigPath, TextureArrayConfigType);

                    // If it already exists, delete it so that it can be replaced.
                    if (objExistingConfig != null)
                    {
                        UnityEditor.AssetDatabase.DeleteAsset(targetTArrayConfigPath);
                    }

                    // Copy Config to default LB Texture Array Config location for this landscape
                    UnityEditor.AssetDatabase.CopyAsset(pathNewTexture2DConfig, targetTArrayConfigPath);
                    isSuccessful = true;
                }

                #endif
            }

            return isSuccessful;
        }

        /// <summary>
        /// MegaSplat requires specific data on each vertex in order to work. This process may need to split some
        /// vertices resulting in a slightly larger vertex count, depending on the topology of the mesh. 
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static Mesh MegaSplatProcessMesh(Mesh mesh, bool showErrors)
        {
            Mesh newMesh = null;

            if (mesh != null)
            {
                #if UNITY_EDITOR
                System.Type MeshProcessorWindowType = null;

                try
                {
                    MeshProcessorWindowType = System.Type.GetType("JBooth.MegaSplat.MeshProcessorWindow, Assembly-CSharp-Editor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", true, true);

                    if (MeshProcessorWindowType == null)
                    {
                        if (showErrors) { Debug.LogWarning("LBIntegration.MegaSplatProcessMesh - could not find MeshProcessorWindow class. Please Report."); }
                    }
                    else
                    {
                        //Debug.Log("INFO: MegaSplatProcessMesh processing: " + mesh.name);
                        newMesh = (Mesh)MeshProcessorWindowType.InvokeMember("Process", System.Reflection.BindingFlags.InvokeMethod, null, null, new object[] { mesh });
                    }
                }
                catch (Exception ex)
                {
                    if (showErrors) { Debug.LogWarning("LBIntegration.MegaSplatProcessMesh something has gone wrong (MegaSplat 1.73+ required). Please report. " + ex.Message); }
                }

                #endif
            }

            return newMesh;
        }

        /// <summary>
        /// Copy the textures from a Unity terrain into a mesh terrain chunk using the terrain custom material
        /// NOTE: A landscape can have multiple terrains but only one custom material.
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="lbMesh"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static Mesh MegaSplatCopyTexturesToMesh(LBLandscape landscape, LBMesh lbMesh, bool showErrors)
        {
            Mesh mesh = lbMesh.mesh;

            if (landscape == null) { if (showErrors) { Debug.LogWarning("LBIntegration.MegaSplatCopyTexturesToMesh - landscape parameter cannot be null"); } }
            else if (landscape.terrainCustomMaterial == null) { if (showErrors) { Debug.LogWarning("LBIntegration.MegaSplatCopyTexturesToMesh - the landscape does not appear to have a MegaSplat material. Has it been initialised in the Landscape Terrain Settings?"); } }
            else if (lbMesh == null) { if (showErrors) { Debug.LogWarning("LBIntegration.MegaSplatCopyTexturesToMesh - lbMesh parameter cannot be null"); } }
            else if (lbMesh.mesh == null) { if (showErrors) { Debug.LogWarning("LBIntegration.MegaSplatCopyTexturesToMesh - " + lbMesh.title + " does not have a mesh"); } }
            else
            {
                // Retrieve the control texture from the terrain custom material.
                Texture2D terrainTexture = landscape.terrainCustomMaterial.GetTexture("_SplatControl") as Texture2D;

                if (terrainTexture == null) { if (showErrors) { Debug.LogWarning("LBIntegration.MegaSplatCopyTexturesToMesh - could not get _SplatControl texture from MegaSplat terrain material. Please Report."); } }
                else
                {
                    // ***** Seems we don't need the terrain data or alphamaps **** //
                    // TODO: Remove terrain info..

                    // Get the terrain name from the mesh name
                    string terrainName = mesh.name.Substring(0, mesh.name.LastIndexOf("_Mesh"));

                    // Find the terrain
                    Terrain[] terrains = landscape.GetComponentsInChildren<Terrain>(true);

                    if (terrains == null) { if (showErrors) { Debug.LogWarning("LBIntegration.MegaSplatCopyTexturesToMesh - could not find any Unity terrains in the landscape. Have you disabled them?"); } }
                    else if (terrains.Length < 1) { if (showErrors) { Debug.LogWarning("LBIntegration.MegaSplatCopyTexturesToMesh - no Unity terrains in the landscape. Have you disabled them?"); } }
                    {
                        Terrain terrain = null;

                        for (int t = 0; t < terrains.Length; t++)
                        {
                            if (terrains[t].name == terrainName)
                            {
                                terrain = terrains[t];
                                break;
                            }
                        }

                        if (terrain == null) { if (showErrors) { Debug.LogWarning("LBIntegration.MegaSplatCopyTexturesToMesh - could not find the Unity terrain (" + terrainName + ") in the landscape (" + landscape.name + ")"); } }
                        else
                        {
                            int texWidth = terrainTexture.width;
                            int texHeight = terrainTexture.height;

                            // Retrieve the data from the mesh
                            Vector2[] uv = mesh.uv;
                            UnityEngine.Color[] colorsArray = mesh.colors;

                            // Create empty list for uv3s and pre-allocated capacity (memory)
                            List<Vector4> uv4List = new List<Vector4>(uv.Length);

                            // Validation
                            if (uv == null || mesh.uv.Length < 1) { if (showErrors) { Debug.LogWarning("LBIntegration.MegaSplatCopyTexturesToMesh - mesh uv data is missing from " + mesh.name); } }
                            else if (colorsArray == null || colorsArray.Length < 1) { if (showErrors) { Debug.LogWarning("LBIntegration.MegaSplatCopyTexturesToMesh - mesh color data is missing from " + mesh.name); } }
                            else if (uv4List == null) { if (showErrors) { Debug.LogWarning("LBIntegration.MegaSplatCopyTexturesToMesh - could not create uv4s for " + mesh.name); } }
                            else if (uv.Length != colorsArray.Length) { if (showErrors) { Debug.LogWarning("LBIntegration.MegaSplatCopyTexturesToMesh - uv and color count mismatch on " + mesh.name); } }
                            else
                            {
                                UnityEngine.Color texColour;
                                Vector4 uv4 = Vector4.zero;

                                int texX = 0, texY = 0;

                                // loop through all the uvs
                                for (int v = 0; v < uv.Length; v++)
                                {
                                    // Get the nearest pixel
                                    texX = Mathf.FloorToInt(uv[v].x * texWidth);
                                    texY = Mathf.FloorToInt(uv[v].y * texHeight);

                                    // Lookup this pixel in the custom terrain texture
                                    texColour = terrainTexture.GetPixel(texX, texY);

                                    // Copy the pixel colour to the uv3 vector4.
                                    colorsArray[v].a = texColour.r;
                                    uv4.w = texColour.g;
                                    uv4.x = texColour.b;

                                    uv4List.Add(uv4);
                                }

                                if (uv4List.Count == uv.Length)
                                {
                                    // Update uv4 - SetUVs channel parm is zero-based.
                                    mesh.SetUVs(3, uv4List);
                                    //Debug.Log("INFO: MegaSplatCopyTexturesToMesh " + mesh.name + " updated uv4");
                                }

                                //Debug.Log("INFO: MegaSplatCopyTexturesToMesh " + mesh.name + " verts:" + mesh.vertexCount + " colors:" + mesh.colors.Length + " uvs:" + mesh.uv.Length + " uv3s:" + mesh.uv3.Length);
                            }
                        }
                    }
                }
            }

            return mesh;
        }

        /// <summary>
        /// MegaSplat TerrainPainter tab order or number may change between versions.
        /// This method will attempt to find a matching index position
        /// </summary>
        /// <param name="tabName"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static int MegaSplatGetTerrainPainterTabIndex(string tabName, bool showErrors)
        {
            int tabIndex = 0;

            if (!IsMegaSplatInstalled(true)) { if (showErrors) { Debug.LogWarning("LBIntegration.MegaSplatGetTerrainPainterTabIndex - MegaSplat 1.73 or newer does not seem to be installed in the project."); } }
            else
            {
                #if UNITY_EDITOR
                System.Type TerrainPainterWindowType = null;

                try
                {
                    TerrainPainterWindowType = System.Type.GetType("JBooth.TerrainPainter.TerrainPainterWindow, Assembly-CSharp-Editor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", true, true);

                    if (TerrainPainterWindowType == null)
                    {
                        if (showErrors) { Debug.LogWarning("LBIntegration.MegaSplatGetTerrainPainterTabIndex - could not find TerrainPainterWindowType editor class. Please Report."); }
                    }
                    else
                    {
                        var windowTerrainPainter = UnityEditor.EditorWindow.GetWindow(TerrainPainterWindowType, false);
                        if (windowTerrainPainter == null) { Debug.LogWarning("LBIntegration.MegaSplatGetTerrainPainterTabIndex - Could not open TerrainPainter window. Please Report."); }
                        else
                        {
                            string[] tabNames = (string[])TerrainPainterWindowType.InvokeMember("tabNames", System.Reflection.BindingFlags.GetField | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public, null, windowTerrainPainter, new object[] { });

                            if (tabNames != null && tabNames.Length > 0)
                            {
                                for (int tn = 0; tn < tabNames.Length; tn++)
                                {
                                    if (tabNames[tn].ToLower() == tabName.ToLower())
                                    {
                                        tabIndex = tn;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (showErrors) { Debug.LogWarning("LBIntegration.MegaSplatGetTerrainPainterTabIndex something has gone wrong (MegaSplat 1.73+ required). Please report. " + ex.Message); }
                }
                #endif
            }

            return tabIndex;
        }

        /// <summary>
        /// Update MegaSplat splat texture arrays in the material which will modify the shader.
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="splatType"></param>
        /// <param name="isMeshShader"></param>
        /// <param name="TextureArrayConfig"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static bool MegaSplatUpdateShaderSplat(LBLandscape landscape, string splatType, bool isMeshShader, UnityEngine.Object TextureArrayConfig, bool showErrors)
        {
            bool isSuccessful = false;

            // Basic validation
            if (landscape == null) { if (showErrors) { Debug.LogWarning("LBIntegration.MegaSplatUpdateShaderSplat - landscape cannot be null"); } }
            if (!isMeshShader && landscape.terrainCustomMaterial == null) { if (showErrors) { Debug.LogWarning("LBIntegration.MegaSplatUpdateShaderSplat - terrain custom material cannot be null"); } }
            if (!isMeshShader && !landscape.terrainCustomMaterial.shader.name.Contains("MegaSplat")) { if (showErrors) { Debug.LogWarning("LBIntegration.MegaSplatUpdateShaderSplat - landscape does not seem to have MegaSplat shader. Check Landscape Terrain Settings Material Type."); } }
            else if (!IsMegaSplatInstalled(true)) { if (showErrors) { Debug.LogWarning("LBIntegration.MegaSplatUpdateShaderSplat - MegaSplat 1.73 or newer does not seem to be installed in the project."); } }
            else if (TextureArrayConfig == null) { if (showErrors) { Debug.LogWarning("LBIntegration.MegaSplatUpdateShaderSplat - TextureArrayConfig cannot be null"); } }
            else
            {
#if UNITY_EDITOR

                string pathTextureArrayConfig = UnityEditor.AssetDatabase.GetAssetPath(TextureArrayConfig);

                if (System.IO.Path.GetExtension(pathTextureArrayConfig) == ".asset")
                {
                    // Find the Texture Array for this landscape

                    // Trim off the file extension
                    string pathTexArrayConfigBaseName = pathTextureArrayConfig.Remove(pathTextureArrayConfig.Length - 6);

                    string pathTextureArray = pathTexArrayConfigBaseName + "_tarray.asset";
                    Texture2DArray texArray = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2DArray>(pathTextureArray);

                    // Try other alternative naming of the TextureArray file
                    if (texArray == null)
                    {
                        // Try example from MegaSplat 1.73
                        pathTextureArray = pathTexArrayConfigBaseName + "_diff_tarray.asset";
                        texArray = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2DArray>(pathTextureArray);

                        if (texArray == null)
                        {
                            // Try another example from MegaSplat 1.73
                            pathTextureArray = pathTexArrayConfigBaseName + "_normSAO_tarray.asset";
                            texArray = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2DArray>(pathTextureArray);
                        }
                    }

                    if (texArray == null) { if (showErrors) { Debug.LogWarning("LBIntegration.MegaSplatUpdateShaderSplat - could not find Texture Array: " + pathTexArrayConfigBaseName + "_tarray.asset | _diff_tarray.asset | _normSAO_tarray.asset"); } }
                    else
                    {
                        if (splatType.ToLower() == "albedo")
                        {
                            if (isMeshShader)
                            {
                                // Find the mesh material for this landscape
                                Material meshMaterial = MegaSplatGetMaterial(landscape.name, isMeshShader);
                                if (meshMaterial == null) { Debug.LogWarning("LBIntegration.MegaSplatUpdateShaderSplat - could not find MegaSplat mesh material: " + MegaSplatGetMaterialName(landscape.name, isMeshShader, false)); }
                                else
                                {
                                    meshMaterial.SetTexture("_Diffuse", texArray);
                                    isSuccessful = true;
                                }
                            }
                            else
                            {
                                landscape.terrainCustomMaterial.SetTexture("_Diffuse", texArray);
                                isSuccessful = true;
                            }
                        }
                    }
                }

#endif
            }

            return isSuccessful;
        }

        #endif
        #endregion
        
        // End MegaSplat Integration
        #endregion

        #region MicroSplat Integration

        /// <summary>
        /// Is MicroSplat installed in this project?
        /// </summary>
        /// <returns></returns>
        public static bool IsMicroSplatInstalled()
        {
            bool isInstalled = false;

            #if __MICROSPLAT__
            isInstalled = true;
            #endif

            return isInstalled;
        }

        /// <summary>
        /// Add (or remove) MicroSplat material to a terrain within a landscape
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="terrain"></param>
        /// <param name="microSplatMaterial"></param>
        /// <param name="isEnabled"></param>
        /// <param name="isFirstTerrain"></param>
        /// <param name="isLastTerrain"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static bool MicroSplatEnable(LBLandscape landscape, Terrain terrain, Material microSplatMaterial, bool isEnabled, bool isFirstTerrain, bool isLastTerrain, bool showErrors)
        {
            bool isSuccessful = false;
            string methodName = "LBIntegration.MicroSplatEnable";

            // Basic validation
            if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscape cannot be null"); } }
            else if (terrain == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - terrain cannot be null"); } }
            else if (!IsMicroSplatInstalled()) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - MicroSplat 2.15 or newer does not seem to be installed in the project."); } }
            else if (isEnabled)
            {
                #if __MICROSPLAT__

                // Check if the terrain already has the script attached, else add it
                MicroSplatTerrain microSplatTerrain = terrain.gameObject.GetComponent<MicroSplatTerrain>();
                if (microSplatTerrain == null)
                {
                    microSplatTerrain = terrain.gameObject.AddComponent<MicroSplatTerrain>();
                    if (microSplatTerrain == null)
                    {
                        if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " could not add MicroSplat Terrain component to terrain: " + terrain.name); }
                    }
                    // User is switching back to an existing MicroSplat terrain material
                    else if (microSplatMaterial != null && terrain.materialType != Terrain.MaterialType.Custom && microSplatMaterial.shader.name.Contains("MicroSplat"))
                    {
                        isSuccessful = MicroSplatUpdateMaterial(landscape, terrain, microSplatMaterial, showErrors);
                        //Debug.Log("[DEBUG] added MicroSplatTerrain component. Shader: " + microSplatMaterial.shader.name);
                    }
                    // typical scenario where a new MicroSplat material needs to be created with LB Initialise button
                    else { isSuccessful = true; }
                }
                else
                {
                    // If it has been previously enabled, check if the material needs updating
                    // landscape custom material should not be null if previously setup, but this caters for an edge case
                    string shaderName = (landscape.terrainCustomMaterial == null ? "" : landscape.terrainCustomMaterial.shader.name);

                    //if (landscape.terrainCustomMaterial == null || (landscape.terrainCustomMaterial != null && landscape.terrainCustomMaterial.shader.name.Contains("MicroSplat")))
                    if (landscape.terrainCustomMaterial == null || shaderName.Contains("MicroSplat"))
                    {
                        // Only update the material if this is the first time OR the shader name hasn't been fixed yet in the LB Editor window (terrain settings)
                        if (string.IsNullOrEmpty(shaderName) || shaderName.Contains("LandscapeTerrain0"))
                        {
                            //Debug.Log("[DEBUG] updating terrain material shaderName: " + shaderName);

                            if (MicroSplatUpdateMaterial(landscape, terrain, microSplatMaterial, showErrors))
                            {
                                //Debug.Log("INFO: Updated material " + microSplatMaterial.name + " for terrain " + terrain.name);
                                //if (isLastTerrain) { MicroSplatTerrain.SyncAll(); }
                            }
                        }
                    }

                    isSuccessful = true;
                }

                #endif
            }
            else
            {
                // De-select the custom material
                landscape.terrainCustomMaterial = null;

                // Reset terrain settings back to default LB values
                terrain.heightmapPixelError = 1;
                terrain.basemapDistance = 1500f;
                if (terrain.terrainData != null) { terrain.terrainData.baseMapResolution = 512; }

                // if there is a MicroSplat terrain manager attached to the terrain, attempt to remove it
                #if __MICROSPLAT__
                Component terrainMgrComponent = terrain.gameObject.GetComponent<MicroSplatTerrain>();
                if (terrainMgrComponent != null) { DestroyImmediate(terrainMgrComponent); }
                #endif

                isSuccessful = true;
            }

            return isSuccessful;
        }

        /// <summary>
        /// Check if the MicroSplat component has been added to this terrain
        /// </summary>
        /// <param name="terrain"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static bool MicroSplatComponentAdded(Terrain terrain, bool showErrors)
        {
            bool isSuccessful = false;
            string methodName = "LBIntegration.MicroSplatComponentAdded";

            if (terrain == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - terrain cannot be null"); } }
            else
            {
                #if __MICROSPLAT__

                // Check if the terrain already has the script attached
                MicroSplatTerrain microSplatTerrain = terrain.gameObject.GetComponent<MicroSplatTerrain>();
                if (microSplatTerrain != null) { isSuccessful = true; }

                #endif
            }

            return isSuccessful;
        }

        /// <summary>
        /// Cleanup the terrain after a failed or partial MicroSplat terrain material change
        /// </summary>
        /// <param name="terrain"></param>
        /// <param name="showErrors"></param>
        public static void MicroSplatCleanup(Terrain terrain, bool showErrors)
        {
            string methodName = "LBIntegration.MicroSplatCleanup";

            if (terrain == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - terrain cannot be null"); } }
            else
            {
                #if __MICROSPLAT__

                // Check if the terrain has the script attached
                MicroSplatTerrain microSplatTerrain = terrain.gameObject.GetComponent<MicroSplatTerrain>();
                if (microSplatTerrain != null)
                {
                    #if UNITY_EDITOR
                    GameObject.DestroyImmediate(microSplatTerrain);
                    #else
                    GameObject.Destroy(microSplatTerrain);
                    #endif
                }

                #endif
            }
        }

        /// <summary>
        /// Update the custom terrain material on the given terrain
        /// NOTE: The landscape must be first initialised/converted to MicroSplat
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="terrain"></param>
        /// <param name="newTerrainCustomMaterial"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static bool MicroSplatUpdateMaterial(LBLandscape landscape, Terrain terrain, Material newTerrainCustomMaterial, bool showErrors)
        {
            bool isSuccessful = false;
            string methodName = "LBIntegration.MicroSplatUpdateMaterial";

            if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscape cannot be null"); } }
            else if (terrain == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - terrain cannot be null"); } }
            else if (!IsMicroSplatInstalled()) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - MicroSplat 2.15 or newer does not seem to be installed in the project."); } }
            else if (newTerrainCustomMaterial == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - the landscape does not appear to have a MicroSplat material. Has it been initialised in the Landscape Terrain Settings?"); } }
            else
            {
                #if __MICROSPLAT__

                // Only change material if this terrain has previously been initialised with MicroSplat
                MicroSplatTerrain microSplatTerrain = terrain.gameObject.GetComponent<MicroSplatTerrain>();
                if (microSplatTerrain != null)
                {
                    terrain.materialTemplate = newTerrainCustomMaterial;
                    terrain.materialType = Terrain.MaterialType.Custom;
                    microSplatTerrain.templateMaterial = newTerrainCustomMaterial;
                    
                    #if UNITY_EDITOR

                    string folder = LBEditorHelper.GetAssetFolder(newTerrainCustomMaterial);
                    if (folder.StartsWith("Assets/") && folder.Length > 7) { folder = folder.Substring(7); }

                    // Find and set the associated propData file (this doesn't get automatically set)
                    MicroSplatPropData propData = LBEditorHelper.GetAsset<MicroSplatPropData>(folder, newTerrainCustomMaterial.name + "_propdata" + ".asset");

                    if (propData != null)
                    {
                        microSplatTerrain.propData = propData;
                    }

                    #endif

                    //microSplatTerrain.Sync();

                    // This is per landscape, but we still update it here
                    landscape.terrainCustomMaterial = newTerrainCustomMaterial;

                    isSuccessful = true;
                }

                #endif
            }

            return isSuccessful;
        }

        #endregion

        #region General Utils

        /// <summary>
        /// Return the last file of a given type updated in a folder
        /// </summary>
        /// <param name="folderName"></param>
        /// <param name="fileExtension"></param>
        /// <returns></returns>
        public static string GetLastUpdatedFile(string folderName, string fileExtension)
        {
            string lastFileUpdated = string.Empty;

            if (!string.IsNullOrEmpty(folderName))
            {
                System.IO.DirectoryInfo dInfo = new System.IO.DirectoryInfo(folderName);
                if (dInfo != null)
                {
                    // Sort files in date order
                    System.IO.FileSystemInfo[] files = dInfo.GetFileSystemInfos("*." + fileExtension);
                    if (files != null)
                    {
                        Array.Sort<System.IO.FileSystemInfo>(files, delegate (System.IO.FileSystemInfo a, System.IO.FileSystemInfo b) { return a.LastWriteTime.CompareTo(b.LastWriteTime); });

                        lastFileUpdated = files[files.Length - 1].Name;
                    }
                }
            }

            return lastFileUpdated;
        }

        #endregion

        #region Reflection

        /// <summary>
        /// Return the class type from a full class name
        /// Returns NULL if the class isn't installed in the project
        /// e.g.
        /// System.Type type = GetClassTypeFromFullName("UnityStandardAssets.Water.WaterBase,Assembly-CSharp", true);
        /// </summary>
        /// <param name="classFullName"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static System.Type GetClassTypeFromFullName(string classFullName, bool showErrors)
        {
            System.Type type = null;
            try
            {
                type = System.Type.GetType(classFullName, true, true);
            }
            catch (Exception ex)
            {
                if (showErrors) { Debug.Log("Class not installed: " + ex.Message); }
            }
            return type;
        }

        /// <summary>
        /// Return the full name (AssemblyQualifiedName) for the given type
        /// eg. 
        /// string qn = LBIntegration.GetClassFullName(typeof(UnityStandardAssets.ImageEffects.Bloom), true);
        /// </summary>
        /// <param name="t"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static string GetClassFullName(Type t, bool showErrors)
        {
            string fullName = string.Empty;

            try
            {
                fullName = t.AssemblyQualifiedName;
            }
            catch (Exception ex)
            {
                if (showErrors) { Debug.Log("Class not installed: " + ex.Message); }
            }
            return fullName;
        }

        public static System.Reflection.MethodInfo[] ReflectionGetMethods(Type t)
        {
            return t.GetMethods(); // System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        }

        public static void ReflectionOutputMethods(Type t, bool showParmeters, bool includePrivate, bool includeStatic)
        {
            // Set up the binding flags
            System.Reflection.BindingFlags bindingFlags;

            if (includePrivate && !includeStatic)
            {
                bindingFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            }
            else if (includeStatic && !includePrivate)
            {
                bindingFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static;
            }
            else if (includeStatic && includePrivate)
            {
                bindingFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
                               System.Reflection.BindingFlags.Static;
            }
            else
            {
                bindingFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public;
            }


            System.Reflection.MethodInfo[] methodInfos = t.GetMethods(bindingFlags);
            foreach (System.Reflection.MethodInfo mInfo in methodInfos)
            {
                Debug.Log("LBIntegration: Type: " + t.Name + " Method: " + mInfo.Name);

                if (showParmeters)
                {
                    System.Reflection.ParameterInfo[] parameters = mInfo.GetParameters();
                    if (parameters != null)
                    {
                        foreach (System.Reflection.ParameterInfo parm in parameters)
                        {
                            Debug.Log(" Parm: " + parm.Name + " ParmType: " + parm.ParameterType.Name);
                        }
                    }
                }
            }
        }

        public static void ReflectionOutputFields(Type t, bool includePrivate, bool includeStatic)
        {
            // Set up the binding flags
            System.Reflection.BindingFlags bindingFlags;

            if (includePrivate && !includeStatic)
            {
                bindingFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            }
            else if (includeStatic && !includePrivate)
            {
                bindingFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static;
            }
            else if (includeStatic && includePrivate)
            {
                bindingFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
                               System.Reflection.BindingFlags.Static;
            }
            else
            {
                bindingFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public;
            }

            // Get the Fields of the type based on the binding flags selected
            System.Reflection.FieldInfo[] fInfos = t.GetFields(bindingFlags);

            foreach (System.Reflection.FieldInfo fInfo in fInfos)
            {
                Debug.Log("LBIntegration: Type: " + t.Name + " field: " + fInfo.Name + " fldtype: " + fInfo.FieldType.Name);
            }
        }

        public static void ReflectionOutputProperties(Type t, bool includePrivate, bool includeStatic)
        {
            // Set up the binding flags
            System.Reflection.BindingFlags bindingFlags;

            if (includePrivate && !includeStatic)
            {
                bindingFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            }
            else if (includeStatic && !includePrivate)
            {
                bindingFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static;
            }
            else if (includeStatic && includePrivate)
            {
                bindingFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
                               System.Reflection.BindingFlags.Static;
            }
            else
            {
                bindingFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public;
            }

            // Get the Properties of the type based on the binding flags selected
            System.Reflection.PropertyInfo[] piArray = t.GetProperties(bindingFlags);

            foreach (System.Reflection.PropertyInfo pi in piArray)
            {
                Debug.Log("LBIntegration: Type: " + t.Name + " property: " + pi.Name + " proptype: " + pi.PropertyType.Name);
            }
        }

        /// <summary>
        /// Get a field value of the given type from the class. If the field is not static, an instance of the class must be supplied.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="classType"></param>
        /// <param name="fieldName"></param>
        /// <param name="classInstance"></param>
        /// <param name="includePrivate"></param>
        /// <param name="isStatic"></param>
        /// <returns></returns>
        public static T ReflectionGetValue<T>(System.Type classType, string fieldName, UnityEngine.Object classInstance, bool includePrivate, bool isStatic)
        {
            // Some types may not be nullable, so instead set to the default value for that type.
            T value = default(T);

            // Set up the binding flags
            System.Reflection.BindingFlags bindingFlags;

            if (includePrivate && !isStatic)
            {
                bindingFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            }
            else if (isStatic && !includePrivate)
            {
                bindingFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static;
            }
            else if (isStatic && includePrivate)
            {
                bindingFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
                               System.Reflection.BindingFlags.Static;
            }
            else
            {
                bindingFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public;
            }

            System.Reflection.FieldInfo fieldInfo = typeof(T).GetField(fieldName, bindingFlags);

            if (fieldInfo != null)
            {
                if (isStatic) { value = (T)fieldInfo.GetValue(null); }
                else { value = (T)fieldInfo.GetValue(classInstance); }
            }

            return value;
        }

        //// TEST REFLECTION CODE
        //private static string Recursive(object o)
        //{
        //    string output = "";
        //    Type t = o.GetType();
        //    Debug.Log("Recursive type:" + t.ToString());
        //    if (t.GetProperty("Item") != null)
        //    {
        //        System.Reflection.PropertyInfo p = t.GetProperty("Item");
        //        int count = -1;
        //        if (t.GetProperty("Count") != null &&
        //            t.GetProperty("Count").PropertyType == typeof(System.Int32))
        //        {
        //            count = (int)t.GetProperty("Count").GetValue(o, null);
        //        }
        //        if (count > 0)
        //        {
        //            object[] index = new object[count];
        //            for (int i = 0; i < count; i++)
        //            {
        //                object val = p.GetValue(o, new object[] { i });
        //                Debug.Log("Recursive val:" + val.ToString());
        //            }
        //        }
        //    }
        //    return output;
        //}

        #endregion
    }
}