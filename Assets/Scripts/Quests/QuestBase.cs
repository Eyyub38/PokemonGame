using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Quests/Creat a new quest")]
public class QuestBase : ScriptableObject{
    [Header("Quest Info")]
    [SerializeField] string _name;
    [SerializeField] string description;

    [Header("Quest Dialogs")]
    [SerializeField] Dialog startDialog;
    [SerializeField] Dialog inProgressDialog;
    [SerializeField] Dialog completeDialog;

    [Header("Quest Requirements")]
    [SerializeField] ItemBase requiredItem;
    [SerializeField] int requiredItemCount = 1;

    [Header("Quest Rewards")]
    [SerializeField] ItemBase rewardItem;
    [SerializeField] int rewardItemCount = 1;

    public string Name => _name;
    public string Description => description;
    public Dialog StartDialog => startDialog;
    public Dialog InProgressDialog => inProgressDialog?.Lines?.Count > 0 ? inProgressDialog : startDialog;
    public Dialog CompleteDialog => completeDialog;
    public ItemBase RequiredItem => requiredItem;
    public int RequiredItemCount => requiredItemCount; 
    public ItemBase RewardItem => rewardItem;
    public int RewardItemCount => rewardItemCount;
}
