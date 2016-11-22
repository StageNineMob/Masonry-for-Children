using UnityEngine;
using System;
using System.Collections.Generic;
using StageNine;
using StageNine.Events;
using System.Collections;
using UnityEngine.SceneManagement;

public class MapManager : MonoBehaviour {

    //enums
    public enum SymmetrySetting
    {
        NONE = 0,
        ROTATIONAL = 1,
        HORIZONTAL = 2
    }

    public enum MapContext
    {
        COMBAT,
        MAP_EDITOR,
        MAIN_MENU
    }

    public enum BrushType
    {
        NONE,
        ERASER,
        GRASS,
        ROCK,
        TREE,
        DESERT,
        WASTELAND,
        WATER,
        RUINS,
        MARSH,
        SWAMP,
        MOUNTAIN,
        JUNGLE,
        SNOW,
        RED_ZONE,
        BLUE_ZONE,
        CLEAR_ZONE,
        PAVE,
        UNPAVE
    }

    //subclasses
    public class PathfindingNode
    {
        int _distance;
        GameObject _destinationTile;
        LinkedList<GameObject> _path;

        public int distance
        {
            get { return _distance; }
        }

        public GameObject destinationTile
        {
            get { return _destinationTile; }
        }

        public LinkedList<GameObject> path
        {
            get { return _path; }
        }

        public PathfindingNode(PathfindingNode source)
        {
            _distance = source._distance;
            _destinationTile = source._destinationTile;
            _path = new LinkedList<GameObject>(source._path);
        }

        public PathfindingNode(PathfindingNode source, int nextStep, GameObject nextTile)
        {
            _distance = source._distance + nextStep;
            _destinationTile = nextTile;
            _path = new LinkedList<GameObject>(source._path);
            _path.AddLast(nextTile);
        }

        public PathfindingNode(int step, GameObject tile)
        {
            _distance = step;
            _destinationTile = tile;
            _path = new LinkedList<GameObject>();
            _path.AddFirst(tile);
        }
    }

    //consts and static data
    public const string PICTURE_FILE_EXTENSION = ".pic";
    public const string PICTURE_DIRECTORY = "/pics/";
    private static readonly string defaultPictureFileName = "test.pic";
    private static readonly Vector3 defaultCameraPosition = new Vector3(0f,0f,-10f);
    private const int DEFAULT_CAMERA_ZOOM_LEVEL = 5;
    private const int PREVIEW_WIDTH = 256;
    private const int PREVIEW_HEIGHT = 256;
    private const int PREVIEW_DEPTH = 24;

    public const bool PREVIEW_MODE = true;
    public const float TILE_WIDTH = 1.28f;
    public const float HEX_SIDE = 0.73900834456272097857171043904251f; // 1.28 / sqrt(3)
    public const float TILE_HEIGHT = 2f * HEX_SIDE;
    public readonly Vector3 GRID_BASE_X = new Vector3(TILE_WIDTH, 0f, 0f);
    public readonly Vector3 GRID_BASE_Y = new Vector3(-0.5f * TILE_WIDTH, 1.5f * HEX_SIDE, 0f); 
    public const float INVERSE_HEX_SIDE = 1.3531646934131853855683174543015f;
    public const float SQRT_THREE_OVER_THREE = 0.57735026918962576450914878050196f;
    public const float ONE_THIRD = 0.3333333333333333333333333333333333333333333333f;
    public const float TWO_THIRDS = 0.66666666666666666666666666666666666666666666667f;

    public static MapManager singleton;

    public GameObject openTilePrefab;
    public GameObject blockedTilePrefab;
    [SerializeField] public GameObject tileHightlightBorderPrefab;

    public int defaultColumns = 10;
    public int defaultRows = 10;
    public IntVector2 lastTileBrushed = null;
    public bool hasChanged = false;

    public string mapName = "", previewMapName = "";
    public uint armyPointLimit = 0, previewArmyPointLimit = 0;

    [SerializeField]private string _currentMapFileName;
    private Dictionary<IntVector2, GameObject> mapTiles = new Dictionary<IntVector2, GameObject>(), mapPreviewTiles = new Dictionary<IntVector2, GameObject>();

    [SerializeField] [Range(1f, 10f)] private float zoomIncrement = 2.5f; // This must be constrained to (1,oo) for zooming to work as intended.
    [SerializeField] private float zoomInitLevel = 5f;
    [SerializeField] private float zoomMax = 15f;
    [SerializeField] private float zoomMin = 5f / 3f;

    private float worldUnitsPerPixel;

    private float xCameraMin = 9999999999f;
    private float xCameraMax = -9999999999f;
    private float yCameraMin = 9999999999f;
    private float yCameraMax = -9999999999f;

    private float xPreviewCameraMin = 9999999999f;
    private float xPreviewCameraMax = -9999999999f;
    private float yPreviewCameraMin = 9999999999f;
    private float yPreviewCameraMax = -9999999999f;

    private float xUsableMin = 0f;
    private float xUsableMax = 0f;
    private float yUsableMin = 0f;
    private float yUsableMax = 0f;

    private bool cameraInterrupt = true;

    private Transform tileHolder, previewTileHolder;
    private Camera currentCamera, previewCamera;
    private RenderTexture previewTexture;

    private BrushType _brushType;
    private SymmetrySetting _symmetrySetting = SymmetrySetting.NONE;

    private Dictionary<BrushType,GameObject> _prefabs;
    [SerializeField] private GameObject grassPrefab, rockPrefab, treePrefab, desertPrefab, wastelandPrefab, waterPrefab, ruinsPrefab, marshPrefab, swampPrefab, mountainPrefab, junglePrefab, snowPrefab, _roadIndicatorPrefab, _roadSegmentPrefab;

    private MapContext _mapContext;

    private IntVector2 lastMouseOver;

    private Vector3 autoCameraInitialPos;
    private Vector3 autoCameraTargetLocation;
    private Vector3 autoCameraTotalMovement;
    private float autoCameraInitialZoom;
    private float autoCameraTargetZoomLevel;
    private float autoCameraZoomFactor;
    private float autoCameraBeginTime;
    private float autoCameraEndTime;
    [SerializeField] private float cameraMoveTimePerDistanceLinear = 0.5f;
    [SerializeField] private float cameraMoveTimePerDistancePower = 0.5f;
    [SerializeField] private float cameraMoveTimePerDistanceExponential = 0.5f;
    [SerializeField] private float cameraZoomTimeLogBase = 2f;
    [SerializeField] private float cameraZoomTimeLogarithmic = 2f;
    [SerializeField] private float cameraTimeMax = 2f;
    [SerializeField] private float defaultCenterWeight = 0.2f;

    [SerializeField] private GameObject symmetryIndicatorPrefab;
    private GameObject symmetryIndicator;

    private List<GameObject> borderHighlights;

    // public properties
    public SymmetrySetting symmetrySetting
    {
        set { _symmetrySetting = value;
            if(symmetryIndicator == null)
            {
                symmetryIndicator = Instantiate(symmetryIndicatorPrefab) as GameObject;
            }
            switch(_symmetrySetting)
            {
                case SymmetrySetting.NONE:
                    symmetryIndicator.SetActive(false);
                    break;
                default:
                    symmetryIndicator.SetActive(true);
                    break;
                //TODO: set sprite according to symmetry type
            }
        }
    }

#if UNITY_IOS || UNITY_ANDROID
    private Vector3 prevDifference;
    private Vector3 prevMidPoint;
    private int prevLength = 1;
#endif

    public string currentFileName
    {
        get
        {
            return _currentMapFileName;
        }
    }

    public MapContext mapContext
    {
        set
        {
            _mapContext = value;
            symmetrySetting = SymmetrySetting.NONE;
            switch (_mapContext)
            {
                case MapContext.MAP_EDITOR:
                    // set camera mode to main camera
                    // immediately set up a new map
                    CreateNewMap();
                    break;
                case MapContext.MAIN_MENU:
                    // set camera mode to preview camera
                    _currentMapFileName = "";
                    break;
                default:
                    // wig out?
                    break;
            }
        }
        get
        {
            return _mapContext;
        }
    }

