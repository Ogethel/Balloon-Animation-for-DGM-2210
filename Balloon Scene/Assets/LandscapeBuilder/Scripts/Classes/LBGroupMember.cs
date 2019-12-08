// Landscape Builder. Copyright (c) 2016-2019 SCSM Pty Ltd. All rights reserved.
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace LandscapeBuilder
{
    [System.Serializable]
    public class LBGroupMember
    {
        #region Enumerations

        /// <summary>
        /// Rotation Type
        /// Face 2 Zone Centre works with the first zone in the Member Filter Zone list (zoneGUIDList)
        /// </summary>
        public enum LBRotationType
        {
            Face2GroupCentre = 5,
            Face2ZoneCentre = 6,
            GroupSpace = 8,
            WorldSpace = 20
        }

        public enum LBMemberEditorTab
        {
            General = 0,
            XYZ = 1,
            Proximity = 2,
            Zone = 3,
            Path = 4
        }

        public enum LBMemberEditorPathTab
        {
            General = 0,
            Objects = 1,
            Points = 2,
            Surface = 3
        }

        public enum LBMemberType
        {
            Prefab = 0,
            ObjPath = 1
        }

        #endregion

        #region Public variables and properties
        // IMPORTANT When changing this section update:
        // SetClassDefaults() and LBGroupMember(LBGroupMember lbGroupMember) clone constructor

        public bool isDisabled;
        public bool showInEditor;
        public int showtabInEditor;         // The zero-indexed tab to be shown for the groupmember in the Editor
        public bool showObjPathDesigner;
        public string GUID;

        public bool isGroupOverride;
        // These values override the Group-level settings if isGroupOverride is enabled
        public float minScale;
        public float maxScale;
        [Range(0f, 1f)] public float minHeight;
        [Range(0f, 1f)] public float maxHeight;
        public float minInclination;
        public float maxInclination;

        // Prefab variables
        public GameObject prefab;
        // Last known prefab name. Used to help detect missing mesh prefabs.
        public string prefabName;
        public bool showPrefabPreview;
        public bool isKeepPrefabConnection;
        public bool isCombineMesh;
        public bool isRemoveAnimator;
        public bool isRemoveEmptyGameObjects;
        public bool isCreateCollider;
        public int maxPrefabSqrKm;
        /// <summary>
        /// The max. number that can be in each
        /// clearing. Does not apply to Uniform groups
        /// </summary>
        public int maxPrefabPerGroup;
        public bool isPlacedInCentre;

        public bool showXYZSettings;

        // Can be used to correct non-zero'd model prefabs.  
        public float modelOffsetX;
        public float modelOffsetY;
        public float modelOffsetZ;

        // Offset the placement position of the prefab instance (in metres). No need for a maxOffsetX, Z but use "min" to keep
        // consistant with OffsetY and in case we need to add maxOffsetX,Z later.
        public float minOffsetX;
        public float minOffsetZ;

        // Useful for sinking rocks into the ground
        public float minOffsetY;
        public float maxOffsetY;
        public bool randomiseOffsetY;

        // Rotation
        public LBRotationType rotationType;
        public bool randomiseRotationY;
        [Range(-359.9f, 359.9f)] public float startRotationY;
        [Range(-359.9f, 359.9f)] public float endRotationY;
        // Used to correct a model or prefab import problem
        // Can also be used with randomiseRotationXZ and endRotationX/Z for giving random "wobble" on x/z axis.
        [Range(-359.9f, 359.9f)] public float rotationX;
        [Range(-359.9f, 359.9f)] public float rotationZ;
        public bool randomiseRotationXZ;
        [Range(-359.9f, 359.9f)] public float endRotationX;
        [Range(-359.9f, 359.9f)] public float endRotationZ;
        /// <summary>
        /// Use to keep a prefab upright along an ObjPath when isTerrainAligned is enabled
        /// </summary>
        public bool isLockTilt;

        // Noise
        public bool useNoise;
        public float noiseTileSize;
        public float noiseOffset;
        public float noisePlacementCutoff;

        // Proximity
        /// <summary>
        /// The distance from the centre of the prefab to the edge
        /// of any other prefab's proximityExtent.
        /// </summary>
        public float proximityExtent;
        public bool isIgnoreProximityOfOthers;
        public bool isProximityIgnoredByOthers;

        public float minGrassProximity;
        public float removeGrassBlendDist;
        /// <summary>
        /// Remove the Terrain tree if it is within the minTreeProximity of a placed group member,
        /// else don't place the group member if it is too close to a terrain tree.
        /// </summary>
        public bool isRemoveTree;
        public float minTreeProximity;

        // Terrain Alignment
        public bool isTerrainAligned;

        // Flatten terrain
        public bool isTerrainFlattened;
        public float flattenDistance;
        [Range(0f, 1f)] public float flattenBlendRate;
        public float flattenHeightOffset;

        // Zone restrictions (a list of GUIDs that reference ones
        // pre-defined at the Group level in lbGroup.zoneList
        public List<string> zoneGUIDList;

        // Edge fill zones, rather than fill the entire zone area.
        // Restricts the member prefab to near the edge only.
        public bool isZoneEdgeFillTop;
        public bool isZoneEdgeFillBottom;
        public bool isZoneEdgeFillLeft;
        public bool isZoneEdgeFillRight;
        public float zoneEdgeFillDistance;

        // Added 2.0.4
        public LBMemberType lbMemberType;
        public LBObjPath lbObjPath;
        public bool isPathOnly;             // Used when the member should only be used to populate an ObjPath
        public LBObjPath.LBObjectOrientation lbObjectOrientation;
        // Added 2.1.0 Beta 8a
        public bool usePathHeight;
        // Added 2.1.1 Beta 1j
        public bool usePathSlope;
        /// <summary>
        /// Added 2.2.1. When usePathHeight is enabled
        /// in a clearing or subgroup, the path will be
        /// adjusted according to the difference in height
        /// between the first point of path and current point.
        /// </summary>
        public bool useTerrainTrend;

        #endregion

        #region Private variables and properties

        /// <summary>
        /// Get a "reasonable" limit, based on the number object to be created in the scene
        /// NOTE: Currently needs some work....
        /// </summary>
        public int MaxPrefabSqrKmLimit { get { if (isCombineMesh && isRemoveEmptyGameObjects) { return 100000; } else { return 50000; } } }

        #endregion

        #region Constructors

        // Basic class constructor
        public LBGroupMember()
        {
            SetClassDefaults();
        }

        // Clone constructor
        public LBGroupMember(LBGroupMember lbGroupMember)
        {
            if (lbGroupMember == null) { SetClassDefaults(); }
            else
            {
                isDisabled = lbGroupMember.isDisabled;
                showInEditor = lbGroupMember.showInEditor;
                showtabInEditor = lbGroupMember.showtabInEditor;
                showObjPathDesigner = lbGroupMember.showObjPathDesigner;

                GUID = lbGroupMember.GUID;

                // Placement rule variables
                isGroupOverride = lbGroupMember.isGroupOverride;
                minScale = lbGroupMember.minScale;
                maxScale = lbGroupMember.maxScale;
                minHeight = lbGroupMember.minHeight;
                maxHeight = lbGroupMember.maxHeight;
                minInclination = lbGroupMember.minInclination;
                maxInclination = lbGroupMember.maxInclination;

                // Prefab varibles
                prefab = lbGroupMember.prefab;
                prefabName = lbGroupMember.prefabName;
                showPrefabPreview = lbGroupMember.showPrefabPreview;
                isKeepPrefabConnection = lbGroupMember.isKeepPrefabConnection;
                isCombineMesh = lbGroupMember.isCombineMesh;
                isRemoveEmptyGameObjects = lbGroupMember.isRemoveEmptyGameObjects;
                isRemoveAnimator = lbGroupMember.isRemoveAnimator;
                isCreateCollider = lbGroupMember.isCreateCollider;
                maxPrefabSqrKm = lbGroupMember.maxPrefabSqrKm;
                maxPrefabPerGroup = lbGroupMember.maxPrefabPerGroup;
                isPlacedInCentre = lbGroupMember.isPlacedInCentre;

                showXYZSettings = lbGroupMember.showXYZSettings;

                // Prefab offset variables
                modelOffsetX = lbGroupMember.modelOffsetX;
                modelOffsetY = lbGroupMember.modelOffsetY;
                modelOffsetZ = lbGroupMember.modelOffsetZ;
                minOffsetX = lbGroupMember.minOffsetX;
                minOffsetZ = lbGroupMember.minOffsetZ;
                minOffsetY = lbGroupMember.minOffsetY;
                maxOffsetY = lbGroupMember.maxOffsetY;
                randomiseOffsetY = lbGroupMember.randomiseOffsetY;

                // Prefab rotation variables
                rotationType = lbGroupMember.rotationType;
                // This was prior to LB 2.1.0 Beta 4w a workaround for a, now unknown, issue with templates.
                // Not sure what the original problem was and why we needed to default randomiseRotationY to true.
                // Now that we do a deep copy (ConvertAll) in LBGroup(LBGroup lbGroup) it no longer works.
                //randomiseRotationY = true;
                randomiseRotationY = lbGroupMember.randomiseRotationY;
                startRotationY = lbGroupMember.startRotationY;
                endRotationY = lbGroupMember.endRotationY;
                randomiseRotationXZ = lbGroupMember.randomiseRotationXZ;
                rotationX = lbGroupMember.rotationX;
                rotationZ = lbGroupMember.rotationZ;
                endRotationX = lbGroupMember.endRotationX;
                endRotationZ = lbGroupMember.endRotationZ;
                isLockTilt = lbGroupMember.isLockTilt;

                // Noise variables
                useNoise = lbGroupMember.useNoise;
                noiseOffset = lbGroupMember.noiseOffset;
                noiseTileSize = lbGroupMember.noiseTileSize;
                noisePlacementCutoff = lbGroupMember.noisePlacementCutoff;

                // Proximity variables
                proximityExtent = lbGroupMember.proximityExtent;
                isIgnoreProximityOfOthers = lbGroupMember.isIgnoreProximityOfOthers;
                isProximityIgnoredByOthers = lbGroupMember.isProximityIgnoredByOthers;
                removeGrassBlendDist = lbGroupMember.removeGrassBlendDist;
                minGrassProximity = lbGroupMember.minGrassProximity;
                isRemoveTree = lbGroupMember.isRemoveTree;
                minTreeProximity = lbGroupMember.minTreeProximity;

                // Terrain Alignment
                isTerrainAligned = lbGroupMember.isTerrainAligned;

                // Prefab flatten terrain variables
                isTerrainFlattened = lbGroupMember.isTerrainFlattened;
                flattenDistance = lbGroupMember.flattenDistance;
                flattenHeightOffset = lbGroupMember.flattenHeightOffset;
                flattenBlendRate = lbGroupMember.flattenBlendRate;

                // Zones
                if (lbGroupMember.zoneGUIDList == null) { zoneGUIDList = new List<string>(); }
                else { zoneGUIDList = new List<string>(lbGroupMember.zoneGUIDList); }

                // Zone edge fill
                isZoneEdgeFillTop = lbGroupMember.isZoneEdgeFillTop;
                isZoneEdgeFillBottom = lbGroupMember.isZoneEdgeFillBottom;
                isZoneEdgeFillLeft = lbGroupMember.isZoneEdgeFillLeft;
                isZoneEdgeFillRight = lbGroupMember.isZoneEdgeFillRight;
                zoneEdgeFillDistance = lbGroupMember.zoneEdgeFillDistance;

                // Object Paths
                lbMemberType = lbGroupMember.lbMemberType;

                if (lbGroupMember.lbObjPath == null) { lbObjPath = null; }
                else { lbObjPath = new LBObjPath(lbGroupMember.lbObjPath); }

                isPathOnly = lbGroupMember.isPathOnly;
                lbObjectOrientation = lbGroupMember.lbObjectOrientation;
                usePathHeight = lbGroupMember.usePathHeight;
                usePathSlope = lbGroupMember.usePathSlope;
                useTerrainTrend = lbGroupMember.useTerrainTrend;
            }
        }

        #endregion

        #region Private Non-Static Methods

        /// <summary>
        ///  Set the default values for a new LBGroupMember class instance
        /// </summary>
        private void SetClassDefaults()
        {
            isDisabled = false;
            showInEditor = true;
            showtabInEditor = 0;
            showObjPathDesigner = false;

            // Assign a unique identifier
            GUID = System.Guid.NewGuid().ToString();

            // Placement rule variables
            isGroupOverride = false;
            minScale = 1f;
            maxScale = 1f;
            minHeight = 0f;
            maxHeight = 1f;
            minInclination = 0f;
            maxInclination = 90f;

            // Prefab varibles
            prefab = null;
            prefabName = string.Empty;
            showPrefabPreview = false;
            isKeepPrefabConnection = false;
            isCombineMesh = false;
            isRemoveEmptyGameObjects = true;
            isRemoveAnimator = true;
            isCreateCollider = false;
            maxPrefabSqrKm = 10;
            maxPrefabPerGroup = 10000;
            isPlacedInCentre = false;

            showXYZSettings = false;

            // Prefab offset variables
            modelOffsetX = 0f;
            modelOffsetZ = 0f;
            minOffsetX = 0f;
            minOffsetZ = 0f;
            minOffsetY = 0f;
            maxOffsetY = 0f;
            randomiseOffsetY = false;

            // Rotation variables
            rotationType = LBRotationType.WorldSpace;
            randomiseRotationY = true;
            startRotationY = 0f;
            endRotationY = 359.9f;
            randomiseRotationXZ = false;
            rotationX = 0f;
            rotationZ = 0f;
            endRotationX = 0f;
            endRotationZ = 0f;
            isLockTilt = false;

            // Noise variables
            useNoise = false;
            noiseTileSize = 500f;
            // Noise offset cannot be modified from the editor as it is randomly set when the terrain
            // is populated with groups
            noisePlacementCutoff = 0.65f;

            // Proximity variables
            proximityExtent = 10f;
            isIgnoreProximityOfOthers = false;
            isProximityIgnoredByOthers = false;
            removeGrassBlendDist = 0.5f;
            minGrassProximity = 0f;
            isRemoveTree = true;
            minTreeProximity = 10f;

            // Terrain Alignment
            isTerrainAligned = false;

            // Prefab flatten terrain variables
            isTerrainFlattened = false;
            flattenDistance = 2f;
            flattenBlendRate = 0.5f;
            flattenHeightOffset = 0f;

            // zones
            zoneGUIDList = new List<string>();

            // Zone edge fill
            isZoneEdgeFillTop = false;
            isZoneEdgeFillBottom = false;
            isZoneEdgeFillLeft = false;
            isZoneEdgeFillRight = false;
            zoneEdgeFillDistance = 1f;

            // Object Paths
            lbMemberType = LBMemberType.Prefab;
            lbObjPath = null;
            isPathOnly = false;
            lbObjectOrientation = LBObjPath.LBObjectOrientation.PathSpace;
            usePathHeight = false;
            usePathSlope = false;
            useTerrainTrend = false;
        }

        #endregion

        #region Public Non-Static Methods

        /// <summary>
        /// Expand or collapse the details of each Object Path point of a this groupmember
        /// </summary>
        /// <param name="isExpanded"></param>
        public void ObjPathPointsExpand(bool isExpanded)
        {
            if (lbObjPath != null)
            {
                int numPathPoints = lbObjPath.pathPointList == null ? 0 : lbObjPath.pathPointList.Count;

                for (int objPathPointIdx = 0; objPathPointIdx < numPathPoints; objPathPointIdx++)
                {
                    if (lbObjPath.pathPointList[objPathPointIdx] != null) { lbObjPath.pathPointList[objPathPointIdx].showInEditor = isExpanded; }
                }
            }
        }

        /// <summary>
        /// Will update the Proximity Extent based on the bounds supplied
        /// </summary>
        public void UpdateProximity(Bounds bounds)
        {
            proximityExtent = (bounds.extents.x > bounds.extents.z ? bounds.extents.x : bounds.extents.z);
        }

        /// <summary>
        /// Does member match the search criteria.
        /// If the search string is empty, always return true.
        /// This is used in the LB Editor window.
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public bool IsInSearchFilter(string searchFilter)
        {
            bool isMatch = false;
            string memberNameText = string.Empty;

            // If the search string is empty, always return true.
            if (string.IsNullOrEmpty(searchFilter)) { isMatch = true; }
            else
            {
                if (lbMemberType == LBGroupMember.LBMemberType.Prefab)
                {
                    
                    if (prefab != null) { memberNameText = prefab.name; }
                    else if (!string.IsNullOrEmpty(prefabName)) { memberNameText = prefabName + " N/A"; }
                    else { memberNameText = "No group member prefab"; }

                    isMatch = (!string.IsNullOrEmpty(memberNameText) && memberNameText.ToLower().Contains(searchFilter.ToLower()));
                }
                else if (lbObjPath != null)
                {
                    // If this ObjPath has no pathname, it can't be a match
                    isMatch = (!string.IsNullOrEmpty(lbObjPath.pathName) && lbObjPath.pathName.ToLower().Contains(searchFilter.ToLower()));
                }
            }

            return isMatch;
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Find a random offset (position) within an area with the given radius for a GroupMember.
        /// USAGE: Vector2 offset = LBGroupMember.GetRandomOffset(lbGroupMember, basePlaneWidth / 2f, 100);
        /// </summary>
        /// <param name="lbGroupMember"></param>
        /// <param name="radius"></param>
        /// <param name="maxAttempts"></param>
        /// <returns></returns>
        public static Vector2 GetRandomOffset(LBGroupMember lbGroupMember, float radius, int maxAttempts)
        {
            Vector2 offset = Vector2.zero;
            Vector2 noiseCoords = Vector2.zero;

            // Declare and initialise variables
            // To start with we have made zero attempts and not found a legal placement
            // (by this we mean a placement within the circle, and following some extra rules like noise)
            bool foundLegalPlacement = false;
            int attempts = 0;

            // Repeat until we find a legal placement or we try maxAttempts times
            while (!foundLegalPlacement && attempts < maxAttempts)
            {
                // Generate a random position - the chance of this placement not being legal
                // is (4 - PI) / 4 = 21.5% (so chance of it being legal is 78.5%)
                // Thus it is 99% likely that within 3 attempts we will have found a correct placement
                offset.x = UnityEngine.Random.Range(-radius, radius);
                offset.y = UnityEngine.Random.Range(-radius, radius);
                // Judge legality: First, based on whether placement is within radius...
                foundLegalPlacement = offset.magnitude < radius;
                if (foundLegalPlacement && lbGroupMember != null && lbGroupMember.useNoise)
                {
                    // ... then by whether it follows noise rules
                    noiseCoords.x = offset.x + lbGroupMember.noiseOffset;
                    noiseCoords.y = offset.y + lbGroupMember.noiseOffset;
                    float noiseValue = Mathf.Abs(LBNoise.PerlinFractalNoise(noiseCoords.x / lbGroupMember.noiseTileSize, noiseCoords.y / lbGroupMember.noiseTileSize, 5) - 0.5f) * 4f;
                    // If the noise value is less than (1 - prefab cutoff value) placement is not legal
                    foundLegalPlacement = noiseValue >= 1f - lbGroupMember.noisePlacementCutoff;
                }

                attempts++;
            }

            // If we have tried maxAttempts times and still not found a correct placement, just set the position to the centre
            if (!foundLegalPlacement) { offset = Vector2.zero; }

            return offset;
        }

        #endregion

    }
}