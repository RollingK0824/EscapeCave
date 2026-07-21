using Unity.Cinemachine;
using UnityEngine;

namespace Game.Environment
{
    [DisallowMultipleComponent]
    public class InfiniteParallaxLayer : MonoBehaviour
    {
        [SerializeField]
        private Transform _target;

        [SerializeField, Range(0f, 1f)]
        [Tooltip("1 = 카메라와 함께 고정되어 가장 멀리 있는 것처럼 보임, 0 = 월드와 같은 속도로 움직여 가장 가까운 것처럼 보임")]
        private float _parallaxAmount = 0.5f;

        [SerializeField]
        private bool _loopHorizontal = true;

        [SerializeField]
        private bool _followVertical = false;

        private SpriteRenderer[] _tiles;
        private float _tileWidth;
        private Vector3 _lastTargetPosition;

        private void Awake()
        {
            _tiles = GetComponentsInChildren<SpriteRenderer>();

            if (_target == null && Camera.main != null)
            {
                _target = Camera.main.transform;
            }

            if (_tiles.Length > 0)
            {
                _tileWidth = _tiles[0].bounds.size.x;
            }

            if (_target != null)
            {
                _lastTargetPosition = _target.position;
            }
        }

        private void OnEnable()
        {
            CinemachineCore.CameraUpdatedEvent.AddListener(HandleCameraUpdated);
        }

        private void OnDisable()
        {
            CinemachineCore.CameraUpdatedEvent.RemoveListener(HandleCameraUpdated);
        }

        private void HandleCameraUpdated(CinemachineBrain brain)
        {
            if (_target == null)
            {
                return;
            }

            Vector3 delta = _target.position - _lastTargetPosition;
            float verticalMove = _followVertical ? delta.y * _parallaxAmount : 0f;
            transform.position += new Vector3(delta.x * _parallaxAmount, verticalMove, 0f);
            _lastTargetPosition = _target.position;

            if (_loopHorizontal && _tileWidth > 0f)
            {
                RecycleTiles();
            }
        }

        private void RecycleTiles()
        {
            float totalWidth = _tileWidth * _tiles.Length;
            float halfSpan = _tileWidth * (_tiles.Length * 0.5f);

            foreach (SpriteRenderer tile in _tiles)
            {
                while (_target.position.x - tile.transform.position.x >= halfSpan)
                {
                    tile.transform.position += new Vector3(totalWidth, 0f, 0f);
                }

                while (_target.position.x - tile.transform.position.x <= -halfSpan)
                {
                    tile.transform.position -= new Vector3(totalWidth, 0f, 0f);
                }
            }
        }
    }
}
