#ifndef EDFCFG_H
#define EDFCFG_H

//-----------------------------------------------------------------------------
#ifdef __cplusplus
extern "C" {
#endif
//-----------------------------------------------------------------------------

#ifndef BLOCK_SIZE
	#ifdef _MSC_VER
		#define BLOCK_SIZE 512
	#else
		#define BLOCK_SIZE 256
	#endif
#endif

//#define LOG_ERR
//#define LOG_ERRF
//#define STREAM_BUF_SIZE 64

//-----------------------------------------------------------------------------
#ifdef __cplusplus
}
#endif
//-----------------------------------------------------------------------------

#include "edf.h"

#endif
