#ifndef POTYPE_H
#define POTYPE_H
#include "_pch.h"

static const char PoTypeStruct[] = "Struct";
static const char PoTypeInt8[] = "Int8";
static const char PoTypeUInt8[] = "UInt8";
static const char PoTypeChar[] = "Char";
static const char PoTypeString[] = "String";

static const char PoTypeUInt16[] = "UInt16";
static const char PoTypeInt16[] = "Int16";
static const char PoTypeHalf[] = "Half";

static const char PoTypeUInt32[] = "UInt32";
static const char PoTypeInt32[] = "Int32";
static const char PoTypeSingle[] = "Single";

static const char PoTypeUInt64[] = "UInt64";
static const char PoTypeInt64[] = "Int64";
static const char PoTypeDouble[] = "Double";

static const char SepOffset[] = "  ";
static const char SepBeginStruct[] = "{";
static const char SepEndStruct[] = "}";
static const char SepBeginArray[] = "[";
static const char SepEndArray[] = "]";
static const char SepVarEnd[] = ";";
static const char SepRecBegin[] = "<= ";
static const char SepRecEnd[] = ">\n";

typedef enum PoType
{
	Struct = 0,
	Char,// char array strings 
	// integres
	Int8,
	UInt8,
	Int16,
	UInt16,
	Int32,
	UInt32,
	Int64,
	UInt64,
	// float
	Half,
	Single,
	Double,
	// strings
	String,
	//VarInt,
	//VarUInt,
	//VarFloat,
} PoType;

uint8_t GetSizeOf(enum PoType p);
uint8_t IsPrimitive(PoType p);
uint8_t IsPoType(PoType p);

typedef enum EdfBlockType
{
	btConfig = 126, //0xB0, ~
	btSchema = 63, //0x3f, ?
	btData = 61, //0x3d, =
} EdfBlockType;

uint8_t IsBlockType(EdfBlockType t);


//-----------------------------------------------------------------------------
typedef struct
{
	uint8_t* Pos;
	const uint8_t* const BufBegin;
	const uint8_t* const BufEnd;
} IoBuffer_t;

static inline void IoBufferInit(IoBuffer_t* buf, const uint8_t* const data, size_t len, size_t pos)
{
	memcpy((void*)&buf->BufBegin, &data, sizeof(uint8_t*)); //((uint8_t*)buf->BufBegin) = (uint8_t*)data;
	uint8_t* d = (uint8_t*)data + len;
	memcpy((void*)&buf->BufEnd, &d, sizeof(uint8_t*)); //(uint8_t*)buf->BufEnd = (uint8_t*)(data + len);
	buf->Pos = (uint8_t*)data + pos;
}

static inline void IoBufferAdvance(IoBuffer_t* buf, size_t len) { buf->Pos += len; }
static inline void IoBufferReset(IoBuffer_t* buf) { buf->Pos = (uint8_t*)buf->BufBegin; }
static inline size_t IoBufferGetAdvanced(const IoBuffer_t* buf) { return buf->Pos - buf->BufBegin; }
static inline size_t IoBufferGetRemain(const IoBuffer_t* buf) { return buf->BufEnd - buf->Pos; }
static inline size_t IoBufferGetSize(const IoBuffer_t* buf) { return buf->BufEnd - buf->BufBegin; }

//-----------------------------------------------------------------------------
typedef struct
{
	IoBuffer_t Src;
	IoBuffer_t Dst;
} WalkContext_t;
//-----------------------------------------------------------------------------
#endif
