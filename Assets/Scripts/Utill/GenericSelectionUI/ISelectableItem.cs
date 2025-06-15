using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public interface ISelectableItem{
    void Init();
    void Clear();
    void OnSelectionChanged(bool selected);
}
