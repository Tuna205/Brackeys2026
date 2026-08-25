Shader "CustomPass/S_QuantizeColor"
{

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "CopyColor"
            ZWrite On
            ZTest Always
            Cull Off
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

            CBUFFER_START(UnityPerMaterial)
                half _Steps;
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
                half4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, IN.uv);
                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "QuantizeColor"
            ZWrite On
            ZTest Always
            Cull Off
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float QuantizeAA(float value, float steps)
            {
                float scaled = value * steps;
                float width = fwidth(scaled);

                float lower = floor(scaled);
                float fraction = scaled - lower;

                float transition = smoothstep(
                0.5 - width,
                0.5 + width,
                fraction
                );

                return (lower + transition) / steps;
            }
            
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

            CBUFFER_START(UnityPerMaterial)
                half _Steps;
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

                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, IN.uv);
                half luminance = dot(color.rgb, half3(0.299, 0.587, 0.114));
                half3 normalizedColor = color.rgb / max(luminance, 0.001);
                // Quantize luminance and color separately
                half quantizedLum = QuantizeAA(luminance, _Steps*5);
                //quantizedLum = lerp(0.02, 1, quantizedLum);
                half3 quantizedColor;
                quantizedColor.r = QuantizeAA(normalizedColor.r, _Steps);
                quantizedColor.g = QuantizeAA(normalizedColor.g, _Steps);
                quantizedColor.b = QuantizeAA(normalizedColor.b, _Steps);
                half3 result = quantizedColor * quantizedLum;
                return half4(result, color.a);
            }

            ENDHLSL
        }
    }
}
