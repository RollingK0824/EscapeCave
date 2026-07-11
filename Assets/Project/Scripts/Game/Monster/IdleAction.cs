using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Idle", story: "[Self] idels until detected", category: "Action", id: "c7d3ba6a870e0543a489199c486e10de")]
public partial class IdleAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    private Rigidbody2D _rb;

    protected override Status OnStart()
    {
        _rb = Self.Value.GetComponent<Rigidbody2D>();
        if (_rb == null)
        {
            _rb.linearVelocity = Vector2.zero;
        }

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        // 감지 노드가 Success 반환할 때까지 대기
        return Status.Running;
    }
}

