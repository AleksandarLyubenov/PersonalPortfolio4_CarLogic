// LaneConnectorToolEditor.cs
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

[CustomEditor(typeof(LaneConnectorTool))]
public class LaneConnectorToolEditor : Editor
{
    private LaneConnectorTool tool;
    private RoadSegment selectedFrom;
    private RoadSegment selectedTo;

    private void OnEnable()
    {
        tool = (LaneConnectorTool)target;
        SceneView.duringSceneGui += OnSceneGUI; // Hook into scene view GUI
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI; // Unhook to prevent memory leaks
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Lane Connection Tools", EditorStyles.boldLabel);

        // Input fields for lane selection and lane end configuration
        tool.fromLane = EditorGUILayout.IntField("From Lane", tool.fromLane);
        tool.toLane = EditorGUILayout.IntField("To Lane", tool.toLane);
        tool.fromEnd = (LaneEndType)EditorGUILayout.EnumPopup("From End", tool.fromEnd);
        tool.toEnd = (LaneEndType)EditorGUILayout.EnumPopup("To End", tool.toEnd);

        // Disable button if segments aren't assigned
        GUI.enabled = tool != null && tool.fromSegment != null && tool.toSegment != null;

        if (GUILayout.Button("Create Lane Connection", GUILayout.Height(40)))
        {
            if (LaneConnectionManager.Instance == null)
            {
                Debug.LogError("LaneConnectionManager is not present in the scene.");
                return;
            }

            // Get the lane waypoints from both segments
            List<Vector3> fromPoints = LanePathPlanner.GetLaneWaypoints(tool.fromSegment, tool.fromLane);
            List<Vector3> toPoints = LanePathPlanner.GetLaneWaypoints(tool.toSegment, tool.toLane);

            // Proceed only if both lanes have enough points
            if (fromPoints.Count >= 1 && toPoints.Count >= 1)
            {
                Vector3 start = (tool.fromEnd == LaneEndType.Start) ? fromPoints[0] : fromPoints[^1];
                Vector3 end = (tool.toEnd == LaneEndType.Start) ? toPoints[0] : toPoints[^1];

                // Calculate a curved midpoint offset from the path
                Vector3 direction = (end - start).normalized;
                Vector3 mid = (start + end) * 0.5f + Vector3.Cross(Vector3.up, direction) * 2f;

                // Create and register the lane connection
                LaneConnection connection = new LaneConnection
                {
                    fromSegment = tool.fromSegment,
                    fromLane = tool.fromLane,
                    toSegment = tool.toSegment,
                    toLane = tool.toLane,
                    controlPoints = new List<Vector3> { start, mid, end }
                };

                LaneConnectionManager.Instance.RegisterConnection(connection);

#if UNITY_EDITOR
                // Allow undo and mark scene dirty if in editor
                if (!Application.isPlaying)
                {
                    Undo.RecordObject(LaneConnectionManager.Instance, "Create Lane Connection");
                    EditorUtility.SetDirty(LaneConnectionManager.Instance);
                    EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                }
#endif
                Debug.Log("Lane connection created.");
            }
            else
            {
                Debug.LogWarning("Insufficient lane waypoints to generate a connection.");
            }
        }

        GUI.enabled = true;

        // Reset currently selected from/to segments
        if (GUILayout.Button("Reset Selection"))
        {
            selectedFrom = null;
            selectedTo = null;
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        Event e = Event.current;

        // Handle left-click (no ALT) in scene view to select road segments
        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                RoadSegment clickedSegment = hit.collider.GetComponentInParent<RoadSegment>();
                if (clickedSegment != null)
                {
                    if (selectedFrom == null)
                    {
                        selectedFrom = clickedSegment;
                    }
                    else if (selectedTo == null && clickedSegment != selectedFrom)
                    {
                        selectedTo = clickedSegment;
                        tool.fromSegment = selectedFrom;
                        tool.toSegment = selectedTo;
                        selectedFrom = null;
                        selectedTo = null;
                    }

                    // Consume event so it doesn't interact with the rest of Unity
                    e.Use();
                }
            }
        }

        // GUI panel in scene view to show current selection state
        Handles.BeginGUI();
        GUILayout.BeginArea(new Rect(10, 10, 300, 60), EditorStyles.helpBox);
        GUILayout.Label("Lane Connector Tool", EditorStyles.boldLabel);
        GUILayout.Label($"From: {(selectedFrom ? selectedFrom.name : "None")} To: {(selectedTo ? selectedTo.name : "None")} ");
        GUILayout.EndArea();
        Handles.EndGUI();
    }
}
