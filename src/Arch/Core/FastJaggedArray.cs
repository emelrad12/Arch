namespace Arch.Core;

/// <summary>
/// Fixed-capacity flat replacement for JaggedArray{T}.
/// </summary>
/// <typeparam name="T"></typeparam>
public class EntityDataArrayProxy
{
    private readonly EntityData _filler;
    public int Capacity
    {
        get => EcsBackingData.SlotIndex.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityDataArrayProxy(EntityData filler)
    {
        _filler = filler;
        Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(int index, in EntityData item)
    {
        EcsBackingData.ArchetypeId[index] = item.archetypeId;
        EcsBackingData.SlotIndex[index] = item.Slot.Index;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Remove(int index)
    {
        EcsBackingData.ArchetypeId[index] = -1;
        EcsBackingData.SlotIndex[index] = -1;
    }

    private EntityData CreateEntityData(int index)
    {
        return new(Archetype.GetById(EcsBackingData.ArchetypeId[index]), new(EcsBackingData.SlotIndex[index], 0), EcsBackingData.Version[index], index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(int index, out EntityData value)
    {
        if (index < 0 || index >= Capacity)
        {
            value = _filler;
            return false;
        }

        var result = CreateEntityData(index);
        if (EqualityComparer<EntityData>.Default.Equals(result, _filler))
        {
            value = _filler;
            return false;
        }

        value = result;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityData TryGetValue(int index, out bool exists)
    {
        if (index < 0 || index >= Capacity)
        {
            exists = false;
            return new();
        }

        var result = CreateEntityData(index);
        if (EqualityComparer<EntityData>.Default.Equals(result, _filler))
        {
            exists = false;
            return new();
        }

        exists = true;
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsKey(int index)
    {
        if (index < 0 || index >= Capacity) return false;
        var result =  CreateEntityData(index);
        return !EqualityComparer<EntityData>.Default.Equals(result, _filler);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EnsureCapacity(int newCapacity)
    {
        // Fixed capacity - no-op
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void TrimExcess()
    {
        // Fixed capacity - no-op
    }

    public ref EntityData this[int i]
    {
        get
        {
            Debug.Assert(i >= 0 && i < Capacity);
            return ref EcsBackingData.EntityData[i];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
    }
}
