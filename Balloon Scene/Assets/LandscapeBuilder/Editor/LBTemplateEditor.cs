// Landscape Builder. Copyright (c) 2016-2019 SCSM Pty Ltd. All rights reserved.
using UnityEngine;
using UnityEditor;
using System.Collections;

namespace LandscapeBuilder
{
    [CustomEditor(typeof(LBTemplate))]
    public class LBTemplateEditor : Editor
    {
        #region Custom Editor variables
        private LBTemplate lbTemplate;
        private string txtColourName = "Black";
        //private Color defaultTextColour = Color.black;
        private string labelText;
        private GUIStyle labelFieldRichText;
        #endregion

        #region SerializedProperty

        private SerializedProperty lbVersionProp;
        private SerializedProperty landscapeNameProp;
        private SerializedProperty lastUpdatedVersionProp;
        private SerializedProperty templateUnityVersionProp;
        private SerializedProperty sizeProp;
        private SerializedProperty startProp;
        private SerializedProperty heightmapResolutionProp;
        private SerializedProperty terrainWidthProp;
        private SerializedProperty terrainHeightProp;
        private SerializedProperty alphaMapResolutionProp;
        private SerializedProperty terrainMaterialTypeProp;

        private SerializedProperty isPopulateLandscapeProp;
        private SerializedProperty useLegacyNoiseOffsetProp;
        private SerializedProperty useGPUGrassProp;
        private SerializedProperty useGPUTexturingProp;
        private SerializedProperty useGPUTopographyProp;
        private SerializedProperty useGPUPathProp;
        private SerializedProperty debugModeProp;
        #endregion

        #region Initialise
        public void OnEnable()
        {
            //lbTemplate = (LBTemplate)target;
            if (EditorGUIUtility.isProSkin)
            {
                txtColourName = "White";
                //defaultTextColour = new Color(180f / 255f, 180f / 255f, 180f / 255f, 1f);
            }

            lbVersionProp = serializedObject.FindProperty("LBVersion");
            landscapeNameProp = serializedObject.FindProperty("landscapeName");
            lastUpdatedVersionProp = serializedObject.FindProperty("LastUpdatedVersion");
            templateUnityVersionProp = serializedObject.FindProperty("templateUnityVersion");
            sizeProp = serializedObject.FindProperty("size");
            startProp = serializedObject.FindProperty("start");
            heightmapResolutionProp = serializedObject.FindProperty("heightmapResolution");
            terrainWidthProp = serializedObject.FindProperty("terrainWidth");
            terrainHeightProp = serializedObject.FindProperty("terrainHeight");
            alphaMapResolutionProp = serializedObject.FindProperty("alphaMapResolution");
            terrainMaterialTypeProp = serializedObject.FindProperty("terrainMaterialType");

            isPopulateLandscapeProp = serializedObject.FindProperty("isPopulateLandscape");
            useLegacyNoiseOffsetProp = serializedObject.FindProperty("useLegacyNoiseOffset");

            useGPUTopographyProp = serializedObject.FindProperty("useGPUTopography");
            useGPUTexturingProp = serializedObject.FindProperty("useGPUTexturing");
            useGPUGrassProp = serializedObject.FindProperty("useGPUGrass");
            useGPUPathProp = serializedObject.FindProperty("useGPUPath");

            debugModeProp = serializedObject.FindProperty("debugMode");

            // Turn off debug mode by default
            debugModeProp.boolValue = false;
            serializedObject.ApplyModifiedProperties();

            lbTemplate = (LBTemplate)target;
        }
        #endregion

