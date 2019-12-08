using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LandscapeBuilder
{
    /// <summary>
    /// Gets attached to prefabs in the scene while LBGroupDesigner is enabled.
    /// </summary>
    [ExecuteInEditMode]
    public class LBGroupDesignerItem : MonoBehaviour
    {
        #region Public variables
        [HideInInspector] public string GroupMemberGUID;
        [HideInInspector] public LBGroupMember lbGroupMember;
        #if UNITY_EDITOR
        [HideInInspector] public LBGroupDesigner lbGroupDesigner;
        #endif
        [HideInInspector] public bool isSelected = false;
        [HideInInspector] public bool isObjPathMember = false;
        [HideInInspector] public string objPathGroupMemberGUID;
        // Items in the GroupDesigner can be members of a SubGroup spawned by a "parent" Clearing Group
        [HideInInspector] public string SubGroupGUID;
        /// <summary>
        /// Is this item being used to show the location of the whole subgroup? If so, set the ParentGroupGUID
        /// but not the GroupMemberGUID or lbGroupMember fields.
        /// </summary>
        [HideInInspector] public bool isSubGroup = false;
        [HideInInspector] public float subGroupRadius = 0f;
        
        /// <summary>
        /// This is not the actual offset to be used in the editor. It is used in the Group Designer to simulate
        /// where it might be placed when applying group members in the LB Window.
        /// </summary>
        [HideInInspector] public Vector3 basePlacementOffset = Vector3.zero;
        [HideInInspector] public Vector3 position;
        [HideInInspector] public Quaternion rotation;
        [HideInInspector] public Vector3 scale;

        #endregion

        #region Private Variables

         #if UNITY_EDITOR
        // dark green
        private Color treeProximityColour = new Color(69f / 255f, 139f / 255f, 0f, 1f);
        private Color flattenAreaColour = new Color(255f / 255f, 255f / 255f, 0f, 0.1f);
        private Color flattenAreaBlendColour = new Color(255f / 255f, 255f / 255f, 0f, 0.5f);
        private Color subGroupAreaColour = new Color(198f / 255f, 134f / 255f, 66f / 255f, 0.2f);
        #endif

        #endregion

        #if UNITY_EDITOR

        #region Initialise Methods

        #endregion

        #region Update Method (EDITOR_ONLY)

        void Update()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { return; }

            if (Selection.activeTransform != null)
            {
                // Has the user selected this prefabinstance or a child of the prefab?
                // NOTE: This could be a little slow if user selects the "wrong" object in hierarchy
                if (Selection.activeTransform.IsChildOf(this.transform))
                {
                    // If user selected a child of the prefab, select the root of the prefab.
                    if (Selection.activeTransform != this.transform) { Selection.activeTransform = this.transform; }

                    // Was this previously unselected in the last frame?
                    if (!isSelected)
                    {
                        isSelected = true;
                        // Auto shrink all other members
                        if (lbGroupDesigner != null && lbGroupMember != null)
                        {
                            if (lbGroupDesigner.lbGroup != null)
                            {
                                lbGroupDesigner.lbGroup.GroupMemberListExpand(false);
                                lbGroupMember.showInEditor = true;
                                LBEditorHelper.RepaintLBW();
                            }
                        }
                    }
                }
                else { isSelected = false; }
            }
        }

        #endregion

        #region Public Member Methods

        /// <summary>
        /// If the prefab is instantiated from LBLandscapeTerrain.PopulateLandscapeWithGroups(..)
        /// it doesn't know about editor-only classes like LBGroupDesigner. That's because it can be
        /// called at runtime. This method can be called to add the GroupDesigner reference to itself.
        /// </summary>
        public void FindGroupDesigner()
        {
            // Get the parent of this object which should be gameobject beginning with [DESIGNER]
            Transform tfm = this.transform.parent;
            if (tfm != null && tfm.name.StartsWith("[DESIGNER]"))
            {
                Transform landscapeTfrm = tfm.parent;
                if (landscapeTfrm != null)
                {
                    // Get the first (and hopefully only) LBGroupDesigner
                    lbGroupDesigner = landscapeTfrm.GetComponentInChildren<LBGroupDesigner>();
                }
            }
            // SubGroup "items" appear directly below the lanscape.
            else if (this.name.StartsWith("[DESIGNER]"))
            {
                Transform landscapeTfrm = this.transform.parent;
                if (landscapeTfrm != null)
                {
                    // Get the first (and hopefully only) LBGroupDesigner
                    lbGroupDesigner = landscapeTfrm.GetComponentInChildren<LBGroupDesigner>();
                }
            }
        }

        #endregion

        #region Event Methods

        // Draw gizmos whenever the designer is being shown in the scene
        // Gizmos are rendered after meshes so will appear infront or over the top of meshes.
        private void OnDrawGizmos()
        {
            if (lbGroupDesigner != null)
            {
                if (lbGroupMember != null && (lbGroupDesigner.showProximity || lbGroupDesigner.showTreeProximity || lbGroupDesigner.showFlattenArea))
                {
                    // Adjust the gizmo position to not take into account model offset
                    Vector3 arcPosition = this.transform.position - (this.transform.localRotation * Vector3.Scale(new Vector3(lbGroupMember.modelOffsetX, lbGroupMember.modelOffsetY, lbGroupMember.modelOffsetZ), this.transform.localScale));
                    arcPosition.y = lbGroupDesigner.BasePlaneOffsetY;

                    if (lbGroupDesigner.showProximity)
                    {
                        // Show the proximity extent as a 2D circle
                        using (new Handles.DrawingScope(Color.blue))
                        {
                            Handles.DrawWireDisc(arcPosition, Vector3.up, lbGroupMember.proximityExtent);
                        }
                    }

                    if (lbGroupDesigner.showTreeProximity)
                    {
                        // Show the proximity extent as a 2D circle
                        using (new Handles.DrawingScope(treeProximityColour))
                        {
                            Handles.DrawWireDisc(arcPosition, Vector3.up, lbGroupMember.minTreeProximity);
                        }
                    }

                    if (lbGroupDesigner.showFlattenArea && lbGroupMember.isTerrainFlattened)
                    {
                        // Show flatten area as a transparent solid disc.
                        using (new Handles.DrawingScope(flattenAreaColour))
                        {
                            Handles.DrawSolidDisc(arcPosition, Vector3.up, lbGroupMember.flattenDistance);
                        }
                        // Show the blend rate as a ring
                        using (new Handles.DrawingScope(flattenAreaBlendColour))
                        {
                            Handles.DrawWireDisc(arcPosition, Vector3.up, lbGroupMember.flattenDistance - (lbGroupMember.flattenDistance * lbGroupMember.flattenBlendRate));
                        }
                    }
                }
                else if (lbGroupDesigner.showSubGroupExtent && isSubGroup)
                {
                    Vector3 arcPosition = this.transform.position;
                    // Show SubGroup as a transparent solid disc.
                    using (new Handles.DrawingScope(subGroupAreaColour))
                    {
                        Handles.DrawSolidDisc(arcPosition, Vector3.up, subGroupRadius);
                    }
                }
            }
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Create a gameobject in the GroupDesigner that can be used to track location and size
        /// of SubGroups. Currently it assumes they are spawned from a Object Path in the parent
        /// Clearing Group. The LBPrefabItem component type may need to be changed for subgroups
        /// not created from an Object Path.
        /// </summary>
        /// <param name="parentTfrm"></param>
        /// <param name="parentGroup"></param>
        /// <param name="parentGroupMember"></param>
        /// <param name="subGroup"></param>
        /// <param name="subGroupPosition"></param>
        /// <param name="subGroupRotation"></param>
        /// <param name="subGroupRadius"></param>
        /// <param name="regionIdx"></param>
        /// <returns></returns>
        public static LBGroupDesignerItem CreateSubGroupItem(Transform parentTfrm, LBGroup parentGroup, LBGroupMember parentGroupMember, LBGroup subGroup, Vector3 subGroupPosition, Vector3 subGroupRotation, float subGroupRadius, int regionIdx)
        {
            LBGroupDesignerItem lbGroupDesignerItem = null;

            if (parentGroup != null && parentTfrm != null && subGroup != null)
            {
                // [DESIGNER] (parentGroupName.subgroup:subGroupName.regionnumber)
                GameObject subGroupGO = new GameObject("[DESIGNER] (" + (string.IsNullOrEmpty(parentGroup.groupName) ? "ParentGroup" : parentGroup.groupName) + ".subgroup:" + (string.IsNullOrEmpty(subGroup.groupName) ? "SubGroup" : subGroup.groupName) + "." + (regionIdx + 1) + ")");

                if (subGroupGO != null)
                {
                    Quaternion rotation = Quaternion.Euler(subGroupRotation);
                    subGroupGO.transform.SetPositionAndRotation(subGroupPosition, rotation);
                    subGroupGO.transform.SetParent(parentTfrm);
                    lbGroupDesignerItem = subGroupGO.AddComponent<LBGroupDesignerItem>();
                    if (lbGroupDesignerItem != null)
                    {
                        lbGroupDesignerItem.isSubGroup = true;
                        lbGroupDesignerItem.SubGroupGUID = subGroup == null ? string.Empty : subGroup.GUID;
                        lbGroupDesignerItem.position = subGroupPosition;
                        lbGroupDesignerItem.rotation = rotation;
                        lbGroupDesignerItem.subGroupRadius = subGroupRadius;
                        lbGroupDesignerItem.FindGroupDesigner();

                        if (parentGroupMember != null)
                        {
                            lbGroupDesignerItem.objPathGroupMemberGUID = parentGroupMember.GUID;
                            lbGroupDesignerItem.isObjPathMember = parentGroupMember.lbMemberType == LBGroupMember.LBMemberType.ObjPath;
                        }
                        else
                        {
                            lbGroupDesignerItem.objPathGroupMemberGUID = string.Empty;
                            lbGroupDesignerItem.isObjPathMember = false;
                        }
                    }

                    LBPrefabItem lbPrefabItem = subGroupGO.AddComponent<LBPrefabItem>();
                    if (lbPrefabItem != null)
                    {
                        lbPrefabItem.prefabItemType = LBPrefabItem.PrefabItemType.ObjPathDesignerPrefab;
                        // Add the GUID of the object path GroupMember so that we can track them in the scene
                        // It lets us enable/disable existing prefabs when using the Object Path Designer
                        // If this Group is being spawned from a parent group, assign the member GUID from that parent instead
                        // of using the current member GUID. This ensures that the Designers can disable and enable the correct gameobject
                        // for subgroups.
                        lbPrefabItem.groupMemberGUID = parentGroupMember != null ? parentGroupMember.GUID : subGroup.GUID;
                    }
                }
            }

            return lbGroupDesignerItem;
        }

        #endregion

        #endif
    }
}