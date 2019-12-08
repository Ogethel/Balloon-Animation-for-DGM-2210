#define _LBImageFX_DEBUG_MODE
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if !UNITY_5_2 && UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

// Landscape Builder. Copyright (c) 2016-2019 SCSM Pty Ltd. All rights reserved.
namespace LandscapeBuilder
{
    [ExecuteInEditMode]
    [AddComponentMenu("Landscape Builder/ImageFX")]
    [RequireComponent(typeof(Camera))]
#if UNITY_5_4_OR_NEWER
    //[ImageEffectAllowedInSceneView]
#endif
    public class LBImageFX : MonoBehaviour
    {
        #region Enumerations

        /// <summary>
        /// Enumeration of cloud styles (0-based)
        /// Also used in LBLighting inspector.
        /// baseTexArray and detailTexArray must have
        /// the same number of items.
        /// </summary>
        public enum CloudStyle
        {
            CloudStyle1 = 0,
            CloudStyle2 = 1,
            CloudStyle3 = 2
        }

        /// <summary>
        /// Enumeration of cloud quality levels (0-based)
        /// </summary>
        public enum CloudQualityLevel
        {
            Low = 0,
            High = 2
        }

        /// <summary>
        /// Enumeration of SSRR settings presets (0-based)
        /// </summary>
        public enum SSRRPreset
        {
            ExtremePerformance = 3,
            Performance = 6,
            Balanced = 9,
            Quality = 12,
            ExtremeQuality = 15,
            UltimateQuality = 18
        }

        /// <summary>
        /// Enumeration of downsampling amounts (0-based)
        /// </summary>
        public enum Downsampling
        {
            None = 0,
            x2 = 2,
            x3 = 3,
            x4 = 4
        }

        public enum BlurQuality
        {
            Low = 3,
            Medium = 6,
            High = 9
        }

        #endregion

        #region Variables and Properties

        [HideInInspector] public Camera cam;

        [HideInInspector] public Shader imageFXShader;
        private Material imageFXMaterial = null;

        private RenderTexture preSSRRRenderTexture;
        private RenderTexture filteredPreSSRRRenderTexture;
        private RenderTexture postSSRRRenderTexture;
        private RenderTexture SSRRFinalRenderTexture;
        private RenderTexture SSRRBlurRenderTexture;
        private RenderTexture SSRRPostFilteringRenderTexture;

        // private Shader backFaceDepthShader;
        // private Camera backFaceCamera;
        // private RenderTexture backFaceDepthTexture;

        private RenderTexture downsampledScreenTexture;
        private RenderTexture downsampledDepthTexture;

        // Fog variables
        [HideInInspector] public bool showFogSettings = false;
        [HideInInspector] public bool useUnityFogColour = true;
        [HideInInspector] public Color fogColour = new Color(95f / 255f, 113f / 255f, 117 / 255f, 1f);
        [HideInInspector] public float heightFogDensity = 0.005f;
        [HideInInspector] public float distanceFogDensity = 0.005f;
        [HideInInspector] [Range(0f, 2000f)] public float fogHeight = 1000f;
        [HideInInspector] public float fogWaterLevel = 0f;
        [HideInInspector] public bool distanceBasedFog = true;
        [HideInInspector] public bool heightBasedFog = false;
        [HideInInspector] public bool fogSkybox = false;
        [HideInInspector] [Range(0f, 1f)] public float maxFogIntensity = 0.75f;
        [HideInInspector] [Range(0f, 2f)] public float fogSineAmplitude = 0.5f;
        [HideInInspector] [Range(0f, 0.05f)] public float fogColourVariance = 0f;

        // Rain variables
        // [HideInInspector] public bool showRainSettings = false;
        // [HideInInspector] public Texture2D rainDropsTexture;
        // [HideInInspector] [Range(0f, 20f)] public float rainDropDistortion = 1f;

        // Cloud variables
        [HideInInspector] public bool showCloudSettings = false;
        [HideInInspector] public bool renderClouds = false;
        [HideInInspector] public bool isHDREnabled = false;
        [HideInInspector] [Range(0f, 1f)] public float cloudsDetailAmount = 0.5f;   // Added 1.4.0 Beta 4e
        [HideInInspector] public bool use3DNoise = false;
        [HideInInspector] public CloudStyle cloudStyle = CloudStyle.CloudStyle1;    // Added 1.4.0 Beta 2a
        [HideInInspector] public float cloudsStartHeight = 10000f;
        [HideInInspector] public float cloudsEndHeight = 12000f;
        [HideInInspector] [Range(1, 50)] public int cloudsRayMarches = 15;          // Unused as of 1.4.0 Beta 4e
        [HideInInspector] public CloudQualityLevel cloudsQualityLevel = CloudQualityLevel.Low;  // Added 1.4.0 Beta 4e
        [HideInInspector] [Range(1, 10)] public int refinementRayMarches = 5;       // Unused as of 1.4.0 Beta 4e
        [HideInInspector] public float cloudsTileSize = 250000f;
        [HideInInspector] public Color cloudsUpperColour = new Color(0.95f, 0.95f, 0.95f, 1f);
        [HideInInspector] public Color cloudsLowerColour = new Color(0.65f, 0.65f, 0.65f, 1f);
        #if UNITY_2018_1_OR_NEWER
        [ColorUsageAttribute(true, true)] public Color cloudsUpperColourHDR = new Color(0.95f, 0.95f, 0.95f, 1f);
        [ColorUsageAttribute(true, true)] public Color cloudsLowerColourHDR = new Color(0.65f, 0.65f, 0.65f, 1f);
        #else
        [ColorUsageAttribute(true, true, 0f, 8f, 0.125f, 3f)] public Color cloudsUpperColourHDR = new Color(0.95f, 0.95f, 0.95f, 1f);
        [ColorUsageAttribute(true, true, 0f, 8f, 0.125f, 3f)] public Color cloudsLowerColourHDR = new Color(0.65f, 0.65f, 0.65f, 1f);
        #endif
        [HideInInspector] [Range(3f, 5f)] public float cloudsDensity = 3f;
        [HideInInspector] [Range(0f, 1f)] public float cloudsCoverage = 0.5f;
        [HideInInspector] public Vector2 cloudsAnimationSpeed = Vector2.one * 250f;
        [HideInInspector] [Range(0f, 1000f)] public float cloudsMorphingSpeed = 50f;    // Added 1.4.0 Beta 4e
        [HideInInspector] private Vector2 cloudsPositionOffset = Vector2.zero;
        [HideInInspector] private float cloudsMorphOffset = 0f;
        [HideInInspector] public bool renderCloudShadows = false;
        [HideInInspector] [Range(5, 15)] public int cloudShadowsRayMarches = 5;
        [HideInInspector] [Range(0.5f, 0.9f)] public float maxShadowStrength = 0.75f;
        [HideInInspector] public Light sun;

        [HideInInspector] public Texture2D perlinBaseTex = null;
        [HideInInspector] public Texture2D perlinDetailTex = null;

        // Array of Texture2D names to match CloudStyle enum positions.
        // Are static readonly as they should be same for all instances and not change at runtime
        // after having been initially defined.
        private static readonly string[] baseTexArray = { "CloudStyle1Base", "CloudStyle2Base", "CloudStyle3Base" };
        private static readonly string[] detailTexArray = { "CloudStyle1Detail", "CloudStyle2Detail", "CloudStyle3Detail" };

        // SSRR variables - If changing or adding, please update SSRR Default Properties
        [HideInInspector] public bool SSRREnabled = false;
        [HideInInspector] public bool showSSRRSettings = false;
        [HideInInspector] public bool PBRReflections = true;
        [HideInInspector] public Downsampling SSRRDownsampling = Downsampling.x2;
        [HideInInspector] public bool SSRRFiltering = true;
        [HideInInspector] [Range(1, 50)] public int pixelStride = 10;
        [HideInInspector] [Range(10, 1000)] public int maxRayMarches = 40;
        [HideInInspector] [Range(1f, 300.0f)] public float maxRayDist = 100f;
        [HideInInspector] [Range(0.01f, 1.0f)] public float maxRayStride = 0.1f;
        [HideInInspector] [Range(0f, 0.2f)] public float screenFadeDist = 0f;
        [HideInInspector] [Range(0f, 3f)] public float fresnelFade = 1f;
        [HideInInspector] [Range(0.1f, 5f)] public float fresnelPower = 1f;
        [HideInInspector] [Range(0f, 1f)] public float blurStrength = 0.3f;
        [HideInInspector] public BlurQuality SSRRBlurQuality = BlurQuality.Medium;
        [HideInInspector] public bool SSRRJitter = true;
        [HideInInspector] public bool reflectNearPixels = true;
        [HideInInspector] public bool reflectFarPixels = true;
        [HideInInspector] public SSRRPreset SSRRSettingsPreset = SSRRPreset.Balanced;

