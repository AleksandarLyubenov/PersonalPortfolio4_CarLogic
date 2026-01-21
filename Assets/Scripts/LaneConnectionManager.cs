using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class LaneConnectionManager : MonoBehaviour
{
    public static LaneConnectionManager Instance { get; private set; }
    public List<LaneConnection> connections = new();

    private void OnEnable()
    {
        // Initialize in Edit Mode too
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            DestroyImmediate(this);
    }

    private void OnDisable()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Reset()
    {
        if (Instance == null)
            Instance = this;
    }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void RegisterConnection(LaneConnection connection)
    {
        connections.Add(connection);
    }

    public List<LaneConnection> GetConnectionsFrom(RoadSegment segment, int lane)
    {
        return connections.FindAll(c => c.fromSegment == segment && c.fromLane == lane);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        foreach (var conn in connections)
        {
            if (conn.controlPoints == null || conn.controlPoints.Count < 2) continue;
            for (int i = 0; i < conn.controlPoints.Count - 1; i++)
            {
                Gizmos.DrawLine(conn.controlPoints[i], conn.controlPoints[i + 1]);
            }
        }
    }
}