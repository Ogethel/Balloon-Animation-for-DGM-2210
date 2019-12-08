using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace LandscapeBuilder
{
    [AddComponentMenu("Landscape Builder/Camera Animator")]
    [RequireComponent(typeof(Camera))]
    [RequireComponent(typeof(AudioListener))]
    public class LBCameraAnimator : MonoBehaviour
    {
        // A simple script that allows you to animate a camera along a defined splined camera path

        #region Variables and Properties

        public bool isRunning = false;
        public bool IsRunning { get { return isRunning; } }     // Is the animation currently running?

        public LBCameraPath cameraPath;                         // The camera path to use

        public bool animateSpeed = false;
        public float moveSpeed = 10f;                           // Camera move speed in m/s
        public float minMoveSpeed = 5f;
        public float maxMoveSpeed = 10f;
        public AnimationCurve speedTimeCurve = DefaultDistanceSpeedCurve(); // AnimationCurve.Linear(0f, 0f, 1f, 1f);

        private float distanceTravelled = 0f;
        public bool rotateCamera = true;                        // Whether the camera is rotated as it is animated along the path
        public float rotationTangentDistance = 1f;
        private float rotTanDistAdjusted = 0f;                  // Used when close to the start
        public bool startOnAwake = false;                       // Set off by default so that it will work correctly with LBTemplate and LBDemoLoader
        public bool enableAudioListener = true;
        public Camera animatorCamera = null;
        public bool addAQUASCameraScript = false;               // Adds an AQUAS_Camera script if AQUAS is installed in project

        public float startDistanceOffset = 0f;                  // The distance from the start in metres to commence the animation
        public float endDistanceOffset = 0f;                    // The distance from the end in metres to finish the animation

        // Items to enable/disable during the animation
        public List<Camera> disableCameraList = null;
        public List<GameObject> disableGameObjectList = null;

        // Animation Fader variables (Uses LBLighing)
        public bool fadeIn = false;
        public bool fadeOut = false;
        public float fadeInDuration = 5f;
        public float fadeOutDuration = 5f;

        // The number of seconds to pause at the end of path
        // before ending animation. Doesn't apply if closedcircuit
        public float pauseAtEndDuration = 0f;
        private float pauseAtEndTimer = 0f;

        public bool isEditorPreviewEnabled = false;
        public float previewDistanceOffset = 0f;                // The distance from the start in metres to place the camera in preview mode     
        public bool showPathInScenePrePreviewMode = false;      // This remembers if the path was shown in the scene before entering preview mode

        private AudioListener audioListener = null;

        private bool move = false;
        private static int keyInt = 0;
        private Vector3 forwards = Vector3.zero;
        private float maxDistanceToTravel = 0f;

        // LBLighting variables
        private LBLighting lbLighting = null;
        private Camera cameraToRestore = null;                  // When an animation begins it updates the lighting main camera. When it finishes, we need to restore the original camera

        private bool isInitialising = false;
        private bool isInitialised = false;

        #endregion

        #region Static Variables and Properties

        public static AnimationCurve DefaultDistanceSpeedCurve()
        {
            AnimationCurve newCurve = new AnimationCurve();
            int keyInt = 0;
            keyInt = newCurve.AddKey(0.00f, 0.00f);
            keyInt = newCurve.AddKey(0.16f, 1.00f);
            keyInt = newCurve.AddKey(0.83f, 1.00f);
            keyInt = newCurve.AddKey(1.00f, 0.00f);
            Keyframe[] curveKeys = newCurve.keys;
            curveKeys[0].inTangent = 0.00f;
            curveKeys[0].outTangent = 0.00f;
            curveKeys[1].inTangent = 0.00f;
            curveKeys[1].outTangent = 0.00f;
            curveKeys[2].inTangent = 0.00f;
            curveKeys[2].outTangent = 0.00f;
            curveKeys[3].inTangent = 0.00f;
            curveKeys[3].outTangent = 0.00f;
            newCurve = new AnimationCurve(curveKeys);
            // Stops warning 'variable is assigned but its value is never used' from appearing in the compiler
            if (keyInt == 0) { }

            return newCurve;
        }

        #endregion

        #region Initialise

        // Use this for initialization
        private void Awake()
        {
            // When calling BeginAnimation(..) from another script, sometimes it can run
            // before Awake. Whichever runs first, will run Initialise(). It only needs to run once.
            if (!isInitialised) { Initialise(); }

            if (startOnAwake) { BeginAnimation(true, startDistanceOffset); }
        }

        /// <summary>
        /// Initialise the Camera Animator
        /// </summary>
        private void Initialise()
        {
            // When calling BeginAnimation(..) from another script, sometimes it can run
            // before Awake. Whichever runs first, will run Initialise(). It only needs to run once.
            if (isInitialising) { return; }
            else { isInitialising = true; }

            // The camera should not be moving at this point
            move = false;
            maxDistanceToTravel = 0f;

            if (disableCameraList == null) { disableCameraList = new List<Camera>(); }
            if (disableGameObjectList == null) { disableGameObjectList = new List<GameObject>(); }

            // Immediately turn off the listener if it is enabled.
            if (audioListener != null) { audioListener.enabled = false; }
            else
            {
                // Attempt to find it on the same gameobject
                audioListener = gameObject.GetComponent<AudioListener>();
                if (audioListener == null)
                {
                    // This is a temporary fix for use at runtime. Unity will remove it once play stops.
                    Debug.LogWarning("LBCameraAnimator - Adding missing audio listener to " + gameObject.name + " gameobject");
                    audioListener = gameObject.AddComponent<AudioListener>();
                }

                if (audioListener != null) { audioListener.enabled = false; }
            }

            // Preview mode is off be default.
            isEditorPreviewEnabled = false;

            // Stops warning 'variable is assigned but its value is never used' from appearing in the compiler
            if (keyInt == 0) { }

            isInitialised = true;
            isInitialising = false;
        }

        #endregion

        #region Event Methods

        // Used for testing
        //private void OnGUI()
        //{
        //    Rect _rect = new Rect(0, Screen.height - 20f, Screen.width, 20f);
        //    GUI.Box(_rect, "Distance: " + distanceTravelled.ToString("N") + " maxDistanceToTravel: " + maxDistanceToTravel.ToString("N") + " speed: " + moveSpeed.ToString("N"));
        //}

        #endregion

        #region Private Methods

        // Update is called once per frame
        private void Update()
        {
            if (cameraPath != null && move)
            {
                if (animateSpeed)
                {
                    moveSpeed = Mathf.Lerp(minMoveSpeed, maxMoveSpeed, speedTimeCurve.Evaluate(distanceTravelled / maxDistanceToTravel));
                }

                distanceTravelled += moveSpeed * Time.deltaTime;

                if (distanceTravelled >= maxDistanceToTravel)
                {
                    // If an End Distance Offset is defined, stop after travelling the desired distance around the loop.
                    // If it is not a closed circuit, then we always want to stop.
                    if (endDistanceOffset > 0f || !cameraPath.lbPath.closedCircuit)
                    {
                        Stop();

                        // Check to see if we need to pause the animation at the end before finishing.
                        pauseAtEndTimer += Time.deltaTime;
                        if (pauseAtEndTimer >= pauseAtEndDuration) { EndAnimation(); }
                    }
                    // Check to see if we've gone past the start and we're starting another "lap"
                    else if (distanceTravelled >= cameraPath.lbPath.splineLength)
                    {
                        distanceTravelled -= cameraPath.lbPath.splineLength;
                    }
                }

                UpdateCameraTransform();
            }
        }

        private void UpdateCameraTransform()
        {
            transform.position = cameraPath.lbPath.GetPathPosition(distanceTravelled, LBPath.PositionType.Centre, true);

            // WARNING: For some reason, when at end of path, Algorithm 1 returns the first point on path.
            // Also, it tends to wobble a little
            //transform.position = cameraPath.lbPath.GetPathPosition(distanceTravelled, 1);
            if (rotateCamera)
            {
                // When close to the start or end of the path, the camera rotation tangent distance must be >4
                // or else the camera will rotate towards it's default direction.
                //if (rotationTangentDistance >= 4f) { rotTanDistAdjusted = distanceTravelled + rotationTangentDistance; }
                //else if (distanceTravelled >= 4f && distanceTravelled < cameraPath.lbPath.splineLength - 4f) { rotTanDistAdjusted = distanceTravelled + rotationTangentDistance; }
                //else { rotTanDistAdjusted = distanceTravelled + 4f; }
                //forwards = cameraPath.lbPath.GetPathPosition(rotTanDistAdjusted, LBPath.PositionType.Centre, true) - transform.position;

                if (cameraPath.lbPath.closedCircuit)
                {
                    if (distanceTravelled < cameraPath.lbPath.splineLengthToLastPoint - rotationTangentDistance)
                    {
                        rotTanDistAdjusted = distanceTravelled + rotationTangentDistance;
                        forwards = cameraPath.lbPath.GetPathPosition(rotTanDistAdjusted, LBPath.PositionType.Centre, true) - transform.position;
                    }
                    else
                    {
                        // Near the end of the path
                        rotTanDistAdjusted = distanceTravelled - rotationTangentDistance;
                        forwards = transform.position - cameraPath.lbPath.GetPathPosition(rotTanDistAdjusted, LBPath.PositionType.Centre, true);
                    }

                }
                else
                {
                    if (distanceTravelled < cameraPath.lbPath.splineLengthToLastPoint - rotationTangentDistance)
                    {
                        rotTanDistAdjusted = distanceTravelled + rotationTangentDistance;
                        forwards = cameraPath.lbPath.GetPathPosition(rotTanDistAdjusted, LBPath.PositionType.Centre, true) - transform.position;
                    }
                    else
                    {
                        // Near the end of the path
                        rotTanDistAdjusted = cameraPath.lbPath.splineLength - rotationTangentDistance;
                        forwards = cameraPath.lbPath.cachedPathPoints[cameraPath.lbPath.cachedPathPointsLength - 1] - cameraPath.lbPath.GetPathPosition(rotTanDistAdjusted, LBPath.PositionType.Centre, true);
                    }
                }

                // avoid "Look rotation viewing vector is zero" message
                //if (forwards != Vector3.zero)
                if (forwards.x != 0f && forwards.y != 0f && forwards.z != 0f)
                {
                    transform.rotation = Quaternion.LookRotation(forwards, Vector3.up);
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initialises the camera animator
        /// Optionally can initialise but not start camera moving
        /// Optionally don't start at the beginning of the path
        /// </summary>
        /// <param name="StartAtDistance"></param>
        public void BeginAnimation(bool StartAnimation = true, float StartAtDistance = 0f)
        {
            if (!isInitialised) { Initialise(); }

            startDistanceOffset = StartAtDistance;

            // Do some validation
            if (animatorCamera == null) { animatorCamera = GetComponent<Camera>(); }
            if (animatorCamera == null)
            {
                Debug.LogWarning("LBCameraAnimator.BeginAnimation - no camera attached to LBCameraAnimator gameobject");
                return;
            }
            else if (cameraPath == null)
            {
                Debug.LogWarning("LBCameraAnimator.BeginAnimation - no camera path defined in the LBCameraAnimator script. Did you forget to add one?");
                return;
            }
            else if (cameraPath.lbPath == null)
            {
                Debug.LogError("LBCameraAnimator.BeginAnimation - the camera path doesn't have a LBPath defined. Please report as a possible bug");
                return;
            }
            else if (cameraPath.lbPath.positionList == null)
            {
                Debug.LogError("LBCameraAnimator.BeginAnimation - the camera path doesn't have a list of positions defined. Please report as a possible bug");
                return;
            }
            else if (cameraPath.lbPath.positionList.Count < 2)
            {
                Debug.LogWarning("LBCameraAnimator.BeginAnimation - the camera path doesn't have 2 or more path points.");
                return;
            }

            // Reset timer
            pauseAtEndTimer = 0f;

            // Cache path position distances
            // CachePathPointDistances also caches the Path Points
            // that is required for CacheSplinePointDistances
            cameraPath.lbPath.CachePathPointDistances();
            cameraPath.lbPath.CacheSplinePointDistances();

            int numDisableCameraList = (disableCameraList == null ? 0 : disableCameraList.Count);

            // Disable other cameras
            if (disableCameraList != null)
            {
                Camera camera;
                AudioListener audioLnr;
                for (int dcIdx = 0; dcIdx < numDisableCameraList; dcIdx++)
                {
                    camera = disableCameraList[dcIdx];
                    if (camera != null)
                    {
                        camera.enabled = false;
                        // If there is an audio listener on this camera, turn it off
                        audioLnr = camera.GetComponent<AudioListener>();
                        if (audioLnr != null) { audioLnr.enabled = false; }
                    }
                }
            }

            if (audioListener != null && enableAudioListener) { audioListener.enabled = true; }

            // Disable the gameobjects
            if (disableGameObjectList != null)
            {
                foreach (GameObject gameObj in disableGameObjectList)
                {
                    if (gameObj != null) { gameObj.SetActive(false); }
                }
            }

            // What is the total distance of the animation path?
            maxDistanceToTravel = cameraPath.lbPath.splineLength - startDistanceOffset - endDistanceOffset;
            // Avoid div0 error
            if (maxDistanceToTravel == 0f) { maxDistanceToTravel = 0.001f; }

            if (StartAnimation && StartAtDistance < 0.001f) { StartFromBeginning(); }
            else
            {
                MoveTo(startDistanceOffset);
                move = StartAnimation && (maxDistanceToTravel > 0f);
            }

            // If required, add the AQUAS Camera script to this gameobject
            if (addAQUASCameraScript) { AddAQUASCameraComponent(true); }

            // Find LBLighting in the scene
            lbLighting = GameObject.FindObjectOfType<LBLighting>();

            // Fade the animation in
            if (StartAnimation && fadeIn && fadeInDuration > 0f)
            {
                if (lbLighting == null)
                {
                    Debug.LogWarning("LBCameraAnimator.BeginAnimation - cannot find LBLighting in the scene");
                }
                else
                {
                    lbLighting.fadeInDuration = fadeInDuration;
                    lbLighting.StartScreenFade(true);
                }
            }

            // If LBLighting is in the scene, remember the current camera
            if (lbLighting != null) { cameraToRestore = lbLighting.mainCamera; }

            // Turn on the camera
            if (animatorCamera != null)
            {
                // Change the LBLighting main camera to the animator camera
                if (lbLighting != null)
                {
                    lbLighting.mainCamera = animatorCamera;
                    lbLighting.CelestialsSetupCameras();
                }

                animatorCamera.enabled = true;

                #if VEGETATION_STUDIO
                Transform parentTrfm = transform.parent;
                if (parentTrfm != null)
                {
                    LBIntegration.VegetationStudioSetCamera(parentTrfm.GetComponent<LBLandscape>(), animatorCamera, 0, false);
                }
                #endif

                isRunning = true;
            }
        }

        /// <summary>
        /// End the animator, re-enable disabled cameras and gameobjects
        /// </summary>
        public void EndAnimation()
        {
            Stop();
            isRunning = false;

            if (animatorCamera != null)
            {
                #if VEGETATION_STUDIO
                Transform parentTrfm = transform.parent;
                if (parentTrfm != null)
                {
                    LBIntegration.VegetationStudioSetCamera(parentTrfm.GetComponent<LBLandscape>(), null, 0, false);
                }
                #endif

                animatorCamera.enabled = false;

                // Restore the LBLighting main camera to the animator camera
                if (lbLighting != null) { lbLighting.mainCamera = cameraToRestore; }
            }

            // Re-enable disabled gameobjects
            if (disableGameObjectList != null)
            {
                if (disableGameObjectList.Count == 1)
                {
                    disableGameObjectList[0].SetActive(true);
                }
                else if (disableGameObjectList.Count > 1)
                {
                    // Re-enable gameobjects in reverse order they were disabled
                    List<GameObject> reverseGameObjectList = new List<GameObject>();
                    reverseGameObjectList.AddRange(disableGameObjectList);
                    reverseGameObjectList.Reverse();
                    foreach (GameObject gameObj in reverseGameObjectList)
                    {
                        if (gameObj != null) { gameObj.SetActive(true); }
                    }
                }
            }

            // Re-enable other cameras
            int numDisableCameraList = (disableCameraList == null ? 0 : disableCameraList.Count);

            if (disableCameraList != null)
            {
                Camera camera;
                AudioListener audioLnr;
                for (int dcIdx = 0; dcIdx < numDisableCameraList; dcIdx++)
                {
                    camera = disableCameraList[dcIdx];
                    if (camera != null)
                    {
                        camera.enabled = true;

                        // If there is an audio listener on this camera, turn it on
                        // NOTE: This assume it was initially on...
                        audioLnr = camera.GetComponent<AudioListener>();
                        if (audioLnr != null) { audioLnr.enabled = true; }
                    }
                }
            }

            // Turn off the audio listener after the other cameras and objects have been re-enabled
            if (audioListener != null) { audioListener.enabled = false; }
        }

        /// <summary>
        /// Start the camera moving from the start of the path
        /// NOTE: The path length must be greater that 0
        /// </summary>
        public void StartFromBeginning()
        {
            distanceTravelled = 0f;
            move = (maxDistanceToTravel > 0f);
        }

        /// <summary>
        /// Stop the camera moving along the path
        /// </summary>
        public void Stop()
        {
            move = false;
        }

        /// <summary>
        /// Continue moving the camera along the path (start it going again)
        /// NOTE: The path length must be greater that 0
        /// </summary>
        public void Continue()
        {
            move = (maxDistanceToTravel > 0f);
        }

        /// <summary>
        /// Move the camera to the point that is distance metres along the path
        /// </summary>
        public void MoveTo(float distance)
        {
            distanceTravelled = distance;
            if (cameraPath != null) { UpdateCameraTransform(); }
        }

        /// <summary>
        /// Set the movement speed of the camera. Making it negative will make it move backwards along the path
        /// </summary>
        public void SetMoveSpeed(float speed)
        {
            moveSpeed = speed;
        }

        /// <summary>
        /// Get the list of cameras in the scene - ignore the Celestrials Camera uses for the stars and
        /// the animator camera. Also ignore Water4Advanced Reflection cameras
        /// </summary>
        public void RefreshCameraList()
        {
            Camera[] sceneCameras = GameObject.FindObjectsOfType<Camera>();

            if (disableCameraList == null) { disableCameraList = new List<Camera>(); }

            disableCameraList.Clear();
            disableCameraList.AddRange(sceneCameras);

            // If present, remove the celestrials and water reflection cameras
            disableCameraList.RemoveAll(c => c.gameObject.name == "Celestials Camera" || c.gameObject.name.StartsWith("Water4AdvancedReflection"));

            // Attempt to remove the camera attached to this animator from the list
            if (animatorCamera != null)
            {
                Camera findCamera = disableCameraList.Find(c => c == animatorCamera);
                if (findCamera != null) { disableCameraList.Remove(findCamera); }
            }
        }

        public bool AddAQUASCameraComponent(bool showErrors)
        {
            bool isSuccessful = false;

            if (addAQUASCameraScript)
            {
                LBIntegration.AddAQUASCameraScript(this.gameObject, showErrors, ref isSuccessful);
            }

            return isSuccessful;
        }

        public bool RemoveAQUASCameraComponent(bool showErrors)
        {
            bool isSuccessful = false;

            if (!addAQUASCameraScript)
            {
                LBIntegration.RemoveAQUASCameraScript(this.gameObject, showErrors, ref isSuccessful);
            }

            return isSuccessful;
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Add a Camera Animator to the scene as a child of a landscape (new in 1.3.2 Beta 8f)
        /// Added isWakeOnStartEnabled (new in 1.3.2 Beta 8s) to work with Templates
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="GameObjectName"></param>
        /// <param name="isWakeOnStartEnabled"></param>
        /// <returns></returns>
        public static LBCameraAnimator CreateCameraAnimator(LBLandscape landscape, string GameObjectName, bool isWakeOnStartEnabled = true)
        {
            LBCameraAnimator lbCameraAnimator = null;

            // Create an empty game object in the scene
            GameObject animatorObject = new GameObject(GameObjectName);
            if (animatorObject != null)
            {
                // If the landcape gameobject is available, add the Camera Animator as a child object.
                if (landscape != null)
                {
                    if (landscape.gameObject != null)
                    {
                        animatorObject.transform.parent = landscape.gameObject.transform;
                    }
                }

                // Add the camera
                Camera animatorCamera = animatorObject.AddComponent<Camera>();
                lbCameraAnimator = animatorObject.AddComponent<LBCameraAnimator>();
                if (animatorCamera != null)
                {
                    // Disable camera by default
                    animatorCamera.enabled = false;

                    // Set near clipping plane to avoid z-fighting (default 0.3)
                    animatorCamera.nearClipPlane = 0.7f;

                    // Set a new far clip plane (Unity default is 1000)
                    animatorCamera.farClipPlane = landscape.size.x * 1.5f;

                    // Get reference to camera so we don't need to call GetComponent later
                    if (lbCameraAnimator != null) { lbCameraAnimator.animatorCamera = animatorCamera; }

                    // If LBLighting is in scene add camera to weathCameraList if useWeather is true
                    LBLighting lbLighting = GameObject.FindObjectOfType<LBLighting>();
                    if (lbLighting != null)
                    {
                        if (lbLighting.weatherCameraList != null && lbLighting.useWeather)
                        {
                            lbLighting.weatherCameraList.Add(animatorCamera);
                        }
                    }
                }
                AudioListener audioListener = animatorObject.GetComponent<AudioListener>();
                if (audioListener != null)
                {
                    audioListener.enabled = false;
                    // Get reference to audiolistener so we don't need to call GetComponent later
                    if (lbCameraAnimator != null) { lbCameraAnimator.audioListener = audioListener; }
                }

                lbCameraAnimator.RefreshCameraList();
            }

            return lbCameraAnimator;
        }

        /// <summary>
        /// Get a list of all camera animators that are under of children of a landscape
        /// </summary>
        /// <param name="lbLandscape"></param>
        /// <returns></returns>
        public static List<LBCameraAnimator> GetCameraAnimatorsInLandscape(LBLandscape lbLandscape)
        {
            if (lbLandscape == null) { return new List<LBCameraAnimator>(); }
            else { return new List<LBCameraAnimator>(lbLandscape.GetComponentsInChildren<LBCameraAnimator>(true)); }
        }

        /// <summary>
        /// Return the first valid CameraAnimator with a valid CameraPath in the landscape
        /// </summary>
        /// <param name="lbLandscape"></param>
        /// <returns></returns>
        public static LBCameraAnimator GetFirstCameraAnimatorInLandscape(LBLandscape lbLandscape)
        {
            if (lbLandscape == null) { return null; }
            else { return GetCameraAnimatorsInLandscape(lbLandscape).Find(c => c != null && c.cameraPath != null && c.cameraPath.lbPath != null); }
        }

        /// <summary>
        /// Remove all the camera animators from the scene which are under or children of a landscape
        /// Stop any animations that are currently running before removing them. This will restore original scripts, cameras etc.
        /// </summary>
        /// <param name="lbLandscape"></param>
        /// <param name="showErrors"></param>
        public static void RemoveCameraAnimatorsFromScene(LBLandscape lbLandscape, bool showErrors = true)
        {
            if (lbLandscape == null) { if (showErrors) { Debug.LogWarning("LBCameraAnimator.RemoveCameraAnimatorsFromScene - landscape is not defined"); } }
            else
            {
                List<LBCameraAnimator> lbCameraAnimatorList = GetCameraAnimatorsInLandscape(lbLandscape);

                if (lbCameraAnimatorList != null)
                {
                    // Go backwards through the list
                    for (int i = lbCameraAnimatorList.Count - 1; i >= 0; i--)
                    {
                        if (lbCameraAnimatorList[i].IsRunning) { lbCameraAnimatorList[i].EndAnimation(); }
                        DestroyImmediate(lbCameraAnimatorList[i].gameObject);
                    }
                }
            }
        }

        #endregion
    }

    #region LBCameraAnimatorInspector

#if UNITY_EDITOR
    [CustomEditor(typeof(LBCameraAnimator))]
    public class LBCameraAnimatorInspector : Editor
    {
        // Custom editor for LB Camera Animator

        #region Variables
        private bool isSceneSaveRequired = false;
        private GUIStyle buttonCompact;
        private int i = 0;
        private Camera cameraToRemove = null;
        private GameObject gameObjToMoveDown = null;
        private int gameObjPosToRemove = -1;
        private int gameObjPosToMoveDown = -1;
        private float pathLength = 0f;

        private string tooltipSpeedDistCurve = "The speed of the camera over the distance of the path. 0 and 1 on the x-axis correspond to the start and end of the path. 0 and 1 on the y-axis correspond to the min and max move speeds.";
        private string tooltipRotTangentDist = "Increasing this value will (up to a point) make the animation of the camera’s rotation smoother but will potentially make it less accurate.";

        private List<int> gizmoClassIdList;
        #endregion

        #region Static GUIContent
        private static GUIContent previewInEditorContent = new GUIContent("Preview in Editor", "Preview the camera animation in the editor");
        private static GUIContent moveSpeedContent = new GUIContent("Move Speed", "The (approximate) speed the camera will move along the path if its speed is not animated, in metres per second.");
        private static GUIContent startOnWakeContent = new GUIContent("Start On Awake", "The camera will automatically start animating along the path when the scene loads.");
        private static GUIContent startDistOffsetContent = new GUIContent("Start Distance Offset", "The distance, in metres, from the start of the path, the camera animation will begin.");
        private static GUIContent endDistOffsetContent = new GUIContent("End Distance Offset", "The distance, in metres, from the end of the path, the camera animation will stop.");
        private static GUIContent enableAudioListenerContent = new GUIContent("Enable Audio Listener", "Turn on the audio listener attached to the camera animator while the animation is running");
        private static GUIContent fadeInContent = new GUIContent("Fade In", "Fade the camera animation in when it first starts");
        private static GUIContent fadeInDurationContent = new GUIContent("Fade In Duration", "The length of time in seconds, it takes to fully fade in the camera animation");
        private static GUIContent endPauseDurationContent = new GUIContent("End Pause Duration", "The length of time in seconds, that the camera will be paused at the end of the animation before it finishes. Not available if the path is a closed circuit.");

        #endregion

        /// <summary>
        /// Called automatically by Unity when the Editor get's enabled, which
        /// is essentially when the parent gameobject gets selected in the Hierarchy
        /// </summary>
        private void OnEnable()
        {
            LBCameraAnimator cameraAnimatorScript = (LBCameraAnimator)target;

            if (gizmoClassIdList == null) { gizmoClassIdList = new List<int>(); }
            if (gizmoClassIdList != null)
            {
                // Only populate the list if it hasn't been done yet
                if (gizmoClassIdList.Count == 0)
                {
                    gizmoClassIdList.Add(20);  // Camera
                    gizmoClassIdList.Add(223); // Canvas
                }
            }

            // If the project has just been opened and preview is on
            // then we need to re-initialise preview mode
            if (cameraAnimatorScript.isEditorPreviewEnabled && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (LBSetup.isProjectOpenedAnimator)
                {
                    //Debug.Log("LBCameraAnimator - initialising Preview Mode");
                    LBSetup.isProjectOpenedAnimator = false;
                    EnablePreviewMode(true);
                }
            }

            // Add an event to trigger when changing the play mode state
            //EditorApplication.playmodeStateChanged -= PlayModeStateChanged;
            //EditorApplication.playmodeStateChanged += PlayModeStateChanged;
        }

        /// <summary>
        /// This event is fired when the state of play mode is changed
        /// To add the event handle see OnEnable()
        /// NOTE: Calling EnablePreviewMode(false) when entering Play will crash Unity.
        /// </summary>
        private void PlayModeStateChanged()
        {
            LBCameraAnimator cameraAnimatorScript = (LBCameraAnimator)target;

            if (EditorApplication.isPlayingOrWillChangePlaymode && cameraAnimatorScript.isEditorPreviewEnabled)
            {
                //Debug.Log("LBCameraAnimator - turning off Preview Mode");
                //EnablePreviewMode(false);
            }
        }

        // This function overides what is normally seen in the inspector window
        // This allows stuff like buttons to be drawn there
        public override void OnInspectorGUI()
        {
            LBCameraAnimator cameraAnimatorScript = (LBCameraAnimator)target;

            EditorGUIUtility.labelWidth = 160f;
            buttonCompact = new GUIStyle("Button");
            buttonCompact.fontSize = 8;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUI.BeginChangeCheck();
            cameraAnimatorScript.cameraPath = (LBCameraPath)EditorGUILayout.ObjectField("Camera Path", cameraAnimatorScript.cameraPath, typeof(LBCameraPath), true);
            if (EditorGUI.EndChangeCheck()) { isSceneSaveRequired = true; pathLength = 0f; }
            if (cameraAnimatorScript.cameraPath == null) { }
            else if (cameraAnimatorScript.cameraPath.lbPath != null)
            {
                pathLength = cameraAnimatorScript.cameraPath.lbPath.splineLength;
                if (GUILayout.Button("Verify Path", GUILayout.MaxWidth(90f)))
                {
                    string msg = string.Empty;
                    if (pathLength < 2) { msg = "There needs to be at least 2 points in your path."; }
                    else
                    {
                        float minDistance = cameraAnimatorScript.cameraPath.lbPath.GetMinDistanceBetweenPoints();

                        if (minDistance < 3f)
                        {
                            msg = "The minimum recommended distance between points is 3 metres. The minimum distance between points is currently " + minDistance.ToString("0.000") +
                                  ". This may cause issues with the animator.";
                        }
                        else { msg = "The current path looks okay. You are good to go."; }
                    }
                    EditorUtility.DisplayDialog("Landscape Builder Camera Animator", msg, "OK");
                }

                // Camera Animator Preview Mode
                if (pathLength > 0f)
                {
                    EditorGUI.BeginChangeCheck();
                    cameraAnimatorScript.isEditorPreviewEnabled = EditorGUILayout.Toggle(previewInEditorContent, cameraAnimatorScript.isEditorPreviewEnabled);
                    if (EditorGUI.EndChangeCheck())
                    {
                        isSceneSaveRequired = true;

                        EnablePreviewMode(cameraAnimatorScript.isEditorPreviewEnabled);
                    }

                    // If in Editor Preview mode, display a slider to allow user to move the camera in the scene
                    if (cameraAnimatorScript.isEditorPreviewEnabled)
                    {
                        EditorGUI.BeginChangeCheck();
                        cameraAnimatorScript.previewDistanceOffset = EditorGUILayout.Slider(cameraAnimatorScript.previewDistanceOffset, 0f, Mathf.Max(1f, pathLength));
                        if (EditorGUI.EndChangeCheck())
                        {
                            UpdatePreviewSceneView(cameraAnimatorScript);
                        }
                    }
                }
                else
                {
                    // If the path length = 0, turn off preview mode
                    cameraAnimatorScript.isEditorPreviewEnabled = false;
                    cameraAnimatorScript.previewDistanceOffset = 0f;
                }
            }

            EditorGUI.BeginChangeCheck();
            cameraAnimatorScript.animateSpeed = EditorGUILayout.Toggle("Animate Speed", cameraAnimatorScript.animateSpeed);
            if (cameraAnimatorScript.animateSpeed)
            {
                cameraAnimatorScript.minMoveSpeed = EditorGUILayout.FloatField("Min Move Speed", cameraAnimatorScript.minMoveSpeed);
                cameraAnimatorScript.maxMoveSpeed = EditorGUILayout.FloatField("Max Move Speed", cameraAnimatorScript.maxMoveSpeed);

                EditorGUILayout.BeginHorizontal();
                cameraAnimatorScript.speedTimeCurve = EditorGUILayout.CurveField(new GUIContent("Speed vs Dist Curve", tooltipSpeedDistCurve), cameraAnimatorScript.speedTimeCurve);
                if (GUILayout.Button("S", buttonCompact, GUILayout.MaxWidth(20f))) { Debug.Log(LBCurve.ScriptCurve(cameraAnimatorScript.speedTimeCurve)); }
                EditorGUILayout.EndHorizontal();
            }
            else { cameraAnimatorScript.moveSpeed = EditorGUILayout.FloatField(moveSpeedContent, cameraAnimatorScript.moveSpeed); }

            cameraAnimatorScript.rotateCamera = EditorGUILayout.Toggle("Rotate Camera", cameraAnimatorScript.rotateCamera);
            if (cameraAnimatorScript.rotateCamera)
            {
                cameraAnimatorScript.rotationTangentDistance = EditorGUILayout.Slider(new GUIContent("Rotation Tangent Distance", tooltipRotTangentDist), cameraAnimatorScript.rotationTangentDistance, 0.1f, 10f);
            }
            cameraAnimatorScript.startOnAwake = EditorGUILayout.Toggle(startOnWakeContent, cameraAnimatorScript.startOnAwake);
            if (cameraAnimatorScript.startOnAwake)
            {
                cameraAnimatorScript.startDistanceOffset = EditorGUILayout.Slider(startDistOffsetContent, cameraAnimatorScript.startDistanceOffset, 0f, Mathf.Max(1f, pathLength));
                // pathlength - endoffset should never = 0 (it may cause div0 error in update)
                cameraAnimatorScript.endDistanceOffset = EditorGUILayout.Slider(endDistOffsetContent, cameraAnimatorScript.endDistanceOffset, 0f, pathLength - cameraAnimatorScript.startDistanceOffset -1f);
            }
            cameraAnimatorScript.enableAudioListener = EditorGUILayout.Toggle(enableAudioListenerContent, cameraAnimatorScript.enableAudioListener);
            if (EditorGUI.EndChangeCheck()) { isSceneSaveRequired = true; }

            // Fade in/out using LBLighting
            EditorGUI.BeginChangeCheck();
            cameraAnimatorScript.fadeIn = EditorGUILayout.Toggle(fadeInContent, cameraAnimatorScript.fadeIn);
            if (cameraAnimatorScript.fadeIn)
            {
                cameraAnimatorScript.fadeInDuration = EditorGUILayout.Slider(fadeInDurationContent, cameraAnimatorScript.fadeInDuration, 0f, 20f);
            }
            //cameraAnimatorScript.fadeOut = EditorGUILayout.Toggle(new GUIContent("Fade Out", "Fade the camera animation out when it first starts"), cameraAnimatorScript.fadeOut);
            //cameraAnimatorScript.fadeOutDuration = EditorGUILayout.Slider(new GUIContent("Fade Out Duration", "The length of time in seconds, it takes to fully fade out the camera animation"), cameraAnimatorScript.fadeOutDuration, 0f, 20f);
            if (EditorGUI.EndChangeCheck()) { isSceneSaveRequired = true; }

            // If the path is not closed, allow user to set a duration to pause the animation at the end
            if (cameraAnimatorScript.cameraPath != null)
            {
                if (!cameraAnimatorScript.cameraPath.lbPath.closedCircuit)
                {
                    EditorGUI.BeginChangeCheck();
                    cameraAnimatorScript.pauseAtEndDuration = EditorGUILayout.Slider(endPauseDurationContent, cameraAnimatorScript.pauseAtEndDuration, 0f, 300f);
                    if (EditorGUI.EndChangeCheck()) { isSceneSaveRequired = true; }
                }
            }

            EditorGUI.BeginChangeCheck();
            cameraAnimatorScript.addAQUASCameraScript = EditorGUILayout.Toggle("Add AQUAS camera script", cameraAnimatorScript.addAQUASCameraScript);
            if (EditorGUI.EndChangeCheck())
            {
                if (cameraAnimatorScript.addAQUASCameraScript) { cameraAnimatorScript.AddAQUASCameraComponent(true); isSceneSaveRequired = true; }
                else
                {
                    cameraAnimatorScript.RemoveAQUASCameraComponent(true);

                    // When a component attached to the same custom inspector is removed, Unity may attempt to redraw it
                    // after it has been removed. To avoid this mark scene as dirty and exit the OnInspectorGUI()
                    isSceneSaveRequired = false;
                    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                    EditorGUIUtility.ExitGUI();
                }
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Cameras to disable", GUILayout.MaxWidth(155f));
            if (GUILayout.Button("Get Cameras", GUILayout.MaxWidth(100f)))
            {
                cameraAnimatorScript.RefreshCameraList();
                isSceneSaveRequired = true;
            }
            if (GUILayout.Button("+", GUILayout.Width(25f)))
            {
                if (cameraAnimatorScript.disableCameraList == null) { cameraAnimatorScript.disableCameraList = new List<Camera>(); }
                cameraAnimatorScript.disableCameraList.Add(null);
                isSceneSaveRequired = true;
            }

            if (GUILayout.Button("-", GUILayout.Width(25f)))
            {
                if (cameraAnimatorScript.disableCameraList == null) { cameraAnimatorScript.disableCameraList = new List<Camera>(); }
                if (cameraAnimatorScript.disableCameraList.Count > 0)
                {
                    cameraAnimatorScript.disableCameraList.RemoveAt(cameraAnimatorScript.disableCameraList.Count - 1);
                    isSceneSaveRequired = true;
                }
            }
            EditorGUILayout.EndHorizontal();

            // Display list of cameras to disable
            if (cameraAnimatorScript.disableCameraList != null)
            {
                cameraToRemove = null;
                for (i = 0; i < cameraAnimatorScript.disableCameraList.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("X", buttonCompact, GUILayout.MaxWidth(20f))) { cameraToRemove = cameraAnimatorScript.disableCameraList[i]; }
                    cameraAnimatorScript.disableCameraList[i] = (Camera)EditorGUILayout.ObjectField(cameraAnimatorScript.disableCameraList[i], typeof(Camera), true);
                    EditorGUILayout.EndHorizontal();
                }

                // Does the user wish to remove a camera from the list?
                if (cameraToRemove != null)
                {
                    cameraAnimatorScript.disableCameraList.Remove(cameraToRemove);
                    isSceneSaveRequired = true;
                }
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("GameObjects to disable");
            if (GUILayout.Button("+", GUILayout.Width(25f)))
            {
                // If the scene hasn't been run, we need to make sure a list is available
                if (cameraAnimatorScript.disableGameObjectList == null) { cameraAnimatorScript.disableGameObjectList = new List<GameObject>(); }
                cameraAnimatorScript.disableGameObjectList.Add(null);
                isSceneSaveRequired = true;
            }

            if (GUILayout.Button("-", GUILayout.Width(25f)))
            {
                // If the scene hasn't been run, we need to make sure a list is available
                if (cameraAnimatorScript.disableGameObjectList == null) { cameraAnimatorScript.disableGameObjectList = new List<GameObject>(); }
                if (cameraAnimatorScript.disableGameObjectList.Count > 0)
                {
                    cameraAnimatorScript.disableGameObjectList.RemoveAt(cameraAnimatorScript.disableGameObjectList.Count - 1);
                    isSceneSaveRequired = true;
                }
            }
            EditorGUILayout.EndHorizontal();

            // Display list of gameobjects to disable
            if (cameraAnimatorScript.disableGameObjectList != null)
            {
                gameObjPosToRemove = -1;
                gameObjPosToMoveDown = -1;
                gameObjToMoveDown = null;
                for (i = 0; i < cameraAnimatorScript.disableGameObjectList.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("v", buttonCompact, GUILayout.MaxWidth(20f))) { gameObjPosToMoveDown = i; gameObjToMoveDown = cameraAnimatorScript.disableGameObjectList[i]; }
                    if (GUILayout.Button("X", buttonCompact, GUILayout.MaxWidth(20f))) { gameObjPosToRemove = i; }
                    cameraAnimatorScript.disableGameObjectList[i] = (GameObject)EditorGUILayout.ObjectField(cameraAnimatorScript.disableGameObjectList[i], typeof(GameObject), true);
                    EditorGUILayout.EndHorizontal();
                }

                // Does the user wish to remove a gameobject from the list?
                if (gameObjPosToRemove >= 0)
                {
                    cameraAnimatorScript.disableGameObjectList.RemoveAt(gameObjPosToRemove);
                    isSceneSaveRequired = true;
                }

                // Does the user wish to move a gameobject down in the list or wrap last one to first position?
                if (gameObjPosToMoveDown >= 0)
                {
                    // Attempt to move this gameobject down one in the list
                    if (cameraAnimatorScript.disableGameObjectList.Count > 1)
                    {
                        // If this is the last in the list we want to put it at the top
                        if (gameObjPosToMoveDown == cameraAnimatorScript.disableGameObjectList.Count - 1)
                        {
                            cameraAnimatorScript.disableGameObjectList.Insert(0, gameObjToMoveDown);
                            cameraAnimatorScript.disableGameObjectList.RemoveAt(cameraAnimatorScript.disableGameObjectList.Count - 1);
                        }
                        else
                        {
                            // Move down one in the list
                            cameraAnimatorScript.disableGameObjectList.RemoveAt(gameObjPosToMoveDown);
                            cameraAnimatorScript.disableGameObjectList.Insert(gameObjPosToMoveDown + 1, gameObjToMoveDown);
                        }
                        isSceneSaveRequired = true;
                    }
                }
            }

            EditorGUILayout.EndVertical();

            // Some of the controls have changed, so mark the scene as changed so that
            // if it is run without the LBCameraPath editor script open, the values will persist.
            if (isSceneSaveRequired && !EditorApplication.isPlaying)
            {
                isSceneSaveRequired = false;
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
        }


        private void UpdatePreviewSceneView(LBCameraAnimator cameraAnimatorScript)
        {
            cameraAnimatorScript.MoveTo(cameraAnimatorScript.previewDistanceOffset);

            if (cameraAnimatorScript.animatorCamera != null)
            {
                try
                {
                    // Align the scene view with the animator camera view
                    SceneView sceneView = UnityEditor.SceneView.lastActiveSceneView;
                    if (sceneView != null)
                    {
                        // Set the position of the camera
                        sceneView.pivot = cameraAnimatorScript.animatorCamera.transform.position;
                        sceneView.rotation = cameraAnimatorScript.animatorCamera.transform.rotation;
                        //sceneView.camera.fieldOfView = cameraAnimatorScript.animatorCamera.fieldOfView;
                        //sceneView.camera.rect = cameraAnimatorScript.animatorCamera.rect;

                        // Align the view with the new position
                        sceneView.AlignViewToObject(cameraAnimatorScript.animatorCamera.transform);

                        sceneView.Repaint();
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("Couldn't set the scene view. Is Scene tab selected?\n" + ex.Message);
                }
            }
        }

        /// <summary>
        /// Enable or disable Editor Preview mode
        /// NOTE: Calling EnablePreviewMode(false) when entering Play will crash Unity.
        /// </summary>
        /// <param name="isEnabled"></param>
        private void EnablePreviewMode(bool isEnabled)
        {
            LBCameraAnimator cameraAnimatorScript = (LBCameraAnimator)target;

            if (isEnabled)
            {
                if (cameraAnimatorScript.cameraPath != null)
                {
                    cameraAnimatorScript.BeginAnimation(false, cameraAnimatorScript.startDistanceOffset);
                    cameraAnimatorScript.previewDistanceOffset = cameraAnimatorScript.startDistanceOffset;
                    cameraAnimatorScript.showPathInScenePrePreviewMode = cameraAnimatorScript.cameraPath.lbPath.showPathInScene;

                    LBEditorHelper.ToggleGizmos(gizmoClassIdList, false);

                    // Hide the camera path in the scene
                    cameraAnimatorScript.cameraPath.lbPath.showPathInScene = false;

                    // Turn off the default scene handles
                    Tools.hidden = true;

                    if (!EditorApplication.isPlaying && !EditorApplication.isPaused)
                    {
                        // Find LBLighting in the scene
                        LBLighting lbLighting = GameObject.FindObjectOfType<LBLighting>();
                        // Calling UpdateLightingPreview() before AddWeatherToCamera can prevent a crash
                        // in UpdateLightingPreview() after UpdatePreviewSceneView(). Not sure why...
                        if (lbLighting != null) { lbLighting.UpdateLightingPreview(); }

                        UpdatePreviewSceneView(cameraAnimatorScript);

                        // If LBLighting is in scene, setup the a point-in-time preview
                        // This will also preview weather if Use Weather is enabled and this
                        // animation camera is in the Weather Cameras list.
                        if (lbLighting != null)
                        {
                            // Check if this camera is in the weather camera list
                            if (lbLighting.weatherCameraList != null)
                            {
                                if (lbLighting.weatherCameraList.FindIndex(c => c == cameraAnimatorScript.animatorCamera) >= 0)
                                {
                                    // Ensure the WeatherFX script is attached correctly
                                    lbLighting.AddWeatherToCamera(cameraAnimatorScript.animatorCamera, true);
                                }
                            }

                            // THIS CAUSES UNITY TO CRASH if UpdateLightingPreview() is not also called above...
                            // Preview the current lighting settings
                            lbLighting.UpdateLightingPreview();

                            //Debug.Log("LBCameraAnimator CurrentTime: " + lightingScript.CurrentTime());
                        }
                    }
                }
            }
            else
            {
                // Disable the preview mode
                cameraAnimatorScript.EndAnimation();
                cameraAnimatorScript.previewDistanceOffset = 0f;
                // Restore the state of the camera path
                cameraAnimatorScript.cameraPath.lbPath.showPathInScene = cameraAnimatorScript.showPathInScenePrePreviewMode;

                LBEditorHelper.ToggleGizmos(gizmoClassIdList, true);

                Tools.hidden = false;
            }
        }

    }
#endif

    #endregion
}