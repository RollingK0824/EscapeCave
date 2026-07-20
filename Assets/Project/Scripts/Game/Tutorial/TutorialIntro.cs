using UnityEngine;

public class TutorialIntro : MonoBehaviour
{
    [SerializeField] private PlayerController _playerController;
    [SerializeField] private TutorialWaterDrop _introWaterDrop;
    [SerializeField] private TutorialManager _tutorialManager;

    private void Awake()
    {
        _playerController.SetAbilityEnabled(PlayerAbility.Move, false);
        _playerController.SetAbilityEnabled(PlayerAbility.Jump, false);
        _playerController.SetAbilityEnabled(PlayerAbility.Attack, false);
        _playerController.SetAbilityEnabled(PlayerAbility.Cry, false);
        _playerController.SetRespawnPoint(_playerController.transform.position);
    }

    private void OnEnable()
    {
        _introWaterDrop.OnLanded += HandleWaterDropLanded;
    }

    private void OnDisable()
    {
        _introWaterDrop.OnLanded -= HandleWaterDropLanded;
    }

    private void HandleWaterDropLanded()
    {
        _tutorialManager.AdvanceStep();
    }
}
