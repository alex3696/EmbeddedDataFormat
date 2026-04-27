#include "_pch.h"
#include "EdfHeader.h"

//-----------------------------------------------------------------------------
EdfHeader_t MakeHeaderDefault(void)
{
	EdfHeader_t h = { 1,0, 65001,BLOCK_SIZE, Default | UseCrc };
	return h;
}
//-----------------------------------------------------------------------------
int MakeHeaderFromBytes(const uint8_t* b, size_t srcSize, EdfHeader_t* h)
{
	if (HEADER_SIZE > srcSize)
		return ERR_SRC_SHORT;
	memcpy(h, b, sizeof(EdfHeader_t));
	return ERR_NO;
}
//-----------------------------------------------------------------------------
size_t HeaderToBytes(const EdfHeader_t* h, uint8_t* b)
{
	memset(b, 0, HEADER_SIZE);
	memcpy(b, h, sizeof(EdfHeader_t));
	return HEADER_SIZE;
}
