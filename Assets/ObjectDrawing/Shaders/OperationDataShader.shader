Shader "Hidden/ObjectDrawing/OperationDataShader"
{
    Properties
    {
        // Expose these here so the SRP Batcher validates the UnityPerMaterial block structure
        _G_MaxSliceSize ("Global Max Slice Size", Float) = 2048.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            ZWrite On
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Now perfectly valid because both variables exist in the Properties block above
            CBUFFER_START(UnityPerMaterial)
                float _G_MaxSliceSize;
            CBUFFER_END

            #ifdef UNITY_DOTS_INSTANCING_ENABLED
                UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
                    UNITY_DOTS_INSTANCED_PROP(float, _G_MaxSliceSize)
                UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

                //#define _G_MaxSliceSize UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float, _G_MaxSliceSize)
            #endif

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(v.vertex.xyz);
                o.pos = vertexInput.positionCS;
                o.uv0 = v.uv0;
                o.uv1 = v.uv1;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                uint rsuv = unity_RendererUserValue;
                float maxSliceSize = _G_MaxSliceSize;

                uint sliceIndex = ((rsuv >> 24) & 0xFF) - 1;
                uint textureIndex = ((rsuv >> 4)  & 0xFFFFF) - 1;
                uint textureSize = rsuv & 0xF;

                int gridSize = (int)(maxSliceSize / textureSize);
                int xIndex = textureIndex % gridSize;
                int yIndex = textureIndex / gridSize;
                float2 offset = float2(xIndex, yIndex) * (1.0f / gridSize);
                float2 tileUV = saturate((i.uv1 / gridSize) + offset);
                
                uint aData = (sliceIndex & 0x3F);

                float r = tileUV.x;
                float g = tileUV.y;
                float b = 0.0;
                float a = (float)aData / 65535.0;

                return float4(r, g, b, a);
            }
            ENDHLSL
        }
    }
}
