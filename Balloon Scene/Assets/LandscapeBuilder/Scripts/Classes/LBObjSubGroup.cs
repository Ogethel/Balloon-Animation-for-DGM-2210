using UnityEngine;

namespace LandscapeBuilder
{
    /// <summary>
    /// Used with Group Object Paths which contain LBObjPathSeries (width-based series). These
    /// series can contain a list of prefabs OR a list of SubGroups which are spawned along
    /// a path.
    /// </summary>
    [System.Serializable]
    public class LBObjSubGroup
    {
        #region Public variables
        // IMPORTANT When changing this section update:
        // SetClassDefaults() and LBObjSubGroup(LBObjSubGroup lbObjSubGroup) clone constructor

        /// <summary>
        /// The link to the SubGroup
        /// </summary>
        public string subGroupGUID;

        #endregion

        #region Constructors

        // Standard constructor
        public LBObjSubGroup()
        {
            SetClassDefaults();
        }

        // Clone constructor
        public LBObjSubGroup(LBObjSubGroup lbObjSubGroup)
        {
            if (lbObjSubGroup == null) { SetClassDefaults(); }
            else
            {
                subGroupGUID = lbObjSubGroup.subGroupGUID;
            }
        }

        #endregion

        #region Private Member Methods

        private void SetClassDefaults()
        {
            subGroupGUID = string.Empty;
        }

        #endregion
    }
}