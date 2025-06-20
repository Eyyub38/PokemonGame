using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class PartyMemberUI : MonoBehaviour{
    [Header("Pokemon Details")]
    [SerializeField] Text nameText;
    [SerializeField] Text levelText;

    [Header("HP&XP Bar")]
    [SerializeField] HPBar hpBar;
    [SerializeField] GameObject xpBar;

    [Header("Pokemon Gender")]
    [SerializeField] Image genderIcon;
    
    [Header("Pokemon Icon")]
    [SerializeField] Image pokemonIcon;

    [Header("Pokemon Detail Background")]
    [SerializeField] Image details;
    [SerializeField] List<Sprite> detailBackground;

    [Header("Pokemon Icon Background")]
    [SerializeField] Image icon;
    [SerializeField] List<Sprite> iconBacgrounrd;

    [Header("Gender Icons")]
    [SerializeField] Sprite maleIcon;
    [SerializeField] Sprite femaleIcon;
    [SerializeField] Sprite genderlessIcon;

    [Header("Status Icon")]
    [SerializeField] Image statusIcon;

    [Header("Able/Not Able Text")]
    [SerializeField] Text messageText;

    [Header("Battle Unit")]
    [SerializeField] BattleUnit currUnit;
    [SerializeField] BattleHud battleHud;

    Pokemon _pokemon;

    
    public void SetData(Pokemon pokemon){
        _pokemon = pokemon;
        SetMessageText("");
        UpdateData();

        _pokemon.OnHpChanged += UpdateData;
        _pokemon.OnExpChanged += UpdateData;
        _pokemon.OnStatusChanged += UpdateData;

        var prevState = GameController.i.StateMachine.GetPrevState();
        if(prevState == InventoryState.i){
            if(_pokemon.HP <= 0){
                details.sprite = detailBackground[1];
                icon.color = Color.gray;
            } else {
                details.sprite = detailBackground[2];
            }
        } else {
            if(_pokemon.HP <= 0){
                details.sprite = detailBackground[1];
                icon.color = Color.gray;
            } else if(_pokemon == currUnit?.Pokemon){
               details.sprite = detailBackground[0];
            } else {
                details.sprite = detailBackground[2];
            }
        }

        if(_pokemon.Pokeball != null){
            icon.sprite = _pokemon.Pokeball.Background;
        } else {
            icon.sprite = iconBacgrounrd[0];   
        }

        if(pokemon.Base.IsGenderless){
            genderIcon.sprite = genderlessIcon;
            genderIcon.gameObject.SetActive(true);
        } else if(pokemon.Gender == Gender.Male){
            genderIcon.sprite = maleIcon;
            genderIcon.gameObject.SetActive(true);
        } else if(pokemon.Gender == Gender.Female){
            genderIcon.sprite = femaleIcon;
            genderIcon.gameObject.SetActive(true);
        } else {
            genderIcon.gameObject.SetActive(false);
        }
    }

    private void UpdateData(){
        pokemonIcon.sprite = _pokemon.Base.IconSprite;
        nameText.text = _pokemon.Base.Name;
        levelText.text ="Lvl." + _pokemon.Level.ToString();
        
        hpBar.SetHP((float)_pokemon.HP / _pokemon.MaxHp);
        float normalizedExp = GetNormalizedExp(_pokemon);
        xpBar.transform.localScale = new Vector3(normalizedExp, 1, 1);

        if(_pokemon.Status != null){
            statusIcon.gameObject.SetActive(true);
            statusIcon.sprite = battleHud.StatusImages[_pokemon.Status.Id];
        } else {
            statusIcon.gameObject.SetActive(false);
        }

        if(_pokemon.Status != null && battleHud.StatusImages[_pokemon.Status.Id] != null){
            this.statusIcon.gameObject.SetActive(true);
            this.statusIcon.sprite = battleHud.StatusImages[_pokemon.Status.Id];
        } else {
            this.statusIcon.gameObject.SetActive(false);
        }
    }

    private float GetNormalizedExp(Pokemon pokemon){
        int currLevelExp = pokemon.Base.GetExpForLevel(pokemon.Level);
        int nextLevelExp = pokemon.Base.GetExpForLevel(pokemon.Level + 1);

        float normalizedExp = (float)(pokemon.Exp - currLevelExp) / (float)(nextLevelExp - currLevelExp);
        return Mathf.Clamp01(normalizedExp);
    }

    public void SetMessageText(string message){
        messageText.text = message;
    }
}
