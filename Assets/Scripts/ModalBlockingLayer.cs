using UnityEngine;
using System.Collections;

public class ModalBlockingLayer : MonoBehaviour {

    const int SORTING_DISPLACEMENT = -1;


    #region public methods

    public void AdjustModalHeight(int height)
    {
        if (height > 1)
        {
            //activate the blocking layer and change the sorting order of the blocking layer (in its own script!)
            gameObject.SetActive(true);
            GetComponent<Canvas>().sortingOrder = ((height * ModalPopup.VISIBLE_CANVAS_INTERVAL) + SORTING_DISPLACEMENT);
        }
        else
        {
            //deactivate the blocking layer
            gameObject.SetActive(false);
        }
    }
    #endregion


    #region monobehaviors
    // Use this for initialization
    void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
    #endregion
}
