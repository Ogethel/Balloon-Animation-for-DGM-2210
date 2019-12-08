using UnityEngine;
using System.Collections;

namespace LandscapeBuilder
{
    [System.Serializable]
    public class LBGradient
    {
        // Gradient class
        private static GradientColorKey[] colorKeys;

        public enum GradientPreset
        {
            DefaultAmbientLight = 0,
            DefaultFogColour = 1,
            DefaultPerlinGradient = 2,
            DefaultAmbientHznLight = 3,
            DefaultAmbientGndLight = 4
        }

        /// <summary>
        /// Returns a gradient given a preset for a gradient
        /// </summary>
        public static Gradient SetGradientFromPreset(LBGradient.GradientPreset gradientPreset)
        {
            Gradient newGradient = new Gradient();
            if (gradientPreset == GradientPreset.DefaultAmbientLight)
            {
                colorKeys = new GradientColorKey[4];
                colorKeys[0] = new GradientColorKey(new Color(31f / 255f, 31f / 255f, 31f / 255f, 1f), 0.166f);
                colorKeys[1] = new GradientColorKey(new Color(71f / 255f, 75f / 255f, 81f / 255f, 1f), 0.333f);
                colorKeys[2] = new GradientColorKey(new Color(71f / 255f, 75f / 255f, 81f / 255f, 1f), 0.666f);
                colorKeys[3] = new GradientColorKey(new Color(31f / 255f, 31f / 255f, 31f / 255f, 1f), 0.833f);
                newGradient.colorKeys = colorKeys;
            }
            else if (gradientPreset == GradientPreset.DefaultFogColour)
            {
                colorKeys = new GradientColorKey[4];
                colorKeys[0] = new GradientColorKey(new Color(0f, 0f, 0f, 1f), 0.208f);
                colorKeys[1] = new GradientColorKey(new Color(158f / 255f, 189f / 255f, 195f / 255f, 1f), 0.292f);
                colorKeys[2] = new GradientColorKey(new Color(158f / 255f, 189f / 255f, 195f / 255f, 1f), 0.708f);
                colorKeys[3] = new GradientColorKey(new Color(0f, 0f, 0f, 1f), 0.792f);
                newGradient.colorKeys = colorKeys;
            }
            else if (gradientPreset == GradientPreset.DefaultPerlinGradient)
            {
                colorKeys = new GradientColorKey[2];
                colorKeys[0] = new GradientColorKey(new Color(0f, 0f, 0f, 1f), 0f);
                colorKeys[1] = new GradientColorKey(new Color(1f, 1f, 11f, 1f), 1f);
                newGradient.colorKeys = colorKeys;
            }
            else if (gradientPreset == GradientPreset.DefaultAmbientHznLight)
            {
                colorKeys = new GradientColorKey[4];
                colorKeys[0] = new GradientColorKey(new Color(12f / 255f, 11f / 255f, 9f / 255f, 1f), 0.166f);
                colorKeys[1] = new GradientColorKey(new Color(29f / 255f, 32f / 255f, 34f / 255f, 1f), 0.333f);
                colorKeys[2] = new GradientColorKey(new Color(29f / 255f, 32f / 255f, 34f / 255f, 1f), 0.666f);
                colorKeys[3] = new GradientColorKey(new Color(12f / 255f, 11f / 255f, 9f / 255f, 1f), 0.833f);
                newGradient.colorKeys = colorKeys;
            }
            else if (gradientPreset == GradientPreset.DefaultAmbientGndLight)
            {
                colorKeys = new GradientColorKey[4];
                colorKeys[0] = new GradientColorKey(new Color(4f / 255f, 4f / 255f, 3f / 255f, 1f), 0.166f);
                colorKeys[1] = new GradientColorKey(new Color(12f / 255f, 11f / 255f, 9f / 255f, 1f), 0.333f);
                colorKeys[2] = new GradientColorKey(new Color(12f / 255f, 11f / 255f, 9f / 255f, 1f), 0.666f);
                colorKeys[3] = new GradientColorKey(new Color(4f / 255f, 4f / 255f, 3f / 255f, 1f), 0.833f);
                newGradient.colorKeys = colorKeys;
            }
            return newGradient;
        }
    }
}