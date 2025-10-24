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

1. Run `./dev_init.sh` from the repository root to provision the .NET SDK (via the local `.dotnet` folder when necessary), restore NuGet packages, and produce a Release build.
   - Set `BEPINEX_PLUGIN_DIR` to copy the compiled plugin to a different BepInEx directory (defaults to `/workspace/plugins`).
   - Use `DOTNET_CHANNELS` to override the space-separated list of .NET SDK channels installed locally (defaults to installing 8.0 for the C# 12 compiler and 6.0 for the net6.0 targeting pack). The legacy `DOTNET_CHANNEL` variable is still honored when `DOTNET_CHANNELS` is unset.
2. If you're working outside of `dev_init.sh`, install the .NET 7 SDK locally (the workflow pins to `7.0.x`) before running any manual build or test commands. Verify the installation with `dotnet --list-sdks` or follow the [official SDK installation guide](https://learn.microsoft.com/dotnet/core/install/).
3. When invoking builds manually, run `dotnet build ./Penumbra.csproj --configuration Release -p:RunGenerateREADME=false` so you're using the same arguments as `dev_init.sh` and the README generation target stays disabled during routine development builds.
4. After building, execute `dotnet test ./Penumbra.csproj --configuration Release --no-build` to validate the Release configuration without rebuilding artifacts.

## Credits

- [BloodyMerchant](https://github.com/oscarpedrero/BloodyMerchant) by [@Trodi](https://github.com/oscarpedrero) was invaluable in putting this together, many thanks to him and other listed contributors!
