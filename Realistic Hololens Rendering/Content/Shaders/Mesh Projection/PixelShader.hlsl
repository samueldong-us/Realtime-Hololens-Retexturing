#define Bias 0.000

struct PixelShaderInput
{
	min16float4 Position : SV_POSITION;
	min16float3 WorldSpace : POSITION;
};

struct PixelShaderOutput
{
	min16float4 Color : SV_Target0;
	// min16float2 QualityAndTime : SV_Target1;
};

cbuffer CameraConstantBuffer : register(b2)
{
	float4x4 CameraViewProjection;
};


// Texture2D<float2> QualityAndTime : register(t0);

Texture2D<uint> LuminanceTexture : register(t1);
Texture2D<uint2> ChrominanceTexture : register(t2);
Texture2D<float> Shadow : register(t3);


SamplerState TextureSamplerState
{
	Filter = MIN_MAG_MIP_POINT;
};

min16float3 YuvToRgb(min16float2 textureUV);

PixelShaderOutput main(PixelShaderInput input)
{
	min16float4 lightSpace = mul(min16float4(input.WorldSpace, 1.0), CameraViewProjection);
	if (lightSpace.w < 0.0)
	{
		discard;
	}
	lightSpace.xyz /= lightSpace.w;
	if (lightSpace.x < -1.0 || lightSpace.x > 1.0 ||
		lightSpace.y < -1.0 || lightSpace.y > 1.0 ||
		lightSpace.z < 0.0 || lightSpace.z > 1.0)
	{
		discard;
	}
	lightSpace.x = (lightSpace.x + 1.0) / 2.0;
	lightSpace.y = (lightSpace.y + 1.0) / -2.0;
	float shadowDepth = Shadow.Sample(TextureSamplerState, lightSpace.xy);
	lightSpace.y *= -1;
	if (shadowDepth < lightSpace.z + Bias)
	{
		// discard;
	}

	PixelShaderOutput output;
	output.Color = min16float4(YuvToRgb(lightSpace.xy), 1.0);
    /*
	float normalized = shadowDepth * 2.0 - 1.0;
	normalized = (2.0 * 0.05 * 10.0) / (10.0 + 0.05 - normalized * (10.0 - 0.05));
	output.Color = min16float4(1.0, normalized / 10.0, 1.0, 1.0);
	*/
	// output.QualityAndTime = min16float2(0.0, 0.0);
	return output;
}

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