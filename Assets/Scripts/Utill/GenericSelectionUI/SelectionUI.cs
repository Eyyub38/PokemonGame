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
        
        public ISelectionInput InputSource { get; set; }

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

        public virtual void HandleUpdate() {
            if(items == null || items.Count == 0) return;
            if(InputSource == null) return;

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

            if(InputSource.SubmitPressedThisFrame){
                OnSelected?.Invoke(selectedItem);
            } else if(InputSource.BackPressedThisFrame){
                OnBack?.Invoke();
            }
        }

        void HandleListSelection() {
            float v = Mathf.RoundToInt(InputSource.Navigate.y);
            if(selectionTimer == 0 && Mathf.Abs(v) > 0.2f){
                selectedItem += -(int) Mathf.Sign(v);
                selectionTimer = 1 / selectionSpeed;
            }
        }

        void HandleGridSelection(){
            float h = InputSource.Navigate.x;
            float v = InputSource.Navigate.y;
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
