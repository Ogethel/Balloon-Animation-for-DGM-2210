using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace LandscapeBuilder
{
    [System.Serializable]
    public class LBLandscapeMesh
    {
        // Landscape mesh class
        #region Enumerations
        public enum MeshPlacingMode
        {
            Height,
            Inclination,
            HeightAndInclination,
            ConstantInfluence,
            Map,
            HeightInclinationMap
        }

        public enum MeshPlacementSpeed
        {
            BestPlacement = 3,
            FastPlacement = 5,
            FastestPlacement = 10
        }

        #endregion

        #region Variables and Properties

        public Mesh mesh;
        public List<Material> materials;
        public Vector3 offset;

        // Added v1.3.1 Beta 8c
        // Use prefab rather than a mesh
        public bool usePrefab;
        public GameObject prefab;

        // Added v1.3.5 Beta 2b
        public bool isCombineMesh;
        public bool isCreateCollider;
        public bool isRemoveEmptyGameObjects;

        public int maxMeshes;

        public float minScale;
        public float maxScale;

        public float minProximity;
        public bool randomiseYRotation;
        // Added v1.3.5 Beta 1c
        [Range(-359.9f, 359.9f)] public float fixedYRotation;
        // Added v1.3.6 Beta 1b - used to correct a model or prefab import problem
        [Range(-359.9f, 359.9f)] public float XRotation;
        [Range(-359.9f, 359.9f)] public float ZRotation;

        // Added v1.3.5 Beta 3c - Should the mesh be aligned with the terrain normal?
        public bool isTerrainAligned;

        public float minHeight;
        public float maxHeight;
        public float minInclination;
        public float maxInclination;

        // Added v1.1 Beta 8
        public Texture2D map;
        public Color mapColour;
        public int mapTolerance;

        // Added v1.3.2 Beta 7b
        public AnimationCurve mapToleranceBlendCurve;
        public bool mapIsPath = false;

        // Added v1.3.1 Beta 8e
        public bool useNoise;
        public float noiseTileSize;
        public float noiseOffset;
        public float meshPlacementCutoff;

        // Added v1.3.5 Beta 1d
        // Used to deploy multiple meshes/prefabs around the same location
        public bool isClustered;
        public float clusterDistance;
        [Range(0.01f, 1f)] public float clusterDensity;
        public float clusterResolution;

        // Added v1.2 Beta 12
        public bool removeGrass;
        public float minGrassProximity;

        // Added v1.3.1 Beta 9g
        public float minTreeProximity;

        // Added v1.2 Beta 12
        public bool mapInverse;

        // Added v1.3.0
        public bool isDisabled;

        public MeshPlacingMode meshPlacingMode;

        // Added v1.3.5 Beta 1b
        public bool isTerrainFlattened;
        public float flattenDistance;
        [Range(0f, 1f)] public float flattenBlendRate;
        public float flattenHeightOffset;

        // Added with v1.2.1
        public List<LBFilter> filterList;

        // Added v1.3.0 Beta 3a
        public bool showMesh;

        // Added v1.4.2 Beta 6b (NOT AVAILABLE AT RUNTIME)
        public bool isKeepPrefabConnection;

        // Added 1.4.2 Beta 6f
        // Last known prefab name. Used to help detect missing mesh prefabs.
        public string prefabName;

        public int MaxMeshLimit { get { if (usePrefab) { return 500; } else { return 10000; } } }

        #endregion

        #region Class Constructors

        // Class Constructor
        public LBLandscapeMesh()
        {
            this.mesh = null;
            this.materials = new List<Material>();
            this.offset = Vector3.zero;
            // When usePrefab is false, combine mesh is always true
            this.isCombineMesh = true;
            this.isCreateCollider = false;
            this.isRemoveEmptyGameObjects = true;
            this.maxMeshes = 100;
            this.minProximity = 10f;
            this.randomiseYRotation = true;
            this.fixedYRotation = 0f;
            this.XRotation = 0f;
            this.ZRotation = 0f;
            this.isTerrainAligned = false;
            this.minScale = 1f;
            this.maxScale = 3f;
            this.minHeight = 0.55f;
            this.maxHeight = 0.75f;
            this.minInclination = 0f;
            this.maxInclination = 30f;
            this.meshPlacingMode = MeshPlacingMode.Height;

            // Added v1.1 Beta 8
            this.map = null;
            this.mapColour = UnityEngine.Color.blue;
            this.mapTolerance = 1;

            // Added v1.3.2 Beta 7b
            this.mapToleranceBlendCurve = LBMap.GetDefaultToleranceBlendCurve;
            this.mapIsPath = false;

            // Added v1.3.1 beta 8e
            this.useNoise = false;
            this.noiseTileSize = 500f;
            // Noise offset cannot be modified from the editor as it is randomly set when the terrain
            // is populated with meshes or prefabs
            this.meshPlacementCutoff = 0.65f;

            // Added v1.3.5 Beta 1d
            this.isClustered = false;
            this.clusterDistance = 5f;
            this.clusterDensity = 0.5f;
            this.clusterResolution = 1f;

            // Added v1.2 Beta 12
            this.removeGrass = true;
            this.minGrassProximity = 2f;

            // Added v1.3.0
            this.showMesh = true;
            this.isDisabled = false;

            // Added v1.3.1 Beta 8c
            this.usePrefab = false;
            this.prefab = null;

            // Added v1.3.1. Beta 9g
            this.minTreeProximity = this.minProximity;

            // Added v1.3.5 Beta 1b
            this.isTerrainFlattened = false;
            this.flattenDistance = 2f;
            this.flattenBlendRate = 0.5f;
            this.flattenHeightOffset = 0f;

            // Added v1.4.2 Beta 6b
            this.isKeepPrefabConnection = false;

            // Added v1.4.2 Beta 6f
            this.prefabName = string.Empty;
        }

        /// <summary>
        /// Constructor for cloning a layer
        /// </summary>
        /// <param name="lbLandscapeMesh"></param>
        public LBLandscapeMesh(LBLandscapeMesh lbLandscapeMesh)
        {
            this.mesh = lbLandscapeMesh.mesh;
            this.materials = new List<Material>(lbLandscapeMesh.materials);
            this.offset = lbLandscapeMesh.offset;
            this.maxMeshes = lbLandscapeMesh.maxMeshes;
            this.minProximity = lbLandscapeMesh.minProximity;
            this.randomiseYRotation = lbLandscapeMesh.randomiseYRotation;
            this.fixedYRotation = lbLandscapeMesh.fixedYRotation;
            this.XRotation = lbLandscapeMesh.XRotation;
            this.ZRotation = lbLandscapeMesh.ZRotation;
            this.isTerrainAligned = lbLandscapeMesh.isTerrainAligned;
            this.minScale = lbLandscapeMesh.minScale;
            this.maxScale = lbLandscapeMesh.maxScale;
            this.minHeight = lbLandscapeMesh.minHeight;
            this.maxHeight = lbLandscapeMesh.maxHeight;
            this.minInclination = lbLandscapeMesh.minInclination;
            this.maxInclination = lbLandscapeMesh.maxInclination;
            this.meshPlacingMode = lbLandscapeMesh.meshPlacingMode;

            this.map = lbLandscapeMesh.map;
            this.mapColour = lbLandscapeMesh.mapColour;
            this.mapTolerance = lbLandscapeMesh.mapTolerance;
            this.mapToleranceBlendCurve = new AnimationCurve(lbLandscapeMesh.mapToleranceBlendCurve.keys);
            this.mapIsPath = lbLandscapeMesh.mapIsPath;

            this.useNoise = lbLandscapeMesh.useNoise;
            this.noiseTileSize = lbLandscapeMesh.noiseTileSize;
            this.noiseOffset = lbLandscapeMesh.noiseOffset;
            this.meshPlacementCutoff = lbLandscapeMesh.meshPlacementCutoff;

            this.isClustered = lbLandscapeMesh.isClustered;
            this.clusterDistance = lbLandscapeMesh.clusterDistance;
            this.clusterDensity = lbLandscapeMesh.clusterDensity;
            this.clusterResolution = lbLandscapeMesh.clusterResolution;

            this.removeGrass = lbLandscapeMesh.removeGrass;
            this.minGrassProximity = lbLandscapeMesh.minGrassProximity;

            this.showMesh = lbLandscapeMesh.showMesh;
            this.isDisabled = lbLandscapeMesh.isDisabled;

            this.usePrefab = lbLandscapeMesh.usePrefab;
            this.prefab = lbLandscapeMesh.prefab;
            this.isCombineMesh = lbLandscapeMesh.isCombineMesh;
            this.isCreateCollider = lbLandscapeMesh.isCreateCollider;
            this.isRemoveEmptyGameObjects = lbLandscapeMesh.isRemoveEmptyGameObjects;
            this.minTreeProximity = lbLandscapeMesh.minTreeProximity;

            this.isTerrainFlattened = lbLandscapeMesh.isTerrainFlattened;
            this.flattenDistance = lbLandscapeMesh.flattenDistance;
            this.flattenBlendRate = lbLandscapeMesh.flattenBlendRate;
            this.flattenHeightOffset = lbLandscapeMesh.flattenHeightOffset;

            if (lbLandscapeMesh.filterList != null) { this.filterList = LBFilter.CopyList(lbLandscapeMesh.filterList); }
            else { this.filterList = new List<LBFilter>(); }

            this.isKeepPrefabConnection = lbLandscapeMesh.isKeepPrefabConnection;

            if (lbLandscapeMesh.prefabName == null) { this.prefabName = string.Empty; }
            else { this.prefabName = lbLandscapeMesh.prefabName; }
        }

        #endregion
    }
}