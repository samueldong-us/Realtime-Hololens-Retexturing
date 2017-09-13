#define Bias 0.005

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

PixelShaderOutput main(PixelShaderInput input)
{
	PixelShaderOutput output;
	output.Color = min16float4(1.0, 1.0, 1.0, 1.0);
	return output;
}