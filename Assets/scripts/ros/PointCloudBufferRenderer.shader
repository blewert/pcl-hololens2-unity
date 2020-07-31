/**
 *
*/

Shader "UoL/PointCloud2 ComputeBuffer Renderer (direct-to-shader)"
{
	Properties
	{
		_Tint("Tint", Color) = (0.5, 0.5, 0.5, 1)
		_PointSize("Point size", Float) = 0.05
		_PointMultMagnitude("Point magnitude multiplier", Vector) = (1, 0.2, 1, 1)
		[Toggle] _Distance ("Apply Distance", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM

			//Define vert/frag
            #pragma vertex vert
            #pragma fragment frag
			//--
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 pos  : POSITION;
                half3 color : COLOR;
            };

            struct v2f
            {
				float4 position : SV_Position;
				half3 color : COLOR;
				half psize : PSIZE;
				UNITY_FOG_COORDS(0)
            };

			//Uniforms for rendering
			uniform half4 _Tint;
			uniform float4x4 _Transform;
			uniform half _PointSize;
			uniform half3 _PointMultMagnitude;

			//Uniforms for buffers
			StructuredBuffer<float3> _Vertices;
			StructuredBuffer<float4> _Colors;

            v2f vert (uint vid : SV_VertexID)
            {
				float3 pt = _Vertices[vid];
				float4 pos = mul(_Transform, float4(pt.xyz, 1));
				half3 col = _Colors[vid];

				col *= _Tint.rgb * 2;

				v2f o;

				//hadamard it
				pos.x *= _PointMultMagnitude.x;
				pos.y *= _PointMultMagnitude.y;
				pos.z *= _PointMultMagnitude.z;

				o.position = UnityObjectToClipPos(pos);
				o.color = col;

				o.psize = _PointSize;

				UNITY_TRANSFER_FOG(o, o.position);
				return o;
            }

			fixed4 frag(v2f i) : SV_Target
			{
				half4 c = half4(i.color, _Tint.a);
				UNITY_APPLY_FOG(i.fogCoord, c);
				return c;
            }
            ENDCG
        }
    }
}
