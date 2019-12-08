using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace LandscapeBuilder
{
    /// <summary>
    /// Class for assisting with Integration with other assets
    /// that require classes contained in an editor script
    /// </summary>
    public class LBEditorIntegration : MonoBehaviour
    {
        #region MegaSplat

        #if __MEGASPLAT__

        /// <summary>
        /// Copy the Landscape textures from LB to MegaSplat
        /// EDITOR-ONLY
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="terrainMaterialType"></param>
        /// <param name="showErrors"></param>
        public static void MegaSplatCopyTextures(LBLandscape landscape, LBLandscape.TerrainMaterialType terrainMaterialType, bool showErrors)
        {
            string methodName = "LBEditorIntegration.MegaSplatCopyTextures";

            // Basic validation
            if (landscape == null) { if (showErrors) { Debug.LogWarning(methodName + " - landscape cannot be null"); } }
            else if (terrainMaterialType != LBLandscape.TerrainMaterialType.MegaSplat) { if (showErrors) { Debug.LogWarning(methodName + " - The Landscape Terrain Settings needs to have a Material Type of Mega Splat. Please set and try again."); } }
            else if (landscape.terrainCustomMaterial == null) { if (showErrors) { Debug.LogWarning(methodName + " - the landscape does not appear to have a MegaSplat material. Has it been initialised in the Landscape Terrain Settings?"); } }
            else
            {
                UnityEngine.Object obj = UnityEditor.Selection.activeObject;

                if (obj != null)
                {
                    System.Type cfgType = obj.GetType();
                    System.Type cfgEditorType = null;
                    System.Type TerrainPainterWindowType = null;
                    System.Type ITerrainPainterUtilityType = null;
                    System.Type TerrainToMegaSplatConfigType = null;

                    if (cfgType.FullName == "JBooth.MegaSplat.TextureArrayConfig")
                    {
                        //Debug.Log("MegaSplatCopyTextures cfgType found: " + cfgType.AssemblyQualifiedName);

                        UnityEngine.Object objTerrainToMegaSplatConfig = LBIntegration.MegaSplatCreateTerrainToMegaSplatConfig(landscape, terrainMaterialType, obj, showErrors);

                        // Get the textures from the landscape
                        List<LBTerrainTexture> terrainTextureList = landscape.TerrainTexturesList();

                        if (terrainTextureList != null)
                        {
                            if (terrainTextureList.Count > 0)
                            { 
                                // Add all the non-disabled textures to a list

                                // Previously MegaSplat uses an array of Texture2Ds. Now it has a TextureEntry class
                                // which can hold multiple textures (diffuse, normalmap, heightmap etc)
                                List<JBooth.MegaSplat.TextureArrayConfig.TextureEntry> textureEntryList = new List<JBooth.MegaSplat.TextureArrayConfig.TextureEntry>();
                                JBooth.MegaSplat.TextureArrayConfig.TextureEntry textureEntry = null;

                                for (int t = 0; t < terrainTextureList.Count; t++)
                                {
                                    LBTerrainTexture lbTerrainTexture = terrainTextureList[t];
                                    if (!lbTerrainTexture.isDisabled)
                                    {
                                        Texture2D texture2D = lbTerrainTexture.texture;
                                        if (texture2D != null)
                                        {
                                            textureEntry = new JBooth.MegaSplat.TextureArrayConfig.TextureEntry();
                                            if (textureEntry != null)
                                            {
                                                textureEntry.diffuse = texture2D;
                                                textureEntry.normal = lbTerrainTexture.normalMap;
                                                textureEntry.height = lbTerrainTexture.heightMap;
                                                textureEntryList.Add(textureEntry);
                                            }
                                        }
                                    }
                                }
                                try
                                {
                                    if (textureEntryList.Count > 0)
                                    {
                                        cfgEditorType = System.Type.GetType("JBooth.MegaSplat.TextureArrayConfigEditor, Assembly-CSharp-Editor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", true, true);
                                        TerrainPainterWindowType = System.Type.GetType("JBooth.TerrainPainter.TerrainPainterWindow, Assembly-CSharp-Editor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", true, true);
                                        ITerrainPainterUtilityType = System.Type.GetType("JBooth.TerrainPainter.ITerrainPainterUtility, Assembly-CSharp-Editor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", true, true);
                                        TerrainToMegaSplatConfigType = objTerrainToMegaSplatConfig.GetType();

                                        var cfgEditorWindow = UnityEditor.EditorWindow.GetWindow<UnityEditor.EditorWindow>("Inspector", false);

                                        if (cfgEditorType == null)
                                        {
                                            if (showErrors) { Debug.LogWarning(methodName + " - could not find TextureArrayConfigEditor editor class. Please Report."); }
                                        }
                                        else if (TerrainPainterWindowType == null)
                                        {
                                            if (showErrors) { Debug.LogWarning(methodName + " - could not find TerrainPainterWindowType editor class. Please Report."); }
                                        }
                                        else if (ITerrainPainterUtilityType == null)
                                        {
                                            if (showErrors) { Debug.LogWarning(methodName + " - could not find ITerrainPainterUtilityType editor class. Please Report."); }
                                        }
                                        else if (TerrainToMegaSplatConfigType == null)
                                        {
                                            if (showErrors) { Debug.LogWarning(methodName + " - could not find TerrainToMegaSplatConfigType editor class. Please Report."); }
                                        }
                                        else
                                        {
                                            // LB 2.0.6 Beta 7j add compile step back in (was previously broken and had to be done manually in the editor)
                                            //string[] shaderFeatures = { "_TERRAIN" };
                                            //LBIntegration.MegaSplatCompileShader(landscape, shaderFeatures, false, true);


                                            // Previously MegaSplat uses an array of Texture2Ds. Now it has a TextureEntry class
                                            cfgType.GetField("sourceTextures").SetValue(obj, textureEntryList);

                                            if (cfgEditorWindow != null)
                                            {
                                                cfgEditorType.InvokeMember("CompileConfig", System.Reflection.BindingFlags.InvokeMethod, null, null, new object[] { obj });

                                                // Select the landscape in the Hierarchy and open the Terrain Painter window
                                                UnityEditor.Selection.activeGameObject = landscape.gameObject;
                                                var windowTerrainPainter = UnityEditor.EditorWindow.GetWindow(TerrainPainterWindowType, false);
                                                if (windowTerrainPainter == null) { Debug.LogWarning("LBIntegration.MegaSplatCopyTextures - Could not open TerrainPainter window. Please Report."); }
                                                else
                                                {
                                                    // Add the TextureArrayConfig to the TerrainToMegaSplatConfig file
                                                    TerrainToMegaSplatConfigType.InvokeMember("config", System.Reflection.BindingFlags.SetField | System.Reflection.BindingFlags.Public, null, objTerrainToMegaSplatConfig, new object[] { obj });

                                                    // Updates the Paint tab in Terrain Painter
                                                    TerrainPainterWindowType.InvokeMember("config", System.Reflection.BindingFlags.SetField | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public, null, windowTerrainPainter, new object[] { obj });

                                                    // Initialise Utilities (else results in a timing issue where Utilities not found first time)
                                                    TerrainPainterWindowType.InvokeMember("InitPluginUtilities", System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public, null, windowTerrainPainter, new object[] { });

                                                    // We need to give time for MegaSplat to configure the Texture Array
                                                    Debug.Log("INFO: Setting up MegaSplat Texture Array...");

                                                    // Update "config" field of the Terrain Converter on the Utilities tab
                                                    IList utilitiesList = (IList)TerrainPainterWindowType.InvokeMember("utilities", System.Reflection.BindingFlags.GetField | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public, null, windowTerrainPainter, new object[] { });
                                                    if (utilitiesList == null) { if (showErrors) { Debug.LogWarning("LBIntegration.MegaSplatCopyTextures - Could not find Utilities. Please Report."); } }
                                                    else if (utilitiesList != null)
                                                    {
                                                        if (utilitiesList.Count == 0) { if (showErrors) { Debug.LogWarning("LBIntegration.MegaSplatCopyTextures - No Utilities found. Please Report."); } }

                                                        // Located the Terrain To Splat Converter utility
                                                        foreach (var tPainterInterface in utilitiesList)
                                                        {
                                                            if (tPainterInterface.ToString() == "JBooth.MegaSplat.TerrainToSplatConverter")
                                                            {
                                                                //Debug.Log("Interface: " + tPainterInterface.GetType().Name);
                                                                tPainterInterface.GetType().InvokeMember("config", System.Reflection.BindingFlags.SetField | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null, tPainterInterface, new object[] { objTerrainToMegaSplatConfig });

                                                                int tabIndex = LBIntegration.MegaSplatGetTerrainPainterTabIndex("Utility", true);

                                                                // Show the Utilities tab
                                                                TerrainPainterWindowType.InvokeMember("tab", System.Reflection.BindingFlags.SetField | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public, null, windowTerrainPainter, new object[] { tabIndex });

                                                                //var terrainJobs = TerrainPainterWindowType.InvokeMember("terrains", System.Reflection.BindingFlags.GetField | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public, null, windowTerrainPainter, new object[] { });

                                                                ////Debug.Log("Calling OnGUI");
                                                                //tPainterInterface.GetType().InvokeMember("OnGUI", System.Reflection.BindingFlags.InvokeMethod, null, tPainterInterface, new object[] { terrainJobs });
                                                                break;
                                                            }
                                                        }
                                                    }

                                                    LBIntegration.MegaSplatUpdateShaderSplat(landscape, "Albedo", false, obj, true);
                                                }
                                            }
                                            else { Debug.LogWarning(methodName + " - Could not open TextureArrayConfigEditor window. Please Report."); }
                                        }
                                    }
                                }
                                catch (System.Exception ex)
                                {
                                    if (showErrors) { Debug.LogWarning(methodName + " something has gone wrong (MegaSplat 1.73+ required). Please report. " + ex.Message); }
                                }
                            }
                        }
                    }
                    else { Debug.LogWarning("MegaSplatCopyTextures - Please select the Texture Array Config in the Project window and try again."); }
                }
                else { Debug.LogWarning("MegaSplatCopyTextures - Please select the Texture Array Config in the Project window and try again."); }
            }
        }


        #endif
        #endregion

        #region MicroSplat
        #if __MICROSPLAT__


        /// <summary>
        /// Attempt to recompile MicroSplat shader, ensuring the name matches the landscape name
        /// USAGE:
        /// bool isSuccessful = MicroSplatCompileShader(landscape, customTerrainMaterial, true);
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="terrainMat"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static bool MicroSplatCompileShader(LBLandscape landscape, Material terrainMat, bool showErrors)
        {
            bool isSuccessful = false;
            string methodName = "LBEditorIntegration.MicroSplatCompileShader";

            //System.Type MicroSplatShaderGUIType = null;

            try
            {
                if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " landscape is null. Please Report"); } }
                else if (terrainMat == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " terrainMat is null. Please Report"); } }
                else
                {
                    Shader shader = terrainMat.shader;

                    if (shader == null)
                    {
                        if (showErrors) { Debug.LogWarning(methodName + " - could not find MicroSplat shader. Please Report."); }
                    }
                    else if (!shader.name.Contains("MicroSplat"))
                    {
                        if (showErrors) { Debug.LogWarning(methodName + " - terrain material is not using a MicroSplat shader. [" + shader.name + "]. Please Report"); }
                    }
                    else
                    {
                        // Create a new instance of the compiler class
                        MicroSplatShaderGUI.MicroSplatCompiler microSplatCompiler = new MicroSplatShaderGUI.MicroSplatCompiler();
                        string shaderPath = AssetDatabase.GetAssetPath(shader);
                        string shaderBasePath = shaderPath.Replace(".shader", "_Base.shader");

                        if (microSplatCompiler == null) { if (showErrors) { Debug.LogWarning(methodName + " - could not create MicroSplatCompiler instance. Please Report."); } }
                        else
                        {
                            microSplatCompiler.Init();
                            string[] shaderFeatures = terrainMat.shaderKeywords;

                            // Ensure shader names match the landscape name
                            string baseName = "Hidden/MicroSplat/" + landscape.name + "_Base";

                            string baseShaderOutput = microSplatCompiler.Compile(shaderFeatures, baseName);
                            string regularShaderOutput = microSplatCompiler.Compile(shaderFeatures, "MicroSplat/" + landscape.name, baseName);

                            //Debug.Log("[DEBUG] regularShaderOutput: " + regularShaderOutput);

                            // Copy the output back to the shader files in the project folder.
                            System.IO.File.WriteAllText(shaderPath, regularShaderOutput);
                            System.IO.File.WriteAllText(shaderBasePath, baseShaderOutput);

                            // Currently we don't use this but we "might" in the future
                            if (shaderFeatures.Contains("_MESHOVERLAYSPLATS"))
                            {
                                string meshOverlayShader = microSplatCompiler.Compile(shaderFeatures, "MicroSplat/" + landscape.name, null, true);
                                System.IO.File.WriteAllText(shaderPath.Replace(".shader", "_MeshOverlay.shader"), meshOverlayShader);
                            }
                            AssetDatabase.Refresh();

                            isSuccessful = !string.IsNullOrEmpty(baseShaderOutput) && !string.IsNullOrEmpty(regularShaderOutput);
                        }                        
                    }
                }
            }
            catch (System.Exception ex)
            {
                if (showErrors) { Debug.LogWarning("LBIntegration.MegaSplatCompileShader something has gone wrong. Please report. " + ex.Message); }
            }

            return isSuccessful;
        }

        /// <summary>
        /// Get the MicroSplatData folder in the project folder, given a terrain material.
        /// trimAsset = true will remove the "Assets/" from the beginning of the folder
        /// </summary>
        /// <param name="terrainMat"></param>
        /// <param name="trimAssets"></param>
        /// <returns></returns>
        public static string GetMicroSplatDataFolder(Material terrainMat, bool trimAssets)
        {
            string path = LBEditorHelper.GetAssetFolder(terrainMat);

            if (string.IsNullOrEmpty(path))
            {
                path = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path;
            }

            if (string.IsNullOrEmpty(path))
            {
                path = "Assets";
            }

            path = path.Replace("\\", "/");
            if (path.Contains("/"))
            {
                path = path.Substring(0, path.LastIndexOf("/"));
            }
            path += "/MicroSplatData";

            if (trimAssets)
            {
                path = path.Replace("Assets/", "");
            }

            //Debug.Log("[DEBUG] path: " + path);

            return path;
        }

        /// <summary>
        /// Find the TextureArrayConfig for a given MicroSplat terrain material.
        /// This assumes it is always in the same project folder
        /// </summary>
        /// <param name="terrainMat"></param>
        /// <returns></returns>
        public static JBooth.MicroSplat.TextureArrayConfig GetTextureArrayConfig(Material terrainMat)
        {
            JBooth.MicroSplat.TextureArrayConfig textureArrayConfig = null;

            if (terrainMat != null)
            {
                string folder = GetMicroSplatDataFolder(terrainMat, true);
                textureArrayConfig = LBEditorHelper.GetAsset<JBooth.MicroSplat.TextureArrayConfig>(folder, terrainMat.name.Replace("MicroSplat", "MicroSplatConfig") + ".asset");
            }
            return textureArrayConfig;
        }

        /// <summary>
        /// Copy LB Texturing tab Textures to MicroSplat.
        /// Typically called from within the Texturing tab of LB Editor window.
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="terrainMaterialType"></param>
        /// <param name="terrainMat"></param>
        /// <param name="showErrors"></param>
        public static void MicroSplatCopyTextures(LBLandscape landscape, LBLandscape.TerrainMaterialType terrainMaterialType, Material terrainMat, bool showErrors)
        {
           string methodName = "LBEditorIntegration.MicroSplatCopyTextures";

            // Basic validation
            if (landscape == null) { if (showErrors) { Debug.LogWarning(methodName + " - landscape cannot be null"); } }
            else if (terrainMaterialType != LBLandscape.TerrainMaterialType.MicroSplat) { if (showErrors) { Debug.LogWarning(methodName + " - The Landscape Terrain Settings needs to have a Material Type of MicroSplat. Please set and try again."); } }
            else if (terrainMat == null) { if (showErrors) { Debug.LogWarning(methodName + " - the landscape does not appear to have a MicroSplat material. Has it been initialised in the Landscape Terrain Settings?"); } }
            else
            {
                // Find the Texture Array Config file
                //JBooth.MicroSplat.TextureArrayConfig textureArrayConfig = LBEditorHelper.GetAsset<JBooth.MicroSplat.TextureArrayConfig>("MicroSplatData", terrainMat.name.Replace("MicroSplat","MicroSplatConfig") + ".asset");
                JBooth.MicroSplat.TextureArrayConfig textureArrayConfig = GetTextureArrayConfig(terrainMat);
                if (textureArrayConfig == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " could not find TextureArrayConfig [MicroSplatData/" + terrainMat.name.Replace("MicroSplat", "MicroSplatConfig") + ".asset]"); } }
                else
                {
                    // Get existing list of Textures from MicroSplat
                    List<JBooth.MicroSplat.TextureArrayConfig.TextureEntry> textureOldEntryList = textureArrayConfig.sourceTextures;
                    //int numMSTextures = (textureOldEntryList == null ? 0 : textureOldEntryList.Count);
                    int numLBTextures = (landscape.terrainTexturesList == null ? 0 : landscape.terrainTexturesList.Count);
                    LBTerrainTexture lbTrnTex = null;
                    JBooth.MicroSplat.TextureArrayConfig.TextureEntry textureEntry = null;

                    //Debug.Log("[DEBUG] numMSTextures: " + numMSTextures + " numLBTextures:" + numLBTextures);

                    // Create a new list
                    List<JBooth.MicroSplat.TextureArrayConfig.TextureEntry> textureNewEntryList = new List<JBooth.MicroSplat.TextureArrayConfig.TextureEntry>();

                    // Loop through all the Landscape Builder Textures
                    for (int lbTexIdx = 0; lbTexIdx < numLBTextures; lbTexIdx++)
                    {
                        lbTrnTex = landscape.terrainTexturesList[lbTexIdx];

                        if (lbTrnTex != null && !lbTrnTex.isDisabled)
                        {
                            // Find matching MicroSplat texture entry
                            textureEntry = textureOldEntryList.Find(te => (lbTrnTex.texture != null && te.diffuse != null && te.diffuse.name == lbTrnTex.texture.name) &&
                                                                          (lbTrnTex.normalMap == null && te.normal == null || (lbTrnTex.normalMap != null && te.normal != null && te.normal.name == lbTrnTex.normalMap.name)) &&
                                                                          (lbTrnTex.heightMap == null && te.height == null || (lbTrnTex.heightMap != null && te.height != null && te.height.name == lbTrnTex.heightMap.name))
                                                                    );
                            //textureEntry = textureOldEntryList.Find(te => lbTrnTex.texture != null && te.diffuse != null && te.diffuse.name == lbTrnTex.texture.name);

                            if (textureEntry != null)
                            {
                                // update tinted texture if required
                                textureEntry.diffuse = lbTrnTex.isTinted ? (lbTrnTex.tintedTexture == null ? lbTrnTex.texture : lbTrnTex.tintedTexture) : lbTrnTex.texture;

                                textureNewEntryList.Add(textureEntry);
                                //Debug.Log("[DEBUG] Added matching entry: " + textureEntry.diffuse.name);
                            }
                            else
                            {
                                // Add missing textures
                                textureEntry = new JBooth.MicroSplat.TextureArrayConfig.TextureEntry();
                                if (textureEntry != null)
                                {
                                    textureEntry.diffuse = lbTrnTex.isTinted ? (lbTrnTex.tintedTexture == null ? lbTrnTex.texture : lbTrnTex.tintedTexture) : lbTrnTex.texture;
                                    textureEntry.normal = lbTrnTex.normalMap;
                                    textureEntry.height = lbTrnTex.heightMap;
                                    textureNewEntryList.Add(textureEntry);

                                    //Debug.Log("[DEBUG] Adding new entry: " + lbTrnTex.texture.name);
                                }
                            }

                            // In MicroSplat the first splatprototype sets the default tilesize
                            if (lbTexIdx == 0)
                            {
                                // Convert UVs to MicroSplat tiling
                                Vector3 terrainSize = landscape.GetLandscapeTerrainSize();
                                // scale, offset (default offset to 0,0)
                                terrainMat.SetVector("_UVScale", new Vector4(1.0f / (lbTrnTex.tileSize.x / terrainSize.x), 1.0f / (lbTrnTex.tileSize.y / terrainSize.z), 0f, 0f));
                            }
                            //Texture2DArray diff = textureArrayConfig.GetTexture("_Diffuse") as Texture2DArray;
                        }
                    }

                    // Update the list of source textures in MicroSplat
                    textureArrayConfig.sourceTextures = textureNewEntryList;
                    staticMSTextureArrayConfig = textureArrayConfig;
                    // Gets called once only, after all the Inspectors have been updated
                    EditorApplication.delayCall += MicroSplatDelayedCompileConfig;
                }
            }
        }

        // Used to pass the TextureArrayConfig in the Project folder to the deletate method below.
        private static JBooth.MicroSplat.TextureArrayConfig staticMSTextureArrayConfig;
        /// <summary>
        /// Delegate method used when changing the list of textures for a landscape that has MicroSplat enabled.
        /// </summary>
        private static void MicroSplatDelayedCompileConfig()
        {
            //Debug.Log("[DEBUG] MicroSplatDelayedCompileConfig " + staticMSTextureArrayConfig.name);
            try
            {
                JBooth.MicroSplat.TextureArrayConfigEditor.CompileConfig(staticMSTextureArrayConfig);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("ERROR: LBEditorIntegration.MicroSplatDelayedCompileConfig failed - " + ex.Message);
            }
        }

        #endif
        #endregion

    }
}