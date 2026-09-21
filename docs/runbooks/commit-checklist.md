# Commit checklist

Use this before presenting a completed patch series.

```text
[ ] one logical change per commit
[ ] tests travel with the behavior they verify
[ ] commits build/test independently where practical
[ ] explicit paths were staged through scripts/stage.ps1
[ ] commits were created through scripts/commit.ps1
[ ] no unrelated cleanup or formatting
[ ] commit messages explain why
[ ] full verification passes
[ ] working tree is clean
[ ] final commit series was inspected
[ ] no WIP/oops/debug commits remain
[ ] no amend/rebase/reset/merge was performed automatically
[ ] pushes had explicit human approval; merges had explicit human instruction
```
