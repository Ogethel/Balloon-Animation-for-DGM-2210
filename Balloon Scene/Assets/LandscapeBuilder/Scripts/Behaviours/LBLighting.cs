// Landscape Builder. Copyright (c) 2016-2019 SCSM Pty Ltd. All rights reserved.
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine.UI;
using System;

namespace LandscapeBuilder
{
    [AddComponentMenu("Landscape Builder/Lighting")]
    public class LBLighting : MonoBehaviour
    {
        #region enumerations
        public enum LightingMode
        {
            Static,
            Dynamic
        }

        public enum SetupMode
        {
            Simple,
            Advanced
        }

        public enum SkyboxType
        {
            Procedural,
            SixSided
        }

        public enum ReflectionProbeUpdateMode
        {
            DontUpdate,
            UpdateThisProbe,
            UpdateAllProbes
        }

        /// <summary>
        /// Baked lighting mode from Unity Light inspector
        /// </summary>
        public enum LightBakingMode
        {
            Mixed = 1,
            Baked = 2,
            Realtime = 4
        }

        /// <summary>
        /// Type of particle precipitation 
        /// </summary>
        public enum PrecipitationType
        {
            Rain = 0,
            Hail = 1,
            Snow = 2
        }

        public enum EnvAmbientSource
        {
            Colour = 0,
            Gradient = 1
        }

        #endregion

        #region Variables and Properties
        public Light sun;
        [Range(0f, 8f)] public float sunIntensity = 1f;
        [Range(0f, 360f)] public float yAxisRotation = 0f;

        public LightingMode lightingMode = LightingMode.Dynamic;
        public SetupMode setupMode = SetupMode.Simple;
        public EnvAmbientSource envAmbientSource = EnvAmbientSource.Colour;

        public SkyboxType skyboxType = SkyboxType.Procedural;
        public List<LBSkybox> skyboxesList;
        public Material proceduralSkybox;
        public Material blendedSkybox;
        private Material defaultSkybox;
        private int currentSkybox1 = -1;
        private int currentSkybox2 = -1;

        // Dynamic lighting variables
        public float dayLength = 1440f;                 // Day length in seconds
        private float dayTimer = 0f;
        public bool realtimeTerrainLighting = true;
        public ReflectionProbeUpdateMode reflProbesUpdateMode = ReflectionProbeUpdateMode.UpdateThisProbe;
        public float lightingUpdateInterval = 5f;       // Interval (in seconds) between lighting updates
        private float lightingUpdateTimer = 0f;

        [Range(0f, 24f)] public float startTimeOfDay = 12f;              // Start time of day in hours - based on 24-hour time
        private float staticTimeFloat = 0f;
        [Range(2f, 10f)] public float sunriseTime = 6f;
        [Range(14f, 22f)] public float sunsetTime = 18f;

        // General Lighting Settings (Default settings)
        public float GetDefaultSunIntensity { get { return 1f; } }
        public float GetDefaultYAxisRotation { get { return 0f; } }
        public float GetDefaultDayLength { get { return 1440f; } }
        public bool GetDefaultRealtimeTerrainLighting { get { return true; } }
        public ReflectionProbeUpdateMode GetDefaultReflProbesUpdateMode { get { return ReflectionProbeUpdateMode.UpdateThisProbe; } }
        public float GetDefaultLightingUpdateInterval { get { return 5f; } }
        public float GetDefaultStartTimeOfDay { get { return 12f; } }
        public float GetDefaultSunriseTime { get { return 6f; } }
        public float GetDefaultSunsetTime { get { return 18f; } }

        // Ambient Light and Night Setttings
        // dayAmbientLight and nightAmbientLight are also used for ambient Sky colour for environment gradient mode
        public Color dayAmbientLight = new Color(71f / 255f, 75f / 255f, 81f / 255f, 1f);
        public Color nightAmbientLight = new Color(21f / 255f, 21f / 255f, 21f / 255f, 1f);
        public Color dayAmbientGndLight = new Color(12f / 255f, 11f / 255f, 9f / 255f, 1f);
        public Color nightAmbientGndLight = new Color(4f / 255f, 4f / 255f, 3f / 255f, 1f);
        public Color dayAmbientHznLight = new Color(29f / 255f, 32f / 255f, 34f / 255f, 1f);
        public Color nightAmbientHznLight = new Color(12f / 255f, 11f / 255f, 9f / 255f, 1f);

        #if UNITY_2018_1_OR_NEWER
        [ColorUsageAttribute(true, true)] public Color dayAmbientLightHDR = new Color(71f / 255f, 75f / 255f, 81f / 255f, 1f);
        [ColorUsageAttribute(true, true)] public Color nightAmbientLightHDR = new Color(21f / 255f, 21f / 255f, 21f / 255f, 1f);
        [ColorUsageAttribute(true, true)] public Color dayAmbientGndLightHDR = new Color(12f / 255f, 11f / 255f, 9f / 255f, 1f);
        [ColorUsageAttribute(true, true)] public Color nightAmbientGndLightHDR = new Color(4f / 255f, 4f / 255f, 3f / 255f, 1f);
        [ColorUsageAttribute(true, true)] public Color dayAmbientHznLightHDR = new Color(29f / 255f, 32f / 255f, 34f / 255f, 1f);
        [ColorUsageAttribute(true, true)] public Color nightAmbientHznLightHDR = new Color(12f / 255f, 11f / 255f, 9f / 255f, 1f);
        #else
        [ColorUsageAttribute(true, true, 0f, 8f, 0.125f, 3f)] public Color dayAmbientLightHDR = new Color(71f / 255f, 75f / 255f, 81f / 255f, 1f);
        [ColorUsageAttribute(true, true, 0f, 8f, 0.125f, 3f)] public Color nightAmbientLightHDR = new Color(21f / 255f, 21f / 255f, 21f / 255f, 1f);
        [ColorUsageAttribute(true, true, 0f, 8f, 0.125f, 3f)] public Color dayAmbientGndLightHDR = new Color(12f / 255f, 11f / 255f, 9f / 255f, 1f);
        [ColorUsageAttribute(true, true, 0f, 8f, 0.125f, 3f)] public Color nightAmbientGndLightHDR = new Color(4f / 255f, 4f / 255f, 3f / 255f, 1f);
        [ColorUsageAttribute(true, true, 0f, 8f, 0.125f, 3f)] public Color dayAmbientHznLightHDR = new Color(29f / 255f, 32f / 255f, 34f / 255f, 1f);
        [ColorUsageAttribute(true, true, 0f, 8f, 0.125f, 3f)] public Color nightAmbientHznLightHDR = new Color(12f / 255f, 11f / 255f, 9f / 255f, 1f);

        #endif

        public Color dayFogColour = new Color(158f / 255f, 189f / 255f, 195f / 255f, 1f);
        public Color nightFogColour = new Color(0f, 0f, 0f, 1f);

        public float dayFogDensity = 0.0005f;
        public float nightFogDensity = 0.005f;

        // Ambient Light and Night Settings (Default settings)
        public Color GetDefaultDayAmbientLight { get { return new Color(71f / 255f, 75f / 255f, 81f / 255f, 1f); } }
        public Color GetDefaultNightAmbientLight { get { return new Color(21f / 255f, 21f / 255f, 21f / 255f, 1f); } }
        public Color GetDefaultDayAmbientGndLight { get { return new Color(12f / 255f, 11f / 255f, 9f / 255f, 1f); } }
        public Color GetDefaultNightAmbientGndLight { get { return new Color(4f / 255f, 4f / 255f, 3f / 255f, 1f); } }
        public Color GetDefaultDayAmbientHznLight { get { return new Color(29f / 255f, 32f / 255f, 34f / 255f, 1f); } }
        public Color GetDefaultNightAmbientHznLight { get { return new Color(12f / 255f, 11f / 255f, 9f / 255f, 1f); } }
        public Color GetDefaultDayFogColour { get { return new Color(158f / 255f, 189f / 255f, 195f / 255f, 1f); } }
        public Color GetDefaultNightFogColour { get { return new Color(0f, 0f, 0f, 1f); } }
        public float GetDefaultDayFogDensity { get { return 0.0005f; } }
        public float GetDefaultNightFogDensity { get { return 0.005f; } }

        // Advanced setup variables
        public AnimationCurve sunIntensityCurve = LBCurve.DefaultSunIntensityCurve();
        // Unity 2018.2+ does not allow Gradient.colorKeys to be set when the monobehaviour class constructor is called. See InitialiseGradients()
        public Gradient ambientLightGradient;  // Also used for ambient Sky colour time-based "gradient" for environment gradient mode
        public Gradient ambientHznLightGradient;
        public Gradient ambientGndLightGradient;
        public Gradient fogColourGradient;
        public AnimationCurve fogDensityCurve = LBCurve.DefaultFogDensityCurve();
        public float minFogDensity = 0.0005f;
        public float maxFogDensity = 0.005f;
        public AnimationCurve moonIntensityCurve = LBCurve.DefaultMoonIntensityCurve();
        public AnimationCurve starVisibilityCurve = LBCurve.DefaultStarVisibilityCurve();

        // Advanced setup variables (Default settings)
        public AnimationCurve GetDefaultSunIntensityCurve { get { return LBCurve.DefaultSunIntensityCurve(); } }
        public Gradient GetDefaultAmbientLightGradient { get { return LBGradient.SetGradientFromPreset(LBGradient.GradientPreset.DefaultAmbientLight); } }
        public Gradient GetDefaultAmbientHznLightGradient { get { return LBGradient.SetGradientFromPreset(LBGradient.GradientPreset.DefaultAmbientHznLight); } }
        public Gradient GetDefaultAmbientGndLightGradient { get { return LBGradient.SetGradientFromPreset(LBGradient.GradientPreset.DefaultAmbientGndLight); } }
        public Gradient GetDefaultFogColourGradient { get { return LBGradient.SetGradientFromPreset(LBGradient.GradientPreset.DefaultFogColour); } }
        public AnimationCurve GetDefaultFogDensityCurve { get { return LBCurve.DefaultFogDensityCurve(); } }
        public float GetDefaultMinFogDensity { get { return 0.0005f; } }
        public float GetDefaultMaxFogDensity { get { return 0.005f; } }
        public AnimationCurve GetDefaultMoonIntensityCurve { get { return LBCurve.DefaultMoonIntensityCurve(); } }
        public AnimationCurve GetDefaultStarVisibilityCurve { get { return LBCurve.DefaultStarVisibilityCurve(); } }

        // Celestials variables
        public bool useCelestials = false;
        public LBCelestials celestials;
        public Camera mainCamera;
        public int numberOfStars = 1000;
        public float starSize = 2f;

        public bool useMoon = true;
        public Light moon;
        public float moonIntensity = 0.3f;
        public float moonYAxisRotation = 0f;
        public float moonSize = 1f;

        // When the moon is on, prevent night sky being blue
        [System.NonSerialized] public Light lbFakeEnvironmentLight = null;

        // Celestials Simple defaults
        public int GetDefaultNumberOfStars { get { return 1000; } }
        public float GetDefaultStarSize { get { return 2f; } }
        public bool GetDefaultUseMoon { get { return true; } }
        public float GetDefaultMoonIntensity { get { return 0.3f; } }
        public float GetDefaultMoonYAxisRotation { get { return 0f; } }
        public float GetDefaultMoonSize { get { return 1f; } }

        // Weather variables
        public bool useWeather = false;
        public bool isHDREnabled = false;
        public List<Camera> weatherCameraList = null;
        public bool useUnityDistanceFog = false;
        public Transform rainParticleSystemPrefab;
        public Transform hailParticleSystemPrefab;
        public Transform snowParticleSystemPrefab;
        public bool useClouds = true;
        public bool use3DNoise = false;  // Added 1.4.0 Beta 4e
        public LBImageFX.CloudStyle cloudStyle = LBImageFX.CloudStyle.CloudStyle1;
        public LBImageFX.CloudQualityLevel cloudsQualityLevel = LBImageFX.CloudQualityLevel.Low;  // Added 1.4.0 Beta 4e
        [Range(0f, 1f)] public float cloudsDetailAmount = 0.5f;  // Added 1.4.0 Beta 4e
        public bool useCloudShadows = true;
        public Color cloudsUpperColourDay = Color.white;
        public Color cloudsLowerColourDay = Color.grey;
        public Color cloudsUpperColourNight = new Color(30f / 255f, 30f / 255f, 30f / 255f, 1f);
        public Color cloudsLowerColourNight = new Color(10f / 255f, 10f / 255f, 10f / 255f, 1f);
        #if UNITY_2018_1_OR_NEWER
        [ColorUsageAttribute(true, true)] public Color cloudsUpperColourDayHDR = Color.white;
        [ColorUsageAttribute(true, true)] public Color cloudsLowerColourDayHDR = Color.grey;
        [ColorUsageAttribute(true, true)] public Color cloudsUpperColourNightHDR = new Color(30f / 255f, 30f / 255f, 30f / 255f, 1f);
        [ColorUsageAttribute(true, true)] public Color cloudsLowerColourNightHDR = new Color(10f / 255f, 10f / 255f, 10f / 255f, 1f);
        #else
        [ColorUsageAttribute(true, true, 0f, 8f, 0.125f, 3f)] public Color cloudsUpperColourDayHDR = Color.white;
        [ColorUsageAttribute(true, true, 0f, 8f, 0.125f, 3f)] public Color cloudsLowerColourDayHDR = Color.grey;
        [ColorUsageAttribute(true, true, 0f, 8f, 0.125f, 3f)] public Color cloudsUpperColourNightHDR = new Color(30f / 255f, 30f / 255f, 30f / 255f, 1f);
        [ColorUsageAttribute(true, true, 0f, 8f, 0.125f, 3f)] public Color cloudsLowerColourNightHDR = new Color(10f / 255f, 10f / 255f, 10f / 255f, 1f);
        #endif
        public List<LBWeatherState> weatherStatesList = null;
        public int startWeatherState = 0;
        public float minWeatherTransitionDuration = 30f;
        public float maxWeatherTransitionDuration = 60f;
        public bool allowAutomaticTransitions = true;
        public bool randomiseWindDirection = true;
        public bool useWindZone = false;
        public WindZone weatherWindZone;

        // Weather Cloud Settings Defaults
        public bool GetDefaultUse3DNoise = false;
        public LBImageFX.CloudStyle GetDefaultCloudStyle = LBImageFX.CloudStyle.CloudStyle1;
        public LBImageFX.CloudQualityLevel GetDefaultCloudsQualityLevel = LBImageFX.CloudQualityLevel.Low;
        public float GetDefaultCloudsDetailAmount = 0.5f;
        public bool GetDefaultCloudShadows = true;
        public Color GetDefaultCloudsUpperColourDay = Color.white;
        public Color GetDefaultCloudsLowerColourDay = Color.grey;
        public Color GetDefaultCloudsUpperColourNight = new Color(30f / 255f, 30f / 255f, 30f / 255f, 1f);
        public Color GetDefaultCloudsLowerColourNight = new Color(10f / 255f, 10f / 255f, 10f / 255f, 1f);
        public int GetDefaultStartWeatherState = 0;
        public float GetDefaultMinWeatherTransitionDuration = 30f;
        public float GetDefaultMaxWeatherTransitionDuration = 60f;
        public bool GetDefaultAllowAutomaticTransitions = true;
        public bool GetDefaultRandomiseWindDirection = true;
        public bool GetDefaultUseWindZone = false;

        // DON'T add these variables to LBTemplate
        private int currentWeatherState = 0;
        private int nextWeatherState = 0;
        private float currentWeatherStateDuration = 30f;
        private float currentWeatherStateTimer = 0f;
        private float currentWeatherTransitionDuration = 30f;
        private float currentWeatherTransitionTimer = 0f;
        public Vector2 currentWindDirection = Vector2.one;
        private Vector2 oldWindDirection = Vector2.one;
        private Vector2 nextWindDirection = Vector2.one;
        private bool transitioningBetweenWeatherStates = false;
        private LBWeatherState oldWeatherState = new LBWeatherState();
        private float currentGlobalWetnessValue = 0f;
        private float currentFogDensityMultiplier = 1f;
        private string rainParticleTag = "";
        private string hailParticleTag = "";
        private string snowParticleTag = "";
        private float rainMaxWindEffect = 10f;
        private float hailMaxWindEffect = 5f;
        private float snowMaxWindEffect = 20f;
        private GameObject weatherParticleGameObject = null;
        private int numWeatherParticleSystems = 0;
        private List<ParticleSystem> particleSystemList;
        private ParticleSystem weatherParticle;
        private int wParticleIdx = 0;
        private int numWeatherCameraList = 0;
        private int wCamIdx = 0;

        // WeatherState public properties
        public string CurrentWeatherStateName
        {
            get
            {
                if (weatherStatesList == null) { return string.Empty; }
                else if (weatherStatesList.Count > currentWeatherState) { return weatherStatesList[currentWeatherState].name; }
                else { return string.Empty; }
            }
        }

        public string NextWeatherStateName
        {
            get
            {
                if (weatherStatesList == null) { return string.Empty; }
                else if (weatherStatesList.Count > nextWeatherState) { return weatherStatesList[nextWeatherState].name; }
                else { return string.Empty; }
            }
        }

        // Screen clock variables
        public bool useScreenClock = false;
        public bool useScreenClockSeconds = false;
        public Canvas lightingCanvas;
        public RectTransform screenClockPanel;
        public Text screenClockText;
        public Color screenClockTextColour = new Color(240f / 255f, 240f / 255f, 240f / 255f, 33f / 255f);
        private float screenClockUpdateTimer = 0f;
        private float screenClockUpdateInterval = 60f;      // # seconds between updates
        private float simTimeRealTimeRatio = 1f;            // How many seconds pass in simulated time to every real time second?

        // Screen clock defaults
        public Color GetDefaultScreenClockTextColour { get { return new Color(240f / 255f, 240f / 255f, 240f / 255f, 33f / 255f); } }

        // Screen Fader variables
        public bool fadeInOnWake = false;
        public bool fadeOutOnWake = false;
        public float fadeInDuration = 5f;
        public float fadeOutDuration = 5f;
        private bool isFadingIn = false;
        private bool isFadingOut = false;
        private float fadeStartAlpha = 255f;
        private float fadeTargetValue = 0f;
        private Canvas fadeCanvas = null;
        private Image fadeImage = null;

        // Screen Fader Public Properties
        public bool IsScreenFadingIn { get { return isFadingIn; } private set { isFadingIn = value; } }
        public bool IsScreenFadingOut { get { return isFadingOut; } private set { isFadingOut = value; } }

        // Fade screen defaults
        public bool GetDefaultFadeInOnWake { get { return false; } }
        public bool GetDefaultFadeOutOnWake { get { return false; } }
        public float GetDefaultFadeInDuration { get { return 5f; } }
        public float GetDefaultFadeOutDuration { get { return 5f; } }

        // Show/Hide variables
        public bool showSkyboxSettings = false;
        public bool showGeneralLightingSettings = true;
        public bool showAmbientAndFogSettings = true;
        public bool showCelestialsSettings = false;
        public bool showWeatherSettings = false;
        public bool showWeatherCameraSettings = false;
        public bool showWeatherCloudSettings = false;
        public bool showWeatherStateSettings = false;
        //public bool showWeatherStates = false;
        public bool showClockSettings = false;
        public bool showScreenFadeSettings = false;

        // Complex Lighting temp variables
        private Terrain[] allTerrains;
        private int numTerrains = 0;
        private ReflectionProbe[] updatingReflectionProbes;
        private Color32 ambientColor32;
        private int numTreeInstances = 0;

        // Time variables
        private float timeNowFloat;

        private float starVisibility;

        #endregion

        #region Initialisation

        // Use this for initialization
        void Awake()
        {
            Initialise();

            // Should we fade in or out the scene when it first starts?
            if (fadeInOnWake) { StartScreenFade(true); }
            else if (fadeOutOnWake) { StartScreenFade(false); }

            // Keep compiler happy
            if (isFadingIn) { }
            if (isFadingOut) { }
        }

        public void Initialise()
        {
            if (!useWeather || useUnityDistanceFog) { RenderSettings.fog = true; }
            else { RenderSettings.fog = false; }
            RenderSettings.fogMode = FogMode.ExponentialSquared;

            // In the Unity Lighting editor, AmbientMode.Flat = Color and Trilight = Gradient
            RenderSettings.ambientMode = envAmbientSource == EnvAmbientSource.Gradient ? UnityEngine.Rendering.AmbientMode.Trilight : UnityEngine.Rendering.AmbientMode.Flat;

            if (weatherCameraList == null) { weatherCameraList = new List<Camera>(); }

            FindAllTerrains();
            FindReflectionProbes();

            ResetToStartOfDay();

            // How many seconds pass in simulated time to every real time second?
            simTimeRealTimeRatio = 86400 / dayLength;

            DisplayScreenClock(useScreenClock);
            if (useScreenClock)
            {
                UpdateScreenClockColour();
                screenClockUpdateTimer = 0f;
                // Determine how often to update the clock
                screenClockUpdateInterval = useScreenClockSeconds ? 1f : 60f;
                screenClockText.text = CurrentTimeString(useScreenClockSeconds);
            }

            InitialiseSkyboxes(timeNowFloat, false);

            // Get the gameobject Unity Tag for each of the weather particle systems
            rainParticleTag = GetPrecipitationParticleTag(PrecipitationType.Rain);
            hailParticleTag = GetPrecipitationParticleTag(PrecipitationType.Hail);
            snowParticleTag = GetPrecipitationParticleTag(PrecipitationType.Snow);

            // Create list to be used in UpdateWeather() to reduce Garbage Collection
            particleSystemList = new List<ParticleSystem>(3);

            // When first loaded, make sure there is at least one weather state
            ValidateWeatherStates();

            // When loading templates with different WeatherStates, the weather state may be invalid
            // when Initialise is called. If so, reset it to 0 to avoid errors.
            if (currentWeatherState > weatherStatesList.Count - 1)
            {
                Debug.Log("LBLighting.Initialise current weather state is invalid, resetting it.");
                currentWeatherState = 0;
            }

            // Determine initial weather state duration time
            currentWeatherStateDuration = UnityEngine.Random.Range(weatherStatesList[currentWeatherState].minStateDuration, weatherStatesList[currentWeatherState].maxStateDuration);

            InitialiseFakeLight();

            // Create the moon light if required
            if (useMoon && moon == null) { AddMoonLight(); }

            // Set gradient defaults if not already defined
            InitialiseGradients();

            UpdateSunAndMoonRotation(timeNowFloat);
            UpdateLighting(timeNowFloat);
            if (!realtimeTerrainLighting)
            {
                realtimeTerrainLighting = true;
                UpdateComplexLighting(timeNowFloat);
                realtimeTerrainLighting = false;
            }
            else
            {
                UpdateComplexLighting(timeNowFloat);
            }
            UpdateFog(timeNowFloat);
            UpdateStartingWeatherState();
            UpdateWeatherUseCloudsSetting(false);
            UpdateWeather(timeNowFloat);

            lightingUpdateTimer = lightingUpdateInterval;

            // Add the celestrials script and camera if it is required but hasn't been
            // added via LBLighting Editor
            if (useCelestials && celestials == null) { AddCelestials(); }
            else if (!useCelestials) { RemoveCelestials(); }

            if (useCelestials && celestials != null && mainCamera != null)
            {
                // Verify that everything looks ok first. Fix anything that's not setup correctly
                celestials.CheckCelestials(this);

                CelestialsSetupCameras();
                if (useMoon && moon != null)
                {
                    celestials.UpdateCelestials(timeNowFloat, true, moon.transform.rotation);
                }
                else
                {
                    celestials.UpdateCelestials(timeNowFloat, false, Quaternion.identity);
                }
            }

            if (lightingMode == LightingMode.Static) { staticTimeFloat = timeNowFloat; if (staticTimeFloat == 0f) { } }
        }

        /// <summary>
        /// Set gradient defaults if not already defined.
        /// Unity 2018.2+ does not allow Gradient.colorKeys to be set when the monobehaviour class constructor is called.
        /// Therefore they cannot be set when the public variables are declared.
        /// </summary>
        public void InitialiseGradients()
        {
            if (ambientLightGradient == null) { ambientLightGradient = GetDefaultAmbientLightGradient; }
            if (ambientHznLightGradient == null) { ambientHznLightGradient = GetDefaultAmbientHznLightGradient; }
            if (ambientGndLightGradient == null) { ambientGndLightGradient = GetDefaultAmbientGndLightGradient; }
            if (fogColourGradient == null) { fogColourGradient = GetDefaultFogColourGradient; }
        }

