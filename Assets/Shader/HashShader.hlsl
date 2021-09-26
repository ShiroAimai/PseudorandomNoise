#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
    StructuredBuffer<uint> _Hashes;
    StructuredBuffer<float3> _Positions;
    StructuredBuffer<float3> _Normals;
#endif

float4 _Config;

float3 GetHashColor()
{
    #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
        uint hash = _Hashes[unity_InstanceID];
        return (1.0 / 255.0) * float3
        (
            hash & 255,
            (hash >> 8) & 255,
            (hash >> 16) & 255
            //last byte is not used    
        );
    #else
        return 1.0; 
    #endif
}

void ConfigureProcedural()
{
    #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
    unity_ObjectToWorld = 0.0;
    unity_ObjectToWorld._m03_m13_m23_m33 = float4(
        _Positions[unity_InstanceID],
        1.0
    );
    unity_ObjectToWorld._m03_m13_m23 += _Config.z  * _Normals[unity_InstanceID]; //(_Config.z) displacement * normal direction
    unity_ObjectToWorld._m00_m11_m22 = _Config.y; // 1 / res
    #endif
}

void ShaderGraphFunction_float (float3 In, out float3 Out, out float3 Color) {
    Out = In;
    Color = GetHashColor();
}

void ShaderGraphFunction_half (half3 In, out half3 Out, out half3 Color) {
    Out = In;
    Color = GetHashColor();
}