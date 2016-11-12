using UnityEngine;
using System.Collections;
using StageNine.Events;

public class UIEconomyTracker : MonoBehaviour, IEventListener {
    //enums
    public enum Side
    {
    	LEFT,
    	RIGHT,
    	TOP,
    	BOTTOM
    }

    //subclasses

    //consts and static data
    
    //public data
    public Side sideToAdjust;

    //private data

    //methods
    #region public methods

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

    public void HandleEvent(StageNine.Events.EventType eType)
    {
        if(eType == StageNine.Events.EventType.UI_ECONOMY_CHECK)
        {
            Debug.Log("[UIEconomyTracker:HandleEvent] event recieved.");
            OnEconomyCheck();
        }
    }

    #endregion

    #region private methods

    private void OnEconomyCheck()
    {
    	if(gameObject.activeInHierarchy)
    	{
    		RectTransform rect = gameObject.GetComponent<RectTransform>();
    		if(rect != null)
    		{
    			Vector3[] corners = new Vector3[4];
    			GetScreenCorners(rect, corners);
    			Debug.Log("[UIEconomyTracker:OnEconomyCheck] " + corners[0] + corners[1] + corners[2] + corners[3]);
	    		switch(sideToAdjust)
	    		{
	    			case Side.LEFT:
	    				float maxX = 0f;
	    				foreach (var corner in corners)
	    				{
	    					if(maxX < corner.x)
	    					{
	    						maxX = corner.x;
	    					}
	    				}
	    				MapManager.singleton.AdjustUsableViewRect(sideToAdjust, maxX);
	    				break;
	    			case Side.RIGHT:
	    				float minX = Camera.main.pixelWidth;
	    				foreach (var corner in corners)
	    				{
	    					if(minX > corner.x)
	    					{
	    						minX = corner.x;
	    					}
	    				}
	    				MapManager.singleton.AdjustUsableViewRect(sideToAdjust, minX);
	    				break;
	    			case Side.TOP:
	    				float minY = Camera.main.pixelHeight;
	    				foreach (var corner in corners)
	    				{
	    					if(minY > corner.y)
	    					{
	    						minY = corner.y;
	    					}
	    				}
	    				MapManager.singleton.AdjustUsableViewRect(sideToAdjust, minY);
	    				break;
	    			case Side.BOTTOM:
	    				float maxY = 0f;
	    				foreach (var corner in corners)
	    				{
	    					if(maxY < corner.y)
	    					{
	    						maxY = corner.y;
	    					}
	    				}
	    				MapManager.singleton.AdjustUsableViewRect(sideToAdjust, maxY);
	    				break;
	    		}
    		}
    		else
    		{
    			Debug.LogError("[UIEconomyTracker:OnEconomyCheck] gameObject has no RectTransform!");
    		}
    	}
    }

    private void GetScreenCorners(RectTransform rect, Vector3[] fourCornersArray)
    {
        rect.GetWorldCorners(fourCornersArray);
        for(int ii = 0; ii < 4; ++ii)
        {
            fourCornersArray[ii] = Camera.main.WorldToScreenPoint(fourCornersArray[ii]);
        }
    }

    #endregion

    #region monobehaviors

    protected virtual void Start()
    {
        Debug.Log("[UIEconomyTracker:Start]");
        if (!Connect())
        {
            Debug.LogError("[UIEconomyTracker:Start] Could not connect listener to EventManager");
        }
    }


    void OnDestroy()
    {
        Debug.Log("[UIEconomyTracker:OnDestroy]");
        if (!Disconnect())
        {
            Debug.LogWarning("[UIEconomyTracker:OnDestroy] Couldn't disconnect from EventManager!");
        }
    }

    #endregion
}
