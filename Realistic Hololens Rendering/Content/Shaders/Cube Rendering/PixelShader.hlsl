struct PixelShaderInput
{
	min16float4 Position : SV_POSITION;
	min16float4 Normal : NORMAL;
	min16float2 UV : TEXCOORD;
};

cbuffer CameraDirectionConstantBuffer : register(b3)
{
	float4 Forward;
}

Texture2D<uint> LuminanceTexture : register(t0);
Texture2D<uint2> ChrominanceTexture : register(t1);

min16float3 YuvToRgb(min16float2 textureUV)
{
	int3 location = int3(0, 0, 0);
	location.x = (int)(1408 * (textureUV.x));
	location.y = (int)(792 * (1.0f - textureUV.y));
	uint y = LuminanceTexture.Load(location).x;
	uint2 uv = ChrominanceTexture.Load(location / 2).xy;
	int c = y - 16;
	int d = uv.x - 128;
	int e = uv.y - 128;
	int r = (298 * c + 409 * e + 128) >> 8;
	int g = (298 * c - 100 * d - 208 * e + 128) >> 8;
	int b = (298 * c + 516 * d + 128) >> 8;
	min16float3 rgb = float3(0.0f, 0.0f, 0.0f);
	rgb.x = max(0, min(255, r));
	rgb.y = max(0, min(255, g));
	rgb.z = max(0, min(255, b));
	bool invalid = (textureUV.x > 1.0 || textureUV.x < 0.0 || textureUV.y > 1.0 || textureUV.y < 0.0);
	if (invalid)
	{
		discard;
	}
	return rgb / 255.0;
}

min16float4 main(PixelShaderInput input) : SV_TARGET
{
	float alpha = max(0.0, dot(normalize(Forward.xyz), -normalize(input.Normal.xyz)));
	return min16float4(YuvToRgb(input.UV), alpha);
}