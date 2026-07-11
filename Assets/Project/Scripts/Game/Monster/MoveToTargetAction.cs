using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "MoveToTarget", story: "[Self] moves toward [PlayerTransform] with [MonsterData]", category: "Action", id: "19c7c11385861f34478f465f902c150e")]
public partial class MoveToTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<Transform> PlayerTransform;
    [SerializeReference] public BlackboardVariable<MonsterData> MonsterData;

    private Rigidbody2D _rb;
    private SpriteRenderer _spriteRenderer;

    protected override Status OnStart()
    {
        _rb = Self.Value.GetComponent<Rigidbody2D>();
        _spriteRenderer = Self.Value.GetComponent<SpriteRenderer>();

        if (_rb == null || PlayerTransform == null || MonsterData.Value == null) 
        {
            return Status.Failure;
        }

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Self.Value == null || _spriteRenderer == null || PlayerTransform == null || MonsterData.Value == null)
        {
            return Status.Failure;
        }

        Vector2 selfPos = Self.Value.transform.position;
        Vector2 targetPos = PlayerTransform.Value.position;
        float distance = Vector2.Distance(selfPos, targetPos);

        // 방향 계산
        Vector2 direction = (targetPos - selfPos).normalized;

        // 스프라이트 방향 전환
        if (direction.x != 0)
        {
            //Vector3 scale = Self.Value.transform.localScale;
            //scale.x = direction.x > 0 ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            //Self.Value.transform.localScale = scale;

            _spriteRenderer.flipX = direction.x < 0;
            Debug.Log($"direction.x: {direction.x}, flipX: {_spriteRenderer.flipX}");
        }

        if (distance <= MonsterData.Value.AttackRange)
        {
            _rb.linearVelocity = Vector2.zero;
            return Status.Success;
        }

      

        // 이동
        _rb.linearVelocity = direction * MonsterData.Value.MoveSpeed;



        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
        }
    }
}

