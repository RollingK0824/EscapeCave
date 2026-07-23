Shader "Hidden/Custom/FullScreenEcholocation_PerceptionSobel"
{
    Properties
    {
        [NoScaleOffset] _MaskTex("Mask Texture (Render Texture)", 2D) = "black" {}
        _BaseColor("Base Darkness", Color) = (0, 0, 0, 1)
        [HDR] _WaveColor("Wave Highlight", Color) = (0, 1, 1, 1)
        _WaveHighlightIntensity("Wave Highlight Intensity", Range(0, 3)) = 1.2
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

    float GetLuminance(half3 color)
    {
        return dot(color, float3(0.2126, 0.7152, 0.0722));
    }

    half4 frag (Varyings input) : SV_Target
    {
        // 1. 화면 원본 색상 샘플링
        half4 screenColor = SAMPLE_TEXTURE2D_X(
            _BlitTexture,
            sampler_LinearClamp,
            input.texcoord
        );

        // 2. Screen-Space Sobel Edge Detection (스크린 공간 인접 8개 텍셀 읽기 및 외곽선 추출)
        float2 texel = _BlitTexture_TexelSize.xy * 1.2;

        float l00 = GetLuminance(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord + float2(-texel.x, -texel.y)).rgb);
        float l10 = GetLuminance(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord + float2( 0.0,     -texel.y)).rgb);
        float l20 = GetLuminance(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord + float2( texel.x, -texel.y)).rgb);

        float l01 = GetLuminance(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord + float2(-texel.x,  0.0)).rgb);
        float l21 = GetLuminance(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord + float2( texel.x,  0.0)).rgb);

        float l02 = GetLuminance(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord + float2(-texel.x,  texel.y)).rgb);
        float l12 = GetLuminance(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord + float2( 0.0,      texel.y)).rgb);
        float l22 = GetLuminance(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord + float2( texel.x,  texel.y)).rgb);

        // Sobel 필터 그래디언트 강도
        float gx = -l00 - 2.0 * l01 - l02 + l20 + 2.0 * l21 + l22;
        float gy = -l00 - 2.0 * l10 - l20 + l02 + 2.0 * l12 + l22;
        float sobelEdge = saturate(sqrt(gx * gx + gy * gy) * 3.5);

        // 3. 미세 음향 굴곡(Wiggle) UV 마스크 샘플링
        float2 wiggleUV = input.texcoord;
        float wiggle = sin(input.texcoord.y * 60.0 + _Time.y * 18.0) * 0.0025;
        wiggleUV.x += wiggle;

        half4 maskColor = SAMPLE_TEXTURE2D(
            _MaskTex,
            sampler_MaskTex,
            wiggleUV
        );

        float waveIntensity = maskColor.r;
        float sharpWave = pow(saturate(waveIntensity), 1.5);

        // 4. Perception 본연의 네온 와이어프레임(Wireframe) 발광:
        // 파동이 스치는 지형의 평면(Flat Face)은 어둡게(20% 알베도) 누르고, 추출된 지형/오브젝트의 외곽선만 HDR 청록색(_WaveColor)으로 네온처럼 빛나게 연산
        half3 wireframeGlow = _WaveColor.rgb * _WaveHighlightIntensity * (sobelEdge * 2.8 + 0.15);
        half3 revealedColor = screenColor.rgb * 0.2 + wireframeGlow;

        // 5. 암흑 지대와 음향 스캔 지대 최종 보간
        half3 finalRGB = lerp(
            _BaseColor.rgb,
            revealedColor,
            sharpWave
        );

        return half4(finalRGB, screenColor.a);
    }

    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "EcholocationPerceptionSobelPass"
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
            #pragma vertex Vert 
            #pragma fragment frag
            ENDHLSL
        }
    }
}
