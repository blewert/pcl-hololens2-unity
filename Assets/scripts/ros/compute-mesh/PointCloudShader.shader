/**
 * @file      PointCloudShader.shader/cg
 * @author    Benjamin Williams <bwilliams@lincoln.ac.uk>
 *
 * @brief     A modified version of inmo-jang's PointCloudShader. 
*/

Shader "Custom/PointCloudShader"
{
	Properties
	{
		_PointSize("PointSize", Float) = 1
		_PointMultMagnitude("Magnitude", Vector) = (1,1,1,1)
	}

	SubShader
	{
		Pass
		{
			LOD 200

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			struct VertexInput
			{
				float4 v : POSITION;
				float4 color: COLOR;
			};

			struct VertexOutput
			{
				float4 pos : SV_POSITION;
				float4 col : COLOR;
				float psize : PSIZE;
			};

			uniform float _PointSize;
			uniform float4 _PointMultMagnitude;

			VertexOutput vert(VertexInput v)
			{
				//Hadamard object-space point by uniform
				v.v.x *= _PointMultMagnitude.x;
				v.v.y *= _PointMultMagnitude.y;
				v.v.z *= _PointMultMagnitude.z;

				//Setup output struct
				VertexOutput o;
				o.pos = UnityObjectToClipPos(v.v);
				o.psize = _PointSize;
				o.col = v.color;

				//And return it
				return o;
			}

			float4 frag(VertexOutput o) : COLOR
			{
				//Very dark? ok discard this pixel:
				if ((o.col.r + o.col.g + o.col.b) < 0.01)
					discard;

				return o.col;
			}

			ENDCG
		}
	}
}