using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LandscapeBuilder
{
    public class LBObjectProximity
    {
        public Vector3 position;
        public float proximity;

        // Currently used for trees, to remember what terrain the tree is from
        public short terrainIndex;
        // Currently used for trees, to remember the index in the terrain trees array
        public int objectIndex;

        #region Constuctors

        public LBObjectProximity(Vector3 pos, float proxim)
        {
            this.position = pos;
            this.proximity = proxim;
            this.terrainIndex = 0;
            this.objectIndex = 0;
        }

        public LBObjectProximity(Vector3 pos, float proxim, short tIndex, int oIndex)
        {
            this.position = pos;
            this.proximity = proxim;
            this.terrainIndex = tIndex;
            this.objectIndex = oIndex;
        }

        #endregion
    }
}