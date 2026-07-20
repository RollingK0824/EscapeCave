using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class TutorialEnding : MonoBehaviour
{
    [SerializeField] private PlayerController _playerController;
    [SerializeField] private PlayerMovement _playerMovement;
    [SerializeField] private CanvasGroup _fadeCanvasGroup;
    [SerializeField] private CinemachineCamera _followCamera;
    [SerializeField] private float _zoomOrthoSize = 2f;
    [SerializeField, Range(0.05f, 1f)] private float _walkSpeedMultiplier = 0.3f;
    [SerializeField] private float _walkDuration = 2f;
    [SerializeField] private float _fadeDuration = 1f;
    [SerializeField] private string _nextSceneName = "ProtoType";

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        GetComponent<Collider2D>().enabled = false;
        StartCoroutine(EndingRoutine());
    }

    private IEnumerator EndingRoutine()
    {
        _playerController.SetAbilityEnabled(PlayerAbility.Move, false);
        _playerController.SetAbilityEnabled(PlayerAbility.Jump, false);
        _playerController.SetAbilityEnabled(PlayerAbility.Attack, false);
        _playerController.SetAbilityEnabled(PlayerAbility.Cry, false);

        CinemachineConfiner2D confiner = null;
        if (_followCamera != null)
        {
            confiner = _followCamera.GetComponent<CinemachineConfiner2D>();
            if (confiner != null)
            {
                confiner.enabled = false;
            }
        }

        float startSize = _followCamera != null ? _followCamera.Lens.OrthographicSize : 0f;

        float elapsed = 0f;
        while (elapsed < _walkDuration)
        {
            _playerMovement.SetMoveInput(Vector2.right * _walkSpeedMultiplier);
            elapsed += Time.deltaTime;

            if (_followCamera != null)
            {
                LensSettings lens = _followCamera.Lens;
                lens.OrthographicSize = Mathf.Lerp(startSize, _zoomOrthoSize, elapsed / _walkDuration);
                _followCamera.Lens = lens;
            }

            yield return null;
        }

        _playerMovement.SetMoveInput(Vector2.zero);

        yield return FadeOutRoutine();

        SceneManager.LoadScene(_nextSceneName);
    }

    private IEnumerator FadeOutRoutine()
    {
        _fadeCanvasGroup.gameObject.SetActive(true);
        _fadeCanvasGroup.blocksRaycasts = true;

        float elapsed = 0f;
        while (elapsed < _fadeDuration)
        {
            elapsed += Time.deltaTime;
            _fadeCanvasGroup.alpha = Mathf.Clamp01(elapsed / _fadeDuration);
            yield return null;
        }

        _fadeCanvasGroup.alpha = 1f;
    }
}