        // SSRR Default Properties
        public bool GetDefaultSSRREnabled { get { return false; } }
        public bool GetDefaultShowSSRRSettings { get { return false; } }
        public bool GetDefaultPBRReflections { get { return true; } }
        public Downsampling GetDefaultSSRRDownsampling { get { return Downsampling.x2; } }
        public bool GetDefaultSSRRFiltering { get { return true; } }
        public int GetDefaultPixelStride { get { return 10; } }
        public int GetDefaultMaxRayMarches { get { return 40; } }
        public float GetDefaultMaxRayDist { get { return 100f; } }
        public float GetDefaultMaxRayStride { get { return 0.1f; } }
        public float GetDefaultScreenFadeDist { get { return 0f; } }
        public float GetDefaultFresnelFade { get { return 1f; } }
        public float GetDefaultFresnelPower { get { return 1f; } }
        public float GetDefaultBlurStrength { get { return 0.3f; } }
        public BlurQuality GetDefaultSSRRBlurQuality { get { return BlurQuality.Medium; } }
        public bool GetDefaultSSRRJitter { get { return true; } }
        public bool GetDefaultReflectNearPixels { get { return true; } }
        public bool GetDefaultRelectFarPixels { get { return true; } }
        public SSRRPreset GetDefaultSSRRSettingsPreset { get { return SSRRPreset.Balanced; } }

        // Unity Project Settings
        [HideInInspector] public bool isLinearColourSpace = true;

        // LB Lighting integration
        [HideInInspector] public LBLighting lbLighting = null;

        private RenderingPath cameraRenderingPath;

        #endregion

        #region Initialisation

        // Use this for initialization
        void Awake()
        {
            cam = GetComponent<Camera>();

            // Is LB Lighting in the scene?
            if (lbLighting == null)
            {
                lbLighting = GameObject.FindObjectOfType<LBLighting>();
            }
        }

        void OnEnable()
        {
            Initialise();
        }

        void OnDisable()
        {
            // Destroy materials that are now not in use
            DestroyImmediate(imageFXMaterial);
            imageFXMaterial = null;

            // // Destroy the render texture camera
            // DestroyImmediate(renderTextureCamera);
        }

