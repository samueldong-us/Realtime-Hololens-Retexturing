cbuffer ScreenPositionBuffer : register(b0)
{
	float4 Bounds;
};

cbuffer ViewProjectionConstantBuffer : register(b1)
{
	float4x4 ViewProjection[2];
};

struct VertexShaderInput
{
	min16float3 Position : Position;
	min16float2 UV : TexCoord;
	uint InstanceID : SV_InstanceID;
};

struct VertexShaderOutput
{
	min16float4 Position : SV_Position;
	min16float2 UV : TexCoord;
	uint InstanceID : SV_RenderTargetArrayIndex;
};

VertexShaderOutput main(VertexShaderInput input)
{
	VertexShaderOutput output;
	output.Position = min16float4(input.Position.xy * Bounds.zw + Bounds.xy, 0.0, 1.0);
	output.UV = input.UV;
	output.InstanceID = input.InstanceID;
	return output;
}