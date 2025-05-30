using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

public class BattleHud : MonoBehaviour{
    [SerializeField] Text nameText;
    [SerializeField] Text levelText;
    [SerializeField] HPBar hpBar;
    [SerializeField] GameObject expBar;
    [SerializeField] Image genderIcon;
    [SerializeField] Sprite maleIcon;
    [SerializeField] Sprite femaleIcon;
    [SerializeField] Sprite genderlessIcon;
    [SerializeField] Image statusImage;

    [SerializeField] Sprite psnImage;
    [SerializeField] Sprite frzImage;
    [SerializeField] Sprite brnImage;
    [SerializeField] Sprite slpImage;
    [SerializeField] Sprite parImage;
    [SerializeField] Sprite toxImage;
    [SerializeField] Sprite froImage;

    Pokemon _pokemon;
    Dictionary<ConditionID, Sprite> statusImages;

    public Dictionary<ConditionID, Sprite> StatusImages {
        get { return statusImages; }
    }
    public Image StatusImage {
        get { return statusImage; }
    }
    
    public void SetData(Pokemon pokemon){
        _pokemon = pokemon;

        nameText.text = pokemon.Base.Name;
        SetLevel();
        hpBar.SetHP((float)pokemon.HP / (float)pokemon.MaxHp);
        SetExp();

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

        statusImages = new Dictionary<ConditionID, Sprite>(){
            { ConditionID.psn, psnImage },
            { ConditionID.frz, frzImage },
            { ConditionID.brn, brnImage },
            { ConditionID.slp, slpImage },
            { ConditionID.par, parImage },
            { ConditionID.tox, toxImage },
            { ConditionID.fro, froImage }
        };

        SetStatusImage();
        _pokemon.OnStatusChanged += SetStatusImage;
    }

    void SetStatusImage(){
        if(_pokemon.Status == null){
            statusImage.gameObject.SetActive(false);
        } else {
            statusImage.gameObject.SetActive(true);
            statusImage.sprite = statusImages[_pokemon.Status.Id];
        }
    }

    public void SetExp(){
        if(expBar == null) return;

        float normalizedExp = GetNormalizedExp();
        expBar.transform.localScale = new Vector3(normalizedExp, 1, 1);
    }
    
    public IEnumerator SetExpSmooth(bool reset = false){
        if(expBar == null) yield break;

        if(reset){
            expBar.transform.localScale = new Vector3( 0, 1, 1 );
        }
        float normalizedExp = GetNormalizedExp();
        yield return expBar.transform.DOScaleX(normalizedExp, 1.5f).WaitForCompletion();
    }

    public void SetLevel(){
        levelText.text = "Lvl." + _pokemon.Level;
    }

    public IEnumerator UpdateHP(){
        if(_pokemon.HpChanged){
            yield return StartCoroutine(hpBar.SetHPSmooth((float)_pokemon.HP / (float)_pokemon.MaxHp));
            _pokemon.HpChanged = false;
        }
    }

    float GetNormalizedExp(){
        int currLevelExp = _pokemon.Base.GetExpForLevel(_pokemon.Level);
        int nextLevelExp = _pokemon.Base.GetExpForLevel(_pokemon.Level + 1);

        float normilizedExp =  (float)( _pokemon.Exp - currLevelExp ) / (float)( nextLevelExp - currLevelExp);
        return Mathf.Clamp01(normilizedExp);
    }
}
