using System.Collections.Generic;
using UnityEngine;

public static class RoadMeshGenerator
{
    // Generates a straight rectangular mesh for a road segment between two points
    public static Mesh GenerateStraightMesh(Vector3 start, Vector3 end, int lanes, float width)
    {
        Mesh mesh = new Mesh();

        Vector3 direction = (end - start).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, direction);
        float half = (lanes * width) / 2f;

        // Define corners of the road
        Vector3 bl = start - right * half;
        Vector3 br = start + right * half;
        Vector3 tl = end - right * half;
        Vector3 tr = end + right * half;

        // Create mesh from quad
        mesh.vertices = new Vector3[] { bl, br, tl, tr };
        mesh.triangles = new int[] { 0, 2, 1, 1, 2, 3 };
        mesh.RecalculateNormals();
        return mesh;
    }

    // Generates a curved road mesh using a smooth cubic Bezier curve with lane width/facing
    public static Mesh GenerateBezierCurveMeshWithFacing(
        Vector3 start, Vector3 end, Vector3 offset,
        float width, int lanes, int resolution = 30)
    {
        Mesh mesh = new Mesh();
        List<Vector3> verts = new();
        List<int> tris = new();

        float halfWidth = (lanes * width) / 2f;

        // Define cubic bezier control points
        Vector3 p0 = start;
        Vector3 p3 = end;
        Vector3 control = (p0 + p3) * 0.5f + offset;
        Vector3 p1 = Vector3.Lerp(p0, control, 0.5f);
        Vector3 p2 = Vector3.Lerp(p3, control, 0.5f);

        // Sample points and tangents along the curve
        List<Vector3> points = new();
        List<Vector3> tangents = new();

        for (int i = 0; i <= resolution; i++)
        {
            float t = i / (float)resolution;
            points.Add(BezierPoint(p0, p1, p2, p3, t));
            tangents.Add(BezierTangent(p0, p1, p2, p3, t));
        }

        // Create quads between curve points
        for (int i = 0; i < resolution; i++)
        {
            Vector3 curr = points[i];
            Vector3 next = points[i + 1];

            Vector3 dir = tangents[i].normalized;
            Vector3 right = Vector3.Cross(Vector3.up, dir);

            Vector3 bl = curr - right * halfWidth;
            Vector3 br = curr + right * halfWidth;
            Vector3 tl = next - right * halfWidth;
            Vector3 tr = next + right * halfWidth;

            int vi = verts.Count;
            verts.Add(bl); verts.Add(br); verts.Add(tl); verts.Add(tr);
            tris.AddRange(new int[] { vi, vi + 2, vi + 1, vi + 1, vi + 2, vi + 3 });
        }

        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    // Computes position of a cubic bezier curve at t
    private static Vector3 BezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1 - t;
        return u * u * u * p0 +
               3 * u * u * t * p1 +
               3 * u * t * t * p2 +
               t * t * t * p3;
    }

    // Computes tangent of a cubic bezier curve at t
    private static Vector3 BezierTangent(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1 - t;
        return
            3 * u * u * (p1 - p0) +
            6 * u * t * (p2 - p1) +
            3 * t * t * (p3 - p2);
    }

    // Generates a flat strip (line) mesh used for road dividers or lane markings
    public static Mesh GenerateLineStrip(Vector3 start, Vector3 end, float width, float dashRepeatLength = 2f)
    {
        Vector3 forward = end - start;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        float length = forward.magnitude;

        Vector3 v0 = start - right * (width * 0.5f);
        Vector3 v1 = start + right * (width * 0.5f);
        Vector3 v2 = end - right * (width * 0.5f);
        Vector3 v3 = end + right * (width * 0.5f);

        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[] { v0, v1, v2, v3 };

        // UVs used for dash texture tiling
        float tiling = length / dashRepeatLength;
        mesh.uv = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, tiling),
            new Vector2(1, tiling)
        };

        mesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
        mesh.RecalculateNormals();
        return mesh;
    }
}
