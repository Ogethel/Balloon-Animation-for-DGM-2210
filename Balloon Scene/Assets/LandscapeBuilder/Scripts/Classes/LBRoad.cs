using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace LandscapeBuilder
{
    [System.Serializable]
    public class LBRoad
    {
        #region Enumerations
        public enum SplineType
        {
            CentreSpline = 0,
            LeftSpline = 1,
            RightSpline = 2,
            MarkerSpline = 3
        }
        #endregion

        #region Public Variables
        public string roadName;
        public string roadTypeDesc;
        public float roadWidth;
        public float roadLength;
        public bool isSelected;
        public bool isReversed; // The spline points should be reversed
        public Vector3[] centreSpline;
        public Vector3[] leftSpline;
        public Vector3[] rightSpline;
        public Vector3[] markerSpline;
        #endregion

        #region Class constructors
        public LBRoad()
        {
            SetClassDefaults();
        }

        // Constructor to create clone copy
        public LBRoad(LBRoad lbRoad)
        {
            if (lbRoad == null) { SetClassDefaults(); }
            else
            {
                isSelected = lbRoad.isSelected;
                isReversed = lbRoad.isReversed;
                roadName = lbRoad.roadName;
                roadTypeDesc = lbRoad.roadTypeDesc;
                roadWidth = lbRoad.roadWidth;
                roadLength = lbRoad.roadLength;
                centreSpline = lbRoad.centreSpline;
                leftSpline = lbRoad.leftSpline;
                rightSpline = lbRoad.rightSpline;
                markerSpline = lbRoad.markerSpline;
            }
        }

        #endregion

        #region Private Methods

        private void SetClassDefaults()
        {
            isSelected = false;
            isReversed = false;
            roadName = string.Empty;
            roadTypeDesc = string.Empty;
            roadWidth = 0f;
            roadLength = 0f;
            // may not be able to serialize nulls..
            centreSpline = new Vector3[1];
            centreSpline[0] = Vector3.zero;
            leftSpline = new Vector3[1];
            leftSpline[0] = Vector3.zero;
            rightSpline = new Vector3[1];
            rightSpline[0] = Vector3.zero;
            markerSpline = new Vector3[1];
            markerSpline[0] = Vector3.zero;
        }

        #endregion
    }
}