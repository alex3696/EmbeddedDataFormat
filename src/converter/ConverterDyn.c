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
/// DYN
//-----------------------------------------------------------------------------
static int8_t ExtractTravel(uint16_t number) // 6bit integer
{
	int8_t result = ((number & 0xFC00) >> 10);
	return (result > 31) ? (result - 64) : result;
}
//-----------------------------------------------------------------------------
int DynToEdf(const char* src, const char* edf, char mode)
{
	assert(8 == strlen("тест"));

	FILE* f = NULL;
	int err = fopen_s(&f, src, "rb");
	if (err)
		return err;

	DYN_FILE_V2_0 dat;
	if (1 != fread(&dat, sizeof(DYN_FILE_V2_0), 1, f))
		return ERR_FREAD;

	EdfWriter_t dw;
	size_t writed = 0;

	if ('t' == mode)
		err = EdfOpen(&dw, edf, "wt");
	else if ('b' == mode)
		err = EdfOpen(&dw, edf, "wb");
	else
		err = ERR_WRONG_PARAMETERS;
	if (err)
		return err;


	EdfHeader_t h = MakeHeaderDefault();
	if ((err = EdfWriteHeader(&dw, &h, &writed)))
		return err;

	//EdfWriteInfData(&dw, 0, String, "Comment", "ResearchTypeId={ECHOGRAM-5, DYNAMOGRAM-6, SAMT-11}");
	const TypeRec_t typeInf = { FileTypeIdType, FILETYPEID };
	EdfWriteInfRecData(&dw, &typeInf, &(FileTypeId_t){ (uint16_t)dat.FileType, 1}, sizeof(FileTypeId_t));

	const TypeRec_t beginDtInf = { DateTimeType, BEGINDATETIME, "BeginDateTime" };
	const DateTime_t beginDtDat =
	{
		dat.Id.Time.Year + 2000, dat.Id.Time.Month, dat.Id.Time.Day,
		dat.Id.Time.Hour, dat.Id.Time.Min, dat.Id.Time.Sec,
	};
	EdfWriteInfRecData(&dw, &beginDtInf, &beginDtDat, sizeof(DateTime_t));

	char field[256] = { 0 };
	char cluster[256] = { 0 };
	char well[256] = { 0 };
	char shop[256] = { 0 };
	snprintf(field, sizeof(field) - 1, "%d", dat.Id.Field);
	memcpy(cluster, dat.Id.Cluster, strnlength(dat.Id.Cluster, FIELD_SIZEOF(RESEARCH_ID_V2_0, Cluster)));
	memcpy(well, dat.Id.Well, strnlength(dat.Id.Well, FIELD_SIZEOF(RESEARCH_ID_V2_0, Well)));
	snprintf(shop, sizeof(shop) - 1, "%d", dat.Id.Shop);
	const TypeRec_t posInf = { PositionType, POSITION, "Position" };
	const Position_t posDat = { .Field = field, .Cluster = cluster, .Well = well, .Shop = shop, };
	EdfWriteInfRecData(&dw, &posInf, &posDat, sizeof(Position_t));

	const TypeRec_t devInf = { DeviceInfoType, DEVICEINFO, "DevInfo", "прибор" };
	const DeviceInfo_t devDat =
	{
		.SwId = dat.Id.DeviceType, .SwModel = 0, .SwRevision = 0,
		.HwId = 0, .HwModel = 0, .HwNumber = dat.Id.DeviceNum
	};
	EdfWriteInfRecData(&dw, &devInf, &devDat, sizeof(DeviceInfo_t));

	const TypeRec_t regInf = { DeviceInfoType, REGINFO, "RegInfo", "регистратор" };
	const DeviceInfo_t regDat =
	{
		.SwId = dat.Id.RegType, .SwModel = 0, .SwRevision = 0,
		.HwId = 0, .HwModel = 0, .HwNumber = dat.Id.RegNum
	};
	EdfWriteInfRecData(&dw, &regInf, &regDat, sizeof(DeviceInfo_t));
	EdfWriteInfData(&dw, 0, UInt16, "Oper", &dat.Id.Oper);

	EdfWriteInfData0(&dw, UInt16, 0, "TravelStep", "величина дискреты перемещения 0.1мм/1", &dat.TravelStep);
	EdfWriteInfData0(&dw, UInt16, 0, "LoadStep", "величина дискреты нагрузки кг/1", &dat.LoadStep);
	EdfWriteInfData0(&dw, UInt16, 0, "TimeStep", "величина дискреты времени мс/1", &dat.TimeStep);

	EdfWriteInfData0(&dw, Single, 0, "Rod", "диаметр штока", &((float) { dat.Rod / 10.0f }));
	EdfWriteInfData0(&dw, UInt16, 0, "Aperture", "номер отверстия", &dat.Aperture);
	EdfWriteInfData0(&dw, UInt32, 0, "MaxWeight", "максимальная нагрузка (кг)", &((uint32_t) { dat.MaxWeight* dat.LoadStep }));
	EdfWriteInfData0(&dw, UInt32, 0, "MinWeight", "минимальная нагрузка (кг)", &((uint32_t) { dat.MinWeight* dat.LoadStep }));
	EdfWriteInfData0(&dw, UInt32, 0, "TopWeight", "вес штанг вверху (кг)", &((uint32_t) { dat.TopWeight* dat.LoadStep }));
	EdfWriteInfData0(&dw, UInt32, 0, "BotWeight", "вес штанг внизу (кг)", &((uint32_t) { dat.BotWeight* dat.LoadStep }));
	EdfWriteInfData0(&dw, Double, 0, "Travel", "ход штока (мм)", &((double) { dat.Travel* dat.TravelStep / 10.0f }));
	EdfWriteInfData0(&dw, Double, 0, "BeginPos", "положение штока перед первым измерением (мм)",
		&((double) { dat.BeginPos* dat.TravelStep / 10.0f }));
	EdfWriteInfData0(&dw, UInt32, 0, "Period", "период качаний (мс)", &((uint32_t) { dat.Period* dat.TimeStep }));
	EdfWriteInfData0(&dw, UInt16, 0, "Cycles", "пропущено циклов", &dat.Cycles);
	EdfWriteInfData0(&dw, Double, 0, "Pressure", "затрубное давление (атм)", &((double) { dat.Pressure / 10.0f }));
	EdfWriteInfData0(&dw, Double, 0, "BufPressure", "буферное давление (атм)", &((double) { dat.BufPressure / 10.0f }));
	EdfWriteInfData0(&dw, Double, 0, "LinePressure", "линейное давление (атм)", &((double) { dat.LinePressure / 10.0f }));
	EdfWriteInfData0(&dw, UInt16, 0, "PumpType", "тип привода станка-качалки {}", &dat.PumpType);
	EdfWriteInfData0(&dw, Single, 0, "Acc", "напряжение аккумулятора датчика, (В)", &((float) { dat.Acc / 10.0f }));
	EdfWriteInfData0(&dw, Single, 0, "Temp", "температура датчика, (°С)", &((float) { dat.Temp / 10.0f }));

	const TypeRec_t chartsInf = { ChartNInf, 0, "DynamogrammChartInfo" };
	const ChartN_t chartsDat[] =
	{
		{ "Position", "m", "", "перемещение" },
		{ "Weight", "T", "", "вес" }
	};
	EdfWriteInfRecData(&dw, &chartsInf, &chartsDat, sizeof(chartsDat));

	EdfWriteInfo(&dw, &(const TypeRec_t){ Point2DInf, 0, "DynChart"}, & writed);
	struct PointXY p = { 0,0 };
	for (size_t i = 0; i < 1000; i++)
	{
		p.x += (float)(ExtractTravel(dat.Data[i]) * dat.TravelStep / 1.E4);
		p.y = (float)((dat.Data[i] & 1023) * dat.LoadStep * 1.0E-3);
		EdfWriteDataBlock(&dw, &p, sizeof(struct PointXY));
	}
	fclose(f);
	EdfClose(&dw);
	return 0;
}
//-----------------------------------------------------------------------------

