using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VerletIntegration : MonoBehaviour {
    struct VerletPoint {
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

    struct RigidLine {
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
    public Vector3 gravity = Vector3.up * 0.05f;
    public float friction = 0.99f;

    List<GameObject> instantiated = new List<GameObject>();
    List<VerletPoint> simulationPoints = new List<VerletPoint>();
    List<RigidLine> constraintLines = new List<RigidLine>();

    private Rigidbody playerRb;
    private Vector3 lastPos;
    private Vector3 gizmoPos = Vector3.zero;



    void Start() {
        playerRb = player.GetComponent<Rigidbody>();
        lastPos = transform.position;

        simulationPoints.Add(new VerletPoint(new Vector3(0, 1, 0)));
        simulationPoints.Add(new VerletPoint(new Vector3(0, 2, 0)));
        simulationPoints.Add(new VerletPoint(new Vector3(0, 3, 0)));
        simulationPoints.Add(new VerletPoint(new Vector3(0, 4, 0)));

        constraintLines.Add(new RigidLine(0, 1));
        constraintLines.Add(new RigidLine(1, 2));
        constraintLines.Add(new RigidLine(2, 3));

        for (int i = 0; i < simulationPoints.Count; i++) {
            instantiated.Add(Instantiate(prefab, simulationPoints[i].pos, Quaternion.identity));
        }
    }

    void Update() {
        UpdatePoints();
        UpdateSticks();
        ApplyConstraints();


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

            point.oldPos = point.pos;
            point.pos += dVec;
            point.pos += gravity;


            simulationPoints[i] = point;
        }
    }
    private void UpdateSticks() {
        for (int i = 0; i < constraintLines.Count; i++) {
            RigidLine line = constraintLines[i];
            VerletPoint p1 = simulationPoints[line.pIndex1];
            VerletPoint p2 = simulationPoints[line.pIndex2];

            float currentDistance = Vector3.Distance(p1.pos, p2.pos);
            Vector3 dPos = p2.pos - p1.pos;

            // By how much the line stretched
            float extensionPercent = (currentDistance - line.distance) / line.distance;

            // Calculate the extension per component axis
            Vector3 correctionPerAxis = dPos * (extensionPercent / 2);

            // Correct each point
            p1.pos += correctionPerAxis;
            p2.pos -= correctionPerAxis;

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
