using System;
using System.Globalization;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
Console.WriteLine(DateTimeFormatInfo.InvariantInfo.GetAbbreviatedMonthName(1));
