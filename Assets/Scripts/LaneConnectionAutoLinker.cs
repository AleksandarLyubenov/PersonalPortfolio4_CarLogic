using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class LaneConnectionAutoLinker : MonoBehaviour
{
    // Maximum distance threshold between lane endpoints to consider them connected
    public float maxDistance = 0.05f;

#if UNITY_EDITOR
    // Adds a right-click context menu item in the Inspector
    [ContextMenu("Auto Link All Lane Connections")]
    public void AutoLinkAllLanes()
    {
        // Find all RoadSegment objects in the scene (unsorted for performance)
        RoadSegment[] segments = Object.FindObjectsByType<RoadSegment>(FindObjectsSortMode.None);
        Debug.Log($"Found {segments.Length} road segments. Starting auto-link...");

        // Iterate over all possible combinations of segments
        foreach (var from in segments)
        {
            foreach (var to in segments)
            {
                if (from == to) continue; // Don't link a segment to itself

                for (int lane = 0; lane < from.laneCount; lane++)
                {
                    // Determine if lane is reverse (left side)
                    bool isReverse = lane < from.laneCount / 2;

                    // Get lane waypoints for both 'from' and 'to' segments
                    List<Vector3> fromPoints = LanePathPlanner.GetLaneWaypoints(from, lane);
                    List<Vector3> toPoints = LanePathPlanner.GetLaneWaypoints(to, lane);

                    // Ensure each lane has enough points to define direction
                    if (fromPoints.Count < 2 || toPoints.Count < 2)
                        continue;

                    // Get connection ends based on lane direction
                    Vector3 fromEnd = isReverse ? fromPoints[0] : fromPoints[^1];
                    Vector3 toStart = isReverse ? toPoints[^1] : toPoints[0];

                    // Check if the end of one lane is close enough to the start of another
                    float distance = Vector3.Distance(fromEnd, toStart);
                    if (distance < maxDistance)
                    {
                        // Create a new lane connection
                        LaneConnection connection = new LaneConnection
                        {
                            fromSegment = from,
                            fromLane = lane,
                            toSegment = to,
                            toLane = lane,
                            controlPoints = LaneFollower.GenerateSmoothLaneSwitch(fromEnd, toStart)
                        };

                        // Register connection with the central manager
                        LaneConnectionManager.Instance.RegisterConnection(connection);

                        Debug.Log($"Linked lane {lane} ({(isReverse ? "reverse" : "forward")}) from {from.name} → {to.name}");
                    }
                }
            }
        }

        Debug.Log("Auto-link complete.");
    }
#endif
}
