using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;

namespace  GDEUtills.GenerciSelectionUI {
    public enum SelectionType {Grid, List}
    public class SelectionUI<T> : MonoBehaviour where T: ISelectableItem{
        List<T> items;

        SelectionType selectionType = SelectionType.List;
        
        float selectionTimer = 0;
        int gridWith = 2;
        protected int selectedItem = 0;
        const float selectionSpeed = 5;

        public event Action<int> OnSelected;
        public event Action OnBack;

        public void SetItems(List<T> items){
            this.items = items;
            items.ForEach(i => i.Init());
            UpdateSelectionInUI();
        }

        public void ClearItems(){
            items?.ForEach(i => i.Clear());
            
            this.items = null;
        }

        public void SetSelectionSettings(SelectionType selectionType, int gridWith){
            this.selectionType = selectionType;
            this.gridWith = gridWith;
        }

        public virtual void HandleUpdate(){
            UpdateSelectionTimer();
            int prevSelection = selectedItem;

            if(selectionType == SelectionType.List){
                HandleListSelection();
            } else if(selectionType == SelectionType.Grid){
                HandleGridSelection();
            }
            
            selectedItem = Mathf.Clamp(selectedItem, 0, items.Count - 1);
           
            if(prevSelection != selectedItem){
                UpdateSelectionInUI();
            }

            if(Input.GetButtonDown("Action")){
                OnSelected?.Invoke(selectedItem);
            } else if(Input.GetButtonDown("Back")){
                OnBack?.Invoke();
            }
        }

        void HandleListSelection(){
            Debug.Log("List");
            float v = Input.GetAxisRaw("Vertical");
            if(selectionTimer == 0 && Mathf.Abs(v) > 0.2f){
                selectedItem += -(int) Mathf.Sign(v);
                selectionTimer = 1 / selectionSpeed;
            }
        }

        void HandleGridSelection(){
            Debug.Log("Grid");
            float v = Input.GetAxisRaw("Vertical");
            float h = Input.GetAxisRaw("Horizontal");
            if(selectionTimer == 0 && (Mathf.Abs(v) > 0.2f || Mathf.Abs(h) > 0.2f)){
                if(Mathf.Abs(h) > Mathf.Abs(v)){
                    selectedItem += (int) Mathf.Sign(h);
                } else {
                    selectedItem += -(int) Mathf.Sign(v) * gridWith;
                }
                selectionTimer = 1 / selectionSpeed;
            }
        }

        public virtual void UpdateSelectionInUI(){
            for(int i = 0; i < items.Count ; i++){
                items[i].OnSelectionChanged( i == selectedItem );
            }
        }

        void UpdateSelectionTimer(){
            if(selectionTimer > 0){
                selectionTimer = Mathf.Clamp( selectionTimer - Time.deltaTime, 0, selectionTimer);
            }
        }
    }
}
