#define _OBJ_PATH_DEBUG_MODE

#if UNITY_EDITOR
// Landscape Builder. Copyright (c) 2016-2019 SCSM Pty Ltd. All rights reserved.
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.SceneManagement;

namespace LandscapeBuilder
{
    /// <summary>
    /// Helper class to be added to a temporary gameobject
    /// under the landscape gameobject. It holds information
    /// about the Object Path Designer. Gets created when editing
    /// the path points in the scene from within a Group Member
    /// </summary>
    [ExecuteInEditMode]
    public class LBObjPathDesigner : MonoBehaviour
    {
        #region Public variables and properties
        [HideInInspector] public LBGroup lbGroup = null;
        [HideInInspector] public LBGroupMember lbGroupMember = null;
        [HideInInspector] public LBObjPath lbObjPath = null;
        [HideInInspector] public bool isInitialised = false;

        // Cache the landscape and the landscape height
        [System.NonSerialized] public LBLandscape landscape;
        [System.NonSerialized] public float landscapeHeight = -1f;

        // Minimum position on y-axis an item can be place in the designer
        [System.NonSerialized] public float minPosY = 0f;
        [System.NonSerialized] public float minPathWidth;

        [System.NonSerialized] public bool showPathPointsList = false;

        [System.NonSerialized] public LBGroupDesigner lbGroupDesigner = null;

        // This should only be true when editing a clearing group which is the owner of this ObjPath member.
        [System.NonSerialized] public bool isInGroupDesignerMode = false;

        // Will be 0 when used on terrains, or the BasePlaneOffsetY with the GroupDesigner
        [System.NonSerialized] public float designerOffsetY = 0f;

        #endregion

        #region Private variables and properties
        private Vector3 pathPosWorldSpace = Vector3.zero;
        private bool isPointInCameraView = false;
        private int numPositionsDrawPath = 0;

        // DrawPath variables
        private Color unSelectedColour;
        private Color lineColour;
        private Color positionColour;
        private Color surroundColour;
        private GUIStyle distanceLabel;
        private Vector3 pointLabelOffset;
        private Vector3 debugLabelOffset;

        // SceneGUI variables
        private bool isCtrlKeyPressed = false;
        private Vector2 posXZ = Vector2.zero;
        private Vector3 snappedPos = Vector3.zero;
        private Vector3 startPos = Vector3.zero;
        private Vector3 handlePos = Vector3.zero;
        private Vector3 forwardsDir = Vector3.forward;
        private Rect landscapeBounds;
        private bool isSceneDirtyRequired = false;
        private int numPositionsSceneGUI = 0;
        private Vector3 rotation = Vector3.zero;
        private LBPathPoint lbPathPoint = null;
        private Vector3 widthLabelOffset = Vector3.up * 4f;
        private GUIStyle widthLabel;
        private Color currentBoxTextColour;

        #endregion

        #region Intialise Methods

        private void Initialise()
        {
            //string methodName = "LBObjPathDesigner.Initialise";
            isInitialised = false;

            if (landscapeHeight < 0f)
            {
                if (landscape != null)
                {
                    landscape.SetLandscapeTerrains(false);
                    landscapeHeight = landscape.GetLandscapeTerrainHeight();
                }
                else
                {
                    // Avoid any divide by 0 errors
                    landscapeHeight = 0.0001f;
                }
            }

            if (landscape == null) { landscapeBounds = new Rect(-100000f, -100000f, 200000f, 200000f); }
            else
            {
                if (isInGroupDesignerMode)
                {
                    // If this is an Object Path in a clearing, set the limits to the GroupDesigner area
                    // grpBasePlaneTrfm is centred at 0, BasePlaneOffsetY, 0
                    Vector2 basePos = lbGroupDesigner.grpBasePlaneCentre2D;
                    landscapeBounds = new Rect(basePos.x - lbGroup.maxClearingRadius, basePos.y - lbGroup.maxClearingRadius, lbGroup.maxClearingRadius * 2f, lbGroup.maxClearingRadius * 2f);
                    minPosY = designerOffsetY;
                    designerOffsetY = lbGroupDesigner.BasePlaneOffsetY;
                }
                else
                {
                    landscapeBounds = landscape.GetLandscapeWorldBoundsFast();
                    minPosY = landscape.start.y;
                    designerOffsetY = 0f;
                }
            }

            // make sure there are no objects selected in the scene
            Selection.activeObject = null;

            isInitialised = true;
        }

        #endregion

        #region Event Methods

        private void OnEnable()
        {            
            #if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui -= SceneGUI;
            SceneView.duringSceneGui += SceneGUI;
            #else
            SceneView.onSceneGUIDelegate -= SceneGUI;
            SceneView.onSceneGUIDelegate += SceneGUI;
            #endif
        }

        private void OnDestroy()
        {
            // Turn on the default scene handles
            Tools.hidden = false;
            Tools.current = Tool.Move;

            #if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui -= SceneGUI;
            #else
            SceneView.onSceneGUIDelegate -= SceneGUI;
            #endif
        }

