// Shared Enum
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public enum LaneEndType { Start, End }

// LaneConnectorTool.cs

[ExecuteInEditMode]
public class LaneConnectorTool : MonoBehaviour
{
    public RoadSegment fromSegment;
    public int fromLane;

    public RoadSegment toSegment;
    public int toLane;

    public LaneEndType fromEnd = LaneEndType.End;
    public LaneEndType toEnd = LaneEndType.Start;

    public void CreateConnection()
    {
        if (!fromSegment || !toSegment || fromLane < 0 || toLane < 0)
        {
            Debug.LogWarning("Invalid segments or lane indices.");
            return;
        }

        LaneConnection connection = new LaneConnection();
        connection.fromSegment = fromSegment;
        connection.fromLane = fromLane;
        connection.toSegment = toSegment;
        connection.toLane = toLane;

        List<Vector3> fromWaypoints = LanePathPlanner.GetLaneWaypoints(fromSegment, fromLane);
        List<Vector3> toWaypoints = LanePathPlanner.GetLaneWaypoints(toSegment, toLane);

        if (fromWaypoints.Count < 1 || toWaypoints.Count < 1)
        {
            Debug.LogWarning("Invalid waypoints.");
            return;
        }

        Vector3 start = (fromEnd == LaneEndType.Start) ? fromWaypoints[0] : fromWaypoints[^1];
        Vector3 end = (toEnd == LaneEndType.Start) ? toWaypoints[0] : toWaypoints[^1];
        Vector3 direction = (end - start).normalized;
        Vector3 mid = (start + end) * 0.5f + Vector3.up * 2f + Vector3.Cross(Vector3.up, direction) * 2f;

        connection.controlPoints = new List<Vector3> { start, mid, end };

        LaneConnectionManager.Instance.RegisterConnection(connection);

#if UNITY_EDITOR
        Undo.RecordObject(LaneConnectionManager.Instance, "Add Lane Connection");
        EditorUtility.SetDirty(LaneConnectionManager.Instance);
#endif

        Debug.Log("LaneConnection created between segments.");
    }
}