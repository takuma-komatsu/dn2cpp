using System;
using System.Globalization;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
bool refused = false;
try { Calendar result = DateTimeFormatInfo.InvariantInfo.Calendar; Console.WriteLine(result is null); }
catch (PlatformNotSupportedException error) { refused = error.Message == "DateTimeFormatInfo.Calendar is not supported"; }
Console.WriteLine("dtfi-calendar-minimal:" + refused);
if (!refused) throw new InvalidOperationException("calendar capability boundary");
