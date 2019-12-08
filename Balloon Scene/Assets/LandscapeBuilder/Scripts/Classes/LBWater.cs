using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace LandscapeBuilder
{
    [System.Serializable]
    public class LBWater
    {
        // Landscape Builder Water Class
        // This is used in the landscape.landscapeWaterList and is stored
        // in the LBLandscape script in the scene (so it can be saved with the scene)
        // The list is visible in Landscape Tab in the Editor under Scene Settings
        // Added in Version 1.2.0
        // Added AQUASLite in Version 1.2.1
        // Available for use in LBPath in Version 1.3.5

        #region Enumerations

        public enum WaterResizingMode
        {
            TransformScaling = 0,
            DuplicatingMeshes = 1,
            StandardAssets = 2,
            AQUASLite = 3,
            AQUAS = 4,
            CalmWater = 8
        }

        // Used with LBMapPath (LBPath) when a mesh
        // is created for a river surface.
        // Also used for Layer Image Modifiers.
        public enum WaterMeshResizingMode
        {
            Custom = 0,
            AQUAS = 5,
            CalmWater = 8,
            RiverAutoMaterial = 16,
            StandardAssets = 20
        }

        #endregion

        #region Variables and Properties

        public bool isPrimaryWater;
        public string waterPrefabName;
        public string waterPrefabPath;
        public float waterLevel;
        public string name;
        public string GUID;
        public Rect meshBoundsRect;
        public Vector3 meshScale;
        public WaterResizingMode resizingMode;
        public Vector2 waterSize;
        public bool keepPrefabAspectRatio;

        public List<LBWaterCaustics> waterCausticList;

        // Secondary water bodies (lakes)
        public Vector3 waterPosition;

        // Added v1.3.0 Beta 1d
        public bool isDisabled;

        // Added v1.3.2 Beta 5a
        public bool isRiver;
        public Material riverMaterial;
        public Texture2D flowMapTexture;
        public float riverFlowSpeed;
        public bool isUnderWaterFXEnabled;
        public string underWaterFXPrefabName;
        public string underWaterFXPrefabPath;

        // Add v1.3.6 Beta 2b to support CalmWater oceans, lakes etc.
        public Material waterMaterial;

        // Added v1.3.5 Beta 4e
        public WaterMeshResizingMode meshResizingMode;

        #endregion

        #region Constructors

        // Constructors
        public LBWater()
        {
            // Assign a unique identifier
            GUID = System.Guid.NewGuid().ToString();

            // Assign default values
            waterLevel = 0f;
            waterPosition = Vector3.zero;
            waterSize = new Vector2(100f, 100f);
            keepPrefabAspectRatio = false;
            name = "secondary water";
            isPrimaryWater = false;
            waterPrefabName = "unknown";
            waterPrefabPath = string.Empty;
            meshBoundsRect = new Rect();
            meshScale = new Vector3(1f, 1f, 1f);
            resizingMode = WaterResizingMode.TransformScaling;
            meshResizingMode = WaterMeshResizingMode.Custom;
            waterCausticList = null;
            isDisabled = false;
            isRiver = false;
            riverMaterial = null;
            flowMapTexture = null;
            riverFlowSpeed = 1.5f;
            isUnderWaterFXEnabled = false;
            underWaterFXPrefabName = "unknown";
            underWaterFXPrefabPath = string.Empty;
            waterMaterial = null;
        }

        /// <summary>
        /// Clone constructor
        /// </summary>
        public LBWater(LBWater lbWater, bool useNewGUID)
        {
            // Assign a unique identifier
            if (useNewGUID) { GUID = System.Guid.NewGuid().ToString(); }
            else { this.GUID = lbWater.GUID; }

            // Assign default values
            waterLevel = lbWater.waterLevel;
            waterPosition = lbWater.waterPosition;
            waterSize = lbWater.waterSize;
            keepPrefabAspectRatio = lbWater.keepPrefabAspectRatio;
            name = lbWater.name;
            isPrimaryWater = lbWater.isPrimaryWater;
            waterPrefabName = lbWater.waterPrefabName;
            waterPrefabPath = lbWater.waterPrefabPath;
            meshBoundsRect = lbWater.meshBoundsRect;
            meshScale = lbWater.meshScale;
            resizingMode = lbWater.resizingMode;
            meshResizingMode = lbWater.meshResizingMode;
            if (waterCausticList != null) { waterCausticList = new List<LBWaterCaustics>(lbWater.waterCausticList); } else { waterCausticList = null; }
            isDisabled = lbWater.isDisabled;
            isRiver = lbWater.isRiver;
            riverMaterial = lbWater.riverMaterial;
            flowMapTexture = lbWater.flowMapTexture;
            riverFlowSpeed = lbWater.riverFlowSpeed;
            isUnderWaterFXEnabled = lbWater.isUnderWaterFXEnabled;
            underWaterFXPrefabName = lbWater.underWaterFXPrefabName;
            underWaterFXPrefabPath = lbWater.underWaterFXPrefabPath;

            // Changed v1.4.4 (To create a true clone we should create a new copy of the material)
            // This will break any links with the original material. Not sure if we want to do this...
            //waterMaterial = lbWater.waterMaterial;
            if (waterMaterial != null) { waterMaterial = new Material(lbWater.waterMaterial); } else { waterMaterial = null; }
        }

        #endregion
    }
}