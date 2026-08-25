Shader "Custom/S_PropMaster"
{
    Properties
    {
        [MainColor] _TintColor("Tint Color", Color) = (1, 1, 1, 1)
        [MainTexture] _DiffuseMap("Diffuse Map", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        _NormalStrength("Normal Strength", Range(0,2)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 uv          : TEXCOORD0;

                float3 positionWS  : TEXCOORD1;
                float3 normalWS    : TEXCOORD2;
                float4 tangentWS   : TEXCOORD3;
            };

            TEXTURE2D(_DiffuseMap);
            SAMPLER(sampler_DiffuseMap);

            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            CBUFFER_START(UnityPerMaterial)

                half4 _TintColor;

                float4 _DiffuseMap_ST;
                float4 _NormalMap_ST;

                float _NormalStrength;

            CBUFFER_END


            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                OUT.positionHCS =
                    TransformObjectToHClip(IN.positionOS.xyz);

                // Diffuse UV
                OUT.uv.xy =
                    TRANSFORM_TEX(
                        IN.uv,
                        _DiffuseMap
                    );

                // Normal UV
                OUT.uv.zw =
                    TRANSFORM_TEX(
                        IN.uv,
                        _NormalMap
                    );

                // Object → World normal
                OUT.normalWS =
                    TransformObjectToWorldNormal(
                        IN.normalOS
                    );

                // Object → World tangent
                OUT.tangentWS.xyz =
                    TransformObjectToWorldDir(
                        IN.tangentOS.xyz
                    );

                // Tangent handedness
                OUT.tangentWS.w =
                    IN.tangentOS.w;

                return OUT;
            }


            half4 frag(Varyings IN) : SV_Target
            {
                // --------------------------------
                // Diffuse
                // --------------------------------

                half4 diffuse =
                    SAMPLE_TEXTURE2D(
                        _DiffuseMap,
                        sampler_DiffuseMap,
                        IN.uv.xy
                    ) * _TintColor;


                // --------------------------------
                // Normal map
                // --------------------------------

                half4 normalSample =
                    SAMPLE_TEXTURE2D(
                        _NormalMap,
                        sampler_NormalMap,
                        IN.uv.zw
                    );

                half3 normalTS =
                    UnpackNormalScale(
                        normalSample,
                        _NormalStrength
                    );


                // --------------------------------
                // Tangent → World
                // --------------------------------

                float3 N =
                    normalize(IN.normalWS);

                float3 T =
                    normalize(IN.tangentWS.xyz);

                float tangentSign =
                    IN.tangentWS.w *
                    GetOddNegativeScale();

                float3 B =
                    normalize(
                        cross(N, T) *
                        tangentSign
                    );

                float3x3 TBN =
                    float3x3(T, B, N);

                float3 normalWS =
                    normalize(
                        mul(normalTS, TBN)
                    );


                // --------------------------------
                // Temporary visualization
                // --------------------------------

                return half4(
                    normalWS * 0.5 + 0.5,
                    1
                );
            }

            ENDHLSL
        }
    }
}