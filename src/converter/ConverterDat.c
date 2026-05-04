#include "_pch.h"
#include "assert.h"
#include "Charts.h"
#include "converter.h"
#include "edf_cfg.h"
#include "KeyValue.h"
#include "math.h"
#include "SiamFileFormat.h"
#include "stdlib.h"
//-----------------------------------------------------------------------------
/// SPSK
//-----------------------------------------------------------------------------
int DatToEdf(const char* src, const char* edf, char mode)
{
	FILE* f = NULL;
	int err = fopen_s(&f, src, "rb");
	if (err)
		return err;

	SPSK_FILE_V1_1 dat;
	if (1 != fread(&dat, sizeof(SPSK_FILE_V1_1), 1, f))
		return ERR_FREAD;

	char* edfMode = NULL;
	if ('t' == mode)
		edfMode = "wt";
	else if ('b' == mode)
		edfMode = "wb";
	else
		return ERR_WRONG_PARAMETERS;

	EdfWriter_t dw;
	size_t writed = 0;
	if ((err = EdfOpen(&dw, edf, edfMode)))
		return err;

	EdfConfig_t h = MakeHeaderDefault();
	if ((err = EdfWriteConfig(&dw, &h, &writed)))
		return err;

	//EdfWritePrimitiveInfData(&dw, String,0, "Comment", NULL, "ResearchTypeId={ECHOGRAM-5, DYNAMOGRAM-6, SAMT-11}");
	const EdfInf_t typeInf = { FILETYPEID, NULL, NULL, FileTypeIdType };
	EdfWriteInfData(&dw, &typeInf, &(FileTypeId_t){ (uint16_t)dat.FileType, 1}, sizeof(FileTypeId_t));

	const EdfInf_t beginDtInf = { BEGINDATETIME, "BeginDateTime", NULL, DateTimeType };
	const DateTime_t beginDtDat = { dat.Year + 2000, dat.Month, dat.Day, };
	EdfWriteInfData(&dw, &beginDtInf, &beginDtDat, sizeof(DateTime_t));

	char field[256] = { 0 };
	char cluster[256] = { 0 };
	char well[256] = { 0 };
	char shop[256] = { 0 };
	snprintf(field, sizeof(field) - 1, "%d", dat.Id.Field);
	memcpy(cluster, dat.Id.Cluster, strnlength(dat.Id.Cluster, FIELD_SIZEOF(FILES_RESEARCH_ID_V1_0, Cluster)));
	memcpy(well, dat.Id.Well, strnlength(dat.Id.Well, FIELD_SIZEOF(FILES_RESEARCH_ID_V1_0, Well)));
	snprintf(shop, sizeof(shop) - 1, "%d", dat.Id.Shop);
	const EdfInf_t posInf = { POSITION, "Position", NULL, PositionType };
	const Position_t posDat = { .Field = field, .Cluster = cluster, .Well = well, .Shop = shop, };
	EdfWriteInfData(&dw, &posInf, &posDat, sizeof(Position_t));

	EdfWritePrimitiveInfData(&dw, UInt16, 0, "PlaceId", "место установки", &dat.Id.PlaceId);
	EdfWritePrimitiveInfData(&dw, Int32, 0, "Depth", "глубина установки", &dat.Id.Depth);

	const EdfInf_t devInf = { DEVICEINFO, "DevInfo", "скважный прибор", DeviceInfoType };
	const DeviceInfo_t devDat =
	{
		.SwId = dat.SensType, .SwModel = dat.SensVer, .SwRevision = 0,
		.HwId = 0, .HwModel = 0, .HwNumber = dat.SensNum
	};
	EdfWriteInfData(&dw, &devInf, &devDat, sizeof(DeviceInfo_t));

	const EdfInf_t regInf = { REGINFO, "RegInfo", "наземный регистратор", DeviceInfoType };
	const DeviceInfo_t regDat =
	{
		.SwId = dat.RegType, .SwModel = dat.RegVer, .SwRevision = 0,
		.HwId = 0, .HwModel = 0, .HwNumber = dat.RegNum
	};
	EdfWriteInfData(&dw, &regInf, &regDat, sizeof(DeviceInfo_t));

	const EdfInf_t chartsInf = { 0, "ChartInfo", NULL, ChartNInf };
	const ChartN_t chartsDat[] =
	{
		{ "Time", "мс", "", "время измерения от начала дня" },
		{ "Press", "0.001 атм","", "давление" },
		{ "Temp", "0.001 °С","", "температура" },
		{ "Vbat", "0.001 V","", "напряжение батареи" },
	};
	EdfWriteInfData(&dw, &chartsInf, &chartsDat, sizeof(chartsDat));

	if ((err = EdfWriteInf(&dw, &(EdfInf_t){OMEGADATA, NULL, NULL, OmegaDataType}, & writed)))
		return err;

	OMEGA_DATA_V1_1 record;
	do
	{
		if (1 == fread(&record, sizeof(OMEGA_DATA_V1_1), 1, f))
		{
			if ((err = EdfWriteData(&dw, &record, sizeof(OMEGA_DATA_V1_1) - 2)))
				return err;
			//EdfFlushData(&dw, &writed);
		}
	} while (!feof(f));

	fclose(f);
	EdfClose(&dw);
	return 0;
}
//-----------------------------------------------------------------------------
int EdfToDat(const char* edfFile, const char* datFile)
{
	int err = 0;

	EdfWriter_t br;
	size_t writed = 0;
	if ((err = EdfOpen(&br, edfFile, "rb")))
		return err;

	FILE* f = NULL;
	if ((err = fopen_s(&f, datFile, "wb")))
		return err;

	// hint
	//int const* ptr; // ptr is a pointer to constant int 
	//int* const ptr;  // ptr is a constant pointer to int

	SPSK_FILE_V1_1 dat = { 0 };
	dat.FileType = 11;
	memcpy(dat.FileDescription, FileDescMt, sizeof(FileDescMt));

	OMEGA_DATA_V1_1 record = { 0 };
	size_t recN = 0;
	const size_t data_len = sizeof(OMEGA_DATA_V1_1) - 2;// skip crc, not used yet
	uint8_t* precord = (void*)&record;
	uint8_t* const recordBegin = precord;
	uint8_t* const recordEnd = recordBegin + data_len;

	size_t skip = 0;
	uint8_t bDst[3 * 256 + 8] = { 0 };
	MemStream_t msDst = { 0 };
	if ((err = MemStreamOpen(&msDst, bDst, sizeof(bDst), 0, "w")))
		return err;

	while (!(err = EdfReadBlock(&br)))
	{
		MemStream_t src = { 0 };
		if ((err = MemStreamInOpen(&src, br.Blk.Data, br.Blk.Len)))
			return err;

		switch (br.Blk.Type)
		{
		default: break;
		case btConfig:
			if (16 == br.Blk.Len)
			{
				//EdfConfig_t h = { 0 };
				//err = MakeHeaderFromBytes(br.Blk.Data, br.Blk.Len, &h);
				//if (!err)
				//	err = EdfWriteConfig(&tw, &h, &writed);
			}
			break;
		case btInf:
		{
			br.TypePtr = NULL;
			EdfInf_t* typeRec = NULL;
			err = StreamWriteBinToCBin(br.Blk.Data, br.Blk.Len, NULL, br.Buf, sizeof(br.Buf), NULL, &typeRec);
			if (!err)
			{
				br.TypePtr = typeRec;
				writed = 0;
			}
			else
			{
				err = 0;
				//return err;// ignore wrong or too big info block
			}
		}
		break;
		case btData:
		{
			if (br.TypePtr->Id)
			{
				switch (br.TypePtr->Id)
				{
				default: break;
				case FILETYPEID:
					if (dat.FileType != ((FileTypeId_t*)br.Blk.Data)->Type)
						return 0;
					break;//case FILETYPE:
				case BEGINDATETIME:
				{
					DateTime_t t = *((DateTime_t*)br.Blk.Data);
					dat.Year = (uint8_t)(t.Year - 2000);
					dat.Month = t.Month;
					dat.Day = t.Day;
				}
				break;
				case POSITION:
				{
					Position_t* p = NULL;
					if ((err = EdfReadBin(&PositionType, &src, &msDst, &p, &skip, NULL)))
						return err;

					unsigned long ulVal = strtoul(p->Field, NULL, 10);
					if (ERANGE == errno)
					{
						errno = 0;
						ulVal = 0;
					}
					dat.Id.Field = (uint16_t)ulVal;

					uint8_t len = MIN(*((uint8_t*)p->Cluster), FIELD_SIZEOF(FILES_RESEARCH_ID_V1_0, Cluster));
					memcpy(dat.Id.Cluster, p->Cluster, len);

					len = MIN(*((uint8_t*)p->Well), FIELD_SIZEOF(FILES_RESEARCH_ID_V1_0, Well));
					memcpy(dat.Id.Well, p->Well, len);

					ulVal = strtoul(p->Shop, NULL, 10);
					if (ERANGE == errno)
					{
						errno = 0;
						ulVal = 0;
					}
					dat.Id.Shop = (uint16_t)ulVal;
				}
				break;
				case DEVICEINFO:
				{
					DeviceInfo_t* dvc = NULL;
					if ((err = EdfReadBin(&DeviceInfoType, &src, &msDst, &dvc, &skip, NULL)))
						return err;
					dat.SensType = (uint16_t)dvc->SwId;
					dat.SensVer = (uint16_t)dvc->SwModel;
					dat.SensNum = (uint32_t)dvc->HwNumber;
				}
				break;
				case REGINFO:
				{
					DeviceInfo_t* dvc = NULL;
					if ((err = EdfReadBin(&DeviceInfoType, &src, &msDst, &dvc, &skip, NULL)))
						return err;
					dat.RegType = (uint16_t)dvc->SwId;
					dat.RegVer = (uint16_t)dvc->SwModel;
					dat.RegNum = (uint16_t)dvc->HwNumber;
				}
				break;

				case OMEGADATA:
				{
					if (0 == recN++)
					{
						dat.crc = MbCrc16(&dat, sizeof(SPSK_FILE_V1_1));
						if (1 != fwrite(&dat, sizeof(SPSK_FILE_V1_1), 1, f))
							return ERR_FWRITE;
					}
					uint8_t* pblock = br.Blk.Data;

					while (0 < br.Blk.Len)
					{
						size_t len = (size_t)MIN(br.Blk.Len, (size_t)(recordEnd - precord));
						memcpy(precord, pblock, len);
						precord += len;
						pblock += len;
						br.Blk.Len -= (uint16_t)len;
						if (recordEnd == precord)
						{
							precord = recordBegin;
							if (1 != fwrite(&record, sizeof(OMEGA_DATA_V1_1), 1, f))
								return ERR_FWRITE;
						}
					}//while (0 < br.Blk.Len)
				}
				break;//OMEGADATAREC

				}//switch

			}
			else if (IsVarName(br.TypePtr, "Shop"))
				dat.Id.Shop = *((uint16_t*)br.Blk.Data);
			else if (IsVarName(br.TypePtr, "PlaceId"))
				dat.Id.PlaceId = *((uint16_t*)br.Blk.Data);
			else if (IsVarName(br.TypePtr, "Depth"))
				dat.Id.Depth = *((int32_t*)br.Blk.Data);

		}//case btData:
		break;
		}//switch (br.Blk.Type)
		if (0 != err)
		{
			LOG_ERR();
			break;
		}
	}//while (!(err = EdfReadBlock(&br)))

	fclose(f);
	EdfClose(&br);
	return 0;
}
//-----------------------------------------------------------------------------
