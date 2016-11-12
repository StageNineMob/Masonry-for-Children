using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Events;

public class InformationPopup : UIEconomyTracker
{
    //enums

    //subclasses

    //consts and static data

    //public data

    //private data
    [SerializeField] private Text _infoText;
    [SerializeField] private Button _confirmButton;
    //method
    #region public methods

    public void Customize(string infoText, UnityAction confirmButtonAction)
    {
        _infoText.text = infoText;
        _confirmButton.onClick.RemoveAllListeners();
        _confirmButton.onClick.AddListener(confirmButtonAction);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
        _confirmButton.Select();
    }

    public void ShowCustom(string infoText, UnityAction confirmButtonAction)
    {
        Customize(infoText, confirmButtonAction);
        Show();
    }
    #endregion

    #region private methods

    #endregion

    #region monobehaviors

    protected override void Start()
    {
        base.Start();
        gameObject.SetActive(false);
    }

    #endregion
}
