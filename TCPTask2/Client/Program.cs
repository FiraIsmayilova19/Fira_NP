using System.Net;
using System.Net.Sockets;
using System.Text;


var port = 27001;

var ep = new IPEndPoint(IPAddress.Loopback, 27001);

var client = new TcpClient();
try
{
    client.Connect(ep);
    if (client.Connected)
    {
        var networkStream = client.GetStream();
        Console.WriteLine("enter path: ");
        var path = Console.ReadLine();
        FileInfo file = new FileInfo(path);
        var fileLengthBytes = Encoding.UTF8.GetBytes(file.Length.ToString());
        networkStream.Write(fileLengthBytes, 0, fileLengthBytes.Length);
        var fileNameBytes = Encoding.UTF8.GetBytes(file.Name);
        networkStream.Write(fileNameBytes, 0, fileNameBytes.Length);


        using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
        {
            var buffer = new byte[fileLengthBytes.Length];
            var len = 0;


            while ((len = fs.Read(buffer, 0, buffer.Length)) > 0)
            {
                networkStream.Write(buffer, 0, len);
            }

            networkStream.Close();
            client.Close();
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}
