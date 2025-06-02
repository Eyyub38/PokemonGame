using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class BattleHud : MonoBehaviour{
    [SerializeField] Text nameText;
    [SerializeField] Text levelText;
    [SerializeField] HPBar hpBar;
    [SerializeField] GameObject expBar;
    [SerializeField] Image genderIcon;
    [SerializeField] Image statusImage;

    Pokemon _pokemon;
    List<Sprite> genderIcons;
    List<Sprite> statusIcons;

    public Image StatusImage {
        get { return statusImage; }
    }

    private void Start(){
        genderIcons = GlobalSettings.i.GenderSprites;
        statusIcons = GlobalSettings.i.StatusIcons;
    }
    
    public void SetData(Pokemon pokemon){
        _pokemon = pokemon;

        nameText.text = pokemon.Base.Name;
        SetLevel();
        hpBar.SetHP((float)pokemon.HP / (float)pokemon.MaxHp);
        SetExp();

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

        GlobalSettings.i.StatusImages = new Dictionary<ConditionID, Sprite>(){
            { ConditionID.psn, statusIcons[0] },
            { ConditionID.frz, statusIcons[1] },
            { ConditionID.brn, statusIcons[2] },
            { ConditionID.slp, statusIcons[3] },
            { ConditionID.par, statusIcons[4] },
            { ConditionID.tox, statusIcons[5] },
            { ConditionID.fro, statusIcons[6] }
        };

        SetStatusImage();
        _pokemon.OnStatusChanged += SetStatusImage;
    }

    void SetStatusImage(){
        if(_pokemon.Status == null){
            statusImage.gameObject.SetActive(false);
        } else {
            statusImage.gameObject.SetActive(true);
            statusImage.sprite = GlobalSettings.i.StatusImages[_pokemon.Status.Id];
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
