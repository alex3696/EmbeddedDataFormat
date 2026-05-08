#ifndef SIAMFILEFORMAT_H
#define SIAMFILEFORMAT_H

#include "edf.h"
//-----------------------------------------------------------------------------
#pragma pack(push,1)
typedef struct
{
	uint32_t Time;			// время измерения от начала дня, мс
	int32_t Press;			// давление, 0.001 атм
	int32_t Temp;			// температура, 0.001 °С
	uint16_t Vbat;			// напряжение батареи,
	uint16_t crc;			// CRC16
} OMEGA_DATA_V1_1;
//-----------------------------------------------------------------------------
typedef struct
{
	uint16_t Shop;
	uint16_t Field;
	char Cluster[6];
	char Well[6];
	uint16_t PlaceId;
	int32_t Depth;
} FILES_RESEARCH_ID_V1_0;
//-----------------------------------------------------------------------------
typedef struct
{
	uint32_t FileType;
	char FileDescription[40];
	uint8_t Year;
	uint8_t Month;
	uint8_t Day;
	uint8_t NotUsed;
	FILES_RESEARCH_ID_V1_0 Id;
	uint16_t RegType;
	uint16_t RegNum;
	uint16_t RegVer;
	uint16_t SensType;
	uint32_t SensNum;
	uint16_t SensVer;
	uint16_t crc;
} SPSK_FILE_V1_1;
//-----------------------------------------------------------------------------
typedef struct
{
	uint8_t Hour;
	uint8_t Min;
	uint8_t Sec;
	uint8_t Day;
	uint8_t Month;
	uint8_t Year;
} TIME;
//-----------------------------------------------------------------------------
typedef struct
{
	uint16_t ResearchType;			//тип исследования
	uint16_t DeviceType;			//тип датчика
	uint32_t DeviceNum;				//номер датчика
	uint16_t Shop;					//номер цеха
	uint16_t Oper;					//номер оператора
	uint16_t Field;					//код месторождения
	char Cluster[6];				//номер куста
	char Well[6];					//номер скважины
	TIME Time;						//время начала исследования
	uint16_t RegType;				//тип регистратора (new)
	uint32_t RegNum;				//номер регистратора (new)
} RESEARCH_ID_V2_0;
//-----------------------------------------------------------------------------
typedef struct
{
	uint32_t FileType;					//тип файла
	char FileDescription[40];			//описание файла
	RESEARCH_ID_V2_0 Id;				//идентификаторы исследования (add)
	uint16_t Reflections;				//число отражений
	uint16_t Level;						//уровень без поправки на скорость звука (для скорости 341.333 м/с), м
	int16_t Pressure;					//затрубное давление, 0.1 атм
	uint16_t Table;						//номер таблицы скоростей
	uint16_t Speed;						//скорость звука, 0.1 м/с
	int16_t BufPressure;				//буферное давление, 0.1 атм (new)
	int16_t LinePressure;				//линейное давление, 0.1 атм (new)
	uint16_t Current;					//ток, 0.1А (new)
	uint8_t IdleHour;					//время простоя, ч (new)
	uint8_t IdleMin;					//время простоя, мин (new)
	uint16_t Mode;						//режим исследования (new)
	uint16_t Acc;						//напряжение аккумулятора датчика, 0.1В (new)
	int16_t Temp;						//температура датчика, 0.1С (new)
	uint8_t Data[3000];					//данные эхограммы
	uint16_t crc;						//crc16
} ECHO_FILE_V2_0;
//-----------------------------------------------------------------------------
typedef struct
{
	uint32_t FileType;					//тип файла
	char FileDescription[40];			//описание файла
	RESEARCH_ID_V2_0 Id;				//идентификаторы исследования (add)
	uint16_t Rod;						//диаметр штока, 0.1 мм
	uint16_t Aperture;					//номер отверстия
	uint16_t MaxWeight;					//максимальная нагрузка, дискрет (изм)
	uint16_t MinWeight;					//минимальная нагрузка, дискрет (изм)
	uint16_t TopWeight;					//вес штанг вверху, дискрет (изм)
	uint16_t BotWeight;					//вес штанг внизу, дискрет (изм)
	uint16_t Travel;					//ход штока, дискрет
	uint16_t BeginPos;					//положение штока перед первым измерением, дискрет
	uint16_t TravelStep;				//величина дискреты перемещения, 0.1 мм
	uint16_t Period;					//период качаний, дискрет
	uint16_t TimeStep;					//величина дискреты времени, мс
	uint16_t Cycles;					//пропущено циклов
	uint16_t LoadStep;					//величина дискреты нагрузки, кг (new)
	int16_t Pressure;					//затрубное давление, 0.1 атм (new)
	int16_t BufPressure;				//буферное давление, 0.1 атм (new)
	int16_t LinePressure;				//линейное давление, 0.1 атм (new)
	uint16_t PumpType;					//тип привода станка-качалки (new)
	uint16_t Acc;						//напряжение аккумулятора датчика, 0.1В (new)
	int16_t Temp;						//температура датчика, 0.1С (new)
	uint16_t Data[1000];				//данные динамограммы
	uint16_t crc;						//crc16
} DYN_FILE_V2_0;

