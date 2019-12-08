#define _LBCURVE_DEBUG_MODE
using UnityEngine;
using System.Collections;

namespace LandscapeBuilder
{
    public class LBCurve
    {
        // Curve class
        private static int keyInt;

        #region Enumerations

        /// <summary>
        /// Full set of curve presets. If making changes,
        /// always attempt to keep same numbers for existing
        /// presets
        /// </summary>
        public enum CurvePreset
        {
            None = 0,
            Invert = 1,
            IncreaseHeight = 2,
            DecreaseHeight = 3,
            OutputMinMax = 4,
            InputMinMax = 5,
            Ridged = 6,
            SmoothRidged = 7,
            DoubleRidged = 8,
            SmoothDoubleRidged = 9,
            PowerOfOnePointFive = 10,
            Squared = 11,
            Cubed = 12,
            PowerOfFour = 13,
            VerySmoothTerraced = 20,
            SmoothTerraced = 21,
            SteepTerraced = 22,
            VerySmoothDoubleTerraced = 24,
            SmoothDoubleTerraced = 25,
            SteepDoubleTerraced = 26,
            SharpDoubleTerraced = 27,
            FiveTieredTerraced = 40,
            TenTieredTerraced = 41,
            TwentyTieredTerraced = 42,
            CanyonTerracing1 = 50,
            CanyonTerracing2 = 51,
            CanyonTerracing3 = 52,
            IslandSmoothing1 = 60,
            IslandSmoothing2 = 61,
            IslandSmoothing3 = 62
        }

        /// <summary>
        /// These are a subset of CurvePreset which can be
        /// used when blending two items together
        /// </summary>
        public enum BlendCurvePreset
        {
            PowerOfOnePointFive = 10,
            Squared = 11,
            Cubed = 12,
            PowerOfFour = 13,
        }

        public enum FilterCurvePreset
        {
            Default,
            WideRange,
            MaxIncreasing,
            MinIncreasing
        }

        public enum MapPathBlendCurvePreset
        {
            EaseInOut = 0,
            Linear = 5
        }

        public enum MapPathHeightCurvePreset
        {
            Flat = 0,
            River1Set = 10,
            River2Set = 11,
            River3Set = 12,
            River4Set = 13,
            River5Set = 14,
            River6Set = 15,
            River1Subtract = 30,
            River2Subtract = 31,
            River3Subtract = 32,
            River4Subtract = 33,
            River5Subtract = 34,
            River6Subtract = 35,
            //Hump1 = 100,
            SlopeLeftSet = 150,
            SlopeRightSet = 155
        }

        public enum ObjPathHeightCurvePreset
        {
            Flat = 0,
            River1 = 10,
            River2 = 11,
            River3 = 12,
            River4 = 13,
            River5 = 14
        }

        #endregion

        #region Script Curve Methods

        /// <summary>
        /// Returns the C# code required to recreate an animation curve.
        /// This can be useful when creating Runtime versions of the Landscape
        /// </summary>
        /// <param name="curve"></param>
        /// <param name="EndOfLineMarker"></param>
        /// <param name="curveName"></param>
        /// <returns></returns>
        public static string ScriptCurve(AnimationCurve curve, string EndOfLineMarker = "\n", string curveName = "newCurve")
        {
            string scriptString = string.Empty;
            string eol = " ";

            if (curve != null)
            {
                // We always need a space between lines OR a end of line marker like "\n"
                if (EndOfLineMarker.Length > 0) { eol = EndOfLineMarker; }

                if (string.IsNullOrEmpty(curveName)) { curveName = "newCurve"; }

                scriptString = "AnimationCurve " + curveName + " = new AnimationCurve();" + eol;

                if (curve.keys != null)
                {
                    if (curve.keys.Length > 0)
                    {
                        // Output the keys
                        foreach (Keyframe key in curve.keys)
                        {
                            scriptString += curveName + ".AddKey(" + key.time.ToString("0.00") + "f," + key.value.ToString("0.00") + "f);" + eol;
                        }

                        scriptString += "Keyframe[] " + curveName + "Keys = " + curveName + ".keys;" + eol;

                        // Output the trangents
                        int keyNumber = 0;
                        foreach (Keyframe key in curve.keys)
                        {
                            scriptString += curveName + "Keys[" + (keyNumber).ToString() + "].inTangent = " + key.inTangent.ToString("0.00") + "f;" + eol;
                            scriptString += curveName + "Keys[" + (keyNumber++).ToString() + "].outTangent = " + key.outTangent.ToString("0.00") + "f;" + eol;
                        }

                        scriptString += curveName + " = new AnimationCurve(" + curveName + "Keys);" + eol + eol;
                    }
                }
            }

            return scriptString;
        }

        /// <summary>
        /// Legacy version. Use ScriptCurve() instead.
        /// Returns the C# code required to recreate an animation curve.
        /// This can be useful when creating Runtime versions of the Landscape
        /// </summary>
        /// <param name="curve"></param>
        /// <param name="EndOfLineMarker"></param>
        /// <returns></returns>
        public static string ScriptCurve2(AnimationCurve curve, string EndOfLineMarker = "\n", string curveName = "newCurve")
        {
            string scriptString = string.Empty;
            string eol = " ";

            if (curve != null)
            {
                // We always need a space between lines OR a end of line marker like "\n"
                if (EndOfLineMarker.Length > 0) { eol = EndOfLineMarker; }

                if (string.IsNullOrEmpty(curveName)) { curveName = "newCurve"; }

                scriptString = "AnimationCurve " + curveName + " = new AnimationCurve();" + eol;

                Keyframe[] keys = curve.keys;
                if (keys != null)
                {
                    if (keys.Length > 0)
                    {
                        scriptString += "int keyInt=0;" + eol;

                        // Output the keys
                        foreach (Keyframe key in keys)
                        {
                            scriptString += "keyInt = " + curveName + ".AddKey(" + key.time.ToString("0.00") + "f," + key.value.ToString("0.00") + "f);" + eol;
                        }

                        scriptString += "Keyframe[] curveKeys = " + curveName + ".keys;" + eol;

                        // Output the trangents
                        int keyNumber = 0;
                        foreach (Keyframe key in keys)
                        {
                            scriptString += "curveKeys[" + (keyNumber).ToString() + "].inTangent = " + key.inTangent.ToString("0.00") + "f;" + eol;
                            scriptString += "curveKeys[" + (keyNumber++).ToString() + "].outTangent = " + key.outTangent.ToString("0.00") + "f;" + eol;
                        }

                        scriptString += curveName + " = new AnimationCurve(curveKeys);" + eol;
                        scriptString += "// Stops warning 'variable is assigned but its value is never used' from appearing in the compiler" + eol;
                        scriptString += "if (keyInt == 0) {}" + eol + eol;
                    }
                }
            }

            return scriptString;
        }

        #endregion

        #region Default Curve Methods

