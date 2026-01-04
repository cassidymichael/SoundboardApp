# Architecture Decision Records

This directory contains Architecture Decision Records (ADRs) documenting significant technical decisions made during development.

## Index

| ADR | Title | Status | Summary |
|-----|-------|--------|---------|
| [001](adr-001-external-virtual-audio-device.md) | External Virtual Audio Device | Accepted | Require external software (VoiceMeeter) for mic routing rather than building it in |
| [002](adr-002-dual-independent-audio-buses.md) | Dual Independent Audio Buses | Accepted | Two separate WASAPI output streams for monitor and inject devices |
| [003](adr-003-voice-limit-and-priority.md) | Voice Limit and Priority | Accepted | Maximum 4 simultaneous voices with FIFO culling and Protected flag |
| [004](adr-004-audio-format-standardization.md) | Audio Format Standardization | Accepted | Standardize all audio to 48kHz float32 stereo, decode on first load |
| [005](adr-005-global-hotkey-implementation.md) | Global Hotkey Implementation | Accepted | Use Win32 RegisterHotKey API (works in ~90% of scenarios, fails in exclusive fullscreen) |

## When to Write an ADR

Write an ADR when:
- There are multiple viable approaches and you chose one
- The decision would confuse future developers if unexplained
- The choice constrains future development options
- Trade-offs were made for non-obvious reasons

## ADR Template

```markdown
# ADR-XXX: Title

## Status
Accepted | Superseded by ADR-XXX | Deprecated

## Date
YYYY-MM-DD

## Context
What problem are we solving? What constraints exist?

## Decision
What did we decide to do?

## Alternatives Considered
| Approach | Pros | Cons |
|----------|------|------|
| ... | ... | ... |

## Rationale
Why did we choose this approach?

## Consequences
### Positive
- ...

### Negative
- ...

### Technical Debt
- ...
```
