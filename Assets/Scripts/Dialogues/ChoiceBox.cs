using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class ChoiceBox : MonoBehaviour{
    [SerializeField] ChoiceText choiceTextPrefab;

    bool choiceSelected = false;

    List<ChoiceText> choiceTexts;
    int currChoice;

    public IEnumerator ShowChoices(List<string> choices, Action<int> onChoiceSelected){
        choiceSelected = false;
        currChoice = 0;

        gameObject.SetActive(true);

        foreach(Transform child in transform){
            Destroy(child.gameObject);
        }

        choiceTexts = new List<ChoiceText>();
        foreach(var choice in choices){
            var choiceTextObj = Instantiate(choiceTextPrefab, transform);
            choiceTextObj.TextField.text = choice;
            choiceTexts.Add(choiceTextObj);
        }
        
        yield return new WaitUntil(() => choiceSelected == true);
        onChoiceSelected?.Invoke(currChoice);
        gameObject.SetActive(false);
    }

    private void Update(){
        if(Keyboard.current.upArrowKey.isPressed){
            --currChoice;
        } else if(Keyboard.current.downArrowKey.isPressed){
            ++currChoice;
        }

        currChoice = Mathf.Clamp(currChoice, 0, choiceTexts.Count - 1);

        for(int i = 0; i < choiceTexts.Count; ++i){
            choiceTexts[i].SetSelected(i == currChoice);
        }

        if(Keyboard.current.enterKey.isPressed){
            choiceSelected = true;
        }
    }   
}