        // Delegate function used because this is not an Editor script
        private void SceneGUI(SceneView sv)
        {
            #region Initialise
            // Validate basics and get out early if there are issues
            if (!isInitialised || lbObjPath == null || landscape == null || lbGroupMember == null) { return; }
            isSceneDirtyRequired = false;
            Event currentEvent = Event.current;
            #endregion

            #region Initialise labels
            // For list of styles see: https://gist.github.com/MadLittleMods/ea3e7076f0f59a702ecb
            if (widthLabel == null)
            {
                widthLabel = new GUIStyle("Box");
                widthLabel.fontSize = 14;
                widthLabel.border = new RectOffset(2, 2, 2, 2);
                widthLabel.onFocused.textColor = UnityEngine.Color.white;
            }
            #endregion

            // Only show the handles if the path list is visible in the editor
            if (lbObjPath.positionList != null && lbObjPath.showPathInScene)
            {
                isCtrlKeyPressed = false;
                bool isRightButton = currentEvent.button == 1;

                // Did the user press "+" key to add a point to the end of the path?
                //if (currentEvent.type == EventType.KeyUp && ((currentEvent.keyCode == KeyCode.Equals && currentEvent.shift) || currentEvent.keyCode == KeyCode.KeypadPlus))
                // Allow for + or = which are mostly the same key (+ is typically shift and '=' key).
                if (currentEvent.type == EventType.KeyUp && (currentEvent.keyCode == KeyCode.Equals || currentEvent.keyCode == KeyCode.KeypadPlus))
                {
                    AddPointAtMousePosition(currentEvent);
                    isSceneDirtyRequired = true;
                }

                #region Context-sensitive Menu
                else if (currentEvent.type == EventType.MouseDown && isRightButton && Tools.current != Tool.View && Tools.current != Tool.None)
                {
                    GenericMenu menu = new GenericMenu();
                    menu.AddDisabledItem(new GUIContent((lbObjPath.pathName == null ? "ObjectPath" : lbObjPath.pathName))); // Header
                    menu.AddItem(new GUIContent("Add Point to End"), false, () => { AddPointAtMousePosition(currentEvent); isSceneDirtyRequired = true; LBEditorHelper.RepaintLBW(); });
                    menu.AddItem(new GUIContent("Insert Point Before"), false, () => { InsertPoint(false); isSceneDirtyRequired = true; LBEditorHelper.RepaintLBW(); });
                    menu.AddItem(new GUIContent("Insert Point After"), false, () => { InsertPoint(true); isSceneDirtyRequired = true; LBEditorHelper.RepaintLBW(); });
                    menu.AddSeparator("");

                    int numSelected = lbObjPath.selectedList == null ? 0 : lbObjPath.selectedList.Count;

                    if (lbGroupDesigner != null)
                    {
                        menu.AddItem(new GUIContent("Close Object Path Designer"), false, () =>
                        {
                            CloseDesigner(landscape, this, lbGroup, lbGroupMember, null);
                            if (lbGroupDesigner.GetAutoRefresh) { lbGroupDesigner.RefreshWorkspace(true); }
                            return;
                        }
                        );
                        menu.AddItem(new GUIContent("Refresh Designer"), false, () => { lbGroupDesigner.RefreshWorkspace(true); });
                        menu.AddItem(new GUIContent("Refresh Path"), false, () => { lbGroupDesigner.RefreshObjPath(lbGroup, lbGroupMember); });

                        menu.AddItem(new GUIContent("Zoom Out"), false, () => { lbGroupDesigner.ZoomExtent(sv); });
                        menu.AddItem(new GUIContent("Zoom In"), false, () => { ZoomIn(sv); });
                    }
                    else if (lbGroup.lbGroupType == LBGroup.LBGroupType.Uniform)
                    {
                        menu.AddItem(new GUIContent("Close Object Path Designer"), false, () =>
                        {
                            CloseDesigner(landscape, this, lbGroup, lbGroupMember, null);
                            return;
                        }
                        );
                        menu.AddItem(new GUIContent("Refresh"), false, () => { if (landscape != null) { landscape.ApplyObjPath(lbGroup, lbGroupMember, true, true, true); } });
                        menu.AddItem(new GUIContent("Zoom In"), false, () => { ZoomIn(sv); });
                    }

                    menu.AddItem(new GUIContent("Show Object Path"), false, () =>
                    {
                        lbGroup.GroupMemberListExpand(false);
                        lbGroupMember.showInEditor = true;
                    });

                    menu.AddItem(new GUIContent("Display/Size 1"), lbObjPath.GetPointDisplayScaleAsInt() == 1, () =>
                    {
                        lbObjPath.SetPointDisplayScale(1);                      
                        LBEditorHelper.RepaintLBW();
                    });
                    menu.AddItem(new GUIContent("Display/Size 2"), lbObjPath.GetPointDisplayScaleAsInt() == 2, () =>
                    {
                        lbObjPath.SetPointDisplayScale(2);
                        LBEditorHelper.RepaintLBW();
                    });
                    menu.AddItem(new GUIContent("Display/Size 3"), lbObjPath.GetPointDisplayScaleAsInt() == 3, () =>
                    {
                        lbObjPath.SetPointDisplayScale(3);
                        LBEditorHelper.RepaintLBW();
                    });
                    menu.AddItem(new GUIContent("Display/Size 4"), lbObjPath.GetPointDisplayScaleAsInt() == 4, () =>
                    {
                        lbObjPath.SetPointDisplayScale(4);
                        LBEditorHelper.RepaintLBW();
                    });
                    menu.AddItem(new GUIContent("Display/Size 5"), lbObjPath.GetPointDisplayScaleAsInt() == 5, () =>
                    {
                        lbObjPath.SetPointDisplayScale(5);
                        LBEditorHelper.RepaintLBW();
                    });
                    menu.AddSeparator("Display/");

                    menu.AddItem(new GUIContent("Display/Distances"), lbObjPath.showDistancesInScene, () =>
                    {
                        lbObjPath.showDistancesInScene = !lbObjPath.showDistancesInScene;
                        LBEditorHelper.RepaintLBW();
                    });
                    menu.AddItem(new GUIContent("Display/Point Labels"), lbObjPath.showPointLabelsInScene, () =>
                    {
                        lbObjPath.showPointLabelsInScene = !lbObjPath.showPointLabelsInScene;
                        LBEditorHelper.RepaintLBW();
                    });
                    if (lbObjPath.useWidth)
                    {
                        menu.AddItem(new GUIContent("Display/Surroundings"), lbObjPath.showSurroundingInScene, () =>
                        {
                            lbObjPath.showSurroundingInScene = !lbObjPath.showSurroundingInScene;
                            LBEditorHelper.RepaintLBW();
                        });
                    }
                    if (lbGroupDesigner != null)
                    {
                        menu.AddSeparator("Display/");
                        menu.AddItem(new GUIContent("Display/SubGroup Extents"), lbGroupDesigner.showSubGroupExtent, () => { lbGroupDesigner.showSubGroupExtent = !lbGroupDesigner.showSubGroupExtent; });
                        menu.AddItem(new GUIContent("Display/Member Extent Proximity"), lbGroupDesigner.showProximity, () => { lbGroupDesigner.showProximity = !lbGroupDesigner.showProximity; });
                    }
                    if (!isInGroupDesignerMode)
                    {
                        menu.AddItem(new GUIContent("Zoom on Find"), lbObjPath.zoomOnFind, () =>
                        {
                            lbObjPath.zoomOnFind = !lbObjPath.zoomOnFind;
                            LBEditorHelper.RepaintLBW();
                        });
                    }
                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Reverse Path"), false, () => { lbObjPath.Reverse(lbObjPath.showSurroundingInScene); isSceneDirtyRequired = true; });
                    if (numSelected > 0)
                    {
                        if (!lbObjPath.snapToTerrain)
                        {
                            menu.AddSeparator("");
                            menu.AddItem(new GUIContent("Snap Point to Terrain"), false, () => { SnapPointsToTerrain(); isSceneDirtyRequired = true; });
                        }
                        menu.AddItem(new GUIContent("Set All Heights/to Selected"), false, () => { SetAllHeightsToSelected(true); isSceneDirtyRequired = true; });
                        menu.AddItem(new GUIContent("Set All Heights/to Minimum"), false, () => { SetAllHeightsToValue(true, 0); isSceneDirtyRequired = true; });
                        menu.AddItem(new GUIContent("Set All Heights/to Maximum"), false, () => { SetAllHeightsToValue(true, 1); isSceneDirtyRequired = true; });
                        menu.AddItem(new GUIContent("Set All Heights/to Average"), false, () => { SetAllHeightsToValue(true, 2); isSceneDirtyRequired = true; });
                        menu.AddItem(new GUIContent("Split at Point"), false, () => { SplitAtPoint(); isSceneDirtyRequired = true; });

                        menu.AddSeparator("");
                        menu.AddItem(new GUIContent("Delete Selected Points"), false, () => { DeleteSelectedPoints(lbObjPath); isSceneDirtyRequired = true; });
                    }
                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Allow Scene View Rotation"), false, () => { Tools.current = Tool.View; });
                    menu.AddItem(new GUIContent("Unselect"), false, () => { Selection.activeObject = null; lbObjPath.selectedList.Clear(); });
                    // The Cancel option is not really necessary as use can just click anywhere else. However, it may help some users.
                    menu.AddItem(new GUIContent("Cancel"), false, () => { });
                    menu.ShowAsContext();
                    currentEvent.Use();
                }
                #endregion

                else
                {
                    numPositionsSceneGUI = lbObjPath.positionList.Count;

                    #region Display Path Points in scene
                    for (int i = 0; i < numPositionsSceneGUI; i++)
                    {
                        // Read from the lists only once
                        startPos = lbObjPath.positionList[i];
                        lbPathPoint = lbObjPath.pathPointList[i];

                        handlePos.x = startPos.x;
                        handlePos.z = startPos.z;
                        handlePos.y = startPos.y + designerOffsetY;

                        // Only display the handles for selected path points
                        if (lbObjPath.selectedList.Exists(pt => pt == i))
                        {
                            // Only get Forwards direction once
                            #if UNITY_2017_3_OR_NEWER
                            if (Tools.current == Tool.Rotate || Tools.current == Tool.Scale || Tools.current == Tool.Transform)
                            #else
                            if (Tools.current == Tool.Rotate || Tools.current == Tool.Scale)
                            #endif
                            {
                                forwardsDir = lbObjPath.GetForwardsFast(i, 1f);
                            }

                            // Process Rotate before Move as rotate doesn't change startPos.
                            // If in debug mode, also shows the direction
                            #region Rotate
                            #if UNITY_2017_3_OR_NEWER
                            if (Tools.current == Tool.Rotate || Tools.current == Tool.Transform)
                            #else
                            if (Tools.current == Tool.Rotate)
                            #endif
                            {
                                //forwardsDir = lbObjPath.GetForwardsFast(i, 1f);

                                using (new Handles.DrawingScope(Color.blue))
                                {
                                    EditorGUI.BeginChangeCheck();
                                    rotation.z = lbPathPoint.rotationZ;
                                    //lbPathPoint.rotationZ = Handles.RotationHandle(Quaternion.Euler(rotation), handlePos).eulerAngles.z;

                                    //Handles.DoPositionHandle(handlePos, Quaternion.Euler(forwardsDir));

                                    #if OBJ_PATH_DEBUG_MODE
                                    // Show Path Direction
                                    Handles.Slider(handlePos, forwardsDir, HandleUtility.GetHandleSize(handlePos), Handles.ArrowHandleCap, 0.5f);
                                    #endif

                                    //Handles.DrawWireArc(handlePos, forwardsDir, Vector3.up, 360f, 3f);

                                    //Handles.Slider(handlePos, forwardsDir, HandleUtility.GetHandleSize(handlePos), (id, position, rotation, size, type) => Handles.ArrowHandleCap(0, handlePos, Quaternion.identity, 1.0f, EventType.Repaint), 0.5f);

                                    // Bug - euler angles converts the -ve angles to +ve
                                    //lbPathPoint.rotationZ = Handles.Disc(Quaternion.Euler(rotation), handlePos, forwardsDir, 5f, false, 0).eulerAngles.z;
                                    lbPathPoint.rotationZ = Handles.Disc(Quaternion.Euler(rotation), handlePos, forwardsDir, HandleUtility.GetHandleSize(handlePos), false, 0).eulerAngles.z;

                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        // This should update the left/right path points, and the left/right etc. spline positions

                                        // No need to update if only the centre spline is used
                                        if (lbObjPath.useWidth)
                                        {
                                            // Force refresh of spline cache
                                            lbObjPath.isSplinesCached2 = false;
                                            lbObjPath.RefreshObjPathPositions(lbObjPath.showSurroundingInScene, false);
                                        }

                                        isSceneDirtyRequired = true;
                                    }
                                }
                            }
                            #endregion

                            #region Scale
                            if (Tools.current == Tool.Scale && lbObjPath.useWidth)
                            {
                                EditorGUI.BeginChangeCheck();

                                // Display a resizing tool to change the width of this point
                                lbObjPath.widthList[i] = Mathf.Abs(Handles.ScaleSlider(lbObjPath.widthList[i], handlePos, Vector3.Cross(forwardsDir, Vector3.down).normalized, Quaternion.identity, 10f, 1f));

                                using(new Handles.DrawingScope(Color.blue))
                                {
                                    currentBoxTextColour = GUI.skin.box.normal.textColor;
                                    GUI.skin.box.normal.textColor = Color.white;
                                    // Display the width of the current point
                                    Handles.Label(handlePos + widthLabelOffset, "Width: " + lbObjPath.widthList[i].ToString("0.00"), widthLabel);
                                    GUI.skin.box.normal.textColor = currentBoxTextColour;
                                }

                                // Don't seem to need to refresh edge positions here...
                                if (EditorGUI.EndChangeCheck())
                                {
                                    isSceneDirtyRequired = true;
                                }
                            }
                            #endregion

                            #region Move
                            #if UNITY_2017_3_OR_NEWER
                            if (Tools.current == Tool.Move || Tools.current == Tool.Transform)
                            #else
                            if (Tools.current == Tool.Move)
                            #endif
                            {
                                EditorGUI.BeginChangeCheck();
                                // Make a handle for each point in the path so that it can be dragged around in the scene view
                                handlePos = Handles.PositionHandle(handlePos, Quaternion.identity);
                                
                                if (EditorGUI.EndChangeCheck())
                                {
                                    handlePos.y -= designerOffsetY;

                                    if (isInGroupDesignerMode)
                                    {
                                        lbGroupDesigner.AddPrefabBlock(300f);
                                        float distanceToCentre = Vector2.Distance(lbGroupDesigner.grpBasePlaneCentre2D, new Vector2(handlePos.x, handlePos.z));
                                        if (distanceToCentre > lbGroup.maxClearingRadius) { handlePos = startPos; }
                                    }

                                    lbObjPath.positionList[i] = handlePos;

                                    // If the user has tried to move the handle outside the landscape (or GroupDesigner), snap to nearest border
                                    lbObjPath.ClampPositionToBounds(i, landscapeBounds, minPosY, landscapeHeight);

                                    // If snapping to the landscape, find the correct offset above the terrain height at this point in worldspace
                                    if (lbObjPath.snapToTerrain)
                                    {
                                        snappedPos.x = lbObjPath.positionList[i].x;
                                        snappedPos.z = lbObjPath.positionList[i].z;
                                        posXZ.x = snappedPos.x;
                                        posXZ.y = snappedPos.z;

                                        // If editing an Object Path from within the GroupDesigner, use the GroupDesigner 
                                        if (isInGroupDesignerMode)
                                        {
                                            snappedPos.y = lbObjPath.heightAboveTerrain;
                                        }
                                        else
                                        {
                                            // LBLandscapeTerrain.GetHeight() returns a normalised height
                                            snappedPos.y = LBLandscapeTerrain.GetHeight(landscape, posXZ, false) + lbObjPath.heightAboveTerrain + landscape.start.y;
                                        }
                                        lbObjPath.positionList[i] = snappedPos;
                                    }

                                    isCtrlKeyPressed = Event.current.control;
                                    if (isCtrlKeyPressed)
                                    {
                                        // Move all the other centre points in the same direction as the current point was moved
                                        lbObjPath.MovePoints(startPos, lbObjPath.positionList[i], i, false);
                                        lbObjPath.RefreshPathHeights(landscape, isInGroupDesignerMode);
                                    }

                                    // Force refresh of spline cache
                                    lbObjPath.isSplinesCached2 = false;
                                    lbObjPath.RefreshObjPathPositions(lbObjPath.showSurroundingInScene, false);

                                    isSceneDirtyRequired = true;
                                }
                            }
                            #endregion

                        }
                        else
                        {
                            // Draw a selectable button for the non-selected points in the path
                            if (Handles.Button(handlePos, Quaternion.identity, 0.01f, 0.75f, Handles.SphereHandleCap))
                            {
                                // Select this piont in the path
                                lbObjPath.selectedList.Clear();
                                lbObjPath.selectedList.Add(i);
                                // Only show the details for the selected point
                                lbGroupMember.ObjPathPointsExpand(false);
                                lbPathPoint.showInEditor = true;
                                LBEditorHelper.RepaintLBW();
                            }
                        }
                    }
                    #endregion
                }

                if (isSceneDirtyRequired && !Application.isPlaying)
                {
                    isSceneDirtyRequired = false;
                    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                    LBEditorHelper.RepaintLBW();
                }
            }
        }

