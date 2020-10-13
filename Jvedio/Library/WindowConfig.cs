using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static Jvedio.StaticVariable;

namespace Jvedio
{
    public class WindowConfig
    {
        string basepath = AppDomain.CurrentDomain.BaseDirectory;

        private string windowName;

        public string WindowName { get => windowName; set => windowName = value; }

        public WindowConfig(string windowname)
        {
            WindowName = windowname;
        }

        public void Save(Rect rect, JvedioWindowState winstate)
        {
            string path = Path.Combine(basepath, WindowName + ".ini");
            string content = $"{rect.X},{rect.Y},{rect.Width},{rect.Height},{(int)winstate}";
            StreamWriter sw = new StreamWriter(path, false);
            sw.WriteAsync(content);
            sw.Close();
        }


        public (Rect, JvedioWindowState) GetValue()
        {
            Rect rect = new Rect(-1, -1, 10, 10);
            JvedioWindowState winstate = JvedioWindowState.None;
            string path = Path.Combine(basepath, WindowName + ".ini");
            if (!File.Exists(path)) return (rect, winstate);
            StreamReader sr = new StreamReader(path);
            string content = sr.ReadToEnd();
            sr.Close();
            try
            {
                var list = content.Split(',');
                winstate = (JvedioWindowState)int.Parse(list[4]);
                rect = new Rect(double.Parse(list[0]), double.Parse(list[1]), double.Parse(list[2]), double.Parse(list[3]));
            }
            catch(IndexOutOfRangeException ex) {  }
            

            return (rect, winstate);
        }

    }

}
