using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "VerticalPatrol", story: "[Self] patrols vertically with [PatrolSpeed] and [PatrolRange]", category: "Action/Monster/Patrol", id: "d0f64c0321fce6c3d58efb6dad6347df")]
public partial class VerticalPatrolAction : Action
{
    //[SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<MonsterController> Monster;

    private MonsterController _monster;
    private Vector2 _startPosition;
    private int _direction;

    protected override Status OnStart()
    {
        _monster = Monster.Value;

        if (_monster == null)
        {
            return Status.Failure;
        }

        _startPosition = _monster.transform.position;
        _direction = 1;

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        float currentY = _monster.transform.position.y;
        float delta = currentY - _startPosition.y;

        if (delta >= _monster.Data.PatrolRange)
        {
            _direction = -1;
        }
        else if (delta <= _monster.Data.PatrolRange)
        {
            _direction = 1;
        }

        _monster.Rb.linearVelocity = new Vector2(_monster.Rb.linearVelocity.x, _direction * _monster.Data.PatrolSpeed);

        return Status.Running;
    }

    protected override void OnEnd()
    {
        _monster?.Stop();
    }
}

