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

#define PRINTF_DECIMAL_BUFFER_SIZE 64
#define PRINTF_MAX_INTEGRAL_DIGITS_FOR_DECIMAL 18
#define PRINTF_USE_DOUBLE_INTERNALLY 1
#define PRINTF_ALIAS_STANDARD_FUNCTION_NAMES_HARD 1
#include "printf.h"
#endif //PCH_H
