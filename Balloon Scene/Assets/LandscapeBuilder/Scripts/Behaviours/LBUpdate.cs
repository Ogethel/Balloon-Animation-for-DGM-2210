using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace LandscapeBuilder
{
    public class LBUpdate : MonoBehaviour
    {
        #region Update Landscape

        /// <summary>
        /// Update the landscape to the current version
        /// v2.0.2 Upgrades the Texture, Grass, and Tree list to include any missing GUIDs.
        /// v2.1.5 Updates Groups to include any missing GUIDs
        /// </summary>
        /// <param name="OldVersion"></param>
        /// <param name="NewVersion"></param>
        /// <param name="landscapeToUpdate"></param>
        /// <param name="isSilentUpdate"></param>
        /// <returns></returns>
        public static bool LandscapeUpdate(string OldVersion, string NewVersion, ref LBLandscape landscapeToUpdate, bool isSilentUpdate = false)
        {
            bool isSuccessful = false;

            if (landscapeToUpdate == null)
            {
                #if UNITY_EDITOR
                if (!isSilentUpdate) { Debug.LogError("LBUpdate LandscapeUpdate - no landscape to update"); }
                #endif
            }
            else
            {
                //Debug.Log("LBUpdate LandscapeUpdate - upgrading " + landscapeToUpdate.gameObject.name + " from version " + OldVersion + " to " + NewVersion);
                landscapeToUpdate.LastUpdatedVersion = NewVersion;

                // Before a Landscape is imported, it doesn't have a grass list, so check first.
                if (landscapeToUpdate.terrainGrassList != null)
                {
                    // Update any Grass types that don't have a GUID
                    List<LBTerrainGrass> updateableGrassList = landscapeToUpdate.terrainGrassList.FindAll(grs => string.IsNullOrEmpty(grs.GUID));

                    int numGrassToUpdate = (updateableGrassList == null ? 0 : updateableGrassList.Count);

                    for (int gIdx = 0; gIdx < numGrassToUpdate; gIdx++)
                    {
                        updateableGrassList[gIdx].GUID = System.Guid.NewGuid().ToString();
                    }                    
                }

                // Before a Landscape is imported, it doesn't have a texture list, so check first.
                if (landscapeToUpdate.terrainTexturesList != null)
                {
                    // Update any Texture types that don't have a GUID
                    List<LBTerrainTexture> updateableTextureList = landscapeToUpdate.terrainTexturesList.FindAll(tx => string.IsNullOrEmpty(tx.GUID));

                    int numTexturesToUpdate = (updateableTextureList == null ? 0 : updateableTextureList.Count);

                    for (int txIdx = 0; txIdx < numTexturesToUpdate; txIdx++)
                    {
                        updateableTextureList[txIdx].GUID = System.Guid.NewGuid().ToString();
                    }
                }

                // Before a Landscape is imported, it doesn't have a tree list, so check first.
                if (landscapeToUpdate.terrainTreesList != null)
                {
                    // Update any Tree types that don't have a GUID
                    List<LBTerrainTree> updateableTreeList = landscapeToUpdate.terrainTreesList.FindAll(tr => string.IsNullOrEmpty(tr.GUID));

                    int numTreesToUpdate = (updateableTreeList == null ? 0 : updateableTreeList.Count);

                    for (int txIdx = 0; txIdx < numTreesToUpdate; txIdx++)
                    {
                        updateableTreeList[txIdx].GUID = System.Guid.NewGuid().ToString();
                    }
                }

                // To use SubGroups, Groups need to have a unique identifier
                if (landscapeToUpdate.lbGroupList != null)
                {
                    // Update any Groups that don't have a GUID
                    List<LBGroup> updateableGroupList = landscapeToUpdate.lbGroupList.FindAll(grp => string.IsNullOrEmpty(grp.GUID));

                    int numGroupsToUpdate = updateableGroupList == null ? 0 : updateableGroupList.Count;

                    for (int gpIdx = 0; gpIdx < numGroupsToUpdate; gpIdx++)
                    {
                        updateableGroupList[gpIdx].GUID = System.Guid.NewGuid().ToString();
                    }
                }

                #if UNITY_EDITOR
                if (!isSilentUpdate) { Debug.Log("LBUpdate LandscapeUpdate - upgraded " + landscapeToUpdate.gameObject.name + " from version " + OldVersion + " to " + NewVersion); }
                #endif

                isSuccessful = true;
            }

            return isSuccessful;
        }
        #endregion

        #region Upgrade Mesh/Prefabs to Groups where possible

        /// <summary>
        /// Attempt to convert Mesh/Prefabs to Uniform Groups
        /// </summary>
        /// <param name="landscape"></param>
        public static void ConvertMeshesToGroups(LBLandscape landscape)
        {
            string methodName = "LBUpdate.ConvertMeshesToGroups";
            if (landscape == null) { Debug.LogWarning("ERROR: " + methodName + " - landscape is null. Please Report."); }
            else if (landscape.landscapeMeshList != null && landscape.lbGroupList != null)
            {
                List<LBGroup> convertedGroupList = new List<LBGroup>();

                int numMeshes = landscape.landscapeMeshList.Count;

                for (int mIdx = 0; mIdx < numMeshes; mIdx++)
                {
                    LBLandscapeMesh lMesh = landscape.landscapeMeshList[mIdx];

                    if (lMesh.isDisabled) { Debug.Log("INFO: " + methodName + " skipping disabled Mesh " + (mIdx + 1)); }
                    else if (lMesh.meshPlacingMode == LBLandscapeMesh.MeshPlacingMode.Map || lMesh.meshPlacingMode == LBLandscapeMesh.MeshPlacingMode.HeightInclinationMap)
                    {
                        Debug.Log("INFO: " + methodName + " cannot convert Mesh " + (mIdx + 1) + " as it contains a Map texture. Recommendation: use a Stencil Layer instead");
                    }
                    else if (!lMesh.usePrefab)
                    {
                        Debug.Log("INFO: " + methodName + " cannot convert Mesh " + (mIdx + 1) + " as Groups only support prefabs.");
                    }
                    else if (lMesh.isClustered)
                    {
                        Debug.Log("INFO: " + methodName + " cannot convert Mesh " + (mIdx + 1) + " as source has Clusters enabled. Recommendation: Create a Procedural Clearing to replace this Mesh/Prefab.");
                    }
                    else
                    {
                        LBGroup lbGroup = null;
                        LBGroupMember lbGroupMember = null;

                        // If this is the first group always create a new one
                        if (convertedGroupList.Count == 0)
                        {
                            lbGroup = new LBGroup();
                            if (lbGroup != null)
                            {
                                lbGroup.showInEditor = false;
                                lbGroup.groupName = "converted group " + (convertedGroupList.Count + 1).ToString("000");
                                lbGroup.lbGroupType = LBGroup.LBGroupType.Uniform;
                                lbGroup.filterList = GetSupportedMeshFilters(lMesh.filterList);
                                convertedGroupList.Add(lbGroup);
                            }
                        }
                        else
                        {
                            // Find a compatible group, else create a new one
                            List<LBFilter> supportedMeshFilterList = GetSupportedMeshFilters(lMesh.filterList);
                            int numThisMeshFilters = (supportedMeshFilterList == null ? 0 : supportedMeshFilterList.Count);

                            // Find a group with the same filters
                            for (int grpIdx = 0; grpIdx < convertedGroupList.Count; grpIdx++)
                            {
                                LBGroup searchGroup = convertedGroupList[grpIdx];

                                List<LBFilter> groupFilterList = searchGroup.filterList;
                                int numThisGroupFilters = (groupFilterList == null ? 0 : groupFilterList.Count);

                                if (numThisMeshFilters == 0 && numThisGroupFilters == 0) { lbGroup = searchGroup; break; }
                                else if (numThisMeshFilters == numThisGroupFilters)
                                {
                                    // Assume they match
                                    bool isMatch = true;
                                    for (int fIdx = 0; fIdx < numThisGroupFilters; fIdx++)
                                    {
                                        if (groupFilterList[fIdx].filterType != supportedMeshFilterList[fIdx].filterType) { isMatch = false; break; }
                                        else if (groupFilterList[fIdx].lbStencilGUID != supportedMeshFilterList[fIdx].lbStencilGUID) { isMatch = false; break; }
                                        else if (groupFilterList[fIdx].lbStencilLayerGUID != supportedMeshFilterList[fIdx].lbStencilLayerGUID) { isMatch = false; break; }
                                    }
                                    if (isMatch) { lbGroup = searchGroup; break; }
                                }
                            }

                            // If no suitable group was found, add a new one
                            if (lbGroup == null)
                            {
                                lbGroup = new LBGroup();
                                lbGroup.showInEditor = false;
                                lbGroup.groupName = "converted group " + (convertedGroupList.Count + 1).ToString("000");
                                lbGroup.lbGroupType = LBGroup.LBGroupType.Uniform;
                                lbGroup.filterList = GetSupportedMeshFilters(lMesh.filterList);
                                convertedGroupList.Add(lbGroup);
                            }
                        }

                        if (lbGroup == null) { Debug.Log("INFO: " + methodName + " could not create a new LBGroup. Please Report"); }
                        else
                        {
                            lbGroupMember = new LBGroupMember();
                            if (lbGroupMember == null) { Debug.Log("INFO: " + methodName + " could not create a new LBGroupMember. Please Report"); }
                            else
                            {
                                lbGroupMember.showInEditor = false;

                                CopyMeshToGroupMember(lMesh, lbGroup, lbGroupMember);

                                lbGroup.groupMemberList.Add(lbGroupMember);

                                // Disable migrated mesh/prefab items
                                lMesh.isDisabled = true;
                            }
                        }
                    }
                }

                if (convertedGroupList.Count > 0) { landscape.lbGroupList.AddRange(convertedGroupList); }
            }
        }

        private static List<LBFilter> GetSupportedMeshFilters(List<LBFilter> filterList)
        {
            List<LBFilter> supportedLBFilterList = new List<LBFilter>();

            if (filterList != null)
            {
                supportedLBFilterList.AddRange(filterList.FindAll(f => f.filterType == LBFilter.FilterType.StencilLayer));
            }

            return supportedLBFilterList;
        }

        private static void CopyMeshToGroupMember(LBLandscapeMesh lMesh, LBGroup lbGroup, LBGroupMember lbGroupMember)
        {
            if (lMesh != null && lbGroup != null && lbGroupMember != null)
            {
                if (lMesh.meshPlacingMode != LBLandscapeMesh.MeshPlacingMode.ConstantInfluence || lMesh.minScale != 1f || lMesh.maxScale != 1f)
                {
                    // This may not the first member of the group so need to check if we need to override group defaults

                    // The first member of a group sets the group-level default settings
                    if (lbGroup.groupMemberList.Count < 1)
                    {
                        lbGroupMember.isGroupOverride = false;
                        lbGroup.minScale = lMesh.minScale;
                        lbGroup.maxScale = lMesh.maxScale;
                        lbGroup.minHeight = lMesh.minHeight;
                        lbGroup.maxHeight = lMesh.maxHeight;
                        lbGroup.minInclination = lMesh.minInclination;
                        lbGroup.maxInclination = lMesh.maxInclination;
                    }
                    // Does this member have the same rules as the group it will be placed into?
                    else if (lMesh.minScale != lbGroup.minScale || lMesh.maxScale != lbGroup.maxScale ||
                             lMesh.minHeight != lbGroup.minHeight || lMesh.maxHeight != lbGroup.maxHeight ||
                             lMesh.minInclination != lbGroup.minInclination || lMesh.maxInclination != lbGroup.maxInclination)
                    {
                        lbGroupMember.isGroupOverride = true;

                        lbGroupMember.minScale = lMesh.minScale;
                        lbGroupMember.maxScale = lMesh.maxScale;
                        lbGroupMember.minHeight = lMesh.minHeight;
                        lbGroupMember.maxHeight = lMesh.maxHeight;
                        lbGroupMember.minInclination = lMesh.minInclination;
                        lbGroupMember.maxInclination = lMesh.maxInclination;
                    }
                    else
                    {
                        // Member rules are the same as the Group, so no need to override group-level defaults
                        lbGroupMember.isGroupOverride = false;
                    }
                }

                lbGroupMember.prefab = lMesh.prefab;
                lbGroupMember.prefabName = lMesh.prefabName;

                lbGroupMember.modelOffsetX = lMesh.offset.x;
                lbGroupMember.modelOffsetY = 0f;
                lbGroupMember.modelOffsetZ = lMesh.offset.z;

                lbGroupMember.randomiseOffsetY = false;
                lbGroupMember.minOffsetY = lMesh.offset.y;
                lbGroupMember.maxOffsetY = lbGroupMember.minOffsetY;

                lbGroupMember.randomiseRotationY = lMesh.randomiseYRotation;
                if (lMesh.randomiseYRotation)
                {
                    lbGroupMember.startRotationY = 0f;
                    lbGroupMember.endRotationY = 359.9f;
                }
                else
                {
                    lbGroupMember.startRotationY = lMesh.fixedYRotation;
                    lbGroupMember.endRotationY = lMesh.fixedYRotation;
                }
                lbGroupMember.randomiseRotationXZ = false;
                lbGroupMember.rotationX = lMesh.XRotation;
                lbGroupMember.endRotationX = lMesh.XRotation;
                lbGroupMember.rotationZ = lMesh.ZRotation;
                lbGroupMember.endRotationZ = lMesh.ZRotation;

                lbGroupMember.isCombineMesh = lMesh.isCombineMesh;
                lbGroupMember.isKeepPrefabConnection = lMesh.isKeepPrefabConnection;
                lbGroupMember.isCreateCollider = lMesh.isCreateCollider;
                lbGroupMember.isRemoveEmptyGameObjects = lMesh.isRemoveEmptyGameObjects;

                lbGroupMember.useNoise = lMesh.useNoise;
                lbGroupMember.noiseTileSize = lMesh.noiseTileSize;
                lbGroupMember.noisePlacementCutoff = lMesh.meshPlacementCutoff;

                lbGroupMember.maxPrefabSqrKm = lMesh.maxMeshes;
                lbGroupMember.maxPrefabPerGroup = 10000;

                // Slightly incompatible as old mesh is centre to centre distance
                // mesh.minProximity can be 0.0, while lbGroupMember.proximityExtent currently must be > 0.01.
                lbGroupMember.proximityExtent = lMesh.minProximity;

                lbGroupMember.isTerrainAligned = lMesh.isTerrainAligned;

                lbGroupMember.isTerrainFlattened = lMesh.isTerrainFlattened;
                lbGroupMember.flattenBlendRate = lMesh.flattenBlendRate;
                lbGroupMember.flattenDistance = lMesh.flattenDistance;
                lbGroupMember.flattenHeightOffset = lMesh.flattenHeightOffset;

                lbGroupMember.minGrassProximity = lMesh.minGrassProximity;
                lbGroupMember.isRemoveTree = true;
                lbGroupMember.minTreeProximity = lMesh.minTreeProximity;
            }
        }

        #endregion
    }
}