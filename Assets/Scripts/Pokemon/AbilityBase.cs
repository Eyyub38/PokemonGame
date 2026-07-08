using UnityEngine;

[CreateAssetMenu(fileName = "Ability", menuName = "Pokemon/Create new Ability")]
public class AbilityBase : ScriptableObject
{
    [Header("Ability Details")]
    [SerializeField] string _name;
    [TextArea]
    [SerializeField] string description;
    [SerializeField] AbilityID abilityId;

    public string Name => _name;
    public string Description => description;
    public AbilityID AbilityId => abilityId;
}
