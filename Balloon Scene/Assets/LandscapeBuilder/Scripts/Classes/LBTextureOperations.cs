// Landscape Builder. Copyright (c) 2016-2019 SCSM Pty Ltd. All rights reserved.
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LandscapeBuilder
{
    public class LBTextureOperations
    {
        // Texture operations class

        #region Enumerations

        public enum InputChannelMode
        {
            R,
            G,
            B,
            A,
            RGBAverage,
            RGBAAverage
        }

        #endregion

        #region Blur Texture Methods

        /// <summary>
        /// Blurs a Texture2D using a box filter algorithm
        /// </summary>
        /// <returns>The blur.</returns>
        /// <param name="inputTexture">Input texture.</param>
        /// <param name="iterations">Iterations.</param>
        public static Texture2D BoxBlur(Texture2D inputTexture, int iterations)
        {
            if (iterations > 0)
            {
                int textureWidth = inputTexture.width;
                int textureHeight = inputTexture.height;

                Texture2D outputTexture = new Texture2D(textureWidth, textureHeight);
                Texture2D referenceTexture = Texture2D.Instantiate(inputTexture);

                Color centrePixel;

                Color[] rimPixels = new Color[8];

                for (int i = 0; i < iterations; i++)
                {
                    for (int x = 0; x < textureWidth; x++)
                    {
                        for (int y = 0; y < textureHeight; y++)
                        {
                            centrePixel = referenceTexture.GetPixel(x, y);

                            // Top Left Pixel
                            if (x > 0 && y > 0) { rimPixels[0] = referenceTexture.GetPixel(x - 1, y - 1); }
                            else { rimPixels[0] = centrePixel; }
                            // Top Mid Pixel
                            if (y > 0) { rimPixels[1] = referenceTexture.GetPixel(x, y - 1); }
                            else { rimPixels[1] = centrePixel; }
                            // Top Right Pixel
                            if (x < textureWidth - 2 && y > 0) { rimPixels[2] = referenceTexture.GetPixel(x + 1, y - 1); }
                            else { rimPixels[2] = centrePixel; }
                            // Mid Left Pixel
                            if (x > 0) { rimPixels[3] = referenceTexture.GetPixel(x - 1, y); }
                            else { rimPixels[3] = centrePixel; }
                            // Mid Right Pixel
                            if (x < textureWidth - 2) { rimPixels[4] = referenceTexture.GetPixel(x + 1, y); }
                            else { rimPixels[4] = centrePixel; }
                            // Bottom Left Pixel
                            if (x > 0 && y < textureWidth - 2) { rimPixels[5] = referenceTexture.GetPixel(x - 1, y + 1); }
                            else { rimPixels[5] = centrePixel; }
                            // Bottom Mid Pixel
                            if (y < textureWidth - 2) { rimPixels[6] = referenceTexture.GetPixel(x, y + 1); }
                            else { rimPixels[6] = centrePixel; }
                            // Bottom Right Pixel
                            if (x < textureWidth - 2 && y < textureWidth - 2) { rimPixels[7] = referenceTexture.GetPixel(x + 1, y + 1); }
                            else { rimPixels[7] = centrePixel; }

                            // Average surrounding pixels to get pixel colour
                            outputTexture.SetPixel(x, y, (rimPixels[0] + rimPixels[1] + rimPixels[2] + rimPixels[3] + rimPixels[4] +
                                                          rimPixels[5] + rimPixels[6] + rimPixels[7]) / 8f);
                        }
                    }

                    referenceTexture = null;
                    referenceTexture = Texture2D.Instantiate(outputTexture);
                }

                referenceTexture = null;

                return outputTexture;
            }
            else
            {
                return inputTexture;
            }
        }

        /// <summary>
        /// Blur or smooth a point in a texture using a Gaussian Blur. This is an adaption of the
        /// BlurPass used in LBImageFX. blurQuality 1-7. blurStrengthRadius is the distance in pixels
        /// that each pixel gets blurred across. 
        /// NOTE: call texture.Apply() after blurring point(s)
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="ptX"></param>
        /// <param name="ptY"></param>
        /// <param name="blurQuality"></param>
        /// <param name="blurStrengthRadius"></param>
        public static Color GaussianBlurTexturePoint(Texture2D texture, ref Color32[] colourArray, int ptX, int ptY, int blurQuality, float blurStrengthRadius, float blurMultiplier)
        {
            Color blurredPixelColour = Color.clear;
            float blurredPixelAlpha = 0f;

            if (texture == null) { Debug.LogWarning("ERROR: LBTextureOperations.GaussianBlurTexturePoint - texture is null"); }
            else
            {
                int textureWidth = texture.width;
                int textureHeight = texture.height;

                int texWidth = textureWidth;

                if (textureWidth < 1 || textureHeight < 1) { Debug.LogWarning("ERROR: LBTextureOperations.GaussianBlurTexturePoint - invalid texture dimensions for " + texture.name); }
                else if (blurQuality < 1 || blurQuality > 7) { Debug.LogWarning("ERROR: LBTextureOperations.GaussianBlurTexturePoint - blurQuality must be between 1 and 7"); }
                else if (blurStrengthRadius < 0f || blurStrengthRadius > textureWidth / 2) { Debug.LogWarning("ERROR: LBTextureOperations.GaussianBlurTexturePoint - blurStrength must be between 0.0 an 1.0"); }
                else
                {
                    float blurAmount = blurStrengthRadius / blurQuality;
                    float gd = 0f, denominator = 0f;

                    Color samplePixelColour;

                    // Sample coordinates
                    int sx, sy;

                    Color currentPixelColour = colourArray[ptY * texWidth + ptX];

                    denominator = 0f;
                    gd = 0f;

                    // Loop through a grid of surrounding pixels (including the original one)
                    for (int bx = -blurQuality; bx <= blurQuality; bx++)
                    {
                        for (int by = -blurQuality; by <= blurQuality; by++)
                        {
                            // Sample the given pixel
                            sx = (int)(ptX + (bx * blurAmount));
                            sy = (int)(ptY + (by * blurAmount));

                            // Ensure sample pixel is within the texture
                            if (sx >= 0 && sx < textureWidth && sy >= 0 && sy < textureHeight)
                            {
                                //samplePixelColour = texture.GetPixel(sx, sy);
                                samplePixelColour = colourArray[sy * texWidth + sx];
                                gd = GaussianDistribution((float)bx, 7.0f);
                                blurredPixelAlpha += (samplePixelColour.a * gd);
                                denominator += gd;
                            }
                        }
                    }

                    blurredPixelColour = currentPixelColour;

                    // If no samples were added no need to update the pixel
                    if (denominator >= 0.001f)
                    {
                        // Divide by total strength of samples
                        blurredPixelAlpha /= denominator;
                        blurredPixelColour.a = Mathf.Lerp(currentPixelColour.a, blurredPixelAlpha, blurMultiplier);
                    }
                }
            }

            return blurredPixelColour;
        }

        /// <summary>
        /// Blur a texture using a Gaussian Blur. This is an adaption of the BlurPass used
        /// in LBImageFX.
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="blurQuality"></param>
        /// <param name="blurStrength"></param>
        public static void GaussianBlurTexture(Texture2D texture, int blurQuality, float blurStrength)
        {
            if (texture == null) { Debug.LogWarning("ERROR: LBTextureOperations.GaussianBlurTexture - texture is null"); }
            else
            {
                int textureWidth = texture.width;
                int textureHeight = texture.height;

                if (textureWidth < 1 || textureHeight < 1) { Debug.LogWarning("ERROR: LBTextureOperations.GaussianBlurTexture - invalid texture dimensions for " + texture.name); }
                else if (blurQuality < 1 || blurQuality > 7) { Debug.LogWarning("ERROR: LBTextureOperations.GaussianBlurTexture - blurQuality must be between 1 and 7"); }
                else if (blurStrength < 0f || blurStrength > 1f) { Debug.LogWarning("ERROR: LBTextureOperations.GaussianBlurTexture - blurStrength must be between 0.0 an 1.0"); }
                else
                {
                    float blurAmount = blurStrength;
                    float gd = 0f, denominator = 0f;

                    Color samplePixelColour, blurredPixelColour;

                    // sample coordinates
                    int sx, sy;

                    for (int x = 0; x < textureWidth; x++)
                    {
                        for (int y = 0; y < textureHeight; y++)
                        {
                            denominator = 0f;
                            gd = 0f;
                            blurredPixelColour = Color.clear;

                            // Loop through a grid of surrounding pixels (including the original one)
                            for (int bx = -blurQuality; bx <= blurQuality; bx++)
                            {
                                for (int by = -blurQuality; by <= blurQuality; by++)
                                {
                                    // Sample the given pixel
                                    sx = (int)(x + (bx * blurAmount));
                                    sy = (int)(y + (by * blurAmount));

                                    // Ensure sample pixel is within the texture
                                    if (sx >= 0 && sx < textureWidth && sy >= 0 && sy < textureHeight)
                                    {
                                        samplePixelColour = texture.GetPixel(sx, sy);

                                        gd = GaussianDistribution((float)bx, 7.0f);
                                        blurredPixelColour += (samplePixelColour * gd);
                                        denominator += gd;
                                    }
                                }
                            }

                            // If no samples were added no need to update the pixel
                            if (denominator >= 0.001f)
                            {
                                // Divide by total strength of samples
                                blurredPixelColour /= denominator;

                                texture.SetPixel(x, y, blurredPixelColour);
                            }
                        }
                    }

                    texture.Apply();
                }
            }
        }

        // Gaussian Probability Distribution Function
        public static float GaussianDistribution(float x, float sigma)
        {
            return 0.39894f * Mathf.Exp(-0.5f * x * x / (sigma * sigma)) / sigma;
        }

        #endregion

        #region Tint Textures

        /// <summary>
        /// Tint a texture with a colour and return the resulting texture with the same name.
        /// NOTE: Probably should be using the grayscale value and then applying tint...
        /// At runtime, if the texture is not readable, it will return the untinted texture
        /// </summary>
        /// <param name="inputTexture"></param>
        /// <param name="tintColour"></param>
        /// <param name="tintStrength"></param>
        /// <returns></returns>
        public static Texture2D TintTexture(Texture2D inputTexture, Color tintColour, float tintStrength)
        {
            if (inputTexture == null) { return null; }

            // Create a copy of the original texture
            bool isInputTextureReadable = IsTextureReadable(inputTexture, false);
            if (!isInputTextureReadable)
            {
#if UNITY_EDITOR
                EnableReadable(inputTexture, true, true);
#else
            return inputTexture;
#endif
            }
            Texture2D tintedTexture = Texture2D.Instantiate(inputTexture);

            tintedTexture.name = inputTexture.name;

            int mipCount = tintedTexture.mipmapCount;
            if (mipCount == 0) { mipCount = 1; }

            for (int mip = 0; mip < mipCount; mip++)
            {
                Color[] colours = tintedTexture.GetPixels(mip);
                for (int i = 0; i < colours.Length; i++)
                {
                    colours[i] = Color.Lerp(colours[i], tintColour, tintStrength);
                }
                tintedTexture.SetPixels(colours, mip);
            }
            tintedTexture.Apply();
            return tintedTexture;
        }

        #endregion

        #region Flip or Rotate Texture Methods

        /// <summary>
        /// Flip a texture top-bottom
        /// </summary>
        /// <param name="inputTexture"></param>
        /// <returns></returns>
        public static Texture2D FlipTexture(Texture2D inputTexture)
        {
            // Create a copy of the original texture
            bool isInputTextureReadable = IsTextureReadable(inputTexture, false);
            if (!isInputTextureReadable)
            {
                #if UNITY_EDITOR
                EnableReadable(inputTexture, true, true);
                #else
                return inputTexture;
                #endif
            }
            Texture2D flippedTexture = Texture2D.Instantiate(inputTexture);

            int mipCount = flippedTexture.mipmapCount;
            if (mipCount == 0) { mipCount = 1; }

            int x, y, mip;

            for (mip = 0; mip < mipCount; mip++)
            {
                for (x = 0; x < inputTexture.width; x++)
                {
                    for (y = 0; y < inputTexture.height; y++)
                    {
                        flippedTexture.SetPixel(x, y, inputTexture.GetPixel(x, inputTexture.height - 1 - y));
                    }
                }
            }
            flippedTexture.Apply();
            return flippedTexture;
        }

        /// <summary>
        /// Rotate a Texture by an angle in degrees.
        /// Give it the same name as the inputTexture
        /// </summary>
        /// <param name="inputTexture"></param>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static Texture2D RotateTexture(Texture2D inputTexture, float angle)
        {
            if (inputTexture == null) { return null; }

            Texture2D rotatedTexture = new Texture2D(inputTexture.width, inputTexture.height);

            bool isInputTextureReadable = IsTextureReadable(inputTexture, false);
            if (!isInputTextureReadable)
            {
#if UNITY_EDITOR
                EnableReadable(inputTexture, true, true);
#else
            return inputTexture;
#endif
            }

            rotatedTexture.name = inputTexture.name;

            // Currently rotation is incorrect by 90 degrees. Correct that here
            angle += 90f;

            int x, y;
            float x1, y1, x2, y2;
            int width = inputTexture.width;
            int height = inputTexture.height;
            float x0 = RotateX(angle, -width / 2.0f, -height / 2.0f) + width / 2.0f;
            float y0 = RotateY(angle, -width / 2.0f, -height / 2.0f) + height / 2.0f;
            float dx_x = RotateX(angle, 1.0f, 0.0f);
            float dx_y = RotateY(angle, 1.0f, 0.0f);
            float dy_x = RotateX(angle, 0.0f, 1.0f);
            float dy_y = RotateY(angle, 0.0f, 1.0f);
            x1 = x0;
            y1 = y0;
            for (x = 0; x < inputTexture.width; x++)
            {
                x2 = x1;
                y2 = y1;
                for (y = 0; y < inputTexture.height; y++)
                {
                    x2 += dx_x;
                    y2 += dx_y;
                    rotatedTexture.SetPixel((int)Mathf.Floor(x), (int)Mathf.Floor(y), RotationGetPixelColour(inputTexture, x2, y2));
                }
                x1 += dy_x;
                y1 += dy_y;
            }
            rotatedTexture.Apply();
            return rotatedTexture;
        }

        private static Color RotationGetPixelColour(Texture2D texture, float x, float y)
        {
            Color pixelColour;
            int x1 = (int)Mathf.Floor(x);
            int y1 = (int)Mathf.Floor(y);
            if (x1 > texture.width || x1 < 0 || y1 > texture.height || y1 < 0)
            {
                pixelColour = Color.clear;
            }
            else
            {
                pixelColour = texture.GetPixel(x1, y1);
            }
            return pixelColour;
        }

        private static float RotateX(float angle, float x, float y)
        {
            float cos = Mathf.Cos(angle / 180.0f * Mathf.PI);
            float sin = Mathf.Sin(angle / 180.0f * Mathf.PI);
            return (x * cos + y * (-sin));
        }

        private static float RotateY(float angle, float x, float y)
        {
            float cos = Mathf.Cos(angle / 180.0f * Mathf.PI);
            float sin = Mathf.Sin(angle / 180.0f * Mathf.PI);
            return (x * sin + y * cos);
        }

        #endregion

        #region Create Normalmap Texture

        /// <summary>
        /// Creates a normal map texture from a Texture2D reference and a curve modifier
        /// </summary>
        /// <returns>The map from reference.</returns>
        /// <param name="inputTexture">Input texture.</param>
        /// <param name="curveModifier">Curve modifier.</param>
        public static Texture2D NormalMapFromReference(Texture2D inputTexture, AnimationCurve curveModifier)
        {
            int textureWidth = inputTexture.width;
            int textureHeight = inputTexture.height;

            Texture2D outputTexture = new Texture2D(textureWidth, textureHeight);

            for (int x = 0; x < textureWidth; x++)
            {
                for (int y = 0; y < textureHeight; y++)
                {
                    float referenceTexInput = inputTexture.GetPixel(x, y).grayscale;
                    outputTexture.SetPixel(x, y, curveModifier.Evaluate(referenceTexInput) * Color.white);
                }
            }

            return outputTexture;
        }

        #endregion

        #region Create Grayscale Texture

        /// <summary>
        /// Creates a grayscale texture from a Texture2D reference
        /// </summary>
        /// <returns>The texture from reference.</returns>
        /// <param name="inputTexture">Input texture.</param>
        public static void GrayscaleFromReference(Texture2D inputTexture, Texture2D outputTexture)
        {
            int textureWidth = inputTexture.width;
            int textureHeight = inputTexture.height;

            float inputGrayscale;

            for (int x = 0; x < textureWidth; x++)
            {
                for (int y = 0; y < textureHeight; y++)
                {
                    inputGrayscale = inputTexture.GetPixel(x, y).grayscale;
                    outputTexture.SetPixel(x, y, inputGrayscale * Color.white);
                }
            }
        }

        #endregion

        #region Create/Destroy Texture2D Methods

        /// <summary>
        /// Create a new Texture2D and make all pixels the same colour.
        /// Will generate mipmaps, sRGB colour space, TextureFormat: RGBA32
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="colour"></param>
        /// <returns></returns>
        public static Texture2D CreateTexture(int width, int height, Color colour)
        {
            Texture2D texture = new Texture2D(width, height);

            if (texture != null)
            {
                // Read all pixels into an array, so we can apply the changes
                // to the array, rather than use the slower SetPixel method.
                Color[] colours = texture.GetPixels();

                if (colours != null)
                {
                    for (int p = 0; p < colours.Length; p++)
                    {
                        colours[p] = colour;
                    }

                    // Copy array back to the texture
                    texture.SetPixels(colours);
                    // Update the texture
                    texture.Apply();
                    colours = null;
                }
            }
            return texture;
        }

        /// <summary>
        /// Create a new Texture2D and make all pixels the same colour.
        /// Colour Space is either sRGB or Linear.
        /// texFormat is typically RGBA32, ARGB32, DXT5Crunched etc
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="colour"></param>
        /// <param name="genMipMaps"></param>
        /// <param name="texFormat"></param>
        /// <param name="isLinearColourSpace"></param>
        /// <returns></returns>
        public static Texture2D CreateTexture(int width, int height, Color colour, bool genMipMaps, TextureFormat texFormat, bool isLinearColourSpace)
        {
            Texture2D texture = new Texture2D(width, height, texFormat, genMipMaps, isLinearColourSpace);

            if (texture != null)
            {
                // Read all pixels into an array, so we can apply the changes
                // to the array, rather than use the slower SetPixel method.
                Color[] colours = texture.GetPixels();

                if (colours != null)
                {
                    for (int p = 0; p < colours.Length; p++)
                    {
                        colours[p] = colour;
                    }

                    // Copy array back to the texture
                    texture.SetPixels(colours);
                    // Update the texture
                    texture.Apply();
                    colours = null;
                }
            }
            return texture;
        }

        /// <summary>
        /// Safely destroy or dispose of a Texture
        /// </summary>
        /// <param name="tex"></param>
        public static void DestroyTexture2D(ref Texture2D tex)
        {
            if (tex != null)
            {
                #if UNITY_EDITOR
                UnityEngine.Object.DestroyImmediate(tex);
                #else
                UnityEngine.Object.Destroy(tex);
                #endif

                tex = null;
            }
        }

        #endregion

        #region Texture2DArray Methods

        /// <summary>
        /// Safely destroy or dispose of a TextureADArray
        /// </summary>
        /// <param name="texArray"></param>
        public static void DestroyTexture2DArray(ref Texture2DArray texArray)
        {
            if (texArray != null)
            {
                #if UNITY_EDITOR
                UnityEngine.Object.DestroyImmediate(texArray);
                #else
                UnityEngine.Object.Destroy(texArray);
                #endif

                texArray = null;
            }
        }

        /// <summary>
        /// Copy a Texture2D into a slot within a Texture2DArray.
        /// arrayIndex is the zero-based Texture2DArray slot.
        /// If possible, do the copy on the GPU.
        /// </summary>
        /// <param name="texArray"></param>
        /// <param name="tex"></param>
        /// <param name="arrayIndex"></param>
        /// <param name="showErrors"></param>
        public static void CopyTextureTo2DArray(ref Texture2DArray texArray, Texture2D tex, int arrayIndex, bool showErrors)
        {
            // Validate the inputs
            if (texArray == null) { if (showErrors) { Debug.LogWarning("ERROR: CopyTextureTo2DArray failed because input Texture2DArray was null"); } }
            else if (tex == null) { if (showErrors) { Debug.LogWarning("ERROR: CopyTextureTo2DArray failed because input Texture2D was null"); } }
            else if (arrayIndex < 0 || arrayIndex > texArray.depth - 1) { if (showErrors) { Debug.LogWarning("ERROR: CopyTextureTo2DArray failed because arrayIndex was greater than the depth of the destination Texture2DArray"); } }
            else
            {
                int texWidth = tex.width;
                int texHeight = tex.height;
                
                if (texWidth != texArray.width || texHeight != texArray.height)
                {
                    if (showErrors) { Debug.LogWarning("ERROR: CopyTextureTo2DArray failed because input Texture2D was not the same width and height as destination Texture2DArray"); }
                }
                else if (SystemInfo.copyTextureSupport == UnityEngine.Rendering.CopyTextureSupport.None)
                {
                    // Perform a slow texture CPU copy into Texture2DArray due to lack of hardware support
                    texArray.SetPixels(tex.GetPixels(0), arrayIndex, 0);
                    texArray.Apply();
                    //Debug.Log("[DEBUG] CopyTextureTo2DArray (CPU) copied " + tex.name + " " + texWidth + "x" + texHeight);
                }
                else
                {
                    // Do the copy on the GPU (fast)
                    Graphics.CopyTexture(tex, 0, texArray, arrayIndex);
                    //Debug.Log("[DEBUG] CopyTextureTo2DArray (GPU) copied " + tex.name + " " + texWidth + "x" + texHeight);
                }
            }
        }

        #endregion

        #region RenderTexture Methods

        /// <summary>
        /// Safely destroy or cleanup a RenderTexture
        /// </summary>
        /// <param name="renderTexture"></param>
        public static void DestroyRenderTexture(ref RenderTexture renderTexture)
        {
            if (renderTexture != null)
            {
                if (renderTexture.IsCreated()) { renderTexture.Release(); }

                #if UNITY_EDITOR
                UnityEngine.Object.DestroyImmediate(renderTexture);
                #else
                UnityEngine.Object.Destroy(renderTexture);
                #endif

                renderTexture = null;
            }
        }

        #endregion

        #region Shadow Methods

        /// <summary>
        /// Creates a texture from a Texture2D reference based on the shadowing of that texture
        /// </summary>
        /// <returns>The texture from reference.</returns>
        /// <param name="inputTexture">Input texture.</param>
        public static void ShadowDetection(Texture2D inputTexture, Texture2D outputTexture, Color lightestShadowColour, bool removeShadows)
        {
            int textureWidth = inputTexture.width;
            int textureHeight = inputTexture.height;

            if (removeShadows)
            {
                // Remove shadows
                Color referenceTexInput;
                for (int x = 0; x < textureWidth; x++)
                {
                    for (int y = 0; y < textureHeight; y++)
                    {
                        referenceTexInput = inputTexture.GetPixel(x, y);
                        outputTexture.SetPixel(x, y, MaxColour(referenceTexInput, lightestShadowColour));
                    }
                }
            }
            else
            {
                // Shadow detection (useful for an occlusion map)
                float lightestShadowGrayscale = lightestShadowColour.grayscale;
                float inputGrayscale;
                float outputGrayscale;
                for (int x = 0; x < textureWidth; x++)
                {
                    for (int y = 0; y < textureHeight; y++)
                    {
                        inputGrayscale = inputTexture.GetPixel(x, y).grayscale;
                        outputGrayscale = Mathf.Clamp01(inputGrayscale / lightestShadowGrayscale);
                        outputTexture.SetPixel(x, y, outputGrayscale * Color.white);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a texture from a Texture2D reference based on the shadowing and low colour of that texture
        /// </summary>
        /// <returns>The texture from reference.</returns>
        /// <param name="inputTexture">Input texture.</param>
        public static void ShadowDetectionAndLowColour(Texture2D inputTexture, Texture2D outputTexture, Color lightestShadowColour, Color lowColour, float colourRange)
        {
            int textureWidth = inputTexture.width;
            int textureHeight = inputTexture.height;

            // Shadow detection (useful for an occlusion map)
            Color referenceTexInput;
            float lightestShadowGrayscale = lightestShadowColour.grayscale;
            float inputGrayscale;
            float outputGrayscale;
            for (int x = 0; x < textureWidth; x++)
            {
                for (int y = 0; y < textureHeight; y++)
                {
                    referenceTexInput = inputTexture.GetPixel(x, y);
                    inputGrayscale = referenceTexInput.grayscale;
                    outputGrayscale = Mathf.Clamp01(inputGrayscale / lightestShadowGrayscale);
                    outputGrayscale -= 1f - Mathf.Clamp01(ColourDifference(lowColour, referenceTexInput) / colourRange);
                    outputGrayscale = Mathf.Clamp01(outputGrayscale);
                    outputTexture.SetPixel(x, y, outputGrayscale * Color.white);
                }
            }
        }

        #endregion

        #region Texture Colour Methods

        /// <summary>
        /// Modify the colour range of a Texture2D using a gradient
        /// </summary>
        /// <returns>The texture from reference.</returns>
        /// <param name="inputTexture">Input texture.</param>
        public static Texture2D ModifyTextureWithGradient(Texture2D inputTexture, Gradient colourGradient)
        {
            int textureWidth = inputTexture.width;
            int textureHeight = inputTexture.height;

            Color referenceTexInput;
            for (int x = 0; x < textureWidth; x++)
            {
                for (int y = 0; y < textureHeight; y++)
                {
                    referenceTexInput = inputTexture.GetPixel(x, y);
                    inputTexture.SetPixel(x, y, colourGradient.Evaluate(referenceTexInput.grayscale));
                }
            }

            return inputTexture;
        }

        /// <summary>
        /// Creates a texture from a Texture2D reference that will return the original texture when tinted with the given colour
        /// </summary>
        /// <returns>The texture from reference.</returns>
        /// <param name="inputTexture">Input texture.</param>
        public static void RemoveTintColour(Texture2D inputTexture, Texture2D outputTexture, Color tintColour)
        {
            int textureWidth = inputTexture.width;
            int textureHeight = inputTexture.height;

            Color referenceTexInput;
            for (int x = 0; x < textureWidth; x++)
            {
                for (int y = 0; y < textureHeight; y++)
                {
                    referenceTexInput = inputTexture.GetPixel(x, y);
                    outputTexture.SetPixel(x, y, DivideColour(referenceTexInput, tintColour));
                }
            }
        }

        /// <summary>
        /// Creates a texture from a Texture2D reference based on the specular range of that texture
        /// </summary>
        /// <returns>The texture from reference.</returns>
        /// <param name="inputTexture">Input texture.</param>
        public static void SpecularRange(Texture2D inputTexture, Texture2D outputTexture, Color minSpecularColour, Color maxSpecularColour)
        {
            int textureWidth = inputTexture.width;
            int textureHeight = inputTexture.height;

            Color referenceTexInput;
            float outputGrayscale;
            for (int x = 0; x < textureWidth; x++)
            {
                for (int y = 0; y < textureHeight; y++)
                {
                    referenceTexInput = inputTexture.GetPixel(x, y);
                    outputGrayscale = InverseLerpColour(minSpecularColour, maxSpecularColour, referenceTexInput);
                    outputTexture.SetPixel(x, y, outputGrayscale * Color.white);
                }
            }
        }

        /// <summary>
        /// Creates a texture from a Texture2D reference based on the pixel variance of that texture
        /// </summary>
        /// <returns>The texture from reference.</returns>
        /// <param name="inputTexture">Input texture.</param>
        public static void PixelVariance(Texture2D inputTexture, Texture2D outputTexture, float smoothVariance, float steepVariance, bool useAlphaChannel)
        {
            int textureWidth = inputTexture.width;
            int textureHeight = inputTexture.height;

            Color referenceTexInput;
            float referenceTexGrayscale;
            float colourSamples;
            float totalVariance;
            Color outputColour;
            for (int x = 0; x < textureWidth; x++)
            {
                for (int y = 0; y < textureHeight; y++)
                {
                    referenceTexInput = inputTexture.GetPixel(x, y);
                    referenceTexGrayscale = referenceTexInput.grayscale;
                    colourSamples = 0f;
                    totalVariance = 0f;
                    if (x != 0) { colourSamples += 1f; totalVariance += Mathf.Abs(referenceTexGrayscale - inputTexture.GetPixel(x - 1, y).grayscale); }
                    if (x != textureWidth - 1) { colourSamples += 1f; totalVariance += Mathf.Abs(referenceTexGrayscale - inputTexture.GetPixel(x + 1, y).grayscale); }
                    if (y != 0) { colourSamples += 1f; totalVariance += Mathf.Abs(referenceTexGrayscale - inputTexture.GetPixel(x, y - 1).grayscale); }
                    if (y != textureHeight - 1) { colourSamples += 1f; totalVariance += Mathf.Abs(referenceTexGrayscale - inputTexture.GetPixel(x, y + 1).grayscale); }
                    totalVariance /= colourSamples;
                    if (useAlphaChannel)
                    {
                        // Only affect alpha channel of texture
                        outputColour = outputTexture.GetPixel(x, y);
                        outputColour.a = Mathf.InverseLerp(steepVariance, smoothVariance, totalVariance);
                        outputTexture.SetPixel(x, y, outputColour);
                    }
                    else { outputTexture.SetPixel(x, y, Mathf.InverseLerp(steepVariance, smoothVariance, totalVariance) * Color.white); }
                }
            }
        }

        /// <summary>
        /// Creates a texture from a Texture2D reference based on the similarity to a specified colour
        /// </summary>
        /// <returns>The texture from reference.</returns>
        /// <param name="inputTexture">Input texture.</param>
        public static void ColourMatch(Texture2D inputTexture, Texture2D outputTexture, Color colourToMatch, float colourRange)
        {
            int textureWidth = inputTexture.width;
            int textureHeight = inputTexture.height;

            Color referenceTexInput;
            float outputGrayscale;
            for (int x = 0; x < textureWidth; x++)
            {
                for (int y = 0; y < textureHeight; y++)
                {
                    referenceTexInput = inputTexture.GetPixel(x, y);
                    outputGrayscale = 1f - Mathf.Clamp01(ColourDifference(colourToMatch, referenceTexInput) / colourRange);
                    outputTexture.SetPixel(x, y, outputGrayscale * Color.white);
                }
            }
        }

        /// <summary>
        /// Creates a texture from a Texture2D reference based on a colour range composed of a high colour, a mid colour and a low colour
        /// </summary>
        /// <returns>The texture from reference.</returns>
        /// <param name="inputTexture">Input texture.</param>
        public static void ColourRange(Texture2D inputTexture, Texture2D outputTexture, Color lowColour, Color midColour, Color highColour)
        {
            int textureWidth = inputTexture.width;
            int textureHeight = inputTexture.height;

            Color referenceTexInput;
            float outputGrayscale;
            for (int x = 0; x < textureWidth; x++)
            {
                for (int y = 0; y < textureHeight; y++)
                {
                    referenceTexInput = inputTexture.GetPixel(x, y);
                    outputGrayscale = InverseLerpColour(lowColour, midColour, referenceTexInput) + InverseLerpColour(midColour, highColour, referenceTexInput);
                    outputGrayscale = Mathf.Clamp(outputGrayscale, 0f, 2f) * 0.5f;
                    outputTexture.SetPixel(x, y, outputGrayscale * Color.white);
                }
            }
        }

        public static void SetTextureRGB(Texture2D outputTexture, Color rgbColour)
        {
            int textureWidth = outputTexture.width;
            int textureHeight = outputTexture.height;

            Color outputColour = rgbColour;
            for (int x = 0; x < textureWidth; x++)
            {
                for (int y = 0; y < textureHeight; y++)
                {
                    // Set everything but the alpha channel
                    outputColour.a = outputTexture.GetPixel(x, y).a;
                    outputTexture.SetPixel(x, y, outputColour);
                }
            }
        }

        public static void SetTextureAlpha(Texture2D outputTexture, float alphaChannel)
        {
            int textureWidth = outputTexture.width;
            int textureHeight = outputTexture.height;

            Color outputColour;
            for (int x = 0; x < textureWidth; x++)
            {
                for (int y = 0; y < textureHeight; y++)
                {
                    // Only set the alpha channel
                    outputColour = outputTexture.GetPixel(x, y);
                    outputColour.a = alphaChannel;
                    outputTexture.SetPixel(x, y, outputColour);
                }
            }
        }

        /// <summary>
        /// Normalise the colour range of a Texture2D and/or invert colours
        /// </summary>
        /// <returns>The texture from reference.</returns>
        /// <param name="inputTexture">Input texture.</param>
        public static Texture2D FinaliseTextureColours(Texture2D inputTexture, bool normaliseColourRange, bool invertColours)
        {
            int textureWidth = inputTexture.width;
            int textureHeight = inputTexture.height;

            if (normaliseColourRange)
            {
                float minGrayscale = Mathf.Infinity;
                float maxGrayscale = Mathf.NegativeInfinity;
                float inputGrayscale;
                float outputGrayscale;

                for (int x = 0; x < textureWidth; x++)
                {
                    for (int y = 0; y < textureHeight; y++)
                    {
                        inputGrayscale = inputTexture.GetPixel(x, y).grayscale;
                        if (inputGrayscale < minGrayscale) { minGrayscale = inputGrayscale; }
                        if (inputGrayscale > maxGrayscale) { maxGrayscale = inputGrayscale; }
                    }
                }

                float scalingFactor = 1f / (maxGrayscale - minGrayscale);

                for (int x = 0; x < textureWidth; x++)
                {
                    for (int y = 0; y < textureHeight; y++)
                    {
                        Color referenceTexInput = inputTexture.GetPixel(x, y);
                        inputGrayscale = referenceTexInput.grayscale;
                        outputGrayscale = (inputGrayscale - minGrayscale) * scalingFactor;
                        inputTexture.SetPixel(x, y, referenceTexInput * (outputGrayscale / inputGrayscale));
                    }
                }
            }
            if (invertColours)
            {
                Color referenceTexInput;
                for (int x = 0; x < textureWidth; x++)
                {
                    for (int y = 0; y < textureHeight; y++)
                    {
                        referenceTexInput = inputTexture.GetPixel(x, y);
                        inputTexture.SetPixel(x, y, Color.white - referenceTexInput);
                    }
                }
            }

            return inputTexture;
        }

        /// <summary>
        /// Returns an averaged colour from 0-1 coords xCoord, yCoord in a colour array
        /// This function finds the four pixels enclosing the coordinates and
        /// blends between them depending on how close the coordinates are to each pixel
        /// </summary>
        /// <param name="xCoord"></param>
        /// <param name="yCoord"></param>
        /// <returns></returns>
        public static Color SampleColourAtCoordinates(Color[] pixelArray, int texWidth, int texHeight, float xCoord, float yCoord)
        {
            // Get texture-space coordinates from 0-1 coordinates
            float scaledXCoord = Mathf.Clamp(xCoord * (texWidth - 1), 0f, (float)texWidth - 1f);
            float scaledYCoord = Mathf.Clamp(yCoord * (texHeight - 1), 0f, (float)texHeight - 1f);
            int minScaledXCoord = (int)scaledXCoord;
            int minScaledYCoord = (int)scaledYCoord;

            if (minScaledXCoord > texWidth - 2) { minScaledXCoord = texWidth - 2; }
            if (minScaledYCoord > texHeight - 2) { minScaledYCoord = texHeight - 2; }

            // Get bottom-left pixel
            Color pixelBL = pixelArray[(minScaledYCoord * texWidth) + minScaledXCoord];
            // Get bottom-right pixel
            Color pixelBR = pixelArray[(minScaledYCoord * texWidth) + (minScaledXCoord + 1)];
            // Get top-left pixel
            Color pixelTL = pixelArray[((minScaledYCoord + 1) * texWidth) + (minScaledXCoord)];
            // Get top-right pixel
            Color pixelTR = pixelArray[((minScaledYCoord + 1) * texWidth) + (minScaledXCoord + 1)];

            // Blend the colours of the various pixels together
            Color finalColour = Color.white;

            float horizontalBlendFactor = scaledXCoord - minScaledXCoord;
            float verticalBlendFactor = scaledYCoord - minScaledYCoord;
            float bottomValues, topValues;

            // Calculate blended R channel
            bottomValues = ((1f - horizontalBlendFactor) * pixelBL.r) + (horizontalBlendFactor * pixelBR.r);
            topValues = ((1f - horizontalBlendFactor) * pixelTL.r) + (horizontalBlendFactor * pixelTR.r);
            finalColour.r = ((1f - verticalBlendFactor) * bottomValues) + (verticalBlendFactor * topValues);

            // Calculate blended G channel
            bottomValues = ((1f - horizontalBlendFactor) * pixelBL.g) + (horizontalBlendFactor * pixelBR.g);
            topValues = ((1f - horizontalBlendFactor) * pixelTL.g) + (horizontalBlendFactor * pixelTR.g);
            finalColour.g = ((1f - verticalBlendFactor) * bottomValues) + (verticalBlendFactor * topValues);

            // Calculate blended B channel
            bottomValues = ((1f - horizontalBlendFactor) * pixelBL.b) + (horizontalBlendFactor * pixelBR.b);
            topValues = ((1f - horizontalBlendFactor) * pixelTL.b) + (horizontalBlendFactor * pixelTR.b);
            finalColour.b = ((1f - verticalBlendFactor) * bottomValues) + (verticalBlendFactor * topValues);

            // Calculate blended A channel
            bottomValues = ((1f - horizontalBlendFactor) * pixelBL.a) + (horizontalBlendFactor * pixelBR.a);
            topValues = ((1f - horizontalBlendFactor) * pixelTL.a) + (horizontalBlendFactor * pixelTR.a);
            finalColour.a = ((1f - verticalBlendFactor) * bottomValues) + (verticalBlendFactor * topValues);

            // Return calculated pixel colour
            return finalColour;
        }

        // Colour equivalent of Mathf.InverseLerp, with some modifications
        public static float InverseLerpColour(Color a, Color b, Color lerpCol)
        {
            float iLerp1 = Mathf.InverseLerp(a.r, b.r, lerpCol.r);
            float iLerp2 = Mathf.InverseLerp(a.g, b.g, lerpCol.g);
            float iLerp3 = Mathf.InverseLerp(a.b, b.b, lerpCol.b);
            return (iLerp1 + iLerp2 + iLerp3) / 3f;
        }

        // Colour equivalent of Mathf.Max, with some modifications
        public static Color MaxColour(Color a, Color b)
        {
            return new Color(Mathf.Max(a.r, b.r), Mathf.Max(a.g, b.g), Mathf.Max(a.b, b.b), Mathf.Max(a.a, b.a));
        }

        // Returns the (unsigned) average difference in colour channels
        public static float ColourDifference(Color a, Color b)
        {
            return (Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b)) / 3f;
        }

        // Same as a / b in a shader
        public static Color DivideColour(Color a, Color b)
        {
            return new Color(a.r / b.r, a.b / b.b, a.g / b.g, a.a / b.a);
        }

        #endregion

        #region Create Texture Noise

        /// <summary>
        /// Populate the outputTexture with noise.
        /// </summary>
        /// <param name="outputTexture"></param>
        /// <param name="noiseOctaves"></param>
        /// <param name="perlinNoiseTileSize"></param>
        /// <param name="isTileable"></param>
        /// <param name="isDistanceToCentreMask"></param>
        public static void CreateTextureNoise(Texture2D outputTexture, int noiseOctaves, float perlinNoiseTileSize, float lacunarity, float gain, bool isTileable, bool isDistanceToCentreMask, float outputStrength, List<AnimationCurve> outputCurveModifiers, List<AnimationCurve> perOctaveCurveModifiers, bool normaliseColourRange, bool invertColours, Gradient colourGradient)
        {
            if (outputTexture != null)
            {
                int textureWidthInt = outputTexture.width;
                int textureHeightInt = outputTexture.height;

                float texWidth = (float)textureWidthInt;
                float texHeight = (float)textureHeightInt;

                //float minValue = Mathf.Infinity;
                //float maxValue = -Mathf.Infinity;
                //float thisValue = 0f;
                float xCoord, yCoord = 0f;

                UnityEngine.Color colour = new Color(1f, 1f, 1f, 1f);

                // Get minimum and maximum colour values first, so we can do scaling with full precision on the texture
                float scalingFactor = 1f;
                float minGrayscale = Mathf.Infinity;
                float maxGrayscale = Mathf.NegativeInfinity;
                if (normaliseColourRange)
                {
                    outputStrength = 1f;

                    for (int x = 0; x < textureWidthInt; x++)
                    {
                        for (int y = 0; y < textureHeightInt; y++)
                        {
                            // Get noise input

                            if (isTileable)
                            {
                                // Calculate coordinates differently for tileable noise
                                xCoord = ((float)x / texWidth) * perlinNoiseTileSize - (perlinNoiseTileSize * 0.5f);
                                yCoord = ((float)y / texHeight) * perlinNoiseTileSize - (perlinNoiseTileSize * 0.5f);
                            }
                            else
                            {
                                xCoord = (x / texWidth) * perlinNoiseTileSize;
                                yCoord = (y / texHeight) * perlinNoiseTileSize;
                            }

                            float noise = LBNoise.PerlinFractalNoise(xCoord, yCoord, noiseOctaves, lacunarity, gain, perOctaveCurveModifiers);

                            // Loop through all the output curve modifiers
                            for (int cm = 0; cm < outputCurveModifiers.Count; cm++)
                            {
                                // Modify the terrain based on animation curves
                                noise = outputCurveModifiers[cm].Evaluate(noise);
                            }

                            noise *= outputStrength;

                            // Record min and max noise values
                            if (noise < minGrayscale) { minGrayscale = noise; }
                            if (noise > maxGrayscale) { maxGrayscale = noise; }
                        }
                    }
                    // Calculate a scaling factor
                    scalingFactor = 1f / (maxGrayscale - minGrayscale);
                }

                // Populate texture with noise

                //Should read into an array so don't need to keep calling Get/SetPixel.
                for (int x = 0; x < textureWidthInt; x++)
                {
                    for (int y = 0; y < textureHeightInt; y++)
                    {
                        if (isTileable)
                        {
                            // Calculate coordinates differently for tileable noise
                            xCoord = ((float)x / texWidth) * perlinNoiseTileSize - (perlinNoiseTileSize * 0.5f);
                            yCoord = ((float)y / texHeight) * perlinNoiseTileSize - (perlinNoiseTileSize * 0.5f);
                        }
                        else
                        {
                            xCoord = (x / texWidth) * perlinNoiseTileSize;
                            yCoord = (y / texHeight) * perlinNoiseTileSize;
                        }

                        float noise = LBNoise.PerlinFractalNoise(xCoord, yCoord, noiseOctaves, lacunarity, gain, perOctaveCurveModifiers);

                        // Loop through all the output curve modifiers
                        for (int cm = 0; cm < outputCurveModifiers.Count; cm++)
                        {
                            // Modify the terrain based on animation curves
                            noise = outputCurveModifiers[cm].Evaluate(noise);
                        }

                        if (normaliseColourRange)
                        {
                            // Scale by output strength and normalise colour range
                            noise = ((noise * outputStrength) - minGrayscale) * scalingFactor;
                        }
                        else
                        {
                            // Just scale by output strength
                            noise = noise * outputStrength;
                        }

                        // Set colour using gradient
                        colour = colourGradient.Evaluate(noise);
                        //colour.r = colour.b = colour.g = colour.a = noise;

                        // Invert colours if necessary
                        if (invertColours) { colour = Color.white - colour; }

                        outputTexture.SetPixel(x, y, colour);
                    }
                }

                // Apply masking

                if (isDistanceToCentreMask)
                {
                    //float interpolate = 0.1f;
                    //UnityEngine.Color colour = new Color(1f, 1f, 1f, 1f);
                    Vector2 centre = new Vector2(texWidth / 2f, texWidth / 2f);

                    AnimationCurve distToCentreMask = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

                    for (int x = 0; x < textureWidthInt; x++)
                    {
                        for (int y = 0; y < textureHeightInt; y++)
                        {
                            xCoord = ((float)x / texWidth);
                            yCoord = ((float)y / texHeight);

                            colour = outputTexture.GetPixel(x, y);

                            float distToCenter = Vector2.Distance(centre, new Vector2(x, y)) / textureWidthInt;
                            colour *= distToCentreMask.Evaluate(distToCenter * 2f);
                            outputTexture.SetPixel(x, y, colour);

                            //float val = (1 - Mathf.Cos(interpolate * Mathf.PI)) * 0.5f;
                            //thisValue = xCoord * (1f - val) + yCoord * val;
                            //outputTexture.SetPixel(x, y, colour * (1f - thisValue));
                        }
                    }
                }

                outputTexture.Apply();
            }
        }

        #endregion
        
        #region Combine Texture Methods

        // Combines multiple textures together, adding the colour value from each texture for the output
        public static void CombineTexturesAdditive(List<Texture2D> inputTexturesList, Texture2D outputTexture)
        {
            int textureWidth = outputTexture.width;
            int textureHeight = outputTexture.height;

            Color totalColour;
            // Loop through all the pixels of every texture
            for (int x = 0; x < textureWidth; x++)
            {
                for (int y = 0; y < textureHeight; y++)
                {
                    // Set the initial total to black (no colour)
                    totalColour = Color.black;
                    for (int t = 0; t < inputTexturesList.Count; t++)
                    {
                        // Add the pixel colour to the stored total
                        totalColour += inputTexturesList[t].GetPixel(x % inputTexturesList[t].width, y % inputTexturesList[t].height);
                    }
                    // Set the pixel output as the stored colour total
                    outputTexture.SetPixel(x, y, totalColour);
                }
            }
        }

        // Combines multiple textures together, selecting the maximum colour value from each texture for the output
        public static void CombineTexturesMaximum(List<Texture2D> inputTexturesList, Texture2D outputTexture)
        {
            int textureWidth = outputTexture.width;
            int textureHeight = outputTexture.height;

            Color referenceTexInput;
            Color maxColour;
            // Loop through all the pixels of every texture
            for (int x = 0; x < textureWidth; x++)
            {
                for (int y = 0; y < textureHeight; y++)
                {
                    maxColour = Color.black;
                    for (int t = 0; t < inputTexturesList.Count; t++)
                    {
                        referenceTexInput = inputTexturesList[t].GetPixel(x % inputTexturesList[t].width, y % inputTexturesList[t].height);
                        // If a pixel has a brighter colour than the others set it as the max colour
                        if (referenceTexInput.grayscale > maxColour.grayscale) { maxColour = referenceTexInput; }
                    }
                    // Set the pixel output as the max colour
                    outputTexture.SetPixel(x, y, maxColour);
                }
            }
        }

        // Combines multiple textures together, selecting the minimum colour value from each texture for the output
        public static void CombineTexturesMinimum(List<Texture2D> inputTexturesList, Texture2D outputTexture)
        {
            int textureWidth = outputTexture.width;
            int textureHeight = outputTexture.height;

            Color referenceTexInput;
            Color minColour;
            // Loop through all the pixels of every texture
            for (int x = 0; x < textureWidth; x++)
            {
                for (int y = 0; y < textureHeight; y++)
                {
                    minColour = Color.white;
                    for (int t = 0; t < inputTexturesList.Count; t++)
                    {
                        referenceTexInput = inputTexturesList[t].GetPixel(x % inputTexturesList[t].width, y % inputTexturesList[t].height);
                        // If a pixel has a brighter colour than the others set it as the min colour
                        if (referenceTexInput.grayscale < minColour.grayscale) { minColour = referenceTexInput; }
                    }
                    // Set the pixel output as the min colour
                    outputTexture.SetPixel(x, y, minColour);
                }
            }
        }

        // Combines multiple textures together by using a different source texture for each channel
        public static void CombineTexturesChannels(List<Texture2D> inputTexturesList, Texture2D outputTexture,
        List<InputChannelMode> inputChannelModeList)
        {
            int textureWidth = outputTexture.width;
            int textureHeight = outputTexture.height;

            Color outputColour;
            // Loop through all the pixels of every texture
            for (int x = 0; x < textureWidth; x++)
            {
                for (int y = 0; y < textureHeight; y++)
                {
                    // Get R channel input
                    outputColour.r = InputChannelModeOutput(inputChannelModeList[0], inputTexturesList[0].GetPixel(x % inputTexturesList[0].width, y % inputTexturesList[0].height));
                    // Get G channel input
                    outputColour.g = InputChannelModeOutput(inputChannelModeList[1], inputTexturesList[1].GetPixel(x % inputTexturesList[1].width, y % inputTexturesList[1].height));
                    // Get B channel input
                    outputColour.b = InputChannelModeOutput(inputChannelModeList[2], inputTexturesList[2].GetPixel(x % inputTexturesList[2].width, y % inputTexturesList[2].height));
                    // Get A channel input
                    outputColour.a = InputChannelModeOutput(inputChannelModeList[3], inputTexturesList[3].GetPixel(x % inputTexturesList[3].width, y % inputTexturesList[3].height));
                    // Set the pixel output as the calculated output colour
                    outputTexture.SetPixel(x, y, outputColour);
                }
            }
        }

        #endregion

        #region Resize Texture Methods

        public static Texture2D GenerateDownscaledTexture(Texture2D sourceTexture, int newResolution)
        {
            int textureWidth = sourceTexture.width;
            int textureHeight = sourceTexture.height;

            int newTextureWidth = newResolution;
            int newTextureHeight = newResolution * (textureHeight / textureWidth);

            if (newTextureWidth > textureWidth) { newTextureWidth = textureWidth; }
            if (newTextureHeight > textureHeight) { newTextureHeight = textureHeight; }

            Texture2D newTexture = new Texture2D(newTextureWidth, newTextureHeight);

            int downscaling = textureWidth / newTextureWidth;

            Color referenceTexInput;
            for (int x = 0; x < newTextureWidth; x++)
            {
                for (int y = 0; y < newTextureHeight; y++)
                {
                    referenceTexInput = sourceTexture.GetPixel(((x + 1) * downscaling) - 1, ((y + 1) * downscaling) - 1);
                    newTexture.SetPixel(x, y, referenceTexInput);
                }
            }

            newTexture.Apply();

            return newTexture;
        }

        /// <summary>
        /// Modified non-threading version from Eric Haines (Eric5h5)'s TextureScale
        /// http://wiki.unity3d.com/index.php/TextureScale
        /// </summary>
        /// <param name="obj"></param>
        public static void TexturePointScale(Texture2D texture, int newWidth, int newHeight)
        {
            if (texture == null) { Debug.LogWarning("ERROR: LBTextureOperations.TexturePointScale - texture cannot be null"); }
            else
            {
                int oldWidth = texture.width;
                int oldHeight = texture.height;

                if (oldWidth < 1 || oldHeight < 1) { Debug.LogWarning("ERROR: LBTextureOperations.TexturePointScale - texture is invalid"); }
                else if (newWidth < 1 || newHeight < 1) { Debug.LogWarning("ERROR: LBTextureOperations.TexturePointScale - new size is invalid"); }
                else
                {
                    Color[] texColors = texture.GetPixels();
                    Color[] newColors = new Color[newWidth * newHeight];

                    float ratioX = ((float)oldWidth) / newWidth;
                    float ratioY = ((float)oldHeight) / newHeight;

                    for (int y = 0; y < newHeight; y++)
                    {
                        int thisY = (int)(ratioY * y) * oldWidth;
                        int yw = y * newWidth;
                        for (int x = 0; x < newWidth; x++)
                        {
                            newColors[yw + x] = texColors[(int)(thisY + ratioX * x)];
                        }
                    }
                    // Resize the texture which also clears all pixel data
                    texture.Resize(newWidth, newHeight);
                    texture.SetPixels(newColors);
                    texture.Apply();
                }
            }
        }

        #endregion

        #region Remove Holes

        /// <summary>
        /// Repairs holes in image data - typically depicted by 0 values or one wildly different from other nearby
        /// values.
        /// TODO RepairHoles - A better approach may be to get a matrix, remove all zeros then take the average
        /// Need to sample all data and work out what makes up a small percentage of the overall data
        /// </summary>
        /// <param name="inputTexture"></param>
        /// <param name="threshold"></param>
        /// <returns></returns>
        public static Texture2D RemoveHoles(Texture2D inputTexture, float threshold)
        {
            if (inputTexture == null)
            {
                return null;
            }
            else
            {
                // Create a copy of the original texture
                Texture2D newTexture = Texture2D.Instantiate(inputTexture);
                float pixelHeight = 0f;
                bool updatePixel = false;

                //int tempCount = 0;

                // Loop through the texture
                for (int x = 0; x < newTexture.width; x++)
                {
                    for (int y = 0; y < newTexture.height; y++)
                    {
                        updatePixel = false;
                        pixelHeight = newTexture.GetPixel(x, y).grayscale;

                        if (pixelHeight <= threshold)
                        {
                            if (x > 0 && x < newTexture.width - 2)
                            {
                                // Check neighbours
                                pixelHeight = newTexture.GetPixel(x - 1, y).grayscale;

                                if (pixelHeight <= threshold)
                                {
                                    pixelHeight = newTexture.GetPixel(x + 1, y).grayscale;

                                    if (pixelHeight <= threshold)
                                    {
                                        if (y > 0 && y < newTexture.height - 2)
                                        {
                                            pixelHeight = newTexture.GetPixel(x, y - 1).grayscale;

                                            if (pixelHeight <= threshold)
                                            {
                                                pixelHeight = newTexture.GetPixel(x, y + 1).grayscale;

                                                if (pixelHeight <= threshold)
                                                {
                                                    pixelHeight = newTexture.GetPixel(x - 1, y - 1).grayscale;

                                                    if (pixelHeight <= threshold)
                                                    {
                                                        pixelHeight = newTexture.GetPixel(x + 1, y + 1).grayscale;

                                                        if (pixelHeight <= threshold)
                                                        {
                                                            pixelHeight = newTexture.GetPixel(x - 1, y + 1).grayscale;

                                                            if (pixelHeight <= threshold)
                                                            {
                                                                pixelHeight = newTexture.GetPixel(x + 1, y - 1).grayscale;

                                                                if (pixelHeight <= threshold)
                                                                {
                                                                    //pixelHeight = 1f;
                                                                    //if (tempCount++ < 5) { Debug.Log(" Didn't Repair hole using XY at " + x.ToString() + "," + y.ToString()); }
                                                                }
                                                            }
                                                            else { updatePixel = true; }
                                                        }
                                                        else { updatePixel = true; }
                                                    }
                                                    else { updatePixel = true; }
                                                }
                                                else { updatePixel = true; }
                                            }
                                            else { updatePixel = true; }
                                        }
                                    }
                                    else { updatePixel = true; }
                                }
                                else { updatePixel = true; }
                            }
                        }
                        if (updatePixel) { newTexture.SetPixel(x, y, new Color(pixelHeight, pixelHeight, pixelHeight, 1f)); }
                    }
                }

                return newTexture;
            }
        }

        #endregion

        #region Misc Texture Methods

        // Returns the input from the given channel/s of a colour
        public static float InputChannelModeOutput(InputChannelMode inputChannelMode, Color inputColour)
        {
            if (inputChannelMode == InputChannelMode.R) { return inputColour.r; }
            else if (inputChannelMode == InputChannelMode.G) { return inputColour.g; }
            else if (inputChannelMode == InputChannelMode.B) { return inputColour.b; }
            else if (inputChannelMode == InputChannelMode.A) { return inputColour.a; }
            else if (inputChannelMode == InputChannelMode.RGBAverage) { return (inputColour.r + inputColour.g + inputColour.b) * 0.333f; }
            else { return (inputColour.r + inputColour.g + inputColour.b + inputColour.a) * 0.25f; }
        }

        /// <summary>
        /// Generates a smaller texture from a portion of a larger texture. 
        /// TexDownScaling determines how much smaller the new texture is than the original texture, 
        /// while min X and Y coords determine where the new texture is located in the coordinate space of the original texture
        /// </summary>
        /// <param name="largerTexture"></param>
        /// <param name="texDownScaling"></param>
        /// <param name="minXCoord"></param>
        /// <param name="minYCoord"></param>
        /// <returns></returns>
        public static Texture2D GenerateTex2DFromPartOfTex2D(Texture2D largerTexture, int texDownScaling, float minXCoord, float minYCoord)
        {
            // Create the new texture at the correct size
            int largerTexWidth = largerTexture.width, largerTexHeight = largerTexture.height;
            int newTexWidth = Mathf.NextPowerOfTwo(largerTexWidth / texDownScaling);
            int newTexHeight = Mathf.NextPowerOfTwo(largerTexHeight / texDownScaling);
            Texture2D newTexture = new Texture2D(newTexWidth, newTexHeight);

            // Get pixel arrays for faster computations
            Color[] largerTexturePixelArray = largerTexture.GetPixels();
            Color[] newTexturePixelArray = newTexture.GetPixels();

            // Loop through all of the pixels in the new texture
            float thisXCoord, thisYCoord, texWidthF = newTexWidth - 1f, texHeightF = newTexHeight - 1f, coordsTexSize = 1f / texDownScaling;
            for (int x = 0; x < newTexWidth; x++)
            {
                for (int y = 0; y < newTexHeight; y++)
                {
                    // Get the coordinates of this pixel in the original texture
                    thisXCoord = ((x / texWidthF) * coordsTexSize) + minXCoord;
                    thisYCoord = ((y / texHeightF) * coordsTexSize) + minYCoord;

                    // Sample this pixel from the original texture
                    newTexturePixelArray[(y * newTexWidth) + x] = SampleColourAtCoordinates(largerTexturePixelArray, largerTexWidth,
                        largerTexHeight, thisXCoord, thisYCoord);
                }
            }

            // Apply new pixel array to the new texture
            newTexture.SetPixels(newTexturePixelArray);
            newTexture.Apply();

            // Return the newly created texture
            return newTexture;
        }

        #region Editor-Only Texture Methods

#if UNITY_EDITOR

        /// <summary>
        /// Get the full file path of a texture
        /// EDITOR ONLY
        /// </summary>
        /// <param name="texture"></param>
        /// <returns></returns>
        public static string GetTextureFilePath(Texture2D texture)
        {
            if (texture == null) { return string.Empty; }
            else return AssetDatabase.GetAssetPath(texture);
        }

        /// <summary>
        /// Enable (or disable) a texture for Read/Write operations such as Texture2D.GetPixel(s)
        /// Textures enabled for Read/Write consume more system memory because a copy needs to be
        /// kept after it uploaded to the graphics API.
        /// </summary>
        /// <param name="textureImporter"></param>
        /// <param name="isReadable"></param>
        public static void EnableReadable(Texture2D texture, bool isReadable, bool showErrors = true)
        {
            if (texture == null)
            {
                if (showErrors) { Debug.LogError("LBTextureOperations.EnableReadable: Texture is null"); }
            }
            else
            {
                string textureAssetPath = AssetDatabase.GetAssetPath(texture);

                TextureImporter textureImporter = (TextureImporter)AssetImporter.GetAtPath(textureAssetPath);

                if (textureImporter != null)
                {
                    // Set to TrueColor (24-bit) prior to Unity 5.5 which helps Topography Image layers - v1.3.0
                    // For Unity 5.5 (which doesn't support textureFormat property), use TextureImporterCompression
#if UNITY_5_5_OR_NEWER
                    textureImporter.textureType = TextureImporterType.Default;
                    textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
#else
                textureImporter.textureType = TextureImporterType.Advanced;
                textureImporter.textureFormat = TextureImporterFormat.AutomaticTruecolor;
#endif
                    textureImporter.isReadable = isReadable;
                    AssetDatabase.ImportAsset(textureAssetPath);
                    AssetDatabase.Refresh();
                }
                else { Debug.LogError("Operation could not be completed because the texture importer for the specified texture is null."); }

                // Re-test after making the change. Sometimes this does not work
                bool canReadTexture = true;
                try { Color colour = texture.GetPixel(0, 0); colour.r = colour.g; } // Use colour again to prevent warning message
                catch (UnityException e)
                {
                    if (e.Message.StartsWith("Texture '" + texture.name + "' is not readable"))
                    {
                        canReadTexture = false;
                    }
                }

                if (isReadable != canReadTexture)
                {
                    string msg = "Could not set " + texture.name + " Read / Write to " + isReadable.ToString();
                    if (isReadable) { msg += ". Set texture type to advanced and tick Read / Write Enabled"; }
                    Debug.LogError(msg);
                }
            }
        }

        /// <summary>
        /// Set attributes on a Texture.
        /// e.g. SetTextureAttributes(texture, TextureImporterFormat.AutomaticTruecolor, FilterMode.Trilinear, false, 256, false);
        /// NOTE: maxTextureSize must be a power of 2.
        /// If maxTextureSize == 0, the size attribute won't be changed
        /// If using Unity 5.5 or newer, textFormat will always set textureCompression property to CompressedHQ. This may be revised
        /// in later versions of LB.
        /// Recommend use SetTextureAttributes(Texture2D texture, TextureImporterCompression textureCompression...)
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="textureFormat"></param>
        /// <param name="isReadable"></param>
        /// <param name="maxTextureSize"></param>
        /// <param name="showErrors"></param>
        public static void SetTextureAttributes(Texture2D texture, TextureImporterFormat textureFormat, FilterMode filterMode, bool isReadable, int maxTextureSize, bool showErrors = true)
        {
            if (texture == null)
            {
                if (showErrors) { Debug.LogError("LBTextureOperations.SetTextureAttributes: Texture is null"); }
            }
            else if (!Mathf.IsPowerOfTwo(maxTextureSize))
            {
                if (showErrors) { Debug.LogError("LBTextureOperations.SetTextureAttributes: maxTextureSize must be a power of 2. e.g. 64, 128, 256, 512, 1024, 2048, 8192"); }
            }
            else
            {
                // If isReadable isn't set to true initially, advanced settings don't take effect.
                if (!isReadable) { EnableReadable(texture, true, showErrors); }

                string textureAssetPath = AssetDatabase.GetAssetPath(texture);
                TextureImporter textureImporter = (TextureImporter)AssetImporter.GetAtPath(textureAssetPath);

                if (textureImporter != null)
                {
                    // textureFormat is no longer supported in 5.5. Just set to Uncompressed (equivalent to AutomaticTrueColor)
#if UNITY_5_5_OR_NEWER
                    if (isReadable) { textureImporter.textureType = TextureImporterType.Default; }
                    textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
#else
                if (isReadable) { textureImporter.textureType = TextureImporterType.Advanced; }
                textureImporter.textureFormat = textureFormat;
#endif
                    textureImporter.filterMode = filterMode;
                    if (maxTextureSize > 0) { textureImporter.maxTextureSize = maxTextureSize; }
                    textureImporter.isReadable = isReadable;
                    // Refresh the asset
                    AssetDatabase.ImportAsset(textureAssetPath);
                    //Debug.Log("Updating: " + textureAssetPath);
                }
                else { Debug.LogError("Operation could not be completed because the texture importer for the specified texture is null."); }

                if (isReadable)
                {
                    // Re-test after making the change. Sometimes this does not work
                    bool canReadTexture = true;
                    try { Color colour = texture.GetPixel(0, 0); colour.r = colour.g; } // Use colour again to prevent warning message
                    catch (UnityException e)
                    {
                        if (e.Message.StartsWith("Texture '" + texture.name + "' is not readable"))
                        {
                            canReadTexture = false;
                        }
                    }

                    if (isReadable != canReadTexture)
                    {
                        string msg = "Could not set " + texture.name + " Read / Write to " + isReadable.ToString();
                        if (isReadable) { msg += ". Set texture type to advanced and tick Read / Write Enabled"; }
                        Debug.LogError(msg);
                    }
                }
            }
        }

#if UNITY_5_5_OR_NEWER
        /// <summary>
        /// Set attributes on a Texture. Requires Unity 5.5 or newer.
        /// e.g. SetTextureAttributes(texture, TextureImporterCompression.CompressedHQ, FilterMode.Trilinear, false, 256, false);
        /// NOTE: maxTextureSize must be a power of 2.
        /// If maxTextureSize == 0, the size attribute won't be changed
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="textureCompression"></param>
        /// <param name="filterMode"></param>
        /// <param name="isReadable"></param>
        /// <param name="maxTextureSize"></param>
        /// <param name="showErrors"></param>
        public static void SetTextureAttributes(Texture2D texture, TextureImporterCompression textureCompression, FilterMode filterMode, bool isReadable, int maxTextureSize, bool showErrors = true)
        {
            if (texture == null)
            {
                if (showErrors) { Debug.LogError("LBTextureOperations.SetTextureAttributes: Texture is null"); }
            }
            else if (!Mathf.IsPowerOfTwo(maxTextureSize))
            {
                if (showErrors) { Debug.LogError("LBTextureOperations.SetTextureAttributes: maxTextureSize must be a power of 2. e.g. 64, 128, 256, 512, 1024, 2048, 8192"); }
            }
            else
            {
                // If isReadable isn't set to true initially, advanced settings don't take effect.
                if (!isReadable) { EnableReadable(texture, true, showErrors); }

                string textureAssetPath = AssetDatabase.GetAssetPath(texture);
                TextureImporter textureImporter = (TextureImporter)AssetImporter.GetAtPath(textureAssetPath);

                if (textureImporter != null)
                {
                    if (isReadable) { textureImporter.textureType = TextureImporterType.Default; }
                    textureImporter.textureCompression = textureCompression;
                    textureImporter.filterMode = filterMode;
                    if (maxTextureSize > 0) { textureImporter.maxTextureSize = maxTextureSize; }
                    textureImporter.isReadable = isReadable;
                    // Refresh the asset
                    AssetDatabase.ImportAsset(textureAssetPath);
                    //Debug.Log("Updating: " + textureAssetPath);
                }
                else { Debug.LogError("Operation could not be completed because the texture importer for the specified texture is null."); }

                if (isReadable)
                {
                    // Re-test after making the change. Sometimes this does not work
                    bool canReadTexture = true;
                    try { Color colour = texture.GetPixel(0, 0); colour.r = colour.g; } // Use colour again to prevent warning message
                    catch (UnityException e)
                    {
                        if (e.Message.StartsWith("Texture '" + texture.name + "' is not readable"))
                        {
                            canReadTexture = false;
                        }
                    }

                    if (isReadable != canReadTexture)
                    {
                        string msg = "Could not set " + texture.name + " Read / Write to " + isReadable.ToString();
                        if (isReadable) { msg += ". Set texture type to advanced and tick Read / Write Enabled"; }
                        Debug.LogError(msg);
                    }
                }
            }
        }
#endif

        /// <summary>
        /// Check to see if a texture is a normalmap.
        /// Change to NormalMap if fixIfRequired = true but texture is not a normalmap
        /// EDITOR ONLY
        /// </summary>
        /// <param name="normalMap"></param>
        /// <param name="fixIfRequired"></param>
        /// <returns></returns>
        public static bool IsNormalMap(Texture2D normalMap, bool fixIfRequired, bool showErrors)
        {
            bool isNormalMap = false;
            string msg = string.Empty;

            if (normalMap == null) { msg = "NormalMap texture is null"; }
            else
            {
                string path = AssetDatabase.GetAssetPath(normalMap);

                TextureImporter texImporter = (TextureImporter)AssetImporter.GetAtPath(path);
                if (texImporter == null) { msg = "Could not import " + normalMap.name + " normal map"; }
                else
                {
#if UNITY_5_5_OR_NEWER
                    isNormalMap = texImporter.textureType == TextureImporterType.NormalMap;
#else
                isNormalMap = texImporter.textureType == TextureImporterType.Bump;
#endif

                    if (!isNormalMap && fixIfRequired)
                    {
                        texImporter.convertToNormalmap = true;

#if UNITY_5_5_OR_NEWER
                        texImporter.alphaSource = TextureImporterAlphaSource.FromGrayScale;
                        texImporter.textureType = TextureImporterType.NormalMap;
#else
                    texImporter.grayscaleToAlpha = true;
                    texImporter.textureType = TextureImporterType.Bump;
#endif

                        texImporter.isReadable = true;
                        // Need to reimport the Texture to apply changes
                        AssetDatabase.ImportAsset(path);
                        AssetDatabase.Refresh();
                        isNormalMap = true;
                    }
                    else { msg = " " + normalMap.name + " is not a normalmap"; }
                }
            }

            if (!isNormalMap && showErrors)
            {
                Debug.Log("LBTextureOperations.IsNormalMap " + msg);
            }

            return isNormalMap;
        }

        /// <summary>
        /// IsHeightMap
        /// EDITOR ONLY
        /// </summary>
        /// <param name="heightMap"></param>
        /// <param name="fixIfRequired"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static bool IsHeightMap(Texture2D heightMap, bool fixIfRequired, bool showErrors)
        {
            bool isHeightMap = false;
            string msg = string.Empty;

            if (heightMap == null) { msg = "HeightMap texture is null"; }
            else
            {
                string path = AssetDatabase.GetAssetPath(heightMap);

                TextureImporter texImporter = (TextureImporter)AssetImporter.GetAtPath(path);
                if (texImporter == null) { msg = "Could not import " + heightMap.name + " height map"; }
                else
                {
#if UNITY_5_5_OR_NEWER
                    isHeightMap = (texImporter.DoesSourceTextureHaveAlpha() || texImporter.alphaSource == TextureImporterAlphaSource.FromGrayScale);
#else
                isHeightMap = (texImporter.DoesSourceTextureHaveAlpha() || texImporter.grayscaleToAlpha);
#endif

                    if (!isHeightMap && fixIfRequired)
                    {
                        texImporter.convertToNormalmap = false;
                        if (!texImporter.DoesSourceTextureHaveAlpha())
                        {
#if UNITY_5_5_OR_NEWER
                            texImporter.alphaSource = TextureImporterAlphaSource.FromGrayScale;
#else
                        texImporter.grayscaleToAlpha = true;
#endif
                        }

#if UNITY_5_5_OR_NEWER
                        texImporter.textureType = TextureImporterType.Default;
#else
                    texImporter.textureType = TextureImporterType.Advanced;
#endif

                        texImporter.isReadable = true;
                        // Need to reimport the Texture to apply changes
                        AssetDatabase.ImportAsset(path);
                        AssetDatabase.Refresh();
                        isHeightMap = true;
                    }
                    else { msg = " " + heightMap.name + " is not a heightmap"; }
                }
            }

            if (!isHeightMap && showErrors)
            {
                Debug.Log("LBTextureOperations.IsHeightMap " + msg);
            }

            return isHeightMap;
        }

        /// <summary>
        /// Save a texture into the existing project
        /// </summary>
        /// <param name="texture2D"></param>
        /// <param name="texturePath"></param>
        /// <param name="maxTextureSize"></param>
        /// <param name="isReadable"></param>
        /// <param name="showErrors"></param>
        public static void SaveTexture(Texture2D texture2D, string texturePath, int maxTextureSize, bool isReadable, bool showErrors)
        {
            // Do some basic validation
            if (texture2D == null) { Debug.LogWarning("LBTextureOperations.SaveTexture - texture is null"); }
            else if (string.IsNullOrEmpty(texturePath)) { Debug.LogWarning("LBTextureOperations.SaveTexture - texturePath is not defined"); }
            else if (!Mathf.IsPowerOfTwo(maxTextureSize))
            {
                if (showErrors) { Debug.LogError("LBTextureOperations.SaveTexture: maxTextureSize must be a power of 2. e.g. 64, 128, 256, 512, 1024, 2048, 8192"); }
            }
            else
            {
                // create a byte array in PNG format
                byte[] textureData = texture2D.EncodeToPNG();
                // Save the byte array to the disk file
                System.IO.File.WriteAllBytes(texturePath, textureData);
                AssetDatabase.Refresh();
                // Get the new texture so that attributes can be modified
                TextureImporter texImporter = (TextureImporter)AssetImporter.GetAtPath(texturePath);

#if UNITY_5_5_OR_NEWER
                texImporter.textureType = TextureImporterType.Default;
#else
            texImporter.textureType = TextureImporterType.Advanced;
#endif

                // Disable normal maps
                texImporter.convertToNormalmap = false;
                // Use TrueColor compression (our import methods don't work with Crunched settings)
                // Some other 8 and 16 bit compression algorithms (includind Unity default Automatic Compressed)
                // return slightly incorrect colour values. For example RGB 0, 153, 0 returns 0, 154, 0.
                // NOTE: Unity 5.5 doesn't support textureFormat property.
#if UNITY_5_5_OR_NEWER
                texImporter.textureCompression = TextureImporterCompression.Uncompressed;
#else
            texImporter.textureFormat = TextureImporterFormat.AutomaticTruecolor;
#endif
                if (maxTextureSize > 0) { texImporter.maxTextureSize = maxTextureSize; }
                // If required, make the new image Read/Write
                texImporter.isReadable = isReadable;
                AssetDatabase.Refresh();
                // Reveals the image that was just added or overwritten in the Project window
                LBEditorHelper.HighlightItemInProjectWindow(texturePath);
            }
        }

#endif

        #endregion

        public static bool IsTextureReadable(Texture2D texture, bool promptToFix)
        {
            bool isValid = false;

            try { Color colour = texture.GetPixel(0, 0); colour.r = colour.g; isValid = true; } // Use colour again to prevent warning message
            catch (UnityException e)
            {
                string msg = "Texture '" + texture.name + "' is not readable";
                if (e.Message.StartsWith(msg))
                {
                    if (promptToFix)
                    {
#if UNITY_EDITOR
                        msg += ". Do you want to fix it now?";
                        if (EditorUtility.DisplayDialog("Texure is not readable", msg, "Yes", "No"))
                        {
                            LBTextureOperations.EnableReadable(texture, true);
                        }
#endif
                    }
                }
            }
            return isValid;
        }

        /// <summary>
        /// Get the filename, including extension, given a full path to the file
        /// Example:
        /// string path = AssetDatabase.GetAssetPath(lbTerrainGrass.texture);
        /// string filename = LBTextureOperations.GetTextureFileNameFromPath(path);
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static string GetTextureFileNameFromPath(string filePath)
        {
            string fileName = string.Empty;

            if (!string.IsNullOrEmpty(filePath))
            {
                int startPos = filePath.LastIndexOf('/');
                if (startPos >= 0 && startPos < filePath.Length - 2)
                {
                    fileName = filePath.Substring(startPos + 1);
                }
            }

            return fileName;
        }

        /// <summary>
        /// Get a byte array of the colour pixels from one channel of a Texture2D
        /// Value Channels are R, G, B or A
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="colourChannel"></param>
        /// <returns></returns>
        public static byte[] GetTextureColourChannel(Texture2D texture, char colourChannel)
        {
            Color32[] pixels = texture.GetPixels32();
            byte[] colourChannelData = new byte[pixels.Length];
            if (colourChannel == 'R') { for (int i = 0; i < pixels.Length; i++) colourChannelData[i] = pixels[i].r; }
            if (colourChannel == 'G') { for (int i = 0; i < pixels.Length; i++) colourChannelData[i] = pixels[i].g; }
            if (colourChannel == 'B') { for (int i = 0; i < pixels.Length; i++) colourChannelData[i] = pixels[i].b; }
            if (colourChannel == 'A') { for (int i = 0; i < pixels.Length; i++) colourChannelData[i] = pixels[i].a; }
            return colourChannelData;
        }

        #endregion

        #region Texture Drawing Methods

        /// <summary>
        /// Draw a circle in an existing Texture2D starting from a circle centre point (cx, cy)
        /// with a radius (r) and a set colour.
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="cx"></param>
        /// <param name="cy"></param>
        /// <param name="r"></param>
        /// <param name="colour"></param>
        public static void DrawCircleSolid(Texture2D texture, int cx, int cy, int r, Color colour)
        {
            if (texture != null)
            {
                int x, y, px, nx, py, ny, dist;
                Color32[] colourArray = texture.GetPixels32();
                int texWidth = texture.width;
                int texHeight = texture.height;

                for (x = 0; x <= r; x++)
                {
                    dist = (int)Mathf.Ceil(Mathf.Sqrt(r * r - x * x));
                    for (y = 0; y <= dist; y++)
                    {
                        px = cx + x;
                        nx = cx - x;
                        py = cy + y;
                        ny = cy - y;

                        if (py >= 0 && py < texHeight)
                        {
                            if (px >= 0 && px < texWidth) { colourArray[py * texWidth + px] = colour; }
                            if (nx >= 0 && nx < texWidth) { colourArray[py * texWidth + nx] = colour; }
                        }
                        if (ny >= 0 && ny < texHeight)
                        {
                            if (px >= 0 && px < texWidth) { colourArray[ny * texWidth + px] = colour; }
                            if (nx >= 0 && nx < texWidth) { colourArray[ny * texWidth + nx] = colour; }
                        }
                    }
                }
                texture.SetPixels32(colourArray);
                texture.Apply();
            }
        }

        /// <summary>
        /// Draw a circle in an existing Texture2D starting from a circle centre point (cx, cy)
        /// with a radius (r). The centre is the primary colour, which gradients to Clear at the
        /// outer rim.
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="cx"></param>
        /// <param name="cy"></param>
        /// <param name="r"></param>
        /// <param name="primaryColour"></param>
        public static void DrawCircleGradient(Texture2D texture, int cx, int cy, int r, Color primaryColour)
        {
            if (texture != null)
            {
                int x, y, px, nx, py, ny, dist;
                float rF = r;
                float distFromCentreN = 0f;
                Color32[] colourArray = texture.GetPixels32();
                Color32 pixelColour = Color.clear;
                int texWidth = texture.width;
                int texHeight = texture.height;

                for (x = 0; x <= r; x++)
                {
                    dist = (int)Mathf.Ceil(Mathf.Sqrt(r * r - x * x));
                    //dist = (int)Mathf.RoundToInt(Mathf.Sqrt(r * r - x * x));

                    for (y = 0; y <= dist; y++)
                    {
                        px = cx + x;
                        nx = cx - x;
                        py = cy + y;
                        ny = cy - y;

                        // Get the normalised distance from the centre
                        distFromCentreN = Mathf.Clamp01(Mathf.Sqrt(x * x + y * y) / rF);

                        // Populate top half of the circle
                        if (ny >= 0 && ny < texHeight)
                        {
                            // Populate top left quadrant of circle
                            if (px >= 0 && px < texWidth)
                            {
                                pixelColour = colourArray[ny * texWidth + px];
                                colourArray[ny * texWidth + px] = Color32.Lerp(primaryColour, pixelColour, distFromCentreN);
                            }
                            // Populate top right quadrant of circle - ignore x = 0 which has already been updated
                            if (x > 0 && nx >= 0 && nx < texWidth)
                            {
                                pixelColour = colourArray[ny * texWidth + nx];
                                colourArray[ny * texWidth + nx] = Color32.Lerp(primaryColour, pixelColour, distFromCentreN);
                            }
                        }

                        // Populate bottom half of the circle - ignore y = 0 which has alredy been updated in top half of circle
                        if (y > 0 && py >= 0 && py < texHeight)
                        {
                            // Populate bottom left quadrant of circle
                            if (px >= 0 && px < texWidth)
                            {
                                // Fetch the original colour for this pixel
                                pixelColour = colourArray[py * texWidth + px];
                                // Apply the gradient to the existing pixel colour
                                colourArray[py * texWidth + px] = Color32.Lerp(primaryColour, pixelColour, distFromCentreN);
                            }
                            // Populate bottom right quadrant of circle - ignore x = 0 which has already been updated above
                            if (x > 0 && nx >= 0 && nx < texWidth)
                            {
                                pixelColour = colourArray[py * texWidth + nx];
                                colourArray[py * texWidth + nx] = Color32.Lerp(primaryColour, pixelColour, distFromCentreN);
                            }
                        }
                    }
                }
                texture.SetPixels32(colourArray);
                texture.Apply();
            }
        }

        /// <summary>
        /// Draw a circle in an existing Texture2D starting from a circle centre point (cx, cy)
        /// with a radius (r) and and subtract an amount or fraction from each pixel.
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="cx"></param>
        /// <param name="cy"></param>
        /// <param name="r"></param>
        /// <param name="subtractFraction"></param>
        public static void DrawCircleSubtract(Texture2D texture, int cx, int cy, int r, float subtractFraction)
        {
            if (texture != null)
            {
                int x, y, px, nx, py, ny, dist;
                float rF = r;
                float distFromCentreN, multiplier;
                Color32[] colourArray = texture.GetPixels32();
                int texWidth = texture.width;
                int texHeight = texture.height;

                for (x = 0; x <= r; x++)
                {
                    dist = (int)Mathf.Ceil(Mathf.Sqrt(r * r - x * x));
                    for (y = 0; y <= dist; y++)
                    {
                        px = cx + x;
                        nx = cx - x;
                        py = cy + y;
                        ny = cy - y;

                        // Get the normalised distance from the centre
                        distFromCentreN = Mathf.Clamp01(1f - (Mathf.Sqrt(x * x + y * y) / rF));

                        // Relative distance from centre is faster but doesn't have the best effect..
                        //distFromCentreN = Mathf.Clamp01(1f - (((x * x) + (y * y)) / (r * r)));

                        // Decrease the effect, the further it is from the centre of the circle
                        multiplier = 1f - (subtractFraction * distFromCentreN);

                        // Populate top half of the circle
                        if (ny >= 0 && ny < texHeight)
                        {
                            // Populate top left quadrant of circle
                            if (px >= 0 && px < texWidth)
                            {
                                // Get the current pixel, subtract the fraction of that current value, and update the pixel with the new value
                                // RoundToInt then Clamp would be more accurate but slower...
                                colourArray[ny * texWidth + px].a = (byte)Mathf.Clamp(colourArray[ny * texWidth + px].a * multiplier, 0f, 255f);
                            }
                            // Populate top right quadrant of circle - ignore x = 0 which has already been updated
                            if (x > 0 && nx >= 0 && nx < texWidth)
                            {
                                colourArray[ny * texWidth + nx].a = (byte)Mathf.Clamp(colourArray[ny * texWidth + nx].a * multiplier, 0f, 255f);
                            }
                        }

                        // Populate bottom half of the circle - ignore y = 0 which has alredy been updated in top half of circle
                        if (y > 0 && py >= 0 && py < texHeight)
                        {
                            // Populate bottom left quadrant of circle
                            if (px >= 0 && px < texWidth)
                            {
                                colourArray[py * texWidth + px].a = (byte)Mathf.Clamp(colourArray[py * texWidth + px].a * multiplier, 0f, 255f);
                            }
                            // Populate bottom right quadrant of circle - ignore x = 0 which has already been updated above
                            if (x > 0 && nx >= 0 && nx < texWidth)
                            {
                                colourArray[py * texWidth + nx].a = (byte)Mathf.Clamp(colourArray[py * texWidth + nx].a * multiplier, 0f, 255f);
                            }
                        }
                    }
                }
                texture.SetPixels32(colourArray);
                texture.Apply();
            }
        }

        /// <summary>
        /// Smooth a circle within a texture. blurQuality 1-7. blurStrength 0.0-1.0
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="cx"></param>
        /// <param name="cy"></param>
        /// <param name="r"></param>
        /// <param name="blurQuality"></param>
        /// <param name="blurStrength"></param>
        public static void SmoothCircle(Texture2D texture, int cx, int cy, int r, int blurQuality, float blurStrength)
        {
            if (texture != null)
            {
                int x, y, px, nx, py, ny, dist;
                Color32[] colourArray = texture.GetPixels32();
                int texWidth = texture.width;
                int texHeight = texture.height;

                float blurStrengthMultiplier = 1f;

                // The distance in pixels that each pixel gets blurred across (try values between 0.01 and 0.1)
                // The multiplication factor needs to be the same as used in LBStencilLayer.PaintLayerWithBrush(..)
                float blurStrengthRadius = Mathf.RoundToInt((r * 0.07f) * blurStrength);

                for (x = 0; x <= r; x++)
                {
                    dist = (int)Mathf.Ceil(Mathf.Sqrt(r * r - x * x));
                    for (y = 0; y <= dist; y++)
                    {
                        px = cx + x;
                        nx = cx - x;
                        py = cy + y;
                        ny = cy - y;

                        // Decrease the blur strength towards the edges of the circle
                        blurStrengthMultiplier = Mathf.Clamp01(1f - (((x * x) + (y * y)) / (r * r)));

                        if (py >= 0 && py < texHeight)
                        {
                            // Bottom left quarter
                            if (px >= 0 && px < texWidth) { colourArray[py * texWidth + px] = GaussianBlurTexturePoint(texture, ref colourArray, px, py, blurQuality, blurStrengthRadius, blurStrengthMultiplier); }
                            // Bottom right quarter
                            if (nx >= 0 && nx < texWidth) { colourArray[py * texWidth + nx] = GaussianBlurTexturePoint(texture, ref colourArray, nx, py, blurQuality, blurStrengthRadius, blurStrengthMultiplier); }
                        }
                        if (ny >= 0 && ny < texHeight)
                        {
                            // Top left quarter
                            if (px >= 0 && px < texWidth) { colourArray[ny * texWidth + px] = GaussianBlurTexturePoint(texture, ref colourArray, px, ny, blurQuality, blurStrengthRadius, blurStrengthMultiplier); }
                            // Top right quarter
                            if (nx >= 0 && nx < texWidth) { colourArray[ny * texWidth + nx] = GaussianBlurTexturePoint(texture, ref colourArray, nx, ny, blurQuality, blurStrengthRadius, blurStrengthMultiplier); }
                        }
                    }
                }
                texture.SetPixels32(colourArray);
                texture.Apply();
            }
        }

        #endregion

        #region Outline Texture Methods

        // Also see LBStencilLayer.OutlineTexture(..)


        /// <summary>
        /// Returns the bounding xy values of the outlined areas in the texture.
        /// Vector4.x = min x, y = min y, z = max x, w = max y.
        /// isNormalised will return the values in range 0.0 to 1.0
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="colour"></param>
        /// <param name="isNormalised"></param>
        /// <returns></returns>
        public static Vector4 TextureOutlineBounds(Texture2D texture, UnityEngine.Color colour, bool isNormalised)
        {
            Vector4 bounds = new Vector4(float.MaxValue, float.MaxValue, float.MinValue, float.MinValue);

            if (texture != null)
            {
                int texWidth = texture.width;
                int texHeight = texture.height;

                if (texWidth > 1 && texHeight > 1)
                {
                    Color[] colourArray = texture.GetPixels();
                    Color pixelColour;
                    int numPixels = (colourArray == null ? 0 : colourArray.Length);

                    if (numPixels == texWidth * texHeight)
                    {
                        for (int y = 0; y < texHeight; y++)
                        {
                            for (int x = 0; x < texWidth; x++)
                            {
                                pixelColour = colourArray[(y * texHeight) + x];
                                if (pixelColour == colour)
                                {
                                    // Check for min/max on x-axis
                                    if ((float)x < bounds.x) { bounds.x = (float)x; }
                                    else if ((float)x > bounds.z) { bounds.z = (float)x; }

                                    // Check for min/max on z-axis
                                    if ((float)y < bounds.y) { bounds.y = (float)y; }
                                    else if ((float)y > bounds.w) { bounds.w = (float)y; }
                                }
                            }
                        }
                    }

                    // If required, normalise bounds
                    if (isNormalised && bounds.x < float.MaxValue)
                    {
                        bounds.x = bounds.x / (texture.width - 1);
                        bounds.y = bounds.y / (texture.height - 1);
                        bounds.z = bounds.z / (texture.width - 1);
                        bounds.w = bounds.w / (texture.height - 1);
                    }
                }
            }

            return bounds;
        }

        /// <summary>
        /// INCOMPLETE
        /// Typically called after LBStencilLayer.OutlineTexture(..) to return
        /// a list of polygon points which describe a single outline of a closed
        /// shape within a texture denoted by coloured line.
        /// polyPoints should be passed in as empty list paramater.
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="polyPoints"></param>
        /// <param name="colour"></param>
        public static void TextureToPolygon(Texture2D texture, List<Vector3> polyPoints, UnityEngine.Color colour)
        {
            if (polyPoints != null && texture != null)
            {
                polyPoints.Clear();

                //int texWidth = texture.width;
                //int texHeight = texture.height;

                //Color[] colourArray = texture.GetPixels();

            }
        }

        #endregion

        #region ColorLerpUnclamped

        /// <summary>
        /// From Eric Haines (Eric5h5)'s TextureScale
        /// http://wiki.unity3d.com/index.php/TextureScale
        /// </summary>
        /// <param name="c1"></param>
        /// <param name="c2"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Color ColorLerpUnclamped(Color c1, Color c2, float value)
        {
            return new Color(c1.r + (c2.r - c1.r) * value, c1.g + (c2.g - c1.g) * value, c1.b + (c2.b - c1.b) * value, c1.a + (c2.a - c1.a) * value);
        }

        #endregion
    }
}