cbuffer ModelConstantBuffer : register(b0)
{
	float4x4 model;
};

cbuffer ViewProjectionConstantBuffer : register(b1)
{
	float4x4 viewProjection[2];
};

struct VertexShaderInput
{
	float3 position : POSITION;
	float2 uv : TEXCOORD0;
	uint instanceId : SV_InstanceID;
};

struct VertexShaderOutput
{
	float4 position : SV_POSITION;
	float2 uv : TEXCOORD0;
	uint renderTargetId : SV_RenderTargetArrayIndex;
};

VertexShaderOutput main(VertexShaderInput input)
{
	VertexShaderOutput output;
	float4 position = float4(input.position, 1.0f);
	position = mul(mul(position, model), viewProjection[input.instanceId % 2]);
	output.position = (float4)position;
	output.uv = input.uv;
	output.renderTargetId = input.instanceId % 2;
	return output;
}