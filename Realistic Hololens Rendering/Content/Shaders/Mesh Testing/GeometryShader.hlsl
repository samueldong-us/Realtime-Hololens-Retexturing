struct GeometryShaderInput
{
	min16float4 Position : SV_POSITION;
	min16float4 Normal : NORMAL;
	uint RenderTargetId : TEXCOORD;
};

struct GeometryShaderOutput
{
	min16float4 Position : SV_POSITION;
	min16float4 Normal : NORMAL;
	uint RenderTargetId : SV_RenderTargetArrayIndex;
};

[maxvertexcount(3)]
void main(triangle GeometryShaderInput input[3], inout TriangleStream<GeometryShaderOutput> outputStream)
{
	GeometryShaderOutput output;
	[unroll(3)]
	for (int i = 0; i < 3; i++)
	{
		output.Position = input[i].Position;
		output.Normal = input[i].Normal;
		output.RenderTargetId = input[i].RenderTargetId;
		outputStream.Append(output);
	}
}