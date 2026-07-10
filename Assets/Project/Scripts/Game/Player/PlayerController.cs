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
    private PlayerMovement movement;
    private PlayerJump jump;
    private PlayerTongueAttack tongueAttack;
    private PlayerGrappleHook grappleHook;
    private PlayerSoundEmitter soundEmitter;

    private PlayerControls controls;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        jump = GetComponent<PlayerJump>();
        tongueAttack = GetComponent<PlayerTongueAttack>();
        grappleHook = GetComponent<PlayerGrappleHook>();
        soundEmitter = GetComponent<PlayerSoundEmitter>();
    }

    private void OnEnable()
    {
        if (controls == null)
        {
            controls = new PlayerControls();

            controls.Player.Move.performed += ctx => movement.SetMoveInput(ctx.ReadValue<Vector2>());
            controls.Player.Move.canceled += ctx => movement.SetMoveInput(Vector2.zero);

            controls.Player.Jump.performed += ctx => jump.StartJump();
            controls.Player.Jump.canceled += ctx => jump.CancelJump();

            controls.Player.Attack.performed += ctx => HandleAttackInput();
            controls.Player.Cry.performed += ctx => soundEmitter.Cry();

            controls.Player.Reel.started += ctx => grappleHook.StartAutoReel();

            controls.Player.ReelIn.started += ctx => grappleHook.SetReelInput(true);
            controls.Player.ReelIn.canceled += ctx => grappleHook.SetReelInput(false);

            controls.Player.ReelOut.started += ctx => grappleHook.SetReelOutInput(true);
            controls.Player.ReelOut.canceled += ctx => grappleHook.SetReelOutInput(false);
        }

        controls.Enable();
    }

    private void OnDisable()
    {
        controls?.Disable();
    }

    private void HandleAttackInput()
    {
        // 갈고리에 매달려 있는 상태에서 공격 버튼을 다시 누르면 로프를 해제합니다.
        if (grappleHook.IsHooking)
        {
            grappleHook.Release();
            return;
        }

        if (tongueAttack.IsAttacking) return;

        tongueAttack.Attack();
    }
}
