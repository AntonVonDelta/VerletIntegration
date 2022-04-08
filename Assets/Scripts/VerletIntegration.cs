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
    struct ColliderPoint {
        public int pointIndex;
        public GameObject holderObj;
    }


    public GameObject spherePrefab;
    public GameObject lineHolderPrefab;
    public Material linesMaterial;
    public bool drawGizmo = true;

    private bool lastShowColliders = false;
    public bool showColliders = false;

    [Header("Physics settings")]
    public Vector3 gravity = Vector3.up * 0.001f;
    public float friction = 0.99f;
    public float constantJitter = 0.05f;

    [Header("Points settings")]
    public int mainBranchPoints = 50;
    public int maxBranchLevels = 3;
    [Tooltip("By what amount to divide the number of points for the next branch")]
    public float branchItemCountHalvingRatio = 2;
    [Tooltip("The distance between consecutive points of the main branch")]
    public float intraBranchPointsDistance = 1f;
    [Tooltip("By what amount to increase the distance of side branches relative to their parent")]
    public float interBranchLinearDistanceFactor = 0.2f;
    [Tooltip("Initial distance of first child branch point from the parent branch")]
    public float distanceAwayFromParentBranch = 0.2f;
    [Tooltip("Value between 0 and 9 inclusive. Accepts one decimal resolution")]
    public float branchingProbability = 4;

    [Header("Collision settings")]
    public float maxRayDistance = 0.4f;

    [Header("Line settings")]
    public float startingWidth = 1;
    public float endWidth = 0.6f;

    private Vector3 gizmoPos = Vector3.zero;
    private List<ColliderPoint> colliderInstances = new List<ColliderPoint>();
    private List<GameObject> lineRenderersParents = new List<GameObject>();
    private List<VerletPoint> simulationPoints = new List<VerletPoint>();
    private List<RigidLine> rigidLines = new List<RigidLine>();
    private List<BranchPointsInfo> branchPointsInterval;

    void Start() {
        PlantGenerator plant = new PlantGenerator(mainBranchPoints, maxBranchLevels, branchItemCountHalvingRatio,
            intraBranchPointsDistance, interBranchLinearDistanceFactor, distanceAwayFromParentBranch, branchingProbability);
        plant.Generate();
        plant.LockPoint(0, transform.position);

        simulationPoints = plant.GetVerletPoints();
        rigidLines = plant.GetRigidLines();
        branchPointsInterval = plant.GetBranchPointsIntervals();

        // Create line renderes for each branch
        float widthChangingRatio = endWidth / startingWidth;
        for (int i = 0; i < branchPointsInterval.Count; i++) {
            GameObject newRenderObject = Instantiate(lineHolderPrefab);
            LineRenderer renderer = newRenderObject.GetComponent<LineRenderer>();
            renderer.positionCount = branchPointsInterval[i].pointsIndexes.Length;
            renderer.material = linesMaterial;
            renderer.generateLightingData = true;

            renderer.startWidth = Mathf.Pow(widthChangingRatio, branchPointsInterval[i].order) * startingWidth;
            renderer.endWidth = Mathf.Pow(widthChangingRatio, branchPointsInterval[i].order + 1) * startingWidth;

            lineRenderersParents.Add(newRenderObject);
        }

        CreateColliderInstances();

        // Set up collider to be only Trigger
        GetComponent<BoxCollider>().isTrigger = true;
    }

    void Update() {
        UpdatePoints();

        for (int i = 0; i < 5; i++) {
            UpdateSticks();
            ApplyConstraints();
        }

        UpdateColliderInstances();
        UpdateLineRenderers();
        RecalculateBounds();

        // Handle UI option
        if (lastShowColliders != showColliders) {
            lastShowColliders = showColliders;

            for (int i = 0; i < colliderInstances.Count; i++) {
                MeshRenderer renderer = colliderInstances[i].holderObj.GetComponent<MeshRenderer>();
                renderer.enabled = showColliders;
            }
        }
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

    private void OnTriggerStay(Collider other) {
        for (int i = 0; i < colliderInstances.Count; i++) {
            // ClosestPoint works only with convex colliders - as noted on https://docs.unity3d.com/ScriptReference/Physics.ClosestPoint.html
            // ComputePenetration only works with convex colliders

            // Get closest point on the aproaching surface
            Vector3 closestPoint = other.ClosestPoint(colliderInstances[i].holderObj.transform.position);

            if ((closestPoint - colliderInstances[i].holderObj.transform.position).sqrMagnitude < Mathf.Epsilon) {
                // The trigger point is inside the collider
            } else {
                // Cast an ray in order to get normal and other information
                Ray ray = new Ray(colliderInstances[i].holderObj.transform.position, closestPoint - colliderInstances[i].holderObj.transform.position);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, maxRayDistance)) {
                    gizmoPos = hit.point;

                    // Use this information to push points back
                    VerletPoint point = simulationPoints[colliderInstances[i].pointIndex];
                    point.pos += hit.normal * maxRayDistance;
                    simulationPoints[colliderInstances[i].pointIndex] = point;
                }
            }
        }
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

    private void CreateColliderInstances() {
        for (int i = 0; i < simulationPoints.Count; i++) {
            // Do not create collider "instances" for locked points because they cannot be moved
            if (simulationPoints[i].locked) continue;

            GameObject newReferenceObject = Instantiate(spherePrefab, simulationPoints[i].pos, Quaternion.identity);
            MeshRenderer renderer = newReferenceObject.GetComponent<MeshRenderer>();

            // Initial values
            renderer.enabled = showColliders;
            newReferenceObject.name = $"Sphere {i}";

            colliderInstances.Add(new ColliderPoint { pointIndex = i, holderObj = newReferenceObject });
        }
    }

    private void RecalculateBounds() {
        BoxCollider collider = GetComponent<BoxCollider>();
        Vector3 dimensionsMax = -1000 * Vector3.one;
        Vector3 dimensionsMin = 1000 * Vector3.one;

        for (int i = 0; i < simulationPoints.Count; i++) {
            VerletPoint point = simulationPoints[i];

            dimensionsMax.x = Mathf.Max(dimensionsMax.x, point.pos.x);
            dimensionsMax.y = Mathf.Max(dimensionsMax.y, point.pos.y);
            dimensionsMax.z = Mathf.Max(dimensionsMax.z, point.pos.z);

            dimensionsMin.x = Mathf.Min(dimensionsMin.x, point.pos.x);
            dimensionsMin.y = Mathf.Min(dimensionsMin.y, point.pos.y);
            dimensionsMin.z = Mathf.Min(dimensionsMin.z, point.pos.z);
        }

        // The center and size are defined relative to the gameobject aka they are "added" to its transform
        collider.center = (dimensionsMax + dimensionsMin) / 2 - gameObject.transform.position;
        collider.size = dimensionsMax - dimensionsMin;
    }

    private void UpdateColliderInstances() {
        for (int i = 0; i < colliderInstances.Count; i++) {
            colliderInstances[i].holderObj.transform.position = simulationPoints[colliderInstances[i].pointIndex].pos;
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
