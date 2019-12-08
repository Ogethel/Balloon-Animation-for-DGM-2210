// Landscape Builder. Copyright (c) 2016-2019 SCSM Pty Ltd. All rights reserved.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace LandscapeBuilder
{
    public class LBPathImporter : EditorWindow
    {
        #region Public variables

        #endregion

        #region Private variables
        private LBLandscape landscape = null;
        
        // General
        private Vector2 scrollPosition = Vector2.zero;
        private Color currentBgndColor = Color.white;
        private string txtColourName = "black";
        private GUIStyle labelFieldRichText = null;
        private GUIStyle foldoutStyleNoLabel = null;
        //private float currentLabelWidth = 0f;
        //private static readonly float labelWidthOffset = 25f;
        private float defaultEditorLabelWidth = 150f;
        private float defaultEditorFieldWidth = 0f;

        // EasyRoads
        #if LB_EDITOR_ER3
        private int splinePointFilterSize = 10;
        #endif

        // Roads
        private List<LBRoad> roadList = null;
        private List<LBRoad> filteredRoadList = null;
        private int numAllRoads = 0;
        private int numFilteredRoads = 0;
        private int numAllRoadTypes = 0;
        private List<LBRoadType> roadTypeList = null;
        private LBRoad lbRoad = null;
        private LBRoadType lbRoadType = null;
        private float colSelectorWidth = 20f;
        private float colRoadTypeWidth = 120f;
        private float colRoadNameWidth = 150f;

        private bool isFilterByRoadType = false;
        private bool isShowFilterList = false;
        private bool isSelectAllFilteredRoads = false;
        private int groupLookupIndex = -1;
        private LBGroup lbGroupTarget = null;
        private List<string> groupNameList = null;
        private int numGroupNames = 0;
        #endregion

        #region GUIContent
        private static readonly GUIContent landscapeContent = new GUIContent(" Landscape", "Drag in the Landscape parent GameObject from the scene Hierarchy.");
        private static readonly GUIContent filterByRoadTypeContent = new GUIContent("Filter by Road Type", "Enable filtering by road type");
        private static readonly GUIContent copyRoadsBtnContent = new GUIContent("Copy->New", "Copy the filtered roads into new Group Object Paths in the Target Group");
        private static readonly GUIContent updateRoadsBtnContent = new GUIContent("Update+New", "Update existing Object Paths based on matching road name in the Target Group. Add new Object Paths where there is not a match.");
        #endregion

        #region GUIContent - ER

        #if LB_EDITOR_ER3
        private static readonly GUIContent erGetRoadsBtnContent = new GUIContent("Get Roads", "Get the roads from the EasyRoads Road Network in the scene");
        private static readonly GUIContent erClearRoadsBtnContent = new GUIContent("Clear Roads", "Clear the current list of roads. NOTE: This does NOT affect the LB Groups");
        private static readonly GUIContent erSplinePointSizeContent = new GUIContent("Spline Filter Size", "The approximate distance between spline points use to create the Object Path");
        #endif

        #endregion

        #region Event Methods

        private void OnGUI()
        {
            #region Initialise

            defaultEditorLabelWidth = EditorGUIUtility.labelWidth;
            defaultEditorFieldWidth = EditorGUIUtility.fieldWidth;

            //if (labelFieldRichText == null)
            {
                labelFieldRichText = new GUIStyle("Label");
                labelFieldRichText.richText = true;
            }

            // Overide default styles
            EditorStyles.foldout.fontStyle = FontStyle.Bold;

            // When using a no-label foldout, don't forget to set the global
            // EditorGUIUtility.fieldWidth to a small value like 15, then back
            // to the original afterward.
            foldoutStyleNoLabel = new GUIStyle(EditorStyles.foldout);
            foldoutStyleNoLabel.fixedWidth = 0.01f;
            #endregion

            GUILayout.BeginVertical("HelpBox");

            EditorGUILayout.HelpBox("The LB Path Importer will help you import Group Object Paths from an external source (e.g. EasyRoads3D) [EXPERIMENTAL]", MessageType.Info, true);
            EditorGUIUtility.labelWidth = 100f;
            landscape = (LBLandscape)EditorGUILayout.ObjectField(landscapeContent, landscape, typeof(LBLandscape), true);
            EditorGUIUtility.labelWidth = defaultEditorLabelWidth;

            EditorGUILayout.Space();

            #region Import - EasyRoads

            currentBgndColor = GUI.backgroundColor;
            GUI.backgroundColor = GUI.backgroundColor * (EditorGUIUtility.isProSkin ? 0.7f : 1.3f);
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = currentBgndColor;
            EditorGUILayout.LabelField("<color=" + txtColourName + "><b>Import from EasyRoads3D</b></color>", labelFieldRichText);

            #if LB_EDITOR_ER3
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(erGetRoadsBtnContent, GUILayout.MaxWidth(100f)))
            {
                GetRoadList();
                GetFilteredRoadList();
            }
            if (GUILayout.Button(erClearRoadsBtnContent, GUILayout.MaxWidth(100f)))
            {
                if (roadList != null) { roadList.Clear(); }
                if (filteredRoadList != null) { filteredRoadList.Clear(); }
                if (roadTypeList != null) { roadTypeList.Clear(); }
                numAllRoads = 0;
                numFilteredRoads = 0;
                numAllRoadTypes = 0;
            }
            GUILayout.EndHorizontal();
            #else
            EditorGUILayout.HelpBox("EasyRoads3D v3.x does not appear to be installed", MessageType.Info, true);
            #endif
            #endregion

            EditorGUILayout.LabelField("Roads (all): " + numAllRoads.ToString("###0"));
            EditorGUILayout.LabelField("Roads (filtered): " + numFilteredRoads.ToString("###0"));
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("<color=" + txtColourName + "><b>Copy Filtered Roads to LB Group</b></color>", labelFieldRichText);
            if (landscape == null)
            {
                EditorGUILayout.HelpBox("Select a landscape to export roads to Group Object Paths", MessageType.Info, true);
            }
            else
            {
                List<LBGroup> groupList = landscape.lbGroupList.FindAll(grp => grp.lbGroupType == LBGroup.LBGroupType.Uniform);

                int numUniformGroups = groupList == null ? 0 : groupList.Count;

                GetGroupNameList();

                if (numUniformGroups == 0)
                {
                    EditorGUILayout.HelpBox("There are no Uniform Groups in the landscape", MessageType.Info, true);
                }
                else
                {
                    EditorGUI.BeginChangeCheck();
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Target Group", GUILayout.Width(90f));
                    if (groupLookupIndex > numGroupNames - 1) { groupLookupIndex = -1; lbGroupTarget = null; };
                    groupLookupIndex = EditorGUILayout.Popup(groupLookupIndex, groupNameList.ToArray());
                    GUILayout.EndHorizontal();
                    if (EditorGUI.EndChangeCheck())
                    {
                        // Attempt to get the actual LBGroup instance
                        if (groupLookupIndex < 0 || groupLookupIndex > numGroupNames - 1) { lbGroupTarget = null; }
                        else
                        {
                            string groupName = groupNameList[groupLookupIndex];
                            if (string.IsNullOrEmpty(groupName)) { lbGroupTarget = null; }
                            else if (groupName.Length < 7) { lbGroupTarget = null; }
                            else
                            {
                                // Extract the group number out of the group name string
                                int nextSpace = groupName.IndexOf(" ", 6);
                                if (nextSpace < 7) { lbGroupTarget = null; }
                                else
                                {
                                    int grpNumber = -1;
                                    int.TryParse(groupName.Substring(6, nextSpace - 6), out grpNumber);
                                    if (grpNumber >= 0)
                                    {
                                        lbGroupTarget = landscape.lbGroupList[grpNumber-1];
                                        //Debug.Log("[DEBUG: group " + lbGroupTarget == null ? "uknown" : lbGroupTarget.groupName);
                                    }
                                }
                            }
                        }
                    }

                    // If user has deleted a Group which was previously selected in the Path Importer, remove this as the target
                    if (groupLookupIndex < 0 && lbGroupTarget != null) { lbGroupTarget = null; }

                    #if LB_EDITOR_ER3
                    splinePointFilterSize = EditorGUILayout.IntSlider(erSplinePointSizeContent, splinePointFilterSize, 1, 50);
                    #endif

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(copyRoadsBtnContent, GUILayout.MaxWidth(100f)))
                    {
                        if (lbGroupTarget != null)
                        {
                            #if LB_EDITOR_ER3
                            GetERSplineForRoads();
                            #endif
                            CopyRoads(false);
                        }
                        else { EditorUtility.DisplayDialog("Copy Roads to Group", "You need to select a Target (Uniform) Group first", "Got it!"); }
                    }
                    if (GUILayout.Button(updateRoadsBtnContent, GUILayout.MaxWidth(100f)))
                    {
                        if (lbGroupTarget != null)
                        {
                            #if LB_EDITOR_ER3
                            GetERSplineForRoads();
                            #endif
                            CopyRoads(true);
                        }
                        else { EditorUtility.DisplayDialog("Update Roads in Group", "You need to select a Target (Uniform) Group first", "Got it!"); }
                    }
                    GUILayout.EndHorizontal();
                }
                //landscape.lbGroupList.Exists(grp => grp.showGroupsInScene == true);


            }

            EditorGUILayout.Space();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            #region Filtering

            GUILayout.BeginHorizontal();
            // A Foldout with no label must have a style fixedWidth of low non-zero value, and have a small (global) fieldWidth.
            EditorGUIUtility.fieldWidth = 17f;
            isShowFilterList = EditorGUILayout.Foldout(isShowFilterList, "", foldoutStyleNoLabel);
            EditorGUIUtility.fieldWidth = defaultEditorFieldWidth;
            EditorGUI.BeginChangeCheck();
            isFilterByRoadType = EditorGUILayout.Toggle(filterByRoadTypeContent, isFilterByRoadType);
            GUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck())
            {
                GetFilteredRoadList();
            }

            if (isFilterByRoadType)
            {
                if (roadTypeList == null) { roadTypeList = new List<LBRoadType>(20); }
                if (isShowFilterList)
                {
                    for (int rt = 0; rt < numAllRoadTypes; rt++)
                    {
                        lbRoadType = roadTypeList[rt];
                        if (lbRoadType != null)
                        {
                            GUILayout.BeginHorizontal();
                            EditorGUI.BeginChangeCheck();
                            lbRoadType.isSelected = EditorGUILayout.Toggle(lbRoadType.isSelected, GUILayout.Width(colSelectorWidth));
                            GUILayout.Label(lbRoadType.roadTypeDesc);
                            GUILayout.EndHorizontal();
                            if (EditorGUI.EndChangeCheck())
                            {
                                GetFilteredRoadList();
                            }
                        }
                    }
                }
            }

            EditorGUILayout.Space();

            #endregion

            #region Display Filtered Road List

            // Header
            GUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            isSelectAllFilteredRoads = EditorGUILayout.Toggle(isSelectAllFilteredRoads, GUILayout.Width(colSelectorWidth));
            if (EditorGUI.EndChangeCheck())
            {
                SelectFilteredRoads(isSelectAllFilteredRoads);
            }
            EditorGUILayout.LabelField("<color=" + txtColourName + "><b>Road Type</b></color>", labelFieldRichText, GUILayout.Width(colRoadTypeWidth));
            EditorGUILayout.LabelField("<color=" + txtColourName + "><b>Road Name</b></color>", labelFieldRichText, GUILayout.Width(colRoadNameWidth));
            GUILayout.EndHorizontal();

            for (int i = 0; i < numFilteredRoads; i++)
            {
                if (i > 200)
                {
                    if (i != numFilteredRoads - 1) { continue; }
                    else
                    {
                        GUILayout.Label("..");
                    }
                }

                lbRoad = filteredRoadList[i];

                if (lbRoad != null)
                {
                    // Display the selectable road
                    GUILayout.BeginHorizontal();
                    //if (GUILayout.Button(new GUIContent("v", "Move road down in the list"), buttonCompact, GUILayout.MaxWidth(20f)))
                    //{
                    //    lbRoadMoveDown = new LBRoad(lbRoad);
                    //    lbRoadMovePos = i;
                    //}
                    lbRoad.isSelected = EditorGUILayout.Toggle(lbRoad.isSelected, GUILayout.Width(colSelectorWidth));
                    //GUILayout.Label(new GUIContent("R", "Reverse direction of path for this road"), GUILayout.Width(12f));
                    //lbRoad.isReversed = EditorGUILayout.Toggle(lbRoad.isReversed, GUILayout.Width(20f));
                    GUILayout.Label(lbRoad.roadTypeDesc, GUILayout.Width(colRoadTypeWidth));
                    GUILayout.Label(lbRoad.roadName, GUILayout.Width(colRoadNameWidth));
                    GUILayout.EndHorizontal();
                }
            }

            #endregion

            GUILayout.EndVertical();

            EditorGUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void OnEnable()
        {
            // These need to be reset here because the LBRoad class is not serializable,
            // while these values are retained all the time the window is open.
            numAllRoads = 0;
            numFilteredRoads = 0;
            numAllRoadTypes = 0;

            // Default to not selecting a group
            groupLookupIndex = -1;

            if (EditorGUIUtility.isProSkin) { txtColourName = "White"; }
        }

        #endregion

        #region Public Static Methods

        // Add a menu item so that the editor window can be opened via the window menu tab
        [MenuItem("Window/Landscape Builder/Landscape Builder Path Importer")]
        public static void ShowWindow()
        {
            // Show existing window instance. If one doesn't exist, make one.
            EditorWindow.GetWindow(typeof(LBPathImporter), false, "LB Path Importer");
        }

        #endregion

        #region Private ER3 Methods

        #if LB_EDITOR_ER3
        
        /// <summary>
        /// Populate the (all) roadList and the roadTypeList.
        /// </summary>
        private void GetRoadList()
        {
            // Reset
            numAllRoads = 0;
            numAllRoadTypes = 0;

            if (roadList == null) { roadList = new List<LBRoad>(50); }
            else { roadList.Clear(); }

            if (roadTypeList == null) { roadTypeList = new List<LBRoadType>(20); }
            else { roadTypeList.Clear(); }

            // Populate the list of roads
            LBIntegration.GetERRoadList(roadList, true);

            numAllRoads = roadList == null ? 0 : roadList.Count;

            // Populate the list of road types
            string roadTypeDesc = string.Empty;
            LBRoadType lbRoadType = null;

            for (int r = 0; r < numAllRoads; r++)
            {
                roadTypeDesc = roadList[r].roadTypeDesc;

                if (!roadTypeList.Exists(typ => typ.roadTypeDesc == roadTypeDesc && !string.IsNullOrEmpty(roadTypeDesc)))
                {
                    lbRoadType = new LBRoadType();
                    if (lbRoadType != null)
                    {
                        lbRoadType.roadTypeDesc = roadTypeDesc;
                        roadTypeList.Add(lbRoadType);
                    }
                }
            }

            numAllRoadTypes = roadTypeList == null ? 0 : roadTypeList.Count;

            //Debug.Log("[DEBUG] roads: " + roadList.Count);
        }

        /// <summary>
        /// Get the splines for filtered roads matching on road name.
        /// Assumes road names are unique in the ER road network
        /// </summary>
        private void GetERSplineForRoads()
        {
            LBRoad lbRoadToUpdate = null;
            if (filteredRoadList == null) { filteredRoadList = new List<LBRoad>(numAllRoads + 20); }
            for (int i = 0; i < numFilteredRoads; i++)
            {
                lbRoadToUpdate = filteredRoadList[i];

                if (lbRoadToUpdate.isSelected)
                {
                    lbRoadToUpdate.centreSpline = LBIntegration.GetERRoadSplinePoints(lbRoadToUpdate, LBRoad.SplineType.CentreSpline, splinePointFilterSize);
                    lbRoadToUpdate.leftSpline = LBIntegration.GetERRoadSplinePoints(lbRoadToUpdate, LBRoad.SplineType.LeftSpline, splinePointFilterSize);
                    lbRoadToUpdate.rightSpline = LBIntegration.GetERRoadSplinePoints(lbRoadToUpdate, LBRoad.SplineType.RightSpline, splinePointFilterSize);

                    //Debug.Log("[DEBUG] GetERSplineForRoads: " + lbRoadToUpdate.roadName + " points: " + lbRoadToUpdate.leftSpline.Length);
                }
            }
        }

        #endif

        #endregion

        #region Private Methods

        /// <summary>
        /// Copy selected filtered roads to a Uniform Group.
        /// 
        /// </summary>
        /// <param name="isUpdateExisting"></param>
        private void CopyRoads(bool isUpdateExisting)
        {
            if (lbGroupTarget != null)
            {
                LBRoad lbRoadToCopy = null;
                LBGroupMember lbGroupMember = null;
                Vector2 leftSplinePt2D = Vector2.zero, rightSplinePt2D = Vector2.zero;
                bool useWidth = false;
                bool addNew = false;

                if (filteredRoadList == null) { filteredRoadList = new List<LBRoad>(numAllRoads + 20); }
                for (int i = 0; i < numFilteredRoads; i++)
                {
                    lbRoadToCopy = filteredRoadList[i];

                    //Debug.Log("[DEBUG] CopyRoads: " + lbRoadToCopy.roadName + " points: " + lbRoadToCopy.centreSpline.Length);

                    if (lbRoadToCopy.isSelected)
                    {
                        lbGroupMember = null;
                        addNew = false;

                        // Validate that road has a matching left and right spline, else turn off useWidth.
                        int numPathPoints = lbRoadToCopy.centreSpline == null ? 0 : lbRoadToCopy.centreSpline.Length;
                        useWidth = lbRoadToCopy.leftSpline != null && lbRoadToCopy.rightSpline != null && lbRoadToCopy.centreSpline != null && numPathPoints == lbRoadToCopy.leftSpline.Length;                        

                        if (isUpdateExisting)
                        {
                            // Find a matching group member
                            lbGroupMember = lbGroupTarget.groupMemberList.Find(gm => gm.lbMemberType == LBGroupMember.LBMemberType.ObjPath && gm.lbObjPath != null && gm.lbObjPath.pathName == lbRoadToCopy.roadName);
                            if (lbGroupMember != null)
                            {
                                //Debug.Log("[DEBUG] found " + lbGroupMember.lbObjPath.pathName);

                                // Clear out the old points data
                                lbGroupMember.lbObjPath.showPathInScene = false;
                                lbGroupMember.lbObjPath.selectedList.Clear();
                                lbGroupMember.lbObjPath.positionList.Clear();
                                lbGroupMember.lbObjPath.pathPointList.Clear();
                                if (lbGroupMember.lbObjPath.widthList != null) { lbGroupMember.lbObjPath.widthList.Clear(); }
                                else { lbGroupMember.lbObjPath.widthList = new List<float>(numPathPoints); }

                                // If useWidth was enabled but now shouldn't be, turn it off
                                if (lbGroupMember.lbObjPath.useWidth && !useWidth)
                                {
                                    // Disable use width
                                    lbGroupMember.lbObjPath.EnablePathWidth(false, true);
                                }
                            }
                            //else { Debug.Log("[DEBUG] " + lbRoadToCopy.roadName + " not found"); }
                        }

                        // Create a new Group Member if required
                        if (lbGroupMember == null)
                        {
                            lbGroupMember = new LBGroupMember();
                            lbGroupMember.lbMemberType = LBGroupMember.LBMemberType.ObjPath;
                            lbGroupMember.lbObjPath = new LBObjPath();
                            if (lbGroupMember.lbObjPath != null)
                            {
                                lbGroupMember.lbObjPath.pathName = lbRoadToCopy.roadName;
                                lbGroupMember.lbObjPath.useWidth = useWidth;
                                lbGroupMember.lbObjPath.showDefaultSeriesInEditor = false;
                                addNew = true;
                            }
                        }

                        if (lbGroupMember != null)
                        {
                            if (lbGroupMember.lbObjPath != null)
                            {
                                lbGroupMember.showInEditor = false;                                

                                //Debug.Log("[DEBUG] CopyRoads: " + lbRoadToCopy.roadName + " points: " + numPathPoints);

                                lbGroupMember.lbObjPath.positionList.AddRange(lbRoadToCopy.centreSpline);
                                if (useWidth)
                                {
                                    lbGroupMember.lbObjPath.positionListLeftEdge.AddRange(lbRoadToCopy.leftSpline);
                                    lbGroupMember.lbObjPath.positionListRightEdge.AddRange(lbRoadToCopy.rightSpline);
                                }

                                for (int ptIdx = 0; ptIdx < numPathPoints; ptIdx++)
                                {
                                    lbGroupMember.lbObjPath.pathPointList.Add(new LBPathPoint());
                                    if (lbGroupMember.lbObjPath.useWidth)
                                    {
                                        // Calculate the 2D distance between the left and right splines. Currently Object Paths are flat
                                        // although they will support this in the future. Currently the Object Path rotation is only for objects on the path.
                                        leftSplinePt2D.x = lbRoadToCopy.leftSpline[ptIdx].x;
                                        leftSplinePt2D.y = lbRoadToCopy.leftSpline[ptIdx].z;

                                        rightSplinePt2D.x = lbRoadToCopy.rightSpline[ptIdx].x;
                                        rightSplinePt2D.y = lbRoadToCopy.rightSpline[ptIdx].z;

                                        lbGroupMember.lbObjPath.widthList.Add(Vector2.Distance(leftSplinePt2D, rightSplinePt2D));
                                    }
                                }

                                lbGroupMember.lbObjPath.showPathInScene = true;

                                if (addNew) { lbGroupTarget.groupMemberList.Add(lbGroupMember); }

                                //Debug.Log("[DEBUG] " + lbGroupTarget.groupName);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Select or unselect all the filtered roads
        /// </summary>
        /// <param name="isSelected"></param>
        private void SelectFilteredRoads(bool isSelected)
        {
            if (filteredRoadList == null) { filteredRoadList = new List<LBRoad>(numAllRoads + 20); }
            for (int i = 0; i < numFilteredRoads; i++)
            {
                filteredRoadList[i].isSelected = isSelected;
            }
        }

        /// <summary>
        /// Populate the filtered list based on the user-settings
        /// </summary>
        private void GetFilteredRoadList()
        {
            // If this method is called before the roadList is populated, it
            // may still be null.
            if (roadList == null) { roadList = new List<LBRoad>(50); }

            if (filteredRoadList == null) { filteredRoadList = new List<LBRoad>(numAllRoads+20); }
            else { filteredRoadList.Clear(); }

            if (isFilterByRoadType)
            {
                string roadTypeDesc = string.Empty;
                for (int r = 0; r < numAllRoads; r++)
                {
                    roadTypeDesc = roadList[r].roadTypeDesc;
                    if (roadTypeList.Exists(typ => typ.roadTypeDesc == roadTypeDesc && typ.isSelected && !string.IsNullOrEmpty(roadTypeDesc)))
                    {
                        filteredRoadList.Add(roadList[r]);
                    }
                }
            }
            else
            {
                filteredRoadList.AddRange(roadList);
            }

            numFilteredRoads = filteredRoadList == null ? 0 : filteredRoadList.Count;
        }

        /// <summary>
        /// Build a list of the Group names in the landscape
        /// </summary>
        private void GetGroupNameList()
        {
            if (groupNameList == null) { groupNameList = new List<string>(20); }
            else { groupNameList.Clear(); }

            LBGroup lbGroup = null;

            if (landscape != null)
            {
                int numGroups = landscape.lbGroupList == null ? 0 : landscape.lbGroupList.Count;

                for (int gIdx = 0; gIdx < numGroups; gIdx++)
                {
                    lbGroup = landscape.lbGroupList[gIdx];

                    if (lbGroup.lbGroupType == LBGroup.LBGroupType.Uniform)
                    {
                        groupNameList.Add("Group " + (gIdx+1) + " " + lbGroup.groupName);
                    }
                }
            }

            // Refresh the number of names in the list for faster lookup
            numGroupNames = groupNameList == null ? 0 : groupNameList.Count;
        }

        #endregion
    }

    #region LBRoadType class

    /// <summary>
    /// Currently only used in LBPathImporter.cs
    /// </summary>
    public class LBRoadType
    {
        public bool isSelected;
        public string roadTypeDesc;
        
        public LBRoadType()
        {
            isSelected = false;
            roadTypeDesc = string.Empty;
        }
    }
    #endregion
}
