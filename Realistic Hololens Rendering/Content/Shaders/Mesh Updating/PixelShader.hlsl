struct PixelShaderInput
{
	min16float4 Position : SV_POSITION;
	min16float2 UV : TEXCOORD;
};

struct PixelShaderOutput
{
	min16float4 Color : SV_Target0;
	// min16float2 QualityAndTime : SV_Target1;
};

Texture2D<float4> Color : register(t0);
Texture2D<float2> QualityAndTime : register(t1);

SamplerState TextureSamplerState
{
	Filter = MIN_MAG_MIP_POINT;
};

PixelShaderOutput main(PixelShaderInput input)
{
	PixelShaderOutput output;
	output.Color = Color.Sample(TextureSamplerState, input.UV);
	// output.QualityAndTime = QualityAndTime.Sample(TextureSamplerState, input.UV);
	return output;
}