        // Scene view Gizmo Drawing
        private void OnDrawGizmos()
        {
            if (lbObjPath.showPathInScene && !lbObjPath.isRefreshing)
            {
                #if OBJ_PATH_DEBUG_MODE
                DrawPath(true, true);
                #else
                DrawPath(true, false);
                #endif
            }
        }

        #endregion

        #region Private Methods

        #region Draw the Path in the Scene view
        private void DrawPath(bool selected, bool isSplinePointsDrawn)
        {
            if (lbObjPath == null) { return; }

            numPositionsDrawPath = lbObjPath.positionList.Count;

            if (numPositionsDrawPath > 0)
            {
                // Path colour is stored as a vector4
                unSelectedColour.r = lbObjPath.pathPointColour.x;
                unSelectedColour.g = lbObjPath.pathPointColour.y;
                unSelectedColour.b = lbObjPath.pathPointColour.z;
                unSelectedColour.a = 0.05f;
                lineColour = (selected ? (Color)lbObjPath.pathPointColour : unSelectedColour);
                positionColour = (selected ? (Color)lbObjPath.pathPointColour : unSelectedColour);
                surroundColour = isInGroupDesignerMode ? Color.yellow : Color.grey;
                Gizmos.color = positionColour;

                bool isDistancesVisible = lbObjPath.showDistancesInScene;
                pointLabelOffset = Vector3.up * 2f;
                debugLabelOffset = Vector3.down * 0.25f;
                bool isPointLabelVisible = lbObjPath.showPointLabelsInScene;
                isPointInCameraView = false;
                if (isDistancesVisible || isPointLabelVisible)
                {
                    distanceLabel = new GUIStyle("Box");
                    distanceLabel.fontSize = 10;
                    distanceLabel.border = new RectOffset(1, 1, 1, 1);
                    distanceLabel.onFocused.textColor = UnityEngine.Color.white;
                }

                bool useWidth = lbObjPath.useWidth;

                // LBPath spline cache will only be updated if the data has changed
                // and the isSplinesCached2 flag is false.
                // LBObjPath cached data will still be updated if useWidth is true.
                if (useWidth) { lbObjPath.RefreshObjPathPositions(lbObjPath.showSurroundingInScene, false); }
                else { lbObjPath.CacheSplinePoints2(); }

                Vector3 from = Vector3.zero;
                Vector3 to = Vector3.zero;
                Vector3 to2 = Vector3.zero;

                // Draw a sphere gizmo at the position of every point in the object path
                for (int i = 0; i < numPositionsDrawPath; i++)
                {
                    // Get the position Vector3 only once from the list.
                    pathPosWorldSpace = lbObjPath.positionList[i];
                    pathPosWorldSpace.y += designerOffsetY;

                    Gizmos.color = positionColour;
                    Gizmos.DrawSphere(pathPosWorldSpace, 1f * lbObjPath.pointDisplayScale);

                    if (useWidth)
                    {
                        // Add the left width marker
                        from = lbObjPath.positionListLeftEdge[i];
                        from.y += designerOffsetY;
                        Gizmos.DrawSphere(from, 0.5f * lbObjPath.pointDisplayScale);

                        // Add the right width marker
                        #if OBJ_PATH_DEBUG_MODE
                        Gizmos.color = Color.yellow;
                        #endif
                        from = lbObjPath.positionListRightEdge[i];
                        from.y += designerOffsetY;
                        Gizmos.DrawSphere(from, 0.5f * lbObjPath.pointDisplayScale);
                        #if OBJ_PATH_DEBUG_MODE
                        Gizmos.color = positionColour;
                        #endif
                    }

                    if (lbObjPath.selectedList.FindIndex(s => s == i) >= 0)
                    {
                        Gizmos.DrawWireCube(pathPosWorldSpace, Vector3.one * 1.25f * lbObjPath.pointDisplayScale);
                    }

                    // If required check IsPointInCameraView() only once in the loop
                    if (isDistancesVisible || isPointLabelVisible)
                    {
                        // Only show the Point labels if they are enabled in Editor AND infront of the scene view camera
                        // This prevents a ghosted handle being displayed when it is behind the camera.
                        isPointInCameraView = LBLandscape.IsPointInCameraView(SceneView.lastActiveSceneView.camera, pathPosWorldSpace);

                        if (isPointInCameraView)
                        {
                            // Display Point and Distance labels
                            if (isPointLabelVisible && isDistancesVisible)
                            {
                                Handles.Label(pathPosWorldSpace + pointLabelOffset, "Pt: " + (i + 1).ToString("000") + "  Distance: " + lbObjPath.cachedPathPointDistances[i].ToString("0.00"), distanceLabel);
                            }
                            else if (isPointLabelVisible)
                            {
                                Handles.Label(pathPosWorldSpace + pointLabelOffset, "Pt: " + (i + 1).ToString("000"), distanceLabel);
                            }
                            else // Just display distance
                            {
                                Handles.Label(pathPosWorldSpace + pointLabelOffset, "Distance: " + lbObjPath.cachedPathPointDistances[i].ToString("0.00"), distanceLabel);
                            }
                        }
                    }
                }

                // To draw the path lines we need at least 2 points
                if (numPositionsDrawPath > 1)
                {
                    int numSplinePoints = (lbObjPath.cachedCentreSplinePointList == null ? 0 : lbObjPath.cachedCentreSplinePointList.Count);

                    #region Draw the centre spline

                    if (numSplinePoints > 1)
                    {
                        from = lbObjPath.cachedCentreSplinePointList[0];
                        from.y += designerOffsetY;
                        for (int i = 0; i < numSplinePoints; i++)
                        {
                            to = lbObjPath.cachedCentreSplinePointList[i];
                            to.y += designerOffsetY;
                            if (selected && isSplinePointsDrawn)
                            {
                                #if OBJ_PATH_DEBUG_MODE
                                Gizmos.color = Color.white;
                                #else
                                Gizmos.color = positionColour;
                                #endif
                                Gizmos.DrawSphere(to, 0.25f * lbObjPath.pointDisplayScale);

                                // Also displays a ghosted handle from behind the camera. IsPointInCameraView should fix this (it does with isPointLabelVisible)... 
                                if (isDistancesVisible || isPointLabelVisible)
                                {
                                    if (LBLandscape.IsPointInCameraView(SceneView.lastActiveSceneView.camera, to))
                                    {
                                        if (isDistancesVisible)
                                        {
                                            Handles.Label(to + debugLabelOffset, "Distance: " + lbObjPath.cachedCentreSplinePointDistancesList[i].ToString("0.00"), distanceLabel);
                                        }
                                        if (isPointLabelVisible)
                                        {
                                            Handles.Label(to + debugLabelOffset + Vector3.down, "SPt: " + i.ToString("000"), distanceLabel);
                                        }
                                    }
                                }
                            }

                            Gizmos.color = lineColour;
                            Gizmos.DrawLine(from, to);
                            // Set the end of this line as the start of the next
                            from = to;
                        }

                        Gizmos.color = lineColour;

                        // Draw a line from the last location to the final point.
                        to = lbObjPath.positionList[numPositionsDrawPath - 1];
                        to.y += designerOffsetY;
                        Gizmos.DrawLine(from, to);

                        if (lbObjPath.closedCircuit)
                        {
                            // Draw a final line to connect the end with the beginning
                            from = lbObjPath.positionList[numPositionsDrawPath - 1];
                            from.y += designerOffsetY;
                            to = lbObjPath.positionList[0];
                            to.y += designerOffsetY;
                            Gizmos.DrawLine(from, to);
                        }
                    }
                    #endregion

                    #region Draw Edge lines
                    if (lbObjPath.useWidth && numSplinePoints > 1)
                    {
                        // Assumes left and right edges have the same number of points and match number of centre spline points
                        Gizmos.color = lineColour;

                        #region Left Edge
                        from = lbObjPath.cachedSplinePointLeftEdgeList[0];
                        from.y += designerOffsetY;
                        #if OBJ_PATH_DEBUG_MODE
                        Gizmos.DrawSphere(from, 0.25f * lbObjPath.pointDisplayScale);
                        #endif

                        for (int i = 1; i < numSplinePoints; i++)
                        {
                            to = lbObjPath.cachedSplinePointLeftEdgeList[i];
                            to.y += designerOffsetY;
                            Gizmos.DrawLine(from, to);
                            #if OBJ_PATH_DEBUG_MODE
                            Gizmos.DrawSphere(to, 0.25f * lbObjPath.pointDisplayScale);
                            #endif
                            // Set the end of this line as the start of the next
                            from = to;
                        }

                        // Draw a line from the last location to the final left point.
                        to = lbObjPath.positionListLeftEdge[numPositionsDrawPath - 1];
                        to.y += designerOffsetY;
                        Gizmos.DrawLine(from, to);
                        #endregion

                        #region Right Edge
                        from = lbObjPath.cachedSplinePointRightEdgeList[0];
                        from.y += designerOffsetY;
                        #if OBJ_PATH_DEBUG_MODE
                        Gizmos.DrawSphere(from, 0.25f * lbObjPath.pointDisplayScale);
                        #endif
                        for (int i = 1; i < numSplinePoints; i++)
                        {
                            to = lbObjPath.cachedSplinePointRightEdgeList[i];
                            to.y += designerOffsetY;
                            Gizmos.DrawLine(from, to);
                            #if OBJ_PATH_DEBUG_MODE
                            Gizmos.DrawSphere(to, 0.25f * lbObjPath.pointDisplayScale);
                            #endif
                            // Set the end of this line as the start of the next
                            from = to;
                        }

                        // Draw a line from the last location to the final left point.
                        to = lbObjPath.positionListRightEdge[numPositionsDrawPath - 1];
                        to.y += designerOffsetY;
                        Gizmos.DrawLine(from, to);
                        #endregion

                        if (lbObjPath.showSurroundingInScene)
                        {
                            Gizmos.color = surroundColour;

                            #region Left Surroundings
                            from = lbObjPath.cachedSplinePointLeftSurroundList[0];
                            from.y += designerOffsetY;

                            #if OBJ_PATH_DEBUG_MODE
                            Gizmos.color = Color.black;
                            Gizmos.DrawSphere(from, 0.25f * lbObjPath.pointDisplayScale);
                            Gizmos.color = surroundColour;
                            #endif

                            // The number of spline points to optionally blend with the surrounds from the start and end if blendStart/End are enabled
                            int blendSplinePoints = Mathf.CeilToInt(lbObjPath.edgeBlendWidth / lbObjPath.pathResolution);

                            for (int i = 1; i < numSplinePoints; i++)
                            {
                                to = lbObjPath.cachedSplinePointLeftSurroundList[i];
                                to.y += designerOffsetY;
                                Gizmos.DrawLine(from, to);
                                #if OBJ_PATH_DEBUG_MODE
                                Gizmos.color = Color.black;
                                Gizmos.DrawSphere(to, 0.25f * lbObjPath.pointDisplayScale);
                                Gizmos.color = surroundColour;
                                #endif

                                if ((lbObjPath.blendStart && i <= blendSplinePoints) || (lbObjPath.blendEnd && i >= numSplinePoints - blendSplinePoints))
                                {
                                    // Draw all the way from the left surround edge to the right inner path edge
                                    // This will connect it to the right surround line which gets draw in the 
                                    // right surround section below. from is i-1.
                                    to2 = lbObjPath.cachedSplinePointRightEdgeList[i - 1];
                                    to2.y += designerOffsetY;
                                }
                                else
                                {
                                    // Connect the surround left outer edge with the (inner) edge of the path
                                    // from is i-1.
                                    to2 = lbObjPath.cachedSplinePointLeftEdgeList[i-1];
                                    to2.y += designerOffsetY;
                                }
                                Gizmos.DrawLine(from, to2);

                                // Set the end of this line as the start of the next
                                from = to;
                            }

                            // Draw end of left surrounding at end of path
                            if (lbObjPath.blendEnd)
                            {
                                // Draw all the way from the left surround edge to the right inner path edge
                                // This will connect it to the right surround line which gets draw in the 
                                // right surround section below. from is i-1.
                                to2 = lbObjPath.cachedSplinePointRightEdgeList[numSplinePoints - 1];
                                to2.y += designerOffsetY;
                            }
                            else
                            {
                                to2 = lbObjPath.cachedSplinePointLeftEdgeList[numSplinePoints - 1];
                                to2.y += designerOffsetY;
                            }
                            Gizmos.DrawLine(from, to2);

                            #endregion

                            #region Right Surroundings
                            from = lbObjPath.cachedSplinePointRightSurroundList[0];
                            from.y += designerOffsetY;
                            #if OBJ_PATH_DEBUG_MODE
                            Gizmos.DrawSphere(from, 0.25f * lbObjPath.pointDisplayScale);
                            #endif
                            for (int i = 1; i < numSplinePoints; i++)
                            {
                                to = lbObjPath.cachedSplinePointRightSurroundList[i];
                                to.y += designerOffsetY;
                                Gizmos.DrawLine(from, to);
                                #if OBJ_PATH_DEBUG_MODE
                                Gizmos.DrawSphere(to, 0.25f * lbObjPath.pointDisplayScale);
                                #endif

                                // Connect the surround right outer edge with the (inner) edge of the path
                                // from is i-1.
                                to2 = lbObjPath.cachedSplinePointRightEdgeList[i-1];
                                to2.y += designerOffsetY;
                                Gizmos.DrawLine(from, to2);

                                // Set the end of this line as the start of the next
                                from = to;
                            }

                            // Draw end of right surrounding at end of path
                            to2 = lbObjPath.cachedSplinePointRightEdgeList[numSplinePoints - 1];
                            to2.y += designerOffsetY;
                            Gizmos.DrawLine(from, to2);

                            #endregion
                        }

                        #region Draw ends
                        if (!lbObjPath.closedCircuit)
                        { 
                            if (!(lbObjPath.blendStart && lbObjPath.showSurroundingInScene))
                            {
                                Gizmos.color = lineColour;
                                from = lbObjPath.positionListLeftEdge[0];
                                from.y += designerOffsetY;
                                to = lbObjPath.positionListRightEdge[0];
                                to.y += designerOffsetY;
                                Gizmos.DrawLine(from, to);
                            }

                            if (!(lbObjPath.blendEnd && lbObjPath.showSurroundingInScene))
                            {
                                from = lbObjPath.positionListLeftEdge[numPositionsDrawPath - 1];
                                from.y += designerOffsetY;
                                to = lbObjPath.positionListRightEdge[numPositionsDrawPath - 1];
                                to.y += designerOffsetY;
                                Gizmos.DrawLine(from, to);
                            }
                        }
                        #endregion
                    }

                    #endregion
                }
            }
        }

