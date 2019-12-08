using UnityEngine;
using System.Collections.Generic;

namespace LandscapeBuilder
{
    public class LBMeshOperations
    {
        // Mesh Operations Class

        #region Submesh Methods

        /// <summary>
        /// Create a new mesh from a submesh of baseMesh with index submeshIndex
        /// </summary>
        public static Mesh CreateMeshFromSubmesh(Mesh baseMesh, int submeshIndex)
        {
            // Function adapted from the MeshCreationHelper.cs script on the Unify Community Wiki

            Mesh newMesh = new Mesh();

            List<int> triangles = new List<int>();
            triangles.AddRange(baseMesh.GetTriangles(submeshIndex)); // the triangles of the sub mesh

            List<Vector3> newVertices = new List<Vector3>();
            List<Vector2> newUvs = new List<Vector2>();
            List<Vector2> newUv2s = new List<Vector2>();
            List<Vector2> newUv3s = new List<Vector2>();
            List<Vector2> newUv4s = new List<Vector2>();
            List<Vector3> newNormals = new List<Vector3>();
            List<Color> newColours = new List<Color>();
            List<Vector4> newTangents = new List<Vector4>();

            Dictionary<int, int> oldToNewIndices = new Dictionary<int, int>();
            int newIndex = 0;
            int i = 0;

            // Improve performance by almost 50% by pre-defining variables and only getting the length of an array once.
            int numBaseMeshVerts = baseMesh.vertices.Length;
            int numBaseMeshColours = (baseMesh.colors == null ? 0 : baseMesh.colors.Length);
            int numBaseMeshNormals = (baseMesh.normals == null ? 0 : baseMesh.normals.Length);
            int numBaseMeshTangents = (baseMesh.tangents == null ? 0 : baseMesh.tangents.Length);
            int numBaseMeshuvs = (baseMesh.uv == null ? 0 : baseMesh.uv.Length);
            int numBaseMeshuv2s = (baseMesh.uv2 == null ? 0 : baseMesh.uv2.Length);
            int numBaseMeshuv3s = (baseMesh.uv3 == null ? 0 : baseMesh.uv3.Length);
            int numBaseMeshuv4s = (baseMesh.uv4 == null ? 0 : baseMesh.uv4.Length);

            // Collect the vertices and uvs
            for (i = 0; i < numBaseMeshVerts; i++)
            {
                if (triangles.Contains(i))
                {
                    newVertices.Add(baseMesh.vertices[i]);

                    if (numBaseMeshColours > i) { newColours.Add(baseMesh.colors[i]); }
                    else { newColours.Add(Color.white); }
                    if (numBaseMeshNormals > i) { newNormals.Add(baseMesh.normals[i]); }
                    if (numBaseMeshTangents > i) { newTangents.Add(baseMesh.tangents[i]); }

                    if (numBaseMeshuvs > i) { newUvs.Add(baseMesh.uv[i]); }
                    if (numBaseMeshuv2s > i) { newUv2s.Add(baseMesh.uv2[i]); }
                    if (numBaseMeshuv3s > i) { newUv3s.Add(baseMesh.uv2[i]); }
                    if (numBaseMeshuv4s > i) { newUv4s.Add(baseMesh.uv2[i]); }

                    oldToNewIndices.Add(i, newIndex);
                    ++newIndex;
                }
            }

            int[] newTriangles = new int[triangles.Count];

            int numNewTriangles = (newTriangles == null ? 0 : newTriangles.Length);

            // Collect the new triangles indicies
            for (i = 0; i < numNewTriangles; i++)
            {
                newTriangles[i] = oldToNewIndices[triangles[i]];
            }
            // Assemble the new mesh with the new vertices/uv/triangles.
            newMesh.vertices = newVertices.ToArray();

            newMesh.triangles = newTriangles;

            if (numBaseMeshuvs > 0) { newMesh.uv = newUvs.ToArray(); }
            if (numBaseMeshuv2s > 0) { newMesh.uv2 = newUv2s.ToArray(); }
            if (numBaseMeshuv3s > 0) { newMesh.uv3 = newUv3s.ToArray(); }
            if (numBaseMeshuv4s > 0) { newMesh.uv4 = newUv4s.ToArray(); }
            if (newMesh.colors.Length > 0) { newMesh.colors = newColours.ToArray(); }
            if (newMesh.normals.Length > 0) { newMesh.normals = newNormals.ToArray(); }
            else { newMesh.RecalculateNormals(); }
            // If tangents don't exist, recalculate tangents so that normalmaps work on combined meshes
            if (newMesh.tangents.Length > 0) { newMesh.tangents = newTangents.ToArray(); }
            else
            {
                #if UNITY_5_6_OR_NEWER
                newMesh.RecalculateTangents();
                #else
                newMesh = LBMeshOperations.CalculateTangents(newMesh);
                #endif
            }

            // Re-calculate bounds for the renderer.
            newMesh.RecalculateBounds();

            return newMesh;
        }
        #endregion

