using UnityEngine;

public class TutorialIntro : MonoBehaviour
{
    [SerializeField] private PlayerController _playerController;
    [SerializeField] private WaterDrop _introWaterDrop;
    [SerializeField] private TutorialManager _tutorialManager;

    private void Awake()
    {
        _playerController.enabled = false;
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
        _playerController.enabled = true;
        _tutorialManager.AdvanceStep();
    }
}
