using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static VerletIntegration;

public class PlantGenerator {
    private List<VerletPoint> simulationPoints = new List<VerletPoint>();
    private int mainBranchSize;
    private int maxBranching;
    private int uniqueIndex = 0;

    public PlantGenerator(int mainBranchSize, int maxBranching) {
        this.mainBranchSize = mainBranchSize;
        this.maxBranching = maxBranching;
    }

    public void Generate() {
        Branch branch = new Branch(uniqueIndex, mainBranchSize);
        Queue<Branch> pendingBranches = new Queue<Branch>();
        Queue<Branch> newBranches = new Queue<Branch>();

        pendingBranches.Enqueue(branch);
        uniqueIndex += branch.GetNodeCount();

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
                        uniqueIndex += branch.GetNodeCount();
                        currentBranch.AddChildBranch(newBranch);
                    }
                }
            }


        }

    }

    public List<VerletPoint> GetVerletPoints() {

    }

    public List<RigidLine> GetRigidLines() {

    }

    private void CreateVerletPoints(int count) {
        for (int i = 0; i < count; i++) {
            simulationPoints.Add(new VerletPoint(Random.insideUnitSphere));
        }
    }
}
