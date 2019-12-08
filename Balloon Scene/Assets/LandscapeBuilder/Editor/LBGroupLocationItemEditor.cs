using UnityEngine;
using UnityEditor;
using System.Collections;

namespace LandscapeBuilder
{
    [CustomEditor(typeof(LBGroupLocationItem))]
    public class LBGroupLocationItemEditor : Editor
    {
        #region Private variables
        private LBGroupLocationItem lbGroupLocationItem;
        private LBGroup lbGroup;
        private LBLandscape landscape;
        private Rect landscapeBounds;
        private Vector3 prevPosition = Vector3.zero;
        private Vector3 tryPosition = Vector3.zero;
        #endregion

        #region Event Methods

        private void OnEnable()
        {
            lbGroupLocationItem = (LBGroupLocationItem)target;
            landscape = lbGroupLocationItem.transform.GetComponentInParent<LBLandscape>();
            landscapeBounds = landscape.GetLandscapeWorldBoundsFast();
            prevPosition = lbGroupLocationItem.transform.position;
            if (lbGroupLocationItem.lbGroup != null && lbGroupLocationItem.lbGroup.isFixedRotation)
            {
                lbGroupLocationItem.rotationY = lbGroupLocationItem.transform.rotation.eulerAngles.y;
                // When first selected, update the rotation in the LB editor
                LBEditorHelper.RepaintEditorWindow(typeof(LandscapeBuilderWindow));
            }
            //Debug.Log("WorldBounds: " + landscapeBounds.xMin + "," + landscapeBounds.yMin + " - " + landscapeBounds.xMax + "," + landscapeBounds.yMax);
        }

        private void OnDisable()
        {
            LBEditorHelper.RepaintEditorWindow(typeof(LandscapeBuilderWindow));
        }

        private void OnSceneGUI()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { return; }

            Event current = Event.current;

