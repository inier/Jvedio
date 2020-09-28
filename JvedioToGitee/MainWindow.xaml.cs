using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Documents;
using System.Diagnostics;

namespace JvedioToGitee
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string path = Path.Combine( AppDomain.CurrentDomain.BaseDirectory , @"Public\File\");
            if (Directory.Exists(path))
            {
                try
                {
                    List<string> fileswithMD5 = new List<string>();
                    var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
                    if (files != null)
                    {
                        foreach (var item in files)
                        {
                            if (File.Exists(item))
                            {
                                FileInfo fileInfo = new FileInfo(item);
                                    fileswithMD5.Add(fileInfo.FullName.Replace(path, "") + " " + GetMD5(item));
                            }
                        }
                    }

                    string total = "";
                    fileswithMD5.ForEach(arg => { total += arg + "\n"; });
                    using (var listfile = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + @"public\list",false))
                    {
                        listfile.Write(total);
                    }

                    opTextBox.AppendText("------------------------\n");
                    opTextBox.AppendText("成功生成校验码！\n");

                    //生成 版本说明

                    using(StreamWriter sw=new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + @"public\Version", false,Encoding.UTF8))
                    {
                        TextRange textRange = new TextRange(contentTextBox.Document.ContentStart, contentTextBox.Document.ContentEnd);
                        sw.Write(textRange.Text);
                    }

                    opTextBox.AppendText("------------------------\n");
                    opTextBox.AppendText("成功生成 Version\n");


                }
                catch(Exception ex)
                {
                    opTextBox.AppendText(ex.Message+ "\n");
                }

                opTextBox.ScrollToEnd();





            }
        }
        public string GetMD5(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
                }
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            opTextBox.AppendText("开始删除\n");

            string basePath = AppDomain.CurrentDomain.BaseDirectory + "public\\file\\";
            if (Directory.Exists(basePath))
            {
                DirectoryInfo folder = new DirectoryInfo(basePath);
                FileInfo[] fileList = folder.GetFiles();
                foreach (FileInfo file in fileList)
                {
                    if (file.Extension == ".xml" | file.Extension == ".ini" | file.Extension == ".application" | file.Extension == ".xml" | file.Extension == ".txt")
                    {
                        try
                        {
                            file.Delete();
                            opTextBox.AppendText($"删除文件 {file}\n");
                        }
                        catch(Exception ex)
                        {
                            opTextBox.AppendText(ex.Message + "\n");
                            continue;
                        }

                    }
                }

                if (File.Exists(basePath + "AI.sqlite")) { File.Delete(basePath + "AI.sqlite"); opTextBox.AppendText($"删除文件 AI.sqlite\n"); }
                if (File.Exists(basePath + "Info.sqlite")) { File.Delete(basePath + "Info.sqlite"); opTextBox.AppendText($"删除文件 Info.sqlite\n"); }
                if (File.Exists(basePath + "Translate.sqlite")) { File.Delete(basePath + "Translate.sqlite"); opTextBox.AppendText($"删除文件 Translate.sqlite\n"); }

                if(Directory.Exists(basePath + "app.publish")) { Directory.Delete(basePath + "app.publish", true); opTextBox.AppendText($"删除目录 app.publish\n"); }
                if (Directory.Exists(basePath + "BackUp")) { Directory.Delete(basePath + "BackUp", true); opTextBox.AppendText($"删除目录 BackUp\n"); }
                if (Directory.Exists(basePath + "DataBase")) { Directory.Delete(basePath + "DataBase", true); opTextBox.AppendText($"删除目录 DataBase\n"); }
                if (Directory.Exists(basePath + "log")) { Directory.Delete(basePath + "log", true); opTextBox.AppendText($"删除目录 log\n"); }
                if (Directory.Exists(basePath + "Pic")) { Directory.Delete(basePath + "Pic", true); opTextBox.AppendText($"删除目录 Pic\n"); }

                opTextBox.ScrollToEnd();

            }



        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "public";

            string[] command = new string[] {
                @"cd /d """ + path + @"""",
                "git -T git@gitee.com"
            };

            using (Process pc = new Process())
            {
                pc.StartInfo.FileName = "cmd.exe";
                pc.StartInfo.CreateNoWindow = true;//隐藏窗口运行
                pc.StartInfo.RedirectStandardError = true;//重定向错误流
                pc.StartInfo.RedirectStandardInput = true;//重定向输入流
                pc.StartInfo.RedirectStandardOutput = true;//重定向输出流
                pc.StartInfo.UseShellExecute = false;
                pc.Start();
                foreach (string com in command)
                {
                    pc.StandardInput.WriteLine(com);//输入CMD命令
                }
                pc.StandardInput.WriteLine("exit");//结束执行，很重要的
                pc.StandardInput.AutoFlush = true;

                string outPut = pc.StandardOutput.ReadToEnd();//读取结果        
                opTextBox.AppendText(outPut + "\n");


                pc.WaitForExit();
                pc.Close();
            }

        }

    }




}
