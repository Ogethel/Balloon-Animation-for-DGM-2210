#define _MAP_PATH_DEBUG_MODE

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace LandscapeBuilder
{
    [HelpURL("http://scsmmedia.com/media/Landscape%20Builder.pdf")]
    [AddComponentMenu("Landscape Builder/Map Path")]
    [ExecuteInEditMode]
    public class LBMapPath : MonoBehaviour
    {
        #region Variables and Properties
        // A script that allows you to define a splined path for a topography to follow

        // The lbPath is a replacement for pathPoints.
        public LBPath lbPath;
        public int mapResolution = 1;

        // EasyRoads integration variables
        public bool useEasyRoads = false;
        public bool isEasyRoadsInstalled = false;
        public List<LBRoad> roadList = null;

        // Vegetation Studio Integration - This requires 1 texture mask per terrain,
        // rather than a single landscape-wide texture.
        public bool isCreateMapPerTerrain = false;

        // Add Water to Mesh
        //public LBWater.WaterMeshResizingMode waterMeshResizingMode; // = LBWater.WaterMeshResizingMode.StandardAssets;
        public List<Camera> waterCameraList;
        public Material meshMaterial;
        [System.NonSerialized] public Camera waterMainCamera;

        [System.NonSerialized] public Texture2D mapTexture;

        // Cache the landscape and the landscape height
        [System.NonSerialized] public LBLandscape landscape;
        [System.NonSerialized] public float landscapeHeight;
        [System.NonSerialized] public float minPathWidth;

        private Vector3 pathPosWorldSpace = Vector3.zero;
        [System.NonSerialized] public bool showPathPointsList = false;
        [System.NonSerialized] public bool showMesh = false;

#if UNITY_EDITOR
        private GUIStyle distanceLabel;
        private Vector3 distanceLabelOffset;
        private Vector3 pointLabelOffset;
#endif
        // Currently selected Tab in the custom editor
        [System.NonSerialized] public int showTabInEditor = 0;

        #endregion

        #region Enumerations

        #if UNITY_EDITOR
        public enum LBMapPathTab
        {
            General = 0,
            Points = 1,
            Mesh = 2,
            ER = 3
        }

        #endif

        #endregion

        #region Initialisation

        public void OnEnable()
        {
            if (lbPath == null) { lbPath = new LBPath(); }
            if (waterCameraList == null) { waterCameraList = new List<Camera>(); }

            if (lbPath == null) { Debug.LogError("LBMapPath.OnEnable - could not create LBPath object"); }
            else
            {
                // If the pathName is null, update it with the name of the parent GameObject name.
                if (string.IsNullOrEmpty(lbPath.pathName)) { if (gameObject != null) { lbPath.pathName = gameObject.name; } }

                minPathWidth = lbPath.GetMinWidth();
            }

            // Find the landscape in the scene - it should be attached to the parent gameobject
            if (landscape == null)
            {
                if (transform.parent == null)
                {
                    Debug.LogWarning("LBMapPath.OnEnable - We recommend that you move the Map Path gameobject under a Landscape gameobject.");
                }
                else
                {
                    landscape = transform.parent.GetComponent<LBLandscape>();
                    if (landscape != null)
                    {
                        if (landscape.landscapeTerrains == null) { landscape.landscapeTerrains = landscape.GetComponentsInChildren<Terrain>(); }
                        landscapeHeight = landscape.GetLandscapeTerrainHeight();
                    }
                }
            }
            else
            {
                // Avoid any divide by 0 errors
                landscapeHeight = 0.0001f;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// If the name of the gameobject has changed, update the pathName
        /// and return true
        /// </summary>
        /// <returns></returns>
        public bool IsPathNameChanged()
        {
            bool isNameChanged = false;

            if (lbPath != null)
            {
                // Has the user changed the name of the gameobject?
                isNameChanged = (lbPath.pathName != gameObject.name);
                if (isNameChanged) { lbPath.pathName = gameObject.name; }
            }
            return isNameChanged;
        }

        /// <summary>
        /// Get the LBLandscape script that is attached to the parent of this LBMapPath
        /// </summary>
        /// <returns></returns>
        public LBLandscape GetLandscape()
        {
            LBLandscape lbLandscape = null;

            // Get the LBLandscape parent script
            GameObject landscapeGameObject = transform.parent.gameObject;
            if (landscapeGameObject == null)
            {
                Debug.LogWarning("LBMapPath - the Path seems to no longer be a child object of the Landscape");
            }
            else
            {
                lbLandscape = landscapeGameObject.GetComponent<LBLandscape>();
                if (lbLandscape == null) { Debug.LogWarning("LBMapPath - the Path seems to no longer be a child object of the Landscape"); }
            }

            return lbLandscape;
        }

        /// <summary>
        /// Get the list of cameras in the scene - ignore the Celestials Camera uses for the stars
        /// </summary>
        /// <param name="updateAQUASComponents"></param>
        /// <param name="showErrors"></param>
        public void RefreshWaterCameraList(bool updateAQUASComponents, bool showErrors)
        {
            List<Camera> originalList = new List<Camera>();

            // Get all the active cameras in the scene
            Camera[] sceneCameras = GameObject.FindObjectsOfType<Camera>();

            if (waterCameraList == null) { waterCameraList = new List<Camera>(); }

            // Remember the original list of cameras
            if (updateAQUASComponents) { originalList.AddRange(waterCameraList); }

            waterCameraList.Clear();
            waterCameraList.AddRange(sceneCameras);

            // If present, remove the celestrials camera
            waterCameraList.RemoveAll(c => c.gameObject.name == "Celestials Camera" || c.gameObject.name.StartsWith("Water4AdvancedReflection"));

            if (updateAQUASComponents)
            {
                bool isSaveSceneRequired = false;

                // Remove ImageFX on cameras no longer in the list
                foreach (Camera camera in originalList)
                {
                    if (camera != null)
                    {
                        bool isSaveSceneRequiredCamera = false;
                        Camera newListCamera = waterCameraList.Find(c => c == camera);
                        if (newListCamera == null) { LBIntegration.RemoveAQUASCameraScript(camera.gameObject, true, ref isSaveSceneRequiredCamera); }
                        isSaveSceneRequired = isSaveSceneRequired || isSaveSceneRequiredCamera;
                    }
                }

#if UNITY_EDITOR
                if (isSaveSceneRequired && !Application.isPlaying)
                {
                    // Can't save scene here as the overhead is too high for sliders
                    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                }
#endif

                // Verify that refreshed camera list has AQUAS script attached
                VerifyAQUASComponent(showErrors);
            }
        }

        /// <summary>
        /// Verify that the AQUAS camera script is attached to the list of water cameras
        /// </summary>
        /// <param name="showErrors"></param>
        public void VerifyAQUASComponent(bool showErrors)
        {
            if (waterCameraList == null) { waterCameraList = new List<Camera>(); }

            bool isSaveSceneRequired = false;

            foreach (Camera camera in waterCameraList)
            {
                if (camera != null)
                {
                    bool isSaveSceneRequiredCamera = false;
                    LBIntegration.AddAQUASCameraScript(camera.gameObject, true, ref isSaveSceneRequiredCamera);
                    isSaveSceneRequired = isSaveSceneRequired || isSaveSceneRequiredCamera;
                }
            }

            #if UNITY_EDITOR
            if (isSaveSceneRequired && !Application.isPlaying)
            {
                // Can't save scene here as the overhead is too high for sliders
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
            #endif
        }

        /// <summary>
        /// Script out the MapPath for use in a runtime script
        /// </summary>
        /// <param name="EndOfLineMarker"></param>
        /// <returns></returns>
        public string ScriptMapPath(string EndOfLineMarker = "\n")
        {
            // Create a new instance of StringBuilder and give it an estimated capacity
            System.Text.StringBuilder sb = new System.Text.StringBuilder(1000);

            string eol = " ";

            // We always need a space between lines OR a end of line marker like "\n"
            if (EndOfLineMarker.Length > 0) { eol = EndOfLineMarker; }

            // NOTE: if this variable name is changed, must also update LBLayer.ScriptLayer(..)
            string nameNoSpaces = name.Replace(" ", "");
            string pathInst = "lbMapPath" + nameNoSpaces;
            string pathInstAbrev = "path" + nameNoSpaces;

            // We always need a space between lines OR a end of line marker like "\n"
            if (EndOfLineMarker.Length > 0) { eol = EndOfLineMarker; }

            sb.Append("// BEGIN Public variables to populate in the editor - Uncomment and add these to top of class" + eol);

            sb.Append("//[Header(\"" + nameNoSpaces + "\")] " + eol);

            sb.Append("//public Material meshMat" + pathInstAbrev +"; " + eol);
            sb.Append("// END Public variables" + eol + eol);

            sb.Append("#region " + pathInst + " - place before Topography" + eol);

            sb.Append("// " + this.name + " generated from Landscape Builder 2 at " + System.DateTime.Now.ToShortTimeString() + " on " + System.DateTime.Now.ToLongDateString() + eol);

            sb.Append("LBMapPath " + pathInst + " = LBMapPath.CreateMapPath(landscape, landscape.gameObject);" + eol);
            sb.Append("if (" + pathInst + " != null)" + eol);
            sb.Append("{" + eol);
            sb.Append("\t" + pathInst + ".name = \"" + this.name + "\";" + eol);
            sb.Append("\tif (" + pathInst + ".lbPath != null)" + eol);
            sb.Append("\t{" + eol);
            sb.Append("\t\t" + pathInst + ".lbPath.pathName = \"" + this.name + "\";" + eol);
            sb.Append("\t\t" + pathInst + ".lbPath.showPathInScene = false;" + eol);
            sb.Append("\t\t" + pathInst + ".mapResolution = " + this.mapResolution + ";" + eol);
            sb.Append("\t\t" + pathInst + ".lbPath.blendStart = " + this.lbPath.blendStart.ToString().ToLower() + ";" + eol);
            sb.Append("\t\t" + pathInst + ".lbPath.blendEnd = " + this.lbPath.blendEnd.ToString().ToLower() + ";" + eol);
            sb.Append("\t\t" + pathInst + ".lbPath.pathResolution = " + this.lbPath.pathResolution + "f;" + eol);
            sb.Append("\t\t" + pathInst + ".lbPath.closedCircuit = " + this.lbPath.closedCircuit.ToString().ToLower() + ";" + eol);
            sb.Append("\t\t" + pathInst + ".lbPath.edgeBlendWidth = " + this.lbPath.edgeBlendWidth + "f;" + eol);
            sb.Append("\t\t" + pathInst + ".lbPath.removeCentre = " + this.lbPath.removeCentre.ToString().ToLower() + ";" + eol);
            sb.Append("\t\t" + pathInst + ".lbPath.leftBorderWidth = " + this.lbPath.leftBorderWidth + "f;" + eol);
            sb.Append("\t\t" + pathInst + ".lbPath.rightBorderWidth = " + this.lbPath.rightBorderWidth + "f;" + eol);
            sb.Append("\t\t" + pathInst + ".lbPath.snapToTerrain = " + this.lbPath.snapToTerrain.ToString().ToLower() + ";" + eol);
            sb.Append("\t\t" + pathInst + ".lbPath.heightAboveTerrain = " + this.lbPath.heightAboveTerrain + "f;" + eol);
            sb.Append("\t\t" + pathInst + ".lbPath.zoomOnFind = " + this.lbPath.zoomOnFind.ToString().ToLower() + ";" + eol);
            sb.Append("\t\t" + pathInst + ".lbPath.findZoomDistance = " + this.lbPath.findZoomDistance + "f;" + eol);
            sb.Append("\t\t// Mesh options" + eol);
            sb.Append("\t\t" + pathInst + ".lbPath.isMeshLandscapeUV = " + this.lbPath.isMeshLandscapeUV.ToString().ToLower() + ";" + eol);
            sb.Append("\t\t" + pathInst + ".lbPath.meshUVTileScale = new Vector2(" + this.lbPath.meshUVTileScale.x + "," + this.lbPath.meshUVTileScale.y + ");" + eol);
            sb.Append("\t\t" + pathInst + ".lbPath.meshYOffset = " + this.lbPath.meshYOffset + "f;" + eol);
            sb.Append("\t\t" + pathInst + ".lbPath.meshEdgeSnapToTerrain = " + this.lbPath.meshEdgeSnapToTerrain.ToString().ToLower() + ";" + eol);
            sb.Append("\t\t" + pathInst + ".lbPath.meshSnapType = LBPath.MeshSnapType." + this.lbPath.meshSnapType + ";" + eol);
            sb.Append("\t\t" + pathInst + ".lbPath.meshIsDoubleSided = " + this.lbPath.meshIsDoubleSided.ToString().ToLower() + ";" + eol);
            sb.Append("\t\t" + pathInst + ".lbPath.meshIncludeEdges = " + this.lbPath.meshIncludeEdges.ToString().ToLower() + ";" + eol);
            sb.Append("\t\t" + pathInst + ".lbPath.meshIncludeWater = " + this.lbPath.meshIncludeWater.ToString().ToLower() + ";" + eol);
            if (this.lbPath.meshIncludeWater)
            {
                sb.Append("\t\t// Add include water coder here" + eol);
            }
            // Never want to save mesh to project folder at runtime
            sb.Append("\t\t" + pathInst + ".lbPath.meshSaveToProjectFolder = false;" + eol);
            sb.Append("\t\t" + pathInst + ".meshMaterial = meshMat" + pathInstAbrev + ";" + eol);

            // Generate the list of points from the " + pathInst + " LBPath.
            if (lbPath.positionList != null && lbPath.widthList != null && lbPath.positionListLeftEdge != null && lbPath.positionListRightEdge != null)
            {
                sb.Append("\t\t// Path Points" + eol);
                foreach (Vector3 pt in lbPath.positionList)
                {
                    sb.Append("\t\t" + pathInst + ".lbPath.positionList.Add(new Vector3(" + pt.x + "f," + pt.y + "f," + pt.z + "f));" + eol);
                }
                foreach (float widthf in lbPath.widthList)
                {
                    sb.Append("\t\t" + pathInst + ".lbPath.widthList.Add(" + widthf + "f);" + eol);
                }
                foreach (Vector3 pt in lbPath.positionListLeftEdge)
                {
                    sb.Append("\t\t" + pathInst + ".lbPath.positionListLeftEdge.Add(new Vector3(" + pt.x + "f," + pt.y + "f," + pt.z + "f));" + eol);
                }
                foreach (Vector3 pt in lbPath.positionListRightEdge)
                {
                    sb.Append("\t\t" + pathInst + ".lbPath.positionListRightEdge.Add(new Vector3(" + pt.x + "f," + pt.y + "f," + pt.z + "f));" + eol);
                }

                // Update the locally cached minimum path width
                sb.Append("\t\t" + pathInst + ".minPathWidth = " + pathInst + ".lbPath.GetMinWidth();" + eol);
            }

            sb.Append("\t}" + eol);
            sb.Append("}" + eol);
            sb.Append("#endregion" + eol + eol);

            // Second component which should be placed after the Topography generation code
            if (lbPath.positionList != null && lbPath.widthList != null && lbPath.positionListLeftEdge != null && lbPath.positionListRightEdge != null)
            {
                if (lbPath.positionList.Count > 1)
                {
                    sb.Append("#region " + pathInst + " - place after Topography generation" + eol);
                    sb.Append("if(" + pathInst + " != null)" + eol);
                    sb.Append("{" + eol);

                    sb.Append("\t" + pathInst + ".lbPath.RefreshPathHeights(landscape);" + eol);

                    // If there is a mesh material, assume we want to create a mesh
                    if (this.meshMaterial != null)
                    {
                        sb.Append("\t// Create Mesh for Path" + eol);
                        sb.Append("\tif(" + pathInst + ".lbPath.CreateMeshFromPath(landscape))" + eol);
                        sb.Append("\t{" + eol);
                        sb.Append("\t\tVector3 meshPosition = new Vector3(0f, " + pathInst + ".lbPath.meshYOffset, 0f);" + eol);

                        sb.Append("\t\tTransform meshTransform = LBMeshOperations.AddMeshToScene(" + pathInst + ".lbPath.lbMesh, meshPosition, " + pathInst + ".lbPath.pathName + \" Mesh\", " + pathInst + ".transform, " + pathInst + ".meshMaterial, true, false);" + eol);
                        sb.Append("\t\tif (meshTransform != null) { }" + eol);
                        sb.Append("\t}" + eol);
                    }
                    sb.Append("}" + eol);

                    sb.Append("#endregion" + eol + eol);
                }
            }

            sb.Append("// END OF CODE SEGMENT" + eol);

            return sb.ToString();
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Create a new Map Path in the scene and return the LBMapPath instance
        /// </summary>
        /// <returns></returns>
        public static LBMapPath CreateMapPath(LBLandscape landscape, GameObject landscapeGameObject)
        {
            LBMapPath lbMapPath = null;

            // Create a map path object
            GameObject mapPathObj = new GameObject("Map Path");
            if (mapPathObj != null)
            {
                mapPathObj.transform.position = landscapeGameObject.transform.position;

                // Make this path a child of the landscape
                mapPathObj.transform.parent = landscapeGameObject.transform;

                lbMapPath = mapPathObj.AddComponent<LBMapPath>();

                if (lbMapPath != null)
                {
                    lbMapPath.showTabInEditor = 0;

                    // Create a new path instance
                    lbMapPath.lbPath = new LBPath(LBPath.PathType.MapPath);
                    if (lbMapPath.lbPath != null)
                    {
                        // Configure the path instance
                        lbMapPath.lbPath.pathName = mapPathObj.name;
                    }
                }
            }

            return lbMapPath;
        }

        /// <summary>
        /// Create a duplicate in the scene of an existing MapPath gameobject
        /// Simply duplicating the gameobject won't work because it will retain references to existing class instances
        /// NOTE: Does not duplicate child objects like meshes as this may be undesirable
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="landscapeGameObject"></param>
        /// <param name="lbMapPathOriginal"></param>
        /// <returns></returns>
        public static LBMapPath CreateMapPathDuplicate(LBLandscape landscape, GameObject landscapeGameObject, LBMapPath lbMapPathOriginal)
        {
            LBMapPath lbMapPath = null;

            // Basic validation
            if (landscape == null) { Debug.LogWarning("ERROR: LBMapPath.CreateMapPathDuplicate - landscape cannot be null"); }
            else if (lbMapPathOriginal == null) { Debug.LogWarning("ERROR: LBMapPath.CreateMapPathDuplicate - source MapPath cannot be null"); }
            else if (lbMapPathOriginal.lbPath == null) { Debug.LogWarning("ERROR: LBMapPath.CreateMapPathDuplicate - source Path cannot be null"); }
            else
            {
                // Create a map path object
                GameObject mapPathObj = new GameObject(lbMapPathOriginal.lbPath.pathName + " (Dup)");
                if (mapPathObj != null)
                {
                    mapPathObj.transform.position = landscapeGameObject.transform.position;

                    // Make this path a child of the landscape
                    mapPathObj.transform.parent = landscapeGameObject.transform;

                    lbMapPath = mapPathObj.AddComponent<LBMapPath>();

                    if (lbMapPath != null)
                    {
                        // Create a new path instance as a copy of the original
                        lbMapPath.lbPath = new LBPath(lbMapPathOriginal.lbPath);
                        if (lbMapPath.lbPath != null)
                        {
                            // Configure the path instance
                            lbMapPath.lbPath.pathName = mapPathObj.name;

                            // Delete any mesh class instance data attached to the path
                            LBMesh.DeleteMesh(lbMapPath.lbPath.lbMesh);
                        }
                    }
                }
            }

            return lbMapPath;
        }

        /// <summary>
        /// Get a list of all the LBMapPaths under a landscape parent gameobject
        /// If there are many prefabs in the landscape this could be slow so don't
        /// put it in an Update()
        /// </summary>
        /// <param name="lbLandscape"></param>
        /// <returns></returns>
        public static List<LBMapPath> GetMapPathsInLandscape(LBLandscape lbLandscape)
        {
            if (lbLandscape == null) { return new List<LBMapPath>(); }
            else { return new List<LBMapPath>(lbLandscape.GetComponentsInChildren<LBMapPath>(true)); }
        }

        /// <summary>
        /// Remove all existing Map Path's from the scene under a landscape parent gameobject
        /// </summary>
        /// <param name="lbLandscape"></param>
        public static void RemoveMapPathsFromLandscape(LBLandscape lbLandscape)
        {
            List<LBMapPath> lbMapPathList = GetMapPathsInLandscape(lbLandscape);

            if (lbMapPathList != null)
            {
                // Go backwards through the list
                for (int i = lbMapPathList.Count - 1; i >= 0; i--)
                {
                    DestroyImmediate(lbMapPathList[i].gameObject);
                }
            }
        }

        #endregion

        #region Private Methods including DrawGizmos

        // Gizmo Drawing
        private void OnDrawGizmos()
        {
            if (lbPath == null) { return; }
            if (lbPath.showPathInScene) { lbPath.CachePathPointDistances(); DrawPath(false, false, false); }
        }

        private void OnDrawGizmosSelected()
        {
            if (lbPath == null) { return; }

            #if MAP_PATH_DEBUG_MODE
            if (lbPath.showPathInScene) { lbPath.CachePathPointDistances(); DrawPath(true, true, true); }
            #else
            if (lbPath.showPathInScene) { lbPath.CachePathPointDistances(); DrawPath(true, false, false); }
            #endif
        }

        /// <summary>
        /// Draw the path in the scene.
        /// </summary>
        /// <param name="selected"></param>
        private void DrawPath(bool selected, bool isCentreSplineDrawn, bool isEdgeSegmentPointsDrawn)
        {
            if (lbPath == null) { return; }
            else if (lbPath.cachedPathPointDistances == null || lbPath.positionList == null) { return; }

            if (lbPath.positionList.Count > 0)
            {
                #if UNITY_EDITOR
                bool isDistancesVisible = lbPath.showDistancesInScene;
                distanceLabelOffset = Vector3.up * 1.5f;
                pointLabelOffset = Vector3.up * 3f;
                bool isPointLabelVisible = lbPath.showPointLabelsInScene;
                if (isDistancesVisible || isPointLabelVisible)
                {
                    distanceLabel = new GUIStyle("Box");
                    distanceLabel.fontSize = 10;
                    distanceLabel.border = new RectOffset(1, 1, 1, 1);
                    distanceLabel.onFocused.textColor = UnityEngine.Color.white;
                }
                #endif

                // Path colour is stored as a vector4
                Color unSelectedColour = new Color(lbPath.pathPointColour.x, lbPath.pathPointColour.y, lbPath.pathPointColour.z, 0.25f);
                Color lineColour = (selected ? (Color)lbPath.pathPointColour : unSelectedColour);
                Color positionColour = (selected ? (Color)lbPath.pathPointColour : unSelectedColour);
                Color leftSegmentPointColour = Color.yellow;
                Color rightSegmentPointColour = Color.blue;

                int numPositions = lbPath.positionList.Count;
                Vector3 from = Vector3.zero;
                Vector3 to = Vector3.zero;

                Gizmos.color = positionColour;

                // Draw a sphere gizmo at the position of every point in the map path
                for (int i = 0; i < numPositions; i++)
                {
                    // Get the position Vector3 only once from the list.
                    pathPosWorldSpace = lbPath.positionList[i];

                    // Draw the central point of the spline
                    Gizmos.DrawSphere(pathPosWorldSpace, 1f * lbPath.pointDisplayScale);

                    // Add the left width marker
                    Gizmos.DrawSphere(lbPath.positionListLeftEdge[i], 0.5f * lbPath.pointDisplayScale);
                    // Add the right width marker
                    Gizmos.DrawSphere(lbPath.positionListRightEdge[i], 0.5f * lbPath.pointDisplayScale);

                    if (lbPath.selectedList.FindIndex(s => s == i) >= 0)
                    {
                        Gizmos.DrawWireCube(pathPosWorldSpace, Vector3.one * 1.5f * lbPath.pointDisplayScale);
                    }

                    #if UNITY_EDITOR
                    if (isPointLabelVisible)
                    {
                        // Only show the Point labels if they are enabled in Editor AND infront of the scene view camera
                        if (LBLandscape.IsPointInCameraView(SceneView.lastActiveSceneView.camera, pathPosWorldSpace))
                        {
                            Handles.Label(pathPosWorldSpace + pointLabelOffset, "Pt: " + (i + 1).ToString("000"), distanceLabel);
                        }
                    }
                    #endif
                }

                // To draw the path lines we need at least 2 points
                if (lbPath.positionList.Count > 1)
                {
                    // ORIGINAL WORKING
                    lbPath.CacheSplinePointDistances();
                    Vector3[] splinePointsCentre = lbPath.cachedCentreSplinePoints;
                    Vector3[] splinePointsLeft = lbPath.GetSplinePathEdgePoints(LBPath.PositionType.Left, false, true);
                    Vector3[] splinePointsRight = lbPath.GetSplinePathEdgePoints(LBPath.PositionType.Right, false, true);

                    // TEST CODE - Currently buggy and slow
                    //lbPath.CacheSplinePoints2();
                    //Vector3[] splinePointsCentre = lbPath.cachedCentreSplinePointList.ToArray();
                    //Vector3[] splinePointsLeft = lbPath.GetSplinePathEdgePoints2(LBPath.PositionType.Left, true, true);
                    //Vector3[] splinePointsRight = lbPath.GetSplinePathEdgePoints2(LBPath.PositionType.Right, true, true);

                    int numSplinePoints = 0;

                    // Draw the centre spline
                    // Uncomment the define MAP_PATH_DEBUG_MODE at the top of this script to enable.
                    if (splinePointsCentre != null && isCentreSplineDrawn)
                    {
                        numSplinePoints = splinePointsCentre.Length;
                        if (numSplinePoints > 1)
                        {
                            from = splinePointsCentre[0];
                            Gizmos.color = lineColour;
                            for (int i = 0; i < numSplinePoints; i++)
                            {
                                to = splinePointsCentre[i];
                                if (selected)
                                {
                                    Gizmos.DrawSphere(to, 0.25f * lbPath.pointDisplayScale);

                                    #if UNITY_EDITOR
                                    // Also displays a ghosted handle from behind the camera. IsPointInCameraView should fix this (it does with isPointLabelVisible)... 
                                    if (isDistancesVisible)
                                    {
                                        if (LBLandscape.IsPointInCameraView(SceneView.lastActiveSceneView.camera, to))
                                        {
                                            Handles.Label(to + distanceLabelOffset, "Distance: " + lbPath.cachedCentreSplinePointDistances[i].ToString("0.00"), distanceLabel);
                                        }
                                    }
                                    #endif
                                }

                                Gizmos.DrawLine(from, to);
                                // Set the end of this line as the start of the next
                                from = to;
                            }

                            // Draw a line from the last location to the final point.
                            Gizmos.DrawLine(from, lbPath.positionList[numPositions - 1]);

                            if (lbPath.closedCircuit)
                            {
                                // Draw a final line to connect the end with the beginning
                                Gizmos.DrawLine(lbPath.positionList[numPositions - 1], lbPath.positionList[0]);
                            }
                        }
                    }

                    // Draw the left edge
                    if (splinePointsLeft != null)
                    {
                        numSplinePoints = splinePointsLeft.Length;
                        if (numSplinePoints > 1)
                        {
                            from = splinePointsLeft[0];
                            for (int i = 0; i < numSplinePoints; i++)
                            {
                                to = splinePointsLeft[i];
                                if (selected && isEdgeSegmentPointsDrawn)
                                {
                                    Gizmos.color = leftSegmentPointColour;
                                    Gizmos.DrawSphere(to, 0.25f * lbPath.pointDisplayScale);
                                }
                                Gizmos.color = lineColour;
                                Gizmos.DrawLine(from, to);
                                // Set the end of this line as the start of the next
                                from = to;
                            }

                            // Draw a line from the last location to the final point.
                            Gizmos.DrawLine(from, lbPath.positionListLeftEdge[numPositions - 1]);

                            if (lbPath.closedCircuit)
                            {
                                // Draw a final line to connect the end with the beginning
                                Gizmos.DrawLine(lbPath.positionListLeftEdge[numPositions - 1], lbPath.positionListLeftEdge[0]);
                            }
                        }
                    }

                    // Draw the right edge
                    from = lbPath.positionListLeftEdge[0];
                    if (splinePointsRight != null)
                    {
                        numSplinePoints = splinePointsRight.Length;
                        if (numSplinePoints > 1 && splinePointsLeft.Length == numSplinePoints)
                        {
                            from = splinePointsRight[0];
                            for (int i = 0; i < numSplinePoints; i++)
                            {
                                to = splinePointsRight[i];
                                if (selected && isEdgeSegmentPointsDrawn)
                                {
                                    Gizmos.color = rightSegmentPointColour;
                                    Gizmos.DrawSphere(to, 0.25f * lbPath.pointDisplayScale);
                                }
                                Gizmos.color = lineColour;
                                Gizmos.DrawLine(from, to);
                                // Set the end of this line as the start of the next
                                from = to;

                                Gizmos.DrawLine(to, splinePointsLeft[i]);
                            }

                            // Draw a line from the last location to the final point.
                            Gizmos.DrawLine(from, lbPath.positionListRightEdge[numPositions - 1]);
                            Gizmos.DrawLine(lbPath.positionListRightEdge[numPositions - 1], lbPath.positionListLeftEdge[numPositions - 1]);

                            if (lbPath.closedCircuit)
                            {
                                // Draw a final line to connect the end with the beginning
                                Gizmos.DrawLine(lbPath.positionListRightEdge[numPositions - 1], lbPath.positionListRightEdge[0]);
                            }
                        }
                    }
                }
            }
        }

        #endregion
    }

    #region Custom Editor

#if UNITY_EDITOR
    [CustomEditor(typeof(LBMapPath))]
    public class LBMapPathInspector : Editor
    {
        #region Variables

        // Custom editor for LB Map Path
        private bool isSceneSaveRequired = false;
        private int insertPointPos = -1;
        private int deletePointPos = -1;
        private int findPointPos = -1;
        private GUIStyle buttonCompact;
        private GUIStyle widthLabel;
        private Vector3 widthLabelOffset = Vector3.up * 4f;
        private bool isGetWidth = false;
        private bool isAddWidth = false;
        private float pathWidth = LBPath.GetDefaultPathWidth;
        private float addWidth = 0f;
        private List<int> gizmoClassIdList;
        private int findZoomDistanceInt = 0;
        private int heightAboveTerrainInt = 0;
        private int pointDisplayScaleInt = 0;
        private int pathResolutionInt = 50;
        private int numPointsInList = 0;
        private string labelText;
        private GUIStyle helpBoxRichText;
        private GUIStyle labelFieldRichText;
        private string txtColourName = "Black";
        private bool isSelected = false;
        private bool autoSaveEnabled = false;
        //private bool sceneSaved = false;

        // EasyRoads3D integration
        private string easyRoadsInstalledText = "";
        private bool isContinueToAddPoints = false;
        private int i = 0;
        private LBRoad lbRoad = null;
        private LBRoad lbRoadMoveDown = null;
        private LBRoad lbCompareRoad = null;
        private int lbRoadMovePos = 0;
        private List<LBRoad> tempRoadList = null;
        private int splinePointFilterSize = 0;

        // Used in OnSceneGUI
        private Vector2 posXZ = Vector2.zero;
        private Vector3 snappedPos = Vector3.zero;
        private Vector3 startPos = Vector3.zero;
        private Rect landscapeBounds;
        private bool isCtrlKeyPressed = false;
        private bool isDistancesVisible = false;
        private Vector3 forwardsDir = Vector3.forward;
        private int numPositionsSceneGUI = 0;
        #endregion

        #region Static GUIContent
        private readonly static GUIContent scriptMapPathContent = new GUIContent("S", "Script the MapPath to the Console window");
        private readonly static GUIContent[] tabMapPathContent = {new GUIContent("General"), new GUIContent("Points"), new GUIContent("Mesh"), new GUIContent("ER")};

        private readonly static GUIContent closedCircuitContent = new GUIContent("Closed Circuit");
        private readonly static GUIContent showPathInSceneContent = new GUIContent("Show Path in Scene", "When enabled the path will be displayed in the scene view");
        private readonly static GUIContent showDistancesInSceneContent = new GUIContent("Show Distances", "When enabled the distances from the start will be displayed in the scene view");
        private readonly static GUIContent showPointLabelInSceneContent = new GUIContent("Show Pt Labels", "When enabled the point labels will be displayed in the scene view.");
        private readonly static GUIContent pathPointColourContent = new GUIContent("Display Colour", "The display path point and spline colour in the scene view.");

        private readonly static GUIContent pathResolutionContent = new GUIContent("Path Resolution", "This is the size of the segments that make up the path. A lower number will be a result in a higher quality path but will be slower to render and map creation will be slower too. (Default: 5)");
        private readonly static GUIContent blendStartContent = new GUIContent("Blend Start", "Blend the starting edge with the surrounds using the Edge Width distance");
        private readonly static GUIContent blendEndContent = new GUIContent("Blend End", "Blend the ending edge with the surrounds using the Edge Width distance");
        private readonly static GUIContent edgeBlendWidthContent = new GUIContent("Edge Width", "The width of the edge that will be blended with the surrounds");
        private readonly static GUIContent removeCentreContent = new GUIContent("Remove Centre", "Do not include the centre of the path when creating a Map. Only include the left and right borders of the path.");
        private readonly static GUIContent leftBorderWidthContent = new GUIContent("Left Border Width", "The width of the left border that will be included when creating a Map texture");
        private readonly static GUIContent rightBorderWidthContent = new GUIContent("Right Border Width", "The width of the right border that will be included when creating a Map texture");
        private readonly static GUIContent snapToTerrainContent = new GUIContent("Snap To Terrain", "Should the path points follow the height of the terrain?");
        private readonly static GUIContent resnapToTerrainButtonContent = new GUIContent("RE-SNAP", "Re-snap the path to the terrain.");
        private readonly static GUIContent heightOffsetContent = new GUIContent("Height Offset", "When 'Snap To Terrain' is enabled, the points will maintain the height specified as they are drawn in the scene view.");
        private readonly static GUIContent pointDisplayScaleContent = new GUIContent("Point Display Size", "The relative size of the path point gizmos in the scene view");

        private readonly static GUIContent zoomOnFindContent = new GUIContent("Zoom On Find", "Display the path point in the scene view, and zoom to the indicated distance");
        private readonly static GUIContent findZoomContent = new GUIContent("Find Zoom", "The distance to zoom out when (F)inding a path point in the scene view");
        private readonly static GUIContent isCreateMapPerTerrainContent = new GUIContent("1 Map per Terrain", "Used for 3rd party products like, Vegetation Studio, that require texture filters or masks for each terrain in the landscape. By default, a single map texture is created for the whole landscape.");
        private readonly static GUIContent createMapButtonContent = new GUIContent("Create Map Texture", "Create and save Map Texture(s) in the Assets/LandscapeBuilder/Maps folder. This texture can be used as a Map rule for Texturing, Trees, Grass placement. In LB Editor on Advanced tab, enable GPU Acceleration - Path for faster creation.");

        private readonly static GUIContent meshLandscapeUVContent = new GUIContent("Mesh Landscape UVs", "UVs for mesh that is created will be based on the dimensions of the landscape, rather than the actual mesh. Can be useful when creating the water surface of a river.");
        private readonly static GUIContent meshUVTilingContent = new GUIContent("Mesh UV Tiling", "UVs for mesh that is created will be scaled or tiled in x and z directions of the landscape");
        private readonly static GUIContent meshYOffsetContent = new GUIContent("Mesh Y Offset", "The offset on the Y-axis between the Map Path gameobject and the mesh");
        private readonly static GUIContent meshEdgeSnapToTerrain = new GUIContent("Mesh Edge Snap", "The edges of the mesh will be snapped to the height of the terrain");
        private readonly static GUIContent meshSnapTypeContent = new GUIContent("Edge Snap Type", "How to snap the edges of the path to the terrain height");
        private readonly static GUIContent meshIsDoubleSidedContent = new GUIContent("Is Double-sided", "Make the mesh double-sided. It will have twice the number of triangles as single-sided.");
        private readonly static GUIContent meshIncludeEdgesContent = new GUIContent("Mesh Include Edges", "Include the edges when creating the mesh");
        private readonly static GUIContent meshMaterialContent = new GUIContent("Mesh Material", "The material used to render onto the path mesh");
        private readonly static GUIContent meshSaveToProjectFolderContent = new GUIContent("Add to Project folder", "Save the Mesh to Assets/LandscapeBuilder/Meshes Project folder. Typically used if creating a prefab for export.");
        private readonly static GUIContent meshIncludeWaterContent = new GUIContent("Mesh Include Water", "Add water to the mesh that is created");
        private readonly static GUIContent meshWaterMainCameraContent = new GUIContent("Water Main Camera", "The scene's main camera");
        private readonly static GUIContent meshWaterResizingModeContent = new GUIContent("Water Resizing Mode", "How to resize the water to fit the mesh");
        private readonly static GUIContent bakeNavMeshButtonContent = new GUIContent("Bake", "Bake NavMesh for the scene");
        private readonly static GUIContent cancelBakeButtonContent = new GUIContent("Cancel");
        private readonly static GUIContent openNavigationButtonContent = new GUIContent("NavMesh Settings");

        #endregion

        #region Initialisation

        // Add a menu item so that a map path can be created via the GameObject > 3D Object menu
        [MenuItem("GameObject/3D Object/Landscape Builder/Map Path")]
        public static void ShowWindow()
        {
            // Create a map path object
            GameObject mapPathObj = new GameObject("Map Path");
            mapPathObj.AddComponent<LBMapPath>();
        }

        /// <summary>
        /// Called automatically by Unity when the Editor get's enabled, which
        /// is essentially when the parent gameobject gets selected in the Hierarchy
        /// </summary>
        private void OnEnable()
        {
            LBMapPath mapPathScript = (LBMapPath)target;

            if (gizmoClassIdList == null) { gizmoClassIdList = new List<int>(); }
            if (gizmoClassIdList != null)
            {
                // Only populate the list if it hasn't been done yet
                //if (gizmoClassIdList.Count == 0)
                //{
                //    gizmoClassIdList.Add(20);  // Camera
                //}
            }

            // Turn off the gizmos for the selected Path Gameobject
            Tools.hidden = true;
            // Switch to the Move tool
            Tools.current = Tool.Move;

            if (mapPathScript.landscape == null) { landscapeBounds = new Rect(-100000f, -100000f, 200000f, 200000f); }
            else
            {
                landscapeBounds = mapPathScript.landscape.GetLandscapeWorldBoundsFast();
            }

            if (mapPathScript.IsPathNameChanged())
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }

            // Dark skin uses a slightly transparent white, while personal edition uses a black
            if (EditorGUIUtility.isProSkin) { txtColourName = "#ffffffaa"; }
            else { txtColourName = "black"; }

            autoSaveEnabled = LBLandscape.GetAutoSaveState();
            if (autoSaveEnabled) { }
        }

        /// <summary>
        /// Called automatically by Unity when the gameobject loses focus
        /// </summary>
        private void OnDisable()
        {
            // Turn on the default scene handles
            Tools.hidden = false;
            Tools.current = Tool.Move;
        }

        private void OnDestroy()
        {

        }

        #endregion

        // This function overides what is normally seen in the inspector window
        // This allows stuff like buttons to be drawn there
        public override void OnInspectorGUI()
        {
            #region Initialise
            LBMapPath mapPathScript = (LBMapPath)target;

            if (mapPathScript.lbPath == null) { return; }

            EditorGUIUtility.labelWidth = 150f;
            buttonCompact = new GUIStyle("Button");
            buttonCompact.fontSize = 8;
            helpBoxRichText = new GUIStyle("HelpBox");
            helpBoxRichText.richText = true;
            labelFieldRichText = new GUIStyle("Label");
            labelFieldRichText.richText = true;

            if (mapPathScript.lbPath.positionList == null) { mapPathScript.lbPath.positionList = new List<Vector3>(); numPointsInList = 0; }
            if (mapPathScript.lbPath.positionListLeftEdge == null) { mapPathScript.lbPath.positionListLeftEdge = new List<Vector3>(); }
            if (mapPathScript.lbPath.positionListRightEdge == null) { mapPathScript.lbPath.positionListRightEdge = new List<Vector3>(); }

            // Get it only once in this frame
            numPointsInList = mapPathScript.lbPath.positionList.Count;
            #endregion

            #region Path Name and length
            if (mapPathScript.lbPath.pathName.Length == 0 || mapPathScript.lbPath.pathName.ToLower() == "map path")
            {
                EditorGUILayout.HelpBox("Please use a unique Map Path Name for the project, especially if you are creating map textures from the path.", MessageType.Warning);
            }

            EditorGUILayout.HelpBox("Path length: " + mapPathScript.lbPath.splineLength.ToString("0") + "m", MessageType.Info);
            #endregion

            GUILayout.BeginVertical(helpBoxRichText);
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("<b>" + mapPathScript.lbPath.pathName + "</b>", labelFieldRichText);
            // Script out the MapPath settings to the console window
            if (GUILayout.Button(scriptMapPathContent, buttonCompact, GUILayout.MaxWidth(20f)))
            {
                #if UNITY_2018_2_OR_NEWER
                LBEditorHelper.CallMenu("Window/General/Console");
                #else
                LBEditorHelper.CallMenu("Window/Console");
                #endif
                Debug.Log(mapPathScript.ScriptMapPath("\n"));
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            if (mapPathScript.showTabInEditor > 3) { mapPathScript.showTabInEditor = 0; }
            mapPathScript.showTabInEditor = GUILayout.Toolbar(mapPathScript.showTabInEditor, tabMapPathContent);

            #region General Tab
            if (mapPathScript.showTabInEditor == (int)LBMapPath.LBMapPathTab.General)
            {
                #region Show Options
                GUILayout.BeginVertical(helpBoxRichText);
                EditorGUI.BeginChangeCheck();
                mapPathScript.lbPath.showPathInScene = EditorGUILayout.Toggle(showPathInSceneContent, mapPathScript.lbPath.showPathInScene);
                mapPathScript.lbPath.showDistancesInScene = EditorGUILayout.Toggle(showDistancesInSceneContent, mapPathScript.lbPath.showDistancesInScene);
                mapPathScript.lbPath.showPointLabelsInScene = EditorGUILayout.Toggle(showPointLabelInSceneContent, mapPathScript.lbPath.showPointLabelsInScene);
                if (EditorGUI.EndChangeCheck()) { isSceneSaveRequired = true; }
                #endregion

                #region Display Colour

                mapPathScript.lbPath.pathPointColour = EditorGUILayout.ColorField(pathPointColourContent, mapPathScript.lbPath.pathPointColour);

                #endregion

                #region ClosedCircuit
                EditorGUI.BeginChangeCheck();
                mapPathScript.lbPath.closedCircuit = EditorGUILayout.Toggle(closedCircuitContent, mapPathScript.lbPath.closedCircuit);
                if (EditorGUI.EndChangeCheck())
                {
                    if (!mapPathScript.useEasyRoads)
                    {
                        // Check if final point is at the same location as the first one.
                        if (numPointsInList > 1)
                        {
                            if (mapPathScript.lbPath.positionList[0] != mapPathScript.lbPath.positionList[numPointsInList - 1])
                            {
                                if (mapPathScript.lbPath.closedCircuit)
                                {
                                    // If the last position isn't the same as the first, add it in.
                                    mapPathScript.lbPath.positionList.Add(mapPathScript.lbPath.positionList[0]);
                                    mapPathScript.lbPath.widthList.Add(mapPathScript.lbPath.widthList[0]);
                                    mapPathScript.lbPath.positionListLeftEdge.Add(mapPathScript.lbPath.positionListLeftEdge[0]);
                                    mapPathScript.lbPath.positionListRightEdge.Add(mapPathScript.lbPath.positionListRightEdge[0]);
                                    numPointsInList++;
                                    isSceneSaveRequired = true;
                                }
                            }
                            else if (!mapPathScript.lbPath.closedCircuit)
                            {
                                // If disabling closedCircuit, remove the last point if it is the same as first.
                                // It might be in the selected list, so remove that first
                                mapPathScript.lbPath.selectedList.Remove(numPointsInList - 1);

                                // Remove the point from position and width lists.
                                mapPathScript.lbPath.positionList.RemoveAt(numPointsInList - 1);
                                mapPathScript.lbPath.positionListLeftEdge.RemoveAt(numPointsInList - 1);
                                mapPathScript.lbPath.positionListRightEdge.RemoveAt(numPointsInList - 1);
                                mapPathScript.lbPath.widthList.RemoveAt(numPointsInList - 1);

                                // Update the locally cached minimum path width
                                mapPathScript.minPathWidth = mapPathScript.lbPath.GetMinWidth();
                                numPointsInList--;
                                isSceneSaveRequired = true;
                            }
                        }
                    }
                }
                #endregion

                EditorGUI.BeginChangeCheck();
                // Store the value as a float, but display as Integer in slider
                pathResolutionInt = Mathf.RoundToInt(mapPathScript.lbPath.pathResolution);
                pathResolutionInt = EditorGUILayout.IntSlider(pathResolutionContent, pathResolutionInt, 2, 100);
                mapPathScript.lbPath.pathResolution = (float)pathResolutionInt;

                mapPathScript.lbPath.edgeBlendWidth = EditorGUILayout.Slider(edgeBlendWidthContent, mapPathScript.lbPath.edgeBlendWidth, (mapPathScript.minPathWidth / 2f < 10f ? 0 : 1f), mapPathScript.minPathWidth / 2f);

                // When the path is a complete circuit, the ends will not be blended.
                if (!mapPathScript.lbPath.closedCircuit)
                {
                    mapPathScript.lbPath.blendStart = EditorGUILayout.Toggle(blendStartContent, mapPathScript.lbPath.blendStart);
                    mapPathScript.lbPath.blendEnd = EditorGUILayout.Toggle(blendEndContent, mapPathScript.lbPath.blendEnd);
                }

                mapPathScript.lbPath.removeCentre = EditorGUILayout.Toggle(removeCentreContent, mapPathScript.lbPath.removeCentre);
                if (mapPathScript.lbPath.removeCentre)
                {
                    mapPathScript.lbPath.leftBorderWidth = EditorGUILayout.Slider(leftBorderWidthContent, mapPathScript.lbPath.leftBorderWidth, 0f, mapPathScript.minPathWidth / 2f);
                    mapPathScript.lbPath.rightBorderWidth = EditorGUILayout.Slider(rightBorderWidthContent, mapPathScript.lbPath.rightBorderWidth, 0f, mapPathScript.minPathWidth / 2f);
                }

                if (EditorGUI.EndChangeCheck()) { isSceneSaveRequired = true; }

                #region Snap to Terrain
                EditorGUI.BeginChangeCheck();
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(snapToTerrainContent, GUILayout.Width(EditorGUIUtility.labelWidth - 4f));
                mapPathScript.lbPath.snapToTerrain = EditorGUILayout.Toggle(mapPathScript.lbPath.snapToTerrain, GUILayout.Width(15f));
                if (mapPathScript.lbPath.snapToTerrain)
                {
                    // We don't need to call anything here because the button click will trigger the EditorGUI.EndChangeCheck below
                    GUILayout.Button(resnapToTerrainButtonContent, buttonCompact, GUILayout.MaxWidth(50f));
                }
                GUILayout.EndHorizontal();
                if (EditorGUI.EndChangeCheck())
                {
                    if (mapPathScript.lbPath.snapToTerrain) { mapPathScript.lbPath.RefreshPathHeights(mapPathScript.landscape); }
                    isSceneSaveRequired = true;
                }

                if (mapPathScript.lbPath.snapToTerrain)
                {
                    // Store the value as a float, but display as Integer in slider
                    heightAboveTerrainInt = Mathf.RoundToInt(mapPathScript.lbPath.heightAboveTerrain);
                    EditorGUI.BeginChangeCheck();
                    heightAboveTerrainInt = EditorGUILayout.IntSlider(heightOffsetContent, heightAboveTerrainInt, 0, 100);
                    if (EditorGUI.EndChangeCheck())
                    {
                        mapPathScript.lbPath.heightAboveTerrain = (float)heightAboveTerrainInt;
                        mapPathScript.lbPath.RefreshPathHeights(mapPathScript.landscape);
                        isSceneSaveRequired = true;
                    }
                }
                #endregion
                
                #region Point Display Scale

                pointDisplayScaleInt = mapPathScript.lbPath.GetPointDisplayScaleAsInt();
                EditorGUI.BeginChangeCheck();
                pointDisplayScaleInt = EditorGUILayout.IntSlider(pointDisplayScaleContent, pointDisplayScaleInt, 1, 5);
                if (EditorGUI.EndChangeCheck())
                {
                    mapPathScript.lbPath.SetPointDisplayScale(pointDisplayScaleInt);
                    if (mapPathScript.lbPath.showPathInScene) { SceneView.RepaintAll(); }
                    isSceneSaveRequired = true;
                }

                #endregion

                EditorGUI.BeginChangeCheck();

                mapPathScript.lbPath.zoomOnFind = EditorGUILayout.Toggle(zoomOnFindContent, mapPathScript.lbPath.zoomOnFind);

                if (mapPathScript.lbPath.zoomOnFind)
                {
                    // Store the value as a float, but display as Integer in slider
                    findZoomDistanceInt = Mathf.RoundToInt(mapPathScript.lbPath.findZoomDistance);
                    findZoomDistanceInt = EditorGUILayout.IntSlider(findZoomContent, findZoomDistanceInt, 2, 1000);
                    mapPathScript.lbPath.findZoomDistance = (float)findZoomDistanceInt;
                }

                if (numPointsInList > 1)
                {
                    mapPathScript.mapResolution = EditorGUILayout.Popup("Map Resolution", mapPathScript.mapResolution, LBMap.MapResolutionArray, GUILayout.Width(220f));
                    mapPathScript.isCreateMapPerTerrain = EditorGUILayout.Toggle(isCreateMapPerTerrainContent, mapPathScript.isCreateMapPerTerrain);
                }

                // Currently this is automatically calculated in LBMap.CreateMapFromPath()
                //mapPathScript.lbPath.showAdvancedOptions = EditorGUILayout.Foldout(mapPathScript.lbPath.showAdvancedOptions, new GUIContent("Advanced Options"));
                //if (mapPathScript.lbPath.showAdvancedOptions)
                //{
                //    mapPathScript.lbPath.curveDetectionInner = EditorGUILayout.IntSlider(new GUIContent("Curve Detection Inner", "Inner curve detection for sharp or wide bends [default 3]. Creating a map will be slower with higher values."), mapPathScript.lbPath.curveDetectionInner, 3, 25);
                //    mapPathScript.lbPath.curveDetectionOuter = EditorGUILayout.IntSlider(new GUIContent("Curve Detection Outer", "Outer curve detection for sharp or wide bends [default 3]. Creating a map will be slower with higher values."), mapPathScript.lbPath.curveDetectionOuter, 3, 25);
                //}

                if (EditorGUI.EndChangeCheck()) { isSceneSaveRequired = true; }

                EditorGUILayout.BeginHorizontal();

                #region Create Map
                if (numPointsInList > 1)
                {
                    //if (GUILayout.Button("Create Map" + (mapPathScript.isCreateMapPerTerrain ? "s" : ""), GUILayout.MaxWidth(100f)))
                    if (GUILayout.Button(createMapButtonContent, GUILayout.MaxWidth(130f)))
                    {
                        // Get the LBLandscape parent script
                        LBLandscape landscape = mapPathScript.GetLandscape();
                        if (landscape != null)
                        {
                            EditorUtility.DisplayProgressBar("Creating Map", "Please Wait", 0.2f);

                            // Ensure the name of the path matches the gameobject before creating the map texture.
                            if (mapPathScript.IsPathNameChanged()) { isSceneSaveRequired = true; }

                            // Create the Map Texture with the path as a solid colour
                            mapPathScript.mapTexture = mapPathScript.lbPath.CreateMapFromPath(landscape, LBMap.MapResolution(mapPathScript.mapResolution));

                            if (mapPathScript.mapTexture != null)
                            {
                                string folderMapPath = LBMap.GetDefaultMapFolder + "/" + landscape.name;

                                LBEditorHelper.CheckFolder(LBMap.GetDefaultMapFolder);
                                LBEditorHelper.CheckFolder(folderMapPath);

                                bool continueToSave = false;
                                string mapFilePath = string.Empty;

                                // Override default behaviour, and create a texture for each terrain?
                                if (mapPathScript.isCreateMapPerTerrain)
                                {
                                    landscape.SetLandscapeTerrains(true);

                                    int numTerrains = (landscape.landscapeTerrains == null ? 0 : landscape.landscapeTerrains.Length);

                                    // Check if there is an existing texture with the same name (just check for the first one)
                                    if (numTerrains > 0)
                                    {
                                        // Append "_BorderOnly" to name of texture file if RemoveCentre is enabled
                                        mapFilePath = folderMapPath + "/" + mapPathScript.lbPath.pathName + "_0000" + (mapPathScript.lbPath.removeCentre ? "_BorderOnly" : "") + ".png";

                                        // Check if there is an existing texture with the same name
                                        string mapFullPath = Application.dataPath + mapFilePath.Substring(6);
                                        EditorUtility.ClearProgressBar();
                                        if (System.IO.File.Exists(mapFullPath))
                                        {

                                            continueToSave = EditorUtility.DisplayDialog("Maps Already Exist", "Are you sure you want to save the Maps? The existing" +
                                                                    " Maps will be lost.", "Overwrite", "Cancel");
                                        }
                                        else { continueToSave = true; }
                                    }

                                    if (continueToSave)
                                    {
                                        Texture2D terrainTexture = null;
                                        int numTerrainsWide = (int)Mathf.Sqrt(numTerrains);
                                        float minXCoord = 0f, minYCoord = 0f;
                                        Vector3 terrainPosition;

                                        for (int tIdx = 0; tIdx < numTerrains; tIdx++)
                                        {
                                            terrainPosition = landscape.landscapeTerrains[tIdx].transform.position;

                                            minXCoord = Mathf.InverseLerp(landscapeBounds.xMin, landscapeBounds.xMax, terrainPosition.x);
                                            minYCoord = Mathf.InverseLerp(landscapeBounds.yMin, landscapeBounds.yMax, terrainPosition.z);

                                            // Append "_BorderOnly" to name of texture file if RemoveCentre is enabled
                                            mapFilePath = folderMapPath + "/" + mapPathScript.lbPath.pathName + "_" + (tIdx).ToString("0000") + (mapPathScript.lbPath.removeCentre ? "_BorderOnly" : "") + ".png";

                                            terrainTexture = LBTextureOperations.GenerateTex2DFromPartOfTex2D(mapPathScript.mapTexture, numTerrainsWide, minXCoord, minYCoord);

                                            if (terrainTexture != null)
                                            {
                                                EditorUtility.DisplayProgressBar("Saving Map", "Please Wait", tIdx + 1f / numTerrains);
                                                // Save in RGBA32 format for Vegetation Studio
                                                LBEditorHelper.SaveMapTexture(terrainTexture, mapFilePath, terrainTexture.width, (tIdx == numTerrains - 1), true);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    // Default behaviour - create a single map texture for the whole landscape.

                                    // Append "_BorderOnly" to name of texture file if RemoveCentre is enabled
                                    mapFilePath = folderMapPath + "/" + mapPathScript.lbPath.pathName + (mapPathScript.lbPath.removeCentre ? "_BorderOnly" : "") + ".png";

                                    // Check if there is an existing texture with the same name
                                    string mapFullPath = Application.dataPath + mapFilePath.Substring(6);
                                    EditorUtility.ClearProgressBar();
                                    if (System.IO.File.Exists(mapFullPath))
                                    {

                                        continueToSave = EditorUtility.DisplayDialog("Map Already Exists", "Are you sure you want to save the Map? The existing" +
                                                                " Map will be lost.", "Overwrite", "Cancel");
                                    }
                                    else { continueToSave = true; }

                                    if (continueToSave)
                                    {
                                        EditorUtility.DisplayProgressBar("Saving Map", "Please Wait", 0.8f);
                                        LBEditorHelper.SaveMapTexture(mapPathScript.mapTexture, mapFilePath, LBMap.MapResolution(mapPathScript.mapResolution), true, false);
                                    }
                                }
                            }

                            EditorUtility.ClearProgressBar();
                        }
                    }
                }
                #endregion

                #region Duplicate MapPath
                if (numPointsInList > 1)
                {
                    if (GUILayout.Button("Duplicate Path", GUILayout.MaxWidth(115f)))
                    {
                        GameObject landscapeGameObject = mapPathScript.transform.parent.gameObject;
                        if (landscapeGameObject == null)
                        {
                            Debug.LogWarning("LBMapPath - the Path seems to no longer be a child object of the Landscape");
                        }
                        else
                        {
                            LBLandscape landscape = landscapeGameObject.GetComponent<LBLandscape>();
                            if (landscape == null) { Debug.LogWarning("LBMapPath - the Path seems to no longer be a child object of the Landscape"); }
                            {
                                LBMapPath duplbMapPath = LBMapPath.CreateMapPathDuplicate(landscape, landscapeGameObject, mapPathScript);
                                if (duplbMapPath != null)
                                {
                                    // Hide current path, and show duplicated path in scene view
                                    mapPathScript.lbPath.showPathInScene = false;
                                    duplbMapPath.lbPath.showPathInScene = true;

                                    // Select the new path and highlight in Hierarchy
                                    Selection.activeGameObject = duplbMapPath.gameObject;
                                    EditorGUIUtility.PingObject(duplbMapPath.gameObject);
                                    LBEditorHelper.ShowSceneView(this.GetType());
                                }
                            }
                        }
                    }
                }
                #endregion

                EditorGUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
            #endregion

            #region EasyRoads Integration
            if (mapPathScript.showTabInEditor == (int)LBMapPath.LBMapPathTab.ER)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                if (mapPathScript.isEasyRoadsInstalled) { easyRoadsInstalledText = "Installed"; }
                else { easyRoadsInstalledText = ""; }
                EditorGUI.BeginChangeCheck();
                GUILayout.BeginHorizontal();
                mapPathScript.useEasyRoads = EditorGUILayout.Toggle(new GUIContent("Use EasyRoads", "Use EasyRoads3D to get the path points"), mapPathScript.useEasyRoads);
                EditorGUILayout.LabelField(easyRoadsInstalledText);
                GUILayout.EndHorizontal();
                if (EditorGUI.EndChangeCheck())
                {
                    isSceneSaveRequired = true;
                    if (mapPathScript.useEasyRoads)
                    {
                        mapPathScript.isEasyRoadsInstalled = LBIntegration.isEasyRoads3DInstalled();
                    }
                }

                // Display EasyRoads options
                if (mapPathScript.useEasyRoads && mapPathScript.isEasyRoadsInstalled)
                {
                    // Display the list of ERRoads (create empty list if it doesn't already exist)
                    if (mapPathScript.roadList == null) { mapPathScript.roadList = new List<LBRoad>(); isSceneSaveRequired = true; }

                    if (mapPathScript.roadList != null)
                    {
                        // Loop through the list of roads
                        for (i = 0; i < mapPathScript.roadList.Count; i++)
                        {
                            lbRoad = mapPathScript.roadList[i];

                            if (lbRoad != null)
                            {
                                // Display the selectable road
                                GUILayout.BeginHorizontal();
                                if (GUILayout.Button(new GUIContent("v", "Move road down in the list"), buttonCompact, GUILayout.MaxWidth(20f)))
                                {
                                    lbRoadMoveDown = new LBRoad(lbRoad);
                                    lbRoadMovePos = i;
                                }
                                lbRoad.isSelected = EditorGUILayout.Toggle(lbRoad.isSelected, GUILayout.Width(20f));
                                GUILayout.Label(new GUIContent("R", "Reverse direction of path for this road"), GUILayout.Width(12f));
                                lbRoad.isReversed = EditorGUILayout.Toggle(lbRoad.isReversed, GUILayout.Width(20f));
                                GUILayout.Label(lbRoad.roadName);
                                GUILayout.EndHorizontal();
                            }
                        }
                    }

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Get Roads", GUILayout.MaxWidth(80f)))
                    {
                        // Save the original list
                        if (tempRoadList == null) { tempRoadList = new List<LBRoad>(); }
                        if (tempRoadList != null)
                        {
                            tempRoadList.Clear();
                            tempRoadList.AddRange(mapPathScript.roadList);
                        }

                        // Retrieve the current list of roads
                        mapPathScript.roadList = LBIntegration.GetERRoadList();

                        // Compare the two list and re-select previously selected roads
                        if (mapPathScript.roadList != null && tempRoadList != null)
                        {
                            for (i = 0; i < mapPathScript.roadList.Count; i++)
                            {
                                lbCompareRoad = tempRoadList.Find(r => r.roadName == mapPathScript.roadList[i].roadName);
                                if (lbCompareRoad != null)
                                {
                                    // Retain user setting for road of the same name
                                    mapPathScript.roadList[i].isSelected = lbCompareRoad.isSelected;
                                    mapPathScript.roadList[i].isReversed = lbCompareRoad.isReversed;
                                }
                            }
                        }
                        isSceneSaveRequired = true;
                    }
                    if (GUILayout.Button("Update Path", GUILayout.MaxWidth(90f)))
                    {
                        // Verify all roads are available

                        isContinueToAddPoints = true;
                        // Remove old path points
                        if (mapPathScript.lbPath.positionList.Count > 0)
                        {
                            isContinueToAddPoints = EditorUtility.DisplayDialog("Overwrite existing path points?", "This action will clear all current path points", "Overwrite", "Cancel");
                        }

                        // Add new path points
                        if (isContinueToAddPoints && mapPathScript.roadList != null)
                        {
                            splinePointFilterSize = 10;
                            mapPathScript.lbPath.CreatePathFromRoad(mapPathScript.roadList, splinePointFilterSize);
                            mapPathScript.minPathWidth = mapPathScript.lbPath.GetMinWidth();
                            isSceneSaveRequired = true;
                        }
                    }
                    GUILayout.EndHorizontal();

                    if (lbRoadMoveDown != null)
                    {
                        // Attempt to move this road down one in the list
                        if (mapPathScript.roadList.Count > 1)
                        {
                            // If this is the last in the list we want to put it at the top
                            if (lbRoadMovePos == mapPathScript.roadList.Count - 1)
                            {
                                mapPathScript.roadList.Insert(0, lbRoadMoveDown);
                                mapPathScript.roadList.RemoveAt(mapPathScript.roadList.Count - 1);
                            }
                            else
                            {
                                // Move down one in the list
                                mapPathScript.roadList.RemoveAt(lbRoadMovePos);
                                mapPathScript.roadList.Insert(lbRoadMovePos + 1, lbRoadMoveDown);
                            }
                            isSceneSaveRequired = true;
                        }
                        lbRoadMoveDown = null;
                    }
                }
                EditorGUILayout.EndVertical();
            }
            // End EasyRoads Integration
            #endregion

            #region Mesh
            //EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            //EditorGUILayout.BeginHorizontal();
            //EditorGUILayout.LabelField("Create Mesh", EditorStyles.boldLabel);
            //if (mapPathScript.showMesh) { if (GUILayout.Button("Hide", GUILayout.Width(50f))) { mapPathScript.showMesh = false; isSceneSaveRequired = true; } }
            //else { if (GUILayout.Button("Show", GUILayout.Width(50f))) { mapPathScript.showMesh = true; isSceneSaveRequired = true; } }
            //EditorGUILayout.EndHorizontal();
            //if (mapPathScript.showMesh)
            if (mapPathScript.showTabInEditor == (int)LBMapPath.LBMapPathTab.Mesh)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                if (numPointsInList > 1)
                {
                    EditorGUI.BeginChangeCheck();
                    mapPathScript.lbPath.isMeshLandscapeUV = EditorGUILayout.Toggle(meshLandscapeUVContent, mapPathScript.lbPath.isMeshLandscapeUV);
                    mapPathScript.lbPath.meshUVTileScale = (Vector2)EditorGUILayout.Vector2Field(meshUVTilingContent, mapPathScript.lbPath.meshUVTileScale);
                    mapPathScript.lbPath.meshYOffset = EditorGUILayout.Slider(meshYOffsetContent, mapPathScript.lbPath.meshYOffset, -100f, 100f);
                    mapPathScript.lbPath.meshEdgeSnapToTerrain = EditorGUILayout.Toggle(meshEdgeSnapToTerrain, mapPathScript.lbPath.meshEdgeSnapToTerrain);
                    if (mapPathScript.lbPath.meshEdgeSnapToTerrain)
                    {
                        mapPathScript.lbPath.meshSnapType = (LBPath.MeshSnapType)EditorGUILayout.EnumPopup(meshSnapTypeContent, mapPathScript.lbPath.meshSnapType);
                    }
                    mapPathScript.lbPath.meshIsDoubleSided = EditorGUILayout.Toggle(meshIsDoubleSidedContent, mapPathScript.lbPath.meshIsDoubleSided);
                    mapPathScript.lbPath.meshIncludeEdges = EditorGUILayout.Toggle(meshIncludeEdgesContent, mapPathScript.lbPath.meshIncludeEdges);
                    if (EditorGUI.EndChangeCheck()) { isSceneSaveRequired = true; }

                    #region Include Water

                    EditorGUI.BeginChangeCheck();
                    mapPathScript.lbPath.meshIncludeWater = EditorGUILayout.Toggle(meshIncludeWaterContent, mapPathScript.lbPath.meshIncludeWater);
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (mapPathScript.lbPath.meshIncludeWater)
                        {
                            // This will work with AQUAS 1.3 and AQUAS River, and Calm Water materials
                            mapPathScript.lbPath.meshUVTileScale = Vector2.one;
                            mapPathScript.lbPath.isMeshLandscapeUV = false;

                            //LBLandscape lbLandscape = mapPathScript.GetLandscape();
                            //if (lbLandscape != null)
                            //{
                            //    // AQUAS River and StandardAsset water should default UVs to size of landscape
                            //    mapPathScript.lbPath.meshUVTileScale = lbLandscape.size;
                            //}
                        }
                        isSceneSaveRequired = true;
                    }

                    if (mapPathScript.lbPath.meshIncludeWater)
                    {
                        EditorGUI.BeginChangeCheck();
                        if (mapPathScript.lbPath.lbWater == null) { mapPathScript.lbPath.lbWater = new LBWater(); }

                        LBWater.WaterMeshResizingMode previousWaterMeshResizingMode = mapPathScript.lbPath.lbWater.meshResizingMode;

                        mapPathScript.lbPath.lbWater.meshResizingMode = (LBWater.WaterMeshResizingMode)EditorGUILayout.EnumPopup(meshWaterResizingModeContent, mapPathScript.lbPath.lbWater.meshResizingMode);

                        if (mapPathScript.lbPath.lbWater.meshResizingMode == LBWater.WaterMeshResizingMode.AQUAS)
                        {
                            mapPathScript.waterMainCamera = (Camera)EditorGUILayout.ObjectField(meshWaterMainCameraContent, mapPathScript.waterMainCamera, typeof(Camera), true);
                            if (mapPathScript.waterMainCamera == null) { mapPathScript.waterMainCamera = Camera.main; }

                            // Preload the AQUAS river material
                            if (mapPathScript.meshMaterial == null)
                            {
                                mapPathScript.meshMaterial = (Material)AssetDatabase.LoadAssetAtPath("Assets/AQUAS/Materials/Water/Desktop&Web/River.mat", typeof(Material));
                            }
                        }
                        if (EditorGUI.EndChangeCheck())
                        {
                            // If changing Resizing mode, remove the previous mesh material
                            if (previousWaterMeshResizingMode != mapPathScript.lbPath.lbWater.meshResizingMode)
                            {
                                mapPathScript.meshMaterial = null;

                                if (mapPathScript.lbPath.lbWater.meshResizingMode == LBWater.WaterMeshResizingMode.StandardAssets)
                                {
                                    // Show user where they can find the water assets
                                    LBEditorHelper.HighlightItemInProjectWindow("Assets/LandscapeBuilder/Standard Assets/Environment/Water", false);
                                }
                                else if (mapPathScript.lbPath.lbWater.meshResizingMode == LBWater.WaterMeshResizingMode.CalmWater)
                                {
                                    // Defaults
                                    mapPathScript.lbPath.meshUVTileScale = Vector2.one;
                                    mapPathScript.lbPath.isMeshLandscapeUV = false;

                                    // Show user where they can find the water materials
                                    LBEditorHelper.HighlightItemInProjectWindow("Assets/Calm Water/Demo/Materials", false);
                                }
                                else if (mapPathScript.lbPath.lbWater.meshResizingMode == LBWater.WaterMeshResizingMode.RiverAutoMaterial)
                                {
                                    // Defaults
                                    mapPathScript.lbPath.meshUVTileScale = Vector2.one * 0.5f;
                                    mapPathScript.lbPath.isMeshLandscapeUV = false;

                                    if (LBIntegration.IsRiverAutoMaterialInstalled(true))
                                    {
                                        // Show user where they can find the water materials
                                        LBEditorHelper.HighlightItemInProjectWindow("Assets/River Auto Material/River/Materials", false);
                                    }
                                }
                            }

                            isSceneSaveRequired = true;
                        }
                    }

                    #endregion

                    EditorGUI.BeginChangeCheck();
                    mapPathScript.meshMaterial = (Material)EditorGUILayout.ObjectField(meshMaterialContent, mapPathScript.meshMaterial, typeof(Material), true);
                    mapPathScript.lbPath.meshSaveToProjectFolder = EditorGUILayout.Toggle(meshSaveToProjectFolderContent, mapPathScript.lbPath.meshSaveToProjectFolder);
                    if (EditorGUI.EndChangeCheck()) { isSceneSaveRequired = true; }

                    #region Mesh Buttons

                    GUILayout.BeginHorizontal();

                    #region Create Mesh
                    if (GUILayout.Button("Create Mesh", GUILayout.MaxWidth(100f)))
                    {
                        // Get the LBLandscape parent script
                        GameObject landscapeGameObject = mapPathScript.transform.parent.gameObject;
                        if (landscapeGameObject == null)
                        {
                            Debug.LogWarning("LBMapPath - the Path seems to no longer be a child object of the Landscape");
                        }
                        else
                        {
                            LBLandscape landscape = landscapeGameObject.GetComponent<LBLandscape>();
                            if (landscape == null) { Debug.LogWarning("LBMapPath - the Path seems to no longer be a child object of the Landscape"); }
                            else
                            {
                                // Ensure the name of the path matches the gameobject before creating the mesh.
                                if (mapPathScript.IsPathNameChanged()) { isSceneSaveRequired = true; }

                                if (mapPathScript.lbPath.CreateMeshFromPath(landscape))
                                {
                                    Vector3 meshPosition = new Vector3(0f, mapPathScript.lbPath.meshYOffset, 0f);

                                    Transform meshTransform = LBMeshOperations.AddMeshToScene(mapPathScript.lbPath.lbMesh, meshPosition, mapPathScript.lbPath.pathName + " Mesh", mapPathScript.transform, mapPathScript.meshMaterial, true, true);

                                    if (meshTransform != null)
                                    {
                                        if (mapPathScript.lbPath.meshIncludeWater)
                                        {
                                            // Populate the paramaters to pass to AddWaterToMesh()
                                            LBWaterParameters lbWaterParms = new LBWaterParameters();
                                            lbWaterParms.landscape = landscape;
                                            lbWaterParms.landscapeGameObject = landscapeGameObject;
                                            lbWaterParms.waterTransform = meshTransform;
                                            lbWaterParms.waterPosition = meshPosition;
                                            //lbWaterParms.waterSize = waterSize;
                                            lbWaterParms.waterIsPrimary = false;
                                            lbWaterParms.waterHeight = 0f;
                                            lbWaterParms.waterPrefab = null;
                                            lbWaterParms.keepPrefabAspectRatio = true;
                                            lbWaterParms.waterMeshResizingMode = mapPathScript.lbPath.lbWater.meshResizingMode;
                                            lbWaterParms.waterMainCamera = mapPathScript.waterMainCamera;
                                            //lbWaterParms.waterCausticsPrefabList = waterCausticsPrefabList;
                                            lbWaterParms.isRiver = true;
                                            lbWaterParms.riverMaterial = mapPathScript.meshMaterial;
                                            lbWaterParms.lbLighting = GameObject.FindObjectOfType<LBLighting>();

                                            // Some water assets (like AQUAS) require a reflection probe
                                            // Place it at the second point in the path
                                            lbWaterParms.reflectionProbePosition = mapPathScript.lbPath.positionList[1];

                                            // We already have an LBWater attached to LBPath. Need to determine if we
                                            // should update the path with the returned LBWater class instance
                                            LBWater addedWater = LBWaterOperations.AddWaterToMesh(lbWaterParms);
                                            if (addedWater != null)
                                            {

                                            }
                                        }

                                        if (mapPathScript.lbPath.lbMesh.mesh != null && mapPathScript.lbPath.meshSaveToProjectFolder)
                                        {
                                            // Save Mesh to Project
                                            // Create Meshes folder if they don't already exist
                                            LBEditorHelper.CheckFolder("Assets/LandscapeBuilder/Meshes");

                                            string _filePath = "Assets/LandscapeBuilder/Meshes/" + mapPathScript.lbPath.lbMesh.mesh.name + ".asset";
                                            bool _continueToSave = true;

                                            if (System.IO.File.Exists(_filePath))
                                            {
                                                _continueToSave = EditorUtility.DisplayDialog("Mesh Already Exists", "Are you sure you want to save this mesh? The currently existing" +
                                                                        " mesh will be lost.", "Overwrite", "Cancel");
                                            }
                                            else { _continueToSave = true; }

                                            if (_continueToSave) { AssetDatabase.CreateAsset(mapPathScript.lbPath.lbMesh.mesh, _filePath); }
                                        }
                                        isSceneSaveRequired = true;
                                    }
                                    else
                                    {
                                        Debug.LogWarning("ERROR: LBMapPath - could not add mesh to scene for " + mapPathScript.lbPath.pathName);
                                    }
                                }
                            }
                        }
                    }
                    #endregion

                    #region Delete Mesh
                    if (GUILayout.Button("Delete Mesh", GUILayout.MaxWidth(100f)))
                    {
                        LBMeshOperations.RemoveMeshFromScene(mapPathScript.lbPath.pathName + " Mesh", mapPathScript.transform);
                    }
                    #endregion

                    #region Bake Navmesh

                    if (UnityEditor.AI.NavMeshBuilder.isRunning)
                    {
                        if (GUILayout.Button(cancelBakeButtonContent, GUILayout.MaxWidth(90f)))
                        {
                            UnityEditor.AI.NavMeshBuilder.Cancel();
                        }
                    }
                    else if(GUILayout.Button(bakeNavMeshButtonContent, GUILayout.MaxWidth(90f)))
                    {
                        UnityEditor.AI.NavMeshBuilder.BuildNavMeshAsync();
                    }

                    #endregion

                    #region Navmesh Settings
                    if (GUILayout.Button(openNavigationButtonContent, GUILayout.MaxWidth(125f)))
                    {
                        #if UNITY_2018_2_OR_NEWER
                        LBEditorHelper.CallMenu("Window/AI/Navigation");
                        #else
                        LBEditorHelper.CallMenu("Window/Navigation");
                        #endif
                    }
                    #endregion

                    GUILayout.EndHorizontal();
                    #endregion
                }
                GUILayout.EndVertical();
            }
            //GUILayout.EndVertical();
            #endregion

            #region ShowPathPoints

            EditorGUI.BeginChangeCheck();
            if (mapPathScript.showTabInEditor == (int)LBMapPath.LBMapPathTab.Points)
            {
                labelText = "Press '+' when mouse pointer is in the scene to append a new point to the path. Right-click for Context Menu. ";
                labelText += "Click F(ind) to select a path point in the scene view. Hold the ctrl key down to move all the points at the same time. ";
                labelText += "Select the scale tool from the Unity toolbar (R) to change the width of the current point.";
                EditorGUILayout.HelpBox(labelText, MessageType.Info);

                #region Width Buttons
                GUILayout.BeginHorizontal();

                #region Set Path Width
                if (numPointsInList > 0)
                {
                    if (isGetWidth)
                    {
                        GUILayoutOption[] guiLayoutPathWidthOptions = { GUILayout.MaxWidth(145f), GUILayout.MaxHeight(19f) };
                        pathWidth = EditorGUILayout.FloatField(pathWidth, guiLayoutPathWidthOptions);
                        // Check if user wants to change all path widths
                        KeyCode keyPressed = Event.current.keyCode;
                        if (keyPressed == KeyCode.Escape) { isGetWidth = false; }
                        else if (keyPressed == KeyCode.KeypadEnter || keyPressed == KeyCode.Return)
                        {
                            mapPathScript.lbPath.SetPathWidths(pathWidth);
                            mapPathScript.minPathWidth = pathWidth;
                            isSceneSaveRequired = true;
                            mapPathScript.lbPath.RefreshPathEdgePositions();
                            isGetWidth = false;
                        }
                    }
                    else if (GUILayout.Button("Set Width (All Points)", GUILayout.MaxWidth(145f)))
                    {
                        pathWidth = mapPathScript.minPathWidth;
                        isGetWidth = true;
                    }
                }
                #endregion

                #region Add Width to path
                if (numPointsInList > 0)
                {
                    if (isAddWidth)
                    {
                        GUILayoutOption[] guiLayoutPathWidthOptions = { GUILayout.MaxWidth(145f), GUILayout.MaxHeight(19f) };
                        addWidth = EditorGUILayout.FloatField(addWidth, guiLayoutPathWidthOptions);
                        // Check if user wants to change all path widths by x amount
                        KeyCode keyPressed = Event.current.keyCode;
                        if (keyPressed == KeyCode.Escape) { isAddWidth = false; }
                        else if (keyPressed == KeyCode.KeypadEnter || keyPressed == KeyCode.Return)
                        {
                            mapPathScript.lbPath.AddPathWidths(addWidth, 2f, 1000f);
                            mapPathScript.minPathWidth = mapPathScript.lbPath.GetMinWidth();
                            isSceneSaveRequired = true;
                            mapPathScript.lbPath.RefreshPathEdgePositions();
                            isAddWidth = false;
                        }
                    }
                    else if (GUILayout.Button("Add Width (All Points)", GUILayout.MaxWidth(145f)))
                    {
                        addWidth = 0f;
                        isAddWidth = true;
                    }
                }
                #endregion

                GUILayout.EndHorizontal();
                #endregion

                #region Display add and remove buttons
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Path Points: " + mapPathScript.lbPath.positionList.Count.ToString("00"));
                if (GUILayout.Button("+", GUILayout.MaxWidth(30f)))
                {
                    AppendPathPoint(mapPathScript, Vector3.zero);

                    isSceneSaveRequired = true;
                }

                // Remove the last point in the path
                if (GUILayout.Button("-", GUILayout.MaxWidth(30f)))
                {
                    int itemToRemove = mapPathScript.lbPath.positionList.Count - 1;

                    if (mapPathScript.lbPath.positionList.Count > 0)
                    {
                        // It might be in the selected list, so remove that first
                        mapPathScript.lbPath.selectedList.Remove(itemToRemove);

                        // Remove the point from position and width lists.
                        mapPathScript.lbPath.positionList.RemoveAt(itemToRemove);
                        mapPathScript.lbPath.positionListLeftEdge.RemoveAt(itemToRemove);
                        mapPathScript.lbPath.positionListRightEdge.RemoveAt(itemToRemove);
                        mapPathScript.lbPath.widthList.RemoveAt(itemToRemove);

                        // Update the locally cached minimum path width
                        mapPathScript.minPathWidth = mapPathScript.lbPath.GetMinWidth();

                        isSceneSaveRequired = true;
                    }
                }
                GUILayout.EndHorizontal();
                #endregion

                // Reset find/insert/delete point positions
                insertPointPos = -1;
                findPointPos = -1;
                deletePointPos = -1;

                #region List all points
                EditorGUI.BeginChangeCheck();
                for (int i = 0; i < mapPathScript.lbPath.positionList.Count; i++)
                {
                    // Display each position in the list
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("F", buttonCompact, GUILayout.MaxWidth(20f))) { findPointPos = i; }
                    if (GUILayout.Button("I", buttonCompact, GUILayout.MaxWidth(20f))) { insertPointPos = i; }
                    isSelected = mapPathScript.lbPath.selectedList.Exists(pt => pt == i);
                    labelText = "<color=" + txtColourName + ">" + (isSelected ? "<b>" : "") + "Pt " + (i + 1).ToString("000") + (isSelected ? "</b>" : "") + "</color>";
                    EditorGUILayout.LabelField(labelText, labelFieldRichText, GUILayout.MaxWidth(48f));
                    mapPathScript.lbPath.positionList[i] = EditorGUILayout.Vector3Field("", mapPathScript.lbPath.positionList[i]);
                    if (GUILayout.Button("X", buttonCompact, GUILayout.MaxWidth(20f))) { deletePointPos = i; }
                    GUILayout.EndHorizontal();
                }
                if (EditorGUI.EndChangeCheck()) { isSceneSaveRequired = true; }
                #endregion

                #region Find Position in Scene
                if (findPointPos >= 0)
                {
                    if (mapPathScript.lbPath.zoomOnFind)
                    {
                        // Move the scene view camera to the selected point
                        LBEditorHelper.PositionSceneView(mapPathScript.lbPath.positionList[findPointPos], mapPathScript.lbPath.findZoomDistance, this.GetType());
                    }
                    mapPathScript.lbPath.selectedList.Clear();
                    mapPathScript.lbPath.selectedList.Add(findPointPos);
                }
                #endregion

                #region Insert or Delete Positions
                if (insertPointPos >= 0)
                {
                    // Insert a duplicate of the selected point (position and width)
                    mapPathScript.lbPath.positionList.Insert(insertPointPos, mapPathScript.lbPath.positionList[insertPointPos]);
                    mapPathScript.lbPath.widthList.Insert(insertPointPos, mapPathScript.lbPath.widthList[insertPointPos]);
                    mapPathScript.lbPath.positionListLeftEdge.Insert(insertPointPos, mapPathScript.lbPath.positionListLeftEdge[insertPointPos]);
                    mapPathScript.lbPath.positionListRightEdge.Insert(insertPointPos, mapPathScript.lbPath.positionListRightEdge[insertPointPos]);
                    LBEditorHelper.PositionSceneView(mapPathScript.lbPath.positionList[insertPointPos], mapPathScript.lbPath.findZoomDistance, this.GetType());
                    mapPathScript.lbPath.selectedList.Clear();
                    mapPathScript.lbPath.selectedList.Add(insertPointPos);
                    // Clear existing point distances so that edge Gizmos get refreshed in scene view.
                    if (mapPathScript.lbPath.showPathInScene) { mapPathScript.lbPath.ClearCachePathPointDistances(); }
                    // Update the locally cached minimum path width
                    mapPathScript.minPathWidth = mapPathScript.lbPath.GetMinWidth();
                    mapPathScript.lbPath.RefreshPathEdgePositions();
                    isSceneSaveRequired = true;
                }
                // Don't permit deletes in the same frame as an insert
                else if (deletePointPos >= 0)
                {
                    // It might be in the selected list, so remove that first
                    mapPathScript.lbPath.selectedList.Remove(deletePointPos);

                    mapPathScript.lbPath.positionList.RemoveAt(deletePointPos);
                    mapPathScript.lbPath.positionListLeftEdge.RemoveAt(deletePointPos);
                    mapPathScript.lbPath.positionListRightEdge.RemoveAt(deletePointPos);
                    mapPathScript.lbPath.widthList.RemoveAt(deletePointPos);
                    // Clear existing point distances so that edge Gizmos get refreshed in scene view.
                    if (mapPathScript.lbPath.showPathInScene) { mapPathScript.lbPath.ClearCachePathPointDistances(); }
                    // Update the locally cached minimum path width
                    mapPathScript.minPathWidth = mapPathScript.lbPath.GetMinWidth();
                    mapPathScript.lbPath.RefreshPathEdgePositions();
                    isSceneSaveRequired = true;
                }
                #endregion
            }

            #endregion

            #region Save and Repaint
            // Some of the controls have changed, so mark the scene as changed so that
            // if it is run without the LBMapPath editor script open, the values will persist.
            if (isSceneSaveRequired && !Application.isPlaying)
            {
                isSceneSaveRequired = false;
                // Can't save scene here as the overhead is too high for sliders
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

                // Force OnDrawGizmosSelected() to be called
                SceneView.RepaintAll();
            }
            #endregion
        }

        #region Append or Insert Path Point Methods

        /// <summary>
        /// Add a new point to the path at the current mouse position
        /// </summary>
        /// <param name="evt"></param>
        private void AddPointAtMousePosition(Event evt)
        {
            Vector3 pos = Vector3.zero;
            LBMapPath lbMapPath = (LBMapPath)target;

            if (lbMapPath != null && lbMapPath.lbPath != null)
            {
                LBLandscape landscape = lbMapPath.landscape;

                // Check to see if the landscape is already cached.
                if (landscape == null) { landscape = lbMapPath.GetComponentInParent<LBLandscape>(); }

                if (landscape != null)
                {
                    // Get the on-ground terrain position from the mouse (this is in WorldSpace)
                    pos = LBEditorHelper.GetLandscapePositionFromMouse(landscape, evt.mousePosition, false, true);

                    // If x and z is zero, it probably means the mouse is outside the bounds of the landscape or designer.
                    if (pos.x != 0 || pos.z != 0) { pos.y += lbMapPath.lbPath.heightAboveTerrain; }

                    evt.Use();
                    AppendPathPoint(lbMapPath, pos);
                }
            }
        }

        /// <summary>
        /// Add a point to the end of the current path
        /// </summary>
        /// <param name="mapPathScript"></param>
        private void AppendPathPoint(LBMapPath mapPathScript, Vector3 pointToAdd)
        {
            bool pointToAddZero = (pointToAdd.x == 0f && pointToAdd.y == 0f && pointToAdd.z == 0f);

            if (!pointToAddZero || mapPathScript.lbPath.positionList.Count > 0)
            {
                int currentLastItem = mapPathScript.lbPath.positionList.Count - 1;
                bool duplicateLastItem = pointToAddZero;

                // When another position is added, by default, make it a duplicate of the previous one
                if (duplicateLastItem)
                {
                    mapPathScript.lbPath.positionList.Add(mapPathScript.lbPath.positionList[currentLastItem]);
                    mapPathScript.lbPath.widthList.Add(mapPathScript.lbPath.widthList[currentLastItem]);
                    mapPathScript.lbPath.positionListLeftEdge.Add(mapPathScript.lbPath.positionListLeftEdge[currentLastItem]);
                    mapPathScript.lbPath.positionListRightEdge.Add(mapPathScript.lbPath.positionListRightEdge[currentLastItem]);
                }
                else
                {
                    // If a location was indicated on the landscape, add that one. This typically occurs when the
                    // user clicks the '+' key when the mouse is over the scene view or using the Context Menu in the scene view
                    mapPathScript.lbPath.positionList.Add(pointToAdd);

                    // Add the width. If this is the first point, use the default
                    if (currentLastItem < 0) { mapPathScript.lbPath.widthList.Add(LBPath.GetDefaultPathWidth); }
                    else { mapPathScript.lbPath.widthList.Add(mapPathScript.lbPath.widthList[currentLastItem]); }

                    // Add left/right edges are same location. They will be correctly positioned in RefreshPathEdgePositions().
                    mapPathScript.lbPath.positionListLeftEdge.Add(pointToAdd);
                    mapPathScript.lbPath.positionListRightEdge.Add(pointToAdd);

                    mapPathScript.lbPath.RefreshPathEdgePositions();
                }

                // Clear the selection list and add this new path point
                mapPathScript.lbPath.selectedList.Clear();
                mapPathScript.lbPath.selectedList.Add(currentLastItem + 1);
                if (mapPathScript.lbPath.zoomOnFind)
                {
                    LBEditorHelper.PositionSceneView(mapPathScript.lbPath.positionList[currentLastItem + 1], mapPathScript.lbPath.findZoomDistance, this.GetType());
                }
            }
            else
            {
                Vector3 firstPointCentre = Vector3.zero;
                Vector3 firstPointLeft = Vector3.zero;
                Vector3 firstPointRight = Vector3.zero;

                if (mapPathScript.landscape == null) { Debug.LogWarning("LBMapPath - cannot find landscape"); }
                else
                {
                    // If possible, place the first point on the landscape in the centre of the sceneview
                    firstPointCentre = LBEditorHelper.GetCentreSceneView(this.GetType());

                    Rect worldBounds = LBLandscapeTerrain.GetLandscapeWorldBounds(mapPathScript.landscape.GetComponentsInChildren<Terrain>());

                    Vector2 firstPointXZ = new Vector2(firstPointCentre.x, firstPointCentre.z);

                    // Is the centre of the screen inside the landscape? If not, set to near bottom left corner of landscape
                    if (!worldBounds.Contains(firstPointXZ))
                    {
                        firstPointCentre.x = worldBounds.xMin + 20f;
                        firstPointCentre.z = worldBounds.yMin + 20f;

                        firstPointXZ.x = firstPointCentre.x;
                        firstPointXZ.y = firstPointCentre.z;
                    }

                    firstPointCentre.y = LBLandscapeTerrain.GetHeight(mapPathScript.landscape, firstPointXZ, false) + mapPathScript.lbPath.heightAboveTerrain + mapPathScript.landscape.start.y;
                }

                mapPathScript.lbPath.positionList.Add(firstPointCentre);
                mapPathScript.lbPath.widthList.Add(LBPath.GetDefaultPathWidth);

                firstPointLeft = (Vector3.forward * (LBPath.GetDefaultPathWidth / 2f)) + firstPointCentre;
                firstPointRight = (Vector3.back * (LBPath.GetDefaultPathWidth / 2f)) + firstPointCentre;

                mapPathScript.lbPath.positionListLeftEdge.Add(firstPointLeft);
                mapPathScript.lbPath.positionListRightEdge.Add(firstPointRight);

                // Clear the selection list and add this new path point
                mapPathScript.lbPath.selectedList.Clear();
                mapPathScript.lbPath.selectedList.Add(0);
                LBEditorHelper.PositionSceneView(mapPathScript.lbPath.positionList[0], mapPathScript.lbPath.findZoomDistance, this.GetType());
            }

            // Update the locally cached minimum path width
            mapPathScript.minPathWidth = mapPathScript.lbPath.GetMinWidth();
        }

        /// <summary>
        /// Insert a point at the first selected point in the path
        /// </summary>
        private void InsertPoint(bool isInsertAfter)
        {
            LBMapPath lbMapPath = (LBMapPath)target;

            if (lbMapPath != null && lbMapPath.lbPath != null && lbMapPath.lbPath.selectedList != null)
            {
                int numSelected = lbMapPath.lbPath.selectedList.Count;
                int numPathPoints = lbMapPath.lbPath.positionList == null ? 0 : lbMapPath.lbPath.positionList.Count;

                if (numSelected > 0 && numPathPoints > 1)
                {
                    // Get the first selected point
                    int insertPointPos = lbMapPath.lbPath.selectedList[0];

                    int newObjPathPointPos = isInsertAfter ? insertPointPos + 1 : insertPointPos;

                    lbMapPath.lbPath.positionList.Insert(newObjPathPointPos, lbMapPath.lbPath.positionList[insertPointPos]);
                    lbMapPath.lbPath.widthList.Insert(newObjPathPointPos, lbMapPath.lbPath.widthList[insertPointPos]);
                    lbMapPath.lbPath.positionListRightEdge.Insert(newObjPathPointPos, lbMapPath.lbPath.positionListRightEdge[insertPointPos]);
                    lbMapPath.lbPath.positionListLeftEdge.Insert(newObjPathPointPos, lbMapPath.lbPath.positionListLeftEdge[insertPointPos]);

                    lbMapPath.lbPath.selectedList.Clear();
                    lbMapPath.lbPath.selectedList.Add(newObjPathPointPos);

                    SceneView.RepaintAll();
                }
            }
        }

        #endregion

        #region OnSceneGUI
        private void OnSceneGUI()
        {
            LBMapPath mapPathScript = (LBMapPath)target;

            numPositionsSceneGUI = (mapPathScript.lbPath == null || mapPathScript.lbPath.positionList == null ? 0 : mapPathScript.lbPath.positionList.Count);

            // Only show the handles if the path list is visible in the editor
            //if (mapPathScript.lbPath.positionList != null && mapPathScript.showPathPointsList && mapPathScript.lbPath.showPathInScene)
            if (mapPathScript.lbPath.positionList != null && mapPathScript.lbPath.showPathInScene)
            {
                isCtrlKeyPressed = false;

                Event currentEvent = Event.current;
                bool isRightButton = (currentEvent.button == 1);

                // Did the user press "+" or "=" key to add a point to the end of the path? Plus is shift-Equals on most keyboards
                if (currentEvent.type == EventType.KeyUp && (currentEvent.keyCode == KeyCode.Equals || currentEvent.keyCode == KeyCode.Plus || currentEvent.keyCode == KeyCode.KeypadPlus))
                {
                    AddPointAtMousePosition(currentEvent);
                    isSceneSaveRequired = true;
                }
                #region Context-sensitive Menu
                if (currentEvent.type == EventType.MouseDown && isRightButton && Tools.current != Tool.View && Tools.current != Tool.None)
                {
                    GenericMenu menu = new GenericMenu();
                    menu.AddDisabledItem(new GUIContent((mapPathScript.lbPath.pathName == null ? "MapPath" : mapPathScript.lbPath.pathName))); // Header
                    menu.AddItem(new GUIContent("Add Point to End"), false, () => { AddPointAtMousePosition(currentEvent); isSceneSaveRequired = true; });
                    menu.AddItem(new GUIContent("Insert Point Before"), false, () => { InsertPoint(false); isSceneSaveRequired = true; });
                    menu.AddItem(new GUIContent("Insert Point After"), false, () => { InsertPoint(true); isSceneSaveRequired = true; });
                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Display/Size 1"), mapPathScript.lbPath.GetPointDisplayScaleAsInt() == 1, () =>
                    {
                        mapPathScript.lbPath.SetPointDisplayScale(1);
                    });
                    menu.AddItem(new GUIContent("Display/Size 2"), mapPathScript.lbPath.GetPointDisplayScaleAsInt() == 2, () =>
                    {
                        mapPathScript.lbPath.SetPointDisplayScale(2);
                    });
                    menu.AddItem(new GUIContent("Display/Size 3"), mapPathScript.lbPath.GetPointDisplayScaleAsInt() == 3, () =>
                    {
                        mapPathScript.lbPath.SetPointDisplayScale(3);
                    });
                    menu.AddItem(new GUIContent("Display/Size 4"), mapPathScript.lbPath.GetPointDisplayScaleAsInt() == 4, () =>
                    {
                        mapPathScript.lbPath.SetPointDisplayScale(4);
                    });
                    menu.AddItem(new GUIContent("Display/Size 5"), mapPathScript.lbPath.GetPointDisplayScaleAsInt() == 5, () =>
                    {
                        mapPathScript.lbPath.SetPointDisplayScale(5);
                    });
                    menu.AddItem(new GUIContent("Find on Zoom"), mapPathScript.lbPath.zoomOnFind, () => { mapPathScript.lbPath.zoomOnFind = !mapPathScript.lbPath.zoomOnFind; });
                    menu.AddItem(new GUIContent("Snap To Terrain"), mapPathScript.lbPath.snapToTerrain, () =>
                    {
                        mapPathScript.lbPath.snapToTerrain = !mapPathScript.lbPath.snapToTerrain;
                        if (mapPathScript.lbPath.snapToTerrain && mapPathScript.landscape != null)
                        {
                            mapPathScript.lbPath.heightAboveTerrain = (float)heightAboveTerrainInt;
                            mapPathScript.lbPath.RefreshPathHeights(mapPathScript.landscape);
                            isSceneSaveRequired = true;
                        }
                    });
                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Delete Selected Points"), false, () =>
                    {
                        mapPathScript.lbPath.DeleteSelectedPoints(true);
                        HandleUtility.Repaint();
                        isSceneSaveRequired = true;
                    });
                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Allow Scene View Rotation"), false, () => { Tools.current = Tool.View; });
                    menu.AddItem(new GUIContent("Unselect"), false, () => { Selection.activeObject = null; mapPathScript.lbPath.selectedList.Clear(); });
                    menu.AddItem(new GUIContent("Cancel"), false, () => { });
                    menu.ShowAsContext();
                    currentEvent.Use();
                }
                #endregion
                else
                {
                    isDistancesVisible = mapPathScript.lbPath.showDistancesInScene;

                    #region Initialise labels
                    // For list of styles see: https://gist.github.com/MadLittleMods/ea3e7076f0f59a702ecb
                    widthLabel = new GUIStyle("Box");
                    widthLabel.fontSize = 14;
                    widthLabel.border = new RectOffset(2, 2, 2, 2);
                    GUI.skin.box.normal.textColor = Color.white;
                    widthLabel.onFocused.textColor = UnityEngine.Color.white;
                    #endregion

                    #region Process Path Positions
                    for (int i = 0; i < numPositionsSceneGUI; i++)
                    {
                        // Read from the list only once
                        startPos = mapPathScript.lbPath.positionList[i];

                        // Only display the handles for selected path points
                        if (mapPathScript.lbPath.selectedList.Exists(pt => pt == i))
                        {
                            if (Tools.current == Tool.Move)
                            {
                                EditorGUI.BeginChangeCheck();

                                //// Read from the list only once
                                //startPos = mapPathScript.lbPath.positionList[i];

                                // Make a handle for each point in the path so that it can be dragged around in the scene view
                                mapPathScript.lbPath.positionList[i] = Handles.PositionHandle(startPos, Quaternion.identity);

                                // Only update the edge positions if a path point has changed
                                if (EditorGUI.EndChangeCheck())
                                {
                                    isCtrlKeyPressed = Event.current.control;

                                    // If the user has tried to move the handle outside the landscape, snap to nearest border
                                    mapPathScript.lbPath.ClampPositionToBounds(i, landscapeBounds, mapPathScript.landscape.transform.position.y, mapPathScript.landscapeHeight);

                                    // If snapping to the landscape, find the correct offset above the terrain height at this point in worldspace
                                    if (mapPathScript.lbPath.snapToTerrain)
                                    {
                                        posXZ.x = mapPathScript.lbPath.positionList[i].x;
                                        posXZ.y = mapPathScript.lbPath.positionList[i].z;
                                        snappedPos.x = mapPathScript.lbPath.positionList[i].x;
                                        snappedPos.z = mapPathScript.lbPath.positionList[i].z;

                                        // LBLandscapeTerrain.GetHeight() returns a normalised height
                                        snappedPos.y = LBLandscapeTerrain.GetHeight(mapPathScript.landscape, posXZ, false) + mapPathScript.lbPath.heightAboveTerrain + mapPathScript.landscape.start.y;
                                        mapPathScript.lbPath.positionList[i] = snappedPos;
                                    }

                                    if (isCtrlKeyPressed)
                                    {
                                        // Move all the other centre points in the same direction as the current point was moved
                                        mapPathScript.lbPath.MovePoints(startPos, mapPathScript.lbPath.positionList[i], i, true);
                                    }
                                    else
                                    {
                                        // Update the edge positions
                                        mapPathScript.lbPath.RefreshPathEdgePositions();
                                    }

                                    isSceneSaveRequired = true;
                                }

                                if (isDistancesVisible && mapPathScript.lbPath.cachedPathPointDistances != null)
                                {
                                    Handles.Label(mapPathScript.lbPath.positionList[i] + widthLabelOffset, "Distance: " + mapPathScript.lbPath.cachedPathPointDistances[i].ToString("0.00"), widthLabel);
                                }
                            }
                            else if (Tools.current == Tool.Scale)
                            {
                                EditorGUI.BeginChangeCheck();

                                forwardsDir = mapPathScript.lbPath.GetForwardsFast(i, 1f);

                                // Display a resizing tool to change the width of this point
                                mapPathScript.lbPath.widthList[i] = Mathf.Abs(Handles.ScaleSlider(mapPathScript.lbPath.widthList[i], startPos, Vector3.Cross(forwardsDir, Vector3.down).normalized, Quaternion.identity, 10f, 1f));

                                //Handles.Disc(Quaternion.Euler(0f,0f,0f), startPos, forwardsDir, HandleUtility.GetHandleSize(startPos), false, 0);

                                Handles.color = Color.blue;

                                // Display the width of the current point
                                Handles.Label(startPos + widthLabelOffset, "Width: " + mapPathScript.lbPath.widthList[i].ToString("0.00"), widthLabel);

                                // Only update edge positions if the width of this point has been changed
                                if (EditorGUI.EndChangeCheck())
                                {
                                    // Update the edge positions
                                    mapPathScript.lbPath.RefreshPathEdgePositions();

                                    // Update cached minimum width
                                    mapPathScript.minPathWidth = mapPathScript.lbPath.GetMinWidth();

                                    isSceneSaveRequired = true;
                                }
                            }
                        }
                        else
                        {
                            // Draw a selectable button for the non-selected points in the path
                            if (Handles.Button(startPos, Quaternion.identity, 0.01f, 0.75f, Handles.SphereHandleCap))
                            {
                                // Select this piont in the path
                                mapPathScript.lbPath.selectedList.Clear();
                                mapPathScript.lbPath.selectedList.Add(i);
                            }
                        }
                    }
                    #endregion
                }

                if (isSceneSaveRequired && !Application.isPlaying)
                {
                    isSceneSaveRequired = false;
                    // Can't save scene here as the overhead is too high
                    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                }
            }
        }
        #endregion

    }
#endif
    #endregion
}