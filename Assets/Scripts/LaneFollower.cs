using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

public enum BlinkerState { None, Left, Right }

public class LaneFollower : MonoBehaviour
{
    // Movement settings
    public float speed = 5f;
    public float targetSpeed = 50f; // in km/h
    public float acceleration = 10f;
    public float steeringSpeed = 5f;

    // Following behavior
    public float followDistance = 5f;
    public LayerMask carLayer;

    // Brake light visuals
    public GameObject brakeLightObject;
    public Material brakeOnMaterial;
    public Material brakeOffMaterial;
    private bool isBraking = false;

    // Path following
    private Queue<Vector3> path = new();
    private Vector3 currentTarget;
    private bool hasPath = false;

    // Turn signal visuals
    public GameObject leftBlinker;
    public GameObject rightBlinker;
    public Material blinkerOnMaterial;
    public Material blinkerOffMaterial;
    public BlinkerState blinkerState = BlinkerState.None;
    private float blinkerTimer = 0f;
    private float blinkerInterval = 0.5f;
    private bool blinkerVisible = false;

    // Traffic logic
    private bool isBlockedByTraffic = false;

    void Start()
    {
        SetBlinker(leftBlinker, false);
        SetBlinker(rightBlinker, false);
    }

    public void SetPath(List<Vector3> waypoints)
    {
        path = new Queue<Vector3>(waypoints);
        if (path.Count > 0)
        {
            currentTarget = path.Dequeue();
            hasPath = true;
        }
    }

    void Update()
    {
        HandleBlinkers();
        HandleFollowingDistance();

        if (!hasPath || isBlockedByTraffic) return;

        // Move toward current waypoint
        if ((transform.position - currentTarget).sqrMagnitude < 0.2f)
        {
            if (path.Count > 0)
                currentTarget = path.Dequeue();
            else
            {
                hasPath = false;
                return;
            }
        }

        Vector3 direction = (currentTarget - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;

        // Rotate smoothly toward direction
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * steeringSpeed);
        }

