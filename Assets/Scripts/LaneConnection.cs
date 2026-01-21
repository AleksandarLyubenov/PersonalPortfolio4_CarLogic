using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LaneConnection
{
    public RoadSegment fromSegment;
    public int fromLane;

    public RoadSegment toSegment;
    public int toLane;

    public List<Vector3> controlPoints;
}