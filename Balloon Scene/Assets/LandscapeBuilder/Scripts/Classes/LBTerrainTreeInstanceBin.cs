using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LandscapeBuilder
{
    /// <summary>
    /// Stores the Unity TreeInstance data for serialization to disk
    /// with a binary formatter.
    /// See also LBUndo class in LBLandscape.cs
    /// and LBLandscape.SaveTrees1D(..), LBLandscape.RevertTrees1D(..)
    /// </summary>
    [System.Serializable]
    public class LBTerrainTreeInstanceBin
    {
        #region Public variables and Properties

        // Vector3 as separate components to save to disk
        public float positionX;
        public float positionY;
        public float positionZ;

        // Store Color32 as 4 floats so they can be serialized to disk
        // Note: Color and Vector4 also cannot be serialized to disk
        public float treeColourR;
        public float treeColourG;
        public float treeColourB;
        public float treeColourA;
        public float lightmapColourR;
        public float lightmapColourG;
        public float lightmapColourB;
        public float lightmapColourA;
        public float heightScale;
        public float widthScale;
        public float rotation; // rotation of the tree on the x-z plane (in radians)
        public int prototypeIndex; // index of this instance in the TerrainData.treePrototypes array

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor, also calls the base class constructor
        /// </summary>
        public LBTerrainTreeInstanceBin()
        {
            positionX = positionY = positionZ = 0f;
            treeColourR = treeColourG = treeColourB = treeColourA = 1f; // White
            lightmapColourR = lightmapColourG = lightmapColourB = lightmapColourA = 1f; // White
            heightScale = 1f;
            widthScale = 1f;
            rotation = 0f;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Takes an array of Unity Terrain TreeInstances, and returns a list of LBTerrainTreeInstanceBin.
        /// This is used to hold the meta data of trees serialized to/from disk. See also LBUndo class,
        /// LBLandscape.SaveTrees1D(..).
        /// </summary>
        /// <param name="treeInstances"></param>
        /// <returns></returns>
        public static List<LBTerrainTreeInstanceBin> ToLBTerrainTreeInstanceBinList(TreeInstance[] treeInstances)
        {
            List<LBTerrainTreeInstanceBin> lbTerrainTreeInstanceBinList = null;

            if (treeInstances != null)
            {
                if (treeInstances.Length > 0)
                {
                    lbTerrainTreeInstanceBinList = new List<LBTerrainTreeInstanceBin>();
                    Color32 colour = new Color32();
                    Vector3 position = Vector3.zero;

                    for (int i = 0; i < treeInstances.Length; i++)
                    {
                        LBTerrainTreeInstanceBin temp = new LBTerrainTreeInstanceBin();

                        position = treeInstances[i].position;
                        temp.positionX = position.x;
                        temp.positionY = position.y;
                        temp.positionZ = position.z;

                        colour = treeInstances[i].color;

                        temp.treeColourR = colour.r / 255f;
                        temp.treeColourG = colour.g / 255f;
                        temp.treeColourB = colour.b / 255f;
                        temp.treeColourA = colour.a / 255f;

                        colour = treeInstances[i].lightmapColor;

                        temp.lightmapColourR = colour.r / 255f;
                        temp.lightmapColourG = colour.g / 255f;
                        temp.lightmapColourB = colour.b / 255f;
                        temp.lightmapColourA = colour.a / 255f;

                        temp.heightScale = treeInstances[i].heightScale;
                        temp.widthScale = treeInstances[i].widthScale;
                        temp.rotation = treeInstances[i].rotation;
                        temp.prototypeIndex = treeInstances[i].prototypeIndex;

                        lbTerrainTreeInstanceBinList.Add(temp);
                        temp = null;
                    }
                }
            }

            return lbTerrainTreeInstanceBinList;
        }

        /// <summary>
        /// Get a list of Unity TreeInstance from a list of LBTerrainTreeInstanceBin
        /// </summary>
        /// <param name="lbTerrainTreeInstanceBinList"></param>
        /// <returns></returns>
        public static List<TreeInstance> ToTreeInstanceList(List<LBTerrainTreeInstanceBin> lbTerrainTreeInstanceBinList)
        {
            List<TreeInstance> treeInstanceList = null;

            if (lbTerrainTreeInstanceBinList != null)
            {
                if (lbTerrainTreeInstanceBinList.Count > 0)
                {
                    treeInstanceList = new List<TreeInstance>();

                    int numTreeInstances = lbTerrainTreeInstanceBinList == null ? 0 : lbTerrainTreeInstanceBinList.Count;

                    if (numTreeInstances > 0)
                    {
                        Color32 colour = new Color32();
                        Vector3 position = Vector3.zero;
                        LBTerrainTreeInstanceBin lbTerrainTreeInstanceBin;

                        for (int i = 0; i < numTreeInstances; i++)
                        {
                            lbTerrainTreeInstanceBin = lbTerrainTreeInstanceBinList[i];

                            if (lbTerrainTreeInstanceBin != null)
                            {
                                TreeInstance temp = new TreeInstance();

                                position.x = lbTerrainTreeInstanceBin.positionX;
                                position.y = lbTerrainTreeInstanceBin.positionY;
                                position.z = lbTerrainTreeInstanceBin.positionZ;
                                temp.position = position;

                                colour.r = (byte)Mathf.RoundToInt(lbTerrainTreeInstanceBin.treeColourR * 255f);
                                colour.g = (byte)Mathf.RoundToInt(lbTerrainTreeInstanceBin.treeColourG * 255f);
                                colour.b = (byte)Mathf.RoundToInt(lbTerrainTreeInstanceBin.treeColourB * 255f);
                                colour.a = (byte)Mathf.RoundToInt(lbTerrainTreeInstanceBin.treeColourA * 255f);
                                temp.color = colour;

                                colour.r = (byte)Mathf.RoundToInt(lbTerrainTreeInstanceBin.lightmapColourR * 255f);
                                colour.g = (byte)Mathf.RoundToInt(lbTerrainTreeInstanceBin.lightmapColourG * 255f);
                                colour.b = (byte)Mathf.RoundToInt(lbTerrainTreeInstanceBin.lightmapColourB * 255f);
                                colour.a = (byte)Mathf.RoundToInt(lbTerrainTreeInstanceBin.lightmapColourA * 255f);
                                temp.lightmapColor = colour;

                                temp.heightScale = lbTerrainTreeInstanceBinList[i].heightScale;
                                temp.widthScale = lbTerrainTreeInstanceBinList[i].widthScale;
                                temp.rotation = lbTerrainTreeInstanceBinList[i].rotation;
                                temp.prototypeIndex = lbTerrainTreeInstanceBinList[i].prototypeIndex;

                                treeInstanceList.Add(temp);
                            }
                        }
                    }
                }
            }

            return treeInstanceList;
        }

        #endregion
    }
}