//-----------------------------------------------------------------------------
// standard schemas
// 
typedef enum
{
	FILETYPEID = 10,
	BEGINDATETIME,
	POSITION,
	DEVICEINFO,
	REGINFO,

	OMEGADATA
} StdSchemaEnum;

static const char FileDescDyn[] = "SIAM COMPLEX DYNAMOGRAM V2.0";
static const char FileDescEcho[] = "SIAM COMPLEX ECHOGRAM V2.0";
static const char FileDescMt[] = "OMEGA SAMT DATA V1.1";

//-----------------------------------------------------------------------------
static const EdfType_t FileDescriptionType =
{
	.Type = Char, .Name = "FileDescription",
	.Dims = { 1, (uint32_t[]) { FIELD_SIZEOF(DYN_FILE_V2_0, FileDescription) } }
};
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
static const EdfType_t FileTypeIdType =
{
	.Type = Struct, .Name = "FileTypeId", .Dims = { 0, NULL }, .Childs =
	{
		.Count = 2,
		.Item = (EdfType_t[])
		{
			{ Int16, "Type" },
			{ Int16, "Version" },
		}
	}
};
//-----------------------------------------------------------------------------
typedef struct
{
	uint16_t Type;
	uint16_t Version;
} FileTypeId_t;
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
static const EdfType_t DateTimeType =
{
	.Type = Struct, .Name = "DateTime", .Dims = { 0, NULL }, .Childs =
	{
		.Count = 8,
		.Item = (EdfType_t[])
		{
			{ Int16, "Year" },
			{ UInt8, "Month" },
			{ UInt8, "Day" },
			{ UInt8, "Hour" },
			{ UInt8, "Min" },
			{ UInt8, "Sec" },
			{ UInt16, "mSec" },
			{ Int8, "Tz" },
		}
	}
};

typedef struct
{
	int16_t Year;
	uint8_t Month;
	uint8_t Day;
	uint8_t Hour;
	uint8_t Min;
	uint8_t Sec;
	uint16_t mSec;
	int8_t Tz;
} DateTime_t;
//-----------------------------------------------------------------------------
static const EdfType_t DeviceInfoType =
{
	.Type = Struct, .Name = "DeviceInfo", .Dims = { 0, NULL }, .Childs =
	{
		.Count = 6,
		.Item = (EdfType_t[])
		{
			{ UInt16, "SwId" },
			{ UInt16, "SwModel" },
			{ UInt16, "SwRevision" },
			{ UInt16, "HwId" },
			{ UInt16, "HwModel" },
			{ UInt64, "HwNumber" },
		}
	}
};

typedef struct
{
	uint16_t SwId;
	uint16_t SwModel;
	uint16_t SwRevision;
	uint16_t HwId;
	uint16_t HwModel;
	uint64_t HwNumber;
} DeviceInfo_t;

//-----------------------------------------------------------------------------
static const EdfType_t PositionType =
{
	.Type = Struct, .Name = "Position", .Dims = { 0, NULL }, .Childs =
	{
		.Count = 6,
		.Item = (EdfType_t[])
		{
			{ String, "Field" },
			{ String, "Cluster" },
			{ String, "Well" },
			{ String, "Shop" },
			{ Double, "Longitude" },
			{ Double, "Latitude" },
		}
	}
};

typedef struct
{
	char* Field;
	char* Cluster;
	char* Well;
	char* Shop;
	double Longitude;
	double Latitude;
} Position_t;
//-----------------------------------------------------------------------------
static const EdfType_t OmegaDataType =
{
	.Type = Struct, .Name = "OMEGA_DATA_V1_1", .Dims = { 0, NULL }, .Childs =
	{
		.Count = 4,
		.Item = (EdfType_t[])
		{
			{ UInt32, "Time" },
			{ Int32, "Press" },
			{ Int32, "Temp" },
			{ UInt16, "Vbat" },
		}
	}
};
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
#pragma pack(pop)
//-----------------------------------------------------------------------------
#endif
