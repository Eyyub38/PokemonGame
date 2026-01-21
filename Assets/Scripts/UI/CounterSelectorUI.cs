using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class CounterSelectorUI : MonoBehaviour{
    [SerializeField] Text counterText;
    [SerializeField] Text priceText; 

    bool selected;
    int currentCount;
    int maxCount;
    float pricePerUnit;

    public IEnumerator ShowSelector(int maxCount, float pricePerUnit, Action<int> onCountSelected){
        this.maxCount = maxCount;
        this.pricePerUnit = pricePerUnit;

        selected = false;
        currentCount = 1;
        
        gameObject.SetActive(true);
        SetValues();

        yield return new WaitUntil(() => selected == true);

        onCountSelected?.Invoke(currentCount);
        gameObject.SetActive(false);
        selected = false;
    }

    void Update(){
        int prevCount = currentCount;

        if(Keyboard.current.upArrowKey.isPressed){
            ++currentCount;
        } else if(Keyboard.current.downArrowKey.isPressed){
            --currentCount;
        }
        currentCount = Mathf.Clamp(currentCount, 1, maxCount);

        if(currentCount != prevCount){
            SetValues();
        }

        if(Keyboard.current.enterKey.isPressed){
            selected = true;
        }
    }

    void SetValues(){
        counterText.text = "x" + currentCount;
        priceText.text = "$" + (currentCount * pricePerUnit);
    }
}
