using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Events;

public class UISaveAsDialog : ModalPopup
{
    //enums

    //subclasses

    //consts and static data
    public delegate void SaveAction(string fileName);
    //public data

    //private data
    [SerializeField] private InputField _fileNameField;
    [SerializeField] private FileViewer fileViewer;
    //private string _fileName;

    private SaveAction _saveAction;
    private UnityAction _confirmedSaveCallback;

    //properties
    public SaveAction saveAction
    {
        set
        {
            _saveAction = value;
        }
    }

    public UnityAction confirmedSaveCallback 
    {
        set
        {
            _confirmedSaveCallback = value;
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

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            fileViewer.NextInList();
        }

        if (Input.GetKeyDown(KeyCode.UpArrow))
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

    public override void BackButtonResponse()
    {
        CancelSave();
    }

    public void OnInputFieldChanged(string value)
    {
        fileViewer.HighlightFileName(_fileNameField.text);
    }

    public void OnInputCompleted()
    {
        Debug.Log("[UISaveAsDialog:OnInputCompleted] Input Ended!");
#if UNITY_STANDALONE
        if (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter))
        {
            ConfirmSave();
        }
#endif
    }

    public void OnSaveButtonPressed()
    {
        ConfirmSave();
    }

    public void OnCancelButtonPressed()
    {
        CancelSave();
    }

    public void DisplayFiles(string directory, string extension)
    {
        fileViewer.ClearMetadata();
        fileViewer.ClearList();
        fileViewer.PopulateList(directory, extension);
        fileViewer.doubleClickCallback = new UnityAction(() =>
        {
            ConfirmSave();
        });
    }

    public void PopulateFileInfo(string directory, string extension, string fileName = "")
    {
        DisplayFiles(directory, extension);
        _fileNameField.text = fileName;
    }
    #endregion

    #region private methods
    private void ConfirmSave()
    {
        _saveAction(_fileNameField.text);

        _fileNameField.text = "";
        EventManager.singleton.ReturnFocus();
        if (_confirmedSaveCallback != null)
        {
            Debug.Log("[UISaveAsDialog:OnSaveButtonPressed] Executing stored callback");
            _confirmedSaveCallback();
            _confirmedSaveCallback = null;
        }
    }

    private void CancelSave()
    {
        _fileNameField.text = "";
        //TODO: need a callback for returning modal focus.
        EventManager.singleton.ReturnFocus();
        _saveAction = null;
        _confirmedSaveCallback = null;
    }

    #endregion

    #region monobehaviors

    #endregion
}
