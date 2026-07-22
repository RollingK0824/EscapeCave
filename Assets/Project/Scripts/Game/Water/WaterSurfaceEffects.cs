using UnityEngine;

public class WaterSurfaceEffects : MonoBehaviour
{
    [SerializeField] private Renderer _renderer;

    private static readonly int RippleOriginXId = Shader.PropertyToID("_RippleOriginX");
    private static readonly int RippleStartTimeId = Shader.PropertyToID("_RippleStartTime");

    private void Awake()
    {
        _renderer.sortingLayerName = "Water";
    }

    public void TriggerRipple(float worldX)
    {
        _renderer.sharedMaterial.SetFloat(RippleOriginXId, worldX);
        _renderer.sharedMaterial.SetFloat(RippleStartTimeId, Time.time);
    }
}