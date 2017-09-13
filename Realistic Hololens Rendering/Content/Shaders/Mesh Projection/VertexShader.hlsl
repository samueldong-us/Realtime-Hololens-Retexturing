cbuffer TransformConstantBuffer : register(b0)
{
	float4x4 VertexModel;
	float4x4 NormalModel;
};

cbuffer ViewProjectionConstantBuffer : register(b1)
{
	float4x4 ViewProjection[2];
};

struct VertexShaderInput
{
	min16float3 Position : Position;
	min16float3 Normal : Normal;
};

struct VertexShaderOutput
{
	min16float4 Position : SV_Position;
	min16float3 WorldSpace : Position;
};

VertexShaderOutput main(VertexShaderInput input)
{
	VertexShaderOutput output;
	output.Position = min16float4(0.0, 0.0, 0.0, 0.0);
	min16float4 position = min16float4(input.Position, 1.0);
	position = mul(position, VertexModel);
	output.WorldSpace = position.xyz;
	return output;
}