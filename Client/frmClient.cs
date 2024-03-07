using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Forms;
using RPON;
using RPON.Extensions;
using RPON.Packet;

namespace Client
{
    public partial class frmClient : Form
    {
        private Socket ClientSocket;
        private byte[] lengthBUFF = new byte[4];
        private bool firstImage = false;
        private Bitmap oldImage;
        private int FPS = 0;
        Stopwatch fpsCounter = new Stopwatch();

        public frmClient()
        {
            InitializeComponent();
        }

        private void Connect(bool status)
        {
            firstImage = false;

            btnConnect.Enabled = !status;
            btnDisconnect.Enabled = status;
            txtPort.Enabled = !status;
            txtIP.Enabled = !status;
            try
            {
                if (status)
                {
                    ClientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    ClientSocket.Connect(IPAddress.Parse(txtIP.Text), int.Parse(txtPort.Text));
                    if (ClientSocket.Connected)
                    {
                        // SendScreenToServer();


                        ClientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        ClientSocket.Connect(IPAddress.Parse(txtIP.Text), int.Parse(txtPort.Text));
                        if (ClientSocket.Connected)
                        {
                            Task.Run(() => SendScreenToServer());
                            Task.Run(() => ReceiveScreenFromServer());
                        }
                       
                    }
                }
                else
                {
                    if (ClientSocket != null && ClientSocket.Connected)
                    {
                        ClientSocket.Shutdown(SocketShutdown.Both);
                        ClientSocket.Disconnect(false);
                        ClientSocket = null;
                    }
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Exception: " + ex.Message);
            }
        }

        private void Begin_Receive(IAsyncResult ar)
        {
            if (ClientSocket != null)
            {
                try
                {
                    ClientSocket.EndReceive(ar);
                    int dataLength = BitConverter.ToInt32(lengthBUFF, 0);
                    byte[] screenData = ReceiveData(dataLength);
                    DisplayReceivedScreen(screenData);
                }
                catch (Exception ex)
                {
                    // Handle exceptions
                }

                if (ClientSocket.Connected)
                {
                    ClientSocket.BeginReceive(lengthBUFF, 0, lengthBUFF.Length, SocketFlags.None, Begin_Receive, null);
                }
            }
        }
        private byte[] ReceiveData(int length)
        {
            byte[] buffer = new byte[length];
            int received = 0;

            try
            {
                while (received < length)
                {
                    int receivedNow = ClientSocket.Receive(buffer, received, length - received, SocketFlags.None);
                    if (receivedNow == 0)
                    {
                        throw new Exception("Connection closed unexpectedly.");
                    }
                    received += receivedNow;
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions
            }

            return buffer;
        }

        private void frmClient_Load(object sender, EventArgs e)
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    txtIP.Text = ip.ToString();
                }
            }
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            Connect(true);
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            Connect(false);
        }

        private void lblGithubLink_Click(object sender, EventArgs e)
        {
            Process.Start(lblGithubLink.Text);
        }


        // Inside frmClient class

        private void SendScreenToServer()
        {
            try
            {
                while (ClientSocket.Connected)
                {
                    Bitmap screen = CaptureScreen();
                    byte[] screenData = screen.ToByteArray(ImageFormat.Jpeg); // Convert screen to byte array
                    SendDataToServer(screenData); // Send screen data to server
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions
            }
        }

        private Bitmap CaptureScreen()
        {
            Rectangle bounds = Screen.PrimaryScreen.Bounds; // Capture primary screen
            Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
            }

            return bitmap;
        }

        private void SendDataToServer(byte[] data)
        {
            try
            {
                int dataLength = data.Length;
                byte[] lengthBuffer = BitConverter.GetBytes(dataLength);

                ClientSocket.Send(lengthBuffer); // Send length of data
                ClientSocket.Send(data); // Send actual screen data
            }
            catch (Exception ex)
            {
                // Handle exceptions
            }
        }

        private void DisplayReceivedScreen(byte[] screenData)
        {
            using (MemoryStream ms = new MemoryStream(screenData))
            {
                Image image = Image.FromStream(ms);
                pictureBox1.Image = image;
            }
        }

        private void ReceiveScreenFromServer()
        {
            try
            {
                while (ClientSocket.Connected)
                {
                    byte[] lengthBuffer = new byte[4];
                    ClientSocket.Receive(lengthBuffer);
                    int dataLength = BitConverter.ToInt32(lengthBuffer, 0);

                    byte[] screenData = ReceiveData(dataLength);
                    DisplayReceivedScreen(screenData);
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions
            }
        }
    }

        // Call SendScreenToServer method after connecting to the server

    
}
