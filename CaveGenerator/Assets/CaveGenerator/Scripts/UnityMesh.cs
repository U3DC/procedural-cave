using System.Collections.Generic;
using System.Linq;
using g3;
using UnityEngine;

namespace ProceduralCave
{
    public class TriangleInfo
    {
        public int index;
        public Vector3f normal;
        public int[] neighbors;

        public TriangleInfo(int index, Vector3f normal, int[] neighbors)
        {
            this.index = index;
            this.normal = normal;
            this.neighbors = neighbors;
        }
    }

    // Stand-in for Unity's Mesh class, created in sub-thread.

    public class UnityMesh
    {
        // TODO Doesn't really belong here, seam vertices refer to the DMesh.
        public VectorArray3d seam;


        private List<TriangleInfo> triangleInfo;
        private int[] vertexUseCount;
        private int[] triangles;
        private List<Vector3d> vertices;
        private List<Vector3f> normals;
        private List<Vector2f> uv;
        private List<Colorf> colors;

        public UnityMesh(int[] vertexUseCount,
                         int[] triangles,
                         List<Vector3d> vertices,
                         List<Vector3f> normals = null,
                         List<Vector2f> uv = null,
                         List<Colorf> colors = null)
        {
            this.vertexUseCount = vertexUseCount;
            this.triangles = triangles;
            this.vertices = vertices;
            this.normals = normals;
            this.uv = uv;
            this.colors = colors;
            triangleInfo = new List<TriangleInfo>();
        }

        public Mesh GetMesh(bool recalculateNormals = false)
        {
            Mesh mesh = new Mesh();
          
            // TODO - Casting via
            // vertices.Cast<Vector3>().ToArray()
            // doesn't seem to work here.

            int n = vertices.Count;
            Vector3[] _vertices = new Vector3[n]; 
            for (int i = 0; i < n; i++)
                _vertices[i] = (Vector3)vertices[i];
            mesh.vertices = _vertices;

            n = normals.Count;
            if (n > 0 && !recalculateNormals)
            {
                Vector3[] _normals = new Vector3[n];
                for (int i = 0; i < n; i++)
                    _normals[i] = normals[i];
                mesh.normals = _normals;
            }

            n = uv.Count;
            if (n > 0)
            {
                Vector2[] _uv = new Vector2[n];
                for (int i = 0; i < n; i++)
                    _uv[i] = uv[i];
                mesh.uv = _uv;
            }

            n = colors.Count;
            if (n > 0)
            {
                Color[] _colors = new Color[n];
                for (int i = 0; i < n; i++)
                    _colors[i] = colors[i];
                mesh.colors = _colors;
            }

            mesh.triangles = triangles;

            if (recalculateNormals)
                mesh.RecalculateNormals();
            
            return mesh;
        }

        public void AddTriangleInfo(int index, Vector3f normal, int[] neighbors)
        {
            triangleInfo.Add(new TriangleInfo(index, normal, neighbors));
        }

        // Selectively reduce surface smoothing.
        //
        // Brute force comparison of angles between neighboring triangles' normals.
        // Triangles must not share vertices if that angle is > thresholdAngle.
        //
        // triangleInfo and vertexUseCount will be outdated after this operation.
        //
        // TODO: Optimize.
        //
        public void IncreaseVertexCount(float thresholdAngle = 0f)
        {
            List<Vector3d> tmpVertices = new List<Vector3d>(vertices);
            //List<Vector3f> tmpNormals = new List<Vector3f>(normals);
            List<Vector2f> tmpUV = new List<Vector2f>(uv);
            List<Colorf> tmpcolors = new List<Colorf>(colors);
            List<int> tmpTriangles = new List<int>(triangles);
            bool bUV = uv.Count > 0;
            bool bCol = colors.Count > 0;

            if (thresholdAngle > 0f)
            {
                foreach (TriangleInfo triangle in triangleInfo)
                {
                    if (NeedDistinctNormal(triangle, thresholdAngle))
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            int ti = triangle.index * 3 + i;
                            int vi = triangles[ti];

                            if (vertexUseCount[vi] > 1)
                            {
                                tmpTriangles[ti] = tmpVertices.Count;
                                //tmpNormals.Add(triangle.normal);
                                tmpVertices.Add(vertices[vi]);

                                if (bUV)
                                    tmpUV.Add(uv[vi]);
                                if (bCol)
                                    tmpcolors.Add(colors[vi]);

                                vertexUseCount[vi]--;
                            }
                            //else
                            //{
                            //    tmpNormals[vi] = triangle.normal;
                            //}
                        }
                    }
                }
            }
            else
            {
                // Zero smoothing.
                // Every triangle gets its dedicated vertices.

                tmpVertices.Clear();
                //tmpNormals.Clear();
                tmpUV.Clear();
                tmpcolors.Clear();
                tmpTriangles.Clear();

                foreach (TriangleInfo triangle in triangleInfo)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        int ti = triangle.index * 3 + i;
                        int vi = triangles[ti];

                        tmpTriangles.Add(tmpVertices.Count);
                        //tmpNormals.Add(triangle.normal);
                        tmpVertices.Add(vertices[vi]);

                        if (bUV)
                            tmpUV.Add(uv[vi]);
                        if (bCol)
                            tmpcolors.Add(colors[vi]);
                    }
                }
            }

            vertices = tmpVertices;
            //normals = tmpNormals;
            uv = tmpUV;
            colors = tmpcolors;
            triangles = tmpTriangles.ToArray();
        }

        private bool NeedDistinctNormal(TriangleInfo tInfo, float thresholdAngle)
        {
            for (int i = 0; i < 3; i++)
            {
                int t = tInfo.neighbors[i];
                if (t != -1)
                {
                    if (tInfo.normal.AngleD(triangleInfo[t].normal) > thresholdAngle)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
