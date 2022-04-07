using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VerletIntegration : MonoBehaviour {
    public struct VerletPoint {
        public Vector3 oldPos;
        public Vector3 pos;
        public bool locked;
        public bool reactToCollisions;


        public VerletPoint(Vector3 pos, bool locked = false, bool reactToCollisions = false) {
            this.oldPos = pos;
            this.pos = pos;
            this.locked = locked;
            this.reactToCollisions = reactToCollisions;
        }
    };

    public struct RigidLine {
        public int pIndex1;
        public int pIndex2;
        public float distance;

        public RigidLine(int pIndex1, int pIndex2, float distance = 1) {
            this.pIndex1 = pIndex1;
            this.pIndex2 = pIndex2;
            this.distance = distance;
        }
    }


    public GameObject player;
    public GameObject prefab;
    public Vector3 gravity = Vector3.up * 0.01f;
    public float friction = 0.99f;

    List<GameObject> instantiated = new List<GameObject>();
    List<VerletPoint> simulationPoints = new List<VerletPoint>();
    List<RigidLine> rigidLines = new List<RigidLine>();

    private Rigidbody playerRb;
    private Vector3 lastPos;
    private Vector3 gizmoPos = Vector3.zero;



    void Start() {
        playerRb = player.GetComponent<Rigidbody>();
        lastPos = transform.position;

        PlantGenerator plant = new PlantGenerator(8, 2);
        plant.Generate();
        plant.LockPoint(0, new Vector3(0, 1, 0));

        simulationPoints = plant.GetVerletPoints();
        rigidLines = plant.GetRigidLines();
        for (int i = 0; i < simulationPoints.Count; i++) {
            GameObject newReferenceObject = Instantiate(prefab, simulationPoints[i].pos, Quaternion.identity);
            newReferenceObject.name = $"Sphere {i}";
            instantiated.Add(newReferenceObject);
        }
    }

    void Update() {
        UpdatePoints();

        for (int i = 0; i < 5; i++) {
            UpdateSticks();
            ApplyConstraints();
        }

        UpdateAttachedObjects();

        //for (int i = 0; i < instantiated.Count; i++) {
        //    RaycastHit hit;
        //    Vector3 direction = Vector3.up;
        //    float radius = instantiated[i].transform.localScale.x;
        //    float maxDistance = 1.2f;

        //    // Can't use OverlapSphere because that method does not release normal and hit point information
        //    //if (Physics.SphereCast(instantiated[i].transform.position, radius, direction, out hit, maxDistance)) {
        //    //    gizmoPos = hit.point;
        //    //}

        //    Collider[] colliders = Physics.OverlapSphere(instantiated[i].transform.position, radius);
        //    for (int j = 0; j < colliders.Length; j++) {
        //        Vector3 closestPoint = colliders[j].ClosestPoint(instantiated[i].transform.position);
        //        Ray ray = new Ray(instantiated[i].transform.position, closestPoint - instantiated[i].transform.position);
        //        if (Physics.Raycast(ray, out hit, maxDistance)) {
        //            gizmoPos = hit.point;

        //            // Use this information to push points back
        //        }
        //    }
        //}
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(gizmoPos, 0.2f);

        for (int i = 0; i < rigidLines.Count; i++) {
            RigidLine line = rigidLines[i];
            VerletPoint p1 = simulationPoints[line.pIndex1];
            VerletPoint p2 = simulationPoints[line.pIndex2];
            Gizmos.DrawLine(p1.pos, p2.pos);
        }
    }

    private void OnTriggerEnter(Collider other) {
        Debug.Log("Trigger enter");
    }

    private void OnCollisionEnter(Collision collision) {
        Debug.Log($"Collision enter with {collision.collider.name}");
    }

    private void UpdatePoints() {
        for (int i = 0; i < simulationPoints.Count; i++) {
            VerletPoint point = simulationPoints[i];
            Vector3 dVec = (point.pos - point.oldPos) * friction;

            if (point.locked) continue;

            point.oldPos = point.pos;
            point.pos += dVec;
            point.pos += gravity;

            simulationPoints[i] = point;
        }
    }
    private void UpdateSticks() {
        for (int i = 0; i < rigidLines.Count; i++) {
            RigidLine line = rigidLines[i];
            VerletPoint p1 = simulationPoints[line.pIndex1];
            VerletPoint p2 = simulationPoints[line.pIndex2];

            float currentDistance = Vector3.Distance(p1.pos, p2.pos);
            Vector3 dPos = p2.pos - p1.pos;
            Vector3 correctionPerAxis;

            // Edge case when two points are overlapping
            if (currentDistance < 0.001f) {
                // We separate them on some random axis to avoid symmetries in the visualization
                Vector3 separationAxis = Random.insideUnitSphere.normalized;

                // We multiply by minus one because we expect normaly the correctionAxis to be negative
                // and this is taken into account when correcting each point as p1 is substracted instead of adding
                // However visually we are moving the p1 point to the right and p2 to the left when p1<p2
                correctionPerAxis = separationAxis * (-line.distance);
            } else {
                // By how much the line stretched
                float extensionPercent = (line.distance - currentDistance) / currentDistance;

                // Calculate the extension per component axis
                correctionPerAxis = dPos * extensionPercent;
            }

            // Correct each point
            if (p1.locked && p2.locked) continue;
            if (p1.locked) {
                p2.pos += correctionPerAxis;
            } else if (p2.locked) {
                p1.pos -= correctionPerAxis;
            } else {
                p1.pos -= correctionPerAxis / 2;
                p2.pos += correctionPerAxis / 2;
            }

            // Update results
            simulationPoints[line.pIndex1] = p1;
            simulationPoints[line.pIndex2] = p2;
        }
    }

    private void ApplyConstraints() {
        for (int i = 0; i < simulationPoints.Count; i++) {
            VerletPoint point = simulationPoints[i];
            Vector3 dVec = point.pos - point.oldPos;




            simulationPoints[i] = point;
        }
    }

    private void UpdateAttachedObjects() {
        for (int i = 0; i < instantiated.Count; i++) {
            instantiated[i].transform.position = simulationPoints[i].pos;
        }
    }
}
