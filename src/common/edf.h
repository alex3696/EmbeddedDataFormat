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
int EdfOpenWithFs(EdfWriter_t* w, const char* file, const char* mode, FileStreamOpenFn fnOpen);
int EdfOpen(EdfWriter_t* w, const char* file, const char* mode);
// освобождает фнутренние буферы и закрывает файли или поток, 
int EdfClose(EdfWriter_t* dw);
// запись конфигурации
int EdfWriteConfig(EdfWriter_t* dw, const EdfConfig_t* h, size_t* writed);
// запись схемы данных
int EdfWriteSchema(EdfWriter_t* dw, const EdfSchema_t* t, size_t* writed);
// запись данных
int EdfWriteData(EdfWriter_t* dw, const void* src, size_t srcLen);
// закрывает и скидывает текущий блок на диск  
int EdfFlushData(EdfWriter_t* dw, size_t* writed);
// Чтение данных используя схему 
int EdfReadBin(const EdfType_t* t, MemStream_t* src, MemStream_t* mem, void** presult,
	size_t* resultPrimOffset, size_t* primReaded);
// чтение блока
int EdfReadBlock(EdfWriter_t* dr);

//shortcut: запись схемы + данных
int EdfWriteSchemaData(EdfWriter_t* dw, const EdfSchema_t* ir, const void* d, size_t len);
int EdfWritePrimSchData(EdfWriter_t* dw, PoType pt, uint16_t schId, char* schName, char* schDesc, const void* d);
//-----------------------------------------------------------------------------
#ifdef __cplusplus
}
#endif
//-----------------------------------------------------------------------------
#endif //EDF_H
