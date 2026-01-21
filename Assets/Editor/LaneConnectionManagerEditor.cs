using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LaneConnectionManager))]
public class LaneConnectionManagerEditor : Editor
{
    private LaneConnectionManager manager;

    private void OnEnable()
    {
        manager = (LaneConnectionManager)target;
        SceneView.duringSceneGui += OnSceneGUI; // Hook into SceneView for visual control points
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI; // Cleanup on editor disable
    }

    // Automatically connect lanes between road segments that are close enough
    public void AutoGenerateForwardConnections(List<RoadSegment> segments, float maxDistance = 0.05f)
    {
        foreach (var from in segments)
        {
            foreach (var to in segments)
            {
                if (from == to) continue; // Skip self

                for (int lane = 0; lane < from.laneCount; lane++)
                {
                    bool isReverse = lane < from.laneCount / 2; // Left lanes are reverse

                    List<Vector3> fromPoints = LanePathPlanner.GetLaneWaypoints(from, lane);
                    List<Vector3> toPoints = LanePathPlanner.GetLaneWaypoints(to, lane);

                    if (fromPoints.Count < 2 || toPoints.Count < 2)
                        continue;

                    // Define start/end based on lane direction
                    Vector3 fromEnd = isReverse ? fromPoints[0] : fromPoints[^1];
                    Vector3 toStart = isReverse ? toPoints[^1] : toPoints[0];

                    float distance = Vector3.Distance(fromEnd, toStart);
                    if (distance < maxDistance)
                    {
                        // Create a lane connection with a smooth curve
                        LaneConnection connection = new LaneConnection
                        {
                            fromSegment = from,
                            fromLane = lane,
                            toSegment = to,
                            toLane = lane,
                            controlPoints = LaneFollower.GenerateSmoothLaneSwitch(fromEnd, toStart)
                        };

                        // Register the connection globally
                        LaneConnectionManager.Instance.RegisterConnection(connection);
                        Debug.Log($"Auto-linked lane {lane} ({(isReverse ? "reverse" : "forward")}) from {from.name} → {to.name}");
                    }
                }
            }
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (Application.isPlaying) return;

        Handles.color = Color.magenta;

        // Visualize and allow dragging of control points in Scene view
        for (int i = 0; i < manager.connections.Count; i++)
        {
            LaneConnection conn = manager.connections[i];
            if (conn.controlPoints == null || conn.controlPoints.Count < 2) continue;

            for (int j = 0; j < conn.controlPoints.Count; j++)
            {
                EditorGUI.BeginChangeCheck();

                // Display draggable handle for each control point
                Vector3 newPos = Handles.PositionHandle(conn.controlPoints[j], Quaternion.identity);

                if (EditorGUI.EndChangeCheck())
                {
                    // Record change and update control point position
                    Undo.RecordObject(manager, "Move Lane Connection Control Point");
                    conn.controlPoints[j] = newPos;
                    EditorUtility.SetDirty(manager);
                }
            }

            // Draw a polyline connecting the control points
            Handles.DrawAAPolyLine(conn.controlPoints.ToArray());
        }
    }
}
