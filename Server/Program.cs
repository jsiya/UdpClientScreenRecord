using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server
{
    internal class Program
    {
        private const int BufferSize = 8000;

        static void Main(string[] args)
        {
            var listener = new UdpClient(27001);
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

            Console.WriteLine("UDP server listening on port 27001");

            while (true)
            {
                byte[] sizeBuffer = listener.Receive(ref remoteEndPoint);
                string screenSizeInfo = Encoding.Default.GetString(sizeBuffer); // clientin ekran olcusunu qebul edir
                string[] screenSize = screenSizeInfo.Split(','); //en, uzunluq

                int screenWidth = int.Parse(screenSize[0]);
                int screenHeight = int.Parse(screenSize[1]);

                byte[] buffer = listener.Receive(ref remoteEndPoint);
                string msg = Encoding.Default.GetString(buffer);

                if (msg == "CaptureScreen")
                {
                    while (true)
                    {
                        using Bitmap memoryImage = new Bitmap(screenWidth, screenHeight);

                        using Graphics memoryGraphics = Graphics.FromImage(memoryImage);
                        memoryGraphics.CopyFromScreen(0, 0, 0, 0, new Size(screenWidth, screenHeight));

                        using MemoryStream ms = new MemoryStream();
                        memoryImage.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);

                        SendImage(listener, remoteEndPoint, ms.ToArray()); //shekili gonderen method
                    }
                }

                Console.WriteLine($"{remoteEndPoint} : {msg}");
            }
        }

        static void SendImage(UdpClient listener, IPEndPoint remoteEndPoint, byte[] imageData)
        {
            int totalChunks = (int)Math.Ceiling((double)imageData.Length / BufferSize); //sekili gondereceyi addim sayi

            for (int i = 0; i < totalChunks; i++)
            {
                int offset = i * BufferSize; // umumi gonderilen byte sayi
                int chunkSize = Math.Min(BufferSize, imageData.Length - offset); //gonderilen byte sayi umumi bufferin olcusunden azdisa qalani gondermek ucun
                byte[] bufferForSending = new byte[chunkSize];

                Array.Copy(imageData, offset, bufferForSending, 0, chunkSize); //o qeder byte yeni buffere kopyalanir

                listener.Send(bufferForSending, bufferForSending.Length, remoteEndPoint); // ve gonderilir
            }

            Console.WriteLine($"Image sent to {remoteEndPoint}");
        }
    }
}
