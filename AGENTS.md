# Penumbra agent playbook

This repository ships a set of automation helpers and formatting rules. Follow the guidance below whenever you modify files under this tree.

## Environment & tooling
- Run `./scripts/bootstrap.sh` from the repository root to provision .NET SDKs locally (installs into `.dotnet/` when required), restore NuGet dependencies, build the plugin, and copy the DLL to `${BEPINEX_PLUGIN_DIR:-/workspace/plugins}`.
- Override SDK channels with `DOTNET_CHANNELS` (space-separated; defaults to `"8.0 6.0"`). The legacy `DOTNET_CHANNEL` variable is honored only when `DOTNET_CHANNELS` is unset.
- Set `BEPINEX_PLUGIN_DIR` when you need the DLL copied somewhere other than `/workspace/plugins`.

## Build & test workflow
- Use `dotnet build ./Penumbra.csproj --configuration Release -p:RunGenerateREADME=false` for manual builds so the README generator stays disabled outside of CI.
- After a successful build, validate changes with `dotnet test ./Penumbra.csproj --configuration Release --no-build`.

## Formatting & style
- Respect `.editorconfig` at the repo root: C# files use 4-space indentation, spaces instead of tabs, CRLF line endings, and no implicit trailing newline insertion.
- Keep `using` directives sorted as the IDE/tooling enforces them and place them outside the namespace declaration (per `csharp_using_directive_placement = outside_namespace`).
- Prefer explicit type names over `var` unless the type is a built-in or crystal-clear from the assignment (the configuration disables the implicit forms).

## Code review expectations
- Break down changes into focused commits with descriptive messages.
- Update documentation and configuration alongside code when workflows change so future contributors can rely on the README and this guide.