        /// <summary>
        /// Find the LBFakeEnvironmentLight in the scene for use in night sky with moon.
        /// Set the initial state
        /// </summary>
        private void InitialiseFakeLight()
        {
            if (sun != null)
            {
                Transform fakeEnvironmentLightTransform = sun.transform.Find("LBFakeEnvironmentLight");
                if (fakeEnvironmentLightTransform != null)
                {
                    lbFakeEnvironmentLight = fakeEnvironmentLightTransform.GetComponent<Light>();
                    //lbFakeEnvironmentLight.enabled = (useMoon && moon != null);
                }
            }
        }

        /// <summary>
        /// Reset the time to the startTimeOfDay
        /// </summary>
        public void ResetToStartOfDay()
        {
            dayTimer = dayLength * (startTimeOfDay / 24f);

            timeNowFloat = CurrentTime() / dayLength;
        }

        #endregion

        #region Update

        // Update is called once per frame
        void Update()
        {
            if (lightingMode == LightingMode.Dynamic)
            {
                dayTimer += Time.deltaTime;

                // Get a float value between 0 and 1 indicating time (0 is very start of day, 1 is end, 0.5 is midday)
                timeNowFloat = CurrentTime() / dayLength;

                // Rotate the sun and moon around their x axes to create a basic day/night cycle
                UpdateSunAndMoonRotation(timeNowFloat);

                // Update blended skybox if that is what we are using
                if (skyboxType == SkyboxType.SixSided) { UpdateBlendedSkybox(timeNowFloat * 24f, false); }

                // Update simple lighting
                UpdateLighting(timeNowFloat);

                // Update complex lighting at every lighting update interval
                // Increment the timer
                lightingUpdateTimer += Time.deltaTime;
                if (lightingUpdateTimer > lightingUpdateInterval)
                {
                    UpdateComplexLighting(timeNowFloat);
                    // Reset the timer
                    lightingUpdateTimer = 0f;
                }

                // Update fog colour and density
                UpdateFog(timeNowFloat);

                // Update weather
                UpdateWeather(timeNowFloat);

                // Update position and visibility of celestial bodies
                UpdateCelestials(timeNowFloat, true);
            }
            else //if (useCelestials && celestials != null)
            {
                UpdateCelestials(timeNowFloat, false);

                // Update position of celestial bodies, accounting for camera rotation
                //celestials.UpdateCelestialsRotation();
            }

            // If the screen clock is on, update the time
            if (useScreenClock)
            {
                if (screenClockText != null)
                {
                    // Clock displays hours and minutes. So need no to update every frame
                    screenClockUpdateTimer += (Time.deltaTime * simTimeRealTimeRatio);
                    //Debug.Log(screenClockUpdateTimer.ToString() + "  " + screenClockUpdateInterval.ToString());
                    if (screenClockUpdateTimer > screenClockUpdateInterval)
                    {
                        screenClockUpdateTimer = 0f;
                        screenClockText.text = CurrentTimeString(useScreenClockSeconds);
                    }
                }
            }
        }

        #endregion

        #region Public Static Methods

        public static LBLighting AddLightingToScene(bool isRemoveDirectionalLights)
        {
            LBLighting lbLighting;

            if (isRemoveDirectionalLights) { RemoveDirectionalLights(); }

            GameObject lightingObject = new GameObject("LB Lighting");
            lightingObject.transform.position = -Vector3.up;
            lightingObject.transform.rotation = Quaternion.identity;
            GameObject sunLightObject = new GameObject("Sun Light");
            sunLightObject.transform.parent = lightingObject.transform;
            sunLightObject.transform.localPosition = Vector3.zero;
            sunLightObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            Light sun = sunLightObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 244f / 255f, 214f / 255, 1f);
            sun.shadows = LightShadows.Soft;
            lbLighting = lightingObject.AddComponent<LBLighting>();
            lbLighting.sun = sun;
            ReflectionProbe lightingProbe = lightingObject.AddComponent<ReflectionProbe>();
            lightingProbe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
            lightingProbe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.ViaScripting;
            lightingProbe.size = Vector3.one;
            lightingProbe.nearClipPlane = 0.3f;
            lightingProbe.farClipPlane = 1f;
            lightingProbe.cullingMask = ~lightingProbe.cullingMask;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(158f / 255f, 189f / 255f, 195f / 255f, 1f);
            RenderSettings.fogDensity = 0.0002f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(71f / 255f, 75f / 255f, 81f / 255f, 1f);

            lbLighting.ValidateWeatherStates();

#if UNITY_EDITOR
            // This is Editor only
            Material lbSkybox = (Material)AssetDatabase.LoadAssetAtPath("Assets/LandscapeBuilder/Materials/LBSkybox.mat", typeof(Material));
            if (lbSkybox != null)
            {
                RenderSettings.skybox = lbSkybox;
            }
            else
            {
                // Check to see if it is in a resources folder
                lbSkybox = (Material)Resources.Load("LBSkybox", typeof(Material));
                if (lbSkybox != null) { RenderSettings.skybox = lbSkybox; }
                else { Debug.LogWarning("LBSkybox material not found at path: Assets/LandscapeBuilder/Materials/LBSkybox or in a Resources folder. Did you accidentally delete it?"); }
            }
#else
            // Attempt to load from a Resources folder - developer would have to copy it there manually
            Material lbSkybox = (Material)Resources.Load("LBSkybox", typeof(Material));
            if (lbSkybox != null) { RenderSettings.skybox = lbSkybox; }
            else { Debug.LogWarning("LBSkybox material not found in a Resources folder"); }
#endif

            return lbLighting;
        }

        /// <summary>
        /// Returns a list of lights in the scene. If includeAllLights is true, lightType is ignored.
        /// </summary>
        /// <param name="includeAllLights"></param>
        /// <param name="lightType"></param>
        /// <returns></returns>
        public static List<Light> GetLightList(bool includeAllLights, LightType lightType = LightType.Directional)
        {
            List<Light> lightList = new List<Light>();
            Light[] lights = GameObject.FindObjectsOfType<Light>();

            if (lights != null)
            {
                if (includeAllLights) { lightList.AddRange(lights); }
                else
                {
                    // Loop through all the active lights found in the scene
                    for (int l = 0; l < lights.Length; l++)
                    {
                        // Check to see if the light matches the criteria
                        if (lights[l].type == lightType)
                        {
                            lightList.Add(lights[l]);
                        }
                    }
                }
            }

            return lightList;
        }

        /// <summary>
        /// Remove all active directional lights in the scene
        /// </summary>
        public static void RemoveDirectionalLights()
        {
            Light[] allSceneLights = GameObject.FindObjectsOfType<Light>();
            for (int i = 0; i < allSceneLights.Length; i++)
            {
                if (allSceneLights[i].type == LightType.Directional)
                {
                    DestroyImmediate(allSceneLights[i].gameObject);
                }
            }
        }

        /// <summary>
        /// Is this device running some version of DirectX?
        /// </summary>
        /// <returns></returns>
        public static bool IsDirectX()
        {
            return (SystemInfo.graphicsDeviceVersion.StartsWith("Direct3D"));
        }

        /// <summary>
        /// Is this device running some version of OpenGL?
        /// </summary>
        /// <returns></returns>
        public static bool IsOpenGL()
        {
            return (SystemInfo.graphicsDeviceVersion.StartsWith("OpenGL"));
        }

