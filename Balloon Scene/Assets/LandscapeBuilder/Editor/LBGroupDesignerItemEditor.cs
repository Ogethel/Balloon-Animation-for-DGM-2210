// Landscape Builder. Copyright (c) 2016-2019 SCSM Pty Ltd. All rights reserved.
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditor.SceneManagement;

namespace LandscapeBuilder
{
    [CustomEditor(typeof(LBGroupDesignerItem))]
    public class LBGroupDesignerItemEditor : Editor
    {
        #region Private Variables
        private LBGroupDesignerItem lbGroupDesignerItem;
        private LBGroupMember lbGroupMember;
        private LBGroup lbGroup;
        private Vector3 prevPosition;

        #endregion

        #region Event Methods
        private void OnEnable()
        {
            lbGroupDesignerItem = (LBGroupDesignerItem)target;
            if (lbGroupDesignerItem != null)
            {
                lbGroupDesignerItem.position = lbGroupDesignerItem.transform.position;
                lbGroupDesignerItem.rotation = lbGroupDesignerItem.transform.rotation;
                lbGroupDesignerItem.scale = lbGroupDesignerItem.transform.localScale;

                // Get a reference to the group from the designer.
                if (lbGroupDesignerItem.lbGroupDesigner != null) { lbGroup = lbGroupDesignerItem.lbGroupDesigner.lbGroup; }

                prevPosition = lbGroupDesignerItem.position;
            }
        }

        private void OnSceneGUI()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { return; }
            // Don't process scene requests for Group Designer items when the ObjPathDesigner is enabled
            else if (lbGroupDesignerItem.lbGroupDesigner != null && lbGroupDesignerItem.lbGroupDesigner.isObjDesignerEnabled)
            {
                lbGroupDesignerItem.transform.position = prevPosition;
                lbGroupDesignerItem.transform.rotation = lbGroupDesignerItem.rotation;
                lbGroupDesignerItem.transform.localScale = lbGroupDesignerItem.scale;
                Selection.activeGameObject = null;
                return;
            }

            Event current = Event.current;

            //Debug.Log("DesignerItemEditor " + lbGroupDesignerItem.name);

