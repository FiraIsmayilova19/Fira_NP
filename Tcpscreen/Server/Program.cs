using System.Net;
using System.Net.Sockets;

//var ip = IPAddress.Parse("192.168.1.76");
var port = 27001;
var ep = new IPEndPoint(IPAddress.Loopback, 27001);
var listener = new TcpListener(ep);

try
{
    listener.Start();
    Console.WriteLine("Server başladı");

    while (true)
    {
        var client = listener.AcceptTcpClient();

        if (client != null)
        {

            _ = Task.Run(() =>
            {
                var networkstream = client.GetStream();
                var remoteEp = client.Client.RemoteEndPoint as IPEndPoint;


                var directoryPath = Path.Combine(Environment.CurrentDirectory, remoteEp.Address.ToString());
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                var path = Path.Combine(directoryPath, $"{DateTime.Now:dd.MM.yyyy.HH.mm.ss}.png");

                using (var filestream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    int len = 0;
                    var bytes = new byte[1024];


                    while ((len = networkstream.Read(bytes, 0, bytes.Length)) > 0)
                    {
                        filestream.Write(bytes, 0, len);
                    }
                }
            });
        }
        else
        {
            Console.WriteLine("Mushteri qebul edilmedi.");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}