    public GameObject roadIndicatorPrefab
    {
        get
        {
            return _roadIndicatorPrefab;
        }
    }

    public GameObject roadSegmentPrefab
    {
        get
        {
            return _roadSegmentPrefab;
        }
    }

    #region public methods
    public List<GameObject> GetMapTiles()
    {
        // TODO: Figure out if we want to do something smoother for this
        var output = new List<GameObject>();
        foreach (var pair in mapTiles)
        {
            output.Add(pair.Value);
        }
        return output;
    }

    public Vector3 GetPositionAt(IntVector2 pos)
    {
        return (pos.x * GRID_BASE_X + pos.y * GRID_BASE_Y);
    }

    public IntVector2 GetGridPositionAt(Vector3 pos)
    {
        // http://www.redblobgames.com/grids/hexagons/
        float q = (pos.x * SQRT_THREE_OVER_THREE - pos.y * ONE_THIRD) * INVERSE_HEX_SIDE;
        float r = pos.y * TWO_THIRDS * INVERSE_HEX_SIDE;

        // get rounded cube coordinates
        int rx = (int)Math.Round(q);
        int ry = (int)Math.Round(-q-r);
        int rz = (int)Math.Round(r);

        // find amount that has been rounded off
        var x_diff = Math.Abs(rx - q);
        var y_diff = Math.Abs(ry + q + r);
        var z_diff = Math.Abs(rz - r);

        // find which of these rounding distances is furthest
        // convert from cube to axial coordinates
        if (x_diff > y_diff && x_diff > z_diff)
        {
            return new IntVector2(-ry, rz);
        }
        if (y_diff > z_diff)
        {
            return new IntVector2(rx + rz, rz);
        }
        return new IntVector2(-ry, -rx - ry);
    }

    public GameObject GetTileAt(IntVector2 tilePos, bool preview = false)
    {
        if(!preview)
        {
            if (mapTiles.ContainsKey(tilePos))
            {
                return mapTiles[tilePos];
            }
        }
        else
        {
            if (mapPreviewTiles.ContainsKey(tilePos))
            {
                return mapPreviewTiles[tilePos];
            }
        }
        return null;
    }

    public GameObject GetTileAtWorldPos(Vector3 worldPos)
    {
        return GetTileAt(GetGridPositionAt(worldPos));
    }

    public GameObject GetTileAtScreenPos(Vector3 screenPos)
    {
        return GetTileAtWorldPos(Camera.main.ScreenToWorldPoint(screenPos));
    }

    public void GetClick(Vector3 clickPosition)
    {
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(clickPosition);

        Debug.Log("[MapManager:GetClick] You clicked on " + GetGridPositionAt(worldPos));

        var tilePos = GetGridPositionAt(worldPos);
        if (mapTiles.ContainsKey(tilePos))
        {
            //TODO: check for the current scene context, call methods/ pass arguments as appropriate.
            //combat context:
            switch (_mapContext)
            {
                default:
                    break;
            }
        }
    }

    public void CheckMouseOver(Vector3 mousePosition)
    {
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePosition);
        var tilePos = GetGridPositionAt(worldPos);

        if(WorldInterfaceLayer.singleton.dragMode == WorldInterfaceLayer.DragMode.BRUSH)
        {
            var highlightList = new List<IntVector2>();
            highlightList.Add(tilePos);
            var symmetryPos = GetSymmetryTile(tilePos);
            if(symmetryPos != tilePos)
            {
                highlightList.Add(symmetryPos);
            }
            DrawBorderHighlights(highlightList);
        }

