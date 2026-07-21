Shader "Custom/WaterSurface"
{
   Properties
{
    _WaterColor("Water Tint", Color) = (0.05, 0.25, 0.35, 0.65)
    _ReflectionTex("Reflection Texture (Mirror Camera RT)", 2D) = "black" {}
    _ReflectionOpacity("Reflection Opacity", Range(0, 1)) = 0.5
    _ReflectionFadeDistance("Reflection Fade Distance", Range(0.1, 10)) = 4
    _DistortionStrength("Ripple Distortion Strength", Range(0, 0.05)) = 0.01
    _WaveFrequency("Ripple Frequency", Range(0, 30)) = 8
    _WaveSpeed("Ripple Speed", Range(0, 10)) = 1.5
    _SurfaceWorldY("Water Surface World Y", Float) = 0
    _RefractionStrength("Underwater Refraction Strength", Range(0, 0.1)) = 0.02
    _UnderwaterTint("Underwater Tint", Color) = (0.0, 0.15, 0.2, 0.4)
    _RippleOriginX("Ripple Origin X", Float) = 0
    _RippleStartTime("Ripple Start Time", Float) = -100
    _RippleAmplitude("Ripple Amplitude", Range(0, 0.1)) = 0.03
    _RippleSpeed("Ripple Wave Speed", Range(0, 10)) = 4
    _RippleFrequency("Ripple Wave Frequency", Range(0, 10)) = 3
    _RippleDamping("Ripple Damping", Range(0, 5)) = 1
}

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

   CBUFFER_START(UnityPerMaterial)
    float4 _WaterColor;
    float _ReflectionOpacity;
    float _ReflectionFadeDistance;
    float _DistortionStrength;
    float _WaveFrequency;
    float _WaveSpeed;
    float _SurfaceWorldY;
    float _RefractionStrength;
    float4 _UnderwaterTint;
    float _RippleOriginX;
    float _RippleStartTime;
    float _RippleAmplitude;
    float _RippleSpeed;
    float _RippleFrequency;
    float _RippleDamping;
CBUFFER_END

    TEXTURE2D(_ReflectionTex);
    SAMPLER(sampler_ReflectionTex);

    TEXTURE2D(_CameraSortingLayerTexture);
    SAMPLER(sampler_CameraSortingLayerTexture);

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

    float rippleDist = abs(input.positionWS.x - _RippleOriginX);
    float rippleElapsed = (_Time.y - _RippleStartTime) * _RippleSpeed - rippleDist;
    float rippleWave = 0;
    if (rippleElapsed > 0)
    {
        rippleWave = _RippleAmplitude * exp(-_RippleDamping * rippleDist) * exp(-_RippleDamping * rippleElapsed) * sin(rippleElapsed * _RippleFrequency);
    }

    float2 screenUV = reflectedScreenPos.xy / reflectedScreenPos.w;
    screenUV += float2(ripple + rippleWave, (ripple + rippleWave) * 0.5);

    half4 reflection = SAMPLE_TEXTURE2D(_ReflectionTex, sampler_ReflectionTex, screenUV);

    // 수면에서 멀어질수록(깊어질수록) 반사가 옅어지며 물 색으로 스며든다.
    float depthBelowSurface = max(0.0, _SurfaceWorldY - input.positionWS.y);
    float reflectionFade = saturate(1.0 - depthBelowSurface / _ReflectionFadeDistance);

   half3 reflectionColor = lerp(_WaterColor.rgb, reflection.rgb, _ReflectionOpacity * reflection.a);

    // 물 표면 아래(언더워터) 영역: 배경을 굴절시켜서 보여준다.
    float4 selfClipPos = TransformWorldToHClip(input.positionWS);
    float4 selfScreenPos = ComputeScreenPos(selfClipPos);
    float2 opaqueUV = selfScreenPos.xy / selfScreenPos.w;
    opaqueUV += float2(sin(input.positionWS.y * _WaveFrequency + _Time.y * _WaveSpeed) * _RefractionStrength + rippleWave, rippleWave * 0.5);

    half3 underwaterColor = SAMPLE_TEXTURE2D(_CameraSortingLayerTexture, sampler_CameraSortingLayerTexture, opaqueUV).rgb;
    underwaterColor = lerp(underwaterColor, _UnderwaterTint.rgb, _UnderwaterTint.a);

    // 수면 근처는 반사, 깊어질수록 언더워터로 자연스럽게 섞인다.
    half3 finalColor = lerp(underwaterColor, reflectionColor, reflectionFade);

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
