using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

using var listener = new TcpListener(IPAddress.Loopback, 0);
listener.Start();
var port = ((IPEndPoint)listener.LocalEndpoint).Port;
var portFile = args[0];
File.WriteAllText(portFile + ".part", port.ToString(CultureInfo.InvariantCulture));
File.Move(portFile + ".part", portFile, true);

var terminator = "\r\n\r\n"u8.ToArray();
for (var request = 0; request < 2; request++)
{
    using var client = await listener.AcceptTcpClientAsync();
    await using var stream = client.GetStream();
    var received = new List<byte>();
    var buffer = new byte[1024];
    while (received.Count < terminator.Length ||
           !received.GetRange(received.Count - terminator.Length, terminator.Length)
               .SequenceEqual(terminator))
    {
        var count = await stream.ReadAsync(buffer);
        if (count == 0 || received.Count + count > 16 * 1024)
        {
            throw new InvalidDataException("invalid HTTP request");
        }
        received.AddRange(buffer.AsSpan(0, count).ToArray());
    }

    var response = Encoding.ASCII.GetBytes(
        "HTTP/1.1 200 OK\r\nContent-Length: 8\r\nConnection: close\r\n\r\nyaha-ok\n");
    await stream.WriteAsync(response);
}
