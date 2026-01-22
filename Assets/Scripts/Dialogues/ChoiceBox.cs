using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class ChoiceBox : MonoBehaviour{
    [SerializeField] ChoiceText choiceTextPrefab;
    [Header("Input")]
    [SerializeField] InputActionAsset actions;
    [SerializeField] string actionMapName = "UI";
    [SerializeField] string navigateName = "Navigate";
    [SerializeField] string selectName = "Select";
    
    InputAction navigate;
    InputAction select;
    
    bool choiceSelected = false;

    List<ChoiceText> choiceTexts;
    int currChoice;

    float navTimer = 0f;
    const float navSpeed = 8f;
    void Awake() {
        if(actions == null) {
                Debug.LogError("PlayerController: actions (InputActionAsset) not found!");
                enabled = false;
                return;
        }

        var map = actions.FindActionMap(actionMapName);
        navigate = map.FindAction(navigateName);
        select = map.FindAction(selectName);
    }

    void OnEnable() {
        navigate?.Enable();
        select?.Enable();
    }

    void OnDisable() {
        navigate?.Disable();
        select?.Disable();
    }
    
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

    private void Update() {
        if(!gameObject.activeInHierarchy) return;
        if(choiceTexts == null || choiceTexts.Count == 0) return;
        
        navTimer = Mathf.Clamp(navTimer - Time.deltaTime, 0, navTimer);
        
        Vector2 navVector = navigate.ReadValue<Vector2>();
        navVector.y = Mathf.RoundToInt(navVector.y);

        if(navTimer == 0 && Mathf.Abs(navVector.y) > 0.2f) {
            currChoice += -(int)Mathf.Sign(navVector.y);
            currChoice = Mathf.Clamp(currChoice, 0, choiceTexts.Count - 1);
            UpdateSelectionVisual();
            
            navTimer = 1 /  navSpeed;
        }

        if(select.WasPressedThisFrame()) {
            choiceSelected = true;
        }
    }

    void UpdateSelectionVisual() {
        for(int i = 0; i < choiceTexts.Count; i++) {
            choiceTexts[i].SetSelected( i ==  currChoice );
        }
    }
}
