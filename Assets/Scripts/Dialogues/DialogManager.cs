using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class DialogManager : MonoBehaviour{
    [SerializeField] GameObject dialogBox;
    [SerializeField] ChoiceBox choiceBox;
    [SerializeField] Text dialogText;
    [SerializeField] int letterPerSecond = 10;

    public event Action OnShowDialog;
    public event Action OnDialogFinished;

    public static DialogManager i{ get; private set;}
    public bool IsShowing { get; private set;}

    void Awake(){
        i = this;
    }

    public IEnumerator ShowDialog(Dialog dialog, List<string> choices = null, Action<int> onChoiceSelected = null){
        yield return new WaitForEndOfFrame();

        OnShowDialog?.Invoke();
        IsShowing = true;
        dialogBox.SetActive(true);

        foreach(var line in dialog.Lines){
            yield return TypeDialog(line);
            yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Return));
        }

        if(choices != null && choices.Count > 1){
            yield return choiceBox.ShowChoices(choices, onChoiceSelected);
        }

        dialogBox.SetActive(false);
        IsShowing = false;
        OnDialogFinished?.Invoke();
    }

    public IEnumerator ShowDialogText(string text, bool waitForInput = true, bool autoClose = true){
        OnShowDialog?.Invoke();
        IsShowing = true;
        dialogBox.SetActive(true);
        yield return TypeDialog(text);

        if(waitForInput){
            yield return new WaitUntil( () => Input.GetKeyDown(KeyCode.Return));
        }
        if(autoClose){
            CloseDialog();
        }
        OnDialogFinished?.Invoke();
    }

    public void CloseDialog(){
        dialogBox.SetActive(false);
        IsShowing = false;
        OnDialogFinished?.Invoke();
    }

    public void HandleUpdate(){}

    public IEnumerator TypeDialog(string line){
        dialogText.text = "";
        foreach (var letter in line.ToCharArray()){
            dialogText.text += letter;
            yield return new WaitForSeconds( 1f / letterPerSecond);
        }
    }
}
