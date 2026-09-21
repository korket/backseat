# Notepad typing experiment

Supervised standard-mode test through `cua-driver call` (one-shot CLI).
The human operator opened a fresh Untitled Notepad and approved the typing scope.
Only the test string below was written; no pre-existing content was touched.

```text
Application: Windows Notepad (packaged app, XAML host)
Version: Windows 11 inbox Notepad
Engine/framework if known: WinUI / XAML
Backend: Cua Driver
Backend version: cua-driver 0.28.2 (x86_64-windows)
PermissionMode: standard (built_in_default)
CapabilityBoundary: none (standard mode, supervised)
Target (exe/PID/window ID): Notepad.exe / pid 9316 / window 3213266
DeliveryRoute: accessibility (UIA ValuePattern) for type_text; synthetic_events for press_key
```

## Observation

- Pre-test snapshot: Document element empty, snapshot id recorded, element token acquired.
- Post-test snapshots read the full Document value back each time.

## Click

Not tested here (covered in `calculator.md`).

## Typing

- `type_text` with `element_token` wrote `Backseat CUA typing test 123` into the empty document.
- Receipt: `delivery.mode=background`, `route=accessibility`, `effect=confirmed` with `value_readback` evidence. Strongest receipt class observed so far.
- Re-snapshot confirmed the exact string in the Document value.
- Note: ValuePattern SetValue left the caret at position 0, not at end of text.

## Special keys

- `press_key` Return with the Document element token: receipt `delivery.mode=background`, `route=synthetic_events`, `effect=unverifiable`, plus `escalation={reason=delivery_failed, target=foreground}`.
- Re-snapshot showed the Return DID land (`\r` inserted at caret 0). The receipt was a false negative: the XAML provider published the change after the synchronous read-back.
- Foreground escalation was NOT attempted (outside the approved scope). The escalation hint is advisory, and this case shows it can fire even when background delivery succeeded.

## Foreground changed

No. No receipt reported a foreground swap. Caveat: Notepad was already the frontmost window (the operator had just opened it), so this run is weak evidence against focus theft; the strong evidence is `delivery.mode=background` in every receipt.

## Physical cursor moved

Cursor positions shifted during the session due to the operator's own concurrent activity. No driver call in this run moves the system cursor; all receipts report background delivery.

## Minimized behavior

Not re-tested here (covered in `calculator.md`).

## Covered behavior

Not tested.

## Unexpected behavior

- Receipt conservatism on XAML hosts: `press_key` reported `unverifiable` + foreground escalation while the keypress had actually landed. Backseat must treat `unverifiable` as "re-snapshot and check", never as proof of failure, and must never auto-escalate to foreground on this signal without policy approval.
- Test string left in the Untitled document (operator closes without saving).

## Conclusion

Background typing on a XAML host works with the strongest receipt class (`confirmed` + read-back). Special-key receipts are conservative false-negative-prone; verification-by-snapshot is mandatory. Combined with `calculator.md`, questions Q3 (background clicks), Q4 (typing without focus steal, within the already-foreground caveat), and Q5 (delivery routes) are answered for WinUI targets.
