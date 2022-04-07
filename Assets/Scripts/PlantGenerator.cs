using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static VerletIntegration;

public class PlantGenerator {
    private List<VerletPoint> simulationPoints = new List<VerletPoint>();
    private List<RigidLine> rigidLines = new List<RigidLine>();
    private int mainBranchSize;
    private int maxBranching;
    private float halvingRatio;
    private float branchLinearDistanceFactor;
    private int uniqueIndex = 0;

    public PlantGenerator(int mainBranchSize, int maxBranching, float halvingRatio, float branchLinearDistanceFactor) {
        this.mainBranchSize = mainBranchSize;
        this.maxBranching = maxBranching;
        this.halvingRatio = halvingRatio;
        this.branchLinearDistanceFactor = branchLinearDistanceFactor;
    }

    public void Generate() {
        Branch mainBranch = new Branch(uniqueIndex, mainBranchSize);
        Queue<Branch> pendingBranches = new Queue<Branch>();
        Queue<Branch> newBranches = new Queue<Branch>();

        pendingBranches.Enqueue(mainBranch);
        uniqueIndex += mainBranch.GetNodeCount();

        // Add points and constraint lines
        simulationPoints.AddRange(mainBranch.GetVerletPoints());
        rigidLines.AddRange(mainBranch.GetRigidLines());


        for (int i = 0; i < maxBranching; i++) {
            while (pendingBranches.Count != 0) {
                Branch currentBranch = pendingBranches.Dequeue();

                for (int j = 0; j < currentBranch.GetNodeCount(); j++) {
                    int rand = Random.Range(0, 9);
                    float nodeProbability = Mathf.Lerp(0, 9, (float)j / (currentBranch.GetNodeCount() - 1));

                    if (rand < nodeProbability) {
                        int newBranchNodeCount = Mathf.Min(currentBranch.GetNodeCount() - j - 1, (int)(currentBranch.GetNodeCount() / halvingRatio));
                        if (newBranchNodeCount == 0) continue;

                        float x = Mathf.Sqrt(1 * 1 + branchLinearDistanceFactor * branchLinearDistanceFactor);

                        Branch newBranch = new Branch(uniqueIndex, newBranchNodeCount, x);
                        uniqueIndex += newBranchNodeCount;
                        currentBranch.AddChildBranch(j, newBranch);

                        newBranches.Enqueue(newBranch);

                        // Add points and constraint lines
                        simulationPoints.AddRange(newBranch.GetVerletPoints());
                        rigidLines.AddRange(newBranch.GetRigidLines());
                        CrossConnectTwoChains(currentBranch, j, newBranch, 1f * Mathf.Sqrt(2), branchLinearDistanceFactor);

                        return;
                    }
                }
            }


            pendingBranches = newBranches;
            newBranches = new Queue<Branch>();
        }

    }

    public void LockPoint(int index, Vector3 position) {
        VerletPoint point = simulationPoints[index];
        point.pos = position;
        point.locked = true;
        simulationPoints[index] = point;
    }

    public List<VerletPoint> GetVerletPoints() {
        return simulationPoints;
    }

    public List<RigidLine> GetRigidLines() {
        return rigidLines;
    }

    /// <summary>
    /// Connect two chains starting and ending at the given indexes
    /// Cross1 controls whether to insert a diagonal line from first to second chain to form a rigid structure
    /// Cross2 controls the other direction. Together they make an X inside a square
    /// </summary>
    private void CrossConnectTwoChains(int startIndex1, int finalIndex1, int startIndex2, int finalIndex2, float distance, float linearIncreaseFactor) {
        int maxIndexOffset = Mathf.Min(finalIndex1 - startIndex1, finalIndex2 - startIndex2);
        for (int i = 0; i < maxIndexOffset + 1; i++) {
            float adjustedDistance = Distance(distance, linearIncreaseFactor, i);

            rigidLines.Add(new RigidLine(startIndex1 + i, startIndex2 + i, adjustedDistance));

            if (i != maxIndexOffset) {
                float diagonalDistance = Mathf.Sqrt(1 * 1 + Mathf.Pow(Distance(distance, linearIncreaseFactor, i + 1), 2));
                rigidLines.Add(new RigidLine(startIndex1 + i, startIndex2 + i + 1, diagonalDistance));
            }
            if (i != maxIndexOffset) {
                float diagonalDistance = Mathf.Sqrt(1 * 1 + Mathf.Pow(Distance(distance, linearIncreaseFactor, i), 2));
                rigidLines.Add(new RigidLine(startIndex1 + i + 1, startIndex2 + i, diagonalDistance));
            }
        }
    }
    private float Distance(float distance, float linearIncreaseFactor, int level) {
        return distance + level * linearIncreaseFactor;
    }

    /// <summary>
    /// The linear increase factor determines how wide the top will get on every consecutive level. This value is added to distance
    /// </summary>
    private void CrossConnectTwoChains(Branch firstBranch, int relativeStartingIndex, Branch secondBranch, float distance, float linearIncreaseFactor) {
        CrossConnectTwoChains(firstBranch.GetStartingNodeIndex() + relativeStartingIndex, firstBranch.GetStartingNodeIndex() + firstBranch.GetNodeCount() - 1,
            secondBranch.GetStartingNodeIndex(), secondBranch.GetStartingNodeIndex() + secondBranch.GetNodeCount() - 1,
            distance, linearIncreaseFactor);
    }
}
