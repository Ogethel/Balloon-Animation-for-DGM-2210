using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LandscapeBuilder
{
    /// <summary>
    /// Used to create optional start/end prefabs in a LBObjPath.
    /// Also used to create a list of main prefabs to fill the path
    /// </summary>
    [System.Serializable]
    public class LBObjPrefab
    {
        #region Public variables
        // IMPORTANT When changing this section update:
        // SetClassDefaults() and LBObjPrefab(LBObjPrefab lbObjPrefab) clone constructor

        /// <summary>
        /// This is the link to the Group Member which contains most of the placement rules,
        /// for the prefab on the LBObjPath.
        /// </summary>
        public string groupMemberGUID;

        public GameObject prefab;
        // Last known prefab name. Used to help detect missing mesh prefabs.
        [System.NonSerialized] public string prefabName;
        [System.NonSerialized] public bool showPrefabPreview;
        [System.NonSerialized] public bool isKeepPrefabConnection;
        #endregion

        #region Constructors

        // Standard constructor
        public LBObjPrefab()
        {
            SetClassDefaults();
        }

        // Clone constructor
        public LBObjPrefab(LBObjPrefab lbObjPrefab)
        {
            groupMemberGUID = lbObjPrefab.groupMemberGUID;

            prefab = lbObjPrefab.prefab;
            prefabName = lbObjPrefab.prefabName;
            showPrefabPreview = lbObjPrefab.showPrefabPreview;
            isKeepPrefabConnection = lbObjPrefab.isKeepPrefabConnection;
        }

        #endregion

        #region Private Member Methods

        private void SetClassDefaults()
        {
            groupMemberGUID = string.Empty;
            prefab = null;
            prefabName = string.Empty;
            showPrefabPreview = false;
            isKeepPrefabConnection = false;
        }

        #endregion
    }
}
