using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public interface Interactable{

    IEnumerator Interact(Transform initiator);
}
