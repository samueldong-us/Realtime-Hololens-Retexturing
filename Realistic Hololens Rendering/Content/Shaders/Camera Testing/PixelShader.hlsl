struct PixelShaderInput
{
	min16float4 position : SV_POSITION;
	min16float2 uv : TEXCOORD0;
};

Texture2D<uint> luminanceTexture : register(t0);
Texture2D<uint2> chrominanceTexture : register(t1);

min16float4 YuvToRgb(uint y, uint2 uv)
{
	int c = y - 16;
	int d = uv.x - 128;
	int e = uv.y - 128;
	int r = (298 * c + 409 * e + 128) >> 8;
	int g = (298 * c - 100 * d - 208 * e + 128) >> 8;
	int b = (298 * c + 516 * d + 128) >> 8;
	min16float4 rgb = float4(0.0f, 0.0f, 0.0f, 255.0f);
	rgb.x = max(0, min(255, r));
	rgb.y = max(0, min(255, g));
	rgb.z = max(0, min(255, b));
	return rgb / 255.0;
	/*
	min16float4 rgb = float4(0.0f, 0.0f, 0.0f, 255.0f);
	rgb.x = y + 1.14f * uv.y;
	rgb.y = y - 0.395f * uv.x - 0.581f * uv.y;
	rgb.z = y + 2.032 * uv.x;
	return rgb / 255.0;
	*/
}

min16float4 main(PixelShaderInput input) : SV_TARGET
{
	int3 location = int3(0, 0, 0);
	location.x = (int)(1408 * (1.0f - input.uv.x));
	location.y = (int)(792 * (1.0f - input.uv.y));
	uint y = luminanceTexture.Load(location).x;
	uint2 uv = chrominanceTexture.Load(location / 2).xy;
	return YuvToRgb(y, uv);
}