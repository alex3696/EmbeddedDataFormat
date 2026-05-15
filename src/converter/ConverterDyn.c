#include "_pch.h"
#include "assert.h"
#include "Charts.h"
#include "converter.h"
#include "edf.h"
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
int DynToEdf(const char* src, const char* edfFile, char mode)
{
	assert(8 == strlen("тест"));

	FILE* f = NULL;
	int err = fopen_s(&f, src, "rb");
	if (err)
		return err;

	DYN_FILE_V2_0 dat;
	if (1 != fread(&dat, sizeof(DYN_FILE_V2_0), 1, f))
		return ERR_FREAD;

	uint8_t edfMem[MEM_BLOCK_SIZE_256] = { 0 };
	EdfContext_t* edf = EdfCreate(edfMem, sizeof(edfMem), &EdfCfg256, &err);
	size_t writed = 0;

	if ('t' == mode)
		err = EdfOpenFile(edf, edfFile, "wt");
	else if ('b' == mode)
		err = EdfOpenFile(edf, edfFile, "wb");
	else
		err = ERR_WRONG_PARAMETERS;
	if (err)
		return err;

	if ((err = EdfWriteConfig(edf, &writed)))
		return err;

	//EdfWritePrimSchData(edf, String,0, "Comment", NULL, "ResearchTypeId={ECHOGRAM-5, DYNAMOGRAM-6, SAMT-11}");
	const EdfSchema_t typeInf = { FILETYPEID, NULL, NULL, FileTypeIdType };
	EdfWriteSchemaData(edf, &typeInf, &(FileTypeId_t){ (uint16_t)dat.FileType, 1}, sizeof(FileTypeId_t));

	const EdfSchema_t beginDtInf = { BEGINDATETIME, "BeginDateTime", NULL, DateTimeType};
	const DateTime_t beginDtDat =
	{
		dat.Id.Time.Year + 2000, dat.Id.Time.Month, dat.Id.Time.Day,
		dat.Id.Time.Hour, dat.Id.Time.Min, dat.Id.Time.Sec,
	};
	EdfWriteSchemaData(edf, &beginDtInf, &beginDtDat, sizeof(DateTime_t));

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
	EdfWriteSchemaData(edf, &posInf, &posDat, sizeof(Position_t));

	const EdfSchema_t devInf = { DEVICEINFO, "DevInfo", "прибор", DeviceInfoType};
	const DeviceInfo_t devDat =
	{
		.SwId = dat.Id.DeviceType, .SwModel = 0, .SwRevision = 0,
		.HwId = 0, .HwModel = 0, .HwNumber = dat.Id.DeviceNum
	};
	EdfWriteSchemaData(edf, &devInf, &devDat, sizeof(DeviceInfo_t));

	const EdfSchema_t regInf = { REGINFO, "RegInfo", "регистратор", DeviceInfoType };
	const DeviceInfo_t regDat =
	{
		.SwId = dat.Id.RegType, .SwModel = 0, .SwRevision = 0,
		.HwId = 0, .HwModel = 0, .HwNumber = dat.Id.RegNum
	};
	EdfWriteSchemaData(edf, &regInf, &regDat, sizeof(DeviceInfo_t));
	EdfWritePrimSchData(edf, UInt16, 0, "Oper", NULL, &dat.Id.Oper);

	EdfWritePrimSchData(edf, UInt16, 0, "TravelStep", "величина дискреты перемещения 0.1мм/1", &dat.TravelStep);
	EdfWritePrimSchData(edf, UInt16, 0, "LoadStep", "величина дискреты нагрузки кг/1", &dat.LoadStep);
	EdfWritePrimSchData(edf, UInt16, 0, "TimeStep", "величина дискреты времени мс/1", &dat.TimeStep);

	EdfWritePrimSchData(edf, Single, 0, "Rod", "диаметр штока", &((float) { dat.Rod / 10.0f }));
	EdfWritePrimSchData(edf, UInt16, 0, "Aperture", "номер отверстия", &dat.Aperture);
	EdfWritePrimSchData(edf, UInt32, 0, "MaxWeight", "максимальная нагрузка (кг)", &((uint32_t) { dat.MaxWeight* dat.LoadStep }));
	EdfWritePrimSchData(edf, UInt32, 0, "MinWeight", "минимальная нагрузка (кг)", &((uint32_t) { dat.MinWeight* dat.LoadStep }));
	EdfWritePrimSchData(edf, UInt32, 0, "TopWeight", "вес штанг вверху (кг)", &((uint32_t) { dat.TopWeight* dat.LoadStep }));
	EdfWritePrimSchData(edf, UInt32, 0, "BotWeight", "вес штанг внизу (кг)", &((uint32_t) { dat.BotWeight* dat.LoadStep }));
	EdfWritePrimSchData(edf, Double, 0, "Travel", "ход штока (мм)", &((double) { dat.Travel* dat.TravelStep / 10.0f }));
	EdfWritePrimSchData(edf, Double, 0, "BeginPos", "положение штока перед первым измерением (мм)",
		&((double) { dat.BeginPos* dat.TravelStep / 10.0f }));
	EdfWritePrimSchData(edf, UInt32, 0, "Period", "период качаний (мс)", &((uint32_t) { dat.Period* dat.TimeStep }));
	EdfWritePrimSchData(edf, UInt16, 0, "Cycles", "пропущено циклов", &dat.Cycles);
	EdfWritePrimSchData(edf, Double, 0, "Pressure", "затрубное давление (атм)", &((double) { dat.Pressure / 10.0f }));
	EdfWritePrimSchData(edf, Double, 0, "BufPressure", "буферное давление (атм)", &((double) { dat.BufPressure / 10.0f }));
	EdfWritePrimSchData(edf, Double, 0, "LinePressure", "линейное давление (атм)", &((double) { dat.LinePressure / 10.0f }));
	EdfWritePrimSchData(edf, UInt16, 0, "PumpType", "тип привода станка-качалки {}", &dat.PumpType);
	EdfWritePrimSchData(edf, Single, 0, "Acc", "напряжение аккумулятора датчика, (В)", &((float) { dat.Acc / 10.0f }));
	EdfWritePrimSchData(edf, Single, 0, "Temp", "температура датчика, (°С)", &((float) { dat.Temp / 10.0f }));

	const EdfSchema_t chartsInf = { 0, "DynamogrammChartInfo", NULL, ChartNType};
	const ChartN_t chartsDat[] =
	{
		{ "Position", "m", "", "перемещение" },
		{ "Weight", "T", "", "вес" }
	};
	EdfWriteSchemaData(edf, &chartsInf, &chartsDat, sizeof(chartsDat));

	EdfWriteSchema(edf, &(const EdfSchema_t){ 0, "DynChart", NULL, Point2DType}, & writed);
	struct PointXY p = { 0,0 };
	for (size_t i = 0; i < 1000; i++)
	{
		p.x += (float)(ExtractTravel(dat.Data[i]) * dat.TravelStep / 1.E4);
		p.y = (float)((dat.Data[i] & 1023) * dat.LoadStep * 1.0E-3);
		EdfWriteData(edf, &p, sizeof(struct PointXY));
	}
	fclose(f);
	EdfClose(edf);
	return 0;
}
//-----------------------------------------------------------------------------

