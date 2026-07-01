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
    // URP 필수 헤더
    // Core.hlsl : UNITY_MATRIX_MVP 등 기본 행렬, 플랫폼 추상화 매크로 제공
    // Blit.hlsl : Vert 함수 + Varying 구조체 + _BlitTexture 제공
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

    // CBUFFER 블록
    CBUFFER_START(UnityPerMaterial)
        float4 _BaseColor;
        float4 _WaveColor;
        float _WaveHighlightIntensity;
    CBUFFER_END

    TEXTURE2D(_MaskTex);
    SAMPLER(sampler_MaskTex);

    half4 frag (Varyings input) : SV_Target
    {
        // 화면 원본 색상
        half4 screenColor = SAMPLE_TEXTURE2D_X(
            _BlitTexture,
            sampler_LinearClamp,
            input.texcoord
        );

        // 마스크 읽기
        half4 maskColor = SAMPLE_TEXTURE2D(
            _MaskTex,
            sampler_MaskTex,
            input.texcoord
        );

        float waveIntensity = maskColor.r;

        // 휘도 기반 오브젝트 감지
        float luminance = dot(screenColor.rgb , float3(0.299, 0.587, 0.114));
        // 오브젝트가 있는지 판단 (원본 밝기 기준)
        float hasObject = step(0.01, luminance);

        // 오브젝트가 있으면 원본 색상, 없으면 WaveColor 틴트
        half3 revealedColor = lerp(
            _WaveColor.rgb * _WaveHighlightIntensity, 
            screenColor.rgb, 
            hasObject
    );

    // 파동 강도로 암전과 합성
    // 파동 없음(waveIntensity=0): BaseColor(검정) = 완전 암전
    // 파동 있음(waveIntensity=1): revealedColor = 오브젝트 or 틴트
    half3 finalRGB = lerp(_BaseColor.rgb, revealedColor, waveIntensity);

    return half4(finalRGB, screenColor.a);
}
    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "EcholocationRTPass"
            
            ZWrite Off  // ZWrite Off : Depth Buffer에 쓰기 비활성화 (깊이 테스트는 수행하지만 깊이 값은 기록하지 않음)
            ZTest Always// ZTest Always : 항상 통과 (깊이 테스트를 무시하고 항상 렌더링)
            Blend Off   // Blend Off : 블렌딩 비활성화 (기본적으로 덮어쓰기)
            Cull Off    // Cull Off : 폴리곤 방향 무시 (풀스크린 삼각형은 방향이 없음)

            HLSLPROGRAM
            #pragma vertex Vert 
            #pragma fragment frag
            ENDHLSL
        }
    }
}