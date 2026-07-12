using UnityEngine;

/// <summary>
/// 입력을 수신해서 각 전담 컴포넌트(PlayerMovement, PlayerJump, PlayerTongueAttack,
/// PlayerGrappleHook, PlayerSoundEmitter)에 명령만 전달하는 오케스트레이터.
/// 이동/점프/공격/갈고리/사운드의 실제 로직은 각 컴포넌트가 담당합니다.
///
/// 같은 GameObject에 아래 컴포넌트들이 함께 붙어 있어야 합니다:
/// PlayerMovement, PlayerJump, PlayerTongueAttack, PlayerGrappleHook, PlayerSoundEmitter,
/// Rigidbody2D, Animator
/// </summary>
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerJump))]
[RequireComponent(typeof(PlayerTongueAttack))]
[RequireComponent(typeof(PlayerGrappleHook))]
[RequireComponent(typeof(PlayerSoundEmitter))]
public class PlayerController : MonoBehaviour
{
    private PlayerMovement _movement;
    private PlayerJump _jump;
    private PlayerTongueAttack _tongueAttack;
    private PlayerGrappleHook _grappleHook;
    private PlayerSoundEmitter _soundEmitter;

    private PlayerControls _controls;
    private bool _attackHeld;

    private void Awake()
    {
        _movement = GetComponent<PlayerMovement>();
        _jump = GetComponent<PlayerJump>();
        _tongueAttack = GetComponent<PlayerTongueAttack>();
        _grappleHook = GetComponent<PlayerGrappleHook>();
        _soundEmitter = GetComponent<PlayerSoundEmitter>();

        // 몬스터 관련 로직 추가
        Managers.MonsterManager.RegisterPlayer(transform);
    }

    private void OnEnable()
    {
        if (_controls == null)
        {
            _controls = new PlayerControls();

            _controls.Player.Move.performed += ctx => _movement.SetMoveInput(ctx.ReadValue<Vector2>());
            _controls.Player.Move.canceled += ctx => _movement.SetMoveInput(Vector2.zero);

            _controls.Player.Jump.performed += ctx => _jump.StartJump();
            _controls.Player.Jump.canceled += ctx => _jump.CancelJump();

            _controls.Player.Attack.started += ctx => { _attackHeld = true; HandleAttackInput(); };
            _controls.Player.Attack.canceled += ctx => _attackHeld = false;
            _controls.Player.Cry.performed += ctx => _soundEmitter.Cry();

            _controls.Player.Reel.started += ctx => _grappleHook.StartAutoReel();

            _controls.Player.ReelIn.started += ctx => _grappleHook.SetReelInput(true);
            _controls.Player.ReelIn.canceled += ctx => _grappleHook.SetReelInput(false);

            _controls.Player.ReelOut.started += ctx => _grappleHook.SetReelOutInput(true);
            _controls.Player.ReelOut.canceled += ctx => _grappleHook.SetReelOutInput(false);
        }

        _controls.Enable();
    }

    private void OnDisable()
    {
        _controls?.Disable();
    }

    private void Update()
    {
        // 공격 버튼을 누르고 있는 동안에만 갈고리가 유지됩니다. 버튼을 놓으면 즉시 해제합니다.
        // (버튼을 뗀 뒤에야 혀가 목표에 도달해 훅이 걸리는 경우도 여기서 바로 정리됩니다.)
        if (_grappleHook.IsHooking && !_attackHeld)
        {
            _grappleHook.Release();
        }
    }

    private void HandleAttackInput()
    {
        if (_grappleHook.IsHooking) return;
        if (_tongueAttack.IsAttacking) return;

        _tongueAttack.Attack();
    }

    // 몬스터 관련 로직 추가
    private void OnDestroy()
    {
        Managers.MonsterManager.UnregisterPlayer(transform);
    }
}
