using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

public class ColorPickerPopup : ModalPopup
{

    public class SwatchData
    {
        
        private float _value = 1;
        private Vector2 _coords;
        private Color _color;
        private Color _brightColor;

        public float value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
                _color = new Color(_brightColor.r * _value, _brightColor.g * _value, _brightColor.b * _value);
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
                Color tempColor = ColorPickerPopup.singleton.GetColorFromWheel(_coords);
                _brightColor = new Color(tempColor.r, tempColor.g, tempColor.b);
                tempColor *= _value;
                _color = new Color(tempColor.r, tempColor.g, tempColor.b);
            }
        }

        public Color color
        {
            get
            {
                return _color;
            }
        }

        public Color brightColor
        {
            get
            {
                return _brightColor;
            }
        }

        public SwatchData()
        {
            _coords = new Vector2(.5f, .5f);
            _value = 1f;
            _color = Color.white;
            _brightColor = Color.white;
        }

        public SwatchData(SwatchData copy)
        {
            _coords = copy._coords;
            _value = copy._value;
            _color = copy._color;
            _brightColor = copy._brightColor;
        }

        public SwatchData(SerializableSwatch copy)
        {
            _value = copy.v;
            coords = new Vector2(copy.x, copy.y);
        }
    }
    [Serializable] public class SerializableSwatch
    {
        public float x, y, v;

        public SerializableSwatch(SwatchData copy)
        {
            x = copy.coords.x;
            y = copy.coords.y;
            v = copy.value;
        }
    }
    //enums

    //subclasses

    //consts and static data
    public static ColorPickerPopup singleton;

    //public data

    public Image colorPicker;
    public Image colorShader;
    public Slider valueSlider;
    public Image reticle;
    public Image selectedSwatchBorder;

    //private data

    private SwatchData[] swatches;
    private Button[] swatchButtons;
    [SerializeField] 
    private Button swatchButton0, swatchButton1, swatchButton2, swatchButton3, swatchButton4, swatchButton5;
    [SerializeField] private Image valueSliderBackground;
    private int selectedSwatch;

    //public properties

    //methods
    #region public methods

#if UNITY_IOS || UNITY_ANDROID
    public override void TouchScreenUpdate()
    {
    }
