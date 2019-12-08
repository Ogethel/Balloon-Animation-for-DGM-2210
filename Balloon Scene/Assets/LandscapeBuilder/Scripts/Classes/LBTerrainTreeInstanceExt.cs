using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace LandscapeBuilder
{
    /// <summary>
    /// Stores the Unity TreeInstance data, used for importing trees from
    /// a Unity Terrain which was not procedurally created by Landscape Builder
    /// </summary>
    [System.Serializable]
    public class LBTerrainTreeInstanceExt : LBTerrainTreeInstance
    {
        #region Public variables and Properties

        public Color32 treeColour;
        public Color32 lightmapColour;
        public float heightScale;
        public float widthScale;
        public float rotation; // rotation of the tree on the x-z plane (in radians)
        public int prototypeIndex; // index of this instance in the TerrainData.treePrototypes array

        #endregion


        #region Constructors

        /// <summary>
        /// Default constructor, also calls the base class constructor
        /// </summary>
        public LBTerrainTreeInstanceExt() : base()
        {
            treeColour = Color.white;
            lightmapColour = Color.white;
            heightScale = 1f;
            widthScale = 1f;
            rotation = 0f;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Takes an array of Unity Terrain TreeInstances, and returns a list of LBTerrainTreeInstanceExt.
        /// This is used to hold the meta data of trees imported into a LB Landscape from a terrain that was
        /// created outside LB. Optionally, filter by the tree prototypeIndex.
        /// </summary>
        /// <param name="treeInstances"></param>
        /// <param name="prototypeIndex"></param>
        /// <returns></returns>
        public static List<LBTerrainTreeInstanceExt> ToLBTerrainTreeInstanceExtList(TreeInstance[] treeInstances, int prototypeIndex = -1)
        {
            List<LBTerrainTreeInstanceExt> lbTerrainTreeInstanceExtList = null;

            if (treeInstances != null)
            {
                if (treeInstances.Length > 0)
                {
                    lbTerrainTreeInstanceExtList = new List<LBTerrainTreeInstanceExt>();

                    for (int i = 0; i < treeInstances.Length; i++)
                    {
                        // If filtering by prototypeIndex, skip if not a match
                        if (prototypeIndex >= 0 && treeInstances[i].prototypeIndex != prototypeIndex) { continue; }

                        LBTerrainTreeInstanceExt temp = new LBTerrainTreeInstanceExt();

                        temp.position = treeInstances[i].position;

                        temp.treeColour = treeInstances[i].color;
                        temp.lightmapColour = treeInstances[i].lightmapColor;
                        temp.heightScale = treeInstances[i].heightScale;
                        temp.widthScale = treeInstances[i].widthScale;
                        temp.rotation = treeInstances[i].rotation;
                        temp.prototypeIndex = treeInstances[i].prototypeIndex;

                        lbTerrainTreeInstanceExtList.Add(temp);
                        temp = null;
                    }
                }
            }

            return lbTerrainTreeInstanceExtList;
        }

        /// <summary>
        /// Get a list of Unity TreeInstance from a list of LBTerrainTreeInstanceExt
        /// This is used to convert serializable meta data of trees imported into a LB Landscape from a terrain
        /// that was created outside LB, back into Unity terrain TreeInstances. e.g. For populating terrains with trees.
        /// </summary>
        /// <param name="lbTerrainTreeInstanceExtList"></param>
        /// <param name="protoTypeIndex"></param>
        /// <returns></returns>
        public static List<TreeInstance> ToTreeInstanceList(List<LBTerrainTreeInstanceExt> lbTerrainTreeInstanceExtList, int protoTypeIndex)
        {
            List<TreeInstance> treeInstanceList = null;

            if (lbTerrainTreeInstanceExtList != null)
            {
                if (lbTerrainTreeInstanceExtList.Count > 0)
                {
                    treeInstanceList = new List<TreeInstance>();

                    if (treeInstanceList != null)
                    {
                        for (int i = 0; i < lbTerrainTreeInstanceExtList.Count; i++)
                        {
                            TreeInstance temp = new TreeInstance();

                            temp.position = lbTerrainTreeInstanceExtList[i].position;
                            temp.color = lbTerrainTreeInstanceExtList[i].treeColour;
                            temp.lightmapColor = lbTerrainTreeInstanceExtList[i].lightmapColour;
                            temp.heightScale = lbTerrainTreeInstanceExtList[i].heightScale;
                            temp.widthScale = lbTerrainTreeInstanceExtList[i].widthScale;
                            temp.rotation = lbTerrainTreeInstanceExtList[i].rotation;
                            temp.prototypeIndex = protoTypeIndex;

                            treeInstanceList.Add(temp);
                        }
                    }
                }
            }

            return treeInstanceList;
        }

        /// <summary>
        /// Get a list of the base class from the extension class
        /// </summary>
        /// <param name="lbTerrainTreeInstanceExtList"></param>
        /// <returns></returns>
        public static List<LBTerrainTreeInstance> ToLBTerrainTreeInstanceList(List<LBTerrainTreeInstanceExt> lbTerrainTreeInstanceExtList)
        {
            List<LBTerrainTreeInstance> lbTerrainTreeInstanceList = null;

            if (lbTerrainTreeInstanceExtList != null)
            {
                if (lbTerrainTreeInstanceExtList.Count > 0)
                {
                    lbTerrainTreeInstanceList = new List<LBTerrainTreeInstance>();

                    if (lbTerrainTreeInstanceList != null)
                    {
                        for (int i = 0; i < lbTerrainTreeInstanceExtList.Count; i++)
                        {
                            LBTerrainTreeInstance lbTerrainTreeInstance = new LBTerrainTreeInstance(lbTerrainTreeInstanceExtList[i].position, 0f, 0f);
                            if (lbTerrainTreeInstance != null)
                            {
                                lbTerrainTreeInstanceList.Add(lbTerrainTreeInstance);
                            }
                        }
                    }
                }
            }

            return lbTerrainTreeInstanceList;
        }

        #endregion
    }
}