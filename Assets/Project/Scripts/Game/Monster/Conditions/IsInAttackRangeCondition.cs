using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "IsInAttackRange", story: "[Monster] is within attack range of [PlayerTransform]", category: "Conditions/Monster", id: "09f6e552876a4cc78feab6c895c5c44b")]
public partial class IsInAttackRangeCondition : Condition
{
    [SerializeReference] public BlackboardVariable<MonsterController> Monster;
    [SerializeReference] public BlackboardVariable<Transform> PlayerTransform;

    public override bool IsTrue()
    {
        if (Monster.Value == null || PlayerTransform.Value == null)
        {
            return false;
        }

        return Monster.Value.IsInAttackRange(Monster.Value.GetChaseTarget(PlayerTransform.Value));
    }
}
