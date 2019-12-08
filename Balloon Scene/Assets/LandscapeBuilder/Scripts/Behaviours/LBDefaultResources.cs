#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace LandscapeBuilder
{
    [ExecuteInEditMode]
    public class LBDefaultResources : MonoBehaviour
    {

        public List<string> texturingPresetNameList;
        public List<LBTerrainTextureList> texturingPresetList;

        public List<string> treePresetNameList;
        public List<LBTerrainTreeList> treePresetList;

        public List<string> grassPresetNameList;
        public List<LBTerrainGrassList> grassPresetList;
    }

    [System.Serializable]
    public class LBTerrainTextureList
    {
        public List<LBTerrainTexture> terrainTextureList;

        public LBTerrainTextureList()
        {
            this.terrainTextureList = new List<LBTerrainTexture>();
        }
    }

    [System.Serializable]
    public class LBTerrainTreeList
    {
        public List<LBTerrainTree> terrainTreeList;

        public LBTerrainTreeList()
        {
            this.terrainTreeList = new List<LBTerrainTree>();
        }
    }

    [System.Serializable]
    public class LBTerrainGrassList
    {
        public List<LBTerrainGrass> terrainGrassList;

        public LBTerrainGrassList()
        {
            this.terrainGrassList = new List<LBTerrainGrass>();
        }
    }

    [CustomEditor(typeof(LBDefaultResources))]
    public class LBDefaultResourcesInspector : Editor
    {

        // Custom editor for LB Default Resources

        private string newTexturingPresetName = "Texturing Preset 1";
        private int selectedTexturingPreset;

        private string newTreePresetName = "Tree Preset 1";
        private int selectedTreePreset;

        private string newGrassPresetName = "Grass Preset 1";
        private int selectedGrassPreset;

        private GUIStyle helpBoxRichText;
        private GUIStyle labelFieldRichText;
        private Vector2 scrollPosition = Vector2.zero;
        private int selectedTabInt = 0;

        private int index;
        private int arrayInt;
        private int temp;

        // This function overides what is normally seen in the inspector window
        // This allows stuff like buttons to be drawn there
        public override void OnInspectorGUI()
        {
            LBDefaultResources resourcesScript = (LBDefaultResources)target;

            // Set up rich text GUIStyles
            helpBoxRichText = new GUIStyle("HelpBox");
            helpBoxRichText.richText = true;
            labelFieldRichText = new GUIStyle("Label");
            labelFieldRichText.richText = true;

            // Display the different tabs
            string[] tabTexts = { "Texturing", "Trees", "Grass" };
            selectedTabInt = GUILayout.SelectionGrid(selectedTabInt, tabTexts, 3);
            EditorGUILayout.Space();

            #region Texturing
            if (selectedTabInt == 0)
            {
                // Create lists if they don't already exist
                if (resourcesScript.texturingPresetNameList == null) { resourcesScript.texturingPresetNameList = new List<string>(); }
                if (resourcesScript.texturingPresetList == null) { resourcesScript.texturingPresetList = new List<LBTerrainTextureList>(); }

                // New preset interface
                newTexturingPresetName = EditorGUILayout.TextField("New Preset Name", newTexturingPresetName);
                if (!resourcesScript.texturingPresetNameList.Contains(newTexturingPresetName))
                {
                    if (GUILayout.Button("Add Texturing Preset"))
                    {
                        resourcesScript.texturingPresetNameList.Add(newTexturingPresetName);
                        resourcesScript.texturingPresetList.Add(new LBTerrainTextureList());
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Preset name already exists. You must provide a unique name to create a new texturing preset!", MessageType.Warning, true);
                }

                if (resourcesScript.texturingPresetNameList.Count > 0)
                {
                    selectedTexturingPreset = EditorGUILayout.Popup("Preset", selectedTexturingPreset, resourcesScript.texturingPresetNameList.ToArray());
                }

                if (resourcesScript.texturingPresetList.Count > selectedTexturingPreset)
                {
                    if (GUILayout.Button("Remove Texturing Preset"))
                    {
                        if (EditorUtility.DisplayDialog("Remove Preset?", "This action cannot be undone.", "Remove", "Cancel"))
                        {
                            resourcesScript.texturingPresetNameList.RemoveAt(selectedTexturingPreset);
                            resourcesScript.texturingPresetList.RemoveAt(selectedTexturingPreset);
                        }
                    }
                    else
                    {
                        List<LBTerrainTexture> terrainTexturesList = resourcesScript.texturingPresetList[selectedTexturingPreset].terrainTextureList;
                        // This code simulates the unity default array functionality, which editorgui can't do
                        arrayInt = terrainTexturesList.Count;
                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button("Add Texture")) { arrayInt++; }
                        if (GUILayout.Button("Remove Texture")) { arrayInt--; }
                        if (arrayInt < 0) { arrayInt = 0; }
                        GUILayout.EndHorizontal();
                        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                        // Add items to the list
                        if (arrayInt > terrainTexturesList.Count)
                        {
                            temp = arrayInt - terrainTexturesList.Count;
                            for (index = 0; index < temp; index++)
                            {
                                terrainTexturesList.Add(new LBTerrainTexture());
                            }
                        }
                        // Remove items from the list
                        else if (arrayInt < terrainTexturesList.Count)
                        {
                            temp = terrainTexturesList.Count - arrayInt;
                            for (index = 0; index < temp; index++)
                            {
                                terrainTexturesList.RemoveAt(terrainTexturesList.Count - 1);
                            }
                        }
                        for (index = 0; index < terrainTexturesList.Count; index++)
                        {
                            // Show the elements of the list
                            EditorGUILayout.Space();
                            EditorGUILayout.Space();
                            EditorGUILayout.LabelField("<color=black><b>Texture " + (index + 1) + "</b></color>", labelFieldRichText);
                            terrainTexturesList[index].texture = (Texture2D)EditorGUILayout.ObjectField("Texture" + GetTextureName(terrainTexturesList[index].texture), terrainTexturesList[index].texture, typeof(Texture2D), false);
                            terrainTexturesList[index].normalMap = (Texture2D)EditorGUILayout.ObjectField("Normal Map" + GetTextureName(terrainTexturesList[index].normalMap), terrainTexturesList[index].normalMap, typeof(Texture2D), false);
                            terrainTexturesList[index].tileSize = (Vector2)EditorGUILayout.Vector2Field("Tiling", terrainTexturesList[index].tileSize);
                            terrainTexturesList[index].metallic = EditorGUILayout.Slider("Metallic", terrainTexturesList[index].metallic, 0f, 1f);
                            terrainTexturesList[index].smoothness = EditorGUILayout.Slider("Smoothness", terrainTexturesList[index].smoothness, 0f, 1f);
                            terrainTexturesList[index].texturingMode = (LBTerrainTexture.TexturingMode)EditorGUILayout.EnumPopup("Texturing Mode", terrainTexturesList[index].texturingMode);
                            if (terrainTexturesList[index].texturingMode == LBTerrainTexture.TexturingMode.Height ||
                                terrainTexturesList[index].texturingMode == LBTerrainTexture.TexturingMode.HeightAndInclination ||
                                terrainTexturesList[index].texturingMode == LBTerrainTexture.TexturingMode.HeightInclinationCurvature ||
                                terrainTexturesList[index].texturingMode == LBTerrainTexture.TexturingMode.HeightInclinationMap)
                            {
                                terrainTexturesList[index].minHeight = EditorGUILayout.Slider(new GUIContent("Min Height", "Minimum height in metres that the texture will appear at"), terrainTexturesList[index].minHeight * 2000f, 0f, 2000f) / 2000f;
                                terrainTexturesList[index].maxHeight = EditorGUILayout.Slider(new GUIContent("Max Height", "Maximum height in metres that the texture will appear at"), terrainTexturesList[index].maxHeight * 2000f, 0f, 2000f) / 2000f;
                                if (terrainTexturesList[index].maxHeight < terrainTexturesList[index].minHeight)
                                {
                                    terrainTexturesList[index].maxHeight = terrainTexturesList[index].minHeight;
                                }
                            }
                            if (terrainTexturesList[index].texturingMode == LBTerrainTexture.TexturingMode.Inclination ||
                                terrainTexturesList[index].texturingMode == LBTerrainTexture.TexturingMode.HeightAndInclination ||
                                terrainTexturesList[index].texturingMode == LBTerrainTexture.TexturingMode.HeightInclinationCurvature ||
                                terrainTexturesList[index].texturingMode == LBTerrainTexture.TexturingMode.HeightInclinationMap)
                            {
                                terrainTexturesList[index].minInclination = EditorGUILayout.Slider(new GUIContent("Min Inclination", "Minimum inclination in degrees that the texture will appear at"), terrainTexturesList[index].minInclination, 0f, 90f);
                                terrainTexturesList[index].maxInclination = EditorGUILayout.Slider(new GUIContent("Max Inclination", "Maximum inclination in degrees that the texture will appear at"), terrainTexturesList[index].maxInclination, 0f, 90f);
                                if (terrainTexturesList[index].maxInclination < terrainTexturesList[index].minInclination)
                                {
                                    terrainTexturesList[index].maxInclination = terrainTexturesList[index].minInclination;
                                }
                            }

                            //terrainTexturesList[index].isCurvatureConcave = false;

                            if (terrainTexturesList[index].texturingMode == LBTerrainTexture.TexturingMode.Map ||
                                terrainTexturesList[index].texturingMode == LBTerrainTexture.TexturingMode.HeightInclinationMap)
                            {
                                terrainTexturesList[index].map = (Texture2D)EditorGUILayout.ObjectField("Map" + GetTextureName(terrainTexturesList[index].map), terrainTexturesList[index].map, typeof(Texture2D), false);
                                terrainTexturesList[index].mapColour = EditorGUILayout.ColorField("Colour", terrainTexturesList[index].mapColour);
                                terrainTexturesList[index].mapTolerance = EditorGUILayout.IntSlider(new GUIContent("Tolerance", "Colour tolerance on the map to allow for blended edges"), terrainTexturesList[index].mapTolerance, 0, 20);
                                terrainTexturesList[index].mapInverse = EditorGUILayout.Toggle(new GUIContent("Inverse", "The texture will not appear in the map areas unless Tolerance allows blended edges"), terrainTexturesList[index].mapInverse);
                            }
                            terrainTexturesList[index].useNoise = EditorGUILayout.Toggle(new GUIContent("Use Noise", "Whether noise will be used to add variation to the texturing"), terrainTexturesList[index].useNoise);
                            if (terrainTexturesList[index].useNoise)
                            {
                                terrainTexturesList[index].isMinimalBlendingEnabled = EditorGUILayout.Toggle(new GUIContent("Minimal Blending", "The strongest texture will rendered but edge blending will still take place"), terrainTexturesList[index].isMinimalBlendingEnabled);
                                terrainTexturesList[index].noiseTileSize = EditorGUILayout.Slider(new GUIContent("Noise Tile Size", "Scaling of the noise on the x-z plane"), terrainTexturesList[index].noiseTileSize, 50f, 1000f);
                            }
                            else
                            {
                                // Minimal Blending is always disabled when Noise is disabled.
                                terrainTexturesList[index].isMinimalBlendingEnabled = false;
                            }

                            terrainTexturesList[index].strength = EditorGUILayout.Slider(new GUIContent("Strength", "Strength of the texture in the splatmap"), terrainTexturesList[index].strength, 0f, 2f);
                        }

                        resourcesScript.texturingPresetList[selectedTexturingPreset].terrainTextureList = terrainTexturesList;

                        EditorGUILayout.EndScrollView();
                    }
                }
            }
            #endregion

            #region Trees
            else if (selectedTabInt == 1)
            {
                // Create lists if they don't already exist
                if (resourcesScript.treePresetNameList == null) { resourcesScript.treePresetNameList = new List<string>(); }
                if (resourcesScript.treePresetList == null) { resourcesScript.treePresetList = new List<LBTerrainTreeList>(); }

                // New preset interface
                newTreePresetName = EditorGUILayout.TextField("New Preset Name", newTreePresetName);
                if (!resourcesScript.treePresetNameList.Contains(newTreePresetName))
                {
                    if (GUILayout.Button("Add Tree Preset"))
                    {
                        resourcesScript.treePresetNameList.Add(newTreePresetName);
                        resourcesScript.treePresetList.Add(new LBTerrainTreeList());
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Preset name already exists. You must provide a unique name to create a new tree preset!", MessageType.Warning, true);
                }

                if (resourcesScript.treePresetNameList.Count > 0)
                {
                    selectedTreePreset = EditorGUILayout.Popup("Preset", selectedTreePreset, resourcesScript.treePresetNameList.ToArray());
                }

                if (resourcesScript.treePresetList.Count > selectedTreePreset)
                {
                    if (GUILayout.Button("Remove Tree Preset"))
                    {
                        if (EditorUtility.DisplayDialog("Remove Preset?", "This action cannot be undone.", "Remove", "Cancel"))
                        {
                            resourcesScript.treePresetNameList.RemoveAt(selectedTreePreset);
                            resourcesScript.treePresetList.RemoveAt(selectedTreePreset);
                        }
                    }
                    else
                    {
                        List<LBTerrainTree> terrainTreesList = resourcesScript.treePresetList[selectedTreePreset].terrainTreeList;
                        // This code simulates the unity default array functionality, which editorgui can't do
                        arrayInt = terrainTreesList.Count;
                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button("Add Tree Type")) { arrayInt++; }
                        if (GUILayout.Button("Remove Tree Type")) { arrayInt--; }
                        if (arrayInt < 0) { arrayInt = 0; }
                        GUILayout.EndHorizontal();
                        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                        // Add items to the list
                        if (arrayInt > terrainTreesList.Count)
                        {
                            temp = arrayInt - terrainTreesList.Count;
                            for (index = 0; index < temp; index++)
                            {
                                terrainTreesList.Add(new LBTerrainTree());
                            }
                        }
                        // Remove items from the list
                        else if (arrayInt < terrainTreesList.Count)
                        {
                            temp = terrainTreesList.Count - arrayInt;
                            for (index = 0; index < temp; index++)
                            {
                                terrainTreesList.RemoveAt(terrainTreesList.Count - 1);
                            }
                        }
                        for (index = 0; index < terrainTreesList.Count; index++)
                        {
                            // Show the elements of the list
                            EditorGUILayout.Space();
                            EditorGUILayout.Space();
                            EditorGUILayout.LabelField("<color=black><b>Tree Type " + (index + 1) + "</b></color>", labelFieldRichText);
                            terrainTreesList[index].prefab = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Prefab", "Tree prefab to use"), terrainTreesList[index].prefab, typeof(GameObject), false);
                            terrainTreesList[index].bendFactor = EditorGUILayout.Slider(new GUIContent("Bend Factor", "Bend factor of the tree - must be non-zero for it to be affected by a wind zone"), terrainTreesList[index].bendFactor, 0f, 1f);
                            terrainTreesList[index].minScale = EditorGUILayout.Slider(new GUIContent("Min Scale", "Minimum scale (relative to the prefab) of each placed tree"), terrainTreesList[index].minScale, 0.1f, 10f);
                            terrainTreesList[index].maxScale = EditorGUILayout.Slider(new GUIContent("Max Scale", "Maximum scale (relative to the prefab) of each placed tree"), terrainTreesList[index].maxScale, 0.1f, 10f);
                            terrainTreesList[index].minProximity = EditorGUILayout.Slider(new GUIContent("Min Proximity", "Minimum distance this tree can be from any other tree"), terrainTreesList[index].minProximity, 0f, 100f);
                            if (terrainTreesList[index].maxScale < terrainTreesList[index].minScale)
                            {
                                terrainTreesList[index].maxScale = terrainTreesList[index].minScale;
                            }
                            terrainTreesList[index].treeScalingMode = (LBTerrainTree.TreeScalingMode)EditorGUILayout.EnumPopup("Tree Scaling Mode", terrainTreesList[index].treeScalingMode);
                            if (terrainTreesList[index].treeScalingMode == LBTerrainTree.TreeScalingMode.RandomScaling)
                            {
                                terrainTreesList[index].lockWidthToHeight = EditorGUILayout.Toggle(new GUIContent("Lock Width To Height", "Whether the width to height ratio is locked to 1:1"), terrainTreesList[index].lockWidthToHeight);
                            }
                            terrainTreesList[index].treePlacingMode = (LBTerrainTree.TreePlacingMode)EditorGUILayout.EnumPopup("Tree Placing Mode", terrainTreesList[index].treePlacingMode);
                            if (terrainTreesList[index].treePlacingMode == LBTerrainTree.TreePlacingMode.Height ||
                                terrainTreesList[index].treePlacingMode == LBTerrainTree.TreePlacingMode.HeightAndInclination ||
                                terrainTreesList[index].treePlacingMode == LBTerrainTree.TreePlacingMode.HeightInclinationCurvature ||
                                terrainTreesList[index].treePlacingMode == LBTerrainTree.TreePlacingMode.HeightInclinationMap)
                            {
                                terrainTreesList[index].minHeight = EditorGUILayout.Slider(new GUIContent("Min Height", "Minimum height in metres that the tree can be placed at"), terrainTreesList[index].minHeight * 2000f, 0f, 2000f) / 2000f;
                                terrainTreesList[index].maxHeight = EditorGUILayout.Slider(new GUIContent("Max Height", "Maximum height in metres that the tree can be placed at"), terrainTreesList[index].maxHeight * 2000f, 0f, 2000f) / 2000f;
                                if (terrainTreesList[index].maxHeight < terrainTreesList[index].minHeight)
                                {
                                    terrainTreesList[index].maxHeight = terrainTreesList[index].minHeight;
                                }
                            }
                            if (terrainTreesList[index].treePlacingMode == LBTerrainTree.TreePlacingMode.Inclination ||
                                terrainTreesList[index].treePlacingMode == LBTerrainTree.TreePlacingMode.HeightAndInclination ||
                                terrainTreesList[index].treePlacingMode == LBTerrainTree.TreePlacingMode.HeightInclinationCurvature ||
                                terrainTreesList[index].treePlacingMode == LBTerrainTree.TreePlacingMode.HeightInclinationMap)
                            {
                                terrainTreesList[index].minInclination = EditorGUILayout.Slider(new GUIContent("Min Inclination", "Minimum inclination in degrees that the tree can be placed at"), terrainTreesList[index].minInclination, 0f, 90f);
                                terrainTreesList[index].maxInclination = EditorGUILayout.Slider(new GUIContent("Max Inclination", "Maximum inclination in degrees that the tree can be placed at"), terrainTreesList[index].maxInclination, 0f, 90f);
                                if (terrainTreesList[index].maxInclination < terrainTreesList[index].minInclination)
                                {
                                    terrainTreesList[index].maxInclination = terrainTreesList[index].minInclination;
                                }
                            }

                            if (terrainTreesList[index].treePlacingMode == LBTerrainTree.TreePlacingMode.Map ||
                               terrainTreesList[index].treePlacingMode == LBTerrainTree.TreePlacingMode.HeightInclinationMap)
                            {
                                terrainTreesList[index].map = (Texture2D)EditorGUILayout.ObjectField("Map" + GetTextureName(terrainTreesList[index].map), terrainTreesList[index].map, typeof(Texture2D), false);
                                terrainTreesList[index].mapColour = EditorGUILayout.ColorField("Colour", terrainTreesList[index].mapColour);
                                terrainTreesList[index].mapTolerance = EditorGUILayout.IntSlider(new GUIContent("Tolerance", "Colour tolerance on the map to allow for blended edges"), terrainTreesList[index].mapTolerance, 0, 20);
                                terrainTreesList[index].mapInverse = EditorGUILayout.Toggle(new GUIContent("Inverse", "Trees will not appear in the map areas"), terrainTreesList[index].mapInverse);
                            }


                            terrainTreesList[index].useNoise = EditorGUILayout.Toggle(new GUIContent("Use Noise", "Whether noise will be used to add variation to the tree placement"), terrainTreesList[index].useNoise);
                            if (terrainTreesList[index].useNoise)
                            {
                                terrainTreesList[index].noiseTileSize = EditorGUILayout.Slider(new GUIContent("Noise Tile Size", "Scaling of the noise on the x-z plane"), terrainTreesList[index].noiseTileSize, 100f, 10000f);
                                terrainTreesList[index].treePlacementCutoff = EditorGUILayout.Slider(new GUIContent("Tree Placement Cutoff", "The noise cutoff value for tree placement. Increasing this value will mean more trees are placed"), terrainTreesList[index].treePlacementCutoff, 0.1f, 1f);
                            }
                        }

                        resourcesScript.treePresetList[selectedTreePreset].terrainTreeList = terrainTreesList;

                        EditorGUILayout.EndScrollView();
                    }
                }
            }
            #endregion

            #region Grass
            else if (selectedTabInt == 2)
            {
                // Create lists if they don't already exist
                if (resourcesScript.grassPresetNameList == null) { resourcesScript.grassPresetNameList = new List<string>(); }
                if (resourcesScript.grassPresetList == null) { resourcesScript.grassPresetList = new List<LBTerrainGrassList>(); }

                // New preset interface
                newGrassPresetName = EditorGUILayout.TextField("New Preset Name", newGrassPresetName);
                if (!resourcesScript.grassPresetNameList.Contains(newGrassPresetName))
                {
                    if (GUILayout.Button("Add Grass Preset"))
                    {
                        resourcesScript.grassPresetNameList.Add(newGrassPresetName);
                        resourcesScript.grassPresetList.Add(new LBTerrainGrassList());
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Preset name already exists. You must provide a unique name to create a new grass preset!", MessageType.Warning, true);
                }

                if (resourcesScript.grassPresetNameList.Count > 0)
                {
                    selectedGrassPreset = EditorGUILayout.Popup("Preset", selectedGrassPreset, resourcesScript.grassPresetNameList.ToArray());
                }

                if (resourcesScript.grassPresetList.Count > selectedGrassPreset)
                {
                    if (GUILayout.Button("Remove Grass Preset"))
                    {
                        if (EditorUtility.DisplayDialog("Remove Preset?", "This action cannot be undone.", "Remove", "Cancel"))
                        {
                            resourcesScript.grassPresetNameList.RemoveAt(selectedGrassPreset);
                            resourcesScript.grassPresetList.RemoveAt(selectedGrassPreset);
                        }
                    }
                    else
                    {
                        List<LBTerrainGrass> terrainGrassList = resourcesScript.grassPresetList[selectedGrassPreset].terrainGrassList;
                        // This code simulates the unity default array functionality, which editorgui can't do
                        arrayInt = terrainGrassList.Count;
                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button("Add Grass Type")) { arrayInt++; }
                        if (GUILayout.Button("Remove Grass Type")) { arrayInt--; }
                        if (arrayInt < 0) { arrayInt = 0; }
                        GUILayout.EndHorizontal();
                        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                        // Add items to the list
                        if (arrayInt > terrainGrassList.Count)
                        {
                            temp = arrayInt - terrainGrassList.Count;
                            for (index = 0; index < temp; index++)
                            {
                                terrainGrassList.Add(new LBTerrainGrass());
                            }
                        }
                        // Remove items from the list
                        else if (arrayInt < terrainGrassList.Count)
                        {
                            temp = terrainGrassList.Count - arrayInt;
                            for (index = 0; index < temp; index++)
                            {
                                terrainGrassList.RemoveAt(terrainGrassList.Count - 1);
                            }
                        }

                        // Calculate the collective grass density
                        int collectiveGrassDensity = 0;
                        for (index = 0; index < terrainGrassList.Count; index++) { collectiveGrassDensity += terrainGrassList[index].density; }
                        int possibleTotalGrassDensity = collectiveGrassDensity * Mathf.RoundToInt((2000f / 1000f) * (2000f / 1000f));
                        int possiblePatchVertices = possibleTotalGrassDensity * 32 * 16;
                        if (possiblePatchVertices > 60000f)
                        {
                            // Show warning if grass density settings may cause detail object vertices per patch limit to be exceeded
                            int maxPossibleDensity = Mathf.FloorToInt(60000f / 32f / 16f / Mathf.RoundToInt((2000f / 1000f) * (2000f / 1000f)));
                            EditorGUILayout.HelpBox("With your current grass density settings (combined density of " + collectiveGrassDensity +
                                                    "), it is possible that some grass patches may exceed the detail object vertices per patch" +
                                                    " limit (65k), meaning that some grass billboards won't be rendered. Using a maximum" +
                                                    " combined density of " + maxPossibleDensity + " will greatly reduce the chances of" +
                                                    " encountering this issue.", MessageType.Warning);
                        }

                        for (index = 0; index < terrainGrassList.Count; index++)
                        {
                            // Show the elements of the list
                            EditorGUILayout.Space();
                            EditorGUILayout.Space();
                            EditorGUILayout.LabelField("<color=black><b>Grass Type " + (index + 1) + "</b></color>", labelFieldRichText);

                            terrainGrassList[index].texture = (Texture2D)EditorGUILayout.ObjectField("Texture" + GetTextureName(terrainGrassList[index].texture), terrainGrassList[index].texture, typeof(Texture2D), false);
                            terrainGrassList[index].detailRenderMode = (DetailRenderMode)EditorGUILayout.EnumPopup("Grass Rendering Mode", terrainGrassList[index].detailRenderMode);
                            if (terrainGrassList[index].detailRenderMode == DetailRenderMode.VertexLit)
                            {
                                terrainGrassList[index].detailRenderMode = DetailRenderMode.Grass;
                                Debug.Log("Vertex-Lit is not currently available as a Grass Rendering Mode. If you would like to place meshes" +
                                          " in your landscape, use the Mesh tab.");
                            }
                            terrainGrassList[index].grassPatchFadingMode = (LBTerrainGrass.GrassPatchFadingMode)EditorGUILayout.EnumPopup("Grass Patch Fading Mode", terrainGrassList[index].grassPatchFadingMode);
                            terrainGrassList[index].healthyColour = EditorGUILayout.ColorField(new GUIContent("Healthy Colour", "The tint colour of the grass when it is 'healthy'"), terrainGrassList[index].healthyColour);
                            terrainGrassList[index].dryColour = EditorGUILayout.ColorField(new GUIContent("Dry Colour", "The tint colour of the grass when it is 'dry'"), terrainGrassList[index].dryColour);
                            terrainGrassList[index].minHeight = EditorGUILayout.Slider(new GUIContent("Min Grass Height", "The minimum height of the grass in metres"), terrainGrassList[index].minHeight, 0.1f, 8f);
                            terrainGrassList[index].maxHeight = EditorGUILayout.Slider(new GUIContent("Max Grass Height", "The maximum height of the grass in metres"), terrainGrassList[index].maxHeight, 0.1f, 8f);
                            if (terrainGrassList[index].maxHeight < terrainGrassList[index].minHeight)
                            {
                                terrainGrassList[index].maxHeight = terrainGrassList[index].minHeight;
                            }
                            terrainGrassList[index].minWidth = EditorGUILayout.Slider(new GUIContent("Min Grass Width", "The minimum width of the grass in metres"), terrainGrassList[index].minWidth, 0.1f, 8f);
                            terrainGrassList[index].maxWidth = EditorGUILayout.Slider(new GUIContent("Max Grass Width", "The maximum width of the grass in metres"), terrainGrassList[index].maxWidth, 0.1f, 8f);
                            if (terrainGrassList[index].maxWidth < terrainGrassList[index].minWidth)
                            {
                                terrainGrassList[index].maxWidth = terrainGrassList[index].minWidth;
                            }
                            terrainGrassList[index].density = EditorGUILayout.IntSlider(new GUIContent("Density", "How densely packed the grass is"), terrainGrassList[index].density, 1, 15);
                            terrainGrassList[index].grassPlacingMode = (LBTerrainGrass.GrassPlacingMode)EditorGUILayout.EnumPopup("Grass Populating Mode", terrainGrassList[index].grassPlacingMode);
                            if (terrainGrassList[index].grassPlacingMode == LBTerrainGrass.GrassPlacingMode.Height ||
                                terrainGrassList[index].grassPlacingMode == LBTerrainGrass.GrassPlacingMode.HeightAndInclination ||
                                terrainGrassList[index].grassPlacingMode == LBTerrainGrass.GrassPlacingMode.HeightInclinationCurvature ||
                                terrainGrassList[index].grassPlacingMode == LBTerrainGrass.GrassPlacingMode.HeightInclinationMap)
                            {
                                terrainGrassList[index].minPopulatedHeight = EditorGUILayout.Slider(new GUIContent("Min Height", "Minimum height in metres that the grass can be placed at"), terrainGrassList[index].minPopulatedHeight * 2000f, 0f, 2000f) / 2000f;
                                terrainGrassList[index].maxPopulatedHeight = EditorGUILayout.Slider(new GUIContent("Max Height", "Maximum height in metres that the grass can be placed at"), terrainGrassList[index].maxPopulatedHeight * 2000f, 0f, 2000f) / 2000f;
                                if (terrainGrassList[index].maxPopulatedHeight < terrainGrassList[index].minPopulatedHeight)
                                {
                                    terrainGrassList[index].maxPopulatedHeight = terrainGrassList[index].minPopulatedHeight;
                                }
                            }
                            if (terrainGrassList[index].grassPlacingMode == LBTerrainGrass.GrassPlacingMode.Inclination ||
                                terrainGrassList[index].grassPlacingMode == LBTerrainGrass.GrassPlacingMode.HeightAndInclination ||
                                terrainGrassList[index].grassPlacingMode == LBTerrainGrass.GrassPlacingMode.HeightInclinationCurvature ||
                                terrainGrassList[index].grassPlacingMode == LBTerrainGrass.GrassPlacingMode.HeightInclinationMap)
                            {
                                terrainGrassList[index].minInclination = EditorGUILayout.Slider(new GUIContent("Min Inclination", "Minimum inclination in degrees that the grass can be placed at"), terrainGrassList[index].minInclination, 0f, 90f);
                                terrainGrassList[index].maxInclination = EditorGUILayout.Slider(new GUIContent("Max Inclination", "Maximum inclination in degrees that the grass can be placed at"), terrainGrassList[index].maxInclination, 0f, 90f);
                                if (terrainGrassList[index].maxInclination < terrainGrassList[index].minInclination)
                                {
                                    terrainGrassList[index].maxInclination = terrainGrassList[index].minInclination;
                                }
                            }

                            if (terrainGrassList[index].grassPlacingMode == LBTerrainGrass.GrassPlacingMode.Map ||
                                terrainGrassList[index].grassPlacingMode == LBTerrainGrass.GrassPlacingMode.HeightInclinationMap)
                            {
                                terrainGrassList[index].map = (Texture2D)EditorGUILayout.ObjectField("Map" + GetTextureName(terrainGrassList[index].map), terrainGrassList[index].map, typeof(Texture2D), false);
                                terrainGrassList[index].mapColour = EditorGUILayout.ColorField("Colour", terrainGrassList[index].mapColour);
                                terrainGrassList[index].mapTolerance = EditorGUILayout.IntSlider(new GUIContent("Tolerance", "Colour tolerance on the map to allow for blended edges"), terrainGrassList[index].mapTolerance, 0, 20);
                                terrainGrassList[index].mapInverse = EditorGUILayout.Toggle(new GUIContent("Inverse", "Grass will not appear in the map areas"), terrainGrassList[index].mapInverse);
                            }
                        }

                        resourcesScript.grassPresetList[selectedGrassPreset].terrainGrassList = terrainGrassList;

                        EditorGUILayout.EndScrollView();
                    }
                }
            }
            #endregion
        }

        private string GetTextureName(Texture2D texture)
        {
            if (texture != null) { return " (" + texture.name + ")"; }
            else { return string.Empty; }
        }
    }
}
#endif