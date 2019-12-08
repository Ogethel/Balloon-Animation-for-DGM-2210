#define LB_COMPUTE

// If changing LB_COMPUTE, also change in LBLandscapeTerrain.cs
// Landscape Builder. Copyright (c) 2016-2019 SCSM Pty Ltd. All rights reserved.
using System.Collections.Generic;
using UnityEngine;

namespace LandscapeBuilder
{
    /// <summary>
    /// [INTERNAL USE ONLY]
    /// Non-serializable class to pass variables (parameters)
    /// for a LBGroup to a method that processes individual
    /// LBGroups. Currently used in LBLandscapeTerrain.PopulateLandscapeWithGroups(..)
    /// and LBLandscapeTerrain.PopulateLandscapeWithGroup(..)
    /// </summary>
    public class LBGroupVariables
    {
        #region Public varibles
        public LBGroupParameters lbGroupParams;

        #region Define Designer variables
        public bool isObjPathDesignerEnabled;
        public bool isGroupDesignerEnabled;
        public Vector3 groupDesignerPos;
        #endregion

        #region Define Landscape-scoped variables
        public bool isPathComputeEnabled;

        // Landscape details
        public string landscapeName;
        public float landscapeWidth;
        public float landscapeLength;
        public float terrainHeight;
        public int landscapeHeightmapSize;
        public int landscapeHeightmapResolution;
        public float landscapeHeightmapResolutionF;
        public Vector3 terrainHeightmapCellSize;    // The size of each heightmap sample
        public Vector3 landscapePosition;
        public Rect landscapeBounds;

        public bool terrainDataIsNotNull = true;

        #if LB_COMPUTE
        // Currently only used for LBObjPath texturing. Set to 1 to avoid div0
        public float terrainWidth = 1f;
        public float terrainLength = 1f;
        #endif

        public Terrain[] landscapeTerrains;
        public TerrainData[] terrainDataArray;
        public Rect[] terrainRectsArray;
        public int numTerrains;
        public int numTerrainsWide;

        public float perSqrKmFactor;

        #endregion

        #region Define Active Group variables
        public List<LBGroup> activeGroupList;
        public int numActiveGroups;
        #endregion

        #region Object Path Compute Variables
        #if LB_COMPUTE
        public int cskPathNumThreads;
        public int cskCopyNumThreads;
        public ComputeShader shaderPath;
        public ComputeBuffer cbufHeightsIn;
        public ComputeBuffer cbufHeightsOut;
        public ComputeBuffer cbufSplinePointsCentre;
        public ComputeBuffer cbufSplinePointsLeft;
        public ComputeBuffer cbufSplinePointsRight;
        public ComputeBuffer cbufObjPathSurroundBlendCurve;
        public ComputeBuffer cbufObjPathProfileHeightCurve;

        // Texturing
        public ComputeShader shaderTex;
        public ComputeBuffer cbufSplatMaps;
        public ComputeBuffer cbufSplatAdditions;

        // Trees
        public ComputeBuffer cbufLBObjectProximities;
        public ComputeBuffer cbufObjPathRemoveTreeIndexes;
        #endif
        #endregion

        #region Group Proximity variables
        public int proximityGroupCellsListWidth;
        public int totalProximityGroupCells;
        public List<LBObjectProximity>[] groupProximitiesList;
        // Proximity variables
        public int thisGroupProximityCellIndex;
        public int proximityGroupCellXCoord;
        public int proximityGroupCellZCoord;
        public List<LBObjectProximity> thisGroupProximitiesList;
        #endregion

        #region Member Proximity variables
        public int proximityMemberCellsListWidth;
        public int totalProximityMemberCells;
        public List<LBObjectProximity>[] objectProximitiesList;
        // Proximity variables
        public int thisObjectProximityCellIndex;
        public int proximityMemberCellXCoord;
        public int proximityMemberCellZCoord;
        public List<LBObjectProximity> thisObjectProximitiesList;
        #endregion

