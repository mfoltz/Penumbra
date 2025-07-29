# Merchant Improvements PRD

## Problem Statement
Users want easier ways to modify merchant inventories directly in game and to have merchants with rotating stock and item rarity upgrades. Currently this requires manual configuration outside the game and lacks features for stock rotation or probabilistic item upgrades.

## Objectives
1. Provide an in-game command to add or update merchant configurations, enabling quick addition of items to merchant stock without editing config files.
2. Allow trader stock to rotate with configurable spawn percentages similar to map NPC merchants.
3. Support chance based upgrades when buying items (e.g., chance for a purchased item to become legendary).

## Features & Acceptance Criteria
- **In-Game Merchant Configuration Command**
  - Command accessible via chat or console.
  - Allows specifying merchant ID, item, price, and amount.
  - Updates existing merchant entries if present, otherwise adds new entries.
  - Validation to prevent invalid items or amounts.

- **Rotatable Stock for Traders**
  - Trader configuration can include multiple items with individual spawn percentages.
  - On restock, items appear according to their chance values.
  - Must persist between restocks and reloads.

- **Random Item Upgrade on Purchase**
  - Configurable percentage chance for an item to upgrade rarity when purchased.
  - Upgrades should respect existing item tier logic.

## Timeline Estimate
- PRD & planning: 1 day
- Implementation: 5 days
- Testing & bug fixes: 2 days

## Stakeholders
- Mod maintainers
- Community players requesting easier merchant management
