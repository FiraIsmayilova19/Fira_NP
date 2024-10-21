using System.Net.Sockets;
using System.Net;
using System.Drawing;

//var ip = IPAddress.Parse("192.168.1.76");
var port = 27001;

var ep = new IPEndPoint(IPAddress.Loopback, 27001);
Console.ReadKey();
try
{
    while (true)
    {
        _ = Task.Run(() => {
            using(var client=new TcpClient())
            {
                client.Connect(ep);
                if (client.Connected) 
                {
                    using (var ms = new MemoryStream())
                    {
                        Bitmap memoryImage;
                        memoryImage = new Bitmap(1920, 1080);
                        Size s = new Size(memoryImage.Width, memoryImage.Height);

                        Graphics memoryGraphics = Graphics.FromImage(memoryImage);

                        memoryGraphics.CopyFromScreen(0, 0, 0, 0, s);

                        memoryImage.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        var buffer = ms.ToArray();
                        var networkStream = client.GetStream();
                        networkStream.Write(buffer, 0, buffer.Length);

                        networkStream.Flush();
                    }
                    Console.WriteLine("gonderildi");
                    client.Close();
                }
                else
                {
                    Console.WriteLine("qosulmaq olmadi");
                }
            }
        });
        await Task.Delay(10000);
    }
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}
