#define _LBGrassEditorAdmin
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace LandscapeBuilder
{
    public class LandscapeBuilderGrassEditor : EditorWindow
    {

        #region Variables
        public bool isRemoteLoading = false;
        public LBTerrainGrass lbTerrainGrassSelected;
        public LandscapeBuilderWindow lbWindow;

        private GUIStyle helpBoxRichText;
        private GUIStyle labelFieldRichText;
        private GUIStyle labelsmallFieldRichText;
        private GUIStyle buttonCompact;
        private string labelText = string.Empty;
        private bool allowRepaint = true;
        private Vector2 scrollPosition = Vector2.zero;
        private LBGrassSetup lbGrassSetup;
        private bool isSaveRequired = false;
        private bool isLoading = true;
        private int numberInstalled = 0;
        private int numberDisplayed = 0;
        private bool isHQPhotoPackVol1Installed;
        private bool isHQPhotoPackVol2Installed;
        private bool isRusticGrassInstalled;
        private string sourceNameHQPhotoPackVol1;
        private string sourceNameHQPhotoPackVol2;
        private string sourceNameRusticGrass;
        private string pathHQPhotoPackVol1;
        private string pathHQPhotoPackVol2;
        private string pathRusticGrass;
        private string sourceFilter;
        private int sourceFilterIndex = 0;
        private LBGrassConfig lbGrassConfigToRemove;
        #endregion

        // Add a menu item so that the editor window can be opened via the window menu tab
        [MenuItem("Window/Landscape Builder/Landscape Builder Grass Editor")]
        public static void ShowWindow()
        {
            // Show existing window instance. If one doesn't exist, make one.
            EditorWindow.GetWindow(typeof(LandscapeBuilderGrassEditor), false, "Grass Editor");
        }

        private void OnEnable()
        {
            if (lbGrassSetup == null) { lbGrassSetup = new LBGrassSetup(); }

            pathHQPhotoPackVol1 = LBLandscape.GetPath(LBSavedData.PathType.HQPhotographicTexturesVol1);
            pathHQPhotoPackVol2 = LBLandscape.GetPath(LBSavedData.PathType.HQPhotographicTexturesVol2);
            pathRusticGrass = LBLandscape.GetPath(LBSavedData.PathType.RusticGrass);

            // If the paths aren't set, set them to the default. Then check if they exist in the current project
            if (string.IsNullOrEmpty(pathHQPhotoPackVol1)) { pathHQPhotoPackVol1 = LBSavedData.GetHQPhotographicTexturesVol1DefaultPath; }
            if (string.IsNullOrEmpty(pathHQPhotoPackVol1)) { isHQPhotoPackVol1Installed = false; }
            else { isHQPhotoPackVol1Installed = AssetDatabase.IsValidFolder("Assets/" + pathHQPhotoPackVol1); }

            if (string.IsNullOrEmpty(pathHQPhotoPackVol2)) { pathHQPhotoPackVol2 = LBSavedData.GetHQPhotographicTexturesVol2DefaultPath; }
            if (string.IsNullOrEmpty(pathHQPhotoPackVol2)) { isHQPhotoPackVol2Installed = false; }
            else { isHQPhotoPackVol2Installed = AssetDatabase.IsValidFolder("Assets/" + pathHQPhotoPackVol2); }

            if (string.IsNullOrEmpty(pathRusticGrass)) { pathRusticGrass = LBSavedData.GetRusticGrassDefaultPath; }
            if (string.IsNullOrEmpty(pathRusticGrass)) { isRusticGrassInstalled = false; }
            else { isRusticGrassInstalled = AssetDatabase.IsValidFolder("Assets/" + pathRusticGrass); }

            sourceNameHQPhotoPackVol1 = LBSavedData.GetHQPhotographicTexturesVol1SourceName;
            sourceNameHQPhotoPackVol2 = LBSavedData.GetHQPhotographicTexturesVol2SourceName;
            sourceNameRusticGrass = LBSavedData.GetRusticGrassSourceName;
        }

        private void OnGUI()
        {
            #region Initialisation

            // Set repaint to false at the start of every OnGUI call
            allowRepaint = false;

            // Set up rich text GUIStyles
            helpBoxRichText = new GUIStyle("HelpBox");
            helpBoxRichText.richText = true;
            labelFieldRichText = new GUIStyle("Label");
            labelFieldRichText.richText = true;
            buttonCompact = new GUIStyle("Button");
            buttonCompact.fontSize = 10;

            labelsmallFieldRichText = new GUIStyle("Label");
            labelsmallFieldRichText.richText = true;
            labelsmallFieldRichText.wordWrap = true;
            labelsmallFieldRichText.fontSize = 9;

            GUILayoutOption[] guiLayoutOptionsTexture2D = { GUILayout.Width(100f), GUILayout.Height(100f) };

            lbGrassConfigToRemove = null;

            #endregion

            #region GrassList

            EditorGUILayout.LabelField("<b>Grass Editor</b>\n\nThe grass editor allows you to edit the default properties of grasses in your project." +
                                       " These are then made available in the Landscape Builder Window Grass Tab.", helpBoxRichText);

            if (lbGrassSetup.lbGrassConfigList != null && !isLoading)
            {
                if (lbGrassSetup.sourceList != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(new GUIContent("Filter", "Filter the list of grasses by Source"), labelFieldRichText, GUILayout.MaxWidth(40f));
                    sourceFilterIndex = EditorGUILayout.Popup(sourceFilterIndex, lbGrassSetup.sourceList.ToArray(), GUILayout.MaxWidth(260f));
                    EditorGUILayout.EndHorizontal();
                }
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Get Grass Settings", buttonCompact, GUILayout.MaxWidth(135f)))
            {
                GetGrassSettings();
                isLoading = true;
            }

            if (lbGrassSetup.lbGrassConfigList != null && !isLoading)
            {
                if (GUILayout.Button("Upgrade Grass Settings", buttonCompact, GUILayout.MaxWidth(165f)))
                {
                    string msg = "WARNING: This will overwrite any custom settings you have made to the grass config settings. " +
                                    "It will apply new values which came with the current version of Landscape Builder " +
                                    "installed in this project.\n\n" +
                                    "NOTE: It will not modify current grass settings in your landscapes.";
                    if (EditorUtility.DisplayDialog("Upgrade Grass Settings", msg, "Overwrite", "Cancel"))
                    {
                        UpgradeGrassSettings();
                        isLoading = true;
                    }
                }
            }
            GUILayout.EndHorizontal();

            if (lbGrassSetup.lbGrassConfigList != null && lbGrassSetup.sourceList != null && !isLoading)
            {
                labelText = "Grass configurations: " + numberDisplayed.ToString() + " [" + numberInstalled.ToString() + " installed of " + lbGrassSetup.lbGrassConfigList.Count.ToString() + "]";
                EditorGUILayout.LabelField(labelText, labelFieldRichText);

                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

                numberInstalled = 0;
                numberDisplayed = 0;
                for (int i = 0; i < lbGrassSetup.lbGrassConfigList.Count; i++)
                {
                    LBGrassConfig lbGrassConfig = lbGrassSetup.lbGrassConfigList[i];

                    if (lbGrassConfig != null)
                    {
                        // Skip any grasses that we don't have a texture in the project as it typically means the
                        // the asset is not in the project.
                        if (lbGrassConfig.texture2D == null) { continue; }
                        // Filter out grasses not installed in this project.
                        if (!isHQPhotoPackVol1Installed && lbGrassConfig.sourceName == sourceNameHQPhotoPackVol1) { continue; }
                        if (!isHQPhotoPackVol2Installed && lbGrassConfig.sourceName == sourceNameHQPhotoPackVol2) { continue; }
                        if (!isRusticGrassInstalled && lbGrassConfig.sourceName == sourceNameRusticGrass) { continue; }
                        numberInstalled++;

                        // Only display grass configs that match the filter
                        if (sourceFilterIndex != 0) { if (lbGrassConfig.sourceName != lbGrassSetup.sourceList[sourceFilterIndex]) { continue; } }

                        // Display the grass config
                        numberDisplayed++;
                        GUILayout.BeginVertical(EditorStyles.helpBox);
                        GUILayout.BeginHorizontal();
                        labelText = "<b>" + lbGrassConfig.grassTextureName.Substring(0, lbGrassConfig.grassTextureName.LastIndexOf('.')) + "</b>";
                        // The left label will force any buttons in the Horizontal layout to be right justified.
                        EditorGUILayout.LabelField(labelText, labelFieldRichText);
                        if (lbGrassConfig.sourceName == LBGrassSetup.UserDefinedSourceFilter)
                        {
                            if (GUILayout.Button(new GUIContent("X", "Delete the current grass configuration"), buttonCompact, GUILayout.Width(20f)))
                            {
                                lbGrassConfigToRemove = lbGrassConfig;
                            }
                        }
#if LBGrassEditorAdmin
                    else
                    {
                        if (GUILayout.Button(new GUIContent("X", "Delete the VENDOR current grass configuration"), buttonCompact, GUILayout.Width(20f)))
                        {
                            lbGrassConfigToRemove = lbGrassConfig;
                        }
                    }
#endif
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        GUILayout.BeginVertical();
                        EditorGUILayout.LabelField(lbGrassConfig.sourceName, labelFieldRichText);
                        GUILayout.EndVertical();
                        EditorGUILayout.LabelField(new GUIContent(lbGrassConfig.texture2D), labelFieldRichText, guiLayoutOptionsTexture2D);
                        GUILayout.EndHorizontal();
                        EditorGUI.BeginChangeCheck();
                        lbGrassConfig.detailRenderMode = (DetailRenderMode)EditorGUILayout.EnumPopup("Detail Rendering Mode", lbGrassConfig.detailRenderMode);
                        lbGrassConfig.grassPatchFadingMode = (LBGrassConfig.GrassPatchFadingMode)EditorGUILayout.EnumPopup("Grass Patch Fading Mode", lbGrassConfig.grassPatchFadingMode);
                        lbGrassConfig.healthyColour = EditorGUILayout.ColorField(new GUIContent("Healthy Colour", "The tint colour of the grass when it is 'healthy'"), lbGrassConfig.healthyColour);
                        lbGrassConfig.dryColour = EditorGUILayout.ColorField(new GUIContent("Dry Colour", "The tint colour of the grass when it is 'dry'"), lbGrassConfig.dryColour);

                        lbGrassConfig.minHeight = EditorGUILayout.Slider(new GUIContent("Min Grass Height", "The minimum height of the grass in metres"), lbGrassConfig.minHeight, 0.1f, 8f);
                        lbGrassConfig.maxHeight = EditorGUILayout.Slider(new GUIContent("Max Grass Height", "The maximum height of the grass in metres"), lbGrassConfig.maxHeight, 0.1f, 8f);
                        if (lbGrassConfig.maxHeight < lbGrassConfig.minHeight)
                        {
                            lbGrassConfig.maxHeight = lbGrassConfig.minHeight;
                        }

                        lbGrassConfig.minWidth = EditorGUILayout.Slider(new GUIContent("Min Grass Width", "The minimum width of the grass in metres"), lbGrassConfig.minWidth, 0.1f, 8f);
                        lbGrassConfig.maxWidth = EditorGUILayout.Slider(new GUIContent("Max Grass Width", "The maximum width of the grass in metres"), lbGrassConfig.maxWidth, 0.1f, 8f);
                        if (lbGrassConfig.maxWidth < lbGrassConfig.minWidth)
                        {
                            lbGrassConfig.maxWidth = lbGrassConfig.minWidth;
                        }
                        if (EditorGUI.EndChangeCheck())
                        {
                            isSaveRequired = true;
                        }

                        GUILayout.EndVertical();
                    }
                }

                EditorGUILayout.EndScrollView();
            }

            #endregion

            // If user pressed "X" delete key on one, remove it.
            if (lbGrassConfigToRemove != null) { lbGrassSetup.lbGrassConfigList.Remove(lbGrassConfigToRemove); isSaveRequired = true; lbGrassConfigToRemove = null; }

            isLoading = false;

            if (isSaveRequired)
            {
                isSaveRequired = false;
                if (lbGrassSetup != null)
                {
                    lbGrassSetup.Save(false);
                    if (lbWindow != null) { lbWindow.isGrassConfigListDirty = true; }
                }
            }

            // Set repaint to true at the end of every OnGUI call
            allowRepaint = true;
        }

        /// <summary>
        /// Gets called automatically 10 times per second
        /// </summary>
        private void OnInspectorUpdate()
        {
            // OnGUI () only registers events when the mouse is positioned over the custom editor window
            // This code forces OnGUI () to run every frame, so it registers events even when the mouse
            // is positioned over the scene view

            if (allowRepaint) { Repaint(); }

            if (isRemoteLoading)
            {
                isRemoteLoading = false;
                GetGrassSettings();
                sourceFilterIndex = 1;
                UserDefinedLookup(lbTerrainGrassSelected, LBGrassSetup.UserDefinedSourceFilter);
            }
        }

        /// <summary>
        /// Add a new LBGrassConfig using an existing LBTerrainGrass instance
        /// </summary>
        /// <param name="lbTerrainGrass"></param>
        private void AddGrassConfig(LBTerrainGrass lbTerrainGrass, string textureName)
        {
            if (lbGrassSetup == null)
            {
                Debug.LogWarning("LandscapeBuilderGrassEditor.AddGrassConfig - Grass Setup is not defined");
            }
            else if (lbTerrainGrass == null)
            {
                Debug.LogWarning("LandscapeBuilderGrassEditor.AddGrassConfig - Terrain Grass cannot be null");
            }
            else if (lbGrassSetup.lbGrassConfigList == null)
            {
                Debug.LogWarning("LandscapeBuilderGrassEditor.AddGrassConfig - GrassConfigList is not defined");
            }
            else
            {
                LBGrassConfig lbGrassConfigNew = new LBGrassConfig();
                if (lbGrassConfigNew != null)
                {
                    lbGrassConfigNew.sourceName = LBGrassSetup.UserDefinedSourceFilter;
                    lbGrassConfigNew.texture2D = lbTerrainGrass.texture;
                    lbGrassConfigNew.grassTexturePath = AssetDatabase.GetAssetPath(lbTerrainGrass.texture);
                    lbGrassConfigNew.grassTextureName = LBTextureOperations.GetTextureFileNameFromPath(lbGrassConfigNew.grassTexturePath);
                    lbGrassConfigNew.detailRenderMode = lbTerrainGrass.detailRenderMode;
                    lbGrassConfigNew.dryColour = lbTerrainGrass.dryColour;
                    lbGrassConfigNew.grassPatchFadingMode = (LBGrassConfig.GrassPatchFadingMode)lbTerrainGrass.grassPatchFadingMode;
                    lbGrassConfigNew.healthyColour = lbTerrainGrass.healthyColour;
                    lbGrassConfigNew.minWidth = lbTerrainGrass.minWidth;
                    lbGrassConfigNew.maxWidth = lbTerrainGrass.maxWidth;
                    lbGrassConfigNew.minHeight = lbTerrainGrass.minHeight;
                    lbGrassConfigNew.maxHeight = lbTerrainGrass.maxHeight;
                    lbGrassSetup.lbGrassConfigList.Add(lbGrassConfigNew);
                    lbGrassSetup.Save(false);
                    if (lbWindow != null) { lbWindow.isGrassConfigListDirty = true; }
                }
            }
        }

        private void UserDefinedLookup(LBTerrainGrass lbTerrainGrass, string sourceName)
        {
            if (lbGrassSetup == null)
            {
                Debug.LogWarning("LandscapeBuilderGrassEditor.UserDefinedLookup - Grass Setup is not defined");
            }
            else if (lbTerrainGrass == null)
            {
                Debug.LogWarning("LandscapeBuilderGrassEditor.UserDefinedLookup - Terrain Grass cannot be null");
            }
            else
            {
                string filePath = LBTextureOperations.GetTextureFilePath(lbTerrainGrass.texture);
                string textureNameWithExtension = LBTextureOperations.GetTextureFileNameFromPath(filePath);

                // Find a match in the current config list
                LBGrassConfig lbGrassConfig = lbGrassSetup.lbGrassConfigList.Find(g => g.grassTextureName == textureNameWithExtension && g.sourceName == sourceName);

                // If we didn't find a match, create a new one
                if (lbGrassConfig == null)
                {
                    AddGrassConfig(lbTerrainGrass, textureNameWithExtension);
                }
            }
        }

        /// <summary>
        /// Get the texture name from the texture.
        /// Return an empty string if texture is null
        /// </summary>
        /// <param name="texture"></param>
        /// <returns></returns>
        private string GetTextureName(Texture2D texture)
        {
            if (texture != null) { return texture.name; }
            else { return string.Empty; }
        }


        /// <summary>
        /// Fetch the grass set from the project-based file
        /// Populate the list of grass configurations
        /// Sort the grass config list
        /// Populate the source list
        /// </summary>
        private void GetGrassSettings()
        {
            if (lbGrassSetup == null) { lbGrassSetup = new LBGrassSetup(); }

            if (lbGrassSetup != null)
            {
                EditorUtility.DisplayProgressBar("Loading grass configurations", "Please Wait", 0f);

                lbGrassSetup.Retrieve();

                if (lbGrassSetup.lbGrassConfigList != null)
                {
                    int numGrassConfigs = lbGrassSetup.lbGrassConfigList.Count;

                    EditorUtility.ClearProgressBar();
                    // Get a list of the Texture2D assets
                    for (int i = 0; i < numGrassConfigs; i++)
                    {
                        LBGrassConfig lbGrassConfig = lbGrassSetup.lbGrassConfigList[i];
                        EditorUtility.DisplayProgressBar("Loading grass settings", "Please Wait", i + 1 / numGrassConfigs);
                        if (lbGrassConfig != null)
                        {
                            lbGrassConfig.texture2D = (Texture2D)AssetDatabase.LoadAssetAtPath(lbGrassConfig.grassTexturePath, typeof(Texture2D));

#if LBGrassEditorAdmin
                        if (lbGrassConfig.texture2D == null)
                        {
                            Debug.Log("LBGrassEditor.GetGrassSettings " + lbGrassConfig.grassTexturePath + " not installed");
                        }
#endif
                            //Debug.Log("GetGrassSettings: " + lbGrassConfig.grassTexturePath);
                        }
                    }

                    // Sort the list by grass texture name
                    lbGrassSetup.lbGrassConfigList.Sort(delegate (LBGrassConfig grass1, LBGrassConfig grass2) { return grass1.grassTextureName.CompareTo(grass2.grassTextureName); });
                }

                lbGrassSetup.PopulateSourceList();
                EditorUtility.ClearProgressBar();
            }
        }

        /// <summary>
        /// This will import grass settings from the latest LB version file
        /// which is located in Assets/LandscapeBuilder/Setup folder and gets
        /// updated with newer versions of LB.
        /// WARNING: It will overwrite any user settings made in this Editor
        /// </summary>
        private void UpgradeGrassSettings()
        {
            // Need to load the textures first.
            GetGrassSettings();

            // Import the file from the Assets folder which was downloaded with the latest package of LB
            LBGrassSetup lbGrassSetupUpgrade = new LBGrassSetup();
            if (lbGrassSetupUpgrade != null)
            {
                lbGrassSetupUpgrade.Retrieve(true);
                if (lbGrassSetupUpgrade.lbGrassConfigList != null)
                {
                    int numConfigsToUpgrade = lbGrassSetupUpgrade.lbGrassConfigList.Count;
                    int upgradingCount = 0;
                    int numAdded = 0;
                    int numUpgraded = 0;
                    int numSkipped = 0;
                    float upgradeProgress = 0f;
                    string progressMsg = string.Empty;
                    string textureFolder = string.Empty;

                    // Loop throught the new grass configs
                    foreach (LBGrassConfig lbGrassConfigUpgrade in lbGrassSetupUpgrade.lbGrassConfigList)
                    {
                        if (lbGrassConfigUpgrade != null)
                        {
                            allowRepaint = true;
                            progressMsg = "Upgrading " + (upgradingCount++).ToString() + " of " + numConfigsToUpgrade.ToString() + " ... Please wait";
                            upgradeProgress = upgradingCount / numConfigsToUpgrade;
                            if (EditorUtility.DisplayCancelableProgressBar("Upgrading Grass Configurations", progressMsg, upgradeProgress)) { break; }

                            // Find a match in the current config list (used in the LandscapeBuilderGrassSelector)
                            LBGrassConfig lbGrassconfigCurrent = lbGrassSetup.lbGrassConfigList.Find(g => g.grassTextureName == lbGrassConfigUpgrade.grassTextureName &&
                                                                                                          g.sourceName == lbGrassConfigUpgrade.sourceName);

                            // Is this a new configuration to be added?
                            if (lbGrassconfigCurrent == null)
                            {
                                LBGrassConfig newGrassConfig = new LBGrassConfig();
                                if (newGrassConfig != null)
                                {
                                    // default configuration
                                    if (!string.IsNullOrEmpty(lbGrassConfigUpgrade.grassTextureName))
                                    {
                                        newGrassConfig = new LBGrassConfig(lbGrassConfigUpgrade);

                                        // Select the correct folder based on the source of the LBGrassConfig (i.e. which Unity package is it from)
                                        if (lbGrassConfigUpgrade.sourceName == sourceNameHQPhotoPackVol1) { textureFolder = "Assets/" + pathHQPhotoPackVol1; }
                                        else if (lbGrassConfigUpgrade.sourceName == sourceNameHQPhotoPackVol2) { textureFolder = "Assets/" + pathHQPhotoPackVol2; }
                                        else if (lbGrassConfigUpgrade.sourceName == sourceNameRusticGrass) { textureFolder = "Assets/" + pathRusticGrass; }
                                        else { textureFolder = "unknown"; }

                                        // The paths may not match the current project
                                        //string[] lookFor = new string[] { textureFolder };

                                        if (textureFolder == "unknown") { numSkipped++; }
                                        else
                                        {
                                            //string shortName = lbGrassConfigUpgrade.grassTextureName.Substring(0, lbGrassConfigUpgrade.grassTextureName.LastIndexOf('.'));

                                            // If the folder doesn't exist this typically means it is not installed in the project, so use the default path.
                                            if (AssetDatabase.IsValidFolder(textureFolder))
                                            {
                                                //Debug.Log("valid: " + textureFolder);
                                                // TODO - LB Grass Editor - get the correct project folder path when upgrading rather than using the default.
                                                // Currently this can return multiple textures for names that have a space in them. For now
                                                // we're going to use the default path...

                                                //string[] textureGUIDArray = AssetDatabase.FindAssets(shortName + " t:texture2D", lookFor);

                                                //if (textureGUIDArray != null)
                                                //{
                                                //    foreach (string guidstr in textureGUIDArray)
                                                //    {
                                                //        string pathToTexture2D = AssetDatabase.GUIDToAssetPath(guidstr);
                                                //        //Debug.Log(" tx: " + pathToTexture2D);
                                                //    }
                                                //}
                                            }

                                            //Debug.Log("upgrade path: " + lbGrassConfigUpgrade.grassTexturePath);
                                        }

                                        lbGrassSetup.lbGrassConfigList.Add(newGrassConfig);
                                        numAdded++;
                                    }
                                }
                            }
                            else
                            {
                                // Existing configuration - so update settings
                                lbGrassconfigCurrent.grassPatchFadingMode = lbGrassConfigUpgrade.grassPatchFadingMode;
                                lbGrassconfigCurrent.healthyColour = lbGrassConfigUpgrade.healthyColour;
                                lbGrassconfigCurrent.dryColour = lbGrassConfigUpgrade.dryColour;
                                lbGrassconfigCurrent.detailRenderMode = lbGrassConfigUpgrade.detailRenderMode;
                                lbGrassconfigCurrent.minWidth = lbGrassConfigUpgrade.minWidth;
                                lbGrassconfigCurrent.maxWidth = lbGrassConfigUpgrade.maxWidth;
                                lbGrassconfigCurrent.minHeight = lbGrassConfigUpgrade.minHeight;
                                lbGrassconfigCurrent.maxHeight = lbGrassConfigUpgrade.maxHeight;

                                // Update the texture settings - only update installed textures
                                if (lbGrassconfigCurrent.texture2D != null)
                                {
#if UNITY_5_5_OR_NEWER
                                    LBTextureOperations.SetTextureAttributes(lbGrassconfigCurrent.texture2D, TextureImporterCompression.CompressedHQ, FilterMode.Bilinear, false, 0, true);
#else
                                LBTextureOperations.SetTextureAttributes(lbGrassconfigCurrent.texture2D, TextureImporterFormat.AutomaticCompressed, FilterMode.Bilinear, false, 0, true);
#endif
                                }
                                numUpgraded++;
                            }
                        }
                    }

                    // Save the updates
                    if (lbGrassSetup != null) { lbGrassSetup.Save(); }
                    Debug.Log("Landscape Builder Grass Editor: Added " + numAdded.ToString() + " new grass configurations added. " + numUpgraded.ToString() + " updated. " + numSkipped.ToString() + " skipped.");
                    allowRepaint = false;
                    // Refresh the on-screen list
                    GetGrassSettings();
                    EditorUtility.ClearProgressBar();
                }
            }
        }
    }
}