int EdfToDyn(const char* edfFile, const char* dynFile)
{
	int err = 0;

	EdfWriter_t br;
	size_t writed = 0;
	if ((err = EdfOpen(&br, edfFile, "rb")))
		return err;

	FILE* f = NULL;
	if ((err = fopen_s(&f, dynFile, "wb")))
		return err;

	DYN_FILE_V2_0 dat = { 0 };
	dat.FileType = 6;
	dat.Id.ResearchType = 1;
	memcpy(dat.FileDescription, FileDescDyn, sizeof(FileDescDyn));
	size_t recN = 0;
	PointXY_t record = { 0 };

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
			skip = 0;
			msDst.WPos = 0;
			br.TypePtr = NULL;
			err = StreamWriteBinToCBin(br.Block, br.DatLen, NULL, br.Buf, sizeof(br.Buf), NULL, &br.TypePtr);
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
			if (br.TypePtr->Id)
			{
				switch (br.TypePtr->Id)
				{
				default: break;
				case FILETYPEID:
					if (dat.FileType != ((FileTypeId_t*)br.Block)->Type)
						return 0;
					break;//case FILETYPE:
				case BEGINDATETIME:
				{
					DateTime_t* t = (DateTime_t*)br.Block;
					dat.Id.Time.Year = (uint8_t)(t->Year - 2000);
					dat.Id.Time.Month = t->Month;
					dat.Id.Time.Day = t->Day;
					dat.Id.Time.Hour = t->Hour;
					dat.Id.Time.Min = t->Min;
					dat.Id.Time.Sec = t->Sec;
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
					dat.Id.DeviceType = (uint16_t)dvc->SwId;
					dat.Id.DeviceNum = (uint32_t)dvc->HwNumber;
				}
				break;
				case REGINFO:
				{
					DeviceInfo_t* dvc = NULL;
					if ((err = EdfReadBin(&DeviceInfoType, &src, &msDst, &dvc, &skip, NULL)))
						return err;
					dat.Id.RegType = (uint16_t)dvc->SwId;
					dat.Id.RegNum = (uint32_t)dvc->HwNumber;
				}
				break;

				}//switch
			}//if (br.TypePtr->Id)
			else if (IsVarName(br.TypePtr, "Oper"))
				dat.Id.Oper = *((uint16_t*)br.Block);
			else if (IsVarName(br.TypePtr, "TravelStep"))
				dat.TravelStep = *(uint16_t*)br.Block;
			else if (IsVarName(br.TypePtr, "LoadStep"))
				dat.LoadStep = *(uint16_t*)br.Block;
			else if (IsVarName(br.TypePtr, "TimeStep"))
				dat.TimeStep = *(uint16_t*)br.Block;
			else if (IsVarName(br.TypePtr, "Rod"))
				dat.Rod = (uint16_t)(*(float*)br.Block * 10);
			else if (IsVarName(br.TypePtr, "Aperture"))
				dat.Aperture = (*(uint16_t*)br.Block);
			else if (IsVarName(br.TypePtr, "MaxWeight"))
				dat.MaxWeight = (uint16_t)(*(uint32_t*)br.Block / dat.LoadStep);
			else if (IsVarName(br.TypePtr, "MinWeight"))
				dat.MinWeight = (uint16_t)(*(uint32_t*)br.Block / dat.LoadStep);
			else if (IsVarName(br.t, "TopWeight"))
				dat.TopWeight = (uint16_t)(*(uint32_t*)br.Block / dat.LoadStep);
			else if (IsVarName(br.TypePtr, "BotWeight"))
				dat.BotWeight = (uint16_t)(*(uint32_t*)br.Block / dat.LoadStep);
			else if (IsVarName(br.TypePtr, "Travel"))
				dat.Travel = (uint16_t)(*(double*)br.Block * 10.0 / dat.TravelStep);
			else if (IsVarName(br.TypePtr, "BeginPos"))
				dat.BeginPos = (uint16_t)(*(double*)br.Block * 10.0 / dat.TravelStep);
			else if (IsVarName(br.TypePtr, "Period"))
				dat.Period = (uint16_t)(*(uint32_t*)br.Block / dat.TimeStep);
			else if (IsVarName(br.TypePtr, "Cycles"))
				dat.Cycles = *(uint16_t*)br.Block;
			else if (IsVarName(br.TypePtr, "BeginPos"))
				dat.Pressure = (uint16_t)(*(double*)br.Block * 10.0);
			else if (IsVarName(br.TypePtr, "BeginPos"))
				dat.BufPressure = (uint16_t)(*(double*)br.Block * 10.0);
			else if (IsVarName(br.TypePtr, "BeginPos"))
				dat.LinePressure = (uint16_t)(*(double*)br.Block * 10.0);
			else if (IsVarName(br.TypePtr, "PumpType"))
				dat.PumpType = *(uint16_t*)br.Block;
			else if (IsVarName(br.TypePtr, "Acc"))
				dat.Acc = (uint16_t)(*(float*)br.Block * 10);
			else if (IsVarName(br.TypePtr, "Temp"))
				dat.Temp = (uint16_t)(*(float*)br.Block * 10);

			else if (IsVarName(br.TypePtr, "DynChart"))
			{
				PointXY_t* s = NULL;
				while (!(err = EdfReadBin(&Point2DInf, &src, &msDst, &s, &skip, NULL))
					&& recN <= FIELD_ITEMS_COUNT(DYN_FILE_V2_0, Data))
				{
					double posDif = recN ? s->x - record.x : s->x;
					//double posDif = s->x;
					uint16_t tr = (((uint16_t)round(posDif * 1.0E4 / dat.TravelStep) & 0x003f)) << 10;
					uint16_t w = ((uint16_t)(round(s->y * 1.0E3 / dat.LoadStep)) & 0x003f);
					dat.Data[recN++] = tr | w;
					record = *s;
					s = NULL;
					skip = 0;
					msDst.WPos = 0;
				}
				skip = skip;
				err = 0;
			}//else
		}//case btVarData:
		break;
		}//switch (br.BlkType)
		if (0 != err)
		{
			LOG_ERR();
			break;
		}
	}//while (!(err = EdfReadBlock(&br)))

	dat.crc = MbCrc16(&dat, sizeof(DYN_FILE_V2_0) - 2);

	if (1 != fwrite(&dat, sizeof(DYN_FILE_V2_0), 1, f))
		return ERR_FWRITE;

	fclose(f);
	EdfClose(&br);
	return 0;
}


//-----------------------------------------------------------------------------
