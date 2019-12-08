// Landscape Builder. Copyright (c) 2016-2019 SCSM Pty Ltd. All rights reserved.
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace LandscapeBuilder
{
    /// <summary>
    /// A series or collection that describes how objects or subgroups are
    /// arranged along an LBObjPath. There can be multiple series
    /// each with their own layout, spacing, and prefab or subgroup attributes.
    /// </summary>
    [System.Serializable]
    public class LBObjPathSeries
    {
        #region Public Variables
        // IMPORTANT When changing this section update:
        // SetClassDefaults() and LBObjPathSeries(LBObjPathSeries lbObjPathSeries) clone constructor

        public string seriesGUID;
        public string seriesName;

        // Added v2.1.5
        public bool useSubGroups;

        // Prefab/SubGroup variables - start/main/end (start and end prefabs are optional)
        public List<LBObjPrefab> mainObjPrefabList;
        public List<LBObjSubGroup> mainObjSubGroupList;

        // Optional start prefab/subgroup to be placed at the start of the path
        public LBObjPrefab startObjPrefab;
        public LBObjSubGroup startObjSubGroup;
        public float startMemberOffset;             // offset to positions after the start member (startObjPrefab)

        // Optional end prefab/subgroup to be placed at the start of the path
        public LBObjPrefab endObjPrefab;
        public LBObjSubGroup endObjSubGroup;
        public float endMemberOffset;               // offset to end member (endObjPrefab) if isLastObjSnappedToEnd is not enabled

        public LBObjPath.LayoutMethod layoutMethod;
        public LBObjPath.SelectionMethod selectionMethod;

        public float spacingDistance;               // Used with LayoutMethod.Spacing for optimum distance apart to place objects on path
        public int maxMainPrefabs = 5;              // The max number of main prefabs (or SubGroups) to place along the path
        public bool isLastObjSnappedToEnd;          // If the prefabs don't evenly fit along the path, ensure the last object is placed on the end of the path
        public float sparcePlacementCutoff;         // The likelihood that the main objects will be placed on the path

        public LBObjPath.PathSpline pathSpline;     // The spline along which the prefabs will be placed
        public float prefabOffsetZ;                 // The offset in path-space on the z-axis for the prefab/subgroup from the spline.
        public float prefabStartOffset;             // The distance from the start of the path to place the first prefab or subgroup
        public float prefabEndOffset;               // The distance from the end of the path to finish prefab/subgroup placement (actual last position will depend on spacing distance and if Snap To End is enabled)
        public bool use3DDistance;                  // Calculate the distance along the path in 3D rather than 2D
        public bool useNonOffsetDistance;           // The distance along the path is calculated on the Placement Spline rather than the spline created with the z-offset. This could be useful when aligning multiple Series of objects with a different Offset Z distance.
        public bool isRandomisePerGroupRegion;      // Use a different random seed per Group region (instance) placement

        public bool showInEditor;

        #endregion

        #region Private variables

        #endregion

        #region Constructors

        public LBObjPathSeries()
        {
            SetClassDefaults();
        }

        // Clone constructor
        public LBObjPathSeries(LBObjPathSeries lbObjPathSeries)
        {
            if (lbObjPathSeries == null) { SetClassDefaults(); }
            else
            {
                seriesGUID = lbObjPathSeries.seriesGUID;
                seriesName = lbObjPathSeries.seriesName;

                useSubGroups = lbObjPathSeries.useSubGroups;

                // Prefab varibles
                if (lbObjPathSeries.mainObjPrefabList != null) { mainObjPrefabList = lbObjPathSeries.mainObjPrefabList.ConvertAll(mp => new LBObjPrefab(mp)); } else { mainObjPrefabList = new List<LBObjPrefab>(); }
                if (lbObjPathSeries.startObjPrefab != null) { startObjPrefab = new LBObjPrefab(lbObjPathSeries.startObjPrefab); } else { startObjPrefab = null; }
                if (lbObjPathSeries.endObjPrefab != null) { endObjPrefab = new LBObjPrefab(lbObjPathSeries.endObjPrefab); } else { endObjPrefab = null; }

                // SubGroup variables
                if (lbObjPathSeries.mainObjSubGroupList != null) { mainObjSubGroupList = lbObjPathSeries.mainObjSubGroupList.ConvertAll(msg => new LBObjSubGroup(msg)); } else { mainObjSubGroupList = new List<LBObjSubGroup>(); }
                if (lbObjPathSeries.startObjSubGroup != null) { startObjSubGroup = new LBObjSubGroup(lbObjPathSeries.startObjSubGroup); } else { startObjSubGroup = null; }
                if (lbObjPathSeries.endObjSubGroup != null) { endObjSubGroup = new LBObjSubGroup(lbObjPathSeries.endObjSubGroup); } else { endObjSubGroup = null; }

                startMemberOffset = lbObjPathSeries.startMemberOffset;
                endMemberOffset = lbObjPathSeries.endMemberOffset;

                layoutMethod = lbObjPathSeries.layoutMethod;
                selectionMethod = lbObjPathSeries.selectionMethod;
                spacingDistance = lbObjPathSeries.spacingDistance;
                maxMainPrefabs = lbObjPathSeries.maxMainPrefabs;
                isLastObjSnappedToEnd = lbObjPathSeries.isLastObjSnappedToEnd;
                sparcePlacementCutoff = lbObjPathSeries.sparcePlacementCutoff;
                pathSpline = lbObjPathSeries.pathSpline;
                prefabOffsetZ = lbObjPathSeries.prefabOffsetZ;
                prefabStartOffset = lbObjPathSeries.prefabStartOffset;
                prefabEndOffset = lbObjPathSeries.prefabEndOffset;
                use3DDistance = lbObjPathSeries.use3DDistance;
                useNonOffsetDistance = lbObjPathSeries.useNonOffsetDistance;
                isRandomisePerGroupRegion = lbObjPathSeries.isRandomisePerGroupRegion;
                showInEditor = lbObjPathSeries.showInEditor;
            }
        }

        #endregion

        #region Public Member Methods

        #endregion

        #region Private Member Methods

        private void SetClassDefaults()
        {
            // Assign a unique identifier
            seriesGUID = System.Guid.NewGuid().ToString();
            seriesName = "(new) series";

            // By default, place prefabs along the path
            useSubGroups = false;

            // Prefab varibles
            mainObjPrefabList = new List<LBObjPrefab>();
            startObjPrefab = new LBObjPrefab();
            endObjPrefab = new LBObjPrefab();

            // SubGroup variables
            mainObjSubGroupList = new List<LBObjSubGroup>();
            startObjSubGroup = new LBObjSubGroup();
            endObjSubGroup = new LBObjSubGroup();

            startMemberOffset = 0f;
            endMemberOffset = 0f;

            layoutMethod = LBObjPath.LayoutMethod.Spacing;
            selectionMethod = LBObjPath.SelectionMethod.Alternating;
            isLastObjSnappedToEnd = true;
            spacingDistance = 10f;
            maxMainPrefabs = 5;
            sparcePlacementCutoff = 1f;
            pathSpline = LBObjPath.PathSpline.Centre;
            prefabOffsetZ = 0f;
            use3DDistance = false;
            useNonOffsetDistance = false;
            prefabStartOffset = 0f;
            prefabEndOffset = 0f;

            // Off by default for backward compatibility
            isRandomisePerGroupRegion = false;

            showInEditor = true;
        }

        #endregion

        #region Public Static Methods

        #endregion
    }
}
