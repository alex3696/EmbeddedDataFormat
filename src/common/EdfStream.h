#ifndef EDFSTREAM_H
#define EDFSTREAM_H

#include "_pch.h"
//-----------------------------------------------------------------------------
// Common stream
#define FSEEK_CUR    1
#define FSEEK_END    2
#define FSEEK_SET    0

typedef int(*WriteFn)	(void* stream, size_t* writed, void const* data, size_t len);
typedef int(*ReadFn)	(void* stream, size_t* readed, void* dst, size_t);
typedef int(*WriteFmtFn)(void* stream, size_t* writed, const char* format, ...);
typedef int(*CloseFn)	(void* stream);
typedef int(*SeekFn)	(void* stream, long offset, int origin);

typedef enum StreamType
{
	T_FILE_STREAM = 9,
	T_MEM_STREAM = 10
} StreamType_t;

typedef struct StreamFnImpl
{
	StreamType_t TypeId;
	WriteFn Write;
	ReadFn Read;
	WriteFmtFn WriteFmt;
	CloseFn Close;
	SeekFn Seek;
} StreamFnImpl_t;

typedef struct Stream
{
	const StreamFnImpl_t* Impl;
	union StreamInstance
	{
		struct FileInstance
		{
			void* Instance;
		} File;
		struct MemInstance
		{
			uint8_t* Buffer;
			size_t Size;
			size_t RPos;
			size_t WPos;
		} Mem;
	} Inst;
} Stream_t;

#define StreamWrite(s, w, data, count) (((s)->Impl->Write)((s), (w), data, count))
#define StreamRead(s, r, data, count) (((s)->Impl->Read)((s), (r), data, count))
#define StreamWriteFmt(s, w, fmt,...) (((s)->Impl->WriteFmt)((s), (w), fmt, __VA_ARGS__))
#define StreamClose(s) ((s)->Impl->Close)((s))
#define StreamSeek(s, offset, origin) ((s)->Impl->Seek)((s),offset, origin)

//-----------------------------------------------------------------------------
// FileStream

typedef struct FileStream
{
	const StreamFnImpl_t* Impl;
	void* Instance;
#ifdef STREAM_BUF_SIZE
	uint8_t Buf[STREAM_BUF_SIZE];
#endif // 
} FileStream_t;

int FileStreamOpen(FileStream_t* w, const char* file, const char* mode);

typedef int(*FileStreamOpenFn)(FileStream_t* w, const char* file, const char* mode);
//-----------------------------------------------------------------------------
//Memory Stream

typedef struct MemStream
{
	const StreamFnImpl_t* Impl;
	uint8_t* Buffer;
	size_t Size;
	size_t RPos;
	size_t WPos;
} MemStream_t;

int MemStreamOpen(MemStream_t* s, uint8_t* buf, size_t size, size_t datalen, const char* mode);
int MemStreamInOpen(MemStream_t* s, uint8_t* buf, size_t size);
int MemStreamOutOpen(MemStream_t* s, uint8_t* buf, size_t size);

int MemAlloc(MemStream_t* s, size_t len, void** pptr);
size_t StreamLen(const MemStream_t* s);
size_t StreamEmptyLen(const MemStream_t* s);
int StreamCpy(MemStream_t* src, MemStream_t* dst, size_t len);
//-----------------------------------------------------------------------------
#endif //EDFSTREAM_H