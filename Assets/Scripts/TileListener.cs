using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using StageNine;
using System;

public class TileListener : MonoBehaviour
{
    public List<TileEdge> neighbors;
    public List<TileModifier> modifiers;
    public TerrainDefinition.TerrainType terrainType;
    public TerrainDefinition terrain;
    public CombatManager.Faction deployable = CombatManager.Faction.NONE;
    public Sprite animaticBackground;

    public SpriteRenderer terrainSprite;

    public bool previewMap;

    private IntVector2 _gridPos;
    private GameObject _occupant;
    private Color defaultColor, moveHighlightColor, attackHighlightColor;
    private Dictionary<IntVector2, GameObject> streetSigns;

    //public TerrainDefinition terrain
    //{
    //    get
    //    {
    //        return TerrainDefinition.GetDefinition(terrainType);
    //    }
    //}


    public GameObject occupant
    {
        get
        {
            return _occupant;
        }
        set
        {
            _occupant = value;
        }
    }

    public bool occupied
    {
        get
        {
            return _occupant != null;
        }
    }

    public IntVector2 gridPos
    {
        get
        {
            return _gridPos;
        }
    }

    #region public methods

    public void OurInit()
    {
        CalculateHighlightColors();
        _occupant = null;
        _gridPos = null;
        modifiers = new List<TileModifier>();
        streetSigns = new Dictionary<IntVector2, GameObject>();
        terrain = TerrainDefinition.GetDefinition(terrainType);
    }
    public void RestoreDefaultColor()
    {
        GetComponent<SpriteRenderer>().color = defaultColor;
    }
    public void HighlightMove()
    {
        GetComponent<SpriteRenderer>().color = moveHighlightColor;
    }
    public void HighlightAttack()
    {
        GetComponent<SpriteRenderer>().color = attackHighlightColor;
    }
    public void HighlightPartialAttack(float highlightAmount)
    {
        GetComponent<SpriteRenderer>().color = attackHighlightColor * highlightAmount + defaultColor * (1f - highlightAmount);
    }
    public void SetGridPosition(int xx, int yy)
    {
        _gridPos = new IntVector2(xx, yy);
    }
    public void SetGridPosition(IntVector2 newPos)
    {
        _gridPos = new IntVector2(newPos);
    }

    public bool IsOccupiedByEnemy(CombatManager.Faction myFaction)
    {
        // If myFaction is NONE, don't check for enemies
        if (myFaction == CombatManager.Faction.NONE)
        {
            return false;
        }
        if (occupied)
        {
            return _occupant.GetComponent<UnitHandler>().faction != myFaction;
        }
        else
        {
            return false;
        }
    }

    public Dictionary<UnitHandler.PathingType, int> GetMovementCost(GameObject neighbor)
    {
        Dictionary<UnitHandler.PathingType, int> output = new Dictionary<UnitHandler.PathingType, int>();
        foreach (UnitHandler.PathingType moveType in Enum.GetValues(typeof(UnitHandler.PathingType)))
        {
            output.Add(moveType, 1);
        }

        var neighborTile = neighbor.GetComponent<TileListener>();

        if (neighborTile.HasTerrainProperty(TerrainDefinition.TerrainProperty.LIMITS_DIRECT))
        {
            output[UnitHandler.PathingType.RANGED_DIRECT] += 1;
        }
        if (neighborTile.HasTerrainProperty(TerrainDefinition.TerrainProperty.LIMITS_INDIRECT))
        {
            output[UnitHandler.PathingType.RANGED_INDIRECT] += 1;
        }

        if (HasTerrainProperty(TerrainDefinition.TerrainProperty.IMPASSABLE))
        {
            output[UnitHandler.PathingType.FOOT] += 999; //TODO: replace magic numbers
            output[UnitHandler.PathingType.WHEEL] += 999;
        }
        else
        {
            if (neighborTile.HasTerrainProperty(TerrainDefinition.TerrainProperty.IMPASSABLE))
            {
                output[UnitHandler.PathingType.FOOT] += 999; //TODO: replace magic numbers
                output[UnitHandler.PathingType.WHEEL] += 999;
            }
            if (neighborTile.HasTerrainProperty(TerrainDefinition.TerrainProperty.SLOWS_WHEELED))
            {
                output[UnitHandler.PathingType.WHEEL] += 1;
            }
            if (neighborTile.HasTerrainProperty(TerrainDefinition.TerrainProperty.SLOWS_LAND))
            {
                output[UnitHandler.PathingType.FOOT] += 1;
                output[UnitHandler.PathingType.WHEEL] += 1;
            }
        }

        if (neighborTile.HasTerrainProperty(TerrainDefinition.TerrainProperty.ROAD) && HasTerrainProperty(TerrainDefinition.TerrainProperty.ROAD))
        {
            output[UnitHandler.PathingType.FOOT] = 1;
            output[UnitHandler.PathingType.WHEEL] = 1;
        }
        return output;
    }

