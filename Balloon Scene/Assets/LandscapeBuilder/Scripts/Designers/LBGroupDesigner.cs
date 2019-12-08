#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace LandscapeBuilder
{
    [ExecuteInEditMode]
    /// <summary>
    /// Helper class to be added to a temporary gameobject
    /// under the landscape gameobject. It holds information
    /// about the Group Designer including original scene
    /// camera setup. 
    /// </summary>
    public class LBGroupDesigner : MonoBehaviour
    {
        #region Public Variables

        // remember current scene setup
        [HideInInspector] public Vector3 origSceneViewPivot;
        [HideInInspector] public Quaternion origSceneViewRotation;
        [HideInInspector] public Vector3 origSceneViewCameraPosition;
        [HideInInspector] public Quaternion origSceneViewCameraRotation;
        [HideInInspector] public Vector3 origSceneViewCameraLocalScale;
        [HideInInspector] public bool origSceneViewIsOrthographic = false;
        [HideInInspector] public float origSceneViewSize = 10f;
        [HideInInspector] public bool origSceneViewShowSelectionOutline = false;
        [HideInInspector] public bool isUnityFogOn = false;

        [HideInInspector] public bool isInitialised = false;
        [HideInInspector] public Transform grpBaseTrfm = null;
        [HideInInspector] public Transform cameraPlaneTrfm = null;
        [HideInInspector] public Transform grpBasePlaneTrfm = null;
        [HideInInspector] public Vector3 grpBaseTrfmPosition;
        [HideInInspector] public Vector2 grpBasePlaneCentre2D = Vector2.zero;
        [HideInInspector] public LBGroup lbGroup = null;

        // Place above the landscape (previously was -2000)
        // Above avoids shadow issues but could create other issues with tall landscapes...
        [HideInInspector] public float BasePlaneOffsetY { get { return 10000; } }

        /// <summary>
        /// Specifies the size of the cube that will restrict where the camera can move to
        /// </summary>
        [HideInInspector] public float cubeLimitsSize = 10f;

        [HideInInspector] public bool autoSaveEnabled = false;
        [HideInInspector] public bool isCheckProximity = true;

        // Display options
        [HideInInspector] public bool showProximity = true;
        [HideInInspector] public bool showGroupExtent = true;
        [HideInInspector] public bool showTreeProximity = false;
        [HideInInspector] public bool showZones = false;
        [HideInInspector] public bool showFlattenArea = false;
        [HideInInspector] public bool showSubGroupExtent = false;

        // ObjPathDesigner variables
        [HideInInspector] public bool isObjDesignerEnabled = false;
        // When the Object Path Designer is enabled a warning is shown (once) about not dragging in Prefabs
        [HideInInspector] public bool isShowPrefabWarning = true;

        public bool GetAutoRefresh { get { return autoRefreshGroupDesigner; } }
        public bool IsRefreshingWorkspace { get { return isRefreshingWorkspace; } }

        #endregion

        #region Private variables

        //private Camera sceneViewCamera = null;

        private UnityEditor.SceneView sceneView = null;
        private UnityEngine.Color backgroundColour = new Color(49f / 255f, 77f / 255f, 121f / 255f, 0.1f);
        //private UnityEngine.Color zoneLineColour = Color.cyan;
        private UnityEngine.Color zoneLabelColour = new Color(47f / 255f, 79f / 255f, 79f / 255f, 1.0f); // Dark Slate Gray
        private GUIStyle zoneLabelStyle;
        private List<int> zoneSelectedList = null;

        private bool isAddingPrefab = false;
        private Vector3 previousZoneMovePosition = Vector3.zero;

        private float lastAddPrefabTime = float.PositiveInfinity;
        private float blockAddPrefabUntilTime = 0f;
        private bool isRefreshingWorkspace = false;

        private bool autoRefreshGroupDesigner = true;
        private LBRandom lbRandomPrefabXZRotation = null;

        #endregion

        #region Intialise Methods

        private void Initialise()
        {
            string methodName = "LBGroupDesigner.Initialise";

            System.Type typePgbr = null;

            try
            {
                // Remember the scene view camera settings
                sceneView = UnityEditor.SceneView.lastActiveSceneView;
                if (sceneView != null)
                {
                    Camera sceneViewCamera = sceneView.camera;

                    if (sceneViewCamera == null) { Debug.LogWarning("ERROR: " + methodName + " - cannot find scene view camera."); }
                    else
                    {
                        // Remember the scene view and camera settings
                        origSceneViewPivot = sceneView.pivot;
                        origSceneViewRotation = sceneView.rotation;
                        origSceneViewCameraPosition = sceneViewCamera.transform.position;
                        origSceneViewCameraRotation = sceneViewCamera.transform.rotation;
                        origSceneViewCameraLocalScale = sceneViewCamera.transform.localScale;
                        origSceneViewIsOrthographic = sceneView.orthographic;
                        origSceneViewSize = sceneView.size;

                        // Remember the scene view gizmo settings
                        origSceneViewShowSelectionOutline = LBEditorHelper.GizmoShowSelectionOutline;

                        // Is Unity Fog on?
                        isUnityFogOn = RenderSettings.fog;

                        // Set up Group Base Plane before changing scene view camera settings
                        Transform landscapeTfrm = this.transform.parent;
                        if (landscapeTfrm == null)
                        {
                            Debug.LogWarning("ERROR: " + methodName + " - could not find parent landscape gameobject. Please Report.");
                            DestroyImmediate(this.gameObject);
                        }
                        else
                        {
                            typePgbr = LBEditorHelper.GetEditorProgressBarType();
                            if (typePgbr != null) { LBEditorHelper.ShowEditorProgressbar(typePgbr, "Initialising...", 0.1f); }

                            CreateGroupBase();
                            CreateBasePlane();
                            CreateCeiling();
                            InitialiseZones();

                            CreateCameraPlane(landscapeTfrm);

                            // Always turn it off
                            RenderSettings.fog = false;

                            // Set up the front view
                            sceneView.orthographic = false;
                            sceneView.pivot = new Vector3(0f, BasePlaneOffsetY, 0f);
                            sceneView.rotation = Quaternion.Euler(0f, 0f, 0f);
                            sceneView.size = 5f;

                            // Lock the rotation
                            //sceneView.isRotationLocked = true;                

                            isCheckProximity = true;

                            zoneSelectedList = new List<int>(20);

                            // Load all the current group members into the designer
                            if (typePgbr != null) { LBEditorHelper.ShowEditorProgressbar(typePgbr, "Refreshing...", 0.2f); }

                            RefreshObjects();

                            autoSaveEnabled = LBLandscape.GetAutoSaveState();

                            ZoomExtent(sceneView);

                            // IMPORTANT make sure there are no objects selected in the scene
                            // that could trigger AddPrefab - which will automatically delete them
                            // as they will not be within the Designer surface extent.
                            Selection.activeObject = null;

                            blockAddPrefabUntilTime = Time.realtimeSinceStartup + 0.100f;
                            isInitialised = true;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {                
                Debug.LogWarning("ERROR: " + methodName + " - Something went wrong. Is Scene tab selected?\n" + ex.Message);
            }
            finally
            {
                if (typePgbr != null) { LBEditorHelper.HideEditorProgressbar(typePgbr); }
            }
        }

        /// <summary>
        /// Create the plane that moves with the camera to block the background.
        /// See also LandscapeBuilderWindow.SceneGUI(..).
        /// On a forced refresh (due to it being deleted unintentionally in code
        /// or otherwise).
        /// </summary>
        /// <param name="landscapeTfrm"></param>
        private void CreateCameraPlane(Transform landscapeTfrm)
        {
            if (landscapeTfrm != null)
            {
                cameraPlaneTrfm = transform.Find("CameraQuad");

                if (cameraPlaneTrfm == null)
                {
                    GameObject gameObjCameraPlane = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    if (gameObjCameraPlane != null)
                    {
                        cameraPlaneTrfm = gameObjCameraPlane.transform;
                        this.transform.localScale = Vector3.one;
                        cameraPlaneTrfm.SetParent(this.transform);
                        gameObjCameraPlane.name = "CameraQuad";
                        Component collider = cameraPlaneTrfm.GetComponent(typeof(Collider));
                        if (collider != null) { DestroyImmediate(collider); }
                        MeshRenderer mRen = cameraPlaneTrfm.GetComponent(typeof(MeshRenderer)) as MeshRenderer;
                        if (mRen != null)
                        {
                            // Disable shadows
                            mRen.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                            mRen.receiveShadows = false;
                            // Assign an unlit material
                            Shader unlitColorShader = Shader.Find("Unlit/Color");
                            Material backgroundUnlitMaterial = new Material(unlitColorShader);
                            backgroundUnlitMaterial.SetColor("_Color", backgroundColour);
                            mRen.sharedMaterial = backgroundUnlitMaterial;
                        }

                        // Don't permit user to change the transform
                        //gameObjCameraPlane.hideFlags = gameObjCameraPlane.hideFlags | HideFlags.NotEditable;
                        // Don't permit user to select in scene or see in hierarchy
                        gameObjCameraPlane.hideFlags = gameObjCameraPlane.hideFlags | HideFlags.HideInHierarchy;
                    }
                }
            }
        }

        /// <summary>
        /// Create the Base gameobject which will be used as the parent for the
        /// BasePlane, CeilingPlane, and user-supplied prefabs (group members).
        /// </summary>
        private void CreateGroupBase()
        {
            string methodName = "LBGroupDesigner.CreateGroupBase";

            Transform landscapeTfrm = this.transform.parent;
            if (landscapeTfrm == null)
            {
                Debug.LogWarning("ERROR: " + methodName + " - could not find parent landscape gameobject. Please Report.");
            }
            else
            {
                grpBaseTrfm = landscapeTfrm.Find("LBGroupDesignerBase");
                if (grpBaseTrfm == null)
                {
                    GameObject gameObjectBase = new GameObject("LBGroupDesignerBase");
                    if (gameObjectBase != null)
                    {
                        grpBaseTrfm = gameObjectBase.transform;
                        grpBaseTrfmPosition = new Vector3(0f, BasePlaneOffsetY, 0f);
                        grpBaseTrfm.position = grpBaseTrfmPosition;
                        grpBaseTrfm.SetParent(landscapeTfrm);
                        // Don't permit user to change the gameobject
                        //gameObjectBase.hideFlags = gameObjectBase.hideFlags | HideFlags.NotEditable;
                        // Don't permit user to select in scene or see in hierarchy
                        gameObjectBase.hideFlags = gameObjectBase.hideFlags | HideFlags.HideInHierarchy;
                    }
                }
            }
        }

        /// <summary>
        /// Get the current BasePlane or create a new one if it doesn't already exist.
        /// Update the grpBasePlaneTrfm field with the base plane's transform.
        /// </summary>
        private void CreateBasePlane()
        {
            string methodName = "LBGroupDesigner.CreateBasePlane";

            if (grpBaseTrfm == null)
            {
                Debug.LogWarning("ERROR: " + methodName + " - could not find parent LBGroupDesignerBase gameobject. Please Report.");
            }
            else
            {
                grpBasePlaneTrfm = grpBaseTrfm.Find("GroupBasePlane");

                if (grpBasePlaneTrfm == null)
                {
                    GameObject gameObjectBasePlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
                    if (gameObjectBasePlane != null)
                    {
                        grpBasePlaneTrfm = gameObjectBasePlane.transform;
                        // Replace the default mesh collider (there is a regression in 2018.2.0 beta which causes drag n drop prefab preview sizing issues)
                        MeshCollider mCol = grpBasePlaneTrfm.GetComponent(typeof(MeshCollider)) as MeshCollider;
                        if (mCol != null) { DestroyImmediate(mCol); }
                        grpBasePlaneTrfm.gameObject.AddComponent(typeof(BoxCollider));

                        // If this position is changed, the ObjPath memberPrefabGameObject position in LBLandscapeTerrain.PopulateLandscapeWithGroups(..) also needs changing.
                        grpBasePlaneTrfm.position = new Vector3(0f, BasePlaneOffsetY, 0f);
                        grpBasePlaneCentre2D = Vector2.zero;
                        grpBasePlaneTrfm.SetParent(grpBaseTrfm);
                        gameObjectBasePlane.name = "GroupBasePlane";
                        MeshRenderer mRen = grpBasePlaneTrfm.GetComponent(typeof(MeshRenderer)) as MeshRenderer;
                        if (mRen != null)
                        {
                            // Disable casting shadows
                            mRen.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                            // Allow to receive shadows from prefabs within the designer
                            mRen.receiveShadows = true;
                        }
                    }
                }

                if (grpBasePlaneTrfm != null)
                {
                    // Allow zoom (cube) limits to be the size of the plane + 1.25% of the radius.
                    cubeLimitsSize = lbGroup.maxClearingRadius * 3.25f;
                    grpBasePlaneTrfm.localScale = Vector3.one * (lbGroup.maxClearingRadius * 2f / 10f);

                    // NOTE: We don't need to resize the box collider as it automatically scales with the plane.

                    // Refresh collider to force it to work correctly in Unity 2018.2.0
                    BoxCollider bCol = grpBasePlaneTrfm.GetComponent(typeof(BoxCollider)) as BoxCollider;
                    if (bCol != null) { bCol.enabled = false; bCol.enabled = true; }

                    // Don't permit user to change the gameobject
                    //grpBasePlaneTrfm.gameObject.hideFlags = grpBasePlaneTrfm.gameObject.hideFlags | HideFlags.NotEditable;
                    // Don't permit user to select in scene or see in hierarchy
                    grpBasePlaneTrfm.gameObject.hideFlags = grpBasePlaneTrfm.gameObject.hideFlags | HideFlags.HideInHierarchy;
                }
            }
        }

        /// <summary>
        /// Create a ceiling to avoid shadows being cast on the prefabs from terrains above
        /// </summary>
        private void CreateCeiling()
        {
            //string methodName = "LBGroupDesigner.CreateCeiling";

            if (grpBaseTrfm != null)
            {
                GameObject gameObjectCeiling = GameObject.CreatePrimitive(PrimitiveType.Plane);
                if (gameObjectCeiling != null)
                {
                    Transform grpCeilingPlaneTrfm = gameObjectCeiling.transform;
                    grpCeilingPlaneTrfm.SetParent(grpBaseTrfm);
                    gameObjectCeiling.name = "GroupCeilingPlane";
                    // Set it to 5X the scaled size as the base plane
                    grpCeilingPlaneTrfm.localScale = grpCeilingPlaneTrfm.localScale * 50f;
                    grpCeilingPlaneTrfm.localRotation = Quaternion.Euler(0f, 0f, 180f);
                    grpCeilingPlaneTrfm.position = new Vector3(0f, grpBaseTrfm.position.y + 500f, 0f);
                    MeshRenderer mRen = grpCeilingPlaneTrfm.GetComponent(typeof(MeshRenderer)) as MeshRenderer;
                    if (mRen != null)
                    {
                        // Disable shadows
                        mRen.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        mRen.receiveShadows = false;

                        // Assign an unlit material
                        Shader unlitColorShader = Shader.Find("Unlit/Color");
                        Material backgroundUnlitMaterial = new Material(unlitColorShader);
                        backgroundUnlitMaterial.SetColor("_Color", backgroundColour);
                        mRen.sharedMaterial = backgroundUnlitMaterial;
                    }
                    // Don't permit user to change the transform
                    //gameObjectCeiling.hideFlags = gameObjectCeiling.hideFlags | HideFlags.NotEditable;
                    // Don't permit user to select in scene or see in hierarchy
                    gameObjectCeiling.hideFlags = gameObjectCeiling.hideFlags | HideFlags.HideInHierarchy;
                }
            }
        }

        /// <summary>
        /// Initialise any zones by ensuring none of them have the isMoving flag set.
        /// </summary>
        private void InitialiseZones()
        {
            if (lbGroup != null && lbGroup.zoneList != null)
            {
                int numZones = lbGroup.zoneList == null ? 0 : lbGroup.zoneList.Count;
                for (int zoneIdx = 0; zoneIdx < numZones; zoneIdx++) { lbGroup.zoneList[zoneIdx].isMoving = false; }

                if (zoneSelectedList == null) { zoneSelectedList = new List<int>(numZones+1); }

                if (numZones > 0)
                {
                    // If a zone isn't already selected, select the first one
                    if (zoneSelectedList != null) { if (zoneSelectedList.Count == 0) { zoneSelectedList.Add(0); } }
                }
                else { if (zoneSelectedList != null) { zoneSelectedList.Clear(); } }
            }
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

        // Draw gizmos whenever the designer is being shown in the scene
        private void OnDrawGizmos()
        {
            if (lbGroup != null && grpBaseTrfmPosition.y != 0f)
            {
                if (showGroupExtent)
                {
                    // Draw outer radius of the clearing
                    UnityEditor.Handles.color = Color.red;
                    // Avoid using the base plane as that may have been destroyed (inadvertently by a bug).
                    UnityEditor.Handles.DrawWireDisc(grpBaseTrfmPosition, Vector3.up, lbGroup.maxClearingRadius);
                }
            }
        }

        // Delegate function used because this is not an Editor script
        // NOTE: Main Context Menu for the GroupDesigner is in Editor\LBGroupDesignerItemEditor
        private void SceneGUI(SceneView sv)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { return; }

            Event currentEvt = Event.current;

            if (showZones && lbGroup.zoneList != null && currentEvt != null)
            {
                Vector3 areaPos = Vector3.zero;
                Vector3 newPos = Vector3.zero;
                // Sometimes the grpBasePlaneTrfm gets deleted (due to a bug)
                //Vector3 forwards = this.transform.forward;
                //Vector3 forwards = this.grpBasePlaneTrfm.transform.forward;
                Vector3 scale = Vector3.one;

                //bool isLeftButton = (currentEvt.button == 0);
                bool isRightButton = (currentEvt.button == 1);

                zoneLabelStyle = new GUIStyle("Box");
                zoneLabelStyle.fontSize = 14;
                zoneLabelStyle.border = new RectOffset(2, 2, 2, 2);
                GUI.skin.box.normal.textColor = zoneLabelColour;
                zoneLabelStyle.onFocused.textColor = zoneLabelColour;

                int numZones = lbGroup.zoneList.Count;
                int numSelected = zoneSelectedList == null ? 0 : zoneSelectedList.Count;

                #region Display the Context-sensitive menu
                if (currentEvt.type == EventType.MouseDown && isRightButton && Tools.current != Tool.View && Tools.current != Tool.None)
                {
                    GenericMenu menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Refresh Designer"), false, () => { RefreshWorkspace(true); });
                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Hide Zones"), false, () => { showZones = false; });
                    menu.AddItem(new GUIContent("Show All Zones"), false, () => { ShowZonesAll(); });
                    if (numSelected > 0)
                    {
                        menu.AddItem(new GUIContent("Hide Selected"), false, () => { ShowSelectedZones(false); LBEditorHelper.RepaintLBW(); });
                        menu.AddItem(new GUIContent("Hide Unselected"), false, () => { HideUnselectedZones(); LBEditorHelper.RepaintLBW(); } );
                        menu.AddItem(new GUIContent("Unselect"), false, () => { zoneSelectedList.Clear(); });
                    }
                    menu.AddItem(new GUIContent("Allow Scene View Rotation"), false, () => { Tools.current = Tool.View; });
                    menu.AddItem(new GUIContent("Cancel"), false, () => { });
                    menu.ShowAsContext();
                    currentEvt.Use();
                }
                #endregion

                #region Show Zones
                else
                {
                    UnityEditor.Handles.color = Color.cyan;
                    for (int zoneIdx = 0; zoneIdx < numZones; zoneIdx++)
                    {
                        LBGroupZone lbGroupZone = lbGroup.zoneList[zoneIdx];

                        if (!lbGroupZone.showInScene) { continue; }

                        // Check to see if this zone is being moved by the user
                        // If moving, use the temporary (current) location of the zone which will change as the user drags it.
                        //areaPos.x = lbGroupZone.isMoving ? lbGroupZone.currentCentrePointX * lbGroup.maxClearingRadius : lbGroupZone.centrePointX * lbGroup.maxClearingRadius;
                        //areaPos.z = lbGroupZone.isMoving ? lbGroupZone.currentCentrePointZ * lbGroup.maxClearingRadius : lbGroupZone.centrePointZ * lbGroup.maxClearingRadius;

                        areaPos.y = BasePlaneOffsetY;
                        areaPos.x = lbGroupZone.centrePointX * lbGroup.maxClearingRadius;
                        areaPos.z = lbGroupZone.centrePointZ * lbGroup.maxClearingRadius;

                        bool isSelected = zoneSelectedList.Exists(zn => zn == zoneIdx);

                        // Only display the handles for selected zones (currently only max one selected)
                        if (isSelected)
                        {

                            #region Move
                     
                            #if UNITY_2017_3_OR_NEWER
                            if (Tools.current == Tool.Move || Tools.current == Tool.Transform)
                            #else
                            if (Tools.current == Tool.Move)
                            #endif
                            {
                                EditorGUI.BeginChangeCheck();
                                // Make a handle centre of zone so that it can be dragged around in the scene view
                                previousZoneMovePosition = areaPos;
                                areaPos = Handles.PositionHandle(areaPos, Quaternion.identity);

                                if (EditorGUI.EndChangeCheck())
                                {
                                    AddPrefabBlock(100f);

                                    // Only update if zone has moved
                                    if (areaPos.x != previousZoneMovePosition.x || areaPos.z != previousZoneMovePosition.z)
                                    {
                                        float distanceToCentre = Vector2.Distance(grpBasePlaneCentre2D, new Vector2(areaPos.x, areaPos.z));
                                        if (distanceToCentre > lbGroup.maxClearingRadius) { areaPos = previousZoneMovePosition; }
                                        previousZoneMovePosition = areaPos;

                                        lbGroupZone.centrePointX = Mathf.Clamp(areaPos.x / lbGroup.maxClearingRadius, -1f, 1f);
                                        lbGroupZone.centrePointZ = Mathf.Clamp(areaPos.z / lbGroup.maxClearingRadius, -1f, 1f);

                                        LBEditorHelper.RepaintLBW();
                                    }
                                }
                            }
                            #endregion

                            #region Scale

                            #if UNITY_2017_3_OR_NEWER
                            if (Tools.current == Tool.Scale || Tools.current == Tool.Transform)
                            #else
                            if (Tools.current == Tool.Scale)
                            #endif
                            {
                                // Length and Width is normalised to the clearing radius
                                scale.x = lbGroupZone.width * lbGroup.maxClearingRadius;
                                scale.z = lbGroupZone.length * lbGroup.maxClearingRadius;

                                if (lbGroupZone.zoneType == LBGroupZone.LBGroupZoneType.circle)
                                {
                                    EditorGUI.BeginChangeCheck();
                                    scale = Handles.ScaleHandle(scale, areaPos, Quaternion.identity, HandleUtility.GetHandleSize(areaPos));
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        AddPrefabBlock(100f);
                                        lbGroupZone.width = scale.x / lbGroup.maxClearingRadius;
                                        lbGroupZone.length = lbGroupZone.width;
                                        GUI.FocusControl(null);
                                        LBEditorHelper.RepaintLBW();
                                    }
                                }
                                else
                                {
                                    EditorGUI.BeginChangeCheck();
                                    scale = Handles.ScaleHandle(scale, areaPos, Quaternion.identity, HandleUtility.GetHandleSize(areaPos));
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        AddPrefabBlock(100f);
                                        lbGroupZone.width = scale.x / lbGroup.maxClearingRadius;
                                        lbGroupZone.length = scale.z / lbGroup.maxClearingRadius;
                                        GUI.FocusControl(null);
                                        LBEditorHelper.RepaintLBW();
                                    }
                                }
                            }

                            #endregion

                        }
                        else
                        {
                            // Draw a selectable button for the non-selected zones
                            if (Handles.Button(areaPos, Quaternion.identity, 1.0f, 0.75f, Handles.SphereHandleCap))
                            {
                                // Select this zone
                                zoneSelectedList.Clear();
                                zoneSelectedList.Add(zoneIdx);
                                lbGroup.GroupZoneListExpand(false);
                                lbGroup.zoneList[zoneIdx].showInEditor = true;
                                LBEditorHelper.RepaintLBW();
                            }
                        }

                        // (Probably) finished dragging the zone, so refresh the GroupDesigner
                        //if (currentEvt != null && isLeftButton && currentEvt.type == EventType.MouseUp)
                        //{
                        //    //RefreshWorkspace();
                        //}

                        #region Draw Zone

                        DrawZone(lbGroupZone, areaPos, Vector3.zero, lbGroup.maxClearingRadius, BasePlaneOffsetY, 0f, isSelected, zoneLabelStyle);

                        #endregion

                        #region Old Event processing code - keep for reference
                        //int controlId = GUIUtility.GetControlID(FocusType.Passive);

                        //// Filter the events
                        //switch (currentEvt.GetTypeForControl(controlId))
                        //{
                        //    case EventType.Layout:
                        //        // First add the control with it's unique ID so that we can track it in later events
                        //        HandleUtility.AddControl(controlId, HandleUtility.DistanceToCircle(areaPos, 3f));
                        //        break;
                        //    case EventType.MouseDown:
                        //        // User want to move the zone
                        //        // Make sure we have the correct handle. This should test if the mouse position is within the handles rect in screen space.
                        //        if (isLeftButton && HandleUtility.nearestControl == controlId)
                        //        {
                        //            lbGroupZone.isMoving = true;
                        //            GUIUtility.hotControl = controlId;

                        //            //Debug.Log("MouseDown - Set moving for " + lbGroupZone.zoneName + " mousePos:" + currentEvt.mousePosition + " areaPos:" + areaPos);
                        //            currentEvt.Use();
                        //        }
                        //        break;
                        //    case EventType.MouseUp:
                        //        // User has stopped moving the zone, so save the current zone location
                        //        if (isLeftButton && HandleUtility.nearestControl == controlId)
                        //        {
                        //            lbGroupZone.isMoving = false;
                        //            // Normalise to values between -1.0 and 1.0 within the group
                        //            lbGroupZone.centrePointX = areaPos.x / lbGroup.maxClearingRadius;
                        //            lbGroupZone.centrePointZ = areaPos.z / lbGroup.maxClearingRadius;
                        //            GUIUtility.hotControl = 0;
                        //            currentEvt.Use();
                        //            //Debug.Log("End movement of " + lbGroupZone.zoneName);
                        //            LBEditorHelper.RepaintLBW();
                        //            RefreshWorkspace();
                        //        }
                        //        break;
                        //    case EventType.MouseDrag:
                        //        // User is moving the zone
                        //        if (isLeftButton && HandleUtility.nearestControl == controlId)
                        //        {
                        //            if (grpBasePlaneTrfm != null)
                        //            {
                        //                newPos = LBEditorHelper.GetTransformPositionFromMouse(grpBasePlaneTrfm, currentEvt.mousePosition, true);
                        //                newPos.y = BasePlaneOffsetY;

                        //                if (newPos.x != 0 && newPos.y != 0)
                        //                {
                        //                    // Prevent user dragging zone centre point outside the group extents
                        //                    float distanceToCentre = Vector2.Distance(grpBasePlaneCentre2D, new Vector2(newPos.x, newPos.z));
                        //                    if (distanceToCentre > lbGroup.maxClearingRadius) { newPos = previousZoneMovePosition; }
                        //                    previousZoneMovePosition = newPos;

                        //                    lbGroupZone.currentCentrePointX = Mathf.Clamp(newPos.x / lbGroup.maxClearingRadius, -1f, 1f);
                        //                    lbGroupZone.currentCentrePointZ = Mathf.Clamp(newPos.z / lbGroup.maxClearingRadius, -1f, 1f);
                        //                    currentEvt.Use();
                        //                }
                        //            }
                        //        }
                        //        break;
                        //    case EventType.Repaint:

                        //        Handles.color = zoneLineColour;
                        //        // This is where we draw handles etc. in the scene view
                        //        newPos = Handles.PositionHandle(areaPos, Quaternion.identity);

                        //        if (lbGroupZone.zoneType == LBGroupZone.LBGroupZoneType.circle)
                        //        {
                        //            UnityEditor.Handles.DrawWireArc(newPos, Vector3.up, forwards, 360, lbGroupZone.width * lbGroup.maxClearingRadius);
                        //        }
                        //        else
                        //        {
                        //            float zoneWidthOffset = lbGroupZone.width / 2f * lbGroup.maxClearingRadius;
                        //            float zoneLengthOffset = lbGroupZone.length / 2f * lbGroup.maxClearingRadius;

                        //            // Draw rectanglar zone
                        //            Vector3[] corners = {   new Vector3( -zoneWidthOffset + newPos.x, BasePlaneOffsetY, zoneLengthOffset + newPos.z ),    // Top Left
                        //                                new Vector3( zoneWidthOffset + newPos.x, BasePlaneOffsetY, zoneLengthOffset + newPos.z ),     // Top Right
                        //                                new Vector3( zoneWidthOffset + newPos.x, BasePlaneOffsetY, -zoneLengthOffset + newPos.z ),    // Bottom Right
                        //                                new Vector3( -zoneWidthOffset + newPos.x, BasePlaneOffsetY, -zoneLengthOffset + newPos.z ),   // Bottom Left
                        //                                new Vector3( -zoneWidthOffset + newPos.x, BasePlaneOffsetY, zoneLengthOffset + newPos.z ) };  // Top Left
                        //            UnityEditor.Handles.DrawPolyLine(corners);
                        //        }

                        //        //Handles.color = Color.black; // zoneLabelColour;
                        //        Handles.Label(newPos + (Vector3.up * 3) + (Vector3.right * 3), lbGroupZone.zoneName, zoneLabelStyle);

                        //        break;
                        //}
                        #endregion
                    }
                }
                #endregion
            }
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Create a new Group Designer and return a reference to the LBGroupDesigner
        /// script attached to the gameobject. Create it under the landscape supplied
        /// </summary>
        /// <param name="landscape"></param>
        /// <returns></returns>
        public static LBGroupDesigner CreateDesigner(LBLandscape landscape, LBGroup lbGroup)
        {
            LBGroupDesigner lbGroupDesigner = null;
            string methodName = "LBGroupDesigner.CreateDesigner";

            if (landscape == null) { Debug.LogWarning("ERROR: " + methodName + " - landscape cannot be null. Please Report"); }
            else if (lbGroup == null) { Debug.LogWarning("ERROR: " + methodName + " - lbGroup cannot be null. Please Report"); }
            else
            {
                // Find the first LBGroupDesigner script attached to a child gameobject (there should be none or one)
                lbGroupDesigner = (LBGroupDesigner)landscape.gameObject.GetComponentInChildren(typeof(LBGroupDesigner), true);

                if (lbGroupDesigner != null)
                {
                    // Remove any existing designers
                    DeleteDesigners(landscape);
                }

                GameObject go = new GameObject("LBGroupDesigner");

                if (go == null) { Debug.LogWarning("ERROR: " + methodName + " - could not create new gameobject in scene for the Group Designer. Please Report"); }
                else
                {
                    go.transform.SetParent(landscape.transform);
                    lbGroupDesigner = go.AddComponent<LBGroupDesigner>();
                    if (lbGroupDesigner != null)
                    {
                        // Prevent user from editing or deleting Group Designer
                        go.hideFlags = go.hideFlags | HideFlags.NotEditable;

                        lbGroupDesigner.lbGroup = lbGroup;
                        lbGroupDesigner.autoRefreshGroupDesigner = landscape.autoRefreshGroupDesigner;
                        lbGroupDesigner.Initialise();
                    }
                }
            }

            return lbGroupDesigner;
        }

        /// <summary>
        /// Open or close the Group Designer for a single Group
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="lbGroup"></param>
        /// <param name="isShown"></param>
        /// <returns></returns>
        public static bool ShowGroupDesigner(LBLandscape landscape, ref LBGroupDesigner lbGroupDesigner, LBGroup lbGroup, bool isShown)
        {
            bool isSuccessful = false;
            string methodName = "LBGroupDesigner.ShowGroupDesigner";

            if (landscape == null) { Debug.LogWarning("ERROR: " + methodName + " - landscape cannot be null. Please Report"); return false; }

            LBEditorHelper.ShowSceneView(typeof(LBGroupDesigner));

            try
            {
                // Remember the scene view camera settings
                SceneView sceneView = UnityEditor.SceneView.lastActiveSceneView;
                if (sceneView != null)
                {
                    Camera sceneViewCamera = sceneView.camera;

                    if (sceneViewCamera == null) { Debug.LogWarning("ERROR: " + methodName + " - cannot find scene view camera."); }
                    else if (isShown)
                    {
                        landscape.LockTerrains(true);
                        lbGroupDesigner = LBGroupDesigner.CreateDesigner(landscape, lbGroup);
                        if (lbGroupDesigner != null) { isSuccessful = true; }
                        else { Debug.LogWarning("ERROR: " + methodName + " could not create designer in scene. Please Report."); }
                    }
                    else
                    {
                        // Remove all the temporary Group Designers items from the scene.
                        // There should be only 1 but just in case check for multiple.
                        LBGroupDesigner.DeleteDesigners(landscape);

                        // Restore Unity fog settings
                        RenderSettings.fog = lbGroupDesigner.isUnityFogOn;

                        // Restore original scene view camera settings
                        sceneView.pivot = lbGroupDesigner.origSceneViewPivot;
                        sceneView.rotation = lbGroupDesigner.origSceneViewRotation;
                        sceneView.orthographic = lbGroupDesigner.origSceneViewIsOrthographic;
                        sceneView.size = lbGroupDesigner.origSceneViewSize;
                        // Restore the original scene view camera location
                        sceneViewCamera.transform.position = lbGroupDesigner.origSceneViewCameraPosition;
                        sceneViewCamera.transform.rotation = lbGroupDesigner.origSceneViewCameraRotation;
                        sceneViewCamera.transform.localScale = lbGroupDesigner.origSceneViewCameraLocalScale;

                        #if UNITY_5_5_OR_NEWER
                        // Unlock the rotation
                        sceneView.isRotationLocked = false;
                        #endif

                        landscape.LockTerrains(false);

                        // Restore the original Gizmo settings
                        LBEditorHelper.GizmoShowSelectionOutline = lbGroupDesigner.origSceneViewShowSelectionOutline;
                        isSuccessful = true;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("ERROR: " + methodName + " - Something went wrong. Is Scene tab selected?\n" + ex.Message);
            }

            return isSuccessful;
        }

        /// <summary>
        /// Delete any Group Designers which are children of a landscape
        /// </summary>
        /// <param name="landscape"></param>
        public static void DeleteDesigners(LBLandscape landscape)
        {
            string methodName = "LBGroupDesigner.DeleteDesigners";
            if (landscape == null) { Debug.LogWarning("ERROR: " + methodName + " - landscape cannot be null. Please Report"); }
            else
            {
                // Find any Group Designers in the scene that are children of the landscape
                List<LBGroupDesigner> itemList = new List<LBGroupDesigner>(landscape.GetComponentsInChildren<LBGroupDesigner>());
                if (itemList != null)
                {
                    // Loop backwards through the Group Designers and delete them from the scene
                    for (int item = itemList.Count - 1; item >= 0; item--)
                    {
                        if (itemList[item] != null)
                        {
                            itemList[item].ClearAllObjects(true);

                            DestroyImmediate(itemList[item].gameObject);
                        }
                    }
                    itemList.Clear();
                    itemList = null;
                }

                Transform grpGroupBaseTrfm = landscape.transform.Find("LBGroupDesignerBase");
                if (grpGroupBaseTrfm != null) { DestroyImmediate(grpGroupBaseTrfm.gameObject); }
            }
        }

        /// <summary>
        /// Draw the outline and label of a zone.
        /// areaPos: zone position in metres within the group + offsetY
        /// groupPos: is the position of the group (which contains the zone) in metres
        /// </summary>
        /// <param name="lbGroupZone"></param>
        /// <param name="areaPos"></param>
        /// <param name="groupPos"></param>
        /// <param name="maxClearingRadius"></param>
        /// <param name="offsetY"></param>
        /// <param name="rotationY"></param>
        /// <param name="isSelected"></param>
        /// <param name="zoneLabelStyle"></param>
        public static void DrawZone(LBGroupZone lbGroupZone, Vector3 areaPos, Vector3 groupPos, float maxClearingRadius, float offsetY, float rotationY, bool isSelected, GUIStyle zoneLabelStyle)
        {
            using (new Handles.DrawingScope(UnityEngine.Color.cyan))
            {
                if (lbGroupZone.zoneType == LBGroupZone.LBGroupZoneType.circle)
                {
                    if (rotationY != 0f)
                    {
                        // Get the rotated group-space zone position
                        Vector3 posRelativeToGroup = Quaternion.Euler(0f, rotationY, 0f) * new Vector3(lbGroupZone.centrePointX * maxClearingRadius, 0f, lbGroupZone.centrePointZ * maxClearingRadius);
                        // Update the rotated zone with the worldspace (??) group position
                        areaPos.x = posRelativeToGroup.x + groupPos.x;
                        areaPos.z = posRelativeToGroup.z + groupPos.z;
                    }
                    UnityEditor.Handles.DrawWireArc(areaPos, Vector3.up, Vector3.right, 360, lbGroupZone.width * maxClearingRadius);
                }
                else
                {
                    float zoneWidthOffset = lbGroupZone.width / 2f * maxClearingRadius;
                    float zoneLengthOffset = lbGroupZone.length / 2f * maxClearingRadius;

                    // Draw rectanglar zone
                    Vector3[] corners = {   new Vector3( -zoneWidthOffset + areaPos.x, offsetY, zoneLengthOffset + areaPos.z ),    // Top Left
                                                    new Vector3( zoneWidthOffset + areaPos.x, offsetY, zoneLengthOffset + areaPos.z ),     // Top Right
                                                    new Vector3( zoneWidthOffset + areaPos.x, offsetY, -zoneLengthOffset + areaPos.z ),    // Bottom Right
                                                    new Vector3( -zoneWidthOffset + areaPos.x, offsetY, -zoneLengthOffset + areaPos.z ),   // Bottom Left
                                                    new Vector3( -zoneWidthOffset + areaPos.x, offsetY, zoneLengthOffset + areaPos.z ) };  // Top Left

                    if (rotationY != 0f)
                    {
                        int numCorners = corners.Length;
                        Vector3 posRelativeToGroup;

                        for (int cn = 0; cn < numCorners; cn++)
                        {
                            // Get the rotated group-space zone corner position
                            posRelativeToGroup = Quaternion.Euler(0f, rotationY, 0f) * new Vector3(corners[cn].x - groupPos.x, 0f, corners[cn].z - groupPos.z);
                            // Update the rotated zone with the worldspace (??) group position
                            corners[cn].x  = posRelativeToGroup.x + groupPos.x;
                            corners[cn].z  = posRelativeToGroup.z + groupPos.z;
                        }

                        // Ensure the label gets rendered in the correct location
                        // Get the rotated group-space zone position
                        posRelativeToGroup = Quaternion.Euler(0f, rotationY, 0f) * new Vector3(lbGroupZone.centrePointX * maxClearingRadius, 0f, lbGroupZone.centrePointZ * maxClearingRadius);
                        // Update the rotated zone with the worldspace (??) group position
                        areaPos.x = posRelativeToGroup.x + groupPos.x;
                        areaPos.z = posRelativeToGroup.z + groupPos.z;
                    }
                    UnityEditor.Handles.DrawPolyLine(corners);
                }

                // Only show the labels if they are enabled in Editor AND infront of the scene view camera
                // This prevents a ghosted handle being displayed when it is behind the camera.
                if (LBLandscape.IsPointInCameraView(SceneView.lastActiveSceneView.camera, areaPos))
                {
                    zoneLabelStyle.fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal;
                    Handles.Label(areaPos + (Vector3.up * 3) + (Vector3.right * 3), lbGroupZone.zoneName, zoneLabelStyle);
                }
            }
        }



        #endregion

        #region Private Non-Static Methods

        private Vector3 GetPrefabScale(LBGroupMember lbGroupMember)
        {
            Vector3 prefabScale;
            // Prefab scaling
            if (lbGroupMember.isGroupOverride) { prefabScale = Vector3.one * UnityEngine.Random.Range(lbGroupMember.minScale, lbGroupMember.maxScale); }
            else { prefabScale = Vector3.one * UnityEngine.Random.Range(lbGroup.minScale, lbGroup.maxScale); }

            return prefabScale;
        }

        private Vector3 GetPrefabOffset(LBGroupMember lbGroupMember)
        {
            // Default prefabOffset
            Vector3 prefabOffset = new Vector3(lbGroupMember.minOffsetX, lbGroupMember.minOffsetY, lbGroupMember.minOffsetZ);

            // If y-axis placement is being randomised (like for raising and lowering a rock in the terrain), override the Y-axis prefab offset.
            if (lbGroupMember.randomiseOffsetY && lbGroupMember.minOffsetY != lbGroupMember.maxOffsetY)
            {
                prefabOffset.y = UnityEngine.Random.Range(lbGroupMember.minOffsetY, lbGroupMember.maxOffsetY);
            }
            else { prefabOffset.y = lbGroupMember.minOffsetY; }

            return prefabOffset;
        }

        /// <summary>
        /// Get the offset for the model. It is affected by scaling of the model.
        /// </summary>
        /// <param name="lbGroupMember"></param>
        /// <param name="modelScale"></param>
        /// <returns></returns>
        private Vector3 GetModelOffset(LBGroupMember lbGroupMember, Vector3 modelScale)
        {
            // Model offset
            return Vector3.Scale(new Vector3(lbGroupMember.modelOffsetX, lbGroupMember.modelOffsetY, lbGroupMember.modelOffsetZ), modelScale);
        }

        /// <summary>
        /// Get the member rotation for placement within the designer.
        /// This is based on the code in LBLandscapeTerrain.PopulateTerrainWithGroups()
        /// </summary>
        /// <param name="lbGroupMember"></param>
        /// <returns></returns>
        private Quaternion GetMemberRotation(LBGroupMember lbGroupMember, Vector3 memberPosition)
        {
            Quaternion prefabRotation;
            float prefabRotationY;

            // Assume the clearing has no rotation (included for completeness)
            float clearingRotationY = 0f;

            // Calculate the Y rotation of the prefab first
            // Random rotation vs. fixed rotation
            if (lbGroupMember.randomiseRotationY) { prefabRotationY = UnityEngine.Random.Range(lbGroupMember.startRotationY, lbGroupMember.endRotationY); }
            else { prefabRotationY = lbGroupMember.startRotationY; }

            // Member normalised position within the group
            Vector3 memberPositionN = new Vector3(memberPosition.x / lbGroup.maxClearingRadius, 0f, memberPosition.z / lbGroup.maxClearingRadius);

            // Clearing prefabs can be placed in world space, group space or face 2 centre space
            // If in group space, rotate so that it is relative to clearing rotation
            if (lbGroupMember.rotationType == LBGroupMember.LBRotationType.GroupSpace)
            {
                prefabRotationY += clearingRotationY;
            }
            // If in face 2 group centre space, rotate to face centre of clearing
            else if (lbGroupMember.rotationType == LBGroupMember.LBRotationType.Face2GroupCentre)
            {
                if (grpBasePlaneTrfm != null && lbGroup != null)
                {
                    //Vector3 memberPositionN = new Vector3(memberPosition.x / lbGroup.maxClearingRadius, 0f, memberPosition.z / lbGroup.maxClearingRadius);
                    prefabRotationY += Quaternion.LookRotation(grpBasePlaneTrfm.position - memberPositionN, Vector3.up).eulerAngles.y;
                }
            }
            // If in face 2 zone centre space, rotate to face the centre of the FIRST zone in the member zone filters list (if any)
            else if (lbGroupMember.rotationType == LBGroupMember.LBRotationType.Face2ZoneCentre)
            {
                // Get the first zone filter (if any)
                if (lbGroup != null && lbGroup.zoneList != null && lbGroupMember.zoneGUIDList != null && lbGroupMember.zoneGUIDList.Count > 0)
                {
                    // Get the zone from the group using the provided GUID
                    LBGroupZone firstZone = lbGroup.zoneList.Find(z => z.GUID == lbGroupMember.zoneGUIDList[0]);
                    if (firstZone != null)
                    {
                        // Get the normalised zone centre point
                        Vector3 zonePositionN = new Vector3(firstZone.centrePointX, 0f, firstZone.centrePointZ);

                        // Get the normalised position of the member within the group
                        //Vector3 memberPositionN = new Vector3(memberPosition.x / lbGroup.maxClearingRadius, 0f, memberPosition.z / lbGroup.maxClearingRadius);

                        if (firstZone.zoneType == LBGroupZone.LBGroupZoneType.circle)
                        {
                            prefabRotationY += Quaternion.LookRotation(zonePositionN - memberPositionN, Vector3.up).eulerAngles.y;
                        }
                        else // rectangular zone
                        {
                            // Declare zone equation variables
                            float zoneZEq1 = 0f, zoneZEq2 = 0f;
                            // Calculate normalised (0-1) coordinates for position in the rectangle
                            float zoneMemberXPos = ((memberPositionN.x - firstZone.centrePointX) / firstZone.width) + 0.5f;
                            float zoneMemberZPos = ((memberPositionN.z - firstZone.centrePointZ) / firstZone.length) + 0.5f;
                            // Calculate the gradient of the dividing lines
                            float zonePosGradient = firstZone.width / firstZone.length;

                            if (firstZone.width >= firstZone.length)
                            {
                                // Calculate the height of each dividing line equation given the x-coordinate
                                // of the zone member
                                zoneZEq1 = Mathf.Clamp(zoneMemberXPos * zonePosGradient, 0f, 0.5f) +
                                    Mathf.Clamp(((zoneMemberXPos - 1f) * zonePosGradient) + 0.5f, 0, 0.5f);
                                zoneZEq2 = Mathf.Clamp(0.5f - (zoneMemberXPos * zonePosGradient), 0f, 0.5f) +
                                    Mathf.Clamp((1f - zoneMemberXPos) * zonePosGradient, 0, 0.5f);
                            }
                            else
                            {
                                // Calculate the height of each dividing line equation given the x-coordinate
                                // of the zone member
                                zoneZEq1 = zoneMemberXPos * zonePosGradient;
                                zoneZEq2 = 1f - (zoneMemberXPos * zonePosGradient);
                                if (zoneMemberXPos > 0.5f) { zoneZEq1 += 1f - zonePosGradient; zoneZEq2 -= 1f - zonePosGradient; }
                            }

                            // Compare the z-coordinate of the zone member with the z-coordinates of the dividing
                            // line equations to work out which side the zone member is on
                            // Top
                            if (zoneMemberZPos >= zoneZEq1 && zoneMemberZPos >= zoneZEq2) { prefabRotationY += 180f; }
                            // Bottom
                            else if (zoneMemberZPos < zoneZEq1 && zoneMemberZPos < zoneZEq2) { prefabRotationY += 0f; }
                            // Left
                            else if (zoneMemberZPos >= zoneZEq1 && zoneMemberZPos < zoneZEq2) { prefabRotationY += 90f; }
                            // Right
                            else { prefabRotationY += -90f; }
                            // Adjust rotation to match clearing rotation
                            prefabRotationY += clearingRotationY;
                        }
                    }
                }
            }
            // If in world space, don't modify rotation

            // No need to do align with terrain in the Group Designer (as the plane is always flat and aligned with horizon)

            // Set the final rotation
            if (lbGroupMember.randomiseRotationXZ && lbRandomPrefabXZRotation != null)
            {
                // LBRandom.Range is minFloat inclusive to maxFloat exclusive, so add a small amount to end rotation.
                prefabRotation = Quaternion.Euler(lbRandomPrefabXZRotation.Range(lbGroupMember.rotationX, lbGroupMember.endRotationX + 0.01f), prefabRotationY, lbRandomPrefabXZRotation.Range(lbGroupMember.rotationZ, lbGroupMember.endRotationZ + 0.01f));
            }
            else { prefabRotation = Quaternion.Euler(lbGroupMember.rotationX, prefabRotationY, lbGroupMember.rotationZ); }

            return prefabRotation;
        }

        /// <summary>
        /// Get a random offset from the centre of the clearing
        /// </summary>
        /// <returns></returns>
        private Vector2 GetBasePlacementOffset(LBGroupMember lbGroupMember)
        {
            Vector2 offset = Vector2.zero;
            
            if (grpBasePlaneTrfm != null)
            {
                // A plane is 10x10
                float basePlaneWidth = grpBasePlaneTrfm.localScale.x * 10f;
                //float basePlaneLength = grpBasePlaneTrfm.localScale.z * 10f;

                offset = LBGroupMember.GetRandomOffset(lbGroupMember, basePlaneWidth / 2f, 100);
            }

            return offset;
        }

        /// <summary>
        /// Remember the random placement offset from centre of the clearing so that we can work out the actual offsets
        /// wanted as the user saves the changes made in the designer.
        /// </summary>
        /// <param name="lbGroupDesignerItem"></param>
        private void SetBasePlacementOffset(LBGroupDesignerItem lbGroupDesignerItem)
        {
            if (lbGroupDesignerItem != null)
            {
                Vector2 offset = GetBasePlacementOffset(lbGroupDesignerItem.lbGroupMember);
                lbGroupDesignerItem.basePlacementOffset.x = offset.x;
                lbGroupDesignerItem.basePlacementOffset.z = offset.y;
            }
        }

        /// <summary>
        /// Show or hide the selected zones in the GroupDesigner when lbGroupDesigner.showZones is true
        /// </summary>
        /// <param name="isShown"></param>
        private void ShowSelectedZones(bool isShown)
        {
            if (lbGroup != null)
            {
                int numZones = lbGroup.zoneList == null ? 0 : lbGroup.zoneList.Count;
                int numSelected = zoneSelectedList == null ? 0 : zoneSelectedList.Count;

                for (int selIdx = 0; selIdx < numSelected; selIdx++)
                {
                    if (zoneSelectedList[selIdx] < numZones) { lbGroup.zoneList[zoneSelectedList[selIdx]].showInScene = isShown; };
                }

                if (numSelected > 0) { LBEditorHelper.RepaintLBW(); }
            }
        }

        /// <summary>
        /// Hide all unselected zones in the scene. Useful for turning off all zones except the
        /// one currently being worked on.
        /// </summary>
        private void HideUnselectedZones()
        {
            if (lbGroup != null)
            {
                int numZones = lbGroup.zoneList == null ? 0 : lbGroup.zoneList.Count;
                int numSelected = zoneSelectedList == null ? 0 : zoneSelectedList.Count;

                if (numZones > 0 && numSelected > 0)
                {
                    for (int znIdx = 0; znIdx < numZones; znIdx++)
                    {
                        // Is this zone in the selected list?
                        if (!zoneSelectedList.Exists(szn => szn == znIdx))
                        {
                            // If not, hide it
                            lbGroup.zoneList[znIdx].showInScene = false;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Show all the zones in the GroupDesigner when lbGroupDesigner.showZones is true
        /// NOTE: Some zones can be hidden when showZones is true. This sets them all to be displayed
        /// </summary>
        private void ShowZonesAll()
        {
            if (lbGroup != null)
            {
                int numZones = lbGroup.zoneList == null ? 0 : lbGroup.zoneList.Count;

                for (int znIdx = 0; znIdx < numZones; znIdx++)
                {
                    lbGroup.zoneList[znIdx].showInScene = true;
                }

                if (numZones > 0) { LBEditorHelper.RepaintLBW(); }
            }
        }

        #endregion

        #region Public Non-Static Methods

        /// <summary>
        /// If the designer has been initialised, refresh
        /// all the objects in the current workspace.
        /// </summary>
        /// <param name="forceRefresh"></param>
        public void RefreshWorkspace(bool forceRefresh = false)
        {
            if (isInitialised && (autoRefreshGroupDesigner || forceRefresh))
            {
                isRefreshingWorkspace = true;
                System.Type typePgbr = LBEditorHelper.GetEditorProgressBarType();
                if (typePgbr != null) { LBEditorHelper.ShowEditorProgressbar(typePgbr, "Refreshing...", 0.2f); }
                ClearAllObjects(false);
                if (typePgbr != null) { LBEditorHelper.ShowEditorProgressbar(typePgbr, "Refreshing...", 0.4f); }
                RefreshObjects();
                if (typePgbr != null) { LBEditorHelper.ShowEditorProgressbar(typePgbr, "Refreshing...", 0.7f); }
                AddPrefabBlock(200f);
                if (typePgbr != null) { LBEditorHelper.HideEditorProgressbar(typePgbr); }
                isRefreshingWorkspace = false;
            }
        }

        /// <summary>
        /// Refresh or reload all group members and add them to the base plane in the scene.
        /// Ignore members without a prefab or are not enabled.
        /// Optionally just refresh one member. Object Paths (and all their member or subgroups)
        /// get populated by calling LBLandscapeTerrain.PopulateLandscapeWithGroups(..) from
        /// this method.
        /// </summary>
        /// <param name="lbGroupMemberToPlace"></param>
        public void RefreshObjects(LBGroupMember lbGroupMemberToPlace = null)
        {
            string methodName = "LBGroupDesigner.RefreshObjects";

            if (lbGroup == null) { Debug.LogWarning("ERROR: " + methodName + " - LBGroup is null. Please Report"); }
            else if (lbGroup.groupMemberList == null) { Debug.LogWarning("ERROR: " + methodName + " - groupMemberList is null for " + lbGroup.groupName + ". Please Report"); }
            else if (grpBaseTrfm == null) { Debug.LogWarning("ERROR: " + methodName + " - Group Base is null for " + lbGroup.groupName + ". Please Report"); }
            else
            {
                Vector3 groupBasePos = grpBaseTrfm.position;
                LBGroupMember lbGroupMember;
                GameObject newPrefabInstance = null;
                Vector3 prefabPosition, proximityPosition, prefabOffset, modelOffset, prefabScale;
                #if UNITY_2018_2_OR_NEWER
                Vector3 nudgeUp = (Vector3.up * 0.001f);
                #endif
                Quaternion prefabRotation;
                int targetNumberOfMembers = 0;
                Vector2 randomOffset = Vector2.zero;
                bool isMemberPlacementPositionOk = false;
                bool isZoneEdgeFillEnabled = false;

                int zoneListIndex = 0;
                LBGroupZone thisZone;
                bool positionInsideZone = false;

                List<LBObjectProximity> objectProximitiesList = new List<LBObjectProximity>();

                // Setup ObjPath variables
                LBGroupParameters lbGroupParm = null;

                if (lbGroup.groupMemberList.Exists(gm => gm.lbMemberType == LBGroupMember.LBMemberType.ObjPath))
                {
                    lbGroupParm = new LBGroupParameters();
                    lbGroupParm.showErrors = true;
                    lbGroupParm.showProgress = false;
                    lbGroupParm.showProgressDelegate = null;
                    lbGroupParm.landscape = this.GetComponentInParent<LBLandscape>();
                    lbGroupParm.isGroupDesignerEnabled = true;
                    lbGroupParm.designerOffsetY = BasePlaneOffsetY;
                    // Pass in the list of proximities in the designer, so they
                    // object on the object path can be used for proximity in the designer.
                    // LBLandscapeTerrain.PopulateLandscapeWithGroups updates the list for the designer.
                    lbGroupParm.objectProximitiesList = objectProximitiesList;
                }

                // Create an instance of LBRandom for the prefab XZ random rotation
                // and set the seed to the x-axis position of the group
                if (lbRandomPrefabXZRotation == null)
                {
                    lbRandomPrefabXZRotation = new LBRandom();
                    if (lbRandomPrefabXZRotation != null)
                    {
                        lbRandomPrefabXZRotation.SetSeed((int)transform.position.x);
                    }
                    else { Debug.LogWarning("ERROR: " + methodName + " could not create LBRandom instance for prefab XZ rotation. Please Report"); }
                }

                int numGroupMembers = lbGroup.groupMemberList.Count;
                for (int gmIdx = 0; gmIdx < numGroupMembers; gmIdx++)
                {
                    lbGroupMember = lbGroup.groupMemberList[gmIdx];

                    // If we want to refresh a single group member, check this is the correct one
                    if (lbGroupMemberToPlace != null && lbGroupMemberToPlace.GUID != lbGroupMember.GUID) { continue; }

                    if (lbGroupMember != null)
                    {
                        if (lbGroupMember.isDisabled) { continue; }
                        else if (lbGroupMember.lbMemberType == LBGroupMember.LBMemberType.ObjPath)
                        {
                            if (lbGroupMember.lbObjPath != null && lbGroupParm != null)
                            {
                                lbGroupParm.lbObjPathParm = new LBObjPathParameters();
                                lbGroupParm.lbObjPathParm.lbGroupOwner = lbGroup;
                                lbGroupParm.lbObjPathParm.lbGroupMemberOwner = lbGroupMember;
                                lbGroupParm.lbObjPathParm.prefabItemType = LBPrefabItem.PrefabItemType.ObjPathDesignerPrefab;

                                LBLandscapeTerrain.PopulateLandscapeWithGroups(lbGroupParm);
                            }

                            // This (ObjPath) member has been processed, so move to the next
                            continue;
                        }
                        // Skip members that are not an Object Path but only should exist on an Object Path, or don't have a prefab
                        else if (lbGroupMember.isPathOnly || lbGroupMember.prefab == null) { continue; }

                        // Calculate the target number in a clearing. Note, radius of each clearing could be different.
                        // Use clearingRadius local variable
                        if (lbGroupMember.isPlacedInCentre) { targetNumberOfMembers = 1; }
                        else
                        {
                            // Clearing diameter = width = length
                            targetNumberOfMembers = Mathf.RoundToInt(((lbGroup.maxClearingRadius * lbGroup.maxClearingRadius * 4) / 1000000f) * lbGroupMember.maxPrefabSqrKm);

                            if (targetNumberOfMembers > lbGroupMember.maxPrefabPerGroup) { targetNumberOfMembers = lbGroupMember.maxPrefabPerGroup; }

                            // We may want at least 1 item placed in each clearing.
                            if (targetNumberOfMembers == 0) { targetNumberOfMembers = 1; }
                        }

                        // 32bit integers wrap to -ve values (then back to +ve). Check that the number of iterations is
                        // within the bounds of a 32bit int. We loop though 256 times the number of target members OR
                        // until we reach the target OR 0.1 seconds have elapsed since a prefab was placed.
                        ulong maxMemberIterationsLong = (ulong)targetNumberOfMembers * (ulong)256;
                        int maxMemberIterations = (maxMemberIterationsLong > (ulong)int.MaxValue) ? int.MaxValue : (int)maxMemberIterationsLong;

                        int numberOfPrefabsCreated = 0;

                        prefabOffset = GetPrefabOffset(lbGroupMember);
                        prefabScale = GetPrefabScale(lbGroupMember);
                        modelOffset = GetModelOffset(lbGroupMember, prefabScale);

                        // Check if only filling edges of a zone
                        isZoneEdgeFillEnabled = (lbGroupMember.isZoneEdgeFillTop || lbGroupMember.isZoneEdgeFillBottom || lbGroupMember.isZoneEdgeFillLeft || lbGroupMember.isZoneEdgeFillRight);

                        for (int mItn = 0; mItn < maxMemberIterations && numberOfPrefabsCreated < targetNumberOfMembers; mItn++)
                        {
                            if (lbGroupMember.isPlacedInCentre) { randomOffset = Vector2.zero; }
                            else
                            {
                                // Give members a random offset on x-z axis
                                randomOffset = GetBasePlacementOffset(lbGroupMember);
                            }

                            // Proximity DOES NOT take into account model offset
                            proximityPosition = groupBasePos + prefabOffset;
                            proximityPosition.x += randomOffset.x;
                            proximityPosition.z += randomOffset.y;

                            prefabRotation = GetMemberRotation(lbGroupMember, proximityPosition);

                            prefabPosition = proximityPosition + (prefabRotation * modelOffset);

                            #region Member Proximity

                            if (isCheckProximity && !lbGroupMember.isIgnoreProximityOfOthers)
                            {
                                isMemberPlacementPositionOk = !objectProximitiesList.Exists(msh => (((proximityPosition.x - msh.position.x) * (proximityPosition.x - msh.position.x)) + ((proximityPosition.z - msh.position.z) * (proximityPosition.z - msh.position.z)))
                                                                                                        < (msh.proximity + lbGroupMember.proximityExtent) * (msh.proximity + lbGroupMember.proximityExtent));
                                if (!isMemberPlacementPositionOk) { continue; }
                            }
                            else { isMemberPlacementPositionOk = true; }

                            #endregion

                            #region Member Zones

                            // Loop through the zones
                            for (zoneListIndex = 0; zoneListIndex < lbGroupMember.zoneGUIDList.Count; zoneListIndex++)
                            {
                                // Get the zone from the group using the provided GUID
                                thisZone = lbGroup.zoneList.Find(z => z.GUID == lbGroupMember.zoneGUIDList[zoneListIndex]);
                                // Comparer depends on zone type
                                if (thisZone.zoneType == LBGroupZone.LBGroupZoneType.circle)
                                {
                                    // Circle - check distance to centre
                                    // Compare the distance from the centre of the zone to the placement position
                                    // with the radius of the zone
                                    // Both distances are squared distances for performance
                                    positionInsideZone = ((prefabPosition.x - (thisZone.centrePointX * lbGroup.maxClearingRadius)) * (prefabPosition.x - (thisZone.centrePointX * lbGroup.maxClearingRadius)))
                                        + ((prefabPosition.z - (thisZone.centrePointZ * lbGroup.maxClearingRadius)) * (prefabPosition.z - (thisZone.centrePointZ * lbGroup.maxClearingRadius))) <
                                        thisZone.width * lbGroup.maxClearingRadius * thisZone.width * lbGroup.maxClearingRadius;

                                    // Check the prefab is within the correct distance of the zone edge
                                    if (positionInsideZone && isZoneEdgeFillEnabled)
                                    {
                                        positionInsideZone = ((prefabPosition.x - (thisZone.centrePointX * lbGroup.maxClearingRadius)) * (prefabPosition.x - (thisZone.centrePointX * lbGroup.maxClearingRadius)))
                                            + ((prefabPosition.z - (thisZone.centrePointZ * lbGroup.maxClearingRadius)) * (prefabPosition.z - (thisZone.centrePointZ * lbGroup.maxClearingRadius))) >
                                            ((thisZone.width * lbGroup.maxClearingRadius - lbGroupMember.zoneEdgeFillDistance) * (thisZone.width * lbGroup.maxClearingRadius- lbGroupMember.zoneEdgeFillDistance));
                                    }
                                }
                                else
                                {
                                    // Rectangle - check min and max coordinates
                                    // Currently assumes that the rectangles cannot be rotated in group space
                                    // Check that the position is between min and max coordinate limits
                                    positionInsideZone = (prefabPosition.x > (thisZone.centrePointX - (thisZone.width/2f)) * lbGroup.maxClearingRadius) &&
                                                         (prefabPosition.x < (thisZone.centrePointX + (thisZone.width/2f)) * lbGroup.maxClearingRadius) &&
                                                         (prefabPosition.z > (thisZone.centrePointZ - (thisZone.length/2f)) * lbGroup.maxClearingRadius) &&
                                                         (prefabPosition.z < (thisZone.centrePointZ + (thisZone.length/2f)) * lbGroup.maxClearingRadius);

                                    // If any of Zone Edge Fill sides are enabled, check prefabs are within those edge distances.
                                    if (positionInsideZone && isZoneEdgeFillEnabled)
                                    {
                                        positionInsideZone = (lbGroupMember.isZoneEdgeFillLeft ? (prefabPosition.x < ((thisZone.centrePointX - (thisZone.width / 2f)) * lbGroup.maxClearingRadius) + lbGroupMember.zoneEdgeFillDistance) : false) ||
                                                             (lbGroupMember.isZoneEdgeFillRight ? (prefabPosition.x > ((thisZone.centrePointX + (thisZone.width / 2f)) * lbGroup.maxClearingRadius) - lbGroupMember.zoneEdgeFillDistance) : false) ||
                                                             (lbGroupMember.isZoneEdgeFillBottom ? (prefabPosition.z < ((thisZone.centrePointZ - (thisZone.length / 2f)) * lbGroup.maxClearingRadius) + lbGroupMember.zoneEdgeFillDistance) : false) ||
                                                             (lbGroupMember.isZoneEdgeFillTop ? (prefabPosition.z > ((thisZone.centrePointZ + (thisZone.length / 2f)) * lbGroup.maxClearingRadius) - lbGroupMember.zoneEdgeFillDistance) : false);
                                    }
                                }

                                if (thisZone.zoneMode == LBGroupZone.ZoneMode.OR)
                                {
                                    // OR
                                    if (zoneListIndex == 0) { isMemberPlacementPositionOk = positionInsideZone; }
                                    else { isMemberPlacementPositionOk = isMemberPlacementPositionOk || positionInsideZone; }
                                }
                                else
                                {
                                    // NOT
                                    if (positionInsideZone) { isMemberPlacementPositionOk = false; break; }
                                    else if (zoneListIndex == 0) { isMemberPlacementPositionOk = !positionInsideZone; }
                                }
                            }

                            if (!isMemberPlacementPositionOk) { continue; }

                            #endregion

                            if (lbGroupMember.isKeepPrefabConnection) { newPrefabInstance = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(lbGroupMember.prefab); }
                            else { newPrefabInstance = (GameObject)UnityEngine.Object.Instantiate(lbGroupMember.prefab, prefabPosition, prefabRotation); }

                            if (newPrefabInstance != null)
                            {
                                numberOfPrefabsCreated++;

                                // Trim "(Clone)" from end of instantiated prefab name.
                                if (newPrefabInstance.name.EndsWith("(Clone)")) { newPrefabInstance.name = newPrefabInstance.name.Substring(0, newPrefabInstance.name.Length - 7); }

                                // Add a component to keep track of meta data about this prefab in the designer
                                LBGroupDesignerItem lbGroupDesignerItem = newPrefabInstance.AddComponent(typeof(LBGroupDesignerItem)) as LBGroupDesignerItem;
                                if (lbGroupDesignerItem == null) { Debug.LogWarning("ERROR: " + methodName + " - could not add LBGroupDesignerItem to " + lbGroup.groupName + " member " + (gmIdx + 1) + ". Please Report"); }
                                else
                                {
                                    lbGroupDesignerItem.GroupMemberGUID = lbGroupMember.GUID;
                                    lbGroupDesignerItem.lbGroupMember = lbGroupMember;
                                    lbGroupDesignerItem.lbGroupDesigner = this;
                                    lbGroupDesignerItem.isSubGroup = false;
                                    lbGroupDesignerItem.basePlacementOffset.x = randomOffset.x;
                                    lbGroupDesignerItem.basePlacementOffset.z = randomOffset.y;

                                    //Debug.Log("INFO Desginer " + lbGroup.groupName + " member " + (gmIdx + 1) + " pos: " + prefabPosition);

                                    if (lbGroupMember.isKeepPrefabConnection)
                                    {
                                        newPrefabInstance.transform.position = prefabPosition;
                                        newPrefabInstance.transform.localRotation = prefabRotation;
                                    }

                                    // Scale the prefab for adding it as a child of the plane (else we'd need to consider the size of the plane)
                                    newPrefabInstance.transform.localScale = prefabScale;

                                    if (!lbGroupMember.isProximityIgnoredByOthers)
                                    {
                                        LBObjectProximity lbObjectProximity = new LBObjectProximity(proximityPosition, lbGroupMember.proximityExtent);
                                        if (lbObjectProximity != null) { objectProximitiesList.Add(lbObjectProximity); }
                                    }

                                    newPrefabInstance.transform.SetParent(grpBaseTrfm);
  
                                    // Workaround for Tools.pivotMode == PivotMode.Center having incorrect prefab centre
                                    // before the prefab is moved in the scene (PivotMode.Pivot and previous versions work fine).
                                    #if UNITY_2018_2_OR_NEWER
                                    if (lbGroupMember.isKeepPrefabConnection)
                                    {
                                        newPrefabInstance.transform.position += nudgeUp;
                                        newPrefabInstance.transform.position -= nudgeUp;
                                    }
                                    #endif
                                }

                                newPrefabInstance = null;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Refresh a single Object Path
        /// </summary>
        /// <param name="objPathGroup"></param>
        /// <param name="objPathGroupMember"></param>
        public void RefreshObjPath(LBGroup objPathGroup, LBGroupMember objPathGroupMember)
        {
            if (objPathGroupMember != null && objPathGroupMember.lbMemberType == LBGroupMember.LBMemberType.ObjPath && objPathGroupMember.lbObjPath != null)
            {
                // Setup ObjPath variables
                LBGroupParameters lbGroupParm = new LBGroupParameters();

                if (lbGroupParm != null)
                {
                    lbGroupParm.showErrors = true;
                    lbGroupParm.showProgress = false;
                    lbGroupParm.showProgressDelegate = null;
                    lbGroupParm.landscape = this.GetComponentInParent<LBLandscape>();
                    lbGroupParm.isGroupDesignerEnabled = true;
                    lbGroupParm.designerOffsetY = BasePlaneOffsetY;

                    lbGroupParm.lbObjPathParm = new LBObjPathParameters();
                    lbGroupParm.lbObjPathParm.lbGroupOwner = objPathGroup;
                    lbGroupParm.lbObjPathParm.lbGroupMemberOwner = objPathGroupMember;
                    lbGroupParm.lbObjPathParm.prefabItemType = LBPrefabItem.PrefabItemType.ObjPathDesignerPrefab;

                    // This could be a little expensive
                    LBLandscapeTerrain.RemoveExistingPrefabs(this.GetComponentInParent<LBLandscape>(), true, LBPrefabItem.PrefabItemType.ObjPathDesignerPrefab, objPathGroupMember.GUID);

                    LBLandscapeTerrain.PopulateLandscapeWithGroups(lbGroupParm);
                }
            }
        }

        /// <summary>
        /// Remove all objects the have been placed on the 
        /// designer plane.
        /// </summary>
        /// <param name="removeGroupBase"></param>
        public void ClearAllObjects(bool removeGroupBase)
        {
            if (grpBaseTrfm != null)
            {
                // Temporarily disable isInitialised
                bool isInitialisedState = isInitialised;
                isInitialised = false;

                DestroyImmediate(grpBaseTrfm.gameObject);

                // This could be a little expensive
                LBLandscapeTerrain.RemoveExistingPrefabs(this.GetComponentInParent<LBLandscape>(), true, LBPrefabItem.PrefabItemType.ObjPathDesignerPrefab, null);

                // If we are not intending to remove the GroupBase,
                // recreate it.
                if (!removeGroupBase)
                {
                    CreateGroupBase();
                    CreateBasePlane();
                    CreateCeiling();
                    InitialiseZones();

                    CreateCameraPlane(transform.parent);
                }

                // Restore isInitialised state
                isInitialised = isInitialisedState;
            }
        }

        /// <summary>
        /// Update the group members in the Designer with values of the LB Editor Window
        /// </summary>
        public void UpdateGroupMembersAll()
        {
            if (lbGroup != null && lbGroup.groupMemberList != null)
            {
                int numGroupMembers = lbGroup.groupMemberList.Count;
                for (int gmIdx = 0; gmIdx < numGroupMembers; gmIdx++) { UpdateGroupMember(lbGroup.groupMemberList[gmIdx]); }
            }
        }

        /// <summary>
        /// Update the group member in the Designer with values of the LB Editor Window
        /// </summary>
        /// <param name="lbGroupMember"></param>
        public void UpdateGroupMember(LBGroupMember lbGroupMember)
        {
            if (lbGroupMember != null)
            {
                LBGroupDesignerItem lbGroupDesignerItem = null;
                List<LBGroupDesignerItem> lbGroupDesignerItemList = GetGroupDesignerItemAll(lbGroupMember);
                if (lbGroupDesignerItemList != null)
                {
                    int numItems = lbGroupDesignerItemList.Count;
                    Vector3 prefabPosition, prefabOffset, prefabScale, modelOffset;

                    for (int itemIdx = 0; itemIdx < numItems; itemIdx++)
                    {
                        lbGroupDesignerItem = lbGroupDesignerItemList[itemIdx];

                        Transform prefab = lbGroupDesignerItem.transform;
                        if (prefab != null)
                        {
                            prefabOffset = GetPrefabOffset(lbGroupMember);
                            prefabScale = GetPrefabScale(lbGroupMember);
                            modelOffset = GetModelOffset(lbGroupMember, prefabScale);

                            prefabPosition = grpBaseTrfm.position + prefabOffset + modelOffset;

                            if (!lbGroupMember.isPlacedInCentre)
                            {
                                // If switching from isPlacedInCentre, generate some random offsets
                                if (lbGroupDesignerItem.basePlacementOffset == Vector3.zero) { SetBasePlacementOffset(lbGroupDesignerItem); }

                                prefabPosition.x += lbGroupDesignerItem.basePlacementOffset.x;
                                prefabPosition.z += lbGroupDesignerItem.basePlacementOffset.z;
                            }

                            prefab.localRotation = GetMemberRotation(lbGroupMember, prefabPosition);

                            // Assume universal scaling on xyz axis
                            prefab.localScale = prefabScale;
                            prefab.position = prefabPosition;
                        }
                    }

                    // If the rotation has been changed in the designer AND the rotation has been changed by the user in the LB Editor Window,
                    // the rotation tool in the scene still has the old rotation values. To avoid this, turn off the rotation tool.
                    #if UNITY_2017_3_OR_NEWER
                    if (numItems > 0 && (Tools.current == Tool.Rotate || Tools.current == Tool.Transform))
                    #else
                    if (numItems > 0 && Tools.current == Tool.Rotate)
                    #endif
                    {
                        Tools.current = Tool.None;
                    }
                }
            }
        }

        /// <summary>
        /// Find the first LBGroupDesignerItem for a given group member 
        /// </summary>
        /// <param name="lbGroupMember"></param>
        /// <returns></returns>
        public LBGroupDesignerItem GetGroupDesignerItem(LBGroupMember lbGroupMember)
        {
            LBGroupDesignerItem lbGroupDesignerItem = null;

            if (grpBaseTrfm != null)
            {
                List<LBGroupDesignerItem> lbGroupDesignerItemList = new List<LBGroupDesignerItem>();

                grpBaseTrfm.GetComponentsInChildren(lbGroupDesignerItemList);

                if (lbGroupDesignerItemList != null)
                {
                    lbGroupDesignerItem = lbGroupDesignerItemList.Find(gItem => gItem.GroupMemberGUID == lbGroupMember.GUID);
                }
            }

            return lbGroupDesignerItem;
        }

        /// <summary>
        /// Find all the GroupDesignerItems with a given group member.
        /// </summary>
        /// <param name="lbGroupMember"></param>
        /// <returns></returns>
        public List<LBGroupDesignerItem> GetGroupDesignerItemAll(LBGroupMember lbGroupMember)
        {
            List<LBGroupDesignerItem> lbGroupDesignerItemList = null;

            if (grpBaseTrfm != null)
            {
                lbGroupDesignerItemList = new List<LBGroupDesignerItem>();

                grpBaseTrfm.GetComponentsInChildren(lbGroupDesignerItemList);

                if (lbGroupDesignerItemList != null)
                {
                    lbGroupDesignerItemList = lbGroupDesignerItemList.FindAll(gItem => gItem.GroupMemberGUID == lbGroupMember.GUID);
                }
            }

            return lbGroupDesignerItemList;
        }

        /// <summary>
        /// Prevent AddPrefab from adding a prefab for the next x milliseconds.
        /// </summary>
        /// <param name="ms"></param>
        public void AddPrefabBlock(float mseconds)
        {
            blockAddPrefabUntilTime = Time.realtimeSinceStartup + (mseconds / 1000f);
        }

        /// <summary>
        /// User is attempting to add a new prefab to the group. They are probably dropping it from the Project folder
        /// into the scene view. By default add it to the centre with the xz offset based on where it is dropped.
        /// We assume that the parent gameobject of a prefab has been scaled uniformly on xyz axis. If not, the user
        /// will need to create a parent gameobject for the prefab with uniform scaling.
        /// </summary>
        /// <param name="newPrefab"></param>
        public void AddPrefab(Transform newPrefab)
        {
            if (isInitialised && newPrefab != null && grpBasePlaneTrfm != null && !isAddingPrefab && !isRefreshingWorkspace && Time.realtimeSinceStartup > blockAddPrefabUntilTime)
            {
                //Debug.Log("AddPrefab..." + Time.realtimeSinceStartup);

                // This code can only be run once at a time, or else when a user drops a new prefab into the designer,
                // multiple copies could get created.
                isAddingPrefab = true;

                // Check to see if this is already assigned in the Designer
                Component designerItem = newPrefab.GetComponent(typeof(LBGroupDesignerItem));

                // Is the user attempting to add another prefab too soon?
                // To avoid duplicates, check nothing was added in the last 2 second
                if ((!float.IsInfinity(lastAddPrefabTime) && Time.realtimeSinceStartup - lastAddPrefabTime < 2.0f))
                {
                    // If this is an existing group object, don't delete it!
                    // This can happen in 2019.3+ when a member is selected in the GroupDesigner and the camera plane
                    // and/or groupdesigner transform is modified in code.
                    if (designerItem == null)
                    {
                        Undo.DestroyObjectImmediate(newPrefab.gameObject);
                        Selection.activeObject = null;
                    }
                    isAddingPrefab = false;
                    //Debug.Log("Trying to add another prefab too soon in Group Designer but isAddingPreb is false");
                    return;
                }

                lastAddPrefabTime = Time.realtimeSinceStartup;

                if (designerItem == null)
                {
                    // Is this a prefab?
                    #if UNITY_2018_3_OR_NEWER
                    UnityEditor.PrefabAssetType prefabAssetType = UnityEditor.PrefabUtility.GetPrefabAssetType(newPrefab);
                    //Debug.Log("Designer newPrefab is " + newPrefab.name + " prefabAssetType:" + prefabAssetType);
                    if (prefabAssetType != UnityEditor.PrefabAssetType.NotAPrefab)
                    #else
                    UnityEditor.PrefabType prefabType = UnityEditor.PrefabUtility.GetPrefabType(newPrefab);
                    //Debug.Log("Designer newPrefab is " + newPrefab.name + " type:" + prefabType);

                    // PrefabInstance = user probably dragged from Project folder into Scene or Hierarchy
                    // Preb = User created prefab. ModelPrefab = imported 3D model asset
                    if (prefabType == UnityEditor.PrefabType.PrefabInstance || prefabType == PrefabType.ModelPrefabInstance || prefabType == UnityEditor.PrefabType.Prefab || prefabType == UnityEditor.PrefabType.ModelPrefab)
                    #endif
                    {
                        //Debug.Log("Adding prefab: " + newPrefab.name);
                        GameObject prefabSource = LBEditorHelper.GetPrefabSource(newPrefab.gameObject);

                        //#if UNITY_2018_2_OR_NEWER
                        //GameObject prefabSource = (GameObject)UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(newPrefab.gameObject);
                        //#else
                        //GameObject prefabSource = (GameObject)UnityEditor.PrefabUtility.GetPrefabParent(newPrefab.gameObject);
                        //#endif
                        if (prefabSource != null)
                        {
                            // Check that it has been added within the radius of the clearing (within the black line of on the base plane)
                            float distanceToCentre = Vector2.Distance(grpBasePlaneCentre2D, new Vector2(newPrefab.position.x, newPrefab.position.z));

                            if (distanceToCentre <= lbGroup.maxClearingRadius)
                            {
                                LBGroupMember lbGroupMember = new LBGroupMember();
                                if (lbGroupMember != null)
                                {
                                    lbGroupMember.prefab = prefabSource;
                                    lbGroupMember.prefabName = prefabSource.name;
                                    lbGroupMember.isPlacedInCentre = true;
                                    lbGroupMember.isKeepPrefabConnection = true;
                                    lbGroupMember.proximityExtent = 2f;
                                    // Manual and Procedural groups are randomly rotated in the scene, so by default
                                    // we want the member prefabs to face the same way when populating the scene.
                                    lbGroupMember.rotationType = LBGroupMember.LBRotationType.GroupSpace;
                                    lbGroupMember.minOffsetX = newPrefab.position.x;
                                    lbGroupMember.minOffsetZ = newPrefab.position.z;
                                    lbGroupMember.minScale = newPrefab.transform.localScale.x;
                                    lbGroupMember.maxScale = lbGroupMember.minScale;

                                    if (lbGroupMember.minScale != lbGroup.minScale || lbGroupMember.maxScale != lbGroup.maxScale)
                                    {
                                        lbGroupMember.isGroupOverride = true;
                                        // Copy Group non-scaling defaults to the member
                                        lbGroupMember.minHeight = lbGroup.minHeight;
                                        lbGroupMember.maxHeight = lbGroup.maxHeight;
                                        lbGroupMember.minInclination = lbGroup.minInclination;
                                        lbGroupMember.maxInclination = lbGroup.maxInclination;
                                    }

                                    lbGroupMember.randomiseRotationY = false;
                                    lbGroupMember.startRotationY = newPrefab.rotation.eulerAngles.y;

                                    if (lbGroup != null && lbGroup.groupMemberList != null)
                                    {
                                        // To guarantee drop placement, insert to being of the list. This means
                                        // it will get placed exactly where the user dropped it into the designer
                                        lbGroup.groupMemberList.Insert(0, lbGroupMember);
                                    }

                                    // Estimate the proximity of prefabs.
                                    lbGroupMember.UpdateProximity(LBMeshOperations.GetBounds(newPrefab.transform, false, true));

                                    //Debug.Log("Destroying originally dropped object");
                                    Selection.activeObject = null;

                                    Undo.DestroyObjectImmediate(newPrefab.gameObject);

                                    // Collapse all other members and show the new one added
                                    lbGroup.GroupMemberListExpand(false);
                                    lbGroupMember.showInEditor = true;
                                    LBEditorHelper.RepaintLBW();

                                    // If autorefresh is off, instantiate just this member
                                    if (!autoRefreshGroupDesigner)
                                    {
                                        RefreshObjects(lbGroupMember);
                                    }
                                    else
                                    {
                                        RefreshWorkspace(true);
                                    }
                                }
                            }
                            else
                            {
                                // When in the Designer, don't allow users to add prefabs outside the designer surface
                                Undo.DestroyObjectImmediate(newPrefab.gameObject);
                                Debug.LogWarning("Please place prefab within the designer area or close the designer to add other items to the scene");
                            }
                        }
                    }
                }
                isAddingPrefab = false;
            }
        }

        /// <summary>
        /// Zoom out to see the whole group in the Designer
        /// </summary>
        /// <param name="sceneView"></param>
        public void ZoomExtent(SceneView sceneView)
        {
            if (sceneView != null && grpBasePlaneTrfm != null)
            {
                Camera svCamera = sceneView.camera;
                if (svCamera != null)
                {
                    Vector3 pivotPosition = grpBaseTrfm.position + (Vector3.up * lbGroup.maxClearingRadius);
                    pivotPosition.z = -cubeLimitsSize * 0.5f;

                    //Debug.Log("ZoomExtent pivotPosition: " + pivotPosition + " cubeLimitsSize: " + cubeLimitsSize);

                    svCamera.transform.position = Vector3.zero;
                    svCamera.transform.rotation = Quaternion.identity;
                    sceneView.pivot = pivotPosition;
                    sceneView.rotation = Quaternion.Euler(0f, 0f, 0f);

                    // The near and far clip plane of the scene view camera are automatically set internally by Unity
                    // based on the focal point.

                    // Attempt to force scene view camera to focus on correct object
                    Object selected = Selection.activeObject;
                    Selection.activeObject = grpBaseTrfm;

                    sceneView.LookAt(grpBaseTrfm.position, Quaternion.Euler(30f, 0f, 0f));
                    // Restore previously selected
                    Selection.activeObject = selected;
                }
            }
        }

        /// <summary>
        /// Attempt to zoom in on a selected object
        /// </summary>
        /// <param name="sceneView"></param>
        public void ZoomIn(SceneView sceneView)
        {
            if (sceneView != null && grpBasePlaneTrfm != null)
            {
                Camera svCamera = sceneView.camera;
                if (svCamera != null)
                {
                    Transform selected = Selection.activeTransform;
                    Vector3 pos = (selected != null ? selected.position : grpBasePlaneTrfm.position);

                    bool isLookAtMember = false;

                    // Did the user attempt to zoom in on a Member object?
                    if (pos.x != 0f && pos.y != 0f && pos.z != 0f && selected != null && selected.GetInstanceID() != grpBasePlaneTrfm.GetInstanceID())
                    {
                        isLookAtMember = (selected.GetComponent<LBGroupDesignerItem>() != null);                     
                    }

                    // The near and far clip plane of the scene view camera are automatically set internally by Unity
                    // based on the focal point.

                    // Did the user attempt to zoom in on a Member object?
                    if (isLookAtMember)
                    {
                        // Place the scene view camera 5m above the position of the object
                        Vector3 pivotPosition = pos + (Vector3.up * 5f);
                        // Set the camera 10m infront of the object
                        pivotPosition.z -= 10f;
                        sceneView.pivot = pivotPosition;
                        sceneView.LookAt(pos, Quaternion.Euler(0f, 0f, 0f));
                    }
                    else
                    {

                        Vector3 pivotPosition = grpBaseTrfm.position + (Vector3.up * 5f);
                        pivotPosition.z = -cubeLimitsSize * 0.2f;

                        sceneView.pivot = pivotPosition;
                        sceneView.rotation = Quaternion.Euler(0f, 0f, 0f);

                        // Attempt to force scene view camera to focus on correct object
                        Selection.activeTransform = grpBaseTrfm;
                        sceneView.LookAt(pos, Quaternion.Euler(30f, 0f, 0f));
                    }

                    // Restore previously selected
                    Selection.activeTransform = selected;
                }
            }
        }

        public void CloseObjPathDesigner(LBObjPathDesigner lbObjPathDesigner, LBGroupMember lbGroupMember)
        {
            LBLandscape landscape = this.GetComponentInParent<LBLandscape>();

            if (landscape != null)
            {
                LBLandscapeTerrain.RemoveExistingPrefabs(landscape, true, LBPrefabItem.PrefabItemType.ObjPathDesignerPrefab, null);
                LBObjPathDesigner.ShowDesigner(landscape, ref lbObjPathDesigner, lbGroup, lbGroupMember, null, false);
            }
        }

        /// <summary>
        /// Update the setting for auto refresh in the GroupDesigner instance and in the landscape
        /// </summary>
        /// <param name="isAutoRefreshEnabled"></param>
        public void SetAutoRefresh(bool isAutoRefreshEnabled)
        {
            // Has the value changed?
            if (autoRefreshGroupDesigner != isAutoRefreshEnabled)
            {
                autoRefreshGroupDesigner = isAutoRefreshEnabled;

                LBLandscape landscape = this.GetComponentInParent<LBLandscape>();
                if (landscape != null) { landscape.autoRefreshGroupDesigner = isAutoRefreshEnabled; }
            }
        }

        #endregion

    }
}
#endif
