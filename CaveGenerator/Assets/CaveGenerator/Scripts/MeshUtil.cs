using System;
using System.Collections;
using System.Collections.Generic;
using g3;
using UnityEngine;

namespace ProceduralCave
{
    public static class MeshUtil
    {
        public static DMesh3 ToDMesh(this Mesh uMesh,
                                     bool bNorm = true,
                                     bool bUV = false,
                                     bool bCol = false)
        {
            int nV = uMesh.vertices.Length;
            bNorm &= (uMesh.normals != null && uMesh.normals.Length == nV);
            bUV &= (uMesh.uv != null && uMesh.uv.Length == nV);
            bCol &= (uMesh.colors != null && uMesh.colors.Length == nV);
         
            DMesh3 dMesh = new DMesh3(bNorm, bCol, bUV);

            for (int i = 0; i < nV; i++)
            {
                NewVertexInfo vi = new NewVertexInfo() { v = uMesh.vertices[i] };
                if (bNorm)
                {
                    vi.bHaveN = true;
                    vi.n = uMesh.normals[i];
                }
                if (bUV)
                {
                    vi.bHaveUV = true;
                    vi.uv = uMesh.uv[i];
                }
                if (bUV)
                {
                    vi.bHaveC = true;
                    vi.c = new Vector3f(uMesh.colors[i].r, 
                                        uMesh.colors[i].g, 
                                        uMesh.colors[i].b);
                }
                int vID = dMesh.AppendVertex(vi);
                Util.gDevAssert(vID == i);
            }

            int nT = uMesh.triangles.Length;
            for (int i = 0; i < nT; i += 3)
            {
                dMesh.AppendTriangle(uMesh.triangles[i],
                                   uMesh.triangles[i + 1],
                                   uMesh.triangles[i + 2]);
            }
            return dMesh;
        }

        public static UnityMesh ToUnityMesh(this DMesh3 dMesh,
                                            bool bNorm = true,
                                            bool bUV = false,
                                            bool bCol = false)
        {
            bNorm &= dMesh.HasVertexNormals; 
            bUV &= dMesh.HasVertexUVs;
            bCol &= dMesh.HasVertexColors;

            int[] vertexMap = new int[dMesh.VerticesBuffer.Length];
            int[] triangleMap = new int[dMesh.TrianglesBuffer.Length];
            int[] triangles = new int[dMesh.TriangleCount * 3];

            List<Vector3d> vertices = new List<Vector3d>();
            List<Vector3f> normals = new List<Vector3f>();
            List<Vector2f> uv = new List<Vector2f>();
            List<Colorf> colors = new List<Colorf>();
            List<int> vertexUseCount = new List<int>();

            NewVertexInfo vInfo = new NewVertexInfo(new Vector3d(),
                                                    new Vector3f(),
                                                    new Vector3f(),
                                                    new Vector2f());

            IEnumerator e = dMesh.TrianglesRefCounts.GetEnumerator();
            int ti = 0;
            while (e.MoveNext())
            {
                int iRef = (int)e.Current;
                Index3i triangle = dMesh.GetTriangle(iRef);
                triangleMap[iRef] = ti; 

                for (int i = 0; i < 3; i++)
                {
                    int vertIndex = triangle[i];
                    if (vertexMap[vertIndex] == 0)
                    {
                        vertexUseCount.Add(1);
                        dMesh.GetVertex(vertIndex, ref vInfo, bNorm, bCol, bUV);
                        vertices.Add(new Vector3f((float)vInfo.v.x,
                                                  (float)vInfo.v.y,
                                                  (float)vInfo.v.z));
                        vertexMap[vertIndex] = vertices.Count - 1;

                        if (bNorm)
                            normals.Add(vInfo.n);
                        if (bUV)
                            uv.Add(vInfo.uv);
                        if (bCol)
                            colors.Add(new Colorf(vInfo.c.x, vInfo.c.y, vInfo.c.z));
                    }
                    else
                    {
                        vertexUseCount[vertexMap[vertIndex]]++;
                    }

                    triangles[ti * 3 + i] = vertexMap[vertIndex];
                }
                ti++;
            }

            UnityMesh uMesh = new UnityMesh(vertexUseCount.ToArray(),
                                            triangles,
                                            vertices,
                                            normals,
                                            uv,
                                            colors);
            
            // Triangle normals and neighbors.
            e = dMesh.TrianglesRefCounts.GetEnumerator();
            while (e.MoveNext())
            {
                int iRef = (int)e.Current;
                int[] nb = dMesh.GetTriNeighbourTris(iRef).array;
                int[] neighbors = new int[3];

                for (int i = 0; i < 3; i++)
                {
                    neighbors[i] = (nb[i] != -1) ? triangleMap[nb[i]] : -1;
                }
                uMesh.AddTriangleInfo(triangleMap[iRef], 
                                  (Vector3f)dMesh.GetTriNormal(iRef), 
                                  neighbors);
            }

            return uMesh;
        }

        private static MeshConstraints meshConstraints = new MeshConstraints();

        public static DMesh3 ReduceTriangles(this DMesh3 dMesh,
                                             int triangleCount,
                                             bool computeNormals = true,
                                             bool fixAllBoundaryEdges = true)
        {
            Reducer reducer = new Reducer(dMesh);
            if (fixAllBoundaryEdges)
            {
                reducer.SetExternalConstraints(meshConstraints);
                MeshConstraintUtil.FixAllBoundaryEdges(reducer.Constraints, dMesh);
            }
            reducer.ReduceToTriangleCount(triangleCount);

            if (computeNormals)
            {
                MeshNormals.QuickCompute(dMesh);
            }

            return dMesh;
        }

        public static DMesh3 ReduceTriangles(this DMesh3 dMesh,
                                             float factor,
                                             bool computeNormals = true,
                                             bool fixAllBoundaryEdges = true)
        {
            return dMesh.ReduceTriangles((int)Math.Floor(dMesh.TriangleCount / factor),
                                        computeNormals,
                                        fixAllBoundaryEdges);
        }
    }
}
