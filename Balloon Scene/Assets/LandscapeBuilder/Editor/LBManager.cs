// Landscape Builder. Copyright (c) 2016-2019 SCSM Pty Ltd. All rights reserved.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace LandscapeBuilder
{
    public class LBManager : EditorWindow
    {
        #region Public variables

        #endregion

        #region Private variables
        private LBLandscape landscape = null;

        // Phase 1
        private string landscapeTemplateName = "Landscape Template";
        private bool createTemplatePackage = true;
        private bool addMapTexturesToTemplatePackage = false;
        private bool addLayerHeightmapTexturesToTemplatePackage = false;
        private bool addLBLightingToTemplate = true;
        private bool addPathsToTemplate = true;
        private bool addStencilsToTemplate = true;
        private bool addPathMeshMaterialsToTemplate = false;

        // Phase 2
        private bool isRemovePrefabItems = false;  // Off by default because cannot restore from a Template
        private bool isRemoveMapPaths = true;
        private bool isRemoveStencils = true;
        private bool isRemoveLBLandscape = false;

        // Phase 3
        private bool isRemoveEditorScripts = true;
        private bool isRemoveDemoScene = true;
        private bool isRemoveDemoAssets = false;
        private bool isRemoveUnityWater = false;
        private bool isRemoveRuntimeSamples = false;
        private bool isRemoveSRP = true;

        // General
        private List<Component> tempComponentList = null;

        private Vector2 scrollPosition = Vector2.zero;
        private Color currentBgndColor = Color.white;
        private string txtColourName = "black";
        private GUIStyle labelFieldRichText = null;
        private static readonly float labelWidthOffset = 25f;

        #endregion

        // TODO
        // 1. Remove mesh controller from parent GO
        // 2. Remove LBLandscape script
        // 3. Warn about standard assets in LB from being deleted/moved before deleting LB folder
        // 4. Search for other LB scripts being used.

        // Things that may need to be retained
        // Terrain shaders
        // LBLighting
        // LB Screen Shot

        #region GUIContent
        private static readonly GUIContent landscapeGUIContent = new GUIContent(" Landscape", "Drag in the Landscape parent GameObject from the scene Hierarchy.");
        private static readonly GUIContent saveTemplateNameContent = new GUIContent("Template Name", "The name of the saved Landscape Template. IMPORTANT: Make sure these are unique in your project to avoid some being overwritten.");
        #endregion

        #region GUIContent - Phase 2
        private static readonly GUIContent removePrefabItemContent = new GUIContent("Remove Group IDs", "Remove all references to the Groups from the gameobjects in the landscape");
        private static readonly GUIContent removeMapPathContent = new GUIContent("Remove Map Paths", "Remove all LBMapPath scripts from the gameobjects in the landscape along with the path points. Path meshes will be retained in the scene.");
        private static readonly GUIContent removeStencilContent = new GUIContent("Remove Stencils", "Remove all LBStencil scripts and stencil data from the gameobjects in the landscape");
        private static readonly GUIContent removeLBLandscapeContent = new GUIContent("Remove Meta-data", "Remove landscape meta-data. This is where the majority of data in the LB Editor comes from. You will need a valid Template for each Landscape to restore this data.");
        private static readonly GUIContent finaliseBtnContent = new GUIContent("Finalise Landscape", "WARNING: This will remove references to Landscape Builder from the current landscape. There is currently no UNDO button. Use with caution.");
        #endregion

        #region GUIContent - Phase 3
        private static readonly GUIContent removeEditorScriptsContent = new GUIContent("Remove Editor Scripts", "Remove all LB Editor scripts from the project");
        private static readonly GUIContent removeDemoSceneContent = new GUIContent("Remove Demo Scene", "Remove LB Demo Scenes from the project");
        private static readonly GUIContent removeDemoAssetsContent = new GUIContent("Remove Demo Assets", "Remove all LB Demo assets from the project");
        private static readonly GUIContent removeUnityWaterContent = new GUIContent("Remove Legacy Unity Water", "Remove old Unity Water Standard Assets included with LB from the project. If you don't use Unity water from Standard Assets you can safely remove these.");
        private static readonly GUIContent removeRuntimeSamplesContent = new GUIContent("Remove Runtime Samples", "Remove runtime prefabs, scripts, and objects from the project");
        private static readonly GUIContent removeSRPContent = new GUIContent("Remove SRP Packages", "Remove the LB SRP folder and packages from the project. Even if using LWRP or HDRP, you will have most likely already opened and installed the assets in these packages. Now they can safely be removed.");
        private static readonly GUIContent uninstallBtnContent = new GUIContent("Uninstall", "WARNING: This will DELETE scripts and folders in your project. There is no UNDO button. Use with caution.");

        #endregion

        #region Event Methods

        private void OnGUI()
        {
            //if (labelFieldRichText == null)
            {
                labelFieldRichText = new GUIStyle("Label");
                labelFieldRichText.richText = true;
            }

            GUILayout.BeginVertical("HelpBox");
            EditorGUILayout.HelpBox("The LB Manager will help you optimise your landscapes prior to shipping your game. It will remove all unneeded LB components. [Technical Preview]", MessageType.Info, true);

            EditorGUI.BeginChangeCheck();
            landscape = (LBLandscape)EditorGUILayout.ObjectField(landscapeGUIContent, landscape, typeof(LBLandscape), true);
            if (EditorGUI.EndChangeCheck() && landscape != null)
            {
                landscapeTemplateName = "LB_Backup_" + landscape.name + "_Template";
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            EditorGUIUtility.labelWidth += labelWidthOffset;

            #region Start of Phase 1
            currentBgndColor = GUI.backgroundColor;
            GUI.backgroundColor = GUI.backgroundColor * (EditorGUIUtility.isProSkin ? 0.7f : 1.3f);
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = currentBgndColor;
            EditorGUILayout.LabelField("<color=" + txtColourName + "><b>Phase 1 - Backup</b></color>", labelFieldRichText);

            EditorGUILayout.HelpBox("You need to: Backup each landscape in each scene to a template.", MessageType.Info, true);
            EditorGUIUtility.labelWidth -= labelWidthOffset;
            landscapeTemplateName = EditorGUILayout.TextField(saveTemplateNameContent, landscapeTemplateName);
            EditorGUIUtility.labelWidth += labelWidthOffset;

            if (GUILayout.Button("Backup Template", GUILayout.MaxWidth(130f)))
            {
                bool isSceneSaveRequired = false;

                if (landscape != null)
                {

                    LBEditorCommon.SaveTemplate(landscape, LBEditorCommon.LBVersion,
                                                landscapeTemplateName,
                                                ref isSceneSaveRequired,
                                                createTemplatePackage,
                                                addMapTexturesToTemplatePackage,
                                                addLayerHeightmapTexturesToTemplatePackage,
                                                addLBLightingToTemplate,
                                                addPathsToTemplate,
                                                addStencilsToTemplate,
                                                addPathMeshMaterialsToTemplate);
                }
                else
                {
                    EditorUtility.DisplayDialog("Backup Template", "Select a landscape above so that you can back it up to a template", "Got it!");
                }
            }

            GUILayout.EndVertical(); // End of Phase 1
            #endregion End Phase 1

            EditorGUILayout.Space();

            #region Start of Phase 2
            currentBgndColor = GUI.backgroundColor;
            GUI.backgroundColor = GUI.backgroundColor * (EditorGUIUtility.isProSkin ? 0.7f : 1.3f);
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = currentBgndColor;
            EditorGUILayout.LabelField("<color=" + txtColourName + "><b>Phase 2 - Optimise</b></color>", labelFieldRichText);

            EditorGUILayout.HelpBox("You need to: Optimise each landscape in EACH scene by removing unnecessary scripts. You will need to open each scene one at a time.", MessageType.Info, true);

            isRemovePrefabItems = EditorGUILayout.Toggle(removePrefabItemContent, isRemovePrefabItems);
            isRemoveMapPaths = EditorGUILayout.Toggle(removeMapPathContent, isRemoveMapPaths);
            isRemoveStencils = EditorGUILayout.Toggle(removeStencilContent, isRemoveStencils);
            isRemoveLBLandscape = EditorGUILayout.Toggle(removeLBLandscapeContent, isRemoveLBLandscape);

            if (GUILayout.Button(finaliseBtnContent, GUILayout.MaxWidth(130f)))
            {
                if (landscape != null)
                {

                    if (EditorUtility.DisplayDialog("Finalise Landscape", "This action will clear all LB Editor data based on your preferences above.\n\nWARNING: There is NO UNDO.", "FINALISE", "Cancel"))
                    {
                        if (isRemovePrefabItems) { RemovePrefabItems(); }
                        if (isRemoveMapPaths) { RemoveMapPaths(); }
                        if (isRemoveStencils) { RemoveStencils(); }
                        if (isRemoveLBLandscape) { RemoveLBLandscape(); }
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("Finalise Landscape", "Select a landscape above so that you can finalise it", "Got it!");
                }
            }

            GUILayout.EndVertical(); // End of Phase 2
            #endregion End Phase 2

            EditorGUILayout.Space();

            #region Start of Phase 3
            currentBgndColor = GUI.backgroundColor;
            GUI.backgroundColor = GUI.backgroundColor * (EditorGUIUtility.isProSkin ? 0.7f : 1.3f);
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = currentBgndColor;
            EditorGUILayout.LabelField("<color=" + txtColourName + "><b>Phase 3 - Uninstall</b></color>", labelFieldRichText);

            EditorGUILayout.HelpBox("You need to: Selectively remove unnecessary Landscape Builder scripts in the project. Re-import LB package to re-install them.", MessageType.Info, true);

            isRemoveDemoScene = EditorGUILayout.Toggle(removeDemoSceneContent, isRemoveDemoScene);
            isRemoveDemoAssets = EditorGUILayout.Toggle(removeDemoAssetsContent, isRemoveDemoAssets);
            isRemoveUnityWater = EditorGUILayout.Toggle(removeUnityWaterContent, isRemoveUnityWater);
            isRemoveRuntimeSamples = EditorGUILayout.Toggle(removeRuntimeSamplesContent, isRemoveRuntimeSamples);
            isRemoveEditorScripts = EditorGUILayout.Toggle(removeEditorScriptsContent, isRemoveEditorScripts);
            isRemoveSRP = EditorGUILayout.Toggle(removeSRPContent, isRemoveSRP);

            if (GUILayout.Button(uninstallBtnContent, GUILayout.MaxWidth(130f)))
            {
                if (EditorUtility.DisplayDialog("Uninstall components", "This action will uninstall all selected components. Make sure you have Finalised all landscape in all scenes before proceeding. If you are unsure Cancel.\n\nWARNING: There is NO UNDO.", "DO IT!", "Cancel"))
                {
                    // Close the LB Editor if it is open
                    var lbW = LBEditorHelper.GetLBW();
                    if (lbW != null) { lbW.Close(); }

                    if (isRemoveDemoAssets) { UninstallDemoAssets(); }
                    else if (isRemoveDemoScene) { UninstallDemoScene(); }
                    if (isRemoveUnityWater) { UninstallUnityWater(); }
                    if (isRemoveRuntimeSamples) { UinstallRuntimeSamples(); }
                    if (isRemoveEditorScripts) { UninstallEditorScripts(); }
                    if (isRemoveSRP) { UninstallSRP(); }
                }
            }

            GUILayout.EndVertical(); // End of Phase 3
            #endregion End Phase 3

            EditorGUIUtility.labelWidth -= 10f;
            EditorGUILayout.EndScrollView();

            GUILayout.EndVertical();
        }

        private void OnEnable()
        {
            if (EditorGUIUtility.isProSkin) { txtColourName = "White"; }
        }

        #endregion

        #region Public Static Methods

        // Add a menu item so that the editor window can be opened via the window menu tab
        [MenuItem("Window/Landscape Builder/Landscape Builder Manager")]
        public static void ShowWindow()
        {
            // Show existing window instance. If one doesn't exist, make one.
            EditorWindow.GetWindow(typeof(LBManager), false, "LB Manager");
        }

        #endregion

        #region Private Methods - General

        /// <summary>
        /// Create or clear the shared recyclable component list to reduce GC
        /// </summary>
        private void ClearTempComponentList()
        {
            if (tempComponentList == null) { tempComponentList = new List<Component>(10); }
            else { tempComponentList.Clear(); }
        }

        #endregion

        #region Private Methods - Phase 2

        /// <summary>
        /// This removes the LBLandscape script and all meta-data about the landscape
        /// from the parent gameobject. It also removes the mesh controller if there is one present
        /// </summary>
        private void RemoveLBLandscape()
        {
            if (landscape != null)
            {
                string landscapeName = landscape.name;

                // Remove the LandscapeMeshController if it exists
                LBLandscapeMeshController lmc = landscape.gameObject.GetComponent<LBLandscapeMeshController>();
                if (lmc != null) { Object.DestroyImmediate(lmc); }

                // Remove any old terrain mesh controllers (these will only exist if something went wrong during a combine mesh operation)
                LBTerrainMeshController[] terrainMeshControllers = landscape.gameObject.GetComponentsInChildren<LBTerrainMeshController>();

                int numTerrainMeshControllers = terrainMeshControllers == null ? 0 : terrainMeshControllers.Length;

                for (int tmc = 0; tmc < numTerrainMeshControllers; tmc++)
                {
                    DestroyImmediate(terrainMeshControllers[tmc].gameObject);
                }

                Object.DestroyImmediate(landscape);

                Debug.Log("LB Manager. Finalise Landscape. Removed LBLandscape component and meta-data from " + landscapeName);
            }
        }

        /// <summary>
        /// Remove LBPrefabItem scripts with their ID data
        /// </summary>
        private void RemovePrefabItems()
        {
            if (landscape != null)
            {
                LBPrefabItem[] prefabItems = landscape.GetComponentsInChildren<LBPrefabItem>(true);

                int numPrefabItems = prefabItems == null ? 0 : prefabItems.Length;

                for (int i = numPrefabItems - 1; i >= 0; i--)
                {
                    Object.DestroyImmediate(prefabItems[i]);
                }

                Debug.Log("LB Manager. Finalise Landscape. Removed " + numPrefabItems + " LBPrefabItem components from " + landscape.name);
            }
        }

        /// <summary>
        /// Remove all the MapPath scripts from the landscape. Retain any path meshes.
        /// </summary>
        private void RemoveMapPaths()
        {
            if (landscape != null)
            {
                LBMapPath[] mapPaths = landscape.GetComponentsInChildren<LBMapPath>(true);
                int numMapPaths = mapPaths == null ? 0 : mapPaths.Length;

                ClearTempComponentList();

                for (int i = numMapPaths - 1; i >= 0; i--)
                {
                    mapPaths[i].gameObject.GetComponentsInChildren(true, tempComponentList);

                    // If there is only the transform and the LBMapPath scripts, remove the whole gameobject,
                    // else only remove the LBMapPath script component
                    if (tempComponentList.Count == 2) { Object.DestroyImmediate(mapPaths[i].gameObject); }
                    else { Object.DestroyImmediate(mapPaths[i]); }

                    ClearTempComponentList();
                }

                Debug.Log("LB Manager. Finalise Landscape. Removed " + numMapPaths + " LBMapPath components and data from " + landscape.name);
            }
        }

        /// <summary>
        /// Remove all Stencil scripts, configuration and data from the landscape
        /// </summary>
        private void RemoveStencils()
        {
            if (landscape != null)
            {
                LBStencil[] stencils = landscape.GetComponentsInChildren<LBStencil>(true);
                int numStencils = stencils == null ? 0 : stencils.Length;

                ClearTempComponentList();

                for (int i = numStencils - 1; i >= 0; i--)
                {
                    stencils[i].gameObject.GetComponentsInChildren(true, tempComponentList);

                    // If there is only the transform and the LBStencil scripts, remove the whole gameobject,
                    // else only remove the LBStencil script component
                    if (tempComponentList.Count == 2) { Object.DestroyImmediate(stencils[i].gameObject); }
                    else { Object.DestroyImmediate(stencils[i]); }

                    ClearTempComponentList();
                }

                Debug.Log("LB Manager. Finalise Landscape. Removed " + numStencils + " LBStencil components and data from " + landscape.name);
            }
        }


        #endregion

        #region Private Methods - Phase 3

        private void UninstallDemoScene()
        {
            if (Directory.Exists(LBSetup.demosceneFolder))
            {
                if (AssetDatabase.DeleteAsset(LBSetup.demosceneFolder + "/LBDemoScene2.unity"))
                {
                    Debug.Log("LB Manager - Uninstalled the Demo Scene");
                }
            }
        }

        /// <summary>
        /// Delete the Demo Scene folder and the templates that ship with LB.
        /// </summary>
        private void UninstallDemoAssets()
        {
            if (Directory.Exists(LBSetup.demosceneFolder))
            {
                AssetDatabase.DeleteAsset(LBSetup.demosceneFolder);
                
            }
            if (Directory.Exists(LBSetup.templatesFolder))
            {
                AssetDatabase.DeleteAsset(LBSetup.templatesFolder + "/DemoLandscape1 Template.prefab");
                AssetDatabase.DeleteAsset(LBSetup.templatesFolder + "/DemoLandscape3 Template.prefab");
                AssetDatabase.DeleteAsset(LBSetup.templatesFolder + "/FPS Forest Demo Template.prefab");
                AssetDatabase.DeleteAsset(LBSetup.templatesFolder + "/Mountains Plane Demo Template.prefab");
                AssetDatabase.DeleteAsset(LBSetup.templatesFolder + "/ObjPath Demo Template.prefab");
                AssetDatabase.DeleteAsset(LBSetup.templatesFolder + "/Swiss Mountains Demo Template.prefab");
            }

            Debug.Log("LB Manager - Uninstalled the Demo assets.");
        }

        /// <summary>
        /// Remove legacy Unity Water assets that are in the Standard Assets
        /// folder in Landscape Builder
        /// </summary>
        private void UninstallUnityWater()
        {
            AssetDatabase.DeleteAsset(LBSetup.lbFolder + "/Standard Assets/Environment/Water");
            AssetDatabase.DeleteAsset(LBSetup.lbFolder + "/Standard Assets/Environment/Water (Basic)");
            Debug.Log("LB Manager - Uninstalled Legacy Unity Water");
        }

        /// <summary>
        /// Delete the Samples folder which includes runtime prefabs, scripts etc.
        /// </summary>
        private void UinstallRuntimeSamples()
        {
            if (Directory.Exists(LBSetup.samplesFolder))
            {
                AssetDatabase.DeleteAsset(LBSetup.samplesFolder);
                Debug.Log("LB Manager - Uninstalled Runtime Samples");
            }
        }

        /// <summary>
        /// Delete Editor scripts including Designers.
        /// Do not delete the LBEditorCommon.cs script as this is used
        /// by LBManager.
        /// </summary>
        private void UninstallEditorScripts()
        {
            // Remove the in-scene Designers
            // This assumes there are no active designers in any landscape in any scene
            if (Directory.Exists(LBSetup.scriptsDesignersFolder))
            {
                // Cannot delete the whole folder because it contains LBStencilBrushPainter.cs which is a
                // dependency of LBStencil.cs (not just the stencil custom editor)

                // Remove the global #define for the LB Editor scripts so that LBGroupDesignerItem
                // is not referenced in LBLandscapeTerrain.PopulateTerrainWithGroups(..)
                const string LBEditor_Define = "LB_EDITOR";
                string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
                if (defines.Contains(LBEditor_Define))
                {
                    defines = defines.Replace(LBEditor_Define + ";", "");
                    defines = defines.Replace(LBEditor_Define, "");
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, defines);
                }

                AssetDatabase.DeleteAsset(LBSetup.scriptsDesignersFolder + "/LBGroupDesigner.cs");
                AssetDatabase.DeleteAsset(LBSetup.scriptsDesignersFolder + "/LBGroupLocationItem.cs");
                AssetDatabase.DeleteAsset(LBSetup.scriptsDesignersFolder + "/LBObjPathDesigner.cs");
                AssetDatabase.DeleteAsset(LBSetup.scriptsDesignersFolder + "/LBGroupDesignerItem.cs");
            }

            if (Directory.Exists(LBSetup.editorFolder))
            {
                AssetDatabase.DeleteAsset(LBSetup.editorFolder + "/3rdPartyLicenses");
                AssetDatabase.DeleteAsset(LBSetup.editorFolder + "/Textures");

                AssetDatabase.DeleteAsset(LBSetup.editorFolder + "/LandscapeBuilderGrassEditor.cs");
                AssetDatabase.DeleteAsset(LBSetup.editorFolder + "/LandscapeBuilderGrassSelector.cs");
                AssetDatabase.DeleteAsset(LBSetup.editorFolder + "/LBGroupDesignerItemEditor.cs");
                AssetDatabase.DeleteAsset(LBSetup.editorFolder + "/LBGroupLocationItemEditor.cs");
                AssetDatabase.DeleteAsset(LBSetup.editorFolder + "/LBTemplateEditor.cs");
                AssetDatabase.DeleteAsset(LBSetup.editorFolder + "/LBTextureGeneratorWindow.cs");

                AssetDatabase.DeleteAsset(LBSetup.editorFolder + "/LandscapeBuilderWindow.cs");
                AssetDatabase.DeleteAsset(LBSetup.editorFolder + "/LBImportTIFF.cs");
                AssetDatabase.DeleteAsset(LBSetup.editorFolder + "/LBEditorIntegration.cs");
                AssetDatabase.DeleteAsset(LBSetup.editorFolder + "/LibTIFF");
            }

            // Remove the setup folder which contains the grass setup data
            AssetDatabase.DeleteAsset(LBSetup.lbFolder + "/Setup");

            // Editor presets
            AssetDatabase.DeleteAsset(LBSetup.lbFolder + "/Prefabs/LB Default Resources.prefab");
            AssetDatabase.DeleteAsset(LBSetup.lbFolder + "/Prefabs/LBLogo.prefab");

            // Remove highlighters used in the editors
            AssetDatabase.DeleteAsset(LBSetup.materialsFolder + "/AreaHighlighter.mat");
            AssetDatabase.DeleteAsset(LBSetup.materialsFolder + "/HeightPickerHighlighter.mat");
            AssetDatabase.DeleteAsset(LBSetup.materialsFolder + "/LBLocation.mat");
            AssetDatabase.DeleteAsset(LBSetup.materialsFolder + "/LBClear.mat");
            AssetDatabase.DeleteAsset(LBSetup.materialsFolder + "/TerrainHighlighter.mat");
            AssetDatabase.DeleteAsset(LBSetup.lbFolder + "/AreaHighlighter.png");
            AssetDatabase.DeleteAsset(LBSetup.lbFolder + "/highlighter2.png");
            AssetDatabase.DeleteAsset(LBSetup.lbFolder + "/highlighter3.png");
            AssetDatabase.DeleteAsset(LBSetup.lbFolder + "/LB1Icon.psd");

            // ProjectorLight shader used with highlighters
            AssetDatabase.DeleteAsset(LBSetup.lbFolder + "/Standard Assets/Effects/Projectors");

            // Remove the manual
            AssetDatabase.DeleteAsset(LBSetup.lbFolder + "/Landscape Builder.pdf");

            Debug.Log("LB Manager - Uninstalled Editor scripts");
        }

        /// <summary>
        /// Remove the LB SRP folder from the project. This contains the LWRP and HDRP
        /// asset packages.
        /// </summary>
        private void UninstallSRP()
        {
            if (Directory.Exists(LBSetup.editorFolder))
            {
                if (AssetDatabase.DeleteAsset(LBSetup.lbFolder + "/SRP"))
                {
                    Debug.Log("LB Manager - Uninstalled SRP folder");
                }
            }
        }

        #endregion
    }
}