        #endregion

        /// <summary>
        /// Attempt to zoom in on a selected object path point
        /// </summary>
        /// <param name="sceneView"></param>
        private void ZoomIn(SceneView sceneView)
        {
            int numSelected = lbObjPath.selectedList == null ? 0 : lbObjPath.selectedList.Count;

            if (sceneView != null && numSelected > 0 && lbObjPath.positionList.Count > numSelected)
            {
                Camera svCamera = sceneView.camera;
                if (svCamera != null)
                {
                    // Get the first selected point in the object path
                    Vector3 pos = lbObjPath.positionList[lbObjPath.selectedList[0]];

                    // Place the scene view camera 8m above the position of the objpath point
                    Vector3 pivotPosition = pos + (Vector3.up * 8f);
                    // Set the camera 10m infront of the object
                    pivotPosition.z -= 10f;
                    sceneView.pivot = pivotPosition;
                    sceneView.LookAt(pos, Quaternion.Euler(0f, 0f, 0f));
                }
            }
        }

        /// <summary>
        /// Add a new point to the path at the current mouse position
        /// </summary>
        /// <param name="evt"></param>
        private void AddPointAtMousePosition(Event evt)
        {
            Vector3 pos = Vector3.zero;

            if (lbGroup.showGroupDesigner)
            {
                if (lbGroupDesigner != null && lbGroupDesigner.grpBasePlaneTrfm != null)
                {
                    pos = LBEditorHelper.GetTransformPositionFromMouse(lbGroupDesigner.grpBasePlaneTrfm, evt.mousePosition, true);

                    // In Group Designer, Y should always start at 0 (then below, the heightAboveTerrain is added)
                    pos.y = 0f;
                }
            }
            else
            {
                // Get the on-ground terrain position from the mouse (this is in WorldSpace)
                pos = LBEditorHelper.GetLandscapePositionFromMouse(landscape, evt.mousePosition, false, true);
            }

            // If x and z is zero, it probably means the mouse is outside the bounds of the landscape or designer.
            if (pos.x != 0 || pos.z != 0) { pos.y += lbObjPath.heightAboveTerrain; }

            evt.Use();
            AppendPathPoint(landscape, lbGroup, lbGroupMember, lbObjPath, this, pos);
        }

