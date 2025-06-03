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

    void Awake(){
        rectTransform = GetComponent<RectTransform>();
    }

    public void SetData(ItemSlot itemSlot){
        nameText.text = itemSlot.Item.Name;
        countText.text = $"x {itemSlot.Count}";
        iconImage.sprite = itemSlot.Item.Icon;
    }
}
