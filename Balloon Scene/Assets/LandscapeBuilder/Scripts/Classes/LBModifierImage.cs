using UnityEngine;
using System.Collections;

namespace LandscapeBuilder
{
    /// <summary>
    /// This class can be used to pass extra information to the LBLandscapeTerrain.ImageModifier() method
    /// </summary>
    public class LBModifierImage
    {
        public Texture2D heightmap;
        public float heightOffset;
        private Vector2 imageGrayscaleMinMax;
        /// <summary>
        /// Min and Max of the grayscale values in the image
        /// Will also set imageGrayscaleRange
        /// </summary>
        public Vector2 ImageGrayscaleMinMax
        {
            get { return imageGrayscaleMinMax; }
            set { imageGrayscaleMinMax = value; imageGrayscaleRange = Mathf.Abs(imageGrayscaleMinMax.y - imageGrayscaleMinMax.x); }
        }
        public float imageGrayscaleRange;
        public float blendRate;
        public bool useNoise;

        public int noiseOctaves;
        public float noiseTileSize;
        public bool noiseRidged;
        public float noiseHeightScale;

        public LBModifierOperations.ModifierLandformCategory modifierLandformCategory;
        public float rotationDegrees;

        // Constructor
        public LBModifierImage(Texture2D Heightmap, float HeightOffset, Vector2 ImageGrayscaleRange, float BlendRate, bool UseNoise)
        {
            this.heightmap = Heightmap;
            this.heightOffset = HeightOffset;
            this.ImageGrayscaleMinMax = ImageGrayscaleRange;
            this.blendRate = BlendRate;
            this.useNoise = UseNoise;
        }

        /// <summary>
        /// RotateImage takes a source Texture and rotates it x Degrees, then returns
        /// the rotated texture.
        /// WARNING: This currently moves the Landform upwards and then as rotation angle
        ///          increases, more of the lanform goes outside the boundaries and is cut off.
        /// </summary>
        /// <param name="SourceImage"></param>
        /// <param name="RotationAngle"></param>
        /// <returns></returns>
        public static Texture2D RotateImage(Texture2D SourceImage, float RotationAngle)
        {
            Texture2D outputImage = null;

            if (SourceImage == null)
            {
                Debug.LogWarning("LBModiferImage RotateImage - source image cannot be null");
            }
            else if (SourceImage.width < 1 || SourceImage.height < 1)
            {
                Debug.LogWarning("LBModiferImage RotateImage - source image must be at least 1x1");
            }
            else if (SourceImage.width != SourceImage.height)
            {
                Debug.LogWarning("LBModiferImage RotateImage - source image must be square");
            }
            else
            {
                outputImage = new Texture2D(SourceImage.width, SourceImage.height);

                int width = SourceImage.width;
                int height = SourceImage.height;
                float angle = -RotationAngle;
                float angleRad = Mathf.Deg2Rad * angle;
                float d = width / 2f;

                float a1 = 0f, c = 0f, h1 = 0f;
                //Vector2 centrepoint = new Vector2(d, d);
                UnityEngine.Color pixelColour;

                int x2 = 0, y2 = 0;
                float a2 = 0f, h2 = 0f;

                //int count = 0;

                // Loop through all the pixels in the source image
                for (int x1 = 0; x1 < width; x1++)
                {
                    for (int y1 = 0; y1 < height; y1++)
                    {
                        h1 = Mathf.Sqrt((x1 * x1) + (y1 * y1));
                        a1 = Mathf.Atan2(y1, x1);

                        c = (h1 * Mathf.Cos(a1)) / d;

                        a2 = a1 + angleRad;
                        h2 = (d * c) / Mathf.Cos(a2);

                        x2 = Mathf.RoundToInt(Mathf.Cos(a2) * h2);
                        y2 = Mathf.RoundToInt(Mathf.Sin(a2) * h2);

                        if (x2 >= 0 && x2 < width && y2 >= 0 && y2 < width)
                        {
                            pixelColour = SourceImage.GetPixel(x2, y2);

                            outputImage.SetPixel(x1, y1, pixelColour);
                        }
                        else
                        {
                            // This cuts off the Landform - not really what we want...
                            outputImage.SetPixel(x1, y1, UnityEngine.Color.black);

                            //if (y2 < 0)
                            //{
                            //    //if (count++ < 20)
                            //    if (x1 == 3 && y1 == 0)
                            //    {

                            //        Debug.Log("x1,y1:" + x1.ToString() + "," + y1.ToString() + " x2,y2:" + x2.ToString() + "," + y2.ToString());

                            //        Debug.Log("h1:" + h1.ToString() + " a1:" + a1.ToString() + " c:" + c.ToString());
                            //        Debug.Log("h2:" + h2.ToString() + " a2:" + a2.ToString() + " d:" + d.ToString());
                            //        Debug.Log("x2,y2f: " + (Mathf.Cos(a2) * h2).ToString() + "," + (Mathf.Sin(a2) * h2).ToString());
                            //    }
                            //}
                        }
                    }
                }
            }

            return outputImage;
        }
    }
}