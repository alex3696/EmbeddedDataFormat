#ifndef PRIMITIVES_H
#define PRIMITIVES_H

#include "_pch.h"
#include "PoType.h"

int BinToBin(PoType t, WalkContext_t* ctx);
int CBinToBin(PoType t, WalkContext_t* ctx);
int CBinToStr(PoType t, WalkContext_t* ctx);
int BinToStr(PoType t, WalkContext_t* ctx);

// BinToCBin // required dynamic mem allocator

int StreamWriteString(Stream_t* s, const char* str, size_t* writed);
int StreamReadString(MemStream_t* tsrc, LineAlloc_t* tmem, char** ti);

#endif
