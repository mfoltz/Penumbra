using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Entities;

namespace Penumbra;
internal static class Buffs
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static DebugEventsSystem DebugEventsSystem => Core.DebugEventsSystem;
    public static bool TryApplyBuff(this Entity entity, PrefabGUID buffPrefabGuid)
    {
        if (!entity.HasBuff(buffPrefabGuid))
        {
            ApplyBuffDebugEvent applyBuffDebugEvent = new()
            {
                BuffPrefabGUID = buffPrefabGuid
            };

            FromCharacter fromCharacter = new()
            {
                Character = entity,
                User = entity
            };

            DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);

            return true;
        }

        return false;
    }
    public static bool TryGetBuff(this Entity entity, PrefabGUID buffPrefabGUID, out Entity buffEntity)
    {
        if (ServerGameManager.TryGetBuff(entity, buffPrefabGUID.ToIdentifier(), out buffEntity))
        {
            return true;
        }

        return false;
    }
    public static bool TryRemoveBuff(this Entity entity, PrefabGUID buffPrefabGuid)
    {
        if (entity.TryGetBuff(buffPrefabGuid, out Entity buffEntity))
        {
            DestroyUtility.Destroy(EntityManager, buffEntity, DestroyDebugReason.TryRemoveBuff);

            return true;
        }

        return false;
    }
    public static bool TryApplyAndGetBuff(this Entity entity, PrefabGUID buffPrefabGuid, out Entity buffEntity)
    {
        buffEntity = Entity.Null;

        if (entity.TryApplyBuff(buffPrefabGuid) && entity.TryGetBuff(buffPrefabGuid, out buffEntity))
        {
            return true;
        }

        return false;
    }
}
