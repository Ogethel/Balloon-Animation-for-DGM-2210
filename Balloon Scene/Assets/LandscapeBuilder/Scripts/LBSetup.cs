#if UNITY_EDITOR
// Landscape Builder. Copyright (c) 2016-2019 SCSM Pty Ltd. All rights reserved.
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.IO;

namespace LandscapeBuilder
{
    /// <summary>
    /// Script that performs miscellaneous setup tasks for Landscape Builder - runs when the project is opened.
    /// Dependencies are LBEditorHelper, LBIntegration
    /// </summary>
    [InitializeOnLoad]
    public static class LBSetup
    {
        #region Public Static Variables
        // Used to detect if the project has just been loaded
        // The LB Camera Animator needs to know this if editor preview mode is enabled
        // NOTE: It will only work with the first animator as it will get set to false
        // as soon as it re-initialises the preview mode.
        public static bool isProjectOpenedAnimator = true;

        public static string lbFolder = "Assets/LandscapeBuilder";
        public static string materialsFolder = "Assets/LandscapeBuilder/Materials";
        public static string modelsFolder = "Assets/LandscapeBuilder/Models";
        public static string shadersFolder = "Assets/LandscapeBuilder/Shaders";
        public static string modifiersFolder = "Assets/LandscapeBuilder/Modifiers";
        public static string demosceneFolder = "Assets/LandscapeBuilder/Demo Scene";
        public static string samplesFolder = "Assets/LandscapeBuilder/Samples";
        public static string editorFolder = "Assets/LandscapeBuilder/Editor";
        public static string templatesFolder = "Assets/LandscapeBuilder/Templates";
        public static string scriptsBehavioursFolder = "Assets/LandscapeBuilder/scripts/behaviours";
        public static string scriptsClassesFolder = "Assets/LandscapeBuilder/scripts/classes";
        public static string scriptsDesignersFolder = "Assets/LandscapeBuilder/scripts/designers";
        #endregion

        #region Private Static variables

        private static SerializedObject tagManager;

        #endregion

        #region Constructor

        static LBSetup()
        {
            string[] tagsToAdd = { "LB Combined Mesh", "LB Combined Group Mesh", "LB Rain Particles", "LB Hail Particles", "LB Snow Particles" };
            int[] layerNumbersToAdd = { LBCelestials.celestialsUnityLayer };
            string[] layersToAdd = { "LB Celestials" };

            FindTagAndLayerManager();
            CreateTags(tagsToAdd);
            CreateLayers(layersToAdd, layerNumbersToAdd);
            CreateFolders();
            SetupGrass();
            DefineSymbols();
            UpgradeProject();

            // Automatically cleanup old undo files
            LBLandscape.PerformUndoCleanup(7, false);
        }

        #endregion

        #region Private Static Methods

