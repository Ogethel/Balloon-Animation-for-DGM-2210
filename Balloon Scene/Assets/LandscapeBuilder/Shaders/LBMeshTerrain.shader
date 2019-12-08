Shader "Landscape Builder/LBMeshTerrain"
{
	Properties
	{
		_Splat0TexPkd("Splat0 Texture", 2D) = "white" {}
		_Splat1TexPkd("Splat1 Texture", 2D) = "white" {}
		//_Splat2TexPkd("Splat2 Texture", 2D) = "white" {}

		_Splat0TexR("Landscape Texture1", 2D) = "white" {}
		_Splat0TexRNM("    Normalmap", 2D) = "bump" {}
		_Splat0TexR_Metallic("    Metallic", Range(0,1)) = 0.0
		_Splat0TexR_Smoothness("    Smoothness", Range(0,1)) = 0.0
		_Splat0TexG("Landscape Texture2", 2D) = "white" {}
		_Splat0TexGNM("    Normalmap", 2D) = "bump" {}
		_Splat0TexG_Metallic("    Metallic", Range(0,1)) = 0.0
		_Splat0TexG_Smoothness("    Smoothness", Range(0,1)) = 0.0
		_Splat0TexB("Landscape Texture3", 2D) = "white" {}
		_Splat0TexBNM("    Normalmap", 2D) = "bump" {}
		_Splat0TexB_Metallic("    Metallic", Range(0,1)) = 0.0
		_Splat0TexB_Smoothness("    Smoothness", Range(0,1)) = 0.0
		_Splat0TexA("Landscape Texture4", 2D) = "white" {}
		_Splat0TexANM("    Normalmap", 2D) = "bump" {}
		_Splat0TexA_Metallic("    Metallic", Range(0,1)) = 0.0
		_Splat0TexA_Smoothness("    Smoothness", Range(0,1)) = 0.0

		_Splat1TexR("Landscape Texture5", 2D) = "white" {}
		_Splat1TexRNM("    Normalmap", 2D) = "bump" {}
		_Splat1TexR_Metallic("    Metallic", Range(0,1)) = 0.0
		_SplatTexR_Smoothness("    Smoothness", Range(0,1)) = 0.0
		_Splat1TexG("Landscape Texture6", 2D) = "white" {}
		_Splat1TexGNM("    Normalmap", 2D) = "bump" {}
		_Splat1TexG_Metallic("    Metallic", Range(0,1)) = 0.0
		_Splat1TexG_Smoothness("    Smoothness", Range(0,1)) = 0.0
		_Splat1TexB("Landscape Texture7", 2D) = "white" {}
		_Splat1TexBNM("    Normalmap", 2D) = "bump" {}
		_Splat1TexB_Metallic("    Metallic", Range(0,1)) = 0.0
		_Splat1TexB_Smoothness("    Smoothness", Range(0,1)) = 0.0
		_Splat1TexA("Landscape Texture8", 2D) = "white" {}
		_Splat1TexANM("    Normalmap", 2D) = "bump" {}
		_Splat1TexA_Metallic("    Metallic", Range(0,1)) = 0.0
		_Splat1TexA_Smoothness("    Smoothness", Range(0,1)) = 0.0
	}

	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		// First Pass Splat0 Tex0-2 (Texture 1,2,3)
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0
		// needs more than 8 texcoords
		#pragma exclude_renderers gles
		#include "UnityPBSLighting.cginc"

		// The packed RGBA texture extracted from the landscape
		sampler2D _Splat0TexPkd;

		// Splat0TexR,G,B are the first active 3 textures in the landscape (taken from LB Texturing tab)
		sampler2D _Splat0TexR, _Splat0TexG, _Splat0TexB;
		sampler2D _Splat0TexRNM, _Splat0TexGNM, _Splat0TexBNM;

		half _Splat0TexR_Metallic, _Splat0TexG_Metallic, _Splat0TexB_Metallic;
		half _Splat0TexR_Smoothness, _Splat0TexG_Smoothness, _Splat0TexB_Smoothness;

		struct Input
		{
			float2 uv_Splat0TexPkd : TEXCOORD0;
			float2 uv_Splat0TexR : TEXCOORD1;
			float2 uv_Splat0TexG : TEXCOORD2;
			float2 uv_Splat0TexB : TEXCOORD3;
		};

		void surf (Input IN, inout SurfaceOutputStandard o)
		{
			fixed4 splat0 = tex2D(_Splat0TexPkd, IN.uv_Splat0TexPkd);

			// Extract the data from the landscape Textures and multiple by packed rgba values
			fixed4 newColour = 0.0;
			newColour += tex2D(_Splat0TexR, IN.uv_Splat0TexR) * splat0.r;
			newColour += tex2D(_Splat0TexG, IN.uv_Splat0TexG) * splat0.g;
			newColour += tex2D(_Splat0TexB, IN.uv_Splat0TexB) * splat0.b;
			newColour.a = 1.0;

			fixed4 nrm = 0.0f;
			nrm += tex2D(_Splat0TexRNM, IN.uv_Splat0TexR) * splat0.r;
			nrm += tex2D(_Splat0TexGNM, IN.uv_Splat0TexG) * splat0.g;
			nrm += tex2D(_Splat0TexBNM, IN.uv_Splat0TexB) * splat0.b;
			o.Normal = UnpackNormal(nrm);

			o.Albedo = newColour.rgb;
			// Metallic and smoothness
			o.Metallic = (_Splat0TexR_Metallic * splat0.r) + (_Splat0TexG_Metallic * splat0.g) + (_Splat0TexB_Metallic * splat0.b);
			o.Smoothness = (_Splat0TexR_Smoothness * splat0.r) + (_Splat0TexG_Smoothness * splat0.g) + (_Splat0TexB_Smoothness * splat0.b);
			o.Alpha = 1.0;
		}
		ENDCG

		// Second pass - Splat0 Tex 3, Splat1, Tex 0 (Texture 4,5)
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		// This is an additive pass, so set decal:add
		#pragma surface surf Standard decal:add fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0
		// needs more than 8 texcoords
		#pragma exclude_renderers gles
		#include "UnityPBSLighting.cginc"

		#pragma multi_compile __ USE_SPLAT1

		// The packed RGBA texture extracted from the landscape
		sampler2D _Splat0TexPkd;
		#if USE_SPLAT1
		sampler2D _Splat1TexPkd;
		#endif

		// Splat0TexR,G,B are the first active 3 textures in the landscape (taken from LB Texturing tab)
		sampler2D _Splat0TexA, _Splat0TexANM;
		#if USE_SPLAT1
		sampler2D _Splat1TexR, _Splat1TexRNM;
		#endif

		half _Splat0TexA_Metallic, _Splat0TexA_Smoothness;
		#if USE_SPLAT1
		half _Splat1TexR_Metallic, _Splat1TexR_Smoothness;
		#endif

		struct Input
		{
			float2 uv_Splat0TexPkd : TEXCOORD0;
			float2 uv_Splat0TexA : TEXCOORD1;
			#if USE_SPLAT1
			float2 uv_Splat1TexPkd : TEXCOORD2;
			float2 uv_Splat1TexR : TEXCOORD3;
			#endif
		};

		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			fixed4 splat0 = tex2D(_Splat0TexPkd, IN.uv_Splat0TexPkd);
			#if USE_SPLAT1
			fixed4 splat1 = tex2D(_Splat1TexPkd, IN.uv_Splat1TexPkd);
			#endif

			// Extract the data from the landscape Textures and multiple by packed rgba values
			fixed4 newColour = 0.0;
			newColour += tex2D(_Splat0TexA, IN.uv_Splat0TexA) * splat0.a;

			#if USE_SPLAT1
			newColour += tex2D(_Splat1TexR, IN.uv_Splat1TexR) * splat1.r;
			#endif

			newColour.a = 1.0;

			fixed4 nrm = 0.0f;
			nrm += tex2D(_Splat0TexANM, IN.uv_Splat0TexA) * splat0.a;
			#if USE_SPLAT1
			nrm += tex2D(_Splat1TexRNM, IN.uv_Splat1TexR) * splat1.r;
			#endif
			o.Normal = UnpackNormal(nrm);

			o.Albedo = newColour.rgb;
			// Metallic and smoothness
			#if USE_SPLAT1
			o.Metallic = (_Splat0TexA_Metallic * splat0.a) + (_Splat1TexR_Metallic * splat1.r);
			o.Smoothness = (_Splat0TexA_Smoothness * splat0.a) + (_Splat1TexR_Smoothness * splat1.r);
			#else
			o.Metallic = (_Splat0TexA_Metallic * splat0.a);
			o.Smoothness = (_Splat0TexA_Smoothness * splat0.a);
			#endif

			o.Alpha = 1.0;
		}
		ENDCG

		// Third pass - Splat1 Tex 1-3 (Texture 6,7,8)
		//CGPROGRAM

		//// Physically based Standard lighting model, and enable shadows on all light types
		//// This is an additive pass, so set decal:add
		//#pragma surface surf Standard decal:add fullforwardshadows

		//// Use shader model 3.0 target, to get nicer looking lighting
		//#pragma target 3.0
		//// needs more than 8 texcoords
		//#pragma exclude_renderers gles
		//#include "UnityPBSLighting.cginc"

		//#pragma multi_compile __ USE_SPLAT1
		//
		//// The packed RGBA texture extracted from the landscape
		//sampler2D _Splat1TexPkd;

		//// Splat1TexG,B,A are active textures 6,7,8 in the landscape (taken from LB Texturing tab)
		//sampler2D _Splat1TexG, _Splat1TexGNM, _Splat1TexB, _Splat1TexBNM, _Splat1TexA, _Splat1TexANM;

		//half _Splat1TexG_Metallic, _Splat1TexB_Metallic, _Splat1TexA_Metallic;
		//half _Splat1TexG_Smoothness, _Splat1TexB_Smoothness, _Splat1TexA_Smoothness;

		//struct Input
		//{
		//	float2 uv_Splat1TexPkd : TEXCOORD0;
		//	float2 uv_Splat1TexG : TEXCOORD1;
		//	float2 uv_Splat1TexB : TEXCOORD2;
		//	float2 uv_Splat1TexA : TEXCOORD2;
		//};

		//#if USE_SPLAT1
		//void surf(Input IN, inout SurfaceOutputStandard o)
		//{
		//	fixed4 splat1 = tex2D(_Splat1TexPkd, IN.uv_Splat1TexPkd);

		//	// Extract the data from the landscape Textures and multiple by packed rgba values
		//	fixed4 newColour = 0.0;
		//	newColour += tex2D(_Splat1TexG, IN.uv_Splat1TexG) * splat1.g;
		//	newColour += tex2D(_Splat1TexB, IN.uv_Splat1TexB) * splat1.b;
		//	newColour += tex2D(_Splat1TexA, IN.uv_Splat1TexA) * splat1.a;

		//	newColour.a = 1.0;

		//	fixed4 nrm = 0.0f;
		//	nrm += tex2D(_Splat1TexGNM, IN.uv_Splat1TexG) * splat1.g;
		//	nrm += tex2D(_Splat1TexBNM, IN.uv_Splat1TexB) * splat1.b;
		//	nrm += tex2D(_Splat1TexANM, IN.uv_Splat1TexA) * splat1.a;
		//	o.Normal = UnpackNormal(nrm);

		//	o.Albedo = newColour.rgb;
		//	// Metallic and smoothness
		//	o.Metallic = (_Splat1TexG_Metallic * splat1.g) + (_Splat1TexB_Metallic * splat1.b) + (_Splat1TexA_Metallic * splat1.a);
		//	o.Smoothness = (_Splat1TexG_Smoothness * splat1.g) + (_Splat1TexB_Smoothness * splat1.b) + (_Splat1TexA_Smoothness * splat1.a);

		//	o.Alpha = 1.0;
		//}
		//#else

		//void surf(Input IN, inout SurfaceOutputStandard o)
		//{

		//}

		//#endif
		//ENDCG

	}
	FallBack "Diffuse"
}
