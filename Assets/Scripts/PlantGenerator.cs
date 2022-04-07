using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static VerletIntegration;

public class PlantGenerator {
    private List<VerletPoint> simulationPoints = new List<VerletPoint>();
    private List<RigidLine> rigidLines = new List<RigidLine>();
    private int mainBranchSize;
    private int maxBranching;
    private int uniqueIndex = 0;

    public PlantGenerator(int mainBranchSize, int maxBranching) {
        this.mainBranchSize = mainBranchSize;
        this.maxBranching = maxBranching;
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
                        int newBranchNodes = currentBranch.GetNodeCount() / 4;
                        if (newBranchNodes == 0) continue;

                        Branch newBranch = new Branch(uniqueIndex, newBranchNodes);
                        uniqueIndex += newBranchNodes;
                        currentBranch.AddChildBranch(j, newBranch);

                        newBranches.Enqueue(newBranch);

                        // Add points and constraint lines
                        simulationPoints.AddRange(newBranch.GetVerletPoints());
                        rigidLines.AddRange(newBranch.GetRigidLines());
                        CrossConnectTwoChains(currentBranch, j, newBranch, 1f);
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
    private void CrossConnectTwoChains(int startIndex1, int finalIndex1, int startIndex2, int finalIndex2, float distance, bool cross1 = true, bool cross2 = true) {
        int maxIndexOffset = Mathf.Min(finalIndex1 - startIndex1, finalIndex2 - startIndex2);
        for (int i = 0; i < maxIndexOffset + 1; i++) {
            rigidLines.Add(new RigidLine(startIndex1 + i, startIndex2 + i, distance));
            if (cross1 && i != maxIndexOffset) {
                rigidLines.Add(new RigidLine(startIndex1 + i, startIndex2 + i + 1, distance * Mathf.Sqrt(2)));
            }
            if (cross2 && i != maxIndexOffset) {
                rigidLines.Add(new RigidLine(startIndex1 + i + 1, startIndex2 + i, distance * Mathf.Sqrt(2)));
            }
        }
    }
    private void CrossConnectTwoChains(Branch firstBranch, int relativeStartingIndex, Branch secondBranch, float distance, bool cross1 = true, bool cross2 = true) {
        CrossConnectTwoChains(firstBranch.GetStartingNodeIndex() + relativeStartingIndex, firstBranch.GetStartingNodeIndex() + firstBranch.GetNodeCount() - 1,
            secondBranch.GetStartingNodeIndex(), secondBranch.GetStartingNodeIndex() + secondBranch.GetNodeCount() - 1,
            distance, cross1, cross2);
    }
}
