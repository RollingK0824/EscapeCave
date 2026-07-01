Shader "Hidden/Custom/FullscreenEcholocation"
{
    HLSLINCLUDE
    // URP의 기본 라이브러리 및 Fullscreen Blit용 라이브러리 포함
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

    // C#에서 전달받을 전역 변수들
    CBUFFER_START(UnityPerMaterial)
        float4 _PingCenterUV; // x, y는 화면 UV 좌표, z는 화면 비율(Aspect Ratio)
        float _PingRadiusUV;  // 화면 비율에 맞게 변환된 반지름
        float _PingWidthUV;   // 화면 비율에 맞게 변환된 파동 두께
        float4 _BaseColor;
        float4 _WaveColor;
    CBUFFER_END

    half4 frag (Varyings input) : SV_Target
    {
        // 1. 현재 화면(카메라가 렌더링한 최종 결과물)의 픽셀 색상 가져오기
        half4 screenColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord);

        // 2. 화면 비율(Aspect Ratio)을 보정하여 찌그러지지 않는 정원(Circle) 거리 계산
        float2 uv = input.texcoord;
        uv.x *= _PingCenterUV.z; 
        
        float2 center = _PingCenterUV.xy;
        center.x *= _PingCenterUV.z;

        float dist = distance(uv, center);

        // 3. 파동 마스크 생성
        float waveMask = smoothstep(_PingRadiusUV - _PingWidthUV, _PingRadiusUV, dist) * step(dist, _PingRadiusUV);

        // 4. 색상 합성: 기본 암전 색상과 원래 화면 색상을 섞음
        half3 finalRGB = lerp(_BaseColor.rgb, screenColor.rgb, waveMask);

        // 5. 파동 경계선 하이라이트 (두께 조절: 0.02)
        float edgeMask = smoothstep(_PingRadiusUV - 0.02, _PingRadiusUV, dist) * step(dist, _PingRadiusUV);
        finalRGB += _WaveColor.rgb * edgeMask * 0.7;

        return half4(finalRGB, screenColor.a);
    }
    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "EcholocationFullscreenPass"
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
            // 꼭짓점 셰이더는 Blit.hlsl에 내장된 Vert 함수를 그대로 사용
            #pragma vertex Vert 
            #pragma fragment frag
            ENDHLSL
        }
    }
}