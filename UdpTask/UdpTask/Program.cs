using System.Net;
using System.Net.Sockets;
using System.Text;


var endPoint = new IPEndPoint(IPAddress.Loopback, 27001);
var rep = new IPEndPoint(IPAddress.Any, 0);
var client = new UdpClient();
try
{
    Console.WriteLine("enter path: ");
 var path =Console.ReadLine();
    FileInfo file = new FileInfo(path);
    var fileNameBytes = Encoding.UTF8.GetBytes(file.Name);
    client.Send(fileNameBytes, fileNameBytes.Length, endPoint);
    var fileLengthBytes = Encoding.UTF8.GetBytes(file.Length.ToString());
    client.Send(fileLengthBytes, fileLengthBytes.Length, endPoint);

    using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
    {
        var bytes = new byte[fileLengthBytes.Length];
        var len = 0;
        while ((len = fs.Read(bytes, 0, bytes.Length)) > 0)
        {
            client.Send(bytes, len, endPoint);
            client.Receive(ref rep);
        }
    }

    Console.WriteLine("File sent");
    Console.ReadKey();
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}
