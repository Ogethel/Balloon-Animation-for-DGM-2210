using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LandscapeBuilder
{
    public class LBModifierOperations
    {
        // Modifier operations class
        #region Enumerations
        public enum ModifierLandformCategory
        {
            Hills = 0,
            Lakes = 1,
            Mesas = 2,
            Mountains = 3,
            Valleys = 4,
            Custom = 5
        }
        #endregion

        #region Static Public Variables
        // NOTE: Cannot use LBSetup.modifiersFolder because it is not available at runtime
        public static string baseModifierPath = "Assets/LandscapeBuilder/Modifiers";
        #endregion

        #region Public Static Methods

        /// <summary>
        /// Populate list with filename in the directory of a particular type.
        /// Mac is case sensitive so always use lowercase for file extensions
        /// </summary>
        /// <param name="ModifierCategory"></param>
        /// <param name="fileExtension"></param>
        /// <returns></returns>
        public static List<string> GetModifierList(string ModifierCategory, string fileExtension)
        {
            List<string> modifierList = new List<string>();
            string fileName = string.Empty;

            if (modifierList != null)
            {
                if (Directory.Exists(baseModifierPath + "/" + ModifierCategory))
                {
                    DirectoryInfo directory = new DirectoryInfo(baseModifierPath + "/" + ModifierCategory);

                    // Filter directory (folder) by PNG image files (Mac is case sensitive to convert to lowercase)
                    FileInfo[] files = directory.GetFiles("*." + fileExtension.ToLower());

                    // Loop through all the PNG files in the folder
                    foreach (FileInfo file in files)
                    {
                        fileName = file.Name;
                        // Strip off the file extension
                        fileName = fileName.Substring(0, fileName.Length - 4);
                        if (!string.IsNullOrEmpty(fileName))
                        {
                            modifierList.Add(fileName);
                        }
                    }
                }
            }

            return modifierList;
        }

        /// <summary>
        /// Get the normalised height of the centre of the modifier selector within the landscape
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="selectionRect"></param>
        public static float GetModifierSelectorCentreHeight(LBLandscape landscape, Rect selectionRect)
        {
            float centrePointHeight = 0f;
            Terrain[] landscapeTerrains;
            TerrainData tData;
            Vector3 worldPosition;
            float terrainWidth = 0f;
            float terrainHeight = 0f;

            Rect terrainRect;
            Vector2 selectionCentrePoint = selectionRect.center;

            if (landscape != null)
            {
                GameObject landscapeGameObject = landscape.gameObject;

                if (landscapeGameObject != null)
                {
                    // Get the terrains attached to the landscape
                    landscapeTerrains = landscapeGameObject.GetComponentsInChildren<Terrain>();

                    if (landscapeTerrains != null)
                    {
                        // Loop through all the terrains in the landscape until we find the one that contains the centre of the modifier selector
                        for (int index = 0; index < landscapeTerrains.Length; index++)
                        {
                            tData = landscapeTerrains[index].terrainData;
                            worldPosition = landscapeTerrains[index].transform.position;

                            // Terrain width (x) and length (z) are always the same in LB
                            terrainWidth = tData.size.x;

                            terrainHeight = tData.size.y;

                            // Get the rectange boundary of the terrain
                            terrainRect = Rect.MinMaxRect(worldPosition.x, worldPosition.z, worldPosition.x + terrainWidth, worldPosition.z + terrainWidth);

                            // Is the centre of the modifier selector within this terrain?
                            if (terrainRect.Contains(selectionCentrePoint))
                            {
                                // Convert modifier selector centrepoint into a normalised position on the terrain
                                float normXPos = Mathf.InverseLerp(worldPosition.x, worldPosition.x + terrainWidth, selectionCentrePoint.x);
                                float normYPos = Mathf.InverseLerp(worldPosition.z, worldPosition.z + terrainWidth, selectionCentrePoint.y);

                                // Get the height at that point on the terrain (0-1)
                                centrePointHeight = tData.GetInterpolatedHeight(normXPos, normYPos) / terrainHeight;
                            }
                        }
                    }
                    else { Debug.LogError("LBModifierOperations.GetModifierSelectorCentreHeight - no terrains attached to selected landscape"); }
                }
                else { Debug.LogError("LBModifierOperations.GetModifierSelectorCentreHeight - landscape parent gameobject is null"); }
            }
            else { Debug.LogError("LBModifierOperations.GetModifierSelectorCentreHeight - landscape script object is null"); }

            return centrePointHeight;
        }

        /// <summary>
        /// Returns the range of the greyscale values in a texture
        /// Min = 0, Max = 1. NOTE: Most textures will not return 0-1
        /// </summary>
        /// <param name="image"></param>
        /// <param name="ExcludeBlack"></param>
        /// <returns></returns>
        public static Vector2 GetTextureGrayscaleMinMax(Texture2D image, bool ExcludeBlack = true)
        {
            Vector2 range = new Vector2();
            float grayscale = 0f;

            range.x = 1f;  // Set minimum (x) to max possible
            range.y = 0f;  // Set maximum (y) to min possible

            if (image != null)
            {
                // Loop through image pixels
                for (int x = 0; x < image.width; x++)
                {
                    for (int y = 0; y < image.height; y++)
                    {
                        grayscale = image.GetPixel(x, y).grayscale;

                        if (grayscale < 0.01f && ExcludeBlack) { continue; }
                        else
                        {
                            // Check if this pixel has the lowest value so far 
                            if (grayscale < range.x) { range.x = grayscale; }

                            // Check if this pixel has the highest value so far
                            if (grayscale > range.y) { range.y = grayscale; }

                            // If range is 0,1 already, break out of the loop
                            if (range.x < 0.001 && range.y > 0.999) { break; }
                        }
                    }
                }
            }

            // if the range hasn't been set, make it 0,0
            if (range.x > 0.999f && range.y < 0.001f) { range = Vector2.zero; }

            return range;
        }

        #endregion

        #region Public Static Methods (EDITOR ONLY)

#if UNITY_EDITOR
        public static Texture2D GetLandformHeightmapImage(ModifierLandformCategory LandformCategory, string LandformName)
        {
            Texture2D heightmapImage = null;

            string modifierLandformFilePath = baseModifierPath + "/" + LandformCategory.ToString() + "/" + LandformName.ToString() + ".png";

            heightmapImage = (Texture2D)AssetDatabase.LoadAssetAtPath(modifierLandformFilePath, typeof(Texture2D));

            return heightmapImage;
        }

        public static string GetModifierFilePath(ModifierLandformCategory LandformCategory, string LandformName, string fileExtension)
        {
            return LBSetup.modifiersFolder + "/" + LandformCategory.ToString() + "/" + LandformName.ToString() + "." + fileExtension;
        }


#endif
        #endregion
    }
}