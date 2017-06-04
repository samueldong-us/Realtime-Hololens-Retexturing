struct PixelShaderInput
{
	min16float4 Position : SV_POSITION;
	min16float4 Normal : NORMAL;
};

min16float4 main(PixelShaderInput input) : SV_TARGET
{
	return input.Normal;
}