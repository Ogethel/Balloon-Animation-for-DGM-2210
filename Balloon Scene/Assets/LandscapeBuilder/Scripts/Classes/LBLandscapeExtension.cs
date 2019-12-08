using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace LandscapeBuilder
{
    [System.Serializable]
    public class LBLandscapeExtension
    {
        #region Public variables and properties
        // IMPORTANT When changing this section update:
        // SetClassDefaults() and LBLandscapeExtension(LBLandscapeExtension lbLandscapeExtension) clone constructor
        public float heightOffset;  // in metres
        public Texture2D texture;
        public Texture2D normalMap;
        // Last known texture name. Used to help detect missing textures.
        public string textureName;
        public string normalMapName;
        public Vector2 tileSize;
        public float metallic;
        public float smoothness;

        #endregion

        #region Private variables and properties

        #endregion

        #region Constructors

        // Basic class constructor
        public LBLandscapeExtension()
        {
            SetClassDefaults();
        }

        // Clone constructor
        public LBLandscapeExtension(LBLandscapeExtension lbLandscapeExtension)
        {
            if (lbLandscapeExtension == null) { SetClassDefaults(); }
            else
            {
                this.heightOffset = lbLandscapeExtension.heightOffset;
                this.texture = lbLandscapeExtension.texture;
                this.normalMap = lbLandscapeExtension.normalMap;
                if (lbLandscapeExtension.textureName == null) { this.textureName = string.Empty; }
                else { this.textureName = lbLandscapeExtension.textureName; }
                if (lbLandscapeExtension.normalMapName == null) { this.normalMapName = string.Empty; }
                else { this.normalMapName = lbLandscapeExtension.normalMapName; }
                this.tileSize = lbLandscapeExtension.tileSize;
                this.smoothness = lbLandscapeExtension.smoothness;
                this.metallic = lbLandscapeExtension.metallic;
            }
        }
        #endregion

        #region Private Non-Static Methods

        /// <summary>
        /// Set the default values for a new LBGroup class instance
        /// </summary>
        private void SetClassDefaults()
        {
            this.heightOffset = 0f;
            this.texture = null;
            this.normalMap = null;
            this.tileSize = Vector2.one * 1000f;
            this.smoothness = 0f;
            this.metallic = 0f;
            this.textureName = string.Empty;
            this.normalMapName = string.Empty;
        }

        #endregion

        #region Public Non-Static Methods

        #endregion

        #region Public Static Methods

        #endregion

    }
}