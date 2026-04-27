#include "_pch.h"
//-----------------------------------------------------------------------------
#ifdef LOG_ERRF
static char errBuf[128];
void Log_ErrF(const char* const fmt, ...)
{
	va_list arglist;
	va_start(arglist, fmt);
	vsnprintf(errBuf, sizeof(errBuf), fmt, arglist);
	perror(errBuf);
	va_end(arglist);
}
#endif 
//-----------------------------------------------------------------------------
size_t strnlength(const char* s, size_t n)
{
	const char* found = memchr(s, '\0', n);
	return found ? (size_t)(found - s) : n;
}
//-----------------------------------------------------------------------------
size_t minStack = (size_t)INTPTR_MAX;
size_t maxStack = (size_t)INTPTR_MIN;

int CallStackSize() 
{
	int var = 0;
	size_t esp = (size_t)(&var);
	var++;

	minStack = MIN(minStack, esp);
	maxStack = MAX(maxStack, esp);

	printf("CURR=%zd MIN=%zd MAX=%zd DIFF=%zd\n", esp, minStack, maxStack, maxStack - minStack);
	return 0;
}
