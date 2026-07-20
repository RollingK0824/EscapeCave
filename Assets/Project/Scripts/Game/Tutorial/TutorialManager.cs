using TMPro;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    [System.Serializable]
    private struct TutorialStep
    {
        [TextArea]
        public string message;
        public PlayerAbility unlockAbility;
    }

    [SerializeField] private TutorialStep[] _steps;
    [SerializeField] private TMP_Text _promptText;
    [SerializeField] private PlayerController _playerController;
    [SerializeField] private float _freezeDuration = 0.5f;

    private int _currentStep = -1;

    public void SetCheckpoint(Vector3 position)
    {
        _playerController.SetRespawnPoint(position);
    }

    public void AdvanceStep()
    {
        _currentStep++;

        if (_currentStep >= _steps.Length)
        {
            _promptText.gameObject.SetActive(false);
            return;
        }

        _playerController.SetAbilityEnabled(_steps[_currentStep].unlockAbility, true);
        _playerController.FreezeMomentarily(_freezeDuration);

        bool hasMessage = !string.IsNullOrEmpty(_steps[_currentStep].message);
        _promptText.gameObject.SetActive(hasMessage);

        if (hasMessage)
        {
            _promptText.text = _steps[_currentStep].message;
        }
    }
}
