#if !PURE_ECS
using Arch.Core.Extensions;
using Arch.Core.Utils;
#endif

namespace Arch.Core;

#if PURE_ECS
/// <summary>
///     The <see cref="Entity"/> struct
///     represents a general-purpose object and can be assigned a set of components that act as data.
/// </summary>
[SkipLocalsInit]
public readonly struct Entity : IEquatable<Entity>, IComparable<Entity>
{
    /// <summary>
    ///     Its Id, unique in its <see cref="World"/>.
    /// </summary>
    public readonly int Id = -1;

    /// <summary>
    ///     The version of an entity.
    /// </summary>
    public readonly int Version;

    /// <summary>
    ///     A null entity, used for comparison.
    /// </summary>
    public static readonly Entity Null = new(-1, 0, -1);

    /// <summary>
    ///     Initializes a new instance of the <see cref="Entity"/> struct.
    /// </summary>
    /// <param name="id">Its unique id.</param>
    /// <param name="worldId">Its world id, not used for this entity since its pure ecs.</param>
    /// <param name="version">Its version.</param>
    internal Entity(int id, int worldId)
    {
        Id = id;
        Version = 1;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Entity"/> struct.
    /// </summary>
    /// <param name="id">Its unique id.</param>
    /// <param name="worldId">Its world id, not used for this entity since its pure ecs.</param>
    /// <param name="version">Its version.</param>
    internal Entity(int id, int worldId, int version)
    {
        Id = id;
        Version = version;
    }

    /// <summary>
    ///     Checks the <see cref="Entity"/> for equality with another one.
    /// </summary>
    /// <param name="other">The other <see cref="Entity"/>.</param>
    /// <returns>True if equal, false if not.</returns>
    public bool Equals(Entity other)
    {
        return ((Id ^ other.Id) | (Version ^ other.Version)) == 0;
    }

    /// <summary>
    ///     Checks the <see cref="Entity"/> for equality with another object..
    /// </summary>
    /// <param name="obj">The other <see cref="Entity"/> object.</param>
    /// <returns>True if equal, false if not.</returns>
    public override bool Equals(object? obj)
    {
        return obj is Entity other && Equals(other);
    }

    /// <summary>
    ///     Compares this <see cref="Entity"/> instace to another one for sorting and ordering.
    ///     <remarks>Orders them by id. Ascending.</remarks>
    /// </summary>
    /// <param name="other">The other <see cref="Entity"/>.</param>
    /// <returns>A int indicating their order.</returns>
    public int CompareTo(Entity other)
    {
        return (Version.CompareTo(other.Version) << 8) | Id.CompareTo(other.Id);
    }

    /// <summary>
    ///     Calculates the hash of this <see cref="Entity"/>.
    /// </summary>
    /// <returns>Its hash.</returns>

    public override int GetHashCode()
    {
        unchecked
        {
            // Overflow is fine, just wrap
            var hash = 17;
            hash = hash * 23 + Id;
            hash = hash * 23 + Version;
            return hash;
        }
    }

    /// <summary>
    ///     Checks the left <see cref="Entity"/> for equality with the right one.
    /// </summary>
    /// <param name="left">The left <see cref="Entity"/>.</param>
    /// <param name="right">The right <see cref="Entity"/>.</param>
    /// <returns>True if both are equal, otherwise false.</returns>

    public static bool operator ==(Entity left, Entity right)
    {
        return left.Equals(right);
    }

    /// <summary>
    ///     Checks the left <see cref="Entity"/> for unequality with the right one.
    /// </summary>
    /// <param name="left">The left <see cref="Entity"/>.</param>
    /// <param name="right">The right <see cref="Entity"/>.</param>
    /// <returns>True if both are unequal, otherwise false.</returns>

    public static bool operator !=(Entity left, Entity right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    ///     Converts this entity to a string.
    /// </summary>
    /// <returns>A string.</returns>
    public override string ToString()
    {
        return $"{nameof(Id)}: {Id}";
    }
}
#else

public static class EcsBackingData
{
    static EcsBackingData()
    {
        var maxEntities = 10_000_000;
        ArchetypeId = new int[maxEntities];
        WorldId = new int[maxEntities];
        SlotIndex = new int[maxEntities];
        IsAlive = new bool[maxEntities];
        Version = new int[maxEntities];
        EntityData = new EntityData[maxEntities];
        for (int i = maxEntities - 1; i >= 0; i--)
        {
            FreeIds.Push(i);
        }
    }

    public static int[] ArchetypeId;
    public static int[] WorldId;
    public static bool[] IsAlive;
    public static int[] Version;
    public static int[] SlotIndex;
    public static EntityData[] EntityData;
    public static Stack<int> FreeIds = new();

    public static Entity CreateNewEntity(int worldId)
    {
        var result = FreeIds.Pop();
        IsAlive[result] = true;
        Version[result]++;
        WorldId[result] = worldId;
        return new(result, WorldId[result], Version[result]);
    }

    public static void DestroyEntity(Entity entity)
    {
        var id = entity.Id;
        FreeIds.Push(id);
        IsAlive[id] = false;
    }
}

public record struct FastEntity(int Id, int SlotIndex)
{
    public ref int ArchetypeId
    {
        get => ref EcsBackingData.ArchetypeId[Id];
    }

    public ref int WorldId
    {
        get => ref EcsBackingData.WorldId[Id];
    }

    public ref bool IsAlive
    {
        get => ref EcsBackingData.IsAlive[Id];
    }
}

/// <summary>
///     The <see cref="Entity"/> struct
///     represents a general-purpose object and can be assigned a set of components that act as data.
/// </summary>
[DebuggerTypeProxy(typeof(EntityDebugView))]
[SkipLocalsInit]
public readonly struct Entity : IEquatable<Entity>, IComparable<Entity>
{
    /// <summary>
    ///      It's Id, unique in its <see cref="World"/>.
    /// </summary>
    public readonly int Id;

    /// <summary>
    ///     The version of an entity.
    /// </summary>
    public readonly int Version;

    public FastEntity Fast
    {
        get => new(Id, SlotIndex);
    }

    public ref int StoredVersion
    {
        get => ref EcsBackingData.Version[Id];
    }

    public ref bool IsAlive
    {
        get => ref EcsBackingData.IsAlive[Id];
    }

    /// <summary>
    /// Its <see cref="World"/> id.
    /// </summary>
    public ref int WorldId
    {
        get => ref EcsBackingData.WorldId[Id];
    }

    public ref int ArchetypeId
    {
        get => ref EcsBackingData.ArchetypeId[Id];
    }

    public int SlotIndex
    {
        get => EcsBackingData.SlotIndex[Id];
    }

    public Archetype Archetype
    {
        get => Archetype.GetById(ArchetypeId);
        set => ArchetypeId = value.id;
    }

    /// <summary>
    ///     A null <see cref="Entity"/> used for comparison.
    /// </summary>
    public readonly static Entity Null = new(-1, 0, -1);

    /// <summary>
    ///     Initializes a new instance of the <see cref="Entity"/> struct with default values.
    /// </summary>
    public Entity()
    {
        Id = -1;
        Version = -1;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Entity"/> struct.
    /// </summary>
    /// <param name="id">Its unique id.</param>
    /// <param name="worldId">Its <see cref="World"/> id.</param>
    /// <param name="version">Its version.</param>
    internal Entity(int id, int worldId)
    {
        Id = id;
        WorldId = worldId;
        Version = 1;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Entity"/> struct.
    /// </summary>
    /// <param name="id">Its unique id.</param>
    /// <param name="worldId">Its <see cref="World"/> id.</param>
    /// <param name="version">Its version.</param>
    public Entity(int id, int worldId, int version)
    {
        Id = id;
        if (id >= 0)
        {
            WorldId = worldId;
            Version = version;
        }
    }

    /// <summary>
    ///     Checks the <see cref="Entity"/> for equality with another one.
    /// </summary>
    /// <param name="other">The other <see cref="Entity"/>.</param>
    /// <returns>True if equal, false if not.</returns>
    public bool Equals(Entity other)
    {
        if (Id == -1 && other.Id == -1)
        {
            return true;
        }

        if (Id == -1 || other.Id == -1)
        {
            return false;
        }

        return ((Id ^ other.Id) | (WorldId ^ other.WorldId) | (Version ^ other.Version)) == 0;
    }

    /// <summary>
    ///     Checks the <see cref="Entity"/> for equality with another <see cref="object"/>.
    /// </summary>
    /// <param name="obj">The other <see cref="Entity"/> object.</param>
    /// <returns>True if equal, false if not.</returns>
    public override bool Equals(object? obj)
    {
        return obj is Entity other && Equals(other);
    }

    /// <summary>
    ///     Compares this <see cref="Entity"/> instace to another one for sorting and ordering.
    ///     <remarks>Orders them by id and world. Ascending.</remarks>
    /// </summary>
    /// <param name="other">The other <see cref="Entity"/>.</param>
    /// <returns>A int indicating their order.</returns>
    public int CompareTo(Entity other)
    {
        return (WorldId.CompareTo(other.WorldId) << 16) | (Version.CompareTo(other.Version) << 8) | Id.CompareTo(other.Id);
    }

    /// <summary>
    ///     Calculates the hash of this <see cref="Entity"/>.
    /// </summary>
    /// <returns>Its hash.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            // Overflow is fine, just wrap
            var hash = 17;
            hash = (hash * 23) + Id;
            hash = (hash * 23) + WorldId;
            hash = (hash * 23) + Version;
            return hash;
        }
    }

    /// <summary>
    ///      Checks the <see cref="Entity"/> for equality with another one.
    /// </summary>
    /// <param name="left">The left <see cref="Entity"/>.</param>
    /// <param name="right">The right <see cref="Entity"/>.</param>
    /// <returns>True if equal, otherwise false.</returns>
    public static bool operator ==(Entity left, Entity right)
    {
        return left.Equals(right);
    }

    /// <summary>
    ///      Checks the <see cref="Entity"/> for inequality with another one.
    /// </summary>
    /// <param name="left">The left <see cref="Entity"/>.</param>
    /// <param name="right">The right <see cref="Entity"/>.</param>
    /// <returns>True if unequal, otherwise false.</returns>
    public static bool operator !=(Entity left, Entity right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    ///     Converts this <see cref="Entity"/> to a string.
    /// </summary>
    /// <returns>Its string.</returns>
    public override string ToString()
    {
        if(this == Null)
        {
            return "Entity = Null";
        }

        return $"Entity = {{ {nameof(Id)} = {Id}, {nameof(WorldId)} = {WorldId}, {nameof(Version)} = {Version} }}";
    }
}
#endif
