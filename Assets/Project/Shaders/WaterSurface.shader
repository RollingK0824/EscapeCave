Shader "Custom/WaterSurface"
{
    Properties
    {
        _WaterColor("Water Tint", Color) = (0.05, 0.25, 0.35, 0.65)
        _ReflectionTex("Reflection Texture (Mirror Camera RT)", 2D) = "black" {}
        _ReflectionOpacity("Reflection Opacity", Range(0, 1)) = 0.5
        _DistortionStrength("Ripple Distortion Strength", Range(0, 0.05)) = 0.01
        _WaveFrequency("Ripple Frequency", Range(0, 30)) = 8
        _WaveSpeed("Ripple Speed", Range(0, 10)) = 1.5
        _EdgeColor("Surface Edge Highlight", Color) = (1, 1, 1, 1)
        _EdgeThickness("Surface Edge Thickness", Range(0, 0.3)) = 0.06
        _SurfaceWorldY("Water Surface World Y", Float) = 0
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    CBUFFER_START(UnityPerMaterial)
        float4 _WaterColor;
        float4 _EdgeColor;
        float _ReflectionOpacity;
        float _DistortionStrength;
        float _WaveFrequency;
        float _WaveSpeed;
        float _EdgeThickness;
        float _SurfaceWorldY;
    CBUFFER_END

    TEXTURE2D(_ReflectionTex);
    SAMPLER(sampler_ReflectionTex);

    struct Attributes
    {
        float4 positionOS : POSITION;
        float2 uv : TEXCOORD0;
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float2 uv : TEXCOORD0;
        float3 positionWS : TEXCOORD1;
    };

    Varyings vert(Attributes input)
    {
        Varyings output;
        output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
        output.positionCS = TransformWorldToHClip(output.positionWS);
        output.uv = input.uv;
        return output;
    }

    half4 frag(Varyings input) : SV_Target
    {
        // 미러 카메라는 메인 카메라와 같은 시점을 찍으므로, 실제 프래그먼트 위치가 아니라
        // 물 표면(_SurfaceWorldY) 기준으로 대칭시킨 위치의 화면 좌표를 계산해 샘플링해야 반사로 보인다.
        float3 reflectedWorldPos = input.positionWS;
        reflectedWorldPos.y = 2.0 * _SurfaceWorldY - reflectedWorldPos.y;

        float4 reflectedClipPos = TransformWorldToHClip(reflectedWorldPos);
        float4 reflectedScreenPos = ComputeScreenPos(reflectedClipPos);

        float ripple = sin(input.positionWS.x * _WaveFrequency + _Time.y * _WaveSpeed) * _DistortionStrength;

        float2 screenUV = reflectedScreenPos.xy / reflectedScreenPos.w;
        screenUV += float2(ripple, ripple * 0.5);

        half4 reflection = SAMPLE_TEXTURE2D(_ReflectionTex, sampler_ReflectionTex, screenUV);

        half3 baseColor = lerp(_WaterColor.rgb, reflection.rgb, _ReflectionOpacity * reflection.a);

        float edge = smoothstep(1.0 - _EdgeThickness, 1.0, input.uv.y);
        half3 finalColor = lerp(baseColor, _EdgeColor.rgb, edge * _EdgeColor.a);

        return half4(finalColor, _WaterColor.a);
    }
    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }

        Pass
        {
            Name "WaterSurfaceForward"

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
    }
}
