# AGENTS.md

## Setup commands
- Run `./scripts/bootstrap.sh` from the repository root. It installs the required .NET SDKs into `.dotnet/` when missing, restores NuGet packages, builds the plugin, and copies the DLL into `${BEPINEX_PLUGIN_DIR:-/workspace/plugins}`.
- Override SDK channels via the `DOTNET_CHANNELS` environment variable (space-separated, default `"8.0 6.0"`). The legacy `DOTNET_CHANNEL` fallback is honored only when `DOTNET_CHANNELS` is unset.
- When working outside `scripts/bootstrap.sh`, ensure the .NET 7 SDK (`7.0.x`) is installed locally and verify with `dotnet --list-sdks` before invoking any builds or tests.

## Testing instructions
- Prefer the bootstrap script for routine verification; otherwise run `dotnet build ./Penumbra.csproj --configuration Release -p:RunGenerateREADME=false` to match CI arguments without regenerating the README.
- After a successful build, execute `dotnet test ./Penumbra.csproj --configuration Release --no-build` so tests run against the freshly produced Release binaries.

## Code style conventions
- Follow the repository `.editorconfig`: C# files use 4-space indentation, spaces instead of tabs, CRLF newlines, and do not auto-insert a trailing newline.
- Keep `using` directives sorted without separating `System` imports, and place them outside the namespace declaration (`csharp_using_directive_placement = outside_namespace`).
- Prefer explicit type names rather than `var` except for built-in types or assignments where the type is unambiguous; align other language and formatting choices with the `.editorconfig` defaults.

## Workflow expectations
- Maintain Semantic Versioning. Bump major for breaking saves/client requirements, minor for new backwards-compatible features, patch for fixes or balance tweaks.
- Keep `<Version>` in `Penumbra.csproj`, `thunderstore.toml`, and the `## Unreleased` section of `CHANGELOG.md` in sync. Use the GitHub Actions `Advance version` workflows to update them when preparing a release; these workflows also satisfy the CI version guard.
- The guarded `Build` workflow runs on `codex` pushes. Increment the version before opening a release PR so CI passes, or manually dispatch the workflow to bypass the guard for reruns.
- For prereleases, dispatch the `Build` workflow with `ready_to_ship=true` to publish artifacts to a prerelease tag. Final Thunderstore releases depend on a published GitHub release backed by that build; approve the protected environment when the `Release` workflow prompts.
- Update documentation and configuration alongside code changes so README guidance and this playbook remain accurate for future contributors.
