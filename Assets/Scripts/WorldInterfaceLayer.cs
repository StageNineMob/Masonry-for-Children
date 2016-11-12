using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WorldInterfaceLayer : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler
{
    public enum DragMode
    {
        CAMERA,
        BRUSH,
        UNIT
    }

    public static WorldInterfaceLayer singleton;

    private const int DRAG_THRESHOLD =
#if UNITY_IOS || UNITY_ANDROID
        10;
#else
    5;
#endif

    private DragMode _dragMode = DragMode.CAMERA;
    private Vector3 mouseLastPos;
    private bool isDragging = false;
    private GameObject draggingUnit = null;
    private Vector3 pointerDownLocation;

    [SerializeField] private GraphicRaycaster raycaster;

    public DragMode dragMode
    {
        get
        {
            return _dragMode;
        }
    }

#region public methods

    public void OnBeginDrag(PointerEventData eventData)
    {
        draggingUnit = null;
        if(_dragMode == DragMode.UNIT)
        {
            Debug.Log("[WorldInterfaceLayer:OnBeginDrag] DragMode is unit, checking for unit");
            var tile = MapManager.singleton.GetTileAtScreenPos(pointerDownLocation);
            if (tile != null)
            {
                if (tile.GetComponent<TileListener>().occupied)
                {
                    Debug.Log("[WorldInterfaceLayer:OnBeginDrag] Unit found, passing OnBeginDrag");
                    if (tile.GetComponent<TileListener>().occupant.GetComponent<UnitHandler>().isDraggable)
                    {
                        draggingUnit = tile.GetComponent<TileListener>().occupant;
                        draggingUnit.GetComponent<DraggableOnBoardUnit>().OnBeginDrag(eventData);
                    }
                }
            }
        }
        //clickBeganPos = Input.mousePosition;
        mouseLastPos = Input.mousePosition;
        isDragging = true;
    }

    public void SetCameraMode()
    {
        _dragMode = DragMode.CAMERA;
    }

    public void SetBrushMode()
    {
        _dragMode = DragMode.BRUSH;
    }

    public void SetUnitMode()
    {
        _dragMode = DragMode.UNIT;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (draggingUnit != null)
        {
            draggingUnit.GetComponent<Draggable>().OnEndDrag(eventData);
            draggingUnit = null;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (draggingUnit == null)
        {
            switch (_dragMode)
            {
                case DragMode.CAMERA:
                case DragMode.UNIT:
                    MapManager.singleton.CameraPan(mouseLastPos - Input.mousePosition);
                    break;
                case DragMode.BRUSH:
                    MapManager.singleton.GetBrush(Input.mousePosition);
                    break;
            }

            mouseLastPos = Input.mousePosition;
        }
        else
        {
            draggingUnit.GetComponent<Draggable>().OnDrag(eventData);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (MapManager.singleton.mapContext == MapManager.MapContext.MAP_EDITOR)
        {
            MapEditorManager.singleton.CloseSubPanels();
        }
        pointerDownLocation = Input.mousePosition;
        Debug.Log("You clicked on WIL!");
        switch (_dragMode)
        {
            case DragMode.CAMERA:
            case DragMode.UNIT:
                break;
            case DragMode.BRUSH:
                MapManager.singleton.GetBrush(Input.mousePosition);
                break;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log("You stopped clicking on WIL.");
        if(isDragging)
        {
            isDragging = false;
        }
        else
        {
            MapManager.singleton.GetClick(Input.mousePosition);
        }
    }


    public bool MouseOverWil()
    {
        PointerEventData pointer = new PointerEventData(EventSystem.current);
        pointer.position = Input.mousePosition;
        List<RaycastResult> results = new List<RaycastResult>();
        
        // raycaster is the GraphicRaycaster of the Combat / Editor UI Canvas
        raycaster.Raycast(pointer, results);
        //Debug.Log("[WorldInterfaceLayer:MouseOverWil] " + results.Count);
        // as such, it will give us how many UI elements are under the cursor... in the way of WIL!
        if(results.Count > 0)
        {
            // so if there's at least one, the mouse is over the UI, not WIL.
            return false;
        }
        return true;
    }
#endregion

#region private methods
    private void InitializeFields()
    {
        EventSystem.current.pixelDragThreshold = DRAG_THRESHOLD;
    }
#endregion

#region monobehaviors

    void Update()
    {
        if(MouseOverWil())
        {
            MapManager.singleton.CheckMouseOver(Input.mousePosition);
        }
        else
        {
            MapManager.singleton.MouseOffMap();
        }
    }

    void Awake()
    {
        Debug.Log("[WorldInterfaceLayer:Awake]");
        if (singleton == null)
        {
            Debug.Log("Hello, world!");
            singleton = this;
            InitializeFields();
        }
        else
        {
            Debug.Log("Goodbye, cruel world!");
            GameObject.Destroy(gameObject);
        }
    }
#endregion
}
