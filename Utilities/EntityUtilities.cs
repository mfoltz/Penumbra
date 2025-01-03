using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Penumbra.Utilities;
internal static class EntityUtilities
{
    static EntityManager EntityManager => Core.EntityManager;

    public static IEnumerable<Entity> GetEntitiesEnumerable(EntityQuery entityQuery) // not sure if need to actually check for empty buff buffer for quest targets but don't really want to find out
    {
        JobHandle handle = GetEntities(entityQuery, out NativeArray<Entity> entities, Allocator.TempJob);
        handle.Complete();
        try
        {
            foreach (Entity entity in entities)
            {
                if (EntityManager.Exists(entity))
                {
                    yield return entity;
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
    static JobHandle GetEntities(EntityQuery entityQuery, out NativeArray<Entity> entities, Allocator allocator = Allocator.TempJob)
    {
        entities = entityQuery.ToEntityArray(allocator);
        return default;
    }
}