        #region Mesh Scene Methods

        /// <summary>
        /// Add a LBMesh mesh to a new gameobject in the scene as a child of the parentTransform
        /// </summary>
        /// <param name="lbMesh"></param>
        /// <param name="position"></param>
        /// <param name="gameobjectName"></param>
        /// <param name="parentTransform"></param>
        /// <param name="meshMaterial"></param>
        /// <param name="isStatic"></param>
        /// <param name="checkForExisting"></param>
        /// <returns></returns>
        public static Transform AddMeshToScene(LBMesh lbMesh, Vector3 position, string gameobjectName, Transform parentTransform, Material meshMaterial, bool isStatic, bool checkForExisting)
        {
            Transform meshTranform = null;
            string methodName = "LBMeshOperations.AddMeshToScene";

            // Basic validation
            if (lbMesh == null) { Debug.LogWarning("ERROR: " + methodName + " - LBMesh cannot be null"); }
            else if (string.IsNullOrEmpty(gameobjectName)) { Debug.LogWarning("ERROR: " + methodName + " - no name specified"); }
            else if (parentTransform == null) { Debug.LogWarning("ERROR: " + methodName + " - no parent transform is specified"); }
            else
            {
                GameObject meshGameObject;
                Transform tfrm = null;

                // Check to see if the GameObject already exists
                if (checkForExisting) { tfrm = parentTransform.Find(gameobjectName); }

                // If it doesn't exist or checkForExisting is false, create a new child object.
                if (tfrm == null)
                {
                    meshGameObject = new GameObject(gameobjectName);
                    if (meshGameObject == null) { Debug.LogWarning("ERROR: " + methodName + " - could not create GameObject " + gameobjectName); }
                    else
                    {
                        meshGameObject.transform.SetParent(parentTransform);
                    }
                }
                else { meshGameObject = tfrm.gameObject; }

                if (meshGameObject != null)
                {
                    meshGameObject.isStatic = isStatic;
                    meshGameObject.transform.position = position;

                    // If the Mesh Filter doesn't exist, add it
                    MeshFilter meshFilter = meshGameObject.GetComponent<MeshFilter>();
                    if (meshFilter == null) { meshFilter = meshGameObject.AddComponent<MeshFilter>(); }

                    // If the Mesh Renderer doesn't exist, add it
                    MeshRenderer meshRenderer = meshGameObject.GetComponent<MeshRenderer>();
                    if (meshRenderer == null) { meshRenderer = meshGameObject.AddComponent<MeshRenderer>(); }

                    if (meshFilter == null) { Debug.LogWarning("ERROR: " + methodName + " - Could not add MeshFilter to " + gameobjectName); }
                    else if (meshRenderer == null) { Debug.LogWarning("ERROR: " + methodName + " - Could not add MeshRenderer to " + gameobjectName); }
                    else
                    {
                        meshFilter.sharedMesh = lbMesh.mesh;
                        meshRenderer.sharedMaterial = meshMaterial;

                        //lbMesh.mesh.RecalculateBounds();

                        // We may already have the trfm (see above) but we want to only return if everything succeeds
                        meshTranform = meshGameObject.transform;
                    }
                }
            }

            return meshTranform;
        }

