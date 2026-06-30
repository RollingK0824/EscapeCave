Shader "Hidden/Custom/FullScreenEcholocationRT"
{
    Properties
    {
        // C#이 아닌 Material Inspector에서 직접 할당하도록 노출 (매우 안전함)
        [NoScaleOffset] _MaskTex("Mask Texture (Render Texture)", 2D) = "black" {}
        _BaseColor("Base Darkness", Color) = (0, 0, 0, 1)
        [HDR] _WaveColor("Wave Highlight", Color) = (0, 1, 1, 1)
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

    // CBUFFER 블록을 제거하고 일반 변수로 받아옵니다.
    sampler2D _MaskTex;
    float4 _BaseColor;
    float4 _WaveColor;

    half4 frag (Varyings input) : SV_Target
    {
        // 1. 화면 원본 색상
        half4 screenColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord);
        
        // 2. 렌더 텍스처 (마스크) 색상 읽기
        half4 maskColor = tex2D(_MaskTex, input.texcoord);

        // 3. 렌더 텍스처의 붉은색(Red) 채널을 마스크 강도로 사용
        float waveIntensity = maskColor.r;

        // 4. 색상 합성
        half3 finalRGB = lerp(_BaseColor.rgb, screenColor.rgb, waveIntensity);
        finalRGB += _WaveColor.rgb * waveIntensity * 0.5;

        // 원본 화면의 알파값을 그대로 유지
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