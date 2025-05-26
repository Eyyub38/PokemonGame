using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class MoveBar : MonoBehaviour{
    [SerializeField] Image typeImage;
    [SerializeField] Text nameText;
    [SerializeField] Text ppText;

    public Image TypeImage => typeImage;
    public Text NameText => nameText;
    public Text PpText => ppText;
}
