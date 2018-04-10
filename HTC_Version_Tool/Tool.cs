using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace HTC_Version_Tool
{
    public class Tool
    {
        public class ADBResult
        {
            public StringReader stdout { get; set; }

            public StringReader stderr { get; set; }
        }

        public static bool SU { get; private set; }

        public enum ADBMode
        {
            None,
            Normal,
            Recovery
        }

        private static ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "adb.exe",
            CreateNoWindow = true,
            UseShellExecute = false,
            WorkingDirectory = Environment.CurrentDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        private static ADBResult ADB(string args)
        {
            var result = new ADBResult();
            using (Process process = new Process())
            {
                process.StartInfo = psi;
                process.StartInfo.Arguments = args;
                process.Start();
                result.stdout = new StringReader(process.StandardOutput.ReadToEnd());
                result.stderr = new StringReader(process.StandardError.ReadToEnd());
                process.WaitForExit(30000);
            }
            return result;
        }

        private static ADBResult ADBShell(string args, bool su = false)
        {
            var result = new ADBResult();
            using (Process process = new Process())
            {
                process.StartInfo = psi;
                process.StartInfo.Arguments = $"shell {(su ? "su -c " : "")}\"{args}\"";
                process.Start();
                result.stdout = new StringReader(process.StandardOutput.ReadToEnd());
                result.stderr = new StringReader(process.StandardError.ReadToEnd());
                process.WaitForExit(30000);
            }
            return result;
        }

        public static ADBMode GetMode
        {
            get
            {
                var result = ADB("devices");
                var sr = result.stdout;
                string text;
                while ((text = sr.ReadLine()) != null)
                {
                    if (text.Contains("\tdevice"))
                    {
                        return ADBMode.Normal;
                    }
                    if (text.Contains("\trecovery"))
                    {
                        return ADBMode.Recovery;
                    }
                }
                return ADBMode.None;
            }
        }

        public static bool CheckPermission()
        {
            var result = ADBShell("whoami");
            var text = result.stdout.ReadLine();
            var root = text.Contains("root");
            result = ADBShell("su -v");
            text = result.stderr.ReadLine();
            SU = (text == null ? true : !text.Contains("not found"));
            if (root)
            {
                SU = false;
            }
            return (SU | root);
        }

        public static string GetMiscLocation()
        {
            var result = ADBShell("ls -l /dev/block/bootdevice/by-name | grep misc", SU);
            var text = result.stdout.ReadLine();
            if (text == null)
            {
                return null;
            }
            if (text.Contains("misc"))
            {
                text = text.Substring(text.IndexOf("/dev/"));
                return text;
            }
            return null;
        }

        public static void DumpMiscPartition(string misc)
        {
            ADBShell($"dd if={misc} of=/sdcard/misc bs=16384 count=1", SU);
            ADB("pull /sdcard/misc");
            ADBShell("rm /sdcard/misc", SU);
        }

        public static int GetVersionOffset()
        {
            var regex = new Regex("^\\d\\.\\d+\\.\\d+\\.\\d+$");
            var buff = File.ReadAllBytes("misc");
            var data = new byte[16];
            for (int i = 0; i < buff.Length - 16; i++)
            {
                Buffer.BlockCopy(buff, i, data, 0, 16);
                string text = Encoding.UTF8.GetString(data).TrimEnd('\0');
                if (regex.IsMatch(text))
                {
                    return i;
                }
            }
            return -1;
        }

        public static string GetVersionString(int offset)
        {
            var buff = File.ReadAllBytes("misc");
            var data = new byte[16];
            Buffer.BlockCopy(buff, offset, data, 0, 16);
            File.Delete("misc");
            return Encoding.UTF8.GetString(data).TrimEnd('\0');
        }

        public static string CheckVersion(string version)
        {
            var regex = new Regex("^\\d\\.\\d+\\.\\d+\\.\\d+$");
            var valid = regex.IsMatch(version);
            if (!valid)
            {
                return "1.00.000.0";
            }
            else
            {
                return version;
            }
        }

        public static void EraseVersion(string misc, int offset)
        {
            ADBShell($"dd if=/dev/zero of={misc} bs=1 seek={offset} count=16", SU);
        }

        public static void WriteVersion(string misc, int offset, string version)
        {
            ADBShell($"echo -ne '{version}' > /sdcard/tmp");
            ADBShell($"dd if=/sdcard/tmp of={misc} bs=1 seek={offset} count={version.Length}", SU);
            ADBShell("rm /sdcard/tmp");
        }

        public static string ReadVersion(string misc, int offset)
        {
            var result = ADBShell($"dd if={misc} bs=1 skip={offset} count=16", SU);
            var version = result.stdout.ReadLine().Substring(0, 16);
            return version;
        }
    }
}
