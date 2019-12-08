Shader "Landscape Builder/Standard-LB Weather ER"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_MaxWaterLevel("Max Water Level", Range(0.25, 2)) = 1.0
		_Metallic("Metallic", Range(0,1)) = 0.0
		_Glossiness("Smoothness", Range(0,1)) = 0.0
		_Parallax("Height", Range(0.00, 0.08)) = 0.02
		_Normal("Normal", Range(0.0, 1.0)) = 1.0
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		[Toggle(ALPHA_CUTOUT)] _AlphaCutout ("Alpha Cutout Enabled", float) = 0
		_BumpMap("Normalmap", 2D) = "bump" {}
		[Toggle(NORMAL_MAPPING)] _NormalMapping ("Normal Mapping Enabled", float) = 0
		_ParallaxMap("Heightmap (R)", 2D) = "black" {}
		[HideInInspector] _DefaultBumpMap("Default Normalmap", 2D) = "bump" {}
	}
	
	SubShader
	{
		Tags{ "RenderType" = "Opaque" }
		LOD 200
		Offset -3,-3

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		#pragma shader_feature ALPHA_CUTOUT
		#pragma shader_feature NORMAL_MAPPING

		sampler2D _MainTex;
		sampler2D _BumpMap;
		sampler2D _ParallaxMap;
		sampler2D _DefaultBumpMap;
		float _Parallax;
		float _Normal;

		struct Input
		{
			float2 uv_MainTex;
			float2 uv_BumpMap;
			float2 uv_ParallaxMap;
			float3 viewDir;
			#if ALPHA_CUTOUT
				// Retrieve vertex colour data
				float4 color : COLOR;
			#endif
		};

		// LB global shader variable
		fixed _LBGlobalWetness;

		half _MaxWaterLevel;
		half _Glossiness;
		half _Metallic;
		fixed4 _Color;
		fixed _AlphaCutout;

		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			// Parallax mapping
			fixed height = tex2D(_ParallaxMap, IN.uv_ParallaxMap).r;
			float2 parallaxOffset = ParallaxOffset(height, _Parallax, IN.viewDir);
			IN.uv_MainTex += parallaxOffset;
			IN.uv_BumpMap += parallaxOffset;

			// Albedo comes from a texture tinted by color
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
			o.Alpha = c.a;
			#if ALPHA_CUTOUT
				// Discard pixels with an alpha of less than 0.5
				// Multiply texture alpha by vertex colour alpha
				clip(c.a * IN.color.a - 0.5);
			#endif

			// Calculate the water level based upon the maximum water level and the global wetness variable
			half waterLevel = _MaxWaterLevel * _LBGlobalWetness;
			// half waterLevel = _MaxWaterLevel * _WaterLevel;
			// From the water level and the parallax map height calculate the water depth
			half waterDepth = clamp(waterLevel - height, 0.0, 1.0);
			// Calculate the water amount (0 means dry, 1 means wet, in-between is edge blending)
			half waterAmount = 1.0 - ((height - (waterLevel - 0.05)) / 0.1);
			waterAmount = clamp(waterAmount, 0.0, 1.0);
			// Calculate water smoothness - increases with depth
			half waterSmoothness = lerp(0.5, 0.9, clamp(waterDepth * 5.0, 0.0, 1.0));
			// Metallic and smoothness come from slider variables and wetness
			o.Smoothness = lerp(_Glossiness, waterSmoothness, waterAmount);
			o.Metallic = lerp(_Metallic, 1.0, waterAmount);

			// Calculate colour - the deeper the water the more the original texture colour is faded out
			fixed4 actualColour = lerp(c, 0.75, waterDepth);
			o.Albedo = actualColour.rgb;

			#if NORMAL_MAPPING
				// Normal mapping
				fixed4 normalValue = tex2D(_BumpMap, IN.uv_BumpMap);
				fixed4 zeroNormalValue = tex2D(_DefaultBumpMap, IN.uv_BumpMap);
				// Wet areas should have unchanged normals as puddles are flat
				o.Normal = lerp(UnpackNormal(zeroNormalValue), UnpackNormal(normalValue), _Normal * (1.0 - waterAmount));
			#else
				o.Normal = UnpackNormal(tex2D(_DefaultBumpMap, IN.uv_BumpMap));
			#endif
		}
	ENDCG
	}
	FallBack "Diffuse"
}
