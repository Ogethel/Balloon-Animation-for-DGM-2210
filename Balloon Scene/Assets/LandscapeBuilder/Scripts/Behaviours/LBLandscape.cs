// Landscape Builder. Copyright (c) 2016-2019 SCSM Pty Ltd. All rights reserved.
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System;
#endif

// Keep LBUndo and LBSavedData classes outside the namespace to ensure backward compatibility with LB v1.x

#region LBUndo Class
// Class for recording undo data
[System.Serializable]
class LBUndo
{
    #if UNITY_EDITOR
    // For recording topography data
    public float[,] heightMap;
    // Currently only used for LBObjPath
    public float[] heightMap1D;
    // Currently only used for LBObjPath
    public float[] splatMaps1D;
    // Currently only used for LBObjPath
    public List<LandscapeBuilder.LBTerrainTreeInstanceBin> trees1D;
    #endif
}
#endregion

#region LBSavedData Class
// Class for recording saved data
[System.Serializable]
public class LBSavedData
{
    #if UNITY_EDITOR
    public enum PathType
    {
        HQPhotographicTexturesVol1 = 0,
        HQPhotographicTexturesVol2 = 1,
        FlowMapPainterExe = 2,
        RusticGrass = 3
    }

    // For recording the state of AutoSave
    public bool autoSaveEnabled;

    public string lastLandscapeGameObjectSelectionName;

    // On the Texture tab, this allows the user to temporarily disable all the textures and then Texture Landscape
    // which effectively removes all texture without removing all the Texture configurations
    public bool disableAllTextures;

    public string pathHQPhotographicTexturesVol1;
    public string pathHQPhotographicTexturesVol2;
    public string pathRusticGrass;

    public static string GetHQPhotographicTexturesVol1DefaultPath { get { return "HQ Photographic Textures Grass Pack Vol.1"; } }
    public static string GetHQPhotographicTexturesVol2DefaultPath { get { return "HQ Photographic Textures Grass Pack Vol.2"; } }
    public static string GetRusticGrassDefaultPath { get { return "RUSTIC Grass"; } }

    public static string GetHQPhotographicTexturesVol1SourceName { get { return "HQ Photographic Textures Grass Pack Vol.1"; } }
    public static string GetHQPhotographicTexturesVol2SourceName { get { return "HQ Photographic Textures Grass Pack Vol.2"; } }
    public static string GetRusticGrassSourceName { get { return "RUSTIC Grass"; } }

    public string pathFlowMapPainterEXE;

    // MegaSplat options
    public bool megaSplatAutoClosePainter;

    public bool isNonSquareTerrainsEnabled;
    public bool isLegacyNoiseOffset;

    // Constructor
    public LBSavedData()
    {
        autoSaveEnabled = true;
        disableAllTextures = false;
        pathHQPhotographicTexturesVol1 = "HQ Photographic Textures Grass Pack Vol.1";
        pathHQPhotographicTexturesVol2 = "HQ Photographic Textures Grass Pack Vol.2";
        pathRusticGrass = "RUSTIC Grass";
        pathFlowMapPainterEXE = string.Empty;
        megaSplatAutoClosePainter = true;
        isNonSquareTerrainsEnabled = false;
        isLegacyNoiseOffset = false;
    }
    #endif
}
#endregion

namespace LandscapeBuilder
{
    [ExecuteInEditMode]
    [HelpURL("http://scsmmedia.com/media/Landscape%20Builder.pdf")]
    public class LBLandscape : MonoBehaviour
    {
        #region Enumerations

        // This is duplicated in LBTemplate.
        public enum TerrainMaterialType
        {
            BuiltInStandard = 0,
            BuiltInLegacyDiffuse = 1,
            BuiltInLegacySpecular = 2,
            LBStandard = 3,
            MegaSplat = 7,
            MicroSplat = 8,
            ReliefTerrainPack = 10,
            LWRP = 11,
            HDRP = 12,
            URP = 17,
            Custom = 20
        }

        public enum UndoType
        {
            HeightMap = 0,
		    Textures = 1,
		    Trees = 2,
            Grass = 3
        }

        public enum MaskMode
        {
            None = 0,
            DistanceToCentre = 1,
            Noise = 2
        }

        public enum ThermalErosionPreset
        {
            ExtremeSoilSlippage = 10,
            SlowSoilSlippage = 20,
            FastSoilSlippage = 23,
            SlowWeathering = 30,
            FastWeathering = 33,
            SoftEarthSlippage = 40,
            HardEarthSlippage = 50
        }

        #endregion

        #region Private Variables
    
	    // Saved data
	    private static LBSavedData tempLBSavedData = new LBSavedData();
	    private static LBSavedData lbSavedData = new LBSavedData();
        // Undo feature
        private LBUndo lbUndo = new LBUndo();
	    // For recording tree data
	    private List<TreePrototype[]> undoTreePrototypes;
	    private List<TreeInstance[]> undoTreeInstances;
	    // For recording texturing data
        #if UNITY_2018_3_OR_NEWER
        private List<TerrainLayer[]> undoTerrainLayers;
        #else
        private List<SplatPrototype[]> undoSplatPrototypes;
        #endif
	    private List<float[,,]> undoAlphamaps;
	    // For recording grass data
	    private List<DetailPrototype[]> undoDetailPrototypes;
	    private List<List<int[,]>> undoDetailLayerLists;

        /// <summary>
        /// This is a temporary cached variable of the terrain height
        /// Update it before use.
        /// </summary>
        private float landscapeHeight = 0f;

        #endregion

        #region Public Variables
        [System.NonSerialized] public Terrain[] landscapeTerrains;

        [HideInInspector]
        public List<LBLayer> topographyLayersList;
	    [HideInInspector] public List<LBTerrainTexture> terrainTexturesList;
	    [HideInInspector] public List<LBTerrainTree> terrainTreesList;
	    [HideInInspector] public List<LBTerrainGrass> terrainGrassList;
	    [HideInInspector] public List<LBLandscapeMesh> landscapeMeshList;
        [HideInInspector] public List<LBWater> landscapeWaterList;
        [HideInInspector] public List<LBGroup> lbGroupList;

	    [HideInInspector] public Vector2 size = Vector2.zero;
	    [HideInInspector] public Vector3 start = Vector3.zero;

        // Topography Mask (one per landscape)
        [HideInInspector] public MaskMode topographyMaskMode = MaskMode.None;
        [HideInInspector] public AnimationCurve distanceToCentreMask = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        [HideInInspector] public float maskWarpAmount = 0f;
        [HideInInspector] public float maskNoiseTileSize = 2000f;
        [HideInInspector] public float maskNoiseOffsetX = 0f;
        [HideInInspector] public float maskNoiseOffsetY = 0f;
        [HideInInspector] public AnimationCurve maskNoiseCurveModifier = AnimationCurve.Linear(0f, 0.5f, 1f, 1f);
   
        /// <summary>
        /// Needs to remain true by default for backward compatibility with legacy terrains.
        /// New terrains can have it disabled.
        /// </summary>
        [HideInInspector] public bool useLegacyNoiseOffset = true;

        [HideInInspector] public Material LBStandardTerrainMaterial;
        [HideInInspector] public Material terrainCustomMaterial;

        // The version this landscape was last updated with
        [HideInInspector] public string LastUpdatedVersion = "1.0";

        // Modifier settings for landscape
        [HideInInspector] public bool modifierAutoDisableHightlighter = true;

        // Remember Tree settings for landscape
        [HideInInspector] public LBTerrainTree.TreePlacementSpeed treePlacementSpeed = LBTerrainTree.TreePlacementSpeed.FastPlacement;
        [HideInInspector] public bool treesHaveColliders = false;
        [HideInInspector] public int maxTreesPerSquareKilometre = 1000;

        // Mesh counts
        [HideInInspector] public LBLandscapeMesh.MeshPlacementSpeed meshPlacementSpeed = LBLandscapeMesh.MeshPlacementSpeed.FastPlacement;
        [HideInInspector] public int numberOfMeshPrefabs = 0;
        [HideInInspector] public int numberOfMeshes = 0;

        // Group prefab counts
        [HideInInspector] public int numberOfGroupPrefabs = 0;
        [HideInInspector] public bool autoRefreshGroupDesigner = false;  // Off by default

        // uNature 1.x integration variables
        [HideInInspector] public bool useuNature = false;
        [HideInInspector] public string uNatureProjectVersion = string.Empty;
        [HideInInspector] public int uNaturePoolAmount = 15;
        [HideInInspector] public string uNaturePoolTypeName = "None";
        [HideInInspector] public int uNatureSectorResolution = 10;
        [HideInInspector] public bool uNatureSectorGrassManagement = false;
        [HideInInspector] public bool uNatureTreeManagement = false;
        [HideInInspector] public string uNatureGrassPresetName = string.Empty;

        [HideInInspector] public bool showTiming = false;

        // Added v1.3.2 Beta 1c
        // A Texture Heightmap can also use an Occlusion Map texture if the first is not available
        [HideInInspector] public bool showTextureHeightmap = false;
        [HideInInspector] public bool rtpUseTessellation = false;

        // Final Pass Smoothing
        // Added v1.4.4 Beta 6c
        [HideInInspector] public bool useFinalPassSmoothing = false;
        [HideInInspector] public int finalPassSmoothingIterations = 1;
        [HideInInspector] public int finalPassPixelRange = 1;
        // Added v2.0.6 Beta 4s
        [HideInInspector] public string fPassSmoothStencilGUID = string.Empty;
        [HideInInspector] public string fPassSmoothStencilLayerGUID = string.Empty;
        [HideInInspector] public LBFilter.FilterMode fPassSmoothFilterMode = LBFilter.FilterMode.AND;
        // Temp storage for display in LandscapeBuilderWindow Editor and LBLandscapeTerrain.FinalPassHeightmap()
        [HideInInspector] [System.NonSerialized] public LBStencil fPassSmoothlbStencil = null;
        // Temp storage for use in LBLandscapeTerrain.FinalPassHeightmap()
        [HideInInspector] [System.NonSerialized] public LBStencilLayer fPassSmoothlbStencilLayer = null;
        [HideInInspector] [System.NonSerialized] public int fPassSmoothStencilLayerRes = 0;
        
        // Final Pass Erosion
        // Added v2.0.0 Beta 5j
        [HideInInspector] public ThermalErosionPreset thermalErosionPreset = ThermalErosionPreset.SoftEarthSlippage;
        [HideInInspector] public bool useThermalErosion = false;
        [HideInInspector] public int thermalErosionIterations = 50;
        [HideInInspector] public float thermalErosionTalusAngle = 45f;
        [HideInInspector] public float thermalErosionStrength = 0.2f;
        // Added v2.0.6 Beta 4t
        [HideInInspector] public string fPassThErosionStencilGUID = string.Empty;
        [HideInInspector] public string fPassThErosionStencilLayerGUID = string.Empty;
        [HideInInspector] public LBFilter.FilterMode fPassThErosionFilterMode = LBFilter.FilterMode.AND;
        // Temp storage for display in LandscapeBuilderWindow Editor and LBLandscapeTerrain.FinalPassHeightmap()
        [HideInInspector] [System.NonSerialized] public LBStencil fPassThErosionlbStencil = null;
        // Temp storage for use in LBLandscapeTerrain.FinalPassHeightmap()
        [HideInInspector] [System.NonSerialized] public LBStencilLayer fPassThErosionlbStencilLayer = null;
        [HideInInspector] [System.NonSerialized] public int fPassThErosionStencilLayerRes = 0;

        // Vegetation Studio Integration variables (added 2.0.2)
        [HideInInspector] public bool useVegetationSystem = false;          // Will automatically add VegetationSystem to each terrain.
        [HideInInspector] public bool useVegetationSystemTextures = false;  // Will LB textures be applied using Vegetation System?
        [HideInInspector] public List<Camera> vegetationStudioCameraList;   // List of cameras used for assigning to Vegetation System scripts for each terrain

        // Landscape Extension - a plane under the landscape that extends towards horizon
        [HideInInspector] public bool useLandscapeExtension = false;
        [HideInInspector] public LBLandscapeExtension lbLandscapeExtension;

        // Added v2.1.2 Beta 2a
        // Store the terraindata in a project folder rather than the scene
        [HideInInspector] public bool useProjectForTerrainData = false;
        // The folder in the project folder. Stored without the Assets/ prefix.
        [HideInInspector] public string terrainDataFolder = string.Empty;

        // Added v2.1.4 Beta 1b
        // UnityEngine.TerrainLayer was introduced in 2018.3 with New Terrain System.
        // When useProjectForTerrainData is true, create TerrainLayers in the project
        // folder and reference in LBLandscapeTerrain.TextureTerrain(..).
        #if UNITY_2018_3_OR_NEWER && UNITY_EDITOR
        [HideInInspector] public List<TerrainLayer> activeTerrainLayerList;
        #endif

        // GPU Acceleration (Experimental)
        [HideInInspector] public bool useGPUTexturing = false;
        [HideInInspector] public bool useGPUTopography = false;
        [HideInInspector] public bool useGPUGrass = false;
        [HideInInspector] public bool useGPUPath = false;

        // Undo Disabled (Experimental when true). Backups on very large landscapes (256+ terrain)
        // is very slow. Avoiding the backup phase can improve operations at the risk of loosing data
        [HideInInspector] public bool isUndoTopographyDisabled = false;

        #endregion

        #region Public Properties
        public int GetLastUpdateMajorVersion
        {
            get
            {
                int numValue = 0;
                if (!string.IsNullOrEmpty(LastUpdatedVersion))
                {

                    int firstDot = LastUpdatedVersion.IndexOf(".", 0);

                    // First dot must exist and not be in first position
                    if (firstDot > 0)
                    {
                        int result;
                        if (int.TryParse(LastUpdatedVersion.Substring(firstDot - 1, firstDot), out result)) { numValue = result; }
                    }
                }
                return numValue;
            }
        }

        public int GetLastUpdateMinorVersion
        {
            get
            {
                int numValue = 0;
                if (!string.IsNullOrEmpty(LastUpdatedVersion))
                {
                    int firstDot = LastUpdatedVersion.IndexOf(".", 0);

                    // First dot must exist and not be in first position
                    if (firstDot > 0)
                    {
                        int result;
                        int secondDot = LastUpdatedVersion.IndexOf(".", firstDot + 1);
                        if (secondDot == -1)
                        {
                            // e.g. 1.3  (not tested)
                            if (int.TryParse(LastUpdatedVersion.Substring(firstDot + 1), out result)) { numValue = result; }
                        }
                        else
                        {
                            // e.g. 1.4.2
                            if (int.TryParse(LastUpdatedVersion.Substring(firstDot + 1, secondDot - firstDot - 1), out result)) { numValue = result; }
                        }
                    }
                }
                return numValue;
            }
        }

        public int GetLastUpdatePatchVersion
        {
            get
            {
                int numValue = 0;
                if (!string.IsNullOrEmpty(LastUpdatedVersion))
                {
                    int firstDot = LastUpdatedVersion.IndexOf(".", 0);

                    // First dot must exist and not be in first position
                    if (firstDot > 0)
                    {
                        int result;
                        int secondDot = LastUpdatedVersion.IndexOf(".", firstDot + 1);
                        // Must exist and cannot be last character
                        if (secondDot > 0 && LastUpdatedVersion.Length > secondDot + 1)
                        {
                            // e.g. 1.4.2
                            //Debug.Log("second dot: " + secondDot + " length: " + LastUpdatedVersion.Length + " substring: " + LastUpdatedVersion.Substring(secondDot + 1));
                            if (int.TryParse(LastUpdatedVersion.Substring(secondDot + 1), out result)) { numValue = result; }
                        }
                    }
                }
                return numValue;
            }
        }

        public int GetNumTerrainTreesList { get { return (terrainTreesList == null ? 0 : terrainTreesList.Count); } }
        public int GetNumTerrainGrassList { get { return (terrainGrassList == null ? 0 : terrainGrassList.Count); } }
        
        #endregion

        #region Public Delegates
        public delegate void ShowProgressDelegate(string title, string contentMsg, float percentComplete);

        #endregion

        #region Initialise

        // This runs when Editor starts and when entering play mode.
        // When it comes out of play mode it runs again.
        void Awake () 
	    {
            #if UNITY_EDITOR
            // Create the undo folder if it doesn't already exist 
            string undoFolderPath = "LandscapeBuilder/Undo/";

            if (!Directory.Exists(undoFolderPath))
            {
                Directory.CreateDirectory(undoFolderPath);
            }
            #endif

            SetLandscapeTerrains(true);

            // This probably runs a bit too often, especially for large landscapes...
            SetTerrainNeighbours(false);
            //Debug.Log("[DEBUG] SetTerrainNeighbours");
        }

        #endregion

        #region Public Undo Methods

        /// <summary>
        /// Saves a part of the terrain data given a certain undo type
        /// </summary>
        /// <param name="undoType">Undo type.</param>
        public void SaveData(UndoType undoType)
        {
            string undoFolderPath = "LandscapeBuilder/Undo/";
		    string filePath = string.Empty;

            if (landscapeTerrains == null)
            {
                #if UNITY_EDITOR
                Debug.Log("ERROR: Landscape.SaveData - landscapeTerrains is not defined");
                #endif
            }
            else if (landscapeTerrains.Length > 0 && landscapeTerrains[0].terrainData == null)
            {
                #if UNITY_EDITOR
                Debug.Log("ERROR: Landscape.SaveData - landscapeTerrains terrainData is not defined or not found. Was it deleted?");
                #endif
            }
            else
            { 
			    if (undoType == UndoType.Trees)
			    {
				    undoTreePrototypes = new List<TreePrototype[]>();
				    undoTreeInstances = new List<TreeInstance[]>();
			    }
			    if (undoType == UndoType.Textures)
			    {
                    #if UNITY_2018_3_OR_NEWER
                    undoTerrainLayers = new List<TerrainLayer[]>();
                    #else
				    undoSplatPrototypes = new List<SplatPrototype[]>();
                    #endif
				    undoAlphamaps = new List<float[,,]>();
			    }
			    if (undoType == UndoType.Grass)
			    {
				    undoDetailPrototypes = new List<DetailPrototype[]>();
				    undoDetailLayerLists = new List<List<int[,]>>();
			    }
                for (int i = 0; i < landscapeTerrains.Length; i++)
                {
				    if (undoType == UndoType.HeightMap)
				    {
					    filePath = undoFolderPath + this.GetInstanceID().ToString() + "_heightmap" + i.ToString() + ".dat";
				    }
                
                    #if UNITY_EDITOR
                    int heightmapResolution = landscapeTerrains[i].terrainData.heightmapResolution;

                    // Attempt to save the terrain data to disk
                    BinaryFormatter binaryFormatter = null;
                    FileStream fs = null;
                    try
                    {
                        binaryFormatter = new BinaryFormatter();

                        if (undoType == UndoType.HeightMap)
                        {
                            lbUndo.heightMap = landscapeTerrains[i].terrainData.GetHeights(0, 0, heightmapResolution, heightmapResolution);
                            fs = File.Open(filePath, FileMode.OpenOrCreate);
                            binaryFormatter.Serialize(fs, lbUndo);
                            fs.Close();
                        }
					    else if (undoType == UndoType.Textures)
					    {
                            #if UNITY_2018_3_OR_NEWER
                            undoTerrainLayers.Add(landscapeTerrains[i].terrainData.terrainLayers);
                            #else
						    undoSplatPrototypes.Add(landscapeTerrains[i].terrainData.splatPrototypes);
                            #endif
						    undoAlphamaps.Add(landscapeTerrains[i].terrainData.GetAlphamaps(0, 0, landscapeTerrains[i].terrainData.alphamapWidth, landscapeTerrains[i].terrainData.alphamapHeight));
					    }
					    else if (undoType == UndoType.Trees)
					    {
						    undoTreePrototypes.Add(landscapeTerrains[i].terrainData.treePrototypes);
						    undoTreeInstances.Add(landscapeTerrains[i].terrainData.treeInstances);
					    }
					    else if (undoType == UndoType.Grass)
					    {
						    undoDetailPrototypes.Add(landscapeTerrains[i].terrainData.detailPrototypes);
						    List<int[,]> newDetailLayerList = new List<int[,]>();
						    for (int i2 = 0; i2 < landscapeTerrains[i].terrainData.detailPrototypes.Length; i2++)
						    {
							    newDetailLayerList.Add(landscapeTerrains[i].terrainData.GetDetailLayer(0, 0, landscapeTerrains[i].terrainData.detailWidth, 
							                                                                                 landscapeTerrains[i].terrainData.detailHeight, i2));
						    }
						    undoDetailLayerLists.Add(newDetailLayerList);
					    }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning("ERROR: Landscape SaveData Exception - " + ex.Message);
                    }
                    finally
                    {
                        // Cleanup
                        if (binaryFormatter != null) { binaryFormatter = null; }
                        if (fs != null) { fs.Close(); fs.Dispose(); fs = null; }
					    lbUndo.heightMap = null;
                        lbUndo.heightMap1D = null;
                        lbUndo.splatMaps1D = null;
                        lbUndo.trees1D = null;
                    }

                    #else
                    // To keep compiler happy
                    if (filePath == string.Empty) { }
                    if (lbUndo != null) { }
                    #endif
                }
            }
        }

        #region Save Heightmap 1D

        /// <summary>
        /// Save the current heightmap to disks and include a unique fileIdentifier in the file name.
        /// NOTE: Has no effect if not in the Unity Editor
        /// </summary>
        /// <param name="fileIdentifier"></param>
        /// <param name="showErrors"></param>
        public void SaveHeightmap1D(string fileIdentifier, bool showErrors)
        {
            SetLandscapeTerrains(false);
            int landscapeHeightmapSize = GetTotalHeightmapSize(false);

            if (landscapeHeightmapSize == 0)
            {
                #if UNITY_EDITOR
                Debug.LogWarning("ERROR: Landscape.SaveHeightmap1D - could not get total heightmap size. PLEASE REPORT");
                #endif
            }
            else
            {
                // Create heightmap as a 1-dimensional array
                float[] heightMap1D = new float[landscapeHeightmapSize];

                if (!LBLandscapeTerrain.GetLandscapeScopedHeightmap(this, heightMap1D, false, showErrors))
                {
                    #if UNITY_EDITOR
                    if (showErrors) { Debug.LogWarning("ERROR: Landscape.SaveHeightmap1D - could not populate landscape-scoped heightmap array. PLEASE REPORT"); }
                    #endif
                }
                else
                {
                    SaveHeightmap1D(heightMap1D, fileIdentifier);
                }
            }
        }

        /// <summary>
        /// Given a 1D heightmap array, save it to a single undo file on disk.
        /// NOTE: Has no effect if not in the Unity Editor.
        /// The fileIdentifier can be used to uniquely identify this backup. For example
        /// it could be the "RP" + lbGroupMember.GUID of an LBObjPath.
        /// </summary>
        /// <param name="heightmap1D"></param>
        /// <param name="fileIdentifier"></param>
        public void SaveHeightmap1D(float[] heightmap1D, string fileIdentifier)
        {
            string undoFolderPath = "LandscapeBuilder/Undo/";
            string filePath = string.Empty;

            if (heightmap1D == null)
            {
                #if UNITY_EDITOR
                Debug.LogWarning("ERROR: Landscape.SaveHeightmap1D - heightmap is not defined. PLEASE REPORT");
                #endif
            }
            else
            {
                filePath = undoFolderPath + this.GetInstanceID().ToString() + "_" + fileIdentifier + "_heightmap1D.dat";

                #if UNITY_EDITOR
                // Attempt to save the terrain data for whole landscape to disk
                BinaryFormatter binaryFormatter = null;
                FileStream fs = null;
                try
                {
                    binaryFormatter = new BinaryFormatter();

                    // Overwrite any existing files of the same name
                    lbUndo.heightMap = null;
                    lbUndo.heightMap1D = heightmap1D;
                    lbUndo.splatMaps1D = null;
                    lbUndo.trees1D = null;
                    fs = File.Open(filePath, FileMode.OpenOrCreate);
                    binaryFormatter.Serialize(fs, lbUndo);
                    fs.Close();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("ERROR: Landscape SaveHeightmap1D Exception - " + ex.Message);
                }
                finally
                {
                    // Cleanup
                    if (binaryFormatter != null) { binaryFormatter = null; }
                    if (fs != null) { fs.Close(); fs.Dispose(); fs = null; }
                    lbUndo.heightMap = null;
                    lbUndo.heightMap1D = null;
                    lbUndo.splatMaps1D = null;
                    lbUndo.trees1D = null;
                }
                #else
                // To keep compiler happy
                if (filePath == string.Empty) { }
                if (lbUndo != null) { }
                #endif
            }
        }

        #endregion Save Heightmap 1D

        #region Save Texture Splatmaps 1D

        /// <summary>
        /// Save a 1D array of splatmap data for a single terrain to disks.
        /// See also SaveTextures1D(string fileIdentifier, bool showErrors) which typically
        /// calls this method.
        /// </summary>
        /// <param name="splatMaps1D"></param>
        /// <param name="fileIdentifier"></param>
        /// <param name="showErrors"></param>
        public void SaveTextures1D(float[] splatMaps1D, string fileIdentifier, bool showErrors)
        {
            string undoFolderPath = "LandscapeBuilder/Undo/";
            string filePath = string.Empty;

            if (splatMaps1D == null)
            {
                #if UNITY_EDITOR
                Debug.LogWarning("ERROR: Landscape.SaveTexture1D - splatMaps are not defined. PLEASE REPORT");
                #endif
            }
            else
            {
                filePath = undoFolderPath + this.GetInstanceID().ToString() + "_" + fileIdentifier + "_splatmaps1D.dat";

                #if UNITY_EDITOR
                // Attempt to save the splatmap data from a terrain to disk
                BinaryFormatter binaryFormatter = null;
                FileStream fs = null;
                try
                {
                    binaryFormatter = new BinaryFormatter();

                    // Overwrite any existing files of the same name
                    lbUndo.heightMap = null;
                    lbUndo.heightMap1D = null;
                    lbUndo.splatMaps1D = splatMaps1D;
                    lbUndo.trees1D = null;
                    fs = File.Open(filePath, FileMode.OpenOrCreate);
                    binaryFormatter.Serialize(fs, lbUndo);
                    fs.Close();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("ERROR: Landscape SaveTexture1D Exception - " + ex.Message);
                }
                finally
                {
                    // Cleanup
                    if (binaryFormatter != null) { binaryFormatter = null; }
                    if (fs != null) { fs.Close(); fs.Dispose(); fs = null; }
                    lbUndo.heightMap = null;
                    lbUndo.heightMap1D = null;
                    lbUndo.splatMaps1D = null;
                    lbUndo.trees1D = null;
                }
                #else
                // To keep compiler happy
                if (filePath == string.Empty) { }
                if (lbUndo != null) { }
                #endif
            }
        }