        public static AnimationCurve DefaultFogDensityCurve()
        {
            AnimationCurve newCurve = new AnimationCurve();
            keyInt = newCurve.AddKey(0f, 1f);
            keyInt = newCurve.AddKey(0.16f, 1f);
            keyInt = newCurve.AddKey(0.33f, 0f);
            keyInt = newCurve.AddKey(0.66f, 0f);
            keyInt = newCurve.AddKey(0.83f, 1f);
            keyInt = newCurve.AddKey(1f, 1f);
            Keyframe[] curveKeys = newCurve.keys;
            curveKeys[0].inTangent = 0f;
            curveKeys[0].outTangent = 0f;
            curveKeys[1].inTangent = 0f;
            curveKeys[1].outTangent = -5.9f;
            curveKeys[2].inTangent = -5.9f;
            curveKeys[2].outTangent = 0f;
            curveKeys[3].inTangent = 0f;
            curveKeys[3].outTangent = 5.9f;
            curveKeys[4].inTangent = 5.9f;
            curveKeys[4].outTangent = 0f;
            curveKeys[5].inTangent = 0f;
            curveKeys[5].outTangent = 0f;
            newCurve = new AnimationCurve(curveKeys);

            // Stops warning "variable is assigned but its value is never used" from appearing in the compiler
            if (keyInt == 0) { }

            return newCurve;
        }

        public static AnimationCurve DefaultSunIntensityCurve()
        {
            AnimationCurve newCurve = new AnimationCurve();
            keyInt = newCurve.AddKey(0f, 0f);
            keyInt = newCurve.AddKey(0.25f, 0f);
            keyInt = newCurve.AddKey(0.29f, 1f);
            keyInt = newCurve.AddKey(0.71f, 1f);
            keyInt = newCurve.AddKey(0.75f, 0f);
            keyInt = newCurve.AddKey(1f, 0f);
            Keyframe[] curveKeys = newCurve.keys;
            curveKeys[0].inTangent = 0f;
            curveKeys[0].outTangent = 0f;
            curveKeys[1].inTangent = 0f;
            curveKeys[1].outTangent = 25f;
            curveKeys[2].inTangent = 25f;
            curveKeys[2].outTangent = 0f;
            curveKeys[3].inTangent = 0f;
            curveKeys[3].outTangent = -25f;
            curveKeys[4].inTangent = -25f;
            curveKeys[4].outTangent = 0f;
            curveKeys[5].inTangent = 0f;
            curveKeys[5].outTangent = 0f;
            newCurve = new AnimationCurve(curveKeys);

            // Stops warning "variable is assigned but its value is never used" from appearing in the compiler
            if (keyInt == 0) { }

            return newCurve;
        }

        public static AnimationCurve DefaultMoonIntensityCurve()
        {
            AnimationCurve newCurve = new AnimationCurve();
            keyInt = newCurve.AddKey(0f, 1f);
            keyInt = newCurve.AddKey(0.21f, 1f);
            keyInt = newCurve.AddKey(0.25f, 0f);
            keyInt = newCurve.AddKey(0.75f, 0f);
            keyInt = newCurve.AddKey(0.79f, 1f);
            keyInt = newCurve.AddKey(1f, 1f);
            Keyframe[] curveKeys = newCurve.keys;
            curveKeys[0].inTangent = 0f;
            curveKeys[0].outTangent = 0f;
            curveKeys[1].inTangent = 0f;
            curveKeys[1].outTangent = -25f;
            curveKeys[2].inTangent = -25f;
            curveKeys[2].outTangent = 0f;
            curveKeys[3].inTangent = 0f;
            curveKeys[3].outTangent = 25f;
            curveKeys[4].inTangent = 25f;
            curveKeys[4].outTangent = 0f;
            curveKeys[5].inTangent = 0f;
            curveKeys[5].outTangent = 0f;
            newCurve = new AnimationCurve(curveKeys);

            // Stops warning "variable is assigned but its value is never used" from appearing in the compiler
            if (keyInt == 0) { }

            return newCurve;
        }

        public static AnimationCurve DefaultStarVisibilityCurve()
        {
            AnimationCurve newCurve = new AnimationCurve();
            keyInt = newCurve.AddKey(0f, 1f);
            keyInt = newCurve.AddKey(0.1f, 1f);
            keyInt = newCurve.AddKey(0.25f, 0f);
            keyInt = newCurve.AddKey(0.75f, 0f);
            keyInt = newCurve.AddKey(0.9f, 1f);
            keyInt = newCurve.AddKey(1f, 1f);
            Keyframe[] curveKeys = newCurve.keys;
            curveKeys[0].inTangent = 0f;
            curveKeys[0].outTangent = 0f;
            curveKeys[1].inTangent = 0f;
            curveKeys[1].outTangent = -6.67f;
            curveKeys[2].inTangent = -6.67f;
            curveKeys[2].outTangent = 0f;
            curveKeys[3].inTangent = 0f;
            curveKeys[3].outTangent = 6.67f;
            curveKeys[4].inTangent = 6.67f;
            curveKeys[4].outTangent = 0f;
            curveKeys[5].inTangent = 0f;
            curveKeys[5].outTangent = 0f;
            newCurve = new AnimationCurve(curveKeys);

            // Stops warning "variable is assigned but its value is never used" from appearing in the compiler
            if (keyInt == 0) { }

            return newCurve;
        }

        #endregion

        #region Curve Preset Methods

