using UnityEngine;

// Attach to any scene object you want to make evaluatable.
// Holds metadata about the intended manufacturing task and optional overrides.
[DisallowMultipleComponent]
public class ManufacturablePart : MonoBehaviour
{
    [Header("Manufacturing Mapping")]
    [Tooltip("Task ID this part should satisfy (must exist in ManufacturingDB.manufacturingTasks)")] public string taskId = "hab_bracket";
    [Tooltip("Override candidate materials (leave empty to use task.candidateMaterials)")] public string[] candidateMaterials;
    [Tooltip("Override candidate processes (leave empty to use task.preferredProcesses)")] public string[] candidateProcesses;

    [Header("Physical & Mass Settings")] 
    [Tooltip("If >= 0, use this mass directly (kg) instead of computing from volume & density")] public float manualMassKg = -1f;
    [Tooltip("If true, attempt a mesh-based volume approximation (fallback to bounds if missing)")] public bool useMeshVolume = false;
    [Tooltip("Fraction of solid mass represented (e.g. 0.25 for 25% effective printed mass)")] [Range(0.01f,1f)] public float infillFactor = 0.25f;
    [Tooltip("Fallback volume (m^3) if no renderer/mesh is found")] public float fallbackVolume_m3 = 0.001f;

    [Header("Runtime (debug)")]
    [SerializeField] float cachedVolume_m3 = -1f;

    public float GetMassKg(ManufacturingDB db, MaterialDef mat)
    {
        if (mat == null) return 0f;
        if (manualMassKg >= 0f) return manualMassKg;
        var v = GetVolume_m3();
        var solidMass = v * Mathf.Max(mat.density_kg_m3, 0.0001f);
        return solidMass * Mathf.Clamp01(infillFactor);
    }

    public float GetVolume_m3()
    {
        if (cachedVolume_m3 >= 0f) return cachedVolume_m3;
        float v = useMeshVolume ? (MeshVolumeApprox() ?? BoundsVolume()) : BoundsVolume();
        if (v <= 0f) v = fallbackVolume_m3;
        cachedVolume_m3 = v;
        return v;
    }

    float BoundsVolume()
    {
        var r = GetComponentInChildren<Renderer>();
        if (!r) return 0f;
        var s = r.bounds.size; // world space
        return s.x * s.y * s.z;
    }

    float? MeshVolumeApprox()
    {
        var mf = GetComponentInChildren<MeshFilter>();
        if (!mf || !mf.sharedMesh) return null;
        var mesh = mf.sharedMesh;
        var verts = mesh.vertices;
        var tris = mesh.triangles;
        double vol = 0.0;
        var t = mf.transform;
        for (int i = 0; i < tris.Length; i += 3)
        {
            Vector3 a = t.TransformPoint(verts[tris[i]]);
            Vector3 b = t.TransformPoint(verts[tris[i+1]]);
            Vector3 c = t.TransformPoint(verts[tris[i+2]]);
            vol += SignedTetraVolume(a, b, c);
        }
        return Mathf.Abs((float)vol);
    }

    double SignedTetraVolume(Vector3 a, Vector3 b, Vector3 c)
        => Vector3.Dot(a, Vector3.Cross(b, c)) / 6.0;
}
