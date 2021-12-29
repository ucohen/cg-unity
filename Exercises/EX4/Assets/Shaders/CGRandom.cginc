#ifndef CG_RANDOM_INCLUDED
// Upgrade NOTE: excluded shader from DX11 because it uses wrong array syntax (type[size] name)
#pragma exclude_renderers d3d11
#define CG_RANDOM_INCLUDED

// Returns a psuedo-random float between -1 and 1 for a given float c
float random(float c)
{
    return -1.0 + 2.0 * frac(43758.5453123 * sin(c));
}

// Returns a psuedo-random float2 with componenets between -1 and 1 for a given float2 c 
float2 random2(float2 c)
{
    c = float2(dot(c, float2(127.1, 311.7)), dot(c, float2(269.5, 183.3)));

    float2 v = -1.0 + 2.0 * frac(43758.5453123 * sin(c));
    return v;
}

// Returns a psuedo-random float3 with componenets between -1 and 1 for a given float3 c 
float3 random3(float3 c)
{
    float j = 4096.0 * sin(dot(c, float3(17.0, 59.4, 15.0)));
    float3 r;
    r.z = frac(512.0*j);
    j *= .125;
    r.x = frac(512.0*j);
    j *= .125;
    r.y = frac(512.0*j);
    r = -1.0 + 2.0 * r;
    return r.yzx;
}

// Interpolates a given array v of 4 float2 values using bicubic interpolation
// at the given ratio t (a float2 with components between 0 and 1)
//
// [0]=====o==[1]
//         |
//         t
//         |
// [2]=====o==[3]
//
float bicubicInterpolation(float2 v[4], float2 t)
{
    float2 u = t * t * (3.0 - 2.0 * t); // Cubic interpolation

    // Interpolate in the x direction
    float x1 = lerp(v[0], v[1], u.x);
    float x2 = lerp(v[2], v[3], u.x);

    // Interpolate in the y direction and return
    return lerp(x1, x2, u.y);
}

// Interpolates a given array v of 4 float2 values using biquintic interpolation
// at the given ratio t (a float2 with components between 0 and 1)
float biquinticInterpolation(float2 v[4], float2 t)
{
    /* 10t^3 - 15t^4 + 6t^5 */
    float2 u = t * t * t * (10 - 15 * t + 6.0 * t * t); // Quintic interpolation

    // Interpolate in the x direction
    float x1 = lerp(v[0], v[1], u.x);
    float x2 = lerp(v[2], v[3], u.x);

    // Interpolate in the y direction and return
    return lerp(x1, x2, u.y);
}

// Interpolates a given array v of 8 float3 values using triquintic interpolation
// at the given ratio t (a float3 with components between 0 and 1)
float triquinticInterpolation(float3 v[8], float3 t)
{
    float3 u = t * t * t * (10 - 15 * t + 6.0 * t * t); // Quintic interpolation

    float z1 = lerp(v[0], v[1], u.z);
    float z2 = lerp(v[2], v[3], u.z);
    float z3 = lerp(v[4], v[5], u.z);
    float z4 = lerp(v[6], v[7], u.z);

    float y1 = lerp(z1, z2, u.y);
    float y2 = lerp(z3, z4, u.y);
    return lerp(y1, y2, u.x);
}

// Returns the value of a 2D value noise function at the given coordinates c
float value2d(float2 c)
{
    // get 4 grid corners containing c
    float2 topleft = float2(floor(c.x), floor(c.y));
    float2 topright= float2(ceil(c.x),  floor(c.y));
    float2 botleft = float2(floor(c.x),  ceil(c.y));
    float2 botright= float2(ceil(c.x),   ceil(c.y));
    
    // use 4 corners to sample a random float:
    float2 s0 = random2(topleft).x;
    float2 s1 = random2(topright).x;
    float2 s2 = random2(botleft).x;
    float2 s3 = random2(botright).x;
    float2 v[4] = {s0, s1, s2, s3};

    // return an interpolation sample
    return bicubicInterpolation(v, frac(c));
}

// Returns the value of a 2D Perlin noise function at the given coordinates c
float perlin2d(float2 c)
{
    // get 4 grid corners containing c
    float2 topleft = float2(floor(c.x), floor(c.y));
    float2 topright= float2(ceil(c.x),  floor(c.y));
    float2 botleft = float2(floor(c.x),  ceil(c.y));
    float2 botright= float2(ceil(c.x),   ceil(c.y));

    // generate 2-parndom numbers per corner
    float2 s0 = random2(topleft);
    float2 s1 = random2(topright);
    float2 s2 = random2(botleft);
    float2 s3 = random2(botright);

    // calculate distance vectors
    float2 d0 = topleft  - c;
    float2 d1 = topright - c;
    float2 d2 = botleft  - c;
    float2 d3 = botright - c;

    float2 v0 = dot(s0, d0);
    float2 v1 = dot(s1, d1);
    float2 v2 = dot(s2, d2);
    float2 v3 = dot(s3, d3);

    float2 v[4] = {v0, v1, v2, v3};

    // return bicubicInterpolation(v, frac(c));
    return biquinticInterpolation(v, frac(c));
}

// Returns the value of a 3D Perlin noise function at the given coordinates c
float perlin3d(float3 c)
{                    
    // get 8 grid corners containing c
    float3 xyz = float3(floor(c.x), floor(c.y), floor(c.z));
    float3 xyZ = float3(floor(c.x), floor(c.y), ceil(c.z));
    float3 xYz = float3(floor(c.x), ceil(c.y), floor(c.z));
    float3 xYZ = float3(floor(c.x), ceil(c.y), ceil(c.z));
    float3 Xyz = float3(ceil(c.x), floor(c.y), floor(c.z));
    float3 XyZ = float3(ceil(c.x), floor(c.y), ceil(c.z));
    float3 XYz = float3(ceil(c.x), ceil(c.y), floor(c.z));
    float3 XYZ = float3(ceil(c.x), ceil(c.y), ceil(c.z));

    // influence values using distance to point
    float3 v_xyz = dot(random3(xyz), xyz-c);
    float3 v_xyZ = dot(random3(xyZ), xyZ-c);
    float3 v_xYz = dot(random3(xYz), xYz-c);
    float3 v_xYZ = dot(random3(xYZ), xYZ-c);
    float3 v_Xyz = dot(random3(Xyz), Xyz-c);
    float3 v_XyZ = dot(random3(XyZ), XyZ-c);
    float3 v_XYz = dot(random3(XYz), XYz-c);
    float3 v_XYZ = dot(random3(XYZ), XYZ-c);

    float3 v[8] = {v_xyz, v_xyZ, v_xYz, v_xYZ, v_Xyz, v_XyZ, v_XYz, v_XYZ};
    return triquinticInterpolation(v, frac(c));
}
#endif // CG_RANDOM_INCLUDED
