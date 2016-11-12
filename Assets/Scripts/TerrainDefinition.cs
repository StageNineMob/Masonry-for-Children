using UnityEngine;
using System.Collections;

public class TerrainDefinition
{
    public enum TerrainProperty
    {
        NONE = 0,

        IMPASSABLE = 1, // only flying units can enter here
        SLOWS_WHEELED = 2, // wheeled units take extra movement to get here
        SLOWS_LAND = 4, // all ground units take extra movement to get here
        ROAD = 8, // ignore movement penalties moving to/from road

        DEFENSE_BONUS = 16,
        DEFENSE_PENALTY = 32,
        ATTACK_PENALTY = 64,

        LIMITS_DIRECT = 128,
        LIMITS_INDIRECT = 256,
    }

    public enum TerrainType
    {
        NONE = 0,
        GRASS = 1,
        ROCK = 2,
        TREE = 3,
        DESERT = 4,
        WASTELAND = 5,
        WATER = 6,
        RUINS = 7,
        MARSH = 8,
        SWAMP = 9,
        MOUNTAIN = 10,
        JUNGLE = 11,
        SNOW = 12
    }

    public static readonly TerrainDefinition GRASS_DEF = new TerrainDefinition(TerrainType.GRASS, TerrainProperty.NONE);
    public static readonly TerrainDefinition ROCK_DEF = new TerrainDefinition(TerrainType.ROCK, TerrainProperty.IMPASSABLE | TerrainProperty.LIMITS_DIRECT);
    public static readonly TerrainDefinition TREE_DEF = new TerrainDefinition(TerrainType.TREE, TerrainProperty.SLOWS_WHEELED | TerrainProperty.SLOWS_LAND | TerrainProperty.LIMITS_DIRECT | TerrainProperty.DEFENSE_BONUS);
    public static readonly TerrainDefinition DESERT_DEF = new TerrainDefinition(TerrainType.DESERT, TerrainProperty.SLOWS_LAND | TerrainProperty.DEFENSE_PENALTY | TerrainProperty.ATTACK_PENALTY);
    public static readonly TerrainDefinition WASTELAND_DEF = new TerrainDefinition(TerrainType.WASTELAND, TerrainProperty.DEFENSE_PENALTY);
    public static readonly TerrainDefinition WATER_DEF = new TerrainDefinition(TerrainType.WATER, TerrainProperty.IMPASSABLE);
    public static readonly TerrainDefinition RUINS_DEF = new TerrainDefinition(TerrainType.RUINS, TerrainProperty.SLOWS_LAND | TerrainProperty.DEFENSE_BONUS | TerrainProperty.LIMITS_DIRECT);
    public static readonly TerrainDefinition MARSH_DEF = new TerrainDefinition(TerrainType.MARSH, TerrainProperty.SLOWS_LAND | TerrainProperty.SLOWS_WHEELED | TerrainProperty.ATTACK_PENALTY);
    public static readonly TerrainDefinition SWAMP_DEF = new TerrainDefinition(TerrainType.SWAMP, TerrainProperty.IMPASSABLE | TerrainProperty.LIMITS_DIRECT);
    public static readonly TerrainDefinition MOUNTAIN_DEF = new TerrainDefinition(TerrainType.MOUNTAIN, TerrainProperty.IMPASSABLE | TerrainProperty.LIMITS_DIRECT | TerrainProperty.LIMITS_INDIRECT);
    public static readonly TerrainDefinition JUNGLE_DEF = new TerrainDefinition(TerrainType.JUNGLE, TerrainProperty.SLOWS_WHEELED | TerrainProperty.SLOWS_LAND | TerrainProperty.DEFENSE_BONUS | TerrainProperty.ATTACK_PENALTY | TerrainProperty.LIMITS_DIRECT | TerrainProperty.LIMITS_INDIRECT);
    public static readonly TerrainDefinition SNOW_DEF = new TerrainDefinition(TerrainType.SNOW, TerrainProperty.SLOWS_LAND | TerrainProperty.ATTACK_PENALTY);

    public TerrainType type;
    public TerrainProperty properties;

    public TerrainDefinition(TerrainType newType, TerrainProperty newProperties)
    {
        type = newType;
        properties = newProperties;
    }

    public TerrainDefinition(TerrainDefinition source)
    {
        type = source.type;
        properties = source.properties;
    }

    public bool HasTerrainProperty(TerrainProperty property)
    {
        return (property & properties) == property;
    }

    public static TerrainDefinition GetDefinition(TerrainType typeRequested)
    {
        switch (typeRequested)
        {
            case TerrainType.GRASS:
                return new TerrainDefinition(GRASS_DEF);
            case TerrainType.ROCK:
                return new TerrainDefinition(ROCK_DEF);
            case TerrainType.TREE:
                return new TerrainDefinition(TREE_DEF);
            case TerrainType.DESERT:
                return new TerrainDefinition(DESERT_DEF);
            case TerrainType.WASTELAND:
                return new TerrainDefinition(WASTELAND_DEF);
            case TerrainType.WATER:
                return new TerrainDefinition(WATER_DEF);
            case TerrainType.RUINS:
                return new TerrainDefinition(RUINS_DEF);
            case TerrainType.MARSH:
                return new TerrainDefinition(MARSH_DEF);
            case TerrainType.SWAMP:
                return new TerrainDefinition(SWAMP_DEF);
            case TerrainType.MOUNTAIN:
                return new TerrainDefinition(MOUNTAIN_DEF);
            case TerrainType.JUNGLE:
                return new TerrainDefinition(JUNGLE_DEF);
            case TerrainType.SNOW:
                return new TerrainDefinition(SNOW_DEF);
            default:
                Debug.LogError("[TerrainDefinition:GetDefinition] invalid type!");
                break;
        }
        return null;
    }
}
