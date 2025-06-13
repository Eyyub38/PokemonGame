using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class IconSlot : MonoBehaviour, ISelectableItem{
    [SerializeField] Image icon;
    [SerializeField] float bounceHeight = 10f;
    [SerializeField] float bounceDuration = 0.5f;

    Vector3 originalIconPosition;
    Sequence bounceSequence;
    
    public void Init(){
        originalIconPosition = icon.transform.localPosition;
    }

    void StartBounceAnimation(){
        StopBounceAnimation();

        icon.transform.localPosition = originalIconPosition;

        bounceSequence = DOTween.Sequence();
        bounceSequence.Append(icon.transform.DOLocalMoveY(originalIconPosition.y + bounceHeight, bounceDuration / 2))
                         .Append(icon.transform.DOLocalMoveY(originalIconPosition.y, bounceDuration / 2))
                         .SetLoops(-1)
                         .SetEase(Ease.InOutQuad);
    }

    void StopBounceAnimation(){
        if (bounceSequence != null){
            bounceSequence.Kill();
            bounceSequence = null;
        }
        icon.transform.localPosition = originalIconPosition;
    }

    void OnDestroy(){
        StopBounceAnimation();
    }

    public void OnSelectionChanged(bool selected){
        if(selected){
            StartBounceAnimation();
        } else {
            StopBounceAnimation();
        }
    }
}
