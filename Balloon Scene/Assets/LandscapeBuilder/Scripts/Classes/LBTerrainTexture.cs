using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;  // Includes Float.Sum() and List.Count(..)

namespace LandscapeBuilder
{
    [System.Serializable]
    public class LBTerrainTexture
    {
        // Terrain texture class

        #region Enumerations
        public enum TexturingMode
        {
            Height = 0,
            Inclination = 1,
            HeightAndInclination = 2,
            ConstantInfluence = 3,
            Map = 4,
            HeightInclinationMap = 5,
            HeightInclinationCurvature = 6,
            Imported = 20
        }
        #endregion

        #region Variables and Properties

        public Texture2D texture;
        public Texture2D normalMap;

        // Added v1.3.2 Beta 1c
        // A Texture Heightmap can also use an Occlusion Map texture if the first is not available
        public Texture2D heightMap;

        // Added v1.4.2 Beta 5b
        // Last known texture name. Used to help detect missing textures.
        public string textureName;
        public string normalMapName;

        public Vector2 tileSize;
        public float metallic;
        public float smoothness;

        public float minHeight;
        public float maxHeight;
        public float minInclination;
        public float maxInclination;
        public float strength;

        // Added v2.0.7 Beta 6
        public bool isCurvatureConcave = false;
        [Range(0.01f, 20f)] public float curvatureMinHeightDiff;
        [Range(0.1f, 50f)] public float curvatureDistance;

        // Added v1.1 Beta 4-9
        public Texture2D map;
        public Color mapColour;
        public bool isDisabled;
        public int mapTolerance;
        public bool useNoise;
        public float noiseTileSize;
        public float noiseOffset;

        // Added v1.2 Beta 3
        public bool isMinimalBlendingEnabled;

        // Added v1.2 Beta 12
        public bool mapInverse;

        // Added v1.3.1 Beta 8f
        public bool useAdvancedMapTolerance;
        public int mapToleranceRed;
        public int mapToleranceGreen;
        public int mapToleranceBlue;
        public int mapToleranceAlpha;
        public float mapWeightRed;
        public float mapWeightGreen;
        public float mapWeightBlue;
        public float mapWeightAlpha;
        public float mapToleranceBlendRate;
        public AnimationCurve mapToleranceBlendCurve;
        public LBCurve.BlendCurvePreset mapToleranceBlendCurvePreset;

        // Added 1.3.2 Beta 5h
        // Was this map created from a LBPath? Is so, special blending will apply
        public bool mapIsPath;

        // Added 1.3.2 Beta 2e
        public bool isTinted;
        public Color tintColour;
        [Range(0.1f, 1f)] public float tintStrength;
        public Texture2D tintedTexture;

        // Added 1.3.2 Beta 7c
        public bool isRotated;
        [Range(-360f, 360f)] public float rotationAngle;
        public Texture2D rotatedTexture;

        public TexturingMode texturingMode;

        // Added with v1.3.0 Beta 3a
        public bool showTexture;

        // NOTE: We must use a LBTextureFilter rather than a LBFilter
        // for LBTerrainTexture class.
        public List<LBTextureFilter> filterList;

        // Added 1.4.2 Beta 4a - stores texture splatmap data from an imported terrain
        // There is a LBTerrainData instance for each terrain in the landscape
        public List<LBTerrainData> lbTerrainDataList;

        // Added v2.0.0
        public string GUID;

        #endregion

        #region Constructors

