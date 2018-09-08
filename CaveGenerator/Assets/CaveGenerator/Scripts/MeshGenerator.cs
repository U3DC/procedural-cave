using System.Collections.Generic;
using g3;

namespace ProceduralCave
{
    // A modified Geometry3Sharp.mesh_generators.TubeGenerator

    public class MeshGen : MeshGenerator
    {
        public VectorArray3d Seam;
        private Frame3f frame = Frame3f.Identity;

        public MeshGen(int nPolyVertices)
        {
            int n = nPolyVertices + 1;
            Seam = new VectorArray3d(n);
        }

        public DMesh3 CreateMesh(List<Vector3d> path, 
                                 List<Polygon2d> polys,
                                 VectorArray3d seam)
        {
            // Ignore first and last path/polys for mesh generation.
            // We just need the extra path positions to calculate a  
            // continuous tangent at the seams.
            List<Vector3d> pathXZ = new List<Vector3d>();
            for (int i = 0; i < path.Count; i++)
                pathXZ.Add(new Vector3d(path[i].x, 0, path[i].z));

            int nVerts = path.Count - 2;
            int nPolys = polys.Count - 2;
            // Same VertexCount for all Polygons.
            int nSlices = polys[0].VertexCount; 
            int nPolySize = nSlices + 1;
            int nVecs = nVerts * nPolySize;

            vertices = new VectorArray3d(nVecs);
            normals = new VectorArray3f(nVecs);
            uv = new VectorArray2f(nVecs);

            int quad_strips = nVerts - 1;
            int nSpanTris = quad_strips * (2 * nSlices);
            triangles = new IndexArray3i(nSpanTris);

            Frame3f fCur = new Frame3f(frame);
            double pathLength = CurveUtils.ArcLength(path.GetRange(1, nVerts));
            double accum_path_u = 0;

            for (int ri = 0; ri < nPolys; ++ri)
            {
                int si = ri + 1; // actual path/polys index for mesh
                Vector3d tangent = CurveUtils.GetTangent(pathXZ, si);
                fCur.Origin = (Vector3f)path[si];
                fCur.AlignAxis(2, (Vector3f)tangent);

                int nStartR = ri * nPolySize;
                double accum_ring_v = 0;
                bool copy = ri == nPolys - 1;
                bool paste = ri == 0;

                for (int j = 0; j < nPolySize; ++j)
                {
                    int k = nStartR + j;
                    Vector2d pv = polys[si].Vertices[j % nSlices];
                    Vector2d pvNext = polys[si].Vertices[(j + 1) % nSlices];
                    Vector3d v = fCur.FromPlaneUV((Vector2f)pv, 2);
                    vertices[k] = v;
                    Vector3f n = (Vector3f)(v - fCur.Origin).Normalized;
                    normals[k] = n; 
                    uv[k] = new Vector2f(accum_path_u, accum_ring_v);
                    accum_ring_v += (pv.Distance(pvNext) / polys[si].ArcLength);
                    if (copy)
                        Seam[j] = vertices[k];
                    else if (paste)
                        vertices[k] = seam[j];
                }
                double d = path[si].Distance(path[si + 1]);
                accum_path_u += d / pathLength;
            }

            int nStop = nVerts - 1;
            int ti = 0;
            for (int ri = 0; ri < nStop; ++ri)
            {
                int r0 = ri * nPolySize;
                int r1 = r0 + nPolySize;
                for (int k = 0; k < nPolySize - 1; ++k)
                {
                    triangles.Set(ti++, r0 + k, r0 + k + 1, r1 + k + 1, Clockwise);
                    triangles.Set(ti++, r0 + k, r1 + k + 1, r1 + k, Clockwise);
                }
            }
            return MakeDMesh();
        }

        override public MeshGenerator Generate()
        {
            return this;
        }
    }
}
