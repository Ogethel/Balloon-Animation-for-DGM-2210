// Landscape Builder. Copyright (c) 2016-2019 SCSM Pty Ltd. All rights reserved.
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace LandscapeBuilder
{
    public class LBLandscapeOperations
    {
        // Landscape Operations Class

        #region Public Static Methods

        /// <summary>
        /// Create a list of LBMesh class instances for a landscape.
        /// Terrains are broken into chunks.
        /// One LBMesh is created for each terrain chunk.
        /// Optionally define a texture containing an outlined area.
        /// If meshPrefix is an empty string, the terrain name will be used
        /// See also LBStencilLayer.OutlineTexture and LBTextureOperations.TextureOutlineBounds()
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="meshPrefix"></param>
        /// <param name="outlineTex"></param>
        /// <param name="outlineTexBoundsN"></param>
        /// <param name="outlineColour"></param>
        /// <returns></returns>
        public static List<LBMesh> GetMeshDataFromLandscape(LBLandscape landscape, string meshPrefix, Texture2D outlineTex, Vector4 outlineTexBoundsN, UnityEngine.Color outlineColour)
        {
            List<LBMesh> lbMeshList = new List<LBMesh>();
            string methodName = "LBLandscapeOperations.CreateMeshFromLandscape";

            // Do some basic validation
            if (landscape == null) { Debug.LogWarning("ERROR: " + methodName + "  - LBLandscape cannot be null"); }
            else
            {
                //float csStart = Time.realtimeSinceStartup;

                Terrain[] landscapeTerrains = landscape.GetComponentsInChildren<Terrain>();

                if (landscapeTerrains == null) { Debug.LogWarning("ERROR: " + methodName + " - no terrains in the landscape " + landscape.name); }
                else if (landscapeTerrains.Length < 1) { Debug.LogWarning("ERROR: " + methodName + " - no terrains in the landscape " + landscape.name + ". Are they enabled?"); }
                else if (landscapeTerrains[0] == null) { Debug.LogWarning("ERROR: " + methodName + " - the first terrain is null in landscape " + landscape.name + ". Are they enabled?"); }
                else if (landscapeTerrains[0].terrainData == null) { Debug.LogWarning("ERROR: " + methodName + " - the first terrain data is null in landscape " + landscape.name + ". Are they enabled?"); }
                else
                {
                    #region Initial variables
                    int heightmapResolution = landscapeTerrains[0].terrainData.heightmapResolution;

                    int chunkSize = heightmapResolution == 65 ? 65 : 129;  // must be power 2 + 1
                    //int maxPoints = (65536 / 4) - 4;

                    // Declare outside loops for less garbage collection
                    Vector3 vertPosition, terrainPosition, terrainLandscapePos, terrainScale;
                    float vertTerrainNX, vertTerrainNZ;     // Vert terrain normalised positions
                    int chunkOffsetX = 0, chunkOffsetZ = 0;
                    int actualX = 0, actualZ = 0, vertCount = 0;
                    int numChunks = 0;
                    int terrainWidth = 0, terrainLength = 0;

                    // If the meshPrefix is an empty string, the terrain name will be used
                    string meshPrefixName = string.Empty;
                    #endregion

                    #region OutlineTex variables
                    // Do we wish to only create meshes within the area outlined within the outlineTex texture?
                    bool useOutlineTex = (outlineTex != null && outlineTex.width > 1 && outlineTexBoundsN != Vector4.zero);
                    int outlineTexWidth = 0, outlineTexHeight = 0;
                    float outlineTexWidthF = 0f, outlineTexHeightF = 0f;
                    Vector3 landscapePosition = landscape.transform.position;
                    Vector3 chunkMinN = Vector3.zero, chunkMaxN = Vector3.zero;
                    float vertLandscapeNX, vertLandscapeNZ; // Vert landscape normalised positions
                    int vertTexX, vertTexY; // Vert position in outlineTex texture
                    bool includeVert = true;
                    List<int> outlineVertsAdd = new List<int>();
                    UnityEngine.Color[] colourArray = new Color[0];

                    if (useOutlineTex)
                    {
                        outlineTexWidth = outlineTex.width;
                        outlineTexHeight = outlineTex.height;
                        outlineTexWidthF = (float)outlineTexWidth;
                        outlineTexHeightF = (float)outlineTexHeight;
                        colourArray = outlineTex.GetPixels(0);
                    }
                    #endregion

                    // Process each terrain in the landscape
                    for (int t = 0; t < landscapeTerrains.Length; t++)
                    {
                        TerrainData tData = landscapeTerrains[t].terrainData;
                        // From U2019.3+ terrain width and height must always be the same
                        terrainWidth = tData.heightmapResolution;
                        terrainLength = tData.heightmapResolution;

                        terrainPosition = landscapeTerrains[t].transform.position;
                        terrainScale = tData.heightmapScale;

                        // Get the mesh prefix name once per terrain
                        if (string.IsNullOrEmpty(meshPrefix))
                        {
                            meshPrefixName = landscapeTerrains[t].name;
                        }
                        // If not using the terrain name, add a terrain identifier
                        else { meshPrefixName = meshPrefix + string.Format("_T{0:0000}", t); }

                        // Get the position, in metres, from the bottom left of the landscape.
                        terrainLandscapePos = terrainPosition - landscapePosition;

                        #region Check terrain is within OutlineTex if required
                        // If filtering by outline Texture, check to see if this terrain is within bounds
                        if (useOutlineTex)
                        {
                            // Get normalised position of this terrain in the landscape
                            Vector4 terrainBoundsN = new Vector4(terrainLandscapePos.x / landscape.size.x, terrainLandscapePos.z / landscape.size.y, (terrainLandscapePos.x + tData.size.x) / landscape.size.x, (terrainLandscapePos.z + tData.size.z) / landscape.size.y);

                            // Is this terrain outside the outlined area in the texture?
                            // terrain xMin > outline xMax OR terrain xMax < outline xMin OR terrain yMin > outline yMax OR terrain yMax < outline yMin
                            if (terrainBoundsN.x > outlineTexBoundsN.z || terrainBoundsN.z < outlineTexBoundsN.x || terrainBoundsN.y > outlineTexBoundsN.w || terrainBoundsN.w < outlineTexBoundsN.y)
                            {
                                continue;
                            }
                        }
                        #endregion

                        numChunks = ((terrainWidth - 1) / (chunkSize - 1));

                        //Debug.Log("INFO: " + methodName + " " + terrainWidth + " chunkSize:" + chunkSize + " numChunks:" + numChunks * numChunks);

                        float[,] heightMap = tData.GetHeights(0, 0, terrainWidth, terrainLength);

                        // Create a chunk (mesh) for each row in the X direction
                        for (int chunkIndexX = 0; chunkIndexX < numChunks; chunkIndexX++)
                        {
                            chunkOffsetX = chunkIndexX * (chunkSize - 1);

                            // Create a chunk (mesh) for each column in the Z direction
                            for (int chunkIndexZ = 0; chunkIndexZ < numChunks; chunkIndexZ++)
                            {
                                chunkOffsetZ = chunkIndexZ * (chunkSize - 1);

                                #region Determine if this chunk is within the outline area
                                if (useOutlineTex)
                                {
                                    // Y scaling is ignored as we only need the 2D values
                                    chunkMinN = Vector3.Scale(terrainScale, new Vector3(chunkOffsetX, 0, chunkOffsetZ)) + terrainLandscapePos;
                                    chunkMaxN = Vector3.Scale(terrainScale, new Vector3(chunkOffsetX + (chunkSize - 1), 0, chunkOffsetZ + (chunkSize - 1))) + terrainLandscapePos;

                                    // Normalise the chunk boundaries (ignore height)
                                    chunkMinN.x = chunkMinN.x / landscape.size.x;
                                    chunkMinN.z = chunkMinN.z / landscape.size.y;
                                    chunkMaxN.x = chunkMaxN.x / landscape.size.x;
                                    chunkMaxN.z = chunkMaxN.z / landscape.size.y;

                                    //Debug.Log("[DEBUG] Mesh Chunk: " + meshPrefixName + "_Mesh" + string.Format("{0:00}:{1:00}", chunkIndexX, chunkIndexZ) + " from " + chunkMinN + " to " + chunkMaxN);

                                    // Chunk position is 3D. Outline boundaries are 2D.
                                    // Chunk xMin > outline xMax OR Chunk xMax < outline xMin OR Chunk zMin > outline yMax OR Chunk zMax < outline yMin
                                    if (chunkMinN.x > outlineTexBoundsN.z || chunkMaxN.x < outlineTexBoundsN.x || chunkMinN.z > outlineTexBoundsN.w || chunkMaxN.z < outlineTexBoundsN.y)
                                    {
                                        continue;
                                    }
                                    else { outlineVertsAdd.Clear(); }
                                }
                                #endregion

                                LBMesh lbMesh = new LBMesh();
                                lbMesh.title = meshPrefixName + "_Mesh" + string.Format("{0:00}:{1:00}", chunkIndexX, chunkIndexZ);

                                // reset variables
                                actualX = 0;
                                actualZ = 0;
                                vertCount = 0;

                                // Loop through all the x,z heightmap coordinates in this chunk of the terrain
                                // Triangle numbers are zero based (triX,triZ) while the position in the terrain
                                // is offset by the starting location of the chunk.
                                for (int x = 0; x < chunkSize; x++)
                                {
                                    for (int z = 0; z < chunkSize; z++)
                                    {
                                        // Offset the verts to be correct position for this chunk
                                        actualX = x + chunkOffsetX;
                                        actualZ = z + chunkOffsetZ;

                                        // Top right of quad
                                        // Scale up the normalised heightmap data by the xyz size of the terrain, then add the terrain worldspace position
                                        vertPosition = Vector3.Scale(terrainScale, new Vector3(actualX, heightMap[actualZ, actualX], actualZ)) + terrainPosition;

                                        #region Check if Vert is within outline area
                                        if (useOutlineTex)
                                        {
                                            // Normalised vert position in landscape
                                            vertLandscapeNX = (vertPosition.x - landscape.start.x) / landscape.size.x;
                                            vertLandscapeNZ = (vertPosition.z - landscape.start.z) / landscape.size.y;

                                            // Check to see if this terrain vert is within the bounds outlined area supplied.
                                            // This is a fast way of eliminating everything outside a rectangular bounding box
                                            if (vertLandscapeNX < outlineTexBoundsN.x || vertLandscapeNX > outlineTexBoundsN.z || vertLandscapeNZ < outlineTexBoundsN.y || vertLandscapeNZ > outlineTexBoundsN.w)
                                            {
                                                // ignore this vert
                                                includeVert = false;
                                            }
                                            else
                                            {
                                                // Sample the texture to see if this point is inside the area or outside
                                                int numCrossed = 0;
                                                //vertTexX = (int)System.Math.Round(vertLandscapeNX * outlineTexWidthF);
                                                //vertTexY = (int)System.Math.Round(vertLandscapeNZ * outlineTexHeightF);

                                                // truncate - take the floor value (USE THIS FOR NOW)
                                                // Does not seem to be any better/worse than using Math.Round()
                                                vertTexX = (int)(vertLandscapeNX * outlineTexWidthF);
                                                vertTexY = (int)(vertLandscapeNZ * outlineTexHeightF);

                                                // cast a "ray" or draw a line from left of the outline area to the
                                                // current vert.
                                                bool isLastMatch = false; // was the last pixel a match?
                                                //int lineWidth = 0; // the width of the current line being intersected.
                                                for (int texX = 0; texX < outlineTexWidth; texX++)
                                                {
                                                    if (texX >= vertTexX) { break; }

                                                    // NOTE: It is faster to calc YOffset, rather than set it to a variable outside the loop
                                                    if (colourArray[(vertTexY * outlineTexHeight) + texX] == outlineColour)
                                                    {
                                                        // If the pixel to the left wasn't a match we are beginning to cross a solid line
                                                        // in the texture. So increment numCrossed
                                                        if (!isLastMatch) { numCrossed++; }

                                                        // Sometimes there is a horizontal line of coloured pixels in the texture.
                                                        // These can skew results.  
                                                        //lineWidth++;
                                                        isLastMatch = true;
                                                    }
                                                    else
                                                    {
                                                        // Reset intersection line width
                                                        //lineWidth = 0;
                                                        isLastMatch = false;
                                                    }
                                                }

                                                // Is the number of times crossed odd?
                                                if ((numCrossed & 1) == 1)
                                                {
                                                    includeVert = true;

                                                    // remember vert index in list
                                                    outlineVertsAdd.Add(vertCount);
                                                }
                                                // ORGINAL CODE
                                                //else { includeVert = false; }
                                                else
                                                {
                                                    // Perform a y-axis ray to verify outcome
                                                    // Complex shapes can sometimes miss data that should be included
                                                    // This helps to pick up those missing blocks.
                                                    // NOTE: It may also add some blocks that should not be there.
                                                    numCrossed = 0;
                                                    isLastMatch = false;
                                                    for (int texY = 0; texY < outlineTexHeight; texY++)
                                                    {
                                                        if (texY >= vertTexY) { break; }

                                                        if (colourArray[(texY * outlineTexHeight) + vertTexX] == outlineColour)
                                                        {
                                                            if (!isLastMatch) { numCrossed++; }
                                                            isLastMatch = true;
                                                        }
                                                        else { isLastMatch = false; }
                                                    }

                                                    if ((numCrossed & 1) == 1)
                                                    {
                                                        includeVert = true;

                                                        // remember vert index in list
                                                        outlineVertsAdd.Add(vertCount);
                                                    }
                                                    else { includeVert = false; }
                                                }
                                            }
                                        }
                                        #endregion

                                        lbMesh.verts.Add(vertPosition);

                                        #region Add triangle verts, uvs and normals
                                        if (includeVert)
                                        {
                                            // Update extents
                                            if (vertPosition.x < lbMesh.minExtent.x) { lbMesh.minExtent.x = vertPosition.x; }
                                            if (vertPosition.y < lbMesh.minExtent.y) { lbMesh.minExtent.y = vertPosition.y; }
                                            if (vertPosition.z < lbMesh.minExtent.z) { lbMesh.minExtent.z = vertPosition.z; }
                                            if (vertPosition.x > lbMesh.maxExtent.x) { lbMesh.maxExtent.x = vertPosition.x; }
                                            if (vertPosition.y > lbMesh.maxExtent.y) { lbMesh.maxExtent.y = vertPosition.y; }
                                            if (vertPosition.z > lbMesh.maxExtent.z) { lbMesh.maxExtent.z = vertPosition.z; }

                                            // Normalised vert position in terrain
                                            vertTerrainNX = (float)actualX / ((float)terrainWidth - 1f);
                                            vertTerrainNZ = (float)actualZ / ((float)terrainLength - 1f);

                                            lbMesh.uvs.Add(new Vector2(vertTerrainNX, vertTerrainNZ));
                                            lbMesh.normals.Add(tData.GetInterpolatedNormal(vertTerrainNX, vertTerrainNZ));

                                            // Add the two triangles for the quad
                                            // Not required if on left or bottom edges of the terrain
                                            if (x < chunkSize - 1 && z < chunkSize - 1)
                                            {
                                                // Bottom (left) triangle
                                                // Bottom left of quad
                                                lbMesh.triangles.Add(vertCount);
                                                // Bottom right of quad
                                                lbMesh.triangles.Add(vertCount + 1);
                                                // Top right of quad
                                                lbMesh.triangles.Add(vertCount + chunkSize + 1);

                                                // Top (right) triangle
                                                // Top left of quad
                                                lbMesh.triangles.Add(vertCount + chunkSize);
                                                // Bottom left of quad
                                                lbMesh.triangles.Add(vertCount);
                                                // Top right of quad
                                                lbMesh.triangles.Add(vertCount + chunkSize + 1);
                                            }
                                        }
                                        #endregion

                                        // Increment the vert count
                                        vertCount++;
                                    }
                                }

                                #region Remove unwanted verts and update triangle if required
                                if (useOutlineTex)
                                {
                                    int numVerts = (lbMesh.verts == null ? 0 : lbMesh.verts.Count);
                                    //int numVertsAdded = (outlineVertsAdd == null ? 0 : outlineVertsAdd.Count);
                                    int numTriangles = (lbMesh.triangles == null ? 0 : lbMesh.triangles.Count);

                                    // Loop backwards through the triangle verts
                                    for (int triIdx = numTriangles - 3; triIdx >=0; triIdx -= 3)
                                    {
                                        // Get next set of triangle vert indexes
                                        int triVert1 = lbMesh.triangles[triIdx];
                                        int triVert2 = lbMesh.triangles[triIdx+1];
                                        int triVert3 = lbMesh.triangles[triIdx+2];

                                        // Remove any triangles that where not all the verts have been included
                                        if (!outlineVertsAdd.Contains(triVert1) || !outlineVertsAdd.Contains(triVert2) || !outlineVertsAdd.Contains(triVert3))
                                        {
                                            lbMesh.triangles.RemoveAt(triIdx + 2);
                                            lbMesh.triangles.RemoveAt(triIdx + 1);
                                            lbMesh.triangles.RemoveAt(triIdx);
                                        }                                
                                    }

                                    // Remove all the unwanted verts
                                    for (int vIdx = numVerts - 1; vIdx >= 0; vIdx--)
                                    {
                                        if (!outlineVertsAdd.Contains(vIdx)) { lbMesh.verts.RemoveAt(vIdx); }
                                    }

                                    // update number of verts and triangles
                                    numVerts = (lbMesh.verts == null ? 0 : lbMesh.verts.Count);
                                    numTriangles = (lbMesh.triangles == null ? 0 : lbMesh.triangles.Count);

                                    // Update triangles with new vert indexes
                                    for (int triIdx = 0; triIdx < numTriangles; triIdx++)
                                    {
                                        // Get the original vert index for this triangle vert
                                        int triVertIdx = lbMesh.triangles[triIdx];
                                        int triNewVertIdx = outlineVertsAdd.FindIndex(v => v == triVertIdx);
                                        // new vert idx should never be -1 but just in case set to 0 to avoid out of bounds issue
                                        lbMesh.triangles[triIdx] = triNewVertIdx < 0 ? 0 : triNewVertIdx;
                                    }
                                }
                                #endregion

                                //Debug.Log("INFO: " + methodName + " Mesh verts:" + lbMesh.verts.Count + " tris:" + lbMesh.triangles.Count);
                                lbMeshList.Add(lbMesh);
                            }
                        }
                    }
                }

                //Debug.Log("[DEBUG] GetMeshDataFromLandscape " + System.DateTime.Now.ToLongTimeString() + " duration: " + (Time.realtimeSinceStartup - csStart).ToString("0.0000") + "s");

            }

            return lbMeshList;
        }

        /// <summary>
        /// Returns a list of textures of the splatmaps from a landscape. There can be up to 3
        /// textures per terrain. The texture.name includes the name of the terrain and _splatn
        /// where "n" is the number of the splatmap 0, 1 or 2.
        /// See also LBLandscapeTerrain.GetTextureFromSplatmap()
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static List<Texture2D> GetSplatmapsFromLandscape(LBLandscape landscape, bool showErrors)
        {
            List<Texture2D> textureList = new List<Texture2D>();

            if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR: LBLandscapeOperations.GetSplatmapsFromLandscape - landscape cannot be null"); } }
            else
            {
                Terrain[] landscapeTerrains = landscape.gameObject.GetComponentsInChildren<Terrain>();
                if (landscapeTerrains == null) { if (showErrors) { Debug.LogWarning("ERROR: LBLandscapeOperations.GetSplatmapsFromLandscape - could not get terrains"); } }
                else if (landscapeTerrains.Length == 0) { if (showErrors) { Debug.LogWarning("ERROR: LBLandscapeOperations.GetSplatmapsFromLandscape - no terrains in landscape"); } }
                else
                {
                    for (int t = 0; t < landscapeTerrains.Length; t++)
                    {
                        Terrain terrain = landscapeTerrains[t];

                        int numAlphaMapLayers = terrain.terrainData.alphamapLayers;

                        for (int splatIndex = 0, textureSlots = 0; splatIndex < 3 && textureSlots < numAlphaMapLayers; splatIndex++, textureSlots += 4)
                        {
                            Texture2D splatTexture = LBLandscapeTerrain.GetTextureFromSplatmap(landscape, terrain, splatIndex, true);
                            if (splatTexture != null) { textureList.Add(splatTexture); }
                            else { break; } // If there was an error, don't continue
                        }
                    }
                }
            }

            return textureList;
        }

        /// <summary>
        /// Create a list of meshes under a supplied parent transform. This will should itself be a
        /// child of the landscape. Typically, this is called for creating navmesh's for a stencil
        /// layer. This method will add a MeshRenderer to each mesh.
        /// To generate a navmesh from the meshes, ensure isStatic = true.
        /// NOTE: To create terrain-sized meshes, use CreateLandscapeMesh(..).
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="lbMeshList"></param>
        /// <param name="tfmParent"></param>
        /// <param name="meshParentName"></param>
        /// <param name="isStatic"></param>
        /// <returns></returns>
        public static Transform CreateMeshList(LBLandscape landscape, List<LBMesh> lbMeshList, Transform tfmParent, string meshParentName, bool isStatic)
        {
            Transform newMeshTransform = null;
            string methodName = "LBLandscapeOperations.CreateLandscapeMesh";

            // Basic validation
            if (landscape == null) { Debug.LogWarning("ERROR: " + methodName +" - landscape cannot be null"); }
            else if (lbMeshList == null) { Debug.LogWarning("ERROR: " + methodName + " - lbMeshList cannot be null"); }
            else if (lbMeshList.Count == 0) { Debug.Log("INFO: " + methodName + " - there are no LBMesh objects to process"); }
            else
            {
                // Find or Create a parent gameobject under the trfmParent supplied.
                Transform meshParentTrfm = tfmParent.Find(meshParentName);

                // If it exists, delete it
                if (meshParentTrfm != null)
                {
                    #if UNITY_EDITOR
                    GameObject.DestroyImmediate(meshParentTrfm.gameObject);
                    #else
                    GameObject.Destroy(meshParentTrfm.gameObject);
                    #endif
                }

                // Create a new parent
                GameObject meshParentGameObject = new GameObject(meshParentName);
                if (meshParentGameObject != null)
                {
                    meshParentTrfm = meshParentGameObject.transform;
                    meshParentTrfm.parent = tfmParent;

                    Transform meshTransform;
                    bool isSuccessful = false;

                    foreach (LBMesh lbMesh in lbMeshList)
                    {
                        GameObject meshGameObject = new GameObject(lbMesh.title);
                        if (meshGameObject != null)
                        {
                            meshTransform = meshGameObject.transform;
                            meshTransform.parent = meshParentTrfm.transform;

                            #if UNITY_EDITOR
                            meshGameObject.isStatic = isStatic;
                            #endif

                            if (lbMesh.mesh == null) { lbMesh.mesh = new Mesh(); }

                            if (lbMesh.mesh == null) { Debug.LogWarning("ERROR: " + methodName + " - could not create new Mesh for " + lbMesh.title); }
                            else
                            {
                                lbMesh.mesh.name = lbMesh.title;

                                MeshFilter meshFilter = meshTransform.gameObject.AddComponent<MeshFilter>();
                                MeshRenderer meshRenderer = meshTransform.gameObject.AddComponent<MeshRenderer>();
                                if (meshFilter == null) { Debug.LogWarning("ERROR: " + methodName + " - Could not add MeshFilter to " + lbMesh.title); }
                                else if (meshRenderer == null) { Debug.LogWarning("ERROR: " + methodName + " - Could not add MeshRenderer to " + lbMesh.title); }
                                else
                                {
                                    // Assign the mesh
                                    meshFilter.sharedMesh = lbMesh.mesh;

                                    // Assign verts, triangle to new Unity Mesh and recalc bounds
                                    isSuccessful = lbMesh.UpdateMesh(false, true);
                                }
                            }
                        }
                    }

                    // When all is well, set the transform to be returned
                    if (isSuccessful) { newMeshTransform = meshParentTrfm.transform; }
                }
            }

            return newMeshTransform;
        }

        /// <summary>
        /// Create meshes parented to the landscape, given a list of LBMesh instances that contain the
        /// mesh data from a landscape.
        /// Typically called after GetMeshDataFromLandscape().
        /// isStatic has no effect at runtime.
        /// Parmeter saveMeshToProjectFolder is only available in EDITOR
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="lbMeshList"></param>
        /// <param name="meshParentName"></param>
        /// <param name="addColliders"></param>
        /// <param name="includeOcclusionAreas"></param>
        /// <param name="isStatic"></param>
        /// <param name="includeSplatmaps"></param>
        /// <param name="useMegaSplat"></param>
        /// <param name="saveMeshToProjectFolder"></param>
        /// <returns></returns>
        public static Transform CreateLandscapeMesh(LBLandscape landscape, List<LBMesh> lbMeshList, string meshParentName, bool addColliders, bool includeOcclusionAreas, bool isStatic, bool includeSplatmaps, bool useMegaSplat, bool saveMeshToProjectFolder)
        {
            Transform newMeshTransform = null;

            // Basic validation
            if (landscape == null) { Debug.LogWarning("ERROR: LBLandscapeOperations.CreateLandscapeMesh - landscape cannot be null"); }
            else if (lbMeshList == null) { Debug.LogWarning("ERROR: LBLandscapeOperations.CreateLandscapeMesh - lbMeshList cannot be null"); }
            else if (lbMeshList.Count == 0) { Debug.Log("INFO: LBLandscapeOperations.CreateLandscapeMesh - there are no LBMesh objects to process"); }
            else
            {
                if (string.IsNullOrEmpty(meshParentName)) { meshParentName = landscape.name + "_Meshes"; }

                // Find or Create a parent gameobject with one child gameobject for each terrain (mesh).
                Transform meshParentTrfm = landscape.transform.Find(meshParentName);

                // If the parent mesh GameObject doesn't exist, attempt to create it.
                if (meshParentTrfm == null)
                {
                    GameObject meshParentGameObject = new GameObject(meshParentName);
                    if (meshParentGameObject != null) { meshParentTrfm = meshParentGameObject.transform; }
                }
                else
                {
                    // If meshes were previously generated, determine if the number created will be the same

                    // Get a list of all child transforms (which includes the parent transform)
                    Transform[] childTfrms = meshParentTrfm.GetComponentsInChildren<Transform>();
                    if (childTfrms != null)
                    {
                        // If the existing list of child transforms doesn't match, user has probably
                        // changed the terrain heightmap resolution
                        if (lbMeshList.Count != childTfrms.Length - 1)
                        {
                            // Loop backwards through the list and delete them
                            for (int t = childTfrms.Length - 1; t >= 0; t--)
                            {
                                if (childTfrms[t] != meshParentTrfm) { GameObject.DestroyImmediate(childTfrms[t].gameObject); }
                            }
                        }
                    }
                }

                if (meshParentTrfm != null)
                {
#if UNITY_EDITOR
                    meshParentTrfm.gameObject.isStatic = isStatic;
#endif

                    meshParentTrfm.parent = landscape.gameObject.transform;

                    bool isSuccessful = false;

                    List<Texture2D> splatTextureList = null;
                    List<LBTerrainTexture> lbTerrainTexture = null;
                    int numActiveTextures = 0;
                    Shader lbMeshTerrainShader = null;
                    Material meshMaterial = null;
                    float terrainWidth = landscape.GetLandscapeTerrainWidth();

                    bool isOkToCreateMeshes = true;

                    if (useMegaSplat)
                    {
#if __MEGASPLAT__
                        // There is a current dependency on the Unity terrains using a MegaSplat material and shader
                        // Although this is not strickly a limitation it is the easilest way to convert the Unity alphamaps into mesh vert properties

                        string msgNeedMegaSplat = "The landscape first needs to be configured to use the MegaSplat material type.";

                        isOkToCreateMeshes = false;

                        if (landscape.GetLandscapeTerrainMaterialType() != Terrain.MaterialType.Custom)
                        {
                            Debug.LogWarning("WARNING: LBLandscapeOperations.CreateLandscapeMesh - " + msgNeedMegaSplat);
                        }
                        else
                        {
                            Material terrainCustomMaterial = landscape.GetLandscapeTerrainCustomMaterial();

                            // Validate the material and shader
                            if (terrainCustomMaterial == null) { Debug.LogWarning("WARNING: LBLandscapeOperations.CreateLandscapeMesh - No MegaSplat material on terrains. " + msgNeedMegaSplat); }
                            else if (!terrainCustomMaterial.shader.name.Contains("MegaSplat")) { Debug.LogWarning("WARNING: LBLandscapeOperations.CreateLandscapeMesh - The terrain shader is not a MegaSplat shader. " + msgNeedMegaSplat); }
                            else
                            {
                                // Create a new shader and material if they don't exist
#if UNITY_EDITOR
                                bool autoClosePainter = LBLandscape.GetMegaSplatAutoClosePainter();
#else
                            bool autoClosePainter = false;
#endif
                                meshMaterial = LBIntegration.MegaSplatConfigureMaterial(landscape, autoClosePainter, true, true);
                                if (meshMaterial == null) { Debug.LogWarning("ERROR: LBLandscapeOperations.CreateLandscapeMesh - could not create the MegaSplat shader and material"); }
                                else
                                {
                                    UnityEngine.Object objTexArrayConfig = LBIntegration.MegaSplatGetTextureArrayConfig(landscape.name, true);

                                    isOkToCreateMeshes = LBIntegration.MegaSplatUpdateShaderSplat(landscape, "Albedo", true, objTexArrayConfig, true);
                                }
                            }
                        }
#endif
                    }

                    if (includeSplatmaps)
                    {
                        splatTextureList = GetSplatmapsFromLandscape(landscape, true);
                        lbMeshTerrainShader = (Shader)Resources.Load("LBMeshTerrain", typeof(Shader));
                        #if UNITY_EDITOR
                        if (lbMeshTerrainShader == null)
                        {
                            lbMeshTerrainShader = LBEditorHelper.GetAsset<Shader>("LandscapeBuilder/Shaders", "LBMeshTerrain.shader");
                        }
                        #endif

                        if (lbMeshTerrainShader == null)
                        {
                            string errMsg = "ERROR: LBLandscapeOperations.CreateLandscapeMesh - could not find LBMeshTerrain shader.";
                            errMsg += " It seems to be missing from Assets/LandscapeBuilder/Shaders/Resources folder. You may need to move it into this folder.";
                            Debug.LogWarning(errMsg);
                            isOkToCreateMeshes = false;
                        }
                        else
                        {
                            // Assume that the terrain splatmaps were created with current list of LBTextures in the landscape
                            // Remove disabled texture from the list. Create new list so we don't affect original in LBLandscape
                            lbTerrainTexture = new List<LBTerrainTexture>(landscape.terrainTexturesList);
                            for (int i = lbTerrainTexture.Count - 1; i >= 0; i--)
                            {
                                if (lbTerrainTexture[i].isDisabled) { lbTerrainTexture.Remove(lbTerrainTexture[i]); }
                            }

                            numActiveTextures = lbTerrainTexture.Count;
                        }
                    }

                    if (isOkToCreateMeshes)
                    {
#if UNITY_EDITOR
                        string meshProjectFolder = "Assets/LandscapeBuilder/Meshes/" + landscape.name;
                        int displayDialogComplexInt = 0;
#endif

                        if (saveMeshToProjectFolder)
                        {
#if UNITY_EDITOR
                            // Save Mesh to Project
                            // Create Meshes parent folder if it doesn't already exist
                            LBEditorHelper.CheckFolder("Assets/LandscapeBuilder/Meshes");
                            // Create Meshes landscape folder if it doesn't already exist 
                            LBEditorHelper.CheckFolder(meshProjectFolder);
#endif
                        }

                        foreach (LBMesh lbMesh in lbMeshList)
                        {
                            // Find or create a child gameobject for each terrain mesh
                            Transform meshTransform = meshParentTrfm.Find(lbMesh.title);

                            // If the terrain mesh GameObject doesn't exist, attempt to create it.
                            if (meshTransform == null)
                            {
                                GameObject meshGameObject = new GameObject(lbMesh.title);
                                if (meshGameObject != null) { meshTransform = meshGameObject.transform; }
                            }

                            if (meshTransform != null)
                            {
#if UNITY_EDITOR
                                meshTransform.gameObject.isStatic = isStatic;

                                // If all static flags are not set, and including Occlusion, then set the correct flags.
                                if (!isStatic && includeOcclusionAreas)
                                {
                                    UnityEditor.StaticEditorFlags staticFlags = UnityEditor.StaticEditorFlags.OccludeeStatic | UnityEditor.StaticEditorFlags.OccluderStatic;
                                    UnityEditor.GameObjectUtility.SetStaticEditorFlags(meshTransform.gameObject, staticFlags);
                                }

#endif

                                meshTransform.parent = meshParentTrfm.transform;

                                if (lbMesh.mesh == null) { lbMesh.mesh = new Mesh(); }

                                if (lbMesh.mesh == null) { Debug.LogWarning("ERROR: LBLandscapeOperations.CreateLandscapeMesh - could not create new Mesh for " + lbMesh.title); }
                                else
                                {
                                    lbMesh.mesh.name = lbMesh.title;

                                    // Check if there is already a mesh collider
                                    MeshCollider meshCollider = meshTransform.GetComponent<MeshCollider>();
                                    // If a collider exists, remove it, because it doesn't automatically get update if we first disable it.
                                    // Updating a mesh collider may be slow, so just re-add it later if required
                                    if (meshCollider != null) { GameObject.DestroyImmediate(meshCollider); }

                                    // If the Mesh Filter doesn't exist, add it
                                    MeshFilter meshFilter = meshTransform.GetComponent<MeshFilter>();
                                    if (meshFilter == null) { meshFilter = meshTransform.gameObject.AddComponent<MeshFilter>(); }

                                    // If the Mesh Renderer doesn't exist, add it
                                    MeshRenderer meshRenderer = meshTransform.GetComponent<MeshRenderer>();
                                    if (meshRenderer == null) { meshRenderer = meshTransform.gameObject.AddComponent<MeshRenderer>(); }

                                    if (meshFilter == null) { Debug.LogWarning("ERROR: LBLandscapeOperations.CreateLandscapeMesh - Could not add MeshFilter to " + lbMesh.title); }
                                    else if (meshRenderer == null) { Debug.LogWarning("ERROR: LBLandscapeOperations.CreateLandscapeMesh - Could not add MeshRenderer to " + lbMesh.title); }
                                    else
                                    {
                                        meshFilter.sharedMesh = lbMesh.mesh;

                                        if (includeSplatmaps && splatTextureList != null && lbMeshTerrainShader != null)
                                        {
                                            // Assumes it ends with "_Mesh00:00"
                                            string terrainName = lbMesh.title.Remove(lbMesh.title.Length - 10);

                                            Texture2D splat0Texture = splatTextureList.Find(tex => tex.name == terrainName + "_splat0");
                                            if (splat0Texture != null)
                                            {
                                                //Debug.Log("found splat texture " + splatTexture.name);
                                                Material splatMat = new Material(lbMeshTerrainShader);
                                                if (splatMat != null)
                                                {
                                                    splatMat.SetTexture("_Splat0TexPkd", splat0Texture);

                                                    if (numActiveTextures > 0) { SetTerrainMeshMaterialTexture(lbTerrainTexture[0], splatMat, "_Splat0TexR", terrainWidth); }
                                                    if (numActiveTextures > 1) { SetTerrainMeshMaterialTexture(lbTerrainTexture[1], splatMat, "_Splat0TexG", terrainWidth); }
                                                    if (numActiveTextures > 2) { SetTerrainMeshMaterialTexture(lbTerrainTexture[2], splatMat, "_Splat0TexB", terrainWidth); }
                                                    if (numActiveTextures > 3) { SetTerrainMeshMaterialTexture(lbTerrainTexture[3], splatMat, "_Splat0TexA", terrainWidth); }

                                                    // If using 5-8 textures, populate the second splatmap (splat1)
                                                    if (numActiveTextures > 4)
                                                    {
                                                        Texture2D splat1Texture = splatTextureList.Find(tex => tex.name == terrainName + "_splat1");
                                                        if (splat1Texture != null)
                                                        {
                                                            splatMat.EnableKeyword("USE_SPLAT1");
                                                            splatMat.SetTexture("_Splat1TexPkd", splat1Texture);
                                                            SetTerrainMeshMaterialTexture(lbTerrainTexture[4], splatMat, "_Splat1TexR", terrainWidth);
                                                            if (numActiveTextures > 5) { SetTerrainMeshMaterialTexture(lbTerrainTexture[5], splatMat, "_Splat1TexG", terrainWidth); }
                                                            if (numActiveTextures > 6) { SetTerrainMeshMaterialTexture(lbTerrainTexture[6], splatMat, "_Splat1TexB", terrainWidth); }
                                                            if (numActiveTextures > 7) { SetTerrainMeshMaterialTexture(lbTerrainTexture[7], splatMat, "_Splat1TexA", terrainWidth); }
                                                        }
                                                    }
                                                    else { splatMat.DisableKeyword("USE_SPLAT1"); }

                                                    meshRenderer.sharedMaterial = splatMat;
                                                }
                                            }
                                        }
                                        else if (useMegaSplat && meshMaterial != null)
                                        {
                                            meshRenderer.sharedMaterial = meshMaterial;
                                        }

                                        // Check to see if an OcclusionArea script is attached
                                        OcclusionArea occlusionArea = meshTransform.GetComponent<OcclusionArea>();

                                        // Occlusion Areas define the boundaries of the mesh(es) that are not rendered if not in view of the camera frustrum
                                        // AND are occluded (obscured) by another object. Before this can take effect, the user will need to Bake the areas.
                                        // This Baking process is a manual action available in the Unity Occlusion window.
                                        // By default, Occlusion Culling is enabled on Cameras.
                                        // Meshes need to be Occlusion Static to be included in the Occlusion Bake.
                                        if (includeOcclusionAreas)
                                        {
                                            if (occlusionArea == null) { occlusionArea = meshTransform.gameObject.AddComponent<OcclusionArea>(); }

                                            if (occlusionArea != null)
                                            {
                                                if (lbMesh.IsMinExtentSet() && lbMesh.IsMaxExtentSet())
                                                {
                                                    occlusionArea.size = new Vector3(lbMesh.maxExtent.x - lbMesh.minExtent.x, lbMesh.maxExtent.y - lbMesh.minExtent.y, lbMesh.maxExtent.z - lbMesh.minExtent.z);
                                                    occlusionArea.center = new Vector3(lbMesh.minExtent.x + (lbMesh.maxExtent.x - lbMesh.minExtent.x) / 2f, lbMesh.minExtent.y + (lbMesh.maxExtent.y - lbMesh.minExtent.y) / 2f, lbMesh.minExtent.z + (lbMesh.maxExtent.z - lbMesh.minExtent.z) / 2f);
                                                }
                                            }
                                        }
                                        else if (occlusionArea != null) { GameObject.DestroyImmediate(occlusionArea); }
                                    }

                                    // Assign verts, triangle to new Unity Mesh and recalc bounds
                                    isSuccessful = lbMesh.UpdateMesh(false, true);

                                    if (isSuccessful)
                                    {
#if UNITY_EDITOR
                                        if (saveMeshToProjectFolder)
                                        {
                                            // Asset names cannot contain colon, so replace with dash.
                                            string _filePath = meshProjectFolder + "/" + lbMesh.mesh.name.Replace(":", "-") + ".asset";
                                            bool _continueToSave = true;

                                            // Has the user already clicked "Overwrite All"? If so, we don't need to see if the file already exists
                                            if (displayDialogComplexInt != 1)
                                            {
                                                if (System.IO.File.Exists(_filePath))
                                                {
                                                    displayDialogComplexInt = UnityEditor.EditorUtility.DisplayDialogComplex("Mesh Already Exists", "Are you sure you want to overwrite " + _filePath + "?", "Overwrite", "Overwrite All", "Cancel");

                                                    _continueToSave = (displayDialogComplexInt == 0 || displayDialogComplexInt == 1);
                                                }
                                            }

                                            if (_continueToSave) { UnityEditor.AssetDatabase.CreateAsset(lbMesh.mesh, _filePath); }
                                        }
#endif

                                        if (useMegaSplat)
                                        {
#if __MEGASPLAT__
                                            // Process the mesh to be used with MegaSplat
                                            lbMesh.mesh = LBIntegration.MegaSplatProcessMesh(lbMesh.mesh, true);

                                            // Extract the texture data from the terrains and add it to the meshes
                                            lbMesh.mesh = LBIntegration.MegaSplatCopyTexturesToMesh(landscape, lbMesh, true);
#endif
                                        }

                                        if (addColliders) { meshCollider = meshTransform.gameObject.AddComponent<MeshCollider>(); }
                                    }
                                    else
                                    {
                                        Debug.LogWarning("ERROR: LBLandscapeOperations.CreateLandscapeMesh - failed to update mesh for " + lbMesh.title);
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    // When all is well, set the transform to be returned
                    if (isSuccessful) { newMeshTransform = meshParentTrfm.transform; }
                }
            }

            return newMeshTransform;
        }

        /// <summary>
        /// Copy the LBTerrainTexture into the correct slot within the material.
        /// Adds the texture and the tiling
        /// </summary>
        /// <param name="lbTerrainTexture"></param>
        /// <param name="material"></param>
        /// <param name="materialPropertyName"></param>
        private static void SetTerrainMeshMaterialTexture(LBTerrainTexture lbTerrainTexture, Material material, string materialPropertyName, float terrainWidth)
        {
            if (lbTerrainTexture != null)
            {
                material.SetTexture(materialPropertyName, lbTerrainTexture.texture);
                if (lbTerrainTexture.tileSize.x > 0f && lbTerrainTexture.tileSize.y > 0f)
                {
                    material.SetTextureScale(materialPropertyName, new Vector2(terrainWidth / lbTerrainTexture.tileSize.x, terrainWidth / lbTerrainTexture.tileSize.y));
                    material.SetTextureScale(materialPropertyName + "NM", new Vector2(terrainWidth / lbTerrainTexture.tileSize.x, terrainWidth / lbTerrainTexture.tileSize.y));
                }
                material.SetTexture(materialPropertyName + "NM", lbTerrainTexture.normalMap);

                material.SetFloat(materialPropertyName + "_Metallic", lbTerrainTexture.metallic);
                material.SetFloat(materialPropertyName + "_Smoothness", lbTerrainTexture.smoothness);
            }
        }


        #endregion

        #region Public EDITOR-ONLY Static Methods
#if UNITY_EDITOR

        /// <summary>
        /// Returns a unique list of Map Texture paths used in a landscape.
        /// Typically Map Textures are used with Texture, Grass, Trees and/or Meshes
        /// for populating/placement restrictions. e.g. Height Inclination Map
        /// </summary>
        /// <param name="landscape"></param>
        /// <returns></returns>
        public static List<string> GetMapTextureAssetPathsFromLandscape(LBLandscape landscape, bool showErrors)
        {
            List<string> mapPathList = new List<string>();
            string mapPath = string.Empty;

            // Basic validation
            if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR: LBLandscapeOperations.GetMapTextureAssetPathsFromLandscape - landscape cannot be null"); } }
            else if (landscape.topographyLayersList == null) { if (showErrors) { Debug.LogWarning("ERROR: LBLandscapeOperations.GetMapTextureAssetPathsFromLandscape - topographyLayersList cannot be null"); } }
            else if (landscape.terrainTexturesList == null) { if (showErrors) { Debug.LogWarning("ERROR: LBLandscapeOperations.GetMapTextureAssetPathsFromLandscape - terrainTexturesList cannot be null"); } }
            else if (landscape.terrainTreesList == null) { if (showErrors) { Debug.LogWarning("ERROR: LBLandscapeOperations.GetMapTextureAssetPathsFromLandscape - terrainTreesList cannot be null"); } }
            else if (landscape.terrainGrassList == null) { if (showErrors) { Debug.LogWarning("ERROR: LBLandscapeOperations.GetMapTextureAssetPathsFromLandscape - terrainGrassList cannot be null"); } }
            else if (landscape.landscapeMeshList == null) { if (showErrors) { Debug.LogWarning("ERROR: LBLandscapeOperations.GetMapTextureAssetPathsFromLandscape - landscapeMeshList cannot be null"); } }
            else
            {
                // Get any Map Texture2D files used with Topography filters
                if (landscape.topographyLayersList.Count > 0)
                {
                    foreach (LBLayer lbLayer in landscape.topographyLayersList)
                    {
                        if (lbLayer != null)
                        {
                            if (lbLayer.filters != null)
                            {
                                // If there are any filters attached to this Topography Layer, check to see if it is a Map Layer Filter
                                foreach (LBLayerFilter lbFilter in lbLayer.filters)
                                {
                                    if (lbFilter != null)
                                    {
                                        if (lbFilter.map != null && lbFilter.type == LBLayerFilter.LayerFilterType.Map)
                                        {
                                            mapPath = UnityEditor.AssetDatabase.GetAssetPath(lbFilter.map);
                                            if (!string.IsNullOrEmpty(mapPath) && !mapPathList.Contains(mapPath)) { mapPathList.Add(mapPath); }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // Get any Map Texture2D files used with Texture placement
                if (landscape.terrainTexturesList.Count > 0)
                {
                    foreach (LBTerrainTexture terrainTexture in landscape.terrainTexturesList)
                    {
                        // Is a Map being used?
                        if (terrainTexture.map != null && (terrainTexture.texturingMode == LBTerrainTexture.TexturingMode.Map || terrainTexture.texturingMode == LBTerrainTexture.TexturingMode.HeightInclinationMap))
                        {
                            mapPath = UnityEditor.AssetDatabase.GetAssetPath(terrainTexture.map);
                            //Debug.Log("INFO: LBLandscapeOperations - Texture - map texture path: " + mapPath);
                            if (!string.IsNullOrEmpty(mapPath) && !mapPathList.Contains(mapPath)) { mapPathList.Add(mapPath); }
                        }
                    }
                }

                // Get any Map Texture2D files used with Tree placement
                if (landscape.terrainTreesList.Count > 0)
                {
                    foreach (LBTerrainTree terrainTrees in landscape.terrainTreesList)
                    {
                        // Is a Map being used?
                        if (terrainTrees.map != null && (terrainTrees.treePlacingMode == LBTerrainTree.TreePlacingMode.Map || terrainTrees.treePlacingMode == LBTerrainTree.TreePlacingMode.HeightInclinationMap))
                        {
                            mapPath = UnityEditor.AssetDatabase.GetAssetPath(terrainTrees.map);
                            //Debug.Log("INFO: LBLandscapeOperations - Tree - map texture path: " + mapPath);
                            if (!string.IsNullOrEmpty(mapPath) && !mapPathList.Contains(mapPath)) { mapPathList.Add(mapPath); }
                        }
                    }
                }

                // Get any Map Texture2D files used with Grass placement
                if (landscape.terrainGrassList.Count > 0)
                {
                    foreach (LBTerrainGrass terrainGrass in landscape.terrainGrassList)
                    {
                        // Is a Map being used?
                        if (terrainGrass.map != null && (terrainGrass.grassPlacingMode == LBTerrainGrass.GrassPlacingMode.Map || terrainGrass.grassPlacingMode == LBTerrainGrass.GrassPlacingMode.HeightInclinationMap))
                        {
                            mapPath = UnityEditor.AssetDatabase.GetAssetPath(terrainGrass.map);
                            //Debug.Log("INFO: LBLandscapeOperations - Grass - map texture path: " + mapPath);
                            if (!string.IsNullOrEmpty(mapPath) && !mapPathList.Contains(mapPath)) { mapPathList.Add(mapPath); }
                        }
                    }
                }

                // Get any Map Texture2D files used with Grass placement
                if (landscape.landscapeMeshList.Count > 0)
                {
                    foreach (LBLandscapeMesh landscapeMesh in landscape.landscapeMeshList)
                    {
                        // Is a Map being used?
                        if (landscapeMesh.map != null && (landscapeMesh.meshPlacingMode == LBLandscapeMesh.MeshPlacingMode.Map || landscapeMesh.meshPlacingMode == LBLandscapeMesh.MeshPlacingMode.HeightInclinationMap))
                        {
                            mapPath = UnityEditor.AssetDatabase.GetAssetPath(landscapeMesh.map);
                            //Debug.Log("INFO: LBLandscapeOperations - Mesh - map texture path: " + mapPath);
                            if (!string.IsNullOrEmpty(mapPath) && !mapPathList.Contains(mapPath)) { mapPathList.Add(mapPath); }
                        }
                    }
                }
            }

            return mapPathList;
        }

        /// <summary>
        /// Export the Splatmap textures as individual PNG files that which cover the whole landscape. For example if you have
        /// 4 textures on the Texturing Tab, you'd get 4 files regardless of how many terrains are in the landscape.
        /// Textures are saved into a folder called Assets/LandscapeBuilder/Splatmaps/[scenename]_[landscapename]
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="channel"></param>
        /// <param name="showErrors"></param>
        public static void ExportSplatmapTextures(LBLandscape landscape, string channel, bool showErrors)
        {
            string methodName = "LBLandscapeOperations.ExportSplatmapTextures";

            if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR " + methodName + " - landscape script object is null"); } }
            else
            {
                landscape.SetLandscapeTerrains(true);
                int numTerrains = (landscape.landscapeTerrains == null ? 0 : landscape.landscapeTerrains.Length);

                if (numTerrains < 1) { if (showErrors) { Debug.LogWarning("ERROR " + methodName + " - no terrains in landscape"); } }
                else if (landscape.landscapeTerrains[0].terrainData == null) { if (showErrors) { Debug.LogWarning("ERROR " + methodName + " - no terrain data in landscape"); } }
                else
                {
                    LBEditorHelper.CheckFolder("Assets/LandscapeBuilder/Splatmaps");
                    string splatProjectFolderPath = "Assets/LandscapeBuilder/Splatmaps/" + UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name + "_" + landscape.name;
                    LBEditorHelper.CheckFolder(splatProjectFolderPath);

                    // Check the first terrain
                    TerrainData tData = landscape.landscapeTerrains[0].terrainData;
                    int numAlphaMapLayers = tData.alphamapLayers;

                    // By default LB creates terrains left to right, bottom to top (0,0 is bottom left corner)
                    int terrainRow = 0;
                    int terrainCol = 0;
                    int numTerrainsWide = Mathf.RoundToInt(Mathf.Sqrt(numTerrains));
                    int heightmapResolution = landscape.GetLandscapeTerrainHeightmapResolution();

                    // Ensure texture size is a power of 2 by using (heightmapResolution-1).
                    int texWidth = numTerrainsWide * (heightmapResolution - 1);

                    // Clamp max texture size to 8192x8192
                    if (texWidth > 8192) { texWidth = 8192; }

                    bool isError = false;
                    string exportPNGFilePath = string.Empty;

                    for (int alphaIdx = 0; !isError && alphaIdx < numAlphaMapLayers; alphaIdx++)
                    {
                        // Create a new output texture for each terrain texture (alphamap)
                        Texture2D splatTexture = new Texture2D(texWidth, texWidth, TextureFormat.ARGB32, false);

                        if (splatTexture == null) { if (showErrors) { Debug.LogWarning("ERROR " + methodName + " - failed to create new output Texture"); break; } }
                        else
                        {
                            splatTexture.name = "Splatmap" + Mathf.FloorToInt(((float)alphaIdx/4f)).ToString("0") + "Tex" + alphaIdx.ToString("00") + channel;

                            // Process each terrain for each terrain texture
                            for (int terrainIdx = 0; terrainIdx < numTerrains; terrainIdx++)
                            {
                                Terrain terrain = landscape.landscapeTerrains[terrainIdx];
                                if (terrain == null) { isError = true; break; }
                                else if (terrain.terrainData == null) { isError = true; break; }
                                else
                                {
                                    terrainRow = terrainIdx % numTerrainsWide;
                                    terrainCol = terrainIdx / numTerrainsWide;

                                    tData = terrain.terrainData;
                                    // Check the number of alphamaps is the same in all terrains
                                    if (tData.alphamapLayers != numAlphaMapLayers)
                                    {
                                        if (showErrors) { Debug.LogWarning("ERROR " + methodName + " - the terrain " + terrain.name + " has a different number of splatmap textures than " + landscape.landscapeTerrains[0].name); }
                                        isError = true;
                                        break;
                                    }
                                    else
                                    {
                                        LBLandscapeTerrain.ExportTextureFromSplatmap(landscape, numTerrains, terrain, terrainRow, terrainCol, alphaIdx, splatTexture, channel, showErrors);
                                    }
                                }
                            }

                            if (!isError)
                            {
                                exportPNGFilePath = splatProjectFolderPath + "/" + splatTexture.name + ".png";
                                LBEditorHelper.SaveMapTexture(splatTexture, exportPNGFilePath, splatTexture.width);
                                Debug.Log("Exported R Channel splat texture as PNG to " + exportPNGFilePath + " successfully.");
                            }
                        }
                    }

                    // Errors can occur if the terrains were not built with LB.
                    if (isError) { if (showErrors) { Debug.LogWarning("ERROR " + methodName + " - sorry, an unexpected error occurred. Please Report."); } }
                }
            }
        }

        /// <summary>
        /// Get the (packed) splatmaps for all the terrains in the landscape, and save them in the Asset/Project folder
        /// within a folder named [ProjectName]_[LandscapeName]
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="showErrors"></param>
        public static void ExportSplatmaps(LBLandscape landscape, bool showErrors)
        {
            Terrain[] landscapeTerrains = landscape.gameObject.GetComponentsInChildren<Terrain>();
            if (landscapeTerrains == null)
            {
                if (showErrors) { Debug.LogWarning("Export Splatmaps: Could not find Terrain objects under Landscape " + landscape.gameObject.name + " GameObject"); }
            }
            else
            {
                LBEditorHelper.CheckFolder("Assets/LandscapeBuilder/Splatmaps");
                string splatSceneFolderPath = "Assets/LandscapeBuilder/Splatmaps/" + UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name + "_" + landscape.name;
                LBEditorHelper.CheckFolder(splatSceneFolderPath);

                for (int index = 0; index < landscapeTerrains.Length; index++)
                {
                    Terrain terrain = landscapeTerrains[index];

                    int numAlphaMapLayers = terrain.terrainData.alphamapLayers;

                    for (int splatIndex = 0, textureSlots = 0; splatIndex < 3 && textureSlots < numAlphaMapLayers; splatIndex++, textureSlots += 4)
                    {
                        Texture2D splatTexture = LBLandscapeTerrain.GetTextureFromSplatmap(landscape, terrain, splatIndex, true);
                        if (splatTexture != null)
                        {
                            LBEditorHelper.SaveMapTexture(splatTexture, splatSceneFolderPath + "/" + splatTexture.name + ".png", splatTexture.width);
                        }
                        else { break; } // If there was an error, don't continue
                    }
                }
            }
        }

#endif
        #endregion
    }
}