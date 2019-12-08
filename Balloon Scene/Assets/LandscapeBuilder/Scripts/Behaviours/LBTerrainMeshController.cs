using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace LandscapeBuilder
{
    /// <summary>
    /// LBTerrainMeshController holds an array of Lists. The lists contain a CombineInstance for each prefab instance
    /// that will be combined with other prefab instances (of the same prefab "type"). All items in the same
    /// CombineInstance list will be combined into one mesh.
    /// This is a temporary component that gets created in the scene hierarchy when Mesh/Prefabs OR Groups
    /// are applied to the landscape. Mesh/Prefabs creates one per terrain. Groups creates one per landscape.
    /// See also LBLandscapeMeshControllers.BuildCombineMeshes(..).
    /// </summary>
    [ExecuteInEditMode]
    public class LBTerrainMeshController : MonoBehaviour
    {
        public int GetCombineInstanceArrayListCount { get { return combineInstanceArrayList == null ? 0 : combineInstanceArrayList.Count;  } }

        private List<CombineInstance[]> combineInstanceArrayList;
        private List<string> groupMemberCombineGUIDList;

        #region Public Member Methods
        /// <summary>
        /// Initialises this instance
        /// </summary>
        public void Initialise()
        {
            combineInstanceArrayList = new List<CombineInstance[]>();
            groupMemberCombineGUIDList = new List<string>();
        }

        /// <summary>
        /// Adds a combine instance array to the list.
        /// groupMemberCombineGUID is the Group Member to have meshes combined.
        /// For legacy Mesh/Prefab LB tab, groupMemberOwnerGUID should be null.
        /// </summary>
        /// <param name="combineInstanceArray"></param>
        /// <param name="groupMemberCombineGUID"></param>
        public void AddCombineInstanceArray(CombineInstance[] combineInstanceArray, string groupMemberCombineGUID = null)
        {
            combineInstanceArrayList.Add(combineInstanceArray);
            if (!string.IsNullOrEmpty(groupMemberCombineGUID)) { groupMemberCombineGUIDList.Add(groupMemberCombineGUID); }
        }

        /// <summary>
        /// Retrieves a Combine Instance array given the index in the list describing which mesh it is
        /// </summary>
        /// <returns>The combine instance of mesh.</returns>
        /// <param name="meshIndex">Mesh index.</param>
        public CombineInstance[] GetCombineInstanceOfMesh(int meshIndex)
        {
            if (combineInstanceArrayList == null) { return null; }  // Added v1.3.0 Beta 7c
            else if (combineInstanceArrayList.Count > meshIndex)
            {
                return combineInstanceArrayList[meshIndex];
            }
            else { return null; }
        }

        /// <summary>
        /// Retrieves a CombineInstance array given the index in the list of arrays,
        /// and populate a supplied List with the retrieved array.
        /// </summary>
        /// <param name="meshIndex"></param>
        /// <param name="combineInstanceList"></param>
        /// <returns></returns>
        public bool GetCombineInstanceOfMesh(int meshIndex, ref List<CombineInstance> combineInstanceList)
        {
            bool isSuccessful = false;

            if (combineInstanceList != null)
            {
                if (combineInstanceArrayList != null)
                {
                    if (combineInstanceArrayList.Count > meshIndex)
                    {
                        CombineInstance[] combineInstance = combineInstanceArrayList[meshIndex];
                        if (combineInstance != null)
                        {
                            combineInstanceList.AddRange(combineInstance);
                        }
                    }
                }
            }

            return isSuccessful;
        }

        /// <summary>
        /// Retrieves a CombineInstance array given the index in the list of arrays,
        /// and populate a supplied List with the retrieved array.
        /// Retrieves the GroupMember's GUID for this CombineInstance array.
        /// </summary>
        /// <param name="meshIndex"></param>
        /// <param name="combineInstanceList"></param>
        /// <param name="groupMemberCombineGUID"></param>
        /// <returns></returns>
        public bool GetCombineInstanceOfMesh(int meshIndex, ref List<CombineInstance> combineInstanceList, ref string groupMemberCombineGUID)
        {
            bool isSuccessful = false;

            if (combineInstanceList != null)
            {
                if (combineInstanceArrayList != null && groupMemberCombineGUIDList != null)
                {
                    if (combineInstanceArrayList.Count > meshIndex && groupMemberCombineGUIDList.Count > meshIndex)
                    {
                        // This will create some Garbage, but probably can't be avoided
                        CombineInstance[] combineInstance = combineInstanceArrayList[meshIndex];
                        string groupMemberGUID = groupMemberCombineGUIDList[meshIndex];
                        if (combineInstance != null && !string.IsNullOrEmpty(groupMemberGUID))
                        {
                            combineInstanceList.AddRange(combineInstance);
                            groupMemberCombineGUID = groupMemberGUID;
                        }
                    }
                }
            }

            return isSuccessful;
        }

        /// <summary>
        /// Get the total number of CombineInstances in the list of combine instance arrays
        /// </summary>
        /// <returns></returns>
        public int GetCombineInstanceOfMeshTotal()
        {
            int numCombineInstances = 0;

            int numCombineInstanceList = (combineInstanceArrayList == null ? 0 : combineInstanceArrayList.Count);

            for (int i = 0; i < numCombineInstanceList; i++)
            {
                numCombineInstances += (combineInstanceArrayList[i] == null ? 0 : combineInstanceArrayList[i].Length);
            }

            return numCombineInstances;
        }

        #endregion

        #region Public Static Members

        /// <summary>
        /// Typically used to remove any (old) controllers that may be in the scene after some sort of failure
        /// </summary>
        /// <param name="tfrm"></param>
        public static void RemoveTerrainMeshControllers(Transform tfrm)
        {
            if (tfrm != null)
            {
                List<LBTerrainMeshController> terrainMeshControllerList = new List<LBTerrainMeshController>();
                if (terrainMeshControllerList != null)
                {
                    tfrm.GetComponentsInChildren<LBTerrainMeshController>(true, terrainMeshControllerList);
                    int numTMCtrlrs = terrainMeshControllerList == null ? 0 : terrainMeshControllerList.Count;
                    for (int tmcIdx = numTMCtrlrs - 1; tmcIdx >= 0; tmcIdx--)
                    {
                        DestroyImmediate(terrainMeshControllerList[tmcIdx].gameObject);
                    }
                }
            }
        }

        #endregion
    }
}
