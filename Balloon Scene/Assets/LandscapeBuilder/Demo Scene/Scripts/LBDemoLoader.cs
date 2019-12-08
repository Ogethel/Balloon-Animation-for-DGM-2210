// Landscape Builder. Copyright (c) 2016-2019 SCSM Pty Ltd. All rights reserved.
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace LandscapeBuilder
{
    public class LBDemoLoader : MonoBehaviour
    {
        #region Public Variables
        public bool ignoreStartPosition = true;
        public Transform waterPrefab;
        public LBTemplate[] lbTemplates;

        [Header("UI Controls")]
        public GameObject infoPanel;
        public Text infoPanelTitle;
        public Text infoPanelText;
        public GameObject backgroundPanel;
        public GameObject menuPanel;
        public Button template1Button;
        public Button template2Button;
        public Button template3Button;
        public Button template4Button;
        public Button infoButton;
        public Button quality1Button;
        public Button quality2Button;
        public Button quality3Button;
        public Button quality4Button;
        public Button gpuButton;

        public Color selectedBackgroundColour = new Color(155f / 255f, 233f / 255f, 255f / 255f);
        public Color unselectedBackgroundColour = Color.white;

        [Header("Assets")]
        public Texture2D[] textures;
        public Material[] materials;
        #endregion

        #region Private Variables
        private LBLandscape lbLandscape;
        private Camera uiCamera = null;
        private string currentTemplateName = string.Empty;
        private bool enableGPU = false;
        private bool isLWRP = false;
        private bool isURP = false;
        private bool isHDRP = false;
        #if UNITY_2019_2_OR_NEWER
        private bool is201920Plus = true;
        #else
        private bool is201920Plus = false;
        #endif
        #endregion

        #region Initialisation Methods
        void Awake()
        {
            // find the first landscape in the scene
            //lbLandscape = GameObject.FindObjectOfType<LBLandscape>();
            //if (lbLandscape == null) { Debug.LogWarning("LBDemoLoader.Awake - could not find landscape - did you delete it?"); }

            uiCamera = Camera.main;
            SetCameraCullingMask();

            InitialiseQualityButtonText();

            // If 5 or more levels assume "Beautiful" (4) is still available
            if (QualitySettings.names.Length > 4)
            {
                SetQuality(4);
            }
            else { SetQuality(QualitySettings.GetQualityLevel()); }

            if (SystemInfo.supportsComputeShaders && SystemInfo.supports2DArrayTextures)
            {
                #if (UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN || UNITY_XBOXONE || UNITY_WSA_10_0)
                enableGPU = true;
                if (gpuButton != null) { SetButtonColour(gpuButton, selectedBackgroundColour); }
                #else
                enableGPU = false;
                #endif
            }
            else
            {
                // If Compute Shaders are not supported, turn off the GPU button
                if (gpuButton != null) { gpuButton.gameObject.SetActive(false); }
                enableGPU = false;
            }

            // Check if URP, LWRP, or HDRP pipelines are in use
            isURP = LBLandscape.IsURP(false);
            isLWRP = !isURP && LBLandscape.IsLWRP(false);
            isHDRP = !isURP && !isLWRP && LBLandscape.IsHDRP(false);

            // Starts being off
            DisplayInfoPanel(false);
            // Toggle the info ON
            ShowInfo();
        }

        private void InitialiseQualityButtonText()
        {
            // Default for Unity 5 and Unity 2017 is 6 (0-5)
            if (QualitySettings.names.Length > 5)
            {
                SetButtonTitle(quality1Button, QualitySettings.names[2]);
                SetButtonTitle(quality2Button, QualitySettings.names[3]);
                SetButtonTitle(quality3Button, QualitySettings.names[4]);
                SetButtonTitle(quality4Button, QualitySettings.names[5]);
            }
        }
        #endregion

        #region Public Methods

        public void LoadTemplate(int templateNumber)
        {
            switch (templateNumber)
            {
                case 0:
                    SetButtonColour(template1Button, selectedBackgroundColour);
                    SetButtonColour(template2Button, unselectedBackgroundColour);
                    SetButtonColour(template3Button, unselectedBackgroundColour);
                    SetButtonColour(template4Button, unselectedBackgroundColour);
                    break;
                case 1:
                    SetButtonColour(template1Button, unselectedBackgroundColour);
                    SetButtonColour(template2Button, selectedBackgroundColour);
                    SetButtonColour(template3Button, unselectedBackgroundColour);
                    SetButtonColour(template4Button, unselectedBackgroundColour);
                    break;
                case 2:
                    SetButtonColour(template1Button, unselectedBackgroundColour);
                    SetButtonColour(template2Button, unselectedBackgroundColour);
                    SetButtonColour(template3Button, selectedBackgroundColour);
                    SetButtonColour(template4Button, unselectedBackgroundColour);
                    break;
                case 3:
                    SetButtonColour(template1Button, unselectedBackgroundColour);
                    SetButtonColour(template2Button, unselectedBackgroundColour);
                    SetButtonColour(template3Button, unselectedBackgroundColour);
                    SetButtonColour(template4Button, selectedBackgroundColour);
                    break;
            }

            SetCameraCullingMask();

            StartCoroutine(LoadTemplateWithUIDelay(templateNumber));
        }

        /// <summary>
        /// Allow the Info page to be toggled on/off
        /// </summary>
        public void ShowInfo()
        {
            if (infoPanel != null)
            {
                bool isVisible = infoPanel.activeSelf;
                string buttonText = "Hide Info";

                // Toggle on/off
                isVisible = !isVisible;
                infoPanel.SetActive(isVisible);

                if (isVisible)
                {
                    SetInfoTitle("Demo Template Information");

                    string infoText = "Templates are small Landscape Builder prefabs that contain the information required to reconstruct ";
                    infoText += "a landscape. Templates get applied to empty landscapes or can overwrite existing landscapes settings.\n\n";
                    infoText += "They can be applied to a landscape using the Landscape Builder Editor so that you can ";
                    infoText += "interact with it in the scene view and modify it's configuration. ";
                    infoText += "They are small enough to be packaged and emailed to other LB users as they ";
                    infoText += "only contain the landscape meta-data, not the actual assets.\n\n";
                    infoText += "To build and edit your landscapes you don't need to use templates, the data is stored in the scene; they are ";
                    infoText += "simply an easy way to move landscape meta-data between projects or computers anywhere in the world.\n\n";
                    infoText += "These Demo Templates only use standard assets but your scenes can use your own or any of the great 3rd party assets from the Unity Asset Store.";

                    SetInfoText(infoText, TextAnchor.UpperLeft);
                }
                else { buttonText = "Show Info"; }

                UpdateInfoButtonText(buttonText);
            }
        }

        /// <summary>
        /// This assumes still have the default Unity Project Settings
        /// </summary>
        /// <param name="qualityLevel"></param>
        public void SetQuality(int qualityLevel)
        {
            if (qualityLevel < QualitySettings.names.Length)
            {
                QualitySettings.SetQualityLevel(qualityLevel, true);
                switch (qualityLevel)
                {
                    case 2:
                        SetButtonColour(quality1Button, selectedBackgroundColour);
                        SetButtonColour(quality2Button, unselectedBackgroundColour);
                        SetButtonColour(quality3Button, unselectedBackgroundColour);
                        SetButtonColour(quality4Button, unselectedBackgroundColour);
                        break;
                    case 3:
                        SetButtonColour(quality1Button, unselectedBackgroundColour);
                        SetButtonColour(quality2Button, selectedBackgroundColour);
                        SetButtonColour(quality3Button, unselectedBackgroundColour);
                        SetButtonColour(quality4Button, unselectedBackgroundColour);
                        break;
                    case 4:
                        SetButtonColour(quality1Button, unselectedBackgroundColour);
                        SetButtonColour(quality2Button, unselectedBackgroundColour);
                        SetButtonColour(quality3Button, selectedBackgroundColour);
                        SetButtonColour(quality4Button, unselectedBackgroundColour);
                        break;
                    case 5:
                        SetButtonColour(quality1Button, unselectedBackgroundColour);
                        SetButtonColour(quality2Button, unselectedBackgroundColour);
                        SetButtonColour(quality3Button, unselectedBackgroundColour);
                        SetButtonColour(quality4Button, selectedBackgroundColour);
                        break;
                }

                // The old Unity 4 trees don't work so well with Anti-Aliasing.
                if (currentTemplateName.StartsWith("FPS Forest Demo")) { QualitySettings.antiAliasing = 0; }
            }
            else { Debug.LogWarning("LBDemoLoader.SetQuality - could not set quality level as you may have customised them in Project Settings"); }
        }

        /// <summary>
        /// Toggle GPU creation
        /// </summary>
        public void ToggleGPU()
        {
            enableGPU = !enableGPU;

            if (gpuButton != null)
            {
                // Highlight the button if turned on
                if (enableGPU)
                {
                    SetButtonColour(gpuButton, selectedBackgroundColour);
                }
                else
                {
                    SetButtonColour(gpuButton, unselectedBackgroundColour);
                }
            }
        }


        #endregion

        #region Private Methods

        private void SetCameraCullingMask()
        {
            // Set the culling mask for the built-in UI layer (5)
            uiCamera.cullingMask = (1 << 5);
        }

        private void EnableButton(Button button, bool isEnabled)
        {
            if (button != null) { button.interactable = isEnabled; }
        }

        private void SetButtonColour(Button button, Color colour)
        {
            if (button != null)
            {
                button.image.color = colour;
            }
        }

        /// <summary>
        /// Set the title of a button, with a max length of 9 characters
        /// </summary>
        /// <param name="button"></param>
        /// <param name="title"></param>
        private void SetButtonTitle(Button button, string title)
        {
            if (button != null && !string.IsNullOrEmpty(title))
            {
                Text text = button.GetComponentInChildren<Text>();
                if (text != null)
                {
                    text.text = title.Substring(0, title.Length <= 9 ? title.Length : 9);
                }
            }
        }

        /// <summary>
        /// Load a template but delay short period of time after updating
        /// the infoPanel text to force it to be displayed
        /// </summary>
        /// <param name="templateNumber"></param>
        /// <returns></returns>
        IEnumerator LoadTemplateWithUIDelay(int templateNumber)
        {
            if (lbTemplates != null)
            {
                int numTemplates = lbTemplates.Length;

                if (templateNumber < numTemplates)
                {
                    LBTemplate lbTemplate = lbTemplates[templateNumber];

                    if (lbTemplate != null)
                    {
                        // Disable buttons
                        EnableButton(quality1Button, false);
                        EnableButton(quality2Button, false);
                        EnableButton(quality3Button, false);
                        EnableButton(quality4Button, false);
                        EnableButton(infoButton, false);
                        EnableButton(gpuButton, false);
                        DisplayBackgroundPanel(true);

                        currentTemplateName = lbTemplate.name;

                        SetInfoTitle("Loading Template... PLEASE WAIT");

                        string infoText = "Applying template... ";

                        SetInfoText(infoText);
                        DisplayInfoPanel(true);
                        yield return new WaitForSeconds(0.1f);

                        // Remove any camera animators
                        LBCameraAnimator.RemoveCameraAnimatorsFromScene(lbLandscape, false);
                        yield return new WaitForSeconds(0.1f);

                        // if LBLighting is in the scene, remove all references to terrains to
                        // prevent errors when the current landscape is deleted.
                        LBLighting lbLighting = GameObject.FindObjectOfType<LBLighting>();
                        if (lbLighting != null) { lbLighting.ClearAllTerrains(); }

                        if (lbLandscape != null) { DestroyImmediate(lbLandscape.gameObject); }
                        lbLandscape = lbTemplate.CreateLandscapeFromTemplate("DemoLandscape");
                        Camera mainCamera = Camera.main;

                        if (mainCamera != null)
                        {
                            mainCamera.transform.position = new Vector3(2000f, 2000f, 0f);
                            mainCamera.transform.LookAt(lbLandscape.transform);
                        }

                        // Currently, when applying template ignore water if LWRP, URP or HDRP are enabled
                        // This could be updated if we had a LWRP/HDRP-compatible water asset
                        if (lbTemplate.ApplyTemplateToLandscape(lbLandscape, ignoreStartPosition, true, (isURP || isLWRP || isHDRP) ? null : waterPrefab, true))
                        {
                            infoText += "DONE\n\n";
                            SetInfoText(infoText);
                            yield return new WaitForSeconds(0.1f);

                            lbLandscape.showTiming = false;
                            lbLandscape.SetLandscapeTerrains(true);

                            // Build the landscape
                            int numTerrains = (lbLandscape.landscapeTerrains == null ? 0 : lbLandscape.landscapeTerrains.Length);
                            if (numTerrains > 0)
                            {
                                // Override the template GPU settings
                                lbLandscape.useGPUTopography = false;
                                lbLandscape.useGPUTexturing = enableGPU;
                                lbLandscape.useGPUGrass = enableGPU;
                                lbLandscape.useGPUPath = false;

                                // Check for URP/LWRP/HDRP or do we need to create a default material for U2019.2.0 or newer
                                if (isURP || isLWRP || isHDRP || is201920Plus)
                                {
                                    float pixelError = 0f;
                                    Terrain terrain = null;
                                    LBLandscape.TerrainMaterialType terrainMaterialType = isURP ? LBLandscape.TerrainMaterialType.URP : (isLWRP ? LBLandscape.TerrainMaterialType.LWRP : (terrainMaterialType = isHDRP ? LBLandscape.TerrainMaterialType.HDRP : LBLandscape.TerrainMaterialType.BuiltInStandard));

                                    for (int tIdx = 0; tIdx < numTerrains; tIdx++)
                                    {
                                        terrain = lbLandscape.landscapeTerrains[tIdx];
                                        lbLandscape.SetTerrainMaterial(terrain, tIdx, (tIdx == numTerrains - 1), terrain.terrainData.size.x, ref pixelError, terrainMaterialType);
                                    }
                                }

                                // These are the cut-down versions without progress bars and minimal validation
                                infoText += "Building Topography... ";
                                SetInfoText(infoText);
                                yield return new WaitForSeconds(0.1f);
                                lbLandscape.ApplyTopography(true, true);
                                infoText += "DONE\n\n";
                                infoText += "Adding Textures" + (lbLandscape.useGPUTexturing ? " (GPU)..." : "...");
                                SetInfoText(infoText);
                                yield return new WaitForSeconds(0.1f);
                                lbLandscape.ApplyTextures(true, true);
                                infoText += "DONE\n\n";
                                infoText += "Placing Trees... ";
                                SetInfoText(infoText);
                                yield return new WaitForSeconds(0.1f);
                                lbLandscape.ApplyTrees(true, true);
                                infoText += "DONE\n\n";
                                infoText += "Placing Grass" + (lbLandscape.useGPUGrass ? " (GPU)..." : "...");
                                SetInfoText(infoText);
                                yield return new WaitForSeconds(0.1f);
                                lbLandscape.ApplyGrass(true, true);
                                infoText += "DONE\n\n";
                                SetInfoText(infoText);
                                yield return new WaitForSeconds(0.1f);

                                // Apply lighting last as it needs the Camera Paths (and Animator camera to set up WeatherFX)
                                if (lbTemplate.isLBLightingIncluded)
                                {
                                    infoText += "Applying lighting... ";
                                    SetInfoText(infoText);
                                    yield return new WaitForSeconds(0.1f);

                                    //if (lbLighting != null) { DestroyImmediate(lbLighting.gameObject); }

                                    if (lbLighting == null)
                                    {
                                        // If LBLighting isn't already in the scene, add it.
                                        lbLighting = LBLighting.AddLightingToScene(true);
                                    }

                                    if (lbLighting != null)
                                    {
                                        // Restore the lighting settings from the template
                                        lbTemplate.ApplyLBLightingSettings(lbLandscape, ref lbLighting, mainCamera, true);
                                    }
                                    infoText += "DONE\n\n";
                                    SetInfoText(infoText);
                                    yield return new WaitForSeconds(0.1f);

                                    // Prior to Unity 5.4, ImageFX and/or camera clear flags are not updated correctly
                                    // when the WeatherFX with celestials is added to the scene after a configuration without
                                    // celestials. The workaround, is to force the GameView to repaint or be re-initalised.
                                    // To be effective, a "reasonable" delay is required between switching away from Game view
                                    // and back. The exact cause of the issue is unknown...
                                    #if !UNITY_5_4_OR_NEWER && UNITY_EDITOR
                                    if (lbLighting.useCelestials)
                                    {
                                        bool wasMaximized = LBEditorHelper.GameViewMaximize(this.GetType(), false);
                                        LBEditorHelper.ShowSceneView(this.GetType(), true);
                                        yield return new WaitForSeconds(0.5f);

                                        LBEditorHelper.GameViewMaximize(this.GetType(), wasMaximized);
                                    }
                                    #endif
                                }

                                // Find the first animation and start it (there should be only one in a demo template)
                                LBCameraAnimator lbCameraAnimator = LBCameraAnimator.GetFirstCameraAnimatorInLandscape(lbLandscape);
                                if (lbCameraAnimator != null)
                                {
                                    if (lbTemplate.name.StartsWith("FPS Forest Demo"))
                                    {
                                        // This uses the old Unity 4 trees which don't work so well with anti-aliasing
                                        QualitySettings.antiAliasing = 0;

                                        if (lbCameraAnimator.animatorCamera != null)
                                        {
                                            lbCameraAnimator.SetMoveSpeed(2f);
                                            lbCameraAnimator.animatorCamera.renderingPath = RenderingPath.Forward;
                                            lbCameraAnimator.animateSpeed = false;
                                        }
                                    }
                                    else
                                    {
                                        lbCameraAnimator.animateSpeed = true;
                                    }

                                    // Reset LBImageFX timing so clouds always appear the same
                                    if (lbCameraAnimator.animatorCamera != null)
                                    {
                                        LBImageFX lbImageFX = lbCameraAnimator.animatorCamera.GetComponent<LBImageFX>();
                                        if (lbImageFX != null)
                                        {
                                            //Debug.Log("LBDemoLoader.LoadTemplateWithUIDelay - Resetting LBImageFX cloud positions");
                                            lbImageFX.ResetCloudPosition();
                                        }
                                    }

                                    //Debug.Log("LBDemoLoader " + lbTemplate.name + " renderpath: " + lbCameraAnimator.animatorCamera.renderingPath);

                                    lbCameraAnimator.pauseAtEndDuration = 999f;
                                    lbCameraAnimator.BeginAnimation(true, 0f);
                                }
                                else { Debug.LogWarning("LBDemoLoader.LoadTemplateWithUIDelay - Couldn't find a camera animator in the demo template"); }
                            }
                        }

                        // Re-enable buttons
                        EnableButton(quality1Button, true);
                        EnableButton(quality2Button, true);
                        EnableButton(quality3Button, true);
                        EnableButton(quality4Button, true);
                        EnableButton(infoButton, true);
                        EnableButton(gpuButton, true);
                        UpdateInfoButtonText("Show Info");
                    }
                }
            }
            DisplayInfoPanel(false);
            DisplayBackgroundPanel(false);
        }

        private void SetInfoText(string infoText, TextAnchor textAlignment = TextAnchor.UpperCenter)
        {
            if (infoPanelText != null)
            {
                infoPanelText.text = infoText;
                infoPanelText.alignment = textAlignment;
                Canvas.ForceUpdateCanvases();
            }
        }

        private void SetInfoTitle(string infoTitleText)
        {
            if (infoPanelTitle != null) { infoPanelTitle.text = infoTitleText; }
        }

        private void DisplayInfoPanel(bool isDisplayed)
        {
            if (infoPanel != null) { infoPanel.SetActive(isDisplayed); }
        }

        private void DisplayBackgroundPanel(bool isDisplayed)
        {
            if (backgroundPanel != null) { backgroundPanel.SetActive(isDisplayed); }
        }

        private void UpdateInfoButtonText(string buttonText)
        {
            if (infoButton != null)
            {
                Text infoButtonTxt = infoButton.GetComponentInChildren<Text>();
                if (infoButtonTxt != null) { infoButtonTxt.text = buttonText; }
            }
        }

        #endregion
    }
}