#include "_pch.h"
#include "edf.h"

//-----------------------------------------------------------------------------
static int StreamWriteImpl(void* stream, size_t* writed, void const* data, size_t len)
{
	FILE* f = (FILE*)((FileStream_t*)stream)->Instance;
	size_t ret = fwrite(data, 1, len, f);
	if (ret != len)
	{
		int err = 0;
		if ((err = ferror(f)))
		{
			LOG_ERRF("Error writing %d", err);
			return err;
		}
	}
	if (writed)
		*writed += ret;
	fflush(f);
	return 0;
}
//-----------------------------------------------------------------------------
static int StreamReadImpl(void* stream, size_t* readed, void* dst, size_t len)
{
	FILE* f = (FILE*)((FileStream_t*)stream)->Instance;
	size_t ret = fread(dst, 1, len, f);
	if (ret != len)
	{
		if (feof(f))
			return ERR_EOF;
		//	printf("Error reading : unexpected end of file\n");
		int err = 0;
		if ((err = ferror(f)))
		{
			LOG_ERRF("Error reading %d", err);
			return err;
		}
	}
	if (readed)
		*readed += ret;
	return 0;
}
//-----------------------------------------------------------------------------
static int StreamWriteFormatImpl(void* stream, size_t* writed, const char* format, ...)
{
#ifdef STREAM_BUF_SIZE
	size_t ret = 0;
	va_list arglist;
	va_start(arglist, format);
	ret = vsnprintf((char*)stream->Buf, STREAM_BUF_SIZE, format, arglist);
	va_end(arglist);
	return StreamWriteImpl(stream, (void*)stream->Buf, ret - 1);
#else
	FILE* f = (FILE*)((FileStream_t*)stream)->Instance;
	va_list arglist;
	va_start(arglist, format);
	int ret = vfprintf(f, format, arglist);
	va_end(arglist);
	if (0 > ret)
	{
		int err = 0;
		if ((err = ferror(f)))
		{
			LOG_ERRF("Error reading %d", err);
			return err;
		}
	}
	if (writed)
		*writed += ret;
	fflush(f);
	return 0;
#endif
}
//-----------------------------------------------------------------------------
static int FileStreamClose(void* stream)
{
	FILE* f = (FILE*)((FileStream_t*)stream)->Instance;
	fflush(f);
	int ret = fclose(f);
	if (!ret)
		((FileStream_t*)stream)->Instance = NULL;
	return ret;
}
//-----------------------------------------------------------------------------
int FileStreamSeek(FileStream_t* stream, long offset, int origin)
{
	FILE* f = (FILE*)(stream->Instance);
	return fseek(f, offset, origin);
}
//-----------------------------------------------------------------------------

const StreamFnImpl_t rwFileSt = { T_FILE_STREAM, StreamWriteImpl ,StreamReadImpl ,StreamWriteFormatImpl,FileStreamClose, FileStreamSeek };
const StreamFnImpl_t wFileSt = { T_FILE_STREAM, StreamWriteImpl ,NULL ,StreamWriteFormatImpl,FileStreamClose, FileStreamSeek };
const StreamFnImpl_t rFileSt = { T_FILE_STREAM, NULL ,StreamReadImpl ,NULL,FileStreamClose, FileStreamSeek };

//-----------------------------------------------------------------------------
int FileStreamOpen(FileStream_t* s, const char* file, const char* inMode)
{
	const char a[] = "ab+";
	const char w[] = "wb";
	const char r[] = "rb";
	const char* mode = NULL;
	const StreamFnImpl_t* impl = NULL;

	int  err = ERR_WRONG_PARAMETERS;

	if (0 == strcmp("wb", inMode))
	{
		mode = w;
		impl = &wFileSt;
	}
	else if (0 == strcmp("ab", inMode))
	{
		mode = a;
		impl = &rwFileSt;
	}
	else if (0 == strcmp("rb", inMode))
	{
		mode = r;
		impl = &rFileSt;
	}

	if (mode)
	{
		FILE* f = NULL;
		err = fopen_s(&f, file, mode);
		if (!err)
		{
			*s = (FileStream_t)
			{
				.Impl = impl,
				.Instance = (void*)f
			};
			return 0;
		}
	}
	LOG_ERR();
	return err;
}

//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
