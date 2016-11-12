using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseMenuPopup : ModalPopup
{
    [SerializeField] private OptionsMenuPanel OptionsMenu;
    [SerializeField] private Button saveGameButton;
    [SerializeField] private GameObject saveAsDialog;

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
        //if (touches.Length == 1)
        //{
        //    //panning or clicking on tiles
        //}
        //else 
        if (touches.Length == 2)
        {
            //zooming or something fancy
        }
    }
#endif
    public void QuitButtonPressed()
    {
        //TODO: Create specific UnityActions to be called here rather than new ones on the fly
        EventManager.singleton.ShowDynamicPopup("Really quit game in progress?",
            "Yes", new UnityAction(() => 
            {
                SceneManager.LoadScene(MainMenu.MAIN_MENU_SCENE);
            }), KeyCode.Y,
            "No", new UnityAction(() =>
            {
                EventManager.singleton.ReturnFocus();
            }), KeyCode.N, 2, 2);
    }

    public void ResumeButtonPressed()
    {
        CloseMenu();
    }

    public void OptionsButtonPressed()
    {
        OptionsMenu.PopulateFields();
        EventManager.singleton.GrantFocus(OptionsMenu.GetComponent<ModalPopup>());
    }

    public void SaveButtonPressed()
    {
        saveAsDialog.GetComponent<UISaveAsDialog>().saveAction = (string fileName) =>
        {
            CombatManager.singleton.SaveGame(fileName);
        };
        EventManager.singleton.GrantFocus(saveAsDialog.GetComponent<ModalPopup>());

        if (CombatManager.singleton.saveFileName == null)
        {
            saveAsDialog.GetComponent<UISaveAsDialog>().PopulateFileInfo(CombatManager.SAVE_DIRECTORY, CombatManager.SAVE_FILE_EXTENSION);
            //TODO: Generate a default file name automatically
        }
        else
        {
            saveAsDialog.GetComponent<UISaveAsDialog>().PopulateFileInfo(CombatManager.SAVE_DIRECTORY, CombatManager.SAVE_FILE_EXTENSION, CombatManager.singleton.saveFileName);
        }
    }

    public override void BackButtonResponse()
    {
        CloseMenu();
    }

    public override void Show(int height)
    {
        saveGameButton.interactable = CombatManager.singleton.GetGameState() == CombatManager.GameState.IDLE;
        base.Show(height);
    }

    private static void CloseMenu()
    {
        EventManager.singleton.ReturnFocus();
        CombatManager.singleton.LogEndOfPause("Game was paused");
    }

    
}
