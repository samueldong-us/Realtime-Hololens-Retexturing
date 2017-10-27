struct PixelShaderInput
{
	float4 Position : SV_Position;
	float2 UV : TexCoord;
};

Texture2D<float4> Color : register(t0);

SamplerState TextureSamplerState
{
	Filter = MIN_MAG_MIP_LINEAR;
};

float4 main(PixelShaderInput input) : SV_Target
{
	return float4(Color.Sample(TextureSamplerState, input.UV).rgb / 2.0, 1.0);
}