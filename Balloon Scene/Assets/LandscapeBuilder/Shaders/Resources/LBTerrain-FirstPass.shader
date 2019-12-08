Shader "Nature/Terrain/LB Standard" 
{
	Properties 
	{
		// Set by terrain engine
		[HideInInspector] _Control ("Control (RGBA)", 2D) = "red" {}
		[HideInInspector] _Splat3 ("Layer 3 (A)", 2D) = "white" {}
		[HideInInspector] _Splat2 ("Layer 2 (B)", 2D) = "white" {}
		[HideInInspector] _Splat1 ("Layer 1 (G)", 2D) = "white" {}
		[HideInInspector] _Splat0 ("Layer 0 (R)", 2D) = "white" {}
		[HideInInspector] _Normal3 ("Normal 3 (A)", 2D) = "bump" {}
		[HideInInspector] _Normal2 ("Normal 2 (B)", 2D) = "bump" {}
		[HideInInspector] _Normal1 ("Normal 1 (G)", 2D) = "bump" {}
		[HideInInspector] _Normal0 ("Normal 0 (R)", 2D) = "bump" {}
		[HideInInspector] [Gamma] _Metallic0 ("Metallic 0", Range(0.0, 1.0)) = 0.0	
		[HideInInspector] [Gamma] _Metallic1 ("Metallic 1", Range(0.0, 1.0)) = 0.0	
		[HideInInspector] [Gamma] _Metallic2 ("Metallic 2", Range(0.0, 1.0)) = 0.0	
		[HideInInspector] [Gamma] _Metallic3 ("Metallic 3", Range(0.0, 1.0)) = 0.0
		[HideInInspector] _Smoothness0 ("Smoothness 0", Range(0.0, 1.0)) = 1.0	
		[HideInInspector] _Smoothness1 ("Smoothness 1", Range(0.0, 1.0)) = 1.0	
		[HideInInspector] _Smoothness2 ("Smoothness 2", Range(0.0, 1.0)) = 1.0	
		[HideInInspector] _Smoothness3 ("Smoothness 3", Range(0.0, 1.0)) = 1.0
		
		[HideInInspector] _TerrainWidth ("Terrain Width", float) = 2000.0

		// Used in fallback on old cards & base map
		[HideInInspector] _MainTex ("BaseMap (RGB)", 2D) = "white" {}
		[HideInInspector] _Color ("Main Color", Color) = (1,1,1,1)
	}

	SubShader 
	{
		Tags 
		{
			"SplatCount" = "4"
			"Queue" = "Geometry-100"
			"RenderType" = "Opaque"
		}

		CGPROGRAM
		#pragma surface surf Standard vertex:SplatmapVert finalcolor:SplatmapFinalColor finalgbuffer:SplatmapFinalGBuffer fullforwardshadows
		//#pragma surface surf Standard fullforwardshadows
		#pragma multi_compile_fog
		#pragma target 3.0
		// needs more than 8 texcoords
		#pragma exclude_renderers gles
		#include "UnityPBSLighting.cginc"

		#pragma multi_compile __ _TERRAIN_NORMAL_MAP

		#define TERRAIN_STANDARD_SHADER
		#define TERRAIN_SURFACE_OUTPUT SurfaceOutputStandard
		//#include "TerrainSplatmapCommon.cginc"

		#define TRIPLANAR_SHADING_
		
		// Get input
		struct Input
		{
			#ifdef TRIPLANAR_SHADING
				// Get world position and world normal
				float3 worldPos;
				float3 worldNormal;
				INTERNAL_DATA
			#else
				float3 worldPos;
				// Get texture coordinates
				float2 uv_Splat0 : TEXCOORD0;
				float2 uv_Splat1 : TEXCOORD1;
				float2 uv_Splat2 : TEXCOORD2;
				float2 uv_Splat3 : TEXCOORD3;
			#endif
			
			float2 tc_Control : TEXCOORD4;	// Not prefixing '_Control' with 'uv' allows a tighter packing of interpolators,
			// which is necessary to support directional lightmap.
			// Get fog coordinates
			UNITY_FOG_COORDS(5)
		};
		
		// Get references to stuff declared in properties
		
		// The "_ST" properties are float4s that Unity automatically creates for each texture
		// The first 2 values are tiling and the last 2 are scale offset
		sampler2D _Control;
		float4 _Control_ST;
		sampler2D _Splat0,_Splat1,_Splat2,_Splat3;
		#ifdef TRIPLANAR_SHADING
		float4 _Splat0_ST,_Splat1_ST,_Splat2_ST,_Splat3_ST;
		#endif

		#ifdef _TERRAIN_NORMAL_MAP
			sampler2D _Normal0, _Normal1, _Normal2, _Normal3;
			float4 _Normal0_ST, _Normal1_ST, _Normal2_ST, _Normal3_ST;
		#else
			sampler2D _Normal0;
		#endif

		half _Metallic0;
		half _Metallic1;
		half _Metallic2;
		half _Metallic3;
		
		half _Smoothness0;
		half _Smoothness1;
		half _Smoothness2;
		half _Smoothness3;
		
		float _TerrainWidth;
		
		half4 TriplanarTex2D (sampler2D texToSample, float3 wp, float3 wn, float4 texST)
		{
			// Find our UVs for each axis based on world position of the fragment.
			// half2 yUV = wp.xz / (half2(_TerrainWidth, _TerrainWidth) / texST.xy);
			// half2 xUV = wp.zy / (half2(_TerrainWidth, _TerrainWidth) / texST.xy);
			// half2 zUV = wp.xy / (half2(_TerrainWidth, _TerrainWidth) / texST.xy);
			half2 yUV = (wp.xz * texST.xy) / _TerrainWidth;
			half2 xUV = (wp.zy * texST.xy) / _TerrainWidth;
			half2 zUV = (wp.xy * texST.xy) / _TerrainWidth;
			// Now get texture samples from the texture with each of the 3 UV sets we've just made.
			half4 yProjection = tex2D (texToSample, yUV);
			half4 xProjection = tex2D (texToSample, xUV);
			half4 zProjection = tex2D (texToSample, zUV);
			// half4 yProjection = (tex2D (texToSample, yUV) + tex2D (texToSample, yUV * 4.0)) * 0.5f;
			// half4 xProjection = (tex2D (texToSample, xUV) + tex2D (texToSample, xUV * 4.0)) * 0.5f;
			// half4 zProjection = (tex2D (texToSample, zUV) + tex2D (texToSample, zUV * 4.0)) * 0.5f;
			// Get the absolute value of the world normal.
			half3 blendWeights = abs(wn);
			// Divide our blend mask by the sum of its components, this will make x+y+z=1
			blendWeights = blendWeights / (blendWeights.x + blendWeights.y + blendWeights.z);
			// Finally, blend together all three samples based on the blend mask.
			return (xProjection * blendWeights.x) + (yProjection * blendWeights.y) + (zProjection * blendWeights.z);
		}

		// Shade the mesh
		void surf (Input IN, inout SurfaceOutputStandard o) 
		{
			fixed4 splatControl = tex2D (_Control, IN.tc_Control);
			// Initialise the mixedDiffuse variable
			fixed4 mixedDiffuse = 0.0;

			half alphaWeight = dot(splatControl, half4(1, 1, 1, 1));

			// Normalize weights before lighting and restore weights in final modifier functions so that the overall
			// lighting result can be correctly weighted.
			splatControl /= (alphaWeight + 1e-3f);

			#ifdef TRIPLANAR_SHADING
				// Get the real world normal
				float3 realWorldNormal = WorldNormalVector(IN, float3(0, 0, 1));

				#ifdef TERRAIN_STANDARD_SHADER
					mixedDiffuse += splatControl.r * TriplanarTex2D(_Splat0, IN.worldPos, realWorldNormal, _Splat0_ST) * half4(1.0, 1.0, 1.0, _Smoothness0);
					mixedDiffuse += splatControl.g * TriplanarTex2D(_Splat1, IN.worldPos, realWorldNormal, _Splat1_ST) * half4(1.0, 1.0, 1.0, _Smoothness1);
					mixedDiffuse += splatControl.b * TriplanarTex2D(_Splat2, IN.worldPos, realWorldNormal, _Splat2_ST) * half4(1.0, 1.0, 1.0, _Smoothness2);
					mixedDiffuse += splatControl.a * TriplanarTex2D(_Splat3, IN.worldPos, realWorldNormal, _Splat3_ST) * half4(1.0, 1.0, 1.0, _Smoothness3);
				#else
					mixedDiffuse += splatControl.r * TriplanarTex2D(_Splat0, IN.worldPos, realWorldNormal, _Splat0_ST);
					mixedDiffuse += splatControl.g * TriplanarTex2D(_Splat1, IN.worldPos, realWorldNormal, _Splat1_ST);
					mixedDiffuse += splatControl.b * TriplanarTex2D(_Splat2, IN.worldPos, realWorldNormal, _Splat2_ST);
					mixedDiffuse += splatControl.a * TriplanarTex2D(_Splat3, IN.worldPos, realWorldNormal, _Splat3_ST);
				#endif
				#ifdef _TERRAIN_NORMAL_MAP
					fixed4 nrm = 0.0;
					// _SplatX_ST is used because _NormalX_ST is ALWAYS (1, 1, 0, 0) for some reason
					// This does give us the limitation though that the tiling of the normal maps can only ever
					// be the same as the tiling for the textures
					nrm += splatControl.r * TriplanarTex2D(_Normal0, IN.worldPos, realWorldNormal, _Splat0_ST);
					nrm += splatControl.g * TriplanarTex2D(_Normal1, IN.worldPos, realWorldNormal, _Splat1_ST);
					nrm += splatControl.b * TriplanarTex2D(_Normal2, IN.worldPos, realWorldNormal, _Splat2_ST);
					nrm += splatControl.a * TriplanarTex2D(_Normal3, IN.worldPos, realWorldNormal, _Splat3_ST);
					o.Normal = UnpackNormal(nrm);
				#else
					// Provide a null texture
					o.Normal = UnpackNormal(tex2D(_Normal0, IN.worldPos));
				#endif
					
			#else
				#ifdef TERRAIN_STANDARD_SHADER
					mixedDiffuse = splatControl.r * tex2D (_Splat0, IN.uv_Splat0) * half4(1.0, 1.0, 1.0, _Smoothness0);
					mixedDiffuse += splatControl.g * tex2D (_Splat1, IN.uv_Splat1) * half4(1.0, 1.0, 1.0, _Smoothness1);
					mixedDiffuse += splatControl.b * tex2D (_Splat2, IN.uv_Splat2) * half4(1.0, 1.0, 1.0, _Smoothness2);
					mixedDiffuse += splatControl.a * tex2D (_Splat3, IN.uv_Splat3) * half4(1.0, 1.0, 1.0, _Smoothness3);
				#else
					mixedDiffuse = splatControl.r * tex2D (_Splat0, IN.uv_Splat0);
					mixedDiffuse += splatControl.g * tex2D (_Splat1, IN.uv_Splat1);
					mixedDiffuse += splatControl.b * tex2D (_Splat2, IN.uv_Splat2);
					mixedDiffuse += splatControl.a * tex2D (_Splat3, IN.uv_Splat3);
				#endif
				#ifdef _TERRAIN_NORMAL_MAP
					fixed4 nrm = 0.0f;
					nrm += splatControl.r * tex2D(_Normal0, IN.uv_Splat0);
					nrm += splatControl.g * tex2D(_Normal1, IN.uv_Splat1);
					nrm += splatControl.b * tex2D(_Normal2, IN.uv_Splat2);
					nrm += splatControl.a * tex2D(_Normal3, IN.uv_Splat3);
					o.Normal = UnpackNormal(nrm);
				#else
					// Provide a null texture
					o.Normal = UnpackNormal(tex2D(_Normal0, IN.worldPos));
				#endif
			#endif
			// Set values
			o.Albedo = mixedDiffuse.rgb;
			// Set Alpha Weights so they can be restored in the final modifier functions
			o.Alpha = alphaWeight;
			o.Smoothness = mixedDiffuse.a;
			o.Metallic = dot(splatControl, half4(_Metallic0, _Metallic1, _Metallic2, _Metallic3));
		}
		
		// These 4 functions were originally from TerrainSplatmapCommon.cginc
		void SplatmapVert(inout appdata_full v, out Input data)
		{
			UNITY_INITIALIZE_OUTPUT(Input, data);
			data.tc_Control = TRANSFORM_TEX(v.texcoord, _Control);	// Need to manually transform uv here, as we choose not to use 'uv' prefix for this texcoord.
			
			//float4 pos = mul (UNITY_MATRIX_MVP, v.vertex);
			float4 pos = UnityObjectToClipPos(v.vertex.xyz); // sending float4, just calls function with float3
			UNITY_TRANSFER_FOG(data, pos);

			#ifdef _TERRAIN_NORMAL_MAP
				v.tangent.xyz = cross(v.normal, float3(0,0,1));
				v.tangent.w = -1;
			#endif
		}
		
		void SplatmapFinalColor(Input IN, TERRAIN_SURFACE_OUTPUT o, inout fixed4 color)
		{
			color *= o.Alpha;
			UNITY_APPLY_FOG(IN.fogCoord, color);
		}

		void SplatmapFinalPrepass(Input IN, TERRAIN_SURFACE_OUTPUT o, inout fixed4 normalSpec)
		{
			normalSpec *= o.Alpha;
		}

		void SplatmapFinalGBuffer(Input IN, TERRAIN_SURFACE_OUTPUT o, inout half4 diffuse, inout half4 specSmoothness, inout half4 normal, inout half4 emission)
		{
			diffuse.rgb *= o.Alpha;
			specSmoothness *= o.Alpha;
			normal.rgb *= o.Alpha;
			emission *= o.Alpha;
		}
		
		ENDCG
	}

	Dependency "AddPassShader" = "Hidden/TerrainEngine/Splatmap/LBTerrain-AddPass"
	Dependency "BaseMapShader" = "Hidden/TerrainEngine/Splatmap/LBTerrain-Base"

	Fallback "Nature/Terrain/Standard"
}
