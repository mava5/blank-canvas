
// The same as OperationDataShader, but drawing only applies to previous paint
Shader "Hidden/ObjectDrawing/PaintOnlyOperationDataShader"
{
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        Pass
        {
            ZWrite On
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "UnityCG.cginc"

            float _G_MaxSliceSize;
            uint _SliceIndex;
            uint _TextureIndex;
            uint _TextureSize;
            uint _ObjectIndex;

            UNITY_DECLARE_TEX2DARRAY(_G_DrawingArrayTexture);

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
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv0 = v.uv0;
                o.uv1 = v.uv1;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                // Calculate the atlas tile UVs
                int gridSize = (int)(_G_MaxSliceSize / _TextureSize);
                int xIndex = _TextureIndex % gridSize;
                int yIndex = _TextureIndex / gridSize;
                float2 offset = float2(xIndex, yIndex) * (1.0f / gridSize);
                float2 tileUV = saturate((i.uv1 / gridSize) + offset);
                
                // Get paint information from global drawing texture array
                float prevPaintInfo = _G_DrawingArrayTexture.Load(uint4(tileUV * _G_MaxSliceSize, _SliceIndex, 0)).r;
                uint prevPaintPackedData = (uint)(prevPaintInfo * 255.0);
                uint prevColorIndex = (prevPaintPackedData >> 5) & 0x7;
                uint prevAmount = prevPaintPackedData & 0x1F;
                
                // Clip pixels that have 0 amount
                clip(prevAmount > 0 ? 1 : -1);
                
                // Combine buffer index and slice index to A-channel
                uint aPacked = ((_ObjectIndex & 0x3FF) << 6) | (_SliceIndex & 0x3F);
                
                float r = tileUV.r;
                float g = tileUV.g;
                float b = 0.0;
                float a = (float)aPacked / 65535.0;

                return float4(r, g, b, a);
            }
            ENDHLSL
        }
    }
}
