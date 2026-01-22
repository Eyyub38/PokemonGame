using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class DialogManager : MonoBehaviour{
    [SerializeField] GameObject dialogBox;
    [SerializeField] ChoiceBox choiceBox;
    [SerializeField] Text dialogText;
    [SerializeField] int letterPerSecond = 10;

    [Header("Input")]
    [SerializeField] InputActionAsset actions;
    [SerializeField] string actionMapName = "UI";
    [SerializeField] string selectName = "Select";
    
    InputAction select;

    [Header("Fast Forward")] 
    [SerializeField]  int fastMultiplier = 8;
    bool skipTyping = false;
    
    public event Action OnShowDialog;
    public event Action OnDialogFinished;

    public static DialogManager i{ get; private set;}
    public bool IsShowing { get; private set;}

    void Awake(){
        i = this;

        if(actions == null) {
            Debug.LogError("DialogManager: actions not set");
            enabled = false;
            return;
        }

        var map = actions.FindActionMap(actionMapName);
        select = map.FindAction(selectName);
    }

    void OnEnable() {
        select?.Enable();
    }

    void OnDisable() {
        select?.Disable();
    }
    
    public IEnumerator ShowDialog(Dialog dialog, List<string> choices = null, Action<int> onChoiceSelected = null){
        yield return new WaitForEndOfFrame();

        OnShowDialog?.Invoke();
        IsShowing = true;
        dialogBox.SetActive(true);

        foreach(var line in dialog.Lines){
            AudioManager.i.PlaySfx(AudioId.UISelecet);
            yield return TypeDialog(line);
            yield return WaitForAdvance();
        }

        if(choices != null && choices.Count > 1){
            yield return choiceBox.ShowChoices(choices, onChoiceSelected);
        }

        dialogBox.SetActive(false);
        IsShowing = false;
        OnDialogFinished?.Invoke();
    }

    IEnumerator WaitForSelectPress() {
        while(select.IsPressed()) {
            yield return null;
        }

        while(!select.IsPressed()) {
            yield return null;
        }
    }

    public IEnumerator ShowDialogText(string text, bool waitForInput = true, bool autoClose = true, List<string> choices = null, Action<int> onChoiceSelected = null){
        OnShowDialog?.Invoke();
        IsShowing = true;
        dialogBox.SetActive(true);
        AudioManager.i.PlaySfx(AudioId.UISelecet);
        yield return TypeDialog(text);
        
        if(waitForInput){
            yield return WaitForAdvance();
        }

        if(choices != null && choices.Count > 1){
            yield return choiceBox.ShowChoices(choices, onChoiceSelected);
        }
        
        if(autoClose){
            CloseDialog();
        }
        OnDialogFinished?.Invoke();
    }

    public void CloseDialog(){
        dialogBox.SetActive(false);
        IsShowing = false;
        //OnDialogFinished?.Invoke(); <- Commented out to prevent double invocation
    }

    public void HandleUpdate(){}

    public IEnumerator TypeDialog(string line){
        dialogText.text = "";
        skipTyping = false;
        foreach (var letter in line){
            if(select.WasPressedThisFrame()) {
                skipTyping = true;
            }

            if(skipTyping) {
                dialogText.text = line;
                yield break;
            }
            
            dialogText.text += letter;
            float speed = letterPerSecond;
            if(select.IsPressed()) {
                speed *= fastMultiplier;
            }
            yield return new WaitForSeconds( 1f / speed);
        }
    }

    IEnumerator WaitForAdvance() {
        while(select.IsPressed()) {
            yield return null;
        }
        while(!select.IsPressed()) {
            yield return null;
        }
    }
}