        /// <summary>
        /// Insert a point at the first selected point in the path
        /// </summary>
        private void InsertPoint(bool isInsertAfter)
        {
            if (lbObjPath != null && lbObjPath.selectedList != null)
            {
                int numSelected = lbObjPath.selectedList.Count;
                int numPathPoints = lbObjPath.pathPointList == null ? 0 : lbObjPath.pathPointList.Count;

                if (numSelected > 0 && numPathPoints > 1)
                {
                    // Get the first selected point
                    int insertObjPathPointPos = lbObjPath.selectedList[0];

                    int newObjPathPointPos = isInsertAfter ? insertObjPathPointPos + 1 : insertObjPathPointPos;

                    lbObjPath.pathPointList.Insert(newObjPathPointPos, new LBPathPoint(lbObjPath.pathPointList[insertObjPathPointPos]));
                    lbObjPath.positionList.Insert(newObjPathPointPos, lbObjPath.positionList[insertObjPathPointPos]);
                    if (lbObjPath.useWidth)
                    {
                        lbObjPath.widthList.Insert(newObjPathPointPos, lbObjPath.widthList[insertObjPathPointPos]);
                        lbObjPath.positionListLeftEdge.Insert(newObjPathPointPos, lbObjPath.positionListLeftEdge[insertObjPathPointPos]);
                        lbObjPath.positionListRightEdge.Insert(newObjPathPointPos, lbObjPath.positionListRightEdge[insertObjPathPointPos]);
                        // TODO - add other left/right splines here
                    }

                    // Show the new duplicate, and hide the original
                    lbObjPath.pathPointList[insertObjPathPointPos].showInEditor = !isInsertAfter;
                    lbObjPath.pathPointList[insertObjPathPointPos + 1].showInEditor = isInsertAfter;

                    lbObjPath.selectedList.Clear();
                    lbObjPath.selectedList.Add(newObjPathPointPos);
                    // force lbPath spline cache refresh
                    lbObjPath.isSplinesCached2 = false;
                    lbObjPath.RefreshObjPathPositions(lbObjPath.showSurroundingInScene, false);
                    RefreshPath();
                }
            }
        }

        /// <summary>
        /// Snap selected points to the terrain
        /// </summary>
        private void SnapPointsToTerrain()
        {
            if (lbObjPath != null)
            {
                int numSelected = lbObjPath.selectedList == null ? 0 : lbObjPath.selectedList.Count;
                int numPathPoints = lbObjPath.pathPointList == null ? 0 : lbObjPath.pathPointList.Count;

                if (numSelected <= numPathPoints)
                {
                    bool isIgnoreTerrain = lbGroup.showGroupDesigner;

                    for (int i = 0; i < numSelected; i++)
                    {
                        posXZ.x = lbObjPath.positionList[lbObjPath.selectedList[i]].x;
                        posXZ.y = lbObjPath.positionList[lbObjPath.selectedList[i]].z;

                        snappedPos.x = posXZ.x;
                        snappedPos.z = posXZ.y;

                        // LBLandscapeTerrain.GetHeight() returns a normalised height
                        snappedPos.y = isIgnoreTerrain ? lbObjPath.heightAboveTerrain : LBLandscapeTerrain.GetHeight(landscape, posXZ, false) + lbObjPath.heightAboveTerrain + landscape.start.y;
                        lbObjPath.positionList[lbObjPath.selectedList[i]] = snappedPos;
                    }

                    // force lbPath spline cache refresh
                    lbObjPath.isSplinesCached2 = false;
                    lbObjPath.RefreshObjPathPositions(lbObjPath.showSurroundingInScene, false);
                    RefreshPath();
                }
            }
        }

        /// <summary>
        /// Delete selected points along an object path
        /// </summary>
        /// <param name="lbObjPathSelected"></param>
        private void DeleteSelectedPoints(LBObjPath lbObjPathSelected)
        {
            if (lbObjPathSelected != null && lbObjPathSelected.selectedList != null)
            {
                lbObjPathSelected.selectedList.Sort();
                int numSelected = lbObjPathSelected.selectedList.Count;
                int pointIdx = 0;
                // Loop backwards through the list of sorted, selected path index items
                for (int s = numSelected - 1; s >= 0; s--)
                {
                    pointIdx = lbObjPathSelected.selectedList[s];
                    lbObjPathSelected.pathPointList.RemoveAt(pointIdx);
                    lbObjPathSelected.positionList.RemoveAt(pointIdx);
                    if (lbObjPathSelected.useWidth)
                    {
                        lbObjPathSelected.widthList.RemoveAt(pointIdx);
                        lbObjPathSelected.positionListLeftEdge.RemoveAt(pointIdx);
                        lbObjPathSelected.positionListRightEdge.RemoveAt(pointIdx);
                        // TODO remove other left/right splines here
                    }
                }
                lbObjPathSelected.selectedList.Clear();
                // force lbPath spline cache refresh
                lbObjPathSelected.isSplinesCached2 = false;
                lbObjPathSelected.RefreshObjPathPositions(lbObjPathSelected.showSurroundingInScene, false);
                LBEditorHelper.RepaintLBW();
            }
        }

