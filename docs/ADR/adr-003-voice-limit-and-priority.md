# ADR-003: Voice Limit and Priority System

## Status

Accepted (with noted uncertainty)

## Date

2026-01-04

## Context

When multiple sounds play simultaneously, we need a policy for:
1. How many concurrent sounds to allow
2. What happens when the limit is exceeded
3. How users can influence which sounds survive

## Decision

We implement:
- **Maximum 4 simultaneous voice pairs** (one voice per output bus)
- **FIFO culling**: When limit exceeded, oldest voice is killed first
- **Protected flag**: Voices marked protected are culled last
- **StopOthers flag**: Sounds can optionally cut all unprotected voices when triggered

### Culling Algorithm

```
When new sound triggered AND active_voices >= 4:
  WHILE active_voices >= 4:
    IF unprotected voices exist:
      Kill oldest unprotected voice (FIFO)
    ELSE:
      Kill oldest voice overall (even if protected)
```

## Alternatives Considered

| Approach | Pros | Cons |
|----------|------|------|
| **4-voice FIFO (chosen)** | Simple, predictable, low overhead | Arbitrary limit; no priority granularity |
| **Unlimited voices** | No artificial limit | No safety bound; potential for audio thread overload under rapid triggering |
| **8 or 16 voices** | More headroom | Still arbitrary; doesn't solve fundamental "what to cull" question |
| **Priority-weighted culling** | Fine-grained control | More complex; requires per-tile priority UI |
| **Loudest-survives** | Perceptually smart | CPU overhead for RMS tracking; volume-dependent behavior |
| **User-configurable limit** | Power user flexibility | Adds cognitive load; shifts troubleshooting burden |

## Rationale

### Why 4?

**Honest answer: This is inherited from the original project with no documented justification.**

Analysis shows:
- **Not memory-constrained**: Per-voice overhead is ~120 bytes; 32 voices = 3.8 KB
- **Not CPU-constrained**: Per-voice mixing cost is ~12 ops/sample; 32 voices at 48kHz uses <1% CPU
- **Empirically acceptable**: Typical interactive use involves 1-3 simultaneous sounds; 4 provides headroom

The limit is conservative rather than derived. It could be raised to 8 with no performance impact.

### Why FIFO?

- **Predictable**: Users can reason about "oldest sound gets cut"
- **Simple**: No additional per-tile configuration needed
- **Matches mental model**: "Sounds play until limit, then oldest makes room"

### Why Protected flag?

- **Use case**: Background music/ambience should persist while SFX come and go
- **Binary simplicity**: On/off is easier than priority numbers
- **Covers 90% of needs**: Most users need "this one sound should never be cut"

## Consequences

### Positive
- Simple mental model for users
- Protected flag covers common "background music + SFX" pattern
- Low implementation complexity
- No performance concerns at current limit

### Negative
- **Arbitrary limit**: 4 is not derived from any constraint; could be 8
- **No user feedback**: When voices are culled, there's no visual indicator
- **Edge case confusion**: With 4 protected sounds, the 5th protected sound still kills the oldest protected (may surprise users)
- **No priority granularity**: Can't express "this sound is more important than that one" beyond binary protected/unprotected

### Technical Debt
- Consider raising to 8 if users report hitting the limit
- Add status bar notification when culling occurs
- Consider per-tile priority (1-10) for power users who need fine-grained control

## Implementation Notes

Key files:
- `Services/AudioEngine.cs` - Voice cap enforcement and culling logic
- `Models/TileConfig.cs` - Protected and StopOthers flags
- `Audio/Voice.cs` - VoiceState tracking

The `StopOthers` flag is processed BEFORE the voice cap check, so a StopOthers sound clears all unprotected voices regardless of whether the cap would be hit.
