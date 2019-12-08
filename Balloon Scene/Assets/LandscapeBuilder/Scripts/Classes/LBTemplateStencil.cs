using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace LandscapeBuilder
{
    /// <summary>
    /// Used to store LBStencil information when saving to and restoring from a LBTemplate
    /// NOTE: Needs to be updated when the public variables of LBStencil are modified.
    /// </summary>
    [System.Serializable]
    public class LBTemplateStencil
    {

        #region Public Variables and Properties

        // These are a subset of the LBStencil public variables. Not all the variables
        // are required when moving a LBStencil using a LBTemplate
        public string stencilName = "Stencil";
        public string GUID = string.Empty;

        public List<LBStencilLayer> stencilLayerList;

        public bool showStencilSettings = true;
        [Range(1f, 500f)] public float brushSize = 50f;
        [Range(0.01f, 1f)] public float smoothStrength = 0.5f;

        #endregion

        #region Constructors

        public LBTemplateStencil()
        {

        }

        /// <summary>
        /// Create a LBTemplateStencil from a LBStencil
        /// </summary>
        /// <param name="lbStencil"></param>
        public LBTemplateStencil(LBStencil lbStencil)
        {
            this.GUID = lbStencil.GUID;
            this.stencilName = lbStencil.stencilName;

            this.stencilLayerList = new List<LBStencilLayer>();

            if (lbStencil.stencilLayerList != null)
            {
                LBStencilLayer templbStencilLayer = null;

                // Add new Stencil Layers to the LBTemplateStencil without the USHORT layerArray and renderTexture
                foreach (LBStencilLayer lbStencilLayer in lbStencil.stencilLayerList)
                {
                    // Copy the StencilLayer with the compressTexture
                    templbStencilLayer = new LBStencilLayer(lbStencilLayer, true, false, false);

                    if (templbStencilLayer != null)
                    {
                        // Copy the compressTexture data into the USHORT so that we can serialise it
                        templbStencilLayer.AllocLayerArrayX();
                        templbStencilLayer.UnCompressToUShortX();
                        // Texture2D class isn't saved with the serialised so no need to keep it.
                        templbStencilLayer.compressedTexture = null;
                    }

                    if (templbStencilLayer != null) { this.stencilLayerList.Add(templbStencilLayer); }
                }
            }

            this.showStencilSettings = lbStencil.showStencilSettings;
            this.brushSize = lbStencil.brushSize;
            this.smoothStrength = lbStencil.smoothStrength;
        }

        #endregion
    }
}