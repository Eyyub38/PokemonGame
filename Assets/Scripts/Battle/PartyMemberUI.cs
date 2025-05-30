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

    [SerializeField] Sprite maleIcon;
    [SerializeField] Sprite femaleIcon;
    [SerializeField] Sprite genderlessIcon;
    [SerializeField] Image statusIcon;

    private Vector3 originalIconPosition;
    private Sequence bounceSequence;
    [SerializeField] float bounceHeight = 10f;
    [SerializeField] float bounceDuration = 0.5f;
    private bool isSelected = false;

    [SerializeField] BattleUnit currUnit;

    [SerializeField] BattleHud battleHud;
    Pokemon _pokemon;
    
    private void Start(){
        originalIconPosition = pokemonIcon.transform.localPosition;
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
            statusIcon.sprite = battleHud.StatusImages[pokemon.Status.Id];
        } else {
            statusIcon.gameObject.SetActive(false);

        }
        pokemonIcon.sprite = pokemon.Base.IconSprite;
        nameText.text = pokemon.Base.Name;
        levelText.text ="Lvl." + pokemon.Level.ToString();

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

        if(pokemon.Status != null && battleHud.StatusImages.ContainsKey(pokemon.Status.Id)){
            this.statusIcon.gameObject.SetActive(true);
            this.statusIcon.sprite = battleHud.StatusImages[pokemon.Status.Id];
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
