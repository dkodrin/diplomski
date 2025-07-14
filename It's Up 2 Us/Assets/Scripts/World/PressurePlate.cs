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

    [Tooltip("If non-empty, runs in COMBINED mode using these child sensors")]
    public PressurePlate[] linkedPlates;

    [Header("Plate Sprites")]
    public Sprite spriteOff;      // no plates pressed
    public Sprite spritePressed;  // some but not all pressed (combined mode only)
    public Sprite spriteOn;       // solo pressed, or all linked plates pressed

    [Header("Events")]
    /// <summary>
    /// Fired whenever the combined state changes (after startup).
    /// Arg = 0 (Off), 1 (Pressed), 2 (On)
    /// </summary>
    public UnityEvent<int> onStateChanged;

    // internals
    SpriteRenderer _sr;
    Collider2D     _col;
    int            _pressCount = 0;  // SOLO mode only
    bool           _isPressed  = false;
    int            _lastState  = -1;
    bool           _initialized = false;

    /// <summary>0=Off, 1=Pressed, 2=On</summary>
    public int LastState => _lastState;

    bool IsCombined => linkedPlates != null && linkedPlates.Length > 0;

    void Awake()
    {
        _sr  = GetComponent<SpriteRenderer>();
        _col = GetComponent<Collider2D>();

        // offset sprite so it sits flush regardless of collider offset
        Vector3 colOff = _col.offset;
        _sr.transform.localPosition = -new Vector3(colOff.x, colOff.y, 0f);

        // subscribe to child plates if in combined mode
        if (IsCombined)
            foreach (var p in linkedPlates)
                p.onStateChanged.AddListener(_ => RecalcState());

        // initial sprite setup without firing events
        RecalcState();
        _initialized = true;
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (IsCombined) return;
        if (((1 << col.gameObject.layer) & pressingMask) == 0) return;

        _pressCount++;
        if (!_isPressed)
        {
            _isPressed = true;
            RecalcState();
        }
    }

    void OnTriggerExit2D(Collider2D col)
    {
        if (IsCombined) return;
        if (((1 << col.gameObject.layer) & pressingMask) == 0) return;

        _pressCount = Math.Max(0, _pressCount - 1);
        if (_isPressed && _pressCount == 0)
        {
            _isPressed = false;
            RecalcState();
        }
    }

    void RecalcState()
    {
        int state;

        if (!IsCombined)
        {
            // SOLO: Off (0) or On (2)
            state = _isPressed ? 2 : 0;
        }
        else
        {
            // COMBINED: count how many linked plates are pressed
            int cnt = linkedPlates.Count(p => p._isPressed);
            if      (cnt == 0)                  state = 0;
            else if (cnt < linkedPlates.Length) state = 1;
            else                                 state = 2;
        }

        if (state == _lastState && _initialized) return;
        _lastState = state;

        // update sprite
        _sr.sprite = (state == 0 ? spriteOff
                  : state == 1 ? spritePressed
                                : spriteOn);

        // fire event only after initialization
        if (_initialized)
            onStateChanged.Invoke(state);
    }
}