        #region OnInspectorGUI
        public override void OnInspectorGUI()
        {
            // If the base command is called, all public fields not hidden, will be displayed
            //base.OnInspectorGUI();

            EditorGUIUtility.labelWidth = 150f;

            if (labelFieldRichText == null)
            {
                labelFieldRichText = new GUIStyle("Label");
                labelFieldRichText.richText = true;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Read in all the properties
            serializedObject.Update();

            EditorGUILayout.BeginHorizontal();
            labelText = "<color=" + txtColourName + "><b>Landscape Name</b></color>";
            EditorGUILayout.LabelField(labelText, labelFieldRichText, GUILayout.Width(EditorGUIUtility.labelWidth));
            EditorGUILayout.PropertyField(landscapeNameProp, GUIContent.none);
            EditorGUILayout.EndHorizontal();

            // This is the installed version of LB when the template was created
            EditorGUILayout.BeginHorizontal();
            labelText = "<color=" + txtColourName + "><b>Landscape Version</b></color>";
            EditorGUILayout.LabelField(labelText, labelFieldRichText, GUILayout.Width(EditorGUIUtility.labelWidth));
            labelText = "<color=" + txtColourName + ">" + lastUpdatedVersionProp.stringValue + "</color>";
            EditorGUILayout.LabelField(labelText, labelFieldRichText);
            EditorGUILayout.EndHorizontal();

            // This is the installed version of LB when the template was created
            EditorGUILayout.BeginHorizontal();
            labelText = "<color=" + txtColourName + "><b>LB Version</b></color>";
            EditorGUILayout.LabelField(labelText, labelFieldRichText, GUILayout.Width(EditorGUIUtility.labelWidth));
            labelText = "<color=" + txtColourName + ">" + lbVersionProp.stringValue + "</color>";
            EditorGUILayout.LabelField(labelText, labelFieldRichText);
            EditorGUILayout.EndHorizontal();

            // This is the installed version of Unity when the template was created
            EditorGUILayout.BeginHorizontal();
            labelText = "<color=" + txtColourName + "><b>Unity Version</b></color>";
            EditorGUILayout.LabelField(labelText, labelFieldRichText, GUILayout.Width(EditorGUIUtility.labelWidth));
            labelText = "<color=" + txtColourName + ">" + templateUnityVersionProp.stringValue + "</color>";
            EditorGUILayout.LabelField(labelText, labelFieldRichText);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            labelText = "<color=" + txtColourName + "><b>Landscape Size</b></color>";
            EditorGUILayout.LabelField(labelText, labelFieldRichText, GUILayout.Width(EditorGUIUtility.labelWidth));
            labelText = "<color=" + txtColourName + ">" + sizeProp.vector2Value + "</color>";
            EditorGUILayout.LabelField(labelText, labelFieldRichText);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            labelText = "<color=" + txtColourName + "><b>Start Position</b></color>";
            EditorGUILayout.LabelField(labelText, labelFieldRichText, GUILayout.Width(EditorGUIUtility.labelWidth));
            labelText = "<color=" + txtColourName + ">X: " + startProp.vector3Value.x + " Y: " + startProp.vector3Value.y + " Z: " + startProp.vector3Value.z + "</color>";
            EditorGUILayout.LabelField(labelText, labelFieldRichText);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            labelText = "<color=" + txtColourName + "><b>Heightmap Resolution</b></color>";
            EditorGUILayout.LabelField(labelText, labelFieldRichText, GUILayout.Width(EditorGUIUtility.labelWidth));
            labelText = "<color=" + txtColourName + ">" + heightmapResolutionProp.intValue + "</color>";
            EditorGUILayout.LabelField(labelText, labelFieldRichText);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            labelText = "<color=" + txtColourName + "><b>Terrain Width</b></color>";
            EditorGUILayout.LabelField(labelText, labelFieldRichText, GUILayout.Width(EditorGUIUtility.labelWidth));
            labelText = "<color=" + txtColourName + ">" + terrainWidthProp.floatValue + "</color>";
            EditorGUILayout.LabelField(labelText, labelFieldRichText);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            labelText = "<color=" + txtColourName + "><b>Terrain Height</b></color>";
            EditorGUILayout.LabelField(labelText, labelFieldRichText, GUILayout.Width(EditorGUIUtility.labelWidth));
            labelText = "<color=" + txtColourName + ">" + terrainHeightProp.floatValue + "</color>";
            EditorGUILayout.LabelField(labelText, labelFieldRichText);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            labelText = "<color=" + txtColourName + "><b>Alphamap Resolution</b></color>";
            EditorGUILayout.LabelField(labelText, labelFieldRichText, GUILayout.Width(EditorGUIUtility.labelWidth));
            labelText = "<color=" + txtColourName + ">" + alphaMapResolutionProp.intValue + "</color>";
            EditorGUILayout.LabelField(labelText, labelFieldRichText);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            labelText = "<color=" + txtColourName + "><b>Terrain Material Type</b></color>";
            EditorGUILayout.LabelField(labelText, labelFieldRichText, GUILayout.Width(EditorGUIUtility.labelWidth));
            EditorGUILayout.PropertyField(terrainMaterialTypeProp, GUIContent.none);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            labelText = "<color=" + txtColourName + "><b>Legacy Noise Offset</b></color>";
            EditorGUILayout.LabelField(labelText, labelFieldRichText, GUILayout.Width(EditorGUIUtility.labelWidth));
            labelText = "<color=" + txtColourName + ">" + useLegacyNoiseOffsetProp.boolValue + "</color>";
            EditorGUILayout.LabelField(labelText, labelFieldRichText);
            EditorGUILayout.EndHorizontal();

            labelText = "<color=" + txtColourName + "><b>GPU Acceleration</b></color>";
            EditorGUILayout.LabelField(labelText, labelFieldRichText, GUILayout.Width(EditorGUIUtility.labelWidth));

            EditorGUILayout.BeginHorizontal();
            labelText = "<color=" + txtColourName + "><b>  Topography</b></color>";
            EditorGUILayout.LabelField(labelText, labelFieldRichText, GUILayout.Width(EditorGUIUtility.labelWidth));
            labelText = "<color=" + txtColourName + ">" + useGPUTopographyProp.boolValue + "</color>";
            EditorGUILayout.LabelField(labelText, labelFieldRichText);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            labelText = "<color=" + txtColourName + "><b>  Texturing</b></color>";
            EditorGUILayout.LabelField(labelText, labelFieldRichText, GUILayout.Width(EditorGUIUtility.labelWidth));
            labelText = "<color=" + txtColourName + ">" + useGPUTexturingProp.boolValue + "</color>";
            EditorGUILayout.LabelField(labelText, labelFieldRichText);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            labelText = "<color=" + txtColourName + "><b>  Grass</b></color>";
            EditorGUILayout.LabelField(labelText, labelFieldRichText, GUILayout.Width(EditorGUIUtility.labelWidth));
            labelText = "<color=" + txtColourName + ">" + useGPUGrassProp.boolValue + "</color>";
            EditorGUILayout.LabelField(labelText, labelFieldRichText);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            labelText = "<color=" + txtColourName + "><b>  Path</b></color>";
            EditorGUILayout.LabelField(labelText, labelFieldRichText, GUILayout.Width(EditorGUIUtility.labelWidth));
            labelText = "<color=" + txtColourName + ">" + useGPUPathProp.boolValue + "</color>";
            EditorGUILayout.LabelField(labelText, labelFieldRichText);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            labelText = "<color=" + txtColourName + "><b>Populate Landscape</b></color>";
            EditorGUILayout.LabelField(labelText, labelFieldRichText, GUILayout.Width(EditorGUIUtility.labelWidth));
            EditorGUILayout.PropertyField(isPopulateLandscapeProp, new GUIContent(""), GUILayout.Width(30));
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Create New unpopulated Landscape from Template"))
            {
                if (lbTemplate == null) { Debug.LogWarning("LBTemplateEditor - create landscape - template not defined. Please Report"); }
                else if (string.IsNullOrEmpty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name))
                {
                    EditorUtility.DisplayDialog("Generate Landscape", "Please save the scene before adding a landscape.", "Got it!");
                }
                else
                {
                    LBLandscape lbLandscape = lbTemplate.CreateLandscapeFromTemplate(lbTemplate.landscapeName);
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                    if (lbLandscape != null)
                    {
                        lbTemplate.ApplyTemplateToLandscape(lbLandscape, false, true, null, true);
                        if (isPopulateLandscapeProp.boolValue)
                        {
                            lbLandscape.ApplyTopography(false, true);
                            lbLandscape.ApplyTextures(false, true);
                            lbLandscape.ApplyTrees(false, true);
                            lbLandscape.ApplyGrass(false, true);
                            lbLandscape.ApplyGroups(false, true);
                            lbLandscape.ApplyMeshes(false, true);
                        }

                        if (lbTemplate.isLBLightingIncluded)
                        {
                            LBLighting lbLighting = GameObject.FindObjectOfType<LBLighting>();
                            if (lbLighting == null)
                            {
                                // If LBLighting isn't already in the scene, add it.
                                lbLighting = LBLighting.AddLightingToScene(true);
                            }

                            if (lbLighting != null)
                            {
                                // Restore the lighting settings from the template
                                lbTemplate.ApplyLBLightingSettings(lbLandscape, ref lbLighting, Camera.main, true);
                            }
                        }

                        LandscapeBuilderWindow.SetLandscape(lbLandscape, true);
                        // Prevent errors in editor for U2017
                        //EditorGUILayout.EndVertical();
                        //return;
                    }
                }
            }

            EditorGUILayout.BeginHorizontal();
            labelText = "<color=" + txtColourName + "><b>Debug Mode</b></color>";
            EditorGUILayout.LabelField(labelText, labelFieldRichText, GUILayout.Width(EditorGUIUtility.labelWidth));
            EditorGUILayout.PropertyField(debugModeProp, new GUIContent(""), GUILayout.Width(30));
            labelText = "<color=" + txtColourName + "><b>INTERNAL USE ONLY</b></color>";
            EditorGUILayout.LabelField(labelText, labelFieldRichText);
            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();

            if (debugModeProp.boolValue)
            {
                // Display unhidden public variables in LBTemplate.cs
                DrawDefaultInspector();
            }

            EditorGUILayout.EndVertical();

        }
        #endregion

    }
}