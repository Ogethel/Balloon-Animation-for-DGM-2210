#if UNITY_EDITOR
// Landscape Builder. Copyright (c) 2016-2019 SCSM Pty Ltd. All rights reserved.
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEditor;
using System.IO;

namespace LandscapeBuilder
{
    public class LBEditorHelper
    {
        #region Enumerations

        /// <summary>
        /// Unity doesn't seem to expose the names of Gizmos
        /// So we keep an enumeration here. To add to the list
        /// call the following method. The value (like 59) is
        /// one of the values returned by the output.
        ///  OutputGizmos(true, 59, true)
        /// </summary>
        public enum UnityGizmos
        {
            Camera = 20,
            OcclusionPortal = 41,
            CircleCollider2D = 58,
            HingeJoint = 59,
            PolygonCollider2D = 60,
            BoxCollider2D = 61,
            MeshCollider = 64,
            BoxCollider = 65,
            EdgeCollider2D = 68,
            AudioSource = 82,
            Animator = 95,
            Light = 108,
            Animation = 111,
            Projector = 119,
            LensFlare = 123,
            SphereCollider = 135,
            CapsuleCollider = 136,
            SkinnedMeshRenderer = 137,
            CharacterController = 143,
            CharacterJoint = 144,
            SpringJoint = 145,
            WheelCollider = 146,
            ConfigurableJoint = 153,
            AudioReverbZone = 167,
            Windzone = 182,
            Cloth = 183,
            OcclusionArea = 192,
            Tree = 193,
            NavMeshAgent = 195,
            ParticleSystem = 198,
            LODGroup = 205,
            NavMeshObstacle = 208,
            ReflectionProbe = 215,
            Terrain = 218,
            LightProbeGroup = 220,
            Canvas = 223,
            SpingJoint2D = 231,
            DistanceJoint2D = 232,
            HingeJoint2D = 233,
            SliderJoint2D = 234,
            WheelJoint2D = 235,
            PlatformEffector2D = 251,
            BuoyancyEffector2D = 253,
            RelativeJoint2D = 254,
            FixedJoint2D = 255,
            FrictionJoint2D = 256,
            TargetJoint2D = 257
        }

        #endregion

        #region Common Folders

        public static string GetDefaultTexturesFolder { get { return "Assets/LandscapeBuilder/Textures"; } }
        public static string GetDefaultHeightmapFolder { get { return "Assets/LandscapeBuilder/Heightmaps"; } }
        public static string GetDefaultEditorTexturesFolder { get { return "Assets/LandscapeBuilder/Editor/Textures/"; } }

        #endregion

        #region Gizmo Methods

        /// <summary>
        /// Get the UnityEditor.AnnotationUtility type. This is used for managing Gizmos
        /// </summary>
        /// <returns></returns>
        public static System.Type GetAnnotationUtilityType()
        {
            System.Type annotationUtilityType = null;

            System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(typeof(Editor));
            annotationUtilityType = asm.GetType("UnityEditor.AnnotationUtility");

            return annotationUtilityType;
        }

