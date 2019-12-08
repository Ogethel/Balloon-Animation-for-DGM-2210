// Landscape Builder. Copyright (c) 2016-2019 SCSM Pty Ltd. All rights reserved.
// Common library of variables and methods used for terrain operations
// Requires LBCGCalc.cginc

#define LB_MAX_SPLAT_TEX 16

// Heightmap variables
int hmapRes;
Texture2D<float4> terrainNormals;

// DX11 supports 128 textures and 16 samplers
SamplerState LBLinearClampSampler;
SamplerState LBLinearRepeatSampler;
SamplerState LBPointClampSampler;
SamplerState LBLinearPointRepeatSampler;

// Terrain variables
float terrainWidth;
float terrainLength;
float terrainHeight;
float3 terrainWorldPos;

// Landscape variables
float3 landscapePos;
float2 landscapeSize;

// Filter variables
// csfilterType Height: 0, Inclination: 1, Map: 2, Stencil: 3, Area: 4, Texture: 5, Proximity: 6
// filterMode AND: 0, OR: 1, NOT: 2.
// areaRect.x: xMin, areaRect.y: yMin, areaRect.z: xMax, areaRect.w: yMax
struct LBFilter
{
	uint csfilterType;
	int filterMode;
	uint lbTexIdx;
	int stencilLayerResolution;
	int stencilLayerTex2DArrIdx;
	float4 areaRect;
	float minHeight;
	float maxHeight;
	float minInclination;
	float maxInclination;
};

// LBMap variables
Texture2DArray<float4> mapsTex2DArray;
SamplerState sampler_mapsTex2DArray;

// Get the position within a flattened 2D array
// NOT IN USE
float GetPos1D(uint2 id, uint width)
{
	return (id.y * width) + id.x;
}

// Get x,y position given the width of the 2D texture
uint2 Get2DPos(uint id, uint width)
{
	// Will be truncated... to an uint (hopefully)
	// maybe floor rather than trunc
	uint y = trunc((float)id / (float)width);
	return uint2(id - (width * y), y);
}

// Get normalised terrain position
float2 GetTopoPos2DN(uint2 id)
{
	return float2((float)id.x / (float)(hmapRes - 1), (float)id.y / (float)(hmapRes - 1));
}

// Get terrain heightmap position from terrain normalised position.
uint2 GetTopoPos2D(float2 pos2DN)
{
	return uint2(round(pos2DN.x * float(hmapRes - 1)), round(pos2DN.y * float(hmapRes - 1)));
}

// Get terrain position in metres
float2 GetTopoPos2DM(uint2 id)
{
	return float2((float)id.x / (float)(hmapRes - 1) * terrainWidth, (float)id.y / (float)(hmapRes - 1) * terrainLength);
}

// Get landscape position in metres from bottom left corner of landscape
float2 GetTopoLandscapePos2DM(uint2 id)
{
	float2 pos2D = GetTopoPos2DM(id);
	return float2(pos2D.x + terrainWorldPos.x - landscapePos.x, pos2D.y + terrainWorldPos.z - landscapePos.z);
}

// Get landscape position in metres from bottom left corner of landscape by supplying a normalised position in landscape
float2 GetTopoLandscapePos2DMfromN(float2 landscapePosN)
{
	return float2(landscapePosN.x * landscapeSize.x, landscapePosN.y * landscapeSize.y);
}

// Get the normalised landscape position from a normalised terrain position
// If landscapeSize is not defined, 0,0 will be returned
float2 GetTopoLandscapePos2DN(float2 terrainPosN)
{
	// Avoid div0
	if (!isnan(landscapeSize.x))
	{
		return float2(((terrainPosN.x * terrainWidth) + terrainWorldPos.x - landscapePos.x) / landscapeSize.x, ((terrainPosN.y * terrainLength) + terrainWorldPos.z - landscapePos.z) / landscapeSize.y);
	}
	else { return float2(0.0, 0.0); }
}

// Return the normalised height from the supplied height data. Assumes
// heightmap resolution = hmapRes
float LBGetTopoHeightN(uint2 pos2D, StructuredBuffer<float> heights)
{
	//int hIdx = (pos2D.y * hmapRes) + pos2D.x;
	return heights[(pos2D.y * hmapRes) + pos2D.x];
}

// Return the Steepness or slope of the terrain normals (0.0-90.0)
// Requires the terrainNormals texture to be populated.
float LBGetSteepness(float2 pos2DN)
{
	// Terrain normals in Red channel as 0.0 - 1.0 values which represent 0.0-90.0 degrees.
	// Need to use Linear Clamp to get consistent sampling across the texture.
	return terrainNormals.SampleLevel(LBLinearClampSampler, pos2DN, 0).r * 90.0;
}

