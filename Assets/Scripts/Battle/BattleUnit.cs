using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class BattleUnit : MonoBehaviour{
    [SerializeField] bool isPlayerUnit;
    [SerializeField] BattleHud hud;

    public Pokemon Pokemon{ get; set; }
    public BattleHud Hud{ get { return hud; } }
    Image image;
    Color originalColor;
    Vector3 originalPos;

    public bool IsPlayerUnit {get{return isPlayerUnit;}}

    private void Awake(){
        image = GetComponent<Image>();
        originalPos = image.transform.localPosition;
        originalColor = image.color;
    }

    public void Setup(Pokemon pokemon){
        Pokemon = pokemon;
        
        if(isPlayerUnit){
            if(pokemon.Base.HasGenderDifferences && pokemon.Gender == Gender.Female){
                image.sprite = Pokemon.Base.FemaleBackSprite;
            } else {
                image.sprite = Pokemon.Base.BackSprite;
            }
        } else {
            if(pokemon.Base.HasGenderDifferences && pokemon.Gender == Gender.Female){
                image.sprite = Pokemon.Base.FemaleFrontSprite;
            } else {
                image.sprite = Pokemon.Base.FrontSprite;
            }
        }

        hud.gameObject.SetActive(true);
        hud.SetData(pokemon);
        transform.localScale = new Vector3( 1, 1, 1);
        image.color = originalColor;
        PlayEnterAnimation();
    }

    public void Clear(){
        hud.gameObject.SetActive(false);
    }

    public void PlayEnterAnimation(){
        if(isPlayerUnit){
            image.transform.localPosition = new Vector3(-500, originalPos.y);
        } else {
            image.transform.localPosition = new Vector3(500, originalPos.y);
        }

        image.transform.DOLocalMoveX(originalPos.x,1f);
    }

    public void PlayAttackAnimation(){
        var sequence = DOTween.Sequence();
        if(isPlayerUnit){
            sequence.Append(image.transform.DOLocalMoveX(originalPos.x + 50f, 0.25f));
        } else {
            sequence.Append(image.transform.DOLocalMoveX(originalPos.x - 50f, 0.25f));
        }
        sequence.Append(image.transform.DOLocalMoveX(originalPos.x, 0.25f));
    }

    public void PlayHitAnimation(){
        var sequence = DOTween.Sequence();
        sequence.Append(image.DOColor(Color.gray,0.1f));
        sequence.Append(image.DOColor(originalColor,0.1f));
    }

    public void PlayFaintedAnimation(){
        var sequence = DOTween.Sequence();
        sequence.Append(image.transform.DOLocalMoveY(originalPos.y - 150f,0.5f));
        sequence.Join(image.DOFade(0f,0.5f));
    }

    public IEnumerator PlayCaptureAnimation() {
        var sequence = DOTween.Sequence();

        this.image.color = Color.red;
        sequence.Append(image.DOFade(0, 0.5f));
        sequence.Join(transform.DOLocalMoveY(originalPos.y + 50f, 0.5f));    
        sequence.Join(transform.DOScale(new Vector3(0.3f, 0.3f, 1f), 0.5f));
        yield return sequence.WaitForCompletion();
    }
    
    public IEnumerator PlayBreakAnimation() {
        var sequence = DOTween.Sequence();
        this.image.color = Color.red;
        sequence.Append(image.DOFade(1, 0.5f));
        sequence.Join(transform.DOLocalMoveY(originalPos.y, 0.5f));    
        sequence.Join(transform.DOScale(new Vector3(1f, 1f, 1f), 0.5f));
        yield return sequence.WaitForCompletion();
        this.image.color = Color.white;
    }
}
