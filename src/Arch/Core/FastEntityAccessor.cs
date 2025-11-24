namespace Arch.Core;

internal record struct InternalFastEntityAccessor(Array data)
{
    public readonly ref T Get<T>(int index)
    {
        var typedData = Unsafe.As<T[]>(data);
        return ref typedData[index];
    }

    public readonly ref T Get<T>(Entity entity)
    {
        var typedData = Unsafe.As<T[]>(data);
        return ref typedData[entity.SlotIndex];
    }
}

public struct FastEntityAccessorT<T>
{
    internal FastEntityAccessorT(InternalFastEntityAccessor data)
    {
        _typedData = data.data as T[];
    }

    private T[] _typedData;

    public readonly ref T Get(int index)
    {
        return ref _typedData[index];
    }

    public readonly ref T Get(Entity entity)
    {
        return ref _typedData[entity.SlotIndex];
    }

    public readonly ref T Get(FastEntity entity)
    {
        return ref _typedData[entity.SlotIndex];
    }
}

public static class FastEntityAccessorCache
{
    internal static InternalFastEntityAccessor[] _cache;
    private static int highestArchetypeId = 0;

    public static void RefreshForWorld(World world)
    {
        foreach (var archetype in world.Archetypes.Items)
        {
            RefreshForChunk(archetype);
        }
    }

    public static void RefreshForChunk(Archetype archetype)
    {
        var totalComponents = ComponentRegistry.Size + 1;
        highestArchetypeId = Math.Max(highestArchetypeId, archetype.id + 1);
        var totalSize = highestArchetypeId * totalComponents;
        if (_cache == null! || _cache.Length < totalSize)
        {
            var newCache = new InternalFastEntityAccessor[totalSize];
            if (_cache != null!)
            {
                Array.Copy(_cache, newCache, _cache.Length);
            }
            _cache = newCache;
        }

        foreach (var type in archetype.Signature.Components)
        {
            var dataForId = archetype.chunk.GetArray(type);
            var accessor = new InternalFastEntityAccessor(dataForId);
            SetCacheItem(archetype.id, type.Id, accessor);
        }
    }

    private static void SetCacheItem(int archetypeId, int componentId, InternalFastEntityAccessor accessor)
    {
        if (accessor.data == null!)
        {
            throw new InvalidOperationException("Cannot cache a null data array.");
        }

        var index = archetypeId * ComponentRegistry.Size + componentId;
        _cache[index] = accessor;
    }

    internal static InternalFastEntityAccessor GetCacheItem(int archetypeId, int componentId)
    {
        var index = archetypeId * ComponentRegistry.Size + componentId;
        return _cache[index];
    }
}