        #region Tree Proximity variables
        public int proximityTreeCellsListWidth;
        public int totalProximityTreeCells;
        public List<LBObjectProximity>[] treeProximitiesList;
        // Proximity variables
        public int thisTreeProximityCellIndex;
        public int proximityTreeCellXCoord;
        public int proximityTreeCellZCoord;
        // The size in metres of each cell
        public float proximityTreeCellSize;
        public int proximityTreeCellMinXCoord;
        public int proximityTreeCellMaxXCoord;
        public int proximityTreeCellMinZCoord;
        public int proximityTreeCellMaxZCoord;
        public int proximityTreeCellBlockHalfWidth;
        public List<LBObjectProximity> thisTreeProximitiesList;
        // Initialise variables for removing trees
        public List<LBObjectProximity> treesToRemoveList;
        public List<LBObjectProximity> thisTreesToRemoveList;
        #endregion

        #region Grass Array variables
        public bool isRemoveGrassPresent;
        public bool isGrassPopulationPresent;

        // A grass array needs to be size of the sum of all grass arrays in each terrain
        public int terrainDetailResolution;
        public int grassArrayCellsListWidth;

        public int totalGrassArrayCells;
        // Grass removal array: 255 = previous grass strengh, 0 = all grass removed 
        public byte[] initialGrassRemovalArray;
        public byte[] finalGrassRemovalArray;
        public int totalGrassAdditionArrays;
        public int grassAdditionArraySize;
        public byte[] grassAdditionArray;
        // The size in metres of each cell
        public float grassArrayCellSize;
        public int thisGrassArrayCellIndex;
        public int grassArrayCellXCoord;
        public int grassArrayCellZCoord;
        public int grassAreaRadius;
        public float grassAreaNoBlendRadius;
        public int grassAreaCentreXIndex;
        public int grassAreaCentreZIndex;
        public int grassAreaMinXIndex;
        public int grassAreaMaxXIndex;
        public int grassAreaMinZIndex;
        public int grassAreaMaxZIndex;
        public int grassAdditionArrayIndexShift;
        // Grass removal variables
        public float grassRemovalBlendFactor;
        public float grassRemovalDist;
        // Grass population variables
        public float grassPlacementDist;
        public float grassPopulationBlendFactor;
        public float grassNoiseValue;

        public List<LBTerrainGrass> terrainGrassList;
        public int terrainGrassListSize;
        public int grsIdx;

        // The Group-level Grass option tab contains GUIDs that reference LBTerrainGrass instances in the
        // landscape class terrainGrassList. These are LBTerrainGrass instances that have been applied
        // to the landscape. Some LBTerrainGrass instances may be disabled or don't have a valid grass texture.
        // This list contains the matching index of the Grass Tab's LBTerrainGrass in the
        // array of unique detailPrototypes for all terrains.
        public List<int> terrainGrassArrayIndexList;

        #endregion

        #region Texture Array variables

        public bool isTexturingPresent;
        public bool isObjPathTexturingPresent;

        public int terrainAlphamapResolution;
        public int textureArrayCellsListWidth;

        public int totalTextureArrayCells;
        // Texture array: 255 = add highest strength, 0 = add nothing
        public int totalTextureAdditionArrays;
        public int textureAdditionArraySize;
        public byte[] textureAdditionArray;
        // Size in metres of each cell
        public float textureArrayCellSize;

        public int thisTextureArrayCellIndex;
        public int textureArrayCellXCoord;
        public int textureArrayCellZCoord;
        public int textureAreaRadius;
        public float textureAreaNoBlendRadius;
        public int textureAreaCentreXIndex;
        public int textureAreaCentreZIndex;
        public int textureAreaMinXIndex;
        public int textureAreaMaxXIndex;
        public int textureAreaMinZIndex;
        public int textureAreaMaxZIndex;
        public int textureAdditionArrayIndexShift;
        // Texture placement variables
        public float texturePlacementDist;
        public float textureBlendFactor;

        public List<LBTerrainTexture> terrainTextureList;
        public int terrainTextureListSize;
        public int txIdx;

        // The Group-level Tex option tab contains GUIDs that reference LBTerrainTexture instances in the
        // landscape class terrainTexturesList. These are LBTerrainTexture instances that have been applied
        // to the landscape. Some LBTerrainTexture instances may be disabled or don't have a valid Texture2D.
        // This list contains the matching index of the Texturing Tab's LBTerrainTexture in the
        // array of unique splatPrototypes for all terrains.
        public List<int> terrainTextureArrayIndexList;

