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
	float2 UV : TexCoord;
};

VertexShaderOutput main(VertexShaderInput input)
{
	VertexShaderOutput output;
	output.Position = float4(0.0, 0.0, 0.0, 0.0);
	output.UV = float2(0.0, 0.0);
	return output;
}