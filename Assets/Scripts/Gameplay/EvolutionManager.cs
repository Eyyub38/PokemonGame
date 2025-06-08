using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class EvolutionManager : MonoBehaviour{
    [SerializeField] GameObject evolutionUI;
    [SerializeField] Image evolutionImage;

    public event Action OnStartEvolution;
    public event Action OnCompleteEvolution;

    public static EvolutionManager i { get; private set; }

    private void Awake(){
        i = this;
    }

    public IEnumerator Evolve(Pokemon pokemon, Evolution evolution){
        OnStartEvolution?.Invoke();

        evolutionUI.SetActive(true);
        evolutionImage.sprite = pokemon.Base.FrontSprite;
        yield return DialogManager.i.ShowDialogText($"{pokemon.Base.Name} is evolving!");

        var oldPokemon = pokemon.Base;
        pokemon.Evolve(evolution);
        evolutionImage.sprite = evolution.EvolvesInto.FrontSprite;
        yield return DialogManager.i.ShowDialogText($"{oldPokemon.Name} evolved into {pokemon.Base.Name}!");

        evolutionUI.SetActive(false);
        OnCompleteEvolution?.Invoke();
    }
}
