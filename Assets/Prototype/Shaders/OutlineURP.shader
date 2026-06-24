Shader "Hidden/Custom/OutlineURP"
{
    Properties { }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Name "OutlinePass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            float4 _OutlineColor;
            float _OutlineScale;
            float _DistanceFalloff;
            float _MinOutlineScale;
            float _DepthThreshold;
            float _NormalThreshold;
            float _GrazingTolerance; // Variabel perbaikan baru
            
            SAMPLER(sampler_BlitTexture);
            
            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                float2 uv = input.texcoord;
                float4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv);

                float rawCenterDepth = SampleSceneDepth(uv);
                float linearEyeDepth = LinearEyeDepth(rawCenterDepth, _ZBufferParams);

                float distanceScale = 1.0 / (1.0 + linearEyeDepth * _DistanceFalloff * 0.01);
                float dynamicScale = max(_MinOutlineScale, _OutlineScale * distanceScale);

                float2 texelSize = float2(1.0 / _ScreenParams.x, 1.0 / _ScreenParams.y);
                float halfScale = dynamicScale * 0.5;

                float2 uv0 = uv - texelSize * halfScale;
                float2 uv1 = uv + texelSize * halfScale;
                float2 uv2 = uv + float2(texelSize.x * halfScale, -texelSize.y * halfScale);
                float2 uv3 = uv + float2(-texelSize.x * halfScale, texelSize.y * halfScale);

                float depth0 = SampleSceneDepth(uv0);
                float depth1 = SampleSceneDepth(uv1);
                float depth2 = SampleSceneDepth(uv2);
                float depth3 = SampleSceneDepth(uv3);

                float3 normal0 = SampleSceneNormals(uv0);
                float3 normal1 = SampleSceneNormals(uv1);
                float3 normal2 = SampleSceneNormals(uv2);
                float3 normal3 = SampleSceneNormals(uv3);

                float depthDiff0 = depth1 - depth0;
                float depthDiff1 = depth3 - depth2;
                float edgeDepth = sqrt(pow(depthDiff0, 2) + pow(depthDiff1, 2)) * 100.0;
                
                // --- PERBAIKAN GRAZING ANGLE ---
                // 1. Ubah Normal World Space menjadi View Space (Arah relatif terhadap Kamera)
                float3 normalVS = TransformWorldToViewDir(normal0, true);
                
                // 2. Jika normalVS.z = -1, artinya menghadap ke arah kita. Jika = 0, artinya menyamping.
                // Menggunakan abs() menjaga agar nilainya aman, sehingga multiplier = 0 saat menghadap kita, dan 1 saat menyamping.
                float grazingMultiplier = 1.0 - saturate(abs(normalVS.z));
                
                // 3. Modifikasi kedalaman toleransi. Semakin miring sudutnya, batas threshold semakin besar.
                float depthThreshold = _DepthThreshold * depth0 * (1.0 + grazingMultiplier * _GrazingTolerance);
                edgeDepth = edgeDepth > depthThreshold ? 1.0 : 0.0;
                // --------------------------------

                float3 normalDiff0 = normal1 - normal0;
                float3 normalDiff1 = normal3 - normal2;
                float edgeNormal = sqrt(dot(normalDiff0, normalDiff0) + dot(normalDiff1, normalDiff1));
                edgeNormal = edgeNormal > _NormalThreshold ? 1.0 : 0.0;

                float edge = max(edgeDepth, edgeNormal);
                float4 outlineResult = float4(_OutlineColor.rgb, _OutlineColor.a * edge);
                
                return float4(lerp(color.rgb, outlineResult.rgb, outlineResult.a), color.a);
            }
            ENDHLSL
        }
    }
}