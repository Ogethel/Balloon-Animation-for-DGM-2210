using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace LandscapeBuilder
{
    [System.Serializable]
    public class LBTerrainGrass
    {
        // Terrain grass class
        #region Enumerations
        public enum GrassPlacingMode
        {
            Height = 0,
            Inclination = 1,
            HeightAndInclination = 2,
            ConstantInfluence = 3,
            Map = 4,
            HeightInclinationMap = 5,
            HeightInclinationCurvature = 6,
            GroupsOnly = 11,
            Imported = 20
        }

        public enum GrassPatchFadingMode
        {
            DontFade = 0,
            Fade = 1
        }
        #endregion

        #region Variables and Properties

        public Texture2D texture;

        // Added v1.4.2 Beta 5c
        // Last known texture name. Used to help detect missing textures.
        public string textureName;

        public float minHeight;
        public float maxHeight;
        public float minWidth;
        public float maxWidth;
        public Color dryColour;
        public Color healthyColour;
        public float noiseSpread;
        public float minPopulatedHeight;
        public float maxPopulatedHeight;
        public float minInclination;
        public float maxInclination;
        public float influence;
        public int minDensity;          // added 1.4.2 beta 4a
        public int density;             // max density of a grass patch
        public DetailRenderMode detailRenderMode;

        // Added v2.0.7 Beta 6
        public bool isCurvatureConcave = false;
        [Range(0.01f, 20f)] public float curvatureMinHeightDiff;
        [Range(0.1f, 50f)] public float curvatureDistance;

        // Added v1.1 Beta 7
        public Texture2D map;
        public Color mapColour;
        public int mapTolerance;
        // Added v1.3.2 Beta 7b
        public AnimationCurve mapToleranceBlendCurve;
        public bool mapIsPath;

        // Added v1.2 Beta 12
        public bool mapInverse;

        // Added v1.2.1
        public bool isDisabled;

        public GrassPlacingMode grassPlacingMode;
        public GrassPatchFadingMode grassPatchFadingMode;

        // Added with v1.2.1
        public List<LBFilter> filterList;

        // Added v1.3.0 Beta 3a
        public bool showGrass;

        // Added v1.3.1 Beta 6a
        public bool useNoise;
        public float noiseTileSize;
        public int noiseOctaves;
        public float grassPlacementCutoff;

        // Added 1.4.2 Beta 3f - stores grass density data from an imported terrain
        // There is a LBTerrainData instance for each terrain in the landscape for this Grass Type
        public List<LBTerrainData> lbTerrainDataList;

        // Added v1.4.2 Beta 5c
        public bool useMeshPrefab;
        public GameObject meshPrefab;
        // Last known prefab name. Used to help detect missing mesh prefabs.
        public string meshPrefabName;
        // Added 2.0.7 Beta 4a
        public bool showPrefabPreview;

        // Added v2.0.0
        public string GUID;

        #endregion

        #region Constructors
        // Class constructors
        public LBTerrainGrass()
        {
            this.texture = null;

            // Added v1.4.2 Beta 5b
            this.textureName = string.Empty;

            this.minHeight = 0.5f;
            this.maxHeight = 1f;
            this.minWidth = 0.5f;
            this.maxWidth = 1f;
            this.healthyColour = new Color(67f / 255f, 249 / 255f, 42 / 255f, 1f);
            this.dryColour = new Color(205f / 255f, 188f / 255f, 26f / 255f, 1f);
            this.noiseSpread = 0.1f;
            this.minPopulatedHeight = 0.5f;
            this.maxPopulatedHeight = 1f;
            this.minInclination = 0f;
            this.maxInclination = 30f;
            this.influence = 0.5f;
            this.minDensity = 0;
            // So to a low default (2) max density to cater for mesh grasses.
            this.density = 2;
            this.detailRenderMode = DetailRenderMode.Grass;
            this.grassPlacingMode = GrassPlacingMode.Height;
            this.grassPatchFadingMode = GrassPatchFadingMode.DontFade;

            // Added v2.0.7 Beta 6
            this.isCurvatureConcave = false;
            this.curvatureMinHeightDiff = 1f;
            this.curvatureDistance = 5f;

            // Added v1.1 Beta 7
            this.map = null;
            this.mapColour = UnityEngine.Color.green;
            this.mapTolerance = 1;
            this.mapInverse = false;

            // Added v1.3.2 Beta 7b
            this.mapToleranceBlendCurve = LBMap.GetDefaultToleranceBlendCurve;
            this.mapIsPath = false;

            // Added v1.2.1
            this.isDisabled = false;

            // Added v1.3.0 Beta 3a
            this.showGrass = true;

            // Added v1.3.1 Beta 6a
            this.useNoise = false;
            this.noiseTileSize = 10f;
            this.noiseOctaves = 5;
            this.grassPlacementCutoff = 0.5f;

            // Added v1.4.2 Beta 3f
            this.lbTerrainDataList = null;

            // Added v1.4.2 Beta 5c
            this.useMeshPrefab = false;
            this.meshPrefab = null;
            this.meshPrefabName = string.Empty;

            // Added v2.0.7 Beta 4a
            this.showPrefabPreview = false;

            // Added v2.0.0
            // Assign a unique identifier
            GUID = System.Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Constructor to create a clone of a LBTerrainGrass instance
        /// </summary>
        /// <param name="lbTerrainGrass"></param>
        public LBTerrainGrass(LBTerrainGrass lbTerrainGrass)
        {
            this.texture = lbTerrainGrass.texture;
            if (lbTerrainGrass.textureName == null) { this.textureName = string.Empty; }
            else { this.textureName = lbTerrainGrass.textureName; }
            this.minHeight = lbTerrainGrass.minHeight;
            this.maxHeight = lbTerrainGrass.maxHeight;
            this.minWidth = lbTerrainGrass.minWidth;
            this.maxWidth = lbTerrainGrass.maxWidth;
            this.healthyColour = lbTerrainGrass.healthyColour;
            this.dryColour = lbTerrainGrass.dryColour;
            this.noiseSpread = lbTerrainGrass.noiseSpread;
            this.minPopulatedHeight = lbTerrainGrass.minPopulatedHeight;
            this.maxPopulatedHeight = lbTerrainGrass.maxPopulatedHeight;
            this.minInclination = lbTerrainGrass.minInclination;
            this.maxInclination = lbTerrainGrass.maxInclination;
            this.influence = lbTerrainGrass.influence;
            this.minDensity = lbTerrainGrass.minDensity;
            this.density = lbTerrainGrass.density;
            this.detailRenderMode = lbTerrainGrass.detailRenderMode;
            this.isCurvatureConcave = lbTerrainGrass.isCurvatureConcave;
            this.curvatureDistance = lbTerrainGrass.curvatureDistance;
            this.curvatureMinHeightDiff = lbTerrainGrass.curvatureMinHeightDiff;
            this.grassPlacingMode = lbTerrainGrass.grassPlacingMode;
            this.grassPatchFadingMode = lbTerrainGrass.grassPatchFadingMode;
            this.map = lbTerrainGrass.map;
            this.mapColour = lbTerrainGrass.mapColour;
            this.mapTolerance = lbTerrainGrass.mapTolerance;
            this.mapInverse = lbTerrainGrass.mapInverse;
            this.mapToleranceBlendCurve = lbTerrainGrass.mapToleranceBlendCurve;
            this.mapIsPath = lbTerrainGrass.mapIsPath;
            this.isDisabled = lbTerrainGrass.isDisabled;
            if (lbTerrainGrass.filterList != null) { this.filterList = LBFilter.CopyList(lbTerrainGrass.filterList); }
            else { this.filterList = new List<LBFilter>(); }
            this.showGrass = lbTerrainGrass.showGrass;
            this.useNoise = lbTerrainGrass.useNoise;
            this.noiseTileSize = lbTerrainGrass.noiseTileSize;
            this.noiseOctaves = lbTerrainGrass.noiseOctaves;
            this.grassPlacementCutoff = lbTerrainGrass.grassPlacementCutoff;
            if (lbTerrainGrass.lbTerrainDataList == null) { this.lbTerrainDataList = null; }
            else { this.lbTerrainDataList = new List<LBTerrainData>(lbTerrainGrass.lbTerrainDataList); }
            this.useMeshPrefab = lbTerrainGrass.useMeshPrefab;
            this.meshPrefab = lbTerrainGrass.meshPrefab;
            if (lbTerrainGrass.meshPrefabName == null) { this.meshPrefabName = string.Empty; }
            else { this.meshPrefabName = lbTerrainGrass.meshPrefabName; }
            this.showPrefabPreview = lbTerrainGrass.showPrefabPreview;
            this.GUID = lbTerrainGrass.GUID;
        }

        #endregion

        #region Public Non-Static Methods

        public string ScriptGrass(int GrassIdx, string EndOfLineMarker = "\n")
        {
            // Create a new instance of StringBuilder and give it an estimated capacity
            System.Text.StringBuilder sb = new System.Text.StringBuilder(1000);

            string eol = " ";

            // We always need a space between lines OR a end of line marker like "\n"
            if (EndOfLineMarker.Length > 0) { eol = EndOfLineMarker; }

            string grInst = "lbTerrainGrass" + (GrassIdx + 1);
            string grInstAbrev = "Grs" + (GrassIdx + 1);

            sb.Append("// Grass Code generated from Landscape Builder 2 at " + System.DateTime.Now.ToShortTimeString() + " on " + System.DateTime.Now.ToLongDateString() + eol + eol);

            sb.Append("// BEGIN Public variables to populate in the editor - Uncomment and add these to top of class " + eol);
            sb.Append("//[Header(\"Grass" + (GrassIdx + 1) + "\")] " + eol);
            sb.Append("//public Texture2D texture" + grInstAbrev + "; " + eol);
            sb.Append("//public GameObject meshPrefab" + grInstAbrev + "; " + eol);
            sb.Append("//public Texture2D map" + grInstAbrev + "; " + eol);
            sb.Append("// END Public variables" + eol + eol);

            sb.Append("#region LBTerrainGrass" + (GrassIdx + 1) + eol);
            sb.Append("LBTerrainGrass " + grInst + " = new LBTerrainGrass(); " + eol);
            sb.Append("if (" + grInst + " != null)" + eol);
            sb.Append("{" + eol);
            sb.Append("\t" + grInst + ".texture = texture" + grInstAbrev + "; " + eol);
            sb.Append("\t" + grInst + ".textureName = " + (string.IsNullOrEmpty(textureName) ? "\"\"" : "\"" + textureName + "\"") + "; " + eol);
            sb.Append("\t" + grInst + ".minHeight = " + minHeight + "f; " + eol);
            sb.Append("\t" + grInst + ".maxHeight = " + maxHeight + "f; " + eol);
            sb.Append("\t" + grInst + ".minWidth = " + minHeight + "f; " + eol);
            sb.Append("\t" + grInst + ".maxWidth = " + maxHeight + "f; " + eol);
            sb.Append("\t" + grInst + ".healthyColour = new Color(" + healthyColour.r + "f," + healthyColour.g + "f," + healthyColour.b + "f," + healthyColour.a + "f); " + eol);
            sb.Append("\t" + grInst + ".dryColour = new Color(" + dryColour.r + "f," + dryColour.g + "f," + dryColour.b + "f," + dryColour.a + "f); " + eol);
            sb.Append("\t" + grInst + ".noiseSpread = " + noiseSpread + "f; " + eol);
            sb.Append("\t" + grInst + ".minPopulatedHeight = " + minPopulatedHeight + "f; " + eol);
            sb.Append("\t" + grInst + ".maxPopulatedHeight = " + maxPopulatedHeight + "f; " + eol);
            sb.Append("\t" + grInst + ".minInclination = " + minInclination + "f; " + eol);
            sb.Append("\t" + grInst + ".maxInclination = " + maxInclination + "f; " + eol);
            sb.Append("\t" + grInst + ".influence = " + influence + "f; " + eol);
            sb.Append("\t" + grInst + ".minDensity = " + minDensity + "; " + eol);
            sb.Append("\t" + grInst + ".density = " + density + "; " + eol);
            sb.Append("\t" + grInst + ".detailRenderMode = DetailRenderMode." + detailRenderMode + "; " + eol);
            sb.Append("\t" + grInst + ".grassPlacingMode = LBTerrainGrass.GrassPlacingMode." + grassPlacingMode + "; " + eol);
            sb.Append("\t" + grInst + ".grassPatchFadingMode = LBTerrainGrass.GrassPatchFadingMode." + grassPatchFadingMode + "; " + eol);
            sb.Append("\t" + grInst + ".isCurvatureConcave = " + isCurvatureConcave.ToString().ToLower() + "; " + eol);
            sb.Append("\t" + grInst + ".curvatureDistance = " + curvatureDistance + "f; " + eol);
            sb.Append("\t" + grInst + ".curvatureMinHeightDiff = " + curvatureMinHeightDiff + "f; " + eol);
            sb.Append("\t" + grInst + ".map = map" + grInstAbrev + "; " + eol);
            sb.Append("\t" + grInst + ".mapColour = new Color(" + mapColour.r + "f," + mapColour.g + "f," + mapColour.b + "f," + mapColour.a + "f); " + eol);
            sb.Append("\t" + grInst + ".mapTolerance = " + mapTolerance + "; " + eol);
            sb.Append("\t" + grInst + ".mapInverse = " + mapInverse.ToString().ToLower() + "; " + eol);
            sb.Append("\t" + grInst + ".mapToleranceBlendCurve = LBMap.GetDefaultToleranceBlendCurve; " + eol);
            sb.Append("\t" + grInst + ".mapIsPath = " + mapIsPath.ToString().ToLower() + "; " + eol);
            sb.Append("\t" + grInst + ".isDisabled = " + isDisabled.ToString().ToLower() + "; " + eol);
            sb.Append("\t" + grInst + ".showGrass = " + showGrass.ToString().ToLower() + "; " + eol);
            sb.Append("\t" + grInst + ".useNoise = " + useNoise.ToString().ToLower() + "; " + eol);
            sb.Append("\t" + grInst + ".noiseTileSize = " + noiseTileSize + "f; " + eol);
            sb.Append("\t" + grInst + ".grassPlacementCutoff = " + grassPlacementCutoff + "f; " + eol);
            sb.Append("\t" + grInst + ".noiseOctaves = " + noiseOctaves + "; " + eol);
            sb.Append("\t" + grInst + ".useMeshPrefab = " + useMeshPrefab.ToString().ToLower() + "; " + eol);
            sb.Append("\t" + grInst + ".meshPrefab = meshPrefab" + grInstAbrev + "; " + eol);
            sb.Append("\t" + grInst + ".meshPrefabName = " + (string.IsNullOrEmpty(meshPrefabName) ? "\"\"" : "\"" + meshPrefabName + "\"") + "; " + eol);
            sb.Append("\t" + grInst + ".showPrefabPreview = " + showPrefabPreview.ToString().ToLower() + "; " + eol);
            sb.Append("\t" + grInst + ".GUID = " + (string.IsNullOrEmpty(GUID) ? "\"\"" : "\"" + GUID + "\"") + "; " + eol);

            sb.Append("\t" + grInst + ".filterList = new List<LBFilter>(); " + eol);
            if (filterList != null)
            {
                // Create a unique variable
                if (filterList.Exists(f => f.filterType == LBFilter.FilterType.StencilLayer))
                {
                    sb.Append("\tLBFilter lbFilter" + grInstAbrev + " = null; " + eol);
                }

                for (int tf = 0; tf < filterList.Count; tf++)
                {
                    LBFilter lbFilter = filterList[tf];

                    if (lbFilter != null)
                    {
                        if (lbFilter.filterType == LBFilter.FilterType.StencilLayer)
                        {
                            sb.Append("\tlbFilter" + grInstAbrev + " = LBFilter.CreateFilter(\"" + lbFilter.lbStencilGUID + "\", \"" + lbFilter.lbStencilLayerGUID + "\", false); " + eol);
                            sb.Append("\tif (lbFilter" + grInstAbrev + " != null) " + eol);
                            sb.Append("\t{ " + eol);
                            sb.Append("\t\tlbFilter" + grInstAbrev + ".filterMode = LBFilter.FilterMode." + lbFilter.filterMode + ";" + eol);
                            sb.Append("\t\t" + grInst + ".filterList.Add(lbFilter" + grInstAbrev + "); " + eol);
                            sb.Append("\t} " + eol);
                            sb.Append(eol);
                        }
                        else
                        {
                            sb.Append("\t// Currently we do not output non-Stencil filters for runtime Trees. Contact support or post in our Unity forum if you need this feature." + eol);
                        }
                    }
                }
            }

            sb.Append("\t" + grInst + ".lbTerrainDataList = null; " + eol);

            sb.Append("\t// NOTE Add the new Grass to the landscape meta-data");
            sb.Append(eol);
            sb.Append("\tlandscape.terrainGrassList.Add(" + grInst + ");");
            sb.Append(eol);

            sb.Append("}" + eol);
            sb.Append("#endregion" + eol);
            sb.Append("// END OF CODE SEGMENT" + eol);

            return sb.ToString();
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Converts a Terrain Grass list to a Detail Prototype list
        /// </summary>
        /// <returns>The detail prototype list.</returns>
        /// <param name="terrainGrassList">Terrain grass list.</param>
        public static List<DetailPrototype> ToDetailPrototypeList(List<LBTerrainGrass> terrainGrassList)
        {
            if (terrainGrassList != null)
            {
                List<DetailPrototype> detailPrototypeList = new List<DetailPrototype>();
                for (int i = 0; i < terrainGrassList.Count; i++)
                {
                    DetailPrototype temp = new DetailPrototype();
                    // NOTE: Texture will be null if a mesh is being used
                    temp.prototypeTexture = terrainGrassList[i].texture;
                    temp.minHeight = terrainGrassList[i].minHeight;
                    temp.maxHeight = terrainGrassList[i].maxHeight;
                    temp.minWidth = terrainGrassList[i].minWidth;
                    temp.maxWidth = terrainGrassList[i].maxWidth;
                    temp.dryColor = terrainGrassList[i].dryColour;
                    temp.healthyColor = terrainGrassList[i].healthyColour;
                    temp.noiseSpread = terrainGrassList[i].noiseSpread;
                    temp.renderMode = terrainGrassList[i].detailRenderMode;
                    // Used when using a mesh prefab instead of a texture for the grass
                    temp.usePrototypeMesh = terrainGrassList[i].useMeshPrefab;
                    temp.prototype = terrainGrassList[i].meshPrefab;
                    detailPrototypeList.Add(temp);
                }
                return detailPrototypeList;
            }
            else { return null; }
        }

        /// <summary>
        /// Converts a Detail Prototype array into a Terrain Grass List
        /// Added version 1.2
        /// </summary>
        /// <param name="detailPrototypes"></param>
        /// <returns></returns>
        public static List<LBTerrainGrass> ToLBTerrainGrassList(DetailPrototype[] detailPrototypes)
        {
            List<LBTerrainGrass> terrainGrassList = null;

            if (detailPrototypes != null)
            {
                if (detailPrototypes.Length > 0)
                {
                    terrainGrassList = new List<LBTerrainGrass>();

                    for (int i = 0; i < detailPrototypes.Length; i++)
                    {
                        LBTerrainGrass temp = new LBTerrainGrass();
                        // NOTE: Texture will be null if a mesh is being used
                        temp.texture = detailPrototypes[i].prototypeTexture;
                        temp.minHeight = detailPrototypes[i].minHeight;
                        temp.maxHeight = detailPrototypes[i].maxHeight;
                        temp.minWidth = detailPrototypes[i].minWidth;
                        temp.maxWidth = detailPrototypes[i].maxWidth;
                        temp.dryColour = detailPrototypes[i].dryColor;
                        temp.healthyColour = detailPrototypes[i].healthyColor;
                        temp.noiseSpread = detailPrototypes[i].noiseSpread;
                        temp.detailRenderMode = detailPrototypes[i].renderMode;
                        // Used when using a mesh prefab instead of a texture for the grass
                        temp.useMeshPrefab = detailPrototypes[i].usePrototypeMesh;
                        temp.meshPrefab = detailPrototypes[i].prototype;
                        terrainGrassList.Add(temp);
                        temp = null;
                    }
                }
            }

            return terrainGrassList;
        }

        /// <summary>
        /// Converts a Detail Prototype array into a Terrain Grass List
        /// An empty (but not null) terrainGrassList is supplied to reduce GC
        /// </summary>
        /// <param name="detailPrototypes"></param>
        /// <param name="terrainGrassList"></param>
        public static void ToLBTerrainGrassList(DetailPrototype[] detailPrototypes, List<LBTerrainGrass> terrainGrassList)
        {
            if (detailPrototypes != null && terrainGrassList != null)
            {
                if (detailPrototypes.Length > 0)
                {
                    terrainGrassList.Clear();

                    for (int i = 0; i < detailPrototypes.Length; i++)
                    {
                        LBTerrainGrass temp = new LBTerrainGrass();
                        // NOTE: Texture will be null if a mesh is being used
                        temp.texture = detailPrototypes[i].prototypeTexture;
                        temp.minHeight = detailPrototypes[i].minHeight;
                        temp.maxHeight = detailPrototypes[i].maxHeight;
                        temp.minWidth = detailPrototypes[i].minWidth;
                        temp.maxWidth = detailPrototypes[i].maxWidth;
                        temp.dryColour = detailPrototypes[i].dryColor;
                        temp.healthyColour = detailPrototypes[i].healthyColor;
                        temp.noiseSpread = detailPrototypes[i].noiseSpread;
                        temp.detailRenderMode = detailPrototypes[i].renderMode;
                        // Used when using a mesh prefab instead of a texture for the grass
                        temp.useMeshPrefab = detailPrototypes[i].usePrototypeMesh;
                        temp.meshPrefab = detailPrototypes[i].prototype;
                        terrainGrassList.Add(temp);
                        temp = null;
                    }
                }
            }
        }

        /// <summary>
        /// To be used to populate a EditorGUILayout.Popup()
        /// </summary>
        /// <param name="terrainGrassList"></param>
        /// <returns></returns>
        public static string[] GetGrassNameArray(List<LBTerrainGrass> terrainGrassList)
        {
            List<string> grassNameList = new List<string>();
            string item;
            string grassName;

            if (terrainGrassList != null)
            {
                for (int t = 0; t < terrainGrassList.Count; t++)
                {
                    item = string.Empty;
                    if (terrainGrassList[t] != null)
                    {
                        item = "Grass " + (t + 1).ToString() + " - ";
                        // Append the name of the grass

                        if (terrainGrassList[t].useMeshPrefab)
                        {
                            // Use the prefabname or get the name from the prefab
                            grassName = terrainGrassList[t].meshPrefabName;
                            item += (!string.IsNullOrEmpty(grassName) ? grassName : (terrainGrassList[t].meshPrefab != null ? terrainGrassList[t].meshPrefab.name : " no grass prefab"));
                        }
                        else
                        {
                            // Use the texturename or get the name of the texture
                            grassName = terrainGrassList[t].textureName;
                            item += (!string.IsNullOrEmpty(grassName) ? grassName : (terrainGrassList[t].texture != null ? terrainGrassList[t].texture.name : " no grass texture"));
                        }
                        //Debug.Log("Grass list " + item + terrainGrassList[t].GUID);
                        grassNameList.Add(item);
                    }
                    //if (!string.IsNullOrEmpty(item)) { grassNameList.Add(item); }
                }
            }

            return grassNameList.ToArray();
        }


        /// <summary>
        /// Get the total number of unique stencil layers of a given resolution within a list of LBFilter.
        /// NOTE: Assumes that stencil, stencilLayer and stencilLayerResolution has already been cached in the LBFilter.
        /// </summary>
        /// <param name="lbFilterList"></param>
        /// <param name="resolution"></param>
        /// <returns></returns>
        public static int GetNumStencilLayersByResolution(List<LBFilter> lbFilterList, int resolution)
        {
            int numUniqueLayers = 0;
            LBFilter lbFilter = null;

            int numLBFilters = lbFilterList == null ? 0 : lbFilterList.Count;

            if (numLBFilters > 0)
            {
                // Create with some initial capacity
                List<string> stencilLayerGUIDList = new List<string>(10);

                for (int fIdx = 0; fIdx < numLBFilters; fIdx++)
                {
                    lbFilter = lbFilterList[fIdx];

                    // Is this a valid stencil layer with the correct resolution?
                    if (lbFilter != null && lbFilter.filterType == LBFilter.FilterType.StencilLayer && !string.IsNullOrEmpty(lbFilter.lbStencilGUID) && !string.IsNullOrEmpty(lbFilter.lbStencilLayerGUID) && lbFilter.stencilLayerResolution == resolution)
                    {
                        //Debug.Log("[DEBUG] GetNumStencilLayersByResolution " + lbFilter.lbStencilLayer.LayerName + " " + lbFilter.stencilLayerResolution + " GUID: " + lbFilter.lbStencilLayerGUID + " " + lbFilter.lbStencilLayer.GUID);

                        // If it doesn't already exist, add it to the list of unique Stencil Layer GUIDs
                        if (!stencilLayerGUIDList.Exists(guid => guid == lbFilter.lbStencilLayerGUID))
                        {
                            numUniqueLayers++;
                            stencilLayerGUIDList.Add(lbFilter.lbStencilLayerGUID);
                            //Debug.Log(" [DEBUG] GetNumStencilLayersByResolution - Adding " + lbFilter.lbStencilLayerGUID + " resolution: " + resolution);
                        }
                    }
                }
            }

            return numUniqueLayers;
        }



        #endregion
    }
}