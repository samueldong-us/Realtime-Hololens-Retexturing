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
	uint InstanceID : SV_InstanceID;
};

struct VertexShaderOutput
{
	float4 Position : SV_Position;
	float2 UV : TexCoord;
	uint RenderTargetId : SV_RenderTargetArrayIndex;
};

VertexShaderOutput main(VertexShaderInput input)
{
	VertexShaderOutput output;
	float4 position = float4(input.Position, 1.0f);
	float4 worldPosition = mul(position, VertexModel);
	position = mul(worldPosition, ViewProjection[input.InstanceID % 2]);
	output.Position = (float4)position;
	output.RenderTargetId = input.InstanceID % 2;
	output.UV = float2(0.0, 0.0);
	return output;
}