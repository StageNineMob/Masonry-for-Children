using UnityEngine;
using System.Collections;
using StageNine.Events;

public class PanelResizer : MonoBehaviour, IEventListener
{
    public float panelSpace = 1f;
    public float height = 1f;

    #region public methods

    public void HandleEvent(StageNine.Events.EventType eType)
    {
        if(eType == StageNine.Events.EventType.RESIZE_SCREEN)
        {
            ScalePanel();
        }
    }

    public bool Connect()
    {
        if (EventManager.singleton != null)
        {
            EventManager.singleton.ConnectListener(this);
            return true;
        }
        return false;
    }

    public bool Disconnect()
    {
        if (EventManager.singleton != null)
        {
            EventManager.singleton.DisconnectListener(this);
            return true;
        }
        return false;
    }

    #endregion

    #region private methods

    private void ScalePanel()
    {
        float newScale = (Screen.height*panelSpace)/height;
        transform.localScale = Vector3.one * newScale;
    }

    #endregion

    #region monobehaviors

    // Use this for initialization
    void Awake()
    {
        Debug.Log("[PanelResizer:Awake]");
    }

    void Start ()
    {
        Debug.Log("[PanelResizer:Start]");
        ScalePanel();
        if (!Connect())
        {
            Debug.LogError("[PanelResizer:Start] Couldn't connect to EventManager!");
        }
	}
	
    //// Update is called once per frame
    //void Update ()
    //{
    //}

    void OnDestroy()
    {
        Debug.Log("[PanelResizer:OnDestroy]");
        if (!Disconnect())
        {
            Debug.LogWarning("[PanelResizer:OnDestroy] Couldn't disconnect from EventManager!");
        }
    }

    #endregion
}