        // Class constructor
        public LBTerrainTexture()
        {
            this.texture = null;
            this.normalMap = null;
            this.heightMap = null;
            this.tileSize = Vector2.one * 25f;
            this.smoothness = 0f;
            this.metallic = 0f;
            this.minHeight = 0.25f;
            this.maxHeight = 0.75f;
            this.minInclination = 0f;
            this.maxInclination = 30f;
            this.strength = 1f;
            // v2.0.6 Beta 5a changed default from Height to ConstantInfluence
            this.texturingMode = TexturingMode.ConstantInfluence;

            // Added v2.0.7 Beta 6
            this.isCurvatureConcave = false;
            this.curvatureMinHeightDiff = 1f;
            this.curvatureDistance = 5f;

            // Added v1.4.2 Beta 5b
            this.textureName = string.Empty;
            this.normalMapName = string.Empty;

            // Added v1.1 Beta 4-9
            this.map = null;
            this.mapColour = UnityEngine.Color.red;
            this.isDisabled = false;
            this.mapTolerance = 1;
            this.useNoise = true;
            this.noiseTileSize = 100f;

            // Added v1.2 Beta 3
            this.isMinimalBlendingEnabled = false;

            // Added v1.2 Beta 12
            this.mapInverse = false;

            // Added v1.3.1 Beta 8f
            this.useAdvancedMapTolerance = false;
            this.mapToleranceRed = 0;
            this.mapToleranceGreen = 0;
            this.mapToleranceBlue = 0;
            this.mapToleranceAlpha = 0;
            this.mapWeightRed = 1f;
            this.mapWeightGreen = 1f;
            this.mapWeightBlue = 1f;
            this.mapWeightAlpha = 1f;
            this.mapToleranceBlendCurvePreset = (LBCurve.BlendCurvePreset)LBCurve.CurvePreset.Cubed;
            this.mapToleranceBlendCurve = LBCurve.SetCurveFromPreset((LBCurve.CurvePreset)this.mapToleranceBlendCurvePreset);

            // Added 1.3.2 Beta 5h
            this.mapIsPath = false;

            // Added 1.3.2 Beta 2e
            this.isTinted = false;
            this.tintColour = UnityEngine.Color.clear;
            this.tintStrength = 0.5f;
            this.tintedTexture = null;

            // Added 1.3.2 Beta 7c
            this.isRotated = false;
            this.rotationAngle = 0f;
            this.rotatedTexture = null;

            // Added v1.3.0 Beta 3a
            this.showTexture = true;

            // Added v1.4.2 Beta 4a
            this.lbTerrainDataList = null;

            // Noise offset cannot be modified from the editor as it is randomly set when the terrain is textured

            // Added v2.0.0
            // Assign a unique identifier
            GUID = System.Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Clone Constructor
        /// </summary>
        /// <param name="lbTerrainTexture"></param>
        public LBTerrainTexture(LBTerrainTexture lbTerrainTexture)
        {
            this.texture = lbTerrainTexture.texture;
            this.normalMap = lbTerrainTexture.normalMap;
            this.heightMap = lbTerrainTexture.heightMap;
            if (lbTerrainTexture.textureName == null) { this.textureName = string.Empty; }
            else { this.textureName = lbTerrainTexture.textureName; }
            if (lbTerrainTexture.normalMapName == null) { this.normalMapName = string.Empty; }
            else { this.normalMapName = lbTerrainTexture.normalMapName; }
            this.tileSize = lbTerrainTexture.tileSize;
            this.smoothness = lbTerrainTexture.smoothness;
            this.metallic = lbTerrainTexture.metallic;
            this.minHeight = lbTerrainTexture.minHeight;
            this.maxHeight = lbTerrainTexture.maxHeight;
            this.minInclination = lbTerrainTexture.minInclination;
            this.maxInclination = lbTerrainTexture.maxInclination;
            this.strength = lbTerrainTexture.strength;
            this.texturingMode = lbTerrainTexture.texturingMode;
            this.isCurvatureConcave = lbTerrainTexture.isCurvatureConcave;
            this.curvatureDistance = lbTerrainTexture.curvatureDistance;
            this.curvatureMinHeightDiff = lbTerrainTexture.curvatureMinHeightDiff;
            this.map = lbTerrainTexture.map;
            this.mapColour = lbTerrainTexture.mapColour;
            this.isDisabled = lbTerrainTexture.isDisabled;
            this.mapTolerance = lbTerrainTexture.mapTolerance;
            this.useNoise = lbTerrainTexture.useNoise;
            this.noiseTileSize = lbTerrainTexture.noiseTileSize;
            this.isMinimalBlendingEnabled = lbTerrainTexture.isMinimalBlendingEnabled;
            this.mapInverse = lbTerrainTexture.mapInverse;
            this.useAdvancedMapTolerance = lbTerrainTexture.useAdvancedMapTolerance;
            this.mapToleranceRed = lbTerrainTexture.mapToleranceRed;
            this.mapToleranceGreen = lbTerrainTexture.mapToleranceGreen;
            this.mapToleranceBlue = lbTerrainTexture.mapToleranceBlue;
            this.mapToleranceAlpha = lbTerrainTexture.mapToleranceAlpha;
            this.mapWeightRed = lbTerrainTexture.mapWeightRed;
            this.mapWeightGreen = lbTerrainTexture.mapWeightGreen;
            this.mapWeightBlue = lbTerrainTexture.mapWeightBlue;
            this.mapWeightAlpha = lbTerrainTexture.mapWeightAlpha;
            this.mapToleranceBlendCurvePreset = lbTerrainTexture.mapToleranceBlendCurvePreset;
            this.mapToleranceBlendCurve = lbTerrainTexture.mapToleranceBlendCurve;
            this.mapIsPath = lbTerrainTexture.mapIsPath;
            this.isTinted = lbTerrainTexture.isTinted;
            this.tintColour = lbTerrainTexture.tintColour;
            this.tintStrength = lbTerrainTexture.tintStrength;
            this.tintedTexture = lbTerrainTexture.tintedTexture;
            this.isRotated = lbTerrainTexture.isRotated;
            this.rotationAngle = lbTerrainTexture.rotationAngle;
            this.rotatedTexture = lbTerrainTexture.rotatedTexture;
            this.showTexture = lbTerrainTexture.showTexture;
            this.noiseOffset = lbTerrainTexture.noiseOffset;
            if (lbTerrainTexture.filterList != null) { this.filterList = new List<LBTextureFilter>(lbTerrainTexture.filterList); }
            else { this.filterList = new List<LBTextureFilter>(); }

            if (lbTerrainTexture.lbTerrainDataList == null) { this.lbTerrainDataList = null; }
            else { this.lbTerrainDataList = new List<LBTerrainData>(lbTerrainTexture.lbTerrainDataList); }

            this.GUID = lbTerrainTexture.GUID;
        }

        #endregion

        #region Public Non-Static Methods

        /// <summary>
        /// Get the texture for this LBTerrainTexture instance. Takes into consideration
        /// if isTinted and/or isRotate enabled.
        /// </summary>
        /// <returns></returns>
        public Texture2D GetTexture2D()
        {
            Texture2D tempTexture = null;

            // If the Texture is tinted, use the tinted texture rather than the main texture
            if (isTinted)
            {
                // If it is Tinted AND rotated, rotate the tinted texture
                if (isRotated)
                {
                    tempTexture = LBTextureOperations.RotateTexture(tintedTexture, rotationAngle);
                }
                else { tempTexture = tintedTexture; }
            }
            else if (isRotated) { tempTexture = rotatedTexture; }
            else { tempTexture = texture; }

            return tempTexture;
        }

        /// <summary>
        /// Compare the current texture with another taking into consideration isTinted and isRotate.
        /// WARNING: If this LBTerrainTexture is tinted AND rotated, the comparision will generate Garbage
        /// and affect GC.
        /// </summary>
        /// <param name="compareTo"></param>
        /// <returns></returns>
        public bool CompareToTexture2D(Texture2D compareTo)
        {
            Texture2D tempTexture = GetTexture2D();

            if (compareTo == null) { return tempTexture == null; }
            else if (tempTexture == null) { return false; }
            else if (tempTexture == compareTo) { return true; }
            // If not same textures and haven't been rotated AND tinted, return false
            else if (!(isRotated && isTinted)) { return false; }
            else
            {
                // If tinted AND rotated it still could be the same texture
                return (compareTo.width == tempTexture.width && compareTo.height == tempTexture.height && compareTo.name == tempTexture.name);
            }
        }

        /// <summary>
        /// Script out the Texture for use in a runtime script.
        /// TextureIdx is the zero-based position in the terrainTexturesList
        /// </summary>
        /// <param name="TextureIdx"></param>
        /// <param name="EndOfLineMarker"></param>
        /// <returns></returns>
        public string ScriptTexture(int TextureIdx, string EndOfLineMarker = "\n")
        {
            // Create a new instance of StringBuilder and give it an estimated capacity
            System.Text.StringBuilder sb = new System.Text.StringBuilder(1000);

            string eol = " ";

            // We always need a space between lines OR a end of line marker like "\n"
            if (EndOfLineMarker.Length > 0) { eol = EndOfLineMarker; }

            string texInst = "lbTerrainTexture" + (TextureIdx + 1);
            string texInstAbrev = "Tex" + (TextureIdx + 1);

            sb.Append("// Texture Code generated from Landscape Builder 2 at " + System.DateTime.Now.ToShortTimeString() + " on " + System.DateTime.Now.ToLongDateString() + eol + eol);

            sb.Append("// BEGIN Public variables to populate in the editor - Uncomment and add these to top of class " + eol);
            sb.Append("//[Header(\"Texture" + (TextureIdx + 1) + "\")] " + eol);
            sb.Append("//public Texture2D texture" + texInstAbrev + "; " + eol);
            sb.Append("//public Texture2D normalMap" + texInstAbrev + "; " + eol);
            sb.Append("//public Texture2D heightMap" + texInstAbrev + "; " + eol);
            sb.Append("//public Texture2D map" + texInstAbrev + "; " + eol);
            sb.Append("// END Public variables" + eol + eol);

            sb.Append("#region LBTerrainTexture" + (TextureIdx + 1) + eol);

            sb.Append(LBCurve.ScriptCurve(mapToleranceBlendCurve, "\n", "mapToleranceBlendCurve" + texInstAbrev));

            sb.Append("LBTerrainTexture " + texInst + " = new LBTerrainTexture(); " + eol);
            sb.Append("if (" + texInst + " != null)" + eol);
            sb.Append("{" + eol);
            sb.Append("\t" + texInst + ".texture = texture" + texInstAbrev + "; " + eol);
            sb.Append("\t" + texInst + ".normalMap = normalMap" + texInstAbrev + "; " + eol);
            sb.Append("\t" + texInst + ".heightMap = heightMap" + texInstAbrev + "; " + eol);
            sb.Append("\t" + texInst + ".textureName = " + (string.IsNullOrEmpty(textureName) ? "\"\"" : "\"" + textureName + "\"") + "; " + eol);
            sb.Append("\t" + texInst + ".normalMapName = " + (string.IsNullOrEmpty(normalMapName) ? "\"\"" : "\"" + normalMapName + "\"") + "; " + eol);
            sb.Append("\t" + texInst + ".tileSize = new Vector2(" + tileSize.x + ", " + tileSize.y + "); " + eol);
            sb.Append("\t" + texInst + ".smoothness = " + smoothness + "f; " + eol);
            sb.Append("\t" + texInst + ".metallic = " + metallic + "f; " + eol);
            sb.Append("\t" + texInst + ".minHeight = " + minHeight + "f; " + eol);
            sb.Append("\t" + texInst + ".maxHeight = " + maxHeight + "f; " + eol);
            sb.Append("\t" + texInst + ".minInclination = " + minInclination + "f; " + eol);
            sb.Append("\t" + texInst + ".maxInclination = " + maxInclination + "f; " + eol);
            sb.Append("\t" + texInst + ".strength = " + strength + "f; " + eol);
            sb.Append("\t" + texInst + ".texturingMode = LBTerrainTexture.TexturingMode." + texturingMode + "; " + eol);
            sb.Append("\t" + texInst + ".isCurvatureConcave = " + isCurvatureConcave.ToString().ToLower() + "; " + eol);
            sb.Append("\t" + texInst + ".curvatureDistance = " + curvatureDistance + "f; " + eol);
            sb.Append("\t" + texInst + ".curvatureMinHeightDiff = " + curvatureMinHeightDiff + "f; " + eol);
            sb.Append("\t" + texInst + ".map = map" + texInstAbrev + "; " + eol);
            sb.Append("\t" + texInst + ".mapColour = new Color(" + mapColour.r + "f," + mapColour.g + "f," + mapColour.b + "f," + mapColour.a + "f); " + eol);
            sb.Append("\t" + texInst + ".isDisabled = " + isDisabled.ToString().ToLower() + "; " + eol);
            sb.Append("\t" + texInst + ".mapTolerance = " + mapTolerance + "; " + eol);
            sb.Append("\t" + texInst + ".useNoise = " + useNoise.ToString().ToLower() + "; " + eol);
            sb.Append("\t" + texInst + ".noiseTileSize = " + noiseTileSize + "f; " + eol);
            sb.Append("\t" + texInst + ".isMinimalBlendingEnabled = " + isMinimalBlendingEnabled.ToString().ToLower() + "; " + eol);
            sb.Append("\t" + texInst + ".mapInverse = " + mapInverse.ToString().ToLower() + "; " + eol);
            sb.Append("\t" + texInst + ".useAdvancedMapTolerance = " + useAdvancedMapTolerance.ToString().ToLower() + "; " + eol);
            sb.Append("\t" + texInst + ".mapToleranceRed = " + mapToleranceRed + "; " + eol);
            sb.Append("\t" + texInst + ".mapToleranceGreen = " + mapToleranceGreen + "; " + eol);
            sb.Append("\t" + texInst + ".mapToleranceBlue = " + mapToleranceBlue + "; " + eol);
            sb.Append("\t" + texInst + ".mapToleranceAlpha = " + mapToleranceAlpha + "; " + eol);
            sb.Append("\t" + texInst + ".mapWeightRed = " + mapWeightRed + "f; " + eol);
            sb.Append("\t" + texInst + ".mapWeightGreen = " + mapWeightGreen + "f; " + eol);
            sb.Append("\t" + texInst + ".mapWeightBlue = " + mapWeightBlue + "f; " + eol);
            sb.Append("\t" + texInst + ".mapWeightAlpha = " + mapWeightAlpha + "f; " + eol);
            sb.Append("\t" + texInst + ".mapToleranceBlendCurvePreset = LBCurve.BlendCurvePreset." + mapToleranceBlendCurvePreset + "; " + eol);
            sb.Append("\t" + texInst + ".mapToleranceBlendCurve = mapToleranceBlendCurve" + texInstAbrev + "; " + eol);
            sb.Append("\t" + texInst + ".mapIsPath = " + mapIsPath.ToString().ToLower() + "; " + eol);
            sb.Append("\t" + texInst + ".isTinted = " + isTinted.ToString().ToLower() + "; " + eol);
            sb.Append("\t" + texInst + ".tintColour = new Color(" + tintColour.r + "f," + tintColour.g + "f," + tintColour.b + "f," + tintColour.a + "f); " + eol);
            sb.Append("\t" + texInst + ".tintStrength = " + tintStrength + "f; " + eol);
            if (isTinted)
            {
                sb.Append("\t" + texInst + ".tintedTexture = LBTextureOperations.TintTexture(texture" + texInstAbrev + ", new Color(" + tintColour.r + "f," + tintColour.g + "f," + tintColour.b + "f," + tintColour.a + "f)," + tintStrength + "f); " + eol);
            }
            else { sb.Append("\t" + texInst + ".tintedTexture = null; " + eol); }
            sb.Append("\t" + texInst + ".isRotated = " + isRotated.ToString().ToLower() + "; " + eol);
            sb.Append("\t" + texInst + ".rotationAngle = " + rotationAngle + "f; " + eol);
            sb.Append("\t" + texInst + ".rotatedTexture = null; " + eol);
            sb.Append("\t" + texInst + ".showTexture = " + showTexture.ToString().ToLower() + "; " + eol);
            sb.Append("\t" + texInst + ".noiseOffset = " + noiseOffset + "f; " + eol);
            sb.Append("\t" + texInst + ".GUID = " + (string.IsNullOrEmpty(GUID) ? "\"\"" : "\"" + GUID + "\"") + "; " + eol);

            sb.Append("\t" + texInst + ".filterList = new List<LBTextureFilter>(); " + eol);
            if (filterList != null)
            {
                // Create a unique variable
                if (filterList.Exists(f => f.filterType == LBTextureFilter.FilterType.StencilLayer))
                {
                    sb.Append("\tLBTextureFilter lbFilter" + texInstAbrev + " = null; " + eol);
                }

                for (int tf = 0; tf < filterList.Count; tf++)
                {
                    LBTextureFilter lbFilterTexture = filterList[tf];

                    if (lbFilterTexture != null)
                    {
                        if (lbFilterTexture.filterType == LBTextureFilter.FilterType.StencilLayer)
                        {
                            sb.Append("\tlbFilter" + texInstAbrev + " = LBTextureFilter.CreateFilter(\"" + lbFilterTexture.lbStencilGUID + "\", \"" + lbFilterTexture.lbStencilLayerGUID + "\", false); " + eol);
                            sb.Append("\tif (lbFilter" + texInstAbrev + " != null) " + eol);
                            sb.Append("\t{ " + eol);
                            sb.Append("\t\tlbFilter" + texInstAbrev + ".filterMode = LBTextureFilter.FilterMode." + lbFilterTexture.filterMode + ";" + eol);
                            sb.Append("\t\t" + texInst + ".filterList.Add(lbFilter" + texInstAbrev + "); " + eol);
                            sb.Append("\t} " + eol);
                            sb.Append(eol);
                        }
                        else
                        {
                            sb.Append("\t// Currently we do not output Area filters for runtime Texturing. Contact support or post in our Unity forum if you need this feature." + eol);
                        }
                    }                  
                }
            }

            sb.Append("\t" + texInst + ".lbTerrainDataList = null; " + eol);

            sb.Append("\t// NOTE Add the new Texture to the landscape meta-data");
            sb.Append(eol);
            sb.Append("\tlandscape.terrainTexturesList.Add(" + texInst + ");");
            sb.Append(eol);

            sb.Append("}" + eol);
            sb.Append("#endregion" + eol);
            sb.Append("// END OF CODE SEGMENT" + eol);

            return sb.ToString();
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Get a new list of LBTerrainTextures from a given list. Only return items that are not
        /// disabled and have a Texture2D texture.
        /// </summary>
        /// <param name="terrainTexturesList"></param>
        /// <returns></returns>
        public static List<LBTerrainTexture> GetActiveTextureList(List<LBTerrainTexture> terrainTexturesList)
        {
            List<LBTerrainTexture> terrainTexturesListNoDisabled = new List<LBTerrainTexture>(terrainTexturesList);
            for (int i = terrainTexturesListNoDisabled.Count - 1; i >= 0; i--)
            {
                if (terrainTexturesListNoDisabled[i].isDisabled || terrainTexturesListNoDisabled[i].texture == null)
                {
                    terrainTexturesListNoDisabled.Remove(terrainTexturesListNoDisabled[i]);
                }
            }

            return terrainTexturesListNoDisabled;
        }

        #if UNITY_2018_3_OR_NEWER

        /// <summary>
        /// Converts a Terrain Texture list to a TerrainLayer list
        /// If the Texture is tinted, use the tinted texture rather than the main texture
        /// Currently doesn't rotate the normalmap as that can produce incorrect results.
        /// </summary>
        /// <param name="terrainTextureList"></param>
        /// <returns></returns>
        public static List<TerrainLayer> ToTerrainLayerList(List<LBTerrainTexture> terrainTextureList)
        {
            if (terrainTextureList != null)
            {
                List<TerrainLayer> terrainLayerList = new List<TerrainLayer>();

                for (int i = 0; i < terrainTextureList.Count; i++)
                {
                    TerrainLayer temp = new TerrainLayer();

                    temp.diffuseTexture = terrainTextureList[i].GetTexture2D();

                    //// If the Texture is tinted, use the tinted texture rather than the main texture
                    //if (terrainTextureList[i].isTinted)
                    //{
                    //    // If it is Tinted AND rotated, rotate the tinted texture
                    //    if (terrainTextureList[i].isRotated)
                    //    {
                    //        temp.diffuseTexture = LBTextureOperations.RotateTexture(terrainTextureList[i].tintedTexture, terrainTextureList[i].rotationAngle);
                    //    }
                    //    else { temp.diffuseTexture = terrainTextureList[i].tintedTexture; }
                    //}
                    //else if (terrainTextureList[i].isRotated) { temp.diffuseTexture = terrainTextureList[i].rotatedTexture; }
                    //else { temp.diffuseTexture = terrainTextureList[i].texture; }

                    temp.name = "Texture " + i.ToString("000 ") + (temp.diffuseTexture == null ? "" : temp.diffuseTexture.name);

                    temp.normalMapTexture = terrainTextureList[i].normalMap;
                    temp.smoothness = terrainTextureList[i].smoothness;
                    temp.metallic = terrainTextureList[i].metallic;
                    temp.tileSize = terrainTextureList[i].tileSize;
                    terrainLayerList.Add(temp);
                }

                return terrainLayerList;
            }
            else { return null; }
        }

        /// <summary>
        /// Converts a TerrainLayers array into a Terrain Texture List
        /// </summary>
        /// <param name="terrainLayers"></param>
        /// <returns></returns>
        public static List<LBTerrainTexture> ToLBTerrainTextureList(TerrainLayer[] terrainLayers)
        {
            List<LBTerrainTexture> terrainTextureList = null;

            int numTerrainLayers = terrainLayers == null ? 0 : terrainLayers.Length;

            if (numTerrainLayers > 0)
            {
                terrainTextureList = new List<LBTerrainTexture>();

                //Debug.Log("[DEBUG] numTerrainLayers: " + numTerrainLayers);

                for (int i = 0; i < terrainLayers.Length; i++)
                {
                    LBTerrainTexture temp = new LBTerrainTexture();

                    if (terrainLayers[i] != null)
                    {
                        temp.texture = terrainLayers[i].diffuseTexture;
                        temp.normalMap = terrainLayers[i].normalMapTexture;
                        temp.smoothness = terrainLayers[i].smoothness;
                        temp.metallic = terrainLayers[i].metallic;
                        temp.tileSize = terrainLayers[i].tileSize;
                    }
                    //else
                    //{
                    //    Debug.Log("[DEBUG] terrainLayer: " + i + " is null");
                    //}
                    terrainTextureList.Add(temp);
                    temp = null;
                }
            }

            return terrainTextureList;
        }

        #else

        /// <summary>
        /// Converts a Terrain Texture list to a Splat Prototype list
        /// If the Texture is tinted, use the tinted texture rather than the main texture
        /// </summary>
        /// <returns>The splat prototype list.</returns>
        /// <param name="terrainTextureList">Terrain texture list.</param>
        public static List<SplatPrototype> ToSplatPrototypeList(List<LBTerrainTexture> terrainTextureList)
        {
            if (terrainTextureList != null)
            {
                List<SplatPrototype> splatPrototypeList = new List<SplatPrototype>();

                for (int i = 0; i < terrainTextureList.Count; i++)
                {
                    SplatPrototype temp = new SplatPrototype();

                    temp.texture = terrainTextureList[i].GetTexture2D();

                    // Currently, rotation of normalmaps produces incorrect results.
                    // If it is rotated, rotate the normalmap too
                    //if (terrainTextureList[i].isRotated && terrainTextureList[i].normalMap != null)
                    //{
                    //    // At Runtime, the normalmap would need to already be readable.
                    //    temp.normalMap = LBTextureOperations.RotateTexture(terrainTextureList[i].normalMap, terrainTextureList[i].rotationAngle);
                    //}
                    //else { temp.normalMap = terrainTextureList[i].normalMap; }

                    temp.normalMap = terrainTextureList[i].normalMap;
                    temp.smoothness = terrainTextureList[i].smoothness;
                    temp.metallic = terrainTextureList[i].metallic;
                    temp.tileSize = terrainTextureList[i].tileSize;
                    splatPrototypeList.Add(temp);
                }
                return splatPrototypeList;
            }
            else { return null; }
        }

        /// <summary>
        /// Converts a Splat Prototype array into a Terrain Texture List
        /// Added version 1.2
        /// </summary>
        /// <param name="splatPrototypes"></param>
        /// <returns></returns>
        public static List<LBTerrainTexture> ToLBTerrainTextureList(SplatPrototype[] splatPrototypes)
        {
            List<LBTerrainTexture> terrainTextureList = null;

            if (splatPrototypes != null)
            {
                if (splatPrototypes.Length > 0)
                {
                    terrainTextureList = new List<LBTerrainTexture>();

                    for (int i = 0; i < splatPrototypes.Length; i++)
                    {
                        LBTerrainTexture temp = new LBTerrainTexture();
                        temp.texture = splatPrototypes[i].texture;
                        temp.normalMap = splatPrototypes[i].normalMap;
                        temp.smoothness = splatPrototypes[i].smoothness;
                        temp.metallic = splatPrototypes[i].metallic;
                        temp.tileSize = splatPrototypes[i].tileSize;
                        terrainTextureList.Add(temp);
                        temp = null;
                    }
                }
            }

            return terrainTextureList;
        }

        #endif

        /// <summary>
        ///  Given a list of LBTerrainTextures, return an array of Placement Rule structures.
        /// </summary>
        /// <param name="terrainTextureList"></param>
        /// <param name="terrainHeight"></param>
        /// <param name="terrainSize"></param>
        /// <returns></returns>
        public static LBLandscapeTerrain.LBTexRule[] GetPlacementRules(List<LBTerrainTexture> terrainTextureList, float terrainHeight, Vector2 terrainSize)
        {
            if (terrainTextureList != null)
            {
                // Create an array equal to the number of textures in the list
                LBLandscapeTerrain.LBTexRule[] lbTexRulesArray = new LBLandscapeTerrain.LBTexRule[terrainTextureList.Count];
                for (int i = 0; i < lbTexRulesArray.Length; i++)
                {
                    // Only use minHeight or maxHeight if this Texture is the correct mode, else set to 0.0 or 1.0
                    if (terrainTextureList[i].texturingMode == TexturingMode.HeightAndInclination ||
                        terrainTextureList[i].texturingMode == TexturingMode.HeightInclinationMap ||
                        terrainTextureList[i].texturingMode == TexturingMode.HeightInclinationCurvature ||
                        terrainTextureList[i].texturingMode == TexturingMode.Height)
                    {
                        lbTexRulesArray[i].minHeightN = terrainTextureList[i].minHeight;
                        lbTexRulesArray[i].maxHeightN = terrainTextureList[i].maxHeight;
                    }
                    else
                    {
                        lbTexRulesArray[i].minHeightN = 0f;
                        lbTexRulesArray[i].maxHeightN = 1f;
                    }

                    // Only use minInclination or maxInclination if this Texture is the correct mode, else set to 0.0 or 90.0
                    if (terrainTextureList[i].texturingMode == TexturingMode.HeightAndInclination ||
                        terrainTextureList[i].texturingMode == TexturingMode.HeightInclinationMap ||
                        terrainTextureList[i].texturingMode == TexturingMode.HeightInclinationCurvature ||
                        terrainTextureList[i].texturingMode == TexturingMode.Inclination)
                    {
                        lbTexRulesArray[i].minInclination = terrainTextureList[i].minInclination;
                        lbTexRulesArray[i].maxInclination = terrainTextureList[i].maxInclination;
                    }
                    else
                    {
                        lbTexRulesArray[i].minInclination = 0f;
                        lbTexRulesArray[i].maxInclination = 90f;
                    }

                    if (terrainTextureList[i].texturingMode == TexturingMode.HeightInclinationCurvature)
                    {
                        lbTexRulesArray[i].isCurvatureConcave = (terrainTextureList[i].isCurvatureConcave ? 1 : 0);
                        lbTexRulesArray[i].curvatureMinHeightDiffN = terrainTextureList[i].curvatureMinHeightDiff / terrainHeight;
                        lbTexRulesArray[i].curvatureDistanceXN = terrainTextureList[i].curvatureDistance / terrainSize.x;
                        lbTexRulesArray[i].curvatureDistanceZN = terrainTextureList[i].curvatureDistance / terrainSize.y;
                    }
                    else
                    {
                        // Defaults
                        lbTexRulesArray[i].isCurvatureConcave = 0;
                        lbTexRulesArray[i].curvatureMinHeightDiffN = 1f / terrainHeight;
                        lbTexRulesArray[i].curvatureDistanceXN = 5f / terrainSize.x;
                        lbTexRulesArray[i].curvatureDistanceZN = 5f / terrainSize.y;
                    }

                }
                return lbTexRulesArray;
            }
            else { return null; }
        }

        /// <summary>
        /// To be used to populate a EditorGUILayout.Popup()
        /// </summary>
        /// <param name="terrainTextureList"></param>
        /// <returns></returns>
        public static string[] GetTextureNameArray(List<LBTerrainTexture> terrainTextureList)
        {
            List<string> textureNameList = new List<string>();
            string item;

            if (terrainTextureList != null)
            {
                for (int t = 0; t < terrainTextureList.Count; t++)
                {
                    item = string.Empty;
                    if (terrainTextureList[t] != null)
                    {
                        item = "Texture " + (t + 1).ToString() + " - ";
                        // Append the name of the texture
                        if (terrainTextureList[t].texture != null) { item += terrainTextureList[t].texture.name; }
                        else { item += " no texture"; }
                    }
                    if (!string.IsNullOrEmpty(item)) { textureNameList.Add(item); }
                }
            }

            return textureNameList.ToArray();
        }

        /// <summary>
        /// Return the active min/max LBTerrainTexture map texture sizes uses in the landscape
        /// TexturingMode must be Map or HeightInclinationMap
        /// x = MinWidth, y = MinHeight, z = MaxWidth, w = MaxHeight
        /// If none, return z and w = -1;
        /// </summary>
        /// <param name="landscape"></param>
        /// <returns></returns>
        public static Vector4 GetMinMaxMapSize(List<LBTerrainTexture> terrainTextureList)
        {
            Vector4 lbMapMinMaxSize = Vector4.one * -1f;
            // Set min values to an arbitory number
            lbMapMinMaxSize.x = 1000000f;
            lbMapMinMaxSize.y = 1000000f;

            if (terrainTextureList != null)
            {
                List<LBTerrainTexture> mapOnlyTextureList = terrainTextureList.FindAll(tt => !tt.isDisabled && (tt.texturingMode == LBTerrainTexture.TexturingMode.Map || tt.texturingMode == LBTerrainTexture.TexturingMode.HeightInclinationMap) && tt.map != null);
                int numMaps = (mapOnlyTextureList == null ? 0 : mapOnlyTextureList.Count);

                for (int txIdx = 0; txIdx < numMaps; txIdx++)
                {
                    float width = (float)mapOnlyTextureList[txIdx].map.width;
                    float height = (float)mapOnlyTextureList[txIdx].map.height;

                    if (width < lbMapMinMaxSize.x) { lbMapMinMaxSize.x = width; }
                    if (width > lbMapMinMaxSize.z) { lbMapMinMaxSize.z = width; }

                    if (height < lbMapMinMaxSize.y) { lbMapMinMaxSize.y = height; }
                    if (height > lbMapMinMaxSize.w) { lbMapMinMaxSize.w = height; }
                }
            }

            return lbMapMinMaxSize;
        }

        /// <summary>
        /// Get the total number of unique stencil layers of a given resolution within a list of LBTerrainTextures.
        /// NOTE: Assumes that stencil, stencilLayer and stencilLayerResolution has already been cached in the LBTextureFilter.
        /// </summary>
        /// <param name="terrainTextureList"></param>
        /// <param name="resolution"></param>
        /// <returns></returns>
        public static int GetNumStencilLayersByResolution(List<LBTerrainTexture> terrainTextureList, int resolution)
        {
            int numUniqueLayers = 0;
            LBTerrainTexture lbTerrainTexture = null;
            LBTextureFilter lbTextureFilter = null;

            int numLBTextures = (terrainTextureList == null ? 0 : terrainTextureList.Count);

            if (numLBTextures > 0)
            {
                // Create with some initial capacity
                List<string> stencilLayerGUIDList = new List<string>(10);

                for (int ttIdx = 0; ttIdx < numLBTextures; ttIdx++)
                {
                    lbTerrainTexture = terrainTextureList[ttIdx];

                    // Get number of LBTerrainFilters for this LBTerrainTexture
                    int numFilters = lbTerrainTexture.filterList == null ? 0 : lbTerrainTexture.filterList.Count;

                    for (int fIdx = 0; fIdx < numFilters; fIdx++)
                    {
                        lbTextureFilter = lbTerrainTexture.filterList[fIdx];

                        if (lbTextureFilter != null && lbTextureFilter.filterType == LBTextureFilter.FilterType.StencilLayer && !string.IsNullOrEmpty(lbTextureFilter.lbStencilGUID) && !string.IsNullOrEmpty(lbTextureFilter.lbStencilLayerGUID) && lbTextureFilter.stencilLayerResolution == resolution)
                        {
                            // If it doesn't already exist, add it to the list of unique Stencil Layer GUIDs
                            if (!stencilLayerGUIDList.Exists(guid => guid == lbTextureFilter.lbStencilLayerGUID))
                            {
                                numUniqueLayers++;
                                stencilLayerGUIDList.Add(lbTextureFilter.lbStencilLayerGUID);
                                //Debug.Log("[DEBUG] GetNumStencilLayersByResolution - Adding " + lbTextureFilter.lbStencilLayerGUID + " resolution: " + resolution);
                            }
                        }
                    }
                }
            }

            return numUniqueLayers;
        }

    }
    #endregion
}