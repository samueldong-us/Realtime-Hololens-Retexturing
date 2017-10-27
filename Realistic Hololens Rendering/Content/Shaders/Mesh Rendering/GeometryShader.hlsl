#define Resolution 4096.0
#define Border 1.0

cbuffer LayoutConstantBuffer : register(b3)
{
	uint Offset;
	uint Size;
};

struct GeometryShaderInput
{
	float4 Position : SV_Position;
	float2 UV : TexCoord;
	uint RenderTargetId : SV_RenderTargetArrayIndex;
};

struct GeometryShaderOutput
{
	float4 Position : SV_Position;
	float2 UV : TexCoord;
	uint RenderTargetId : SV_RenderTargetArrayIndex;
};

float2 GetInnerUV(uint vertexID, uint primitiveID, uint offset, uint size);

[maxvertexcount(3)]
void main(triangle GeometryShaderInput inputs[3], uint primitiveID : SV_PrimitiveID, inout TriangleStream<GeometryShaderOutput> outputStream)
{
	[unroll]
	for (int i = 0; i < 3; i++)
	{
		GeometryShaderOutput output;
		output.Position = inputs[i].Position;
		output.UV = GetInnerUV(i, primitiveID, Offset, Size);
		output.RenderTargetId = inputs[i].RenderTargetId;
		outputStream.Append(output);
	}
}

float2 GetInnerUV(uint vertexID, uint primitiveID, uint offset, uint size)
{
	float pixel = 1.0 / Resolution * size;
	float2 Offsets[6] =
	{
		float2(Border * pixel, Border * pixel),
		float2(1.0 - 2.0 * Border * pixel, Border * pixel),
		float2(Border * pixel, 1.0 - 2.0 * Border * pixel),
		float2(1.0 - Border * pixel, 2.0 * Border * pixel),
		float2(1.0 - Border * pixel, 1.0 - Border * pixel),
		float2(2.0 * Border * pixel, 1.0 - Border * pixel)
	};
	primitiveID = primitiveID + offset;
	uint squareID = primitiveID / 2;
	float squareSize = 1.0 / size;
	float2 topLeft;
	topLeft.x = (squareID % size) * squareSize;
	topLeft.y = (squareID / size) * squareSize;
	return topLeft + Offsets[primitiveID % 2 * 3 + vertexID] * squareSize;
}