int EdfToDyn(const char* edfFile, const char* dynFile)
{
	int err = 0;

	uint8_t edfMem[MEM_BLOCK_SIZE_256] = { 0 };
	EdfContext_t* bdfr = EdfCreate(edfMem, sizeof(edfMem), &EdfCfg256, &err);

	size_t writed = 0;
	if ((err = EdfOpenFile(bdfr, edfFile, "rb")))
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

	while (!(err = EdfReadBlock(bdfr)))
	{
		MemStream_t src = { 0 };
		if ((err = MemStreamInOpen(&src, bdfr->Blk->Conent.Record.Data, GetContentDataLen(bdfr->Blk))))
			return err;

		switch (bdfr->Blk->Type)
		{
		default: break;
		case btConfig:
			break;
		case btSchema:
		{
			skip = 0;
			msDst.WPos = 0;
			bdfr->SchemaPtr = NULL;
			err = WriteSchemaBinToCBin(bdfr->Blk->Conent.Schema.Data, GetContentDataLen(bdfr->Blk), NULL, bdfr->Buf, bdfr->Cfg.Blocksize, NULL, &bdfr->SchemaPtr);
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
			if (bdfr->SchemaPtr->Id)
			{
				switch (bdfr->SchemaPtr->Id)
				{
				default: break;
				case FILETYPEID:
					if (dat.FileType != ((FileTypeId_t*)bdfr->Blk->Conent.Record.Data)->Type)
						return 0;
					break;//case FILETYPE:
				case BEGINDATETIME:
				{
					DateTime_t* t = (DateTime_t*)bdfr->Blk->Conent.Record.Data;
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
			}//if (bdfr->SchemaPtr->Id)
			else if (IsVarName(bdfr->SchemaPtr, "Oper"))
				dat.Id.Oper = *((uint16_t*)bdfr->Blk->Conent.Record.Data);
			else if (IsVarName(bdfr->SchemaPtr, "TravelStep"))
				dat.TravelStep = *(uint16_t*)bdfr->Blk->Conent.Record.Data;
			else if (IsVarName(bdfr->SchemaPtr, "LoadStep"))
				dat.LoadStep = *(uint16_t*)bdfr->Blk->Conent.Record.Data;
			else if (IsVarName(bdfr->SchemaPtr, "TimeStep"))
				dat.TimeStep = *(uint16_t*)bdfr->Blk->Conent.Record.Data;
			else if (IsVarName(bdfr->SchemaPtr, "Rod"))
				dat.Rod = (uint16_t)(*(float*)bdfr->Blk->Conent.Record.Data * 10);
			else if (IsVarName(bdfr->SchemaPtr, "Aperture"))
				dat.Aperture = (*(uint16_t*)bdfr->Blk->Conent.Record.Data);
			else if (IsVarName(bdfr->SchemaPtr, "MaxWeight"))
				dat.MaxWeight = (uint16_t)(*(uint32_t*)bdfr->Blk->Conent.Record.Data / dat.LoadStep);
			else if (IsVarName(bdfr->SchemaPtr, "MinWeight"))
				dat.MinWeight = (uint16_t)(*(uint32_t*)bdfr->Blk->Conent.Record.Data / dat.LoadStep);
			else if (IsVarName(bdfr->SchemaPtr, "TopWeight"))
				dat.TopWeight = (uint16_t)(*(uint32_t*)bdfr->Blk->Conent.Record.Data / dat.LoadStep);
			else if (IsVarName(bdfr->SchemaPtr, "BotWeight"))
				dat.BotWeight = (uint16_t)(*(uint32_t*)bdfr->Blk->Conent.Record.Data / dat.LoadStep);
			else if (IsVarName(bdfr->SchemaPtr, "Travel"))
				dat.Travel = (uint16_t)(*(double*)bdfr->Blk->Conent.Record.Data * 10.0 / dat.TravelStep);
			else if (IsVarName(bdfr->SchemaPtr, "BeginPos"))
				dat.BeginPos = (uint16_t)(*(double*)bdfr->Blk->Conent.Record.Data * 10.0 / dat.TravelStep);
			else if (IsVarName(bdfr->SchemaPtr, "Period"))
				dat.Period = (uint16_t)(*(uint32_t*)bdfr->Blk->Conent.Record.Data / dat.TimeStep);
			else if (IsVarName(bdfr->SchemaPtr, "Cycles"))
				dat.Cycles = *(uint16_t*)bdfr->Blk->Conent.Record.Data;
			else if (IsVarName(bdfr->SchemaPtr, "BeginPos"))
				dat.Pressure = (uint16_t)(*(double*)bdfr->Blk->Conent.Record.Data * 10.0);
			else if (IsVarName(bdfr->SchemaPtr, "BeginPos"))
				dat.BufPressure = (uint16_t)(*(double*)bdfr->Blk->Conent.Record.Data * 10.0);
			else if (IsVarName(bdfr->SchemaPtr, "BeginPos"))
				dat.LinePressure = (uint16_t)(*(double*)bdfr->Blk->Conent.Record.Data * 10.0);
			else if (IsVarName(bdfr->SchemaPtr, "PumpType"))
				dat.PumpType = *(uint16_t*)bdfr->Blk->Conent.Record.Data;
			else if (IsVarName(bdfr->SchemaPtr, "Acc"))
				dat.Acc = (uint16_t)(*(float*)bdfr->Blk->Conent.Record.Data * 10);
			else if (IsVarName(bdfr->SchemaPtr, "Temp"))
				dat.Temp = (uint16_t)(*(float*)bdfr->Blk->Conent.Record.Data * 10);

			else if (IsVarName(bdfr->SchemaPtr, "DynChart"))
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
		}//switch (bdfr->Blk->Type)
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
	EdfClose(bdfr);
	return 0;
}


//-----------------------------------------------------------------------------
