using System;
using System.Globalization;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
#if FULL_MONTH
Console.WriteLine(DateTimeFormatInfo.InvariantInfo.GetMonthName(1));
#else
Console.WriteLine(DateTimeFormatInfo.InvariantInfo.GetAbbreviatedMonthName(1));
#endif