            if (current != null)
            {
                if (lbGroupLocationItem == null) { return; }
                else if (lbGroup == null) { lbGroup = lbGroupLocationItem.lbGroup; if (lbGroup == null) { return; } }

                bool isLeftButton = (current.button == 0);
                bool isRightButton = (current.button == 1);

                tryPosition = lbGroupLocationItem.transform.position;

                // Clamp position of clearing
                if (tryPosition.x < landscapeBounds.xMin || tryPosition.x > landscapeBounds.xMax || tryPosition.z < landscapeBounds.yMin || tryPosition.z > landscapeBounds.yMax)
                {
                    lbGroupLocationItem.transform.position = prevPosition;
                }

                // Update prevPostion with the current position
                prevPosition = lbGroupLocationItem.transform.position;

                prevPosition.y = LBLandscapeTerrain.GetHeight(landscape, new Vector2(prevPosition.x, prevPosition.z), false) + landscape.start.y;
                // Snap the clearing location to the terrain height at this point
                lbGroupLocationItem.transform.position = prevPosition;

                if (current.type == EventType.MouseDown && isRightButton)
                {
                    #region Display the Context-sensitive menu
                    // NOTE: The Context Menu that is display when a location is NOT selected, can be found
                    // in LandscapeBuilderWindow.SceneGUI(SceneView sv)

                    GenericMenu menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Delete Postion"), false, DeleteLocation);
                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Delete ALL"), false, DeleteAll);
                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Unselect"), false, () => { Selection.activeObject = null; });
                    // The Cancel option is not really necessary as use can just click anywhere else. However, it may help some users.
                    menu.AddItem(new GUIContent("Cancel"), false, () => { });
                    menu.ShowAsContext();
                    current.Use();
                    #endregion
                }
                // Record the starting positions 
                else if (current.type == EventType.MouseDown && isLeftButton && landscape != null)
                {
                    lbGroupLocationItem.position = lbGroupLocationItem.transform.position - landscape.start;
                    lbGroupLocationItem.rotationY = lbGroupLocationItem.transform.rotation.eulerAngles.y;
                }
                else if (current.type == EventType.MouseUp && isLeftButton)
                {
                    if (lbGroup.isFixedRotation)
                    {
                        #if UNITY_2017_3_OR_NEWER
                        if (Tools.current == Tool.Rotate || Tools.current == Tool.Transform)
                        #else
                        if (Tools.current == Tool.Rotate)
                        #endif
                        {
                            // Locate the first matching clearing position
                            // Only check x,z axis as the y-axis may be slightly wrong
                            int idx = lbGroup.positionList.FindIndex(pos => pos.x == lbGroupLocationItem.position.x && pos.z == lbGroupLocationItem.position.z);
                            
                            if (idx > -1)
                            {
                                // update with the new rotation
                                lbGroupLocationItem.rotationY = lbGroupLocationItem.transform.rotation.eulerAngles.y;
                                lbGroup.rotationYList[idx] = lbGroupLocationItem.rotationY;
                                lbGroupLocationItem.transform.rotation = Quaternion.Euler(0f, lbGroupLocationItem.rotationY, 0f);
                                // Update the LB Editor Windows
                                LBEditorHelper.RepaintEditorWindow(typeof(LandscapeBuilderWindow));
                            }
                        }
                    }


                    #if UNITY_2017_3_OR_NEWER
                    if ((Tools.current == Tool.Move || Tools.current == Tool.Transform) && landscape != null)
                    #else
                    if (Tools.current == Tool.Move && landscape != null)
                    #endif
                    {
                        // Locate the first matching clearing position
                        // Only check x,z axis as the y-axis may be slightly wrong
                        int idx = lbGroup.positionList.FindIndex(pos => pos.x == lbGroupLocationItem.position.x && pos.z == lbGroupLocationItem.position.z);

                        if (idx > -1)
                        {
                            // update it with the new position
                            lbGroup.positionList[idx] = lbGroupLocationItem.transform.position - landscape.start;
                        }
                    }

                    #if UNITY_2017_3_OR_NEWER
                    if (Tools.current == Tool.Scale || Tools.current == Tool.Transform)
                    #else
                    if (Tools.current == Tool.Scale)
                    #endif
                    {
                        lbGroup.minClearingRadius = lbGroupLocationItem.transform.localScale.x / 2f;
                        lbGroup.maxClearingRadius = lbGroup.minClearingRadius;

                        // Update the LB Editor Windows
                        LBEditorHelper.RepaintEditorWindow(typeof(LandscapeBuilderWindow));

                        // Resize all the locations in the scene
                        LBGroupLocationItem.RefreshLocationSizesInScene(landscape, lbGroup, true);
                    }
                }

                #region Changed Rotation
                
                // Clamp rotation
                if (!lbGroup.isFixedRotation) { lbGroupLocationItem.transform.rotation = Quaternion.identity; }
                //else
                //{
                //    Vector3 eulerAngles = lbGroupLocationItem.transform.rotation.eulerAngles;
                //    if (eulerAngles.x != 0f || eulerAngles.z != 0f)
                //    {
                //        //lbGroupLocationItem.transform.rotation = Quaternion.Euler(0f, lbGroupLocationItem.rotationY, 0f);
                //    }
                //}
                #endregion

                #region Changed Radius
#if UNITY_2017_3_OR_NEWER
                if (Tools.current == Tool.Scale || Tools.current == Tool.Transform)
#else
                if (Tools.current == Tool.Scale)
                #endif
                {
                    // Equally scale x-z axis                 
                    float currentScale = lbGroup.minClearingRadius * 2f;
                    float maxScale = -currentScale;
                    Vector3 localScale = lbGroupLocationItem.transform.localScale;

                    // Delta = Abs(localScale.) - currentScale
                    float deltaX = (localScale.x < 0f ? -localScale.x : localScale.x);
                    float deltaY = (localScale.y < 0f ? -localScale.y : localScale.y);
                    float deltaZ = (localScale.z < 0f ? -localScale.z : localScale.z);

                    // Get the max scale amount of any of the axis
                    if (deltaX != currentScale && deltaX > maxScale) { maxScale = localScale.x; }
                    if (deltaZ != currentScale && deltaZ > maxScale) { maxScale = localScale.z; }

                    // Did the user change the scale?
                    if (maxScale != -currentScale)
                    {
                        // Clamp scaling to 0.2
                        if (maxScale < 0.2f) { maxScale = 0.2f; }

                        localScale.x = maxScale;

                        // Make x-z axis the same
                        localScale.z = localScale.x;

                        // Don't change y-axis
                        localScale.y = 0.1f;

                        lbGroupLocationItem.transform.localScale = localScale;
                    }
                    else if (deltaY > 0f)
                    {
                        // Local Y axis scaling
                        localScale.y = 0.1f;
                        lbGroupLocationItem.transform.localScale = localScale;
                    }
                }
                #endregion
            }
        }

        #endregion

        #region Private Non-Static Methods

        private void DeleteLocation()
        {
            if (lbGroup != null && landscape != null)
            {
                if (lbGroup.positionList != null)
                {
                    // Find the location in the list and remove it
                    int idx = lbGroup.positionList.FindIndex(pos => pos == lbGroupLocationItem.transform.position - landscape.start);

                    if (idx > -1)
                    {
                        //Debug.Log("Delete location at " + lbGroupLocationItem.transform.position);
                        lbGroup.positionList.RemoveAt(idx);
                        if (lbGroup.isFixedRotation && lbGroup.rotationYList != null) { lbGroup.rotationYList.RemoveAt(idx); }
                        DestroyImmediate(lbGroupLocationItem.gameObject);
                        LBEditorHelper.RepaintEditorWindow(typeof(LandscapeBuilderWindow));
                    }
                }
            }
        }

        private void DeleteAll()
        {
            if (lbGroup != null && lbGroupLocationItem != null)
            {
                if (lbGroup.positionList != null)
                {
                    lbGroup.positionList.Clear();
                    if (lbGroup.isFixedRotation && lbGroup.rotationYList != null) { lbGroup.rotationYList.Clear(); }
                    LBGroupLocationItem.RemoveLocationsFromScene(landscape, true);
                    LBEditorHelper.RepaintEditorWindow(typeof(LandscapeBuilderWindow));
                }
            }
        }

        #endregion
    }
}