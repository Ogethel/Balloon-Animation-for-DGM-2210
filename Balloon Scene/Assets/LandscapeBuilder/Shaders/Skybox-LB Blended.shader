// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Skybox/LB Blended" {
    Properties {
	_Tint ("Tint Color", Color) = (.5, .5, .5, .5)
    _Blend ("Blend", Range(0.0,1.0)) = 0.5
    _Rotation ("Rotation", Range(0, 360)) = 0
	[Gamma] _Exposure ("Exposure", Range(0, 8)) = 1.0
	[NoScaleOffset] _FrontTex ("Front [+Z]   (HDR)", 2D) = "grey" {}
	[NoScaleOffset] _BackTex ("Back [-Z]   (HDR)", 2D) = "grey" {}
	[NoScaleOffset] _LeftTex ("Left [+X]   (HDR)", 2D) = "grey" {}
	[NoScaleOffset] _RightTex ("Right [-X]   (HDR)", 2D) = "grey" {}
	[NoScaleOffset] _UpTex ("Up [+Y]   (HDR)", 2D) = "grey" {}
	[NoScaleOffset] _DownTex ("Down [-Y]   (HDR)", 2D) = "grey" {}
    [NoScaleOffset] _FrontTex2 ("Front [+Z] 2 (HDR)", 2D) = "grey" {}
	[NoScaleOffset] _BackTex2 ("Back [-Z] 2 (HDR)", 2D) = "grey" {}
	[NoScaleOffset] _LeftTex2 ("Left [+X] 2 (HDR)", 2D) = "grey" {}
	[NoScaleOffset] _RightTex2 ("Right [-X] 2 (HDR)", 2D) = "grey" {}
	[NoScaleOffset] _UpTex2 ("Up [+Y] 2 (HDR)", 2D) = "grey" {}
	[NoScaleOffset] _DownTex2 ("Down [-Y] 2 (HDR)", 2D) = "grey" {}
}

SubShader {
	Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
	Cull Off ZWrite Off
	
	CGINCLUDE
	#include "UnityCG.cginc"

	half4 _Tint;
	half _Exposure;
	float _Rotation;

	float4 RotateAroundYInDegrees (float4 vertex, float degrees)
	{
		float alpha = degrees * UNITY_PI / 180.0;
		float sina, cosa;
		sincos(alpha, sina, cosa);
		float2x2 m = float2x2(cosa, -sina, sina, cosa);
		return float4(mul(m, vertex.xz), vertex.yw).xzyw;
	}
	
	struct appdata_t {
		float4 vertex : POSITION;
		float2 texcoord : TEXCOORD0;
	};
	struct v2f {
		float4 vertex : SV_POSITION;
		float2 texcoord : TEXCOORD0;
	};
	v2f vert (appdata_t v)
	{
		v2f o;
		o.vertex = UnityObjectToClipPos(RotateAroundYInDegrees(v.vertex, _Rotation));
		o.texcoord = v.texcoord;
		return o;
	}
	half4 skybox_frag (v2f i, sampler2D smp, half4 smpDecode)
	{
		half4 tex = tex2D (smp, i.texcoord);
		half3 c = DecodeHDR (tex, smpDecode);
		c = c * _Tint.rgb * unity_ColorSpaceDouble.rgb;
		c *= _Exposure;
		return half4(c, 1);
	}
	ENDCG
	
	Pass {
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		sampler2D _FrontTex;
		half4 _FrontTex_HDR;
        sampler2D _FrontTex2;
		half4 _FrontTex2_HDR;
        half _Blend;
		half4 frag (v2f i) : SV_Target { return lerp(skybox_frag(i,_FrontTex, _FrontTex_HDR), skybox_frag(i,_FrontTex2, _FrontTex2_HDR), _Blend); }
		ENDCG 
	}
	Pass{
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		sampler2D _BackTex;
		half4 _BackTex_HDR;
        sampler2D _BackTex2;
		half4 _BackTex2_HDR;
        half _Blend;
		half4 frag (v2f i) : SV_Target { return lerp(skybox_frag(i,_BackTex, _BackTex_HDR), skybox_frag(i,_BackTex2, _BackTex2_HDR), _Blend); }
		ENDCG 
	}
	Pass{
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		sampler2D _LeftTex;
		half4 _LeftTex_HDR;
        sampler2D _LeftTex2;
		half4 _LeftTex2_HDR;
        half _Blend;
		half4 frag (v2f i) : SV_Target { return lerp(skybox_frag(i,_LeftTex, _LeftTex_HDR), skybox_frag(i,_LeftTex2, _LeftTex2_HDR), _Blend); }
		ENDCG
	}
	Pass{
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		sampler2D _RightTex;
		half4 _RightTex_HDR;
        sampler2D _RightTex2;
		half4 _RightTex2_HDR;
        half _Blend;
		half4 frag (v2f i) : SV_Target { return lerp(skybox_frag(i,_RightTex, _RightTex_HDR), skybox_frag(i,_RightTex2, _RightTex2_HDR), _Blend); }
		ENDCG
	}	
	Pass{
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		sampler2D _UpTex;
		half4 _UpTex_HDR;
        sampler2D _UpTex2;
		half4 _UpTex2_HDR;
        half _Blend;
		half4 frag (v2f i) : SV_Target { return lerp(skybox_frag(i,_UpTex, _UpTex_HDR), skybox_frag(i,_UpTex2, _UpTex2_HDR), _Blend); }
		ENDCG
	}	
	Pass{
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		sampler2D _DownTex;
		half4 _DownTex_HDR;
        sampler2D _DownTex2;
		half4 _DownTex2_HDR;
        half _Blend;
		half4 frag (v2f i) : SV_Target { return lerp(skybox_frag(i,_DownTex, _DownTex_HDR), skybox_frag(i,_DownTex2, _DownTex2_HDR), _Blend); }
		ENDCG
	}
}
}
