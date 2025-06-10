using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class ItemSlotUI : MonoBehaviour{
    [SerializeField] Text nameText;
    [SerializeField] Text countText;
    [SerializeField] Image iconImage;

    public Text NameText => nameText;
    public Text CountText => countText;
    public float Height => rectTransform.rect.height;

    RectTransform rectTransform;

    public void SetData(ItemSlot itemSlot){
        rectTransform = GetComponent<RectTransform>();

        nameText.text = itemSlot.Item.Name;
        countText.text = $"x {itemSlot.Count}";
        iconImage.sprite = itemSlot.Item.Icon;
    }

    public void SetNameAndPrice(ItemBase item){
        rectTransform = GetComponent<RectTransform>();

        nameText.text = item.Name;
        countText.text = $"$ {item.Price}";
        iconImage.sprite = item.Icon;
    }
}
