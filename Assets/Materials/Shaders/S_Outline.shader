Shader "CustomPass/S_Outline"
{
    SubShader
    {
        Tags {"RenderPipeline" = "UniversalPipeline"}
        Pass
        {
            Name "Outline"
            ZWrite On
            ZTest Less
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D_X(_BlitTexture);

            TEXTURE2D_X(_DepthTexture);

            CBUFFER_START(UnityPerMaterial)
                float _NormalThreshold;
                float _DepthThreshold;
                float2 _TexelSize;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = GetFullScreenTriangleVertexPosition(IN.vertexID);
                OUT.uv = GetFullScreenTriangleTexCoord(IN.vertexID);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                int outlineRadius = 1;
                half4 normal = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, IN.uv);
                //normal = normal * 2 - 1;
                float rawDepth = SAMPLE_TEXTURE2D_X(_DepthTexture, sampler_LinearClamp, IN.uv).r;
                float linearDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                float sampleOffsetDistance = (saturate(1 - linearDepth * .1), .5, 1);
                half edgeNormal = 0.0;
                half depthEdge = 0.0;
                for(int y = -outlineRadius; y <= outlineRadius; y++)
                {
                    for(int x = -outlineRadius; x <= outlineRadius; x++)
                    {
                        float2 offset = float2(x,y) * _TexelSize * sampleOffsetDistance;
                        
                        half4 neighbourNormal = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, IN.uv + offset);
                        //neighbourNormal = neighbourNormal * 2 - 1;
                        
                        half normalDifference = 1.0 - saturate(dot(normalize(normal.rgb), normalize(neighbourNormal.rgb)));
                        edgeNormal = max(edgeNormal, normalDifference);
                        
                        half neighbourDepth = SAMPLE_TEXTURE2D_X(_DepthTexture, sampler_LinearClamp, IN.uv + offset).r;
                        neighbourDepth = LinearEyeDepth(neighbourDepth, _ZBufferParams);
                        
                        float depthDifference = saturate(abs(neighbourDepth - linearDepth)) / linearDepth;
                        depthEdge = step(_DepthThreshold, depthDifference);
                        depthEdge = max(depthEdge, depthDifference);
                    }
                }
                edgeNormal = step(_NormalThreshold, edgeNormal);
                half edge = max(edgeNormal, depthEdge);

                return half4(0,0,0,edge);
            }

            ENDHLSL
        }
    }
}