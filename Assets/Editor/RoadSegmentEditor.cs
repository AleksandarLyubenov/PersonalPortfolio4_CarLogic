using UnityEngine;
using UnityEditor;

// Custom editor for the RoadSegment component
[CustomEditor(typeof(RoadSegment))]
public class RoadSegmentEditor : Editor
{
    // Override the default inspector GUI
    public override void OnInspectorGUI()
    {
        // Draw all default inspector properties
        DrawDefaultInspector();

        RoadSegment segment = (RoadSegment)target;

        GUILayout.Space(10); // Add some spacing

        // Add a custom button below the inspector
        if (GUILayout.Button("Re-generate Divider Lines"))
        {
            RegenerateDividerLines(segment);
        }
    }

    private void RegenerateDividerLines(RoadSegment segment)
    {
        // Loop through children and remove any previously generated divider lines
        for (int i = segment.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = segment.transform.GetChild(i);

            // Only delete children that start with "Line_"
            if (child.name.StartsWith("Line_"))
            {
#if UNITY_EDITOR
                DestroyImmediate(child.gameObject); // Immediate in editor context
#else
                Destroy(child.gameObject); // Runtime fallback
#endif
            }
        }

        // Attempt to find the road editor tool on a parent object
        var tool = segment.GetComponentInParent<RoadEditorTool>();
        if (tool != null)
        {
            // Re-generate divider visuals using the tool's method
            tool.GenerateDividerLines(segment.gameObject, segment.startNode.transform.position, segment.endNode.transform.position, segment);
        }
        else
        {
            Debug.LogWarning("No RoadEditorTool found in parent.");
        }
    }
}
