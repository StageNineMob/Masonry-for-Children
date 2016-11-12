using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public abstract class Draggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
	[HideInInspector]public bool isDragging = false;
    [SerializeField]protected Sprite dragImage;

    #region public methods
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        //CombatManager.singleton.forceNoMouseDrag = true;
        isDragging = true;
        Debug.Log("[Draggable:OnBeginDrag] Detected click!");
        EventManager.singleton.draggablePuppet.SetActive(true);
        EventManager.singleton.draggablePuppet.GetComponent<Image>().sprite = dragImage;
        if (GetComponent<Image>() != null)
        {
            EventManager.singleton.draggablePuppet.GetComponent<Image>().color = GetComponent<Image>().color;
            EventManager.singleton.draggablePuppet.transform.localScale *= (GetComponent<RectTransform>().rect.width * transform.lossyScale.x) / (EventManager.singleton.draggablePuppet.GetComponent<RectTransform>().rect.width * EventManager.singleton.draggablePuppet.transform.lossyScale.x);
            GetComponent<Image>().enabled = false;
        }
        else
        {
            EventManager.singleton.draggablePuppet.GetComponent<Image>().color = GetComponent<SpriteRenderer>().color;
            EventManager.singleton.draggablePuppet.transform.localScale *= (transform.lossyScale.x) / (EventManager.singleton.draggablePuppet.GetComponent<RectTransform>().rect.width * EventManager.singleton.draggablePuppet.transform.lossyScale.x);
            GetComponent<SpriteRenderer>().enabled = false;
            // TODO: Set things up so we can avoid using transform.Find
            Transform overlay = transform.Find("Sprite Overlay");
            if (null != overlay)
            {
                overlay.GetComponent<SpriteRenderer>().enabled = false;
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log("[Draggable:OnEndDrag] Drag has ended");
        EventManager.singleton.draggablePuppet.SetActive(false);
        //CombatManager.singleton.forceNoMouseDrag = false;
        isDragging = false;

        // Check if the mouse position is over a collider in worldspace.
        GameObject mouseAt = UIObjectAtMouse(eventData);
        if (mouseAt != null)
        {
            Debug.Log("[Draggable:OnEndDrag] Cursor is over " + mouseAt.name);
            if (mouseAt.name == "World Interface Layer")
            {
                var tile = MapManager.singleton.GetTileAtScreenPos(Input.mousePosition);
                if (tile != null)
                {
                    Debug.Log("Tile Position: " + tile.GetComponent<TileListener>().gridPos);
                    OnDropOntoTile(tile);
                }
            }
            else
            {
                OnDropOntoUI(eventData);
            }
        }
        if (GetComponent<Image>() != null)
        {
            GetComponent<Image>().enabled = true;
        }
        else
        {
            GetComponent<SpriteRenderer>().enabled = true;
            // TODO: Set things up so we can avoid using transform.Find
            Transform overlay = transform.Find("Sprite Overlay");
            if (null != overlay)
            {
                overlay.GetComponent<SpriteRenderer>().enabled = true;
            }
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isDragging)
        {
            Vector2 WorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            EventManager.singleton.draggablePuppet.transform.position = WorldPos;
        }
    }

    public abstract void OnDropOntoTile(GameObject tile);

    public abstract void OnDropOntoUI(PointerEventData eventData);

    public void SetPosition(Vector3 newPos)
    {
        transform.position = newPos;
    }

    #endregion

    #region protected methods

    protected GameObject UIObjectAtMouse(PointerEventData eventData)
    {
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        if (results.Count == 0)
        {
            return null;
        }
        else
        {
            return results[0].gameObject;
        }
    }

    protected bool IsUIObjectAtMouse(PointerEventData eventData, string objectName)
    {
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        foreach (var result in results)
        {
            if (objectName == result.gameObject.name)
            {
                return true;
            }
        }
        Debug.Log("could not find " + objectName);
        return false;
    }

    #endregion

    #region monobehaviors

    // Use this for initialization
    void Start()
    {
    }

    #endregion

}
