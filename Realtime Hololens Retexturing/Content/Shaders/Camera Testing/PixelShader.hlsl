struct PixelShaderInput
{
	float4 position : SV_POSITION;
	float2 uv : TEXCOORD0;
};

Texture2D<uint> luminanceTexture : register(t0);
Texture2D<uint2> chrominanceTexture : register(t1);

float4 YuvToRgb(float2 textureUV)
{
	int3 location = int3(0, 0, 0);
	location.x = (int)(1408 * (1.0f - textureUV.x));
	location.y = (int)(792 * (1.0f - textureUV.y));
	uint y = luminanceTexture.Load(location).x;
	uint2 uv = chrominanceTexture.Load(location / 2).xy;
	int c = y - 16;
	int d = uv.x - 128;
	int e = uv.y - 128;
	int r = (298 * c + 409 * e + 128) >> 8;
	int g = (298 * c - 100 * d - 208 * e + 128) >> 8;
	int b = (298 * c + 516 * d + 128) >> 8;
	float4 rgb = float4(0.0f, 0.0f, 0.0f, 255.0f);
	rgb.x = max(0, min(255, r));
	rgb.y = max(0, min(255, g));
	rgb.z = max(0, min(255, b));
	return rgb / 255.0;
}

float4 main(PixelShaderInput input) : SV_TARGET
{
	return YuvToRgb(input.uv);
}