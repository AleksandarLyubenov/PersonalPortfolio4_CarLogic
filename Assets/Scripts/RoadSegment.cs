using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[ExecuteAlways]
public class RoadSegment : MonoBehaviour
{
    public RoadNode startNode;
    public RoadNode endNode;
    public RoadType roadType;
    public int laneCount = 2;
    public float roadWidth = 3f;
    public float switchInterval = 10f;

    [Tooltip("Divider between left/right side of road")]
    public LaneDivider centerDivider = LaneDivider.YellowDashed;

    [Tooltip("Dividers between same-direction lanes")]
    public LaneDivider[] laneDividers;

    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    public List<RoadSegment> connectedSegments = new();

#if UNITY_EDITOR
    private List<GameObject> spawnedGizmos = new(); // Used in editor for preview/drawing
#endif

    // Keep lane count even and update dividers array
    private void OnValidate()
    {
        laneCount = Mathf.Max(2, laneCount);
        if (laneCount % 2 != 0) laneCount++;

        int expectedDividers = Mathf.Max(0, laneCount - 1);
        if (laneDividers == null || laneDividers.Length != expectedDividers)
        {
            LaneDivider[] newDividers = new LaneDivider[expectedDividers];
            for (int i = 0; i < expectedDividers; i++)
                newDividers[i] = (i == laneCount / 2 - 1) ? centerDivider : LaneDivider.WhiteDashed;

            laneDividers = newDividers;
        }
    }

    // Draws lane directions, dividers, and labels in the Scene view
    private void OnDrawGizmosSelected()
    {
        if (meshFilter == null || meshFilter.sharedMesh == null) return;

        Mesh mesh = Application.isPlaying ? meshFilter.mesh : meshFilter.sharedMesh;

        if (mesh == null || !mesh.isReadable)
            return;

        Vector3[] verts = mesh.vertices;

        if (verts.Length < 4) return;

        Vector3 worldStart = transform.TransformPoint((verts[0] + verts[1]) * 0.5f);
        Vector3 worldEnd = transform.TransformPoint((verts[2] + verts[3]) * 0.5f);
        Vector3 forward = (worldEnd - worldStart).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        float spacing = (laneCount * roadWidth) / 2f;

        // Draw individual lane lines with direction
        for (int i = 0; i < laneCount; i++)
        {
            float offset = -spacing + roadWidth * (i + 0.5f);
            Vector3 laneStart = worldStart + right * offset;
            Vector3 laneEnd = worldEnd + right * offset;

            bool isLeft = i < laneCount / 2;
            Gizmos.color = isLeft ? Color.red : Color.green;
            Gizmos.DrawLine(laneStart, laneEnd);

            Vector3 arrowPos = Vector3.Lerp(laneStart, laneEnd, 0.25f);
            Vector3 dir = isLeft ? (laneStart - laneEnd).normalized : (laneEnd - laneStart).normalized;
            Handles.ArrowHandleCap(0, arrowPos, Quaternion.LookRotation(dir), 1.2f, EventType.Repaint);
            Handles.Label(arrowPos + Vector3.up * 0.5f, $"Lane {i}");
        }

        // Draw lane dividers
        for (int i = 0; i < laneCount - 1; i++)
        {
            float offset = -spacing + roadWidth * (i + 1);
            LaneDivider divider = laneDividers[i];
            if (divider == LaneDivider.None) continue;

            Color color = divider.ToString().Contains("Yellow") ? Color.yellow : Color.white;
            bool dashed = divider.ToString().Contains("Dashed");

            Vector3 start = worldStart + right * offset;
            Vector3 end = worldEnd + right * offset;

            Handles.color = color;
            if (dashed)
                Handles.DrawDottedLine(start, end, 3f);
            else
                Handles.DrawLine(start, end);
        }

        // Draw the central divider
        if (centerDivider != LaneDivider.None)
        {
            Color color = centerDivider.ToString().Contains("Yellow") ? Color.yellow : Color.white;
            bool dashed = centerDivider.ToString().Contains("Dashed");

            float centerOffset = -spacing + roadWidth * (laneCount / 2f);
            Vector3 centerStart = worldStart + right * centerOffset;
            Vector3 centerEnd = worldEnd + right * centerOffset;

            Handles.color = color;
            if (dashed)
                Handles.DrawDottedLine(centerStart, centerEnd, 3f);
            else
                Handles.DrawLine(centerStart, centerEnd);
        }

        // Label start/end nodes
        if (startNode != null)
            Handles.Label(startNode.transform.position + Vector3.up, "Start");

        if (endNode != null)
            Handles.Label(endNode.transform.position + Vector3.up, "End");
    }

    // Get lane-switching points allowed by divider type and interval
    public List<LaneSwitchPoint> GetLaneSwitchPoints()
    {
        List<LaneSwitchPoint> switches = new();

        if (meshFilter == null || meshFilter.sharedMesh == null) return switches;

        Mesh mesh = Application.isPlaying ? meshFilter.mesh : meshFilter.sharedMesh;

        if (mesh == null || !mesh.isReadable)
            return switches;

        Vector3[] verts = mesh.vertices;

        if (verts.Length < 4) return switches;

        Vector3 worldStart = transform.TransformPoint((verts[0] + verts[1]) * 0.5f);
        Vector3 worldEnd = transform.TransformPoint((verts[2] + verts[3]) * 0.5f);
        Vector3 forward = (worldEnd - worldStart).normalized;
        float totalLength = Vector3.Distance(worldStart, worldEnd);
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        float spacing = (laneCount * roadWidth) / 2f;

        int switchCount = Mathf.FloorToInt(totalLength / switchInterval);

        for (int i = 0; i < laneCount - 1; i++)
        {
            LaneDivider divider = laneDividers[i];

            // Solid dividers cannot be crossed
            bool noCrossing = divider == LaneDivider.WhiteSolid || divider == LaneDivider.YellowSolid;
            if (noCrossing) continue;

            for (int j = 0; j <= switchCount; j++)
            {
                float t = (j + 1f) / (switchCount + 1f);
                Vector3 along = Vector3.Lerp(worldStart, worldEnd, t);

                float offsetA = -spacing + roadWidth * (i + 0.5f);
                float offsetB = -spacing + roadWidth * (i + 1 + 0.5f);

                Vector3 from = along + right * offsetA;
                Vector3 to = along + right * offsetB;

                switches.Add(new LaneSwitchPoint { fromPosition = from, toPosition = to });
                switches.Add(new LaneSwitchPoint { fromPosition = to, toPosition = from });
            }
        }

        return switches;
    }
}

// Holds start and end of a lane segment (may be used for linking)
[System.Serializable]
public class LaneEnd
{
    public Vector3 startPosition;
    public Vector3 endPosition;
}

// Legacy (possibly unused) reference setup for lane ends using transforms
[System.Serializable]
public class LaneEndPoint
{
    public Transform start;
    public Transform end;
}

// Data structure for defining a cross-lane switch opportunity
[System.Serializable]
public struct LaneSwitchPoint
{
    public Vector3 fromPosition;
    public Vector3 toPosition;
}
