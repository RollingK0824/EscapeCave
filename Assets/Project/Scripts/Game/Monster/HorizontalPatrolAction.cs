using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using Unity.AppUI.UI;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "HorizontalPatrol", story: "[Monster] patrols horizontally", category: "Action/Monster/Patrol", id: "9fdd01d860b0016e277ecaa49fa24002")]
public partial class HorizontalPatrolAction : Action
{
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
        
        _monster.HorizontalPatrol(ref _direction, ref _startPosition);
        return Status.Running;
    }

    protected override void OnEnd()
    {
        _monster?.Stop();
    }
}

