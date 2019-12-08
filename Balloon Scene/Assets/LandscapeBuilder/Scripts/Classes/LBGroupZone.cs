using System.Collections.Generic;
using UnityEngine;

namespace LandscapeBuilder
{
    /// <summary>
    /// A region or "zone" within a Group Clearing where items can be included in or excluded from.
    /// Zones can be circular or rectangular. The centrePoint (X and Z) is normalised so that it will
    /// always appear in the same relative location within the group clearing.
    /// See also LBGroup.zoneList.
    /// </summary>
    [System.Serializable]
    public class LBGroupZone
    {
        #region Enumerations

        public enum LBGroupZoneType
        {
            circle = 3,
            rectangle = 5
        }

        // Currently no need for an AND mode
        public enum ZoneMode
        {
            OR = 1,
            NOT = 2
        }

        #endregion

        #region Public variables

        public LBGroupZoneType zoneType;
        public ZoneMode zoneMode;
        public string zoneName;
        public string GUID;
        // Normalised centre point of the zone
        [Range(-1f, 1f)] public float centrePointX;
        [Range(-1f, 1f)] public float centrePointZ;

        // For circular zones width = length (normalised)
        // Circles will be 0-1, and rectangles will be 0-2 as the
        // width and length can be up to twice the radius.
        [Range(0.0001f, 2f)] public float width;
        [Range(0.0001f, 2f)] public float length;

        // When the radius of the Group changes, should the centre point, width and/or length scale too?
        public bool isScaledPointX;
        public bool isScaledPointZ;
        public bool isScaledWidth;
        public bool isScaledLength;

        public bool showInEditor;
        public bool showInScene;

        public bool useBiome;
        public LBBiome lbBiome;

        // Used in the Group Designer to track changes
        [System.NonSerialized] public bool isMoving;
        [System.NonSerialized] public float currentCentrePointX;
        [System.NonSerialized] public float currentCentrePointZ;
        [System.NonSerialized] public float currentZoneWidth;
        [System.NonSerialized] public float currentZoneLength;

        #endregion

        #region Constructors

        // Basic class constructor
        public LBGroupZone()
        {
            SetClassDefaults();
        }

        // Clone constructor
        public LBGroupZone(LBGroupZone lbGroupZone)
        {
            if (lbGroupZone == null) { SetClassDefaults(); }
            else
            {
                zoneType = lbGroupZone.zoneType;
                zoneMode = lbGroupZone.zoneMode;
                zoneName = lbGroupZone.zoneName;
                GUID = lbGroupZone.GUID;
                centrePointX = lbGroupZone.centrePointX;
                centrePointZ = lbGroupZone.centrePointZ;
                width = lbGroupZone.width;
                length = lbGroupZone.length;
                isScaledPointX = lbGroupZone.isScaledPointX;
                isScaledPointZ = lbGroupZone.isScaledPointZ;
                isScaledWidth = lbGroupZone.isScaledWidth;
                isScaledLength = lbGroupZone.isScaledLength;
                showInScene = lbGroupZone.showInScene;
                showInEditor = lbGroupZone.showInEditor;
                useBiome = lbGroupZone.useBiome;
                lbBiome = lbGroupZone.lbBiome;
            }
        }

        #endregion

        #region Non-Static Private Methods

        private void SetClassDefaults()
        {
            zoneType = LBGroupZoneType.circle;
            zoneMode = ZoneMode.OR;
            zoneName = "new group zone";
            // Assign a unique identifier
            GUID = System.Guid.NewGuid().ToString();

            centrePointX = 0f;
            centrePointZ = 0f;
            width = 10f;
            length = 10f;
            isScaledPointX = true;
            isScaledPointZ = true;
            isScaledWidth = true;
            isScaledLength = true;
            showInScene = true;
            showInEditor = true;
            useBiome = false;
            // This probably has little or no effect as it will always create
            // a default LBBiome as serializer cannot store nulls.
            lbBiome = null;
        }

        #endregion

        #region Public Non-Static Methods

        /// <summary>
        /// Does member match the search criteria.
        /// If the search string is empty, always return true.
        /// This is used in the LB Editor window.
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public bool IsInSearchFilter(string searchFilter)
        {
            // If the search string is empty, always return true.
            if (string.IsNullOrEmpty(searchFilter)) { return true; }
            else
            {
                return (!string.IsNullOrEmpty(zoneName) && zoneName.ToLower().Contains(searchFilter.ToLower()));
            }
        }


        #endregion
    }
}