        if (mapTiles.ContainsKey(tilePos))
        {
            if(lastMouseOver != tilePos)
            {
                if(lastMouseOver != null)
                {
                    MouseExitTile(lastMouseOver);
                }
                lastMouseOver = tilePos;
                MouseEnterTile(tilePos);
            }
        }
        else
        {
            if(lastMouseOver != null)
            {
                MouseExitTile(lastMouseOver);
                lastMouseOver = null;
            }
        }
    }

    public void MouseOffMap()
    {
        if (lastMouseOver != null)
        {
            MouseExitTile(lastMouseOver);
            lastMouseOver = null;
        }
        ClearBorderHighlights();
    }

    public void GetBrush(Vector3 brushPosition)
    {
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(brushPosition);
        var tilePos = GetGridPositionAt(worldPos);
        Debug.Log("[MapManager:GetBrush] You brushed on " + tilePos);

        if (BrushTypeIsDeploymentZone() && (_symmetrySetting != SymmetrySetting.NONE) && tilePos == GetSymmetryTile(tilePos))
        {
            //Do not brush deploment zones on symmetry points in brush mode
            return;
        }
        BrushTile(tilePos);
        if (lastTileBrushed == null)
            ;
        else if ((lastTileBrushed - tilePos).magnitude <= 1)
            ;
        else
            DrawLine(lastTileBrushed, tilePos);
        lastTileBrushed = tilePos;

        // TODO: special case logic
        var symmetryTile = GetSymmetryTile(tilePos);
        if (symmetryTile != tilePos)
        {
            ToggleZoneColor();
            BrushTile(symmetryTile);
            ToggleZoneColor();
        }
    }

    private void DrawLine(IntVector2 start, IntVector2 end)
    {
        if (end == start)
            return;
        IntVector2 offset = end - start;
        int lineLength = offset.magnitude;
        IntVector2 primaryDirection = IntVector2.GetVectorFromCase(IntVector2.GetDirectionFromVector(offset));
        IntVector2 secondaryOffset = offset - (lineLength * primaryDirection);
        int secondaryLength = secondaryOffset.magnitude;
        IntVector2 secondaryDirection = IntVector2.GetVectorFromCase(IntVector2.GetDirectionFromVector(secondaryOffset));

        for(int ii = 1; ii < lineLength; ii++)
        {
            int secondaryPortion = (int)((((float)ii * secondaryLength) / lineLength) +0.5f);
            BrushTile(start + ii * primaryDirection + secondaryPortion * secondaryDirection);
        }
    }

    public void EndBrush()
    {
        lastTileBrushed = null;
    }

    private bool BrushTypeIsDeploymentZone()
    {
        return (_brushType == BrushType.RED_ZONE || _brushType == BrushType.BLUE_ZONE);
    }

    private void ToggleZoneColor()
    {
        if (BrushTypeIsDeploymentZone())
        {
            if (_brushType == BrushType.RED_ZONE)
                _brushType = BrushType.BLUE_ZONE;
            else
                _brushType = BrushType.RED_ZONE;
        }
    }

    public void BrushTile(IntVector2 tilePos)
    {
        if (mapTiles.ContainsKey(tilePos))
        {
            switch(_brushType)
            {
                case BrushType.ERASER:
                    Debug.Log("[MapManager:GetBrush] Erasing tile.");
                    DeleteTile(mapTiles[tilePos]);
                    hasChanged = true;
                    break;
                case BrushType.GRASS:
                case BrushType.TREE:
                case BrushType.DESERT:
                case BrushType.ROCK:
                case BrushType.WASTELAND:
                case BrushType.WATER:
                case BrushType.RUINS:
                case BrushType.MARSH:
                case BrushType.SWAMP:
                case BrushType.MOUNTAIN:
                case BrushType.JUNGLE:
                case BrushType.SNOW:
                    Debug.Log("[MapManager:GetBrush] brushing over tile.");
                    OverwriteTile(mapTiles[tilePos], _prefabs[_brushType]);
                    hasChanged = true;
                    break;
                //case BrushType.RED_ZONE:
                //case BrushType.BLUE_ZONE:
                //case BrushType.CLEAR_ZONE:
                //    Debug.Log("[MapManager:GetBrush] brushing tile mod.");
                //    SetDeployZone(mapTiles[tilePos], _factions[_brushType]);
                //    hasChanged = true;
                //    break;
                //case BrushType.PAVE:
                //    Debug.Log("[MapManager:GetBrush] paving tile.");
                //    SetTileRoadProperty(mapTiles[tilePos], true);
                //    hasChanged = true;
                //    break;
                //case BrushType.UNPAVE:
                //    Debug.Log("[MapManager:GetBrush] unpaving tile.");
                //    SetTileRoadProperty(mapTiles[tilePos], false);
                //    hasChanged = true;
                //    break;
                case BrushType.NONE:
                    Debug.LogError("[MapManager:GetBrush] Has Key, No brush type!");
                    break;
            }
            //TODO: change tile's type, delete if eraser
        }
        else
        {
            //TODO: create new tile, do nothing if eraser
            //
            switch(_brushType)
            {
                case BrushType.ERASER:
                //case BrushType.RED_ZONE:
                //case BrushType.BLUE_ZONE:
                //case BrushType.CLEAR_ZONE:
                //case BrushType.PAVE:
                //case BrushType.UNPAVE:
                //    Debug.Log("[MapManager:GetBrush] No tile to erase or mod.");
                //    break;
                case BrushType.GRASS:
                case BrushType.TREE:
                case BrushType.ROCK:
                case BrushType.WASTELAND:
                case BrushType.WATER:
                case BrushType.RUINS:
                case BrushType.MARSH:
                case BrushType.SWAMP:
                case BrushType.MOUNTAIN:
                case BrushType.JUNGLE:
                case BrushType.SNOW:
                case BrushType.DESERT:
                    Debug.Log("[MapManager:GetBrush] brushing new tile.");
                    InstantiateTile(_prefabs[_brushType], tilePos);
                    mapTiles[tilePos].GetComponent<TileListener>().mainColor = MapEditorManager.singleton.currentBrushColor;
                    hasChanged = true;
                    break;
                case BrushType.NONE:
                    Debug.LogError("[MapManager:GetBrush] No Key, No brush type!");
                    break;
            }
        }
    }

    public List<GameObject> AdjacentTiles(IntVector2 tilePos, bool preview = false)
    {
        List<GameObject> output = new List<GameObject>();
        GameObject nextTile;

        nextTile = GetTileAt(tilePos + IntVector2.RIGHT, preview);
        if (nextTile != null)
            output.Add(nextTile);
        nextTile = GetTileAt(tilePos + IntVector2.ONE, preview);
        if (nextTile != null)
            output.Add(nextTile);
        nextTile = GetTileAt(tilePos + IntVector2.UP, preview);
        if (nextTile != null)
            output.Add(nextTile);
        nextTile = GetTileAt(tilePos - IntVector2.RIGHT, preview);
        if (nextTile != null)
            output.Add(nextTile);
        nextTile = GetTileAt(tilePos - IntVector2.ONE, preview);
        if (nextTile != null)
            output.Add(nextTile);
        nextTile = GetTileAt(tilePos - IntVector2.UP, preview);
        if (nextTile != null)
            output.Add(nextTile);

        return output;
    }

    public GameObject InstantiateTile(GameObject prefab, IntVector2 newPos, bool preview = false)
    {
        GameObject instance = Instantiate(prefab, GetPositionAt(newPos), Quaternion.identity) as GameObject;
        if (!preview)
        {
            instance.transform.SetParent(tileHolder);
        }
        else
        {
            instance.transform.SetParent(previewTileHolder);
        }
        instance.GetComponent<TileListener>().OurInit();
        instance.GetComponent<TileListener>().SetGridPosition(newPos);
        instance.GetComponent<TileListener>().previewMap = preview;
        AdjustCameraLimits(newPos);
        if (!preview)
        {
            mapTiles.Add(newPos, instance);
        }
        else
        {
            mapPreviewTiles.Add(newPos, instance);
        }
        return instance;
    }

    public void LimitCameraScrolling()
    {
        Vector3 pos = currentCamera.transform.position;
        float x = Math.Min(Math.Max(pos.x, xCameraMin), xCameraMax);
        float y = Math.Min(Math.Max(pos.y, yCameraMin), yCameraMax);
        currentCamera.transform.position = new Vector3(x, y, pos.z);
    }

    public void AdjustCameraLimits(TileListener tile, bool preview = false)
    {
        AdjustCameraLimits(tile.gridPos, preview);
    }

    public void RestoreAllTileColors()
    {
        foreach (var pair in mapTiles)
        {
            pair.Value.GetComponent<TileListener>().RestoreDefaultColor();
        }
    }

    public void CameraPan(Vector3 pan)
    {
        currentCamera.transform.position = pan * worldUnitsPerPixel + currentCamera.transform.position;
        LimitCameraScrolling();
        cameraInterrupt = true;
    }

    public void CameraZoom(float zoomStep)
    {
        if (zoomStep > 0)
        {
            //zoom in
            currentCamera.orthographicSize = Mathf.Max(currentCamera.orthographicSize * Mathf.Pow(zoomIncrement, -zoomStep), zoomMin);
        }
        else
        {
            //zoom out
            currentCamera.orthographicSize = Mathf.Min(currentCamera.orthographicSize * Mathf.Pow(zoomIncrement, -zoomStep), zoomMax);
        }
        AdjustPanSpeed();
        cameraInterrupt = true;
    }

    public void CameraZoom(float zoomStep, Vector3 screenPos)
    {
        var worldPos = currentCamera.ScreenToWorldPoint(screenPos);
        CameraZoom(zoomStep);
        currentCamera.transform.position -= currentCamera.ScreenToWorldPoint(screenPos) - worldPos;
        LimitCameraScrolling();
    }

    public void SetCameraZoom(float zoomLevel)
    {
        //Debug.Log("[MapManager:SetCameraZoom] Setting zoom level to " + zoomLevel);
        if(zoomLevel <= zoomMax &&
           zoomLevel >= zoomMin)
        {
            currentCamera.orthographicSize = zoomLevel;
        }
        AdjustPanSpeed();
        //Debug.Log("[MapManager:SetCameraZoom] Zoom level is now " + currentCamera.orthographicSize);
    }

    public void ForceCameraZoom(float zoomLevel, bool preview = false)
    {
        currentCamera.orthographicSize = zoomLevel;
        if (!preview)
        {
            AdjustPanSpeed();
        }
    }

    public void ScaleCameraZoom(float multiplier)
    {
        float zoomLevel = currentCamera.orthographicSize * multiplier;
        SetCameraZoom(zoomLevel);
    }

    public void ScaleCameraZoom(float multiplier, Vector3 screenPos)
    {
        var worldPos = currentCamera.ScreenToWorldPoint(screenPos);
        ScaleCameraZoom(multiplier);
        currentCamera.transform.position -= currentCamera.ScreenToWorldPoint(screenPos) - worldPos;
    }

#if UNITY_IOS || UNITY_ANDROID
    public void HandleTouchPanAndZoom()
    {
        if (Input.touches.Length > 0)
        {
            // for touches of any length, find midpoint, store length
            // if length has changed, perform opposite pan action to counter jumpiness
            // if length is 2 and was 2 last time, perform stretches/pinches
            var touches = Input.touches;
            Vector3 midPoint = Vector3.zero;
            foreach (var touch in touches)
            {
                midPoint += (Vector3)touch.position;
            }
            midPoint /= touches.Length;
            if (touches.Length != prevLength && prevLength != 0)
            {
                CameraPan(midPoint - prevMidPoint);
            }
            if (touches.Length == 2)
            {
                int multiplier = 1;
                Vector3 curDifference = Vector3.zero;
                foreach (var touch in touches)
                {
                    curDifference += (Vector3)touch.position * multiplier;
                    multiplier = -1;

                }

                if (prevLength == 2)
                {
                    ScaleCameraZoom(prevDifference.magnitude / curDifference.magnitude, midPoint);
                }

                prevDifference = curDifference;
            }
            prevMidPoint = midPoint;
            prevLength = touches.Length;
        }
        else
        {
            prevLength = 0;
        }
    }
