using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Branch;
using static VerletIntegration;

public class PlantGenerator {
    private List<VerletPoint> simulationPoints = new List<VerletPoint>();
    private List<RigidLine> rigidLines = new List<RigidLine>();

    // This stores the interval of the indexes of points of each branch represented in the main array simulationPoints
    private List<int[]> branchPointsInterval = new List<int[]>();

    private int mainBranchSize;
    private int maxBranching;
    private float halvingRatio;
    private float branchLinearDistanceFactor;
    public float distanceAwayFromParentBranch;

    private Branch mainBranch;
    private int uniqueIndex = 0;

    public PlantGenerator(int mainBranchSize, int maxBranching, float halvingRatio, float branchLinearDistanceFactor, float distanceAwayFromParentBranch) {
        this.mainBranchSize = mainBranchSize;
        this.maxBranching = maxBranching;
        this.halvingRatio = halvingRatio;
        this.branchLinearDistanceFactor = branchLinearDistanceFactor;
        this.distanceAwayFromParentBranch = distanceAwayFromParentBranch;
    }

    public void Generate() {
        mainBranch = new Branch(null, uniqueIndex, mainBranchSize);
        Queue<Branch> pendingBranches = new Queue<Branch>();
        Queue<Branch> newBranches = new Queue<Branch>();

        pendingBranches.Enqueue(mainBranch);
        uniqueIndex += mainBranch.GetNodeCount();

        // Add points and constraint lines
        simulationPoints.AddRange(mainBranch.GetVerletPoints());
        rigidLines.AddRange(mainBranch.GetRigidLines());

        // Add indexes for line renderers
        AddBranchPointsIndexes(mainBranch);

        for (int i = 0; i < maxBranching; i++) {
            while (pendingBranches.Count != 0) {
                Branch currentBranch = pendingBranches.Dequeue();

                for (int j = 0; j < currentBranch.GetNodeCount(); j++) {
                    int rand = Random.Range(0, 9);
                    float nodeProbability = Mathf.Lerp(0, 9, (float)j / (currentBranch.GetNodeCount() - 1));

                    if (rand < nodeProbability) {
                        int newBranchNodeCount = Mathf.Min(currentBranch.GetNodeCount() - j - 1, (int)(currentBranch.GetNodeCount() / halvingRatio));
                        float branchPointsDistance = Mathf.Sqrt(Mathf.Pow(currentBranch.GetDistance(), 2) + Mathf.Pow(branchLinearDistanceFactor, 2));

                        // Exclude 0 or 1 item branches
                        if (newBranchNodeCount <= 1) continue;

                        Branch newBranch = new Branch(currentBranch, uniqueIndex, newBranchNodeCount, branchPointsDistance);
                        uniqueIndex += newBranchNodeCount;
                        Attachment newAttachment = currentBranch.AddChildBranch(j, newBranch);
                        AddBranchPointsIndexes(newAttachment);

                        newBranches.Enqueue(newBranch);

                        // Add points and constraint lines
                        simulationPoints.AddRange(newBranch.GetVerletPoints());
                        rigidLines.AddRange(newBranch.GetRigidLines());
                        CrossConnectTwoChains(currentBranch, j, newBranch, distanceAwayFromParentBranch, branchLinearDistanceFactor);


                        // Create symetrical branch
                        Branch newBranch2 = new Branch(currentBranch, uniqueIndex, newBranchNodeCount, branchPointsDistance);
                        uniqueIndex += newBranchNodeCount;
                        newAttachment = currentBranch.AddChildBranch(j, newBranch2);
                        AddBranchPointsIndexes(newAttachment);

                        newBranches.Enqueue(newBranch2);

                        // Add points and constraint lines
                        simulationPoints.AddRange(newBranch2.GetVerletPoints());
                        rigidLines.AddRange(newBranch2.GetRigidLines());
                        CrossConnectTwoChains(currentBranch, j, newBranch2, distanceAwayFromParentBranch, branchLinearDistanceFactor);




                        // Get the two newly created branches and connect between them to prevent both collapsing
                        // in the same space
                        // We do this by connecting a line between them that keeps them separate
                        CrossConnectTwoSymetricalChains(newBranch, newBranch2, distanceAwayFromParentBranch, branchLinearDistanceFactor);


                        // Move iterator to next possible branching position
                        j +=Mathf.Max(0, newBranchNodeCount - 4);
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

    public List<int[]> GetBranchPointsIntervals() {
        return branchPointsInterval;
    }

    /// <summary>
    /// Connect two chains starting and ending at the given indexes
    /// Cross1 controls whether to insert a diagonal line from first to second chain to form a rigid structure
    /// Cross2 controls the other direction. Together they make an X inside a square
    /// </summary>
    private void CrossConnectTwoChains(int startIndex1, int finalIndex1, int startIndex2, int finalIndex2, float distance, float distanceOnfirstBranch, float linearIncreaseFactor) {
        int maxIndexOffset = Mathf.Min(finalIndex1 - startIndex1, finalIndex2 - startIndex2);
        for (int i = 0; i < maxIndexOffset + 1; i++) {
            float adjustedDistance = Distance(distance, linearIncreaseFactor, i);

            rigidLines.Add(new RigidLine(startIndex1 + i, startIndex2 + i, adjustedDistance));

            if (startIndex2 + i + 1 <= finalIndex2) {
                float diagonalDistance = Mathf.Sqrt(distanceOnfirstBranch * distanceOnfirstBranch + Mathf.Pow(Distance(distance, linearIncreaseFactor, i + 1), 2));
                rigidLines.Add(new RigidLine(startIndex1 + i, startIndex2 + i + 1, diagonalDistance));
            }
            if (startIndex1 + i + 1 <= finalIndex1) {
                float diagonalDistance = Mathf.Sqrt(distanceOnfirstBranch * distanceOnfirstBranch + Mathf.Pow(Distance(distance, linearIncreaseFactor, i), 2));
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
            distance, firstBranch.GetDistance(), linearIncreaseFactor);

    }

    private void CrossConnectTwoSymetricalChains(int startIndex1, int finalIndex1, int startIndex2, int finalIndex2, float distance, float distanceOnfirstBranch, float linearIncreaseFactor) {
        int maxIndexOffset = Mathf.Min(finalIndex1 - startIndex1, finalIndex2 - startIndex2);
        for (int i = 0; i < maxIndexOffset + 1; i++) {
            float adjustedDistance = 2 * Distance(distance, linearIncreaseFactor, i);

            rigidLines.Add(new RigidLine(startIndex1 + i, startIndex2 + i, adjustedDistance));

        }
    }
    private void CrossConnectTwoSymetricalChains(Branch firstBranch, Branch secondBranch, float distance, float linearIncreaseFactor) {
        CrossConnectTwoSymetricalChains(firstBranch.GetStartingNodeIndex(), firstBranch.GetStartingNodeIndex() + firstBranch.GetNodeCount() - 1,
            secondBranch.GetStartingNodeIndex(), secondBranch.GetStartingNodeIndex() + secondBranch.GetNodeCount() - 1,
            distance, firstBranch.GetDistance(), linearIncreaseFactor);

    }

    private void AddBranchPointsIndexes(Attachment attachment) {
        List<int> pointsIndexes = new List<int>();

        pointsIndexes.Add(attachment.nodeIndex);
        for (int i = 0; i < attachment.childBranch.GetNodeCount(); i++) {
            pointsIndexes.Add(attachment.childBranch.GetStartingNodeIndex() + i);
        }
        branchPointsInterval.Add(pointsIndexes.ToArray());
    }
    private void AddBranchPointsIndexes(Branch branch) {
        List<int> pointsIndexes = new List<int>();

        for (int i = 0; i < branch.GetNodeCount(); i++) {
            pointsIndexes.Add(branch.GetStartingNodeIndex() + i);
        }
        branchPointsInterval.Add(pointsIndexes.ToArray());
    }
}
