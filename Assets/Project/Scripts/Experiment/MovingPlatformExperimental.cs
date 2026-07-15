using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MovingPlatform 실험용 사본. 팀원이 관리하는 원본(MovingPlatform.cs)은 건드리지 않고
/// 탑승자 추적(델타 이동 전이) 로직만 별도로 테스트하기 위한 스크립트입니다.
/// 원본과 달리 Initialize()를 외부(맵 생성기)에서 호출해주지 않아도,
/// 인스펙터 값만으로 Play를 누르면 바로 좌우/상하로 움직입니다.
/// </summary>
public class MovingPlatformExperimental : MonoBehaviour
{
    [SerializeField] private MoveDirection direction = MoveDirection.Horizontal;
    [SerializeField] private float moveRange = 3f;
    [SerializeField] private float speed = 2f;

    private Vector3 startPos;
    private float progress = 0f;
    private bool movingForward = true;

    [SerializeField, Tooltip("이 값보다 아래(플랫폼 쪽)를 향하는 접촉면만 '위에 탑승'으로 인정합니다")]
    private float _topNormalMinY = 0.5f;

    private Rigidbody2D _rb;
    private readonly Dictionary<Collider2D, bool> _topContacts = new Dictionary<Collider2D, bool>();

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        startPos = transform.position;
    }

    private void FixedUpdate()
    {
        if (moveRange <= 0) return;

        Vector3 prevPos = transform.position;

        if (movingForward)
        {
            progress += speed * Time.fixedDeltaTime;
            if (progress >= moveRange)
            {
                progress = moveRange;
                movingForward = false;
            }
        }
        else
        {
            progress -= speed * Time.fixedDeltaTime;
            if (progress <= 0)
            {
                progress = 0;
                movingForward = true;
            }
        }

        Vector3 newPos = startPos;
        if (direction == MoveDirection.Horizontal)
        {
            newPos.x += progress;
        }
        else
        {
            newPos.y += progress;
        }

        if (_rb != null)
        {
            _rb.MovePosition(newPos);
        }
        else
        {
            transform.position = newPos;
        }

        Vector3 delta = newPos - prevPos;
        if (delta == Vector3.zero) return;

        foreach (KeyValuePair<Collider2D, bool> contact in _topContacts)
        {
            if (!contact.Value) continue;

            Rigidbody2D passengerRb = contact.Key.attachedRigidbody;
            if (passengerRb != null)
            {
                passengerRb.position += (Vector2)delta;
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        UpdateTopContact(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        UpdateTopContact(collision);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        _topContacts.Remove(collision.collider);
    }

    private void UpdateTopContact(Collision2D collision)
    {
        bool isOnTop = false;
        int contactCount = collision.contactCount;
        for (int i = 0; i < contactCount; i++)
        {
            if (collision.GetContact(i).normal.y < -_topNormalMinY)
            {
                isOnTop = true;
                break;
            }
        }

        _topContacts[collision.collider] = isOnTop;
    }
}