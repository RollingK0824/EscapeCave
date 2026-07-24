using System.Collections;
using Managers;
using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MonsterRevealTrigger : MonoBehaviour
{
    [SerializeField] private CinemachineCamera _focusCamera;
    [SerializeField] private float _holdDuration = 1.5f;
    [SerializeField] private TutorialManager _tutorialManager;

    [Header("카메라 전환 시 에코 웨이브 연출")]
    [SerializeField] private float _echoIntensity = 10f;
    [SerializeField] private float _echoSpeed = 5f;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        GetComponent<Collider2D>().enabled = false;
        StartCoroutine(RevealRoutine());
    }

    private IEnumerator RevealRoutine()
    {
        Time.timeScale = 0f;
        _focusCamera.gameObject.SetActive(true);
        TriggerFocusEcho();
        _tutorialManager.AdvanceStep();

        yield return new WaitForSecondsRealtime(_holdDuration);

        _focusCamera.gameObject.SetActive(false);
        Time.timeScale = 1f;
    }

    private void TriggerFocusEcho()
    {
        if (EchoManager.Instance == null) return;

        Vector3 echoOrigin = _focusCamera.transform.position;
        echoOrigin.z = 0f;

        EchoManager.Instance.TriggerSound(echoOrigin, _echoIntensity, _echoSpeed);
    }
}
