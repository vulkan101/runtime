#ifndef ENUM_CODEC_
#define ENUM_CODEC_

#ifdef __cplusplus
extern "C" {
#endif

#include <glib.h>
#include <stdint.h>

#define MAX_SIG_PARAMS 6

	typedef enum  {
		SIG_PARAM_VOID = 0,
		SIG_PARAM_I4 = 4,
		SIG_PARAM_I8 = 8,
		SIG_PARAM_INVALID = 666
	} param_type_e;

	typedef struct
	{
		int returnType;			// bytes used by return type (0 for void)
		int paramCount;			// total parameter count (excluding return value)
		int params[MAX_SIG_PARAMS];	// number of bytes per param
	} InterpInternalSignature;

	extern void check_signature(InterpInternalSignature* s_in);
	extern uint16_t encode_signature(const int* params, int param_count, int return_type);
	extern void decode_signature(uint16_t encoded, int* params_out, int* param_count_out, int* return_type_out);	
	extern gboolean check_signatures(const InterpInternalSignature* a, const InterpInternalSignature* b);
	extern param_type_e get_return_type(uint16_t encoded);
	extern int get_num_params(uint16_t encoded);	
#ifdef __cplusplus
}
#endif

#endif // !_ENUM_CODEC__
