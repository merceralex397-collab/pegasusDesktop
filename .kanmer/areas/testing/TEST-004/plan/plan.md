# Plan — TEST-004 Desktop ViewModelTests project

## Governing documents

This ticket remains docs_todo: true until the planned desktop governing documents are authored. The local Test/UAT and locked-decision material is binding now; do not create a competing product document in this task.

## Chosen approach

Scaffold tests/Pegasus.Desktop.ViewModelTests targeting net10.0-windows10.0.26100.0 with no UI-thread requirement.

## Steps

1. Inspect existing test framework, target framework and package conventions.
2. Create the desktop view-model test project with no XAML dispatcher or UI-thread dependency.
3. Reuse one shared fake clock/date convention and generated/gateway fakes.
4. Add it to the solution and run focused tests.

## Verification

- The project targets the approved Windows TFM and runs headlessly.
- Tests do not require an installed MSIX or UI thread.
- Locked restore and Release build pass.

## Risks

Do not add another FixedTimeProvider or a desktop-specific business-rule copy.

Use the detected runner/framework and record exact command output when implementation begins. Complete a simplification pass and independent review before merge.
