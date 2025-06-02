using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System.Collections.Generic;

public class PartyMemberUI : MonoBehaviour{
    [SerializeField] Text nameText;
    [SerializeField] Text levelText;
    [SerializeField] HPBar hpBar;
    [SerializeField] Image genderIcon;
    [SerializeField] Image pokemonIcon;

    [SerializeField] Image details;
    [SerializeField] Image icon;

    [SerializeField] List<Sprite> detailBackground;
    [SerializeField] List<Sprite> iconBacgrounrd;

    [SerializeField] Image statusIcon;
    [SerializeField] float bounceHeight = 10f;
    [SerializeField] float bounceDuration = 0.5f;
    [SerializeField] BattleUnit currUnit;
    [SerializeField] BattleHud battleHud;

    private Vector3 originalIconPosition;
    private Sequence bounceSequence;
    private bool isSelected = false;

    Pokemon _pokemon;
    List<Sprite> genderIcons;
    List<Sprite> statusIcons;
    Dictionary<ConditionID, Sprite> statusImages;
    
    private void Start(){
        originalIconPosition = pokemonIcon.transform.localPosition;
        statusIcons = GlobalSettings.i.StatusIcons;
        genderIcons = GlobalSettings.i.GenderSprites;
        statusImages = GlobalSettings.i.StatusImages;
    }

    public void SetSelected(bool selected){
        if(isSelected == selected) return;
        
        isSelected = selected;
        
        if(selected){
            StartBounceAnimation();
        } else {
            StopBounceAnimation();
        }
    }
    
    public void SetData(Pokemon pokemon){
        _pokemon = pokemon;
        if(_pokemon.HP <= 0){
            details.sprite = detailBackground[1];
            icon.sprite = iconBacgrounrd[1];
        } else if(_pokemon == currUnit.Pokemon){
            details.sprite = detailBackground[0];
            icon.sprite = iconBacgrounrd[0];
        } else {
            details.sprite = detailBackground[2];
            icon.sprite = iconBacgrounrd[2];
        }

        if(pokemon.Status != null){
            statusIcon.gameObject.SetActive(true);
            statusIcon.sprite = statusImages[pokemon.Status.Id];
        } else {
            statusIcon.gameObject.SetActive(false);

        }
        pokemonIcon.sprite = pokemon.Base.IconSprite;
        nameText.text = pokemon.Base.Name;
        levelText.text ="Lvl." + pokemon.Level.ToString();

        if(pokemon.Base.IsGenderless){
            genderIcon.sprite = genderIcons[0];
            genderIcon.gameObject.SetActive(true);
        } else if(pokemon.Gender == Gender.Male){
            genderIcon.sprite = genderIcons[1];
            genderIcon.gameObject.SetActive(true);
        } else if(pokemon.Gender == Gender.Female){
            genderIcon.sprite = genderIcons[2];
            genderIcon.gameObject.SetActive(true);
        } else {
            genderIcon.gameObject.SetActive(false);
        }

        if(pokemon.Status != null && statusImages.ContainsKey(pokemon.Status.Id)){
            this.statusIcon.gameObject.SetActive(true);
            this.statusIcon.sprite = statusImages[pokemon.Status.Id];
        } else {
            this.statusIcon.gameObject.SetActive(false);
        }

        hpBar.SetHP((float)pokemon.HP / (float)pokemon.MaxHp);

        if(isSelected){
            StartBounceAnimation();
        }
    }

    private void StartBounceAnimation(){
        StopBounceAnimation();

        pokemonIcon.transform.localPosition = originalIconPosition;

        bounceSequence = DOTween.Sequence();
        bounceSequence.Append(pokemonIcon.transform.DOLocalMoveY(originalIconPosition.y + bounceHeight, bounceDuration / 2))
                     .Append(pokemonIcon.transform.DOLocalMoveY(originalIconPosition.y, bounceDuration / 2))
                     .SetLoops(-1)
                     .SetEase(Ease.InOutQuad);
    }

    private void StopBounceAnimation(){
        if (bounceSequence != null){
            bounceSequence.Kill();
            bounceSequence = null;
        }
        pokemonIcon.transform.localPosition = originalIconPosition;
    }

    private void OnDestroy(){
        StopBounceAnimation();
    }
}
