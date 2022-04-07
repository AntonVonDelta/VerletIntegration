using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static VerletIntegration;

public class Branch {
    public struct Attachment {
        public int nodeIndex;
        public Branch childBranch;
    }

    private List<VerletPoint> simulationPoints = new List<VerletPoint>();
    private List<Attachment> childBranches = new List<Attachment>();
    private int startingNodeIndex = -1;

    public Branch(int startingNodeIndex, int nodes) {
        this.startingNodeIndex = startingNodeIndex;

        CreateVerletPoints(nodes);
    }

    public void AddChildBranch(int relativeAttachmentNodeIndex, Branch child) {
        childBranches.Add(new Attachment { nodeIndex = startingNodeIndex + relativeAttachmentNodeIndex, childBranch = child });
    }

    public int GetStartingNodeIndex() {
        return startingNodeIndex;
    }

    public int GetNodeCount() {
        return simulationPoints.Count;
    }

    public List<Attachment> GetChildBranches() {
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
