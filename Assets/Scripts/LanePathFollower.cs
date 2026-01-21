using System.Collections.Generic;
using UnityEngine;

public static class LanePathPlanner
{
    // Builds a list of waypoints across connected segments, handling lane switches and turns
    public static List<Vector3> PlanLanePath(RoadSegment startSegment, int laneIndex, LaneFollower follower, int maxHops = 20)
    {
        List<Vector3> waypoints = new();
        HashSet<RoadSegment> visited = new();

        RoadSegment current = startSegment;
        int currentLane = laneIndex;
        int hops = 0;

        while (current != null && hops < maxHops)
        {
            // Attempt lateral lane switches on current segment
            List<LaneSwitchPoint> switchPoints = current.GetLaneSwitchPoints();
            Debug.Log($"[Switch Check] Segment: {current.name} | Lane: {currentLane} | Switch Points: {switchPoints.Count}");

            foreach (var sp in switchPoints)
            {
                for (int targetLane = 0; targetLane < current.laneCount; targetLane++)
                {
                    if (targetLane == currentLane) continue;

                    float fromOffset = GetLaneOffset(current, currentLane);
                    float toOffset = GetLaneOffset(current, targetLane);

                    Vector3 forward = (current.endNode.transform.position - current.startNode.transform.position).normalized;
                    Vector3 right = Vector3.Cross(Vector3.up, forward);

                    Vector3 fromExpected = Vector3.Lerp(current.startNode.transform.position, current.endNode.transform.position, 0.5f) + right * fromOffset;
                    Vector3 toExpected = Vector3.Lerp(current.startNode.transform.position, current.endNode.transform.position, 0.5f) + right * toOffset;

                    float distFrom = Vector3.Distance(sp.fromPosition, fromExpected);
                    float distTo = Vector3.Distance(sp.toPosition, toExpected);

                    if (distFrom < 2f && distTo < 2f)
                    {
                        var smooth = LaneFollower.GenerateSmoothLaneSwitch(sp.fromPosition, sp.toPosition);
                        waypoints.AddRange(smooth);
                        Debug.Log($"Switching from lane {currentLane} to {targetLane}");

                        if (follower != null)
                            follower.blinkerState = (targetLane > currentLane) ? BlinkerState.Right : BlinkerState.Left;

                        currentLane = targetLane;
                        break;
                    }
                }
            }

            visited.Add(current);

            // Add current segment’s waypoints for this lane
            waypoints.AddRange(GetLaneWaypoints(current, currentLane));
            Debug.Log($"Evaluating next segment from: {current.name}, lane {currentLane}");

            bool movedToNext = false;

            // Try explicit lane-to-lane turn connections via LaneConnectionManager
            if (LaneConnectionManager.Instance != null)
            {
                List<LaneConnection> possibleTurns = LaneConnectionManager.Instance.GetConnectionsFrom(current, currentLane);
                if (possibleTurns != null && possibleTurns.Count > 0)
                {
                    LaneConnection turn = possibleTurns[Random.Range(0, possibleTurns.Count)];

                    List<Vector3> fromPoints = GetLaneWaypoints(current, currentLane);
                    if (fromPoints.Count >= 2 && turn.controlPoints.Count >= 2)
                    {
                        Vector3 currentDir = (fromPoints[^1] - fromPoints[^2]).normalized;
                        Vector3 turnDir = (turn.controlPoints[^1] - turn.controlPoints[0]).normalized;

                        float angle = Vector3.SignedAngle(currentDir, turnDir, Vector3.up);

                        if (follower != null)
                        {
                            if (angle > 15f) follower.blinkerState = BlinkerState.Left;
                            else if (angle < -15f) follower.blinkerState = BlinkerState.Right;
                            else follower.blinkerState = BlinkerState.None;

                            Debug.Log($"Blinker: {follower.blinkerState} ({angle:F1}°)");
                        }
                    }

                    // Apply turn and advance
                    waypoints.AddRange(turn.controlPoints);
                    current = turn.toSegment;
                    currentLane = turn.toLane;
                    movedToNext = true;
                }
            }

            // Fallback: continue forward through connected segments
            if (!movedToNext)
            {
                RoadSegment next = GetNextSegment(current);
                if (next != null && !visited.Contains(next))
                {
                    Debug.Log($"Default forward connection from {current.name} to {next.name} (lane {currentLane})");
                    current = next;
                    movedToNext = true;
                }
            }

            if (!movedToNext) break; // No more path available
            hops++;
        }

        return waypoints;
    }

    // Calculates the horizontal offset of a given lane (0 is leftmost)
    private static float GetLaneOffset(RoadSegment segment, int laneIndex)
    {
        float spacing = (segment.laneCount * segment.roadWidth) / 2f;
        return -spacing + segment.roadWidth * (laneIndex + 0.5f);
    }

    // Returns two world-space points: start -> end for the lane (or reversed for reverse lanes)
    public static List<Vector3> GetLaneWaypoints(RoadSegment segment, int laneIndex)
    {
        List<Vector3> points = new();
        if (segment == null || segment.startNode == null || segment.endNode == null)
            return points;

        Vector3 from = segment.startNode.transform.position;
        Vector3 to = segment.endNode.transform.position;
        Vector3 forward = (to - from).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward);

        float spacing = (segment.laneCount * segment.roadWidth) / 2f;
        float offset = -spacing + segment.roadWidth * (laneIndex + 0.5f);

        Vector3 start = from + right * offset;
        Vector3 end = to + right * offset;

        bool isReverse = laneIndex < segment.laneCount / 2;
        if (isReverse)
        {
            points.Add(end);
            points.Add(start);
        }
        else
        {
            points.Add(start);
            points.Add(end);
        }

        return points;
    }

    // Finds a segment forward-connected from the current one, excluding already visited ones
    private static RoadSegment GetNextSegment(RoadSegment current)
    {
        if (current == null)
        {
            Debug.LogWarning("GetNextSegment: current is null");
            return null;
        }

        if (current.endNode == null)
        {
            Debug.LogWarning($"GetNextSegment: current.endNode is null on segment {current.name}");
            return null;
        }

        if (current.endNode.connectedSegments == null)
        {
            Debug.LogWarning($"GetNextSegment: connectedSegments is null on node {current.endNode.name}");
            return null;
        }

        foreach (var seg in current.endNode.connectedSegments)
        {
            if (seg != null && seg != current && seg.startNode == current.endNode)
                return seg;
        }

        return null;
    }
}
