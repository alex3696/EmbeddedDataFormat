#ifndef ITOA_H
#define ITOA_H

#include <stdint.h>
#include <stddef.h>

size_t Int64ToA(int64_t val, char* dst, size_t dstLen);
size_t UInt64ToA(uint64_t val, char* dst, size_t dstLen);

size_t Int32ToA(int32_t val, char* dst, size_t dstLen);
size_t UInt32ToA(uint32_t val, char* dst, size_t dstLen);

#endif
