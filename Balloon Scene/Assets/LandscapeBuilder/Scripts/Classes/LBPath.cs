// Landscape Builder. Copyright (c) 2016-2019 SCSM Pty Ltd. All rights reserved.
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace LandscapeBuilder
{
    [System.Serializable]
    public class LBPath
    {
        // LBPath is a serializable list of points that helps create a path through the landscape
        // that can be used for with LBCameraPath, LBMapPath or LBObjPath (new in LB 2.0.4)
        // New in v1.3.2

        #region Enumerations

        public enum PathType
        {
            CameraPath = 10,
            MapPath = 20,
            ObjPath = 30
        }

        public enum PositionType
        {
            Centre = 0,
            Left = 1,
            Right = 2
        }

        public enum MeshSnapType
        {
            BothEdges = 5,
            AvgLeftRightEdges = 10,
            MinLeftRightEdges = 20,
            MaxLeftRightEdges = 21
        }

        #endregion

        #region Variables and Properties

        public string pathName;
        public PathType pathType;
        public bool closedCircuit = false;
        public bool showPathInScene = true;
        public bool showDistancesInScene = false;
        public bool showPointLabelsInScene = false;
        public bool snapToTerrain = true;       // Should new points be snapped to the terrain height? (plus the heightAboveTerrain)
        public float heightAboveTerrain = 5f;   // How high above the terrain should we place each (new) point
        public float pointDisplayScale = 1f;    // The amount to scale the point gizmos in the scene view 
        public float pathLength;                // Length of the path in metres using the (fewer) path points - use this with cachedPathPointDistances
        public float pathLengthToLastPoint;     // Length or distance to the Last Point in metres using the (fewer) path points - use with cachedPathPointDistances
        public float splineLength;              // Length of the spline in metres using the spline points on the path (more accurate than pathLength) - use with cachedCentreSplinePointDistances
        public float splineLengthToLastPoint;   // Length or distance to the Last Point in metres using the spline points (more accurate than pathLengthToLastPoint)
        public float splineDistTo2ndLastPoint;  // Length of distance from start to the second last point in metres (used for closed circuits)
        public float pathResolution;            // This is the segment distance - the distance between cached path points
        public float edgeBlendWidth;            // This is the width of the edge that will blend with the surroundings (applies to all points)
        public bool blendStart = true;
        public bool blendEnd = true;
        public bool removeCentre = false;       // When exporting a map, should we remove the centre of the path based on the border width?
        public float leftBorderWidth = 0f;      // The left border width to write to a Map texture when removeCentre = true. For a LBObjPath, this is the left indent of the (mesh) surface.
        public float rightBorderWidth = 0f;     // The right border width to write to a Map texture when removeCentre = true. For a LBObjPath, this is the right indent of the (mesh) surface.
        public bool isMeshLandscapeUV = false;  // Are the mesh UVs calculcated based on the vert positions in the landscape?
        public Vector2 meshUVTileScale;         // The tiling of the UVs when the mesh is created.
        public float meshYOffset;               // The position Y offset between the parent gameobject and the child mesh gameobject
        public bool meshEdgeSnapToTerrain;      // When creating a mesh, should the edges be snapped to the height of the terrain?
        public bool meshIncludeEdges;           // Include the blend edges when creating the mesh
        public bool meshIncludeWater;           // Add water to the mesh that is created
        public MeshSnapType meshSnapType;       // Determine how meshes are snapped to the terrain height (requires meshEdgeSnapToTerrain = true)
        public bool meshIsDoubleSided;          // Added v1.3.6 Beta 1e
        public bool meshSaveToProjectFolder;    // Added v1.3.6 Beta 2a - Create a Mesh in the Asset database (Project - Assets/LandscapeBuilder/Meshes)
        public Material meshTempMaterial;       // Added v1.4.0 Beta 4g - Used only when saving/restoring to/from an LBTemplate
        public bool showAdvancedOptions = false;
        public Vector4 pathPointColour;         // A colour can be stored as a vector4.
        public float positionSpacingTarget;     // The target distance between points in the positionList. Used to redistribute the positions along the path.

        // Currently not required - but keep in just in case we use later
        [Range(0, 25)] public int curveDetectionInner = 3;      // Use with LBMap.CreateMapFromSplines() to determine number of quads to look ahead for pixel matching
        [Range(0, 25)] public int curveDetectionOuter = 3;      // Use with LBMap.CreateMapFromSplines() to determine number of quads to look ahead for pixel matching

        // These are the points along the path which can be modified in the scene view with handles by the use
        // For Clearing Groups, they are stored in Group-Space.
        public List<Vector3> positionList;
        public List<Vector3> positionListLeftEdge;
        public List<Vector3> positionListRightEdge;
        public List<Vector3> rotationList;
        public List<float> widthList;
        public List<int> selectedList;

        public LBMesh lbMesh;
        public LBWater lbWater;

        public float findZoomDistance;
        public bool zoomOnFind;                 // Move path point into sceneview and zoom in to required distance

        // Defaults
        public static float GetDefaultPathWidth { get { return 10f; } }
        public static AnimationCurve GetDefaultBlendCurve { get { return AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); } }

        // CACHE NOTES
        // There are two caching systems. The original version uses mostly cached arrays. Although these are fast,
        // the problem is they lead to memory fragmentation. The original caching uses an older path forward and distance methods.
        // The second (newer) caching system attempts to use lists where possible so memory gets reused. It uses the newer
        // GetPathPosition (float distance, int algorithm = 0) method.
        // Update Cache System1: CachePathPointDistances(); CacheSplinePointDistances();
        // - Arrays: 
        // Update Cache System2: CacheSplinePoints2()
        // - Arrays: cachedPathPoints, cachedPathPointDistances
        // - Arrays: cachedPathPointsLeftEdge, cachedPathPointsRightEdge
        // - Lists: cachedPathPointDistanceList, cachedCentreSplinePointList, cachedCentreSplinePointDistancesList
        // - LBObjPath.cs also can cache cachedSplinePointLeftEdgeList, cachedSplinePointRightEdgeList

        /// <summary>
        /// Is the cached spline data up-to-date? Used by CacheSplinePoints2() to determine
        /// if the cached data needs to be refreshed.
        /// </summary>
        [System.NonSerialized] public bool isSplinesCached2 = false;

        // These are similar to positionLists but may include a final point if the circuit is a closed loop
        [System.NonSerialized] public Vector3[] cachedPathPoints;
        [System.NonSerialized] public int cachedPathPointsLength = 0;                   // The number of items in the array - for length in metres, see pathLength
        [System.NonSerialized] public Vector3[] cachedPathPointsLeftEdge;
        [System.NonSerialized] public Vector3[] cachedPathPointsRightEdge;
        [System.NonSerialized] public float[] cachedPathPointDistances;
        [System.NonSerialized] public int cachedPathPointDistancesLength = 0;           // The number of items in the array
        [System.NonSerialized] public List<float> cachedPathPointDistanceList;          // The list may be used for distance searches.

        // These are the spline points used for the path segments which are pathResolution distance apart
        [System.NonSerialized] public Vector3[] cachedCentreSplinePoints;
        [System.NonSerialized] public List<Vector3> cachedCentreSplinePointList;        // Used with LBObjPath
        [System.NonSerialized] public int cachedCentreSplinePointsLength;               // Number of items in the array - for the length in metres, see splineLength
        [System.NonSerialized] public float[] cachedCentreSplinePointDistances;
        [System.NonSerialized] public int cachedCentreSplinePointDistancesLength = 0;   // number of items in the array
        [System.NonSerialized] public List<float> cachedCentreSplinePointDistancesList; // The list may be used for distance searches

        // Temporary variables used for catmull-rom equation
        private Vector3 point1;
        private Vector3 point2;
        private Vector3 point3;
        private Vector3 point4;
        private float inverseLerpPos;

        [System.NonSerialized] public bool isRefreshing = false;

        #endregion

        #region Constructors

        // Class constructors
        public LBPath()
        {
            positionList = new List<Vector3>();
            positionListLeftEdge = new List<Vector3>();
            positionListRightEdge = new List<Vector3>();
            rotationList = new List<Vector3>();
            widthList = new List<float>();
            selectedList = new List<int>();
            pathType = PathType.CameraPath;
            findZoomDistance = 50f;
            zoomOnFind = true;
            snapToTerrain = true;
            heightAboveTerrain = 5f;
            pointDisplayScale = 1f;
            pathResolution = 20f;
            edgeBlendWidth = 5f;
            closedCircuit = false;
            pathLength = 0f;
            pathLengthToLastPoint = 0f;
            splineLength = 0f;
            splineLengthToLastPoint = 0f;
            splineDistTo2ndLastPoint = 0f;
            blendStart = true;
            blendEnd = true;
            showAdvancedOptions = false;
            // slightly transparent red
            pathPointColour = new Color(1f, 0f, 0f, 0.75f);
            positionSpacingTarget = 20f;
            curveDetectionInner = 3;
            curveDetectionOuter = 3;
            removeCentre = false;
            leftBorderWidth = 0f;
            rightBorderWidth = 0f;
            isMeshLandscapeUV = false;
            meshUVTileScale = Vector2.one;
            meshYOffset = 0f;
            meshEdgeSnapToTerrain = false;
            meshIncludeEdges = true;
            meshIncludeWater = false;
            meshSnapType = MeshSnapType.BothEdges;
            meshIsDoubleSided = false;
            meshSaveToProjectFolder = false;
            meshTempMaterial = null;
            lbMesh = null;
        }

        public LBPath(PathType pathTypeToCreate)
        {
            positionList = new List<Vector3>();
            positionListLeftEdge = new List<Vector3>();
            positionListRightEdge = new List<Vector3>();
            rotationList = new List<Vector3>();
            widthList = new List<float>();
            selectedList = new List<int>();
            pathType = pathTypeToCreate;
            findZoomDistance = 50f;
            snapToTerrain = true;
            heightAboveTerrain = 5f;
            pointDisplayScale = 1f;
            zoomOnFind = true;
            if (pathTypeToCreate == PathType.MapPath) { pathResolution = 5f; }
            else { pathResolution = 20f; }
            edgeBlendWidth = 5f;
            closedCircuit = false;
            pathLength = 0f;
            pathLengthToLastPoint = 0f;
            splineLength = 0f;
            splineLengthToLastPoint = 0f;
            splineDistTo2ndLastPoint = 0f;
            blendStart = true;
            blendEnd = true;
            showAdvancedOptions = false;
            // slightly transparent red
            pathPointColour = new Color(1f, 0f, 0f, 0.75f);
            positionSpacingTarget = 20f;
            curveDetectionInner = 3;
            curveDetectionOuter = 3;
            removeCentre = false;
            leftBorderWidth = 0f;
            rightBorderWidth = 0f;
            isMeshLandscapeUV = false;
            meshUVTileScale = Vector2.one;
            meshYOffset = 0f;
            meshEdgeSnapToTerrain = false;
            meshIncludeEdges = true;
            meshIncludeWater = false;
            meshSnapType = MeshSnapType.BothEdges;
            meshIsDoubleSided = false;
            meshSaveToProjectFolder = false;
            meshTempMaterial = null;
            lbMesh = null;
        }

        // Constructor to create clone copy
        // When updating, also update LBObjPath(LBObjPath lbObjPath).
        public LBPath(LBPath lbPath)
        {
            pathName = lbPath.pathName;
            pathType = lbPath.pathType;
            closedCircuit = lbPath.closedCircuit;
            showPathInScene = lbPath.showPathInScene;
            showDistancesInScene = lbPath.showDistancesInScene;
            showPointLabelsInScene = lbPath.showPointLabelsInScene;
            pathLength = lbPath.pathLength;
            pathLengthToLastPoint = lbPath.pathLengthToLastPoint;
            splineLength = lbPath.splineLength;
            splineLengthToLastPoint = lbPath.splineLengthToLastPoint;
            splineDistTo2ndLastPoint = lbPath.splineDistTo2ndLastPoint;
            findZoomDistance = lbPath.findZoomDistance;
            zoomOnFind = lbPath.zoomOnFind;
            snapToTerrain = lbPath.snapToTerrain;
            heightAboveTerrain = lbPath.heightAboveTerrain;
            pointDisplayScale = lbPath.pointDisplayScale;
            pathResolution = lbPath.pathResolution;
            edgeBlendWidth = lbPath.edgeBlendWidth;
            blendStart = lbPath.blendStart;
            blendEnd = lbPath.blendEnd;
            removeCentre = lbPath.removeCentre;
            if (lbPath.pathPointColour == Vector4.zero) { pathPointColour = UnityEngine.Color.red; }
            else { pathPointColour = lbPath.pathPointColour; }
            positionSpacingTarget = lbPath.positionSpacingTarget;
            leftBorderWidth = lbPath.leftBorderWidth;
            rightBorderWidth = lbPath.rightBorderWidth;
            isMeshLandscapeUV = lbPath.isMeshLandscapeUV;
            meshUVTileScale = lbPath.meshUVTileScale;
            meshYOffset = lbPath.meshYOffset;
            meshEdgeSnapToTerrain = lbPath.meshEdgeSnapToTerrain;
            meshIncludeEdges = lbPath.meshIncludeEdges;
            meshIncludeWater = lbPath.meshIncludeWater;
            meshSnapType = lbPath.meshSnapType;
            meshIsDoubleSided = lbPath.meshIsDoubleSided;
            meshSaveToProjectFolder = lbPath.meshSaveToProjectFolder;
            if (meshTempMaterial != null) { meshTempMaterial = new Material(lbPath.meshTempMaterial); } else { meshTempMaterial = null; }
            showAdvancedOptions = lbPath.showAdvancedOptions;
            curveDetectionInner = lbPath.curveDetectionInner;
            curveDetectionOuter = lbPath.curveDetectionOuter;

            this.positionList = new List<Vector3>(lbPath.positionList);
            this.positionListLeftEdge = new List<Vector3>(lbPath.positionListLeftEdge);
            this.positionListRightEdge = new List<Vector3>(lbPath.positionListRightEdge);
            this.rotationList = new List<Vector3>(lbPath.rotationList);
            this.widthList = new List<float>(lbPath.widthList);
            this.selectedList = new List<int>(lbPath.selectedList);

            if (lbPath.lbMesh != null) { lbMesh = new LBMesh(lbPath.lbMesh); } else { lbMesh = null; }
            if (lbPath.lbWater != null) { lbWater = new LBWater(lbPath.lbWater, true); } else { lbWater = null; }
        }

        #endregion

        #region Public Methods

        #region Map Texture Methods

        /// <summary>
        /// Create an image "Map" from the current Path.
        /// Currently only supports MapPath
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="mapResolution"></param>
        /// <returns></returns>
        public Texture2D CreateMapFromPath(LBLandscape landscape, int mapResolution)
        {
            if (landscape != null && pathType == PathType.MapPath)
            {
                float tStart = Time.realtimeSinceStartup;

                Texture2D mapTexture = LBTextureOperations.CreateTexture(mapResolution, mapResolution, Color.clear);
                if (mapTexture != null)
                {
                    LBMap lbMap = new LBMap(mapTexture, Color.white, 0);
                    if (lbMap != null)
                    {
                        lbMap.map.name = pathName;

                        if (landscape.IsGPUAccelerationAvailable() && landscape.useGPUPath)
                        {
                            lbMap.CreateMapFromPathCompute(landscape, this);
                        }
                        else
                        {
                            lbMap.CreateMapFromPath(landscape, this, curveDetectionInner, curveDetectionOuter);
                        }

                        if (landscape.showTiming) { Debug.Log("LBPath.CreateMapFromPath - created Map from splines in " + (Time.realtimeSinceStartup - tStart).ToString("0.000") + " seconds"); }

                        mapTexture = lbMap.map;
                    }
                }
                return mapTexture;
            }
            else { return null; }
        }

        #endregion

        #region Path Mesh Methods

        /// <summary>
        /// Create a mesh from a path.
        /// WARNING: Currently doesn't support > 64K verts
        /// </summary>
        /// <param name="landscape"></param>
        /// <returns></returns>
        public bool CreateMeshFromPath(LBLandscape landscape)
        {
            bool isSuccess = false;

            #if UNITY_EDITOR
            float tStart = Time.realtimeSinceStartup;
            #endif
            lbMesh = new LBMesh();

            if (landscape == null) { Debug.LogWarning("CreateMeshFromPath - landscape cannot be null"); }
            else if (pathType == PathType.CameraPath) { Debug.Log("Sorry, CreateMeshFromPath does not support Camera Paths, use a Map Path instead."); }
            else if (lbMesh != null)
            {
                CachePathPointDistances();
                CacheSplinePointDistances();
                Vector3[] splinePointsCentre = cachedCentreSplinePoints;
                Vector3[] splinePointsLeft = GetSplinePathEdgePoints(PositionType.Left, true, meshIncludeEdges);
                Vector3[] splinePointsRight = GetSplinePathEdgePoints(PositionType.Right, true, meshIncludeEdges);

                // Perform some validation
                if (splinePointsCentre == null) { Debug.LogWarning("CreateMeshFromPath - no centre splines defined"); }
                else if (splinePointsLeft == null) { Debug.LogWarning("CreateMeshFromPath - no left splines defined"); }
                else if (splinePointsRight == null) { Debug.LogWarning("CreateMeshFromPath - no right splines defined"); }
                else if (splinePointsCentre.Length < 2) { Debug.LogWarning("CreateMeshFromPath - must have at least 2 centre spline points to create mesh"); }
                else if (splinePointsLeft.Length < 2) { Debug.LogWarning("CreateMeshFromPath - must have at least 2 left spline points to create mesh"); }
                else if (splinePointsRight.Length < 2) { Debug.LogWarning("CreateMeshFromPath - must have at least 2 right spline points to create mesh"); }
                else if (splinePointsLeft.Length != splinePointsRight.Length) { Debug.LogWarning("CreateMeshFromPath - in this release the number of left and right spline points must be the same"); }
                else if (splinePointsCentre.Length != splinePointsRight.Length) { Debug.LogWarning("CreateMeshFromPath - in this release the number of centre, left and right spline points must be the same"); }
                else
                {
                    List<Vector3> vertices = new List<Vector3>();
                    List<int> triangles = new List<int>();
                    List<Vector2> uvs = new List<Vector2>();
                    //List<Vector2> uv2s = new List<Vector2>();
                    //List<Vector2> uv3s = new List<Vector2>();
                    //List<Vector2> uv4s = new List<Vector2>();
                    List<Vector3> normals = new List<Vector3>();
                    List<Vector4> tangents = new List<Vector4>();
                    List<Vector4> colours = new List<Vector4>();    // Store as Vector4s rather than Color so they are serializable

                    //Vector3 normal = new Vector3(0f, 1f, 0f);
                    //Vector4 tangentLeft = new Vector4(1f, 0f, 0f, -1f);

                    // Default colour of each vert (stored in LB as a Vector4)
                    Vector4 defaultVertColour = new Vector4(1f, 1f, 1f, 1f);

                    int numRightVerts = splinePointsRight.Length;
                    int maxPoints = (65536 / 4) - 4;

                    int currentBottomRightVert = 0;
                    float uvRepeat = splineLengthToLastPoint / GetMinWidth();

                    // Used for calculating UVs when relative to Landscape
                    Vector3 landscapeWorldPosition = landscape.transform.position;

                    Vector3 vert1, vert2;
                    Vector3 vert1N, vert2N;
                    Vector2 pt1N, pt2N;
                    float leftEdgeTerrainHeight, rightEdgeTerrainHeight, minEdgeTerrainHeight, avgEdgeTerrainHeight;

                    // For normals we need to consider the previous (pt-1), and the next points (pt+1)
                    Vector3 vert1prev = Vector3.zero, vert1next = Vector3.zero, vert2prev = Vector3.zero, vert2next = Vector3.zero;
                    Vector3 v1v2;
                    Vector4 tangent;

                    if (numRightVerts > 0)
                    {
                        vert1prev = splinePointsRight[0];
                        vert2prev = splinePointsLeft[0];
                    }

                    for (int pt = 0; pt < numRightVerts && pt < maxPoints; pt++)
                    {
                        vert1 = splinePointsRight[pt];
                        vert2 = splinePointsLeft[pt];

                        if (meshEdgeSnapToTerrain)
                        {
                            // Find the terrain height at the left and right positions in the landscape.
                            rightEdgeTerrainHeight = LBLandscapeTerrain.GetHeight(landscape, new Vector2(vert1.x, vert1.z), false) + landscape.start.y;
                            leftEdgeTerrainHeight = LBLandscapeTerrain.GetHeight(landscape, new Vector2(vert2.x, vert2.z), false) + landscape.start.y;

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

                        // Set default vert colour
                        colours.Add(defaultVertColour);
                        colours.Add(defaultVertColour);

                        if (pt > 0)
                        {
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

                            // Calculate normals
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

                        v1v2 = (vert2 - vert1).normalized;
                        tangent = new Vector4(v1v2.x, v1v2.y, v1v2.z, 1f);
                        tangents.Add(tangent);
                        tangents.Add(tangent);

                        // Create UVs
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
                            float splineDistPtNormalised = cachedCentreSplinePointDistances[pt] / splineLengthToLastPoint;

                            // Muliply by the uvRepeat factor which assumes the texture covers the full width
                            // of the path and that the texture being used is set to Repeat not Clamp.
                            splineDistPtNormalised *= uvRepeat;

                            splineDistPtNormalised *= meshUVTileScale.y;

                            // Add UVs in same order as verts
                            // u is in direction of the path, v is left to right across the path
                            uvs.Add(new Vector2(meshUVTileScale.x, splineDistPtNormalised));
                            uvs.Add(new Vector2(0f, splineDistPtNormalised));
                        }

                        vert1prev = vert1;
                        vert2prev = vert2;

                        if (pt > 0) { currentBottomRightVert += 2; }
                    }

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

                    #if UNITY_EDITOR
                    if (landscape.showTiming) { Debug.Log("LBPath.CreateMeshFromPath - created Mesh (verts " + vertices.Count + " tris " + (triangles.Count / 3) + ") in " + (Time.realtimeSinceStartup - tStart).ToString("0.000") + " seconds"); }
                    #endif
                }
            }
            return isSuccess;
        }

        /// <summary>
        /// Rebuild the mesh from the verts, triangles, normals, uvs, tangents etc.
        /// Create a new mesh if one doesn't already exist
        /// </summary>
        /// <returns></returns>
        public bool RebuildMesh()
        {
            bool isSuccess = false;

            if (lbMesh.mesh == null) { lbMesh.mesh = new Mesh(); }

            if (lbMesh.mesh == null) { Debug.LogWarning("ERROR: LBPath.RebuildMesh - could not create new Mesh"); }
            else
            {
                lbMesh.mesh.name = pathName;

                // Assign verts, triangle to new Unity Mesh and recalc bounds and normals
                isSuccess = lbMesh.UpdateMesh(false, true);
            }

            return isSuccess;
        }

        #endregion

        #region Path Edge Methods

        /// <summary>
        /// Get a (new) path position based on the position type
        /// If the PositionType for the current PathType isn't applicable or the
        /// positionIndex is out of range, Vector3.zero will be returned.
        /// PositionType of Centre will return the current Vector3 value
        /// lockYToCentrePosition will keep left and right edge in same xz plane as centre point.
        /// </summary>
        /// <param name="positionIndex"></param>
        /// <param name="positionType"></param>
        /// <param name="lockYtoCentrePosition"></param>
        /// <returns></returns>
        public Vector3 GetPathEdgePosition(int positionIndex, PositionType positionType, bool lockYtoCentrePosition)
        {
            Vector3 pathPosition = Vector3.zero;
            Vector3 centrePathPosition = Vector3.zero;
            Vector3 forwards = Vector3.zero;

            if (positionList == null) { Debug.LogWarning("LBPath.GetPathPosition - positionList is null"); }
            else if (positionIndex < 0 || positionIndex > positionList.Count - 1) { Debug.LogWarning("ERROR: LBPath.GetPathPosition - positionIndex is out of range"); }
            else
            {
                centrePathPosition = positionList[positionIndex];

                // A camera path has no width so, just return the centre point
                if (positionType == PositionType.Centre || pathType == PathType.CameraPath) { pathPosition = centrePathPosition; }
                else if (pathType == PathType.MapPath)
                {
                    if (closedCircuit && positionIndex == positionList.Count - 1)
                    {
                        forwards = GetForwards(0);
                    }
                    else
                    {
                        forwards = GetForwards(positionIndex);
                    }

                    // Rotate the local forwards direction left or right, then add that to the current centre point on the path
                    if (positionType == PositionType.Left)
                    {
                        pathPosition = ((Quaternion.Euler(0f, -90f, 0f) * forwards.normalized) * widthList[positionIndex] / 2f) + centrePathPosition;
                    }
                    else if (positionType == PositionType.Right)
                    {
                        pathPosition = ((Quaternion.Euler(0f, 90f, 0f) * forwards.normalized) * widthList[positionIndex] / 2f) + centrePathPosition;
                    }

                    // forwards.normalized will also change the Y position which may not be desirable.
                    // keep left and right edge in same xz plane as centre point.
                    if (lockYtoCentrePosition) { pathPosition.y = centrePathPosition.y; }
                }
            }

            return pathPosition;
        }

        /// <summary>
        /// Calculate all the edge positions of the curve based on the centre spline and the width of the
        /// path at that point in the path.
        /// </summary>
        public void RefreshPathEdgePositions()
        {
            if (pathType == PathType.MapPath)
            {
                if (positionList != null && positionListLeftEdge != null && positionListRightEdge != null)
                {
                    int numPositions = positionList.Count;

                    for (int i = 0; i < numPositions; i++)
                    {
                        positionListLeftEdge[i] = GetPathEdgePosition(i, PositionType.Left, true);
                        positionListRightEdge[i] = GetPathEdgePosition(i, PositionType.Right, true);
                    }
                }
            }
            // Camera Paths don't have edges
            else if (pathType == PathType.CameraPath) { return; }
        }

        /// <summary>
        /// Get the edge spline points along the path. The distance between spline points is based on the path resolution.
        /// By default the left and right edge points are at the very edge of the path. If includeEdge is false,
        /// the spline that is returned doesn't include the EdgeWidth
        /// </summary>
        /// <param name="positionType"></param>
        /// <param name="showErrors"></param>
        /// <param name="includeEdge"></param>
        /// <returns></returns>
        public Vector3[] GetSplinePathEdgePoints(PositionType positionType, bool showErrors, bool includeEdge = true)
        {
            List<Vector3> splinePointList = new List<Vector3>();
            Vector3 centreSplinePoint = Vector3.zero;
            Vector3 splinePoint = Vector3.zero;
            Vector3 forwards = Vector3.zero;
            float halfWidth = 0f;

            if (pathType == PathType.MapPath)
            {
                // If (mistakenly) requested the centre point, returned the cached array
                if (positionType == PositionType.Centre) { return cachedCentreSplinePoints; }
                // Perform some validation
                else if (cachedCentreSplinePoints == null)
                {
                    if (showErrors) { Debug.LogWarning("LBPath.GetSplinePathEdgePoints - centreSplinePathPoints is null"); }
                }
                else if (cachedCentreSplinePointDistances == null)
                {
                    if (showErrors) { Debug.LogWarning("LBPath.GetSplinePathEdgePoints - splinePathPointDistances is null"); }
                }
                else
                {
                    int numSplinePathPoints = cachedCentreSplinePointsLength;
                    int numSplinePathPointDistances = cachedCentreSplinePointDistancesLength;

                    if (numSplinePathPoints != numSplinePathPointDistances) { if (showErrors) { Debug.LogWarning("LBPath.GetSplinePathEdgePoints - spline and distance arrays must be same size"); } }
                    else
                    {
                        for (int i = 0; i < numSplinePathPoints; i++)
                        {
                            centreSplinePoint = cachedCentreSplinePoints[i];

                            // Get the forwards direction
                            // For the first and last positions in the spline, use the first and last from the positionList
                            // so that the spline and user-defined position points match.
                            if (i == 0) { forwards = GetForwards(0); }
                            else if (closedCircuit && i == numSplinePathPoints - 1) { forwards = GetForwards(0); }
                            else if (i == numSplinePathPoints - 1) { forwards = GetForwards(positionList.Count - 1); }
                            else { forwards = GetForwardsOnSpline(i); }

                            // Get the interpolated width at this point along the spline
                            halfWidth = GetWidthOnSpline(i) / 2f;

                            if (!includeEdge) { halfWidth -= edgeBlendWidth; }

                            // Apply the 90 deg rotation to left/right points + width
                            // Rotate the local forwards direction left or right, then add that to the current spline centre point on the path
                            if (positionType == PositionType.Left)
                            {
                                splinePoint = ((Quaternion.Euler(0f, -90f, 0f) * forwards.normalized) * halfWidth) + centreSplinePoint;
                            }
                            else if (positionType == PositionType.Right)
                            {
                                splinePoint = ((Quaternion.Euler(0f, 90f, 0f) * forwards.normalized) * halfWidth) + centreSplinePoint;
                            }

                            // The edges should have the same Y as the centre point
                            splinePoint.y = centreSplinePoint.y;

                            splinePointList.Add(splinePoint);
                        }
                    }
                }
            }
            else
            {
                if (showErrors) { Debug.LogWarning("LBPath.GetSplinePathEdgePoints - " + pathType + " does not support edge points"); }
            }

            return splinePointList.ToArray();
        }

        /// <summary>
        /// Get the edge spline points along the path. The distance between spline points is based on the path resolution.
        /// By default the left and right edge points are at the very edge of the path. If includeEdge is false,
        /// the spline that is returned doesn't include the EdgeWidth.
        /// NOTE: If using a ObjPath with width and potentially rotation, use LBObjPath.RefreshObjPathPositions() instead.
        /// Pre-requisite: CacheSplinePoints2();
        /// This routine returns a new array each time so may result in memory fragmentation
        /// </summary>
        /// <param name="positionType"></param>
        /// <param name="showErrors"></param>
        /// <param name="includeEdge"></param>
        /// <returns></returns>
        public Vector3[] GetSplinePathEdgePoints2(PositionType positionType, bool showErrors, bool includeEdge = true)
        {
            List<Vector3> splinePointList = new List<Vector3>();

            if (pathType == PathType.MapPath)
            {
                int numPositionPoints = (positionList == null ? 0 : positionList.Count);

                if (positionType == PositionType.Centre) { return cachedCentreSplinePointList.ToArray(); }
                else
                {

                    bool isLeft = positionType == PositionType.Left;
                    float halfWidth = 0f;
                    Vector3 splinePoint = Vector3.zero;

                    // Update the left/right path position points (based on path positions set by the user in the editor)
                    for (int i = 0; i < numPositionPoints; i++)
                    {
                        // Add the outermost left and right edge points (rotation for MapPath is always 0.
                        if (isLeft) { positionListLeftEdge[i] = GetPathOffsetPosition(i, widthList[i] / -2f, 0f); }
                        else { positionListRightEdge[i] = GetPathOffsetPosition(i, widthList[i] / 2f, 0f); } 
                    }

                    // Update the cached left and right spline points
                    int numSplinePoints = (cachedCentreSplinePointList == null ? 0 : cachedCentreSplinePointList.Count);

                    for (int i = 0; i < numSplinePoints; i++)
                    {
                        // Get the interpolated width at this point along the spline
                        halfWidth = GetWidthOnSpline(i, widthList) / 2f;

                        if (!includeEdge) { halfWidth -= edgeBlendWidth; }

                        if (isLeft) { halfWidth = -halfWidth; }

                        splinePoint = GetPathOffsetPosition(cachedCentreSplinePointDistancesList[i], halfWidth, 0f);

                        // The edges should have the same Y as the centre point
                        splinePoint.y = cachedCentreSplinePointList[i].y;

                        splinePointList.Add(splinePoint);
                    }
                }
            }
            else
            {
                if (showErrors) { Debug.LogWarning("LBPath.GetSplinePathEdgePoints2 - " + pathType + " edge points are not support with this method."); }
            }

            return splinePointList.ToArray();
        }

        #endregion

        #region Path Offset Methods
        // Similar to Path Edge Methods but can have any offset
        // Most of these methods depend on the newer CacheSplinePoints2() method.

        /// <summary>
        /// Get a 3D position relative to a point along the path's central spline. -ve offsets are on left and
        /// +ve offsets are on the right. positionIndex is the user-defined path point zero-based index.
        /// Prerequisite: CacheSplinePoints2()
        /// NOTE: If updating, also update GetPathOffsetPosition(float distanceAlongObjPath, float offsetDistance, float rotationZ)
        /// TODO take into consideration RotationZ
        /// </summary>
        /// <param name="positionIndex"></param>
        /// <param name="offsetDistance"></param>
        /// <param name="rotationZ"></param>
        /// <returns></returns>
        public Vector3 GetPathOffsetPosition(int positionIndex, float offsetDistance, float rotationZ)
        {
            int numPositionPoints = (positionList == null ? 0 : positionList.Count);

            if (positionIndex < 0 || positionIndex > numPositionPoints - 1) { return Vector3.zero; }
            else
            {
                Vector3 centrePathPosition = positionList[positionIndex];
                Vector3 forwards = Vector3.zero;
                Vector3 offsetPosition = Vector3.zero;

                if (positionIndex == numPositionPoints - 1)
                {
                    // At the end, so look backwards along path
                    forwards = GetForwardsAtEnd();
                    // Use GetForwardsFast as it correctly determines forward direction of last user-defined point in the path
                    //forwards = GetForwardsFast(positionIndex, 1f);
                }
                else
                {
                    // Get distance along path.

                    // Find the distance that we are currently at along the object path
                    // First find the distances along the object path (ignore nearestSplinePoint if this is the end of the path)
                    int nearestSplinePoint = GetNearestSplinePointFromPath(positionIndex);
                    float distanceAlongPath = cachedCentreSplinePointDistancesList[nearestSplinePoint];

                    // Add the distance between closest spline point and the path point
                    distanceAlongPath += Vector3.Distance(cachedCentreSplinePointList[nearestSplinePoint], centrePathPosition);

                    // Get forwards direction on path at the current position
                    if (distanceAlongPath < splineLength - 0.1f)
                    {
                        forwards = GetPathPosition(distanceAlongPath + 0.1f, 1) - centrePathPosition;
                    }
                    else
                    {
                        // Near or at the end, so look backwards along path
                        forwards = centrePathPosition - GetPathPosition(distanceAlongPath - 0.1f, 1);
                    }
                }

                // Calculate, using a cross product of the world up direction and the path forwards direction, the path right direction
                // This won't have a length of one, so normalise it
                // Then multiply this vector by the offset distance (this will be negative for the left of the path)
                // and add it to the centrePathPosition to calculate the offset position
                offsetPosition = (Vector3.Cross(Vector3.up, forwards).normalized * offsetDistance) + centrePathPosition;

                // TODO Apply z-axis rotation if required


                return offsetPosition;
            }
        }

        /// <summary>
        /// Get a 3D position relative to a point along the path's central spline. -ve offsets are on left and
        /// +ve offsets are on the right.
        /// Prerequisite: CacheSplinePoints2()
        /// NOTE: If updating, also update GetPathOffsetPosition(int positionIndex, float offsetDistance, float rotationZ)
        /// TODO take into consideration RotationZ
        /// </summary>
        /// <param name="distanceAlongObjPath"></param>
        /// <param name="offsetDistance"></param>
        /// <param name="rotationZ"></param>
        /// <returns></returns>
        public Vector3 GetPathOffsetPosition(float distanceAlongObjPath, float offsetDistance, float rotationZ)
        {
            int numPositionPoints = (positionList == null ? 0 : positionList.Count);

            if (numPositionPoints < 1) { return Vector3.zero; }
            else
            {
                // Get position on path
                Vector3 centrePathPosition = GetPathPosition(distanceAlongObjPath, 1);
                Vector3 forwards = Vector3.zero;
                Vector3 offsetPosition = Vector3.zero;

                // Get forwards direction on path at the current position
                if (distanceAlongObjPath < splineLength - 0.1f)
                {
                    forwards = GetPathPosition(distanceAlongObjPath + 0.1f, 1) - centrePathPosition;
                }
                else
                {
                    // Near or at the end, so look backwards along path
                    forwards = centrePathPosition - GetPathPosition(distanceAlongObjPath - 0.1f, 1);
                }

                // Calculate, using a cross product of the world up direction and the path forwards direction, the path right direction
                // This won't have a length of one, so normalise it
                // Then multiply this vector by the offset distance (this will be negative for the left of the path)
                // and add it to the centrePathPosition to calculate the offset position
                offsetPosition = (Vector3.Cross(Vector3.up, forwards).normalized * offsetDistance) + centrePathPosition;

                // TODO Apply z-axis rotation if required


                return offsetPosition;
            }
        }

        /// <summary>
        /// Get a 3D position relative to a point along the path's central spline. -ve offsets are on left and
        /// +ve offsets are on the right.
        /// NOTE: Parameter forwards must be normalized.
        /// </summary>
        /// <param name="centreSplinePosition"></param>
        /// <param name="forwards"></param>
        /// <param name="offsetDistance"></param>
        /// <param name="rotationZ"></param>
        /// <returns></returns>
        public Vector3 GetPathOffsetPosition(Vector3 centreSplinePosition, Vector3 forwards, float offsetDistance, float rotationZ)
        {
            // Calculate, using a cross product of the world up direction and the path forwards direction, the path right direction
            // This won't have a length of one, so normalise it
            // Then multiply this vector by the offset distance (this will be negative for the left of the path)
            // and add it to the centrePathPosition to calculate the offset position
            Vector3 offsetPosition = (Vector3.Cross(Vector3.up, forwards).normalized * offsetDistance) + centreSplinePosition;

            // TODO Apply z-axis rotation if required


            return offsetPosition;
        }

        #endregion

        #region Path Position point methods

        /// <summary>
        /// Get the list of positions for this PositionType
        /// </summary>
        /// <param name="positionType"></param>
        /// <returns></returns>
        public List<Vector3> GetPositionList(PositionType positionType)
        {
            if (positionType == PositionType.Left) { return positionListLeftEdge; }
            else if (positionType == PositionType.Right) { return positionListRightEdge; }
            else { return positionList; }
        }

        /// <summary>
        /// Return a new list of points along the current path with the new points
        /// spaced a given distance apart.
        /// Prerequisite: CacheSplinePoints2() 
        /// </summary>
        /// <param name="newIntervalDistance"></param>
        /// <param name="isLastObjSnappedToEnd"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public List<Vector3> GetPositionList(float newIntervalDistance, bool isLastObjSnappedToEnd, bool showErrors)
        {
            // Sample Usage with LBObjPath
            // lbObjPath.isSplinesCached2 = false;
            // lbObjPath.CacheSplinePoints2();
            // List<Vector3> newPosList = lbObjPath.GetPositionList(15f, false, true);
            // Debug.Log("INFO: New list count: " + (newPosList == null ? 0 : newPosList.Count));
            // if (newPosList != null)
            // {
            //     lbObjPath.pathPointList.Clear();
            //     lbObjPath.positionList.Clear();
            //     lbObjPath.positionList.AddRange(newPosList);
            //     foreach (Vector3 pos in newPosList)
            //     {
            //         lbObjPath.pathPointList.Add(new LBPathPoint());
            //     }
            // }
            // newPosList.Clear();
            // newPosList = null;

            List<Vector3> newPositionList = new List<Vector3>();
            string methodName = "LBPath.GetPositionList";
            float distanceAlongSpline = 0f;
            int numNewList = 0;

            // In this faster version of GetPathPosition we can only ever use centre spline points as they are a fixed distance apart (based on path resolution)
            if (cachedCentreSplinePointList == null) { if (showErrors) { Debug.LogWarning(methodName + " no cached points to process"); } }
            else if (newIntervalDistance <= 0f) { if (showErrors) { Debug.LogWarning(methodName + " newIntervalDistance must be greater than zero"); } }
            else if (splineLength <= 0f) { if (showErrors) { Debug.LogWarning(methodName + " the spline length is zero"); } }
            else if (positionList == null) { if (showErrors) { Debug.LogWarning(methodName + " positionList is null"); } }
            else if (positionList.Count < 2) { if (showErrors) { Debug.LogWarning(methodName + " positionList must contain at least 2 points. Only " + positionList.Count + " found."); } }
            else if (newPositionList == null) { if (showErrors) { Debug.LogWarning(methodName + " could not create new list."); } }
            else
            {
                // If path is valid, always add the first point
                newPositionList.Add(positionList[0]);

                if (newIntervalDistance <= splineLength)
                {
                    distanceAlongSpline += newIntervalDistance;

                    while (distanceAlongSpline <= splineLength)
                    {
                        // Algorithm 1 (linear interpolation) seems to give much more accurate spacing than algorithm 0 (catmull rom)
                        newPositionList.Add(GetPathPosition(distanceAlongSpline, 1));
                        distanceAlongSpline += newIntervalDistance;
                    }

                    numNewList = newPositionList.Count;

                    if (isLastObjSnappedToEnd)
                    {
                        Vector3 finalPos = positionList[positionList.Count - 1];
                        //if (finalPos != newPositionList[newPositionList.Count - 1])
                        if (finalPos != newPositionList[numNewList - 1])
                        {
                            //newPositionList[newPositionList.Count - 1] = finalPos;
                            newPositionList[numNewList - 1] = finalPos;
                        }
                    }

                    // Should check if second last position, assuming more than 2 points, is not to close to last point
                    //if (numNewList > 2)
                    //{
                    //    if (Vector3.Distance(newPositionList[numNewList - 2], newPositionList[numNewList - 1]) < newIntervalDistance)
                    //    {
                    //        newPositionList.RemoveAt(numNewList - 2);
                    //    }
                    //}
                }
            }

            return newPositionList;
        }

        /// <summary>
        /// Given a distance along the path from the start, find the Vector3 position
        /// isSplinePointPosition is used when the points are along a spline.
        /// For paths with a lot of points, setting the startPoint to search from is
        /// faster if the approximate location is known. The default start point is the
        /// second in the path (1).
        /// NOTE: A path must have at least 2 points
        /// </summary>
        /// <param name="distance"></param>
        /// <param name="positionType"></param>
        /// <param name="isSplinePointPosition"></param>
        /// <param name="startPoint"></param>
        /// <returns></returns>
        public Vector3 GetPathPosition(float distance, PositionType positionType, bool isSplinePointPosition = false, int startPoint = 1)
        {
            Vector3[] cachePathPointsForType = GetCachedPathPoints(positionType, isSplinePointPosition);
            List<Vector3> positionListForType = GetPositionList(positionType);

            float[] cachedPointDistancesForType = null;
            int cachedPointDistancesLengthForType = 0;

            if (isSplinePointPosition)
            {
                cachedPointDistancesForType = cachedCentreSplinePointDistances;
                cachedPointDistancesLengthForType = cachedCentreSplinePointDistancesLength;
            }
            else
            {
                cachedPointDistancesForType = cachedPathPointDistances;
                cachedPointDistancesLengthForType = cachedPathPointDistancesLength;
            }

            if (positionListForType == null || cachePathPointsForType == null) { return Vector3.zero; }
            else
            {
                // Store numberOfPathPoints to save having to look it up a lot
                // use cachedPathPoints.length rather than pathPoints.Count.
                int numberOfPathPoints = cachePathPointsForType.Length;

                // If no points in path, return a zero vector
                if (numberOfPathPoints == 0) { return Vector3.zero; }
                // If only one point in path or we're right at the beginning, return the first point
                else if (numberOfPathPoints == 1 || distance < 0.001f) { return positionListForType[0]; }
                else
                {
                    int point = startPoint; // Default start point is the second point (index of 1)
                    float _cachedLength = pathLength;
                    float _distanceToLastPoint = pathLengthToLastPoint;
                    float _distanceTo2ndLastPoint = 0f;   // NOTE: Only applies to spline paths
                    if (isSplinePointPosition)
                    {
                        _cachedLength = splineLength;
                        _distanceToLastPoint = splineLengthToLastPoint;
                        _distanceTo2ndLastPoint = splineDistTo2ndLastPoint;
                    }

                    // If the distance is at the end of the path or beyond it, return the position of the last point.
                    if (distance >= _distanceToLastPoint && !closedCircuit) { return cachePathPointsForType[numberOfPathPoints - 1]; }

                    // These are the points on the path in the last segment (not including the gap between end and start of path)
                    else if (isSplinePointPosition && closedCircuit && distance >= _distanceTo2ndLastPoint && distance < _distanceToLastPoint)
                    {
                        return Vector3.Lerp(cachePathPointsForType[numberOfPathPoints - 3], cachePathPointsForType[numberOfPathPoints - 2], (distance - _distanceTo2ndLastPoint) / (_distanceToLastPoint - _distanceTo2ndLastPoint));
                    }

                    // Mathf.Repeat works as a modulo (%) operator for floats
                    //distance = Mathf.Repeat(distance, _cachedLength);

                    // Works the same as Mathf.Repeat(distance, _cachedLength) for +ve numbers
                    while (distance > 0f && distance > _cachedLength) { distance -= _cachedLength; }

                    // Iterate through path points until a point is found that is further away from the start
                    // of the path than the specified distance
                    // Looping through the array is faster than doing a FindIndex on the list.
                    while (distance > cachedPointDistancesForType[point])
                    {
                        //point++;
                        if (++point >= cachedPointDistancesLengthForType) { point = 0; break; }
                    }

                    // Get the nearest four path points using the point variable
                    // The modulo (%) operator basically just makes sure that any values below zero or above the length
                    // of the postions array are looped around to their logical position

                    // When closed circuit is off, the second point (point=1) in the path needs special treatment
                    // or else the path will be influenced by the last point in the path.
                    if (!closedCircuit && point == 1)
                    {
                        point2 = cachePathPointsForType[(point - 1 + numberOfPathPoints) % numberOfPathPoints];
                        point3 = cachePathPointsForType[point % numberOfPathPoints];
                        point4 = cachePathPointsForType[(point + 1 + numberOfPathPoints) % numberOfPathPoints];

                        // Don't consider the last point in the path when not using closedCircuit
                        // Instead set the first point to be the same as the second.
                        point1 = point2;
                    }
                    // When closed circuit is off, the last point in the path also needs special treatment
                    // or else the path will be influenced by the first point in the path.
                    else if (!closedCircuit && point == numberOfPathPoints - 1)
                    {
                        point1 = cachePathPointsForType[(point - 2 + numberOfPathPoints) % numberOfPathPoints];
                        point2 = cachePathPointsForType[(point - 1 + numberOfPathPoints) % numberOfPathPoints];
                        point3 = cachePathPointsForType[point % numberOfPathPoints];

                        // Don't consider the last point in the path when not using closedCircuit
                        // Instead set the first point to be the same as the second.
                        point4 = point3;
                    }
                    else
                    {
                        point1 = cachePathPointsForType[(point - 2 + numberOfPathPoints) % numberOfPathPoints];
                        point2 = cachePathPointsForType[(point - 1 + numberOfPathPoints) % numberOfPathPoints];
                        point3 = cachePathPointsForType[point % numberOfPathPoints];
                        point4 = cachePathPointsForType[(point + 1 + numberOfPathPoints) % numberOfPathPoints];
                    }

                    // The inverseLerp function provides a parameter for the curvature of the spline
                    inverseLerpPos = Mathf.InverseLerp(cachedPointDistancesForType[(point - 1) % numberOfPathPoints], cachedPointDistancesForType[point % numberOfPathPoints], distance);

                    return CatmullRom(point1, point2, point3, point4, inverseLerpPos);
                }
            }
        }

        /// <summary>
        /// Given a distance along the path from the start, find the Vector3 position of the point on the spline.
        /// This is a faster version of GetPathPosition which is limited to (cached) centre spline points only.
        /// It optimises the function by using the fact that spline points are spaced at regular intervals.
        /// If algorithm is set to 0, catmull rom is used to interpolate between spline points.
        /// If algorithm is set to 1, linear interpolation is used to interpolate between spline points.
        /// If algorithm is set to 2, the algorithm simply chooses the closest spline point.
        /// Lower algorithm numbers are more precise while higher algorithm numbers are faster.
        /// NOTE: A path must have at least 2 points
        /// PREREQUISITE: CacheSplinePoints2()
        /// WARNING: For some reason, when at end of path, Algorithm 1 returns the first point on path.
        /// Also Algorithm 1, it tends to wobble a little side to side along a path.
        /// Uses cachedCentreSplinePointList which gets populated with CacheSplinePoints2()
        /// </summary>
        /// <param name="distance"></param>
        /// <param name="isSplinePointPosition"></param>
        /// <returns></returns>
        public Vector3 GetPathPosition (float distance, int algorithm = 0)
        {
            // In this faster version of GetPathPosition we can only ever use centre spline points
            // These are the spline points used for the path segments which are pathResolution distance apart.

            // Store numberOfPathPoints to save having to look it up a lot
            // use cachedPathPoints.length rather than pathPoints.Count.
            int numberOfPathPoints = (cachedCentreSplinePointList == null ? 0 : cachedCentreSplinePointList.Count);

            // If no points in path, return a zero vector
            if (numberOfPathPoints == 0) { return Vector3.zero; }
            else
            {
                // If only one point in path or we're right at the beginning, return the first point
                if (numberOfPathPoints == 1 || distance < 0.001f) { return cachedCentreSplinePointList[0]; }
                else
                {
                    // Probably need to do some sort of modulo for distance here...
                    // modulo (%) operator for +ve floats numbers
                    while (distance > 0f && distance > splineLength) { distance -= splineLength; }

                    #region Algorithm 0 - catmull rom used to interpolate between spline points
                    if (algorithm == 0)
                    {
                        // Find the index of the first point that is further than the specified distance from the start of the path
                        int pointIndex = Mathf.CeilToInt(distance / pathResolution);

                        // Get the nearest four path points using the point variable
                        // The modulo (%) operator basically just makes sure that any values below zero or above the length
                        // of the postions array are looped around to their logical position

                        // When closed circuit is off, the second point (point=1) in the path needs special treatment
                        // or else the path will be influenced by the last point in the path.
                        if (!closedCircuit && pointIndex == 1)
                        {
                            point2 = cachedCentreSplinePointList[(pointIndex - 1 + numberOfPathPoints) % numberOfPathPoints];
                            point3 = cachedCentreSplinePointList[pointIndex % numberOfPathPoints];
                            point4 = cachedCentreSplinePointList[(pointIndex + 1 + numberOfPathPoints) % numberOfPathPoints];

                            // Don't consider the last point in the path when not using closedCircuit
                            // Instead set the first point to be the same as the second.
                            point1 = point2;
                        }
                        // When closed circuit is off, the last point in the path also needs special treatment
                        // or else the path will be influenced by the first point in the path.
                        else if (!closedCircuit && pointIndex == numberOfPathPoints - 1)
                        {
                            point1 = cachedCentreSplinePointList[(pointIndex - 2 + numberOfPathPoints) % numberOfPathPoints];
                            point2 = cachedCentreSplinePointList[(pointIndex - 1 + numberOfPathPoints) % numberOfPathPoints];
                            point3 = cachedCentreSplinePointList[pointIndex % numberOfPathPoints];

                            // Don't consider the last point in the path when not using closedCircuit
                            // Instead set the first point to be the same as the second.
                            point4 = point3;
                        }
                        else
                        {
                            point1 = cachedCentreSplinePointList[(pointIndex - 2 + numberOfPathPoints) % numberOfPathPoints];
                            point2 = cachedCentreSplinePointList[(pointIndex - 1 + numberOfPathPoints) % numberOfPathPoints];
                            point3 = cachedCentreSplinePointList[pointIndex % numberOfPathPoints];
                            point4 = cachedCentreSplinePointList[(pointIndex + 1 + numberOfPathPoints) % numberOfPathPoints];
                        }

                        // The inverseLerp function provides a parameter for the curvature of the spline
                        inverseLerpPos = Mathf.InverseLerp(cachedCentreSplinePointDistancesList[(pointIndex - 1) % numberOfPathPoints], cachedCentreSplinePointDistancesList[pointIndex % numberOfPathPoints], distance);

                        // Use the catmull rom algorithm to interpolate between the given points
                        return CatmullRom(point1, point2, point3, point4, inverseLerpPos);
                    }
                    #endregion

                    #region Algorithm 1 - linear interpolation used to interpolate between spline points
                    else if (algorithm == 1)
                    {
                        // Calculate the index of the closest point, keeping any fractional parts
                        float fractionalPointIndex = distance / pathResolution;
                        // Calculate the indices of the points on either side
                        int ceilPointIndex = Mathf.CeilToInt(fractionalPointIndex);
                        if (ceilPointIndex >= numberOfPathPoints) { ceilPointIndex = numberOfPathPoints - 1; }
                        // Linearly interpolate between the two points
                        try
                        {
                            point1 = cachedCentreSplinePointList[ceilPointIndex - 1];
                            point2 = cachedCentreSplinePointList[ceilPointIndex];
                        }
                        catch (System.Exception ex)
                        {
                            Debug.Log("[Debug] GetPathPosition numberOfPathPoints: " + numberOfPathPoints + " ceilPointIndex:" + ceilPointIndex + " distance:" + distance + " " + ex.Message);
                        }

                        // Original
                        //return point1 + ((point2 - point1) * (fractionalPointIndex - ceilPointIndex + 1));
                        // Should be faster - do vector maths last
                        return ((fractionalPointIndex - ceilPointIndex + 1f) * (point2 - point1)) + point1;
                    }
                    #endregion

                    #region Algorithm 2 - no interpolation between spline points (just chooses the closest one)
                    else
                    {
                        int pointIndex = Mathf.RoundToInt(distance / pathResolution);
                        if (pointIndex >= numberOfPathPoints) { pointIndex = numberOfPathPoints - 1; }
                        return cachedCentreSplinePointList[pointIndex];
                    }
                    #endregion
                }
            }
        }

        /// <summary>
        /// Update the supplied position and forwards params, using the distance along the path.
        /// If algorithm is set to 0, catmull rom is used to interpolate between spline points.
        /// If algorithm is set to 1, linear interpolation is used to interpolate between spline points.
        /// If algorithm is set to 2, the algorithm simply chooses the closest spline point.
        /// Lower algorithm numbers are more precise while higher algorithm numbers are faster.
        /// forwards is returned normalized.
        /// NOTE: distance must be less than equal to splineLength
        /// PREREQUISITE: CacheSplinePoints2()
        /// </summary>
        /// <param name="distance"></param>
        /// <param name="algorithm"></param>
        /// <param name="position"></param>
        /// <param name="forwards"></param>
        public void GetPathPositionForwards(float distance, int algorithm, ref Vector3 position, ref Vector3 forwards)
        {
            if (distance == 0f) { position = cachedCentreSplinePointList[0]; }
            // This should work for both closedCircuit and non-closed
            else if (distance == splineLength) { position = cachedCentreSplinePointList[cachedCentreSplinePointsLength - 1]; }
            else { position = GetPathPosition(distance, algorithm); }

            if (distance < splineLength - 0.1f) { forwards = (GetPathPosition(distance + 0.1f, algorithm) - position).normalized; }
            // Near or at the end of the path, so treat as special case. Currently use algorithm 1 for GetForwardsAtEnd().
            else {  forwards = GetForwardsAtEnd(); }
        }

        /// <summary>
        /// Clamp the path position point to be within the boundaries of a rectangle with a given height and Y offset
        /// ClampPositionToBounds(i, landscapeBounds, landscape.transform.position.y, landscapeHeight)
        /// </summary>
        /// <param name="positionIndex"></param>
        /// <param name="boundsXZ"></param>
        /// <param name="minHeight"></param>
        /// <param name="maxHeight"></param>
        public void ClampPositionToBounds(int positionIndex, Rect boundsXZ, float minHeight, float maxHeight)
        {
            Vector3 snappedPos = positionList[positionIndex];

            if (positionList[positionIndex].x < boundsXZ.xMin) { snappedPos.x = boundsXZ.xMin; }
            else if (positionList[positionIndex].x > boundsXZ.xMax) { snappedPos.x = boundsXZ.xMax; }

            if (positionList[positionIndex].y < minHeight) { snappedPos.y = minHeight; }
            else if (positionList[positionIndex].y > maxHeight) { snappedPos.y = maxHeight; }

            if (positionList[positionIndex].z < boundsXZ.yMin) { snappedPos.z = boundsXZ.yMin; }
            else if (positionList[positionIndex].z > boundsXZ.yMax) { snappedPos.z = boundsXZ.yMax; }

            positionList[positionIndex] = snappedPos;
        }

        /// <summary>
        /// Move all points along the same vector give a single start and end position
        /// </summary>
        /// <param name="startPos"></param>
        /// <param name="endPos"></param>
        /// <param name="positionIndex"></param>
        /// <param name="updateEdges"></param>
        public void MovePoints(Vector3 startPos, Vector3 endPos, int positionIndex, bool updateEdges)
        {
            if (positionList != null && positionListLeftEdge != null && positionListRightEdge != null)
            {
                int numPositions = positionList.Count;

                // Get a vector that points from the start position to the end position
                Vector3 direction = endPos - startPos;

                for (int i = 0; i < numPositions; i++)
                {
                    // Don't update the source position
                    if (i != positionIndex)
                    {
                        positionList[i] = positionList[i] + direction;
                    }
                }

                if (updateEdges) { RefreshPathEdgePositions(); }
            }
        }

        /// <summary>
        /// Move all points using an offset and scale them on each axis
        /// If no offset is required, set offset to Vector3.zero
        /// If no scaling is required, set scale to Vector3.one
        /// This is typically called when importing a LBTemplate
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="scale"></param>
        public void MovePoints(Vector3 offset, Vector3 scale)
        {
            if (positionList != null)
            {
                int numPositions = positionList.Count;

                for (int i = 0; i < numPositions; i++)
                {
                    // Move the positions based the offset, then scale the new location
                    //positionList[i] = new Vector3((positionList[i].x + offset.x) * scale.x, (positionList[i].y + offset.y) * scale.y, (positionList[i].z + offset.z) * scale.z);

                    // Correct in LB 2.0.0 Alpha 1j
                    // Scale the location, then add the offset
                    positionList[i] = new Vector3((positionList[i].x * scale.x) + offset.x, (positionList[i].y * scale.y) + offset.y, (positionList[i].z * scale.z) + offset.z);
                }

                // We need to recalculate the distances
                CachePathPointDistances();

                if (pathType == PathType.MapPath)
                {
                    // Re-scale the width
                    if (widthList != null)
                    {
                        int numWidths = widthList.Count;
                        // Take the average of the x and y scaling (prob should be x and z...)
                        float widthScale = (scale.x + scale.y) / 2f;
                        if (widthScale != 0f)
                        {
                            for (int w = 0; w < numWidths; w++)
                            {
                                widthList[w] *= widthScale;
                            }
                        }
                    }
                    RefreshPathEdgePositions();
                }
            }
        }

        /// <summary>
        /// Delete the selected points in the path.
        /// </summary>
        /// <param name="deleteEdges"></param>
        public void DeleteSelectedPoints(bool deleteEdges)
        {
            if (selectedList != null)
            {
                selectedList.Sort();
                int numSelected = selectedList.Count;
                int pointIdx = 0;
                // Loop backwards through the list of sorted, selected path index items
                for (int s = numSelected - 1; s >= 0; s--)
                {
                    pointIdx = selectedList[s];
                    positionList.RemoveAt(pointIdx);

                    if (deleteEdges && widthList != null && widthList.Count > pointIdx) { widthList.RemoveAt(pointIdx); }
                }

                if (deleteEdges)
                {
                    // Clear existing point distances so that edge Gizmos get refreshed in scene view.
                    ClearCachePathPointDistances();
                    RefreshPathEdgePositions();
                }

                selectedList.Clear();
            }
        }

        #endregion

        #region Path Height Methods

        /// <summary>
        /// Set all the positions in the path to be the same height above the terrain.
        /// Optionally ignore the terrain height (useful for OBjPath in GroupDesigner)
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="isIgnoreTerrain"></param>
        public void RefreshPathHeights(LBLandscape landscape, bool isIgnoreTerrain = false)
        {
            if (landscape != null)
            {
                if (positionList != null && !isRefreshing && (pathType == PathType.CameraPath || pathType == PathType.ObjPath || (pathType == PathType.MapPath && positionListLeftEdge != null && positionListRightEdge != null)))
                {
                    isRefreshing = true;
                    int numPositions = positionList.Count;

                    Vector2 posXZ = Vector2.zero;
                    Vector3 snappedPos = Vector3.zero;

                    for (int i = 0; i < numPositions; i++)
                    {
                        posXZ.x = positionList[i].x;
                        posXZ.y = positionList[i].z;
                        snappedPos.x = posXZ.x;
                        snappedPos.z = posXZ.y;

                        // LBLandscapeTerrain.GetHeight() returns a normalised height
                        snappedPos.y = isIgnoreTerrain ? heightAboveTerrain : LBLandscapeTerrain.GetHeight(landscape, posXZ, false) + heightAboveTerrain + landscape.start.y;
                        positionList[i] = snappedPos;
                    }

                    // If this is a MapPath, we also need to refresh the edge heights (positions)
                    if (pathType == PathType.MapPath) { RefreshPathEdgePositions(); }

                    isRefreshing = false;
                }
            }
        }

        #endregion

        #region Path Width Methods

        /// <summary>
        /// Sets all the path widths to the same value
        /// </summary>
        /// <param name="pathWidth"></param>
        public void SetPathWidths(float pathWidth)
        {
            if (widthList != null)
            {
                for (int w = 0; w < widthList.Count; w++)
                {
                    widthList[w] = pathWidth;
                }
            }
        }

        /// <summary>
        /// Add an amount to all widths in the path.
        /// -ve amounts will subtract it.
        /// </summary>
        /// <param name="addWidth"></param>
        /// <param name="minWidth"></param>
        /// <param name="maxWidth"></param>
        public void AddPathWidths(float addWidth, float minWidth, float maxWidth)
        {
            if (widthList != null)
            {
                for (int w = 0; w < widthList.Count; w++)
                {
                    widthList[w] = Mathf.Clamp(widthList[w] + addWidth, minWidth, maxWidth);
                }
            }
        }

        /// <summary>
        /// Get the width from a spline point in the path. This uses the
        /// cachedCentreSplinePointDistances which are the points between the path segments
        /// that are pathResolution apart.
        /// </summary>
        /// <param name="splinePointIndex"></param>
        /// <returns></returns>
        public float GetWidthOnSpline(int splinePointIndex)
        {
            float width = 0f, prevWidth = 0f, nextWidth = 0f;
            float distanceAlongSpline = 0f, distanceBetweenPathPoints = 0f;

            if (pathType == PathType.MapPath && cachedCentreSplinePointDistances != null && cachedPathPointDistances != null && widthList != null)
            {
                // The widthList contains the width, typically set by user, at each position along the path
                // Positions can be adjusted by using the handles in the scene view
                int numWidths = widthList.Count;

                int numSplineDistances = cachedCentreSplinePointDistancesLength;

                // The number of user-defined points should equal the number of Widths
                int numPathDistances = cachedPathPointDistancesLength;

                // If this is the first or last point, set the width's accordingly
                if (splinePointIndex == 0 && numWidths > 0) { width = widthList[0]; }
                else if (splinePointIndex == numSplineDistances - 1 && numWidths > 0) { width = widthList[numWidths - 1]; }
                else if (numSplineDistances > 2 && numPathDistances >= 2)
                {
                    distanceAlongSpline = cachedCentreSplinePointDistances[splinePointIndex];

                    // Iterate through cached point distance points until a point is found that is further away from the start
                    // of the path than the specified distance. This will give the user-defined width for the closest previous path position.
                    // We are using the Path Position Distances, rather than the Spline Point Distances because the Path Positions have
                    // matching user-defined widths (stored in widthList).
                    int positionIndex = 0;
                    while (cachedPathPointDistances[positionIndex] < distanceAlongSpline + 0.001f)
                    {
                        positionIndex++;
                        if (positionIndex >= numPathDistances - 1) { break; }
                    }

                    // Get the widths of the previous and next points closest to the Spline point on the centre spline
                    prevWidth = widthList[positionIndex - 1];
                    if (positionIndex > numWidths - 1 && closedCircuit) { nextWidth = widthList[0]; }
                    else { nextWidth = widthList[positionIndex]; }

                    // Get the distance between the previous and next Path Position points (where the matching widths came from)
                    if (numPathDistances > positionIndex)
                    {
                        distanceBetweenPathPoints = cachedPathPointDistances[positionIndex] - cachedPathPointDistances[positionIndex - 1];

                        if (distanceBetweenPathPoints == 0f) { width = prevWidth; }
                        else { width = Mathf.Lerp(prevWidth, nextWidth, (distanceAlongSpline - cachedPathPointDistances[positionIndex - 1]) / distanceBetweenPathPoints); }
                    }
                }
            }

            return width;
        }

        /// <summary>
        /// Get the width from a spline point in the path using a list of spline widths and
        /// cachedCentreSplinePointDistancesList which are the points between the path segments
        /// that are pathResolution apart.
        /// Prerequisites: CacheSplinePoints2()
        /// </summary>
        /// <param name="splinePointIndex"></param>
        /// <param name="splineWidthList"></param>
        /// <returns></returns>
        public float GetWidthOnSpline(int splinePointIndex, List<float> splineWidthList)
        {
            float width = 0f, prevWidth = 0f, nextWidth = 0f;
            float distanceAlongSpline = 0f, distanceBetweenPathPoints = 0f;

            // The splineWidthList contains the width, typically set by user, at each position along the path
            // It can be the LBPath.widthList or a custom list set by LBObjPath
            int numWidths = (splineWidthList == null ? 0 : splineWidthList.Count);

            // Assume things have been set up correctly in CacheSplinePoints2() to avoid unnecessary Count operations
            int numSplineDistances = cachedCentreSplinePointDistancesLength;

            // The number of user-defined points should equal the number of Widths
            int numPathDistances = cachedPathPointDistancesLength;

            if (numWidths > 0 && numSplineDistances > 0 && numPathDistances == numWidths)
            {
                // If this is the first or last point, set the width's accordingly
                if (splinePointIndex == 0 && numWidths > 0) { width = splineWidthList[0]; }
                else if (splinePointIndex == numSplineDistances - 1 && numWidths > 0) { width = splineWidthList[numWidths - 1]; }
                else if (numSplineDistances > 2 && numPathDistances >= 2)
                {
                    distanceAlongSpline = cachedCentreSplinePointDistancesList[splinePointIndex];

                    // Iterate through cached point distance points until a point is found that is further away from the start
                    // of the path than the specified distance. This will give the user-defined width for the closest previous path position.
                    // We are using the Path Position Distances, rather than the Spline Point Distances because the Path Positions have
                    // matching user-defined widths (stored in widthList).
                    int positionIndex = 0;
                    while (cachedPathPointDistances[positionIndex] < distanceAlongSpline + 0.001f)
                    {
                        positionIndex++;
                        if (positionIndex >= numPathDistances - 1) { break; }
                    }

                    // Get the widths of the previous and next points closest to the Spline point on the centre spline
                    prevWidth = splineWidthList[positionIndex - 1];
                    if (positionIndex > numWidths - 1 && closedCircuit) { nextWidth = splineWidthList[0]; }
                    else { nextWidth = splineWidthList[positionIndex]; }

                    // Get the distance between the previous and next Path Position points (where the matching widths came from)
                    if (numPathDistances > positionIndex)
                    {
                        distanceBetweenPathPoints = cachedPathPointDistances[positionIndex] - cachedPathPointDistances[positionIndex - 1];

                        if (distanceBetweenPathPoints == 0f) { width = prevWidth; }
                        else { width = Mathf.Lerp(prevWidth, nextWidth, (distanceAlongSpline - cachedPathPointDistances[positionIndex - 1]) / distanceBetweenPathPoints); }
                    }
                }
            }

            return width;
        }

        /// <summary>
        /// Get the minimum width of the path
        /// </summary>
        /// <returns></returns>
        public float GetMinWidth()
        {
            float minWidth = Mathf.Infinity;

            if (widthList != null)
            {
                for (int w = 0; w < widthList.Count; w++)
                {
                    if (widthList[w] < minWidth) { minWidth = widthList[w]; }
                }
            }

            return minWidth;
        }

        public float GetMaxWidth()
        {
            float maxWidth = 0f;

            if (widthList != null)
            {
                for (int w = 0; w < widthList.Count; w++)
                {
                    if (widthList[w] > maxWidth) { maxWidth = widthList[w]; }
                }
            }

            return maxWidth;
        }

        #endregion

        #region Path Cache Methods

        /// <summary>
        /// Get the cached array of positions for the PositionType
        /// </summary>
        /// <param name="positionType"></param>
        /// <returns></returns>
        public Vector3[] GetCachedPathPoints(PositionType positionType, bool isSplinePointPosition)
        {
            if (positionType == PositionType.Left) { return cachedPathPointsLeftEdge; }
            else if (positionType == PositionType.Right) { return cachedPathPointsRightEdge; }
            else
            {
                if (isSplinePointPosition) { return cachedCentreSplinePoints; }
                else { return cachedPathPoints; }
            }
        }

        /// <summary>
        /// Caches a float array of the distance from the start for each point in the path
        /// This is measured as the distance through each of the earlier path points
        /// NOTE: There must be at least 2 points in the path.
        /// IMPORTANT: These distances will not match the cachedCentreSplineDistances.
        /// It is VERY likely that the distances will be SHORTER than the centre spline
        /// as these are the DIRECT or STRAIGHT distances between the points the user has
        /// created in the path.
        /// </summary>
        public void CachePathPointDistances()
        {
            cachedPathPointsLength = 0;
            cachedPathPointDistancesLength = 0;
            if (positionList != null)
            {
                int positionListCount = positionList.Count;

                // Pre-allocate distance list capacity
                if (cachedPathPointDistanceList == null) { cachedPathPointDistanceList = new List<float>(positionListCount + 1); }
                else { cachedPathPointDistanceList.Clear(); }

                // Cater for edge cases of no path points or only one
                if (positionListCount == 0)
                {
                    cachedPathPoints = null;
                    cachedPathPointDistances = null;
                    pathLength = 0f;
                    pathLengthToLastPoint = 0f;

                    if (pathType == PathType.MapPath)
                    {
                        cachedPathPointsLeftEdge = null;
                        cachedPathPointsRightEdge = null;
                    }
                    cachedPathPointsLength = 0;
                    cachedPathPointDistancesLength = 0;
                }
                else if (positionListCount == 1)
                {
                    cachedPathPoints = new Vector3[positionListCount];
                    cachedPathPointDistances = new float[positionListCount];
                    cachedPathPoints[0] = positionList[0];
                    cachedPathPointDistances[0] = 0f;

                    if (pathType == PathType.MapPath)
                    {
                        cachedPathPointsLeftEdge = new Vector3[positionListCount];
                        cachedPathPointsRightEdge = new Vector3[positionListCount];

                        // Add width for first point
                        cachedPathPointsLeftEdge[0] = positionListLeftEdge[0];
                        cachedPathPointsRightEdge[0] = positionListRightEdge[0];
                    }

                    pathLength = 0f;
                    pathLengthToLastPoint = 0f;
                    cachedPathPointsLength = 1;
                    cachedPathPointDistanceList.AddRange(cachedPathPointDistances);
                    cachedPathPointDistancesLength = 1;
                }
                else
                {
                    // If the path is a closed circuit, include the first point twice in the array - a second time at the end
                    // unless the first and last positions are already at the same point (Added extra condition v1.3.2 Beta 2g)
                    if (closedCircuit && positionList[0] != positionList[positionListCount - 1])
                    {
                        //Debug.Log("Adding extra cache point at end");
                        cachedPathPoints = new Vector3[positionListCount + 1];
                        cachedPathPointDistances = new float[positionListCount + 1];

                        if (pathType == PathType.MapPath)
                        {
                            cachedPathPointsLeftEdge = new Vector3[positionListCount + 1];
                            cachedPathPointsRightEdge = new Vector3[positionListCount + 1];
                        }
                    }
                    else
                    {
                        //Debug.Log("NOT Adding extra cache point at end");
                        cachedPathPoints = new Vector3[positionListCount];
                        cachedPathPointDistances = new float[positionListCount];

                        if (pathType == PathType.MapPath)
                        {
                            cachedPathPointsLeftEdge = new Vector3[positionListCount];
                            cachedPathPointsRightEdge = new Vector3[positionListCount];
                        }
                    }

                    float cumulativeDistance = 0f;
                    int positionToAdd = 0;
                    cachedPathPointsLength = (cachedPathPoints == null ? 0 : cachedPathPoints.Length);
                    cachedPathPointDistancesLength = (cachedPathPointDistances == null ? 0 : cachedPathPointDistances.Length);
                    for (int i = 0; i < cachedPathPointDistancesLength; i++)
                    {
                        cachedPathPointDistances[i] = cumulativeDistance;
                        if (i + 1 < positionListCount)
                        {
                            // We are iterating through the list, so get the distance between this point and the next point in the list
                            cumulativeDistance += Vector3.Distance(positionList[i], positionList[i + 1]);
                            positionToAdd = i;
                        }
                        else if (i + 1 == positionList.Count)
                        {
                            // We have reached the end of the list, so get the distance between this point and the first point
                            cumulativeDistance += Vector3.Distance(positionList[i], positionList[0]);
                            positionToAdd = i;
                        }
                        else
                        {
                            // We have gone just past the end of the list, so get the distance between the first point and the second point
                            // cumulativeDistance += Vector3.Distance(positionList[0], positionList[1]);
                            positionToAdd = 0;
                            cachedPathPoints[i] = positionList[0];
                        }

                        cachedPathPoints[i] = positionList[positionToAdd];
                        if (pathType == PathType.MapPath)
                        {
                            cachedPathPointsLeftEdge[i] = positionListLeftEdge[positionToAdd];
                            cachedPathPointsRightEdge[i] = positionListRightEdge[positionToAdd];
                        }
                    }

                    // Also cache the total length of the path
                    if (closedCircuit)
                    {
                        pathLength = cumulativeDistance;

                        // Cache the length of the path from first point to last point (not including distance back to first point)
                        pathLengthToLastPoint = cachedPathPointDistances[cachedPathPointDistancesLength - 2];
                    }
                    else
                    {
                        pathLength = cachedPathPointDistances[cachedPathPointDistancesLength - 1];

                        // Cache the length of the path from first point to last point (not including distance back to first point)
                        pathLengthToLastPoint = pathLength;
                    }

                    cachedPathPointDistanceList.AddRange(cachedPathPointDistances);
                }
            }
        }

        /// <summary>
        /// Get an array of the points along a path which are a cetain distance apart.
        /// NOTE: Only caches Centre spline points (not left/right spline points)
        /// </summary>
        public void CacheSplinePoints()
        {
            List<Vector3> splinePointList = new List<Vector3>();
            cachedCentreSplinePointsLength = 0;

            // v1.3.3 Beta 1a
            float maxDistanceAlongRoute = pathLengthToLastPoint;
            if (pathType == PathType.MapPath)
            {
                maxDistanceAlongRoute = pathLength + 0.001f;
            }

            // Get the number of vector3s specified by visualization Substeps
            //for (float distanceAlongRoute = 0; distanceAlongRoute < pathLengthToLastPoint; distanceAlongRoute += pathResolution)
            // v1.3.3 Beta 1a
            for (float distanceAlongRoute = 0; distanceAlongRoute < maxDistanceAlongRoute; distanceAlongRoute += pathResolution)
            {
                // Find the position in the path
                splinePointList.Add(GetPathPosition(distanceAlongRoute, PositionType.Centre));
            }

            int numPositions = positionList.Count;
            // Add the final point.
            splinePointList.Add(positionList[numPositions - 1]);

            // If the path is a closed circuit, include the first point twice in the array - a second time at the end
            // unless the first and last positions are already at the same point
            if (closedCircuit && splinePointList[0] != splinePointList[splinePointList.Count - 1])
            {
                // The final point is the beginning point
                splinePointList.Add(positionList[0]);
            }

            cachedCentreSplinePoints = splinePointList.ToArray();
            cachedCentreSplinePointsLength = cachedCentreSplinePoints.Length;
        }

        #region New Methods July 25 2018

        /// <summary>
        /// Caches a float array of each point in the path.
        /// Initialise the cachedPathPointDistances and sets them all to 0f.
        /// To populate distances call CacheSplinePoints2().
        /// </summary>
        public void CachePathPoints()
        {
            cachedPathPointsLength = 0;
            cachedPathPointDistancesLength = 0;
            if (positionList != null)
            {
                int positionListCount = positionList.Count;

                // Pre-allocate distance list capacity
                if (cachedPathPointDistanceList == null) { cachedPathPointDistanceList = new List<float>(positionListCount + 1); }
                else { cachedPathPointDistanceList.Clear(); }

                // Cater for edge cases of no path points or only one
                if (positionListCount == 0)
                {
                    cachedPathPoints = null;
                    cachedPathPointDistances = null;
                    pathLength = 0f;
                    pathLengthToLastPoint = 0f;

                    if (pathType == PathType.MapPath)
                    {
                        cachedPathPointsLeftEdge = null;
                        cachedPathPointsRightEdge = null;
                    }
                    cachedPathPointsLength = 0;
                    cachedPathPointDistancesLength = 0;
                }
                else if (positionListCount == 1)
                {
                    cachedPathPoints = new Vector3[positionListCount];
                    cachedPathPointDistances = new float[positionListCount];
                    cachedPathPoints[0] = positionList[0];
                    cachedPathPointDistances[0] = 0f;

                    if (pathType == PathType.MapPath)
                    {
                        cachedPathPointsLeftEdge = new Vector3[positionListCount];
                        cachedPathPointsRightEdge = new Vector3[positionListCount];

                        // Add width for first point
                        cachedPathPointsLeftEdge[0] = positionListLeftEdge[0];
                        cachedPathPointsRightEdge[0] = positionListRightEdge[0];
                    }

                    pathLength = 0f;
                    pathLengthToLastPoint = 0f;
                    cachedPathPointsLength = 1;
                    cachedPathPointDistanceList.AddRange(cachedPathPointDistances);
                    cachedPathPointDistancesLength = 1;
                }
                else
                {
                    int positionToAdd = 0;
                    pathLength = 0f;
                    pathLengthToLastPoint = 0f;
                    cachedPathPointsLength = positionListCount;
                    cachedPathPointDistancesLength = positionListCount;

                    // If the path is a closed circuit, include the first point twice in the array - a second time at the end
                    // unless the first and last positions are already at the same point (Added extra condition v1.3.2 Beta 2g)
                    if (closedCircuit && positionList[0] != positionList[positionListCount - 1])
                    {
                        //Debug.Log("Adding extra cache point at end");
                        cachedPathPointsLength += 1;
                        cachedPathPointDistancesLength += 1;
                    }

                    cachedPathPoints = new Vector3[cachedPathPointsLength];
                    cachedPathPointDistances = new float[cachedPathPointDistancesLength];

                    if (pathType == PathType.MapPath)
                    {
                        cachedPathPointsLeftEdge = new Vector3[cachedPathPointsLength];
                        cachedPathPointsRightEdge = new Vector3[cachedPathPointsLength];
                    }

                    for (int i = 0; i < cachedPathPointsLength; i++)
                    {
                        if (i + 1 < positionListCount)
                        {
                            // We are iterating through the list
                            positionToAdd = i;
                        }
                        else if (i + 1 == positionList.Count)
                        {
                            // We have reached the end of the list
                            positionToAdd = i;
                        }
                        else
                        {
                            // We have gone just past the end of the list
                            positionToAdd = 0;
                            cachedPathPoints[i] = positionList[0];
                        }

                        cachedPathPoints[i] = positionList[positionToAdd];
                        if (pathType == PathType.MapPath)
                        {
                            cachedPathPointsLeftEdge[i] = positionListLeftEdge[positionToAdd];
                            cachedPathPointsRightEdge[i] = positionListRightEdge[positionToAdd];
                        }
                    }

                    cachedPathPointDistanceList.AddRange(cachedPathPointDistances);
                }
            }
        }

        /// <summary>
        /// Get an array of the points and distances along a path which are a cetain distance apart.
        /// Cache path points (with an adjustment for closedCircuit)
        /// Initialise the cachedPathPointDistances array (values all 0)
        /// Initialise the cachedPathPointDistanceList (values all 0)
        /// Cache centre spline points (not left/right spline points)
        /// Cache centre spline point distances
        /// WARNING: NOT FULLY TESTED WITH CAMERA PATH or MAP PATH
        /// NOTE: Set isSplinesCached2 = false to force an update.
        /// </summary>
        public void CacheSplinePoints2 ()
        {
            if (isSplinesCached2)
            {
                // Double check if lists have been initialised
                isSplinesCached2 = (cachedCentreSplinePointList != null && cachedCentreSplinePointDistancesList != null);

                if (isSplinesCached2) { return; }
            }

            CachePathPoints();

            // Reset LBPath public variables
            cachedCentreSplinePointsLength = 0;
            cachedCentreSplinePointDistances = null;
            cachedCentreSplinePointDistancesLength = 0;

            // Re-use cached lists to minimise memory fragmentation caused by creating and destroying small list
            // If the list hasn't been initialised yet, create it and give it some capacity
            if (cachedCentreSplinePointList == null) { cachedCentreSplinePointList = new List<Vector3>(100); }
            else { cachedCentreSplinePointList.Clear(); }
            if (cachedCentreSplinePointDistancesList == null) { cachedCentreSplinePointDistancesList = new List<float>(100); }
            else { cachedCentreSplinePointDistancesList.Clear(); }

            // Initialise length variables
            float cumulativePathPointDistance = 0f;
            float cumulativeSplinePointDistance = 0f;
            float distanceRelativeToPoint = 0f;
            float pathSegmentLength = 0f;
            int numCachedPathPoints = (cachedPathPoints == null ? 0 : cachedPathPoints.Length);

            // Loop through the list of points
            for (int point = 0; point < numCachedPathPoints - 1; point++)
            {
                // Get the 4 points needed for Catmull-Rom

                point2 = cachedPathPoints[point];
                point3 = cachedPathPoints[point + 1];

                if (point == 0)
                {
                    if (numCachedPathPoints == 2)
                    {
                        // Point1 and Point4 need a different calculation
                        point1 = point2 + (point2 - point3);
                        point4 = point3 + (point3 - point2);
                    }
                    else
                    {
                        // Point1 needs a different calculation
                        point1 = point2 + (point2 - point3);
                        point4 = cachedPathPoints[point + 2];
                    }
                }
                else if (point == numCachedPathPoints - 2)
                {
                    // Point4 needs a different calculation
                    point1 = cachedPathPoints[point - 1];
                    point4 = point3 + (point3 - point2);
                }
                else
                {
                    point1 = cachedPathPoints[point - 1];
                    point4 = cachedPathPoints[point + 2];
                }

                // Calculate (approximately) the length of this path segment.
                // This is the estimated distance between the user-created path points on the CatmullRom path.
                // Increase the last parameter to improve precision
                pathSegmentLength = MeasureSplineSegmentLength(point1, point2, point3, point4, 0f, 1f, 100);

                // Loop through and place spline points at regular intervals (the pathResolution distance apart)
                // The while loop condition ensures we stop doing this when we have gone past the current segment
                while (cumulativeSplinePointDistance <= cumulativePathPointDistance + pathSegmentLength)
                {
                    // Calculate our distance along the path from point2
                    distanceRelativeToPoint = cumulativeSplinePointDistance - cumulativePathPointDistance;

                    // The last parameter is the maximum amount of acceptable error in the t-value, so decreasing
                    // this value will increase precision accordingly
                    cachedCentreSplinePointList.Add(FindPointOnSegment(point1, point2, point3, point4, distanceRelativeToPoint, pathSegmentLength, 0.001f));

                    //if (float.IsInfinity(cachedCentreSplinePointList[cachedCentreSplinePointList.Count - 1].x))
                    //{
                    //    Debug.Log("[DEBUG] cachedCentreSplinePointList.Add " + point1 + "," + point2 + "," + point3 + "," + point4 + " d:" + distanceRelativeToPoint + " sgmtl:" + pathSegmentLength + " " + cachedCentreSplinePointList[cachedCentreSplinePointList.Count - 1]);
                    //}

                    cachedCentreSplinePointDistancesList.Add(cumulativeSplinePointDistance);

                    // Increment the distance along the spline for the next spline point
                    cumulativeSplinePointDistance += pathResolution;
                }

                // Use this to cache path point distances
                cachedPathPointDistances[point] = cumulativePathPointDistance;
                cachedPathPointDistanceList[point] = cumulativePathPointDistance;

                // Increment the cumulative path point distance
                cumulativePathPointDistance += pathSegmentLength;
            }

            // Set the distance for the last point and add the last spline point
            if (numCachedPathPoints > 0)
            {
                cachedPathPointDistances[numCachedPathPoints - 1] = cumulativePathPointDistance;
                cachedPathPointDistanceList[numCachedPathPoints - 1] = cumulativePathPointDistance;

                // Add in last spline point if not exactly at end of path
                if (cumulativePathPointDistance / pathResolution > 0.0001f)
                {
                    // Add the last user-defined point
                    cachedCentreSplinePointList.Add(cachedPathPoints[closedCircuit ? numCachedPathPoints - 2 : numCachedPathPoints - 1]);
                    cachedCentreSplinePointDistancesList.Add(cumulativePathPointDistance);
                }
            }

            int numSplinePoints = (cachedCentreSplinePointList == null ? 0 : cachedCentreSplinePointList.Count);

            // If the path is a closed circuit, include the first point twice in the array - a second time at the end
            // unless the first and last positions are already at the same point
            if (closedCircuit && numSplinePoints > 0 && cachedCentreSplinePointList[0] != cachedCentreSplinePointList[numSplinePoints - 1])
            {
                // The final point is the beginning point
                cachedCentreSplinePointList.Add(positionList[0]);
                cachedCentreSplinePointDistancesList.Add(cumulativeSplinePointDistance);
                numSplinePoints++;
            }

            cachedCentreSplinePointsLength = numSplinePoints;
            cachedCentreSplinePointDistancesLength = (cachedCentreSplinePointDistancesList == null ? 0 : cachedCentreSplinePointDistancesList.Count);

            // Length of the path in metres using the (fewer) path points (using this method, now same as splineLength)
            pathLength = cumulativePathPointDistance;

            // Length or distance to the Last Point in metres using the (fewer) path points
            pathLengthToLastPoint = cumulativePathPointDistance;

            // Length of the spline in metres using the spline points on the path (using this method, now same as pathLength)
            splineLength = cumulativePathPointDistance;

            // Length or distance to the Last Point in metres using the spline points (more accurate than pathLengthToLastPoint)
            splineLengthToLastPoint = splineLength;

            // Length of distance from start to the second last point in metres (used for closed circuits)
            splineDistTo2ndLastPoint = (cachedCentreSplinePointDistancesLength > 1 ? cachedCentreSplinePointDistancesList[cachedCentreSplinePointDistancesLength-2] : 0f);

            // Data has been cached so update flag
            isSplinesCached2 = true;
        }

        /// <summary>
        /// Calculate the approximate distance between the given min and max t-values for a given segment of the Catmull-Rom spline
        /// (determined by the points p1, p2, p3 and p4). T intervals determines how many intervals the segment is broken into, so
        /// increasing t intervals will increase precision accordingly.
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        /// <param name="p4"></param>
        /// <param name="minTValue"></param>
        /// <param name="maxTValue"></param>
        /// <param name="tIntervals"></param>
        /// <returns></returns>
        public float MeasureSplineSegmentLength (Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, float minTValue, float maxTValue, int tIntervals)
        {
            // Calculate (approximately) the length of this path segment
            float splineSegmentLength = 0f;
            Vector3 interpolatedPathPoint1 = CatmullRom(p1, p2, p3, p4, minTValue), interpolatedPathPoint2;
            // Loop through the path from minTValue to maxTValue in tIntervals
            float tValueRange = maxTValue - minTValue;
            for (int tInt = 0; tInt < tIntervals; tInt++)
            {
                // Get the point on the Catmull-Rom spline at the given t-value
                interpolatedPathPoint2 = CatmullRom(p1, p2, p3, p4, ((float)tInt / (float)tIntervals * tValueRange) + minTValue);
                // Add the distance between this point and the last point to the segment length
                splineSegmentLength += Vector3.Distance(interpolatedPathPoint1, interpolatedPathPoint2);
                // Remember this path point
                interpolatedPathPoint1 = interpolatedPathPoint2;
            }
            // Return the calculated segment length
            return splineSegmentLength;
        }

        /// <summary>
        /// Iteratively calculate the t-value required for Catmull-Rom to place a point a given distance along a segment, then use it to
        /// find the point on the segment. MaxError determines the maximum amount of acceptable error in the t-value.
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        /// <param name="p4"></param>
        /// <param name="distAlongSegment"></param>
        /// <param name="segmentLength"></param>
        /// <param name="maxError"></param>
        /// <returns></returns>
        public Vector3 FindPointOnSegment (Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, float distAlongSegment, float segmentLength, float maxError)
        {
            // Get a good first guess for the tValue
            float tValue = distAlongSegment / segmentLength, nextTValue, tVariance;

            // Continuously loop until we get what we need, but exit out early if we reach a maximum number of interations
            int iterationCount = 0;
            while (iterationCount < 1000)
            {
                // Assume that the distance is linear, just so that we can try and calculate a more accurate t-value
                nextTValue = tValue + ((distAlongSegment - MeasureSplineSegmentLength(p1, p2, p3, p4, 0f, tValue, 100)) / segmentLength);

                // This can sometimes create points a long way off the path
                //nextTValue = tValue + ((distAlongSegment - MeasureSplineSegmentLength(p1, p2, p3, p4, 0f, tValue, Mathf.CeilToInt(100f * distAlongSegment/ segmentLength))) / segmentLength);

                // If tValue is very large (e.g. -1.744179E+13), nextTValue will be NaN.
                if (float.IsNaN(nextTValue))
                {
                    //THIS WILL RETURN +/- Infinity for CatmullRom... so reset to first guess.
                    tValue = distAlongSegment / segmentLength;
                    break;
                }

                tVariance = tValue > nextTValue ? tValue - nextTValue : nextTValue - tValue;

                tValue = nextTValue;

                if (tVariance < maxError) { break; }
                else { iterationCount++; }
            }

            return CatmullRom(p1, p2, p3, p4, tValue);
        }

        #endregion

        /// <summary>
        /// Get an array of cummulative distances (from the start) for the array
        /// of centre points in the spline. These are typically placed along the path
        /// at a distance apart being equal to the pathResolution.
        /// Calls CacheSplinePoints() first. 
        /// NOTE: CachePathPointDistances() is required as it caches the Path Points.
        /// </summary>
        /// <returns></returns>
        public float[] CacheSplinePointDistances()
        {
            cachedCentreSplinePointDistances = null;
            cachedCentreSplinePointDistancesLength = 0;

            //Debug.Log("[DEBUG] CacheSplinePointDistances()");

            CacheSplinePoints();

            if (cachedCentreSplinePoints != null)
            {
                int numSplinePoints = cachedCentreSplinePointsLength;

                // Pre-allocate distance list capacity
                if (cachedCentreSplinePointDistancesList == null) { cachedCentreSplinePointDistancesList = new List<float>(numSplinePoints + 1); }
                else { cachedCentreSplinePointDistancesList.Clear(); }

                // Cater for edge cases of no path points or only one
                if (numSplinePoints == 0)
                {
                    cachedCentreSplinePointDistances = null;
                    splineLength = 0f;
                    splineLengthToLastPoint = 0f;
                    splineDistTo2ndLastPoint = 0f;
                    cachedCentreSplinePointDistancesLength = 0;
                }
                else if (numSplinePoints == 1)
                {
                    cachedCentreSplinePointDistances = new float[numSplinePoints];
                    cachedCentreSplinePointDistances[0] = 0f;
                    splineLength = 0f;
                    splineLengthToLastPoint = 0f;
                    splineDistTo2ndLastPoint = 0f;
                    cachedCentreSplinePointDistancesLength = 1;
                    cachedCentreSplinePointDistancesList.AddRange(cachedCentreSplinePointDistances);
                }
                else
                {
                    // If the path is a closed circuit, include the first point twice in the array - a second time at the end
                    // unless the first and last positions are already at the same point
                    if (closedCircuit && cachedCentreSplinePoints[0] != cachedCentreSplinePoints[numSplinePoints - 1])
                    {
                        cachedCentreSplinePointDistances = new float[numSplinePoints + 1];
                    }
                    else
                    {
                        cachedCentreSplinePointDistances = new float[numSplinePoints];
                    }

                    float cumulativeDistance = 0f;
                    cachedCentreSplinePointDistancesLength = (cachedCentreSplinePointDistances == null ? 0 : cachedCentreSplinePointDistances.Length);

                    // v2.04
                    if (cachedCentreSplinePointDistancesLength > 0) { cachedCentreSplinePointDistances[0] = 0f; }

                    for (int i = 1; i < cachedCentreSplinePointDistancesLength; i++)
                    {
                        if (i < numSplinePoints - 1)
                        {
                            // We are iterating through the list, so get the distance between this point and the next point in the list
                            // v2.0.4 code
                            cumulativeDistance += pathResolution;
                            cachedCentreSplinePointDistances[i] = cumulativeDistance;
                        }
                        else if (i < numSplinePoints)
                        {
                            // Last point
                            cumulativeDistance += Vector3.Distance(cachedCentreSplinePoints[i - 1], cachedCentreSplinePoints[i]);
                            cachedCentreSplinePointDistances[i] = cumulativeDistance;
                        }
                        // v1.3.3 Beta 1a (closed circuit)
                        else if (i + 1 == numSplinePoints && closedCircuit)
                        {
                            // We have reached the end of the list, so get the distance between this point and the first point
                            cumulativeDistance += Vector3.Distance(cachedCentreSplinePoints[i], cachedCentreSplinePoints[0]);
                        }
                        else
                        {
                            // We have gone just past the end of the list, so get the distance between the first point and the second point
                            // cumulativeDistance += Vector3.Distance(splineCentrePoints[0], splineCentrePoints[1]);
                        }
                    }

                    // Also cache the total length of the path
                    if (closedCircuit)
                    {
                        splineLength = cumulativeDistance;

                        // Cache the length of the path from first point to last point (not including distance back to first point)
                        splineLengthToLastPoint = cachedCentreSplinePointDistances[cachedCentreSplinePointDistancesLength - 2];

                        // Cache the distance from the first point to the second last point.
                        if (cachedCentreSplinePointDistancesLength > 2) { splineDistTo2ndLastPoint = cachedCentreSplinePointDistances[cachedCentreSplinePointDistancesLength - 3]; }
                        else splineDistTo2ndLastPoint = 0f;
                    }
                    else
                    {
                        splineLength = cachedCentreSplinePointDistances[cachedCentreSplinePointDistancesLength - 1];

                        // Cache the length of the path from first point to last point (not including distance back to first point)
                        splineLengthToLastPoint = splineLength;

                        // Cache the distance from the first point to the second last point.
                        splineDistTo2ndLastPoint = cachedCentreSplinePointDistances[cachedCentreSplinePointDistancesLength - 2];
                    }

                    cachedCentreSplinePointDistancesList.AddRange(cachedCentreSplinePointDistances);
                }
            }
            return cachedCentreSplinePointDistances;
        }

        /// <summary>
        /// Clearing the path point distances is sometime necessary when refreshing the
        /// edges of a path in the scene view. This ensures the gizmos are drawn in the
        /// correct location.
        /// </summary>
        public void ClearCachePathPointDistances()
        {
            cachedPathPointDistances = null;
            if (cachedPathPointDistanceList != null) { cachedPathPointDistanceList.Clear(); }
        }

        #endregion

        #region Path Nearest Point Methods

        /// <summary>
        /// Get the nearest (next) spline point along the spline.
        /// Prerequisite: CacheSplinePoints2()
        /// </summary>
        /// <param name="distance"></param>
        /// <returns></returns>
        public int GetNearestSplinePoint(float distance)
        {
            int _nearestPointIdx = -1;

            if (cachedCentreSplinePointList == null) { Debug.Log("LBPath.GetNearestSplinePoint - centre spline points aren't cached."); }
            else if (cachedCentreSplinePointDistancesList == null) { Debug.Log("LBPath.GetNearestSplinePoint - centre spline point distances aren't cached."); }
            else if (distance <= 0f) { _nearestPointIdx = 0; }
            else
            {
                int numberOfPathPoints = cachedCentreSplinePointsLength;

                // If no points in path, return -1
                if (numberOfPathPoints == 0) { _nearestPointIdx = -1; }
                // If only one point in path or we're right at the beginning, return the first point
                else if (numberOfPathPoints == 1 || distance < 0.001f) { _nearestPointIdx = 0; }
                else
                {
                    // Mathf.Repeat works as a modulo (%) operator for floats
                    distance = Mathf.Repeat(distance, splineLength);

                    // Start as second point
                    _nearestPointIdx = 1;

                    // Iterate through path points until a point is found that is further away from the start
                    // of the path than the specified distance
                    while (distance > cachedCentreSplinePointDistancesList[_nearestPointIdx])
                    {
                        //_nearestPointIdx++;
                        if (++_nearestPointIdx >= cachedCentreSplinePointDistancesLength) { _nearestPointIdx = 0; break; }
                    }
                }
            }

            return _nearestPointIdx;
        }

        /// <summary>
        /// Given a point along the path, find the nearest cached spline point.
        /// NOTE: The cached spline points are closely spaced based on the path resolution.
        /// The path points are the cached position list, which may include an additional point if the circuit is closed.
        /// Will Return -1 if one is not found.
        /// Prerequisite: CacheSplinePoints2()
        /// </summary>
        /// <param name="pathIndex"></param>
        /// <returns></returns>
        public int GetNearestSplinePointFromPath(int pathIndex)
        {
            int _nearestPointIdx = -1;

            if (pathIndex < cachedPathPointDistancesLength)
            {
                float pathDistance = cachedPathPointDistances[pathIndex];

                // Find the next closest spline point, based on path distance
                //_nearestPointIdx = cachedCentreSplinePointDistancesList.FindIndex(pt => pt >= pathDistance);

                _nearestPointIdx = 0;

                while (pathDistance > cachedCentreSplinePointDistancesList[_nearestPointIdx])
                {
                    if (++_nearestPointIdx >= cachedCentreSplinePointDistancesLength) { _nearestPointIdx = 0; break; }
                }

                // Now look at the previous one to see if it is closer.

            }

            return _nearestPointIdx;
        }

        #endregion

        #region Path Forwards Methods

        /// <summary>
        /// Get the forwards direction from a position point in the path
        /// </summary>
        /// <param name="positionIndex"></param>
        /// <returns></returns>
        public Vector3 GetForwards(int positionIndex)
        {
            Vector3 forwards = Vector3.zero;

            if (cachedPathPointDistances == null) { CachePathPointDistances(); }

            int numPositions = cachedPathPointDistancesLength;
            float forwardDistance = 0f, backDistance = 0f;

            // Validate things
            if (cachedPathPoints != null && positionList != null && positionIndex < numPositions)
            {
                if (positionIndex < cachedPathPointsLength)
                {
                    // Is this the last point?
                    if (positionIndex == numPositions - 1 && numPositions != 1)
                    {
                        forwardDistance = cachedPathPointDistances[positionIndex];
                        backDistance = cachedPathPointDistances[positionIndex] - 2f;
                    }
                    else if (positionIndex == 0 && numPositions != 1)
                    {
                        forwardDistance = cachedPathPointDistances[positionIndex] + 2f;
                        backDistance = cachedPathPointDistances[positionIndex];
                    }
                    else
                    {
                        forwardDistance = cachedPathPointDistances[positionIndex] + 2f;
                        backDistance = cachedPathPointDistances[positionIndex] - 2f;
                    }

                    if (numPositions != 1)
                    {
                        forwardDistance = Mathf.Clamp(forwardDistance, 0f, pathLength);
                        backDistance = Mathf.Clamp(backDistance, 0f, pathLength);

                        // (A little more accurate)
                        forwards = GetPathPosition(forwardDistance, PositionType.Centre) - GetPathPosition(backDistance, PositionType.Centre);
                    }
                    else { forwards = Vector3.right; }

                    //if (positionIndex == numPositions - 1) { forwards *= -1f; }
                }
            }
            return forwards;
        }

        /// <summary>
        /// Get the fowards direction from a user-placed path point. This uses the
        /// cachedCentreSplinePointDistances which are the points between the path segments
        /// that are pathResolution apart and uses an optimised position algorithm.
        /// distanceToLookAhead determines how far to look ahead of the current point to
        /// determine the forwards direction. Near the end, it looks backwards.
        /// PREREQUISITE: CacheSplinePoints2()
        /// </summary>
        /// <param name="positionIndex"></param>
        /// <param name="distanceToLookAhead"></param>
        /// <returns></returns>
        public Vector3 GetForwardsFast(int positionIndex, float distanceToLookAhead = 1f)
        {
            Vector3 forwardDir = Vector3.zero;

            int numPositions = cachedPathPointDistancesLength;

            if (numPositions > 1 && cachedCentreSplinePointDistancesList != null && positionIndex < cachedPathPointDistancesLength)
            {
                // Treat end of path as a special case
                if (positionIndex == numPositions - 1 && cachedCentreSplinePointsLength > 0)
                {
                    if (splineLength < pathResolution) { forwardDir = positionList[positionIndex] - positionList[positionIndex - 1]; }
                    // Is distance to last spline point > 0.5 metres?
                    else if (cachedCentreSplinePointDistancesList[cachedCentreSplinePointDistancesLength - 1] < splineLength - 0.5f)
                    {
                        // Use the previous spline point to help determine forwards direction
                        forwardDir = positionList[positionIndex] - cachedCentreSplinePointList[cachedCentreSplinePointsLength - 1];
                    }
                    else
                    {
                        forwardDir = positionList[positionIndex] - GetPathPosition(splineLength - 1f, 1);
                    }
                }
                else
                {
                    float distanceAlongPath = cachedPathPointDistanceList[positionIndex];

                    if (distanceAlongPath < splineLength - distanceToLookAhead)
                    {
                        forwardDir = GetPathPosition(distanceAlongPath + distanceToLookAhead, 1) - positionList[positionIndex];
                    }
                    else
                    {
                        // Near or at the end, so look backwards along path
                        forwardDir = positionList[positionIndex] - GetPathPosition(distanceAlongPath - distanceToLookAhead, 1);
                    }
                }
            }

            return forwardDir;
        }

        /// <summary>
        /// Get the forwards direction at the end of the path. This is a special case and can be problematic (for several reasons).
        /// Forwards is returned normalized.
        /// PREREQUISITE: CacheSplinePoints2()
        /// </summary>
        /// <returns></returns>
        public Vector3 GetForwardsAtEnd()
        {
            Vector3 forwardDir = Vector3.zero;

            if (cachedPathPointsLength > 1 && cachedCentreSplinePointsLength > 1 && cachedCentreSplinePointDistancesLength > 1 && cachedCentreSplinePointDistancesList != null)
            {
                if (splineLength < pathResolution) { forwardDir = positionList[cachedPathPointsLength-1] - positionList[cachedPathPointsLength - 2]; }
                // Is distance to last spline point > 0.5 metres?
                else if (cachedCentreSplinePointDistancesList[cachedCentreSplinePointDistancesLength - 1] < splineLength - 0.5f)
                {
                    // Use the previous spline point to help determine forwards direction
                    forwardDir = positionList[cachedPathPointsLength-1] - cachedCentreSplinePointList[cachedCentreSplinePointsLength - 1];
                }
                else
                {
                    forwardDir = positionList[cachedPathPointsLength-1] - GetPathPosition(splineLength - 1f, 1);
                }
            }

            return forwardDir.normalized;
        }

        /// <summary>
        /// Get the forwards direction from a spline point in the path. This uses the
        /// cachedCentreSplinePointDistances which are the points between the path segments
        /// that are pathResolution apart.
        /// </summary>
        /// <param name="splinePointIndex"></param>
        /// <returns></returns>
        public Vector3 GetForwardsOnSpline(int splinePointIndex)
        {
            Vector3 forwards = Vector3.zero;
            bool forwardsDefined = false;
            if (cachedCentreSplinePointDistances != null && cachedCentreSplinePoints != null)
            {
                int numPositions = cachedCentreSplinePointDistancesLength;
                float forwardDistance = 0f, backDistance = 0f;

                // Validate things
                if (splinePointIndex < numPositions)
                {
                    if (splinePointIndex < cachedCentreSplinePointsLength)
                    {
                        // Is this the last point?
                        if (splinePointIndex == numPositions - 1 && numPositions != 1)
                        {
                            /// Changed v1.3.3 Beta 1a
                            //If available, use last position point edges so spline and position point left/ right edges are same.
                            if (closedCircuit && cachedPathPointDistances != null)
                            {
                                forwards = GetForwards(0);
                                forwardsDefined = true;
                            }
                            else
                            {
                                // If this is the last point, use the last point and go back towards
                                // start by 1 metres
                                forwardDistance = cachedCentreSplinePointDistances[splinePointIndex];
                                backDistance = cachedCentreSplinePointDistances[splinePointIndex] - 1f;

                                //Debug.Log("LBPath.GetForwardsOnSpline - cachedPathPointDistances is not defined");
                            }
                        }
                        else if (splinePointIndex == 0 && numPositions != 1)
                        {
                            // If available, use the first position so that splinepoint start edges are same as position left/right edges
                            if (cachedPathPointDistances != null)
                            {
                                forwards = GetForwards(0);
                                forwardsDefined = true;
                            }
                            else
                            {
                                forwardDistance = cachedCentreSplinePointDistances[splinePointIndex] + 1f;
                                backDistance = cachedCentreSplinePointDistances[splinePointIndex];
                            }
                        }
                        else
                        {
                            forwardDistance = cachedCentreSplinePointDistances[splinePointIndex] + 1f;
                            backDistance = cachedCentreSplinePointDistances[splinePointIndex] - 1f;
                        }

                        if (!forwardsDefined)
                        {
                            if (numPositions != 1)
                            {
                                forwardDistance = Mathf.Clamp(forwardDistance, 0f, splineLength);
                                backDistance = Mathf.Clamp(backDistance, 0f, splineLength);

                                forwards = GetPathPosition(forwardDistance, PositionType.Centre, true) - GetPathPosition(backDistance, PositionType.Centre, true);
                            }
                            else { forwards = Vector3.right; }
                        }
                        //if (splinePointIndex == numPositions - 1) { forwards *= -1f; }
                    }
                }
            }
            return forwards;
        }

        #endregion

        #region Path Distance Calculation Methods

        /// <summary>
        /// Get the minimum distance between points in the path.
        /// </summary>
        /// <returns></returns>
        public float GetMinDistanceBetweenPoints()
        {
            float minDistance = Mathf.Infinity;
            float deltaDistance = 0f;

            // Populate the array of path distances
            CachePathPointDistances();

            if (cachedPathPointDistances != null)
            {
                // Needs to be at least 2 points in path
                if (cachedPathPointDistancesLength > 1)
                {
                    for (int i = 1; i < cachedPathPointDistancesLength; i++)
                    {
                        deltaDistance = cachedPathPointDistances[i] - cachedPathPointDistances[i - 1];

                        if (deltaDistance < minDistance) { minDistance = deltaDistance; }
                    }
                }
            }

            return minDistance;
        }

        #endregion

        #region Create Path From Road Methods

        /// <summary>
        /// This method takes input a list of LBRoads which consists of left, centre and right splines.
        /// Typically it would get the splines from a third party product like EasyRoads3D Pro
        /// </summary>
        /// <param name="lbRoadList"></param>
        /// <param name="splinePointFilterSize"></param>
        public void CreatePathFromRoad(List<LBRoad> lbRoadList, int splinePointFilterSize)
        {
            LBRoad lbRoad;
            List<Vector3> tempPathSortList = new List<Vector3>();

            if (lbRoadList == null) { return; }
            else if (lbRoadList.Count == 0) { return; }

            // Clear the lists
            positionList.Clear();
            positionListLeftEdge.Clear();
            positionListRightEdge.Clear();
            widthList.Clear();

            // Loop through the list of roads
            for (int i = 0; i < lbRoadList.Count; i++)
            {
                lbRoad = lbRoadList[i];

                if (lbRoad != null)
                {
                    // Get the list of spline points for this road if it is selected in the list
                    if (lbRoad.isSelected)
                    {
                        // Get the spline points
                        lbRoad.centreSpline = LBIntegration.GetERRoadSplinePoints(lbRoad, LBRoad.SplineType.CentreSpline, splinePointFilterSize);
                        lbRoad.leftSpline = LBIntegration.GetERRoadSplinePoints(lbRoad, LBRoad.SplineType.LeftSpline, splinePointFilterSize);
                        lbRoad.rightSpline = LBIntegration.GetERRoadSplinePoints(lbRoad, LBRoad.SplineType.RightSpline, splinePointFilterSize);

                        // Validate the returned data then add the points the path
                        if (lbRoad.centreSpline == null)
                        {
                            Debug.LogWarning("LBPath.CreatePathFromRoad no centre spline points to add to update path for road: " + lbRoad.roadName);
                        }
                        else if (lbRoad.centreSpline.Length == 0)
                        {
                            Debug.LogWarning("LBPath.CreatePathFromRoad no centre spline points to add to update path for road: " + lbRoad.roadName);
                        }
                        else if (lbRoad.leftSpline == null)
                        {
                            Debug.LogWarning("LBPath.CreatePathFromRoad no left spline points to add to update path for road: " + lbRoad.roadName);
                        }
                        else if (lbRoad.leftSpline.Length == 0)
                        {
                            Debug.LogWarning("LBPath.CreatePathFromRoad no left spline points to add to update path for road: " + lbRoad.roadName);
                        }
                        else if (lbRoad.rightSpline == null)
                        {
                            Debug.LogWarning("LBPath.CreatePathFromRoad no right spline points to add to update path for road: " + lbRoad.roadName);
                        }
                        else if (lbRoad.rightSpline.Length == 0)
                        {
                            Debug.LogWarning("LBPath.CreatePathFromRoad no right spline points to add to update path for road: " + lbRoad.roadName);
                        }
                        else if (lbRoad.centreSpline.Length != lbRoad.leftSpline.Length)
                        {
                            Debug.LogWarning("LBPath.CreatePathFromRoad centre and left splines do not have the same number of points for road: " + lbRoad.roadName);
                        }
                        else if (lbRoad.leftSpline.Length != lbRoad.rightSpline.Length)
                        {
                            Debug.LogWarning("LBPath.CreatePathFromRoad left and right splines do not have the same number of points for road: " + lbRoad.roadName);
                        }
                        else
                        {
                            for (int steps = 0; steps < 3; steps++)
                            {
                                if (steps == 0) { tempPathSortList.AddRange(lbRoad.centreSpline); }
                                else if (steps == 1) { tempPathSortList.AddRange(lbRoad.leftSpline); }
                                else { tempPathSortList.AddRange(lbRoad.rightSpline); }

                                // Reverse the list of points if required
                                if (lbRoad.isReversed) { tempPathSortList.Reverse(); }

                                // Add the height above the terrain (or in this case the road)
                                for (int p = 0; p < tempPathSortList.Count; p++)
                                {
                                    tempPathSortList[p] = new Vector3(tempPathSortList[p].x, tempPathSortList[p].y + heightAboveTerrain, tempPathSortList[p].z);
                                }

                                // Add the modified path list to the pathPoints lists
                                if (steps == 0) { positionList.AddRange(tempPathSortList); }
                                else if (steps == 1) { positionListLeftEdge.AddRange(tempPathSortList); }
                                else { positionListRightEdge.AddRange(tempPathSortList); }

                                tempPathSortList.Clear();
                            }

                            // Calculate the widths
                            for (int p = 0; p < positionListLeftEdge.Count; p++)
                            {
                                widthList.Add(Vector3.Distance(positionListLeftEdge[p], positionListRightEdge[p]));
                            }
                            selectedList.Clear();

                        }
                    }
                }
            }
        }

        #endregion

        #region Path Gizmos Point Display Scale
    
        /// <summary>
        /// Given a integer slider scale from 1 to 5, set the gizmo
        /// point display scale. NOTE: It is not a direct conversion
        /// from int to float.
        /// </summary>
        /// <param name="pointDisplayScaleInt"></param>
        public void SetPointDisplayScale(int pointDisplayScaleInt)
        {
            switch (pointDisplayScaleInt)
            {
                case 1:
                    pointDisplayScale = 0.2f;
                    break;
                case 2:
                    pointDisplayScale = 0.5f;
                    break;
                case 3:
                    pointDisplayScale = 0.8f;
                    break;
                case 5:
                    pointDisplayScale = 1.5f;
                    break;
                default:
                    pointDisplayScale = 1f;
                    break;
            }
        }

        /// <summary>
        /// Get an integer slider scale from 1 to 5 from the
        /// pointDisplayScale float value. NOTE: It is not a
        /// direct conversion from float to int.
        /// </summary>
        /// <returns></returns>
        public int GetPointDisplayScaleAsInt()
        {
            if (pointDisplayScale <= 0.2f) { return 1; }
            else if (pointDisplayScale <= 0.5f) { return 2; }
            else if (pointDisplayScale <= 0.8f) { return 3; }
            else if (pointDisplayScale <= 1f) { return 4; }
            else return 5;
        }

        #endregion

        #endregion

        #region Public Static Methods

        #region CatmullRom Methods

        /// <summary>
        /// Catmull-Rom - a path smoothing algorithm developed by Edwin Catmull and Raphael Rom
        /// It is a curve that goes through 4 points (p1-p4) but only draws points p2 and p3.
        /// No idea how this works, but it does
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        /// <param name="i"></param>
        /// <returns></returns>
        public static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float i)
        {
            float i2 = i * i;
            return 0.5f * ((2f * p1) + (-p0 + p2) * i + (2 * p0 - 5 * p1 + 4 * p2 - p3) * i2 + (-p0 + 3 * p1 - 3 * p2 + p3) * (i2 * i));
        }

        #endregion

        /// <summary>
        /// Get the 2D bounds of Path splines (supplied as params)
        /// returns minX, minZ, maxX, maxZ worldspace boundaries.
        /// NOTE: Assumes left and right splines have equal number of points.
        /// See also LBObjPath.GetSurroundBounds().
        /// </summary>
        /// <param name="splineLeft"></param>
        /// <param name="splineRight"></param>
        /// <returns></returns>
        public static Vector4 GetSplineBounds(Vector3[] splineLeft, Vector3[] splineRight)
        {
            Vector4 bounds = new Vector4(float.PositiveInfinity, float.PositiveInfinity, float.NegativeInfinity, float.NegativeInfinity);

            int numSplinePoints = (splineLeft == null ? 0 : splineLeft.Length);
            // Ensure both splines have the same number of points
            if (numSplinePoints == (splineRight == null ? 0 : splineRight.Length))
            {
                Vector3 pt = Vector3.zero;
                for (int ptIdx = 0; ptIdx < numSplinePoints; ptIdx++)
                {
                    // Check left path point
                    pt = splineLeft[ptIdx];
                    // Check xMin, zMin
                    if (pt.x < bounds.x) { bounds.x = pt.x; }
                    if (pt.z < bounds.y) { bounds.y = pt.z; }
                    // Check xMax, zMax
                    if (pt.x > bounds.z) { bounds.z = pt.x; }
                    if (pt.z > bounds.w) { bounds.w = pt.z; }

                    // Check right path point
                    pt = splineRight[ptIdx];
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
    }
}