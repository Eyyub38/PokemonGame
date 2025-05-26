using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class BattleHud : MonoBehaviour{
    [SerializeField] Text nameText;
    [SerializeField] Text levelText;
    [SerializeField] HPBar hpBar;
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
    
    public void SetData(Pokemon pokemon){
        _pokemon = pokemon;

        nameText.text = pokemon.Base.Name;
        levelText.text ="Lvl." + pokemon.Level.ToString();
        hpBar.SetHP((float)pokemon.HP / (float)pokemon.MaxHp);

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

    public IEnumerator UpdateHP(){
        if(_pokemon.HpChanged){
            yield return StartCoroutine(hpBar.SetHPSmooth((float)_pokemon.HP / (float)_pokemon.MaxHp));
            _pokemon.HpChanged = false;
        }
    }
}
