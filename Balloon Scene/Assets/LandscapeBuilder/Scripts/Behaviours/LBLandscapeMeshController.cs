using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace LandscapeBuilder
{
    [ExecuteInEditMode]
    public class LBLandscapeMeshController : MonoBehaviour
    {
        /// <summary>
        /// Builds a combined mesh from a list of Landscape Meshes
        /// Returns true when combined meshes were created, else false.
        /// If there were no meshes, then will also return false.
        /// </summary>
        /// <param name="landscapeMeshList"></param>
        /// <returns></returns>
        public bool BuildCombinedMeshes(List<LBLandscapeMesh> landscapeMeshList)
        {
            bool combinedMeshesCreated = false;

            LBTerrainMeshController[] terrainMeshControllers = GetComponentsInChildren<LBTerrainMeshController>();

            bool missingCombinedInstance = false;
            int combinedMeshIndex = 0;

            for (int m = 0; m < landscapeMeshList.Count; m++)
            {
                if (landscapeMeshList[m].isDisabled) { continue; }

                List<CombineInstance> combineInstanceList = new List<CombineInstance>();

                for (int tmc = 0; tmc < terrainMeshControllers.Length; tmc++)
                {
                    //Debug.Log("INFO: LBLandscapeMeshControllers.BuildCombineMeshes CombineInstanceofMesh:" + terrainMeshControllers[tmc].GetCombineInstanceOfMesh(combinedMeshIndex).Length);

                    if (terrainMeshControllers[tmc].GetCombineInstanceOfMesh(combinedMeshIndex) == null)
                    {
                        if (!missingCombinedInstance && !landscapeMeshList[m].usePrefab && (landscapeMeshList[m].mesh != null))
                        {
                            missingCombinedInstance = true;
                            Debug.LogWarning("LBLandscapeMeshController.BuildCombinedMeshes: Could not find all combined mesh instances in the scene." +
                                           " If the mesh prefab is not null, try deleting all [meshname] Combined Mesh gameobjects under the Landscape gameobject and Populate Landscape again");
                        }
                    }
                    else { combineInstanceList.AddRange(terrainMeshControllers[tmc].GetCombineInstanceOfMesh(combinedMeshIndex)); }
                }

                //Debug.Log("BuildCombinedMeshes mc: " + terrainMeshControllers.Length + " instances: " + combineInstanceList.Count);

                // There will be no instances if the mesh/prefab has Combine 
                if (combineInstanceList.Count < 1) { combinedMeshesCreated = true; continue; }

                #region Type: Prefabs
                if (landscapeMeshList[m].usePrefab)
                {
                    if (landscapeMeshList[m].prefab != null)
                    {
                        GameObject newPrefabInstance = (GameObject)UnityEngine.Object.Instantiate(landscapeMeshList[m].prefab, Vector3.zero, Quaternion.identity);
                        if (newPrefabInstance != null)
                        {
                            MeshFilter[] prefabMeshFilters = newPrefabInstance.GetComponentsInChildren<MeshFilter>(true);
                            if (prefabMeshFilters != null)
                            {
                                if (prefabMeshFilters.Length > 0)
                                {
                                    // Create a separate object for each mesh in the prefab
                                    for (int mf = 0; mf < prefabMeshFilters.Length; mf++)
                                    {
                                        // Each mesh can have 2 ^ 16 (65536) less 1 verts
                                        int maxMeshesPerCombine = Mathf.FloorToInt(65535f / prefabMeshFilters[mf].sharedMesh.vertexCount);

                                        Matrix4x4 meshTRS = prefabMeshFilters[mf].transform.localToWorldMatrix;

                                        int meshCount = 0;
                                        while (meshCount < combineInstanceList.Count)
                                        {
                                            // Create a new GameObject for the combined mesh
                                            GameObject combinedMeshGameObject = new GameObject(landscapeMeshList[m].prefab.name + " Combined Prefab (" + prefabMeshFilters[mf].sharedMesh.name + ")");
                                            // Set the tag of the combined mesh
                                            combinedMeshGameObject.tag = "LB Combined Mesh";
                                            // Set the transform of the combined mesh
                                            combinedMeshGameObject.transform.parent = transform;
                                            combinedMeshGameObject.transform.localPosition = Vector3.zero; // -transform.position;
                                            combinedMeshGameObject.transform.localRotation = Quaternion.identity;

                                            // Add a MeshFilter and a MeshRenderer
                                            MeshFilter mFilter = combinedMeshGameObject.AddComponent<MeshFilter>();
                                            MeshRenderer mRenderer = combinedMeshGameObject.AddComponent<MeshRenderer>();

                                            // Calculate how many meshes are to be used in this CombineInstance arrays
                                            int meshesInCombinedMesh = Mathf.Min(combineInstanceList.Count - meshCount, maxMeshesPerCombine);

                                            // Create a list of CombineInstance arrays - one for each submesh in the mesh
                                            List<CombineInstance[]> submeshCombineList = new List<CombineInstance[]>();

                                            // Create a list of all the submeshes in the mesh within the prefab
                                            List<Mesh> subMeshList = new List<Mesh>();
                                            for (int sm = 0; sm < prefabMeshFilters[mf].sharedMesh.subMeshCount; sm++)
                                            {
                                                // Populate both of the arrays
                                                submeshCombineList.Add(new CombineInstance[meshesInCombinedMesh]);
                                                subMeshList.Add(LBMeshOperations.CreateMeshFromSubmesh(prefabMeshFilters[mf].sharedMesh, sm));
                                            }

                                            // Create a new CombineInstance array - this will be used to combine all the combined meshes
                                            // we create - which will contain all the meshes in each submesh
                                            CombineInstance[] combineInstance = new CombineInstance[submeshCombineList.Count];

                                            for (int sm = 0; sm < submeshCombineList.Count; sm++)
                                            {
                                                for (int i = 0; i < submeshCombineList[sm].Length; i++)
                                                {
                                                    // Populate the CombineInstance arrays for each submesh
                                                    submeshCombineList[sm][i].mesh = subMeshList[sm];
                                                    // * meshTRS is used so that we apply the transformation of the transform this mesh is
                                                    // attached to before applying any prefab scaling etc. transformations
                                                    submeshCombineList[sm][i].transform = combineInstanceList[meshCount + i].transform * meshTRS;
                                                }

                                                // Combine all the meshes into one mesh - these are all part of the same "submesh"
                                                combineInstance[sm].mesh = new Mesh();
                                                combineInstance[sm].mesh.CombineMeshes(submeshCombineList[sm], true, true);
                                                combineInstance[sm].transform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
                                            }

                                            // Increase the mesh count by the number of meshes in this combined mesh
                                            meshCount += meshesInCombinedMesh;

                                            // Create a new mesh and rename it
                                            mFilter.sharedMesh = new Mesh();
                                            mFilter.sharedMesh.name = prefabMeshFilters[mf].sharedMesh.name + " Combined Mesh";
                                            // Combine all the created "submesh" meshes into one mesh with defined separate submeshes
                                            mFilter.sharedMesh.CombineMeshes(combineInstance, false, true);

#if !UNITY_5_5_OR_NEWER
                                        // Optimize the mesh for better performance at runtime
                                        mFilter.sharedMesh.Optimize();
#endif

                                            // Get the renderer for this mesh from the prefab
                                            MeshRenderer prefabMeshRenderer = prefabMeshFilters[mf].GetComponent<MeshRenderer>();
                                            if (prefabMeshRenderer != null)
                                            {
                                                // Set materials from the prefab
                                                //mRenderer.materials = prefabMeshRenderer.sharedMaterials;
                                                mRenderer.sharedMaterials = prefabMeshRenderer.sharedMaterials;

                                                // Copy the renderer settings from the prefab
                                                mRenderer.enabled = prefabMeshRenderer.enabled;
                                                mRenderer.shadowCastingMode = prefabMeshRenderer.shadowCastingMode;

#if UNITY_5_4_OR_NEWER
                                                mRenderer.lightProbeUsage = prefabMeshRenderer.lightProbeUsage;
#else
                                            mRenderer.useLightProbes = prefabMeshRenderer.useLightProbes;
#endif

                                                mRenderer.reflectionProbeUsage = prefabMeshRenderer.reflectionProbeUsage;
                                                mRenderer.probeAnchor = prefabMeshRenderer.probeAnchor;
                                            }

                                            // Set the create GameObject to static
                                            combinedMeshGameObject.isStatic = true;

                                            if (landscapeMeshList[m].isCreateCollider)
                                            {
                                                combinedMeshGameObject.AddComponent<MeshCollider>();
                                            }

                                            combinedMeshesCreated = true;
                                        }
                                    }
                                }
                            }

                            DestroyImmediate(newPrefabInstance);
                        }
                    }
                }
                #endregion

                #region Type: Meshes
                else if (landscapeMeshList[m].mesh != null)
                {
                    // Each mesh can have 2 ^ 16 (65536) less 1 verts
                    int maxMeshesPerCombine = Mathf.FloorToInt(65535f / (float)landscapeMeshList[m].mesh.vertexCount);

                    int meshCount = 0;
                    while (meshCount < combineInstanceList.Count)
                    {
                        // Create a new GameObject for the combined mesh
                        GameObject combinedMeshGameObject = new GameObject(landscapeMeshList[m].mesh.name + " Combined Mesh");
                        // Set the tag of the combined mesh
                        combinedMeshGameObject.tag = "LB Combined Mesh";
                        // Set the transform of the combined mesh
                        combinedMeshGameObject.transform.parent = transform;
                        combinedMeshGameObject.transform.localPosition = Vector3.zero;
                        combinedMeshGameObject.transform.localRotation = Quaternion.identity;
                        // Add a MeshFilter and a MeshRenderer
                        MeshFilter mFilter = combinedMeshGameObject.AddComponent<MeshFilter>();
                        MeshRenderer mRenderer = combinedMeshGameObject.AddComponent<MeshRenderer>();

                        // Calculate how many meshes are to be used in this CombineInstance arrays
                        int meshesInCombinedMesh = Mathf.Min(combineInstanceList.Count - meshCount, maxMeshesPerCombine);
                        // Create a list of CombineInstance arrays - one for each submesh in the mesh
                        List<CombineInstance[]> submeshCombineList = new List<CombineInstance[]>();
                        // Create a list of all the submeshes in the mesh
                        List<Mesh> subMeshList = new List<Mesh>();
                        for (int sm = 0; sm < landscapeMeshList[m].mesh.subMeshCount; sm++)
                        {
                            // Populate both of the arrays
                            submeshCombineList.Add(new CombineInstance[meshesInCombinedMesh]);
                            subMeshList.Add(LBMeshOperations.CreateMeshFromSubmesh(landscapeMeshList[m].mesh, sm));
                        }

                        // Create a new CombineInstance array - this will be used to combine all the combined meshes
                        // we create - which will contain all the meshes in each submesh
                        CombineInstance[] combineInstance = new CombineInstance[submeshCombineList.Count];

                        //Debug.Log("LBLandscapeMeshController submeshes " + submeshCombineList.Count);

                        for (int sm = 0; sm < submeshCombineList.Count; sm++)
                        {
                            for (int i = 0; i < submeshCombineList[sm].Length; i++)
                            {
                                // Populate the CombineInstance arrays for each submesh
                                submeshCombineList[sm][i].mesh = subMeshList[sm];
                                submeshCombineList[sm][i].transform = combineInstanceList[meshCount + i].transform;
                            }
                            // Combine all the meshes into one mesh - these are all part of the same "submesh"
                            combineInstance[sm].mesh = new Mesh();
                            combineInstance[sm].mesh.CombineMeshes(submeshCombineList[sm], true, true);
                            combineInstance[sm].transform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
                        }

                        // Increase the mesh count by the number of meshes in this combined mesh
                        meshCount += meshesInCombinedMesh;

                        // Create a new mesh and rename it
                        mFilter.sharedMesh = new Mesh();
                        mFilter.sharedMesh.name = landscapeMeshList[m].mesh.name + " Combined Mesh";
                        // Combine all the created "submesh" meshes into one mesh with defined separate submeshes
                        mFilter.sharedMesh.CombineMeshes(combineInstance, false, true);

#if !UNITY_5_5_OR_NEWER
					// Optimize the mesh for better performance at runtime
					mFilter.sharedMesh.Optimize();
#endif

                        // Set the material array
                        mRenderer.materials = landscapeMeshList[m].materials.ToArray();

                        // Set the create GameObject to static
                        combinedMeshGameObject.isStatic = true;

                        if (landscapeMeshList[m].isCreateCollider)
                        {
                            combinedMeshGameObject.AddComponent<MeshCollider>();
                        }

                        combinedMeshesCreated = true;
                    }
                }
                #endregion

                #region Type: Unknown
                else
                {
                    Debug.Log("A combined mesh could not be created because the mesh of Mesh Type " + (m + 1) + " is null.");
                }
                #endregion

                combinedMeshIndex++;
            }

            for (int tmc = 0; tmc < terrainMeshControllers.Length; tmc++)
            {
                DestroyImmediate(terrainMeshControllers[tmc].gameObject);
            }

            terrainMeshControllers = null;
            return combinedMeshesCreated;

        }

        /// <summary>
        /// Builds a combined mesh from a list of Landscape Prefab meshes
        /// Returns true when combined meshes were created, else false.
        /// If there were no meshes, then will also return false.
        /// </summary>
        /// <param name="lbGroupParm"></param>
        /// <returns></returns>
        public bool BuildCombinedMeshes(LBGroupParameters lbGroupParm)
        {
            // When Group Members are created with the Groups system, each member can be enabled for CombineMesh. All of the instantiated
            // members can be combined into a single mesh. The prefabs are instantiate in the scene in LBLandscapeTerrain.PopulateTerrainWithGroups(..).
            // When they are initiated, their position is recorded and added to a CombineInstance list for the current GroupMember. This list is
            // then added (as an Array) to the LBTerrainMeshController.combineInstanceArrayList using LBTerrainMeshController.AddCombineInstanceArray().

            bool combinedMeshesCreated = false;
            string methodName = "LBLandscapeMeshControllers.BuildCombineMeshes (Groups)";

            #region Basic Validation - exit if there are issues
            // Get out fast if basic validation fails
            if (lbGroupParm == null) { Debug.LogWarning("ERROR: " + methodName + " - lbGroupParm is null. Please Report."); return false; }
            else if (lbGroupParm.landscape == null) { if (lbGroupParm.showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscape is null. Please Report."); } return false; }
            else if (lbGroupParm.landscape.lbGroupList == null) { if (lbGroupParm.showErrors) { Debug.LogWarning("ERROR: " + methodName + " - the list of groups cannot be null. Please Report."); } return false; }

            List<LBGroup> lbGroupList = LBGroup.GetActiveGroupList(lbGroupParm.landscape.lbGroupList);
            int numGroups = (lbGroupList == null ? 0 : lbGroupList.Count);

            // There should only ever be one TerrainMeshController for Groups (unlike Mesh/Prefabs tab were there is one per terrain)
            LBTerrainMeshController terrainMeshController = lbGroupParm.landscape.GetComponentInChildren<LBTerrainMeshController>();
            if (terrainMeshController == null) { if (lbGroupParm.showErrors && numGroups > 0) { Debug.LogWarning("ERROR: " + methodName + " could not find LBTerrainMeshController. Please Report."); } return false; }
            #endregion

            #region Variables

            LBGroup lbGroup = null;
            LBGroupMember lbGroupMember = null;
            string lbGroupMemberGUID = null;
            List<CombineInstance> combineInstanceList = new List<CombineInstance>();

            // Pre-declare variables for 4% performance improvement        
            int numCombineInstances = 0;
            int numSubmeshCombineList = 0;
            int numPrefabMeshFilters = 0;
            int meshesInCombinedMesh = 0;
            int maxMeshesPerCombine = 0;
            int meshCount = 0;
            int mf = 0;
            int sm = 0;
            Mesh sharedMesh;
            GameObject combinedMeshGameObject;

            LB_Extension.StringFast combinedMeshGameObjectName = new LB_Extension.StringFast(250);

            // To assist with progress, get the total number of prefab instances to be processed.
            int numCombineInstancesTotal = terrainMeshController.GetCombineInstanceOfMeshTotal();
            int numCombineInstancesCompleted = 0;
            float progress = 0f;
            float lastProgressUpdateTime = 0f;

            int numCombineInstArrays = terrainMeshController.GetCombineInstanceArrayListCount;

            //Debug.Log("numCombineInstArrays: " + numCombineInstArrays);

            #endregion

            // Loop through all of the CombineInstance Arrays in the order they were created
            for (int cmIdx = 0; cmIdx < numCombineInstArrays; cmIdx++)
            {
                combineInstanceList.Clear();
                lbGroupMemberGUID = string.Empty;

                terrainMeshController.GetCombineInstanceOfMesh(cmIdx, ref combineInstanceList, ref lbGroupMemberGUID);

                numCombineInstances = (combineInstanceList == null ? 0 : combineInstanceList.Count);
                //Debug.Log("numCombineInstances: " + numCombineInstances + " mbr:" + lbGroupMemberGUID);

                // If there are no combine instances move to the next one
                if (numCombineInstances < 1 || string.IsNullOrEmpty(lbGroupMemberGUID)) { combinedMeshesCreated = true; continue; }
                else
                {
                    // Find the parent Group for the GroupMember returned
                    lbGroup = lbGroupList.Find(grp => grp.groupMemberList.Exists(m => m.GUID == lbGroupMemberGUID));

                    if (lbGroup == null) { combinedMeshesCreated = true; continue; }
                    else
                    {
                        // Lookup the GroupMember
                        lbGroupMember = lbGroup.groupMemberList.Find(gmbr => gmbr.GUID == lbGroupMemberGUID);
                        if (lbGroupMember == null) { combinedMeshesCreated = true; continue; }
                        else
                        {
                            //Debug.Log("INFO: " + methodName + " Group " + lbGroup.groupName + " Member " + (lbGroup.groupMemberList.FindIndex(gmbr => gmbr.GUID == lbGroupMemberGUID) + 1).ToString("00") + " combineInstanceList:" + numCombineInstances);

                            combinedMeshGameObjectName.Set(lbGroup.groupName);
                            combinedMeshGameObjectName.Append(".");
                            combinedMeshGameObjectName.Append(lbGroupMember.prefab.name);
                            combinedMeshGameObjectName.Append(" Combined Prefab");

                            if (lbGroupMember.prefab == null)
                            {
                                Debug.LogWarning("ERROR: " + methodName + " - A combined mesh could not be created because the prefab of the Group (" + lbGroup.groupName + ") Member " + (lbGroup.groupMemberList.FindIndex(gmbr => gmbr.GUID == lbGroupMemberGUID) + 1).ToString("00") + " is null.");
                            }
                            else
                            {
                                GameObject newPrefabInstance = (GameObject)UnityEngine.Object.Instantiate(lbGroupMember.prefab, Vector3.zero, Quaternion.identity);
                                if (newPrefabInstance != null)
                                {
                                    MeshFilter[] prefabMeshFilters = newPrefabInstance.GetComponentsInChildren<MeshFilter>(true);

                                    numPrefabMeshFilters = (prefabMeshFilters == null ? 0 : prefabMeshFilters.Length);

                                    // Create a separate object for each mesh in the prefab
                                    for (mf = 0; mf < numPrefabMeshFilters; mf++)
                                    {
                                        sharedMesh = prefabMeshFilters[mf].sharedMesh;

                                        Matrix4x4 meshTRS = prefabMeshFilters[mf].transform.localToWorldMatrix;

                                        //Debug.Log(" Initialising Mesh at " + (Time.realtimeSinceStartup - tStart).ToString("0000.00") + " for " + sharedMesh.name);

                                        // Each mesh can have 2 ^ 16 (65536) less 1 verts
                                        maxMeshesPerCombine = Mathf.FloorToInt(65535f / sharedMesh.vertexCount);

                                        #region Create a list of all the submeshes in the mesh within the prefab.
                                        // Only need to do this once per unique sharedMesh in a prefab
                                        List<Mesh> subMeshList = new List<Mesh>();
                                        for (sm = 0; sm < sharedMesh.subMeshCount; sm++)
                                        {
                                            // NOTE: CreateMeshFromSubmesh is relatively slow, and the has the biggest impact of performance
                                            //Debug.Log(" Before CreateMeshFromSubmesh ( " + sm + "): " + (Time.realtimeSinceStartup - tStart).ToString("0000.00"));
                                            subMeshList.Add(LBMeshOperations.CreateMeshFromSubmesh(sharedMesh, sm));
                                            //Debug.Log("   After " + (Time.realtimeSinceStartup - tStart).ToString("0000.00"));
                                        }
                                        #endregion

                                        //Debug.Log(" Initialised Mesh at " + (Time.realtimeSinceStartup - tStart).ToString("0000.00") + " for " + sharedMesh.name);

                                        meshCount = 0;

                                        while (meshCount < numCombineInstances)
                                        {
                                            //Debug.Log("Start combineInstances mesh: " + meshCount + " " + (Time.realtimeSinceStartup - tStart).ToString("0000.00"));

                                            #region Setup Combined Mesh Gameobject
                                            // Create a new GameObject for the combined mesh
                                            combinedMeshGameObject = new GameObject(combinedMeshGameObjectName.ToString() + " (" + sharedMesh.name + ")");

                                            // Set the tag of the combined mesh
                                            combinedMeshGameObject.tag = "LB Combined Group Mesh";
                                            // Set the transform of the combined mesh
                                            combinedMeshGameObject.transform.parent = transform;
                                            // Can't use SetPositionAndRotation as that is worldspace position and rotation
                                            combinedMeshGameObject.transform.localPosition = Vector3.zero;
                                            combinedMeshGameObject.transform.localRotation = Quaternion.identity;

                                            // Add a MeshFilter and a MeshRenderer
                                            MeshFilter mFilter = combinedMeshGameObject.AddComponent<MeshFilter>();
                                            MeshRenderer mRenderer = combinedMeshGameObject.AddComponent<MeshRenderer>();
                                            #endregion

                                            // Calculate how many meshes are to be used in this CombineInstance arrays
                                            meshesInCombinedMesh = Mathf.Min(numCombineInstances - meshCount, maxMeshesPerCombine);

                                            #region Submeshes
                                            // Create a list of CombineInstance arrays - one for each submesh in the mesh
                                            List<CombineInstance[]> submeshCombineList = new List<CombineInstance[]>();

                                            //Debug.Log(" Before pop submeshCombineList, subMeshList: " + (Time.realtimeSinceStartup - tStart).ToString("0000.00") + " sharedMesh.subMeshCount: " + sharedMesh.subMeshCount);

                                            // Create a list of all the submeshes in the mesh within the prefab
                                            for (sm = 0; sm < sharedMesh.subMeshCount; sm++)
                                            {
                                                // Populate an array of the combined instances for each submesh in the current mesh of the prefab
                                                submeshCombineList.Add(new CombineInstance[meshesInCombinedMesh]);
                                            }

                                            numSubmeshCombineList = (submeshCombineList == null ? 0 : submeshCombineList.Count);

                                            // Create a new CombineInstance array - this will be used to combine all the combined meshes
                                            // we create - which will contain all the meshes in each submesh
                                            CombineInstance[] combineInstance = new CombineInstance[numSubmeshCombineList];

                                            for (sm = 0; sm < numSubmeshCombineList; sm++)
                                            {
                                                for (int i = 0; i < submeshCombineList[sm].Length; i++)
                                                {
                                                    // Populate the CombineInstance arrays for each submesh
                                                    submeshCombineList[sm][i].mesh = subMeshList[sm];
                                                    // * meshTRS is used so that we apply the transformation of the transform this mesh is
                                                    // attached to before applying any prefab scaling etc. transformations
                                                    submeshCombineList[sm][i].transform = combineInstanceList[meshCount + i].transform * meshTRS;
                                                }

                                                // Combine all the meshes into one mesh - these are all part of the same "submesh"
                                                combineInstance[sm].mesh = new Mesh();
                                                combineInstance[sm].mesh.CombineMeshes(submeshCombineList[sm], true, true);
                                                combineInstance[sm].transform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
                                            }
                                            #endregion

                                            // Increase the mesh count by the number of meshes in this combined mesh
                                            meshCount += meshesInCombinedMesh;

                                            #region Create Combined Mesh
                                            // Create a new mesh and rename it
                                            mFilter.sharedMesh = new Mesh();
                                            mFilter.sharedMesh.name = sharedMesh.name + " Combined Mesh";
                                            // Combine all the created "submesh" meshes into one mesh with defined separate submeshes
                                            mFilter.sharedMesh.CombineMeshes(combineInstance, false, true);

                                            // Get the renderer for this mesh from the prefab
                                            MeshRenderer prefabMeshRenderer = prefabMeshFilters[mf].GetComponent<MeshRenderer>();
                                            if (prefabMeshRenderer != null)
                                            {
                                                // Set materials from the prefab
                                                //mRenderer.materials = prefabMeshRenderer.sharedMaterials;
                                                mRenderer.sharedMaterials = prefabMeshRenderer.sharedMaterials;

                                                // Copy the renderer settings from the prefab
                                                mRenderer.enabled = prefabMeshRenderer.enabled;
                                                mRenderer.shadowCastingMode = prefabMeshRenderer.shadowCastingMode;

                                                #if UNITY_5_4_OR_NEWER
                                                mRenderer.lightProbeUsage = prefabMeshRenderer.lightProbeUsage;
                                                #else
                                                mRenderer.useLightProbes = prefabMeshRenderer.useLightProbes;
                                                #endif

                                                mRenderer.reflectionProbeUsage = prefabMeshRenderer.reflectionProbeUsage;
                                                mRenderer.probeAnchor = prefabMeshRenderer.probeAnchor;
                                            }
                                            #endregion

                                            // Set the create GameObject to static
                                            combinedMeshGameObject.isStatic = true;

                                            if (lbGroupMember.isCreateCollider)
                                            {
                                                combinedMeshGameObject.AddComponent<MeshCollider>();
                                            }

                                            combinedMeshesCreated = true;
                                            numCombineInstancesCompleted++;

                                            #region Progress
                                            // Only update progress bar every fixed amount of time
                                            if (lbGroupParm.showProgress && Time.realtimeSinceStartup > lastProgressUpdateTime + 1.0f)
                                            {
                                                // Call back to update the progress bar
                                                progress = (float)numCombineInstancesCompleted / (float)numCombineInstancesTotal;
                                                lastProgressUpdateTime = Time.realtimeSinceStartup;
                                                lbGroupParm.showProgressDelegate("Populating Landscape With Groups", "Finalising combining meshes " + numCombineInstancesCompleted + " of " + numCombineInstancesTotal, progress);
                                            }
                                            #endregion

                                            //Debug.Log(" Combined instance duration: " + (Time.realtimeSinceStartup - tStart).ToString("0000.00"));
                                        }

                                    }

                                    DestroyImmediate(newPrefabInstance);
                                }
                            }
                        }
                    }
                }
            }


            if (terrainMeshController != null)
            {
                DestroyImmediate(terrainMeshController.gameObject);
                terrainMeshController = null;
            }

            return combinedMeshesCreated;
        }

        /// <summary>
        /// Builds a combined mesh from a list of Landscape Prefab meshes
        /// Returns true when combined meshes were created, else false.
        /// If there were no meshes, then will also return false.
        /// </summary>
        /// <param name="lbGroupList"></param>
        /// <returns></returns>
        public bool BuildCombinedMeshesOld(LBGroupParameters lbGroupParm)
        {
            bool combinedMeshesCreated = false;
            string methodName = "LBLandscapeMeshControllers.BuildCombineMeshes (Groups)";
            //float tStart = Time.realtimeSinceStartup;

            // Get out fast if basic validation fails
            if (lbGroupParm == null) { Debug.LogWarning("ERROR: " + methodName + " - lbGroupParm is null. Please Report."); return false; }
            else if (lbGroupParm.landscape == null) { if (lbGroupParm.showErrors) { Debug.LogWarning("ERROR: " + methodName + " - landscape is null. Please Report."); } return false; }
            else if (lbGroupParm.landscape.lbGroupList == null) { if (lbGroupParm.showErrors) { Debug.LogWarning("ERROR: " + methodName + " - the list of groups cannot be null. Please Report."); } return false; }

            List<LBGroup> lbGroupList = LBGroup.GetActiveGroupList(lbGroupParm.landscape.lbGroupList);
            int numGroups = (lbGroupList == null ? 0 : lbGroupList.Count);

            // There should only ever be one TerrainMeshController for Groups (unlike Mesh/Prefabs tab were there is one per terrain)
            LBTerrainMeshController terrainMeshController = lbGroupParm.landscape.GetComponentInChildren<LBTerrainMeshController>();
            if (terrainMeshController == null) { if (lbGroupParm.showErrors && numGroups > 0) { Debug.LogWarning("ERROR: " + methodName + " could not find LBTerrainMeshController. Please Report."); } return false; }

            int combinedMeshIndex = 0;

            LBGroup lbGroup = null;
            LBGroupMember lbGroupMember = null;
            List<CombineInstance> combineInstanceList = new List<CombineInstance>();

            // Pre-declare variables for 4% performance improvement        
            int grpIdx = 0;
            int numCombineInstances = 0;
            int numSubmeshCombineList = 0;
            int numPrefabMeshFilters = 0;
            int gmbrIdx = 0;
            int meshesInCombinedMesh = 0;
            int maxMeshesPerCombine = 0;
            int meshCount = 0;
            int mf = 0;
            int sm = 0;
            Mesh sharedMesh;
            GameObject combinedMeshGameObject;
            string combinedMeshGameObjectName;

            // To assist with progress, get the total number of prefab instances to be processed.
            int numCombineInstancesTotal = terrainMeshController.GetCombineInstanceOfMeshTotal();
            int numCombineInstancesCompleted = 0;
            float progress = 0f;
            float lastProgressUpdateTime = 0f;

            // Iterate through all the groups
            for (grpIdx = 0; grpIdx < numGroups; grpIdx++)
            {
                lbGroup = lbGroupList[grpIdx];

                // Ignore inactive groups
                if (lbGroup.isDisabled || lbGroup.groupMemberList == null) { continue; }
                else if (lbGroup.groupMemberList.Count < 1) { continue; }
                else
                {
                    for (gmbrIdx = 0; gmbrIdx < lbGroup.groupMemberList.Count; gmbrIdx++)
                    {
                        lbGroupMember = lbGroup.groupMemberList[gmbrIdx];

                        // Ignore inactive members or ones without a prefab (mesh)
                        if (lbGroupMember.isDisabled || lbGroupMember.prefab == null || !lbGroupMember.isCombineMesh) { continue; }

                        combineInstanceList.Clear();

                        terrainMeshController.GetCombineInstanceOfMesh(combinedMeshIndex, ref combineInstanceList);

                        numCombineInstances = (combineInstanceList == null ? 0 : combineInstanceList.Count);
                        //Debug.Log("INFO: " + methodName + " Group " + (grpIdx + 1) + " Member " + (gmbrIdx + 1) + " combineInstanceList:" + numCombineInstances);

                        combinedMeshGameObjectName = lbGroup.groupName + "." + lbGroupMember.prefab.name + " Combined Prefab";

                        // There will be no instances if the prefab has Combine 
                        if (numCombineInstances < 1) { combinedMeshesCreated = true; continue; }

                        if (lbGroupMember.prefab == null)
                        {
                            Debug.LogWarning("ERROR: " + methodName + " - A combined mesh could not be created because the prefab of Group " + (grpIdx + 1) + " Member " + (gmbrIdx + 1) + " is null.");
                        }
                        else
                        {
                            GameObject newPrefabInstance = (GameObject)UnityEngine.Object.Instantiate(lbGroupMember.prefab, Vector3.zero, Quaternion.identity);
                            if (newPrefabInstance != null)
                            {
                                MeshFilter[] prefabMeshFilters = newPrefabInstance.GetComponentsInChildren<MeshFilter>(true);

                                numPrefabMeshFilters = (prefabMeshFilters == null ? 0 : prefabMeshFilters.Length);

                                if (numPrefabMeshFilters > 0)
                                {
                                    // Create a separate object for each mesh in the prefab
                                    for (mf = 0; mf < numPrefabMeshFilters; mf++)
                                    {
                                        sharedMesh = prefabMeshFilters[mf].sharedMesh;

                                        //Debug.Log(" Initialising Mesh at " + (Time.realtimeSinceStartup - tStart).ToString("0000.00") + " for " + sharedMesh.name);

                                        // Each mesh can have 2 ^ 16 (65536) less 1 verts
                                        maxMeshesPerCombine = Mathf.FloorToInt(65535f / sharedMesh.vertexCount);

                                        // Create a list of all the submeshes in the mesh within the prefab.
                                        // Only need to do this once per unique sharedMesh in a prefab
                                        List<Mesh> subMeshList = new List<Mesh>();
                                        for (sm = 0; sm < sharedMesh.subMeshCount; sm++)
                                        {
                                            // NOTE: CreateMeshFromSubmesh is relatively slow, and the has the biggest impact of performance
                                            //Debug.Log(" Before CreateMeshFromSubmesh ( " + sm + "): " + (Time.realtimeSinceStartup - tStart).ToString("0000.00"));
                                            subMeshList.Add(LBMeshOperations.CreateMeshFromSubmesh(sharedMesh, sm));
                                            //Debug.Log("   After " + (Time.realtimeSinceStartup - tStart).ToString("0000.00"));
                                        }

                                        //Debug.Log(" Initialised Mesh at " + (Time.realtimeSinceStartup - tStart).ToString("0000.00") + " for " + sharedMesh.name);

                                        meshCount = 0;
                                        while (meshCount < numCombineInstances)
                                        {
                                            //Debug.Log("Start combineInstances mesh: " + meshCount + " " + (Time.realtimeSinceStartup - tStart).ToString("0000.00"));

                                            // Create a new GameObject for the combined mesh
                                            combinedMeshGameObject = new GameObject(combinedMeshGameObjectName + " (" + sharedMesh.name + ")");

                                            // Set the tag of the combined mesh
                                            combinedMeshGameObject.tag = "LB Combined Group Mesh";
                                            // Set the transform of the combined mesh
                                            combinedMeshGameObject.transform.parent = transform;
                                            combinedMeshGameObject.transform.localPosition = Vector3.zero;
                                            combinedMeshGameObject.transform.localRotation = Quaternion.identity;

                                            // Add a MeshFilter and a MeshRenderer
                                            MeshFilter mFilter = combinedMeshGameObject.AddComponent<MeshFilter>();
                                            MeshRenderer mRenderer = combinedMeshGameObject.AddComponent<MeshRenderer>();

                                            // Calculate how many meshes are to be used in this CombineInstance arrays
                                            meshesInCombinedMesh = Mathf.Min(numCombineInstances - meshCount, maxMeshesPerCombine);

                                            // Create a list of CombineInstance arrays - one for each submesh in the mesh
                                            List<CombineInstance[]> submeshCombineList = new List<CombineInstance[]>();

                                            //Debug.Log(" Before pop submeshCombineList, subMeshList: " + (Time.realtimeSinceStartup - tStart).ToString("0000.00") + " sharedMesh.subMeshCount: " + sharedMesh.subMeshCount);

                                            // Create a list of all the submeshes in the mesh within the prefab
                                            //List <Mesh> subMeshList = new List<Mesh>();
                                            for (sm = 0; sm < sharedMesh.subMeshCount; sm++)
                                            {
                                                // Populate an array of the combined instances for each submesh in the current mesh of the prefab
                                                submeshCombineList.Add(new CombineInstance[meshesInCombinedMesh]);
                                            }

                                            numSubmeshCombineList = (submeshCombineList == null ? 0 : submeshCombineList.Count);

                                            // Create a new CombineInstance array - this will be used to combine all the combined meshes
                                            // we create - which will contain all the meshes in each submesh
                                            CombineInstance[] combineInstance = new CombineInstance[numSubmeshCombineList];

                                            for (sm = 0; sm < numSubmeshCombineList; sm++)
                                            {
                                                for (int i = 0; i < submeshCombineList[sm].Length; i++)
                                                {
                                                    // Populate the CombineInstance arrays for each submesh
                                                    submeshCombineList[sm][i].mesh = subMeshList[sm];
                                                    submeshCombineList[sm][i].transform = combineInstanceList[meshCount + i].transform;
                                                }

                                                // Combine all the meshes into one mesh - these are all part of the same "submesh"
                                                combineInstance[sm].mesh = new Mesh();
                                                combineInstance[sm].mesh.CombineMeshes(submeshCombineList[sm], true, true);
                                                combineInstance[sm].transform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
                                            }

                                            // Increase the mesh count by the number of meshes in this combined mesh
                                            meshCount += meshesInCombinedMesh;

                                            // Create a new mesh and rename it
                                            mFilter.sharedMesh = new Mesh();
                                            mFilter.sharedMesh.name = sharedMesh.name + " Combined Mesh";
                                            // Combine all the created "submesh" meshes into one mesh with defined separate submeshes
                                            mFilter.sharedMesh.CombineMeshes(combineInstance, false, true);

                                            #if !UNITY_5_5_OR_NEWER
                                            // Optimize the mesh for better performance at runtime
                                            mFilter.sharedMesh.Optimize();
                                            #endif

                                            // Get the renderer for this mesh from the prefab
                                            MeshRenderer prefabMeshRenderer = prefabMeshFilters[mf].GetComponent<MeshRenderer>();
                                            if (prefabMeshRenderer != null)
                                            {
                                                // Set materials from the prefab
                                                //mRenderer.materials = prefabMeshRenderer.sharedMaterials;
                                                mRenderer.sharedMaterials = prefabMeshRenderer.sharedMaterials;

                                                // Copy the renderer settings from the prefab
                                                mRenderer.enabled = prefabMeshRenderer.enabled;
                                                mRenderer.shadowCastingMode = prefabMeshRenderer.shadowCastingMode;

                                                #if UNITY_5_4_OR_NEWER
                                                mRenderer.lightProbeUsage = prefabMeshRenderer.lightProbeUsage;
                                                #else
                                                mRenderer.useLightProbes = prefabMeshRenderer.useLightProbes;
                                                #endif

                                                mRenderer.reflectionProbeUsage = prefabMeshRenderer.reflectionProbeUsage;
                                                mRenderer.probeAnchor = prefabMeshRenderer.probeAnchor;
                                            }

                                            // Set the create GameObject to static
                                            combinedMeshGameObject.isStatic = true;

                                            if (lbGroupMember.isCreateCollider)
                                            {
                                                combinedMeshGameObject.AddComponent<MeshCollider>();
                                            }

                                            combinedMeshesCreated = true;
                                            numCombineInstancesCompleted++;

                                            // Only update progress bar every fixed amount of time
                                            if (lbGroupParm.showProgress && Time.realtimeSinceStartup > lastProgressUpdateTime + 1.0f)
                                            {
                                                // Call back to update the progress bar
                                                progress = (float)numCombineInstancesCompleted / (float)numCombineInstancesTotal;
                                                lastProgressUpdateTime = Time.realtimeSinceStartup;
                                                lbGroupParm.showProgressDelegate("Populating Landscape With Groups", "Finalising combining meshes " + numCombineInstancesCompleted + " of " + numCombineInstancesTotal, progress);
                                            }

                                            //Debug.Log(" Combined instance duration: " + (Time.realtimeSinceStartup - tStart).ToString("0000.00"));
                                        }
                                    }
                                }

                                DestroyImmediate(newPrefabInstance);
                            }
                        }

                        combinedMeshIndex++;
                    } // End of group members
                } // End of group
            } // End of groups

            lbGroupMember = null;
            lbGroup = null;

            if (terrainMeshController != null)
            {
                DestroyImmediate(terrainMeshController.gameObject);
                terrainMeshController = null;
            }

            return combinedMeshesCreated;
        }


        /// <summary>
        /// Removes the existing combined meshes
        /// </summary>
        public void RemoveExistingCombinedMeshes(string tagName = "LB Combined Mesh")
        {
            MeshFilter[] mFilters = GetComponentsInChildren<MeshFilter>();
            for (int i = 0; i < mFilters.Length; i++)
            {
                if (mFilters[i].gameObject.tag == tagName)
                {
                    DestroyImmediate(mFilters[i].gameObject);
                }
            }
            mFilters = null;
        }
    }
}