        // Upgrade project folders and move anything to correct location
        static void UpgradeProject()
        {
            // Move materials into a resources folders so that they are found in runtime generation
            LBEditorHelper.MoveAsset("LBSetup.UpgradeProject", materialsFolder, materialsFolder + "/Resources", "LBMoon.mat");
            LBEditorHelper.MoveAsset("LBSetup.UpgradeProject", materialsFolder, materialsFolder + "/Resources", "LBStar.mat");
            LBEditorHelper.MoveAsset("LBSetup.UpgradeProject", materialsFolder, materialsFolder + "/Resources", "LBTerrain.mat");

            // Move stars into resources folder so that celestials get built at runtime correctly
            LBEditorHelper.MoveAsset("LBSetup.UpgradeProject", modelsFolder, modelsFolder + "/Resources", "StarLowPolyFBX.fbx");

            // Move the shaders into a resources folder
            LBEditorHelper.MoveAsset("LBSetup.UpgradeProject", shadersFolder, shadersFolder + "/Resources", "LBTerrain-Base.shader");
            LBEditorHelper.MoveAsset("LBSetup.UpgradeProject", shadersFolder, shadersFolder + "/Resources", "LBTerrain-AddPass.shader");
            LBEditorHelper.MoveAsset("LBSetup.UpgradeProject", shadersFolder, shadersFolder + "/Resources", "LBTerrain-FirstPass.shader");

            // v1.3.5
            if (File.Exists(shadersFolder + "/" + "LBWeatherFX.shader") || File.Exists(shadersFolder + "/Resources/" + "LBWeatherFX.shader"))
            {
                Debug.LogWarning("WARNING: Landscape Builder - LBWeatherFX was replaced in v1.3.5 with LBImageFX - please consult the manual or contact support");
            }

            if (File.Exists(shadersFolder + "/" + "LBSimpleSSRR.shader"))
            {
                Debug.LogWarning("WARNING: Landscape Builder - LBSimpleSSRR was replaced in v1.3.5 with LBImageFX - please consult the manual or contact support");
            }

            // If the Designers are in the wrong folder, move them
            #if LB_EDITOR
            LBEditorHelper.MoveAsset("LBSetup.UpgradeProject", scriptsBehavioursFolder, scriptsDesignersFolder, "LBGroupDesigner.cs");
            #endif
       
            LBEditorHelper.MoveAsset("LBSetup.UpgradeProject", scriptsBehavioursFolder, scriptsDesignersFolder, "LBStencilBrushPainter.cs");

            // LB 2.0.4 may have incorrectly created a Material folder in Demo Scene\Models
            if (Directory.Exists(demosceneFolder + "/Models/Materials"))
            {
                if (AssetDatabase.DeleteAsset(demosceneFolder + "/Models/Materials"))
                {
                    Debug.Log("INFO: Landscape Builder - Removed legacy " + demosceneFolder + "/Models/Materials folder");
                }
            }

            // LB 2.2.0 Support for multiple SRP versions
            LBEditorHelper.RenameAsset("LBSetup.UpgradeProject", lbFolder + "/SRP", "LB_LWRP.unitypackage", "LB_LWRP_4.0.1.unitypackage");
            LBEditorHelper.RenameAsset("LBSetup.UpgradeProject", lbFolder + "/SRP", "LB_HDRP.unitypackage", "LB_HDRP_4.9.0.unitypackage");

            // LB 2.2.1 If the SRP version-based packages where imported, the rename above would not have updated original packages
            LBEditorHelper.DeleteAsset("LBSetup.UpgradeProject", lbFolder + "/SRP", "LB_HDRP.unitypackage");
            LBEditorHelper.DeleteAsset("LBSetup.UpgradeProject", lbFolder + "/SRP", "LB_LWRP.unitypackage");
        }

        /// <summary>
        /// Create the default folders used outside the Project Assets folder
        /// </summary>
        static void CreateFolders()
        {
            string undoFolderPath = "LandscapeBuilder/Undo/";
            string ssFolderPath = "LandscapeBuilder/ScreenShots/";

            // Create the undo folder if it doesn't already exist 
            if (!Directory.Exists(undoFolderPath))
            {
                Directory.CreateDirectory(undoFolderPath);
            }

            // Create screenshot folder if it doesn't exist
            if (!Directory.Exists(ssFolderPath))
            {
                Directory.CreateDirectory(ssFolderPath);
            }
        }

        /// <summary>
        /// Add the defines for LB so devs can use the following define in their scripts to call LB methods
        /// #if LANDSCAPE_BUILDER
        ///    // Call LB APIs
        /// #endif
        /// </summary>
        static void DefineSymbols()
        {
            // Is LB installed in this project
            const string LB_Define = "LANDSCAPE_BUILDER";

            // Are the LB Editor scripts installed in this project?
            // This is a subset of LB. See Manager\LBManager.cs
            const string LBEditor_Define = "LB_EDITOR";

            // EasyRoads3D v3 does not have it's own #define so we add one if it is installed
            const string LBEditorER3_Define = "LB_EDITOR_ER3";

            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);

            if (!defines.Contains(LB_Define))
            {
                if (string.IsNullOrEmpty(defines)) { defines = LB_Define; }
                else if (!defines.EndsWith(";")) { defines += ";" + LB_Define; }

                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, defines);
            }

            // Do the Landscape Builder editor scripts appear in the project?
            // Check for the LBGroupDesignerItem.cs
            System.Type groupDesignerType = System.Type.GetType("LandscapeBuilder.LBGroupDesignerItem, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", false, true);

