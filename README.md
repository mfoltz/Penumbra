## Table of Contents

- [Sponsors](#sponsors)
- [Features](#features)
- [Configuration](#configuration)
- [Commands](#commands)
- [Credits](#credits)

## Sponsor this project

[![patreon](https://i.imgur.com/u6aAqeL.png)](https://www.patreon.com/join/4865914)  [![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/zfolmt)

## Sponsors

Jairon O.; Odjit; Jera; Kokuren TCG and Gaming Shop; Rexxn; Eduardo G.; DirtyMike; Imperivm Draconis; Geoffrey D.; SirSaia; Robin C.;

## Features

- **Configurable Merchants:** Comes with several default sets of wares that can be modified, replaced or added to (follow the example template shown in the configuration section below and increment merchant number for additional sets of wares).

## Configuration

### Merchant(#)
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
  Time between restocks in minutes (5 minimum or can use 0 for no restocking).
- **Roam**: (bool, default: false)
  Pace around or stay put.

## Commands
- `.penumbra spawnmerchant [TraderPrefab] [Wares]` 🔒
  - Spawns merchant at mouse location with configured wares ('.pen sm 1631713257 3' will spawn a major noctem trader with the third wares as configured).
  - Shortcut: *.pen sm [TraderPrefab] [Wares]*
- `.penumbra removemerchant` 🔒
  - Removes hovered merchant.
  - Shortcut: *.pen rm*

## Credits

- [BloodyMerchant](https://github.com/oscarpedrero/BloodyMerchant) by [@Trodi](https://github.com/oscarpedrero) was invaluable in putting this together, many thanks to him and other listed contributors!
