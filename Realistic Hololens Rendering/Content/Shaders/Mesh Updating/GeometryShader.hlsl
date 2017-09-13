cbuffer UpdateLayoutConstantBuffer : register(b2)
{
	uint OldOffset;
	uint NewOffset;
	uint OldSize;
	uint NewSize;
};

struct GeometryShaderInput
{
	min16float4 Position : SV_Position;
	min16float2 UV : TexCoord;
};

struct GeometryShaderOutput
{
	min16float4 Position : SV_Position;
	min16float2 UV : TexCoord;
};

min16float2 GetPosition(uint vertexID, uint primitiveID, uint offset, uint size);

[maxvertexcount(3)]
void main(triangle GeometryShaderInput inputs[3], uint primitiveID : SV_PrimitiveID, inout TriangleStream<GeometryShaderOutput> outputStream)
{
	[unroll]
	for (int i = 0; i < 3; i++)
	{
		GeometryShaderOutput output;
		min16float2 texturePosition = GetPosition(i, primitiveID, NewOffset, NewSize);
		texturePosition.x = texturePosition.x * 2.0 - 1.0;
		texturePosition.y = texturePosition.y * -2.0 + 1.0;
		output.Position = min16float4(texturePosition, 0.0, 1.0);
		output.UV = GetPosition(i, primitiveID, OldOffset, OldSize);
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