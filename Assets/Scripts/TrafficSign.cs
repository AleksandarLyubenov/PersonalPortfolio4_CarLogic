using UnityEngine;

public enum TrafficSignType
{
    Stop,
    Yield,
    SpeedLimit
}

[RequireComponent(typeof(Collider))]
public class TrafficSign : MonoBehaviour
{
    public TrafficSignType type;
    public float speedLimit = 50f; // km/h

    private void OnDrawGizmos()
    {
        Gizmos.color = type switch
        {
            TrafficSignType.Stop => Color.red,
            TrafficSignType.Yield => Color.yellow,
            TrafficSignType.SpeedLimit => Color.green,
            _ => Color.white
        };
        Gizmos.DrawWireCube(transform.position + Vector3.up * 1f, Vector3.one * 1.2f);
    }
}
