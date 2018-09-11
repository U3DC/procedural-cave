using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using g3;
using UnityEngine;

namespace ProceduralCave
{
    // Manages chunk generation & destruction.

    public class CaveGenerator : MonoBehaviour
    {
        [SerializeField] Player player;
        [SerializeField] Chunk chunkPrefab;
        [Header("Mesh Generator")]
        [SerializeField] int ringsPerChunk = 25;
        [SerializeField] int ringDetail = 50;
        [SerializeField] float ringWidth = 0.5f;
        [Header("Mesh Processor")]
        [Tooltip("Reduce triangles: factor > 1")]
        [SerializeField] float triangleReductionFactor = 3f;
        [Tooltip("Max smoothing: angle >= 180, No smoothing: angle = 0")]
        [SerializeField] float smoothingThresholdAngle = 10f;

        private Path path;
        private MeshGen meshGen;
        private CrossSection xSec;
        private Queue<Vector3d> qPath;
        private Queue<Polygon2d> qPoly;

        private Vector3 chunkPos;
        private Quaternion chunkRot;
        private bool reduceSmoothing;
        private int nPolys;
        private int bufferSize;
        // Counters start with 1.
        private int stepCount;
        private int chunkCount;

        private bool meshGenPending;
        private UnityMesh tmpMesh;
        private Chunk tmpChunk;
        private List<Vector3> tmpAbsPath;
        private List<Vector3> tmpRelPath;
        private VectorArray3d tmpSeam;

        private void Start()
        {
            path = new Path(ringWidth);
            meshGen = new MeshGen(ringDetail);
            xSec = new CrossSection(ringDetail);
            qPath = new Queue<Vector3d>();
            qPoly = new Queue<Polygon2d>();

            chunkPos = Vector3.zero;
            chunkRot = Quaternion.identity;
            reduceSmoothing = smoothingThresholdAngle < 180f;
            nPolys = ringsPerChunk + 1;
            bufferSize = ringsPerChunk + 2;

            tmpAbsPath = new List<Vector3>();
            tmpRelPath = new List<Vector3>();
            tmpSeam = meshGen.Seam;

            Chunk.InitRecycler(chunkPrefab, transform);
            // Create first chunk.
            BatchUpdate(ringsPerChunk + 3);
        }

        private void Update()
        {
            // Waiting for new mesh.
            if (tmpMesh != null)
            {
                chunkCount++;
                InitializeChunk();
                tmpMesh = null;
                meshGenPending = false;
            }

            // Demo - player is always moving forward.
            if (player.isReady && !meshGenPending)
            {
                if (player.chunk.id > chunkCount - 15)
                {
                    BatchUpdate(ringsPerChunk);
                }

                if (Chunk.nActive > 20)
                {
                    Chunk.Release(transform.GetChild(0).GetComponent<Chunk>());
                }
            }

            // AnimateSharedMaterial();
        }

        // Chunk creation should be agnostic to wether new 
        // path positions are added incrementally at each Update() 
        // or in batches.
        private void BatchUpdate(int nSteps)
        {
            for (int i = 0; i < nSteps; i++)
            {
                StepUpdate();
            }
        }
        private void StepUpdate()
        {
            stepCount++;
            if (UpdateQueue())
            {
                CreateChunk();
            }
        }

        private bool UpdateQueue()
        {
            if (qPath.Count > bufferSize)
            {
                qPath.Dequeue();
                qPoly.Dequeue();
            }
            qPoly.Enqueue(xSec.GetPolygon());
            qPath.Enqueue(path.GetNextPosition());

            return NeedNewChunk();
        }
        private bool NeedNewChunk()
        {
            // 3 because we're passing ringsPerChunk + 3 
            // path positions and polys to the mesh generator:
            // - n polys = n rings + 1
            // - also, we need 2 additional path positions  
            //   for calculating tangents at mesh start & end
            return stepCount % ringsPerChunk == 3 && stepCount > 3;
        }

        private async Task CreateChunk()
        {
            List<Vector3d> lPath = qPath.ToList();
            tmpAbsPath.Clear();
            tmpRelPath.Clear();

            Vector3d endPos = lPath[nPolys];
            float hrzAngle = Vector2.SignedAngle(Vector2.up, (Vector2)endPos.xz) * 2f;
            for (int i = 0; i < lPath.Count; i++)
            {
                tmpAbsPath.Add((Vector3)lPath[i]);
                // Offset and rotate path positions.
                lPath[i] = chunkRot * (Vector3)(lPath[i] - chunkPos);
                tmpRelPath.Add((Vector3)lPath[i]);
            }

            tmpChunk = Chunk.Retrieve(chunkPos, Quaternion.Inverse(chunkRot));
            // Position & rotation offset for next chunk prefab.
            chunkPos = (Vector3)endPos;
            chunkRot = Quaternion.AngleAxis(hrzAngle, Vector3.up);

            for (int i = 0; i < tmpSeam.Count; i++)
            {
                // Seam vertices global to local.
                tmpSeam[i] = tmpChunk.transform.InverseTransformPoint((Vector3)tmpSeam[i]);
            }

            meshGenPending = true;
            tmpMesh = await Task.Run(() => CreateMesh(lPath));
        }

        private void InitializeChunk()
        {
            tmpChunk.Initialize(chunkCount,
                                tmpRelPath.GetRange(1, nPolys),
                                tmpAbsPath.GetRange(1, nPolys),
                                tmpMesh.GetMesh(reduceSmoothing));

            for (int i = 0; i < tmpMesh.seam.Count; i++)
            {
                // Seam vertices local to global.
                tmpSeam[i] = tmpChunk.transform.TransformPoint((Vector3)tmpMesh.seam[i]);
            }
        }

        private UnityMesh CreateMesh(List<Vector3d> lPath)
        {
            DMesh3 dMesh = meshGen.CreateMesh(lPath, qPoly.ToList(), tmpSeam);

            // Defer normal calculation if we reduce smoothing later.
            if (triangleReductionFactor > 1f)
            {
                dMesh.ReduceTriangles(triangleReductionFactor, !reduceSmoothing);
            }
            else if (!reduceSmoothing)
            {
                MeshNormals.QuickCompute(dMesh);
            }

            UnityMesh uMesh = dMesh.ToUnityMesh();
            uMesh.seam = meshGen.Seam; // TODO

            if (reduceSmoothing)
            {
                uMesh.IncreaseVertexCount(smoothingThresholdAngle);
            }

            return uMesh;
        }


        // Demo animation.
        //
        private Material material;
        private float lum;
        private float lumPrev;
        private float lumNext;
        private Oscillator lumOsc;

        private void AnimateSharedMaterial()
        {
            if (material == null)
            {
                if (chunkCount > 0)
                {
                    lumOsc = new Oscillator(300f, 400f);
                    material = transform.GetChild(0).GetComponent<Renderer>().sharedMaterial;
                }
            }
            else
            {
                if (!lumOsc.MoveNext())
                {
                    lumPrev = lumNext;
                    lumNext = Random.Range(-0.4f, 0.4f);
                }
                lum = Mathf.Lerp(lumPrev, lumNext, lumOsc.Current);
                material.color = Color.HSVToRGB(0.085f, 0.3f, 0.4f + lum);
                material.SetFloat("_Glossiness", 0.4f - lum);
                material.SetFloat("_Metallic", 0.5f - lum * 0.5f);
            }
        }
    }
}
