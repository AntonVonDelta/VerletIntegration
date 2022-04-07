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
    private List<RigidLine> rigidLines = new List<RigidLine>();
    private int startingNodeIndex = -1;

    public Branch(int startingNodeIndex, int nodes, float distance=1f) {
        this.startingNodeIndex = startingNodeIndex;

        CreateVerletPoints(nodes);
        ConnectPointsByLines(nodes, distance);
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
    public List<RigidLine> GetRigidLines() {
        return rigidLines;
    }



    private void ConnectPointsByLines(int count, float distance) {
        for (int i = 0; i < count - 1; i++) {
            rigidLines.Add(new RigidLine(startingNodeIndex + i, startingNodeIndex + i + 1, distance));
        }
    }
    private void CreateVerletPoints(int count) {
        for (int i = 0; i < count; i++) {
            simulationPoints.Add(new VerletPoint(Random.insideUnitSphere));
        }
    }
}
