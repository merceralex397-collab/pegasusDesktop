## Packaging retry — 2026-08-29

On branch `task/desktop-dev-msix`:

- `pwsh -NoProfile -File ./.codex/skills/winui-dev-workflow/BuildAndRun.ps1 src/Pegasus.Desktop/Pegasus.Desktop.csproj /p:Configuration=Release -SkipRun` — exit 0; Release x64 build succeeded and reported output `src/Pegasus.Desktop/bin/x64/Release/net10.0-windows10.0.26100.0/win-x64`.
- `winapp cert info ./devcert.pfx` — subject `CN=Collision Engineers`, thumbprint `AC3468D9C8D1FF64FAE3980F93A0E92CC0BA3AED`, private key present.
- `winapp package ./src/Pegasus.Desktop/bin/x64/Release/net10.0-windows10.0.26100.0/win-x64 --cert ./devcert.pfx --self-contained` — exit 1: `Runtime files not found at .winapp/self-contained/x64/deployment`.
- `dotnet publish ... --runtime win-x64 --self-contained true -p:WindowsAppSDKSelfContained=true` — exit 0 and produced the publish output, but did not populate the WinApp CLI deployment directory; no tracked files changed.

The exact package command still fails on the local toolchain. Existing identity/certificate matching is therefore not the remaining cause. FND-039 stays in Review and no package/install proof is claimed.
