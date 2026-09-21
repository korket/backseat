# Product vision

## One sentence

Backseat gives AI agents a controllable computing environment alongside the user's own desktop.

## Problem

Computer-use agents are often implemented as if they own the machine:

- they move the user's mouse;
- they type through the global keyboard;
- they steal foreground focus;
- they capture the entire desktop;
- they provide weak evidence about what input path was actually used.

That is acceptable for a disposable VM or dedicated machine, but it is a poor fit for a personal computer that a human is actively using.

## Vision

Backseat should make computer-use sessions behave more like independent, auditable workloads.

A human should be able to continue using their computer while an agent operates a specifically authorized application whenever the operating system and application permit it.

When this is not possible, Backseat should report the limitation rather than hiding it.

## First real workload

Visual novels are the first practical workload.

They are useful because they exercise:

- long-running sessions;
- continuous visual observation;
- repetitive input;
- menus and branching choices;
- custom-rendered applications;
- recording;
- recovery from pauses or unexpected dialogs.

Backseat must not become visual-novel-specific. The same runtime should later be usable for other desktop applications.

## Product principles

### Explicit boundaries

Agents should know exactly what they are allowed to control.

### Evidence over assumptions

Platform behavior must be measured. Backseat should record what delivery route was used and whether the user's foreground or cursor was affected.

### Small trusted runtime

Keep model reasoning outside the computer execution core.

### Replaceable backends

Backseat should be able to evolve from existing computer-use runtimes to native or isolated backends without rewriting the agent-facing API.

### Auditable sessions

Actions, observations, errors, and recordings should be inspectable after a run.
