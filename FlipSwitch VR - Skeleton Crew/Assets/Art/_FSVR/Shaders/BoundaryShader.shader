Shader "FlipSwitch/Boundary - Alpha Flash" {
	Properties{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo (RGB)", 2D) = "white" {}

		_Speed("Sin Speed", Range(1,100)) = 2
		_Min("Min Alpha", Range(0,1)) = .5
			_Max("Max Alpha", Range(0,1)) = 1


		_Color2("Color", Color) = (1,1,1,1)

		_MainTex2("moving (RGB)", 2D) = "white" {}
		_ScrollXSpeed("X Scroll Speed", Range(0, 10)) = 2
		_ScrollYSpeed("Y Scroll Speed", Range(0, 10)) = 2

	}
	SubShader {
		// Transparent (ordering)
		Tags
		{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
		}

		LOD 200

		// Removes back geometry if culled
		Cull Off
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		CGPROGRAM
		// Alpha blending
		#pragma surface surf Unlit alpha:fade
		


		sampler2D _MainTex;
		sampler2D _MainTex2;

		fixed4 _Color;
		fixed4 _Color2;

		float _Speed;
		float _Min;
		float _Max;

		fixed _ScrollXSpeed;
		fixed _ScrollYSpeed;


		struct Input {
			float2 uv_MainTex;
		};

		half4 LightingUnlit(SurfaceOutput s, half3 lightDir, half atten)
		{
			return half4(s.Albedo, s.Alpha);
		}

		void surf(Input IN, inout SurfaceOutput o) {
			float4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;

			fixed2 scrolledUV = IN.uv_MainTex;
			fixed xScrollValue = _ScrollXSpeed * _Time;
			fixed yScrollValue = _ScrollYSpeed * _Time;

			scrolledUV += fixed2(xScrollValue, yScrollValue);
			half4 c2 = tex2D(_MainTex2, scrolledUV) * _Color2;

			o.Albedo = c.rgb + c2.rgb;
			o.Alpha = min( min(_Min + abs(sin(_Time * _Speed)), c.a), _Max);
		}
		ENDCG
	}
	FallBack "Diffuse"
}