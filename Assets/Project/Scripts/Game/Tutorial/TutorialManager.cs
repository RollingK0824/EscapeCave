using TMPro;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    [System.Serializable]
    private struct TutorialStep
    {
        [TextArea]
        public string message;
    }

    [SerializeField] private TutorialStep[] _steps;
    [SerializeField] private TMP_Text _promptText;

    private int _currentStep = -1;

    public void AdvanceStep()
    {
        _currentStep++;

        if (_currentStep >= _steps.Length)
        {
            _promptText.gameObject.SetActive(false);
            return;
        }

        bool hasMessage = !string.IsNullOrEmpty(_steps[_currentStep].message);
        _promptText.gameObject.SetActive(hasMessage);

        if (hasMessage)
        {
            _promptText.text = _steps[_currentStep].message;
        }
    }
}
