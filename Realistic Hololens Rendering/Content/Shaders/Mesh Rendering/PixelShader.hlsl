struct PixelShaderInput
{
	min16float4 Position : SV_Position;
	min16float2 UV : TexCoord;
};

Texture2D<float4> Color : register(t0);


SamplerState TextureSamplerState
{
	Filter = MIN_MAG_MIP_POINT;
};

min16float4 main(PixelShaderInput input) : SV_Target
{
	return Color.Sample(TextureSamplerState, input.UV);
}