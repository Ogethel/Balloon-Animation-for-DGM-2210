using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace LandscapeBuilder
{
    public class LandscapeBuilderGrassSelector : EditorWindow
    {
        // Variables
        private GUIStyle helpBoxRichText;
        private GUIStyle labelFieldRichText;
        private GUIStyle labelsmallFieldRichText;
        private GUIStyle buttonCompact;
        private GUIStyle alternativeTextureStyle;
        private string labelText = string.Empty;
        private float lineHeight = 150f;
        private bool allowRepaint = true;
        private LBGrassSetup lbGrassSetup;
        private Vector2 scrollPosition = Vector2.zero;
        private bool isLoading = false;
        private bool isStartedLoading = false;
        private string sourceFilter;
        private int sourceFilterIndex = 0;
        private int numberDisplayed = 0;
        private int numberInstalled = 0;

        // A reference to the LB editor window where the use clicked
        // on the button to bring up the list of grasses.
        private LandscapeBuilderWindow callingLBWindow;

        /// <summary>
        /// Used to pass in the script to the selector so that it
        /// can return the LBGrassConfig item selected.
        /// </summary>
        /// <param name="lbWindow"></param>
        public void Initialize(LandscapeBuilderWindow lbWindow, List<LBGrassConfig> lbGrassConfigList)
        {
            callingLBWindow = lbWindow;
            if (lbGrassSetup == null) { lbGrassSetup = new LBGrassSetup(); }

            if (lbGrassSetup != null)
            {
                if (lbGrassSetup.lbGrassConfigList == null) { lbGrassSetup.lbGrassConfigList = new List<LBGrassConfig>(lbGrassConfigList); }

                if (lbGrassSetup.lbGrassConfigList != null)
                {
                    isStartedLoading = true;
                    isLoading = true;

                    if (lbGrassSetup.lbGrassConfigList != null)
                    {
                        int grassConfigInt = 0;
                        int numGrassConfigs = lbGrassSetup.lbGrassConfigList.Count;

                        // Get a list of the Texture2D assets
                        // This could be quite memory hungry...
                        foreach (LBGrassConfig lbGrassConfig in lbGrassSetup.lbGrassConfigList)
                        {
                            EditorUtility.DisplayProgressBar("Loading grasses", "Please Wait", (grassConfigInt++) / numGrassConfigs);
                            lbGrassConfig.texture2D = (Texture2D)AssetDatabase.LoadAssetAtPath(lbGrassConfig.grassTexturePath, typeof(Texture2D));
                            lbGrassConfig.texture2DAlternative = (Texture2D)AssetDatabase.LoadAssetAtPath(lbGrassConfig.grassTextureAlternativePath, typeof(Texture2D));
                        }

                        // Sort the list by grass texture name
                        lbGrassSetup.lbGrassConfigList.Sort(delegate (LBGrassConfig grass1, LBGrassConfig grass2) { return grass1.grassTextureName.CompareTo(grass2.grassTextureName); });
                        lbGrassSetup.PopulateSourceList();
                        EditorUtility.ClearProgressBar();
                        isLoading = false;
                    }
                }
            }
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
            alternativeTextureStyle = new GUIStyle("Button");

            labelsmallFieldRichText = new GUIStyle("Label");
            labelsmallFieldRichText.richText = true;
            labelsmallFieldRichText.wordWrap = true;
            labelsmallFieldRichText.fontSize = 9;

            GUILayoutOption[] guiLayoutOptionsButton = { GUILayout.Width(150f), GUILayout.Height(150f) };
            GUILayoutOption[] guiLayoutOptionsAltTexture2D = { GUILayout.Width(150f), GUILayout.Height(150f) };
            #endregion

            if (lbGrassSetup.lbGrassConfigList != null && isStartedLoading && !isLoading)
            {
                if (lbGrassSetup.sourceList != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(new GUIContent("Filter", "Filter the list of grasses by Source"), labelFieldRichText, GUILayout.MaxWidth(40f));
                    sourceFilterIndex = EditorGUILayout.Popup(sourceFilterIndex, lbGrassSetup.sourceList.ToArray(), GUILayout.MaxWidth(260f));
                    labelText = "[" + numberDisplayed.ToString() + " of " + numberInstalled.ToString() + "]";
                    EditorGUILayout.LabelField(labelText, labelFieldRichText);
                    EditorGUILayout.EndHorizontal();
                }

                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                numberDisplayed = 0;
                numberInstalled = 0;
                for (int i = 0; i < lbGrassSetup.lbGrassConfigList.Count; i++)
                {
                    LBGrassConfig lbGrassConfig = lbGrassSetup.lbGrassConfigList[i];

                    if (lbGrassConfig != null)
                    {
                        // Skip any grasses that we don't have a texture in the project as it typically means the
                        // the asset is not in the project.
                        if (lbGrassConfig.texture2D == null) { continue; }

                        numberInstalled++;

                        // Only display grass configs that match the filter
                        if (sourceFilterIndex != 0) { if (lbGrassConfig.sourceName != lbGrassSetup.sourceList[sourceFilterIndex]) { continue; } }

                        // Display the grass config
                        GUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Height(lineHeight));
                        GUILayout.BeginHorizontal();
                        GUILayout.BeginVertical();
                        labelText = "<b>" + lbGrassConfig.grassTextureName.Substring(0, lbGrassConfig.grassTextureName.LastIndexOf('.')) + "</b>";
                        EditorGUILayout.LabelField(labelText, labelFieldRichText, GUILayout.MaxWidth(300f));
                        EditorGUILayout.LabelField(lbGrassConfig.sourceName, labelFieldRichText);
                        GUILayout.EndVertical();
                        EditorGUILayout.LabelField(new GUIContent(lbGrassConfig.texture2DAlternative), alternativeTextureStyle, guiLayoutOptionsAltTexture2D);
                        if (GUILayout.Button(new GUIContent(lbGrassConfig.texture2D), guiLayoutOptionsButton))
                        {
                            if (callingLBWindow != null)
                            {
                                callingLBWindow.isGrassConfigSelected = true;
                                // Use a clone of the lbGrassConfig so we don't hold any resources when window closes
                                callingLBWindow.lbGrassConfigSelected = new LBGrassConfig(lbGrassConfig);
                                this.Close();
                            }
                            else
                            {
                                // User may have closed the LB Editor so just exit out
                                this.Close();
                            }
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndVertical();
                        numberDisplayed++;
                    }
                }

                EditorGUILayout.EndScrollView();
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
        }
    }
}