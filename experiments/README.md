# Experiments

This directory is for disposable technical experiments.

Real-desktop tests must be explicitly initiated by a human and stay opt-in (see `docs/runbooks/compatibility-testing.md`).

Record the delivery route, whether the cursor moved, and whether focus changed for every action. Never claim background-safe behavior without a backend receipt.

Experiments answer uncertain questions before those assumptions become production architecture.

Each experiment should contain:

- the question;
- setup;
- exact reproduction steps;
- observed result;
- conclusion;
- implications for Backseat.

Experiment code may be rough.

Do not make production code depend on files under this directory.