        /// <summary>
        /// Remove an existing mesh from the scene
        /// </summary>
        /// <param name="lbMesh"></param>
        /// <param name="gameobjectName"></param>
        /// <param name="parentTransform"></param>
        public static void RemoveMeshFromScene(string gameobjectName, Transform parentTransform, bool showErrors = true)
        {
            string methodName = "LBMeshOperations.RemoveMeshFromScene";
            if (string.IsNullOrEmpty(gameobjectName)) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - no name specified"); } }
            else if (parentTransform == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " - no parent transform is specified"); } }
            else
            {
                // Check to see if the GameObject exists
                Transform tfrm = parentTransform.Find(gameobjectName);

                if (tfrm != null) { GameObject.DestroyImmediate(tfrm.gameObject); }
            }
        }

        #endregion

        #region Mesh Water Methods

        /// <summary>
        /// Create an LBMesh object to hold the water mesh for a Topography Image Modifier Layer.
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="lbLayer"></param>
        /// <param name="layerIdx"></param>
        /// <param name="meshTitle"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static bool CreateMeshForWaterFromLayer(LBLandscape landscape, LBLayer lbLayer, int layerIdx, string meshTitle, bool showErrors)
        {
            bool isSuccessful = false;

            string methodName = "LBMeshOperations.CreateMeshForWaterFromLayer";

            if (landscape == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " landscape is null. Please Report"); } }
            else if (lbLayer == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " lbLayer is null. Please Report"); } }
            else if (lbLayer.type != LBLayer.LayerType.ImageModifier) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " LayerType must be ImageModifier. Please Report"); } }
            {
                // TODO get the minium area below zero for the RAW modifier file to use for meshWidth/Length

                // Get the absolute width and length
                Vector2 meshSize = Vector2.zero;
                meshSize.x = lbLayer.areaRect.width < 0 ? -lbLayer.areaRect.width : lbLayer.areaRect.width;
                meshSize.y = lbLayer.areaRect.height < 0 ? -lbLayer.areaRect.height : lbLayer.areaRect.height;

                // Get the bottom-left point of mesh with landscape (it might be outside the landscape)
                Vector2 meshBottomLeft = Vector2.zero;
                meshBottomLeft.x = lbLayer.areaRect.x - (meshSize.x / 2f);
                meshBottomLeft.y = lbLayer.areaRect.y - (meshSize.y / 2f);
                if (meshBottomLeft.x < 0) { meshBottomLeft.x += meshSize.x; } else if (meshBottomLeft.x == meshSize.x) { meshBottomLeft.x = 0f; }
                if (meshBottomLeft.y < 0) { meshBottomLeft.y += meshSize.y; } else if (meshBottomLeft.y == meshSize.y) { meshBottomLeft.y = 0f; }

                //Debug.Log("INFO: " + methodName + " areaRect xy: " + lbLayer.areaRect.x + "," + lbLayer.areaRect.y + " b-l:" + meshBottomLeft);

                // If there is an existing LBMesh instance, remove all the mesh data
                if (lbLayer.modifierWaterLBMesh != null) { LBMesh.DeleteMesh(lbLayer.modifierWaterLBMesh); }
                else { lbLayer.modifierWaterLBMesh = new LBMesh(); }

                if (lbLayer.modifierWaterLBMesh == null) { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " Could not create LBMesh for the water in Layer " + (layerIdx + 1) + ". Please Report"); } }
                else if (meshSize.x < 0.1f || meshSize.y < 0.1f) { { if (showErrors) { Debug.LogWarning("ERROR: " + methodName + " LayerType " + (layerIdx + 1) + " has an invalid water mesh size " + meshSize.x + "," + meshSize.y); } } }
                {
                    lbLayer.modifierWaterLBMesh.title = meshTitle;

                    // Initialise mesh lists
                    List<Vector3> verts = new List<Vector3>();
                    List<Vector2> uvs = new List<Vector2>();
                    List<int> triangles = new List<int>();
                    List<Vector3> normals = new List<Vector3>();
                    List<Vector4> tangents = new List<Vector4>();
                    List<Vector4> colours = new List<Vector4>();    // Store as Vector4s rather than Color so they are serializable

                    // Declare outside loops for less garbage collection
                    // We are creating a flat plane, so can use Vector2.
                    Vector2 vertPositionN = Vector2.zero;  // Normalised position within mesh
                    Vector2 vertLandscapePosN = Vector2.zero; // Normalised postion within the landscape
                    int vertCount = 0;

                    // Default colour of each vert (stored in LB as a Vector4)
                    Vector4 defaultVertColour = new Vector4(1f, 1f, 1f, 1f);

                    // The number of cells wide and long in the mesh
                    int meshWidth = 10;
                    int meshLength = 10;

                    for (int x = 0; x < meshWidth; x++)
                    {
                        for (int z = 0; z < meshLength; z++)
                        {
                            // Create the vert as a 0-1 position
                            // Normalise the RAW Pixel - converting it to a range of 0 to 1
                            vertPositionN.x = (float)x / (float)(meshWidth - 1);
                            vertPositionN.y = (float)z / (float)(meshLength - 1);
                            verts.Add(new Vector3(vertPositionN.x * meshSize.x, 0f, vertPositionN.y * meshSize.y));
                            normals.Add(Vector3.up);

                            if (lbLayer.modifierWaterIsMeshLandscapeUV)
                            {
                                // UVs normalised to the landscape
                                vertLandscapePosN.x = ((vertPositionN.x * meshSize.x) + meshBottomLeft.x) / landscape.size.x;
                                vertLandscapePosN.y = ((vertPositionN.y * meshSize.y) + meshBottomLeft.y) / landscape.size.y;

                                uvs.Add(new Vector2(vertLandscapePosN.x / lbLayer.modifierWaterMeshUVTileScale.x, vertLandscapePosN.y / lbLayer.modifierWaterMeshUVTileScale.y));
                            }
                            else
                            {
                                // Generic uvs (simply 0-1 coordinates of vert position)
                                uvs.Add(new Vector2(vertPositionN.x / lbLayer.modifierWaterMeshUVTileScale.x, vertPositionN.y / lbLayer.modifierWaterMeshUVTileScale.y));
                            }

                            // Create tangent
                            tangents.Add(new Vector4(1f, 0f, 0f, 1f));

                            // Add the two triangles for the quad
                            // Not required if on left or bottom edges of the mesh
                            if (x < meshWidth - 1 && z < meshLength - 1)
                            {
                                // Bottom (left) triangle
                                // Bottom left of quad
                                triangles.Add(vertCount);
                                // Bottom right of quad
                                triangles.Add(vertCount + 1);
                                // Top right of quad
                                triangles.Add(vertCount + meshWidth + 1);

                                // Top (right) triangle
                                // Top left of quad
                                triangles.Add(vertCount + meshWidth);
                                // Bottom left of quad
                                triangles.Add(vertCount);
                                // Top right of quad
                                triangles.Add(vertCount + meshWidth + 1);
                            }

                            // Set default vert colour
                            colours.Add(defaultVertColour);

                            // Increment the vert count
                            vertCount++;
                        }
                    }

                    lbLayer.modifierWaterLBMesh.verts = verts;
                    lbLayer.modifierWaterLBMesh.triangles = triangles;
                    lbLayer.modifierWaterLBMesh.normals = normals;
                    lbLayer.modifierWaterLBMesh.uvs = uvs;
                    lbLayer.modifierWaterLBMesh.tangents = tangents;

                    //Debug.Log("INFO " + methodName + " - created Mesh (verts " + verts.Count + " tris " + (triangles.Count / 3) + ")");

                    isSuccessful = true;
                }
            }

            return isSuccessful;
        }


        #endregion

        #region Mesh Bounds methods

        /// <summary>
        /// Get the Bounds of all the meshes that are children of a transform in WorldSpace.
        /// NOTE: This will create garbage as it doesn't use the optimised GetComponentsInChildren
        /// bounds.extents.magnitude is the radius of the prefab.
        /// To correct for a prefab with a non-zero position, see LBEditorHelper.GetPrefabBounds(..)
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="includeInactive"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static Bounds GetBounds(Transform transform, bool includeInactive, bool showErrors)
        {
            string methodName = "LBMeshOperations.GetBounds";
            Bounds combinedBounds = new Bounds(transform.position, Vector3.zero);

            if (transform == null) { if (showErrors) { Debug.LogWarning("WARNING: " + methodName + " transform is null"); } }
            else
            {
                MeshRenderer[] renderers = transform.GetComponentsInChildren<MeshRenderer>(includeInactive);
                int numRenderers = renderers == null ? 0 : renderers.Length;

                if (renderers != null)
                {
                    for (int r = 0; r < numRenderers; r++)
                    {
                        // bounds is not nullable
                        combinedBounds.Encapsulate(renderers[r].bounds);

                        //Debug.Log(renderers[r].name + " " + renderers[r].bounds.extents + " new extents: " + combinedBounds.extents.magnitude);
                    }
                }
            }

            return combinedBounds;
        }

        #endregion

        #region Normal Methods
        /// <summary>
        /// Flip the normals of a mesh. This includes flipping the normals
        /// AND reversing the order of the first two points in each triangle.
        /// UVs are unchanged.
        /// </summary>
        /// <param name="mesh"></param>
        public static void FlipNormals(Mesh mesh)
        {
            if (mesh != null)
            {
                if (mesh.normals != null)
                {
                    int numSubMeshes = mesh.subMeshCount;

                    if (mesh.normals.Length > 0 && numSubMeshes > 0)
                    {
                        //Debug.Log("Flipping mesh normals");

                        // Modify normals outside a mesh so we don't modify the mesh
                        // each time through the loop
                        Vector3[] flippedNormals = mesh.normals;

                        for (int i = 0; i < mesh.normals.Length; i++)
                        {
                            flippedNormals[i] = -flippedNormals[i];
                        }

                        // Submeshes are just separate triangles for each material
                        for (int mIndex = 0; mIndex < numSubMeshes; mIndex++)
                        {
                            // Get the triangles in the submesh
                            int[] triangles = mesh.GetTriangles(mIndex);

                            // Reverse the position of the first two points in the triangle
                            for (int tpt = 0; tpt < triangles.Length; tpt += 3)
                            {
                                // Remember the first point in the current triangle
                                int firstPt = triangles[tpt];
                                // Set the first point to be the second point
                                triangles[tpt] = triangles[tpt + 1];
                                // Set the second point to be the first
                                triangles[tpt + 1] = firstPt;
                            }

                            mesh.SetTriangles(triangles, mIndex);
                        }

                        // Update the mesh normals
                        mesh.normals = flippedNormals;
                    }
                }
            }
        }

        #endregion

        #region Tangent Methods

        /// <summary>
        /// Simplified method to set tangents on a flat mesh
        /// </summary>
        /// <param name="mesh"></param>
        /// <returns></returns>
        public static Mesh ReCalculateTangents(Mesh mesh)
        {
            List<Vector4> tangents = new List<Vector4>();

            if (mesh == null) { Debug.LogWarning("ERROR: LBMeshOperations.ReCalculateTangents - mesh cannot be null"); }
            else
            {
                Vector3[] vertices = mesh.vertices;
                if (vertices != null)
                {
                    int numVerts = vertices.Length;

                    //Vector4 tangent = new Vector4(1f, 0f, 0f, -1f);
                    //Vector4 tangent = new Vector4(0f, 1f, 0f, -1f); // Looks pretty good
                    Vector4 tangent = new Vector4(0f, 1f, 0f, 0f);

                    for (int v = 0; v < numVerts; v++)
                    {
                        tangents.Add(tangent);
                    }
                    if (tangents.Count > 0)
                    {
                        mesh.tangents = tangents.ToArray();
                    }
                }
            }

            return mesh;
        }

        /// <summary>
        ///  Derived from Lengyel, Eric. "Computing Tangent Space Basis Vectors for an Arbitrary Mesh". Terathon Software 3D Graphics Library, 2001.
        ///  http://www.terathon.com/code/tangent.html
        /// </summary>
        /// <param name="mesh"></param>
        public static Mesh CalculateTangents(Mesh mesh)
        {
            int[] triangles = mesh.triangles;
            Vector3[] vertices = mesh.vertices;
            Vector2[] uv = mesh.uv;
            Vector3[] normals = mesh.normals;

            //variable definitions
            int triangleCount = triangles.Length;
            int vertexCount = vertices.Length;

            Vector3[] tan1 = new Vector3[vertexCount];
            Vector3[] tan2 = new Vector3[vertexCount];

            Vector4[] tangents = new Vector4[vertexCount];

            for (long a = 0; a < triangleCount; a += 3)
            {
                long i1 = triangles[a + 0];
                long i2 = triangles[a + 1];
                long i3 = triangles[a + 2];

                Vector3 v1 = vertices[i1];
                Vector3 v2 = vertices[i2];
                Vector3 v3 = vertices[i3];

                Vector2 w1 = uv[i1];
                Vector2 w2 = uv[i2];
                Vector2 w3 = uv[i3];

                float x1 = v2.x - v1.x;
                float x2 = v3.x - v1.x;
                float y1 = v2.y - v1.y;
                float y2 = v3.y - v1.y;
                float z1 = v2.z - v1.z;
                float z2 = v3.z - v1.z;

                float s1 = w2.x - w1.x;
                float s2 = w3.x - w1.x;
                float t1 = w2.y - w1.y;
                float t2 = w3.y - w1.y;

                //float r = 1.0f / (s1 * t2 - s2 * t1);
                float div = s1 * t2 - s2 * t1;
                float r = div == 0.0f ? 0.0f : 1.0f / div;

                Vector3 sdir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
                Vector3 tdir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

                tan1[i1] += sdir;
                tan1[i2] += sdir;
                tan1[i3] += sdir;

                tan2[i1] += tdir;
                tan2[i2] += tdir;
                tan2[i3] += tdir;
            }

            for (long a = 0; a < vertexCount; ++a)
            {
                Vector3 n = normals[a];
                Vector3 t = tan1[a];

                Vector3.OrthoNormalize(ref n, ref t);
                tangents[a].x = t.x;
                tangents[a].y = t.y;
                tangents[a].z = t.z;

                tangents[a].w = (Vector3.Dot(Vector3.Cross(n, t), tan2[a]) < 0.0f) ? -1.0f : 1.0f;
            }

            mesh.tangents = tangents;

            return mesh;
        }

        #endregion
    }
}