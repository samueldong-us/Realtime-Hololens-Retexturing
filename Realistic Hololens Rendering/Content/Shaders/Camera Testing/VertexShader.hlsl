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
	min16float3 position : POSITION;
	min16float2 uv : TEXCOORD0;
	uint instanceId : SV_InstanceID;
};

struct VertexShaderOutput
{
	min16float4 position : SV_POSITION;
	min16float2 uv : TEXCOORD0;
	uint renderTargetId : TEXCOORD1;
};

VertexShaderOutput main(VertexShaderInput input)
{
	VertexShaderOutput output;
	float4 position = float4(input.position, 1.0f);
	position = mul(mul(viewProjection[input.instanceId], model), position);
	output.position = (min16float4)position;
	output.uv = input.uv;
	output.renderTargetId = input.instanceId;
	return output;
}