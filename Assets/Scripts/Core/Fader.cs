using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class Fader : MonoBehaviour{
    Image image;

    public static Fader i {get; private set;}
    private void Awake(){
        i = this;
        image = GetComponent<Image>();
    }

    public IEnumerator FadeIn(float time){
        yield return image.DOFade( 1f, time).WaitForCompletion();
    }

    public IEnumerator FadeOut(float time){
        yield return image.DOFade( 0f, time).WaitForCompletion();
    }
}
