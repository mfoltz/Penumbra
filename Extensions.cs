using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using System.Collections;
using System.Runtime.InteropServices;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Penumbra;
internal static class Extensions
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static DebugEventsSystem DebugEventsSystem => Core.DebugEventsSystem;
    static PrefabCollectionSystem PrefabCollectionSystem => Core.PrefabCollectionSystem;

    public delegate void WithRefHandler<T>(ref T item);
    public static void With<T>(this Entity entity, WithRefHandler<T> action) where T : struct
    {
        T item = entity.ReadRW<T>();
        action(ref item);

        EntityManager.SetComponentData(entity, item);
    }
    public static void AddWith<T>(this Entity entity, WithRefHandler<T> action) where T : struct
    {
        if (!entity.Has<T>())
        {
            entity.Add<T>();
        }

        entity.With(action);
    }
    public unsafe static void Write<T>(this Entity entity, T componentData) where T : struct
    {
        ComponentType componentType = new(Il2CppType.Of<T>());
        TypeIndex typeIndex = componentType.TypeIndex;

        byte[] byteArray = StructureToByteArray(componentData);
        int size = Marshal.SizeOf<T>();

        fixed (byte* byteData = byteArray)
        {
            EntityManager.SetComponentDataRaw(entity, typeIndex, byteData, size);
        }
    }
    static byte[] StructureToByteArray<T>(T structure) where T : struct
    {
        int size = Marshal.SizeOf(structure);
        byte[] byteArray = new byte[size];

        IntPtr ptr = Marshal.AllocHGlobal(size);
        Marshal.StructureToPtr(structure, ptr, true);

        Marshal.Copy(ptr, byteArray, 0, size);
        Marshal.FreeHGlobal(ptr);

        return byteArray;
    }
    unsafe static T ReadRW<T>(this Entity entity) where T : struct
    {
        ComponentType componentType = new(Il2CppType.Of<T>());
        TypeIndex typeIndex = componentType.TypeIndex;

        void* componentData = EntityManager.GetComponentDataRawRW(entity, typeIndex);
        return Marshal.PtrToStructure<T>(new IntPtr(componentData));
    }
    public unsafe static T Read<T>(this Entity entity) where T : struct
    {
        ComponentType componentType = new(Il2CppType.Of<T>());
        TypeIndex typeIndex = componentType.TypeIndex;

        void* componentData = EntityManager.GetComponentDataRawRO(entity, typeIndex);
        return Marshal.PtrToStructure<T>(new IntPtr(componentData));
    }
    public static DynamicBuffer<T> ReadBuffer<T>(this Entity entity) where T : struct
    {
        return EntityManager.GetBuffer<T>(entity);
    }
    public static bool TryGetComponent<T>(this Entity entity, out T componentData) where T : struct
    {
        componentData = default;

        if (entity.Has<T>())
        {
            componentData = entity.Read<T>();

            return true;
        }

        return false;
    }
    public static bool TryRemoveComponent<T>(this Entity entity) where T : struct
    {
        if (entity.Has<T>())
        {
            entity.Remove<T>();

            return true;
        }

        return false;
    }
    public static bool Has<T>(this Entity entity) where T : struct
    {
        return EntityManager.HasComponent(entity, new(Il2CppType.Of<T>()));
    }
    public static bool Has(this Entity entity, ComponentType componentType)
    {
        return EntityManager.HasComponent(entity, componentType);
    }
    public static string GetPrefabName(this PrefabGUID prefabGuid)
    {
        return PrefabCollectionSystem.PrefabGuidToNameDictionary.TryGetValue(prefabGuid, out string prefabName) ? $"{prefabName}" : "String.Empty";
    }
    public static void Add<T>(this Entity entity)
    {
        EntityManager.AddComponent(entity, new(Il2CppType.Of<T>()));
    }
    public static void Add(this Entity entity, ComponentType componentType)
    {
        EntityManager.AddComponent(entity, componentType);
    }
    public static void Remove<T>(this Entity entity)
    {
        EntityManager.RemoveComponent(entity, new(Il2CppType.Of<T>()));
    }
    public static bool IsTrader(this Entity entity)
    {
        if (entity.Has<Trader>())
        {
            return true;
        }

        return false;
    }
    public static bool IsMerchant(this Entity entity)
    {
        if (entity.Has<Trader>() && entity.Has<Immortal>() && entity.Has<Energy>())
        {
            return true;
        }

        return false;
    }
    public static bool Exists(this Entity entity)
    {
        return entity.HasValue() && EntityManager.Exists(entity);
    }
    public static bool HasValue(this Entity entity)
    {
        return entity != Entity.Null;
    }
    public static NetworkId GetNetworkId(this Entity entity)
    {
        if (entity.TryGetComponent(out NetworkId networkId))
        {
            return networkId;
        }

        return NetworkId.Empty;
    }
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (var item in source)
        {
            action(item);
        }
    }
    public static Entity GetUserEntity(this Entity character)
    {
        if (character.TryGetComponent(out PlayerCharacter playerCharacter)) return playerCharacter.UserEntity;

        return Entity.Null;
    }
    public static User GetUser(this Entity entity)
    {
        if (entity.TryGetComponent(out PlayerCharacter playerCharacter) && playerCharacter.UserEntity.TryGetComponent(out User user)) return user;
        else if (entity.TryGetComponent(out user)) return user;

        return User.Empty;
    }
    public static bool HasBuff(this Entity entity, PrefabGUID buffPrefabGuid)
    {
        return ServerGameManager.HasBuff(entity, buffPrefabGuid.ToIdentifier());
    }
    public static void Destroy(this Entity entity)
    {
        if (entity.Exists()) DestroyUtility.Destroy(EntityManager, entity);
    }
    public static void SetPosition(this Entity entity, float3 position)
    {
        if (entity.Has<AggroConsumer>())
        {
            entity.With((ref AggroConsumer aggroConsumer) =>
            {
                aggroConsumer.PreCombatPosition = position;
            });
        }

        if (entity.Has<SpawnTransform>())
        {
            entity.With((ref SpawnTransform spawnTransform) =>
            {
                spawnTransform.Position = position;
            });
        }

        if (entity.Has<Height>())
        {
            entity.With((ref Height height) =>
            {
                height.LastPosition = position;
            });
        }

        if (entity.Has<LocalTransform>())
        {
            entity.With((ref LocalTransform localTransform) =>
            {
                localTransform.Position = position;
            });
        }

        if (entity.Has<Translation>())
        {
            entity.With((ref Translation translation) =>
            {
                translation.Value = position;
            });
        }
        
        if (entity.Has<LastTranslation>())
        {
            entity.With((ref LastTranslation lastTranslation) =>
            {
                lastTranslation.Value = position;
            });
        }
    }
    public static void SetFaction(this Entity entity, PrefabGUID factionPrefabGUID)
    {
        if (entity.Has<FactionReference>())
        {
            entity.With((ref FactionReference factionReference) =>
            {
                factionReference.FactionGuid._Value = factionPrefabGUID;
            });
        }
    }
    public static void EnableAggroable(this Entity entity)
    {
        if (entity.Has<Aggroable>())
        {
            entity.With((ref Aggroable aggroable) =>
            {
                aggroable.Value._Value = true;
                aggroable.DistanceFactor._Value = 1f;
                aggroable.AggroFactor._Value = 1f;
            });
        }
    }
    public static void Start(this IEnumerator routine)
    {
        Core.StartCoroutine(routine);
    }
}