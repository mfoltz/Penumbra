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

