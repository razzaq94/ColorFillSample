using UnityEngine;

public class BoxCastDebugger : MonoBehaviour
{
    public Renderer _renderer;          // Assign if you have it; else we’ll fall back to collider
    public Collider[] colliders;        // Your existing colliders list (use index 0 as in your code)
    public bool drawGizmos = true;
    public Color gizmoColor = new Color(1f, 0.85f, 0f, 0.9f); // amber
    public float hitMarkerRadius = 0.08f;

    // Cached from last cast (optional, but useful if you only cast in Update during Play)
    private bool _lastHit;
    private Vector3 _lastHitPoint;

    // Call this from your Update after your BoxCast (optional)
    public void CacheHit(bool hit, Vector3 point)
    {
        _lastHit = hit;
        _lastHitPoint = point;
    }

    void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        // Recompute the same parameters you use for the cast
        var rend = _renderer ? _renderer : GetComponentInChildren<Renderer>();
        var col = (colliders != null && colliders.Length > 0) ? colliders[0] : GetComponentInChildren<Collider>();

        Bounds b = rend ? rend.bounds : (col ? col.bounds : new Bounds(transform.position, Vector3.one));

        float distance = 6f;       
        float halfSweep = distance * 0.5f;

        Vector3 dir = Vector3.up;       
        Vector3 origin = b.center - dir * halfSweep;
        Quaternion rot = Quaternion.identity;

        Vector3 halfExtents = new Vector3(b.extents.x * 0.98f, 0.05f, b.extents.z * 0.98f);

        DrawBoxCastGizmo(origin, halfExtents, dir, distance, rot, gizmoColor);

        if (_lastHit)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_lastHitPoint, hitMarkerRadius);
        }
    }

    private static void DrawBoxCastGizmo(Vector3 origin, Vector3 halfExtents, Vector3 direction, float distance, Quaternion rotation, Color color)
    {
        Vector3 end = origin + direction.normalized * distance;

        Vector3[] startCorners = GetBoxCorners(origin, halfExtents, rotation);
        Vector3[] endCorners = GetBoxCorners(end, halfExtents, rotation);

        Gizmos.color = color;

        DrawWireFromCorners(startCorners);
        DrawWireFromCorners(endCorners);

        for (int i = 0; i < 8; i++)
            Gizmos.DrawLine(startCorners[i], endCorners[i]);
    }

    private static Vector3[] GetBoxCorners(Vector3 center, Vector3 he, Quaternion rot)
    {
        Vector3[] local =
        {
            new Vector3(-he.x, -he.y, -he.z),
            new Vector3(+he.x, -he.y, -he.z),
            new Vector3(+he.x, -he.y, +he.z),
            new Vector3(-he.x, -he.y, +he.z),

            new Vector3(-he.x, +he.y, -he.z),
            new Vector3(+he.x, +he.y, -he.z),
            new Vector3(+he.x, +he.y, +he.z),
            new Vector3(-he.x, +he.y, +he.z)
        };

        Vector3[] world = new Vector3[8];
        for (int i = 0; i < 8; i++)
            world[i] = center + rot * local[i];

        return world;
    }

    private static void DrawWireFromCorners(Vector3[] c)
    {
        // bottom square 0-1-2-3
        Gizmos.DrawLine(c[0], c[1]); Gizmos.DrawLine(c[1], c[2]);
        Gizmos.DrawLine(c[2], c[3]); Gizmos.DrawLine(c[3], c[0]);
        // top square 4-5-6-7
        Gizmos.DrawLine(c[4], c[5]); Gizmos.DrawLine(c[5], c[6]);
        Gizmos.DrawLine(c[6], c[7]); Gizmos.DrawLine(c[7], c[4]);
        // verticals
        Gizmos.DrawLine(c[0], c[4]); Gizmos.DrawLine(c[1], c[5]);
        Gizmos.DrawLine(c[2], c[6]); Gizmos.DrawLine(c[3], c[7]);
    }
}