        #endregion

        #region Mesh Controller Variables
        public LBTerrainMeshController terrainMeshController;
        #endregion

        #region Stencil variables
        public bool[] isStencilLayerFiltersToApply;
        // Stencil LBLayerFilter temp variable
        public Vector2 stencilLayerPosN;
        #endregion

        #region Vegetation Studio Pro variables
        #if VEGETATION_STUDIO_PRO
        public AwesomeTechnologies.VegetationSystem.VegetationSystemPro vsPro;
        #endif
        #endregion

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor to create an instance given a LBGroupParameters class instance.
        /// NOTE: There is no default class constructor that takes no paramters.
        /// </summary>
        /// <param name="groupParams"></param>
        public LBGroupVariables(LBGroupParameters groupParams)
        {
            if (groupParams != null && groupParams.landscape != null)
            {
                lbGroupParams = groupParams;

                #region Designer variables
                isObjPathDesignerEnabled = (groupParams.lbObjPathParm != null);
                isGroupDesignerEnabled = groupParams.isGroupDesignerEnabled;
                groupDesignerPos = new Vector3(0f, groupParams.designerOffsetY, 0f);
                #endregion

                #region Landscape-scoped variables

                #if !(UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN || UNITY_XBOXONE || UNITY_WSA_10_0)
                isPathComputeEnabled = false;
                #else
                isPathComputeEnabled = (groupParams.landscape == null ? false : groupParams.landscape.useGPUPath);
                #endif

                // Landscape details
                landscapeName = groupParams.landscape.name;
                landscapeWidth = groupParams.landscape.size.x;
                landscapeLength = groupParams.landscape.size.y;
                terrainHeight = groupParams.landscape.GetLandscapeTerrainHeight();
                landscapeHeightmapSize = groupParams.landscape.GetTotalHeightmapSize(false);
                landscapeHeightmapResolution = Mathf.RoundToInt(Mathf.Sqrt(landscapeHeightmapSize));
                landscapeHeightmapResolutionF = (float)landscapeHeightmapResolution;
                terrainHeightmapCellSize = groupParams.landscape.GetLandscapeTerrainHeightmapScale();  // The size of each heightmap sample
                landscapePosition = groupParams.landscape.transform.position;
                landscapeBounds = groupParams.landscape.GetLandscapeWorldBoundsFast();

                terrainDataIsNotNull = true;

                #if LB_COMPUTE
                // Currently only used for LBObjPath texturing. Set to 1 to avoid div0
                terrainWidth = 1f;
                terrainLength = 1f;
                #endif

                groupParams.landscape.SetLandscapeTerrains(true);
                landscapeTerrains = groupParams.landscape.landscapeTerrains;
                terrainDataArray = new TerrainData[0];
                terrainRectsArray = new Rect[0];
                numTerrains = landscapeTerrains == null ? 0 : landscapeTerrains.Length;

                if (numTerrains > 0)
                {
                    // Create arrays for terrain data and terrain rects
                    terrainDataArray = new TerrainData[numTerrains];
                    terrainRectsArray = new Rect[numTerrains];
                    for (int i = 0; i < numTerrains; i++)
                    {
                        terrainDataArray[i] = landscapeTerrains[i].terrainData;
                        if (terrainDataArray[i] == null) { terrainDataIsNotNull = false; break; }
                        Vector3 landscapePositionVector = landscapeTerrains[i].transform.position - landscapePosition;
                        terrainRectsArray[i] = Rect.MinMaxRect(landscapePositionVector.x, landscapePositionVector.z, landscapePositionVector.x + landscapeTerrains[i].terrainData.size.x + 0.005f,
                            landscapePositionVector.z + landscapeTerrains[i].terrainData.size.z + 0.005f);
                    }

                    numTerrainsWide = (int)Mathf.Sqrt(numTerrains);

                    #if LB_COMPUTE
                    terrainWidth = terrainDataArray[0].size.x;
                    terrainLength = terrainDataArray[0].size.z;
                    #endif
                }
                else
                {
                    numTerrainsWide = 0;
                    terrainDataIsNotNull = false;
                }

                perSqrKmFactor = landscapeWidth * landscapeLength / 1000000f;

                #endregion
            }
        }