        /// <summary>
        /// Split the path at the first selected point.
        /// Cannot be performed at first or last point.
        /// </summary>
        private void SplitAtPoint()
        {
            if (lbObjPath != null)
            {
                int numSelected = lbObjPath.selectedList == null ? 0 : lbObjPath.selectedList.Count;
                int numPathPoints = lbObjPath.pathPointList == null ? 0 : lbObjPath.pathPointList.Count;

                if (numSelected <= numPathPoints)
                {
                    int firstSelected = lbObjPath.selectedList[0];

                    if (firstSelected == 0 || firstSelected == numPathPoints - 1)
                    {
                        EditorUtility.DisplayDialog("Split at Point", "Cannot split the path at the start or end points", "Got it!");
                    }
                    else if (numPathPoints < 3) { EditorUtility.DisplayDialog("Split at Point", "Cannot split a path with less than 3 points", "Got it!"); }
                    else
                    {
                        if (EditorUtility.DisplayDialog("Split at Point", "Do you wish to split the path at the selected point? There is no undo", "Sure, do it!", "CANCEL"))
                        {
                            lbObjPath.selectedList.Clear();

                            // Unselect any objects to prevent AddPrefab() being called
                            if (lbGroupDesigner != null) { Selection.activeObject = null; }

                            int groupMemberPos = lbGroup.groupMemberList.FindIndex(gmbr => gmbr.GUID == lbGroupMember.GUID);

                            if (groupMemberPos < 0) { Debug.LogWarning("ERROR: LBObjPathDesigner.SplitAtPoint could not find group member. PLEASE REPORT"); }
                            else
                            {
                                LBGroupMember groupMemberInserted = new LBGroupMember(lbGroupMember);

                                if (groupMemberInserted != null)
                                {
                                    groupMemberInserted.showObjPathDesigner = false;
                                    groupMemberInserted.showInEditor = false;
                                    groupMemberInserted.GUID = System.Guid.NewGuid().ToString();
                                    groupMemberInserted.lbObjPath.pathName += " (split)";
                                    groupMemberInserted.lbObjPath.pathPointColour = lbGroupMember.lbObjPath.pathPointColour;
                                    // Copy the randomiseRotationY from original as it gets set to true in Copy Constructor to solve some other issue...
                                    groupMemberInserted.randomiseRotationY = lbGroupMember.randomiseRotationY;

                                    // Remove the points before selected in new path
                                    for (int pIdx = 0; pIdx < firstSelected; pIdx++)
                                    {
                                        groupMemberInserted.lbObjPath.selectedList.Add(pIdx);
                                    }
                                    DeleteSelectedPoints(groupMemberInserted.lbObjPath);
                                    groupMemberInserted.lbObjPath.ResetPathPointGUIDs();

                                    // Remove the points after the selected in the original path
                                    for (int pIdx = firstSelected + 1; pIdx < numPathPoints; pIdx++)
                                    {
                                        lbObjPath.selectedList.Add(pIdx);
                                    }
                                    DeleteSelectedPoints(lbObjPath);

                                    // Insert the split path below the selected object path group member
                                    lbGroup.groupMemberList.Insert(groupMemberPos+1, groupMemberInserted);

                                    // Reselect the point that was split
                                    lbObjPath.selectedList.Add(firstSelected);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// If at least one user-defined point is selected, set all points in the list to the
        /// same height as the first one selected.
        /// </summary>
        /// <param name="promptUser"></param>
        private void SetAllHeightsToSelected(bool promptUser)
        {
            if (lbObjPath != null)
            {
                int numSelected = lbObjPath.selectedList == null ? 0 : lbObjPath.selectedList.Count;
                int numPathPoints = lbObjPath.pathPointList == null ? 0 : lbObjPath.pathPointList.Count;

                if (numSelected > 0 && numSelected <= numPathPoints)
                {
                    float selectedHeight = lbObjPath.positionList[lbObjPath.selectedList[0]].y;
                    Vector3 positionPoint;

                    bool isContinue = true;
                    if (promptUser) { isContinue = LBEditorHelper.PromptForContinue("Set All Path Heights", "Do you want to set all path heights to " + selectedHeight + "?\n\n There is NO UNDO."); }

                    if (isContinue)
                    {
                        for (int pIdx = 0; pIdx < numPathPoints; pIdx++)
                        {
                            positionPoint = lbObjPath.positionList[pIdx];
                            positionPoint.y = selectedHeight;
                            lbObjPath.positionList[pIdx] = positionPoint;
                        }

                        // Turn off snap to terrain if it is on.
                        lbObjPath.snapToTerrain = false;

                        // force lbPath spline cache refresh
                        lbObjPath.isSplinesCached2 = false;
                        lbObjPath.RefreshObjPathPositions(lbObjPath.showSurroundingInScene, false);
                        RefreshPath();
                        LBEditorHelper.RepaintLBW();
                    }
                }
            }
        }

        /// <summary>
        /// Set all heights along the path to a valueType of:
        /// 0: the lowest or minimum point (This could be useful for a river).
        /// 1: the highest or maximum point
        /// 2: the average point
        /// </summary>
        /// <param name="promptUser"></param>
        private void SetAllHeightsToValue(bool promptUser, int valueType)
        {
            if (lbObjPath != null)
            {
                int numSelected = lbObjPath.selectedList == null ? 0 : lbObjPath.selectedList.Count;
                int numPathPoints = lbObjPath.pathPointList == null ? 0 : lbObjPath.pathPointList.Count;

                if (numSelected > 0 && numSelected <= numPathPoints)
                {
                    float selectedHeight;
                    string promptTitle = string.Empty;

                    if (valueType == 0) { selectedHeight = lbObjPath.positionList.Min(pt => pt.y); promptTitle = "Set All Path Heights To Minimum"; }
                    else if (valueType == 1) { selectedHeight = lbObjPath.positionList.Max(pt => pt.y); }
                    else { selectedHeight = lbObjPath.positionList.Average(pt => pt.y); }

                    Vector3 positionPoint;

                    bool isContinue = true;
                    if (promptUser)
                    {
                        if (valueType == 0) { promptTitle = "Set All Path Heights To Minimum"; }
                        else if (valueType == 1) { promptTitle = "Set All Path Heights To Maximum"; }
                        else { promptTitle = "Set All Path Heights To Average"; }

                        isContinue = LBEditorHelper.PromptForContinue(promptTitle, "Do you want to set all path heights to " + selectedHeight + "?\n\n There is NO UNDO.");
                    }

                    if (isContinue)
                    {
                        for (int pIdx = 0; pIdx < numPathPoints; pIdx++)
                        {
                            positionPoint = lbObjPath.positionList[pIdx];
                            positionPoint.y = selectedHeight;
                            lbObjPath.positionList[pIdx] = positionPoint;
                        }

                        // Turn off snap to terrain if it is on.
                        lbObjPath.snapToTerrain = false;

                        // force lbPath spline cache refresh
                        lbObjPath.isSplinesCached2 = false;
                        lbObjPath.RefreshObjPathPositions(lbObjPath.showSurroundingInScene, false);
                        RefreshPath();
                        LBEditorHelper.RepaintLBW();
                    }
                }
            }
        }

        #endregion

        #region Public Non-Static Methods

        public void RefreshPath()
        {
            if (isInitialised)
            {
                SceneView.RepaintAll();
            }
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Show or close the Object Path Designer.
        /// If opening in Procedural or Manual clearing Group, also set lbGroupDesigner, else pass in null.
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="lbObjPathDesigner"></param>
        /// <param name="lbGroup"></param>
        /// <param name="lbGroupMember"></param>
        /// <param name="lbGroupDesigner"></param>
        /// <param name="isShown"></param>
        /// <returns></returns>
        public static bool ShowDesigner(LBLandscape landscape, ref LBObjPathDesigner lbObjPathDesigner, LBGroup lbGroup, LBGroupMember lbGroupMember, LBGroupDesigner lbGroupDesigner, bool isShown)
        {
            bool isSuccessful = false;
            string methodName = "LBObjPathDesigner.ShowDesigner";

            if (landscape == null) { Debug.LogWarning("ERROR: " + methodName + " - landscape cannot be null. Please Report"); return false; }
            else if (landscape.lbGroupList == null) { Debug.LogWarning("ERROR: " + methodName + " - lbGroupList cannot be null. Please Report"); return false; }
            else if (lbGroup == null) { Debug.LogWarning("ERROR: " + methodName + " - lbGroup cannot be null. Please Report"); }
            else if (lbGroupMember == null) { Debug.LogWarning("ERROR: " + methodName + " - lbGroupMember cannot be null. Please Report"); }
            else if (lbGroupMember.lbMemberType != LBGroupMember.LBMemberType.ObjPath) { Debug.LogWarning("ERROR: " + methodName + " - lbGroupMember must be of type ObjPath. Please Report"); }
            else if (lbGroupMember.lbObjPath == null) { Debug.LogWarning("ERROR: " + methodName + " - lbGroupMember.lbObjPath cannot be null. Please Report"); }
            else
            {
                bool isGroupDesignerShown = (lbGroupDesigner != null) && lbGroup.lbGroupType != LBGroup.LBGroupType.Uniform;

                if (isShown)
                {
                    bool isGroupTypeUniform = (lbGroup.lbGroupType == LBGroup.LBGroupType.Uniform);

                    // Assume user will not switch in/out of GPU for Path when ObjPathDesigner is enabled.
                    if (isGroupTypeUniform && landscape.useGPUPath && lbGroupMember.lbObjPath.useWidth)
                    {
                        // Create a restore point and apply current Topography tab settings
                        InitialiseHeightmapForDesigner(landscape, lbGroupMember);

                        // Create a restore point and apply current Texturing tab settings
                        // In LB 2.2.0 we need to take into consideration subgroups that could modify ground texturing
                        if (LBGroup.IsApplyObjPathTexturesPresent(landscape.lbGroupList, lbGroup, lbGroupMember.lbObjPath))
                        {
                            InitialiseSplatMapsForDesigner(landscape, lbGroupMember);
                        }

                        // Create a restore point and apply current Trees tab settings
                        if (lbGroupMember.lbObjPath.isRemoveExistingTrees)
                        {
                            InitialiseTreesForDesigner(landscape, lbGroupMember);
                        }
                    }

                    // Only switch to scene view when opening the ObjPathDesigner
                    LBEditorHelper.ShowSceneView(typeof(LBObjPathDesigner));

                    // If not being used with the Group Designer, lock the terrains to prevent accidental movement
                    if (isGroupTypeUniform) { landscape.LockTerrains(true); }

                    lbObjPathDesigner = LBObjPathDesigner.CreateDesigner(landscape, lbGroup, lbGroupMember, lbGroupDesigner);
                    if (lbObjPathDesigner != null)
                    {
                        if (isGroupDesignerShown)
                        {
                            lbGroup.GroupMemberListExpand(false);
                            lbGroupMember.showInEditor = true;
                        }
                        else
                        {
                            // If not in the GroupDesigner, disable existing path prefabs in the scene
                            LBLandscapeTerrain.EnableExistingPrefabs(landscape, LBPrefabItem.PrefabItemType.ObjPathPrefab, false, lbGroupMember.GUID);
                        }
                        // Switch to the Move tool
                        Tools.current = Tool.Move;
                        isSuccessful = true;
                    }
                    else { Debug.LogWarning("ERROR: " + methodName + " could not create designer in scene. Please Report."); }
                }
                else
                {
                    // Remove all the temporary Object Path Designers items from the scene.
                    // There should be only 1 but just in case check for multiple.
                    LBObjPathDesigner.DeleteDesigners(landscape);

                    // If not being used with the Group Designer, re-enable ObjPath prefabs, and unlock the terrains
                    if (lbGroup.lbGroupType == LBGroup.LBGroupType.Uniform && lbGroupMember != null)
                    {
                        LBLandscapeTerrain.EnableExistingPrefabs(landscape, LBPrefabItem.PrefabItemType.ObjPathPrefab, true, lbGroupMember.GUID);
                        landscape.LockTerrains(false);
                    }

                    // If the GroupDesigner was enabled AND this ObjPathDesigner was enabled, tell the GroupDesigner the
                    // ObjPathDesigner has been turned off.
                    if (lbObjPathDesigner != null && lbObjPathDesigner.lbGroupDesigner != null && lbObjPathDesigner.isInGroupDesignerMode)
                    {
                        lbObjPathDesigner.lbGroupDesigner.isObjDesignerEnabled = false;
                        lbObjPathDesigner.lbGroupDesigner.isShowPrefabWarning = true;
                    }

                    lbGroupMember.showObjPathDesigner = false;
                    lbObjPathDesigner = null;
                    isSuccessful = true;
                }
            }

            return isSuccessful;
        }

        /// <summary>
        /// Typically you should be using ShowDesigner(...) with isShown parameter set to false.
        /// This is a simplier CloseDesigner method that unlike ShowDesigner, does not require a reference to an instance of LBObjPathDesigner.
        /// This allows it to be called from an instance of LBObjPathDesigner. It does minimal error checking.
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="lbObjPathDesigner"></param>
        /// <param name="lbGroup"></param>
        /// <param name="lbGroupMember"></param>
        /// <param name="lbGroupDesigner"></param>
        /// <returns></returns>
        public static void CloseDesigner(LBLandscape landscape, LBObjPathDesigner lbObjPathDesigner, LBGroup lbGroup, LBGroupMember lbGroupMember, LBGroupDesigner lbGroupDesigner)
        {
            string methodName = "LBObjPathDesigner.CloseDesigner";

            if (landscape == null) { Debug.LogWarning("ERROR: " + methodName + " - landscape cannot be null. Please Report"); }
            else if (lbGroup != null && lbGroupMember != null && lbObjPathDesigner != null)
            {
                // Remove all the temporary Object Path Designers items from the scene.
                // There should be only 1 but just in case check for multiple.
                LBObjPathDesigner.DeleteDesigners(landscape);

                // If not being used with the Group Designer, re-enable ObjPath prefabs, and unlock the terrains
                if (lbGroup.lbGroupType == LBGroup.LBGroupType.Uniform && lbGroupMember != null)
                {
                    LBLandscapeTerrain.EnableExistingPrefabs(landscape, LBPrefabItem.PrefabItemType.ObjPathPrefab, true, lbGroupMember.GUID);
                    LBLandscapeTerrain.RemoveExistingPrefabs(landscape, true, LBPrefabItem.PrefabItemType.ObjPathDesignerPrefab, null);

                    // Restore the heightmap to how it was before entering ObjPath Editor mode
                    if (landscape.useGPUPath && lbGroupMember.lbObjPath != null && lbGroupMember.lbObjPath.useWidth)
                    {
                        EditorUtility.DisplayProgressBar("Restoring heightmap", "PLEASE WAIT", 0.1f);
                        landscape.RevertHeightmap1D("RP_" + lbGroupMember.GUID, true, true);

                        if (!string.IsNullOrEmpty(lbGroupMember.lbObjPath.coreTextureGUID) || !string.IsNullOrEmpty(lbGroupMember.lbObjPath.surroundTextureGUID))
                        {
                            EditorUtility.DisplayProgressBar("Restoring splatmaps", "PLEASE WAIT", 0.15f);
                            landscape.RevertTextures1D("RP_" + lbGroupMember.GUID, true);
                        }

                        if (lbGroupMember.lbObjPath.isRemoveExistingTrees)
                        {
                            EditorUtility.DisplayProgressBar("Restoring Unity terrain trees", "PLEASE WAIT", 0.2f);
                            landscape.RevertTrees1D("RP_" + lbGroupMember.GUID, true);
                        }

                        EditorUtility.ClearProgressBar();
                    }

                    landscape.LockTerrains(false);
                }

                // If the GroupDesigner was enabled AND this ObjPathDesigner was enabled, tell the GroupDesigner the
                // ObjPathDesigner has been turned off.
                if (lbObjPathDesigner.lbGroupDesigner != null && lbObjPathDesigner.isInGroupDesignerMode)
                {
                    lbObjPathDesigner.lbGroupDesigner.isObjDesignerEnabled = false;
                    lbObjPathDesigner.lbGroupDesigner.isShowPrefabWarning = true;
                }

                lbGroupMember.showObjPathDesigner = false;
                LBEditorHelper.RepaintLBW();
                lbObjPathDesigner = null;
            }
        }

        /// <summary>
        /// Create a new Object Path Designer and return a reference to the LBObjPathDesigner
        /// script attached to the gameobject. Create it under the landscape supplied
        /// </summary>
        /// <param name="landscape"></param>
        /// <returns></returns>
        public static LBObjPathDesigner CreateDesigner(LBLandscape landscape, LBGroup lbGroup, LBGroupMember lbGroupMember, LBGroupDesigner lbGroupDesigner)
        {
            LBObjPathDesigner lbObjPathDesigner = null;
            string methodName = "LBObjPathDesigner.CreateDesigner";

            if (landscape == null) { Debug.LogWarning("ERROR: " + methodName + " - landscape cannot be null. Please Report"); }
            else if (lbGroup == null) { Debug.LogWarning("ERROR: " + methodName + " - lbGroup cannot be null. Please Report"); }
            else if (lbGroupMember == null) { Debug.LogWarning("ERROR: " + methodName + " - lbGroupMember cannot be null. Please Report"); }
            else if (lbGroupMember.lbMemberType != LBGroupMember.LBMemberType.ObjPath) { Debug.LogWarning("ERROR: " + methodName + " - lbGroupMember must be of type ObjPath. Please Report"); }
            else if (lbGroupMember.lbObjPath == null) { Debug.LogWarning("ERROR: " + methodName + " - lbGroupMember.lbObjPath cannot be null. Please Report"); }
            else
            {
                // Find the first LBObjPathDesigner script attached to a child gameobject (there should be none or one)
                lbObjPathDesigner = (LBObjPathDesigner)landscape.gameObject.GetComponentInChildren(typeof(LBObjPathDesigner), true);

                if (lbObjPathDesigner != null)
                {
                    // Remove any existing designers
                    DeleteDesigners(landscape);
                }

                GameObject go = new GameObject("LBObjectPathDesigner");

                if (go == null) { Debug.LogWarning("ERROR: " + methodName + " - could not create new gameobject in scene for the Group Designer. Please Report"); }
                else
                {
                    go.transform.SetParent(landscape.transform);
                    lbObjPathDesigner = go.AddComponent<LBObjPathDesigner>();
                    if (lbObjPathDesigner != null)
                    {
                        // Prevent user from editing or deleting Object Path Designer
                        go.hideFlags = go.hideFlags | HideFlags.NotEditable;

                        lbObjPathDesigner.lbGroup = lbGroup;
                        lbObjPathDesigner.lbGroupMember = lbGroupMember;
                        lbObjPathDesigner.lbObjPath = lbGroupMember.lbObjPath;
                        lbObjPathDesigner.landscape = landscape;

                        // Get a reference to the currently open GroupDesigner (if any) - Only applies to Procedural or Manual Clearings
                        if (lbGroupDesigner != null && lbGroup.lbGroupType != LBGroup.LBGroupType.Uniform)
                        {
                            if (lbGroupDesigner.isInitialised)
                            {
                                lbObjPathDesigner.lbGroupDesigner = lbGroupDesigner;
                                // The group designer is enabled and initialised for the Group that contains the ObjPath being edited
                                lbObjPathDesigner.isInGroupDesignerMode = true;
                                lbGroupDesigner.isObjDesignerEnabled = true;
                                lbGroupDesigner.isShowPrefabWarning = true;
                            }
                        }

                        lbObjPathDesigner.Initialise();
                    }
                }
            }

            return lbObjPathDesigner;
        }

        /// <summary>
        /// Delete any Object Path Designers which are children of a landscape
        /// </summary>
        /// <param name="landscape"></param>
        public static void DeleteDesigners(LBLandscape landscape)
        {
            string methodName = "LBObjPathDesigner.DeleteDesigners";
            if (landscape == null) { Debug.LogWarning("ERROR: " + methodName + " - landscape cannot be null. Please Report"); }
            else
            {
                // Find any Object Path Designers in the scene that are children of the landscape
                List<LBObjPathDesigner> itemList = new List<LBObjPathDesigner>(landscape.GetComponentsInChildren<LBObjPathDesigner>());
                if (itemList != null)
                {
                    // Loop backwards through the Object Path Designers and delete them from the scene
                    for (int item = itemList.Count - 1; item >= 0; item--)
                    {
                        if (itemList[item] != null)
                        {
                            //itemList[item].ClearAllObjects(true);
                            DestroyImmediate(itemList[item].gameObject);
                        }
                    }
                    itemList.Clear();
                    itemList = null;
                }
            }
        }

        #region AppendPathPoint

        /// <summary>
        /// Add a point to the end of the supplied object path.
        /// See also AddPointAtMousePosition()
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="lbGroupOwner"></param>
        /// <param name="lbGroupMemberOwner"></param>
        /// <param name="lbObjPathAppend"></param>
        /// <param name="pointToAdd"></param>
        public static void AppendPathPoint(LBLandscape landscape, LBGroup lbGroupOwner, LBGroupMember lbGroupMemberOwner, LBObjPath lbObjPathAppend, LBObjPathDesigner lbObjPathDesigner, Vector3 pointToAdd)
        {
            string methodName = "LBObjPathDesigner.AppendPathPoint";

            // Validate basics and get out early if there are issues
            if (lbObjPathAppend == null) { Debug.LogWarning("ERROR: " + methodName + " LBObjPath is null"); return; }
            else if (landscape == null) { Debug.LogWarning("ERROR: " + methodName + " landscape is null"); return; }
            else if (lbGroupOwner == null) { Debug.LogWarning("ERROR: " + methodName + " lbGroupOwner is null"); return; }

            LBPathPoint lbPathPointToAdd = null;
            lbGroupMemberOwner.ObjPathPointsExpand(false);

            bool pointToAddZero = (pointToAdd.x == 0f && pointToAdd.y == 0f && pointToAdd.z == 0f);

            if (!pointToAddZero || lbObjPathAppend.positionList.Count > 0)
            {
                int currentLastItem = lbObjPathAppend.positionList.Count - 1;
               
                // When another position is added, by default, make it a duplicate of the previous one if the point given is at 0,0,0
                if (pointToAddZero)
                {
                    lbPathPointToAdd = new LBPathPoint(lbObjPathAppend.pathPointList[currentLastItem]);
                    if (lbPathPointToAdd != null)
                    {
                        lbObjPathAppend.pathPointList.Add(lbPathPointToAdd);
                        lbObjPathAppend.positionList.Add(lbObjPathAppend.positionList[currentLastItem]);
                        if (lbObjPathAppend.useWidth)
                        {
                            lbObjPathAppend.widthList.Add(lbObjPathAppend.widthList[currentLastItem]);
                            lbObjPathAppend.positionListLeftEdge.Add(lbObjPathAppend.positionListLeftEdge[currentLastItem]);
                            lbObjPathAppend.positionListRightEdge.Add(lbObjPathAppend.positionListRightEdge[currentLastItem]);
                            // TODO add other left/right splines here
                        }
                        lbPathPointToAdd.showInEditor = true;
                    }
                }
                else
                {
                    // If a location was indicated on the landscape, add that one. This typically occurs when the
                    // user clicks the '+' key when the mouse is over the scene view.

                    lbPathPointToAdd = new LBPathPoint();
                    if (lbPathPointToAdd != null)
                    {
                        lbObjPathAppend.pathPointList.Add(lbPathPointToAdd);
                        lbObjPathAppend.positionList.Add(pointToAdd);
                        if (lbObjPathAppend.useWidth)
                        {
                            // Add width of last point, or set to default if this is the first point
                            lbObjPathAppend.widthList.Add(currentLastItem >= 0 ? lbObjPathAppend.widthList[currentLastItem] : LBPath.GetDefaultPathWidth);
                            lbObjPathAppend.positionListLeftEdge.Add(pointToAdd);
                            lbObjPathAppend.positionListRightEdge.Add(pointToAdd);
                            // TODO add other left/right splines here
                        }

                        lbPathPointToAdd.showInEditor = true;
                    }
                }

                // Clear the selection list and add this new path point
                lbObjPathAppend.selectedList.Clear();
                lbObjPathAppend.selectedList.Add(currentLastItem + 1);

                // Only zoom and change scene view camera if zoom is enabled AND we're NOT in the Group Designer
                if (lbObjPathAppend.zoomOnFind && lbGroupOwner.lbGroupType == LBGroup.LBGroupType.Uniform)
                {
                    LBEditorHelper.PositionSceneView(lbObjPathAppend.positionList[currentLastItem + 1], lbObjPathAppend.findZoomDistance, typeof(LBObjPathDesigner));
                }
            }
            else
            {
                Vector3 firstPointCentre = Vector3.zero;

                if (landscape == null) { Debug.LogWarning("AppendPathPoint - cannot find landscape"); }
                // Add the first 
                else if (lbGroupMemberOwner.lbMemberType == LBGroupMember.LBMemberType.ObjPath)
                {
                    // lbObjPathDesigner != null && lbObjPathDesigner.isInGroupDesignerMode

                    lbPathPointToAdd = new LBPathPoint();
                    if (lbPathPointToAdd != null)
                    {
                        lbObjPathAppend.pathPointList.Add(lbPathPointToAdd);
                        lbObjPathAppend.positionList.Add(firstPointCentre);
                        if (lbObjPathAppend.useWidth)
                        {
                            lbObjPathAppend.widthList.Add(LBPath.GetDefaultPathWidth);
                            lbObjPathAppend.positionListLeftEdge.Add(firstPointCentre);
                            lbObjPathAppend.positionListRightEdge.Add(firstPointCentre);
                            // TODO add other left/right splines here
                        }

                        // Should prob get Y but needs to check if in GroupDesigner AND if it is within bounds

                        // Clear the selection list and add this new path point
                        lbObjPathAppend.selectedList.Clear();
                        lbObjPathAppend.selectedList.Add(0);
                        lbPathPointToAdd.showInEditor = true;
                    }
                }
                else
                {
                    // If possible, place the first point on the landscape in the centre of the sceneview
                    firstPointCentre = LBEditorHelper.GetCentreSceneView(typeof(LBObjPathDesigner));

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

                    firstPointCentre.y = LBLandscapeTerrain.GetHeight(landscape, firstPointXZ, false) + lbObjPathAppend.heightAboveTerrain + landscape.start.y;

                    lbPathPointToAdd = new LBPathPoint();
                    if (lbPathPointToAdd != null)
                    {
                        lbObjPathAppend.pathPointList.Add(lbPathPointToAdd);
                        lbObjPathAppend.positionList.Add(firstPointCentre);
                        if (lbObjPathAppend.useWidth)
                        {
                            lbObjPathAppend.widthList.Add(LBPath.GetDefaultPathWidth);
                            lbObjPathAppend.positionListLeftEdge.Add(firstPointCentre);
                            lbObjPathAppend.positionListRightEdge.Add(firstPointCentre);
                            // TODO add other left/right splines here
                        }

                        // Clear the selection list and add this new path point
                        lbObjPathAppend.selectedList.Clear();
                        lbObjPathAppend.selectedList.Add(0);

                        // Only zoom and change scene view camera if we're NOT in the Group Designer
                        if (lbGroupOwner.lbGroupType == LBGroup.LBGroupType.Uniform)
                        {
                            LBEditorHelper.PositionSceneView(lbObjPathAppend.positionList[0], lbObjPathAppend.findZoomDistance, typeof(LBObjPathDesigner));
                        }
                        lbPathPointToAdd.showInEditor = true;
                    }
                }
            }

            // Force lbPath spline cache update
            lbObjPathAppend.isSplinesCached2 = false;
            // Update splines
            lbObjPathAppend.RefreshObjPathPositions(lbObjPathAppend.showSurroundingInScene, false);
        }

        #endregion

        /// <summary>
        /// This enables the ObjectPathDesigner modify the topography without the previous path topography
        /// changes being present in the landscape. Currently it only applies to UNIFORM groups.
        /// 1. Create restore point of heightmap
        /// 2. Apply the current Topography tab settings
        /// 3. Create a temp restore point to allow multiple interations of the path topography changes
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="lbGroupMember"></param>
        public static void InitialiseHeightmapForDesigner(LBLandscape landscape, LBGroupMember lbGroupMember)
        {
            // Create restore point of heightmap
            EditorUtility.DisplayProgressBar("Creating restore point", "Backing up heightmap, PLEASE WAIT", 0.1f);
            landscape.SaveHeightmap1D("RP_" + lbGroupMember.GUID, true);

            // Somehow remove the existing ObjPath topography...
            // Currently we just apply the current Topography tab settings. See note below.
            // Doesn't perform any backups
            EditorUtility.DisplayProgressBar("Creating restore point", "Applying Topography tab settings, PLEASE WAIT", 0.3f);
            landscape.ApplyTopography(false, true);

            // NOTE: If we were to apply any of the other topography-modifying Groups or Group Members to display a more
            // accurate landscape in ObjPath Designer mode, here is where we'd do it...

            // This will be used to restore the previous unmodified Topography before applying the Object Path
            EditorUtility.DisplayProgressBar("Creating restore point", "Almost ready...", 0.8f);
            landscape.SaveHeightmap1D("ObjPathDesigner", true);
            EditorUtility.ClearProgressBar();
        }

        /// <summary>
        /// This enables the ObjectPathDesigner to modify the terrain texturing without the previous path texturing
        /// changes being present in the landscape. Currently it only applies to UNIFORM groups.
        /// 1. Create restore point of splatmaps
        /// 2. Apply the current Texturing tab settings
        /// 3. Create a temp restore point to allow multiple interations of the ObjPath texturing changes
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="lbGroupMember"></param>
        public static void InitialiseSplatMapsForDesigner(LBLandscape landscape, LBGroupMember lbGroupMember)
        {
            // Create restore point of splatmaps
            EditorUtility.DisplayProgressBar("Creating restore point", "Backing up splatmaps, PLEASE WAIT", 0.1f);
            landscape.SaveTextures1D("RP_" + lbGroupMember.GUID, true);

            // Remove the existing ObjPath texturing...
            // Currently we just apply the current Texturing tab settings. See note below.
            // Doesn't perform any backups
            EditorUtility.DisplayProgressBar("Creating restore point", "Applying Texturing tab settings, PLEASE WAIT", 0.3f);
            landscape.ApplyTextures(false, true);

            // NOTE: If we were to apply any of the other texturing-modifying Groups or Group Members to display a more
            // accurate landscape in ObjPath Designer mode, here is where we'd do it...

            // This will be used to restore the previous unmodified Texturing before applying the Object Path
            EditorUtility.DisplayProgressBar("Creating restore point", "Almost ready...", 0.8f);
            landscape.SaveTextures1D("ObjPathDesigner", true);
            EditorUtility.ClearProgressBar();
        }

        /// <summary>
        /// This enables the ObjectPathDesigner modify the Unity terrain trees without the previous path trees
        /// changes being present in the landscape. Currently it only applies to UNIFORM groups.
        /// 1. Create restore point of trees
        /// 2. Apply the current Trees tab settings
        /// 3. Create a temp restore point to allow multiple interations of the ObjPath trees changes
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="lbGroupMember"></param>
        public static void InitialiseTreesForDesigner(LBLandscape landscape, LBGroupMember lbGroupMember)
        {
            // Create restore point of splatmaps
            EditorUtility.DisplayProgressBar("Creating restore point", "Backing up Unity terrain trees, PLEASE WAIT", 0.1f);
            landscape.SaveTrees1D("RP_" + lbGroupMember.GUID, true);

            // Remove the existing ObjPath trees...
            // Currently we just apply the current Trees tab settings. See note below.
            // Doesn't perform any backups
            EditorUtility.DisplayProgressBar("Creating restore point", "Applying Trees tab settings, PLEASE WAIT", 0.3f);
            landscape.ApplyTrees(false, true);

            // NOTE: If we were to apply any of the other Unity terrain tree-modifying Groups or Group Members to display a more
            // accurate landscape in ObjPath Designer mode, here is where we'd do it...

            // This will be used to restore the previous unmodified Unity terrain Trees before applying the Object Path
            EditorUtility.DisplayProgressBar("Creating restore point", "Almost ready...", 0.8f);
            landscape.SaveTrees1D("ObjPathDesigner", true);
            EditorUtility.ClearProgressBar();
        }

        #endregion
    }
}
#endif