# Non-goals

Backseat is intentionally narrow during its early milestones.

The following are not current goals.

## Not an AI model

Backseat does not train or provide a computer-use model.

## Not a visual novel engine

Visual novels are an initial workload, not the core product.

Do not put route planning, character affinity logic, Ren'Py parsing, or story-specific heuristics into the core runtime.

## Not a hypervisor

Backseat may later integrate with isolated sessions or VMs, but building a hypervisor or VM manager is not an early goal.

## Not an RPA suite

Backseat is not trying to reproduce a full enterprise robotic process automation platform.

## Not a remote desktop product

Remote viewing, collaboration, and remote administration are outside the early scope.

## Not a cloud computer service

The first target is local Windows execution.

## Not cross-platform yet

Cross-platform abstractions are only useful where they naturally follow from the backend boundary.

Do not weaken the Windows design to simulate platform portability before another platform is an actual requirement.

## Not a GUI-first product

The CLI, runtime contracts, and compatibility behavior should stabilize before a dedicated GUI is designed.

## Not multi-agent orchestration

One agent controlling one session is enough for the early architecture.

## Not a custom OCR project

Use existing observation sources first. Add OCR only when a demonstrated workload requires it.
