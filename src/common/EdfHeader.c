#include "_pch.h"
#include "EdfHeader.h"

//-----------------------------------------------------------------------------
EdfConfig_t MakeDefaultConfig(void)
{
	EdfConfig_t h = { 1,0, 65001,BLOCK_SIZE, Default };
	return h;
}
//-----------------------------------------------------------------------------
int MakeConfigFromBytes(const uint8_t* b, size_t srcSize, EdfConfig_t* h)
{
	if (EDF_CONFIG_SIZE > srcSize)
		return ERR_SRC_SHORT;
	memcpy(h, b, sizeof(EdfConfig_t));
	return ERR_NO;
}
//-----------------------------------------------------------------------------
size_t ConfigToBytes(const EdfConfig_t* h, uint8_t* b)
{
	memset(b, 0, EDF_CONFIG_SIZE);
	memcpy(b, h, sizeof(EdfConfig_t));
	return EDF_CONFIG_SIZE;
}
