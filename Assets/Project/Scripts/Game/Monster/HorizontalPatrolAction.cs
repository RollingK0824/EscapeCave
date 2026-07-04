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
    // [SerializeReference] public BlackboardVariable<GameObject> Self;
    //[SerializeReference] public BlackboardVariable<float> PatrolSpeed;
    //[SerializeReference] public BlackboardVariable<float> PatrolRange;
    [SerializeReference] public BlackboardVariable<MonsterController> Monster;


    private MonsterController _monster;
    private Vector2 _startPosition;
    private int _direction = 1;
    

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
        float currentX = _monster.transform.position.x;
        float delta = currentX - _startPosition.x;

        if (delta >= _monster.Data.PatrolRange)
        {
            _direction = -1;
        }
        else if (delta <= -_monster.Data.PatrolRange)
        {
            _direction = 1;
        }

        _monster.Rb.linearVelocity = new Vector2(_direction * _monster.Data.PatrolSpeed, _monster.Rb.linearVelocity.y);

        _monster.FlipSprite(new Vector2(_direction, 0));

        return Status.Running;
    }

    protected override void OnEnd()
    {
        _monster?.Stop();
    }
}