        /// <summary>
        /// HDR colours can have values greater than 1. This method clamps the values to max 1.0f.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static Color ConvertColourFromHDR(Color color)
        {
            return (new Color(color.r > 1f ? 1f : color.r, color.g > 1f ? 1f : color.g, color.b > 1f ? 1f : color.b, color.a > 1f ? 1f : color.a));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Finds all the terrains currently in the scene
        /// </summary>
        public void FindAllTerrains()
        {
            allTerrains = GameObject.FindObjectsOfType<Terrain>();
        }

        /// <summary>
        /// Sets the allTerrains array to null. This is useful when deleting a
        /// landscape from the scene at runtime. It can prevent UpdateComplexLighting()
        /// from trying to access terrain trees that have been removed from scene.
        /// </summary>
        public void ClearAllTerrains()
        {
            allTerrains = null;
        }

        /// <summary>
        /// Finds all the specified reflection probes currently in the scene
        /// </summary>
        public void FindReflectionProbes()
        {
            if (reflProbesUpdateMode == ReflectionProbeUpdateMode.UpdateAllProbes)
            {
                updatingReflectionProbes = GameObject.FindObjectsOfType<ReflectionProbe>();
            }
            else if (reflProbesUpdateMode == ReflectionProbeUpdateMode.UpdateThisProbe)
            {
                updatingReflectionProbes = new ReflectionProbe[1];
                updatingReflectionProbes[0] = GetComponent<ReflectionProbe>();
            }
        }

        /// <summary>
        /// Fade the screen in or out. The lighting will continue to be updated
        /// as normal.
        /// </summary>
        /// <param name="isFadeIn"></param>
        public void StartScreenFade(bool isFadeIn)
        {
            // Get or create the Canvas attached to the LBLighting gameobject
            fadeCanvas = GetComponent<Canvas>();
            if (fadeCanvas == null) { fadeCanvas = gameObject.AddComponent<Canvas>(); }
            if (fadeCanvas == null) { Debug.LogError("LBLighting.StartScreenFade - could not add canvas"); }
            else
            {
                fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                // Aligns canvas with screen pixels for potentially sharper UI (may impact smoothness)
                fadeCanvas.pixelPerfect = true;
                // Set to highest value within this renderer sorting layer as we want on top
                fadeCanvas.sortingOrder = 32767;
                fadeImage = gameObject.GetComponent<Image>();
                if (fadeImage == null) { fadeImage = gameObject.AddComponent<Image>(); }
                if (fadeImage == null) { Debug.LogError("LBLighting.StartScreenFade - could not add image to canvas"); }
                else
                {
                    // immediately turn off the image in case it was just added
                    fadeImage.enabled = false;

                    // Set up the transistion values
                    if (isFadeIn)
                    { fadeStartAlpha = 1f; fadeTargetValue = 0f; }
                    else { fadeStartAlpha = 0f; fadeTargetValue = 1f; }
                    // Initialise the fade image colour
                    //fadeImage.color = Color.black;
                    fadeImage.color = new Color(Color.black.r, Color.black.g, Color.black.b, fadeStartAlpha);

                    // Could use fadeImage.CrossFadeAlpha(fadeTargetValue, fadeInDuration, true)
                    // but have no notification when done - would need to check in Update()

                    if (isFadeIn) { StartCoroutine(FadeIn()); }
                    else { StartCoroutine(FadeOut()); }
                }
            }
        }

        /// <summary>
        /// Stop the screen from fading in or out
        /// </summary>
        public void StopScreenFade()
        {
            isFadingIn = false;
            isFadingOut = false;
            // Turn off the image
            if (fadeImage != null)
            {
                fadeImage.enabled = false;
            }
        }

        public void UpdateFog(float timeFloat)
        {
            // Update fog
            if (!useWeather) { currentFogDensityMultiplier = 1f; }
            if (setupMode == SetupMode.Simple)
            {
                RenderSettings.fogColor = LerpFogColor(timeFloat, dayFogColour, nightFogColour);
                RenderSettings.fogDensity = LerpFloat(timeFloat, dayFogDensity, nightFogDensity) * currentFogDensityMultiplier;
            }
            else
            {
                RenderSettings.fogColor = fogColourGradient.Evaluate(timeFloat);
                RenderSettings.fogDensity = ((fogDensityCurve.Evaluate(timeFloat) * (maxFogDensity - minFogDensity)) + minFogDensity) * currentFogDensityMultiplier;
            }
        }

        #endregion

        #region Time Methods

        /// <summary>
        /// Returns the time in seconds from the start of the current day
        /// </summary>
        /// <returns>The time.</returns>
        public float CurrentTime()
        {
            return dayTimer % dayLength;
        }

        /// <summary>
        /// Returns a formatted time string in HH:MM or HH:MM:SS
        /// </summary>
        /// <param name="includeSeconds"></param>
        /// <returns></returns>
        public string CurrentTimeString(bool includeSeconds = false)
        {
            float timeOfDay = (CurrentTime() / dayLength) * 24f;
            float minutes = (timeOfDay % 1f) * 60f;
            if (includeSeconds)
            {
                int minsInt = (int)System.Math.Floor(minutes);
                return LB_Extension.StringFast.FormatTime((int)System.Math.Floor(timeOfDay), minsInt, (int)((minutes - minsInt) * 60f));
            }
            else
            {
                return LB_Extension.StringFast.FormatTime((int)System.Math.Floor(timeOfDay), (int)System.Math.Floor(minutes));
            }
        }

        /// <summary>
        /// Returns a formatted 24-hour time string as HH:MM or optionally HH:MM:SS
        /// </summary>
        /// <param name="timeFloat"></param>
        /// <param name="includeSeconds"></param>
        /// <returns></returns>
        public string CurrentTimeString(float timeFloat, bool includeSeconds = false)
        {
            if (timeFloat == 0f || timeFloat == 24f) { return includeSeconds ? "00:00:00" : "00:00"; }
            else
            {
                float minutes = (timeFloat % 1f) * 60f;

                if (includeSeconds)
                {
                    int minsInt = (int)System.Math.Floor(minutes);
                    return LB_Extension.StringFast.FormatTime((int)System.Math.Floor(timeFloat), minsInt, (int)((minutes - minsInt) * 60f));
                }
                else
                {
                    return LB_Extension.StringFast.FormatTime((int)System.Math.Floor(timeFloat), (int)System.Math.Floor(minutes));
                }
            }
        }

        #endregion

        #region Skybox Methods

        /// <summary>
        /// Initialise the skyboxes
        /// Originally taken from Awake()
        /// showErrorDialogBox will display a Popup DialogBox if in the editor and this
        /// is a critical issue. It gets passed to UpdateBlendedSkybox(). Set this to false
        /// if this will be called multiple times in succession (like at runtime)
        /// </summary>
        /// <param name="timefloat"></param>
        /// <param name="showErrorDialogBox"></param>
        public void InitialiseSkyboxes(float timefloat, bool showErrorDialogBox)
        {
            // Pre-load the default unity skybox
            #if UNITY_EDITOR
            defaultSkybox = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
            #else
            defaultSkybox = null;
            #endif

            // Update the skyboxes
            if (skyboxType == SkyboxType.Procedural) { RenderSettings.skybox = proceduralSkybox; }
            else if (skyboxesList == null)
            {
                skyboxesList = new List<LBSkybox>();
                RenderSettings.skybox = defaultSkybox;
            }
            else if (skyboxesList.Count == 0)
            {
                // Set to Unity default
                RenderSettings.skybox = defaultSkybox;
            }
            else if (skyboxesList.Count == 1)
            {
                // Only one skybox so can't do any blending
                // Set the skybox material. If it doesn't have a material, set to Unity default
                if (skyboxesList[0].material != null)
                {
                    RenderSettings.skybox = skyboxesList[0].material;
                }
                else
                {
                    RenderSettings.skybox = defaultSkybox;
                    Debug.Log("LBLighting - did you forget to set the skybox material in Skybox Settings?");

                    if (showErrorDialogBox)
                    {
                        #if UNITY_EDITOR
                        EditorUtility.DisplayDialog("LBLighting - Missing Skybox Material", "Please add the six-sided skybox material(s) to the Skybox Settings", "OK");
                        #endif
                    }
                }
            }
            else
            {
                // Only use blended skybox if there are 2 or more skyboxes

                // Validate the skybox materials
                bool SkyboxesValidated = true;
                string validationMessage = string.Empty;

                foreach (LBSkybox lbskybox in skyboxesList)
                {
                    // Check for missing and invalid materials
                    SkyboxesValidated = lbskybox.ValidateSixSidedSkybox(out validationMessage);

                    if (!SkyboxesValidated)
                    {
                        if (validationMessage.Contains("null"))
                        {
                            Debug.Log("LBLighting - did you forget to set the skybox materials in Skybox Settings?");
                        }
                        else
                        {
                            Debug.Log("LBLighting - " + validationMessage);
                        }
                        break;
                    }
                }

                if (SkyboxesValidated)
                {
                    // Reset current skybox variables
                    currentSkybox1 = -1;
                    currentSkybox2 = -1;

                    UpdateBlendedSkybox(timefloat * 24f, showErrorDialogBox);
                    RenderSettings.skybox = blendedSkybox;
                }
            }
        }

        /// <summary>
        /// Update the skybox.
        /// showErrorDialogBox will display a Popup DialogBox if in the editor and this
        /// is a critical issue. Set this to false if this will be called multiple times
        /// in succession (like at runtime)
        /// </summary>
        /// <param name="timeFloat24"></param>
        /// <param name="showErrorDialogBox"></param>
        private void UpdateBlendedSkybox(float timeFloat24, bool showErrorDialogBox)
        {
            if (skyboxesList != null)
            {
                if (skyboxesList.Count > 1)
                {
                    // Find the skyboxes to transition from and to
                    int s1 = -1, s2 = -1;

                    // Find the next sky that will be transitioned to
                    for (int sb = 0; sb < skyboxesList.Count; sb++)
                    {
                        if (skyboxesList[sb].transitionStartTime > timeFloat24) { s2 = sb; break; }
                    }

                    // If we didn't find one, that means the next skybox will be tomorrow (i.e. on or before the current startTimeOfDay)
                    if (s2 == -1)
                    {
                        if (skyboxesList[skyboxesList.Count - 2].transitionEndTime > timeFloat24)
                        {
                            s1 = skyboxesList.Count - 2; s2 = skyboxesList.Count - 1;
                        }
                        else { s1 = skyboxesList.Count - 1; s2 = 0; }
                    }
                    else if (s2 == 0) { s1 = skyboxesList.Count - 1; s2 = 0; }
                    else { s1 = s2 - 1; }

                    // Check if the skybox material needs to be modified
                    if (currentSkybox1 != s1 || currentSkybox2 != s2)
                    {
                        // Validate the skyboxes before attempting to blend them.
                        // To optimise may wish to perform this only at Activation
                        if (skyboxesList[s1].ValidateSixSidedSkybox())
                        {
                            if (skyboxesList[s2].ValidateSixSidedSkybox())
                            {
                                // Create the blended skybox material
                                blendedSkybox = LBSkybox.BlendedSkybox(blendedSkybox, skyboxesList[s1].material, skyboxesList[s2].material);
                            }
                        }
                    }
                    currentSkybox1 = s1;
                    currentSkybox2 = s2;

                    if (blendedSkybox != null)
                    {
                        // Set the blend weight
                        if (s2 != 0)
                        {
                            blendedSkybox.SetFloat("_Blend", Mathf.InverseLerp(skyboxesList[s1].transitionStartTime, skyboxesList[s1].transitionEndTime, timeFloat24));
                        }
                        else if (timeFloat24 < skyboxesList[s1].transitionEndTime && timeFloat24 > skyboxesList[s1].transitionStartTime)
                        {
                            blendedSkybox.SetFloat("_Blend", Mathf.InverseLerp(skyboxesList[s1].transitionStartTime, skyboxesList[s1].transitionEndTime, timeFloat24));
                        }
                        else if ((skyboxesList[s1].transitionEndTime <= skyboxesList[s2].transitionStartTime) && timeFloat24 < skyboxesList[s2].transitionEndTime)
                        {
                            blendedSkybox.SetFloat("_Blend", Mathf.InverseLerp(skyboxesList[s1].transitionStartTime, skyboxesList[s1].transitionEndTime, timeFloat24));
                        }
                        else { blendedSkybox.SetFloat("_Blend", 0f); }
                    }
                    else if (showErrorDialogBox)
                    {
#if UNITY_EDITOR
                        EditorUtility.DisplayDialog("LBLighting - Missing Skybox Material", "Please add the six-sided skybox material(s) to the Skybox Settings", "OK");
#endif
                    }
                }
            }
        }

        #endregion

        #region Lighting Preview Methods

        public void UpdateLightingPreview()
        {
            // In the Unity Lighting editor, AmbientMode.Flat = Color and Trilight = Gradient
            RenderSettings.ambientMode = envAmbientSource == EnvAmbientSource.Gradient ? UnityEngine.Rendering.AmbientMode.Trilight : UnityEngine.Rendering.AmbientMode.Flat;

            if (useWeather) { UpdateStartingWeatherState(); }

            float timeFloat = startTimeOfDay / 24f;

            InitialiseSkyboxes(timeFloat, true);

            // Get the gameobject Unity Tag for each of the weather particle systems
            rainParticleTag = GetPrecipitationParticleTag(PrecipitationType.Rain);
            hailParticleTag = GetPrecipitationParticleTag(PrecipitationType.Hail);
            snowParticleTag = GetPrecipitationParticleTag(PrecipitationType.Snow);

            FindReflectionProbes();

            InitialiseFakeLight();

            // Create the moon light if required
            if (useMoon && moon == null) { AddMoonLight(); }

            UpdateSunAndMoonRotation(timeFloat);
            UpdateLighting(timeFloat);
            UpdateComplexLighting(timeFloat);

            // Update fog, Turn off Unity fog if we are using LBImageFX and haven't chosen to use Unity fog
            if (!useWeather || useUnityDistanceFog) { RenderSettings.fog = true; }
            else { RenderSettings.fog = false; }
            UpdateFog(timeFloat);

            // Update weather
            // LBLighting imagefx integration - update all the active cameras
            if (useWeather)
            {
                UpdateWeatherUseCloudsSetting(true);
                UpdateWeather(timeFloat);
            }

            // Update celestials
            UpdateCelestials(timeFloat, true);
            CelestialsSetupCameras();

            // Update the screen clock
            if (useScreenClock && screenClockText != null)
            {
                screenClockText.text = CurrentTimeString(startTimeOfDay, useScreenClockSeconds);
                UpdateScreenClockColour();
            }
        }

        #endregion

        #region Public Celestials Methods

        /// <summary>
        /// Add the Celestrials parent gameobject and script as a child of LBLighting
        /// </summary>
        public void AddCelestials()
        {
            // If the gameobject doesn't exist, add it and initialise
            Transform celestialTrm = transform.Find("Celestials");

            if (celestialTrm == null)
            {
                GameObject celestialsObj = new GameObject("Celestials");
                celestialsObj.transform.parent = transform;
                celestials = celestialsObj.AddComponent<LBCelestials>();
                celestials.Initialise(this);
            }
            else
            {
                // Find the LBCelestials component
                celestials = celestialTrm.GetComponent<LBCelestials>();
                if (celestials == null)
                {
                    // Re-add the component if it has been deleted (probably by the user)
                    celestials = celestialTrm.gameObject.AddComponent<LBCelestials>();
                }
                if (celestials != null) { celestials.Initialise(this); }
            }
        }

        /// <summary>
        /// Remove the celestials from under LBLighting gameobject
        /// </summary>
        public void RemoveCelestials()
        {
            Transform celestialTrm = transform.Find("Celestials");

            if (celestialTrm != null)
            {
                DestroyImmediate(celestialTrm.gameObject);
                celestials = null;
                if (mainCamera != null)
                {
                    mainCamera.cullingMask = -1;
                    mainCamera.clearFlags = CameraClearFlags.Skybox;
                }
            }
        }

        /// <summary>
        /// Update position and visibility of celestial bodies, accounting for camera rotation
        /// </summary>
        /// <param name="timeFloat"></param>
        /// <param name="isDynamicLightingMode"></param>
        public void UpdateCelestials(float timeFloat, bool isDynamicLightingMode)
        {
            if (useCelestials && celestials != null)
            {
                if (isDynamicLightingMode)
                {
                    if (setupMode == SetupMode.Simple)
                    {
                        if (useMoon && moon != null)
                        {
                            UpdateMoonRotation(timeFloat);
                            celestials.UpdateCelestials(timeFloat, true, moon.transform.rotation);
                        }
                        else
                        {
                            celestials.UpdateCelestials(timeFloat, false, Quaternion.identity);
                        }
                    }
                    else
                    {
                        starVisibility = starVisibilityCurve.Evaluate(timeFloat);
                        if (useMoon && moon != null)
                        {
                            celestials.UpdateCelestialsAdvanced(starVisibility, true, moon.transform.rotation);
                        }
                        else
                        {
                            celestials.UpdateCelestialsAdvanced(starVisibility, false, Quaternion.identity);
                        }
                    }
                }
                // Static Lighting
                else { celestials.UpdateCelestialsRotation(); }
            }
        }

        /// <summary>
        /// Set up the cameras to use celestials.
        /// NOTE: Currently only works with 1 camera (mainCamera)
        /// </summary>
        public void CelestialsSetupCameras()
        {
            if (useCelestials && celestials != null && mainCamera != null)
            {
                mainCamera.cullingMask = ~(1 << LBCelestials.celestialsUnityLayer);
                mainCamera.clearFlags = CameraClearFlags.Depth;

                //Debug.Log("LBLighting.CelestialsSetupCameras - camera: " + mainCamera.gameObject.name);

                // If the RefreshWeatherCameraList() has been run the ImageFX may be attached even
                // when useWeather is false
                bool isWeatherAttached = (mainCamera.GetComponent<LBImageFX>() != null);

                // Currently Forward doesn't work correctly on Celestials camera with DirectX and ImageFX (the camera is not cleared correctly between frames)
                if (IsDirectX() && isWeatherAttached && celestials.celestialsCamera.renderingPath != RenderingPath.DeferredShading)
                {
                    celestials.celestialsCamera.renderingPath = RenderingPath.DeferredShading;
                    Debug.Log("LBLighting.CelestrialsSetupCameras - Switching the Celestials camera to Deferred Render Path as Celestials (stars) combined with ImageFX on DirectX requires Deferred.");
                }
            }
        }

        #endregion

        #region Sun and Moon Public Methods

        /// <summary>
        /// If the moon (light) is null, create a new one
        /// as a child of the LBLighting gameobject.
        /// </summary>
        public void AddMoonLight()
        {
            if (moon == null)
            {
                GameObject moonObj = new GameObject("Moon Light");
                moonObj.transform.SetParent(transform);
                //moonObj.transform.parent = transform;
                moon = moonObj.AddComponent<Light>();
                moon.intensity = 0f;
                moon.color = Color.white;
                moon.shadows = LightShadows.Soft;
                moon.type = LightType.Directional;
            }
        }

        public void UpdateSunRotation(float timeFloat)
        {
            float sunRotationFloat = 0f;
            timeFloat *= 24f;
            if (timeFloat < sunriseTime || timeFloat > sunsetTime)
            {
                sunRotationFloat = (Mathf.InverseLerp(sunsetTime - 24f, sunriseTime, timeFloat) * 0.5f) - 0.25f;
            }
            else
            {
                sunRotationFloat = (Mathf.InverseLerp(sunriseTime, sunsetTime, timeFloat) * 0.5f) + 0.25f;
            }
            sun.transform.rotation = Quaternion.Euler((sunRotationFloat * 360f) - 90f, yAxisRotation, 0f);
        }

        public void UpdateMoonRotation(float timeFloat)
        {
            moon.transform.rotation = Quaternion.Euler((timeFloat * 360f) + 90f, moonYAxisRotation, 0f);
        }

        public void UpdateSunAndMoonRotation(float timeFloat)
        {
            // Calculate an adjusted rotation float based on sunrise and sunset times
            float sunRotationFloat = 0f;
            timeFloat *= 24f;
            if (timeFloat < sunriseTime)
            {
                sunRotationFloat = (Mathf.InverseLerp(sunsetTime - 24f, sunriseTime, timeFloat) * 0.5f) - 0.25f;
            }
            else if (timeFloat > sunsetTime)
            {
                sunRotationFloat = (Mathf.InverseLerp(sunsetTime, sunriseTime + 24f, timeFloat) * 0.5f) - 0.25f;
            }
            else
            {
                sunRotationFloat = (Mathf.InverseLerp(sunriseTime, sunsetTime, timeFloat) * 0.5f) + 0.25f;
            }

            if (sun != null)
            {
                // Update the sun rotation
                sun.transform.rotation = Quaternion.Euler((sunRotationFloat * 360f) - 90f, yAxisRotation, 0f);
            }
            if (useMoon && moon != null)
            {
                // Update the moon rotation
                moon.transform.rotation = Quaternion.Euler((sunRotationFloat * 360f) + 90f, moonYAxisRotation, 0f);
            }
        }

        /// <summary>
        /// Update the ambient or environment light, and the sun and moon intensity
        /// </summary>
        /// <param name="timeFloat"></param>
        public void UpdateLighting(float timeFloat)
        {
            // Update ambient light
            if (setupMode == SetupMode.Simple)
            {
                if (envAmbientSource == EnvAmbientSource.Colour)
                {
                    RenderSettings.ambientLight = LerpColor(timeFloat, isHDREnabled ? dayAmbientLightHDR : dayAmbientLight, isHDREnabled ? nightAmbientLightHDR : nightAmbientLight);
                }
                else // Environment Light mode Gradient
                {
                    RenderSettings.ambientSkyColor = LerpColor(timeFloat, isHDREnabled ? dayAmbientLightHDR : dayAmbientLight, isHDREnabled ? nightAmbientLightHDR : nightAmbientLight);
                    RenderSettings.ambientEquatorColor = LerpColor(timeFloat, isHDREnabled ? dayAmbientHznLightHDR : dayAmbientHznLight, isHDREnabled ? nightAmbientHznLightHDR : nightAmbientHznLight);
                    RenderSettings.ambientGroundColor = LerpColor(timeFloat, isHDREnabled ? dayAmbientGndLightHDR : dayAmbientGndLight, isHDREnabled ? nightAmbientGndLightHDR : nightAmbientGndLight);
                }
            }
            else
            {
                if (envAmbientSource == EnvAmbientSource.Colour)
                {
                    RenderSettings.ambientLight = ambientLightGradient.Evaluate(timeFloat);
                }
                else // Environment Light mode Gradient
                {
                    RenderSettings.ambientSkyColor = ambientLightGradient.Evaluate(timeFloat);
                    RenderSettings.ambientEquatorColor = ambientHznLightGradient.Evaluate(timeFloat);
                    RenderSettings.ambientGroundColor = ambientGndLightGradient.Evaluate(timeFloat);
                }
            }

            float sunriseFloat = sunriseTime / 24f, sunsetFloat = sunsetTime / 24f;

            float newMoonIntensity = 0f;

            // Update moon intensity
            if (useMoon && moon != null)
            {
                if (setupMode == SetupMode.Simple)
                {
                    if (timeFloat > sunriseFloat - 0.04f && timeFloat < 0.5f) { moon.intensity = Mathf.InverseLerp(0, -0.04f, timeFloat - sunriseFloat) * moonIntensity; }
                    else if (timeFloat < sunsetFloat + 0.04f && timeFloat > 0.5f) { moon.intensity = Mathf.InverseLerp(0, 0.04f, timeFloat - sunsetFloat) * moonIntensity; }
                    else { moon.intensity = moonIntensity; }
                }
                else { moon.intensity = moonIntensityCurve.Evaluate(timeFloat) * moonIntensity; }
                moon.bounceIntensity = moon.intensity;
                newMoonIntensity = moon.intensity;
            }

            // Update sun intensity
            if (sun != null)
            {
                if (setupMode == SetupMode.Simple)
                {
                    if (timeFloat < sunriseFloat + 0.04f)
                    {
                        sun.intensity = Mathf.InverseLerp(0f, 0.04f, timeFloat - sunriseFloat) * sunIntensity;
                        //newSunIntensity = Mathf.InverseLerp(0f, 0.04f, timeFloat - sunriseFloat) * sunIntensity;
                        // Fade fake light out at sunrise
                        if (lbFakeEnvironmentLight != null)
                        {
                            lbFakeEnvironmentLight.intensity = (1f - Mathf.InverseLerp(-0.02f, 0f, timeFloat - sunriseFloat)) * 8f;
                        }
                    }
                    else if (timeFloat > sunsetFloat - 0.04f)
                    {
                        sun.intensity = Mathf.InverseLerp(0, -0.04f, timeFloat - sunsetFloat) * sunIntensity;
                        //newSunIntensity = Mathf.InverseLerp(0, -0.04f, timeFloat - sunsetFloat) * sunIntensity;
                        // Fade fake light in at sunset
                        if (lbFakeEnvironmentLight != null)
                        {
                            lbFakeEnvironmentLight.intensity = Mathf.InverseLerp(0.0f, -0.02f, sunsetFloat - timeFloat) * 8f;
                        }
                    }
                    else
                    {
                        sun.intensity = sunIntensity;
                        //newSunIntensity = sunIntensity;
                        // After sunrise and before dusk, fake light should always be off (zero intensity)
                        if (lbFakeEnvironmentLight != null) { lbFakeEnvironmentLight.intensity = 0f; }
                    }
                }
                else
                {
                    sun.intensity = sunIntensityCurve.Evaluate(timeFloat) * sunIntensity;

                    // TODO - TEST cater for Fake Environment light in SetupMode.Advanced
                    if (lbFakeEnvironmentLight != null)
                    {
                        if (timeFloat < sunriseFloat + 0.04f) { lbFakeEnvironmentLight.intensity = (1f - Mathf.InverseLerp(-0.02f, 0f, timeFloat - sunriseFloat)) * 8f; }
                        else if (timeFloat > sunsetFloat - 0.04f) { lbFakeEnvironmentLight.intensity = Mathf.InverseLerp(0.0f, -0.02f, sunsetFloat - timeFloat) * 8f; }
                        else { lbFakeEnvironmentLight.intensity = 0f; }
                    }
                }

                // If there is no light at sunrise or sunset then fog and clouds with stop rendering for a few frames (seen as a "flash" of the screen)
                // Similarly, if there is no moonlight at night, fog and clouds won't render.
                if (newMoonIntensity == 0f && sun.intensity < 0.001f && ((timeFloat > sunriseFloat - 0.001f && timeFloat < sunsetFloat + 0.001f) || !useMoon)) { sun.intensity = 0.001f; }

                //if (sun.intensity < 0.001f) { sun.intensity = 0.001f; }

                sun.bounceIntensity = sun.intensity;
            }
        }

        /// <summary>
        /// Update tree lighmapcolor (if realtime terrain lighting is enabled).
        /// Update reflection probes (if reflection probe update mode is not set to none).
        /// </summary>
        /// <param name="timeFloat"></param>
        public void UpdateComplexLighting(float timeFloat)
        {
            numTerrains = (allTerrains == null ? 0 : allTerrains.Length);

            // Update terrain lightmap data
            if (realtimeTerrainLighting && numTerrains > 0)
            {
                //Color prevColour = new Color(ambientColor.r, ambientColor.g, ambientColor.b, ambientColor.a);

                // Get the ambient colour for this time of day
                // Convert from Color to Color32 outside the loop to avoid expensive Color32.op_Impicit conversions inside the loop.
                ambientColor32 = LerpColor(timeFloat, Color.white, nightAmbientLight);

                //if (prevColour != ambientColor) { Debug.Log("ambientColor has changed from " + prevColour + " to " + ambientColor); }

                // Iterate through all terrains
                for (int i = 0; i < numTerrains; i++)
                {
                    // Check if terrains have been removed during this update
                    if (allTerrains != null)
                    {
                        // Check for condition when terrainData is missing
                        if (allTerrains[i].terrainData != null)
                        {
                            // Iterate through all trees in this terrain
                            TreeInstance[] trees = allTerrains[i].terrainData.treeInstances;
                            numTreeInstances = (trees == null ? 0 : trees.Length);
                            for (int i2 = 0; i2 < numTreeInstances; i2++)
                            {
                                // Set the lightmap colour (does nothing for SpeedTree trees as they have their own in-built lighting)
                                trees[i2].lightmapColor = ambientColor32;
                            }
                            // Send the new tree instances array back to the terrain
                            if (allTerrains != null && numTreeInstances > 0) { allTerrains[i].terrainData.treeInstances = trees; }
                            else { break; }
                        }
                    }
                    else { break; }
                }
            }
            // Update specified reflection probes
            if (reflProbesUpdateMode != ReflectionProbeUpdateMode.DontUpdate && updatingReflectionProbes != null)
            {
                for (int i = 0; i < updatingReflectionProbes.Length; i++)
                {
                    if (updatingReflectionProbes[i] != null && updatingReflectionProbes[i].mode != UnityEngine.Rendering.ReflectionProbeMode.Baked)
                    {
                        // Update the probe
                        updatingReflectionProbes[i].RenderProbe();
                    }
                }
            }
        }

        #endregion

        #region Public ImageFX Methods

        /// <summary>
        /// Set the Starting WeatherState by name
        /// Will return false if the name is not found in the list of
        /// WeatherStates currently configured.
        /// </summary>
        /// <param name="weatherStateName"></param>
        /// <returns></returns>
        public bool SetStartingWeatherStateByName(string weatherStateName)
        {
            bool isSuccessful = false;

            if (weatherStatesList == null) { Debug.LogWarning("LBLighting.SetStartingWeatherStateByName - weatherStatesList is null - Please report this as a bug"); }
            else if (weatherStatesList.Count < 1) { Debug.Log("LBLighting.SetStartingWeatherStateByName - there are no WeatherStates currently configured."); }
            else
            {
                int _weatherState = weatherStatesList.FindIndex(w => w.name.ToLower() == weatherStateName.ToLower());
                if (_weatherState >= 0) { startWeatherState = _weatherState; isSuccessful = true; }
            }

            return isSuccessful;
        }

        /// <summary>
        /// Set the Current WeatherState by name
        /// Will return false if the name is not found in the list of
        /// WeatherStates currently configured.
        /// </summary>
        /// <param name="weatherStateName"></param>
        /// <returns></returns>
        public bool SetCurrentWeatherStateByName(string weatherStateName)
        {
            bool isSuccessful = false;

            if (weatherStatesList == null) { Debug.LogWarning("LBLighting.SetCurrentWeatherStateByName - weatherStatesList is null - Please report this as a bug"); }
            else if (weatherStatesList.Count < 1) { Debug.Log("LBLighting.SetCurrentWeatherStateByName - there are no WeatherStates currently configured."); }
            else
            {
                int _weatherState = weatherStatesList.FindIndex(w => w.name.ToLower() == weatherStateName.ToLower());
                if (_weatherState >= 0) { currentWeatherState = _weatherState; isSuccessful = true; }
            }

            return isSuccessful;
        }

        /// <summary>
        /// Public method that can be called from other scripts to instantly begin transitioning to a new weather state
        /// If you know the weatherState index in the weatherStatesList, use TransitionToWeatherState(weatherStateNumber, weatherTransitionDuration)
        /// as it will be slightly faster.
        /// </summary>
        /// <param name="weatherStateName"></param>
        /// <param name="weatherTransitionDuration"></param>
        /// <returns></returns>
        public bool TransitionToWeatherStateByName(string weatherStateName, float weatherTransitionDuration)
        {
            bool isSuccessful = false;

            if (weatherStatesList == null) { Debug.LogWarning("LBLighting.TransitionToWeatherStateByName - weatherStatesList is null - Please report this as a bug"); }
            else if (weatherStatesList.Count < 1) { Debug.Log("LBLighting.TransitionToWeatherStateByName - there are no WeatherStates currently configured."); }
            else
            {
                int _weatherState = weatherStatesList.FindIndex(w => w.name.ToLower() == weatherStateName.ToLower());
                if (_weatherState >= 0)
                {
                    currentWeatherState = _weatherState;
                    TransitionToWeatherState(_weatherState, weatherTransitionDuration);
                    isSuccessful = true;
                }
            }

            return isSuccessful;
        }

        /// <summary>
        /// Public method that can be called from other scripts to instantly begin transitioning to a new weather state.
        /// weatherStateNumber is the zero-based position in the list of weather states visible in the LBLighting Editor
        /// </summary>
        /// <param name="weatherStateNumber"></param>
        /// <param name="weatherTransitionDuration"></param>
        public void TransitionToWeatherState(int weatherStateNumber, float weatherTransitionDuration)
        {
            if (transitioningBetweenWeatherStates)
            {
                // Marking current weather state as -1 tells LBLighting to use saved current state values
                currentWeatherState = -1;
                nextWeatherState = weatherStateNumber;

                // Save current state values
                oldWeatherState.wetness = currentGlobalWetnessValue;
                if (weatherCameraList != null && weatherCameraList.Count > 0)
                {
                    LBImageFX imageFXScript;
                    Camera weatherCamera = weatherCameraList[0];
                    if (weatherCamera != null)
                    {
                        imageFXScript = weatherCamera.GetComponent<LBImageFX>();
                        if (imageFXScript != null)
                        {
                            oldWeatherState.cloudDensity = imageFXScript.cloudsDensity;
                            oldWeatherState.cloudCoverage = imageFXScript.cloudsCoverage;
                            oldWeatherState.cloudShadowStrength = imageFXScript.maxShadowStrength;
                            oldWeatherState.cloudsMorphingSpeed = imageFXScript.cloudsMorphingSpeed;

                            // Calculate the current fog density multiplier being used from the inverse of what is used to set it
                            // in the UpdateWeather function
                            if (setupMode == SetupMode.Simple)
                            {
                                oldWeatherState.fogDensityMultiplier = imageFXScript.distanceFogDensity / LerpFloat(CurrentTime() / dayLength, dayFogDensity, nightFogDensity);
                            }
                            else
                            {
                                oldWeatherState.fogDensityMultiplier = imageFXScript.distanceFogDensity / ((fogDensityCurve.Evaluate(CurrentTime() / dayLength) * (maxFogDensity - minFogDensity)) + minFogDensity);
                            }
                        }
                    }
                }
                if (useWindZone && weatherWindZone != null)
                {
                    oldWeatherState.windZoneMain = weatherWindZone.windMain;
                    oldWeatherState.windZoneTurbulence = weatherWindZone.windTurbulence;
                }

                currentWeatherTransitionDuration = weatherTransitionDuration;
                currentWeatherTransitionTimer = 0f;
            }
            else
            {
                transitioningBetweenWeatherStates = true;
                nextWeatherState = weatherStateNumber;
                currentWeatherTransitionDuration = weatherTransitionDuration;
                currentWeatherTransitionTimer = 0f;
            }

            if (randomiseWindDirection)
            {
                oldWindDirection = currentWindDirection;
                // Determine a random wind direction
                nextWindDirection = (new Vector2(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f))).normalized;
            }

            // Clamp the input weather state between its possible values
            if (nextWeatherState < 0) { nextWeatherState = 0; }
            else if (nextWeatherState >= weatherStatesList.Count) { nextWeatherState = weatherStatesList.Count - 1; }
        }

        // Public method that can be used to extract the current global wetness value
        public float GetCurrentGlobalWetnessValue()
        {
            return currentGlobalWetnessValue;
        }

        public void UpdateStartingWeatherState()
        {
            if (startWeatherState < weatherStatesList.Count) { currentWeatherState = startWeatherState; }
            // Randomise starting weather state
            else { currentWeatherState = UnityEngine.Random.Range(0, weatherStatesList.Count); }
        }

        /// <summary>
        /// Weather may be in a particular state, or transitioning between states.
        /// Update all the weather cameras LBImageFX settings with the current weather data.
        /// Update the state of the precipitation rain/hail/snow for each weather camera.
        /// </summary>
        /// <param name="timeFloat"></param>
        public void UpdateWeather(float timeFloat)
        {
            // Set default values
            float currentCloudDensity = 3f;
            float currentCloudCoverage = 0f;
            float currentCloudShadowStrength = 0.5f;
            float currentRainStrength = 0f;
            float currentHailStrength = 0f;
            float currentSnowStrength = 0f;
            currentFogDensityMultiplier = 1f;
            float currentCloudsMorphingSpeed = 0f;
            float currentWindStrength = 0f;
            float currentWindZoneMain = 0f;
            float currentWindZoneTurbulence = 0f;
            currentGlobalWetnessValue = 0f;

            // Check that the current and next weather state integers are within the correct range
            if (currentWeatherState >= 0 && currentWeatherState < weatherStatesList.Count && nextWeatherState >= 0 && nextWeatherState < weatherStatesList.Count)
            {
                if (transitioningBetweenWeatherStates)
                {
                    // Lerp values between weather states
                    float transitionBlendValue = currentWeatherTransitionTimer / currentWeatherTransitionDuration;
                    if (currentWeatherState > -1)
                    {
                        currentCloudDensity = Mathf.Lerp(weatherStatesList[currentWeatherState].cloudDensity, weatherStatesList[nextWeatherState].cloudDensity, transitionBlendValue);
                        currentCloudCoverage = Mathf.Lerp(weatherStatesList[currentWeatherState].cloudCoverage, weatherStatesList[nextWeatherState].cloudCoverage, transitionBlendValue);
                        currentCloudShadowStrength = Mathf.Lerp(weatherStatesList[currentWeatherState].cloudShadowStrength, weatherStatesList[nextWeatherState].cloudShadowStrength, transitionBlendValue);
                        currentGlobalWetnessValue = Mathf.Lerp(weatherStatesList[currentWeatherState].wetness, weatherStatesList[nextWeatherState].wetness, transitionBlendValue);
                        currentRainStrength = Mathf.Lerp(weatherStatesList[currentWeatherState].rainStrength, weatherStatesList[nextWeatherState].rainStrength, transitionBlendValue);
                        currentHailStrength = Mathf.Lerp(weatherStatesList[currentWeatherState].hailStrength, weatherStatesList[nextWeatherState].hailStrength, transitionBlendValue);
                        currentSnowStrength = Mathf.Lerp(weatherStatesList[currentWeatherState].snowStrength, weatherStatesList[nextWeatherState].snowStrength, transitionBlendValue);
                        currentFogDensityMultiplier = Mathf.Lerp(weatherStatesList[currentWeatherState].fogDensityMultiplier, weatherStatesList[nextWeatherState].fogDensityMultiplier, transitionBlendValue);
                        currentCloudsMorphingSpeed = Mathf.Lerp(weatherStatesList[currentWeatherState].cloudsMorphingSpeed, weatherStatesList[nextWeatherState].cloudsMorphingSpeed, transitionBlendValue);
                        currentWindStrength = Mathf.Lerp(weatherStatesList[currentWeatherState].windStrength, weatherStatesList[nextWeatherState].windStrength, transitionBlendValue);
                        currentWindZoneMain = Mathf.Lerp(weatherStatesList[currentWeatherState].windZoneMain, weatherStatesList[nextWeatherState].windZoneMain, transitionBlendValue);
                        currentWindZoneTurbulence = Mathf.Lerp(weatherStatesList[currentWeatherState].windZoneTurbulence, weatherStatesList[nextWeatherState].windZoneTurbulence, transitionBlendValue);
                    }
                    else
                    {
                        currentCloudDensity = Mathf.Lerp(oldWeatherState.cloudDensity, weatherStatesList[nextWeatherState].cloudDensity, transitionBlendValue);
                        currentCloudCoverage = Mathf.Lerp(oldWeatherState.cloudCoverage, weatherStatesList[nextWeatherState].cloudCoverage, transitionBlendValue);
                        currentCloudShadowStrength = Mathf.Lerp(oldWeatherState.cloudShadowStrength, weatherStatesList[nextWeatherState].cloudShadowStrength, transitionBlendValue);
                        currentGlobalWetnessValue = Mathf.Lerp(oldWeatherState.wetness, weatherStatesList[nextWeatherState].wetness, transitionBlendValue);
                        currentRainStrength = Mathf.Lerp(oldWeatherState.rainStrength, weatherStatesList[nextWeatherState].rainStrength, transitionBlendValue);
                        currentHailStrength = Mathf.Lerp(oldWeatherState.hailStrength, weatherStatesList[nextWeatherState].hailStrength, transitionBlendValue);
                        currentSnowStrength = Mathf.Lerp(oldWeatherState.snowStrength, weatherStatesList[nextWeatherState].snowStrength, transitionBlendValue);
                        currentFogDensityMultiplier = Mathf.Lerp(oldWeatherState.fogDensityMultiplier, weatherStatesList[nextWeatherState].fogDensityMultiplier, transitionBlendValue);
                        currentCloudsMorphingSpeed = Mathf.Lerp(oldWeatherState.cloudsMorphingSpeed, weatherStatesList[nextWeatherState].cloudsMorphingSpeed, transitionBlendValue);
                        currentWindStrength = Mathf.Lerp(oldWeatherState.windStrength, weatherStatesList[nextWeatherState].windStrength, transitionBlendValue);
                        currentWindZoneMain = Mathf.Lerp(oldWeatherState.windZoneMain, weatherStatesList[nextWeatherState].windZoneMain, transitionBlendValue);
                        currentWindZoneTurbulence = Mathf.Lerp(oldWeatherState.windZoneTurbulence, weatherStatesList[nextWeatherState].windZoneTurbulence, transitionBlendValue);
                    }

                    if (randomiseWindDirection)
                    {
                        currentWindDirection = Vector2.Lerp(oldWindDirection, nextWindDirection, transitionBlendValue);
                    }

                    currentWeatherTransitionTimer += Time.deltaTime;
                    if (currentWeatherTransitionTimer > currentWeatherTransitionDuration)
                    {
                        // End the transition
                        currentWeatherState = nextWeatherState;
                        transitioningBetweenWeatherStates = false;

                        currentWindDirection = nextWindDirection;

                        // Determine state duration time
                        currentWeatherStateDuration = UnityEngine.Random.Range(weatherStatesList[currentWeatherState].minStateDuration, weatherStatesList[currentWeatherState].maxStateDuration);
                        currentWeatherStateTimer = 0f;
                    }
                }
                else
                {
                    // Set values for current weather state
                    currentCloudDensity = weatherStatesList[currentWeatherState].cloudDensity;
                    currentCloudCoverage = weatherStatesList[currentWeatherState].cloudCoverage;
                    currentCloudShadowStrength = weatherStatesList[currentWeatherState].cloudShadowStrength;
                    currentGlobalWetnessValue = weatherStatesList[currentWeatherState].wetness;
                    currentRainStrength = weatherStatesList[currentWeatherState].rainStrength;
                    currentHailStrength = weatherStatesList[currentWeatherState].hailStrength;
                    currentSnowStrength = weatherStatesList[currentWeatherState].snowStrength;
                    currentFogDensityMultiplier = weatherStatesList[currentWeatherState].fogDensityMultiplier;
                    currentCloudsMorphingSpeed = weatherStatesList[currentWeatherState].cloudsMorphingSpeed;
                    currentWindStrength = weatherStatesList[currentWeatherState].windStrength;
                    currentWindZoneMain = weatherStatesList[currentWeatherState].windZoneMain;
                    currentWindZoneTurbulence = weatherStatesList[currentWeatherState].windZoneTurbulence;

                    if (allowAutomaticTransitions)
                    {
                        currentWeatherStateTimer += Time.deltaTime;
                        if (currentWeatherStateTimer > currentWeatherStateDuration)
                        {
                            transitioningBetweenWeatherStates = true;

                            // Set next weather state
                            List<int> availableWeatherStates = new List<int>();
                            availableWeatherStates.Add(currentWeatherState);
                            if (currentWeatherState > 0) { availableWeatherStates.Add(currentWeatherState - 1); }
                            if (currentWeatherState + 1 < weatherStatesList.Count) { availableWeatherStates.Add(currentWeatherState + 1); }

                            float totalProbability = 0;
                            for (int ws = 0; ws < availableWeatherStates.Count; ws++)
                            {
                                totalProbability += weatherStatesList[availableWeatherStates[ws]].probability;
                            }
                            float randomProbability = UnityEngine.Random.Range(0f, totalProbability);
                            float cumulativeProbability = 0;
                            for (int ws = 0; ws < availableWeatherStates.Count; ws++)
                            {
                                cumulativeProbability += weatherStatesList[availableWeatherStates[ws]].probability;
                                if (cumulativeProbability >= randomProbability)
                                {
                                    nextWeatherState = availableWeatherStates[ws];
                                    ws = availableWeatherStates.Count;
                                }
                            }

                            if (randomiseWindDirection)
                            {
                                oldWindDirection = currentWindDirection;
                                // Determine a random wind direction
                                nextWindDirection = (new Vector2(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f))).normalized;
                            }

                            // Determine transition time
                            currentWeatherTransitionDuration = UnityEngine.Random.Range(minWeatherTransitionDuration, maxWeatherTransitionDuration);
                            currentWeatherTransitionTimer = 0f;
                        }
                    }
                }
            }

