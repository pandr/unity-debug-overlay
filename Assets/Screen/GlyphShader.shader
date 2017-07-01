Shader "Instanced/GlyphShader" {
	Properties{
		_MainTex("Albedo (RGB)", 2D) = "white" {}
	}
	SubShader{

		Pass{

			Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }

			ZWrite off
			ZTest Always
			Cull off
			//Blend One One
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
			#pragma target 4.5

			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float sdx;
			float sdy;
			float tdx;
			float tdy;

			struct instanceData
			{
				float4 position;
				float4 size;
				float4 color;
			};

			StructuredBuffer<instanceData> positionBuffer;

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv_MainTex : TEXCOORD0;
				float4 color : TEXCOORD3;
			};

			v2f vert(appdata_full v, uint instanceID : SV_InstanceID)
			{
				float4 data = positionBuffer[instanceID].position;

				float2 uv = (data.zw + v.texcoord) * float2(tdx, tdy);
				uv.y = 1.0 - uv.y;
				float2 pos = data.xy;
				float2 scale = positionBuffer[instanceID].size.xy;
				float4 color = positionBuffer[instanceID].color;
				/*
				if (pos.x < 0)
				{
					pos.xy = -pos.xy;
					scale.xy = -data.zw;
					uv = v.texcoord;
				}
				*/


				v2f o;
				float2 p = (v.vertex.xy*scale + pos)*float2(sdx, sdy);
				p = float2(-1, -1) + p*2.0;
				o.pos = float4(p.xy, 1, 1);
				o.uv_MainTex = uv;
				o.color = color;
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 albedo = fixed4(1,1,1,1);
				if (length(i.uv_MainTex) > 0)
				{
					albedo = tex2D(_MainTex, i.uv_MainTex);
					albedo = lerp(albedo, fixed4(1, 1, 1, 1), i.color.a);
				}
				fixed4 output = albedo * float4(i.color.rgb, 1);
				return output;
			}

		ENDCG
	}
	}
}
