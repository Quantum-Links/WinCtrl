using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

using Microsoft.Win32;


namespace WinCtrl
{
    public partial class MainForm : Form
    {
        byte[] success = Encoding.UTF8.GetBytes("success");
        public MainForm()
        {
            InitializeComponent();
            contextMenu.ItemClicked += ContextMenu_ItemClicked;
            UdpClient udpClient = new UdpClient(10001);
            AsyncReceive(udpClient);
            SetStartup(true);
        }
        async void AsyncReceive(UdpClient udpClient)
        {
            while (true)
            {
                try
                {
                    var buffer = await udpClient.ReceiveAsync();
                    var str = Encoding.UTF8.GetString(buffer.Buffer);
                    if (str.Substring(0, 6) == "volume")
                    {
                        int volume = int.Parse(str.Substring(6));
                        SetMasterVolume(volume);
                        continue;
                    }
                    switch (str)
                    {
                        case "shutdown":
                            StartCMD("-s");
                            break;
                        case "restart":
                            StartCMD("-r");
                            break;
                    }
                    _ = udpClient.SendAsync(success, success.Length, buffer.RemoteEndPoint);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        public void SetMasterVolume(int volume)
        {
            Audio.Volume = volume;
        }
        void StartCMD(string order)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "shutdown",
                Arguments = $"{order} -t 0",
                CreateNoWindow = true,
                UseShellExecute = false
            };
            Process.Start(psi);
        }
        private void ContextMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            Application.Exit();
        }
        private void SetStartup(bool enable)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                if (enable)
                {
                    // 设置开机自启
                    key.SetValue(Application.ProductName, Application.ExecutablePath);
                }
                else
                {
                    // 取消开机自启
                    key.DeleteValue(Application.ProductName, false);
                }
            }
        }
    }
}
[Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IAudioEndpointVolume
{
    int _0(); int _1(); int _2(); int _3();
    int SetMasterVolumeLevelScalar(float fLevel, Guid pguidEventContext);
    int _5();
    int GetMasterVolumeLevelScalar(out float pfLevel);
    int _7(); int _8(); int _9(); int _10(); int _11(); int _12();
}

[Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IMMDevice
{
    int Activate(ref System.Guid id, int clsCtx, int activationParams, out IAudioEndpointVolume aev);
}

[Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IMMDeviceEnumerator
{
    int _0();
    int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice endpoint);
}

[ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")] class MMDeviceEnumeratorComObject { }
public class Audio
{
    private static readonly IAudioEndpointVolume _MMVolume;

    static Audio()
    {
        var enumerator = new MMDeviceEnumeratorComObject() as IMMDeviceEnumerator;
        enumerator.GetDefaultAudioEndpoint(0, 1, out IMMDevice dev);
        var aevGuid = typeof(IAudioEndpointVolume).GUID;
        dev.Activate(ref aevGuid, 1, 0, out _MMVolume);
    }

    public static int Volume
    {
        get
        {
            _MMVolume.GetMasterVolumeLevelScalar(out float level);
            return (int)(level * 100);
        }
        set
        {
            _MMVolume.SetMasterVolumeLevelScalar((float)value / 100, default);
        }
    }
}