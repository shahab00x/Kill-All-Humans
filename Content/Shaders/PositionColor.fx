//------------------------------------------------------------
// Microsoft® XNA Game Studio Creator's Guide, Second Edition
// by Stephen Cawood and Pat McGee 
// Copyright (c) McGraw-Hill/Osborne. All rights reserved.
// https://www.mhprofessional.com/product.php?isbn=0071614060
//------------------------------------------------------------
float4x4 wvpMatrix  : WORLDVIEWPROJ;

struct VSinput
{
	float4 position : POSITION0;
	float4 color	: COLOR0;
};

struct VStoPS
{
	float4 position : POSITION0;
	float4 color	: COLOR0;
};

struct PSoutput
{
	float4 color	: COLOR0;
};

// alter vertex inputs
void VertexShaderFunction(in VSinput IN, out VStoPS OUT)
{
	// transform vertex
	OUT.position = mul(IN.position, wvpMatrix);
	OUT.color = IN.color;
}

// alter vs color output
void PixelShaderFunction(in VStoPS IN, out PSoutput OUT)
{
	float4 color = IN.color;
	OUT.color	 = clamp(color, 0, 1); // range between 0 and 1
}

// the shader starts here
technique BasicShader
{
	pass p0
	{ 
		// declare & initialize ps & vs
		vertexShader = compile vs_2_0 VertexShaderFunction(); //VertexShader can not be used & set vs higher than 1_1
        pixelShader  = compile ps_2_0 PixelShaderFunction(); //PixelShader can not be used & set ps higher than 1_1
    
	}
}
