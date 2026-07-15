using System;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmPopup : MonoBehaviour
{
    [SerializeField] private Button _yesButton;
    [SerializeField] private Button _noButton;

    private Action _onConfirm;

    private void Awake()
    {
        _yesButton.onClick.AddListener(HandleYes);
        _noButton.onClick.AddListener(HandleNo);
    }

    public void Show(Action onConfirm)
    {
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