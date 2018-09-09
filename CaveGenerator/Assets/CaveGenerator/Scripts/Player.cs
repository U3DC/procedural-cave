using UnityEngine;

namespace ProceduralCave
{
    // In the demo, the player is just a sphere 
    // that's being pushed along the current chunk's direction.
    public class Player : MonoBehaviour
    {
        [HideInInspector] public bool isReady;
        [HideInInspector] public Chunk chunk;
        [SerializeField] Camera cam;

        private Rigidbody rb;
        private Vector3 velocity;
        private Vector3 direction;
        private int iPath;

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            RaycastHit hit;
            Ray ray = new Ray(transform.position, Vector3.down);
            if (Physics.Raycast(ray, out hit, 5f, 1 << 8))
            {
                isReady = true;
                chunk = hit.collider.GetComponent<Chunk>();
                
                if (rb.velocity.magnitude < 10f)
                    rb.AddForce(direction * 20f, ForceMode.Acceleration);
                else if (rb.velocity.magnitude > 15f)
                    rb.AddForce(direction * -20f, ForceMode.Acceleration);
            }
        }

        // Cam follows path.
        private void LateUpdate()
        {
            if (isReady)
            {
                iPath = GetClosestPathIndex(transform.position);
                direction = GetPathDirection(iPath);

                cam.transform.position = Vector3.Lerp(cam.transform.position, 
                                                      chunk.absPath[iPath], 0.1f);
                cam.transform.rotation = Quaternion.Lerp(cam.transform.rotation,
                                         Quaternion.LookRotation(direction), 0.1f);
            }
        }

        private int GetClosestPathIndex(Vector3 p, int iL = 0, int iR = -1)
        {
            iR = (iR == -1) ? chunk.absPath.Count - 1 : iR;
            bool bL = Vector3.Distance(p, chunk.absPath[iL]) 
                             < Vector3.Distance(p, chunk.absPath[iR]);
            if (iR - iL == 1)
                return bL ? iL : iR;
            int iM = iL + (iR - iL) / 2;
            return GetClosestPathIndex(p, bL ? iL : iM, bL ? iM : iR);
        }

        private Vector3 GetPathDirection(int i)
        {
            if (i == 0)
                return (chunk.absPath[1] - chunk.absPath[0]).normalized;
            return (chunk.absPath[i] - chunk.absPath[i - 1]).normalized;
        }
    }
}
