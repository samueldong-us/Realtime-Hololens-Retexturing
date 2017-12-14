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
	float3 WorldSpace : Position;
};

struct GeometryShaderOutput
{
	float4 Position : SV_Position;
	float3 WorldSpace : Position;
};

float2 GetInnerUV(uint vertexID, uint primitiveID, uint offset, uint size);
float2 GetOuterUV(uint vertexID, uint primitiveID, uint offset, uint size);
float2 UVToPosition(float2 uv);
float2 ProjectOntoSegment(float2 start, float2 end, float2 position);

[maxvertexcount(39)]
void main(triangle GeometryShaderInput inputs[3], uint primitiveID : SV_PrimitiveID, inout TriangleStream<GeometryShaderOutput> outputStream)
{
	GeometryShaderOutput innerVertices[3];
	GeometryShaderOutput outerVertices[3];
	[unroll]
	for (int i = 0; i < 3; i++)
	{
		innerVertices[i].Position = float4(UVToPosition(GetInnerUV(i, primitiveID, Offset, Size)), 0.0, 1.0);
		innerVertices[i].WorldSpace = inputs[i].WorldSpace;
		outerVertices[i].Position = float4(UVToPosition(GetOuterUV(i, primitiveID, Offset, Size)), 0.0, 1.0);
		outerVertices[i].WorldSpace = inputs[i].WorldSpace;
	}
	[unroll]
	for (int i = 0; i < 3; i++)
	{
		int j = (i + 1) % 3;
		GeometryShaderOutput firstBorder, secondBorder;
		firstBorder.Position = float4(ProjectOntoSegment(outerVertices[i].Position.xy, outerVertices[j].Position.xy, innerVertices[i].Position.xy), 0.0, 1.0);
		firstBorder.WorldSpace = inputs[i].WorldSpace;
		secondBorder.Position = float4(ProjectOntoSegment(outerVertices[i].Position.xy, outerVertices[j].Position.xy, innerVertices[j].Position.xy), 0.0, 1.0);
		secondBorder.WorldSpace = inputs[j].WorldSpace;

		outputStream.Append(outerVertices[i]);
		outputStream.Append(firstBorder);
		outputStream.Append(innerVertices[i]);

		outputStream.Append(firstBorder);
		outputStream.Append(secondBorder);
		outputStream.Append(innerVertices[i]);

		outputStream.Append(innerVertices[i]);
		outputStream.Append(secondBorder);
		outputStream.Append(innerVertices[j]);

		outputStream.Append(secondBorder);
		outputStream.Append(outerVertices[j]);
		outputStream.Append(innerVertices[j]);
	}
	[unroll]
	for (int i = 0; i < 3; i++)
	{
		outputStream.Append(innerVertices[i]);
	}
}

float2 GetOuterUV(uint vertexID, uint primitiveID, uint offset, uint size)
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
	topLeft.x = (squareID % size) * squareSize;
	topLeft.y = (squareID / size) * squareSize;
	return topLeft + Offsets[primitiveID % 2 * 3 + vertexID] * squareSize;
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

float2 UVToPosition(float2 uv)
{
	uv.x = uv.x * 2.0 - 1.0;
	uv.y = uv.y * -2.0 + 1.0;
	return uv;
}

float2 ProjectOntoSegment(float2 start, float2 end, float2 position)
{
	float2 direction = normalize(end - start);
	return dot(position - start, direction) * direction + start;
}