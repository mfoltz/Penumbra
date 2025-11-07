# AGENTS.md

## Setup Commands
- Run `./scripts/bootstrap.sh` from the repository root. It installs the required .NET SDKs into `.dotnet/` when missing, restores NuGet packages, builds the plugin, and copies the DLL into `${BEPINEX_PLUGIN_DIR:-/workspace/plugins}`.
- Override SDK channels via the `DOTNET_CHANNELS` environment variable (space-separated, default `"8.0 6.0"`). The bootstrap script installs both the 8.0 and 6.0 channels so the preview compiler features stay available while matching the runtime target; mirror that mix when installing the SDK manually. The legacy `DOTNET_CHANNEL` fallback is honored only when `DOTNET_CHANNELS` is unset.
- When working outside `scripts/bootstrap.sh`, ensure a modern SDK that understands the preview features in `Penumbra.csproj` is present—while the project still targets `net6.0`, the C# preview language features require the .NET 8 (or at least 7) SDK. Verify your local install with `dotnet --list-sdks` before invoking any builds or tests. You should see both `8.0.x` and `6.0.x` entries; the newer SDK provides the compiler features while the 6.0 SDK matches the runtime target.

## Testing Instructions
- Prefer the bootstrap script for routine verification; local and CI builds now regenerate the README automatically, so `./scripts/bootstrap.sh` keeps documentation and binaries aligned while compiling the plugin.
- When automation churn becomes noisy (for example, on quick smoke builds), opt out of README generation by setting `RUN_GENERATE_README=false` **or** adding `-p:RunGenerateREADME=false` to `dotnet build`; both switches map to the same MSBuild property and apply locally as well as in pipelines when explicitly provided.
- Commit the regenerated README alongside related command updates so reviewers see the expected documentation diffs that come from the auto-refresh step in CI.
- After a successful build, execute `dotnet test ./Penumbra.csproj --configuration Release --no-build` so tests run against the freshly produced Release binaries.

## Coding Expectations
- **Name things clearly** (methods/vars/types express intent; avoid abbreviations that hide meaning).
- **Single Responsibility** per class/method; keep methods short and purposeful.
- **Avoid magic numbers**: use named `const`/`readonly` or config.
- **State & invariants**: assert/guard preconditions; prefer early returns over deeply nested branches.
- **Small, focused tests**: cover critical paths & edge cases near changes you introduce.
- **Defer to `.editorconfig`** for all formatting and style rules. Do not duplicate or override locally.

## Preview Features
**Goal:** Use modern C# features when they improve clarity/maintainability; fall back gracefully on lanes without the needed SDK.
**Language (compiler) features**
- Prefer C# **preview** when available. If a lane fails due to preview, downshift to stable idioms in that PR.
- Keep `Nullable` **enabled** for safer patterns.
**API (library) preview**
- Opt into preview APIs only when the benefit is clear; guard via `<EnablePreviewFeatures>true</EnablePreviewFeatures>` and an assembly-level `[RequiresPreviewFeatures]` in the affected project.
- If preview APIs cause CI noise, prefer a stable alternative unless essential.
**Authoritative config**
- Language/version & preview toggle → `Directory.Build.props` (or the project’s `.csproj` if you prefer per-project).
- Formatting & naming rules → `.editorconfig`.
**Modern constructs to prefer (when analyzers suggest them)**
- Collection expressions/initializers, null coalescing/propagation (`??`, `??=`, `?.`), pattern matching, switch expressions, object/with initializers, file-scoped namespaces.

## Workflow Policy
- Maintain Semantic Versioning. Bump major for breaking saves/client requirements, minor for new backwards-compatible features, patch for fixes or balance tweaks.
- Keep `<Version>` in `Penumbra.csproj`, `thunderstore.toml`, and the `## WIP` section of `CHANGELOG.md` in sync. Use the GitHub Actions `Advance version` workflows to update them when preparing a release; these workflows also satisfy the CI version guard.
- The guarded `Build` workflow runs on `codex` pushes. Increment the version before opening a release PR so CI passes, or manually dispatch the workflow to bypass the guard for reruns.
- For prereleases, dispatch the `Build` workflow with `ready_to_ship=true` to publish artifacts to a prerelease tag. Final Thunderstore releases depend on a published GitHub release backed by that build; approve the protected environment when the `Release` workflow prompts.
- Update documentation and configuration alongside code changes so README guidance and this playbook remain accurate for future contributors.

## Sources of Truth
- **Style/formatting** → `.editorconfig`
- **Project/targeting** → `Penumbra.csproj`
- **Changelog & versions** → `CHANGELOG.md`
- **Distribution metadata** → `thunderstore.toml`
- **CI/release logic** → `.github/workflows/*`

