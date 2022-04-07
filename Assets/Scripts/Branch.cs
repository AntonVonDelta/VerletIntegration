using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static VerletIntegration;

public class Branch {

    private List<VerletPoint> simulationPoints = new List<VerletPoint>();
    private List<Branch> childBranches = new List<Branch>();
    private int startingNodeIndex = -1;

    public Branch(int startingNodeIndex, int nodes) {
        this.startingNodeIndex = startingNodeIndex;

        CreateVerletPoints(nodes);
    }

    public void AddChildBranch(Branch child) {
        childBranches.Add(child);
    }
    //public Branch BranchOff(int startingNodeIndex, int nodes) {
    //    Branch child = new Branch(startingNodeIndex, nodes);
    //    childBranches.Add(child);
    //    return child;
    //}

    public int GetNodeCount() {
        return simulationPoints.Count;
    }

    public List<Branch> GetChildBranches() {
        return childBranches;
    }

    public List<VerletPoint> GetVerletPoints() {
        return simulationPoints;
    }

    private void CreateVerletPoints(int count) {
        for (int i = 0; i < count; i++) {
            simulationPoints.Add(new VerletPoint(Random.insideUnitSphere));
        }
    }
}