        /// <summary>
        /// Save the current splatmaps to disks and include a unique fileIdentifier in the file name.
        /// NOTE: Has no effect if not in the Unity Editor
        /// NOTE: This may cause issues for Imported Terrains with different textures in each terrain.
        /// ASSUMPTIONS: Landscape has equal number of terrains in x and z directions
        /// </summary>
        /// <param name="fileIdentifier"></param>
        /// <param name="showErrors"></param>
        public void SaveTextures1D(string fileIdentifier, bool showErrors)
        {
            SetLandscapeTerrains(false);

            int numTerrains = landscapeTerrains == null ? 0 : landscapeTerrains.Length;

            if (numTerrains > 0)
            {
                int terrainAlphamapResolution = GetLandscapeTerrainAlphaMapResolution();

                // Get the size of all alphamaps (splatmaps) FOR EACH TERRAIN
                int numActiveTerrainTextures = GetNumActiveTerrainTextures(true);
                int splatMapSize = terrainAlphamapResolution * terrainAlphamapResolution;
                int splatMaps1DSize = splatMapSize * numActiveTerrainTextures;

                float[] splatMaps1D = new float[splatMaps1DSize];

                for (int tIdx = 0; tIdx < numTerrains; tIdx++)
                {
                    Terrain terrain = landscapeTerrains[tIdx];

                    if (terrain == null) { if (showErrors) { Debug.LogWarning("ERROR: LBLandscape.SaveTexture1D - terrain is null. PLEASE REPORT"); } break; }
                    else
                    {
                        TerrainData tData = terrain.terrainData;
                        if (tData == null) { if (showErrors) { Debug.LogWarning("ERROR: LBLandscape.SaveTexture1D - terrainData is null. PLEASE REPORT"); } break; }
                        else
                        {
                            // Retrieve the texture arrays from the terrain data
                            float[,,] terrainTextureArray = tData.GetAlphamaps(0, 0, terrainAlphamapResolution, terrainAlphamapResolution);

                            if (terrainTextureArray == null) { if (showErrors) { Debug.LogWarning("ERROR: LBLandscape.SaveTexture1D - could not get splatmap from terrain. PLEASE REPORT"); } break; }
                            else
                            {
                                // Convert from 3D alphamap to 1D array
                                for (int smTexIdx = 0; smTexIdx < numActiveTerrainTextures; smTexIdx++)
                                {
                                    for (int z = 0; z < terrainAlphamapResolution; z++)
                                    {
                                        for (int x = 0; x < terrainAlphamapResolution; x++)
                                        {
                                            splatMaps1D[(splatMapSize * smTexIdx) + (terrainAlphamapResolution * z) + x] = terrainTextureArray[z, x, smTexIdx];
                                        }
                                    }
                                }

                                // Write file to disk
                                SaveTextures1D(splatMaps1D, fileIdentifier + "_T" + tIdx.ToString("000"), showErrors);
                            }
                        }
                    }
                }
            }
        }

        #endregion Save Texture Splatmaps 1D

        #region Save Tree 1D

        /// <summary>
        /// Save a 1D array of tree instance data for a single terrain to disks.
        /// See also SaveTrees1D(string fileIdentifier, bool showErrors) which typically
        /// calls this method.
        /// </summary>
        /// <param name="trees1D"></param>
        /// <param name="fileIdentifier"></param>
        /// <param name="showErrors"></param>
        public void SaveTrees1D(TreeInstance[] trees1D, string fileIdentifier, bool showErrors)
        {
            string undoFolderPath = "LandscapeBuilder/Undo/";
            string filePath = string.Empty;

            if (trees1D == null)
            {
                #if UNITY_EDITOR
                Debug.LogWarning("ERROR: Landscape.SaveTrees1D - trees are not defined. PLEASE REPORT");
                #endif
            }
            else
            {
                filePath = undoFolderPath + this.GetInstanceID().ToString() + "_" + fileIdentifier + "_trees1D.dat";

                #if UNITY_EDITOR
                // Attempt to save the trees data from a terrain to disk
                BinaryFormatter binaryFormatter = null;
                FileStream fs = null;
                try
                {
                    binaryFormatter = new BinaryFormatter();

                    // Overwrite any existing files of the same name
                    lbUndo.heightMap = null;
                    lbUndo.heightMap1D = null;
                    lbUndo.splatMaps1D = null;
                    lbUndo.trees1D = LBTerrainTreeInstanceBin.ToLBTerrainTreeInstanceBinList(trees1D);
                    fs = File.Open(filePath, FileMode.OpenOrCreate);
                    binaryFormatter.Serialize(fs, lbUndo);
                    fs.Close();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("ERROR: Landscape SaveTrees1D Exception - " + ex.Message);
                }
                finally
                {
                    // Cleanup
                    if (binaryFormatter != null) { binaryFormatter = null; }
                    if (fs != null) { fs.Close(); fs.Dispose(); fs = null; }
                    lbUndo.heightMap = null;
                    lbUndo.heightMap1D = null;
                    lbUndo.splatMaps1D = null;
                    lbUndo.trees1D = null;
                }
                #else
                // To keep compiler happy
                if (filePath == string.Empty) { }
                if (lbUndo != null) { }
                #endif
            }
        }

