using UnityEngine;
using UnityEngine.Events;
using System;
using System.Linq;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class PressurePlate : MonoBehaviour
{
    [Header("Sensor Settings")]
    [Tooltip("Which layers can press this plate (players, crates, stones…)")]
    public LayerMask pressingMask;

    [Tooltip("If true, the plate locks permanently On the first time it is pressed.\n" +
             "Use this for single plates that trigger one-way events (ladder drop, etc.).\n" +
             "Has no effect when groupPlates is non-empty.")]
    public bool lockOnPress = false;

    [Tooltip("Other plates in the same group. When set, this plate shows\n" +
             "'Pressed' (yellow) when only some of the group are pressed,\n" +
             "and 'On' (green) only when every plate in the group is pressed.\n" +
             "Leave empty for a standalone plate that goes straight Off→On.")]
    public PressurePlate[] groupPlates;

    [Header("Plate Sprites")]
    public Sprite spriteOff;       // nobody on this plate
    public Sprite spritePressed;   // this plate is down but not all group plates are
    public Sprite spriteOn;        // all group plates (or this standalone plate) are pressed

    [Header("Events")]
    /// <summary>
    /// Fired whenever this plate's visual state changes.
    /// Arg = 0 (Off), 1 (Pressed), 2 (On)
    /// </summary>
    public UnityEvent<int> onStateChanged;

    // ── internals ──────────────────────────────────────────
    SpriteRenderer _sr;
    int            _pressCount  = 0;
    bool           _locked      = false;
    int            _lastState   = -1;
    bool           _initialized = false;

    /// <summary>True while at least one valid object is standing on this plate.</summary>
    public bool IsPressed => _pressCount > 0 || _locked;

    /// <summary>0 = Off, 1 = Pressed (partial group), 2 = On (all pressed / locked)</summary>
    public int LastState => _lastState;

    bool IsGrouped => groupPlates != null && groupPlates.Length > 0;

    // ── Unity ──────────────────────────────────────────────
    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();

        // When grouped, listen to every sibling so we can repaint when they change
        if (IsGrouped)
            foreach (var p in groupPlates)
                p.onStateChanged.AddListener(_ => RecalcState());

        RecalcState();
        _initialized = true;
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (_locked) return;
        if (((1 << col.gameObject.layer) & pressingMask) == 0) return;

        _pressCount++;
        RecalcState();
    }

    void OnTriggerExit2D(Collider2D col)
    {
        if (_locked) return;
        if (((1 << col.gameObject.layer) & pressingMask) == 0) return;

        _pressCount = Math.Max(0, _pressCount - 1);
        RecalcState();
    }

    // ── Public API ─────────────────────────────────────────

    /// <summary>
    /// Called by DoorController to permanently lock this plate in the On state.
    /// </summary>
    public void Lock()
    {
        if (_locked) return;
        _locked = true;
        RecalcState();
    }

    // ── Core state logic ───────────────────────────────────
    void RecalcState()
    {
        int state;

        if (_locked)
        {
            state = 2;
        }
        else if (!IsGrouped)
        {
            // Standalone plate: Off or On only
            state = (_pressCount > 0) ? 2 : 0;

            // Lock permanently on first press if requested
            if (state == 2 && lockOnPress)
                _locked = true;
        }
        else
        {
            // Grouped: show Pressed when this plate is down but not all siblings
            bool thisDown = _pressCount > 0;

            if (!thisDown)
            {
                // This plate isn't pressed — Off regardless of siblings
                state = 0;
            }
            else
            {
                // This plate is pressed — check if every sibling is also pressed
                bool allDown = groupPlates.All(p => p.IsPressed);
                state = allDown ? 2 : 1;
            }
        }

        if (state == _lastState && _initialized) return;
        _lastState = state;

        _sr.sprite = state == 0 ? spriteOff
                   : state == 1 ? spritePressed
                                : spriteOn;

        if (_initialized)
            onStateChanged.Invoke(state);
    }
}