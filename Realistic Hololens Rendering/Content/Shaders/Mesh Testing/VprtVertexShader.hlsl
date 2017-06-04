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
	min16float3 Position : POSITION;
	min16float3 Normal : NORMAL;
	uint InstanceId : SV_InstanceID;
};

struct VertexShaderOutput
{
	min16float4 Position : SV_POSITION;
	min16float4 Normal : NORMAL;
	uint RenderTargetId : SV_RenderTargetArrayIndex;
};

VertexShaderOutput main(VertexShaderInput input)
{
	VertexShaderOutput output;
	float4 position = float4(input.Position, 1.0f);
	position = mul(mul(position, VertexModel), ViewProjection[input.InstanceId % 2]);
	output.Position = (min16float4)position;
	float4 normal = float4(input.Normal, 0.0f);
	normal = mul(normal, NormalModel);
	output.Normal = (min16float4)normal;
	output.RenderTargetId = input.InstanceId % 2;
	return output;
}