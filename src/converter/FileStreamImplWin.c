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
			return err;
		}
	}
	if (writed)
		*writed += ret;
	//fflush(f);
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
			return err;
		}
	}
	if (readed)
		*readed += ret;
	return 0;
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
int FileStreamOpen(FileStream_t* s, const char* file, const char* inMode)
{
	const char a[] = "ab+";
	const char w[] = "wb";
	const char r[] = "rb";
	const char* mode = NULL;
	StreamFnImpl_t impl = {0};

	int  err = ERR_WRONG_PARAMETERS;

	if (0 == strcmp("wb", inMode))
	{
		mode = w;
		impl = (StreamFnImpl_t){ T_FILE_STREAM, StreamWriteImpl, NULL,           FileStreamClose, FileStreamSeek };
	}
	else if (0 == strcmp("ab", inMode))
	{
		mode = a;
		impl = (StreamFnImpl_t){ T_FILE_STREAM, StreamWriteImpl, StreamReadImpl, FileStreamClose, FileStreamSeek };
	}
	else if (0 == strcmp("rb", inMode))
	{
		mode = r;
		impl = (StreamFnImpl_t){ T_FILE_STREAM, NULL,            StreamReadImpl, FileStreamClose, FileStreamSeek };
	}

	if (mode)
	{
		FILE* f = NULL;
		err = fopen_s(&f, file, mode);
		if (!err)
		{
			memcpy((FileStream_t*)&s->Impl, &impl, sizeof(StreamFnImpl_t));
			s->Instance = (void*)f;
			return 0;
		}
	}
	return err;
}

//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
