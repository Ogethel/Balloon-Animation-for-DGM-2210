using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace LandscapeBuilder
{
    [System.Serializable]
    public class LBStencilLayer
    {
        // Class containing Stencil Layer

        #region Public Variables and Properties

        public string LayerName;
        public string GUID;
        public Texture2D compressedTexture;
        public LBStencil.LayerResolution layerResolution;
        public bool showLayerInScene;                       // make the layer visible in the scene view
        public bool showLayerInEditor;                      // expand the layer properties in the custom inspector within the editor
        public Color colourInEditor;

        // renderTexture and layerArray need to be serialized so they can be modified by
        // in Editor using SerializedProperty methods like MoveArrayElement.
        // However, we do wish to set them to null when not in use in the editor
        // so they don't make it into a playable game.
        public Texture2D renderTexture;
        [HideInInspector] public ushort[,] layerArray;

        /// <summary>
        /// layerArrayX gets round an issue with layerArray which is 2 dimensional and cannot be serialized
        /// into a LBTemplate. Hide in the inspector so user isn't tempted to expand it and freeze Unity Editor
        /// </summary>
        [HideInInspector] public ushort[] layerArrayX;

        #endregion

        #region Private Variables and Properties

        // How many metres is each layer pixel in the landscape? Needs to be updated before use
        private float layerArrayPixelSize = 0;
        private float renderTexturePixelSize = 0;

        #endregion

        #region Constructors

        // Basic class constructor
        public LBStencilLayer()
        {
            // Assign a unique identifier
            GUID = System.Guid.NewGuid().ToString();

            LayerName = "New Layer";
            compressedTexture = null;
            layerResolution = LBStencil.LayerResolution._1024x1024;
            showLayerInScene = true;
            showLayerInEditor = true;
            colourInEditor = Color.white;

            layerArray = null;
            renderTexture = null;
        }

        /// <summary>
        /// Constructor for cloning a stencil layer.
        /// NOTE: Clones the original GUID
        /// </summary>
        /// <param name="lbStencilLayer"></param>
        /// <param name="cloneCompressedTexture"></param>
        /// <param name="cloneLayerArray"></param>
        /// <param name="cloneRenderTexture"></param>
        public LBStencilLayer(LBStencilLayer lbStencilLayer, bool cloneCompressedTexture, bool cloneLayerArray, bool cloneRenderTexture)
        {
            this.GUID = lbStencilLayer.GUID;
            this.LayerName = lbStencilLayer.LayerName;

            if (cloneCompressedTexture && lbStencilLayer.compressedTexture != null)
            {
                this.compressedTexture = Texture2D.Instantiate(lbStencilLayer.compressedTexture);
            }
            else { this.compressedTexture = null; }

            if (cloneRenderTexture && lbStencilLayer.renderTexture != null) { this.renderTexture = Texture2D.Instantiate(lbStencilLayer.renderTexture); }
            else { this.renderTexture = null; }

            this.layerResolution = lbStencilLayer.layerResolution;
            this.showLayerInScene = lbStencilLayer.showLayerInScene;
            this.showLayerInEditor = lbStencilLayer.showLayerInEditor;
            this.colourInEditor = lbStencilLayer.colourInEditor;

            if (cloneLayerArray && lbStencilLayer.layerArray != null)
            {
                // Clone the array doing a deep copy (don't just copy the references)
                int layerArrayDimension1 = lbStencilLayer.layerArray.GetLength(0);
                int layerArrayDimension2 = lbStencilLayer.layerArray.GetLength(1);

                this.layerArray = new ushort[layerArrayDimension1, layerArrayDimension2];
                for (int x = 0; x < layerArrayDimension1; x++)
                {
                    for (int y = 0; y < layerArrayDimension2; y++)
                    {
                        this.layerArray[x, y] = lbStencilLayer.layerArray[x, y];
                    }
                }
            }
            else { layerArray = null; }
        }

        #endregion

        #region Non-Static Public Methods

        /// <summary>
        /// Allocate a new (default 2-dimensional) USHORT array.
        /// </summary>
        public void AllocLayerArray()
        {
            if (layerArray != null)
            {
                Debug.LogWarning("ERROR: LBStencilLayer.AllocLayerArray - array is already initialised");
            }
            else
            {
                // Allocate a new array
                layerArray = new ushort[(int)layerResolution, (int)layerResolution];
            }
        }

        /// <summary>
        /// Allocate a new (1-dimensional) USHORT array. Currently is used with LBTemplate.
        /// </summary>
        public void AllocLayerArrayX()
        {
            if (layerArrayX != null)
            {
                Debug.LogWarning("ERROR: LBStencilLayer.AllocLayerArrayX - array is already initialised");
            }
            else
            {
                // Allocate a new array
                layerArrayX = new ushort[(int)layerResolution * (int)layerResolution];
            }
        }

        /// <summary>
        /// Deallocate memory for the default 2-dimensional USHORT array
        /// </summary>
        public void DeallocLayerArray()
        {
            if (layerArray != null) { layerArray = null; }
        }

        /// <summary>
        /// Deallocate memory for the 1-dimensional USHORT array. Currently used with LBTemplate
        /// </summary>
        public void DeallocLayerArrayX()
        {
            if (layerArrayX != null) { layerArrayX = null; }
        }

        /// <summary>
        /// Change the resolution of the layer, copying the data to the modified layer array
        /// </summary>
        /// <param name="newLayerResolution"></param>
        public void ChangeLayerResolution(int newLayerResolution)
        {
            if (compressedTexture == null) { Debug.LogWarning("ERROR: LBStencilLayer.ChangeLayerResolution - compressedTexture is null"); }
            else
            {
                // Don't use layerResolution as the previous resolution - most reliable is the compress texture
                int currentResolution = compressedTexture.width;

                if (currentResolution != newLayerResolution)
                {
                    LBTextureOperations.TexturePointScale(compressedTexture, newLayerResolution, newLayerResolution);
                    if (layerArray != null)
                    {
                        DeallocLayerArray();
                        AllocLayerArray();
                        UnCompressToUShort();
                    }
                }
            }
        }

        /// <summary>
        /// Update the private layerPixelSize using the landscape width and layer resolution
        /// Update the private renderTexturePixelSize using the landscape with and render texture width
        /// </summary>
        /// <param name="landscapeWidth"></param>
        public void UpdatePixelSize(float landscapeWidth)
        {
            layerArrayPixelSize = landscapeWidth / (int)layerResolution;
            if (renderTexture == null) { renderTexturePixelSize = 0; }
            else { renderTexturePixelSize = landscapeWidth / renderTexture.width; }
        }

        /// <summary>
        /// Paint the LayerArray with a brush at the landscape normalised point, using the selected brush of a given brushSize
        /// pointN is the normalised point in the landscape
        /// </summary>
        /// <param name="pointN"></param>
        /// <param name="stencilBrushType"></param>
        /// <param name="brushSize"></param>
        /// <returns></returns>
        public bool PaintLayerWithBrush(Vector2 pointN, LBStencil.StencilBrushType stencilBrushType, float brushSize)
        {
            bool isSuccess = false;

            if (layerArray != null)
            {
                // Get centre point of brush in layerArray
                int _layerResolution = (int)layerResolution;
                int posX = Mathf.FloorToInt(pointN.x * (_layerResolution - 1));
                int posZ = Mathf.FloorToInt(pointN.y * (_layerResolution - 1));

                // The brush size is always in metres
                // Scale the brush radius to the layer USHORT array pixel size
                int brushRadius = Mathf.FloorToInt((brushSize / 2f) / layerArrayPixelSize);
                if (brushRadius < 1) { brushRadius = 1; }

                ushort pixelValue = 65535;
                float brushRadiusF = brushRadius;

                float blurStrengthMultiplier = 1f;

                // The distance in pixels that each pixel gets blurred across (try values between 0.01 and 0.1)
                // The multiplication factor needs to be the same as used in LBTextureOperation.SmoothCircle(..)
                float blurStrengthRadius = Mathf.RoundToInt(brushRadiusF * 0.07f);

                if (stencilBrushType == LBStencil.StencilBrushType.EraserCircleSolid) { pixelValue = 0; }

                int x, y, px, nx, py, ny, dist;
                float distFromCentreN = 0f;

                // NOTE: If subtractFactor for CircleSubtract value is changed, must also change value in PaintLayerRenderTextureWithBrush(..)
                float subtractFactor = 0.01f;
                float multiplier;

                for (x = 0; x <= brushRadius; x++)
                {
                    dist = (int)Mathf.Ceil(Mathf.Sqrt(brushRadius * brushRadius - x * x));

                    for (y = 0; y <= dist; y++)
                    {
                        px = posX + x;
                        nx = posX - x;
                        py = posZ + y;
                        ny = posZ - y;

                        if (stencilBrushType == LBStencil.StencilBrushType.CircleGradient)
                        {
                            // Get the normalised distance from the centre
                            distFromCentreN = Mathf.Clamp01(Mathf.Sqrt(x * x + y * y) / brushRadiusF);

                            // Populate top half of the circle
                            if (ny >= 0 && ny < _layerResolution)
                            {
                                // Populate top left quadrant of circle
                                if (px >= 0 && px < _layerResolution)
                                {
                                    pixelValue = layerArray[px, ny];
                                    layerArray[px, ny] = (ushort)Mathf.Lerp(65535f, (float)pixelValue, distFromCentreN);
                                }
                                // Populate top right quadrant of circle - ignore x = 0 which has already been updated
                                if (x > 0 && nx >= 0 && nx < _layerResolution)
                                {
                                    pixelValue = layerArray[nx, ny];
                                    layerArray[nx, ny] = (ushort)Mathf.Lerp(65535f, (float)pixelValue, distFromCentreN);
                                }
                            }

                            // Populate bottom half of the circle - ignore y = 0 which has alredy been updated in top half of circle
                            if (y > 0 && py >= 0 && py < _layerResolution)
                            {
                                // Populate bottom left quadrant of circle
                                if (px >= 0 && px < _layerResolution)
                                {
                                    // Fetch the original value for this pixel (0-65535)
                                    pixelValue = layerArray[px, py];
                                    // Apply the gradient to the existing pixel value
                                    layerArray[px, py] = (ushort)Mathf.Lerp(65535f, (float)pixelValue, distFromCentreN);
                                }
                                // Populate bottom right quadrant of circle - ignore x = 0 which has already been updated above
                                if (x > 0 && nx >= 0 && nx < _layerResolution)
                                {
                                    pixelValue = layerArray[nx, py];
                                    layerArray[nx, py] = (ushort)Mathf.Lerp(65535f, (float)pixelValue, distFromCentreN);
                                }
                            }
                        }
                        else if (stencilBrushType == LBStencil.StencilBrushType.CircleSubtract)
                        {
                            // Get the normalised distance from the centre
                            distFromCentreN = Mathf.Clamp01(1f - (Mathf.Sqrt(x * x + y * y) / brushRadiusF));

                            // Relative distance from centre is faster but doesn't have the best effect..
                            //distFromCentreN = Mathf.Clamp01(1f - (((x * x) + (y * y)) / (brushRadius * brushRadius)));

                            // Decrease the effect, the further it is from the centre of the circle
                            multiplier = 1f - (subtractFactor * distFromCentreN);

                            // Populate top half of the circle
                            if (ny >= 0 && ny < _layerResolution)
                            {
                                // Populate top left quadrant of circle
                                if (px >= 0 && px < _layerResolution)
                                {
                                    // Get the current pixel, subtract the fraction of that current value, and update the pixel with the new value
                                    // RoundToInt then Clamp would be more accurate but slower...
                                    layerArray[px, ny] = (ushort)Mathf.Clamp(layerArray[px, ny] * multiplier, 0f, 65535f);
                                }
                                // Populate top right quadrant of circle - ignore x = 0 which has already been updated
                                if (x > 0 && nx >= 0 && nx < _layerResolution)
                                {
                                    layerArray[nx, ny] = (ushort)Mathf.Clamp(layerArray[nx, ny] * multiplier, 0f, 65535f);
                                }
                            }

                            // Populate bottom half of the circle - ignore y = 0 which has alredy been updated in top half of circle
                            if (y > 0 && py >= 0 && py < _layerResolution)
                            {
                                // Populate bottom left quadrant of circle
                                if (px >= 0 && px < _layerResolution)
                                {
                                    layerArray[px, py] = (ushort)Mathf.Clamp(layerArray[px, py] * multiplier, 0f, 65535f);
                                }
                                // Populate bottom right quadrant of circle - ignore x = 0 which has already been updated above
                                if (x > 0 && nx >= 0 && nx < _layerResolution)
                                {
                                    layerArray[nx, py] = (ushort)Mathf.Clamp(layerArray[nx, py] * multiplier, 0f, 65535f);
                                }
                            }
                        }
                        else if (stencilBrushType == LBStencil.StencilBrushType.CircleSmooth)
                        {
                            // Decrease the blur strength towards the edges of the circle
                            blurStrengthMultiplier = Mathf.Clamp01(1f - (((x * x) + (y * y)) / (brushRadius * brushRadius)));

                            // NOTE: If blurQuality parameter value is changed, must also change value in PaintLayerRenderTextureWithBrush(..)
                            if (py >= 0 && py < _layerResolution)
                            {
                                if (px >= 0 && px < _layerResolution) { GaussianBlurPoint(px, py, 5, blurStrengthRadius, blurStrengthMultiplier); }
                                if (nx >= 0 && nx < _layerResolution) { GaussianBlurPoint(nx, py, 5, blurStrengthRadius, blurStrengthMultiplier); }
                            }

                            if (ny >= 0 && ny < _layerResolution)
                            {
                                if (px >= 0 && px < _layerResolution) { GaussianBlurPoint(px, ny, 5, blurStrengthRadius, blurStrengthMultiplier); }
                                if (nx >= 0 && nx < _layerResolution) { GaussianBlurPoint(nx, ny, 5, blurStrengthRadius, blurStrengthMultiplier); }
                            }
                        }
                        // CircleSolid or EraserCircleSolid brush
                        else
                        {
                            if (py >= 0 && py < _layerResolution)
                            {
                                if (px >= 0 && px < _layerResolution) { layerArray[px, py] = pixelValue; }
                                if (nx >= 0 && nx < _layerResolution) { layerArray[nx, py] = pixelValue; }
                            }

                            if (ny >= 0 && ny < _layerResolution)
                            {
                                if (px >= 0 && px < _layerResolution) { layerArray[px, ny] = pixelValue; }
                                if (nx >= 0 && nx < _layerResolution) { layerArray[nx, ny] = pixelValue; }
                            }
                        }
                    }
                }

                isSuccess = true;
            }
            else { Debug.LogWarning("ERROR: lbStencilLayer.PaintLayerWithBrush layerArray is null in " + LayerName); }

            return isSuccess;
        }

        /// <summary>
        /// Paint the Render Texture with a brush at the landscape normalised point, using the selected brush of a given brushSize.
        /// pointN is the normalised point in the landscape.
        /// </summary>
        /// <param name="pointN"></param>
        /// <param name="stencilBrushType"></param>
        /// <param name="brushSize"></param>
        /// <returns></returns>
        public bool PaintLayerRenderTextureWithBrush(Vector2 pointN, LBStencil.StencilBrushType stencilBrushType, float brushSize)
        {
            bool isSuccess = false;

            if (renderTexture != null)
            {
                // Get the zero-based max width based on the width of the render texture
                int maxWidthValue = renderTexture.width - 1;
                // The render texture (for some reason) has the x values reversed.
                int rendTexX = Mathf.FloorToInt((1f - pointN.x) * maxWidthValue);
                int rendTexZ = Mathf.FloorToInt(pointN.y * maxWidthValue);

                // The brush size is always in metres
                // Scale the brush radius to the layer USHORT array pixel size
                int brushRadius = Mathf.FloorToInt((brushSize / 2f) / renderTexturePixelSize);
                if (brushRadius < 1) { brushRadius = 1; }

                switch (stencilBrushType)
                {
                    case LBStencil.StencilBrushType.CircleSolid:
                        LBTextureOperations.DrawCircleSolid(renderTexture, rendTexX, rendTexZ, brushRadius, colourInEditor);
                        break;
                    case LBStencil.StencilBrushType.CircleGradient:
                        LBTextureOperations.DrawCircleGradient(renderTexture, rendTexX, rendTexZ, brushRadius, colourInEditor);
                        break;
                    case LBStencil.StencilBrushType.EraserCircleSolid:
                        LBTextureOperations.DrawCircleSolid(renderTexture, rendTexX, rendTexZ, brushRadius, Color.clear);
                        break;
                    case LBStencil.StencilBrushType.CircleSmooth:
                        // NOTE: If blurQuality or blurStength is changed, must also change values in PaintLayerWithBrush(..)
                        LBTextureOperations.SmoothCircle(renderTexture, rendTexX, rendTexZ, brushRadius, 5, 1f);
                        break;
                    case LBStencil.StencilBrushType.CircleSubtract:
                        // NOTE: If subtraction parameter value is changed, must also change value in PaintLayerWithBrush(..)
                        LBTextureOperations.DrawCircleSubtract(renderTexture, rendTexX, rendTexZ, brushRadius, 0.01f);
                        break;
                    default:
                        LBTextureOperations.DrawCircleSolid(renderTexture, rendTexX, rendTexZ, brushRadius, colourInEditor);
                        break;
                }
            }

            return isSuccess;
        }

        /// <summary>
        /// Blur or smooth a point using a Gaussian Blur. This is an adaption of the BlurPass used
        /// in LBImageFX. blurQuality 1-7. blurStrength 0.0-1.0
        /// </summary>
        /// <param name="ptX"></param>
        /// <param name="ptY"></param>
        /// <param name="blurQuality"></param>
        /// <param name="blurStrength"></param>
        public void GaussianBlurPoint(int ptX, int ptY, int blurQuality, float blurStrength, float blurMultiplier)
        {
            if (layerArray != null)
            {
                int sx, sy;
                float denominator = 0f;
                float gd = 0f;
                float blurredPixelN = 0f, samplePixelN, pixelValue;
                float blurAmount = blurStrength / blurQuality;
                int layerArrayDimension1 = (int)layerResolution;
                int layerArrayDimension2 = (int)layerResolution;

                float originalPixelN = layerArray[ptX, ptY] / 65535f;

                // Loop through a grid of surrounding pixels (including the original one)
                for (int bx = -blurQuality; bx <= blurQuality; bx++)
                {
                    for (int by = -blurQuality; by <= blurQuality; by++)
                    {
                        // Sample the given pixel
                        sx = (int)(ptX + bx * blurAmount);
                        sy = (int)(ptY + by * blurAmount);

                        if (sx >= 0 && sx < layerArrayDimension1 && sy >= 0 && sy < layerArrayDimension2)
                        {
                            samplePixelN = layerArray[sx, sy] / 65535f;

                            gd = LBTextureOperations.GaussianDistribution((float)bx, 7.0f);
                            blurredPixelN += (samplePixelN * gd);
                            denominator += gd;
                        }
                    }
                }

                // If no samples were added no need to update the pixel
                if (denominator >= 0.001f)
                {
                    // Divide by total strength of samples
                    blurredPixelN /= denominator;

                    pixelValue = Mathf.Lerp(originalPixelN, blurredPixelN, blurMultiplier) * 65535f;
                    if (pixelValue < 0) { layerArray[ptX, ptY] = (ushort)0; }
                    else if (pixelValue > 65535f) { layerArray[ptX, ptY] = (ushort)65535; }
                    else { layerArray[ptX, ptY] = (ushort)Mathf.RoundToInt(pixelValue); }
                }
            }
        }

        /// <summary>
        /// Blur a layer using a Gaussian Blur. This is an adaption of the BlurPass used
        /// in LBImageFX. blurQuality 1-7. blurStrength 0.0-1.0
        /// </summary>
        /// <param name="blurQuality"></param>
        /// <param name="blurStrength"></param>
        /// <returns></returns>
        public bool GaussianBlurLayer(int blurQuality, float blurStrength)
        {
            bool isSuccess = false;

            if (layerArray != null)
            {
                //int _layerResolution = (int)layerResolution;
                int layerArrayDimension1 = layerArray.GetLength(0);
                int layerArrayDimension2 = layerArray.GetLength(1);

                // Validate the dimensions
                if (layerArrayDimension1 != (int)layerResolution || layerArrayDimension2 != (int)layerResolution)
                {
                    Debug.LogWarning("ERROR: LBStencilLayer.GaussianBlurLayer - layer array does not match the layer resolution for " + LayerName);
                }
                else if (blurQuality < 1 || blurQuality > 7) { Debug.LogWarning("ERROR: LBStencilLayer.GaussianBlurLayer - blurQuality must be between 1 and 7"); }
                else if (blurStrength < 0f || blurStrength > 1f) { Debug.LogWarning("ERROR: LBStencilLayer.GaussianBlurLayer - blurStrength must be between 0.0 an 1.0"); }
                else
                {
                    //#if UNITY_5_3
                    //UnityEngine.Random.seed = 0;
                    //#else
                    //UnityEngine.Random.InitState(0);
                    //#endif

                    float radius = 250f * blurStrength;
                    float blurAmount = (radius * 0.07f) / blurQuality;
                    float gd, denominator, pixelValue;
                    //float blurAmountVariable;

                    // Represented as normalised fraction of 65535 (0 = 0.0, 65535 = 1.0)
                    float blurredPixelN = 0f, samplePixelN = 0f;

                    // sample coordinates
                    int sx, sy;

                    for (int x = 0; x < layerArrayDimension1; x++)
                    {
                        for (int y = 0; y < layerArrayDimension2; y++)
                        {
                            denominator = 0f;
                            gd = 0f;
                            blurredPixelN = 0f;

                            // The idea of the variable blurAmount is to reduce stripping...
                            //blurAmountVariable = blurAmount + UnityEngine.Random.Range(0f, 0.2f);

                            // Loop through a grid of surrounding pixels (including the original one)
                            for (int bx = -blurQuality; bx <= blurQuality; bx++)
                            {
                                for (int by = -blurQuality; by <= blurQuality; by++)
                                {
                                    // Sample the given pixel
                                    sx = (int)(x + bx * blurAmount);
                                    sy = (int)(y + by * blurAmount);

                                    if (sx >= 0 && sx < layerArrayDimension1 && sy >= 0 && sy < layerArrayDimension2)
                                    {
                                        samplePixelN = layerArray[sx, sy] / 65535f;

                                        gd = LBTextureOperations.GaussianDistribution((float)bx, 7.0f);
                                        blurredPixelN += (samplePixelN * gd);
                                        denominator += gd;
                                    }
                                }
                            }

                            // If no samples were added no need to update the pixel
                            if (denominator >= 0.001f)
                            {
                                // Divide by total strength of samples
                                blurredPixelN /= denominator;

                                pixelValue = blurredPixelN * 65535f;
                                if (pixelValue < 0) { layerArray[x, y] = (ushort)0; }
                                else if (pixelValue > 65535f) { layerArray[x, y] = (ushort)65535; }
                                else { layerArray[x, y] = (ushort)Mathf.RoundToInt(pixelValue); }
                            }
                        }
                    }

                    isSuccess = true;
                }
            }
            else { Debug.LogWarning("ERROR: lbStencilLayer.GaussianBlurLayer layerArray is null in " + LayerName); }

            return isSuccess;
        }

        /// <summary>
        /// Converts the default 2-dimensional ushort array to a compressed texture
        /// </summary>
        public void CompressFromUShort()
        {
            // If an existing compressed texture is not the same resolution
            // as the current setting, delete the texture.
            if (compressedTexture != null)
            {
                if (compressedTexture.width != (int)layerResolution)
                {
                    compressedTexture = null;
                }
            }

            // If the texture doesn't exist, create a new one
            if (compressedTexture == null)
            {
                compressedTexture = LBTextureOperations.CreateTexture((int)layerResolution, (int)layerResolution, Color.clear, false, TextureFormat.ARGB32, false);
            }

            // Check that we were able to create a texture or that a valid one already existed.
            if (compressedTexture == null) { Debug.LogWarning("ERROR: LBStencilLayer.CompressFromUShort - Could not create a new texture for " + LayerName); }
            else if (layerArray == null) { Debug.LogWarning("ERROR: LBStencilLayer.CompressFromUShort - layerArray is null for " + LayerName); }
            else
            {
                int layerArrayDimension1 = layerArray.GetLength(0);
                int layerArrayDimension2 = layerArray.GetLength(1);

                // Update the compress texture name to match the layer name
                compressedTexture.name = "LBCompressedTexture " + LayerName;

                // Validate the dimensions
                if (layerArrayDimension1 != (int)layerResolution || layerArrayDimension2 != (int)layerResolution)
                {
                    Debug.LogWarning("ERROR: LBStencilLayer.CompressFromUShort - layer resolution is not the same as compressed texture size for " + LayerName);
                }
                else
                {
                    Color pixelColour = new Color();
                    ushort arrayPixel = (ushort)0;

                    // Fetch all the pixels into a temporary array. Uses more memory
                    // but should be more performant
                    Color[] compressedArray = compressedTexture.GetPixels();

                    // copy the array data into the texture
                    for (int x = 0; x < layerArrayDimension1; x++)
                    {
                        for (int y = 0; y < layerArrayDimension2; y++)
                        {
                            arrayPixel = layerArray[x, y];
                            // Compress the ushort values into the Red and Green channels
                            pixelColour.r = Mathf.Floor(arrayPixel / 256f) / 255f;
                            pixelColour.g = (arrayPixel % 256f) / 255f;

                            compressedArray[(y * layerArrayDimension2) + x] = pixelColour;
                        }
                    }

                    // Copy all the updated pixels back into the texture
                    compressedTexture.SetPixels(compressedArray);
                    compressedTexture.Apply();
                }
            }
        }

        /// <summary>
        /// Converts the 1-dimensional ushort arrayX to a compressed texture.
        /// Currently used with LBTemplate.
        /// </summary>
        public void CompressFromUShortX()
        {
            // If an existing compressed texture is not the same resolution
            // as the current setting, delete the texture.
            if (compressedTexture != null)
            {
                if (compressedTexture.width != (int)layerResolution)
                {
                    compressedTexture = null;
                }
            }

            // If the texture doesn't exist, create a new one
            if (compressedTexture == null)
            {
                compressedTexture = LBTextureOperations.CreateTexture((int)layerResolution, (int)layerResolution, Color.clear, false, TextureFormat.ARGB32, false);
            }

            // Check that we were able to create a texture or that a valid one already existed.
            if (compressedTexture == null) { Debug.LogWarning("ERROR: LBStencilLayer.CompressFromUShortX - Could not create a new texture for " + LayerName); }
            else if (layerArrayX == null) { Debug.LogWarning("ERROR: LBStencilLayer.CompressFromUShortX - layerArrayX is null for " + LayerName); }
            else
            {
                int layerArrayDimension = layerArrayX.GetLength(0);

                // Update the compress texture name to match the layer name
                compressedTexture.name = "LBCompressedTexture " + LayerName;

                // Validate the dimensions
                if (layerArrayDimension != (int)layerResolution * (int)layerResolution)
                {
                    Debug.LogWarning("ERROR: LBStencilLayer.CompressFromUShortX - layer resolution is not the same as compressed texture size for " + LayerName);
                }
                else
                {
                    Color pixelColour = new Color();
                    ushort arrayPixel = (ushort)0;

                    // Fetch all the pixels into a temporary array. Uses more memory
                    // but should be more performant
                    Color[] compressedArray = compressedTexture.GetPixels();

                    // copy the array data into the texture
                    for (int x = 0; x < layerArrayDimension; x++)
                    {
                        arrayPixel = layerArrayX[x];
                        // Compress the ushort values into the Red and Green channels
                        pixelColour.r = Mathf.Floor(arrayPixel / 256f) / 255f;
                        pixelColour.g = (arrayPixel % 256f) / 255f;

                        compressedArray[x] = pixelColour;
                    }

                    // Copy all the updated pixels back into the texture
                    compressedTexture.SetPixels(compressedArray);
                    compressedTexture.Apply();
                }
            }
        }

        /// <summary>
        /// Uncompress the texture into the (default 2 dimensional) USHORT layerArray.
        /// </summary>
        public void UnCompressToUShort()
        {
            if (compressedTexture != null)
            {
                if (layerArray == null) { Debug.LogWarning("ERROR: LBStencilLayer.UnCompressToUShort - layer array is not defined for " + LayerName); }
                else
                {
                    int layerArrayDimension1 = layerArray.GetLength(0);
                    int layerArrayDimension2 = layerArray.GetLength(1);

                    // Validate the dimensions
                    if (layerArrayDimension1 != (int)layerResolution || layerArrayDimension2 != (int)layerResolution)
                    {
                        Debug.LogWarning("ERROR: LBStencilLayer.UnCompressToUShort - layer array does not match the layer resolution for " + LayerName);
                    }
                    else if (layerArrayDimension1 != compressedTexture.width)
                    {
                        Debug.LogWarning("ERROR: LBStencilLayer.UnCompressToUShort - layer resolution (" + layerArrayDimension1 + "x" + layerArrayDimension2 + ") is not the same as compressed texture size (" + compressedTexture.width + "x" + compressedTexture.width + ") for " + LayerName);
                    }
                    else
                    {
                        Color pixelColour = new Color();

                        // Fetch all the pixels into a temporary array. Uses more memory
                        // but should be more performant
                        Color[] compressedArray = compressedTexture.GetPixels();

                        // uncompress the texture into the ushort array
                        for (int x = 0; x < layerArrayDimension1; x++)
                        {
                            for (int y = 0; y < layerArrayDimension2; y++)
                            {
                                pixelColour = compressedArray[(y * layerArrayDimension2) + x];

                                layerArray[x, y] = (ushort)((pixelColour.r * 255f * 256f) + (pixelColour.g * 255f));
                            }
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning("ERROR: LBStencilLayer.UnCompressToUShort - compress texture is null for " + LayerName);
            }
        }

        /// <summary>
        /// Uncompress the texture into the USHORT layerArrayX single dimensional array.
        /// </summary>
        public void UnCompressToUShortX()
        {
            if (compressedTexture != null)
            {
                if (layerArrayX == null) { Debug.LogWarning("ERROR: LBStencilLayer.UnCompressToUShortX - layerArrayX is not defined for " + LayerName); }
                else
                {
                    int layerArrayDimension = layerArrayX.GetLength(0);

                    // Validate the dimension
                    if (layerArrayDimension != (int)layerResolution * (int)layerResolution)
                    {
                        Debug.LogWarning("ERROR: LBStencilLayer.UnCompressToUShortX - layer array does not match the layer resolution for " + LayerName);
                    }
                    else if (layerArrayDimension != compressedTexture.width * compressedTexture.height)
                    {
                        Debug.LogWarning("ERROR: LBStencilLayer.UnCompressToUShortX - layer resolution (" + layerArrayDimension + "x" + layerArrayDimension + ") is not the same as compressed texture size (" + compressedTexture.width + "x" + compressedTexture.height + ") for " + LayerName);
                    }
                    else
                    {
                        Color pixelColour = new Color();

                        // Fetch all the pixels into a temporary array. Uses more memory
                        // but should be more performant
                        Color[] compressedArray = compressedTexture.GetPixels();

                        // uncompress the texture into the ushort array
                        for (int x = 0; x < layerArrayDimension; x++)
                        {
                            pixelColour = compressedArray[x];
                            layerArrayX[x] = (ushort)((pixelColour.r * 255f * 256f) + (pixelColour.g * 255f));
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning("ERROR: LBStencilLayer.UnCompressToUShortX - compress texture is null for " + LayerName);
            }
        }

        /// <summary>
        /// Copy the UShort array data into the temporary RenderTexture.
        /// Typically this would be done when a layer is first displayed in the scene.
        /// If render texture resolution is higher than layer resolution, downsize then
        /// upsize render texture to display "missing" pixels.
        /// </summary>
        public void CopyUShortToRenderTexture()
        {
            // Validation
            if (layerArray == null) { Debug.LogWarning("ERROR: LBStencilLayer.CopyUShortToRenderTexture - layer array is not defined for " + LayerName); }
            else if (renderTexture == null) { Debug.LogWarning("ERROR: LBStencilLayer.CopyUShortToRenderTexture - render texture is not defined for " + LayerName); }
            else
            {
                int posX, posZ;

                int layerArrayDimension1 = layerArray.GetLength(0);
                int layerArrayDimension2 = layerArray.GetLength(1);

                int originalRenderTextureWidth = renderTexture.width;
                int originalRenderTextueHeight = renderTexture.height;

                if (layerArrayDimension1 < renderTexture.width)
                {
                    // Resize the texture to same size as layer - which also clears all pixel data
                    renderTexture.Resize(layerArrayDimension1, layerArrayDimension2);
                }

                ushort arrayPixel = (ushort)0;
                Vector2 pointN = Vector2.zero;
                Color pixelColour = Color.black;
                Color[] texColourArray = renderTexture.GetPixels();

                // Get the 0-based max width and height of the texture
                int maxWidthValue = renderTexture.width - 1;
                int maxHeightValue = renderTexture.height - 1;

                //Debug.Log("LBStencilLayer.CopyUShortToRenderTexture for " + LayerName);

                for (int x = 0; x < layerArrayDimension1; x++)
                {
                    for (int y = 0; y < layerArrayDimension2; y++)
                    {
                        arrayPixel = layerArray[x, y];

                        // Get the normalised position
                        pointN.x = (float)x / (float)(layerArrayDimension1 - 1);
                        pointN.y = (float)y / (float)(layerArrayDimension2 - 1);

                        // Get the point in the render texture
                        posX = Mathf.RoundToInt((1f - pointN.x) * maxWidthValue);
                        posZ = Mathf.RoundToInt(pointN.y * maxHeightValue);

                        // Set the colour, then lerp the alpha as the shader takes care of the rest
                        pixelColour = this.colourInEditor;
                        pixelColour.a = arrayPixel / 65535f;

                        texColourArray[(posZ * renderTexture.height) + posX] = pixelColour;
                    }
                }

                renderTexture.SetPixels(texColourArray);
                renderTexture.Apply();

                if (originalRenderTextureWidth != renderTexture.width)
                {
                    // Upscale the render texture
                    LBTextureOperations.TexturePointScale(renderTexture, originalRenderTextureWidth, originalRenderTextueHeight);
                }
            }
        }

        /// <summary>
        /// Copy the UShort array data into a texture. Typically used to create
        /// a texture Map to be saved into the project folder.
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="isFlipX"></param>
        /// <param name="isFlipY"></param>
        /// <returns></returns>
        public bool CopyUShortToTexture(Texture2D texture, bool isFlipX, bool isFlipY)
        {
            bool isSuccess = false;

            // Validation
            if (layerArray == null) { Debug.LogWarning("ERROR: LBStencilLayer.CopyUShortToTexture - layer array is not defined for " + LayerName); }
            else if (texture == null) { Debug.LogWarning("ERROR: LBStencilLayer.CopyUShortToTexture - destination texture is not defined for " + LayerName); }
            else
            {
                int posX, posZ;

                int layerArrayDimension1 = layerArray.GetLength(0);
                int layerArrayDimension2 = layerArray.GetLength(1);

                ushort arrayPixel = (ushort)0;
                Vector2 pointN = Vector2.zero;
                Color pixelColour = Color.clear;
                Color[] texColourArray = texture.GetPixels();

                // Get the 0-based max width and height of the texture
                int maxWidthValue = texture.width - 1;
                int maxHeightValue = texture.height - 1;

                for (int x = 0; x < layerArrayDimension1; x++)
                {
                    for (int y = 0; y < layerArrayDimension2; y++)
                    {
                        arrayPixel = layerArray[x, y];

                        // Get the normalised position
                        pointN.x = (float)x / (float)(layerArrayDimension1 - 1);
                        pointN.y = (float)y / (float)(layerArrayDimension2 - 1);

                        // Get the point in the texture
                        if (isFlipX) { posX = Mathf.RoundToInt((1f - pointN.x) * maxWidthValue); }
                        else { posX = Mathf.RoundToInt(pointN.x * maxWidthValue); }

                        if (isFlipY) { posZ = Mathf.RoundToInt((1f - pointN.y) * maxHeightValue); }
                        else { posZ = Mathf.RoundToInt(pointN.y * maxHeightValue); }

                        // Use lerpcolour to display the weighted colour
                        if (arrayPixel == (ushort)0) { pixelColour = Color.clear; }
                        //else { pixelColour = Color.Lerp(Color.clear, this.colourInEditor, arrayPixel / 65535f); }
                        else { pixelColour = Color.Lerp(Color.clear, Color.white, arrayPixel / 65535f); }

                        texColourArray[(posZ * texture.height) + posX] = pixelColour;
                    }
                }

                texture.SetPixels(texColourArray);
                texture.Apply();
                isSuccess = true;
            }

            return isSuccess;
        }

        /// <summary>
        /// Populate the supplied Texture2D with outline edge
        /// contours around painted areas in the stencil layer.
        /// Paint the edge with the given colour.
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="colour"></param>
        /// <returns></returns>
        public bool OutlineTexture(Texture2D texture, UnityEngine.Color colour)
        {
            bool isSuccess = false;
            string methodName = "LBStencilLayer.OutlineTexture";

            if (compressedTexture == null) { Debug.LogWarning("ERROR: " + methodName + " - compressedTexture is not defined for " + LayerName); }
            else if (texture == null) { Debug.LogWarning("ERROR: " + methodName + " - destination texture is not defined for " + LayerName); }
            else
            {
                int texWidth = texture.width;
                int texHeight = texture.height;
                int currentResolution = compressedTexture.width;

                if (texWidth != currentResolution || texHeight != currentResolution) { Debug.LogWarning("ERROR: " + methodName + " - destination texture dimensions " + texWidth + "x" + texHeight + " do not match " + LayerName + " resolution of " + currentResolution + "x" + currentResolution); }
                else
                {
                    Color[] texColourArray = texture.GetPixels();
                    Color[] compressedArray = compressedTexture.GetPixels();
                    Color pixelColour;
                    int paintedValue = 0;  // 0 = unpainted, 1 = painted

                    // Convert compressedArray into a 1D array of 0 (unpainted) or 1 (painted) values
                    int size1D = texWidth * texHeight;
                    int[] paintedArray = new int[size1D];
                    for (int pIdx = 0; pIdx < size1D; pIdx++)
                    {
                        pixelColour = compressedArray[pIdx];
                        if (pixelColour.r > 0f || pixelColour.g > 0f) { paintedArray[pIdx] = 1; }
                        else paintedArray[pIdx] = 0;
                    }

                    for (int y = 1; y < texHeight - 1; y++)
                    {
                        for (int x = 1; x < texWidth - 1; x++)
                        {
                            paintedValue = paintedArray[(y * texHeight) + x];
                            // Any value > 0 is considered painted.
                            if (paintedValue == 1)
                            {
                                // Get surrounding pixels
                                // x - 1, y
                                // x + 1, y
                                // x, y - 1
                                // x, y + 1
                                if (paintedArray[(y * texHeight) + x - 1] == 0 ||
                                    paintedArray[(y * texHeight) + x + 1] == 0 ||
                                    paintedArray[((y-1) * texHeight) + x] == 0 ||
                                    paintedArray[((y + 1) * texHeight) + x] == 0 )
                                {
                                    //texColourArray[(y * texHeight) + x] = colour;
                                    // stencil compressed texture has y-axis inverted
                                    texColourArray[((texHeight - 1 - y) * texHeight) + x] = colour;
                                }
                            }
                        }
                    }
                    texture.SetPixels(texColourArray);
                    texture.Apply();
                    isSuccess = true;
                }
            }

            return isSuccess;
        }

        /// <summary>
        /// Create a new render texture if required. The resolution is set at the
        /// lbStencil level. We may not always wish to refresh the data from the USHORT
        /// array if a new render texture is required.
        /// </summary>
        /// <param name="renderResolution"></param>
        /// <param name="refreshDataIfRequired"></param>
        /// <returns></returns>
        public bool ValidateRenderTexture(int renderResolution, bool refreshDataIfRequired)
        {
            bool isSuccessful = false;
            bool reloadTextureData = false;

            if (renderTexture != null)
            {
                if (renderTexture.width != renderResolution)
                {
                    //Debug.Log("INFO: LBStencilLayer.CreateRenderTexture - render texture size (" + renderTexture.width + "x" + renderTexture.width + ") is incorrect for " + LayerName);
                    renderTexture = null;
                    reloadTextureData = true;
                }
                else { isSuccessful = true; }
            }

            // If the rendertexture doesn't already exist, create it now
            if (renderTexture == null)
            {
                renderTexture = LBTextureOperations.CreateTexture(renderResolution, renderResolution, Color.clear, false, TextureFormat.ARGB32, false);

                if (renderTexture != null)
                {
                    renderTexture.name = LayerName + "_renderTexture";
                    //Debug.Log("INFO: LBStencilLayer.CreateRenderTexture - created layer temporary render texture: " + renderTexture.name + " for " + LayerName);

                    // If the render resolution has changed, will need to reload the texture data from the array
                    if (reloadTextureData && refreshDataIfRequired)
                    {
                        CopyUShortToRenderTexture();
                    }
                    isSuccessful = true;
                }
                else
                {
                    Debug.LogWarning("ERROR: LBStencilLayer.CreateRenderTexture- could not create render texture for " + LayerName);
                }
            }

            return isSuccessful;
        }

        #endregion

        #region Static Public Methods

        #endregion
    }
}