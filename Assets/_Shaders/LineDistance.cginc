
//float3 CalculateLineDistance( float4 h, float bias)
//{
//	bool raisedBevel = step(1, fmod(_ShaderFlags, 2));

//	h += bias+_BevelOffset;

//	float bevelWidth = max(.01, _OutlineWidth+_BevelWidth);

//	// Track outline
//	h -= .5;
//	h /= bevelWidth;
//	h = saturate(h+.5);

//	if(raisedBevel) h = 1 - abs(h*2.0 - 1.0);
//	h = lerp(h, sin(h*3.141592/2.0), _BevelRoundness);
//	h = min(h, 1.0-_BevelClamp);
//	h *= _Bevel * bevelWidth * _GradientScale * -2.0;

//	float3 va = normalize(float3(1.0, 0.0, h.y - h.x));
//	float3 vb = normalize(float3(0.0, -1.0, h.w - h.z));

//	return cross(va, vb);
//}
