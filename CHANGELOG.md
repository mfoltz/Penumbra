`1.2.9`
- Added player-to-player token trading via `.pen tradetokens`.

`1.2.8`
- Added config option (`Token Economy`; can be used without the `Token System`) to remove coin recipes when enabled; recipes are added back on world load when disabled where applicable.
- Spawn command no longer takes trader prefabs directly (e.g., `.pen sm 1`), defaulting to major noctem trader; other traders may still be used via config.
- Merchant positions now cleared in config when removed; apologies for the oversight, this should prevent unwanted autospawns.
- 25 max stock entries per merchant; extras are trimmed (more than 25 results in merchant interface issues, per user reports).

`1.1.7`
- fixed json deserialization for tokens/login data

`1.1.6`
- merchants with restock intervals of 0 will not refill their wares as was originally intended
- fixed daily login timestamp

`1.1.5`
- integrated token system
- added name, trader prefab and position entries to config; name for easier keeping track of which wares are which and is also used to verify existence in world, trader prefab and position for saving existing merchant details to then be able to be used on another server (if these have valid values and no existing merchants detected with same name mod will spawn them in at that location with the prefab)
- changed faction for merchants to prevent being targeted for combat

`1.0.4`
- minor change after hotfix

`1.0.3`
- Back to VRising.Unhollowed.Client nuget for github workflow, versioning for Thunderstore

`1.0.2`
- Updated for VRising 1.1 compatibility

`1.0.1`
- added missing VCF dependency to thunderstore.toml
 
`1.0.0`
- initial Thunderstore release