            if (weatherCameraList != null)
            {
                LBImageFX imageFXScript;
                Camera weatherCamera;
                numWeatherCameraList = weatherCameraList.Count;

                // Loop through all the cameras with an attached ImageFX script
                for (wCamIdx = 0; wCamIdx < numWeatherCameraList; wCamIdx++)
                {
                    weatherCamera = weatherCameraList[wCamIdx];

                    if (weatherCamera == null) { continue; }
                    imageFXScript = weatherCamera.GetComponent<LBImageFX>();
                    if (imageFXScript != null)
                    {
                        // Update fog density - at the moment all this takes into account is lighting conditions and a Multiplier
                        // for each weather state
                        if (setupMode == SetupMode.Simple)
                        {
                            imageFXScript.distanceFogDensity = LerpFloat(timeFloat, dayFogDensity, nightFogDensity) * currentFogDensityMultiplier;
                        }
                        else
                        {
                            imageFXScript.distanceFogDensity = ((fogDensityCurve.Evaluate(timeFloat) * (maxFogDensity - minFogDensity)) + minFogDensity) * currentFogDensityMultiplier;
                        }

                        // Update cloud density, coverage, animation, morphing speed, and colour
                        if (useClouds)
                        {
                            imageFXScript.cloudsDensity = currentCloudDensity;
                            imageFXScript.cloudsCoverage = currentCloudCoverage;
                            imageFXScript.maxShadowStrength = currentCloudShadowStrength;
                            if (isHDREnabled)
                            {
                                // LerpFogColor uses Color.Lerp. We haven't verified if this works correctly with HDR
                                imageFXScript.cloudsUpperColourHDR = LerpFogColor(timeFloat, cloudsUpperColourDayHDR, cloudsUpperColourNightHDR);
                                imageFXScript.cloudsLowerColourHDR = LerpFogColor(timeFloat, cloudsLowerColourDayHDR, cloudsLowerColourNightHDR);
                            }
                            else
                            {
                                imageFXScript.cloudsUpperColour = LerpFogColor(timeFloat, cloudsUpperColourDay, cloudsUpperColourNight);
                                imageFXScript.cloudsLowerColour = LerpFogColor(timeFloat, cloudsLowerColourDay, cloudsLowerColourNight);
                            }
                            imageFXScript.cloudsMorphingSpeed = currentCloudsMorphingSpeed;
                            if (randomiseWindDirection) { imageFXScript.cloudsAnimationSpeed = currentWindDirection * currentWindStrength; }
                            else { imageFXScript.cloudsAnimationSpeed = imageFXScript.cloudsAnimationSpeed.normalized * currentWindStrength; currentWindDirection = imageFXScript.cloudsAnimationSpeed.normalized; }
                        }
                    }

                    // Use a precreated and sized list to eliminate garbage collection
                    weatherCamera.GetComponentsInChildren(particleSystemList);
                    numWeatherParticleSystems = (particleSystemList == null ? 0 : particleSystemList.Count);

                    float _currentParticleStrength = 0f;
                    float _currentParticleMaxWindEffect = 0f;

                    for (wParticleIdx = 0; wParticleIdx < numWeatherParticleSystems; wParticleIdx++)
                    {
                        weatherParticle = particleSystemList[wParticleIdx];

                        if (weatherParticle != null)
                        {
                            weatherParticleGameObject = weatherParticle.gameObject;

                            // Use CompareTag to eliminate garbage collection
                            if (weatherParticleGameObject.CompareTag(rainParticleTag))
                            {
                                _currentParticleStrength = currentRainStrength * 500f;
                                _currentParticleMaxWindEffect = rainMaxWindEffect;
                            }
                            else if (weatherParticleGameObject.CompareTag(hailParticleTag))
                            {
                                _currentParticleStrength = currentHailStrength * 500f;
                                _currentParticleMaxWindEffect = hailMaxWindEffect;
                            }
                            else if (weatherParticleGameObject.CompareTag(snowParticleTag))
                            {
                                _currentParticleStrength = currentSnowStrength * 500f;
                                _currentParticleMaxWindEffect = snowMaxWindEffect;
                            }

                            // Set particle emission rate
                            ParticleSystem.EmissionModule psem = weatherParticle.emission;

                            // The rate particles are spawned has been replaced in U5.5 with
                            // rateOverDistance and rateOverTime
                            #if UNITY_5_5_OR_NEWER
                            ParticleSystem.MinMaxCurve psmmc = psem.rateOverTime;
                            #else
                            ParticleSystem.MinMaxCurve psmmc = psem.rate;
                            #endif

                            #if UNITY_5_4_OR_NEWER
                            psmmc.constant = _currentParticleStrength;
                            #else
                            psmmc.constantMin = _currentParticleStrength;
                            psmmc.constantMax = _currentParticleStrength;
                            #endif

                            // The rate particles are spawned has been replaced in U5.5 with
                            // rateOverDistance and rateOverTime                 
                            #if UNITY_5_5_OR_NEWER
                            psem.rateOverTime = psmmc;
                            #else
                            psem.rate = psmmc;
                            #endif

                            // Set particle velocity direction
                            ParticleSystem.VelocityOverLifetimeModule pvolm = weatherParticle.velocityOverLifetime;
                            ParticleSystem.MinMaxCurve pvolmx = pvolm.x;
                            ParticleSystem.MinMaxCurve pvolmz = pvolm.z;
                            Vector2 currentWindDir = currentWindDirection * currentWindStrength * 0.01f;
                            #if UNITY_5_4_OR_NEWER
                            pvolmx.constant = Mathf.Clamp(currentWindDir.x, -_currentParticleMaxWindEffect, _currentParticleMaxWindEffect);
                            pvolmz.constant = Mathf.Clamp(currentWindDir.y, -_currentParticleMaxWindEffect, _currentParticleMaxWindEffect);
                            #else
                            pvolmx.constantMin = Mathf.Clamp(currentWindDir.x, -_currentParticleMaxWindEffect, _currentParticleMaxWindEffect);
                            pvolmx.constantMax = Mathf.Clamp(currentWindDir.x, -_currentParticleMaxWindEffect, _currentParticleMaxWindEffect);
                            pvolmz.constantMin = Mathf.Clamp(currentWindDir.y, -_currentParticleMaxWindEffect, _currentParticleMaxWindEffect);
                            pvolmz.constantMax = Mathf.Clamp(currentWindDir.y, -_currentParticleMaxWindEffect, _currentParticleMaxWindEffect);
                            #endif
                            pvolm.x = pvolmx;
                            pvolm.z = pvolmz;
                        }
                    }
                }
            }

            if (useWindZone && weatherWindZone != null)
            {
                // Update wind zone
                weatherWindZone.windMain = currentWindZoneMain;
                weatherWindZone.windTurbulence = currentWindZoneTurbulence;
                weatherWindZone.transform.rotation = Quaternion.LookRotation(new Vector3(currentWindDirection.x, 0f, currentWindDirection.y));
            }

            // Update global shader variable
            Shader.SetGlobalFloat("_LBGlobalWetness", currentGlobalWetnessValue);
        }

        /// <summary>
        /// This will enable or disable the use of clouds (and cloud shadows) for each
        /// weather camera in weatherCameraList. It should NOT be used
        /// each frame as it will trigger a shader recompile if the 
        /// value is changed. It should never be called from Update()
        /// or UpdateWeather()
        /// </summary>
        /// <param name="showErrors"></param>
        public void UpdateWeatherUseCloudsSetting(bool showErrors)
        {
            // Verify that refreshed camera list has ImageFX attached
            VerifyImageFXComponent(showErrors);

            // Update to enable renderClouds on multiple cameras
            if (useWeather && weatherCameraList != null)
            {
                LBImageFX imageFXScript;

                // Loop through all the cameras with an attached ImageFX script
                foreach (Camera weatherCamera in weatherCameraList)
                {
                    if (weatherCamera == null) { continue; }
                    imageFXScript = weatherCamera.GetComponent<LBImageFX>();
                    if (imageFXScript != null)
                    {
                        imageFXScript.renderClouds = useClouds;
                        imageFXScript.renderCloudShadows = useCloudShadows;
                        imageFXScript.isHDREnabled = isHDREnabled;

                        imageFXScript.cloudStyle = cloudStyle;
                        imageFXScript.SetCloudStyleTextures();

                        imageFXScript.use3DNoise = use3DNoise;
                        imageFXScript.cloudsQualityLevel = cloudsQualityLevel;
                        imageFXScript.cloudsDetailAmount = cloudsDetailAmount;
                    }
                }
            }
        }

        /// <summary>
        /// Verify that there is a valid weatherStatesList
        /// Verify that there is at least one LBWeatherState in the list
        /// </summary>
        public void ValidateWeatherStates()
        {
            if (weatherStatesList == null) { weatherStatesList = new List<LBWeatherState>(); }
            if (weatherStatesList.Count == 0)
            {
                LBWeatherState defaultWeatherState = new LBWeatherState();
                defaultWeatherState.name = "Clear";
                defaultWeatherState.probability = 1f;
                defaultWeatherState.wetness = 0f;
                defaultWeatherState.cloudDensity = 3f;
                defaultWeatherState.cloudCoverage = 0.3f;
                defaultWeatherState.cloudShadowStrength = 0.5f;
                defaultWeatherState.rainStrength = 0f;
                defaultWeatherState.fogDensityMultiplier = 1f;
                defaultWeatherState.windStrength = 250f;
                defaultWeatherState.windZoneMain = 1f;
                defaultWeatherState.windZoneTurbulence = 1f;
                weatherStatesList.Add(defaultWeatherState);
            }
        }

        /// <summary>
        /// Get the list of cameras in the scene - ignore the Celestials Camera uses for the stars
        /// </summary>
        public void RefreshWeatherCameraList(bool updateImageFXComponents, bool showErrors)
        {
            List<Camera> originalList = new List<Camera>();

            // Get all the active cameras in the scene
            Camera[] sceneCameras = GameObject.FindObjectsOfType<Camera>();

            if (weatherCameraList == null) { weatherCameraList = new List<Camera>(); }

            // Remember the original list of cameras
            if (updateImageFXComponents) { originalList.AddRange(weatherCameraList); }

            weatherCameraList.Clear();
            weatherCameraList.AddRange(sceneCameras);

            // If present, remove the celestrials camera
            weatherCameraList.RemoveAll(c => c.gameObject.name == "Celestials Camera" || c.gameObject.name.StartsWith("Water4AdvancedReflection"));

            if (updateImageFXComponents)
            {
                // Remove ImageFX on cameras no longer in the list
                foreach (Camera camera in originalList)
                {
                    if (camera != null)
                    {
                        Camera newListCamera = weatherCameraList.Find(c => c == camera);
                        if (newListCamera == null) { RemoveWeatherFromCamera(camera, showErrors); RemoveAllParticlesFromCamera(camera, showErrors); }
                    }
                }

                // Verify that refreshed camera list has ImageFX attached
                VerifyImageFXComponent(showErrors);
            }
        }

        /// <summary>
        /// Verify that refreshed camera list has ImageFX (and rain particles system) attached
        /// </summary>
        /// <param name="showErrors"></param>
        public void VerifyImageFXComponent(bool showErrors)
        {
            if (weatherCameraList == null) { weatherCameraList = new List<Camera>(); }

            foreach (Camera camera in weatherCameraList)
            {
                if (camera != null)
                {
                    LBImageFX addedImageFX = AddWeatherToCamera(camera, showErrors);
                    if (addedImageFX == null) { if (showErrors) { Debug.LogWarning("LBLighting.VerifyImageFXComponent failed to add ImageFX script to " + camera.name); } }
                    AddParticlesToCamera(camera, PrecipitationType.Rain, showErrors);
                    AddParticlesToCamera(camera, PrecipitationType.Hail, showErrors);
                }
            }
        }

        /// <summary>
        /// Attach the LBImageFX script to a camera if it isn't already attached
        /// Return a reference to the imagefx script
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public LBImageFX AddWeatherToCamera(Camera camera, bool showErrors)
        {
            LBImageFX lbImageFX = null;

            if (camera == null) { if (showErrors) { Debug.LogWarning("LBLighting.AddWeatherToCamera - the camera object is null"); } }
            else
            {
                // Only add the ImageFX script if it isn't already on the camera
                lbImageFX = camera.GetComponent<LBImageFX>();
                if (lbImageFX == null) { lbImageFX = LBImageFX.AddImageFX(camera); }
            }
            return lbImageFX;
        }

        /// <summary>
        /// If a LBImageFX script is attached to the camera gameobject, remove it.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="showErrors"></param>
        public void RemoveWeatherFromCamera(Camera camera, bool showErrors)
        {
            if (camera == null) { if (showErrors) { Debug.LogWarning("LBLighting.RemoveWeatherFromCamera - the camera object is null"); } }
            else
            {
                // Only attempt to remove ImageFX script if it is attached to the camera
                LBImageFX lbImageFX = camera.GetComponent<LBImageFX>();
                if (lbImageFX != null) { DestroyImmediate(lbImageFX); }
            }
        }

        /// <summary>
        /// Add rain, hail or snow particle prefabs to a camera
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="precipitationType"></param>
        /// <param name="showErrors"></param>
        public void AddParticlesToCamera(Camera camera, PrecipitationType precipitationType, bool showErrors)
        {
            if (camera == null) { if (showErrors) { Debug.LogWarning("LBLighting.AddParticlesToCamera - the camera object is null"); } }
            else
            {
                string _tag = GetPrecipitationParticleTag(precipitationType);
                string _prefabName = GetPrecipitationParticlePrefabName(precipitationType);

                // Only add weather particle system if it isn't already on the camera
                ParticleSystem[] cameraParticles = camera.GetComponentsInChildren<ParticleSystem>();
                bool alreadyHasWeatherParticles = false;
                for (int ps = 0; ps < cameraParticles.Length; ps++)
                {
                    if (cameraParticles[ps].gameObject.tag == _tag) { alreadyHasWeatherParticles = true; }
                }
                if (!alreadyHasWeatherParticles)
                {
                    Transform weatherParticleSystemPrefab = (Transform)Resources.Load(_prefabName, typeof(Transform));
                    if (weatherParticleSystemPrefab != null)
                    {
                        // Update the prefab transform variables in the class
                        // These are used in LBTemplate.cs when tranferring LBLighting between projects.
                        switch (precipitationType)
                        {
                            case PrecipitationType.Rain:
                                rainParticleSystemPrefab = weatherParticleSystemPrefab;
                                break;
                            case PrecipitationType.Hail:
                                hailParticleSystemPrefab = weatherParticleSystemPrefab;
                                break;
                            case PrecipitationType.Snow:
                                snowParticleSystemPrefab = weatherParticleSystemPrefab;
                                break;
                            default:
                                break;
                        }

                        Transform wpsObj = (Transform)Instantiate(weatherParticleSystemPrefab, Vector3.zero, Quaternion.identity);
                        wpsObj.parent = camera.transform;
                        wpsObj.localPosition = new Vector3(0f, 1f, 3f);
                        wpsObj.localRotation = Quaternion.Euler(270f, 0f, 0f);
                        // Give the name the same name as the Unity Tag
                        wpsObj.gameObject.name = _tag;
                    }
                }
            }
        }

        /// <summary>
        /// Remove a weather precipitation particle child gameobject from a camera
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="precipitationType"></param>
        /// <param name="showErrors"></param>
        public void RemoveParticlesFromCamera(Camera camera, PrecipitationType precipitationType, bool showErrors)
        {
            if (camera == null) { if (showErrors) { Debug.LogWarning("LBLighting.RemoveParticlesFromCamera - the camera object is null"); } }
            else
            {
                string _tag = GetPrecipitationParticleTag(precipitationType);

                // Only attempt to remove rain particles system if it is attached to the camera
                ParticleSystem[] cameraParticles = camera.GetComponentsInChildren<ParticleSystem>();
                for (int ps = 0; ps < cameraParticles.Length; ps++)
                {
                    if (cameraParticles[ps].gameObject.tag == _tag) { DestroyImmediate(cameraParticles[ps].gameObject); }
                }
            }
        }

        /// <summary>
        /// Remove Rain/Hail/Snow weather precipitation particle child gameobjects from a camera
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="showErrors"></param>
        public void RemoveAllParticlesFromCamera(Camera camera, bool showErrors)
        {
            if (camera == null) { if (showErrors) { Debug.LogWarning("LBLighting.RemoveAllParticlesFromCamera - the camera object is null"); } }
            else
            {
                string rainTag = GetPrecipitationParticleTag(PrecipitationType.Rain);
                string hailTag = GetPrecipitationParticleTag(PrecipitationType.Hail);
                string snowTag = GetPrecipitationParticleTag(PrecipitationType.Snow);

                // Only attempt to remove rain particles system if it is attached to the camera
                ParticleSystem[] cameraParticles = camera.GetComponentsInChildren<ParticleSystem>();
                for (int ps = 0; ps < cameraParticles.Length; ps++)
                {
                    string _tag = cameraParticles[ps].gameObject.tag;
                    if (_tag == rainTag || _tag == hailTag || _tag == snowTag) { DestroyImmediate(cameraParticles[ps].gameObject); }
                }
            }
        }

        #endregion

        #region Screen Clock Methods

        /// <summary>
        /// Turn on/off the on-screen clock
        /// </summary>
        /// <param name="isDisplayed"></param>
        public void DisplayScreenClock(bool isDisplayed)
        {
            // If clock is to be displayed, first need to validate all
            // the components are correctly configured
            if (isDisplayed)
            {
                ValidateLBLightingCanvas();
            }

            // Turn display on/off
            if (screenClockPanel != null)
            {
                screenClockPanel.gameObject.SetActive(isDisplayed);
            }
            else if (!isDisplayed)
            {
                // After a loading LBLighting from a template it is possible
                // that the clock was on before the template was loaded but it isn't required
                // in the current LBLighting settings.

                Transform clockCanvas = this.transform.Find("LB Lighting Canvas");
                if (clockCanvas != null)
                {
                    Transform clockPanel = clockCanvas.Find("ClockPanel");
                    if (clockPanel != null) { clockPanel.gameObject.SetActive(false); }
                }
            }
        }

        public void UpdateScreenClockColour()
        {
            if (screenClockText != null)
            {
                try
                {
                    screenClockText.color = screenClockTextColour;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("LBLighting UpdateScreenClockColour " + ex.Message);
                }
            }
        }

        #endregion

        #region EDITOR_ONLY Public Methods

#if UNITY_EDITOR

        /// <summary>
        /// SetLightBakeMode - not available at runtime
        /// </summary>
        /// <param name="lightList"></param>
        /// <param name="lightBakingMode"></param>
        public static void SetLightBakeMode(List<Light> lightList, LightBakingMode lightBakingMode)
        {
            if (lightList != null)
            {
                foreach (Light light in lightList)
                {
                    // NOTE: These Serialized classes belong to UnityEditor and aren't available at runtime
                    SerializedObject serializedLight = new SerializedObject(light);
                    SerializedProperty m_LightmappingProp = serializedLight.FindProperty("m_Lightmapping");

                    Debug.Log("Light: " + light.name + " prop:" + m_LightmappingProp.intValue.ToString());

                    m_LightmappingProp.intValue = 2;

                    serializedLight.ApplyModifiedProperties();
                    serializedLight.Update();
                }
            }
        }
