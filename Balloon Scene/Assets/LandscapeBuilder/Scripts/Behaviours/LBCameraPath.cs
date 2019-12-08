//#define CAMERA_PATH_DEBUG_MODE

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace LandscapeBuilder
{
    [AddComponentMenu("Landscape Builder/Camera Path")]
    [ExecuteInEditMode]
    public class LBCameraPath : MonoBehaviour
    {
        #region Variables and Properties
        // A script that allows you to define a splined path for a camera to follow

        // The lbPath is a replacement for pathPoints.
        public LBPath lbPath;

        // This is the old way of storing the path points. It will be maintained for
        // backward compatibility only.
        public List<Vector3> pathPoints;
        public List<Vector3> pathPointsUndo;  // stores a list of previous points to roll back to previous list

        // Replaced with lbPath lengths
        public float pathLength; // Length of the path in metres
        public float pathLengthToLastPoint;

        // EasyRoads integration variables
        public bool useEasyRoads = false;
        public bool isEasyRoadsInstalled = false;
        public float heightAboveRoad = 2f;
        public LBRoad.SplineType splineType = LBRoad.SplineType.MarkerSpline;
        public List<LBRoad> roadList = null;

        // Cache the landscape
        [System.NonSerialized] public LBLandscape landscape;
        [System.NonSerialized] public float landscapeHeight;

        [System.NonSerialized] public bool showPathPointsList = false;

        // Temporary variables used for catmull-rom equation
        private Vector3 point1;
        private Vector3 point2;
        private Vector3 point3;
        private Vector3 point4;
        private float inverseLerpPos;

        #endregion

        #region Initialisation

        public void OnEnable()
        {
            if (lbPath == null) { lbPath = new LBPath(); }

            if (lbPath == null) { Debug.LogError("LBCameraPath.OnEnable - could not create LBPath object"); }
            else
            {
                // If the pathName is null, update it with the name of the parent GameObject name.
                if (string.IsNullOrEmpty(lbPath.pathName)) { if (gameObject != null) { lbPath.pathName = gameObject.name; } }

                // For backward compatibility upgrade pre-v1.3.2 paths
                if (pathPoints != null)
                {
                    int numPointsToBeUpgraded = pathPoints.Count;
                    if (numPointsToBeUpgraded > 0)
                    {
                        Debug.Log("LBCameraPath.OnEnable - upgrading path points...");
                        lbPath.positionList.Clear();
                        lbPath.positionList.AddRange(pathPoints);
                        pathPoints.Clear();
#if UNITY_EDITOR
                        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
#endif
                        Debug.Log("LBCameraPath.OnEnable - upgraded " + numPointsToBeUpgraded.ToString() + " path points ");
                    }
                }

                // Find the landscape in the scene - it should be attached to the parent gameobject
                if (landscape == null)
                {
                    if (transform.parent == null)
                    {
                        Debug.LogWarning("LBCameraPath.OnEnable - We recommend that you move the Camera Path and Camera Animation gameobjects under a Landscape gameobject.");
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
            }
        }

        #endregion

        #region Public Static Methods
        /// <summary>
        /// Create a new Camera Path in the scene and return the LBCameraPath instance
        /// Add it under the landscape gameobject provided
        /// </summary>
        /// <returns></returns>
        public static LBCameraPath CreateCameraPath(LBLandscape landscape, GameObject landscapeGameObject)
        {
            LBCameraPath lbcameraPath = null;

            // Create a camera path object
            GameObject cameraPathObj = new GameObject("Camera Path");
            if (cameraPathObj != null)
            {
                if (landscapeGameObject != null) { cameraPathObj.transform.parent = landscapeGameObject.transform; }

                lbcameraPath = cameraPathObj.AddComponent<LBCameraPath>();

                if (lbcameraPath != null)
                {
                    // Create a new path instance
                    lbcameraPath.lbPath = new LBPath();
                    if (lbcameraPath.lbPath != null)
                    {
                        // Configure the path instance
                        lbcameraPath.lbPath.snapToTerrain = false;
                        lbcameraPath.lbPath.pathType = LBPath.PathType.CameraPath;
                        lbcameraPath.lbPath.pathName = cameraPathObj.name;
                    }
                }
            }

            return lbcameraPath;
        }

        /// <summary>
        /// Get a list of all the LBCameraPaths in the scene. This will be slow
        /// so don't put it in an Update().
        /// </summary>
        /// <returns></returns>
        public static List<LBCameraPath> GetCameraPathsInScene()
        {
            return new List<LBCameraPath>(GameObject.FindObjectsOfType<LBCameraPath>());
        }

        /// <summary>
        /// Get a list of all the LBCameraPaths under a landscape parent gameobject
        /// If there are many prefabs in the landscape this could be slow so don't
        /// put it in an Update()
        /// </summary>
        /// <param name="lbLandscape"></param>
        /// <returns></returns>
        public static List<LBCameraPath> GetCameraPathsInLandscape(LBLandscape lbLandscape)
        {
            if (lbLandscape == null) { return new List<LBCameraPath>(); }
            else { return new List<LBCameraPath>(lbLandscape.GetComponentsInChildren<LBCameraPath>(true)); }
        }

        /// <summary>
        /// Remove all existing CameraPath's from the scene within a landscape
        /// </summary>
        /// <param name="lbLandscape"></param>
        public static void RemoveCameraPathsFromScene(LBLandscape lbLandscape)
        {
            if (lbLandscape == null) { Debug.LogWarning("LBCameraPath.RemoveCameraPathsFromScene - landscape is not defined"); }
            else
            {
                List<LBCameraPath> lbCameraPathList = GetCameraPathsInLandscape(lbLandscape);

                if (lbCameraPathList != null)
                {
                    // Go backwards through the list
                    for (int i = lbCameraPathList.Count - 1; i >= 0; i--)
                    {
                        DestroyImmediate(lbCameraPathList[i].gameObject);
                    }
                }
            }
        }

        #endregion

        #region Public Non-Static Methods

        /// <summary>
        /// Update the LBCameraPath to use the centre points along
        /// a LBMapPath's path.
        /// </summary>
        /// <param name="lbMapPath"></param>
        /// <returns></returns>
        public bool ImportMapPathPoints(LBMapPath lbMapPath)
        {
            bool isSuccessful = false;

            if (lbMapPath != null)
            {
                LBPath newlbPath = new LBPath(LBPath.PathType.CameraPath);
                if (newlbPath != null)
                {
                    newlbPath.positionList.AddRange(lbMapPath.lbPath.positionList);
                    newlbPath.selectedList.Clear();
                    newlbPath.selectedList.Add(0);
                    // Copy across some of the previous values
                    newlbPath.findZoomDistance = lbPath.findZoomDistance;
                    newlbPath.zoomOnFind = lbPath.zoomOnFind;
                    newlbPath.heightAboveTerrain = lbPath.heightAboveTerrain;
                    newlbPath.pathResolution = lbPath.pathResolution;
                    this.lbPath = null;
                    this.lbPath = newlbPath;
                    isSuccessful = true;
                }
            }

            return isSuccessful;
        }

        /// <summary>
        /// Update the LBCameraPath to use the centre spoints along a LBPath
        /// </summary>
        /// <param name="lbPathToImport"></param>
        /// <returns></returns>
        public bool ImportPathPoints(LBPath lbPathToImport)
        {
            bool isSuccessful = false;

            if (lbPathToImport != null)
            {
                LBPath newlbPath = new LBPath(LBPath.PathType.CameraPath);
                if (newlbPath != null)
                {
                    newlbPath.positionList.AddRange(lbPathToImport.positionList);
                    newlbPath.selectedList.Clear();
                    newlbPath.selectedList.Add(0);
                    // Copy across some of the previous values
                    newlbPath.findZoomDistance = lbPath.findZoomDistance;
                    newlbPath.zoomOnFind = lbPath.zoomOnFind;
                    newlbPath.heightAboveTerrain = lbPath.heightAboveTerrain;
                    newlbPath.pathResolution = lbPath.pathResolution;
                    this.lbPath = null;
                    this.lbPath = newlbPath;
                    isSuccessful = true;
                }
            }

            return isSuccessful;
        }

        #endregion

        #region Private Methods

        // Gizmo Drawing
        private void OnDrawGizmos()
        {
            if (lbPath.showPathInScene) { lbPath.CachePathPointDistances(); DrawPath(false, false); }
        }

        private void OnDrawGizmosSelected()
        {
            if (lbPath == null) { return; }

#if CAMERA_PATH_DEBUG_MODE
        if (lbPath.showPathInScene) { lbPath.CachePathPointDistances(); DrawPath(true, true); }
#else
            if (lbPath.showPathInScene) { lbPath.CachePathPointDistances(); DrawPath(true, false); }
#endif
        }

        private void DrawPath(bool selected, bool isSplinePointsDrawn)
        {
            if (lbPath == null) { return; }
            else if (lbPath.cachedPathPointDistances == null || lbPath.positionList == null) { return; }

            int numPositions = lbPath.positionList.Count;

            if (numPositions > 0)
            {
                // Make the gizmos red, and if this object is not the one selected, make the gizmos transparent
                if (selected) { Gizmos.color = Color.red; }
                else { Gizmos.color = new Color(1f, 0f, 0f, 0.25f); }

                // Draw a sphere gizmo at the position of every point in the camera path
                for (int i = 0; i < lbPath.positionList.Count; i++)
                {
                    Gizmos.DrawSphere(lbPath.positionList[i], 1f);

                    if (lbPath.selectedList.FindIndex(s => s == i) >= 0)
                    {
                        Gizmos.DrawWireCube(lbPath.positionList[i], Vector3.one * 1.5f);
                    }
                }

                // To draw the path lines we need at least 2 points
                if (lbPath.positionList.Count > 1)
                {
                    lbPath.CacheSplinePointDistances();
                    Vector3[] splinePointsCentre = lbPath.cachedCentreSplinePoints;
                    int numSplinePoints = 0;
                    Vector3 from = Vector3.zero;
                    Vector3 to = Vector3.zero;

                    // Draw the centre spline
                    if (splinePointsCentre != null)
                    {
                        numSplinePoints = splinePointsCentre.Length;
                        if (numSplinePoints > 1)
                        {
                            from = splinePointsCentre[0];
                            for (int i = 0; i < numSplinePoints; i++)
                            {
                                to = splinePointsCentre[i];
                                if (selected && isSplinePointsDrawn)
                                {
                                    Gizmos.DrawSphere(to, 0.25f);
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
                }
            }
        }

        #endregion
    }

    #region Custom Editor

#if UNITY_EDITOR
    [CustomEditor(typeof(LBCameraPath))]
    public class LBCameraPathInspector : Editor
    {
        #region Variables

        // Custom editor for LB Camera Path
        private bool isSceneSaveRequired = false;
        private bool isContinueToAddPoints = false;
        private string easyRoadsInstalledText = "";
        private int i = 0, p = 0;
        private LBRoad lbRoad = null;
        private LBRoad lbRoadMoveDown = null;
        private LBRoad lbCompareRoad = null;
        private int lbRoadMovePos = 0;
        private List<LBRoad> tempRoadList = null;
        private List<Vector3> tempPathSortList = new List<Vector3>();
        private Vector3[] splinePoints = null;
        private int splinePointFilterSize = 0;
        private int pathResolutionInt = 50;
        private int insertPointPos = -1;
        private int deletePointPos = -1;
        private int findPointPos = -1;
        private GUIStyle buttonCompact;
        private int findZoomDistanceInt = 0;
        private int heightAboveTerrainInt = 0;
        private string txtColourName = "Black";
        private bool isSelected = false;
        private string labelText;
        private GUIStyle labelFieldRichText;
        private bool isGetMapPath = false;
        private LBMapPath lbMapPath = null;
        private bool isGetObjPath = false;
        private string objPathGUID = "";

        // OnSceneGUI only variables
        private bool isCtrlKeyPressed = false;
        private Vector3 startPos = Vector3.zero;
        private bool isSceneDirtyRequired = false;
        private int numPositionsSceneGUI = 0;

        #endregion

        #region Static GUIContent
        private static GUIContent pathResolutionContent = new GUIContent("Path Resolution", "This is the size of the segments that make up the path. A lower number will be a result in a higher quality path but will be slower to render. (Default: 20)");
        private static GUIContent resnapToTerrainButtonContent = new GUIContent("RE-SNAP", "Re-snap the path to the terrain.");
        private static GUIContent heightOffsetContent = new GUIContent("Height Offset", "When 'Snap To Terrain' is enabled, the points will maintain the height specified as they are drawn in the scene view.");
        private static GUIContent zoomOnFindContent = new GUIContent("Zoom On Find", "Display the path point in the scene view, and zoom to the indicated distance");
        private static GUIContent findZoomDistanceContent = new GUIContent("Find Zoom", "The distance to zoom out when (F)inding a path point in the scene view");
        private readonly static GUIContent objPathGUIDContent = new GUIContent("Obj Path GUID", "This is the Group Member GUID of the Object Path. Find it by scripting out the Group (S button on Group), then looking in the script.");
        #endregion

        #region Public Static Methods
        // Add a menu item so that a camera path can be created via the GameObject > 3D Object menu
        [MenuItem("GameObject/3D Object/Landscape Builder/Camera Path")]
        public static void ShowWindow()
        {
            // Create a camera path object
            GameObject cameraPathObj = new GameObject("Camera Path");
            cameraPathObj.AddComponent<LBCameraPath>();
        }

        /// <summary>
        /// Add a point to the end of the supplied camera path.
        /// See also AddPointAtMousePosition()
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="lbCameraPathAppend"></param>
        /// <param name="pointToAdd"></param>
        public static void AppendPathPoint(LBLandscape landscape, LBCameraPath lbCameraPathAppend, Vector3 pointToAdd)
        {
            string methodName = "LBCameraPath.AppendPathPoint";

            // Validate basics and get out early if there are issues
            if (lbCameraPathAppend == null) { Debug.LogWarning("ERROR: " + methodName + " LBCameraPath is null"); return; }
            else if (lbCameraPathAppend.lbPath == null) { Debug.LogWarning("ERROR: " + methodName + " LBCameraPath.lbPath is null"); return; }
            else if (landscape == null) { Debug.LogWarning("ERROR: " + methodName + " landscape is null"); return; }

            bool pointToAddZero = (pointToAdd.x == 0f && pointToAdd.y == 0f && pointToAdd.z == 0f);

            if (!pointToAddZero || lbCameraPathAppend.lbPath.positionList.Count > 0)
            {
                int currentLastItem = lbCameraPathAppend.lbPath.positionList.Count - 1;

                // When another position is added, by default, make it a duplicate of the previous one if the point given is at 0,0,0
                if (pointToAddZero)
                {
                    lbCameraPathAppend.lbPath.positionList.Add(lbCameraPathAppend.lbPath.positionList[currentLastItem]);
                }
                else
                {
                    // If a location was indicated on the landscape, add that one. This typically occurs when the
                    // user clicks the '+' key when the mouse is over the scene view.
                    lbCameraPathAppend.lbPath.positionList.Add(pointToAdd);
                }

                // Clear the selection list and add this new path point
                lbCameraPathAppend.lbPath.selectedList.Clear();
                lbCameraPathAppend.lbPath.selectedList.Add(currentLastItem + 1);

                // Only zoom and change scene view camera if zoom is enabled
                if (lbCameraPathAppend.lbPath.zoomOnFind)
                {
                    LBEditorHelper.PositionSceneView(lbCameraPathAppend.lbPath.positionList[currentLastItem + 1], lbCameraPathAppend.lbPath.findZoomDistance, typeof(LBCameraPath));
                }
            }
            else
            {
                Vector3 firstPointCentre = Vector3.zero;

                if (landscape == null) { Debug.LogWarning("WARNING: " + methodName + " - cannot find landscape"); }
                // Add the first 
                else
                {
                    // If possible, place the first point on the landscape in the centre of the sceneview
                    firstPointCentre = LBEditorHelper.GetCentreSceneView(typeof(LBCameraPath));

                    Rect worldBounds = LBLandscapeTerrain.GetLandscapeWorldBounds(landscape.GetComponentsInChildren<Terrain>());

                    Vector2 firstPointXZ = new Vector2(firstPointCentre.x, firstPointCentre.z);

                    // Is the centre of the screen inside the landscape? If not, set to near bottom left corner of landscape
                    if (!worldBounds.Contains(firstPointXZ))
                    {
                        firstPointCentre.x = worldBounds.xMin + 20f;
                        firstPointCentre.z = worldBounds.yMin + 20f;

                        firstPointXZ.x = firstPointCentre.x;
                        firstPointXZ.y = firstPointCentre.z;
                    }

                    firstPointCentre.y = LBLandscapeTerrain.GetHeight(landscape, firstPointXZ, false) + lbCameraPathAppend.lbPath.heightAboveTerrain + landscape.start.y;

                    lbCameraPathAppend.lbPath.positionList.Add(firstPointCentre);

                    // Clear the selection list and add this new path point
                    lbCameraPathAppend.lbPath.selectedList.Clear();
                    lbCameraPathAppend.lbPath.selectedList.Add(0);

                    LBEditorHelper.PositionSceneView(lbCameraPathAppend.lbPath.positionList[0], lbCameraPathAppend.lbPath.findZoomDistance, typeof(LBCameraPath));
                }
            }
        }

        #endregion

        #region Initialise Custom Inspector

        /// <summary>
        /// Called automatically by Unity when the Editor get's enabled, which
        /// is essentially when the parent gameobject gets selected in the Hierarchy
        /// </summary>
        private void OnEnable()
        {
            // Turn off the gizmos for the selected Path Gameobject
            Tools.hidden = true;
            // Switch to the Move tool
            Tools.current = Tool.Move;

            // Dark skin uses a slightly transparent white, while personal edition uses a black
            if (EditorGUIUtility.isProSkin) { txtColourName = "#ffffffaa"; }
            else { txtColourName = "black"; }
        }

        /// <summary>
        /// Called automatically by Unity when the gameobject looses focus
        /// </summary>
        private void OnDisable()
        {
            // Turn on the default scene handles
            Tools.hidden = false;
            Tools.current = Tool.Move;
        }

        #endregion

        // This function overides what is normally seen in the inspector window
        // This allows stuff like buttons to be drawn there
        public override void OnInspectorGUI()
        {
            LBCameraPath cameraPathScript = (LBCameraPath)target;

            EditorGUIUtility.labelWidth = 150f;
            buttonCompact = new GUIStyle("Button");
            buttonCompact.fontSize = 8;
            labelFieldRichText = new GUIStyle("Label");
            labelFieldRichText.richText = true;

            if (cameraPathScript.lbPath.positionList == null) { cameraPathScript.lbPath.positionList = new List<Vector3>(); }

            EditorGUILayout.HelpBox("Path length: " + cameraPathScript.lbPath.splineLength.ToString("0") + "m", MessageType.Info);

            GUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUI.BeginChangeCheck();
            cameraPathScript.lbPath.showPathInScene = EditorGUILayout.Toggle("Show Path in Scene", cameraPathScript.lbPath.showPathInScene);
            cameraPathScript.lbPath.closedCircuit = EditorGUILayout.Toggle("Closed Circuit", cameraPathScript.lbPath.closedCircuit);

            // Store the value as a float, but display as Integer in slider
            pathResolutionInt = Mathf.RoundToInt(cameraPathScript.lbPath.pathResolution);
            pathResolutionInt = EditorGUILayout.IntSlider(pathResolutionContent, pathResolutionInt, 2, 100);
            cameraPathScript.lbPath.pathResolution = (float)pathResolutionInt;

            if (EditorGUI.EndChangeCheck()) { isSceneSaveRequired = true; }

            EditorGUI.BeginChangeCheck();
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Snap To Terrain", "Should the path points follow the height of the terrain?"), GUILayout.Width(EditorGUIUtility.labelWidth - 4f));
            cameraPathScript.lbPath.snapToTerrain = EditorGUILayout.Toggle(cameraPathScript.lbPath.snapToTerrain, GUILayout.Width(15f));
            if (cameraPathScript.lbPath.snapToTerrain)
            {
                // We don't need to call anything here because the button click will trigger the EditorGUI.EndChangeCheck below
                GUILayout.Button(resnapToTerrainButtonContent, buttonCompact, GUILayout.MaxWidth(50f));
            }
            GUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck())
            {
                if (cameraPathScript.lbPath.snapToTerrain) { cameraPathScript.lbPath.RefreshPathHeights(cameraPathScript.landscape); }
                isSceneSaveRequired = true;
            }

            if (cameraPathScript.lbPath.snapToTerrain)
            {
                // Store the value as a float, but display as Integer in slider
                heightAboveTerrainInt = Mathf.RoundToInt(cameraPathScript.lbPath.heightAboveTerrain);
                EditorGUI.BeginChangeCheck();
                heightAboveTerrainInt = EditorGUILayout.IntSlider(heightOffsetContent, heightAboveTerrainInt, 0, 500);
                if (EditorGUI.EndChangeCheck())
                {
                    cameraPathScript.lbPath.heightAboveTerrain = (float)heightAboveTerrainInt;
                    cameraPathScript.lbPath.RefreshPathHeights(cameraPathScript.landscape);
                    isSceneSaveRequired = true;
                }
            }

            EditorGUI.BeginChangeCheck();
            cameraPathScript.lbPath.zoomOnFind = EditorGUILayout.Toggle(zoomOnFindContent, cameraPathScript.lbPath.zoomOnFind);

            if (cameraPathScript.lbPath.zoomOnFind)
            {
                // Store the value as a float, but display as Integer in slider
                findZoomDistanceInt = Mathf.RoundToInt(cameraPathScript.lbPath.findZoomDistance);
                findZoomDistanceInt = EditorGUILayout.IntSlider(findZoomDistanceContent, findZoomDistanceInt, 2, 300);
                cameraPathScript.lbPath.findZoomDistance = (float)findZoomDistanceInt;
            }

            if (EditorGUI.EndChangeCheck()) { isSceneSaveRequired = true; }
            GUILayout.EndVertical();

            #region EasyRoads Integration
            GUILayout.BeginVertical(EditorStyles.helpBox);
            if (cameraPathScript.isEasyRoadsInstalled) { easyRoadsInstalledText = "Installed"; }
            else { easyRoadsInstalledText = ""; }
            EditorGUI.BeginChangeCheck();
            GUILayout.BeginHorizontal();
            cameraPathScript.useEasyRoads = EditorGUILayout.Toggle("Use EasyRoads", cameraPathScript.useEasyRoads);
            EditorGUILayout.LabelField(easyRoadsInstalledText);
            GUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck())
            {
                isSceneSaveRequired = true;
                if (cameraPathScript.useEasyRoads)
                {
                    cameraPathScript.isEasyRoadsInstalled = LBIntegration.isEasyRoads3DInstalled();
                }
            }

            // Display EasyRoads options
            if (cameraPathScript.useEasyRoads && cameraPathScript.isEasyRoadsInstalled)
            {
                EditorGUI.BeginChangeCheck();
                cameraPathScript.heightAboveRoad = EditorGUILayout.Slider(new GUIContent("Camera height", "The camera path position above the road"), cameraPathScript.heightAboveRoad, 0.1f, 20f);
                cameraPathScript.splineType = (LBRoad.SplineType)EditorGUILayout.EnumPopup(new GUIContent("Spline Type", "The EasyRoads spline which will make up the path points (default: MarkerSpline)"), cameraPathScript.splineType);
                if (EditorGUI.EndChangeCheck()) { isSceneSaveRequired = true; }

                // Display the list of ERRoads (create empty list if it doesn't already exist)
                if (cameraPathScript.roadList == null) { cameraPathScript.roadList = new List<LBRoad>(); isSceneSaveRequired = true; }

                if (cameraPathScript.roadList != null)
                {
                    // Loop through the list of roads
                    for (i = 0; i < cameraPathScript.roadList.Count; i++)
                    {
                        lbRoad = cameraPathScript.roadList[i];

                        if (lbRoad != null)
                        {
                            // Display the selectable road
                            GUILayout.BeginHorizontal();
                            if (GUILayout.Button("v", buttonCompact, GUILayout.MaxWidth(20f)))
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
                        tempRoadList.AddRange(cameraPathScript.roadList);
                    }

                    // Retrieve the current list of roads
                    cameraPathScript.roadList = LBIntegration.GetERRoadList();

                    // Compare the two list and re-select previously selected roads
                    if (cameraPathScript.roadList != null && tempRoadList != null)
                    {
                        for (i = 0; i < cameraPathScript.roadList.Count; i++)
                        {
                            lbCompareRoad = tempRoadList.Find(r => r.roadName == cameraPathScript.roadList[i].roadName);
                            if (lbCompareRoad != null)
                            {
                                // Retain user setting for road of the same name
                                cameraPathScript.roadList[i].isSelected = lbCompareRoad.isSelected;
                                cameraPathScript.roadList[i].isReversed = lbCompareRoad.isReversed;
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
                    if (cameraPathScript.lbPath.positionList.Count > 0)
                    {
                        isContinueToAddPoints = EditorUtility.DisplayDialog("Overwrite existing path points?", "This action will clear all current path points", "Overwrite", "Cancel");
                        if (isContinueToAddPoints)
                        {
                            // Save current points in undo list
                            cameraPathScript.pathPointsUndo.Clear();
                            cameraPathScript.pathPointsUndo.AddRange(cameraPathScript.lbPath.positionList);
                            // Clear the current list
                            cameraPathScript.lbPath.positionList.Clear();
                        }
                    }

                    // Add new path points
                    if (isContinueToAddPoints && cameraPathScript.roadList != null)
                    {
                        // Loop through the list of roads
                        for (i = 0; i < cameraPathScript.roadList.Count; i++)
                        {
                            lbRoad = cameraPathScript.roadList[i];

                            if (lbRoad != null)
                            {
                                // Get the list of spline points for this road if it is selected in the list
                                if (lbRoad.isSelected)
                                {
                                    // For Centre, Left and Right splines, reduce the number of points returned.
                                    if (cameraPathScript.splineType == LBRoad.SplineType.MarkerSpline) { splinePointFilterSize = 0; }
                                    else { splinePointFilterSize = 10; }

                                    // Get the spline points
                                    splinePoints = LBIntegration.GetERRoadSplinePoints(lbRoad, cameraPathScript.splineType, splinePointFilterSize);

                                    // Validate the returned data then add the points the path
                                    if (splinePoints == null)
                                    {
                                        Debug.LogWarning("LBCameraPath no spline points to add to update path for road: " + lbRoad.roadName);
                                    }
                                    else if (splinePoints.Length == 0)
                                    {
                                        Debug.LogWarning("LBCameraPath no spline points to add to update path for road: " + lbRoad.roadName);
                                    }
                                    else
                                    {
                                        // update the LBRoad class instance with the spline points
                                        if (cameraPathScript.splineType == LBRoad.SplineType.CentreSpline)
                                        {
                                            lbRoad.centreSpline = splinePoints;
                                        }
                                        else if (cameraPathScript.splineType == LBRoad.SplineType.LeftSpline)
                                        {
                                            lbRoad.leftSpline = splinePoints;
                                        }
                                        else if (cameraPathScript.splineType == LBRoad.SplineType.RightSpline)
                                        {
                                            lbRoad.rightSpline = splinePoints;
                                        }
                                        else if (cameraPathScript.splineType == LBRoad.SplineType.MarkerSpline)
                                        {
                                            lbRoad.markerSpline = splinePoints;
                                        }

                                        tempPathSortList.AddRange(splinePoints);

                                        // Reverse the list of points if required
                                        if (lbRoad.isReversed) { tempPathSortList.Reverse(); }

                                        // Add the camera height
                                        for (p = 0; p < tempPathSortList.Count; p++)
                                        {
                                            tempPathSortList[p] = new Vector3(tempPathSortList[p].x, tempPathSortList[p].y + cameraPathScript.heightAboveRoad, tempPathSortList[p].z);
                                        }

                                        // Add the modified path list to the pathPoints list
                                        cameraPathScript.lbPath.positionList.AddRange(tempPathSortList);
                                        tempPathSortList.Clear();
                                    }
                                }
                            }
                        }
                        isSceneSaveRequired = true;
                    }
                }
                if (GUILayout.Button("Undo Path", GUILayout.MaxWidth(70f)))
                {
                    cameraPathScript.lbPath.positionList.Clear();
                    cameraPathScript.lbPath.positionList.AddRange(cameraPathScript.pathPointsUndo);
                    isSceneSaveRequired = true;
                }
                GUILayout.EndHorizontal();

                if (lbRoadMoveDown != null)
                {
                    // Attempt to move this road down one in the list
                    if (cameraPathScript.roadList.Count > 1)
                    {
                        // If this is the last in the list we want to put it at the top
                        if (lbRoadMovePos == cameraPathScript.roadList.Count - 1)
                        {
                            cameraPathScript.roadList.Insert(0, lbRoadMoveDown);
                            cameraPathScript.roadList.RemoveAt(cameraPathScript.roadList.Count - 1);
                        }
                        else
                        {
                            // Move down one in the list
                            cameraPathScript.roadList.RemoveAt(lbRoadMovePos);
                            cameraPathScript.roadList.Insert(lbRoadMovePos + 1, lbRoadMoveDown);
                        }
                        isSceneSaveRequired = true;
                    }
                    lbRoadMoveDown = null;
                }
            }
            GUILayout.EndVertical();
            // End ER Integration
            #endregion

            #region Import From MapPath

            GUILayout.BeginHorizontal(EditorStyles.helpBox);
            if (isGetMapPath)
            {
                //GUILayoutOption[] guiLayoutPathWidthOptions = { GUILayout.MaxWidth(140f), GUILayout.MaxHeight(19f) };
                // Allow user to drag in or select a Map Path from the scene view
                lbMapPath = (LBMapPath)EditorGUILayout.ObjectField("Map Path", lbMapPath, typeof(LBMapPath), true);

                // Check if user wants to use this map path
                KeyCode keyPressed = Event.current.keyCode;
                if (keyPressed == KeyCode.Escape) { lbMapPath = null; isGetMapPath = false; }
                else if (lbMapPath != null)
                {
                    string msg = "LBCameraPath - " + lbMapPath.name + " doesn't have any points to import.";
                    if (lbMapPath.lbPath == null) { Debug.LogWarning(msg); }
                    else if (lbMapPath.lbPath.positionList == null) { Debug.LogWarning(msg); }
                    else if (lbMapPath.lbPath.positionList.Count < 1) { Debug.LogWarning(msg); }
                    else
                    {
                        // Check if there are any existing points in path
                        isContinueToAddPoints = true;

                        if (cameraPathScript.lbPath.positionList.Count > 0)
                        {
                            isContinueToAddPoints = EditorUtility.DisplayDialog("Overwrite existing path points?", "This action will clear all current path points", "Overwrite", "Cancel");
                        }

                        if (isContinueToAddPoints)
                        {
                            LBPath lbPath = new LBPath(LBPath.PathType.CameraPath);
                            if (lbPath != null)
                            {
                                lbPath.positionList.AddRange(lbMapPath.lbPath.positionList);
                                lbPath.selectedList.Clear();
                                lbPath.selectedList.Add(0);
                                // Copy across some of the previous values
                                lbPath.findZoomDistance = cameraPathScript.lbPath.findZoomDistance;
                                lbPath.zoomOnFind = cameraPathScript.lbPath.zoomOnFind;
                                cameraPathScript.lbPath = null;
                                cameraPathScript.lbPath = lbPath;
                                if (lbPath.zoomOnFind)
                                {
                                    LBEditorHelper.PositionSceneView(cameraPathScript.lbPath.positionList[0], cameraPathScript.lbPath.findZoomDistance, this.GetType());
                                }
                            }
                        }
                    }
                    // Hide the MapPath ObjectField and show the Import from Map path button
                    lbMapPath = null;
                    isSceneSaveRequired = true;
                    isGetMapPath = false;
                }
            }

            // Clicking this button will make the MapPath ObjectField to become visible on the next frame
            else if (GUILayout.Button("Import from Map Path", GUILayout.MaxWidth(140f)))
            {
                isGetMapPath = true;
            }

            GUILayout.EndHorizontal();

            #endregion

            #region Import From ObjPath

            GUILayout.BeginHorizontal(EditorStyles.helpBox);
            if (isGetObjPath)
            {
                // Allow user to drag in or select a Map Path from the scene view
                objPathGUID = EditorGUILayout.TextField(objPathGUIDContent, objPathGUID);

                // Check if user wants to use this map path
                KeyCode keyPressed = Event.current.keyCode;
                if (keyPressed == KeyCode.Escape) { objPathGUID = string.Empty; isGetObjPath = false; }

                else if (GUILayout.Button("Get", GUILayout.MaxWidth(40f)))
                {
                    // Look up the object path
                    LBObjPath lbObjPath = LBGroup.GetObjectPath(cameraPathScript.landscape.lbGroupList, objPathGUID);

                    if (lbObjPath == null) { Debug.LogWarning("LBCameraPath - could not find Object Path."); }
                    else if (lbObjPath.positionList == null) { Debug.LogWarning("LBCameraPath - " + lbObjPath.pathName + " doesn't have any points to import."); }
                    else if (lbObjPath.positionList.Count < 1) { Debug.LogWarning("LBCameraPath - " + lbObjPath.pathName + " doesn't have any points to import."); }
                    else
                    {
                        // Check if there are any existing points in path
                        isContinueToAddPoints = true;

                        if (cameraPathScript.lbPath.positionList.Count > 0)
                        {
                            isContinueToAddPoints = EditorUtility.DisplayDialog("Overwrite existing path points?", "This action will clear all current path points", "Overwrite", "Cancel");
                        }

                        if (isContinueToAddPoints)
                        {
                            LBPath lbPath = new LBPath(LBPath.PathType.CameraPath);
                            if (lbPath != null)
                            {
                                lbPath.positionList.AddRange(lbObjPath.positionList);
                                lbPath.selectedList.Clear();
                                lbPath.selectedList.Add(0);
                                // Copy across some of the previous values
                                lbPath.findZoomDistance = cameraPathScript.lbPath.findZoomDistance;
                                lbPath.zoomOnFind = cameraPathScript.lbPath.zoomOnFind;
                                cameraPathScript.lbPath = null;
                                cameraPathScript.lbPath = lbPath;
                                if (lbPath.zoomOnFind)
                                {
                                    LBEditorHelper.PositionSceneView(cameraPathScript.lbPath.positionList[0], cameraPathScript.lbPath.findZoomDistance, this.GetType());
                                }
                            }
                        }
                    }
                    // Hide the ObjPath ObjectField and show the Import from Object path button
                    objPathGUID = string.Empty;
                    lbObjPath = null;
                    isSceneSaveRequired = true;
                    isGetObjPath = false;
                }
            }

            // Clicking this button will make the ObjPath ObjectField to become visible on the next frame
            else if (GUILayout.Button("Import from Obj Path", GUILayout.MaxWidth(140f)))
            {
                isGetObjPath = true;
            }

            GUILayout.EndHorizontal();

            #endregion

            EditorGUI.BeginChangeCheck();
            cameraPathScript.showPathPointsList = EditorGUILayout.Foldout(cameraPathScript.showPathPointsList, "Path Points");
            if (EditorGUI.EndChangeCheck()) { SceneView.RepaintAll(); }
            if (!cameraPathScript.showPathPointsList)
            {
                EditorGUILayout.LabelField("Path Points: " + cameraPathScript.lbPath.positionList.Count.ToString("00"));
            }
            else
            {
                labelText = "Click F(ind) to select a path point in the scene view. Hold the ctrl key down to move all the points at the same time.";
                EditorGUILayout.HelpBox(labelText, MessageType.Info);

                // Display add and remove buttons
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Path Points: " + cameraPathScript.lbPath.positionList.Count.ToString("00"));

                if (GUILayout.Button("+", GUILayout.MaxWidth(30f)))
                {
                    if (cameraPathScript.lbPath.positionList.Count > 0)
                    {
                        int itemToDup = cameraPathScript.lbPath.positionList.Count - 1;

                        // When another position is added, make it a duplicate of the previous one
                        cameraPathScript.lbPath.positionList.Add(cameraPathScript.lbPath.positionList[itemToDup]);

                        // Clear the selection list and add this new path point
                        cameraPathScript.lbPath.selectedList.Clear();
                        cameraPathScript.lbPath.selectedList.Add(itemToDup + 1);
                        LBEditorHelper.PositionSceneView(cameraPathScript.lbPath.positionList[itemToDup + 1], cameraPathScript.lbPath.findZoomDistance, this.GetType());
                    }
                    else
                    {
                        Vector3 firstPoint = new Vector3();
                        firstPoint = Vector3.zero;

                        if (cameraPathScript.landscape != null)
                        {
                            // If possible, place the first point on the landscape in the centre of the sceneview
                            firstPoint = LBEditorHelper.GetCentreSceneView(this.GetType());

                            Rect worldBounds = LBLandscapeTerrain.GetLandscapeWorldBounds(cameraPathScript.landscape.GetComponentsInChildren<Terrain>());
                            Vector2 firstPointXZ = new Vector2(firstPoint.x, firstPoint.z);

                            // Is the centre of the screen inside the landscape? If not, set to near bottom left corner of landscape
                            if (!worldBounds.Contains(firstPointXZ))
                            {
                                firstPoint.x = worldBounds.xMin + 20f;
                                firstPoint.z = worldBounds.yMin + 20f;

                                firstPointXZ.x = firstPoint.x;
                                firstPointXZ.y = firstPoint.z;
                            }

                            firstPoint.y = LBLandscapeTerrain.GetHeight(cameraPathScript.landscape, firstPointXZ, false) + cameraPathScript.landscape.start.y + 2f;
                        }
                        else
                        {
                            // Find the first landscape in the scene, and add the first path point 2m above the corner
                            LBLandscape landscape = GameObject.FindObjectOfType<LBLandscape>();

                            if (landscape != null)
                            {
                                firstPoint.x = landscape.transform.position.x;
                                firstPoint.z = landscape.transform.position.z;

                                // Get the first terrain
                                Terrain terrain = landscape.GetComponentInChildren<Terrain>();
                                if (terrain != null)
                                {
                                    // Get the terrain height at the corner, and add 2 metres
                                    firstPoint.y = terrain.terrainData.GetInterpolatedHeight(0f, 0f) + 2f;
                                }
                            }
                        }

                        cameraPathScript.lbPath.selectedList.Clear();
                        cameraPathScript.lbPath.positionList.Add(firstPoint);
                        cameraPathScript.lbPath.selectedList.Add(0);
                        LBEditorHelper.PositionSceneView(cameraPathScript.lbPath.positionList[0], cameraPathScript.lbPath.findZoomDistance, this.GetType());
                    }
                    isSceneSaveRequired = true;
                }
                if (GUILayout.Button("-", GUILayout.MaxWidth(30f)))
                {
                    int itemToRemove = cameraPathScript.lbPath.positionList.Count - 1;

                    if (cameraPathScript.lbPath.positionList.Count > 0)
                    {
                        // It might be in the selected list, so remove that first
                        cameraPathScript.lbPath.selectedList.Remove(itemToRemove);

                        // Remove the point from position list.
                        cameraPathScript.lbPath.positionList.RemoveAt(itemToRemove);

                        isSceneSaveRequired = true;
                    }
                }
                GUILayout.EndHorizontal();

                // Reset find/insert/delete point positions
                insertPointPos = -1;
                deletePointPos = -1;
                findPointPos = -1;

                EditorGUI.BeginChangeCheck();
                for (int i = 0; i < cameraPathScript.lbPath.positionList.Count; i++)
                {
                    // Display each position in the list
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("F", buttonCompact, GUILayout.MaxWidth(20f))) { findPointPos = i; }
                    if (GUILayout.Button("I", buttonCompact, GUILayout.MaxWidth(20f))) { insertPointPos = i; }
                    isSelected = cameraPathScript.lbPath.selectedList.Exists(pt => pt == i);
                    labelText = "<color=" + txtColourName + ">" + (isSelected ? "<b>" : "") + "Pt " + (i + 1).ToString("000") + (isSelected ? "</b>" : "") + "</color>";
                    EditorGUILayout.LabelField(labelText, labelFieldRichText, GUILayout.MaxWidth(48f));
                    cameraPathScript.lbPath.positionList[i] = EditorGUILayout.Vector3Field("", cameraPathScript.lbPath.positionList[i]);
                    if (GUILayout.Button("X", buttonCompact, GUILayout.MaxWidth(20f))) { deletePointPos = i; }
                    GUILayout.EndHorizontal();
                }
                if (EditorGUI.EndChangeCheck()) { isSceneSaveRequired = true; }

                #region Find Position in Scene
                if (findPointPos >= 0)
                {
                    // Move the scene view camera to the selected point
                    if (cameraPathScript.lbPath.zoomOnFind)
                    {
                        LBEditorHelper.PositionSceneView(cameraPathScript.lbPath.positionList[findPointPos], cameraPathScript.lbPath.findZoomDistance, this.GetType());
                    }
                    cameraPathScript.lbPath.selectedList.Clear();
                    cameraPathScript.lbPath.selectedList.Add(findPointPos);
                }
                #endregion

                #region Insert or Delete Positions
                if (insertPointPos >= 0)
                {
                    // Insert a duplicate of the selected point
                    cameraPathScript.lbPath.positionList.Insert(insertPointPos, cameraPathScript.lbPath.positionList[insertPointPos]);
                    LBEditorHelper.PositionSceneView(cameraPathScript.lbPath.positionList[insertPointPos], cameraPathScript.lbPath.findZoomDistance, this.GetType());
                    cameraPathScript.lbPath.selectedList.Clear();
                    cameraPathScript.lbPath.selectedList.Add(insertPointPos);
                    isSceneSaveRequired = true;
                }
                // Don't permit deletes in the same frame as an insert
                else if (deletePointPos >= 0)
                {
                    // It might be in the selected list, so remove that first
                    cameraPathScript.lbPath.selectedList.Remove(deletePointPos);

                    cameraPathScript.lbPath.positionList.RemoveAt(deletePointPos);
                    isSceneSaveRequired = true;
                }
                #endregion
            }

            // Some of the controls have changed, so mark the scene as changed so that
            // if it is run without the LBCameraPath editor script open, the values will persist.
            if (isSceneSaveRequired && !Application.isPlaying)
            {
                isSceneSaveRequired = false;
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

                // Force OnDrawGizmosSelected() to be called
                SceneView.RepaintAll();
            }
        }

        private void OnSceneGUI()
        {
            LBCameraPath cameraPathScript = (LBCameraPath)target;

            numPositionsSceneGUI = (cameraPathScript.lbPath == null || cameraPathScript.lbPath.positionList == null ? 0 : cameraPathScript.lbPath.positionList.Count);

            // Only show the handles if the path list is visible in the editor
            if (cameraPathScript.lbPath.positionList != null && cameraPathScript.showPathPointsList && cameraPathScript.lbPath.showPathInScene)
            {
                isCtrlKeyPressed = false;
                Event currentEvent = Event.current;
                bool isRightButton = (currentEvent.button == 1);

                // Did the user press "+" key to add a point to the end of the path?
                if (currentEvent.type == EventType.KeyUp && ((currentEvent.keyCode == KeyCode.Equals && currentEvent.shift) || currentEvent.keyCode == KeyCode.KeypadPlus))
                {
                    AddPointAtMousePosition(currentEvent);
                    isSceneDirtyRequired = true;
                }
                #region Context-sensitive Menu
                if (currentEvent.type == EventType.MouseDown && isRightButton && Tools.current != Tool.View && Tools.current != Tool.None)
                {
                    GenericMenu menu = new GenericMenu();

                    menu.AddItem(new GUIContent("Add Point to End"), false, () => { AddPointAtMousePosition(currentEvent); isSceneDirtyRequired = true; LBEditorHelper.RepaintLBW(); });
                    menu.AddItem(new GUIContent("Find on Zoom"), cameraPathScript.lbPath.zoomOnFind, () =>
                    {
                        cameraPathScript.lbPath.zoomOnFind = !cameraPathScript.lbPath.zoomOnFind;
                        LBEditorHelper.RepaintLBW();
                    });
                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Delete Selected Points"), false, () => { cameraPathScript.lbPath.DeleteSelectedPoints(false); isSceneDirtyRequired = true; LBEditorHelper.RepaintLBW(); });
                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Allow Scene View Rotation"), false, () => { Tools.current = Tool.View; });
                    menu.AddItem(new GUIContent("Unselect"), false, () => { Selection.activeObject = null; cameraPathScript.lbPath.selectedList.Clear(); });
                    // The Cancel option is not really necessary as use can just click anywhere else. However, it may help some users.
                    menu.AddItem(new GUIContent("Cancel"), false, () => { });
                    menu.ShowAsContext();
                    currentEvent.Use();
                }
                #endregion
                else
                {

                    for (int i = 0; i < numPositionsSceneGUI; i++)
                    {
                        // Read from the list only once
                        startPos = cameraPathScript.lbPath.positionList[i];

                        // Only display the handles for selected path points
                        if (cameraPathScript.lbPath.selectedList.Exists(pt => pt == i))
                        {
                            EditorGUI.BeginChangeCheck();

                            // Make a handle for each point in the path so that it can be dragged around in the scene view
                            cameraPathScript.lbPath.positionList[i] = Handles.PositionHandle(startPos, Quaternion.identity);
                            //Handles.RotationHandle(Quaternion.identity, cameraPathScript.lbPath.positionList[i]);
                            if (EditorGUI.EndChangeCheck())
                            {
                                isCtrlKeyPressed = Event.current.control;
                                if (isCtrlKeyPressed)
                                {
                                    // Move all the other centre points in the same direction as the current point was moved
                                    cameraPathScript.lbPath.MovePoints(startPos, cameraPathScript.lbPath.positionList[i], i, false);
                                }

                                isSceneDirtyRequired = true;
                            }
                        }
                        else
                        {
                            // Draw a selectable button for the non-selected points in the path
                            if (Handles.Button(startPos, Quaternion.identity, 0.01f, 0.75f, Handles.SphereHandleCap))
                            {
                                // Select this piont in the path
                                cameraPathScript.lbPath.selectedList.Clear();
                                cameraPathScript.lbPath.selectedList.Add(i);
                            }
                        }
                    }
                }

                #region Save Scene
                if (isSceneDirtyRequired && !Application.isPlaying)
                {
                    isSceneDirtyRequired = false;
                    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                }
                #endregion
            }
        }

        #region Private Methods

        /// <summary>
        /// Add a new point to the path at the current mouse position
        /// </summary>
        /// <param name="evt"></param>
        private void AddPointAtMousePosition(Event evt)
        {
            Vector3 pos = Vector3.zero;
            LBCameraPath cameraPathScript = (LBCameraPath)target;

            if (cameraPathScript != null && cameraPathScript.lbPath != null)
            {
                LBLandscape landscape = cameraPathScript.GetComponentInParent<LBLandscape>();

                if (landscape != null)
                {
                    // Get the on-ground terrain position from the mouse (this is in WorldSpace)
                    pos = LBEditorHelper.GetLandscapePositionFromMouse(landscape, evt.mousePosition, false, true);

                    // If x and z is zero, it probably means the mouse is outside the bounds of the landscape or designer.
                    if (pos.x != 0 || pos.z != 0) { pos.y += cameraPathScript.lbPath.heightAboveTerrain; }

                    evt.Use();
                    AppendPathPoint(landscape, cameraPathScript, pos);
                }
            }
        }

        #endregion
    }
#endif
    #endregion
}