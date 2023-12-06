using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace Client
{
    public partial class MainWindow : Window
    {
        private const int bufferSize = 8000;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void RequestButton_Click(object sender, RoutedEventArgs e)
        {
            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse("192.168.4.116"), 27001);

            try
            {
                await Task.Run(() =>
                {
                    // Ekranin olcusu
                    Screen screen = Screen.PrimaryScreen;
                    string screenSizeInfo = $"{screen.Bounds.Width},{screen.Bounds.Height}";


                    byte[] sizeBuffer = Encoding.Default.GetBytes(screenSizeInfo);// servere her client ucun ayri ekran olcusu gedir
                    client.SendTo(sizeBuffer, serverEndPoint);


                    byte[] requestBuffer = Encoding.Default.GetBytes("CaptureScreen"); // screenshot ucun sorgu
                    client.SendTo(requestBuffer, serverEndPoint);

                    while (true)
                    {
                        byte[] imageBytes = ReceiveImage(client, serverEndPoint); //shekili tam qebul elemek ucun method
                        using (MemoryStream ms = new MemoryStream(imageBytes))
                        {
                            Bitmap image = new Bitmap(ms);
                            ImageFrame.Dispatcher.Invoke(() => ImageFrame.Source = ToBitmapImage(image)); //gelen byte array stream kimi bitmapa verdikden sonra image cevirmek ucun
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
            finally
            {
                client.Close();
            }
        }

        private byte[] ReceiveImage(Socket client, IPEndPoint serverEndPoint)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                while (true)
                {
                    byte[] dataBuffer = new byte[bufferSize];
                    EndPoint serverResponse = new IPEndPoint(IPAddress.Any, 0);

                    // her defe gelen hisse oxunub streame yazilir
                    int bytesRead = client.ReceiveFrom(dataBuffer, ref serverResponse);
                    ms.Write(dataBuffer, 0, bytesRead);

                    if (bytesRead < bufferSize) //gelen datanin uzunlugu bufferde azdisa sonuncunu oxuyub break edir
                        break;
                }

                return ms.ToArray();
            }
        }

        private BitmapImage ToBitmapImage(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Jpeg);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                return bitmapImage;
            }
        }
    }
}
