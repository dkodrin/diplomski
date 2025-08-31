// Assets/Scripts/Lighting/TilemapShadowCaster2D.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// ShadowCaster2D namespace varies by Unity/URP version:
#if UNITY_2022_2_OR_NEWER
using UnityEngine.Rendering.Universal;
#else
using UnityEngine.Experimental.Rendering.Universal;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

/// Generates merged (rectangular) ShadowCaster2D regions for a Tilemap so 2D lights are
/// cleanly occluded (no per-tile “teeth”).
/// • Assign a 1x1 white square sprite (any solid square PNG).
/// • Click “Generate” or enable autoRegenerateInEditor to rebuild while painting.
/// • Requires URP with the 2D Renderer as Default, and Sprite-Lit-Default materials.
[ExecuteAlways]
[RequireComponent(typeof(Tilemap), typeof(TilemapRenderer))]
public class TilemapShadowCaster2D : MonoBehaviour
{
    [Header("Required")]
    [Tooltip("Any solid 1x1 white square sprite (used invisibly as a silhouette).")]
    public Sprite squareSprite;

    [Header("Options")]
    [Tooltip("Prefix for generated children.")]
    public string childPrefix = "SC_";

    [Tooltip("Auto-regenerate while editing this tilemap in the Editor.")]
    public bool autoRegenerateInEditor = true;

    [Tooltip("Also generate at runtime on Awake/OnEnable.")]
    public bool generateAtRuntime = true;

    // Internals
    Tilemap _tm;
    Grid    _grid;
    int     _lastUsedTiles = -1;

    void OnEnable()
    {
        _tm   = GetComponent<Tilemap>();
        _grid = GetComponentInParent<Grid>();

#if UNITY_EDITOR
        if (!Application.isPlaying && autoRegenerateInEditor)
        {
            Generate();
            _lastUsedTiles = SafeUsedTilesCount();
        }
#endif
        if (Application.isPlaying && generateAtRuntime)
            Generate();
    }

#if UNITY_EDITOR
    void Update()
    {
        if (!Application.isPlaying && autoRegenerateInEditor)
        {
            int used = SafeUsedTilesCount();
            if (used != _lastUsedTiles)
            {
                _lastUsedTiles = used;
                Generate();
            }
        }
    }
#endif

    int SafeUsedTilesCount()
    {
        try { return _tm.GetUsedTilesCount(); }
        catch { return -1; }
    }

    [ContextMenu("Generate")]
    public void Generate()
    {
        if (_tm == null)   _tm = GetComponent<Tilemap>();
        if (_grid == null) _grid = GetComponentInParent<Grid>();

        Clear();

        if (squareSprite == null)
        {
            Debug.LogWarning("[TilemapShadowCaster2D] Assign a 1x1 white square sprite first.");
            return;
        }

        // Build boolean map of filled cells
        var bounds = _tm.cellBounds;
        int width  = bounds.size.x;
        int height = bounds.size.y;
        if (width <= 0 || height <= 0) return;

        bool[,] filled = new bool[width, height];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            var cell = new Vector3Int(bounds.xMin + x, bounds.yMin + y, 0);
            filled[x, y] = _tm.HasTile(cell);
        }

        bool[,] used = new bool[width, height];

        // Match Tilemap’s sorting so light layers line up
        var tmRenderer     = GetComponent<TilemapRenderer>();
        int sortingLayerID = tmRenderer.sortingLayerID;
        int sortingOrder   = tmRenderer.sortingOrder;

        Vector3 cellSize = _grid ? (Vector3)_grid.cellSize : Vector3.one;

        // Greedy rectangle merge
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            if (!filled[x, y] || used[x, y]) continue;

            // Find max width on this row
            int rectW = 0;
            while (x + rectW < width && filled[x + rectW, y] && !used[x + rectW, y]) rectW++;

            // Grow downward while each subsequent row matches that width
            int rectH = 1;
            bool canGrow = true;
            while (canGrow && y + rectH < height)
            {
                for (int sx = 0; sx < rectW; sx++)
                {
                    if (!filled[x + sx, y + rectH] || used[x + sx, y + rectH])
                    {
                        canGrow = false;
                        break;
                    }
                }
                if (canGrow) rectH++;
            }

            // Mark the area as used
            for (int yy = 0; yy < rectH; yy++)
                for (int xx = 0; xx < rectW; xx++)
                    used[x + xx, y + yy] = true;

            // Compute rectangle world center/size
            var blCell   = new Vector3Int(bounds.xMin + x, bounds.yMin + y, 0);
            var blWorld  = _tm.GetCellCenterLocal(blCell) - new Vector3(cellSize.x, cellSize.y, 0f) * 0.5f;
            Vector3 size = new Vector3(rectW * cellSize.x, rectH * cellSize.y, 1f);
            Vector3 ctr  = blWorld + new Vector3(size.x, size.y, 0f) * 0.5f;

            // Create merged caster child
            var go = new GameObject($"{childPrefix}{bounds.xMin + x}_{bounds.yMin + y}_{rectW}x{rectH}");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = ctr;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale    = Vector3.one;

            // Invisible sprite as silhouette for ShadowCaster2D
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = squareSprite;
            sr.color  = new Color(1f, 1f, 1f, 0f); // invisible
            sr.sortingLayerID = sortingLayerID;
            sr.sortingOrder   = sortingOrder;

            // Scale quad so its bounds match the merged rectangle, independent of PPU
            Vector2 spriteWorldSize = sr.sprite.bounds.size; // size in world units at scale = 1
            float scaleX = (spriteWorldSize.x != 0f) ? size.x / spriteWorldSize.x : 1f;
            float scaleY = (spriteWorldSize.y != 0f) ? size.y / spriteWorldSize.y : 1f;
            go.transform.localScale = new Vector3(scaleX, scaleY, 1f);

            // Shadow caster uses renderer silhouette to block 2D lights
            var sc = go.AddComponent<ShadowCaster2D>();
            sc.castsShadows = true;
            sc.selfShadows  = false;
        }
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var c = transform.GetChild(i);
            if (c.name.StartsWith(childPrefix))
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(c.gameObject);
                else Destroy(c.gameObject);
#else
                Destroy(c.gameObject);
#endif
            }
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(TilemapShadowCaster2D))]
public class TilemapShadowCaster2DEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GUILayout.Space(6);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Generate")) ((TilemapShadowCaster2D)target).Generate();
            if (GUILayout.Button("Clear"))    ((TilemapShadowCaster2D)target).Clear();
        }
    }
}
#endif
