#ifndef EDFCONFIG_H
#define EDFCONFIG_H

#include "_pch.h"

typedef enum Options
{
	Default = 0,
} Options_t;

typedef struct
{
	uint8_t VersMajor;
	uint8_t VersMinor;
	uint16_t Encoding;
	uint16_t Blocksize;
	uint16_t Reserved;
	uint32_t Flags; //Options_t
} EdfConfig_t;

EdfConfig_t MakeDefaultConfig(void);
int MakeConfigFromBytes(const uint8_t* b, size_t srcSize, EdfConfig_t* h);

#endif
