using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Windows.Forms.Integration;
using System.Windows.Controls;

namespace WinCtrl
{
    public partial class MainForm : Form
    {
        private MediaElement mediaElement;
		private readonly byte[] _success = Encoding.UTF8.GetBytes("success");
        public MainForm()
        {
            InitializeComponent();
            contextMenu.ItemClicked += ContextMenu_ItemClicked;
            UdpClient udpClient = new UdpClient(10001);
            AsyncReceive(udpClient);
            SetStartup(true);
			mediaElement = new MediaElement();
			mediaElement.LoadedBehavior = MediaState.Manual;
			elementHost1.Child = mediaElement;
			mediaElement.MediaEnded += (s, e) =>
			{
				mediaElement.Position = TimeSpan.Zero;
				mediaElement.Play();
			};
		}
        private void PlayVideo(string url)
        {
			try
			{
				mediaElement.Source = new Uri(url);
				mediaElement.Play();
				WindowState = FormWindowState.Maximized;
			}
			catch (Exception ex)
			{
				MessageBox.Show($"播放失败：{ex.Message}\n{url}");
				WindowState = FormWindowState.Minimized;
			}
		}
        [DllImport("winmm.dll")]
        public static extern int waveOutGetVolume(IntPtr hwo, out uint dwVolume);
        private async void AsyncReceive(UdpClient udpClient)
		{
			while (true)
			{
				try
				{
					var result = await udpClient.ReceiveAsync();
					var receivedStr = Encoding.UTF8.GetString(result.Buffer, 0, result.Buffer.Length);
					HandleCommand(receivedStr, result.RemoteEndPoint);
					await udpClient.SendAsync(_success, _success.Length, result.RemoteEndPoint);
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.ToString());
				}
			}
		}
        private void HandleCommand(string command, IPEndPoint remoteEndpoint)
        {
            switch (command)
            {
                case "shutdown":
                    StartCmd("-s");
                    break;
                case "restart":
                    StartCmd("-r");
                    break;
                case "volumeup":
                    Audio.Volume = Math.Min(Audio.Volume + 5, 100);
                    break;
                case "volumedown":
                    Audio.Volume = Math.Max(Audio.Volume - 5, 0);
                    break;
                case "play":
                    mediaElement.Play();
                    break;
                case "pause":
                    mediaElement.Pause();
                    break;
                case "hidevideo":
                    WindowState = FormWindowState.Minimized;
					break;
				default:
                    Console.WriteLine($"Unknown command: {command}");
                    break;
            }

            if (command.StartsWith("volume"))
            {
                if (int.TryParse(command.Substring(6), out int volume))
                {
                    Audio.Volume = volume;
                }
                else
                {
                    Console.WriteLine($"Invalid volume value: {command}");
                }
            }
            if (command.StartsWith("playvideo"))
            {
                PlayVideo(command.Substring(9));
            }
			if (command.StartsWith("setvideo"))
			{
				TimeSpan totalDuration = mediaElement.NaturalDuration.TimeSpan;
				var percentage = float.Parse(command.Substring(8));
				mediaElement.Position = new TimeSpan((long)(totalDuration.Ticks * percentage));
			}
		}

		private static void StartCmd(string order)
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
        private static void ContextMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            Application.Exit();
        }
        private void SetStartup(bool enable)
        {
            using (var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                if (enable)
                {
                    key?.SetValue(Application.ProductName, Application.ExecutablePath);
                }
                else
                {
                    key?.DeleteValue(Application.ProductName, false);
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
    private static readonly IAudioEndpointVolume MmVolume;

    static Audio()
    {
        var enumerator = new MMDeviceEnumeratorComObject() as IMMDeviceEnumerator;
        enumerator.GetDefaultAudioEndpoint(0, 1, out IMMDevice dev);
        var aevGuid = typeof(IAudioEndpointVolume).GUID;
        dev.Activate(ref aevGuid, 1, 0, out MmVolume);
    }

    public static int Volume
    {
        get
        {
            MmVolume.GetMasterVolumeLevelScalar(out var level);
            return (int)(level * 100);
        }
        set => MmVolume.SetMasterVolumeLevelScalar((float)value / 100, default);
    }
}