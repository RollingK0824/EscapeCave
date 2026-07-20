using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MonsterRevealTrigger : MonoBehaviour
{
    [SerializeField] private CinemachineCamera _focusCamera;
    [SerializeField] private float _holdDuration = 1.5f;
    [SerializeField] private TutorialManager _tutorialManager;

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
        _tutorialManager.AdvanceStep();

        yield return new WaitForSecondsRealtime(_holdDuration);

        _focusCamera.gameObject.SetActive(false);
        Time.timeScale = 1f;
    }
}