    public void GetNeighbors()
    {
        neighbors = new List<TileEdge>();
        foreach (GameObject tile in MapManager.singleton.AdjacentTiles(_gridPos, previewMap))
        {
            neighbors.Add(new TileEdge(tile, GetMovementCost(tile)));
        }
    }

    public void PropertiesChanged()
    {
        CalculateHighlightColors();
        RecalculateMovementCosts();
    }

    public TileEdge GetEdgeTo(GameObject tile)
    {
        foreach (TileEdge edge in neighbors)
        {
            if (edge.tile == tile)
                return edge;
        }
        return null;
    }

    public bool HasTerrainProperty(TerrainDefinition.TerrainProperty property)
    {
        return terrain.HasTerrainProperty(property);
    }

    public void SetRoadProperty(bool isRoad)
    {
        if(HasTerrainProperty(TerrainDefinition.TerrainProperty.ROAD) != isRoad)
        {
            Debug.Log("[TileListener:SetRoadProperty] Setting ROAD to " + isRoad + ".");
            terrain.properties ^= TerrainDefinition.TerrainProperty.ROAD;
            if(isRoad)
            {
                var indicator = Instantiate(MapManager.singleton.roadIndicatorPrefab) as GameObject;
                indicator.transform.SetParent(transform);
                indicator.transform.localPosition = Vector3.zero;
                streetSigns.Add(IntVector2.ZERO, indicator);
                // add streetSigns for adjacent road tiles
                foreach (GameObject tile in MapManager.singleton.AdjacentTiles(_gridPos,previewMap))
                {
                    var tL = tile.GetComponent<TileListener>();
                    if (tL.HasTerrainProperty(TerrainDefinition.TerrainProperty.ROAD))
                    {
                        var direction = tL.gridPos - gridPos;
                        AddRoadSegment(direction);
                        tL.AddRoadSegment(-direction);
                    }
                }
            }
            else
            {
                foreach(var pair in streetSigns)
                {
                    Destroy(pair.Value);
                    if(pair.Key != IntVector2.ZERO)
                    {
                        // remove matching streetSign from adjacent tile
                        var tL = MapManager.singleton.GetTileAt(gridPos + pair.Key, previewMap).GetComponent<TileListener>();
                        tL.DeleteRoadSegment(-(pair.Key));
                    }
                }
                streetSigns.Clear();
            }
        }
    }
     
    public void AddRoadSegment(IntVector2 direction)
    {
        if (streetSigns.ContainsKey(direction))
        {
            Debug.LogError("[TileListener:AddRoadSegment] Already a RoadSegment in " + direction);
        }
        else
        {
            var segment = Instantiate(MapManager.singleton.roadSegmentPrefab) as GameObject;
            segment.transform.SetParent(transform);
            segment.transform.localPosition = Vector3.zero;
            Quaternion angle = Quaternion.FromToRotation(Vector3.right, MapManager.singleton.GetTileAt(gridPos + direction, previewMap).transform.position - transform.position);
            segment.transform.localRotation = angle;
            streetSigns.Add(direction, segment);
        }
    }

    public void DeleteRoadSegment(IntVector2 direction)
    {
        if (streetSigns.ContainsKey(direction))
        {
            Destroy(streetSigns[direction]);
            streetSigns.Remove(direction);
        }
        else
        {
            Debug.LogError("[TileListener:AddRoadSegment] Already a RoadSegment in " + direction);
        }
    }

    public void AddModifier(TileModifier newMod)
    {
        if (modifiers != null)
        {
            if (!modifiers.Contains(newMod))
            {
                modifiers.Add(newMod);
            }
        }
    }

    public void RemoveModifier(TileModifier modToRemove)
    {
        modifiers.Remove(modToRemove);
    }

    public TileModifier.PathingType GetPathingType(GameObject unit, bool isAttack)
    {

        if (modifiers.Count == 0)
        {
            return TileModifier.PathingType.CLEAR;
        }
        var worstModType = TileModifier.PathingType.CLEAR;
        foreach (var mod in modifiers)
        {
            if (mod.source.WillUnitTrigger(unit, isAttack) && (int)mod.pathingType > (int)worstModType)
            {
                worstModType = mod.pathingType;
            }
            if (worstModType == TileModifier.PathingType.IMPASSABLE)
            {
                break;
            }
        }
        Debug.Log("[TileListener:GetPathingType] " + worstModType.ToString());
        return worstModType;
    }

    public bool TileEnter(GameObject enteringUnit)
    {
        int size = modifiers.Count;
        bool triggeredMod = false;
        if (size == 0)
        {
            return false;
        }
        TileModifier[] tempMods = new TileModifier[size];
        modifiers.CopyTo(tempMods);
        foreach (var mod in tempMods)
        {
            if (mod.source.WillUnitTrigger(enteringUnit, false))
            {
                triggeredMod = true;
                mod.source.TileEnter(mod, enteringUnit);
            }
        }
        return triggeredMod;
    }

