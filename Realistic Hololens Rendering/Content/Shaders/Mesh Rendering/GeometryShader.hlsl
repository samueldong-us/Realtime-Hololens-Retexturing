cbuffer LayoutConstantBuffer : register(b3)
{
	uint Offset;
	uint Size;
};

struct GeometryShaderInput
{
	min16float4 Position : SV_Position;
	min16float2 UV : TexCoord;
	uint RenderTargetId : SV_RenderTargetArrayIndex;
};

struct GeometryShaderOutput
{
	min16float4 Position : SV_Position;
	min16float2 UV : TexCoord;
	uint RenderTargetId : SV_RenderTargetArrayIndex;
};

min16float2 GetPosition(uint vertexID, uint primitiveID, uint offset, uint size);

[maxvertexcount(3)]
void main(triangle GeometryShaderInput inputs[3], uint primitiveID : SV_PrimitiveID, inout TriangleStream<GeometryShaderOutput> outputStream)
{
	[unroll]
	for (int i = 0; i < 3; i++)
	{
		GeometryShaderOutput output;
		output.Position = inputs[i].Position;
		output.UV = GetPosition(i, primitiveID, Offset, Size);
		output.RenderTargetId = inputs[i].RenderTargetId;
		outputStream.Append(output);
	}
}

min16float2 GetPosition(uint vertexID, uint primitiveID, uint offset, uint size)
{
	static min16float2 Offsets[6] = 
	{
		min16float2(0.0, 0.0),
		min16float2(1.0, 0.0),
		min16float2(0.0, 1.0),
		min16float2(1.0, 0.0),
		min16float2(1.0, 1.0),
		min16float2(0.0, 1.0)
	};
	primitiveID = primitiveID + offset;
	uint squareID = primitiveID / 2;
	float squareSize = 1.0 / size;
	min16float2 topLeft;
	topLeft.x = (squareID % size) * squareSize;
	topLeft.y = (squareID / size) * squareSize;
	return topLeft + Offsets[primitiveID % 2 * 3 + vertexID] * squareSize;
}