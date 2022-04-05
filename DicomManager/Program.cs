using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DicomManager.utility;
using System.Threading;
using System.Runtime.InteropServices;
using System.IO;

namespace DicomManager
{
    class Program
    {
        private static Timer timer;
        private static void StartServer(Object source)
        {
            try
            {
                if (!(ENV.con.State == System.Data.ConnectionState.Open))
                    ENV.con.Open();

                //util.WriteLogs("dsafadsfasdf");
                util.DownloadDb();
                util.ConvertJPG2DCM();
                util.DCMUpload();

            }
            catch (Exception ex)
            {
                util.WriteLogs(ex.Message);
            }
            
        }

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        static void Main(string[] args)
        {

            var handle = GetConsoleWindow();
            ShowWindow(handle, SW_HIDE);

            ENV.SetEnv();

            timer = new Timer(StartServer, null, 0, 20000);
            for (;;)
            {
                // add a sleep for 100 mSec to reduce CPU usage
                Thread.Sleep(100);
            }
        }
    }
}
