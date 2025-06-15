using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class HPBar : MonoBehaviour{
    [SerializeField] GameObject health;

    public bool IsUpdating{ get; private set; }

    public void SetHP(float hpNormalized){
        health.transform.localScale = new Vector3( hpNormalized, 1f);
        health.GetComponent<Image>().color = GlobalSettings.i.HealthBarGradient.Evaluate(hpNormalized);
    }

    public IEnumerator SetHPSmooth(float newHP){
        IsUpdating = true;

        float currHp = health.transform.localScale.x;
        bool isDamaging = (currHp - newHP > 0 ) ? true : false;
        float changeAmt = currHp - newHP;

        while ((isDamaging) ? (currHp - newHP > Mathf.Epsilon) : (newHP - currHp < Mathf.Epsilon)){
            currHp -= changeAmt * Time.deltaTime;
            health.transform.localScale = new Vector3( currHp, 1f);
            health.GetComponent<Image>().color = GlobalSettings.i.HealthBarGradient.Evaluate(currHp);
            yield return null;
        }
        health.transform.localScale = new Vector3( newHP, 1f);
        health.GetComponent<Image>().color = GlobalSettings.i.HealthBarGradient.Evaluate(currHp);
        IsUpdating = false;
    }
}
