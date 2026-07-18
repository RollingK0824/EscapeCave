using Managers;
using UnityEngine;

/// <summary>
/// 소리(에코) 발생과 울음(Cry) 애니메이션 트리거를 전담하는 컴포넌트.
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerSoundEmitter : MonoBehaviour, IEchoable
{
    [SerializeField] private float _soundIntensity;
    [SerializeField] private float _soundSpeed;

    public float SoundIntensity => _soundIntensity;
    public float SoundSpeed => _soundSpeed;

    private Animator _animator;
    private PlayerMovement _movement;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _movement = GetComponent<PlayerMovement>();
    }

    public void Echo()
    {
        EchoManager.Instance.TriggerSound(transform.position, SoundIntensity, SoundSpeed);
        // 몬스터 관련 로직 추가
        MonsterManager.NotifySound(transform);
        Debug.Log($"PlayerSoundEmitter) position : ({transform.position})");
    }

    public void Cry()
    {
        // 비행 중에는 Cry 애니메이션으로 덮어쓰지 않고 Fly 애니메이션을 유지한 채 에코만 발생시킨다.
        if (_movement == null || !_movement.IsFlying)
        {
            _animator.SetTrigger("Cry");
        }
        Echo();
    }
}
