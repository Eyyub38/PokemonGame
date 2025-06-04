using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class BattleHud : MonoBehaviour{
    [Header("Pokemon Details")]
    [SerializeField] Text nameText;
    [SerializeField] Text levelText;

    [Header("HP & XP Bar")]
    [SerializeField] HPBar hpBar;
    [SerializeField] GameObject expBar;

    [Header("Gender")]
    [SerializeField] Image genderIcon;
    [SerializeField] Sprite maleIcon;
    [SerializeField] Sprite femaleIcon;
    [SerializeField] Sprite genderlessIcon;

    [Header("Status")]
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
    
    public Dictionary<ConditionID, Sprite> StatusImages { get { return statusImages; } set { statusImages = value; }}
    
    public void SetData(Pokemon pokemon){
        ClearData();
        _pokemon = pokemon;

        nameText.text = pokemon.Base.Name;
        levelText.text ="Lvl." + pokemon.Level.ToString();

        hpBar.SetHP((float)pokemon.HP / pokemon.MaxHp);
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
        _pokemon.OnHpChanged += UpdateHp;
        _pokemon.OnExpChanged += UpdateExpBar;
    }

    public void ClearData(){
        if(_pokemon != null){
            _pokemon.OnStatusChanged -= SetStatusImage;
            _pokemon.OnHpChanged -= UpdateHp;
            _pokemon.OnExpChanged -= UpdateExpBar;
        }
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

    public IEnumerator UpdateHPAsync(){
        yield return StartCoroutine(hpBar.SetHPSmooth((float)_pokemon.HP / (float)_pokemon.MaxHp));
    }

    float GetNormalizedExp(){
        int currLevelExp = _pokemon.Base.GetExpForLevel(_pokemon.Level);
        int nextLevelExp = _pokemon.Base.GetExpForLevel(_pokemon.Level + 1);

        float normilizedExp =  (float)( _pokemon.Exp - currLevelExp ) / (float)( nextLevelExp - currLevelExp);
        return Mathf.Clamp01(normilizedExp);
    }

    private void UpdateHp(){
        StartCoroutine(UpdateHPAsync());
    }

    public IEnumerator WaitForHPUpdate(){
        yield return new WaitUntil(() => hpBar.IsUpdating == false);
    }

    private void UpdateExpBar(){
        if(gameObject.activeInHierarchy){
            StartCoroutine(SetExpSmooth());
        } else {
            SetExp();
        }
    }
}
