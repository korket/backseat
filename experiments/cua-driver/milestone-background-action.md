# Milestone proof: session-driven background action

Supervised end-to-end run of the first milestone acceptance criteria through `Session` + `CuaCliBackend`, not the raw CLI.

```text
Application: Windows Calculator (packaged WinUI app)
Backend: Cua Driver
Backend version: cua-driver 0.28.2 (x86_64-windows)
PermissionMode: standard (supervised)
Target (exe/PID/window ID): Calculator / pid 9124 / window 46860110
DeliveryRoute: accessibility for every click
```

## Procedure

1. Launched Calculator hidden (`launch_app`, `active=false`).
2. `Session.SelectTargetAsync` matched the backend-reported target exactly.
3. Fetched element tokens with the same runner the adapter uses, then issued four `Session.ExecuteAsync(ClickAction(token))` calls: Six, Multiply by, Seven, Equals.
4. Re-observed through the session and read the display element.

## Results against the milestone criteria

| Criterion | Result |
| --- | --- |
| Exact target discovery | pid 9124 / window 46860110 selected through backend authorization |
| One observation | `Display is 42` captured in the accessibility tree |
| One background-targeted action | Four clicks, all `delivery=Background`, `route=accessibility` |
| Trustworthy action receipt | Every click receipt reports background delivery and no foreground swap; `effect=Unverifiable` (the driver's conservative classification) with the result confirmed by re-observation |
| No unreported cursor movement or focus theft | Receipts report background delivery; `CursorMoved` stays `unknown` because the driver does not report it. The desktop was in human use, so this is receipt-based evidence, not an idle-desktop measurement |
| Clean session shutdown | `CloseAsync` reached `Closed` with the lifetime token cancelled, 1 observation and 4 actions logged |

## Findings

- Click receipts on this WinUI target are `Unverifiable` rather than `Confirmed`; verification by observation is mandatory, exactly as the Notepad experiment showed for `press_key`.
- Element tokens are the reliable addressing path. A guessed pixel coordinate for the title-bar close button hit Minimize instead, which then made the app suspend: a minimized packaged app returns an empty UIA tree and cannot be restored without `bring_to_front` (a focus raise the operator declined).
- `Observation` exposes the raw tree and screenshot but not structured element tokens; the smoke had to fetch tokens through the runner. A structured element list on `Observation` is a natural follow-up slice.
- `kill_app` provenance still refuses cross-invocation launches, so cleanup for a hidden-launched packaged app depends on a working close path.

## Conclusion

The first milestone is demonstrated end to end through Backseat's own runtime objects: discovery, authorization, observation, background-targeted actions with faithful receipts, and clean shutdown. The remaining caveats are receipt honesty (unknown stays unknown) and cleanup ergonomics, not capability gaps.
