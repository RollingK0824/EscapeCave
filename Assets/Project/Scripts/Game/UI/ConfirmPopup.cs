using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmPopup : MonoBehaviour
{
    [SerializeField] private Button _yesButton;
    [SerializeField] private Button _noButton;
    [SerializeField] private TMP_Text _messageText;

    private Action _onConfirm;

    private void Awake()
    {
        _yesButton.onClick.AddListener(HandleYes);
        _noButton.onClick.AddListener(HandleNo);
    }

    public void Show(Action onConfirm)
    {
        Show(string.Empty, onConfirm);
    }

    public void Show(string message, Action onConfirm)
    {
        if (_messageText != null)
        {
            _messageText.text = message;
        }

        _onConfirm = onConfirm;
        gameObject.SetActive(true);
    }

    private void HandleYes()
    {
        gameObject.SetActive(false);
        _onConfirm?.Invoke();
    }

    private void HandleNo()
    {
        gameObject.SetActive(false);
    }
}