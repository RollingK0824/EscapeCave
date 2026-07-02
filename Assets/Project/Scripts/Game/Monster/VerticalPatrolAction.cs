using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "VerticalPatrol", story: "[Self] patrols vertically with [PatrolSpeed] and [PatrolRange]", category: "Action/Monster/Patrol", id: "d0f64c0321fce6c3d58efb6dad6347df")]
public partial class VerticalPatrolAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<float> PatrolSpeed;
    [SerializeReference] public BlackboardVariable<float> PatrolRange;

    private Rigidbody2D _rb;
    private Vector2 _startPosition;
    private int _direction;

    protected override Status OnStart()
    {
        _rb = Self.Value.GetComponent<Rigidbody2D>();
        if (_rb == null)
        {
            return Status.Failure;
        }

        _startPosition = Self.Value.transform.position;
        _direction = 1;

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        float currentY = Self.Value.transform.position.y;
        float delta = currentY - _startPosition.y;

        if (delta >= PatrolRange.Value)
        {
            _direction = -1;
        }
        else if (delta <= PatrolRange.Value)
        {
            _direction = 1;
        }

        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _direction * PatrolSpeed.Value);

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

