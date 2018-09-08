using System.Collections.Generic;
using g3;
using UnityEngine;

namespace ProceduralCave
{
    public class Chunk : MonoBehaviour
    {
        [HideInInspector] public int id;
        [HideInInspector] public Vector3 absCenter;
        // First path pos of this chunk is
        // last path pos of previous chunk.
        [HideInInspector] public List<Vector3> absPath;

        [SerializeField] Transform rocksPrefab;
        private int nRocks;

        private List<Vector3> relPath;
        private Vector3 relCenter;
       
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

            // Add a bunch of rocks.
            nRocks = (int)Random.Range(0f, 20f);
        }

        public void Destroy()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
            Destroy(GetComponent<MeshFilter>().mesh);
            Destroy(GetComponent<MeshCollider>().sharedMesh);
            Destroy(gameObject);
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
