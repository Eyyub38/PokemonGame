using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class GlobalSettings : MonoBehaviour{
    [SerializeField] Color highlightedColor;
    [SerializeField] List<Sprite> genderSprites;
    [SerializeField] List<Sprite> categories = new List<Sprite>();
    [SerializeField] List<Sprite> typeBarSprites = new List<Sprite>();
    [SerializeField] List<Sprite> statusIcons = new List<Sprite>();
    [SerializeField] Sprite empty;

    Dictionary<ConditionID, Sprite> statusImages;

    public Dictionary<ConditionID, Sprite> StatusImages { get { return statusImages; } set { statusImages = value; }}
    public Color HighlightedColor => highlightedColor;
    public List<Sprite> GenderSprites => genderSprites;
    public List<Sprite> Categories => categories;
    public List<Sprite> TypeBarSprites => typeBarSprites;
    public Sprite Empty => empty;
    public List<Sprite> StatusIcons => statusIcons;

    public static GlobalSettings i {get; private set;}

    private void Awake(){
        i = this;
    }
}