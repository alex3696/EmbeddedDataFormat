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

	EdfHeader_t h = MakeHeaderDefault();
	if ((err = EdfWriteHeader(&dw, &h, &writed)))
		return err;

	//EdfWriteInfData(&dw, 0, String, "Comment", "ResearchTypeId={ECHOGRAM-5, DYNAMOGRAM-6, SAMT-11}");
	const TypeRec_t typeInf = { FileTypeIdType, FILETYPEID };
	EdfWriteInfRecData(&dw, &typeInf, &(FileTypeId_t){ (uint16_t)dat.FileType, 1}, sizeof(FileTypeId_t));

	const TypeRec_t beginDtInf = { DateTimeType, BEGINDATETIME, "BeginDateTime" };
	const DateTime_t beginDtDat = { dat.Year + 2000, dat.Month, dat.Day, };
	EdfWriteInfRecData(&dw, &beginDtInf, &beginDtDat, sizeof(DateTime_t));

	char field[256] = { 0 };
	char cluster[256] = { 0 };
	char well[256] = { 0 };
	char shop[256] = { 0 };
	snprintf(field, sizeof(field) - 1, "%d", dat.Id.Field);
	memcpy(cluster, dat.Id.Cluster, strnlength(dat.Id.Cluster, FIELD_SIZEOF(FILES_RESEARCH_ID_V1_0, Cluster)));
	memcpy(well, dat.Id.Well, strnlength(dat.Id.Well, FIELD_SIZEOF(FILES_RESEARCH_ID_V1_0, Well)));
	snprintf(shop, sizeof(shop) - 1, "%d", dat.Id.Shop);
	const TypeRec_t posInf = { PositionType, POSITION, "Position" };
	const Position_t posDat = { .Field = field, .Cluster = cluster, .Well = well, .Shop = shop, };
	EdfWriteInfRecData(&dw, &posInf, &posDat, sizeof(Position_t));

	EdfWriteInfData0(&dw, UInt16, 0, "PlaceId", "место установки", &dat.Id.PlaceId);
	EdfWriteInfData0(&dw, Int32, 0, "Depth", "глубина установки", &dat.Id.Depth);

	const TypeRec_t devInf = { DeviceInfoType, DEVICEINFO, "DevInfo", "скважный прибор" };
	const DeviceInfo_t devDat =
	{
		.SwId = dat.SensType, .SwModel = dat.SensVer, .SwRevision = 0,
		.HwId = 0, .HwModel = 0, .HwNumber = dat.SensNum
	};
	EdfWriteInfRecData(&dw, &devInf, &devDat, sizeof(DeviceInfo_t));

	const TypeRec_t regInf = { DeviceInfoType, REGINFO, "RegInfo", "наземный регистратор" };
	const DeviceInfo_t regDat =
	{
		.SwId = dat.RegType, .SwModel = dat.RegVer, .SwRevision = 0,
		.HwId = 0, .HwModel = 0, .HwNumber = dat.RegNum
	};
	EdfWriteInfRecData(&dw, &regInf, &regDat, sizeof(DeviceInfo_t));

	const TypeRec_t chartsInf = { ChartNInf, 0, "ChartInfo" };
	const ChartN_t chartsDat[] =
	{
		{ "Time", "мс", "", "время измерения от начала дня" },
		{ "Press", "0.001 атм","", "давление" },
		{ "Temp", "0.001 °С","", "температура" },
		{ "Vbat", "0.001 V","", "напряжение батареи" },
	};
	EdfWriteInfRecData(&dw, &chartsInf, &chartsDat, sizeof(chartsDat));

	if ((err = EdfWriteInfo(&dw, &(TypeRec_t){OmegaDataType, OMEGADATA}, & writed)))
		return err;

	OMEGA_DATA_V1_1 record;
	do
	{
		if (1 == fread(&record, sizeof(OMEGA_DATA_V1_1), 1, f))
		{
			if ((err = EdfWriteDataBlock(&dw, &record, sizeof(OMEGA_DATA_V1_1) - 2)))
				return err;
			//EdfFlushDataBlock(&dw, &writed);
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
		if ((err = MemStreamInOpen(&src, br.Block, br.DatLen)))
			return err;

		switch (br.BlkType)
		{
		default: break;
		case btHeader:
			if (16 == br.DatLen)
			{
				//EdfHeader_t h = { 0 };
				//err = MakeHeaderFromBytes(br.Block, br.DatLen, &h);
				//if (!err)
				//	err = EdfWriteHeader(&tw, &h, &writed);
			}
			break;
		case btVarInfo:
		{
			br.t = NULL;
			err = StreamWriteBinToCBin(br.Block, br.DatLen, NULL, br.Buf, sizeof(br.Buf), NULL, &br.t);
			if (!err)
			{
				writed = 0;
			}
			else
			{
				err = 0;
				//return err;// ignore wrong or too big info block
			}
		}
		break;
		case btVarData:
		{
			if (br.t->Id)
			{
				switch (br.t->Id)
				{
				default: break;
				case FILETYPEID:
					if (dat.FileType != ((FileTypeId_t*)br.Block)->Type)
						return 0;
					break;//case FILETYPE:
				case BEGINDATETIME:
				{
					DateTime_t t = *((DateTime_t*)br.Block);
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
					uint8_t* pblock = br.Block;

					while (0 < br.DatLen)
					{
						size_t len = (size_t)MIN(br.DatLen, (size_t)(recordEnd - precord));
						memcpy(precord, pblock, len);
						precord += len;
						pblock += len;
						br.DatLen -= (uint16_t)len;
						if (recordEnd == precord)
						{
							precord = recordBegin;
							if (1 != fwrite(&record, sizeof(OMEGA_DATA_V1_1), 1, f))
								return ERR_FWRITE;
						}
					}//while (0 < br.DatLen)
				}
				break;//OMEGADATAREC

				}//switch

			}
			else if (IsVarName(br.t, "Shop"))
				dat.Id.Shop = *((uint16_t*)br.Block);
			else if (IsVarName(br.t, "PlaceId"))
				dat.Id.PlaceId = *((uint16_t*)br.Block);
			else if (IsVarName(br.t, "Depth"))
				dat.Id.Depth = *((int32_t*)br.Block);

		}//case btVarData:
		break;
		}//switch (br.BlkType)
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
