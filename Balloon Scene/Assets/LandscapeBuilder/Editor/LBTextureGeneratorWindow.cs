using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.IO;

namespace LandscapeBuilder
{
    public class LBTextureGeneratorWindow : EditorWindow
    {
        #region Enumerations

        private enum GeneratorMode
        {
            TextureGenerator = 0,
            TextureCombiner = 1
        }

        private enum SourceType
        {
            Texture = 0,
            PerlinNoise = 1
        }

        private enum OutputType
        {
            Albedo = 0,
            AlbedoAndSmoothness = 1,
            //AlbedoAndTransparency = 2,
            MetallicAndSmoothness = 5,
            Specular = 6,
            NormalMap = 10,
            HeightMap = 11,
            Occlusion = 15,
            Emission = 16,
            Noise = 30
        }

        private enum AlbedoAlgorithm
        {
            ShadowRemoval = 0,
            ColourTintRemoval = 1,
            ShadowRemovalAndColourTintRemoval = 2
        }

        private enum MetallicAlgorithm
        {
            Specular = 0,
            MatchColour = 1
        }

        private enum SmoothnessAlgorithm
        {
            PixelVariance = 0
        }

        private enum HeightAlgorithm
        {
            ShadowDetection = 0,
            ColourRange = 1,
            ShadowDetectionAndLowColour = 2
        }

        private enum OcclusionAlgorithm
        {
            ShadowDetection = 0,
            PixelVariance = 1
        }

        private enum TextureCombineMode
        {
            Additive = 0,
            Minimum = 1,
            Maximum = 2,
            Channels = 2
        }

        private enum PreviewQuality
        {
            Low = 0,
            High = 1,
            Ultra = 2
        }

        private enum TextureResolution
        {
            _64x64 = 64,
            _128x128 = 128,
            _256x256 = 256,
            _512x512 = 512,
            _1024x1024 = 1024,
            _2048x2048 = 2048,
            _4096x4096 = 4096,
            _8192x8192 = 8192
        }

        #endregion

        #region Private Variables and Properties

        private GeneratorMode generatorMode = GeneratorMode.TextureGenerator;

        //private SourceType sourceType = SourceType.Texture;

        // Texture variables
        private Texture2D sourceTexture;
        private Texture2D sourceTextureSelection;
        private string generatedTextureName = "GeneratedTexture";
        private OutputType outputType = OutputType.NormalMap;
        [SerializeField] private OutputType outputTypeSelection;
        [SerializeField] private bool normaliseColourRange = false;
        [SerializeField] private bool invertColours = false;
        private bool updateTexturePreview = true;
        private PreviewQuality previewQuality = PreviewQuality.Low;
        private PreviewQuality previewQualitySelection = PreviewQuality.Low;
        private bool previewUpdateRequired = false;
        private Texture2D texturePreview;
        private Texture2D sourceTexturePreview; // = new Texture2D(2, 2);

        // Algorithm variables
        [SerializeField] private AlbedoAlgorithm albedoAlgorithm = AlbedoAlgorithm.ShadowRemoval;
        [SerializeField] private MetallicAlgorithm metallicAlgorithm = MetallicAlgorithm.Specular;
        [SerializeField] private SmoothnessAlgorithm smoothnessAlgorithm = SmoothnessAlgorithm.PixelVariance;
        [SerializeField] private HeightAlgorithm heightAlgorithm = HeightAlgorithm.ShadowDetection;
        [SerializeField] private OcclusionAlgorithm occlusionAlgorithm = OcclusionAlgorithm.ShadowDetection;

        // Shadow detection/removal algorithm variables
        [SerializeField] private Color lightestShadowColour = new Color(0.1f, 0.1f, 0.1f, 1f);
        [SerializeField] private Color shadowingLowColour = new Color(0.1f, 0.1f, 0.1f, 1f);
        [SerializeField] private float shadowingLowColourRange = 0.1f;

        // Colour tint removal variables
        [SerializeField] private Color tintColourToRemove = new Color(1f, 0f, 0f, 1f);

        // Specular algorithm variables
        [SerializeField] private Color minSpecularColour = Color.grey;
        [SerializeField] private Color maxSpecularColour = Color.white;

        // Pixel variance algorithm variables
        [SerializeField] private float smoothVariance = 0f;
        [SerializeField] private float steepVariance = 0.1f;

        // Colour match algorithm variables
        [SerializeField] private Color colourToMatch = new Color(0.8f, 0.8f, 0.8f, 1f);
        [SerializeField] private float colourRange = 0.1f;

        // Colour range algorithm variables
        [SerializeField] private Color lowColour = new Color(0.1f, 0.1f, 0.1f, 1f);
        [SerializeField] private Color midColour = new Color(0.25f, 0.25f, 0.25f, 1f);
        [SerializeField] private Color highColour = new Color(0.5f, 0.5f, 0.5f, 5f);

        // Noise variables
        [SerializeField] private TextureResolution outputTextureResolution = TextureResolution._512x512;
        [SerializeField] private int perlinNoiseOctaves = 8;
        [SerializeField] private bool showCurves = true;
        [SerializeField] private List<AnimationCurve> perlinNoiseOutputCurveModifiers = null;
        [SerializeField] private List<LBCurve.CurvePreset> perlinNoiseOutputCurveModifierPresets = null;
        [SerializeField] private List<AnimationCurve> perlinNoisePerOctaveCurveModifiers = null;
        [SerializeField] private List<LBCurve.CurvePreset> perlinNoisePerOctaveCurveModifierPresets = null;
        [SerializeField] private float perlinNoiseTileSize = 1f;
        [SerializeField] private bool isTileable = true;
        [SerializeField] private bool isDistToCentreMask = false;
        [SerializeField] private float outputStrength = 1f;
        [SerializeField] private float perlinNoiseLacunarity = 2f;
        [SerializeField] private float perlinNoiseGain = 0.5f;
        [SerializeField] private Gradient perlinNoiseGradient = LBGradient.SetGradientFromPreset(LBGradient.GradientPreset.DefaultPerlinGradient);

        // Other variables
        private bool calculateMetallicData = true;
        private float metallicParameter = 0f;
        private bool calculateSmoothnessData = true;
        private float smoothnessParameter = 0.5f;

        // Texture Combiner variables
        private List<Texture2D> sourceTexturesList;
        private int combinedTextureResolution = 1024;
        private TextureCombineMode textureCombineMode = TextureCombineMode.Maximum;
        private TextureCombineMode textureCombineModeSelection;
        private List<LBTextureOperations.InputChannelMode> inputChannelModeList;
        private int arrayInt;
        private int temp;
        private int index, index2;

        private GUIStyle buttonCompact;
        private GUIStyle labelFieldRichText;
        private Vector2 scrollPosition = Vector2.zero;

        #endregion

        // Add a menu item so that the editor window can be opened via the window menu tab
        [MenuItem("Window/Landscape Builder/Texture Generator")]
        public static void ShowWindow()
        {
            // Show existing window instance. If one doesn't exist, make one.
            EditorWindow.GetWindow(typeof(LBTextureGeneratorWindow), false, "LB Tex Gen");
        }

