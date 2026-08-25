using System.Globalization;
using Cysharp.Net.Http;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

using var handler = new YetAnotherHttpHandler();
using var client = new HttpClient(handler);
using var response = await client.GetAsync(args[0]);
var body = await response.Content.ReadAsStringAsync();

Console.WriteLine($"yaha status={(int)response.StatusCode}");
Console.Write($"yaha body={body}");
Console.WriteLine("yaha callback=complete");