#endif

        #endregion

        #region Private Methods

        /// <summary>
        /// This is private because it is called from StartScreenFade()
        /// </summary>
        /// <returns></returns>
        private IEnumerator FadeIn()
        {
            float currentAlphaValue = fadeStartAlpha;
            isFadingIn = true;
            fadeImage.enabled = true;
            if (fadeInDuration == 0) { fadeInDuration = 0.1f; }

            float currentTimeFadeValue = fadeStartAlpha;

            while (currentAlphaValue > fadeTargetValue)
            {
                // Use Unscaled time so not to be affected by Time.timeScale
                currentTimeFadeValue -= Time.unscaledDeltaTime / fadeInDuration;

                // Fade out slowly at first because alpha values < 0.4 have limited visual effect
                currentAlphaValue = Mathf.Pow(Mathf.Clamp01(currentTimeFadeValue), 0.33f);

                fadeImage.color = new Color(Color.black.r, Color.black.g, Color.black.b, currentAlphaValue);
                yield return null;
            }
            fadeImage.enabled = false;
            isFadingIn = false;
            yield return null;
        }

        /// <summary>
        /// This is private because it is called from StartScreenFade()
        /// </summary>
        private IEnumerator FadeOut()
        {
            float currentAlphaValue = fadeStartAlpha;
            isFadingOut = true;
            fadeImage.enabled = true;
            if (fadeOutDuration == 0) { fadeOutDuration = 0.1f; }
            while (currentAlphaValue <= fadeTargetValue)
            {
                // Use Unscaled time so not to be affected by Time.timeScale
                currentAlphaValue += Time.unscaledDeltaTime / fadeOutDuration;
                fadeImage.color = new Color(Color.black.r, Color.black.g, Color.black.b, currentAlphaValue);
                yield return null;
            }
            fadeImage.enabled = false;
            isFadingOut = false;
            yield return null;
        }

        private void ValidateLBLightingCanvas()
        {
            if (lightingCanvas == null)
            {
                // Find the lighting canvas
                lightingCanvas = GetComponentInChildren<Canvas>(true);

                // If lighting canvas doesn't exist create it
                if (lightingCanvas == null)
                {
                    GameObject lightingCanvasGameObject = new GameObject("LB Lighting Canvas");
                    lightingCanvasGameObject.transform.parent = transform;
                    lightingCanvas = lightingCanvasGameObject.AddComponent<Canvas>();
                    if (lightingCanvas != null)
                    {
                        lightingCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                        CanvasScaler canvasScaler = lightingCanvasGameObject.AddComponent<CanvasScaler>();
                        if (canvasScaler) { canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; }
                    }
                }
            }

            if (lightingCanvas != null)
            {
                // If clock UI components aren't set, get them from under the canvas in the scene
                if (screenClockPanel == null)
                {
                    RectTransform[] rectTransforms = lightingCanvas.GetComponentsInChildren<RectTransform>(true);
                    if (rectTransforms != null)
                    {
                        foreach (RectTransform rt in rectTransforms)
                        {
                            if (rt.name == "ClockPanel")
                            {
                                screenClockPanel = rt;
                                break;
                            }
                        }
                    }

                    // If the clock panel doesn't exist, create it
                    if (screenClockPanel == null)
                    {
                        // Add the clock panel as a child of the Canvas
                        GameObject screenClockPanelGameObject = new GameObject("ClockPanel");
                        if (screenClockPanelGameObject != null)
                        {
                            screenClockPanelGameObject.transform.parent = lightingCanvas.transform;
                            screenClockPanel = screenClockPanelGameObject.AddComponent<RectTransform>();

                            // Configure clock Panel rect transform
                            if (screenClockPanel != null)
                            {
                                // Place the panel in the lower right corner
                                screenClockPanel.anchorMin = new Vector2(0.8f, 0f);
                                screenClockPanel.anchorMax = new Vector2(0.97f, 0.15f);
                                screenClockPanel.sizeDelta = Vector2.zero;
                                screenClockPanel.anchoredPosition = Vector2.zero;
                                screenClockPanel.localScale = Vector3.one;
                            }

                            // Add the UI Text component as a child of the ClockPanel
                            GameObject screenClockTextGameObject = new GameObject("ClockText");
                            if (screenClockTextGameObject != null)
                            {
                                screenClockTextGameObject.transform.parent = screenClockPanelGameObject.transform;

                                // Add and configure the clock Text RectTransform
                                RectTransform screenClockTextRectTranform = screenClockTextGameObject.AddComponent<RectTransform>();
                                if (screenClockTextRectTranform != null)
                                {
                                    screenClockTextRectTranform.anchorMin = Vector2.zero;
                                    screenClockTextRectTranform.anchorMax = Vector2.one;
                                    screenClockTextRectTranform.sizeDelta = Vector2.zero;
                                    screenClockTextRectTranform.anchoredPosition = Vector2.zero;
                                    screenClockTextRectTranform.localScale = Vector3.one;

                                    // Add and configure the clock Text
                                    screenClockText = screenClockTextGameObject.AddComponent<Text>();
                                    if (screenClockText != null)
                                    {
                                        // Configure the UI Text
                                        screenClockText.raycastTarget = false;
                                        screenClockText.resizeTextForBestFit = true;
                                        screenClockText.alignment = TextAnchor.MiddleRight;
                                        screenClockText.text = "00:00";
                                    }
                                }
                            }
                        }
                    }

                    if (screenClockPanel != null && screenClockText == null)
                    {
                        screenClockText = lightingCanvas.GetComponentInChildren<Text>(true);
                    }
                }
            }
        }

        private void LerpColor(float timeFloat, Color dayColour, Color nightColour, ref Color updateColour)
        {
            if (timeFloat < 0.5f)
            {
                timeFloat = Mathf.InverseLerp(sunriseTime - 2f, sunriseTime + 2f, timeFloat * 24f);
                updateColour = Color.Lerp(nightColour, dayColour, timeFloat);
            }
            else
            {
                timeFloat = Mathf.InverseLerp(sunsetTime - 2f, sunsetTime + 2f, timeFloat * 24f);
                updateColour = Color.Lerp(dayColour, nightColour, timeFloat);
            }
        }

        private Color LerpColor(float timeFloat, Color dayColour, Color nightColour)
        {
            if (timeFloat < 0.5f)
            {
                timeFloat = Mathf.InverseLerp(sunriseTime - 2f, sunriseTime + 2f, timeFloat * 24f);
                return Color.Lerp(nightColour, dayColour, timeFloat);
            }
            else
            {
                timeFloat = Mathf.InverseLerp(sunsetTime - 2f, sunsetTime + 2f, timeFloat * 24f);
                return Color.Lerp(dayColour, nightColour, timeFloat);
            }
        }

        private Color LerpFogColor(float timeFloat, Color dayColour, Color nightColour)
        {
            if (timeFloat < 0.5f)
            {
                timeFloat = Mathf.InverseLerp(sunriseTime - 1f, sunriseTime + 1f, timeFloat * 24f);
                return Color.Lerp(nightColour, dayColour, timeFloat);
            }
            else
            {
                timeFloat = Mathf.InverseLerp(sunsetTime - 1f, sunsetTime + 1f, timeFloat * 24f);
                return Color.Lerp(dayColour, nightColour, timeFloat);
            }
        }

        private float LerpFloat(float timeFloat, float dayFloat, float nightFloat)
        {
            if (timeFloat < 0.5f)
            {
                timeFloat = Mathf.InverseLerp(sunriseTime - 2f, sunriseTime + 2f, timeFloat * 24f);
                return Mathf.Lerp(nightFloat, dayFloat, timeFloat);
            }
            else
            {
                timeFloat = Mathf.InverseLerp(sunsetTime - 2f, sunsetTime + 2f, timeFloat * 24f);
                return Mathf.Lerp(dayFloat, nightFloat, timeFloat);
            }
        }

        /// <summary>
        /// Returns the Unity Tag used for the weather particle prefabs added as child
        /// of a camera.
        /// </summary>
        /// <param name="precipitationType"></param>
        /// <returns></returns>
        private string GetPrecipitationParticleTag(PrecipitationType precipitationType)
        {
            string _tag = string.Empty;

            switch (precipitationType)
            {
                case PrecipitationType.Rain:
                    _tag = "LB Rain Particles";
                    break;
                case PrecipitationType.Hail:
                    _tag = "LB Hail Particles";
                    break;
                case PrecipitationType.Snow:
                    _tag = "LB Snow Particles";
                    break;
                default:
                    _tag = string.Empty;
                    break;
            }

            return _tag;
        }

        /// <summary>
        /// Returns the name of the Weather Precipitation Particle Prefab
        /// </summary>
        /// <param name="precipitationType"></param>
        /// <returns></returns>
        private string GetPrecipitationParticlePrefabName(PrecipitationType precipitationType)
        {
            string _prefabName = string.Empty;

            switch (precipitationType)
            {
                case PrecipitationType.Rain:
                    _prefabName = "LBRainParticles";
                    break;
                case PrecipitationType.Hail:
                    _prefabName = "LBHailParticles";
                    break;
                case PrecipitationType.Snow:
                    _prefabName = "LBSnowParticles";
                    break;
                default:
                    break;
            }

            return _prefabName;
        }

        #endregion
    }

    #region CUSTOM EDITOR