        #endregion

        #region Public Methods

        public void InitialiseObjPathComputeVariables()
        {
            #if LB_COMPUTE
            cskPathNumThreads = 16; // Must match LB_PATH_NUM_THREADS in LBCSPath.compute
            cskCopyNumThreads = 256; // Must match LB_CPY_NUM_THREADS in LBCSPath.compute
            shaderPath = null;
            cbufHeightsIn = null;
            cbufHeightsOut = null;
            cbufSplinePointsCentre = null;
            cbufSplinePointsLeft = null;
            cbufSplinePointsRight = null;
            cbufObjPathSurroundBlendCurve = null;
            cbufObjPathProfileHeightCurve = null;

            // Texturing
            shaderTex = null;
            cbufSplatMaps = null;
            cbufSplatAdditions = null;

            // Trees
            cbufLBObjectProximities = null;
            cbufObjPathRemoveTreeIndexes = null;
            #endif
        }

        public void InitialiseGroupProximity()
        {
            int proximityGroupCellsListWidth = 1;
            if (isGroupDesignerEnabled)
            {
                // Set proximityGroupCellsListWidth dynamically based on group radius and 2x the maximum proximity 
                proximityGroupCellsListWidth = (int)(activeGroupList[0].maxClearingRadius / LBGroup.GetMaxGroupProximityExtent(activeGroupList));
            }
            else
            {
                // Set proximityGroupCellsListWidth dynamically based on landscape width and 2x the maximum proximity of Clearings
                proximityGroupCellsListWidth = (int)(landscapeWidth * 0.5f / LBGroup.GetMaxGroupProximityExtent(activeGroupList));
            }
            if (proximityGroupCellsListWidth > 100) { proximityGroupCellsListWidth = 100; }
            if (proximityGroupCellsListWidth < 1) { proximityGroupCellsListWidth = 1; }

            totalProximityGroupCells = proximityGroupCellsListWidth * proximityGroupCellsListWidth;
            groupProximitiesList = new List<LBObjectProximity>[totalProximityGroupCells];
            for (int i = 0; i < totalProximityGroupCells; i++) { groupProximitiesList[i] = new List<LBObjectProximity>(); }
            // Initialise proximity variables
            thisGroupProximityCellIndex = 0;
            proximityGroupCellXCoord = 0;
            proximityGroupCellZCoord = 0;
            thisGroupProximitiesList = new List<LBObjectProximity>();
        }

        public void InitialiseMemberProximity()
        {
            proximityMemberCellsListWidth = 1;
            if (isGroupDesignerEnabled)
            {
                // Set proximityMemberCellsListWidth dynamically based on group radius and 2x the maximum proximity 
                proximityMemberCellsListWidth = (int)(activeGroupList[0].maxClearingRadius / LBGroup.GetMaxMemberProximityExtent(activeGroupList));
            }
            else
            {
                // Set proximityMemberCellsListWidth dynamically based on landscape width and 2x the maximum proximity                        
                proximityMemberCellsListWidth = (int)(landscapeWidth * 0.5f / LBGroup.GetMaxMemberProximityExtent(activeGroupList));
            }

            if (proximityMemberCellsListWidth > 100) { proximityMemberCellsListWidth = 100; }
            if (proximityMemberCellsListWidth < 1) { proximityMemberCellsListWidth = 1; }
            // Populate proximity lists
            totalProximityMemberCells = proximityMemberCellsListWidth * proximityMemberCellsListWidth;
            objectProximitiesList = new List<LBObjectProximity>[totalProximityMemberCells];
            for (int i = 0; i < totalProximityMemberCells; i++) { objectProximitiesList[i] = new List<LBObjectProximity>(); }
            // Initialise proximity variables
            thisObjectProximityCellIndex = 0;
            proximityMemberCellXCoord = 0;
            proximityMemberCellZCoord = 0;
            thisObjectProximitiesList = new List<LBObjectProximity>();
        }

