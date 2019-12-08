// Landscape Builder. Copyright (c) 2016-2018 SCSM Pty Ltd. All rights reserved.
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/LBImageFX"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}

		_FogParams ("Fog Parameters", Vector) = (0.0, 0.0, 0.0, 0.0)
		_FogParams2 ("Fog Parameters 2", Vector) = (0.0, 0.0, 0.0, 0.0)
		_FogParams3("Fog Parameters 3", Float) = 0.0
		_FogColour ("Fog Colour", Color) = (0.5, 0.5, 0.5, 1.0)
		_RainDropsTex ("Rain Drops Texture", 2D) = "white" {}
		_RainParams ("Rain Parameters", Vector) = (0.0, 0.0, 0.0, 0.0)
		_CloudsParams ("Clouds Parameters", Vector) = (0.0, 0.0, 0.0, 0.0)
		_CloudsParams2 ("Clouds Parameters 2", Vector) = (0.0, 0.0, 0.0, 0.0)
		_CloudsParams3 ("Clouds Parameters 3", Vector) = (0.0, 0.0, 0.0, 0.0)
		_CloudsUpperColour ("Clouds Upper Colour", Color) = (1.0, 1.0, 1.0, 1.0)
		_CloudsLowerColour ("Clouds Lower Colour", Color) = (0.5, 0.5, 0.5, 0.5)
		_PerlinBaseTex ("Perlin Base Texture", 2D) = "white" {}
		_PerlinDetailTex ("Perlin Detail Texture", 2D) = "white" {}
		_ReflectionParams ("Reflection Parameters", Vector) = (0, 0, 0, 0)
		_ReflectionParams2 ("Reflection Parameters 2", Vector) = (0, 0, 0, 0)
		_ReflectionParams3 ("Reflection Parameters 3", Vector) = (0, 0, 0, 0)
		_FilteringParams ("Filtering Parameters", Vector) = (0, 0, 0, 0)
	}

	CGINCLUDE

		#include "UnityCG.cginc"
		#include "UnityPBSLighting.cginc"

		// Shader booleans - Cloud rendering
		#pragma multi_compile CLOUD_QUALITY_LOW CLOUD_QUALITY_HIGH
		#pragma multi_compile __ USE_3D_NOISE

		// Shader booleans - SSRR	
		#pragma multi_compile __ REFLECT_NEAR_PIXELS
		#pragma multi_compile __ REFLECT_FAR_PIXELS
		#pragma multi_compile __ USING_DEFERRED_RENDERING_PATH
		#pragma multi_compile __ PHYSICALLY_BASED_REFLECTIONS
		
		uniform float4x4 _FrustumCornersWS;
		uniform float4 _CameraWS;
		
		uniform float4 _MainTex_TexelSize;
		
		// Transformation matrices
		float4x4 _ClipToWorld;
		float4x4 _WorldToView;

		// Tangent of half the field of view on each axis
		uniform float _TanHalfFOVX;
		uniform float _TanHalfFOVY;

		struct v2f
		{
			float4 pos : SV_POSITION;
			float2 uv : TEXCOORD0;
			float2 uv_depth : TEXCOORD1;
			// Used for WeatherFX
			float3 worldSpaceCameraToFarPlane : TEXCOORD2;
			// Used for SSRR
			float3 viewSpaceCameraToFarPlane : TEXCOORD3;
			// Used for handling vertically flipped depth textures
			float vflip : TEXCOORD4;
		};

		v2f vert (appdata_img v)
		{
			v2f o;

			// Handles vertically-flipped case.
			o.vflip = sign(_MainTex_TexelSize.y);

			v.vertex.z = 0.1;
			o.pos = UnityObjectToClipPos(v.vertex);
			o.uv = v.texcoord.xy;
			o.uv_depth = (v.texcoord.xy - 0.5) * float2(1.0, o.vflip) + 0.5;

			// Clip space X and Y coords
			float2 clipXY = o.pos.xy / o.pos.w;

			// Get view space camera ray
			float4 vsCameraRay = float4( o.uv * 2.0 - 1.0, 1.0, 1.0);

			// If we're rendering with a flipped projection camera, flip the clip space y coordinate
			if (_ProjectionParams.x < 0.0) { clipXY.y *= -1.0; }
					
			// Position of the far plane in clip space
			float4 farPlaneClip = float4(clipXY, 1, 1);
					
			// Homogeneous world position on the far plane
			float4 farPlaneWorld4 = mul(_ClipToWorld, farPlaneClip);
					
			// World position on the far plane
			float3 farPlaneWorld = farPlaneWorld4.xyz / farPlaneWorld4.w;
					
			// Vector from the camera to the far plane
			o.worldSpaceCameraToFarPlane = farPlaneWorld - _WorldSpaceCameraPos;

			// Calculate view space vector equivalent
			vsCameraRay = mul(unity_CameraInvProjection, vsCameraRay);
			o.viewSpaceCameraToFarPlane = vsCameraRay.xyz / vsCameraRay.w;
			
			return o;
		}

		// x is cloud rendering, y is cloud shadows, z is distance based fog, w is height based fog
		uniform int4 _FeaturesEnabled;	// 0 is OFF, 1 is ON
		
		// Screen And Depth Textures

		// Texture that contains original colour of each pixel
		uniform sampler2D _MainTex;
		#if USING_DEFERRED_RENDERING_PATH
			// Texture that contains depth (z dist to object) for each pixel
			uniform sampler2D_float _CameraDepthTexture;
			// The G Buffer Textures are only populated in the deferred rendering path
			#if REFLECT_NEAR_PIXELS || REFLECT_FAR_PIXELS
				// Texture that contains specular in the RGB channels and smoothness in the alpha channel
				uniform sampler2D _CameraGBufferTexture1;
				#if PHYSICALLY_BASED_REFLECTIONS
					// Texture that contains base colour in the RGB channels and occlusion in the alpha channel
					uniform sampler2D _CameraGBufferTexture0;
					// Texture that contains reflection data at each pixel in the RGB channels
					uniform sampler2D _CameraReflectionsTexture;
				#endif
			#endif
			// Texture that contains the world space normals at each pixel in the RGB channels
			uniform sampler2D _CameraGBufferTexture2;
		#else
			#if REFLECT_NEAR_PIXELS || REFLECT_FAR_PIXELS
				// Texture that contains depth and view space normals for each pixel
				uniform sampler2D_float _CameraDepthNormalsTexture;
			#else
				// Texture that contains depth (z dist to object) for each pixel
				uniform sampler2D_float _CameraDepthTexture;
			#endif
		#endif
		// Texture that contains SSRR texture results
		uniform sampler2D _SSRRTexture;
		
		// Weather FX Variables

		uniform float3 _WorldLightDir;	// The vector direction of the directional light in world space
										// The unity supplied variable doesn't seem to work...
		
		uniform half4 _FogColour;		// The provided fog colour
		uniform half4 _FogParams;		// x is distance fog density, y is height fog density, z is fog height
										// and w is fog sine amplitude
		uniform half4 _FogParams2;		// x is fog colour variance, y is max fog intensity, z is fog skybox
										// and w is use Unity fog colour
		uniform half _FogParams3;		// Fog water level
		uniform int _AnimateFog;		// Whether fog is animated (changes over time)
		uniform int _UseUnityFogColour;	// Whether the unity defined fog colour is used for fog rendering
		
		// CURRENTLY UNUSED
		uniform sampler2D _RainDropsTex;
		uniform half4 _RainParams; 		// x is rain drop distortion, y, z and w are unused
		
		uniform half4 _CloudsParams;	// x is start height, y is end height, z is raymarches (now unused) and w is tile size
		uniform half4 _CloudsParams2;	// x is cloud density, y is cloud coverage, z is cloud animation speed x
										// and w is cloud animation speed z
		uniform half4 _CloudsParams3;	// x is clouds morphing speed, y is cloud shadows ray marches
										// z is max cloud shadow strength and w is clouds detail amount
		uniform half4 _CloudsUpperColour;
		uniform half4 _CloudsLowerColour;
		
		uniform sampler2D _PerlinBaseTex;	// Tileable perlin noise base texture
		uniform sampler2D _PerlinDetailTex;	// Tileable perlin noise detail texture

		// SSRR Variables

		// x is pixel stride, y is max ray marches, z is max ray distance and w is screen fade distance
		uniform half4 _ReflectionParams;
		// x is fresnel fade, y is fresnel power, z is jitter and w is blur strength (currently unused)
		uniform half4 _ReflectionParams2;
		// x is downsampling, y is blur quality, z and w are unused
		uniform half4 _ReflectionParams3;
		
		// Filtering variables

		// x is filtering texel size, y is filtering samples, z and w are unused
		uniform half4 _FilteringParams;

		// ------------------------------------------------------------------------------
		// Helper functions
		// ------------------------------------------------------------------------------
		
		// 3D Noise Function - 3D noise from a 2D noise texture
		fixed Noise3D (fixed3 wsPos, sampler2D noiseTex)
		{
			#if USE_3D_NOISE
				// Transform y firstly by taking only the fractional part multiplied by 50
				wsPos.y = frac(wsPos.y) * 50.0;
				// Take the floor of this new value - segmenting it so there will be a period of 50
				fixed floorY = floor(wsPos.y);
				// Take the fractional part as well
				fixed fractY = wsPos.y - floorY;
				// Apply domain warping, based upon the floor value
				// The first domain warping value comes from floor of y pos
				// The fixed2 values are just some random primes - it really doesn't matter too much what these are...
				fixed2 a_offset = float2(23.0, 29.0) * (floorY) / 50.0;
				// The second value comes from floor of y pos plus one, so that it can blend smoothly into next value
				fixed2 b_offset = float2(23.0, 29.0) * (floorY+1.0) / 50.0;
				// Divide the x and z values by the tile size
				// Get two values of the noise based upon xz pos and some domain warping based upon y pos
				fixed noiseA = tex2Dlod(noiseTex, fixed4(wsPos.xz + a_offset, 0.0, 0.0)).r;
				fixed noiseB = tex2Dlod(noiseTex, fixed4(wsPos.xz + b_offset, 0.0, 0.0)).r;
				// Interpolate by the fractional part of the y value - so this will lerp between 0 and 1 with a period of 1/50?
				return lerp(noiseA, noiseB, fractY);
			#else
				// Super simple version of above - only takes into account 2D component, so it is much faster
				// but it can't do changing volumetric clouds
				return tex2Dlod(noiseTex, fixed4(wsPos.xz, 0.0, 0.0)).r;
			#endif
		}
		
		// ShaderLab doesn't have a builtin InverseLerp function
		float InverseLerp (float a, float b, float x)
		{
			float a2 = min (a, b);
			float b2 = max (a, b);
			float ans = (x - a2) / (a2 + b2);
			// Clamp return value between 0 and 1
			ans = min (1.0, ans);
			ans = max (0.0, ans);
			return ans;
		}
		
		float CalculateDistThroughHeightFog (float3 wsPos, float3 wsPosDelta, float wsSin, float fogDist, bool varyFogheight)
		{
			float fogHeight = _FogParams.z;
			float heightFogDist;
			
			// Only vary the fog height if it is not a far pixel - otherwise vertical artifacts will appear on the horizon
			if (varyFogheight) { fogHeight += wsSin * _FogParams.w; }
			
			float fogHeightDelta = fogHeight - _WorldSpaceCameraPos.y;
			float pixelHeightDelta = wsPosDelta.y;
			if (pixelHeightDelta > 0.0)
			{
				if (fogHeightDelta > 0.0)
				{
					// Some fog
					heightFogDist = fogDist * min(fogHeightDelta / pixelHeightDelta, 1.0);
				}
				else
				{
					// No fog
					heightFogDist = 0.0;
				}
			}
			else
			{
				if (fogHeightDelta < 0.0)
				{
					// Some fog
					heightFogDist = fogDist * min((pixelHeightDelta - fogHeightDelta) / pixelHeightDelta, 1.0);
				}
				else
				{
					// Full fog
					heightFogDist = fogDist;
				}
			}
			
			// float percentFog = InverseLerp(_WorldSpaceCameraPos.y, wsPos.y, fogHeight);
			// percentFog = min(percentFog, 1.0);
			// percentFog = max(percentFog, 0.0);
			// heightFogDist = fogDist * percentFog;
			
			return max(heightFogDist, 0.0);
		}

		// ShaderLab doesn't have a builtin angle function
		// This function returns the cosine of the angle between two vectors
		float CosAngleBetweenVectors (float3 v1, float3 v2)
		{
			return dot(v1, v2) / (length(v1) * length(v2));
		}
		
		// Function for quickly squaring numbers
		float FastSquare (float a)
		{
			return a * a;
		}

		// Function for quickly getting an estimate for the square root of a number
		// It uses the so-called Newton's Method
		float ApproxSqrt (float a)
		{
			// Get a first approximation of the square root
			float b = 1.0 + ((a - 1.0) * 0.5);
			// Refine the approximation iteratively
			b = (b + (a / b)) * 0.5;
			b = (b + (a / b)) * 0.5;
			b = (b + (a / b)) * 0.5;
			return b;
		}

		// Gaussian Probability Distribution Function
		float GaussianDistribution (float x, float sigma)
		{
			return 0.39894 * exp(-0.5 * x * x / (sigma * sigma)) / sigma;
		}
		
		// Returns average of RGB channels
		fixed Grayscale (fixed3 col)
		{
			return (col.r + col.g + col.b) * 0.333;
		}
		
		// Returns fractional part of a number
		float fract (float a)
		{
			return a - floor(a);
		}
		
		// Generates a random vector direction based on an input vector position
		float3 RandomVector (float3 inputV3)
		{
			return normalize(float3(sin(inputV3.x * 100), sin(inputV3.y * 100), sin(inputV3.z * 100)));
		}
		
		// Converts a view space position to a screen space position
		// X and Z are 0-1 screen coordinates, z is depth
		half3 ViewSpaceToScreenSpace (float3 viewSpacePos)
		{
			float3 screenSpacePos = viewSpacePos.xyz;
			// Scale x and y values based on distance from the camera
			screenSpacePos.x /= _TanHalfFOVX * -viewSpacePos.z;
			screenSpacePos.y /= _TanHalfFOVY * -viewSpacePos.z;
			// Values are now from -1 to 1, so convert them to be from 0 to 1
			screenSpacePos.xy = (screenSpacePos.xy + 1.0) / 2.0;
			// Take negative of depth as by default -z is forwards in camera view space
			screenSpacePos.z *= -1.0;
			return screenSpacePos;
		}
		
		float3 PixelLengthRayMarchVS (float3 vsRayStartPoint, float3 vsRayDir)
		{
			// Get x and y pixel sizes at current projection distance
			float xDist = (-vsRayStartPoint.z * _TanHalfFOVX * 2.0) / ((_ScreenParams.x / _ReflectionParams3.x) / _ReflectionParams.x);
			float yDist = (-vsRayStartPoint.z * _TanHalfFOVY * 2.0) / ((_ScreenParams.y / _ReflectionParams3.x) / _ReflectionParams.x);
			float x1 = xDist / abs(vsRayDir.x);
			float y1 = yDist / abs(vsRayDir.y);
			return vsRayDir * min(x1, y1);
		}
		
		bool OffScreen (half2 screenCoords)
		{
			return (screenCoords.x < 0.0 || screenCoords.x > 1.0 || screenCoords.y < 0.0 || screenCoords.y > 1.0);
		}
		
		fixed ScreenFadeMultiplier (half2 screenCoords)
		{
			fixed fadeMultiplier = 1.0;
			fixed fadeCalc;
			if (screenCoords.x > 0.5)
			{
				fadeCalc = 1.0 - InverseLerp(1.0 - _ReflectionParams.w, 1.0, screenCoords.x);
				fadeMultiplier = min(fadeCalc, fadeMultiplier);
			}
			else
			{
				fadeCalc = InverseLerp(0.0, _ReflectionParams.w, screenCoords.x);
				fadeMultiplier = min(fadeCalc, fadeMultiplier);
			}
			if (screenCoords.y > 0.5)
			{
				fadeCalc = 1.0 - InverseLerp(1.0 - _ReflectionParams.w, 1.0, screenCoords.y);
				fadeMultiplier = min(fadeCalc, fadeMultiplier);
			}
			else
			{
				fadeCalc = InverseLerp(0.0, _ReflectionParams.w, screenCoords.y);
				fadeMultiplier = min(fadeCalc, fadeMultiplier);
			}
			// if (screenCoords.x > 0.0)
			// {
			// 	fadeCalc = 1.0 - InverseLerp(1.0 - _ReflectionParams.w, 1.0, screenCoords.x);
			// 	fadeMultiplier = min(fadeCalc, fadeMultiplier);
			// }
			// else
			// {
			// 	fadeCalc = InverseLerp(-1.0, _ReflectionParams.w - 1.0, screenCoords.x);
			// 	fadeMultiplier = min(fadeCalc, fadeMultiplier);
			// }
			// if (screenCoords.y > 0.0)
			// {
			// 	fadeCalc = 1.0 - InverseLerp(1.0 - _ReflectionParams.w, 1.0, screenCoords.y);
			// 	fadeMultiplier = min(fadeCalc, fadeMultiplier);
			// }
			// else
			// {
			// 	fadeCalc = InverseLerp(-1.0, _ReflectionParams.w - 1.0, screenCoords.y);
			// 	fadeMultiplier = min(fadeCalc, fadeMultiplier);
			// }
			return fadeMultiplier;
		}

		fixed FarPixelScreenFadeMultiplier (half2 screenCoords)
		{
			fixed fadeMultiplier = 1.0;
			fixed fadeCalc;
			if (screenCoords.x > 0.5)
			{
				fadeCalc = 1.0 - InverseLerp(0.5, 1.0, screenCoords.x);
				fadeMultiplier = min(fadeCalc, fadeMultiplier);
			}
			else
			{
				fadeCalc = InverseLerp(0.0, 0.5, screenCoords.x);
				fadeMultiplier = min(fadeCalc, fadeMultiplier);
			}
			if (screenCoords.y > 0.5)
			{
				fadeCalc = 1.0 - InverseLerp(0.5, 1.0, screenCoords.y);
				fadeMultiplier = min(fadeCalc, fadeMultiplier);
			}
			else
			{
				fadeCalc = InverseLerp(0.0, 0.5, screenCoords.y);
				fadeMultiplier = min(fadeCalc, fadeMultiplier);
			}
			// if (screenCoords.x > 0.0)
			// {
			// 	fadeCalc = 1.0 - InverseLerp(0.5, 1.0, screenCoords.x);
			// 	fadeMultiplier = min(fadeCalc, fadeMultiplier);
			// }
			// else
			// {
			// 	fadeCalc = InverseLerp(-1.0, -0.5, screenCoords.x);
			// 	fadeMultiplier = min(fadeCalc, fadeMultiplier);
			// }
			// if (screenCoords.y > 0.0)
			// {
			// 	fadeCalc = 1.0 - InverseLerp(0.5, 1.0, screenCoords.y);
			// 	fadeMultiplier = min(fadeCalc, fadeMultiplier);
			// }
			// else
			// {
			// 	fadeCalc = InverseLerp(-1.0, -0.5, screenCoords.y);
			// 	fadeMultiplier = min(fadeCalc, fadeMultiplier);
			// }
			return fadeMultiplier;
		}
		
		// Current implementations of tex2Dlod from Unity seem to be broken on OpenGL...
		float SampleDepthTextureLOD (sampler2D depthTex, half2 depthUVs, float vflip)
		{
			depthUVs = (depthUVs - 0.5) * float2(1.0, vflip) + 0.5;
			return tex2Dlod(depthTex, half4(depthUVs, 1.0, 1.0)).r;
		}
		
		fixed4 SampleTex2DLOD (sampler2D tex, half2 texUVs)
		{
			return tex2Dlod(tex, half4(texUVs, 1.0, 1.0));
		}
		
		fixed4 GetFarPixelColourViewSpace (float3 vsRay, sampler2D screenTex)
		{
			half3 screenUVs = ViewSpaceToScreenSpace(vsRay);
			return SampleTex2DLOD(screenTex, screenUVs.xy);
		}

		fixed ModifyMinMax (fixed fixedIn, fixed minVal, fixed maxVal)
		{
			// Modify a value so that minVal and lower maps to 0 and maxVal and higher maps to 1
			// with linear mapping in between
			return max(min((fixedIn - minVal) * (1.0 / (maxVal - minVal)), 1.0), 0.0);
		}

		// Get depth, normals and world space position data
		void GetDepthAndNormalsData (inout float depth, inout float3 decodedNormal, inout float3 viewSpaceNormal, 
		inout float3 worldSpacePos, inout float3 worldSpacePosDelta, v2f IN)
		{
			#if USING_DEFERRED_RENDERING_PATH
				// Calculate view-space depth
				//float depth = Linear01Depth(SampleDepthTextureLOD(_DepthTexture, IN.uv_depth, 1.0));
				depth = Linear01Depth(SampleDepthTextureLOD(_CameraDepthTexture, IN.uv_depth, 1.0));
				// Decode world space normal of pixel
				decodedNormal = SampleTex2DLOD(_CameraGBufferTexture2, IN.uv).rgb * 2.0 - 1.0;
				// Calculate view space normal
				viewSpaceNormal = mul((float3x3)_WorldToView, decodedNormal);
			#else
				#if REFLECT_NEAR_PIXELS || REFLECT_FAR_PIXELS
					// Decode view-space depth and normals from same texture if needing reflections in forward
					DecodeDepthNormal(SampleTex2DLOD(_CameraDepthNormalsTexture, IN.uv_depth), depth, viewSpaceNormal);
				#else
					// Otherwise just use the single depth texture for improved precision
					depth = Linear01Depth(SampleDepthTextureLOD(_CameraDepthTexture, IN.uv_depth, 1.0));
				#endif
			#endif
			
			// Reconstruct the world position of the pixel
			worldSpacePosDelta = (IN.worldSpaceCameraToFarPlane * depth);
			worldSpacePos = worldSpacePosDelta + _WorldSpaceCameraPos;
		}

		// ------------------------------------------------------------------------------
		// Fragment functions
		// ------------------------------------------------------------------------------

		fixed4 CloudsFragment (v2f IN, fixed4 screenPixelColour, float depth)
		{
			fixed3 ncp = ((_CloudsParams.x - _WorldSpaceCameraPos.y) / IN.worldSpaceCameraToFarPlane.y);
			fixed3 fcp = ((_CloudsParams.y - _WorldSpaceCameraPos.y) / IN.worldSpaceCameraToFarPlane.y);
			bool cloudLayerVisible = (min(length(ncp), length(fcp)) < depth) || (depth > 0.999999);
			if (cloudLayerVisible)
			{
				fixed totalCloudDensity = 0.0;
				fixed detailCloudDensity = 0.0;
				fixed noiseVal = 0.0;
				fixed visCloudPos = 0.0;
				fixed3 visCloudWsPos;
				fixed visCloudDensity;
				fixed cdr;
				#if CLOUD_QUALITY_LOW
				fixed cloudsRaymarches = 16.0;
				#elif CLOUD_QUALITY_HIGH
				fixed cloudsRaymarches = 32.0;
				#endif
				// Only draw clouds if we are looking towards the cloud layer
				if (sign(IN.worldSpaceCameraToFarPlane.y) == sign(_CloudsParams.x - _WorldSpaceCameraPos.y))
				{
					// Calculate the world position of the start of the cloud layer for this pixel
					fixed3 nearCloudPos = ((_CloudsParams.x - _WorldSpaceCameraPos.y) / IN.worldSpaceCameraToFarPlane.y) * IN.worldSpaceCameraToFarPlane;
					// Calculate the world position of the end of the cloud layer for this pixel
					fixed3 farCloudPos = ((_CloudsParams.y - _WorldSpaceCameraPos.y) / IN.worldSpaceCameraToFarPlane.y) * IN.worldSpaceCameraToFarPlane;
					// We only want to add on the camera's xz position - we have already added the y position in the
					// near and far cloud position
					fixed3 worldSpaceCameraXZ = float3(_WorldSpaceCameraPos.x, 0.0, _WorldSpaceCameraPos.z);
					// Fade out the cloud density towards the horizon as an optimisation (will only help for some scenes though)
					fixed cloudDensityAmount = 1.0 - InverseLerp(_CloudsParams.y * 15.0, _CloudsParams.y * 25.0, distance(_WorldSpaceCameraPos, nearCloudPos + worldSpaceCameraXZ));
					// Only do clouds calculations if they aren't going to be completely faded out
					if (cloudDensityAmount > 0.01)
					{
						// Decrease the number of ray marches as the clouds fade into the distance, but not less than one
						// Currently this does nothing at all, as we have switched to using fixed loops for raymarching
						// cloudsRaymarches = floor(cloudsRaymarches * cloudDensityAmount);
						// cloudsRaymarches = max(cloudsRaymarches, 1.0);
						// Calculate a ray march distance from the delta between start and end positions divided by the number
						// of ray marches we want to do
						// Both this and the sample position will be divided by the tile size to avoid having to do it multiple times
						fixed3 rayMarch = ((farCloudPos - nearCloudPos) / cloudsRaymarches) / _CloudsParams.w;
						// Calculate the initial sample position (animation is calculated separately in the script,
						// thus no reference to time is necessary)
						fixed3 animVector = float3(_CloudsParams2.z, _CloudsParams3.x, _CloudsParams2.w);
						fixed3 samplePos = (nearCloudPos + worldSpaceCameraXZ + animVector) / _CloudsParams.w;
						// Calculate this once, so we don't have to do it multiple times in the loop
						// Is equal to cloud density / number of ray marches * cloud density amount
						fixed cloudDensityMultiplier = (_CloudsParams2.x / cloudsRaymarches) * cloudDensityAmount;
						// Calculate the min/max curve values for modification of the texture to change cloud coverage
						// Good values are: 0.01-0.10, 0.1-0.65, 0.16-1.0
						fixed noiseMinVal = max(0.16 - (_CloudsParams2.y * 0.15), 0.0);
						fixed noiseMaxVal = min(1.00 - (_CloudsParams2.y * 0.90), 1.0);
						// fixed noiseMinVal = max(0.32 - (_CloudsParams2.y * 0.30), 0.0);
						// fixed noiseMaxVal = min(2.00 - (_CloudsParams2.y * 1.80), 1.0);
						// fixed noiseMinVal = max(0.24 - (_CloudsParams2.y * 0.225), 0.0);
						// fixed noiseMaxVal = min(1.50 - (_CloudsParams2.y * 1.350), 1.0);

						// fixed noiseMinVal = max(0.31 - (_CloudsParams2.y * 0.30), 0.0);
						// fixed noiseMaxVal = min(1.90 - (_CloudsParams2.y * 1.80), 1.0);

						// fixed noiseMinVal = 0.60;
						// fixed noiseMaxVal = 1.00;

						// Initialise some matrices for efficient storage
						fixed4x4 CDRMatrix1 = {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
						#if CLOUD_QUALITY_HIGH
						fixed4x4 CDRMatrix2 = {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
						#endif
						// Raymarch from the start pos to the end pos
						#if CLOUD_QUALITY_LOW
						for (uint r = 0; r < 16; r++)
						#elif CLOUD_QUALITY_HIGH
						for (uint r = 0; r < 32; r++)
						#endif
						{
							// Progress the sample position using the calculated ray march distance
							samplePos += rayMarch;
							// Calculate the cloud density of the new sample pos and add it to the total amount
							noiseVal = Noise3D(samplePos, _PerlinBaseTex);
							cdr = ModifyMinMax(noiseVal, noiseMinVal, noiseMaxVal) * cloudDensityMultiplier * 0.9;
							totalCloudDensity += cdr;
							// Store the results in matrices (instead of arrays, which are slow)
							#if CLOUD_QUALITY_LOW
							CDRMatrix1[floor(r/4)][r%4] = cdr;
							#elif CLOUD_QUALITY_HIGH
							if (r < 16) { CDRMatrix1[floor(r/4)][r%4] = cdr; }
							else { CDRMatrix2[floor((r-16)/4)][(r-16)%4] = cdr; }
							#endif
							if (totalCloudDensity < 0.5) { visCloudPos = r; visCloudWsPos = samplePos; visCloudDensity = totalCloudDensity; }
							// Stop looping through if we reach the maximum value
							else if (totalCloudDensity > 1.0) { break; }
						}

						// Fade out detail intensity over distance as an optimisation
						fixed detailAmount = 1.0 - InverseLerp(_CloudsParams.y, _CloudsParams.y * 5.0, distance(_WorldSpaceCameraPos, nearCloudPos + worldSpaceCameraXZ));
						// Modify detail amount so that it only adds on to the initial cloud amount by a spcified amount
						// Also modify it so that it is only drawn on top of the cloud bodies
						detailAmount *= _CloudsParams3.w * totalCloudDensity;
						// Don't draw the detail if the detail amount is very small
						bool drawDetail = detailAmount > 0.01;
						
						if (drawDetail)
						{
							// Reset density and sample position
							totalCloudDensity = 0.0;
							samplePos = (nearCloudPos + worldSpaceCameraXZ + animVector) / _CloudsParams.w;
							// Loop through again
							#if CLOUD_QUALITY_LOW
							for (uint r = 0; r < 16; r++)
							#elif CLOUD_QUALITY_HIGH
							for (uint r = 0; r < 32; r++)
							#endif
							{
								// Progress the sample position using the calculated ray march distance
								samplePos += rayMarch;
								// Add the originally calculated value to the total density
								#if CLOUD_QUALITY_LOW
								totalCloudDensity += CDRMatrix1[floor(r/4)][r%4];
								#elif CLOUD_QUALITY_HIGH
								if (r < 16) { totalCloudDensity += CDRMatrix1[floor(r/4)][r%4]; }
								else { totalCloudDensity += CDRMatrix2[floor((r-16)/4)][(r-16)%4]; }
								#endif
								// Calculate the cloud density of the new sample pos and add it to the total amount
								totalCloudDensity += Noise3D(samplePos * 10.0, _PerlinDetailTex) * cloudDensityMultiplier * detailAmount;
								if (totalCloudDensity < 0.5) { visCloudPos = r; visCloudWsPos = samplePos; visCloudDensity = totalCloudDensity; }
								// Stop looping through if we reach the maximum value
								else if (totalCloudDensity > 1.0) { break; }
							}
						}

						// Modify the cloud density to fit within certain paramaters
						totalCloudDensity = clamp(totalCloudDensity, 0.0, 1.0);
						totalCloudDensity = (totalCloudDensity - 0.65) / 0.35;
						totalCloudDensity = clamp(totalCloudDensity, 0.0, 1.0);
						
						// Refinement ray marching - ray march between the two positions we know the cloud vis position to be,
						// so that we can refine the position with a minimal performance impact
						samplePos = visCloudWsPos;
						fixed totalCloudDensityTest = visCloudDensity;
						// Divide the ray march distance and the cloud density multiplier by the number of
						// refinement ray marches
						// 0.167 = 1 / 6
						rayMarch *= 0.167;
						cloudDensityMultiplier *= 0.167;
						for (uint r2 = 1; r2 < 6; r2++)
						{
							// Progress the sample position using the calculated ray march distance
							samplePos += rayMarch;
							// Calculate the cloud density of the new sample pos and add it to the total amount
							noiseVal = Noise3D(samplePos, _PerlinBaseTex);
							totalCloudDensityTest += ModifyMinMax(noiseVal, noiseMinVal, noiseMaxVal) * cloudDensityMultiplier * 0.9;
							if (drawDetail) { totalCloudDensityTest += Noise3D(samplePos * 10.0, _PerlinDetailTex) * cloudDensityMultiplier * detailAmount; }
							if (totalCloudDensityTest < 0.5) { visCloudPos += 0.167; }
							// Stop looping if the optimal point is found
							else { break; }
						}
					}
				}

				// Calculate cloud colour
				float vcp = (visCloudPos / cloudsRaymarches * -2.0) + 1.0;
				float colourParamA = 1.0 - min(abs(vcp - _WorldLightDir.y), 1.0);
				float colourParamB = (dot(IN.worldSpaceCameraToFarPlane, _WorldLightDir) / length(IN.worldSpaceCameraToFarPlane));
				colourParamB = max(colourParamB, 0.0);
				half4 cloudsColour = lerp(_CloudsLowerColour, _CloudsUpperColour, (colourParamA + colourParamB) * 0.5);

				// float vcp = (visCloudPos / _CloudsParams.z * 2.0) - 1.0;
				// float3 camDir = normalize(IN.worldSpaceCameraToFarPlane);
				// float3 approxCloudNormal = float3(-camDir.x, vcp, -camDir.z);
				// float colourParam = (dot(-normalize(approxCloudNormal), _WorldLightDir) + 1.0) * 0.5;
				// // float colourParam = max(dot(-normalize(approxCloudNormal), _WorldLightDir), 0.0);
				// half4 cloudsColour = lerp(_CloudsLowerColour, _CloudsUpperColour, colourParam);

				// Test code
				//half4 _CloudsLowerColourLin = half4(LinearToGammaSpace(_CloudsLowerColour.rgb), _CloudsLowerColour.a);
				//half4 _CloudsUpperColourLin = half4(LinearToGammaSpace(_CloudsUpperColour.rgb), _CloudsUpperColour.a);
				//cloudsColour = lerp(_CloudsLowerColourLin, _CloudsUpperColourLin, visCloudPos / _CloudsParams.z);

				//cloudsColour = half4(LinearToGammaSpace(cloudsColour.rgb), 0); // cloudsColour.a / 2.2);

				//totalCloudDensity = totalCloudDensity / 2.2;

				//return totalCloudDensity;

				// screenPixelColour = lerp(sceneColour, cloudsColour, totalCloudDensity);
				screenPixelColour = lerp(screenPixelColour, cloudsColour, totalCloudDensity);
				screenPixelColour.a = 1.0;
			}
			return screenPixelColour;
		}

		fixed4 CloudShadowsFragment (v2f IN, fixed4 screenPixelColour, float depth, float3 worldSpacePos)
		{
			// Cloud shadows - drawn AFTER fog
			fixed totalCloudDensity = 0.0;
			fixed noiseVal = 0.0;
			// Only draw cloud shadows if the light direction is downwards and not at a low angle
			if (_WorldLightDir.y < 0.0)
			{
				// Calculate the world position of the start of the cloud layer for this pixel's light direction
				fixed3 nearCloudPos = ((_CloudsParams.x - worldSpacePos.y) / -_WorldLightDir.y) * -_WorldLightDir;
				// Calculate the world position of the end of the cloud layer for this pixel's light direction
				fixed3 farCloudPos = ((_CloudsParams.y - worldSpacePos.y) / -_WorldLightDir.y) * -_WorldLightDir;
				// We only want to add on the pixel's xz position - we have already added the y position in the
				// near and far cloud position
				fixed3 worldSpacePosXZ = float3(worldSpacePos.x, 0.0, worldSpacePos.z);
				// Fade out the cloud density towards the horizon as an optimisation (will only help for some scenes though)
				fixed cloudDensityAmount = 1.0 - InverseLerp(_CloudsParams.y * 15.0, _CloudsParams.y * 25.0, distance(worldSpacePos, nearCloudPos + worldSpacePosXZ));
				// Only do clouds calculations if they aren't going to be completely faded out
				if (cloudDensityAmount > 0.0001)
				{
					// Decrease the number of ray marches as the clouds fade into the distance, but not less than one
					_CloudsParams3.y = floor(_CloudsParams3.y * cloudDensityAmount);
					_CloudsParams3.y = max(_CloudsParams3.y, 1.0);
					// Calculate a ray march distance from the delta between start and end positions divided by the number
					// of ray marches we want to do
					// Both this and the sample position will be divided by the tile size to avoid having to do it multiple times
					fixed3 rayMarch = ((farCloudPos - nearCloudPos) / _CloudsParams3.y) / _CloudsParams.w;
					// Calculate the initial sample position
					fixed3 animVector = float3(_CloudsParams2.z, _CloudsParams3.x, _CloudsParams2.w);
					fixed3 samplePos = (nearCloudPos + worldSpacePosXZ + animVector) / _CloudsParams.w;
					// Calculate this once, so we don't have to do it multiple times in the loop
					// Is equal to cloud density / number of ray marches * cloud density amount
					fixed cloudDensityMultiplier = (_CloudsParams2.x / _CloudsParams3.y) * cloudDensityAmount;
					// Calculate the min/max curve values for modification of the texture to change cloud coverage
					fixed noiseMinVal = 0.16 - (_CloudsParams2.y * 0.15);
					fixed noiseMaxVal = 1.00 - (_CloudsParams2.y * 0.90);
					// fixed noiseMinVal = max(0.32 - (_CloudsParams2.y * 0.30), 0.0);
					// fixed noiseMaxVal = min(2.00 - (_CloudsParams2.y * 1.80), 1.0);
					// fixed noiseMinVal = max(0.24 - (_CloudsParams2.y * 0.225), 0.0);
					// fixed noiseMaxVal = min(1.50 - (_CloudsParams2.y * 1.350), 1.0);
					// Calculate the cloud density of the initial sample pos
					noiseVal = Noise3D(samplePos, _PerlinBaseTex);
					totalCloudDensity = ModifyMinMax(noiseVal, noiseMinVal, noiseMaxVal) * cloudDensityMultiplier;
					// The unroll keyword tells the loop that the maximum number of iterations it can
					// go through is 15 - this allows it to compile
					//for (int r = 0; r < _CloudsParams3.y; r++)
					for (int r = 0; r < (int)_CloudsParams3.y; r++)
					{
						// Progress the sample position using the calculated ray march distance
						samplePos += rayMarch;
						// Calculate the cloud density of the new sample pos and add it to the total amount
						noiseVal = Noise3D(samplePos, _PerlinBaseTex);
						totalCloudDensity += ModifyMinMax(noiseVal, noiseMinVal, noiseMaxVal) * cloudDensityMultiplier;
						// Stop looping through if we reach the maximum value
						if (totalCloudDensity > 1.0) { break; }
					}
					totalCloudDensity = clamp(totalCloudDensity, 0.0, 1.0);
					totalCloudDensity = (totalCloudDensity - 0.65) / 0.35;
					// Clamp the shadow strength
					totalCloudDensity = clamp(totalCloudDensity, 0.0, _CloudsParams3.z);
					// Fade out shadows as the light goes to a low angle
					totalCloudDensity *= InverseLerp(-0.1, 0.0, _WorldLightDir.y);
					
					// No refinement ray marching - all we want to calculate is the density value for shadows!

					// Blend between original pixel colour and black (shadow colour)
					// based upon the cloud density - which in this case can be interpreted as shadow strength
					screenPixelColour = lerp(screenPixelColour, half4(0.0, 0.0, 0.0, screenPixelColour.a), totalCloudDensity);
				}
			}
			return screenPixelColour;
		}

		fixed4 FirstPassFragment (v2f IN) : SV_Target
		{
			//half2 screenUV = IN.uv;
			//screenUV += half2(sin(screenUV.x + _Time.w), sin(screenUV.y + _Time.w)) * _RainParams.x;
			// half2 rainDropPos = half2(0.25, 0.25);
			// half2 distortion = screenUV - rainDropPos;
			// if (length(distortion) > 0.05) { distortion = 0.0; }
			// half2 rainUV = screenUV;
			// rainUV.y += _Time.y;
			// half2 distortion = UnpackNormal(tex2D(_RainDropsTex, rainUV)).rg;
			// screenUV += distortion * _RainParams.x;
			
			// Get the original colour of the pixel
			fixed4 screenPixelColour = tex2D(_MainTex, IN.uv);

			// Get depth and normals data
			float depth = 1.0;
			float3 viewSpaceNormal = 1.0;
			float3 decodedNormal = 1.0;
			float3 worldSpacePos = 0.0;
			float3 worldSpacePosDelta = 0.0;
			GetDepthAndNormalsData(depth, decodedNormal, viewSpaceNormal, worldSpacePos, worldSpacePosDelta, IN);

			bool renderFog = false;
			if (_FeaturesEnabled.z == 1 || _FeaturesEnabled.w == 1)
			{
				// If we are told to fog skybox, always render fog
				renderFog = true;
				if (_FogParams2.z == 0)
				{
					// Else, don't fog far pixels (which will be the skybox)
					renderFog = depth < 0.999999;
				}
			}
			
			// Declare variables
			half4 fogColour = half4(1.0, 1.0, 1.0, 1.0);
			float fogFactor = 1.0;
			
			// Only calculate fog values if we need to...
			if (renderFog)
			{	
				// Calculate the sine of the world position
				float sinWorldPos = 0.0;
				if (_FeaturesEnabled.w == 1)
				{
					// This is only used for height-based fog
					float sinWorldPos = sin((worldSpacePos.x * 0.1) + _Time.y) + sin((worldSpacePos.z * 0.1) + _Time.y);
					sinWorldPos += (sin((worldSpacePos.x * 0.5) + _Time.x) + sin((worldSpacePos.z * 0.5) + _Time.x)) * 0.25;
				}
				float sinWorldPos2 = sin((worldSpacePos.x * 0.01) + _Time.y) + sin((worldSpacePos.z * 0.01) + _Time.y);
				sinWorldPos2 += (sin((worldSpacePos.x * 0.05) + _Time.x) + sin((worldSpacePos.z * 0.05) + _Time.x)) * 0.5;

				// Calculate the distance light travels through fog
				float distThroughFog = (depth * _ProjectionParams.z) - _ProjectionParams.y;
				// Adjust for sea level - FUTURE when the camera is below water don't modify the fog.
				if (_FogParams3 >= _WorldSpaceCameraPos.y && _FogParams3 >= worldSpacePos.y) { distThroughFog = 0.0; }
				else if (_FogParams3 >= _WorldSpaceCameraPos.y) { distThroughFog *= (worldSpacePos.y - _FogParams3) / (worldSpacePos.y - _WorldSpaceCameraPos.y); }
				else if (_FogParams3 >= worldSpacePos.y) { distThroughFog *= (_WorldSpaceCameraPos.y - _FogParams3) / (_WorldSpaceCameraPos.y - worldSpacePos.y); }

				if (_FeaturesEnabled.z == 1 && _FeaturesEnabled.w == 1)
				{
					float distThroughHeightFog = CalculateDistThroughHeightFog(worldSpacePos, worldSpacePosDelta, sinWorldPos, distThroughFog, depth != 1);
					float distThroughDistFog = distThroughFog - distThroughHeightFog;
					// Calculate fog factor
					fogFactor = (_FogParams.x * max(distThroughDistFog, 0.0)) + (_FogParams.y * max(distThroughHeightFog, 0.0));
					fogFactor = saturate(exp2(-fogFactor * fogFactor));
				}
				else if (_FeaturesEnabled.z == 1)
				{
					float distThroughDistFog = distThroughFog;
					// Calculate fog factor
					fogFactor = _FogParams.x * max(distThroughDistFog, 0.0);
					fogFactor = saturate(exp2(-fogFactor * fogFactor));
				}
				else if (_FeaturesEnabled.w == 1)
				{
					float distThroughHeightFog = CalculateDistThroughHeightFog(worldSpacePos, worldSpacePosDelta, sinWorldPos, distThroughFog, depth != 1);
					// Calculate fog factor
					fogFactor = _FogParams.y * max(distThroughHeightFog, 0.0);
					fogFactor = saturate(exp2(-fogFactor * fogFactor));
				}
				
				// _FogParams2.w is the use Unity fog colour bool
				if (_FogParams2.w == 1)
				{
					// Use Unity fog colour
					fogColour = unity_FogColor;
				}
				else
				{
					// Use given fog colour
					fogColour = _FogColour;
				}
				
				// Vary fog colour, but not the fog occluding the skybox
				if (depth < 1) { fogColour += half4(1.0, 1.0, 1.0, 0) * sinWorldPos2 * _FogParams2.x; }
				
				// Clamp fog intensity based on user setting
				if (fogFactor < 1 - _FogParams2.y) { fogFactor = 1 - _FogParams2.y; }
			}

			// Cloud Rendering

			if (_FeaturesEnabled.x == 1)
			{
				screenPixelColour = CloudsFragment(IN, screenPixelColour, depth);
			}

			// TODO: Should this be drawn after SSRR?

			if (renderFog)
			{
				// Allow fog to be drawn over clouds if "fog skybox" is enabled
				screenPixelColour = lerp(fogColour, screenPixelColour, fogFactor);
			}
			
			return screenPixelColour;
		}

		fixed4 CalculateReflectionStrength (fixed4 gBuffer1Col, v2f IN, float depth, float3 viewSpaceNormal)
		{
			// Reflection strength based mostly on smoothness
			// Unrealistic but works for now
			fixed reflectionMultiplier = gBuffer1Col.a;
			// Calculate view space position of pixel
			float3 pixelViewSpacePos = IN.viewSpaceCameraToFarPlane * depth;
			// Calculate reflection ray
			float3 reflectionRayViewSpace = reflect(IN.viewSpaceCameraToFarPlane, viewSpaceNormal);
			// Calculate reflection ray march origin and direction (magnitude is calculated dynamically on the fly)
			float3 samplePosVS = pixelViewSpacePos;
			float3 rayMarchVS = normalize(reflectionRayViewSpace);
			// Calculate fresnel strength
			fixed cosReflAngle = abs((CosAngleBetweenVectors(-IN.viewSpaceCameraToFarPlane, rayMarchVS)));
			fixed fresnelStrength = lerp(pow(cosReflAngle, _ReflectionParams2.y), 1.0, 1.0 - _ReflectionParams2.x);
			// Multiply the reflection strength by the fresnel strength
			reflectionMultiplier *= max(fresnelStrength, 0.0);
			// Fade out rays moving towards the camera
			reflectionMultiplier *= 1.0 - InverseLerp(0.0, 0.5, rayMarchVS.z);
			// Clamp reflection multiplier to realistic values
			reflectionMultiplier = clamp(reflectionMultiplier, 0.0, 1.0);
			// Return calculated reflection strength
			return reflectionMultiplier;
		}

		fixed4 SSRRPassFragment (v2f IN) : SV_Target
		{
			// Get the current colour of the pixel - modified by the first pass
			fixed4 screenPixelColour = SampleTex2DLOD(_MainTex, IN.uv);

			// Get depth and normals data
			float depth = 1.0;
			float3 viewSpaceNormal = 1.0;
			float3 decodedNormal = 1.0;
			float3 worldSpacePos = 0.0;
			float3 worldSpacePosDelta = 0.0;
			GetDepthAndNormalsData(depth, decodedNormal, viewSpaceNormal, worldSpacePos, worldSpacePosDelta, IN);

			#if REFLECT_NEAR_PIXELS || REFLECT_FAR_PIXELS
				if (depth < 1.0)
				{
					//fixed reflectionMultiplier = 1.0;
					#if USING_DEFERRED_RENDERING_PATH
						// Retrieve specular and smoothness data
						fixed4 gBuffer1Col = SampleTex2DLOD(_CameraGBufferTexture1, IN.uv);
						fixed3 specular = gBuffer1Col.rgb;
						fixed smoothness = gBuffer1Col.a;
						// // Make reflections on smoother surfaces stronger
						// // Not physically correct - this will hopefully be implemented later
						// // using the blur pass instead
						// reflectionMultiplier *= smoothness;
						#if PHYSICALLY_BASED_REFLECTIONS
							// When using the deferred rendering path we have access to a large array of rendering data
							// Retrieve base colour and occlusion data
							fixed4 gBuffer0Col = SampleTex2DLOD(_CameraGBufferTexture0, IN.uv);
							fixed3 baseColour = gBuffer0Col.rgb;
							fixed occlusion = gBuffer0Col.a;
							// Get specular emission data from camera reflections texture
							fixed4 camReflectionsCol = SampleTex2DLOD(_CameraReflectionsTexture, IN.uv);
							fixed4 specularEmission = fixed4(camReflectionsCol.rgb, 1.0);
							//fixed reflectionValidity = camReflectionsCol.a;
							// Populate oneMinusReflectivity variable
							fixed oneMinusReflectivity;
							baseColour = EnergyConservationBetweenDiffuseAndSpecular(baseColour, specular, oneMinusReflectivity);
							// Below is technically incorrect as without command buffers no way to actually get reflection data
							// However the data from _CameraReflectionsTexture works well as an approximation
							UnityLight simulatedLight = (UnityLight)0.0;
							simulatedLight.color = 0.0;
							simulatedLight.dir = 0.0;
							UnityIndirect simulatedIndirect = (UnityIndirect)0.0;
							simulatedIndirect.diffuse = 0.0;
							simulatedIndirect.specular = specularEmission.rgb;
							float3 eyeVec = mul(unity_CameraToWorld, float4(normalize(IN.viewSpaceCameraToFarPlane), 0.0)).xyz;
							// Calculate a reflection tinting colour
							float4 reflectionTint = float4(UNITY_BRDF_PBS (0.0, specular, oneMinusReflectivity, smoothness,
								decodedNormal, -eyeVec, simulatedLight, simulatedIndirect).rgb, 1.0);
							reflectionTint *= occlusion;
						#endif
					#else
						// Approximate the specular colour as the actual colour of the pixel onscreen
						// This is nowhere near physically accurate (and in some cases is completely wrong) but in
						// a number of cases will work fine, allowing SSR to work in the forward rendering path
						fixed3 specular = screenPixelColour;
						fixed smoothness = 1.0;
						fixed4 gBuffer1Col = fixed4(screenPixelColour.r, screenPixelColour.g, screenPixelColour.b, smoothness);
					#endif

					// Calculate view space position of pixel
					float3 pixelViewSpacePos = IN.viewSpaceCameraToFarPlane * depth;
					
					// Calculate reflection ray
					float3 reflectionRayViewSpace = reflect(IN.viewSpaceCameraToFarPlane, viewSpaceNormal);
					
					// Calculate reflection ray march origin and direction (magnitude is calculated dynamically on the fly)
					float3 samplePosVS = pixelViewSpacePos;
					float3 rayMarchVS = normalize(reflectionRayViewSpace);
					
					// // Calculate fresnel strength
					// fixed cosReflAngle = abs((CosAngleBetweenVectors(-IN.viewSpaceCameraToFarPlane, rayMarchVS)));
					// fixed fresnelStrength = lerp(pow(cosReflAngle, _ReflectionParams2.y), 1.0, 1.0 - _ReflectionParams2.x);
					// // Multiply the reflection strength by the fresnel strength
					// reflectionMultiplier *= max(fresnelStrength, 0.0);
					
					// // Fade out rays moving towards the camera
					// reflectionMultiplier *= 1.0 - InverseLerp(0.0, 0.5, rayMarchVS.z);
					
					// Declare variables outside of loop
					half3 uvCoord = 0.0;
					half3 flippedUvCoord = 0.0;
					float3 newRayMarchVS;
					bool foundReflectedPixel = false;
					float rayMarchDepth = 1.0;
					float testDepth = 0.0;
					float backFaceDepth = 0.0;
					
					// // Clamp reflection multiplier to realistic values
					// reflectionMultiplier = clamp(reflectionMultiplier, 0.0, 1.0);

					float3 initialSamplePosVS = samplePosVS;

					// Jitter - add a random amount (0-1) of the ray march vector onto the initial position
					// This helps to fix jagged artefacts (it blurs them a fair bit)
					float randomJValue = ((sin(samplePosVS.x * 100) + sin(samplePosVS.y * 100) + sin(samplePosVS.z * 100)) + 3.0) * 0.167;
					samplePosVS += randomJValue * rayMarchVS * _ReflectionParams2.z;
					// newRayMarchVS = PixelLengthRayMarchVS(samplePosVS, rayMarchVS);
					// samplePosVS += randomJValue * newRayMarchVS * _ReflectionParams2.z;

					float initialVSZ = samplePosVS.z;

					fixed reflectionMultiplier = CalculateReflectionStrength(gBuffer1Col, IN, depth, viewSpaceNormal);
					
					// Assign a cutoff value of 0.01 to avoid spending lots of time calculating hard-to-see reflections
					if (reflectionMultiplier > 0.01)
					{
						#if REFLECT_NEAR_PIXELS
							for (int r = 0; r < (int)_ReflectionParams.y; r++)
							{
								// Ray march from the starting vector
								newRayMarchVS = PixelLengthRayMarchVS(samplePosVS, rayMarchVS);
								samplePosVS += newRayMarchVS;
								// Stop looping through if the maximum ray distance is reached
								// if (initialVSZ - samplePosVS.z > _ReflectionParams.z) { break; }
								if (length(initialSamplePosVS - samplePosVS) > _ReflectionParams.z) { break; }
								// Transform the view space position into screen pixel coordinates
								uvCoord = ViewSpaceToScreenSpace(samplePosVS);
								// Stop looping through if the ray goes offscreen
								if (OffScreen(uvCoord)) { break; }
								rayMarchDepth = uvCoord.z / _ProjectionParams.z;
								// Break out of the loop if we go past the far plane
								if (rayMarchDepth > 1.0) { break; }
								// Sample the depth buffer at the given coordinates
								#if USING_DEFERRED_RENDERING_PATH
									testDepth = Linear01Depth(SampleDepthTextureLOD(_CameraDepthTexture, uvCoord.xy, IN.vflip));
								#else
									flippedUvCoord.xy = (uvCoord.xy - 0.5) * float2(1.0, IN.vflip) + 0.5;
									DecodeDepthNormal(SampleTex2DLOD(_CameraDepthNormalsTexture, flippedUvCoord.xy), testDepth, viewSpaceNormal);
								#endif
								// If the pixel is in front of our ray, we have intersected scene geometry
								// Therefore this is the pixel that should be reflected
								// NEED TO DO SOMETHING FOR GEOMETRY THICKNESS - MAYBE BACKFACES?
								if (testDepth < rayMarchDepth)// && testDepth + 0.005 > rayMarchDepth)
								{
									// Refine using binary search
									fixed stepDir = -1.0;
									for (int b = 0; b < 4; b++)
									{
										// Reduce step size by factor of a half
										newRayMarchVS *= 0.5;
										samplePosVS += newRayMarchVS * stepDir;
										// Transform the view space position into screen pixel coordinates
										uvCoord = ViewSpaceToScreenSpace(samplePosVS);
										// Sample the depth buffer at the given coordinates
										#if USING_DEFERRED_RENDERING_PATH
											testDepth = Linear01Depth(SampleDepthTextureLOD(_CameraDepthTexture, uvCoord.xy, IN.vflip));
										#else
											flippedUvCoord.xy = (uvCoord.xy - 0.5) * float2(1.0, IN.vflip) + 0.5;
											DecodeDepthNormal(SampleTex2DLOD(_CameraDepthNormalsTexture, flippedUvCoord.xy), testDepth, viewSpaceNormal);
										#endif
										// Change the direction of the ray march depending on which direction we 
										// will need to step in next
										stepDir = sign(testDepth - (uvCoord.z / _ProjectionParams.z));
									}
									// Sample the original screen pixel colour
									fixed4 reflectedPixelColour = SampleTex2DLOD(_MainTex, uvCoord.xy);
									#if PHYSICALLY_BASED_REFLECTIONS && USING_DEFERRED_RENDERING_PATH
										// Multiply it by the calculated reflection tint colour colour
										// to get more physically accurate reflections
										reflectedPixelColour *= reflectionTint;
										//reflectedPixelColour *= lerp(fixed4(specular, 1.0), reflectionTint, reflectionValidity);
									#else
										#if USING_DEFERRED_RENDERING_PATH
											// Multiply it by the specular colour of the pixel it is reflected
											// from - not physically correct but may look more visually appealing
											reflectedPixelColour *= fixed4(specular, 1.0);
										#else
											reflectedPixelColour *= screenPixelColour;
										#endif
									#endif
									// Fade out reflections of pixels near the edges of the screen
									reflectionMultiplier *= ScreenFadeMultiplier(uvCoord.xy);
									// Fade out reflections of pixels near the max ray marches
									// float totalRayDist = length(initialSamplePosVS - samplePosVS);
									// reflectionMultiplier *= min(1.0 - (totalRayDist / _ReflectionParams.z), 1.0);
									// Lerp between original colour and new colour
									screenPixelColour = lerp(screenPixelColour, reflectedPixelColour, reflectionMultiplier);
									// Encode smoothness/specular into alpha channel for blur pass
									#if USING_DEFERRED_RENDERING_PATH
										screenPixelColour.a = smoothness * 0.9 * specular.r * specular.g * specular.b;
									#else
										screenPixelColour.a = smoothness * 0.9;
									#endif
									foundReflectedPixel = true;
									break;
								}
							}
						#endif
						
						#if REFLECT_FAR_PIXELS
							if (!foundReflectedPixel)
							{
								// Find the UV coordinates of the reflected pixel
								uvCoord = ViewSpaceToScreenSpace(rayMarchVS);
								// Fade out reflections of pixels near the edges of the screen
								reflectionMultiplier *= FarPixelScreenFadeMultiplier(uvCoord.xy);
								if (reflectionMultiplier > 0.01)
								{
									// Get the reflected pixel
									fixed4 reflectedPixelColour = SampleTex2DLOD(_MainTex, uvCoord.xy);
									#if PHYSICALLY_BASED_REFLECTIONS && USING_DEFERRED_RENDERING_PATH
										// Multiply it by the calculated reflection tint colour colour
										// to get more physically accurate reflections
										reflectedPixelColour *= reflectionTint;
										//reflectedPixelColour *= lerp(fixed4(specular, 1.0), reflectionTint, reflectionValidity);
									#else
										#if USING_DEFERRED_RENDERING_PATH
											// Multiply it by the specular colour of the pixel it is reflected
											// from - not physically correct but may look more visually appealing
											reflectedPixelColour *= fixed4(specular, 1.0);
										#else
											reflectedPixelColour *= screenPixelColour;
										#endif
									#endif
									// Lerp between original colour and new colour
									screenPixelColour = lerp(screenPixelColour, reflectedPixelColour, reflectionMultiplier);
									// Encode smoothness/specular into alpha channel for blur pass
									#if USING_DEFERRED_RENDERING_PATH
										screenPixelColour.a = smoothness * 0.9 * specular.r * specular.g * specular.b;
									#else
										screenPixelColour.a = smoothness * 0.9;
									#endif
								}
								else { screenPixelColour.a = 1.0; }
							}
						#else
							// If far pixels are turned off still store smoothness/specular data
							// in alpha channel so blur works exactly the same 
							if (!foundReflectedPixel)
							{
								if (reflectionMultiplier > 0.01)
								{
									// Encode smoothness/specular into alpha channel for blur pass
									#if USING_DEFERRED_RENDERING_PATH
										screenPixelColour.a = smoothness * 0.9 * specular.r * specular.g * specular.b;
									#else
										screenPixelColour.a = smoothness * 0.9;
									#endif
								}
								else { screenPixelColour.a = 1.0; }
							}
						#endif
					}
					else { screenPixelColour.a = 1.0; }
				}
			#endif

			return screenPixelColour;
		}

		fixed4 CombinePassFragment (v2f IN) : SV_Target
		{
			// Get the current colour of the pixel - modified by the first pass
			fixed4 screenPixelColour = tex2D(_MainTex, IN.uv);

			// Get depth and normals data
			float depth = 1.0;
			float3 viewSpaceNormal = 1.0;
			float3 decodedNormal = 1.0;
			float3 worldSpacePos = 0.0;
			float3 worldSpacePosDelta = 0.0;
			GetDepthAndNormalsData(depth, decodedNormal, viewSpaceNormal, worldSpacePos, worldSpacePosDelta, IN);

			#if REFLECT_NEAR_PIXELS || REFLECT_FAR_PIXELS
				// Combine (potentially) downsampled SSRR pass back into this pass
				fixed4 SSRRPixelColour = SampleTex2DLOD(_SSRRTexture, IN.uv);
				// if (SSRRPixelColour.a < 0.95)
				// {
				// 	// If not much downsampling, combine all SSRR pixels
				// 	if (_ReflectionParams3.x < 2.1) { screenPixelColour = SSRRPixelColour; }
				// 	// Otherwise, don't combine edge SSRR pixels
				// 	screenPixelColour = SSRRPixelColour;
				// 	// else
				// 	// {
				// 	// 	// Uses a simple cross gradient filter to detect edges
				// 	// 	fixed3 edgeDetectPixel1 = screenPixelColour.rgb;
				// 	// 	fixed3 edgeDetectPixel2 = SampleTex2DLOD(_MainTex, IN.uv + half2(-_MainTex_TexelSize.x, -_MainTex_TexelSize.y)).rgb;
				// 	// 	fixed3 edgeDetectPixel3 = SampleTex2DLOD(_MainTex, IN.uv + half2(+_MainTex_TexelSize.x, -_MainTex_TexelSize.y)).rgb;
				// 	// 	fixed3 edgeDetectDiff = (edgeDetectPixel1 * 2.0) - edgeDetectPixel2 - edgeDetectPixel3;
				// 	// 	fixed edgeDetectLen = dot(edgeDetectDiff, edgeDetectDiff);
				// 	// 	if (edgeDetectLen < 0.01) { screenPixelColour = SSRRPixelColour; }
				// 	// }
				// }
				// Calculate reflection multiplier in full resolution pass to minimise downsampling edge artefacts
				#if USING_DEFERRED_RENDERING_PATH
					fixed reflectionMultiplier = CalculateReflectionStrength(SampleTex2DLOD(_CameraGBufferTexture1, IN.uv), IN, depth, viewSpaceNormal);
				#else
					fixed4 gBuffer1Col = fixed4(screenPixelColour.r, screenPixelColour.g, screenPixelColour.b, 1.0);
					fixed reflectionMultiplier = CalculateReflectionStrength(gBuffer1Col, IN, depth, viewSpaceNormal);
				#endif
				if (reflectionMultiplier > 0.01) { screenPixelColour = lerp(screenPixelColour, SSRRPixelColour, reflectionMultiplier); }
			#endif

			// Cloud Shadow Rendering
			if (depth < 1.0)
			{
				if (_FeaturesEnabled.y == 1)
				{
					screenPixelColour = CloudShadowsFragment(IN, screenPixelColour, depth, worldSpacePos);
				}
			}
			
			return screenPixelColour;
		}

		fixed4 BlurPassFragment (v2f IN) : SV_Target
		{
			// Get the original colour of the pixel
			fixed4 screenPixelColour = tex2D(_MainTex, IN.uv);

			int blurQualityInt = (int)_ReflectionParams3.y;

			fixed sData = screenPixelColour.a;
			// fixed blurAmount = 0.005 * _ReflectionParams2.w;
			fixed blurAmount = (0.015 * _ReflectionParams2.w) / _ReflectionParams3.y;
			fixed denominator = 0.0;
			fixed gd = 0.0;
			fixed4 samplePixelColour;
			fixed4 blurredPixelColour = 0.0;

			// Loop through a grid of surrounding pixels (including the original one)
			for (int x = -blurQualityInt; x <= blurQualityInt; x++)
			{
				for (int y = -blurQualityInt; y <= blurQualityInt; y++)
				{
					// Sample the given pixel
					samplePixelColour = SampleTex2DLOD(_MainTex, float2(IN.uv.x + x * blurAmount, IN.uv.y + y * blurAmount));
					// Only add the sample if it is marked to be blurred and has a smoothness/specular value 
					// similar to that of the original pixel
					// We do this so that (for the most part) different objects are not blurred together
					if (samplePixelColour.a < 0.95 && abs(samplePixelColour.a - sData) < 0.01)
					{
						gd = GaussianDistribution((float)x, 7.0);
						blurredPixelColour += samplePixelColour * gd;
						denominator += gd;
					}
				}
			}

			// If no samples were added just use the original pixel colour
			if (denominator < 0.001) { blurredPixelColour = screenPixelColour; }
			else
			{
				// Divide by total strength of samples
				blurredPixelColour /= denominator;
				// Set the alpha channel to the alpha of the original pixel
				blurredPixelColour.a = screenPixelColour.a;
			}

			return blurredPixelColour;
		}

		fixed PixelVariance (fixed4 p1, fixed4 p2)
		{
			return (abs(p1.r - p2.r) + abs(p1.g - p2.g) + abs(p1.b - p2.b)) * 0.333;
		}

		fixed4 FilterPassFragment (v2f IN) : SV_Target
		{
			int downsamplePixelCount = _FilteringParams.y - 1;
			fixed4 originalPixel = SampleTex2DLOD(_MainTex, IN.uv);
			fixed4 filteredPixelCol = originalPixel;
			half2 texelSize = _MainTex_TexelSize / _FilteringParams.x;
			fixed4 sampleCol = 0.0;
			fixed denominatorX = 0.0;
			fixed denominatorY = 0.0;
			fixed filterMultiplier = 1.0;
			// Get samples in a cross pattern
			for (int px = -downsamplePixelCount; px <= downsamplePixelCount; px++)
			{
				// Don't sample middle pixel again
				if (px != 0) 
				{ 
					filteredPixelCol += SampleTex2DLOD(_MainTex, IN.uv + half2(texelSize.x * px, 0.0));
					// sampleCol = SampleTex2DLOD(_MainTex, IN.uv + half2(texelSize.x * px, 0.0)); 
					// filterMultiplier = PixelVariance(sampleCol, originalPixel);
					// filteredPixelCol += sampleCol * filterMultiplier;
					// denominatorX += filterMultiplier;
				}
			}
			for (int py = -downsamplePixelCount; py <= downsamplePixelCount; py++)
			{
				// Don't sample middle pixel again
				if (py != 0) 
				{ 
					filteredPixelCol += SampleTex2DLOD(_MainTex, IN.uv + half2(0.0, texelSize.x * py));
					// sampleCol = SampleTex2DLOD(_MainTex, IN.uv + half2(0.0, texelSize.x * py));
					// filterMultiplier = PixelVariance(sampleCol, originalPixel);
					// filteredPixelCol += sampleCol * filterMultiplier;
					// denominatorY += filterMultiplier;
				}
			}
			filteredPixelCol /= ((downsamplePixelCount * 4.0) + 1.0);
			// filteredPixelCol /= (denominatorX + denominatorY + 1.0);
			return filteredPixelCol;
		}

	ENDCG

	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		// 0: FOG AND CLOUD RENEDERING PASS
		Pass
		{
			CGPROGRAM
				#pragma exclude_renderers flash
				#pragma vertex vert
				#pragma fragment FirstPassFragment
				#pragma target 3.0
			ENDCG
		}

		// 1: SSRR PASS (DOWNSAMPLED)
		Pass
		{
			CGPROGRAM
				#pragma exclude_renderers flash
				#pragma vertex vert
				#pragma fragment SSRRPassFragment
				#pragma target 3.0
			ENDCG
		}

		// 2: SSRR COMBINE AND CLOUD SHADOWS PASS
		Pass
		{
			CGPROGRAM
				#pragma exclude_renderers flash
				#pragma vertex vert
				#pragma fragment CombinePassFragment
				#pragma target 3.0
			ENDCG
		}

		// 3: BLUR PASS
		Pass
		{
			CGPROGRAM
				#pragma exclude_renderers flash
				#pragma vertex vert
				#pragma fragment BlurPassFragment
				#pragma target 3.0
			ENDCG
		}

		// 4: FILTERING PASS
		Pass
		{
			CGPROGRAM
				#pragma exclude_renderers flash
				#pragma vertex vert
				#pragma fragment FilterPassFragment
				#pragma target 3.0
			ENDCG
		}

		// TODO: Create a single pass for 0+3 when not using SSRR

	}
}
