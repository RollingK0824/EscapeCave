using UnityEngine;
using UnityEngine.UI;
using Managers;

/// <summary>
/// 옵션 UI의 볼륨 슬라이더를 SoundManager의 BGM 마스터 볼륨에 연동한다.
/// VolumeSlider(Slider) 오브젝트에 부착하면 자동으로 연결된다.
/// </summary>
[RequireComponent(typeof(Slider))]
public class BGMVolumeSlider : MonoBehaviour
{
    private Slider _slider;

    private void Awake()
    {
        _slider = GetComponent<Slider>();
    }

    private void Start()
    {
        // 저장된 현재 BGM 볼륨으로 슬라이더 위치를 맞춘다 (콜백 발생 없이)
        _slider.SetValueWithoutNotify(SoundManager.Instance.BGMVolume);
        _slider.onValueChanged.AddListener(OnSliderChanged);
    }

    private void OnDestroy()
    {
        if (_slider != null)
        {
            _slider.onValueChanged.RemoveListener(OnSliderChanged);
        }
    }

    private void OnSliderChanged(float value)
    {
        SoundManager.Instance.SetBGMVolume(value);
    }
}
