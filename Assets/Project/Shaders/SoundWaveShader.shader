Shader "Custom/SoundWaveShader"
{
    Properties
    {
        [HideInInspector] _MainTex("Base", 2D) = "white" {}
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline" 
        }
        
        Cull Off 
        ZWrite Off 
        ZTest Always

        Pass
        {
            Name "SoundWaveFullscreenPass"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            TEXTURE2D_X(_BlitTexture);
            SAMPLER(sampler_BlitTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _WaveCenters[5];
                float _WaveRadii[5];
            CBUFFER_END

            Varyings vert(uint vertexID : SV_VertexID)
            {
                Varyings OUT;
                
                float4 positions[3] = {
                    float4(-1.0, -1.0, 0.0, 1.0),
                    float4(-1.0,  3.0, 0.0, 1.0),
                    float4( 3.0, -1.0, 0.0, 1.0)
                };
                
                float2 uvs[3] = {
                    float2(0.0, 0.0), 
                    float2(0.0, 2.0), 
                    float2(2.0, 0.0)
                };

                OUT.positionCS = positions[vertexID];
                OUT.uv = uvs[vertexID];

                // ★ [Y축 뒤집힘 해결] Blit 시 뒤집히는 UV를 정상으로 돌려놓습니다.
                OUT.uv.y = 1.0 - OUT.uv.y;
                
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 1. 원본 화면 색상 샘플링
                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, IN.uv);

                // 2. 스크린 -> 2D 월드 좌표 역산
                float4 ndcPos = float4(IN.uv * 2.0 - 1.0, 0.0, 1.0);
                #if UNITY_UV_STARTS_AT_TOP
                ndcPos.y = -ndcPos.y;
                #endif
                
                float4 decodedWorldPos = mul(UNITY_MATRIX_I_VP, ndcPos);
                float2 worldPos2D = decodedWorldPos.xy / decodedWorldPos.w;

                float totalMask = 0.0;

                // 3. 5개 파동 마스킹 연산
                for(int j = 0; j < 5; j++)
                {
                    if(_WaveRadii[j] < 0.0) continue;

                    float dist = distance(worldPos2D, _WaveCenters[j].xy);
                    
                    // 파동의 두께 및 선명도 제어 (1.5 값을 올릴수록 파동 선이 얇고 날카로워집니다)
                    float waveMask = saturate(1.0 - abs(dist - _WaveRadii[j]) * 1.5);
                    totalMask = max(totalMask, waveMask);
                }

                // 4. [완벽한 암전 로직으로 수정]
                // 파동 마스크가 없는 곳은 완전히 시커먼 블랙(0,0,0)으로 밀어버립니다.
                // 만약 완전 블랙이 너무 답답해서 실루엣만 0.1% 주고 싶다면 맨 아래 주석을 해제하세요.
                
                half3 finalRGB = color.rgb * totalMask;

                // 예외 처리: 만약 아예 파동이 하나도 활성화 안 된 에디터 상태라면 쌩 블랙으로 밀기
                if (totalMask <= 0.0)
                {
                    finalRGB = half3(0.0, 0.0, 0.0); // 완전히 아무것도 안 보이는 상태
                    
                    // (선택 사항) 만약 미세하게 형체만 보이고 싶다면 아래 코드 주석을 풀고 위 0.0을 지우세요.
                    // finalRGB = color.rgb * 0.02; 
                }

                return half4(finalRGB, color.a);
            }
            ENDHLSL
        }
    }
}