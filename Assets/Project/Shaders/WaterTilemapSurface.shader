Shader "Custom/WaterTilemapSurface"
{
    // TilemapRenderer(AutoTile/AnimatedTile 스프라이트)에 직접 물릴 수 있는 워터 셰이더.
    // URP "Sprite-Lit-Default"의 3-패스 구조(Universal2D / NormalsRendering / UniversalForward)를 그대로 따르되,
    // Universal2D 패스에서만 알베도 합성 단계에 반사/굴절 효과를 끼워 넣는다.
    // _SurfaceWorldY를 기준으로 화면을 상하 대칭시켜 "진짜 거울 반사"를 만든다(미러 카메라가 채워주는 _ReflectionTex 사용).
    // 청크마다 웅덩이가 여러 개, 높이가 다르면 이 기준면 하나로는 전부 정확히 맞출 수 없다는 한계가 있음 —
    // 현재는 화면에 웅덩이가 하나만 잡히는 상황(테스트 씬 등)을 우선 지원하는 트레이드오프.
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _MaskTex("Mask", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        [MaterialToggle] _ZWrite("ZWrite", Float) = 0

        _ReflectionTex("Reflection Texture (Mirror Camera RT)", 2D) = "black" {}
        _ReflectionOpacity("Reflection Opacity", Range(0, 1)) = 0.7
        _ReflectionTint("Reflection Darken/Tint", Color) = (0.5, 0.6, 0.7, 1)
        _WaterColor("Water Base Color", Color) = (0.06, 0.2, 0.3, 1)
        _RefractionAmount("Refraction Amount", Range(0, 1)) = 0.35
        _SurfaceWorldY("Water Surface World Y", Float) = 0
        _ReflectionFadeDistance("Reflection Fade Distance", Range(0.1, 10)) = 4
        _DistortionStrength("Ripple Distortion Strength", Range(0, 0.05)) = 0.006
        _WaveFrequency("Ripple Frequency", Range(0, 30)) = 8
        _WaveSpeed("Ripple Speed", Range(0, 10)) = 1.5
        _WaterEffectStrength("Water Effect Strength", Range(0, 1)) = 0.85
        _MinSurfaceAlpha("Min Water Opacity", Range(0, 1)) = 0.92

        _GlintNoiseTex("Glint Noise (fine-grained)", 2D) = "black" {}
        _GlintNoiseTiling("Glint Noise Tiling", Range(0.05, 10)) = 1
        _GlintVerticalSquash("Glint Vertical Squash", Range(1, 8)) = 3
        _GlintThreshold("Glint Noise Threshold", Range(0, 1)) = 0.78
        _GlintStrength("Glint Strength", Range(0, 1)) = 0.2

        _FlowDirection("Flow Direction", Vector) = (1, 0.2, 0, 0)
        _FlowSpeed("Flow Speed", Range(0, 2)) = 0.15
        _ShimmerStrength("Shimmer Strength", Range(0, 1)) = 0.15

        _NoiseTex("Surface Noise", 2D) = "gray" {}
        _NoiseTiling("Noise Tiling", Range(0.05, 5)) = 0.4
        _NoiseVerticalStretch("Noise Vertical Stretch", Range(0.05, 1)) = 0.3
        _NoiseStrength("Noise Distortion Strength", Range(0, 0.1)) = 0.02

        // Legacy properties. They're here so that materials using this shader can gracefully fallback to the legacy sprite shader.
        [HideInInspector] _Color("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _AlphaTex("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite [_ZWrite]

        Pass
        {
            Name "WaterTilemapUniversal2D"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex WaterLitVertex
            #pragma fragment WaterLitFragment

            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma multi_compile _ SKINNED_SPRITE

            TEXTURE2D(_ReflectionTex);
            SAMPLER(sampler_ReflectionTex);

            TEXTURE2D(_CameraSortingLayerTexture);
            SAMPLER(sampler_CameraSortingLayerTexture);

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            TEXTURE2D(_GlintNoiseTex);
            SAMPLER(sampler_GlintNoiseTex);

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float3 normal     : NORMAL;
                half4 color       : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv          : TEXCOORD0;
                half2 lightingUV   : TEXCOORD1;
                float3 positionWS  : TEXCOORD2;
                half4 color        : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // _MainTex/_MaskTex/_NormalMap 텍스처 선언 + CombinedShapeLightShared(Light2D 합성)를 재사용하기 위해 포함.
            // Attributes/Varyings가 이미 정의된 뒤에 include해야 함(Lit2DCommon.hlsl의 CommonLitVertex/Fragment가 이 타입들을 참조).
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Lit2DCommon.hlsl"

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _ReflectionOpacity;
                half4 _ReflectionTint;
                half4 _WaterColor;
                half _RefractionAmount;
                float _SurfaceWorldY;
                half _ReflectionFadeDistance;
                half _DistortionStrength;
                half _WaveFrequency;
                half _WaveSpeed;
                half _WaterEffectStrength;
                half _MinSurfaceAlpha;
                half4 _FlowDirection;
                half _FlowSpeed;
                half _ShimmerStrength;
                half _NoiseTiling;
                half _NoiseVerticalStretch;
                half _NoiseStrength;
                half _GlintNoiseTiling;
                half _GlintVerticalSquash;
                half _GlintThreshold;
                half _GlintStrength;
            CBUFFER_END

            Varyings WaterLitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);

                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                o.positionCS = TransformObjectToHClip(input.positionOS);
                o.positionWS = TransformObjectToWorld(input.positionOS);
                o.uv = input.uv;
                o.lightingUV = half2(ComputeScreenPos(o.positionCS / o.positionCS.w).xy);
                o.color = input.color * _Color * unity_SpriteColor;

                return o;
            }

            half4 WaterLitFragment(Varyings input) : SV_Target
            {
                half4 tileColor = input.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, input.uv);
                half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv));

                // 물 위상은 타일 UV가 아니라 월드 좌표 기준으로 계산해야 타일 경계에서 이음매 없이 이어진다.
                // 서로 다른 주파수/속도의 파동 두 개를 합쳐 제자리 출렁임이 아니라 유기적으로 흐르는 느낌을 만든다.
                float wave1 = sin(input.positionWS.x * _WaveFrequency + _Time.y * _WaveSpeed) * _DistortionStrength;
                float wave2 = sin((input.positionWS.x * 2.3 + input.positionWS.y * 1.7) * _WaveFrequency * 0.5 - _Time.y * _WaveSpeed * 1.3) * _DistortionStrength * 0.5;
                float ripple = wave1 + wave2;

                // flowOffset은 "물에 비친 상"이 아니라 노이즈 텍스처를 읽는 좌표를 패닝하는 데만 쓴다.
                // (반사/굴절 샘플 좌표에 그대로 더하면 비친 오브젝트 자체가 한 방향으로 흘러가버려서 안 됨 — 흘러야 하는 건 물 표면 무늬지, 비친 장면이 아니다.)
                // _Time.y를 그대로 오래 누적해서 곱하면(특히 _FlowSpeed가 크면) 부동소수점 정밀도가 깨져
                // 텍스처 좌표가 몇 개 값 사이를 널뛰기하며 줄무늬(barcode) 아티팩트가 생긴다. 주기적으로 감아준다.
                float2 flowDir = normalize(_FlowDirection.xy + 1e-5);
                float2 flowOffset = flowDir * fmod(_Time.y * _FlowSpeed, 1000.0);

                // 노이즈 텍스처를 서로 다른 타일링/속도/방향으로 두 번 패닝해서 샘플링(플로우맵 기법).
                // 사인파만 쓸 때보다 훨씬 불규칙하고 자연스러운 표면 왜곡이 나오고, 두 레이어가 서로 다르게 흘러 반복 패턴이 눈에 덜 띈다.
                // Y축만 _NoiseVerticalStretch(<1)로 눌러서 무늬가 세로로 길게 늘어난 것처럼 보이게 한다.
                float2 noiseWorldPos = input.positionWS.xy * float2(1, _NoiseVerticalStretch);
                float2 noiseUV1 = noiseWorldPos * _NoiseTiling + flowOffset * 0.5;
                float2 noiseUV2 = noiseWorldPos * _NoiseTiling * 1.7 - flowOffset.yx * 0.35 + float2(0.37, 0.71);
                half n1 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV1).r;
                half n2 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV2).r;
                float2 noiseOffset = (half2(n1, n2) * 2.0 - 1.0) * _NoiseStrength;

                // 반사/굴절 샘플 좌표에 더하는 왜곡은 시간이 지나도 계속 쌓이지 않고 제자리에서 오실레이션하는 값(파동+노이즈)만 사용한다.
                // 그래야 비친 장면(가게, 나무 등)은 제자리에 그대로 있고, 표면만 자잘하게 일렁인다.
                float2 distortion = noiseOffset + float2(ripple, ripple * 0.5 + wave2 * 0.3);

                // 반사: 미러 카메라와 같은 시점을 찍은 _ReflectionTex를, 물 표면(_SurfaceWorldY) 기준으로
                // 상하 대칭시킨 화면 좌표에서 샘플링해야 위쪽 장면이 뒤집혀 비치는 "진짜 반사"처럼 보인다.
                float3 reflectedWorldPos = input.positionWS;
                reflectedWorldPos.y = 2.0 * _SurfaceWorldY - reflectedWorldPos.y;
                float4 reflectedClipPos = TransformWorldToHClip(reflectedWorldPos);
                float4 reflectedScreenPos = ComputeScreenPos(reflectedClipPos);
                float2 reflectionUVRaw = reflectedScreenPos.xy / reflectedScreenPos.w + distortion;

                // 수면에서 깊이 내려갈수록 대칭된 샘플 위치가 미러 카메라가 실제로 찍은 화면 범위(0~1)를 벗어난다.
                // 이 경우 Wrap 설정과 무관하게 가장자리 픽셀이 반복/왜곡되어 같은 오브젝트가 여러 번 비치는 것처럼 보이므로,
                // 범위를 벗어나기 전에 반사 기여도를 0으로 페이드시키고 샘플 좌표 자체도 0~1로 clamp한다.
                half2 edgeFade = 1.0 - smoothstep(0.7, 1.0, abs(reflectionUVRaw * 2.0 - 1.0));
                half reflectionValidity = saturate(edgeFade.x * edgeFade.y);
                float2 reflectionUV = saturate(reflectionUVRaw);

                half4 reflection = SAMPLE_TEXTURE2D(_ReflectionTex, sampler_ReflectionTex, reflectionUV);

                // 반사를 원본 밝기 그대로 쓰면 또렷하고 쨍하게 보여서 "잔잔한 물" 느낌이 안 산다.
                // 어둡고 살짝 푸른 톤(_ReflectionTint)으로 눌러줘야 수면 아래로 가라앉은 듯한 느낌이 난다.
                reflection.rgb *= _ReflectionTint.rgb;

                // 굴절: 대칭시키지 않고 픽셀 자신의 화면 좌표에서 배경을 살짝 왜곡해 샘플링(수면 아래 비쳐 보이는 느낌).
                float2 refractionUVRaw = input.lightingUV + distortion * 0.6;

                // 반사와 마찬가지로, 왜곡 때문에 샘플 좌표가 0~1 범위를 벗어나면 clamp된 가장자리 픽셀이
                // 가로/세로 줄처럼 늘어나 보인다. 범위를 벗어나기 전에 굴절 기여도도 페이드시킨다.
                half2 refractEdgeFade = 1.0 - smoothstep(0.85, 1.0, abs(refractionUVRaw * 2.0 - 1.0));
                half refractionValidity = saturate(refractEdgeFade.x * refractEdgeFade.y);
                float2 refractionUV = saturate(refractionUVRaw);

                half3 refractionSample = SAMPLE_TEXTURE2D(_CameraSortingLayerTexture, sampler_CameraSortingLayerTexture, refractionUV).rgb;

                // 배경을 그대로 복사한 굴절색만 쓰면, 반사가 약해지는 순간 "물색"이 아니라 "배경 그 자체"가 나와서
                // 물이 아예 뚫려 보이는 것처럼 느껴진다. 항상 고유의 물색(_WaterColor)이 바탕에 깔리도록 섞어준다.
                // 가장자리에 걸쳐 clamp된 픽셀도 refractionValidity로 물색 쪽으로 자연스럽게 페이드된다.
                half3 refraction = lerp(_WaterColor.rgb, refractionSample, _RefractionAmount * refractionValidity);

                // 표면에서 멀어질수록(깊어질수록) 반사가 옅어지고 굴절 색으로 스며든다.
                float depthBelowSurface = max(0.0, _SurfaceWorldY - input.positionWS.y);
                float reflectionFade = saturate(1.0 - depthBelowSurface / _ReflectionFadeDistance);

                half3 waterLayer = lerp(refraction, reflection.rgb, reflectionFade * reflectionValidity * _ReflectionOpacity * reflection.a);

                // 물거품/윤슬 하이라이트: 왜곡용 노이즈(_NoiseTex, 부드러움)와는 별개로 촘촘하고 거친 전용 노이즈
                // (_GlintNoiseTex)를 써서 임계값을 넘는 지점에만 하얗게 더한다. 이건 UV를 휘게 만드는 게 아니라
                // 그 자리에서 값만 읽어 마스크로 쓰는 거라, 거친 노이즈를 써도 무아레 걱정 없이 콕콕 찍힌 반짝임이 나온다.
                // frac()으로 직접 0~1에 감아서 샘플링한다 — 이 텍스처의 Wrap Mode가 Clamp라서, 월드 좌표가
                // 조금만 커져도(플레이어가 움직이기만 해도) 범위를 벗어나 가장자리 픽셀이 줄줄이 늘어나 보이는 문제를 막는다.
                // Y축 타일링을 X축보다 더 크게 줘서 알갱이 하나하나가 세로로 눌린 모양이 되게 한다.
                float2 glintWorldPos = input.positionWS.xy * float2(1, _GlintVerticalSquash);
                float2 glintNoiseUV = frac(glintWorldPos * _GlintNoiseTiling + flowOffset);
                half glintNoise = SAMPLE_TEXTURE2D(_GlintNoiseTex, sampler_GlintNoiseTex, glintNoiseUV).r;
                // step() 대신 smoothstep()으로 완만하게 걸쳐지게 해서, 슬라이더를 조금 움직였을 때 "안 보임 ↔ 확 밝아짐"으로
                // 튀지 않고 서서히 늘어나도록 한다.
                half glint = smoothstep(_GlintThreshold - 0.08, _GlintThreshold + 0.08, glintNoise) * glintNoise * _GlintStrength;
                waterLayer += glint;

                // 파동 위상 + 노이즈에 맞춰 밝기를 살짝 흔들어 표면에 반짝이는 결(shimmer)을 더한다.
                half shimmer = 1.0 + (wave2 + (n1 * n2 - 0.25)) * _ShimmerStrength * 5.0;
                waterLayer *= shimmer;

                half3 albedo = lerp(tileColor.rgb, waterLayer, _WaterEffectStrength);

                // 물은 반투명 스프라이트가 아니라 "위쪽을 비추는 표면"이어야 한다.
                // 타일 실루엣(가장자리 안티에일리어싱)은 그대로 살리되, 그 안쪽은 알파를 밀어올려 사실상 불투명하게 만든다.
                // 그래야 뒤에 있는 실제 배경이 그대로 비쳐 보이지 않고, 우리가 계산한 반사/굴절 색만 채워진다.
                half finalAlpha = tileColor.a > 0.003 ? max(tileColor.a, _MinSurfaceAlpha) : 0.0;

                SurfaceData2D surfaceData;
                InputData2D inputData;

                InitializeSurfaceData(albedo, finalAlpha, mask, normalTS, surfaceData);
                InitializeInputData(input.uv, input.lightingUV, inputData);

