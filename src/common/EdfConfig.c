#include "_pch.h"
#include "EdfConfig.h"

//-----------------------------------------------------------------------------
EdfConfig_t MakeDefaultConfig(void)
{
	EdfConfig_t h = { EDF_VERSMAJOR,EDF_VERSMINOR, EDF_ENCODING, BLOCK_SIZE, Default };
	return h;
}
//-----------------------------------------------------------------------------
int MakeConfigFromBytes(const uint8_t* b, size_t srcSize, EdfConfig_t* h)
{
	if (sizeof(EdfConfig_t) > srcSize)
		return ERR_SRC_SHORT;
	memcpy(h, b, sizeof(EdfConfig_t));
	return ERR_NO;
}
