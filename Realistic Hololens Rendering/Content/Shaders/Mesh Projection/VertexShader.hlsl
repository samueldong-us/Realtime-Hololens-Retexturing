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
	float3 Position : Position;
	float3 Normal : Normal;
};

struct VertexShaderOutput
{
	float4 Position : SV_Position;
	float3 WorldSpace : Position;
};

VertexShaderOutput main(VertexShaderInput input)
{
	VertexShaderOutput output;
	output.Position = float4(0.0, 0.0, 0.0, 0.0);
	float4 position = float4(input.Position, 1.0);
	position = mul(position, VertexModel);
	output.WorldSpace = position.xyz;
	return output;
}