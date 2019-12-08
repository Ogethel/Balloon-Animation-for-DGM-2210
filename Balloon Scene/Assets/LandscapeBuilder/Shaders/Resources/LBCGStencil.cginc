// Landscape Builder. Copyright (c) 2016-2019 SCSM Pty Ltd. All rights reserved.

// Stencil Layers
Texture2DArray<float4> stencilLayer128Tex2DArray;
Texture2DArray<float4> stencilLayer256Tex2DArray;
Texture2DArray<float4> stencilLayer512Tex2DArray;
Texture2DArray<float4> stencilLayer1024Tex2DArray;
Texture2DArray<float4> stencilLayer2048Tex2DArray;
Texture2DArray<float4> stencilLayer4096Tex2DArray;
Texture2DArray<float4> stencilLayer8192Tex2DArray;

//SamplerState sampler_stencilLayer128Tex2DArray;
//SamplerState sampler_stencilLayer256Tex2DArray;
//SamplerState sampler_stencilLayer512Tex2DArray;
//SamplerState sampler_stencilLayer1024Tex2DArray;
//SamplerState sampler_stencilLayer2048Tex2DArray;
//SamplerState sampler_stencilLayer4096Tex2DArray;
//SamplerState sampler_stencilLayer8192Tex2DArray;


// Get the values of a pixel within a compressed stencil layer texture
// posN: normalised (landscape) xy position or uv
// slResolution: 128, 256, 512, 1024, 2048, 4096 or 8192
// txtArrayIdx is the Texture2D slot within the Texture2DArray
// NOTE: stencilLayer128Tex2DArray to stencilLayer8096Tex2DArrays must already be populated
// TODO This could be returned as a half rather than a float
float GetStencilLayerPoint(float2 posN, int slResolution, uint txtArrayIdx)
{
	float2 pixelRG = float2(0.0, 0.0);

	// Compressed texture is flipped on Y axis for display in Stencil editor
	if (slResolution == 1024) { pixelRG = stencilLayer1024Tex2DArray[uint3(posN.x * (slResolution - 1), (1.0 - posN.y) * (slResolution - 1), txtArrayIdx)].xy; }
	else if (slResolution == 2048) { pixelRG = stencilLayer2048Tex2DArray[uint3(posN.x * (slResolution - 1), (1.0 - posN.y) * (slResolution - 1), txtArrayIdx)].xy; }
	else if (slResolution == 4096) { pixelRG = stencilLayer4096Tex2DArray[uint3(posN.x * (slResolution - 1), (1.0 - posN.y) * (slResolution - 1), txtArrayIdx)].xy; }
	else if (slResolution == 8192) { pixelRG = stencilLayer8192Tex2DArray[uint3(posN.x * (slResolution - 1), (1.0 - posN.y) * (slResolution - 1), txtArrayIdx)].xy; }
	else if (slResolution == 512) { pixelRG = stencilLayer512Tex2DArray[uint3(posN.x * (slResolution - 1), (1.0 - posN.y) * (slResolution - 1), txtArrayIdx)].xy; }
	else if (slResolution == 256) { pixelRG = stencilLayer256Tex2DArray[uint3(posN.x * (slResolution - 1), (1.0 - posN.y) * (slResolution - 1), txtArrayIdx)].xy; }
	else if (slResolution == 128) { pixelRG = stencilLayer128Tex2DArray[uint3(posN.x * (slResolution - 1), (1.0 - posN.y) * (slResolution - 1), txtArrayIdx)].xy; }

	float pixelValue = ((pixelRG.x * 255.0 * 256.0) + (pixelRG.y * 255.0));

	// Clamp 0-65535
	if (pixelValue < 0.0) { return 0.0; }
	else if (pixelValue > 65535.0) { return 65535.0; }
	else { return pixelValue; }
}