            // If editor scripts exist, add the Editor define if it is missing
            if (groupDesignerType != null)
            {
                if (!defines.Contains(LBEditor_Define))
                {
                    if (string.IsNullOrEmpty(defines)) { defines = LBEditor_Define; }
                    else if (!defines.EndsWith(";")) { defines += ";" + LBEditor_Define; }

                    PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, defines);
                }
            }

            // If EasyRoads3D v3.x exists, add a #defines for it. This is currently used by LBPathImporter
            if (LBIntegration.isEasyRoads3DInstalled(false) && !defines.Contains(LBEditorER3_Define))
            {
                if (string.IsNullOrEmpty(defines)) { defines = LBEditorER3_Define; }
                else if (!defines.EndsWith(";")) { defines += ";" + LBEditorER3_Define; }
                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, defines);
            }
        }

        #endregion

        #region Public Static Methods
        public static void FindTagAndLayerManager()
        {
            tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        }

        public static void CreateTags(string[] newTags)
        {
            if (tagManager != null)
            {
                // Get the array of existing tags
                SerializedProperty tags = tagManager.FindProperty("tags");
                if (tags != null)
                {
                    if (tags.isArray)
                    {
                        // Iterate through the array of tags to add
                        for (int i = 0; i < newTags.Length; i++)
                        {
                            bool tagAlreadyExists = false;

                            // Iterate through the list of existing tags
                            for (int t = 0; t < tags.arraySize; t++)
                            {
                                if (tags.GetArrayElementAtIndex(t).stringValue == newTags[i])
                                {
                                    // Note if this tag is the same as the tag we are trying to add
                                    tagAlreadyExists = true;
                                    //Debug.Log("Tag exists : " + newTags[i]);
                                    break;
                                }
                            }

                            // If the tag doesn't already exist...
                            if (!tagAlreadyExists)
                            {
                                // ... add the tag to the array
                                int arrayInt = tags.arraySize;
                                tags.InsertArrayElementAtIndex(arrayInt);
                                SerializedProperty tagSP = tags.GetArrayElementAtIndex(arrayInt);
                                tagSP.stringValue = newTags[i];
                                Debug.Log("Adding tag: " + newTags[i]);
                            }
                        }

                        // Apply the modifications
                        tagManager.ApplyModifiedProperties();
                    }
                    else { Debug.LogWarning("LBSetup - Tags Serialized Property is not in the expected format (array), so tags could not be created."); }
                }
                else { Debug.LogWarning("LBSetup - Tags Serialized Property is null, so tags could not be created"); }
            }
            else { Debug.LogWarning("LBSetup - TagManager.asset could not be found, so tags could not be created."); }
        }

        public static void CreateLayers(string[] newLayers, int[] newlayerNumbers)
        {
            if (tagManager != null)
            {
                // Get the array of existing layers
                SerializedProperty layers = tagManager.FindProperty("layers");
                if (layers != null)
                {
                    if (layers.isArray)
                    {
                        for (int l = 0; l < newlayerNumbers.Length; l++)
                        {
                            SerializedProperty layerSP = layers.GetArrayElementAtIndex(newlayerNumbers[l]);
                            if (layerSP.stringValue != newLayers[l])
                            {
                                Debug.Log("Adding layer " + newlayerNumbers[l].ToString() + ": " + newLayers[l]);
                                layerSP.stringValue = newLayers[l];
                            }
                        }

                        // Apply the modifications
                        tagManager.ApplyModifiedProperties();
                    }
                    else { Debug.LogWarning("Layers Serialized Property is not in the expected format (array), so layers could not be created."); }
                }
                else { Debug.LogWarning("Layers Serialized Property is null, so layers could not be created"); }
            }
            else { Debug.LogWarning("TagManager.asset could not be found, so layers could not be created."); }
        }

        /// <summary>
        /// 1. Checks to see if there is LBGrassSetup in the Landscape folder
        /// </summary>
        public static void SetupGrass()
        {
            string LBGrassSetupProjectPath = "LandscapeBuilder/LBGrassSetup.dat";
            string LBGrassSetupAssetPath = "Assets/LandscapeBuilder/Setup/LBGrassSetup.dat";

            if (!File.Exists(LBGrassSetupProjectPath))
            {
                if (!File.Exists(LBGrassSetupAssetPath))
                {
                    Debug.LogError("Landscape Builder. LBSetup.SetupGrass - could not find " + LBGrassSetupAssetPath);
                }
                else
                {
                    // Attempt to copy the LBGrassSetup.dat file to the Assets folder
                    FileUtil.CopyFileOrDirectory(LBGrassSetupAssetPath, LBGrassSetupProjectPath);
                }
            }
        }
        #endregion
    }
}
#endif