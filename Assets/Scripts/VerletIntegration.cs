using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PlantGenerator;

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
    public GameObject renderPrefab;
    public Material linesMaterial;
    public bool drawGizmo = true;
    public bool showSpheres = false;

    [Header("Physics settings")]
    public Vector3 gravity = Vector3.up * 0.01f;
    public float friction = 0.99f;
    public float constantJitter = 0.05f;

    [Header("Points settings")]
    public int mainBranchPoints = 20;
    public int maxBranchLevels = 4;
    [Tooltip("By what amount to divide the number of points for the next branch")]
    public float branchItemCountHalvingRatio = 2;
    [Tooltip("By what amount to increase the distance of side branches relative to their parent")]
    public float linearDistancingFromParentFactor = 0.2f;
    [Tooltip("Initial distance of first child branch point from the parent branch")]
    public float distanceAwayFromParentBranch = 1;
    [Tooltip("Value between 0 and 9 inclusive. Accepts one decimal resolution")]
    public float branchingProbability = 4;

    [Header("Line settings")]
    public float startingWidth = 1;
    public float endWidth = 0.2f;

    private Rigidbody playerRb;
    private Vector3 lastPos;
    private Vector3 gizmoPos = Vector3.zero;

    private List<GameObject> instantiated = new List<GameObject>();
    private List<GameObject> lineRenderersParents = new List<GameObject>();
    private List<VerletPoint> simulationPoints = new List<VerletPoint>();
    private List<RigidLine> rigidLines = new List<RigidLine>();
    private List<BranchPointsInfo> branchPointsInterval;

    void Start() {
        playerRb = player.GetComponent<Rigidbody>();
        lastPos = transform.position;

        PlantGenerator plant = new PlantGenerator(mainBranchPoints, maxBranchLevels, branchItemCountHalvingRatio, linearDistancingFromParentFactor, distanceAwayFromParentBranch, branchingProbability);
        plant.Generate();
        plant.LockPoint(0, transform.position);

        simulationPoints = plant.GetVerletPoints();
        rigidLines = plant.GetRigidLines();
        branchPointsInterval = plant.GetBranchPointsIntervals();

        // Create line renderes for each branch
        float widthChangingRatio = endWidth / startingWidth;
        for (int i = 0; i < branchPointsInterval.Count; i++) {
            GameObject newRenderObject = Instantiate(renderPrefab);
            LineRenderer renderer = newRenderObject.GetComponent<LineRenderer>();
            renderer.positionCount = branchPointsInterval[i].pointsIndexes.Length;
            renderer.material = linesMaterial;
            renderer.generateLightingData = true;

            renderer.startWidth = Mathf.Pow(widthChangingRatio, branchPointsInterval[i].order) * startingWidth;
            renderer.endWidth = Mathf.Pow(widthChangingRatio, branchPointsInterval[i].order + 1) * startingWidth;

            lineRenderersParents.Add(newRenderObject);
        }

        for (int i = 0; i < simulationPoints.Count; i++) {
            GameObject newReferenceObject = Instantiate(prefab, simulationPoints[i].pos, Quaternion.identity);
            newReferenceObject.name = $"Sphere {i}";

            newReferenceObject.SetActive(false);
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
        UpdateLineRenderers();
        RecalculateBounds();

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

        if (!drawGizmo) return;

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
            Vector3 dVec = (point.pos - point.oldPos) * friction + Random.insideUnitSphere.normalized * constantJitter;

            if (point.locked) continue;

            point.oldPos = point.pos;
            point.pos += dVec;

            Vector3 denominator = Vector3.one * Mathf.Max(0, point.pos.y) + new Vector3(1 / gravity.x, 1 / gravity.y, 1 / gravity.z);
            Vector3 adjustedGravitiy = new Vector3(1 / denominator.x, 1 / denominator.y, 1 / denominator.z);
            point.pos += adjustedGravitiy;

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

    private void RecalculateBounds() {
        BoxCollider collider = GetComponent<BoxCollider>();
        Vector3 dimensions = Vector3.zero;
        Vector3 center = collider.center;

        for (int i = 0; i < simulationPoints.Count; i++) {
            VerletPoint point = simulationPoints[i];

            dimensions.x = Mathf.Max(dimensions.x, Mathf.Abs(point.pos.x-center.x));
            dimensions.y = Mathf.Max(dimensions.y, Mathf.Abs(point.pos.y - center.z));
            dimensions.z = Mathf.Max(dimensions.z, Mathf.Abs(point.pos.y - center.z));


            simulationPoints[i] = point;
        }

        collider.size = dimensions;
    }

    private void UpdateAttachedObjects() {
        for (int i = 0; i < instantiated.Count; i++) {
            if (showSpheres) {
                if (!instantiated[i].activeInHierarchy) instantiated[i].SetActive(true);
                instantiated[i].transform.position = simulationPoints[i].pos;
            } else if (instantiated[i].activeInHierarchy) instantiated[i].SetActive(false);
        }
    }

    private void UpdateLineRenderers() {
        for (int i = 0; i < lineRenderersParents.Count; i++) {
            LineRenderer renderer = lineRenderersParents[i].GetComponent<LineRenderer>();

            for (int j = 0; j < branchPointsInterval[i].pointsIndexes.Length; j++) {
                renderer.SetPosition(j, simulationPoints[branchPointsInterval[i].pointsIndexes[j]].pos);
            }
        }
    }
}
