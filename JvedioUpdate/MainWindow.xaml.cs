using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace JvedioUpdate
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {

        public Point WindowPoint = new Point(100, 100);
        public Size WindowSize = new Size(500, 500);
        public int WinState = 1;

        static string basepath = AppDomain.CurrentDomain.BaseDirectory;
        string temppath = basepath + "Temp";

        Dictionary<string, string> filemd5 = new Dictionary<string, string>();

        public List<string> downloadlist = new List<string>();


        public MainWindow()
        {
            InitializeComponent();


            ProgressBar.Visibility = Visibility.Hidden;
            progressText.Visibility = Visibility.Hidden;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                CheckUpdate();
            });
        }



        

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            progressText.Visibility = Visibility.Hidden;
            ProgressBar.Visibility = Visibility.Hidden;

            Task.Run(() => {
                CheckUpdate();
            });
            
        }


        private async void CheckUpdate()
        {

            this.Dispatcher.Invoke((Action)delegate () { CheckUpdateButton.IsEnabled = false; statusText.Text = "开始检查更新"; });
            //检查版本
            bool IsToUpdate = false;
            string url_version = "http://hitchao.gitee.io/jvedioupdate/Version";
            string contentversion; int statusCodeversion;
            (contentversion, statusCodeversion) = await HttpGet(url_version,ContentType: "text/html; charset=UTF8");

            //写入 Version 文件
            using(StreamWriter sr=new StreamWriter(AppDomain.CurrentDomain.BaseDirectory +  "NewVersion",false))
            {
                sr.Write(contentversion);
            }


            string localversion = "4.0.0.0";
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory  + "OldVersion"))
                using (var localfile = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + "OldVersion")) 
                { 
                    
                    localversion =Regex.Match( localfile.ReadToEnd(),@"\d\.\d\.\d\.\d").Value; 
                }
            this.Dispatcher.Invoke((Action)delegate () { localversionTextBlock.Text = $"当前版本：{localversion}"; });

            string remoteversion = "4.0.0.0";
            string remoteinfo = "";
            if (contentversion != "")
            {
                
                remoteversion = contentversion.Split('\n')[0];
                remoteinfo = contentversion.Replace(remoteversion + "\n", "");
                if (localversion.CompareTo(remoteinfo) < 0) { IsToUpdate = true; } else { IsToUpdate = false; }
                this.Dispatcher.Invoke((Action)delegate () { remoteversionTextBlock.Text = $"检测到新版本：{remoteversion}"; UpdateTextBox.Text = contentversion; });

                //校验文件一致性
                string url_list = "http://hitchao.gitee.io/jvedioupdate/list";
                string content; int statusCode;
                (content, statusCode) = await HttpGet(url_list);
                if (content != "")
                {
                    this.Dispatcher.Invoke((Action)delegate () { statusText.Text = "文件一致性校验"; });
                    filemd5.Clear();
                    foreach (var item in content.Split('\n'))
                    {
                        if (!string.IsNullOrEmpty(item))
                        {
                            string[] info = item.Split(' ');
                            if (!filemd5.ContainsKey(info[0])) filemd5.Add(info[0], info[1]);
                        }
                    }
                    List<string> filenamelist = filemd5.Keys.ToList();

                    downloadlist.Clear();
                    filenamelist.ForEach(arg => {
                        
                        string localfilepath = Path.Combine(basepath, arg);
                        if (File.Exists(localfilepath))
                        {
                            //存在 => 校验
                            if (GetMD5(localfilepath) != filemd5[arg])
                            {
                                downloadlist.Add(arg);//md5 不一致 ，下载
                            }
                        }
                        else
                        {
                            downloadlist.Add(arg); //不存在 =>下载
                        }
                    });

                    if (downloadlist.Count > 0 )
                    {
                        if (IsToUpdate)
                        {
                            this.Dispatcher.Invoke((Action)delegate () { UpdateButton.IsEnabled = true; statusText.Text = "检查到新版本"; UpdateButton.IsEnabled = true; });
                            //显示更新内容
                            
                        }

                        else
                        {
                            this.Dispatcher.Invoke((Action)delegate () { statusText.Text = "已是最新版，但检测到部分文件变动，点击更新以覆盖"; UpdateButton.IsEnabled = true; });
                        }
                            
                    }
                    else
                    {
                        this.Dispatcher.Invoke((Action)delegate () { UpdateButton.IsEnabled = false; statusText.Text = "已是最新版"; }); 
                    }
                }
                else
                {
                    this.Dispatcher.Invoke((Action)delegate () { statusText.Text = "文件一致性校验失败，请重试"; UpdateButton.IsEnabled = false; });
                    
                }


            }
            else
            {
                this.Dispatcher.Invoke((Action)delegate () { statusText.Text = "无法连接到服务器"; UpdateButton.IsEnabled = false; });
            }

            this.Dispatcher.Invoke((Action)delegate () { CheckUpdateButton.IsEnabled = true; });

        }

        public void OpenUrl(object sender ,RoutedEventArgs e )
        {
            Hyperlink hyperlink = sender as Hyperlink;
            Process.Start(hyperlink.NavigateUri.ToString());
        }

        private async void DownLoadFromGitee()
        {
            this.Dispatcher.Invoke((Action)delegate () {
                ProgressBar.Visibility = Visibility.Visible;
                progressText.Visibility = Visibility.Visible;
                UpdateButton.IsEnabled = false;
                CheckUpdateButton.IsEnabled = false;
            });

            await Task.Delay(1);

            //开始下载
            double progressvalue = 0;
            double progressmaximum = downloadlist.Count;
            //新建临时文件夹
            if (!Directory.Exists(temppath)) Directory.CreateDirectory(temppath);
            downloadlist.ForEach(arg => {
                Console.WriteLine(arg);
                //检查是否已经下载
                string filepath = Path.Combine(temppath, arg);
                if (!File.Exists(filepath))
                {
                    DownLoadFile(temppath, arg);
                    Task.Delay(300).Wait();
                }

                progressvalue++;
                //更新进度
                this.Dispatcher.Invoke((Action)delegate () {
                    ProgressBar.Value = ProgressBar.Maximum * (progressvalue / progressmaximum);
                    int p = (int)(ProgressBar.Value) ;
                    progressText.Text = $"{p} %";
                });


            });

            //复制文件并覆盖
            this.Dispatcher.Invoke((Action)delegate () { statusText.Text = "复制文件"; });

            try
            {
                downloadlist.ForEach(arg => { File.Copy(Path.Combine(temppath, arg), Path.Combine(basepath, arg), true); });
            }
            catch(Exception e) { Console.WriteLine(e.Message); }


            //删除 Temp 文件夹
            try
            {
                if (Directory.Exists(temppath)) Directory.Delete(temppath, true);
            }
            catch { }

            //写入Version文件


            
            this.Dispatcher.Invoke((Action)delegate () { statusText.Text = "更新完成！"; });

            this.Dispatcher.Invoke((Action)delegate () {
                UpdateButton.IsEnabled = false;
                CheckUpdateButton.IsEnabled = true;
            });

            try
            {
                Process.Start(AppDomain.CurrentDomain.BaseDirectory + "Jvedio.exe");
            }
            catch { }
            finally
            {
                this.Dispatcher.Invoke((Action)delegate () { this.Close(); });

            }
           
        }

    private void Button_Click_1(object sender, RoutedEventArgs e)
        {

            //检查 jvedio 是否在运行
            Process[] pname = Process.GetProcessesByName("Jvedio");
            if (pname.Length != 0){
                MessageBox.Show("Jvedio正在运行，请关闭后重试!", "Jvedio正在运行");
                return;
            }

            InfoStackPanel.Visibility = Visibility.Collapsed;
            ProgressStackPanel.Visibility = Visibility.Visible;

            Task.Run(() => { DownLoadFromGitee(); });

            }



        public void DownLoadFile(string temppath,string filename)
        {
            this.Dispatcher.Invoke((Action)delegate () { statusText.Text = $"下载 {filename}"; });
            byte[] filebyte = GetFile($"http://hitchao.gitee.io/jvedioupdate/File/{filename}");
            try
            {
                using (var fs = new FileStream(Path.Combine(temppath, filename), FileMode.Create, FileAccess.Write))
                {
                    fs.Write(filebyte, 0, filebyte.Length);
                }
            }
            catch {  }
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


        public byte[] GetFile(string URL, int TryNum = 2, WebProxy Proxy = null, string host = "", string setCookie = "")
        {
            byte[] imagebyte = null;
            string cookies = setCookie;
            int num = 0;
            while (num < TryNum & imagebyte == null)
            {
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                HttpWebRequest Request;
                var Response = default(HttpWebResponse);
                try
                {
                    Request = (HttpWebRequest)HttpWebRequest.Create(URL);
                    if (host != "") Request.Host = host;
                    if (setCookie != "") Request.Headers.Add("Cookie", setCookie);
                    Request.Accept = "*/*";
                    Request.Timeout = 5000;
                    Request.Method = "GET";
                    Request.KeepAlive = false;
                    Request.Referer = URL;
                    Request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/69.0.3497.100 Safari/537.36";
                    Request.ReadWriteTimeout = 5000;
                    if (Proxy != null) Request.Proxy = Proxy;
                    Response = (HttpWebResponse)Request.GetResponse();

                    switch (Response.StatusCode)
                    {
                        case HttpStatusCode.OK:
                            MemoryStream ms = new MemoryStream();
                            Response.GetResponseStream().CopyTo(ms);
                            imagebyte = ms.ToArray();
                            break;

                        default:
                            num = 2;
                            break;

                    }
                    Response.Close();
                }
                catch (WebException e)
                {
                    if (e.Status == WebExceptionStatus.Timeout) { num += 1; } else { num = 2; }
                }
                catch 
                {
                    num = 2;
                }
                finally
                {
                    if (Response is object)
                        Response.Close();
                }
            }
            return imagebyte;
        }



        public async Task<(string, int)> HttpGet(string URL, int TryNum = 2, WebProxy Proxy = null,string ContentType="")
        {
            string HtmlText = "";
            int statusCode = 404;
            int num = 0;
            string result = "";
            while (num < TryNum & string.IsNullOrEmpty(HtmlText))
            {
                HtmlText = await Task.Run(() =>
                {

                    HttpWebRequest Request;
                    var Response = default(HttpWebResponse);
                    try
                    {
                        Request = (HttpWebRequest)HttpWebRequest.Create(URL);
                        Request.Accept = "*/*";
                        Request.Timeout = 3000;
                        Request.Method = "GET";
                        Request.KeepAlive = false;
                        Request.Referer = URL;
                        Request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/69.0.3497.100 Safari/537.36";
                        Request.ReadWriteTimeout = 3000;
                        if (Proxy != null) Request.Proxy = Proxy;
                        if (ContentType != "") Request.ContentType = ContentType;
                        Response = (HttpWebResponse)Request.GetResponse();

                        switch (Response.StatusCode)
                        {
                            case HttpStatusCode.OK:
                                var SR = new StreamReader(Response.GetResponseStream());
                                result = SR.ReadToEnd();
                                SR.Close();
                                statusCode = 200;
                                break;

                            default:
                                num = 2;
                                break;

                        }
                        Response.Close();
                    }
                    catch (WebException e)
                    {
                        if (e.Status == WebExceptionStatus.Timeout)
                        {
                            num += 1;    // 超时
                        }
                        else
                        {
                            num = 2;    //404
                            statusCode = 404;
                        }
                    }
                    catch (Exception e)
                    {
                        num = 2;
                    }
                    finally
                    {
                        if (Response is object)
                            Response.Close();
                    }

                    return result;
                });
            }

            return (HtmlText, statusCode);
        }


        public void CloseWindow(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public void MinWindow(object sender, MouseButtonEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }


        private void MoveWindow(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) this?.DragMove();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            Process.Start("https://hitchao.gitee.io/jvediowebpage/");
        }
    }
}
