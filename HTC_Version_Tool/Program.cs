using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace HTC_Version_Tool
{
    class Program
    {
        static void LOG(string text, int sleep = 0, bool exit = false)
        {
            Console.Write(text);
            if (sleep > 0)
            {
                Thread.Sleep(sleep);
            }
            if (exit)
            {
                Environment.Exit(0);
            }
        }

        static void Main(string[] args)
        {
            LOG("HTC Version Tool, code by sabpprook@xda-developers\r\n\r\n");
            var mode = Tool.GetMode;
            if (mode == Tool.ADBMode.None)
            {
                LOG("No device detected!", 5000, true);
            }
            if (!Tool.CheckPermission())
            {
                LOG("No ROOT or SU permission!", 5000, true);
            }
            var misc = Tool.GetMiscLocation();
            if (misc == null)
            {
                LOG("Can not locate misc partition!", 5000, true);
            }
            LOG($"-- misc: {misc}\r\n");
            Tool.DumpMiscPartition(misc);
            if (!File.Exists("misc"))
            {
                LOG("Can not dump misc partition!", 5000, true);
            }
            var offset = Tool.GetVersionOffset();
            if (offset == -1)
            {
                LOG("Misc offset invalid!", 5000, true);
            }
            var version = Tool.GetVersionString(offset);
            LOG($"-- offset: {offset}\r\n");
            LOG($"-- version: {version}\r\n");
            LOG($"\r\nnew version: ");
            var new_version = Tool.CheckVersion(Console.ReadLine());
            LOG($"\r\nPress ENTER to continue...");
            while (Console.ReadKey().Key != ConsoleKey.Enter) { }
            LOG("\r\n\r\n");
            LOG("-- Erase version...\r\n");
            Tool.EraseVersion(misc, offset);
            LOG($"-- Write version [{new_version}]...\r\n");
            Tool.WriteVersion(misc, offset, new_version);
            new_version = Tool.ReadVersion(misc, offset);
            LOG($"-- current version: {new_version}\r\n");
            LOG("\r\nFinish!\r\n");
            LOG("\r\nFeel free to donate if you get success to downgrading.\r\n");
            LOG("https://www.paypal.me/sabpprook\r\n", 60000, true);
        }
    }
}
