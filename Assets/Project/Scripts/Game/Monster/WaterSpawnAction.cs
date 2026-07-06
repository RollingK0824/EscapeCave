using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "WaterSpawn", story: "[Self] spawns from water at [SpawnPoint]", category: "Action/Monster/Spawn", id: "c0af0120c30fbc7b82eb6c429918e4f3")]
public partial class WaterSpawnAction : Action
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

        _rb.gravityScale = 0.5f;
        _rb.linearVelocity = Vector2.zero;

        if (SpawnPoint.Value != null)
        {
            Self.Value.transform.position = SpawnPoint.Value.position;
        }

        return Status.Success;
    }
}

