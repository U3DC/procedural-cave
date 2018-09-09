using System.Collections.Generic;
using g3;
using UnityEngine;

namespace ProceduralCave
{
    public class Chunk : MonoBehaviour
    {
        private static Stack<Chunk> inactive;
        private static Chunk prefab;
        private static Transform parent;

        public static void InitRecycler(Chunk prefab, Transform parent)
        {
            Chunk.prefab = prefab;
            Chunk.parent = parent;
            inactive = new Stack<Chunk>();
        }

        public static Chunk Retrieve(Vector3 pos, Quaternion rot)
        {
            Chunk chunk = inactive.Count > 0 ? inactive.Pop() : Instantiate(prefab, parent);
            chunk.transform.SetAsLastSibling();
            chunk.transform.position = pos;
            chunk.transform.rotation = rot;
            return chunk;
        }

        public static int Release(Chunk chunk)
        {
            chunk.Clear();
            inactive.Push(chunk);
            return inactive.Count;
        }

        [HideInInspector] public int id;
        [HideInInspector] public Vector3 absCenter;
        // First path pos of this chunk is
        // last path pos of previous chunk.
        [HideInInspector] public List<Vector3> absPath;

        [SerializeField] Transform rocksPrefab;
        private int nRocks;

        private List<Vector3> relPath;
        private Vector3 relCenter;

        private void Awake()
        {
            gameObject.SetActive(false);
        }

        public void Initialize(int id,
                               List<Vector3> relPath,
                               List<Vector3> absPath,
                               Mesh mesh)
        {
            this.id = id;
            this.name = "Chunk#" + id;
            this.relPath = relPath;
            this.absPath = absPath;
            relCenter = mesh.bounds.center;
            absCenter = transform.position + relCenter;

            GetComponent<MeshFilter>().mesh = mesh;
            GetComponent<MeshCollider>().sharedMesh = mesh;
            gameObject.SetActive(true);

            // Add a bunch of rocks.
            nRocks = (int)Random.Range(0f, 20f);
        }

        public void Clear()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
            Destroy(GetComponent<MeshFilter>().mesh);
            Destroy(GetComponent<MeshCollider>().sharedMesh);
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (id > 5 && transform.childCount < nRocks)
            {
                int iR = (int)Random.Range(0f, 7f);
                int iP = (int)Random.Range(0f, absPath.Count);
                Instantiate(rocksPrefab.GetChild(iR), absPath[iP], Random.rotation, transform);
            }
        }
    }
}
