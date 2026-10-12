#ifndef PRIMITIVES_H
#define PRIMITIVES_H

#include "_pch.h"
#include "PoType.h"
#include "EdfSchema.h"
typedef int (*WritePrimitiveFn)(const EdfType_t* t, WalkContext_t* ctx);

int BinToBin(const EdfType_t* t, WalkContext_t* ctx);
int CBinToBin(const EdfType_t* t, WalkContext_t* ctx);
int CBinToStr(const EdfType_t* t, WalkContext_t* ctx);
int BinToStr(const EdfType_t* t, WalkContext_t* ctx);

// BinToCBin // required dynamic mem allocator

int StreamWriteString(Stream_t* s, const char* str, size_t* writed);
int StreamReadString(MemStream_t* tsrc, LineAlloc_t* tmem, char** ti);

#endif