        /// <summary>
        /// Save the current Unity terrain trees to disks and include a unique fileIdentifier in the file name.
        /// NOTE: Has no effect if not in the Unity Editor
        /// NOTE: This may cause issues for Imported Terrains with different trees in each terrain.
        /// ASSUMPTIONS: Landscape has equal number of terrains in x and z directions
        /// </summary>
        /// <param name="fileIdentifier"></param>
        /// <param name="showErrors"></param>
        public void SaveTrees1D(string fileIdentifier, bool showErrors)
        {
            SetLandscapeTerrains(false);

            int numTerrains = landscapeTerrains == null ? 0 : landscapeTerrains.Length;

            if (numTerrains > 0)
            {
                for (int tIdx = 0; tIdx < numTerrains; tIdx++)
                {
                    Terrain terrain = landscapeTerrains[tIdx];

                    if (terrain == null) { if (showErrors) { Debug.LogWarning("ERROR: LBLandscape.SaveTrees1D - terrain is null. PLEASE REPORT"); } break; }
                    else
                    {
                        TerrainData tData = terrain.terrainData;
                        if (tData == null) { if (showErrors) { Debug.LogWarning("ERROR: LBLandscape.SaveTrees1D - terrainData is null. PLEASE REPORT"); } break; }
                        else
                        {
                            // Write file to disk
                            SaveTrees1D(tData.treeInstances, fileIdentifier + "_T" + tIdx.ToString("000"), showErrors);
                        }
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// Restores the last saved landscape
        /// e.g. landscape.RevertToLastSave(Landscape.UndoType.HeightMap);
        /// </summary>
        /// <param name="undoType"></param>
        public void RevertToLastSave(UndoType undoType)
        {
            string undoFolderPath = "LandscapeBuilder/Undo/";
            string filePath = string.Empty;

            if (landscapeTerrains == null)
            {
                Debug.Log("ERROR: Landscape.RevertToLastSave - landscapeTerrains is not defined");
            }
            else
            {
			    if (undoType == UndoType.HeightMap)
			    {
	                for (int i = 0; i < landscapeTerrains.Length; i++)
	                {
	                    if (undoType == UndoType.HeightMap)
	                    {
						    filePath = undoFolderPath + this.GetInstanceID().ToString() + "_heightmap" + i.ToString() + ".dat";
	                    }

	                    #if UNITY_EDITOR
	                    if (File.Exists(filePath))
	                    {
	                        BinaryFormatter binaryFormatter = null;
	                        FileStream fs = null;
	                        try
	                        {
	                            // Read the binary data from file in the application default data folder
	                            binaryFormatter = new BinaryFormatter();

	                            if (undoType == UndoType.HeightMap)
	                            {
	                                fs = File.Open(filePath, FileMode.Open);
	                                lbUndo = (LBUndo)binaryFormatter.Deserialize(fs);
	                                fs.Close();

	                                // Revert to saved data
	                                landscapeTerrains[i].terrainData.SetHeights(0, 0, lbUndo.heightMap);
	                            }
	                        }
	                        catch (Exception ex)
	                        {
	                            Debug.Log("ERROR: RevertToLastSave Load: Exception - " + ex.Message);
	                        }
	                        finally
	                        {
	                            // Cleanup
	                            if (binaryFormatter != null) { binaryFormatter = null; }
	                            if (fs != null) { fs.Close(); fs.Dispose(); fs = null; }
							    lbUndo.heightMap = null;
                                lbUndo.heightMap1D = null;
                                lbUndo.splatMaps1D = null;
                                lbUndo.trees1D = null;
                            }
	                    }
                        #else
                        // To keep compiler happy
                        if (filePath == string.Empty) { }
	                    #endif
	                }
			    }
			    else
			    {
				    if (undoType == UndoType.Textures)
				    {
                        #if UNITY_2018_3_OR_NEWER
                        if (undoTerrainLayers != null && undoAlphamaps != null && undoTerrainLayers.Count > landscapeTerrains.Length - 1 &&
					        undoAlphamaps.Count > landscapeTerrains.Length - 1)
					    {
						    for (int i = 0; i < landscapeTerrains.Length; i++)
						    {
							    landscapeTerrains[i].terrainData.terrainLayers = undoTerrainLayers[i];
							    landscapeTerrains[i].terrainData.SetAlphamaps(0, 0, undoAlphamaps[i]);
						    }
					    }
                        #else
					    if (undoSplatPrototypes != null && undoAlphamaps != null && undoSplatPrototypes.Count > landscapeTerrains.Length - 1 &&
					        undoAlphamaps.Count > landscapeTerrains.Length - 1)
					    {
						    for (int i = 0; i < landscapeTerrains.Length; i++)
						    {
							    landscapeTerrains[i].terrainData.splatPrototypes = undoSplatPrototypes[i];
							    landscapeTerrains[i].terrainData.SetAlphamaps(0, 0, undoAlphamaps[i]);
						    }
					    }
                        #endif
				    }
				    else if (undoType == UndoType.Trees)
				    {
					    if (undoTreePrototypes != null && undoTreeInstances != null && undoTreePrototypes.Count > landscapeTerrains.Length - 1 &&
					        undoTreeInstances.Count > landscapeTerrains.Length - 1)
					    {
						    for (int i = 0; i < landscapeTerrains.Length; i++)
						    {
							    landscapeTerrains[i].terrainData.treePrototypes = undoTreePrototypes[i];
							    landscapeTerrains[i].terrainData.treeInstances = undoTreeInstances[i];
						    }
					    }
				    }
				    else if (undoType == UndoType.Grass)
				    {
					    if (undoDetailPrototypes != null && undoDetailLayerLists != null && undoDetailPrototypes.Count > landscapeTerrains.Length - 1 &&
					        undoDetailLayerLists.Count > landscapeTerrains.Length - 1)
					    {
						    for (int i = 0; i < landscapeTerrains.Length; i++)
						    {
							    landscapeTerrains[i].terrainData.detailPrototypes = undoDetailPrototypes[i];
							    if (undoDetailLayerLists[i] != null)
							    {
								    for (int i2 = 0; i2 < undoDetailLayerLists[i].Count; i2++)
								    {
									    landscapeTerrains[i].terrainData.SetDetailLayer(0, 0, i2, undoDetailLayerLists[i][i2]);
								    }
							    }
						    }
					    }
				    }
			    }
                UpdateTerrainColliders();
            }
        }

        #region Revert Heightmap

        /// <summary>
        /// Revert or restore a previously saved heightmap to the current landscape.
        /// NOTE: Has no effect if run outside the Unity Editor
        /// The fileIdentifier can be used to uniquely identify this backup. For example
        /// it could be the "RP" + lbGroupMember.GUID of an LBObjPath.
        /// </summary>
        /// <param name="fileIdentifier"></param>
        /// <param name="applyTerrainLOD"></param>
        /// <param name="showErrors"></param>
        public void RevertHeightmap1D(string fileIdentifier, bool applyTerrainLOD, bool showErrors)
        {
            #if UNITY_EDITOR
            // Get the name of the file to restore
            string filePath = "LandscapeBuilder/Undo/" + this.GetInstanceID().ToString() + "_" + fileIdentifier + "_heightmap1D.dat";

            // Check if the file exists
            if (File.Exists(filePath))
            {
                SetLandscapeTerrains(false);

                if (landscapeTerrains == null)
                {
                    Debug.LogWarning("ERROR: Landscape.RevertHeightmap1D - could not get landscapeTerrains. PLEASE REPORT");
                }
                else
                {
                    BinaryFormatter binaryFormatter = null;
                    FileStream fs = null;
                    try
                    {
                        // Read the binary data from file in the undo folder
                        binaryFormatter = new BinaryFormatter();

                        fs = File.Open(filePath, FileMode.Open);
                        lbUndo = (LBUndo)binaryFormatter.Deserialize(fs);
                        fs.Close();

                        // Revert to saved heightmap data
                        if (!LBLandscapeTerrain.CommitLandscapeScopedHeightmap(this, lbUndo.heightMap1D, false, showErrors))
                        {
                            if (showErrors) { Debug.LogWarning("ERROR: Landscape.RevertHeightmap1D - could not copy landscape-scoped heightmap array back to terrains. PLEASE REPORT"); }
                        }
                        else if (applyTerrainLOD)
                        {
                            // We need to update the terrain LOD and vegetation information
                            LBLandscapeTerrain.ApplyDelayedHeightmapLOD(landscapeTerrains);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Log("ERROR: RevertHeightmap1D Load: Exception - " + ex.Message);
                    }
                    finally
                    {
                        // Cleanup
                        if (binaryFormatter != null) { binaryFormatter = null; }
                        if (fs != null) { fs.Close(); fs.Dispose(); fs = null; }
                        lbUndo.heightMap = null;
                        lbUndo.heightMap1D = null;
                        lbUndo.splatMaps1D = null;
                        lbUndo.trees1D = null;
                    }
                }
            }
            #else
            // To keep compiler happy
            if (lbUndo != null) { }
            #endif
        }

        #endregion

        #region Revert Texture Splatmaps 1D

        /// <summary>
        /// Revert or restore a previously saved splatmaps to the current landscape.
        /// NOTE: Has no effect if run outside the Unity Editor
        /// The fileIdentifier can be used to uniquely identify this backup. For example
        /// it could be the "RP" + lbGroupMember.GUID of an LBObjPath.
        /// WARNING: This will fail if they splatprototypes have been changed since
        /// the last SaveTextures1D(..) operation.
        /// </summary>
        /// <param name="fileIdentifier"></param>
        /// <param name="showErrors"></param>
        public void RevertTextures1D(string fileIdentifier, bool showErrors)
        {
            #if UNITY_EDITOR
            SetLandscapeTerrains(false);

            int numTerrains = landscapeTerrains == null ? 0 : landscapeTerrains.Length;

            if (numTerrains > 0)
            {
                int terrainAlphamapResolution = GetLandscapeTerrainAlphaMapResolution();

                // Get the size of all alphamaps (splatmaps) FOR EACH TERRAIN
                int numActiveTerrainTextures = GetNumActiveTerrainTextures(true);
                int splatMapSize = terrainAlphamapResolution * terrainAlphamapResolution;

                // Create an empty 3D array for the terrain splatmaps
                float[,,] terrainTextureArray = new float[terrainAlphamapResolution, terrainAlphamapResolution, numActiveTerrainTextures];

                // Use StringFast to avoid unnecessary GC
                LB_Extension.StringFast filePath = new LB_Extension.StringFast(100);

                BinaryFormatter binaryFormatter = null;
                FileStream fs = null;

                for (int tIdx = 0; tIdx < numTerrains; tIdx++)
                {
                    // Get the name of the file to restore
                    filePath.Set("LandscapeBuilder/Undo/");
                    filePath.Append(this.GetInstanceID().ToString());
                    filePath.Append("_");
                    filePath.Append(fileIdentifier);
                    filePath.Append("_T");
                    filePath.Append(tIdx.ToString("000"));
                    filePath.Append("_splatmaps1D.dat");

                    try
                    {
                        // Read the binary data from file in undo folder
                        binaryFormatter = new BinaryFormatter();

                        fs = File.Open(filePath.ToString(), FileMode.Open);
                        lbUndo = (LBUndo)binaryFormatter.Deserialize(fs);
                        fs.Close();

                        if (lbUndo.splatMaps1D != null)
                        {
                            // Convert from 1D array to 3D alphamap
                            for (int smTexIdx = 0; smTexIdx < numActiveTerrainTextures; smTexIdx++)
                            {
                                for (int z = 0; z < terrainAlphamapResolution; z++)
                                {
                                    for (int x = 0; x < terrainAlphamapResolution; x++)
                                    {
                                        terrainTextureArray[z, x, smTexIdx] = lbUndo.splatMaps1D[(splatMapSize * smTexIdx) + (terrainAlphamapResolution * z) + x];
                                    }
                                }
                            }

                            landscapeTerrains[tIdx].terrainData.SetAlphamaps(0, 0, terrainTextureArray);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Log("ERROR: RevertTextures1D Load: Exception - " + ex.Message);
                    }
                    finally
                    {
                        // Cleanup
                        if (binaryFormatter != null) { binaryFormatter = null; }
                        if (fs != null) { fs.Close(); fs.Dispose(); fs = null; }
                        lbUndo.heightMap = null;
                        lbUndo.heightMap1D = null;
                        lbUndo.splatMaps1D = null;
                        lbUndo.trees1D = null;
                    }
                }
            }
            #endif
        }

        #endregion Revert Texture Splatmaps 1D

        #region Revert Trees1D

        /// <summary>
        /// Revert or restore a previously saved Unity terrain trees to the current landscape.
        /// NOTE: Has no effect if run outside the Unity Editor
        /// The fileIdentifier can be used to uniquely identify this backup. For example
        /// it could be the "RP" + lbGroupMember.GUID of an LBObjPath.
        /// WARNING: This will fail if the treetypes have been changed since
        /// the last SaveTrees1D(..) operation.
        /// </summary>
        /// <param name="fileIdentifier"></param>
        /// <param name="showErrors"></param>
        public void RevertTrees1D(string fileIdentifier, bool showErrors)
        {
            #if UNITY_EDITOR
            SetLandscapeTerrains(false);

            int numTerrains = landscapeTerrains == null ? 0 : landscapeTerrains.Length;

            if (numTerrains > 0)
            {
                // Use StringFast to avoid unnecessary GC
                LB_Extension.StringFast filePath = new LB_Extension.StringFast(100);

                BinaryFormatter binaryFormatter = null;
                FileStream fs = null;

                for (int tIdx = 0; tIdx < numTerrains; tIdx++)
                {
                    // Get the name of the file to restore
                    filePath.Set("LandscapeBuilder/Undo/");
                    filePath.Append(this.GetInstanceID().ToString());
                    filePath.Append("_");
                    filePath.Append(fileIdentifier);
                    filePath.Append("_T");
                    filePath.Append(tIdx.ToString("000"));
                    filePath.Append("_trees1D.dat");

                    try
                    {
                        // Read the binary data from file in undo folder
                        binaryFormatter = new BinaryFormatter();

                        fs = File.Open(filePath.ToString(), FileMode.Open);
                        lbUndo = (LBUndo)binaryFormatter.Deserialize(fs);
                        fs.Close();

                        if (lbUndo.trees1D != null)
                        {
                            //Debug.Log("[DEBUG] RevertTrees1D - " + filePath);
                            landscapeTerrains[tIdx].terrainData.treeInstances = LBTerrainTreeInstanceBin.ToTreeInstanceList(lbUndo.trees1D).ToArray();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Log("ERROR: RevertTrees1D Load: Exception - " + ex.Message);
                    }
                    finally
                    {
                        // Cleanup
                        if (binaryFormatter != null) { binaryFormatter = null; }
                        if (fs != null) { fs.Close(); fs.Dispose(); fs = null; }
                        lbUndo.heightMap = null;
                        lbUndo.heightMap1D = null;
                        lbUndo.splatMaps1D = null;
                        lbUndo.trees1D = null;
                    }
                }
            }
            #endif
        }

        #endregion

        /// <summary>
        /// Clean up (delete) all serialized undo files older than a number of days
        /// </summary>
        /// <param name="daysOldToDelete"></param>
        /// <param name="isDisplayCompleted"></param>
        public static void PerformUndoCleanup (int daysOldToDelete, bool isDisplayCompleted)
	    {
		    #if UNITY_EDITOR
            try
            {
		        string undoFolder = "LandscapeBuilder/Undo";
		        if (Directory.Exists(undoFolder))
		        {
			        // Retrieve file paths for all files in folder
			        string[] filePaths = Directory.GetFiles(undoFolder);
			        if (filePaths != null)
			        {
				        foreach(string filePath in filePaths)
				        {
                            if (File.GetLastAccessTime(filePath) < DateTime.Now.AddDays(-daysOldToDelete))
                            {
                                //Debug.Log("Undo Cleanup: Deleting " + filePath);
                                File.Delete(filePath);
                            }
				        }
			        }

                    if (isDisplayCompleted) { Debug.Log("Undo Cleanup Complete"); }
		        }
            }
            catch (System.IO.IOException ioEx)
            {
                Debug.LogWarning("LBLandscape.PerformUndoCleanup " + ioEx.Message);
            }
		    #endif
	    }

        #endregion

        #region EDITOR-ONLY Methods

        /// <summary>
        /// Save whether AutoSave is enabled to a serialized file
        /// </summary>
        public static void SetAutoSaveState (bool autoSaveEnabled)
	    {
            #if UNITY_EDITOR
		    if (File.Exists ("LandscapeBuilder/LBSaveFile.dat")) 
		    {
			    // If the save file already exists retrieve previously saved data - we only want to change one thing
			    lbSavedData = RetrieveSavedData ();
			    tempLBSavedData = null;
		    }
		    else
		    {
			    // If the save file doesn't already exist we will have to create a new one
			    lbSavedData = new LBSavedData();
		    }

		    if (lbSavedData != null)
		    {
			    // Attempt to save the saved data to disk
			    BinaryFormatter binaryFormatter = null;
			    FileStream fs = null;
			    try
			    {
				    binaryFormatter = new BinaryFormatter();
				    fs = File.Open("LandscapeBuilder/LBSaveFile.dat", FileMode.OpenOrCreate);
				    lbSavedData.autoSaveEnabled = autoSaveEnabled;
				    binaryFormatter.Serialize(fs, lbSavedData);
				    fs.Close();
			    }
			    catch (Exception ex)
			    {
				    Debug.Log("ERROR: Landscape SetAutoSaveState Exception - " + ex.Message);
			    }
			    finally
			    {
				    // Cleanup
				    if (binaryFormatter != null) { binaryFormatter = null; }
				    if (fs != null) { fs.Close(); fs.Dispose(); fs = null; }
			    }
		    }
		    else { Debug.Log("ERROR: Landscape SetAutoSaveState could retrieve saved data"); }
            #else
            // Keep compiler happy
            if (lbSavedData != null) {}
            #endif
	    }
    
        /// <summary>
	    /// Save the last select Landscape GameObject to a serialized file
	    /// </summary>
	    public static void SetLastLandscapeGameObjectSelection(string LastLandscapeGameObjectName)
	    {
            #if UNITY_EDITOR
		    if (File.Exists ("LandscapeBuilder/LBSaveFile.dat")) 
		    {
			    // If the save file already exists retrieve previously saved data - we only want to change the last Landscape selected
			    lbSavedData = RetrieveSavedData ();
			    tempLBSavedData = null;
		    }
		    else
		    {
			    // If the save file doesn't already exist we will have to create a new one
			    lbSavedData = new LBSavedData();
		    }

		    if (lbSavedData != null)
		    {
			    // Attempt to save the saved data to disk
			    BinaryFormatter binaryFormatter = null;
			    FileStream fs = null;
			    try
			    {
				    binaryFormatter = new BinaryFormatter();
				    fs = File.Open("LandscapeBuilder/LBSaveFile.dat", FileMode.OpenOrCreate);
                    lbSavedData.lastLandscapeGameObjectSelectionName = LastLandscapeGameObjectName;
                    binaryFormatter.Serialize(fs, lbSavedData);
				    fs.Close();
			    }
			    catch (Exception ex)
			    {
				    Debug.Log("ERROR: Landscape SetLastLandscapeGameObjectSelection Exception - " + ex.Message);
			    }
			    finally
			    {
				    // Cleanup
				    if (binaryFormatter != null) { binaryFormatter = null; }
				    if (fs != null) { fs.Close(); fs.Dispose(); fs = null; }
			    }
		    }
		    else { Debug.Log("ERROR: Landscape SetLastLandscapeGameObjectSelection could retrieve saved data"); }
            #endif
	    }

        /// <summary>
        /// Save whether disableAllTextures is ON to a serialized file
        /// </summary>
        /// <param name="isDisableAllTexturesOn"></param>
        public static void SetDisableAllTextures (bool isDisableAllTexturesOn)
	    {
            #if UNITY_EDITOR
		    if (File.Exists ("LandscapeBuilder/LBSaveFile.dat")) 
		    {
			    // If the save file already exists retrieve previously saved data - we only want to change disableAllTextures
			    lbSavedData = RetrieveSavedData ();
			    tempLBSavedData = null;
		    }
		    else
		    {
			    // If the save file doesn't already exist we will have to create a new one
			    lbSavedData = new LBSavedData();
		    }

		    if (lbSavedData != null)
		    {
			    // Attempt to save the saved data to disk
			    BinaryFormatter binaryFormatter = null;
			    FileStream fs = null;
			    try
			    {
				    binaryFormatter = new BinaryFormatter();
				    fs = File.Open("LandscapeBuilder/LBSaveFile.dat", FileMode.OpenOrCreate);
				    lbSavedData.disableAllTextures = isDisableAllTexturesOn;
				    binaryFormatter.Serialize(fs, lbSavedData);
				    fs.Close();
			    }
			    catch (Exception ex)
			    {
				    Debug.LogWarning("ERROR: Landscape SetDisableAllTextures Exception - " + ex.Message);
			    }
			    finally
			    {
				    // Cleanup
				    if (binaryFormatter != null) { binaryFormatter = null; }
				    if (fs != null) { fs.Close(); fs.Dispose(); fs = null; }
			    }
		    }
		    else { Debug.LogWarning("ERROR: Landscape SetDisableAllTextures could retrieve saved data"); }
            #endif
	    }

        /// <summary>
        /// Save whether isNonSquareTerrainsEnabled is ON to a serialized file
        /// </summary>
        /// <param name="isEnabled"></param>
        public static void SetIsNonSquareTerrainsEnabled(bool isEnabled)
	    {
            #if UNITY_EDITOR
		    if (File.Exists ("LandscapeBuilder/LBSaveFile.dat")) 
		    {
			    // If the save file already exists retrieve previously saved data - we only want to change one thing
			    lbSavedData = RetrieveSavedData ();
			    tempLBSavedData = null;
		    }
		    else
		    {
			    // If the save file doesn't already exist we will have to create a new one
			    lbSavedData = new LBSavedData();
		    }

		    if (lbSavedData != null)
		    {
			    // Attempt to save the saved data to disk
			    BinaryFormatter binaryFormatter = null;
			    FileStream fs = null;
			    try
			    {
				    binaryFormatter = new BinaryFormatter();
				    fs = File.Open("LandscapeBuilder/LBSaveFile.dat", FileMode.OpenOrCreate);
				    lbSavedData.isNonSquareTerrainsEnabled = isEnabled;
				    binaryFormatter.Serialize(fs, lbSavedData);
				    fs.Close();
			    }
			    catch (Exception ex)
			    {
				    Debug.LogWarning("ERROR: Landscape SetIsNonSquareTerrainsEnabled Exception - " + ex.Message);
			    }
			    finally
			    {
				    // Cleanup
				    if (binaryFormatter != null) { binaryFormatter = null; }
				    if (fs != null) { fs.Close(); fs.Dispose(); fs = null; }
			    }
		    }
		    else { Debug.LogWarning("ERROR: Landscape SetIsNonSquareTerrainsEnabled could retrieve saved data"); }
            #else
            // Keep compiler happy
            if (lbSavedData != null) {}
            #endif
	    }

        /// <summary>
        /// Save whether isLegacyNoiseOffset is ON to a serialized file
        /// </summary>
        /// <param name="isEnabled"></param>
        public static void SetIsLegacyNoiseOffsetEnabled(bool isEnabled)
	    {
            #if UNITY_EDITOR
		    if (File.Exists ("LandscapeBuilder/LBSaveFile.dat")) 
		    {
			    // If the save file already exists retrieve previously saved data - we only want to change one thing
			    lbSavedData = RetrieveSavedData ();
			    tempLBSavedData = null;
		    }
		    else
		    {
			    // If the save file doesn't already exist we will have to create a new one
			    lbSavedData = new LBSavedData();
		    }

		    if (lbSavedData != null)
		    {
			    // Attempt to save the saved data to disk
			    BinaryFormatter binaryFormatter = null;
			    FileStream fs = null;
			    try
			    {
				    binaryFormatter = new BinaryFormatter();
				    fs = File.Open("LandscapeBuilder/LBSaveFile.dat", FileMode.OpenOrCreate);
				    lbSavedData.isLegacyNoiseOffset = isEnabled;
				    binaryFormatter.Serialize(fs, lbSavedData);
				    fs.Close();
			    }
			    catch (Exception ex)
			    {
				    Debug.LogWarning("ERROR: Landscape SetIsLegacyNoiseOffsetEnabled Exception - " + ex.Message);
			    }
			    finally
			    {
				    // Cleanup
				    if (binaryFormatter != null) { binaryFormatter = null; }
				    if (fs != null) { fs.Close(); fs.Dispose(); fs = null; }
			    }
		    }
		    else { Debug.LogWarning("ERROR: Landscape SetIsLegacyNoiseOffsetEnabled could retrieve saved data"); }
            #else
            // Keep compiler happy
            if (lbSavedData != null) {}
            #endif
	    }


        #if UNITY_EDITOR
	    /// <summary>
	    /// Retrieve whether AutoSave is enabled from a serialized file
	    /// </summary>
	    public static bool GetAutoSaveState ()
	    {
		    lbSavedData = RetrieveSavedData ();
		    tempLBSavedData = null;
		    if (lbSavedData != null) { return lbSavedData.autoSaveEnabled; }
		    else { Debug.LogWarning("ERROR: Landscape GetAutoSaveState could not retrieve saved data");  return true; }
	    }
        #endif

        #if UNITY_EDITOR
        /// <summary>
        /// Retrieve the last Landscape selected name from a serialized file
        /// </summary>
        public static string GetLastLandscapeGameObjectSelection()
        {
            lbSavedData = RetrieveSavedData();
            tempLBSavedData = null;
            if (lbSavedData != null) { return lbSavedData.lastLandscapeGameObjectSelectionName; }
            else { Debug.LogWarning("ERROR: Landscape GetLastLandscapeGameObjectSelection could not retrieve saved data"); return null; }
        }
        #endif


        #if UNITY_EDITOR
	    /// <summary>
	    /// Retrieve whether isNonSquareTerrainsEnabled is enabled from a serialized file
	    /// </summary>
	    public static bool GetIsNonSquareTerrainsEnabled ()
	    {
		    lbSavedData = RetrieveSavedData ();
		    tempLBSavedData = null;
		    if (lbSavedData != null) { return lbSavedData.isNonSquareTerrainsEnabled; }
		    else { Debug.LogWarning("ERROR: Landscape GetIsNonSquareTerrainsEnabled could not retrieve saved data");  return false; }
	    }
        #endif

        #if UNITY_EDITOR
	    /// <summary>
	    /// Retrieve whether isLegacyNoiseOffset is enabled from a serialized file
        /// Defaults to false if it couldn't retrieve data
	    /// </summary>
	    public static bool GetIsLegacyNoiseOffsetEnabled ()
	    {
		    lbSavedData = RetrieveSavedData ();
		    tempLBSavedData = null;
		    if (lbSavedData != null) { return lbSavedData.isLegacyNoiseOffset; }
		    else { Debug.LogWarning("ERROR: Landscape GetIsLegacyNoiseOffsetEnabled could not retrieve saved data");  return false; }
	    }
        #endif

        #if UNITY_EDITOR
        /// <summary>
        /// Retrieve whether disableAllTextures is ON from a serialized file
        /// </summary>
        /// <returns></returns>
        public static bool GetDisableAllTextures()
        {
            lbSavedData = RetrieveSavedData();
            tempLBSavedData = null;
            if (lbSavedData != null) { return lbSavedData.disableAllTextures; }
            else { Debug.LogWarning("ERROR: Landscape GetDisableAllTexturese could not retrieve saved data"); return false; }
        }

        #region Get and Set Path Methods

        public static string GetPath(LBSavedData.PathType pathType)
        {
            string pathName = string.Empty;

            lbSavedData = RetrieveSavedData();
            if (lbSavedData != null)
            {
                switch (pathType)
                {
                    case LBSavedData.PathType.HQPhotographicTexturesVol1:
                        pathName = lbSavedData.pathHQPhotographicTexturesVol1;
                        break;
                    case LBSavedData.PathType.HQPhotographicTexturesVol2:
                        pathName = lbSavedData.pathHQPhotographicTexturesVol2;
                        break;
                    case LBSavedData.PathType.FlowMapPainterExe:
                        pathName = lbSavedData.pathFlowMapPainterEXE;
                        break;
                    case LBSavedData.PathType.RusticGrass:
                        pathName = lbSavedData.pathRusticGrass;
                        break;
                    default:
                        pathName = string.Empty;
                        break;
                }
            }
            return pathName;
        }

        public static void SetPath(LBSavedData.PathType pathType, string newPathName)
        {
            if (File.Exists("LandscapeBuilder/LBSaveFile.dat"))
            {
                // If the save file already exists retrieve previously saved data
                lbSavedData = RetrieveSavedData();
                tempLBSavedData = null;
            }
            else
            {
                // If the save file doesn't already exist we will have to create a new one
                lbSavedData = new LBSavedData();
            }

            if (lbSavedData != null)
            {
                // Attempt to save the saved data to disk
                BinaryFormatter binaryFormatter = null;
                FileStream fs = null;
                try
                {
                    binaryFormatter = new BinaryFormatter();
                    fs = File.Open("LandscapeBuilder/LBSaveFile.dat", FileMode.OpenOrCreate);

                    switch (pathType)
                    {
                        case LBSavedData.PathType.HQPhotographicTexturesVol1:
                            lbSavedData.pathHQPhotographicTexturesVol1 = newPathName;
                            break;
                        case LBSavedData.PathType.HQPhotographicTexturesVol2:
                            lbSavedData.pathHQPhotographicTexturesVol2 = newPathName;
                            break;
                        case LBSavedData.PathType.FlowMapPainterExe:
                            lbSavedData.pathFlowMapPainterEXE = newPathName;
                            break;
                        case LBSavedData.PathType.RusticGrass:
                            lbSavedData.pathRusticGrass = newPathName;
                            break;
                        default:
                            // Do nothing
                            break;
                    }

                    binaryFormatter.Serialize(fs, lbSavedData);
                    fs.Close();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("ERROR: Landscape.SetPath Exception - " + ex.Message);
                }
                finally
                {
                    // Cleanup
                    if (binaryFormatter != null) { binaryFormatter = null; }
                    if (fs != null) { fs.Close(); fs.Dispose(); fs = null; }
                }
            }
            else { Debug.LogWarning("ERROR: Landscape.SetPath could retrieve saved data"); }
        }

        #endregion
    
        #endif


        #endregion

        #region MegaSplat Methods (Editor Only)

        #if UNITY_EDITOR
	    /// <summary>
	    /// Retrieve whether MegaSplat AutoClosePainter is enabled from a serialized file
	    /// </summary>
	    public static bool GetMegaSplatAutoClosePainter()
	    {
		    lbSavedData = RetrieveSavedData ();
		    tempLBSavedData = null;
		    if (lbSavedData != null) { return lbSavedData.megaSplatAutoClosePainter; }
		    else { Debug.LogWarning("ERROR: Landscape GetMegaSplatAutoClosePainter could not retrieve saved data");  return true; }
	    }

        /// <summary>
        ///  Save whether MegaSplat AutoClosePainter is enabled to a serialized file
        /// </summary>
        /// <param name="autoClosePainter"></param>
	    public static void SetMegaSplatAutoClosePainter(bool autoClosePainter)
	    {
            lbSavedData = GetSavedData();
		    if (lbSavedData != null)
		    {
			    // Attempt to save the saved data to disk
			    BinaryFormatter binaryFormatter = null;
			    FileStream fs = null;
			    try
			    {
				    binaryFormatter = new BinaryFormatter();
				    fs = File.Open("LandscapeBuilder/LBSaveFile.dat", FileMode.OpenOrCreate);
				    lbSavedData.megaSplatAutoClosePainter = autoClosePainter;
				    binaryFormatter.Serialize(fs, lbSavedData);
				    fs.Close();
			    }
			    catch (Exception ex)
			    {
				    Debug.LogWarning("ERROR: Landscape SetMegaSplatAutoClosePainter Exception - " + ex.Message);
			    }
			    finally
			    {
				    // Cleanup
				    if (binaryFormatter != null) { binaryFormatter = null; }
				    if (fs != null) { fs.Close(); fs.Dispose(); fs = null; }
			    }
		    }
		    else { Debug.LogWarning("ERROR: Landscape SetMegaSplatAutoClosePainter could retrieve saved data"); }
	    }

        #endif

        #endregion

        #region Save Retrieve Data Methods

        /// <summary>
        /// If LBSaveFile.dat exists, retrieved the data from the file
        /// else create an default instance of the class.
        /// </summary>
        /// <returns></returns>
        private static LBSavedData GetSavedData()
        {
            #if UNITY_EDITOR
            if (File.Exists("LandscapeBuilder/LBSaveFile.dat"))
            {
                // If the save file already exists retrieve previously saved data - we only want to change one thing
                lbSavedData = RetrieveSavedData();
                tempLBSavedData = null;
            }
            else
            #endif
            {
                // If the save file doesn't already exist we will have to create a new one
                lbSavedData = new LBSavedData();
            }
            return lbSavedData;
        }

        /// <summary>
        /// Retrieve all saved data from a serialized file
        /// </summary>
        private static LBSavedData RetrieveSavedData ()
	    {
		    tempLBSavedData = null;

            #if UNITY_EDITOR
            if (File.Exists("LandscapeBuilder/LBSaveFile.dat"))
		    {
			    BinaryFormatter binaryFormatter = null;
			    FileStream fs = null;
			    try
			    {
				    // Read the binary data from file in the application default data folder
				    binaryFormatter = new BinaryFormatter();

				    fs = File.Open("LandscapeBuilder/LBSaveFile.dat", FileMode.Open);
				    tempLBSavedData = (LBSavedData)binaryFormatter.Deserialize(fs);
				    fs.Close();
			    }
			    catch (Exception ex)
			    {
				    Debug.Log("ERROR: RetrieveSavedData Load: Exception - " + ex.Message);
			    }
			    finally
			    {
				    // Cleanup
				    if (binaryFormatter != null) { binaryFormatter = null; }
				    if (fs != null) { fs.Close(); fs.Dispose(); fs = null; }
			    }
		    }
		    #endif

		    return tempLBSavedData;
	    }

        #endregion

        #region Terrain Collider Methods

        /// <summary>
        /// Update terrain collider (force it to work)
        /// </summary>
        public void UpdateTerrainColliders()
        {
            if (landscapeTerrains == null)
            {
                Debug.Log("ERROR: Landscape.UpdateTerrainColliders - landscapeTerrains is not defined");
            }
            else
            {
                for (int i = 0; i < landscapeTerrains.Length; i++)
                {
                    landscapeTerrains[i].GetComponent<TerrainCollider>().terrainData = landscapeTerrains[i].terrainData;
                }
            }
        }

        #endregion

        #region Landscape Lists

        /// <summary>
        /// Returns the Topography Layers list for this landscape
        /// </summary>
        /// <returns>The topography layers list.</returns>
        public List<LBLayer> TopographyLayersList ()
	    {
		    return topographyLayersList;
	    }

	    /// <summary>
	    /// Returns the Terrain Textures list for this landscape
	    /// </summary>
	    /// <returns>The textures list.</returns>
        public List<LBTerrainTexture> TerrainTexturesList ()
	    {
		    return terrainTexturesList;
	    }

	    /// <summary>
	    /// Returns the Terrain Trees list for this landscape
	    /// </summary>
	    /// <returns>The trees list.</returns>
	    public List<LBTerrainTree> TerrainTreesList ()
	    {
		    return terrainTreesList;
	    }
	
	    /// <summary>
	    /// Returns the Terrain Grass list for this landscape
	    /// </summary>
	    /// <returns>The grass list.</returns>
	    public List<LBTerrainGrass> TerrainGrassList ()
	    {
		    return terrainGrassList;
	    }

        /// <summary>
	    /// Returns the Landscape Mesh list for this landscape
	    /// </summary>
	    /// <returns>The mesh list.</returns>
	    public List<LBLandscapeMesh> LandscapeMeshList ()
	    {
		    return landscapeMeshList;
	    }

        /// <summary>
        /// Returns the LBGroup list for this landscape
        /// </summary>
        /// <returns></returns>
        public List<LBGroup> GroupList()
        {
            return lbGroupList;
        }

        /// <summary>
        /// Get the number of terrain textures that are not disabled and have a valid Texture2D
        /// </summary>
        /// <returns></returns>
        public int GetNumActiveTerrainTextures(bool checkPrototypes)
        {
            int numTextures = 0;

            if (checkPrototypes)
            {
                SetLandscapeTerrains(false);
                if (landscapeTerrains != null)
                {
                    if (landscapeTerrains.Length > 0)
                    {
                        TerrainData tData = landscapeTerrains[0].terrainData;
                        if (tData != null)
                        {
                            #if UNITY_2018_3_OR_NEWER
                            TerrainLayer[] terrainLayers = tData.terrainLayers;
                            if (terrainLayers != null) { numTextures = terrainLayers.Length; }
                            #else
                            SplatPrototype[] splatPrototypes = tData.splatPrototypes;
                            if (splatPrototypes != null) { numTextures = splatPrototypes.Length; }
                            #endif
                        }
                    }
                }
            }
            else
            {
                numTextures = (terrainTexturesList == null ? 0 : terrainTexturesList.FindAll(tx => !tx.isDisabled && tx.texture != null).Count);
            }

            return numTextures;
        }

        /// <summary>
        /// Returns the list of unique splatphototypes (as LBTerrainTexures)
        /// from all the terrains within the landscape
        /// </summary>
        /// <returns></returns>
        public List<LBTerrainTexture> TerrainTexturesAvailableList()
        {
            List<LBTerrainTexture> availableTextures = new List<LBTerrainTexture>();
            TerrainData tData = null;
            LBTerrainTexture lbTerrainTexture = null;
            List<LBTerrainTexture> tTextureList = null;
            LBTerrainTexture existingTexture = null;

            landscapeTerrains = GetComponentsInChildren<Terrain>();

            if (landscapeTerrains != null)
            {
                // Look for existing Textures in the terrains 
                for (int t = 0; t < landscapeTerrains.Length; t++)
                {
                    tData = landscapeTerrains[t].terrainData;

                    if (tData != null)
                    {
                        // Get the textures for this terrain
                        #if UNITY_2018_3_OR_NEWER
                        tTextureList = LBTerrainTexture.ToLBTerrainTextureList(tData.terrainLayers);
                        #else
                        tTextureList = LBTerrainTexture.ToLBTerrainTextureList(tData.splatPrototypes);
                        #endif

                        if (tTextureList != null)
                        {
                           if (tTextureList.Count > 0)
                            {
                                // Look for textures that already exist in other terrains already processed
                                // Loop backwards so that we can remove matching textures
                                for (int d = tTextureList.Count - 1; d >= 0; d--)
                                {
                                    lbTerrainTexture = tTextureList[d];

                                    if (lbTerrainTexture != null)
                                    {
                                        existingTexture = availableTextures.Find(tx => tx.texture == lbTerrainTexture.texture &&
                                                                                        tx.normalMap == lbTerrainTexture.normalMap &&
                                                                                        tx.smoothness == lbTerrainTexture.smoothness &&
                                                                                        tx.metallic == lbTerrainTexture.metallic &&
                                                                                        tx.tileSize == lbTerrainTexture.tileSize);
                                        // Did we find a match?
                                        if (existingTexture != null)
                                        {
                                            // No need to add this as it was in another terrain
                                            tTextureList.Remove(lbTerrainTexture);
                                        }
                                    }
                                }

                                // Add the textures from this terrain to the available landscape textures which appear in the Texturing Tab
                                availableTextures.AddRange(tTextureList);
                            }
                        }
                        tData = null;
                    }
                }
            }
            return availableTextures;
        }

        /// <summary>
        /// When the terrain data is stored in the project folder AND the New Terrain System is used,
        /// save TerrainLayer objects in the project folder like can be done in the Unity Terrain Editor.
        /// Has no effect unless in Unity 2018.3+ and useProjectForTerrainData is true.
        /// </summary>
        public void SetTextureTerrainLayers()
        {
            #if UNITY_2018_3_OR_NEWER && UNITY_EDITOR
            // When the terrain data is stored in the project folder AND the New Terrain System is used,
            // save TerrainLayer objects in the project folder like can be done in the Unity Terrain Editor
            if (useProjectForTerrainData)
            {
                List<LBTerrainTexture> terrainTexturesListNoDisabled = LBTerrainTexture.GetActiveTextureList(terrainTexturesList);

                activeTerrainLayerList = LBTerrainTexture.ToTerrainLayerList(terrainTexturesListNoDisabled);

                int numTL = activeTerrainLayerList == null ? 0 : activeTerrainLayerList.Count;

                string tDataFolderPath = "Assets/" + terrainDataFolder;

                if (!UnityEditor.AssetDatabase.IsValidFolder(tDataFolderPath.TrimEnd('/')))
                {
                    Debug.LogWarning("ERROR: LBLandscape.SetTextureTerrainLayers - the following folder does not exist: " + tDataFolderPath);
                }
                else
                {
                    if (!tDataFolderPath.EndsWith("/")) { tDataFolderPath += "/"; }

                    for (int tl = 0; tl < numTL; tl++)
                    {
                        string tLyrFile = name + "TerrainLayer" + tl.ToString("000") + ".terrainlayer";
                    
                        //TerrainLayer tLayer = LBEditorHelper.GetAsset<TerrainLayer>(terrainDataFolder, tLyrFile);

                        UnityEditor.AssetDatabase.CreateAsset(activeTerrainLayerList[tl], tDataFolderPath + tLyrFile);
                    }

                    UnityEditor.AssetDatabase.Refresh();
                }
            }
            #endif
        }

        #endregion

        #region Landscape-scoped Methods

        #region Landscape Misc

        /// <summary>
        /// Get all the terrains in the landscape and store in public
        /// variable landscapeTerrains.
        /// </summary>
        /// <param name="forceRefresh"></param>
        public void SetLandscapeTerrains(bool forceRefresh)
        {
            if (forceRefresh || landscapeTerrains == null)
            {
                landscapeTerrains = GetComponentsInChildren<Terrain>();
            }
        }

        /// <summary>
        /// Lock or unlock the terrains from accidental movement or deletion
        /// </summary>
        /// <param name="isLocked"></param>
        public void LockTerrains(bool isLocked)
        {
            SetLandscapeTerrains(false);
            int numTerrains = (landscapeTerrains == null ? 0 : landscapeTerrains.Length);

            for (int t = 0; t < numTerrains; t++)
            {
                if (isLocked)
                {
                    // This will toggle NoEditable - not sure why &= HideFlags.NotEditable doesn't work...
                    landscapeTerrains[t].gameObject.hideFlags |= HideFlags.NotEditable;
                }
                else { landscapeTerrains[t].gameObject.hideFlags &= ~HideFlags.NotEditable; }
            }
        }

        /// <summary>
        /// Get the number of terrains in the landscape AND the number of terrains wide (in the x direction)
        /// and the number of terrains in the z direction.
        /// The values are passed in by reference and updated.
        /// Returns 0 by default
        /// PREREQUISITIES: SetLandscapeTerrains(..);
        /// </summary>
        /// <param name="numTerrains"></param>
        /// <param name="numTerrainsX"></param>
        /// <param name="numTerrainsZ"></param>
        public void GetNumTerrainsXZ(ref int numTerrains, ref int numTerrainsX, ref int numTerrainsZ)
        {
            numTerrainsX = 0;
            numTerrainsZ = 0;

            numTerrains = landscapeTerrains == null ? 0 : landscapeTerrains.Length;
            if (numTerrains > 0)
            {
                if (numTerrains == 1) { numTerrainsX = 1; numTerrainsZ = 1; }
                else if (size.x == size.y)
                {
                    numTerrainsX = Mathf.RoundToInt(Mathf.Sqrt(numTerrains));
                    numTerrainsZ = numTerrainsX;
                }
                else if (landscapeTerrains[0] != null && landscapeTerrains[0].terrainData != null)
                {
                    Vector3 terrainSize = landscapeTerrains[0].terrainData.size;
                    numTerrainsX = (int)(size.x / terrainSize.x);
                    numTerrainsZ = (int)(size.y / terrainSize.z);
                }
            }
        }

        /// <summary>
        /// Get the total number of heightmap 'pixels' from all the terrains in the landscape.
        /// Apart from a single sqrt, should be pretty fast, although it does need to get the
        /// heightmap resolution from the first terrain.
        /// If the landscape is non-square, it also needs to get the terrainSize of the first terrain.
        /// </summary>
        /// <param name="isIncludeInnerEdges"></param>
        /// <returns></returns>
        public int GetTotalHeightmapSize(bool isIncludeInnerEdges)
        {
            if (landscapeTerrains == null) { return 0; }
            else if (landscapeTerrains.Length < 1) { return 0; }
            else
            {
                //int numTerrains = landscapeTerrains.Length;
                //int numTerrainsWide = Mathf.RoundToInt(Mathf.Sqrt(numTerrains));

                int numTerrains = 0, numTerrainsWide = 0, numTerrainsDeep = 0;
                GetNumTerrainsXZ(ref numTerrains, ref numTerrainsWide, ref numTerrainsDeep);

                int heightmapResolution = GetLandscapeTerrainHeightmapResolution();

                if (numTerrainsWide == 1) { return heightmapResolution * heightmapResolution; }
                else if (isIncludeInnerEdges) { return heightmapResolution * heightmapResolution * numTerrains; }
                else
                {
                    //Debug.Log("[DEBUG] Landscpae size " + size + " terrains: " + numTerrainsWide + " x " + numTerrainsDeep);

                    return ((numTerrainsWide * (heightmapResolution - 1)) + 1) * ((numTerrainsDeep * (heightmapResolution - 1)) + 1);
                }
            }
        }

        /// <summary>
        /// Get the total number of heightmap 'pixels' from all the terrains in the landscape,
        /// at a supplied heightmapResolution, and terrainSize.
        /// Apart from a single sqrt, should be pretty fast.
        /// NOTE: Supports non-square landscapes
        /// </summary>
        /// <param name="isIncludeInnerEdges"></param>
        /// <param name="terrainHeightmapResolution"></param>
        /// <param name="terrainSize"></param>
        /// <returns></returns>
        public int GetTotalHeightmapSize(bool isIncludeInnerEdges, int terrainHeightmapResolution, Vector3 terrainSize)
        {
            if (landscapeTerrains == null) { return 0; }
            else if (landscapeTerrains.Length < 1) { return 0; }
            else
            {
                int numTerrains = landscapeTerrains.Length;
                int numTerrainsWide = Mathf.RoundToInt(Mathf.Sqrt(numTerrains));
                if (numTerrainsWide == 1) { return terrainHeightmapResolution * terrainHeightmapResolution; }
                else if (isIncludeInnerEdges) { return terrainHeightmapResolution * terrainHeightmapResolution * numTerrains; }
                // Is the landscape non-square?
                else if (size.x != size.y)
                {
                    numTerrainsWide = (int)(size.x / terrainSize.x);
                    int numTerrainsDeep = (int)(size.y / terrainSize.z);

                    //Debug.Log("[DEBUG] Landscpae size " + size + " terrains: " + numTerrainsWide + " x " + numTerrainsDeep);

                    return ((numTerrainsWide * (terrainHeightmapResolution - 1)) + 1) * ((numTerrainsDeep * (terrainHeightmapResolution - 1)) + 1);
                }
                else
                {
                    return ((numTerrainsWide * (terrainHeightmapResolution - 1)) + 1) * ((numTerrainsWide * (terrainHeightmapResolution - 1)) + 1);
                }
            }
        }

        /// <summary>
        /// Get the minimum and maximum heights of the terrains in this landscape
        /// Values are returned in metres.
        /// </summary>
        public Vector2 GetLandscapeMinMaxHeights()
        {
            Vector2 heightRange = Vector2.zero;
            Vector2 landscapeMinMaxHeight = new Vector2(float.PositiveInfinity, float.NegativeInfinity); ;

            if (this.gameObject != null)
            {
                SetLandscapeTerrains(true);

                if (landscapeTerrains != null)
                {
                    for (int index = 0; index < landscapeTerrains.Length; index++)
                    {
                        // Get the minimum and maximium heights for this terrain in metres
                        heightRange = LBLandscapeTerrain.GetTerrainHeightRange(landscapeTerrains[index].terrainData, false);

                        // If it lower and/or higher than previously tested terrains in this landscape, update values.
                        landscapeMinMaxHeight.x = Mathf.Min(heightRange.x, landscapeMinMaxHeight.x);
                        landscapeMinMaxHeight.y = Mathf.Max(heightRange.y, landscapeMinMaxHeight.y);
                    }
                }
            }

            // If the value didn't get updating, set it to 0,0
            if (landscapeMinMaxHeight == new Vector2(float.PositiveInfinity, float.NegativeInfinity))
            {
                landscapeMinMaxHeight = Vector2.zero;
            }

            return landscapeMinMaxHeight;
        }

        /// <summary>
        /// Move the landscape start position.
        /// Move MapPath path points.
        /// Move CameraPath path points.
        /// Move Manual Clearing Group positions.
        /// </summary>
        /// <param name="newStartLocation"></param>
        public void MoveLandscape(Vector3 newStartLocation)
        {
            Vector3 offsetDiff = newStartLocation - start;

            //Debug.Log("newStartLocation: " + newStartLocation + " current: " + start + " diff: " + offsetDiff);

            // Update Water
            //int lbWaterCount = (landscapeWaterList == null ? 0 : landscapeWaterList.Count);

            // Find water in landscape
            //Transform waterTransform = LBWaterOperations.FindWaterInLandscape(this.gameObject, lbWater);


            // Update MapPaths
            List<LBMapPath> allMapPathList = LBMapPath.GetMapPathsInLandscape(this);
            LBMapPath lbMapPath;

            int numMapPaths = (allMapPathList == null ? 0 : allMapPathList.Count);

            for (int mpIdx = 0; mpIdx < numMapPaths; mpIdx++)
            {
                lbMapPath = allMapPathList[mpIdx];
                if (lbMapPath != null && lbMapPath.lbPath != null)
                {
                    lbMapPath.lbPath.MovePoints(offsetDiff, Vector3.one);
                }
            }

            // Update Camera Paths
            List<LBCameraPath> allCameraPathList = LBCameraPath.GetCameraPathsInLandscape(this);
            LBCameraPath lbCameraPath;

            int numCameraPaths = (allCameraPathList == null ? 0 : allCameraPathList.Count);

            for (int cpIdx = 0; cpIdx < numCameraPaths; cpIdx++)
            {
                lbCameraPath = allCameraPathList[cpIdx];
                if (lbCameraPath != null && lbCameraPath.lbPath != null)
                {
                    lbCameraPath.lbPath.MovePoints(offsetDiff, Vector3.one);
                }
            }

            // Update Manual Groups and Object Path Points
            LBGroup.LandscapeDimensionLocationChanged(lbGroupList, offsetDiff, Vector3.one);

            start = newStartLocation;
        }

        /// <summary>
        /// This is a little subjective and will change based on hardware but it might
        /// help in making performance judgements in code. Like when recommending to
        /// disable auto-save.
        /// </summary>
        /// <returns></returns>
        public bool IsBigHeightmap()
        {
            bool isBig = false;

            SetLandscapeTerrains(false);

            int numTerrains = (landscapeTerrains == null ? 0 : landscapeTerrains.Length);

            if (numTerrains > 0)
            {
                int hmapTotal = numTerrains * GetLandscapeTerrainHeightmapResolution();
                
                // If there are more than 16 terrains with 513 resolution (or equivalent), then consider big.
                isBig = (hmapTotal >= (513 * 16));
            }

            return isBig;
        }

        #endregion

        #region ApplyTopography

        /// <summary>
        /// Generate the Topography at runtime.
        /// RUNTIME USAGE
        ///   SetLandscapeTerrains(true);
        ///   ApplyTopography(false, false);
        /// NOTE: Does not support the legacy NoiseGeneratorType.ValueBased
        /// </summary>
        /// <param name="showErrors"></param>
        public void ApplyTopography(bool bypassValidation, bool showErrors)
        {
            if (landscapeTerrains == null) { if (showErrors) { Debug.LogWarning("LBLandscape.ApplyTopography - the array of terrains is not defined for this landscape"); } return; }

            bool InvalidTextures = false;

            if (!bypassValidation)
            {

                // If using images, ensure they are enabled for read/write    
                for (int l = 0; l < topographyLayersList.Count; l++)
                {
                    if (topographyLayersList[l].type == LBLayer.LayerType.ImageBase || topographyLayersList[l].type == LBLayer.LayerType.ImageAdditive ||
                        topographyLayersList[l].type == LBLayer.LayerType.ImageSubtractive || topographyLayersList[l].type == LBLayer.LayerType.ImageDetail)
                    {
                        if (topographyLayersList[l].heightmapImage != null)
                        {
                            // Don't attempt to fix unreadable textures (only works in Unity Editor)
                            InvalidTextures = !LBTextureOperations.IsTextureReadable(topographyLayersList[l].heightmapImage, false);
                            if (InvalidTextures) { break; }
                        }
                        else { Debug.LogWarning("GenerateTopography - Layer " + (l + 1).ToString() + " heightmap Image is null"); }

                        if (!InvalidTextures && topographyLayersList[l].imageRepairHoles)
                        {
                            topographyLayersList[l].processingImage = LBTextureOperations.RemoveHoles(topographyLayersList[l].heightmapImage, topographyLayersList[l].threshholdRepairHoles);
                        }
                    }

                    // Check for Map filters
                }
            }

            float generationStartTime = Time.realtimeSinceStartup;

            for (int index = 0; index < landscapeTerrains.Length && !InvalidTextures; index++)
            {

                landscapeTerrains[index].terrainData =
                    LBLandscapeTerrain.HeightmapFromLayers(this, landscapeTerrains[index].terrainData,
                                                            landscapeTerrains[index].transform.position, this.size, this.transform.position,
                                                            topographyLayersList);

                // Apply mask if needed
                int maskModeInt = 0;
                if (this.topographyMaskMode == LBLandscape.MaskMode.DistanceToCentre) { maskModeInt = 1; }
                else if (this.topographyMaskMode == LBLandscape.MaskMode.Noise) { maskModeInt = 2; }
                if (maskModeInt != 0)
                {
                    landscapeTerrains[index].terrainData =
                        LBLandscapeTerrain.MaskedHeightmap(landscapeTerrains[index].terrainData,
                                                           landscapeTerrains[index].transform.position, this.size, this.transform.position,
                                                           maskModeInt, this.distanceToCentreMask, this.maskWarpAmount, this.maskNoiseTileSize,
                                                           new Vector2(this.maskNoiseOffsetX, this.maskNoiseOffsetY), this.maskNoiseCurveModifier);
                }
            }

            // Added 2.1.0 Beta 4k
            if (useThermalErosion || useFinalPassSmoothing)
            {
                // TODO - convert final pass parameters into a class
                LBLandscapeTerrain.FinalPassHeightmap(this, this.transform.position, false, 0, 0f, 0f, 0f, 0f, false, null, true);
            }

            // Topography Layer smoothing (since v1.3.2 beta 2a) is a post-topography process
            if (!InvalidTextures)
            {
                for (int index = 0; index < landscapeTerrains.Length; index++)
                {
                    landscapeTerrains[index].terrainData = LBLandscapeTerrain.SmoothHeightmapFromLayers(this, landscapeTerrains[index].terrainData, landscapeTerrains[index].transform.position, topographyLayersList);
                }
            }

            // We need to update the terrain LOD and vegetation information
            LBLandscapeTerrain.ApplyDelayedHeightmapLOD(landscapeTerrains);

            // Workaround for Smoothing terrain corner issue (only apply if required)
            if (!InvalidTextures && landscapeTerrains.Length > 1 && topographyLayersList != null)
            {
                bool fixedEdges = false;
                foreach (LBLayer lbLayer in topographyLayersList)
                {
                    if (lbLayer != null)
                    {
                        if (lbLayer.isDisabled) { continue; }

                        if (lbLayer.type == LBLayer.LayerType.ImageAdditive || lbLayer.type == LBLayer.LayerType.ImageBase ||
                            lbLayer.type == LBLayer.LayerType.ImageDetail)
                        {
                            if (lbLayer.detailSmoothRate > 0.001f)
                            {
                                LBLandscapeTerrain.FixTerrainEdges(this, landscapeTerrains);
                                fixedEdges = true;
                                break;
                            }
                        }

                        // Check for any layer filters that use smoothing
                        if (lbLayer.filters != null && (lbLayer.type == LBLayer.LayerType.PerlinAdditive || lbLayer.type == LBLayer.LayerType.PerlinSubtractive ||
                            lbLayer.type == LBLayer.LayerType.PerlinDetail || lbLayer.type == LBLayer.LayerType.ImageDetail))
                        {
                            foreach (LBLayerFilter lbLayerFilter in lbLayer.filters)
                            {
                                if (lbLayerFilter.type == LBLayerFilter.LayerFilterType.Map && lbLayerFilter.map != null && lbLayerFilter.smoothRate > 0.01f)
                                {
                                    LBLandscapeTerrain.FixTerrainEdges(this, landscapeTerrains);
                                    fixedEdges = true;
                                    break;
                                }
                            }
                        }

                        // Check for any MapPath layers that use smoothing
                        if (!fixedEdges && lbLayer.type == LBLayer.LayerType.MapPath && lbLayer.detailSmoothRate > 0.001f)
                        {
                            LBLandscapeTerrain.FixTerrainEdges(this, landscapeTerrains);
                            fixedEdges = true;
                            break;
                        }

                        // If we fixed the edges stop processing layers
                        if (fixedEdges) { break; }
                    }
                }
            }

            // If Stencil Layer Filters where used, free up any extra memory allocated
            LBStencil.FreeStencilResources(this, true);

            this.UpdateTerrainColliders();

            // Update the Object Path Y positions after the topography has changed if snapToTerrain is enabled
            LBGroup.ObjPathSnapToTerrain(this, lbGroupList);

            #if VEGETATION_STUDIO_PRO
            if (useVegetationSystem)
            {
                LBIntegration.VegetationStudioProRefresh(true);
            }
            #endif

            //if (terrainMaterialType == LBLandscape.TerrainMaterialType.ReliefTerrainPack && this.rtpUseTessellation)
            //{
            //    LBIntegration.RTPUpdateTessellationMaps(this, landscapeTerrains, true);
            //}

            if (this.showTiming)
            {
                Debug.Log("Time taken to generate topography: " + (Time.realtimeSinceStartup - generationStartTime).ToString("00.00") + " seconds.");
            }
        }

        #endregion

        #region ApplyTextures

        /// <summary>
        /// Apply textures in terrainTexturesList to all the terrains in the landscape
        /// RUNTIME USAGE
        ///   SetLandscapeTerrains(true);
        ///   ApplyTextures(false, false);
        /// WARNING: Performs minimal error checking. Assumes all component, filters, normalmaps, heightmaps, maps etc are valid.
        /// NOTE: Does not include any progress indicator and is designed for runtime use.
        /// </summary>
        /// <param name="showErrors"></param>
        public void ApplyTextures(bool bypassValidation, bool showErrors)
        {
            if (landscapeTerrains == null) { if (showErrors) { Debug.LogWarning("LBLandscape.ApplyTextures - the array of terrains is not defined for this landscape"); } }
            else
            {
                bool hasInvalidTexture = false;

                // Validating NormalMaps and Heightmaps is currently an EDITOR ONLY feature

                if (!bypassValidation)
                {
                    // Validate any Textures that use the Map constraint have valid readable texture maps
                    for (int index = 0; index < terrainTexturesList.Count; index++)
                    {
                        if (terrainTexturesList[index].texturingMode == LBTerrainTexture.TexturingMode.Map ||
                            terrainTexturesList[index].texturingMode == LBTerrainTexture.TexturingMode.HeightInclinationMap)
                        {
                            if (terrainTexturesList[index].map != null)
                            {
                                hasInvalidTexture = !LBTextureOperations.IsTextureReadable(terrainTexturesList[index].map, false);
                                if (hasInvalidTexture) { break; }
                            }
                        }
                    }
                }

                if (!hasInvalidTexture)
                {
                    // Texture the terrains
                    for (int t = 0; t < landscapeTerrains.Length; t++)
                    {
                        // Use the LBLandscapeTerrain.TextureTerrain function for texturing the terrain
                        landscapeTerrains[t].terrainData = LBLandscapeTerrain.TextureTerrain(landscapeTerrains[t].terrainData, terrainTexturesList,
                            landscapeTerrains[t].transform.position, this.size, this.transform.position, false, this);
                    }

                    // If Stencil Layer Filters where used, free up any extra memory allocated
                    LBStencil.FreeStencilResources(this, true);

                    this.UpdateTerrainColliders();
                }
            }
        }

        #endregion

        #region ApplyTrees

        /// <summary>
        /// Apply Tree Types in terrainTreesList to all the terrains in the landscape.
        /// RUNTIME USAGE
        ///   SetLandscapeTerrains(true);
        ///   ApplyTrees(true, false);
        /// NOTE: Does not include any progress indicator and is designed for runtime use.
        /// </summary>
        /// <param name="showErrors"></param>
        public void ApplyTrees(bool bypassValidation, bool showErrors)
        {
            if (terrainTreesList == null) { if (showErrors) { Debug.LogWarning("LBLandscape.ApplyTrees - terrainTreesList is not defined"); } }
            else if (landscapeTerrains == null) { if (showErrors) { Debug.LogWarning("LBLandscape.ApplyTrees - the array of terrains is not defined in this landscape"); } }
            else
            {
                // Turn off the showArea indicator in any filters to prevent layout errors
                foreach (LBTerrainTree lbTerrainTree in terrainTreesList)
                {
                    if (lbTerrainTree.filterList != null)
                    {
                        foreach (LBFilter lbFilter in lbTerrainTree.filterList)
                        {
                            lbFilter.showAreaHighlighter = false;
                        }
                    }
                }

                bool hasInvalidTexture = false;

                if (!bypassValidation)
                {
                    // Validate any trees that use the Map constraint have valid readable texture maps
                    for (int index = 0; index < terrainTreesList.Count; index++)
                    {
                        if (terrainTreesList[index].treePlacingMode == LBTerrainTree.TreePlacingMode.Map ||
                            terrainTreesList[index].treePlacingMode == LBTerrainTree.TreePlacingMode.HeightInclinationMap)
                        {
                            if (terrainTreesList[index].map != null)
                            {
                                // Don't attempt to fix unreadable textures (only works in Unity Editor)
                                hasInvalidTexture = !LBTextureOperations.IsTextureReadable(terrainTreesList[index].map, false);
                                if (hasInvalidTexture) { break; }
                            }
                        }
                    }
                }

                if (!hasInvalidTexture)
                {
                    float generationStartTime = Time.realtimeSinceStartup;
                    for (int index = 0; index < landscapeTerrains.Length; index++)
                    {
                        landscapeTerrains[index].terrainData = LBLandscapeTerrain.PopulateTerrainWithTrees(landscapeTerrains[index].terrainData, terrainTreesList, maxTreesPerSquareKilometre, landscapeTerrains[index].transform.position, size, transform.position, treePlacementSpeed, showTiming, this);
                    }

                    // If Stencil Layer Filters where used, free up any extra memory allocated
                    LBStencil.FreeStencilResources(this, true);

                    UpdateTerrainColliders();

                    if (showTiming)
                    {
                        Debug.Log("Time taken to place trees: " + (Time.realtimeSinceStartup - generationStartTime).ToString("00.00") + " seconds.");
                    }
                }
                else { if (showErrors) { Debug.LogWarning("LBLandscape.ApplyTrees - invalid or unreadable Map textures found."); } }
            }
        }

        #endregion

        #region ApplyGrass

        /// <summary>
        /// Apply Grass types in terrainGrassList to all the terrains in the landscape.
        /// RUNTIME USAGE
        ///   SetLandscapeTerrains(true);
        ///   ApplyGrass(true, false);
        /// NOTE: Does not include any progress indicator and is designed for runtime use.
        /// </summary>
        /// <param name="showErrors"></param>
        public void ApplyGrass(bool bypassValidation, bool showErrors)
        {
            if (landscapeTerrains == null) { if (showErrors) { Debug.LogWarning("LBLandscape.ApplyGrass - the array of terrains is not defined for this landscape"); } }

            // Turn off the showArea indicator in any filters to prevent layout errors
            foreach (LBTerrainGrass lbTerrainGrass in terrainGrassList)
            {
                if (lbTerrainGrass.filterList != null)
                {
                    foreach (LBFilter lbFilter in lbTerrainGrass.filterList)
                    {
                        lbFilter.showAreaHighlighter = false;
                    }
                }
            }

            bool hasInvalidTexture = false;

            if (!bypassValidation)
            {
                // Validate any grass that use the Map constraint have valid readable texture maps
                for (int index = 0; index < terrainGrassList.Count; index++)
                {
                    if (terrainGrassList[index].grassPlacingMode == LBTerrainGrass.GrassPlacingMode.Map ||
                        terrainGrassList[index].grassPlacingMode == LBTerrainGrass.GrassPlacingMode.HeightInclinationMap)
                    {
                        if (terrainGrassList[index].map != null)
                        {
                            // Don't attempt to fix unreadable textures (only works in Unity Editor)
                            hasInvalidTexture = !LBTextureOperations.IsTextureReadable(terrainGrassList[index].map, false);
                            if (hasInvalidTexture) { break; }
                        }
                    }
                }
            }

            if (!hasInvalidTexture)
            {
                float generationStartTime = Time.realtimeSinceStartup;
                for (int index = 0; index < landscapeTerrains.Length; index++)
                {
                    landscapeTerrains[index].terrainData = LBLandscapeTerrain.PopulateTerrainWithGrass(landscapeTerrains[index].terrainData, terrainGrassList, landscapeTerrains[index].transform.position, this.size, this.transform.position, this.showTiming, this);
                }

                // If Stencil Layer Filters where used, free up any extra memory allocated
                LBStencil.FreeStencilResources(this, true);

                this.UpdateTerrainColliders();

                if (this.showTiming)
                {
                    Debug.Log("Time taken to place grass: " + (Time.realtimeSinceStartup - generationStartTime).ToString("00.00") + " seconds.");
                }
            }
        }

        #endregion

        #region ApplyMeshes

        /// <summary>
        /// Apply the meshes (or prefabs) to the current landscape
        /// CURRENTLY NOT TESTED
        /// RUNTIME USAGE
        ///   SetLandscapeTerrains(true);
        ///   ApplyMeshes(false, true);
        /// NOTE: Does not include any progress indicator and is designed for runtime use.
        /// </summary>
        /// <param name="showErrors"></param>
        public void ApplyMeshes(bool bypassValidation, bool showErrors)
        {
            if (landscapeTerrains == null) { if (showErrors) { Debug.LogWarning("LBLandscape.ApplyMeshes - the array of terrains is not defined for this landscape"); } }

            // Turn off the showArea indicator in any filters to prevent layout errors
            foreach (LBLandscapeMesh lbLandscapeMesh in landscapeMeshList)
            {
                if (lbLandscapeMesh.filterList != null)
                {
                    foreach (LBFilter lbFilter in lbLandscapeMesh.filterList)
                    {
                        lbFilter.showAreaHighlighter = false;
                    }
                }
            }

            bool hasInvalidTexture = false;

            if (!bypassValidation)
            {
                // Validate any meshes that use the Map constraint have valid readable texture maps
                for (int index = 0; index < landscapeMeshList.Count; index++)
                {
                    if (landscapeMeshList[index].meshPlacingMode == LBLandscapeMesh.MeshPlacingMode.Map ||
                        landscapeMeshList[index].meshPlacingMode == LBLandscapeMesh.MeshPlacingMode.HeightInclinationMap)
                    {
                        if (landscapeMeshList[index].map != null)
                        {
                            // Don't attempt to fix unreadable textures (only works in Unity Editor)
                            hasInvalidTexture = !LBTextureOperations.IsTextureReadable(landscapeMeshList[index].map, false);
                            if (hasInvalidTexture) { break; }
                        }
                    }
                }
            }

            if (!hasInvalidTexture)
            {
                // Delete all mesh prefab parent gameobjects from the landscape
                // These gameobjects contain all previously placed prefabs
                LBLandscapeTerrain.RemoveExistingPrefabs(this, true, LBPrefabItem.PrefabItemType.LegacyMeshPrefab, null);

                // Reset the counts (no undo on these)
                this.numberOfMeshes = 0;
                this.numberOfMeshPrefabs = 0;

                float generationStartTime = Time.realtimeSinceStartup;
                for (int index = 0; index < landscapeTerrains.Length; index++)
                {
                    landscapeTerrains[index].terrainData = LBLandscapeTerrain.PopulateTerrainWithMeshes(this, landscapeTerrains[index].terrainData, landscapeTerrains[index].transform,
                                                                                                        this.transform, landscapeMeshList, landscapeTerrains[index].transform.position,
                                                                                                        this.size, this.transform.position, this.showTiming, landscapeTerrains[index].name);
                }

                // We need to update the terrain LOD and vegetation information
                LBLandscapeTerrain.ApplyDelayedHeightmapLOD(landscapeTerrains);

                // If Stencil Layer Filters where used, free up any extra memory allocated
                LBStencil.FreeStencilResources(this, true);

                this.UpdateTerrainColliders();

                LBLandscapeMeshController lmc = this.gameObject.GetComponent<LBLandscapeMeshController>();
                if (lmc == null) { lmc = this.gameObject.AddComponent<LBLandscapeMeshController>(); }
                lmc.RemoveExistingCombinedMeshes("LB Combined Mesh");
                if (!lmc.BuildCombinedMeshes(landscapeMeshList))
                {
                    // If no combined meshes were created we don't need the MeshController component
                    DestroyImmediate(lmc);
                }

                if (this.showTiming)
                {
                    Debug.Log("Time taken to place meshes: " + (Time.realtimeSinceStartup - generationStartTime).ToString("00.00") + " seconds.");
                }
            }
        }

        #endregion

        #region ApplyGroups

        /// <summary>
        /// Apply the LBGroups to the current landscape
        /// RUNTIME USAGE
        ///   ApplyGroups(false, true);
        /// </summary>
        public void ApplyGroups(bool doBackup, bool showProgress)
        {
            // Only give option to show progress bar if in the editor.
            // Only provide show progress call-back delegate option if in the editor
            #if UNITY_EDITOR
            ShowProgressDelegate showProgressDelegate = null;
            if (showProgress)
            {
                UnityEditor.EditorUtility.ClearProgressBar();
                showProgressDelegate = new ShowProgressDelegate(LBLandscape.ShowProgressBar);
            }
            // Keep compiler happy
            else { if (showProgressDelegate != null) { } }
            #endif

            SetLandscapeTerrains(true);
        
            #if UNITY_EDITOR
            if (doBackup && showProgress)
            { 
                UnityEditor.EditorUtility.DisplayProgressBar("Backing up " + (isUndoTopographyDisabled ? "" : "heightmap, ") + "tex, trees and grass for group placement", "Please Wait", 0.5f);
            }
            #endif

            if (doBackup)
            {
                SaveData(LBLandscape.UndoType.Textures);
                SaveData(LBLandscape.UndoType.Trees);
                SaveData(LBLandscape.UndoType.Grass);
                if (!isUndoTopographyDisabled) { SaveData(LBLandscape.UndoType.HeightMap); }
            }

            // Delete all GroupMember prefab parent gameobjects from the landscape
            // These gameobjects contain all previously placed prefabs that used Groups
            LBLandscapeTerrain.RemoveExistingPrefabs(this, true, LBPrefabItem.PrefabItemType.GroupMemberPrefab, null);
            LBLandscapeTerrain.RemoveExistingPrefabs(this, true, LBPrefabItem.PrefabItemType.ObjPathPrefab, null);

            // Groups and zones are converted to Vegetation Studio Pro VegetatioMaskAreas and/or Biome Mask Areas.
            // Even if VSPro is not enabled for this landscape, should check if there are any in case
            // it was used previously and now has been turned off.
            #if VEGETATION_STUDIO_PRO
            LBLandscapeTerrain.RemoveExistingPrefabs(this, true, LBPrefabItem.PrefabItemType.VSProVegMaskArea, null);
            LBLandscapeTerrain.RemoveExistingPrefabs(this, true, LBPrefabItem.PrefabItemType.VSProBiomeMaskArea, null);
            #endif

            // Remove any (old) controllers that may be in the scene after some sort of failure
            LBTerrainMeshController.RemoveTerrainMeshControllers(this.transform);

            // Reset the prefab counter (no undo on these)
            numberOfGroupPrefabs = 0;

            #if UNITY_EDITOR
            if (showProgress)
            { 
                UnityEditor.EditorUtility.ClearProgressBar();
            }
            #endif

            LBGroupParameters lbGroupParm = new LBGroupParameters();
            lbGroupParm.landscape = this;
            lbGroupParm.isGroupDesignerEnabled = false;
            lbGroupParm.designerOffsetY = 0f;
            #if UNITY_EDITOR
            lbGroupParm.showErrors = true;
            lbGroupParm.showProgress = showProgress;
            lbGroupParm.showProgressDelegate = showProgressDelegate;
            #endif

            if (!LBLandscapeTerrain.PopulateLandscapeWithGroups(lbGroupParm))
            {
                if (doBackup)
                {
                    // Failed so roll-back all changes
                    if (!isUndoTopographyDisabled) { RevertToLastSave(LBLandscape.UndoType.HeightMap); }
                    RevertToLastSave(LBLandscape.UndoType.Textures);
                    RevertToLastSave(LBLandscape.UndoType.Trees);
                    RevertToLastSave(LBLandscape.UndoType.Grass);
                }
            }

            #if UNITY_EDITOR
            // Is this really necessary???
            if (showProgress)
            {
                UnityEditor.EditorUtility.ClearProgressBar();

                if (UnityEditor.EditorUtility.DisplayCancelableProgressBar("Populating Landscape With Groups", "Editing landscape", 0.5f))
                {
                    if (doBackup)
                    {
                        // If the user cancels the process, revert to the last save
                        if (!isUndoTopographyDisabled) { RevertToLastSave(LBLandscape.UndoType.HeightMap); }
                        RevertToLastSave(LBLandscape.UndoType.Textures);
                        RevertToLastSave(LBLandscape.UndoType.Trees);
                        RevertToLastSave(LBLandscape.UndoType.Grass);
                    }
                }
            }
            #endif

            #if UNITY_EDITOR
            if (showProgress)
            {
                UnityEditor.EditorUtility.ClearProgressBar();
                UnityEditor.EditorUtility.DisplayProgressBar("Populating Landscape With Groups", "Finalising - Please Wait", 0.7f);
            }
            #endif

            // We need to update the terrain LOD and vegetation information
            LBLandscapeTerrain.ApplyDelayedHeightmapLOD(landscapeTerrains);

            // If required update tree data in Vegetation Studio / VS Pro
            #if VEGETATION_STUDIO
            if (useVegetationSystem)
            {
                #if UNITY_EDITOR
                if (showProgress)
                {
                    UnityEditor.EditorUtility.ClearProgressBar();
                    UnityEditor.EditorUtility.DisplayProgressBar("Updating Vegetation Studio Tree data", "Please Wait", 0.72f);
                }
                #endif

                LBIntegration.VegetationStudioImportTrees(this, true);
            }
            #elif VEGETATION_STUDIO_PRO
            if (useVegetationSystem)
            {
                #if UNITY_EDITOR
                if (showProgress)
                {
                    UnityEditor.EditorUtility.ClearProgressBar();
                    UnityEditor.EditorUtility.DisplayProgressBar("Updating Vegetation Studio Pro Tree data", "Please Wait", 0.72f);
                }
                #endif
                LBIntegration.VegetationStudioProImportTrees(this, true);
                LBIntegration.VegetationStudioProRefresh(true);
            }
            #endif

            #if UNITY_EDITOR
            if (showProgress)
            {
                UnityEditor.EditorUtility.DisplayProgressBar("Populating Landscape With Groups", "Finalising - Please Wait", 0.75f);
            }
            #endif

            // If Stencil Layer Filters where used, free up any extra memory allocated
            LBStencil.FreeStencilResources(this, true);

            UpdateTerrainColliders();
        
            #if UNITY_EDITOR
            if (showProgress)
            {
                UnityEditor.EditorUtility.DisplayProgressBar("Populating Landscape With Groups", "Finalising - Please Wait", 0.8f);
            }
            #endif

            try
            {
                LBLandscapeMeshController lmc = this.gameObject.GetComponent<LBLandscapeMeshController>();
                if (lmc == null) { lmc = this.gameObject.AddComponent<LBLandscapeMeshController>(); }
                if (lmc != null)
                {
                    lmc.RemoveExistingCombinedMeshes("LB Combined Group Mesh");
                    if (!lmc.BuildCombinedMeshes(lbGroupParm))
                    {
                        // If no combined prefab meshes were created we don't need the MeshController component
                        DestroyImmediate(lmc);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("ERROR: ApplyGroups - " + ex.Message);
            }

            #if UNITY_EDITOR
            if (showProgress) { UnityEditor.EditorUtility.ClearProgressBar(); }
            #endif
        }

        #endregion

        #region ApplyLayerWater

        /// <summary>
        /// Apply the Topography Layer water to the landscape
        /// </summary>
        /// <param name="showProgress"></param>
        /// <param name="showErrors"></param>
        public void ApplyLayerWater(bool showProgress, bool showErrors)
        {
            string methodName = "LBLandscape.ApplyLayerWater";

            if (landscapeTerrains == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - the array of terrains is not defined for this landscape."); } return; }
            else if (topographyLayersList == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - topographyLayersList is not defined for this landscape."); } return; }
            else if (topographyLayersList.Count > 0)
            {

                #if UNITY_EDITOR
                if (showProgress) { UnityEditor.EditorUtility.ClearProgressBar(); }
                #endif

                SetLandscapeTerrains(true);
        
                #if UNITY_EDITOR
                if (showProgress)
                { 
                    UnityEditor.EditorUtility.DisplayProgressBar("Populating Landscape With Water", "Please Wait", 0.1f);
                }
                #endif

                LBLayer lbLayer = null;
                bool isMeshBuilt = false;

                for (int l = 0; l < topographyLayersList.Count; l++)
                {
                    lbLayer = topographyLayersList[l];

                    // Does this Topography Layer contain water?
                    if (!lbLayer.isDisabled && lbLayer.type == LBLayer.LayerType.ImageModifier && lbLayer.modifierUseWater && lbLayer.modifierWaterLBMesh != null)
                    {
                        if (lbLayer.modifierWaterLBMesh.mesh == null) { lbLayer.modifierWaterLBMesh.mesh = new Mesh(); }

                        if (!lbLayer.modifierWaterLBMesh.IsMeshDataValid()) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - LBMesh for Layer " + (l + 1) + " water does not look valid. Please Report."); } }
                        else if (lbLayer.modifierLBWater == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - LBWater is null for Layer " + (l + 1) + ". Please Report."); } }
                        else if (lbLayer.modifierWaterLBMesh.mesh == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not create new (water) Mesh for Layer " + (l+1)); } }
                        else
                        {
                            lbLayer.modifierWaterLBMesh.title = "Layer Water " + lbLayer.modifierLBWater.GUID;
                            lbLayer.modifierWaterLBMesh.mesh.name = lbLayer.modifierWaterLBMesh.title;

                            // Assign verts, triangle to new Unity Mesh and recalc bounds and tangents
                            isMeshBuilt = lbLayer.modifierWaterLBMesh.UpdateMesh(false, showErrors);

                            if (isMeshBuilt)
                            {
                                float terrainHeight = GetLandscapeTerrainHeight();

                                Vector3 waterPosition = CalcLayerWaterPosition(lbLayer, terrainHeight);

                                //Vector2 waterSize = new Vector2(lbLayer.modifierWaterLBMesh.mesh.bounds.size.x, lbLayer.modifierWaterLBMesh.mesh.bounds.size.z);

                                //Debug.Log("INFO: " + methodName + " waterPosition:" + waterPosition + " lbLayer.modifierWaterLBMesh.mesh.bounds.size:" + lbLayer.modifierWaterLBMesh.mesh.bounds.size + " lbLayer.floorOffsetY:" + lbLayer.floorOffsetY + " terrainHeight:"+ terrainHeight);

                                Transform meshTransform = LBMeshOperations.AddMeshToScene(lbLayer.modifierWaterLBMesh, waterPosition, lbLayer.modifierWaterLBMesh.mesh.name, this.transform, lbLayer.modifierLBWater.waterMaterial, true, true);

                                lbLayer.modifierWaterTransform = meshTransform;

                                if (meshTransform != null)
                                {
                                    // Set the rotation (reset first as RotateAround changes the existing transform)
                                    meshTransform.rotation = Quaternion.identity;
                                    // Rotate around the centre of the modifier mesh
                                    meshTransform.RotateAround(new Vector3(lbLayer.areaRect.x, 0f, lbLayer.areaRect.y) + this.transform.position, Vector3.up, lbLayer.areaRectRotation);

                                    // Populate the paramaters to pass to AddWaterToMesh()
                                    LBWaterParameters lbWaterParms = new LBWaterParameters();
                                    lbWaterParms.landscape = this;
                                    lbWaterParms.landscapeGameObject = this.gameObject;
                                    lbWaterParms.waterTransform = meshTransform;
                                    lbWaterParms.waterPosition = waterPosition;
                                    //lbWaterParms.waterSize = waterSize;
                                    lbWaterParms.waterIsPrimary = false;
                                    lbWaterParms.waterHeight = 0f;
                                    lbWaterParms.waterPrefab = null;
                                    lbWaterParms.keepPrefabAspectRatio = true;
                                    lbWaterParms.waterMeshResizingMode = lbLayer.modifierLBWater.meshResizingMode;
                                    lbWaterParms.waterMainCamera = lbLayer.modifierWaterMainCamera;
                                    //lbWaterParms.waterCausticsPrefabList = waterCausticsPrefabList;
                                    lbWaterParms.isRiver = true;
                                    lbWaterParms.riverMaterial = lbLayer.modifierLBWater.waterMaterial;
                                    lbWaterParms.lbLighting = GameObject.FindObjectOfType<LBLighting>();

                                    // Some water assets (like AQUAS) require a reflection probe
                                    // Place it at the second point in the path
                                    lbWaterParms.reflectionProbePosition = waterPosition;

                                    // We already have an LBWater attached to LBLayer. Need to determine if we
                                    // should update the LBLayer with the returned LBWater class instance
                                    LBWater addedWater = LBWaterOperations.AddWaterToMesh(lbWaterParms);
                                    if (addedWater != null)
                                    {
                                        //Debug.Log("INFO: " + methodName + " water added for " + lbLayer.modifierWaterLBMesh.title);
                                    }
                                }
                            }
                        }
                    }
                }

                #if UNITY_EDITOR
                if (showProgress) { UnityEditor.EditorUtility.ClearProgressBar(); }
                #endif

            }
        }

        /// <summary>
        /// After a Layer Water mesh has been created in the scene, this method can be used
        /// to update it position
        /// </summary>
        /// <param name="lbLayer"></param>
        /// <param name="terrainHeight"></param>
        public void UpdateLayerWaterTransform(LBLayer lbLayer, float terrainHeight)
        {
            if (lbLayer != null && lbLayer.modifierUseWater && lbLayer.modifierLBWater != null && lbLayer.modifierWaterLBMesh != null)
            {
                // If we haven't cached the water transform, do it now
                if (lbLayer.modifierWaterTransform == null) { lbLayer.modifierWaterTransform = this.transform.Find(lbLayer.modifierWaterLBMesh.title); }

                if (lbLayer.modifierWaterTransform != null)
                {
                    Vector3 waterPosition = CalcLayerWaterPosition(lbLayer, terrainHeight);

                    lbLayer.modifierWaterTransform.position = waterPosition;

                    // Set the rotation (reset first as RotateAround changes the existing transform)
                    lbLayer.modifierWaterTransform.rotation = Quaternion.identity;
                    // Rotate around the centre of the modifier mesh
                    lbLayer.modifierWaterTransform.RotateAround(new Vector3(lbLayer.areaRect.x, 0f, lbLayer.areaRect.y) + this.transform.position, Vector3.up, lbLayer.areaRectRotation);
                }
            }
        }

        /// <summary>
        /// Calculate the position of the Topography Layer water in world space
        /// </summary>
        /// <param name="lbLayer"></param>
        /// <param name="terrainHeight"></param>
        /// <returns></returns>
        public Vector3 CalcLayerWaterPosition(LBLayer lbLayer, float terrainHeight)
        {
            Vector3 waterPosition = Vector3.zero;

            if (lbLayer != null && lbLayer.modifierLBWater != null)
            {
                Vector3 areaRectCentre = new Vector3(lbLayer.areaRect.x, 0f, lbLayer.areaRect.y) + this.transform.position;

                // areaRect x,y are at the centre of the area for the ImageModifiers.
                // Add the landscape gameobject position as the layer areaRect is the location within the current landscape (not world space)
                waterPosition = new Vector3(areaRectCentre.x - (lbLayer.areaRect.width / 2f), 0f, areaRectCentre.z - (lbLayer.areaRect.height / 2f));

                // Calculate the position of the water on the y-axis in worldspace
                if (lbLayer.modifierMode == LBLayer.LayerModifierMode.Add)
                {
                    waterPosition.y = lbLayer.additiveAmount * terrainHeight;
                }
                else
                {
                    waterPosition.y = lbLayer.floorOffsetY * terrainHeight;
                }

                waterPosition.y = terrainHeight - waterPosition.y + lbLayer.modifierLBWater.waterLevel;
            }

            return waterPosition;
        }

        /// <summary>
        /// Resize the Layer ImageModifier water (if there is any water)
        /// </summary>
        /// <param name="lbLayer"></param>
        /// <param name="layerIdx"></param>
        /// <param name="terrainHeight"></param>
        /// <param name="showErrors"></param>
        public void ResizeLayerWater(LBLayer lbLayer, int layerIdx, float terrainHeight, bool showErrors)
        {
            string methodName = "LBLandscape.ResizeLayerWater";

            if (lbLayer != null && lbLayer.modifierUseWater && lbLayer.modifierLBWater != null && lbLayer.modifierWaterLBMesh != null)
            {
                // If we haven't cached the water transform, do it now
                if (lbLayer.modifierWaterTransform == null) { lbLayer.modifierWaterTransform = this.transform.Find(lbLayer.modifierWaterLBMesh.title); }

                if (lbLayer.modifierWaterTransform != null)
                {
                    //Debug.Log("found water transform:" + lbLayer.modifierWaterTransform.name);

                    if (LBMeshOperations.CreateMeshForWaterFromLayer(this, lbLayer, layerIdx, "Layer Water " + lbLayer.modifierLBWater.GUID, showErrors))
                    {
                        if (lbLayer.modifierWaterLBMesh.mesh == null) { lbLayer.modifierWaterLBMesh.mesh = new Mesh(); }

                        if (!lbLayer.modifierWaterLBMesh.IsMeshDataValid()) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - LBMesh for Layer " + (layerIdx + 1) + " water does not look valid. Please Report."); } }
                        else if (lbLayer.modifierLBWater == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - LBWater is null for Layer " + (layerIdx + 1) + ". Please Report."); } }
                        else if (lbLayer.modifierWaterLBMesh.mesh == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not create new (water) Mesh for Layer " + (layerIdx + 1)); } }
                        else
                        {
                            //lbLayer.modifierWaterLBMesh.title = "Layer Water " + lbLayer.modifierLBWater.GUID;
                            lbLayer.modifierWaterLBMesh.mesh.name = lbLayer.modifierWaterLBMesh.title;

                            // Assign verts, triangle to new Unity Mesh and recalc bounds and tangents
                            if(lbLayer.modifierWaterLBMesh.UpdateMesh(false, showErrors))
                            {
                                // update the mesh for the water transform in the scene
                                MeshFilter meshFilter = lbLayer.modifierWaterTransform.GetComponent<MeshFilter>();
                                if (meshFilter != null)
                                {
                                    meshFilter.sharedMesh = lbLayer.modifierWaterLBMesh.mesh;
                                }

                                UpdateLayerWaterTransform(lbLayer, terrainHeight);
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region ApplyObjPaths

        /// <summary>
        /// Apply an object path to the landscape
        /// </summary>
        /// <param name="lbObjPathToApply"></param>
        /// <param name="doBackup"></param>
        /// <param name="showProgress"></param>
        /// <param name="showErrors"></param>
        public void ApplyObjPath(LBGroup lbGroupOwner, LBGroupMember lbGroupMemberOwner, bool doBackup, bool showProgress, bool showErrors)
        {
            string methodName = "LBLandscape.ApplyObjPath";

            // Exit early basic validation fails
            if (lbGroupOwner == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - LBGroup is null"); } return; }
            else if (lbGroupMemberOwner == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - lbGroupMemberOwner is null"); } return; }
            else if (lbGroupMemberOwner.lbMemberType != LBGroupMember.LBMemberType.ObjPath) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - groupmember is not an Object Path"); } return; }
            else if (lbGroupMemberOwner.lbObjPath == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - lbGroupMemberOwner.lbObjPath is null"); } return; }

            // Only give option to show progress bar if in the editor.
            // Only provide show progress call-back delegate option if in the editor
            #if UNITY_EDITOR
            ShowProgressDelegate showProgressDelegate = null;
            if (showProgress)
            {
                UnityEditor.EditorUtility.ClearProgressBar();
                showProgressDelegate = new ShowProgressDelegate(LBLandscape.ShowProgressBar);
            }
            // Keep compiler happy
            else { if (showProgressDelegate != null) { } }
            #endif

            #if UNITY_EDITOR
            if (doBackup && showProgress)
            { 
                UnityEditor.EditorUtility.DisplayProgressBar("Backing up tex, trees and grass for object placement", "Please Wait", 0.6f);
            }
            #endif

            bool isSaveTextureDataRequired = doBackup;
            bool isSaveTreeDataRequired = doBackup;

            if (doBackup)
            {            
                SaveData(LBLandscape.UndoType.Trees);
                SaveData(LBLandscape.UndoType.Grass);

                #if UNITY_EDITOR
                if (showProgress && useGPUPath && lbGroupMemberOwner.lbObjPath.useWidth)
                {
                    // Remove the previously applied Object Path topography changes
                    UnityEditor.EditorUtility.DisplayProgressBar("Getting heightmap restore point", "Please Wait", 0.7f);
                    RevertHeightmap1D("ObjPathDesigner", false, showErrors);

                    // Remove the previously applied Object Path texture changes
                    // In LB 2.2.0+ we also need to check subGroups for potential splatmap changes
                    if (LBGroup.IsApplyObjPathTexturesPresent(lbGroupList, lbGroupOwner, lbGroupMemberOwner.lbObjPath))
                    //if (!string.IsNullOrEmpty(lbGroupMemberOwner.lbObjPath.coreTextureGUID) || !string.IsNullOrEmpty(lbGroupMemberOwner.lbObjPath.surroundTextureGUID))
                    {
                        UnityEditor.EditorUtility.DisplayProgressBar("Getting splatmaps restore point", "Please Wait", 0.75f);
                        RevertTextures1D("ObjPathDesigner", true);
                        isSaveTextureDataRequired = false;
                    }

                    if (lbGroupMemberOwner.lbObjPath.isRemoveExistingTrees)
                    {
                        UnityEditor.EditorUtility.DisplayProgressBar("Getting Unity terrain trees restore point", "Please Wait", 0.8f);
                        RevertTrees1D("ObjPathDesigner", true);
                        isSaveTreeDataRequired = false;
                    }
                }
                #endif

                if (isSaveTextureDataRequired) { SaveData(LBLandscape.UndoType.Textures); }
                if (isSaveTreeDataRequired) { SaveData(LBLandscape.UndoType.Trees); }
            }

            // Delete all prefab parent gameobjects from the landscape that were created for the ObjPathDesigner.
            // These gameobjects contain all previously placed prefabs
            LBLandscapeTerrain.RemoveExistingPrefabs(this, true, LBPrefabItem.PrefabItemType.ObjPathDesignerPrefab, null);

            // Remove any (old) controllers that may be in the scene after some sort of failure
            LBTerrainMeshController.RemoveTerrainMeshControllers(this.transform);

            LBGroupParameters lbGroupParm = new LBGroupParameters();
            lbGroupParm.landscape = this;
            #if UNITY_EDITOR
            lbGroupParm.showErrors = showErrors;
            lbGroupParm.showProgress = showProgress;
            lbGroupParm.showProgressDelegate = showProgressDelegate;
            lbGroupParm.isGroupDesignerEnabled = lbGroupOwner.showGroupDesigner;
            lbGroupParm.designerOffsetY = 0f;

            // Configure the Object Path parameters which will restrict population to only
            // members of this single object path.
            lbGroupParm.lbObjPathParm = new LBObjPathParameters();
            lbGroupParm.lbObjPathParm.lbGroupOwner = lbGroupOwner;
            lbGroupParm.lbObjPathParm.lbGroupMemberOwner = lbGroupMemberOwner;
            lbGroupParm.lbObjPathParm.prefabItemType = LBPrefabItem.PrefabItemType.ObjPathDesignerPrefab;
            #endif

            if (!LBLandscapeTerrain.PopulateLandscapeWithGroups(lbGroupParm))
            {
                if (doBackup)
                {
                    // Failed so roll-back all changes
                    //RevertToLastSave(LBLandscape.UndoType.HeightMap);
                    if (isSaveTextureDataRequired) { RevertToLastSave(LBLandscape.UndoType.Textures); }
                    RevertToLastSave(LBLandscape.UndoType.Trees);
                    RevertToLastSave(LBLandscape.UndoType.Grass);
                }
            }

            #if UNITY_EDITOR
            if (showProgress)
            {
                UnityEditor.EditorUtility.ClearProgressBar();
                UnityEditor.EditorUtility.DisplayProgressBar("Populating Landscape With Object Path: " + lbGroupMemberOwner.lbObjPath.pathName, "Finalising - Please Wait", 0.7f);
            }
            #endif

            // We need to update the terrain LOD and vegetation information
            LBLandscapeTerrain.ApplyDelayedHeightmapLOD(landscapeTerrains);

            // If Stencil Layer Filters where used, free up any extra memory allocated
            LBStencil.FreeStencilResources(this, true);

            // TODO We "may" wish to combine meshes... for now remove the component
            LBTerrainMeshController terrainMeshController = this.GetComponentInChildren<LBTerrainMeshController>();

            if (terrainMeshController != null)
            {
                DestroyImmediate(terrainMeshController.gameObject);
                terrainMeshController = null;
            }

            #if VEGETATION_STUDIO_PRO
            if (useVegetationSystem)
            {
                LBIntegration.VegetationStudioProRefresh(showErrors);
            }
            #endif

            #if UNITY_EDITOR
            if (showProgress) { UnityEditor.EditorUtility.ClearProgressBar(); }
            #endif
        }

        #endregion

        #region Landscape Extension

        /// <summary>
        /// Enable or disable the flat mesh that extends the landscape towards the horizon
        /// </summary>
        /// <param name="isEnabled"></param>
        /// <param name="showErrors"></param>
        public void EnableLandscapeExtension(bool isEnabled, bool showErrors)
        {
            string methodName = "LBLandscape.EnableLandscapeExtension";
            Transform landscapeTrfm = this.transform;

            if (landscapeTrfm == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not find landscape transform"); } }
            else
            {
                Transform extensionTfrm = landscapeTrfm.Find("LandscapeExtension");

                // Remove existing (if any)
                if (extensionTfrm != null) { DestroyImmediate(extensionTfrm.gameObject); }

                if (isEnabled)
                {
                    if (lbLandscapeExtension == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscapeExtension object cannot be null"); } }
                    else
                    {
                        GameObject gameObjectExtPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
                        if (gameObjectExtPlane != null)
                        {
                            extensionTfrm = gameObjectExtPlane.transform;
                            extensionTfrm.SetParent(landscapeTrfm);
                            gameObjectExtPlane.name = "LandscapeExtension";
                            MeshRenderer mRen = extensionTfrm.GetComponent(typeof(MeshRenderer)) as MeshRenderer;
                            if (mRen != null)
                            {
                                // Disable casting and receiving shadows
                                mRen.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                                mRen.receiveShadows = false;

                                // Create a material using the Standard Shader
                                Material extensionMaterial = new Material(Shader.Find("Standard"));
                                if (extensionMaterial == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not create a material from the Standard Shader"); } }
                                else
                                {    
                                    extensionMaterial.SetTexture("_MainTex", lbLandscapeExtension.texture);
                                    extensionMaterial.SetTexture("_BumpMap", lbLandscapeExtension.normalMap);

                                    mRen.sharedMaterial = extensionMaterial;

                                    // Tell the Standard Shader which variant to use
                                    if (lbLandscapeExtension.normalMap == null) { mRen.sharedMaterial.DisableKeyword("_NORMALMAP"); }
                                    else { mRen.sharedMaterial.EnableKeyword("_NORMALMAP"); }
                                }
                            }

                            // Calculate the size (a plane is 10x10 units). Extend by 10km in each direction
                            Vector3 extScale = new Vector3((20000f + size.x) / 10f, 1f, (20000f + size.y) / 10f);

                            // Centre the plane under the landscape
                            extensionTfrm.localPosition = new Vector3(size.x / 2f, 0f, size.y / 2f);

                            // Resize the extension
                            extensionTfrm.localScale = extScale;

                            UpdateLandscapeExtensionMaterial(showErrors);
                            UpdateLandscapeExtensionHeight(true, showErrors);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Update the material of the Landscape Extension mesh
        /// </summary>
        /// <param name="showErrors"></param>
        public void UpdateLandscapeExtensionMaterial(bool showErrors)
        {
            string methodName = "LBLandscape.UpdateLandscapeExtensionMaterial";
            Transform landscapeTrfm = this.transform;

            if (landscapeTrfm == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not find landscape transform"); } }
            else if (lbLandscapeExtension == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscapeExtension object cannot be null"); } }
            else
            {
                Transform extensionTfrm = landscapeTrfm.Find("LandscapeExtension");
                if (extensionTfrm != null)
                {
                    MeshRenderer mRen = extensionTfrm.GetComponent(typeof(MeshRenderer)) as MeshRenderer;
                    if (mRen != null)
                    {
                        mRen.sharedMaterial.mainTextureScale = lbLandscapeExtension.tileSize;
                        mRen.sharedMaterial.SetFloat("_Metallic", lbLandscapeExtension.metallic);
                        mRen.sharedMaterial.SetFloat("_Glossiness", lbLandscapeExtension.smoothness);
                    }
                }
            }
        }

        /// <summary>
        /// Update the main and normalmap textures of the Landscape Extension mesh
        /// </summary>
        /// <param name="showErrors"></param>
        public void UpdateLandscapeExtensionTextures(bool showErrors)
        {
            string methodName = "LBLandscape.UpdateLandscapeExtensionTextures";
            Transform landscapeTrfm = this.transform;

            if (landscapeTrfm == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not find landscape transform"); } }
            else if (lbLandscapeExtension == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscapeExtension object cannot be null"); } }
            else
            {
                Transform extensionTfrm = landscapeTrfm.Find("LandscapeExtension");
                if (extensionTfrm != null)
                {
                    MeshRenderer mRen = extensionTfrm.GetComponent(typeof(MeshRenderer)) as MeshRenderer;
                    if (mRen != null)
                    {
                        mRen.sharedMaterial.SetTexture("_MainTex", lbLandscapeExtension.texture);
                        mRen.sharedMaterial.SetTexture("_BumpMap", lbLandscapeExtension.normalMap);

                        // Tell the Standard Shader which variant to use
                        if (lbLandscapeExtension.normalMap == null) { mRen.sharedMaterial.DisableKeyword("_NORMALMAP"); }
                        else { mRen.sharedMaterial.EnableKeyword("_NORMALMAP"); }
                    }
                }
            }
        }

        /// <summary>
        /// Update the height of the extension mesh. Optionally use the minimum height of the terrain data
        /// </summary>
        /// <param name="useRecommended"></param>
        /// <param name="showErrors"></param>
        public void UpdateLandscapeExtensionHeight(bool useRecommended, bool showErrors)
        {
            string methodName = "LBLandscape.UpdateLandscapeExtensionHeight";
            Transform landscapeTrfm = this.transform;

            if (landscapeTrfm == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - could not find landscape transform"); } }
            else if (lbLandscapeExtension == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscapeExtension object cannot be null"); } }
            else
            {
                // This could get a little slow if there are lots of child objects in the landscape
                Transform extensionTfrm = landscapeTrfm.Find("LandscapeExtension");
                if (extensionTfrm != null)
                {
                    if (useRecommended)
                    {
                        // This can be a little slow...
                        Vector2 landscapeMinMaxHeight = GetLandscapeMinMaxHeights();

                        // Set the extension to the lowest height of the landscape
                        if (!float.IsInfinity(landscapeMinMaxHeight.x)) { lbLandscapeExtension.heightOffset = (float)System.Math.Round(landscapeMinMaxHeight.x,2); }
                    }

                    extensionTfrm.localPosition = new Vector3(extensionTfrm.localPosition.x, lbLandscapeExtension.heightOffset, extensionTfrm.localPosition.z);
                }
            }
        }

        #endregion

        #endregion

        #region LB Editor Settings

        /// <summary>
        /// Saves the editor settings
        /// </summary>
        /// <param name="terrainTextures">Terrain textures.</param>
        /// <param name="terrainTrees">Terrain trees.</param>
        /// <param name="terrainGrass">Terrain grass.</param>
        /// <param name="landscapeMeshes">Landscape meshes.</param>
        public void SaveEditorSettings (List<LBLayer> topographyLayers, List<LBTerrainTexture> terrainTextures, List<LBTerrainTree> terrainTrees, List<LBTerrainGrass> terrainGrass, List<LBLandscapeMesh> landscapeMeshes, List<LBGroup> groupList)
	    {
		    // Save editor settings in scene
            topographyLayersList = topographyLayers;
		    terrainTexturesList = terrainTextures;
		    terrainTreesList = terrainTrees;
		    terrainGrassList = terrainGrass;
		    landscapeMeshList = landscapeMeshes;
            lbGroupList = groupList;
	    }

        #endregion

        #region GPU methods

        /// <summary>
        /// Verify that GPU acceleration is supported
        /// </summary>
        /// <returns></returns>
        public bool IsGPUAccelerationAvailable()
        {
            return (SystemInfo.supportsComputeShaders && SystemInfo.supports2DArrayTextures);
        }

        #endregion

        #region Terrain Attribute Methods

        /// <summary>
        /// Gets the height of the terrains in this landscape (y-axis)
        /// </summary>
        /// <returns>The landscape terrain height.</returns>
        public float GetLandscapeTerrainHeight ()
	    {
            if (landscapeTerrains != null)
            {
                if (landscapeTerrains.Length > 0)
                {
                    if (landscapeTerrains[0] != null)
                    {
                        if (landscapeTerrains[0].terrainData != null)
                        {
                            return landscapeTerrains[0].terrainData.size.y;
                        }
                    }
                }
            }
            else { Debug.LogError("ERROR: LBLandscape GetLandscapeTerrainHeight - landscapeTerrains not defined"); }
            return 2000f;
	    }

        /// <summary>
        /// Adjust only the height (y-axis) of all the terrains in this landscape
        /// </summary>
        /// <param name="newHeight"></param>
        public void SetLandscapeTerrainHeight(float newHeight)
        {
            if (landscapeTerrains != null)
            {
                for (int i = 0; i < landscapeTerrains.Length; i++)
                {
                    if (landscapeTerrains[i] != null)
                    {
                        if (landscapeTerrains[i].terrainData != null)
                        {
                            landscapeTerrains[i].terrainData.size = new Vector3(landscapeTerrains[i].terrainData.size.x, newHeight, landscapeTerrains[i].terrainData.size.z);
                        }
                    }
                }
            }
        }

	    /// <summary>
	    /// Gets the width of the terrains in this landscape (x-axis)
	    /// </summary>
	    /// <returns>The landscape terrain width.</returns>
	    public float GetLandscapeTerrainWidth ()
	    {
            if (landscapeTerrains != null)
            {
                if (landscapeTerrains.Length > 0)
                {
                    if (landscapeTerrains[0] != null)
                    {
                        if (landscapeTerrains[0].terrainData != null)
                        {
                            return landscapeTerrains[0].terrainData.size.x;
                        }
                    }
                }
            }
            else { Debug.LogError("ERROR: LBLandscape GetLandscapeTerrainWidth - landscapeTerrains not defined"); }
            return 2000f;
	    }

        /// <summary>
        /// Gets the length of the terrains in this landscape (z-axis)
        /// </summary>
        /// <returns></returns>
        public float GetLandscapeTerrainLength()
        {
            if (landscapeTerrains != null)
            {
                if (landscapeTerrains.Length > 0)
                {
                    if (landscapeTerrains[0] != null)
                    {
                        if (landscapeTerrains[0].terrainData != null)
                        {
                            return landscapeTerrains[0].terrainData.size.z;
                        }
                    }
                }
            }
            else { Debug.LogError("ERROR: LBLandscape GetLandscapeTerrainLength - landscapeTerrains not defined"); }
            return 2000f;
        }

        /// <summary>
        /// Gets the 3D size of the terrains in this landscape (width, height, length) x,y,z
        /// </summary>
        /// <returns></returns>
        public Vector3 GetLandscapeTerrainSize()
        {
            if (landscapeTerrains != null)
            {
                if (landscapeTerrains.Length > 0)
                {
                    if (landscapeTerrains[0] != null)
                    {
                        if (landscapeTerrains[0].terrainData != null)
                        {
                            return landscapeTerrains[0].terrainData.size;
                        }
                    }
                }
            }
            else { Debug.LogError("ERROR: LBLandscape GetLandscapeTerrainSize - landscapeTerrains not defined"); }
            return Vector3.one * 2000f;
        }

        /// <summary>
        /// Gets the heightmap scale of the terrains in this landscape (the 3D size of each heightmap cell)
        /// </summary>
        /// <returns>The landscape heightmap scale.</returns>
        public Vector3 GetLandscapeTerrainHeightmapScale ()
	    {
            if (landscapeTerrains != null)
            {
                if (landscapeTerrains.Length > 0)
                {
                    if (landscapeTerrains[0] != null)
                    {
                        if (landscapeTerrains[0].terrainData != null)
                        {
                            return landscapeTerrains[0].terrainData.heightmapScale;
                        }
                    }
                }
            }
            else { Debug.LogError("ERROR: LBLandscape GetLandscapeTerrainHeightmapScale - landscapeTerrains not defined"); }
            return Vector3.one;
	    }

        /// <summary>
        /// Gets the pixel error of the terrains in this landscape
        /// </summary>
        /// <returns>The landscape terrain pixel error.</returns>
        public float GetLandscapeTerrainPixelError ()
	    {
            if (landscapeTerrains != null)
            {
                if (landscapeTerrains.Length > 0)
                {
                    if (landscapeTerrains[0] != null)
                    {
                        return landscapeTerrains[0].heightmapPixelError;
                    }
                }
            }
            else { Debug.LogError("ERROR: LBLandscape GetLandscapeTerrainPixelError - landscapeTerrains not defined"); }
            return 5f;
	    }

	    /// <summary>
	    /// Gets the base map distance of the terrains in this landscape
	    /// </summary>
	    /// <returns>The landscape terrain base map dist.</returns>
	    public float GetLandscapeTerrainBaseMapDist ()
	    {
            if (landscapeTerrains != null)
            {
                if (landscapeTerrains.Length > 0)
                {
                    if (landscapeTerrains[0] != null)
                    {
                        return landscapeTerrains[0].basemapDistance;
                    }
                }
            }
            else { Debug.LogWarning("ERROR: LBLandscape GetLandscapeTerrainBaseMapDist - landscapeTerrains not defined"); }
            return 1000f;
	    }

        /// <summary>
        /// Set the base map distance on all the terrains in the landscape.
        /// Used in LBStencil.ShowStencil() with HDRP.
        /// </summary>
        /// <param name="newBaseMapDistance"></param>
        public void SetLandscapeTerrainBaseMapDist(float newBaseMapDistance)
        {
            if (landscapeTerrains != null)
            {
                for (int i = 0; i < landscapeTerrains.Length; i++)
                {
                    if (landscapeTerrains[i] != null)
                    {
                        landscapeTerrains[i].basemapDistance = newBaseMapDistance;
                    }
                }
            }
            else { Debug.LogWarning("ERROR: LBLandscape SetLandscapeTerrainBaseMapDist - landscapeTerrains not defined"); }
        }

        /// <summary>
        /// Gets the base texture resolution of the terrains in this landscape
        /// </summary>
        /// <returns>The landscape terrain base texture resolution.</returns>
        public int GetLandscapeTerrainBaseTextureResolution ()
	    {
            if (landscapeTerrains != null)
            {
                if (landscapeTerrains.Length > 0)
                {
                    if (landscapeTerrains[0] != null)
                    {
                        if (landscapeTerrains[0].terrainData != null)
                        {
                            return landscapeTerrains[0].terrainData.baseMapResolution;
                        }
                    }
                }
            }
            else { Debug.LogError("ERROR: LBLandscape GetLandscapeTerrainBaseTextureResolution - landscapeTerrains not defined"); }
            return 512;
	    }

        /// <summary>
        /// Gets the alphamap resolution of the terrains in this landscape
        /// </summary>
        /// <returns></returns>
        public int GetLandscapeTerrainAlphaMapResolution()
        {
            if (landscapeTerrains != null)
            {
                if (landscapeTerrains.Length > 0)
                {
                    if (landscapeTerrains[0] != null)
                    {
                        if (landscapeTerrains[0].terrainData != null)
                        {
                            return landscapeTerrains[0].terrainData.alphamapResolution;
                        }
                    }
                }
            }
            else { Debug.LogWarning("ERROR: LBLandscape GetLandscapeTerrainAlphaMapResolution - landscapeTerrains not defined"); }
            return 512;
        }

        /// <summary>
        /// Gets the detail resolution of the terrains in this landscape
        /// </summary>
        /// <returns></returns>
        public int GetLandscapeTerrainDetailResolution()
        {
            if (landscapeTerrains != null)
            {
                if (landscapeTerrains.Length > 0)
                {
                    if (landscapeTerrains[0] != null)
                    {
                        if (landscapeTerrains[0].terrainData != null)
                        {
                            return landscapeTerrains[0].terrainData.detailResolution;
                        }
                    }
                }
            }
            else { Debug.LogError("ERROR: LBLandscape GetLandscapeTerrainDetailResolution - landscapeTerrains not defined"); }
            return 512;
        }

        /// <summary>
        /// Gets the tree distance of the terrains in this landscape
        /// </summary>
        /// <returns>The landscape terrain tree distance.</returns>
        public float GetLandscapeTerrainTreeDistance ()
	    {
            if (landscapeTerrains != null)
            {
                if (landscapeTerrains.Length > 0)
                {
                    if (landscapeTerrains[0] != null)
                    {
                        return landscapeTerrains[0].treeDistance;
                    }
                }
            }
            else { Debug.LogError("ERROR: LBLandscape GetLandscapeTerrainTreeDistance - landscapeTerrains not defined"); }
            return 10000f;
	    }

	    /// <summary>
	    /// Gets the tree billboard of the terrains in this landscape
	    /// </summary>
	    /// <returns>The landscape terrain tree billboard start.</returns>
	    public float GetLandscapeTerrainTreeBillboardStart ()
	    {
            if (landscapeTerrains != null)
            {
                if (landscapeTerrains.Length > 0)
                {
                    if (landscapeTerrains[0] != null)
                    {
                        return landscapeTerrains[0].treeBillboardDistance;
                    }
                }
            }
            else { Debug.LogError("ERROR: LBLandscape GetLandscapeTerrainTreeBillboardStart - landscapeTerrains not defined"); }
            return 200f;
	    }

	    /// <summary>
	    /// Gets the terrain detail distance of the terrains in this landscape
	    /// </summary>
	    /// <returns>The landscape terrain detail distance.</returns>
	    public float GetLandscapeTerrainDetailDistance ()
	    {
            if (landscapeTerrains != null)
            {
                if (landscapeTerrains.Length > 0)
                {
                    if (landscapeTerrains[0] != null)
                    {
                        return landscapeTerrains[0].detailObjectDistance;
                    }
                }
            }
            else { Debug.LogError("ERROR: LBLandscape GetLandscapeTerrainDetailDistance - landscapeTerrains not defined"); }
            return 200f;
	    }

        /// <summary>
        /// Gets the terrain detail density of the terrains in this landscape
        /// </summary>
        /// <returns></returns>
        public float GetLandscapeTerrainDetailDensity()
        {
            if (landscapeTerrains != null)
            {
                if (landscapeTerrains.Length > 0)
                {
                    if (landscapeTerrains[0] != null)
                    {
                        return landscapeTerrains[0].detailObjectDensity;
                    }
                }
            }
            else { Debug.LogError("ERROR: LBLandscape GetLandscapeTerrainDetailDensity - landscapeTerrains not defined"); }
            return 1f;
        }

        /// <summary>
        /// Gets the tree fade length of the terrains in this landscape
        /// </summary>
        /// <returns>The landscape terrain tree fade length.</returns>
        public float GetLandscapeTerrainTreeFadeLength ()
	    {
            if (landscapeTerrains != null)
            {
                if (landscapeTerrains.Length > 0)
                {
                    if (landscapeTerrains[0] != null)
                    {
                        return landscapeTerrains[0].treeCrossFadeLength;
                    }
                }
            }
            else { Debug.LogError("ERROR: LBLandscape GetLandscapeTerrainTreeFadeLength - landscapeTerrains not defined"); }
            return 5f;
	    }

        /// <summary>
        /// Gets if drawInstanced is enabled on the first terrain
        /// in the landscape. By default return true.
        /// Always returns true for versions prior to U2018.3 so that
        /// when a project is upgraded to 2018.3+ it will be on by default.
        /// </summary>
        /// <returns></returns>
        public bool GetLandscapeTerrainDrawInstanced()
        {
            #if UNITY_2018_3_OR_NEWER
            if (landscapeTerrains != null)
            {
                if (landscapeTerrains.Length > 0)
                {
                    if (landscapeTerrains[0] != null)
                    {
                        return landscapeTerrains[0].drawInstanced;
                    }
                }
            }
            else { Debug.LogWarning("ERROR: LBLandscape GetLandscapeTerrainDrawInstanced - landscapeTerrains not defined"); }
            #endif
            return true;
        }

        /// <summary>
        /// Gets if terrainGroupID on the first terrain
        /// in the landscape. By default return 100.
        /// Always returns 100 for versions prior to U2018.3 so that
        /// when a project is upgraded to 2018.3+ it will be set by default.
        /// </summary>
        /// <returns></returns>
        public int GetLandscapeTerrainGroupingID()
        {
            #if UNITY_2018_3_OR_NEWER
            if (landscapeTerrains != null)
            {
                if (landscapeTerrains.Length > 0)
                {
                    if (landscapeTerrains[0] != null)
                    {
                        return landscapeTerrains[0].groupingID;
                    }
                }
            }
            else { Debug.LogWarning("ERROR: LBLandscape GetLandscapeTerrainGroupingID - landscapeTerrains not defined"); }
            #endif
            return 100;
        }

        /// <summary>
        /// Gets if allowAutoConnect is enabled on the first terrain
        /// in the landscape. By default return true.
        /// Always returns true for versions prior to U2018.3 so that
        /// when a project is upgraded to 2018.3+ it will be on by default.
        /// </summary>
        /// <returns></returns>
        public bool GetLandscapeTerrainAutoConnect()
        {
            #if UNITY_2018_3_OR_NEWER
            if (landscapeTerrains != null)
            {
                if (landscapeTerrains.Length > 0)
                {
                    if (landscapeTerrains[0] != null)
                    {
                        return landscapeTerrains[0].allowAutoConnect;
                    }
                }
            }
            else { Debug.LogWarning("ERROR: LBLandscape GetLandscapeTerrainAutoConnect - landscapeTerrains not defined"); }
            #endif
            return true;
        }

	    /// <summary>
	    /// Gets the heightmap resolution of the terrains in this landscape
	    /// </summary>
	    /// <returns>The landscape terrain heightmap resolution.</returns>
	    public int GetLandscapeTerrainHeightmapResolution ()
	    {
            if (landscapeTerrains != null)
            {
                if (landscapeTerrains.Length > 0)
                {
                    if (landscapeTerrains[0] != null)
                    {
                        if (landscapeTerrains[0].terrainData != null)
                        {
                            return landscapeTerrains[0].terrainData.heightmapResolution;
                        }
                    }
                }
            }
            else { Debug.LogError("ERROR: LBLandscape GetLandscapeTerrainHeightmapResolution - landscapeTerrains not defined"); }
            return 513;
	    }

        /// <summary>
        /// Returns the Layer Index [0-31] of the terrains in this landscape
        /// </summary>
        /// <returns></returns>
        public int GetLandscapeTerrainLayerIndex()
        {
            if (landscapeTerrains != null)
            {
                if (landscapeTerrains.Length > 0)
                {
                    if (landscapeTerrains[0] != null)
                    {
                        return landscapeTerrains[0].gameObject.layer;
                    }
                }
            }
            else { Debug.LogError("ERROR: LBLandscape GetLandscapeTerrainLayerIndex - landscapeTerrains not defined"); }
            return 0;
        }

        /// <summary>
        /// Gets whether the foliage and trees are drawn in the terrains in this landscape
        /// </summary>
        /// <returns><c>true</c>, if landscape terrain draw trees and foliage was gotten, <c>false</c> otherwise.</returns>
        public bool GetLandscapeTerrainDrawTreesAndFoliage ()
	    {
            if (landscapeTerrains != null)
            {
                if (landscapeTerrains.Length > 0)
                {
                    if (landscapeTerrains[0] != null)
                    {
                        return landscapeTerrains[0].drawTreesAndFoliage;
                    }
                }
            }
            else { Debug.LogError("ERROR: LBLandscape GetLandscapeTerrainDrawTreesAndFoliage - landscapeTerrains not defined"); }
            return true;
	    }

	    /// <summary>
	    /// Gets the grass wind speed of the terrains in this landscape
	    /// </summary>
	    /// <returns>The landscape terrain grass wind speed.</returns>
	    public float GetLandscapeTerrainGrassWindSpeed ()
	    {
            if (landscapeTerrains != null)
            {
                if (landscapeTerrains.Length > 0)
                {
                    if (landscapeTerrains[0] != null)
                    {
                        if (landscapeTerrains[0].terrainData != null)
                        {
                            return landscapeTerrains[0].terrainData.wavingGrassSpeed;
                        }
                    }
                }
            }
            else { Debug.LogError("ERROR: LBLandscape GetLandscapeTerrainGrassWindSpeed - landscapeTerrains not defined"); }
            return 0.5f;
	    }

	    /// <summary>
	    /// Gets the grass wind ripple size of the terrains in this landscape
	    /// </summary>
	    /// <returns>The landscape terrain grass wind ripple size.</returns>
	    public float GetLandscapeTerrainGrassWindRippleSize ()
	    {
            if (landscapeTerrains != null)
            {
                if (landscapeTerrains.Length > 0)
                {
                    if (landscapeTerrains[0] != null)
                    {
                        if (landscapeTerrains[0].terrainData != null)
                        {
                            return landscapeTerrains[0].terrainData.wavingGrassAmount;
                        }
                    }
                }
            }
            else { Debug.LogError("ERROR: LBLandscape GetLandscapeTerrainGrassWindRippleSize - landscapeTerrains not defined"); }
            return 0.5f;
	    }

	    /// <summary>
	    /// Gets the grass wind bending factor of the terrains in this landscape
	    /// </summary>
	    /// <returns>The landscape terrain grass wind bending.</returns>
	    public float GetLandscapeTerrainGrassWindBending ()
	    {
            if (landscapeTerrains != null)
            {
                if (landscapeTerrains.Length > 0)
                {
                    if (landscapeTerrains[0] != null)
                    {
                        if (landscapeTerrains[0].terrainData != null)
                        {
                            return landscapeTerrains[0].terrainData.wavingGrassStrength;
                        }
                    }
                }
            }
            else { Debug.LogError("ERROR: LBLandscape GetLandscapeTerrainGrassWindBending - landscapeTerrains not defined"); }
            return 0.5f;
	    }

        /// <summary>
        /// Gets the grass wind Tint colour of the terrains in this landscape
        /// NOTE: The Unity default is #B2997F00 (an off burnt brown colour)
        /// </summary>
        /// <returns></returns>
        public Color GetLandscapeTerrainGrassWindTint()
        {
            if (landscapeTerrains != null)
            {
                if (landscapeTerrains.Length > 0)
                {
                    if (landscapeTerrains[0] != null)
                    {
                        if (landscapeTerrains[0].terrainData != null)
                        {
                            return landscapeTerrains[0].terrainData.wavingGrassTint;
                        }
                    }
                }
            }
            else { Debug.LogError("ERROR: LBLandscape GetLandscapeTerrainGrassWindTint - landscapeTerrains not defined"); }
            return Color.white;
        }

        /// <summary>
        /// Gets the material type of the terrains in this landscape
        /// </summary>
        /// <returns>The landscape terrain material type.</returns>
        #if UNITY_2019_2_OR_NEWER
        [System.Obsolete("This method is obsolete")]
        #endif
        public Terrain.MaterialType GetLandscapeTerrainMaterialType ()
	    {
            if (landscapeTerrains != null)
            {
                if (landscapeTerrains.Length > 0)
                {
                    if (landscapeTerrains[0] != null)
                    {
                        return landscapeTerrains[0].materialType;
                    }
                }
            }
            else { Debug.LogError("ERROR: LBLandscape GetLandscapeTerrainMaterialType - landscapeTerrains not defined"); }
            return Terrain.MaterialType.BuiltInStandard;
	    }

        /// <summary>
        /// Gets the material type of the terrains in this landscape.
        /// NOTE: This only returns default values in Unity 2019.2+.
        /// </summary>
        /// <returns>The landscape terrain legacy specular colour.</returns>
        public Color GetLandscapeTerrainLegacySpecular ()
	    {
            #if !UNITY_2019_2_OR_NEWER
            if (landscapeTerrains != null)
            {
                if (landscapeTerrains.Length > 0)
                {
                    if (landscapeTerrains[0] != null)
                    {
                        return landscapeTerrains[0].legacySpecular;
                    }
                }
            }
            else { Debug.LogError("ERROR: LBLandscape GetLandscapeTerrainLegacySpecular - landscapeTerrains not defined"); }
            #endif
            return new Color(0.5f, 0.5f, 0.5f, 1f);
	    }
    
        /// <summary>
	    /// Gets the shininess value of the terrains in this landscape.
        /// NOTE: This only returns default values in Unity 2019.2+.
	    /// </summary>
	    /// <returns>The landscape terrain legacy shininess value.</returns>
	    // #if UNITY_2019_2_OR_NEWER
        // System.Obsolete("This method is obsolete")]
        // #endif
        public float GetLandscapeTerrainLegacyShininess ()
	    {
            #if !UNITY_2019_2_OR_NEWER
            if (landscapeTerrains != null)
            {
                if (landscapeTerrains.Length > 0)
                {
                    if (landscapeTerrains[0] != null)
                    {
                        return landscapeTerrains[0].legacyShininess;
                    }
                }
            }
            else { Debug.LogError("ERROR: LBLandscape GetLandscapeTerrainLegacyShininess - landscapeTerrains not defined"); }
            #endif
            return 0.1f;
	    }

        /// <summary>
        /// Gets the custom material of the terrains in this landscape
        /// </summary>
        /// <returns>The landscape terrain custom material.</returns>
        public Material GetLandscapeTerrainCustomMaterial ()
	    {
            if (landscapeTerrains != null)
            {
                if (landscapeTerrains.Length > 0)
                {
                    if (landscapeTerrains[0] != null)
                    {
                        return landscapeTerrains[0].materialTemplate;
                    }
                }
            }
            else { Debug.LogError("ERROR: LBLandscape GetLandscapeTerrainCustomMaterial - landscapeTerrains not defined"); }
            return null;
	    }
    
        /// <summary>
	    /// Gets the LB Standard terrain material of this landscape
	    /// </summary>
	    /// <returns>The landscape LB Standard terrain material.</returns>
	    public Material GetLandscapeLBStandardTerrainMaterial ()
	    {
            return LBStandardTerrainMaterial;
	    }
    
        /// <summary>
	    /// Sets the LB Standard terrain material of this landscape
	    /// </summary>
	    public void SetLandscapeLBStandardTerrainMaterial (Material terrainMat)
	    {
            LBStandardTerrainMaterial = terrainMat;
        }

        /// <summary>
        /// This is a fast way of getting the worldspace boundaries of the landscape.
        /// A more accurate method would be to examine the terrain data using
        /// LBLandscapeTerrain.GetLandscapeWorldBounds(terrains);
        /// </summary>
        /// <returns></returns>
        public Rect GetLandscapeWorldBoundsFast()
        {
            Rect worldBounds = new Rect();
            Vector3 landscapePosition = gameObject.transform.position;

            worldBounds.xMin = landscapePosition.x;
            worldBounds.yMin = landscapePosition.z;

            worldBounds.width = size.x;
            worldBounds.height = size.y;

            return worldBounds;
        }

        #endregion

        #region Point Methods for the Landscape

        /// <summary>
        /// Check if a point is within the worldspace limits of the landscape
        /// For a 2D check, set the point.y to 0.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public bool IsPointInLandscapeBoundsFast(Vector3 point)
        {
            Rect wb = GetLandscapeWorldBoundsFast();

            if (point.y != 0f)
            {
                // Cache the landscapeHeight if it has not be accessed before in this sesssion
                if (landscapeHeight == 0f) { landscapeHeight = GetLandscapeTerrainHeight(); }
            }

            return (point.x >= wb.xMin && point.x <= wb.xMax && point.z >= wb.yMin && point.z <= wb.yMax && point.y >= start.y && point.y <= landscapeHeight);
        }

        /// <summary>
        /// Using a world space point, normalise (0.0-1.0) it to the landscape
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public Vector2 NormalisePointFromWorldPos2DFast(Vector2 point)
        {
            return new Vector2((point.x - this.start.x) / size.x, (point.y - this.start.z) / size.y);
        }

        #endregion

        #region Enable-Disable Terrain Methods

        /// <summary>
        /// Enable or disable all Unity terrains within the landscape
        /// </summary>
        /// <param name="isEnabled"></param>
        public void EnableTerrains(bool isEnabled)
        {
            Terrain[] landscapeTerrains = GetComponentsInChildren<Terrain>(true);
            if (landscapeTerrains != null)
            {
                for (int t = 0; t < landscapeTerrains.Length; t++) { landscapeTerrains[t].gameObject.SetActive(isEnabled); }
            }
        }

        /// <summary>
        /// Enable or disable all the Mesh Terrains in this landscape
        /// </summary>
        /// <param name="isEnabled"></param>
        public void EnableMeshTerrains(bool isEnabled)
        {
            // Find the parent gameobject with one child gameobject for each terrain (mesh).
            Transform meshParentTrfm = this.transform.Find(this.name + "_Meshes");

            // If the parent mesh GameObject exist, attempt to enable or disable it.
            if (meshParentTrfm != null)
            {
                meshParentTrfm.gameObject.SetActive(isEnabled);
            }
        }

        #endregion

        #region Terrain Neighbour Methods

        /// <summary>
        /// Sets the terrain neighbours in this landscape
        /// If you wish to update the sceneview in the editor, set flushTerrain to true
        /// </summary>
        /// <param name="flushTerrain"></param>
        public void SetTerrainNeighbours(bool flushTerrain = false)
	    {
            if (landscapeTerrains != null)
            {
                for (int i = 0; i < landscapeTerrains.Length; i++)
                {
                    float terrainWidth = GetLandscapeTerrainWidth();
                    Terrain topTerrain = null;
                    Vector3 topTerrainPos = landscapeTerrains[i].transform.position + (Vector3.forward * terrainWidth);
                    for (int i2 = 0; i2 < landscapeTerrains.Length; i2++)
                    {
                        if (Vector3.Distance(landscapeTerrains[i2].transform.position, topTerrainPos) < terrainWidth * 0.01f)
                        {
                            topTerrain = landscapeTerrains[i2];
                            break;
                        }
                    }
                    Terrain bottomTerrain = null;
                    Vector3 bottomTerrainPos = landscapeTerrains[i].transform.position - (Vector3.forward * terrainWidth);
                    for (int i2 = 0; i2 < landscapeTerrains.Length; i2++)
                    {
                        if (Vector3.Distance(landscapeTerrains[i2].transform.position, bottomTerrainPos) < terrainWidth * 0.01f)
                        {
                            bottomTerrain = landscapeTerrains[i2];
                            break;
                        }
                    }
                    Terrain leftTerrain = null;
                    Vector3 leftTerrainPos = landscapeTerrains[i].transform.position - (Vector3.right * terrainWidth);
                    for (int i2 = 0; i2 < landscapeTerrains.Length; i2++)
                    {
                        if (Vector3.Distance(landscapeTerrains[i2].transform.position, leftTerrainPos) < terrainWidth * 0.01f)
                        {
                            leftTerrain = landscapeTerrains[i2];
                            break;
                        }
                    }
                    Terrain rightTerrain = null;
                    Vector3 rightTerrainPos = landscapeTerrains[i].transform.position + (Vector3.right * terrainWidth);
                    for (int i2 = 0; i2 < landscapeTerrains.Length; i2++)
                    {
                        if (Vector3.Distance(landscapeTerrains[i2].transform.position, rightTerrainPos) < terrainWidth * 0.01f)
                        {
                            rightTerrain = landscapeTerrains[i2];
                            break;
                        }
                    }

                    landscapeTerrains[i].SetNeighbors(leftTerrain, topTerrain, rightTerrain, bottomTerrain);
                    if (flushTerrain) { landscapeTerrains[i].Flush(); }
                }
            }
            else { Debug.LogError("ERROR: LBLandscape SetTerrainNeighbours - landscapeTerrains not defined"); }
        }

        /// <summary>
        /// Get the terrain that is located on the given edge of the terrain which has the zero-based
        /// position in the landscapeTerrains list.
        /// e.g. Get the left neighbour of the 5th terrain in the landscape's list of terrains
        /// Terrain leftTerrain = GetTerrainNeighbour(4, "LEFT");
        /// </summary>
        /// <param name="terrain"></param>
        /// <param name="terrainEdge"></param>
        /// <returns></returns>
        public Terrain GetTerrainNeighbour(Terrain terrain, string terrainEdge)
        {
            Terrain terrainNeighbour = null;

            if (landscapeTerrains == null)
            {
                Debug.LogError("ERROR: LBLandscape GetTerrainNeighbour - landscapeTerrains is not defined");
            }
            else if (landscapeTerrains.Length == 0)
            {
                Debug.LogError("ERROR: LBLandscape GetTerrainNeighbour - there are no terrains in this landscape");
            }
            else if (terrain == null)
            {
                Debug.LogError("ERROR: LBLandscape GetTerrainNeighbour - terrain is null");
            }
            else
            {
                float terrainWidth = GetLandscapeTerrainWidth();

                if (terrainEdge.ToUpper() == "TOP")
                {
                    Vector3 topTerrainPos = terrain.transform.position + (Vector3.forward * terrainWidth);
                    for (int i2 = 0; i2 < landscapeTerrains.Length; i2++)
                    {
                        if (Vector3.Distance(landscapeTerrains[i2].transform.position, topTerrainPos) < terrainWidth * 0.01f)
                        {
                            terrainNeighbour = landscapeTerrains[i2]; break;
                        }
                    }
                }
                else if (terrainEdge.ToUpper() == "BOTTOM")
                {
                    Vector3 bottomTerrainPos = terrain.transform.position - (Vector3.forward * terrainWidth);
                    for (int i2 = 0; i2 < landscapeTerrains.Length; i2++)
                    {
                        if (Vector3.Distance(landscapeTerrains[i2].transform.position, bottomTerrainPos) < terrainWidth * 0.01f)
                        {
                            terrainNeighbour = landscapeTerrains[i2]; break;
                        }
                    }
                }
                else if (terrainEdge.ToUpper() == "LEFT")
                {
                    Vector3 leftTerrainPos = terrain.transform.position - (Vector3.right * terrainWidth);
                    for (int i2 = 0; i2 < landscapeTerrains.Length; i2++)
                    {
                        if (Vector3.Distance(landscapeTerrains[i2].transform.position, leftTerrainPos) < terrainWidth * 0.01f)
                        {
                            terrainNeighbour = landscapeTerrains[i2]; break;
                        }
                    }
                }
                else if (terrainEdge.ToUpper() == "RIGHT")
                {
                    Vector3 rightTerrainPos = terrain.transform.position + (Vector3.right * terrainWidth);
                    for (int i2 = 0; i2 < landscapeTerrains.Length; i2++)
                    {
                        if (Vector3.Distance(landscapeTerrains[i2].transform.position, rightTerrainPos) < terrainWidth * 0.01f)
                        {
                            terrainNeighbour = landscapeTerrains[i2]; break;
                        }
                    }
                }
            }

            return terrainNeighbour;
        }

        #endregion

        #region GameObject and LayerMask Methods

        /// <summary>
        /// Get a list of all objects in a Landscape which are in a Unity Layer that
        /// matches the layerMask and have a given tag. If the tag parameter is set
        /// to LBFilter.FilterByAllTags, then the object tag is not checked.
        /// Positions are returned in world space.
        /// </summary>
        /// <param name="layerMask"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        public List<Vector3> GetGameObjectPositions(LayerMask layerMask, string tag)
        {
            List<Vector3> objectPositionList = new List<Vector3>();

            // Find all the gameobjects in the landscape
            Transform[] transforms = gameObject.GetComponentsInChildren<Transform>();

            // This should always be true as we'll get the terrains too
            if (transforms != null)
            {
                foreach(Transform trfm in transforms)
                {
                    // Ignore the parent landscape gameobject
                    if (trfm.gameObject != this.gameObject)
                    {
                        // Check any gameobject that looks like a terrain
                        if (trfm.gameObject.name.StartsWith("LandscapeTerrain"))
                        {
                            // If it has a Terrain script attached ignore it
                            if (trfm.gameObject.GetComponent<Terrain>() != null) { continue; }
                        }

                        if (LBLandscape.IsInLayerMask(layerMask, trfm.gameObject))
                        {
                            if (tag == LBFilter.FilterByAllTags)
                            {
                                objectPositionList.Add(trfm.position);
                            }
                            else if (trfm.gameObject.CompareTag(tag))
                            {
                                objectPositionList.Add(trfm.position);
                            }
                        }
                    }
                }
            }

            return objectPositionList;
        }

        /// <summary>
        /// Check if the gameobject is a layer that is in the LayerMask
        /// </summary>
        /// <param name="layerMask"></param>
        /// <param name="gameObj"></param>
        /// <returns></returns>
        public static bool IsInLayerMask(LayerMask layerMask, GameObject gameObj)
        {
            return ((layerMask.value & (1 << gameObj.layer)) > 0);
        }

        #endregion

        #region Terrain Material Methods

        /// <summary>
        /// Set the terrain material for a given terrain. This will typically also change the shader used for the terrain
        /// Currently only used by LBDemoLoader.cs
        /// Setting MegaSplat and MicroSplat terrain material not supported.
        /// NOTE: Needs to be updated when LandscapeBuilderWindow.SetTerrainMaterial() is also updated
        /// </summary>
        /// <param name="terrain"></param>
        /// <param name="terrainIndex"></param>
        /// <param name="isLastTerrain"></param>
        /// <param name="terrainWidth"></param>
        /// <param name="pixelError"></param>
        /// <param name="tMaterialType"></param>
        /// <param name="prevtMaterialType"></param>
        public void SetTerrainMaterial(Terrain terrain, int terrainIndex, bool isLastTerrain, float terrainWidth, ref float pixelError,
                                       TerrainMaterialType tMaterialType, TerrainMaterialType prevtMaterialType = TerrainMaterialType.BuiltInStandard)
        {
            bool isFirstTerrain = (terrainIndex == 0);
            Material terrainMat = null;

            #region Check if cleanup is required on previous materials
            // If we're not using using RTP but it is installed the user may
            // have switch from it at some point. Make sure it is not
            // selected for this terrain 
            if (prevtMaterialType == TerrainMaterialType.ReliefTerrainPack)
            {
                if (this.gameObject != null && LBIntegration.isRTPInstalled(false))
                {
                    if (LBIntegration.RTPEnable(this, terrain, false, isFirstTerrain, isLastTerrain, false)) { }
                    // Set the pixel error back to a sensible value
                    if (pixelError > 5f) { terrain.heightmapPixelError = 5f; pixelError = 5f; }
                }
            }
            else if (prevtMaterialType == LBLandscape.TerrainMaterialType.MegaSplat)
            {
                if (this.gameObject != null && LBIntegration.IsMegaSplatInstalled(false))
                {
                    if (LBIntegration.MegaSplatEnable(this, terrain, terrain.materialTemplate, false, true)) { }
                }
            }
            else if (prevtMaterialType == LBLandscape.TerrainMaterialType.MicroSplat)
            {
                if (this.gameObject != null && LBIntegration.IsMicroSplatInstalled())
                {
                    if (LBIntegration.MicroSplatEnable(this, terrain, terrain.materialTemplate, false, isFirstTerrain, isLastTerrain, true)) { }
                }
            }
            // Check for following scenario:
            // MicroSplat was selected in Terrain Settings, Applied, but not initialised. Now another Material Type has been selected and Applied.
            else if (tMaterialType != LBLandscape.TerrainMaterialType.MicroSplat && LBIntegration.IsMicroSplatInstalled())
            {
                LBIntegration.MicroSplatCleanup(terrain, true);
            }
            // Previous was URP, but now want something different
            else if (prevtMaterialType == LBLandscape.TerrainMaterialType.URP && tMaterialType != LBLandscape.TerrainMaterialType.URP)
            {
                terrainCustomMaterial = null;
                terrain.materialTemplate = null;
            }
            // Previous was LWRP, but now want something different
            else if (prevtMaterialType == LBLandscape.TerrainMaterialType.LWRP && tMaterialType != LBLandscape.TerrainMaterialType.LWRP)
            {
                terrainCustomMaterial = null;
                terrain.materialTemplate = null;
            }
            else if (prevtMaterialType == LBLandscape.TerrainMaterialType.HDRP && tMaterialType != LBLandscape.TerrainMaterialType.HDRP)
            {
                terrainCustomMaterial = null;
                terrain.materialTemplate = null;
            }
            #endregion

            if (tMaterialType == TerrainMaterialType.BuiltInStandard)
            {
                #if UNITY_2019_2_OR_NEWER              
                // Does the material already exist?
                terrainMat = GetLandscapeTerrainCustomMaterial();
                if (terrainMat == null || !terrainMat.shader.name.Contains("Nature/Terrain/Standard"))
                {
                    // This is designed for runtime usage
                    terrain.materialTemplate = CreateTerrainMaterial(terrainMat, "LBTerrainStandard", "Assets/LandscapeBuilder/Materials/");
                }
                else { terrain.materialTemplate = terrainMat; if (isFirstTerrain) { terrainCustomMaterial = terrainMat; } }
                #else
                terrain.materialType = Terrain.MaterialType.BuiltInStandard;
                #endif     
            }
            else if (tMaterialType == TerrainMaterialType.BuiltInLegacyDiffuse)
            {
                #if UNITY_2019_2_OR_NEWER              
                // Does the material already exist?
                terrainMat = GetLandscapeTerrainCustomMaterial();
                if (terrainMat == null || !terrainMat.shader.name.Contains("Nature/Terrain/Diffuse"))
                {
                    // This is designed for runtime usage
                    terrain.materialTemplate = CreateTerrainMaterial(terrainMat, "LBTerrainDiffuse", "Assets/LandscapeBuilder/Materials/");
                }
                else { terrain.materialTemplate = terrainMat; if (isFirstTerrain) { terrainCustomMaterial = terrainMat; } }
                #else
                terrain.materialType = Terrain.MaterialType.BuiltInLegacyDiffuse;
                #endif 
            }
            else if (tMaterialType == TerrainMaterialType.BuiltInLegacySpecular)
            {
                #if UNITY_2019_2_OR_NEWER              
                // Does the material already exist?
                terrainMat = GetLandscapeTerrainCustomMaterial();
                if (terrainMat == null || !terrainMat.shader.name.Contains("Nature/Terrain/Specular"))
                {
                    // This is designed for runtime usage
                    terrain.materialTemplate = CreateTerrainMaterial(terrainMat, "LBTerrainSpecular", "Assets/LandscapeBuilder/Materials/");
                }
                else { terrain.materialTemplate = terrainMat; if (isFirstTerrain) { terrainCustomMaterial = terrainMat; } }
                #else
                terrain.materialType = Terrain.MaterialType.BuiltInLegacySpecular;
                #endif 
            }

            #region URP
            else if (tMaterialType == LBLandscape.TerrainMaterialType.URP)
            {
                // Set material type to custom to allow us to use our own material
                #if !UNITY_2019_2_OR_NEWER
                terrain.materialType = Terrain.MaterialType.Custom;
                #endif

                // Does the material already exist?
                terrainMat = GetLandscapeTerrainCustomMaterial();

                // If it does not exist OR it is not a URP 7.1.2+ shader, create a new material
                if (terrainMat == null || !terrainMat.shader.name.Contains("Universal Render Pipeline/Terrain/Lit"))
                {
                    // Load the material
                    Material tempMat = (Material)Resources.Load("LBTerrain", typeof(Material));

                    // Make sure it has been updated from SRP folder package
                    if (tempMat == null)
                    {             
                        Debug.LogWarning("URP LBTerrain material could not be found at path: Assets/LandscapeBuilder/Materials/Resources/LBTerrain.mat. Please Report");
                    }
                    else if (!tempMat.shader.name.Contains("Universal Render Pipeline/Terrain/Lit"))
                    {
                        Debug.LogWarning("ERROR: Did you apply the LB_URP package from the LandscapeBuilder/SRP folder? If you did, please report this error.");
                    }
                    else
                    {
                        #if UNITY_2019_2_OR_NEWER
                        terrainMat = CreateTerrainMaterial(tempMat, "Assets/LandscapeBuilder/Materials/");
                        terrain.materialTemplate = terrainMat;
                        #else
                        // Create a copy
                        terrainMat = new Material(tempMat);
                        #if UNITY_EDITOR
                        UnityEditor.AssetDatabase.CreateAsset(terrainMat, LBSetup.materialsFolder + "/" + this.name + "TerrainMaterial.mat");
                        UnityEditor.AssetDatabase.Refresh();
                        #endif
                        this.terrainCustomMaterial = terrainMat;
                        #endif
                    }
                }

                if (terrainMat == null)
                {
                    Debug.LogWarning("LandscapeBuilder - Setting URP terrain material failed. Did you apply the LB_URP package from the LandscapeBuilder/SRP folder?");
                    // Fallback to standard builtin material
                    #if UNITY_2019_2_OR_NEWER              
                    // Does the material already exist?
                    terrainMat = GetLandscapeTerrainCustomMaterial();
                    if (terrainMat == null || !terrainMat.shader.name.Contains("Nature/Terrain/Standard"))
                    {
                        // This is designed for runtime usage
                        terrain.materialTemplate = CreateTerrainMaterial(terrainMat, "LBTerrainStandard", "Assets/LandscapeBuilder/Materials/");
                    }
                    else { terrain.materialTemplate = terrainMat; }
                    #else
                    terrain.materialType = Terrain.MaterialType.BuiltInStandard;
                    #endif
                }
                else
                {
                    #if UNITY_2018_3_OR_NEWER
                    // There is one material per landscape. If this is the first terrain, update the material            
                    if (terrainIndex == 0)
                    {
                        // If Per Pixel normals are available AND Draw Instanced is enabled, by default enable per-pixel normals.
                        // If we don't want per-pixel normals we need to update it in code later (or set it manually in LB Editor terrain settings).
                        if (terrainMat.HasProperty("_TERRAIN_INSTANCED_PERPIXEL_NORMAL"))
                        {
                            if (GetLandscapeTerrainDrawInstanced())
                            {
                                // Appear to have to update the shader_feature as a keyword, AND set the Float property.
                                terrainMat.EnableKeyword("_TERRAIN_INSTANCED_PERPIXEL_NORMAL");
                                terrainMat.SetFloat("_TERRAIN_INSTANCED_PERPIXEL_NORMAL", 1f);
                                #if UNITY_EDITOR
                                UnityEditor.EditorUtility.SetDirty(terrainMat);
                                #endif
                            }
                        }
                        else
                        {
                            Debug.LogWarning("URP material property _TERRAIN_INSTANCED_PERPIXEL_NORMAL is not available. Do you have URP 7.1.2 or newer in your project? Check Package Manager");
                        }
                    }
                    #endif

                    terrain.materialTemplate = terrainMat;
                }
            }
            #endregion

            #region LWRP
            else if (tMaterialType == LBLandscape.TerrainMaterialType.LWRP)
            {
                // Set material type to custom to allow us to use our own material
                #if !UNITY_2019_2_OR_NEWER
                terrain.materialType = Terrain.MaterialType.Custom;
                #endif

                // Does the material already exist?
                terrainMat = GetLandscapeTerrainCustomMaterial();

                // If it does not exist OR it is not a LWRP 4.0.1+ shader, create a new material
                if (terrainMat == null || !terrainMat.shader.name.Contains("Lightweight Render Pipeline/Terrain/Lit"))
                {
                    // Load the material
                    Material tempMat = (Material)Resources.Load("LBTerrain", typeof(Material));

                    // Make sure it has been updated from SRP folder package
                    if (tempMat == null)
                    {             
                        Debug.LogWarning("LWRP LBTerrain material could not be found at path: Assets/LandscapeBuilder/Materials/Resources/LBTerrain.mat. Please Report");
                    }
                    else if (!tempMat.shader.name.Contains("Lightweight Render Pipeline/Terrain/Lit"))
                    {
                        Debug.LogWarning("ERROR: Did you apply the LB_LWRP package from the LandscapeBuilder/SRP folder? If you did, please report this error.");
                    }
                    else
                    {
                        #if UNITY_2019_2_OR_NEWER
                        terrainMat = CreateTerrainMaterial(tempMat, "Assets/LandscapeBuilder/Materials/");
                        terrain.materialTemplate = terrainMat;
                        #else
                        // Create a copy
                        terrainMat = new Material(tempMat);
                        #if UNITY_EDITOR
                        UnityEditor.AssetDatabase.CreateAsset(terrainMat, LBSetup.materialsFolder + "/" + this.name + "TerrainMaterial.mat");
                        UnityEditor.AssetDatabase.Refresh();
                        #endif
                        this.terrainCustomMaterial = terrainMat;
                        #endif
                    }
                }

                if (terrainMat == null)
                {
                    Debug.LogWarning("LandscapeBuilder - Setting LWRP terrain material failed. Did you apply the LB_LWRP package from the LandscapeBuilder/SRP folder?");
                    // Fallback to standard builtin material
                    #if UNITY_2019_2_OR_NEWER              
                    // Does the material already exist?
                    terrainMat = GetLandscapeTerrainCustomMaterial();
                    if (terrainMat == null || !terrainMat.shader.name.Contains("Nature/Terrain/Standard"))
                    {
                        // This is designed for runtime usage
                        terrain.materialTemplate = CreateTerrainMaterial(terrainMat, "LBTerrainStandard", "Assets/LandscapeBuilder/Materials/");
                    }
                    else { terrain.materialTemplate = terrainMat; }
                    #else
                    terrain.materialType = Terrain.MaterialType.BuiltInStandard;
                    #endif
                }
                else
                {
                    #if UNITY_2018_3_OR_NEWER
                    // There is one material per landscape. If this is the first terrain, update the material            
                    if (terrainIndex == 0)
                    {
                        // If Per Pixel normals are available AND Draw Instanced is enabled, by default enable per-pixel normals.
                        // If we don't want per-pixel normals we need to update it in code later (or set it manually in LB Editor terrain settings).
                        if (terrainMat.HasProperty("_TERRAIN_INSTANCED_PERPIXEL_NORMAL"))
                        {
                            if (GetLandscapeTerrainDrawInstanced())
                            {
                                // Appear to have to update the shader_feature as a keyword, AND set the Float property.
                                terrainMat.EnableKeyword("_TERRAIN_INSTANCED_PERPIXEL_NORMAL");
                                terrainMat.SetFloat("_TERRAIN_INSTANCED_PERPIXEL_NORMAL", 1f);
                                #if UNITY_EDITOR
                                UnityEditor.EditorUtility.SetDirty(terrainMat);
                                #endif
                            }
                        }
                        else
                        {
                            Debug.LogWarning("LWRP material property _TERRAIN_INSTANCED_PERPIXEL_NORMAL is not available. Do you have LWRP 4.0.1 or newer in your project? Check Package Manager");
                        }
                    }
                    #endif

                    terrain.materialTemplate = terrainMat;
                }
            }
            #endregion

            #region HDRP
            else if (tMaterialType == LBLandscape.TerrainMaterialType.HDRP)
            {
                // Set material type to custom to allow us to use our own material
                #if !UNITY_2019_2_OR_NEWER
                terrain.materialType = Terrain.MaterialType.Custom;
                #endif

                // Does the material already exist?
                terrainMat = GetLandscapeTerrainCustomMaterial();

                // If it does not exist OR it is not a HDRP 4.9.0+ or 4.0.1+ shader, create a new material
                if (terrainMat == null || (!terrainMat.shader.name.Contains("HDRP/TerrainLit") && !terrainMat.shader.name.Contains("HDRenderPipeline/TerrainLit")))
                {
                    // Load the material
                    Material tempMat = (Material)Resources.Load("LBTerrain", typeof(Material));

                    // Make sure it has been updated from SRP folder package
                    if (tempMat == null)
                    {             
                        Debug.LogWarning("HDRP LBTerrain material could not be found at path: Assets/LandscapeBuilder/Materials/Resources/LBTerrain.mat. Please Report");
                    }
                    // Check for 4.9.0+ or 4.0.1 shaders
                    else if (!tempMat.shader.name.Contains("HDRP/TerrainLit") && !tempMat.shader.name.Contains("HDRenderPipeline/TerrainLit"))
                    {
                        Debug.LogWarning("ERROR: Did you apply the LB_HDRP package from the LandscapeBuilder/SRP folder? If you did, please report this error.");
                    }
                    else
                    {
                        #if UNITY_2019_2_OR_NEWER
                        terrainMat = CreateTerrainMaterial(tempMat, "Assets/LandscapeBuilder/Materials/");
                        terrain.materialTemplate = terrainMat;
                        #else
                        // Create a copy
                        terrainMat = new Material(tempMat);
                        #if UNITY_EDITOR
                        UnityEditor.AssetDatabase.CreateAsset(terrainMat, LBSetup.materialsFolder + "/" + this.name + "TerrainMaterial.mat");
                        UnityEditor.AssetDatabase.Refresh();
                        #endif
                        this.terrainCustomMaterial = terrainMat;
                        #endif
                    }
                }

                if (terrainMat == null)
                {
                    Debug.LogWarning("LandscapeBuilder - Setting HDRP terrain material failed. Did you apply the LB_HDRP package from the LandscapeBuilder/SRP folder?");
                    // Fallback to standard builtin material
                    #if UNITY_2019_2_OR_NEWER              
                    // Does the material already exist?
                    terrainMat = GetLandscapeTerrainCustomMaterial();
                    if (terrainMat == null || !terrainMat.shader.name.Contains("Nature/Terrain/Standard"))
                    {
                        // This is designed for runtime usage
                        terrain.materialTemplate = CreateTerrainMaterial(terrainMat, "LBTerrainStandard", "Assets/LandscapeBuilder/Materials/");
                    }
                    else { terrain.materialTemplate = terrainMat; }
                    #else
                    terrain.materialType = Terrain.MaterialType.BuiltInStandard;
                    #endif

                }
                else
                {
                    // There is one material per landscape. If this is the first terrain, update the material
                    // NOTE: CURRENTLY PERPIXEL NORMAL not supported in HDRP 4.0.1
                    //if (terrainIndex == 0)
                    //{
                    //    // If Per Pixel normals are available AND Draw Instanced is enabled, by default enable per-pixel normals.
                    //    // If we don't want per-pixel normals we need to update it in code later (or set it manually in LB Editor terrain settings).
                    //    if (terrainMat.HasProperty("_TERRAIN_INSTANCED_PERPIXEL_NORMAL"))
                    //    {
                    //        if (GetLandscapeTerrainDrawInstanced())
                    //        {
                    //            // Appear to have to update the shader_feature as a keyword, AND set the Float property.
                    //            terrainMat.EnableKeyword("_TERRAIN_INSTANCED_PERPIXEL_NORMAL");
                    //            terrainMat.SetFloat("_TERRAIN_INSTANCED_PERPIXEL_NORMAL", 1f);
                    //            #if UNITY_EDITOR
                    //            UnityEditor.EditorUtility.SetDirty(terrainMat);
                    //            #endif
                    //        }
                    //    }
                    //    else
                    //    {
                    //        Debug.LogWarning("HDRP material property _TERRAIN_INSTANCED_PERPIXEL_NORMAL is not available. Do you have HDRP 4.0.1 or newer in your project? Check Package Manager");
                    //    }
                    //}

                    terrain.materialTemplate = terrainMat;
                }
            }
            #endregion

            #region LBStandard
            else if (tMaterialType == TerrainMaterialType.LBStandard)
            {
                // Set material type to custom to allow us to use our own material
                #if !UNITY_2019_2_OR_NEWER
                terrain.materialType = Terrain.MaterialType.Custom;
                #endif
                terrainMat = null;
                // Check if a material has already been created for this landscape
                terrainMat = GetLandscapeLBStandardTerrainMaterial();
                if (terrainMat != null)
                {
                    // If a material has already been created, set the terrain width and use it
                    terrainMat.SetFloat("_TerrainWidth", terrainWidth);
                    terrain.materialTemplate = terrainMat;
                }
                else
                {
                    // If not, create and save a new material
                    // In v1.3.2 Beta 10+ expect it to be in Assets/LandscapeBuilder/Materials/Resources folder
                    Material tempMat = (Material)Resources.Load("LBTerrain", typeof(Material));

                    #if UNITY_EDITOR
                    if (tempMat == null)
                    {
                        Debug.LogWarning("LBTerrain material could not be found at path: Assets/LandscapeBuilder/Materials/Resources/LBTerrain.mat. Looking in old location..");
                        // Attempt to load from pre-v1.3.2 Beta 10 location
                        tempMat = (Material)UnityEditor.AssetDatabase.LoadAssetAtPath("Assets/LandscapeBuilder/Materials/LBTerrain.mat", typeof(Material));
                    }
                    #endif

                    // Create a new copy of the LBTerrain material
                    if (tempMat != null) { terrainMat = new Material(tempMat); }

                    if (terrainMat != null)
                    {
                        // Can only save the new material if in the editor (not available at runtime)
                        #if UNITY_EDITOR
                        UnityEditor.AssetDatabase.CreateAsset(terrainMat, "Assets/LandscapeBuilder/Materials/" + this.gameObject.name + "TerrainMaterial.mat");
                        #endif
                        // The terrain material has now been created, set the terrain width and use it
                        terrainMat.SetFloat("_TerrainWidth", terrainWidth);
                        terrain.materialTemplate = terrainMat;
                        // Send the newly created material back to the landscape for future reference
                        SetLandscapeLBStandardTerrainMaterial(terrainMat);
                    }
                    else
                    {
                        // If the material doesn't exist, show a warning and revert to the Built-In Standard shader
                        Debug.LogWarning("LBTerrain material not found at path: Assets/LandscapeBuilder/Materials/LBTerrain. Did you accidentally delete it? Reverting to Built-In Standard shader.");
                        #if UNITY_2019_2_OR_NEWER              
                        // Does the material already exist?
                        terrainMat = GetLandscapeTerrainCustomMaterial();
                        if (terrainMat == null || !terrainMat.shader.name.Contains("Nature/Terrain/Standard"))
                        {
                            // This is designed for runtime usage
                            terrain.materialTemplate = CreateTerrainMaterial(terrainMat, "LBTerrainStandard", "Assets/LandscapeBuilder/Materials/");
                        }
                        else { terrain.materialTemplate = terrainMat; }
                        #else
                        terrain.materialType = Terrain.MaterialType.BuiltInStandard;
                        #endif
                    }
                }
            }
            #endregion

            #region RTP
            else if (tMaterialType == TerrainMaterialType.ReliefTerrainPack)
            {
                if (!LBIntegration.isRTPInstalled(true))
                {
                    // Fallback to standard builtin material
                    #if UNITY_2019_2_OR_NEWER              
                    // Does the material already exist?
                    terrainMat = GetLandscapeTerrainCustomMaterial();
                    if (terrainMat == null || !terrainMat.shader.name.Contains("Nature/Terrain/Standard"))
                    {
                        // This is designed for runtime usage
                        terrain.materialTemplate = CreateTerrainMaterial(terrainMat, "LBTerrainStandard", "Assets/LandscapeBuilder/Materials/");
                    }
                    else { terrain.materialTemplate = terrainMat; }
                    #else
                    terrain.materialType = Terrain.MaterialType.BuiltInStandard;
                    #endif
                }
                else if (!LBIntegration.RTPValidation(this, true))
                {
                    // Fallback to standard builtin material
                    #if UNITY_2019_2_OR_NEWER              
                    // Does the material already exist?
                    terrainMat = GetLandscapeTerrainCustomMaterial();
                    if (terrainMat == null || !terrainMat.shader.name.Contains("Nature/Terrain/Standard"))
                    {
                        // This is designed for runtime usage
                        terrain.materialTemplate = CreateTerrainMaterial(terrainMat, "LBTerrainStandard", "Assets/LandscapeBuilder/Materials/");
                    }
                    else { terrain.materialTemplate = terrainMat; }
                    #else
                    terrain.materialType = Terrain.MaterialType.BuiltInStandard;
                    #endif
                }
                else
                {
                    // If the terrain already has a custom terrain material, RTP users that material rather than
                    // creating it's own and assigning it's own shader. This overcomes that issue.
                    if (prevtMaterialType == TerrainMaterialType.LBStandard || prevtMaterialType == TerrainMaterialType.Custom)
                    {
                        //Debug.Log("SetTerrainMaterial mat1: " + terrain.materialTemplate.name);
                        terrain.materialTemplate = null;
                        #if UNITY_2019_2_OR_NEWER              
                        // Does the material already exist?
                        terrainMat = GetLandscapeTerrainCustomMaterial();
                        if (terrainMat == null || !terrainMat.shader.name.Contains("Nature/Terrain/Standard"))
                        {
                            // This is designed for runtime usage
                            terrain.materialTemplate = CreateTerrainMaterial(terrainMat, "LBTerrainStandard", "Assets/LandscapeBuilder/Materials/");
                        }
                        else { terrain.materialTemplate = terrainMat; }
                        #else
                        terrain.materialType = Terrain.MaterialType.BuiltInStandard;
                        #endif
                    }

                    // Set material type to custom to allow us to use our own material
                    #if !UNITY_2019_2_OR_NEWER
                    terrain.materialType = Terrain.MaterialType.Custom;
                    #endif

                    if (this.gameObject != null)
                    {
                        if (!LBIntegration.RTPEnable(this, terrain, true, isFirstTerrain, isLastTerrain, true))
                        {
                            // Rollback the change
                            if (LBIntegration.RTPEnable(this, terrain, false, isFirstTerrain, isLastTerrain, false)) { }
                            #if UNITY_2019_2_OR_NEWER              
                            // Does the material already exist?
                            terrainMat = GetLandscapeTerrainCustomMaterial();
                            if (terrainMat == null || !terrainMat.shader.name.Contains("Nature/Terrain/Standard"))
                            {
                                // This is designed for runtime usage
                                terrain.materialTemplate = CreateTerrainMaterial(terrainMat, "LBTerrainStandard", "Assets/LandscapeBuilder/Materials/");
                            }
                            else { terrain.materialTemplate = terrainMat; }
                            #else
                            terrain.materialType = Terrain.MaterialType.BuiltInStandard;
                            #endif
                        }
                        else
                        {
                            // RTP creates the custom materials when the component is added to the terrain
                            // So set our customer material to the one RTP created.
                            terrainCustomMaterial = terrain.materialTemplate;
                        }
                    }
                }
            }
            #endregion

            #region MegaSplat
            else if (tMaterialType == TerrainMaterialType.MegaSplat)
            {
                Debug.LogWarning("LandscapeBuilder - Setting MegaSplat terrain materials at runtime is currently not supported by MegaSplat.");
                // Fallback to standard builtin material
                #if UNITY_2019_2_OR_NEWER              
                // Does the material already exist?
                terrainMat = GetLandscapeTerrainCustomMaterial();
                if (terrainMat == null || !terrainMat.shader.name.Contains("Nature/Terrain/Standard"))
                {
                    // This is designed for runtime usage
                    terrain.materialTemplate = CreateTerrainMaterial(terrainMat, "LBTerrainStandard", "Assets/LandscapeBuilder/Materials/");
                }
                else { terrain.materialTemplate = terrainMat; }
                #else
                terrain.materialType = Terrain.MaterialType.BuiltInStandard;
                #endif

            }
            #endregion

            #region MicroSplat
            else if (tMaterialType == TerrainMaterialType.MicroSplat)
            {
                Debug.LogWarning("LandscapeBuilder - Setting MicroSplat terrain materials at runtime is currently not supported.");
                // Fallback to standard builtin material
                #if UNITY_2019_2_OR_NEWER              
                // Does the material already exist?
                terrainMat = GetLandscapeTerrainCustomMaterial();
                if (terrainMat == null || !terrainMat.shader.name.Contains("Nature/Terrain/Standard"))
                {
                    // This is designed for runtime usage
                    terrain.materialTemplate = CreateTerrainMaterial(terrainMat, "LBTerrainStandard", "Assets/LandscapeBuilder/Materials/");
                }
                else { terrain.materialTemplate = terrainMat; }
                #else
                terrain.materialType = Terrain.MaterialType.BuiltInStandard;
                #endif

            }
            #endregion

            else
            {
                #if !UNITY_2019_2_OR_NEWER
                terrain.materialType = Terrain.MaterialType.Custom;
                #endif
                terrain.materialTemplate = terrainCustomMaterial;
            }
        }

        /// <summary>
        /// In U2019.2+, the build-in render pipeline requires a materialTemplate to be set rather than a materialType.
        /// This method creates a new material. If used in the Unity Editor, the material is also added to the
        /// AssetDatabase (i.e. saved to the Project folder).
        /// For HDRP/LWRP/URP call CreateTerrainMaterial(Material terrainMat, string outputPath).
        /// </summary>
        /// <param name="terrainMat"></param>
        /// <param name="targetMaterialName"></param>
        /// <param name="outputPath"></param>
        /// <returns></returns>
        public Material CreateTerrainMaterial(Material terrainMat, string targetMaterialName, string outputPath)
        {
            Material newMat = null;

            Material tempMat = (Material)Resources.Load(targetMaterialName, typeof(Material));

            if (tempMat == null)
            {
                #if UNITY_EDITOR
                Debug.LogWarning("LBLandscape.CreateTerrainMaterial could not load material: " + targetMaterialName);
                #endif
            }
            else
            {
                // Create a copy of the material
                newMat = new Material(tempMat);
                
                if (newMat != null)
                {
                    newMat.name = this.name + "TerrainMaterial.mat";
                    terrainCustomMaterial = newMat;
                    #if UNITY_EDITOR
                    LBEditorHelper.CheckFolderStructure(outputPath);
                    UnityEditor.AssetDatabase.CreateAsset(newMat, outputPath + newMat.name);
                    UnityEditor.AssetDatabase.Refresh();
                    #endif
                }
            }

            return newMat;
        }

        /// <summary>
        /// In U2019.2+, HDRP/LWRP/URP require a materialTemplate to be set rather than a materialType.
        /// This method creates a new material. If used in the Unity Editor, the material is also added to the
        /// AssetDatabase (i.e. saved to the Project folder).
        /// The HDRP/LWRP/URP terrain material is passed in as a parameter which is used to create a new copy.
        /// </summary>
        /// <param name="terrainMat"></param>
        /// <param name="outputPath"></param>
        /// <returns></returns>
        public Material CreateTerrainMaterial(Material terrainMat, string outputPath)
        {
            Material newMat = null;

            if (terrainMat == null)
            {
                #if UNITY_EDITOR
                Debug.LogWarning("LBLandscape.CreateTerrainMaterial no terrain material supplied");
                #endif
            }
            else
            {
                // Create a copy of the material
                newMat = new Material(terrainMat);
                
                if (newMat != null)
                {
                    newMat.name = this.name + "TerrainMaterial.mat";
                    terrainCustomMaterial = newMat;
                    #if UNITY_EDITOR
                    LBEditorHelper.CheckFolderStructure(outputPath);
                    UnityEditor.AssetDatabase.CreateAsset(newMat, outputPath + newMat.name);
                    UnityEditor.AssetDatabase.Refresh();
                    #endif
                }
            }

            return newMat;
        }

        /// <summary>
        /// Get the TerrainMaterialType from the landscape. Prior to 2019.2 it examines the Terrain.MaterialType
        /// of the first terrain in the landscape. For custom terrain materials like for LB Standard, LWRP, URP,
        /// HDRP, and RTP, it also looks at the material shader name.
        /// From 2019.2+ this method always examines the materialTemplate (Material).
        /// </summary>
        public LBLandscape.TerrainMaterialType GetTerrainMaterialType()
        {
            LBLandscape.TerrainMaterialType _terrainMaterialType = LBLandscape.TerrainMaterialType.BuiltInStandard;

            #if !UNITY_2019_2_OR_NEWER
            Terrain.MaterialType tempMaterialType = GetLandscapeTerrainMaterialType();

            if (tempMaterialType == Terrain.MaterialType.BuiltInStandard) { _terrainMaterialType = LBLandscape.TerrainMaterialType.BuiltInStandard; }
            else if (tempMaterialType == Terrain.MaterialType.BuiltInLegacyDiffuse) { _terrainMaterialType = LBLandscape.TerrainMaterialType.BuiltInLegacyDiffuse; }
            else if (tempMaterialType == Terrain.MaterialType.BuiltInLegacySpecular) { _terrainMaterialType = LBLandscape.TerrainMaterialType.BuiltInLegacySpecular; }
            #else
            // U2019.2+ no longer uses a materialType. Instead it just uses the materialTemplate
            if (terrainCustomMaterial == null)
            {
                terrainCustomMaterial = GetLandscapeTerrainCustomMaterial();
            }
            if (terrainCustomMaterial == null) { Debug.LogWarning("LBLandscape.GetTerrainMaterialType() terrain materialTemplate is null. Check your Terrain Settings on the Landscape tab."); }
            else if (terrainCustomMaterial.shader.name == "Nature/Terrain/Standard") { _terrainMaterialType = LBLandscape.TerrainMaterialType.BuiltInStandard; }
            else if (terrainCustomMaterial.shader.name == "Nature/Terrain/Diffuse") { _terrainMaterialType = LBLandscape.TerrainMaterialType.BuiltInLegacyDiffuse; }
            else if (terrainCustomMaterial.shader.name == "Nature/Terrain/Specular") { _terrainMaterialType = LBLandscape.TerrainMaterialType.BuiltInLegacySpecular; }

            #endif
            else
            {
                // If the customMaterial variable isn't set, try to discover it.
                // Added in LB 2.0.7 Beta 7b for LWRP
                if (terrainCustomMaterial == null)
                {
                    terrainCustomMaterial = GetLandscapeTerrainCustomMaterial();
                }

                if (terrainCustomMaterial != null)
                {
                    //Debug.Log("GetTerrainMaterialType " + terrainCustomMaterial.shader.name);

                    if (terrainCustomMaterial.shader.name == "Nature/Terrain/LB Standard") { _terrainMaterialType = LBLandscape.TerrainMaterialType.LBStandard; }
                    else if (terrainCustomMaterial.shader.name == "Relief Pack/ReliefTerrain-FirstPass") { _terrainMaterialType = LBLandscape.TerrainMaterialType.ReliefTerrainPack; }
                    else if (terrainCustomMaterial.shader.name.Contains("MegaSplat")) { _terrainMaterialType = LBLandscape.TerrainMaterialType.MegaSplat; }
                    else if (terrainCustomMaterial.shader.name.Contains("MicroSplat")) { _terrainMaterialType = LBLandscape.TerrainMaterialType.MicroSplat; }
                    // HDRP 4.9.0+
                    else if (terrainCustomMaterial.shader.name.Contains("HDRP/TerrainLit")) { _terrainMaterialType = LBLandscape.TerrainMaterialType.HDRP; }
                    // HDRP 4.0.1+
                    else if (terrainCustomMaterial.shader.name.Contains("HDRenderPipeline/TerrainLit")) { _terrainMaterialType = LBLandscape.TerrainMaterialType.HDRP; }
                    // URP 7.1.2+
                    else if (terrainCustomMaterial.shader.name.Contains("Universal Render Pipeline/Terrain/Lit")) { _terrainMaterialType = LBLandscape.TerrainMaterialType.URP; }
                    // LWRP 4.0.1+
                    else if (terrainCustomMaterial.shader.name.Contains("Lightweight Render Pipeline/Terrain/Lit")) { _terrainMaterialType = LBLandscape.TerrainMaterialType.LWRP; }
                    // LWRP 3.3.0
                    else if (terrainCustomMaterial.shader.name.Contains("LightweightPipeline/Terrain/Standard Terrain")) { _terrainMaterialType = LBLandscape.TerrainMaterialType.LWRP; }
                    else { _terrainMaterialType = LBLandscape.TerrainMaterialType.Custom; }
                }
                else { _terrainMaterialType = LBLandscape.TerrainMaterialType.Custom; }
            }

            return _terrainMaterialType;
        }


        #endregion

        #region Presets

        // Creates an LBLayer object based on a given preset
        public void SetThermalErosionSettingsFromPreset (ThermalErosionPreset tePreset)
        {
            // Set LBLayer variables according to the given preset
            if (tePreset == ThermalErosionPreset.ExtremeSoilSlippage)
            {
                thermalErosionTalusAngle = 0f;
                thermalErosionStrength = 0.2f;
            }
            else if (tePreset == ThermalErosionPreset.SlowSoilSlippage)
            {
                thermalErosionTalusAngle = 15f;
                thermalErosionStrength = 0.2f;
            }
            else if (tePreset == ThermalErosionPreset.FastSoilSlippage)
            {
                thermalErosionTalusAngle = 15f;
                thermalErosionStrength = 0.8f;
            }
            else if (tePreset == ThermalErosionPreset.SlowWeathering)
            {
                thermalErosionTalusAngle = 30f;
                thermalErosionStrength = 0.1f;
            }
            else if (tePreset == ThermalErosionPreset.FastWeathering)
            {
                thermalErosionTalusAngle = 30f;
                thermalErosionStrength = 0.8f;
            }
            else if (tePreset == ThermalErosionPreset.SoftEarthSlippage)
            {
                thermalErosionTalusAngle = 45f;
                thermalErosionStrength = 0.2f;
            }
            else
            {
                thermalErosionTalusAngle = 60f;
                thermalErosionStrength = 0.25f;
            }
        }

        #endregion

        #region Delegate Progress Bar

        /// <summary>
        /// Show the progress bar with a % complete.
        /// TODO - convert into in-window progress bar
        /// This is passed to other classes as a delegate callback function. See Public Delegates at the top
        /// of this class. 
        /// </summary>
        /// <param name="title"></param>
        /// <param name="contentMsg"></param>
        /// <param name="percentComplete"></param>
        public static void ShowProgressBar(string title, string contentMsg, float percentComplete)
        {
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.DisplayProgressBar(title, contentMsg + " - Please wait", percentComplete);
            //Debug.Log("Progress: " + contentMsg);
            #endif
        }

        #endregion

        #region Non-Static Camera Methods

        // Refresh a list of camera with those in the current scene.
        public void RefreshCameraList(List<Camera> cameraList)
        {
            if (cameraList == null) { cameraList = new List<Camera>(); }
            else { cameraList.Clear(); }
            
            cameraList.AddRange(GameObject.FindObjectsOfType<Camera>());

            // If present, remove the celestrials and water reflection cameras
            cameraList.RemoveAll(c => c.gameObject.name == "Celestials Camera" || c.gameObject.name.StartsWith("Water4AdvancedReflection"));
        }

        #endregion

        #region Static Camera Methods

        /// <summary>
        /// Given a point in worldscape, is it [potentially] in view of the supplied camera?
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="worldSpacePosition"></param>
        /// <returns></returns>
        public static bool IsPointInCameraView(Camera camera, Vector3 worldSpacePosition)
        {
            if (camera != null)
            {
                Vector3 screenPosition = camera.WorldToScreenPoint(worldSpacePosition);

                bool isBehindPlayer = screenPosition.z < 0.0f;
                bool offLeftEdge = screenPosition.x < 0.0f;
                bool offRightEdge = screenPosition.x > Screen.width;
                bool offLowerEdge = screenPosition.y < 0.0f;
                bool offUpperEdge = screenPosition.y > Screen.height;
                
                return (!(isBehindPlayer || offLeftEdge || offRightEdge || offLowerEdge || offUpperEdge));
            }
            else { return false; }
        }

        #endregion

        #region Scriptable Render Pipeline Methods

        /// <summary>
        /// Is the High Definition Render Pipeline installed in this project?
        /// </summary>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static bool IsHDRP(bool showErrors)
        {
            bool isInstalled = false;

            try
            {
                //System.Type hdRenderPipeLineAssetType = System.Type.GetType("Unity.RenderPipelines.HighDefinition.Runtime, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null", true, true);
                //if (hdRenderPipeLineAssetType != null) { isInstalled = true; }
                //hdRenderPipeLineAssetType = null;
                var renderPipelineAsset = UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset;
                if (renderPipelineAsset != null && renderPipelineAsset.GetType().Name.Contains("HDRenderPipelineAsset")) { isInstalled = true; }
                else if (showErrors) { Debug.LogWarning("LBEditorHelper.IsHDRP: it appears that High Definition Render Pipeline is not installed in this project."); }
                //Debug.Log("renderPipelineAsset: " + (renderPipelineAsset.GetType()).Name);
            }
            catch
            {
                if (showErrors) { Debug.LogWarning("LBEditorHelper.IsHDRP: it appears that High Definition Render Pipeline is not installed in this project."); }
            }
            return isInstalled;
        }

        /// <summary>
        /// Is the Light Weight Render Pipeline installed in this project?
        /// </summary>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static bool IsLWRP(bool showErrors)
        {
            bool isInstalled = false;

            try
            {
                var renderPipelineAsset = UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset;

                if (renderPipelineAsset != null && renderPipelineAsset.GetType().Name.Contains("LightweightRenderPipelineAsset")) { isInstalled = true; }
                else if (showErrors) { Debug.LogWarning("LBEditorHelper.IsLWRP: it appears that Light Weight Render Pipeline is not installed in this project."); }
                
                //Debug.Log("renderPipelineAsset: " + (renderPipelineAsset.GetType()).Name);
            }
            catch
            {
                if (showErrors) { Debug.LogWarning("LBEditorHelper.IsLWRP: it appears that Light Weight Render Pipeline is not installed in this project."); }
            }

            return isInstalled;
        }

        /// <summary>
        /// Is the Universal Render Pipeline installed in this project?
        /// </summary>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static bool IsURP(bool showErrors)
        {
            bool isInstalled = false;

            try
            {
                var renderPipelineAsset = UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset;

                if (renderPipelineAsset != null && renderPipelineAsset.GetType().Name.Contains("UniversalRenderPipelineAsset")) { isInstalled = true; }
                else if (showErrors) { Debug.LogWarning("LBEditorHelper.IsURP: it appears that Universal Render Pipeline is not installed in this project."); }

                //Debug.Log("renderPipelineAsset: " + (renderPipelineAsset.GetType()).Name);
            }
            catch
            {
                if (showErrors) { Debug.LogWarning("LBEditorHelper.IsURP: it appears that Light Weight Render Pipeline is not installed in this project."); }
            }

            return isInstalled;
        }

        #endregion

        #region Scripting Methods (Editor Only)

#if UNITY_EDITOR

        public string ScriptLandscapeSettings(string landscapeName, string EndOfLineMarker)
        {
            // Create a new instance of StringBuilder and give it an estimated capacity
            System.Text.StringBuilder sb = new System.Text.StringBuilder(1000);

            string eol = " ";

            // We always need a space between lines OR a end of line marker like "\n"
            if (EndOfLineMarker.Length > 0) { eol = EndOfLineMarker; }

            const string csRegion = "#region";
            const string csRegionTab1 = "\t#region";
            string csEndRegion = "#endregion" + eol + eol;
            string csEndRegionTab1 = "\t#endregion" + eol + eol;

            sb.Append("using UnityEngine;" + eol);
            sb.Append("using System.Collections;" + eol);
            sb.Append("using System.Collections.Generic;" + eol);
            sb.Append("using LandscapeBuilder;" + eol);
            sb.Append(eol);
            sb.Append("// Landscape Builder script settings segment for " + landscapeName + eol);
            sb.Append("// Generated from Landscape Last Updated Version: " + LastUpdatedVersion + eol);
            sb.Append("[RequireComponent(typeof(LBLandscape))]" + eol);
            sb.Append("public class RuntimeLandscape1 : MonoBehaviour { " + eol);
            sb.Append(csRegion);
            sb.Append(" Public Variables");
            sb.Append(eol);
            sb.Append("public Vector2 landscapeSize = Vector2.one * " + this.size.x.ToString() + "f;" + eol);
            sb.Append("public float terrainWidth = " + GetLandscapeTerrainWidth().ToString() + "f;" + eol);
            sb.Append("public float terrainHeight = " + GetLandscapeTerrainHeight().ToString() + "f;" + eol);
            sb.Append(eol);
            sb.Append("public bool showErrors = false;" + eol);
            sb.Append("public bool useGPUTopography = true;" + eol);
            sb.Append("public bool useGPUTexturing = true;" + eol);
            sb.Append("public bool useGPUGrass = true;" + eol);
            sb.Append("public bool useGPUPath = true;" + eol);
            sb.Append(csEndRegion);
            sb.Append(csRegion);
            sb.Append(" Private variables");
            sb.Append(eol);
            sb.Append("private LBLandscape landscape = null;" + eol);
            sb.Append("private List<LBLayer> topographyLayers;" + eol);
            sb.Append(csEndRegion);
            sb.Append("void Awake()" + eol + "{" + eol);

            sb.Append(csRegionTab1);
            sb.Append(" Initialise");
            sb.Append(eol);

            sb.Append("\t// Get a link to the LBLandscape script" + eol);
            sb.Append("\tlandscape = this.GetComponent<LBLandscape>();" + eol + eol);

            sb.Append("\tif (landscape == null)" + eol + "\t{" + eol);
            sb.Append("\t\tDebug.Log(\"Could not add LBLandscape script to gameobject at Runtime\");" + eol);
            sb.Append("\t\treturn;" + eol + "\t}" + eol + eol);

            sb.Append("\telse if (landscape.IsGPUAccelerationAvailable())" + eol + "\t{" + eol);
            sb.Append("\t\tlandscape.useGPUGrass = useGPUGrass;" + eol);
            sb.Append("\t\tlandscape.useGPUTexturing = useGPUTexturing;" + eol);
            sb.Append("\t\tlandscape.useGPUTopography = useGPUTopography;" + eol);
            sb.Append("\t\tlandscape.useGPUPath = useGPUPath;" + eol + "\t}" + eol);
            sb.Append("\telse" + eol + "\t{" + eol);
            sb.Append("\t\t#if UNITY_EDITOR" + eol);
            sb.Append("\t\tif (useGPUTopography || useGPUTexturing || useGPUGrass || useGPUPath)" + eol + "\t\t{" + eol);
            sb.Append("\t\t\tDebug.Log(\"Sorry, your hardware does not support GPU acceleration\");" + eol + "\t\t}" + eol);
            sb.Append("\t\t#endif" + eol);
            sb.Append("\t\tlandscape.useGPUTopography = false;" + eol);
            sb.Append("\t\tlandscape.useGPUTexturing = false;" + eol);
            sb.Append("\t\tlandscape.useGPUGrass = false;" + eol);
            sb.Append("\t\tlandscape.useGPUPath = false;" + eol + "\t}" + eol);
            sb.Append(csEndRegionTab1);

            sb.Append("\t// Update the size" + eol);
            sb.Append("\tlandscape.size = landscapeSize;" + eol);
            sb.Append(eol);
            sb.Append(csRegionTab1);
            sb.Append(" Create the terrains");
            sb.Append(eol);
            sb.Append("\tint terrainNumber = 0;" + eol + eol);

            sb.Append("\tfor (float tx = 0f; tx < landscapeSize.x - 1f; tx += terrainWidth)" + eol);
            sb.Append("\t{" + eol);
            sb.Append("\t\tfor (float ty = 0f; ty < landscapeSize.y - 1f; ty += terrainWidth)" + eol);
            sb.Append("\t\t{" + eol);
            sb.Append("\t\t\t// Create a new gameobject" + eol);
            sb.Append("\t\t\tGameObject terrainObj = new GameObject(\"RuntimeTerrain\" + (terrainNumber++).ToString(\"000\"));" + eol);
            sb.Append("\t\t\t// Create a new gameobject" + eol);

            sb.Append("\t\t\t// Correctly parent and position the terrain" + eol);
            sb.Append("\t\t\tterrainObj.transform.parent = this.transform;" + eol);
            sb.Append("\t\t\tterrainObj.transform.localPosition = new Vector3(tx, 0f, ty);" + eol);

            sb.Append("\t\t\t// Add a terrain component" + eol);
            sb.Append("\t\t\tTerrain newTerrain = terrainObj.AddComponent<Terrain>();" + eol);

            sb.Append("\t\t\t// Set terrain settings)" + eol);
            sb.Append("\t\t\tnewTerrain.heightmapPixelError = " + GetLandscapeTerrainPixelError().ToString() + "f;" + eol);
            sb.Append("\t\t\tnewTerrain.basemapDistance = " + GetLandscapeTerrainBaseMapDist().ToString() + "f;" + eol);
            sb.Append("\t\t\tnewTerrain.treeDistance = " + GetLandscapeTerrainTreeDistance().ToString() + "f;" + eol);
            sb.Append("\t\t\tnewTerrain.treeBillboardDistance = " + GetLandscapeTerrainTreeBillboardStart() + "f;" + eol);
            sb.Append("\t\t\tnewTerrain.detailObjectDistance = " + GetLandscapeTerrainDetailDistance().ToString() + "f;" + eol);
            sb.Append("\t\t\tnewTerrain.treeCrossFadeLength = " + GetLandscapeTerrainTreeFadeLength().ToString() + "f;" + eol);

            sb.Append("\t\t\t// Set terrain data settings" + eol);
            sb.Append("\t\t\tTerrainData newTerrainData = new TerrainData();" + eol + eol);

            sb.Append("\t\t\tnewTerrainData.heightmapResolution = " + GetLandscapeTerrainHeightmapResolution().ToString() + ";" + eol);
            sb.Append("\t\t\tnewTerrainData.size = new Vector3(terrainWidth, terrainHeight, terrainWidth);" + eol);
            // Currently LB uses a fixed detail (grass) patch resolution
            sb.Append("\t\t\tnewTerrainData.SetDetailResolution(" + GetLandscapeTerrainDetailResolution().ToString() + ", 16);" + eol);
            sb.Append("\t\t\tnewTerrain.terrainData = newTerrainData;" + eol + eol);

            sb.Append("\t\t\t// Set up the terrain collider" + eol);
            sb.Append("\t\t\tTerrainCollider newTerrainCol = terrainObj.AddComponent<TerrainCollider>();" + eol);
            sb.Append("\t\t\tnewTerrainCol.terrainData = newTerrainData;" + eol + eol);

            sb.Append("\t\t}" + eol);
            sb.Append("\t}" + eol);
            sb.Append(csEndRegionTab1); // end creating terrains

            sb.Append("\tlandscape.SetLandscapeTerrains(true);" + eol + eol);

            sb.Append("} // end of Awake()" + eol);
            sb.Append("} // end of RuntimeLandscape1 class" + eol);
            sb.Append("// --- END OF SCRIPT ---" + eol + eol);

            return sb.ToString();
        }

        #endif
        #endregion
    }
}