#endif

    public void MoveCameraFocus(List<GameObject> focusTiles)
    {
        MoveCameraFocus(focusTiles, defaultCenterWeight);
    }

    public void MoveCameraFocus(List<GameObject> focusTiles, float centerWeight)
    {
        cameraInterrupt = true;

        FindUsableViewRect();
        Vector3 worldUsableBottomLeft = currentCamera.ScreenToWorldPoint(new Vector3(xUsableMin, yUsableMin));
        Vector3 worldUsableTopRight = currentCamera.ScreenToWorldPoint(new Vector3(xUsableMax, yUsableMax));

        float xFocusMin = 9999999999f;
        float xFocusMax = -9999999999f;
        float yFocusMin = 9999999999f;
        float yFocusMax = -9999999999f;

        bool moveCamera = false;
        foreach (var gameObj in focusTiles)
        {
            var worldPosition = gameObj.transform.position;
            xFocusMin = Math.Min(xFocusMin, worldPosition.x - TILE_WIDTH * 0.5f);
            xFocusMax = Math.Max(xFocusMax, worldPosition.x + TILE_WIDTH * 0.5f);
            yFocusMin = Math.Min(yFocusMin, worldPosition.y - TILE_HEIGHT * 0.5f);
            yFocusMax = Math.Max(yFocusMax, worldPosition.y + TILE_HEIGHT * 0.5f);

            if (worldPosition.x < worldUsableBottomLeft.x || worldPosition.x > worldUsableTopRight.x ||
                worldPosition.y < worldUsableBottomLeft.y || worldPosition.y > worldUsableTopRight.y)
            {
                moveCamera = true;
            }
        }

        if (!moveCamera)
        {
            return;
        }

        float usableWidth = xUsableMax - xUsableMin;
        float usableHeight = yUsableMax - yUsableMin;
        float focusWidth = xFocusMax - xFocusMin;
        float focusHeight = yFocusMax - yFocusMin;
        // TODO: To optimize, reduce divisions by inverting zoomRatio
        float zoomRatio = Mathf.Min(usableWidth * worldUnitsPerPixel / focusWidth, usableHeight * worldUnitsPerPixel / focusHeight);

        Debug.Log("[MapManager:MoveCameraFocus] zoomRatio is " + zoomRatio);

        if(zoomRatio > 1f)
        {
            zoomRatio = 1f;
        }
        else
        {
            //@HAX : temporarily change zoom level to get screen distance maths.
            currentCamera.orthographicSize /= zoomRatio;
            worldUsableBottomLeft = currentCamera.ScreenToWorldPoint(new Vector3(xUsableMin, yUsableMin));
            worldUsableTopRight = currentCamera.ScreenToWorldPoint(new Vector3(xUsableMax, yUsableMax));
        }

        if (worldUsableTopRight.x < xFocusMax || worldUsableTopRight.y < yFocusMax ||
            worldUsableBottomLeft.x > xFocusMin || worldUsableBottomLeft.y > yFocusMin)
        {
            Vector3 usableCenter = (worldUsableBottomLeft + worldUsableTopRight) *0.5f;
            Vector3 targetLocationCenter = currentCamera.transform.position + new Vector3((xFocusMin + xFocusMax) * 0.5f, (yFocusMin + yFocusMax) * 0.5f, -10.0f) - usableCenter;

            float minimumMoveX, minimumMoveY;
            if(xFocusMin < worldUsableBottomLeft.x)
            {
                minimumMoveX = xFocusMin - worldUsableBottomLeft.x;
            }
            else if(xFocusMax > worldUsableTopRight.x)
            {
                minimumMoveX = xFocusMax - worldUsableTopRight.x;
            }
            else
            {
                minimumMoveX = 0;
            }

            if (yFocusMin < worldUsableBottomLeft.y)
            {
                minimumMoveY = yFocusMin - worldUsableBottomLeft.y;
            }
            else if (yFocusMax > worldUsableTopRight.y)
            {
                minimumMoveY = yFocusMax - worldUsableTopRight.y;
            }
            else
            {
                minimumMoveY = 0;
            }
            Vector3 targetLocationMinMove = new Vector3(minimumMoveX, minimumMoveY) + currentCamera.transform.position;

            Vector3 targetLocation = targetLocationCenter * centerWeight + targetLocationMinMove * (1 - centerWeight);

            //@HAX : HAAAAAACK 
            currentCamera.orthographicSize *= zoomRatio;

            //move time depends on distance and change in zoom factor
            float distance = (currentCamera.transform.position - targetLocation).magnitude / currentCamera.orthographicSize;
            float cameraMoveTime = distance * cameraMoveTimePerDistanceLinear + Mathf.Pow(distance, cameraMoveTimePerDistancePower) * cameraMoveTimePerDistanceExponential;
            float cameraZoomTime = Mathf.Abs(Mathf.Log(zoomRatio, cameraZoomTimeLogBase)) * cameraZoomTimeLogarithmic;
            float cameraTime = Mathf.Max(cameraMoveTime, cameraZoomTime);
            cameraTime = Mathf.Min(cameraTime, cameraTimeMax);
            //StartCoroutine(AutomaticCameraMove(targetLocation, currentCamera.orthographicSize / zoomRatio, 2f));
            BeginAutomaticCameraMove(targetLocation, currentCamera.orthographicSize / zoomRatio, cameraTime);
        }
    }

    public void AdjustUsableViewRect(UIEconomyTracker.Side side, float limit)
    {
        Debug.Log("[MapManager:AdjustUsableViewRect] " + side + " " + limit);
        switch(side)
        {
            case UIEconomyTracker.Side.LEFT:
                if(xUsableMin < limit)
                {
                    xUsableMin = limit;
                }
                break;
            case UIEconomyTracker.Side.RIGHT:
                if(xUsableMax > limit)
                {
                    xUsableMax = limit;
                }
                break;
            case UIEconomyTracker.Side.TOP:
                if(yUsableMax > limit)
                {
                    yUsableMax = limit;
                }
                break;
            case UIEconomyTracker.Side.BOTTOM:
                if(yUsableMin < limit)
                {
                    yUsableMin = limit;
                }
                break;
        }
    }

    public void CreateDefaultMap()
    {
        GameObject toInstantiate;

        tileHolder = new GameObject("Tiles").transform;

        for (int ixx = 0; ixx < defaultColumns; ixx++)
        {
            for (int iyy = 0; iyy < defaultRows; iyy++)
            {
                if ((ixx * iyy) % (ixx + iyy + 1) == 2) // change this later if we need terrain to make more sense
                {
                    toInstantiate = blockedTilePrefab;
                }
                else
                {
                    toInstantiate = openTilePrefab;
                }

                InstantiateTile(toInstantiate, new IntVector2(ixx, iyy));
            }
        }

        CreateTileNodeGraph();

        InitCameraMapCenter();
        AdjustPanSpeed();

        // test saving a map file
        SaveCurrentMapAs(defaultPictureFileName);
        SerializableMap load = FileManager.singleton.Load<SerializableMap>(defaultPictureFileName);
        Debug.Log("[MapManager:CreateDefaultMap] " + load.ToString());
    }

    //@deprecated
    //public void SaveCurrentMap(string mapName, string fileName)
    //{
    //    FileManager.singleton.EnsureDirectoryExists(mapDirectory);

    //    SerializableMap map = new SerializableMap();
    //    map.mapName = mapName;
    //    map.tiles = new SerializableTile[mapTiles.Count];
    //    int ii = 0;
    //    foreach (var pair in mapTiles)
    //    {
    //        map.tiles[ii] = new SerializableTile();
    //        map.tiles[ii].x = pair.Key.x;
    //        map.tiles[ii].y = pair.Key.y;
    //        map.tiles[ii].type = pair.Value.GetComponent<TileListener>().terrainType;
    //        ii++;
    //    }
    //    FileManager.singleton.Save<SerializableMap>(map, mapDirectory + fileName);
    //}

    public void SaveCurrentMapAs(string fileName)
    {
        Debug.Log("[MapManager:SaveCurrentMapAs] Changing current map to " + fileName);
        if (FileManager.FileExtensionIs(fileName, PICTURE_FILE_EXTENSION))
        {
            _currentMapFileName = fileName;
        }
        else
        {
            _currentMapFileName = fileName + PICTURE_FILE_EXTENSION;
        }
        SaveCurrentMap();
    }

    public void SaveCurrentMap()
    {
        Debug.Log("[MapManager:SaveCurrentMap] Saving current map " + _currentMapFileName);
        FileManager.singleton.EnsureDirectoryExists(PICTURE_DIRECTORY);
        SerializableMap map = SerializeMap();
        FileManager.singleton.Save<SerializableMap>(map, PICTURE_DIRECTORY + _currentMapFileName);
        hasChanged = false;
    }

    public SerializableMap SerializeMap()
    {
        SerializableMap map = new SerializableMap();
        //map.mapName = mapName;
        //map.armyPointLimit = armyPointLimit;
        //Debug.Log("[MapManager:SaveCurrentMap] Saved Map Name: " + map.mapName);
        map.tiles = new SerializableTile[mapTiles.Count];
        int ii = 0;
        foreach (var pair in mapTiles)
        {
            map.tiles[ii] = new SerializableTile();
            map.tiles[ii].x = pair.Key.x;
            map.tiles[ii].y = pair.Key.y;
            map.tiles[ii].mainColor = new SerializableColor(pair.Value.GetComponent<TileListener>().mainColor);
            //map.tiles[ii].deployable = pair.Value.GetComponent<TileListener>().deployable;
            //map.tiles[ii].type = pair.Value.GetComponent<TileListener>().terrain.type;
            //map.tiles[ii].isRoad = pair.Value.GetComponent<TileListener>().HasTerrainProperty(TerrainDefinition.TerrainProperty.ROAD);
            ii++;
        }

        return map;
    }

    public bool LoadMap(string fileName, bool preview = false)
    {
        string correctedFileName = fileName;
        if (!FileManager.FileExtensionIs(fileName, PICTURE_FILE_EXTENSION))
        {
            correctedFileName += PICTURE_FILE_EXTENSION;
        }
        SerializableMap load = FileManager.singleton.Load<SerializableMap>(PICTURE_DIRECTORY + correctedFileName);
        if (load == null)
            return false;//TODO: Throw exceptions?
        //if file was opened successfully, change the current map file path name
        if (!preview)
        {
            _currentMapFileName = correctedFileName;

            //armyPointLimit = load.armyPointLimit;
            //mapName = load.mapName;
            //Debug.Log("[MapManager:LoadMap] Loaded Map Name: " + mapName);

            DeserializeMap(load);
        }
        else
        {
            //previewArmyPointLimit = load.armyPointLimit;
            //previewMapName = load.mapName;
            DeserializeMap(load, PREVIEW_MODE);
        }
        return true;

    }

    public void DeserializeMap(SerializableMap load, bool preview = false)
    {
        GameObject toInstantiate;
        //clear tile holder and mapTiles before we load a map.
        if (!preview)
        {
            ClearTileHolder();
        }
        else
        {
            ClearTileHolder(PREVIEW_MODE);
        }
        foreach (var tile in load.tiles)
        {
            //switch (tile.type)
            //{
            //    default:
            //    case TerrainDefinition.TerrainType.GRASS:
            //        toInstantiate = _prefabs[BrushType.GRASS];
            //        break;
            //    case TerrainDefinition.TerrainType.ROCK:
            //        toInstantiate = _prefabs[BrushType.ROCK];
            //        break;
            //    case TerrainDefinition.TerrainType.TREE:
            //        toInstantiate = _prefabs[BrushType.TREE];
            //        break;
            //    case TerrainDefinition.TerrainType.DESERT:
            //        toInstantiate = _prefabs[BrushType.DESERT];
            //        break;
            //    case TerrainDefinition.TerrainType.WASTELAND:
            //        toInstantiate = _prefabs[BrushType.WASTELAND];
            //        break;
            //    case TerrainDefinition.TerrainType.WATER:
            //        toInstantiate = _prefabs[BrushType.WATER];
            //        break;
            //    case TerrainDefinition.TerrainType.RUINS:
            //        toInstantiate = _prefabs[BrushType.RUINS];
            //        break;
            //    case TerrainDefinition.TerrainType.MARSH:
            //        toInstantiate = _prefabs[BrushType.MARSH];
            //        break;
            //    case TerrainDefinition.TerrainType.SWAMP:
            //        toInstantiate = _prefabs[BrushType.SWAMP];
            //        break;
            //    case TerrainDefinition.TerrainType.MOUNTAIN:
            //        toInstantiate = _prefabs[BrushType.MOUNTAIN];
            //        break;
            //    case TerrainDefinition.TerrainType.JUNGLE:
            //        toInstantiate = _prefabs[BrushType.JUNGLE];
            //        break;
            //    case TerrainDefinition.TerrainType.SNOW:
            //        toInstantiate = _prefabs[BrushType.SNOW];
            //        break;
            //}

            IntVector2 pos = new IntVector2(tile.x, tile.y);
            toInstantiate = _prefabs[BrushType.GRASS];
            var instance = InstantiateTile(toInstantiate, pos, preview);
            instance.GetComponent<TileListener>().mainColor = new Color(tile.mainColor.r, tile.mainColor.g, tile.mainColor.b, tile.mainColor.a);
            //SetDeployZone(instance, tile.deployable);
            //instance.GetComponent<TileListener>().SetRoadProperty(tile.isRoad);
        }

        CreateTileNodeGraph(preview);
        RecalculateCameraLimits(preview);
        if (!preview)
        {
            InitCameraMapCenter();
            AdjustPanSpeed();
            hasChanged = false;
        }
    }

    public void CreateNewMap()
    {
        ClearTileHolder();

        //recenter the camera
        currentCamera.transform.position = defaultCameraPosition;
        currentCamera.orthographicSize = DEFAULT_CAMERA_ZOOM_LEVEL;
        AdjustPanSpeed();
        RecalculateCameraLimits();
        //clear the current file name
        _currentMapFileName = "";
        Debug.Log("[MapManager:CreateNewMap] Clearing Map Name");
        mapName = "";
        armyPointLimit = 0;
        hasChanged = false;
    }

    public void SelectEraserBrush()
    {
        _brushType = BrushType.ERASER;
    }

    public void SelectSwatch1Brush()
    {
        _brushType = BrushType.GRASS;
    }

    public void SelectDesertBrush()
    {
        _brushType = BrushType.DESERT;
    }

    public void SelectRockBrush()
    {
        _brushType = BrushType.ROCK;
    }

    public void SelectTreeBrush()
    {
        _brushType = BrushType.TREE;
    }

    public void SelectWastelandBrush()
    {
        _brushType = BrushType.WASTELAND;
    }

    public void SelectWaterBrush()
    {
        _brushType = BrushType.WATER;
    }

    public void SelectRuinsBrush()
    {
        _brushType = BrushType.RUINS;
    }

    public void SelectMarshBrush()
    {
        _brushType = BrushType.MARSH;
    }

    public void SelectSwampBrush()
    {
        _brushType = BrushType.SWAMP;
    }

    public void SelectMountainBrush()
    {
        _brushType = BrushType.MOUNTAIN;
    }

    public void SelectJungleBrush()
    {
        _brushType = BrushType.JUNGLE;
    }

    public void SelectSnowBrush()
    {
        _brushType = BrushType.SNOW;
    }

    public void SelectRedDeploymentZoneBrush()
    {
        _brushType = BrushType.RED_ZONE;
    }

    public void SelectBlueDeploymentZoneBrush()
    {
        _brushType = BrushType.BLUE_ZONE;
    }

    public void SelectClearDeploymentZoneBrush()
    {
        _brushType = BrushType.CLEAR_ZONE;
    }

    public void SelectPaveBrush()
    {
        _brushType = BrushType.PAVE;
    }

    public void SelectUnpaveBrush()
    {
        _brushType = BrushType.UNPAVE;
    }

    public void OverwriteTile(GameObject tile, GameObject prefab)
    {
        tile.GetComponent<SpriteRenderer>().sprite = prefab.GetComponent<SpriteRenderer>().sprite;
        tile.GetComponent<TileListener>().mainColor = MapEditorManager.singleton.currentBrushColor;
        // remove road before changing terrain type, so if any road stuff needs to cleaned up, we still know whether or not the tile has a road... resetting to definition will overwrite that
        //tile.GetComponent<TileListener>().SetRoadProperty(false);
        //tile.GetComponent<TileListener>().terrain = TerrainDefinition.GetDefinition(prefab.GetComponent<TileListener>().terrainType);
        //tile.GetComponent<TileListener>().deployable = CombatManager.Faction.NONE;

        //tile.GetComponent<TileListener>().PropertiesChanged();
    }

    public void DeleteTile(GameObject tile)
    {
        if(tile != null)
        {
            tile.GetComponent<TileListener>().DeleteTile();
            mapTiles.Remove(tile.GetComponent<TileListener>().gridPos);
            GameObject.Destroy(tile);
            RecalculateCameraLimits();
        }
    }

    /// <summary>
    /// Pathfinding Dijkstra's algorithm
    /// </summary>
    /// <param name="reachableTiles">A dictionary of tiles that can be reached by pathfinding, output by this function</param>
    /// <param name="unsearchedTiles">The initial seed tiles, determining where to start searching from</param>
    /// <param name="maxRange">The maximum distance from the origin to check</param>
    /// <param name="faction">The faction of the initiating unit, if pathing needs to avoid enemy units. If faction is NONE, unit collision is ignored</param>
    /// <param name="unitGameObject">The gameObject of the initiating unit, to determine movement type</param>
    //public static void PathingInternal(out LinkedList<PathfindingNode> doneTiles, LinkedList<PathfindingNode> unsearchedTiles, int maxRange, UnitHandler.PathingType pathingType, CombatManager.Faction faction, GameObject unitGameObject)
    //{
    //    bool isAttack = Enum.IsDefined(typeof(UnitHandler.AttackType), (UnitHandler.AttackType)pathingType);

    //    //  doneTiles:          Tiles checked for neighbors are moved from unsearchedTiles to here
    //    doneTiles = new LinkedList<PathfindingNode>();

    //    // Continue processing until unsearchedTiles is empty
    //    while (unsearchedTiles.Count > 0)
    //    {
    //        // unsearchedTiles should be kept in order by distance,
    //        // so First should always be the closest or tied for closest
    //        TileListener curTile = unsearchedTiles.First.Value.destinationTile.GetComponent<TileListener>();
    //        int distance = unsearchedTiles.First.Value.distance;
    //        foreach (var edge in curTile.neighbors)
    //        {
    //            bool matchFound = false;
    //            bool shouldInsert = true;
    //            // Check that this neighbor is within range and that the neighbor isn't occupied by an enemy unit
    //            if (distance + edge.cost[pathingType] <= maxRange && !edge.tile.GetComponent<TileListener>().IsOccupiedByEnemy(faction))
    //            {
    //                // Loop through doneTiles to see if this was already found
    //                PathingCheckRedundantDoneEntry(doneTiles, pathingType, distance, edge, ref matchFound, ref shouldInsert);

    //                // Loop through unsearchedTiles to insert this neighbor and/or remove duplicates
    //                PathingAttemptInsertIntoUnsearchedTiles(doneTiles, unsearchedTiles, pathingType, unitGameObject, isAttack, distance, edge, matchFound, ref shouldInsert);
    //            }
    //        }
    //        // Remove first from unsearched list, add to done list
    //        // MAYBEDO: Is it better to insert it in a specific location?
    //        doneTiles.AddFirst(unsearchedTiles.First.Value);
    //        unsearchedTiles.RemoveFirst();
    //    }
    //}

    public Texture2D MapPreview(bool preview = false)
    {
        currentCamera = previewCamera;
        if(!preview)
        {
            InitCameraMapCenter();
            ZoomCameraToFitMap();
            bool holderActive = tileHolder.gameObject.activeSelf;
            tileHolder.gameObject.SetActive(true);

            currentCamera.Render();
            RenderTexture.active = previewTexture;
            Texture2D mapPreview = new Texture2D(PREVIEW_WIDTH, PREVIEW_HEIGHT, TextureFormat.ARGB32, false); // false to ignore mipmap data
            mapPreview.ReadPixels(new Rect(0, 0, PREVIEW_WIDTH, PREVIEW_HEIGHT), 0, 0);
            mapPreview.Apply();
            RenderTexture.active = null;

            currentCamera = Camera.main;
            tileHolder.gameObject.SetActive(holderActive);
            return mapPreview;
        }
        else
        {
            InitCameraMapCenter(PREVIEW_MODE);
            ZoomCameraToFitMap(PREVIEW_MODE);

            bool holderActive = false;
            if (tileHolder != null)
            {
                holderActive = tileHolder.gameObject.activeSelf;
                tileHolder.gameObject.SetActive(false);
            }
            bool previewHolderActive = previewTileHolder.gameObject.activeSelf;
            previewTileHolder.gameObject.SetActive(true);

            currentCamera.Render();
            RenderTexture.active = previewTexture;
            Texture2D mapPreview = new Texture2D(PREVIEW_WIDTH, PREVIEW_HEIGHT, TextureFormat.ARGB32, false); // false to ignore mipmap data
            mapPreview.ReadPixels(new Rect(0, 0, PREVIEW_WIDTH, PREVIEW_HEIGHT), 0, 0);
            mapPreview.Apply();
            RenderTexture.active = null;

            currentCamera = Camera.main;
            if(tileHolder != null)
            {
                tileHolder.gameObject.SetActive(holderActive);
            }
            previewTileHolder.gameObject.SetActive(previewHolderActive);
            return mapPreview;
        }
    }

    public void HideTiles()
    {
        tileHolder.gameObject.SetActive(false);
    }

    public void ShowTiles()
    {
        tileHolder.gameObject.SetActive(true);
    }

    public void DrawBorderHighlights(List<IntVector2> positions)
    {
        ClearBorderHighlights();
        foreach(var pos in positions)
        {
            TileBorderHighlight(pos);
        }
    }

    #endregion

    #region private methods

    private IntVector2 GetSymmetryTile(IntVector2 tilePos)
    {
        switch(_symmetrySetting)
        {
            case SymmetrySetting.NONE:
                return tilePos;
            case SymmetrySetting.ROTATIONAL:
                return -tilePos;
            case SymmetrySetting.HORIZONTAL:
                return new IntVector2(tilePos.y - tilePos.x, tilePos.y);
            default:
                Debug.LogError("[MapManager:GetSymmetryTile] Invalid Symmetry Type");
                return tilePos;
        }
    }

    private void MouseEnterTile(IntVector2 tileLocation)
    {
        //Debug.Log("[MapManager:MouseEnterTile] You moused over " + tileLocation);
    }

    private void MouseExitTile(IntVector2 tileLocation)
    {
        //Debug.Log("[MapManager:MouseExitTile] You stopped mousing over " + tileLocation);
    }

    /// <summary>
    /// TODO: Summarize this
    /// </summary>
    /// <param name="doneTiles"></param>
    /// <param name="pathingType"></param>
    /// <param name="distance"></param>
    /// <param name="edge"></param>
    /// <param name="matchFound"></param>
    /// <param name="shouldInsert"></param>
    //private static void PathingCheckRedundantDoneEntry(LinkedList<PathfindingNode> doneTiles, UnitHandler.PathingType pathingType, int distance, TileEdge edge, ref bool matchFound, ref bool shouldInsert)
    //{
    //    for (LinkedListNode<PathfindingNode> node = doneTiles.First; node != null; node = node.Next)
    //    {
    //        if (edge.tile == node.Value.destinationTile)
    //        {
    //            matchFound = true;
    //            //found previously searched tile, check for shorter path.
    //            //(9/9/2015) this is now possible because tile mods can make tiles go directly to doneTiles.
    //            if (edge.cost[pathingType] + distance < node.Value.distance)
    //            {
    //                doneTiles.Remove(node);
    //            }
    //            else
    //            {
    //                shouldInsert = false;
    //            }

    //            //shouldInsert = false;
    //        }

    //        // If you've found a match, break out of the loop.
    //        if (matchFound)
    //        {
    //            break;
    //        }
    //    }
    //}

    /// <summary>
    /// TODO: Summarize this
    /// </summary>
    /// <param name="doneTiles"></param>
    /// <param name="unsearchedTiles"></param>
    /// <param name="pathingType"></param>
    /// <param name="unitGameObject"></param>
    /// <param name="isAttack"></param>
    /// <param name="distance"></param>
    /// <param name="edge"></param>
    /// <param name="matchFound"></param>
    /// <param name="shouldInsert"></param>
    /// <returns></returns>
    //private static void PathingAttemptInsertIntoUnsearchedTiles(LinkedList<PathfindingNode> doneTiles, LinkedList<PathfindingNode> unsearchedTiles, UnitHandler.PathingType pathingType, GameObject unitGameObject, bool isAttack, int distance, TileEdge edge, bool matchFound, ref bool shouldInsert)
    //{
    //    for (LinkedListNode<PathfindingNode> node = unsearchedTiles.First; ; node = node.Next)
    //    {
    //        // If either a match hasn't been found or you still need to insert the tile, keep looping
    //        if (!shouldInsert && matchFound)
    //        {
    //            break;
    //        }
    //        // If node == null, we've reached the end of the list
    //        if (node == null)
    //        {
    //            // If we haven't inserted the tile yet, add it to the end
    //            if (shouldInsert)
    //            {
    //                PathingInsertTileSorted(doneTiles, unsearchedTiles, pathingType, unitGameObject, isAttack, distance, edge, null);
    //            }
    //            // Either way, break out of the loop
    //            break;
    //        }
    //        // We're keeping the list in order.
    //        //      If node's distance is greater than the total distance to this tile
    //        //      and we haven't added it or ruled it out yet, insert it before this node.
    //        if (distance + edge.cost[pathingType] < node.Value.distance && shouldInsert)
    //        {
    //            PathingInsertTileSorted(doneTiles, unsearchedTiles, pathingType, unitGameObject, isAttack, distance, edge, node);
    //            shouldInsert = false;
    //        }
    //        // If we find a duplicate...
    //        if (edge.tile == node.Value.destinationTile)
    //        {
    //            // If we've passed the distance of the new tile...
    //            if (distance + edge.cost[pathingType] < node.Value.distance)
    //            {
    //                // This tile is old. Get rid of it!
    //                unsearchedTiles.Remove(node);
    //            }
    //            // Otherwise, the tile already exists as close or closer.
    //            // Either way, we're done!
    //            break;
    //        }
    //    }
    //}

    /// <summary>
    /// TODO: Summarize this
    /// </summary>
    /// <param name="doneTiles"></param>
    /// <param name="unsearchedTiles"></param>
    /// <param name="pathingType"></param>
    /// <param name="unitGameObject"></param>
    /// <param name="isAttack"></param>
    /// <param name="distance"></param>
    /// <param name="edge"></param>
    /// <param name="node"></param>