        public void InitialiseTreeProximity(string methodName)
        {
            // Set proximityTreeCellsListWidth dynamically based on landscape width and 2x the maximum tree proximity
            // Populate proximity lists
            proximityTreeCellsListWidth = (int)(landscapeWidth * 0.5f / LBGroup.GetMaxMemberTreeProximity(activeGroupList));
            if (proximityTreeCellsListWidth > 100) { proximityTreeCellsListWidth = 100; }
            if (proximityTreeCellsListWidth < 1) { proximityTreeCellsListWidth = 1; }

            totalProximityTreeCells = proximityTreeCellsListWidth * proximityTreeCellsListWidth;
            treeProximitiesList = new List<LBObjectProximity>[totalProximityTreeCells];
            for (int i = 0; i < totalProximityTreeCells; i++) { treeProximitiesList[i] = new List<LBObjectProximity>(); }
            // Initialise proximity variables
            thisTreeProximityCellIndex = 0;
            proximityTreeCellXCoord = 0;
            proximityTreeCellZCoord = 0;
            // Calculate the size in metres of each cell
            proximityTreeCellSize = lbGroupParams.landscape.size.x / proximityTreeCellsListWidth;
            proximityTreeCellMinXCoord = 0;
            proximityTreeCellMaxXCoord = 0;
            proximityTreeCellMinZCoord = 0;
            proximityTreeCellMaxZCoord = 0;
            proximityTreeCellBlockHalfWidth = 0;
            thisTreeProximitiesList = new List<LBObjectProximity>();
            // Populate tree positions
            for (int i = 0; i < numTerrains; i++)
            {
                // Get and initialise terrain info
                Terrain tTerrain = landscapeTerrains[i];
                if (tTerrain == null) { Debug.LogWarning("ERROR " + methodName + " - terrain " + i.ToString() + " is invalid or could not be found. Please Report."); }
                else
                {
                    TerrainData tTerrainData = tTerrain.terrainData;
                    if (tTerrainData == null) { Debug.LogWarning("ERROR " + methodName + " - some terrain data is invalid or could not be found. Please Report."); }
                    else
                    {
                        // Get the position of the terrain relative to the landscape
                        Vector3 tTerrainPos = tTerrain.transform.position;
                        // Get the trees array
                        TreeInstance[] terrainTreesArray = tTerrainData.treeInstances;
                        int terrainTreesArrayLength = terrainTreesArray.Length;
                        // Initialise tree variables
                        Vector3 tTreeWorldPos = Vector3.zero;
                        Vector3 tTreeLandscapePos = Vector3.zero;
                        Vector2 tTreeNormalisedPos = Vector2.zero;
                        TreeInstance terrainTreeInstance;
                        for (int t = 0; t < terrainTreesArrayLength; t++)
                        {
                            terrainTreeInstance = terrainTreesArray[t];
                            // Get landscape position of the tree
                            tTreeWorldPos = Vector3.Scale(terrainTreeInstance.position, tTerrainData.size) + tTerrainPos;
                            tTreeLandscapePos = tTreeWorldPos - landscapePosition;
                            // Get normalised position of the tree
                            tTreeNormalisedPos.x = tTreeLandscapePos.x / landscapeWidth;
                            tTreeNormalisedPos.y = tTreeLandscapePos.z / landscapeLength;
                            // Find which cell we are in
                            proximityTreeCellXCoord = (int)(tTreeNormalisedPos.x * proximityTreeCellsListWidth);
                            proximityTreeCellZCoord = (int)(tTreeNormalisedPos.y * proximityTreeCellsListWidth);
                            if (proximityTreeCellXCoord >= proximityTreeCellsListWidth) { proximityTreeCellXCoord = proximityTreeCellsListWidth - 1; }
                            if (proximityTreeCellZCoord >= proximityTreeCellsListWidth) { proximityTreeCellZCoord = proximityTreeCellsListWidth - 1; }
                            thisTreeProximityCellIndex = proximityTreeCellXCoord + (proximityTreeCellZCoord * proximityTreeCellsListWidth);
                            if (thisTreeProximityCellIndex < 0) { thisTreeProximityCellIndex = 0; }
                            else if (thisTreeProximityCellIndex >= totalProximityTreeCells) { thisTreeProximityCellIndex = totalProximityTreeCells - 1; }
                            // Add this tree position to the cell
                            treeProximitiesList[thisTreeProximityCellIndex].Add(new LBObjectProximity(tTreeLandscapePos, 0f, (short)i, t));
                        }
                    }
                }
            }
            // Initialise variables for removing trees
            treesToRemoveList = new List<LBObjectProximity>();
            thisTreesToRemoveList = new List<LBObjectProximity>();
        }