        void Initialise()
        {
            // Load the ImageFX shader
            if (imageFXShader == null)
            {
                imageFXShader = Shader.Find("Hidden/LBImageFX");
                if (imageFXShader == null) { Debug.LogWarning("LBImageFX.Initialise - could not load Hidden/LBImageFX shader"); }
            }

            SetCloudStyleTextures();

            // Attempt to find the sun in the scene
            if (sun == null)
            {
                // See if we can get the sun from LBLighting
                if (lbLighting != null)
                {
                    sun = lbLighting.sun;
                }
                else
                {
                    // No LBLighting, so try to discover a sun in the scene
                    List<Light> directionalLights = LBLighting.GetLightList(false, LightType.Directional);
                    if (directionalLights != null)
                    {
                        for (int l = 0; l < directionalLights.Count; l++)
                        {
                            Light light = directionalLights[l];
                            if (light.name.ToLower().Contains("sun")) { sun = light; break; }
                        }

                        // If everything fails, just select the first one
                        if (sun == null && directionalLights.Count > 0)
                        {
                            sun = directionalLights[0];
                        }
                    }
                }
            }

            #if UNITY_EDITOR
            isLinearColourSpace = (PlayerSettings.colorSpace == ColorSpace.Linear);
            #else
            isLinearColourSpace = (QualitySettings.activeColorSpace == ColorSpace.Linear);
            #endif

            // // Create a camera for rendering the back-face depth of the scene
            // if (backFaceCamera == null)
            // {
            // GameObject backFaceCameraObj = new GameObject("BackFaceDepthCamera");
            // backFaceCameraObj.hideFlags = HideFlags.HideAndDontSave;
            // backFaceCamera = backFaceCameraObj.AddComponent<Camera>();
            // }

            // // Load the back-face depth shader
            // if (backFaceDepthShader == null)
            // {
            //     backFaceDepthShader = Shader.Find("Hidden/LBBackFaceDepth");
            //     if (backFaceDepthShader == null) { Debug.LogWarning("LBImageFX.Initialise - could not load Hidden/LBBackFaceDepth shader"); }
            // }
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Add a LBImageFX script to a Camera
        /// Locate and add the LBImageFX shader
        /// NOTE: The shader will not be added at runtime unless it
        /// has been moved into Shaders\Resources project folder.
        /// </summary>
        /// <param name="camera"></param>
        /// <returns></returns>
        public static LBImageFX AddImageFX(Camera camera)
        {
            LBImageFX lbImageFX = null;

            if (camera.gameObject != null)
            {
                lbImageFX = camera.gameObject.AddComponent<LBImageFX>();
                if (lbImageFX != null)
                {
                    // Currently LBImageFX is not in a resource folder, so this will
                    // fail at runtime.
                    lbImageFX.imageFXShader = Shader.Find("Hidden/LBImageFX");
                }
            }

            return lbImageFX;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Set or load the cloud textures based on the current CloudStyle
        /// </summary>
        public void SetCloudStyleTextures()
        {
            //Debug.Log("INFO: LBImageFX.SetCloudStyleTextures cloudStyle: " + (int)cloudStyle);

            // Default Texture2D names
            string baseTexName = "CloudStyle1Base";
            string detailTexName = "CloudStyle1Detail";

            int cloudStyleIndex = (int)cloudStyle;

            // Basic validation
            if (cloudStyleIndex >= baseTexArray.Length) { Debug.LogWarning("ERROR: LBImageFX.SetCloudStyleTextures - cloud style does not exist in base texture array"); }
            else if (cloudStyleIndex >= detailTexArray.Length) { Debug.LogWarning("ERROR: LBImageFX.SetCloudStyleTextures - cloud style does not exist in detail texture array"); }
            else
            {
                // Get the Texture2D names from the arrays using the current CloudStyle
                baseTexName = baseTexArray[cloudStyleIndex];
                detailTexName = detailTexArray[cloudStyleIndex];

                //Debug.Log("INFO: LBImageFX.SetCloudStyleTextures - loading LandscapeBuilder/Resources/Textures/" + baseTexName);
                // Attempt to load the Texture2D from memory or the Resources folder for current CloudStyle
                perlinBaseTex = Resources.Load("Textures/" + baseTexName, typeof(Texture2D)) as Texture2D;
                if (perlinBaseTex == null) { Debug.LogWarning("LBImageFX.SetCloudStyleTextures - could not find LandscapeBuilder/Resources/Textures/" + baseTexName); }

                //Debug.Log("INFO: LBImageFX.SetCloudStyleTextures - loading LandscapeBuilder/Resources/Textures/" + detailTexName);
                perlinDetailTex = Resources.Load("Textures/" + detailTexName, typeof(Texture2D)) as Texture2D;
                if (perlinBaseTex == null) { Debug.LogWarning("LBImageFX.SetCloudStyleTextures - could not find LandscapeBuilder/Resources/Textures/" + detailTexName); }
            }

            // Fallback - Attempt to pre-populate the shader textures used for clouds with default textures
            if (perlinBaseTex == null)
            {
                baseTexName = "Perlin10BaseTex";
                Debug.Log("INFO: LBImageFX.SetCloudStyleTextures - loading default LandscapeBuilder/Resources/Textures/" + baseTexName);
                perlinBaseTex = Resources.Load("Textures/" + baseTexName, typeof(Texture2D)) as Texture2D;
                if (perlinBaseTex == null) { Debug.LogWarning("LBImageFX.SetCloudStyleTextures - could not find LandscapeBuilder/Resources/Textures/" + baseTexName); }
            }
            if (perlinDetailTex == null)
            {
                detailTexName = "Perlin10DetailTex";
                Debug.Log("INFO: LBImageFX.SetCloudStyleTextures - loading default LandscapeBuilder/Resources/Textures/" + detailTexName);
                perlinDetailTex = Resources.Load("Textures/" + detailTexName, typeof(Texture2D)) as Texture2D;
                if (perlinBaseTex == null) { Debug.LogWarning("LBImageFX.SetCloudStyleTextures - could not find LandscapeBuilder/Resources/Textures/" + detailTexName); }
            }
        }

        public void ResetCloudPosition()
        {
            cloudsPositionOffset = Vector2.zero;
            cloudsMorphOffset = 0f;
        }

        #endregion

        #region Private Methods

        private bool CreateImageEffectMaterial()
        {
            bool materialCreated = false;
            if (imageFXShader != null)
            {
                // If the material has already been created, no need to create an new one
                if (imageFXMaterial != null) { return true; }

                // Create a new material using the given shader
                imageFXMaterial = new Material(imageFXShader);
                if (imageFXMaterial != null)
                {
                    // Material is not shown in the hierarchy, not saved in the scene and not unloaded by Resources.UnloadUnusedAssets
                    imageFXMaterial.hideFlags = HideFlags.HideAndDontSave;
                    // The material has now been successfully created
                    materialCreated = true;
                }
            }
            // Return whether the material was successfully created
            return materialCreated;
        }

        // // Method that runs just before any cameras attached to this gameobject cull objects
        // // Here we will render the backface depth buffer to a render texture using a second camera
        // void OnPreCull ()
        // {
        //     if (SSRREnabled)
        //     {
        //         // Create the new render texture - use 16 bit depth buffer and (R) float format to get 32 bit precision for it
        //         // Will need to downsample this
        //         backFaceDepthTexture = RenderTexture.GetTemporary(cam.pixelWidth, cam.pixelHeight, 16, RenderTextureFormat.RFloat);

        //         // Set the second camera's position and settings to that of our first camera
        //         backFaceCamera.CopyFrom(cam);
        //         // Disable the camera so that it doesn't waste time unnecessarily rendering
        //         backFaceCamera.enabled = false;
        //         // Set our camera to render using the back-face depth shader
        //         backFaceCamera.SetReplacementShader(backFaceDepthShader, null); 
        //         backFaceCamera.backgroundColor = Color.white;
        //         backFaceCamera.clearFlags = CameraClearFlags.SolidColor;
        //         // Render the camera into our designated render texture
        //         backFaceCamera.targetTexture = backFaceDepthTexture;
        //         backFaceCamera.Render();
        //     }
        // }

        // Image Effects are applied before Transparent materials
        // Fixes issues with transparent objects like water but breaks other things (clouds) in Deferred render path
        [ImageEffectOpaque]
        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            // If the image effect material was successfully created...
            if (CreateImageEffectMaterial())
            {
                // ... render the screen with the post processing effect shader
                // Set up the material first

                if (cam == null) { cam = GetComponent<Camera>(); }

                int renderCloudsInt = 0;
                if (renderClouds) { renderCloudsInt = 1; }
                int renderCloudShadowsInt = 0;
                if (renderClouds && renderCloudShadows) { renderCloudShadowsInt = 1; }
                int distanceFogInt = 0;
                if (distanceBasedFog) { distanceFogInt = 1; }
                int heightFogInt = 0;
                if (heightBasedFog) { heightFogInt = 1; }
                imageFXMaterial.SetVector("_FeaturesEnabled", new Vector4(renderCloudsInt, renderCloudShadowsInt, distanceFogInt, heightFogInt));

                imageFXMaterial.SetColor("_FogColour", fogColour);

                float fogSkyboxFloat = 0f;
                if (fogSkybox) { fogSkyboxFloat = 1f; }
                float useUnityFogColourFloat = 0f;
                if (useUnityFogColour) { useUnityFogColourFloat = 1f; }
                imageFXMaterial.SetVector("_FogParams", new Vector4(distanceFogDensity, heightFogDensity, fogHeight, fogSineAmplitude));
                imageFXMaterial.SetVector("_FogParams2", new Vector4(fogColourVariance, maxFogIntensity, fogSkyboxFloat, useUnityFogColourFloat));
                imageFXMaterial.SetFloat("_FogParams3", fogWaterLevel);

                if (cloudsQualityLevel == CloudQualityLevel.Low)
                {
                    imageFXMaterial.EnableKeyword("CLOUD_QUALITY_LOW");
                    imageFXMaterial.DisableKeyword("CLOUD_QUALITY_HIGH");
                }
                else
                {
                    imageFXMaterial.DisableKeyword("CLOUD_QUALITY_LOW");
                    imageFXMaterial.EnableKeyword("CLOUD_QUALITY_HIGH");
                }

                // The specified RenderingPath may not be available due to limitations with the current
                // device. ActualRenderingPath is the one "actually" being used.
                // cameraRenderingPath = cam.actualRenderingPath;
                // if (cameraRenderingPath == RenderingPath.DeferredShading) { imageFXMaterial.EnableKeyword("USING_DEFERRED_RENDERING_PATH"); }
                // else { imageFXMaterial.DisableKeyword("USING_DEFERRED_RENDERING_PATH"); }

                // imageFXMaterial.SetTexture("_RainDropsTex", rainDropsTexture);
                // imageFXMaterial.SetVector("_RainParams", new Vector4(rainDropDistortion, 0f, 0f, 0f));

                // Animate clouds position
                // This needs to be subtracted instead of added each frame as it offsets not the position but the noise sample point
                cloudsPositionOffset -= cloudsAnimationSpeed * Time.deltaTime;
                cloudsMorphOffset -= cloudsMorphingSpeed * Time.deltaTime;

                if (renderClouds) { imageFXMaterial.EnableKeyword("RENDER_CLOUDS"); }
                else { imageFXMaterial.DisableKeyword("RENDER_CLOUDS"); }
                if (use3DNoise) { imageFXMaterial.EnableKeyword("USE_3D_NOISE"); }
                else { imageFXMaterial.DisableKeyword("USE_3D_NOISE"); }
                imageFXMaterial.SetVector("_CloudsParams", new Vector4(cloudsStartHeight, cloudsEndHeight, 0f, cloudsTileSize));
                imageFXMaterial.SetVector("_CloudsParams2", new Vector4(cloudsDensity, cloudsCoverage, cloudsPositionOffset.x, cloudsPositionOffset.y));
                imageFXMaterial.SetVector("_CloudsParams3", new Vector4(cloudsMorphOffset, (float)cloudShadowsRayMarches, maxShadowStrength, cloudsDetailAmount));
                imageFXMaterial.SetColor("_CloudsUpperColour", isHDREnabled ? cloudsUpperColourHDR : cloudsUpperColour);
                imageFXMaterial.SetColor("_CloudsLowerColour", isHDREnabled ? cloudsLowerColourHDR : cloudsLowerColour);
                imageFXMaterial.SetTexture("_PerlinBaseTex", perlinBaseTex);
                imageFXMaterial.SetTexture("_PerlinDetailTex", perlinDetailTex);

                if (sun != null)
                {
                    // Manually pass the world space direction of the current directional light into the shader
                    imageFXMaterial.SetVector("_WorldLightDir", sun.transform.forward);
                }

                if (cam != null)
                {
                    imageFXMaterial.SetMatrix("_ClipToWorld", cam.cameraToWorldMatrix * cam.projectionMatrix.inverse);
                    imageFXMaterial.SetMatrix("_WorldToView", cam.worldToCameraMatrix);

                    float tanHalfFOV = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
                    imageFXMaterial.SetFloat("_TanHalfFOVX", tanHalfFOV * cam.aspect);
                    imageFXMaterial.SetFloat("_TanHalfFOVY", tanHalfFOV);

                    RenderingPath camRP = cam.renderingPath;
                    if (camRP == RenderingPath.DeferredShading)
                    {
                        imageFXMaterial.EnableKeyword("USING_DEFERRED_RENDERING_PATH");
                        cam.depthTextureMode = DepthTextureMode.Depth;
                        if (PBRReflections) { imageFXMaterial.EnableKeyword("PHYSICALLY_BASED_REFLECTIONS"); }
                        else { imageFXMaterial.DisableKeyword("PHYSICALLY_BASED_REFLECTIONS"); }
                    }
                    else
                    {
                        imageFXMaterial.DisableKeyword("USING_DEFERRED_RENDERING_PATH");
                        cam.depthTextureMode = DepthTextureMode.DepthNormals;
                        imageFXMaterial.DisableKeyword("PHYSICALLY_BASED_REFLECTIONS");
                    }
                }
                else
                {
                    Debug.Log("LBImageFX - Camera is null");
                    imageFXMaterial.DisableKeyword("PHYSICALLY_BASED_REFLECTIONS");
                }

                imageFXMaterial.SetVector("_ReflectionParams", new Vector4((float)pixelStride, (float)maxRayMarches, maxRayDist, screenFadeDist));
                float SSRRjitterFloat = 0f;
                if (SSRRJitter) { SSRRjitterFloat = 1f; }
                imageFXMaterial.SetVector("_ReflectionParams2", new Vector4(fresnelFade, fresnelPower, SSRRjitterFloat, blurStrength));
                float SSRRBlurQualityFloat;
                if (SSRRBlurQuality == BlurQuality.Low) { SSRRBlurQualityFloat = 1f; }
                else if (SSRRBlurQuality == BlurQuality.Medium) { SSRRBlurQualityFloat = 3f; }
                else { SSRRBlurQualityFloat = 5f; }
                int SSRRDownsamplingInt;
                if (SSRRDownsampling == Downsampling.None) { SSRRDownsamplingInt = 1; }
                else if (SSRRDownsampling == Downsampling.x2) { SSRRDownsamplingInt = 2; }
                else if (SSRRDownsampling == Downsampling.x3) { SSRRDownsamplingInt = 3; }
                else { SSRRDownsamplingInt = 4; }
                imageFXMaterial.SetVector("_ReflectionParams3", new Vector4((float)SSRRDownsamplingInt, SSRRBlurQualityFloat, 0f, 0f));

                if (!SSRREnabled)
                {
                    imageFXMaterial.DisableKeyword("REFLECT_NEAR_PIXELS");
                    imageFXMaterial.DisableKeyword("REFLECT_FAR_PIXELS");
                }
                else
                {
                    if (reflectNearPixels) { imageFXMaterial.EnableKeyword("REFLECT_NEAR_PIXELS"); }
                    else { imageFXMaterial.DisableKeyword("REFLECT_NEAR_PIXELS"); }
                    if (reflectFarPixels) { imageFXMaterial.EnableKeyword("REFLECT_FAR_PIXELS"); }
                    else { imageFXMaterial.DisableKeyword("REFLECT_FAR_PIXELS"); }
                }

                // Render the screen with the image effect applied
                if (SSRREnabled)
                {
                    // TODO: Do we need to request a HDR render texture if it is a HDR enabled camera?
                    // Request a full resolution texture, which the fog and clouds result will be read into
                    preSSRRRenderTexture = RenderTexture.GetTemporary(source.width, source.height, 0, source.format);
                    // First pass - fog + clouds
                    Graphics.Blit(source, preSSRRRenderTexture, imageFXMaterial, 0);
                    // Calculate downsampled pixel width and height
                    int downsampledPixelWidth = source.width / SSRRDownsamplingInt;
                    int downsampledPixelHeight = source.height / SSRRDownsamplingInt;
                    ComputeSSRRTexture(source.width, source.height, downsampledPixelWidth, downsampledPixelHeight, SSRRDownsamplingInt);
                    // Set computed SSRR texture result in shader
                    imageFXMaterial.SetTexture("_SSRRTexture", SSRRFinalRenderTexture);
                    // Final pass - SSRR combine + cloud shadows
                    Graphics.Blit(preSSRRRenderTexture, destination, imageFXMaterial, 2);
                    // Release render textures
                    RenderTexture.ReleaseTemporary(preSSRRRenderTexture);
                    RenderTexture.ReleaseTemporary(SSRRFinalRenderTexture);
                }
                else
                {
                    // Request a full resolution texture, which the fog and clouds result will be read into
                    // Eventually it would be nice to not need this (as without SSRR it is not strictly necessary)
                    preSSRRRenderTexture = RenderTexture.GetTemporary(source.width, source.height, 0, source.format);
                    // First pass - fog + clouds
                    Graphics.Blit(source, preSSRRRenderTexture, imageFXMaterial, 0);
                    // Second pass - cloud shadows
                    Graphics.Blit(preSSRRRenderTexture, destination, imageFXMaterial, 2);
                    // Release render textures
                    RenderTexture.ReleaseTemporary(preSSRRRenderTexture);
                }
            }
            // Else...
            else
            {
                // ... don't do anything
                Graphics.Blit(source, destination);
            }
        }

        /// <summary>
        /// Compute the Screen Space Ray traced Reflection texture.
        /// Added preSSRRRenderTexture.format to RenderTexture.GetTemporary() in LB 2.0.6
        /// to cater for HDR. May need to test this further.
        /// </summary>
        /// <param name="sw"></param>
        /// <param name="sh"></param>
        /// <param name="dpw"></param>
        /// <param name="dph"></param>
        /// <param name="ssrrdi"></param>
        private void ComputeSSRRTexture(int sw, int sh, int dpw, int dph, int ssrrdi)
        {
            if (SSRRDownsampling != Downsampling.None)
            {
                if (SSRRFiltering)
                {
                    // Request a full resolution texture, which the final SSRR result will be read into
                    SSRRFinalRenderTexture = RenderTexture.GetTemporary(sw, sh, 0, preSSRRRenderTexture.format);
                    // Request a downsampled texture, which the filtered pre-SSRR result will be read into
                    filteredPreSSRRRenderTexture = RenderTexture.GetTemporary(dpw, dph, 0, preSSRRRenderTexture.format);
                    // Request a downsampled texture, which the initial SSRR result will be read into
                    postSSRRRenderTexture = RenderTexture.GetTemporary(dpw, dph, 0, preSSRRRenderTexture.format);
                    // Filter and downsample pre-SSRR texture
                    imageFXMaterial.SetVector("_FilteringParams", new Vector4(1f, (float)ssrrdi, 0f, 0f));
                    Graphics.Blit(preSSRRRenderTexture, filteredPreSSRRRenderTexture, imageFXMaterial, 4);
                    // Second pass - SSRR (downsampled)
                    Graphics.Blit(filteredPreSSRRRenderTexture, postSSRRRenderTexture, imageFXMaterial, 1);
                    if (blurStrength > 0f)
                    {
                        // Request a downsampled texture, which the post-blur result will be read into
                        SSRRBlurRenderTexture = RenderTexture.GetTemporary(dpw, dph, 0, preSSRRRenderTexture.format);
                        // Request a downsampled texture, which the post-filter result will be read into
                        SSRRPostFilteringRenderTexture = RenderTexture.GetTemporary(dpw, dph, 0, preSSRRRenderTexture.format);
                        // Third pass - SSRR blur
                        Graphics.Blit(postSSRRRenderTexture, SSRRBlurRenderTexture, imageFXMaterial, 3);
                        // Filter post-SSRR texture
                        imageFXMaterial.SetVector("_FilteringParams", new Vector4(1f, 2f, 0f, 0f));
                        Graphics.Blit(SSRRBlurRenderTexture, SSRRPostFilteringRenderTexture, imageFXMaterial, 4);
                        // Blit to full resolution texture
                        Graphics.Blit(SSRRPostFilteringRenderTexture, SSRRFinalRenderTexture);
                        // Release render textures
                        RenderTexture.ReleaseTemporary(SSRRBlurRenderTexture);
                    }
                    else
                    {
                        // Request a downsampled texture, which the post-filter result will be read into
                        SSRRPostFilteringRenderTexture = RenderTexture.GetTemporary(dpw, dph, 0, preSSRRRenderTexture.format);
                        // Filter post-SSRR texture
                        imageFXMaterial.SetVector("_FilteringParams", new Vector4(1f, 2f, 0f, 0f));
                        Graphics.Blit(postSSRRRenderTexture, SSRRPostFilteringRenderTexture, imageFXMaterial, 4);
                        // Blit to full resolution texture
                        Graphics.Blit(SSRRPostFilteringRenderTexture, SSRRFinalRenderTexture);
                    }
                    // Release render textures
                    RenderTexture.ReleaseTemporary(filteredPreSSRRRenderTexture);
                    RenderTexture.ReleaseTemporary(postSSRRRenderTexture);
                    RenderTexture.ReleaseTemporary(SSRRPostFilteringRenderTexture);
                }
                else
                {
                    // Request a downsampled texture, which the final SSRR result will be read into
                    SSRRFinalRenderTexture = RenderTexture.GetTemporary(dpw, dph, 0, preSSRRRenderTexture.format);
                    if (blurStrength > 0f)
                    {
                        // Request a downsampled texture, which the post-blur result will be read into
                        SSRRBlurRenderTexture = RenderTexture.GetTemporary(dpw, dph, 0, preSSRRRenderTexture.format);
                        // Second pass - SSRR (downsampled)
                        Graphics.Blit(preSSRRRenderTexture, SSRRBlurRenderTexture, imageFXMaterial, 1);
                        // Third pass - SSRR blur
                        Graphics.Blit(SSRRBlurRenderTexture, SSRRFinalRenderTexture, imageFXMaterial, 3);
                        // Release render textures
                        RenderTexture.ReleaseTemporary(SSRRBlurRenderTexture);
                    }
                    else
                    {
                        // Second pass - SSRR (downsampled)
                        Graphics.Blit(preSSRRRenderTexture, SSRRFinalRenderTexture, imageFXMaterial, 1);
                    }
                }
            }
            else
            {
                // Request a full resolution texture, which the final SSRR result will be read into
                SSRRFinalRenderTexture = RenderTexture.GetTemporary(sw, sh, 0, preSSRRRenderTexture.format);
                if (blurStrength > 0f)
                {
                    // Request a full resolution texture, which the post-blur result will be read into
                    SSRRBlurRenderTexture = RenderTexture.GetTemporary(sw, sh, 0, preSSRRRenderTexture.format);
                    // Second pass - SSRR
                    Graphics.Blit(preSSRRRenderTexture, SSRRBlurRenderTexture, imageFXMaterial, 1);
                    // Third pass - SSRR blur
                    Graphics.Blit(SSRRBlurRenderTexture, SSRRFinalRenderTexture, imageFXMaterial, 3);
                    // Release render textures
                    RenderTexture.ReleaseTemporary(SSRRBlurRenderTexture);
                }
                else
                {
                    // Second pass - SSRR
                    Graphics.Blit(preSSRRRenderTexture, SSRRFinalRenderTexture, imageFXMaterial, 1);
                }
            }
        }

        #endregion
    }

