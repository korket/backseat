# Terminology

## Agent

The reasoning system that decides what computer action to request.

Backseat does not assume a specific model provider.

## Backend

The implementation responsible for interacting with an operating system or computer-use runtime.

## Session

A long-lived Backseat execution context for one controlled workload.

## Target

The specific application or window that the session is authorized to observe and control.

## Observation

A timestamped representation of what the agent can perceive.

## Action

A requested computer operation.

## ActionReceipt

The authoritative result of an attempted action.

## Background-safe

An operation that does not require moving the user's physical cursor or taking over the user's active application.

This term must describe observed backend behavior, not an assumption.

## Foreground-required

An operation that needs the target application to become the active foreground application.

## Intrusive input

Input that affects the user's global interactive desktop, such as global mouse movement or keyboard injection.

## Run

The persisted record of one session.

## Compatibility experiment

A controlled investigation of how a backend behaves against a real application.

Experiments produce evidence. They are not production implementation.
