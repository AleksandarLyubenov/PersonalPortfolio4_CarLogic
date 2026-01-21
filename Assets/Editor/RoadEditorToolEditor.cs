using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RoadEditorTool))]
public class RoadEditorToolEditor : Editor
{
    private RoadEditorTool tool;
    private RoadNode hoveredNode;
    private RoadNode selectedNode;

    private void OnEnable()
    {
        tool = (RoadEditorTool)target;
        SceneView.duringSceneGui += OnSceneGUI; // Subscribe to Scene GUI rendering
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI; // Unsubscribe to avoid memory leaks
    }

    public override void OnInspectorGUI()
    {
        // Draw base inspector properties
        DrawDefaultInspector();

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Node Tools", EditorStyles.boldLabel);

        // Button to create a node in front of the Scene camera
        if (GUILayout.Button("Create Node in Front of Scene Camera"))
        {
            Vector3 pos = SceneView.lastActiveSceneView.camera.transform.position +
                          SceneView.lastActiveSceneView.camera.transform.forward * 10f;
            tool.CreateNode(pos);
        }

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Auto Node Placement", EditorStyles.boldLabel);

        // Button to add a node in the forward direction of nodeA
        if (GUILayout.Button("Add Point Along Direction"))
        {
            AddPointInDirection(tool);
        }

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Connect Nodes", EditorStyles.boldLabel);

        // Button to manually connect nodeA and nodeB
        if (GUILayout.Button("Connect Selected Nodes"))
        {
            tool.ConnectSelectedNodes();
        }

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Editor Parameters", EditorStyles.boldLabel);

        // Manual field assignments
        tool.nodeA = (RoadNode)EditorGUILayout.ObjectField("Node A", tool.nodeA, typeof(RoadNode), true);
        tool.nodeB = (RoadNode)EditorGUILayout.ObjectField("Node B", tool.nodeB, typeof(RoadNode), true);
        tool.selectedRoadType = (RoadType)EditorGUILayout.EnumPopup("Road Type", tool.selectedRoadType);
        tool.laneCount = EditorGUILayout.IntSlider("Lane Count", tool.laneCount, 1, 6);
        tool.placementDistance = EditorGUILayout.FloatField("Placement Distance", tool.placementDistance);

        if (GUI.changed)
        {
            EditorUtility.SetDirty(tool); // Mark dirty if values changed
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (tool == null) return;

        // Label displayed in Scene GUI
        Handles.BeginGUI();
        GUI.Label(new Rect(10, 10, 300, 20), "Drag from node to node to create a road.");
        Handles.EndGUI();

        hoveredNode = null;
        float minDist = 1.5f;

        // Detect nearest node to mouse cursor
        foreach (RoadNode node in Object.FindObjectsByType<RoadNode>(FindObjectsSortMode.None))
        {
            float dist = Vector3.Distance(HandleUtility.GUIPointToWorldRay(Event.current.mousePosition).origin, node.transform.position);
            if (dist < minDist)
            {
                hoveredNode = node;
                minDist = dist;
            }

            // Draw button at each node
            Handles.color = Color.yellow;
            if (Handles.Button(node.transform.position, Quaternion.identity, 1f, 1f, Handles.SphereHandleCap))
            {
                if (selectedNode == null)
                {
                    selectedNode = node;
                }
                else if (selectedNode != node)
                {
                    tool.ConnectNodes(selectedNode, node, tool.selectedRoadType, tool.laneCount);
                    selectedNode = null;
                }

                Event.current.Use();
            }

            // Preview connection line when dragging from one node to another
            if (selectedNode != null && hoveredNode != null && selectedNode != hoveredNode)
            {
                Handles.color = Color.cyan;
                Handles.DrawDottedLine(selectedNode.transform.position, hoveredNode.transform.position, 4f);
            }
        }

        // Handle scene mouse interaction
        if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && hoveredNode != null)
        {
            if (selectedNode == null)
            {
                selectedNode = hoveredNode;
                Event.current.Use();
            }
            else if (selectedNode != hoveredNode)
            {
                tool.ConnectNodes(selectedNode, hoveredNode, tool.selectedRoadType, tool.laneCount);
                selectedNode = null;
                Event.current.Use();
            }
        }

        // Redraw link preview line if dragging
        if (selectedNode != null && hoveredNode != null && selectedNode != hoveredNode)
        {
            Handles.color = Color.cyan;
            Handles.DrawDottedLine(selectedNode.transform.position, hoveredNode.transform.position, 5f);
        }

        SceneView.RepaintAll(); // Ensure real-time updates
    }

    private void AddPointInDirection(RoadEditorTool tool)
    {
        if (tool.nodeA == null)
        {
            Debug.LogWarning("Set Node A first.");
            return;
        }

        Vector3 dir = tool.nodeA.transform.forward.normalized;
        Vector3 newPos = tool.nodeA.transform.position + dir * tool.placementDistance;

        // Instantiate node prefab and set transform
        GameObject nodeGO = PrefabUtility.InstantiatePrefab(tool.nodePrefab) as GameObject;
        nodeGO.transform.position = newPos;
        nodeGO.transform.SetParent(tool.transform);
        nodeGO.name = "RoadNode_" + System.Guid.NewGuid().ToString("N").Substring(0, 6);

        // Connect new node to Node A
        RoadNode newNode = nodeGO.GetComponent<RoadNode>();
        if (newNode != null)
        {
            tool.ConnectNodes(tool.nodeA, newNode, RoadType.Straight, tool.laneCount, autoDetect: true);
            tool.nodeA = newNode; // Continue building from this node
        }

        SceneView.RepaintAll();
    }
}
