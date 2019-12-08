using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace LandscapeBuilder
{
    public class LBWaterParameters
    {
        // Class to pass parmeters to LBWaterOperations.AddWaterToScene()
        // This enables new features to be added without breaking backward
        // compatibility

        #region Public Variables and Properties
        public LBLandscape landscape;
        public GameObject landscapeGameObject;
        public Transform waterTransform;    // Used for adding water to an existing mesh (like MapPath river mesh or Calm Water ocean)
        public Vector3 waterPosition;
        public Vector2 waterSize;
        public bool waterIsPrimary;
        public float waterHeight;
        public Transform waterPrefab;
        public bool keepPrefabAspectRatio;
        public LBWater.WaterResizingMode waterResizingMode;
        public LBWater.WaterMeshResizingMode waterMeshResizingMode;
        public int waterMaxMeshThreshold;
        public Camera waterMainCamera;
        public List<Camera> waterCameraList;
        public List<Transform> waterCausticsPrefabList;
        public bool isRiver;                // Support for AQUAS rivers
        public Material riverMaterial;
        public Material waterMaterial;      // Used to support water assets that don't use prefabs. e.g. Calm Water oceans, lakes etc. 
        public Texture2D flowMapTexture;
        public float riverFlowSpeed;
        public bool isUnderWaterFXEnabled;
        public Transform underWaterFXPrefab;
        public string underWaterFXPrefabName;
        public string underWaterFXPrefabPath;
        public Vector3 reflectionProbePosition;
        public LBLighting lbLighting;
        #endregion

        #region Constructors
        public LBWaterParameters()
        {
            // Defaults
            landscape = null;
            landscapeGameObject = null;
            waterTransform = null;
            waterPosition = Vector3.zero;
            waterSize = Vector2.one * 100f;
            waterIsPrimary = false;
            waterHeight = 1000f;
            waterPrefab = null;
            keepPrefabAspectRatio = true;
            waterResizingMode = LBWater.WaterResizingMode.StandardAssets;
            waterMeshResizingMode = LBWater.WaterMeshResizingMode.StandardAssets;
            waterMaxMeshThreshold = 5000;
            waterMainCamera = null;
            waterCameraList = new List<Camera>();
            waterCausticsPrefabList = null;
            isRiver = false;
            riverMaterial = null;
            waterMaterial = null;
            flowMapTexture = null;
            riverFlowSpeed = 1.5f;
            isUnderWaterFXEnabled = false;
            underWaterFXPrefab = null;
            underWaterFXPrefabName = "unknown";
            underWaterFXPrefabPath = string.Empty;
            reflectionProbePosition = Vector3.zero;
            lbLighting = null;
        }
        #endregion

    }
}