    #region CustomEditor

#if UNITY_EDITOR
    [CustomEditor(typeof(LBImageFX))]
    [CanEditMultipleObjects]
    public class LBImageFXEditor : Editor
    {
        // Private variables

        // Save scene / mark dirty no longer required when
        // 100% using PropertFields
        //private bool isSceneSaveRequired = false;
        private bool isNotSupported = false;

        #region SerializedProperties
        //SerializedProperty imageFXShaderProp;

        // Fog variables
        SerializedProperty showFogSettingsProp;
        SerializedProperty useUnityFogColourProp;
        SerializedProperty fogColourProp;
        SerializedProperty heightFogDensityProp;
        SerializedProperty distanceFogDensityProp;
        SerializedProperty fogHeightProp;
        SerializedProperty fogWaterLevelProp;
        SerializedProperty distanceBasedFogProp;
        SerializedProperty heightBasedFogProp;
        SerializedProperty fogSkyboxProp;
        SerializedProperty maxFogIntensityProp;
        SerializedProperty fogSineAmplitudeProp;
        SerializedProperty fogColourVarianceProp;

        // Rain variables
        // SerializedProperty showRainSettingsProp;
        // SerializedProperty rainDropsTextureProp;
        // SerializedProperty rainDropDistortionProp;

        // Cloud variables
        SerializedProperty showCloudSettingsProp;
        SerializedProperty renderCloudsProp;
        SerializedProperty cloudsDetailAmountProp;
        SerializedProperty use3DNoiseProp;
        SerializedProperty isHDREnabledProp;
        SerializedProperty cloudStyleProp;
        SerializedProperty cloudsStartHeightProp;
        SerializedProperty cloudsEndHeightProp;
        // SerializedProperty cloudsRayMarchesProp;
        SerializedProperty cloudsQualityLevelProp;
        // SerializedProperty refinementRayMarchesProp;
        SerializedProperty cloudsTileSizeProp;
        SerializedProperty cloudsUpperColourProp;
        SerializedProperty cloudsLowerColourProp;
        SerializedProperty cloudsUpperColourHDRProp;
        SerializedProperty cloudsLowerColourHDRProp;
        SerializedProperty cloudsDensityProp;
        SerializedProperty cloudsCoverageProp;
        SerializedProperty cloudsAnimationSpeedProp;
        private Vector2 cloudsAnimationSpeedVector2 = Vector2.zero;
        SerializedProperty cloudsMorphingSpeedProp;
        SerializedProperty renderCloudShadowsProp;
        SerializedProperty cloudShadowsRayMarchesProp;
        SerializedProperty maxShadowStrengthProp;
        SerializedProperty sunProp;

