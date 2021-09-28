using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIHandler : MonoBehaviour
{
    public Dropdown modeSelector;
    public Button startButton;
    [HideInInspector] public Toggle saveInsertionButton;
    [HideInInspector] public Slider insertSpeed;
    // public Text textInsertSpeed;
    [HideInInspector] public Slider critPoint;
    // public Text textCritPoint;
    [HideInInspector] public Slider curlingMod;
    // public Text textCurlingMod;
    [HideInInspector] public Slider insertionInterp;
    // public Text textInsertionInterp;

    [HideInInspector] public int currentMode;
    [HideInInspector] public bool startButtonPress;

    // private string sliderMessage;

    private void Start(){

        startButtonPress = false;


        modeSelector.onValueChanged.AddListener(delegate
        {
            ModeChange(modeSelector);
        });

        startButton.onClick.AddListener(delegate
        {
            InsertionStarter();
        });


    }

    public void ModeChange(Dropdown sender){
        currentMode = sender.value;
    }

    public void InsertionStarter(){
        startButtonPress = true;
        startButton.interactable = false;
        modeSelector.interactable = false;

    }

    public void saveSession(){

    }

    // public void ShowSliderVals(){
    //     sliderMessage = insertSpeed.value.ToString() + " mm/s";
    //     textInsertSpeed.text = sliderMessage;

    //     sliderMessage = critPoint.value.ToString();
    //     textCritPoint.text = sliderMessage;

    //     sliderMessage = curlingMod.value.ToString();
    //     textCurlingMod.text = sliderMessage;

    //     sliderMessage = insertionInterp.value.ToString();
    //     textInsertionInterp.text = sliderMessage;

    // }

}
