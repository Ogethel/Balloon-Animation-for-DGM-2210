using UnityEngine;
using System.Collections.Generic;

namespace LandscapeBuilder
{
    /// <summary>
    /// Placed in the scene at design time to track where (Manual) Clearings should
    /// be located. Should be removed from scene after the LBGroup.positionList is updated.
    /// Used to assist selection in the scene view.
    /// </summary>
    [ExecuteInEditMode]
    public class LBGroupLocationItem : MonoBehaviour
    {
        #region Public variables

        [HideInInspector] public LBGroup lbGroup;
        [HideInInspector] public Vector3 position;
        [HideInInspector] public float rotationY;

        #endregion

        #region Private Variables


        #endregion

#if UNITY_EDITOR

        #region Private Editor Variables
        private bool isInitialised = false;
        private int numZones = 0;
        //private UnityEngine.Color zoneLabelColour = new Color(47f / 255f, 79f / 255f, 79f / 255f, 1.0f); // Dark Slate Gray
        private UnityEngine.Color zoneLabelColour = Color.black;
        private GUIStyle zoneLabelStyle = null;

        #endregion

        #region Event Methods

        // Draw gizmos whenever the designer is being shown in the scene
        private void OnDrawGizmos()
        {
            if (isInitialised && lbGroup != null)
            {
                // Show the clearing location as a 2D circle
                UnityEditor.Handles.color = Color.blue;
                Vector3 locPos = transform.position;
                UnityEditor.Handles.DrawWireDisc(locPos, Vector3.up, lbGroup.maxClearingRadius);

                //Vector3 arcPosition = this.transform.position;
                //UnityEditor.Handles.DrawWireArc(arcPosition, Vector3.up, this.transform.forward, 360, lbGroup.maxClearingRadius);

                // Show the zones                            
                using (new UnityEditor.Handles.DrawingScope(Color.cyan))
                {
                    Vector3 areaPos = new Vector3(0f, locPos.y, 0f);

                    if (zoneLabelStyle == null)
                    {
                        zoneLabelStyle = new GUIStyle("Box");
                        zoneLabelStyle.fontSize = 14;
                        zoneLabelStyle.border = new RectOffset(2, 2, 2, 2);
                        GUI.skin.box.normal.textColor = zoneLabelColour;
                        zoneLabelStyle.onFocused.textColor = zoneLabelColour;
                    }

                    // Show zones in scene for the current manual clearing location
                    for (int zoneIdx = 0; zoneIdx < numZones; zoneIdx++)
                    {
                        LBGroupZone lbGroupZone = lbGroup.zoneList[zoneIdx];

                        if (lbGroupZone.showInScene)
                        {
                            areaPos.x = (lbGroupZone.centrePointX * lbGroup.maxClearingRadius) + locPos.x;
                            areaPos.z = (lbGroupZone.centrePointZ * lbGroup.maxClearingRadius) + locPos.z;

                            LBGroupDesigner.DrawZone(lbGroupZone, areaPos, locPos, lbGroup.maxClearingRadius, areaPos.y, rotationY, false, zoneLabelStyle);
                        }
                    }
                }
            }
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Remove any existing Location objects from the landscape. Only add back in ones from the selected group.
        /// This assumes only one group can be displayed for the current landscape. 
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="lbGroupLocation"></param>
        /// <param name="showErrors"></param>
        public static void RefreshLocationsInScene(LBLandscape landscape, LBGroup lbGroupLocation, Material locationMaterial, bool showErrors)
        {
            string methodName = "LBGroupLocationItem.RefreshLocationsInScene";

            // Basic validation
            if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " landscape is null"); } }
            else
            {
                RemoveLocationsFromScene(landscape, showErrors);

                if (lbGroupLocation == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " lbGroupLocation is null"); } }
                else
                {
                    int numLocations = (lbGroupLocation.positionList == null ? 0 : lbGroupLocation.positionList.Count);

                    for (int i = 0; i < numLocations; i++)
                    {
                        CreateLocationItemInScene(landscape, lbGroupLocation, locationMaterial, lbGroupLocation.positionList[i] + landscape.start, lbGroupLocation.isFixedRotation ? lbGroupLocation.rotationYList[i] : 0f, i + 1, showErrors);
                    }
                }
            }
        }

        /// <summary>
        /// Create and initialise a location of a manual group in the scene.
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="lbGroupLocation"></param>
        /// <param name="locationMaterial"></param>
        /// <param name="locationPos"></param>
        /// <param name="locationYRotation"></param>
        /// <param name="locationNumber"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        private static LBGroupLocationItem CreateLocationItemInScene(LBLandscape landscape, LBGroup lbGroupLocation, Material locationMaterial, Vector3 locationPos, float locationYRotation, int locationNumber, bool showErrors)
        {
            LBGroupLocationItem lbGroupLocationItem = null;
            string methodName = "LBGroupLocationItem.CreateLocationItemInScene";

            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            if (cylinder != null)
            {
                lbGroupLocationItem = cylinder.AddComponent<LBGroupLocationItem>();
                if (lbGroupLocationItem == null)
                {
                    DestroyImmediate(cylinder);
                    if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " could not add LBGroupLocationItem to location gameobject. Please Report."); }
                }
                else
                {                  
                    lbGroupLocationItem.lbGroup = lbGroupLocation;
                    cylinder.name = lbGroupLocation.groupName + "_loc" + locationNumber.ToString("0000");
                    cylinder.transform.SetPositionAndRotation(locationPos, Quaternion.Euler(0f, locationYRotation, 0f));
                    cylinder.transform.localScale = new Vector3(lbGroupLocation.maxClearingRadius * 2f, 0.1f, lbGroupLocation.maxClearingRadius * 2f);
                    MeshRenderer mRen = cylinder.GetComponent(typeof(MeshRenderer)) as MeshRenderer;
                    if (mRen != null)
                    {
                        // Disable casting and receive shadows
                        mRen.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        mRen.receiveShadows = false;
                        if (locationMaterial != null) { mRen.sharedMaterial = locationMaterial; }
                    }

                    // Remove capsule collider
                    Component collider = cylinder.GetComponent(typeof(Collider));
                    if (collider != null) { DestroyImmediate(collider); }

                    cylinder.transform.SetParent(landscape.transform);

                    lbGroupLocationItem.rotationY = locationYRotation;
                    // Get the number of zones so that they can be shown in OnDrawGizmos
                    lbGroupLocationItem.numZones = lbGroupLocation.zoneList == null ? 0 : lbGroupLocation.zoneList.Count;

                    lbGroupLocationItem.isInitialised = true;
                }
            }
            return lbGroupLocationItem;
        }

        /// <summary>
        /// Find all the manual clearing location objects in the scene, filter them by a group, and set their size.
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="lbGroupLocation"></param>
        /// <param name="showErrors"></param>
        public static void RefreshLocationSizesInScene(LBLandscape landscape, LBGroup lbGroupLocation, bool showErrors)
        {
            string methodName = "LBGroupLocationItem.RefreshLocationSizesInScene";

            // Basic validation
            if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " landscape is null"); } }
            else
            {
                if (lbGroupLocation == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " lbGroupLocation is null"); } }
                else
                {
                    List<LBGroupLocationItem> lbGroupLocationItemList = new List<LBGroupLocationItem>();

                    landscape.GetComponentsInChildren(lbGroupLocationItemList);

                    // Update the size of all the current location items in the scene for this landscape
                    if (lbGroupLocationItemList != null)
                    {
                        lbGroupLocationItemList.RemoveAll(locItem => locItem.lbGroup.groupName != lbGroupLocation.groupName);

                        int numItems = lbGroupLocationItemList.Count;
                        for (int i = 0; i < numItems; i++)
                        {
                            LBGroupLocationItem lbGroupLocationItem = lbGroupLocationItemList[i];
                            lbGroupLocationItem.transform.localScale = new Vector3(lbGroupLocation.maxClearingRadius * 2f, 0.1f, lbGroupLocation.maxClearingRadius * 2f);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Remove all the Location items from the lanscape in the scene
        /// </summary>
        /// <param name="landscape"></param>
        public static void RemoveLocationsFromScene(LBLandscape landscape, bool showErrors)
        {
            string methodName = "LBGroupLocationItem.RemoveLocationsFromScene";

            // Basic validation
            if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " landscape is null"); } }
            else
            {
                List<LBGroupLocationItem> lbGroupLocationItemList = new List<LBGroupLocationItem>();

                landscape.GetComponentsInChildren(lbGroupLocationItemList);

                // Remove all the current location items from the scene for this landscape
                if (lbGroupLocationItemList != null)
                {
                    int numItems = lbGroupLocationItemList.Count;
                    for (int i = numItems - 1; i > -1; i--)
                    {
                        DestroyImmediate(lbGroupLocationItemList[i].gameObject);
                    }
                }
            }
        }

        /// <summary>
        /// Refresh the number of zones in all location items in the scene for this a group.
        /// Typically called when manual clearing groups are being edited, and the number of zones is changed.
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="lbGroupLocation"></param>
        /// <param name="showErrors"></param>
        public static void RefreshLocationZonesInScene(LBLandscape landscape, LBGroup lbGroupLocation, bool showErrors)
        {
            string methodName = "LBGroupLocationItem.RefreshLocationZonesInScene";

            // Basic validation
            if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " landscape is null"); } }
            else
            {
                if (lbGroupLocation == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " lbGroupLocation is null"); } }
                else
                {
                    List<LBGroupLocationItem> lbGroupLocationItemList = new List<LBGroupLocationItem>();

                    landscape.GetComponentsInChildren(lbGroupLocationItemList);

                    // Update the zone count for all the current location items in the scene for this group
                    if (lbGroupLocationItemList != null)
                    {
                        lbGroupLocationItemList.RemoveAll(locItem => locItem.lbGroup.GUID != lbGroupLocation.GUID);

                        int numItems = lbGroupLocationItemList.Count;
                        for (int i = 0; i < numItems; i++)
                        {
                            LBGroupLocationItem lbGroupLocationItem = lbGroupLocationItemList[i];
                            lbGroupLocationItem.numZones = lbGroupLocation.zoneList == null ? 0 : lbGroupLocation.zoneList.Count;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Add a new Location object and script to the scene. Append the location 
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="lbGroupLocation"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static LBGroupLocationItem AddNewLocationToScene(LBLandscape landscape, LBGroup lbGroupLocation, Vector2 mousePosition, Camera svCamera, bool showErrors)
        {
            LBGroupLocationItem lbGroupLocationItem = null;
            string methodName = "LBGroupLocationItem.AddNewLocationToScene";

            // Basic validation
            if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " landscape is null"); } }
            else if (lbGroupLocation == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " lbGroupLocation is null"); } }
            else
            {
                Vector3 locationPoint = Vector3.zero;

                locationPoint = LBEditorHelper.GetLandscapePositionFromMouse(landscape, mousePosition, false, true);
                if (locationPoint.x != 0 && locationPoint.z != 0)
                {
                    //Debug.Log("[DEBUG] locationPoint " + locationPoint + " mousePos: " + mousePosition);

                    Material locationMaterial = null;
                    locationMaterial = LBEditorHelper.GetMaterialFromAssets(LBSetup.materialsFolder, "LBLocation.mat");

                    lbGroupLocationItem = CreateLocationItemInScene(landscape, lbGroupLocation, locationMaterial, locationPoint, 0f, lbGroupLocation.positionList.Count + 1, showErrors);

                    if (lbGroupLocationItem != null)
                    {
                        lbGroupLocationItem.lbGroup.positionList.Add(locationPoint - landscape.start);
                        if (lbGroupLocationItem.lbGroup.isFixedRotation)
                        {
                            lbGroupLocationItem.lbGroup.rotationYList.Add(0f);
                        }
                        LBEditorHelper.RepaintLBW();
                    }
                }
            }

            return lbGroupLocationItem;
        }

        #endregion

#endif
    }
}