Shader "Hidden/Custom/FullScreenEcholocationRT"
{
    Properties
    {
        [NoScaleOffset] _MaskTex("Mask Texture (Render Texture)", 2D) = "black" {}
        _BaseColor("Base Darkness", Color) = (0, 0, 0, 1)
        [HDR] _WaveColor("Wave Highlight", Color) = (0, 1, 1, 1)
        _WaveHighlightIntensity("Wave Highlight Intensity", Range(0, 1)) = 0.4
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

    CBUFFER_START(UnityPerMaterial)
        float4 _BaseColor;
        float4 _WaveColor;
        float _WaveHighlightIntensity;
    CBUFFER_END

    TEXTURE2D(_MaskTex);
    SAMPLER(sampler_MaskTex);

    half4 frag (Varyings input) : SV_Target
    {
        half4 screenColor = SAMPLE_TEXTURE2D_X(
            _BlitTexture,
            sampler_LinearClamp,
            input.texcoord
        );

        half4 maskColor = SAMPLE_TEXTURE2D(
            _MaskTex,
            sampler_MaskTex,
            input.texcoord
        );

        float waveIntensity = maskColor.r;
        float edge = saturate(fwidth(waveIntensity) * 8.0);

        half3 revealedColor = screenColor.rgb + _WaveColor.rgb * _WaveHighlightIntensity * edge;

        half3 finalRGB = lerp(
            _BaseColor.rgb,
            revealedColor,
            waveIntensity
        );

        return half4(finalRGB, screenColor.a);
    }

    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "EcholocationRTPass"
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
            #pragma vertex Vert 
            #pragma fragment frag
            ENDHLSL
        }
    }
}