        // Enable define at top of script for testing noise textures
#if LBImageFX_DEBUG_MODE
    SerializedProperty perlinBaseTexProp;
    SerializedProperty perlinDetailTexProp;
#endif

        // SSRR Properties
        SerializedProperty showSSRRSettingsProp;
        SerializedProperty renderSSRRProp;
        SerializedProperty PBRReflectionsProp;
        SerializedProperty SSRRDownsamplingProp;
        SerializedProperty SSRRFilteringProp;
        SerializedProperty pixelStrideProp;
        SerializedProperty maxRayMarchesProp;
        SerializedProperty maxRayDistProp;
        // SerializedProperty maxRayStrideProp;
        SerializedProperty screenFadeDistProp;
        SerializedProperty fresnelFadeProp;
        SerializedProperty fresnelPowerProp;
        SerializedProperty reflectNearPixelsProp;
        SerializedProperty reflectFarPixelsProp;
        SerializedProperty blurStrengthProp;
        SerializedProperty SSRRBlurQualityProp;
        SerializedProperty SSRRJitterProp;
        SerializedProperty SSRRSettingsPresetProp;

        // Unity Project settings
        SerializedProperty isLinearColourSpaceProp;

        #endregion

        #region GUIContent - Fog
        private static readonly GUIContent fogUseUnityFogColourContent = new GUIContent("Use Unity Fog Colour", "Whether the colour of the fog from the Unity Lighting Editor is used. Or if LBLighting is in the scene, tick this if you want to use fog colour from LBLighting.");
        private static readonly GUIContent fogColourContent = new GUIContent("Fog Colour", "The alternate colour of the fog if not using Use Unity Fog Colour");
        private static readonly GUIContent fogDistanceBasedFogContent = new GUIContent("Distance Based Fog", "Whether fog that increases with distance is used");
        private static readonly GUIContent fogDistanceFogDensityContent = new GUIContent("Distance Fog Density", "The fog density of distance based fog - controlled by LBLighting if you have specified this camera as a ImageFX camera");
        private static readonly GUIContent fogHeightBasedFogContent = new GUIContent("Height Based Fog", "Whether fog that appears below a certain height is used");
        private static readonly GUIContent fogHeightFogDensityContent = new GUIContent("Height Fog Density", "The density of height based fog");
        private static readonly GUIContent fogWaterLevelContent = new GUIContent("Water Level", "Distance-based fog will not appear below this level. This is a worldspace y-axis value.");
        private static readonly GUIContent fogMaxHeightContent = new GUIContent("Fog Height", "Height based fog will appear below this height");
        private static readonly GUIContent fogSineAmplitudeContent = new GUIContent("Fog Sine Amplitude", "The amplitude of the 'waves' of height based fog");
        private static readonly GUIContent fogSkyBoxContent = new GUIContent("Fog Skybox", "Whether rendered fog can occlude the skybox");
        private static readonly GUIContent fogMaxFogIntensityContent = new GUIContent("Max Fog Intensity", "The maximum fog intensity");
        private static readonly GUIContent fogColourVarianceContent = new GUIContent("Fog Colour Variance", "How much fog colour is varied throughout the scene");

        #endregion

        #region GUIContent - Clouds
        private static readonly GUIContent renderCloudsContent = new GUIContent("Render Clouds", "Whether clouds are rendered");
        private static readonly GUIContent isHDREnabledContent = new GUIContent("Use HDR", "Allows HDR colours to be selected. This can be useful when also using the Unity Post Processing stack. This is controlled by LBLighting if you have specified this camera as a ImageFX camera");
        private static readonly GUIContent use3DNoiseContent = new GUIContent("Use 3D Noise", "Whether the clouds are rendered using 3D as opposed to 2D noise - this is particularly performance heavy - controlled by LBLighting if you have specified this camera as a ImageFX camera");
        private static readonly GUIContent cloudStyleContent = new GUIContent("Cloud Style", "The style of the clouds");
        private static readonly GUIContent cloudsStartHeightContent = new GUIContent("Clouds Start Height", "The height in 3D space at which the clouds start");
        private static readonly GUIContent cloudsEndHeightContent = new GUIContent("Clouds End Height", "The height in 3D space at which the clouds end");
        private static readonly GUIContent cloudsTileSizeContent = new GUIContent("Clouds Tile Size", "The tile size in 3D space over which cloud noise 'tiles' - increase this value to get wider/longer clouds");
        private static readonly GUIContent cloudsUpperColourContent = new GUIContent("Clouds Upper Colour", "The colour of the upper part of clouds - controlled by LBLighting if you have specified this camera as a ImageFX camera");
        private static readonly GUIContent cloudsLowerColourContent = new GUIContent("Clouds Lower Colour", "The colour of the lower part of clouds - controlled by LBLighting if you have specified this camera as a ImageFX camera");
        private static readonly GUIContent cloudsQualityLevelContent = new GUIContent("Clouds Quality Level", "The quality of the raymarching used to determine cloud density - higher settings increase quality at the cost of performance - controlled by LBLighting if you have specified this camera as a ImageFX camera");
        private static readonly GUIContent cloudsDensityContent = new GUIContent("Clouds Density", "The density of clouds - increase this to make clouds darker - controlled by LBLighting if you have specified this camera as a ImageFX camera");
        private static readonly GUIContent cloudsCoverageContent = new GUIContent("Clouds Coverage", "The coverage of clouds - increase this to make clouds cover more of the sky - controlled by LBLighting if you have specified this camera as a ImageFX camera");
        private static readonly GUIContent cloudsDetailAmountContent = new GUIContent("Clouds Detail Amount", "The amount of extra detail added to them - increasing this will decrease performance slightly - controlled by LBLighting if you have specified this camera as a ImageFX camera");
        private static readonly GUIContent cloudsAnimationSpeedContent = new GUIContent("Clouds Animation Speed", "The velocity of cloud movement in the scene - controlled by LBLighting if you have specified this camera as a ImageFX camera");
        private static readonly GUIContent cloudsMorphingSpeedContent = new GUIContent("Clouds Morphing Speed", "How fast cloud formations will change over time - controlled by LBLighting if you have specified this camera as a ImageFX camera");
        private static readonly GUIContent cloudsCastShadowsContent = new GUIContent("Cast Shadows", "Render shadows from the clouds onto the scene");
        private static readonly GUIContent cloudsShadowsRayMarchesContent = new GUIContent("Shadows Ray Marches", "The number of ray marches used to determine cloud density for shadows - increasing this value increases quality at the cost of performance");
        private static readonly GUIContent maxShadowStrengthContent = new GUIContent("Max Shadow Strength", "The maximum strength of shadows cast by clouds");
        private static readonly GUIContent sunContent = new GUIContent("Sun", "The light used to determine the direction in which shadows are cast - controlled by LBLighting if you have specified this camera as a ImageFX camera");

