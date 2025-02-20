#ifndef LIB_CONSTANTS
#define LIB_CONSTANTS

#define PI 3.1415926536
#define PI_2 6.2831853072
#define FLOAT_MAX 3.402823E+38
//#define FLOAT_MAX 3.402823466e+38F
#define UINT_MAX 4294967295u
#define UINT_MAX_HALF 2147483647.5

#define SHADER_ID_RAYGEN "raygeneration"
#define SHADER_ID_CLOSESTHIT "closesthit"
#define SHADER_ID_ANYHIT "anyhit"
#define SHADER_ID_MISS "miss"

// 4% seems to be a commonly used value for
// dielectric reflectance (at surface normal, aka 0 degrees)
// in rendering (eg unreal engine 4)
#define DIELECTRIC_REFLECTANCE_F0 0.04
#define DIELECTRIC_REFLECTANCE_F0_RCP 25

#define SUN_SIZE 0.0000109394 // 1 - cosine of the angular radius of the sun on earth (without atmospheric effects)
//#define SUN_SIZE 0.00005
#define SUN_SIZE_VISUAL 0.001 // vanilla sun size

#define EMISSIVE_MULTI 20
#define SUNLIGHT_MULTI (25 / SUN_SIZE) // solid angle compensation. should be 25 for voxels only
#define SKYBOX_MULTI 5
#define BLOCKLIGHT_MULTI 20

#define LIGHT_SAMPLE_SUN_ID (UINT_MAX - 1u)
#define LIGHT_SAMPLE_INVALID_ID UINT_MAX

#endif
