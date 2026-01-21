using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class RoadEditorTool : MonoBehaviour
{
    [Header("Prefabs & Materials")]
    public GameObject nodePrefab;
    public Material roadMaterial;
    public Material dashedWhiteLineMaterial;
    public Material dashedYellowLineMaterial;
    public Material solidWhiteLineMaterial;
    public Material solidYellowLineMaterial;

    [Header("Editor Selection")]
    public RoadNode nodeA;
    public RoadNode nodeB;
    public RoadType selectedRoadType = RoadType.Straight;
    public RoadDetectionMode detectionMode = RoadDetectionMode.Auto;
    public int laneCount = 2;

    [Header("Placement Settings")]
    public float placementDistance = 20f;

    // Spawns a new road node at a specified position
    public void CreateNode(Vector3 position)
    {
        GameObject nodeGO = Instantiate(nodePrefab, position, Quaternion.identity, transform);
        nodeGO.name = "RoadNode_" + System.Guid.NewGuid().ToString("N").Substring(0, 6);

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Undo.RegisterCreatedObjectUndo(nodeGO, "Create Road Node");
            EditorUtility.SetDirty(nodeGO);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(nodeGO.scene);
        }
#endif
    }

    // Connects the selected nodes A and B using the defined road type/laneCount
    public void ConnectSelectedNodes()
    {
        if (nodeA == null || nodeB == null)
        {
            Debug.LogWarning("Please assign both Node A and Node B.");
            return;
        }

        ConnectNodes(nodeA, nodeB, selectedRoadType, laneCount, autoDetect: true);
    }

    // Determines whether to place a straight or curved road segment
    private RoadType DetermineRoadType(Vector3 from, Vector3 to, Vector3? previousDirection = null, float angleThreshold = 15f)
    {
        if (previousDirection.HasValue)
        {
            Vector3 newDir = (to - from).normalized;
            float angle = Vector3.Angle(previousDirection.Value.normalized, newDir);
            if (angle <= angleThreshold)
                return RoadType.Straight;
        }
        return RoadType.Curve;
    }

    // Core road connection logic between two nodes
    public void ConnectNodes(RoadNode a, RoadNode b, RoadType type, int lanes = 2, bool autoDetect = false)
    {
        if (autoDetect || detectionMode == RoadDetectionMode.Auto)
        {
            Vector3? lastDirection = a.connectedSegments.Count > 0
                ? (Vector3?)(a.transform.position - a.connectedSegments[^1].startNode.transform.position)
                : null;

            type = DetermineRoadType(a.transform.position, b.transform.position, lastDirection);
        }
        else if (detectionMode == RoadDetectionMode.ForceStraight)
        {
            type = RoadType.Straight;
        }
        else if (detectionMode == RoadDetectionMode.ForceCurve)
        {
            type = RoadType.Curve;
        }

        // Instantiate new segment GameObject
        GameObject segmentGO = new GameObject($"Segment_{a.name}_to_{b.name}");
        segmentGO.transform.parent = transform;
        segmentGO.isStatic = true;
        segmentGO.layer = LayerMask.NameToLayer("Default");

        // Setup segment data and mesh
        RoadSegment segment = segmentGO.AddComponent<RoadSegment>();
        segment.startNode = a;
        segment.endNode = b;
        segment.roadType = type;
        segment.laneCount = lanes;
        segment.meshFilter = segmentGO.AddComponent<MeshFilter>();
        segment.meshRenderer = segmentGO.AddComponent<MeshRenderer>();
        segment.meshRenderer.sharedMaterial = roadMaterial;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Undo.RegisterCreatedObjectUndo(segmentGO, "Create Road Segment");
            EditorUtility.SetDirty(segmentGO);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(segmentGO.scene);
        }
#endif

        Mesh mesh = null;

        if (type == RoadType.Straight)
        {
            mesh = RoadMeshGenerator.GenerateStraightMesh(a.transform.position, b.transform.position, lanes, segment.roadWidth);
        }
        else if (type == RoadType.Curve)
        {
            Vector3 mid = (a.transform.position + b.transform.position) * 0.5f;
            Vector3 offset = Vector3.Cross(Vector3.up, (b.transform.position - a.transform.position)).normalized * 10f;
            mesh = RoadMeshGenerator.GenerateBezierCurveMeshWithFacing(a.transform.position, b.transform.position, offset, segment.roadWidth, lanes);
        }

        segment.meshFilter.sharedMesh = mesh;
        a.connectedSegments.Add(segment);
        b.connectedSegments.Add(segment);

        GenerateDividerLines(segmentGO, a.transform.position, b.transform.position, segment);
    }

    // Simple multi-node intersection from one central hub
    public void CreateIntersection(List<RoadNode> nodes, float roadWidth)
    {
        Vector3 center = Vector3.zero;
        foreach (var node in nodes) center += node.transform.position;
        center /= nodes.Count;

        GameObject hubGO = new GameObject("IntersectionHub");
        hubGO.transform.position = center;
        RoadNode hubNode = hubGO.AddComponent<RoadNode>();

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Undo.RegisterCreatedObjectUndo(hubGO, "Create Intersection Hub");
            EditorUtility.SetDirty(hubGO);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(hubGO.scene);
        }
#endif

        foreach (RoadNode node in nodes)
        {
            ConnectNodes(node, hubNode, RoadType.Straight, laneCount, false);
            ConnectNodes(hubNode, node, RoadType.Straight, laneCount, false);
        }
    }

    // Creates a central hub with curved connections between all nodes
    public void CreateCustomIntersection(List<RoadNode> connectedNodes, Vector3 hubPosition, int laneCount = 2, string name = "IntersectionHub")
    {
        GameObject hubGO = new GameObject(name);
        hubGO.transform.position = hubPosition;
        RoadNode hubNode = hubGO.AddComponent<RoadNode>();

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Undo.RegisterCreatedObjectUndo(hubGO, "Create Custom Intersection Hub");
            EditorUtility.SetDirty(hubGO);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(hubGO.scene);
        }
