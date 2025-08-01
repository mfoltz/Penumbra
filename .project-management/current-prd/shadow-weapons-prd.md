# Shadow Weapons PRD

## Problem Statement
Players want unique Shadow weapons that grant special bonuses and abilities when equipped. Currently the mod lacks configurable stat boosts and automatic ability replacement for these weapons. Merchants also should be able to sell upgraded Shadow weapons.

## Objectives
1. Allow custom stat buffs for Shadow weapons via configuration.
2. Replace player abilities when a Shadow weapon is equipped.
3. Provide merchant integration so Shadow weapons can appear in merchant stock if desired.

## Features & Acceptance Criteria
- **Configurable Stat Buffs**
  - Each Shadow weapon type supports entries for damage bonuses and other stat modifiers.
  - Buffs apply only while the weapon is equipped.
- **Ability Replacement**
  - On equip, specified abilities are swapped with Shadow versions.
  - Abilities revert when the weapon is unequipped.
- **Configuration Entries**
  - Settings file includes sections for each weapon defining stat buffs and ability replacements.
  - Optional merchant settings determine if Shadow weapons can be purchased.
- **Merchant Integration**
  - If enabled, merchants can stock Shadow weapons using existing merchant configuration logic.

## Timeline Estimate
- PRD & planning: 1 day
- Implementation: 4 days
- Testing & bug fixes: 2 days

## Stakeholders
- Mod maintainers
- Community players requesting advanced Shadow weapons
- Server administrators who manage merchant stock

