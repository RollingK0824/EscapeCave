using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 공용 Renderer2D 에셋에 등록된 태양 갓레이 Full Screen Pass Renderer Feature를
/// 이 오브젝트가 활성화된 동안(엔딩 씬)에만 켜고, 비활성화되면 꺼서 다른 씬으로 새는 것을 막습니다.
/// </summary>
public class EndingSunRaysToggle : MonoBehaviour
{
    [SerializeField] private ScriptableRendererFeature _sunGodRaysFeature;

    private void OnEnable()
    {
        if (_sunGodRaysFeature != null)
        {
            _sunGodRaysFeature.SetActive(true);
        }
    }

    private void OnDisable()
    {
        if (_sunGodRaysFeature != null)
        {
            _sunGodRaysFeature.SetActive(false);
        }
    }
}
