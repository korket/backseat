# Windows input research

Status: Evidence required

## Goal

Document Windows input mechanisms only as they become relevant to Backseat.

## Candidate mechanisms

Potential mechanisms include:

- accessibility/UI Automation actions;
- window messages;
- targeted APIs exposed by an application framework;
- global system input;
- isolated interactive sessions.

This list is not an implementation recommendation.

## Required evidence for each mechanism

For any mechanism Backseat considers, document:

- whether it requires foreground focus;
- whether it moves the user's physical cursor;
- whether it works against custom-rendered applications;
- whether the target can be covered or minimized;
- how success can be verified;
- known security or integrity-level restrictions;
- whether failure is detectable.

## Rule

Do not design a production input stack from platform folklore.

Create an experiment and record actual behavior.
