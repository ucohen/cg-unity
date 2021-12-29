#ifndef CG_UTILS_INCLUDED
#define CG_UTILS_INCLUDED

#define PI 3.141592653

// A struct containing all the data needed for bump-mapping
struct bumpMapData
{ 
    float3 normal;       // Mesh surface normal at the point
    float3 tangent;      // Mesh surface tangent at the point
    float2 uv;           // UV coordinates of the point
    sampler2D heightMap; // Heightmap texture to use for bump mapping
    float du;            // Increment size for u partial derivative approximation
    float dv;            // Increment size for v partial derivative approximation
    float bumpScale;     // Bump scaling factor
};


// Receives pos in 3D cartesian coordinates (x, y, z)
// Returns UV coordinates corresponding to pos using spherical texture mapping
float2 getSphericalUV(float3 pos)
{
    float r = sqrt(pos.x*pos.x + pos.y*pos.y + pos.z*pos.z);
    float theta = atan2(pos.z, pos.x);
    float phi = acos(pos.y/r);
    return float2(0.5 + theta/(2*PI), 1 - (phi/PI));
}

// Implements an adjusted version of the Blinn-Phong lighting model
fixed3 blinnPhong(float3 n, float3 v, float3 l, float shininess, fixed4 albedo, fixed4 specularity, float ambientIntensity)
{
    float3 h = normalize(l+v);
    fixed4 Ambient = albedo * ambientIntensity;
    fixed4 Diffuse = albedo * max(0, dot(n,l));
    fixed4 Specular = specularity * pow(max(0, dot(n,h)), shininess);
    return (Ambient + Diffuse + Specular);
}

// Returns the world-space bump-mapped normal for the given bumpMapData
float3 getBumpMappedNormal(bumpMapData i)
{
    // n_tangentspace = normalize(float3(-s*F_u, -s*F_v, 1))
    float F_prime_u = ( tex2D(i.heightMap, i.uv + float2(i.du, 0)) - tex2D(i.heightMap, i.uv) ) / i.du;
    float F_prime_v = ( tex2D(i.heightMap, i.uv + float2(0, i.dv)) - tex2D(i.heightMap, i.uv) ) / i.dv;

    float3 n_tangentspace = normalize(float3(-i.bumpScale * F_prime_u, -i.bumpScale * F_prime_v, 1));
    float3 binormal = normalize(cross(i.tangent, i.normal));

    // n_worldspace   = t*n_tangentspace.x + n*n_tangentspace.y + b*n_tangentspace.z
    float3 n_worldspace = i.tangent * n_tangentspace.x +
                          i.normal  * n_tangentspace.z +
                          binormal  * n_tangentspace.y;
    return normalize(n_worldspace);
}


#endif // CG_UTILS_INCLUDED