        #endregion

        #region Initialisation

        public void OnEnable()
        {
            // Auto-select the ImageFX shader
            if (((LBImageFX)target).imageFXShader == null)
            {
                ((LBImageFX)target).imageFXShader = Shader.Find("Hidden/LBImageFX");
            }

            #region Find Properties 
            //imageFXShaderProp = serializedObject.FindProperty("imageFXShader");

            // Fog properties
            showFogSettingsProp = serializedObject.FindProperty("showFogSettings");
            useUnityFogColourProp = serializedObject.FindProperty("useUnityFogColour");
            fogColourProp = serializedObject.FindProperty("fogColour");
            heightFogDensityProp = serializedObject.FindProperty("heightFogDensity");
            distanceFogDensityProp = serializedObject.FindProperty("distanceFogDensity");
            fogHeightProp = serializedObject.FindProperty("fogHeight");
            fogWaterLevelProp = serializedObject.FindProperty("fogWaterLevel");
            distanceBasedFogProp = serializedObject.FindProperty("distanceBasedFog");
            heightBasedFogProp = serializedObject.FindProperty("heightBasedFog");
            fogSkyboxProp = serializedObject.FindProperty("fogSkybox");
            maxFogIntensityProp = serializedObject.FindProperty("maxFogIntensity");
            fogSineAmplitudeProp = serializedObject.FindProperty("fogSineAmplitude");
            fogColourVarianceProp = serializedObject.FindProperty("fogColourVariance");

            // Rain Properties
            // showRainSettingsProp = serializedObject.FindProperty("showRainSettings");
            // rainDropsTextureProp = serializedObject.FindProperty("rainDropsTexture");
            // rainDropDistortionProp = serializedObject.FindProperty("rainDropDistortion");

            // Cloud Properties
            showCloudSettingsProp = serializedObject.FindProperty("showCloudSettings");
            renderCloudsProp = serializedObject.FindProperty("renderClouds");
            cloudsDetailAmountProp = serializedObject.FindProperty("cloudsDetailAmount");
            use3DNoiseProp = serializedObject.FindProperty("use3DNoise");
            isHDREnabledProp = serializedObject.FindProperty("isHDREnabled");
            cloudStyleProp = serializedObject.FindProperty("cloudStyle");
            cloudsStartHeightProp = serializedObject.FindProperty("cloudsStartHeight");
            cloudsEndHeightProp = serializedObject.FindProperty("cloudsEndHeight");
            // cloudsRayMarchesProp = serializedObject.FindProperty("cloudsRayMarches");
            cloudsQualityLevelProp = serializedObject.FindProperty("cloudsQualityLevel");
            // refinementRayMarchesProp = serializedObject.FindProperty("refinementRayMarches");
            cloudsTileSizeProp = serializedObject.FindProperty("cloudsTileSize");
            cloudsUpperColourProp = serializedObject.FindProperty("cloudsUpperColour");
            cloudsLowerColourProp = serializedObject.FindProperty("cloudsLowerColour");
            cloudsUpperColourHDRProp = serializedObject.FindProperty("cloudsUpperColourHDR");
            cloudsLowerColourHDRProp = serializedObject.FindProperty("cloudsLowerColourHDR");
            cloudsDensityProp = serializedObject.FindProperty("cloudsDensity");
            cloudsCoverageProp = serializedObject.FindProperty("cloudsCoverage");
            cloudsAnimationSpeedProp = serializedObject.FindProperty("cloudsAnimationSpeed");
            cloudsMorphingSpeedProp = serializedObject.FindProperty("cloudsMorphingSpeed");
            renderCloudShadowsProp = serializedObject.FindProperty("renderCloudShadows");
            cloudShadowsRayMarchesProp = serializedObject.FindProperty("cloudShadowsRayMarches");
            maxShadowStrengthProp = serializedObject.FindProperty("maxShadowStrength");
            sunProp = serializedObject.FindProperty("sun");

            // Enable when testing noise textures
#if LBImageFX_DEBUG_MODE
        perlinBaseTexProp = serializedObject.FindProperty("perlinBaseTex");
        perlinDetailTexProp = serializedObject.FindProperty("perlinDetailTex");
#endif

            // SSRR Properties
            showSSRRSettingsProp = serializedObject.FindProperty("showSSRRSettings");
            renderSSRRProp = serializedObject.FindProperty("SSRREnabled");
            PBRReflectionsProp = serializedObject.FindProperty("PBRReflections");
            SSRRDownsamplingProp = serializedObject.FindProperty("SSRRDownsampling");
            SSRRFilteringProp = serializedObject.FindProperty("SSRRFiltering");
            pixelStrideProp = serializedObject.FindProperty("pixelStride");
            maxRayMarchesProp = serializedObject.FindProperty("maxRayMarches");
            maxRayDistProp = serializedObject.FindProperty("maxRayDist");
            // maxRayStrideProp = serializedObject.FindProperty("maxRayStride");	
            screenFadeDistProp = serializedObject.FindProperty("screenFadeDist");
            fresnelFadeProp = serializedObject.FindProperty("fresnelFade");
            fresnelPowerProp = serializedObject.FindProperty("fresnelPower");
            reflectNearPixelsProp = serializedObject.FindProperty("reflectNearPixels");
            reflectFarPixelsProp = serializedObject.FindProperty("reflectFarPixels");
            blurStrengthProp = serializedObject.FindProperty("blurStrength");
            SSRRBlurQualityProp = serializedObject.FindProperty("SSRRBlurQuality");
            SSRRJitterProp = serializedObject.FindProperty("SSRRJitter");
            SSRRSettingsPresetProp = serializedObject.FindProperty("SSRRSettingsPreset");

            // Unity Project Settings
            isLinearColourSpaceProp = serializedObject.FindProperty("isLinearColourSpace");

            #endregion

            isNotSupported = (LBLandscape.IsURP(false) || LBLandscape.IsLWRP(false) || LBLandscape.IsHDRP(false));
        }

        #endregion

        // This function overides what is normally seen in the inspector window
        // This allows stuff like buttons to be drawn there
        public override void OnInspectorGUI()
        {
            #region Initialise
            // Instead of using target for fields and properties, should use SerializedProperties
            // May still need target to call methods etc.
            // http://docs.unity3d.com/ScriptReference/Editor.html
            //LBImageFX lbImageFX = (LBImageFX)target;

            // Reset at the start of each frame
            //isSceneSaveRequired = false;

            EditorGUIUtility.labelWidth = 150f;

            bool isCloudStyleModified = false;

            if (isNotSupported)
            {
                EditorGUILayout.HelpBox("LBImageFX is not supported on Universal, Lightweight or High Definition Render Pipeline", MessageType.Error);
            }

            // Read in all the properties
            serializedObject.Update();
            #endregion

            #region Linear Colorspace test
            // Prompt user to switch to Linear ColorSpace if using Gamma space.
            if (!isLinearColourSpaceProp.boolValue)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.HelpBox("Your project is using the older Gamma Colour Space. ImageFX prefers Linear colour space. Click 'Fix it' to change to linear colour space in Project Player Settings", MessageType.Warning, true);
                if (GUILayout.Button("Fix it", GUILayout.Width(50f)))
                {
                    bool originalRenderClouds = renderCloudsProp.boolValue;

                    // Turn off clouds first to prevent Unity Editor crash
                    renderCloudsProp.boolValue = false;

                    PlayerSettings.colorSpace = ColorSpace.Linear;
                    isLinearColourSpaceProp.boolValue = true;

                    // Restore previous cloud settings
                    renderCloudsProp.boolValue = originalRenderClouds;
                }
                EditorGUILayout.EndHorizontal();
            }
            #endregion