        /// <summary>
        /// Returns a curve given a preset for a curve
        /// </summary>
        public static AnimationCurve SetCurveFromPreset(LBCurve.CurvePreset curvePreset)
        {
            AnimationCurve newCurve = new AnimationCurve();
            if (curvePreset == CurvePreset.None)
            {
                newCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            }
            else if (curvePreset == CurvePreset.Invert)
            {
                newCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            }
            else if (curvePreset == CurvePreset.IncreaseHeight)
            {
                keyInt = newCurve.AddKey(0f, 0f);
                keyInt = newCurve.AddKey(0.45f, 0.55f);
                keyInt = newCurve.AddKey(1f, 1f);
            }
            else if (curvePreset == CurvePreset.DecreaseHeight)
            {
                keyInt = newCurve.AddKey(0f, 0f);
                keyInt = newCurve.AddKey(0.55f, 0.45f);
                keyInt = newCurve.AddKey(1f, 1f);
            }
            else if (curvePreset == CurvePreset.OutputMinMax)
            {
                keyInt = newCurve.AddKey(0f, 0.25f);
                keyInt = newCurve.AddKey(0.25f, 0.25f);
                keyInt = newCurve.AddKey(0.75f, 0.75f);
                keyInt = newCurve.AddKey(1f, 0.75f);
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = 0f;
                curveKeys[0].outTangent = 0f;
                curveKeys[1].inTangent = 0f;
                curveKeys[1].outTangent = 1f;
                curveKeys[2].inTangent = 1f;
                curveKeys[2].outTangent = 0f;
                curveKeys[3].inTangent = 0f;
                curveKeys[3].outTangent = 0f;
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (curvePreset == CurvePreset.InputMinMax)
            {
                keyInt = newCurve.AddKey(0f, 0f);
                keyInt = newCurve.AddKey(0.25f, 0f);
                keyInt = newCurve.AddKey(0.75f, 1f);
                keyInt = newCurve.AddKey(1f, 1f);
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = 0f;
                curveKeys[0].outTangent = 0f;
                curveKeys[1].inTangent = 0f;
                curveKeys[1].outTangent = 2f;
                curveKeys[2].inTangent = 2f;
                curveKeys[2].outTangent = 0f;
                curveKeys[3].inTangent = 0f;
                curveKeys[3].outTangent = 0f;
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (curvePreset == CurvePreset.Ridged)
            {
                keyInt = newCurve.AddKey(0f, 0f);
                keyInt = newCurve.AddKey(0.5f, 1f);
                keyInt = newCurve.AddKey(1f, 0f);
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = 2f;
                curveKeys[0].outTangent = 2f;
                curveKeys[1].inTangent = 2f;
                curveKeys[1].outTangent = -2f;
                curveKeys[2].inTangent = -2f;
                curveKeys[2].outTangent = -2f;
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (curvePreset == CurvePreset.SmoothRidged)
            {
                // Primary keys
                keyInt = newCurve.AddKey(0f, 0f);
                // Interpolation sharpeners 
                keyInt = newCurve.AddKey(0.05f, 0.1f);
                keyInt = newCurve.AddKey(0.45f, 0.9f);
                // Primary keys
                keyInt = newCurve.AddKey(0.5f, 1f);
                // Interpolation sharpeners 
                keyInt = newCurve.AddKey(0.55f, 0.9f);
                keyInt = newCurve.AddKey(0.95f, 0.1f);
                // Primary keys
                keyInt = newCurve.AddKey(1f, 0f);
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = 0f;
                curveKeys[0].outTangent = 0f;
                curveKeys[1].inTangent = 2f;
                curveKeys[1].outTangent = 2f;
                curveKeys[2].inTangent = 2f;
                curveKeys[2].outTangent = 2f;
                curveKeys[3].inTangent = 0f;
                curveKeys[3].outTangent = 0f;
                curveKeys[4].inTangent = -2f;
                curveKeys[4].outTangent = -2f;
                curveKeys[5].inTangent = -2f;
                curveKeys[5].outTangent = -2f;
                curveKeys[6].inTangent = 0f;
                curveKeys[6].outTangent = 0f;
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (curvePreset == CurvePreset.DoubleRidged)
            {
                keyInt = newCurve.AddKey(0f, 0.5f);
                keyInt = newCurve.AddKey(0.25f, 0f);
                keyInt = newCurve.AddKey(0.5f, 1f);
                keyInt = newCurve.AddKey(0.75f, 0f);
                keyInt = newCurve.AddKey(1f, 0.5f);
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = -2f;
                curveKeys[0].outTangent = -2f;
                curveKeys[1].inTangent = -2f;
                curveKeys[1].outTangent = 4f;
                curveKeys[2].inTangent = 4f;
                curveKeys[2].outTangent = -4f;
                curveKeys[3].inTangent = -4f;
                curveKeys[3].outTangent = 2f;
                curveKeys[4].inTangent = 2f;
                curveKeys[4].outTangent = 2f;
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (curvePreset == CurvePreset.SmoothDoubleRidged)
            {
                // Primary keys
                keyInt = newCurve.AddKey(0f, 0.5f);
                // Interpolation sharpeners 
                keyInt = newCurve.AddKey(0.05f, 0.4f);
                keyInt = newCurve.AddKey(0.2f, 0.1f);
                // Primary keys
                keyInt = newCurve.AddKey(0.25f, 0f);
                // Interpolation sharpeners 
                keyInt = newCurve.AddKey(0.3f, 0.2f);
                keyInt = newCurve.AddKey(0.45f, 0.8f);
                // Primary keys
                keyInt = newCurve.AddKey(0.5f, 1f);
                // Interpolation sharpeners 
                keyInt = newCurve.AddKey(0.55f, 0.8f);
                keyInt = newCurve.AddKey(0.7f, 0.2f);
                // Primary keys
                keyInt = newCurve.AddKey(0.75f, 0f);
                // Interpolation sharpeners 
                keyInt = newCurve.AddKey(0.8f, 0.1f);
                keyInt = newCurve.AddKey(0.95f, 0.4f);
                // Primary keys
                keyInt = newCurve.AddKey(1f, 0.5f);
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = 0f;
                curveKeys[0].outTangent = 0f;
                curveKeys[1].inTangent = -2f;
                curveKeys[1].outTangent = -2f;
                curveKeys[2].inTangent = -2f;
                curveKeys[2].outTangent = -2f;
                curveKeys[3].inTangent = 0f;
                curveKeys[3].outTangent = 0f;
                curveKeys[4].inTangent = 4f;
                curveKeys[4].outTangent = 4f;
                curveKeys[5].inTangent = 4f;
                curveKeys[5].outTangent = 4f;
                curveKeys[6].inTangent = 0f;
                curveKeys[6].outTangent = 0f;
                curveKeys[7].inTangent = -4f;
                curveKeys[7].outTangent = -4f;
                curveKeys[8].inTangent = -4f;
                curveKeys[8].outTangent = -4f;
                curveKeys[9].inTangent = 0f;
                curveKeys[9].outTangent = 0f;
                curveKeys[10].inTangent = 2f;
                curveKeys[10].outTangent = 2f;
                curveKeys[11].inTangent = 2f;
                curveKeys[11].outTangent = 2f;
                curveKeys[12].inTangent = 0f;
                curveKeys[12].outTangent = 0f;
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (curvePreset == CurvePreset.PowerOfOnePointFive)
            {
                keyInt = newCurve.AddKey(0f, 0f);
                keyInt = newCurve.AddKey(0.5f, 0.35f);
                keyInt = newCurve.AddKey(1f, 1f);
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = 0f;
                curveKeys[0].outTangent = 0f;
                curveKeys[1].inTangent = 1.06f;
                curveKeys[1].outTangent = 1.06f;
                curveKeys[2].inTangent = 1.5f;
                curveKeys[2].outTangent = 1.5f;
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (curvePreset == CurvePreset.Squared)
            {
                keyInt = newCurve.AddKey(0f, 0f);
                keyInt = newCurve.AddKey(0.5f, 0.25f);
                keyInt = newCurve.AddKey(1f, 1f);
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = 0f;
                curveKeys[0].outTangent = 0f;
                curveKeys[1].inTangent = 1f;
                curveKeys[1].outTangent = 1f;
                curveKeys[2].inTangent = 2f;
                curveKeys[2].outTangent = 2f;
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (curvePreset == CurvePreset.Cubed)
            {
                keyInt = newCurve.AddKey(0f, 0f);
                keyInt = newCurve.AddKey(0.5f, 0.125f);
                keyInt = newCurve.AddKey(1f, 1f);
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = 0f;
                curveKeys[0].outTangent = 0f;
                curveKeys[1].inTangent = 0.75f;
                curveKeys[1].outTangent = 0.75f;
                curveKeys[2].inTangent = 3f;
                curveKeys[2].outTangent = 3f;
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (curvePreset == CurvePreset.PowerOfFour)
            {
                keyInt = newCurve.AddKey(0f, 0f);
                keyInt = newCurve.AddKey(0.5f, 0.0625f);
                keyInt = newCurve.AddKey(1f, 1f);
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = 0f;
                curveKeys[0].outTangent = 0f;
                curveKeys[1].inTangent = 0.5f;
                curveKeys[1].outTangent = 0.5f;
                curveKeys[2].inTangent = 4f;
                curveKeys[2].outTangent = 4f;
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (curvePreset == CurvePreset.VerySmoothTerraced)
            {
                keyInt = newCurve.AddKey(0f, 0f);
                keyInt = newCurve.AddKey(0.3f, 0.25f);
                keyInt = newCurve.AddKey(0.6f, 0.75f);
                keyInt = newCurve.AddKey(1f, 1f);
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = 0f;
                curveKeys[0].outTangent = 0f;
                curveKeys[1].inTangent = 1f;
                curveKeys[1].outTangent = 1f;
                curveKeys[2].inTangent = 1f;
                curveKeys[2].outTangent = 1f;
                curveKeys[3].inTangent = 0f;
                curveKeys[3].outTangent = 0f;
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (curvePreset == CurvePreset.SmoothTerraced)
            {
                keyInt = newCurve.AddKey(0f, 0f);
                keyInt = newCurve.AddKey(0.4f, 0.2f);
                keyInt = newCurve.AddKey(0.6f, 0.8f);
                keyInt = newCurve.AddKey(1f, 1f);
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = 0f;
                curveKeys[0].outTangent = 0f;
                curveKeys[1].inTangent = 1f;
                curveKeys[1].outTangent = 1f;
                curveKeys[2].inTangent = 1f;
                curveKeys[2].outTangent = 1f;
                curveKeys[3].inTangent = 0f;
                curveKeys[3].outTangent = 0f;
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (curvePreset == CurvePreset.SteepTerraced)
            {
                keyInt = newCurve.AddKey(0f, 0f);
                keyInt = newCurve.AddKey(0.45f, 0.1f);
                keyInt = newCurve.AddKey(0.55f, 0.9f);
                keyInt = newCurve.AddKey(1f, 1f);
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = 0f;
                curveKeys[0].outTangent = 0f;
                curveKeys[1].inTangent = 1f / Mathf.Sqrt(3f);
                curveKeys[1].outTangent = 1f / Mathf.Sqrt(3f);
                curveKeys[2].inTangent = 1f / Mathf.Sqrt(3f);
                curveKeys[2].outTangent = 1f / Mathf.Sqrt(3f);
                curveKeys[3].inTangent = 0f;
                curveKeys[3].outTangent = 0f;
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (curvePreset == CurvePreset.VerySmoothDoubleTerraced)
            {
                keyInt = newCurve.AddKey(0f, 0f);
                keyInt = newCurve.AddKey(0.2f, 0.15f);
                keyInt = newCurve.AddKey(0.4f, 0.45f);
                keyInt = newCurve.AddKey(0.6f, 0.55f);
                keyInt = newCurve.AddKey(0.8f, 0.85f);
                keyInt = newCurve.AddKey(1f, 1f);
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = 0f;
                curveKeys[0].outTangent = 0f;
                curveKeys[1].inTangent = 1f;
                curveKeys[1].outTangent = 1f;
                curveKeys[2].inTangent = 1f;
                curveKeys[2].outTangent = 1f;
                curveKeys[3].inTangent = 1f;
                curveKeys[3].outTangent = 1f;
                curveKeys[4].inTangent = 1f;
                curveKeys[4].outTangent = 1f;
                curveKeys[5].inTangent = 0f;
                curveKeys[5].outTangent = 0f;
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (curvePreset == CurvePreset.SmoothDoubleTerraced)
            {
                keyInt = newCurve.AddKey(0f, 0f);
                keyInt = newCurve.AddKey(0.3f, 0.1f);
                keyInt = newCurve.AddKey(0.4f, 0.45f);
                keyInt = newCurve.AddKey(0.6f, 0.55f);
                keyInt = newCurve.AddKey(0.7f, 0.9f);
                keyInt = newCurve.AddKey(1f, 1f);
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = 0f;
                curveKeys[0].outTangent = 0f;
                curveKeys[1].inTangent = 1f;
                curveKeys[1].outTangent = 1f;
                curveKeys[2].inTangent = 1f;
                curveKeys[2].outTangent = 1f;
                curveKeys[3].inTangent = 1f;
                curveKeys[3].outTangent = 1f;
                curveKeys[4].inTangent = 1f;
                curveKeys[4].outTangent = 1f;
                curveKeys[5].inTangent = 0f;
                curveKeys[5].outTangent = 0f;
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (curvePreset == CurvePreset.SteepDoubleTerraced)
            {
                keyInt = newCurve.AddKey(0f, 0f);
                keyInt = newCurve.AddKey(0.3f, 0.1f);
                keyInt = newCurve.AddKey(0.35f, 0.45f);
                keyInt = newCurve.AddKey(0.65f, 0.55f);
                keyInt = newCurve.AddKey(0.7f, 0.9f);
                keyInt = newCurve.AddKey(1f, 1f);
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = 0f;
                curveKeys[0].outTangent = 0f;
                curveKeys[1].inTangent = 1f / Mathf.Sqrt(3f);
                curveKeys[1].outTangent = 1f / Mathf.Sqrt(3f);
                curveKeys[2].inTangent = 1f / Mathf.Sqrt(3f);
                curveKeys[2].outTangent = 1f / Mathf.Sqrt(3f);
                curveKeys[3].inTangent = 1f / Mathf.Sqrt(3f);
                curveKeys[3].outTangent = 1f / Mathf.Sqrt(3f);
                curveKeys[4].inTangent = 1f / Mathf.Sqrt(3f);
                curveKeys[4].outTangent = 1f / Mathf.Sqrt(3f);
                curveKeys[5].inTangent = 0f;
                curveKeys[5].outTangent = 0f;
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (curvePreset == CurvePreset.SharpDoubleTerraced)
            {
                keyInt = newCurve.AddKey(0f, 0f);
                keyInt = newCurve.AddKey(0.3f, 0f);
                keyInt = newCurve.AddKey(0.35f, 0.5f);
                keyInt = newCurve.AddKey(0.65f, 0.5f);
                keyInt = newCurve.AddKey(0.7f, 1f);
                keyInt = newCurve.AddKey(1f, 1f);
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = 0f;
                curveKeys[0].outTangent = 0f;
                curveKeys[1].inTangent = 0f;
                curveKeys[1].outTangent = 0f;
                curveKeys[2].inTangent = 0f;
                curveKeys[2].outTangent = 0f;
                curveKeys[3].inTangent = 0f;
                curveKeys[3].outTangent = 0f;
                curveKeys[4].inTangent = 0f;
                curveKeys[4].outTangent = 0f;
                curveKeys[5].inTangent = 0f;
                curveKeys[5].outTangent = 0f;
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (curvePreset == CurvePreset.FiveTieredTerraced)
            {
                keyInt = newCurve.AddKey(0f, 0f);
                for (int i = 1; i < 6; i++)
                {
                    keyInt = newCurve.AddKey((i * 0.2f) - 0.14f, (i * 0.2f) - 0.04f);
                    keyInt = newCurve.AddKey(i * 0.2f, i * 0.2f);
                }
                Keyframe[] curveKeys = newCurve.keys;
                for (int i = 0; i < 11; i++)
                {
                    curveKeys[i].inTangent = 1f;
                    curveKeys[i].outTangent = 1f;
                }
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (curvePreset == CurvePreset.TenTieredTerraced)
            {
                keyInt = newCurve.AddKey(0f, 0f);
                for (int i = 1; i < 11; i++)
                {
                    keyInt = newCurve.AddKey((i * 0.1f) - 0.07f, (i * 0.1f) - 0.02f);
                    keyInt = newCurve.AddKey(i * 0.1f, i * 0.1f);
                }
                Keyframe[] curveKeys = newCurve.keys;
                for (int i = 0; i < 21; i++)
                {
                    curveKeys[i].inTangent = 1f;
                    curveKeys[i].outTangent = 1f;
                }
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (curvePreset == CurvePreset.TwentyTieredTerraced)
            {
                keyInt = newCurve.AddKey(0f, 0f);
                for (int i = 1; i < 21; i++)
                {
                    keyInt = newCurve.AddKey((i * 0.05f) - 0.035f, (i * 0.05f) - 0.01f);
                    keyInt = newCurve.AddKey(i * 0.05f, i * 0.05f);
                }
                Keyframe[] curveKeys = newCurve.keys;
                for (int i = 0; i < 41; i++)
                {
                    curveKeys[i].inTangent = 1f;
                    curveKeys[i].outTangent = 1f;
                }
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (curvePreset == CurvePreset.CanyonTerracing1)
            {
                // 3 Terraces
                keyInt = newCurve.AddKey(0f, 0f);
                for (int i = 1; i < 6; i++)
                {
                    keyInt = newCurve.AddKey((i * 0.2f) - 0.165f, (i * 0.2f) - 0.04f);
                    keyInt = newCurve.AddKey(i * 0.2f, (i * 0.2f) + 0.02f);
                }
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = 1f;
                curveKeys[0].outTangent = 1f;
                for (int i = 1; i < 11; i += 2)
                {
                    curveKeys[i].inTangent = 0f;
                    curveKeys[i].outTangent = 0f;
                    curveKeys[i + 1].inTangent = 1f;
                    curveKeys[i + 1].outTangent = 1f;
                }
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (curvePreset == CurvePreset.CanyonTerracing2)
            {
                // 10 Terraces
                keyInt = newCurve.AddKey(0f, 0f);
                for (int i = 1; i < 11; i++)
                {
                    keyInt = newCurve.AddKey((i * 0.1f) - 0.085f, (i * 0.1f) - 0.02f);
                    keyInt = newCurve.AddKey(i * 0.1f, (i * 0.1f) + 0.01f);
                }
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = 1f;
                curveKeys[0].outTangent = 1f;
                for (int i = 1; i < 21; i += 2)
                {
                    curveKeys[i].inTangent = 0f;
                    curveKeys[i].outTangent = 0f;
                    curveKeys[i + 1].inTangent = 1f;
                    curveKeys[i + 1].outTangent = 1f;
                }
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (curvePreset == CurvePreset.CanyonTerracing3)
            {
                // 100 Terraces
                keyInt = newCurve.AddKey(0f, 0f);
                for (int i = 1; i < 101; i++)
                {
                    keyInt = newCurve.AddKey((i * 0.01f) - 0.007f, (i * 0.01f) - 0.002f);
                    keyInt = newCurve.AddKey(i * 0.01f, i * 0.01f);
                }
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = 1f;
                curveKeys[0].outTangent = 1f;
                for (int i = 1; i < 101; i += 2)
                {
                    curveKeys[i].inTangent = 0f;
                    curveKeys[i].outTangent = 0f;
                    curveKeys[i + 1].inTangent = 1f;
                    curveKeys[i + 1].outTangent = 1f;
                }
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (curvePreset == CurvePreset.IslandSmoothing1)
            {
                keyInt = newCurve.AddKey(0f, 0f);
                keyInt = newCurve.AddKey(0.325f, 0.45f);
                keyInt = newCurve.AddKey(0.675f, 0.55f);
                keyInt = newCurve.AddKey(1f, 1f);
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = 0f;
                curveKeys[0].outTangent = 0f;
                curveKeys[1].inTangent = 1f / Mathf.Sqrt(3f);
                curveKeys[1].outTangent = 1f / Mathf.Sqrt(3f);
                curveKeys[2].inTangent = 1f / Mathf.Sqrt(3f);
                curveKeys[2].outTangent = 1f / Mathf.Sqrt(3f);
                curveKeys[3].inTangent = 0f;
                curveKeys[3].outTangent = 0f;
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (curvePreset == CurvePreset.IslandSmoothing2)
            {
                keyInt = newCurve.AddKey(0f, 0f);
                keyInt = newCurve.AddKey(0.375f, 0.45f);
                keyInt = newCurve.AddKey(0.625f, 0.55f);
                keyInt = newCurve.AddKey(1f, 1f);
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = 0f;
                curveKeys[0].outTangent = 0f;
                curveKeys[1].inTangent = 1f / Mathf.Sqrt(3f);
                curveKeys[1].outTangent = 1f / Mathf.Sqrt(3f);
                curveKeys[2].inTangent = 1f / Mathf.Sqrt(3f);
                curveKeys[2].outTangent = 1f / Mathf.Sqrt(3f);
                curveKeys[3].inTangent = 0f;
                curveKeys[3].outTangent = 0f;
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (curvePreset == CurvePreset.IslandSmoothing3)
            {
                keyInt = newCurve.AddKey(0f, 0f);
                keyInt = newCurve.AddKey(0.45f, 0.475f);
                keyInt = newCurve.AddKey(0.55f, 0.525f);
                keyInt = newCurve.AddKey(1f, 1f);
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = 0f;
                curveKeys[0].outTangent = 0f;
                curveKeys[1].inTangent = 1f / Mathf.Sqrt(3f);
                curveKeys[1].outTangent = 1f / Mathf.Sqrt(3f);
                curveKeys[2].inTangent = 1f / Mathf.Sqrt(3f);
                curveKeys[2].outTangent = 1f / Mathf.Sqrt(3f);
                curveKeys[3].inTangent = 0f;
                curveKeys[3].outTangent = 0f;
                newCurve = new AnimationCurve(curveKeys);
            }

            // Stops warning "variable is assigned but its value is never used" from appearing in the compiler
            if (keyInt == 0) { }

            return newCurve;
        }

        /// <summary>
        /// Returns a curve given a preset for a curve
        /// </summary>
        public static AnimationCurve SetCurveFromPreset(LBCurve.FilterCurvePreset filterCurvePreset)
        {
            AnimationCurve newCurve = new AnimationCurve();
            if (filterCurvePreset == FilterCurvePreset.Default)
            {
                keyInt = newCurve.AddKey(0f, 0f);
                keyInt = newCurve.AddKey(0.5f, 1f);
                keyInt = newCurve.AddKey(1f, 0f);
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = 2f;
                curveKeys[0].outTangent = 2f;
                curveKeys[1].inTangent = 2f;
                curveKeys[1].outTangent = -2f;
                curveKeys[2].inTangent = -2f;
                curveKeys[2].outTangent = -2f;
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (filterCurvePreset == FilterCurvePreset.WideRange)
            {
                keyInt = newCurve.AddKey(0f, 0f);
                keyInt = newCurve.AddKey(0.25f, 1f);
                keyInt = newCurve.AddKey(0.75f, 1f);
                keyInt = newCurve.AddKey(1f, 0f);
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = 4f;
                curveKeys[0].outTangent = 4f;
                curveKeys[1].inTangent = 4f;
                curveKeys[1].outTangent = 0f;
                curveKeys[2].inTangent = 0f;
                curveKeys[2].outTangent = -4f;
                curveKeys[3].outTangent = -4f;
                curveKeys[3].outTangent = -4f;
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (filterCurvePreset == FilterCurvePreset.MaxIncreasing)
            {
                newCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            }
            else if (filterCurvePreset == FilterCurvePreset.MinIncreasing)
            {
                newCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            }

            return newCurve;
        }

        /// <summary>
        /// Returns a curve given a MapPathBlendCurvePreset for a curve
        /// </summary>
        /// <param name="mapPathBlendCurvePreset"></param>
        /// <returns></returns>
        public static AnimationCurve SetCurveFromPreset(LBCurve.MapPathBlendCurvePreset mapPathBlendCurvePreset)
        {
            AnimationCurve newCurve = new AnimationCurve();
            if (mapPathBlendCurvePreset == MapPathBlendCurvePreset.EaseInOut)
            {
                newCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            }
            else if (mapPathBlendCurvePreset == MapPathBlendCurvePreset.Linear)
            {
                newCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            }

            return newCurve;
        }

        /// <summary>
        /// Returns a curve given a MapPathHeightCurvePreset for a curve
        /// </summary>
        /// <param name="mapPathHeightCurvePreset"></param>
        /// <returns></returns>
        public static AnimationCurve SetCurveFromPreset(LBCurve.MapPathHeightCurvePreset mapPathHeightCurvePreset)
        {
            AnimationCurve newCurve = new AnimationCurve();
            if (mapPathHeightCurvePreset == MapPathHeightCurvePreset.Flat)
            {
                newCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
            }
            else if (mapPathHeightCurvePreset == MapPathHeightCurvePreset.River1Set)
            {
                keyInt = newCurve.AddKey(0.00f, 1.00f);
                keyInt = newCurve.AddKey(0.50f, 0.00f);
                keyInt = newCurve.AddKey(1.00f, 1.00f);
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = 0.00f;
                curveKeys[0].outTangent = 0.00f;
                curveKeys[1].inTangent = 0.00f;
                curveKeys[1].outTangent = 0.00f;
                curveKeys[2].inTangent = 0.00f;
                curveKeys[2].outTangent = 0.00f;
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (mapPathHeightCurvePreset == MapPathHeightCurvePreset.River2Set)
            {
                keyInt = newCurve.AddKey(0.00f, 1.00f);
                keyInt = newCurve.AddKey(0.25f, 1.00f);
                keyInt = newCurve.AddKey(0.50f, 0.00f);
                keyInt = newCurve.AddKey(0.75f, 1.00f);
                keyInt = newCurve.AddKey(1.00f, 1.00f);
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = 0.00f;
                curveKeys[0].outTangent = 0.00f;
                curveKeys[1].inTangent = 0.00f;
                curveKeys[1].outTangent = 0.00f;
                curveKeys[2].inTangent = 0.00f;
                curveKeys[2].outTangent = 0.00f;
                curveKeys[3].inTangent = 0.00f;
                curveKeys[3].outTangent = 0.00f;
                curveKeys[4].inTangent = 0.00f;
                curveKeys[4].outTangent = 0.00f;
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (mapPathHeightCurvePreset == MapPathHeightCurvePreset.River3Set)
            {
                keyInt = newCurve.AddKey(0.00f, 1.00f);
                keyInt = newCurve.AddKey(0.33f, 0.00f);
                keyInt = newCurve.AddKey(0.67f, 0.00f);
                keyInt = newCurve.AddKey(1.00f, 1.00f);
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = 0.00f;
                curveKeys[0].outTangent = 0.00f;
                curveKeys[1].inTangent = 0.00f;
                curveKeys[1].outTangent = 0.00f;
                curveKeys[2].inTangent = 0.00f;
                curveKeys[2].outTangent = 0.00f;
                curveKeys[3].inTangent = 0.00f;
                curveKeys[3].outTangent = 0.00f;
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (mapPathHeightCurvePreset == MapPathHeightCurvePreset.River4Set)
            {
                keyInt = newCurve.AddKey(0.00f, 1.00f);
                keyInt = newCurve.AddKey(0.20f, 1.00f);
                keyInt = newCurve.AddKey(0.40f, 0.00f);
                keyInt = newCurve.AddKey(0.60f, 0.00f);
                keyInt = newCurve.AddKey(0.80f, 1.00f);
                keyInt = newCurve.AddKey(1.00f, 1.00f);
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = 0.00f;
                curveKeys[0].outTangent = 0.00f;
                curveKeys[1].inTangent = 0.00f;
                curveKeys[1].outTangent = 0.00f;
                curveKeys[2].inTangent = 0.00f;
                curveKeys[2].outTangent = 0.00f;
                curveKeys[3].inTangent = 0.00f;
                curveKeys[3].outTangent = 0.00f;
                curveKeys[4].inTangent = 0.00f;
                curveKeys[4].outTangent = 0.00f;
                curveKeys[5].inTangent = 0.00f;
                curveKeys[5].outTangent = 0.00f;
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (mapPathHeightCurvePreset == MapPathHeightCurvePreset.River5Set)
            {
                keyInt = newCurve.AddKey(0.00f, 1.00f);
                keyInt = newCurve.AddKey(0.20f, 0.80f);
                keyInt = newCurve.AddKey(0.40f, 0.00f);
                keyInt = newCurve.AddKey(0.60f, 0.00f);
                keyInt = newCurve.AddKey(0.80f, 0.80f);
                keyInt = newCurve.AddKey(1.00f, 1.00f);
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = 0.00f;
                curveKeys[0].outTangent = 0.00f;
                curveKeys[1].inTangent = 0.00f;
                curveKeys[1].outTangent = 0.00f;
                curveKeys[2].inTangent = 0.00f;
                curveKeys[2].outTangent = 0.00f;
                curveKeys[3].inTangent = 0.00f;
                curveKeys[3].outTangent = 0.00f;
                curveKeys[4].inTangent = 0.00f;
                curveKeys[4].outTangent = 0.00f;
                curveKeys[5].inTangent = 0.00f;
                curveKeys[5].outTangent = 0.00f;
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (mapPathHeightCurvePreset == MapPathHeightCurvePreset.River6Set)
            {
                keyInt = newCurve.AddKey(0.00f, 1.00f);
                keyInt = newCurve.AddKey(0.33f, 0.00f);
                keyInt = newCurve.AddKey(0.40f, 0.93f);
                keyInt = newCurve.AddKey(0.60f, 0.95f);
                keyInt = newCurve.AddKey(0.80f, 0.99f);
                keyInt = newCurve.AddKey(1.00f, 1.00f);
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = 0.00f;
                curveKeys[0].outTangent = 0.00f;
                curveKeys[1].inTangent = 0.00f;
                curveKeys[1].outTangent = 0.00f;
                curveKeys[2].inTangent = 0.00f;
                curveKeys[2].outTangent = 0.00f;
                curveKeys[3].inTangent = 0.00f;
                curveKeys[3].outTangent = 0.00f;
                curveKeys[4].inTangent = 0.00f;
                curveKeys[4].outTangent = 0.00f;
                curveKeys[5].inTangent = 0.00f;
                curveKeys[5].outTangent = 0.00f;
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (mapPathHeightCurvePreset == MapPathHeightCurvePreset.River1Subtract)
            {
                keyInt = newCurve.AddKey(0.00f, 0.00f);
                keyInt = newCurve.AddKey(0.50f, 1.00f);
                keyInt = newCurve.AddKey(1.00f, 0.00f);
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = 0.00f;
                curveKeys[0].outTangent = 0.00f;
                curveKeys[1].inTangent = 0.00f;
                curveKeys[1].outTangent = 0.00f;
                curveKeys[2].inTangent = 0.00f;
                curveKeys[2].outTangent = 0.00f;
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (mapPathHeightCurvePreset == MapPathHeightCurvePreset.River2Subtract)
            {
                keyInt = newCurve.AddKey(0.00f, 0.00f);
                keyInt = newCurve.AddKey(0.25f, 0.00f);
                keyInt = newCurve.AddKey(0.50f, 1.00f);
                keyInt = newCurve.AddKey(0.75f, 0.00f);
                keyInt = newCurve.AddKey(1.00f, 0.00f);
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = 0.00f;
                curveKeys[0].outTangent = 0.00f;
                curveKeys[1].inTangent = 0.00f;
                curveKeys[1].outTangent = 0.00f;
                curveKeys[2].inTangent = 0.00f;
                curveKeys[2].outTangent = 0.00f;
                curveKeys[3].inTangent = 0.00f;
                curveKeys[3].outTangent = 0.00f;
                curveKeys[4].inTangent = 0.00f;
                curveKeys[4].outTangent = 0.00f;
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (mapPathHeightCurvePreset == MapPathHeightCurvePreset.River3Subtract)
            {
                keyInt = newCurve.AddKey(0.00f, 0.00f);
                keyInt = newCurve.AddKey(0.33f, 1.00f);
                keyInt = newCurve.AddKey(0.67f, 1.00f);
                keyInt = newCurve.AddKey(1.00f, 0.00f);
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = 0.00f;
                curveKeys[0].outTangent = 0.00f;
                curveKeys[1].inTangent = 0.00f;
                curveKeys[1].outTangent = 0.00f;
                curveKeys[2].inTangent = 0.00f;
                curveKeys[2].outTangent = 0.00f;
                curveKeys[3].inTangent = 0.00f;
                curveKeys[3].outTangent = 0.00f;
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (mapPathHeightCurvePreset == MapPathHeightCurvePreset.River4Subtract)
            {
                keyInt = newCurve.AddKey(0.00f, 0.00f);
                keyInt = newCurve.AddKey(0.20f, 0.00f);
                keyInt = newCurve.AddKey(0.40f, 1.00f);
                keyInt = newCurve.AddKey(0.60f, 1.00f);
                keyInt = newCurve.AddKey(0.80f, 0.00f);
                keyInt = newCurve.AddKey(1.00f, 0.00f);
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = 0.00f;
                curveKeys[0].outTangent = 0.00f;
                curveKeys[1].inTangent = 0.00f;
                curveKeys[1].outTangent = 0.00f;
                curveKeys[2].inTangent = 0.00f;
                curveKeys[2].outTangent = 0.00f;
                curveKeys[3].inTangent = 0.00f;
                curveKeys[3].outTangent = 0.00f;
                curveKeys[4].inTangent = 0.00f;
                curveKeys[4].outTangent = 0.00f;
                curveKeys[5].inTangent = 0.00f;
                curveKeys[5].outTangent = 0.00f;
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (mapPathHeightCurvePreset == MapPathHeightCurvePreset.River5Subtract)
            {
                keyInt = newCurve.AddKey(0.00f, 0.00f);
                keyInt = newCurve.AddKey(0.20f, 0.20f);
                keyInt = newCurve.AddKey(0.40f, 1.00f);
                keyInt = newCurve.AddKey(0.60f, 1.00f);
                keyInt = newCurve.AddKey(0.80f, 0.20f);
                keyInt = newCurve.AddKey(1.00f, 0.00f);
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = 0.00f;
                curveKeys[0].outTangent = 0.00f;
                curveKeys[1].inTangent = 0.00f;
                curveKeys[1].outTangent = 0.00f;
                curveKeys[2].inTangent = 0.00f;
                curveKeys[2].outTangent = 0.00f;
                curveKeys[3].inTangent = 0.00f;
                curveKeys[3].outTangent = 0.00f;
                curveKeys[4].inTangent = 0.00f;
                curveKeys[4].outTangent = 0.00f;
                curveKeys[5].inTangent = 0.00f;
                curveKeys[5].outTangent = 0.00f;
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (mapPathHeightCurvePreset == MapPathHeightCurvePreset.River6Subtract)
            {
                keyInt = newCurve.AddKey(0.00f, 0.00f);
                keyInt = newCurve.AddKey(0.33f, 1.00f);
                keyInt = newCurve.AddKey(0.40f, 0.07f);
                keyInt = newCurve.AddKey(0.60f, 0.05f);
                keyInt = newCurve.AddKey(0.80f, 0.01f);
                keyInt = newCurve.AddKey(1.00f, 0.00f);
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = 0.00f;
                curveKeys[0].outTangent = 0.00f;
                curveKeys[1].inTangent = 0.00f;
                curveKeys[1].outTangent = 0.00f;
                curveKeys[2].inTangent = 0.00f;
                curveKeys[2].outTangent = 0.00f;
                curveKeys[3].inTangent = 0.00f;
                curveKeys[3].outTangent = 0.00f;
                curveKeys[4].inTangent = 0.00f;
                curveKeys[4].outTangent = 0.00f;
                curveKeys[5].inTangent = 0.00f;
                curveKeys[5].outTangent = 0.00f;
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (mapPathHeightCurvePreset == MapPathHeightCurvePreset.SlopeLeftSet)
            {
                keyInt = newCurve.AddKey(0.00f, 0.00f);
                keyInt = newCurve.AddKey(0.20f, 0.33f);
                keyInt = newCurve.AddKey(0.40f, 1.00f);
                keyInt = newCurve.AddKey(0.60f, 0.67f);
                keyInt = newCurve.AddKey(0.80f, 0.33f);
                keyInt = newCurve.AddKey(1.00f, 0.00f);
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = 0.00f;
                curveKeys[0].outTangent = 0.00f;
                curveKeys[1].inTangent = 3.00f;
                curveKeys[1].outTangent = 3.00f;
                curveKeys[2].inTangent = -1.00f;
                curveKeys[2].outTangent = -1.00f;
                curveKeys[3].inTangent = -1.50f;
                curveKeys[3].outTangent = -1.50f;
                curveKeys[4].inTangent = -1.50f;
                curveKeys[4].outTangent = -1.50f;
                curveKeys[5].inTangent = -1.50f;
                curveKeys[5].outTangent = -1.50f;
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (mapPathHeightCurvePreset == MapPathHeightCurvePreset.SlopeRightSet)
            {
                keyInt = newCurve.AddKey(0.00f, 0.00f);
                keyInt = newCurve.AddKey(0.20f, 0.33f);
                keyInt = newCurve.AddKey(0.40f, 0.67f);
                keyInt = newCurve.AddKey(0.60f, 1.00f);
                keyInt = newCurve.AddKey(0.80f, 0.33f);
                keyInt = newCurve.AddKey(1.00f, 0.00f);
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = 1.50f;
                curveKeys[0].outTangent = 1.50f;
                curveKeys[1].inTangent = 1.50f;
                curveKeys[1].outTangent = 1.50f;
                curveKeys[2].inTangent = 1.50f;
                curveKeys[2].outTangent = 1.50f;
                curveKeys[3].inTangent = 1.00f;
                curveKeys[3].outTangent = 1.00f;
                curveKeys[4].inTangent = -3.00f;
                curveKeys[4].outTangent = -3.00f;
                curveKeys[5].inTangent = 0.00f;
                curveKeys[5].outTangent = 0.00f;
                newCurve = new AnimationCurve(curveKeys);
            }

            // Stops warning "variable is assigned but its value is never used" from appearing in the compiler
            if (keyInt == 0) { }

            return newCurve;
        }

        /// <summary>
        /// Returns a curve given an ObjPathHeightCurvePreset for a curve
        /// </summary>
        /// <param name="objPathHeightCurvePreset"></param>
        /// <returns></returns>
        public static AnimationCurve SetCurveFromPreset(LBCurve.ObjPathHeightCurvePreset objPathHeightCurvePreset)
        {
            AnimationCurve newCurve = new AnimationCurve();
            if (objPathHeightCurvePreset == ObjPathHeightCurvePreset.Flat)
            {
                newCurve = AnimationCurve.Linear(0f, 0.5f, 1f, 0.5f);
            }
            else if (objPathHeightCurvePreset == ObjPathHeightCurvePreset.River1)
            {
                keyInt = newCurve.AddKey(0.00f, 0.50f);
                keyInt = newCurve.AddKey(0.50f, 0.00f);
                keyInt = newCurve.AddKey(1.00f, 0.50f);
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = 0.00f;
                curveKeys[0].outTangent = 0.00f;
                curveKeys[1].inTangent = 0.00f;
                curveKeys[1].outTangent = 0.00f;
                curveKeys[2].inTangent = 0.00f;
                curveKeys[2].outTangent = 0.00f;
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (objPathHeightCurvePreset == ObjPathHeightCurvePreset.River2)
            {
                keyInt = newCurve.AddKey(0.00f, 0.5f);
                keyInt = newCurve.AddKey(0.25f, 0.5f);
                keyInt = newCurve.AddKey(0.50f, 0.00f);
                keyInt = newCurve.AddKey(0.75f, 0.5f);
                keyInt = newCurve.AddKey(1.00f, 0.5f);
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = 0.00f;
                curveKeys[0].outTangent = 0.00f;
                curveKeys[1].inTangent = 0.00f;
                curveKeys[1].outTangent = 0.00f;
                curveKeys[2].inTangent = 0.00f;
                curveKeys[2].outTangent = 0.00f;
                curveKeys[3].inTangent = 0.00f;
                curveKeys[3].outTangent = 0.00f;
                curveKeys[4].inTangent = 0.00f;
                curveKeys[4].outTangent = 0.00f;
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (objPathHeightCurvePreset == ObjPathHeightCurvePreset.River3)
            {
                keyInt = newCurve.AddKey(0.00f, 0.5f);
                keyInt = newCurve.AddKey(0.33f, 0.00f);
                keyInt = newCurve.AddKey(0.67f, 0.00f);
                keyInt = newCurve.AddKey(1.00f, 0.5f);
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = 0.00f;
                curveKeys[0].outTangent = 0.00f;
                curveKeys[1].inTangent = 0.00f;
                curveKeys[1].outTangent = 0.00f;
                curveKeys[2].inTangent = 0.00f;
                curveKeys[2].outTangent = 0.00f;
                curveKeys[3].inTangent = 0.00f;
                curveKeys[3].outTangent = 0.00f;
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (objPathHeightCurvePreset == ObjPathHeightCurvePreset.River4)
            {
                keyInt = newCurve.AddKey(0.00f, 0.5f);
                keyInt = newCurve.AddKey(0.20f, 0.5f);
                keyInt = newCurve.AddKey(0.40f, 0.00f);
                keyInt = newCurve.AddKey(0.60f, 0.00f);
                keyInt = newCurve.AddKey(0.80f, 0.5f);
                keyInt = newCurve.AddKey(1.00f, 0.5f);
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = 0.00f;
                curveKeys[0].outTangent = 0.00f;
                curveKeys[1].inTangent = 0.00f;
                curveKeys[1].outTangent = 0.00f;
                curveKeys[2].inTangent = 0.00f;
                curveKeys[2].outTangent = 0.00f;
                curveKeys[3].inTangent = 0.00f;
                curveKeys[3].outTangent = 0.00f;
                curveKeys[4].inTangent = 0.00f;
                curveKeys[4].outTangent = 0.00f;
                curveKeys[5].inTangent = 0.00f;
                curveKeys[5].outTangent = 0.00f;
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (objPathHeightCurvePreset == ObjPathHeightCurvePreset.River5)
            {
                keyInt = newCurve.AddKey(0.00f, 0.5f);
                keyInt = newCurve.AddKey(0.20f, 0.40f);
                keyInt = newCurve.AddKey(0.40f, 0.00f);
                keyInt = newCurve.AddKey(0.60f, 0.00f);
                keyInt = newCurve.AddKey(0.80f, 0.40f);
                keyInt = newCurve.AddKey(1.00f, 0.5f);
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = 0.00f;
                curveKeys[0].outTangent = 0.00f;
                curveKeys[1].inTangent = 0.00f;
                curveKeys[1].outTangent = 0.00f;
                curveKeys[2].inTangent = 0.00f;
                curveKeys[2].outTangent = 0.00f;
                curveKeys[3].inTangent = 0.00f;
                curveKeys[3].outTangent = 0.00f;
                curveKeys[4].inTangent = 0.00f;
                curveKeys[4].outTangent = 0.00f;
                curveKeys[5].inTangent = 0.00f;
                curveKeys[5].outTangent = 0.00f;
                newCurve = new AnimationCurve(curveKeys);
            }
            else
            {
                newCurve = AnimationCurve.Linear(0f, 0.5f, 1f, 0.5f);
            }

            return newCurve;
        }

        #endregion

        #region Compute Shader Methods

        /// <summary>
        /// Creates a package array of keyframes from an AnimationCurve.
        /// Used to pass to a compute shader StructuredBuffer.
        /// Vector4 format: x = time, y = value, z = inTangent, w = outTangent
        /// </summary>
        /// <param name="curve"></param>
        /// <returns></returns>
        public static Vector4[] GetComputeKeyFrames(AnimationCurve curve)
        {
            Vector4[] pkKeyFrames = null;

            if (curve != null)
            {
                Keyframe[] keysFrames = curve.keys;

                int numKeys = (keysFrames == null ? 0 : keysFrames.Length);

                // Create an array to hold the packed keyframes
                if (numKeys > 0) { pkKeyFrames = new Vector4[numKeys]; }

                for (int kfIdx = 0; kfIdx < numKeys; kfIdx++)
                {
                    pkKeyFrames[kfIdx].x = keysFrames[kfIdx].time;
                    pkKeyFrames[kfIdx].y = keysFrames[kfIdx].value;
                    pkKeyFrames[kfIdx].z = keysFrames[kfIdx].inTangent;
                    pkKeyFrames[kfIdx].w = keysFrames[kfIdx].outTangent;

                    #if LBCURVE_DEBUG_MODE && UNITY_EDITOR
                    Debug.Log("Curve keyframe: " + pkKeyFrames[kfIdx]);
                    #endif
                }

                #if LBCURVE_DEBUG_MODE && UNITY_EDITOR
                Debug.Log("Curve numKeys: " + numKeys + "\n");
                #endif
            }

            return pkKeyFrames;
        }


        #endregion
    }
}