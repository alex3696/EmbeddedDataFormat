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


	EdfConfig_t h = MakeDefaultConfig();
	if ((err = EdfWriteConfig(&dw, &h, &writed)))
		return err;

	//EdfWritePrimSchData(&dw, String,0, "Comment", NULL, "ResearchTypeId={ECHOGRAM-5, DYNAMOGRAM-6, SAMT-11}");
	const EdfSchema_t typeInf = { FILETYPEID, NULL, NULL, FileTypeIdType };
	EdfWriteSchemaData(&dw, &typeInf, &(FileTypeId_t){ (uint16_t)dat.FileType, 1}, sizeof(FileTypeId_t));

	const EdfSchema_t beginDtInf = { BEGINDATETIME, "BeginDateTime", NULL, DateTimeType};
	const DateTime_t beginDtDat =
	{
		dat.Id.Time.Year + 2000, dat.Id.Time.Month, dat.Id.Time.Day,
		dat.Id.Time.Hour, dat.Id.Time.Min, dat.Id.Time.Sec,
	};
	EdfWriteSchemaData(&dw, &beginDtInf, &beginDtDat, sizeof(DateTime_t));

	char field[256] = { 0 };
	char cluster[256] = { 0 };
	char well[256] = { 0 };
	char shop[256] = { 0 };
	snprintf(field, sizeof(field) - 1, "%d", dat.Id.Field);
	memcpy(cluster, dat.Id.Cluster, strnlength(dat.Id.Cluster, FIELD_SIZEOF(RESEARCH_ID_V2_0, Cluster)));
	memcpy(well, dat.Id.Well, strnlength(dat.Id.Well, FIELD_SIZEOF(RESEARCH_ID_V2_0, Well)));
	snprintf(shop, sizeof(shop) - 1, "%d", dat.Id.Shop);
	const EdfSchema_t posInf = { POSITION, "Position" , NULL, PositionType };
	const Position_t posDat = { .Field = field, .Cluster = cluster, .Well = well, .Shop = shop, };
	EdfWriteSchemaData(&dw, &posInf, &posDat, sizeof(Position_t));

	const EdfSchema_t devInf = { DEVICEINFO, "DevInfo", "прибор", DeviceInfoType};
	const DeviceInfo_t devDat =
	{
		.SwId = dat.Id.DeviceType, .SwModel = 0, .SwRevision = 0,
		.HwId = 0, .HwModel = 0, .HwNumber = dat.Id.DeviceNum
	};
	EdfWriteSchemaData(&dw, &devInf, &devDat, sizeof(DeviceInfo_t));

	const EdfSchema_t regInf = { REGINFO, "RegInfo", "регистратор", DeviceInfoType };
	const DeviceInfo_t regDat =
	{
		.SwId = dat.Id.RegType, .SwModel = 0, .SwRevision = 0,
		.HwId = 0, .HwModel = 0, .HwNumber = dat.Id.RegNum
	};
	EdfWriteSchemaData(&dw, &regInf, &regDat, sizeof(DeviceInfo_t));
	EdfWritePrimSchData(&dw, UInt16, 0, "Oper", NULL, &dat.Id.Oper);

	EdfWritePrimSchData(&dw, UInt16, 0, "TravelStep", "величина дискреты перемещения 0.1мм/1", &dat.TravelStep);
	EdfWritePrimSchData(&dw, UInt16, 0, "LoadStep", "величина дискреты нагрузки кг/1", &dat.LoadStep);
	EdfWritePrimSchData(&dw, UInt16, 0, "TimeStep", "величина дискреты времени мс/1", &dat.TimeStep);

	EdfWritePrimSchData(&dw, Single, 0, "Rod", "диаметр штока", &((float) { dat.Rod / 10.0f }));
	EdfWritePrimSchData(&dw, UInt16, 0, "Aperture", "номер отверстия", &dat.Aperture);
	EdfWritePrimSchData(&dw, UInt32, 0, "MaxWeight", "максимальная нагрузка (кг)", &((uint32_t) { dat.MaxWeight* dat.LoadStep }));
	EdfWritePrimSchData(&dw, UInt32, 0, "MinWeight", "минимальная нагрузка (кг)", &((uint32_t) { dat.MinWeight* dat.LoadStep }));
	EdfWritePrimSchData(&dw, UInt32, 0, "TopWeight", "вес штанг вверху (кг)", &((uint32_t) { dat.TopWeight* dat.LoadStep }));
	EdfWritePrimSchData(&dw, UInt32, 0, "BotWeight", "вес штанг внизу (кг)", &((uint32_t) { dat.BotWeight* dat.LoadStep }));
	EdfWritePrimSchData(&dw, Double, 0, "Travel", "ход штока (мм)", &((double) { dat.Travel* dat.TravelStep / 10.0f }));
	EdfWritePrimSchData(&dw, Double, 0, "BeginPos", "положение штока перед первым измерением (мм)",
		&((double) { dat.BeginPos* dat.TravelStep / 10.0f }));
	EdfWritePrimSchData(&dw, UInt32, 0, "Period", "период качаний (мс)", &((uint32_t) { dat.Period* dat.TimeStep }));
	EdfWritePrimSchData(&dw, UInt16, 0, "Cycles", "пропущено циклов", &dat.Cycles);
	EdfWritePrimSchData(&dw, Double, 0, "Pressure", "затрубное давление (атм)", &((double) { dat.Pressure / 10.0f }));
	EdfWritePrimSchData(&dw, Double, 0, "BufPressure", "буферное давление (атм)", &((double) { dat.BufPressure / 10.0f }));
	EdfWritePrimSchData(&dw, Double, 0, "LinePressure", "линейное давление (атм)", &((double) { dat.LinePressure / 10.0f }));
	EdfWritePrimSchData(&dw, UInt16, 0, "PumpType", "тип привода станка-качалки {}", &dat.PumpType);
	EdfWritePrimSchData(&dw, Single, 0, "Acc", "напряжение аккумулятора датчика, (В)", &((float) { dat.Acc / 10.0f }));
	EdfWritePrimSchData(&dw, Single, 0, "Temp", "температура датчика, (°С)", &((float) { dat.Temp / 10.0f }));

	const EdfSchema_t chartsInf = { 0, "DynamogrammChartInfo", NULL, ChartNType};
	const ChartN_t chartsDat[] =
	{
		{ "Position", "m", "", "перемещение" },
		{ "Weight", "T", "", "вес" }
	};
	EdfWriteSchemaData(&dw, &chartsInf, &chartsDat, sizeof(chartsDat));

	EdfWriteSchema(&dw, &(const EdfSchema_t){ 0, "DynChart", NULL, Point2DType}, & writed);
	struct PointXY p = { 0,0 };
	for (size_t i = 0; i < 1000; i++)
	{
		p.x += (float)(ExtractTravel(dat.Data[i]) * dat.TravelStep / 1.E4);
		p.y = (float)((dat.Data[i] & 1023) * dat.LoadStep * 1.0E-3);
		EdfWriteData(&dw, &p, sizeof(struct PointXY));
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
		if ((err = MemStreamInOpen(&src, br.Blk.Data, br.Blk.Len)))
			return err;

		switch (br.Blk.Type)
		{
		default: break;
		case btConfig:
			if (16 == br.Blk.Len)
			{
				//EdfConfig_t h = { 0 };
				//err = MakeConfigFromBytes(br.Blk.Data, br.Blk.Len, &h);
				//if (!err)
				//	err = EdfWriteConfig(&tw, &h, &writed);
			}
			break;
		case btSchema:
		{
			skip = 0;
			msDst.WPos = 0;
			br.SchemaPtr = NULL;
			err = WriteSchemaBinToCBin(br.Blk.Data, br.Blk.Len, NULL, br.Buf, sizeof(br.Buf), NULL, &br.SchemaPtr);
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
		case btData:
		{
			if (br.SchemaPtr->Id)
			{
				switch (br.SchemaPtr->Id)
				{
				default: break;
				case FILETYPEID:
					if (dat.FileType != ((FileTypeId_t*)br.Blk.Data)->Type)
						return 0;
					break;//case FILETYPE:
				case BEGINDATETIME:
				{
					DateTime_t* t = (DateTime_t*)br.Blk.Data;
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
			}//if (br.SchemaPtr->Id)
			else if (IsVarName(br.SchemaPtr, "Oper"))
				dat.Id.Oper = *((uint16_t*)br.Blk.Data);
			else if (IsVarName(br.SchemaPtr, "TravelStep"))
				dat.TravelStep = *(uint16_t*)br.Blk.Data;
			else if (IsVarName(br.SchemaPtr, "LoadStep"))
				dat.LoadStep = *(uint16_t*)br.Blk.Data;
			else if (IsVarName(br.SchemaPtr, "TimeStep"))
				dat.TimeStep = *(uint16_t*)br.Blk.Data;
			else if (IsVarName(br.SchemaPtr, "Rod"))
				dat.Rod = (uint16_t)(*(float*)br.Blk.Data * 10);
			else if (IsVarName(br.SchemaPtr, "Aperture"))
				dat.Aperture = (*(uint16_t*)br.Blk.Data);
			else if (IsVarName(br.SchemaPtr, "MaxWeight"))
				dat.MaxWeight = (uint16_t)(*(uint32_t*)br.Blk.Data / dat.LoadStep);
			else if (IsVarName(br.SchemaPtr, "MinWeight"))
				dat.MinWeight = (uint16_t)(*(uint32_t*)br.Blk.Data / dat.LoadStep);
			else if (IsVarName(br.SchemaPtr, "TopWeight"))
				dat.TopWeight = (uint16_t)(*(uint32_t*)br.Blk.Data / dat.LoadStep);
			else if (IsVarName(br.SchemaPtr, "BotWeight"))
				dat.BotWeight = (uint16_t)(*(uint32_t*)br.Blk.Data / dat.LoadStep);
			else if (IsVarName(br.SchemaPtr, "Travel"))
				dat.Travel = (uint16_t)(*(double*)br.Blk.Data * 10.0 / dat.TravelStep);
			else if (IsVarName(br.SchemaPtr, "BeginPos"))
				dat.BeginPos = (uint16_t)(*(double*)br.Blk.Data * 10.0 / dat.TravelStep);
			else if (IsVarName(br.SchemaPtr, "Period"))
				dat.Period = (uint16_t)(*(uint32_t*)br.Blk.Data / dat.TimeStep);
			else if (IsVarName(br.SchemaPtr, "Cycles"))
				dat.Cycles = *(uint16_t*)br.Blk.Data;
			else if (IsVarName(br.SchemaPtr, "BeginPos"))
				dat.Pressure = (uint16_t)(*(double*)br.Blk.Data * 10.0);
			else if (IsVarName(br.SchemaPtr, "BeginPos"))
				dat.BufPressure = (uint16_t)(*(double*)br.Blk.Data * 10.0);
			else if (IsVarName(br.SchemaPtr, "BeginPos"))
				dat.LinePressure = (uint16_t)(*(double*)br.Blk.Data * 10.0);
			else if (IsVarName(br.SchemaPtr, "PumpType"))
				dat.PumpType = *(uint16_t*)br.Blk.Data;
			else if (IsVarName(br.SchemaPtr, "Acc"))
				dat.Acc = (uint16_t)(*(float*)br.Blk.Data * 10);
			else if (IsVarName(br.SchemaPtr, "Temp"))
				dat.Temp = (uint16_t)(*(float*)br.Blk.Data * 10);

			else if (IsVarName(br.SchemaPtr, "DynChart"))
			{
				PointXY_t* s = NULL;
				while (!(err = EdfReadBin(&Point2DType, &src, &msDst, &s, &skip, NULL))
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
		}//case btData:
		break;
		}//switch (br.Blk.Type)
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
