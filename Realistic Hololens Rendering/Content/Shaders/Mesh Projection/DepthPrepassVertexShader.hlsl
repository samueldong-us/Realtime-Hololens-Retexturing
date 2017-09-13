cbuffer TransformConstantBuffer : register(b0)
{
	float4x4 VertexModel;
	float4x4 NormalModel;
};

cbuffer ViewProjectionConstantBuffer : register(b1)
{
	float4x4 ViewProjection[2];
};

cbuffer CameraConstantBuffer : register(b2)
{
	float4x4 CameraViewProjection;
};

struct VertexShaderInput
{
	min16float3 Position : Position;
	min16float3 Normal : Normal;
};

struct VertexShaderOutput
{
	min16float4 Position : SV_Position;
};

VertexShaderOutput main(VertexShaderInput input)
{
	VertexShaderOutput output;
	min16float4 position = min16float4(input.Position, 1.0);
	position = mul(mul(position, VertexModel), CameraViewProjection);
	output.Position = position;
	return output;
}