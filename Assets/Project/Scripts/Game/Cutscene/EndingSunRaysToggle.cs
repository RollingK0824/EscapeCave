using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 공용 Renderer2D 에셋에 등록된 태양 갓레이 Full Screen Pass Renderer Feature를
/// 이 오브젝트가 활성화된 동안(엔딩 씬)에만 켜고, 비활성화되면 꺼서 다른 씬으로 새는 것을 막습니다.
/// 에코로케이션 마스크 피처는 EchoManager가 이전 씬에서 켜둔 상태로 남아있을 수 있어
/// 엔딩 씬에 들어오는 동안엔 강제로 꺼둡니다.
/// </summary>
public class EndingSunRaysToggle : MonoBehaviour
{
    [SerializeField] private ScriptableRendererFeature _sunGodRaysFeature;
    [SerializeField] private ScriptableRendererFeature _echolocationMaskFeature;

    private void OnEnable()
    {
        if (_sunGodRaysFeature != null)
        {
            _sunGodRaysFeature.SetActive(true);
        }

        if (_echolocationMaskFeature != null)
        {
            _echolocationMaskFeature.SetActive(false);
        }
    }

    private void OnDisable()
    {
        if (_sunGodRaysFeature != null)
        {
            _sunGodRaysFeature.SetActive(false);
        }

        // 엔딩 씬을 벗어날 때 EchoManager가 다시 사용할 수 있도록 마스크 피처를 원래대로 켜둔다
        if (_echolocationMaskFeature != null)
        {
            _echolocationMaskFeature.SetActive(true);
        }
    }
}
