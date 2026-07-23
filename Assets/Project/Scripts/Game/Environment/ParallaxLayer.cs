using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    [Header("Parallax Settings")]
    [Tooltip("0: 카메라에 고정, 0.5: 중경, 1: 맵과 동일한 속도(전경)")]
    [Range(0f, 1f)]
    [SerializeField] private float parallaxFactorX = 0.5f;
    [Range(0f, 1f)]
    [SerializeField] private float parallaxFactorY = 1.0f;

    [Header("Camera & Scale")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool autoScaleToHeight = true;

    [Header("Child Sprites")]
    [Tooltip("연속으로 배치될 2~3개의 자식 Transform")]
    [SerializeField] private Transform[] backgroundSprites;

    private Transform cam;
    private float spriteWorldWidth;
    private float wrapDistance;
    private Vector3 startCamPos;
    private Vector3 startParentPos;
    private float initialOffsetY;

    private void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        cam = targetCamera.transform;

        if (autoScaleToHeight && backgroundSprites != null && backgroundSprites.Length > 0)
        {
            ScaleChildrenToCameraHeight();
        }
    }

    private void Start()
    {
        if (backgroundSprites == null || backgroundSprites.Length == 0) return;

        startCamPos = cam.position;
        startParentPos = transform.position;
        initialOffsetY = transform.position.y - cam.position.y;

        SpriteRenderer firstRenderer = backgroundSprites[0].GetComponent<SpriteRenderer>();
        spriteWorldWidth = firstRenderer.bounds.size.x;
        wrapDistance = spriteWorldWidth * backgroundSprites.Length;

        for (int i = 0; i < backgroundSprites.Length; i++)
        {
            if (backgroundSprites[i] != null)
            {
                Vector3 newPos = transform.position + new Vector3(i * spriteWorldWidth, 0f, 0f);
                newPos.z = backgroundSprites[i].position.z;
                backgroundSprites[i].position = newPos;
            }
        }
    }

    private void LateUpdate()
    {
        if (backgroundSprites == null || backgroundSprites.Length == 0) return;

        Vector3 camDelta = cam.position - startCamPos;

        float targetX = startParentPos.x + (camDelta.x * parallaxFactorX);
        float targetY = cam.position.y + initialOffsetY;

        if (parallaxFactorY < 1.0f)
        {
            targetY = startParentPos.y + (camDelta.y * parallaxFactorY);
        }

        transform.position = new Vector3(targetX, targetY, transform.position.z);

        foreach (Transform bg in backgroundSprites)
        {
            if (bg == null) continue;

            float distFromCam = cam.position.x - bg.position.x;

            if (distFromCam >= spriteWorldWidth)
            {
                bg.position += new Vector3(wrapDistance, 0f, 0f);
            }
            else if (distFromCam <= -spriteWorldWidth)
            {
                bg.position -= new Vector3(wrapDistance, 0f, 0f);
            }
        }
    }

    /// <summary>
    /// Orthographic 카메라의 세로 높이에 맞게 스프라이트 비율을 유지하며 스케일 업/다운
    /// </summary>
    private void ScaleChildrenToCameraHeight()
    {
        float worldScreenHeight = targetCamera.orthographicSize * 2.0f;

        foreach (Transform bg in backgroundSprites)
        {
            if (bg == null) continue;

            SpriteRenderer sr = bg.GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null) continue;

            float originalSpriteHeight = sr.sprite.bounds.size.y;
            float scaleY = worldScreenHeight / originalSpriteHeight;

            bg.localScale = new Vector3(scaleY, scaleY, 1f);
        }
    }
}