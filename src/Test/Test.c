#include "converter.h"
#include "edf_cfg.h"
#include "assert.h"


//-----------------------------------------------------------------------------
static char* GetTestFilePath(char* filename)
{
	return filename;
}
//-----------------------------------------------------------------------------
static size_t GetCString(const char* str, uint32_t arr_len, uint8_t* dst, size_t dst_len)
{
	if (NULL == str)
		return 0;
	size_t len = strnlength(str, 0xFE) + 1;
	if (0 == len || len > dst_len)
		return 0;
	memcpy(dst, str, len);
	memset(dst + len, 0, arr_len - len);
	return arr_len;
}
//-----------------------------------------------------------------------------
static int CompareFiles(const char* src, const char* dst)
{
	int ret = 0;
	errno_t err = 0;
	FILE* f1 = NULL;
	err = fopen_s(&f1, src, "rb");
	if (err)
		return err;
	FILE* f2 = NULL;
	err = fopen_s(&f2, dst, "rb");
	if (err)
		return err;
	uint8_t buf1[1024];
	uint8_t buf2[1024];
	size_t readed1 = 0;
	size_t readed2 = 0;
	do
	{
		readed1 = fread(buf1, 1, sizeof(buf1), f1);
		readed2 = fread(buf2, 1, sizeof(buf2), f2);
		if (readed1 != readed2 || memcmp(buf1, buf2, readed1))
		{
			ret = 1;
			break;
		}

	} while (!feof(f1) && !feof(f2));


	fclose(f1);
	fclose(f2);
	return ret;
}
//-----------------------------------------------------------------------------
static void TestMemStream(void)
{
	size_t writed = 0;
	MemStream_t ms = { 0 };
	uint8_t buf[256];
	assert(!MemStreamOpen(&ms, buf, sizeof(buf), 0, "rw"));
	const char test[] = "test 123";
	Stream_t* stream = (Stream_t*)&ms;
	assert(!StreamWrite(stream, &writed, test, sizeof(test) - 1));
	assert(!StreamWriteFmt(stream, &writed, " format %d", 1));

	size_t readed = 0;
	char outBuf[256] = { 0 };
	assert(!StreamRead(stream, &readed, outBuf, writed));

	assert(writed == readed);
	assert(0 == memcmp("test 123 format 1", outBuf, readed));
}
//-----------------------------------------------------------------------------
static int PackUnpack()
{
	size_t writed = 0;
	int err = 0;
#pragma pack(push,1)
	typedef struct TestStruct
	{
		char* Key;
		char* Value;
		uint8_t Arr[3];
	} TestStruct_t;
	EdfInf_t TestStructInf =
	{
		.Inf =
		{
			.Type = Struct,
			.Name = "KeyValue",
			.Dims = {1, (uint32_t[]) { 2 } } ,
			.Childs =
			{
				.Count = (uint8_t)3,
				.Item = (EdfType_t[])
				{
					{ String, "Key" },
					{ String, "Value" },
					{
						.Type = Struct, .Name = "Internal",
						.Childs =
						{
							.Count = 1,
							.Item = (EdfType_t[])
							{
								{ UInt8, "Test", .Dims = {1, (uint32_t[]) { 3 } } },
							}
						}
					}
				}
			}
		}
	};

#pragma pack(pop)
	size_t primReaded = 0;
	size_t skip = 0;
	EdfWriter_t w = { 0 };
	EdfWriter_t* dw = &w;

	uint8_t binBuf[1024] = { 0 };
	MemStream_t memStream = { 0 };
	if ((err = MemStreamOutOpen(&memStream, binBuf, sizeof(binBuf))))
		return err;
	err = EdfOpenStream(dw, (Stream_t*)&memStream, "wb");
	err = EdfWriteInf(dw, &TestStructInf, &writed);
	dw->Stream.Inst.Mem.WPos = 0;

	TestStruct_t val1 = { "Key1", "Value1", { 11,12,13 } };
	TestStruct_t val2 = { "Key2", "Value2", { 21,22,23 } };
	EdfWriteData(dw, &val1, sizeof(TestStruct_t));
	EdfWriteData(dw, &val2, sizeof(TestStruct_t));
	EdfClose(dw);

	MemStream_t mssrc = { 0 };
	if ((err = MemStreamInOpen(&mssrc, &binBuf[4], 100)))
		return err;
	uint8_t buf[1024] = { 0 };
	MemStream_t mem = { 0 };
	if ((err = MemStreamOutOpen(&mem, buf, sizeof(buf))))
		return err;

	TestStruct_t* kv = NULL;
	if ((err = EdfReadBin(&TestStructInf.Inf, &mssrc, &mem, &kv, &skip, &primReaded)))
		return err;

	if (!kv || 10 != primReaded)
		return 1;

	if (0 != strcmp(val1.Key, kv->Key)
		|| 0 != strcmp(val1.Value, kv->Value)
		|| 0 != memcmp(&val1.Arr, &kv->Arr, FIELD_SIZEOF(TestStruct_t, Arr)))
		return 1;

	kv++;
	if (0 != strcmp(val2.Key, kv->Key)
		|| 0 != strcmp(val2.Value, kv->Value)
		|| 0 != memcmp(&val2.Arr, &kv->Arr, FIELD_SIZEOF(TestStruct_t, Arr)))
		return 1;

	return 0;
}



