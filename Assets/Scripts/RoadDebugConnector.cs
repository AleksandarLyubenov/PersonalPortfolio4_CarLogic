using UnityEngine;

public class RoadDebugConnector : MonoBehaviour
{
    public RoadEditorTool tool;
    public RoadNode nodeA;
    public RoadNode nodeB;

    private void Start()
    {
        if (tool && nodeA && nodeB)
        {
            tool.ConnectNodes(nodeA, nodeB, RoadType.Straight, 2);
        }
    }
}