        void OnGUI()
        {
            EditorGUIUtility.labelWidth = 150f;

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            GUILayout.BeginVertical(EditorStyles.helpBox);

            // Setup styles
            buttonCompact = new GUIStyle("Button");
            buttonCompact.fontSize = 10;

            labelFieldRichText = new GUIStyle("Label");
            labelFieldRichText.richText = true;

            generatorMode = (GeneratorMode)EditorGUILayout.EnumPopup(new GUIContent("Generator Mode", "The mode used for creating the generated texture"), generatorMode);

            if (generatorMode == GeneratorMode.TextureGenerator)
            {
                EditorGUILayout.HelpBox("In Texture Generator mode, you can generate textures for your materials such as metallic, normal maps, height maps, occlusion, or emission from a source texture.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("In Texture Combiner mode, you can combine multiple textures together in different ways.", MessageType.Info);
            }

            generatedTextureName = EditorGUILayout.TextField(new GUIContent("Asset Name", "The file name that will be used for the generated texture"), generatedTextureName);

            EditorGUI.BeginChangeCheck();

            //sourceType = (SourceType)EditorGUILayout.EnumPopup(new GUIContent("Source Type", ""), sourceType);

            #region TextureGenerator
            if (generatorMode == GeneratorMode.TextureGenerator)
            {
                #region SourceTexture
                if (outputTypeSelection != OutputType.Noise)
                {
                    sourceTextureSelection = (Texture2D)EditorGUILayout.ObjectField(new GUIContent("Source Texture", "The texture used as an input"), sourceTextureSelection, typeof(Texture2D), false);
                    if (sourceTextureSelection != sourceTexture)
                    {
                        sourceTexture = sourceTextureSelection;
                        if (sourceTexture != null)
                        {
                            ChangeTextureName(sourceTexture.name);
                            LBTextureOperations.EnableReadable(sourceTexture, true);
                            if (sourceTexturePreview == null) { sourceTexturePreview = new Texture2D(2, 2); }
                            int texturePreviewResolution = 2;
                            if (previewQuality == PreviewQuality.Low) { texturePreviewResolution = 128; }
                            else if (previewQuality == PreviewQuality.High) { texturePreviewResolution = 256; }
                            else { texturePreviewResolution = 512; }
                            sourceTexturePreview = LBTextureOperations.GenerateDownscaledTexture(sourceTexture, texturePreviewResolution);
                        }
                    }
                }
                #endregion

                #region OutputType
                outputTypeSelection = (OutputType)EditorGUILayout.EnumPopup(new GUIContent("Output Type", "The type of texture that will be generated as an output"), outputTypeSelection);
                if (outputTypeSelection != outputType)
                {
                    outputType = outputTypeSelection;
                    if (sourceTexture != null && outputType != OutputType.Noise) { ChangeTextureName(sourceTexture.name); }

                    // Switched to Noise - set default tilesize
                    if (outputType == OutputType.Noise)
                    {
                        perlinNoiseTileSize = 1f;
                        if (sourceTexturePreview == null) { sourceTexturePreview = new Texture2D(2, 2); }
                        generatedTextureName = "NoiseTexture";
                    }
                }
                #endregion

                #region Albedo
                if (outputType == OutputType.Albedo)
                {
                    EditorGUILayout.HelpBox("In Albedo mode, you can remove shadows or a colour tint from your texture.", MessageType.Info);
                    albedoAlgorithm = (AlbedoAlgorithm)EditorGUILayout.EnumPopup(new GUIContent("Albedo Algorithm", "The algorithm used for modifying the albedo texture"), albedoAlgorithm);
                    if (albedoAlgorithm == AlbedoAlgorithm.ShadowRemoval)
                    {
                        // Shadow removal - allows for approximate shadow removal from textures
                        lightestShadowColour = EditorGUILayout.ColorField(new GUIContent("Lightest Shadow Colour", "The colour of the lightest shadows"), lightestShadowColour);
                    }
                    else if (albedoAlgorithm == AlbedoAlgorithm.ColourTintRemoval)
                    {
                        // Colour tint removal - creates the texture such that the given tint colour (as would be used in the shader)
                        // will give back the original texture
                        tintColourToRemove = EditorGUILayout.ColorField(new GUIContent("Tint Colour", "The tint colour to remove from the texture"), tintColourToRemove);
                    }
                    else
                    {
                        // Shadow removal - allows for approximate shadow removal from textures
                        lightestShadowColour = EditorGUILayout.ColorField(new GUIContent("Lightest Shadow Colour", "The colour of the lightest shadows"), lightestShadowColour);
                        // Colour tint removal - creates the texture such that the given tint colour (as would be used in the shader)
                        // will give back the original texture
                        tintColourToRemove = EditorGUILayout.ColorField(new GUIContent("Tint Colour", "The tint colour to remove from the texture"), tintColourToRemove);
                    }
                }
                #endregion

                #region AlbedoAndSmoothness
                else if (outputType == OutputType.AlbedoAndSmoothness)
                {
                    EditorGUILayout.HelpBox("In Albedo And Smoothness mode, you can remove shadows or a colour tint from your texture, while encoding smoothness data into the alpha channel of your texture.", MessageType.Info);
                    albedoAlgorithm = (AlbedoAlgorithm)EditorGUILayout.EnumPopup(new GUIContent("Albedo Algorithm", "The algorithm used for modifying the albedo texture"), albedoAlgorithm);
                    if (albedoAlgorithm == AlbedoAlgorithm.ShadowRemoval)
                    {
                        // Shadow removal - allows for approximate shadow removal from textures
                        lightestShadowColour = EditorGUILayout.ColorField(new GUIContent("Lightest Shadow Colour", "The colour of the lightest shadows - making this colour lighter this will remove more shadows but may lighten the overall texture"), lightestShadowColour);
                    }
                    else if (albedoAlgorithm == AlbedoAlgorithm.ColourTintRemoval)
                    {
                        // Colour tint removal - creates the texture such that the given tint colour (as would be used in the shader)
                        // will give back the original texture
                        tintColourToRemove = EditorGUILayout.ColorField(new GUIContent("Tint Colour", "The tint colour to remove from the texture"), tintColourToRemove);
                    }
                    else
                    {
                        // Shadow removal - allows for approximate shadow removal from textures
                        lightestShadowColour = EditorGUILayout.ColorField(new GUIContent("Lightest Shadow Colour", "The colour of the lightest shadows"), lightestShadowColour);
                        // Colour tint removal - creates the texture such that the given tint colour (as would be used in the shader)
                        // will give back the original texture
                        tintColourToRemove = EditorGUILayout.ColorField(new GUIContent("Tint Colour", "The tint colour to remove from the texture"), tintColourToRemove);
                    }

                    calculateSmoothnessData = EditorGUILayout.Toggle(new GUIContent("Calculate Smoothness Data", "Whether the smoothness data encoded into the texture is calculated"), calculateSmoothnessData);
                    if (calculateSmoothnessData)
                    {
                        // Smoothness - encoded in alpha channel
                        smoothnessAlgorithm = (SmoothnessAlgorithm)EditorGUILayout.EnumPopup(new GUIContent("Smoothness Algorithm", "The algorithm used for calculating encoded smoothness data"), smoothnessAlgorithm);
                        if (smoothnessAlgorithm == SmoothnessAlgorithm.PixelVariance)
                        {
                            // Calculates smoothness from the pixel variance in a texture
                            smoothVariance = EditorGUILayout.Slider(new GUIContent("Smooth Variance", "The variance between pixels classified as 'smooth', i.e. an encoded smoothness value of 1.0"), smoothVariance, 0f, 0.25f);
                            steepVariance = EditorGUILayout.Slider(new GUIContent("Steep Variance", "The variance between pixels classified as 'steep', i.e. an encoded smoothness value of 0.0"), steepVariance, 0f, 0.25f);
                        }
                    }
                    else
                    {
                        smoothnessParameter = EditorGUILayout.Slider(new GUIContent("Smoothness Parameter", "The smoothness parameter that will be encoded into the texture"), smoothnessParameter, 0f, 1f);
                    }
                }
                #endregion

                #region AlbedoAndTransparency - NOT USED
                // else if (outputType == OutputType.AlbedoAndTransparency)
                // {
                // 	EditorGUILayout.HelpBox("In Albedo And Transparency mode, you can remove shadows or a colour tint from your texture, while encoding transparency data into the alpha channel (STILL WIP).", MessageType.Info);
                // 	albedoAlgorithm = (AlbedoAlgorithm)EditorGUILayout.EnumPopup(new GUIContent("Albedo Algorithm", "The algorithm used for modifying the albedo texture"), albedoAlgorithm);
                // 	if (albedoAlgorithm == AlbedoAlgorithm.ShadowRemoval)
                // 	{
                // 		// Shadow removal - allows for approximate shadow removal from textures
                // 		lightestShadowColour = EditorGUILayout.ColorField(new GUIContent("Lightest Shadow Colour", "The colour of the lightest shadows - making this colour lighter this will remove more shadows but may lighten the overall texture"), lightestShadowColour);
                // 	}
                // 	else
                // 	{
                // 		// Colour tint removal - creates the texture such that the given tint colour (as would be used in the shader)
                // 		// will give back the original texture
                // 		tintColourToRemove = EditorGUILayout.ColorField(new GUIContent("Tint Colour", "The tint colour to remove from the texture"), tintColourToRemove);
                // 	}
                // }
                #endregion

                #region MetallicAndSmoothness
                else if (outputType == OutputType.MetallicAndSmoothness)
                {
                    EditorGUILayout.HelpBox("In Metallic And Smoothness mode, you can create a texture to be used in materials with the standard shader that provides extra metallic and smoothness data. Metallic data is encoded into the RGB channels while smoothness data is encoded into the alpha channel.", MessageType.Info);
                    calculateMetallicData = EditorGUILayout.Toggle(new GUIContent("Calculate Metallic Data", "Whether the metallic data encoded into the texture is calculated"), calculateMetallicData);
                    if (calculateMetallicData)
                    {
                        metallicAlgorithm = (MetallicAlgorithm)EditorGUILayout.EnumPopup(new GUIContent("Metallic Algorithm", "The algorithm used for calculating encoded metallic data"), metallicAlgorithm);
                        if (metallicAlgorithm == MetallicAlgorithm.Specular)
                        {
                            // Specular algorithm - calculates metallic from a specular range in the input texture
                            minSpecularColour = EditorGUILayout.ColorField(new GUIContent("Min Metallic Colour", "The colour closest to the 'max metallic colour' that is designated as having a 0.0 metallic value"), minSpecularColour);
                            maxSpecularColour = EditorGUILayout.ColorField(new GUIContent("Max Metallic Colour", "The colour designated as having the highest metallic value in a texture"), maxSpecularColour);
                        }
                        else
                        {
                            // Colour match algorithm
                            colourToMatch = EditorGUILayout.ColorField(new GUIContent("Metallic Colour", "The colour of the texture that most corresponds with a high metallic value"), colourToMatch);
                            colourRange = EditorGUILayout.Slider(new GUIContent("Colour Range", "The size of the metallic colour range - increasing this value will increase the amount of colours in the texture that are classified as metallic"), colourRange, 0.01f, 0.5f);
                        }
                    }
                    else
                    {
                        metallicParameter = EditorGUILayout.Slider(new GUIContent("Metallic Parameter", "The metallic parameter that will be encoded into the texture"), metallicParameter, 0f, 1f);
                    }

                    calculateSmoothnessData = EditorGUILayout.Toggle(new GUIContent("Calculate Smoothness Data", "Whether the smoothness data encoded into the texture is calculated"), calculateSmoothnessData);
                    if (calculateSmoothnessData)
                    {
                        // Smoothness - encoded in alpha channel
                        smoothnessAlgorithm = (SmoothnessAlgorithm)EditorGUILayout.EnumPopup(new GUIContent("Smoothness Algorithm", "The algorithm used for calculating encoded smoothness data"), smoothnessAlgorithm);
                        if (smoothnessAlgorithm == SmoothnessAlgorithm.PixelVariance)
                        {
                            // Calculates smoothness from the pixel variance in a texture
                            smoothVariance = EditorGUILayout.Slider(new GUIContent("Smooth Variance", "The variance between pixels classified as 'smooth', i.e. an encoded smoothness value of 1.0"), smoothVariance, 0f, 0.25f);
                            steepVariance = EditorGUILayout.Slider(new GUIContent("Steep Variance", "The variance between pixels classified as 'steep', i.e. an encoded smoothness value of 0.0"), steepVariance, 0f, 0.25f);
                        }
                    }
                    else
                    {
                        smoothnessParameter = EditorGUILayout.Slider(new GUIContent("Smoothness Parameter", "The smoothness parameter that will be encoded into the texture"), smoothnessParameter, 0f, 1f);
                    }
                }
                #endregion

                #region Specular
                else if (outputType == OutputType.Specular)
                {
                    EditorGUILayout.HelpBox("In Specular mode, you can create a texture that provides extra specular data to your shaders. Specular data is encoded into the RGB channels.", MessageType.Info);
                    // Specular - calculates specular from a specular range in the input texture
                    minSpecularColour = EditorGUILayout.ColorField(new GUIContent("Min Specular Colour", "The colour closest to the 'max specular colour' that is designated as having a 0.0 specular value"), minSpecularColour);
                    maxSpecularColour = EditorGUILayout.ColorField(new GUIContent("Max Specular Colour", "The colour designated as having the highest specular value in a texture"), maxSpecularColour);
                }
                #endregion

                #region NormalMap
                else if (outputType == OutputType.NormalMap)
                {
                    EditorGUILayout.HelpBox("In Normal Map mode, you can create a texture to be used as a normal map for its input.", MessageType.Info);
                    // Normal map - calculates a grayscale height using approximate shadow detection before converting it to a normal map
                    heightAlgorithm = (HeightAlgorithm)EditorGUILayout.EnumPopup(new GUIContent("Height Algorithm", "The algorithm used for calculating encoded height data"), heightAlgorithm);
                    if (heightAlgorithm == HeightAlgorithm.ShadowDetection)
                    {
                        // Shadow detection algorithm - same inputs as shadow removal, but lightest shadow colour must be grayscale
                        lightestShadowColour = EditorGUILayout.ColorField(new GUIContent("Lightest Shadow Colour", "The grayscale colour of the lightest shadows - making this colour lighter this will remove more shadows but may lighten the overall texture"), lightestShadowColour);
                        lightestShadowColour = lightestShadowColour.grayscale * Color.white;
                    }
                    else if (heightAlgorithm == HeightAlgorithm.ColourRange)
                    {
                        // Colour range algorithm
                        lowColour = EditorGUILayout.ColorField(new GUIContent("Low Colour", "The colour designated as being the 'lowest' in a texture"), lowColour);
                        midColour = EditorGUILayout.ColorField(new GUIContent("Mid Colour", "The colour designated as being the 'height midpoint' in a texture"), midColour);
                        highColour = EditorGUILayout.ColorField(new GUIContent("High Colour", "The colour designated as being the 'highest' in a texture"), highColour);
                    }
                    else
                    {
                        // Shadow detection and low colour algorithm
                        lightestShadowColour = EditorGUILayout.ColorField(new GUIContent("Lightest Shadow Colour", "The grayscale colour of the lightest shadows - making this colour lighter this will remove more shadows but may lighten the overall texture"), lightestShadowColour);
                        lightestShadowColour = lightestShadowColour.grayscale * Color.white;
                        shadowingLowColour = EditorGUILayout.ColorField(new GUIContent("Low Colour", "The colour designated as being the 'lowest' in a texture"), shadowingLowColour);
                        shadowingLowColourRange = EditorGUILayout.Slider(new GUIContent("Low Colour Range", "The size of the 'low' colour range - increasing this will increase the number of of colours in a texture designated as 'low'"), shadowingLowColourRange, 0.01f, 0.5f);
                    }
                }
                #endregion

                #region HeightMap
                else if (outputType == OutputType.HeightMap)
                {
                    EditorGUILayout.HelpBox("In Height Map mode, you can create a texture to be used as a parallax map for its input.", MessageType.Info);
                    heightAlgorithm = (HeightAlgorithm)EditorGUILayout.EnumPopup(new GUIContent("Height Algorithm", "The algorithm used for calculating encoded metallic data"), heightAlgorithm);
                    if (heightAlgorithm == HeightAlgorithm.ShadowDetection)
                    {
                        // Shadow detection algorithm - same inputs as shadow removal, but lightest shadow colour must be grayscale
                        lightestShadowColour = EditorGUILayout.ColorField(new GUIContent("Lightest Shadow Colour", "The grayscale colour of the lightest shadows - making this colour lighter this will remove more shadows but may lighten the overall texture"), lightestShadowColour);
                        lightestShadowColour = lightestShadowColour.grayscale * Color.white;
                    }
                    else if (heightAlgorithm == HeightAlgorithm.ColourRange)
                    {
                        // Colour range algorithm
                        lowColour = EditorGUILayout.ColorField(new GUIContent("Low Colour", "The colour designated as being the 'lowest' in a texture"), lowColour);
                        midColour = EditorGUILayout.ColorField(new GUIContent("Mid Colour", "The colour designated as being the 'height midpoint' in a texture"), midColour);
                        highColour = EditorGUILayout.ColorField(new GUIContent("High Colour", "The colour designated as being the 'highest' in a texture"), highColour);
                    }
                    else
                    {
                        // Shadow detection and low colour algorithm
                        lightestShadowColour = EditorGUILayout.ColorField(new GUIContent("Lightest Shadow Colour", "The grayscale colour of the lightest shadows - making this colour lighter this will remove more shadows but may lighten the overall texture"), lightestShadowColour);
                        lightestShadowColour = lightestShadowColour.grayscale * Color.white;
                        shadowingLowColour = EditorGUILayout.ColorField(new GUIContent("Low Colour", "The colour designated as being the 'lowest' in a texture"), shadowingLowColour);
                        shadowingLowColourRange = EditorGUILayout.Slider(new GUIContent("Low Colour Range", "The size of the 'low' colour range - increasing this will increase the number of colours in a texture designated as 'low'"), shadowingLowColourRange, 0.01f, 0.5f);
                    }
                }
                #endregion

                #region Occlusion
                else if (outputType == OutputType.Occlusion)
                {
                    EditorGUILayout.HelpBox("In Occlusion mode, you can create a texture that provides occlusion data to your shaders. Occlusion data is encoded into the RGB channels.", MessageType.Info);
                    occlusionAlgorithm = (OcclusionAlgorithm)EditorGUILayout.EnumPopup(new GUIContent("Occlusion Algorithm", "The algorithm used for calculating encoded occlusion data"), occlusionAlgorithm);
                    if (occlusionAlgorithm == OcclusionAlgorithm.ShadowDetection)
                    {
                        // Shadow detection algorithm - same inputs as shadow removal, but lightest shadow colour must be grayscale
                        lightestShadowColour = EditorGUILayout.ColorField(new GUIContent("Lightest Shadow Colour", "The grayscale colour of the lightest shadows - making this colour lighter this will remove more shadows but may lighten the overall texture"), lightestShadowColour);
                        lightestShadowColour = lightestShadowColour.grayscale * Color.white;
                    }
                    else
                    {
                        // Pixel variance algorithm
                        smoothVariance = EditorGUILayout.Slider(new GUIContent("Smooth Variance", "The variance between pixels classified as 'smooth', i.e. an encoded smoothness value of 1.0"), smoothVariance, 0f, 0.25f);
                        steepVariance = EditorGUILayout.Slider(new GUIContent("Steep Variance", "The variance between pixels classified as 'steep', i.e. an encoded smoothness value of 0.0"), steepVariance, 0f, 0.25f);
                    }
                }
                #endregion

                #region Emission
                else if (outputType == OutputType.Emission)
                {
                    EditorGUILayout.HelpBox("In Emission mode, you can create a texture that provides emissive data to your shaders. Emissive data is encoded into the RGB channels.", MessageType.Info);
                    // Emission - calculates areas of emission from a specular range in the input texture
                    minSpecularColour = EditorGUILayout.ColorField(new GUIContent("Min Specular Colour", "The colour closest to the 'max specular colour' that is designated as having a 0.0 specular value"), minSpecularColour);
                    maxSpecularColour = EditorGUILayout.ColorField(new GUIContent("Max Specular Colour", "The colour designated as having the highest specular value in a texture"), maxSpecularColour);
                }
                #endregion

                #region Noise
                else if (outputType == OutputType.Noise)
                {
                    EditorGUILayout.HelpBox("In Noise mode, you can create a texture using a noise algorithm", MessageType.Info);
                    outputTextureResolution = (TextureResolution)EditorGUILayout.EnumPopup(new GUIContent("Texture Resolution", "The dimensions of the output texture."), outputTextureResolution);
                    perlinNoiseOctaves = EditorGUILayout.IntSlider(new GUIContent("Noise Octaves", "Number of Perlin noise octaves"), perlinNoiseOctaves, 1, 15);
                    perlinNoiseLacunarity = EditorGUILayout.Slider(new GUIContent("Noise Lacunarity", "Factor each octave of the noise is scaled down by on the x-z plane"), perlinNoiseLacunarity, 1.5f, 3f);
                    perlinNoiseGain = EditorGUILayout.Slider(new GUIContent("Noise Gain", "Factor each octave of the noise is scaled by on the y-axis"), perlinNoiseGain, 0.25f, 0.75f);

                    // Show/hide foldout for curves
                    showCurves = EditorGUILayout.Foldout(showCurves, "Curve Modifiers");


                    if (perlinNoiseOutputCurveModifiers == null) { perlinNoiseOutputCurveModifiers = new List<AnimationCurve>(); }
                    if (perlinNoiseOutputCurveModifierPresets == null) { perlinNoiseOutputCurveModifierPresets = new List<LBCurve.CurvePreset>(); }

                    if (perlinNoiseOutputCurveModifiers != null)
                    {
                        if (perlinNoiseOutputCurveModifiers.Count == 0) { perlinNoiseOutputCurveModifiers.Add(AnimationCurve.Linear(0f, 0f, 1f, 1f)); }
                        if (perlinNoiseOutputCurveModifierPresets.Count == 0) { perlinNoiseOutputCurveModifierPresets.Add(LBCurve.CurvePreset.None); }

                        if (showCurves)
                        {
                            arrayInt = perlinNoiseOutputCurveModifiers.Count;
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField(new GUIContent("<b>Output Curve Modifiers</b>", "Curve Modifiers that modify the overall output of this texture"), labelFieldRichText);
                            if (GUILayout.Button("+")) { arrayInt++; }
                            if (GUILayout.Button("-")) { arrayInt--; }
                            if (arrayInt < 0) { arrayInt = 0; }
                            GUILayout.EndHorizontal();

                            // Add items to the list
                            if (arrayInt > perlinNoiseOutputCurveModifiers.Count)
                            {
                                temp = arrayInt - perlinNoiseOutputCurveModifiers.Count;
                                for (index2 = 0; index2 < temp; index2++)
                                {
                                    perlinNoiseOutputCurveModifiers.Add(AnimationCurve.Linear(0f, 0f, 1f, 1f));
                                    perlinNoiseOutputCurveModifierPresets.Add(LBCurve.CurvePreset.None);
                                }
                            }
                            // Remove items from the list
                            else if (arrayInt < perlinNoiseOutputCurveModifiers.Count)
                            {
                                temp = perlinNoiseOutputCurveModifiers.Count - arrayInt;
                                for (index2 = 0; index2 < temp; index2++)
                                {
                                    perlinNoiseOutputCurveModifiers.RemoveAt(perlinNoiseOutputCurveModifiers.Count - 1);
                                    perlinNoiseOutputCurveModifierPresets.RemoveAt(perlinNoiseOutputCurveModifierPresets.Count - 1);
                                }
                            }

                            // Show the elements of the output curve modifier list
                            for (int curveModifierIndex = 0; curveModifierIndex < perlinNoiseOutputCurveModifiers.Count; curveModifierIndex++)
                            {
                                LBCurve.CurvePreset newCurvePreset;
                                newCurvePreset = (LBCurve.CurvePreset)EditorGUILayout.EnumPopup("Preset", perlinNoiseOutputCurveModifierPresets[curveModifierIndex]);
                                if (newCurvePreset != perlinNoiseOutputCurveModifierPresets[curveModifierIndex])
                                {
                                    perlinNoiseOutputCurveModifiers[curveModifierIndex] = LBCurve.SetCurveFromPreset(newCurvePreset);
                                    perlinNoiseOutputCurveModifierPresets[curveModifierIndex] = newCurvePreset;
                                }
                                EditorGUILayout.BeginHorizontal();
                                GUILayout.Label(new GUIContent("Curve Modifier " + (curveModifierIndex + 1), "Used to modify the output of the noise"), GUILayout.Width(EditorGUIUtility.labelWidth - 3f));
                                perlinNoiseOutputCurveModifiers[curveModifierIndex] = EditorGUILayout.CurveField(perlinNoiseOutputCurveModifiers[curveModifierIndex], GUILayout.Height(30f));
                                EditorGUILayout.EndHorizontal();
                            }
                        }
                    }

                    if (perlinNoisePerOctaveCurveModifiers == null) { perlinNoisePerOctaveCurveModifiers = new List<AnimationCurve>(); }
                    if (perlinNoisePerOctaveCurveModifierPresets == null) { perlinNoisePerOctaveCurveModifierPresets = new List<LBCurve.CurvePreset>(); }

                    if (perlinNoisePerOctaveCurveModifiers != null)
                    {
                        if (perlinNoisePerOctaveCurveModifiers.Count == 0) { perlinNoisePerOctaveCurveModifiers.Add(AnimationCurve.Linear(0f, 0f, 1f, 1f)); }
                        if (perlinNoisePerOctaveCurveModifierPresets.Count == 0) { perlinNoisePerOctaveCurveModifierPresets.Add(LBCurve.CurvePreset.None); }

                        if (showCurves)
                        {
                            arrayInt = perlinNoisePerOctaveCurveModifiers.Count;
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField(new GUIContent("<b>Per Octave Curve Modifiers</b>", "Curve Modifiers that modify the output of each octave of this texture"), labelFieldRichText);
                            if (GUILayout.Button("+")) { arrayInt++; }
                            if (GUILayout.Button("-")) { arrayInt--; }
                            if (arrayInt < 0) { arrayInt = 0; }
                            GUILayout.EndHorizontal();

                            // Add items to the list
                            if (arrayInt > perlinNoisePerOctaveCurveModifiers.Count)
                            {
                                temp = arrayInt - perlinNoisePerOctaveCurveModifiers.Count;
                                for (index2 = 0; index2 < temp; index2++)
                                {
                                    perlinNoisePerOctaveCurveModifiers.Add(AnimationCurve.Linear(0f, 0f, 1f, 1f));
                                    perlinNoisePerOctaveCurveModifierPresets.Add(LBCurve.CurvePreset.None);
                                }
                            }
                            // Remove items from the list
                            else if (arrayInt < perlinNoisePerOctaveCurveModifiers.Count)
                            {
                                temp = perlinNoisePerOctaveCurveModifiers.Count - arrayInt;
                                for (index2 = 0; index2 < temp; index2++)
                                {
                                    perlinNoisePerOctaveCurveModifiers.RemoveAt(perlinNoisePerOctaveCurveModifiers.Count - 1);
                                    perlinNoisePerOctaveCurveModifierPresets.RemoveAt(perlinNoisePerOctaveCurveModifierPresets.Count - 1);
                                }
                            }

                            // Show the elements of the output curve modifier list
                            for (int curveModifierIndex = 0; curveModifierIndex < perlinNoisePerOctaveCurveModifiers.Count; curveModifierIndex++)
                            {
                                LBCurve.CurvePreset newCurvePreset;
                                newCurvePreset = (LBCurve.CurvePreset)EditorGUILayout.EnumPopup("Preset", perlinNoisePerOctaveCurveModifierPresets[curveModifierIndex]);
                                if (newCurvePreset != perlinNoisePerOctaveCurveModifierPresets[curveModifierIndex])
                                {
                                    perlinNoisePerOctaveCurveModifiers[curveModifierIndex] = LBCurve.SetCurveFromPreset(newCurvePreset);
                                    perlinNoisePerOctaveCurveModifierPresets[curveModifierIndex] = newCurvePreset;
                                }
                                EditorGUILayout.BeginHorizontal();
                                GUILayout.Label(new GUIContent("Curve Modifier " + (curveModifierIndex + 1), "Used to modify the output of each individual octave of the noise"), GUILayout.Width(EditorGUIUtility.labelWidth - 3f));
                                perlinNoisePerOctaveCurveModifiers[curveModifierIndex] = EditorGUILayout.CurveField(perlinNoisePerOctaveCurveModifiers[curveModifierIndex], GUILayout.Height(30f));
                                EditorGUILayout.EndHorizontal();
                            }
                        }
                    }

                    isTileable = EditorGUILayout.Toggle(new GUIContent("Is Tileable", "Whether the noise generated is tileable."), isTileable);

                    // if (isTileable)
                    // {
                    //     perlinNoiseTileSize = EditorGUILayout.FloatField(new GUIContent("Noise Tile Size", "Scaling of the noise on the x-z plane"), perlinNoiseTileSize);
                    //     if (perlinNoiseTileSize == 0f) { perlinNoiseTileSize = (float)outputTextureResolution; }
                    // }

                    perlinNoiseTileSize = EditorGUILayout.FloatField(new GUIContent("Noise Tile Size", "Scaling of the noise on the x-z plane"), perlinNoiseTileSize);
                    if (perlinNoiseTileSize == 0f) { perlinNoiseTileSize = 1f; }

                    isDistToCentreMask = EditorGUILayout.Toggle(new GUIContent("Distance to Centre Mask", "Whether to apply a distance to centre mask."), isDistToCentreMask);
                    if (!normaliseColourRange)
                    {
                        outputStrength = EditorGUILayout.Slider(new GUIContent("Output Strength", "The relative strength of each pixel in the output texture."), outputStrength, 0f, 5.0f);
                    }

                    // EditorGUILayout doesn't have an exposed editorguilayout field editor so we have to treat gradients as properties
                    EditorGUI.BeginChangeCheck();
                    SerializedObject serializedObj = new SerializedObject(this);
                    SerializedProperty serializedProp = serializedObj.FindProperty("perlinNoiseGradient");
                    EditorGUILayout.PropertyField(serializedProp, new GUIContent("Colour Gradient", "Gradient mapping noise values to colours"), true, null);
                    if (EditorGUI.EndChangeCheck()) { serializedObj.ApplyModifiedProperties(); }
                }

                #endregion

                normaliseColourRange = EditorGUILayout.Toggle(new GUIContent("Normalise Colour Range", "Whether the colour range of the texture will be normalised - if ticked, the lightest colour will be white and the darkest colour will be black"), normaliseColourRange);
                invertColours = EditorGUILayout.Toggle(new GUIContent("Invert Colours", "Whether the colours of the texture will be inverted"), invertColours);

                if (EditorGUI.EndChangeCheck()) { previewUpdateRequired = true; }

                // Reset values for current outputType
                if (GUILayout.Button("Reset", GUILayout.Width(60f)))
                {
                    ResetDefaults();
                    previewUpdateRequired = true;
                }

                string generateButtonText = "Generate Texture";
                string generateProgressBarText = "Generating Texture";

                PopulateGenerateText(out generateButtonText, out generateProgressBarText);

                #region Generate Noise Texture
                if (outputTypeSelection == OutputType.Noise && GUILayout.Button(generateButtonText))
                {
                    //EditorUtility.DisplayProgressBar(generateProgressBarText, "Please Wait", 0.5f);

                    Texture2D newTexture = new Texture2D((int)outputTextureResolution, (int)outputTextureResolution);
                    if (newTexture == null) { }
                    else
                    {
                        LBEditorHelper.CheckFolder("Assets/LandscapeBuilder/Textures");

                        string outputFolder = "LandscapeBuilder/Textures/" + outputType;
                        LBEditorHelper.CheckFolder("Assets/" + outputFolder);

                        GenerateTexture(newTexture);

                        string textureAssetPath = "Assets/" + outputFolder + "/" + generatedTextureName + ".png";

                        // Check to see if the file already exists. If so, prompt the user to overwrite or cancel.
                        bool _saveTexture = true;
                        if (File.Exists(Application.dataPath + "/" + outputFolder + "/" + generatedTextureName + ".png"))
                        {
                            EditorUtility.ClearProgressBar();
                            _saveTexture = EditorUtility.DisplayDialog("A noise texture of the same name already exists", "Are you sure you want to save the texture? " +
                                                                "The currently existing texture will be lost.", "Overwrite", "Cancel");
                        }

                        if (_saveTexture)
                        {
                            //EditorUtility.DisplayProgressBar("Generating Noise Texture", "Please Wait", 0.75f);
                            LBTextureOperations.SaveTexture(newTexture, textureAssetPath, 0, true, true);
                        }
                    }

                    //EditorUtility.ClearProgressBar();
                }
                #endregion

                #region Generate Texture
                else if (sourceTexture != null && outputTypeSelection != OutputType.Noise && GUILayout.Button(generateButtonText))
                {
                    EditorUtility.DisplayProgressBar(generateProgressBarText, "Please Wait", 0.5f);

                    // Need to enable source texture readability here...
                    Texture2D newTexture = new Texture2D(sourceTexture.width, sourceTexture.height);

                    GenerateTexture(sourceTexture, newTexture);

                    LBEditorHelper.CheckFolder("Assets/LandscapeBuilder/Textures");

                    string outputFolder = "LandscapeBuilder/Textures/" + outputType;
                    LBEditorHelper.CheckFolder("Assets/" + outputFolder);

                    string textureAssetPath = "/" + outputFolder + "/" + generatedTextureName + ".png";
                    //Debug.Log("Texture output path: " + textureAssetPath);
                    byte[] encodedTexture = newTexture.EncodeToPNG();
                    FileStream file = File.Open(Application.dataPath + textureAssetPath, FileMode.Create);
                    BinaryWriter binary = new BinaryWriter(file);
                    binary.Write(encodedTexture);
                    file.Close();
                    AssetDatabase.Refresh();
                    TextureImporter texImporter = (TextureImporter)AssetImporter.GetAtPath("Assets" + textureAssetPath);
                    if (outputType == OutputType.NormalMap)
                    {
                        // Make the generated texture a normal map
                        texImporter.convertToNormalmap = true;
                        texImporter.alphaSource = TextureImporterAlphaSource.FromGrayScale;
                        texImporter.textureType = TextureImporterType.NormalMap;
                    }
                    texImporter.isReadable = true;
                    // Need to reimport the Texture to apply changes
                    AssetDatabase.ImportAsset("Assets" + textureAssetPath);
                    AssetDatabase.Refresh();

                    LBEditorHelper.HighlightItemInProjectWindow("Assets" + textureAssetPath);

                    EditorUtility.ClearProgressBar();
                }
                #endregion
            }
            #endregion

            #region TextureCombiner
            else if (generatorMode == GeneratorMode.TextureCombiner)
            {
                if (sourceTexturesList == null) { sourceTexturesList = new List<Texture2D>(); }

                textureCombineModeSelection = (TextureCombineMode)EditorGUILayout.EnumPopup(new GUIContent("Texture Combine Mode", "The way in which to combine the textures"), textureCombineModeSelection);
                if (textureCombineModeSelection != textureCombineMode)
                {
                    textureCombineMode = textureCombineModeSelection;
                }

                combinedTextureResolution = EditorGUILayout.IntField("Texture Resolution", combinedTextureResolution);

                // This code simulates the unity default array functionality, which editorgui can't do
                arrayInt = sourceTexturesList.Count;
                GUILayout.BeginHorizontal();
                if (textureCombineMode != TextureCombineMode.Channels)
                {
                    if (GUILayout.Button("Add Source Texture")) { arrayInt++; }
                    if (GUILayout.Button("Remove Source Texture")) { arrayInt--; }
                }
                else
                {
                    // Adding and removing textures is not allowed in Channels mode as there must always be exactly 4 textures
                    if (arrayInt > 4) { sourceTexturesList.RemoveRange(4, arrayInt - 4); }
                    else if (arrayInt < 4) { sourceTexturesList.AddRange(new List<Texture2D>(4 - arrayInt)); }
                    arrayInt = 4;
                    // Initialise the input channel mode list
                    if (inputChannelModeList == null || inputChannelModeList.Count == 0)
                    {
                        inputChannelModeList = new List<LBTextureOperations.InputChannelMode>();
                        inputChannelModeList.Add(LBTextureOperations.InputChannelMode.R);
                        inputChannelModeList.Add(LBTextureOperations.InputChannelMode.G);
                        inputChannelModeList.Add(LBTextureOperations.InputChannelMode.B);
                        inputChannelModeList.Add(LBTextureOperations.InputChannelMode.A);
                    }
                }
                if (arrayInt < 0) { arrayInt = 0; }
                GUILayout.EndHorizontal();
                // Add items to the list
                if (arrayInt > sourceTexturesList.Count)
                {
                    temp = arrayInt - sourceTexturesList.Count;
                    for (index = 0; index < temp; index++)
                    {
                        sourceTexturesList.Add(null);
                    }
                }
                // Remove items from the list
                else if (arrayInt < sourceTexturesList.Count)
                {
                    temp = sourceTexturesList.Count - arrayInt;
                    for (index = 0; index < temp; index++)
                    {
                        sourceTexturesList.RemoveAt(sourceTexturesList.Count - 1);
                    }
                }

                for (index = 0; index < sourceTexturesList.Count; index++)
                {
                    EditorGUI.BeginChangeCheck();
                    if (textureCombineMode == TextureCombineMode.Channels)
                    {
                        string channelText;
                        switch (index)
                        {
                            case 0: channelText = "R"; break;
                            case 1: channelText = "G"; break;
                            case 2: channelText = "B"; break;
                            case 3: channelText = "A"; break;
                            default: channelText = "R"; break;
                        }
                        inputChannelModeList[index] = (LBTextureOperations.InputChannelMode)EditorGUILayout.EnumPopup(new GUIContent("Input Channel/s (" + channelText + ")", "The channel/s of the source texture used as input for the " + channelText + " channel of the combined texture"), inputChannelModeList[index]);
                        sourceTexturesList[index] = (Texture2D)EditorGUILayout.ObjectField(new GUIContent("Source Texture (" + channelText + ")", "The source texture for the " + channelText + " channel of the combined texture"), sourceTexturesList[index], typeof(Texture2D), false);
                    }
                    else
                    {
                        sourceTexturesList[index] = (Texture2D)EditorGUILayout.ObjectField(new GUIContent("Source Texture " + (index + 1), "Texture Input " + (index + 1)), sourceTexturesList[index], typeof(Texture2D), false);
                    }
                    if (EditorGUI.EndChangeCheck() && sourceTexturesList[index] != null)
                    {
                        bool _isReadable = LBTextureOperations.IsTextureReadable(sourceTexturesList[index], true);
                        if (!_isReadable)
                        {
                            // Check to see if user fixed when prompted to do so
                            _isReadable = LBTextureOperations.IsTextureReadable(sourceTexturesList[index], false);
                            if (!_isReadable) { sourceTexturesList[index] = null; }
                        }
                    }
                }

                if (sourceTexturesList.Count > 0 && sourceTexturesList[0] != null && GUILayout.Button("Generate Combined Texture"))
                {
                    EditorUtility.DisplayProgressBar("Generating Combined Texture", "Please Wait", 0.1f);

                    // Need to enable source textures readability here...
                    bool hasInvalidTexture = false;
                    bool hasNullTexture = false;
                    foreach (Texture2D texture in sourceTexturesList)
                    {
                        if (texture == null) { hasNullTexture = true; break; }
                        hasInvalidTexture = hasInvalidTexture || !LBTextureOperations.IsTextureReadable(texture, true);
                    }

                    if (hasNullTexture) { Debug.LogWarning("LBTextureGenerator Combine Textures - some textures are null. Operation aborted."); }
                    else if (hasInvalidTexture) { Debug.LogWarning("LBTextureGenerator Combine Textures - some textures needed fixing, please try again."); }
                    else
                    {
                        // int textureWidth = sourceTexturesList[0].width;
                        // int textureHeight = sourceTexturesList[0].height;

                        int textureWidth = combinedTextureResolution;
                        int textureHeight = combinedTextureResolution;

                        EditorUtility.DisplayProgressBar("Generating Combined Texture", "Please Wait", 0.25f);
                        Texture2D newTexture = new Texture2D(textureWidth, textureHeight);

                        GenerateTexture(sourceTexturesList, newTexture);

                        LBEditorHelper.CheckFolder("Assets/LandscapeBuilder/Textures");

                        string outputFolder = "LandscapeBuilder/Textures/Combined";
                        LBEditorHelper.CheckFolder("Assets/" + outputFolder);

                        string textureAssetPath = "Assets/" + outputFolder + "/" + generatedTextureName + ".png";

                        // Check to see if the file already exists. If so, prompt the user to overwrite or cancel.
                        bool _generateTexture = true;
                        if (File.Exists(Application.dataPath + "/" + outputFolder + "/" + generatedTextureName + ".png"))
                        {
                            EditorUtility.ClearProgressBar();
                            _generateTexture = EditorUtility.DisplayDialog("A combined texture of the same name already exists", "Are you sure you want to save the texture? " +
                                                                "The currently existing texture will be lost.", "Overwrite", "Cancel");
                        }

                        if (_generateTexture)
                        {
                            EditorUtility.DisplayProgressBar("Generating Combined Texture", "Please Wait", 0.75f);
                            LBTextureOperations.SaveTexture(newTexture, textureAssetPath, 0, true, true);

                            //Debug.Log("Texture output path: " + textureAssetPath);
                            //byte[] encodedTexture = newTexture.EncodeToPNG();
                            //FileStream file = File.Open(Application.dataPath + textureAssetPath, FileMode.Create);
                            //BinaryWriter binary = new BinaryWriter(file);
                            //binary.Write(encodedTexture);
                            //file.Close();
                            //AssetDatabase.Refresh();
                            //TextureImporter texImporter = (TextureImporter)AssetImporter.GetAtPath("Assets" + textureAssetPath);
                            //texImporter.isReadable = true;
                            //// Need to reimport the Texture to apply changes
                            //AssetDatabase.ImportAsset("Assets" + textureAssetPath);
                            //AssetDatabase.Refresh();

                            //LBEditorHelper.HighlightItemInProjectWindow("Assets" + textureAssetPath);
                        }
                    }
                    EditorUtility.ClearProgressBar();
                }
            }
            #endregion

            GUILayout.EndVertical();

            #region Texture Preview
            if (generatorMode == GeneratorMode.TextureGenerator)
            {
                GUILayout.BeginVertical(EditorStyles.helpBox);

                updateTexturePreview = EditorGUILayout.Toggle(new GUIContent("Update Texture Preview", "Whether a realtime texture preview will be shown"), updateTexturePreview);

                // Noise has no source texture, so needs different preview method that other output types.
                if (updateTexturePreview && outputType == OutputType.Noise)
                {
                    previewQualitySelection = (PreviewQuality)EditorGUILayout.EnumPopup(new GUIContent("Preview Quality", "The quality of the texture preview - increasing this will display a higher resolution preview but will take longer to compute"), previewQualitySelection);

                    if (previewQualitySelection != previewQuality)
                    {
                        previewQuality = previewQualitySelection;
                        previewUpdateRequired = true;

                        int texturePreviewResolution = 2;
                        if (previewQuality == PreviewQuality.Low) { texturePreviewResolution = 128; }
                        else if (previewQuality == PreviewQuality.High) { texturePreviewResolution = 256; }
                        else { texturePreviewResolution = 512; }
                        sourceTexturePreview = new Texture2D(texturePreviewResolution, texturePreviewResolution);
                    }
                    else if (sourceTexturePreview == null) { sourceTexturePreview = new Texture2D(2, 2); }

                    if (texturePreview == null || texturePreview.width != sourceTexturePreview.width || texturePreview.height != sourceTexturePreview.height)
                    {
                        texturePreview = new Texture2D(sourceTexturePreview.width, sourceTexturePreview.height);
                    }

                    if (texturePreview != null)
                    {
                        if (previewUpdateRequired)
                        {
                            //Debug.Log("Generate preview texture...");
                            GenerateTexture(texturePreview);
                            texturePreview.Apply();
                            previewUpdateRequired = false;
                        }
                        GUILayout.BeginHorizontal();
                        GUIStyle boldCentredLabelStyle = EditorStyles.boldLabel;
                        boldCentredLabelStyle.alignment = TextAnchor.MiddleCenter;
                        EditorGUILayout.LabelField("OUTPUT", boldCentredLabelStyle, GUILayout.MaxWidth(position.width * 0.5f));
                        // Revert back to usual setting, otherwise this changes every editor?
                        boldCentredLabelStyle.alignment = TextAnchor.MiddleLeft;
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.ObjectField(new GUIContent("", ""), texturePreview, typeof(Texture2D), false, GUILayout.Height(200f), GUILayout.MaxWidth(position.width * 0.5f));
                        GUILayout.EndHorizontal();
                    }
                }
                // Preview of output types other than Noise
                else if (updateTexturePreview && sourceTexturePreview != null)
                {
                    previewQualitySelection = (PreviewQuality)EditorGUILayout.EnumPopup(new GUIContent("Preview Quality", "The quality of the texture preview - increasing this will display a higher resolution preview but will take longer to compute"), previewQualitySelection);
                    if (previewQualitySelection != previewQuality)
                    {
                        previewUpdateRequired = true;
                        previewQuality = previewQualitySelection;
                        if (sourceTexture != null)
                        {
                            int texturePreviewResolution = 2;
                            if (previewQuality == PreviewQuality.Low) { texturePreviewResolution = 128; }
                            else if (previewQuality == PreviewQuality.High) { texturePreviewResolution = 256; }
                            else { texturePreviewResolution = 512; }
                            sourceTexturePreview = LBTextureOperations.GenerateDownscaledTexture(sourceTexture, texturePreviewResolution);
                        }
                    }

                    if (texturePreview == null || texturePreview.width != sourceTexturePreview.width || texturePreview.height != sourceTexturePreview.height)
                    {
                        texturePreview = new Texture2D(sourceTexturePreview.width, sourceTexturePreview.height);
                    }

                    if (texturePreview != null)
                    {
                        if (previewUpdateRequired)
                        {
                            GenerateTexture(sourceTexturePreview, texturePreview);
                            texturePreview.Apply();
                            previewUpdateRequired = false;
                        }
                        // Rect previewRect = new Rect(0f, 250f, position.width, position.width);
                        // EditorGUI.DrawPreviewTexture(previewRect, texturePreview, null, ScaleMode.ScaleToFit);
                        GUILayout.BeginHorizontal();
                        GUIStyle boldCentredLabelStyle = EditorStyles.boldLabel;
                        boldCentredLabelStyle.alignment = TextAnchor.MiddleCenter;
                        EditorGUILayout.LabelField("INPUT", boldCentredLabelStyle, GUILayout.MaxWidth(position.width * 0.5f));
                        EditorGUILayout.LabelField("OUTPUT", boldCentredLabelStyle, GUILayout.MaxWidth(position.width * 0.5f));
                        // Revert back to usual setting, otherwise this changes every editor?
                        boldCentredLabelStyle.alignment = TextAnchor.MiddleLeft;
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.ObjectField(new GUIContent("", ""), sourceTexturePreview, typeof(Texture2D), false, GUILayout.Height(200f), GUILayout.MaxWidth(position.width * 0.5f));
                        EditorGUILayout.ObjectField(new GUIContent("", ""), texturePreview, typeof(Texture2D), false, GUILayout.Height(200f), GUILayout.MaxWidth(position.width * 0.5f));
                        GUILayout.EndHorizontal();
                    }
                }

                GUILayout.EndVertical();
            }
            else if (generatorMode == GeneratorMode.TextureCombiner)
            {

            }
            #endregion

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// Populates the Button Text and the ProgressBar text based on the current outputType
        /// </summary>
        /// <param name="generateButtonText"></param>
        /// <param name="generateProgressBarText"></param>
        private void PopulateGenerateText(out string generateButtonText, out string generateProgressBarText)
        {
            if (outputType == OutputType.Albedo)
            {
                generateButtonText = "Generate Texture";
                generateProgressBarText = "Generating Albedo Texture";
            }
            else if (outputType == OutputType.AlbedoAndSmoothness)
            {
                generateButtonText = "Generate Texture";
                generateProgressBarText = "Generating Albedo/Smoothness Texture";
            }
            // else if (outputType == OutputType.AlbedoAndTransparency)
            // {
            // 	generateButtonText = "Generate Texture";
            // 	generateProgressBarText = "Generating Albedo/Transparency Texture";
            // }
            else if (outputType == OutputType.MetallicAndSmoothness)
            {
                generateButtonText = "Generate Metallic/Smoothness Map";
                generateProgressBarText = "Generating Metallic/Smoothness Map";
            }
            else if (outputType == OutputType.Specular)
            {
                generateButtonText = "Generate Specular Map";
                generateProgressBarText = "Generating Specular Map";
            }
            else if (outputType == OutputType.NormalMap)
            {
                generateButtonText = "Generate Normal Map";
                generateProgressBarText = "Generating Normal Map";
            }
            else if (outputType == OutputType.HeightMap)
            {
                generateButtonText = "Generate Height Map";
                generateProgressBarText = "Generating Height Map";
            }
            else if (outputType == OutputType.Occlusion)
            {
                generateButtonText = "Generate Occlusion Map";
                generateProgressBarText = "Generating Occlusion Map";
            }
            else if (outputType == OutputType.Emission)
            {
                generateButtonText = "Generate Emission Map";
                generateProgressBarText = "Generating Emission Map";
            }
            else // Default
            {
                generateButtonText = "Generate Texture";
                generateProgressBarText = "Generating Texture";
            }
        }

        private void GenerateTexture(Texture2D source, Texture2D tex)
        {
            // Generate the texture according to the chosen options
            if (outputType == OutputType.Albedo)
            {
                if (albedoAlgorithm == AlbedoAlgorithm.ShadowRemoval)
                {
                    // Shadow removal algorithm
                    LBTextureOperations.ShadowDetection(source, tex, lightestShadowColour, true);
                }
                else if (albedoAlgorithm == AlbedoAlgorithm.ColourTintRemoval)
                {
                    // Tint removal algorithm
                    LBTextureOperations.RemoveTintColour(source, tex, tintColourToRemove);
                }
                else
                {
                    // Shadow removal algorithm
                    LBTextureOperations.ShadowDetection(source, tex, lightestShadowColour, true);
                    // Tint removal algorithm
                    LBTextureOperations.RemoveTintColour(source, tex, tintColourToRemove);
                }
            }
            else if (outputType == OutputType.AlbedoAndSmoothness)
            {
                if (albedoAlgorithm == AlbedoAlgorithm.ShadowRemoval)
                {
                    // Shadow removal algorithm
                    LBTextureOperations.ShadowDetection(source, tex, lightestShadowColour, true);
                }
                else if (albedoAlgorithm == AlbedoAlgorithm.ColourTintRemoval)
                {
                    // Tint removal algorithm
                    LBTextureOperations.RemoveTintColour(source, tex, tintColourToRemove);
                }
                else
                {
                    // Shadow removal algorithm
                    LBTextureOperations.ShadowDetection(source, tex, lightestShadowColour, true);
                    // Tint removal algorithm
                    LBTextureOperations.RemoveTintColour(source, tex, tintColourToRemove);
                }
                if (calculateSmoothnessData)
                {
                    // Encode smoothness in alpha channel
                    if (smoothnessAlgorithm == SmoothnessAlgorithm.PixelVariance)
                    {
                        // Pixel variance algorithm
                        LBTextureOperations.PixelVariance(source, tex, smoothVariance, steepVariance, true);
                    }
                }
                else
                {
                    // Set all of alpha channel to single value
                    LBTextureOperations.SetTextureAlpha(tex, smoothnessParameter);
                }
            }
            // else if (outputType == OutputType.AlbedoAndTransparency)
            // {
            // 	if (albedoAlgorithm == AlbedoAlgorithm.ShadowRemoval)
            // 	{
            // 		// Shadow removal algorithm
            // 		LBTextureOperations.ShadowDetection(source, tex, lightestShadowColour, true);
            // 	}
            // 	else
            // 	{
            // 		// Tint removal algorithm
            // 		LBTextureOperations.RemoveTintColour(source, tex, tintColourToRemove);
            // 	}
            // }
            else if (outputType == OutputType.MetallicAndSmoothness)
            {
                if (calculateMetallicData)
                {
                    if (metallicAlgorithm == MetallicAlgorithm.Specular)
                    {
                        // Specular algorithm
                        LBTextureOperations.SpecularRange(source, tex, minSpecularColour, maxSpecularColour);
                    }
                    else
                    {
                        // Colour match algorithm
                        LBTextureOperations.ColourMatch(source, tex, colourToMatch, colourRange);
                    }
                }
                else
                {
                    // Set all of RGB channels to single value
                    LBTextureOperations.SetTextureRGB(tex, Color.white * metallicParameter);
                }
                if (calculateSmoothnessData)
                {
                    // Encode smoothness in alpha channel
                    if (smoothnessAlgorithm == SmoothnessAlgorithm.PixelVariance)
                    {
                        // Pixel variance algorithm
                        LBTextureOperations.PixelVariance(source, tex, smoothVariance, steepVariance, true);
                    }
                }
                else
                {
                    // Set all of alpha channel to single value
                    LBTextureOperations.SetTextureAlpha(tex, smoothnessParameter);
                }
            }
            else if (outputType == OutputType.Specular)
            {
                LBTextureOperations.SpecularRange(source, tex, minSpecularColour, maxSpecularColour);
            }
            else if (outputType == OutputType.NormalMap)
            {
                if (heightAlgorithm == HeightAlgorithm.ShadowDetection)
                {
                    // Shadow detection algorithm
                    LBTextureOperations.ShadowDetection(source, tex, lightestShadowColour, false);
                }
                else if (heightAlgorithm == HeightAlgorithm.ColourRange)
                {
                    // Colour range algorithm
                    LBTextureOperations.ColourRange(source, tex, lowColour, midColour, highColour);
                }
                else
                {
                    // Shadow detection and low colour algorithm
                    LBTextureOperations.ShadowDetectionAndLowColour(source, tex, lightestShadowColour, shadowingLowColour, shadowingLowColourRange);
                }
            }
            else if (outputType == OutputType.HeightMap)
            {
                if (heightAlgorithm == HeightAlgorithm.ShadowDetection)
                {
                    // Shadow detection algorithm
                    LBTextureOperations.ShadowDetection(source, tex, lightestShadowColour, false);
                }
                else if (heightAlgorithm == HeightAlgorithm.ColourRange)
                {
                    // Colour range algorithm
                    LBTextureOperations.ColourRange(source, tex, lowColour, midColour, highColour);
                }
                else
                {
                    // Shadow detection and low colour algorithm
                    LBTextureOperations.ShadowDetectionAndLowColour(source, tex, lightestShadowColour, shadowingLowColour, shadowingLowColourRange);
                }
            }
            else if (outputType == OutputType.Occlusion)
            {
                // Encode occlusion in RGB channels
                if (occlusionAlgorithm == OcclusionAlgorithm.ShadowDetection)
                {
                    // Shadow detection algorithm
                    LBTextureOperations.ShadowDetection(source, tex, lightestShadowColour, false);
                }
                else
                {
                    // Pixel variance algorithm
                    LBTextureOperations.PixelVariance(source, tex, smoothVariance, steepVariance, false);
                }
            }
            else if (outputType == OutputType.Emission)
            {
                LBTextureOperations.SpecularRange(source, tex, minSpecularColour, maxSpecularColour);
            }

            // Normalise the colour range and/or invert colours if required
            tex = LBTextureOperations.FinaliseTextureColours(tex, normaliseColourRange, invertColours);
        }

        /// <summary>
        /// Generate a texture without needing a source texture.
        /// Populate the texture provided (tex).
        /// Currently used for generating Noise textures.
        /// </summary>
        /// <param name="tex"></param>
        private void GenerateTexture(Texture2D tex)
        {
            if (outputType == OutputType.Noise)
            {
                LBTextureOperations.CreateTextureNoise(tex, perlinNoiseOctaves, perlinNoiseTileSize, perlinNoiseLacunarity,
                 perlinNoiseGain, isTileable, isDistToCentreMask, outputStrength, perlinNoiseOutputCurveModifiers,
                  perlinNoisePerOctaveCurveModifiers, normaliseColourRange, invertColours, perlinNoiseGradient);
            }
        }

        /// <summary>
        /// Combine textures and populate the texture provided (tex)
        /// </summary>
        /// <param name="sourceTextures"></param>
        /// <param name="tex"></param>
        private void GenerateTexture(List<Texture2D> sourceTextures, Texture2D tex)
        {
            if (textureCombineMode == TextureCombineMode.Additive)
            {
                LBTextureOperations.CombineTexturesAdditive(sourceTextures, tex);
            }
            else if (textureCombineMode == TextureCombineMode.Minimum)
            {
                LBTextureOperations.CombineTexturesMinimum(sourceTextures, tex);
            }
            else if (textureCombineMode == TextureCombineMode.Maximum)
            {
                LBTextureOperations.CombineTexturesMaximum(sourceTextures, tex);
            }
            else
            {
                LBTextureOperations.CombineTexturesChannels(sourceTextures, tex, inputChannelModeList);
            }
        }

        private void ChangeTextureName(string sourceTextureName)
        {
            if (outputType == OutputType.Albedo) { generatedTextureName = sourceTextureName + "_albedo"; }
            else if (outputType == OutputType.AlbedoAndSmoothness) { generatedTextureName = sourceTextureName + "_albedo"; }
            // else if (outputType == OutputType.AlbedoAndTransparency) { generatedTextureName = sourceTextureName + "_albedo"; }
            else if (outputType == OutputType.MetallicAndSmoothness) { generatedTextureName = sourceTextureName + "_metallic"; }
            else if (outputType == OutputType.Specular) { generatedTextureName = sourceTextureName + "_specular"; }
            else if (outputType == OutputType.NormalMap) { generatedTextureName = sourceTextureName + "_normal"; }
            else if (outputType == OutputType.HeightMap) { generatedTextureName = sourceTextureName + "_parallax"; }
            else if (outputType == OutputType.Occlusion) { generatedTextureName = sourceTextureName + "_occlusion"; }
            else { generatedTextureName = sourceTextureName + "_emission"; }
        }

        /// <summary>
        /// Resets the UI controls based on the current outputType
        /// </summary>
        private void ResetDefaults()
        {
            if (outputType == OutputType.Albedo)
            {
                albedoAlgorithm = AlbedoAlgorithm.ShadowRemoval;
                lightestShadowColour = new Color(0.1f, 0.1f, 0.1f, 1f);
                tintColourToRemove = new Color(1f, 0f, 0f, 1f);
            }
            else if (outputType == OutputType.AlbedoAndSmoothness)
            {
                albedoAlgorithm = AlbedoAlgorithm.ShadowRemoval;
                lightestShadowColour = new Color(0.1f, 0.1f, 0.1f, 1f);
                tintColourToRemove = new Color(1f, 0f, 0f, 1f);
                calculateSmoothnessData = true;
                smoothnessAlgorithm = SmoothnessAlgorithm.PixelVariance;
                smoothVariance = 0f;
                steepVariance = 0.1f;
            }
            else if (outputType == OutputType.MetallicAndSmoothness)
            {
                calculateMetallicData = true;
                metallicAlgorithm = MetallicAlgorithm.Specular;
                minSpecularColour = Color.grey;
                maxSpecularColour = Color.white;
                colourToMatch = new Color(0.8f, 0.8f, 0.8f, 1f);
                colourRange = 0.1f;
                metallicParameter = 0f;
                calculateSmoothnessData = true;
                smoothnessAlgorithm = SmoothnessAlgorithm.PixelVariance;
                smoothVariance = 0f;
                steepVariance = 0.1f;
            }
            else if (outputType == OutputType.Specular)
            {
                minSpecularColour = Color.grey;
                maxSpecularColour = Color.white;
            }
            else if (outputType == OutputType.NormalMap)
            {
                heightAlgorithm = HeightAlgorithm.ShadowDetection;
                lightestShadowColour = new Color(0.1f, 0.1f, 0.1f, 1f);
                lowColour = new Color(0.1f, 0.1f, 0.1f, 1f);
                midColour = new Color(0.25f, 0.25f, 0.25f, 1f);
                highColour = new Color(0.5f, 0.5f, 0.5f, 5f);
                shadowingLowColour = new Color(0.1f, 0.1f, 0.1f, 1f);
                shadowingLowColourRange = 0.1f;
            }
            else if (outputType == OutputType.HeightMap)
            {
                heightAlgorithm = HeightAlgorithm.ShadowDetection;
                lightestShadowColour = new Color(0.1f, 0.1f, 0.1f, 1f);
                lowColour = new Color(0.1f, 0.1f, 0.1f, 1f);
                midColour = new Color(0.25f, 0.25f, 0.25f, 1f);
                highColour = new Color(0.5f, 0.5f, 0.5f, 5f);
                shadowingLowColour = new Color(0.1f, 0.1f, 0.1f, 1f);
                shadowingLowColourRange = 0.1f;
            }
            else if (outputType == OutputType.Occlusion)
            {
                occlusionAlgorithm = OcclusionAlgorithm.ShadowDetection;
                lightestShadowColour = new Color(0.1f, 0.1f, 0.1f, 1f);
                smoothVariance = 0f;
                steepVariance = 0.1f;
            }
            else if (outputType == OutputType.Emission)
            {
                minSpecularColour = Color.grey;
                maxSpecularColour = Color.white;
            }
            else if (outputType == OutputType.Noise)
            {
                outputTextureResolution = TextureResolution._512x512;
                perlinNoiseOctaves = 8;
                isTileable = true;
                perlinNoiseTileSize = 1;
                isDistToCentreMask = false;
                outputStrength = 1f;
                perlinNoiseLacunarity = 2f;
                perlinNoiseGain = 0.5f;
                perlinNoiseGradient = LBGradient.SetGradientFromPreset(LBGradient.GradientPreset.DefaultPerlinGradient);
            }

            // Applies to all
            invertColours = false;
            normaliseColourRange = false;
        }
    }
}