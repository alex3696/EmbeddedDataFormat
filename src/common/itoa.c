#include "itoa.h"

// Таблица пар символов от "00" до "99"
static const char DigitPairs[] =
"0001020304050607080910111213141516171819"
"2021222324252627282930313233343536373839"
"4041424344454647484950515253545556575859"
"6061626364656667686970717273747576777879"
"8081828384858687888990919293949596979899";

// подсчет десятичных цифр для uint64_t через бинарный поиск
static inline size_t digits_count_universal(uint64_t v)
{
	if (v < 10000000000ULL) { // 1 - 10 цифр
		if (v < 100000ULL) { // 1 - 5 цифр
			if (v < 100ULL) return (v < 10ULL) ? 1 : 2;
			if (v < 10000ULL) return (v < 1000ULL) ? 3 : 4;
			return 5;
		}
		else { // 6 - 10 цифр
			if (v < 10000000ULL) return (v < 1000000ULL) ? 6 : 7;
			if (v < 1000000000ULL) return (v < 100000000ULL) ? 8 : 9;
			return 10;
		}
	}
	else { // 11 - 20 цифр
		if (v < 1000000000000005ULL) { // 11 - 15 цифр (защита от переполнения констант на некоторых старых компиляторах)
			if (v < 1000000000000ULL) return (v < 100000000000ULL) ? 11 : 12;
			if (v < 100000000000000ULL) return (v < 10000000000000ULL) ? 13 : 14;
			return 15;
		}
		else { // 16 - 20 цифр
			if (v < 100000000000000000ULL) return (v < 10000000000000000ULL) ? 16 : 17;
			if (v < 10000000000000000000ULL) return (v < 1000000000000000000ULL) ? 18 : 19;
			return 20;
		}
	}
}

static inline size_t internal_utoa(uint64_t uval, int is_negative, char* dst, size_t dstLen)
{
	// 1. Считаем точную длину строки без '\0'
	size_t len = digits_count_universal(uval) + (is_negative ? 1 : 0);
	// Проверяем, поместятся ли данные и '\0' в буфер
	if (len >= dstLen)
	{
		return 0;
	}
	// 2. Ставим указатель в самый конец будущей строки
	char* p = dst + len;
	*p = '\0';
	// 3. Заполняем буфер сразу на месте, двигаясь назад
	while (uval >= 100)
	{
		uint32_t rem = (uint32_t)(uval % 100);
		uval /= 100;
		p -= 2;
		p[0] = DigitPairs[rem * 2];
		p[1] = DigitPairs[rem * 2 + 1];
	}

	// Обработка последнего разряда / пары
	if (uval >= 10)
	{
		p -= 2;
		p[0] = DigitPairs[uval * 2];
		p[1] = DigitPairs[uval * 2 + 1];
	}
	else
	{
		*--p = (char)('0' + uval);
	}
	// Добавляем минус
	if (is_negative)
	{
		*--p = '-';
	}
	return len;
}

// Обёртка для знаковых чисел
size_t Int64ToA(int64_t val, char* dst, size_t dstLen)
{
	int is_negative = 0;
	uint64_t uval;
	if (val < 0) {
		is_negative = 1;
		uval = (val == INT64_MIN) ? (uint64_t)INT64_MAX + 1 : (uint64_t)(-val);
	}
	else {
		uval = (uint64_t)val;
	}
	return internal_utoa(uval, is_negative, dst, dstLen);
}

// Обёртка для беззнаковых чисел
size_t UInt64ToA(uint64_t val, char* dst, size_t dstLen)
{
	return internal_utoa(val, 0, dst, dstLen);
}

//-----------------------------------------------------------------------------
static inline size_t digits_count_32(uint32_t v)
{
	if (v < 100000U) { // 1 - 5 цифр
		if (v < 100U) return (v < 10U) ? 1 : 2;
		if (v < 10000U) return (v < 1000U) ? 3 : 4;
		return 5;
	}
	else { // 6 - 10 цифр
		if (v < 10000000U) return (v < 1000000U) ? 6 : 7;
		if (v < 1000000000U) return (v < 100000000U) ? 8 : 9;
		return 10;
	}
}
static inline size_t internal_utoa32(uint32_t uval, int is_negative, char* dst, size_t dstLen) {
	// 1. Точный расчет длины итоговой строки (максимум 11 символов с минусом)
	size_t len = digits_count_32(uval) + (is_negative ? 1 : 0);
	// Защита от переполнения: проверяем вместимость с учетом '\0'
	if (len >= dstLen)
	{
		return 0;
	}
	// 2. Устанавливаем указатель в конец будущей строки
	char* p = dst + len;
	*p = '\0';

	// 3. Заполняем буфер сразу на месте, двигаясь назад.
	// На Cortex-M3 деление на 100 автоматически оптимизируется компилятором
	// в быстрое аппаратное умножение (через fixed-point обратное число) и сдвиг.
	while (uval >= 100) {
		uint32_t rem = uval % 100;
		uval /= 100;
		p -= 2;
		p[0] = DigitPairs[rem * 2];
		p[1] = DigitPairs[rem * 2 + 1];
	}
	// Обработка оставшихся разрядов
	if (uval >= 10) {
		p -= 2;
		p[0] = DigitPairs[uval * 2];
		p[1] = DigitPairs[uval * 2 + 1];
	}
	else
	{
		*--p = (char)('0' + uval);
	}
	// Добавляем знак минуса
	if (is_negative)
	{
		*--p = '-';
	}
	return len;
}

// Универсальная обёртка для знаковых 32-битных чисел
size_t Int32ToA(int32_t val, char* dst, size_t dstLen) {
	int is_negative = 0;
	uint32_t uval;

	if (val < 0) {
		is_negative = 1;
		// Безопасное приведение INT32_MIN (0x80000000) без UB переполнения знака
		uval = (val == INT32_MIN) ? (uint32_t)INT32_MAX + 1 : (uint32_t)(-val);
	}
	else {
		uval = (uint32_t)val;
	}

	return internal_utoa32(uval, is_negative, dst, dstLen);
}

// Универсальная обёртка для беззнаковых 32-битных чисел
size_t UInt32ToA(uint32_t val, char* dst, size_t dstLen) {
	return internal_utoa32(val, 0, dst, dstLen);
}
