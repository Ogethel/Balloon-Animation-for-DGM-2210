// Landscape Builder. Copyright (c) 2016-2019 SCSM Pty Ltd. All rights reserved.
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace LandscapeBuilder
{
    /// <summary>
    /// A LBGroup contains a list of LBGroupMember classes. The purpsoe of the group is to
    /// store a collection of prefabs and other member components that will be
    /// distributed across the landscape based on rules contained with the class.
    /// There can be multiple groups within a LBLandscape. These will be stored
    /// in lbLandscape.lbGroupList
    /// </summary>
    [System.Serializable]
    public class LBGroup
    {
        #region Enumerations

        public enum LBGroupType
        {
            ProceduralClearing = 5,
            ManualClearing = 7,
            SubGroup = 9,
            Uniform = 10
        }

        #endregion

        #region Public variables and properties
        // IMPORTANT When changing this section update:
        // SetClassDefaults() and LBGroup(LBGroup lbGroup) clone constructor

        public bool isDisabled;
        public bool showInEditor;
        public bool showGroupDefaultsInEditor;
        public bool showGroupOptionsInEditor;
        public bool showGroupMembersInEditor;
        public bool showGroupDesigner;
        public int showtabInEditor;     // The zero-indexed tab to be shown for the group in the Editor
        public bool isMemberListExpanded;
        public bool isZoneListExpanded;

        /// <summary>
        /// Currently used for Manual Clearings. To allow user to add/edit/delete
        /// locations of manually placed clearings.
        /// </summary>
        public bool showGroupsInScene;

        /// <summary>
        /// Stores a list of position in the landscape where (manual) clearings or SubGroups will appear.
        /// When used with the GroupDesigner, the positions are relative to the centre of the Group.
        /// For subgroups, this is only a temporary list used during spawing.
        /// </summary>
        public List<Vector3> positionList;

        /// <summary>
        /// Stores a list of manual rotations for manual clearing groups.
        /// When isFixedRotation is enabled, this list must be in sync with
        /// positionList. Can call SyncRotationList() to verify.
        /// </summary>
        public List<float> rotationYList;

        /// <summary>
        /// Used with Manual Clearings. User configures each location in the
        /// scene (positionList) to have a fixed rotation (rotationList).
        /// </summary>
        public bool isFixedRotation;

        /// <summary>
        /// A list of subgroup rotations which should be in sync with positionList.
        /// Currently only used during group processing to pass rotations of subgroups
        /// along an Object Path to the placement of those SubGroups.
        /// NOTE: This is the base rotation before any subgroup-specific rotation is applied.
        /// See LBLandscapeTerrain.ProcessGroup(..)
        /// </summary>
        [System.NonSerialized] public List<Vector3> subGroupRotationList;

        public string groupName;
        public LBGroupType lbGroupType;
        public string GUID;

        public int maxGroupSqrKm;

        // Group-Level Default settings - GroupMembers inherit these settings
        public float minScale;
        public float maxScale;
        [Range(0f, 1f)] public float minHeight;
        [Range(0f, 1f)] public float maxHeight;
        public float minInclination;
        public float maxInclination;

        // Proximity
        /// <summary>
        /// The distance from the centre of the Group (Clearing) to the edge
        /// of any other Group's proximityExtent. If this is less than the
        /// radius, the groups may overlap.
        /// </summary>
        public float proximityExtent;

        // Clearing Group variables
        [Range(0.01f, 1000f)] public float minClearingRadius;
        [Range(0.01f, 1000f)] public float maxClearingRadius;
        [System.NonSerialized] public float clearingRadius;  // Used to store the actual radius. Also becomes the flattenDistance.
                                                             
        [Range(-359.9f, 359.9f)] public float startClearingRotationY;   // Starting random range rotation on y-axis of clearing
        [Range(-359.9f, 359.9f)] public float endClearingRotationY;     // Ending random range rotation on y-axis of clearing

        public bool isRemoveExistingGrass;
        /// <summary>
        /// Normalised to the radius of the group. If value = 0, there is no blending
        /// and grass stops at the edge of the clearing. If > 0 it blends the blenddist
        /// x Radius outwards from the group clearing edge.
        /// </summary>
        public float removeExistingGrassBlendDist;
        public bool isRemoveExistingTrees;
        public List<LBGroupZone> zoneList;
        public List<LBGroupTexture> textureList;
        public List<LBGroupGrass> grassList;

        // Clearing Group - Flatten terrain
        public bool isTerrainFlattened;
        [Range(0f, 1f)] public float flattenBlendRate;
        public float flattenHeightOffset;

        public List<LBGroupMember> groupMemberList;
        public List<LBFilter> filterList;
        // Should the Clearing Radius be considered when placing clearings using Stencil filters?
        // Default behaviour is use centre point for clearing placement, then filter each object
        // within the clearing.
        public bool isClearingRadiusFiltered;

        /// <summary>
        /// Get a "reasonable" limit, based on the number object to be created in the scene
        /// NOTE: Currently needs some work....
        /// </summary>
        public int MaxGroupLimit { get { if (lbGroupType == LBGroupType.ProceduralClearing) { return 1000; } else { return 10000; } } }

        [System.NonSerialized] public string editorSearchMemberFilter;
        [System.NonSerialized] public string editorSearchZoneFilter;

        #endregion

        #region Private variables and properties

        #endregion

        #region Constructors

        // Basic class constructor
        public LBGroup()
        {
            SetClassDefaults();
        }

        // Clone constructor
        public LBGroup(LBGroup lbGroup)
        {
            if (lbGroup == null) { SetClassDefaults(); }
            else
            {
                isDisabled = lbGroup.isDisabled;
                showInEditor = lbGroup.showInEditor;
                showGroupDefaultsInEditor = lbGroup.showGroupDefaultsInEditor;
                showGroupOptionsInEditor = lbGroup.showGroupOptionsInEditor;
                showGroupMembersInEditor = lbGroup.showGroupMembersInEditor;
                showGroupDesigner = lbGroup.showGroupDesigner;
                showGroupsInScene = lbGroup.showGroupsInScene;
                showtabInEditor = lbGroup.showtabInEditor;
                isMemberListExpanded = lbGroup.isMemberListExpanded;
                isZoneListExpanded = lbGroup.isZoneListExpanded;

                if (string.IsNullOrEmpty(lbGroup.groupName)) { groupName = "new group"; }
                else { groupName = lbGroup.groupName; }
                lbGroupType = lbGroup.lbGroupType;

                GUID = lbGroup.GUID;

                maxGroupSqrKm = lbGroup.maxGroupSqrKm;

                // Clearing Group variables
                minClearingRadius = lbGroup.minClearingRadius;
                maxClearingRadius = lbGroup.maxClearingRadius;
                startClearingRotationY = lbGroup.startClearingRotationY;
                endClearingRotationY = lbGroup.endClearingRotationY;
                isRemoveExistingGrass = lbGroup.isRemoveExistingGrass;
                removeExistingGrassBlendDist = lbGroup.removeExistingGrassBlendDist;
                isRemoveExistingTrees = lbGroup.isRemoveExistingTrees;

                // Proximity variables
                proximityExtent = lbGroup.proximityExtent;

                // Default values per group
                minScale = lbGroup.minScale;
                maxScale = lbGroup.maxScale;
                minHeight = lbGroup.minHeight;
                maxHeight = lbGroup.maxHeight;
                minInclination = lbGroup.minInclination;
                maxInclination = lbGroup.maxInclination;

                // Group flatten terrain variables
                isTerrainFlattened = lbGroup.isTerrainFlattened;
                flattenHeightOffset = lbGroup.flattenHeightOffset;
                flattenBlendRate = lbGroup.flattenBlendRate;

                //#SMS LB 2.1.0 Beta 4w - perform a deep copy
                if (lbGroup.groupMemberList != null) { groupMemberList = lbGroup.groupMemberList.ConvertAll(gmbr => new LBGroupMember(gmbr)); } else { groupMemberList = new List<LBGroupMember>(); }

                // This code retains the link between the Template prefab and the list in the scene.
                //if (lbGroup.groupMemberList == null) { groupMemberList = new List<LBGroupMember>(); }
                //else { groupMemberList = new List<LBGroupMember>(lbGroup.groupMemberList); }

                if (lbGroup.filterList == null) { filterList = new List<LBFilter>(); }
                else { filterList = new List<LBFilter>(lbGroup.filterList); }

                isClearingRadiusFiltered = lbGroup.isClearingRadiusFiltered;

                // Stores a list of position in the landscape where (manual) clearings or subgroup will appear
                if (lbGroup.positionList == null) { positionList = new List<Vector3>(); }
                else { positionList = new List<Vector3>(lbGroup.positionList); }

                isFixedRotation = lbGroup.isFixedRotation;
                // Stores a list of y-axis rotations of (manual) clearing groups
                if (lbGroup.rotationYList == null) { rotationYList = new List<float>(); }
                else { rotationYList = new List<float>(lbGroup.rotationYList); }

                if (lbGroup.zoneList == null) { zoneList = new List<LBGroupZone>(); }
                else { zoneList = new List<LBGroupZone>(lbGroup.zoneList); }

                if (lbGroup.textureList == null) { textureList = new List<LBGroupTexture>(); }
                else { textureList = new List<LBGroupTexture>(lbGroup.textureList); }

                if (lbGroup.grassList == null) { grassList = new List<LBGroupGrass>(); }
                else { grassList = new List<LBGroupGrass>(lbGroup.grassList); }
            }
        }

        #endregion

        #region Private Non-Static Methods

        /// <summary>
        /// Set the default values for a new LBGroup class instance
        /// </summary>
        private void SetClassDefaults()
        {
            isDisabled = false;
            showInEditor = true;
            showGroupDefaultsInEditor = true;
            showGroupOptionsInEditor = true;
            // When adding a new group, the members list should be expanded so user can see the first member
            showGroupMembersInEditor = true;
            showGroupDesigner = false;
            showGroupsInScene = false;
            showtabInEditor = 0;
            isMemberListExpanded = true;
            isZoneListExpanded = true;

            groupName = "new group";
            lbGroupType = LBGroupType.Uniform;
            // Assign a unique identifier
            GUID = System.Guid.NewGuid().ToString();

            maxGroupSqrKm = 10;

            // Clearing Group variables
            minClearingRadius = 100f;
            maxClearingRadius = 100f;
            startClearingRotationY = 0f;
            endClearingRotationY = 359.9f;
            isRemoveExistingGrass = true;
            removeExistingGrassBlendDist = 0.5f;
            isRemoveExistingTrees = true;
            zoneList = new List<LBGroupZone>();
            textureList = new List<LBGroupTexture>();
            grassList = new List<LBGroupGrass>();

            // Proximity variables
            proximityExtent = 10f;

            // Group-Level Default settings
            minScale = 1f;
            maxScale = 1f;
            minHeight = 0f;
            maxHeight = 1f;
            minInclination = 0f;
            maxInclination = 90f;

            // Flatten terrain variables
            isTerrainFlattened = false;
            flattenBlendRate = 0.5f;
            flattenHeightOffset = 0f;

            groupMemberList = new List<LBGroupMember>();
            filterList = new List<LBFilter>();
            isClearingRadiusFiltered = false;
            positionList = new List<Vector3>();
            rotationYList = new List<float>();
            isFixedRotation = false;
        }

        #endregion

        #region Public Non-Static Methods

        /// <summary>
        /// Expand or collapse the details of each Member in this group
        /// </summary>
        /// <param name="isExpanded"></param>
        public void GroupMemberListExpand(bool isExpanded)
        {
            if (groupMemberList != null)
            {
                for (int grpMbrIdx = 0; grpMbrIdx < groupMemberList.Count; grpMbrIdx++)
                {
                    if (groupMemberList[grpMbrIdx] != null) { groupMemberList[grpMbrIdx].showInEditor = isExpanded; }
                }
            }
        }

        /// <summary>
        /// Expand or collapse the details of each Zone in this group
        /// </summary>
        /// <param name="isExpanded"></param>
        public void GroupZoneListExpand(bool isExpanded)
        {
            if (zoneList != null)
            {
                for (int znIdx = 0; znIdx < zoneList.Count; znIdx++)
                {
                    if (zoneList[znIdx] != null) { zoneList[znIdx].showInEditor = isExpanded; }
                }
            }
        }

        /// <summary>
        /// Enabled or disable group members. Optionally apply a filter.
        /// </summary>
        /// <param name="isEnabled"></param>
        public void EnableGroupMembers(bool isEnabled, string filter = "")
        {
            LBGroupMember lbGroupMember = null;
            if (groupMemberList != null)
            {
                for (int grpMbrIdx = 0; grpMbrIdx < groupMemberList.Count; grpMbrIdx++)
                {
                    lbGroupMember = groupMemberList[grpMbrIdx];

                    if (lbGroupMember != null) { if (lbGroupMember.IsInSearchFilter(filter)) { lbGroupMember.isDisabled = !isEnabled; } }
                }
            }
        }

        /// <summary>
        /// Move all Manual Clearing positions using an offset and scale them on each axis.
        /// If no offset is required, set offset to Vector3.zero
        /// If no scaling is required, set scale to Vector3.one
        /// This is typically called when importing a LBTemplate.
        /// See also LBGroup.LandscapeDimensionLocationChanged()
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="scale"></param>
        public void MoveClearings(Vector3 offset, Vector3 scale)
        {
            if (positionList != null)
            {
                int numPos = positionList.Count;
                for (int p = 0; p < numPos; p++)
                {
                    // Scale the clearing location, then add the offset
                    positionList[p] = new Vector3((positionList[p].x * scale.x), (positionList[p].y * scale.y), (positionList[p].z * scale.z));
                }
            }
        }

        /// <summary>
        /// To be used to populate a EditorGUILayout.Popup()
        /// GetMemberNameList(LBGroupMember.LBMemberType.Prefab).ToArray();
        /// </summary>
        /// <param name="memberType"></param>
        /// <returns></returns>
        public List<string> GetMemberNameList(LBGroupMember.LBMemberType memberType)
        {
            List<string> groupMemberNameList = new List<string>();
            string item;
            LBGroupMember lbGroupMember;

            int numGroupMembers = groupMemberList == null ? 0 : groupMemberList.Count;

            if (numGroupMembers > 0)
            {
                for (int m = 0; m < numGroupMembers; m++)
                {
                    lbGroupMember = groupMemberList[m];
                    item = string.Empty;
                    if (lbGroupMember != null && lbGroupMember.lbMemberType == memberType)
                    {
                        // Format number as 00 so that we can always extract it from the list
                        item = "Member " + (m + 1).ToString("00") + " - ";

                        if (memberType == LBGroupMember.LBMemberType.Prefab)
                        {
                            // Append the name of the prefab
                            if (lbGroupMember.prefab != null) { item += lbGroupMember.prefab.name; }
                            else { item += " no prefab"; }
                        }
                        else if (memberType == LBGroupMember.LBMemberType.ObjPath)
                        {
                            if (lbGroupMember.lbObjPath != null && !string.IsNullOrEmpty(lbGroupMember.lbObjPath.pathName))
                            {
                                item += lbGroupMember.lbObjPath.pathName;
                            }
                            else { item += " no path name"; }
                        }
                        else { item += " unknown member type"; }
                    }
                    if (!string.IsNullOrEmpty(item)) { groupMemberNameList.Add(item); }
                }
            }

            return groupMemberNameList;
        }

        /// <summary>
        /// Get the member which has the supplied GUID. Optionally ignore Disabled members.
        /// </summary>
        /// <param name="memberGUID"></param>
        /// <param name="isDisabledIgnored"></param>
        /// <returns></returns>
        public LBGroupMember GetMemberByGUID(string memberGUID, bool isDisabledIgnored = false)
        {
            if (groupMemberList != null)
            {
                return groupMemberList.Find(gmbr => gmbr.GUID == memberGUID && (!isDisabledIgnored || !gmbr.isDisabled) && !string.IsNullOrEmpty(gmbr.GUID));
            }
            else { return null; }   
        }

        /// <summary>
        /// Make any changes necessary when a group's radius is changed.
        /// Includes modifying zone position, width and/or length as required
        /// </summary>
        /// <param name="previousMaxRadius"></param>
        public void ResizeGroup(float previousMaxRadius)
        {
            int numZones = zoneList == null ? 0 : zoneList.Count;
            LBGroupZone lbGroupZone = null;

            // Avoid div0
            if (maxClearingRadius == 0f) { minClearingRadius = 0.01f; maxClearingRadius = 0.01f; }

            for (int zn = 0; zn < numZones; zn++)
            {
                lbGroupZone = zoneList[zn];
                if (lbGroupZone != null)
                {
                    // If not scaling, get the new normalised value for the x,z centre of the zone
                    if (!lbGroupZone.isScaledPointX) { lbGroupZone.centrePointX = (lbGroupZone.centrePointX * previousMaxRadius) / maxClearingRadius; lbGroupZone.currentCentrePointX = lbGroupZone.centrePointX; }
                    if (!lbGroupZone.isScaledPointZ) { lbGroupZone.centrePointZ = (lbGroupZone.centrePointZ * previousMaxRadius) / maxClearingRadius; lbGroupZone.currentCentrePointZ = lbGroupZone.centrePointZ; }

                    if (lbGroupZone.zoneType == LBGroupZone.LBGroupZoneType.rectangle)
                    {
                        if (!lbGroupZone.isScaledWidth) { lbGroupZone.width = (lbGroupZone.width * previousMaxRadius) / maxClearingRadius; lbGroupZone.currentZoneWidth = lbGroupZone.width; }
                        if (!lbGroupZone.isScaledLength) { lbGroupZone.length = (lbGroupZone.length * previousMaxRadius) / maxClearingRadius; lbGroupZone.currentZoneLength = lbGroupZone.length; }
                    }
                    else
                    {
                        if (!lbGroupZone.isScaledLength)
                        {
                            lbGroupZone.length = (lbGroupZone.length * previousMaxRadius) / maxClearingRadius;
                            lbGroupZone.currentZoneLength = lbGroupZone.length;

                            lbGroupZone.width = lbGroupZone.length;
                            lbGroupZone.currentZoneWidth = lbGroupZone.length;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns a list of Main ObjPrefabs for a given GroupMember with LBMemberType.ObjPath type.
        /// Only return ones which match and active GroupMember with a prefab.
        /// </summary>
        /// <param name="lbGroupMember"></param>
        /// <returns></returns>
        public List<LBObjPrefab> GetActiveMainObjPrefabList(LBGroupMember lbGroupMember)
        {
            List<LBObjPrefab> activeMainObjPrefabList = new List<LBObjPrefab>();
            LBObjPath lbObjPath = null;
            LBObjPrefab lbObjPrefab = null;

            if (lbGroupMember != null && lbGroupMember.lbMemberType == LBGroupMember.LBMemberType.ObjPath)
            {
                lbObjPath = lbGroupMember.lbObjPath;

                if (lbObjPath != null)
                {
                    int numMainObjPrefs = (lbObjPath.mainObjPrefabList == null ? 0 : lbObjPath.mainObjPrefabList.Count);

                    // Loop through all the Main Obj Prefabs (if any)
                    for (int prefabIdx = 0; prefabIdx < numMainObjPrefs; prefabIdx++)
                    {
                        lbObjPrefab = lbObjPath.mainObjPrefabList[prefabIdx];

                        // Does this Main Obj Prefab have a GUID?
                        if (!string.IsNullOrEmpty(lbObjPrefab.groupMemberGUID))
                        {
                            // Is this GUID valid and is the GroupMember enabled?
                            if (groupMemberList.FindIndex(gmbr => !gmbr.isDisabled && gmbr.GUID == lbObjPrefab.groupMemberGUID && !string.IsNullOrEmpty(gmbr.GUID)) >= 0)
                            {
                                activeMainObjPrefabList.Add(lbObjPrefab);
                            }
                        }
                    }
                }
            }

            return activeMainObjPrefabList;
        }

        /// <summary>
        /// Populates a list of Main ObjPrefabs for a given ObjPathSeries.
        /// Only adds ones which are active GroupMembers.
        /// Assumes the input list is empty (not null). This enables the list
        /// to be used multiple times and may avoid some Garbage Collection.
        /// </summary>
        /// <param name="lbObjPathSeries"></param>
        /// <param name="activeMainObjPrefabList"></param>
        public void GetActiveMainObjPrefabList(LBObjPathSeries lbObjPathSeries, List<LBObjPrefab> activeMainObjPrefabList)
        {
            if (lbObjPathSeries != null && activeMainObjPrefabList != null)
            {
                LBObjPrefab lbObjPrefab = null;

                int numMainObjPrefs = (lbObjPathSeries.mainObjPrefabList == null ? 0 : lbObjPathSeries.mainObjPrefabList.Count);

                // Loop through all the Main Obj Prefabs (if any)
                for (int prefabIdx = 0; prefabIdx < numMainObjPrefs; prefabIdx++)
                {
                    lbObjPrefab = lbObjPathSeries.mainObjPrefabList[prefabIdx];

                    // Does this Main Obj Prefab have a GUID?
                    if (!string.IsNullOrEmpty(lbObjPrefab.groupMemberGUID))
                    {
                        // Is this GUID valid and is the GroupMember enabled?
                        if (groupMemberList.FindIndex(gmbr => !gmbr.isDisabled && gmbr.GUID == lbObjPrefab.groupMemberGUID && !string.IsNullOrEmpty(gmbr.GUID)) >= 0)
                        {
                            activeMainObjPrefabList.Add(lbObjPrefab);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Populates a list of Main Obj SubGroups for a given ObjPathSeries.
        /// Only adds ones which have active groups of type SubGroup AND have
        /// at least one GroupMember.
        /// Assumes the input list is empty (not null). This enables the list
        /// to be used multiple times and may avoid some Garbage Collection.
        /// </summary>
        /// <param name="lbObjPathSeries"></param>
        /// <param name="activeGroupList"></param>
        /// <param name="activeMainObjSubGroupList"></param>
        public void GetActiveMainObjSubGroupList(LBObjPathSeries lbObjPathSeries, List<LBGroup> activeGroupList, List<LBObjSubGroup> activeMainObjSubGroupList)
        {
            if (lbObjPathSeries != null && activeMainObjSubGroupList != null)
            {
                LBObjSubGroup lbObjSubGroup = null;
                LBGroup subGroup = null;

                int numMainObjSubGroups = lbObjPathSeries.mainObjSubGroupList == null ? 0 : lbObjPathSeries.mainObjSubGroupList.Count;

                // Loop through all the Main Ob SubGroups (if any)
                for (int sgpIdx = 0; sgpIdx < numMainObjSubGroups; sgpIdx++)
                {
                    lbObjSubGroup = lbObjPathSeries.mainObjSubGroupList[sgpIdx];

                    // Does this Main Obj SubGroup have a GUID?
                    if (!string.IsNullOrEmpty(lbObjSubGroup.subGroupGUID))
                    {
                        // Is the SubGroup active and does it have any active members
                        subGroup = LBGroup.GetGroupByGUID(activeGroupList, lbObjSubGroup.subGroupGUID, true);

                        if (subGroup != null && !subGroup.isDisabled && subGroup.groupMemberList.Exists(gmbr => !gmbr.isDisabled && !string.IsNullOrEmpty(gmbr.GUID)))
                        {
                            activeMainObjSubGroupList.Add(lbObjSubGroup);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// For use with Manual Clearing groups, make sure the rotationY list has the correct
        /// number of entries that matches the number of manual positions.
        /// This is used when isFixedRotation is enabled.
        /// </summary>
        public void SyncRotationList()
        {
            int numPositions = positionList == null ? 0 : positionList.Count;
            int numRotations = rotationYList == null ? 0 : rotationYList.Count;

            if (numPositions < numRotations)
            {
                rotationYList.RemoveRange(numPositions, numRotations - numPositions);
            }
            else if (numPositions > numRotations)
            {
                if (rotationYList == null) { rotationYList = new List<float>(numPositions); }
                for (int rIdx = numRotations; rIdx < numPositions; rIdx++) { rotationYList.Add(0f); }
            }

            //Debug.Log("[DEBUG] SyncRotationList positions: " + numPositions + " rotations: " + (rotationYList == null ? 0 : rotationYList.Count));
        }

        /// <summary>
        /// Script out the group for use in a runtime script.
        /// GroupIdx is the zero-based position in the LBGroupList
        /// </summary>
        /// <param name="GroupIdx"></param>
        /// <param name="EndOfLineMarker"></param>
        /// <returns></returns>
        public string ScriptGroup(int GroupIdx, string EndOfLineMarker = "\n")
        {
            // Create a new instance of StringBuilder and give it an estimated capacity
            StringBuilder sb = new StringBuilder(1000);

            string eol = " ";

            // We always need a space between lines OR a end of line marker like "\n"
            if (EndOfLineMarker.Length > 0) { eol = EndOfLineMarker; }

            string groupInst = "lbGroup" + (GroupIdx + 1);
            string groupInstAbrev = "Grp" + (GroupIdx + 1);

            sb.Append("// Group Code generated from Landscape Builder 2 at " + System.DateTime.Now.ToShortTimeString() + " on " + System.DateTime.Now.ToLongDateString() + eol + eol);

            sb.Append("// BEGIN Public variables to populate in the editor - Uncomment and add these to top of class" + eol);
            sb.Append("//[Header(\"Group" + (GroupIdx + 1) + "\")] " + eol);
            // Generate the variables for the member prefabs
            for (int gm = 0; gm < groupMemberList.Count; gm++)
            {
                if (groupMemberList[gm].lbMemberType == LBGroupMember.LBMemberType.Prefab)
                {
                    sb.Append("// public GameObject Group" + (GroupIdx + 1).ToString("000") + "_Member" + (gm + 1).ToString("000") + "prefab;" + eol);
                }
                else if (groupMemberList[gm].lbMemberType == LBGroupMember.LBMemberType.ObjPath)
                {
                    LBObjPath lbObjPath = groupMemberList[gm].lbObjPath;
                    if (lbObjPath != null)
                    {
                        if (lbObjPath.useSurfaceMesh)
                        {
                            sb.Append("// public Material Group" + (GroupIdx + 1).ToString("000") + "_Member" + (gm + 1).ToString("000") + "surfaceMeshMat;" + eol);
                            sb.Append("// public Material Group" + (GroupIdx + 1).ToString("000") + "_Member" + (gm + 1).ToString("000") + "baseMeshMaterial;" + eol);
                        }
                    }
                }
            }

            sb.Append("//" + eol);
            sb.Append("// END Public variables" + eol + eol);

            #region Group
            sb.Append("#region Group" + (GroupIdx + 1) + (string.IsNullOrEmpty(groupName) ? " [NO NAME]" : " [" + groupName + "]") + eol);
            sb.Append("LBGroup " + groupInst + " = new LBGroup();" + eol);
            sb.Append("if (" + groupInst + " != null)" + eol);
            sb.Append("{" + eol);
            sb.Append("\t#region Group" + (GroupIdx + 1) + "-level variables" + eol);
            sb.Append("\t" + groupInst + ".groupName = " + (string.IsNullOrEmpty(groupName) ? "\"\"" : "\"" + groupName + "\"") + "; " + eol);
            sb.Append("\t" + groupInst + ".GUID = " + (string.IsNullOrEmpty(GUID) ? "\"\"" : "\"" + GUID + "\"") + "; " + eol);
            sb.Append("\t" + groupInst + ".lbGroupType = LBGroup.LBGroupType." + lbGroupType + ";" + eol);
            sb.Append("\t" + groupInst + ".maxGroupSqrKm = " + maxGroupSqrKm + ";" + eol);
            sb.Append("\t" + groupInst + ".isDisabled = " + isDisabled.ToString().ToLower() + ";" + eol);
            sb.Append("\t" + groupInst + ".showInEditor = " + showInEditor.ToString().ToLower() + ";" + eol);
            sb.Append("\t" + groupInst + ".showGroupDefaultsInEditor = " + showGroupDefaultsInEditor.ToString().ToLower() + ";" + eol);
            sb.Append("\t" + groupInst + ".showGroupOptionsInEditor = " + showGroupOptionsInEditor.ToString().ToLower() + ";" + eol);
            sb.Append("\t" + groupInst + ".showGroupMembersInEditor = " + showGroupMembersInEditor.ToString().ToLower() + ";" + eol);
            sb.Append("\t" + groupInst + ".showGroupDesigner = " + showGroupDesigner.ToString().ToLower() + ";" + eol);
            sb.Append("\t" + groupInst + ".showGroupsInScene = " + showGroupsInScene.ToString().ToLower() + ";" + eol);
            sb.Append("\t" + groupInst + ".showtabInEditor = " + showtabInEditor + ";" + eol);
            sb.Append("\t" + groupInst + ".isMemberListExpanded = " + isMemberListExpanded.ToString().ToLower() + ";" + eol);

            sb.Append("\t// Clearing Group variables" + eol);
            sb.Append("\t" + groupInst + ".minClearingRadius = " + minClearingRadius + "f;" + eol);
            sb.Append("\t" + groupInst + ".maxClearingRadius = " + maxClearingRadius + "f;" + eol);
            sb.Append("\t" + groupInst + ".isFixedRotation = " + isFixedRotation.ToString().ToLower() + ";" + eol);
            sb.Append("\t" + groupInst + ".startClearingRotationY = " + startClearingRotationY + "f;" + eol);
            sb.Append("\t" + groupInst + ".endClearingRotationY = " + endClearingRotationY + "f;" + eol);
            sb.Append("\t" + groupInst + ".isRemoveExistingGrass = " + isRemoveExistingGrass.ToString().ToLower() + ";" + eol);
            sb.Append("\t" + groupInst + ".removeExistingGrassBlendDist = " + removeExistingGrassBlendDist + "f;" + eol);
            sb.Append("\t" + groupInst + ".isRemoveExistingTrees = " + isRemoveExistingTrees.ToString().ToLower() + ";" + eol);

            sb.Append("\t// Proximity variables" + eol);
            sb.Append("\t" + groupInst + ".proximityExtent = " + proximityExtent + "f;" + eol);

            sb.Append("\t// Default values per group" + eol);
            sb.Append("\t" + groupInst + ".minScale = " + minScale + "f;" + eol);
            sb.Append("\t" + groupInst + ".maxScale = " + maxScale + "f;" + eol);
            sb.Append("\t" + groupInst + ".minHeight = " + minHeight + "f;" + eol);
            sb.Append("\t" + groupInst + ".maxHeight = " + maxHeight + "f;" + eol);
            sb.Append("\t" + groupInst + ".minInclination = " + minInclination + "f;" + eol);
            sb.Append("\t" + groupInst + ".maxInclination = " + maxInclination + "f;" + eol);

            sb.Append("\t// Group flatten terrain variables" + eol);
            sb.Append("\t" + groupInst + ".isTerrainFlattened = " + isTerrainFlattened.ToString().ToLower() + ";" + eol);
            sb.Append("\t" + groupInst + ".flattenHeightOffset = " + flattenHeightOffset + "f;" + eol);
            sb.Append("\t" + groupInst + ".flattenBlendRate = " + flattenBlendRate + "f;" + eol);
            sb.Append("\t#endregion" + eol);
            #endregion

            #region Zones
            // A zoneList is created when the new Group is created above
            sb.Append(eol);
            sb.Append("\t#region Group" + (GroupIdx + 1) + " Zones" + eol);
            sb.Append("\t// Start Group" + (GroupIdx + 1) + "-Level Zones" + eol);
            if (zoneList != null)
            {
                for (int zn = 0; zn < zoneList.Count; zn++)
                {
                    string groupZnInst = "lbGroupZone" + (zn + 1);
                    LBGroupZone lbGroupZone = zoneList[zn];

                    sb.Append("\tLBGroupZone " + groupZnInst + " = new LBGroupZone();" + eol);
                    sb.Append("\tif (" + groupZnInst + " != null)" + eol);
                    sb.Append("\t{" + eol);
                    sb.Append("\t\t" + groupZnInst + ".zoneType = LBGroupZone.LBGroupZoneType." + lbGroupZone.zoneType + ";" + eol);
                    sb.Append("\t\t" + groupZnInst + ".zoneMode = LBGroupZone.ZoneMode." + lbGroupZone.zoneMode + ";" + eol);
                    sb.Append("\t\t" + groupZnInst + ".zoneName = \"" + lbGroupZone.zoneName + "\";" + eol);
                    sb.Append("\t\t" + groupZnInst + ".GUID = \"" + lbGroupZone.GUID + "\";" + eol);
                    sb.Append("\t\t" + groupZnInst + ".centrePointX = " + lbGroupZone.centrePointX + "f;" + eol);
                    sb.Append("\t\t" + groupZnInst + ".centrePointZ = " + lbGroupZone.centrePointZ + "f;" + eol);
                    sb.Append("\t\t" + groupZnInst + ".width = " + lbGroupZone.width + "f;" + eol);
                    sb.Append("\t\t" + groupZnInst + ".length = " + lbGroupZone.length + "f;" + eol);
                    sb.Append("\t\t" + groupZnInst + ".isScaledPointX = " + lbGroupZone.isScaledPointX.ToString().ToLower() + ";" + eol);
                    sb.Append("\t\t" + groupZnInst + ".isScaledPointZ = " + lbGroupZone.isScaledPointZ.ToString().ToLower() + ";" + eol);
                    sb.Append("\t\t" + groupZnInst + ".isScaledWidth = " + lbGroupZone.isScaledWidth.ToString().ToLower() + ";" + eol);
                    sb.Append("\t\t" + groupZnInst + ".isScaledLength = " + lbGroupZone.isScaledLength.ToString().ToLower() + ";" + eol);
                    sb.Append("\t\t" + groupZnInst + ".useBiome = " + lbGroupZone.useBiome.ToString().ToLower() + ";" + eol);
                    if (lbGroupZone.useBiome && lbGroupZone.lbBiome != null)
                    {
                        sb.Append("\t\tLBBiome " + groupZnInst + "Biome = new LBBiome();" + eol);
                        sb.Append("\t\tif (" + groupZnInst + "Biome != null)" + eol);
                        sb.Append("\t\t{" + eol);
                        sb.Append("\t\t\t" + groupZnInst + "Biome.biomeIndex = " + lbGroupZone.lbBiome.biomeIndex + ";" + eol);
                        sb.Append("\t\t\t" + groupZnInst + "Biome.minBlendDist = " + lbGroupZone.lbBiome.minBlendDist + "f;" + eol);
                        sb.Append("\t\t}" + eol);
                    }
                    sb.Append("\t}" + eol);

                    sb.Append("\t" + groupInst + ".zoneList.Add(" + groupZnInst + ");" + eol);
                    sb.Append(eol);
                }
            }
            sb.Append("\t// End Group" + (GroupIdx + 1) + "-Level Zones" + eol);
            sb.Append("\t#endregion" + eol);
            #endregion

            #region Clearing Positions
            if (lbGroupType == LBGroupType.ManualClearing)
            {
                sb.Append(eol);
                sb.Append("\t#region Group" + (GroupIdx + 1) + " Manual Clearing Positions" + eol);
                sb.Append(eol + "\t// Start Group" + (GroupIdx + 1) + "-Level Manual Clearing Positions" + eol);

                if (positionList != null)
                {
                    foreach (Vector3 pos in positionList)
                    {
                        sb.Append("\t" + groupInst + ".positionList.Add(new Vector3(" + pos.x + "f, " + pos.y + "f, " + pos.z + "f));" + eol);
                    }
                }

                sb.Append(eol + "\t// End Group" + (GroupIdx + 1) + "-Level Manual Clearing Positions" + eol);
                sb.Append("\t#endregion" + eol);

                if (isFixedRotation)
                {
                    sb.Append(eol);
                    sb.Append("\t#region Group" + (GroupIdx + 1) + " Manual Clearing Y Rotations" + eol);
                    sb.Append(eol + "\t// Start Group" + (GroupIdx + 1) + "-Level Manual Clearing Y Rotations" + eol);

                    if (rotationYList != null)
                    {
                        foreach (float rotY in rotationYList)
                        {
                            sb.Append("\t" + groupInst + ".rotationYList.Add(" + rotY + "f));" + eol);
                        }
                    }

                    sb.Append(eol + "\t// End Group" + (GroupIdx + 1) + "-Level Manual Clearing Y Rotations" + eol);
                    sb.Append("\t#endregion" + eol);
                }
            }
            #endregion

            #region Stencil Filters
            // Filters - currently contains Stencils
            sb.Append(eol);
            sb.Append("\t#region Group" + (GroupIdx + 1) + " Filters" + eol);
            sb.Append("\t" + groupInst + ".filterList = new List<LBFilter>(); " + eol);
            if (filterList != null)
            {
                // Create a unique variable
                if (filterList.Exists(f => f.filterType == LBFilter.FilterType.StencilLayer))
                {
                    sb.Append("\tLBFilter lbFilter" + groupInstAbrev + " = null; " + eol);
                }

                for (int tf = 0; tf < filterList.Count; tf++)
                {
                    LBFilter lbFilter = filterList[tf];

                    if (lbFilter != null)
                    {
                        if (lbFilter.filterType == LBFilter.FilterType.StencilLayer)
                        {
                            sb.Append("\tlbFilter" + groupInstAbrev + " = LBFilter.CreateFilter(\"" + lbFilter.lbStencilGUID + "\", \"" + lbFilter.lbStencilLayerGUID + "\", false); " + eol);
                            sb.Append("\tif (lbFilter" + groupInstAbrev + " != null) " + eol);
                            sb.Append("\t{ " + eol);
                            sb.Append("\t\tlbFilter" + groupInstAbrev + ".filterMode = LBFilter.FilterMode." + lbFilter.filterMode + ";" + eol);
                            sb.Append("\t\t" + groupInst + ".filterList.Add(lbFilter" + groupInstAbrev + "); " + eol);
                            sb.Append("\t} " + eol);
                            sb.Append(eol);
                        }
                        else
                        {
                            sb.Append("\t// Currently we do not output non-Stencil filters for runtime Groups. Contact support or post in our Unity forum if you need this feature." + eol);
                        }
                    }
                }
            }
            sb.Append("\t" + groupInst + ".isClearingRadiusFiltered = " + isClearingRadiusFiltered.ToString().ToLower() + ";" + eol);
            sb.Append("\t#endregion" + eol);
            #endregion

            #region Textures
            sb.Append(eol);
            sb.Append("\t#region Group" + (GroupIdx + 1) + " Textures" + eol);
            sb.Append("\t// Only apply to Procedural and Manual Clearing groups" + eol);
            sb.Append("\t" + groupInst + ".textureList = new List<LBGroupTexture>(); " + eol);
            if (textureList != null)
            {
                if (textureList.Count > 0)
                {
                    string groupTexInst = "lbGroupTexture" + groupInstAbrev;
                    sb.Append("\tLBGroupTexture " + groupTexInst + " = null; " + eol);
                    for (int gtx = 0; gtx < textureList.Count; gtx++)
                    {
                        LBGroupTexture lbGroupTexture = textureList[gtx];

                        sb.Append("\t" + groupTexInst + " = new LBGroupTexture();" + eol);
                        sb.Append("\tif (" + groupTexInst + " != null)" + eol);
                        sb.Append("\t{" + eol);
                        sb.Append("\t\t" + groupTexInst + ".lbTerrainTextureGUID = \"" + lbGroupTexture.lbTerrainTextureGUID + "\";" + eol);
                        sb.Append("\t\t" + groupTexInst + ".isWholeGroup = " + lbGroupTexture.isWholeGroup.ToString().ToLower() + ";" + eol);
                        sb.Append("\t\t" + groupTexInst + ".minBlendDist = " + lbGroupTexture.minBlendDist + "f;" + eol);
                        sb.Append("\t\t" + groupTexInst + ".edgeBlendDist = " + lbGroupTexture.edgeBlendDist + "f;" + eol);
                        sb.Append("\t\t" + groupTexInst + ".strength = " + lbGroupTexture.strength + "f;" + eol);
                        sb.Append("\t\t" + groupTexInst + ".useNoise = " + lbGroupTexture.useNoise.ToString().ToLower() + ";" + eol);
                        sb.Append("\t\t" + groupTexInst + ".noiseTileSize = " + lbGroupTexture.noiseTileSize + "f;" + eol);
                        sb.Append("\t\t" + groupTexInst + ".noiseOffset = " + lbGroupTexture.noiseOffset + "f;" + eol);
                        sb.Append("\t\t" + groupTexInst + ".zoneGUIDList = new List<string>();" + eol);
                        if (lbGroupTexture.zoneGUIDList != null)
                        {
                            for (int zn=0; zn < lbGroupTexture.zoneGUIDList.Count; zn++)
                            {
                                sb.Append("\t\t" + groupTexInst + ".zoneGUIDList.Add(\"" + lbGroupTexture.zoneGUIDList[zn] + "\");" + eol);
                            }
                        }
                        sb.Append("\t" + groupInst + ".textureList.Add(" + groupTexInst + "); " + eol);
                        sb.Append("\t}" + eol);
                    }
                }
            }
            sb.Append("\t#endregion" + eol);
            #endregion

            #region Grass
            sb.Append(eol);
            sb.Append("\t#region Group" + (GroupIdx + 1) + " Grass" + eol);
            sb.Append("\t// Only apply to Procedural and Manual Clearing groups" + eol);
            sb.Append("\t" + groupInst + ".grassList = new List<LBGroupGrass>(); " + eol);
            if (grassList != null)
            {
                if (grassList.Count > 0)
                {
                    string groupGrsInst = "lbGroupTexture" + groupInstAbrev;
                    sb.Append("\tLBGroupGrass " + groupGrsInst + " = null; " + eol);
                    for (int ggrs = 0; ggrs < grassList.Count; ggrs++)
                    {
                        LBGroupGrass lbGroupGrass = grassList[ggrs];

                        sb.Append("\t " + groupGrsInst + " = new LBGroupGrass();" + eol);
                        sb.Append("\tif (" + groupGrsInst + " != null)" + eol);
                        sb.Append("\t{" + eol);

                        sb.Append("\t\t" + groupGrsInst + ".lbTerrainGrassGUID = \"" + lbGroupGrass.lbTerrainGrassGUID + "\";" + eol);
                        sb.Append("\t\t" + groupGrsInst + ".isWholeGroup = " + lbGroupGrass.isWholeGroup.ToString().ToLower() + ";" + eol);
                        sb.Append("\t\t" + groupGrsInst + ".minBlendDist = " + lbGroupGrass.minBlendDist + "f;" + eol);
                        sb.Append("\t\t" + groupGrsInst + ".edgeBlendDist = " + lbGroupGrass.edgeBlendDist + "f;" + eol);
                        sb.Append("\t\t" + groupGrsInst + ".minDensity = " + lbGroupGrass.minDensity + ";" + eol);
                        sb.Append("\t\t" + groupGrsInst + ".maxDensity = " + lbGroupGrass.maxDensity + ";" + eol);
                        sb.Append("\t\t" + groupGrsInst + ".useNoise = " + lbGroupGrass.useNoise.ToString().ToLower() + ";" + eol);
                        sb.Append("\t\t" + groupGrsInst + ".noiseTileSize = " + lbGroupGrass.noiseTileSize + "f;" + eol);
                        sb.Append("\t\t" + groupGrsInst + ".noiseOffset = " + lbGroupGrass.noiseOffset + "f;" + eol);
                        sb.Append("\t\t" + groupGrsInst + ".noiseOctaves = " + lbGroupGrass.noiseOctaves + ";" + eol);
                        sb.Append("\t\t" + groupGrsInst + ".grassPlacementCutoff = " + lbGroupGrass.grassPlacementCutoff + "f;" + eol);
                        sb.Append("\t\t" + groupGrsInst + ".zoneGUIDList = new List<string>();" + eol);

                        if (lbGroupGrass.zoneGUIDList != null)
                        {
                            for (int zn = 0; zn < lbGroupGrass.zoneGUIDList.Count; zn++)
                            {
                                sb.Append("\t\t" + groupGrsInst + ".zoneGUIDList.Add(\"" + lbGroupGrass.zoneGUIDList[zn] + "\");" + eol);
                            }
                        }
                        sb.Append("\t" + groupInst + ".grassList.Add(" + groupGrsInst + "); " + eol);
                        sb.Append("\t}" + eol);
                    }
                }
            }
            sb.Append("\t#endregion" + eol);
            #endregion

            #region Group Members
            sb.Append(eol + "\t// Start Group Members");

            // A new member list is created when a new Group is created above
            if (groupMemberList != null)
            {
                for (int gm = 0; gm < groupMemberList.Count; gm++)
                {
                    string groupMbrInst = "group" + (GroupIdx + 1) + "_lbGroupMember" + (gm + 1);
                    string groupMbrInstAbv = "grp" + (GroupIdx + 1) + "_gmbr" + (gm + 1);
                    LBGroupMember lbGroupMember = groupMemberList[gm];
                    sb.Append(eol);
                    sb.Append("\t#region Group" + (GroupIdx + 1) + " Member" + (gm+1) + " " + lbGroupMember.prefabName + eol);

                    sb.Append("\tLBGroupMember " + groupMbrInst + " = new LBGroupMember();" + eol);
                    sb.Append("\tif (" + groupMbrInst + " != null)" + eol);
                    sb.Append("\t{" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".isDisabled = " + lbGroupMember.isDisabled.ToString().ToLower() + ";" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".showInEditor = " + lbGroupMember.showInEditor.ToString().ToLower() + ";" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".showtabInEditor = " + lbGroupMember.showtabInEditor.ToString().ToLower() + ";" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".GUID = \"" + lbGroupMember.GUID + "\";" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".lbMemberType = LBGroupMember.LBMemberType." + lbGroupMember.lbMemberType + ";" + eol);

                    #region Placement rule variables
                    sb.Append("\t\t" + groupMbrInst + ".isGroupOverride = " + lbGroupMember.isGroupOverride.ToString().ToLower() + ";" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".minScale = " + lbGroupMember.minScale + "f;" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".maxScale = " + lbGroupMember.maxScale + "f;" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".minHeight = " + lbGroupMember.minHeight + "f;" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".maxHeight = " + lbGroupMember.maxHeight + "f;" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".minInclination = " + lbGroupMember.minInclination + "f;" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".maxInclination = " + lbGroupMember.maxInclination + "f;" + eol);
                    #endregion

                    #region Prefab varibles
                    if (lbGroupMember.lbMemberType == LBGroupMember.LBMemberType.Prefab)
                    {
                        sb.Append("\t\t" + groupMbrInst + ".prefab = Group" + (GroupIdx + 1).ToString("000") + "_Member" + (gm + 1).ToString("000") + "prefab;" + eol);
                        sb.Append("\t\t" + groupMbrInst + ".prefabName = \"" + lbGroupMember.prefabName + "\";" + eol);
                    }
                    else
                    {
                        sb.Append("\t\t" + groupMbrInst + ".prefab = null;" + eol);
                        sb.Append("\t\t" + groupMbrInst + ".prefabName = \"\";" + eol);
                    }                 
                    sb.Append("\t\t" + groupMbrInst + ".showPrefabPreview = " + lbGroupMember.showPrefabPreview.ToString().ToLower() + ";" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".isKeepPrefabConnection = " + lbGroupMember.isKeepPrefabConnection.ToString().ToLower() + ";" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".isCombineMesh = " + lbGroupMember.isCombineMesh.ToString().ToLower() + ";" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".isRemoveEmptyGameObjects = " + lbGroupMember.isRemoveEmptyGameObjects.ToString().ToLower() + ";" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".isRemoveAnimator = " + lbGroupMember.isRemoveAnimator.ToString().ToLower() + ";" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".isCreateCollider = " + lbGroupMember.isCreateCollider.ToString().ToLower() + ";" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".maxPrefabSqrKm = " + lbGroupMember.maxPrefabSqrKm + ";" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".maxPrefabPerGroup = " + lbGroupMember.maxPrefabPerGroup + ";" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".isPlacedInCentre = " + lbGroupMember.isPlacedInCentre.ToString().ToLower() + ";" + eol);

                    sb.Append("\t\t" + groupMbrInst + ".showXYZSettings = " + lbGroupMember.showXYZSettings.ToString().ToLower() + "; " + eol);

                    // Prefab offset variables
                    sb.Append("\t\t" + groupMbrInst + ".modelOffsetX = " + lbGroupMember.modelOffsetX + "f;" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".modelOffsetY = " + lbGroupMember.modelOffsetY + "f;" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".modelOffsetZ = " + lbGroupMember.modelOffsetZ + "f;" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".minOffsetX = " + lbGroupMember.minOffsetX + "f;" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".minOffsetZ = " + lbGroupMember.minOffsetZ + "f;" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".minOffsetY = " + lbGroupMember.minOffsetY + "f;" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".maxOffsetY = " + lbGroupMember.maxOffsetY + "f;" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".randomiseOffsetY = " + lbGroupMember.randomiseOffsetY.ToString().ToLower() + ";" + eol);

                    // Prefab rotation variables
                    sb.Append("\t\t" + groupMbrInst + ".rotationType = LBGroupMember.LBRotationType." + lbGroupMember.rotationType + ";" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".randomiseRotationY = " + lbGroupMember.randomiseRotationY.ToString().ToLower() + ";" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".startRotationY = " + lbGroupMember.startRotationY + "f;" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".endRotationY = " + lbGroupMember.endRotationY + "f;" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".randomiseRotationXZ = " + lbGroupMember.randomiseRotationXZ.ToString().ToLower() + ";" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".rotationX = " + lbGroupMember.rotationX + "f;" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".endRotationX = " + lbGroupMember.endRotationX + "f;" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".rotationZ = " + lbGroupMember.rotationZ + "f;" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".endRotationZ = " + lbGroupMember.endRotationZ + "f;" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".isLockTilt = " + lbGroupMember.isLockTilt.ToString().ToLower() + "; " + eol);
                    #endregion

                    #region Noise variables
                    sb.Append("\t\t" + groupMbrInst + ".useNoise = " + lbGroupMember.useNoise.ToString().ToLower() + ";" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".noiseOffset = " + lbGroupMember.noiseOffset + "f;" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".noiseTileSize = " + lbGroupMember.noiseTileSize + "f;" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".noisePlacementCutoff = " + lbGroupMember.noisePlacementCutoff + "f;" + eol);
                    #endregion

                    #region Proximity variables
                    sb.Append("\t\t" + groupMbrInst + ".proximityExtent = " + lbGroupMember.proximityExtent + "f;" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".removeGrassBlendDist = " + lbGroupMember.removeGrassBlendDist.ToString().ToLower() + "f;" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".minGrassProximity = " + lbGroupMember.minGrassProximity + "f;" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".isRemoveTree = " + lbGroupMember.isRemoveTree.ToString().ToLower() + ";" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".minTreeProximity = " + lbGroupMember.minTreeProximity + "f;" + eol);
                    #endregion

                    // Terrain Alignment
                    sb.Append("\t\t" + groupMbrInst + ".isTerrainAligned = " + lbGroupMember.isTerrainAligned.ToString().ToLower() + ";" + eol);

                    #region Prefab flatten terrain variables
                    sb.Append("\t\t" + groupMbrInst + ".isTerrainFlattened = " + lbGroupMember.isTerrainFlattened.ToString().ToLower() + ";" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".flattenDistance = " + lbGroupMember.flattenDistance + "f;" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".flattenHeightOffset = " + lbGroupMember.flattenHeightOffset + "f;" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".flattenBlendRate = " + lbGroupMember.flattenBlendRate + "f;" + eol);
                    #endregion

                    #region Zones
                    // Zones - a new zoneGUIDList is created when a GroupMember is created above
                    sb.Append(eol + "\t\t// Start Member-Level Zones references for " + groupMbrInst + eol);
                    if (lbGroupMember.zoneGUIDList != null)
                    {
                        for (int zn = 0; zn < lbGroupMember.zoneGUIDList.Count; zn++)
                        {
                            sb.Append("\t\t" + groupMbrInst + ".zoneGUIDList.Add(\"" + lbGroupMember.zoneGUIDList[zn] + "\");" + eol);
                        }                    
                    }
                    sb.Append(eol + "\t\t// End Member-Level Zones references for " + groupMbrInst + eol + eol);

                    // Zone edge settings
                    sb.Append("\t\t" + groupMbrInst + ".isZoneEdgeFillTop = " + lbGroupMember.isZoneEdgeFillTop.ToString().ToLower() + ";" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".isZoneEdgeFillBottom = " + lbGroupMember.isZoneEdgeFillBottom.ToString().ToLower() + ";" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".isZoneEdgeFillLeft = " + lbGroupMember.isZoneEdgeFillLeft.ToString().ToLower() + ";" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".isZoneEdgeFillRight = " + lbGroupMember.isZoneEdgeFillRight.ToString().ToLower() + ";" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".zoneEdgeFillDistance = " + lbGroupMember.zoneEdgeFillDistance + "f;" + eol);
                    #endregion

                    // Path Member settings
                    sb.Append("\t\t" + groupMbrInst + ".isPathOnly = " + lbGroupMember.isPathOnly.ToString().ToLower() + ";" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".usePathHeight = " + lbGroupMember.usePathHeight.ToString().ToLower() + ";" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".usePathSlope = " + lbGroupMember.usePathSlope.ToString().ToLower() + ";" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".useTerrainTrend = " + lbGroupMember.useTerrainTrend.ToString().ToLower() + ";" + eol);
                    sb.Append("\t\t" + groupMbrInst + ".lbObjectOrientation = LBObjPath.LBObjectOrientation." + lbGroupMember.lbObjectOrientation + ";" + eol);

                    #region ObjPath settings
                    if (lbGroupMember.lbMemberType == LBGroupMember.LBMemberType.ObjPath && lbGroupMember.lbObjPath != null)
                    {
                        string pathName = (string.IsNullOrEmpty(lbGroupMember.lbObjPath.pathName) ? "No Name Obj Path" : lbGroupMember.lbObjPath.pathName);
                        //string pathInstAbrev = pathName.Replace(" ", "");

                        sb.Append(eol + "\t\t#region Start Object Path settings for [" + pathName + "]" + eol);

                        #region Path Settings
                        sb.Append("\t\t" + groupMbrInst  + ".lbObjPath = new LBObjPath();" + eol);
                        sb.Append("\t\tif (" + groupMbrInst + ".lbObjPath != null)" + eol);
                        sb.Append("\t\t{" + eol);
                        sb.Append("\t\t\t// LBPath settings" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.pathName = \"" + pathName + "\";" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.showPathInScene = false;" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.blendStart = " + lbGroupMember.lbObjPath.blendStart.ToString().ToLower() + ";" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.blendEnd = " + lbGroupMember.lbObjPath.blendEnd.ToString().ToLower() + ";" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.pathResolution = " + lbGroupMember.lbObjPath.pathResolution + "f;" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.closedCircuit = " + lbGroupMember.lbObjPath.closedCircuit.ToString().ToLower() + ";" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.edgeBlendWidth = " + lbGroupMember.lbObjPath.edgeBlendWidth + "f;" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.removeCentre = " + lbGroupMember.lbObjPath.removeCentre.ToString().ToLower() + ";" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.leftBorderWidth = " + lbGroupMember.lbObjPath.leftBorderWidth + "f;" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.rightBorderWidth = " + lbGroupMember.lbObjPath.rightBorderWidth + "f;" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.snapToTerrain = " + lbGroupMember.lbObjPath.snapToTerrain.ToString().ToLower() + ";" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.heightAboveTerrain = " + lbGroupMember.lbObjPath.heightAboveTerrain + "f;" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.zoomOnFind = " + lbGroupMember.lbObjPath.zoomOnFind.ToString().ToLower() + ";" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.findZoomDistance = " + lbGroupMember.lbObjPath.findZoomDistance + "f;" + eol);
                        #endregion

                        #region Path Mesh options
                        sb.Append("\t\t\t// LBPath Surface options" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.isMeshLandscapeUV = " + lbGroupMember.lbObjPath.isMeshLandscapeUV.ToString().ToLower() + ";" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.meshUVTileScale = new Vector2(" + lbGroupMember.lbObjPath.meshUVTileScale.x + "f," + lbGroupMember.lbObjPath.meshUVTileScale.y + "f);" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.meshYOffset = " + lbGroupMember.lbObjPath.meshYOffset + "f;" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.meshEdgeSnapToTerrain = " + lbGroupMember.lbObjPath.meshEdgeSnapToTerrain.ToString().ToLower() + ";" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.meshSnapType = LBPath.MeshSnapType." + lbGroupMember.lbObjPath.meshSnapType + ";" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.meshIsDoubleSided = " + lbGroupMember.lbObjPath.meshIsDoubleSided.ToString().ToLower() + ";" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.meshIncludeEdges = " + lbGroupMember.lbObjPath.meshIncludeEdges.ToString().ToLower() + ";" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.meshIncludeWater = " + lbGroupMember.lbObjPath.meshIncludeWater.ToString().ToLower() + ";" + eol);
                        if (lbGroupMember.lbObjPath.meshIncludeWater) { sb.Append("\t\t\t// Add include water coder here" + eol); }
                        // Never want to save mesh to project folder at runtime
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.meshSaveToProjectFolder = false;" + eol);
                        #endregion

                        #region Object Path points
                        // Generate the list of points from the lbObjPath
                        sb.Append("\t\t\t// Path Points" + eol);
                        if (lbGroupMember.lbObjPath.positionList != null)
                        {
                            foreach (Vector3 pt in lbGroupMember.lbObjPath.positionList)
                            {
                                sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.positionList.Add(new Vector3(" + pt.x + "f," + pt.y + "f," + pt.z + "f));" + eol);
                            }

                            // Path width
                            if (lbGroupMember.lbObjPath.useWidth && lbGroupMember.lbObjPath.widthList != null && lbGroupMember.lbObjPath.positionListLeftEdge != null && lbGroupMember.lbObjPath.positionListRightEdge != null)
                            {
                                foreach (float widthf in lbGroupMember.lbObjPath.widthList)
                                {
                                    sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.widthList.Add(" + widthf + "f);" + eol);
                                }
                                foreach (Vector3 pt in lbGroupMember.lbObjPath.positionListLeftEdge)
                                {
                                    sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.positionListLeftEdge.Add(new Vector3(" + pt.x + "f," + pt.y + "f," + pt.z + "f));" + eol);
                                }
                                foreach (Vector3 pt in lbGroupMember.lbObjPath.positionListRightEdge)
                                {
                                    sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.positionListRightEdge.Add(new Vector3(" + pt.x + "f," + pt.y + "f," + pt.z + "f));" + eol);
                                }

                                // Update the locally cached minimum path width
                                sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.minPathWidth = " + groupMbrInst + ".lbObjPath.GetMinWidth();" + eol);
                            }
                        }
                        #endregion

                        #region LBObjPath settings
                        sb.Append("\t\t\t// LBObjPath settings" + eol);

                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.useWidth = " + lbGroupMember.lbObjPath.useWidth.ToString().ToLower() + ";" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.useSurfaceMesh = " + lbGroupMember.lbObjPath.useSurfaceMesh.ToString().ToLower() + ";" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.isCreateSurfaceMeshCollider = " + lbGroupMember.lbObjPath.isCreateSurfaceMeshCollider.ToString().ToLower() + ";" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.layoutMethod = LBObjPath.LayoutMethod." + lbGroupMember.lbObjPath.layoutMethod + ";" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.selectionMethod = LBObjPath.SelectionMethod." + lbGroupMember.lbObjPath.selectionMethod + ";" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.spacingDistance = " + lbGroupMember.lbObjPath.spacingDistance + "f;" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.maxMainPrefabs = " + lbGroupMember.lbObjPath.maxMainPrefabs + ";" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.isLastObjSnappedToEnd = " + lbGroupMember.lbObjPath.isLastObjSnappedToEnd.ToString().ToLower() + ";" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.isRandomisePerGroupRegion = " + lbGroupMember.lbObjPath.isRandomisePerGroupRegion.ToString().ToLower() + ";" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.surroundSmoothing = " + lbGroupMember.lbObjPath.surroundSmoothing + "f;" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.addTerrainHeight = " + lbGroupMember.lbObjPath.addTerrainHeight + "f;" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.isSwitchMeshUVs = " + lbGroupMember.lbObjPath.isSwitchMeshUVs.ToString().ToLower() + ";" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.isSwitchBaseMeshUVs = " + lbGroupMember.lbObjPath.isSwitchBaseMeshUVs.ToString().ToLower() + ";" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.baseMeshThickness = " + lbGroupMember.lbObjPath.baseMeshThickness + "f;" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.baseMeshUVTileScale = new Vector2(" + lbGroupMember.lbObjPath.baseMeshUVTileScale.x + "f," + lbGroupMember.lbObjPath.baseMeshUVTileScale.y + "f);" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.baseMeshUseIndent = " + lbGroupMember.lbObjPath.baseMeshUseIndent.ToString().ToLower() + ";" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.isCreateBaseMeshCollider = " + lbGroupMember.lbObjPath.isCreateBaseMeshCollider.ToString().ToLower() + ";" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.coreTextureGUID = \"" + lbGroupMember.lbObjPath.coreTextureGUID + "\";" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.coreTextureNoiseTileSize = " + lbGroupMember.lbObjPath.coreTextureNoiseTileSize + "f;" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.coreTextureStrength = " + lbGroupMember.lbObjPath.coreTextureStrength + "f;" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.surroundTextureGUID = \"" + lbGroupMember.lbObjPath.surroundTextureGUID + "\";" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.surroundTextureNoiseTileSize = " + lbGroupMember.lbObjPath.surroundTextureNoiseTileSize + "f;" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.surroundTextureStrength = " + lbGroupMember.lbObjPath.surroundTextureStrength + "f;" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.isRemoveExistingGrass = " + lbGroupMember.lbObjPath.isRemoveExistingGrass.ToString().ToLower() + ";" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.isRemoveExistingTrees = " + lbGroupMember.lbObjPath.isRemoveExistingTrees.ToString().ToLower() + ";" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.treeDistFromEdge = " + lbGroupMember.lbObjPath.treeDistFromEdge + "f;" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.useBiomes = " + lbGroupMember.lbObjPath.useBiomes.ToString().ToLower() + ";" + eol);

                        if (lbGroupMember.lbObjPath.useSurfaceMesh)
                        {
                            sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.surfaceMeshMaterial = Group" + (GroupIdx + 1).ToString("000") + "_Member" + (gm + 1).ToString("000") + "surfaceMeshMat;" + eol);
                            sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.baseMeshMaterial = Group" + (GroupIdx + 1).ToString("000") + "_Member" + (gm + 1).ToString("000") + "baseMeshMaterial;" + eol);
                        }
                        else
                        {
                            sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.surfaceMeshMaterial = null;" + eol);
                            sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.baseMeshMaterial = null;" + eol);
                        }

                        #endregion

                        #region ObjPath Curves
                        sb.Append("\t\t\t// ObjPath Curves" + eol);
                        sb.Append(LBCurve.ScriptCurve(lbGroupMember.lbObjPath.profileHeightCurve, "\n", groupMbrInstAbv + "profileHeightCurve"));
                        sb.Append(LBCurve.ScriptCurve(lbGroupMember.lbObjPath.surroundBlendCurve, "\n", groupMbrInstAbv + "surroundBlendCurve"));

                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.profileHeightCurve = " + groupMbrInstAbv + "profileHeightCurve;" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.surroundBlendCurve = " + groupMbrInstAbv + "surroundBlendCurve;" + eol);

                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.profileHeightCurvePreset = LBCurve.ObjPathHeightCurvePreset." + lbGroupMember.lbObjPath.profileHeightCurvePreset + ";" + eol);
                        sb.Append(eol);
                        #endregion

                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.pathPointList = new List<LBPathPoint>();" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.mainObjPrefabList = new List<LBObjPrefab>();" + eol);

                        int numPathPoints = lbGroupMember.lbObjPath.pathPointList == null ? 0 : lbGroupMember.lbObjPath.pathPointList.Count;
                        int numMainObjPrefabs = lbGroupMember.lbObjPath.mainObjPrefabList == null ? 0 : lbGroupMember.lbObjPath.mainObjPrefabList.Count;

                        #region ObjPath pathPointList
                        if (numPathPoints > 0)
                        {
                            sb.Append("\t\t\t// ObjPath Points" + eol);
                            LBPathPoint lbPathPoint = null;

                            // Re-use the same LBPathPoint
                            string pathPointInst = groupMbrInst + "PathPoint";
                            sb.Append("\t\t\tLBPathPoint " + pathPointInst + " = null;" + eol);

                            for (int ptIdx = 0; ptIdx < numPathPoints; ptIdx++)
                            {
                                lbPathPoint = lbGroupMember.lbObjPath.pathPointList[ptIdx];
                                if (lbPathPoint != null)
                                {
                                    sb.Append("\t\t\t" + pathPointInst + " = new LBPathPoint();" + eol);
                                    sb.Append("\t\t\t" + pathPointInst + ".GUID = \"" + lbPathPoint.GUID + "\";" + eol);
                                    sb.Append("\t\t\t" + pathPointInst + ".showInEditor = false;" + eol);
                                    sb.Append("\t\t\t" + pathPointInst + ".rotationZ = " + lbPathPoint.rotationZ + "f;" + eol);
                                    sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.pathPointList.Add(" + pathPointInst + ");" + eol);
                                    sb.Append("\t\t\t" + pathPointInst + " = null;" + eol);
                                }
                            }
                        }
                        #endregion

                        #region Main, Start and End Prefabs
                        if (numMainObjPrefabs > 0)
                        {
                            sb.Append("\t\t\t// Main ObjPrefabs" + eol);
                            string objInst = groupMbrInst + "ObjPathPrefab";
                            sb.Append("\t\t\tLBObjPrefab " + objInst + " = null;" + eol);
                            
                            for (int objPrefabIdx = 0; objPrefabIdx < numMainObjPrefabs; objPrefabIdx++)
                            {
                                sb.Append("\t\t\t" + objInst + " = new LBObjPrefab();" + eol);
                                sb.Append("\t\t\t" + objInst + ".groupMemberGUID = \"" + lbGroupMember.lbObjPath.mainObjPrefabList[objPrefabIdx].groupMemberGUID + "\";" + eol);
                                sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.mainObjPrefabList.Add(" + objInst + ");" + eol);
                                sb.Append("\t\t\t" + objInst + " = null;" + eol);
                            }
                        }

                        if (lbGroupMember.lbObjPath.startObjPrefab != null)
                        {
                            sb.Append("\t\t\t// Start ObjPrefab" + eol);
                            sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.startObjPrefab = new LBObjPrefab();" + eol);
                            sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.startObjPrefab.groupMemberGUID = \"" + lbGroupMember.lbObjPath.startObjPrefab.groupMemberGUID + "\";" + eol);
                        }

                        if (lbGroupMember.lbObjPath.endObjPrefab != null)
                        {
                            sb.Append("\t\t\t// End ObjPrefab" + eol);
                            sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.endObjPrefab = new LBObjPrefab();" + eol);
                            sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.endObjPrefab.groupMemberGUID = \"" + lbGroupMember.lbObjPath.endObjPrefab.groupMemberGUID + "\";" + eol);
                        }
                        #endregion

                        #region ObjPathSeries

                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.lbObjPathSeriesList = new List<LBObjPathSeries>();" + eol);

                        int numObjPathSeries = lbGroupMember.lbObjPath.lbObjPathSeriesList == null ? 0 : lbGroupMember.lbObjPath.lbObjPathSeriesList.Count;
                        if (numObjPathSeries > 0)
                        {
                            sb.Append(eol);
                            sb.Append("\t\t\t#region ObjPathSeries" + eol);
                            LBObjPathSeries lbObjPathSeries = null;
                            sb.Append(eol);

                            // Re-use the same LBObjPathSeries
                            string pathSeriesInst = groupMbrInst + "ObjPathSeries";
                            sb.Append("\t\t\tLBObjPathSeries " + pathSeriesInst + " = null;" + eol);

                            for (int osIdx = 0; osIdx < numObjPathSeries; osIdx++)
                            {
                                lbObjPathSeries = lbGroupMember.lbObjPath.lbObjPathSeriesList[osIdx];
                                if (lbObjPathSeries != null)
                                {
                                    sb.Append("\t\t\t#region Series " + (osIdx+1).ToString() + eol);
                                    sb.Append("\t\t\t" + pathSeriesInst + " = new LBObjPathSeries();" + eol);
                                    sb.Append("\t\t\t" + pathSeriesInst + ".seriesGUID = \"" + lbObjPathSeries.seriesGUID + "\";" + eol);
                                    sb.Append("\t\t\t" + pathSeriesInst + ".seriesName = \"" + lbObjPathSeries.seriesName + "\";" + eol);
                                    sb.Append("\t\t\t" + pathSeriesInst + ".showInEditor = false;" + eol);
                                    sb.Append("\t\t\t" + pathSeriesInst + ".layoutMethod = LBObjPath.LayoutMethod." + lbObjPathSeries.layoutMethod + ";" + eol);
                                    sb.Append("\t\t\t" + pathSeriesInst + ".selectionMethod = LBObjPath.SelectionMethod." + lbObjPathSeries.selectionMethod + ";" + eol);
                                    sb.Append("\t\t\t" + pathSeriesInst + ".useSubGroups = " + lbObjPathSeries.useSubGroups.ToString().ToLower() + ";" + eol);

                                    sb.Append("\t\t\t" + pathSeriesInst + ".mainObjPrefabList = new List<LBObjPrefab>();" + eol);

                                    numMainObjPrefabs = lbObjPathSeries.mainObjPrefabList == null ? 0 : lbObjPathSeries.mainObjPrefabList.Count;

                                    if (numMainObjPrefabs > 0)
                                    {
                                        sb.Append("\t\t\t// Series Main ObjPrefabs" + eol);
                                        string objInst = groupMbrInst + "ObjPathPrefab" + osIdx.ToString();
                                        sb.Append("\t\t\tLBObjPrefab " + objInst + " = null;" + eol);

                                        for (int objPrefabIdx = 0; objPrefabIdx < numMainObjPrefabs; objPrefabIdx++)
                                        {
                                            sb.Append("\t\t\t" + objInst + " = new LBObjPrefab();" + eol);
                                            sb.Append("\t\t\t" + objInst + ".groupMemberGUID = \"" + lbObjPathSeries.mainObjPrefabList[objPrefabIdx].groupMemberGUID + "\";" + eol);
                                            sb.Append("\t\t\t" + pathSeriesInst + ".mainObjPrefabList.Add(" + objInst + ");" + eol);
                                            sb.Append("\t\t\t" + objInst + " = null;" + eol);
                                        }
                                    }

                                    if (lbObjPathSeries.startObjPrefab != null)
                                    {
                                        sb.Append("\t\t\t// Series Start ObjPrefab" + eol);
                                        sb.Append("\t\t\t" + pathSeriesInst + ".startObjPrefab = new LBObjPrefab();" + eol);
                                        sb.Append("\t\t\t" + pathSeriesInst + ".startObjPrefab.groupMemberGUID = \"" + lbObjPathSeries.startObjPrefab.groupMemberGUID + "\";" + eol);
                                    }

                                    if (lbObjPathSeries.endObjPrefab != null)
                                    {
                                        sb.Append("\t\t\t// Series End ObjPrefab" + eol);
                                        sb.Append("\t\t\t" + pathSeriesInst + ".endObjPrefab = new LBObjPrefab();" + eol);
                                        sb.Append("\t\t\t" + pathSeriesInst + ".endObjPrefab.groupMemberGUID = \"" + lbObjPathSeries.endObjPrefab.groupMemberGUID + "\";" + eol);
                                    }

                                    int numMainObjSubGroups = lbObjPathSeries.mainObjSubGroupList == null ? 0 : lbObjPathSeries.mainObjSubGroupList.Count;

                                    sb.Append("\t\t\t" + pathSeriesInst + ".mainObjSubGroupList = new List<LBObjSubGroup>();" + eol);

                                    if (numMainObjSubGroups > 0)
                                    {
                                        sb.Append("\t\t\t// Series Main ObjSubGroups" + eol);
                                        string objInst = groupMbrInst + "ObjPathSubGroup" + osIdx.ToString();
                                        sb.Append("\t\t\tLBObjSubGroup " + objInst + " = null;" + eol);

                                        for (int objSubGroupIdx = 0; objSubGroupIdx < numMainObjSubGroups; objSubGroupIdx++)
                                        {
                                            sb.Append("\t\t\t" + objInst + " = new LBObjSubGroup();" + eol);
                                            sb.Append("\t\t\t" + objInst + ".subGroupGUID = \"" + lbObjPathSeries.mainObjSubGroupList[objSubGroupIdx].subGroupGUID + "\";" + eol);
                                            sb.Append("\t\t\t" + pathSeriesInst + ".mainObjSubGroupList.Add(" + objInst + ");" + eol);
                                            sb.Append("\t\t\t" + objInst + " = null;" + eol);
                                        }
                                    }

                                    if (lbObjPathSeries.startObjSubGroup != null)
                                    {
                                        sb.Append("\t\t\t// Series Start ObjSubGroup" + eol);
                                        sb.Append("\t\t\t" + pathSeriesInst + ".startObjSubGroup = new LBObjSubGroup();" + eol);
                                        sb.Append("\t\t\t" + pathSeriesInst + ".startObjSubGroup.subGroupGUID = \"" + lbObjPathSeries.startObjSubGroup.subGroupGUID + "\";" + eol);
                                    }

                                    if (lbObjPathSeries.endObjSubGroup != null)
                                    {
                                        sb.Append("\t\t\t// Series End ObjPrefab" + eol);
                                        sb.Append("\t\t\t" + pathSeriesInst + ".endObjSubGroup = new LBObjSubGroup();" + eol);
                                        sb.Append("\t\t\t" + pathSeriesInst + ".endObjSubGroup.subGroupGUID = \"" + lbObjPathSeries.endObjSubGroup.subGroupGUID + "\";" + eol);
                                    }

                                    // Start/End Member offset for the startObjPreb/endObjPrefab
                                    sb.Append("\t\t\t" + pathSeriesInst + ".startMemberOffset = " + lbObjPathSeries.startMemberOffset + "f;" + eol);
                                    sb.Append("\t\t\t" + pathSeriesInst + ".endMemberOffset = " + lbObjPathSeries.endMemberOffset + "f;" + eol);

                                    sb.Append("\t\t\t" + pathSeriesInst + ".spacingDistance = " + lbObjPathSeries.spacingDistance + "f;" + eol);
                                    sb.Append("\t\t\t" + pathSeriesInst + ".maxMainPrefabs = " + lbObjPathSeries.maxMainPrefabs + ";" + eol);
                                    sb.Append("\t\t\t" + pathSeriesInst + ".isLastObjSnappedToEnd = " + lbObjPathSeries.isLastObjSnappedToEnd.ToString().ToLower() + ";" + eol);
                                    sb.Append("\t\t\t" + pathSeriesInst + ".sparcePlacementCutoff = " + lbObjPathSeries.sparcePlacementCutoff + "f;" + eol);
                                    sb.Append("\t\t\t" + pathSeriesInst + ".pathSpline = LBObjPath.PathSpline." + lbObjPathSeries.pathSpline + ";" + eol);
                                    sb.Append("\t\t\t" + pathSeriesInst + ".prefabOffsetZ = " + lbObjPathSeries.prefabOffsetZ + "f;" + eol);
                                    sb.Append("\t\t\t" + pathSeriesInst + ".prefabStartOffset = " + lbObjPathSeries.prefabStartOffset + "f;" + eol);
                                    sb.Append("\t\t\t" + pathSeriesInst + ".prefabEndOffset = " + lbObjPathSeries.prefabEndOffset + "f;" + eol);
                                    sb.Append("\t\t\t" + pathSeriesInst + ".use3DDistance = " + lbObjPathSeries.use3DDistance.ToString().ToLower() + ";" + eol);
                                    sb.Append("\t\t\t" + pathSeriesInst + ".useNonOffsetDistance = " + lbObjPathSeries.useNonOffsetDistance.ToString().ToLower() + ";" + eol);
                                    sb.Append("\t\t\t" + pathSeriesInst + ".isRandomisePerGroupRegion = " + lbObjPathSeries.isRandomisePerGroupRegion.ToString().ToLower() + ";" + eol);

                                    sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.lbObjPathSeriesList.Add(" + pathSeriesInst + ");" + eol);
                                    sb.Append("\t\t\t" + pathSeriesInst + " = null;" + eol);
                                    sb.Append("\t\t\t#endregion Series" + eol);
                                    sb.Append(eol);
                                }
                            }

                            sb.Append("\t\t\t#endregion ObjPathSeries" + eol);
                            sb.Append(eol);
                        }

                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.isSeriesListOverride = " + lbGroupMember.lbObjPath.isSeriesListOverride.ToString().ToLower() + ";" + eol);
                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.seriesListGroupMemberGUID = \"" + lbGroupMember.lbObjPath.seriesListGroupMemberGUID + "\";" + eol);

                        #endregion

                        #region Biomes
                        if (lbGroupMember.lbObjPath.useBiomes)
                        {
                            sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.lbBiomeList = new List<LBBiome>();" + eol);

                            int numBiomes = lbGroupMember.lbObjPath.lbBiomeList == null ? 0 : lbGroupMember.lbObjPath.lbBiomeList.Count;
                            if (numBiomes > 0)
                            {
                                sb.Append(eol);
                                sb.Append("\t\t\t#region ObjPath Biomes" + eol);
                                LBBiome lbBiome = null;
                                sb.Append(eol);

                                // Re-use the same LBBiome
                                string pathBiomeInst = groupMbrInst + "Biome";
                                sb.Append("\t\t\tLBBiome " + pathBiomeInst + " = null;" + eol);

                                for (int obmIdx = 0; obmIdx < numBiomes; obmIdx++)
                                {
                                    lbBiome = lbGroupMember.lbObjPath.lbBiomeList[obmIdx];
                                    if (lbBiome != null)
                                    {
                                        sb.Append("\t\t\t#region Biome " + (obmIdx + 1).ToString() + eol);
                                        sb.Append("\t\t\t" + pathBiomeInst + " = new LBObjPathSeries();" + eol);
                                        sb.Append("\t\t\t" + pathBiomeInst + ".biomeIndex = " + lbBiome.biomeIndex + ";" + eol);
                                        sb.Append("\t\t\t" + pathBiomeInst + ".minBlendDist = " + lbBiome.minBlendDist + "f;" + eol);

                                        sb.Append("\t\t\t" + groupMbrInst + ".lbObjPath.lbBiomeList.Add(" + pathBiomeInst + ");" + eol);
                                        sb.Append("\t\t\t" + pathBiomeInst + " = null;" + eol);
                                        sb.Append("\t\t\t#endregion Biome" + eol);
                                    }
                                }

                                sb.Append("\t\t\t#endregion ObjPath Biomes" + eol);
                                sb.Append(eol);
                            }
                        }
                        #endregion

                        sb.Append("\t\t}" + eol);
                        sb.Append("\t\t#endregion End Object Path settings for [" + pathName + "]" + eol);
                    }
                    #endregion

                    sb.Append("\t}" + eol);

                    sb.Append("\t" + groupInst + ".groupMemberList.Add("+ groupMbrInst + ");" + eol);
                    sb.Append("\t#endregion" + eol);
                }
            }

            sb.Append("\t// End Group Members" + eol);
            sb.Append(eol);
            #endregion

            sb.Append("\t// NOTE Add the new Group to the landscape meta-data");
            sb.Append(eol);
            sb.Append("\tlandscape.lbGroupList.Add(" + groupInst + ");");
            sb.Append(eol);

            sb.Append("}" + eol);
            sb.Append("#endregion" + eol);
            sb.Append("// END OF CODE SEGMENT" + eol);

            return sb.ToString();
        }

        /// <summary>
        /// Returns the largest value of min blend dist. used in any of the LBGroupTextures in this group. 
        /// Always returns at least zero (no negative values)
        /// </summary>
        /// <returns></returns>
        public float GetMaxTextureMinBlendDist ()
        {
            float maxTextureMinBlendDist = 0f;

            if (textureList != null)
            {
                for (int tIdx = 0; tIdx < textureList.Count; tIdx++)
                {
                    if (textureList[tIdx].minBlendDist > maxTextureMinBlendDist) { maxTextureMinBlendDist = textureList[tIdx].minBlendDist; }
                }
            }

            return maxTextureMinBlendDist;
        }

        /// <summary>
        /// Returns the largest value of min blend dist. used in any of the LBGroupGrass in this group. 
        /// Always returns at least zero (no negative values)
        /// </summary>
        /// <returns></returns>
        public float GetMaxGrassMinBlendDist()
        {
            float maxGrassMinBlendDist = 0f;

            if (grassList != null)
            {
                for (int gIdx = 0; gIdx < grassList.Count; gIdx++)
                {
                    if (grassList[gIdx].minBlendDist > maxGrassMinBlendDist) { maxGrassMinBlendDist = grassList[gIdx].minBlendDist; }
                }
            }

            return maxGrassMinBlendDist;
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Remove disabled groups from the list. Create new list so we don't affect original in list.
        /// Also remove groups with no active members.
        /// </summary>
        /// <param name="lbGroupList"></param>
        /// <returns></returns>
        public static List<LBGroup> GetActiveGroupList(List<LBGroup> lbGroupList)
        {
            List<LBGroup> activeGroupList = new List<LBGroup>(lbGroupList);
            LBGroup lbGroup = null;

            for (int gpIdx = activeGroupList.Count - 1; gpIdx >= 0; gpIdx--)
            {
                lbGroup = activeGroupList[gpIdx];
                // Remove disabled groups or those with no group members
                if (lbGroup.isDisabled || lbGroup.groupMemberList == null)
                {
                    activeGroupList.Remove(lbGroup);
                }
                // Remove groups with no active members
                else if (lbGroup.groupMemberList.FindIndex(gm => !gm.isDisabled) < 0)
                {
                    activeGroupList.Remove(lbGroup);
                }
            }

            return activeGroupList;
        }

        /// <summary>
        /// Get the Group which has the supplied GUID. Optionally ignore Disabled members.
        /// </summary>
        /// <param name="lbGroupList"></param>
        /// <param name="groupGUID"></param>
        /// <param name="isDisabledIgnored"></param>
        /// <returns></returns>
        public static LBGroup GetGroupByGUID(List<LBGroup> lbGroupList, string groupGUID, bool isDisabledIgnored = false)
        {
            if (lbGroupList != null)
            {
                return lbGroupList.Find(gp => gp.GUID == groupGUID && (!isDisabledIgnored || !gp.isDisabled) && !string.IsNullOrEmpty(gp.GUID));
            }
            else { return null; }
        }

        /// <summary>
        /// Do any active groups need to remove grass?
        /// </summary>
        /// <param name="lbGroupList"></param>
        /// <returns></returns>
        public static bool IsRemoveGrassPresent(List<LBGroup> lbGroupList)
        {
            bool isRemoveGrass = false;

            List<LBGroup> activeGroupList = GetActiveGroupList(lbGroupList);

            int numGroups = (activeGroupList == null ? 0 : activeGroupList.Count);

            for (int gpIdx = 0; gpIdx < numGroups; gpIdx++)
            {
                if ((lbGroupList[gpIdx].isRemoveExistingGrass && lbGroupList[gpIdx].lbGroupType != LBGroupType.Uniform) || lbGroupList[gpIdx].groupMemberList.FindIndex(gm => gm.prefab != null && gm.minGrassProximity > 0f) >= 0)
                {
                    isRemoveGrass = true;
                    break;
                }
            }

            return isRemoveGrass;
        }

        /// <summary>
        /// Do any active groups need to populate grass?
        /// </summary>
        /// <param name="lbGroupList"></param>
        /// <returns></returns>
        public static bool IsPopulateGrassPresent(List<LBGroup> lbGroupList)
        {
            bool isPopulateGrass = false;

            List<LBGroup> activeGroupList = GetActiveGroupList(lbGroupList);

            int numGroups = (activeGroupList == null ? 0 : activeGroupList.Count);

            for (int gpIdx = 0; gpIdx < numGroups; gpIdx++)
            {
                LBGroup lbGroup = activeGroupList[gpIdx];

                if (lbGroup.grassList == null ? false : lbGroup.grassList.Exists(gx => !string.IsNullOrEmpty(gx.lbTerrainGrassGUID)))
                {
                    isPopulateGrass = true;
                    break;
                }
            }

            return isPopulateGrass;
        }

        /// <summary>
        /// Does this group need to have ground textures applied? NOTE: If this Group
        /// contains SubGroups, then call something like the following instead.
        /// IsApplyObjPathTexturesPresent(List<LBGroup> lbGroupList, LBGroup lbGroup, LBObjPath lbObjPath)
        /// </summary>
        /// <param name="lbGroup"></param>
        /// <returns></returns>
        public static bool IsApplyTexturesPresent(LBGroup lbGroup)
        {
            bool isApplyTextures = false;

            if (lbGroup != null)
            {
                // Check to see if that subgroup has any object paths that have ground texturing applied.
                if (lbGroup.groupMemberList.Exists(mbr => mbr.lbMemberType == LBGroupMember.LBMemberType.ObjPath && mbr.lbObjPath != null && (!string.IsNullOrEmpty(mbr.lbObjPath.coreTextureGUID) || !string.IsNullOrEmpty(mbr.lbObjPath.surroundTextureGUID))))
                {
                    isApplyTextures = true;
                }
                // Check to see if Texturing is being applied or removed at the group-level
                else if (lbGroup.textureList == null ? false : lbGroup.textureList.Exists(tx => !string.IsNullOrEmpty(tx.lbTerrainTextureGUID)))
                {
                    isApplyTextures = true;
                }
            }

            return isApplyTextures;
        }

        /// <summary>
        /// Do any active groups need to have textures applied to them?
        /// </summary>
        /// <param name="lbGroupList"></param>
        /// <returns></returns>
        public static bool IsApplyTexturesPresent(List<LBGroup> lbGroupList)
        {
            bool isApplyTextures = false;

            List<LBGroup> activeGroupList = GetActiveGroupList(lbGroupList);

            int numGroups = (activeGroupList == null ? 0 : activeGroupList.Count);

            for (int gpIdx = 0; gpIdx < numGroups; gpIdx++)
            {
                LBGroup lbGroup = activeGroupList[gpIdx];

                if (lbGroup.textureList == null ? false : lbGroup.textureList.Exists(tx => !string.IsNullOrEmpty(tx.lbTerrainTextureGUID)))
                {
                    isApplyTextures = true;
                    break;
                }
            }

            return isApplyTextures;
        }

        /// <summary>
        /// Check to see if any group members of any groups have an Object Path that
        /// applies a terrain texture under it.
        /// </summary>
        /// <param name="lbGroupList"></param>
        /// <returns></returns>
        public static bool IsApplyObjPathTexturesPresent(List<LBGroup> lbGroupList)
        {
            bool isApplyObjPathTextures = false;

            List<LBGroup> activeGroupList = GetActiveGroupList(lbGroupList);

            int numGroups = (activeGroupList == null ? 0 : activeGroupList.Count);

            for (int gpIdx = 0; gpIdx < numGroups; gpIdx++)
            {
                LBGroup lbGroup = activeGroupList[gpIdx];

                if (lbGroup.groupMemberList.Exists(mbr => mbr.lbMemberType == LBGroupMember.LBMemberType.ObjPath && mbr.lbObjPath != null && (!string.IsNullOrEmpty(mbr.lbObjPath.coreTextureGUID) || !string.IsNullOrEmpty(mbr.lbObjPath.surroundTextureGUID))))
                {
                    isApplyObjPathTextures = true;
                    break;
                }
            }

            return isApplyObjPathTextures;
        }

        /// <summary>
        /// Check to see if any ground textures are applied along the Object Path
        /// specified. They could also be in SubGroups attached to a wide-based
        /// ObjectPathSeries or a width-based series from another active group if the
        /// isSeriesListOverride is enabled. The GroupList is required for when
        /// isSeriesListOverride is used.
        /// </summary>
        /// <param name="lbGroupList"></param>
        /// <param name="lbGroup"></param>
        /// <param name="lbObjPath"></param>
        /// <returns></returns>
        public static bool IsApplyObjPathTexturesPresent(List<LBGroup> lbGroupList, LBGroup lbGroup, LBObjPath lbObjPath)
        {
            // First check the Surface on the current Object Path
            bool isApplyObjPathTextures = lbObjPath != null && lbObjPath.useWidth && (!string.IsNullOrEmpty(lbObjPath.coreTextureGUID) || !string.IsNullOrEmpty(lbObjPath.surroundTextureGUID));

            if (!isApplyObjPathTextures)
            {
                List<LBObjPathSeries> objPathSeriesList = null;
                if (lbObjPath.isSeriesListOverride)
                {
                    // Look up the series list in the override GroupMember Object Path
                    if (!string.IsNullOrEmpty(lbObjPath.seriesListGroupMemberGUID))
                    {
                        // Find the matching LBGroupMember (if any). It must be an Object Path in the same Group
                        int gmbrLookupIdx = lbGroup.groupMemberList.FindIndex(gmbr => gmbr.GUID == lbObjPath.seriesListGroupMemberGUID && !string.IsNullOrEmpty(gmbr.GUID) && gmbr.lbMemberType == LBGroupMember.LBMemberType.ObjPath);
                        if (gmbrLookupIdx >= 0)
                        {
                            LBGroupMember lbGroupMemberSeriesOverride = lbGroup.groupMemberList[gmbrLookupIdx];
                            if (lbGroupMemberSeriesOverride != null && lbGroupMemberSeriesOverride.lbObjPath != null)
                            {
                                objPathSeriesList = lbGroupMemberSeriesOverride.lbObjPath.lbObjPathSeriesList;
                            }
                        }
                    }
                }
                else
                {
                    objPathSeriesList = lbObjPath.lbObjPathSeriesList;
                }

                int numObjPathSeries = objPathSeriesList == null ? 0 : objPathSeriesList.Count;
                LBObjPathSeries lbObjPathSeries = null;
                LBGroup subGroup = null;
                LBObjSubGroup lbObjSubGroup = null;

                for (int sIdx = 0; sIdx < numObjPathSeries; sIdx++)
                {
                    lbObjPathSeries = objPathSeriesList[sIdx];
                    if (lbObjPathSeries != null && lbObjPathSeries.useSubGroups)
                    {
                        // Find the subgroup that is being placed along the Object path
                        subGroup = lbGroupList.Find(grp => grp.GUID == lbObjPathSeries.startObjSubGroup.subGroupGUID && !string.IsNullOrEmpty(lbObjPathSeries.startObjSubGroup.subGroupGUID));
                        if (subGroup != null)
                        {
                            if (IsApplyTexturesPresent(subGroup)) { isApplyObjPathTextures = true; break; }
                        }

                        subGroup = lbGroupList.Find(grp => grp.GUID == lbObjPathSeries.endObjSubGroup.subGroupGUID && !string.IsNullOrEmpty(lbObjPathSeries.endObjSubGroup.subGroupGUID));
                        if (subGroup != null)
                        {
                            if (IsApplyTexturesPresent(subGroup)) { isApplyObjPathTextures = true; break; }
                        }

                        // Loop through all the Main Object SubGroups
                        int numSubGroups = lbObjPathSeries.mainObjSubGroupList == null ? 0 : lbObjPathSeries.mainObjSubGroupList.Count;
                        for (int sgIdx = 0; sgIdx < numSubGroups; sgIdx++)
                        {
                            lbObjSubGroup = lbObjPathSeries.mainObjSubGroupList[sgIdx];
                            if (lbObjSubGroup != null)
                            {
                                // Find the subgroup that is being placed along the Object path
                                subGroup = lbGroupList.Find(grp => grp.GUID == lbObjSubGroup.subGroupGUID && !string.IsNullOrEmpty(lbObjSubGroup.subGroupGUID));
                                if (subGroup != null)
                                {
                                    if (IsApplyTexturesPresent(subGroup)) { isApplyObjPathTextures = true; break; }
                                }
                            }
                        }

                        if (isApplyObjPathTextures) { break; }
                    }
                }
            }

            return isApplyObjPathTextures;
        }

        /// <summary>
        /// Check to see if any groups or their members modify Unity Terrain trees
        /// </summary>
        /// <param name="lbGroupList"></param>
        /// <returns></returns>
        public static bool IsModifyTrees(List<LBGroup> lbGroupList)
        {
            bool isModifyTrees = false;

            List<LBGroup> activeGroupList = GetActiveGroupList(lbGroupList);

            int numGroups = (activeGroupList == null ? 0 : activeGroupList.Count);

            if (numGroups > 0)
            {
                // Check if any groups remove existing trees

                isModifyTrees = activeGroupList.Exists(grp => grp.isRemoveExistingTrees == true);
                if (!isModifyTrees)
                {
                    for (int gpIdx = 0; gpIdx < numGroups; gpIdx++)
                    {
                        LBGroup lbGroup = activeGroupList[gpIdx];

                        if (lbGroup.groupMemberList.Exists(mbr => mbr.isRemoveTree && mbr.minTreeProximity > 0f))
                        {
                            isModifyTrees = true;
                            break;
                        }
                    }
                }
            }

            return isModifyTrees;
        }

        /// <summary>
        /// Get the maximum (or greatest) ProximityExtent of all the active groups.
        /// Currently only applies to Clearings.
        /// </summary>
        /// <param name="lbGroupList"></param>
        /// <returns></returns>
        public static float GetMaxGroupProximityExtent(List<LBGroup> lbGroupList)
        {
            float maxProximityExtent = 0f;
            int numGroups = 0;
            LBGroup lbGroup = null;

            if (lbGroupList != null) { numGroups = lbGroupList.Count; }

            for (int gpIdx = 0; gpIdx < numGroups; gpIdx++)
            {
                lbGroup = lbGroupList[gpIdx];

                // Ignore disabled groups or those with no group members
                if (lbGroup != null && !lbGroup.isDisabled && lbGroup.proximityExtent > maxProximityExtent)
                {
                    maxProximityExtent = lbGroup.proximityExtent;
                }
            }

            return maxProximityExtent;
        }

        /// <summary>
        /// Get the maximum (or greatest) ProximityExtent of all the active group members
        /// within the active groups that contain a prefab.
        /// </summary>
        /// <param name="lbGroupList"></param>
        /// <returns></returns>
        public static float GetMaxMemberProximityExtent(List<LBGroup> lbGroupList)
        {
            float maxProximityExtent = 0f;
            int numGroups = 0, numGroupMembers = 0;
            LBGroup lbGroup = null;
            LBGroupMember lbGroupMember = null;

            if (lbGroupList != null) { numGroups = lbGroupList.Count; }

            for (int gpIdx = 0; gpIdx < numGroups; gpIdx++)
            {
                lbGroup = lbGroupList[gpIdx];

                // Igorne disabled groups or those with no group members
                if (lbGroup != null && !lbGroup.isDisabled && lbGroup.groupMemberList != null)
                {
                    numGroupMembers = lbGroup.groupMemberList.Count;
                    for (int gpmbrIdx = 0; gpmbrIdx < numGroupMembers; gpmbrIdx++)
                    {
                        lbGroupMember = lbGroup.groupMemberList[gpmbrIdx];
                        // Ignore disabled members without a prefab
                        if (lbGroupMember != null && !lbGroupMember.isDisabled && lbGroupMember.prefab != null && lbGroupMember.proximityExtent > maxProximityExtent)
                        {
                            maxProximityExtent = lbGroupMember.proximityExtent;
                        }
                    }
                }
            }

            return maxProximityExtent;
        }

        /// <summary>
        /// Get the maximum (or greatest) MinTreeProximity of all the active group members
        /// within the active groups that contain a prefab.
        /// </summary>
        /// <param name="lbGroupList"></param>
        /// <returns></returns>
        public static float GetMaxMemberTreeProximity(List<LBGroup> lbGroupList)
        {
            float maxTreeProximity = 0f;
            int numGroups = 0, numGroupMembers = 0;
            LBGroup lbGroup = null;
            LBGroupMember lbGroupMember = null;

            if (lbGroupList != null) { numGroups = lbGroupList.Count; }

            for (int gpIdx = 0; gpIdx < numGroups; gpIdx++)
            {
                lbGroup = lbGroupList[gpIdx];

                // Igorne disabled groups or those with no group members
                if (lbGroup != null && !lbGroup.isDisabled && lbGroup.groupMemberList != null)
                {
                    numGroupMembers = lbGroup.groupMemberList.Count;
                    for (int gpmbrIdx = 0; gpmbrIdx < numGroupMembers; gpmbrIdx++)
                    {
                        lbGroupMember = lbGroup.groupMemberList[gpmbrIdx];
                        // Ignore disabled members without a prefab
                        if (lbGroupMember != null && !lbGroupMember.isDisabled && lbGroupMember.prefab != null && lbGroupMember.minTreeProximity > maxTreeProximity)
                        {
                            maxTreeProximity = lbGroupMember.minTreeProximity;
                        }
                    }
                }
            }

            return maxTreeProximity;
        }

        /// <summary>
        /// Expand or collapse the details of each Group in the list provided
        /// </summary>
        /// <param name="lbGroupList"></param>
        /// <param name="isExpanded"></param>
        public static void GroupListExpand(List<LBGroup> lbGroupList, bool isExpanded)
        {
            if (lbGroupList != null)
            {
                for (int grpIdx = 0; grpIdx < lbGroupList.Count; grpIdx++)
                {
                    if (lbGroupList[grpIdx] != null) { lbGroupList[grpIdx].showInEditor = isExpanded; }
                }
            }
        }

        /// <summary>
        /// If the landscape dimensions or the start location has changed, some items may need to be modified
        /// in order to keep their relative positions in the landscape. This typically occurs when a LBTemplate
        /// is applied to a landscape that has a different size or location to the original landscape that
        /// generated the template.
        /// If no offset is required, set offset to Vector3.zero
        /// If no scaling is required, set scale to Vector3.one
        /// </summary>
        /// <param name="lbGroupList"></param>
        /// <param name="offset"></param>
        /// <param name="scale"></param>
        public static void LandscapeDimensionLocationChanged(List<LBGroup> lbGroupList, Vector3 offset, Vector3 scale)
        {
            if (lbGroupList != null)
            {
                LBGroup lbGroup = null;
                LBGroupMember lbGroupMember = null;

                //Debug.Log("LBGroup.LandscapeDimensionLocationChanged offset:" + offset + " scale:" + scale);

                int numGroups = lbGroupList.Count;
                for (int g = 0; g < numGroups; g++)
                {
                    lbGroup = lbGroupList[g];

                    lbGroup.MoveClearings(offset, scale);

                    // Only move and scale Uniform group object paths. Within a Clearing or SubGroup, paths don't need to be changed
                    if (lbGroup.lbGroupType == LBGroupType.Uniform)
                    {
                        int numGroupMembers = lbGroup.groupMemberList == null ? 0 : lbGroup.groupMemberList.Count;

                        for (int m = 0; m < numGroupMembers; m++)
                        {
                            lbGroupMember = lbGroup.groupMemberList[m];
                            if (lbGroupMember.lbMemberType == LBGroupMember.LBMemberType.ObjPath)
                            {
                                if (lbGroupMember.lbObjPath != null && lbGroupMember.lbObjPath.positionList != null)
                                {
                                    lbGroupMember.lbObjPath.MovePoints(offset, scale);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Resnap all Object Path Uniform Group Members in a list of groups to the correct terrain heights.
        /// Does not apply to paths with snapToTerrain = false which is the default when useWidth = true.
        /// Typically called after the terrain heightmap has changed.
        /// </summary>
        /// <param name="lbGroupList"></param>
        public static void ObjPathSnapToTerrain(LBLandscape landscape, List<LBGroup> lbGroupList)
        {
            LBGroup lbGroup = null;
            LBGroupMember lbGroupMember = null;

            if (landscape != null)
            {
                int numGroups = lbGroupList == null ? 0 : lbGroupList.Count;

                for (int g = 0; g < numGroups; g++)
                {
                    lbGroup = lbGroupList[g];

                    if (lbGroup != null && lbGroup.lbGroupType == LBGroupType.Uniform)
                    {
                        int numGroupMembers = lbGroup.groupMemberList == null ? 0 : lbGroup.groupMemberList.Count;

                        for (int m = 0; m < numGroupMembers; m++)
                        {
                            lbGroupMember = lbGroup.groupMemberList[m];
                            if (lbGroupMember.lbMemberType == LBGroupMember.LBMemberType.ObjPath)
                            {
                                if (lbGroupMember.lbObjPath != null && lbGroupMember.lbObjPath.positionList != null && lbGroupMember.lbObjPath.snapToTerrain)
                                {
                                    lbGroupMember.lbObjPath.RefreshPathHeights(landscape);

                                    // Force refresh of spline cache
                                    lbGroupMember.lbObjPath.isSplinesCached2 = false;

                                    // Update left/right positions
                                    lbGroupMember.lbObjPath.RefreshObjPathPositions(lbGroupMember.lbObjPath.showSurroundingInScene, false);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Enabled or disable group members. Optionally apply a filter.
        /// </summary>
        /// <param name="isEnabled"></param>
        public static void EnableGroupMembers(List<LBGroup> lbGroupList, int grpIdx, bool isEnabled, string filter = "")
        {
            int numGroups = (lbGroupList == null ? 0 : lbGroupList.Count);
            
            if (numGroups > grpIdx)
            {
                List<LBGroupMember> groupMemberList = lbGroupList[grpIdx].groupMemberList;

                int numGroupMembers = (groupMemberList == null ? 0 : groupMemberList.Count);
                LBGroupMember lbGroupMember = null;

                for (int grpMbrIdx = 0; grpMbrIdx < numGroupMembers; grpMbrIdx++)
                {
                    lbGroupMember = groupMemberList[grpMbrIdx];

                    if (lbGroupMember != null) { if (lbGroupMember.IsInSearchFilter(filter)) { lbGroupMember.isDisabled = !isEnabled; } }
                }
            }
        }

        /// <summary>
        /// Move a group member to the top of the members list within a group
        /// </summary>
        /// <param name="lbGroupList"></param>
        /// <param name="grpIdx"></param>
        /// <param name="grpMbrIdx"></param>
        public static void MoveGroupMemberToTop(List<LBGroup> lbGroupList, int grpIdx, int grpMbrIdx)
        {
            int numGroups = (lbGroupList == null ? 0 : lbGroupList.Count);

            if (numGroups > grpIdx)
            {
                List<LBGroupMember> groupMemberList = lbGroupList[grpIdx].groupMemberList;

                int numGroupMembers = (groupMemberList == null ? 0 : groupMemberList.Count);             

                // Check if member to move is not already at top of list
                if (numGroupMembers > grpMbrIdx && grpMbrIdx > 0)
                {
                    LBGroupMember groupMemberMoveToTop = groupMemberList[grpMbrIdx];

                    groupMemberList.RemoveAt(grpMbrIdx);
                    groupMemberList.Insert(0, groupMemberMoveToTop);
                }
            }
        }

        /// <summary>
        /// Move a group member to the end of the members list within a group
        /// </summary>
        /// <param name="lbGroupList"></param>
        /// <param name="grpIdx"></param>
        /// <param name="grpMbrIdx"></param>
        public static void MoveGroupMemberToEnd(List<LBGroup> lbGroupList, int grpIdx, int grpMbrIdx)
        {
            int numGroups = (lbGroupList == null ? 0 : lbGroupList.Count);

            if (numGroups > grpIdx)
            {
                List<LBGroupMember> groupMemberList = lbGroupList[grpIdx].groupMemberList;

                int numGroupMembers = (groupMemberList == null ? 0 : groupMemberList.Count);

                // Check if member to move is not already at end of list
                if (grpMbrIdx < numGroupMembers - 1)
                {
                    LBGroupMember groupMemberMoveToEnd = groupMemberList[grpMbrIdx];

                    groupMemberList.RemoveAt(grpMbrIdx);
                    groupMemberList.Add(groupMemberMoveToEnd);
                }
            }
        }

        /// <summary>
        /// Get an Object Path group member, given the GUID of the group member
        /// </summary>
        /// <param name="lbGroupList"></param>
        /// <param name="groupMemberGUID"></param>
        public static LBObjPath GetObjectPath(List<LBGroup> lbGroupList, string groupMemberGUID)
        {
            LBObjPath lbObjPath = null;

            // Find the group which contains the object path
            LBGroup objPathGroup = lbGroupList.Find(grp => grp.groupMemberList.Exists(m => m.GUID == groupMemberGUID));

            if (objPathGroup != null)
            {
                // Find the group member which is the object path
                LBGroupMember objPathGroupMember = objPathGroup.groupMemberList.Find(m => m.GUID == groupMemberGUID);

                // Verify it is an object path rather than a prefab member
                if (objPathGroupMember != null && objPathGroupMember.lbMemberType == LBGroupMember.LBMemberType.ObjPath)
                {
                    lbObjPath = objPathGroupMember.lbObjPath;
                }
            }

            return lbObjPath;
        }

        #endregion
    }
}