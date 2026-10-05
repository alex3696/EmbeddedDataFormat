#include "_pch.h"
#include "EdfUtils.h"

//-----------------------------------------------------------------------------
size_t strnlength(const char* s, size_t n)
{
	const char* found = memchr(s, '\0', n);
	return found ? (size_t)(found - s) : n;
}
//-----------------------------------------------------------------------------
size_t minStack = (size_t)INTPTR_MAX;
size_t maxStack = (size_t)INTPTR_MIN;

int CallStackSize(void)
{
	int var = 0;
	size_t esp = (size_t)(&var);
	var++;

	minStack = MIN(minStack, esp);
	maxStack = MAX(maxStack, esp);

	printf("CURR=%zd MIN=%zd MAX=%zd DIFF=%zd\n", esp, minStack, maxStack, maxStack - minStack);
	return 0;
}