        public void InitialiseGrassArrays()
        {
            isRemoveGrassPresent = LBGroup.IsRemoveGrassPresent(activeGroupList);
            isGrassPopulationPresent = LBGroup.IsPopulateGrassPresent(activeGroupList);

            // A grass array needs to be size of the sum of all grass arrays in each terrain
            terrainDetailResolution = lbGroupParams.landscape.GetLandscapeTerrainDetailResolution();
            grassArrayCellsListWidth = numTerrainsWide * terrainDetailResolution;

            totalGrassArrayCells = grassArrayCellsListWidth * grassArrayCellsListWidth;
            // Grass removal array: 255 = previous grass strengh, 0 = all grass removed 
            initialGrassRemovalArray = new byte[totalGrassArrayCells];
            finalGrassRemovalArray = new byte[totalGrassArrayCells];
            // Find out how many grass addition arrays we need, then pack them all into a single array
            grassAdditionArraySize = lbGroupParams.landscape.GetNumTerrainGrassList * totalGrassArrayCells;
            grassAdditionArray = new byte[grassAdditionArraySize];
            // Set defaults for each array
            for (int i = 0; i < totalGrassArrayCells; i++) { initialGrassRemovalArray[i] = 255; finalGrassRemovalArray[i] = 255; }
            for (int i = 0; i < grassAdditionArraySize; i++) { grassAdditionArray[i] = 0; }
            // Calculate the size in metres of each cell
            grassArrayCellSize = lbGroupParams.landscape.size.x / grassArrayCellsListWidth;
            // Initialise grass array variables
            thisGrassArrayCellIndex = 0;
            grassArrayCellXCoord = 0;
            grassArrayCellZCoord = 0;
            grassAreaRadius = 0;
            grassAreaNoBlendRadius = 0f;
            grassAreaCentreXIndex = 0;
            grassAreaCentreZIndex = 0;
            grassAreaMinXIndex = 0;
            grassAreaMaxXIndex = 0;
            grassAreaMinZIndex = 0;
            grassAreaMaxZIndex = 0;
            grassAdditionArrayIndexShift = 0;
            // Initialise grass removal variables
            grassRemovalBlendFactor = 0f;
            grassRemovalDist = 0f;
            // Initialise grass population variables
            grassPlacementDist = 0f;
            grassPopulationBlendFactor = 0f;
            grassNoiseValue = 0f;

            terrainGrassList = lbGroupParams.landscape.TerrainGrassList();
            terrainGrassListSize = terrainGrassList == null ? 0 : terrainGrassList.Count;
            grsIdx = 0;

            // The Group-level Grass option tab contains GUIDs that reference LBTerrainGrass instances in the
            // landscape class terrainGrassList. These are LBTerrainGrass instances that have been applied
            // to the landscape. Some LBTerrainGrass instances may be disabled or don't have a valid grass texture.
            // This list contains the matching index of the Grass Tab's LBTerrainGrass in the
            // array of unique detailPrototypes for all terrains.
            terrainGrassArrayIndexList = new List<int>(new int[terrainGrassListSize]);
        }

