#include "_pch.h"
#include "EdfHeader.h"

//-----------------------------------------------------------------------------
EdfConfig_t MakeHeaderDefault(void)
{
	EdfConfig_t h = { 1,0, 65001,BLOCK_SIZE, Default };
	return h;
}
//-----------------------------------------------------------------------------
int MakeHeaderFromBytes(const uint8_t* b, size_t srcSize, EdfConfig_t* h)
{
	if (EDF_HEADER_SIZE > srcSize)
		return ERR_SRC_SHORT;
	memcpy(h, b, sizeof(EdfConfig_t));
	return ERR_NO;
}
//-----------------------------------------------------------------------------
size_t HeaderToBytes(const EdfConfig_t* h, uint8_t* b)
{
	memset(b, 0, EDF_HEADER_SIZE);
	memcpy(b, h, sizeof(EdfConfig_t));
	return EDF_HEADER_SIZE;
}
