using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using Unity.AppUI.UI;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "HorizontalPatrol", story: "[Self] patrols horizontally with [PatrolSpeed] and [PatrolRange]", category: "Action/Monster/Patrol", id: "9fdd01d860b0016e277ecaa49fa24002")]
public partial class HorizontalPatrolAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<float> PatrolSpeed;
    [SerializeReference] public BlackboardVariable<float> PatrolRange;

    private Rigidbody2D _rb;
    private Vector2 _startPosition;
    private int _direction = 1;

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
        float currentX = Self.Value.transform.position.x;
        float delta = currentX - _startPosition.x;

        if (delta >= PatrolRange.Value)
        {
            _direction = -1;
        }
        else if (delta <= PatrolRange.Value)
        {
            _direction = 1;
        }

        _rb.linearVelocity = new Vector2(_direction * PatrolSpeed.Value, _rb.linearVelocity.y);

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

