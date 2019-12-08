#define LB_MAP_COMPUTE

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace LandscapeBuilder
{
    #region LBMapPoint

    public class LBMapPoint
    {
        public int x;
        public int y;

        // constructor
        public LBMapPoint(int px, int py)
        {
            x = px;
            y = py;
        }

        public override string ToString()
        {
            return "(" + x.ToString() + "," + y.ToString() + ")";
        }
    }
    #endregion

    public class LBMap
    {
        // Map Class
        #region Enumerations
        public enum ColourChannel
        {
            Red = 0,
            Green = 1,
            Blue = 2,
            Alpha = 3
        }

        #endregion

        #region Variables and Properties

        /// <summary>
        /// The resolution of the map Texture2D
        /// NOTE: can't use an enum because it starts with numbers and contains dots
        /// </summary>
        public static string[] MapResolutionArray = new string[] { "1024", "2048", "4096", "8192" };
        public static int[] MapResolutionArrayLookup = new int[] { 1024, 2048, 4096, 8192 };

        public static int MapResolution(int Index)
        {
            // Defaults to 4096 resolution
            int resolution = 2;

            if (MapResolutionArray != null && MapResolutionArrayLookup != null)
            {
                if (MapResolutionArray.Length > Index && MapResolutionArrayLookup.Length == MapResolutionArray.Length)
                {
                    resolution = MapResolutionArrayLookup[Index];
                }
            }
            return resolution;
        }

        public static string GetDefaultMapFolder { get { return "Assets/LandscapeBuilder/Maps"; } }

        public Texture2D map;
        public int tolerance;
        public bool inverse;

        // Added v1.3.1 Beta 8f
        public bool useAdvancedTolerance;
        public int toleranceRed;
        public int toleranceGreen;
        public int toleranceBlue;
        public int toleranceAlpha;
        public float mapWeightRed;
        public float mapWeightGreen;
        public float mapWeightBlue;
        public float mapWeightAlpha;
        public AnimationCurve toleranceBlendCurve;
        public LBCurve.BlendCurvePreset toleranceBlendCurvePreset;

        public static AnimationCurve GetDefaultToleranceBlendCurve { get { return LBCurve.SetCurveFromPreset(LBCurve.CurvePreset.Cubed); } }

        // The mapColour is the what the user sets in the Editor
        // This is the colour the use wants for an exact map in the texture
        // Deviations from this colour can be detected using tolerance
        public UnityEngine.Color mapColour { get; private set; }
        public int mapColourR { get; private set; }
        public int mapColourG { get; private set; }
        public int mapColourB { get; private set; }
        public int mapColourA { get; private set; }

        // The colour of the pixel in the texture map (PNG image)
        public UnityEngine.Color pixelColour { get; private set; }
        public int pixelColourR { get; private set; }
        public int pixelColourG { get; private set; }
        public int pixelColourB { get; private set; }
        public int pixelColourA { get; private set; }

        // Colour components (RGBA) range to match
        public int cMatchStartR { get; private set; }
        public int cMatchEndR { get; private set; }
        public int cMatchStartG { get; private set; }
        public int cMatchEndG { get; private set; }
        public int cMatchStartB { get; private set; }
        public int cMatchEndB { get; private set; }
        public int cMatchStartA { get; private set; }
        public int cMatchEndA { get; private set; }

        // temporary internal variables - pre-declared to avoid creating/deleting

        #endregion

        #region Compute Shader Variables
        #if LB_MAP_COMPUTE
        // Compute shaders
        private static readonly string LBCSPath = "LBCSPath";
        // Compute kernal methods
        private static readonly string CSKPathCreateMap = "PathCreateMap";

        // Compute shader variables names
        private static readonly string CSmapTex = "mapTex";
        private static readonly string CSmapTexWidth = "mapTexWidth";
        private static readonly string CSmapTexHeight = "mapTexHeight";
        private static readonly string CSnumSplineCentrePoints = "numSplineCentrePoints";
        private static readonly string CSsplinePointsCentre = "splinePointsCentre";
        private static readonly string CSsplinePointsLeft = "splinePointsLeft";
        private static readonly string CSsplinePointsRight = "splinePointsRight";
        private static readonly string CSmaxWidth = "maxWidth";
        private static readonly string CScheckEdges = "checkEdges";
        private static readonly string CSblendEnds = "blendEnds";
        private static readonly string CSblendStart = "blendStart";
        private static readonly string CSblendEnd = "blendEnd";
        private static readonly string CSquadLookAhead = "quadLookAhead";
        private static readonly string CSclosedCircuit = "closedCircuit";
        private static readonly string CSremoveCentre = "removeCentre";
        private static readonly string CSedgeBlendWidth = "edgeBlendWidth";
        private static readonly string CSsqrblendEdgeWidth = "sqrblendEdgeWidth";
        private static readonly string CSsqrborderLeftWidth = "sqrborderLeftWidth";
        private static readonly string CSsqrborderRightWidth = "sqrborderRightWidth";
        private static readonly string CSpathBounds = "pathBounds";
        // Compute shader landscape variable names
        private static readonly string CSlandscapePos = "landscapePos";
        private static readonly string CSlandscapeSize = "landscapeSize";
        // Compute shader variables not used with PathCreateMap but set to avoid issue
        private static readonly string CSheightmapResolution = "hmapRes";
        private static readonly string CSheightScale = "heightScale";
        private static readonly string CSminHeight = "minHeight";
        private static readonly string CSpathBlendCurveNumKeys = "blendCurveNumKeys";
        private static readonly string CSpathHeightCurveNumKeys = "heightCurveNumKeys";
        private static readonly string CSpathTypeMode = "typeMode";
        private static readonly string CSpathInvertMultipler = "invertMultipler";
        private static readonly string CSsurroundSmoothing = "surroundSmoothing";
        private static readonly string CSblendTerrainHeight = "blendTerrainHeight";
        // Compute shader terrain variable names (not used in PathCreateMap)
        private static readonly string CSterrainWidth = "terrainWidth";
        private static readonly string CSterrainLength = "terrainLength";
        private static readonly string CSterrainHeight = "terrainHeight";
        private static readonly string CSterrainWorldPos = "terrainWorldPos";

        #endif
        #endregion

        #region Constructors
        // Constructor
        public LBMap(Texture2D mapTexture, UnityEngine.Color colour, int colourTolerance)
        {
            this.map = mapTexture;
            this.mapColour = colour;
            this.tolerance = colourTolerance;

            // convert Texture  map colour into integer
            mapColourR = Mathf.RoundToInt(mapColour.r * 255f);
            mapColourG = Mathf.RoundToInt(mapColour.g * 255f);
            mapColourB = Mathf.RoundToInt(mapColour.b * 255f);
            mapColourA = Mathf.RoundToInt(mapColour.a * 255f);

            UpdateColourMatchRange();

            // defaults
            this.inverse = false;
            this.useAdvancedTolerance = false;
            this.toleranceRed = 0;
            this.toleranceGreen = 0;
            this.toleranceBlue = 0;
            this.toleranceAlpha = 0;
            this.mapWeightRed = 1f;
            this.mapWeightGreen = 1f;
            this.mapWeightBlue = 1f;
            this.mapWeightAlpha = 1f;
            this.toleranceBlendCurvePreset = (LBCurve.BlendCurvePreset)LBCurve.CurvePreset.Cubed;
            this.toleranceBlendCurve = LBCurve.SetCurveFromPreset((LBCurve.CurvePreset)this.toleranceBlendCurvePreset);
        }

        #endregion

        #region Colour Static Methods

        /// <summary>
        /// Gets the integer value of RGB float colour component
        /// Usage: int colourR = GetRGBAComponentAsInteger(mapColour.r, false);
        /// </summary>
        /// <param name="colourValue"></param>
        /// <param name="isClamped"></param>
        /// <returns></returns>
        public static int GetRGBAComponentAsInteger(float colourValue, bool isClamped)
        {
            if (isClamped)
            {
                return Mathf.Clamp(Mathf.RoundToInt(colourValue * 255f), 0, 255);
            }
            else
            {
                return Mathf.RoundToInt(colourValue * 255f);
            }
        }

        #endregion

        #region Colour Member Methods

        /// <summary>
        /// This should be called if the tolerances have changed and/or useAdvancedTolerance has changed
        /// </summary>
        public void UpdateColourMatchRange()
        {
            if (useAdvancedTolerance)
            {
                // Get the Red, Green and Blue values to match with a individual RGBA channel tolerances
                cMatchStartR = Mathf.Clamp(Mathf.RoundToInt(mapColour.r * 255f) - toleranceRed, 0, 255);
                cMatchEndR = Mathf.Clamp(Mathf.RoundToInt(mapColour.r * 255f) + toleranceRed, 0, 255);

                cMatchStartG = Mathf.Clamp(Mathf.RoundToInt(mapColour.g * 255f) - toleranceGreen, 0, 255);
                cMatchEndG = Mathf.Clamp(Mathf.RoundToInt(mapColour.g * 255f) + toleranceGreen, 0, 255);

                cMatchStartB = Mathf.Clamp(Mathf.RoundToInt(mapColour.b * 255f) - toleranceBlue, 0, 255);
                cMatchEndB = Mathf.Clamp(Mathf.RoundToInt(mapColour.b * 255f) + toleranceBlue, 0, 255);

                cMatchStartA = Mathf.Clamp(Mathf.RoundToInt(mapColour.a * 255f) - toleranceAlpha, 0, 255);
                cMatchEndA = Mathf.Clamp(Mathf.RoundToInt(mapColour.a * 255f) + toleranceAlpha, 0, 255);
            }
            else
            {
                // Get the Red, Green and Blue values to match with a single tolerance across all channels
                cMatchStartR = Mathf.Clamp(Mathf.RoundToInt(mapColour.r * 255f) - tolerance, 0, 255);
                cMatchEndR = Mathf.Clamp(Mathf.RoundToInt(mapColour.r * 255f) + tolerance, 0, 255);

                cMatchStartG = Mathf.Clamp(Mathf.RoundToInt(mapColour.g * 255f) - tolerance, 0, 255);
                cMatchEndG = Mathf.Clamp(Mathf.RoundToInt(mapColour.g * 255f) + tolerance, 0, 255);

                cMatchStartB = Mathf.Clamp(Mathf.RoundToInt(mapColour.b * 255f) - tolerance, 0, 255);
                cMatchEndB = Mathf.Clamp(Mathf.RoundToInt(mapColour.b * 255f) + tolerance, 0, 255);

                cMatchStartA = Mathf.Clamp(Mathf.RoundToInt(mapColour.a * 255f) - tolerance, 0, 255);
                cMatchEndA = Mathf.Clamp(Mathf.RoundToInt(mapColour.a * 255f) + tolerance, 0, 255);
            }
        }

        /// <summary>
        /// Returns the colour of the pixel at the given x,y coordinates in the map Texture
        /// </summary>
        /// <param name="xMapPos"></param>
        /// <param name="yMapPos"></param>
        /// <returns></returns>
        public Color GetPixelColourFromMapTexture(int xMapPos, int yMapPos)
        {
            return map.GetPixel(xMapPos, yMapPos);
        }

        /// <summary>
        /// Check if the pixel in the map is the same as the mapColour within the Tolerance factor.
        /// Check if it outside when inverse is enabled.
        /// </summary>
        /// <param name="xMapPos"></param>
        /// <param name="yMapPos"></param>
        /// <param name="compareAlphaChannel"></param>
        /// <param name="useTolerance"></param>
        /// <returns></returns>
        public bool IsMapPixelMatchToMapColour(int xMapPos, int yMapPos, bool compareAlphaChannel, bool useTolerance)
        {
            bool isMatch = false;

            pixelColour = GetPixelColourFromMapTexture(xMapPos, yMapPos);

            // convert pixel image colour into integer
            pixelColourR = LBMap.GetRGBAComponentAsInteger(pixelColour.r, false);
            pixelColourG = LBMap.GetRGBAComponentAsInteger(pixelColour.g, false);
            pixelColourB = LBMap.GetRGBAComponentAsInteger(pixelColour.b, false);
            pixelColourA = LBMap.GetRGBAComponentAsInteger(pixelColour.a, false);

            if (useTolerance)
            {
                isMatch = (pixelColourR >= cMatchStartR && pixelColourR <= cMatchEndR &&
                            pixelColourG >= cMatchStartG && pixelColourG <= cMatchEndG &&
                            pixelColourB >= cMatchStartB && pixelColourB <= cMatchEndB &&
                            ((pixelColourA >= cMatchStartA && pixelColourA <= cMatchEndA) || !compareAlphaChannel)
                          );
            }
            else
            {
                // Colour of pixel in map must be an exact match to the mapColour 
                isMatch = (pixelColourR == mapColourR && pixelColourG == mapColourG && pixelColourB == mapColourB && (pixelColourA == mapColourA || !compareAlphaChannel));
            }

            // Return the opposite result if inverse is enabled
            if (inverse) { isMatch = !isMatch; }

            return isMatch;
        }

        /// <summary>
        /// Check if the pixel in the map is the same as the mapColour
        /// NOTE: This does not check the map first. To do that use LBMap.IsMapPixelMatchToMapColour(...)
        /// </summary>
        /// <param name="compareAlphaChannel"></param>
        /// <returns></returns>
        public bool IsPixelColourExactMatch(bool compareAlphaChannel)
        {
            bool isMatch = false;

            // Colour of pixel in map must be an exact match to the mapColour 
            isMatch = (pixelColourR == mapColourR && pixelColourG == mapColourG && pixelColourB == mapColourB && (pixelColourA == mapColourA || !compareAlphaChannel));

            // Return the opposite result if inverse is enabled
            if (inverse) { isMatch = !isMatch; }

            return isMatch;
        }

        /// <summary>
        /// Returns, as a float, the average variation between the RGB and optionally Alpha components
        /// between the map texture pixel colour, and the MapColour set by the user in the editor
        /// NOTE: pixelColour is typically first set by calling IsMapPixelMatchToMapColour()
        /// </summary>
        /// <param name="includeAlphaChannel"></param>
        /// <returns></returns>
        public float GetPixelColourAvgVariation(bool includeAlphaChannel = false)
        {
            if (includeAlphaChannel)
            {
                return (Mathf.Abs(mapColourR - pixelColourR) + Mathf.Abs(mapColourG - pixelColourG) + Mathf.Abs(mapColourB - pixelColourB) + Mathf.Abs(mapColourA - pixelColourA)) / 4f;
            }
            else
            {
                return (Mathf.Abs(mapColourR - pixelColourR) + Mathf.Abs(mapColourG - pixelColourG) + Mathf.Abs(mapColourB - pixelColourB)) / 3f;
            }
        }

        /// <summary>
        /// Returns the variation between the current pixel and mapColourR/G/B/A set in class constructor
        /// NOTE: pixelColour is typically first set by calling IsMapPixelMatchToMapColour()
        /// </summary>
        /// <param name="colourChannel"></param>
        /// <returns></returns>
        public int GetPixelColourVariation(ColourChannel colourChannel)
        {
            int variation = 0;

            if (colourChannel == ColourChannel.Red) { variation = Mathf.Abs(mapColourR - pixelColourR); }
            else if (colourChannel == ColourChannel.Green) { variation = Mathf.Abs(mapColourG - pixelColourG); }
            else if (colourChannel == ColourChannel.Blue) { variation = Mathf.Abs(mapColourB - pixelColourB); }
            else if (colourChannel == ColourChannel.Alpha) { variation = Mathf.Abs(mapColourA - pixelColourA); }

            return variation;
        }

        /// <summary>
        ///  Returns the variation (0-1) between the current pixel and mapColour set in class constructor
        ///  NOTE: pixelColour is typically first set by calling GetPixelColourFromMapTexture() or IsMapPixelMatchToMapColour()
        /// </summary>
        /// <param name="colourChannel"></param>
        /// <returns></returns>
        public float GetPixelColourVariationNormalised(ColourChannel colourChannel)
        {
            float variation = 0;

            if (colourChannel == ColourChannel.Red) { variation = Mathf.Abs(mapColour.r - pixelColour.r); }
            else if (colourChannel == ColourChannel.Green) { variation = Mathf.Abs(mapColour.g - pixelColour.g); }
            else if (colourChannel == ColourChannel.Blue) { variation = Mathf.Abs(mapColour.b - pixelColour.b); }
            else if (colourChannel == ColourChannel.Alpha) { variation = Mathf.Abs(mapColour.a - pixelColour.a); }

            return variation;
        }

        /// <summary>
        /// Returns the variation (0-1) between the pixel at x,y and mapColour set in class constructor,
        /// using the background colour as a starting colour. This is typically used with maps created with
        /// LBMapPath and topography map filters
        /// </summary>
        /// <param name="xMapPos"></param>
        /// <param name="yMapPos"></param>
        /// <param name="backgroundColour"></param>
        /// <returns></returns>
        public float GetPixelColourVariationNormalised(int xMapPos, int yMapPos, Color backgroundColour)
        {
            float variation = 0f;

            // Get the colour of this pixel in the map texture
            pixelColour = map.GetPixel(xMapPos, yMapPos);

            // convert pixel image colour into integer
            pixelColourR = LBMap.GetRGBAComponentAsInteger(pixelColour.r, false);
            pixelColourG = LBMap.GetRGBAComponentAsInteger(pixelColour.g, false);
            pixelColourB = LBMap.GetRGBAComponentAsInteger(pixelColour.b, false);
            pixelColourA = LBMap.GetRGBAComponentAsInteger(pixelColour.a, false);

            if (!(pixelColourR == mapColourR && pixelColourG == mapColourG && pixelColourB == mapColourB))
            {
                // Compare it to the background colour
                variation = Mathf.InverseLerp(mapColour.grayscale, backgroundColour.grayscale, pixelColour.grayscale);
            }

            return variation;
        }

        /// <summary>
        ///  Returns the weighted average of the variation between the current pixel and mapColour set in class constructor
        ///  NOTE: pixelColour is typically first set by calling GetPixelColourFromMapTexture() or IsMapPixelMatchToMapColour()
        /// </summary>
        /// <returns></returns>
        public float GetPixelColourVariationNormalisedWeightedAvg()
        {
            float variationR = GetPixelColourVariationNormalised(ColourChannel.Red);
            float variationG = GetPixelColourVariationNormalised(ColourChannel.Green);
            float variationB = GetPixelColourVariationNormalised(ColourChannel.Blue);
            float variationA = GetPixelColourVariationNormalised(ColourChannel.Alpha);

            float variationSum = mapWeightRed + mapWeightGreen + mapWeightBlue + mapWeightAlpha;
            // Multiply by a small number to avoid div 0 error
            if (variationSum == 0f) { variationSum = 0.0000f; }

            return (((variationR * mapWeightRed) + (variationG * mapWeightGreen) + (variationB * mapWeightBlue) + (variationA * mapWeightAlpha)) / variationSum);
        }

        public float GetMapColourWeightedAvg()
        {
            if (useAdvancedTolerance)
            {
                float variationSum = mapWeightRed + mapWeightGreen + mapWeightBlue + mapWeightAlpha;
                // Multiply by a small number to avoid div 0 error
                if (variationSum == 0f) { variationSum = 0.0000f; }

                return (((mapColour.r * mapWeightRed) + (mapColour.g * mapWeightGreen) + (mapColour.b * mapWeightBlue) + (mapColour.a * mapWeightAlpha)) / variationSum);
            }
            else
            {
                // All colours have equal weight when not useAdvancedTolerance
                return ((mapColour.r + mapColour.g + mapColour.b + mapColour.a) / 4f);
            }
        }

        public float GetToleranceWeightedAvg()
        {
            if (useAdvancedTolerance)
            {
                float variationSum = mapWeightRed + mapWeightGreen + mapWeightBlue + mapWeightAlpha;
                // Multiply by a small number to avoid div 0 error
                if (variationSum == 0f) { variationSum = 0.0000f; }

                return (((toleranceRed * mapWeightRed) + (toleranceGreen * mapWeightGreen) + (toleranceBlue * mapWeightBlue) + (toleranceAlpha * mapWeightAlpha)) / variationSum);
            }
            else
            {
                // Just return tolerance when not useAdvancedTolerance
                return (float)tolerance;
            }
        }

        #endregion

        #region Position Methods

        /// <summary>
        /// Convert the normalised terrain coordinates into a point on the Map texture
        /// </summary>
        /// <param name="xPos"></param>
        /// <param name="yPos"></param>
        /// <param name="startX"></param>
        /// <param name="endX"></param>
        /// <param name="startY"></param>
        /// <param name="endY"></param>
        /// <returns></returns>
        public LBMapPoint GetMapPositionFromTerrainPosition(float xPos, float yPos, float startX, float endX, float startY, float endY)
        {
            LBMapPoint mapPos = new LBMapPoint(0, 0);

            if (map != null)
            {
                // Get the Map Position, taking into consideration the normalised start and end positions
                // of a terrain's positions within the landscape
                // Note: xPos and yPos are reversed...
                // LB 1.4.0 Beta 11k * by map.width - 1 rather than map.width as it is zero-based
                mapPos.x = Mathf.RoundToInt((((endX - startX) * yPos) + startX) * (map.width - 1));
                mapPos.y = Mathf.RoundToInt((((endY - startY) * xPos) + startY) * (map.height - 1));

                mapPos.x = Mathf.Clamp(mapPos.x, 0, map.width - 1);
                mapPos.y = Mathf.Clamp(mapPos.y, 0, map.height - 1);
            }

            return mapPos;
        }

        /// <summary>
        /// Convert a non-normalised landscape position into a point on the Map texture
        /// </summary>
        /// <param name="LandscapeSize"></param>
        /// <param name="landscapePoint"></param>
        /// <returns></returns>
        public LBMapPoint GetMapPositionFromLandscapePosition(Vector2 LandscapeSize, Vector3 landscapePoint)
        {
            LBMapPoint mapPos = new LBMapPoint(0, 0);
            float xPos = 0f, zPos = 0f;

            if (map != null)
            {
                // Get the normalised position of the point in the landscape
                xPos = landscapePoint.x / LandscapeSize.x;
                zPos = landscapePoint.z / LandscapeSize.y;

                // Get the landscape point on the Map
                mapPos.x = Mathf.RoundToInt(xPos * (float)map.width);
                mapPos.y = Mathf.RoundToInt(zPos * (float)map.height);

                mapPos.x = Mathf.Clamp(mapPos.x, 0, map.width - 1);
                mapPos.y = Mathf.Clamp(mapPos.y, 0, map.height - 1);
            }

            return mapPos;
        }

        /// <summary>
        /// Get the Map pixel position in worldspace within the landscape
        /// </summary>
        /// <param name="landscapeWorldPosition"></param>
        /// <param name="landscapeSize"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public Vector3 GetWorldPositionFromMapPosition(Vector3 landscapeWorldPosition, Vector2 landscapeSize, int x, int y)
        {
            Vector3 worldPos = landscapeWorldPosition;

            if (map != null)
            {
                // Get normalised map pixel position
                Vector2 mapPosN = new Vector2((float)x / ((float)map.width - 1f), (float)y / ((float)map.height - 1f));

                worldPos.x += (landscapeSize.x * mapPosN.x);
                worldPos.z += (landscapeSize.y * mapPosN.y);
            }

            return worldPos;
        }

        #endregion

        #region Create from spline or path methods

        /// <summary>
        /// Export a Texture2D map that follows a spline of points in the landscape
        /// The map can have a different path thickness on the right or left of the spline
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="splinePoints"></param>
        /// <param name="pathLeftWidth"></param>
        /// <param name="pathRightWidth"></param>
        /// <param name="splineColour"></param>
        /// <param name="isSplineDisplayed"></param>
        /// <returns></returns>
        public bool CreateMapFromSpline(LBLandscape landscape, Vector3[] splinePoints, float pathLeftWidth, float pathRightWidth, UnityEngine.Color splineColour, bool isSplineDisplayed)
        {
            bool isSuccessful = false;

            if (landscape != null && map != null)
            {
                if (splinePoints == null)
                {
                    Debug.LogWarning("CreateMapFromSpline - no splines defined");
                }
                else if (splinePoints.Length < 2)
                {
                    Debug.LogWarning("CreateMapFromSpline - must have at least 2 spline points to map");
                }
                else
                {
                    Vector3 landscapeWorldPosition = landscape.transform.position;
                    Vector3 currentSplinePointPos = Vector3.zero;
                    LBMapPoint mapSplinePoint = new LBMapPoint(0, 0);
                    LBMapPoint mapLeftEdgePoint = new LBMapPoint(0, 0);
                    LBMapPoint mapRightEdgePoint = new LBMapPoint(0, 0);
                    Vector3 currentSplinePoint = Vector3.zero;
                    Vector3 prevSplinePoint = Vector3.zero;
                    Vector3 leftEdgePoint = Vector3.zero;
                    Vector3 rightEdgePoint = Vector3.zero;
                    Vector3 directionToNext = Vector3.zero;
                    Vector3 directionToLeftEdge = Vector3.zero;
                    Vector3 directionToRightEdge = Vector3.zero;

                    float distToPrevPoint = 0f, distCovered = 0f;
                    float mapPixelSize = 0f;
                    Vector3 insertPoint = Vector3.zero;

                    int pathLeftWidthInt = Mathf.RoundToInt(pathLeftWidth);
                    int pathRightWidthInt = Mathf.RoundToInt(pathRightWidth);

                    if (splinePoints != null)
                    {
                        List<Vector3> splinePointsFilled = new List<Vector3>();

                        // Get the map pixel size. Then reduce it to make sure we have good coverage on bends.
                        mapPixelSize = (landscape.size.x / map.width) * 0.30f;

                        // Build another list to fill in any gaps that could appear in the map texture
                        for (int i = 1; i < splinePoints.Length; i++)
                        {
                            currentSplinePoint = splinePoints[i];
                            prevSplinePoint = splinePoints[i - 1];

                            distToPrevPoint = Vector3.Distance(currentSplinePoint, prevSplinePoint);

                            // Lerp between the spline points based on the pixel size of the map texture in worldspace
                            distCovered = 0f;
                            while (distCovered < distToPrevPoint)
                            {
                                insertPoint = Vector3.Lerp(prevSplinePoint, currentSplinePoint, distCovered / distToPrevPoint);
                                splinePointsFilled.Add(insertPoint);
                                distCovered += mapPixelSize;
                            }
                        }

                        UnityEngine.Color pixelColour = splineColour;
                        pixelColour.a = 1f;

                        UnityEngine.Color edgeColour = mapColour;
                        edgeColour.a = 1f;

                        for (int i = 0; i < splinePointsFilled.Count; i++)
                        {
                            currentSplinePoint = splinePointsFilled[i];

                            // Get the position of the spline point in the landscape
                            currentSplinePointPos = currentSplinePoint - landscapeWorldPosition;

                            // Get the current spline point on the Map
                            mapSplinePoint = GetMapPositionFromLandscapePosition(landscape.size, currentSplinePointPos);

                            if (isSplineDisplayed)
                            {
                                // Set the pixel colour to the spline colour
                                map.SetPixel(mapSplinePoint.x, mapSplinePoint.y, pixelColour);
                            }
                            else
                            {
                                // Don't highlight the spline, so put it in the same colour as the edges
                                map.SetPixel(mapSplinePoint.x, mapSplinePoint.y, edgeColour);
                            }

                            if (splinePointsFilled.Count > 1)
                            {
                                if (i == 0)
                                {
                                    directionToNext = (splinePointsFilled[i + 1] - currentSplinePoint).normalized;
                                }
                                else if (i < splinePointsFilled.Count)
                                {
                                    directionToNext = (currentSplinePoint - prevSplinePoint).normalized;
                                }

                                // The direction to the left and right are perpendicular to the spline direction
                                directionToLeftEdge = Quaternion.Euler(0f, -90f, 0f) * directionToNext;
                                directionToRightEdge = Quaternion.Euler(0f, 90f, 0f) * directionToNext;

                                // Add the left edge of the path at this point in the spline
                                for (int w = 1; w < pathLeftWidthInt + 1; w++)
                                {
                                    // Get the left edge point on the Map
                                    leftEdgePoint = currentSplinePointPos + (directionToLeftEdge * (float)w);
                                    mapLeftEdgePoint = GetMapPositionFromLandscapePosition(landscape.size, leftEdgePoint);

                                    map.SetPixel(mapLeftEdgePoint.x, mapLeftEdgePoint.y, edgeColour);
                                }

                                // Add the right edge of the path at this point in the spline
                                for (int w = 1; w < pathRightWidthInt + 1; w++)
                                {
                                    // Get the right edge point on the Map
                                    rightEdgePoint = currentSplinePointPos + (directionToRightEdge * (float)w);
                                    mapRightEdgePoint = GetMapPositionFromLandscapePosition(landscape.size, rightEdgePoint);

                                    map.SetPixel(mapRightEdgePoint.x, mapRightEdgePoint.y, edgeColour);
                                }
                            }

                            // Remember this position
                            prevSplinePoint = currentSplinePoint;
                        }
                    }
                    isSuccessful = true;
                }
            }

            return isSuccessful;
        }

        /// <summary>
        /// Export a Texture2D map that follows a right and left spline of points in the landscape
        /// The map can have a different path thickness on the right or left of the spline.
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="splinePointsLeft"></param>
        /// <param name="splinePointsRight"></param>
        /// <param name="pathLeftMinWidth"></param>
        /// <param name="pathLeftMaxWidth"></param>
        /// <param name="pathRightMinWidth"></param>
        /// <param name="pathRightMaxWidth"></param>
        /// <returns></returns>
        public bool CreateMapFromSplines(LBLandscape landscape, Vector3[] splinePointsLeft, Vector3[] splinePointsRight,
                                         float pathLeftMinWidth, float pathLeftMaxWidth, float pathRightMinWidth, float pathRightMaxWidth)
        {
            bool isSuccessful = false;

            if (landscape != null && map != null)
            {
                if (splinePointsLeft == null) { Debug.LogWarning("CreateMapFromSplines - no left splines defined"); }
                else if (splinePointsRight == null) { Debug.LogWarning("CreateMapFromSplines - no right splines defined"); }
                else if (splinePointsLeft.Length < 2) { Debug.LogWarning("CreateMapFromSplines - must have at least 2 left spline points to map"); }
                else if (splinePointsRight.Length < 2) { Debug.LogWarning("CreateMapFromSplines - must have at least 2 right spline points to map"); }
                else if (splinePointsLeft.Length != splinePointsRight.Length) { Debug.LogWarning("CreateMapFromSplines - in this release the number of left and right spline points must be the same"); }
                else
                {
                    Vector3 landscapeWorldPosition = landscape.transform.position;
                    Vector3 currentSplinePointPos = Vector3.zero;
                    LBMapPoint mapLeftEdgePoint = new LBMapPoint(0, 0);
                    LBMapPoint mapRightEdgePoint = new LBMapPoint(0, 0);
                    Vector3 currentSplinePoint = Vector3.zero;
                    Vector3 prevSplinePoint = Vector3.zero;
                    Vector3 leftEdgePoint = Vector3.zero;
                    Vector3 rightEdgePoint = Vector3.zero;
                    Vector3 directionToNext = Vector3.zero;
                    Vector3 directionToLeftEdge = Vector3.zero;
                    Vector3 directionToRightEdge = Vector3.zero;

                    float distToPrevPoint = 0f, distCovered = 0f;
                    float mapPixelSize = 0f;
                    Vector3 insertPoint = Vector3.zero;

                    int pathLeftMinWidthInt = Mathf.RoundToInt(pathLeftMinWidth);
                    int pathLeftMaxWidthInt = Mathf.RoundToInt(pathLeftMaxWidth);
                    int pathRightMinWidthInt = Mathf.RoundToInt(pathRightMinWidth);
                    int pathRightMaxWidthInt = Mathf.RoundToInt(pathRightMaxWidth);

                    UnityEngine.Color edgeColour = mapColour;
                    edgeColour.a = 1f;

                    List<Vector3> splinePointsLeftFilled = new List<Vector3>();
                    List<Vector3> splinePointsRightFilled = new List<Vector3>();

                    // Get the map pixel size. Then reduce it to make sure we have good coverage on bends.
                    mapPixelSize = (landscape.size.x / map.width) * 0.30f;

                    // Build new left spline list to fill in any gaps that could appear in the map texture
                    for (int i = 1; i < splinePointsLeft.Length; i++)
                    {
                        currentSplinePoint = splinePointsLeft[i];
                        prevSplinePoint = splinePointsLeft[i - 1];

                        distToPrevPoint = Vector3.Distance(currentSplinePoint, prevSplinePoint);

                        // Lerp between the spline points based on the pixel size of the map texture in worldspace
                        distCovered = 0f;
                        while (distCovered < distToPrevPoint)
                        {
                            insertPoint = Vector3.Lerp(prevSplinePoint, currentSplinePoint, distCovered / distToPrevPoint);
                            splinePointsLeftFilled.Add(insertPoint);
                            distCovered += mapPixelSize;
                        }
                    }

                    // Build new right spline list to fill in any gaps that could appear in the map texture
                    for (int i = 1; i < splinePointsRight.Length; i++)
                    {
                        currentSplinePoint = splinePointsRight[i];
                        prevSplinePoint = splinePointsRight[i - 1];

                        distToPrevPoint = Vector3.Distance(currentSplinePoint, prevSplinePoint);

                        // Lerp between the spline points based on the pixel size of the map texture in worldspace
                        distCovered = 0f;
                        while (distCovered < distToPrevPoint)
                        {
                            insertPoint = Vector3.Lerp(prevSplinePoint, currentSplinePoint, distCovered / distToPrevPoint);
                            splinePointsRightFilled.Add(insertPoint);
                            distCovered += mapPixelSize;
                        }
                    }

                    // Populate the map with the left edge
                    for (int i = 0; i < splinePointsLeftFilled.Count; i++)
                    {
                        currentSplinePoint = splinePointsLeftFilled[i];

                        // Get the position of the spline point in the landscape
                        currentSplinePointPos = currentSplinePoint - landscapeWorldPosition;

                        if (splinePointsLeftFilled.Count > 1)
                        {
                            if (i == 0)
                            {
                                directionToNext = (splinePointsLeftFilled[i + 1] - currentSplinePoint).normalized;
                            }
                            else if (i < splinePointsLeftFilled.Count)
                            {
                                directionToNext = (currentSplinePoint - prevSplinePoint).normalized;
                            }

                            // The direction to the left is perpendicular to the spline direction
                            directionToLeftEdge = Quaternion.Euler(0f, -90f, 0f) * directionToNext;

                            // Add the left edge of the path at this point in the spline
                            for (int w = pathLeftMinWidthInt; w < pathLeftMaxWidthInt + 1; w++)
                            {
                                // Get the left edge point on the Map
                                leftEdgePoint = currentSplinePointPos + (directionToLeftEdge * (float)w);
                                mapLeftEdgePoint = GetMapPositionFromLandscapePosition(landscape.size, leftEdgePoint);

                                map.SetPixel(mapLeftEdgePoint.x, mapLeftEdgePoint.y, edgeColour);
                            }
                        }

                        // Remember this position
                        prevSplinePoint = currentSplinePoint;
                    }

                    // Populate the map with the right edge
                    for (int i = 0; i < splinePointsRightFilled.Count; i++)
                    {
                        currentSplinePoint = splinePointsRightFilled[i];

                        // Get the position of the spline point in the landscape
                        currentSplinePointPos = currentSplinePoint - landscapeWorldPosition;

                        if (splinePointsRightFilled.Count > 1)
                        {
                            if (i == 0)
                            {
                                directionToNext = (splinePointsRightFilled[i + 1] - currentSplinePoint).normalized;
                            }
                            else if (i < splinePointsRightFilled.Count)
                            {
                                directionToNext = (currentSplinePoint - prevSplinePoint).normalized;
                            }

                            // The direction to the right is perpendicular to the spline direction
                            directionToRightEdge = Quaternion.Euler(0f, 90f, 0f) * directionToNext;

                            // Add the right edge of the path at this point in the spline
                            for (int w = pathRightMinWidthInt; w < pathRightMaxWidthInt + 1; w++)
                            {
                                // Get the right edge point on the Map
                                rightEdgePoint = currentSplinePointPos + (directionToRightEdge * (float)w);
                                mapRightEdgePoint = GetMapPositionFromLandscapePosition(landscape.size, rightEdgePoint);

                                map.SetPixel(mapRightEdgePoint.x, mapRightEdgePoint.y, edgeColour);
                            }
                        }

                        // Remember this position
                        prevSplinePoint = currentSplinePoint;
                    }

                    isSuccessful = true;
                }
            }

            return isSuccessful;
        }

        /// <summary>
        /// Export a Texture2D map that follows a path with left and right splines in the landscape
        /// The splines are typically created by a LBMapPath.
        /// NOTE: Currently the look ahead value is pre-calulated (see quadLookAheadInt below)
        /// CurveDetectionInner and Outer are used to determine how many quads to look forward and
        /// backwards from the nearest point on the curve from the current Map pixel.
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="lbPath"></param>
        /// <param name="curveDetectionInner"></param>
        /// <param name="curveDetectionOuter"></param>
        /// <returns></returns>
        public bool CreateMapFromPath(LBLandscape landscape, LBPath lbPath, int curveDetectionInner, int curveDetectionOuter)
        {
            bool isSuccess = false;

            if (landscape != null && map != null && lbPath != null)
            {
                lbPath.CachePathPointDistances();
                lbPath.CacheSplinePointDistances();
                Vector3[] splinePointsCentre = lbPath.cachedCentreSplinePoints;
                Vector3[] splinePointsLeft = lbPath.GetSplinePathEdgePoints(LBPath.PositionType.Left, true);
                Vector3[] splinePointsRight = lbPath.GetSplinePathEdgePoints(LBPath.PositionType.Right, true);

                bool blendEdges = (lbPath.edgeBlendWidth > 0f);
                float maxWidth = lbPath.GetMaxWidth();
                bool blendEnds = (lbPath.blendStart || lbPath.blendEnd);

                // Override blend ends settings if it is a complete closed circuit.
                if (lbPath.closedCircuit) { blendEnds = false; }

                // Perform some validation
                if (splinePointsCentre == null) { Debug.LogWarning("CreateMapFromPath - no centre splines defined"); }
                else if (splinePointsLeft == null) { Debug.LogWarning("CreateMapFromPath - no left splines defined"); }
                else if (splinePointsRight == null) { Debug.LogWarning("CreateMapFromPath - no right splines defined"); }
                else if (splinePointsCentre.Length < 2) { Debug.LogWarning("CreateMapFromPath - must have at least 2 centre spline points to map"); }
                else if (splinePointsLeft.Length < 2) { Debug.LogWarning("CreateMapFromPath - must have at least 2 left spline points to map"); }
                else if (splinePointsRight.Length < 2) { Debug.LogWarning("CreateMapFromPath - must have at least 2 right spline points to map"); }
                else if (splinePointsLeft.Length != splinePointsRight.Length) { Debug.LogWarning("CreateMapFromPath - in this release the number of left and right spline points must be the same"); }
                else if (splinePointsCentre.Length != splinePointsRight.Length) { Debug.LogWarning("CreateMapFromPath - in this release the number of centre, left and right spline points must be the same"); }
                else
                {
                    int mapWidth = map.width;
                    int mapHeigth = map.height;
                    int closestPoint = 0, secondclosestPoint = 0;
                    int numSplinePoints = splinePointsCentre.Length;
                    Vector3 landscapeWorldPosition = landscape.transform.position;
                    Vector2 landscapeSize = landscape.size;
                    Color blendColour = Color.white;
                    Color edgeColour = Color.clear;
                    Color endBlendColour = Color.clear;
                    float blendValue = 0f;

                    float sqrDistLeft = 0f, sqrDistRight = 0f, sqrPixelDistFromEdge = 0f;
                    float sqrDistToEnd = 0f, sqrDistToStart = 0f;

                    float sqrblendEdgeWidth = lbPath.edgeBlendWidth * lbPath.edgeBlendWidth;

                    // Used when removeCentre = true
                    float sqrborderLeftWidth = lbPath.leftBorderWidth * lbPath.leftBorderWidth;
                    float sqrborderRightWidth = lbPath.rightBorderWidth * lbPath.rightBorderWidth;
                    bool isLeftSide = false;

                    Vector3 quadP1 = Vector3.zero, quadP2 = Vector3.zero, quadP3 = Vector3.zero, quadP4 = Vector3.zero;

                    // Cache the last left/right points so we don't need to get them from the list for each map pixel
                    Vector3 lastLeftPoint = splinePointsLeft[numSplinePoints - 1];
                    Vector3 lastRightPoint = splinePointsRight[numSplinePoints - 1];
                    Vector3 firstLeftPoint = splinePointsLeft[0];
                    Vector3 firstRightPoint = splinePointsRight[0];

                    bool isMatch = false;

                    int prevIndex = 0, nextIndex = 0;

                    #region Determine Quad LookAhead
                    // If changing, also change in CreateMapFromPathCompute()

                    // Based on the width and the path resolution (segment length), determine the maximum number of quad to look forward
                    // or backwards from the current point.
                    float quadLookAhead = Mathf.Sqrt((((maxWidth * maxWidth) / (4f * lbPath.pathResolution * lbPath.pathResolution)) + 0.25f));
                    int quadLookAheadInt = Mathf.CeilToInt(quadLookAhead);

                    // Disable for now, may not be required
                    quadLookAheadInt = 1;
                    #endregion

                    Vector3 mapWorldPos = Vector3.zero;
                    // Loop through all the map pixels
                    for (int x = 0; x < mapWidth; x++)
                    {
                        for (int y = 0; y < mapHeigth; y++)
                        {
                            // Get the worldspace position of this map pixel
                            mapWorldPos = GetWorldPositionFromMapPosition(landscapeWorldPosition, landscapeSize, x, y);
                            mapWorldPos.y = 0f;

                            // Find the closest central spline point
                            closestPoint = FindClosestPoint(splinePointsCentre, mapWorldPos);

                            // Find the closest of its consecutive points
                            secondclosestPoint = FindClosestConsecutivePoint(splinePointsCentre, mapWorldPos, closestPoint);

                            // New Quad method
                            int firstPt = (closestPoint < secondclosestPoint ? closestPoint : secondclosestPoint);
                            int secondPt = (closestPoint < secondclosestPoint ? secondclosestPoint : closestPoint);

                            quadP1.x = splinePointsLeft[firstPt].x;
                            quadP1.z = splinePointsLeft[firstPt].z;
                            quadP2.x = splinePointsLeft[secondPt].x;
                            quadP2.z = splinePointsLeft[secondPt].z;
                            quadP3.x = splinePointsRight[firstPt].x;
                            quadP3.z = splinePointsRight[firstPt].z;
                            quadP4.x = splinePointsRight[secondPt].x;
                            quadP4.z = splinePointsRight[secondPt].z;

                            isMatch = IsInQuad(quadP1, quadP2, quadP3, quadP4, mapWorldPos);

                            // Check the 3 previous quads to get all edge fragments from the outside of corners and the inside of the last curve
                            for (prevIndex = 0; !isMatch && firstPt > prevIndex && prevIndex < quadLookAheadInt; prevIndex++)
                            {
                                quadP1.x = splinePointsLeft[firstPt - prevIndex - 1].x;
                                quadP1.z = splinePointsLeft[firstPt - prevIndex - 1].z;
                                quadP2.x = splinePointsLeft[firstPt - prevIndex].x;
                                quadP2.z = splinePointsLeft[firstPt - prevIndex].z;
                                quadP3.x = splinePointsRight[firstPt - prevIndex - 1].x;
                                quadP3.z = splinePointsRight[firstPt - prevIndex - 1].z;
                                quadP4.x = splinePointsRight[firstPt - prevIndex].x;
                                quadP4.z = splinePointsRight[firstPt - prevIndex].z;

                                isMatch = IsInQuad(quadP1, quadP2, quadP3, quadP4, mapWorldPos);
                            }

                            // Check the 3 next quads to get all edge fragments from the inside of corners and the outside of the last curve
                            for (nextIndex = 0; !isMatch && secondPt + nextIndex + 1 < numSplinePoints && nextIndex < quadLookAheadInt; nextIndex++)
                            {
                                quadP1.x = splinePointsLeft[secondPt + nextIndex].x;
                                quadP1.z = splinePointsLeft[secondPt + nextIndex].z;
                                quadP2.x = splinePointsLeft[secondPt + nextIndex + 1].x;
                                quadP2.z = splinePointsLeft[secondPt + nextIndex + 1].z;
                                quadP3.x = splinePointsRight[secondPt + nextIndex].x;
                                quadP3.z = splinePointsRight[secondPt + nextIndex].z;
                                quadP4.x = splinePointsRight[secondPt + nextIndex + 1].x;
                                quadP4.z = splinePointsRight[secondPt + nextIndex + 1].z;

                                isMatch = IsInQuad(quadP1, quadP2, quadP3, quadP4, mapWorldPos);
                            }

                            // v1.3.3 Beta 1a
                            // Fill in the gap halfway between the second last spline point and the
                            // last spline point (which should be the same as the first spline point).
                            if (!isMatch && lbPath.closedCircuit)
                            {
                                quadP1.x = splinePointsLeft[numSplinePoints - 2].x;
                                quadP1.z = splinePointsLeft[numSplinePoints - 2].z;
                                quadP2.x = splinePointsLeft[0].x;
                                quadP2.z = splinePointsLeft[0].z;
                                quadP3.x = splinePointsRight[numSplinePoints - 2].x;
                                quadP3.z = splinePointsRight[numSplinePoints - 2].z;
                                quadP4.x = splinePointsRight[0].x;
                                quadP4.z = splinePointsRight[0].z;

                                isMatch = IsInQuad(quadP1, quadP2, quadP3, quadP4, mapWorldPos);
                            }

                            if (isMatch)
                            {
                                if ((blendEdges && lbPath.edgeBlendWidth > 0f) || lbPath.removeCentre)
                                {
                                    // How far away from the edge is this point?
                                    sqrDistLeft = PlanarSquareDistance(mapWorldPos, quadP1);
                                    sqrDistRight = PlanarSquareDistance(mapWorldPos, quadP3);

                                    isLeftSide = (sqrDistLeft < sqrDistRight);
                                    // Left side
                                    if (isLeftSide)
                                    {
                                        // How wide is the path from centre to left at closest point?
                                        //sqrLeftWidth = PlanarSquareDistance(splinePointsCentre[closestPoint], quadP1);
                                        // What is the distance from this point to the left edge?
                                        sqrPixelDistFromEdge = SquareDistanceToSide(quadP1, quadP2, mapWorldPos);

                                        // If we're not within the border distance from the left edge, don't colour this pixel
                                        if (lbPath.removeCentre && sqrPixelDistFromEdge > sqrborderLeftWidth) { continue; }
                                    }
                                    // Right side
                                    else
                                    {
                                        // How wide is the path from centre to right at closest point?
                                        //sqrRightWidth = PlanarSquareDistance(splinePointsCentre[closestPoint], quadP3);
                                        // What is the distance from this point to the right edge?
                                        sqrPixelDistFromEdge = SquareDistanceToSide(quadP3, quadP4, mapWorldPos);

                                        // If we're not within the border distance from the right edge, don't colour this pixel
                                        if (lbPath.removeCentre && sqrPixelDistFromEdge > sqrborderRightWidth) { continue; }
                                    }

                                    // Only blend if we are within the BlendEdgeWidth distance
                                    if (sqrPixelDistFromEdge <= sqrblendEdgeWidth)
                                    {
                                        blendValue = sqrPixelDistFromEdge / sqrblendEdgeWidth;
                                        blendColour.r = blendValue;
                                        blendColour.g = blendValue;
                                        blendColour.b = blendValue;
                                        blendColour.a = (blendValue > 0f ? 1f : 0f);
                                        //blendColour = Color.Lerp(edgeColour, mapColour, sqrPixelDistFromEdge / sqrblendEdgeWidth);
                                    }
                                    else { blendColour = mapColour; }

                                    // Only process the ends if blending is required there
                                    // Blend ends settings are overridden if it is a complete closed circuit.
                                    if (blendEnds)
                                    {
                                        sqrDistToEnd = SquareDistanceToSide(lastLeftPoint, lastRightPoint, mapWorldPos);
                                        sqrDistToStart = SquareDistanceToSide(firstLeftPoint, firstRightPoint, mapWorldPos);

                                        // NOTE: If the path length < 2 x blend Edge Width, the results may be a little unpredicable
                                        if (lbPath.blendStart && sqrDistToStart <= sqrblendEdgeWidth)
                                        {
                                            endBlendColour = Color.Lerp(edgeColour, blendColour, sqrDistToStart / sqrblendEdgeWidth);
                                            if (endBlendColour.grayscale < blendColour.grayscale) { blendColour = endBlendColour; }
                                        }

                                        if (lbPath.blendEnd && sqrDistToEnd <= sqrblendEdgeWidth)
                                        {
                                            endBlendColour = Color.Lerp(edgeColour, blendColour, sqrDistToEnd / sqrblendEdgeWidth);
                                            if (endBlendColour.grayscale < blendColour.grayscale) { blendColour = endBlendColour; }
                                        }
                                    }

                                    map.SetPixel(x, y, blendColour);
                                }
                                else { map.SetPixel(x, y, mapColour); }
                            }
                        }
                    }
                    map.Apply();
                    isSuccess = true;
                }
            }
            return isSuccess;
        }


        /// <summary>
        /// Export a Texture2D map that follows a path with left and right splines in the landscape
        /// The splines are typically created by a LBMapPath. Similar to CreateMapFromPath except it
        /// the Texture2D is populated in a compute shader. It also uses the more modern, LBPath.CacheSplinePoints2().
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="lbPath"></param>
        /// <returns></returns>
        public bool CreateMapFromPathCompute(LBLandscape landscape, LBPath lbPath)
        {
            bool isSuccessful = false;
            string methodName = "LBMap.CreateMapFromPathCompute";

            if (landscape != null && map != null && lbPath != null)
            {
                // Force a refresh of spline cache
                lbPath.isSplinesCached2 = false;
                lbPath.CacheSplinePoints2();

                List<Vector3> splinePointsCentre = lbPath.cachedCentreSplinePointList;
                Vector3[] splinePointsLeft = lbPath.GetSplinePathEdgePoints2(LBPath.PositionType.Left, true, true);
                Vector3[] splinePointsRight = lbPath.GetSplinePathEdgePoints2(LBPath.PositionType.Right, true, true);

                bool checkEdges = (lbPath.edgeBlendWidth > 0f) || lbPath.removeCentre;
                float maxWidth = lbPath.GetMaxWidth();
                bool blendEnds = (lbPath.blendStart || lbPath.blendEnd);

                // Override blend ends settings if it is a complete closed circuit.
                if (lbPath.closedCircuit) { blendEnds = false; }

                int numSplineCentrePoints = (splinePointsCentre == null ? 0 : splinePointsCentre.Count);

                // Perform some validation
                if (splinePointsCentre == null) { Debug.LogWarning("ERROR: " + methodName + " - no centre splines defined"); }
                else if (splinePointsLeft == null) { Debug.LogWarning("ERROR: " + methodName + " - no left splines defined"); }
                else if (splinePointsRight == null) { Debug.LogWarning("ERROR: " + methodName + " - no right splines defined"); }
                else if (numSplineCentrePoints < 2) { Debug.LogWarning("ERROR: " + methodName + " - must have at least 2 centre spline points to map"); }
                else if (splinePointsLeft.Length < 2) { Debug.LogWarning("ERROR: " + methodName + " - must have at least 2 left spline points to map"); }
                else if (splinePointsRight.Length < 2) { Debug.LogWarning("ERROR: " + methodName + "- must have at least 2 right spline points to map"); }
                else if (splinePointsLeft.Length != splinePointsRight.Length) { Debug.LogWarning("ERROR: " + methodName + " - in this release the number of left and right spline points must be the same"); }
                else if (numSplineCentrePoints != splinePointsRight.Length) { Debug.LogWarning("ERROR: " + methodName + " - in this release the number of centre, left and right spline points must be the same"); }
                else
                {
                    #if LB_MAP_COMPUTE

                    #if !(UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN || UNITY_XBOXONE || UNITY_WSA_10_0)
                    bool isPathComputeEnabled = false;
                    #else
                    bool isPathComputeEnabled = (landscape == null ? false : landscape.useGPUPath);
                    #endif

                    if (isPathComputeEnabled)
                    {
                        #region Reset Garbage Collection
                        System.GC.Collect();
                        #endregion

                        #region Determine Quad Look Ahead
                        // If changing, also change CreateMapFromPath()

                        // Based on the width and the path resolution (segment length), determine the maximum number of quad to look forward
                        // or backwards from the current point.
                        float quadLookAhead = Mathf.Sqrt((((maxWidth * maxWidth) / (4f * lbPath.pathResolution * lbPath.pathResolution)) + 0.25f));
                        int quadLookAheadInt = Mathf.CeilToInt(quadLookAhead);

                        // Disable for now, may not be required
                        quadLookAheadInt = 1;
                        #endregion

                        #region Initialise Compute
                        int cskPathNumThreads = 16; // Must match LB_PATH_NUM_THREADS in LBCSPath.compute
                        ComputeShader shaderPath = null;
                        ComputeBuffer cbufSplinePointsCentre = null;
                        ComputeBuffer cbufSplinePointsLeft = null;
                        ComputeBuffer cbufSplinePointsRight = null;
                        RenderTexture mapRT = null;
                        int mapTexWidth = map.width;
                        int mapTexHeight = map.height;

                        shaderPath = (ComputeShader)Resources.Load(LBCSPath, typeof(ComputeShader));
                        if (shaderPath == null) { Debug.LogWarning("ERROR: " + methodName + " " + LBCSPath + ".shader not found. Please Report"); }
                        #endregion

                        #region Compute Map Texture

                        try
                        {
                            if (shaderPath != null)
                            {
                                // Get the index to the Method in the compute shader
                                int kPathCreateMapIdx = shaderPath.FindKernel(CSKPathCreateMap);

                                #region Create Buffers
                                shaderPath.SetInt(CSnumSplineCentrePoints, numSplineCentrePoints);

                                // Assume all spline have the same number of items (which is validated above)
                                cbufSplinePointsCentre = new ComputeBuffer(numSplineCentrePoints, sizeof(float) * 3, ComputeBufferType.Default);
                                cbufSplinePointsLeft = new ComputeBuffer(numSplineCentrePoints, sizeof(float) * 3, ComputeBufferType.Default);
                                cbufSplinePointsRight = new ComputeBuffer(numSplineCentrePoints, sizeof(float) * 3, ComputeBufferType.Default);
                                #endregion

                                #region Create RenderTexture
                                mapRT = new RenderTexture(mapTexWidth, mapTexHeight, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                                if (mapRT != null)
                                {
                                    mapRT.useMipMap = false;
                                    mapRT.autoGenerateMips = false;
                                    mapRT.hideFlags = HideFlags.DontSave;
                                    mapRT.enableRandomWrite = true;
                                    mapRT.name = map.name;
                                    mapRT.Create();
                                }
                                #endregion

                                if (cbufSplinePointsCentre != null && cbufSplinePointsLeft != null && cbufSplinePointsRight != null)
                                {
                                    #region Copy spline data to shader
                                    // Allocate buffers to the compute shader method
                                    shaderPath.SetBuffer(kPathCreateMapIdx, CSsplinePointsCentre, cbufSplinePointsCentre);
                                    shaderPath.SetBuffer(kPathCreateMapIdx, CSsplinePointsLeft, cbufSplinePointsLeft);
                                    shaderPath.SetBuffer(kPathCreateMapIdx, CSsplinePointsRight, cbufSplinePointsRight);
                                    // Copy the data to the shader
                                    cbufSplinePointsCentre.SetData(splinePointsCentre.ToArray());
                                    cbufSplinePointsLeft.SetData(splinePointsLeft);
                                    cbufSplinePointsRight.SetData(splinePointsRight);
                                    #endregion

                                    #region Initialise map texture data
                                    shaderPath.SetInt(CSmapTexWidth, mapTexWidth);
                                    shaderPath.SetInt(CSmapTexHeight, mapTexHeight);
                                    shaderPath.SetTexture(kPathCreateMapIdx, CSmapTex, mapRT);
                                    #endregion

                                    #region Set path variables in shader
                                    shaderPath.SetFloat(CSmaxWidth, maxWidth);
                                    shaderPath.SetBool(CScheckEdges, checkEdges);
                                    shaderPath.SetBool(CSblendEnds, blendEnds);
                                    shaderPath.SetBool(CSblendStart, lbPath.blendStart);
                                    shaderPath.SetBool(CSblendEnd, lbPath.blendEnd);
                                    shaderPath.SetBool(CSclosedCircuit, lbPath.closedCircuit);
                                    shaderPath.SetInt(CSquadLookAhead, quadLookAheadInt);
                                    shaderPath.SetBool(CSremoveCentre, lbPath.removeCentre);
                                    shaderPath.SetFloat(CSedgeBlendWidth, lbPath.edgeBlendWidth);
                                    shaderPath.SetFloat(CSsqrblendEdgeWidth, lbPath.edgeBlendWidth * lbPath.edgeBlendWidth);
                                    // Used when removeCentre = true
                                    shaderPath.SetFloat(CSsqrborderLeftWidth, lbPath.leftBorderWidth * lbPath.leftBorderWidth);
                                    shaderPath.SetFloat(CSsqrborderRightWidth, lbPath.rightBorderWidth * lbPath.rightBorderWidth);
                                    shaderPath.SetVector(CSpathBounds, LBPath.GetSplineBounds(splinePointsLeft, splinePointsRight));
                                    #endregion

                                    #region Unused shader variables
                                    // Variables not used with PathCreateMap but set to avoid issues
                                    shaderPath.SetInt(CSheightmapResolution, 1);
                                    shaderPath.SetFloat(CSheightScale, 0f);
                                    shaderPath.SetFloat(CSminHeight, 0f);
                                    shaderPath.SetInt(CSpathBlendCurveNumKeys, 0);
                                    shaderPath.SetInt(CSpathHeightCurveNumKeys, 0);
                                    shaderPath.SetInt(CSpathTypeMode, 0);
                                    shaderPath.SetFloat(CSpathInvertMultipler, 1f);
                                    shaderPath.SetFloat(CSsurroundSmoothing, 0f);
                                    shaderPath.SetFloat(CSblendTerrainHeight, 0f);
                                    // Set unused terrain variables
                                    shaderPath.SetFloat(CSterrainWidth, 0f);
                                    shaderPath.SetFloat(CSterrainLength, 0f);
                                    shaderPath.SetVector(CSterrainWorldPos, Vector3.zero);
                                    shaderPath.SetFloat(CSterrainHeight, 1f);
                                    #endregion

                                    #region Set landscape variables in shader
                                    // Landscape variables
                                    shaderPath.SetVector(CSlandscapePos, landscape.transform.position);
                                    shaderPath.SetVector(CSlandscapeSize, landscape.size);
                                    #endregion

                                    #region Execute Shader

                                    // This assumes input map texture is 2 ^ n and is greater than cskPathNumThreads wide and high
                                    int threadGroupX = Mathf.CeilToInt(mapTexWidth / cskPathNumThreads);
                                    int threadGroupY = Mathf.CeilToInt(mapTexHeight / cskPathNumThreads);

                                    shaderPath.Dispatch(kPathCreateMapIdx, threadGroupX, threadGroupY, 1);
                                    #endregion

                                    #region Get Computed map texture

                                    // Copy RenderTexture into Texture2D
                                    // Remember the current active texture
                                    RenderTexture currentRT = RenderTexture.active;
                                    RenderTexture.active = mapRT;
                                    // ReadPixels reads from the active RenderTexture and writes to the Texture2D
                                    map.ReadPixels(new Rect(0, 0, mapTexWidth, mapTexHeight), 0, 0);
                                    map.Apply();
                                    // Restore the current texture
                                    RenderTexture.active = currentRT;

                                    #endregion
                                }
                            }
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogWarning("ERROR " + methodName + " - could not compute map texture. " + ex.Message);
                        }
                        #region Free Resources for Compute shader
                        finally
                        {
                            if (cbufSplinePointsCentre != null) { cbufSplinePointsCentre.Release(); cbufSplinePointsCentre = null; }
                            if (cbufSplinePointsLeft != null) { cbufSplinePointsLeft.Release(); cbufSplinePointsLeft = null; }
                            if (cbufSplinePointsRight != null) { cbufSplinePointsRight.Release(); cbufSplinePointsRight = null; }
                            LBTextureOperations.DestroyRenderTexture(ref mapRT);
                        }
                        #endregion

                        #endregion
                    }
                    #endif
                }
            }

            return isSuccessful;
        }

        #endregion

        #region Static Shape and Point Math Methods

        private static bool IsInPolygon(List<Vector2> points, Vector2 sample)
        {
            bool isInPolygon = false;

            int j = points.Count - 1;

            for (int i = 0; i < points.Count; j = i++)
            {
                if (((points[i].y <= sample.y && sample.y < points[j].y) || (points[j].y <= sample.y && sample.y < points[i].y)) &&
                   (sample.x < (points[j].x - points[i].x) * (sample.y - points[i].y) / (points[j].y - points[i].y) + points[i].x))
                    isInPolygon = !isInPolygon;
            }

            return isInPolygon;
        }

        private static bool IsInPolygon2(List<Vector2> points, Vector2 sample)
        {
            bool isInPolygon = false;

            int j = points.Count - 1;

            for (int i = 0; i < points.Count; j = i++)
            {
                if (((points[i].y < sample.y && sample.y < points[j].y) || (points[j].y < sample.y && sample.y < points[i].y)) &&
                   (sample.x < (points[j].x - points[i].x) * (sample.y - points[i].y) / (points[j].y - points[i].y) + points[i].x))
                { isInPolygon = !isInPolygon; }
            }

            return isInPolygon;
        }

        /// <summary>
        /// Is the sample point inside the quad which has points p1, p2, p3 and p4?
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        /// <param name="p4"></param>
        /// <param name="sample"></param>
        /// <returns></returns>
        public static bool IsInQuad(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, Vector3 sample)
        {
            //return true;
            return (IsInTriangle(p1, p2, p3, sample) || IsInTriangle(p4, p2, p3, sample));
        }

        /// <summary>
        /// Is the sample point inside the triangle which has points: p1,p2 & p3
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        /// <param name="sample"></param>
        /// <returns></returns>
        public static bool IsInTriangle(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 sample)
        {
            //return (IsSameSide(p1, p2, sample, p3) && IsSameSide(p1, p3, sample, p2) && IsSameSide(p2, p3, sample, p1));

            //float angle1 = Mathf.Abs(Vector3.Angle(p1 - sample, p2 - sample));
            //float angle2 = Mathf.Abs(Vector3.Angle(p2 - sample, p3 - sample));
            //float angle3 = Mathf.Abs(Vector3.Angle(p3 - sample, p1 - sample));
            ////return (Mathf.Approximately(angle1 + angle2 + angle3, 360f));
            //float sumAngles = angle1 + angle2 + angle3;

            //return ((sumAngles > 359.9f && sumAngles < 360.1f));
            //return ((sumAngles > 179.5f && sumAngles < 180.5f) || (sumAngles > 359.9f && sumAngles < 360.1f));

            bool halfPlaneSide1 = HalfPlaneSideSign(sample, p1, p2) < 0f;
            bool halfPlaneSide2 = HalfPlaneSideSign(sample, p2, p3) < 0f;
            bool halfPlaneSide3 = HalfPlaneSideSign(sample, p3, p1) < 0f;

            // Working code
            return ((halfPlaneSide1 == halfPlaneSide2) && (halfPlaneSide2 == halfPlaneSide3));

            // Old code
            //if ((halfPlaneSide1 == halfPlaneSide2) && (halfPlaneSide2 == halfPlaneSide3)) { return true; }
            //else if (SquareDistanceToSide(p1, p2, sample) < 60f || SquareDistanceToSide(p1, p3, sample) < 60f || SquareDistanceToSide(p2, p3, sample) < 60f)
            //{
            //    return true;
            //}
            //else { return false; }
        }


        public static float HalfPlaneSideSign(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            return (p1.x - p3.x) * (p2.z - p3.z) - (p2.x - p3.x) * (p1.z - p3.z);
        }

        /// <summary>
        /// Allows for a "thickness" of each triangle to be specified to allow for error
        /// </summary>
        /// <param name="sp1"></param>
        /// <param name="sp2"></param>
        /// <param name="sample"></param>
        /// <returns></returns>
        public static float SquareDistanceToSide(Vector3 sp1, Vector3 sp2, Vector3 sample)
        {
            float squareSideLength = PlanarSquareDistance(sp1, sp2);
            float dotProduct = ((sample.x - sp1.x) * (sp2.x - sp1.x) + (sample.z - sp1.z) * (sp2.z - sp1.z)) / squareSideLength;
            if (dotProduct < 0)
            {
                return PlanarSquareDistance(sample, sp1);
            }
            else if (dotProduct <= 1)
            {
                return PlanarSquareDistance(sample, sp1) - dotProduct * dotProduct * squareSideLength;
            }
            else
            {
                return PlanarSquareDistance(sample, sp2);
            }
        }

        /// <summary>
        /// Square distance calculation ignoring y distance
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static float PlanarSquareDistance(Vector3 p1, Vector3 p2)
        {
            // Basically pythagoras but without y and without final square root
            return (((p1.x - p2.x) * (p1.x - p2.x)) + ((p1.z - p2.z) * (p1.z - p2.z)));
        }

        /// <summary>
        /// Are samplep1 and samplep2 points on the same side as the line between sp1 and sp2
        /// OLD METHOD: CURRENTLY UNUSED
        /// </summary>
        /// <param name="sp1"></param>
        /// <param name="sp2"></param>
        /// <param name="samplep1"></param>
        /// <param name="samplep2"></param>
        /// <returns></returns>
        public static bool IsSameSide(Vector3 sp1, Vector3 sp2, Vector3 samplep1, Vector3 samplep2)
        {
            // OLD METHOD: CURRENTLY UNUSED
            //Vector3 cp1 = Vector3.Cross(p2 - p1, samplep1 - p1);
            //Vector3 cp2 = Vector3.Cross(p2 - p1, samplep2 - p1);
            return (Vector3.Dot(Vector3.Cross(sp2 - sp1, samplep1 - sp1), Vector3.Cross(sp2 - sp1, samplep2 - sp1)) >= 0);
        }

        /// <summary>
        /// Find the closest central spline point
        /// </summary>
        /// <param name="splinePoints"></param>
        /// <param name="pointToMatch"></param>
        /// <returns></returns>
        public static int FindClosestPoint(Vector3[] splinePoints, Vector3 pointToMatch)
        {
            float sqrDist = 0f;
            float closestSqrDist = Mathf.Infinity;
            int closestPoint = 0;

            if (splinePoints != null)
            {
                for (int i = 0; i < splinePoints.Length; i++)
                {
                    sqrDist = PlanarSquareDistance(splinePoints[i], pointToMatch);
                    if (sqrDist < closestSqrDist) { closestSqrDist = sqrDist; closestPoint = i; }
                }
            }

            return closestPoint;
        }

        /// <summary>
        /// Find closest consecutive path point to this one
        /// </summary>
        /// <param name="splinePoints"></param>
        /// <param name="pointToMatch"></param>
        /// <param name="consecutiveTo"></param>
        /// <returns></returns>
        public static int FindClosestConsecutivePoint(Vector3[] splinePoints, Vector3 pointToMatch, int consecutiveTo)
        {
            int closestPoint = 0;

            if (splinePoints != null)
            {
                // Check if the consecutive points exist
                bool c1Exists = consecutiveTo - 1 >= 0;
                bool c2Exists = splinePoints.Length > consecutiveTo + 1;
                if (c1Exists && c2Exists)
                {
                    // Compare the distances to both of the consecutive points, return the closest point
                    if (PlanarSquareDistance(splinePoints[consecutiveTo - 1], pointToMatch) < PlanarSquareDistance(splinePoints[consecutiveTo + 1], pointToMatch))
                    {
                        closestPoint = consecutiveTo - 1;
                    }
                    else { closestPoint = consecutiveTo + 1; }
                }
                // Return any point that exists
                else if (c1Exists) { closestPoint = consecutiveTo - 1; }
                else if (c2Exists) { closestPoint = consecutiveTo + 1; }
            }

            return closestPoint;
        }

        /// <summary>
        /// Find furthest consecutive path point to this one
        /// </summary>
        /// <param name="splinePoints"></param>
        /// <param name="pointToMatch"></param>
        /// <param name="consecutiveTo"></param>
        /// <returns></returns>
        public static int FindFurthestConsecutivePoint(Vector3[] splinePoints, Vector3 pointToMatch, int consecutiveTo)
        {
            int closestPoint = 0;

            if (splinePoints != null)
            {
                // Check if the consecutive points exist
                bool c1Exists = consecutiveTo - 1 >= 0;
                bool c2Exists = splinePoints.Length > consecutiveTo + 1;
                if (c1Exists && c2Exists)
                {
                    // Compare the distances to both of the consecutive points, return the furthest point
                    if (PlanarSquareDistance(splinePoints[consecutiveTo - 1], pointToMatch) < PlanarSquareDistance(splinePoints[consecutiveTo + 1], pointToMatch))
                    {
                        closestPoint = consecutiveTo + 1;
                    }
                    else { closestPoint = consecutiveTo - 1; }
                }
                // Return any point that exists
                else if (c1Exists) { closestPoint = consecutiveTo - 1; }
                else if (c2Exists) { closestPoint = consecutiveTo + 1; }
            }

            return closestPoint;
        }

        #endregion

    }
}