using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class DialogManager : MonoBehaviour{
    [SerializeField] GameObject dialogBox;
    [SerializeField] Text dialogText;
    [SerializeField] int letterPerSecond = 10;

    public event Action OnShowDialog;
    public event Action OnCloseDialog;
    Action onDialogFinished;

    int currentLine = 0;
    bool isTyping = false;
    Dialog dialog;

    public static DialogManager i{ get; private set;}
    public bool IsShowing { get; private set;}

    void Awake(){
        i = this;
    }

    public IEnumerator ShowDialog(Dialog dialog, Action onFinished = null){
        yield return new WaitForEndOfFrame();
        OnShowDialog?.Invoke();

        IsShowing = true;
        this.dialog = dialog;
        onDialogFinished = onFinished;
        
        dialogBox.SetActive(true);
        StartCoroutine(TypeDialog(dialog.Lines[0]));
    }

    public IEnumerator ShowDialogText(string text, bool waitForInput = true, bool autoClose = true){
        IsShowing = true;
        dialogBox.SetActive(true);
        yield return TypeDialog(text);

        if(waitForInput){
            yield return new WaitUntil( () => Input.GetKeyDown(KeyCode.Return));
        }
        if(autoClose){
            CloseDialog();
        }
    }

    public void CloseDialog(){
        dialogBox.SetActive(false);
        IsShowing = false;
    }

    public void HandleUpdate(){
        if(Input.GetKeyDown(KeyCode.Return) && !isTyping){
            ++currentLine;
            if(currentLine < dialog.Lines.Count){
                StartCoroutine(TypeDialog(dialog.Lines[currentLine]));
            } else {
                currentLine = 0;
                IsShowing = false;
                dialogBox.SetActive(false);
                onDialogFinished?.Invoke();
                OnCloseDialog?.Invoke();
            }
        }
    }

    public IEnumerator TypeDialog(string line){
        isTyping = true;
        dialogText.text = "";
        foreach (var letter in line.ToCharArray()){
            dialogText.text += letter;
            yield return new WaitForSeconds( 1f / letterPerSecond);
        }
        isTyping = false;
    }
}