#if UNITY_EDITOR
    [CustomEditor(typeof(LBLighting))]
    public class LBLightingInspector : Editor
    {
        // Custom editor for LB Lighting

        #region Custom Editor variables
        //private Terrain[] allTerrains;
        private ReflectionProbe[] updatingReflectionProbes;

        //private SerializedObject serializedObj;
        //private SerializedProperty serializedProp;

        private int arrayInt;
        private int temp;
        private int index;
        private string labelText;

        private int insertWeatherStatePos = -1;
        private LBWeatherState weatherStateToMove = null;
        private int moveWeatherStatePos = -1;

        private GUIStyle buttonCompact;

        private bool isSceneSaveRequired = false;
        private Material defaultSkybox;

        private Camera cameraToRemove = null;
        private Camera currentCamera = null;
        private int cameraToRemoveIdx = -1;

        // By default assume support in case there are issues
        // go in and out of play mode
        private bool isNotSupported = false;

        #endregion

        #region Serialized Properties General Settings
        private SerializedProperty lightingModeProp;
        private SerializedProperty sunProp;
        //private SerializedProperty setupModeProp;
        private SerializedProperty sunIntensityProp;
        private SerializedProperty sunIntensityCurveProp;
        private SerializedProperty yAxisRotationProp;
        private SerializedProperty startTimeOfDayProp;
        private SerializedProperty sunriseTimeProp;
        private SerializedProperty sunsetTimeProp;
        private SerializedProperty dayLengthProp;
        private SerializedProperty realtimeTerrainLightingProp;
        private SerializedProperty lightingUpdateIntervalProp;
        private SerializedProperty reflProbesUpdateModeProp;
        #endregion

        #region Serialized Properties Ambient light and fog
        private SerializedProperty dayAmbientLightProp;
        private SerializedProperty nightAmbientLightProp;
        private SerializedProperty dayAmbientLightHDRProp;
        private SerializedProperty nightAmbientLightHDRProp;
        private SerializedProperty dayAmbientGndLightProp;
        private SerializedProperty nightAmbientGndLightProp;
        private SerializedProperty dayAmbientGndLightHDRProp;
        private SerializedProperty nightAmbientGndLightHDRProp;
        private SerializedProperty dayAmbientHznLightProp;
        private SerializedProperty nightAmbientHznLightProp;
        private SerializedProperty dayAmbientHznLightHDRProp;
        private SerializedProperty nightAmbientHznLightHDRProp;

        private SerializedProperty dayFogColourProp;
        private SerializedProperty nightFogColourProp;
        private SerializedProperty dayFogDensityProp;
        private SerializedProperty nightFogDensityProp;
        private SerializedProperty ambientLightGradientProp;
        private SerializedProperty ambientHznLightGradientProp;
        private SerializedProperty ambientGndLightGradientProp;
        private SerializedProperty fogColourGradientProp;
        private SerializedProperty minFogDensityProp;
        private SerializedProperty maxFogDensityProp;
        private SerializedProperty fogDensityCurveProp;

        #endregion

        #region Serialized Properties Clouds

        private SerializedProperty isHDREnabledProp;
        private SerializedProperty use3DNoiseProp;
        private SerializedProperty cloudsUpperColourDayProp;
        private SerializedProperty cloudsUpperColourNightProp;
        private SerializedProperty cloudsLowerColourDayProp;
        private SerializedProperty cloudsLowerColourNightProp;
        private SerializedProperty cloudsUpperColourDayHDRProp;
        private SerializedProperty cloudsUpperColourNightHDRProp;
        private SerializedProperty cloudsLowerColourDayHDRProp;
        private SerializedProperty cloudsLowerColourNightHDRProp;
        private SerializedProperty cloudStyleProp;
        private SerializedProperty cloudsQualityLevelProp;
        private SerializedProperty cloudsDetailAmountProp;
        private SerializedProperty useCloudShadowsProp;

        #endregion

        #region Static GUIContent

        // General light settings
        private static readonly GUIContent lightModeContent = new GUIContent("Light Mode", "Choose whether the lighting is static or dynamic. In static mode, the lighting doesn’t change at runtime. In dynamic mode, a customisable day/night cycle is implemented.");
        private static readonly GUIContent sunContent = new GUIContent("Sun", "The sun object");
        private static readonly GUIContent setupModeContent = new GUIContent("Setup Mode", "Use Advanced mode if you need more control over the sun intensity, moon intensity, ambient light or fog colour at different parts of the day.");
        private static readonly GUIContent envAmbientSourceContent = new GUIContent("Ambient Source", "The source of environment ambient light. Can be a single blended colour between day and night, or can have separate blends for ground, horizon and sky.");
        private static readonly GUIContent sunIntensityContent = new GUIContent("Sun Intensity", "The light intensity (and bounce intensity) of the sun");
        private static readonly GUIContent maxSunIntensityContent = new GUIContent("Max Sun Intensity", "The maximum light intensity (and bounce intensity) of the sun");
        private static readonly GUIContent sunIntensityCurveContent = new GUIContent("Sun Intensity Curve", "Curve defining the sun intensity at each time in the day (time of day is expressed as a 0-1 float) - with 0 being 0 intensity and 1 maximum intensity");
        private static readonly GUIContent sunOrbitRotationContent = new GUIContent("Sun Orbit Rotation", "The rotation of the sun's orbit on the y-axis");
        private static readonly GUIContent timeOfDayContent = new GUIContent("Time Of Day", "Time of day as a 24-hour float");
        private static readonly GUIContent sunriseTimeContent = new GUIContent("Sunrise Time", "Sunrise time of day as a 24-hour float");
        private static readonly GUIContent sunsetTimeContent = new GUIContent("Sunset Time", "Sunset time of day as a 24-hour float");
        private static readonly GUIContent dayLengthContent = new GUIContent("Day Length", "Day/night cycle length in seconds");
        private static readonly GUIContent startTimeOfDayContent = new GUIContent("Start Time Of Day", "Time of day at which the game starts as a 24-hour float");
        private static readonly GUIContent realtimeTerrainLightingContent = new GUIContent("Realtime Terrain Lighting", "Whether tree and terrain lightmap colours will be updated periodically (has no effect for SpeedTrees). Can have some performance impact.");
        private static readonly GUIContent lightingUpdateIntervalContent = new GUIContent("Lighting Update Interval", "Time in seconds between each lighting update");
        private static readonly GUIContent reflProbeUpdateModeContent = new GUIContent("Refl. Probes Update Mode", "Which reflection probes will be updated");
        private static readonly GUIContent resetGeneralSettingsContent = new GUIContent("Reset", "Reset General Light Settings to factory defaults");
        private static readonly GUIContent isHDREnabledContent = new GUIContent("Use HDR", "Allows High Dynamic Range colours to be selected. HDR can be useful when also using the Unity Post Processing stack.");

        // AMBIENT LIGHT AND FOG SETTINGS
        private static GUIContent ambientLightGradientContent = new GUIContent("Ambient Light Gradient", "");  // Gets updated
        private static readonly GUIContent ambientHznLightGradientContent = new GUIContent("Ambient Light Horizon", "Gradient defining the ambient light horizon colour at each time in the day (time of day is expressed as a 0-1 float)");
        private static readonly GUIContent ambientGndLightGradientContent = new GUIContent("Ambient Light Ground", "Gradient defining the ambient light ground colour at each time in the day (time of day is expressed as a 0-1 float)");
        private static readonly GUIContent fogColourGradientContent = new GUIContent("Fog Colour Gradient", "Gradient defining the fog colour at each time in the day (time of day is expressed as a 0-1 float)");
        private static readonly GUIContent minFogDensityContent = new GUIContent("Min Fog Density", "The minimum fog density, corresponding to 0 on the fog density curve");
        private static readonly GUIContent maxFogDensityContent = new GUIContent("Max Fog Density", "The maximum fog density, corresponding to 1 on the fog density curve");
        private static readonly GUIContent fogDensityCurveContent = new GUIContent("Fog Density Curve", "Curve defining the fog density at each time in the day (time of day is expressed as a 0-1 float)");
        // Not readonly as tooltip gets updated
        private static GUIContent dayAmbientLightContent = new GUIContent("Day Ambient Light", "");
        private static GUIContent nightAmbientLightContent = new GUIContent("Night Ambient Light", "");
        private static GUIContent dayAmbientGndLightContent = new GUIContent("Day Ambient Light", "");
        private static GUIContent nightAmbientGndLightContent = new GUIContent("Night Ambient Light", "");
        private static GUIContent dayAmbientHznLightContent = new GUIContent("Day Ambient Light", "");
        private static GUIContent nightAmbientHznLightContent = new GUIContent("Night Ambient Light", "");
        private static GUIContent dayFogColourContent = new GUIContent("Day Fog Colour", "");
        private static GUIContent nightFogColourContent = new GUIContent("Night Fog Colour", "");
        private static GUIContent dayFogDensityContent = new GUIContent("Day Fog Density", "");
        private static GUIContent nightFogDensityContent = new GUIContent("Night Fog Density", "");

        // Celestials settings
        private static readonly GUIContent useCelestialsContent = new GUIContent("Use Celestials", "Whether celestial objects will be calculated and rendered");
        private static readonly GUIContent mainCameraContent = new GUIContent("Main Camera", "The scene's main camera");
        private static readonly GUIContent numStarsContent = new GUIContent("Number Of Stars", "The number of stars to be created");
        private static readonly GUIContent starSizeContent = new GUIContent("Star Size", "The size of the rendered stars");
        private static readonly GUIContent starVisibilityCurveContent = new GUIContent("Star Visibility Curve", "Curve defining the star visibility at each time in the day (time of day is expressed as a 0-1 float) - with 0 being no stars visible and 1 being all stars visible");
        private static readonly GUIContent useMoonContent = new GUIContent("Use Moon", "Whether moon light will be calculated and rendered. This is also required if you want to render image effects at night.");
        private static readonly GUIContent moonIntensityContent = new GUIContent("Moon Intensity", "The light intensity (and bounce intensity) of the moon");
        private static readonly GUIContent maxMoonIntensityContent = new GUIContent("Max Moon Intensity", "The maximum light intensity (and bounce intensity) of the moon");
        private static readonly GUIContent moonIntensityCurveContent = new GUIContent("Moon Intensity Curve", "Curve defining the moon light intensity at each time in the day (time of day is expressed as a 0-1 float) - with 0 being 0 intensity and 1 maximum intensity");
        private static readonly GUIContent moonOrbitRotationContent = new GUIContent("Moon Orbit Rotation", "The rotation of the moon's orbit on the y-axis");
        private static readonly GUIContent moonSizeContent = new GUIContent("Moon Size", "The size of the rendered moon");

        // Weather settings
        private static readonly GUIContent useWeatherContent = new GUIContent("Use Weather", "Whether a weather cycle will be simulated");
        private static readonly GUIContent useUnityDistanceFogContent = new GUIContent("Use Unity Fog", "Whether Unity fog is used in parallel with LBImageFX fog. Enable this if you have transparent objects in your scene that need to work with fog");
        private static readonly GUIContent useCloudsContent = new GUIContent("Render Clouds", "Whether clouds will be rendered");
        private static readonly GUIContent use3DNoiseContent = new GUIContent("Use 3D Noise", "Whether the clouds are rendered using 3D as opposed to 2D noise - this is particularly performance heavy");
        private static readonly GUIContent cloudStyleContent = new GUIContent("Cloud Style", "The style of the clouds rendered");
        private static readonly GUIContent cloudsQualityLevelContent = new GUIContent("Clouds Quality Level", "The quality of the raymarching used to determine cloud density - higher settings increase quality at the cost of performance");
        private static readonly GUIContent cloudsDetailAmountContent = new GUIContent("Clouds Detail Amount", "The amount of extra detail added to them - increasing this will decrease performance slightly");
        private static readonly GUIContent cloudsUpperColourDayContent = new GUIContent("Upper Colour (Day)", "The upper colour of clouds at midday");
        private static readonly GUIContent cloudsUpperColourNightContent = new GUIContent("Upper Colour (Night)", "The upper colour of clouds at midnight");
        private static readonly GUIContent cloudsLowerColourDayContent = new GUIContent("Lower Colour (Day)", "The lower colour of clouds at midday");
        private static readonly GUIContent cloudsLowerColourNightContent = new GUIContent("Lower Colour (Night)", "The lower colour of clouds at midnight");
        private static readonly GUIContent useCloudShadowsContent = new GUIContent("Render Cloud Shadows", "Whether cloud shadows will be rendered");

        // Clock settings
        private static readonly GUIContent useScreenClockContent = new GUIContent("Display Screen Clock", "Whether a clock will be displayed in the scene");
        private static readonly GUIContent useScreenClockSecsContent = new GUIContent("Show Seconds", "Include seconds. HH:MM:SS");
        private static readonly GUIContent useScreenClockResetContent = new GUIContent("Reset", "Reset Screen Clock Settings to factory defaults");
        private static readonly GUIContent useScreenClockTextColourContent = new GUIContent("Clock Text Colour", "The colour of the on-screen 24-hour clock text");

        #endregion

        #region Initialise
        void OnEnable()
        {
            // Check support
            isNotSupported = (LBLandscape.IsURP(false) || LBLandscape.IsHDRP(false) || LBLandscape.IsLWRP(false));

            if (!EditorApplication.isPlaying)
            {
                // Ensure the fade image is disabled if not in play mode when the
                // custom editor is enabled. This can help if play mode has been exited
                // while a fade is still underway.
                LBLighting lightingScript = (LBLighting)target;
                Image fadeImage = lightingScript.gameObject.GetComponent<Image>();
                if (fadeImage != null) { fadeImage.enabled = false; }

                // Check if the Fake Environment Light is present, if not add it.
                // By default, Unity uses the brightest light in the scene for the sun.
                // This happens when in the Unity Lighting inspect, on the Scene tab under
                // Environment Lighting if the sun object is not populated.
                // This can lead to LB having a bright sky at night
                if (lightingScript.sun != null)
                {
                    // If the fake light doesn't exist, add it in.
                    if (!lightingScript.sun.transform.Find("LBFakeEnvironmentLight"))
                    {
                        Transform fakeLightPrefab = (Transform)AssetDatabase.LoadAssetAtPath("Assets/LandscapeBuilder/Prefabs/LBFakeEnvironmentLight.prefab", typeof(Transform));
                        if (fakeLightPrefab != null)
                        {
                            Transform fakeLightObj = (Transform)Instantiate(fakeLightPrefab, Vector3.zero, Quaternion.identity);
                            if (fakeLightObj != null)
                            {
                                fakeLightObj.parent = lightingScript.sun.transform;
                                // Remove the (Clone) from end of name
                                fakeLightObj.name = "LBFakeEnvironmentLight";
                                fakeLightObj.localPosition = Vector3.zero;
                                fakeLightObj.localRotation = Quaternion.identity;
                                fakeLightObj.localScale = Vector3.one;
                                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                            }
                        }
                    }
                }
            }

            #region Find Properties General Settings
            lightingModeProp = serializedObject.FindProperty("lightingMode");
            sunProp = serializedObject.FindProperty("sun");
            //setupModeProp = serializedObject.FindProperty("setupMode");
            sunIntensityProp = serializedObject.FindProperty("sunIntensity");
            sunIntensityCurveProp = serializedObject.FindProperty("sunIntensityCurve");
            yAxisRotationProp = serializedObject.FindProperty("yAxisRotation");
            startTimeOfDayProp = serializedObject.FindProperty("startTimeOfDay");
            sunriseTimeProp = serializedObject.FindProperty("sunriseTime");
            sunsetTimeProp = serializedObject.FindProperty("sunsetTime");
            dayLengthProp = serializedObject.FindProperty("dayLength");
            realtimeTerrainLightingProp = serializedObject.FindProperty("realtimeTerrainLighting");
            lightingUpdateIntervalProp = serializedObject.FindProperty("lightingUpdateInterval");
            reflProbesUpdateModeProp = serializedObject.FindProperty("reflProbesUpdateMode");
            #endregion

            #region Find Properties Ambient light and fog
            dayAmbientLightProp = serializedObject.FindProperty("dayAmbientLight");
            nightAmbientLightProp = serializedObject.FindProperty("nightAmbientLight");
            dayAmbientLightHDRProp = serializedObject.FindProperty("dayAmbientLightHDR");
            nightAmbientLightHDRProp = serializedObject.FindProperty("nightAmbientLightHDR");
            dayAmbientGndLightProp = serializedObject.FindProperty("dayAmbientGndLight");
            nightAmbientGndLightProp = serializedObject.FindProperty("nightAmbientGndLight");
            dayAmbientGndLightHDRProp = serializedObject.FindProperty("dayAmbientGndLightHDR");
            nightAmbientGndLightHDRProp = serializedObject.FindProperty("nightAmbientGndLightHDR");
            dayAmbientHznLightProp = serializedObject.FindProperty("dayAmbientHznLight");
            nightAmbientHznLightProp = serializedObject.FindProperty("nightAmbientHznLight");
            dayAmbientHznLightHDRProp = serializedObject.FindProperty("dayAmbientHznLightHDR");
            nightAmbientHznLightHDRProp = serializedObject.FindProperty("nightAmbientHznLightHDR");
            dayFogColourProp = serializedObject.FindProperty("dayFogColour");
            nightFogColourProp = serializedObject.FindProperty("nightFogColour");
            dayFogDensityProp = serializedObject.FindProperty("dayFogDensity");
            nightFogDensityProp = serializedObject.FindProperty("nightFogDensity");
            ambientLightGradientProp = serializedObject.FindProperty("ambientLightGradient");
            ambientHznLightGradientProp = serializedObject.FindProperty("ambientHznLightGradient");
            ambientGndLightGradientProp = serializedObject.FindProperty("ambientGndLightGradient");
            fogColourGradientProp = serializedObject.FindProperty("fogColourGradient");
            minFogDensityProp = serializedObject.FindProperty("minFogDensity");
            maxFogDensityProp = serializedObject.FindProperty("maxFogDensity");
            fogDensityCurveProp = serializedObject.FindProperty("fogDensityCurve");

            #endregion

            #region Find Properties Clouds
            isHDREnabledProp = serializedObject.FindProperty("isHDREnabled");
            use3DNoiseProp = serializedObject.FindProperty("use3DNoise");
            cloudsDetailAmountProp = serializedObject.FindProperty("cloudsDetailAmount");
            cloudStyleProp = serializedObject.FindProperty("cloudStyle");
            cloudsQualityLevelProp = serializedObject.FindProperty("cloudsQualityLevel");
            cloudsUpperColourDayProp = serializedObject.FindProperty("cloudsUpperColourDay");
            cloudsUpperColourNightProp = serializedObject.FindProperty("cloudsUpperColourNight");
            cloudsLowerColourDayProp = serializedObject.FindProperty("cloudsLowerColourDay");
            cloudsLowerColourNightProp = serializedObject.FindProperty("cloudsLowerColourNight");
            cloudsUpperColourDayHDRProp = serializedObject.FindProperty("cloudsUpperColourDayHDR");
            cloudsUpperColourNightHDRProp = serializedObject.FindProperty("cloudsUpperColourNightHDR");
            cloudsLowerColourDayHDRProp = serializedObject.FindProperty("cloudsLowerColourDayHDR");
            cloudsLowerColourNightHDRProp = serializedObject.FindProperty("cloudsLowerColourNightHDR");
            useCloudShadowsProp = serializedObject.FindProperty("useCloudShadows");

            #endregion
        }

        #endregion

        // This function overides what is normally seen in the inspector window
        // This allows stuff like buttons to be drawn there
        public override void OnInspectorGUI()
        {
            #region Initialise
            LBLighting lightingScript = (LBLighting)target;

            // Need to only do this once...
            if (lightingScript.proceduralSkybox == null)
            {
                lightingScript.proceduralSkybox = (Material)AssetDatabase.LoadAssetAtPath("Assets/LandscapeBuilder/Materials/LBSkybox.mat", typeof(Material));
            }

            // Reset at the start of each frame
            isSceneSaveRequired = false;

            buttonCompact = new GUIStyle("Button");
            buttonCompact.fontSize = 10;

            EditorGUIUtility.labelWidth = 150f;

            if (isNotSupported)
            {
                EditorGUILayout.HelpBox("LBLighting is not supported on Universal, Lightweight or High Definition Render Pipeline", MessageType.Error);
            }

            #endregion

            lightingScript.setupMode = (LBLighting.SetupMode)EditorGUILayout.EnumPopup(setupModeContent, lightingScript.setupMode);

            // SKYBOX SETTINGS
            #region SKYBOX SETTINGS     
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Skybox Settings", EditorStyles.boldLabel);
            if (lightingScript.showSkyboxSettings) { if (GUILayout.Button("Hide", GUILayout.Width(50f))) { lightingScript.showSkyboxSettings = false; isSceneSaveRequired = true; } }
            else { if (GUILayout.Button("Show", GUILayout.Width(50f))) { lightingScript.showSkyboxSettings = true; isSceneSaveRequired = true; } }
            EditorGUILayout.EndHorizontal();
            if (lightingScript.showSkyboxSettings)
            {
                EditorGUI.BeginChangeCheck();
                lightingScript.skyboxType = (LBLighting.SkyboxType)EditorGUILayout.EnumPopup(new GUIContent("Skybox Type", "Procedural is recommended but six-sided supports Unity 4 hand-painted Skyboxes and transitioning between multiple skyboxes."), lightingScript.skyboxType);
                if (EditorGUI.EndChangeCheck()) { isSceneSaveRequired = true; }
                if (lightingScript.skyboxType == LBLighting.SkyboxType.Procedural)
                {
                    EditorGUI.BeginChangeCheck();
                    lightingScript.proceduralSkybox = (Material)EditorGUILayout.ObjectField(new GUIContent("Skybox Material", "The skybox material to use"), lightingScript.proceduralSkybox, typeof(Material), true);
                    if (EditorGUI.EndChangeCheck()) { isSceneSaveRequired = true; }
                }
                else
                {
                    if (lightingScript.blendedSkybox == null)
                    {
                        lightingScript.blendedSkybox = (Material)AssetDatabase.LoadAssetAtPath("Assets/LandscapeBuilder/Materials/LBSkybox6Sided.mat", typeof(Material));
                        // Need to only do this once...
                        isSceneSaveRequired = true;
                    }
                    if (lightingScript.skyboxesList == null) { lightingScript.skyboxesList = new List<LBSkybox>(); }
                    arrayInt = lightingScript.skyboxesList.Count;
                    if (GUILayout.Button("Add Skybox")) { arrayInt++; }
                    if (arrayInt < 0) { arrayInt = 0; }
                    // Add items to the list
                    if (arrayInt > lightingScript.skyboxesList.Count)
                    {
                        temp = arrayInt - lightingScript.skyboxesList.Count;
                        for (index = 0; index < temp; index++)
                        {
                            lightingScript.skyboxesList.Add(new LBSkybox());
                        }
                        isSceneSaveRequired = true;
                    }
                    for (index = 0; index < lightingScript.skyboxesList.Count; index++)
                    {
                        // Show the elements of the list
                        GUILayout.BeginHorizontal();
                        EditorGUIUtility.labelWidth = 75f;
                        lightingScript.skyboxesList[index].material = (Material)EditorGUILayout.ObjectField("Skybox " + (index + 1), lightingScript.skyboxesList[index].material, typeof(Material), true);

                        EditorGUIUtility.labelWidth = 150f;
                        GUILayout.EndHorizontal();

                        if (lightingScript.skyboxesList.Count > 1 && lightingScript.skyboxesList[index].material != null)
                        {
                            if (!lightingScript.skyboxesList[index].ValidateSixSidedSkybox())
                            {
                                EditorGUILayout.HelpBox(lightingScript.skyboxesList[index].material.name + " does not appear to be valid six-sided skybox material", MessageType.Warning, true);
                            }
                        }

                        float minTime = 0f;
                        float maxTime = 24f;
                        if (index != 0 && index != lightingScript.skyboxesList.Count - 1)
                        {
                            minTime = lightingScript.skyboxesList[index - 1].transitionEndTime;
                        }
                        if (index < lightingScript.skyboxesList.Count - 2)
                        {
                            maxTime = lightingScript.skyboxesList[index + 1].transitionStartTime;
                        }
                        EditorGUI.BeginChangeCheck();
                        lightingScript.skyboxesList[index].transitionStartTime = EditorGUILayout.Slider(new GUIContent("Transition Start Time",
                        "Start time of the transition between skybox " + (index + 1) + " and skybox " + ((index + 1) % lightingScript.skyboxesList.Count + 1) +
                        " as a 24-hour float"), lightingScript.skyboxesList[index].transitionStartTime, minTime, maxTime);
                        lightingScript.skyboxesList[index].transitionEndTime = EditorGUILayout.Slider(new GUIContent("Transition End Time",
                        "End time of the transition between skybox " + (index + 1) + " and skybox " + ((index + 1) % lightingScript.skyboxesList.Count + 1) +
                        " as a 24-hour float"), lightingScript.skyboxesList[index].transitionEndTime, minTime, maxTime);
                        if (lightingScript.skyboxesList[index].transitionEndTime < lightingScript.skyboxesList[index].transitionStartTime)
                        {
                            lightingScript.skyboxesList[index].transitionEndTime = lightingScript.skyboxesList[index].transitionStartTime;
                            isSceneSaveRequired = true;
                        }
                        if (EditorGUI.EndChangeCheck()) { isSceneSaveRequired = true; }

                        // Check for the Removal of a skybox at the end of the for(;;) loop
                        if (GUILayout.Button("Remove", GUILayout.Width(60f)))
                        {
                            // Remove the item from the list
                            lightingScript.skyboxesList.RemoveAt(index);
                            index--;
                            isSceneSaveRequired = true;
                        }
                    }
                }
            }
            EditorGUILayout.EndVertical();
            #endregion

            // GENERAL LIGHTING SETTINGS
            #region GENERAL LIGHTING SETTINGS
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("General Lighting Settings", EditorStyles.boldLabel);
            if (lightingScript.showGeneralLightingSettings) { if (GUILayout.Button("Hide", GUILayout.Width(50f))) { lightingScript.showGeneralLightingSettings = false; isSceneSaveRequired = true; } }
            else { if (GUILayout.Button("Show", GUILayout.Width(50f))) { lightingScript.showGeneralLightingSettings = true; isSceneSaveRequired = true; } }
            EditorGUILayout.EndHorizontal();
            if (lightingScript.showGeneralLightingSettings)
            {
                serializedObject.Update();
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.PropertyField(lightingModeProp, lightModeContent);
                EditorGUILayout.PropertyField(sunProp, sunContent);

                //lightingScript.lightingMode = (LBLighting.LightingMode)EditorGUILayout.EnumPopup("Lighting Mode", lightingScript.lightingMode);
                //lightingScript.sun = (Light)EditorGUILayout.ObjectField(sunContent, lightingScript.sun, typeof(Light), true);
                if (lightingScript.setupMode == LBLighting.SetupMode.Simple)
                {
                    // Simple setup mode - script takes care of fading in and out sun intensity at sunrise and sunset
                    EditorGUILayout.PropertyField(sunIntensityProp, sunIntensityContent);
                }
                else
                {
                    // Advanced setup mode - gives user full control over the sun intensity at any time of the day
                    EditorGUILayout.PropertyField(sunIntensityProp, maxSunIntensityContent);
                    EditorGUILayout.PropertyField(sunIntensityCurveProp, sunIntensityCurveContent);
                }
                EditorGUILayout.PropertyField(yAxisRotationProp, sunOrbitRotationContent);

                bool prevIsHDREnabled = isHDREnabledProp.boolValue;

                if (lightingScript.lightingMode == LBLighting.LightingMode.Static)
                {
                    EditorGUILayout.PropertyField(startTimeOfDayProp, timeOfDayContent);
                    EditorGUILayout.PropertyField(sunriseTimeProp, sunriseTimeContent);
                    EditorGUILayout.PropertyField(sunsetTimeProp, sunsetTimeContent);
                    if (sunsetTimeProp.floatValue < sunriseTimeProp.floatValue)
                    {
                        sunsetTimeProp.floatValue = sunriseTimeProp.floatValue;
                    }
                    EditorGUILayout.PropertyField(isHDREnabledProp, isHDREnabledContent);
                }
                else if (lightingScript.lightingMode == LBLighting.LightingMode.Dynamic)
                {
                    lightingScript.dayLength = EditorGUILayout.FloatField(dayLengthContent, lightingScript.dayLength);

                    EditorGUILayout.PropertyField(startTimeOfDayProp, startTimeOfDayContent);
                    EditorGUILayout.PropertyField(sunriseTimeProp, sunriseTimeContent);
                    EditorGUILayout.PropertyField(sunsetTimeProp, sunsetTimeContent);
                    if (sunsetTimeProp.floatValue < sunriseTimeProp.floatValue)
                    {
                        sunsetTimeProp.floatValue = sunriseTimeProp.floatValue;
                    }
                    EditorGUILayout.PropertyField(isHDREnabledProp, isHDREnabledContent);
                    EditorGUILayout.PropertyField(realtimeTerrainLightingProp, realtimeTerrainLightingContent);
                    lightingUpdateIntervalProp.floatValue = EditorGUILayout.Slider(lightingUpdateIntervalContent, lightingUpdateIntervalProp.floatValue, 0.1f, 30f);
                }

                // Has user toggled High Dynamic Range (HDR)
                if (prevIsHDREnabled != isHDREnabledProp.boolValue)
                {
                    // Copy values
                    if (isHDREnabledProp.boolValue)
                    {
                        dayAmbientLightHDRProp.colorValue = dayAmbientLightProp.colorValue;
                        nightAmbientLightHDRProp.colorValue = nightAmbientLightProp.colorValue;
                        dayAmbientGndLightHDRProp.colorValue = dayAmbientGndLightProp.colorValue;
                        nightAmbientGndLightHDRProp.colorValue = nightAmbientGndLightProp.colorValue;
                        dayAmbientHznLightHDRProp.colorValue = dayAmbientHznLightProp.colorValue;
                        nightAmbientHznLightHDRProp.colorValue = nightAmbientHznLightProp.colorValue;
                        cloudsUpperColourDayHDRProp.colorValue = cloudsUpperColourDayProp.colorValue;
                        cloudsUpperColourNightHDRProp.colorValue = cloudsUpperColourNightProp.colorValue;
                        cloudsLowerColourDayHDRProp.colorValue = cloudsLowerColourDayProp.colorValue;
                        cloudsLowerColourNightHDRProp.colorValue = cloudsLowerColourNightProp.colorValue;
                    }
                    else
                    {
                        dayAmbientLightProp.colorValue = LBLighting.ConvertColourFromHDR(dayAmbientLightHDRProp.colorValue);
                        nightAmbientLightProp.colorValue = LBLighting.ConvertColourFromHDR(nightAmbientLightHDRProp.colorValue);
                        dayAmbientGndLightProp.colorValue = LBLighting.ConvertColourFromHDR(dayAmbientGndLightHDRProp.colorValue);
                        nightAmbientGndLightProp.colorValue = LBLighting.ConvertColourFromHDR(nightAmbientGndLightHDRProp.colorValue);
                        dayAmbientHznLightProp.colorValue = LBLighting.ConvertColourFromHDR(dayAmbientHznLightHDRProp.colorValue);
                        nightAmbientHznLightProp.colorValue = LBLighting.ConvertColourFromHDR(nightAmbientHznLightHDRProp.colorValue);
                        cloudsUpperColourDayProp.colorValue = LBLighting.ConvertColourFromHDR(cloudsUpperColourDayHDRProp.colorValue);
                        cloudsUpperColourNightProp.colorValue = LBLighting.ConvertColourFromHDR(cloudsUpperColourNightHDRProp.colorValue);
                        cloudsLowerColourDayProp.colorValue = LBLighting.ConvertColourFromHDR(cloudsLowerColourDayHDRProp.colorValue);
                        cloudsLowerColourNightProp.colorValue = LBLighting.ConvertColourFromHDR(cloudsLowerColourNightHDRProp.colorValue);
                    }
                }

                EditorGUILayout.PropertyField(reflProbesUpdateModeProp, reflProbeUpdateModeContent);

                if (EditorGUI.EndChangeCheck()) { isSceneSaveRequired = true; serializedObject.ApplyModifiedProperties(); }

                // Reset to factory defaults
                if (GUILayout.Button(resetGeneralSettingsContent, GUILayout.Width(60f)))
                {
                    serializedObject.Update();
                    // Simple
                    sunIntensityProp.floatValue = lightingScript.GetDefaultSunIntensity;

                    // Advanced
                    sunIntensityCurveProp.animationCurveValue = lightingScript.GetDefaultSunIntensityCurve;

                    // Static
                    yAxisRotationProp.floatValue = lightingScript.GetDefaultYAxisRotation;
                    startTimeOfDayProp.floatValue = lightingScript.GetDefaultStartTimeOfDay;
                    sunriseTimeProp.floatValue = lightingScript.GetDefaultSunriseTime;
                    sunsetTimeProp.floatValue = lightingScript.GetDefaultSunsetTime;

                    // Dynamic extras
                    dayLengthProp.floatValue = lightingScript.GetDefaultDayLength;
                    realtimeTerrainLightingProp.boolValue = lightingScript.GetDefaultRealtimeTerrainLighting;
                    lightingUpdateIntervalProp.floatValue = lightingScript.GetDefaultLightingUpdateInterval;
                    serializedObject.ApplyModifiedProperties();

                    lightingScript.reflProbesUpdateMode = lightingScript.GetDefaultReflProbesUpdateMode;

                    isSceneSaveRequired = true;
                }
            }
            EditorGUILayout.EndVertical();
            #endregion

            // AMBIENT LIGHT AND FOG SETTINGS
            #region AMBIENT LIGHT AND FOG SETTINGS
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Ambient Light And Fog Settings", EditorStyles.boldLabel);
            if (lightingScript.showAmbientAndFogSettings) { if (GUILayout.Button("Hide", GUILayout.Width(50f))) { lightingScript.showAmbientAndFogSettings = false; isSceneSaveRequired = true; } }
            else { if (GUILayout.Button("Show", GUILayout.Width(50f))) { lightingScript.showAmbientAndFogSettings = true; isSceneSaveRequired = true; } }
            EditorGUILayout.EndHorizontal();
            if (lightingScript.showAmbientAndFogSettings)
            {
                // Don't use a propertyfield here as undo may not be what we want
                lightingScript.envAmbientSource = (LBLighting.EnvAmbientSource)EditorGUILayout.EnumPopup(envAmbientSourceContent, lightingScript.envAmbientSource);

                #region Ambient and Fog Simple
                if (lightingScript.setupMode == LBLighting.SetupMode.Simple)
                {
                    EditorGUI.BeginChangeCheck();
                    // Simple setup just linearly interpolates between two values
                    string startSRLerpTime = CurrentTimeString12Hr15Min(lightingScript.sunriseTime - 2f), endSRLerpTime = CurrentTimeString12Hr15Min(lightingScript.sunriseTime + 2f);
                    string startSSLerpTime = CurrentTimeString12Hr15Min(lightingScript.sunsetTime - 2f), endSSLerpTime = CurrentTimeString12Hr15Min(lightingScript.sunsetTime + 2f);
                    string startSRFogLerpTime = CurrentTimeString12Hr15Min(lightingScript.sunriseTime - 1f), endSRFogLerpTime = CurrentTimeString12Hr15Min(lightingScript.sunriseTime + 1f);
                    string startSSFogLerpTime = CurrentTimeString12Hr15Min(lightingScript.sunsetTime - 1f), endSSFogLerpTime = CurrentTimeString12Hr15Min(lightingScript.sunsetTime + 1f);

                    serializedObject.Update();

                    if (lightingScript.envAmbientSource == LBLighting.EnvAmbientSource.Colour)
                    {
                        dayAmbientLightContent.tooltip = "Ambient light colour from " + endSRLerpTime + " to " + startSSLerpTime;
                        nightAmbientLightContent.tooltip = "Ambient light colour from " + endSSLerpTime + " to " + startSRLerpTime;

                        if (isHDREnabledProp.boolValue)
                        {
                            EditorGUILayout.PropertyField(dayAmbientLightHDRProp, dayAmbientLightContent);
                            EditorGUILayout.PropertyField(nightAmbientLightHDRProp, nightAmbientLightContent);
                        }
                        else
                        {
                            EditorGUILayout.PropertyField(dayAmbientLightProp, dayAmbientLightContent);
                            EditorGUILayout.PropertyField(nightAmbientLightProp, nightAmbientLightContent);
                        }
                    }
                    else // Gradient Environment lighting (Sky, Ground, Horizon)
                    {
                        dayAmbientLightContent.tooltip = "Ambient light sky colour from " + endSRLerpTime + " to " + startSSLerpTime;
                        nightAmbientLightContent.tooltip = "Ambient light sky colour from " + endSSLerpTime + " to " + startSRLerpTime;
                        dayAmbientGndLightContent.tooltip = "Ambient light ground colour from " + endSRLerpTime + " to " + startSSLerpTime;
                        nightAmbientGndLightContent.tooltip = "Ambient light ground colour from " + endSSLerpTime + " to " + startSRLerpTime;
                        dayAmbientHznLightContent.tooltip = "Ambient light horizon colour from " + endSRLerpTime + " to " + startSSLerpTime;
                        nightAmbientHznLightContent.tooltip = "Ambient light horizon colour from " + endSSLerpTime + " to " + startSRLerpTime;

                        if (isHDREnabledProp.boolValue)
                        {
                            EditorGUILayout.LabelField("Sky", EditorStyles.boldLabel);
                            EditorGUILayout.PropertyField(dayAmbientLightHDRProp, dayAmbientLightContent);
                            EditorGUILayout.PropertyField(nightAmbientLightHDRProp, nightAmbientLightContent);
                            EditorGUILayout.LabelField("Horizon", EditorStyles.boldLabel);
                            EditorGUILayout.PropertyField(dayAmbientHznLightHDRProp, dayAmbientHznLightContent);
                            EditorGUILayout.PropertyField(nightAmbientHznLightHDRProp, nightAmbientHznLightContent);
                            EditorGUILayout.LabelField("Ground", EditorStyles.boldLabel);
                            EditorGUILayout.PropertyField(dayAmbientGndLightHDRProp, dayAmbientGndLightContent);
                            EditorGUILayout.PropertyField(nightAmbientGndLightHDRProp, nightAmbientGndLightContent);
                        }
                        else
                        {
                            EditorGUILayout.LabelField("Sky", EditorStyles.boldLabel);
                            EditorGUILayout.PropertyField(dayAmbientLightProp, dayAmbientLightContent);
                            EditorGUILayout.PropertyField(nightAmbientLightProp, nightAmbientLightContent);
                            EditorGUILayout.LabelField("Horizon", EditorStyles.boldLabel);
                            EditorGUILayout.PropertyField(dayAmbientHznLightProp, dayAmbientHznLightContent);
                            EditorGUILayout.PropertyField(nightAmbientHznLightProp, nightAmbientHznLightContent);
                            EditorGUILayout.LabelField("Ground", EditorStyles.boldLabel);
                            EditorGUILayout.PropertyField(dayAmbientGndLightProp, dayAmbientGndLightContent);
                            EditorGUILayout.PropertyField(nightAmbientGndLightProp, nightAmbientGndLightContent);
                        }
                        EditorGUILayout.LabelField(" ", GUILayout.MaxHeight(5f));
                    }

                    dayFogColourContent.tooltip = "Fog colour from " + endSRFogLerpTime + " to " + startSSFogLerpTime;
                    nightFogColourContent.tooltip = "Fog colour from " + endSSFogLerpTime + " to " + startSRFogLerpTime;
                    dayFogDensityContent.tooltip = "Fog density from " + endSRLerpTime + " to " + startSSLerpTime;
                    nightFogDensityContent.tooltip = "Fog density from " + endSSLerpTime + " to " + startSRLerpTime;

                    EditorGUILayout.PropertyField(dayFogColourProp, dayFogColourContent);
                    EditorGUILayout.PropertyField(nightFogColourProp, nightFogColourContent);
                    EditorGUILayout.PropertyField(dayFogDensityProp, dayFogDensityContent);
                    EditorGUILayout.PropertyField(nightFogDensityProp, nightFogDensityContent);

                    if (EditorGUI.EndChangeCheck()) { isSceneSaveRequired = true; }

                    // Reset to factory defaults
                    if (GUILayout.Button(new GUIContent("Reset", "Reset Ambient Light and Fog Settings to factory defaults"), GUILayout.Width(60f)))
                    {
                        dayAmbientLightProp.colorValue = lightingScript.GetDefaultDayAmbientLight;
                        nightAmbientLightProp.colorValue = lightingScript.GetDefaultNightAmbientLight;
                        dayAmbientLightHDRProp.colorValue = lightingScript.GetDefaultDayAmbientLight;
                        nightAmbientLightHDRProp.colorValue = lightingScript.GetDefaultNightAmbientLight;
                        dayAmbientGndLightProp.colorValue = lightingScript.GetDefaultDayAmbientGndLight;
                        nightAmbientGndLightProp.colorValue = lightingScript.GetDefaultNightAmbientGndLight;
                        dayAmbientGndLightHDRProp.colorValue = lightingScript.GetDefaultDayAmbientGndLight;
                        nightAmbientGndLightHDRProp.colorValue = lightingScript.GetDefaultNightAmbientGndLight;
                        dayAmbientHznLightProp.colorValue = lightingScript.GetDefaultDayAmbientHznLight;
                        nightAmbientHznLightProp.colorValue = lightingScript.GetDefaultNightAmbientHznLight;
                        dayAmbientHznLightHDRProp.colorValue = lightingScript.GetDefaultDayAmbientHznLight;
                        nightAmbientHznLightHDRProp.colorValue = lightingScript.GetDefaultNightAmbientHznLight;
                        dayFogColourProp.colorValue = lightingScript.GetDefaultDayFogColour;
                        nightFogColourProp.colorValue = lightingScript.GetDefaultNightFogColour;
                        dayFogDensityProp.floatValue = lightingScript.GetDefaultDayFogDensity;
                        nightFogDensityProp.floatValue = lightingScript.GetDefaultNightFogDensity;
                        lightingScript.envAmbientSource = LBLighting.EnvAmbientSource.Colour;
                        isSceneSaveRequired = true;
                    }

                    serializedObject.ApplyModifiedProperties();
                }
                #endregion

                #region Ambient and Fog Advanced
                else
                {
                    // Advanced setup gets values from user-defined curves and gradients
                    lightingScript.InitialiseGradients();
                    serializedObject.Update();
                    EditorGUI.BeginChangeCheck();

                    if (lightingScript.envAmbientSource == LBLighting.EnvAmbientSource.Colour)
                    {
                        ambientLightGradientContent.text = "Ambient Light Gradient";
                        ambientLightGradientContent.tooltip = "Gradient defining the ambient light colour at each time in the day (time of day is expressed as a 0-1 float)";
                        // Gradients don't have an exposed editorguilayout field editor, so we have to treat them as a property
                        EditorGUILayout.PropertyField(ambientLightGradientProp, ambientLightGradientContent, true, null);
                    }
                    else
                    {
                        ambientLightGradientContent.text = "Ambient Light Sky";
                        ambientLightGradientContent.tooltip = "Gradient defining the ambient light sky colour at each time in the day (time of day is expressed as a 0-1 float)";
                        EditorGUILayout.PropertyField(ambientLightGradientProp, ambientLightGradientContent, true, null);
                        EditorGUILayout.PropertyField(ambientHznLightGradientProp, ambientHznLightGradientContent, true, null);
                        EditorGUILayout.PropertyField(ambientGndLightGradientProp, ambientGndLightGradientContent, true, null);
                    }

                    EditorGUILayout.PropertyField(fogColourGradientProp, fogColourGradientContent, true, null);
                    EditorGUILayout.PropertyField(minFogDensityProp, minFogDensityContent);
                    EditorGUILayout.PropertyField(maxFogDensityProp, maxFogDensityContent);
                    EditorGUILayout.PropertyField(fogDensityCurveProp, fogDensityCurveContent);

                    if (EditorGUI.EndChangeCheck()) { isSceneSaveRequired = true; serializedObject.ApplyModifiedProperties(); }
                    
                    //EditorGUI.BeginChangeCheck();
                    //serializedObj = new SerializedObject(lightingScript);
                    //serializedProp = serializedObj.FindProperty("ambientLightGradient");
                    //EditorGUILayout.PropertyField(serializedProp, ambientLightGradientContent, true, null);
                    //if (EditorGUI.EndChangeCheck()) { serializedObj.ApplyModifiedProperties(); isSceneSaveRequired = true; }
                    //EditorGUI.BeginChangeCheck();
                    //serializedObj = new SerializedObject(lightingScript);
                    //serializedProp = serializedObj.FindProperty("fogColourGradient");
                    //EditorGUILayout.PropertyField(serializedProp, fogColourGradientContent, true, null);
                    //if (EditorGUI.EndChangeCheck()) { serializedObj.ApplyModifiedProperties(); isSceneSaveRequired = true; }

                    // Reset to factory defaults
                    if (GUILayout.Button("Reset", GUILayout.Width(60f)))
                    {
                        //Debug.Log(lightingScript.ambientLightGradient.colorKeys[0].color.ToString());
                        lightingScript.ambientLightGradient = lightingScript.GetDefaultAmbientLightGradient;
                        //Debug.Log(lightingScript.ambientLightGradient.colorKeys[0].color.ToString());
                        lightingScript.ambientHznLightGradient = lightingScript.GetDefaultAmbientHznLightGradient;
                        lightingScript.ambientGndLightGradient = lightingScript.GetDefaultAmbientGndLightGradient;
                        lightingScript.fogColourGradient = lightingScript.GetDefaultFogColourGradient;

                        serializedObject.Update();
                        minFogDensityProp.floatValue = lightingScript.GetDefaultMinFogDensity;
                        maxFogDensityProp.floatValue = lightingScript.GetDefaultMaxFogDensity;
                        fogDensityCurveProp.animationCurveValue = lightingScript.GetDefaultFogDensityCurve;
                        serializedObject.ApplyModifiedProperties();
                        isSceneSaveRequired = true;
                    }
                }
                #endregion
            }
            EditorGUILayout.EndVertical();
            #endregion

            // CELESTIALS SETTINGS
            #region CELESTIALS SETTINGS
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Celestials Settings", EditorStyles.boldLabel);
            if (lightingScript.showCelestialsSettings) { if (GUILayout.Button("Hide", GUILayout.Width(50f))) { lightingScript.showCelestialsSettings = false; isSceneSaveRequired = true; } }
            else { if (GUILayout.Button("Show", GUILayout.Width(50f))) { lightingScript.showCelestialsSettings = true; isSceneSaveRequired = true; } }
            EditorGUILayout.EndHorizontal();
            if (lightingScript.showCelestialsSettings)
            {
                EditorGUI.BeginChangeCheck();
                lightingScript.useCelestials = EditorGUILayout.Toggle(useCelestialsContent, lightingScript.useCelestials);
                if (EditorGUI.EndChangeCheck()) { isSceneSaveRequired = true; }
                if (lightingScript.useCelestials)
                {
                    if (lightingScript.celestials == null)
                    {
                        lightingScript.AddCelestials();
                        isSceneSaveRequired = true;
                    }
                    EditorGUI.BeginChangeCheck();
                    lightingScript.mainCamera = (Camera)EditorGUILayout.ObjectField(mainCameraContent, lightingScript.mainCamera, typeof(Camera), true);
                    if (lightingScript.mainCamera == null) { lightingScript.mainCamera = Camera.main; }
                    lightingScript.numberOfStars = EditorGUILayout.IntSlider(numStarsContent, lightingScript.numberOfStars, 100, 1500);
                    lightingScript.starSize = EditorGUILayout.Slider(starSizeContent, lightingScript.starSize, 0.1f, 10f);
                    if (lightingScript.setupMode == LBLighting.SetupMode.Advanced)
                    {
                        lightingScript.starVisibilityCurve = EditorGUILayout.CurveField(starVisibilityCurveContent, lightingScript.starVisibilityCurve);
                    }

                    lightingScript.useMoon = EditorGUILayout.Toggle(useMoonContent, lightingScript.useMoon);
                    if (EditorGUI.EndChangeCheck()) { isSceneSaveRequired = true; }
                    if (lightingScript.useMoon)
                    {
                        // Add a moon light object if it doesn't already exist
                        if (lightingScript.moon == null)
                        {
                            lightingScript.AddMoonLight();
                            isSceneSaveRequired = true;
                        }
                        EditorGUI.BeginChangeCheck();
                        if (lightingScript.setupMode == LBLighting.SetupMode.Simple)
                        {
                            // Simple setup mode - script takes care of fading in and out moon intensity at sunrise and sunset
                            lightingScript.moonIntensity = EditorGUILayout.Slider(moonIntensityContent, lightingScript.moonIntensity, 0f, 8f);
                        }
                        else
                        {
                            // Advanced setup mode - gives user full control over the moon intensity at any time of the day
                            lightingScript.moonIntensity = EditorGUILayout.Slider(maxMoonIntensityContent, lightingScript.moonIntensity, 0f, 8f);
                            lightingScript.moonIntensityCurve = EditorGUILayout.CurveField(moonIntensityCurveContent, lightingScript.moonIntensityCurve);
                        }
                        lightingScript.moonYAxisRotation = EditorGUILayout.Slider(moonOrbitRotationContent, lightingScript.moonYAxisRotation, 0f, 360f);
                        lightingScript.moonSize = EditorGUILayout.Slider(moonSizeContent, lightingScript.moonSize, 0.1f, 10f);
                        if (EditorGUI.EndChangeCheck()) { isSceneSaveRequired = true; }
                    }
                    else
                    {
                        // Destroy the moon light object if one exists
                        if (lightingScript.moon != null)
                        {
                            DestroyImmediate(lightingScript.moon.gameObject);
                            lightingScript.moon = null;
                            isSceneSaveRequired = true;
                        }
                    }
                }
                else
                {
                    if (lightingScript.celestials != null)
                    {
                        DestroyImmediate(lightingScript.celestials.gameObject);
                        lightingScript.celestials = null;
                        if (lightingScript.mainCamera != null)
                        {
                            lightingScript.mainCamera.cullingMask = -1;
                            lightingScript.mainCamera.clearFlags = CameraClearFlags.Skybox;
                        }
                        isSceneSaveRequired = true;
                    }
                    EditorGUI.BeginChangeCheck();
                    // Allow users to use the moon light object even with celestials turned off
                    lightingScript.useMoon = EditorGUILayout.Toggle(useMoonContent, lightingScript.useMoon);
                    if (lightingScript.useMoon)
                    {
                        // Add a moon light object if it doesn't already exist
                        if (lightingScript.moon == null)
                        {
                            lightingScript.AddMoonLight();
                            isSceneSaveRequired = true;
                        }
                        if (lightingScript.setupMode == LBLighting.SetupMode.Simple)
                        {
                            // Simple setup mode - script takes care of fading in and out moon intensity at sunrise and sunset
                            lightingScript.moonIntensity = EditorGUILayout.Slider(moonIntensityContent, lightingScript.moonIntensity, 0f, 8f);
                        }
                        else
                        {
                            // Advanced setup mode - gives user full control over the moon intensity at any time of the day
                            lightingScript.moonIntensity = EditorGUILayout.Slider(maxMoonIntensityContent, lightingScript.moonIntensity, 0f, 8f);
                            lightingScript.moonIntensityCurve = EditorGUILayout.CurveField(moonIntensityCurveContent, lightingScript.moonIntensityCurve);
                        }
                        lightingScript.moonYAxisRotation = EditorGUILayout.Slider(moonOrbitRotationContent, lightingScript.moonYAxisRotation, 0f, 360f);
                    }
                    else
                    {
                        // Destroy the moon light object if one exists
                        if (lightingScript.moon != null)
                        {
                            DestroyImmediate(lightingScript.moon.gameObject);
                            lightingScript.moon = null;
                            isSceneSaveRequired = true;
                        }
                    }
                    if (EditorGUI.EndChangeCheck()) { isSceneSaveRequired = true; }
                }

                GUILayout.BeginHorizontal();
                // Reset to Celestials factory to defaults
                if (GUILayout.Button(new GUIContent("Reset", "Reset Celestials Settings to factory defaults"), GUILayout.Width(60f)))
                {
                    lightingScript.numberOfStars = lightingScript.GetDefaultNumberOfStars;
                    lightingScript.starSize = lightingScript.GetDefaultStarSize;
                    lightingScript.starVisibilityCurve = lightingScript.GetDefaultStarVisibilityCurve;
                    lightingScript.useMoon = lightingScript.GetDefaultUseMoon;
                    lightingScript.moonIntensity = lightingScript.GetDefaultMoonIntensity;
                    lightingScript.moonIntensityCurve = lightingScript.GetDefaultMoonIntensityCurve;
                    lightingScript.moonYAxisRotation = lightingScript.GetDefaultMoonYAxisRotation;
                    lightingScript.moonSize = lightingScript.GetDefaultMoonSize;
                    isSceneSaveRequired = true;
                }

                if (lightingScript.useCelestials)
                {
                    if (GUILayout.Button("Build Celestials"))
                    {
                        lightingScript.celestials.BuildCelestrials(lightingScript);
                        //if (lightingScript.celestials.lighting == null) { lightingScript.celestials.Initialise(lightingScript); }
                        //lightingScript.celestials.DestroyStarGameObject();
                        //lightingScript.celestials.DestroyMoonGameObject();
                        //lightingScript.celestials.CreateStars(lightingScript.numberOfStars, lightingScript.starSize);
                        //lightingScript.celestials.CreateMoon(lightingScript.moonSize);
                        isSceneSaveRequired = true;
                    }
                }
                GUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
            #endregion

            // WEATHER SETTINGS
            #region WEATHER SETTINGS
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Weather Settings", EditorStyles.boldLabel);
            if (lightingScript.showWeatherSettings) { if (GUILayout.Button("Hide", GUILayout.Width(50f))) { lightingScript.showWeatherSettings = false; isSceneSaveRequired = true; } }
            else { if (GUILayout.Button("Show", GUILayout.Width(50f))) { lightingScript.showWeatherSettings = true; isSceneSaveRequired = true; } }
            EditorGUILayout.EndHorizontal();
            if (lightingScript.showWeatherSettings)
            {
                #region Use Weather
                EditorGUI.BeginChangeCheck();
                lightingScript.useWeather = EditorGUILayout.Toggle(useWeatherContent, lightingScript.useWeather);
                if (EditorGUI.EndChangeCheck())
                {
                    // Automatically add cameras in the scene so that user has something to start with
                    if (lightingScript.useWeather) { lightingScript.RefreshWeatherCameraList(true, false); }
                    // Turn off Use Weather
                    else if (lightingScript.weatherCameraList != null)
                    {
                        // Remove ImageFX from cameras in list
                        // Loop backwards through the list and remove the ImageFX scripts
                        for (index = lightingScript.weatherCameraList.Count - 1; index > -1; index--)
                        {
                            // Remove the ImageFX and rain/hail/snow particle gameobjects from the camera at the end of the list.
                            lightingScript.RemoveWeatherFromCamera(lightingScript.weatherCameraList[lightingScript.weatherCameraList.Count - 1], true);
                            lightingScript.RemoveAllParticlesFromCamera(lightingScript.weatherCameraList[lightingScript.weatherCameraList.Count - 1], true);
                            // Remove the Camera from the end of the list.
                            lightingScript.weatherCameraList.RemoveAt(lightingScript.weatherCameraList.Count - 1);
                        }
                    }
                    isSceneSaveRequired = true;
                }
                #endregion

                if (lightingScript.useWeather)
                {
                    #region Weather Cameras

                    EditorGUI.BeginChangeCheck();
                    GUILayout.BeginHorizontal();
                    // Indent the Foldout control
                    GUILayout.Space(12f);
                    lightingScript.showWeatherCameraSettings = EditorGUILayout.Foldout(lightingScript.showWeatherCameraSettings, "ImageFX Cameras");
                    EditorGUILayout.EndHorizontal();
                    if (EditorGUI.EndChangeCheck()) { isSceneSaveRequired = true; }

                    if (lightingScript.showWeatherCameraSettings)
                    {
                        // Display a list of the camera that use the ImageFX script
                        EditorGUILayout.BeginHorizontal();
                        if (GUILayout.Button("Get Cameras", GUILayout.MaxWidth(100f)))
                        {
                            lightingScript.RefreshWeatherCameraList(true, false);
                            isSceneSaveRequired = true;
                        }
                        if (GUILayout.Button("+", GUILayout.Width(25f)))
                        {
                            if (lightingScript.weatherCameraList == null) { lightingScript.weatherCameraList = new List<Camera>(); }
                            lightingScript.weatherCameraList.Add(null);
                            isSceneSaveRequired = true;
                        }

                        if (GUILayout.Button("-", GUILayout.Width(25f)))
                        {
                            if (lightingScript.weatherCameraList == null) { lightingScript.weatherCameraList = new List<Camera>(); }
                            if (lightingScript.weatherCameraList.Count > 0)
                            {
                                lightingScript.RemoveWeatherFromCamera(lightingScript.weatherCameraList[lightingScript.weatherCameraList.Count - 1], true);
                                lightingScript.RemoveAllParticlesFromCamera(lightingScript.weatherCameraList[lightingScript.weatherCameraList.Count - 1], true);
                                lightingScript.weatherCameraList.RemoveAt(lightingScript.weatherCameraList.Count - 1);
                                isSceneSaveRequired = true;
                            }
                        }
                        EditorGUILayout.EndHorizontal();

                        // Display list of cameras that will have the ImageFX component attached and updated
                        if (lightingScript.weatherCameraList != null)
                        {
                            cameraToRemove = null;
                            cameraToRemoveIdx = -1;
                            for (index = 0; index < lightingScript.weatherCameraList.Count; index++)
                            {
                                EditorGUILayout.BeginHorizontal();
                                if (GUILayout.Button("X", buttonCompact, GUILayout.MaxWidth(20f))) { cameraToRemove = lightingScript.weatherCameraList[index]; cameraToRemoveIdx = index; }
                                currentCamera = lightingScript.weatherCameraList[index];
                                EditorGUI.BeginChangeCheck();
                                lightingScript.weatherCameraList[index] = (Camera)EditorGUILayout.ObjectField(currentCamera, typeof(Camera), true);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    // Remove ImageFX and rain/hail/snow particle gameobjects from original camera if it isn't in another slot
                                    if (lightingScript.weatherCameraList.FindIndex(c => c == currentCamera) < 0)
                                    {
                                        lightingScript.RemoveWeatherFromCamera(currentCamera, false);
                                        lightingScript.RemoveAllParticlesFromCamera(currentCamera, false);
                                    }
                                    // Add ImageFX and rain particle system to the newly selected camera
                                    if (lightingScript.weatherCameraList[index] != null)
                                    {
                                        lightingScript.AddWeatherToCamera(lightingScript.weatherCameraList[index], false);
                                        lightingScript.AddParticlesToCamera(lightingScript.weatherCameraList[index], LBLighting.PrecipitationType.Rain, false);
                                        lightingScript.AddParticlesToCamera(lightingScript.weatherCameraList[index], LBLighting.PrecipitationType.Hail, false);
                                    }
                                    isSceneSaveRequired = true;
                                }
                                EditorGUILayout.EndHorizontal();
                            }

                            // Does the user wish to remove a camera from the list?
                            if (cameraToRemoveIdx >= 0)
                            {
                                if (cameraToRemove != null)
                                {
                                    lightingScript.RemoveWeatherFromCamera(cameraToRemove, true);
                                    lightingScript.RemoveAllParticlesFromCamera(cameraToRemove, true);
                                    lightingScript.weatherCameraList.Remove(cameraToRemove);
                                }
                                else
                                {
                                    // If there is no camera, just remove the item from this list
                                    lightingScript.weatherCameraList.RemoveAt(cameraToRemoveIdx);
                                }
                                isSceneSaveRequired = true;
                            }
                        }
                    }

                    #endregion

                    #region Weather Clouds
                    EditorGUI.BeginChangeCheck();
                    lightingScript.useUnityDistanceFog = EditorGUILayout.Toggle(useUnityDistanceFogContent, lightingScript.useUnityDistanceFog);

                    GUILayout.BeginHorizontal();
                    // Indent the Foldout control
                    GUILayout.Space(12f);
                    lightingScript.showWeatherCloudSettings = EditorGUILayout.Foldout(lightingScript.showWeatherCloudSettings, "Cloud Settings");
                    EditorGUILayout.EndHorizontal();
                    if (lightingScript.showWeatherCloudSettings)
                    {
                        lightingScript.useClouds = EditorGUILayout.Toggle(useCloudsContent, lightingScript.useClouds);

                        #region Use Clouds
                        if (lightingScript.useClouds)
                        {
                            serializedObject.Update();

                            EditorGUILayout.PropertyField(use3DNoiseProp, use3DNoiseContent);
                            EditorGUILayout.PropertyField(cloudStyleProp, cloudStyleContent);
                            EditorGUILayout.PropertyField(cloudsQualityLevelProp, cloudsQualityLevelContent);
                            EditorGUILayout.PropertyField(cloudsDetailAmountProp, cloudsDetailAmountContent);

                            if (isHDREnabledProp.boolValue)
                            {
                                EditorGUILayout.PropertyField(cloudsUpperColourDayHDRProp, cloudsUpperColourDayContent);
                                EditorGUILayout.PropertyField(cloudsUpperColourNightHDRProp, cloudsUpperColourNightContent);
                                EditorGUILayout.PropertyField(cloudsLowerColourDayHDRProp, cloudsLowerColourDayContent);
                                EditorGUILayout.PropertyField(cloudsLowerColourNightHDRProp, cloudsLowerColourNightContent);
                            }
                            else
                            {
                                EditorGUILayout.PropertyField(cloudsUpperColourDayProp, cloudsUpperColourDayContent);
                                EditorGUILayout.PropertyField(cloudsUpperColourNightProp, cloudsUpperColourNightContent);
                                EditorGUILayout.PropertyField(cloudsLowerColourDayProp, cloudsLowerColourDayContent);
                                EditorGUILayout.PropertyField(cloudsLowerColourNightProp, cloudsLowerColourNightContent);
                            }
                            EditorGUILayout.PropertyField(useCloudShadowsProp, useCloudShadowsContent);

                            serializedObject.ApplyModifiedProperties();
                        }
                        #endregion

                        #region Reset weather cloud settings defaults
                        if (GUILayout.Button(new GUIContent("Reset", "Reset Cloud Settings to factory defaults"), GUILayout.Width(60f)))
                        {
                            lightingScript.use3DNoise = lightingScript.GetDefaultUse3DNoise;
                            lightingScript.isHDREnabled = false;
                            lightingScript.cloudStyle = lightingScript.GetDefaultCloudStyle;
                            lightingScript.cloudsQualityLevel = lightingScript.GetDefaultCloudsQualityLevel;
                            lightingScript.cloudsDetailAmount = lightingScript.GetDefaultCloudsDetailAmount;
                            lightingScript.useCloudShadows = lightingScript.GetDefaultCloudShadows;
                            lightingScript.cloudsUpperColourDay = lightingScript.GetDefaultCloudsUpperColourDay;
                            lightingScript.cloudsLowerColourDay = lightingScript.GetDefaultCloudsLowerColourDay;
                            lightingScript.cloudsUpperColourNight = lightingScript.GetDefaultCloudsUpperColourNight;
                            lightingScript.cloudsLowerColourNight = lightingScript.GetDefaultCloudsLowerColourNight;
                            lightingScript.cloudsUpperColourDayHDR = lightingScript.GetDefaultCloudsUpperColourDay;
                            lightingScript.cloudsLowerColourDayHDR = lightingScript.GetDefaultCloudsLowerColourDay;
                            lightingScript.cloudsUpperColourNightHDR = lightingScript.GetDefaultCloudsUpperColourNight;
                            lightingScript.cloudsLowerColourNightHDR = lightingScript.GetDefaultCloudsLowerColourNight;
                        }
                        #endregion
                    }
                    #endregion

                    #region Weather States
                    GUILayout.BeginHorizontal();
                    // Indent the Foldout control
                    GUILayout.Space(12f);
                    lightingScript.showWeatherStateSettings = EditorGUILayout.Foldout(lightingScript.showWeatherStateSettings, "State Settings");
                    EditorGUILayout.EndHorizontal();
                    if (lightingScript.showWeatherStateSettings)
                    {
                        lightingScript.ValidateWeatherStates();

                        #region Weather state overall settings
                        // Populate a list of ints and strings so that we can create a drop-down menu for the starting weather state
                        List<int> wsInts = new List<int>();
                        List<string> wsStrings = new List<string>();
                        for (int ws = 0; ws < lightingScript.weatherStatesList.Count; ws++)
                        {
                            wsInts.Add(ws);
                            wsStrings.Add(lightingScript.weatherStatesList[ws].name);
                        }
                        wsInts.Add(lightingScript.weatherStatesList.Count);
                        wsStrings.Add("Random");
                        lightingScript.startWeatherState = EditorGUILayout.IntPopup("Start Weather State", lightingScript.startWeatherState, wsStrings.ToArray(), wsInts.ToArray());

                        lightingScript.allowAutomaticTransitions = EditorGUILayout.Toggle(new GUIContent("Automatic Transitions", "If enabled, the weather will transition between states automatically. Whether this is enabled or not it is always possible to trigger state transitions from script."), lightingScript.allowAutomaticTransitions);
                        if (lightingScript.allowAutomaticTransitions)
                        {
                            lightingScript.minWeatherTransitionDuration = EditorGUILayout.FloatField(new GUIContent("Min Transition Duration", "The minimum duration of any transition (in seconds)"), lightingScript.minWeatherTransitionDuration);
                            lightingScript.maxWeatherTransitionDuration = EditorGUILayout.FloatField(new GUIContent("Max Transition Duration", "The maximum duration of any transition (in seconds)"), lightingScript.maxWeatherTransitionDuration);
                            if (lightingScript.minWeatherTransitionDuration > lightingScript.maxWeatherTransitionDuration)
                            {
                                lightingScript.maxWeatherTransitionDuration = lightingScript.minWeatherTransitionDuration;
                            }
                        }

                        lightingScript.randomiseWindDirection = EditorGUILayout.Toggle(new GUIContent("Randomise Wind Direction", "Whether wind direction can change when the weather state changes"), lightingScript.randomiseWindDirection);
                        lightingScript.useWindZone = EditorGUILayout.Toggle(new GUIContent("Use Wind Zone", "Whether a specified wind zone is controlled by the weather state"), lightingScript.useWindZone);
                        if (lightingScript.useWindZone)
                        {
                            lightingScript.weatherWindZone = (WindZone)EditorGUILayout.ObjectField(new GUIContent("Wind Zone", "The wind zone that will be controlled by the weather state"), lightingScript.weatherWindZone, typeof(WindZone), true);
                        }
                        #endregion

                        #region Reset weather cloud settings defaults
                        if (GUILayout.Button(new GUIContent("Reset", "Reset State Settings to factory defaults"), GUILayout.Width(60f)))
                        {
                            lightingScript.startWeatherState = lightingScript.GetDefaultStartWeatherState;
                            lightingScript.minWeatherTransitionDuration = lightingScript.GetDefaultMinWeatherTransitionDuration;
                            lightingScript.maxWeatherTransitionDuration = lightingScript.GetDefaultMaxWeatherTransitionDuration;
                            lightingScript.allowAutomaticTransitions = lightingScript.GetDefaultAllowAutomaticTransitions;
                            lightingScript.randomiseWindDirection = lightingScript.GetDefaultRandomiseWindDirection;
                            lightingScript.useWindZone = lightingScript.GetDefaultUseWindZone;
                            lightingScript.weatherWindZone = null;
                        }
                        #endregion

                        #region Add weather state
                        arrayInt = lightingScript.weatherStatesList.Count;
                        if (GUILayout.Button("Add Weather State")) { arrayInt++; }
                        if (arrayInt < 0) { arrayInt = 0; }
                        // Add items to the list
                        if (arrayInt > lightingScript.weatherStatesList.Count)
                        {
                            temp = arrayInt - lightingScript.weatherStatesList.Count;
                            for (index = 0; index < temp; index++)
                            {
                                lightingScript.weatherStatesList.Add(new LBWeatherState());
                            }
                            isSceneSaveRequired = true;
                        }
                        #endregion

                        // No WeatherStates to insert/move at the start of the loop
                        insertWeatherStatePos = -1;
                        weatherStateToMove = null;
                        moveWeatherStatePos = -1;

                        #region Display weather states

                        for (index = 0; index < lightingScript.weatherStatesList.Count; index++)
                        {
                            // Show the elements of the list
                            bool displayContents = true;
                            if (index > 0) { EditorGUILayout.Separator(); }
                            GUILayout.BeginHorizontal();
                            labelText = "Weather State " + (index + 1).ToString() + ": " + lightingScript.weatherStatesList[index].name;
                            EditorGUILayout.LabelField(labelText, EditorStyles.boldLabel);

                            if (GUILayout.Button(new GUIContent("V", "Move Weather State down. If this is the last state, make it the first."), buttonCompact, GUILayout.Width(20f)))
                            {
                                weatherStateToMove = new LBWeatherState(lightingScript.weatherStatesList[index]);
                                moveWeatherStatePos = index;
                            }
                            if (GUILayout.Button(new GUIContent("I", "Insert new State above this Weather State"), buttonCompact, GUILayout.Width(20f))) { insertWeatherStatePos = index; }
                            if (GUILayout.Button(new GUIContent("X", "Remove this Weather State"), buttonCompact, GUILayout.MaxWidth(20f)))
                            {
                                labelText += " will be deleted\n\nThis action will remove the Weather State from the list and cannot be undone.";
                                if (EditorUtility.DisplayDialog("Delete Weather State?", labelText, "Delete Now", "Cancel"))
                                {
                                    lightingScript.weatherStatesList.RemoveAt(index);
                                    displayContents = false;
                                    index--;
                                }
                            }
                            EditorGUILayout.EndHorizontal();
                            if (displayContents)
                            {
                                lightingScript.weatherStatesList[index].name = EditorGUILayout.TextField(new GUIContent("Name", "The name of the weather state"), lightingScript.weatherStatesList[index].name);
                                if (lightingScript.allowAutomaticTransitions)
                                {
                                    lightingScript.weatherStatesList[index].probability = EditorGUILayout.Slider(new GUIContent("Probability", "The (proportional) probability of the weather state occuring"), lightingScript.weatherStatesList[index].probability, 0f, 1f);
                                }
                                lightingScript.weatherStatesList[index].wetness = EditorGUILayout.Slider(new GUIContent("Wetness", "The wetness value used while in this weather state"), lightingScript.weatherStatesList[index].wetness, 0f, 1f);
                                if (lightingScript.useClouds)
                                {
                                    lightingScript.weatherStatesList[index].cloudDensity = EditorGUILayout.Slider(new GUIContent("Cloud Density", "The cloud density used in this weather state"), lightingScript.weatherStatesList[index].cloudDensity, 3f, 5f);
                                    lightingScript.weatherStatesList[index].cloudCoverage = EditorGUILayout.Slider(new GUIContent("Cloud Coverage", "The cloud coverage used in this weather state"), lightingScript.weatherStatesList[index].cloudCoverage, 0f, 1f);
                                    if (lightingScript.useCloudShadows)
                                    {
                                        lightingScript.weatherStatesList[index].cloudShadowStrength = EditorGUILayout.Slider(new GUIContent("Cloud Shadow Strength", "The strength of cloud shadows in this weather state"), lightingScript.weatherStatesList[index].cloudShadowStrength, 0.5f, 1f);
                                    }
                                }
                                lightingScript.weatherStatesList[index].rainStrength = EditorGUILayout.Slider(new GUIContent("Rain Strength", "How heavy the rain is while in this weather state"), lightingScript.weatherStatesList[index].rainStrength, 0f, 1f);
                                lightingScript.weatherStatesList[index].hailStrength = EditorGUILayout.Slider(new GUIContent("Hail Strength", "How heavy the hail is while in this weather state"), lightingScript.weatherStatesList[index].hailStrength, 0f, 1f);
                                lightingScript.weatherStatesList[index].fogDensityMultiplier = EditorGUILayout.Slider(new GUIContent("Fog Density Multiplier", "Multiplier for fog density in this weather state - increase this value to increase fog density in this weather state"), lightingScript.weatherStatesList[index].fogDensityMultiplier, 0.01f, 100f);
                                if (lightingScript.use3DNoise)
                                {
                                    lightingScript.weatherStatesList[index].cloudsMorphingSpeed = EditorGUILayout.Slider(new GUIContent("Clouds Morphing Speed", "How fast cloud formations will change over time"), lightingScript.weatherStatesList[index].cloudsMorphingSpeed, 1f, 1000f);
                                }
                                lightingScript.weatherStatesList[index].windStrength = EditorGUILayout.FloatField(new GUIContent("Wind Strength", "How strong the wind is while in this weather state"), lightingScript.weatherStatesList[index].windStrength);
                                if (lightingScript.useWindZone)
                                {
                                    lightingScript.weatherStatesList[index].windZoneMain = EditorGUILayout.FloatField(new GUIContent("Wind Zone Main", "The value of the 'main' property for wind zones used in this weather state"), lightingScript.weatherStatesList[index].windZoneMain);
                                    lightingScript.weatherStatesList[index].windZoneTurbulence = EditorGUILayout.FloatField(new GUIContent("Wind Zone Turbulence", "The value of the 'turbulence' property for wind zones used in this weather state"), lightingScript.weatherStatesList[index].windZoneTurbulence);
                                }
                                if (lightingScript.allowAutomaticTransitions)
                                {
                                    lightingScript.weatherStatesList[index].minStateDuration = EditorGUILayout.FloatField(new GUIContent("Min State Duration", "The minimum duration of this state (in seconds)"), lightingScript.weatherStatesList[index].minStateDuration);
                                    lightingScript.weatherStatesList[index].maxStateDuration = EditorGUILayout.FloatField(new GUIContent("Max State Duration", "The maximum duration of this state (in seconds)"), lightingScript.weatherStatesList[index].maxStateDuration);
                                    if (lightingScript.weatherStatesList[index].minStateDuration > lightingScript.weatherStatesList[index].maxStateDuration)
                                    {
                                        lightingScript.weatherStatesList[index].maxStateDuration = lightingScript.weatherStatesList[index].minStateDuration;
                                    }
                                }
                            }
                        }

                        #endregion

                        #region Insert/Move weather states

                        // Does the user wish to insert a new (duplicate) weather state into the list?
                        if (insertWeatherStatePos >= 0)
                        {
                            // Insert a duplicate above the selected Weather State
                            lightingScript.weatherStatesList.Insert(insertWeatherStatePos, new LBWeatherState(lightingScript.weatherStatesList[insertWeatherStatePos]));
                            lightingScript.weatherStatesList[insertWeatherStatePos].name = "[new] " + lightingScript.weatherStatesList[insertWeatherStatePos].name;
                            isSceneSaveRequired = true;
                        }
                        // Does the user wish to move a Weather State downward in the list (or move from last position to top)?
                        else if (moveWeatherStatePos >= 0 && weatherStateToMove != null)
                        {
                            // Attempt to move this Weather State down one in the list
                            if (lightingScript.weatherStatesList.Count > 1)
                            {
                                // If this is the last in the list we want to put it at the top
                                if (moveWeatherStatePos == lightingScript.weatherStatesList.Count - 1)
                                {
                                    lightingScript.weatherStatesList.Insert(0, weatherStateToMove);
                                    lightingScript.weatherStatesList.RemoveAt(lightingScript.weatherStatesList.Count - 1);
                                }
                                else
                                {
                                    // Move down one in the list
                                    lightingScript.weatherStatesList.RemoveAt(moveWeatherStatePos);
                                    lightingScript.weatherStatesList.Insert(moveWeatherStatePos + 1, weatherStateToMove);
                                }
                                isSceneSaveRequired = true;
                            }
                            weatherStateToMove = null;
                        }
                        #endregion
                    }
                    #endregion

                    if (EditorGUI.EndChangeCheck()) { isSceneSaveRequired = true; }
                }
            }
            EditorGUILayout.EndVertical();
            #endregion

            // ON SCREEN CLOCK SETTINGS
            #region ON SCREEN CLOCK SETTINGS
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("On Screen Clock Settings", EditorStyles.boldLabel);
            if (lightingScript.showClockSettings) { if (GUILayout.Button("Hide", GUILayout.Width(50f))) { lightingScript.showClockSettings = false; isSceneSaveRequired = true; } }
            else { if (GUILayout.Button("Show", GUILayout.Width(50f))) { lightingScript.showClockSettings = true; isSceneSaveRequired = true; } }
            EditorGUILayout.EndHorizontal();
            if (lightingScript.showClockSettings)
            {
                EditorGUI.BeginChangeCheck();
                lightingScript.useScreenClock = EditorGUILayout.Toggle(useScreenClockContent, lightingScript.useScreenClock);

                if (lightingScript.useScreenClock)
                {
                    lightingScript.screenClockTextColour = EditorGUILayout.ColorField(useScreenClockTextColourContent, lightingScript.screenClockTextColour);
                    lightingScript.useScreenClockSeconds = EditorGUILayout.Toggle(useScreenClockSecsContent, lightingScript.useScreenClockSeconds);
                }

                // Reset to screen clock factory defaults
                if (GUILayout.Button(useScreenClockResetContent, GUILayout.Width(60f)))
                {
                    lightingScript.screenClockTextColour = lightingScript.GetDefaultScreenClockTextColour;
                    lightingScript.useScreenClockSeconds = false;
                }
                if (EditorGUI.EndChangeCheck()) { isSceneSaveRequired = true; }

                lightingScript.DisplayScreenClock(lightingScript.useScreenClock);
            }
            EditorGUILayout.EndVertical();
            #endregion

            // SCREEN FADE SETTINGS
            #region SCREEN FADE SETTINGS
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Screen Fade Settings", EditorStyles.boldLabel);
            if (lightingScript.showScreenFadeSettings) { if (GUILayout.Button("Hide", GUILayout.Width(50f))) { lightingScript.showScreenFadeSettings = false; isSceneSaveRequired = true; } }
            else { if (GUILayout.Button("Show", GUILayout.Width(50f))) { lightingScript.showScreenFadeSettings = true; isSceneSaveRequired = true; } }
            EditorGUILayout.EndHorizontal();

            if (lightingScript.showScreenFadeSettings)
            {
                EditorGUI.BeginChangeCheck();
                if (!lightingScript.fadeOutOnWake)
                {
                    lightingScript.fadeInOnWake = EditorGUILayout.Toggle(new GUIContent("Fade In On Wake", "Fade the scene in when it first starts"), lightingScript.fadeInOnWake);
                    lightingScript.fadeInDuration = EditorGUILayout.Slider(new GUIContent("Fade In Duration", "The length of time in seconds, it takes to fully fade in the scene"), lightingScript.fadeInDuration, 0f, 20f);
                }

                if (!lightingScript.fadeInOnWake)
                {
                    lightingScript.fadeOutOnWake = EditorGUILayout.Toggle(new GUIContent("Fade Out On Wake", "Fade the scene out when it first starts"), lightingScript.fadeOutOnWake);
                    lightingScript.fadeOutDuration = EditorGUILayout.Slider(new GUIContent("Fade Out Duration", "The length of time in seconds, it takes to fully fade out the scene"), lightingScript.fadeOutDuration, 0f, 20f);
                }

                // Reset to fade settings factory defaults
                if (GUILayout.Button(new GUIContent("Reset", "Reset Screen Fade Settings to factory defaults"), GUILayout.Width(60f)))
                {
                    lightingScript.fadeInOnWake = lightingScript.GetDefaultFadeInOnWake;
                    lightingScript.fadeOutOnWake = lightingScript.GetDefaultFadeOutOnWake;
                    lightingScript.fadeInDuration = lightingScript.GetDefaultFadeInDuration;
                    lightingScript.fadeOutDuration = lightingScript.GetDefaultFadeOutDuration;
                    isSceneSaveRequired = true;
                }

                if (EditorGUI.EndChangeCheck()) { isSceneSaveRequired = true; }
            }
            EditorGUILayout.EndVertical();
            #endregion

            #region Update Lighting Preview
            if (GUILayout.Button("Update Lighting Preview"))
            {
                // Display preview of lighting conditions
                lightingScript.UpdateLightingPreview();

                isSceneSaveRequired = true;
            }
            #endregion

            // Some of the controls have changed, so mark the scene as changed so that
            // if it is run without the LBLighting editor script open, the values will persist.
            if (isSceneSaveRequired && !Application.isPlaying)
            {
                isSceneSaveRequired = false;
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }

        }

        /// <summary>
        /// Returns a formatted time string (12 hr time, 15 minute increments)
        /// </summary>
        /// <returns>The time string.</returns>
        public string CurrentTimeString12Hr15Min(float timeFloat)
        {
            float timeOfDay = timeFloat;
            string ampm = "A.M";
            if (timeOfDay >= 12f) { ampm = "P.M."; }
            if ((timeOfDay < 12f || timeOfDay > 13f) && timeOfDay >= 1f)
            {
                timeOfDay = timeOfDay % 12f;
            }
            else if (timeOfDay < 1f) { timeOfDay += 12f; }
            float minutes = Mathf.RoundToInt((timeOfDay % 1f) * 4f) * 15f;
            return (Mathf.FloorToInt(timeOfDay).ToString("0") + ":" + Mathf.FloorToInt(minutes).ToString("00") + " " + ampm);
        }

    }