//    private static void PathingInsertTileSorted(LinkedList<PathfindingNode> doneTiles, LinkedList<PathfindingNode> unsearchedTiles, UnitHandler.PathingType pathingType, GameObject unitGameObject, bool isAttack, int distance, TileEdge edge, LinkedListNode<PathfindingNode> node)
//    {
//        switch (edge.tile.GetComponent<TileListener>().GetPathingType(unitGameObject, isAttack))
//        {
//            case TileModifier.PathingType.CLEAR:
//                if (node != null)
//                {
//                    unsearchedTiles.AddBefore(node, new PathfindingNode(unsearchedTiles.First.Value, edge.cost[pathingType], edge.tile));
////                    unsearchedTiles.AddBefore(node, new KeyValuePair<int, GameObject>(distance + edge.cost[pathingType], edge.tile));
//                }
//                else
//                {
//                    unsearchedTiles.AddLast(new PathfindingNode(unsearchedTiles.First.Value, edge.cost[pathingType], edge.tile));
////                    unsearchedTiles.AddLast(new KeyValuePair<int, GameObject>(distance + edge.cost[pathingType], edge.tile));
//                }
//                break;
//            case TileModifier.PathingType.STOP:
//                doneTiles.AddLast(new PathfindingNode(unsearchedTiles.First.Value, edge.cost[pathingType], edge.tile));
////                doneTiles.AddLast(new KeyValuePair<int, GameObject>(distance + edge.cost[pathingType], edge.tile));
//                break;
//            case TileModifier.PathingType.IMPASSABLE:
//                break;
//            default:
//                Debug.LogError("[MapManager:PathingInsertTileSorted] Invalid Tile Modifier Type found");
//                break;
//        }
//    }

    private void ClearTileHolder(bool preview = false)
    {
        if (!preview)
        {
            if (tileHolder != null)
            {
                DestroyAllTiles();
                GameObject.Destroy(tileHolder.gameObject);
                Debug.Log("[MapManager:ClearTileHolder] cleaned up old map successfully");
            }
            mapTiles.Clear();
            tileHolder = new GameObject("Tiles").transform;
        }
        else
        {
            if (previewTileHolder != null)
            {
                DestroyAllTiles(true);
                GameObject.Destroy(previewTileHolder.gameObject);
                Debug.Log("[MapManager:ClearTileHolder] cleaned up old map successfully");
            }
            mapPreviewTiles.Clear();
            previewTileHolder = new GameObject("PreviewTiles").transform;
            previewTileHolder.gameObject.SetActive(false);
        }
    }

    private void ZoomCameraToFitMap(bool preview = false)
    {
        if(!preview)
        {
            ForceCameraZoom(Math.Max((xCameraMax - xCameraMin) / currentCamera.aspect, yCameraMax - yCameraMin) * 0.5f);
        }
        else
        {
            ForceCameraZoom(Math.Max((xPreviewCameraMax - xPreviewCameraMin) / currentCamera.aspect, yPreviewCameraMax - yPreviewCameraMin) * 0.5f, PREVIEW_MODE);
        }
    }

    private void AdjustCameraLimits(IntVector2 pos, bool preview = false)
    {
        var worldPosition = GetPositionAt(pos);
        if(!preview)
        {
            xCameraMin = Math.Min(xCameraMin, worldPosition.x - TILE_WIDTH * 0.5f);
            xCameraMax = Math.Max(xCameraMax, worldPosition.x + TILE_WIDTH * 0.5f);
            yCameraMin = Math.Min(yCameraMin, worldPosition.y - TILE_HEIGHT * 0.5f);
            yCameraMax = Math.Max(yCameraMax, worldPosition.y + TILE_HEIGHT * 0.5f);
        }
        else
        {
            xPreviewCameraMin = Math.Min(xPreviewCameraMin, worldPosition.x - TILE_WIDTH * 0.5f);
            xPreviewCameraMax = Math.Max(xPreviewCameraMax, worldPosition.x + TILE_WIDTH * 0.5f);
            yPreviewCameraMin = Math.Min(yPreviewCameraMin, worldPosition.y - TILE_HEIGHT * 0.5f);
            yPreviewCameraMax = Math.Max(yPreviewCameraMax, worldPosition.y + TILE_HEIGHT * 0.5f);
        }
    }

    private void RecalculateCameraLimits(bool preview = false)
    {
        if (!preview)
        {
            if (mapTiles.Count == 0)
            {
                xCameraMin = 0f;
                xCameraMax = 0f;
                yCameraMin = 0f;
                yCameraMax = 0f;
            }
            else
            {
                xCameraMin = 9999999999f;
                xCameraMax = -9999999999f;
                yCameraMin = 9999999999f;
                yCameraMax = -9999999999f;

                foreach (var pair in mapTiles)
                {
                    AdjustCameraLimits(pair.Key);
                }
            }
        }
        else
        {
            if (mapPreviewTiles.Count == 0)
            {
                xPreviewCameraMin = 0f;
                xPreviewCameraMax = 0f;
                yPreviewCameraMin = 0f;
                yPreviewCameraMax = 0f;
            }
            else
            {
                xPreviewCameraMin = 9999999999f;
                xPreviewCameraMax = -9999999999f;
                yPreviewCameraMin = 9999999999f;
                yPreviewCameraMax = -9999999999f;

                foreach (var pair in mapPreviewTiles)
                {
                    AdjustCameraLimits(pair.Key, PREVIEW_MODE);
                }
            }
        }
        
    }

    private void AdjustPanSpeed()
    {
        worldUnitsPerPixel = GetZoomAdjustedWorldDist();
        //Debug.Log("[MapManager:AdjustPanSpeed] worldUnitsPerPixel = " + worldUnitsPerPixel + ", Orthographic size = " + currentCamera.orthographicSize + ", ratio = " + currentCamera.orthographicSize / worldUnitsPerPixel);
    }

    private float GetZoomAdjustedWorldDist()
    {
        //we are assuming the camera never rotates. if it does, use magnitude instead of x.
        return (currentCamera.ScreenToWorldPoint(new Vector2(1f, 0f)) - currentCamera.ScreenToWorldPoint(new Vector2(0f, 0f))).x;
    }

    private void InitCameraMapCenter(bool preview = false)
    {
        if (!preview)
        {
            currentCamera.transform.position = new Vector3((xCameraMin + xCameraMax) * 0.5f, (yCameraMin + yCameraMax) * 0.5f, -10f);
        }
        else
        {
            currentCamera.transform.position = new Vector3((xPreviewCameraMin + xPreviewCameraMax) * 0.5f, (yPreviewCameraMin + yPreviewCameraMax) * 0.5f, -10f);
        }
    }

    private void ResetUsableViewRect()
    {
        xUsableMin = 0f;
        xUsableMax = Camera.main.pixelWidth;
        yUsableMin = 0f;
        yUsableMax = Camera.main.pixelHeight;
    }

    private void FindUsableViewRect()
    {
        ResetUsableViewRect();
        EventManager.singleton.Broadcast(StageNine.Events.EventType.UI_ECONOMY_CHECK);
    }

    private void BeginAutomaticCameraMove(Vector3 location, float orthographicSize, float time)
    {
        cameraInterrupt = false;
        autoCameraInitialPos = currentCamera.transform.position;
        autoCameraTargetLocation = new Vector3(location.x, location.y, autoCameraInitialPos.z);
        autoCameraTotalMovement = autoCameraTargetLocation - autoCameraInitialPos;
        autoCameraInitialZoom = currentCamera.orthographicSize;
        autoCameraTargetZoomLevel = Mathf.Min(zoomMax, Mathf.Max(zoomMin, orthographicSize));
        autoCameraZoomFactor = autoCameraTargetZoomLevel / autoCameraInitialZoom;

        autoCameraBeginTime = Time.time;
        autoCameraEndTime = Time.time + time;
    }

    private void ContinueAutomaticCameraMove()
    {
        if (cameraInterrupt)
            return;
        if (Time.time >= autoCameraEndTime)
        {
            currentCamera.transform.position = autoCameraTargetLocation;
            SetCameraZoom(autoCameraTargetZoomLevel);
            cameraInterrupt = true;
        }
        else
        {
            float elapsedTime = Time.time - autoCameraBeginTime;
            float totalTime = autoCameraEndTime - autoCameraBeginTime;
            float timeProgress = elapsedTime / totalTime;
            float tSquare = timeProgress * timeProgress;
            float tCube = tSquare * timeProgress;
            float progress = -2f * tCube + 3f * tSquare;
            currentCamera.transform.position = autoCameraInitialPos + autoCameraTotalMovement * progress;
            SetCameraZoom(autoCameraInitialZoom * Mathf.Pow(autoCameraZoomFactor, progress));
            elapsedTime += Time.deltaTime;
        }
    }

    // @deprecated
    //private IEnumerator AutomaticCameraMove(Vector3 location, float orthographicSize, float time)
    //{
    //    cameraInterrupt = false;
    //    Vector3 initialPos = Camera.main.transform.position;
    //    Vector3 targetLocation = new Vector3(location.x, location.y, initialPos.z);
    //    Vector3 totalMovement = targetLocation - initialPos;
    //    float initialZoom = Camera.main.orthographicSize;
    //    float targetZoomLevel = Mathf.Min(zoomMax, Mathf.Max(zoomMin, orthographicSize));
    //    float zoomFactor = targetZoomLevel / initialZoom;

    //    float elapsedTime = 0f;
    //    while(elapsedTime < time)
    //    {
    //        if(cameraInterrupt)
    //        {
    //            yield break;
    //        }
    //        float timeProgress = elapsedTime / time;
    //        float tSquare = timeProgress * timeProgress;
    //        float tCube = tSquare * timeProgress;
    //        float progress = -2f * tCube + 3f * tSquare;
    //        Camera.main.transform.position = initialPos + totalMovement * progress;
    //        SetCameraZoom(initialZoom * Mathf.Pow(zoomFactor, progress));
    //        elapsedTime += Time.deltaTime;
    //        yield return null;
    //    }
    //    Camera.main.transform.position = targetLocation;
    //    SetCameraZoom(targetZoomLevel);
    //}

    private void CreateTileNodeGraph(bool preview = false)
    {
        if(!preview)
        {
            foreach (var pair in mapTiles)
            {
                pair.Value.GetComponent<TileListener>().GetNeighbors();
            }
        }
        else
        {
            foreach (var pair in mapPreviewTiles)
            {
                pair.Value.GetComponent<TileListener>().GetNeighbors();
            }
        }
    }

    /// <summary>
    /// Removes all tiles from the TileHolder and mapTiles Dictionary
    /// </summary>
    private void DestroyAllTiles(bool preview = false)
    {
        if (!preview)
        {
            foreach (var pair in mapTiles)
            {
                pair.Value.SetActive(false);
                GameObject.Destroy(pair.Value);
            }
            mapTiles.Clear();
        }
        else
        {
            foreach (var pair in mapPreviewTiles)
            {
                pair.Value.SetActive(false);
                GameObject.Destroy(pair.Value);
            }
            mapPreviewTiles.Clear();
        }
    }

    private void InitializeFields()
    {
        currentCamera = Camera.main;
        previewTexture = new RenderTexture(PREVIEW_WIDTH, PREVIEW_HEIGHT, PREVIEW_DEPTH);
        _prefabs = new Dictionary<BrushType, GameObject>();
        borderHighlights = new List<GameObject>();
        _prefabs[BrushType.GRASS] = grassPrefab;
        _prefabs[BrushType.DESERT] = desertPrefab;
        _prefabs[BrushType.TREE] = treePrefab;
        _prefabs[BrushType.ROCK] = rockPrefab;
        _prefabs[BrushType.WASTELAND] = wastelandPrefab;
        _prefabs[BrushType.WATER] = waterPrefab;
        _prefabs[BrushType.RUINS] = ruinsPrefab;
        _prefabs[BrushType.MARSH] = marshPrefab;
        _prefabs[BrushType.SWAMP] = swampPrefab;
        _prefabs[BrushType.MOUNTAIN] = mountainPrefab;
        _prefabs[BrushType.JUNGLE] = junglePrefab;
        _prefabs[BrushType.SNOW] = snowPrefab;
        lastMouseOver = null;
    }

    private void ClearBorderHighlights()
    {
        foreach (var border in borderHighlights)
        {
            Destroy(border);
        }
        borderHighlights.Clear();
    }

    private void TileBorderHighlight(IntVector2 pos)
    {
        var instance = Instantiate(tileHightlightBorderPrefab);
        instance.transform.position = GetPositionAt(pos);
        borderHighlights.Add(instance);
    }

    #endregion

    #region MonoBehaviors

    void Awake()
    {
        Debug.Log("[MapManager:Awake]");
        if (singleton == null)
        {
            Debug.Log("MapManager checking in.");
            DontDestroyOnLoad(gameObject);
            singleton = this;
            InitializeFields();
        }
        else
        {
            //if anything needs to be re:init, have this call the singleton.
            Debug.Log("MapManager checking out.");
            GameObject.Destroy(gameObject);
        }
        _currentMapFileName = defaultPictureFileName;
    }

    void Start()
    {
        OnLevelWasLoaded(SceneManager.GetActiveScene().buildIndex);
    }

    void Update()
    {
        ContinueAutomaticCameraMove();
    }

    void OnLevelWasLoaded(int level)
    {
        if (singleton == this)
        {
            currentCamera = Camera.main;
            previewCamera = EventManager.singleton.previewCamera;
            if (previewCamera != null)
            {
                previewCamera.targetTexture = previewTexture;
                previewCamera.aspect = (float)PREVIEW_WIDTH / (float)PREVIEW_HEIGHT;
            }
        }
    }

    #endregion
}

[Serializable]
public class SerializableMap
{
    //public string mapName;
    //public uint armyPointLimit;
    public SerializableTile[] tiles;

    #region public methods
    public override string ToString()
    {
        string output = "";
        foreach(var tile in tiles)
        {
            output += "\n   " + tile.ToString();
        }
        return output;
    }
    #endregion
}