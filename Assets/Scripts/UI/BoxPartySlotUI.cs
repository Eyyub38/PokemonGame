using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class BoxPartySlotUI : MonoBehaviour{
    [SerializeField] Text nameText;
    [SerializeField] Text levelText;
    [SerializeField] Image pokemonIcon;
    [SerializeField] Image pokeballIcon;
    [SerializeField] Image gender;

    [SerializeField] Sprite maleIcon;
    [SerializeField] Sprite femaleIcon;
    [SerializeField] Sprite genderlessIcon;

    public void SetData(Pokemon pokemon){
        nameText.text = pokemon.Base.Name;
        levelText.text = "Lvl " + pokemon.Level;

        pokemonIcon.color = new Color(255, 255, 255, 100);
        pokemonIcon.sprite = pokemon.Base.IconSprite;

        pokeballIcon.color = new Color(255, 255, 255, 100);
        pokeballIcon.sprite = pokemon.Pokeball.Background;

        gender.color = new Color(255, 255, 255, 100);
        if(pokemon.Base.IsGenderless){
            gender.sprite = genderlessIcon;
            gender.gameObject.SetActive(true);
        } else if(pokemon.Gender == Gender.Male){
            gender.sprite = maleIcon;
            gender.gameObject.SetActive(true);
        } else if(pokemon.Gender == Gender.Female){
            gender.sprite = femaleIcon;
            gender.gameObject.SetActive(true);
        } else {
            gender.gameObject.SetActive(false);
        }
    }

    public void ClearData(){
        nameText.text = "";
        levelText.text = "";
        gender.sprite = null;
        gender.color = new Color(255, 255, 255, 0);
        pokemonIcon.sprite = null;
        pokemonIcon.color = new Color(255, 255, 255, 0);
        pokeballIcon.sprite = null;
        pokeballIcon.color = new Color(255, 255, 255, 0);
    }
}
