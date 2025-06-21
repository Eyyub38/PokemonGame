using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class ShopUI : MonoBehaviour{
    [Header("Item List")]
    [SerializeField] GameObject itemList;
    [SerializeField] ItemSlotUI itemSlotUI;

    [Header("Item Details")]
    [SerializeField] Text description;
    [SerializeField] Text itemTypeText;
    
    [Header ("Arrows")]
    [SerializeField] Image upArrow;
    [SerializeField] Image downArrow;

    List<ItemBase> avaliableItems;
    List<ItemSlotUI> slotUIList;
    RectTransform itemListRect;
    Action<ItemBase> onItemSelected;
    Action onBack;
    int selectedItem = 0;
    int  itemsInViewport = 6;

    void Awake(){
        itemListRect = itemList.GetComponent<RectTransform>();
    }

    public void Show(List<ItemBase> avaliableItems, Action<ItemBase> onItemSelected, Action onBack){
        this.onItemSelected = onItemSelected;
        this.onBack = onBack;
        this.avaliableItems = avaliableItems;
        gameObject.SetActive(true);
        UpdateItemList();
    }

    public void Close(){
        gameObject.SetActive(false);
    }

    void UpdateItemList(){
        foreach(Transform child in itemList.transform){
            Destroy(child.gameObject);
        }

        slotUIList = new List<ItemSlotUI>();
        foreach(var item in avaliableItems){
            var slotUIObj = Instantiate(itemSlotUI, itemList.transform);
            slotUIObj.SetNameAndPrice(item);

            slotUIList.Add(slotUIObj);
        }

        UpdateItemSelection();
    }

    void UpdateItemSelection(){
        selectedItem = Mathf.Clamp(selectedItem, 0, avaliableItems.Count - 1);

        for(int i = 0; i < slotUIList.Count; ++i){
            if(i == selectedItem){
                slotUIList[i].NameText.color = GlobalSettings.i.HighlightedColor;
                slotUIList[i].CountText.color = GlobalSettings.i.HighlightedColor;
            } else {
                slotUIList[i].NameText.color = Color.white;
                slotUIList[i].CountText.color = Color.white;
            }
        }
        if(avaliableItems.Count > 0){
            var item = avaliableItems[selectedItem];
            description.text = item.Description;
            itemTypeText.text = item.ItemType.ToString();
        }
        HandleScrolling();
    }

    void HandleScrolling(){
        if(slotUIList.Count <= itemsInViewport){
            return;
        }
        var scrollPos = Mathf.Clamp(selectedItem - itemsInViewport / 2, 0, selectedItem) * slotUIList[0].Height;
        itemListRect.localPosition = new Vector2(itemListRect.localPosition.x, scrollPos);

        bool showUpArrow = selectedItem > itemsInViewport / 2;
        upArrow.gameObject.SetActive(showUpArrow);

        bool showDownArrow = selectedItem < slotUIList.Count - itemsInViewport / 2;
        downArrow.gameObject.SetActive(showDownArrow);
    }

    public void HandleUpdate(){
        var prevSelection = selectedItem;

        if(Input.GetKeyDown(KeyCode.DownArrow)){
            ++selectedItem;
        } else if(Input.GetKeyDown(KeyCode.UpArrow)){
            --selectedItem;
        }

        selectedItem = Mathf.Clamp(selectedItem, 0, avaliableItems.Count - 1);

        if(prevSelection != selectedItem){
            UpdateItemSelection();
        }

        if(Input.GetKeyDown(KeyCode.Return)){
            onItemSelected?.Invoke(avaliableItems[selectedItem]);
        } else if(Input.GetKeyDown(KeyCode.Escape)){
            onBack?.Invoke();
        }
    }
}
