# Shadow Weapons PRD

## Problem Statement
Players want unique Shadow weapons that grant special bonuses and abilities when equipped. Currently the mod lacks configurable stat boosts and automatic ability replacement for these weapons.

## Objectives
1. Allow custom stat buffs for Shadow weapons via configuration.
2. Replace player abilities when a Shadow weapon is equipped.
3. Handle setting ability cooldowns to reasonable numbers, as most custom abilities won't have a cooldown; see ability script patch, can make this configurable probably

## Features & Acceptance Criteria
- **Configurable Stat Buffs**
  - Each Shadow weapon type supports entries for damage bonuses and other stat modifiers.
  - Buffs apply only while the weapon is equipped.
- **Ability Replacement**
  - On equip, specified abilities are swapped with Shadow versions.
  - Abilities revert when the weapon is unequipped.
- **Configuration Entries**
  - Settings file includes sections for each weapon defining stat buffs, ability replacements and their cooldowns.

## Timeline Estimate
- PRD & planning: 1 day
- Implementation: 4 days
- Testing & bug fixes: 2 days

## Stakeholders
- Mod maintainers
- Community players requesting advanced Shadow weapons