#endif

    public override void BackButtonResponse()
    {
        PressedCancelButton();
    }

    public void PressedCancelButton()
    {
        EventManager.singleton.ReturnFocus();
    }

    public void PressedConfirmButton()
    {
        for(int ii = 0; ii < 6; ii++)
        {
            MapEditorManager.singleton.swatchData[ii] = swatches[ii];
        }
        MapEditorManager.singleton.SetSwatchButtonColors();
        EventManager.singleton.ReturnFocus();
        MapEditorManager.singleton.PressedSwatchButton(selectedSwatch);
        MapEditorManager.singleton.ResetToLastTool();
        WorldInterfaceLayer.singleton.SetBrushMode();
    }

    public void PressedColorPicker()
    {
        Vector2 mousePosition = Input.mousePosition;
        Vector2 texturePosition = ConvertToTexture(mousePosition);
        swatches[selectedSwatch].coords = ForceColorInbounds(texturePosition);
        UpdateButtonColor(selectedSwatch);
        UpdateValueSliderBackgroundColor();
        UpdateReticle();
    }

    private void UpdateButtonColor(int swatchNumber)
    {
        swatchButtons[swatchNumber].GetComponent<Image>().color = swatches[swatchNumber].color;
        if (swatchNumber == selectedSwatch)
            UpdateSwatchBorderColor();
    }

    private void UpdateSwatchBorderColor()
    {
        float intensity = 0f;
        if (swatches[selectedSwatch].value < .5f)
            intensity = 1f;
        selectedSwatchBorder.color = new Color (intensity, intensity, intensity);
    }

    private void UpdateValueSliderBackgroundColor()
    {
        valueSliderBackground.color = swatches[selectedSwatch].brightColor;
    }
    private void UpdateReticle()
    {
        Vector2 textureCoords = swatches[selectedSwatch].coords;
        Vector3[] worldCorners = new Vector3[4];
        colorPicker.rectTransform.GetWorldCorners(worldCorners);

        Vector2 bottomLeft = worldCorners[0];
        Vector2 topRight = worldCorners[2];

        float worldX = bottomLeft.x + textureCoords.x * (topRight.x - bottomLeft.x);
        float worldY = bottomLeft.y + textureCoords.y * (topRight.y - bottomLeft.y);

        reticle.transform.position = new Vector2(worldX, worldY);
    }

    private Vector2 ConvertToTexture(Vector2 mousePosition)
    {
        Vector3[] worldCorners = new Vector3[4];
        colorPicker.rectTransform.GetWorldCorners(worldCorners);

        Vector2 bottomLeft = worldCorners[0];
        Vector2 topRight = worldCorners[2];

        float textureX = (mousePosition.x - bottomLeft.x) / (topRight.x - bottomLeft.x);
        float textureY = (mousePosition.y - bottomLeft.y) / (topRight.y - bottomLeft.y);

        Vector2 texturePosition = new Vector2(textureX, textureY);
        Debug.Log(texturePosition);

        return texturePosition;
    }
    
    public Vector2 ForceColorInbounds(Vector2 original)
    {
        if(original.y >= 0.5f)
        { // we are in the upper half
            if (original.y <= -0.5f + (2f * original.x))
            { // we are in upper right
                if (original.y <= 2.5f - (2f * original.x))
                {
                    return original;
                }
                return PointToCenterIntersection(original, -2f, 2.5f);
            }
            else if (original.y >= 1.5f - (2f * original.x))
            { // we are in upper center
                if (original.y <= 1f)
                {
                    return original;
                }
                return PointToCenterIntersection(original, 0f, 1f);
            }
            else
            { // we are in upper left
                if (original.y <= 0.5f + (2f * original.x))
                {
                    return original;
                }
                return PointToCenterIntersection(original, 2f, 0.5f);
            }
        }
        else
        { // we are in the lower half
            if (original.y >= -0.5f + (2f * original.x))
            { // we are in lower left
                if (original.y >= 0.5f - (2f * original.x))
                {
                    return original;
                }
                return PointToCenterIntersection(original, -2f, 0.5f);
            }
            else if (original.y <= 1.5f - (2f * original.x))
            { // we are in lower center
                if (original.y >= 0f)
                {
                    return original;
                }
                return PointToCenterIntersection(original, 0f, 0f);
            }
            else
            { // we are in lower right
                if (original.y >= -1.5f + (2f * original.x))
                {
                    return original;
                }
                return PointToCenterIntersection(original, 2f, -1.5f);
            }
        }
    }

    // point is the point we're correcting towards center (0.5,0.5), slope and intercept are the properties of the line we're correcting onto
    public Vector2 PointToCenterIntersection(Vector2 point, float slope, float intercept)
    {
        if(point.x == 0.5f)
        {
            return new Vector2(0.5f, intercept);
            // GENERAL CASE which we probably don't need:
            // return new Vector2(0.5f, 0.5f * slope + intercept);
        }
        float slope2 = (point.y - 0.5f) / (point.x - 0.5f);
        float intercept2 = 0.5f - (0.5f * slope2);

        float intersectX = (intercept - intercept2) / (slope2 - slope);
        float intersectY = slope * intersectX + intercept;

        return new Vector2(intersectX, intersectY);
    }

    public void ValueSliderChanged()
    {
        float value = valueSlider.value;
        float shade = 1.0f - value;
        colorShader.color = new Color(0, 0, 0, shade);
        swatches[selectedSwatch].value = value;
        UpdateButtonColor(selectedSwatch);
    }

    public void SelectSwatch(int swatch)
    {
        selectedSwatch = swatch;
        UpdateReticle();
        valueSlider.value = swatches[selectedSwatch].value;
        selectedSwatchBorder.transform.position = swatchButtons[selectedSwatch].transform.position;
        UpdateValueSliderBackgroundColor();
        UpdateSwatchBorderColor();
    }

    public void InitializeValues(int swatch)
    {

    }

    public void InitializeAllSwatches()
    {
        for(int ii = 0; ii < 6; ii++)
        {
            swatches[ii] = new SwatchData(MapEditorManager.singleton.swatchData[ii]);
            UpdateButtonColor(ii);
        }
    }

    public Color GetColorFromWheel(Vector2 coords)
    {
        // TODO: sanity checking
        var tex2D = colorPicker.sprite.texture;
        Color pixelColor = tex2D.GetPixel((int)(coords.x * tex2D.width), (int)(coords.y * tex2D.height));
        Debug.Log("[ColorPickerPopup:GetColorFromWheel] pixelColor " + pixelColor);
        return pixelColor;
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
            for (int ii = 0; ii < 6; ++ii)
            {
                swatches[ii] = new SwatchData();
            }
            swatchButtons = new Button[6];
            swatchButtons[0] = swatchButton0;
            swatchButtons[1] = swatchButton1;
            swatchButtons[2] = swatchButton2;
            swatchButtons[3] = swatchButton3;
            swatchButtons[4] = swatchButton4;
            swatchButtons[5] = swatchButton5;
            selectedSwatch = 0;
            gameObject.SetActive(false);
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
