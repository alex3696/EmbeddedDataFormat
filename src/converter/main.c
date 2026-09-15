#include "_pch.h"
#include "converter.h"
// Measure-Command { Start-Process .\Cedf.exe -ArgumentList "qwe.bdf", "t" -NoNewWindow -Wait }
//-----------------------------------------------------------------------------
int main(int argc, char* argv[])
{
	if (2 < argc && NULL != argv[1])
	{
		char dst[4096];
		size_t len = strlen(argv[1]);
		if (sizeof(dst) < len)
			return -1;
		snprintf(dst, sizeof(dst), "%s", argv[1]);
		if ('t' == *argv[2])
		{
			if (IsExt(dst, "bdf") && 0 == ChangeExt(dst, "tdf"))
				return BinToText(argv[1], dst);
			if (IsExt(dst, "dat") && 0 == ChangeExt(dst, "tdf"))
				return DatToEdf(argv[1], dst, 't');
			if (IsExt(dst, "e") && 0 == ChangeExt(dst, "tdf"))
				return EchoToEdf(argv[1], dst, 't');
			if (IsExt(dst, "d") && 0 == ChangeExt(dst, "tdf"))
				return DynToEdf(argv[1], dst, 't');
		}
		else if ('b' == *argv[2])
		{
			if (IsExt(dst, "tdf") && 0 == ChangeExt(dst, "bdf"))
				return TextToBin(argv[1], dst);
			if (IsExt(dst, "dat") && 0 == ChangeExt(dst, "bdf"))
				return DatToEdf(argv[1], dst, 'b');
			if (IsExt(dst, "e") && 0 == ChangeExt(dst, "bdf"))
				return EchoToEdf(argv[1], dst, 'b');
			if (IsExt(dst, "d") && 0 == ChangeExt(dst, "bdf"))
				return DynToEdf(argv[1], dst, 'b');
		}
		else if (0 == _strnicmp("dat", argv[2], 3))
		{
			if (IsExt(dst, "bdf") && 0 == ChangeExt(dst, "dat"))
				return EdfToDat(argv[1], dst);
		}
		else if (0 == _strnicmp("e", argv[2], 3))
		{
			if (IsExt(dst, "bdf") && 0 == ChangeExt(dst, "e"))
				return EdfToEcho(argv[1], dst);
		}
		else if (0 == _strnicmp("d", argv[2], 3))
		{
			if (IsExt(dst, "bdf") && 0 == ChangeExt(dst, "d"))
				return EdfToDyn(argv[1], dst);
		}
	}
	return 0;
}

