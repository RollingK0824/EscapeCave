using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Spawn", story: "[Monster] spawns at [SpawnPoint]", category: "Action/Monster/Spawn", id: "772a55254b66f5b8b13ca68229ac2a77")]
public partial class SpawnAction : Action
{
    [SerializeReference] public BlackboardVariable<MonsterController> Monster;
    [SerializeReference] public BlackboardVariable<Transform> SpawnPoint;

    protected override Status OnStart()
    {
        if (Monster.Value == null)
        {
            return Status.Failure;
        }

        var rb = Monster.Value.Rb;

        rb.gravityScale = Monster.Value.Data.UseGravity ? Monster.Value.Data.GravityScale : 0f;
        rb.linearVelocity = Vector2.zero;

        if (SpawnPoint.Value != null)
        {
            Monster.Value.transform.position = SpawnPoint.Value.position;
        }

        return Status.Success;
    }
}

