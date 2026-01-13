namespace Arch.Core;

public record struct InternalFastEntityAccessor(Array data)
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

public readonly struct FastEntityAccessorT<T, TMain> where TMain : IMainSingleArchetypeComponent
{
    public static FastEntityAccessorT<T> Value;
}

public readonly struct FastEntityAccessorT<T>
{
    public FastEntityAccessorT(InternalFastEntityAccessor data)
    {
        _typedData = data.data as T[];
    }

    public static FastEntityAccessorT<T> Value;
    public readonly T[] _typedData;

    public readonly ref T Get(int index)
    {
        return ref _typedData[index];
    }

    public readonly unsafe ref T Get(Entity entity)
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
    public static InternalFastEntityAccessor[] _cache = [];
    private static int _highestArchetypeId = 0;
    private static World _activeWorld = null!;

    public static void SetActiveWorld(World world)
    {
        _activeWorld = world;
    }

    public static void RefreshForWorld(World world)
    {
        Array.Clear(_cache);
        foreach (var archetype in world.Archetypes.Items)
        {
            RefreshForChunk(archetype);
        }
    }

    public static void RefreshForChunk(Archetype archetype)
    {
        var totalComponents = ComponentRegistry.Size + 1;
        _highestArchetypeId = Math.Max(_highestArchetypeId, archetype.id + 1);
        var totalSize = _highestArchetypeId * totalComponents;
        if (_cache == null! || _cache.Length < totalSize)
        {
            var newCache = new InternalFastEntityAccessor[totalSize];
            if (_cache != null!)
            {
                Array.Copy(_cache, newCache, _cache.Length);
            }

            _cache = newCache;
        }

        foreach (var item in archetype.Signature.Components)
        {
            var dataForId = archetype.chunk.GetArray(item);
            var accessor = new InternalFastEntityAccessor(dataForId);
            if (typeof(ISingleArchetypeComponent).IsAssignableFrom(item.Type))
            {
                // set FastEntityAccessorT<T>.Value
                var fastEntityAccessorType = typeof(FastEntityAccessorT<>).MakeGenericType(item.Type);
                var field = fastEntityAccessorType.GetField("Value", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                var fastEntityAccessorInstance = Activator.CreateInstance(fastEntityAccessorType, accessor);
                field!.SetValue(null, fastEntityAccessorInstance);
            }

            if (typeof(IMainSingleArchetypeComponent).IsAssignableFrom(item.Type))
            {
                foreach (var innerItem in archetype.Signature.Components)
                {
                    if (!typeof(ISingleArchetypeComponent).IsAssignableFrom(innerItem.Type))
                    {
                        // Field lives on FastEntityAccessorT<TInner, TMain> and its type is FastEntityAccessorT<TInner>
                        var holderType = typeof(FastEntityAccessorT<,>).MakeGenericType(innerItem.Type, item.Type);
                        var field = holderType.GetField("Value", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

                        // Create FastEntityAccessorT<TInner>(InternalFastEntityAccessor)
                        var accessorType = typeof(FastEntityAccessorT<>).MakeGenericType(innerItem.Type);
                        var dataForInnerId = archetype.chunk.GetArray(innerItem);
                        var innerAccessor = new InternalFastEntityAccessor(dataForInnerId);
                        var accessorInstance = Activator.CreateInstance(accessorType, innerAccessor);

                        field!.SetValue(null, accessorInstance);
                    }
                }
            }

            SetCacheItem(archetype.id, item.Id, accessor);
        }
    }

    private static int GetIndex(int archetypeId, int componentId) => archetypeId * ComponentRegistry.Size + componentId;

    private static void SetCacheItem(int archetypeId, int componentId, InternalFastEntityAccessor accessor)
    {
        if (accessor.data == null!)
        {
            throw new InvalidOperationException("Cannot cache a null data array.");
        }

        _cache[GetIndex(archetypeId, componentId)] = accessor;
    }

    internal static InternalFastEntityAccessor GetCacheItem(int archetypeId, int componentId)
    {
        return _cache[GetIndex(archetypeId, componentId)];
    }
}