//-----------------------------------------------------------------------------
static int WriteSample(EdfWriter_t* dw)
{
	size_t writed = 0;
	int err = 0;

	EdfConfig_t h = MakeHeaderDefault();
	err = EdfWriteConfig(dw, &h, &writed);

#pragma pack(push,1)
	typedef struct KeyValue
	{
		char* Key;
		char* Value;
	} KeyValue_t;
	EdfInf_t keyValueType =
	{
		0, .Name = "VariableKV", .Desc = "comment",
		.Inf =
		{
			.Type = Struct, .Name = "KeyValue", .Dims = {0, NULL},
			.Childs =
			{
				.Count = 2,
				.Item = (EdfType_t[])
				{
					{ String, "Key" },
					{ String, "Value" },
				}
			}
		}
	};
#pragma pack(pop)

	err = EdfWriteInf(dw, &keyValueType, &writed);
	EdfWriteData(dw, &((KeyValue_t) { "Key1", "Value1" }), sizeof(KeyValue_t));
	EdfWriteData(dw, &((KeyValue_t) { "Key2", "Value2" }), sizeof(KeyValue_t));
	EdfWriteData(dw, &((KeyValue_t) { "Key3", "Value3" }), sizeof(KeyValue_t));

	// пример записи строки
	const char* strVal = "Value 1";
	EdfWritePrimitiveInfData(dw, String, 0, "тестовый ключ 1", NULL, &strVal);
	EdfWritePrimitiveInfData(dw, String, 0, "тестовый ключ 2", NULL, &(const char*){"Value 2"});
	EdfWritePrimitiveInfData(dw, String, 0, "тестовый ключ 3", NULL, EDF_CONSTSTR("Value 3"));

	// тест нулевой строки
	EdfWritePrimitiveInfData(dw, String, 0, "test NULL string", NULL, EDF_CONSTSTR(""));
	// тест строки длиннее 255 - должна быть обрезана на 255 символов
	const char chBegin = '0'; const char chEnd = '9';
	char ch = chBegin;
	char tctArr260[260];
	for (size_t i = 0; i < 260; i++)
	{
		tctArr260[i] = ch;
		ch++;
		if(chEnd <ch)
			ch = chBegin;
	}
	EdfWritePrimitiveInfData(dw, String, 0, "test 260 string", NULL, EDF_CONSTSTR(tctArr260));

	EdfInf_t t = { { Int32 }, 0, "weight variable" };
	err = EdfWriteInf(dw, &t, &writed);
	uint8_t test[100] = { 0 };
	(*(int32_t*)test) = (int32_t)(0xFFFFFFFF);
	EdfWriteData(dw, test, 4);
	EdfFlushData(dw, &writed);

	EdfInf_t td = { { Double }, 0, "TestDouble" };
	err = EdfWriteInf(dw, &td, &writed);
	double dd = 1.1;
	EdfWriteData(dw, &dd, sizeof(double));
	dd = 2.1;
	EdfWriteData(dw, &dd, sizeof(double));
	dd = 3.1;
	EdfWriteData(dw, &dd, sizeof(double));

	EdfInf_t tchar = { {.Type = Char, .Dims = { 1, (uint32_t[]) { 20 } } }, 0, "Char Text" };
	err = EdfWriteInf(dw, &tchar, &writed);
	size_t len = 0;
	len += GetCString("Char", 20, test + len, sizeof(test));
	len += GetCString("Value", 20, test + len, sizeof(test) - len);
	len += GetCString("Array     Value", 20, test + len, sizeof(test) - len);
	EdfWriteData(dw, test, len);

	EdfType_t comlexVarInf =
	{
		.Type = Struct, .Name = "ComplexVariable", .Dims = {0, NULL},
		.Childs =
		{
			.Count = 2,
			.Item = (EdfType_t[])
			{
				(EdfType_t)
				{
					Int64, "time"
				},
				(EdfType_t)
				{
					Struct, "State", { 1, (uint32_t[]) { 3 }} ,
					.Childs =
					{
						.Count = 3,
						.Item = (EdfType_t[])
						{
							(EdfType_t)
							{
								Int8, "text"
							},
							(EdfType_t)
							{
								Struct, "Pos",{0, NULL} ,
								.Childs =
								{
									.Count = 2,
									.Item = (EdfType_t[])
									{
										{ Int32, "x" },
										{ Int32, "y" },
									}
								}
							},
							(EdfType_t)
							{
								Double, "Temp",{ 2, (uint32_t[]) { 2,2 }},
							},
						}
					}
				},
			}
		}
	};
	err = EdfWriteInf(dw, &(EdfInf_t){comlexVarInf}, & writed);
#pragma pack(push,1)
	struct ComplexVariable
	{
		int64_t time;
		struct State
		{
			int8_t text;
			struct
			{
				int32_t x;
				int32_t y;
			} Pos;
			double Temp[2][2];
		} State[3];
	};
#pragma pack(pop)
	struct ComplexVariable cv =
	{
		.time = -123,
		.State =
		{
			{ 1, { 11, 12 }, {1.1,1.2,1.3,1.4 } },
			{ 2, { 21, 22 }, {2.1,2.2,2.3,2.4 } },
			{ 3, { 31, 32 }, {3.1,3.2,3.3,3.4 } },
		}
	};
	EdfWriteData(dw, &cv, sizeof(struct ComplexVariable));
	return err;
}
//-----------------------------------------------------------------------------
static int Test_WriteSample()
{
	char* binFile = GetTestFilePath("t_write.bdf");
	char* txtFile = GetTestFilePath("t_write.tdf");
	char* txtConvFile = GetTestFilePath("t_writeConv.tdf");
	EdfWriter_t w;
	int err = 0;
	// TEXT write
	err = EdfOpen(&w, txtFile, "wt");
	WriteSample(&w);
	EdfClose(&w);
	// test append
	memset(&w, 0, sizeof(EdfWriter_t));
	err = EdfOpen(&w, txtFile, "at");
	if (0 != err)
		return err;
	EdfWritePrimitiveInfData(&w, Int32, 0, "Int32 Key", NULL, &((int32_t) { 0xb1b2b3b4 }));
	EdfClose(&w);

	// BINary write
	err = EdfOpen(&w, binFile, "wb");
	WriteSample(&w);
	EdfClose(&w);
	// test append
	memset(&w, 0, sizeof(EdfWriter_t));
	if ((err = EdfOpen(&w, binFile, "ab")))
		return err;
	EdfWritePrimitiveInfData(&w, Int32, 0, "Int32 Key", NULL, &((int32_t) { 0xb1b2b3b4 }));
	EdfClose(&w);

	BinToText(binFile, txtConvFile);
	err = CompareFiles(txtFile, txtConvFile);
	if (err)
		LOG_ERRF("err %d: t_write files not equal", err);
	assert(0 == err);
	return err;
}
//-----------------------------------------------------------------------------
static void WriteBigVar(EdfWriter_t* dw)
{
	int err = 0;
	size_t writed = 0;
	EdfConfig_t h = MakeHeaderDefault();
	err = EdfWriteConfig(dw, &h, &writed);

	size_t arrLen = (size_t)(BLOCK_SIZE / sizeof(uint32_t) * 2.5);
	EdfInf_t t = { {.Type = Int32, .Name = "variable", .Dims = { 1, (uint32_t[]) { arrLen }} }, 0xF0F1F2F3 };
	err = EdfWriteInf(dw, &t, &writed);

	uint32_t test[1000] = { 0 };
	for (uint32_t i = 0; i < arrLen; i++)
		test[i] = i;
	assert(ERR_NO == EdfWriteData(dw, test, sizeof(uint32_t) * arrLen));

	uint8_t* test2 = (uint8_t*)test;
	assert(ERR_SRC_SHORT == EdfWriteData(dw, test2, 15));
	assert(ERR_SRC_SHORT == EdfWriteData(dw, test2 + 15, 149));
	assert(ERR_NO == EdfWriteData(dw, test2 + 15 + 149, (sizeof(uint32_t) * arrLen) - 15 - 149));

	EdfFlushData(dw, &writed);
}
static void Test_WriteBigVar()
{
	char* binFile = GetTestFilePath("t_big.bdf");
	char* txtFile = GetTestFilePath("t_big.tdf");
	char* txtConvFile = GetTestFilePath("t_bigConv.tdf");
	int err = 0;
	EdfWriter_t bw;
	err = EdfOpen(&bw, binFile, "wb");
	WriteBigVar(&bw);
	EdfClose(&bw);

	EdfWriter_t tw;
	err = EdfOpen(&tw, txtFile, "wt");
	WriteBigVar(&tw);
	EdfClose(&tw);

	BinToText(binFile, txtConvFile);
	err = CompareFiles(txtFile, txtConvFile);

	if (err)
		LOG_ERRF("err: t_big %d", err);
	assert(0 == err);
}
//-----------------------------------------------------------------------------
static void DatFormatTest()
{
	assert(0 == DatToEdf("1DAT.dat", "1DAT.tdf", 't'));
	assert(0 == DatToEdf("1DAT.dat", "1DAT.bdf", 'b'));
	assert(0 == BinToText("1DAT.bdf", "1DATConv.tdf"));
	assert(0 == CompareFiles("1DAT.tdf", "1DATConv.tdf"));
	assert(0 == EdfToDat("1DAT.bdf", "1DATConv.dat"));
	assert(0 == CompareFiles("1DAT.dat", "1DATConv.dat"));

	assert(0 == EchoToEdf("1E.E", "1E.tdf", 't'));
	assert(0 == EchoToEdf("1E.E", "1E.bdf", 'b'));
	assert(0 == BinToText("1E.bdf", "1EConv.tdf"));
	assert(0 == CompareFiles("1E.tdf", "1EConv.tdf"));
	assert(0 == EdfToEcho("1E.bdf", "1EConv.E"));
	assert(0 == CompareFiles("1E.E", "1EConv.E"));

	assert(0 == DynToEdf("1D.D", "1D.tdf", 't'));
	assert(0 == DynToEdf("1D.D", "1D.bdf", 'b'));
	assert(0 == BinToText("1D.bdf", "1DConv.tdf"));
	assert(0 == CompareFiles("1D.tdf", "1DConv.tdf"));
	assert(0 == EdfToDyn("1D.bdf", "1DConv.D"));
	assert(0 == CompareFiles("1D.D", "1DConv.D"));
}
//-----------------------------------------------------------------------------
static void MbCrc16accTest()
{
	const char* test =
		"some test data text 1"
		"some test data text 2"
		"some test data text 3"
		"some test data text 4";
	size_t len = strnlength(test, 256);
	uint16_t crc = MbCrc16(test, len);

	uint16_t crcAcc = 0xFFFF;
	crcAcc = MbCrc16acc(test, 17, crcAcc);
	crcAcc = MbCrc16acc(test + 17, len - 17, crcAcc);
	assert(crcAcc == crc);
}
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
int main()
{
	LOG_ERR();
	Test_WriteSample();
	assert(0 == PackUnpack());
	MbCrc16accTest();
	Test_WriteBigVar();
	DatFormatTest();
	TestMemStream();
	return 0;
}
