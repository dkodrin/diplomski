// File: Assets/Scripts/Lighting/TilemapShadowCasterGenerator.cs
using UnityEngine;
using UnityEngine.Tilemaps;


#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[RequireComponent(typeof(Tilemap), typeof(TilemapRenderer))]
public class TilemapShadowCasterGenerator : MonoBehaviour
{
    [Header("Required")]
    public Sprite squareSprite;     // 1x1 white sprite (any solid square)

    [Header("Children naming")]
    public string childPrefix = "SC_"; // generated objects will start with this

    Tilemap _tm;
    Grid _grid;

    void OnEnable()
    {
        _tm = GetComponent<Tilemap>();
        _grid = GetComponentInParent<Grid>();
    }

    [ContextMenu("Generate Shadow Casters")]
    public void Generate()
    {
        if (_tm == null) _tm = GetComponent<Tilemap>();
        if (_grid == null) _grid = GetComponentInParent<Grid>();

        Clear();

        if (squareSprite == null)
        {
            Debug.LogWarning("[TilemapShadowCasterGenerator] Assign a 1x1 square sprite first.");
            return;
        }

        Vector3 cellSize = (_grid != null) ? (Vector3)_grid.cellSize : Vector3.one;

        var bounds = _tm.cellBounds;
        for (int y = bounds.yMin; y < bounds.yMax; y++)
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            var cell = new Vector3Int(x, y, 0);
            if (_tm.GetTile(cell) == null) continue;

            // Create child GO
            var go = new GameObject($"{childPrefix}{x}_{y}");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = _tm.GetCellCenterLocal(cell);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            // Transparent SpriteRenderer provides the silhouette for ShadowCaster2D
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = squareSprite;
            sr.color = new Color(1, 1, 1, 0); // invisible
            sr.sortingLayerID = GetComponent<TilemapRenderer>().sortingLayerID;
            sr.sortingOrder = GetComponent<TilemapRenderer>().sortingOrder;

            // Scale sprite to grid cell size if needed
            // Assume the sprite's pixels-per-unit makes it 1x1 units; if not, adjust via transform:
            go.transform.localScale = new Vector3(cellSize.x, cellSize.y, 1f);

            // Shadow caster from silhouette
            var sc = go.AddComponent<UnityEngine.Rendering.Universal.ShadowCaster2D>();
            sc.castsShadows = true;
            // UseRendererSilhouette = true is implicit when a SpriteRenderer is present

            // We don't want physics, but some setups prefer a trigger to ensure silhouette:
            // var box = go.AddComponent<BoxCollider2D>();
            // box.isTrigger = true;
            // box.size = Vector2.one;
        }
    }

    [ContextMenu("Clear Generated")]
    public void Clear()
    {
        // delete previously generated children
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
[CustomEditor(typeof(TilemapShadowCasterGenerator))]
public class TilemapShadowCasterGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GUILayout.Space(6);
        var gen = (TilemapShadowCasterGenerator)target;
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Generate Shadow Casters"))
            {
                gen.Generate();
            }
            if (GUILayout.Button("Clear Generated"))
            {
                gen.Clear();
            }
        }
    }
}
#endif
