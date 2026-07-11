using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "GroundSpawn", story: "[Self] spawns from ground at [SpawnPoint]", category: "Action/Monster/Spawn", id: "3672cd0182297d95ba971a8e10c7ea68")]
public partial class GroundSpawnAction : Action
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

        _rb.gravityScale = 1.0f;
        _rb.linearVelocity = Vector2.zero;

        if (SpawnPoint.Value != null)
        {
            Self.Value.transform.position = SpawnPoint.Value.position;
        }

        return Status.Success;
    }
}