        public static void OutputGizmos(bool isEnableGizmo, int gizmoClassID, bool outputRawValues)
        {
            int val = isEnableGizmo ? 1 : 0;

            System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(typeof(Editor));
            Type annotationUtilityType = asm.GetType("UnityEditor.AnnotationUtility");
            if (annotationUtilityType != null)
            {
                System.Reflection.MethodInfo getAnnotations = annotationUtilityType.GetMethod("GetAnnotations", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                System.Reflection.MethodInfo setGizmoEnabled = annotationUtilityType.GetMethod("SetGizmoEnabled", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                System.Reflection.MethodInfo setIconEnabled = annotationUtilityType.GetMethod("SetIconEnabled", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                var annotations = getAnnotations.Invoke(null, null);
                foreach (object annotation in (IEnumerable)annotations)
                {
                    Type annotationType = annotation.GetType();

                    //LBIntegration.ReflectionOutputFields(annotationType, true, true);
                    //LBIntegration.ReflectionOutputMethods(annotationType, false, true, true);

                    System.Reflection.FieldInfo classIdField = annotationType.GetField("classID", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    System.Reflection.FieldInfo scriptClassField = annotationType.GetField("scriptClass", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (classIdField != null && scriptClassField != null)
                    {
                        int classId = (int)classIdField.GetValue(annotation);


                        // Scripts in the scene may have icons or gizmos attached to them
                        // Built-in components typically don't have a scriptClass value.
                        string scriptClass = (string)scriptClassField.GetValue(annotation);

                        if (outputRawValues)
                        {
                            // Display a list of the classId. Don't know of any way to get the actual Gizmo name
                            Debug.Log("Gizmo script: " + scriptClass + " id:" + classId.ToString() + " name: " + classIdField.Name);
                        }

                        if (classId == gizmoClassID)
                        {
                            setGizmoEnabled.Invoke(null, new object[] { classId, scriptClass, val });
                            setIconEnabled.Invoke(null, new object[] { classId, scriptClass, val });
                        }
                    }
                }
            }
        }

        public static void ToggleGizmos(List<int> gizmoClassIdList, bool isEnableGizmo)
        {
            int val = isEnableGizmo ? 1 : 0;

            System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(typeof(Editor));
            Type annotationUtilityType = asm.GetType("UnityEditor.AnnotationUtility");
            if (annotationUtilityType != null)
            {
                System.Reflection.MethodInfo getAnnotations = annotationUtilityType.GetMethod("GetAnnotations", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                System.Reflection.MethodInfo setGizmoEnabled = annotationUtilityType.GetMethod("SetGizmoEnabled", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                System.Reflection.MethodInfo setIconEnabled = annotationUtilityType.GetMethod("SetIconEnabled", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                var annotations = getAnnotations.Invoke(null, null);
                foreach (object annotation in (IEnumerable)annotations)
                {
                    Type annotationType = annotation.GetType();
                    System.Reflection.FieldInfo classIdField = annotationType.GetField("classID", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    System.Reflection.FieldInfo scriptClassField = annotationType.GetField("scriptClass", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (classIdField != null && scriptClassField != null)
                    {
                        int classId = (int)classIdField.GetValue(annotation);

                        // Scripts in the scene may have icons or gizmos attached to them
                        // Built-in components typically don't have a scriptClass value.
                        string scriptClass = (string)scriptClassField.GetValue(annotation);

                        // Is this Gizmo in the list we wish to enable/disable?
                        if (gizmoClassIdList.FindIndex(g => g == classId) >= 0)
                        {
                            setGizmoEnabled.Invoke(null, new object[] { classId, scriptClass, val });
                            setIconEnabled.Invoke(null, new object[] { classId, scriptClass, val });
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get or Set the Gizmos 3D Icons in the Scene View 
        /// </summary>
        public static bool GizmoUse3DIcons
        {
            get
            {
                System.Reflection.PropertyInfo pInfo = GizmoUse3DIconsPropInfo;
                if (pInfo != null) { return (bool)pInfo.GetValue(null, null); }
                return false;
            }
            set
            {
                System.Reflection.PropertyInfo pInfo = GizmoUse3DIconsPropInfo;
                if (pInfo != null) { pInfo.SetValue(null, value, null); }
            }
        }

        /// <summary>
        /// Get or Set the Gizmos Show Selection Outline in the Scene View 
        /// Available in Unity 5.5+. Earlier versions will Get false, or take
        /// no action for Set operations.
        /// </summary>
        public static bool GizmoShowSelectionOutline
        {
            get
            {
                System.Reflection.PropertyInfo pInfo = GizmoShowSelectionOutlinePropInfo;
                if (pInfo != null) { return (bool)pInfo.GetValue(null, null); }
                return false;
            }
            set
            {
                System.Reflection.PropertyInfo pInfo = GizmoShowSelectionOutlinePropInfo;
                if (pInfo != null) { pInfo.SetValue(null, value, null); }
            }
        }

        /// <summary>
        /// Get the PropertyInfo for the Gizmo use 3D Icons
        /// </summary>
        /// <returns></returns>
        private static System.Reflection.PropertyInfo GizmoUse3DIconsPropInfo
        {
            get
            {
                System.Type annotationUtilityType = GetAnnotationUtilityType();
                if (annotationUtilityType != null)
                {
                    return annotationUtilityType.GetProperty("use3dGizmos", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                }
                else return null;
            }
        }

        /// <summary>
        /// Get the PropertyInfo for the Gizmo Show Selection Outline.
        /// Available in Unity 5.5+. Earlier versions will return null
        /// </summary>
        private static System.Reflection.PropertyInfo GizmoShowSelectionOutlinePropInfo
        {
            get
            {
                System.Type annotationUtilityType = GetAnnotationUtilityType();
                if (annotationUtilityType != null)
                {
#if UNITY_5_5_OR_NEWER
                    return annotationUtilityType.GetProperty("showSelectionOutline", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
#else
                return null;
#endif
                }
                else return null;
            }
        }

        #endregion

        #region LB Editor Window

        public static EditorWindow GetLBW()
        {
            System.Type LBWType = null;
            EditorWindow lbw = null;

            try
            {
                LBWType = System.Type.GetType("LandscapeBuilder.LandscapeBuilderWindow, Assembly-CSharp-Editor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", true, true);
                if (LBWType != null)
                {
                    lbw = UnityEditor.EditorWindow.GetWindow(LBWType, false);
                }
                else { Debug.Log("LBEditorHelper - could not open Landscape Builder Editor Window. Please Report"); }
            }
            catch (Exception ex)
            {
                Debug.Log("LBEditorHelper - could not open Landscape Builder Editor Window. Please Report. " + ex.Message);
            }
            return lbw;
        }

        /// <summary>
        /// Set focus to the Landscape Builder Editor Window
        /// Open one if it is not already open
        /// </summary>
        public static void SetFocusLBW()
        {
            System.Type LBWType = null;

            try
            {
                LBWType = System.Type.GetType("LandscapeBuilder.LandscapeBuilderWindow, Assembly-CSharp-Editor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", true, true);
                if (LBWType != null)
                {
                    UnityEditor.EditorWindow.GetWindow(LBWType, false);
                }
                else { Debug.Log("LBEditorHelper - could not open Landscape Builder Editor Window. Please Report"); }
            }
            catch (Exception ex)
            {
                Debug.Log("LBEditorHelper - could not open Landscape Builder Editor Window. Please Report. " + ex.Message);
            }
        }

        /// <summary>
        /// Repaint the Landscape Builder window. Typically called from a non-Editor script.
        /// From an editor script, use RepaintEditorWindow(typeof(LandscapeBuilderWindow));
        /// </summary>
        public static void RepaintLBW()
        {
            System.Type LBWType = null;

            try
            {
                LBWType = System.Type.GetType("LandscapeBuilder.LandscapeBuilderWindow, Assembly-CSharp-Editor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", true, true);
                if (LBWType != null)
                {
                    EditorWindow editorWindow = UnityEditor.EditorWindow.GetWindow(LBWType);
                    if (editorWindow != null) { editorWindow.Repaint(); }
                }
                else { Debug.Log("LBEditorHelper.RepaintLBW - could not open Landscape Builder Editor Window. Please Report"); }
            }
            catch (Exception ex)
            {
                Debug.Log("LBEditorHelper.RepaintLBW - could not open Landscape Builder Editor Window. Please Report. " + ex.Message);
            }
        }

        #endregion

        #region Editor Windows

        /// <summary>
        /// Given an EditorWindow type, force a repaint on the window
        /// USAGE LBEditorHelper.RepaintEditorWindow(typeof(LandscapeBuilderWindow));
        /// </summary>
        /// <param name="editorWindowType"></param>
        public static void RepaintEditorWindow(System.Type editorWindowType)
        {
            if (editorWindowType != null)
            {
                EditorWindow editorWindow = UnityEditor.EditorWindow.GetWindow(editorWindowType);
                if (editorWindow != null) { editorWindow.Repaint(); }
            }
        }

        /// <summary>
        /// Saves a YAML file containing the Editor Layout into the LandscapeBuilder folder (which is outside
        /// the Assets project folder).
        /// </summary>
        /// <param name="layoutName"></param>
        public static void SaveEditorLayout(string layoutName)
        {
            Type windowLayout = LBIntegration.GetClassTypeFromFullName("UnityEditor.WindowLayout, UnityEditor", true);

            if (windowLayout != null && !string.IsNullOrEmpty(layoutName))
            {
                string filePath = "LandscapeBuilder/" + layoutName + ".wlt";
                try
                {
                    windowLayout.InvokeMember("SaveWindowLayout", System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, null, null, new object[] { filePath });
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("ERROR: LBEditorHelper.SaveEditorLayout " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Loads the Editor layout YAML file from a previously saved file in the LandscapeBuilder folder (which is outside
        /// the Asset project folder).
        /// </summary>
        /// <param name="layoutName"></param>
        public static void LoadEditorLayout(string layoutName)
        {
            Type windowLayout = LBIntegration.GetClassTypeFromFullName("UnityEditor.WindowLayout, UnityEditor", true);

            if (windowLayout != null && !string.IsNullOrEmpty(layoutName))
            {
                string filePath = "LandscapeBuilder/" + layoutName + ".wlt";
                try
                {
                    windowLayout.InvokeMember("LoadWindowLayout", System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, null, null, new object[] { filePath, true });
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("ERROR: LBEditorHelper.LoadEditorLayout " + ex.Message);
                }
            }
        }

        #endregion

        #region Menu Helper Methods

        /// <summary>
        /// Call an item from the Unity menu. Menu can also be one custom created.
        /// USAGE: LBEditorHelper.CallMenu("Edit/Project Settings/Player");
        /// </summary>
        /// <param name="menuItemPath"></param>
        public static void CallMenu(string menuItemPath)
        {
            if (!string.IsNullOrEmpty(menuItemPath))
            {
                EditorApplication.ExecuteMenuItem(menuItemPath);
            }
        }

        #endregion

        #region SceneView Helper Methods

        /// <summary>
        /// Refresh the last scene view used
        /// USAGE: LBEditorHelper.RefreshSceneView();
        /// </summary>
        public static void RefreshSceneView()
        {
            try
            {
                if (SceneView.lastActiveSceneView != null) { SceneView.lastActiveSceneView.Repaint(); }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("LBEditorHelper.RefreshSceneView - Could not repaint scene view\n" + ex.Message);
            }
        }

        /// <summary>
        /// Attempt to display the point in the centre of the sceneview
        /// zoomDistance is the distance in metres to zoom out from the point in the scene
        /// </summary>
        /// <param name="centrePoint"></param>
        /// <param name="zoomDistance"></param>
        /// <param name="sourceType"></param>
        public static void PositionSceneView(Vector3 centrePoint, float zoomDistance, Type sourceType)
        {
            try
            {
                SceneView sceneView = UnityEditor.SceneView.lastActiveSceneView;

                // If the sceneView hasn't had the focus in this session, lastActiveSceneView will return null
                if (sceneView == null)
                {
                    // Get the first scene view window that is open
                    var window = UnityEditor.SceneView.GetWindow(typeof(SceneView));

                    // Try again
                    if (window != null) { sceneView = UnityEditor.SceneView.lastActiveSceneView; }
                }

                if (sceneView != null)
                {
                    sceneView.LookAt(centrePoint);
                    sceneView.size = zoomDistance;
                }
                else { Debug.LogWarning(sourceType.Name + ": Couldn't find active scene view"); }
            }
            catch (System.Exception ex)
            {
                Debug.LogError(sourceType.Name + ": Couldn't position scene view\n" + ex.Message);
            }
        }

        /// <summary>
        /// Frame and object (e.g. a landscape), in the centre of the Scene view window
        /// </summary>
        /// <param name="sceneViewSize"></param>
        /// <param name="sourceType"></param>
        public static void FrameObjectInSceneView(float sceneViewSize, Vector3 rotation, Type sourceType)
        {
            try
            {
                // Switch to the scene view
                #if UNITY_2018_2_OR_NEWER
                CallMenu("Window/General/Scene");
                #else
                CallMenu("Window/Scene");
                #endif

                SceneView sceneView = UnityEditor.SceneView.lastActiveSceneView;

                // If the sceneView hasn't had the focus in this session, lastActiveSceneView will return null
                if (sceneView != null)
                {
                    sceneView.orthographic = false;

                    // Calling Edit menu items is broken in 5.4, 5.5, and 5.6
                    #if UNITY_2017_1_OR_NEWER
                    CallMenu("Edit/Frame Selected");
                    #else
                    sceneView.FrameSelected();
                    #endif

                    sceneView.rotation = Quaternion.Euler(rotation);
                    sceneView.size = sceneViewSize;
                }
                else { Debug.LogWarning("LBEditorHelper.FrameObjectInSceneView " + sourceType.Name + ": The scene view window may not be open"); }
            }
            catch (System.Exception ex)
            {
                Debug.LogError("LBEditorHelper.FrameObjectInSceneView " + sourceType.Name + ": Couldn't position scene view\n" + ex.Message);
            }
        }

        /// <summary>
        /// Show the Scene View (open it if one is not already open)
        /// </summary>
        public static void ShowSceneView()
        {
            // Switch to the scene view
            #if UNITY_2018_2_OR_NEWER
            CallMenu("Window/General/Scene");
            #else
            CallMenu("Window/Scene");
            #endif
        }

        /// <summary>
        /// Show the Scene View (open it if one is not already open)
        /// The soureType is used when displaying any warnings or errors
        /// </summary>
        /// <param name="sourceType"></param>
        public static void ShowSceneView(Type sourceType, bool DockNextToGameView = false)
        {
            try
            {
                if (DockNextToGameView)
                {
                    // Get the first scene view window that is open
                    var window = SceneView.GetWindow(typeof(SceneView));
                    // Now close it (just in case there was an undocked one already open)
                    if (window != null) { window.Close(); }

                    // Get the GameView window type
                    System.Type gameViewType = System.Type.GetType("UnityEditor.GameView,UnityEditor");

                    window = EditorWindow.GetWindow<SceneView>("Scene View", true, gameViewType);
                    // Did the scene view open or get focus?
                    if (window == null)
                    {
                        Debug.LogWarning(sourceType.Name + ": Couldn't open scene view next to Game View");
                    }
                }
                else
                {
                    // Get the first scene view window that is open
                    var window = SceneView.GetWindow(typeof(SceneView));

                    // Did the scene view open or get focus?
                    if (window == null)
                    {
                        Debug.LogWarning(sourceType.Name + ": Couldn't open scene view");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError(sourceType.Name + ": Couldn't open scene view\n" + ex.Message);
            }
        }

        /// <summary>
        /// Get the world space coordinates of the centre of the screen.
        /// Always set Y to 0
        /// </summary>
        /// <param name="sourceType"></param>
        /// <returns></returns>
        public static Vector3 GetCentreSceneView(Type sourceType)
        {
            Vector3 centrePoint = Vector3.zero;

            try
            {
                SceneView sceneView = UnityEditor.SceneView.lastActiveSceneView;

                if (sceneView != null)
                {
                    centrePoint.x = sceneView.pivot.x;
                    centrePoint.z = sceneView.pivot.z;
                }
                else { Debug.LogWarning(sourceType.Name + ": Couldn't find active scene view"); }
            }
            catch (System.Exception ex)
            {
                Debug.LogError(sourceType.Name + ": Couldn't get centre of scene view\n" + ex.Message);
            }

            return centrePoint;
        }

        /// <summary>
        /// Given a mouse position in 2D space, get the 3D Worldspace position on a Transform's meshes. If user didn't click on
        /// a point in the transform, return the Transform position. Return 0,0,0 is something went wrong.
        /// </summary>
        /// <param name="trfm"></param>
        /// <param name="mousePosition"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static Vector3 GetTransformPositionFromMouse(Transform trfm, Vector2 mousePosition, bool showErrors)
        {
            Vector3 locationPoint = Vector3.zero;
            string methodName = "LBEditorHelper.GetLandscapePositionFromMouse";

            if (trfm == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " transform is null"); } }
            else
            {
                // default to the position of the transform
                locationPoint = trfm.position;
                try
                {
                    SceneView sceneView = SceneView.lastActiveSceneView;
                    bool isHit = false;
                    //Vector2 pointXZ = Vector2.zero;

                    if (sceneView != null)
                    {
                        Camera svCamera = sceneView.camera;
                        if (svCamera != null)
                        {
                            // Cast a ray from the scene view camera through the mouse point onto (hopefully) the transform (or the nearest object)
                            // Mouse position Y in screen space is inverted
                            Ray ray = svCamera.ScreenPointToRay(new Vector3(mousePosition.x, svCamera.pixelHeight - mousePosition.y, svCamera.nearClipPlane));
                            RaycastHit hit;
                            if (Physics.Raycast(ray, out hit))
                            {
                                // We hit something
                                locationPoint = hit.point;
                                isHit = true;
                                //pointXZ = new Vector2(locationPoint.x, locationPoint.z);
                            }

                            // Is the mouse point inside the transform? If not, do what....??

                            if (isHit && !LBMeshOperations.GetBounds(trfm, false, showErrors).Contains(locationPoint))
                            {
                                locationPoint = trfm.position;

                                //locationPoint.y = trfm.position.y;
                                //Debug.Log("Found point " + locationPoint);
                            }
                        }
                    }

                }
                catch (System.Exception ex) { Debug.LogWarning(methodName + " - sorry, something went wrong\n" + ex.Message); }
            }
            return locationPoint;
        }

        /// <summary>
        /// Given a mouse position in 2D space, get the 3D Worldspace position on a terrain within the landscape. There is an option to return the centre of the
        /// landscape if user didn't click on a point in the terrain (else it will return 0,0,0).
        /// NOTE: Prior to U2017.3, GUIPointToWorldRay(..) can return different ray directions depending on the event type. This is a
        /// known Unity bug (Issue 932897) and was fixed some time between 2017.2.0f3 and 2017.3.0f3.
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="mousePosition"></param>
        /// <param name="isDefaultLandscapeCentre"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static Vector3 GetLandscapePositionFromMouse(LBLandscape landscape, Vector2 mousePosition, bool isDefaultLandscapeCentre, bool showErrors)
        {
            Vector3 locationPoint = Vector3.zero;
            string methodName = "LBEditorHelper.GetTransformPositionFromMouse";         

            if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " landscape is null"); } }
            else
            {
                landscape.SetLandscapeTerrains(false);
                Rect worldBounds = LBLandscapeTerrain.GetLandscapeWorldBounds(landscape.landscapeTerrains);

                try
                {
                    SceneView sceneView = SceneView.lastActiveSceneView;
                    bool isHit = false;
                    Vector2 pointXZ = Vector2.zero;

                    if (sceneView != null)
                    {
                        Camera svCamera = sceneView.camera;
                        if (svCamera != null)
                        {
                            // Cast a ray from the scene view camera through the mouse point onto (hopefully) the terrain (or the nearest object)
                            // Mouse position Y in screen space is inverted
                            //Ray ray = svCamera.ScreenPointToRay(new Vector3(mousePosition.x, svCamera.pixelHeight - mousePosition.y, svCamera.nearClipPlane));

                            if (Camera.current == null) { Camera.SetupCurrent(svCamera); }

                            Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);

                            RaycastHit hit;
                            if (Physics.Raycast(ray, out hit))
                            {
                                // We hit something
                                locationPoint = hit.point;
                                isHit = true;
                                pointXZ = new Vector2(locationPoint.x, locationPoint.z);
                            }

                            //Debug.Log("[DEBUG] hit " + isHit + " mousePosition: " + mousePosition + " ray " + ray.origin + " dir " + ray.direction);

                            // Is the mouse point inside the landscape? If not, check the centre of the scene view
                            if (isHit && worldBounds.Contains(pointXZ))
                            {
                                // Found a point on a terrain within the landscape
                                locationPoint.y = LBLandscapeTerrain.GetHeight(landscape, pointXZ, false) + landscape.start.y;
                                //Debug.Log("[DEBUG] Found point " + pointXZ);
                            }
                            else
                            {
                                // Check the centre of the scene view OR set to the centre of the landscape
                                pointXZ.x = sceneView.pivot.x;
                                pointXZ.y = sceneView.pivot.z;
                                if (worldBounds.Contains(pointXZ))
                                {
                                    //Debug.Log("Found point (sceneView) " + pointXZ);
                                    locationPoint.x = pointXZ.x;
                                    locationPoint.z = pointXZ.y;
                                    locationPoint.y = LBLandscapeTerrain.GetHeight(landscape, pointXZ, false) + landscape.start.y;
                                }
                                else if (isDefaultLandscapeCentre)
                                {
                                    // set to the centre of the landscape
                                    locationPoint.x = worldBounds.center.x;
                                    locationPoint.z = worldBounds.center.y;
                                    locationPoint.y = LBLandscapeTerrain.GetHeight(landscape, pointXZ, false) + landscape.start.y;
                                    if (showErrors) { Debug.Log("Could not get mouse point on landscape - Adding to the centre of the landscape."); }
                                }
                                else
                                {
                                    if (showErrors) { Debug.Log("Could not get mouse point on landscape"); }
                                }
                            }
                        }
                    }
                }
                catch (System.Exception ex) { Debug.LogWarning(methodName + " - sorry, something went wrong\n" + ex.Message); }
            }

            return locationPoint;
        }

        /// <summary>
        /// Find the bounding rectange for the current SceneView
        /// </summary>
        /// <returns></returns>
        private Rect GetSceneViewRect()
        {
            Rect sceneViewRect = new Rect();
            string methodName = "LBEditorHelper.GetSceneViewRect";

            try
            {
                SceneView sceneView = UnityEditor.SceneView.lastActiveSceneView;

                if (sceneView != null)
                {
                    sceneViewRect = sceneView.position;
                }
                else { Debug.LogWarning(methodName + " - Couldn't find active scene view"); }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning(methodName + " - Couldn't get scene view\n" + ex.Message);
            }

            return sceneViewRect;
        }

        #endregion

        #region GameView Methods

        /// <summary>
        /// Show the Game View (open it if it is not already open)
        /// The soureType is used when displaying any warnings or errors
        /// </summary>
        /// <param name="sourceType"></param>
        public static void ShowGameView(Type sourceType)
        {
            try
            {
                // Get the GameView window type
                System.Type gameViewType = System.Type.GetType("UnityEditor.GameView,UnityEditor");

                // Get the first game view window that is open
                var gameviewWindow = EditorWindow.GetWindow(gameViewType, false, "Game", true);

                // Did the game view open or get focus?
                if (gameviewWindow == null)
                {
                    Debug.LogWarning(sourceType.Name + ": Couldn't open Game view");
                }
                else
                {
                    gameviewWindow.Repaint();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError(sourceType.Name + ": Couldn't open game view\n" + ex.Message);
            }
        }

        /// <summary>
        /// Close the game view window
        /// </summary>
        /// <param name="sourceType"></param>
        public static void CloseGameView(Type sourceType)
        {
            try
            {
                // Get the GameView window type
                System.Type gameViewType = System.Type.GetType("UnityEditor.GameView,UnityEditor");

                // Get the first game view window that is open
                var gameviewWindow = EditorWindow.GetWindow(gameViewType, false, "Game", true);

                // Did the game view open or get focus?
                if (gameviewWindow != null)
                {
                    gameviewWindow.Close();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError(sourceType.Name + ": Could not close game view\n" + ex.Message);
            }
        }

        /// <summary>
        /// Maximize the Game View.
        /// If maximizeWindow = false, maximized for the Game View will be set to false.
        /// Returns the original status of the window (maximized true or false)
        /// </summary>
        /// <param name="sourceType"></param>
        /// <param name="maximizeWindow"></param>
        /// <returns></returns>
        public static bool GameViewMaximize(Type sourceType, bool maximizeWindow)
        {
            bool isMaximised = false;

            try
            {
                // Get the GameView window type
                System.Type gameViewType = System.Type.GetType("UnityEditor.GameView,UnityEditor");

                // Get the first game view window that is open
                var gameviewWindow = EditorWindow.GetWindow(gameViewType, false, "Game", true);

                // Did the game view open or get focus?
                if (gameviewWindow == null)
                {
                    Debug.LogWarning(sourceType.Name + ": Couldn't open Game view");
                }
                else
                {
                    // Store the original status of the window
                    isMaximised = gameviewWindow.maximized;

                    // Set the preferred state
                    gameviewWindow.maximized = maximizeWindow;

                    gameviewWindow.Repaint();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError(sourceType.Name + ": Couldn't open Game view\n" + ex.Message);
            }

            return isMaximised;
        }

        #endregion

        #region Texture Helper Methods

        /// <summary>
        /// Save a map texture to the Project
        /// USAGE: SaveMapTexture(exportHeightMapImage, exportPNGFilePath, landscape.size.x, true);
        /// NOTE: Set isRGBA32 true for exporting to Vegetation Studio
        /// </summary>
        /// <param name="mapTexture"></param>
        /// <param name="mapTexturePath"></param>
        /// <param name="width"></param>
        /// <param name="highlight"></param>
        /// <param name="isRGBA32"></param>
        public static void SaveMapTexture(Texture2D mapTexture, string mapTexturePath, int width, bool highlight = true, bool isRGBA32 = false)
        {
            // create a byte array in PNG format
            byte[] heightMapData = mapTexture.EncodeToPNG();
            // Save the byte array to the disk file
            File.WriteAllBytes(mapTexturePath, heightMapData);
            AssetDatabase.Refresh();
            // Get the new texture so that attributes can be modified
            TextureImporter texImporter = (TextureImporter)AssetImporter.GetAtPath(mapTexturePath);

            #if UNITY_5_5_OR_NEWER
            texImporter.textureType = TextureImporterType.Default;
            #else
            texImporter.textureType = TextureImporterType.Advanced;
            #endif

            // Texture Maps aren't used for rendering, so no need to generate smaller texture Mip Maps
            texImporter.mipmapEnabled = false;

            // Disable normal maps
            texImporter.convertToNormalmap = false;
            // Use TrueColor compression (our import methods don't work with Crunched settings)
            // Some other 8 and 16 bit compression algorithms (includind Unity default Automatic Compressed)
            // return slightly incorrect colour values. For example RGB 0, 153, 0 returns 0, 154, 0.
            // Use RGBA32 for Vegetation Studio masks.
            // NOTE: Unity 5.5 doesn't support textureFormat property.
            
            #if UNITY_5_5_OR_NEWER
            if (isRGBA32)
            {
                TextureImporterPlatformSettings txtPlatSettings = texImporter.GetDefaultPlatformTextureSettings();
                txtPlatSettings.format = TextureImporterFormat.RGBA32;
                texImporter.SetPlatformTextureSettings(txtPlatSettings);
            }
            else { texImporter.textureCompression = TextureImporterCompression.Uncompressed; }
            #else
            if (isRGBA32) { texImporter.textureFormat = TextureImporterFormat.RGBA32; }
            else { texImporter.textureFormat = TextureImporterFormat.AutomaticTruecolor; }  
            #endif

            // Set the size based on the landscape being exported
            if (width > 4096) { texImporter.maxTextureSize = 8192; }
            else if (width > 2048) { texImporter.maxTextureSize = 4096; }
            else if (width > 1024) { texImporter.maxTextureSize = 2048; }
            else if (width > 512) { texImporter.maxTextureSize = 1024; }
            else { texImporter.maxTextureSize = 512; }
            // Make the new image Read/Write
            texImporter.isReadable = true;
            texImporter.SaveAndReimport();  // Added v2.0.2
            AssetDatabase.Refresh();
            // Reveals and selects the image that was just added or overwritten in the Project window
            if (highlight) { HighlightItemInProjectWindow(mapTexturePath); }
        }

        /// <summary>
        /// Get a list of Texture2D images from a project folder
        /// texturePathList = LBEditorHelper.GetTexturePathListFromFolder("Assets/" + pathHQPhotoPackVol1, ".psd");
        /// [EDITOR ONLY]
        /// </summary>
        /// <param name="folderName"></param>
        /// <returns></returns>
        public static List<string> GetTexturePathListFromFolder(string folderName, string filetype)
        {
            List<string> texturePathList = new List<string>();

            string pathToTexture2D = string.Empty;
            string[] lookFor = new string[] { folderName };

            if (!AssetDatabase.IsValidFolder(folderName))
            {
                Debug.LogWarning("LBEditorHelper.GetTextureListFromFolder " + folderName + " not found");
            }
            else
            {
                string[] textureGUIDArray = AssetDatabase.FindAssets("t:texture2D", lookFor);

                if (textureGUIDArray != null)
                {
                    foreach (string guidstr in textureGUIDArray)
                    {
                        pathToTexture2D = AssetDatabase.GUIDToAssetPath(guidstr);
                        if (!string.IsNullOrEmpty(pathToTexture2D))
                        {
                            // Only add it if it matches the file type
                            if (pathToTexture2D.EndsWith(filetype))
                            {
                                texturePathList.Add(pathToTexture2D);
                                //Debug.Log("Texture2D: " + pathToTexture2D);
                            }
                        }
                    }
                    if (texturePathList.Count > 1) { texturePathList.Sort(); }
                }
            }
            return texturePathList;
        }

        /// <summary>
        /// Attempts to load an icon (Texture2D) into the supplied GUIContent
        /// </summary>
        /// <param name="guiContent"></param>
        public static void LoadGUIContentIcon(GUIContent guiContent, string imageName, string callingMethodName)
        {
            if (guiContent != null)
            {
                Texture2D tex = AssetDatabase.LoadAssetAtPath(GetDefaultEditorTexturesFolder + imageName, typeof(Texture2D)) as Texture2D;
                if (tex != null) { guiContent.image = tex; }
                else { Debug.LogWarning("ERROR: " + callingMethodName + " - could not load " + GetDefaultEditorTexturesFolder + imageName); }
            }
        }

        #endregion

        #region Material Helper Methods

        /// <summary>
        /// Get a material from the Project Assets folder with a given name
        /// USAGE: locationMaterial = LBEditorHelper.GetMaterialFromAssets(LBSetup.materialsFolder, "LBLocation.mat");
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="materialName"></param>
        /// <returns></returns>
        public static Material GetMaterialFromAssets(string folder, string materialName)
        {
            if (!folder.EndsWith("/")) { folder += "/"; }

            Material mat = (Material)AssetDatabase.LoadAssetAtPath(folder + materialName, typeof(Material));
            if (mat != null) { return mat; }
            else { Debug.LogWarning(materialName + " material not found at path: " + folder + ". Did you accidentally delete it? If so, reimport the Landscape Builder package."); return null; }
        }

        #endregion

        #region Project Helper Methods

        /// <summary>
        /// Reveal or hightlight the item that in the Project window
        /// Highlights the item in yellow for a second or two.
        /// By default also selects the object. Not selecting the object can be useful if called
        /// from a CustomEditor inspector script so as not to loose focus.
        /// NOTE: The Project won't expand if it is already selected AND the user has collapsed it.
        /// </summary>
        /// <param name="AssetPath"></param>
        /// <param name="selectAsset"></param>
        public static void HighlightItemInProjectWindow(string AssetPath, bool selectAsset = true)
        {
            // Reveal or hightlight the item that in the Project window
            UnityEngine.Object obj = AssetDatabase.LoadMainAssetAtPath(AssetPath);

            if (obj != null)
            {
                // Highlight the item in yellow for a second or two.
                EditorGUIUtility.PingObject(obj);

                // Reveal or hightlight the item that in the Project window
                if (selectAsset) { Selection.activeObject = obj; }
            }
        }

        /// <summary>
        /// Reveal or hightlight the folder that in the Project window
        /// Highlights the item in yellow for a second or two.
        /// By default also selects the object. Not selecting the object can be useful if called
        /// from a CustomEditor inspector script so as not to loose focus.
        /// NOTE: The Project won't expand if it is already selected AND the user has collapsed it.
        /// </summary>
        /// <param name="folderPath"></param>
        /// <param name="selectFolder"></param>
        /// <param name="showErrors"></param>
        public static void HighlightFolderInProjectWindow(string folderPath, bool selectFolder, bool showErrors)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath(folderPath, typeof(UnityEngine.Object));
                if (obj != null)
                {
                    // Highlight the item in yellow for a second or two.
                    EditorGUIUtility.PingObject(obj);

                    if (selectFolder) { UnityEditor.Selection.activeObject = obj; }
                }
            }
            else { Debug.Log("LBEditorHelper.HighLightFolder - the following folder does not exist: " + folderPath); }
        }

        /// <summary>
        /// Select an object in the Project Window and optionally show inspector
        /// </summary>
        /// <param name="obj"></param>
        public static void SelectObjectInProjectWindow(UnityEngine.Object obj, bool showInspector = false)
        {
            if (obj != null)
            {
                Selection.activeObject = obj;

                if (showInspector)
                {
                    #if UNITY_2018_2_OR_NEWER
                    CallMenu("Window/General/Inspector");
                    #else
                    CallMenu("Window/Inspector");
                    #endif

                    //EditorWindow.GetWindow<EditorWindow>("Inspector"); // This only gets the last opened window...
                    //EditorWindow.GetWindow<EditorWindow>("Inspector", System.Type.GetType("LandscapeBuilderWindow"));

                    //System.Type inspectorType = GetEditorType("InspectorWindow", false, true);

                    //try
                    //{
                    //    // InspectorWindow inherits from EditorWindow class
                    //    var inspectorInstance = Editor.CreateInstance("InspectorWindow") as EditorWindow;
                    //    if (inspectorInstance != null)
                    //    {
                    //        inspectorInstance.Show();
                    //    }
                    //}
                    //catch (Exception ex)
                    //{
                    //    Debug.LogWarning("LBEditorHelper.SelectObjectInProjectWindow " + ex.Message);
                    //}
                }
            }
        }

        /// <summary>
        /// Check to see if a folder is in the Project using the AssetDatabase
        /// [Editor Only]
        /// </summary>
        /// <param name="folderPath"></param>
        /// <returns></returns>
        public static bool IsProjectFolderAvailable(string folderPath)
        {
            return AssetDatabase.IsValidFolder(folderPath);
        }

        /// <summary>
        /// Move a file from one folder to another in the Project Heirarchy
        /// Keep the meta-data the same so as not to break linked assets in scenes
        /// USAGE: LBEditorHelper.MoveAsset("LBSetup.UpgradeProject", materialsFolder, materialsFolder + "/Resources", "LBMoon.mat");
        /// </summary>
        /// <param name="sourceMethod"></param>
        /// <param name="sourceFolder"></param>
        /// <param name="destFolder"></param>
        /// <param name="fileName"></param>
        public static void MoveAsset(string sourceMethod, string sourceFolder, string destFolder, string fileName)
        {
            CheckFolder(destFolder);

            if (!File.Exists(destFolder + "/" + fileName))
            {
                Debug.Log(sourceMethod + " - moving " + fileName + " from " + sourceFolder + " to " + destFolder + "...");
                string status = AssetDatabase.MoveAsset(sourceFolder + "/" + fileName, destFolder + "/" + fileName);
                if (status != "") { Debug.LogWarning(sourceMethod + " - " + status); }
                else { Debug.Log(sourceMethod + " - moved " + fileName + " from " + sourceFolder + " to " + destFolder + "."); }
            }
        }

        /// <summary>
        /// Rename a file if it exists and the new file does not exist
        /// USAGE: LBEditorHelper.RenameAsset("LBSetup.UpgradeProject", "Assets/LandscapeBuilder/SRP", "LB_HDRP.unitypackage", "LB_HDRP_4.9.0.unitypackage");
        /// </summary>
        /// <param name="sourceMethod"></param>
        /// <param name="filePath"></param>
        /// <param name="oldFileName"></param>
        /// <param name="newFilename"></param>
        public static void RenameAsset(string sourceMethod, string filePath, string oldFileName, string newFilename)
        {
            if (!File.Exists(filePath + "/" + newFilename) && File.Exists(filePath + "/" + oldFileName))
            {
                string status = AssetDatabase.RenameAsset(filePath + "/" + oldFileName, newFilename);
                if (status != "") { Debug.LogWarning(sourceMethod + " - " + status); }
                else { Debug.Log(sourceMethod + " - renamed " + filePath + "/" + oldFileName + " to " + newFilename); }
            }
        }

        /// <summary>
        /// If the file exists, remove it from the asset database
        /// USAGE: LBEditorHelper.DeleteAsset("LBSetup.UpgradeProject", "Assets/LandscapeBuilder/SRP", "LB_HDRP.unitypackage");
        /// </summary>
        /// <param name="sourceMethod"></param>
        /// <param name="folderPath"></param>
        /// <param name="fileName"></param>
        public static void DeleteAsset(string sourceMethod, string folderPath, string fileName)
        {
            string filePath = folderPath + "/" + fileName;
            if (File.Exists(filePath))
            {
                if (!AssetDatabase.DeleteAsset(filePath))
                { Debug.LogWarning(sourceMethod + " - " + filePath + " was not deleted"); }
                else { Debug.Log(sourceMethod + " - " + filePath + " was deleted."); }
            }
        }

        /// <summary>
        /// Get the location of the Assets folder if run in the editor
        /// </summary>
        /// <returns></returns>
        public static string GetAssetsFolder()
        {
            return Application.dataPath;
        }

        /// <summary>
        /// Get the relative path to an asset within the project folder.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string GetAssetFolder(UnityEngine.Object obj)
        {
            string folderPath = string.Empty;

            if (obj != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(obj);
                if (System.IO.File.Exists(assetPath))
                {
                    folderPath = System.IO.Path.GetDirectoryName(assetPath);
                    folderPath = folderPath.Replace("\\", "/");
                }
            }

            return folderPath;
        }

        /// <summary>
        /// Select a folder in the project view panel
        /// </summary>
        /// <param name="folderPath"></param>
        /// <param name="showErrors"></param>
        public static void SelectFolder(string folderPath, bool showErrors)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath(folderPath, typeof(UnityEngine.Object));
                if (obj != null)
                {
                    UnityEditor.Selection.activeObject = obj;
                }
            }
            else { Debug.Log("LBEditorHelper.SelectFolder - the following folder does not exist: " + folderPath); }
        }

        /// <summary>
        /// Create a scriptableObject of a given type in the project folder. In UniqueRenameMode, the object
        /// will be created in the currently selected project folder, and will be highlighted in Rename Mode.
        /// If Unique Rename Mode is false, CreateAsset WILL OVERWRITE any existing asset of the same name.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T CreateAsset<T>(string prefix, bool isUniqueRenameMode) where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();

            if (isUniqueRenameMode)
            {
                ProjectWindowUtil.CreateAsset(asset, prefix + typeof(T).Name + ".asset");
            }
            else
            {
                AssetDatabase.CreateAsset(asset, "Assets/" + prefix + typeof(T).Name + ".asset");
            }
            return asset;
        }

        /// <summary>
        /// Find an asset of a particular type in the project folder. The type must be derived from
        /// a Unity Object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public static T GetAsset<T>(string prefix) where T : UnityEngine.Object
        {
            return AssetDatabase.LoadAssetAtPath("Assets/" + prefix + typeof(T).Name + ".asset", typeof(T)) as T;
        }

        /// <summary>
        /// Find an asset of a particular type in a project folder. The type must be derived from a Unity Object
        /// e.g. Material mat = LBEditorHelper.GetAsset<Material>("MicroSplatData", terrainCustomMaterial.name + ".mat");
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="folder"></param>
        /// <param name="assetName"></param>
        /// <returns></returns>
        public static T GetAsset<T>(string folder, string assetName) where T : UnityEngine.Object
        {
            return AssetDatabase.LoadAssetAtPath("Assets/" + folder + (folder.EndsWith("/") ? "" : "/") + assetName, typeof(T)) as T;
        }

        #endregion

        #region Editor Internals

        /// <summary>
        /// Get a class (type) from within the UnityEditor namespace
        /// USAGE: System.Type type = LBEditorHelper.GetEditorType("InspectorWindow", false, true);
        /// </summary>
        /// <param name="editorTypeName"></param>
        /// <param name="showSuccess"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static System.Type GetEditorType(string editorTypeName, bool showSuccess, bool showErrors)
        {
            System.Type editorType = null;

            try
            {
                editorType = System.Type.GetType("UnityEditor." + editorTypeName + ", UnityEditor", true, true);
                if (editorType != null)
                {
                    if (showSuccess) { Debug.Log("LBEditorHelper.GetInspector found " + editorTypeName); }
                }
                editorType = null;
            }
            catch (Exception ex)
            {
                if (showErrors) { Debug.LogWarning("LBEditorHelper.GetEditorType. " + ex.Message); }
            }

            return editorType;
        }

        #endregion

        #region IO Helper Methods

        // Check folder is valid. If it is missing, create it.
        // Does not check for multiple folder levels. Just the last one
        public static void CheckFolder(string folderPath)
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                int i = folderPath.LastIndexOf('/');

                if (i >= 0 && i < folderPath.Length - 2)
                {
                    Debug.Log("INFO LBEditorHelper - Creating new folder " + folderPath.Substring(i + 1) + " in " + folderPath.Substring(0, i));

                    AssetDatabase.CreateFolder(folderPath.Substring(0, i), folderPath.Substring(i + 1));
                }
            }
        }

        /// <summary>
        /// Check folder path and create any missing folders.
        /// </summary>
        /// <param name="folderPath"></param>
        public static void CheckFolderStructure(string folderPath)
        {
            // Check if whole structure is valid first
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                int posCurrent = 0, i;
                int len = folderPath.Length;

                while (posCurrent < len - 1)
                {
                    i = folderPath.IndexOf('/', posCurrent);

                    if (i < 0)
                    {
                        CheckFolder(folderPath);
                        break;
                    }
                    else
                    {
                        CheckFolder(folderPath.Substring(0, i));
                        posCurrent = i + 1;
                    }
                }
            }
        }

        /// <summary>
        /// Get an folder from the Project Asset folder from the user
        /// EXAMPLE: GetPathFromUser(LBSavedData.PathType.HQPhotographicTexturesVol1, "Assets", ref pathHQPhotoPackVol1);
        /// </summary>
        /// <param name="pathType"></param>
        /// <param name="relativeFolderToOpen"></param>
        /// <param name="pathToUpdate"></param>
        public static void GetPathFromUser(LBSavedData.PathType pathType, string relativeFolderToOpen, ref string pathToUpdate, bool savePath)
        {
            // Returns the full absolute path
            string path = EditorUtility.OpenFolderPanel(pathType.ToString(), relativeFolderToOpen, "");
            if (path.Contains(Application.dataPath))
            {
                // Get the relative path from Assets
                if (path.Length > Application.dataPath.Length) { path = path.Remove(0, Application.dataPath.Length); }
                if (path.Length > 1)
                {
                    if (path[0] == '/') { path = path.Remove(0, 1); }
                }

                // Make sure the text field doesn't have the focus, else it won't update until user
                // moves to another control.
                GUI.FocusControl("");

                pathToUpdate = path;

                if (savePath) { LBLandscape.SetPath(pathType, pathToUpdate); }
            }
            // Did user cancel open folder panel?
            else if (string.IsNullOrEmpty(path)) { }
            else
            {
                EditorUtility.DisplayDialog(pathType.ToString() + " Folder", "The folder must be in the Assets folder of your project", "OK");
            }
        }

        /// <summary>
        /// Get a folder from the user (EDITOR ONLY)
        /// EXAMPLE: GetPathFromUser("RAW Heightmap", LBEditorHelper.GetDefaultHeightmapFolder, false, ref rawFileFolder);
        /// </summary>
        /// <param name="dialogTitle"></param>
        /// <param name="relativeFolderToOpen"></param>
        /// <param name="restrictToProject"></param>
        /// <param name="folderPath"></param>
        /// <returns></returns>
        public static bool GetPathFromUser(string dialogTitle, string relativeFolderToOpen, bool restrictToProject, ref string folderPath)
        {
            bool isSuccessful = false;

            // Returns the full absolute path
            string path = EditorUtility.OpenFolderPanel(dialogTitle, relativeFolderToOpen, "");
            if ((!restrictToProject && !string.IsNullOrEmpty(path)) || path.Contains(Application.dataPath))
            {
                if (restrictToProject)
                {
                    // Get the relative path from Assets
                    if (path.Length > Application.dataPath.Length) { path = path.Remove(0, Application.dataPath.Length); }
                    if (path.Length > 1)
                    {
                        if (path[0] == '/') { path = path.Remove(0, 1); }
                    }
                }

                // Make sure the text field doesn't have the focus, else it won't update until user
                // moves to another control.
                GUI.FocusControl("");

                folderPath = path;
                isSuccessful = true;
            }
            // Did user cancel open folder panel?
            else if (string.IsNullOrEmpty(path)) { }
            else
            {
                EditorUtility.DisplayDialog(dialogTitle + " Folder", "The folder must be in the Assets folder of your project", "OK");
            }

            return isSuccessful;
        }

        /// <summary>
        /// Get a full file path from the user.
        /// Has the option to restrict to only files in the current Unity project.
        /// </summary>
        /// <param name="fileTypeName"></param>
        /// <param name="relativeFolderToOpen"></param>
        /// <param name="fileExtension"></param>
        /// <param name="restrictToProject"></param>
        /// <param name="folderPath"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static bool GetFilePathFromUser(string fileTypeName, string relativeFolderToOpen, string fileExtension, bool restrictToProject, ref string folderPath, ref string fileName)
        {
            bool isSuccessful = false;

            // Returns the full absolute path
            string path = EditorUtility.OpenFilePanel(fileTypeName, relativeFolderToOpen, fileExtension);
            if (path.Contains(Application.dataPath) || (!restrictToProject && !string.IsNullOrEmpty(path)))
            {
                // Make sure the text field doesn't have the focus, else it won't update until user
                // moves to another control.
                GUI.FocusControl("");

                folderPath = Path.GetDirectoryName(path);
                fileName = Path.GetFileName(path);

                isSuccessful = true;
            }
            // Did user cancel open file panel?
            else if (string.IsNullOrEmpty(path)) { }
            else
            {
                EditorUtility.DisplayDialog(fileTypeName + " File", "The file must be in the Assets folder of your project", "OK");
            }

            return isSuccessful;
        }

        /// <summary>
        /// Get a full file path from the user.
        /// Has the option to restrict to only files in the current Unity project.
        /// fileExtensions format: { "Texture2D", "png,psd,jpg,jpeg", "All files", "*" }
        /// </summary>
        /// <param name="fileTypeName"></param>
        /// <param name="relativeFolderToOpen"></param>
        /// <param name="fileExtensions"></param>
        /// <param name="restrictToProject"></param>
        /// <param name="folderPath"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static bool GetFilePathFromUser(string fileTypeName, string relativeFolderToOpen, string[] fileExtensions, bool restrictToProject, ref string folderPath, ref string fileName)
        {
            bool isSuccessful = false;

            // Returns the full absolute path
            string path = EditorUtility.OpenFilePanelWithFilters(fileTypeName, relativeFolderToOpen, fileExtensions);
            if (path.Contains(Application.dataPath) || (!restrictToProject && !string.IsNullOrEmpty(path)))
            {
                // Make sure the text field doesn't have the focus, else it won't update until user
                // moves to another control.
                GUI.FocusControl("");

                folderPath = Path.GetDirectoryName(path);
                fileName = Path.GetFileName(path);

                isSuccessful = true;
            }
            // Did user cancel open file panel?
            else if (string.IsNullOrEmpty(path)) { }
            else
            {
                EditorUtility.DisplayDialog(fileTypeName + " File", "The file must be in the Assets folder of your project", "OK");
            }

            return isSuccessful;
        }

        /// <summary>
        /// Get a file path from the user.
        /// Has the option to restrict to only files in the current Unity project.
        /// </summary>
        /// <param name="pathType"></param>
        /// <param name="relativeFolderToOpen"></param>
        /// <param name="fileExtension"></param>
        /// <param name="restrictToProject"></param>
        /// <param name="pathToUpdate"></param>
        /// <param name="savePath"></param>
        public static void GetFilePathFromUser(LBSavedData.PathType pathType, string relativeFolderToOpen, string fileExtension, bool restrictToProject, ref string pathToUpdate, bool savePath)
        {
            // Returns the full absolute path
            string path = EditorUtility.OpenFilePanel(pathType.ToString(), relativeFolderToOpen, fileExtension);
            if (path.Contains(Application.dataPath) || (!restrictToProject && !string.IsNullOrEmpty(path)))
            {
                if (restrictToProject)
                {
                    // Get the relative path from Assets
                    if (path.Length > Application.dataPath.Length) { path = path.Remove(0, Application.dataPath.Length); }
                    if (path.Length > 1)
                    {
                        if (path[0] == '/') { path = path.Remove(0, 1); }
                    }
                }

                // Make sure the text field doesn't have the focus, else it won't update until user
                // moves to another control.
                GUI.FocusControl("");

                pathToUpdate = path;

                if (savePath) { LBLandscape.SetPath(pathType, pathToUpdate); }
            }
            // Did user cancel open file panel?
            else if (string.IsNullOrEmpty(path)) { }
            else
            {
                EditorUtility.DisplayDialog(pathType.ToString() + " File", "The file must be in the Assets folder of your project", "OK");
            }
        }

        /// <summary>
        /// Combine a folder name with a filename to get the correct full file path.
        /// Unity / .NET will drop the \\ in favour of \ because it thinks it is an escaped char
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static string GetCombinedPath(string folder, string filename)
        {
            string combinedPath = Path.Combine(folder, filename);

            // Replace single backslash (\\) with the default for this OS. In "\\", the first slash is the escape character.
            combinedPath = combinedPath.Replace('\\', Path.DirectorySeparatorChar);

            if (combinedPath.Length > 2)
            {
                // If the first character is a backslash but the second one isn't, add a backslash
                // to the start of the path so that it correctly mimics the double-backslash required
                // for a windows network URL e.g. \\servername\fileshare
                if (combinedPath[0] == '\\' && combinedPath[1] != '\\')
                {
                    combinedPath = '\\' + combinedPath;
                }
            }


            return combinedPath;
        }

        #endregion

        #region Popup Methods

        /// <summary>
        /// Find the first matching item in the list
        /// Default to 0 if no matches found (first item)
        /// Currently doesn't check for null or empty lists
        /// </summary>
        /// <param name="itemList"></param>
        /// <param name="itemToMatch"></param>
        /// <returns></returns>
        public static int GetSelectedIndex(List<string> itemList, string itemToMatch)
        {
            int index = 0;

            index = itemList.FindIndex(l => l == itemToMatch);

            // Default to first item in list if item not found
            if (index < 0) { index = 0; }

            return index;
        }

        #endregion

        #region String Manipulation

        /// <summary>
        /// Given a string, return a new one with the specific length. Has option to automatically
        /// add two dots (ellipsis) for truncated strings.
        /// </summary>
        /// <param name="inputString"></param>
        /// <param name="maxLength"></param>
        /// <param name="isAutoEllipsisEnabled"></param>
        /// <returns></returns>
        public static string TruncateString(string inputString, int maxLength, bool isAutoEllipsisEnabled)
        {
            if (string.IsNullOrEmpty(inputString)) { return inputString; }
            else if (isAutoEllipsisEnabled && maxLength > 3) { return inputString.Length <= maxLength ? inputString : inputString.Substring(0, maxLength - 3) + ".."; }
            else { return inputString.Length <= maxLength ? inputString : inputString.Substring(0, maxLength); }
        }

        #endregion

        #region Dialog Boxes

        /// <summary>
        /// Prompt user to respond Yes or No
        /// </summary>
        /// <param name="dialogTile"></param>
        /// <param name="dialogText"></param>
        /// <returns></returns>
        public static bool PromptYesNo(string dialogTile, string dialogText)
        {
            return EditorUtility.DisplayDialog(dialogTile, dialogText, "Yes", "NO!");
        }

        /// <summary>
        /// Prompt user to continue with an action.
        /// </summary>
        /// <param name="dialogTile"></param>
        /// <param name="dialogText"></param>
        /// <returns></returns>
        public static bool PromptForContinue(string dialogTile, string dialogText)
        {
            return EditorUtility.DisplayDialog(dialogTile, dialogText, "Yes", "CANCEL!");
        }

        /// <summary>
        /// Prompt the user to delete something or cancel.
        /// string labelText = "Group " + (groupPos + 1).ToString() + " will be deleted\n\nThis action will remove the group from the list and cannot be undone.";
        /// if (LBEditorHelper.PromptForDelete("Delete Group?", labelText)) {...}
        /// </summary>
        /// <param name="dialogTile"></param>
        /// <param name="dialogText"></param>
        /// <returns></returns>
        public static bool PromptForDelete(string dialogTile, string dialogText)
        {
            return EditorUtility.DisplayDialog(dialogTile, dialogText, "Delete Now", "Cancel");
        }

        /// <summary>
        /// Prompt the user to delete something or cancel.
        /// if (LBEditorHelper.PromptForDelete("Tree Type", "", 0, false)) {...}
        /// if (LBEditorHelper.PromptForDelete("Layer", layerToRemove.type.ToString(), layerPos, false)) {..}
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="objectDetailName"></param>
        /// <param name="objectIndex"></param>
        /// <param name="isUndoAvailable"></param>
        /// <returns></returns>
        public static bool PromptForDelete(string objectName, string objectDetailName, int objectIndex, bool isUndoAvailable)
        {
            return EditorUtility.DisplayDialog("Delete " + objectName + "?", objectName + " " + (objectIndex + 1).ToString() + (string.IsNullOrEmpty(objectDetailName) ? "" : " " + objectDetailName) + " will be deleted\n\nThis action will remove the " + objectName.ToLower() + (isUndoAvailable ? "." : " and cannot be undone."), "Delete Now", "Cancel");
        }

        #endregion

        #region Progress Bars

        /// <summary>
        /// Get the System.Type of the internal Editor ProgressBar
        /// This bar is displayed in bottom right corner of main Unity Editor window
        /// </summary>
        /// <returns></returns>
        public static System.Type GetEditorProgressBarType()
        {
            return LBIntegration.GetClassTypeFromFullName("UnityEditor.AsyncProgressBar, UnityEditor", true);
        }

        /// <summary>
        /// Show the internal Editor ProgressBar.
        /// Call LBEditorHelper.GetEditorProgressBarType() to get typePgr.
        /// NOTE: Currently in Windows 10 only updates the icon in the Windows taskbar
        /// </summary>
        /// <param name="typePgbr"></param>
        /// <param name="progressText"></param>
        /// <param name="progress"></param>
        public static void ShowEditorProgressbar(System.Type typePgbr, string progressText, float progress)
        {
            if (typePgbr != null)
            {
                try
                {
                    System.Reflection.MethodInfo methodInfo = typePgbr.GetMethod("Display");
                    if (methodInfo != null)
                    {
                        methodInfo.Invoke(null, new object[] { progressText, progress });
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("ERROR: LBEditorHelper.ShowEditorProgressbar had an error. Please Report. " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Hide or clear the internal Editor ProgressBar
        /// Call LBEditorHelper.GetEditorProgressBarType() to get typePgr.
        /// </summary>
        /// <param name="typePgbr"></param>
        public static void HideEditorProgressbar(System.Type typePgbr)
        {
            if (typePgbr != null)
            {
                try
                {
                    System.Reflection.MethodInfo methodInfo = typePgbr.GetMethod("Clear");
                    if (methodInfo != null)
                    {
                        methodInfo.Invoke(null,null);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("ERROR: LBEditorHelper.HideEditorProgressbar had an error. Please Report. " + ex.Message);
                }
            }
        }

        #endregion

        #region Layer Methods

        /// <summary>
        /// Return a list of layer names which aren't empty.
        /// isFiltered will remove common layers that would typically not be selectable in Landscape Builder
        /// </summary>
        /// <param name="isFiltered"></param>
        /// <returns></returns>
        public static List<string> GetLayerList(bool isFiltered)
        {
            // There is a max of 32 layers in Unity
            List<string> layerNamesList = new List<string>();
            for (int i = 0; i < 32; i++)
            {
                string layerName = LayerMask.LayerToName(i);
                if (layerName != null && layerName.Length > 0)
                {
                    if (layerName == "Ignore Raycast" || layerName == "UI" || layerName == "LB Celestials") { continue; }
                    layerNamesList.Add(layerName);
                }
            }

            return layerNamesList;
        }

        /// <summary>
        /// Return the layer index within the range 0..31 from a Layer Name.
        /// If the layer doesn't exist, returns 0 ("Default" inbuilt Unity layer)
        /// </summary>
        /// <param name="layerName"></param>
        /// <returns></returns>
        public static int GetLayerIndex(string layerName)
        {
            int layerIndex = 0; // In-build "Default" Unity layer

            int _lookupIndex = LayerMask.NameToLayer(layerName);
            if (_lookupIndex >= 0 && _lookupIndex < 32) { layerIndex = _lookupIndex; }

            return layerIndex;
        }

        /// <summary>
        /// Display a restricted list of the Unity Layers as a multi-selectable list
        /// NOTE: Does not work with Everything in Popup control
        /// NOT IS USE - please use LayerMaskField()
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="labelText"></param>
        /// <param name="labelToolTip"></param>
        /// <param name="guiLayoutOption"></param>
        /// <returns></returns>
        public static LayerMask LayerField(LayerMask layer, string labelText, string labelToolTip, GUILayoutOption guiLayoutOption)
        {
            LayerMask result;

            // There is a max of 32 layers in Unity
            List<string> layerNamesList = new List<string>();
            for (int i = 0; i < 32; i++)
            {
                string layerName = LayerMask.LayerToName(i);
                if (layerName != null && layerName.Length > 0)
                {
                    if (layerName == "Ignore Raycast" || layerName == "UI" || layerName == "LB Celestials") { continue; }
                    layerNamesList.Add(layerName);
                }
            }

            if (guiLayoutOption != null)
            {
                result = EditorGUILayout.MaskField(new GUIContent(labelText, labelToolTip), layer.value, layerNamesList.ToArray(), guiLayoutOption);
            }
            else { result = EditorGUILayout.MaskField(new GUIContent(labelText, labelToolTip), layer.value, layerNamesList.ToArray()); }

            return result;
        }

        /// <summary>
        /// Return a full list of the Unity Layers without the ones with no names
        /// Also works with Nothing and Everything in the multi-select mask popup list
        /// </summary>
        /// <param name="layerMask"></param>
        /// <param name="labelText"></param>
        /// <param name="labelToolTip"></param>
        /// <param name="guiLayoutOption"></param>
        /// <returns></returns>
        public static LayerMask LayerMaskField(LayerMask layerMask, string labelText, string labelToolTip, GUILayoutOption guiLayoutOption)
        {
            List<string> layers = new List<string>();
            List<int> layerNumbers = new List<int>();

            for (int i = 0; i < 32; i++)
            {
                string layerName = LayerMask.LayerToName(i);
                if (layerName != "")
                {
                    layers.Add(layerName);
                    layerNumbers.Add(i);
                }
            }
            int maskWithoutEmpty = 0;
            for (int i = 0; i < layerNumbers.Count; i++)
            {
                if (((1 << layerNumbers[i]) & layerMask.value) > 0) maskWithoutEmpty |= (1 << i);
            }

            if (guiLayoutOption != null)
            {
                maskWithoutEmpty = EditorGUILayout.MaskField(new GUIContent(labelText, labelToolTip), maskWithoutEmpty, layers.ToArray(), guiLayoutOption);
            }
            else { maskWithoutEmpty = EditorGUILayout.MaskField(new GUIContent(labelText, labelToolTip), maskWithoutEmpty, layers.ToArray()); }

            int mask = 0;
            for (int i = 0; i < layerNumbers.Count; i++)
            {
                if ((maskWithoutEmpty & (1 << i)) > 0) mask |= (1 << layerNumbers[i]);
            }
            layerMask.value = mask;
            return layerMask;
        }

        /// <summary>
        /// Get the Full Unity LayerMask given a restricted LayerMask with potentially
        /// multiple items selected by the user.
        /// NOT IN USE - See LayerMaskField()
        /// </summary>
        /// <param name="restrictedlayerMask"></param>
        /// <returns></returns>
        public static LayerMask GetFullLayerMask(LayerMask restrictedlayerMask)
        {
            // Extract the list of selected numbers from the layerMask.
            List<int> maskIntSelectedList = new List<int>();
            int currentInt = restrictedlayerMask.value;

            do
            {
                if (!Mathf.IsPowerOfTwo(currentInt))
                {
                    int po2 = (int)Mathf.NextPowerOfTwo(currentInt) / 2;
                    maskIntSelectedList.Add(po2);
                    currentInt -= po2;
                }
                else
                {
                    maskIntSelectedList.Add(currentInt);
                    currentInt = 0;
                }
            } while (currentInt > 0);
            maskIntSelectedList.Sort();

            // Build the current restricted list of Unity layers
            // These are the ones we expose in the filter.
            List<int> restrictedLayerIDList = new List<int>();
            for (int i = 0; i < 32; i++)
            {
                string layerName = LayerMask.LayerToName(i);
                if (layerName != null && layerName.Length > 0)
                {
                    if (layerName == "Ignore Raycast" || layerName == "UI" || layerName == "LB Celestials") { continue; }
                    restrictedLayerIDList.Add(i);
                }
            }

            // Create Int list
            List<int> maskIntRestrictedList = new List<int>();

            string[] namesRestricted = new string[restrictedLayerIDList.Count];
            for (int i = 0; i < restrictedLayerIDList.Count; i++)
            {
                namesRestricted[i] = LayerMask.LayerToName(restrictedLayerIDList[i]);
                maskIntRestrictedList.Add(LBNoise.IntPow(2, i));
            }

            // Build the selected list of names so that we can create a full
            // Unity Layers mask using LayerMask.GetMask() which takes a list
            // of layer names.
            List<string> selectedLayers = new List<string>();
            if (maskIntRestrictedList.Count > 0)
            {
                for (int i = 0; i < restrictedLayerIDList.Count; i++)
                {
                    if (maskIntSelectedList.Contains(maskIntRestrictedList[i]))
                    {
                        selectedLayers.Add(namesRestricted[i]);
                    }
                }
            }

            return LayerMask.GetMask(selectedLayers.ToArray());
        }

        /// <summary>
        /// Get the Restricted Unity LayerMask given a Full LayerMask with potentially
        /// multiple items selected in the mask.
        /// NOT IN USE - See LayerMaskField()
        /// </summary>
        /// <param name="layerMask"></param>
        /// <returns></returns>
        public static LayerMask GetRestrictedLayerMask(LayerMask layerMask)
        {
            LayerMask tempLayerMask = new LayerMask();
            string layerName = string.Empty;

            // Extract the list of selected numbers and names from the layerMask.
            List<int> fullmaskIntSelectedList = new List<int>();
            int currentInt = layerMask.value;

            do
            {
                if (!Mathf.IsPowerOfTwo(currentInt))
                {
                    int po2 = (int)Mathf.NextPowerOfTwo(currentInt) / 2;
                    fullmaskIntSelectedList.Add(po2);
                    currentInt -= po2;
                }
                else
                {
                    fullmaskIntSelectedList.Add(currentInt);
                    currentInt = 0;
                }
            } while (currentInt > 0);
            fullmaskIntSelectedList.Sort();

            // Build the current restricted list of Unity layers
            // These are the ones we expose in the filter.
            List<int> restrictedLayerIDList = new List<int>();
            List<string> restrictedLayerNameList = new List<string>();
            for (int i = 0; i < 32; i++)
            {
                layerName = LayerMask.LayerToName(i);
                if (layerName != null && layerName.Length > 0)
                {
                    if (layerName == "Ignore Raycast" || layerName == "UI" || layerName == "LB Celestials") { continue; }
                    restrictedLayerIDList.Add(i);
                    restrictedLayerNameList.Add(layerName);
                }
            }

            // The Full LayerID is Log10(exponent) = Log10(layermaskvalue)/Log10(2)
            // Which give 2^exponent = layermaskvalue
            // e.g. 2^18 = 262144 or 18 = Log10(262144)/Log10(2)
            if (fullmaskIntSelectedList.Count > 0)
            {
                foreach (int i in fullmaskIntSelectedList)
                {
                    if (i == 0)
                    {
                        // Nothing or Everything is selected... not sure which as they both have masklayer.value = 0
                    }
                    else
                    {
                        // Determine the Full Unity LayerID using Log10 as above, then get the name
                        layerName = LayerMask.LayerToName((int)(Math.Log10(i) / Math.Log10(2)));
                        //Debug.Log("Full Int: " + i.ToString() + " " + layerName);
                        // See if there is a matching one selected?
                        int restrictedPos = restrictedLayerNameList.FindIndex(l => l == layerName);
                        if (restrictedPos >= 0)
                        {
                            tempLayerMask += LBNoise.IntPow(2, restrictedPos + 1);
                        }
                    }
                }
            }

            return tempLayerMask;
        }

        #endregion

        #region Prefab Helpers

        /// <summary>
        /// Is the object a prefab from the Assets folder?
        /// NOTE: NOT TESTED WITH 2018.3+
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="isShowInfoMsg"></param>
        /// <returns></returns>
        public static bool IsPrefab(UnityEngine.Object prefab, bool isShowInfoMsg)
        {
            bool isPrefab = false;

            // Make sure it was a prefab and not a simple gameobject like a fbx file etc.
            if (prefab != null)
            {
                #if UNITY_2018_3_OR_NEWER
                PrefabAssetType prefabAssetType = PrefabUtility.GetPrefabAssetType(prefab);

                // NotAPrefab, Regular = User created prefab, Model = imported 3D model asset, Variant = Prefab Variant, MissingAsset (unknown prefab type)
                if (prefabAssetType != PrefabAssetType.Regular && prefabAssetType != PrefabAssetType.Model && prefabAssetType != PrefabAssetType.Variant)
                {
                    if (isShowInfoMsg) { Debug.Log("INFO: Landscape Builder - Only user defined, variant or imported 3D asset prefabs are permitted. Ensure you are adding a prefab. " + prefab.name); }
                }
                else { isPrefab = true; }
                #else
                PrefabType prefabType = PrefabUtility.GetPrefabType(prefab);

                // Preb = User created prefab. ModelPrefab = imported 3D model asset
                if (prefabType != PrefabType.Prefab && prefabType != PrefabType.ModelPrefab)
                {
                    if (isShowInfoMsg) { Debug.Log("INFO: Landscape Builder - Only user defined or imported 3D asset prefabs are permitted. Ensure you are adding a prefab. " + prefab.name); }
                }
                else { isPrefab = true; }
                #endif
            }
            return isPrefab;
        }

        /// <summary>
        /// Is this object a prefab instance in the scene?
        /// INCOMPLETE NOT TESTED WITH 2018.3+
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="isShowInfoMsg"></param>
        /// <returns></returns>
        public static bool IsPrefabInstance(UnityEngine.Object prefab, bool isShowInfoMsg)
        {
            bool isPrefab = false;

            // Make sure it was a prefab and not a simple gameobject like a fbx file etc.
            if (prefab != null)
            {
                #if UNITY_2018_3_OR_NEWER
                PrefabAssetType prefabAssetType = PrefabUtility.GetPrefabAssetType(prefab);

                // NotAPrefab, Regular = User created prefab, Model = imported 3D model asset, Variant = Prefab Variant, MissingAsset (unknown prefab type)
                if (prefabAssetType != PrefabAssetType.Regular && prefabAssetType != PrefabAssetType.Model && prefabAssetType != PrefabAssetType.Variant)
                {
                    if (isShowInfoMsg) { Debug.Log("INFO: Landscape Builder - Only user defined, variant or imported 3D asset prefabs are permitted. Ensure you are adding a prefab. " + prefab.name); }
                }
                else { isPrefab = true; }
                #else
                PrefabType prefabType = PrefabUtility.GetPrefabType(prefab);

                // Preb = User created prefab in Project. ModelPrefab = imported 3D model asset in Project
                // PrefabInstance or ModelPrefabInstance are in the scene.
                if (prefabType != PrefabType.PrefabInstance && prefabType != PrefabType.ModelPrefabInstance)
                {
                    if (isShowInfoMsg) { Debug.Log("INFO: Landscape Builder - Only user defined or imported 3D asset prefabs in the scene are permitted. Ensure you are adding a prefab from the scene. " + prefab.name); }
                }
                else { isPrefab = true; }
                #endif
            }
            return isPrefab;
        }

        /// <summary>
        /// Get the parent asset GameObject of a prefab
        /// </summary>
        /// <param name="prefab"></param>
        /// <returns></returns>
        public static GameObject GetPrefabSource(UnityEngine.Object prefab)
        {
            GameObject prefabSource = null;

            if (prefab != null)
            {
                #if UNITY_2018_2_OR_NEWER
                prefabSource = (GameObject)UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(prefab);
                #else
                prefabSource = (GameObject)UnityEditor.PrefabUtility.GetPrefabParent(prefab);
                #endif
            }

            return prefabSource;
        }

        /// <summary>
        /// Get Bounds of Prefab. Will temporarily zero out position.
        /// To get world bounds, add the prefabTrfm position to the returned
        /// bounds center.
        /// Bounds bounds = LBEditorHelper.GetPrefabBounds(prefab, true);
        /// bounds.center += prefab.position;
        /// </summary>
        /// <param name="prefabTrfm"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static Bounds GetPrefabBounds(Transform prefabTrfm, bool showErrors)
        {
            Bounds bounds = new Bounds();

            if (prefabTrfm != null)
            {
                // Need to reset postion for GetBounds to work correctly
                Vector3 tempPos = prefabTrfm.position;
                prefabTrfm.position = Vector3.zero;
                bounds = LBMeshOperations.GetBounds(prefabTrfm, false, showErrors);
                prefabTrfm.position = tempPos;
            }

            return bounds;
        }

        #endregion

        #region Custom Controllers

        /// <summary>
        /// Displays a logarithmic slider and returns the corresponding value selected by the user.
        /// For an Integer slider, set isIntSlider = true.
        /// To display without a label, set labelContent to null (labelWidth will be ignored).
        /// Currently cannot have a minValue between -0.1 and 0.1.
        /// </summary>
        /// <param name="labelContent"></param>
        /// <param name="labelWidth"></param>
        /// <param name="currentValue"></param>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <param name="isIntSlider"></param>
        /// <param name="logBase"></param>
        /// <returns></returns>
        public static float LogarithmicSlider(GUIContent labelContent, float labelWidth, float currentValue, float minValue, float maxValue, bool isIntSlider, int logBase = 10)
        {
            // If the slider contains negative values, we need to treat it a little differently
            bool onlyPositiveValues = minValue > 0f;

            // Declare variables
            float minLogIndex = 0f;
            float maxLogIndex = 1f;
            float currentLogIndex = 0f;
            float newValue = 0f;
            float floatFieldWidth = 50f;

            //GUI.FocusControl(null);

            // Override labelWidth if the label has not been set is null
            if (labelContent == null) { labelWidth = 0; }

            if (onlyPositiveValues)
            {
                // Get min and max indices
                minLogIndex = 0f;
                maxLogIndex = Mathf.Log(maxValue / minValue, logBase);
                // Get current index
                currentLogIndex = Mathf.Log(currentValue / minValue, logBase);
            }
            else
            {
                // Get min and max indices
                minLogIndex = -Mathf.Log(-minValue / 0.1f, logBase);
                maxLogIndex = Mathf.Log(maxValue / 0.1f, logBase) + 1f;
                // Get current index
                if (currentValue > 0.1f) { currentLogIndex = Mathf.Log(currentValue / 0.1f, logBase) + 1f; }
                else if (currentValue < -0.1f) { currentLogIndex = -Mathf.Log(-currentValue / 0.1f, logBase); }
                else { currentLogIndex = Mathf.InverseLerp(-0.1f, 0.1f, currentValue); }
            }

            GUILayout.BeginHorizontal();

            // Trim the label width a little
            if (labelContent != null) { EditorGUILayout.LabelField(labelContent, GUILayout.Width(labelWidth - 4f)); }

            // Calc width of the slider
            float sliderWidth = EditorGUIUtility.currentViewWidth - labelWidth - floatFieldWidth - 52f;

            GUILayoutOption[] guiLayoutOptions = { GUILayout.MaxWidth(sliderWidth), GUILayout.ExpandWidth(true) };
            Rect posRect = EditorGUILayout.GetControlRect(guiLayoutOptions);

            float newLogIndex = GUI.HorizontalSlider(posRect, currentLogIndex, minLogIndex, maxLogIndex);

            // Calculate values from new index
            if (onlyPositiveValues) { newValue = minValue * Mathf.Pow(logBase, newLogIndex); }
            else
            {
                if (newLogIndex > 1f) { newValue = maxValue / Mathf.Pow(logBase, maxLogIndex - newLogIndex); }
                else if (newLogIndex < 0f) { newValue = minValue / Mathf.Pow(logBase, newLogIndex - minLogIndex); }
                else { newValue = Mathf.Lerp(-0.1f, 0.1f, newLogIndex); }
            }

            // Round output value to 3 decimal places
            newValue = (int)(newValue * 1000f) / 1000f;

            newValue = Mathf.Clamp(newValue, minValue, maxValue);

            if (isIntSlider) { newValue = Mathf.RoundToInt(newValue); }

            // Add the text field for manual numeric entry by user
            newValue = EditorGUILayout.FloatField(newValue, GUILayout.Width(floatFieldWidth));
            GUILayout.EndHorizontal();
  
            return newValue;
        }

        #endregion
    }
}
#endif
