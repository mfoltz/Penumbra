using ProjectM;
using ProjectM.Gameplay.Scripting;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Entities;

namespace Penumbra.Utilities;
internal static class BuffUtilities
{
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static DebugEventsSystem DebugEventsSystem => Core.DebugEventsSystem;
    public static bool TryApplyBuff(Entity character, PrefabGUID buffPrefab)
    {
        ApplyBuffDebugEvent applyBuffDebugEvent = new()
        {
            BuffPrefabGUID = buffPrefab
        };

        FromCharacter fromCharacter = new()
        {
            Character = character,
            User = character
        };

        if (!ServerGameManager.HasBuff(character, buffPrefab.ToIdentifier()))
        {
            DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
            return true;
        }

        return false;
    }
    public static bool TryApplyBuffWithOwner(Entity target, Entity familiar, PrefabGUID buffPrefab)
    {
        ApplyBuffDebugEvent applyBuffDebugEvent = new()
        {
            BuffPrefabGUID = buffPrefab,
            Who = target.Read<NetworkId>()
        };

        FromCharacter fromCharacter = new() // fam should be entityOwner
        {
            Character = familiar,
            User = familiar
        };

        if (!ServerGameManager.HasBuff(target, buffPrefab.ToIdentifier()))
        {
            DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
            return true;
        }

        return false;
    }
}