            #region Fog Properties
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Fog Settings", EditorStyles.boldLabel);
            if (showFogSettingsProp.boolValue) { if (GUILayout.Button("Hide", GUILayout.Width(50f))) { showFogSettingsProp.boolValue = false; } }
            else { if (GUILayout.Button("Show", GUILayout.Width(50f))) { showFogSettingsProp.boolValue = true; } }
            EditorGUILayout.EndHorizontal();
            if (showFogSettingsProp.boolValue)
            {
                EditorGUILayout.PropertyField(useUnityFogColourProp, fogUseUnityFogColourContent);               
                if (!useUnityFogColourProp.boolValue)
                {
                    EditorGUILayout.PropertyField(fogColourProp, fogColourContent);
                }
                EditorGUILayout.PropertyField(distanceBasedFogProp, fogDistanceBasedFogContent);
                if (distanceBasedFogProp.boolValue)
                {
                    EditorGUILayout.PropertyField(fogWaterLevelProp, fogWaterLevelContent);
                    EditorGUILayout.PropertyField(distanceFogDensityProp, fogDistanceFogDensityContent);
                }
                EditorGUILayout.PropertyField(heightBasedFogProp, fogHeightBasedFogContent);
                if (heightBasedFogProp.boolValue)
                {
                    EditorGUILayout.PropertyField(heightFogDensityProp, fogHeightFogDensityContent);                  
                    EditorGUILayout.PropertyField(fogHeightProp, fogMaxHeightContent);
                    EditorGUILayout.PropertyField(fogSineAmplitudeProp, fogSineAmplitudeContent);
                }
                if (distanceBasedFogProp.boolValue || heightBasedFogProp.boolValue)
                {
                    EditorGUILayout.PropertyField(fogSkyboxProp, fogSkyBoxContent);
                    EditorGUILayout.PropertyField(maxFogIntensityProp, fogMaxFogIntensityContent);
                    EditorGUILayout.PropertyField(fogColourVarianceProp, fogColourVarianceContent);
                }
            }
            EditorGUILayout.EndVertical();
            #endregion

            #region Rain Properties (UNUSED)
            // EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            // EditorGUILayout.BeginHorizontal();
            // EditorGUILayout.LabelField("Rain Settings", EditorStyles.boldLabel);
            // if (showRainSettingsProp.boolValue) { if (GUILayout.Button("Hide", GUILayout.Width(50f))) { showRainSettingsProp.boolValue = false; } }
            // else { if (GUILayout.Button("Show", GUILayout.Width(50f))) { showRainSettingsProp.boolValue = true; } }
            // EditorGUILayout.EndHorizontal();
            // if (showRainSettingsProp.boolValue)
            // {
            //     EditorGUILayout.PropertyField(rainDropsTextureProp, new GUIContent("Rain Drops Texture"));
            //     EditorGUILayout.PropertyField(rainDropDistortionProp, new GUIContent("Rain Drop Distortion"));
            // }
            // EditorGUILayout.EndVertical();
            #endregion

            #region Cloud Properties
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Cloud Settings", EditorStyles.boldLabel);
            if (showCloudSettingsProp.boolValue) { if (GUILayout.Button("Hide", GUILayout.Width(50f))) { showCloudSettingsProp.boolValue = false; } }
            else { if (GUILayout.Button("Show", GUILayout.Width(50f))) { showCloudSettingsProp.boolValue = true; } }
            EditorGUILayout.EndHorizontal();
            if (showCloudSettingsProp.boolValue)
            {
                EditorGUILayout.PropertyField(renderCloudsProp, renderCloudsContent);
                if (renderCloudsProp.boolValue)
                {
                    EditorGUILayout.PropertyField(use3DNoiseProp, use3DNoiseContent);
                    bool prevIsHDREnabled = isHDREnabledProp.boolValue;
                    EditorGUILayout.PropertyField(isHDREnabledProp, isHDREnabledContent);
                    if (prevIsHDREnabled != isHDREnabledProp.boolValue)
                    {
                        // Copy values
                        if (isHDREnabledProp.boolValue)
                        {
                            cloudsUpperColourHDRProp.colorValue = cloudsUpperColourProp.colorValue;
                            cloudsLowerColourHDRProp.colorValue = cloudsLowerColourProp.colorValue;

                        }
                        else
                        {
                            cloudsUpperColourProp.colorValue = LBLighting.ConvertColourFromHDR(cloudsUpperColourHDRProp.colorValue);
                            cloudsLowerColourProp.colorValue = LBLighting.ConvertColourFromHDR(cloudsLowerColourHDRProp.colorValue);
                        }
                    }

                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(cloudStyleProp, cloudStyleContent);
                    isCloudStyleModified = EditorGUI.EndChangeCheck();
                    EditorGUILayout.PropertyField(cloudsStartHeightProp, cloudsStartHeightContent);
                    EditorGUILayout.PropertyField(cloudsEndHeightProp, cloudsEndHeightContent);
                    if (cloudsEndHeightProp.floatValue > 1.25f * cloudsStartHeightProp.floatValue)
                    {
                        EditorGUILayout.HelpBox("Clouds End Height recommended to be less than or equal or to 125% of Clouds Start Height", MessageType.Warning, true);
                    }
                    EditorGUILayout.PropertyField(cloudsTileSizeProp, cloudsTileSizeContent);

                    if (isHDREnabledProp.boolValue)
                    {
                        EditorGUILayout.PropertyField(cloudsUpperColourHDRProp, cloudsUpperColourContent);
                        EditorGUILayout.PropertyField(cloudsLowerColourHDRProp, cloudsLowerColourContent);
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(cloudsUpperColourProp, cloudsUpperColourContent);
                        EditorGUILayout.PropertyField(cloudsLowerColourProp, cloudsLowerColourContent);
                    }

                    EditorGUILayout.PropertyField(cloudsQualityLevelProp, cloudsQualityLevelContent);
                    EditorGUILayout.PropertyField(cloudsDensityProp, cloudsDensityContent);
                    EditorGUILayout.PropertyField(cloudsCoverageProp, cloudsCoverageContent);
                    EditorGUILayout.PropertyField(cloudsDetailAmountProp, cloudsDetailAmountContent);
                    // For some unknown reason, using a Vector2 with a PropertyField and HideInInspector produces
                    // stange results and errors. Use a Vector2Field as a workaround
                    cloudsAnimationSpeedVector2 = cloudsAnimationSpeedProp.vector2Value;
                    EditorGUI.BeginChangeCheck();
                    cloudsAnimationSpeedVector2 = EditorGUILayout.Vector2Field(cloudsAnimationSpeedContent, cloudsAnimationSpeedVector2);
                    if (EditorGUI.EndChangeCheck()) { cloudsAnimationSpeedProp.vector2Value = cloudsAnimationSpeedVector2; }
                    if (use3DNoiseProp.boolValue) { EditorGUILayout.PropertyField(cloudsMorphingSpeedProp, cloudsMorphingSpeedContent); }
                    EditorGUILayout.PropertyField(renderCloudShadowsProp, cloudsCastShadowsContent);
                    if (renderCloudShadowsProp.boolValue)
                    {
                        EditorGUILayout.PropertyField(cloudShadowsRayMarchesProp, cloudsShadowsRayMarchesContent);
                        EditorGUILayout.PropertyField(maxShadowStrengthProp, maxShadowStrengthContent);
                        EditorGUILayout.PropertyField(sunProp, sunContent);
                    }

#if LBImageFX_DEBUG_MODE
                // Typically, these should not be user-configurable - so we won't display them
                EditorGUILayout.PropertyField(perlinBaseTexProp, new GUIContent("Perlin Base Texture"));
                EditorGUILayout.PropertyField(perlinDetailTexProp, new GUIContent("Perlin Detail Texture"));
#endif
                }
            }
            EditorGUILayout.EndVertical();
            #endregion

