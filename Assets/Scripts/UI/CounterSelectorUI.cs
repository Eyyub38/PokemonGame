using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class CounterSelectorUI : MonoBehaviour{
    [SerializeField] Text counterText;
    [SerializeField] Text priceText; 

    [Header("Input")]
    [SerializeField] InputActionAsset actions;
    [SerializeField] string actionMapName = "UI";
    [SerializeField] string navigateName = "Navigate";
    [SerializeField] string selectName = "Select"; 
    
    InputAction navigate;
    InputAction select;
    
    bool selected;
    int currentCount;
    int maxCount;
    float pricePerUnit;
    
    float navTimer = 0f;
    const float navSpeed = 10f;

    void Awake() {
        if (actions == null) {
            Debug.LogError("CounterSelectorUI: actions not set");
            enabled = false;
            return;
        }

        var map = actions.FindActionMap(actionMapName, true);
        navigate = map.FindAction(navigateName, true);
        select   = map.FindAction(selectName, true);
    }
    void OnEnable() {
        navigate?.Enable();
        select?.Enable();
    }

    void OnDisable() {
        navigate?.Disable();
        select?.Disable();
    }
    
    public IEnumerator ShowSelector(int maxCount, float pricePerUnit, Action<int> onCountSelected){
        this.maxCount = maxCount;
        this.pricePerUnit = pricePerUnit;

        selected = false;
        currentCount = 1;
        
        gameObject.SetActive(true);
        SetValues();

        while(select.IsPressed()) {
            yield return null;
        }
        
        yield return new WaitUntil(() => selected);
        
        onCountSelected?.Invoke(currentCount);
        gameObject.SetActive(false);
        selected = false;
    }

    void Update() {
        if(!gameObject.activeInHierarchy) return;
        
        navTimer = Mathf.Clamp(navTimer - Time.deltaTime, 0f, navSpeed);

        int prevCount = currentCount;
        
        Vector2 navVector = navigate.ReadValue<Vector2>();
        navVector.y = Mathf.RoundToInt(navVector.y);

        if(navTimer == 0 || Mathf.Abs(navVector.y) > 0.2f) {
            currentCount += (int)Mathf.Sign(navVector.y);
            navTimer = 1f/navSpeed;
        }
        
        currentCount = Mathf.Clamp(currentCount, 1, maxCount);

        if(currentCount != prevCount) {
            SetValues();
        }

        if(select.WasPressedThisFrame()) {
            selected = true;
        }
    }

    void SetValues(){
        counterText.text = "x" + currentCount;
        priceText.text = "$" + (currentCount * pricePerUnit);
    }
}