        // Accelerate toward target speed
        speed = Mathf.MoveTowards(speed, targetSpeed / 3.6f, acceleration * Time.deltaTime);
        UpdateBrakeLights();
    }

    void HandleFollowingDistance()
    {
        float detectionDistance = followDistance + 2f;
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 direction = transform.forward;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, detectionDistance, carLayer))
        {
            float dist = hit.distance;

            if (dist < followDistance)
            {
                // Emergency stop
                speed = 0f;
                isBraking = true;
            }
            else
            {
                // Proportional slowdown
                float desiredSpeed = Mathf.Lerp(0f, targetSpeed / 3.6f, (dist - followDistance) / 5f);
                speed = Mathf.MoveTowards(speed, desiredSpeed, acceleration * Time.deltaTime);
                isBraking = true;
            }
        }
        else
        {
            isBraking = false;
        }

        Debug.DrawRay(origin, direction * detectionDistance, Color.red);
    }

    void UpdateBrakeLights()
    {
        if (!brakeLightObject) return;
        var renderers = brakeLightObject.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
            renderer.material = isBraking ? brakeOnMaterial : brakeOffMaterial;
    }

    void HandleBlinkers()
    {
        if (blinkerState == BlinkerState.None)
        {
            SetBlinker(leftBlinker, false);
            SetBlinker(rightBlinker, false);
            return;
        }

        blinkerTimer += Time.deltaTime;
        if (blinkerTimer >= blinkerInterval)
        {
            blinkerTimer = 0f;
            blinkerVisible = !blinkerVisible;

            if (blinkerState == BlinkerState.Left)
            {
                SetBlinker(leftBlinker, blinkerVisible);
                SetBlinker(rightBlinker, false);
            }
            else if (blinkerState == BlinkerState.Right)
            {
                SetBlinker(rightBlinker, blinkerVisible);
                SetBlinker(leftBlinker, false);
            }
        }
    }

    void SetBlinker(GameObject blinker, bool on)
    {
        if (!blinker) return;
        foreach (var renderer in blinker.GetComponentsInChildren<Renderer>())
            renderer.material = on ? blinkerOnMaterial : blinkerOffMaterial;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Handle traffic lights
        if (other.TryGetComponent(out TrafficLight light))
        {
            if (!light.IsGreen())
                StartCoroutine(WaitAtTrafficLight(light));
            return;
        }

        // Handle traffic signs
        if (!other.TryGetComponent(out TrafficSign sign)) return;

        // Check if sign is on right-hand side
        Vector3 toSign = (sign.transform.position - transform.position).normalized;
        if (Vector3.Dot(transform.right, toSign) < 0f) return;

        switch (sign.type)
        {
            case TrafficSignType.Stop:
                StartCoroutine(HandleStopSign());
                break;
            case TrafficSignType.Yield:
                StartCoroutine(HandleYieldSign());
                break;
            case TrafficSignType.SpeedLimit:
                targetSpeed = Random.Range(sign.speedLimit - 5f, sign.speedLimit + 5f);
                break;
        }
    }

    IEnumerator HandleStopSign()
    {
        Debug.Log("Stopping at stop sign");
        isBlockedByTraffic = true;
        speed = 0f;

        yield return new WaitForSeconds(3f);
        yield return HandleUncontrolledIntersection();
        yield return CheckForCrossTraffic();

        isBlockedByTraffic = false;
    }

    IEnumerator HandleUncontrolledIntersection()
    {
        float detectionRadius = 12f;
        Collider[] nearby = Physics.OverlapSphere(transform.position, detectionRadius, carLayer);

        foreach (var car in nearby)
        {
            if (car.gameObject == gameObject) continue;

            Vector3 dir = (car.transform.position - transform.position).normalized;
            float angle = Vector3.SignedAngle(transform.forward, dir, Vector3.up);

            if (angle > 0 && angle < 120f)
            {
                Debug.Log("Yielding to car from the right");
                isBlockedByTraffic = true;
                speed = 0f;
                yield return new WaitForSeconds(2.5f);
                isBlockedByTraffic = false;
                break;
            }
        }
    }

    private IEnumerator CheckForCrossTraffic()
    {
        float checkTime = 0f;
        float maxCheckTime = 10f;
        float interval = 0.1f;
        float fieldLength = 15f;
        float fieldWidth = 20f;

        while (checkTime < maxCheckTime)
        {
            Vector3 center = transform.position + transform.forward * (fieldLength / 2f) + Vector3.up * 0.5f;
            Vector3 halfExtents = new Vector3(fieldWidth / 2f, 1f, fieldLength / 2f);
            Quaternion rotation = Quaternion.LookRotation(transform.forward);

            Collider[] hits = Physics.OverlapBox(center, halfExtents, rotation, carLayer);

            if (blinkerState == BlinkerState.Left && IsOncomingCarBlocking())
            {
                Debug.Log("Oncoming traffic is going straight — waiting");
                checkTime += interval;
                yield return new WaitForSeconds(interval);
                continue;
            }

            if (!hits.Any(hit => hit.gameObject != gameObject))
            {
                Debug.Log("Clear — proceed");
                targetSpeed = 50f;
                yield return new WaitForSeconds(0.3f);
                yield break;
            }

            Debug.Log($"Blocked by {hits.Length} vehicle(s)");
            checkTime += interval;
            yield return new WaitForSeconds(interval);
        }

        Debug.Log("Timed out — forced proceed");
    }

    private bool IsOncomingCarBlocking()
    {
        Collider[] cars = Physics.OverlapSphere(transform.position, 12f, carLayer);

        foreach (var car in cars)
        {
            if (car.gameObject == gameObject) continue;

            Vector3 dir = (car.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, dir);

            if (angle < 20f)
            {
                if (car.TryGetComponent(out LaneFollower other) && other.blinkerState == BlinkerState.None)
                    return true;
            }
        }
        return false;
    }

    IEnumerator HandleYieldSign()
    {
        float detectionRadius = 20f;
        Collider[] nearby = Physics.OverlapSphere(transform.position, detectionRadius, LayerMask.GetMask("Vehicle"));

        foreach (var car in nearby)
        {
            if (car.gameObject == gameObject) continue;

            Vector3 dirToCar = (car.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, dirToCar);

            if (angle < 60f)
            {
                yield return WaitToYield();
                break;
            }
        }

        if (nearby.Length > 1)
        {
            float waitTime = 2f;
            speed = 0f;
            yield return new WaitForSeconds(waitTime);
            targetSpeed = 50f;
        }
    }

    private IEnumerator WaitAtTrafficLight(TrafficLight light)
    {
        Debug.Log("Car triggered traffic light and is waiting");
        isBlockedByTraffic = true;
        speed = 0f;

        while (!light.IsGreen())
            yield return null;

        targetSpeed = 50f;
        yield return new WaitForSeconds(0.2f);
        isBlockedByTraffic = false;
    }

    private IEnumerator WaitToYield()
    {
        Debug.Log("Yielding to oncoming car");
        float waitTime = 2.5f;
        speed = 0f;
        yield return new WaitForSeconds(waitTime);
        targetSpeed = 50f;
    }

    // For debugging: draws cross-traffic check field
    private void OnDrawGizmosSelected()
    {
        float fieldLength = 15f;
        float fieldWidth = 20f;
        Vector3 center = transform.position + transform.forward * (fieldLength / 2f) + Vector3.up * 0.5f;
        Vector3 halfExtents = new Vector3(fieldWidth / 2f, 1f, fieldLength / 2f);
        Quaternion rotation = Quaternion.LookRotation(transform.forward);

        Gizmos.color = Color.magenta;
        Matrix4x4 matrix = Matrix4x4.TRS(center, rotation, Vector3.one);
        Gizmos.matrix = matrix;
        Gizmos.DrawWireCube(Vector3.zero, halfExtents * 2f);
    }

    // Static method for smooth Bezier curve for lane change
    public static List<Vector3> GenerateSmoothLaneSwitch(Vector3 start, Vector3 end)
    {
        Vector3 lateralDir = Vector3.Cross(Vector3.up, (end - start).normalized);
        float lateralDist = Vector3.Distance(start, end) * 0.5f;
        Vector3 mid = (start + end) * 0.5f + lateralDir.normalized * lateralDist;

        return BezierPath(start, mid, end, 20);
    }

    private static List<Vector3> BezierPath(Vector3 p0, Vector3 p1, Vector3 p2, int resolution)
    {
        List<Vector3> path = new();
        for (int i = 0; i <= resolution; i++)
        {
            float t = i / (float)resolution;
            path.Add(Mathf.Pow(1 - t, 2) * p0 + 2 * (1 - t) * t * p1 + Mathf.Pow(t, 2) * p2);
        }
        return path;
    }
}
