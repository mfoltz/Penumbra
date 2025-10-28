## Table of Contents

- [Sponsors](#sponsors)
- [Features](#features)
- [Configuration](#configuration)
- [Commands](#commands)
- [Development](#development)
- [Credits](#credits)

## Sponsor this project

[![patreon](https://i.imgur.com/u6aAqeL.png)](https://www.patreon.com/join/4865914)  [![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/zfolmt)

## Sponsors

Jairon O.; Odjit; Jera; Kokuren TCG and Gaming Shop; Rexxn; Eduardo G.; DirtyMike; Imperivm Draconis; Geoffrey D.; SirSaia; Robin C.; Colin F.; Jade K.; Jorge L.; Adrian L.;

## Features

- **Configurable Merchants:** Comes with several default sets of wares that can be modified, replaced or added to (follow the example template shown in the configuration section below and increment merchant number for additional sets of wares).
- **Tokens & Login Rewards:** Earn tokens for time spent online and redeem them for trader currency! Optional daily login bonus.

## Configuration

### Merchant(#)
- **Name**: (string, default: "Merchant#")
  Name/identifier for the merchant, used for easy referencing and verifying existence when autospawning merchants.
- **Output Items**: (string, default: "-1370210913,1915695899,862477668,429052660,28358550")
  Item prefabGUIDs for outputs.
- **Output Amounts**: (string, default: "1,1,1500,15,250")
  Item output amounts.
- **Input Items**: (string, default: "-257494203,-257494203,-257494203,-257494203,-257494203")
  Item prefabGUIDs for inputs.
- **Input Amounts**: (string, default: "250,250,250,250,250")
  Item input amounts.
- **Stock Amounts**: (string, default: "99,99,99,99,99")
  Stock amounts for outputs.
- **Restock Time**: (int, default: 60)
  Time between restocks in minutes (1min minimum, no option to not restock atm since I completely forgot about that >_>).
- **Trader Prefab**: (int, default: 0)
  Trader prefab ID; leave this blank, will be saved out by the mod after spawning a merchant.
- **Position**: (string, default: "")
  Position of merchant spawn in world; leave this blank, will be saved out by the mod after spawning a merchant.
- **Roam**: (bool, default: false)
  Pace around or stay put.

## Commands
- `.penumbra spawnmerchant [#]` 🔒
  - Spawns merchant as configured at mouse; defaults to major noctem trader (can set per merchant in config).
  - Shortcut: *.pen sm [#]*
- `.penumbra removemerchant` 🔒
  - Removes hovered merchant.
  - Shortcut: *.pen rm*
- `.penumbra redeemtokens`
  - Redeems tokens for configured item.
  - Shortcut: *.pen rt*
- `.penumbra gettokens`
  - Shows and updates tokens.
  - Shortcut: *.pen gt*
- `.penumbra tradetokens <player> <amount>`
  - Transfers tokens to another player.
  - Shortcut: *.pen tt <player> <amount>*
- `.penumbra getdaily`
  - Check time remaining or receive daily login reward if eligible.
  - Shortcut: *.pen gd*

## Development

1. Run `./scripts/bootstrap.sh` from the repository root to provision the .NET SDK (via the local `.dotnet` folder when necessary), restore NuGet packages, and produce a Release build.
   - Set `BEPINEX_PLUGIN_DIR` to copy the compiled plugin to a different BepInEx directory (defaults to `/workspace/plugins`).
   - Use `DOTNET_CHANNELS` to override the space-separated list of .NET SDK channels installed locally (defaults to installing 8.0 for the C# 12 compiler and 6.0 for the net6.0 targeting pack). The legacy `DOTNET_CHANNEL` variable is still honored when `DOTNET_CHANNELS` is unset.
2. If you're working outside of `scripts/bootstrap.sh`, install the .NET 7 SDK locally (the workflow pins to `7.0.x`) before running any manual build or test commands. Verify the installation with `dotnet --list-sdks` or follow the [official SDK installation guide](https://learn.microsoft.com/dotnet/core/install/).
3. When invoking builds manually, run `dotnet build ./Penumbra.csproj --configuration Release -p:RunGenerateREADME=false` so you're using the same arguments as `scripts/bootstrap.sh` and the README generation target stays disabled during routine development builds.
4. After building, execute `dotnet test ./Penumbra.csproj --configuration Release --no-build` to validate the Release configuration without rebuilding artifacts.

### Release workflow

- **Semantic versioning policy:** The mod follows Semantic Versioning so the version guard and Thunderstore metadata stay predictable. Bump the **major** number when a change requires players to update their game client or wipes persistent progress, the **minor** number for new features or backwards-compatible content drops, and the **patch** number for bug fixes, balance tweaks, or other maintenance-only updates.
- **Codex branch CI:** Every push to `codex` runs the `Build` workflow for verification only. These builds honor the version guard in `.github/workflows/build.yml`, so bump the `<Version>` in `Penumbra.csproj` before opening a release PR. Keeping `codex` ahead of the latest `v*` tag ensures the guard allows the build to run while still preventing accidental repackaging of an already-published version.
- **Changelog workflow:** Keep the `## Unreleased` section of [`CHANGELOG.md`](CHANGELOG.md) current. The version-advance workflows promote those notes into a new `## <version>` heading during a release and restore an empty `## Unreleased` placeholder for future updates. Draft your release notes under `## Unreleased` before triggering the bump in the next bullet.
- **Version bump workflows (`Advance version`, `Advance major`, `Advance minor`):** Use the GitHub Actions tab to run `Advance version` whenever you need to bump the version without touching code. Choose `major`, `minor`, or `patch` from the picker (or use the dedicated shortcut workflows to skip that choice), target the branch you want to update (defaults to `codex`), and only provide a prerelease label such as `beta.1` when you want the resulting version treated as a preview build—leave it blank for stable releases. The workflow updates `Penumbra.csproj`, `thunderstore.toml`, and [`CHANGELOG.md`](CHANGELOG.md) in-place, then either pushes directly (if the branch is unprotected) or opens a PR. It also emits the `new_version` output that downstream workflows—like guarded `Build` runs or automation consuming `workflow_call`—can use. Because the files are already bumped, the Build workflow’s version guard sees the new SemVer and allows CI to proceed on the next push.
- **Promoting a prerelease:** When the branch is ready for external testing, manually dispatch the `Build` workflow with `ready_to_ship=true`. That flips the workflow into prerelease mode: it updates `thunderstore.toml`, produces a Release build under `./bin/Release/net6.0/`, and uploads the DLL plus `CHANGELOG.md` to a GitHub prerelease tagged `v<version>-pre`. Use this artifact bundle when sanity-checking what will land on Thunderstore.
- **Version bumps & overrides:** The version guard compares `Penumbra.csproj` against the newest `v*` tag. If you must rerun CI without bumping the version (for example to re-test a build), trigger the workflow manually—manual dispatch bypasses the guard. Otherwise, increment the version before retrying so that both the guard and Thunderstore metadata stay in sync.
- **Creating the release:** Once the build is signed off, create and publish the GitHub release for the final `v<version>` tag. Ensure a `Build` workflow run with `ready_to_ship=true` exists for that commit—the run uploads the Thunderstore-ready artifact to the release so the downstream pipeline can reuse the exact bits validated by CI.
- **Releasing to Thunderstore:** Publishing the GitHub release triggers the `Release` workflow automatically. Approve the protected environment when the workflow pauses for confirmation; the gate, combined with the hand-off from the `Build` run's artifact, guarantees Thunderstore receives the same payload that passed CI without requiring any manual downloads from the GitHub release page. After approval, the workflow promotes the packaged artifact directly to Thunderstore.
- **Republishing older builds:** If you need to reissue an already-validated build (for example, to retry a transient Thunderstore failure), manually dispatch the `Release` workflow with `workflow_dispatch` and set the `run_id` input to the original `Build` run you want to ship. The workflow will reuse that artifact instead of rebuilding, preserving the exact contents previously verified.

## Credits

- [BloodyMerchant](https://github.com/oscarpedrero/BloodyMerchant) by [@Trodi](https://github.com/oscarpedrero) was invaluable in putting this together, many thanks to him and other listed contributors!
