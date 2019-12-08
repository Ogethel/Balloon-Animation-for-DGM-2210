// Landscape Builder. Copyright (c) 2016-2019 SCSM Pty Ltd. All rights reserved.
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace LandscapeBuilder
{
    [System.Serializable]
    public class LBMesh
    {
        /// <summary>
        /// NOTES: Vertex's have position, normal, uv, colour. So, if any of these change, we need a new vert.
        /// i.e. A corner of a cube has 3 verts because it has 3 normals on a corner. A cube has 24 verts not 8.
        /// A 2D plane would have 4 verts but the top of a cube has 12 verts.
        /// </summary>

        #region Enumerations

        #endregion

        #region Variables and Properties
        public Mesh mesh;
        public string title;            // Not always used
        public List<Vector3> verts;
        public List<Vector3> normals;
        public List<int> triangles;
        public List<Vector2> uvs;
        public List<Vector2> uv2s;
        public List<Vector2> uv3s;
        public List<Vector2> uv4s;
        public List<Vector4> tangents;
        public List<Vector4> colours;   // Store Vert colours as Vector4 for ease of serialization
        public Vector3 minExtent;       // Not always populated. Defaults to float.PositiveInfinity - check before using
        public Vector3 maxExtent;       // Not always populated. Defaults to float.NegativeInfinity - check before using

        #endregion

        #region Constructors

        // constructors
        public LBMesh()
        {
            // A Mesh cannot be created outside the main Unity thread and must be called
            // from that is typically inherited from MonoBehaviour
            mesh = null;
            title = string.Empty;
            verts = new List<Vector3>();
            normals = new List<Vector3>();
            triangles = new List<int>();
            uvs = new List<Vector2>();
            uv2s = new List<Vector2>();
            uv3s = new List<Vector2>();
            uv4s = new List<Vector2>();
            tangents = new List<Vector4>();
            minExtent = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            maxExtent = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
        }

        /// <summary>
        /// Clone constructor
        /// </summary>
        /// <param name="lbMesh"></param>
        public LBMesh(LBMesh lbMesh)
        {
            // A Mesh cannot be created outside the main Unity thread and must be called
            // from that is typically inherited from MonoBehaviour
            this.mesh = null;
            this.title = lbMesh.title;
            this.verts = new List<Vector3>(lbMesh.verts);
            this.normals = new List<Vector3>(lbMesh.normals);
            this.triangles = new List<int>(lbMesh.triangles);
            if (lbMesh.uvs != null) { this.uvs = new List<Vector2>(lbMesh.uvs); } else { uvs = new List<Vector2>(); }
            if (lbMesh.uv2s != null) { this.uv2s = new List<Vector2>(lbMesh.uv2s); } else { uv2s = new List<Vector2>(); }
            if (lbMesh.uv3s != null) { this.uv3s = new List<Vector2>(lbMesh.uv3s); } else { uv3s = new List<Vector2>(); }
            if (lbMesh.uv4s != null) { this.uv4s = new List<Vector2>(lbMesh.uv4s); } else { uv4s = new List<Vector2>(); }
            if (lbMesh.tangents != null) { this.tangents = new List<Vector4>(lbMesh.tangents); } else { tangents = new List<Vector4>(); }
            minExtent = lbMesh.minExtent;
            maxExtent = lbMesh.maxExtent;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Is the current mesh data valid? Does it look reasonable?
        /// Validates that there are > 3 verts and it contains triangles,
        /// and UVs.
        /// </summary>
        /// <returns></returns>
        public bool IsMeshDataValid()
        {
            bool isValid = false;

            if (verts != null && triangles != null && uvs != null && normals != null)
            {
                int numVerts = verts.Count;

                isValid = (numVerts >= 3 && triangles.Count > 0 && uvs.Count == numVerts);
            }

            return isValid;
        }

        /// <summary>
        /// By default MinExtent is set to PositiveInfinity. This method determines
        /// if the MinExtent Vector3 has been updated
        /// </summary>
        /// <returns></returns>
        public bool IsMinExtentSet()
        {
            return !(float.IsPositiveInfinity(minExtent.x) || float.IsPositiveInfinity(minExtent.y) || float.IsPositiveInfinity(minExtent.z));
        }

        /// <summary>
        /// By default MaxExtent is set to NegativeInfinity. This method determines
        /// if the MaxExtent Vector3 has been updated
        /// </summary>
        /// <returns></returns>
        public bool IsMaxExtentSet()
        {
            return !(float.IsNegativeInfinity(maxExtent.x) || float.IsNegativeInfinity(maxExtent.y) || float.IsNegativeInfinity(maxExtent.z));
        }

        /// <summary>
        /// Update the mesh with the vert, triangle, normal data.
        /// Recalculate Bounds
        /// Recalculate Normals if required
        /// NOTE: MegaSplat uses color.a, uv4.x, uv4.w
        /// Currently doesn't update uvs as vector4s..
        /// </summary>
        /// <param name="flipNormals"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public bool UpdateMesh(bool flipNormals, bool showErrors)
        {
            bool isSuccess = false;

            // Do some validation
            if (mesh == null) { if (showErrors) { Debug.Log("LBMesh.UpdateMesh - mesh cannot be null"); } }
            else if (verts == null) { if (showErrors) { Debug.Log("LBMesh.UpdateMesh - verts cannot be null"); } }
            else if (verts.Count > 65000) { if (showErrors) { Debug.Log("LBMesh.UpdateMesh - vert count of " + verts.Count + " is greater than 65000"); } }
            else if (normals == null) { if (showErrors) { Debug.Log("LBMesh.UpdateMesh - normals cannot be null"); } }
            else if (uvs == null) { if (showErrors) { Debug.Log("LBMesh.UpdateMesh - uvs cannot be null"); } }
            else if (triangles == null) { if (showErrors) { Debug.Log("LBMesh.UpdateMesh - triangles cannot be null"); } }
            //else if (colours == null) { if (showErrors) { Debug.Log("LBMesh.UpdateMesh - colours cannot be null"); } }
            else
            {
                mesh.Clear();
                mesh.SetVertices(verts);
                mesh.SetTriangles(triangles, 0, true);

                if (colours != null)
                {
                    if (colours.Count > 0)
                    {
                        // Populate an array with the Vector4 "colour" data from lbMesh.
                        UnityEngine.Color[] colorsArray = new Color[colours.Count];
                        if (colorsArray != null)
                        {
                            for (int c = 0; c < colours.Count; c++)
                            {
                                Vector4 colour = colours[c];
                                // TODO A color has direct conversion to vector4 so probably just setting to vector4 would
                                // be faster. Need to test.
                                colorsArray[c] = new Color(colour.x, colour.y, colour.z, colour.w);
                            }
                            mesh.colors = colorsArray;
                        }
                    }
                }

                // If they exist, apply the UVs
                if (uvs != null && uvs.Count > 0) { mesh.SetUVs(0, uvs); }
                if (uv2s != null && uv2s.Count > 0) { mesh.SetUVs(1, uv2s); }
                if (uv3s != null && uv3s.Count > 0) { mesh.SetUVs(2, uv3s); }
                if (uv4s != null && uv4s.Count > 0) { mesh.SetUVs(3, uv4s); }

                // uv3,4 can also be Vector4s...
                if (normals != null && normals.Count > 0) { mesh.SetNormals(normals); }
                else { mesh.RecalculateNormals(); }

                // flip normals on the mesh itself after normals have been either added from LBMesh
                // or by recalculating the normals. Don't update the original LBMesh normals list
                if (flipNormals) { LBMeshOperations.FlipNormals(mesh); }

                // Recalc gets done with SetTriangles() above
                //mesh.RecalculateBounds();

                if (tangents != null && tangents.Count > 0) { mesh.SetTangents(tangents); }
                else
                {
                    //mesh = LBMeshOperations.ReCalculateTangents(mesh);
                    mesh = LBMeshOperations.CalculateTangents(mesh);
                }

                isSuccess = true;
            }

            return isSuccess;
        }

        /// <summary>
        /// Move all verts using an offset and scale them on each axis
        /// If no offset is required, set offset to Vector3.zero
        /// If no scaling is required, set scale to Vector3.one
        /// This is typically called when importing a LBTemplate
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="scale"></param>
        public void MoveVerts(Vector3 offset, Vector3 scale)
        {
            if (verts != null)
            {
                int numVerts = verts.Count;

                for (int v = 0; v < numVerts; v++)
                {
                    // Scale the vertices location, then add the offset
                    verts[v] = new Vector3((verts[v].x * scale.x) + offset.x, (verts[v].y * scale.y) + offset.y, (verts[v].z * scale.z) + offset.z);
                }
            }
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Delete the Unity Mesh class instance and release memory. Clears all LBMesh data but doesn't delete the class instance.
        /// NOTE: Memory will be released by LB back to the available heap within Unity. However, whether or not Unity
        /// releases that memory back to the OS or not is beyond the control of LB.
        /// </summary>
        /// <param name="lbMesh"></param>
        public static void DeleteMesh(LBMesh lbMesh)
        {
            if (lbMesh != null)
            {
                if (lbMesh.mesh != null) { lbMesh.mesh.Clear(false); lbMesh.mesh = null; }
                if (lbMesh.normals != null) { lbMesh.normals.Clear(); lbMesh.normals.TrimExcess(); lbMesh.normals = null; }
                if (lbMesh.tangents != null) { lbMesh.tangents.Clear(); lbMesh.tangents.TrimExcess(); lbMesh.tangents = null; }
                if (lbMesh.uvs != null) { lbMesh.uvs.Clear(); lbMesh.uvs.TrimExcess(); lbMesh.uvs = null; }
                if (lbMesh.uv2s != null) { lbMesh.uv2s.Clear(); lbMesh.uv2s.TrimExcess(); lbMesh.uv2s = null; }
                if (lbMesh.uv3s != null) { lbMesh.uv3s.Clear(); lbMesh.uv3s.TrimExcess(); lbMesh.uv3s = null; }
                if (lbMesh.uv4s != null) { lbMesh.uv4s.Clear(); lbMesh.uv4s.TrimExcess(); lbMesh.uv4s = null; }
                if (lbMesh.triangles != null) { lbMesh.triangles.Clear(); lbMesh.triangles.TrimExcess(); lbMesh.triangles = null; }
                if (lbMesh.verts != null) { lbMesh.verts.Clear(); lbMesh.verts.TrimExcess(); lbMesh.verts = null; }
                System.GC.Collect();
            }
        }

        #endregion
    }

    public struct LBOrientedPoint
    {
        public Vector3 position;
        public Quaternion rotation;

        public LBOrientedPoint(Vector3 position, Quaternion rotation)
        {
            this.position = position;
            this.rotation = rotation;
        }

        public Vector3 LocalToWorld(Vector3 point)
        {
            return position + rotation * point;
        }

        public Vector3 WorldToLocal(Vector3 point)
        {
            return Quaternion.Inverse(rotation) * (point - position);
        }

        public Vector3 LocalToWorldDirection(Vector3 direction)
        {
            return rotation * direction;
        }
    }
}