using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
namespace FireVolumeLimit
{
    
    class Program
    {
        static string TargetDevice = "192.168.178.114:5555";

        static string Adb(string args)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.UseShellExecute = false;
            startInfo.FileName = "adb";
            startInfo.Arguments = args;
            Process process = new Process();
            process.StartInfo = startInfo;

            var r = new List<string>();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.OutputDataReceived += (sender,data) => r.Add( data.Data);
            process.ErrorDataReceived += (sender, data) => r.Add(data.Data);
            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();
            return String.Join("\r\n", r);
        }

        static bool IsConnected()
        {
            return Adb("devices").IndexOf(TargetDevice) >= 0;
        }
        static void Connect()
        {
            if (!IsConnected())
                Adb("connect " + TargetDevice);
        }

        static void VolumeDown()
        {
            Connect();
            Adb("shell input keyevent KEYCODE_VOLUME_DOWN");
        }

        static float GetVolume()
        {
            Connect();

            var r = Adb("shell dumpsys audio").Replace("\r","").Split('\n');
            var Stream = "";
            var Muted = false;
            var Min = -1;
            var Max = -1;
            var Current = new Dictionary<string, int>();
            var Devices = "";
            for(var i = 0; Devices=="" && i < r.Length; i++)
            {
                if (r[i].Trim() == "- STREAM_SYSTEM:")
                    Stream = "System";

                if (Stream == "System")
                {
                    {
                        var m = new Regex(@"^\s*Muted\:\s+(false|true)\s*$").Match(r[i]);
                        if (m.Success)
                            Muted = m.Groups[1].Value == "true";
                    }
                    {
                        var m = new Regex(@"^\s*Min\:\s+([0-9]+)\s*$").Match(r[i]);
                        if (m.Success)
                            Min = int.Parse(m.Groups[1].Value);
                    }
                    {
                        var m = new Regex(@"^\s*Max\:\s+([0-9]+)\s*$").Match(r[i]);
                        if (m.Success)
                            Max = int.Parse(m.Groups[1].Value);
                    }
                    {
                        var m = new Regex(@"^\s*Devices\:\s+([^\s]+)\s*$").Match(r[i]);
                        if (m.Success)
                            Devices = m.Groups[1].Value;
                    }
                    {
                        var m = new Regex(@"^\s*Current\:\s+(.*?)\s*$").Match(r[i]);
                        if (m.Success)
                        {
                            var AllCurrent = m.Groups[1].Value;
                            var allM = new Regex(@"[0-9]+\s+\((.*?)\)\:\s+([0-9]+)").Matches(AllCurrent);
                            for(var d=0;d<allM.Count;d++)
                            {
                                try
                                {
                                    Current.Add(allM[d].Groups[1].Value, int.Parse(allM[d].Groups[2].Value.Trim()));
                                }
                                catch (Exception) { }
                            }
                        }
                        
                    }

                }

            }

            if(Devices!="" && Current.ContainsKey(Devices))
            {
                if (Muted)
                    return 0;

                var relMax = Max - Min;
                var relVal=Current[Devices] - Min;
                return (float)relVal / (float)relMax;
            }

            return -1;

        }
        static void Main(string[] args)
        {
            var TargetVolume = 0.5;
            if (args.Length > 0)
                TargetVolume = float.Parse(args[0]);
            if (args.Length > 1)
                TargetDevice = args[1];

            while (true)
            {
                var volume = GetVolume();
                Console.Write("\r" + volume);
                if (volume > 0.5)
                {
                    Console.WriteLine(" "+DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    VolumeDown();
                }
            }
            Thread.Sleep(5000);
        }
    }
}
