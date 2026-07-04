using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "MoveToTarget", story: "[Self] moves toward [PlayerTransform] with [MoveSpeed]", category: "Action/Monster/Chase", id: "19c7c11385861f34478f465f902c150e")]
public partial class MoveToTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<Transform> PlayerTransform;
    [SerializeReference] public BlackboardVariable<float> MoveSpeed;

    protected override Status OnUpdate()
    {
        if (Self.Value == null || PlayerTransform == null)
        {
            return Status.Failure;
        }

        Vector2 selfPos = Self.Value.transform.position;
        Vector2 targetPos = PlayerTransform.Value.position;

        // 방향 계산
        Vector2 direction = (targetPos - selfPos).normalized;

        // 이동
        Self.Value.transform.position
            = Vector2.MoveTowards(selfPos, targetPos, MoveSpeed.Value * Time.deltaTime);

        // 스프라이트 방향 전환
        if (direction.x != 0)
        {
            Vector3 scale = Self.Value.transform.localScale;
            scale.x = direction.x > 0 ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            Self.Value.transform.localScale = scale;
        }

        return Status.Running;
    }
}

