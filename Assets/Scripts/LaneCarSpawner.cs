using UnityEngine;

public class LaneCarSpawner : MonoBehaviour
{
    public GameObject carPrefab;
    public RoadSegment startSegment;
    public int laneIndex = 0;

    void Start()
    {
        if (!carPrefab || !startSegment)
        {
            Debug.LogError("Spawner missing carPrefab or startSegment.");
            return;
        }

        GameObject car = Instantiate(carPrefab);
        LaneFollower follower = car.GetComponent<LaneFollower>();
        if (!follower)
        {
            Debug.LogError("Spawned car missing LaneFollower script.");
            return;
        }

        var path = LanePathPlanner.PlanLanePath(startSegment, laneIndex, follower);
        if (path.Count > 0)
        {
            car.transform.position = path[0];
            follower.SetPath(path);
        }
    }
}