            if (current != null)
            {
                // Get the group member. If we can't, and this isn't a SubGroup item, get out.
                if (lbGroupMember == null) { lbGroupMember = lbGroupDesignerItem.lbGroupMember; if (lbGroupMember == null && !lbGroupDesignerItem.isSubGroup) { return; } }

                bool isInSubGroup = !string.IsNullOrEmpty(lbGroupDesignerItem.SubGroupGUID);

                if (lbGroupDesignerItem.isObjPathMember || isInSubGroup)
                {
                    // If user attempts to move/rotate or scale an ObjPath member, SubGroup member, or a SubGroup, reset it
                    lbGroupDesignerItem.transform.position = prevPosition;
                    lbGroupDesignerItem.transform.rotation = lbGroupDesignerItem.rotation;
                    lbGroupDesignerItem.transform.localScale = lbGroupDesignerItem.scale;
                }
                else
                {
                    #region Check if Scale is overridden
                    // If Override is off, scaling is at the group level, not the member level, so don't allow scaling.
                    if (!lbGroupMember.isGroupOverride && Tools.current == Tool.Scale) { Tools.current = Tool.None; }
                    #if UNITY_2017_3_OR_NEWER
                    else if (Tools.current == Tool.Scale || Tools.current == Tool.Transform)
                    #else
                    else if (Tools.current == Tool.Scale)
                    #endif
                    {
                        // If using Transform tool in U2017.3+, may need to reset scaling back to pre-scaled value.
                        if (!lbGroupMember.isGroupOverride) { lbGroupDesignerItem.transform.localScale = lbGroupDesignerItem.scale; }
                        else
                        {
                            // Equally scale all axis
                            float maxScale = 0f;
                            Vector3 localScale = lbGroupDesignerItem.transform.localScale;

                            // Get the max scale amount of any of the axis
                            if (Mathf.Abs(localScale.x) > maxScale) { maxScale = localScale.x; }
                            if (Mathf.Abs(localScale.y) > maxScale) { maxScale = localScale.y; }
                            if (Mathf.Abs(localScale.z) > maxScale) { maxScale = localScale.z; }

                            // Make each axis the same
                            localScale = maxScale * Vector3.one;

                            // Clamp scaling to between 0.1 and 10
                            if (localScale.x < 0.1f) { localScale = Vector3.one * 0.1f; }
                            if (localScale.x > 10f) { localScale = Vector3.one * 10f; }
                            lbGroupDesignerItem.transform.localScale = localScale;
                        }
                    }
                    #endregion

                    #region Lock Y rotation if randomise is enabled
                    #if UNITY_2017_3_OR_NEWER
                    if ((Tools.current == Tool.Rotate || Tools.current == Tool.Transform) && lbGroupMember.randomiseRotationY)
                    #else
                    if (Tools.current == Tool.Rotate && lbGroupMember.randomiseRotationY)
                    #endif
                    {
                        // Pivotmode of center can cause issues with some prefabs that aren't centred correctly.
                        // Prevent x,z movement of prefab when only y rotation change is attempted
                        if (Tools.pivotMode == PivotMode.Center) { Tools.pivotMode = PivotMode.Pivot; }

                        lbGroupDesignerItem.transform.eulerAngles = new Vector3(lbGroupDesignerItem.transform.eulerAngles.x, lbGroupDesignerItem.rotation.eulerAngles.y, lbGroupDesignerItem.transform.eulerAngles.z);
                    }
                    #endregion

                    #region Clamp MinOffsetX,Z
                    #if UNITY_2017_3_OR_NEWER
                    if (Tools.current == Tool.Move || Tools.current == Tool.Transform)
                    #else
                    if (Tools.current == Tool.Move)
                    #endif
                    {
                        if (lbGroupMember.isPlacedInCentre)
                        {
                            if (lbGroup != null && lbGroupDesignerItem.lbGroupDesigner != null)
                            {
                                // If the user attempts to move it outside the clearing radius, lock the position to the last known position
                                Vector3 newPos = lbGroupDesignerItem.transform.position;
                                float distanceToCentre = Vector2.Distance(lbGroupDesignerItem.lbGroupDesigner.grpBasePlaneCentre2D, new Vector2(newPos.x, newPos.z));
                                if (distanceToCentre > lbGroup.maxClearingRadius) { lbGroupDesignerItem.transform.position = prevPosition; }
                            }
                        }
                        else if (!lbGroupMember.randomiseOffsetY)
                        {
                            // Don't allow movement on x,z axis for items that aren't offset from the Centre of the clearing AND don't use randomiseOffsetY.
                            lbGroupDesignerItem.transform.position = new Vector3(lbGroupDesignerItem.position.x, lbGroupDesignerItem.transform.position.y, lbGroupDesignerItem.position.z);
                        }
                        else
                        {
                            // Don't allow movement on any axis for items that aren't offset from the Centre of the clearing.
                            lbGroupDesignerItem.transform.position = lbGroupDesignerItem.position;
                        }

                        // Update last known position
                        prevPosition = lbGroupDesignerItem.transform.position;
                    }
                    #endregion
                }
                bool isLeftButton = (current.button == 0);
                bool isRightButton = (current.button == 1);

                // ISSUE (ignore if vertex snapping is not enabled [v key held down]) current.keyCode != KeyCode.V

                // Record the starting positions 
                if (!lbGroupDesignerItem.isObjPathMember && current.type == EventType.MouseDown && isLeftButton)
                {
                    Tools.hidden = false;
                    //Debug.Log("Left Btn Down");
                    lbGroupDesignerItem.position = lbGroupDesignerItem.transform.position;
                    lbGroupDesignerItem.rotation = lbGroupDesignerItem.transform.rotation;
                    lbGroupDesignerItem.scale = lbGroupDesignerItem.transform.localScale;
                }
                else if (!lbGroupDesignerItem.isObjPathMember && !isInSubGroup && current.type == EventType.MouseUp && lbGroupMember != null && isLeftButton)
                {
                    #region Move
#if UNITY_2017_3_OR_NEWER
                    if (Tools.current == Tool.Move || Tools.current == Tool.Transform)
#else
                    if (Tools.current == Tool.Move)
#endif
                    {
                        if (lbGroupMember.isPlacedInCentre)
                        {
                            lbGroupMember.minOffsetX = lbGroupDesignerItem.transform.position.x;
                            lbGroupMember.minOffsetZ = lbGroupDesignerItem.transform.position.z;
                            if (!lbGroupMember.randomiseOffsetY) { lbGroupMember.minOffsetY = lbGroupDesignerItem.transform.position.y - lbGroupDesignerItem.lbGroupDesigner.BasePlaneOffsetY; }
                        }
                        else if (!lbGroupMember.randomiseOffsetY)
                        {
                            
                            lbGroupMember.minOffsetY = lbGroupDesignerItem.transform.position.y - lbGroupDesignerItem.lbGroupDesigner.BasePlaneOffsetY;
                            lbGroupMember.maxOffsetY = lbGroupMember.minOffsetY;

                            // Update all the instances of this member in the Designer
                            if (lbGroupDesignerItem.lbGroupDesigner != null) { lbGroupDesignerItem.lbGroupDesigner.UpdateGroupMember(lbGroupMember); }
                        }
                    }
                    #endregion

                    #region Rotate
#if UNITY_2017_3_OR_NEWER
                    if (Tools.current == Tool.Rotate || Tools.current == Tool.Transform)
#else
                    if (Tools.current == Tool.Rotate)
#endif
                    {
                        Vector3 newRotation = lbGroupDesignerItem.transform.rotation.eulerAngles;

                        lbGroupMember.rotationX = newRotation.x;
                        lbGroupMember.rotationZ = newRotation.z;

                        if (!lbGroupMember.randomiseRotationY)
                        {
                            lbGroupMember.startRotationY = newRotation.y;
                            lbGroupMember.endRotationY = newRotation.y;

                            if (!lbGroupMember.isPlacedInCentre && lbGroupDesignerItem.lbGroupDesigner != null)
                            {
                                // Update all the instances of this member in the Designer
                                lbGroupDesignerItem.lbGroupDesigner.UpdateGroupMember(lbGroupMember);
                            }
                        }
                    }
                    #endregion

                    #region Scale
#if UNITY_2017_3_OR_NEWER
                    if ((Tools.current == Tool.Scale || Tools.current == Tool.Transform) && lbGroupMember.isGroupOverride)
#else
                    if (Tools.current == Tool.Scale && lbGroupMember.isGroupOverride)
#endif
                    {
                        //Debug.Log("Scale start:" + lbGroupDesignerItem.scale + " mouseup:" +  lbGroupDesignerItem.transform.localScale);
                        lbGroupMember.minScale = lbGroupDesignerItem.transform.localScale.x;
                        lbGroupMember.maxScale = lbGroupMember.minScale;

                        if (!lbGroupMember.isPlacedInCentre && lbGroupDesignerItem.lbGroupDesigner != null)
                        {
                            // Update all the instances of this member in the Designer
                            lbGroupDesignerItem.lbGroupDesigner.UpdateGroupMember(lbGroupMember);
                        }
                    }
                    #endregion

                    // Update the LB Editor Windows
                    LBEditorHelper.RepaintEditorWindow(typeof(LandscapeBuilderWindow));

                    //Debug.Log("Prefab: " + this.name + " start pos:" + lbGroupDesignerItem.position + " end pos:" + lbGroupDesignerItem.transform.position);
                }

                //if (current.keyCode == KeyCode.V && current.type == EventType.KeyUp)
                //if (current.keyCode != KeyCode.V)
                //{
                //    //LBIntegration.ReflectionOutputFields(typeof(Tools), true, true);
                //    bool isVertexDragging = false;
                //    try
                //    {
                //        isVertexDragging = LBIntegration.ReflectionGetValue<bool>(typeof(Tools), "vertexDragging", null, true, true);
                //    }
                //    catch (System.Exception ex)
                //    {
                //        Debug.LogWarning("LBGroupDesignerItemEditor could not find VertexDragging - PLEASE REPORT " + ex.Message);
                //    }

                //    Debug.Log("Vertex Snapping enabled..." + Time.realtimeSinceStartup + " vertexDragging: " + isVertexDragging);
                //}


                #region Display the Context-sensitive menu
                else if (current.type == EventType.MouseDown && isRightButton)
                {
                    bool isCheckProximity = (lbGroupDesignerItem.lbGroupDesigner == null ? true : lbGroupDesignerItem.lbGroupDesigner.isCheckProximity);

                    GenericMenu menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Close Designer"), false, CloseGroupDesigner);
                    menu.AddItem(new GUIContent("Refresh Designer"), false, () => RefreshDesigner(true));
                    menu.AddItem(new GUIContent("Check Proximity"), isCheckProximity, CheckProximity, !isCheckProximity);
                    menu.AddItem(new GUIContent("Auto Refresh"), lbGroupDesignerItem.lbGroupDesigner.GetAutoRefresh, () => { lbGroupDesignerItem.lbGroupDesigner.SetAutoRefresh(!lbGroupDesignerItem.lbGroupDesigner.GetAutoRefresh); });
                    // The following context menu items only apply to GroupMembers.
                    // Also exclude members in a subgroup within the Clearing Group
                    if (!lbGroupDesignerItem.isSubGroup && !isInSubGroup)
                    {
                        menu.AddSeparator("");
                        if (lbGroupDesignerItem.lbGroupDesigner.showZones)
                        {
                            menu.AddItem(new GUIContent("Add/"), false, () => { });
                            menu.AddItem(new GUIContent("Add/Zone under Object"), false, AddZoneToObject);
                        }
                        menu.AddItem(new GUIContent("Reset/"), false, () => { });
                        menu.AddItem(new GUIContent("Reset/Reset Rotation"), false, ResetRotation);
                        menu.AddItem(new GUIContent("Reset/Reset Position"), false, ResetPosition);
                        if (lbGroupMember.isGroupOverride) { menu.AddItem(new GUIContent("Reset/Reset Scale"), false, ResetScale); }
                        menu.AddItem(new GUIContent("Snap/"), false, () => { });
                        menu.AddItem(new GUIContent("Snap/Pivot to Ground"), false, () => SnapToGround(false));
                        menu.AddItem(new GUIContent("Snap/Model to Ground"), false, () => SnapToGround(true));
                        if (!lbGroupDesignerItem.isObjPathMember)
                        {
                            menu.AddItem(new GUIContent("Place In Centre +offset"), lbGroupMember.isPlacedInCentre, TogglePlaceInCentre);
                        }
                        menu.AddItem(new GUIContent("Override Group"), lbGroupMember.isGroupOverride, ToggleOverrideGroupDefaults);
                        menu.AddItem(new GUIContent("Rotation/Face 2 Group Centre"), lbGroupMember.rotationType == LBGroupMember.LBRotationType.Face2GroupCentre, SetRotationType, LBGroupMember.LBRotationType.Face2GroupCentre);
                        menu.AddItem(new GUIContent("Rotation/Face 2 Zone Centre"), lbGroupMember.rotationType == LBGroupMember.LBRotationType.Face2ZoneCentre, SetRotationType, LBGroupMember.LBRotationType.Face2ZoneCentre);
                        menu.AddItem(new GUIContent("Rotation/Group Space"), lbGroupMember.rotationType == LBGroupMember.LBRotationType.GroupSpace, SetRotationType, "GroupSpace");
                        menu.AddItem(new GUIContent("Rotation/World Space"), lbGroupMember.rotationType == LBGroupMember.LBRotationType.WorldSpace, SetRotationType, "WorldSpace");
                        menu.AddItem(new GUIContent("Rotation/"), false, () => { });
                        menu.AddItem(new GUIContent("Rotation/Randomise Y"), lbGroupMember.randomiseRotationY, () =>
                        {
                            lbGroupMember.randomiseRotationY = !lbGroupMember.randomiseRotationY;
                            lbGroupMember.showtabInEditor = (int)LBGroupMember.LBMemberEditorTab.XYZ;
                            LBEditorHelper.RepaintEditorWindow(typeof(LandscapeBuilderWindow));
                            lbGroupDesignerItem.lbGroupDesigner.UpdateGroupMember(lbGroupMember);
                        });
                    }

                    menu.AddSeparator("");

                    if (lbGroupDesignerItem.isObjPathMember && !isInSubGroup)
                    {
                        menu.AddItem(new GUIContent("Show Object Path"), false, () =>
                        {
                            if (!string.IsNullOrEmpty(lbGroupDesignerItem.objPathGroupMemberGUID) && lbGroupDesignerItem.lbGroupDesigner.lbGroup != null)
                            {
                                LBGroupMember objPathGroupMember = lbGroupDesignerItem.lbGroupDesigner.lbGroup.GetMemberByGUID(lbGroupDesignerItem.objPathGroupMemberGUID, false);
                                if (objPathGroupMember != null)
                                {
                                    lbGroupDesignerItem.lbGroupDesigner.lbGroup.GroupMemberListExpand(false);
                                    lbGroupDesignerItem.lbGroupDesigner.lbGroup.showGroupMembersInEditor = true;
                                    objPathGroupMember.showInEditor = true;
                                    LBEditorHelper.RepaintLBW();
                                }
;                            };
                        });
                    }

                    menu.AddItem(new GUIContent("Zoom Out"), false, () => { lbGroupDesignerItem.lbGroupDesigner.ZoomExtent(SceneView.lastActiveSceneView); });
                    menu.AddItem(new GUIContent("Zoom In"), false, () => { lbGroupDesignerItem.lbGroupDesigner.ZoomIn(SceneView.lastActiveSceneView); });
                    menu.AddItem(new GUIContent("Display/Group Extent"), lbGroupDesignerItem.lbGroupDesigner.showGroupExtent, () => { lbGroupDesignerItem.lbGroupDesigner.showGroupExtent = !lbGroupDesignerItem.lbGroupDesigner.showGroupExtent; });
                    menu.AddItem(new GUIContent("Display/SubGroup Extents"), lbGroupDesignerItem.lbGroupDesigner.showSubGroupExtent, () => { lbGroupDesignerItem.lbGroupDesigner.showSubGroupExtent = !lbGroupDesignerItem.lbGroupDesigner.showSubGroupExtent; });
                    menu.AddItem(new GUIContent("Display/Member Extent Proximity"), lbGroupDesignerItem.lbGroupDesigner.showProximity, () =>
                    {
                        lbGroupDesignerItem.lbGroupDesigner.showProximity = !lbGroupDesignerItem.lbGroupDesigner.showProximity;
                        if (lbGroupDesignerItem.lbGroupDesigner.showProximity) { lbGroupMember.showtabInEditor = (int)LBGroupMember.LBMemberEditorTab.Proximity; }
                        LBEditorHelper.RepaintEditorWindow(typeof(LandscapeBuilderWindow));
                    });
                    menu.AddItem(new GUIContent("Display/Member Tree Proximity"), lbGroupDesignerItem.lbGroupDesigner.showTreeProximity, () =>
                    {
                        lbGroupDesignerItem.lbGroupDesigner.showTreeProximity = !lbGroupDesignerItem.lbGroupDesigner.showTreeProximity;
                        if (lbGroupDesignerItem.lbGroupDesigner.showTreeProximity) { lbGroupMember.showtabInEditor = (int)LBGroupMember.LBMemberEditorTab.Proximity; }
                        LBEditorHelper.RepaintEditorWindow(typeof(LandscapeBuilderWindow));
                    });
                    menu.AddItem(new GUIContent("Display/Member Flatten Area"), lbGroupDesignerItem.lbGroupDesigner.showFlattenArea, () =>
                    {
                        lbGroupDesignerItem.lbGroupDesigner.showFlattenArea = !lbGroupDesignerItem.lbGroupDesigner.showFlattenArea;
                        if (lbGroupDesignerItem.lbGroupDesigner.showFlattenArea) { lbGroupMember.showtabInEditor = (int)LBGroupMember.LBMemberEditorTab.General; }
                        LBEditorHelper.RepaintEditorWindow(typeof(LandscapeBuilderWindow));
                    });
                    menu.AddItem(new GUIContent("Display/Zones"), lbGroupDesignerItem.lbGroupDesigner.showZones, () =>
                    {
                        lbGroupDesignerItem.lbGroupDesigner.showZones = !lbGroupDesignerItem.lbGroupDesigner.showZones;
                        if (lbGroupDesignerItem.lbGroupDesigner.showZones) { lbGroupMember.showtabInEditor = (int)LBGroupMember.LBMemberEditorTab.Zone; }
                        LBEditorHelper.RepaintEditorWindow(typeof(LandscapeBuilderWindow));
                    });
                    // Cannot directly delete items:
                    // 1. in an Object Path
                    // 2. in a subgroup within the current Group
                    // 3. a whole subgroup within the current Group
                    if (!lbGroupDesignerItem.isObjPathMember && !isInSubGroup && !lbGroupDesignerItem.isSubGroup)
                    {
                        menu.AddSeparator("");
                        menu.AddItem(new GUIContent("Delete"), false, DeleteMember);
                    }
                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Unselect"), false, () => { Selection.activeObject = null; });
                    // The Cancel option is not really necessary as use can just click anywhere else. However, it may help some users.
                    menu.AddItem(new GUIContent("Cancel"), false, () => { });
                    menu.ShowAsContext();
                    current.Use();
                }
                #endregion
            }
        }

