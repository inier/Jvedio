using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jvedio
{
    public static class Logger
    {
        private static object NetWorkLock = new object();
        private static object DataBaseLock = new object();
        private static object ExceptionLock = new object();
        private static object ScanLogLock = new object();

        public static void LogE(Exception e)
        {
            Console.WriteLine(e.StackTrace);
            Console.WriteLine(e.Message);
            string path = AppDomain.CurrentDomain.BaseDirectory + "Log";
            if (!Directory.Exists(path)) { Directory.CreateDirectory(path); }
            string filepath = path + "/" + DateTime.Now.ToString("yyyy-MM-dd") + ".log";
            lock(ExceptionLock)
            {
                StreamWriter sr = new StreamWriter(filepath, true);
                string content ;
                content = "\n-----【" + DateTime.Now.ToString() + "】-----";
                content += $"\nMessage=>{ e.Message}";
                content += $"\nStackTrace=>\n{ GetAllFootprints(e)}";
                //content += $"\nInnerException=>{ e.InnerException}";
                //content += $"\nSource=>{ e.Source}";
                //content += $"\nTargetSite=>{ e.TargetSite}";
                //content += $"\nData=>{ e.Data}\n";
                try { sr.Write(content); } catch { }
                sr.Close();
            }
        }


        public static void LogF(Exception e)
        {
            Console.WriteLine(e.StackTrace);
            Console.WriteLine(e.Message);
            string path = AppDomain.CurrentDomain.BaseDirectory + "Log\\File";
            if (!Directory.Exists(path)) { Directory.CreateDirectory(path); }
            string filepath = path + "/" + DateTime.Now.ToString("yyyy-MM-dd") + ".log";
            lock (ExceptionLock)
            {
                StreamWriter sr = new StreamWriter(filepath, true);
                string content;
                content = "\n-----【" + DateTime.Now.ToString() + "】-----";
                content += $"\nMessage=>{ e.Message}";
                content += $"\nStackTrace=>\n{ GetAllFootprints(e)}";
                try { sr.Write(content); } catch { }
                sr.Close();
            }
        }



        public static void LogN(string NetWorkStatus)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "Log\\NetWork";
            if (!Directory.Exists(path)) { Directory.CreateDirectory(path); }
            string filepath = path + "/" + DateTime.Now.ToString("yyyy-MM-dd") + ".log";
            lock (NetWorkLock)
            {
                StreamWriter sr = new StreamWriter(filepath, true);
                string content;
                content = "\n【" + DateTime.Now.ToString() + $"】=>{NetWorkStatus}";
                try { sr.Write(content); } catch { }
                sr.Close();
            }

        }


        public static void LogD(Exception e)
        {
            Console.WriteLine(e.StackTrace);
            Console.WriteLine(e.Message);
            string path = AppDomain.CurrentDomain.BaseDirectory + "Log\\DataBase";
            if (!Directory.Exists(path)) { Directory.CreateDirectory(path); }
            string filepath = path + "/" + DateTime.Now.ToString("yyyy-MM-dd") + ".log";
            lock (DataBaseLock)
            {
                StreamWriter sr = new StreamWriter(filepath, true);
                string content;

                content = "\n-----【" + DateTime.Now.ToString() + "】-----";
                content += $"\nMessage=>{ e.Message}";
                content += $"\nStackTrace=>\n{ GetAllFootprints(e)}";
                //content += $"\nInnerException=>{ e.InnerException}";
                //content += $"\nSource=>{ e.Source}";
                //content += $"\nTargetSite=>{ e.TargetSite}";
                //content += $"\nData=>{ e.Data}\n";
                try { sr.Write(content); } catch { }
                sr.Close();
            }

        }

        public static string GetAllFootprints(Exception x)
        {
            var st = new StackTrace(x, true);
            var frames = st.GetFrames();
            var traceString = new StringBuilder();
            foreach (var frame in new StackTrace(x, true).GetFrames())
            {
                if (frame.GetFileLineNumber() < 1)
                    continue;
                traceString.Append($"    文件: {frame.GetFileName()}" );
                traceString.Append($" 方法: {frame.GetMethod().Name}");
                traceString.Append($" 行数: {frame.GetFileLineNumber()}\n");
            }

            return traceString.ToString();
        }


        public static void LogScanInfo(string content)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "log/scanlog";
            if (!Directory.Exists(path)) { Directory.CreateDirectory(path); }
            string filepath = path + "/" + DateTime.Now.ToString("yyyy-MM-dd") + ".log";
            lock (ScanLogLock)
            {
                StreamWriter sr = new StreamWriter(filepath, true);
                try { sr.Write(content); } catch { }
                sr.Close();
            }
        }




    }
}