        public void InitialiseTextureArrays()
        {
            isTexturingPresent = LBGroup.IsApplyTexturesPresent(activeGroupList);
            isObjPathTexturingPresent = LBGroup.IsApplyObjPathTexturesPresent(activeGroupList) && !isGroupDesignerEnabled;

            // A texture array needs to be size of the sum of all alphamap arrays in each terrain
            terrainAlphamapResolution = lbGroupParams.landscape.GetLandscapeTerrainAlphaMapResolution();
            textureArrayCellsListWidth = numTerrainsWide * terrainAlphamapResolution;

            totalTextureArrayCells = textureArrayCellsListWidth * textureArrayCellsListWidth;
            // Texture array: 255 = add highest strength, 0 = add nothing
            // Find out how many texture arrays we need, then pack them all into a single array
            // NOTE: This may cause issues for Imported Terrains with different textures in each terrain.
            totalTextureAdditionArrays = lbGroupParams.landscape.GetNumActiveTerrainTextures(true);
            textureAdditionArraySize = totalTextureAdditionArrays * totalTextureArrayCells;
            textureAdditionArray = new byte[textureAdditionArraySize];
            // Set defaults for each array
            for (int i = 0; i < textureAdditionArraySize; i++) { textureAdditionArray[i] = 0; }
            // Calculate the size in metres of each cell
            textureArrayCellSize = lbGroupParams.landscape.size.x / textureArrayCellsListWidth;
            // Initialise texture array variables
            thisTextureArrayCellIndex = 0;
            textureArrayCellXCoord = 0;
            textureArrayCellZCoord = 0;
            textureAreaRadius = 0;
            textureAreaNoBlendRadius = 0f;
            textureAreaCentreXIndex = 0;
            textureAreaCentreZIndex = 0;
            textureAreaMinXIndex = 0;
            textureAreaMaxXIndex = 0;
            textureAreaMinZIndex = 0;
            textureAreaMaxZIndex = 0;
            textureAdditionArrayIndexShift = 0;
            // Initialise texture placement variables
            texturePlacementDist = 0f;
            textureBlendFactor = 0f;

            terrainTextureList = lbGroupParams.landscape.TerrainTexturesList();
            terrainTextureListSize = terrainTextureList == null ? 0 : terrainTextureList.Count;
            txIdx = 0;

            // The Group-level Tex option tab contains GUIDs that reference LBTerrainTexture instances in the
            // landscape class terrainTexturesList. These are LBTerrainTexture instances that have been applied
            // to the landscape. Some LBTerrainTexture instances may be disabled or don't have a valid Texture2D.
            // This list contains the matching index of the Texturing Tab's LBTerrainTexture in the
            // array of unique splatPrototypes for all terrains.
            terrainTextureArrayIndexList = new List<int>(new int[terrainTextureListSize]);

            // Fill the lookup array with -ve numbers to show there is no matching splatprototype to begin with
            for (int txArrayIdx = 0; txArrayIdx < terrainTextureListSize; txArrayIdx++) { terrainTextureArrayIndexList[txArrayIdx] = -1; }

            if ((isTexturingPresent || isObjPathTexturingPresent) && terrainTextureList != null)
            {
                // Get a list of unique textures from the terrain data
                for (int t = 0; t < numTerrains; t++)
                {
                    #if UNITY_2018_3_OR_NEWER
                    List<LBTerrainTexture> splatTextures = LBTerrainTexture.ToLBTerrainTextureList(terrainDataArray[t].terrainLayers);
                    #else
                    List<LBTerrainTexture> splatTextures = LBTerrainTexture.ToLBTerrainTextureList(terrainDataArray[t].splatPrototypes);
                    #endif

                    if (splatTextures != null)
                    {
                        for (txIdx = 0; txIdx < terrainTextureListSize; txIdx++)
                        {
                            // Skip textures that have already been matched
                            if (terrainTextureArrayIndexList[txIdx] < 0)
                            {
                                LBTerrainTexture lbTerrainTexture = terrainTextureList[txIdx];

                                // Attempt to match this Texture with a splatPrototype
                                // Check for tinted textures
                                //int splatIdx = splatTextures.FindIndex(stx => stx.texture == (lbTerrainTexture.isTinted ? lbTerrainTexture.tintedTexture : lbTerrainTexture.texture) &&
                                int splatIdx = splatTextures.FindIndex(stx => lbTerrainTexture.CompareToTexture2D(stx.texture) &&
                                                                        stx.normalMap == lbTerrainTexture.normalMap &&
                                                                        stx.smoothness == lbTerrainTexture.smoothness &&
                                                                        stx.metallic == lbTerrainTexture.metallic &&
                                                                        stx.tileSize == lbTerrainTexture.tileSize);

                                if (splatIdx >= 0)
                                {
                                    // Add this splatPrototype index to the array of LBTerrainTexture indexes
                                    // This will enable us to tell which splatPrototype (if any) matches the current LBTerrainTexture
                                    terrainTextureArrayIndexList[txIdx] = splatIdx;
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Set up a mesh controller object for this landscape
        /// </summary>
        /// <param name="methodName"></param>
        public void AddMeshController(string methodName)
        {
            // Initialise variables
            GameObject landscapeMeshControllerObj = null;
            terrainMeshController = null;

            if (!isGroupDesignerEnabled)
            {
                landscapeMeshControllerObj = new GameObject("Terrain Mesh Controller");
                if (landscapeMeshControllerObj == null) { if (lbGroupParams.showErrors) { Debug.LogWarning("ERROR: " + methodName + " could not create terrain mesh controller gameobject for " + landscapeName + " Please Report."); } }
                else
                {
                    landscapeMeshControllerObj.transform.parent = lbGroupParams.landscape.transform;
                    terrainMeshController = landscapeMeshControllerObj.AddComponent<LBTerrainMeshController>();
                    if (terrainMeshController == null) { if (lbGroupParams.showErrors) { Debug.LogWarning("ERROR: " + methodName + " could not add terrain mesh controller for " + landscapeName + " Please Report."); } }
                    else { terrainMeshController.Initialise(); }
                }
            }
        }

        /// <summary>
        /// Preload the stencils for ALL groups
        /// </summary>
        public void PreloadStencilData()
        {
            isStencilLayerFiltersToApply = new bool[numActiveGroups];

            for (int i = 0; i < numActiveGroups; i++)
            {
                // Are there any Stencil Layer filters for this group?
                isStencilLayerFiltersToApply[i] = LBFilter.Contains(activeGroupList[i].filterList, LBFilter.FilterType.StencilLayer);
                if (isStencilLayerFiltersToApply[i])
                {
                    // Preload the stencil data
                    if (activeGroupList[i].filterList != null)
                    {
                        foreach (LBFilter lbFilter in activeGroupList[i].filterList)
                        {
                            if (lbFilter != null)
                            {
                                // Currently all stencil filters are AND
                                // Is this a valid Stencil Filter?
                                if (lbFilter.filterType == LBFilter.FilterType.StencilLayer && !string.IsNullOrEmpty(lbFilter.lbStencilGUID) && !string.IsNullOrEmpty(lbFilter.lbStencilLayerGUID))
                                {
                                    // If the temporary class instance isn't defined, look it up and validate it is in the current landscape
                                    if (lbFilter.lbStencil == null)
                                    {
                                        lbFilter.lbStencil = LBStencil.GetStencilInLandscape(lbGroupParams.landscape, lbFilter.lbStencilGUID, lbGroupParams.showErrors);
                                    }

                                    if (lbFilter.lbStencil != null)
                                    {
                                        // Find the Stencil Layer for this Layer Filter and populate the temporary class instance
                                        lbFilter.lbStencilLayer = lbFilter.lbStencil.GetStencilLayerByGUID(lbFilter.lbStencilLayerGUID);

                                        // Load the USHORT data
                                        if (lbFilter.lbStencilLayer != null)
                                        {
                                            if (lbFilter.lbStencilLayer.layerArray == null)
                                            {
                                                lbFilter.lbStencilLayer.AllocLayerArray();
                                                lbFilter.lbStencilLayer.UnCompressToUShort();
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Stencil LBLayerFilter temp variables
            stencilLayerPosN = new Vector2();
        }

        public void PrefetchVSProSettings(string methodName)
        {
            #if VEGETATION_STUDIO_PRO
            vsPro = null;
            if (lbGroupParams.landscape.useVegetationSystem)
            {
                vsPro = LBIntegration.GetVegetationSystemPro();
                if (vsPro == null && lbGroupParams.showErrors) { Debug.LogWarning("ERROR " + methodName + " - could not find the VegetationSystemPro component in the scene."); }
            }
            #endif
        }

        public void SetPart2Variables()
        {

        }

        #endregion
    }
}