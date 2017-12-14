cbuffer LayoutConstantBuffer : register(b3)
{
	uint Offset;
	uint Size;
};

struct GeometryShaderInput
{
	float4 Position : SV_Position;
	float3 WorldSpace : Position;
};

struct GeometryShaderOutput
{
	float4 Position : SV_Position;
	float3 WorldSpace : Position;
};

float2 GetPosition(uint vertexID, uint primitiveID, uint offset, uint size);

[maxvertexcount(6)]
void main(triangle GeometryShaderInput inputs[3], uint primitiveID : SV_PrimitiveID, inout LineStream<GeometryShaderOutput> outputStream)
{
	[unroll]
	for (int i = 0; i < 3; i++)
	{
		int j = (i + 1) % 3;
		GeometryShaderOutput output;
		float2 texturePosition = GetPosition(i, primitiveID, Offset, Size);
		texturePosition.x = texturePosition.x * 2.0 - 1.0;
		texturePosition.y = texturePosition.y * -2.0 + 1.0;
		output.Position = float4(texturePosition, 0.0, 1.0);
		output.WorldSpace = inputs[i].WorldSpace;
		outputStream.Append(output);

		texturePosition = GetPosition(j, primitiveID, Offset, Size);
		texturePosition.x = texturePosition.x * 2.0 - 1.0;
		texturePosition.y = texturePosition.y * -2.0 + 1.0;
		output.Position = float4(texturePosition, 0.0, 1.0);
		output.WorldSpace = inputs[j].WorldSpace;
		outputStream.Append(output);
	}
}

float2 GetPosition(uint vertexID, uint primitiveID, uint offset, uint size)
{
	static float2 Offsets[6] = 
	{
		float2(0.0, 0.0),
		float2(1.0, 0.0),
		float2(0.0, 1.0),
		float2(1.0, 0.0),
		float2(1.0, 1.0),
		float2(0.0, 1.0)
	};
	primitiveID = primitiveID + offset;
	uint squareID = primitiveID / 2;
	float squareSize = 1.0 / size;
	float2 topLeft;
	topLeft.x = (squareID % size) * squareSize - 1.0;
	topLeft.y = (squareID / size) * squareSize - 1.0;
	return topLeft + Offsets[primitiveID % 2 * 3 + vertexID] * squareSize;
}