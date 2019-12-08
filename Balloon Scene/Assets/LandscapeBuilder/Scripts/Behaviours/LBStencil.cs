using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace LandscapeBuilder
{
    [System.Serializable]
    [ExecuteInEditMode]
    public class LBStencil : MonoBehaviour
    {

        #region Enumerations

        public enum RenderResolution
        {
            _512x512 = 512,
            _1024x1024 = 1024,
            _2048x2048 = 2048,
        }

        public enum LayerResolution
        {
            _128x128 = 128,
            _256x256 = 256,
            _512x512 = 512,
            _1024x1024 = 1024,
            _2048x2048 = 2048,
            _4096x4096 = 4096,
            _8192x8192 = 8192
        }

        public enum StencilBrushType
        {
            CircleSolid = 10,
            CircleGradient = 15,
            CircleSmooth = 25,
            CircleSubtract = 35,
            EraserCircleSolid = 200
        }

        /// <summary>
        /// The source channel or method for importing pixels into a stencil layer compressed texture
        /// </summary>
        public enum LayerImportMethod
        {
            Alpha = 0,
            Grayscale = 1,
            RedChannel = 10,
            GreenChannel = 11,
            BlueChannel = 12
        }

        public enum LayerImportSource
        {
            PNG = 0,
            Terrains = 10,
            Texture2D = 15
        }

        #endregion

        #region Public Variables and Properties

        // NOTE: When these variables are changed or new ones are added, they many need to
        // also change in the LBTemplateStencil.cs class.

        public string stencilName = "Stencil";
        public string GUID = string.Empty;
        public bool showStencilInScene = false;
        public RenderResolution renderResolution = RenderResolution._1024x1024;
        public LayerImportSource layerImportSource = LayerImportSource.PNG;
        public LayerImportMethod layerImportMethod = LayerImportMethod.Alpha;
        public bool isFlipImportTopBottom = false;
        public bool isCreateMapAsRGBA = false;      // Use with Vegetation Studio
        public bool isCreateMapPerTerrain = false;  // Use with Vegetation Studio
        public bool autoShowLBEditor = true;
        public List<LBStencilLayer> stencilLayerList;

        public bool showStencilSettings = false;
        [Range(1f, 500f)] public float brushSize = 50f;
        [Range(0.01f, 1f)] public float smoothStrength = 0.5f;

        public bool brushEnabled = false;
        #if UNITY_EDITOR
        public LBStencilBrushPainter brushPainter = null;
        #endif
        public bool allowRepaint = false;
        public float landscapeHeight = -1f;

        // Serializing the activeStencilLayer seems to cause issues. Setting it to null in code doesn't seem to work as it "sticks" in the editor
        [System.NonSerialized] public LBStencilLayer activeStencilLayer = null;
        public LBLandscape landscapeCached = null;

        // If this is made a private variable in LBStencilInspector, it sometimes doesn't get updated correctly
        [System.NonSerialized] public StencilBrushType currentBrushType = StencilBrushType.CircleSolid;

        // remember current scene setup
        public Vector3 origSceneViewPivot;
        public Quaternion origSceneViewRotation;
        public Vector3 origSceneViewCameraPosition;
        public Quaternion origSceneViewCameraRotation;
        public Vector3 origSceneViewCameraLocalScale;
        public bool origSceneViewIsOrthographic = false;
        public float origSceneViewSize = 10f;
        public bool origSceneViewShowSelectionOutline = false;
        public bool isUnityFogOn = false;
        public float origTerrainBaseMapDistance = 0f;

        #endregion

        #region Initialisation

        // Use this for initialization - Gets called when selected in Unity Editor
        public void OnEnable()
        {
            if (stencilLayerList == null) { stencilLayerList = new List<LBStencilLayer>(); }
            if (string.IsNullOrEmpty(GUID)) { GUID = System.Guid.NewGuid().ToString(); }
        }

        #endregion

        #region Non-Static Public Methods

        /// <summary>
        /// Get a Stencil Layer from the Stencil, using the Stencil Layer GUID
        /// </summary>
        /// <param name="StencilLayerGUID"></param>
        /// <returns></returns>
        public LBStencilLayer GetStencilLayerByGUID(string stencilLayerGUID)
        {
            if (stencilLayerList != null)
            {
                return stencilLayerList.Find(slr => slr.GUID == stencilLayerGUID);
            }
            else { return null; }
        }

        /// <summary>
        /// Get the first Stencil Layer from the Stencil, using the LayerName.
        /// </summary>
        /// <param name="stencilLayerName"></param>
        /// <returns></returns>
        public LBStencilLayer GetStencilLayerByName(string stencilLayerName)
        {
            if (stencilLayerList != null)
            {
                return stencilLayerList.Find(slr => slr.LayerName == stencilLayerName);
            }
            else { return null; }
        }

        /// <summary>
        /// Copy values of an existing lbStencil into the current Stencil.
        /// NOTE: showStencilInScene will be set to false.
        /// </summary>
        /// <param name="lbStencilToCopy"></param>
        /// <param name="isGUIDCopied"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public bool CopyFrom(LBStencil lbStencilToCopy, bool isGUIDCopied, bool showErrors)
        {
            bool isSuccess = false;

            if (lbStencilToCopy == null) { if (showErrors) { Debug.LogWarning("ERROR: LBStencil.CopyFrom - lbStencilToCopy is null"); } }
            else
            {
                if (isGUIDCopied) { this.GUID = lbStencilToCopy.GUID; }

                // Check to see if the StencilToCopy or the existing GUID was defined. If not, create one
                if (string.IsNullOrEmpty(this.GUID)) { this.GUID = System.Guid.NewGuid().ToString(); }

                this.stencilName = lbStencilToCopy.stencilName;
                this.showStencilInScene = false;
                this.renderResolution = lbStencilToCopy.renderResolution;
                this.layerImportSource = lbStencilToCopy.layerImportSource;
                this.layerImportMethod = lbStencilToCopy.layerImportMethod;

                this.stencilLayerList = new List<LBStencilLayer>();

                if (lbStencilToCopy.stencilLayerList != null)
                {
                    LBStencilLayer templbStencilLayer = null;

                    foreach (LBStencilLayer lbStencilLayer in lbStencilToCopy.stencilLayerList)
                    {
                        templbStencilLayer = new LBStencilLayer(lbStencilLayer, true, false, false);
                        if (templbStencilLayer != null) { this.stencilLayerList.Add(templbStencilLayer); }
                    }
                }

                this.showStencilSettings = lbStencilToCopy.showStencilSettings;
                this.brushSize = lbStencilToCopy.brushSize;
                this.smoothStrength = lbStencilToCopy.smoothStrength;

                isSuccess = true;
            }
            return isSuccess;
        }

        /// <summary>
        /// Copy values of LBTemplateStencil into the current Stencil.
        /// NOTE: showStencilInScene will be set to false and renderResolution will be default (1024x1024)
        /// </summary>
        /// <param name="lbTemplateStencil"></param>
        /// <param name="isGUIDCopied"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public bool CopyFromTemplateStencil(LBTemplateStencil lbTemplateStencil, bool isGUIDCopied, bool showErrors)
        {
            bool isSuccess = false;

            if (lbTemplateStencil == null) { if (showErrors) { Debug.LogWarning("ERROR: LBStencil.CopyFrom - lbStencilToCopy is null"); } }
            else
            {
                if (isGUIDCopied) { this.GUID = lbTemplateStencil.GUID; }

                // Check to see if the StencilToCopy or the existing GUID was defined. If not, create one
                if (string.IsNullOrEmpty(this.GUID)) { this.GUID = System.Guid.NewGuid().ToString(); }

                this.stencilName = lbTemplateStencil.stencilName;
                this.showStencilInScene = false;

                this.stencilLayerList = new List<LBStencilLayer>();

                if (lbTemplateStencil.stencilLayerList != null)
                {
                    LBStencilLayer templbStencilLayer = null;

                    // Add cloned StencilLayers to the Stencil in the scene
                    foreach (LBStencilLayer lbStencilLayer in lbTemplateStencil.stencilLayerList)
                    {
                        templbStencilLayer = new LBStencilLayer(lbStencilLayer, false, false, false);
                        if (templbStencilLayer != null)
                        {
                            // Create and populate the 1-dimensional USHORT array with data from the Template
                            int layerArrayXDim = lbStencilLayer.layerArrayX.GetLength(0);

                            templbStencilLayer.layerArrayX = new ushort[layerArrayXDim];
                            for (int x = 0; x < layerArrayXDim; x++)
                            {
                                templbStencilLayer.layerArrayX[x] = lbStencilLayer.layerArrayX[x];
                            }

                            // Add the StencilLayer to the Stencil in the scene
                            this.stencilLayerList.Add(templbStencilLayer);
                        }
                    }

                    // Re-create the compressed texture from the USHORT layerArray
                    for (int sl = 0; sl < stencilLayerList.Count; sl++)
                    {
                        LBStencilLayer lbStencilLayer = stencilLayerList[sl];
                        if (lbStencilLayer.layerArrayX == null) { Debug.LogWarning("ERROR LBStencil.CopyFromTemplateStencil - could not get layerArrayX data for " + lbStencilLayer.LayerName); }
                        else
                        {
                            lbStencilLayer.CompressFromUShortX();
                            // Free up the temporary array used by the TemplateStencil
                            lbStencilLayer.DeallocLayerArrayX();
                        }
                    }
                }

                this.showStencilSettings = lbTemplateStencil.showStencilSettings;
                this.brushSize = lbTemplateStencil.brushSize;
                this.smoothStrength = lbTemplateStencil.smoothStrength;

                isSuccess = true;
            }
            return isSuccess;
        }

        /// <summary>
        /// Import a texture file (2D Map) into a new Stencil Layer.
        /// Uses the current setting for layerImportMethod (Alpha or Grayscale)
        /// </summary>
        /// <param name="folderPath"></param>
        /// <param name="fileName"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public bool ImportMap(string folderPath, string fileName, bool showErrors)
        {
            bool isSuccessful = false;

            string filePath = folderPath;
            if (!folderPath.EndsWith("/")) { filePath += "/"; }
            filePath += fileName;

            if (!File.Exists(filePath)) { if (showErrors) { Debug.Log("LBStencil.ImportMap - file does not exist: " + filePath); } }
            else if (Path.GetExtension(filePath).ToLower() != ".png") { Debug.LogWarning("ERROR: LBStencil.ImportMap - in this release, only PNG files are supported"); }
            else
            {
                try
                {
                    // Read in all the data from the PNG file
                    byte[] texData = File.ReadAllBytes(filePath);
                    Texture2D texture = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                    if (texture == null) { Debug.LogWarning("ERROR: LBStencil.ImportMap - could not create Texture2D - please report"); }
                    else if (!texture.LoadImage(texData, false)) { Debug.LogWarning("ERROR: LBStencil.ImportMap - could not import texture from " + filePath); }
                    else if (texture.width != texture.height) { Debug.LogWarning("ERROR: LBStencil.ImportMap - non-square textures are not supported"); }
                    else
                    {
                        string _layerName = Path.GetFileNameWithoutExtension(filePath);
                        isSuccessful = ImportTexture(_layerName, texture, showErrors);
                    }
                }
                catch (System.Exception ex)
                {
                    if (showErrors) { Debug.LogWarning("ERROR LBStencil.ImportMap - failed. " + ex.Message); }
                }
            }

            return isSuccessful;
        }

        public bool ImportMap(Texture2D texture, bool showErrors)
        {
            bool isSuccessful = false;

            if (texture == null) { Debug.LogWarning("ERROR: LBStencil.ImportMap - Texture2D is null - PLEASE REPORT"); }
            else if (texture.width != texture.height) { Debug.LogWarning("ERROR: LBStencil.ImportMap - non-square textures are not supported"); }
            else
            {
                isSuccessful = ImportTexture("heightmaps stencil layer", texture, showErrors);
            }

            return isSuccessful;
        }

        /// <summary>
        /// Import a texture into a new Stencil Layer.
        /// Uses the current setting for layerImportMethod (Alpha, R, G, B or Grayscale channels)
        /// </summary>
        /// <param name="layerName"></param>
        /// <param name="texture"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public bool ImportTexture(string layerName, Texture2D texture, bool showErrors)
        {
            bool isSuccessful = false;

            if (texture == null) { if (showErrors) { Debug.LogWarning("ERROR: LBStencil.ImportTexture - texture cannot be null"); } }
            else
            {
                int texWidth = texture.width;
                int texHeight = texture.height;

                int matchingResolution = 0, lowestMatch = 0;

                System.Array layerResEnumArray = System.Enum.GetValues(typeof(LayerResolution));

                // Loop through all the potential layer resolutions in the enumeration
                foreach (LayerResolution layerResEnumItem in layerResEnumArray)
                {
                    if (texWidth == (int)layerResEnumItem) { matchingResolution = texWidth; break; }
                    if (texWidth > (int)layerResEnumItem) { lowestMatch = (int)layerResEnumItem; }
                }

                // No matching resolution, so resize
                if (matchingResolution == 0)
                {
                    // If the imported texture is smaller than the lowest resolution, use the lowest resolution
                    if (lowestMatch == 0) { lowestMatch = (int)layerResEnumArray.GetValue(0); }

                    Debug.Log("INFO: LBStencil.ImportTexture - resizing from " + texWidth + "x" + texHeight + " to " + lowestMatch + "x" + lowestMatch);
                    LBTextureOperations.TexturePointScale(texture, lowestMatch, lowestMatch);

                    matchingResolution = lowestMatch;
                }

                // Add an empty layer with the matching resolution
                LBStencilLayer lbStencilLayer = new LBStencilLayer();
                if (lbStencilLayer != null)
                {
                    lbStencilLayer.LayerName = layerName;
                    lbStencilLayer.layerResolution = (LayerResolution)matchingResolution;
                    lbStencilLayer.compressedTexture = LBTextureOperations.CreateTexture(matchingResolution, matchingResolution, Color.clear, false, TextureFormat.ARGB32, false);
                    stencilLayerList.Add(lbStencilLayer);

                    if (lbStencilLayer.compressedTexture != null)
                    {
                        // Update the width/height variables in the case it was resized
                        texWidth = texture.width;
                        texHeight = texture.height;

                        if (isFlipImportTopBottom) { texture = LBTextureOperations.FlipTexture(texture); }

                        // Revalidate the input and output texture sizes
                        if (lbStencilLayer.compressedTexture.width != texWidth || lbStencilLayer.compressedTexture.height != texHeight)
                        {
                            if (showErrors) { Debug.LogWarning("ERROR: LBStencil.ImportTexture - the input texture does not match the compressed texture size."); }
                        }
                        else
                        {
                            Color[] compressedArray = lbStencilLayer.compressedTexture.GetPixels();
                            Color[] textureArray = texture.GetPixels();
                            if (compressedArray != null && textureArray != null)
                            {
                                ushort arrayPixel = (ushort)0;
                                Color inputPixelColour;
                                Color outputPixelColour = new Color();

                                // Convert the input texture into the Stencil Layer format
                                for (int px = 0; px < textureArray.Length; px++)
                                {
                                    inputPixelColour = textureArray[px];

                                    // Convert input pixel into a value 0-65535
                                    switch (layerImportMethod)
                                    {
                                        case LayerImportMethod.Alpha:
                                            arrayPixel = (ushort)Mathf.RoundToInt(inputPixelColour.a * 65535f);
                                            break;
                                        case LayerImportMethod.Grayscale:
                                            arrayPixel = (ushort)Mathf.RoundToInt(inputPixelColour.grayscale * 65535f);
                                            break;
                                        case LayerImportMethod.RedChannel:
                                            arrayPixel = (ushort)Mathf.RoundToInt(inputPixelColour.r * 65535f);
                                            break;
                                        case LayerImportMethod.GreenChannel:
                                            arrayPixel = (ushort)Mathf.RoundToInt(inputPixelColour.g * 65535f);
                                            break;
                                        case LayerImportMethod.BlueChannel:
                                            arrayPixel = (ushort)Mathf.RoundToInt(inputPixelColour.b * 65535f);
                                            break;
                                    }

                                    // Compress the ushort values into the Red and Green channels
                                    outputPixelColour.r = Mathf.Floor(arrayPixel / 256f) / 255f;
                                    outputPixelColour.g = (arrayPixel % 256f) / 255f;

                                    compressedArray[px] = outputPixelColour;
                                }
                                // Copy all the updated pixels back into the compressed texture
                                lbStencilLayer.compressedTexture.SetPixels(compressedArray);
                                lbStencilLayer.compressedTexture.Apply();
                                isSuccessful = true;
                            }
                        }
                    }
                }
            }

            return isSuccessful;
        }

        /// <summary>
        /// Script out the Stencil for use in a runtime script.
        /// NOTE: Currently does not output Layer Textures
        /// </summary>
        /// <param name="EndOfLineMarker"></param>
        /// <returns></returns>
        public string ScriptStencil(string EndOfLineMarker = "\n")
        {
            // Create a new instance of StringBuilder and give it an estimated capacity
            System.Text.StringBuilder sb = new System.Text.StringBuilder(1000);

            string eol = " ";

            // We always need a space between lines OR a end of line marker like "\n"
            if (EndOfLineMarker.Length > 0) { eol = EndOfLineMarker; }

            string stencilNameInst = "lbSt" + stencilName.Replace(" ", "");

            sb.Append("// Stencil Code generated from Landscape Builder 2 at " + System.DateTime.Now.ToShortTimeString() + " on " + System.DateTime.Now.ToLongDateString() + eol);
            sb.Append("// Assumes a LBLandscape object called landscape has already been defined." + eol);
            sb.Append("// Stencil Layer textures can be created with the SaveMaps feature in the Stencil tool." + eol);
            sb.Append(eol);
            sb.Append("// BEGIN Public variables to populate in the editor - Uncomment and add these to top of class" + eol);
            if (stencilLayerList != null)
            {
                sb.Append("// Stencil Layer input images" + eol);
                for (int sl = 0; sl < stencilLayerList.Count; sl++)
                {
                    LBStencilLayer stencilLayer = stencilLayerList[sl];
                    if (stencilLayer != null)
                    {
                        string stencilLayerVariableName = stencilNameInst + stencilLayer.LayerName + "Tex";
                        // Remove spaces
                        stencilLayerVariableName = stencilLayerVariableName.Replace(" ", "");
                        sb.Append("//public Texture2D " + stencilLayerVariableName + " = null;");
                        sb.Append(eol);
                    }
                }
            }

            sb.Append("//" + eol);
            sb.Append("// END Public variables" + eol + eol);

            sb.Append("#region Stencil " + stencilName + eol);
            sb.Append("// Add a new Stencil to the (empty) landscape");
            sb.Append(eol);

            sb.Append("LBStencil " + stencilNameInst + " = LBStencil.CreateStencilInScene(landscape, landscape.gameObject);" + eol);
            sb.Append("if (" + stencilNameInst + " != null)" + eol);
            sb.Append("{" + eol);
            sb.Append("\t" + stencilNameInst + ".stencilName = \"" + stencilName + "\";" + eol);
            sb.Append("\t" + stencilNameInst + ".name = \"" + stencilName + "\";" + eol);
            sb.Append("\t" + stencilNameInst + ".GUID = \"" + GUID + "\";" + eol);
            sb.Append("\t/// Import PNG files into stencil layers (at runtime) and add as filters." + eol);
            sb.Append("\t" + stencilNameInst + ".layerImportSource = LBStencil.LayerImportSource." + layerImportSource.ToString() + ";" + eol);
            sb.Append("\t" + stencilNameInst + ".layerImportMethod = LBStencil.LayerImportMethod." + layerImportMethod.ToString() + ";" + eol);
            sb.Append("\t// If importing LB Map Textures into Stencil Layers, set isFlipImportTopBottom to true. At runtime, this should almost always be true" + eol);
            sb.Append("\t" + stencilNameInst + ".isFlipImportTopBottom = true; " + eol);
            //sb.Append("\t" + stencilNameInst + ".isFlipImportTopBottom = " + isFlipImportTopBottom.ToString().ToLower() + "; " + eol);
            sb.Append("\t" + stencilNameInst + ".isCreateMapAsRGBA = " + isCreateMapAsRGBA.ToString().ToLower() + "; " + eol);
            sb.Append("\t" + stencilNameInst + ".isCreateMapPerTerrain = " + isCreateMapPerTerrain.ToString().ToLower() + "; " + eol);
            sb.Append(eol);

            sb.Append("\t// Remove the default first layer" + eol);
            sb.Append("\tif (" + stencilNameInst + ".stencilLayerList.Count > 0) { " + stencilNameInst + ".stencilLayerList.RemoveAt(0); }" + eol);
            sb.Append(eol);

            if (stencilLayerList != null)
            {
                sb.Append("\t// Start Stencil Layers" + eol);
                for (int sl = 0; sl < stencilLayerList.Count; sl++)
                {
                    LBStencilLayer stencilLayer = stencilLayerList[sl];
                    if (stencilLayer != null)
                    {
                        string stencilLayerVariableName = stencilNameInst + stencilLayer.LayerName + "Tex";
                        // Remove spaces
                        stencilLayerVariableName = stencilLayerVariableName.Replace(" ", "");

                        sb.Append("\tif (" + stencilNameInst + ".ImportTexture(\"" + stencilLayer.LayerName + "\"," + stencilLayerVariableName + ", false))" + eol);
                        sb.Append("\t{" + eol);
                        sb.Append("\t\tint slIdx = " + stencilNameInst + ".stencilLayerList.Count - 1;" + eol);
                        sb.Append("\t\t" + stencilNameInst + ".stencilLayerList[slIdx].GUID = \"" + stencilLayer.GUID + "\";" + eol);
                        sb.Append("\t\t" + stencilNameInst + ".stencilLayerList[slIdx].layerResolution = LBStencil.LayerResolution." + stencilLayer.layerResolution + ";" + eol);
                        sb.Append("\t\t" + stencilNameInst + ".stencilLayerList[slIdx].colourInEditor = new Color(" + stencilLayer.colourInEditor.r + "f," + stencilLayer.colourInEditor.g + "f," + stencilLayer.colourInEditor.b + "f," + stencilLayer.colourInEditor.a + "f);" + eol);
                        sb.Append("\t}" + eol);
                    }
                }
                sb.Append("\t// End Stencil Layers" + eol);
            }

            sb.Append("}" + eol);
            sb.Append("#endregion" + eol);
            sb.Append("// END OF CODE SEGMENT" + eol);

            return sb.ToString();
        }

        #endregion

        #region Static Public Methods

        /// <summary>
        ///  Create a new Stencil in the scene with 1 Stencil Layer, and return the LBStencil instance
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="landscapeGameObject"></param>
        /// <returns></returns>
        public static LBStencil CreateStencilInScene(LBLandscape landscape, GameObject landscapeGameObject)
        {
            LBStencil lbStencil = null;

            if (landscapeGameObject == null) { Debug.LogWarning("ERROR: LBStencil.CreateStencilInScene landscapeGameObject cannot be null"); }
            else
            {
                // Create Stencil gameobject
                GameObject stencilObj = new GameObject("Stencil");
                if (stencilObj != null)
                {
                    stencilObj.transform.position = landscapeGameObject.transform.position;

                    // Make this stencil a child of the landscape
                    stencilObj.transform.parent = landscapeGameObject.transform;

                    lbStencil = stencilObj.AddComponent<LBStencil>();

                    if (lbStencil != null)
                    {
                        lbStencil.stencilLayerList = new List<LBStencilLayer>();

                        LBStencilLayer lbStencilLayer = new LBStencilLayer();
                        if (lbStencilLayer != null)
                        {
                            // Save texture space by not generating MipMaps.
                            lbStencilLayer.compressedTexture = LBTextureOperations.CreateTexture((int)lbStencilLayer.layerResolution, (int)lbStencilLayer.layerResolution, Color.clear, false, TextureFormat.ARGB32, false);
                            lbStencil.stencilLayerList.Add(lbStencilLayer);
                        }
                    }
                }
            }

            return lbStencil;
        }

        /// <summary>
        /// Create a new Stencil in the landscape with the data supplied by a Template.
        /// Typically used when restoring a Stencil from a LBTemplate
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="landscapeGameObject"></param>
        /// <param name="lbTemplateStencil"></param>
        /// <returns></returns>
        public static LBStencil CreateStencilInScene(LBLandscape landscape, LBTemplateStencil lbTemplateStencil, bool showErrors)
        {
            LBStencil lbStencil = null;

            if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR: LBStencil.CreateStencilInScene landscape cannot be null"); } }
            else if (landscape.gameObject == null) { if (showErrors) { Debug.LogWarning("ERROR: LBStencil.CreateStencilInScene landscape parent GameObject cannot be null"); } }
            else if (lbTemplateStencil == null) { if (showErrors) { Debug.LogWarning("ERROR: LBStencil.CreateStencilInScene lbTemplateStencil cannot be null"); } }
            else
            {
                // Create Stencil gameobject
                GameObject stencilObj = new GameObject(lbTemplateStencil.stencilName);
                if (stencilObj != null)
                {
                    stencilObj.transform.position = landscape.transform.position;

                    // Make this stencil a child of the landscape
                    stencilObj.transform.parent = landscape.transform;

                    lbStencil = stencilObj.AddComponent<LBStencil>();

                    if (lbStencil != null)
                    {
                        lbStencil.CopyFromTemplateStencil(lbTemplateStencil, true, showErrors);
                    }
                }
            }

            return lbStencil;
        }

        /// <summary>
        /// Get a list of the LBStencils which are child objects of a landscape
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static List<LBStencil> GetStencilsInLandscape(LBLandscape landscape, bool showErrors)
        {
            List<LBStencil> lbStencilList = new List<LBStencil>();

            if (landscape == null)
            {
                if (showErrors) { Debug.LogWarning("ERROR: LBStencil.GetStencilsInLandscape - landscape is not defined"); }
            }
            else
            {
                // Get the list of stencils in the landscape
                landscape.GetComponentsInChildren(false, lbStencilList);
            }

            return lbStencilList;
        }

        /// <summary>
        /// Find a stencil in a landscape given the stencil GUID.
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="stencilGUID"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static LBStencil GetStencilInLandscape(LBLandscape landscape, string stencilGUID, bool showErrors)
        {
            LBStencil lbStencil = null;

            if (landscape == null)
            {
                if (showErrors) { Debug.LogWarning("ERROR: LBStencil.GetStencilsInLandscape - landscape is not defined"); }
            }
            else
            {
                // Get the list of stencils in the landscape
                List<LBStencil> lbStencilList = LBStencil.GetStencilsInLandscape(landscape, showErrors);
                // Find the stencil in the list
                if (lbStencilList != null) { lbStencil = lbStencilList.Find(s => s.GUID == stencilGUID); }
            }

            return lbStencil;
        }

        /// <summary>
        /// Free memory no longer required for stencils that are not shown in the scene
        /// or the stencil layer is not shown in the scene
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="showErrors"></param>
        public static void FreeStencilResources(LBLandscape landscape, bool showErrors)
        {
            List<LBStencil> lbStencilList = GetStencilsInLandscape(landscape, showErrors);

            if (lbStencilList != null)
            {
                for (int s = 0; s < lbStencilList.Count; s++)
                {
                    LBStencil lbStencil = lbStencilList[s];

                    if (lbStencil != null)
                    {
                        // Deallocate the temporary USHORT arrays
                        // Deallocate renderTexture
                        if (lbStencil.stencilLayerList != null)
                        {
                            foreach (LBStencilLayer lbStencilLayer in lbStencil.stencilLayerList)
                            {
                                if (lbStencilLayer != null)
                                {
                                    if (!lbStencil.showStencilInScene || !lbStencilLayer.showLayerInScene)
                                    {
                                        lbStencilLayer.DeallocLayerArray();
                                        lbStencilLayer.renderTexture = null;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Remove all existing Stencils from the scene under a landscape parent gameobject
        /// </summary>
        /// <param name="lbLandscape"></param>
        public static void RemoveStencilsFromLandscape(LBLandscape lbLandscape, bool showErrors)
        {
            List<LBStencil> lbStencilList = GetStencilsInLandscape(lbLandscape, showErrors);

            if (lbStencilList != null)
            {
                // Go backwards through the list
                for (int i = lbStencilList.Count - 1; i >= 0; i--)
                {
                    DestroyImmediate(lbStencilList[i].gameObject);
                }
            }
        }

        #endregion
    }

    #region CustomEditor
#if UNITY_EDITOR

    [CustomEditor(typeof(LBStencil))]
    public class LBStencilInspector : Editor
    {
        #region Custom Editor variables

        private Texture2D texBrushCircleSolid = null;
        private Texture2D texBrushCircleGradient = null;
        private Texture2D texBrushCircleSmooth = null;
        private Texture2D texBrushCircleSubtract = null;
        private Texture2D texBrushEraserSolid = null;
        private Texture2D texArrowRight = null;
        private Texture2D texArrowLeft = null;
        private Texture2D texArrowUp = null;
        private Texture2D texArrowDown = null;

        private LBStencil lbStencil;
        private Event currentEvent;

        private GUIStyle buttonCompact;
        private GUIStyle buttonToggle;
        private static GUIStyle toggleButtonStyleNormal = null;
        private static GUIStyle toggleButtonStyleToggled = null;
        private static GUIStyle toggleBrushButtonStyleNormal = null;
        private static GUIStyle toggleBrushButtonStyleToggled = null;
        private string txtColourName = "Black";
        private Color defaultTextColour = Color.black;
        private string labelText;
        private GUIStyle labelFieldRichText;
        private Vector2 scrollPosition = Vector2.zero;
        private int layerIndex = 0;
        private int insertLayerPos = -1;
        private int moveLayerPos = -1;
        private int removeLayerPos = -1;
        private bool showLayerInSceneToggled = false;
        private bool refreshLayersInSceneRequired = false;

        //private Rect landscapeWorldBounds;

        #endregion

        #region Static GUIContent
        // Per stencil GUIContent
        private readonly static GUIContent stencilNameContent = new GUIContent("Stencil Name", "Create a unique name for the stencil in the project. This is important when saving Map textures");
        private readonly static GUIContent showLayersContent = new GUIContent("Stencil Layers", "Expand the list of Stencil Layers");
        private readonly static GUIContent autoShowLBEditorContent = new GUIContent("Auto Show LB Editor", "Automatically show the Landscape Builder Editor when Show Stencil is unselected");
        private readonly static GUIContent layerImportSourceContent = new GUIContent("Import Source", "Import a PNG texture file or use existing terrain heightmap data to create a new Stencil Layer. Use Texture2D to import psd, tif, png, jpg from a texture in an Assets folder.");
        private readonly static GUIContent layerImportMethodContent = new GUIContent("Import Method", "The method for importing a texture file into a new Stencil Layer");
        private readonly static GUIContent isFlipImportTopBottomContent = new GUIContent("Import Flip Texture", "Flip the texture top-bottom when importing a texture into a layer. Helpful if the texture is a LB Map Texture");
        private readonly static GUIContent isCreateMapAsRGBAContent = new GUIContent("Save Map As RGBA", "Used for 3rd party products like Vegetation Studio. By default Map textures are saved uncompressed for use as Maps for Texturing, Grass, Trees, and Meshes.");
        private readonly static GUIContent isCreateMapPerTerrainContent = new GUIContent("1 Map per Terrain", "Used for 3rd party products like, Vegetation Studio, that require texture filters or masks for each terrain in the landscape. By default, a single map texture is created for the whole landscape.");
        private readonly static GUIContent showStencilSettingsContent = new GUIContent("Stencil Settings", "Expand the Stencil Settings");
        private readonly static GUIContent showStencilInSceneContent = new GUIContent("Show Stencil", "Show the Stencil in the scene view with a top-down view. Make sure you turn this off before entering PLAY mode.");
        private readonly static GUIContent renderResolutionContent = new GUIContent("Render Resolution", "The resolution that the stencil layers are rendered in the scene view");
        private readonly static GUIContent brushSizeContent = new GUIContent("Brush Size", "The size of the brush to use while painting in the scene view");
        private readonly static GUIContent importMapContent = new GUIContent("Import", "Import a square image file into a new Stencil Layer");
        private readonly static GUIContent saveMapsContent = new GUIContent("Save Maps", "Save the enabled layers to Map textures in the LandscapeBuilder\\Maps folder.");
        private readonly static GUIContent paintButtonContent = new GUIContent("Paint", "Toggle Paint in scene view On/Off");
        private readonly static GUIContent undoPaintButtonContent = new GUIContent("Undo", "Undo current paint session");
        private readonly static GUIContent zoomAllButtonContent = new GUIContent("Zoom All", "Zoom out to see the whole landscape. Keyboard: /");
        private readonly static GUIContent zoomInButtonContent = new GUIContent("Zoom +", "Zoom In. Keyboard: [");
        private readonly static GUIContent zoomOutButtonContent = new GUIContent("Zoom -", "Zoom Out. Keyboard: ]");
        private readonly static GUIContent smoothButtonContent = new GUIContent("Smooth", "Smooth the edges of painted areas in the whole active Stencil Layer. Use Strength to change the amount of smoothing. The brush size does not affect smoothing.");
        private readonly static GUIContent smoothStrengthContent = new GUIContent("Strength", "The amount of smoothing to apply to the whole active Stencil Layer");
        private readonly static GUIContent stencilScriptContent = new GUIContent("S", "Script Stencil to Console window");
        // Brush content gets populated in OnEnable()
        private static GUIContent brushButtonCircleSolidContent = new GUIContent();
        private static GUIContent brushButtonCircleGradientContent = new GUIContent();
        private static GUIContent brushButtonEraserSolidContent = new GUIContent();
        private static GUIContent brushButtonCircleSmoothContent = new GUIContent();
        private static GUIContent brushButtonCircleSubtractContent = new GUIContent();
        // Navigation content gets populated in OnEnable()
        private static GUIContent buttonLeftContent = new GUIContent();
        private static GUIContent buttonRightContent = new GUIContent();
        private static GUIContent buttonUpContent = new GUIContent();
        private static GUIContent buttonDownContent = new GUIContent();

        // Per layer GUIContent
        private readonly static GUIContent activeLayerContent = new GUIContent("A", "Make this the active layer for painting");
        private readonly static GUIContent moveLayerDownContent = new GUIContent("V", "Move Layer down. If this is the last Stencil Layer, make it the first.");
        private readonly static GUIContent insertNewLayerContent = new GUIContent("I", "Insert new duplicate Layer above this Stencil Layer");
        private readonly static GUIContent layerNameContent = new GUIContent("Layer Name", "The user-defined name of this Stencil Layer");
        private readonly static GUIContent layerResolutionContent = new GUIContent("Resolution", "The resolution for this stencil layer");
        private readonly static GUIContent layerColourInEditorContent = new GUIContent("Colour", "The colour for this Stencil Layer as it will appear in the scene view");
        private readonly static GUIContent showButtonContent = new GUIContent("Show", "Show or expand item details");
        private readonly static GUIContent hideButtonContent = new GUIContent("Hide", "Hide or shrink item details");
        private readonly static GUIContent addMeshButtonContent = new GUIContent("Add Mesh", "Add or update a mesh based on the closed shapes in the Stencil Layer. Typically used for a navmesh. By default will add a clear material.");
        private readonly static GUIContent delMeshButtonContent = new GUIContent("Delete Mesh", "Delete the mesh for this Stencil Layer");
        private readonly static GUIContent bakeNavMeshButtonContent = new GUIContent("Bake", "Bake NavMesh for the scene");
        private readonly static GUIContent cancelBakeButtonContent = new GUIContent("Cancel");
        private readonly static GUIContent openNavigationButtonContent = new GUIContent("NavMesh Settings");
        #endregion

        #region SerializedProperties
        // Per-Stencil properties
        private SerializedProperty stencilNameProp;
        private SerializedProperty showStencilInSceneProp;
        private SerializedProperty renderResolutionProp;
        private SerializedProperty autoShowLBProp;
        private SerializedProperty layerImportMethodProp;
        private SerializedProperty layerImportSourceProp;
        private SerializedProperty isFlipImportTopBottomProp;
        private SerializedProperty isCreateMapAsRGBAProp;
        private SerializedProperty isCreateMapPerTerrainProp;
        private SerializedProperty showStencilSettingsProp;
        private SerializedProperty stencilLayerListProp;
        private SerializedProperty brushSizeProp;
        private SerializedProperty smoothStrengthProp;

        // Per-StencilLayer properties
        private SerializedProperty stencilLayerProp;
        private SerializedProperty layerNameProp;
        private SerializedProperty layerResolutionProp;
        private SerializedProperty showLayerInSceneProp;
        private SerializedProperty showLayerInEditorProp;
        private SerializedProperty colourInEditorProp;

        #endregion

        #region Initialisation

        public void OnEnable()
        {
            lbStencil = (LBStencil)target;

            if (EditorGUIUtility.isProSkin) { txtColourName = "White"; defaultTextColour = new Color(180f / 255f, 180f / 255f, 180f / 255f, 1f); }

            stencilNameProp = serializedObject.FindProperty("stencilName");
            showStencilInSceneProp = serializedObject.FindProperty("showStencilInScene");
            renderResolutionProp = serializedObject.FindProperty("renderResolution");
            autoShowLBProp = serializedObject.FindProperty("autoShowLBEditor");
            layerImportSourceProp = serializedObject.FindProperty("layerImportSource");
            layerImportMethodProp = serializedObject.FindProperty("layerImportMethod");
            isFlipImportTopBottomProp = serializedObject.FindProperty("isFlipImportTopBottom");
            isCreateMapAsRGBAProp = serializedObject.FindProperty("isCreateMapAsRGBA");
            isCreateMapPerTerrainProp = serializedObject.FindProperty("isCreateMapPerTerrain");
            showStencilSettingsProp = serializedObject.FindProperty("showStencilSettings");
            stencilLayerListProp = serializedObject.FindProperty("stencilLayerList");
            brushSizeProp = serializedObject.FindProperty("brushSize");
            smoothStrengthProp = serializedObject.FindProperty("smoothStrength");

            #if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui -= SceneGUI;
            SceneView.duringSceneGui += SceneGUI;
            #else
            SceneView.onSceneGUIDelegate -= SceneGUI;
            SceneView.onSceneGUIDelegate += SceneGUI;
            #endif

            // Load the textures and populate tooltips for brush buttons
            string editorTextureFolder = "Assets/LandscapeBuilder/Editor/Textures/";
            if (texBrushEraserSolid == null)
            {
                texBrushEraserSolid = AssetDatabase.LoadAssetAtPath(editorTextureFolder + "brushEraserSolid.png", typeof(Texture2D)) as Texture2D;
                if (texBrushEraserSolid != null) { brushButtonEraserSolidContent.image = texBrushEraserSolid; }
                else { Debug.LogWarning("ERROR: LBStencilInspector.OnEnable - could not load " + editorTextureFolder + "brushEraserSolid.png"); }
                brushButtonEraserSolidContent.tooltip = "Brush: Eraser Solid";
            }
            if (texBrushCircleSolid == null)
            {
                texBrushCircleSolid = AssetDatabase.LoadAssetAtPath(editorTextureFolder + "brushCircleSolid.png", typeof(Texture2D)) as Texture2D;
                if (texBrushCircleSolid != null) { brushButtonCircleSolidContent.image = texBrushCircleSolid; }
                else { Debug.LogWarning("ERROR: LBStencilInspector.OnEnable - could not load " + editorTextureFolder + "brushCircleSolid.png"); }
                brushButtonCircleSolidContent.tooltip = "Brush: Circle Solid";
            }
            if (texBrushCircleGradient == null)
            {
                texBrushCircleGradient = AssetDatabase.LoadAssetAtPath(editorTextureFolder + "brushCircleGradient.png", typeof(Texture2D)) as Texture2D;
                if (texBrushCircleGradient != null) { brushButtonCircleGradientContent.image = texBrushCircleGradient; }
                else { Debug.LogWarning("ERROR: LBStencilInspector.OnEnable - could not load " + editorTextureFolder + "brushCircleGradient.png"); }
                brushButtonCircleGradientContent.tooltip = "Brush: Circle Gradient";
            }
            if (texBrushCircleSmooth == null)
            {
                texBrushCircleSmooth = AssetDatabase.LoadAssetAtPath(editorTextureFolder + "brushCircleSmooth.png", typeof(Texture2D)) as Texture2D;
                if (texBrushCircleSmooth != null) { brushButtonCircleSmoothContent.image = texBrushCircleSmooth; }
                else { Debug.LogWarning("ERROR: LBStencilInspector.OnEnable - could not load " + editorTextureFolder + "brushCircleSmooth.png"); }
                brushButtonCircleSmoothContent.tooltip = "Brush: Circle Smooth";
            }
            if (texBrushCircleSubtract == null)
            {
                texBrushCircleSubtract = AssetDatabase.LoadAssetAtPath(editorTextureFolder + "brushCircleSubtract.png", typeof(Texture2D)) as Texture2D;
                if (texBrushCircleSubtract != null) { brushButtonCircleSubtractContent.image = texBrushCircleSubtract; }
                else { Debug.LogWarning("ERROR: LBStencilInspector.OnEnable - could not load " + editorTextureFolder + "brushCircleSubtract.png"); }
                brushButtonCircleSubtractContent.tooltip = "Brush: Reduction - reduce the effect of the pixels";
            }

            // Load the textures and populate tooltips for navigation arrow buttons
            if (texArrowDown == null)
            {
                texArrowDown = AssetDatabase.LoadAssetAtPath(editorTextureFolder + "arrowDown.png", typeof(Texture2D)) as Texture2D;
                if (texArrowDown != null) { buttonDownContent.image = texArrowDown; }
                else { Debug.LogWarning("ERROR: LBStencilInspector.OnEnable - could not load " + editorTextureFolder + "arrowDown.png"); }
                buttonDownContent.tooltip = "Scroll Scene view down (hold SHIFT to scroll faster). Keyboard: S";
            }
            if (texArrowUp == null)
            {
                texArrowUp = AssetDatabase.LoadAssetAtPath(editorTextureFolder + "arrowUp.png", typeof(Texture2D)) as Texture2D;
                if (texArrowUp != null) { buttonUpContent.image = texArrowUp; }
                else { Debug.LogWarning("ERROR: LBStencilInspector.OnEnable - could not load " + editorTextureFolder + "arrowUp.png"); }
                buttonUpContent.tooltip = "Scroll Scene view up (hold SHIFT to scroll faster). Keyboard: W";
            }
            if (texArrowLeft == null)
            {
                texArrowLeft = AssetDatabase.LoadAssetAtPath(editorTextureFolder + "arrowLeft.png", typeof(Texture2D)) as Texture2D;
                if (texArrowLeft != null) { buttonLeftContent.image = texArrowLeft; }
                else { Debug.LogWarning("ERROR: LBStencilInspector.OnEnable - could not load " + editorTextureFolder + "arrowLeft.png"); }
                buttonLeftContent.tooltip = "Scroll Scene view Left (hold SHIFT to scroll faster). Keyboard: A";
            }
            if (texArrowRight == null)
            {
                texArrowRight = AssetDatabase.LoadAssetAtPath(editorTextureFolder + "arrowRight.png", typeof(Texture2D)) as Texture2D;
                if (texArrowRight != null) { buttonRightContent.image = texArrowRight; }
                else { Debug.LogWarning("ERROR: LBStencilInspector.OnEnable - could not load " + editorTextureFolder + "arrowRight.png"); }
                buttonRightContent.tooltip = "Scroll Scene view right (hold SHIFT to scroll faster). Keyboard: D";
            }

            UpdateLandscape(true);

            //Debug.Log("OnEnable");
        }

        #endregion

        #region OnInspectorGUI

        // This function overides what is normally seen in the inspector window
        // This allows stuff like buttons to be drawn there
        public override void OnInspectorGUI()
        {
            lbStencil.allowRepaint = false;

            EditorGUIUtility.labelWidth = 150f;

            #region Configure Buttons and Styles
            if (buttonCompact == null)
            {
                buttonCompact = new GUIStyle("Button");
                buttonCompact.fontSize = 10;
            }

            if (labelFieldRichText == null)
            {
                labelFieldRichText = new GUIStyle("Label");
                labelFieldRichText.richText = true;
            }

            // Set up the toggle buttons styles
            if (toggleButtonStyleNormal == null)
            {
                // Create a new button or else will effect the Button style for other buttons too
                toggleButtonStyleNormal = new GUIStyle("Button");
                toggleButtonStyleToggled = new GUIStyle(toggleButtonStyleNormal);
                toggleButtonStyleNormal.fontStyle = FontStyle.Normal;
                toggleButtonStyleToggled.fontStyle = FontStyle.Bold;
                toggleButtonStyleToggled.normal.background = toggleButtonStyleToggled.active.background;
            }

            if (toggleBrushButtonStyleNormal == null)
            {
                toggleBrushButtonStyleNormal = new GUIStyle("Button");
                toggleBrushButtonStyleNormal.fixedWidth = 32;
                toggleBrushButtonStyleNormal.fixedHeight = 32;
                toggleBrushButtonStyleToggled = new GUIStyle(toggleBrushButtonStyleNormal);
                toggleBrushButtonStyleToggled.normal.background = toggleBrushButtonStyleToggled.active.background;
            }
            #endregion

            refreshLayersInSceneRequired = false;

            // Read in all the properties
            serializedObject.Update();

            // Stencil Settings
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(stencilNameProp, stencilNameContent);
            if (EditorGUI.EndChangeCheck())
            {
                if (stencilNameProp.stringValue.Length > 0) { lbStencil.gameObject.name = stencilNameProp.stringValue; }
            }

            if (lbStencil.gameObject.name.Length == 0 || lbStencil.gameObject.name.ToLower() == "stencil")
            {
                EditorGUILayout.HelpBox("Please create a unique Stencil Name for the project", MessageType.Warning);
            }

            #region Toggle Stencil ON/OFF
            // Show Stencil in Scene Toggle
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(showStencilInSceneProp, showStencilInSceneContent);
            if (EditorGUI.EndChangeCheck())
            {
                // Apply any property changes so far, change variables in ShowStencil, then read the properties again.
                serializedObject.ApplyModifiedProperties();

                // If not showing stencil in scene view, turn off Brush Painter if it is on.
                if (!showStencilInSceneProp.boolValue && lbStencil.brushEnabled)
                {
                    // Save the current layer
                    if (lbStencil.activeStencilLayer != null)
                    {
                        lbStencil.activeStencilLayer.CompressFromUShort();
                    }

                    lbStencil.brushEnabled = false;
                    EnableBrushPainter(false);
                }

                ShowStencil(showStencilInSceneProp.boolValue);

                ValidateActiveLayer();

                if (showStencilInSceneProp.boolValue) { SetDefaultActiveLayer(); }

                serializedObject.Update();
            }
            #endregion

            #region Stencil Settings
            EditorGUI.indentLevel += 1;
            showStencilSettingsProp.boolValue = EditorGUILayout.Foldout(showStencilSettingsProp.boolValue, showStencilSettingsContent);
            EditorGUI.indentLevel -= 1;

            if (showStencilSettingsProp.boolValue)
            {
                EditorGUILayout.PropertyField(autoShowLBProp, autoShowLBEditorContent);
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(renderResolutionProp, renderResolutionContent, GUILayout.MaxWidth(280f));
                if (EditorGUI.EndChangeCheck() && showStencilInSceneProp.boolValue)
                {
                    // User has changed the render texture resolution, so update the render textures
                    serializedObject.ApplyModifiedProperties();
                    RefreshLayers();
                    serializedObject.Update();
                }
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(layerImportSourceProp, layerImportSourceContent, GUILayout.MaxWidth(280f));
                if (EditorGUI.EndChangeCheck())
                {
                    if (layerImportSourceProp.intValue == (int)LBStencil.LayerImportSource.Terrains)
                    {
                        // Importing from terrain heightmap data requires Grayscale.
                        layerImportMethodProp.intValue = (int)LBStencil.LayerImportMethod.Grayscale;
                        // The heightmap also needs to be flipped
                        isFlipImportTopBottomProp.boolValue = true;
                    }
                    else if (layerImportSourceProp.intValue == (int)LBStencil.LayerImportSource.Texture2D)
                    {
                        // May need to flip the incoming texture
                        isFlipImportTopBottomProp.boolValue = true;
                    }
                    else
                    {
                        isFlipImportTopBottomProp.boolValue = false;
                    }
                }
                if (layerImportSourceProp.intValue == (int)LBStencil.LayerImportSource.PNG || layerImportSourceProp.intValue == (int)LBStencil.LayerImportSource.Texture2D)
                {
                    EditorGUILayout.PropertyField(layerImportMethodProp, layerImportMethodContent, GUILayout.MaxWidth(280f));
                    EditorGUILayout.PropertyField(isFlipImportTopBottomProp, isFlipImportTopBottomContent);
                }

                EditorGUILayout.PropertyField(isCreateMapAsRGBAProp, isCreateMapAsRGBAContent);

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(isCreateMapPerTerrainProp, isCreateMapPerTerrainContent);
                if (EditorGUI.EndChangeCheck())
                {
                    // When 1 Map per Terrain, by default enable (or disable) RGBA
                    isCreateMapAsRGBAProp.boolValue = isCreateMapPerTerrainProp.boolValue;
                }
            }
            #endregion

            if (showStencilInSceneProp.boolValue && stencilLayerListProp.arraySize > 0)
            {
                #region Paint and Zoom buttons
                // Paint and Zoom buttons
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(paintButtonContent, lbStencil.brushEnabled ? toggleButtonStyleToggled : toggleButtonStyleNormal, GUILayout.Width(68f)))
                {
                    ValidateActiveLayer();

                    // Toggle on/off
                    lbStencil.brushEnabled = !lbStencil.brushEnabled;

                    if (lbStencil.brushEnabled)
                    {
                        if (lbStencil.activeStencilLayer != null && lbStencil.activeStencilLayer.showLayerInScene)
                        {
                            EnableBrushPainter(true);
                        }
                        else
                        {
                            // No active layers
                            Debug.Log("INFO: LBStencil - please (A)ctivate a visible layer to begin painting");
                            lbStencil.brushEnabled = false;
                        }
                    }
                    // We must have just turned off the brush so save the data
                    else
                    {
                        EnableBrushPainter(lbStencil.brushEnabled);

                        if (lbStencil.activeStencilLayer != null)
                        {
                            lbStencil.activeStencilLayer.CompressFromUShort();
                            refreshLayersInSceneRequired = true;
                        }
                    }
                }

                // Return the scene view camera to the default zoomed, centred position
                if (GUILayout.Button(zoomAllButtonContent, GUILayout.MaxWidth(68f))) { Zoom(1f, true); }
                // The Repaint() is required for the RepeatButton to work.
                if (GUILayout.RepeatButton(zoomInButtonContent, GUILayout.Width(68f))) { ZoomIn(0.01f); Repaint(); }
                if (GUILayout.RepeatButton(zoomOutButtonContent, GUILayout.Width(68f))) { ZoomIn(-0.01f); Repaint(); }

                if (currentEvent == null) { currentEvent = Event.current; }

                if (currentEvent != null)
                {
                    if (currentEvent.type == EventType.KeyDown)
                    {
                        if (currentEvent.keyCode == KeyCode.A) { MoveSceneCamera("LEFT", -2f); Repaint(); }
                        else if (currentEvent.keyCode == KeyCode.D) { MoveSceneCamera("RIGHT", 2f); Repaint(); }
                        else if (currentEvent.keyCode == KeyCode.S) { MoveSceneCamera("DOWN", -2f); Repaint(); }
                        else if (currentEvent.keyCode == KeyCode.W) { MoveSceneCamera("UP", 2f); Repaint(); }
                        else if (currentEvent.keyCode == KeyCode.LeftBracket) { ZoomIn(0.1f); Repaint(); }
                        else if (currentEvent.keyCode == KeyCode.RightBracket) { ZoomIn(-0.1f); Repaint(); }
                    }
                    else if (currentEvent.type == EventType.KeyUp)
                    {
                        if (currentEvent.keyCode == KeyCode.Backslash) { Zoom(1f, true); Repaint(); }
                    }
                }


                if (lbStencil.brushEnabled)
                {
                    if (GUILayout.Button(undoPaintButtonContent, GUILayout.MaxWidth(50f)))
                    {
                        lbStencil.brushEnabled = false;
                        EnableBrushPainter(false);
                        lbStencil.activeStencilLayer.UnCompressToUShort();
                        lbStencil.activeStencilLayer.CopyUShortToRenderTexture();
                    }
                }

                EditorGUILayout.EndHorizontal();
                #endregion

                //EditorGUILayout.LabelField("EnableBrushPainter " + (lbStencil.brushEnabled ? "ON" : "OFF"));

                #region Brushes and Controls
                // Brushes
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button(brushButtonCircleSolidContent, lbStencil.currentBrushType == LBStencil.StencilBrushType.CircleSolid ? toggleBrushButtonStyleToggled : toggleBrushButtonStyleNormal))
                {
                    lbStencil.currentBrushType = LBStencil.StencilBrushType.CircleSolid;
                }

                if (GUILayout.Button(brushButtonCircleGradientContent, lbStencil.currentBrushType == LBStencil.StencilBrushType.CircleGradient ? toggleBrushButtonStyleToggled : toggleBrushButtonStyleNormal))
                {
                    lbStencil.currentBrushType = LBStencil.StencilBrushType.CircleGradient;
                }

                if (GUILayout.Button(brushButtonCircleSmoothContent, lbStencil.currentBrushType == LBStencil.StencilBrushType.CircleSmooth ? toggleBrushButtonStyleToggled : toggleBrushButtonStyleNormal))
                {
                    lbStencil.currentBrushType = LBStencil.StencilBrushType.CircleSmooth;
                }

                if (GUILayout.Button(brushButtonCircleSubtractContent, lbStencil.currentBrushType == LBStencil.StencilBrushType.CircleSubtract ? toggleBrushButtonStyleToggled : toggleBrushButtonStyleNormal))
                {
                    lbStencil.currentBrushType = LBStencil.StencilBrushType.CircleSubtract;
                }

                if (GUILayout.Button(brushButtonEraserSolidContent, lbStencil.currentBrushType == LBStencil.StencilBrushType.EraserCircleSolid ? toggleBrushButtonStyleToggled : toggleBrushButtonStyleNormal))
                {
                    lbStencil.currentBrushType = LBStencil.StencilBrushType.EraserCircleSolid;
                }

                #region Scene view Navigation
                //EditorGUILayout.BeginHorizontal();

                if (GUILayout.RepeatButton(buttonLeftContent, toggleBrushButtonStyleNormal)) { MoveSceneCamera("LEFT", -2f); Repaint(); }
                if (GUILayout.RepeatButton(buttonUpContent, toggleBrushButtonStyleNormal)) { MoveSceneCamera("UP", 2f); Repaint(); }
                if (GUILayout.RepeatButton(buttonDownContent, toggleBrushButtonStyleNormal)) { MoveSceneCamera("DOWN", -2f); Repaint(); }
                if (GUILayout.RepeatButton(buttonRightContent, toggleBrushButtonStyleNormal)) { MoveSceneCamera("RIGHT", 2f); Repaint(); }

                //EditorGUILayout.EndHorizontal();
                #endregion

                EditorGUILayout.EndHorizontal();

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(brushSizeProp, brushSizeContent);
                if (lbStencil.brushEnabled && EditorGUI.EndChangeCheck() && lbStencil.brushPainter != null)
                {
                    lbStencil.brushPainter.SetProjectorSize(brushSizeProp.floatValue);
                }

                // Smoothing is only available during painting
                if (lbStencil.brushEnabled)
                {
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button(smoothButtonContent, GUILayout.Width(70f)))
                    {
                        SmoothStencilLayer(smoothStrengthProp.floatValue);
                    }
                    EditorGUILayout.PropertyField(smoothStrengthProp, smoothStrengthContent);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space();
                }

                #endregion
            }

            EditorGUI.indentLevel += 1;
            // There was a change in 2019.3. Using PropertyField(stencilLayerListProp, showLayersContent) will show
            // all the list elements, than than show a Foldout - which was the default prior to 2019.3.
            // This will not make the scene dirty when changed
            stencilLayerListProp.isExpanded = EditorGUILayout.Foldout(stencilLayerListProp.isExpanded, showLayersContent);
            EditorGUI.indentLevel -= 1;

            if (stencilLayerListProp.isExpanded)
            {
                #region SaveMaps Import and +/- layer buttons
                // SaveMaps and +/- layer buttons not available during painting
                if (!lbStencil.brushEnabled)
                {
                    if (showStencilInSceneProp.boolValue) { EditorGUIUtility.labelWidth = 100f; }
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Layers: " + stencilLayerListProp.arraySize.ToString("00"));

                    if (GUILayout.Button(stencilScriptContent, GUILayout.Width(20f))) { Debug.Log(lbStencil.ScriptStencil("\n")); }

                    if (GUILayout.Button(importMapContent, GUILayout.Width(60f)))
                    {
                        #region Import from PNG File
                        if (layerImportSourceProp.intValue == (int)LBStencil.LayerImportSource.PNG)
                        {
                            string folderPath = string.Empty, fileName = string.Empty;
                            string dataFileFolderRelPath = LBEditorHelper.GetAssetsFolder();

                            if (LBEditorHelper.GetFilePathFromUser("Import Stencil Layer Data", dataFileFolderRelPath, "png", false, ref folderPath, ref fileName))
                            {
                                EditorUtility.DisplayProgressBar("Importing Stencil Layer", "Please Wait", 0.2f);
                                bool isImported = lbStencil.ImportMap(folderPath, fileName, true);
                                EditorUtility.ClearProgressBar();
                                if (isImported && showStencilInSceneProp.boolValue) { ShowLayer(lbStencil.stencilLayerList.Count - 1); }
                            }
                        }
                        #endregion

                        #region Import from Texture2D in asset folder
                        else if (layerImportSourceProp.intValue == (int)LBStencil.LayerImportSource.Texture2D)
                        {
                            string folderPath = string.Empty, fileName = string.Empty;
                            string dataFileFolderRelPath = LBEditorHelper.GetAssetsFolder();
                            string[] extensions = { "Texture2D", "png,psd,jpg,jpeg,tif" };

                            if (LBEditorHelper.GetFilePathFromUser("Import Stencil Layer Data", dataFileFolderRelPath, extensions, true, ref folderPath, ref fileName))
                            {
                                string filePath = folderPath;
                                if (!folderPath.EndsWith("/")) { filePath += "/"; }
                                filePath += fileName;

                                string _layerName = Path.GetFileNameWithoutExtension(filePath);

                                // Remove the prefix to the asset folder
                                filePath = "Assets/" + filePath.Replace(dataFileFolderRelPath + "/", "");

                                Texture2D tex = AssetDatabase.LoadAssetAtPath(filePath, typeof(Texture2D)) as Texture2D;
                                bool texIsReadable = true;

                                if (tex == null) { Debug.LogWarning("LBStencil Import Layer - could not find file at " + filePath); }
                                else
                                {
                                    try { Color testColour = tex.GetPixel(0, 0); if (testColour.a == 1f) { } }
                                    catch (UnityException e)
                                    {
                                        if (e.Message.StartsWith("Texture '" + tex.name + "' is not readable"))
                                        {
                                            if (EditorUtility.DisplayDialog("Texure is not readable", "Do you want to fix it now?", "Yes", "No"))
                                            {
                                                LBTextureOperations.EnableReadable(tex, true);
                                            }
                                            else { texIsReadable = false; }
                                        }
                                    }

                                    if (texIsReadable)
                                    {
                                        lbStencil.ImportTexture(_layerName, tex, true);
                                    }
                                }
                            }
                        }
                        #endregion

                        #region Import from existing terrain heightmaps
                        else if (layerImportSourceProp.intValue == (int)LBStencil.LayerImportSource.Terrains)
                        {
                            UpdateLandscape(true);
                            string baseMsg = "ERROR: LBStencil Import Terrain heightmaps into new layer - ";
                            if (lbStencil.landscapeCached == null) { Debug.LogWarning(baseMsg + "landscape is not cached. PLEASE REPORT"); }
                            else if (lbStencil.landscapeCached.landscapeTerrains == null) { { Debug.LogWarning(baseMsg + "terrain array is null. PLEASE REPORT"); } }
                            else if (lbStencil.landscapeCached.landscapeTerrains.Length < 1) { { Debug.LogWarning(baseMsg + "no terrains in this landscape. PLEASE REPORT"); } }
                            else
                            {
                                // Create a texture the size of the landscape
                                try
                                {
                                    bool continueToImport = true;
                                    int numTerrains = lbStencil.landscapeCached.landscapeTerrains.Length;
                                    int numTerrainsWide = Mathf.RoundToInt(Mathf.Sqrt(numTerrains));
                                    int heightmapResolution = lbStencil.landscapeCached.GetLandscapeTerrainHeightmapResolution();

                                    // We want to normalise the image created from the heightmaps, so we need the min/max heights
                                    Vector2 landscapeMinMaxHeight = lbStencil.landscapeCached.GetLandscapeMinMaxHeights();
                                    if (float.IsInfinity(landscapeMinMaxHeight.x) || float.IsInfinity(landscapeMinMaxHeight.y))
                                    {
                                        landscapeMinMaxHeight = Vector2.zero;
                                    }

                                    // Ensure texture is a power of 2 by using (heightmapResolution-1).
                                    Texture2D importHeightMapImage = new Texture2D(numTerrainsWide * (heightmapResolution - 1), numTerrainsWide * (heightmapResolution - 1), TextureFormat.ARGB32, false);

                                    // Loop through all the terrains, and update the appropriate pixels in the image for each terrain
                                    for (int index = 0; index < numTerrains; index++)
                                    {

                                        if (EditorUtility.DisplayCancelableProgressBar("Import Stencil Layer from Terrain Heightmap(s)", "Importing heightmap " + (index + 1).ToString() + " of " + numTerrains.ToString() + " terrains",
                                                   (float)(index + 1) / (float)numTerrains))
                                        {
                                            // If the user cancels the process, so exit loop and don't try to save the file
                                            continueToImport = false;
                                            break;
                                        }

                                        LBLandscapeTerrain.ExportImageBasedHeightmap(lbStencil.landscapeCached.landscapeTerrains[index].terrainData, lbStencil.landscapeCached.landscapeTerrains[index].transform.position,
                                                                                         lbStencil.landscapeCached.size, lbStencil.landscapeCached.transform.position, importHeightMapImage, landscapeMinMaxHeight);
                                    }

                                    // Check to see if the user cancelled
                                    if (continueToImport && importHeightMapImage != null)
                                    {
                                        bool isImported = lbStencil.ImportMap(importHeightMapImage, true);
                                        importHeightMapImage = null;
                                        EditorUtility.ClearProgressBar();
                                        if (isImported && showStencilInSceneProp.boolValue) { ShowLayer(lbStencil.stencilLayerList.Count - 1); }
                                    }
                                }
                                catch (System.Exception ex)
                                {
                                    Debug.LogError("Could not import heightmap data to new Stencil Layer - " + ex.Message);
                                }

                                EditorUtility.ClearProgressBar();
                            }
                        }
                        #endregion
                    }

                    // Save Maps button is only available when the stencil is shown in the scene.
                    // SaveLayersToMaps requires the compressed texture has been loaded into the USHORT array.
                    if (showStencilInSceneProp.boolValue)
                    {
                        if (GUILayout.Button(saveMapsContent, GUILayout.Width(80f)))
                        {
                            SaveLayersToMaps();
                        }
                    }
                    // Set limit of 8 StencilLayers per Stencil
                    if (GUILayout.Button("+", GUILayout.MaxWidth(30f)) && stencilLayerListProp.arraySize < 8)
                    {
                        // this will make a duplicate. Will only use default values if array (list) is empty
                        //stencilLayerListProp.arraySize += 1;

                        // Apply any property changes so far, update the list, then read the properties again.
                        serializedObject.ApplyModifiedProperties();

                        LBStencilLayer lbStencilLayer = new LBStencilLayer();
                        if (lbStencilLayer != null)
                        {
                            lbStencilLayer.compressedTexture = LBTextureOperations.CreateTexture((int)lbStencilLayer.layerResolution, (int)lbStencilLayer.layerResolution, Color.clear, false, TextureFormat.ARGB32, false);
                            lbStencil.stencilLayerList.Add(lbStencilLayer);
                            // Activate the new layer immediately so it can be painted into.
                            lbStencil.activeStencilLayer = lbStencilLayer;
                        }

                        serializedObject.Update();
                        if (showStencilInSceneProp.boolValue) { refreshLayersInSceneRequired = true; }
                    }
                    if (GUILayout.Button("-", GUILayout.MaxWidth(30f)))
                    {
                        if (stencilLayerListProp.arraySize > 0)
                        {
                            // Get the last Layer in the list
                            stencilLayerProp = stencilLayerListProp.GetArrayElementAtIndex(stencilLayerListProp.arraySize - 1);
                            string _layerName = "unknown";
                            if (stencilLayerProp != null)
                            {
                                layerNameProp = stencilLayerProp.FindPropertyRelative("LayerName");
                                if (layerNameProp != null) { _layerName = layerNameProp.stringValue; }
                            }

                            if (EditorUtility.DisplayDialog("Delete Stencil Layer?", "Deleting " + _layerName + " is final and cannot be undone.", "Delete Now", "Cancel"))
                            {
                                // If painting, save the current layer and turn off painting
                                if (lbStencil.activeStencilLayer != null && lbStencil.brushEnabled)
                                {
                                    lbStencil.activeStencilLayer.CompressFromUShort();
                                    EnableBrushPainter(false);
                                }

                                stencilLayerListProp.arraySize -= 1;

                                // Do we need to clean up the scene and update the position of remaining layers?
                                if (showStencilInSceneProp.boolValue) { refreshLayersInSceneRequired = true; }
                            }
                        }
                    }
                    GUILayout.EndHorizontal();
                    EditorGUIUtility.labelWidth = 150f;
                }
                #endregion

                EditorGUILayout.EndVertical();

                // Reset insert/move variables
                insertLayerPos = -1;
                moveLayerPos = -1;
                removeLayerPos = -1;

                // Create an auto-scrollable area for the Stencil Layers
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

                #region Layers
                // Iterate through the list of LBStencilLayers
                for (layerIndex = 0; layerIndex < stencilLayerListProp.arraySize; layerIndex++)
                {
                    showLayerInSceneToggled = false;
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    stencilLayerProp = stencilLayerListProp.GetArrayElementAtIndex(layerIndex);
                    if (stencilLayerProp != null)
                    {
                        #region Get properties for this LBStencilLayer in the list
                        layerNameProp = stencilLayerProp.FindPropertyRelative("LayerName");
                        layerResolutionProp = stencilLayerProp.FindPropertyRelative("layerResolution");
                        showLayerInSceneProp = stencilLayerProp.FindPropertyRelative("showLayerInScene");
                        showLayerInEditorProp = stencilLayerProp.FindPropertyRelative("showLayerInEditor");
                        colourInEditorProp = stencilLayerProp.FindPropertyRelative("colourInEditor");
                        #endregion

                        GUILayout.BeginHorizontal();
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.PropertyField(showLayerInSceneProp, GUIContent.none, GUILayout.Width(20f));
                        showLayerInSceneToggled = EditorGUI.EndChangeCheck();

                        // Did user turn off this layer AND was painting AND this was the active layer?
                        if (showLayerInSceneToggled && !showLayerInSceneProp.boolValue && lbStencil.brushEnabled && lbStencil.activeStencilLayer == lbStencil.stencilLayerList[layerIndex])
                        {
                            // Save the layer
                            // Save the current layer being painted
                            lbStencil.activeStencilLayer.CompressFromUShort();
                            lbStencil.activeStencilLayer = null;
                        }

                        if (showLayerInEditorProp.boolValue) { labelText = "<color=" + txtColourName + "><b>Layer " + (layerIndex + 1) + "</b></color>"; }
                        else { labelText = "<color=" + txtColourName + "><b>Layer " + (layerIndex + 1) + "</b> - " + layerNameProp.stringValue + "</color>"; }
                        EditorGUILayout.LabelField(labelText, labelFieldRichText);

                        #region Activate layer

                        // Bold the "A" text on the button if this is the active layer
                        buttonCompact.fontStyle = (lbStencil.activeStencilLayer == lbStencil.stencilLayerList[layerIndex]) ? FontStyle.Bold : FontStyle.Normal;
                        buttonCompact.normal.textColor = (lbStencil.activeStencilLayer == lbStencil.stencilLayerList[layerIndex]) ? Color.blue : defaultTextColour;

                        // Active Layer
                        if (GUILayout.Button(activeLayerContent, buttonCompact, GUILayout.Width(20f)))
                        {
                            // If painting, save the active layer first
                            if (lbStencil.brushEnabled && lbStencil.activeStencilLayer != null)
                            {
                                // Save the current layer being painted
                                lbStencil.activeStencilLayer.CompressFromUShort();
                            }

                            lbStencil.activeStencilLayer = lbStencil.stencilLayerList[layerIndex];
                            // Always make the active layer visible in the scene so that user can paint on it
                            if (!showLayerInSceneProp.boolValue)
                            {
                                showLayerInSceneProp.boolValue = true;
                                showLayerInSceneToggled = true;
                            }
                            //Debug.Log("INFO: Setting active layer " + lbStencil.activeStencilLayer.LayerName);
                        }

                        // Reset style back to normal
                        buttonCompact.fontStyle = FontStyle.Normal;
                        buttonCompact.normal.textColor = defaultTextColour;

                        #endregion

                        #region Move or Insert Layer
                        if (!lbStencil.brushEnabled)
                        {
                            // Move Down
                            if (GUILayout.Button(moveLayerDownContent, buttonCompact, GUILayout.Width(20f)))
                            {
                                moveLayerPos = layerIndex;
                                if (stencilLayerListProp.arraySize > 1)
                                {
                                    // Clear the active layer so that it doesn't mess up the Move
                                    lbStencil.activeStencilLayer = null;

                                    // Move down one position, or wrap round to start of list
                                    if (moveLayerPos < stencilLayerListProp.arraySize - 1)
                                    {
                                        stencilLayerListProp.MoveArrayElement(moveLayerPos, moveLayerPos + 1);
                                    }
                                    else { stencilLayerListProp.MoveArrayElement(moveLayerPos, 0); }
                                }
                                if (showStencilInSceneProp.boolValue) { refreshLayersInSceneRequired = true; }
                            }

                            // Insert duplicate (with new GUID)
                            if (GUILayout.Button(insertNewLayerContent, buttonCompact, GUILayout.Width(20f)) && stencilLayerListProp.arraySize < 8)
                            {
                                // Record the position to insert. The insert will occur outside the loop to prevent errors
                                insertLayerPos = layerIndex;
                            }
                        }
                        #endregion

                        if (showLayerInEditorProp.boolValue) { if (GUILayout.Button(hideButtonContent, buttonCompact, GUILayout.Width(40f))) { GUI.FocusControl(null); showLayerInEditorProp.boolValue = false; } }
                        else { if (GUILayout.Button(showButtonContent, buttonCompact, GUILayout.Width(40f))) { GUI.FocusControl(null); showLayerInEditorProp.boolValue = true; } }

                        #region Delete Layer
                        // Cannot delete a layer while painting is enabled
                        if (!lbStencil.brushEnabled)
                        {
                            if (GUILayout.Button("X", buttonCompact, GUILayout.MaxWidth(20f)))
                            {
                                if (EditorUtility.DisplayDialog("Delete Stencil Layer?", "Deleting " + layerNameProp.stringValue + " is final and cannot be undone.", "Delete Now", "Cancel"))
                                {
                                    // If painting, save the current layer and turn off painting before deleting a layer
                                    if (lbStencil.activeStencilLayer != null && lbStencil.brushEnabled)
                                    {
                                        lbStencil.activeStencilLayer.CompressFromUShort();
                                        EnableBrushPainter(false);
                                    }

                                    // Can't seem to remove the last array once we've fetched it above
                                    if (layerIndex < stencilLayerListProp.arraySize - 1) { stencilLayerListProp.DeleteArrayElementAtIndex(layerIndex); }
                                    else { removeLayerPos = layerIndex; }

                                    // Do we need to clean up the scene and update the position of remaining layers?
                                    if (showStencilInSceneProp.boolValue) { refreshLayersInSceneRequired = true; }
                                }
                            }
                        }
                        #endregion

                        GUILayout.EndHorizontal();

                        if (showLayerInEditorProp.boolValue)
                        {
                            #region Layer Settings
                            EditorGUILayout.PropertyField(layerNameProp, layerNameContent);
                            EditorGUI.BeginChangeCheck();
                            EditorGUILayout.PropertyField(layerResolutionProp, layerResolutionContent, GUILayout.MaxWidth(280f));
                            if (EditorGUI.EndChangeCheck())
                            {
                                // If painting, save the current layer and turn off painting before changing the layer resolution
                                if (lbStencil.activeStencilLayer != null && lbStencil.brushEnabled)
                                {
                                    lbStencil.activeStencilLayer.CompressFromUShort();
                                    serializedObject.ApplyModifiedProperties();
                                    EnableBrushPainter(false);
                                    serializedObject.Update();
                                }
                                else
                                {
                                    serializedObject.ApplyModifiedProperties();
                                    serializedObject.Update();
                                }

                                lbStencil.stencilLayerList[layerIndex].ChangeLayerResolution(layerResolutionProp.intValue);

                                if (showStencilInSceneProp.boolValue)
                                {
                                    // BUG: Doesn't seem to refresh the render texture
                                    ShowLayer(layerIndex);
                                    serializedObject.Update();
                                }
                            }
                            EditorGUI.BeginChangeCheck();
                            EditorGUILayout.PropertyField(colourInEditorProp, layerColourInEditorContent);
                            if (EditorGUI.EndChangeCheck() && showStencilInSceneProp.boolValue)
                            {
                                serializedObject.ApplyModifiedProperties();
                                UpdateLayerMaterialColour(layerIndex);
                                serializedObject.Update();
                            }
                            #endregion

                            #region Mesh and NavMesh
                            GUILayout.BeginHorizontal();

                            #region Add Mesh
                            if (GUILayout.Button(addMeshButtonContent, buttonCompact, GUILayout.MaxWidth(80f)))
                            {
                                // TODO - remove current nav meshes

                                // Create a temporary texture to store outline of the painted area in the stencil layer
                                Texture2D outlineTex = LBTextureOperations.CreateTexture(layerResolutionProp.intValue, layerResolutionProp.intValue, Color.clear, false, TextureFormat.ARGB32, true);

                                LBStencilLayer lbStencilLayer = lbStencil.stencilLayerList[layerIndex];

                                // Populate the texture with the outline
                                if (lbStencilLayer.OutlineTexture(outlineTex, Color.blue))
                                {
                                    // For fast culling, get the boundaries of the outlined area in within the stencil layer
                                    Vector4 bounds = LBTextureOperations.TextureOutlineBounds(outlineTex, Color.blue, true);

                                    // Build a list of mesh vert data for the outlined area
                                    List<LBMesh> lbMeshList = LBLandscapeOperations.GetMeshDataFromLandscape(lbStencil.landscapeCached, lbStencilLayer.LayerName, outlineTex, bounds, Color.blue);

                                    int numLBMesh = (lbMeshList == null ? 0 : lbMeshList.Count);

                                    if (numLBMesh > 0)
                                    {
                                        // Add the meshes to the scene
                                        Transform stencilLayerMeshTrfm = LBLandscapeOperations.CreateMeshList(lbStencil.landscapeCached, lbMeshList, lbStencil.transform, lbStencilLayer.LayerName + "_Meshes", true);
                                        if (stencilLayerMeshTrfm != null)
                                        {
                                            // Assign a material
                                            MeshRenderer[] meshRenderers = stencilLayerMeshTrfm.GetComponentsInChildren<MeshRenderer>();
                                            MeshRenderer meshRenderer = null;
                                            int numMeshRenderers = (meshRenderers == null ? 0 : meshRenderers.Length);
                                            if (numMeshRenderers > 0)
                                            {
                                                // Find and assign a clear material to each mesh
                                                Material matLBClear = LBEditorHelper.GetMaterialFromAssets(LBSetup.materialsFolder, "LBClear.mat");

                                                if (matLBClear == null) { Debug.LogWarning("LBStencil - could not find material for navmesh. Please Report"); }
                                                for (int mrIdx = 0; mrIdx < numMeshRenderers; mrIdx++)
                                                {
                                                    meshRenderer = meshRenderers[mrIdx];
                                                    if (meshRenderer != null)
                                                    {
                                                        // Turn off shadows etc
                                                        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                                                        meshRenderer.receiveShadows = false;
                                                        meshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                                                        meshRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

                                                        meshRenderer.sharedMaterial = matLBClear;
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    // TEST OUTPUT ONLY
                                    //string stencilFolderMapPath = LBMap.GetDefaultMapFolder + "/" + lbStencil.stencilName;

                                    //// Create the folders if they don't already exist in the project
                                    //LBEditorHelper.CheckFolder(LBMap.GetDefaultMapFolder);
                                    //LBEditorHelper.CheckFolder(stencilFolderMapPath);

                                    //string mapFilePath = stencilFolderMapPath + "/" + lbStencil.stencilLayerList[layerIndex].LayerName + "_" + (layerIndex).ToString("00") + ".png";

                                    //// TEMP - save output so we can see result
                                    //LBEditorHelper.SaveMapTexture(outlineTex, mapFilePath, outlineTex.width, false, false);
                                }

                                if (outlineTex != null) { LBTextureOperations.DestroyTexture2D(ref outlineTex); }
                            }
                            #endregion

                            #region Delete Mesh
                            if (GUILayout.Button(delMeshButtonContent, buttonCompact, GUILayout.MaxWidth(90f)))
                            {
                                // Find the parent mesh gameobject under the Stencil in the scene.
                                Transform meshParentTrfm = lbStencil.transform.Find(lbStencil.stencilLayerList[layerIndex].LayerName + "_Meshes");

                                // If it exists, delete it
                                if (meshParentTrfm != null)
                                {
                                    GameObject.DestroyImmediate(meshParentTrfm.gameObject);
                                }
                            }
                            #endregion

                            #region Bake Navmesh

                            if (UnityEditor.AI.NavMeshBuilder.isRunning)
                            {
                               if (GUILayout.Button(cancelBakeButtonContent, buttonCompact, GUILayout.MaxWidth(60f)))
                               {
                                    UnityEditor.AI.NavMeshBuilder.Cancel();
                               }
                            }
                            else if(GUILayout.Button(bakeNavMeshButtonContent, buttonCompact, GUILayout.MaxWidth(60f)))
                            {
                                UnityEditor.AI.NavMeshBuilder.BuildNavMeshAsync();
                            }

                            #endregion

                            #region Navmesh Settings
                            if (GUILayout.Button(openNavigationButtonContent, buttonCompact, GUILayout.MaxWidth(125f)))
                            {
                                #if UNITY_2018_2_OR_NEWER
                                LBEditorHelper.CallMenu("Window/AI/Navigation");
                                #else
                                LBEditorHelper.CallMenu("Window/Navigation");
                                #endif
                            }
                            #endregion

                            GUILayout.EndHorizontal();
                            #endregion
                        }
                    }

                    EditorGUILayout.EndVertical();

                    // Update visible status of Stencil Layer in the scene
                    if (showStencilInSceneProp.boolValue && showLayerInSceneToggled)
                    {
                        serializedObject.ApplyModifiedProperties();
                        ShowLayer(layerIndex);
                        serializedObject.Update();
                    }
                }
                #endregion

                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.EndVertical();
            }

            #region Insert or Remove a Layer
            // Perform the insert outside the loop to prevent errors
            if (insertLayerPos >= 0)
            {
                serializedObject.ApplyModifiedProperties();

                // Create a true clone with no reference to the original Stencil Layer.
                LBStencilLayer insertedLayer = new LBStencilLayer(lbStencil.stencilLayerList[insertLayerPos], true, false, false);
                if (insertedLayer != null)
                {
                    insertedLayer.GUID = System.Guid.NewGuid().ToString();
                    insertedLayer.LayerName += " (dup)";
                    // Show the new duplicate, and hide the original
                    insertedLayer.showLayerInEditor = true;
                    lbStencil.stencilLayerList[insertLayerPos].showLayerInEditor = false;
                    //Debug.Log("original: " + lbStencil.stencilLayerList[insertLayerPos].GUID + " dup: " + insertedLayer.GUID);
                    lbStencil.activeStencilLayer = null;

                    // Insert a duplicate above the selected layer
                    lbStencil.stencilLayerList.Insert(insertLayerPos, insertedLayer);
                    lbStencil.activeStencilLayer = insertedLayer;
                }

                serializedObject.Update();

                if (showStencilInSceneProp.boolValue) { refreshLayersInSceneRequired = true; }
            }

            // If required, attempt to remove the last layer
            if (removeLayerPos == stencilLayerListProp.arraySize - 1)
            {
                if (stencilLayerListProp.arraySize > 0) { stencilLayerListProp.arraySize -= 1; }
            }
            #endregion

            serializedObject.ApplyModifiedProperties();

            if (refreshLayersInSceneRequired) { RefreshLayers(); }


            lbStencil.allowRepaint = true;
        }

        #endregion

        #region Event Methods

        /// <summary>
        /// Called when the gameobject loses focus or Unity Editor enters/exits
        /// play mode
        /// </summary>
        void OnDestroy()
        {
            #if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui -= SceneGUI;
            #else
            SceneView.onSceneGUIDelegate -= SceneGUI;
            #endif

            EnableBrushPainter(false);
            lbStencil.landscapeCached = null;
        }

        /// <summary>
        /// Gets called automatically 10 times per second
        /// </summary>
        void OnInspectorUpdate()
        {
            // OnInspectorGUI() only registers events when the mouse is positioned over the custom editor window
            // This code forces OnInspectorGUI() to run every frame, so it registers events even when the mouse
            // is positioned over the scene view
            if (lbStencil.brushEnabled && lbStencil.allowRepaint) { Repaint(); }
        }

        /// <summary>
        /// Delegate which gets called every frame (??). Called more often than OnSceneGUI().
        /// NOTE: Local variables in custom editor act strangely and seem to not be accessible every
        /// alternate time it is called. To overcome, put variables in source script (e.g. LBStencil rather than LBStencilEditor).
        /// </summary>
        /// <param name="sv"></param>
        private void SceneGUI(SceneView sv)
        {
            //Debug.Log("SceneGUI EnableBrushPainter " + (lbStencil.brushEnabled ? "ON" : "OFF"));

            if (lbStencil == null || !lbStencil.showStencilInScene) { return; }

            currentEvent = Event.current;

            if (lbStencil.brushEnabled && lbStencil.brushPainter != null)
            {
                if (lbStencil.landscapeHeight > 0f)
                {
                    lbStencil.brushPainter.SetProjectorValues(lbStencil.brushSize, lbStencil.landscapeHeight);

                    // Check for left mouse button clicked or held down
                    if ((currentEvent.type == EventType.MouseDown || currentEvent.type == EventType.MouseDrag) && currentEvent.button == 0)
                    {
                        //Debug.Log("painting...");
                        PaintToLayer();
                    }
                }

                if (currentEvent.type == EventType.Layout)
                {
                    // Never let the mouse (or keyboard) get control. This will prevent the user from clicking
                    // on gameobjects (like the terrain) in the scene.
                    HandleUtility.AddDefaultControl(GUIUtility.GetControlID(GetHashCode(), FocusType.Passive));
                }
            }

            // Scene must have focus for this to have effect
            if (currentEvent.type == EventType.KeyDown)
            {
                if (currentEvent.keyCode == KeyCode.A) { MoveSceneCamera("LEFT", -2f); Repaint(); }
                else if (currentEvent.keyCode == KeyCode.D) { MoveSceneCamera("RIGHT", 2f); Repaint(); }
                else if (currentEvent.keyCode == KeyCode.S) { MoveSceneCamera("DOWN", -2f); Repaint(); }
                else if (currentEvent.keyCode == KeyCode.W) { MoveSceneCamera("UP", 2f); Repaint(); }
                else if (currentEvent.keyCode == KeyCode.LeftBracket) { ZoomIn(0.1f); Repaint(); }
                else if (currentEvent.keyCode == KeyCode.RightBracket) { ZoomIn(-0.1f); Repaint(); }
            }
            else if (currentEvent.type == EventType.KeyUp)
            {
                if (currentEvent.keyCode == KeyCode.Backslash) { Zoom(1f, true); Repaint(); }
            }

            // When user right-clicks on stencil or clicks the middle scroll mouse button, move this to the centre of the scene view.
            else if (currentEvent.type == EventType.MouseDown && (currentEvent.button == 1 || currentEvent.button == 2))
            {
                if (lbStencil.landscapeCached != null)
                {
                    // Get the on-ground terrain position from the mouse (this is in WorldSpace)
                    Vector3 newCameraPos = LBEditorHelper.GetLandscapePositionFromMouse(lbStencil.landscapeCached, currentEvent.mousePosition, false, true);
                    // Set the new camera height to be the same as existing scene view camera height
                    newCameraPos.y = sv.pivot.y;
                    // Update the scene view camera position.
                    sv.pivot = newCameraPos;

                    //Debug.Log("Stencil right click pos: " + newCameraPos);
                }
            }

            // Consume mouse events while showing stencil so that right mouse button has no effect (prevents rotation etc.)
            if (currentEvent.isMouse) { if (lbStencil.showStencilInScene) { currentEvent.Use(); } }
        }

        #endregion

        #region Private Brush Methods

        /// <summary>
        /// Enable or disable the current b
        /// </summary>
        /// <param name="isEnabled"></param>
        private void EnableBrushPainter(bool isEnabled)
        {
            //Debug.Log("Set EnableBrushPainter() " + (isEnabled ? "ON" : "OFF"));

            if (isEnabled)
            {
                Tools.hidden = true;
                // Check that height is populated.
                UpdateLandscapeTerrainHeight();
                lbStencil.brushPainter = LBStencilBrushPainter.CreateTerrainHighLighter();
            }
            else
            {
                Tools.hidden = false;
                if (lbStencil.brushPainter != null) { DestroyImmediate(lbStencil.brushPainter.gameObject); lbStencil.brushEnabled = false; }
            }
        }

        /// <summary>
        /// Take the screen mouse coordinates, convert to real world position.
        /// Then convert to landscape position.
        /// Update the the temporary array with values between 0 and 64K.
        /// </summary>
        private void PaintToLayer()
        {
            if (lbStencil.brushEnabled && lbStencil.brushPainter != null && lbStencil.activeStencilLayer != null)
            {
                Vector3 worldPos = lbStencil.brushPainter.GetRealWorldPosition();

                if (lbStencil.landscapeCached != null)
                {
                    if (lbStencil.landscapeCached.IsPointInLandscapeBoundsFast(worldPos))
                    {
                        Vector2 pointN = lbStencil.landscapeCached.NormalisePointFromWorldPos2DFast(new Vector2(worldPos.x, worldPos.z));
                        // Terrain goes from botton left to top right. Texture goes from top left to bottom right.
                        //pointN.x = 1f - pointN.x;
                        pointN.y = 1f - pointN.y;

                        if (lbStencil.activeStencilLayer.PaintLayerWithBrush(pointN, lbStencil.currentBrushType, lbStencil.brushSize))
                        {
                            // Update the rendertexture
                            if (lbStencil.activeStencilLayer.renderTexture != null)
                            {
                                // Draw brush into render texture
                                lbStencil.activeStencilLayer.PaintLayerRenderTextureWithBrush(pointN, lbStencil.currentBrushType, lbStencil.brushSize);
                            }
                        }
                        else { Debug.LogWarning("ERROR: lbStencil.PaintToLayer layerArray is null in " + lbStencil.activeStencilLayer.LayerName); }
                    }
                }
            }
        }

        #endregion

        #region Private Stencil Methods

        /// <summary>
        /// Show or Hide the Stencil top-down view
        /// </summary>
        /// <param name="isShown"></param>
        private void ShowStencil(bool isShown)
        {
            LBEditorHelper.ShowSceneView(this.GetType());

            try
            {
                // Remember the scene view camera settings
                SceneView sceneView = UnityEditor.SceneView.lastActiveSceneView;
                if (sceneView != null)
                {
                    Camera sceneViewCamera = sceneView.camera;

                    if (sceneViewCamera == null) { Debug.LogWarning("ERROR: ShowStencil - cannot find scene view camera."); }
                    else
                    {
                        UpdateLandscape();
                        if (lbStencil.landscapeCached == null) { Debug.LogWarning("WARNING: ShowStencil - cannot find parent landscape. Have you moved the Stencil?"); }
                        else if (isShown)
                        {
                            // Remember the scene view and camera settings
                            lbStencil.origSceneViewPivot = sceneView.pivot;
                            lbStencil.origSceneViewRotation = sceneView.rotation;
                            lbStencil.origSceneViewCameraPosition = sceneViewCamera.transform.position;
                            lbStencil.origSceneViewCameraRotation = sceneViewCamera.transform.rotation;
                            lbStencil.origSceneViewCameraLocalScale = sceneViewCamera.transform.localScale;
                            lbStencil.origSceneViewIsOrthographic = sceneView.orthographic;
                            lbStencil.origSceneViewSize = sceneView.size;
                            // Remember the scene view gizmo settings
                            lbStencil.origSceneViewShowSelectionOutline = LBEditorHelper.GizmoShowSelectionOutline;

                            // Is Unity Fog on?
                            lbStencil.isUnityFogOn = RenderSettings.fog;

                            // Save the Original Editor layout
                            LBEditorHelper.SaveEditorLayout("StencilLayout");

                            LBEditorHelper.CloseGameView(this.GetType());

                            // Always turn it off
                            RenderSettings.fog = false;

                            // Set up the top-down view
                            sceneView.orthographic = true;
                            sceneView.pivot = Vector3.zero;
                            sceneView.rotation = Quaternion.Euler(90f, 0f, 0f);
                            sceneView.size = 500f;

                            // Position in centre of landscape
                            sceneView.size = lbStencil.landscapeCached.size.x;

                            sceneView.pivot = GetDefaultSceneViewCameraCentrePosition();

                            // Lock the rotation
                            sceneView.isRotationLocked = true;

                            if (LBLandscape.IsHDRP(false))
                            {
                                lbStencil.origTerrainBaseMapDistance = lbStencil.landscapeCached.GetLandscapeTerrainBaseMapDist();
                                lbStencil.landscapeCached.SetLandscapeTerrainBaseMapDist(5000f);
                            }

                            // Disable the Selection Outline feature in the Scene view
                            LBEditorHelper.GizmoShowSelectionOutline = false;

                            lbStencil.landscapeCached.LockTerrains(true);

                            // Show or hide layers
                            RefreshLayers();
                        }
                        else // Hide Stencil
                        {
                            // Remove all the temporary stencil layer items from the scene
                            CleanupStencilLayers(lbStencil);

                            // Restore the original Editor Layout
                            // Must be done before restoring scene view settings
                            LBEditorHelper.LoadEditorLayout("StencilLayout");

                            // Get the new scene view and camera. Note: lastActiveSceneView is still NULL at this point
                            sceneView = (SceneView)UnityEditor.SceneView.sceneViews[UnityEditor.SceneView.sceneViews.Count-1];
                            sceneViewCamera = sceneView.camera;

                            // Restore Unity fog settings
                            RenderSettings.fog = lbStencil.isUnityFogOn;

                            // Restore original scene view camera settings
                            sceneView.pivot = lbStencil.origSceneViewPivot;
                            sceneView.rotation = lbStencil.origSceneViewRotation;
                            sceneView.orthographic = lbStencil.origSceneViewIsOrthographic;
                            sceneView.size = lbStencil.origSceneViewSize;
                            // Restore the original scene view camera location
                            sceneViewCamera.transform.position = lbStencil.origSceneViewCameraPosition;
                            sceneViewCamera.transform.rotation = lbStencil.origSceneViewCameraRotation;
                            sceneViewCamera.transform.localScale = lbStencil.origSceneViewCameraLocalScale;

                            // Unlock the rotation
                            sceneView.isRotationLocked = false;

                            if (LBLandscape.IsHDRP(false))
                            {
                                // Restore original baseMapDistance on each terrain
                                lbStencil.landscapeCached.SetLandscapeTerrainBaseMapDist(lbStencil.origTerrainBaseMapDistance);
                            }

                            lbStencil.landscapeCached.LockTerrains(false);

                            // Restore the original Gizmo settings
                            LBEditorHelper.GizmoShowSelectionOutline = lbStencil.origSceneViewShowSelectionOutline;
                        }

                        sceneView.Repaint();

                        // Automatically switch the LB Editor window
                        if (autoShowLBProp != null && !isShown)
                        {
                            if (autoShowLBProp.boolValue) { LBEditorHelper.SetFocusLBW(); }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("ERROR: ShowStencil - Something went wrong. Is Scene tab selected?\n" + ex.Message);
            }
        }

        /// <summary>
        /// Get the default centre point for the scene view camera
        /// </summary>
        /// <returns></returns>
        private Vector3 GetDefaultSceneViewCameraCentrePosition()
        {
            Vector3 newCameraPos = new Vector3(0f, 2000f, 0f);

            UpdateLandscapeTerrainHeight();

            newCameraPos.x = lbStencil.landscapeCached.start.x + (lbStencil.landscapeCached.size.x / 2f);
            newCameraPos.z = lbStencil.landscapeCached.start.z + (lbStencil.landscapeCached.size.y / 2f);
            newCameraPos.y = (lbStencil.landscapeCached.start.y + lbStencil.landscapeHeight + 100f);

            return newCameraPos;
        }

        /// <summary>
        /// Zoom the scene view in my x amount. -ve amounts zoom out.
        /// </summary>
        /// <param name="zoomIncrementAmount"></param>
        private void ZoomIn(float zoomIncrementAmount)
        {
            if (lbStencil != null)
            {
                if (lbStencil.showStencilInScene && lbStencil.landscapeCached != null)
                {
                    SceneView sceneView = UnityEditor.SceneView.lastActiveSceneView;

                    if (sceneView != null)
                    {
                        float newSceneViewSize = sceneView.size;
                        newSceneViewSize -= (sceneView.size * zoomIncrementAmount);

                        // Lock min/max zoom level
                        if (newSceneViewSize > lbStencil.landscapeCached.size.x) { sceneView.size = lbStencil.landscapeCached.size.x; }
                        //else if (newSceneViewSize < lbStencil.landscapeCached.size.x * 0.001f) { sceneView.size = lbStencil.landscapeCached.size.x * 0.001f; }
                        // zoom down to a 2m wide area
                        else if (newSceneViewSize < 2f) { sceneView.size = 2f; }
                        else { sceneView.size = newSceneViewSize; }
                    }
                }
            }
        }

        /// <summary>
        /// Zoom the scene view camera to the desired zoom amount (0-1).
        /// Optionally, centre the scene view camera on the landscape.
        /// </summary>
        /// <param name="zoomAmount"></param>
        /// <param name="isCentred"></param>
        private void Zoom(float zoomAmount, bool isCentred)
        {
            if (lbStencil != null)
            {
                if (lbStencil.showStencilInScene && lbStencil.landscapeCached != null)
                {
                    SceneView sceneView = UnityEditor.SceneView.lastActiveSceneView;

                    if (sceneView != null)
                    {
                        if (isCentred)
                        {
                            // Reset the top-down view
                            sceneView.orthographic = true;
                            sceneView.rotation = Quaternion.Euler(90f, 0f, 0f);

                            sceneView.pivot = GetDefaultSceneViewCameraCentrePosition();
                        }

                        sceneView.size = (lbStencil.landscapeCached.size.x * zoomAmount);
                    }
                }
            }
        }

        /// <summary>
        /// Move the scene view camera in a particular direction by a given amount
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="incrementAmount"></param>
        private void MoveSceneCamera(string direction, float incrementAmount)
        {
            if (lbStencil != null)
            {
                if (lbStencil.showStencilInScene && lbStencil.landscapeCached != null)
                {
                    SceneView sceneView = UnityEditor.SceneView.lastActiveSceneView;

                    if (sceneView != null)
                    {
                        Vector3 newCameraPos = sceneView.pivot;

                        if (direction == "LEFT" || direction == "RIGHT")
                        {
                            if (Event.current.shift) { newCameraPos.x += (incrementAmount * 10f); }
                            else { newCameraPos.x += incrementAmount; }
                        }
                        else
                        {
                            if (Event.current.shift) { newCameraPos.z += (incrementAmount * 10f); }
                            else { newCameraPos.z += incrementAmount; }
                        }

                        sceneView.pivot = newCameraPos;
                    }
                }
            }
        }

        #endregion

        #region Private Layer Methods
        /// <summary>
        /// Deallocate the Stencil Layer USHORT arrays
        /// Deallocate the Stencil Layer temporary render texture
        /// Remove all Stencil Layer planes from the scene
        /// </summary>
        /// <param name="lbStencil"></param>
        private void CleanupStencilLayers(LBStencil lbStencil)
        {
            if (lbStencil != null)
            {
                // Deallocate the temporary USHORT arrays
                // Deallocate renderTexture
                if (lbStencil.stencilLayerList != null)
                {
                    foreach (LBStencilLayer lbStencilLayer in lbStencil.stencilLayerList)
                    {
                        if (lbStencilLayer != null)
                        {
                            lbStencilLayer.DeallocLayerArray();
                            lbStencilLayer.renderTexture = null;
                        }
                    }
                }

                // No layers should be active
                lbStencil.activeStencilLayer = null;

                // Find any stencil layer planes in the scene that are children of the Stencil
                List<LBStencilLayerItem> itemList = new List<LBStencilLayerItem>(lbStencil.GetComponentsInChildren<LBStencilLayerItem>());
                if (itemList != null)
                {
                    // Loop backwards through the layer items and delete them from the scene
                    for (int item = itemList.Count - 1; item >= 0; item--)
                    {
                        if (itemList[item] != null)
                        {
                            DestroyImmediate(itemList[item].gameObject);
                        }
                    }
                    itemList.Clear();
                    itemList = null;
                }
            }
        }

        /// <summary>
        /// Add any new (visible) stencil layers to the scene view.
        /// Update layer gameobject positions in scene view.
        /// </summary>
        private void RefreshLayers()
        {
            EditorUtility.DisplayProgressBar("Refreshing...", "Please Wait", 0.2f);
            UpdateLandscape();
            if (lbStencil.landscapeCached != null)
            {
                if (lbStencil.stencilLayerList == null) { Debug.LogWarning("ERROR: LBStencil.RefreshLayers - layer list is null. Please Report"); }
                else
                {
                    UpdateLandscapeTerrainHeight();

                    LBStencilLayer lbStencilLayer;
                    LBStencilLayerItem lbStencilLayerItem;

                    // Show or hide layers
                    for (int l = 0; l < lbStencil.stencilLayerList.Count; l++)
                    {
                        lbStencilLayer = lbStencil.stencilLayerList[l];

                        if (lbStencilLayer != null)
                        {
                            ShowLayer(lbStencil.landscapeCached, lbStencilLayer, l, lbStencil.landscapeHeight);

                            // Update the position
                            lbStencilLayerItem = GetLayerItem(lbStencilLayer.GUID);
                            if (lbStencilLayerItem != null)
                            {
                                // Centre each layer and position 100m above the top for the max. height of landscape + 2m for each layer
                                lbStencilLayerItem.transform.position = new Vector3(lbStencil.landscapeCached.start.x + (lbStencil.landscapeCached.size.x / 2f), lbStencil.landscapeCached.start.y + lbStencil.landscapeHeight + 100f + (l * 2f), lbStencil.landscapeCached.start.z + (lbStencil.landscapeCached.size.y / 2f));
                                // Each plane is 10m x 10m.
                                lbStencilLayerItem.transform.localScale = new Vector3(lbStencil.landscapeCached.size.x / 10f, 1f, lbStencil.landscapeCached.size.y / 10f);

                                //Debug.Log("INFO: LBStencil.RefreshLayers " + lbStencilLayer.LayerName + " pos.y: " + lbStencilLayerItem.transform.position.y);
                            }
                        }
                    }

                    // Remove any stale/old layers
                    List<LBStencilLayerItem> itemList = new List<LBStencilLayerItem>(lbStencil.GetComponentsInChildren<LBStencilLayerItem>());
                    if (itemList != null)
                    {
                        // Loop backward through the layer items in the scene for the current stencil
                        for (int i = itemList.Count - 1; i >= 0; i--)
                        {
                            // Is this layer item in the list of valid stencil layers? If not, remove it from the scene
                            if (!lbStencil.stencilLayerList.Exists(layer => layer.GUID == itemList[i].GUID))
                            {
                                DestroyImmediate(itemList[i].gameObject);
                            }
                        }
                    }
                }
            }
            EditorUtility.ClearProgressBar();
        }

        /// <summary>
        /// Save all enabled Layers to Map Texture files in the LandscapeBuilder/Maps folder
        /// </summary>
        private void SaveLayersToMaps()
        {
            if (lbStencil == null) { Debug.LogWarning("ERROR: LBStencil.SaveLayersToMaps - lbStencil is null. Please Report"); }
            else
            {
                LBStencilLayer lbStencilLayer;

                string stencilFolderMapPath = LBMap.GetDefaultMapFolder + "/" + lbStencil.stencilName;

                // Create the folders if they don't already exist in the project
                LBEditorHelper.CheckFolder(LBMap.GetDefaultMapFolder);
                LBEditorHelper.CheckFolder(stencilFolderMapPath);

                float _progress = 0f;

                // Set up things for saving to multiple Map textures per terrain
                Rect landscapeBounds = new Rect();
                Terrain[] terrains = null;
                int numTerrains = 0;

                if (lbStencil.isCreateMapPerTerrain && lbStencil.landscapeCached != null && lbStencil.landscapeCached.landscapeTerrains != null)
                {
                    terrains = lbStencil.landscapeCached.landscapeTerrains;
                    numTerrains = (terrains == null ? 0 : terrains.Length);
                    landscapeBounds = lbStencil.landscapeCached.GetLandscapeWorldBoundsFast();
                }

                for (int l = 0; l < lbStencil.stencilLayerList.Count; l++)
                {
                    _progress = l / (float)lbStencil.stencilLayerList.Count;
                    if (_progress < 0.1f) { _progress = 0.1f; }

                    EditorUtility.DisplayProgressBar("Saving Stencil Layer Maps", "Please Wait", _progress);
                    lbStencilLayer = lbStencil.stencilLayerList[l];

                    // Is the layer enabled (visible in the scene)
                    if (lbStencilLayer.showLayerInScene)
                    {
                        Texture2D texture = LBTextureOperations.CreateTexture((int)lbStencilLayer.layerResolution, (int)lbStencilLayer.layerResolution, Color.clear, false, TextureFormat.ARGB32, false);

                        if (texture == null)
                        {
                            Debug.LogWarning("ERROR: LBStencil.SaveLayersToMaps - could not create new texture for layer " + lbStencilLayer.LayerName);
                        }
                        else if (lbStencilLayer.CopyUShortToTexture(texture, false, true))
                        {
                            bool continueToSave = false;
                            string mapFilePath;

                            // Override default behaviour, and create a texture for each terrain?
                            if (lbStencil.isCreateMapPerTerrain)
                            {
                                if (lbStencil.landscapeCached != null && terrains != null)
                                {
                                    // Check if there is an existing texture with the same name (just check for the first one)
                                    if (numTerrains > 0)
                                    {
                                        mapFilePath = stencilFolderMapPath + "/" + lbStencilLayer.LayerName + "_0000" + ".png";

                                        // Check if there is an existing texture with the same name
                                        string mapFullPath = Application.dataPath + mapFilePath.Substring(6);
                                        EditorUtility.ClearProgressBar();
                                        if (System.IO.File.Exists(mapFullPath))
                                        {

                                            continueToSave = EditorUtility.DisplayDialog("Maps Already Exist", "Are you sure you want to save the Maps? The existing" +
                                                                    " Maps will be lost.\n\n" + lbStencilLayer.LayerName, "Overwrite", "Cancel");
                                        }
                                        else { continueToSave = true; }
                                    }

                                    if (continueToSave)
                                    {
                                        Texture2D terrainTexture = null;
                                        int numTerrainsWide = (int)Mathf.Sqrt(numTerrains);
                                        float minXCoord = 0f, minYCoord = 0f;
                                        Vector3 terrainPosition;

                                        for (int tIdx = 0; tIdx < numTerrains; tIdx++)
                                        {
                                            terrainPosition = terrains[tIdx].transform.position;

                                            minXCoord = Mathf.InverseLerp(landscapeBounds.xMin, landscapeBounds.xMax, terrainPosition.x);
                                            minYCoord = Mathf.InverseLerp(landscapeBounds.yMin, landscapeBounds.yMax, terrainPosition.z);

                                            mapFilePath = stencilFolderMapPath + "/" + lbStencilLayer.LayerName + "_" + (tIdx).ToString("0000") + ".png";

                                            terrainTexture = LBTextureOperations.GenerateTex2DFromPartOfTex2D(texture, numTerrainsWide, minXCoord, minYCoord);

                                            if (terrainTexture != null)
                                            {
                                                EditorUtility.DisplayProgressBar("Saving Map", "Please Wait", tIdx + 1f / numTerrains);
                                                // Save in RGBA32 format for Vegetation Studio
                                                LBEditorHelper.SaveMapTexture(terrainTexture, mapFilePath, terrainTexture.width, (tIdx == numTerrains - 1), lbStencil.isCreateMapAsRGBA);
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // Get the path for the proposed texture map file
                                mapFilePath = stencilFolderMapPath + "/" + lbStencilLayer.LayerName + ".png";

                                // Check if there is an existing texture with the same name

                                string mapFullPath = Application.dataPath + mapFilePath.Substring(6);

                                if (System.IO.File.Exists(mapFullPath))
                                {
                                    EditorUtility.ClearProgressBar();
                                    continueToSave = EditorUtility.DisplayDialog("Map Already Exists", "Are you sure you want to save the Map? The existing" +
                                                            " Map will be lost.\n\n" + lbStencilLayer.LayerName + ".png", "Overwrite", "Cancel");
                                }
                                else { continueToSave = true; }

                                if (continueToSave)
                                {
                                    EditorUtility.DisplayProgressBar("Saving Stencil Layer Maps", "Please Wait", _progress);
                                    LBEditorHelper.SaveMapTexture(texture, mapFilePath, texture.width, false, lbStencil.isCreateMapAsRGBA);
                                    LBEditorHelper.HighlightItemInProjectWindow(mapFilePath, false);
                                }
                            }
                        }
                        else
                        {
                            Debug.LogWarning("ERROR: LBStencil.SaveLayersToMaps - could not copy the layer data to the map texture for layer " + lbStencilLayer.LayerName);
                        }
                    }
                }
                EditorUtility.ClearProgressBar();
            }
        }

        /// <summary>
        /// Smooth each enabled Stencil Layer
        /// </summary>
        /// <param name="smoothStrength"></param>
        private void SmoothStencilLayer(float smoothStrength)
        {
            if (lbStencil == null) { Debug.LogWarning("ERROR: LBStencil.SmoothStencilLayers - lbStencil is null. Please Report"); }
            else if (lbStencil.activeStencilLayer == null) { Debug.LogWarning("ERROR: LBStencil.SmoothStencilLayers - layer is not active. Please Report"); }
            else
            {
                EditorUtility.DisplayProgressBar("Smoothing stencil layer...", "Please Wait", 0.2f);

                // Is the layer enabled (visible in the scene)
                if (lbStencil.activeStencilLayer.showLayerInScene)
                {
                    // Update the USHORT array, then copy to render Texture
                    if (lbStencil.activeStencilLayer.GaussianBlurLayer(7, smoothStrength))
                    {
                        // Do a second smoothing pass with lower quality and less smoothing just to reduce
                        // the stripping effect of smoothing.
                        EditorUtility.DisplayProgressBar("Smoothing stencil layer...", "Please Wait", 0.5f);
                        lbStencil.activeStencilLayer.GaussianBlurLayer(3, 0.01f);

                        EditorUtility.DisplayProgressBar("Smoothing stencil layer...", "Please Wait", 0.7f);
                        lbStencil.activeStencilLayer.CopyUShortToRenderTexture();
                    }
                }

                EditorUtility.ClearProgressBar();
            }
        }

        /// <summary>
        /// Find the LBStencilLayerItem in the scene given a LBStencilLayer.GUID
        /// usage: LBStencilLayerItem lbStencilLayerItem = GetLayerItem(lbStencilLayer.GUID);
        /// </summary>
        /// <param name="GUID"></param>
        /// <returns></returns>
        private LBStencilLayerItem GetLayerItem(string GUID)
        {
            LBStencilLayerItem lbStencilLayerItem = null;

            // Check if plane is already in scene
            List<LBStencilLayerItem> itemList = new List<LBStencilLayerItem>(lbStencil.GetComponentsInChildren<LBStencilLayerItem>());
            if (itemList != null)
            {
                lbStencilLayerItem = itemList.Find(item => item.GUID == GUID);
            }
            return lbStencilLayerItem;
        }

        /// <summary>
        /// Given the position in the list of LBStencilLayers, show or hide a
        /// temporary layer plane in the scene.
        /// </summary>
        /// <param name="layerIndex"></param>
        private void ShowLayer(int layerIndex)
        {
            UpdateLandscape();

            if (lbStencil.stencilLayerList.Count <= layerIndex) { Debug.LogWarning("ERROR: Stencil ShowLayer - layer index is out of range. Please report."); }
            else if (lbStencil.landscapeCached != null)
            {
                LBStencilLayer lbStencilLayer = lbStencil.stencilLayerList[layerIndex];
                if (lbStencilLayer == null) { Debug.LogWarning("ERROR: Stencil ShowLayer - Stencil Layer is null. Please report."); }
                else
                {
                    UpdateLandscapeTerrainHeight();
                    ShowLayer(lbStencil.landscapeCached, lbStencilLayer, layerIndex, lbStencil.landscapeHeight);
                }
            }
        }

        /// <summary>
        /// Show or Hide a Stencil Layer in the scene view
        /// Only create new (temporary) layer planes in the scene when they are visible. This will potentially
        /// make enabling and disabling Stencils faster.
        /// When shown, also updates the Layer Pixel Size
        /// </summary>
        /// <param name="lbStencilLayer"></param>
        /// <param name="isShown"></param>
        private void ShowLayer(LBLandscape landscape, LBStencilLayer lbStencilLayer, int layerIndex, float landscapeHeight)
        {
            if (landscape != null && lbStencil != null)
            {
                // Check if plane is already in scene
                LBStencilLayerItem lbStencilLayerItem = GetLayerItem(lbStencilLayer.GUID);

                if (lbStencilLayer.showLayerInScene)
                {
                    if (lbStencilLayerItem == null)
                    {
                        // Can't find a matching stencil layer, so create a new (temporary) layer plane in the scene
                        GameObject newPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
                        if (newPlane == null) { Debug.LogWarning("ERROR: Stencil ShowLayer - could not create layer primitive for " + lbStencilLayer.LayerName); }
                        else
                        {
                            lbStencilLayerItem = newPlane.AddComponent<LBStencilLayerItem>();

                            newPlane.name = lbStencilLayer.LayerName;
                            if (lbStencilLayerItem == null) { Debug.LogWarning("ERROR: Stencil ShowLayer - could not add layer item to " + lbStencilLayer.LayerName); }
                            else
                            {
                                // Link the temporary gameobject in then scene with the Stencil Layer
                                lbStencilLayerItem.GUID = lbStencilLayer.GUID;
                            }

                            newPlane.transform.SetParent(lbStencil.transform);
                            // Centre each layer and position 100m above the top for the max. height of landscape + 2m for each layer
                            newPlane.transform.position = new Vector3(landscape.start.x + (landscape.size.x / 2f), landscape.start.y + landscapeHeight + 100f + (layerIndex * 2f), landscape.start.z + (landscape.size.y / 2f));
                            // Each plane is 10m x 10m.
                            newPlane.transform.localScale = new Vector3(landscape.size.x / 10f, 1f, landscape.size.y / 10f);

                            // Remove the default collider
                            MeshCollider collider = newPlane.GetComponent<MeshCollider>();
                            if (collider != null) { DestroyImmediate(collider); }

                            MeshRenderer meshRenderer = newPlane.GetComponent<MeshRenderer>();
                            if (meshRenderer == null) { Debug.LogWarning("ERROR: Stencil ShowLayer - could not get mesh renderer for " + lbStencilLayer.LayerName); }
                            else
                            {
                                // Turn off shadows and probes
                                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                                meshRenderer.receiveShadows = false;
                                meshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                                meshRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

                                // Use the LBStencilLayer shader
                                string shaderPath = "Assets/LandscapeBuilder/Shaders/LBStencilLayer.shader";
                                Shader lbStencilLayerShader = (Shader)AssetDatabase.LoadAssetAtPath(shaderPath, typeof(Shader));
                                if (lbStencilLayerShader == null) { Debug.LogWarning("ERROR: Stencil ShowLayer - could not find " + shaderPath + ". Did you move it?"); }
                                else
                                {
                                    Material mat = new Material(lbStencilLayerShader);
                                    if (mat == null) { Debug.LogWarning("ERROR: Stencil ShowLayer - could not create material for " + lbStencilLayer.LayerName); }
                                    else
                                    {
                                        lbStencilLayer.AllocLayerArray();
                                        lbStencilLayer.UnCompressToUShort();

                                        if (lbStencilLayer.ValidateRenderTexture((int)lbStencil.renderResolution, false))
                                        {
                                            mat.name = lbStencilLayer.LayerName + "_Mat";
                                            mat.SetColor("_MainColour", lbStencilLayer.colourInEditor);
                                            mat.SetTexture("_MainTex", lbStencilLayer.renderTexture);
                                            meshRenderer.sharedMaterial = mat;

                                            lbStencilLayer.CopyUShortToRenderTexture();
                                        }

                                        //Debug.Log("layer render texture: " + lbStencilLayer.renderTexture.name + " mat mainTexture: " + mat.mainTexture.name);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        // Found the (temporary) stencil layer plane in the scene
                        MeshRenderer meshRenderer = lbStencilLayerItem.GetComponent<MeshRenderer>();
                        if (meshRenderer != null) { meshRenderer.enabled = true; }

                        //Debug.Log("INFO: LBStencil.ShowLayer " + lbStencilLayer.LayerName + " LayerItem:" + lbStencilLayerItem.name);

                        // Check to see if the render resolution has changed. If so, recreate and populate the render texture
                        if (lbStencilLayer.ValidateRenderTexture((int)lbStencil.renderResolution, true))
                        {
                            // If validated, update the meshRenderer with the renderTexture.
                            meshRenderer.sharedMaterial.SetTexture("_MainTex", lbStencilLayer.renderTexture);
                            //Debug.Log("INFO: LBStencil.ShowLayer " + lbStencilLayer.LayerName + " LayerItem:" + lbStencilLayerItem.name + " rendertexture:" + lbStencilLayer.renderTexture.name);
                        }
                    }

                    lbStencilLayer.UpdatePixelSize(landscape.size.x);
                }
                else
                {
                    // (De)activate layer if it was the active layer and has now been hidden in the scene
                    if (lbStencil.activeStencilLayer == lbStencilLayer) { lbStencil.activeStencilLayer = null; }

                    // If plane is in scene, turn it off
                    if (lbStencilLayerItem != null)
                    {
                        MeshRenderer meshRenderer = lbStencilLayerItem.GetComponent<MeshRenderer>();
                        if (meshRenderer != null) { meshRenderer.enabled = false; }
                    }
                }
            }
        }

        /// <summary>
        /// Update the material colour used to paint on the layer.
        /// Re-render the layer using the new colour.
        /// </summary>
        /// <param name="layerIndex"></param>
        private void UpdateLayerMaterialColour(int layerIndex)
        {
            // Ensure index is in range
            if (lbStencil != null && lbStencil.stencilLayerList != null)
            {
                if (lbStencil.stencilLayerList.Count > layerIndex)
                {
                    LBStencilLayer lbStencilLayer = lbStencil.stencilLayerList[layerIndex];

                    if (lbStencilLayer != null)
                    {
                        LBStencilLayerItem lbStencilLayerItem = GetLayerItem(lbStencilLayer.GUID);
                        if (lbStencilLayerItem != null)
                        {
                            MeshRenderer meshRenderer = lbStencilLayerItem.GetComponent<MeshRenderer>();
                            if (meshRenderer != null)
                            {
                                Material mat = meshRenderer.sharedMaterial;
                                if (mat != null)
                                {
                                    mat.SetColor("_MainColour", lbStencilLayer.colourInEditor);

                                    if (lbStencilLayer.showLayerInScene)
                                    {
                                        lbStencilLayer.CopyUShortToRenderTexture();
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Validate that the activelayer is in the stencil's layer list.
        /// If not, reset it to null.
        /// </summary>
        private void ValidateActiveLayer()
        {
            if (lbStencil != null && lbStencil.activeStencilLayer != null && lbStencil.stencilLayerList != null)
            {
                // If the activeLayer no longer exists in the list, set it to null
                if (!lbStencil.stencilLayerList.Exists(l => l == lbStencil.activeStencilLayer))
                {
                    // Debug.LogWarning("ERROR: Stencil ShowLayer - could not find active layer " + lbStencil.activeStencilLayer.LayerName + " in " + lbStencil.name);
                    lbStencil.activeStencilLayer = null;
                }
            }
        }

        /// <summary>
        /// If there is no active layers, set the first layer shown in the scene to be the active layer.
        /// </summary>
        private void SetDefaultActiveLayer()
        {
            if (lbStencil != null && lbStencil.activeStencilLayer == null && lbStencil.stencilLayerList != null)
            {
                // find the first layer shown in the scene, and set it to the active layer
                lbStencil.activeStencilLayer = lbStencil.stencilLayerList.Find(l => l.showLayerInScene == true);
            }
        }

        #endregion

        #region Private Landscape Methods
        /// <summary>
        /// Get the landscape script associated with this stencil
        /// For faster access, call UpdateLandscape() to check that the landscape
        /// script reference has been cached, then use landscapeCached variable.
        /// </summary>
        /// <returns></returns>
        private LBLandscape GetLandscape()
        {
            LBLandscape lbLandscape = null;

            Transform trfmParent = lbStencil.transform.parent;

            if (trfmParent == null) { Debug.LogWarning("WARNING: LBStencilInspector.GetLandscape - cannot find parent landscape gameobject. Have you moved the Stencil?"); }
            else
            {
                lbLandscape = trfmParent.GetComponent<LBLandscape>();
                if (lbLandscape == null) { Debug.LogWarning("WARNING: LBStencilInspector.GetLandscape - cannot find parent landscape. Have you moved the Stencil?"); }
            }

            return lbLandscape;
        }

        /// <summary>
        /// Updates the landscape variable for faster
        /// cached access to the script within the inspector.
        /// </summary>
        private void UpdateLandscape(bool forcedUpdate = false)
        {
            if (forcedUpdate || lbStencil.landscapeCached == null)
            {
                lbStencil.landscapeCached = GetLandscape();
                if (lbStencil.landscapeCached != null)
                {
                    lbStencil.landscapeCached.SetLandscapeTerrains(forcedUpdate);
                    //landscapeWorldBounds = landscapeCached.GetLandscapeWorldBoundsFast();
                }
            }
        }

        /// <summary>
        /// If the height variable hasn't been set, update it now
        /// </summary>
        private void UpdateLandscapeTerrainHeight()
        {
            if (lbStencil.landscapeHeight < 0f)
            {
                UpdateLandscape(false);
                if (lbStencil.landscapeCached != null) { lbStencil.landscapeHeight = lbStencil.landscapeCached.GetLandscapeTerrainHeight(); }
            }
        }
        #endregion
    }

#endif
    #endregion
}