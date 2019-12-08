using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace LandscapeBuilder
{
    [System.Serializable]
    public class LBTerrainTree
    {
        // Terrain tree class
        #region Enumerations
        public enum TreePlacingMode
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

        public enum TreeScalingMode
        {
            RandomScaling = 0,
            ScaleByTerrainHeight = 1
        }

        public enum TreePlacementSpeed
        {
            BestPlacement = 3,
            FastPlacement = 5,
            FastestPlacement = 10
        }
        #endregion

        #region Variables and Properties
        public int maxTreesPerSqrKm;

        public float bendFactor;
        public GameObject prefab;
        public float minScale;
        public float maxScale;
        public TreeScalingMode treeScalingMode;
        public bool lockWidthToHeight;

        public float minProximity;

        public float minHeight;
        public float maxHeight;
        public float minInclination;
        public float maxInclination;

        // Added v2.0.7 Beta 5
        public bool isCurvatureConcave = false;
        [Range(0.01f, 20f)] public float curvatureMinHeightDiff;
        [Range(0.1f, 50f)] public float curvatureDistance;
        [System.NonSerialized] public float curvatureDistanceXN;
        [System.NonSerialized] public float curvatureDistanceZN;

        // Added v1.1 Beta 7
        public Texture2D map;
        public Color mapColour;
        public int mapTolerance;
        public bool useNoise;
        public float noiseTileSize;
        public float noiseOffset;
        public float treePlacementCutoff;

        // Added v1.2 Beta 12
        public bool mapInverse;

        // Added 1.3.2 Beta 7b
        public AnimationCurve mapToleranceBlendCurve;
        public bool mapIsPath;

        // Added 1.3.5 Beta 5a - helps to lower bottom of tree on steep slopes
        [Range(-2f, 0f)] public float offsetY;

        // Added v1.2.1
        public bool isDisabled;

        public TreePlacingMode treePlacingMode;

        // Added with v1.2.1
        public List<LBFilter> filterList;

        // Added v1.3.0 Beta 3a
        public bool showTree;

        // Added v1.4.0 Beta 5b
        public bool isTinted;
        [Range(0f, 0.3f)] public float maxTintStrength;
        public Color tintColour;

        // Added 1.4.2 Beta 3a - stores tree instance data from an imported terrain
        // There is a LBTerrainData instance for each terrain in the landscape
        public List<LBTerrainData> lbTerrainDataList;

        // Added 1.4.2 Beta 6d
        // Last known prefab name. Used to help detect missing tree prefabs.
        public string prefabName;
        // Added 2.0.7 Beta 4a
        public bool showPrefabPreview;

        // Added v2.0.2
        public string GUID;

        #endregion

        #region Constructors

        // Class constructor
        public LBTerrainTree()
        {
            // Added v1.3.1 Beta 3b
            this.maxTreesPerSqrKm = 250;

            this.bendFactor = 0.5f;
            this.prefab = null;
            this.minScale = 0.8f;
            this.maxScale = 1.2f;
            this.treeScalingMode = TreeScalingMode.RandomScaling;
            this.lockWidthToHeight = true;
            this.minProximity = 25f;
            this.minHeight = 0.5f;
            this.maxHeight = 0.75f;
            this.minInclination = 0f;
            this.maxInclination = 30f;
            this.treePlacingMode = TreePlacingMode.HeightAndInclination;

            // Added v2.0.7 Beta 5
            this.isCurvatureConcave = false;
            this.curvatureMinHeightDiff = 1f;
            this.curvatureDistance = 5f;

            // Added v1.1 Beta 7
            this.map = null;
            this.mapColour = UnityEngine.Color.white;
            this.mapTolerance = 1;
            this.mapInverse = false;
            this.useNoise = true;
            this.noiseTileSize = 500f;
            // Noise offset cannot be modified from the editor as it is randomly set when the terrain is populated with trees
            this.treePlacementCutoff = 0.65f;

            // Added 1.3.2 Beta 7b
            this.mapToleranceBlendCurve = LBMap.GetDefaultToleranceBlendCurve;
            this.mapIsPath = false;

            // Added 1.3.5 Beta 5a
            this.offsetY = 0f;

            // Added v1.2.1
            this.isDisabled = false;

            // Added v1.3.0 Beta 3
            this.showTree = true;

            // Added v1.4.0 Beta 5b
            this.isTinted = true;
            this.maxTintStrength = 0.2f;
            this.tintColour = Color.green;

            // Added v1.4.2 Beta 3a
            this.lbTerrainDataList = null;

            // Added v1.4.2 Beta 6d
            this.prefabName = string.Empty;

            // Added v2.0.7 Beta 4a
            this.showPrefabPreview = false;

            // Added v2.0.2
            // Assign a unique identifier
            GUID = System.Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Clone Constructor
        /// </summary>
        /// <param name="lbTerrainTree"></param>
        public LBTerrainTree(LBTerrainTree lbTerrainTree)
        {
            this.maxTreesPerSqrKm = lbTerrainTree.maxTreesPerSqrKm;
            this.bendFactor = lbTerrainTree.bendFactor;
            this.prefab = lbTerrainTree.prefab;
            this.minScale = lbTerrainTree.minScale;
            this.maxScale = lbTerrainTree.maxScale;
            this.treeScalingMode = lbTerrainTree.treeScalingMode;
            this.lockWidthToHeight = lbTerrainTree.lockWidthToHeight;
            this.minProximity = lbTerrainTree.minProximity;
            this.minHeight = lbTerrainTree.minHeight;
            this.maxHeight = lbTerrainTree.maxHeight;
            this.minInclination = lbTerrainTree.minInclination;
            this.maxInclination = lbTerrainTree.maxInclination;
            this.treePlacingMode = lbTerrainTree.treePlacingMode;
            this.isCurvatureConcave = lbTerrainTree.isCurvatureConcave;
            this.curvatureDistance = lbTerrainTree.curvatureDistance;
            this.curvatureMinHeightDiff = lbTerrainTree.curvatureMinHeightDiff;
            this.map = lbTerrainTree.map;
            this.mapColour = lbTerrainTree.mapColour;
            this.mapTolerance = lbTerrainTree.mapTolerance;
            this.mapInverse = lbTerrainTree.mapInverse;
            this.useNoise = lbTerrainTree.useNoise;
            this.noiseTileSize = lbTerrainTree.noiseTileSize;
            this.noiseOffset = lbTerrainTree.noiseOffset;
            this.treePlacementCutoff = lbTerrainTree.treePlacementCutoff;
            this.mapToleranceBlendCurve = lbTerrainTree.mapToleranceBlendCurve;
            this.mapIsPath = lbTerrainTree.mapIsPath;
            this.isDisabled = lbTerrainTree.isDisabled;
            this.offsetY = lbTerrainTree.offsetY;
            this.showTree = lbTerrainTree.showTree;
            if (lbTerrainTree.filterList != null) { this.filterList = LBFilter.CopyList(lbTerrainTree.filterList); }
            else { this.filterList = new List<LBFilter>(); }
            this.isTinted = lbTerrainTree.isTinted;
            this.maxTintStrength = lbTerrainTree.maxTintStrength;
            this.tintColour = lbTerrainTree.tintColour;
            if (lbTerrainTree.lbTerrainDataList == null) { this.lbTerrainDataList = null; }
            else { this.lbTerrainDataList = new List<LBTerrainData>(lbTerrainTree.lbTerrainDataList); }
            if (lbTerrainTree.prefabName == null) { this.prefabName = string.Empty; }
            else { this.prefabName = lbTerrainTree.prefabName; }
            this.showPrefabPreview = lbTerrainTree.showPrefabPreview;

            this.GUID = lbTerrainTree.GUID;
        }

        #endregion

        #region Public Non-Static Methods

        /// <summary>
        /// Script out the Tree for use in a runtime script.
        /// TreeIdx is the zero-based position in the terrainTreesList
        /// </summary>
        /// <param name="TreeIdx"></param>
        /// <param name="EndOfLineMarker"></param>
        /// <returns></returns>
        public string ScriptTree(int TreeIdx, string EndOfLineMarker = "\n")
        {
            // Create a new instance of StringBuilder and give it an estimated capacity
            System.Text.StringBuilder sb = new System.Text.StringBuilder(1000);

            string eol = " ";

            // We always need a space between lines OR a end of line marker like "\n"
            if (EndOfLineMarker.Length > 0) { eol = EndOfLineMarker; }

            string treeInst = "lbTerrainTree" + (TreeIdx + 1);
            string treeInstAbrev = "Tree" + (TreeIdx + 1);

            sb.Append("// Tree Code generated from Landscape Builder 2 at " + System.DateTime.Now.ToShortTimeString() + " on " + System.DateTime.Now.ToLongDateString() + eol + eol);

            sb.Append("// BEGIN Public variables to populate in the editor - Uncomment and add these to top of class " + eol);
            sb.Append("//[Header(\"Tree" + (TreeIdx + 1) + "\")] " + eol);
            sb.Append("//public GameObject prefab" + treeInstAbrev + "; " + eol);
            sb.Append("//public Texture2D map" + treeInstAbrev + "; " + eol);
            sb.Append("// END Public variables" + eol + eol);

            sb.Append("#region LBTerrainTree" + (TreeIdx + 1) + eol);

            sb.Append("LBTerrainTree " + treeInst + " = new LBTerrainTree(); " + eol);
            sb.Append("if (" + treeInst + " != null)" + eol);
            sb.Append("{" + eol);
            sb.Append("\t" + treeInst + ".maxTreesPerSqrKm = " + maxTreesPerSqrKm + "; " + eol);
            sb.Append("\t" + treeInst + ".bendFactor = " + bendFactor + "f; " + eol);
            sb.Append("\t" + treeInst + ".prefab = prefab" + treeInstAbrev + "; " + eol);
            sb.Append("\t" + treeInst + ".minScale = " + minScale + "f; " + eol);
            sb.Append("\t" + treeInst + ".maxScale = " + maxScale + "f; " + eol);
            sb.Append("\t" + treeInst + ".treeScalingMode = LBTerrainTree.TreeScalingMode." + treeScalingMode + "; " + eol);
            sb.Append("\t" + treeInst + ".lockWidthToHeight = " + lockWidthToHeight.ToString().ToLower() + "; " + eol);
            sb.Append("\t" + treeInst + ".minProximity = " + minProximity + "f; " + eol);
            sb.Append("\t" + treeInst + ".minHeight = " + minHeight + "f; " + eol);
            sb.Append("\t" + treeInst + ".maxHeight = " + maxHeight + "f; " + eol);
            sb.Append("\t" + treeInst + ".minInclination = " + minInclination + "f; " + eol);
            sb.Append("\t" + treeInst + ".maxInclination = " + maxInclination + "f; " + eol);
            sb.Append("\t" + treeInst + ".treePlacingMode = LBTerrainTree.TreePlacingMode." + treePlacingMode + "; " + eol);
            sb.Append("\t" + treeInst + ".isCurvatureConcave = " + isCurvatureConcave.ToString().ToLower() + "; " + eol);
            sb.Append("\t" + treeInst + ".curvatureDistance = " + curvatureDistance + "f; " + eol);
            sb.Append("\t" + treeInst + ".curvatureMinHeightDiff = " + curvatureMinHeightDiff + "f; " + eol);
            sb.Append("\t" + treeInst + ".map = map" + treeInstAbrev + "; " + eol);
            sb.Append("\t" + treeInst + ".mapColour = new Color(" + mapColour.r + "f, " + mapColour.g + "f, " + mapColour.b + "f, " + mapColour.a + "f); " + eol);
            sb.Append("\t" + treeInst + ".mapTolerance = " + mapTolerance + "; " + eol);
            sb.Append("\t" + treeInst + ".mapInverse = " + mapInverse.ToString().ToLower() + "; " + eol);
            sb.Append("\t" + treeInst + ".useNoise = " + useNoise.ToString().ToLower() + "; " + eol);
            sb.Append("\t" + treeInst + ".noiseTileSize = " + noiseTileSize + "f; " + eol);
            sb.Append("\t" + treeInst + ".noiseOffset = " + noiseOffset + "f; " + eol);
            sb.Append("\t" + treeInst + ".treePlacementCutoff = " + treePlacementCutoff + "f; " + eol);
            sb.Append("\t" + treeInst + ".mapToleranceBlendCurve = LBMap.GetDefaultToleranceBlendCurve; " + eol);
            sb.Append("\t" + treeInst + ".mapIsPath = " + mapIsPath.ToString().ToLower() + "; " + eol);
            sb.Append("\t" + treeInst + ".isDisabled = " + isDisabled.ToString().ToLower() + "; " + eol);
            sb.Append("\t" + treeInst + ".offsetY = " + offsetY + "f; " + eol);
            sb.Append("\t" + treeInst + ".showTree = " + showTree.ToString().ToLower() + "; " + eol);
            sb.Append("\t" + treeInst + ".isTinted = " + isTinted.ToString().ToLower() + "; " + eol);
            sb.Append("\t" + treeInst + ".maxTintStrength = " + maxTintStrength + "f; " + eol);
            sb.Append("\t" + treeInst + ".tintColour = new Color(" + tintColour.r + "f, " + tintColour.g + "f, " + tintColour.b + "f, " + tintColour.a + "f); " + eol);
            sb.Append("\t" + treeInst + ".prefabName = " + (string.IsNullOrEmpty(prefabName) ? "\"\"" : "\"" + prefabName + "\"") + "; " + eol);
            sb.Append("\t" + treeInst + ".showPrefabPreview = " + showPrefabPreview.ToString().ToLower() + "; " + eol);
            sb.Append("\t" + treeInst + ".GUID = " + (string.IsNullOrEmpty(GUID) ? "\"\"" : "\"" + GUID + "\"") + "; " + eol);

            sb.Append("\t" + treeInst + ".filterList = new List<LBFilter>(); " + eol);
            if (filterList != null)
            {
                // Create a unique variable
                if (filterList.Exists(f => f.filterType == LBFilter.FilterType.StencilLayer))
                {
                    sb.Append("\tLBFilter lbFilter" + treeInstAbrev + " = null; " + eol);
                }

                for (int tf = 0; tf < filterList.Count; tf++)
                {
                    LBFilter lbFilter = filterList[tf];

                    if (lbFilter != null)
                    {
                        if (lbFilter.filterType == LBFilter.FilterType.StencilLayer)
                        {
                            sb.Append("\tlbFilter" + treeInstAbrev + " = LBFilter.CreateFilter(\"" + lbFilter.lbStencilGUID + "\", \"" + lbFilter.lbStencilLayerGUID + "\", false); " + eol);
                            sb.Append("\tif (lbFilter" + treeInstAbrev + " != null) " + eol);
                            sb.Append("\t{ " + eol);
                            sb.Append("\t\tlbFilter" + treeInstAbrev + ".filterMode = LBFilter.FilterMode." + lbFilter.filterMode + ";" + eol);
                            sb.Append("\t\t" + treeInst + ".filterList.Add(lbFilter" + treeInstAbrev + "); " + eol);
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

            sb.Append("\t" + treeInst + ".lbTerrainDataList = null; " + eol);

            sb.Append("\t// NOTE Add the new Tree to the landscape meta-data");
            sb.Append(eol);
            sb.Append("\tlandscape.terrainTreesList.Add(" + treeInst + ");");
            sb.Append(eol);

            sb.Append("}" + eol);
            sb.Append("#endregion" + eol);
            sb.Append("// END OF CODE SEGMENT" + eol);

            return sb.ToString();
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Converts a Terrain Tree list to a Unity Tree Prototype list
        /// </summary>
        /// <returns>The tree prototype list.</returns>
        /// <param name="terrainTreeList">Terrain tree list.</param>
        public static List<TreePrototype> ToTreePrototypeList(List<LBTerrainTree> terrainTreeList)
        {
            if (terrainTreeList != null)
            {
                List<TreePrototype> treePrototypeList = new List<TreePrototype>();
                for (int i = 0; i < terrainTreeList.Count; i++)
                {
                    TreePrototype temp = new TreePrototype();
                    temp.bendFactor = terrainTreeList[i].bendFactor;
                    temp.prefab = terrainTreeList[i].prefab;
                    treePrototypeList.Add(temp);
                }
                return treePrototypeList;
            }
            else { return null; }
        }

        /// <summary>
        /// Converts a TreePrototype array into a LBTerrainTree list
        /// Added version 1.2
        /// </summary>
        /// <param name="treePrototypes"></param>
        /// <returns></returns>
        public static List<LBTerrainTree> ToLBTerrainTreeList(TreePrototype[] treePrototypes)
        {
            List<LBTerrainTree> terrainTreeList = null;

            if (treePrototypes != null)
            {
                if (treePrototypes.Length > 0)
                {
                    terrainTreeList = new List<LBTerrainTree>();

                    for (int i = 0; i < treePrototypes.Length; i++)
                    {
                        LBTerrainTree temp = new LBTerrainTree();
                        temp.bendFactor = treePrototypes[i].bendFactor;
                        temp.prefab = treePrototypes[i].prefab;
                        terrainTreeList.Add(temp);
                        temp = null;
                    }
                }
            }

            return terrainTreeList;
        }

        /// <summary>
        /// Converts a TreePrototype array into a LBTerrainTree list.
        /// Accepts an existing list to reduce GC (can't be NULL else
        /// would need to pass in as a ref.
        /// </summary>
        /// <param name="treePrototypes"></param>
        /// <param name="lbTerrainTreeList"></param>
        public static void ToLBTerrainTreeList(TreePrototype[] treePrototypes, List<LBTerrainTree> lbTerrainTreeList)
        {
            if (lbTerrainTreeList == null) { return; }
            else { lbTerrainTreeList.Clear(); }

            int numTreePrototypes = (treePrototypes == null ? 0 : treePrototypes.Length);

            if (lbTerrainTreeList != null)
            {
                for (int i = 0; i < numTreePrototypes; i++)
                {
                    LBTerrainTree temp = new LBTerrainTree();
                    temp.bendFactor = treePrototypes[i].bendFactor;
                    temp.prefab = treePrototypes[i].prefab;
                    lbTerrainTreeList.Add(temp);
                    temp = null;
                }
            }
        }

        #endregion
    }
}