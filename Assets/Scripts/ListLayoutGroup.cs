using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ListLayoutGroup : MonoBehaviour
{
    //enums

    //subclasses

    //consts and static data

    

    //public data

    //private data
    private List<GameObject> uiEntities;
    [SerializeField] private Vector3 sizeOffset;
    [SerializeField] private Vector3 listTop = Vector3.zero;
    //public properties

    public Vector3 offset
    {
        get { return sizeOffset; }
    }

    //methods
    #region public methods

    public void Add(GameObject toAdd)
    {
        if(toAdd == null)
        {
            // bluh
            Debug.LogError("[ListLayoutGroup:Add] Game Object is null");
        }
        else if(uiEntities.Contains(toAdd))
        {
            // double bluh
            Debug.LogError("[ListLayoutGroup:Add] Game Object is already in the list");
        }
        else
        {
            uiEntities.Add(toAdd);
            SetPosition(toAdd);
        }
    }

    public void Remove(GameObject toRemove)
    {
        if (uiEntities.Contains(toRemove))
        {
            int index = uiEntities.IndexOf(toRemove);

            uiEntities.Remove(toRemove);
            Destroy(toRemove);

            for(;index < uiEntities.Count; index++)
            {
                SetPosition(uiEntities[index]);
            }
        }
        else
        {
            Debug.LogError("[ListLayoutGroup:Remove] Game Object not found in the list");
        }
    }

    #endregion

    #region private methods

    private void SetPosition(GameObject thingy)
    {
        thingy.transform.SetParent(this.transform);
        thingy.transform.SetAsFirstSibling();
        thingy.transform.localScale = Vector3.one;

        int index = uiEntities.IndexOf(thingy);

        if (0 == index)
        {
            // set y position to top
            // x position is same as offset of list
            thingy.transform.localPosition = listTop;
        }
        else
        {
            // get information from uiEntities[index - 1]
            thingy.transform.localPosition = uiEntities[index - 1].transform.localPosition + sizeOffset;    
        }
    }

    #endregion

    #region monobehaviors

    void Awake()
    {
        uiEntities = new List<GameObject>();
    }

    #endregion
}