        #endregion

        #region Private Methods

        private void CloseGroupDesigner()
        {
            LBLandscape landscape = lbGroupDesignerItem.lbGroupDesigner.transform.GetComponentInParent<LBLandscape>();
            if (landscape != null && lbGroupDesignerItem != null && lbGroupDesignerItem.lbGroupDesigner != null)
            {
                lbGroupDesignerItem.lbGroupDesigner.lbGroup.showGroupDesigner = !LBGroupDesigner.ShowGroupDesigner(landscape, ref lbGroupDesignerItem.lbGroupDesigner, lbGroupDesignerItem.lbGroupDesigner.lbGroup, false);

                // Save the scene after attempting to close the Designer
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    if (lbGroupDesignerItem.lbGroupDesigner.autoSaveEnabled) { EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene()); }
                    else { EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene()); }
                }
            }
        }

        private void RefreshDesigner(bool forceRefresh)
        {
            if (lbGroupDesignerItem.lbGroupDesigner != null) { lbGroupDesignerItem.lbGroupDesigner.RefreshWorkspace(forceRefresh); }
        }

        private void CheckProximity(System.Object obj)
        {
            if (lbGroupDesignerItem.lbGroupDesigner != null)
            {
                lbGroupDesignerItem.lbGroupDesigner.isCheckProximity = (bool)obj;
                lbGroupDesignerItem.lbGroupDesigner.RefreshWorkspace();
            }
        }

        private void SetRotationType(object obj)
        {
            if (lbGroupMember != null && lbGroupDesignerItem != null)
            {
                // Find the matching RotationType, and update the instances of the member in the Group Designer.
                foreach (LBGroupMember.LBRotationType rotationType in System.Enum.GetValues(typeof(LBGroupMember.LBRotationType)))
                {
                    if (rotationType.ToString() == obj.ToString())
                    {
                        lbGroupMember.rotationType = rotationType;
                        lbGroupDesignerItem.lbGroupDesigner.UpdateGroupMember(lbGroupMember);
                        LBEditorHelper.RepaintEditorWindow(typeof(LandscapeBuilderWindow));
                        break;
                    }
                }
            }
        }

        private void ToggleOverrideGroupDefaults()
        {
            if (lbGroupMember != null && lbGroupDesignerItem != null)
            {
                lbGroupMember.isGroupOverride = !lbGroupMember.isGroupOverride;
                if (lbGroupMember.isGroupOverride)
                {
                    // Copy Group defaults to the member
                    lbGroupMember.minScale = lbGroup.minScale;
                    lbGroupMember.maxScale = lbGroup.maxScale;
                    lbGroupMember.minHeight = lbGroup.minHeight;
                    lbGroupMember.maxHeight = lbGroup.maxHeight;
                    lbGroupMember.minInclination = lbGroup.minInclination;
                    lbGroupMember.maxInclination = lbGroup.maxInclination;
                }
                LBEditorHelper.RepaintEditorWindow(typeof(LandscapeBuilderWindow));
                lbGroupDesignerItem.lbGroupDesigner.UpdateGroupMember(lbGroupMember);
            }
        }

        private void TogglePlaceInCentre()
        {
            lbGroupMember.isPlacedInCentre = !lbGroupMember.isPlacedInCentre;
            if (lbGroupMember.isPlacedInCentre)
            {
                // When turning on isPlacedInCentre, take the location of the current instance of the member.
                lbGroupMember.minOffsetX = lbGroupDesignerItem.position.x;
                lbGroupMember.minOffsetZ = lbGroupDesignerItem.position.z;
            }
            else
            {
                // Reset when turning off isPlacedInCentre
                lbGroupMember.minOffsetX = 0f;
                lbGroupMember.minOffsetZ = 0f;
            }

            lbGroupMember.showtabInEditor = (int)LBGroupMember.LBMemberEditorTab.General;

            // Turning off/on isPlacedInCentre for a member could significantly change the layout, so refresh the entire workspace
            RefreshDesigner(false);
            LBEditorHelper.RepaintEditorWindow(typeof(LandscapeBuilderWindow));
        }

        /// <summary>
        /// Snap the prefab to ground (y-axis). Sets the prefab position to 0 on y-axis.
        /// If isConsiderPrefabExtents is true, set ground to be the bottom of the prefab
        /// rather than its pivot point.
        /// NOTE: Currently doesn't consider scaling.
        /// </summary>
        /// <param name="isConsiderPrefabExtents"></param>
        private void SnapToGround(bool isConsiderPrefabExtents = false)
        {
            if (lbGroupMember != null && lbGroupDesignerItem != null)
            {
                // Default
                lbGroupMember.minOffsetY = 0f;
                lbGroupMember.maxOffsetY = 0f;

                if (isConsiderPrefabExtents && lbGroupMember.prefab != null && lbGroupMember.lbMemberType == LBGroupMember.LBMemberType.Prefab)
                {
                    // Need to reset y value for GetBounds to work correctly
                    Vector3 tempPos = lbGroupMember.prefab.transform.position;
                    tempPos.y = 0f;
                    lbGroupMember.prefab.transform.position = tempPos;

                    Bounds bounds = LBMeshOperations.GetBounds(lbGroupMember.prefab.transform, false, true);
                    if (bounds.extents.y != 0f)
                    {
                        // bounds.extents.y is half the height of the prefab.
                        lbGroupMember.minOffsetY = bounds.extents.y - bounds.center.y;
                        lbGroupMember.maxOffsetY = lbGroupMember.minOffsetY;
                    }
                }

                lbGroupDesignerItem.lbGroupDesigner.UpdateGroupMember(lbGroupMember);
                LBEditorHelper.RepaintEditorWindow(typeof(LandscapeBuilderWindow));
            }
        }

        /// <summary>
        /// Add a zone at the same location as an object/prefab using its extents.
        /// NOTE: Rectangular zones cannot be rotated, so only width and length can be switched
        /// </summary>
        private void AddZoneToObject()
        {
            if (lbGroup != null && lbGroupMember != null && lbGroupDesignerItem != null && lbGroupMember.prefab != null && lbGroupMember.lbMemberType == LBGroupMember.LBMemberType.Prefab)
            {
                // Need to reset postion for GetBounds to work correctly
                Vector3 tempPos = lbGroupMember.prefab.transform.position;
                lbGroupMember.prefab.transform.position = Vector3.zero;
                Bounds bounds = LBMeshOperations.GetBounds(lbGroupMember.prefab.transform, false, true);
                lbGroupMember.prefab.transform.position = tempPos;

                if (bounds.extents.x != 0f && bounds.extents.z != 0f && lbGroup.maxClearingRadius > 0f)
                {
                    LBGroupZone lbGroupZone = new LBGroupZone();
                    if (lbGroupZone != null)
                    {
                        if (bounds.extents.x == bounds.extents.z)
                        {
                            lbGroupZone.zoneType = LBGroupZone.LBGroupZoneType.circle;
                            lbGroupZone.centrePointX = lbGroupMember.minOffsetX / lbGroup.maxClearingRadius;
                            lbGroupZone.centrePointZ = lbGroupMember.minOffsetZ / lbGroup.maxClearingRadius;
                            lbGroupZone.width = (bounds.extents.x - bounds.center.x) / lbGroup.maxClearingRadius;
                            lbGroupZone.length = lbGroupZone.width;
                        }
                        else
                        {
                            lbGroupZone.zoneType = LBGroupZone.LBGroupZoneType.rectangle;
                            lbGroupZone.centrePointX = lbGroupMember.minOffsetX / lbGroup.maxClearingRadius;
                            lbGroupZone.centrePointZ = lbGroupMember.minOffsetZ / lbGroup.maxClearingRadius;
                            lbGroupZone.width = (bounds.extents.x - bounds.center.x) * 2f / lbGroup.maxClearingRadius;
                            lbGroupZone.length = (bounds.extents.z - bounds.center.z) * 2f / lbGroup.maxClearingRadius;

                            float rotationY = lbGroupDesignerItem.transform.rotation.eulerAngles.y;

                            // Get the absolute rotation on y-axis
                            if (rotationY < 0) { rotationY = -rotationY; }

                            // cater for rotation by switching width and length
                            if ((rotationY > 45f && rotationY < 135f) || (rotationY > 225f && rotationY < 315f))
                            {
                                float width = lbGroupZone.width;
                                lbGroupZone.width = lbGroupZone.length;
                                lbGroupZone.length = width;
                            }
                        }

                        lbGroupZone.zoneName = lbGroupMember.prefab.name + " zone";
                        lbGroup.zoneList.Add(lbGroupZone);
                    }
                }

                LBEditorHelper.RepaintEditorWindow(typeof(LandscapeBuilderWindow));
            }
        }

        private void ResetScale()
        {
            if (lbGroupMember != null && lbGroupDesignerItem != null)
            {
                if (lbGroupMember.isGroupOverride)
                {
                    if (lbGroupMember.isKeepPrefabConnection)
                    {
                        // Find the original source prefab in the project folder
                        #if UNITY_2018_2_OR_NEWER
                        GameObject prefabSource = (GameObject)UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(lbGroupDesignerItem.gameObject);
                        #else
                        GameObject prefabSource = (GameObject)UnityEditor.PrefabUtility.GetPrefabParent(lbGroupDesignerItem.gameObject);
                        #endif
                        if (prefabSource != null)
                        {
                            lbGroupMember.minScale = prefabSource.transform.localScale.x;
                            lbGroupMember.maxScale = lbGroupMember.minScale;
                        }
                        else
                        {
                            lbGroupMember.minScale = 1f;
                            lbGroupMember.maxScale = 1f;
                        }
                    }
                    else
                    {
                        lbGroupMember.minScale = 1f;
                        lbGroupMember.maxScale = 1f;
                    }
                    lbGroupDesignerItem.lbGroupDesigner.UpdateGroupMember(lbGroupMember);
                    LBEditorHelper.RepaintEditorWindow(typeof(LandscapeBuilderWindow));
                }
            }
        }

        private void ResetPosition()
        {
            if (lbGroupMember != null && lbGroupDesignerItem != null)
            {
                lbGroupMember.minOffsetX = 0f;
                lbGroupMember.minOffsetZ = 0f;

                if (!lbGroupMember.randomiseOffsetY) { lbGroupMember.minOffsetY = 0f; }

                lbGroupDesignerItem.lbGroupDesigner.UpdateGroupMember(lbGroupMember);
                LBEditorHelper.RepaintEditorWindow(typeof(LandscapeBuilderWindow));
            }
        }

        private void ResetRotation()
        {
            if (lbGroupMember != null && lbGroupDesignerItem != null)
            {
                lbGroupMember.rotationX = 0f;
                lbGroupMember.rotationZ = 0f;

                lbGroupMember.startRotationY = 0f;
                lbGroupMember.endRotationY = 359.9f;

                lbGroupDesignerItem.lbGroupDesigner.UpdateGroupMember(lbGroupMember);
                LBEditorHelper.RepaintEditorWindow(typeof(LandscapeBuilderWindow));
            }
        }

        private void DeleteMember()
        {
            if (lbGroupMember != null && lbGroupDesignerItem != null)
            {
                int groupMemberPos = lbGroup.groupMemberList.FindIndex(gmbr => gmbr == lbGroupMember);
                if (LBEditorHelper.PromptForDelete("Group Member", "", groupMemberPos, false))
                {
                    lbGroup.groupMemberList.Remove(lbGroupMember);
                    lbGroupMember = null;

                    if (lbGroupDesignerItem.lbGroupDesigner)
                    {
                        lbGroupDesignerItem.lbGroupDesigner.RefreshWorkspace();
                    }
                }
            }
        }

        #endregion
    }
}