// Return the Steepness or slope of the terrain normals (0.0-1.0)
// Requires the terrainNormals texture to be populated.
float LBGetSteepnessN(float2 pos2DN)
{
	// Terrain normals in Red channel as 0.0 - 1.0 values which represent 0.0-90.0 degrees.
	// Need to use Linear Clamp to get consistent sampling across the texture.
	return terrainNormals.SampleLevel(LBLinearClampSampler, pos2DN, 0).r;
}

// Get the values of a pixel within a Map texture
// posN: normalised (landscape) xy position or uv
// dimmensions: width and height of the texture
// mapsTex2DArray must already be populated
float GetMapPoint(float2 posN, uint txtArrayIdx, uint width, uint height)
{
	return LBGrayScale(mapsTex2DArray[uint3(posN.x * width, posN.y * height, txtArrayIdx)].xyz);
}

// Returns the Normalised height (0.0-1.0) which is interpolated between
// the nearest heightmap points.
float LBGetInterpolatedHeightN(float2 pos2DN, StructuredBuffer<float> heights)
{
	uint2 terrainPos2D = GetTopoPos2D(pos2DN);

	// TODO Could potentially check some extra neighbouring terrains' edge heights data
	// that are passed into shader - so we get consistent heights along terrain borders.

	// Get min/max x,y points
	uint2 minPos2D = uint2(trunc(pos2DN.x * float(hmapRes - 1)), trunc(pos2DN.y * float(hmapRes - 1)));

	// Check if we're on an edge
	if (minPos2D.x == uint(hmapRes - 1)) { --minPos2D.x; }
	if (minPos2D.y == uint(hmapRes - 1)) { --minPos2D.y; }

	uint2 maxPos2D = uint2(minPos2D.x + 1, minPos2D.y + 1);

	// Get surrounding heights
	float h1 = LBGetTopoHeightN(uint2(terrainPos2D.x, minPos2D.y), heights);
	float h2 = LBGetTopoHeightN(uint2(terrainPos2D.x, maxPos2D.y), heights);
	float h3 = LBGetTopoHeightN(uint2(minPos2D.x, terrainPos2D.y), heights);
	float h4 = LBGetTopoHeightN(uint2(maxPos2D.x, terrainPos2D.y), heights);
	// Just avg 4 surrounding points (which is closest to Unity's GetInterpolatedHeight)
	// NOTE: Including diagonals doesn't improve it (slower and more jaggard)
	return (h1 + h2 + h3 + h4) / 4.0;

	//return LBGetTopoHeightN(terrainPos2D, heights);

	// The following returns a bit more jaggard edge with outline values
	// Get normalised min/max positions
	//float2 minPos2DN = float2((float)minPos2D.x / (float)(hmapRes - 1), (float)minPos2D.y / (float)(hmapRes - 1));
	//float2 maxPos2DN = float2((float)maxPos2D.x / (float)(hmapRes - 1), (float)maxPos2D.y / (float)(hmapRes - 1));

	//float deltaX = maxPos2DN.x - minPos2DN.x;
	//float deltaY = maxPos2DN.y - minPos2DN.y;
	//float y = maxPos2DN.y - pos2DN.y;
	//float x = maxPos2DN.x - pos2DN.x;

	//float hA = h1 + ((y / deltaY) * (h2 - h1));
	//float hB = h3 + ((y / deltaY) * (h4 - h3));
	//return hA + ((x / deltaX) * (hB - hA));
}

// Returns the Normalised height (0.0-1.0) which is interpolated between
// the nearest heightmap points. This method is typically called when
// using a low-res landscape-wide heightmap.
float LBGetInterpolatedHeightN(float2 pos2DN, StructuredBuffer<float> heights, uint heightmapResolution)
{
	uint2 terrainPos2D = uint2(round(pos2DN.x * float(heightmapResolution - 1)), round(pos2DN.y * float(heightmapResolution - 1)));
	//uint2 terrainPos2D = uint2(trunc(pos2DN.x * float(heightmapResolution - 1)), trunc(pos2DN.y * float(heightmapResolution - 1)));

	// Get min/max x,y points
	uint2 minPos2D = terrainPos2D;

	// Check if we're on an edge
	if (minPos2D.x == uint(heightmapResolution - 1)) { --minPos2D.x; }
	if (minPos2D.y == uint(heightmapResolution - 1)) { --minPos2D.y; }

	uint2 maxPos2D = uint2(minPos2D.x + 1, minPos2D.y + 1);

	// Get surrounding heights
	float h = heights[(minPos2D.y * heightmapResolution) + terrainPos2D.x];
	h += heights[(maxPos2D.y * heightmapResolution) + terrainPos2D.x];
	h += heights[(terrainPos2D.y * heightmapResolution) + minPos2D.x];
	h += heights[(terrainPos2D.y * heightmapResolution) + maxPos2D.x];
	// Just avg 4 surrounding points (which is closest to Unity's GetInterpolatedHeight)
	return h / 4.0;
}