using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace LandscapeBuilder
{
    /// <summary>
    /// Non-serializable class to pass parmeters to LBLandscapeTerrain.
    /// This enables new features to be added without breaking backward
    /// compatibility
    /// </summary>
    public class LBGroupParameters
    {
        #region Public varibles

        public LBLandscape landscape;
        public Terrain terrain;
        public bool showErrors;
        public bool showProgress;
        public LBLandscape.ShowProgressDelegate showProgressDelegate;

        public bool isGroupDesignerEnabled;

        // Will be zero when used on terrains, or BasePlaneOffsetY with the GroupDesigner
        public float designerOffsetY;

        // Used when editing a single Object Path with the ObjPathDesigner.
        public LBObjPathParameters lbObjPathParm;

        public List<LBObjectProximity> objectProximitiesList;

        #endregion

        #region Constructors

        public LBGroupParameters()
        {
            landscape = null;
            terrain = null;
            showErrors = false;
            showProgress = false;
            showProgressDelegate = null;
            isGroupDesignerEnabled = false;
            designerOffsetY = 0f;
            lbObjPathParm = null;
            objectProximitiesList = null;
        }

        #endregion
    }
}