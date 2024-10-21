using System.Net;
using System.Net.Sockets;
using System.Text;


var port = 27001;
var ep = new IPEndPoint(IPAddress.Loopback, 27001);

var listener = new TcpListener(ep);

try
{
    listener.Start();

    while (true)
    {
        var client = listener.AcceptTcpClient();

        _ = Task.Run(() =>
        {
            var networkStream = client.GetStream();
            var remoteEp = client.Client.RemoteEndPoint as IPEndPoint;
            var directoryPath = Path.Combine(Environment.CurrentDirectory, remoteEp!.Address.ToString());

            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);
            var buffer = new byte[4];
            var fileNameLength = networkStream.Read(buffer, 0, buffer.Length);
            var fileNameBuffer = new byte[fileNameLength];
            var fileNameBytes = networkStream.Read(fileNameBuffer, 0, fileNameLength);
            var fileName = Encoding.UTF8.GetString(fileNameBuffer);


            var path = Path.Combine(directoryPath, $"{DateTime.Now:dd.MM.yyyy.HH.mm.ss}.{Path.GetExtension(fileName)}");


            using (var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write))
            {
                int len = 0;
                var bytes = new byte[fileNameLength];

                while ((len = networkStream.Read(bytes, 0, bytes.Length)) > 0)
                {
                    fs.Write(bytes, 0, len);
                }
            };

            Console.WriteLine("File recieved");
            client.Close();
        });
    }
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}

