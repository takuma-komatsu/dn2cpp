using System;
using System.Globalization;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
#if FULL_DAY
Console.WriteLine(DateTimeFormatInfo.InvariantInfo.GetDayName((DayOfWeek)1));
#else
Console.WriteLine(DateTimeFormatInfo.InvariantInfo.GetAbbreviatedDayName((DayOfWeek)1));
#endif
