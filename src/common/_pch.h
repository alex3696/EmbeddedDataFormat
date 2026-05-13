#ifndef PCH_H
#define PCH_H

#ifdef _MSC_VER
#pragma warning(disable : 5045)
#endif

#ifdef __cplusplus
#include <cstdint>
#include <cstdio>
#include <cstring>
#else
#include "stdint.h"
#include "stdio.h"
#include "string.h"
#endif

#include "stddef.h"
#include "memory.h"
#include "stdarg.h"

#endif //PCH_H
