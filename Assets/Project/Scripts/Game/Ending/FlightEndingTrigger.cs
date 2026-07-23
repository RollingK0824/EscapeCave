using System.Collections;
using Managers;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// UnlockTree 씬에 배치. 플라이트(Final) 노드가 해금되면 화면을 하얗게 전환한 뒤 Ending 씬으로 넘어갑니다.
/// </summary>
public class FlightEndingTrigger : MonoBehaviour
{
    [SerializeField] private UnlockNodeData _flightNode;
    [SerializeField] private CanvasGroup _whiteCanvasGroup;
    [SerializeField] private SoundDataSO _unlockSound;
    [SerializeField] private float _delayBeforeFade = 0.3f;
    [SerializeField] private float _fadeDuration = 0.6f;
    [SerializeField] private string _endingSceneName = "Ending";

    private void OnEnable()
    {
        UIManager.Instance.OnUnlockChanged += HandleUnlockChanged;
    }

    private void OnDisable()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OnUnlockChanged -= HandleUnlockChanged;
        }
    }

    private void HandleUnlockChanged(UnlockNodeData node)
    {
        if (node != _flightNode) return;

        StartCoroutine(TransitionRoutine());
    }

    private IEnumerator TransitionRoutine()
    {
        // 노드 해금 확인 팝업이 닫히자마자, 페이드 시작을 기다리지 않고 바로 입력을 막는다.
        // (그전엔 _delayBeforeFade 동안 트리 UI가 그대로 클릭 가능해서 다른 노드나 뒤로가기를 눌러버릴 수 있었음)
        // WhiteFade 오브젝트가 씬에 기본 비활성 상태로 있어서, 먼저 켜주지 않으면
        // blocksRaycasts/alpha를 바꿔도 비활성 오브젝트라 아무 효과가 없다.
        if (_whiteCanvasGroup != null)
        {
            _whiteCanvasGroup.gameObject.SetActive(true);
            _whiteCanvasGroup.alpha = 0f;
            _whiteCanvasGroup.blocksRaycasts = true;
        }

        if (_unlockSound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(_unlockSound);
        }

        yield return new WaitForSeconds(_delayBeforeFade);
        yield return FadeToWhiteRoutine();
        SceneManager.LoadScene(_endingSceneName);
    }

    private IEnumerator FadeToWhiteRoutine()
    {
        if (_whiteCanvasGroup == null) yield break;

        float elapsed = 0f;
        while (elapsed < _fadeDuration)
        {
            elapsed += Time.deltaTime;
            _whiteCanvasGroup.alpha = Mathf.Clamp01(elapsed / _fadeDuration);
            yield return null;
        }

        _whiteCanvasGroup.alpha = 1f;
    }
}
