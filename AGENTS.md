# AGENTS.md

## Setup commands
- Run `./scripts/bootstrap.sh` from the repository root. It installs the required .NET SDKs into `.dotnet/` when missing, restores NuGet packages, builds the plugin, and copies the DLL into `${BEPINEX_PLUGIN_DIR:-/workspace/plugins}`.
- Override SDK channels via the `DOTNET_CHANNELS` environment variable (space-separated, default `"8.0 6.0"`). The bootstrap script installs both the 8.0 and 6.0 channels so the preview compiler features stay available while matching the runtime target; mirror that mix when installing the SDK manually. The legacy `DOTNET_CHANNEL` fallback is honored only when `DOTNET_CHANNELS` is unset.
- When working outside `scripts/bootstrap.sh`, ensure a modern SDK that understands the preview features in `Penumbra.csproj` is present—while the project still targets `net6.0`, the C# preview language features require the .NET 8 (or at least 7) SDK. Verify your local install with `dotnet --list-sdks` before invoking any builds or tests.
  You should see both `8.0.x` and `6.0.x` entries; the newer SDK provides the compiler features while the 6.0 SDK matches the runtime target.

## Testing instructions
- Prefer the bootstrap script for routine verification; local builds now regenerate the README automatically, so `./scripts/bootstrap.sh` ensures docs stay in sync while compiling the plugin.
- In the rare cases where you must skip README generation (for example, during smoke builds on slower machines), set `RUN_GENERATE_README=false` in your environment **or** pass `-p:RunGenerateREADME=false` to `dotnet build`—both toggles share the same MSBuild property so opt-outs behave consistently.
- Commit the regenerated README alongside related command updates; CI already exports `GITHUB_ACTIONS=true`/`CI=true`, so the generator target skips automatically in pipeline runs and reviewers only see documentation diffs when you check them in locally.
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
