using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class MapEditorMenu : ModalPopup {
    //enums

    //subclasses

    //consts and static data

    //public data

    //private data

    //methods
    #region public methods

#if UNITY_STANDALONE

    public override void KeyboardUpdate()
    {
        base.KeyboardUpdate();
    }

#endif
#if UNITY_IOS || UNITY_ANDROID
    public override void TouchScreenUpdate()
    {
        var touches = Input.touches;
        if (touches.Length == 1)
        {
            //panning or clicking on tiles
        }
        else if (touches.Length == 2)
        {
            //zooming or something fancy
        }
    }
#endif
    public void QuitButtonPressed()
    {
        UnityAction action = new UnityAction(() => { SceneManager.LoadScene(MainMenu.MAIN_MENU_SCENE); });
        AskForSaveBefore(action);
    }

    public void OptionsButtonPressed()
    {
        MapEditorManager.singleton.ShowOptionsDialog();
    }

    public void SaveButtonPressed()
    {
        if(MapManager.singleton.currentFileName != "")
        {
            MapManager.singleton.SaveCurrentMap();
        }
        else
        {
            MapEditorManager.singleton.ShowSaveAsDialog();
        }
    }

    public void SaveAsButtonPressed()
    {
        MapEditorManager.singleton.ShowSaveAsDialog();
    }

    public void OpenButtonPressed()
    {
        UnityAction action = new UnityAction(() => 
        { 
            MapEditorManager.singleton.ShowOpenFileDialog();
        });
        AskForSaveBefore(action);
    }

    public void NewMapButtonPressed()
    {
        UnityAction action = new UnityAction(() => 
        { 
            MapEditorManager.singleton.RequestNewMap();
            CloseMenu();
        });
        AskForSaveBefore(action);
    }

    private void AskForSaveBefore(UnityAction action)
    {
        // Pop up a confirmation dialog to ask the user if they want
        // to save their changes.  They have 3 options, "Yes", "No" and "Cancel".
        if (MapManager.singleton.hasChanged)
        {
            Debug.Log("entered the if statement");
            //@LAST COMMIT
            EventManager.singleton.ShowDynamicPopup("You have unsaved changes. Save?",
            "Yes", new UnityAction(() =>
            {
                // Save changes
                CloseMenu();
                SaveAction(action);
                // Do new action
            }), KeyCode.Y,
            "No", new UnityAction(() =>
            {
                // Do new action
                CloseMenu();
                action();
            }), KeyCode.N,
            "Cancel", new UnityAction(() =>
            {
                // Abort new action
                CloseMenu();
            }), KeyCode.C, 3, 3);
        }
        else
        {
            //EventManager.singleton.ReturnFocus();
            action();
        }
    }

    public void ResumeButtonPressed()
    {
        CloseMenu();
        //Hide();
    }


    public override void BackButtonResponse()
    {
        CloseMenu();
    }
    #endregion

    #region private methods

    private void SaveAction(UnityAction action)
    {
        if (MapManager.singleton.currentFileName != "")
        {
            MapManager.singleton.SaveCurrentMap();
            action();
        }
        else
        {
            //launch the save as dialog
            MapEditorManager.singleton.ShowSaveAsDialog(action);
        }
    }

    private static void CloseMenu()
    {
        MapEditorManager.singleton.SetBrushPanelVisibility(true);
        EventManager.singleton.ReturnFocus();
    }

    #endregion

    #region monobehaviors

    #endregion
}