    public bool CheckForMods(GameObject enteringUnit, bool isAttack)
    {

        if (modifiers.Count == 0)
        {
            return false;
        }

        foreach (var mod in modifiers)
        {
            if (mod.source.WillUnitTrigger(enteringUnit, isAttack))
            {
                return true;
            }
        }
        return false;
    }

    public void TileClickedCombat()
    {
        CombatManager.singleton.HideInformationPopup();
        CombatManager.singleton.TargetReticleOff();

        //TODO: this check only matters in a battle or pre-battle setup
        if (occupied)
        {
            _occupant.GetComponent<UnitHandler>().RespondToClick();
        }
        else
        {
            print("Tile at " + _gridPos + " clicked:  " + gameObject.transform.position.ToString());

            switch (CombatManager.singleton.GetGameState())
            {
                case CombatManager.GameState.MOVING:
                    CombatManager.singleton.selectedUnit.GetComponent<UnitHandler>().MoveTo(_gridPos);
                    break;
                case CombatManager.GameState.INSPECTING:
                    CombatManager.singleton.SelectGameState(CombatManager.GameState.IDLE, null);
                    break;
                case CombatManager.GameState.SPECIAL:
                case CombatManager.GameState.CONFIRMING:
                    CombatManager.singleton.curAbility.TileClick(gameObject, CombatManager.singleton.GetGameState());
                    break;
                default:
                    if (CombatManager.singleton.selectedUnit != null)
                    {
                        CombatManager.singleton.selectedUnit.GetComponent<UnitHandler>().ClearTarget();
                    }
                    break;
            }
        }
    }

    public void DeleteTile()
    {
        // if you have an occupant delete that occupant
        if(occupied)
        {
            CombatManager.singleton.DeleteUnit(_occupant);
        }
        SetRoadProperty(false);
        // if you have neighbors, tell them to remove you from the list
        if(neighbors != null && neighbors.Count > 0)
        {
            foreach(var edge in neighbors)
            {
                edge.tile.GetComponent<TileListener>().RemoveNeighbor(gameObject);
            }
        }
        // clean up modifiers
        if(modifiers != null)
        {
            while(modifiers.Count > 0)
            {
                modifiers[0].Cleanup();
            }
        }
    }

    public List<GameObject> GetTargetableTilesFromTile(int targetRange, UnitHandler.AttackType targetType)
    {
        // Two lists of pairs of distances and tiles
        //  unsearchedTiles:    Tiles discovered by the algorithm, but not checked for neighbors
        LinkedList<MapManager.PathfindingNode> unsearchedTiles = new LinkedList<MapManager.PathfindingNode>();

        // Start by adding our current location to unsearchedTiles

        unsearchedTiles.AddFirst(new MapManager.PathfindingNode(0, gameObject));

        LinkedList<MapManager.PathfindingNode> doneTiles;
        MapManager.PathingInternal(out doneTiles, unsearchedTiles, targetRange, (UnitHandler.PathingType)targetType, CombatManager.Faction.NONE, gameObject);

        var attackableTiles = new List<GameObject>();
        foreach (var pathNode in doneTiles)
        {
            attackableTiles.Add(pathNode.destinationTile);
        }

        return attackableTiles;
    }


    //private methods

    private void RemoveNeighbor(GameObject tile)
    {
        if(neighbors != null)
        {
            foreach(var edge in neighbors)
            {
                if(edge.tile == tile)
                {
                    neighbors.Remove(edge);
                    break;
                }
            }
        }
    }

    #endregion

    #region private methods

    private void CalculateHighlightColors()
    {
        defaultColor = GetComponent<SpriteRenderer>().color;
        moveHighlightColor = new Color(0.5f * 0.75f + defaultColor.r * 0.25f, 0.75f * 0.75f + defaultColor.g * 0.25f, 1.0f * 0.75f + defaultColor.b * 0.25f);
        attackHighlightColor = new Color(1.0f * 0.75f + defaultColor.r * 0.25f, 0.5f * 0.75f + defaultColor.g * 0.25f, 0.5f * 0.75f + defaultColor.b * 0.25f);
    }

    private void RecalculateMovementCosts()
    {
        if(neighbors != null)
        {
            foreach (TileEdge edge in neighbors)
            {
                edge.cost = GetMovementCost(edge.tile);
                TileEdge reverseEdge = edge.tile.GetComponent<TileListener>().GetEdgeTo(gameObject);
                reverseEdge.cost = edge.tile.GetComponent<TileListener>().GetMovementCost(gameObject);
            }
        }
        else
        {
            // TODO: Make this error appear in modes where it should
            // Debug.LogError("[TileListener:RecalculateMovementCosts] Tile doesn't have neighbors?!");
        }
    }
    #endregion

    #region monobehaviors
    #endregion

}

[Serializable]
public class SerializableTile
{
    public int x;
    public int y;
    public TerrainDefinition.TerrainType type;
    public CombatManager.Faction deployable;
    public bool isRoad;

    public override string ToString()
    {
        return "(" + x + ", " + y + "), " + type;
    }
}