#endif

        foreach (var node in connectedNodes)
        {
            ConnectNodes(node, hubNode, RoadType.Straight, laneCount, false);
        }

        for (int i = 0; i < connectedNodes.Count; i++)
        {
            for (int j = 0; j < connectedNodes.Count; j++)
            {
                if (i == j) continue;

                var from = connectedNodes[i];
                var to = connectedNodes[j];

                Vector3 dirFrom = (hubPosition - from.transform.position).normalized;
                Vector3 dirTo = (to.transform.position - hubPosition).normalized;
                Vector3 cornerOffset = Vector3.Cross(Vector3.up, dirFrom + dirTo).normalized * 5f;

                GameObject segGO = new GameObject($"Turn_{from.name}_to_{to.name}");
                segGO.transform.parent = transform;

                RoadSegment turnSegment = segGO.AddComponent<RoadSegment>();
                turnSegment.startNode = from;
                turnSegment.endNode = to;
                turnSegment.roadType = RoadType.Curve;
                turnSegment.laneCount = laneCount;
                turnSegment.meshFilter = segGO.AddComponent<MeshFilter>();
                turnSegment.meshRenderer = segGO.AddComponent<MeshRenderer>();
                turnSegment.meshRenderer.sharedMaterial = roadMaterial;

                Mesh curve = RoadMeshGenerator.GenerateBezierCurveMeshWithFacing(
                    from.transform.position,
                    to.transform.position,
                    cornerOffset,
                    turnSegment.roadWidth,
                    laneCount
                );

                turnSegment.meshFilter.mesh = curve;
                from.connectedSegments.Add(turnSegment);
                to.connectedSegments.Add(turnSegment);

                GenerateDividerLines(segGO, from.transform.position, to.transform.position, turnSegment);

#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    Undo.RegisterCreatedObjectUndo(segGO, "Create Turn Segment");
                    EditorUtility.SetDirty(segGO);
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(segGO.scene);
                }
#endif
            }
        }
    }

    // Only creates straight connections between input nodes and hub
    public void CreateStraightOnlyIntersection(List<RoadNode> connectedNodes, Vector3 hubPosition, int laneCount = 2, string name = "IntersectionHub")
    {
        GameObject hubGO = new GameObject(name);
        hubGO.transform.position = hubPosition;
        RoadNode hubNode = hubGO.AddComponent<RoadNode>();

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Undo.RegisterCreatedObjectUndo(hubGO, "Create Straight Intersection Hub");
            EditorUtility.SetDirty(hubGO);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(hubGO.scene);
        }
#endif

        foreach (var node in connectedNodes)
        {
            ConnectNodes(node, hubNode, RoadType.Straight, laneCount, false);
            ConnectNodes(hubNode, node, RoadType.Straight, laneCount, false);
        }

        for (int i = 0; i < connectedNodes.Count; i++)
        {
            for (int j = 0; j < connectedNodes.Count; j++)
            {
                if (i == j) continue;

                RoadNode from = connectedNodes[i];
                RoadNode to = connectedNodes[j];

                Vector3 forward = (to.transform.position - from.transform.position).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

                bool shouldFlip = Vector3.Dot(forward, Vector3.forward) < -0.9f || Vector3.Dot(forward, Vector3.right) < -0.9f;

                if (shouldFlip)
                {
                    var temp = from;
                    from = to;
                    to = temp;
                }

                ConnectNodes(from, to, RoadType.Straight, laneCount, false);
            }
        }
    }

    // Adds divider lines between lanes with visual mesh strips
    public void GenerateDividerLines(GameObject parent, Vector3 start, Vector3 end, RoadSegment segment)
    {
        Vector3 forward = (end - start).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        float spacing = (segment.laneCount * segment.roadWidth) / 2f;

        if (segment.laneDividers == null || segment.laneDividers.Length < segment.laneCount - 1)
        {
            segment.laneDividers = new LaneDivider[Mathf.Max(0, segment.laneCount - 1)];
            for (int i = 0; i < segment.laneDividers.Length; i++)
                segment.laneDividers[i] = LaneDivider.WhiteDashed;
        }

        for (int i = 0; i < Mathf.Min(segment.laneDividers.Length, segment.laneCount - 1); i++)
        {
            LaneDivider divider = segment.laneDividers[i];
            if (divider == LaneDivider.None) continue;

            Material mat = GetMaterialForDivider(divider);
            float offset = -spacing + segment.roadWidth * (i + 1);

            Vector3 posStart = start + right * offset + Vector3.up * 0.01f;
            Vector3 posEnd = end + right * offset + Vector3.up * 0.01f;

            GameObject line = new GameObject($"Line_{i}");
            line.transform.parent = parent.transform;

            MeshFilter mf = line.AddComponent<MeshFilter>();
            MeshRenderer mr = line.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;

            float lineWidth = 0.15f;
            mf.mesh = RoadMeshGenerator.GenerateLineStrip(posStart, posEnd, lineWidth, 2f);
        }
    }

    // Returns appropriate material based on divider type
    private Material GetMaterialForDivider(LaneDivider divider)
    {
        return divider switch
        {
            LaneDivider.WhiteDashed => dashedWhiteLineMaterial,
            LaneDivider.YellowDashed => dashedYellowLineMaterial,
            LaneDivider.WhiteSolid => solidWhiteLineMaterial,
            LaneDivider.YellowSolid => solidYellowLineMaterial,
            _ => null
        };
    }
}
