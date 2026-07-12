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

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    public void Echo()
    {
        EchoManager.Instance.TriggerSound(transform.position, SoundIntensity, SoundSpeed);
    }

    public void Cry()
    {
        _animator.SetTrigger("Cry");
        Echo();
    }
}
