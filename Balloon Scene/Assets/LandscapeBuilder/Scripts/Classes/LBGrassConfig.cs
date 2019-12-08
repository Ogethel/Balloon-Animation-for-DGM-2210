using UnityEngine;
using System.Collections;

namespace LandscapeBuilder
{
    [System.Serializable]
    public class LBGrassConfig
    {
        /// <summary>
        /// LBGrassConfig stores the configuation for individual grass textures
        /// that are associated with Texture2D assets in a Unity project. Examples
        /// include Texture2D assets from HQ Photographic Textures Grass Pack Vol. 1
        /// </summary>

        // This needs to match up with the enum of the same
        // name in LBTerrainGrass.cs
        public enum GrassPatchFadingMode
        {
            DontFade = 0,
            Fade = 1
        }

        public string grassTextureName;
        public string grassTexturePath;
        public string grassTextureAlternativePath;
        public string sourceName;    // e.g. Unity, HQ Photographic Textures Grass Pack Vol. 1 or 2
        public GrassPatchFadingMode grassPatchFadingMode;
        public DetailRenderMode detailRenderMode;

        // Color is not serializable. With BinaryFormatter must use POD types
        // We could use a .NET SurogateSelector class but in this case just use POD types

        public float dryColourRed;
        public float dryColourGreen;
        public float dryColourBlue;
        public float dryColourAlpha;

        public float healthyColourRed;
        public float healthyColourGreen;
        public float healthyColourBlue;
        public float healthyColourAlpha;

        public float minHeight;
        public float maxHeight;
        public float minWidth;
        public float maxWidth;

        public Color dryColour
        {
            get { return new Color(dryColourRed, dryColourGreen, dryColourBlue, dryColourAlpha); }
            set { dryColourRed = value.r; dryColourGreen = value.g; dryColourBlue = value.b; dryColourAlpha = value.a; }
        }

        public Color healthyColour
        {
            get { return new Color(healthyColourRed, healthyColourGreen, healthyColourBlue, healthyColourAlpha); }
            set { healthyColourRed = value.r; healthyColourGreen = value.g; healthyColourBlue = value.b; healthyColourAlpha = value.a; }
        }

        // Texture2D is non-serializable and will prevent this class from being
        // serialised with the BinaryFormatter.
        [System.NonSerialized] public Texture2D texture2D;

        // This alternative image can be used to store real-life image of grass
        [System.NonSerialized] public Texture2D texture2DAlternative;

        // Basic constructor
        public LBGrassConfig()
        {

        }

        /// <summary>
        /// Constructor to create clone copy
        /// </summary>
        /// <param name="lbGrassConfig"></param>
        public LBGrassConfig(LBGrassConfig lbGrassConfig)
        {
            grassTextureName = lbGrassConfig.grassTextureName;
            grassTexturePath = lbGrassConfig.grassTexturePath;
            grassTextureAlternativePath = lbGrassConfig.grassTextureAlternativePath;
            sourceName = lbGrassConfig.sourceName;
            grassPatchFadingMode = lbGrassConfig.grassPatchFadingMode;
            detailRenderMode = lbGrassConfig.detailRenderMode;
            dryColour = lbGrassConfig.dryColour;
            healthyColour = lbGrassConfig.healthyColour;
            minHeight = lbGrassConfig.minHeight;
            maxHeight = lbGrassConfig.maxHeight;
            minWidth = lbGrassConfig.minWidth;
            maxWidth = lbGrassConfig.maxWidth;
        }
    }
}

