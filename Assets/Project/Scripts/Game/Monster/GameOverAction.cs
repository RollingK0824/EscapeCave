using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "GameOver", story: "[Monster] catches the player", category: "Action/Monster", id: "af90542cfb11725b230d162b0e323a4b")]
public partial class GameOverAction : Action
{
    [SerializeReference] public BlackboardVariable<PauseMenu> Monster;

    protected override Status OnStart()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        return Status.Success;
    }
}