            #region SSRR Properties
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("SSRR Settings", EditorStyles.boldLabel);
            if (showSSRRSettingsProp.boolValue) { if (GUILayout.Button("Hide", GUILayout.Width(50f))) { showSSRRSettingsProp.boolValue = false; } }
            else { if (GUILayout.Button("Show", GUILayout.Width(50f))) { showSSRRSettingsProp.boolValue = true; } }
            EditorGUILayout.EndHorizontal();
            if (showSSRRSettingsProp.boolValue)
            {
                EditorGUILayout.PropertyField(renderSSRRProp, new GUIContent("Use SSRR", "Whether SSRR is enabled"));
                if (renderSSRRProp.boolValue)
                {
                    bool usingDeferred = true;
                    if (((LBImageFX)target).cam != null) { usingDeferred = ((LBImageFX)target).cam.actualRenderingPath == RenderingPath.DeferredShading; }
                    if (usingDeferred) { EditorGUILayout.PropertyField(PBRReflectionsProp, new GUIContent("PBR Reflections", "Whether the reflections use PBR calculations. This option is only available for the deferred rendering path")); }
                    EditorGUILayout.PropertyField(reflectNearPixelsProp, new GUIContent("Reflect Near Pixels", "Enable this to allow near pixels to be reflected - this should always be enabled unless you only want to reflect far pixels"));
                    EditorGUILayout.PropertyField(reflectFarPixelsProp, new GUIContent("Reflect Far Pixels", "Enable this to allow far pixels to be reflected - helpful if the skybox (or other far pixels) are written to after reflection probes are calculated"));
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(SSRRSettingsPresetProp, new GUIContent("Preset", "The preset to use for SSRR settings"));
                    if (EditorGUI.EndChangeCheck())
                    {
                        string selectedEnumName = SSRRSettingsPresetProp.enumNames[SSRRSettingsPresetProp.enumValueIndex];
                        // If the preset is changed, change the settings
                        if (selectedEnumName == "ExtremePerformance")
                        {
                            SSRRDownsamplingProp.enumValueIndex = 3;
                            SSRRFilteringProp.boolValue = true;
                            pixelStrideProp.intValue = 10;
                            maxRayMarchesProp.intValue = 15;
                            maxRayDistProp.floatValue = 50f;
                            fresnelFadeProp.floatValue = 2f;
                            fresnelPowerProp.floatValue = 4f;
                            SSRRJitterProp.boolValue = true;
                            blurStrengthProp.floatValue = 0.3f;
                            SSRRBlurQualityProp.enumValueIndex = 0;
                        }
                        else if (selectedEnumName == "Performance")
                        {
                            SSRRDownsamplingProp.enumValueIndex = 1;
                            SSRRFilteringProp.boolValue = true;
                            pixelStrideProp.intValue = 15;
                            maxRayMarchesProp.intValue = 25;
                            maxRayDistProp.floatValue = 50f;
                            fresnelFadeProp.floatValue = 1f;
                            fresnelPowerProp.floatValue = 1f;
                            SSRRJitterProp.boolValue = true;
                            blurStrengthProp.floatValue = 0f;
                            SSRRBlurQualityProp.enumValueIndex = 0;
                        }
                        else if (selectedEnumName == "Balanced")
                        {
                            SSRRDownsamplingProp.enumValueIndex = 1;
                            SSRRFilteringProp.boolValue = true;
                            pixelStrideProp.intValue = 10;
                            maxRayMarchesProp.intValue = 40;
                            maxRayDistProp.floatValue = 100f;
                            fresnelFadeProp.floatValue = 1f;
                            fresnelPowerProp.floatValue = 1f;
                            SSRRJitterProp.boolValue = true;
                            blurStrengthProp.floatValue = 0.3f;
                            SSRRBlurQualityProp.enumValueIndex = 1;
                        }
                        else if (selectedEnumName == "Quality")
                        {
                            SSRRDownsamplingProp.enumValueIndex = 0;
                            SSRRFilteringProp.boolValue = true;
                            pixelStrideProp.intValue = 10;
                            maxRayMarchesProp.intValue = 75;
                            maxRayDistProp.floatValue = 200f;
                            fresnelFadeProp.floatValue = 0.5f;
                            fresnelPowerProp.floatValue = 1f;
                            SSRRJitterProp.boolValue = true;
                            blurStrengthProp.floatValue = 0f;
                            SSRRBlurQualityProp.enumValueIndex = 0;
                        }
                        else if (selectedEnumName == "ExtremeQuality")
                        {
                            SSRRDownsamplingProp.enumValueIndex = 0;
                            SSRRFilteringProp.boolValue = true;
                            pixelStrideProp.intValue = 5;
                            maxRayMarchesProp.intValue = 150;
                            maxRayDistProp.floatValue = 200f;
                            fresnelFadeProp.floatValue = 0.5f;
                            fresnelPowerProp.floatValue = 1f;
                            SSRRJitterProp.boolValue = true;
                            blurStrengthProp.floatValue = 0.3f;
                            SSRRBlurQualityProp.enumValueIndex = 2;
                        }
                        else
                        {
                            SSRRDownsamplingProp.enumValueIndex = 0;
                            SSRRFilteringProp.boolValue = true;
                            pixelStrideProp.intValue = 1;
                            maxRayMarchesProp.intValue = 1000;
                            maxRayDistProp.floatValue = 300f;
                            fresnelFadeProp.floatValue = 0.5f;
                            fresnelPowerProp.floatValue = 1f;
                            SSRRJitterProp.boolValue = true;
                            blurStrengthProp.floatValue = 0.1f;
                            SSRRBlurQualityProp.enumValueIndex = 2;
                        }
                    }
                    EditorGUILayout.PropertyField(SSRRDownsamplingProp, new GUIContent("Downsampling", "How much the reflection textures are downsampled - increasing this will increase performance but will reduce quality"));
                    if (SSRRDownsamplingProp.enumValueIndex != 0)
                    {
                        EditorGUILayout.PropertyField(SSRRFilteringProp, new GUIContent("Filtering", "Whether the reflection textures are filtered - enabling this will increase quality at a slight performance cost"));
                    }
                    if (reflectNearPixelsProp.boolValue)
                    {
                        EditorGUILayout.PropertyField(pixelStrideProp, new GUIContent("Pixel Stride", "The length in pixels of each (near pixel) ray stride - increase this value to improve performance at the cost of visual fidelity"));
                        EditorGUILayout.PropertyField(maxRayMarchesProp, new GUIContent("Max Ray Marches", "The maximum number of (near pixel) ray marches to be performed per pixel - increasing this value allows for reflections of objects further away but will increase performance cost"));
                        EditorGUILayout.PropertyField(maxRayDistProp, new GUIContent("Max Ray Dist.", "The maximum ray distance in metres - increasing this value allows for reflections of objects further away but will increase performance hit"));
                        //EditorGUILayout.PropertyField(maxRayStrideProp, new GUIContent("Max Ray Stride", "The maximum ray stride as percentage of distance from camera - decreasing this value can improve visual clarity but costs some performance"));
                    }
                    EditorGUILayout.PropertyField(screenFadeDistProp, new GUIContent("Screen Fade Dist.", "The distance over which reflections of pixels leaving the screen are faded out"));
                    EditorGUILayout.PropertyField(fresnelFadeProp, new GUIContent("Fresnel Fade", "The factor by which reflections are faded out due to the fresnel effect - increase this to increase fading of reflections further 'into' objects"));
                    EditorGUILayout.PropertyField(fresnelPowerProp, new GUIContent("Fresnel Power", "The speed at which reflections are faded out due to the fresnel effect - increase this to fade reflections faster as they go further 'into' objects"));
                    if (reflectNearPixelsProp.boolValue)
                    {
                        EditorGUILayout.PropertyField(SSRRJitterProp, new GUIContent("Jitter", "Whether jitter is added to reflection rays - enabling this can help to remove jagged artefacts"));
                    }
                    EditorGUILayout.PropertyField(blurStrengthProp, new GUIContent("Blur Strength", "The strength of blurring for reflections - having this set to more than zero will turn blur on and decrease performance slightly"));
                    if (blurStrengthProp.floatValue > 0f) { EditorGUILayout.PropertyField(SSRRBlurQualityProp, new GUIContent("Blur Quality", "The quality of SSRR blur - setting this to medium or high will increase quality at the expense of performance")); }
                    if (!reflectNearPixelsProp.boolValue && !reflectFarPixelsProp.boolValue) { reflectNearPixelsProp.boolValue = true; }

                    if (GUILayout.Button(new GUIContent("Reset", "Reset SSRR Settings to factory defaults"), GUILayout.Width(60f)))
                    {
                        PBRReflectionsProp.boolValue = ((LBImageFX)target).GetDefaultPBRReflections;
                        SSRRDownsamplingProp.intValue = (int)((LBImageFX)target).GetDefaultSSRRDownsampling;
                        SSRRFilteringProp.boolValue = ((LBImageFX)target).GetDefaultSSRRFiltering;
                        pixelStrideProp.intValue = ((LBImageFX)target).GetDefaultPixelStride;
                        maxRayMarchesProp.intValue = ((LBImageFX)target).GetDefaultMaxRayMarches;
                        maxRayDistProp.floatValue = ((LBImageFX)target).GetDefaultMaxRayDist;
                        screenFadeDistProp.floatValue = ((LBImageFX)target).GetDefaultScreenFadeDist;
                        fresnelFadeProp.floatValue = ((LBImageFX)target).GetDefaultFresnelFade;
                        fresnelPowerProp.floatValue = ((LBImageFX)target).GetDefaultFresnelPower;
                        blurStrengthProp.floatValue = ((LBImageFX)target).GetDefaultBlurStrength;
                        SSRRBlurQualityProp.intValue = (int)((LBImageFX)target).GetDefaultSSRRBlurQuality;
                        SSRRJitterProp.boolValue = ((LBImageFX)target).GetDefaultSSRRJitter;
                        reflectNearPixelsProp.boolValue = ((LBImageFX)target).GetDefaultReflectNearPixels;
                        reflectFarPixelsProp.boolValue = ((LBImageFX)target).GetDefaultRelectFarPixels;
                        SSRRSettingsPresetProp.intValue = (int)((LBImageFX)target).GetDefaultSSRRSettingsPreset;
                    }
                }
            }
            EditorGUILayout.EndVertical();
            #endregion

            // Apply any property changes
            serializedObject.ApplyModifiedProperties();

            if (isCloudStyleModified)
            {
                ((LBImageFX)target).SetCloudStyleTextures();
            }

            // Must be before or after serializeObject.Update/ApplyModifiedProperties pair
            //DrawDefaultInspector();

            // Some of the controls have changed, so mark the scene as changed so that
            // if it is run without the LBImageFX editor script open, the values will persist.
            //if (isSceneSaveRequired && !Application.isPlaying)
            //{
            //    isSceneSaveRequired = false;
            //    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            //}
        }
    }
#endif
    #endregion
}