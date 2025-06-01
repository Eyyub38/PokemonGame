using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class Fader : MonoBehaviour{
    Image image;
    private void Awake(){
        image = GetComponent<Image>();
    }

    public IEnumerator FadeIn(float time){
        yield return image.DOFade( 1f, time).WaitForCompletion();
    }

    public IEnumerator FadeOut(float time){
        yield return image.DOFade( 0f, time).WaitForCompletion();
    }
}
