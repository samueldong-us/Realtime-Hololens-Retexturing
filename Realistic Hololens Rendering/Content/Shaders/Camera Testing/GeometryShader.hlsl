struct GeometryShaderInput
{
	min16float4 position : SV_POSITION;
	min16float2 uv : TEXCOORD0;
	uint renderTargetId : TEXCOORD1;
};

struct GeometryShaderOutput
{
	min16float4 position : SV_POSITION;
	min16float2 uv : TEXCOORD0;
	uint renderTargetId : SV_RenderTargetArrayIndex;
};

[maxvertexcount(3)]
void main(triangle GeometryShaderInput input[3], inout TriangleStream<GeometryShaderOutput> outputStream)
{
	GeometryShaderOutput output;
	[unroll(3)]
	for (int i = 0; i < 3; i++)
	{
		output.position = input[i].position;
		output.uv = input[i].uv;
		output.renderTargetId = input[i].renderTargetId;
		outputStream.Append(output);
	}
}