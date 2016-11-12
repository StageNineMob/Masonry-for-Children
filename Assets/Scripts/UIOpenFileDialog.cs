using UnityEngine;
using System.Collections;
using System;
using UnityEngine.UI;
using UnityEngine.Events;

public class UIOpenFileDialog : ModalPopup
{
    //enums

    //subclasses

    //consts and static data
    public delegate void LoadingCallback(string fileName);
    //public data

    //private data
    [SerializeField] private InputField _fileNameField;
    [SerializeField] private FileViewer fileViewer;
    //private string _fileName;

    private LoadingCallback _loadingCallback;

    //properties

    /// <summary>
    /// expects to do something with the filename that the user inputs.
    /// </summary>
    public LoadingCallback loadingCallback
    {
        set
        {
            _loadingCallback = value;
        }
    }

    //methods
    #region public methods
#if UNITY_STANDALONE

    public override void KeyboardUpdate()
    {
        base.KeyboardUpdate();

        //TODO:Think about limiting scroll event handling to only trigger when the point is in the desired region
        var mouseWheelAxis = Input.GetAxis("Mouse ScrollWheel");
        if (mouseWheelAxis != 0)
        {
            //scroll the scroll rect via scroll bar.
            fileViewer.Scroll(mouseWheelAxis);
        }

        if(Input.GetKeyDown(KeyCode.DownArrow))
        {
            fileViewer.NextInList();
        }

        if(Input.GetKeyDown(KeyCode.UpArrow))
        {
            fileViewer.PreviousInList();
        }
    }

#endif
#if UNITY_IOS || UNITY_ANDROID
    public override void TouchScreenUpdate()
    {

    }
#endif

    public void OnInputFieldChanged(string value)
    {
        fileViewer.HighlightFileName(_fileNameField.text);
    }

    public void OnInputCompleted()
    {
        Debug.Log("[UIOpenDialog:OnInputCompleted] Input Ended!");
#if UNITY_STANDALONE
        if (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter))
        {
            ConfirmOpen();
        }
#endif
    }

    public void OnOpenButtonPressed()
    {
        ConfirmOpen();
    }

    public void OnCancelButtonPressed()
    {
        CancelOpen();
    }

    public override void BackButtonResponse()
    {
        CancelOpen();
    }

    public void DisplayFiles(string directory, string extension)
    {
        fileViewer.ClearMetadata();
        fileViewer.ClearList();
        fileViewer.PopulateList(directory, extension);
        fileViewer.doubleClickCallback = new UnityAction(() =>
        {
            ConfirmOpen();
        });
    }

    public void PopulateFileInfo(string directory, string extension, string fileName = "")
    {
        DisplayFiles(directory, extension);
        _fileNameField.text = fileName;
    }
    #endregion

    #region private methods

    private void ConfirmOpen()
    {
        Debug.Log("[UIOpenDialog:ConfirmOpen] file name is " + _fileNameField.text);
        EventManager.singleton.ReturnFocus();
        _loadingCallback(_fileNameField.text);
    }

    private void CancelOpen()
    {
        _fileNameField.text = "";
        //TODO: need a callback for returning modal focus.
        EventManager.singleton.ReturnFocus();
    }
    #endregion

    #region monobehaviors

    #endregion
}