#endif

    #endregion

    #region LBSkybox Class

    [System.Serializable]
    public class LBSkybox
    {
        // Six sided skybox class

        public Material material;
        public float transitionStartTime;
        public float transitionEndTime;

        public bool IsValidSixSidedSkybox { get; set; }

        // Class constructor
        public LBSkybox()
        {
            this.material = null;
            this.transitionStartTime = 8f;
            this.transitionEndTime = 16f;
            this.IsValidSixSidedSkybox = false;
        }

        // Create a blended skybox material from two normal skybox materials
        public static Material BlendedSkybox(Material blendedSkyboxMaterial, Material skybox1Material, Material skybox2Material)
        {
            // Validate the skybox materials
            if (skybox1Material != null && skybox2Material != null)
            {
                // Set the textures for the first skybox
                blendedSkyboxMaterial.SetTexture("_FrontTex", skybox1Material.GetTexture("_FrontTex"));
                blendedSkyboxMaterial.SetTexture("_BackTex", skybox1Material.GetTexture("_BackTex"));
                blendedSkyboxMaterial.SetTexture("_LeftTex", skybox1Material.GetTexture("_LeftTex"));
                blendedSkyboxMaterial.SetTexture("_RightTex", skybox1Material.GetTexture("_RightTex"));
                blendedSkyboxMaterial.SetTexture("_UpTex", skybox1Material.GetTexture("_UpTex"));
                blendedSkyboxMaterial.SetTexture("_DownTex", skybox1Material.GetTexture("_DownTex"));

                // Set the textures for the second skybox
                blendedSkyboxMaterial.SetTexture("_FrontTex2", skybox2Material.GetTexture("_FrontTex"));
                blendedSkyboxMaterial.SetTexture("_BackTex2", skybox2Material.GetTexture("_BackTex"));
                blendedSkyboxMaterial.SetTexture("_LeftTex2", skybox2Material.GetTexture("_LeftTex"));
                blendedSkyboxMaterial.SetTexture("_RightTex2", skybox2Material.GetTexture("_RightTex"));
                blendedSkyboxMaterial.SetTexture("_UpTex2", skybox2Material.GetTexture("_UpTex"));
                blendedSkyboxMaterial.SetTexture("_DownTex2", skybox2Material.GetTexture("_DownTex"));
            }

            // Return the blended skybox
            return blendedSkyboxMaterial;
        }

        /// <summary>
        /// Verify that a skybox material is suitable for use as a six-sided skybox
        /// It takes a string (validationMessage) which it populates and returns
        /// </summary>
        /// <param name="validationMessage"></param>
        /// <returns></returns>
        public bool ValidateSixSidedSkybox(out string validationMessage)
        {
            bool isValid = false;
            string missingTextureMsg = " does not contain a {0} texture and is not a valid six-sided skybox material";

            validationMessage = string.Empty;

            if (material == null)
            {
                validationMessage = "Material is null";
            }
            else if (!material.HasProperty("_FrontTex"))
            {
                validationMessage = material.name + string.Format(missingTextureMsg, "_FrontText");
            }
            else if (!material.HasProperty("_BackTex"))
            {
                validationMessage = material.name + string.Format(missingTextureMsg, "_BackTex");
            }
            else if (!material.HasProperty("_LeftTex"))
            {
                validationMessage = material.name + string.Format(missingTextureMsg, "_LeftTex");
            }
            else if (!material.HasProperty("_RightTex"))
            {
                validationMessage = material.name + string.Format(missingTextureMsg, "_RightTex");
            }
            else if (!material.HasProperty("_UpTex"))
            {
                validationMessage = material.name + string.Format(missingTextureMsg, "_UpTex");
            }
            else if (!material.HasProperty("_DownTex"))
            {
                validationMessage = material.name + string.Format(missingTextureMsg, "_DownTex");
            }
            else
            {
                isValid = true;
            }

            // Update property
            IsValidSixSidedSkybox = isValid;

            return isValid;
        }

        /// <summary>
        /// Verify that a skybox material is suitable for use as a six-sided skybox
        /// A more optimised version of ValidateSixSidedSkybox(out string validationMessage)
        /// without error messages for use at runtime.
        /// NOTE: Does not update isValidSixSidedSkybox property
        /// </summary>
        /// <returns></returns>
        public bool ValidateSixSidedSkybox()
        {
            if (material == null) { return false; }
            else if (material.HasProperty("_FrontTex") &&
                    material.HasProperty("_BackTex") &&
                    material.HasProperty("_LeftTex") &&
                    material.HasProperty("_RightTex") &&
                    material.HasProperty("_UpTex") &&
                    material.HasProperty("_DownTex")
                    )
            {
                return true;
            }
            else { return false; }
        }
    }

    #endregion

    #region LBWeatherState class

    [System.Serializable]
    public class LBWeatherState
    {
        // Weather state class

        #region Variables and Properties
        // NOTE: When adding/changing properties update both constructors
        public string name;
        public float probability;
        public float wetness;
        public float cloudDensity;
        public float cloudCoverage;
        public float cloudShadowStrength;
        public float rainStrength;
        public float hailStrength;
        public float snowStrength;
        public float fogDensityMultiplier;
        [Range(0f, 1000f)] public float cloudsMorphingSpeed;  // Added 1.4.0 Beta 4e
        public float windStrength;
        public float windZoneMain;
        public float windZoneTurbulence;
        public float minStateDuration;
        public float maxStateDuration;

        #endregion

        // Class constructor
        public LBWeatherState()
        {
            this.name = "New Weather State";
            this.probability = 0.25f;
            this.wetness = 0.5f;
            this.cloudDensity = 3f;
            this.cloudCoverage = 0.5f;
            this.cloudShadowStrength = 0.85f;
            this.rainStrength = 0.5f;
            this.hailStrength = 0f;
            this.snowStrength = 0f;
            this.fogDensityMultiplier = 1f;
            this.cloudsMorphingSpeed = 50f;
            this.windStrength = 250f;
            this.windZoneMain = 1f;
            this.windZoneTurbulence = 1f;
            this.minStateDuration = 30f;
            this.maxStateDuration = 60f;
        }

        /// <summary>
        /// LBWeatherState Clone constructor
        /// </summary>
        /// <param name="lbWeatherState"></param>
        public LBWeatherState(LBWeatherState lbWeatherState)
        {
            this.name = lbWeatherState.name;
            this.probability = lbWeatherState.probability;
            this.wetness = lbWeatherState.wetness;
            this.cloudDensity = lbWeatherState.cloudDensity;
            this.cloudCoverage = lbWeatherState.cloudCoverage;
            this.cloudShadowStrength = lbWeatherState.cloudShadowStrength;
            this.rainStrength = lbWeatherState.rainStrength;
            this.hailStrength = lbWeatherState.hailStrength;
            this.snowStrength = lbWeatherState.snowStrength;
            this.fogDensityMultiplier = lbWeatherState.fogDensityMultiplier;
            this.cloudsMorphingSpeed = lbWeatherState.cloudsMorphingSpeed;
            this.windStrength = lbWeatherState.windStrength;
            this.windZoneMain = lbWeatherState.windZoneMain;
            this.windZoneTurbulence = lbWeatherState.windZoneTurbulence;
            this.minStateDuration = lbWeatherState.minStateDuration;
            this.maxStateDuration = lbWeatherState.maxStateDuration;
        }
    }

    #endregion
}