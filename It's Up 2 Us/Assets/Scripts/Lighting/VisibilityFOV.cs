// Assets/Scripts/Lighting/VisibilityFOV.cs
using System.Collections.Generic;
using UnityEngine;

public class VisibilityFOV : MonoBehaviour
{
    [Header("Occlusion")]
    [Tooltip("Layers that block vision (Ground, Walls, Crates, etc.). Exclude all player layers.")]
    public LayerMask obstacleMask;
    [Tooltip("How far the player can see (world units).")]
    public float viewRadius = 12f;
    [Range(32, 720)] public int rayCount = 256;

    [Header("Rendering")]
    [Tooltip("Material that writes ONLY to stencil (FogCutoutWrite.mat).")]
    public Material fogCutoutWriteMaterial;
    [Tooltip("Local Z offset for the FOV mesh (fog overlay is at 0).")]
    public float fovZOffset = 0.1f;
    [Tooltip("Optional: layer to assign to the FOV mesh (e.g., \"FOV\").")]
    public string fovLayerName = "FOV";

    // internal
    Mesh _mesh;
    Transform _meshT;
    MeshRenderer _mr;
    MeshFilter _mf;

    void Awake()
    {
        // Create a child to hold the FOV mesh (do NOT touch the player's transform)
        var go = new GameObject("FOV_Mesh");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 0f, fovZOffset);
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale    = Vector3.one;

        _meshT = go.transform;
        _mf    = go.AddComponent<MeshFilter>();
        _mr    = go.AddComponent<MeshRenderer>();

        _mesh = new Mesh { name = "FOVMesh" };
        _mf.sharedMesh = _mesh;

        if (fogCutoutWriteMaterial != null)
            _mr.sharedMaterial = fogCutoutWriteMaterial;

        // Keep it lightweight & invisible (stencil only)
        _mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _mr.receiveShadows    = false;

        // Optional: put on a dedicated layer
        int fovLayer = LayerMask.NameToLayer(fovLayerName);
        if (fovLayer != -1) go.layer = fovLayer;
    }

    void LateUpdate()
    {
        BuildVisibilityMesh();
    }

    void BuildVisibilityMesh()
    {
        if (_mesh == null) return;

        int vCount = rayCount + 1; // + center
        var verts = new Vector3[vCount];
        var tris  = new int[rayCount * 3];

        verts[0] = Vector3.zero; // center in local space

        float step = 360f / rayCount;
        Vector3 origin = transform.position;

        for (int i = 0; i < rayCount; i++)
        {
            float ang = i * step * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));

            RaycastHit2D hit = Physics2D.Raycast(origin, dir, viewRadius, obstacleMask);

            Vector3 end = hit ? (Vector3)hit.point - origin : (Vector3)(dir * viewRadius);
            verts[i + 1] = new Vector3(end.x, end.y, 0f);

            if (i < rayCount - 1)
            {
                int t = i * 3;
                tris[t]     = 0;
                tris[t + 1] = i + 1;
                tris[t + 2] = i + 2;
            }
        }
        // stitch last tri to first outer vertex
        int lt = (rayCount - 1) * 3;
        tris[lt]     = 0;
        tris[lt + 1] = rayCount;
        tris[lt + 2] = 1;

        _mesh.Clear();
        _mesh.SetVertices(verts);
        _mesh.SetTriangles(tris, 0);
        _mesh.RecalculateBounds();

        // keep the child at the requested Z (in case parent moved)
        _meshT.localPosition = new Vector3(0f, 0f, fovZOffset);
    }
}