#if defined(DEBUG_DISPLAY)
                SETUP_DEBUG_TEXTURE_DATA_2D_NO_TS(inputData, input.positionWS, input.positionCS, _MainTex);
                surfaceData.normalWS = half3(0, 0, 1);
#endif

                return CombinedShapeLightShared(surfaceData, inputData);
            }
            ENDHLSL
        }

        Pass
        {
            Name "WaterTilemapNormalsRendering"
            Tags { "LightMode" = "NormalsRendering"}

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex NormalsRenderingVertex
            #pragma fragment NormalsRenderingFragment

            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_NORMALS_INPUTS
                float4 color        : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_NORMALS_OUTPUTS
                half4   color           : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Normals2DCommon.hlsl"

            CBUFFER_START( UnityPerMaterial )
                half4 _Color;
            CBUFFER_END

            Varyings NormalsRenderingVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings o = CommonNormalsVertex(input);
                o.color = input.color * _Color * unity_SpriteColor;

                return o;
            }

            half4 NormalsRenderingFragment(Varyings input) : SV_Target
            {
                return CommonNormalsFragment(input, input.color);
            }
            ENDHLSL
        }

        Pass
        {
            Name "WaterTilemapForwardFallback"
            Tags { "LightMode" = "UniversalForward" "Queue"="Transparent" "RenderType"="Transparent"}

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex UnlitVertex
            #pragma fragment UnlitFragment

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"

            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
            CBUFFER_END

            Varyings UnlitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings o = CommonUnlitVertex(input);
                o.color = input.color *_Color * unity_SpriteColor;
                return o;
            }

            half4 UnlitFragment(Varyings input) : SV_Target
            {
                return CommonUnlitFragment(input, input.color);
            }
            ENDHLSL
        }
    }
}
