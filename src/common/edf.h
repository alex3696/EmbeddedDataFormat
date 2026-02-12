#ifndef EDF_H
#define EDF_H
//-----------------------------------------------------------------------------
#ifdef __cplusplus
extern "C" {
#endif
//-----------------------------------------------------------------------------
#pragma pack(push,1)
#include "EdfWriter.h"
#pragma pack(pop)
//-----------------------------------------------------------------------------
// mode 
// "wb" - Write Binary file
// "wt" - Write Text file
// "ab" - Append existing Binary file
// "at" - Append existing Text file
// "rb" - Read Binary file
// "rt" - Read Text file
// Открыть поток для чтения (до)записи, поток может быть файловым или память
int EdfOpenStream(EdfWriter_t* w, Stream_t* stream, const char* mode);
// Открыть файл для чтения (до)записи, внутри обращается к EdfOpenStream
int EdfOpen(EdfWriter_t* w, const char* file, const char* mode);
// освобождает фнутренние буферы и закрывает файли или поток, 
int EdfClose(EdfWriter_t* dw);
// запись заголовка и конфигурации
int EdfWriteHeader(EdfWriter_t* dw, const EdfHeader_t* h, size_t* writed);
// запись схемы данных
int EdfWriteInfo(EdfWriter_t* dw, const TypeRec_t* t, size_t* writed);
// запись данных
int EdfWriteDataBlock(EdfWriter_t* dw, const void* src, size_t srcLen);
// закрывает и скидывает текущий блок на диск  
int EdfFlushDataBlock(EdfWriter_t* dw, size_t* writed);
// Чтение данных используя схему 
int EdfReadBin(const TypeInfo_t* t, MemStream_t* src, MemStream_t* mem, 
	void** presult, int* skip);
// чтение блока
int EdfReadBlock(EdfWriter_t* dr);

//shortcut: запись схемы + данных
int EdfWriteInfRecData(EdfWriter_t* dw, const TypeRec_t* ir, const void* d, size_t len);
int EdfWriteInfData0(EdfWriter_t* dw, PoType pt, uint32_t id, char* name, char* desc, const void* d);
int EdfWriteInfData(EdfWriter_t* dw, uint32_t id, PoType, char* name, const void* data);
int EdfWriteInfDataString(EdfWriter_t* dw, uint32_t id, char* name, void* data, size_t maxLen);
//-----------------------------------------------------------------------------
#ifdef __cplusplus
}
#endif
//-----------------------------------------------------------------------------
#endif //EDF_H