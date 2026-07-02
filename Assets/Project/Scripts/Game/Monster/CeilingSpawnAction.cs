using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "CeilingSpawn", story: "[Self] spawns from ceiling at [SpawnPoint]", category: "Action/Monster/Spawn", id: "0348e67a44bb9d6108057ff885c7e584")]
public partial class CeilingSpawnAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<Transform> SpawnPoint;

    private Rigidbody2D _rb;

    protected override Status OnStart()
    {
        _rb = Self.Value.GetComponent<Rigidbody2D>();
        if (_rb == null)
        {
            return Status.Failure;
        }

        _rb.gravityScale = 0f;
        _rb.linearVelocity = Vector2.zero;

        if (SpawnPoint.Value != null)
        {
            Self.Value.transform.position = SpawnPoint.Value.position;
        }

        return Status.Success;
    }
}

