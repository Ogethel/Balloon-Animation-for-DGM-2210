using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LandscapeBuilder
{
    /// <summary>
    /// Non-serializable class to pass parmeters to LBLandscapeTerrain.
    /// This enables new features to be added without breaking backward
    /// compatibility
    /// </summary>
    public class LBObjPathParameters
    {
        #region Public varibles

        public LBLandscape landscape;
        public LBGroup lbGroupOwner;
        public LBGroupMember lbGroupMemberOwner;    // of MemberType ObjPath
        public LBPrefabItem.PrefabItemType prefabItemType;
        public bool showErrors;
        public bool showProgress;
        public LBLandscape.ShowProgressDelegate showProgressDelegate;
        public float clearingRotationY;

        // variables used in LBLandscapeTerrain.PlaceObjOnPath()
        public LBGroupMember lbGroupMember;         // of MemberType Prefab
        public GameObject prefab;
        public Vector3 pathPoint;
        public float distanceAlongPath;
        public Transform parentTfm;
        public TerrainData[] terrainDataArray;
        public Rect[] terrainRectsArray;
        public float terrainHeight;

        #endregion

        #region Constructors

        public LBObjPathParameters()
        {
            landscape = null;
            lbGroupOwner = null;
            lbGroupMemberOwner = null;
            prefabItemType = LBPrefabItem.PrefabItemType.ObjPathPrefab;
            showErrors = false;
            showProgress = false;
            showProgressDelegate = null;
            clearingRotationY = 0f;

            lbGroupMember = null;
            prefab = null;
            pathPoint = Vector3.zero;
            distanceAlongPath = 0f;
            parentTfm = null;
            terrainDataArray = new TerrainData[0];
            terrainRectsArray = new Rect[0];
            terrainHeight = 1000f;
        }

        #endregion

    }
}