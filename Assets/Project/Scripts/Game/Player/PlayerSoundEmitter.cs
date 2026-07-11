using Managers;
using UnityEngine;

/// <summary>
/// 소리(에코) 발생과 울음(Cry) 애니메이션 트리거를 전담하는 컴포넌트.
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerSoundEmitter : MonoBehaviour, IEchoable
{
    [SerializeField] private float soundIntensity;
    [SerializeField] private float soundSpeed;

    public float SoundIntensity => soundIntensity;
    public float SoundSpeed => soundSpeed;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void Echo()
    {
        EchoManager.Instance.TriggerSound(transform.position, SoundIntensity, SoundSpeed);
        // 몬스터 관련 로직 추가
        MonsterManager.NotifySound();
        Debug.Log($"PlayerSoundEmitter) position : ({transform.position})");
    }

    public void Cry()
    {
        animator.SetTrigger("Cry");
        Debug.Log("Cry called");
        Echo();
    }
}
