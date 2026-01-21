#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[InitializeOnLoad]
public static class LaneSwitchVisualizer
{
    static LaneSwitchVisualizer()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private static void OnSceneGUI(SceneView view)
    {
        var allSegments = GameObject.FindObjectsByType<RoadSegment>(FindObjectsSortMode.None);
        foreach (var segment in allSegments)
        {
            if (segment == null || segment.laneCount <= 1)
                continue;

            List<LaneSwitchPoint> switches = segment.GetLaneSwitchPoints();

            foreach (var sw in switches)
            {
                Vector3 from = sw.fromPosition + Vector3.up * 0.05f;
                Vector3 to = sw.toPosition + Vector3.up * 0.05f;

                // Optional: Only draw if distance is small enough (avoid noise)
                if (Vector3.Distance(from, to) > 10f)
                    continue;

                Handles.color = Color.cyan;
                Handles.DrawDottedLine(from, to, 2f);

                Vector3 dir = (to - from).normalized;
                Vector3 mid = Vector3.Lerp(from, to, 0.5f);
                Handles.ArrowHandleCap(0, mid, Quaternion.LookRotation(dir), 0.6f, EventType.Repaint);
            }
        }
    }
}
#endif
