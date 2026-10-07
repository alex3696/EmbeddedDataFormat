#ifndef EDFCONFIG_H
#define EDFCONFIG_H

#include "_pch.h"

typedef enum Options
{
	Default = 0,
	DisableStructBlockTransfer = 1,
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

#define CreateConfig(blkLen) ((EdfConfig_t){ EDF_VERSMAJOR,EDF_VERSMINOR, EDF_ENCODING, blkLen, 0, Default })
extern const EdfConfig_t EdfCfg256;

#endif
