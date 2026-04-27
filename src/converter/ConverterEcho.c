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
/// ECHO
//-----------------------------------------------------------------------------

static const double Discrete3000 = 0.00585938;
static const double Discrete6000 = 0.01171875;
//-----------------------------------------------------------------------------
static double ExtractDiscrete(uint16_t level)
{
	return (level & 0x4000) ? Discrete6000 : Discrete3000;
}
//-----------------------------------------------------------------------------
static double ExtractLevel(uint16_t level)
{
	return level & 0xBFFF;
}
//-----------------------------------------------------------------------------
static uint16_t PackLevel(double level, double discrete)
{
	return ((uint16_t)round(level)) | (Discrete6000 == discrete ? 0x4000 : 0);
}
//-----------------------------------------------------------------------------
static uint16_t ExtractReflections(uint16_t val)
{
	// ахтунг! параметр передаётся в двоично десятичном виде :-(
	// правильнее:  десятично-шестнадцатиричном :-)
	const uint16_t mask = 0x000F;
	uint16_t dec = (uint16_t)((((val >> 4)) & mask) * 10);
	uint16_t sig = (uint16_t)(val & mask);
	int refect = dec + sig;
	return (refect > 99) ? (uint16_t)99 : (uint16_t)refect;
}
//-----------------------------------------------------------------------------
// функция преобразования двоичного в 2/10 число. 0xffff=>65535
static uint16_t PackReflections(int16_t  chisl)
{
	int16_t  pr;
	uint32_t  sum = 0;
	for (int i = 0; i < 5; ++i)
	{
		pr = chisl / 10;
		sum += ((uint32_t)(chisl - pr * 10)) << (4 * i);
		chisl = pr;
	}
	return (uint16_t)sum;
}
//-----------------------------------------------------------------------------
static double UnPow(double v, double p)
{
	if (0 > v)
		return pow(fabs(v), p) * (-1.0f);
	return pow(v, p);
}
static double Pow(double v, double p)
{
	if (0 > v)
		return pow(fabs(v), p) * (-1.0f);
	return pow(v, p);
}
//-----------------------------------------------------------------------------
int EchoToEdf(const char* src, const char* edf, char mode)
{
	FILE* f = NULL;
	int err = fopen_s(&f, src, "rb");
	if (err)
		return err;

	ECHO_FILE_V2_0 dat;
	if (1 != fread(&dat, sizeof(ECHO_FILE_V2_0), 1, f))
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

	double discrete = ExtractDiscrete(dat.Level);
	int maxDepthMult = 1;
	if (Discrete3000 < discrete)
		maxDepthMult = 2;
	float speed = (float)dat.Speed / 10;
	float xDiscrete = speed / 341;

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

	EdfWriteInfData0(&dw, Double, 0, "Discrete", "величина дискреты", &discrete);
	EdfWriteInfData0(&dw, UInt16, 0, "Reflections", "число отражений",
		&((uint16_t) { ExtractReflections(dat.Reflections) }));
	EdfWriteInfData0(&dw, Double, 0, "Level", "уровень без поправки на скорость звука (для скорости 341.333 м/с), м",
		&((double) { ExtractLevel(dat.Level) }));
	EdfWriteInfData0(&dw, Double, 0, "Pressure", "затрубное давление (атм)", &((double) { dat.Pressure / 10.0f }));
	EdfWriteInfData0(&dw, UInt16, 0, "Table", "номер таблицы скоростей", &dat.Table);
	EdfWriteInfData0(&dw, Single, 0, "Speed", "скорость звука, м/с", &speed);
	EdfWriteInfData0(&dw, Double, 0, "BufPressure", "буферное давление (атм)", &((double) { dat.BufPressure / 10.0f }));
	EdfWriteInfData0(&dw, Double, 0, "LinePressure", "линейное давление (атм)", &((double) { dat.LinePressure / 10.0f }));
	EdfWriteInfData0(&dw, UInt16, 0, "Current", "ток, 0.1А", &dat.Current);
	EdfWriteInfData0(&dw, UInt8, 0, "IdleHour", "время простоя, ч", &dat.IdleHour);
	EdfWriteInfData0(&dw, UInt8, 0, "IdleMin", "время простоя, мин", &dat.IdleMin);
	EdfWriteInfData0(&dw, UInt8, 0, "Mode", "режим исследования", &dat.Mode);
	EdfWriteInfData0(&dw, Single, 0, "Acc", "напряжение аккумулятора датчика, (В)", &((float) { dat.Acc / 10.0f }));
	EdfWriteInfData0(&dw, Single, 0, "Temp", "температура датчика, (°С)", &((float) { dat.Temp / 10.0f }));

	const TypeRec_t chartsInf = { ChartNInf, 0, "EchoChartInfo" };
	const ChartN_t chartsDat[] =
	{
		{ "Depth", "m", "", "глубина" },
		{ "Val", "adc", "", "амплитуда" },
	};
	EdfWriteInfRecData(&dw, &chartsInf, &chartsDat, sizeof(chartsDat));

	EdfWriteInfo(&dw, &(const TypeRec_t){ Point2DInf, 0, "EchoChart"}, & writed);
	struct PointXY p = { 0,0 };
	for (size_t i = 0; i < 3000; i++)
	{
		if (dat.Data[i] > 127)
			p.y = (float)UnPow(-1 * (dat.Data[i] - 127), 1.0 / 0.35) / 1000;
		else
			p.y = (float)UnPow(dat.Data[i], 1.0 / 0.35) / 1000;

		p.x = xDiscrete * i * maxDepthMult;

		EdfWriteDataBlock(&dw, &p, sizeof(struct PointXY));
	}
	fclose(f);
	EdfClose(&dw);
	return 0;
}
//-----------------------------------------------------------------------------
int EdfToEcho(const char* edfFile, const char* echoFile)
{
	int err = 0;

	EdfWriter_t br;
	size_t writed = 0;
	if ((err = EdfOpen(&br, edfFile, "rb")))
		return err;

	FILE* f = NULL;
	if ((err = fopen_s(&f, echoFile, "wb")))
		return err;

	double discrete = Discrete3000;
	ECHO_FILE_V2_0 dat = { 0 };
	dat.FileType = 5;
	dat.Id.ResearchType = 1;
	memcpy(dat.FileDescription, FileDescEcho, sizeof(FileDescEcho));
	size_t recN = 0;
	PointXY_t record = { 0 };

	int skip = 0;
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
					if ((err = EdfReadBin(&PositionType, &src, &msDst, &p, &skip)))
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
					if ((err = EdfReadBin(&DeviceInfoType, &src, &msDst, &dvc, &skip)))
						return err;
					dat.Id.DeviceType = (uint16_t)dvc->SwId;
					dat.Id.DeviceNum = (uint32_t)dvc->HwNumber;
				}
				break;
				case REGINFO:
				{
					DeviceInfo_t* dvc = NULL;
					if ((err = EdfReadBin(&DeviceInfoType, &src, &msDst, &dvc, &skip)))
						return err;
					dat.Id.RegType = (uint16_t)dvc->SwId;
					dat.Id.RegNum = (uint32_t)dvc->HwNumber;
				}
				break;
				}//switch
			}//if (br.t->Id)
			else if (IsVarName(br.t, "Oper"))
				dat.Id.Oper = *((uint16_t*)br.Block);
			else if (IsVarName(br.t, "Discrete"))
				discrete = *((double*)br.Block);
			else if (IsVarName(br.t, "Reflections"))
				dat.Reflections = PackReflections(*((uint16_t*)br.Block));
			else if (IsVarName(br.t, "Level"))
				dat.Level = PackLevel(*((double*)br.Block), discrete);
			else if (IsVarName(br.t, "Pressure"))
				dat.Pressure = (int16_t)round(*((double*)br.Block) * 10);
			else if (IsVarName(br.t, "Table"))
				dat.Table = *((uint16_t*)br.Block);
			else if (IsVarName(br.t, "Speed"))
				dat.Speed = (uint16_t)round(*((float*)br.Block) * 10);
			else if (IsVarName(br.t, "BufPressure"))
				dat.BufPressure = (int16_t)round(*((double*)br.Block) * 10);
			else if (IsVarName(br.t, "LinePressure"))
				dat.LinePressure = (int16_t)round(*((double*)br.Block) * 10);
			else if (IsVarName(br.t, "Current"))
				dat.Current = *((uint16_t*)br.Block);
			else if (IsVarName(br.t, "IdleHour"))
				dat.IdleHour = *((uint8_t*)br.Block);
			else if (IsVarName(br.t, "IdleMin"))
				dat.IdleMin = *((uint8_t*)br.Block);
			else if (IsVarName(br.t, "Acc"))
				dat.Acc = (int16_t)round(*((float*)br.Block) * 10);
			else if (IsVarName(br.t, "Temp"))
				dat.Temp = (int16_t)round(*((float*)br.Block) * 10);

			else if (IsVarName(br.t, "EchoChart"))
			{
				PointXY_t* s = NULL;
				while (!(err = EdfReadBin(&Point2DInf, &src, &msDst, &s, &skip))
					&& recN <= FIELD_ITEMS_COUNT(ECHO_FILE_V2_0, Data))
				{
					dat.Data[recN] = (int8_t)round(pow(fabs(s->y * 1000), 0.35));
					if (0 > s->y)
						dat.Data[recN] += 127;
					recN++;
					record = *s;
					s = NULL;
					skip = 0;
					msDst.WPos = 0;
				}
				skip = -skip;
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

	dat.crc = MbCrc16(&dat, sizeof(ECHO_FILE_V2_0) - 2);

	if (1 != fwrite(&dat, sizeof(ECHO_FILE_V2_0), 1, f))
		return ERR_FWRITE;

	fclose(f);
	EdfClose(&br);
	return 0;
}