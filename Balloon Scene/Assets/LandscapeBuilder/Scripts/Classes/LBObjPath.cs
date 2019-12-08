// Landscape Builder. Copyright (c) 2016-2019 SCSM Pty Ltd. All rights reserved.
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace LandscapeBuilder
{
    /// <summary>
    /// LBObjPath is a superset of LBPath and is used to store data
    /// and modify a path in the scene which objects (prefabs) are
    /// placed along according to a set of user-defined rules.
    /// NOTE: The 3D position in scene or LBGroup is stored in LBPath.positionList
    /// The number of pathPoints in pathPointList must match the number in LBPath.positionList.
    /// </summary>
    [System.Serializable]
    public class LBObjPath : LBPath
    {
        #region Enumerations

        /// <summary>
        /// Spacing: evenly space prefabs
        /// QtyPer100m: how many per 100m on path
        /// </summary>
        public enum LayoutMethod
        {
            Spacing = 0,
            ExactQty = 2,
            QtyPer100m = 5
        }

        /// <summary>
        /// Determines which algorithm to use when
        /// selecting an object from the mainObjPrefabList
        /// in the default series or an LBObjPathSeries.
        /// RandomUnique gives the first random object.
        /// </summary>
        public enum SelectionMethod
        {
            Alternating = 0,
            Random = 10,
            RandomLessRepeats = 11,
            RandomUnique = 14
        }

        public enum LBObjectOrientation
        {
            PathSpace = 0,
            DefaultSpace = 10
        }

        public enum PathSpline
        {
            Centre = 0,
            LeftEdge = 1,
            RightEdge = 2
        }

        #endregion

        #region Public variables
        // IMPORTANT When changing this section update:
        // SetClassDefaults() and LBObjPath(LBObjPath lbObjPath) clone constructor

        // NOTE: The number of pathPoints in pathPointList must match the number in LBPath.positionList.
        public List<LBPathPoint> pathPointList;

        // Prefab variables - start/main/end (start and end prefabs are optional)
        public List<LBObjPrefab> mainObjPrefabList;

        // Optional start prefab to be placed at the start of the path
        public LBObjPrefab startObjPrefab;

        // Optional end prefab to be placed at the start of the path
        public LBObjPrefab endObjPrefab;

        public LayoutMethod layoutMethod;
        public SelectionMethod selectionMethod;

        // Default LBPathPoint settings - can be overridden for each point
        public float spacingDistance;           // Used with LayoutMethod.Spacing for optimum distance apart to place objects on path
        public int maxMainPrefabs = 5;          // The max number of main prefabs to place along the path
        public bool isLastObjSnappedToEnd;      // If the prefabs don't evenly fit along the path, ensure the last object is placed on the end of the path
        public bool isRandomisePerGroupRegion;  // // Use a different random seed per Group region (instance) placement

        // A list of series which describe how objects will be placed along the path
        public List<LBObjPathSeries> lbObjPathSeriesList;
        public bool isSeriesListOverride;
        public string seriesListGroupMemberGUID;

        // Show/Hide editor variables
        public bool showSceneViewSettings;
        public bool showObjectSettings;
        public bool showDefaultSeriesInEditor;
        public bool showSeriesListInEditor;
        [System.NonSerialized] public bool showTargetSpacingInEditor;

        // Show scene items
        public bool showSurroundingInScene;     // Show the extent to which the path is blended with the surrounding topography

        // Path has width
        public bool useWidth;
        public AnimationCurve surroundBlendCurve;   // Used to blend the flattened path with the surrounding topography
        public AnimationCurve profileHeightCurve;   // The height profile or cross-section of the heights from left to right edge on the flattened path
        public static AnimationCurve GetDefaultProfileHeightCurve { get { return LBCurve.SetCurveFromPreset(LBCurve.ObjPathHeightCurvePreset.Flat); } }
        public LBCurve.ObjPathHeightCurvePreset profileHeightCurvePreset;
        public float surroundSmoothing;
        public float addTerrainHeight;              // The heightscale to add to the terrain when LBGroupMember.isTerrainFlattened is enabled

        // Object Path Surface Mesh
        public bool useSurfaceMesh;
        public Material surfaceMeshMaterial;
        public bool isCreateSurfaceMeshCollider;
        public bool isSwitchMeshUVs;                // Switch the direction of the UVs

        // Object Path Base Mesh(es)
        public float baseMeshThickness;             // A base mesh that sits below the surface mesh. 0 = no base mesh
        public List<LBMesh> baseMeshList;           // A list of meshes that make up the base of the path
        public Material baseMeshMaterial;
        public Vector2 baseMeshUVTileScale;         // The tiling of the UVs when the base mesh is created.
        public bool isSwitchBaseMeshUVs;            // Switch the direction of the UVs
        public bool baseMeshUseIndent;              // Use the surface mesh indent. This makes the base the same width as the surface mesh rather than the path width
        public bool isCreateBaseMeshCollider;

        // Terrain texturing
        public string coreTextureGUID;
        public float coreTextureNoiseTileSize;      // A value of 0.0, means do not use noise
        public float coreTextureStrength;
        public string surroundTextureGUID;
        public float surroundTextureNoiseTileSize;  // A value of 0.0, means do not use noise
        public float surroundTextureStrength;

        public bool isRemoveExistingGrass;      // Currently only supported by Vegetation Studio Pro Vegetation Mask Areas
        public bool isRemoveExistingTrees;
        public float treeDistFromEdge;          // The distance from left/right path edges the trees should be removed. Max is the surround distance

        // Biomes - currently only used for Vegetation Studio Pro
        public bool useBiomes;
        public List<LBBiome> lbBiomeList;

        // Min Path Width used in editor to add or set all points
        [System.NonSerialized] public float minPathWidth;
        [System.NonSerialized] public float addPathWidth;

        // Used to add width or set the width of all points in the path 
        [System.NonSerialized] public bool isGetPointWidthMode;

        // A mode used in the Editor to enable importing of points from
        // an external source. e.g. LBMapPath in the scene
        [System.NonSerialized] public bool isGetPointsMode;

        // Used with RefreshObjPathPositions() and LBObjPathDesigner.DrawPath(..).
        [System.NonSerialized] public List<Vector3> cachedSplinePointLeftEdgeList;
        [System.NonSerialized] public List<Vector3> cachedSplinePointRightEdgeList;

        // Used with RefreshObjPathPositions(), LBObjPathDesigner.DrawPath(..), Group ObjPath Flatten LBLandscapeTerrain.PopulateLandscapeWithGroups(..).
        // Object Paths use a separate set of cached spline points that are outside the cachedSplinePointLeft/RightEdgeList points.
        // Unlike a LBMapPath, the edge distance is outside the left/right path points (which is why we have a separate set of surround spline points)
        [System.NonSerialized] public List<Vector3> cachedSplinePointLeftSurroundList;
        [System.NonSerialized] public List<Vector3> cachedSplinePointRightSurroundList;

        // Used with RefreshObjPathPositions(), Group ObjPath surfaceMesh LBLandscapeTerrain.PopulateLandscapeWithGroups(..).
        // Left and Right inner boarder splines based on the LBPath.leftBorderWidth and rightBorderWidth.
        [System.NonSerialized] public List<Vector3> cachedSplinePointLeftBorderList;
        [System.NonSerialized] public List<Vector3> cachedSplinePointRightBorderList;

        // There is no final left/right points for the surround list when the pathlength is not exactly divisible by the path resolution
        //[System.NonSerialized] public Vector3 cachedSplinePointEndLeftSurround;
        //[System.NonSerialized] public Vector3 cachedSplinePointEndRightSurround;

        #endregion

        #region Private variables

        #endregion

        #region Constructors

        // Standard constructor - calls default LBPath() constructor first
        public LBObjPath() : base()
        {
            SetClassDefaults();            
        }

        // Clone constructor
        public LBObjPath(LBObjPath lbObjPath)
        {
            if (lbObjPath == null) { SetClassDefaults(); }
            else
            {
                // Base clone
                pathName = lbObjPath.pathName;
                pathType = lbObjPath.pathType;
                closedCircuit = lbObjPath.closedCircuit;
                showPathInScene = lbObjPath.showPathInScene;
                showDistancesInScene = lbObjPath.showDistancesInScene;
                showPointLabelsInScene = lbObjPath.showPointLabelsInScene;
                showSurroundingInScene = lbObjPath.showSurroundingInScene;
                useWidth = lbObjPath.useWidth;
                useSurfaceMesh = lbObjPath.useSurfaceMesh;
                if (lbObjPath.surfaceMeshMaterial != null) { surfaceMeshMaterial = new Material(lbObjPath.surfaceMeshMaterial); } else { surfaceMeshMaterial = null; }
                isCreateSurfaceMeshCollider = lbObjPath.isCreateSurfaceMeshCollider;
                if (lbObjPath.surroundBlendCurve == null) { surroundBlendCurve = GetDefaultBlendCurve; }
                else { surroundBlendCurve = new AnimationCurve(lbObjPath.surroundBlendCurve.keys); }
                if (lbObjPath.profileHeightCurve == null) { surroundBlendCurve = GetDefaultProfileHeightCurve; }
                else { profileHeightCurve = new AnimationCurve(lbObjPath.profileHeightCurve.keys); }
                profileHeightCurvePreset = lbObjPath.profileHeightCurvePreset;
                surroundSmoothing = lbObjPath.surroundSmoothing;
                addTerrainHeight = lbObjPath.addTerrainHeight;

                isSwitchMeshUVs = lbObjPath.isSwitchMeshUVs;
                isSwitchBaseMeshUVs = lbObjPath.isSwitchBaseMeshUVs;
                baseMeshThickness = lbObjPath.baseMeshThickness;
                if (lbObjPath.baseMeshList != null) { baseMeshList = lbObjPath.baseMeshList.ConvertAll(msh => new LBMesh(msh)); } else { baseMeshList = null; }
                if (lbObjPath.baseMeshMaterial != null) { baseMeshMaterial = new Material(lbObjPath.baseMeshMaterial); } else { baseMeshMaterial = null; }
                baseMeshUVTileScale = lbObjPath.baseMeshUVTileScale;
                baseMeshUseIndent = lbObjPath.baseMeshUseIndent;
                isCreateBaseMeshCollider = lbObjPath.isCreateBaseMeshCollider;

                coreTextureGUID = lbObjPath.coreTextureGUID;
                coreTextureStrength = lbObjPath.coreTextureStrength;
                coreTextureNoiseTileSize = lbObjPath.coreTextureNoiseTileSize;
                surroundTextureGUID = lbObjPath.surroundTextureGUID;
                surroundTextureStrength = lbObjPath.surroundTextureStrength;
                surroundTextureNoiseTileSize = lbObjPath.surroundTextureNoiseTileSize;
                isRemoveExistingGrass = lbObjPath.isRemoveExistingGrass;
                isRemoveExistingTrees = lbObjPath.isRemoveExistingTrees;
                treeDistFromEdge = lbObjPath.treeDistFromEdge;
                pathLength = lbObjPath.pathLength;
                pathLengthToLastPoint = lbObjPath.pathLengthToLastPoint;
                splineLength = lbObjPath.splineLength;
                splineLengthToLastPoint = lbObjPath.splineLengthToLastPoint;
                splineDistTo2ndLastPoint = lbObjPath.splineDistTo2ndLastPoint;
                findZoomDistance = lbObjPath.findZoomDistance;
                zoomOnFind = lbObjPath.zoomOnFind;
                snapToTerrain = lbObjPath.snapToTerrain;
                heightAboveTerrain = lbObjPath.heightAboveTerrain;
                pathResolution = lbObjPath.pathResolution;
                edgeBlendWidth = lbObjPath.edgeBlendWidth;
                blendStart = lbObjPath.blendStart;
                blendEnd = lbObjPath.blendEnd;
                removeCentre = lbObjPath.removeCentre;
                leftBorderWidth = lbObjPath.leftBorderWidth;
                rightBorderWidth = lbObjPath.rightBorderWidth;
                isMeshLandscapeUV = lbObjPath.isMeshLandscapeUV;
                meshUVTileScale = lbObjPath.meshUVTileScale;
                meshYOffset = lbObjPath.meshYOffset;
                meshEdgeSnapToTerrain = lbObjPath.meshEdgeSnapToTerrain;
                meshIncludeEdges = lbObjPath.meshIncludeEdges;
                meshIncludeWater = lbObjPath.meshIncludeWater;
                meshSnapType = lbObjPath.meshSnapType;
                meshIsDoubleSided = lbObjPath.meshIsDoubleSided;
                meshSaveToProjectFolder = lbObjPath.meshSaveToProjectFolder;
                if (lbObjPath.meshTempMaterial != null) { meshTempMaterial = new Material(lbObjPath.meshTempMaterial); } else { meshTempMaterial = null; }
                showAdvancedOptions = lbObjPath.showAdvancedOptions;
                curveDetectionInner = lbObjPath.curveDetectionInner;
                curveDetectionOuter = lbObjPath.curveDetectionOuter;

                this.positionList = new List<Vector3>(lbObjPath.positionList);
                this.positionListLeftEdge = new List<Vector3>(lbObjPath.positionListLeftEdge);
                this.positionListRightEdge = new List<Vector3>(lbObjPath.positionListRightEdge);
                this.rotationList = new List<Vector3>(lbObjPath.rotationList);
                this.widthList = new List<float>(lbObjPath.widthList);
                this.selectedList = new List<int>(lbObjPath.selectedList);

                if (lbObjPath.lbMesh != null) { lbMesh = new LBMesh(lbObjPath.lbMesh); } else { lbMesh = null; }
                if (lbObjPath.lbWater != null) { lbWater = new LBWater(lbObjPath.lbWater, true); } else { lbWater = null; }

                // Clone LBObjPath specific fields - use ConvertAll to do a deep copy
                if (lbObjPath.pathPointList != null) { pathPointList = lbObjPath.pathPointList.ConvertAll(pp => new LBPathPoint(pp)); } else { pathPointList = new List<LBPathPoint>(); }

                showSceneViewSettings = lbObjPath.showSceneViewSettings;
                showObjectSettings = lbObjPath.showObjectSettings;
                showDefaultSeriesInEditor = lbObjPath.showDefaultSeriesInEditor;
                showSeriesListInEditor = lbObjPath.showSeriesListInEditor;

                // Prefab varibles
                if (lbObjPath.mainObjPrefabList != null) { mainObjPrefabList = lbObjPath.mainObjPrefabList.ConvertAll(mp => new LBObjPrefab(mp)); } else { mainObjPrefabList = new List<LBObjPrefab>(); }
                if (lbObjPath.startObjPrefab != null) { startObjPrefab = new LBObjPrefab(lbObjPath.startObjPrefab); } else { startObjPrefab = null; }
                if (lbObjPath.endObjPrefab != null) { endObjPrefab = new LBObjPrefab(lbObjPath.endObjPrefab); } else { endObjPrefab = null; }

                layoutMethod = lbObjPath.layoutMethod;
                selectionMethod = lbObjPath.selectionMethod;
                isLastObjSnappedToEnd = lbObjPath.isLastObjSnappedToEnd;
                spacingDistance = lbObjPath.spacingDistance;
                maxMainPrefabs = lbObjPath.maxMainPrefabs;
                isRandomisePerGroupRegion = lbObjPath.isRandomisePerGroupRegion;

                // Series
                if (lbObjPath.lbObjPathSeriesList != null) { lbObjPathSeriesList = lbObjPath.lbObjPathSeriesList.ConvertAll(ops => new LBObjPathSeries(ops)); } else { lbObjPathSeriesList = new List<LBObjPathSeries>(); }
                isSeriesListOverride = lbObjPath.isSeriesListOverride;
                seriesListGroupMemberGUID = lbObjPath.seriesListGroupMemberGUID;

                // Biomes
                useBiomes = lbObjPath.useBiomes;
                if (lbObjPath.lbBiomeList != null) { lbBiomeList = lbObjPath.lbBiomeList.ConvertAll(bme => new LBBiome(bme)); } else { lbBiomeList = new List<LBBiome>(); }
            }
        }

        #endregion

        #region Public Member Methods

        /// <summary>
        /// Assign new GUIDs to all the points in the path
        /// </summary>
        public void ResetPathPointGUIDs()
        {
            int numPathPoints = pathPointList == null ? 0 : pathPointList.Count;

            for (int ppIdx = 0; ppIdx < numPathPoints; ppIdx++)
            {
                pathPointList[ppIdx].GUID = System.Guid.NewGuid().ToString();
            }
        }

        /// <summary>
        /// Reverse the direction of the path points.
        /// NOTE: Does not consider cached points
        /// </summary>
        public void Reverse(bool includeSurround)
        {
            if (pathPointList != null)
            {
                pathPointList.Reverse();
            }

            int numPositions = (positionList == null ? 0 : positionList.Count);

            if (numPositions > 0)
            {
                positionList.Reverse();

                if (useWidth && widthList != null)
                {
                    widthList.Reverse();

                    // To be certain, refresh everything
                    isSplinesCached2 = false;
                    RefreshObjPathPositions(includeSurround, false);
                }
            }

            if (selectedList != null && selectedList.Count > 1) { selectedList.Reverse(); }
        }

        /// <summary>
        /// ObjPaths can potentially have multiple offset positions for each user-created spline points.
        /// positionListLeftEdge and RightEdge are permanently stored in LBPath.
        /// Spline left and right edge points are cached in LBObjPath and are derived from the cached centre spline points.
        /// Can also be used to refresh and cache centre spline and distances.
        /// If includeSurround = true, refresh boundary splines of blended surroundings
        /// NOTE: To force LPPath cache to be updated, set isSplinesCached2 = false before calling this method.
        /// </summary>
        /// <param name="includeSurround"></param>
        /// <param name="includeBorders"></param>
        public void RefreshObjPathPositions(bool includeSurround, bool includeBorders)
        {
            if (!isRefreshing && positionList != null)
            {
                isRefreshing = true;
                int numPositionPoints = (positionList == null ? 0 : positionList.Count);

                // Note: cache is only updated if isSplineCached2 is false
                CacheSplinePoints2();

                // Update the cached left and right spline points
                if (useWidth && positionListLeftEdge != null && positionListRightEdge != null)
                {
                    float halfWidth = 0f;
                    Vector3 splinePoint = Vector3.zero;

                    int numSplinePoints = (cachedCentreSplinePointList == null ? 0 : cachedCentreSplinePointList.Count);

                    // Clear the left/right edge spline point cache
                    // Re-use cached lists to minimise memory fragmentation caused by creating and destroying small list
                    // If the list hasn't been initialised yet, create it and give it some capacity
                    if (cachedSplinePointLeftEdgeList == null) { cachedSplinePointLeftEdgeList = new List<Vector3>(numSplinePoints); }
                    else { cachedSplinePointLeftEdgeList.Clear(); }

                    if (cachedSplinePointRightEdgeList == null) { cachedSplinePointRightEdgeList = new List<Vector3>(numSplinePoints); }
                    else { cachedSplinePointRightEdgeList.Clear(); }

                    // Clear Surround Lists. Only create if we need to refresh them
                    if (cachedSplinePointLeftSurroundList == null) { if (includeSurround) { cachedSplinePointLeftSurroundList = new List<Vector3>(numSplinePoints); } }
                    else { cachedSplinePointLeftSurroundList.Clear(); }

                    if (cachedSplinePointRightSurroundList == null) { if (includeSurround) { cachedSplinePointRightSurroundList = new List<Vector3>(numSplinePoints); } }
                    else { cachedSplinePointRightSurroundList.Clear(); }

                    // Clear Border Lists. Only create if we need to refresh them
                    if (cachedSplinePointLeftBorderList == null) { if (includeBorders) { cachedSplinePointLeftBorderList = new List<Vector3>(numSplinePoints); } }
                    else { cachedSplinePointLeftBorderList.Clear(); }

                    if (cachedSplinePointRightBorderList == null) { if (includeBorders) { cachedSplinePointRightBorderList = new List<Vector3>(numSplinePoints); } }
                    else { cachedSplinePointRightBorderList.Clear(); }

                    // Update left/right path positions (based on path positions created by the user)
                    for (int i = 0; i < numPositionPoints; i++)
                    {
                        //Debug.Log("[DEBUG] numPositionPoints: " + numPositionPoints + " i: " + i + " widthlist: " + widthList.Count + " positionListRightEdge: " + positionListRightEdge.Count);

                        // Add the outermost left and right edge points
                        positionListRightEdge[i] = GetPathOffsetPosition(i, widthList[i] / 2f, pathPointList[i].rotationZ);
                        positionListLeftEdge[i] = GetPathOffsetPosition(i, widthList[i] / -2f, pathPointList[i].rotationZ);
                    }

                    Vector3 centreSplinePoint = Vector3.zero, forwardsDir = Vector3.zero;

                    // Spline points are closer together than user-placed path positions. They are path resolution distance apart along spline.
                    // Width is variable.
                    for (int i = 0; i < numSplinePoints; i++)
                    {
                        // Get the interpolated width at this point along the spline
                        halfWidth = GetWidthOnSpline(i, widthList) / 2f;

                        // TODO Get the interpolated rotation at this point along the spline


                        // Get the point on the centre spline once
                        GetPathPositionForwards(cachedCentreSplinePointDistancesList[i], 1, ref centreSplinePoint, ref forwardsDir);

                        splinePoint = GetPathOffsetPosition(centreSplinePoint, forwardsDir, halfWidth, 0f);

                        // The edges should have the same Y as the centre point (if no rotation)
                        splinePoint.y = cachedCentreSplinePointList[i].y;

                        cachedSplinePointRightEdgeList.Add(splinePoint);

                        splinePoint = GetPathOffsetPosition(centreSplinePoint, forwardsDir, -halfWidth, 0f);

                        // The edges should have the same Y as the centre point (if no rotation)
                        splinePoint.y = cachedCentreSplinePointList[i].y;

                        cachedSplinePointLeftEdgeList.Add(splinePoint);

                        if (includeSurround)
                        {
                            splinePoint = GetPathOffsetPosition(centreSplinePoint, forwardsDir, halfWidth + edgeBlendWidth, 0f);
                            splinePoint.y = cachedCentreSplinePointList[i].y;
                            cachedSplinePointRightSurroundList.Add(splinePoint);

                            splinePoint = GetPathOffsetPosition(centreSplinePoint, forwardsDir, -(halfWidth + edgeBlendWidth), 0f);
                            splinePoint.y = cachedCentreSplinePointList[i].y;
                            cachedSplinePointLeftSurroundList.Add(splinePoint);
                        }

                        if (includeBorders)
                        {
                            splinePoint = GetPathOffsetPosition(centreSplinePoint, forwardsDir, halfWidth - rightBorderWidth, 0f);
                            splinePoint.y = cachedCentreSplinePointList[i].y;
                            cachedSplinePointRightBorderList.Add(splinePoint);

                            splinePoint = GetPathOffsetPosition(centreSplinePoint, forwardsDir, -(halfWidth - leftBorderWidth), 0f);
                            splinePoint.y = cachedCentreSplinePointList[i].y;
                            cachedSplinePointLeftBorderList.Add(splinePoint);
                        }
                    }
                }

                isRefreshing = false;
            }
        }

        /// <summary>
        /// Populate a supplied (empty but not null) list of points along the Spline with the new points
        /// spaced a given distance apart.
        /// Populate supplied (empty but not null) lists of forward and right vectors for each splinePointList item.
        /// Populate a supplied (empty but not null) list of cumulative distances for each splinePointList item.
        /// Prerequisite: CacheSplinePoints2(), RefreshObjPathPositions(true, lbObjPath.useSurfaceMesh);
        /// NOTE: lbObjPath.useWidth must be true.
        /// See also LBPath.GetPositionList(float newIntervalDistance, bool isLastObjSnappedToEnd, bool showErrors)
        /// </summary>
        /// <param name="splinePositionList"></param>
        /// <param name="splinePositionForwardsList"></param>
        /// <param name="splinePositionRightList"></param>
        /// <param name="splinePositionDistancesList"></param>
        /// <param name="lbObjPathSeries"></param>
        /// <param name="newIntervalDistance"></param>
        /// <param name="showErrors"></param>
        public void GetObjPathPositionList(List<Vector3> splinePositionList, List<Vector3> splinePositionForwardsList, List<Vector3> splinePositionRightList, List<float> splinePositionDistancesList, LBObjPathSeries lbObjPathSeries, float newIntervalDistance, bool showErrors)
        {
            if (useWidth && splinePositionList != null)
            {
                string methodName = "LBObjPath.GetObjPathPositionList";
                int numList = 0;

                // Validation
                if (lbObjPathSeries.pathSpline == PathSpline.Centre && cachedCentreSplinePointList == null) { if (showErrors) { Debug.LogWarning(methodName + " no cached centre points to process"); } }
                else if (lbObjPathSeries.pathSpline == PathSpline.LeftEdge && cachedSplinePointLeftEdgeList == null) { if (showErrors) { Debug.LogWarning(methodName + " no cached left edge points to process"); } }
                else if (lbObjPathSeries.pathSpline == PathSpline.RightEdge && cachedSplinePointRightEdgeList == null) { if (showErrors) { Debug.LogWarning(methodName + " no cached right edge points to process"); } }
                else if (newIntervalDistance <= 0f) { if (showErrors) { Debug.LogWarning(methodName + " newIntervalDistance must be greater than zero"); } }
                else if (splineLength <= 0f) { if (showErrors) { Debug.LogWarning(methodName + " the spline length is zero"); } }
                else
                {
                    List<Vector3> cachedSplinePointList = null;

                    switch (lbObjPathSeries.pathSpline)
                    {
                        case PathSpline.LeftEdge: cachedSplinePointList = cachedSplinePointLeftEdgeList; break;
                        case PathSpline.RightEdge: cachedSplinePointList = cachedSplinePointRightEdgeList; break;
                        default: cachedSplinePointList = cachedCentreSplinePointList; break;
                    }

                    int numSplinePoints = cachedSplinePointList == null ? 0 : cachedSplinePointList.Count;

                    if (numSplinePoints < 2) { if (showErrors) { Debug.LogWarning(methodName + " there must be at least 2 spline points to process"); } }
                    else
                    {
                        // Initialise variables, set default values
                        Vector3 lastSplinePoint = cachedSplinePointList[0], currentSplinePoint;
                        Vector3 splinePointForwards = cachedSplinePointList[1] - cachedSplinePointList[0];
                        Vector3 lastOffsetPoint = GetPathOffsetPosition(lastSplinePoint, splinePointForwards, lbObjPathSeries.useNonOffsetDistance ? 0f : lbObjPathSeries.prefabOffsetZ, 0f), currentOffsetPoint;
                        Vector3 thisPointFwd;
                        // All distances start at zero (start of path)
                        float lastOffsetPointDist = 0f, currentOffsetPointDist = 0f;
                        float lastObjectDistance = 0f;

                        if (lbObjPathSeries.prefabStartOffset == 0f)
                        {
                            // Add the first point if there is no start offset distance
                            splinePositionList.Add(lastOffsetPoint);

                            // Forwards for each new position point is stored in a list which is returned to the caller.
                            // This is used for Prefab Rotation and avoids having to calculate it again.
                            splinePositionForwardsList.Add(splinePointForwards);
                            // TODO: Tilt calculations
                            splinePositionRightList.Add(Vector3.Cross(Vector3.up, splinePointForwards));

                            splinePositionDistancesList.Add(lastObjectDistance);
                            // Offset second object position by start member offset
                            lastObjectDistance += lbObjPathSeries.startMemberOffset;
                        }

                        // Loop through spline points, starting with second point
                        for (int spIdx = 1; spIdx < numSplinePoints; spIdx++)
                        {
                            // Don't update forwards if this is the last point on the path
                            // When this is the case, forwards will be the same as the last point
                            if (spIdx < numSplinePoints - 1)
                            {
                                // Forwards for this point is direction from this spline point to the next one
                                splinePointForwards = cachedSplinePointList[spIdx + 1] - cachedSplinePointList[spIdx];                                
                            }

                            // Get current spline point
                            currentSplinePoint = cachedSplinePointList[spIdx];
                            // Get current offset point
                            currentOffsetPoint = GetPathOffsetPosition(currentSplinePoint, splinePointForwards, lbObjPathSeries.useNonOffsetDistance ? 0f : lbObjPathSeries.prefabOffsetZ, 0f);

                            // Caclulate distance from start of path to this point - simply add distance from the
                            // last offset point to this one
                            currentOffsetPointDist += lbObjPathSeries.use3DDistance ? Vector3.Distance(lastOffsetPoint, currentOffsetPoint) : Mathf.Sqrt(LBMap.PlanarSquareDistance(lastOffsetPoint, currentOffsetPoint));

                            // Have objects already been placed on the path?
                            if (lbObjPathSeries.prefabStartOffset <= lastOffsetPointDist)
                            {
                                // Loop through and find any object points between the last point and this one
                                while (lastObjectDistance + newIntervalDistance <= currentOffsetPointDist)
                                {
                                    // Add interval distance to distance of this object
                                    lastObjectDistance += newIntervalDistance;

                                    // Check to see if we're at or beyond the starting distance along the path
                                    if (lastObjectDistance >= lbObjPathSeries.prefabStartOffset)
                                    {
                                        // Lerp: Start at last offset spline point
                                        // Then add the vector from the last offset spline point to the current offset spline point, multiplied by:
                                        // The distance from the last offset spline point to the object divided by
                                        // the distance from the last offset spline point to the current offset spline point
                                        splinePositionList.Add(lastOffsetPoint + ((currentOffsetPoint - lastOffsetPoint) * ((lastObjectDistance - lastOffsetPointDist) / (currentOffsetPointDist - lastOffsetPointDist))));

                                        // Blended forwards calculation code: might be needed someday...
                                        thisPointFwd = cachedSplinePointList[spIdx] - cachedSplinePointList[spIdx - 1];
                                        //Vector3 prevPointFwd = thisPointFwd, nextPointFwd = thisPointFwd;
                                        //if (spIdx > 1) { prevPointFwd = cachedSplinePointList[spIdx - 1] - cachedSplinePointList[spIdx - 2]; }
                                        //if (spIdx < numSplinePoints - 2) { nextPointFwd = cachedSplinePointList[spIdx + 1] - cachedSplinePointList[spIdx]; }
                                        //thisPointFwd = Vector3.Lerp(prevPointFwd, nextPointFwd, (lastObjectDistance - lastOffsetPointDist) / (currentOffsetPointDist - lastOffsetPointDist));
                                        //splinePositionForwardsList.Add(thisPointFwd);

                                        // Calculate forwards direction from this object using direction from last point to the current point
                                        splinePositionForwardsList.Add(thisPointFwd);
                                        // TODO: Tilt calculations
                                        splinePositionRightList.Add(Vector3.Cross(Vector3.up, thisPointFwd));

                                        splinePositionDistancesList.Add(lastObjectDistance);
                                    }
                                }
                            }
                            // Is the object start offset distance between the last spline point and the current spline point?
                            else if (lbObjPathSeries.prefabStartOffset <= currentOffsetPointDist)
                            {
                                // Add the first object point
                                splinePositionForwardsList.Add(splinePointForwards);
                                // TODO: Tilt calculations
                                splinePositionRightList.Add(Vector3.Cross(Vector3.up, splinePointForwards));
                                // Lerp: Start at last offset spline point
                                // Then add the vector from the last offset spline point to the current offset spline point, multiplied by:
                                // The distance from the last offset spline point to the prefab start offset position divided by
                                // the distance from the last offset spline point to the current offset spline point
                                splinePositionList.Add(lastOffsetPoint + ((currentOffsetPoint - lastOffsetPoint) * ((lbObjPathSeries.prefabStartOffset - lastOffsetPointDist) / (currentOffsetPointDist - lastOffsetPointDist))));

                                lastObjectDistance = lbObjPathSeries.prefabStartOffset;

                                splinePositionDistancesList.Add(lastObjectDistance);
                                // Offset second object position by start member offset
                                lastObjectDistance += lbObjPathSeries.startMemberOffset;
                            }
                            // Else: Path Objects haven't started yet so advance to the next spline point

                            // Set "last" values to "current" values in preparation for next iteration
                            lastSplinePoint = currentSplinePoint;
                            lastOffsetPoint = currentOffsetPoint;
                            lastOffsetPointDist = currentOffsetPointDist;
                        }

                        numList = splinePositionList.Count;

                        // If user wishes to finish prefab placement before the end of the path,
                        // remove any prefab positions beyond lastOffsetPointDist - prefabEndOffset
                        if (lbObjPathSeries.prefabEndOffset > 0 && numList > 0)
                        {
                            // Calculate the distance along the path to the prefab end offset
                            // This is the distance from the start to the last offset point minus the prefab end offset distance
                            float endOffsetDist = lastOffsetPointDist - lbObjPathSeries.prefabEndOffset;
                            Vector3 lastPosRemoved = Vector3.zero;
                            float lastPosRemovedDist = 0f;
                            int numPositionsRemoved = 0;

                            // Loop backwards through the list of newly created prefab positions
                            for (int posIdx = numList - 1; posIdx >= 0; posIdx--)
                            {
                                // Remove prefab positions closer to the end of the path than the prefabEndOffset distance
                                if (lastObjectDistance > endOffsetDist)
                                {
                                    int lastPosIdx = splinePositionList.Count - 1;

                                    // Remember the last prefab position removed and the distance for that position
                                    lastPosRemoved = splinePositionList[lastPosIdx];
                                    lastPosRemovedDist = lastObjectDistance;

                                    splinePositionForwardsList.RemoveAt(lastPosIdx);
                                    splinePositionRightList.RemoveAt(lastPosIdx);
                                    splinePositionList.RemoveAt(lastPosIdx);
                                    splinePositionDistancesList.RemoveAt(lastPosIdx);

                                    // TODO: Need to somehow convert this to use varying distances
                                    //lastObjectDistance -= newIntervalDistance;
                                    if (lastPosIdx > 0) { lastObjectDistance = splinePositionDistancesList[lastPosIdx - 1]; }

                                    numPositionsRemoved++;
                                }
                                else { break; }
                            }

                            numList = splinePositionList.Count;

                            // If required, snap last prefab to the end of the shortened path
                            if (lbObjPathSeries.isLastObjSnappedToEnd && numList > 1)
                            {
                                // Find the final position of the shortened path
                                if (numPositionsRemoved > 0)
                                {
                                    // Set the lastOffsetPoint to the last offset point still added,
                                    // and the currentOffsetPoint to the last offset point removed
                                    lastOffsetPoint = splinePositionList[numList - 1];
                                    currentOffsetPoint = lastPosRemoved;

                                    // Get the distances from the start of the path to these two points
                                    lastOffsetPointDist = splinePositionDistancesList[numList - 1];
                                    currentOffsetPointDist = lastPosRemovedDist;

                                    // Lerp: Start at last offset point still added
                                    // Then add the vector from the last offset point still added to the last offset point removed, multiplied by:
                                    // The distance from the last offset point still added to the new path end point divided by
                                    // the distance from the last offset point still added to the last offset point removed
                                    //splinePositionList.Add(lastOffsetPoint + ((currentOffsetPoint - lastOffsetPoint) * (endOffsetDist - (lastPosRemovedDist - newIntervalDistance)) / newIntervalDistance));
                                    splinePositionList.Add(lastOffsetPoint + ((currentOffsetPoint - lastOffsetPoint) * (endOffsetDist - lastOffsetPointDist) / (currentOffsetPointDist - lastOffsetPointDist)));

                                    // Calculate forwards direction from the two object position points this is between
                                    splinePointForwards = currentOffsetPoint - lastOffsetPoint;
                                    splinePositionForwardsList.Add(splinePointForwards);
                                    // TODO: Tilt calculations
                                    splinePositionRightList.Add(Vector3.Cross(Vector3.up, splinePointForwards));
                                    // This distance is assumed to be the end offset distance
                                    splinePositionDistancesList.Add(endOffsetDist);
                                }
                                else
                                {
                                    // No prefab positions were removed because the last prefab position was less than
                                    // prefabEndOffset metres before the end of the path.

                                    // Set the lastOffsetPoint to the last object position,
                                    // and the currentOffsetPoint to the last offset point (this will be the end of the path)
                                    currentOffsetPoint = lastOffsetPoint;
                                    lastOffsetPoint = splinePositionList[numList - 1];

                                    // Calculate the distance from the last object position to the last offset point
                                    float lastToCurrentOffsetPointDist = lbObjPathSeries.use3DDistance ? Vector3.Distance(lastOffsetPoint, currentOffsetPoint) : Mathf.Sqrt(LBMap.PlanarSquareDistance(lastOffsetPoint, currentOffsetPoint));

                                    // Lerp: Start at last object position
                                    // Then add the vector from the last object position to the last offset point, multiplied by:
                                    // The distance from the last object position to the new path end point 
                                    // (the end offset distance minus the last object distance) divided by
                                    // the distance from the last object position to the last offset point
                                    splinePositionList.Add(lastOffsetPoint + ((currentOffsetPoint - lastOffsetPoint) * (endOffsetDist - lastObjectDistance) / lastToCurrentOffsetPointDist));

                                    // Calculate forwards direction from the two object position points this is between
                                    splinePointForwards = currentOffsetPoint - lastOffsetPoint;
                                    splinePositionForwardsList.Add(splinePointForwards);
                                    // TODO: Tilt calculations
                                    splinePositionRightList.Add(Vector3.Cross(Vector3.up, splinePointForwards));
                                    // This distance is assumed to be the end offset distance
                                    splinePositionDistancesList.Add(endOffsetDist);
                                }
                            }

                        }
                        // If no prefabEndOffset distance is set, snap to end of path if required
                        else if (lbObjPathSeries.isLastObjSnappedToEnd && numList > 1)
                        {
                            Vector3 finalPos = GetPathOffsetPosition(cachedSplinePointList[numSplinePoints - 1], splinePointForwards, lbObjPathSeries.useNonOffsetDistance ? 0f : lbObjPathSeries.prefabOffsetZ, 0f);
                            // Only add the new position if it is different from the last position
                            if (finalPos != splinePositionList[numList - 1])
                            {
                                splinePositionList.Add(finalPos);
                                // Copy forwards direction from last point
                                splinePositionForwardsList.Add(splinePositionForwardsList[numList - 1]);
                                splinePositionRightList.Add(splinePositionRightList[numList - 1]);
                                // Guess the distance to the final point, by simply adding the straight-line distance from the last point
                                float lastToFinalPointDist = lbObjPathSeries.use3DDistance ? Vector3.Distance(splinePositionList[numList - 1], finalPos) : Mathf.Sqrt(LBMap.PlanarSquareDistance(splinePositionList[numList - 1], finalPos));
                                splinePositionDistancesList.Add(splinePositionDistancesList[numList - 1] + lastToFinalPointDist);
                            }
                        }

                        numList = splinePositionList.Count;

                        // If snap object to end is enabled, we will have currently simply added another point to the end
                        // Now, if this point is within half the interval distance of the previous point, remove the previous
                        // point, creating the effect of simply "snapping" the previous point to the end
                        if (lbObjPathSeries.isLastObjSnappedToEnd && numList > 1)
                        {
                            lastOffsetPoint = splinePositionList[numList - 2];
                            currentOffsetPoint = splinePositionList[numList - 1];
                            float distBetweenLastTwoPoints = lbObjPathSeries.use3DDistance ? Vector3.Distance(lastOffsetPoint, currentOffsetPoint) : Mathf.Sqrt(LBMap.PlanarSquareDistance(lastOffsetPoint, currentOffsetPoint));
                            if (distBetweenLastTwoPoints < newIntervalDistance / 2f)
                            {
                                splinePositionList.RemoveAt(numList - 2);
                                splinePositionForwardsList.RemoveAt(numList - 2);
                                splinePositionRightList.RemoveAt(numList - 2);
                                splinePositionDistancesList.RemoveAt(numList - 2);
                            }
                        }
                        // If snap object to end is disabled, we need to adjust the position of the last object to match end member offset
                        else if (lbObjPathSeries.endMemberOffset != 0f)
                        {
                            // Calculate the new distance for this object from the start of the path
                            lastObjectDistance = splinePositionDistancesList[numList - 1] + lbObjPathSeries.endMemberOffset;
                            splinePositionDistancesList[numList - 1] = lastObjectDistance;

                            // Now we want to do the same loop as we would for any other point:
                            // Loop through all the passed in spline points and find which two points the end member distance is in between
                            // Then use this to calculate the new end member position and forwards direction

                            // Initialise variables, set default values
                            lastSplinePoint = cachedSplinePointList[0];
                            splinePointForwards = cachedSplinePointList[1] - cachedSplinePointList[0];
                            lastOffsetPoint = GetPathOffsetPosition(lastSplinePoint, splinePointForwards, lbObjPathSeries.useNonOffsetDistance ? 0f : lbObjPathSeries.prefabOffsetZ, 0f);
                            // All distances start at zero (start of path)
                            lastOffsetPointDist = 0f;
                            currentOffsetPointDist = 0f;

                            bool foundNewEndMemberPosition = false;

                            // Loop through spline points, starting with second point
                            for (int spIdx = 1; spIdx < numSplinePoints; spIdx++)
                            {
                                // Don't update forwards if this is the last point on the path
                                // When this is the case, forwards will be the same as the last point
                                if (spIdx < numSplinePoints - 1)
                                {
                                    // Forwards for this point is direction from this spline point to the next one
                                    splinePointForwards = cachedSplinePointList[spIdx + 1] - cachedSplinePointList[spIdx];
                                }

                                // Get current spline point
                                currentSplinePoint = cachedSplinePointList[spIdx];
                                // Get current offset point
                                currentOffsetPoint = GetPathOffsetPosition(currentSplinePoint, splinePointForwards, lbObjPathSeries.useNonOffsetDistance ? 0f : lbObjPathSeries.prefabOffsetZ, 0f);

                                // Caclulate distance from start of path to this point - simply add distance from the
                                // last offset point to this one
                                currentOffsetPointDist += lbObjPathSeries.use3DDistance ? Vector3.Distance(lastOffsetPoint, currentOffsetPoint) : Mathf.Sqrt(LBMap.PlanarSquareDistance(lastOffsetPoint, currentOffsetPoint));

                                // Check to see if the new end member distance is between these two points
                                if (lastOffsetPointDist < lastObjectDistance && currentOffsetPointDist > lastObjectDistance)
                                {
                                    // Lerp: Start at last offset spline point
                                    // Then add the vector from the last offset spline point to the current offset spline point, multiplied by:
                                    // The distance from the last offset spline point to the object divided by
                                    // the distance from the last offset spline point to the current offset spline point
                                    splinePositionList[numList - 1] = lastOffsetPoint + ((currentOffsetPoint - lastOffsetPoint) * ((lastObjectDistance - lastOffsetPointDist) / (currentOffsetPointDist - lastOffsetPointDist)));

                                    // Blended forwards calculation code: might be needed someday...
                                    //Vector3 thisPointFwd = cachedSplinePointList[spIdx] - cachedSplinePointList[spIdx - 1];
                                    //Vector3 prevPointFwd = thisPointFwd, nextPointFwd = thisPointFwd;
                                    //if (spIdx > 1) { prevPointFwd = cachedSplinePointList[spIdx - 1] - cachedSplinePointList[spIdx - 2]; }
                                    //if (spIdx < numSplinePoints - 2) { nextPointFwd = cachedSplinePointList[spIdx + 1] - cachedSplinePointList[spIdx]; }
                                    //thisPointFwd = Vector3.Lerp(prevPointFwd, nextPointFwd, (lastObjectDistance - lastOffsetPointDist) / (currentOffsetPointDist - lastOffsetPointDist));
                                    //splinePositionForwardsList.Add(thisPointFwd);

                                    // Calculate forwards direction from this object using direction from last point to the current point
                                    splinePositionForwardsList[numList - 1] = cachedSplinePointList[spIdx] - cachedSplinePointList[spIdx - 1];
                                    // TODO: Tilt calculations
                                    splinePositionRightList[numList - 1] = Vector3.Cross(Vector3.up, splinePositionForwardsList[numList - 1]);

                                    foundNewEndMemberPosition = true;
                                }

                                // Set "last" values to "current" values in preparation for next iteration
                                lastSplinePoint = currentSplinePoint;
                                lastOffsetPoint = currentOffsetPoint;
                                lastOffsetPointDist = currentOffsetPointDist;
                            }

                            // If we didn't find the new position of the end member, it must be further than the length of the path
                            if (!foundNewEndMemberPosition)
                            {
                                // Since it is further than the length of the path, simply extrapolate the position
                                // using the forwards information
                                splinePositionList[numList - 1] += splinePositionForwardsList[numList - 1] * lbObjPathSeries.endMemberOffset;
                            }
                        }

                        // If using the distances along the Placement Spline (Centre, Left or Right), without the Offset Z,
                        // update the positions with the OffsetZ distance perpendicular to the Placement Spline.
                        if (lbObjPathSeries.useNonOffsetDistance && lbObjPathSeries.prefabOffsetZ != 0f)
                        {
                            numList = splinePositionList.Count;
                            for (int sppIdx = 0; sppIdx < numList; sppIdx++)
                            {
                                splinePositionList[sppIdx] = GetPathOffsetPosition(splinePositionList[sppIdx], splinePositionForwardsList[sppIdx], lbObjPathSeries.prefabOffsetZ, 0f);
                            }
                        }
                    }
                }
            }
        }

        #region Bounds Methods

        /// <summary>
        /// Get the worldspace 2D bounds of Object Path including the extend to
        /// which it is blended with the surroundings topography
        /// returns minX, minZ, maxX, maxZ worldspace boundaries.
        /// PREREQUISITES: RefreshObjPathPositions() and useWidth = true.
        /// NOTE: Assumes centre spline, left and right surrounds splines have equal number of points.
        /// See also GetSplineBounds(Vector3[] splineLeft, Vector3[] splineRight).
        /// </summary>
        /// <returns></returns>
        public Vector4 GetSurroundBounds()
        {
            Vector4 bounds = new Vector4(float.PositiveInfinity, float.PositiveInfinity, float.NegativeInfinity, float.NegativeInfinity);

            if (useWidth && isSplinesCached2)
            {
                int numSurroundSplinePoints = (cachedSplinePointLeftSurroundList == null ? 0 : cachedSplinePointLeftSurroundList.Count);
                if (numSurroundSplinePoints == cachedCentreSplinePointsLength)
                {
                    Vector3 pt = Vector3.zero;
                    for (int ptIdx = 0; ptIdx < numSurroundSplinePoints; ptIdx++)
                    {
                        // Check left surrounds path point
                        pt = cachedSplinePointLeftSurroundList[ptIdx];
                        // Check xMin, zMin
                        if (pt.x < bounds.x) { bounds.x = pt.x; }
                        if (pt.z < bounds.y) { bounds.y = pt.z; }
                        // Check xMax, zMax
                        if (pt.x > bounds.z) { bounds.z = pt.x; }
                        if (pt.z > bounds.w) { bounds.w = pt.z; }
                        
                        // Check right surrounds path point
                        pt = cachedSplinePointRightSurroundList[ptIdx];
                        // Check xMin, zMin
                        if (pt.x < bounds.x) { bounds.x = pt.x; }
                        if (pt.z < bounds.y) { bounds.y = pt.z; }
                        // Check xMax, zMax
                        if (pt.x > bounds.z) { bounds.z = pt.x; }
                        if (pt.z > bounds.w) { bounds.w = pt.z; }
                    }
                }
            }

            return bounds;
        }

        /// <summary>
        /// Get the 2D bounds of Object Path left and right splines.
        /// returns minX, minZ, maxX, maxZ worldspace boundaries.
        /// NOTE: Assumes left and right splines have equal number of points.
        /// See also GetSplineBounds(splineLeft, splineRight), GetSurroundBounds().
        /// PREREQUISITES: RefreshObjPathPositions() and useWidth = true.
        /// </summary>
        /// <returns></returns>
        public Vector4 GetSplineBounds()
        {
            Vector4 bounds = new Vector4(float.PositiveInfinity, float.PositiveInfinity, float.NegativeInfinity, float.NegativeInfinity);

            int numSplinePoints = (cachedSplinePointLeftEdgeList == null ? 0 : cachedSplinePointLeftEdgeList.Count);
            // Ensure both splines have the same number of points
            if (numSplinePoints == (cachedSplinePointRightEdgeList == null ? 0 : cachedSplinePointRightEdgeList.Count))
            {
                Vector3 pt = Vector3.zero;
                for (int ptIdx = 0; ptIdx < numSplinePoints; ptIdx++)
                {
                    // Check left path point
                    pt = cachedSplinePointLeftEdgeList[ptIdx];
                    // Check xMin, zMin
                    if (pt.x < bounds.x) { bounds.x = pt.x; }
                    if (pt.z < bounds.y) { bounds.y = pt.z; }
                    // Check xMax, zMax
                    if (pt.x > bounds.z) { bounds.z = pt.x; }
                    if (pt.z > bounds.w) { bounds.w = pt.z; }

                    // Check right path point
                    pt = cachedSplinePointRightEdgeList[ptIdx];
                    // Check xMin, zMin
                    if (pt.x < bounds.x) { bounds.x = pt.x; }
                    if (pt.z < bounds.y) { bounds.y = pt.z; }
                    // Check xMax, zMax
                    if (pt.x > bounds.z) { bounds.z = pt.x; }
                    if (pt.z > bounds.w) { bounds.w = pt.z; }
                }
            }

            return bounds;
        }

        #endregion

        /// <summary>
        /// Convert a list of spline points from Group-space to World-space as an array.
        /// Groups can be manually or procedurally placed in the landscape. The centre of
        /// each Group (param: groupWorldPos) can be rotated on the Y-axis (param: groupRotationY).
        /// Typically used in converting LBObjPath for Clearing Groups into World Space coordinates.
        /// </summary>
        /// <param name="splineListIn"></param>
        /// <param name="splineArrayOut"></param>
        /// <param name="groupWorldPos"></param>
        /// <param name="groupRotationY"></param>
        public void ToWorldSpace(List<Vector3> splineListIn, Vector3[] splineArrayOut, Vector3 groupWorldPos, float groupRotationY)
        {
            int numSplinePointsIn = splineListIn == null ? 0 : splineListIn.Count;
            int numSplinePointsOut = splineArrayOut == null ? 0 : splineArrayOut.Length;

            // Make sure there is some valid data to process
            if (numSplinePointsIn > 0 && numSplinePointsIn == numSplinePointsOut)
            {
                for (int ptIdx = 0; ptIdx < numSplinePointsIn; ptIdx++)
                {
                    splineArrayOut[ptIdx] = Quaternion.Euler(0f, groupRotationY, 0f) * splineListIn[ptIdx];
                    splineArrayOut[ptIdx] += groupWorldPos;
                }
            }
        }

        /// <summary>
        /// Create a populated LBMesh class from the Object Path's left and right splines.
        /// See also LBPath.CreateMeshFromPath which is typically used for a LBMapPath mesh.
        /// PREREQUISITES: RefreshObjPathPositions(.., ** true **) and useWidth = true.
        /// WARNING: Currently doesn't support > 64K verts.
        /// The regionNumber is used to identify clearings in the landscape. For uniform
        /// groups, it is always 1. For clearings or subgroups it is regionIdx + 1.
        /// NOTE: addNormals can be set to false to use mesh.RecalculateNormals() and
        /// have Unity automatically calculate them. This is a WORKAROUND for ObjPath
        /// surface meshes in non-Uniform Groups which for some reason are direction sensitive.
        /// isFollowTerrain indicates that the mesh should follow the general trend of the terrain,
        /// based on the terrain height at each user-defined point, relative to the anchor point
        /// at the start of the path.
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="heightMap1D"></param>
        /// <param name="heightmapResolution"></param>
        /// <param name="convertToWorldSpace"></param>
        /// <param name="groupWorldPos"></param>
        /// <param name="groupRotationY"></param>
        /// <param name="isGroupDesigner"></param>
        /// <param name="regionNumber"></param>
        /// <param name="addNormals"></param>
        /// <param name="isFollowTerrain"></param>
        /// <param name="anchorHeightLS"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public bool CreateMeshFromPath
        (
            LBLandscape landscape, float[] heightMap1D, int heightmapResolution, bool convertToWorldSpace,
            Vector3 groupWorldPos, float groupRotationY, bool isGroupDesigner, int regionNumber,
            bool addNormals, bool isFollowTerrain, float anchorHeightLS, bool showErrors
        )
        {
            bool isSuccess = false;

            #if UNITY_EDITOR
            float tStart = Time.realtimeSinceStartup;
            #endif

            lbMesh = new LBMesh();

            if (landscape == null) { if (showErrors) { Debug.LogWarning("LBObjPath.CreateMeshFromPath - landscape cannot be null"); } }
            else if (!isGroupDesigner && heightMap1D == null) { if (showErrors) { Debug.LogWarning("LBObjPath.CreateMeshFromPath - heightMap1D cannot be null"); } }
            else if (cachedCentreSplinePointsLength < 2) { if (showErrors) { Debug.LogWarning("LBObjPath.CreateMeshFromPath - must have at least 2 left spline points to create mesh"); } }
            else if (isFollowTerrain && cachedCentreSplinePointDistancesList == null) { if (showErrors) { Debug.LogWarning("LBObjPath.CreateMeshFromPath - cachedCentreSplinePointDistancesList cannot be null"); } }
            else if (lbMesh != null)
            {
                #region Get Splines
                Vector3[] splinePointsLeft = null;
                Vector3[] splinePointsRight = null;

                // Only used when isFollowTerrain is true
                Vector3[] splinePointsCentre = null;

                // If this ObjPath belongs to a Manual or Procedural Clearing Group, then the spline points need to be
                // converted from Group-space to World-space coordinates.
                if (convertToWorldSpace)
                {
                    splinePointsLeft = new Vector3[cachedCentreSplinePointsLength];
                    splinePointsRight = new Vector3[cachedCentreSplinePointsLength];

                    ToWorldSpace(cachedSplinePointLeftBorderList, splinePointsLeft, groupWorldPos, groupRotationY);
                    ToWorldSpace(cachedSplinePointRightBorderList, splinePointsRight, groupWorldPos, groupRotationY);

                    if (isFollowTerrain)
                    {
                        splinePointsCentre = new Vector3[cachedCentreSplinePointsLength];
                        ToWorldSpace(cachedCentreSplinePointList, splinePointsCentre, groupWorldPos, groupRotationY);
                    }
                }
                else
                {
                    splinePointsLeft = cachedSplinePointLeftBorderList.ToArray();
                    splinePointsRight = cachedSplinePointRightBorderList.ToArray();

                    // isFollowTerrain is typically only used inside Clearings and SubGroups but added here for completeness
                    if (isFollowTerrain)
                    {
                        splinePointsCentre = cachedCentreSplinePointList.ToArray();
                    }
                }
                #endregion

                // Perform some validation
                if (splinePointsLeft == null) { Debug.LogWarning("LBObjPath.CreateMeshFromPath - no left splines defined"); }
                else if (splinePointsRight == null) { Debug.LogWarning("LBObjPath.CreateMeshFromPath - no right splines defined"); }
                else if (splinePointsLeft.Length < 2) { Debug.LogWarning("LBObjPath.CreateMeshFromPath - must have at least 2 left spline points to create mesh"); }
                else if (splinePointsRight.Length < 2) { Debug.LogWarning("LBObjPath.CreateMeshFromPath - must have at least 2 right spline points to create mesh"); }
                else if (splinePointsLeft.Length != splinePointsRight.Length) { Debug.LogWarning("LBObjPath.CreateMeshFromPath - in this release the number of left and right spline points must be the same"); }
                else
                {
                    #region Initialise Variables
                    List<Vector3> vertices = new List<Vector3>();
                    List<int> triangles = new List<int>();
                    List<Vector2> uvs = new List<Vector2>();
                    //List<Vector2> uv2s = new List<Vector2>();
                    //List<Vector2> uv3s = new List<Vector2>();
                    //List<Vector2> uv4s = new List<Vector2>();
                    List<Vector3> normals = new List<Vector3>();
                    List<Vector4> tangents = new List<Vector4>();
                    List<Vector4> colours = new List<Vector4>();    // Store as Vector4s rather than Color so they are serializable

                    // Default colour of each vert (stored in LB as a Vector4)
                    Vector4 defaultVertColour = new Vector4(1f, 1f, 1f, 1f);

                    int numSplinePts = splinePointsRight.Length;
                    int numRightVerts = numSplinePts;
                    int maxPoints = (65536 / 4) - 4;

                    int startIdx = 0;
                    int endPtIdx = numSplinePts - 1;

                    // The number of spline points to optionally trim from the start and end if blendStart/End are enabled
                    int blendSplinePoints = Mathf.CeilToInt(edgeBlendWidth / pathResolution);

                    // Adjust the starting and ending spline points for the mesh when the ends are blended with the terrain
                    if (blendStart) { startIdx = blendSplinePoints; numRightVerts -= startIdx; }
                    if (blendEnd) { endPtIdx -= blendSplinePoints+1; numRightVerts -= blendSplinePoints+1; }

                    int currentBottomRightVert = 0;
                    float meshLength = splineLengthToLastPoint - (startIdx * pathResolution);
                    float uvRepeat = meshLength / GetMinWidth();

                    // Used for calculating UVs when relative to Landscape
                    // landscape.start should be the same as transform.position (but without tranform overhead)
                    Vector3 landscapeWorldPosition = landscape.start;

                    Vector3 vert1, vert2;
                    Vector3 vert1N, vert2N;
                    Vector2 pt1N, pt2N;
                    float leftEdgeTerrainHeight = 0f, rightEdgeTerrainHeight = 0f, minEdgeTerrainHeight, avgEdgeTerrainHeight;
                    float centreTerrainHeightLS = 0f, centreTerrainHeightDeltaLS = 0f;
                    Vector3 centrePt;

                    // For normals we need to consider the previous (pt-1), and the next points (pt+1)
                    Vector3 vert1prev = Vector3.zero, vert1next = Vector3.zero, vert2prev = Vector3.zero, vert2next = Vector3.zero;
                    Vector3 v1v2;
                    Vector4 tangent;

                    #endregion

                    if (numRightVerts > 0 && startIdx < numRightVerts)
                    {
                        // TODO - these should probably consider meshEdgeSnapToTerrain
                        vert1prev = splinePointsRight[startIdx];
                        vert2prev = splinePointsLeft[startIdx];
                    }

                    // Get terrain height once
                    landscape.SetLandscapeTerrains(false);
                    float terrainHeight = landscape.GetLandscapeTerrainHeight();

                    for (int pt = startIdx; pt <= endPtIdx && pt < maxPoints; pt++)
                    {
                        #region Verts
                        vert1 = splinePointsRight[pt];
                        vert2 = splinePointsLeft[pt];

                        if (isFollowTerrain)
                        {
                            if (!isGroupDesigner)
                            {
                                centrePt = splinePointsCentre[pt];
                                // Get the terrain height, in landscape-space (world-space without the landscape.start.y offset)
                                centreTerrainHeightLS = LBLandscapeTerrain.GetHeight(landscape, heightMap1D, heightmapResolution, new Vector2(centrePt.x, centrePt.z), true) * terrainHeight;

                                // This is a really simple delta for each spline point - it should be lerped between user-defined path points
                                centreTerrainHeightDeltaLS = centreTerrainHeightLS - anchorHeightLS;
                                vert1.y += centreTerrainHeightDeltaLS;
                                vert2.y += centreTerrainHeightDeltaLS;
                            }
                        }
                        else if (meshEdgeSnapToTerrain)
                        {
                            if (!isGroupDesigner)
                            {
                                // Find the terrain height at the left and right positions in the landscape.
                                rightEdgeTerrainHeight = (LBLandscapeTerrain.GetHeight(landscape, heightMap1D, heightmapResolution, new Vector2(vert1.x, vert1.z), true) * terrainHeight) + landscape.start.y;
                                leftEdgeTerrainHeight = (LBLandscapeTerrain.GetHeight(landscape, heightMap1D, heightmapResolution, new Vector2(vert2.x, vert2.z), true) * terrainHeight) + landscape.start.y;
                            }

                            if (meshSnapType == MeshSnapType.BothEdges)
                            {
                                vert1.y = rightEdgeTerrainHeight;
                                vert2.y = leftEdgeTerrainHeight;
                            }
                            else if (meshSnapType == MeshSnapType.MinLeftRightEdges)
                            {
                                minEdgeTerrainHeight = rightEdgeTerrainHeight < leftEdgeTerrainHeight ? rightEdgeTerrainHeight : leftEdgeTerrainHeight;
                                vert1.y = minEdgeTerrainHeight;
                                vert2.y = minEdgeTerrainHeight;
                            }
                            else if (meshSnapType == MeshSnapType.MaxLeftRightEdges)
                            {
                                minEdgeTerrainHeight = rightEdgeTerrainHeight > leftEdgeTerrainHeight ? rightEdgeTerrainHeight : leftEdgeTerrainHeight;
                                vert1.y = minEdgeTerrainHeight;
                                vert2.y = minEdgeTerrainHeight;
                            }
                            else // Average of left and right heights
                            {
                                avgEdgeTerrainHeight = (rightEdgeTerrainHeight + leftEdgeTerrainHeight) / 2f;
                                vert1.y = avgEdgeTerrainHeight;
                                vert2.y = avgEdgeTerrainHeight;
                            }
                        }

                        vertices.Add(vert1);
                        vertices.Add(vert2);
                        #endregion

                        // Set default vert colour
                        colours.Add(defaultVertColour);
                        colours.Add(defaultVertColour);

                        if (pt > startIdx)
                        {
                            #region Triangles
                            // Bottom (right) triangle
                            triangles.Add(currentBottomRightVert);
                            triangles.Add(currentBottomRightVert + 1);
                            triangles.Add(currentBottomRightVert + 2);

                            // Top (left) triangle
                            triangles.Add(currentBottomRightVert + 1);
                            triangles.Add(currentBottomRightVert + 3);
                            triangles.Add(currentBottomRightVert + 2);

                            if (meshIsDoubleSided)
                            {
                                // Bottom (right) triangle
                                triangles.Add(currentBottomRightVert);
                                triangles.Add(currentBottomRightVert + 2);
                                triangles.Add(currentBottomRightVert + 1);

                                // Top (left) triangle
                                triangles.Add(currentBottomRightVert + 1);
                                triangles.Add(currentBottomRightVert + 2);
                                triangles.Add(currentBottomRightVert + 3);
                            }
                            #endregion

                            // Used to calculate normals
                            if (pt < numRightVerts - 1)
                            {
                                vert1next = splinePointsRight[pt + 1];
                                vert2next = splinePointsLeft[pt + 1];
                            }
                            else
                            {
                                // Last two points
                                vert1next = vert1;
                                vert2next = vert2;
                            }
                        }

                        // Add the normals for the new verts
                        normals.Add(-Vector3.Cross(vert1next - vert1prev, vert2 - vert1).normalized);
                        normals.Add(Vector3.Cross(vert2next - vert2prev, vert1 - vert2).normalized);

                        #region Tangents
                        v1v2 = (vert2 - vert1).normalized;
                        tangent = new Vector4(v1v2.x, v1v2.y, v1v2.z, 1f);
                        tangents.Add(tangent);
                        tangents.Add(tangent);
                        #endregion

                        #region Create UVs
                        if (isMeshLandscapeUV)
                        {
                            // The UVs are relative to the vert position in the landscape. Useful for things like rivers
                            // where the water asset usually covers the whole landscape
                            vert1N = vert1 - landscapeWorldPosition;
                            vert2N = vert2 - landscapeWorldPosition;

                            // Verts normalised to landscape
                            pt1N = new Vector2(vert1N.x / landscape.size.x * meshUVTileScale.x, vert1N.z / landscape.size.y * meshUVTileScale.y);
                            pt2N = new Vector2(vert2N.x / landscape.size.x * meshUVTileScale.x, vert2N.z / landscape.size.y * meshUVTileScale.y);

                            // Add UVs in same order as verts
                            // u is in direction of the path, v is left to right across the path
                            uvs.Add(pt1N);
                            uvs.Add(pt2N);
                        }
                        else
                        {
                            float splineDistPtNormalised = cachedCentreSplinePointDistancesList[pt] / meshLength;

                            // Muliply by the uvRepeat factor which assumes the texture covers the full width
                            // of the path and that the texture being used is set to Repeat not Clamp.
                            splineDistPtNormalised *= uvRepeat;

                            splineDistPtNormalised *= meshUVTileScale.y;

                            // Add UVs in same order as verts
                            if (isSwitchMeshUVs)
                            {
                                // u is left to right across the path, v is in the direction of the path
                                uvs.Add(new Vector2(splineDistPtNormalised, meshUVTileScale.x));
                                uvs.Add(new Vector2(splineDistPtNormalised, 0f));
                            }
                            else
                            {
                                // u is in the direction of the path, v is left to right across the path
                                uvs.Add(new Vector2(meshUVTileScale.x, splineDistPtNormalised));
                                uvs.Add(new Vector2(0f, splineDistPtNormalised));
                            }

                        }
                        #endregion

                        vert1prev = vert1;
                        vert2prev = vert2;

                        if (pt > startIdx) { currentBottomRightVert += 2; }
                    }

                    // As a workaround for Clearing and SubGroups, get Unity to calculate normals in LBMesh.UpdateMesh
                    if (!addNormals) { normals.Clear(); }
                    int numNormals = (normals == null ? 0 : normals.Count);

                    if (numNormals > 1)
                    {
                        // Set starting normals
                        normals[0] = Vector3.up;
                        normals[1] = Vector3.up;
                        if (numNormals > 3)
                        {
                            // Set normals at end
                            //normals[numNormals - 2] = Vector3.up;
                            //normals[numNormals - 1] = Vector3.up;
                        }
                    }

                    lbMesh.verts = vertices;
                    lbMesh.triangles = triangles;
                    lbMesh.normals = normals;
                    lbMesh.uvs = uvs;
                    //lbMesh.uv2s = new List<Vector2>(uvs);
                    lbMesh.tangents = tangents;

                    isSuccess = RebuildMesh();

                    // update the name of the mesh
                    if (isSuccess && lbMesh.mesh != null) { lbMesh.mesh.name += "." + regionNumber; }

                    #if UNITY_EDITOR
                    if (landscape.showTiming) { Debug.Log("LBObjPath.CreateMeshFromPath - created Mesh (verts " + vertices.Count + " tris " + (triangles.Count / 3) + ") in " + (Time.realtimeSinceStartup - tStart).ToString("0.000") + " seconds"); }
                    #endif
                }
            }
            return isSuccess;
        }

        /// <summary>
        /// Create a base mesh under the surface mesh.
        /// The regionNumber is used to identify clearings in the landscape. For uniform
        /// groups, it is always 1. For clearings or subgroups it is regionIdx + 1.
        /// Meshes that are no longer required should be cleaned up / deleted from the scene
        /// using LBLandscapeTerrain.RemoveExistingPrefabs(..) before this method is called.
        /// NOTE: addNormals can be set to false to use mesh.RecalculateNormals() and
        /// have Unity automatically calculate them. This is a WORKAROUND for ObjPath
        /// surface meshes in non-Uniform Groups which for some reason are direction sensitive.
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="heightMap1D"></param>
        /// <param name="heightmapResolution"></param>
        /// <param name="convertToWorldSpace"></param>
        /// <param name="groupWorldPos"></param>
        /// <param name="groupRotationY"></param>
        /// <param name="isGroupDesigner"></param>
        /// <param name="regionNumber"></param>
        /// <param name="addNormals"></param>
        /// <param name="isFollowTerrain"></param>
        /// <param name="anchorHeightLS"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public bool CreateBaseMesh
        (
            LBLandscape landscape, float[] heightMap1D, int heightmapResolution, bool convertToWorldSpace,
            Vector3 groupWorldPos, float groupRotationY, bool isGroupDesigner, int regionNumber,
            bool addNormals, bool isFollowTerrain, float anchorHeightLS, bool showErrors
        )
        {
            bool isSuccess = false;

            #if UNITY_EDITOR
            float tStart = Time.realtimeSinceStartup;
            #endif

            LBMesh lbMeshItem = null;

            // Start with an empty list
            if (baseMeshList == null) { baseMeshList = new List<LBMesh>(); }
            else
            {
                // Remove any existing meshes and data from the list
                // Meshes that are no longer required should be cleaned up / deleted from the scene
                // using LBLandscapeTerrain.RemoveExistingPrefabs(..) before this method is called.
                for (int mIdx = 0; mIdx < baseMeshList.Count; mIdx++)
                {
                    lbMeshItem = baseMeshList[mIdx];

                    if (lbMeshItem != null)
                    {
                        if (lbMeshItem.mesh != null)
                        {
                            // If this is being created in a procedural or manual clearing, or a SubGroup,
                            // there many be many instances (regions) in the scene. As there is only
                            // one LBObjPath for this mesh, clearing them all will delete the verts from meshes
                            // already created from other regions in the scene.
                            // SubGroups have a single region for each placement along the ObjPath of a Uniform
                            // or clearing group. So we can't clear the first region as only the last one would
                            // get a mesh. Another problem is going in an out of the ObjPath Editor in Uniform groups.
                            //if (regionNumber == 1) { lbMeshItem.mesh.Clear(); }

                            // remove the link to the previous mesh
                            lbMeshItem.mesh = null;
                        }
                    }
                }
                baseMeshList.Clear();
            }

            if (baseMeshThickness > 0f)
            {
                if (landscape == null) { if (showErrors) { Debug.LogWarning("LBObjPath.CreateBaseMesh - landscape cannot be null"); } }
                else if (!isGroupDesigner && heightMap1D == null) { if (showErrors) { Debug.LogWarning("LBObjPath.CreateBaseMesh - heightMap1D cannot be null"); } }
                else if (cachedCentreSplinePointsLength < 2) { Debug.LogWarning("LBObjPath.CreateBaseMesh - must have at least 2 left spline points to create mesh"); }
                else if (isFollowTerrain && cachedCentreSplinePointDistancesList == null) { if (showErrors) { Debug.LogWarning("LBObjPath.CreateBaseMesh - cachedCentreSplinePointDistancesList cannot be null"); } }
                else
                {
                    // If there is no indent for the surface mesh then we don't want to
                    // add the inner verts at the top, else they will be in the same position
                    // as the top-right and top-left verts.
                    bool isWideBase = leftBorderWidth > 0f ? !baseMeshUseIndent : false;

                    // isWideBase = false
                    // Vert order: TR, BR, BR, BL, BL, TR
                    // 2 verts for each corner plus 1 for top point (6 verts)
                    // End profile of base mesh
                    //  TL          TR
                    //  |           |
                    //  |           |
                    //  -------------
                    //  BL          BR

                    // isWideBase = true
                    // Vert order: ITR, TR, TR, BR, BR, BL, BL, TL, TL, ITL
                    // 2 verts for each corner plus 1 for inner top left/right points (10 verts)
                    // End profile of base mesh
                    //  TL--ITL    ITR--TR
                    //  |               |
                    //  |               |
                    //  -----------------
                    //  BL              BR

                    #region Get Splines
                    Vector3[] splinePointsLeft = null;
                    Vector3[] splinePointsRight = null;
                    Vector3[] splinePointsLeftInner = null;
                    Vector3[] splinePointsRightInner = null;

                    // Only used when isFollowTerrain is true
                    Vector3[] splinePointsCentre = null;

                    // If this ObjPath belongs to a Manual or Procedural Clearing Group, then the spline points need to be
                    // converted from Group-space to World-space coordinates.
                    if (convertToWorldSpace)
                    {
                        splinePointsLeft = new Vector3[cachedCentreSplinePointsLength];
                        splinePointsRight = new Vector3[cachedCentreSplinePointsLength];

                        if (isWideBase)
                        {
                            // For a wide base the outer verts are on the edge of the path
                            ToWorldSpace(cachedSplinePointLeftEdgeList, splinePointsLeft, groupWorldPos, groupRotationY);
                            ToWorldSpace(cachedSplinePointRightEdgeList, splinePointsRight, groupWorldPos, groupRotationY);

                            // For a wide base we also need the border spline to get the inner top-left and top-right verts
                            splinePointsLeftInner = new Vector3[cachedCentreSplinePointsLength];
                            splinePointsRightInner = new Vector3[cachedCentreSplinePointsLength];

                            ToWorldSpace(cachedSplinePointLeftBorderList, splinePointsLeftInner, groupWorldPos, groupRotationY);
                            ToWorldSpace(cachedSplinePointRightBorderList, splinePointsRightInner, groupWorldPos, groupRotationY);
                        }
                        else
                        {
                            ToWorldSpace(cachedSplinePointLeftBorderList, splinePointsLeft, groupWorldPos, groupRotationY);
                            ToWorldSpace(cachedSplinePointRightBorderList, splinePointsRight, groupWorldPos, groupRotationY);
                        }

                        if (isFollowTerrain)
                        {
                            splinePointsCentre = new Vector3[cachedCentreSplinePointsLength];
                            ToWorldSpace(cachedCentreSplinePointList, splinePointsCentre, groupWorldPos, groupRotationY);
                        }
                    }
                    else
                    {
                        if (isWideBase)
                        {
                            // For a wide base the outer verts are on the edge of the path
                            splinePointsLeft = cachedSplinePointLeftEdgeList.ToArray();
                            splinePointsRight = cachedSplinePointRightEdgeList.ToArray();

                            // For a wide base we also need the border spline to get the inner top-left and top-right verts
                            splinePointsLeftInner = cachedSplinePointLeftBorderList.ToArray();
                            splinePointsRightInner = cachedSplinePointRightBorderList.ToArray();
                        }
                        else
                        {
                            splinePointsLeft = cachedSplinePointLeftBorderList.ToArray();
                            splinePointsRight = cachedSplinePointRightBorderList.ToArray();
                        }

                        // isFollowTerrain is typically only used inside Clearings and SubGroups but added here for completeness
                        if (isFollowTerrain)
                        {
                            splinePointsCentre = cachedCentreSplinePointList.ToArray();
                        }
                    }
                    #endregion

                    // Perform some validation
                    if (splinePointsLeft == null) { Debug.LogWarning("LBObjPath.CreateBaseMesh - no left splines defined"); }
                    else if (splinePointsRight == null) { Debug.LogWarning("LBObjPath.CreateBaseMesh - no right splines defined"); }
                    else if (splinePointsLeft.Length < 2) { Debug.LogWarning("LBObjPath.CreateBaseMesh - must have at least 2 left spline points to create mesh"); }
                    else if (splinePointsRight.Length < 2) { Debug.LogWarning("LBObjPath.CreateBaseMesh - must have at least 2 right spline points to create mesh"); }
                    else if (splinePointsLeft.Length != splinePointsRight.Length) { Debug.LogWarning("LBObjPath.CreateBaseMesh - in this release the number of left and right spline points must be the same"); }
                    else
                    {
                        #region Initialise variables
                        // Get terrain height once
                        landscape.SetLandscapeTerrains(false);
                        float terrainHeight = landscape.GetLandscapeTerrainHeight();

                        // Used for calculating UVs when relative to Landscape
                        // landscape.start should be the same as transform.position (but without tranform overhead)
                        Vector3 landscapeWorldPosition = landscape.start;

                        List<Vector3> vertices = new List<Vector3>();
                        List<int> triangles = new List<int>();
                        List<Vector2> uvs = new List<Vector2>();
                        List<Vector3> normals = new List<Vector3>();
                        List<Vector4> tangents = new List<Vector4>();
                        List<Vector4> colours = new List<Vector4>();    // Store as Vector4s rather than Color so they are serializable

                        // Default colour of each vert (stored in LB as a Vector4)
                        Vector4 defaultVertColour = new Vector4(1f, 1f, 1f, 1f);

                        int numVertsPerPathPoint = isWideBase ? 10 : 6;

                        // When using wide base, there are 2 extra verts at the start of each "row" of verts. This is to account for the
                        // top right plane and is made up of inner top right (ITR) and top right (TR) verts. See diagrams above.
                        int wideBaseVertIdxOffset = isWideBase ? 2 : 0;

                        int maxPoints = Mathf.CeilToInt(65536f / (float)numVertsPerPathPoint) - numVertsPerPathPoint;

                        int numSplinePts = splinePointsRight.Length;
                        int numRightVerts = numSplinePts;

                        // The number of spline points to optionally trim from the start and end if blendStart/End are enabled
                        int blendSplinePoints = Mathf.CeilToInt(edgeBlendWidth / pathResolution);

                        Vector3 vertBR = Vector3.zero, vertBL = Vector3.zero, vertTR = Vector3.zero, vertTL = Vector3.zero, vertITR = Vector3.zero, vertITL = Vector3.zero;
                        Vector3 vertBRN = Vector3.zero, vertBLN = Vector3.zero;
                        Vector3 vertTRN = Vector3.zero, vertTLN = Vector3.zero;
                        Vector3 vertITRN = Vector3.zero, vertITLN = Vector3.zero;
                        Vector2 ptBRN, ptBLN, ptTRN, ptTLN, ptITRN = Vector3.zero, ptITLN = Vector3.zero;
                        float leftEdgeTerrainHeight = 0f, rightEdgeTerrainHeight = 0f, minEdgeTerrainHeight, avgEdgeTerrainHeight;
                        float centreTerrainHeightLS = 0f, centreTerrainHeightDeltaLS = 0f;
                        Vector3 centrePt;

                        // For normals we need to consider the previous (pt-1), and the next points (pt+1)
                        Vector3 vertBRprev = Vector3.zero, vertBRnext = Vector3.zero, vertBLprev = Vector3.zero, vertBLnext = Vector3.zero;
                        Vector3 vertTRprev = Vector3.zero, vertTRnext = Vector3.zero, vertTLprev = Vector3.zero, vertTLnext = Vector3.zero;
                        Vector3 vertITRprev = Vector3.zero, vertITRnext = Vector3.zero, vertITLprev = Vector3.zero, vertITLnext = Vector3.zero;
                        Vector3 vBRBL, vTRBR, vTLBL, vITRTR, vTLITL;
                        Vector4 tangent;
                        #endregion

                        // TODO - loop based on max vert limit

                        // TODO calc number of meshes required
                        int numMeshes = 1;

                        for (int mshIdx = 0; mshIdx < numMeshes; mshIdx++)
                        {
                            int startIdx = 0;
                            int endPtIdx = numSplinePts - 1;

                            // Adjust the starting and ending spline points for the mesh when the ends are blended with the terrain
                            if (blendStart) { startIdx = blendSplinePoints; numRightVerts -= startIdx; }
                            if (blendEnd) { endPtIdx -= blendSplinePoints + 1; numRightVerts -= blendSplinePoints + 1; }

                            int currentQuadBottomRightVert = 0;
                            float meshLength = splineLengthToLastPoint - (startIdx * pathResolution);
                            float uvRepeat = meshLength / GetMinWidth();

                            lbMeshItem = new LBMesh();

                            if (numRightVerts > 0 && startIdx < numRightVerts)
                            {
                                // TODO - these should probably consider meshEdgeSnapToTerrain
                                vertTRprev = splinePointsRight[startIdx];
                                vertTLprev = splinePointsLeft[startIdx];
                                vertBRprev = vertTRprev;
                                vertBRprev.y -= baseMeshThickness;
                                vertBLprev = vertTLprev;
                                vertBLprev.y -= baseMeshThickness;
                                if (isWideBase)
                                {
                                    vertITRprev = splinePointsRightInner[startIdx];
                                    vertITLprev = splinePointsLeftInner[startIdx];
                                }
                            }

                            for (int pt = startIdx; pt <= endPtIdx && pt < maxPoints; pt++)
                            {
                                if (isWideBase)
                                {
                                    vertITR = splinePointsRightInner[pt];
                                    vertITL = splinePointsLeftInner[pt];
                                }

                                #region Verts
                                vertTR = splinePointsRight[pt];
                                vertTL = splinePointsLeft[pt];
                                vertBR = vertTR;
                                vertBL = vertTL;
                                vertBR.y -= baseMeshThickness;
                                vertBL.y -= baseMeshThickness;

                                if (isFollowTerrain)
                                {
                                    if (!isGroupDesigner)
                                    {
                                        centrePt = splinePointsCentre[pt];
                                        // Get the terrain height, in landscape-space (world-space without the landscape.start.y offset)
                                        centreTerrainHeightLS = LBLandscapeTerrain.GetHeight(landscape, heightMap1D, heightmapResolution, new Vector2(centrePt.x, centrePt.z), true) * terrainHeight;

                                        // This is a really simple delta for each spline point - it should be lerped between user-defined path points
                                        centreTerrainHeightDeltaLS = centreTerrainHeightLS - anchorHeightLS;

                                        if (isWideBase)
                                        {
                                            vertITR.y += centreTerrainHeightDeltaLS;
                                            vertITL.y += centreTerrainHeightDeltaLS;
                                        }

                                        vertTR.y += centreTerrainHeightDeltaLS;
                                        vertTL.y += centreTerrainHeightDeltaLS;
                                        vertBR.y += centreTerrainHeightDeltaLS;
                                        vertBL.y += centreTerrainHeightDeltaLS;
                                    }
                                }
                                else if (meshEdgeSnapToTerrain)
                                {
                                    if (!isGroupDesigner)
                                    {
                                        // Find the terrain height at the left and right positions in the landscape.
                                        if (isWideBase)
                                        {
                                            // For a wide base we want to use the height of the terrain at the edge of the surface mesh, not the edge of the path
                                            rightEdgeTerrainHeight = (LBLandscapeTerrain.GetHeight(landscape, heightMap1D, heightmapResolution, new Vector2(vertITR.x, vertITR.z), true) * terrainHeight) + landscape.start.y;
                                            leftEdgeTerrainHeight = (LBLandscapeTerrain.GetHeight(landscape, heightMap1D, heightmapResolution, new Vector2(vertITL.x, vertITL.z), true) * terrainHeight) + landscape.start.y;
                                        }
                                        else
                                        {
                                            rightEdgeTerrainHeight = (LBLandscapeTerrain.GetHeight(landscape, heightMap1D, heightmapResolution, new Vector2(vertTR.x, vertTR.z), true) * terrainHeight) + landscape.start.y;
                                            leftEdgeTerrainHeight = (LBLandscapeTerrain.GetHeight(landscape, heightMap1D, heightmapResolution, new Vector2(vertTL.x, vertTL.z), true) * terrainHeight) + landscape.start.y;
                                        }
                                    }

                                    if (meshSnapType == MeshSnapType.BothEdges)
                                    {
                                        if (isWideBase)
                                        {
                                            vertITR.y = rightEdgeTerrainHeight;
                                            vertITL.y = leftEdgeTerrainHeight;
                                        }
                                        vertTR.y = rightEdgeTerrainHeight;
                                        vertTL.y = leftEdgeTerrainHeight;
                                        vertBR.y = rightEdgeTerrainHeight - baseMeshThickness;
                                        vertBL.y = leftEdgeTerrainHeight - baseMeshThickness;
                                    }
                                    else if (meshSnapType == MeshSnapType.MinLeftRightEdges)
                                    {
                                        minEdgeTerrainHeight = rightEdgeTerrainHeight < leftEdgeTerrainHeight ? rightEdgeTerrainHeight : leftEdgeTerrainHeight;
                                        if (isWideBase)
                                        {
                                            vertITR.y = minEdgeTerrainHeight;
                                            vertITL.y = minEdgeTerrainHeight;
                                        }
                                        vertTR.y = minEdgeTerrainHeight;
                                        vertTL.y = minEdgeTerrainHeight;
                                        vertBR.y = minEdgeTerrainHeight - baseMeshThickness;
                                        vertBL.y = minEdgeTerrainHeight - baseMeshThickness;
                                    }
                                    else if (meshSnapType == MeshSnapType.MaxLeftRightEdges)
                                    {
                                        minEdgeTerrainHeight = rightEdgeTerrainHeight > leftEdgeTerrainHeight ? rightEdgeTerrainHeight : leftEdgeTerrainHeight;
                                        if (isWideBase)
                                        {
                                            vertITR.y = minEdgeTerrainHeight;
                                            vertITL.y = minEdgeTerrainHeight;
                                        }
                                        vertTR.y = minEdgeTerrainHeight;
                                        vertTL.y = minEdgeTerrainHeight;
                                        vertBR.y = minEdgeTerrainHeight - baseMeshThickness;
                                        vertBL.y = minEdgeTerrainHeight - baseMeshThickness;
                                    }
                                    else // Average of left and right heights
                                    {
                                        avgEdgeTerrainHeight = (rightEdgeTerrainHeight + leftEdgeTerrainHeight) / 2f;
                                        if (isWideBase)
                                        {
                                            vertITR.y = avgEdgeTerrainHeight;
                                            vertITL.y = avgEdgeTerrainHeight;
                                        }
                                        vertTR.y = avgEdgeTerrainHeight;
                                        vertTL.y = avgEdgeTerrainHeight;
                                        vertBR.y = avgEdgeTerrainHeight - baseMeshThickness;
                                        vertBL.y = avgEdgeTerrainHeight - baseMeshThickness;
                                    }
                                }

                                // Right-top plane
                                if (isWideBase) { vertices.Add(vertITR); vertices.Add(vertTR); }

                                // Right-size plane
                                vertices.Add(vertTR);
                                vertices.Add(vertBR);

                                // Bottom plane Original
                                vertices.Add(vertBR);
                                vertices.Add(vertBL);

                                // Left-size plane
                                vertices.Add(vertBL);
                                vertices.Add(vertTL);

                                // Left-top plane
                                if (isWideBase) { vertices.Add(vertTL); vertices.Add(vertITL); }
                                #endregion

                                // Set default vert colour
                                for (int cIdx = 0; cIdx < numVertsPerPathPoint; cIdx++) { colours.Add(defaultVertColour); }

                                #region Triangles
                                if (pt > startIdx)
                                {
                                    // Verts are going anti-clockwise around triangle, starting in bottom rightmost corner of triangle

                                    // right top plane
                                    if (isWideBase)
                                    {
                                        // These need to be clockwise because it is facing upwards
                                        // Bottom (right) triangle (right top plane)
                                        triangles.Add(currentQuadBottomRightVert + 1);
                                        triangles.Add(currentQuadBottomRightVert);
                                        triangles.Add(currentQuadBottomRightVert + 11);

                                        // Top (left) triangle (right side plane)
                                        triangles.Add(currentQuadBottomRightVert);
                                        triangles.Add(currentQuadBottomRightVert + 10);
                                        triangles.Add(currentQuadBottomRightVert + 11);
                                    }

                                    // Bottom (right) triangle (right side plane)
                                    triangles.Add(currentQuadBottomRightVert + (isWideBase ? 2 : 0));
                                    triangles.Add(currentQuadBottomRightVert + (isWideBase ? 12 : 6));
                                    triangles.Add(currentQuadBottomRightVert + (isWideBase ? 3 : 1));

                                    // Top (left) triangle (right side plane)
                                    triangles.Add(currentQuadBottomRightVert + (isWideBase ? 3 : 1));
                                    triangles.Add(currentQuadBottomRightVert + (isWideBase ? 12 : 6));
                                    triangles.Add(currentQuadBottomRightVert + (isWideBase ? 13 : 7));

                                    // Bottom (right) triangle (bottom plane)
                                    triangles.Add(currentQuadBottomRightVert + (isWideBase ? 4 : 2));
                                    triangles.Add(currentQuadBottomRightVert + (isWideBase ? 14 : 7));
                                    triangles.Add(currentQuadBottomRightVert + (isWideBase ? 5 : 3));

                                    // Top (left) triangle (bottom plane)
                                    triangles.Add(currentQuadBottomRightVert + (isWideBase ? 5 : 3));
                                    triangles.Add(currentQuadBottomRightVert + (isWideBase ? 14 : 8));
                                    triangles.Add(currentQuadBottomRightVert + (isWideBase ? 15 : 9));

                                    // Bottom (right) triangle (left side plane)
                                    triangles.Add(currentQuadBottomRightVert + (isWideBase ? 6 : 4));
                                    triangles.Add(currentQuadBottomRightVert + (isWideBase ? 16 : 10));
                                    triangles.Add(currentQuadBottomRightVert + (isWideBase ? 7 : 5));

                                    // Top (left) triangle (left side plane)
                                    triangles.Add(currentQuadBottomRightVert + (isWideBase ? 7 : 5));
                                    triangles.Add(currentQuadBottomRightVert + (isWideBase ? 16 : 10));
                                    triangles.Add(currentQuadBottomRightVert + (isWideBase ? 17 : 11));

                                    // left top plane
                                    if (isWideBase)
                                    {
                                        // These need to be clockwise because it is facing upwards
                                        // Bottom (right) triangle (left top plane)
                                        triangles.Add(currentQuadBottomRightVert + 9);
                                        triangles.Add(currentQuadBottomRightVert + 8);
                                        triangles.Add(currentQuadBottomRightVert + 19);

                                        // Top (left) triangle (left top plane)
                                        triangles.Add(currentQuadBottomRightVert + 8);
                                        triangles.Add(currentQuadBottomRightVert + 18);
                                        triangles.Add(currentQuadBottomRightVert + 19);
                                    }

                                    if (meshIsDoubleSided)
                                    {
                                        // Verts are going clockwise around triangle, starting in bottom rightmost corner of triangle

                                        // right top plane
                                        if (isWideBase)
                                        {
                                            // These need to be anticlockwise because it is facing downwards
                                            // Bottom (right) triangle (right top plane)
                                            triangles.Add(currentQuadBottomRightVert + 1);
                                            triangles.Add(currentQuadBottomRightVert + 11);
                                            triangles.Add(currentQuadBottomRightVert);

                                            // Top (left) triangle (right side plane)
                                            triangles.Add(currentQuadBottomRightVert);
                                            triangles.Add(currentQuadBottomRightVert + 11);
                                            triangles.Add(currentQuadBottomRightVert + 10);
                                        }

                                        // Bottom (right) triangle (right side plane)
                                        triangles.Add(currentQuadBottomRightVert + (isWideBase ? 2 : 0));
                                        triangles.Add(currentQuadBottomRightVert + (isWideBase ? 3 : 1));
                                        triangles.Add(currentQuadBottomRightVert + (isWideBase ? 12 : 6));

                                        // Top (left) triangle (right side plane)
                                        triangles.Add(currentQuadBottomRightVert + (isWideBase ? 3 : 1));
                                        triangles.Add(currentQuadBottomRightVert + (isWideBase ? 13 : 7));
                                        triangles.Add(currentQuadBottomRightVert + (isWideBase ? 12 : 6));

                                        // Bottom (right) triangle (bottom plane)
                                        triangles.Add(currentQuadBottomRightVert + (isWideBase ? 4 : 2));
                                        triangles.Add(currentQuadBottomRightVert + (isWideBase ? 5 : 3));
                                        triangles.Add(currentQuadBottomRightVert + (isWideBase ? 14 : 7));

                                        // Top (left) triangle (bottom plane)
                                        triangles.Add(currentQuadBottomRightVert + (isWideBase ? 5 : 3));
                                        triangles.Add(currentQuadBottomRightVert + (isWideBase ? 15 : 9));
                                        triangles.Add(currentQuadBottomRightVert + (isWideBase ? 14 : 8));

                                        // Bottom (right) triangle (left side plane)
                                        triangles.Add(currentQuadBottomRightVert + (isWideBase ? 6 : 4));
                                        triangles.Add(currentQuadBottomRightVert + (isWideBase ? 7 : 5));
                                        triangles.Add(currentQuadBottomRightVert + (isWideBase ? 16 : 10));

                                        // Top (left) triangle (left side plane)
                                        triangles.Add(currentQuadBottomRightVert + (isWideBase ? 7 : 5));
                                        triangles.Add(currentQuadBottomRightVert + (isWideBase ? 17 : 11));
                                        triangles.Add(currentQuadBottomRightVert + (isWideBase ? 16 : 10));

                                        // left top plane
                                        if (isWideBase)
                                        {
                                            // These need to be anticlockwise because it is facing downwards
                                            // Bottom (right) triangle (left top plane)
                                            triangles.Add(currentQuadBottomRightVert + 9);
                                            triangles.Add(currentQuadBottomRightVert + 19);
                                            triangles.Add(currentQuadBottomRightVert + 8);

                                            // Top (left) triangle (left top plane)
                                            triangles.Add(currentQuadBottomRightVert + 8);
                                            triangles.Add(currentQuadBottomRightVert + 19);
                                            triangles.Add(currentQuadBottomRightVert + 18);
                                        }
                                    }

                                    // Used to calculate normals
                                    if (pt < numRightVerts - 1)
                                    {
                                        if (isWideBase)
                                        {
                                            vertITRnext = splinePointsRightInner[pt + 1];
                                            vertITLnext = splinePointsLeftInner[pt + 1];
                                        }
                                        vertTRnext = splinePointsRight[pt + 1];
                                        vertTLnext = splinePointsLeft[pt + 1];
                                        vertBRnext = vertTRnext;
                                        vertBLnext = vertTLnext;
                                        vertBRnext.y -= baseMeshThickness;
                                        vertBLnext.y -= baseMeshThickness;
                                    }
                                    else
                                    {
                                        // Last two path points
                                        if (isWideBase)
                                        {
                                            vertITRnext = vertITR;
                                            vertITLnext = vertITL;
                                        }
                                        vertTRnext = vertTR;
                                        vertTLnext = vertTL;
                                        vertBRnext = vertBR;
                                        vertBLnext = vertBL;
                                    }
                                }
                                #endregion

                                #region Normals
                                if (isWideBase)
                                {
                                    // Add the normals for the Right-top plane
                                    normals.Add(-Vector3.Cross(vertTRnext - vertTRprev, vertITR - vertTR).normalized);
                                    normals.Add(Vector3.Cross(vertITRnext - vertITRprev, vertTR - vertITR).normalized);
                                }

                                // Add the normals for the Right-side plane
                                normals.Add(Vector3.Cross(vertTRnext - vertTRprev, vertBR - vertTR).normalized);
                                normals.Add(-Vector3.Cross(vertBRnext - vertBRprev, vertBL - vertBR).normalized);

                                // Add the normals for the new verts - Bottom plane (original)
                                normals.Add(Vector3.Cross(vertBRnext - vertBRprev, vertBL - vertBR).normalized);
                                normals.Add(-Vector3.Cross(vertBLnext - vertBLprev, vertBR - vertBL).normalized);

                                // Add the normals for the Left-side plane
                                normals.Add(Vector3.Cross(vertBLnext - vertBLprev, vertBR - vertBL).normalized);
                                normals.Add(-Vector3.Cross(vertTLnext - vertTLprev, vertBL - vertTL).normalized);

                                if (isWideBase)
                                {
                                    // Add the normals for the Left-top plane
                                    normals.Add(Vector3.Cross(vertTLnext - vertTLprev, vertITL - vertTL).normalized);
                                    normals.Add(-Vector3.Cross(vertITLnext - vertITLprev, vertTL - vertITL).normalized);
                                }
                                #endregion

                                #region Tangents
                                // Calculate tangents - Right-top plane
                                if (isWideBase)
                                {
                                    vITRTR = (vertITR - vertTR).normalized;
                                    tangent = new Vector4(vITRTR.x, vITRTR.y, vITRTR.z, 1f);
                                    tangents.Add(tangent);
                                    tangents.Add(tangent);
                                }

                                // Calculate tangents - Right-side plane
                                vTRBR = (vertTR - vertBR).normalized;
                                tangent = new Vector4(vTRBR.x, vTRBR.y, vTRBR.z, 1f);
                                tangents.Add(tangent);
                                tangents.Add(tangent);

                                // Calculate tangents - Bottom plane (original)
                                vBRBL = (vertBL - vertBR).normalized;
                                tangent = new Vector4(vBRBL.x, vBRBL.y, vBRBL.z, 1f);
                                tangents.Add(tangent);
                                tangents.Add(tangent);

                                // Calculate tangents - Left-side plane
                                vTLBL = (vertTL - vertBL).normalized;
                                tangent = new Vector4(vTLBL.x, vTLBL.y, vTLBL.z, 1f);
                                tangents.Add(tangent);
                                tangents.Add(tangent);

                                // Calculate tangents - Left-top plane
                                if (isWideBase)
                                {
                                    vTLITL = (vertTL - vertITL).normalized;
                                    tangent = new Vector4(vTLITL.x, vTLITL.y, vTLITL.z, 1f);
                                    tangents.Add(tangent);
                                    tangents.Add(tangent);
                                }
                                #endregion

                                #region Create UVs
                                if (isMeshLandscapeUV)
                                {
                                    // The UVs are relative to the vert position in the landscape. Useful for things like rivers
                                    // where the water asset usually covers the whole landscape

                                    // Right-side plane
                                    vertTRN = vertTR - landscapeWorldPosition;

                                    // Bottom plane
                                    vertBRN = vertBR - landscapeWorldPosition;
                                    vertBLN = vertBL - landscapeWorldPosition;

                                    // Left-side plane
                                    vertTLN = vertTL - landscapeWorldPosition;

                                    // Verts normalised to landscape

                                    // Right-side plane
                                    ptTRN = new Vector2(vertTRN.x / landscape.size.x * baseMeshUVTileScale.x, vertTRN.z / landscape.size.y * baseMeshUVTileScale.y);

                                    // Bottom plane (original)
                                    ptBRN = new Vector2(vertBRN.x / landscape.size.x * baseMeshUVTileScale.x, vertBRN.z / landscape.size.y * baseMeshUVTileScale.y);
                                    ptBLN = new Vector2(vertBLN.x / landscape.size.x * baseMeshUVTileScale.x, vertBLN.z / landscape.size.y * baseMeshUVTileScale.y);

                                    // Left-side plane
                                    ptTLN = new Vector2(vertTLN.x / landscape.size.x * baseMeshUVTileScale.x, vertTLN.z / landscape.size.y * baseMeshUVTileScale.y);

                                    if (isWideBase)
                                    {
                                        // Right-top plane
                                        vertITRN = vertITR - landscapeWorldPosition;
                                        // Right-top plane
                                        ptITRN = new Vector2(vertITRN.x / landscape.size.x * baseMeshUVTileScale.x, vertITRN.z / landscape.size.y * baseMeshUVTileScale.y);
                                        // Left-top plane
                                        vertITLN = vertITL - landscapeWorldPosition;
                                        // Left-top plane
                                        ptITLN = new Vector2(vertITLN.x / landscape.size.x * baseMeshUVTileScale.x, vertITLN.z / landscape.size.y * baseMeshUVTileScale.y);
                                    }

                                    // Add UVs in same order as verts
                                    // u is in direction of the path, v is left to right across the path

                                    // Right-top plane
                                    if (isWideBase)
                                    {
                                        uvs.Add(ptITRN);
                                        uvs.Add(ptTRN);
                                    }

                                    // Right-side plane
                                    uvs.Add(ptTRN);
                                    uvs.Add(ptBRN);

                                    // Bottom plane (original)
                                    uvs.Add(ptBRN);
                                    uvs.Add(ptBLN);

                                    // Left-side plane
                                    uvs.Add(ptBLN);
                                    uvs.Add(ptTLN);

                                    // Right-left plane
                                    if (isWideBase)
                                    {
                                        uvs.Add(ptTLN);
                                        uvs.Add(ptITLN);
                                    }
                                }
                                else
                                {
                                    float splineDistPtNormalised = cachedCentreSplinePointDistancesList[pt] / meshLength;

                                    // Muliply by the uvRepeat factor which assumes the texture covers the full width
                                    // of the path and that the texture being used is set to Repeat not Clamp.
                                    splineDistPtNormalised *= uvRepeat;
                                    splineDistPtNormalised *= baseMeshUVTileScale.y;

                                    // Add UVs in same order as verts
                                    // u is in direction of the path, v is left to right across the path

                                    // Right-top plane
                                    if (isWideBase)
                                    {
                                        // TODO - NEEDS FIXING - Top-right plane
                                        if (isSwitchBaseMeshUVs)
                                        {
                                            uvs.Add(new Vector2(splineDistPtNormalised, 0f));
                                            uvs.Add(new Vector2(splineDistPtNormalised, baseMeshUVTileScale.x));
                                        }
                                        else
                                        {
                                            uvs.Add(new Vector2(0f, splineDistPtNormalised));
                                            uvs.Add(new Vector2(baseMeshUVTileScale.x, splineDistPtNormalised));
                                        }
                                    }

                                    if (isSwitchBaseMeshUVs)
                                    {
                                        // Right-side plane
                                        uvs.Add(new Vector2(splineDistPtNormalised, baseMeshUVTileScale.x));
                                        uvs.Add(new Vector2(splineDistPtNormalised, 0f));

                                        // Bottom plane (original)
                                        uvs.Add(new Vector2(splineDistPtNormalised, 0f));
                                        uvs.Add(new Vector2(splineDistPtNormalised, baseMeshUVTileScale.x));

                                        // Left-side plane
                                        uvs.Add(new Vector2(splineDistPtNormalised, 0f));
                                        uvs.Add(new Vector2(splineDistPtNormalised, baseMeshUVTileScale.x));
                                    }
                                    else
                                    {
                                        // Right-side plane
                                        uvs.Add(new Vector2(baseMeshUVTileScale.x, splineDistPtNormalised));
                                        uvs.Add(new Vector2(0f, splineDistPtNormalised));

                                        // Bottom plane (original)
                                        uvs.Add(new Vector2(0f, splineDistPtNormalised));
                                        uvs.Add(new Vector2(baseMeshUVTileScale.x, splineDistPtNormalised));

                                        // Left-side plane
                                        uvs.Add(new Vector2(0f, splineDistPtNormalised));
                                        uvs.Add(new Vector2(baseMeshUVTileScale.x, splineDistPtNormalised));
                                    }

                                    // Left-top plane
                                    if (isWideBase)
                                    {
                                        // TODO - NEEDS FIXING - Top-left plane
                                        if (isSwitchBaseMeshUVs)
                                        {
                                            uvs.Add(new Vector2(splineDistPtNormalised, 0f));
                                            uvs.Add(new Vector2(splineDistPtNormalised, baseMeshUVTileScale.x));
                                        }
                                        else
                                        {
                                            uvs.Add(new Vector2(0f, splineDistPtNormalised));
                                            uvs.Add(new Vector2(baseMeshUVTileScale.x, splineDistPtNormalised));
                                        }
                                    }
                                }

                                #endregion

                                if (isWideBase)
                                {
                                    vertITRprev = vertITR;
                                    vertITLprev = vertITL;
                                }
                                vertTRprev = vertTR;
                                vertBRprev = vertBR;
                                vertBLprev = vertBL;
                                vertTLprev = vertTL;

                                if (pt > startIdx) { currentQuadBottomRightVert += numVertsPerPathPoint; }
                            }

                            // As a workaround for Clearing and SubGroups, get Unity to calculate normals in LBMesh.UpdateMesh
                            if (!addNormals && normals != null) { normals.Clear(); }

                            int numNormals = (normals == null ? 0 : normals.Count);

                            #region Adjust Start Normals
                            if (numNormals > 1)
                            {
                                // Set starting normals
                                if (isWideBase)
                                {
                                    // Top right plane
                                    normals[0] = Vector3.up;
                                    normals[1] = Vector3.up;
                                    // Top left plane
                                    normals[8] = Vector3.up;
                                    normals[9] = Vector3.up;
                                }

                                // right plane
                                normals[0 + wideBaseVertIdxOffset] = Vector3.right;
                                normals[1 + wideBaseVertIdxOffset] = Vector3.right;

                                // bottom plane - original 0,1 normals
                                normals[2 + wideBaseVertIdxOffset] = Vector3.up;
                                normals[3 + wideBaseVertIdxOffset] = Vector3.up;

                                // left plane
                                normals[4 + wideBaseVertIdxOffset] = Vector3.left;
                                normals[5 + wideBaseVertIdxOffset] = Vector3.left;
                            }
                            #endregion

                            lbMeshItem.verts = vertices;
                            lbMeshItem.triangles = triangles;
                            lbMeshItem.normals = normals;
                            lbMeshItem.uvs = uvs;
                            //lbMeshItem.uv2s = new List<Vector2>(uvs);
                            lbMeshItem.tangents = tangents;

                            baseMeshList.Add(lbMeshItem);
                        }

                        isSuccess = true;

                        for (int mshIdx = 0; mshIdx < numMeshes && isSuccess; mshIdx++)
                        {
                            isSuccess = false;
                            lbMeshItem = baseMeshList[mshIdx];

                            if (lbMeshItem.mesh == null) { lbMeshItem.mesh = new Mesh(); }

                            if (lbMeshItem.mesh == null) { Debug.LogWarning("ERROR: LBObjPath.CreateBaseMesh - could not create new Mesh"); }
                            else
                            {
                                lbMeshItem.mesh.name = pathName + "." + regionNumber + "base" + mshIdx;

                                // Assign verts, triangle to new Unity Mesh and recalc bounds and normals
                                isSuccess = lbMeshItem.UpdateMesh(false, true);

                                //Debug.Log("[DEBUG]: LBObjPath.CreateBaseMesh - updated mesh " + lbMeshItem.mesh.name + " " + System.DateTime.Now);
                            }
                        }
                    }
                }
            }

            return isSuccess;
        }

        #endregion

        #region Private Member Methods

        private void SetClassDefaults()
        {
            pathType = PathType.ObjPath;
            pathResolution = 2f;
            heightAboveTerrain = 2f;
            pointDisplayScale = 1f;
            pathPointList = new List<LBPathPoint>();

            showSceneViewSettings = true;
            showObjectSettings = false;
            showDefaultSeriesInEditor = true;
            showSeriesListInEditor = false;
            showSurroundingInScene = false;

            useWidth = false;
            useSurfaceMesh = false;
            surfaceMeshMaterial = null;
            isCreateSurfaceMeshCollider = false;
            surroundBlendCurve = GetDefaultBlendCurve;
            profileHeightCurve = GetDefaultProfileHeightCurve;
            profileHeightCurvePreset = LBCurve.ObjPathHeightCurvePreset.Flat;

            surroundSmoothing = 0.0f;   // Off by default
            addTerrainHeight = 0f;
            isSwitchMeshUVs = false;
            isSwitchBaseMeshUVs = false;
            baseMeshThickness = 0f;     // Off by default
            baseMeshList = null;
            baseMeshMaterial = null;
            baseMeshUVTileScale = new Vector2(1f, 1f);
            baseMeshUseIndent = false;
            isCreateBaseMeshCollider = false;

            coreTextureGUID = string.Empty;
            coreTextureStrength = 1f;
            coreTextureNoiseTileSize = 0f;
            surroundTextureGUID = string.Empty;
            surroundTextureStrength = 1f;
            surroundTextureNoiseTileSize = 0f;

            isRemoveExistingGrass = true;
            isRemoveExistingTrees = false;  // off by default to avoid issues with people not using the Trees tab for tree placement
            treeDistFromEdge = 0f;

            // Set 0.5m edge for surface mesh indent
            leftBorderWidth = 0.5f;
            rightBorderWidth = 0.5f;

            // Prefab varibles
            mainObjPrefabList = new List<LBObjPrefab>();
            startObjPrefab = new LBObjPrefab();
            endObjPrefab = new LBObjPrefab();

            layoutMethod = LayoutMethod.Spacing;
            selectionMethod = SelectionMethod.Alternating;
            isLastObjSnappedToEnd = true;
            spacingDistance = 10f;
            maxMainPrefabs = 5;

            // Default to false for backward compatibility
            isRandomisePerGroupRegion = false;

            // Series variables
            lbObjPathSeriesList = new List<LBObjPathSeries>();
            isSeriesListOverride = false;
            seriesListGroupMemberGUID = string.Empty;

            // Biome variables
            useBiomes = false;
            lbBiomeList = null;
        }

        #endregion

        #region Public Static Methods

        #endregion

        #region Public EDITOR only Methods
        #if UNITY_EDITOR

        /// <summary>
        /// Change the user-defined position points, to a set distance apart (positionSpacingTarget).
        /// If useWidth is enabled, this will also create new left/right splines.
        /// Currently does not preserve Rotation
        /// Currently resets all widths to min. width of path.
        /// EDITOR-ONLY
        /// </summary>
        public void ChangePositionSpacing()
        {
            isSplinesCached2 = false;
            CacheSplinePoints2();

            List<Vector3> newPosList = this.GetPositionList(positionSpacingTarget, true, true);

            int numNewPositionPoints = (newPosList == null ? 0 : newPosList.Count);
            int numPositionPoints = (positionList == null ? 0 : positionList.Count);

            if (numNewPositionPoints > 1)
            {
                //Debug.Log("[DEBUG] now " + numNewPositionPoints);

                bool isUpdatePath = true;
                if (numNewPositionPoints < numPositionPoints)
                {
                    isUpdatePath = UnityEditor.EditorUtility.DisplayDialog("WARNING", "The new object path will have less points and may cut corners. There is no UNDO feature. Do you wish to continue?", "YES CHANGE OBJECT PATH", "CANCEL");
                }

                if (isUpdatePath)
                {
                    //List<float> newWidthList;
                    float minPathWidth = 0;

                    // Use to help find the width
                    //List<float> oldcachedPathPointDistanceList;
                    //List<float> oldwidthList;
                    //float oldTotalDistance = splineLength;

                    if (useWidth)
                    {
                        //newWidthList = new List<float>(numNewPositionPoints);
                        minPathWidth = GetMinWidth();

                        // Get a copy of the widths and distances at each user-defined postion point
                        // With floats, we don't need to do a deep copy
                        //oldwidthList = new List<float>(widthList);
                        //oldcachedPathPointDistanceList = new List<float>(cachedPathPointDistanceList);

                        widthList.Clear();
                        positionListLeftEdge.Clear();
                        positionListRightEdge.Clear();
                    }
                    positionList.Clear();
                    pathPointList.Clear();

                    foreach (Vector3 pos in newPosList)
                    {
                        positionList.Add(pos);
                        pathPointList.Add(new LBPathPoint());

                        // Get the width at a certain percentage along the new spline

                        if (useWidth)
                        {
                            // Add a default value
                            widthList.Add(minPathWidth);

                            //Add some dummy values
                            positionListLeftEdge.Add(pos);
                            positionListRightEdge.Add(pos);
                        }
                    }

                    isSplinesCached2 = false;
                    RefreshObjPathPositions(showSurroundingInScene, false);
                }
            }
        }

        /// <summary>
        /// Enable (or disable) the useWidth feature
        /// EDITOR ONLY as not required at runtime
        /// </summary>
        /// <param name="isEnabled"></param>
        public void EnablePathWidth(bool isEnabled, bool showErrors)
        {
            int numPathPoints = (pathPointList == null ? 0 : pathPointList.Count);
            int numPositionPoints = (positionList == null ? 0 : positionList.Count);

            if (isEnabled)
            {
                // Basic validation
                if (numPathPoints != numPositionPoints) { if (showErrors) { Debug.LogWarning("LBObjPath.EnablePathWidth positions do not match path points. Please Report"); } }
                else
                {
                    int listCapacity = numPathPoints > 0 ? numPathPoints : 20;

                    // Create or clear width-related lists
                    if (widthList == null) { widthList = new List<float>(listCapacity); } else { widthList.Clear(); }
                    if (positionListLeftEdge == null) { positionListLeftEdge = new List<Vector3>(listCapacity); } else { positionListLeftEdge.Clear(); }
                    if (positionListRightEdge == null) { positionListRightEdge = new List<Vector3>(listCapacity); } else { positionListRightEdge.Clear(); }

                    if (widthList != null)
                    {
                        for (int pt = 0; pt < numPositionPoints; pt++)
                        {
                            widthList.Add(LBPath.GetDefaultPathWidth);

                            // Add left/right edges are same location. They will be correctly positioned in RefreshPathEdgePositions().
                            positionListLeftEdge.Add(Vector3.zero);
                            positionListRightEdge.Add(Vector3.zero);

                            // TODO Add other left/right positions here
                        }

                        if (numPathPoints > 0)
                        {
                            // Force spline cache refresh
                            isSplinesCached2 = false;
                            RefreshObjPathPositions(showSurroundingInScene,false);
                        }

                        // After refreshing for the first time after enabling useWidth, turn off snap to terrain
                        snapToTerrain = false;
                    }
                }
            }
            else
            {
                // Cleanup old data when useWidth was previously enabled
                if (widthList != null) { widthList.Clear(); }
                if (positionListLeftEdge != null) { positionListLeftEdge.Clear(); }
                if (positionListRightEdge != null) { positionListRightEdge.Clear(); }

                // snap to terrain is always enabled when useWidth = false
                snapToTerrain = true;
                isGetPointWidthMode = false;

                // Mesh is only available if useWidth is enabled
                useSurfaceMesh = false;

                // TODO Cleanup other left/right positions here

                // Force spline cache refresh
                isSplinesCached2 = false;
                RefreshObjPathPositions(false, false);
            }
            UnityEditor.SceneView.RepaintAll();
        }

        #endif
        #endregion
    }
}