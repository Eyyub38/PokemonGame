using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using GDEUtills.GenerciSelectionUI;

public class InventoryUI : SelectionUI<TextSlot>{
    [Header("Item List")]
    [SerializeField] GameObject itemList;
    [SerializeField] ItemSlotUI itemSlotUI;

    [Header("Item Details")]
    [SerializeField] Text description;
    [SerializeField] Text itemTypeText;
    [SerializeField] Text priceText;

    [Header("Categories")]
    [SerializeField] Text categoryText;
    [SerializeField] Image prevCategoryImage;
    [SerializeField] Image currCategoryImage;
    [SerializeField] Image nextCategoryImage;
    [SerializeField] List<Sprite> categoryImages;

    [Header ("Arrows")]
    [SerializeField] Image upArrow;
    [SerializeField] Image downArrow;


    Inventory inventory;
    RectTransform itemListRect;

    List<ItemSlotUI> slotUIList;

    int selectedCategory = 0;

    const int itemsInViewport = 6;

    public ItemBase SelectedItem => inventory.GetItem(selectedItem, selectedCategory);
    public int SelectedCategory => selectedCategory;

    private void Awake(){
        inventory = Inventory.GetInventory();
        itemListRect = itemList.GetComponent<RectTransform>();
    }

    private void Start(){
        UpdateItemList();
        categoryText.text = Inventory.ItemCategories[selectedCategory];
        ChangeCategoryIcons(selectedCategory);
        inventory.OnUpdated += UpdateItemList;
    }

    void UpdateItemList(){
        foreach(Transform child in itemList.transform){
            Destroy(child.gameObject);
        }

        slotUIList = new List<ItemSlotUI>();
        foreach(var itemSlot in inventory.GetItemSlotsByCategory(selectedCategory)){
            var slotUIObj = Instantiate(itemSlotUI, itemList.transform);
            slotUIObj.SetData(itemSlot);

            slotUIList.Add(slotUIObj);
        }

        SetItems(slotUIList.Select(s => s.GetComponent<TextSlot>()).ToList());
        UpdateSelectionInUI();
    }

    public override void HandleUpdate(){
        int prevCategory = selectedCategory;

        if(Input.GetKeyDown(KeyCode.LeftArrow)){
            --selectedCategory;
        }
        else if(Input.GetKeyDown(KeyCode.RightArrow)){
            ++selectedCategory;
        }

        if(selectedCategory > Inventory.ItemCategories.Count - 1){
            selectedCategory = 0;
        } else if(selectedCategory < 0){
            selectedCategory = Inventory.ItemCategories.Count - 1;
        }

        if(prevCategory != selectedCategory){
            ResetSelection();
            categoryText.text = Inventory.ItemCategories[selectedCategory];
            ChangeCategoryIcons(selectedCategory);
            UpdateItemList();
        }
        base.HandleUpdate();
    }

    void ChangeCategoryIcons(int selectedCategory){
        currCategoryImage.sprite = categoryImages[selectedCategory];
        if(selectedCategory == 0){
            nextCategoryImage.sprite = categoryImages[selectedCategory + 1];
            prevCategoryImage.sprite = categoryImages[Inventory.ItemCategories.Count - 1];
        } else if(selectedCategory == Inventory.ItemCategories.Count - 1){
            prevCategoryImage.sprite = categoryImages[selectedCategory - 1];
            nextCategoryImage.sprite = categoryImages[0];
        } else {
            prevCategoryImage.sprite = categoryImages[selectedCategory - 1];            
            nextCategoryImage.sprite = categoryImages[selectedCategory + 1];
        }
    }

    void ResetSelection(){
        selectedItem = 0;
        upArrow.gameObject.SetActive(false);
        downArrow.gameObject.SetActive(false);

        description.text = "";
        itemTypeText.text = "";
        priceText.text = "";
    }

    public override void UpdateSelectionInUI(){
        base.UpdateSelectionInUI();

        var slots = inventory.GetItemSlotsByCategory(selectedCategory);
        selectedItem = Mathf.Clamp(selectedItem, 0, slots.Count - 1);
        
        if(slots.Count > 0){
            var item = slots[selectedItem].Item;
            description.text = item.Description;
            itemTypeText.text = item.ItemType.ToString();
            if(item.IsSellable){
                priceText.text = "$ " + item.Price.ToString();
            }
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
}
