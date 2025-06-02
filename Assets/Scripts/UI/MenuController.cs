using System;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class MenuController : MonoBehaviour{
    [SerializeField] GameObject menu;

    Color highlightedColor;
    List<Text> menuItems;
    int selectedItem = 0;

    public event Action<int> onMenuSelected;
    public event Action onBack;

    private void Start(){
        highlightedColor = GlobalSettings.i.HighlightedColor;
    }

    private void Awake(){
        menuItems = menu.GetComponentsInChildren<Text>().ToList();
    }

    public void OpenMenu(){
        menu.SetActive(true);
        UpdateItemSelection();
    }

    public void CloseMenu(){
        menu.SetActive(false);
    }

    public void HandleUpdate(){
        int prevSelection = selectedItem;

        if(Input.GetKeyDown(KeyCode.DownArrow)){
            ++selectedItem;
        } else if(Input.GetKeyDown(KeyCode.UpArrow)){
            --selectedItem;
        }

        selectedItem = Mathf.Clamp(selectedItem, 0, menuItems.Count - 1);
        if(prevSelection != selectedItem){
            UpdateItemSelection();
        }

        if(Input.GetKeyDown(KeyCode.Return)){
            onMenuSelected?.Invoke(selectedItem);
            CloseMenu();
        }
        if(Input.GetKeyDown(KeyCode.Escape)){
            onBack?.Invoke();
            CloseMenu();
        }
    }

    public void UpdateItemSelection(){
        for(int i = 0; i < menuItems.Count; i++){
            if(i == selectedItem){
                menuItems[i].color = highlightedColor;
            } else {
                menuItems[i].color = Color.black;
            }
        }   
    }
}
