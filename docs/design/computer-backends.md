# Computer backends

Status: Draft

## Goal

Backseat should be able to use an existing computer-use runtime today while preserving the option to add a native or isolated implementation later.

## Capability-based design

Backends differ.

Do not pretend every backend supports the same guarantees.

Capabilities may include:

- screenshot capture;
- accessibility tree;
- targeted click;
- targeted text input;
- key input;
- scroll;
- wait;
- focus target;
- recording;
- background-safe click;
- background-safe typing;
- foreground activation;
- cursor restoration.

The backend contract should expose capabilities or return explicit unsupported results rather than silently degrading.

## Action delivery

An action request should describe intent.

The receipt should describe actual delivery.

Terms `Background-safe`, `Foreground-required`, and `Intrusive input` are defined in `docs/product/terminology.md`.

Coordinates in action requests are target-client relative unless the backend documents otherwise; the receipt must report any translation to screen coordinates and any fallback delivery route.

Example:

```text
requested:
  click target=(x, y)
  require_background_safe=true

receipt:
  succeeded=false
  reason=foreground_required
```

A backend must not reinterpret this as permission to send global input.

## Backend-specific data

Backend identifiers and diagnostics may need to be retained.

Keep them in backend-specific metadata or typed extension fields rather than leaking backend types through the entire application.

## First backend

The first compatibility work will evaluate Cua Driver.

This is an implementation strategy, not a permanent architectural commitment.
