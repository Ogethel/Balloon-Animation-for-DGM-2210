// Landscape Builder. Copyright (c) 2016-2019 SCSM Pty Ltd. All rights reserved.
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LandscapeBuilder
{
    [System.Serializable]
    public class LBLayer
    {
        // Class containing topography layer info
        // NOTE: If updating the Variables, also update all Constructors and ScriptLayer()

        #region Enumerations
        // Layer presets
        public enum LayerPreset
        {
            MountainRangeBase = 0,
            MountainRangeComplexBase = 1,
            MountainPeaksBase = 2,
            SwissMountainsBase = 3,
            RollingHillsBase = 4,
            GentleValleysBase = 5,
            DesertBase = 6,
            DesertFloorBase = 7,
            CanyonBase = 8,
            SmoothIslandsBase = 9,
            RuggedIslandsBase = 10,
            PlainsBase = 11,
            AlienDesertBase = 12,
            RuggedHillsBase = 13,
            MountainRangeAdditive = 100,
            MountainPeaksAdditive = 101,
            SwissMountainsAdditive = 102,
            GentleValleysAdditive = 103,
            DesertDunesAdditive = 104,
            CanyonAdditive = 105,
            RuggedHillsAdditive = 106,
            DefaultDetail = 200,
            RidgedDetail = 201,
            SwissDetail = 202,
            DesertDetail = 203,
            HillsDetail = 204,
            WarpedDetail = 205,
            GentleRiverSubtractive = 300
        }

        // Types of layers
        // If changing, also update LayerTypeInt Property
        public enum LayerType
        {
            PerlinBase = 0,
            PerlinAdditive = 1,
            PerlinSubtractive = 2,
            PerlinDetail = 3,
            ImageBase = 4,
            ImageAdditive = 5,
            ImageSubtractive = 6,
            ImageDetail = 7,
            MapPath = 20,
            //LandformModifier = 30,
            ImageModifier = 35,
            UnityTerrains = 50
        }

        // Currently this is only used for MapPath Layers
        public enum LayerTypeMode
        {
            Add = 0,
            Set = 5,
            Flatten = 10
        }

        // Added 1.3.0 Beta 4a
        // The source of the Image data may determine
        // how we import or interpret the heightmapImage
        public enum LayerImageSource
        {
            Default = 0,
            MIT_LRO_LOLA = 6,
            TerrainParty = 10
        }

        // Added 1.4.4 Beta 2
        public enum LayerModifierMode
        {
            Add = 0,
            Set = 5
        }

        #endregion

        #region Variables and Properties

        public LayerType type;
        public LayerPreset preset;

        // Added 2.1.2 Beta 1f - optional user-defined metadata
        public string layerName;

        // Added 1.3.2 Beta 9c
        // Currently only used MapPath type
        public LayerTypeMode layerTypeMode;

        public float noiseTileSize;
        public float noiseOffsetX;
        public float noiseOffsetZ;

        public int octaves;

        public int downscaling;
        public float lacunarity;
        public float gain;

        // Additive layer variables
        public float additiveAmount;
        public AnimationCurve additiveCurve;
        public bool removeBaseNoise;
        // Added 1.4.0 Beta 9c
        // AddHeight only used when removeBaseNoise is true
        public bool addMinHeight;
        public float addHeight;

        // Subtractive layer variables
        public float subtractiveAmount;
        public AnimationCurve subtractiveCurve;

        // Restrict area variables
        // x,y values are in metres from bottom left of landscape. They are in landscape-space co-ordinates, not true worldspace
        // landscape.position is subtracted from worldspace co-ordinates
        public bool restrictArea;
        public Rect areaRect;
        public float detailSmoothRate;  // Also used for MapPath smoothing
        public float areaBlendRate;

        public float warpAmount;
        public int warpOctaves;

        public List<AnimationCurve> outputCurveModifiers;
        public List<LBCurve.CurvePreset> outputCurveModifierPresets;
        public List<AnimationCurve> perOctaveCurveModifiers;
        public List<LBCurve.CurvePreset> perOctaveCurveModifierPresets;

        public float heightScale;

        // Image layer variables
        public Texture2D heightmapImage;
        public Texture2D blurredImage;
        // Used to pre-process images, remove holes in data etc. Added v1.3.2 Beta 1e
        [System.NonSerialized] public Texture2D processingImage;
        public int interpolationSmoothing;
        public float imageHeightScale;
        public List<AnimationCurve> imageCurveModifiers;
        public List<LBCurve.CurvePreset> imageCurveModifierPresets;
        // Added 1.3.0 Beta 4a - also used for GeoTIFF imported data in 1.4.3   
        public bool normaliseImage;     // Get the range of greyscale data and convert it to 0-1
        public bool isSmoothLayerOnly;  // Only apply detail smoothing to the current (image) layer, not the whole landscape
        public bool isSourceDataNormalised; // Greyscale TIFF heightmap files from World Creator are normalised values 0-65535

        // LayerImageSource: Terrain.Party
        // This data is adjusted for the Cities Skylines game. Each export from the website
        // is different but all have the lowest value set to 40m. Then the original heights
        // are added to the 40m (which is the artificial sea level). The README.txt file that
        // is exported with the images describes what the min/max heights are for the exported
        // terrain.party data.

        // LayerImageSource: MIT LRO-LOLA
        // Lunar Orbiter Laser Altimeter
        // http://imbrium.mit.edu/
        // http://imbrium.mit.edu/EXTRAS/SLDEM2015/TILES/
        public LayerImageSource imageSource;

        // Added 1.3.2 Beta 1e
        public bool imageRepairHoles;   // Will attempt to fix holes or zero values in source image data
        [Range(0f, 1f)] public float threshholdRepairHoles;

        // Added 1.4.3 Beta 2a
        public float floorOffsetY;       // Raise the floor of the data x metres on the y-axis to allow for rivers to be cut into floor of heightmap data

        public List<LBLayerFilter> filters;

        // Added 1.3.2 Beta 9c
        // MapPath LayerType variables
        public AnimationCurve mapPathBlendCurve;
        public LBCurve.MapPathBlendCurvePreset mapPathBlendCurvePreset;
        public AnimationCurve mapPathHeightCurve;
        public LBCurve.MapPathHeightCurvePreset mapPathHeightCurvePreset;
        public bool mapPathAddInvert;   // Used with TypeMode = Add, subtract instead of add
        public float minHeight;         // Used with TypeMode = Set, maxHeight re-uses heightScale variable
        public LBMapPath lbMapPath;     // This will not be serialized with the LBLayer but will be saved in the editor
        public LBPath lbPath;           // The serializable path class for the lbMapPath component. This stores the essential items in a path

        public bool isDisabled;
        public bool showLayer;
        public bool showAdvancedSettings;
        public bool showCurvesAndFilters;
        public bool showAreaHighlighter;

        // Added 1.4.2 Beta 2a
        // There is a LBTerrainData instance for each terrain in the landscape for this layer
        // There should only be 1 layer with imported Unity terrain data.
        public List<LBTerrainData> lbTerrainDataList;
        // Used for Imported Terrain data. Check if Height range of the data matches the landscape height
        public bool isCheckHeightRangeDiff = false;

        // Added 2.1.0 Beta 2
        // GRMT GeoTIFF data contains both +ve and -ve height data
        // After importing, setting this enabled a user to adjust the values
        public bool isBelowSeaLevelDataIncluded;

        // Added 1.4.4 Beta 2
        public bool modifierAddInvert;
        public bool modifierUseBlending;
        public float areaRectRotation;
        public LayerModifierMode modifierMode;
        public LBRaw modifierRAWFile;
        public LBModifierOperations.ModifierLandformCategory modifierLandformCategory;
        public LBRaw.SourceFileType modifierSourceFileType;
        public bool modifierUseWater;
        public LBWater modifierLBWater;
        public LBMesh modifierWaterLBMesh;
        public bool modifierWaterIsMeshLandscapeUV;         // Are the mesh UVs calculcated based on the vert positions in the landscape?
        public Vector2 modifierWaterMeshUVTileScale;        // The tiling of the UVs when the mesh is created.
        [System.NonSerialized] public Transform modifierWaterTransform;
        [System.NonSerialized] public Camera modifierWaterMainCamera;
        [System.NonSerialized] public List<string> modifierLandformList;

        public bool showVolumeHighlighter;

        private static int keyInt;

        /// <summary>
        /// Property to get the type as an integer
        /// Returns -1 if there is no match which should never occur
        /// </summary>
        public int LayerTypeInt
        {
            get
            {
                int t = -1;
                if (type == LBLayer.LayerType.PerlinBase) { t = 0; }
                else if (type == LBLayer.LayerType.PerlinAdditive) { t = 1; }
                else if (type == LBLayer.LayerType.PerlinSubtractive) { t = 2; }
                else if (type == LBLayer.LayerType.PerlinDetail) { t = 3; }
                else if (type == LBLayer.LayerType.ImageBase) { t = 4; }
                else if (type == LBLayer.LayerType.ImageAdditive) { t = 5; }
                else if (type == LBLayer.LayerType.ImageSubtractive) { t = 6; }
                else if (type == LBLayer.LayerType.ImageDetail) { t = 7; }
                else if (type == LBLayer.LayerType.MapPath) { t = 20; }
                //else if (type == LBLayer.LayerType.LandformModifier) { t = 30; }
                else if (type == LBLayer.LayerType.ImageModifier) { t = 35; }
                else if (type == LBLayer.LayerType.UnityTerrains) { t = 50; }
                return t;
            }
        }

        #endregion

        #region Constructors

        // Basic class constructor
        public LBLayer()
        {
            this.type = LayerType.PerlinAdditive;
            this.preset = LayerPreset.GentleValleysBase;
            this.layerName = string.Empty;
            this.layerTypeMode = LayerTypeMode.Add;
            this.modifierMode = LayerModifierMode.Set;

            this.additiveAmount = 0.5f;
            this.subtractiveAmount = 0.2f;
            this.additiveCurve = new AnimationCurve();
            this.subtractiveCurve = new AnimationCurve();
            this.removeBaseNoise = true;
            this.addMinHeight = false;
            this.addHeight = 0f;
            this.restrictArea = false;
            this.areaRect = new Rect(0f, 0f, 1000f, 1000f);
            this.areaRectRotation = 0f;
            this.interpolationSmoothing = 0;
            this.imageHeightScale = 1f;
            this.imageCurveModifiers = new List<AnimationCurve>();
            this.imageCurveModifierPresets = new List<LBCurve.CurvePreset>();
            this.isDisabled = false;
            this.showLayer = true;
            this.showAdvancedSettings = false;
            this.showCurvesAndFilters = false;
            this.showAreaHighlighter = false;
            this.showVolumeHighlighter = false;
            this.detailSmoothRate = 0f;
            this.areaBlendRate = 0.5f;
            this.downscaling = 1;
            this.warpAmount = 0f;
            this.warpOctaves = 0;
            this.normaliseImage = true;
            this.isSmoothLayerOnly = false;
            this.isSourceDataNormalised = false;
            this.imageSource = LayerImageSource.Default;
            this.imageRepairHoles = false;
            this.threshholdRepairHoles = 0f;
            this.floorOffsetY = 0f;
            this.blurredImage = null;
            this.processingImage = null;
            this.mapPathBlendCurve = LBCurve.SetCurveFromPreset(LBCurve.MapPathBlendCurvePreset.EaseInOut);
            this.mapPathBlendCurvePreset = LBCurve.MapPathBlendCurvePreset.EaseInOut;
            this.mapPathHeightCurve = LBCurve.SetCurveFromPreset(LBCurve.MapPathHeightCurvePreset.Flat);
            this.mapPathHeightCurvePreset = LBCurve.MapPathHeightCurvePreset.Flat;
            this.mapPathAddInvert = false;
            this.minHeight = 0f;
            this.lbPath = null;
            this.lbMapPath = null;
            this.lbTerrainDataList = null;
            this.isCheckHeightRangeDiff = true;
            this.isBelowSeaLevelDataIncluded = false;

            // Modifier variables
            this.modifierRAWFile = null;
            this.modifierLandformCategory = LBModifierOperations.ModifierLandformCategory.Custom;
            this.modifierSourceFileType = LBRaw.SourceFileType.RAW;
            this.modifierAddInvert = false;
            this.modifierUseBlending = false;
            this.modifierUseWater = false;
            this.modifierLBWater = null;
            this.modifierWaterLBMesh = null;
            this.modifierWaterIsMeshLandscapeUV = false;
            this.modifierWaterMeshUVTileScale = Vector2.one;
            this.modifierWaterMainCamera = null;
        }

        /// <summary>
        /// Constructor for cloning a layer
        /// </summary>
        /// <param name="lbLayer"></param>
        public LBLayer(LBLayer lbLayer)
        {
            this.type = lbLayer.type;
            this.preset = lbLayer.preset;
            this.layerName = lbLayer.layerName;
            this.layerTypeMode = lbLayer.layerTypeMode;
            this.modifierMode = lbLayer.modifierMode;
            this.noiseTileSize = lbLayer.noiseTileSize;
            this.noiseOffsetX = lbLayer.noiseOffsetX;
            this.noiseOffsetZ = lbLayer.noiseOffsetZ;
            this.octaves = lbLayer.octaves;
            this.downscaling = lbLayer.downscaling;
            this.lacunarity = lbLayer.lacunarity;
            this.gain = lbLayer.gain;

            this.additiveAmount = lbLayer.additiveAmount;
            this.subtractiveAmount = lbLayer.subtractiveAmount;
            this.additiveCurve = lbLayer.additiveCurve;
            this.subtractiveCurve = lbLayer.subtractiveCurve;
            this.removeBaseNoise = lbLayer.removeBaseNoise;
            this.addMinHeight = lbLayer.addMinHeight;
            this.addHeight = lbLayer.addHeight;
            this.restrictArea = lbLayer.restrictArea;
            this.areaRect = lbLayer.areaRect;
            this.areaRectRotation = lbLayer.areaRectRotation;
            this.interpolationSmoothing = lbLayer.interpolationSmoothing;
            this.imageHeightScale = lbLayer.imageHeightScale;
            if (lbLayer.imageCurveModifiers == null) { this.imageCurveModifiers = null; }
            else { this.imageCurveModifiers = new List<AnimationCurve>(lbLayer.imageCurveModifiers); }
            if (lbLayer.imageCurveModifierPresets == null) { this.imageCurveModifierPresets = null; }
            else { this.imageCurveModifierPresets = new List<LBCurve.CurvePreset>(lbLayer.imageCurveModifierPresets); }
            if (lbLayer.filters == null) { this.filters = new List<LBLayerFilter>(); }
            else { this.filters = lbLayer.filters.ConvertAll(filter => new LBLayerFilter(filter)); } // Deep copy
            this.isDisabled = lbLayer.isDisabled;
            this.showLayer = lbLayer.showLayer;
            this.showAdvancedSettings = lbLayer.showAdvancedSettings;
            this.showCurvesAndFilters = lbLayer.showCurvesAndFilters;
            this.showAreaHighlighter = lbLayer.showAreaHighlighter;
            this.showVolumeHighlighter = lbLayer.showVolumeHighlighter;
            this.detailSmoothRate = lbLayer.detailSmoothRate;
            this.areaBlendRate = lbLayer.areaBlendRate;
            this.downscaling = lbLayer.downscaling;
            this.warpAmount = lbLayer.warpAmount;
            this.warpOctaves = lbLayer.warpOctaves;
            if (lbLayer.outputCurveModifiers == null) { this.outputCurveModifiers = null; }
            else { this.outputCurveModifiers = new List<AnimationCurve>(lbLayer.outputCurveModifiers); }
            if (lbLayer.outputCurveModifierPresets == null) { this.outputCurveModifierPresets = null; }
            else { this.outputCurveModifierPresets = new List<LBCurve.CurvePreset>(lbLayer.outputCurveModifierPresets); }
            if (lbLayer.perOctaveCurveModifiers == null) { this.perOctaveCurveModifiers = null; }
            else { this.perOctaveCurveModifiers = new List<AnimationCurve>(lbLayer.perOctaveCurveModifiers); }
            if (lbLayer.perOctaveCurveModifierPresets == null) { this.perOctaveCurveModifierPresets = null; }
            else { this.perOctaveCurveModifierPresets = new List<LBCurve.CurvePreset>(lbLayer.perOctaveCurveModifierPresets); }
            this.heightScale = lbLayer.heightScale;
            this.minHeight = lbLayer.minHeight;
            this.normaliseImage = lbLayer.normaliseImage;
            this.isSmoothLayerOnly = lbLayer.isSmoothLayerOnly;
            this.isSourceDataNormalised = lbLayer.isSourceDataNormalised;
            this.imageSource = lbLayer.imageSource;
            this.imageRepairHoles = lbLayer.imageRepairHoles;
            this.threshholdRepairHoles = lbLayer.threshholdRepairHoles;
            this.floorOffsetY = lbLayer.floorOffsetY;
            this.blurredImage = lbLayer.blurredImage;
            this.processingImage = lbLayer.processingImage;
            this.mapPathBlendCurve = new AnimationCurve(lbLayer.mapPathBlendCurve.keys);
            this.mapPathBlendCurvePreset = lbLayer.mapPathBlendCurvePreset;
            this.mapPathHeightCurve = new AnimationCurve(lbLayer.mapPathHeightCurve.keys);
            this.mapPathHeightCurvePreset = lbLayer.mapPathHeightCurvePreset;
            this.mapPathAddInvert = lbLayer.mapPathAddInvert;

            // Reference to the original LBPath and LBMapPath in the scene
            this.lbPath = lbLayer.lbPath;
            this.lbMapPath = lbLayer.lbMapPath;

            if (lbLayer.lbTerrainDataList == null) { this.lbTerrainDataList = null; }
            //else { this.lbTerrainDataList = new List<LBTerrainData>(lbLayer.lbTerrainDataList); }
            else { this.lbTerrainDataList = lbLayer.lbTerrainDataList.ConvertAll(td => new LBTerrainData(td)); }
            this.isCheckHeightRangeDiff = lbLayer.isCheckHeightRangeDiff;
            this.isBelowSeaLevelDataIncluded = lbLayer.isBelowSeaLevelDataIncluded;

            // Modifier variables
            this.modifierAddInvert = lbLayer.modifierAddInvert;
            this.modifierUseBlending = lbLayer.modifierUseBlending;
            this.modifierRAWFile = lbLayer.modifierRAWFile;
            this.modifierLandformCategory = lbLayer.modifierLandformCategory;
            this.modifierSourceFileType = lbLayer.modifierSourceFileType;
            this.modifierUseWater = lbLayer.modifierUseWater;
            if (lbLayer.modifierLBWater == null) { this.modifierLBWater = null; } else { this.modifierLBWater = new LBWater(lbLayer.modifierLBWater, true); }
            if (lbLayer.modifierWaterLBMesh != null) { modifierWaterLBMesh = new LBMesh(lbLayer.modifierWaterLBMesh); } else { modifierWaterLBMesh = null; }

            this.modifierWaterIsMeshLandscapeUV = lbLayer.modifierWaterIsMeshLandscapeUV;
            this.modifierWaterMeshUVTileScale = lbLayer.modifierWaterMeshUVTileScale;
            this.modifierWaterMainCamera = lbLayer.modifierWaterMainCamera;
        }

        #endregion

        #region Presets

        // Creates an LBLayer object based on a given preset
        public static LBLayer SetLayerFromPreset(LayerPreset layerPreset)
        {
            // Create the new LBLayer object
            LBLayer layerFromPreset = new LBLayer();
            // Set the new preset
            layerFromPreset.preset = layerPreset;
            // Set noise offset to 0
            layerFromPreset.noiseOffsetX = 0f;
            layerFromPreset.noiseOffsetZ = 0f;

            // Set LBLayer variables according to the given preset
            if (layerPreset == LayerPreset.MountainRangeBase)
            {
                layerFromPreset.type = LayerType.PerlinBase;
                layerFromPreset.noiseTileSize = 5000f;
                layerFromPreset.octaves = 3;
                layerFromPreset.downscaling = 1;
                layerFromPreset.lacunarity = 1.95f;
                layerFromPreset.gain = 0.4f;
                layerFromPreset.warpAmount = 0f;
                layerFromPreset.warpOctaves = 1;
                layerFromPreset.outputCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.outputCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.perOctaveCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.perOctaveCurveModifiers.Add(LBCurve.SetCurveFromPreset(LBCurve.CurvePreset.SmoothDoubleRidged));
                layerFromPreset.perOctaveCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.perOctaveCurveModifierPresets.Add(LBCurve.CurvePreset.SmoothDoubleRidged);
                layerFromPreset.heightScale = 0.75f;
            }
            else if (layerPreset == LayerPreset.MountainRangeComplexBase)
            {
                layerFromPreset.type = LayerType.PerlinBase;
                layerFromPreset.noiseTileSize = 5000f;
                layerFromPreset.octaves = 8;
                layerFromPreset.downscaling = 1;
                layerFromPreset.lacunarity = 1.92f;
                layerFromPreset.gain = 0.45f;
                layerFromPreset.warpAmount = 0f;
                layerFromPreset.warpOctaves = 1;
                layerFromPreset.outputCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.outputCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.perOctaveCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.perOctaveCurveModifiers.Add(LBCurve.SetCurveFromPreset(LBCurve.CurvePreset.DoubleRidged));
                layerFromPreset.perOctaveCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.perOctaveCurveModifierPresets.Add(LBCurve.CurvePreset.DoubleRidged);
                layerFromPreset.heightScale = 1f;
            }
            else if (layerPreset == LayerPreset.MountainPeaksBase)
            {
                layerFromPreset.type = LayerType.PerlinBase;
                layerFromPreset.noiseTileSize = 5000f;
                layerFromPreset.octaves = 7;
                layerFromPreset.downscaling = 1;
                layerFromPreset.lacunarity = 1.75f;
                layerFromPreset.gain = 0.475f;
                layerFromPreset.warpAmount = 0f;
                layerFromPreset.warpOctaves = 1;
                layerFromPreset.outputCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.outputCurveModifiers.Add(LBCurve.SetCurveFromPreset(LBCurve.CurvePreset.PowerOfFour));
                layerFromPreset.outputCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.outputCurveModifierPresets.Add(LBCurve.CurvePreset.PowerOfFour);
                layerFromPreset.perOctaveCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.perOctaveCurveModifiers.Add(LBCurve.SetCurveFromPreset(LBCurve.CurvePreset.SmoothRidged));
                layerFromPreset.perOctaveCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.perOctaveCurveModifierPresets.Add(LBCurve.CurvePreset.SmoothRidged);
                layerFromPreset.heightScale = 1.5f;
            }
            else if (layerPreset == LayerPreset.SwissMountainsBase)
            {
                layerFromPreset.type = LayerType.PerlinBase;
                layerFromPreset.noiseTileSize = 5000f;
                layerFromPreset.octaves = 5;
                layerFromPreset.downscaling = 1;
                layerFromPreset.lacunarity = 1.75f;
                layerFromPreset.gain = 0.45f;
                layerFromPreset.warpAmount = 0f;
                layerFromPreset.warpOctaves = 1;
                layerFromPreset.outputCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.outputCurveModifiers.Add(LBCurve.SetCurveFromPreset(LBCurve.CurvePreset.PowerOfFour));
                layerFromPreset.outputCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.outputCurveModifierPresets.Add(LBCurve.CurvePreset.PowerOfFour);
                layerFromPreset.perOctaveCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.perOctaveCurveModifiers.Add(LBCurve.SetCurveFromPreset(LBCurve.CurvePreset.SmoothRidged));
                layerFromPreset.perOctaveCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.perOctaveCurveModifierPresets.Add(LBCurve.CurvePreset.SmoothRidged);
                layerFromPreset.heightScale = 2f;
            }
            else if (layerPreset == LayerPreset.RollingHillsBase)
            {
                layerFromPreset.type = LayerType.PerlinBase;
                layerFromPreset.noiseTileSize = 5000f;
                layerFromPreset.octaves = 5;
                layerFromPreset.downscaling = 1;
                layerFromPreset.lacunarity = 1.95f;
                layerFromPreset.gain = 0.4f;
                layerFromPreset.warpAmount = 0f;
                layerFromPreset.warpOctaves = 1;
                layerFromPreset.outputCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.outputCurveModifiers.Add(LBCurve.SetCurveFromPreset(LBCurve.CurvePreset.Invert));
                layerFromPreset.outputCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.outputCurveModifierPresets.Add(LBCurve.CurvePreset.Invert);
                layerFromPreset.perOctaveCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.perOctaveCurveModifiers.Add(LBCurve.SetCurveFromPreset(LBCurve.CurvePreset.SmoothRidged));
                layerFromPreset.perOctaveCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.perOctaveCurveModifierPresets.Add(LBCurve.CurvePreset.SmoothRidged);
                layerFromPreset.heightScale = 0.75f;
            }
            else if (layerPreset == LayerPreset.GentleValleysBase)
            {
                layerFromPreset.type = LayerType.PerlinBase;
                layerFromPreset.noiseTileSize = 5000f;
                layerFromPreset.octaves = 10;
                layerFromPreset.downscaling = 1;
                layerFromPreset.lacunarity = 1.75f;
                layerFromPreset.gain = 0.475f;
                layerFromPreset.warpAmount = 0f;
                layerFromPreset.warpOctaves = 1;
                layerFromPreset.outputCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.outputCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.perOctaveCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.perOctaveCurveModifiers.Add(LBCurve.SetCurveFromPreset(LBCurve.CurvePreset.SmoothRidged));
                layerFromPreset.perOctaveCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.perOctaveCurveModifierPresets.Add(LBCurve.CurvePreset.SmoothRidged);
                layerFromPreset.heightScale = 1f;
            }
            else if (layerPreset == LayerPreset.DesertBase)
            {
                layerFromPreset.type = LayerType.PerlinBase;
                layerFromPreset.noiseTileSize = 5000f;
                layerFromPreset.octaves = 4;
                layerFromPreset.downscaling = 1;
                layerFromPreset.lacunarity = 1.6f;
                layerFromPreset.gain = 0.475f;
                layerFromPreset.warpAmount = 0f;
                layerFromPreset.warpOctaves = 1;
                layerFromPreset.outputCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.outputCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.perOctaveCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.perOctaveCurveModifiers.Add(LBCurve.SetCurveFromPreset(LBCurve.CurvePreset.Ridged));
                layerFromPreset.perOctaveCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.perOctaveCurveModifierPresets.Add(LBCurve.CurvePreset.Ridged);
                layerFromPreset.heightScale = 1f;
            }
            else if (layerPreset == LayerPreset.DesertFloorBase)
            {
                layerFromPreset.type = LayerType.PerlinBase;
                layerFromPreset.noiseTileSize = 5000f;
                layerFromPreset.octaves = 8;
                layerFromPreset.downscaling = 1;
                layerFromPreset.lacunarity = 1.75f;
                layerFromPreset.gain = 0.49f;
                layerFromPreset.warpAmount = 0f;
                layerFromPreset.warpOctaves = 1;
                layerFromPreset.outputCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.outputCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.perOctaveCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.perOctaveCurveModifiers.Add(LBCurve.SetCurveFromPreset(LBCurve.CurvePreset.Ridged));
                layerFromPreset.perOctaveCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.perOctaveCurveModifierPresets.Add(LBCurve.CurvePreset.Ridged);
                layerFromPreset.heightScale = 0.25f;
            }
            else if (layerPreset == LayerPreset.CanyonBase)
            {
                layerFromPreset.type = LayerType.PerlinBase;
                layerFromPreset.noiseTileSize = 8000f;
                layerFromPreset.octaves = 12;
                layerFromPreset.downscaling = 1;
                layerFromPreset.lacunarity = 1.95f;
                layerFromPreset.gain = 0.5f;
                layerFromPreset.warpAmount = 0f;
                layerFromPreset.warpOctaves = 1;
                layerFromPreset.outputCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.outputCurveModifiers.Add(LBCurve.SetCurveFromPreset(LBCurve.CurvePreset.CanyonTerracing2));
                layerFromPreset.outputCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.outputCurveModifierPresets.Add(LBCurve.CurvePreset.CanyonTerracing2);
                layerFromPreset.perOctaveCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.perOctaveCurveModifiers.Add(LBCurve.SetCurveFromPreset(LBCurve.CurvePreset.InputMinMax));
                layerFromPreset.perOctaveCurveModifiers.Add(LBCurve.SetCurveFromPreset(LBCurve.CurvePreset.InputMinMax));
                layerFromPreset.perOctaveCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.perOctaveCurveModifierPresets.Add(LBCurve.CurvePreset.InputMinMax);
                layerFromPreset.perOctaveCurveModifierPresets.Add(LBCurve.CurvePreset.InputMinMax);
                layerFromPreset.heightScale = 1f;
            }
            else if (layerPreset == LayerPreset.SmoothIslandsBase)
            {
                layerFromPreset.type = LayerType.PerlinBase;
                layerFromPreset.noiseTileSize = 4000f;
                layerFromPreset.octaves = 4;
                layerFromPreset.downscaling = 1;
                layerFromPreset.lacunarity = 2f;
                layerFromPreset.gain = 0.5f;
                layerFromPreset.warpAmount = 0.5f;
                layerFromPreset.warpOctaves = 1;
                layerFromPreset.outputCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.outputCurveModifiers.Add(LBCurve.SetCurveFromPreset(LBCurve.CurvePreset.IslandSmoothing3));
                layerFromPreset.outputCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.outputCurveModifierPresets.Add(LBCurve.CurvePreset.IslandSmoothing3);
                layerFromPreset.perOctaveCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.perOctaveCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.heightScale = 1f;
            }
            else if (layerPreset == LayerPreset.RuggedIslandsBase)
            {
                layerFromPreset.type = LayerType.PerlinBase;
                layerFromPreset.noiseTileSize = 4000f;
                layerFromPreset.octaves = 8;
                layerFromPreset.downscaling = 1;
                layerFromPreset.lacunarity = 1.95f;
                layerFromPreset.gain = 0.48f;
                layerFromPreset.warpAmount = 0.5f;
                layerFromPreset.warpOctaves = 1;
                layerFromPreset.outputCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.outputCurveModifiers.Add(LBCurve.SetCurveFromPreset(LBCurve.CurvePreset.IslandSmoothing1));
                layerFromPreset.outputCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.outputCurveModifierPresets.Add(LBCurve.CurvePreset.IslandSmoothing1);
                layerFromPreset.perOctaveCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.perOctaveCurveModifiers.Add(LBCurve.SetCurveFromPreset(LBCurve.CurvePreset.SmoothTerraced));
                layerFromPreset.perOctaveCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.perOctaveCurveModifierPresets.Add(LBCurve.CurvePreset.SmoothTerraced);
                layerFromPreset.heightScale = 1.05f;
            }
            else if (layerPreset == LayerPreset.PlainsBase)
            {
                layerFromPreset.type = LayerType.PerlinBase;
                layerFromPreset.noiseTileSize = 5000f;
                layerFromPreset.octaves = 8;
                layerFromPreset.downscaling = 1;
                layerFromPreset.lacunarity = 1.75f;
                layerFromPreset.gain = 0.5f;
                layerFromPreset.warpAmount = 0f;
                layerFromPreset.warpOctaves = 1;
                layerFromPreset.outputCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.outputCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.perOctaveCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.perOctaveCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.heightScale = 0.25f;
            }
            else if (layerPreset == LayerPreset.AlienDesertBase)
            {
                layerFromPreset.type = LayerType.PerlinBase;
                layerFromPreset.noiseTileSize = 2000f;
                layerFromPreset.octaves = 6;
                layerFromPreset.downscaling = 1;
                layerFromPreset.lacunarity = 1.75f;
                layerFromPreset.gain = 0.5f;
                layerFromPreset.warpAmount = 2f;
                layerFromPreset.warpOctaves = 5;
                layerFromPreset.outputCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.outputCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.perOctaveCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.perOctaveCurveModifiers.Add(LBCurve.SetCurveFromPreset(LBCurve.CurvePreset.SmoothRidged));
                layerFromPreset.perOctaveCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.perOctaveCurveModifierPresets.Add(LBCurve.CurvePreset.SmoothRidged);
                layerFromPreset.heightScale = 0.3f;
            }
            else if (layerPreset == LayerPreset.RuggedHillsBase)
            {
                layerFromPreset.type = LayerType.PerlinBase;
                layerFromPreset.noiseTileSize = 5000f;
                layerFromPreset.octaves = 7;
                layerFromPreset.downscaling = 1;
                layerFromPreset.lacunarity = 1.95f;
                layerFromPreset.gain = 0.48f;
                layerFromPreset.warpAmount = 0f;
                layerFromPreset.warpOctaves = 1;
                layerFromPreset.outputCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.outputCurveModifiers.Add(LBCurve.SetCurveFromPreset(LBCurve.CurvePreset.SmoothTerraced));
                layerFromPreset.outputCurveModifiers.Add(LBCurve.SetCurveFromPreset(LBCurve.CurvePreset.PowerOfOnePointFive));
                layerFromPreset.outputCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.outputCurveModifierPresets.Add(LBCurve.CurvePreset.SmoothTerraced);
                layerFromPreset.outputCurveModifierPresets.Add(LBCurve.CurvePreset.PowerOfOnePointFive);
                layerFromPreset.perOctaveCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.perOctaveCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.heightScale = 1f;
            }
            else if (layerPreset == LayerPreset.MountainRangeAdditive)
            {
                layerFromPreset.type = LayerType.PerlinAdditive;
                layerFromPreset.noiseTileSize = 5000f;
                layerFromPreset.octaves = 8;
                layerFromPreset.downscaling = 1;
                layerFromPreset.lacunarity = 1.85f;
                layerFromPreset.gain = 0.4f;
                layerFromPreset.warpAmount = 0f;
                layerFromPreset.warpOctaves = 1;
                layerFromPreset.outputCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.outputCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.perOctaveCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.perOctaveCurveModifiers.Add(LBCurve.SetCurveFromPreset(LBCurve.CurvePreset.SmoothDoubleRidged));
                layerFromPreset.perOctaveCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.perOctaveCurveModifierPresets.Add(LBCurve.CurvePreset.SmoothDoubleRidged);
                layerFromPreset.heightScale = 1.3f;
            }
            else if (layerPreset == LayerPreset.MountainPeaksAdditive)
            {
                layerFromPreset.type = LayerType.PerlinAdditive;
                layerFromPreset.noiseTileSize = 5000f;
                layerFromPreset.octaves = 7;
                layerFromPreset.downscaling = 1;
                layerFromPreset.lacunarity = 1.75f;
                layerFromPreset.gain = 0.475f;
                layerFromPreset.warpAmount = 0f;
                layerFromPreset.warpOctaves = 1;
                layerFromPreset.outputCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.outputCurveModifiers.Add(LBCurve.SetCurveFromPreset(LBCurve.CurvePreset.PowerOfFour));
                layerFromPreset.outputCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.outputCurveModifierPresets.Add(LBCurve.CurvePreset.PowerOfFour);
                layerFromPreset.perOctaveCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.perOctaveCurveModifiers.Add(LBCurve.SetCurveFromPreset(LBCurve.CurvePreset.SmoothRidged));
                layerFromPreset.perOctaveCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.perOctaveCurveModifierPresets.Add(LBCurve.CurvePreset.SmoothRidged);
                layerFromPreset.heightScale = 1.5f;
            }
            else if (layerPreset == LayerPreset.SwissMountainsAdditive)
            {
                layerFromPreset.type = LayerType.PerlinAdditive;
                layerFromPreset.noiseTileSize = 5000f;
                layerFromPreset.octaves = 5;
                layerFromPreset.downscaling = 1;
                layerFromPreset.lacunarity = 1.75f;
                layerFromPreset.gain = 0.45f;
                layerFromPreset.warpAmount = 0f;
                layerFromPreset.warpOctaves = 1;
                layerFromPreset.outputCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.outputCurveModifiers.Add(LBCurve.SetCurveFromPreset(LBCurve.CurvePreset.PowerOfFour));
                layerFromPreset.outputCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.outputCurveModifierPresets.Add(LBCurve.CurvePreset.PowerOfFour);
                layerFromPreset.perOctaveCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.perOctaveCurveModifiers.Add(LBCurve.SetCurveFromPreset(LBCurve.CurvePreset.SmoothRidged));
                layerFromPreset.perOctaveCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.perOctaveCurveModifierPresets.Add(LBCurve.CurvePreset.SmoothRidged);
                layerFromPreset.heightScale = 2f;
            }
            else if (layerPreset == LayerPreset.GentleValleysAdditive)
            {
                layerFromPreset.type = LayerType.PerlinAdditive;
                layerFromPreset.noiseTileSize = 5000f;
                layerFromPreset.octaves = 10;
                layerFromPreset.downscaling = 1;
                layerFromPreset.lacunarity = 1.75f;
                layerFromPreset.gain = 0.475f;
                layerFromPreset.warpAmount = 0f;
                layerFromPreset.warpOctaves = 1;
                layerFromPreset.outputCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.outputCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.perOctaveCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.perOctaveCurveModifiers.Add(LBCurve.SetCurveFromPreset(LBCurve.CurvePreset.SmoothRidged));
                layerFromPreset.perOctaveCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.perOctaveCurveModifierPresets.Add(LBCurve.CurvePreset.SmoothRidged);
                layerFromPreset.heightScale = 1f;
            }
            else if (layerPreset == LayerPreset.DesertDunesAdditive)
            {
                layerFromPreset.type = LayerType.PerlinAdditive;
                layerFromPreset.noiseTileSize = 5000f;
                layerFromPreset.octaves = 4;
                layerFromPreset.downscaling = 1;
                layerFromPreset.lacunarity = 1.6f;
                layerFromPreset.gain = 0.475f;
                layerFromPreset.warpAmount = 0f;
                layerFromPreset.warpOctaves = 1;
                layerFromPreset.outputCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.outputCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.perOctaveCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.perOctaveCurveModifiers.Add(LBCurve.SetCurveFromPreset(LBCurve.CurvePreset.Ridged));
                layerFromPreset.perOctaveCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.perOctaveCurveModifierPresets.Add(LBCurve.CurvePreset.Ridged);
                layerFromPreset.heightScale = 1f;
            }
            else if (layerPreset == LayerPreset.CanyonAdditive)
            {
                layerFromPreset.type = LayerType.PerlinAdditive;
                layerFromPreset.noiseTileSize = 8000f;
                layerFromPreset.octaves = 12;
                layerFromPreset.downscaling = 1;
                layerFromPreset.lacunarity = 1.95f;
                layerFromPreset.gain = 0.5f;
                layerFromPreset.warpAmount = 0f;
                layerFromPreset.warpOctaves = 1;
                layerFromPreset.outputCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.outputCurveModifiers.Add(LBCurve.SetCurveFromPreset(LBCurve.CurvePreset.CanyonTerracing1));
                layerFromPreset.outputCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.outputCurveModifierPresets.Add(LBCurve.CurvePreset.CanyonTerracing1);
                layerFromPreset.perOctaveCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.perOctaveCurveModifiers.Add(LBCurve.SetCurveFromPreset(LBCurve.CurvePreset.InputMinMax));
                layerFromPreset.perOctaveCurveModifiers.Add(LBCurve.SetCurveFromPreset(LBCurve.CurvePreset.InputMinMax));
                layerFromPreset.perOctaveCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.perOctaveCurveModifierPresets.Add(LBCurve.CurvePreset.InputMinMax);
                layerFromPreset.perOctaveCurveModifierPresets.Add(LBCurve.CurvePreset.InputMinMax);
                layerFromPreset.heightScale = 1f;
            }
            else if (layerPreset == LayerPreset.RuggedHillsAdditive)
            {
                layerFromPreset.type = LayerType.PerlinAdditive;
                layerFromPreset.noiseTileSize = 5000f;
                layerFromPreset.octaves = 7;
                layerFromPreset.downscaling = 1;
                layerFromPreset.lacunarity = 1.95f;
                layerFromPreset.gain = 0.48f;
                layerFromPreset.warpAmount = 0f;
                layerFromPreset.warpOctaves = 1;
                layerFromPreset.outputCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.outputCurveModifiers.Add(LBCurve.SetCurveFromPreset(LBCurve.CurvePreset.SmoothTerraced));
                layerFromPreset.outputCurveModifiers.Add(LBCurve.SetCurveFromPreset(LBCurve.CurvePreset.PowerOfOnePointFive));
                layerFromPreset.outputCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.outputCurveModifierPresets.Add(LBCurve.CurvePreset.SmoothTerraced);
                layerFromPreset.outputCurveModifierPresets.Add(LBCurve.CurvePreset.PowerOfOnePointFive);
                layerFromPreset.perOctaveCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.perOctaveCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.heightScale = 1f;
            }
            else if (layerPreset == LayerPreset.DefaultDetail)
            {
                layerFromPreset.type = LayerType.PerlinDetail;
                layerFromPreset.noiseTileSize = 5000f;
                layerFromPreset.octaves = 8;
                layerFromPreset.downscaling = 32;
                layerFromPreset.lacunarity = 2f;
                layerFromPreset.gain = 0.5f;
                layerFromPreset.warpAmount = 0f;
                layerFromPreset.warpOctaves = 1;
                layerFromPreset.outputCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.outputCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.perOctaveCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.perOctaveCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.heightScale = 0.8f;
            }
            else if (layerPreset == LayerPreset.RidgedDetail)
            {
                layerFromPreset.type = LayerType.PerlinDetail;
                layerFromPreset.noiseTileSize = 5000f;
                layerFromPreset.octaves = 7;
                layerFromPreset.downscaling = 64;
                layerFromPreset.lacunarity = 1.95f;
                layerFromPreset.gain = 0.45f;
                layerFromPreset.warpAmount = 0f;
                layerFromPreset.warpOctaves = 1;
                layerFromPreset.outputCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.outputCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.perOctaveCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.perOctaveCurveModifiers.Add(LBCurve.SetCurveFromPreset(LBCurve.CurvePreset.SmoothRidged));
                layerFromPreset.perOctaveCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.perOctaveCurveModifierPresets.Add(LBCurve.CurvePreset.SmoothRidged);
                layerFromPreset.heightScale = 0.75f;
            }
            else if (layerPreset == LayerPreset.SwissDetail)
            {
                layerFromPreset.type = LayerType.PerlinDetail;
                layerFromPreset.noiseTileSize = 5000f;
                layerFromPreset.octaves = 7;
                layerFromPreset.downscaling = 32;
                layerFromPreset.lacunarity = 1.95f;
                layerFromPreset.gain = 0.5f;
                layerFromPreset.warpAmount = 0f;
                layerFromPreset.warpOctaves = 1;
                layerFromPreset.outputCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.outputCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.perOctaveCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.perOctaveCurveModifiers.Add(LBCurve.SetCurveFromPreset(LBCurve.CurvePreset.SmoothTerraced));
                layerFromPreset.perOctaveCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.perOctaveCurveModifierPresets.Add(LBCurve.CurvePreset.SmoothTerraced);
                layerFromPreset.heightScale = 0.5f;
            }
            else if (layerPreset == LayerPreset.DesertDetail)
            {
                layerFromPreset.type = LayerType.PerlinDetail;
                layerFromPreset.noiseTileSize = 5000f;
                layerFromPreset.octaves = 3;
                layerFromPreset.downscaling = 256;
                layerFromPreset.lacunarity = 1.6f;
                layerFromPreset.gain = 0.475f;
                layerFromPreset.warpAmount = 0f;
                layerFromPreset.warpOctaves = 1;
                layerFromPreset.outputCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.outputCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.perOctaveCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.perOctaveCurveModifiers.Add(LBCurve.SetCurveFromPreset(LBCurve.CurvePreset.Ridged));
                layerFromPreset.perOctaveCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.perOctaveCurveModifierPresets.Add(LBCurve.CurvePreset.Ridged);
                layerFromPreset.heightScale = 0.5f;
            }
            else if (layerPreset == LayerPreset.HillsDetail)
            {
                layerFromPreset.type = LayerType.PerlinDetail;
                layerFromPreset.noiseTileSize = 10000f;
                layerFromPreset.octaves = 7;
                layerFromPreset.downscaling = 64;
                layerFromPreset.lacunarity = 2f;
                layerFromPreset.gain = 0.5f;
                layerFromPreset.warpAmount = 0f;
                layerFromPreset.warpOctaves = 1;
                layerFromPreset.outputCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.outputCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.perOctaveCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.perOctaveCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.heightScale = 0.5f;
            }
            else if (layerPreset == LayerPreset.WarpedDetail)
            {
                layerFromPreset.type = LayerType.PerlinDetail;
                layerFromPreset.noiseTileSize = 5000f;
                layerFromPreset.octaves = 8;
                layerFromPreset.downscaling = 32;
                layerFromPreset.lacunarity = 1.95f;
                layerFromPreset.gain = 0.45f;
                layerFromPreset.warpAmount = 2f;
                layerFromPreset.warpOctaves = 5;
                layerFromPreset.outputCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.outputCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.perOctaveCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.perOctaveCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.heightScale = 0.4f;
            }
            else if (layerPreset == LayerPreset.GentleRiverSubtractive)
            {
                layerFromPreset.type = LayerType.PerlinSubtractive;
                layerFromPreset.noiseTileSize = 5000f;
                layerFromPreset.octaves = 7;
                layerFromPreset.downscaling = 1;
                layerFromPreset.lacunarity = 1.75f;
                layerFromPreset.gain = 0.475f;
                layerFromPreset.warpAmount = 0f;
                layerFromPreset.warpOctaves = 1;
                layerFromPreset.outputCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.outputCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.perOctaveCurveModifiers = new List<AnimationCurve>();
                layerFromPreset.perOctaveCurveModifiers.Add(LBCurve.SetCurveFromPreset(LBCurve.CurvePreset.IslandSmoothing3));
                layerFromPreset.perOctaveCurveModifierPresets = new List<LBCurve.CurvePreset>();
                layerFromPreset.perOctaveCurveModifierPresets.Add(LBCurve.CurvePreset.IslandSmoothing3);
                layerFromPreset.heightScale = 0.02f;
            }

            layerFromPreset.filters = new List<LBLayerFilter>();

            // Return the new LBLayer object
            return layerFromPreset;
        }

        #endregion

        #region Non-Static Public Methods

        public void AddRiverMapFilter()
        {
            if (filters == null) { filters = new List<LBLayerFilter>(); }
            if (filters.Count == 0)
            {
                LBLayerFilter newLayerFilter = new LBLayerFilter();
                newLayerFilter.type = LBLayerFilter.LayerFilterType.Map;
                filters.Add(newLayerFilter);
                removeBaseNoise = false;
                heightScale = 0.01f;
                subtractiveAmount = 1f;
                showCurvesAndFilters = true;
            }
        }

        /// <summary>
        /// Set the default values of a Image Modifier layer
        /// </summary>
        /// <param name="areaSize"></param>
        public void SetDefaultImageModifierValues(Vector2 areaSize)
        {
            // Don't reset invert as it will mess up water with Lakes / Valleys
            //modifierAddInvert = false;
            additiveAmount = 0.5f;
            imageHeightScale = 0.25f;
            areaRect = new Rect(areaSize.x * 0.5f, areaSize.y * 0.5f, areaSize.x, areaSize.y);
            areaRectRotation = 0f;
            floorOffsetY = 0f;
            modifierUseBlending = false;
            modifierWaterIsMeshLandscapeUV = false;
            modifierWaterMeshUVTileScale = Vector2.one;
            if (modifierLBWater != null)
            {
                modifierLBWater.waterLevel = -5f;
            }
        }

        /// <summary>
        /// Script out the layer for use in a runtime script
        /// </summary>
        /// <param name="IndexID"></param>
        /// <param name="EndOfLineMarker"></param>
        /// <returns></returns>
        public string ScriptLayer(int IndexID, string EndOfLineMarker = "\n")
        {
            // Create a new instance of StringBuilder and give it an estimated capacity
            System.Text.StringBuilder sb = new System.Text.StringBuilder(1000);

            string eol = " ";

            // We always need a space between lines OR a end of line marker like "\n"
            if (EndOfLineMarker.Length > 0) { eol = EndOfLineMarker; }

            string layerInst = "lbBaseLayer" + (IndexID + 1);
            string layerInstAbrev = "Lyr" + (IndexID + 1);

            sb.Append("// Layer Code generated from Landscape Builder at " + System.DateTime.Now.ToShortTimeString() + " on " + System.DateTime.Now.ToLongDateString() + eol + eol);
            sb.Append("// BEGIN Public variables to populate in the editor - Uncomment and add these to top of class" + eol);
            if (heightmapImage != null) { sb.Append("//public Texture2D heightmapImage" + layerInstAbrev + "; " + eol); }
            if (modifierRAWFile != null && type == LayerType.ImageModifier)
            {
                // TODO - create a LBRaw file from a .RAW or .PNG file

            }
            sb.Append("// END Public variables" + eol + eol);

            sb.Append("#region Topography Layer" + (IndexID + 1) + eol);

            sb.Append(LBCurve.ScriptCurve(additiveCurve, "\n", "additiveCurve" + layerInstAbrev) + eol);
            sb.Append(LBCurve.ScriptCurve(subtractiveCurve, "\n", "subtractiveCurve" + layerInstAbrev) + eol);

            sb.Append("List<LBLayerFilter> filters" + layerInstAbrev + " = new List<LBLayerFilter>();" + eol);
            sb.Append("// Add LayerFilter code here" + eol);

            sb.Append("List <AnimationCurve> imageCurveModifiers" + layerInstAbrev + " = new List<AnimationCurve>();" + eol);
            if (imageCurveModifiers != null)
            {
                for (int ocm = 0; ocm < imageCurveModifiers.Count; ocm++)
                {
                    sb.Append(LBCurve.ScriptCurve(imageCurveModifiers[ocm], "\n", "imageCurve" + layerInstAbrev + (ocm + 1).ToString()));
                    sb.Append("imageCurveModifiers" + layerInstAbrev + ".Add(imageCurve" + layerInstAbrev + (ocm + 1).ToString() + ");" + eol);
                }
            }
            sb.Append(eol);

            sb.Append("List <AnimationCurve> outputCurveModifiers" + layerInstAbrev + " = new List<AnimationCurve>();" + eol);
            if (outputCurveModifiers != null)
            {
                for (int ocm = 0; ocm < outputCurveModifiers.Count; ocm++)
                {
                    sb.Append(LBCurve.ScriptCurve(outputCurveModifiers[ocm], "\n", "outputCurve" + layerInstAbrev + (ocm + 1).ToString()));
                    sb.Append("outputCurveModifiers" + layerInstAbrev + ".Add(outputCurve" + layerInstAbrev + (ocm + 1).ToString() + ");" + eol);
                }
            }
            sb.Append(eol);

            sb.Append("List<LBCurve.CurvePreset> outputCurveModifierPresets" + layerInstAbrev + " = new List<LBCurve.CurvePreset>();");
            sb.Append(eol);
            if (outputCurveModifierPresets != null)
            {
                for (int ocmp = 0; ocmp < outputCurveModifierPresets.Count; ocmp++)
                {
                    sb.Append("outputCurveModifierPresets" + layerInstAbrev + ".Add(LBCurve.CurvePreset." + outputCurveModifierPresets[ocmp].ToString() + ");");
                    sb.Append(eol);
                }
            }
            sb.Append(eol);

            sb.Append("List <AnimationCurve> perOctaveCurveModifiers" + layerInstAbrev + " = new List<AnimationCurve>();" + eol);
            if (perOctaveCurveModifiers != null)
            {
                for (int pom = 0; pom < perOctaveCurveModifiers.Count; pom++)
                {
                    sb.Append(LBCurve.ScriptCurve(perOctaveCurveModifiers[pom], "\n", "perOctaveCurve" + layerInstAbrev + (pom + 1).ToString()));
                    sb.Append("perOctaveCurveModifiers" + layerInstAbrev + ".Add(perOctaveCurve" + layerInstAbrev + (pom + 1).ToString() + ");" + eol);
                }
            }
            sb.Append(eol);

            sb.Append("List<LBCurve.CurvePreset> perOctaveCurveModifierPresets" + layerInstAbrev + " = new List<LBCurve.CurvePreset>();");
            sb.Append(eol);
            if (outputCurveModifierPresets != null)
            {
                for (int pocmp = 0; pocmp < outputCurveModifierPresets.Count; pocmp++)
                {
                    sb.Append("perOctaveCurveModifierPresets" + layerInstAbrev + ".Add(LBCurve.CurvePreset." + perOctaveCurveModifierPresets[pocmp].ToString() + ");");
                    sb.Append(eol);
                }
            }
            sb.Append(eol);

            sb.Append(LBCurve.ScriptCurve(mapPathBlendCurve, "\n", "mapPathBlendCurve" + layerInstAbrev));
            sb.Append(LBCurve.ScriptCurve(mapPathHeightCurve, "\n", "mapPathHeightCurve" + layerInstAbrev));

            sb.Append("LBLayer " + layerInst + " = new LBLayer();" + eol);
            sb.Append("if (" + layerInst + " != null)" + eol);
            sb.Append("{" + eol);
            sb.Append("\t" + layerInst + ".layerName = \"" + layerName + "\";" + eol);
            sb.Append("\t" + layerInst + ".type = LBLayer.LayerType." + type + ";" + eol);
            sb.Append("\t" + layerInst + ".preset = LBLayer.LayerPreset." + preset + ";" + eol);
            sb.Append("\t" + layerInst + ".layerTypeMode = LBLayer.LayerTypeMode." + layerTypeMode + ";" + eol);
            sb.Append("\t" + layerInst + ".noiseTileSize = " + noiseTileSize + ";" + eol);
            sb.Append("\t" + layerInst + ".noiseOffsetX = " + noiseOffsetX + ";" + eol);
            sb.Append("\t" + layerInst + ".noiseOffsetZ = " + noiseOffsetZ + ";" + eol);
            sb.Append("\t" + layerInst + ".octaves = " + octaves + ";" + eol);
            sb.Append("\t" + layerInst + ".downscaling = " + downscaling + ";" + eol);
            sb.Append("\t" + layerInst + ".lacunarity = " + lacunarity + "f;" + eol);
            sb.Append("\t" + layerInst + ".gain = " + gain + "f;" + eol);
            sb.Append("\t" + layerInst + ".additiveAmount = " + additiveAmount + "f; " + eol);
            sb.Append("\t" + layerInst + ".subtractiveAmount = " + subtractiveAmount + "f; " + eol);
            sb.Append("\t" + layerInst + ".additiveCurve = additiveCurve" + layerInstAbrev + ";" + eol);
            sb.Append("\t" + layerInst + ".subtractiveCurve = subtractiveCurve" + layerInstAbrev + ";" + eol);
            sb.Append("\t" + layerInst + ".removeBaseNoise = " + removeBaseNoise.ToString().ToLower() + "; " + eol);
            sb.Append("\t" + layerInst + ".addMinHeight = " + addMinHeight.ToString().ToLower() + "; " + eol);
            sb.Append("\t" + layerInst + ".addHeight = " + addHeight + "f; " + eol);
            sb.Append("\t" + layerInst + ".restrictArea = " + restrictArea.ToString().ToLower() + "; " + eol);
            sb.Append("\t" + layerInst + ".areaRect = new Rect(" + areaRect.x + "," + areaRect.y + "," + areaRect.width + "," + areaRect.height + "); " + eol);
            sb.Append("\t" + layerInst + ".interpolationSmoothing = " + interpolationSmoothing + "; " + eol);
            if (heightmapImage != null) { sb.Append("\t" + layerInst + ".heightmapImage = heightmapImage" + layerInstAbrev + "; " + eol); }
            else { sb.Append("\t// " + layerInst + ".heightmapImage = heightmapImage" + layerInstAbrev + ";" + eol); }
            sb.Append("\t" + layerInst + ".imageHeightScale = " + imageHeightScale + "f; " + eol);
            sb.Append("\t" + layerInst + ".imageCurveModifiers = imageCurveModifiers" + layerInstAbrev + ";" + eol);
            //imageCurveModifierPresets = new List<LBCurve.CurvePreset>(imageCurveModifierPresets" + layerInstAbrev + ") + ";" + eol;
            sb.Append("\t" + layerInst + ".filters = filters" + layerInstAbrev + ";" + eol);
            sb.Append("\t" + layerInst + ".isDisabled = " + isDisabled.ToString().ToLower() + "; " + eol);
            sb.Append("\t" + layerInst + ".showLayer = " + showLayer.ToString().ToLower() + "; " + eol);
            sb.Append("\t" + layerInst + ".showAdvancedSettings = " + showAdvancedSettings.ToString().ToLower() + "; " + eol);
            sb.Append("\t" + layerInst + ".showCurvesAndFilters = " + showCurvesAndFilters.ToString().ToLower() + "; " + eol);
            sb.Append("\t" + layerInst + ".showAreaHighlighter = " + showAreaHighlighter.ToString().ToLower() + "; " + eol);
            sb.Append("\t" + layerInst + ".detailSmoothRate = " + detailSmoothRate + "f; " + eol);
            sb.Append("\t" + layerInst + ".areaBlendRate = " + areaBlendRate + "f; " + eol);
            sb.Append("\t" + layerInst + ".downscaling = " + downscaling + "; " + eol);
            sb.Append("\t" + layerInst + ".warpAmount = " + warpAmount + "f; " + eol);
            sb.Append("\t" + layerInst + ".warpOctaves = " + warpOctaves + "; " + eol);
            sb.Append("\t" + layerInst + ".outputCurveModifiers = outputCurveModifiers" + layerInstAbrev + "; " + eol);
            sb.Append("\t" + layerInst + ".outputCurveModifierPresets = outputCurveModifierPresets" + layerInstAbrev + "; " + eol);
            sb.Append("\t" + layerInst + ".perOctaveCurveModifiers = perOctaveCurveModifiers" + layerInstAbrev + "; " + eol);
            sb.Append("\t" + layerInst + ".perOctaveCurveModifierPresets = perOctaveCurveModifierPresets" + layerInstAbrev + "; " + eol);
            sb.Append("\t" + layerInst + ".heightScale = " + heightScale + "f; " + eol);
            sb.Append("\t" + layerInst + ".minHeight = " + minHeight + "f; " + eol);
            sb.Append("\t" + layerInst + ".imageSource = LBLayer.LayerImageSource." + imageSource + "; " + eol);
            sb.Append("\t" + layerInst + ".imageRepairHoles = " + imageRepairHoles.ToString().ToLower() + "; " + eol);
            sb.Append("\t" + layerInst + ".threshholdRepairHoles = " + threshholdRepairHoles + "f; " + eol);
            sb.Append("\t" + layerInst + ".normaliseImage = " + normaliseImage.ToString().ToLower() + "; " + eol);
            sb.Append("\t" + layerInst + ".isBelowSeaLevelDataIncluded = " + isBelowSeaLevelDataIncluded.ToString().ToLower() + "; " + eol);
            sb.Append("\t" + layerInst + ".floorOffsetY = " + floorOffsetY + "f; " + eol);
            //blurredImage = blurredImage + "; " + eol;
            //processingImage = processingImage + "; " + eol;
            sb.Append("\t" + layerInst + ".mapPathBlendCurve = mapPathBlendCurve" + layerInstAbrev + "; " + eol);
            sb.Append("\t" + layerInst + ".mapPathHeightCurve = mapPathHeightCurve" + layerInstAbrev + "; " + eol);

            //mapPathBlendCurvePreset = mapPathBlendCurvePreset" + layerInstAbrev + " + "; " + eol;
            //mapPathHeightCurvePreset = mapPathHeightCurvePreset" + layerInstAbrev + " + "; " + eol;
            sb.Append("\t" + layerInst + ".mapPathAddInvert = " + mapPathAddInvert.ToString().ToLower() + "; " + eol);

            // Add path details if required. NOTE: Must match variable name from LBMapPath.ScriptLayer(..)
            if (type == LayerType.MapPath && lbMapPath != null)
            {
                string pathInst = "lbMapPath" + lbMapPath.name.Replace(" ", "");
                //Reference to the original LBPath and LBMapPath in the scene
                sb.Append("\t" + layerInst + ".lbPath = " + pathInst + ".lbPath; " + eol);
                sb.Append("\t" + layerInst + ".lbMapPath = " + pathInst + "; " + eol);
            }         

            if (type == LayerType.ImageModifier)
            {
                if (modifierRAWFile != null)
                {
                    sb.Append(eol);
                    sb.Append("\t// MANAUL STEPS REQUIRED" + eol);
                    sb.Append("\t// 1. In the Application.dataPath folder create a Heightmaps folder." + eol);
                    sb.Append("\t// 2. If using a LB modifier, create a duplicate of the .raw file." + eol);
                    sb.Append("\t// 3. Copy the .raw file into the new Heightmaps folder." + eol);
                    sb.Append("\t// 4. Ensure the file in Heightmaps folder is the same name as the original." + eol);
                    sb.Append("\tLBRaw lbRaw" + layerInstAbrev + " = LBRaw.ImportHeightmapRAW(Application.dataPath + \"/Heightmaps/" + modifierRAWFile.dataSourceName + "\", false, false);" + eol);
                    sb.Append("\tif (lbRaw" + layerInstAbrev + " != null)" + eol);
                    sb.Append("\t{" + eol);
                    sb.Append("\t\tlbBaseLayer1.modifierRAWFile = lbRaw" + layerInstAbrev + ";" + eol);
                    sb.Append("\t}" + eol);
                }
            }

            // Modifier Layer variables
            sb.Append("\t" + layerInst + ".modifierMode = LBLayer.LayerModifierMode." + modifierMode + "; " + eol);
            sb.Append("\t" + layerInst + ".modifierAddInvert = " + modifierAddInvert.ToString().ToLower() + "; " + eol);
            sb.Append("\t" + layerInst + ".modifierUseBlending = " + modifierUseBlending.ToString().ToLower() + "; " + eol);
            sb.Append("\t" + layerInst + ".areaRectRotation = " + areaRectRotation + "f; " + eol);
            sb.Append("\t" + layerInst + ".modifierLandformCategory = LBModifierOperations.ModifierLandformCategory." + modifierLandformCategory.ToString() + "; " + eol);
            sb.Append("\t" + layerInst + ".modifierSourceFileType = LBRaw.SourceFileType." + modifierSourceFileType.ToString() + "; " + eol);

            sb.Append("\t// Currently runtime does not support water with Modifier Layers - contact support or post in our Unity forum if you need this feature " + eol);
            sb.Append("\t" + layerInst + ".modifierUseWater = false;" + eol);
            //sb.Append("\t" + layerInst + ".modifierUseWater = " + modifierUseWater.ToString().ToLower() + "; " + eol);
            //if (lbLayer.modifierLBWater == null) { this.modifierLBWater = null; } else { this.modifierLBWater = new LBWater(lbLayer.modifierLBWater, true); }
            //if (lbLayer.modifierWaterLBMesh != null) { modifierWaterLBMesh = new LBMesh(lbLayer.modifierWaterLBMesh); } else { modifierWaterLBMesh = null; }

            sb.Append("\t" + layerInst + ".modifierWaterIsMeshLandscapeUV = " + modifierWaterIsMeshLandscapeUV.ToString().ToLower() + ";" + eol);
            sb.Append("\t" + layerInst + ".modifierWaterMeshUVTileScale = new Vector2(" + modifierWaterMeshUVTileScale.x + "f," + modifierWaterMeshUVTileScale.y + "f);" + eol);

            //sb.Append("\tmodifierWaterMainCamera = lbLayer.modifierWaterMainCamera;

            sb.Append("\t// NOTE Add the new layer to the landscape meta-data");
            sb.Append(eol);
            sb.Append("\tlandscape.topographyLayersList.Add(" + layerInst + ");");
            sb.Append(eol);

            sb.Append("}" + eol);

            sb.Append("#endregion" + eol);
            sb.Append("// END OF CODE SEGMENT" + eol);

            return sb.ToString();
        }

        #endregion

        #region Static Public Methods

        // Create a curve for an additive layer based on a float value
        public static AnimationCurve CreateAdditiveCurve(float additiveFloat)
        {
            AnimationCurve newCurve = new AnimationCurve();

            if (additiveFloat < 1f) { keyInt = newCurve.AddKey(0f, 0f); }
            keyInt = newCurve.AddKey(1f - additiveFloat, 0f);
            if (additiveFloat > 0f) { keyInt = newCurve.AddKey(1f, 1f); }

            Keyframe[] curveKeys = newCurve.keys;
            if (curveKeys.Length > 2)
            {
                curveKeys[0].inTangent = 0f;
                curveKeys[0].outTangent = 0f;
                curveKeys[1].inTangent = 0f;
                curveKeys[1].outTangent = 1f / additiveFloat;
                curveKeys[2].inTangent = 1f / additiveFloat;
                curveKeys[2].outTangent = 0f;
            }
            else
            {
                curveKeys[0].inTangent = 0f;
                curveKeys[0].outTangent = 1f;
                curveKeys[1].inTangent = 1f;
                curveKeys[1].outTangent = 0f;
            }
            newCurve = new AnimationCurve(curveKeys);

            // Stops warning "variable is assigned but its value is never used" from appearing in the compiler
            if (keyInt == 0) { }

            return newCurve;
        }

        // Create a curve for an subtractive layer based on a float value
        public static AnimationCurve CreateSubtractiveCurve(float subtractiveFloat)
        {
            AnimationCurve newCurve = new AnimationCurve();

            if (subtractiveFloat < 1f) { keyInt = newCurve.AddKey(0f, 0f); }
            keyInt = newCurve.AddKey(1f - subtractiveFloat, 0f);
            if (subtractiveFloat > 0f) { keyInt = newCurve.AddKey(1f, 1f); }

            Keyframe[] curveKeys = newCurve.keys;
            if (curveKeys.Length > 2)
            {
                curveKeys[0].inTangent = 0f;
                curveKeys[0].outTangent = 0f;
                curveKeys[1].inTangent = 0f;
                curveKeys[1].outTangent = 1f / subtractiveFloat;
                curveKeys[2].inTangent = 1f / subtractiveFloat;
                curveKeys[2].outTangent = 0f;
            }
            else
            {
                curveKeys[0].inTangent = 0f;
                curveKeys[0].outTangent = 1f;
                curveKeys[1].inTangent = 1f;
                curveKeys[1].outTangent = 0f;
            }
            newCurve = new AnimationCurve(curveKeys);

            // Stops warning "variable is assigned but its value is never used" from appearing in the compiler
            if (keyInt == 0) { }

            return newCurve;
        }

        /// <summary>
        /// Get the total number of unique stencil layers of a given resolution within a list of LBLayerFilter.
        /// NOTE: Assumes that stencil, stencilLayer and stencilLayerResolution has already been cached in the LBLayerFilter.
        /// </summary>
        /// <param name="lbLayerFilterList"></param>
        /// <param name="resolution"></param>
        /// <returns></returns>
        public static int GetNumStencilLayersByResolution(List<LBLayerFilter> lbLayerFilterList, int resolution)
        {
            int numUniqueLayers = 0;
            LBLayerFilter lbLayerFilter = null;

            int numLBFilters = lbLayerFilterList == null ? 0 : lbLayerFilterList.Count;

            if (numLBFilters > 0)
            {
                // Create with some initial capacity
                List<string> stencilLayerGUIDList = new List<string>(10);

                for (int fIdx = 0; fIdx < numLBFilters; fIdx++)
                {
                    lbLayerFilter = lbLayerFilterList[fIdx];

                    // Is this a valid stencil layer with the correct resolution?
                    if (lbLayerFilter != null && lbLayerFilter.type == LBLayerFilter.LayerFilterType.StencilLayer && !string.IsNullOrEmpty(lbLayerFilter.lbStencilGUID) && !string.IsNullOrEmpty(lbLayerFilter.lbStencilLayerGUID) && lbLayerFilter.stencilLayerResolution == resolution)
                    {
                        //Debug.Log("[DEBUG] GetNumStencilLayersByResolution " + lbLayerFilter.lbStencilLayer.LayerName + " " + lbLayerFilter.stencilLayerResolution + " GUID: " + lbLayerFilter.lbStencilLayerGUID + " " + lbLayerFilter.lbStencilLayer.GUID);

                        // If it doesn't already exist, add it to the list of unique Stencil Layer GUIDs
                        if (!stencilLayerGUIDList.Exists(guid => guid == lbLayerFilter.lbStencilLayerGUID))
                        {
                            numUniqueLayers++;
                            stencilLayerGUIDList.Add(lbLayerFilter.lbStencilLayerGUID);
                            //Debug.Log(" [DEBUG] GetNumStencilLayersByResolution - Adding " + lbLayerFilter.lbStencilLayerGUID + " resolution: " + resolution);
                        }
                    }
                }
            }

            return numUniqueLayers;
        }

        #endregion
    }
}