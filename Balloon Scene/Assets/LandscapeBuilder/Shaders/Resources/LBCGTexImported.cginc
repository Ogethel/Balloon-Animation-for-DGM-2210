// Landscape Builder. Copyright (c) 2016-2019 SCSM Pty Ltd. All rights reserved.

// Imported Textures
Texture2DArray<float4> texImportedTex2DArray;

// Get the values of a pixel within a compressed stencil layer texture
// posN: normalised (terrain) xy position or uv
// txtArrayIdx is the Texture2D slot within the Texture2DArray
float4 GetImportedTexturePoint(float2 posN, uint resolution, uint txtArrayIdx)
{
	return texImportedTex2DArray[uint3(posN.x * (resolution - 1), posN.y * (resolution - 1), txtArrayIdx)];
}
