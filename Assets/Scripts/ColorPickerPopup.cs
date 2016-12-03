using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

public class ColorPickerPopup : ModalPopup
{

    public class SwatchData
    {
        private float _value;
        private Vector2 _coords;
        private Color _color;

        public float value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
                _color = ColorPickerPopup.singleton.GetColorFromWheel(_coords) * _value;
            }
        }

        public Vector2 coords
        {
            get
            {
                return _coords;
            }
            set
            {
                _coords = value;
                _color = ColorPickerPopup.singleton.GetColorFromWheel(_coords) * _value;
            }
        }

        public Color color
        {
            get
            {
                return _color;
            }
        }
    }
    //enums

    //subclasses

    //consts and static data
    public static ColorPickerPopup singleton;

    //public data

    public Image colorPicker;

    //private data

    private SwatchData[] swatches;
    private Button[] swatchButtons;
    [SerializeField] 
    private Button swatchButton0, swatchButton1, swatchButton2, swatchButton3, swatchButton4, swatchButton5;
    private int selectedSwatch;

    //public properties

    //methods
    #region public methods
    public override void BackButtonResponse()
    {
        PressedCancelButton();
    }

    public void PressedCancelButton()
    {
        EventManager.singleton.ReturnFocus();
        // TODO: TEST THIS
    }

    public void PressedConfirmButton()
    {

    }

    public void PressedColorPicker()
    {
        //TODO: this
    }

    public void ValueSliderChanged()
    {

    }

    public void SwatchSelected(int swatch)
    {

    }

    public void InitializeValues(int swatch)
    {

    }

    public Color GetColorFromWheel(Vector2 coords)
    {
        // TODO: sanity checking
        var tex2D = colorPicker.sprite.texture;
        return tex2D.GetPixel((int)(coords.x * tex2D.width), (int)(coords.y * tex2D.height));
    }
    #endregion

    #region private methods
    #endregion

    #region monobehaviors
    void Awake()
    {
        Debug.Log("[ColorPickerPopup:Awake]");
        if (singleton == null)
        {
            Debug.Log("ColorPickerPopup checking in.");
            singleton = this;
            swatches = new SwatchData[6];
            swatchButtons = new Button[6];
            swatchButtons[0] = swatchButton0;
            swatchButtons[1] = swatchButton1;
            swatchButtons[2] = swatchButton2;
            swatchButtons[3] = swatchButton3;
            swatchButtons[4] = swatchButton4;
            swatchButtons[5] = swatchButton5;
        }
        else
        {
            Debug.Log("ColorPickerPopup checking out.");
            GameObject.Destroy(gameObject);
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
    #endregion

}
