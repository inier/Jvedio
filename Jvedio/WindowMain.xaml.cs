using DynamicData;
using Jvedio.ViewModel;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static Jvedio.StaticVariable;
using static Jvedio.StaticClass;

namespace Jvedio
{
    /// <summary>
    /// Main.xaml 的交互逻辑
    /// </summary>
    public partial class Main : Window
    {
        public const string UpdateUrl = "http://hitchao.gitee.io/jvedioupdate/Version";
        public const string NoticeUrl = "https://hitchao.gitee.io/jvediowebpage/notice";

        public DispatcherTimer CheckurlTimer = new DispatcherTimer();
        public int CheckurlInterval = 10;//每5分钟检测一次网址


        public Point WindowPoint = new Point(100, 100);
        public Size WindowSize = new Size(1000, 600);
        public JvedioWindowState WinState = JvedioWindowState.Normal;


        public List<Actress> SelectedActress = new List<Actress>();

        public bool IsMouseDown = false;
        public Point MosueDownPoint;


        public CancellationTokenSource RefreshScanCTS;
        public CancellationToken RefreshScanCT;


        public Settings WindowSet = null;
        public VieModel_Main vieModel;
        public WindowSearch windowSearch = null;

        System.Windows.Forms.NotifyIcon notifyIcon = new System.Windows.Forms.NotifyIcon();



        DispatcherTimer FlowTimer = new DispatcherTimer();
        //public int FLowNum = 0;

        DispatcherTimer PopupTimer = new DispatcherTimer();

        public Main()
        {
            InitializeComponent();

            SettingsContextMenu.Placement = PlacementMode.Mouse;

            this.Cursor = Cursors.Wait;
            PopupTimer.Interval = TimeSpan.FromSeconds(2);
            PopupTimer.Tick += new EventHandler(PopupTimer_Tick);
            #region "最小化"
            //最小化

            Stream iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/Jvedio.ico")).Stream;
            notifyIcon.Icon = new System.Drawing.Icon(iconStream);
            notifyIcon.Visible = false;
            notifyIcon.Text = "Jvedio";
            notifyIcon.BalloonTipText = "Jvedio";
            notifyIcon.ShowBalloonTip(500);
            notifyIcon.MouseDown +=
                delegate (object sender, System.Windows.Forms.MouseEventArgs args)
                {
                    if (args.Button == System.Windows.Forms.MouseButtons.Right) { NotifyPopup.IsOpen = true; PopupTimer.Start(); }
                };
            notifyIcon.DoubleClick +=
                delegate (object sender, EventArgs args)
                {
                    notifyIcon.Visible = false;
                    this.Show();
                    this.Opacity = 1;
                    this.WindowState = WindowState.Normal;
                };
            #endregion

            ProgressBar.Visibility = Visibility.Hidden;

            WinState = 0;




            if (Properties.Settings.Default.SortDescending) { SortArrow.Text = "↓"; } else { SortArrow.Text = "↑"; }
            AdjustWindow();





        }

        public void InitMovie()
        {
            vieModel = new VieModel_Main();
            if (Properties.Settings.Default.RandomDisplay)
            {
                vieModel.RandomDisplay();
            }
            else
            {
                vieModel.Reset();
                vieModel.Sort();
                AllRB.IsChecked = true;
            }
            this.DataContext = vieModel;
            vieModel.CurrentMovieListHideOrChanged += (s, ev) => { StopDownLoad(); };
            vieModel.CurrentMovieListChanged += (s, ev) => { SetSelected(); };
            vieModel.FlipOverCompleted += (s, ev) =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    ScrollViewer.ScrollToTop();
                });
            };
            FlowTimer.Interval = TimeSpan.FromSeconds(0.1);
            FlowTimer.Tick += new EventHandler(FlowTimer_Tick);

            CheckurlTimer.Interval = TimeSpan.FromMinutes(CheckurlInterval);
            CheckurlTimer.Tick += new EventHandler(CheckurlTimer_Tick);

            



        }

        public async Task<bool> InitActor()
        {
            vieModel.GetActorList();
            await Task.Delay(1);
            return true;
        }



        public void BeginCheckurlThread()
        {
            Thread threadObject = new Thread(CheckUrl);
            threadObject.Start();
        }



        private void CheckurlTimer_Tick(object sender, EventArgs e)
        {
            BeginCheckurlThread();
        }


        private void FlowTimer_Tick(object sender, EventArgs e)
        {
            //FLowNum++;
            vieModel.FlowNum++;
            Console.WriteLine(vieModel.FlowNum);
            vieModel.Flow();
            FlowTimer.Stop();
        }


        private void PopupTimer_Tick(object sender, EventArgs e)
        {
            NotifyPopup.IsOpen = false;
            PopupTimer.Stop();
        }

        public void Notify_Close(object sender, RoutedEventArgs e)
        {
            NotifyPopup.IsOpen = false;
            notifyIcon.Visible = false; this.Close();
        }

        public void Notify_Show(object sender, RoutedEventArgs e)
        {
            NotifyPopup.IsOpen = false;
            notifyIcon.Visible = false;
            this.Show();
            this.Opacity = 1;
            this.WindowState = WindowState.Normal;
        }

        public void Notify_LostFocus(object sender, RoutedEventArgs e)
        {
            NotifyPopup.IsOpen = false;
        }


        void CheckUpdate()
        {
            LoadingStackPanel.Visibility = Visibility.Visible;

            Task.Run(async () =>
            {
                string content = ""; int statusCode;
                try
                {
                    (content, statusCode) = await Net.Http(UpdateUrl, Proxy: null);
                }
                catch (TimeoutException ex) { Logger.LogN($"URL={UpdateUrl},Message-{ex.Message}"); }

                if (content != "")
                {
                    //检查更新
                    this.Dispatcher.Invoke((Action)delegate ()
                    {
                        LoadingStackPanel.Visibility = Visibility.Hidden;
                        string remote = content.Split('\n')[0];
                        string local = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

                        using (StreamWriter sw = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "OldVersion"))
                        {
                            sw.WriteLine(local + "\n");
                            sw.WriteLine("内容：");
                        }


                        //string local = "3.9.9.9";
                        LocalVersionTextBlock.Text = $"当前版本：{local}";
                        RemoteVersionTextBlock.Text = $"最新版本：{remote}";

                        if (local.CompareTo(remote) < 0) UpdateGrid.Visibility = Visibility.Visible;
                    });
                }
            });
        }


        void ShowNotice()
        {
            Task.Run(async () =>
            {
                string notices = "";
                string path = AppDomain.CurrentDomain.BaseDirectory + "Notice.txt";
                if (File.Exists(path))
                {
                    StreamReader sr = new StreamReader(path);
                    notices = sr.ReadToEnd();
                    sr.Close();
                }
                string content = ""; int statusCode = 404;
                try
                {
                    (content, statusCode) = await Net.Http(NoticeUrl, Proxy: null);
                }
                catch (TimeoutException ex) { Logger.LogN($"URL={NoticeUrl},Message-{ex.Message}"); }
                if (content != "")
                {
                    if (content != notices)
                    {
                        StreamWriter sw = new StreamWriter(path, false);
                        sw.Write(content);
                        sw.Close();
                        this.Dispatcher.Invoke((Action)delegate ()
                        {
                            NoticeTextBlock.Text = content;
                            NoticeGrid.Visibility = Visibility.Visible;
                        });
                    }

                }
            });
        }



        public DownLoader DownLoader;

        public void StartDownload(List<Movie> movieslist)
        {
            List<Movie> movies = new List<Movie>();
            List<Movie> moviesFC2 = new List<Movie>();
            if (movieslist != null)
            {
                foreach (var item in movieslist)
                {
                    if (item.title == "" | item.smallimageurl == "" | item.bigimageurl == "" | item.sourceurl == "" | item.smallimage == null | item.bigimage == null)
                        if (item.id.ToUpper().IndexOf("FC2") >= 0) { moviesFC2.Add(item); } else { movies.Add(item); }
                }
            }

            //添加到下载列表
            DownLoader?.CancelDownload();
            DownLoader = new DownLoader(movies, moviesFC2);
            DownLoader.StartThread();
            double totalcount = moviesFC2.Count + movies.Count;
            Console.WriteLine(totalcount);
            if (totalcount == 0) return;
            //UI更新
            DownLoader.InfoUpdate += (s, e) =>
            {
                InfoUpdateEventArgs eventArgs = e as InfoUpdateEventArgs;
                for (int i = 0; i < vieModel.CurrentMovieList.Count; i++)
                {
                    try
                    {
                        if (vieModel.CurrentMovieList[i]?.id.ToUpper() == eventArgs.Movie.id.ToUpper())
                        {
                            try { Refresh(i, eventArgs, totalcount); }
                            catch (TaskCanceledException ex) { Logger.LogE(ex); }
                            break;
                        }
                    }
                    catch (Exception ex1)
                    {
                        Console.WriteLine(ex1.StackTrace);
                        Console.WriteLine(ex1.Message);
                    }
                }
            };


        }

        public void RefreshCurrentPage(object sender, RoutedEventArgs e)
        {
            CancelSelect();
            vieModel.Refresh();
        }


        public void CancelSelect()
        {
            Properties.Settings.Default.EditMode = false; vieModel.SelectedMovie.Clear(); SetSelected();
        }

        public void SelectAll(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (Properties.Settings.Default.EditMode) { CancelSelect(); return; }
            Properties.Settings.Default.EditMode = true;
            foreach (var item in vieModel.CurrentMovieList)
            {
                if (!vieModel.SelectedMovie.Contains(item))
                {
                    vieModel.SelectedMovie.Add(item);

                }
            }
            SetSelected();


        }

        public async void ScrollToEnd(object sender, RoutedEventArgs e)
        {
            await Task.Run(() =>
            {
                while (ScrollViewer.VerticalOffset < ScrollViewer.ScrollableHeight)
                {
                    this.Dispatcher.Invoke((Action)delegate
                    {
                        ScrollViewer.ScrollToBottom();
                    });
                    Task.Delay(100).Wait();
                }

            });
        }


        private  void Refresh(int i, InfoUpdateEventArgs eventArgs, double totalcount)
        {
            Dispatcher.Invoke((Action) async delegate ()
            {
                ProgressBar.Value = ProgressBar.Maximum * (eventArgs.progress / totalcount); ProgressBar.Visibility = Visibility.Visible;
                if (ProgressBar.Value == ProgressBar.Maximum)
                {
                    DownLoader.State = DownLoadState.Completed; ProgressBar.Visibility = Visibility.Hidden;
                    Console.WriteLine("下载已完成");

                }
                if (DownLoader.State == DownLoadState.Completed | DownLoader.State == DownLoadState.Fail) ProgressBar.Visibility = Visibility.Hidden;

                DataBase cdb = new DataBase();
                Movie movie = await cdb.SelectMovieByID(eventArgs.Movie.id);
                cdb.CloseDB();
                if (Properties.Settings.Default.ShowImageMode == "预览图")
                {

                }
                else
                {
                    movie.smallimage = StaticClass.GetBitmapImage(movie.id, "SmallPic");
                    movie.bigimage = StaticClass.GetBitmapImage(movie.id, "BigPic");
                }

                vieModel.CurrentMovieList[i] = null;
                vieModel.CurrentMovieList[i] = movie;


            });
        }


        public void OpenSubSuctionVedio(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            StackPanel stackPanel = button.Parent as StackPanel;
            TextBlock textBlock = stackPanel.Children.OfType<TextBlock>().Last();
            string filepath = textBlock.Text;
            if (File.Exists(filepath))
            {
                if (File.Exists(filepath)) { Process.Start(filepath); } else { new Msgbox(this, "无法打开 " + filepath).ShowDialog(); }
            }

        }



        private static void OnCreated(object obj, FileSystemEventArgs e)
        {
            //导入数据库

            if (Scan.IsProperMovie(e.FullPath))
            {
                FileInfo fileinfo = new FileInfo(e.FullPath);

                //获取创建日期
                string createDate = "";
                try { createDate = fileinfo.CreationTime.ToString("yyyy-MM-dd HH:mm:ss"); }
                catch { }
                if (createDate == "") createDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                DataBase cdb = new DataBase();
                Movie movie = new Movie()
                {
                    filepath = e.FullPath,
                    id = Identify.GetFanhao(fileinfo.Name),
                    filesize = fileinfo.Length,
                    vediotype = (int)Identify.GetVedioType(Identify.GetFanhao(fileinfo.Name)),
                    scandate = createDate
                };
                if (!string.IsNullOrEmpty(movie.id) & movie.vediotype > 0) { cdb.InsertScanMovie(movie); }
                cdb.CloseDB();
                Console.WriteLine($"成功导入{e.FullPath}");
            }




        }

        private static void OnDeleted(object obj, FileSystemEventArgs e)
        {
            if (Properties.Settings.Default.ListenAllDir & Properties.Settings.Default.DelFromDBIfDel)
            {
                DataBase cdb = new DataBase();
                cdb.DelInfoByType("movie", "filepath", e.FullPath);
                cdb.CloseDB();
            }
            Console.WriteLine("成功删除" + e.FullPath);
        }



        public FileSystemWatcher[] fileSystemWatcher;
        public string failwatcherMessage = "";

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public void AddListen()
        {
            string[] drives = Environment.GetLogicalDrives();
            fileSystemWatcher = new FileSystemWatcher[drives.Count()];
            for (int i = 0; i < drives.Count(); i++)
            {
                try
                {

                    if (drives[i] == @"C:\") { continue; }
                    FileSystemWatcher watcher = new FileSystemWatcher();
                    watcher.Path = drives[i];
                    watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                    watcher.Filter = "*.*";
                    watcher.Created += OnCreated;
                    watcher.Deleted += OnDeleted;
                    watcher.EnableRaisingEvents = true;
                    fileSystemWatcher[i] = watcher;

                }
                catch
                {
                    failwatcherMessage += drives[i] + ",";
                    continue;
                }
            }

            if (failwatcherMessage != "")
                new PopupWindow(this, $"监听{failwatcherMessage}不成功").Show();
        }

        public async void CheckUrl()
        {
            Console.WriteLine("开始检测");
            vieModel.CheckingUrl = true;
            Dictionary<string, bool> result = new Dictionary<string, bool>();

            //获取网址集合

            List<string> urlList = new List<string>();
            urlList.Add(Properties.Settings.Default.Bus);
            urlList.Add(Properties.Settings.Default.BusEurope);
            urlList.Add(Properties.Settings.Default.Library);
            urlList.Add(Properties.Settings.Default.DB);
            urlList.Add(Properties.Settings.Default.Fc2Club);
            urlList.Add(Properties.Settings.Default.Jav321);
            urlList.Add(Properties.Settings.Default.DMM);

            List<bool> enableList = new List<bool>();
            enableList.Add(Properties.Settings.Default.EnableBus);
            enableList.Add(Properties.Settings.Default.EnableBusEu);
            enableList.Add(Properties.Settings.Default.EnableLibrary);
            enableList.Add(Properties.Settings.Default.EnableDB);
            enableList.Add(Properties.Settings.Default.EnableFC2);
            enableList.Add(Properties.Settings.Default.Enable321);
            enableList.Add(Properties.Settings.Default.EnableDMM);

            for (int i = 0; i < urlList.Count; i++)
            {
                bool enable = enableList[i];
                string url = urlList[i];
                if (enable)
                {
                    bool CanConnect = false; bool enablecookie = false; string cookie = "";
                    if (url == Properties.Settings.Default.DB)
                    {
                        enablecookie = true;
                        cookie = Properties.Settings.Default.DBCookie;
                    }
                    try
                    {
                        CanConnect = await Net.TestUrl(url, enablecookie, cookie, "DB");
                    }
                    catch (TimeoutException ex) { Logger.LogN($"URL={url},Message-{ex.Message}"); }

                    if (CanConnect) { if (!result.ContainsKey(url)) result.Add(url, true); } else { if (!result.ContainsKey(url)) result.Add(url, false); }
                }
                else
                   if (!result.ContainsKey(url)) result.Add(url, false);
            }


            this.Dispatcher.Invoke((Action)delegate ()
            {

                if (result[Properties.Settings.Default.Bus]) { BusStatus.Fill = Brushes.Green; }
                if (result[Properties.Settings.Default.DB]) { DBStatus.Fill = Brushes.Green; }
                if (result[Properties.Settings.Default.Library]) { LibraryStatus.Fill = Brushes.Green; }
                if (result[Properties.Settings.Default.Fc2Club]) { FC2Status.Fill = Brushes.Green; }
                if (result[Properties.Settings.Default.BusEurope]) { BusEuropeStatus.Fill = Brushes.Green; }
                if (result[Properties.Settings.Default.Jav321]) { Jav321Status.Fill = Brushes.Green; }
                if (result[Properties.Settings.Default.DMM]) { DMMStatus.Fill = Brushes.Green; }


            });

            bool IsAllConnect = true;
            bool IsOneConnect = false;
            for (int i = 0; i < enableList.Count; i++)
            {
                if (enableList[i])
                {
                    if (!result[urlList[i]])
                        IsAllConnect = false;
                    else
                        IsOneConnect = true;
                }
            }

            this.Dispatcher.Invoke((Action)delegate ()
            {

                if (IsAllConnect)
                    AllStatus.Background = Brushes.Green;
                else if (!IsAllConnect & !IsOneConnect)
                    AllStatus.Background = Brushes.Red;
                else if (IsOneConnect & !IsAllConnect)
                    AllStatus.Background = Brushes.Yellow;

            });
            vieModel.CheckingUrl = false;
        }


        public void AdjustWindow()
        {
            //读取窗体设置
            WindowConfig cj = new WindowConfig(this.GetType().Name);
            Rect rect;
            (rect, WinState) = cj.GetValue();

            if (rect.X != -1 && rect.Y != -1)
            {
                //读到属性值
                if (WinState == JvedioWindowState.FullScreen)
                {
                    this.WindowState = WindowState.Maximized;
                }
                else
                {
                    this.WindowState = WindowState.Normal;
                    this.Left = rect.X > 0 ? rect.X : 0;
                    this.Top = rect.Y > 0 ? rect.Y : 0;
                    this.Height = rect.Height > 100 ? rect.Height : 100;
                    this.Width = rect.Width > 100 ? rect.Width : 100;
                    if (this.Width == SystemParameters.WorkArea.Width | this.Height == SystemParameters.WorkArea.Height) { WinState = JvedioWindowState.Maximized; }
                }
            }
            else
            {
                WinState = 0;
                this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            HideMargin();
        }




        private void Window_Closed(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.CloseToTaskBar & this.IsVisible == true)
            {
                notifyIcon.Visible = true;
                this.Hide();
                WindowSet?.Hide();
            }
            else
            {
                StopDownLoad();
                ProgressBar.Visibility = Visibility.Hidden;
                WindowTools windowTools = null;
                foreach (Window item in App.Current.Windows)
                {
                    if (item.GetType().Name == "WindowTools") windowTools = item as WindowTools;
                }

                if (windowTools?.IsVisible == true)
                {
                }
                else
                {
                    System.Environment.Exit(0);
                }


            }
        }

        public async void FadeOut()
        {
            if (Properties.Settings.Default.EnableWindowFade)
            {
                double opacity = this.Opacity;
                await Task.Run(() =>
                {
                    while (opacity > 0.1)
                    {
                        this.Dispatcher.Invoke((Action)delegate { this.Opacity -= 0.05; opacity = this.Opacity; });
                        Task.Delay(1).Wait();
                    }
                });
                this.Opacity = 0;
            }
            this.Close();
        }


        public void CloseWindow(object sender, MouseButtonEventArgs e)
        {
            FadeOut();
        }

        public async void MinWindow(object sender, MouseButtonEventArgs e)
        {
            if (Properties.Settings.Default.EnableWindowFade)
            {
                double opacity = this.Opacity;
                await Task.Run(() =>
                {
                    while (opacity > 0.2)
                    {
                        this.Dispatcher.Invoke((Action)delegate { this.Opacity -= 0.1; opacity = this.Opacity; });
                        Task.Delay(20).Wait();
                    }
                });
            }

            this.WindowState = WindowState.Minimized;
            this.Opacity = 1;

        }


        public void MaxWindow(object sender, MouseButtonEventArgs e)
        {
            if (WinState == 0)
            {
                //最大化
                WinState = JvedioWindowState.Maximized;
                WindowPoint = new Point(this.Left, this.Top);
                WindowSize = new Size(this.Width, this.Height);
                this.Height = SystemParameters.WorkArea.Height;
                this.Width = SystemParameters.WorkArea.Width;
                this.Left = SystemParameters.WorkArea.Left;
                this.Top = SystemParameters.WorkArea.Top;

            }
            else
            {
                WinState = JvedioWindowState.Normal;
                this.Left = WindowPoint.X;
                this.Top = WindowPoint.Y;
                this.Width = WindowSize.Width;
                this.Height = WindowSize.Height;
            }
            this.WindowState = WindowState.Normal;
            this.OnLocationChanged(EventArgs.Empty);
            HideMargin();
        }

        private void HideMargin()
        {
            if (WinState == JvedioWindowState.Normal)
            {
                MainGrid.Margin = new Thickness(2);
                this.ResizeMode = ResizeMode.CanResizeWithGrip;
            }
            else if (WinState == JvedioWindowState.Maximized || this.WindowState == WindowState.Maximized)
            {
                MainGrid.Margin = new Thickness(0);
                this.ResizeMode = ResizeMode.NoResize;
            }
        }

        public void FullScreen(object sender, MouseButtonEventArgs e)
        {
            if (WinState == JvedioWindowState.FullScreen)
            {
                WinState = JvedioWindowState.Normal;
                this.WindowState = WindowState.Normal;
                this.Left = WindowPoint.X;
                this.Top = WindowPoint.Y;
                this.Width = WindowSize.Width;
                this.Height = WindowSize.Height;
            }
            else if (WinState == JvedioWindowState.Normal)
            {
                WinState = JvedioWindowState.FullScreen;
                WindowPoint = new Point(this.Left, this.Top);
                WindowSize = new Size(this.Width, this.Height);
                this.WindowState = WindowState.Maximized;
            }
            else
            {
                WinState = JvedioWindowState.FullScreen;
                this.WindowState = WindowState.Maximized;
            }
            this.OnLocationChanged(EventArgs.Empty);
            this.OnStateChanged(EventArgs.Empty);
            HideMargin();
        }


        private void MoveWindow(object sender, MouseEventArgs e)
        {
            //移动窗口
            if (e.LeftButton == MouseButtonState.Pressed && WinState == JvedioWindowState.Normal)
            {
                this.DragMove();
            }
        }

        WindowTools WindowTools;

        private void OpenTools(object sender, RoutedEventArgs e)
        {
            //SettingsPopup.IsOpen = false;
            if (WindowTools != null) { WindowTools.Close(); }
            WindowTools = new WindowTools();
            WindowTools.Show();

        }


        private void OpenDataBase(object sender, RoutedEventArgs e)
        {
            Window_DBManagement window_DBManagement = Jvedio.GetWindow.Get("Window_DBManagement") as Window_DBManagement;
            if (window_DBManagement == null) window_DBManagement = new Window_DBManagement();


            window_DBManagement.Show();
            

        }


        private void OpenUrl(object sender, RoutedEventArgs e)
        {
            Hyperlink hyperlink = sender as Hyperlink;
            Process.Start(hyperlink.NavigateUri.ToString());
        }

        private void OpenFeedBack(object sender, RoutedEventArgs e)
        {
            Process.Start("https://docs.qq.com/sheet/DRmJ3aVNvU3dMbUhp");
        }

        private void OpenHelp(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.kancloud.cn/hitchao/jvedio/content/Jvedio.md");
        }

        private void OpenThanks(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.kancloud.cn/hitchao/jvedio/1921337");
        }

        private void OpenJvedioWebPage(object sender, RoutedEventArgs e)
        {
            Process.Start("https://hitchao.gitee.io/jvediowebpage/");
        }


        private void HideGrid(object sender, MouseButtonEventArgs e)
        {
            Grid grid = ((Border)sender).Parent as Grid;
            grid.Visibility = Visibility.Hidden;

        }

        private void ShowAbout(object sender, RoutedEventArgs e)
        {
            AboutGrid.Visibility = Visibility.Visible;
            VersionTextBlock.Text = $"版本：{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()}";
        }

        private void ShowThanks(object sender, RoutedEventArgs e)
        {
            ThanksGrid.Visibility = Visibility.Visible;
        }

        private void ShowUpdate(object sender, MouseButtonEventArgs e)
        {
            UpdateGrid.Visibility = Visibility.Visible;
        }





        private void OpenSet_MouseDown(object sender, RoutedEventArgs e)
        {
            //SettingsPopup.IsOpen = false;
            if (WindowSet != null) { WindowSet.Close(); }
            WindowSet = new Settings();
            WindowSet.Show();

        }



        public void SearchContent(object sender, MouseButtonEventArgs e)
        {
            Grid grid = ((Canvas)(sender)).Parent as Grid;
            TextBox SearchTextBox = grid.Children.OfType<TextBox>().First() as TextBox;
            if (grid.Name == "AllSearchGrid") { vieModel.SearchAll = true; } else { vieModel.SearchAll = false; }
            vieModel.Search = SearchTextBox.Text;
        }

        private void scrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta / 4);
            e.Handled = true;
        }


        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            //if ( vieModel.SearchAll) AllSearchPopup.IsOpen = false;
            //if (SearchCandidate != null & !vieModel.SearchAll) SearchCandidate.Visibility = Visibility.Hidden;
        }

        private void SetSearchValue(object sender, MouseButtonEventArgs e)
        {
            if (vieModel.SearchAll)
                AllSearchTextBox.Text = ((Label)sender).Content.ToString();
            else
                SearchTextBox.Text = ((Label)sender).Content.ToString();
        }

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            Grid grid = tb.Parent as Grid;
            if (grid.Name == "AllSearchGrid") { vieModel.SearchAll = true; } else { vieModel.SearchAll = false; }


            //if ( vieModel.SearchAll) AllSearchPopup.IsOpen = true;
            //if (SearchCandidate != null & !vieModel.SearchAll) SearchCandidate.Visibility = Visibility.Visible;
        }



        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!Properties.Settings.Default.SearchImmediately & AllSearchTextBox.Text != "") return;


            TextBox SearchTextBox = sender as TextBox;
            Grid grid = SearchTextBox.Parent as Grid;
            if (SearchTextBox != null)
            {
                string searchtext = SearchTextBox.Text;
                if (grid.Name == "AllSearchGrid") { vieModel.SearchAll = true; } else { vieModel.SearchAll = false; }
                vieModel.Search = searchtext;
            }
        }

        private void SearchTextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TextBox SearchTextBox = sender as TextBox;
                Grid grid = SearchTextBox.Parent as Grid;
                if (SearchTextBox != null)
                {
                    string searchtext = SearchTextBox.Text;
                    vieModel.Search = searchtext;
                }
            }
        }




        private void ShowMovieGrid(object sender, RoutedEventArgs e)
        {
            Grid_GAL.Visibility = Visibility.Hidden;
            Grid_Movie.Visibility = Visibility.Visible;
            ActorInfoGrid.Visibility = Visibility.Collapsed;
            BeginScanStackPanel.Visibility = Visibility.Hidden;

        }


        public  void ShowGenreGrid(object sender, RoutedEventArgs e)
        {
                Grid_Movie.Visibility = Visibility.Hidden;
                Grid_GAL.Visibility = Visibility.Visible;
                Grid_Genre.Visibility = Visibility.Visible;
                Grid_Actor.Visibility = Visibility.Hidden;
                Grid_Label.Visibility = Visibility.Hidden;
            this.vieModel.ClickGridType = 0;
            ActorToolsStackPanel.Visibility = Visibility.Hidden;

        }

        private void ShowActorGrid(object sender, RoutedEventArgs e)
        {
            Grid_Movie.Visibility = Visibility.Hidden;
            Grid_GAL.Visibility = Visibility.Visible;
            Grid_Genre.Visibility = Visibility.Hidden;
            Grid_Actor.Visibility = Visibility.Visible;
            Grid_Label.Visibility = Visibility.Hidden;
            this.vieModel.ClickGridType = 1;
            ActorToolsStackPanel.Visibility = Visibility.Visible;
        }

        private void ShowLabelGrid(object sender, RoutedEventArgs e)
        {
            Grid_Movie.Visibility = Visibility.Hidden;
            Grid_GAL.Visibility = Visibility.Visible;
            Grid_Genre.Visibility = Visibility.Hidden;
            Grid_Actor.Visibility = Visibility.Hidden;
            Grid_Label.Visibility = Visibility.Visible;
            this.vieModel.ClickGridType = 2;
            ActorToolsStackPanel.Visibility = Visibility.Hidden;
        }

        private void ShowLabelEditGrid(object sender, RoutedEventArgs e)
        {
            //LabelEditGrid.Visibility = Visibility.Visible;
        }

        public void Tag_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Label label = (Label)sender;
            string tag = label.Content.ToString();
            vieModel = new VieModel_Main();
            vieModel.GetMoviebyTag(tag);
            this.DataContext = vieModel;
            ShowMovieGrid(sender, new RoutedEventArgs());
        }

        public void Studio_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Label label = (Label)sender;
            string genre = label.Content.ToString();
            vieModel = new VieModel_Main();
            vieModel.GetMoviebyStudio(genre);
            this.DataContext = vieModel;
            ShowMovieGrid(sender, new RoutedEventArgs());
        }

        public void Director_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Label label = (Label)sender;
            string genre = label.Content.ToString();
            vieModel = new VieModel_Main();
            vieModel.GetMoviebyDirector(genre);
            this.DataContext = vieModel;
            ShowMovieGrid(sender, new RoutedEventArgs());
        }

        public void Genre_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Label label = (Label)sender;
            string genre = label.Content.ToString().Split('(')[0];
            vieModel = new VieModel_Main();
            vieModel.GetMoviebyGenre(genre);
            this.DataContext = vieModel;
            ShowMovieGrid(sender, new RoutedEventArgs());
            vieModel.TextType = genre;
        }

        public void ShowActorMovieFromDetailWindow(Actress actress)
        {
            vieModel = new VieModel_Main();
            vieModel.GetMoviebyActress(actress);
            vieModel.Actress = actress;
            this.DataContext = vieModel;
            ShowMovieGrid(this, new RoutedEventArgs());
            ActorInfoGrid.Visibility = Visibility.Visible;
        }

        public void ActorCheckBox_Click(object sender, RoutedEventArgs e)
        {
            SelectedActress.Clear();
            ActorSetSelected();
        }

        public void ActorSetSelected()
        {
            for (int i = 0; i < ActorItemsControl.Items.Count; i++)
            {
                ContentPresenter c = (ContentPresenter)ActorItemsControl.ItemContainerGenerator.ContainerFromItem(ActorItemsControl.Items[i]);
                WrapPanel wrapPanel = FindElementByName<WrapPanel>(c, "ActorWrapPanel");
                if (wrapPanel != null)
                {
                    Border border = wrapPanel.Children[0] as Border;
                    TextBox textBox = c.ContentTemplate.FindName("ActorNameTextBox", c) as TextBox;
                    if (textBox != null)
                    {
                        border.Background = (SolidColorBrush)Application.Current.Resources["BackgroundSide"];
                        foreach (Actress actress in SelectedActress)
                        {
                            if (actress.name == textBox.Text.Split('(')[0])
                            {
                                border.Background = Brushes.LightGreen; break;
                            }
                        }
                    }
                }
            }

        }


        public void BorderMouseEnter(object sender, RoutedEventArgs e)
        {
            if (Properties.Settings.Default.EditMode)
            {
                Border border = sender as Border;
                border.BorderBrush = (SolidColorBrush)Application.Current.Resources["Selected_BorderBrush"];
            }

        }

        public void BorderMouseLeave(object sender, RoutedEventArgs e)
        {
            if (Properties.Settings.Default.EditMode)
            {
                Border border = sender as Border;
                border.BorderBrush = Brushes.Transparent;
            }
        }



        public void ActorBorderMouseEnter(object sender, RoutedEventArgs e)
        {
            if (!Properties.Settings.Default.ActorEditMode)
            {
                Border border = sender as Border;
                border.Background = (SolidColorBrush)Application.Current.Resources["BackgroundTitle"];
            }

        }

        public void ActorBorderMouseLeave(object sender, RoutedEventArgs e)
        {
            if (!Properties.Settings.Default.ActorEditMode)
            {
                Border border = sender as Border;
                border.Background = (SolidColorBrush)Application.Current.Resources["BackgroundSide"];
            }
        }

        public void ShowSameActor(object sender, MouseButtonEventArgs e)
        {
            Border border = sender as Border;
            StackPanel sp = border.Child as StackPanel;
            TextBox textBox = sp.Children.OfType<TextBox>().First();
            string name = textBox.Text.Split('(')[0];


            if (Properties.Settings.Default.ActorEditMode)
            {
                foreach (Actress actress in vieModel.ActorList)
                {
                    if (actress.name == name)
                    {

                        if (SelectedActress.Contains(actress))
                        {
                            SelectedActress.Remove(actress);
                        }
                        else
                        {
                            SelectedActress.Add(actress);
                        }
                        break;
                    }

                }
                ActorSetSelected();
            }
            else
            {
                //await Task.Delay(50);
                Actress actress = new Actress();
                foreach (Actress item in vieModel.ActorList)
                {
                    if (item.name.ToUpper() == name.ToUpper()) { actress = item; break; }
                }
                vieModel = new VieModel_Main();
                vieModel.GetMoviebyActressAndVetioType(actress);
                vieModel.Actress = actress;
                this.DataContext = vieModel;
                ShowMovieGrid(sender, new RoutedEventArgs());
                ActorInfoGrid.Visibility = Visibility.Visible;
                vieModel.TextType = actress.name;
            }
        }



        public void Label_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Label label = (Label)sender;
            string genre = label.Content.ToString().Split('(')[0];
            vieModel = new VieModel_Main();
            vieModel.GetMoviebyLabel(genre);
            this.DataContext = vieModel;
            ShowMovieGrid(sender, new RoutedEventArgs());
            vieModel.TextType = genre;
        }

        WindowDetails wd;
        private void ShowDetails(object sender, MouseEventArgs e)
        {
            StackPanel parent = ((sender as FrameworkElement).Parent as Grid).Parent as StackPanel;
            var TB = parent.Children.OfType<TextBox>().First();//识别码
            //Console.WriteLine(TB.Text.ToUpper());
            if (Properties.Settings.Default.EditMode)
            {
                foreach (Movie movie in vieModel.CurrentMovieList)
                {
                    if (movie.id.ToUpper() == TB.Text.ToUpper())
                    {

                        if (vieModel.SelectedMovie.Contains(movie))
                        {
                            vieModel.SelectedMovie.Remove(movie);
                        }
                        else
                        {
                            vieModel.SelectedMovie.Add(movie);
                        }
                        break;
                    }

                }
                SetSelected();
            }
            else
            {
                StopDownLoad();

                if (wd != null) { wd.Close(); }
                wd = new WindowDetails(TB.Text);
                wd.Show();
                //wd.AdjustWindow();
            }

        }


        public void ShowSideBar(object sender, MouseButtonEventArgs e)
        {
            if (vieModel.ShowSideBar)
            {
                vieModel.ShowSideBar = false;
            }
            else { vieModel.ShowSideBar = true; }

        }



        public void ShowStatus(object sender, RoutedEventArgs e)
        {
            if (StatusPopup.IsOpen == true)
                StatusPopup.IsOpen = false;
            else
                StatusPopup.IsOpen = true;

        }




        public void ShowMenu(object sender, MouseButtonEventArgs e)
        {
            Grid grid = sender as Grid;
            Popup popup = grid.Children.OfType<Popup>().First();
            popup.IsOpen = true;
        }


        public void ShowViewMenu(object sender, MouseButtonEventArgs e)
        {
            if (ViewMenu.Visibility == Visibility.Visible)
            {
                ViewMenu.Visibility = Visibility.Hidden;
            }
            else { ViewMenu.Visibility = Visibility.Visible; }

        }

        public void ShowImageMenu(object sender, MouseButtonEventArgs e)
        {
            if (ImageMenu.Visibility == Visibility.Visible)
            {
                ImageMenu.Visibility = Visibility.Hidden;
            }
            else { ImageMenu.Visibility = Visibility.Visible; }

        }


        public void ShowDownloadMenu(object sender, MouseButtonEventArgs e)
        {
            DownloadPopup.IsOpen = true;
        }






        public async void RefreshScanPath(object sender, MouseButtonEventArgs e)
        {
            if (DownLoader?.State == DownLoadState.DownLoading)
            {
                new PopupWindow(this, "停止当前下载后再试").Show();
                return;
            }
                
            //刷新文件夹

            if (vieModel.IsScanning) {
                vieModel.IsScanning = false;
                RefreshScanCTS?.Cancel();
            }
            else
            {
                vieModel.IsScanning = true;
                RefreshScanCTS = new CancellationTokenSource();
                RefreshScanCTS.Token.Register(() => Console.WriteLine("取消任务"));
                RefreshScanCT = RefreshScanCTS.Token;
                await Task.Run(() =>
                {
                    List<string> filepaths = Scan.ScanPaths(ReadScanPathFromConfig(Properties.Settings.Default.DataBasePath.Split('\\').Last().Split('.').First()), RefreshScanCT);
                    DataBase cdb = new DataBase();
                    Scan.DistinctMovieAndInsert(filepaths, RefreshScanCT);
                    cdb.CloseDB();
                    vieModel.IsScanning = false;

                    this.Dispatcher.BeginInvoke(new Action(() => { vieModel.Reset(); }), System.Windows.Threading.DispatcherPriority.Render);

                    
                }, RefreshScanCTS.Token);

            }

        }


        public void ShowSearchMenu(object sender, MouseButtonEventArgs e)
        {
            SearchOptionPopup.IsOpen = true;
        }

        public void ShowTypeMenu(object sender, MouseButtonEventArgs e)
        {
            if (TypeMenu.Visibility == Visibility.Visible)
            {
                TypeMenu.Visibility = Visibility.Hidden;
            }
            else { TypeMenu.Visibility = Visibility.Visible; TypeMenu.Focus(); }
        }

        public void SetTypeValue(object sender, MouseButtonEventArgs e)
        {
            Label label = sender as Label;
            this.vieModel.VedioType = (VedioType)Enum.Parse(typeof(VedioType), label.Content.ToString());
            Properties.Settings.Default.VedioType = label.Content.ToString();
            Properties.Settings.Default.Save();
            TypeMenu.Visibility = Visibility.Hidden;
        }


        public void ShowDownloadActorMenu(object sender, MouseButtonEventArgs e)
        {
            DownloadActorPopup.IsOpen = true;
        }




        private void BeginSelect(object sender, MouseButtonEventArgs e)
        {
            //IsMouseDown = true;
            MosueDownPoint = Mouse.GetPosition(MovieMainGrid);
            Canvas.SetLeft(SelectBorder, Mouse.GetPosition(MovieMainGrid).X);
            Canvas.SetTop(SelectBorder, Mouse.GetPosition(MovieMainGrid).Y);
            SelectBorder.Width = 0;
            SelectBorder.Height = 0;
        }

        private void Selecting(object sender, MouseEventArgs e)
        {
            if (IsMouseDown & Properties.Settings.Default.EditMode)
            {
                //设置选中框范围
                SelectBorder.Visibility = Visibility.Visible;
                Point MousePoistion = Mouse.GetPosition(MovieMainGrid);
                double width = MousePoistion.X - MosueDownPoint.X - 5;
                double height = MousePoistion.Y - MosueDownPoint.Y - 5;
                SelectBorder.Width = Math.Abs(width);
                SelectBorder.Height = Math.Abs(height);
                if (width < 0) Canvas.SetLeft(SelectBorder, MousePoistion.X);
                if (height < 0) Canvas.SetTop(SelectBorder, MousePoistion.Y);

                //获取选中项
                Rect borderRect = GetBounds.BoundsRelativeTo(SelectBorder, MovieMainGrid);

                for (int i = 0; i < MovieItemsControl.Items.Count; i++)
                {
                    ContentPresenter c = (ContentPresenter)MovieItemsControl.ItemContainerGenerator.ContainerFromItem(MovieItemsControl.Items[i]);

                    WrapPanel wrapPanel = c.ContentTemplate.FindName("MovieWrapPanel", c) as WrapPanel;
                    if (wrapPanel != null)
                    {
                        Border border = wrapPanel.Children[0] as Border;
                        Rect rect = GetBounds.BoundsRelativeTo(border, MovieMainGrid);
                        if (borderRect.IntersectsWith(rect))
                        {
                            if (!vieModel.SelectedMovie.Contains(vieModel.CurrentMovieList[i]))
                            {
                                vieModel.SelectedMovie.Add(vieModel.CurrentMovieList[i]);
                            }
                        }
                    }

                }

                SetSelected();


            }

        }

        public void SetSelected()
        {
            for (int i = 0; i < MovieItemsControl.Items.Count; i++)
            {
                ContentPresenter c = (ContentPresenter)MovieItemsControl.ItemContainerGenerator.ContainerFromItem(MovieItemsControl.Items[i]);
                WrapPanel wrapPanel = FindElementByName<WrapPanel>(c, "MovieWrapPanel");
                if (wrapPanel != null)
                {
                    Border border = wrapPanel.Children[0] as Border;
                    TextBox textBox = c.ContentTemplate.FindName("idTextBox", c) as TextBox;
                    if (textBox != null)
                    {
                        border.Background = (SolidColorBrush)Application.Current.Resources["BackgroundSide"];
                        foreach (Movie movie in vieModel.SelectedMovie)
                        {
                            if (movie.id.ToUpper() == textBox.Text.ToUpper())
                            {
                                border.Background = (SolidColorBrush)Application.Current.Resources["Selected_Background"];
                                break;
                            }
                        }
                    }
                }
            }

        }

        public T FindElementByName<T>(FrameworkElement element, string sChildName) where T : FrameworkElement
        {
            T childElement = null;
            if (element == null) return childElement;
            var nChildCount = VisualTreeHelper.GetChildrenCount(element);
            for (int i = 0; i < nChildCount; i++)
            {
                FrameworkElement child = VisualTreeHelper.GetChild(element, i) as FrameworkElement;

                if (child == null)
                    continue;

                if (child is T && child.Name.Equals(sChildName))
                {
                    childElement = (T)child;
                    break;
                }

                childElement = FindElementByName<T>(child, sChildName);

                if (childElement != null)
                    break;
            }

            return childElement;
        }

        private Panel GetItemsPanel(DependencyObject itemsControl)
        {
            ItemsPresenter itemsPresenter = GetBounds.GetVisualChild<ItemsPresenter>(itemsControl);
            Panel itemsPanel = VisualTreeHelper.GetChild(itemsPresenter, 0) as Panel;
            return itemsPanel;
        }



        private void EndSelect(object sender, MouseButtonEventArgs e)
        {

            ES();
        }

        private void ES()
        {
            IsMouseDown = false;
            SelectBorder.Visibility = Visibility.Collapsed;
            SelectBorder.Width = 0;
            SelectBorder.Height = 0;
        }

        private void MovieMainGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            ES();
        }



        public void SetSortValue(object sender, MouseButtonEventArgs e)
        {

            Label label = sender as Label;
            if (label.Content.ToString() == SortLabel.Text.ToString())
            {
                Properties.Settings.Default.SortDescending = !Properties.Settings.Default.SortDescending;
            }
            else
            {
                SortLabel.Text = label.Content.ToString();
            }
            if (Properties.Settings.Default.SortDescending) { SortArrow.Text = "↓"; } else { SortArrow.Text = "↑"; }
            Properties.Settings.Default.Save();
            vieModel.Sort();
        }

        public void SaveAllSearchType(object sender, RoutedEventArgs e)
        {
            RadioButton radioButton = sender as RadioButton;
            Properties.Settings.Default.AllSearchType = radioButton.Content.ToString();
            //vieModel?.GetAllSearchCandidate();
        }

        public void SaveSearchType(object sender, RoutedEventArgs e)
        {
            RadioButton radioButton = sender as RadioButton;
            Properties.Settings.Default.SearchType = radioButton.Content.ToString();
            vieModel?.GetSearchCandidate();
        }

        public void SaveShowViewMode(object sender, RoutedEventArgs e)
        {
            RadioButton radioButton = sender as RadioButton;
            Properties.Settings.Default.ShowViewMode = radioButton.Content.ToString();
            Properties.Settings.Default.Save();
        }


        public void SaveShowImageMode(object sender, RoutedEventArgs e)
        {
            vieModel.FlowNum = 0;
            RadioButton radioButton = sender as RadioButton;
            Properties.Settings.Default.ShowImageMode = radioButton.Content.ToString();
            Properties.Settings.Default.Save();
            vieModel.FlipOver();
        }


        public List<ImageSlide> ImageSlides;
        public void Loadslide()
        {
            ImageSlides?.Clear();
            ImageSlides = new List<ImageSlide>();
            for (int i = 0; i < MovieItemsControl.Items.Count; i++)
            {
                ContentPresenter myContentPresenter = (ContentPresenter)MovieItemsControl.ItemContainerGenerator.ContainerFromIndex(i);
                if (myContentPresenter != null)
                {
                    Movie movie = (Movie)MovieItemsControl.Items[i];
                    DataTemplate myDataTemplate = myContentPresenter.ContentTemplate;
                    Image myImage = (Image)myDataTemplate.FindName("myImage", myContentPresenter);
                    Image myImage2 = (Image)myDataTemplate.FindName("myImage2", myContentPresenter);

                    ImageSlide imageSlide = new ImageSlide(StaticVariable.BasePicPath + $"ExtraPic\\{movie.id}", myImage, myImage2);
                    ImageSlides.Add(imageSlide);
                    imageSlide.PlaySlideShow();
                }

            }
        }


        private void ScrollViewer_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            //Console.WriteLine("ScrollViewer_IsVisibleChanged");
            if (!ScrollViewer.IsVisible)
            {
                FlowTimer.Start();
            }
        }




        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {

            ScrollViewer sv = sender as ScrollViewer;
            if (sv.VerticalOffset >= 500)
            {
                GoToTopCanvas.Visibility = Visibility.Visible;
            }
            else
            {
                GoToTopCanvas.Visibility = Visibility.Hidden;
            }

            if (sv.ScrollableHeight - sv.VerticalOffset <= 10 && sv.VerticalOffset != 0)
            {

                if (vieModel.CurrentMovieList.Count < Properties.Settings.Default.DisplayNumber && vieModel.CurrentMovieList.Count < vieModel.MovieList.Count && vieModel.CurrentMovieList.Count + (vieModel.CurrentPage - 1) * Properties.Settings.Default.DisplayNumber < vieModel.MovieList.Count)
                {
                    sv.ScrollToVerticalOffset(sv.VerticalOffset - 20);
                    FlowTimer.Start();
                }

            }

        }

        public bool IsUserVisible(FrameworkElement element, FrameworkElement container)
        {
            if (!element.IsVisible)
                return false;

            Rect bounds = element.TransformToAncestor(container).TransformBounds(new Rect(0.0, 0.0, element.ActualWidth, element.ActualHeight));
            Rect rect = new Rect(0.0, 0.0, container.ActualWidth, container.ActualHeight);
            return rect.Contains(bounds.TopLeft) || rect.Contains(bounds.BottomRight);
        }

        public void GotoTop(object sender, MouseButtonEventArgs e)
        {
            ScrollViewer.ScrollToTop();
        }

        public void PlayVedio(object sender, MouseButtonEventArgs e)
        {
            StackPanel parent = ((sender as FrameworkElement).Parent as Grid).Parent as StackPanel;
            var TB = parent.Children.OfType<TextBox>().Last();
            string filepath = TB.Text;
            if (File.Exists(filepath))
            {
                //使用默认播放器
                if(!string.IsNullOrEmpty(Properties.Settings.Default.VedioPlayerPath) && File.Exists(Properties.Settings.Default.VedioPlayerPath))
                {
                    try
                    {
                        Process.Start(Properties.Settings.Default.VedioPlayerPath, filepath);
                    }
                    catch(Exception ex)
                    {
                        Logger.LogE(ex);
                        Process.Start(filepath);
                    }
                    
                }
                else
                {
                    Process.Start(filepath);
                }
            }
            else
                new Msgbox(this, "无法打开 " + filepath).ShowDialog();

        }


        public void OpenImagePath(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Properties.Settings.Default.EditMode) vieModel.SelectedMovie.Clear();
                MenuItem _mnu = sender as MenuItem;
                MenuItem mnu = _mnu.Parent as MenuItem;

                StackPanel sp = null;
                if (mnu != null)
                {
                    int index = mnu.Items.IndexOf(_mnu);
                    sp = ((ContextMenu)mnu.Parent).PlacementTarget as StackPanel;
                    var TB = sp.Children.OfType<TextBox>().First();
                    Movie CurrentMovie = GetMovieFromVieModel(TB.Text);
                    if (!vieModel.SelectedMovie.Select(g => g.id).ToList().Contains(CurrentMovie.id)) vieModel.SelectedMovie.Add(CurrentMovie);

                    if (Properties.Settings.Default.EditMode & vieModel.SelectedMovie.Count >= 2)
                        if (new Msgbox(this, $"是否打开选中的 {vieModel.SelectedMovie.Count}个所在文件夹？").ShowDialog() == false) { return; }

                    string failpath = ""; int num = 0;
                    vieModel.SelectedMovie.ToList().ForEach(arg =>
                    {

                        string filepath = arg.filepath;
                        if (index == 0) { filepath = arg.filepath; }
                        else if (index == 1) { filepath = BasePicPath + $"BigPic\\{arg.id}.jpg"; }
                        else if (index == 2) { filepath = BasePicPath + $"SmallPic\\{arg.id}.jpg"; }
                        else if (index == 3) { filepath = BasePicPath + $"ExtraPic\\{arg.id}\\"; }
                        else if (index == 4) { filepath = BasePicPath + $"ScreenShot\\{arg.id}\\"; }
                        else if (index == 5) { if (arg.actor.Length > 0) filepath = BasePicPath + $"Actresses\\{arg.actor.Split(new char[] { ' ', '/' })[0]}.jpg"; else filepath = ""; }

                        if (index == 3 | index == 4)
                        {
                            if (Directory.Exists(filepath)) { Process.Start("explorer.exe", "\"" + filepath + "\""); }
                            else
                            {
                                failpath += filepath + "\n";
                                num++;
                            }
                        }
                        else
                        {
                            if (File.Exists(filepath)) { Process.Start("explorer.exe", "/select, \"" + filepath + "\""); }
                            else
                            {
                                failpath += filepath + "\n";
                                num++;
                            }
                        }




                    });
                    if (failpath != "")
                        new Msgbox(this, $"成功打开{vieModel.SelectedMovie.Count - num}个，失败{num}个，因为不存在 ：\n{failpath}").ShowDialog();

                }

            }
            catch (Exception ex) { Console.WriteLine(ex.StackTrace); Console.WriteLine(ex.Message); }
            if (!Properties.Settings.Default.EditMode) vieModel.SelectedMovie.Clear();
        }

        public void OpenFilePath(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Properties.Settings.Default.EditMode) vieModel.SelectedMovie.Clear();
                MenuItem _mnu = sender as MenuItem;
                MenuItem mnu = _mnu.Parent as MenuItem;
                StackPanel sp = null;
                if (mnu != null)
                {
                    sp = ((ContextMenu)mnu.Parent).PlacementTarget as StackPanel;
                    var TB = sp.Children.OfType<TextBox>().First();
                    Movie CurrentMovie = GetMovieFromVieModel(TB.Text);
                    if (!vieModel.SelectedMovie.Select(g => g.id).ToList().Contains(CurrentMovie.id)) vieModel.SelectedMovie.Add(CurrentMovie);

                    if (Properties.Settings.Default.EditMode & vieModel.SelectedMovie.Count >= 2)
                        if (new Msgbox(this, $"是否打开选中的 {vieModel.SelectedMovie.Count}个所在文件夹？").ShowDialog() == false) { return; }

                    string failpath = ""; int num = 0;
                    vieModel.SelectedMovie.ToList().ForEach(arg =>
                    {
                        if (File.Exists(arg.filepath)) { Process.Start("explorer.exe", "/select, \"" + arg.filepath + "\""); }
                        else
                        {
                            failpath += arg.filepath + "\n";
                            num++;
                        }


                    });
                    if (failpath != "")
                        new Msgbox(this, $"成功打开{vieModel.SelectedMovie.Count - num}个，失败{num}个，因为文件夹不存在 ：\n{failpath}").ShowDialog();

                }

            }
            catch (Exception ex) { Console.WriteLine(ex.StackTrace); Console.WriteLine(ex.Message); }
            if (!Properties.Settings.Default.EditMode) vieModel.SelectedMovie.Clear();
        }


        public async void TranslateMovie(object sender, RoutedEventArgs e)
        {
            if (!Properties.Settings.Default.Enable_TL_BAIDU & !Properties.Settings.Default.Enable_TL_YOUDAO) { new PopupWindow(this, "请在设置中开启并测试").Show(); return; }


            if (!Properties.Settings.Default.EditMode) vieModel.SelectedMovie.Clear();
            MenuItem mnu = sender as MenuItem;
            StackPanel sp = null;
            if (mnu != null)
            {
                sp = ((ContextMenu)mnu.Parent).PlacementTarget as StackPanel;
                var TB = sp.Children.OfType<TextBox>().First();
                Movie CurrentMovie = GetMovieFromVieModel(TB.Text);
                if (!vieModel.SelectedMovie.Select(g => g.id).ToList().Contains(CurrentMovie.id)) vieModel.SelectedMovie.Add(CurrentMovie);
                string result = "";
                DataBase dataBase = new DataBase("Translate");


                int successNum = 0;
                int failNum = 0;
                int translatedNum = 0;

                foreach (Movie movie in vieModel.SelectedMovie)
                {

                    //检查是否已经翻译过，如有则跳过
                    if (!string.IsNullOrEmpty(dataBase.SelectInfoByID("translate_title", "youdao",movie.id))) { translatedNum++; continue; }
                    if (movie.title != "")
                    {

                        if (Properties.Settings.Default.Enable_TL_YOUDAO) result = await Translate.Youdao(movie.title);
                        //保存
                        if (result != "")
                        {

                            dataBase.SaveYoudaoTranslateByID(movie.id, movie.title, result, "title");

                            //显示
                            int index1 = vieModel.CurrentMovieList.IndexOf(movie);
                            int index2 = vieModel.MovieList.IndexOf(movie);
                            movie.title = result;
                            vieModel.CurrentMovieList[index1] = null;
                            vieModel.MovieList[index2] = null;
                            vieModel.CurrentMovieList[index1] = movie;
                            vieModel.MovieList[index2] = movie;
                            successNum++;
                        }

                    }
                    else { failNum++; }

                    if (movie.plot != "")
                    {
                        if (Properties.Settings.Default.Enable_TL_YOUDAO) result = await Translate.Youdao(movie.plot);
                        //保存
                        if (result != "")
                        {
                            dataBase.SaveYoudaoTranslateByID(movie.id, movie.plot, result, "plot");
                            dataBase.CloseDB();
                        }

                    }

                }
                dataBase.CloseDB();

                new PopupWindow(this, $"成功：{successNum}个，失败：{failNum}个，跳过：{translatedNum}个").Show();

            }
            if (!Properties.Settings.Default.EditMode) vieModel.SelectedMovie.Clear();
        }


        public void ClearSearch(object sender, MouseButtonEventArgs e)
        {
            AllSearchTextBox.Text = "";
        }

        public async void GenerateActor(object sender, RoutedEventArgs e)
        {
            if (!Properties.Settings.Default.Enable_BaiduAI) { new PopupWindow(this, "请在设置中开启并测试").Show(); return; }
            if (!Properties.Settings.Default.EditMode) vieModel.SelectedMovie.Clear();
            MenuItem _mnu = sender as MenuItem;
            MenuItem mnu = _mnu.Parent as MenuItem;
            StackPanel sp = null;
            if (mnu != null)
            {



                sp = ((ContextMenu)mnu.Parent).PlacementTarget as StackPanel;
                var TB = sp.Children.OfType<TextBox>().First();
                Movie CurrentMovie = GetMovieFromVieModel(TB.Text);
                if (!vieModel.SelectedMovie.Select(g => g.id).ToList().Contains(CurrentMovie.id)) vieModel.SelectedMovie.Add(CurrentMovie);


                if (vieModel.SelectedMovie.Count > 3 && new Msgbox(this, $"预计用时 {(float)vieModel.SelectedMovie.Count / 2} s，是否继续？").ShowDialog() == false) return;

                this.Cursor = Cursors.Wait;
                int successNum = 0;

                foreach (Movie movie in vieModel.SelectedMovie)
                {
                    if (movie.actor == "") continue;
                    string BigPicPath = Properties.Settings.Default.BasePicPath + $"BigPic\\{movie.id}.jpg";

                    string name;
                    if (ActorInfoGrid.Visibility == Visibility.Visible)
                        name = vieModel.Actress.name;
                    else
                        name = movie.actor.Split(new char[] { ' ', '/' })[0];


                    string ActressesPicPath = Properties.Settings.Default.BasePicPath + $"Actresses\\{name}.jpg";
                    if (File.Exists(BigPicPath))
                    {
                        Int32Rect int32Rect = await GetAIResult(movie, BigPicPath);
                        if (int32Rect != Int32Rect.Empty)
                        {
                            await Task.Delay(500);
                            //切割演员头像
                            System.Drawing.Bitmap SourceBitmap = new System.Drawing.Bitmap(BigPicPath);
                            BitmapImage bitmapImage = ImageProcess.BitmapToBitmapImage(SourceBitmap);
                            ImageSource actressImage = ImageProcess.CutImage(bitmapImage, ImageProcess.GetActressRect(bitmapImage, int32Rect));
                            System.Drawing.Bitmap bitmap = ImageProcess.ImageSourceToBitmap(actressImage);
                            try { bitmap.Save(ActressesPicPath, System.Drawing.Imaging.ImageFormat.Jpeg); successNum++; }
                            catch (Exception ex) { Logger.LogE(ex); }
                        }
                    }
                }
                new PopupWindow(this, $"成功切割 {successNum} / {vieModel.SelectedMovie.Count} 个头像").Show();
            }
            if (!Properties.Settings.Default.EditMode) vieModel.SelectedMovie.Clear();
            this.Cursor = Cursors.Arrow;
        }


        public async void GetScreenShot(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(Properties.Settings.Default.FFMPEG_Path)) { new PopupWindow(this, "请设置 ffmpeg.exe 的路径 ").Show(); return; }
            if (!Properties.Settings.Default.EditMode) vieModel.SelectedMovie.Clear();
            MenuItem mnu = sender as MenuItem;
            StackPanel sp = null;

            if (mnu != null)
            {
                int successNum = 0;
                sp = ((ContextMenu)mnu.Parent).PlacementTarget as StackPanel;
                var TB = sp.Children.OfType<TextBox>().First();
                Movie CurrentMovie = GetMovieFromVieModel(TB.Text);
                if (!vieModel.SelectedMovie.Select(g => g.id).ToList().Contains(CurrentMovie.id)) vieModel.SelectedMovie.Add(CurrentMovie);
                this.Cursor = Cursors.Wait;
                foreach (Movie movie in vieModel.SelectedMovie)
                {
                    if (!File.Exists(movie.filepath)) continue;
                    bool result = false;
                    try { result = await ScreenShot(movie); }catch(Exception ex) { Logger.LogF(ex); }
                   
                    if (result) successNum++;
                }
                new PopupWindow(this, $"成功截图 {successNum} / {vieModel.SelectedMovie.Count} 个影片").Show();
            }
            if (!Properties.Settings.Default.EditMode) vieModel.SelectedMovie.Clear();
            this.Cursor = Cursors.Arrow;
        }





        public Task<bool> ScreenShot(Movie movie)
        {
            return Task.Run(() =>
            {
                if (!File.Exists(Properties.Settings.Default.FFMPEG_Path)) return false;


                //获得影片长度数组
                string ScreenShotPath = BasePicPath + "ScreenShot\\" + movie.id;
                if (!Directory.Exists(ScreenShotPath)) Directory.CreateDirectory(ScreenShotPath);

                string[] cutoffArray = MediaParse.GetCutOffArray(movie.filepath);
                if (cutoffArray != null)
                {
                    for (int i = 0; i < cutoffArray.Count(); i++)
                    {
                        if (string.IsNullOrEmpty(cutoffArray[i])) continue;

                        System.Diagnostics.Process p = new System.Diagnostics.Process();
                        p.StartInfo.FileName = "cmd.exe";
                        p.StartInfo.UseShellExecute = false;    //是否使用操作系统shell启动
                        p.StartInfo.RedirectStandardInput = true;//接受来自调用程序的输入信息
                        p.StartInfo.RedirectStandardOutput = true;//由调用程序获取输出信息
                        p.StartInfo.RedirectStandardError = true;//重定向标准错误输出
                        p.StartInfo.CreateNoWindow = true;//不显示程序窗口
                        p.Start();//启动程序

                        string str = $"{Properties.Settings.Default.FFMPEG_Path} -ss {cutoffArray[i]} -i \"{movie.filepath}\" -f image2 -frames:v 1 {ScreenShotPath}\\ScreenShot-{i.ToString().PadLeft(2, '0')}.jpg";
                        Console.WriteLine(str);
                        p.StandardInput.WriteLine(str + "&exit");
                        p.StandardInput.WriteLine("exit");//结束执行，很重要的
                        p.StandardInput.AutoFlush = true;
                        p.WaitForExit();//等待程序执行完退出进程
                        p.Close();
                    }
                    return true;
                }
                else
                {
                    return false;
                }








            });
        }


        public async void GenerateSmallImage(object sender, RoutedEventArgs e)
        {
            if (!Properties.Settings.Default.Enable_BaiduAI) { new PopupWindow(this, "请在设置中开启并测试").Show(); return; }
            if (!Properties.Settings.Default.EditMode) vieModel.SelectedMovie.Clear();
            MenuItem _mnu = sender as MenuItem;
            MenuItem mnu = _mnu.Parent as MenuItem;
            StackPanel sp = null;
            if (mnu != null)
            {
                sp = ((ContextMenu)mnu.Parent).PlacementTarget as StackPanel;
                var TB = sp.Children.OfType<TextBox>().First();
                Movie CurrentMovie = GetMovieFromVieModel(TB.Text);
                if (!vieModel.SelectedMovie.Select(g => g.id).ToList().Contains(CurrentMovie.id)) vieModel.SelectedMovie.Add(CurrentMovie);

                if (vieModel.SelectedMovie.Count > 3 && new Msgbox(this, $"预计用时 {(float)vieModel.SelectedMovie.Count / 2} s，是否继续？").ShowDialog() == false) return;
                int successNum = 0;
                this.Cursor = Cursors.Wait;
                foreach (Movie movie in vieModel.SelectedMovie)
                {
                    string BigPicPath = Properties.Settings.Default.BasePicPath + $"BigPic\\{movie.id}.jpg";
                    string SmallPicPath = Properties.Settings.Default.BasePicPath + $"SmallPic\\{movie.id}.jpg";
                    if (File.Exists(BigPicPath))
                    {
                        Int32Rect int32Rect = await GetAIResult(movie, BigPicPath);

                        if (int32Rect != Int32Rect.Empty)
                        {
                            await Task.Delay(500);
                            //切割缩略图
                            System.Drawing.Bitmap SourceBitmap = new System.Drawing.Bitmap(BigPicPath);
                            BitmapImage bitmapImage = ImageProcess.BitmapToBitmapImage(SourceBitmap);
                            ImageSource smallImage = ImageProcess.CutImage(bitmapImage, ImageProcess.GetRect(bitmapImage, int32Rect));
                            System.Drawing.Bitmap bitmap = ImageProcess.ImageSourceToBitmap(smallImage);
                            try
                            {
                                bitmap.Save(SmallPicPath, System.Drawing.Imaging.ImageFormat.Jpeg); successNum++;
                            }
                            catch (Exception ex) { Logger.LogE(ex); }


                            //读取
                            int index1 = vieModel.CurrentMovieList.IndexOf(movie);
                            int index2 = vieModel.MovieList.IndexOf(movie);
                            movie.smallimage = StaticClass.GetBitmapImage(movie.id, "SmallPic");
                            vieModel.CurrentMovieList[index1] = null;
                            vieModel.MovieList[index2] = null;
                            vieModel.CurrentMovieList[index1] = movie;
                            vieModel.MovieList[index2] = movie;
                        }

                    }

                }
                new PopupWindow(this, $"成功切割 {successNum} / {vieModel.SelectedMovie.Count} 个头像").Show();


            }
            if (!Properties.Settings.Default.EditMode) vieModel.SelectedMovie.Clear();
            this.Cursor = Cursors.Arrow;
        }


        private Task<Int32Rect> GetAIResult(Movie movie, string path)
        {
            return Task.Run(() =>
            {
                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(path);
                string token = AccessToken.getAccessToken();
                string FaceJson = FaceDetect.faceDetect(token, bitmap);

                Dictionary<string, string> result;
                Int32Rect int32Rect;
                (result, int32Rect) = FaceParse.Parse(FaceJson);
                if (result != null && int32Rect != Int32Rect.Empty)
                {
                    DataBase dataBase = new DataBase("AI");
                    dataBase.SaveBaiduAIByID(movie.id, result);
                    dataBase.CloseDB();
                    return int32Rect;
                }
                else
                    return Int32Rect.Empty;
            });

        }

        public void CopyFile(object sender, RoutedEventArgs e)
        {
            if (!Properties.Settings.Default.EditMode) vieModel.SelectedMovie.Clear();
            MenuItem mnu = sender as MenuItem;
            StackPanel sp = null;
            if (mnu != null)
            {
                sp = ((ContextMenu)mnu.Parent).PlacementTarget as StackPanel;
                var TB = sp.Children.OfType<TextBox>().First();
                Movie CurrentMovie = GetMovieFromVieModel(TB.Text);
                if (!vieModel.SelectedMovie.Select(g => g.id).ToList().Contains(CurrentMovie.id)) vieModel.SelectedMovie.Add(CurrentMovie);
                StringCollection paths = new StringCollection();
                int num = 0;
                vieModel.SelectedMovie.ToList().ForEach(arg => { if (File.Exists(arg.filepath)) { paths.Add(arg.filepath); num++; } });
                if (paths.Count > 0)
                {
                    try
                    {
                        Clipboard.SetFileDropList(paths);
                        new Msgbox(this, $"已复制 {num}/{vieModel.SelectedMovie.Count} 个文件").ShowDialog();
                    }
                    catch (Exception ex) { Console.WriteLine(ex.Message); }
                }
                else
                {
                    new PopupWindow(this, $"文件不存在！无法复制！ ").Show();
                }



            }
            if (!Properties.Settings.Default.EditMode) vieModel.SelectedMovie.Clear();
        }

        public void DeleteFile(object sender, RoutedEventArgs e)
        {
            if (!Properties.Settings.Default.EditMode) vieModel.SelectedMovie.Clear();
            MenuItem mnu = sender as MenuItem;
            StackPanel sp = null;
            if (mnu != null)
            {
                sp = ((ContextMenu)mnu.Parent).PlacementTarget as StackPanel;
                var TB = sp.Children.OfType<TextBox>().First();
                Movie CurrentMovie = GetMovieFromVieModel(TB.Text);
                if (!vieModel.SelectedMovie.Select(g => g.id).ToList().Contains(CurrentMovie.id)) vieModel.SelectedMovie.Add(CurrentMovie);

                if (Properties.Settings.Default.EditMode)
                    if (new Msgbox(this, $"是否确认删除选中的 {vieModel.SelectedMovie.Count}个视频（保留数据库信息）？").ShowDialog() == false) { return; }

                int num = 0;
                vieModel.SelectedMovie.ToList().ForEach(arg =>
                {
                    if (File.Exists(arg.filepath))
                    {
                        try
                        {
                            FileSystem.DeleteFile(arg.filepath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                            num++;
                        }
                        catch (Exception ex) { Logger.LogF(ex); }

                    }
                });
                if (num > 0)
                    new PopupWindow(this, $"已删除 {num}/{vieModel.SelectedMovie.Count}个视频到回收站").Show();
                else
                    new PopupWindow(this, $"删除失败！ ").Show();


            }
            if (!Properties.Settings.Default.EditMode) vieModel.SelectedMovie.Clear();
        }

        public WindowEdit WindowEdit;


        public void EditInfo(object sender, RoutedEventArgs e)
        {
            if (Properties.Settings.Default.EditMode) { new PopupWindow(this, "编辑模式不可批量修改信息！").Show(); return; }
            if (DownLoader?.State == DownLoadState.DownLoading) { new Msgbox(this, "请等待下载完成！").ShowDialog(); return; }
            MenuItem mnu = sender as MenuItem;
            StackPanel sp = null;
            if (mnu != null)
            {
                sp = ((ContextMenu)mnu.Parent).PlacementTarget as StackPanel;
                var TB = sp.Children.OfType<TextBox>().First();
                string id = TB.Text;
                if (WindowEdit != null) { WindowEdit.Close(); }
                WindowEdit = new WindowEdit(id);
                WindowEdit.ShowDialog();
            }
        }

        public async void DeleteID(object sender, RoutedEventArgs e)
        {
            if (DownLoader?.State == DownLoadState.DownLoading) { new Msgbox(this, "请等待下载完成！").ShowDialog(); return; }
            if (!Properties.Settings.Default.EditMode) vieModel.SelectedMovie.Clear();
            MenuItem mnu = sender as MenuItem;
            StackPanel sp = null;
            if (mnu != null)
            {
                sp = ((ContextMenu)mnu.Parent).PlacementTarget as StackPanel;
                var TB = sp.Children.OfType<TextBox>().First();
                Movie CurrentMovie = GetMovieFromVieModel(TB.Text);
                if (!vieModel.SelectedMovie.Select(g => g.id).ToList().Contains(CurrentMovie.id)) vieModel.SelectedMovie.Add(CurrentMovie);

                if (Properties.Settings.Default.EditMode)
                    if (new Msgbox(this, $"是否从数据库删除 {vieModel.SelectedMovie.Count}个视频？（保留文件）").ShowDialog() == false) { return; }

                DataBase cdb = new DataBase();
                vieModel.SelectedMovie.ToList().ForEach(arg => {
                    cdb.DelInfoByType("movie", "id", arg.id); 
                    vieModel.CurrentMovieList.Remove(arg); //从主界面删除
                    vieModel.MovieList.Remove(arg);
                });
                cdb.CloseDB();

                //从详情窗口删除
                if (Jvedio.GetWindow.Get("WindowDetails") != null)
                {
                    WindowDetails windowDetails = Jvedio.GetWindow.Get("WindowDetails") as WindowDetails;
                    foreach (var item in vieModel.SelectedMovie.ToList())
                    {
                        if (windowDetails.vieModel.DetailMovie.id.ToUpper() == item.id.ToUpper())
                        {
                            windowDetails.Close();
                            break;
                        }
                    }
                }

                new PopupWindow(this, $"已从数据库删除 {vieModel.SelectedMovie.Count}个视频 ").Show();

                vieModel.SelectedMovie.Clear();

               vieModel. MovieCount = $"本页有 {vieModel.CurrentMovieList.Count} 个，总计{vieModel.MovieList.Count} 个";

               
                await Task.Run(() => { Task.Delay(1000).Wait(); });
            }
            if (!Properties.Settings.Default.EditMode) vieModel.SelectedMovie.Clear();
        }

        public Movie GetMovieFromVieModel(string id)
        {
            foreach (Movie movie in vieModel.CurrentMovieList)
            {
                if (movie.id.ToUpper() == id.ToUpper())
                {
                    return movie;
                }
            }
            return null;
        }

        public Actress GetActressFromVieModel(string name)
        {
            foreach (Actress actress in vieModel.ActorList)
            {
                if (actress.name == name)
                {
                    return actress;
                }
            }
            return null;
        }

        public string GetFormatGenreString(List<Movie> movies, string type = "genre")
        {
            List<string> list = new List<string>();
            if (type == "genre")
            {
                movies.ForEach(arg =>
                {
                    foreach (var item in arg.genre.Split(' '))
                    {
                        if (!string.IsNullOrEmpty(item) & item.IndexOf(' ') < 0)
                            if (!list.Contains(item)) list.Add(item);
                    }
                });
            }
            else if (type == "label")
            {
                movies.ForEach(arg =>
                {
                    foreach (var item in arg.label.Split(' '))
                    {
                        if (!string.IsNullOrEmpty(item) & item.IndexOf(' ') < 0)
                            if (!list.Contains(item)) list.Add(item);
                    }
                });
            }
            else if (type == "actor")
            {

                movies.ForEach(arg =>
                {

                    foreach (var item in arg.actor.Split(new char[] { ' ', '/' }))
                    {
                        if (!string.IsNullOrEmpty(item) & item.IndexOf(' ') < 0)
                            if (!list.Contains(item)) list.Add(item);
                    }
                });
            }

            string result = "";
            list.ForEach(arg => { result += arg + " "; });
            return result;
        }


        //清空标签
        public void ClearLabel(object sender, RoutedEventArgs e)
        {
            if (!Properties.Settings.Default.EditMode) vieModel.SelectedMovie.Clear();
            MenuItem _mnu = sender as MenuItem;
            MenuItem mnu = _mnu.Parent as MenuItem;
            StackPanel sp = null;
            if (mnu != null)
            {
                sp = ((ContextMenu)mnu.Parent).PlacementTarget as StackPanel;
                var TB = sp.Children.OfType<TextBox>().First();
                Movie CurrentMovie = GetMovieFromVieModel(TB.Text);
                if (!vieModel.SelectedMovie.Select(g => g.id).ToList().Contains(CurrentMovie.id)) vieModel.SelectedMovie.Add(CurrentMovie);

                foreach (var movie in this.vieModel.MovieList)
                {
                    foreach (var item in vieModel.SelectedMovie)
                    {
                        if (item.id.ToUpper() == movie.id.ToUpper())
                        {
                            this.vieModel.SaveModel(item.id.ToUpper(), "label", "", "String");
                            break;
                        }
                    }

                }
                new PopupWindow(this, $"成功清空标签").Show();


            }
            if (!Properties.Settings.Default.EditMode) vieModel.SelectedMovie.Clear();

        }


        //删除标签
        public void DelLabel(object sender, RoutedEventArgs e)
        {
            if (!Properties.Settings.Default.EditMode) vieModel.SelectedMovie.Clear();
            MenuItem _mnu = sender as MenuItem;
            MenuItem mnu = _mnu.Parent as MenuItem;
            StackPanel sp = null;
            if (mnu != null)
            {
                sp = ((ContextMenu)mnu.Parent).PlacementTarget as StackPanel;
                var TB = sp.Children.OfType<TextBox>().First();
                Movie CurrentMovie = GetMovieFromVieModel(TB.Text);
                if (!vieModel.SelectedMovie.Select(g => g.id).ToList().Contains(CurrentMovie.id)) vieModel.SelectedMovie.Add(CurrentMovie);

                //string TotalLabel = GetFormatGenreString(vieModel.SelectedMovie,"label");
                var di = new DialogInput(this, "请输入需删除的标签，每个标签空格隔开", "");
                di.ShowDialog();
                if (di.DialogResult == true & di.Text != "")
                {
                    foreach (var movie in this.vieModel.MovieList)
                    {
                        foreach (var item in vieModel.SelectedMovie)
                        {
                            if (item.id.ToUpper() == movie.id.ToUpper())
                            {
                                List<string> originlabel = LabelToList(movie.label);
                                List<string> newlabel = LabelToList(di.Text);
                                movie.label = ListToLabel(originlabel.Except(newlabel).ToList());
                                this.vieModel.SaveModel(item.id.ToUpper(), "label", movie.label, "String");
                                break;
                            }
                        }

                    }
                    new PopupWindow(this, $"成功删除标签{di.Text}").Show();
                }

            }
            if (!Properties.Settings.Default.EditMode) vieModel.SelectedMovie.Clear();
        }

        //增加标签
        public void AddLabel(object sender, RoutedEventArgs e)
        {
            if (!Properties.Settings.Default.EditMode) vieModel.SelectedMovie.Clear();
            MenuItem _mnu = sender as MenuItem;
            MenuItem mnu = _mnu.Parent as MenuItem;
            StackPanel sp = null;
            if (mnu != null)
            {
                sp = ((ContextMenu)mnu.Parent).PlacementTarget as StackPanel;
                var TB = sp.Children.OfType<TextBox>().First();
                Movie CurrentMovie = GetMovieFromVieModel(TB.Text);
                if (!vieModel.SelectedMovie.Select(g => g.id).ToList().Contains(CurrentMovie.id)) vieModel.SelectedMovie.Add(CurrentMovie);
                var di = new DialogInput(this, "请输入需添加的标签，每个标签空格隔开", "");
                di.ShowDialog();
                if (di.DialogResult == true & di.Text != "")
                {
                    foreach (var movie in this.vieModel.MovieList)
                    {
                        foreach (var item in vieModel.SelectedMovie)
                        {
                            if (item.id.ToUpper() == movie.id.ToUpper())
                            {
                                List<string> originlabel = LabelToList(movie.label);
                                List<string> newlabel = LabelToList(di.Text);
                                movie.label = ListToLabel(originlabel.Union(newlabel).ToList());
                                originlabel.ForEach(arg => Console.WriteLine(arg));
                                newlabel.ForEach(arg => Console.WriteLine(arg));
                                originlabel.Union(newlabel).ToList().ForEach(arg => Console.WriteLine(arg));
                                this.vieModel.SaveModel(item.id.ToUpper(), "label", movie.label, "String");
                                break;
                            }
                        }

                    }
                    new PopupWindow(this, $"成功增加标签{di.Text}").Show();
                }

            }
            if (!Properties.Settings.Default.EditMode) vieModel.SelectedMovie.Clear();
        }

        public string ListToLabel(List<string> label)
        {
            string result = "";
            label.ForEach(arg =>
            {
                string l = arg.Replace(" ", "");
                result += l + " ";
            });
            return result.Length > 0 ? result.Substring(0, result.Length - 1) : result;
        }

        public List<string> LabelToList(string label)
        {
            List<string> result = new List<string>();
            if (label.IndexOf(' ') > 0)
            {
                foreach (var item in label.Split(' '))
                {
                    if (item.Length > 0)
                        if (!result.Contains(item)) result.Add(item);
                }
            }
            else { if (label.Length > 0) result.Add(label.Replace(" ", "")); }
            return result;
        }

        //设置喜爱
        public void SetFavorites(object sender, RoutedEventArgs e)
        {
            if (!Properties.Settings.Default.EditMode) vieModel.SelectedMovie.Clear();
            MenuItem mnu = sender as MenuItem;
            int favorites = int.Parse(mnu.Header.ToString());
            StackPanel sp = null;
            if (mnu != null)
            {
                sp = ((ContextMenu)((MenuItem)mnu.Parent).Parent).PlacementTarget as StackPanel;
                var TB = sp.Children.OfType<TextBox>().First();
                Movie CurrentMovie = GetMovieFromVieModel(TB.Text);
                if (!vieModel.SelectedMovie.Select(g => g.id).ToList().Contains(CurrentMovie.id)) vieModel.SelectedMovie.Add(CurrentMovie);

                foreach (var movie in this.vieModel.MovieList)
                {
                    foreach (var item in vieModel.SelectedMovie)
                    {
                        if (item.id.ToUpper() == movie.id.ToUpper())
                        {
                            movie.favorites = favorites;
                            this.vieModel.SaveModel(item.id.ToUpper(), "favorites", favorites);
                            break;
                        }
                    }
                }


            }
            if (!Properties.Settings.Default.EditMode) vieModel.SelectedMovie.Clear();

        }

        //打开网址
        private void OpenWeb(object sender, RoutedEventArgs e)
        {

            try
            {
                if (!Properties.Settings.Default.EditMode) vieModel.SelectedMovie.Clear();
                MenuItem mnu = sender as MenuItem;
                if (mnu != null)
                {
                    StackPanel sp = ((ContextMenu)mnu.Parent).PlacementTarget as StackPanel;
                    var TB = sp.Children.OfType<TextBox>().First();
                    Movie CurrentMovie = GetMovieFromVieModel(TB.Text);
                    if (!vieModel.SelectedMovie.Select(g => g.id).ToList().Contains(CurrentMovie.id)) vieModel.SelectedMovie.Add(CurrentMovie);


                    if (Properties.Settings.Default.EditMode & vieModel.SelectedMovie.Count >= 2)
                        if (new Msgbox(this, $"是否打开选中的 {vieModel.SelectedMovie.Count}个网站？").ShowDialog() == false) { return; }

                    vieModel.SelectedMovie.ToList().ForEach(arg =>
                    {
                        if (arg.sourceurl != "") Process.Start(arg.sourceurl);
                    });
                }

            }
            catch (Exception ex) { Console.WriteLine(ex.StackTrace); Console.WriteLine(ex.Message); }
            if (!Properties.Settings.Default.EditMode) vieModel.SelectedMovie.Clear();
        }


        private void DownLoadSelectMovie(object sender, RoutedEventArgs e)
        {
            if (DownLoader?.State == DownLoadState.DownLoading)
                new PopupWindow(this, "已有任务在下载！").Show();
            else
            {
                try
                {
                    if (!Properties.Settings.Default.EditMode) vieModel.SelectedMovie.Clear();

                    MenuItem _mnu = sender as MenuItem;
                    MenuItem mnu = _mnu.Parent as MenuItem;
                    StackPanel sp = null;

                    if (mnu != null)
                    {
                        int index = mnu.Items.IndexOf(_mnu);
                        sp = ((ContextMenu)mnu.Parent).PlacementTarget as StackPanel;
                        var TB = sp.Children.OfType<TextBox>().First();
                        Movie CurrentMovie = GetMovieFromVieModel(TB.Text);
                        if (!vieModel.SelectedMovie.Select(g => g.id).ToList().Contains(CurrentMovie.id)) vieModel.SelectedMovie.Add(CurrentMovie);

                        StartDownload(vieModel.SelectedMovie.ToList());
                    }
                }
                catch (Exception ex) { Console.WriteLine(ex.StackTrace); Console.WriteLine(ex.Message); }
            }
            if (!Properties.Settings.Default.EditMode) vieModel.SelectedMovie.Clear();
        }


        private void Canvas_MouseEnter(object sender, MouseEventArgs e)
        {
            if (ImageSlides == null) return;
            Canvas canvas = (Canvas)sender;
            TextBlock textBlock = canvas.Children.OfType<TextBlock>().First();
            int index = int.Parse(textBlock.Text);
            if (index < ImageSlides.Count)
            {
                ImageSlides[index].PlaySlideShow();
                ImageSlides[index].Start();
            }

        }

        private void Canvas_MouseLeave(object sender, MouseEventArgs e)
        {
            if (ImageSlides == null) return;
            Canvas canvas = (Canvas)sender;
            TextBlock textBlock = canvas.Children.OfType<TextBlock>().First();
            int index = int.Parse(textBlock.Text);
            if (index < ImageSlides.Count)
            {
                ImageSlides[index].Stop();
            }

        }



        private void EditActress(object sender, MouseButtonEventArgs e)
        {
            vieModel.EnableEditActress = !vieModel.EnableEditActress;
        }

        private void SaveActress(object sender,KeyEventArgs e)
        {
            if (vieModel.EnableEditActress)
            {
                if (e.Key == Key.Enter)
                {
                    ScrollViewer.Focus();
                    vieModel.EnableEditActress = false;
                    DataBase dataBase = new DataBase();
                    dataBase.InsertActress(vieModel.Actress);
                    dataBase.CloseDB();
                }
            }
            
        }

        private void BeginDownLoadActress(object sender, MouseButtonEventArgs e)
        {
            List<Actress> actresses = new List<Actress>();
            actresses.Add(vieModel.Actress);
            DownLoadActress downLoadActress = new DownLoadActress(actresses) ;
            downLoadActress.BeginDownLoad();

            downLoadActress.InfoUpdate += (s, ev) =>
            {
                ActressUpdateEventArgs actressUpdateEventArgs = ev as ActressUpdateEventArgs;
                try
                {
                    Dispatcher.Invoke((Action)delegate ()
                    {
                        vieModel.Actress = null;
                        vieModel.Actress = actressUpdateEventArgs.Actress;
                        downLoadActress.State = DownLoadState.Completed;
                    });
                }
                catch (TaskCanceledException ex) { Logger.LogE(ex); }

            };


        }



        private void ProgressBar_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ProgressBar PB = sender as ProgressBar;
            if (PB.Value + PB.LargeChange <= PB.Maximum)
            {
                PB.Value += PB.LargeChange;
            }
            else
            {
                PB.Value = PB.Minimum;
            }
        }

        private void DelLabel(object sender, MouseButtonEventArgs e)
        {
            Border border = sender as Border;
            StackPanel stackPanel = border.Parent as StackPanel;

            Console.WriteLine(stackPanel.Parent.GetType().ToString());

        }
        


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.WindowState != WindowState.Minimized)
            {
                if (this.WindowState == WindowState.Normal) WinState = JvedioWindowState.Normal;
                else if (this.WindowState == WindowState.Maximized) WinState = JvedioWindowState.FullScreen;
                else if (this.Width == SystemParameters.WorkArea.Width & this.Height == SystemParameters.WorkArea.Height) WinState = JvedioWindowState.Maximized;

                WindowConfig cj = new WindowConfig(this.GetType().Name);
                Rect rect = new Rect(this.Left, this.Top, this.Width, this.Height);
                cj.Save(rect, WinState);
            }
            Properties.Settings.Default.EditMode = false;
            Properties.Settings.Default.ActorEditMode = false;
            Properties.Settings.Default.Save();

            if (Properties.Settings.Default.CloseToTaskBar & this.IsVisible == true)
            {
                e.Cancel = true;
                notifyIcon.Visible = true;
                this.Hide();
                WindowSet?.Hide();
            }


        }

        private void ActorTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (vieModel.TotalActorPage <= 1) return;
            if (e.Key == Key.Enter)
            {
                string pagestring = ((TextBox)sender).Text;
                int page = 1;
                if (pagestring == null) { page = 1; }
                else
                {
                    var isnumeric = int.TryParse(pagestring, out page);
                }
                if (page > vieModel.TotalActorPage) { page = vieModel.TotalActorPage; } else if (page <= 0) { page = 1; }
                vieModel.CurrentActorPage = page;
                vieModel.ActorFlipOver();
            }
        }

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (vieModel.TotalPage <= 1) return;
            if (e.Key == Key.Enter)
            {
                string pagestring = ((TextBox)sender).Text;
                int page = 1;
                if (pagestring == null) { page = 1; }
                else
                {
                    var isnumeric = int.TryParse(pagestring, out page);
                }
                if (page > vieModel.TotalPage) { page = vieModel.TotalPage; } else if (page <= 0) { page = 1; }
                vieModel.CurrentPage = page;
                vieModel.FlipOver();
            }
        }


        public void StopDownLoad()
        {
            Console.WriteLine("停止下载");
            DownLoader?.CancelDownload();
            downLoadActress?.CancelDownload();
            this.Dispatcher.BeginInvoke((Action)delegate
            {
                ProgressBar.Visibility = Visibility.Hidden;

            });

        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            tb.SelectAll();
        }


        private async void PreviousActorPage(object sender, MouseButtonEventArgs e)
        {
            if (vieModel.TotalActorPage <= 1) return;
            if (vieModel.CurrentActorPage - 1 <= 0)
                vieModel.CurrentActorPage = vieModel.TotalActorPage;
            else
                vieModel.CurrentActorPage -= 1;
            vieModel.ActorFlipOver();

            await Task.Delay(1);
            ActorSetSelected();
        }

        private async void NextActorPage(object sender, MouseButtonEventArgs e)
        {
            if (vieModel.TotalActorPage <= 1) return;
            if (vieModel.CurrentActorPage + 1 > vieModel.TotalActorPage)
                vieModel.CurrentActorPage = 1;
            else
                vieModel.CurrentActorPage += 1;

            vieModel.ActorFlipOver();
            await Task.Delay(1);
            ActorSetSelected();
        }



        private void PreviousPage(object sender, MouseButtonEventArgs e)
        {
            if (vieModel.TotalPage <= 1) return;
            if (vieModel.CurrentPage - 1 <= 0)
                vieModel.CurrentPage = vieModel.TotalPage;
            else
                vieModel.CurrentPage -= 1;
            vieModel.FlipOver();
            SetSelected();
        }

        private void NextPage(object sender, MouseButtonEventArgs e)
        {
            if (vieModel.TotalPage <= 1) return;
            if (vieModel.CurrentPage + 1 > vieModel.TotalPage)
                vieModel.CurrentPage = 1;
            else
                vieModel.CurrentPage += 1;
            vieModel.FlipOver();
            SetSelected();
        }






        private void Image_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            ((Image)sender).Source = new BitmapImage(new Uri("/Resources/Picture/NoPrinting_B.jpg", UriKind.Relative));
        }


        private void ActorGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.A & Properties.Settings.Default.ActorEditMode)
            {
                foreach (var item in vieModel.ActorList)
                {
                    if (!SelectedActress.Contains(item))
                    {
                        SelectedActress.Add(item);

                    }
                }
                ActorSetSelected();
            }
        }


        private void Grid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.A & Properties.Settings.Default.EditMode)
            {
                foreach (var item in vieModel.CurrentMovieList)
                {
                    if (!vieModel.SelectedMovie.Contains(item))
                    {
                        vieModel.SelectedMovie.Add(item);

                    }
                }
                SetSelected();
            }

        }

        public void StopDownLoadActress(object sender, RoutedEventArgs e)
        {
            DownloadActorPopup.IsOpen = false;
            downLoadActress?.CancelDownload();
            new PopupWindow(this, "已停止所有任务").Show();
        }

        public void DownLoadSelectedActor(object sender, RoutedEventArgs e)
        {
            if (downLoadActress?.State == DownLoadState.DownLoading)
            {
                new PopupWindow(this, "已有任务在下载！").Show(); return;
            }

            if (!Properties.Settings.Default.ActorEditMode) SelectedActress.Clear();
            MenuItem mnu = sender as MenuItem;
            StackPanel sp = null;
            if (mnu != null)
            {
                sp = ((ContextMenu)mnu.Parent).PlacementTarget as StackPanel;
                var TB = sp.Children.OfType<TextBox>().First();
                string name = TB.Text.Split('(')[0];
                Actress CurrentActress = GetActressFromVieModel(name);
                if (!SelectedActress.Select(g => g.name).ToList().Contains(CurrentActress.name)) SelectedActress.Add(CurrentActress);
                StartDownLoadActor(SelectedActress);

            }
            if (!Properties.Settings.Default.ActorEditMode) SelectedActress.Clear();
        }

        public void SelectAllActor(object sender, RoutedEventArgs e)
        {
            if (Properties.Settings.Default.ActorEditMode) { ActorCancelSelect(); return; }
            Properties.Settings.Default.ActorEditMode = true;
            foreach (var item in vieModel.CurrentActorList)
                if (!SelectedActress.Contains(item)) SelectedActress.Add(item);

            ActorSetSelected();
        }

        public void ActorCancelSelect()
        {
            Properties.Settings.Default.ActorEditMode = false; SelectedActress.Clear(); ActorSetSelected();
        }

        public void RefreshCurrentActressPage(object sender, RoutedEventArgs e)
        {
            ActorCancelSelect();
            vieModel.RefreshActor();
        }

        public void StartDownLoadActor(List<Actress> actresses)
        {
            if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + "BusActress.sqlite"))return;

            downLoadActress = new DownLoadActress(actresses);
            downLoadActress?.BeginDownLoad();
            try
            {
                downLoadActress.InfoUpdate += (s, ev) =>
                {
                    ActressUpdateEventArgs actressUpdateEventArgs = ev as ActressUpdateEventArgs;
                    for (int i = 0; i < vieModel.ActorList.Count; i++)
                    {
                        if (vieModel.ActorList[i].name == actressUpdateEventArgs.Actress.name)
                        {
                            try
                            {
                                Dispatcher.Invoke((Action)delegate ()
                                {
                                    vieModel.ActorList[i] = actressUpdateEventArgs.Actress;
                                    ProgressBar.Value = actressUpdateEventArgs.progressBarUpdate.value / actressUpdateEventArgs.progressBarUpdate.maximum * 100; ProgressBar.Visibility = Visibility.Visible;
                                    if (ProgressBar.Value == ProgressBar.Maximum) downLoadActress.State = DownLoadState.Completed;
                                    if (ProgressBar.Value == ProgressBar.Maximum | actressUpdateEventArgs.state == DownLoadState.Fail | actressUpdateEventArgs.state == DownLoadState.Completed) { ProgressBar.Visibility = Visibility.Hidden; }
                                });
                            }
                            catch (TaskCanceledException ex) { Logger.LogE(ex); }
                            break;
                        }
                    }
                };
        }
            catch(Exception e) { Console.WriteLine(e.Message); }



}


        DownLoadActress downLoadActress;
        public void StartDownLoadActress(object sender, RoutedEventArgs e)
        {
            DownloadActorPopup.IsOpen = false;

            if (DownLoader?.State == DownLoadState.DownLoading)
                new PopupWindow(this, "已有任务在下载！").Show();
            else
                StartDownLoadActor(vieModel.ActorList.ToList());



        }


        private void Grid_Actor_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(bool)e.NewValue) { downLoadActress?.CancelDownload(); ProgressBar.Visibility = Visibility.Hidden; }
        }

        private void ProgressBar_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {

        }

        public void ShowSubsection(object sender, MouseButtonEventArgs e)
        {
            Image image = sender as Image;
            var grid = image.Parent as Grid;
            Canvas canvas = grid.Children.OfType<Canvas>().First();
            if (canvas.Visibility == Visibility.Hidden)
                canvas.Visibility = Visibility.Visible;
            else
                canvas.Visibility = Visibility.Hidden;

        }


        private void Grid_Drop(object sender, DragEventArgs e)
        {
            //分为文件夹和文件
            string[] dragdropFiles = (string[])e.Data.GetData(DataFormats.FileDrop);
            List<string> files = new List<string>();
            StringCollection stringCollection = new StringCollection();
            foreach (var item in dragdropFiles)
            {
                if (IsFile(item))
                    files.Add(item);
                else
                    stringCollection.Add(item);
            }
            List<string> filepaths = new List<string>();
            //扫描导入
            if (stringCollection.Count > 0)
                filepaths = Scan.ScanPaths(stringCollection, new CancellationToken());

            if (files.Count > 0) filepaths.AddRange(files);
            double num = Scan.DistinctMovieAndInsert(filepaths, new CancellationToken());
            new PopupWindow(this, $"总计导入{num}个文件").Show();
        }



        private void Grid_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Link;
            e.Handled = true;//必须加
        }

        private void Button_StopDownload(object sender, RoutedEventArgs e)
        {
            DownloadPopup.IsOpen = false;
            StopDownLoad();
            new PopupWindow(this, "停止下载！").Show();
        }

        private void Button_StartDownload(object sender, RoutedEventArgs e)
        {
            DownloadPopup.IsOpen = false;
            if (DownLoader?.State == DownLoadState.DownLoading)
                new PopupWindow(this, "已有任务在下载！").Show();
            else
                StartDownload(vieModel.CurrentMovieList.ToList());
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Loadslide();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            CheckUpdate();
        }

        private void OpenUpdate(object sender, RoutedEventArgs e)
        {
            if (new Msgbox(this, "是否关闭程序开始更新？").ShowDialog() == true)
            {
                Process.Start(AppDomain.CurrentDomain.BaseDirectory + "JvedioUpdate.exe");
                this.Close();
            }
        }

        private void DoubleAnimation_Completed(object sender, EventArgs e)
        {
            Border border = sender as Border;
            border.Opacity = 1;
        }



        public void ShowSettingsPopup(object sender, MouseButtonEventArgs e)
        {
            if (SettingsContextMenu.IsOpen)
                SettingsContextMenu.IsOpen = false;
            else
            {
                SettingsContextMenu.IsOpen = true;
                SettingsContextMenu.PlacementTarget = SettingsBorder;
                SettingsContextMenu.Placement = PlacementMode.Bottom;
            }

        }

        public void ShowSkinPopup(object sender, MouseButtonEventArgs e)
        {
            if (SkinPopup.IsOpen)
                SkinPopup.IsOpen = false;
            else
                SkinPopup.IsOpen = true;
        }


        private void Window_ContentRendered(object sender, EventArgs e)
        {

            if (Properties.Settings.Default.FirstRun)
            {
                BeginScanStackPanel.Visibility = Visibility.Visible;
                Properties.Settings.Default.FirstRun = false;
            }


            if (Properties.Settings.Default.Opacity_Main >= 0.5)
                this.Opacity = Properties.Settings.Default.Opacity_Main;
            else
                this.Opacity = 1;


            SetSkin();

            //监听文件改动
            if (Properties.Settings.Default.ListenAllDir)
            {
                try { AddListen(); }
                catch (Exception ex) { Logger.LogE(ex); }
            }

            //显示公告
            ShowNotice();


            //检查更新
            if (Properties.Settings.Default.AutoCheckUpdate) CheckUpdate();


            //检查网络连接
                

            this.Cursor = Cursors.Arrow;

            ScrollViewer.Focus();


            //设置当前数据库
            for (int i = 0; i < vieModel.DataBases.Count; i++)
            {
                if (vieModel.DataBases[i].ToLower() == Properties.Settings.Default.DataBasePath.Split('\\').Last().Split('.').First().ToLower())
                {
                    DatabaseComboBox.SelectedIndex = i;
                    break;
                }
            }

            if (vieModel.DataBases.Count == 1) DatabaseComboBox.Visibility = Visibility.Hidden;
            CheckurlTimer.Start();
            BeginCheckurlThread();

        }

        public void SetSkin()
        {
            if (Properties.Settings.Default.Themes == "黑色")
            {
                Application.Current.Resources["BackgroundTitle"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22252A"));
                Application.Current.Resources["BackgroundMain"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#393D40"));
                Application.Current.Resources["BackgroundSide"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#323639"));
                Application.Current.Resources["BackgroundTab"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#383838"));
                Application.Current.Resources["BackgroundSearch"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#18191B"));
                Application.Current.Resources["BackgroundMenu"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("Black"));
                Application.Current.Resources["ForegroundGlobal"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AFAFAF"));
                Application.Current.Resources["ForegroundSearch"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
                Application.Current.Resources["BorderBursh"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("Transparent"));
                SideBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#323639"));
                TopBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22252A"));
            }
            else if (Properties.Settings.Default.Themes == "白色")
            {
                Application.Current.Resources["BackgroundTitle"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1D1D1"));
                Application.Current.Resources["BackgroundMain"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("White"));
                Application.Current.Resources["BackgroundSide"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5E5E5"));
                Application.Current.Resources["BackgroundTab"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF5EE"));
                Application.Current.Resources["BackgroundSearch"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1D1D1"));
                Application.Current.Resources["BackgroundMenu"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("White"));
                Application.Current.Resources["ForegroundGlobal"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#555555"));
                Application.Current.Resources["ForegroundSearch"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#000000"));
                Application.Current.Resources["BorderBursh"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("Gray"));

                SideBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5E5E5"));
                TopBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1D1D1"));
            }
            else if (Properties.Settings.Default.Themes == "蓝色")

            {
                Application.Current.Resources["BackgroundTitle"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0B72BD"));
                Application.Current.Resources["BackgroundMain"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2BA2D2"));
                Application.Current.Resources["BackgroundSide"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#61AEDA"));
                Application.Current.Resources["BackgroundTab"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3DBEDE"));
                Application.Current.Resources["BackgroundSearch"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("White"));
                Application.Current.Resources["BackgroundMenu"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("LightBlue"));
                Application.Current.Resources["ForegroundGlobal"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("White"));
                Application.Current.Resources["ForegroundSearch"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#555555"));
                Application.Current.Resources["BorderBursh"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#95DCED"));


                //设置侧边栏渐变

                LinearGradientBrush myLinearGradientBrush = new LinearGradientBrush();
                myLinearGradientBrush.StartPoint = new Point(0.5, 0);
                myLinearGradientBrush.EndPoint = new Point(0.5, 1);
                myLinearGradientBrush.GradientStops.Add(new GradientStop(Color.FromRgb(62, 191, 223), 0));
                myLinearGradientBrush.GradientStops.Add(new GradientStop(Color.FromRgb(11, 114, 189), 1));
                SideBorder.Background = myLinearGradientBrush;

                LinearGradientBrush myLinearGradientBrush2 = new LinearGradientBrush();
                myLinearGradientBrush2.MappingMode = BrushMappingMode.RelativeToBoundingBox;
                myLinearGradientBrush2.StartPoint = new Point(0, 0.5);
                myLinearGradientBrush2.EndPoint = new Point(1, 0);
                myLinearGradientBrush2.GradientStops.Add(new GradientStop(Color.FromRgb(11, 114, 189), 1));
                myLinearGradientBrush2.GradientStops.Add(new GradientStop(Color.FromRgb(62, 191, 223), 0));
                TopBorder.Background = myLinearGradientBrush2;

            }
            
        }

        private void SetSkinProperty(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Themes = ((Button)sender).Content.ToString();
            Properties.Settings.Default.Save();
            SetSkin();
            SetSelected();
            ActorSetSelected();
        }


        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            vieModel.SelectedMovie.Clear();
            SetSelected();
        }


        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            new Msgbox(this, "123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf123123sdfsdfsdf").ShowDialog();


        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            OpenTools(sender, e);
        }

        private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.F)
            {
                //高级检索
                if (windowSearch != null) { windowSearch.Close(); }
                windowSearch = new WindowSearch();
                windowSearch.Show();
            }
            else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.Right)
            {
                //末页
                if (Grid_GAL.Visibility == Visibility.Hidden)
                {
                    vieModel.CurrentPage = vieModel.TotalPage;
                    vieModel.FlipOver();
                    SetSelected();
                }
                else
                {
                    vieModel.CurrentActorPage = vieModel.TotalActorPage;
                    vieModel.ActorFlipOver();
                    ActorSetSelected();
                }

            }
            else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.Left)
            {
                //首页
                if (Grid_GAL.Visibility == Visibility.Hidden)
                {
                    vieModel.CurrentPage = 1;
                    vieModel.FlipOver();
                    SetSelected();
                }

                else
                {
                    vieModel.CurrentActorPage = 1;
                    vieModel.ActorFlipOver();
                    ActorSetSelected();
                }

            }
            else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.Up)
            {
                //回到顶部
                ScrollViewer.ScrollToTop();
            }
            else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.Down)
            {
                //滑倒底端
                ScrollToEnd(sender, new RoutedEventArgs());
            }
            else if (Grid_GAL.Visibility == Visibility.Hidden && e.Key == Key.Right)
                NextPage(sender, new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice, 0, MouseButton.Left));
            else if (Grid_GAL.Visibility == Visibility.Hidden && e.Key == Key.Left)
                PreviousPage(sender, new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice, 0, MouseButton.Left));
            else if (Grid_GAL.Visibility == Visibility.Visible && e.Key == Key.Right)
                NextActorPage(sender, new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice, 0, MouseButton.Left));
            else if (Grid_GAL.Visibility == Visibility.Visible && e.Key == Key.Left)
                PreviousActorPage(sender, new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice, 0, MouseButton.Left));




        }

        private void Window_Activated(object sender, EventArgs e)
        {
            AllSearchTextBox.Focus();
        }

        private void DatabaseComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0) return;
            if (e.AddedItems[0].ToString().ToLower() != Properties.Settings.Default.DataBasePath.Split('\\').Last().Split('.').First().ToLower())
            {
                if(e.AddedItems[0].ToString()=="info")
                    Properties.Settings.Default.DataBasePath = AppDomain.CurrentDomain.BaseDirectory + $"{e.AddedItems[0].ToString()}.sqlite";
                else
                    Properties.Settings.Default.DataBasePath = AppDomain.CurrentDomain.BaseDirectory + $"DataBase\\{e.AddedItems[0].ToString()}.sqlite";
                //切换数据库
                vieModel.Reset();
                AllRB.IsChecked = true;


            }
        }
    }

    public class DownLoadProgress
    {
        public double maximum = 0;
        public double value = 0;
        public object lockobject;

    }



    public class BoolToVisibilityConverter : IValueConverter
    {
        //数字转换为选中项的地址
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value) return Visibility.Visible; else return Visibility.Collapsed;
        }


        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }


    }

    public class IntToCheckedConverter : IValueConverter
    {
        //数字转换为选中项的地址
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null | parameter == null) { return false; }
            int intparameter = int.Parse(parameter.ToString());
            if ((int)value == intparameter)
                return true;
            else
                return false;
        }

        //选中项地址转换为数字

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null | parameter == null) return 0;
            int intparameter = int.Parse(parameter.ToString());
            return intparameter;
        }


    }


    public class StringToCheckedConverter : IValueConverter
    {
        //判断是否相同
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value.ToString() == parameter.ToString()) { return true; } else { return false; }
        }

        //选中项地址转换为数字

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return parameter.ToString();
        }


    }

    public class SearchTypeEnumConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return false;

            return (((MySearchType)value).ToString() == parameter.ToString());
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (bool)value ? Enum.Parse(typeof(MySearchType), parameter.ToString(), true) : null;
        }
    }

    public enum MySearchType { 识别码, 名称, 演员 }





    public class ViewTypeEnumConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return false;

            return (((MyViewType)value).ToString() == parameter.ToString());
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (bool)value ? Enum.Parse(typeof(MyViewType), parameter.ToString(), true) : null;
        }
    }

    public enum MyViewType { 默认, 有图, 无图 }



    public class StringToUriStringConverterMain : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value.ToString() == "黑色")
                return $"Resources/Skin/black/{parameter.ToString()}.png";
            else if (value.ToString() == "白色")
                return $"Resources/Skin/white/{parameter.ToString()}.png";
            else if (value.ToString() == "蓝色")
                return $"Resources/Skin/black/{parameter.ToString()}.png";
            else
                return $"Resources/Skin/black/{parameter.ToString()}.png";
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }


    public class StringToUriStringConverterOther : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value.ToString() == "黑色")
                return $"pack://application:,,,/Resources/Skin/black/{parameter.ToString()}.png";
            else if (value.ToString() == "白色")
                return $"pack://application:,,,/Resources/Skin/white/{parameter.ToString()}.png";
            else if (value.ToString() == "蓝色")
                return $"pack://application:,,,/Resources/Skin/black/{parameter.ToString()}.png";
            else
                return $"pack://application:,,,/Resources/Skin/black/{parameter.ToString()}.png";
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    public class ImageTypeEnumConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return false;

            return (((MyImageType)value).ToString() == parameter.ToString());
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (bool)value ? Enum.Parse(typeof(MyImageType), parameter.ToString(), true) : null;
        }
    }

    public enum MyImageType { 缩略图, 海报图, 预览图 }


    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value.ToString() == "预览图")
                return Visibility.Visible;
            else
                return Visibility.Collapsed;
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }




    public class MovieStampTypeToStringConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
            {
                return "";
            }
            else
            {
                MovieStampType movieStampType = (MovieStampType)value;
                if (movieStampType == MovieStampType.高清中字)
                {
                    return "高清中字";
                }
                else if (movieStampType == MovieStampType.无码流出)
                {
                    return "无码流出";
                }
                else
                {
                    return "无";

                }


            }



        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }


    public class MovieStampTypeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!Properties.Settings.Default.DisplayStamp) return Visibility.Hidden;

            if (value == null)
            {
                return Visibility.Hidden;
            }
            else
            {
                MovieStampType movieStampType = (MovieStampType)value;
                if (movieStampType == MovieStampType.无)
                {
                    return Visibility.Hidden;
                }
                else
                {
                    return Visibility.Visible;
                }


            }



        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }


    public static class GetWindow
    {
        public static Window Get(string name)
        {
            foreach (Window window in App.Current.Windows)
            {
                if (window.GetType().Name.ToUpper() == name.ToUpper()) return window;
            }
            return null;
        }
    }


    public class DownLoadActress
    {

        public event EventHandler InfoUpdate;
        public DownLoadProgress downLoadProgress;
        private Semaphore Semaphore;
        private ProgressBarUpdate ProgressBarUpdate;
        private bool Cancel { get; set; }
        public DownLoadState State;

        public List<Actress> ActorList { get; set; }

        public DownLoadActress(List<Actress> actresses)
        {
            ActorList = actresses;
            Cancel = false;
            Semaphore = new Semaphore(3, 3);
            ProgressBarUpdate = new ProgressBarUpdate() { value = 0, maximum = 1 };
        }

        public void CancelDownload()
        {
            Cancel = true;
            State = DownLoadState.Fail;
        }

        public void BeginDownLoad()
        {
            if (ActorList.Count == 0) { this.State = DownLoadState.Completed; return; }


            //先根据 BusActress.sqlite 获得 id
            DataBase cdb = new DataBase("BusActress");
            List<Actress> actresslist = new List<Actress>();
            foreach (Actress item in ActorList)
            {
                if (item.smallimage == null || item.birthday == null)
                {
                    Actress actress = item;
                    if (item.id == "")
                    {

                        actress.id = cdb.GetInfoBySql($"select id from censored where name='{item.name}'");
                        if (item.imageurl == null) { actress.imageurl = cdb.GetInfoBySql($"select smallpicurl from censored where id='{actress.id}'"); }
                    }
                    else
                    {
                        if (item.imageurl == null) { actress.imageurl = cdb.GetInfoBySql($"select smallpicurl from censored where id='{actress.id}'"); }
                    }

                    actresslist.Add(actress);
                }
            }
            cdb.CloseDB();

            ProgressBarUpdate.maximum = actresslist.Count;
            for (int i = 0; i < actresslist.Count; i++)
            {
                Thread threadObject = new Thread(DownLoad);
                threadObject.Start(actresslist[i]);
            }
        }

        private async void DownLoad(object o)
        {
            try
            {
                Semaphore.WaitOne();
                Actress actress = o as Actress;
                if (Cancel | actress.id == "") return;
                this.State = DownLoadState.DownLoading;

                //下载头像
                if (!string.IsNullOrEmpty(actress.imageurl))
                {
                    string url = actress.imageurl;
                    byte[] imageBytes = null; string cookies = "";
                    imageBytes = await Task.Run(() =>
                    {
                        (imageBytes, cookies) = Net.DownLoadFile(url, Host: "pics.javcdn.pw");
                        return imageBytes;
                    });
                    if (imageBytes != null)
                    {

                        StaticClass.SaveImage(actress.name, imageBytes, ImageType.ActorImage, url);
                        actress.smallimage = StaticClass.GetBitmapImage(actress.name, "Actresses");

                    }

                }
                //下载信息
                bool success = false;
                success = await Task.Run(() =>
                {
                    Task.Delay(300).Wait();
                    return Net.DownActress(actress.id, actress.name);
                });

                if (success)
                {
                     DataBase cdb = new DataBase();
                    actress = cdb.SelectInfoFromActress(actress);
                    cdb.CloseDB();
                }

                ProgressBarUpdate.value += 1;
                InfoUpdate?.Invoke(this, new ActressUpdateEventArgs() { Actress = actress, progressBarUpdate = ProgressBarUpdate, state = State });
            }
            finally
            {
                Semaphore.Release();
            }
        }
    }

    public class ActressUpdateEventArgs : EventArgs
    {
        public Actress Actress;
        public ProgressBarUpdate progressBarUpdate;
        public DownLoadState state;
    }

    public class ProgressBarUpdate
    {
        public double value;
        public double maximum;
    }


    public class CloseEventArgs : EventArgs
    {
        public bool IsExitApp = true;
    }

    public static class GetBounds
    {
        public static Rect BoundsRelativeTo(this FrameworkElement element, Visual relativeTo)
        {
            return element.TransformToVisual(relativeTo).TransformBounds(new Rect(element.RenderSize));
        }

        public static T GetVisualChild<T>(DependencyObject parent) where T : Visual
        {
            T child = default(T);

            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child == null)
                {
                    child = GetVisualChild<T>(v);
                }
                if (child != null)
                {
                    break;
                }
            }
            return child;
        }


    }


    
    public class PlusToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value.ToString() == " + ")
                return Visibility.Collapsed;
            else
                return Visibility.Visible;

        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }


    public class LabelToListConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return " + ";
            List<string> result= value.ToString().Split(' ').ToList();
            result.Insert(0, " + ");
            return result;

        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return "";
            List<string> vs = value as List<string>;
            vs.Remove(" + ");
            return string.Join(" ", vs);
        }
    }

    public class OutFlowConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {

            if (!Properties.Settings.Default.DisplayStamp) return Visibility.Collapsed;
            if (value == null) return Visibility.Collapsed;


            if (Identify.IsFlowOut(value.ToString()))
                return Visibility.Visible;
            else
                return Visibility.Collapsed;



        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    public class CHSConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {

            if (!Properties.Settings.Default.DisplayStamp) return Visibility.Collapsed;
            if (value == null) return Visibility.Collapsed;


            if (Identify.IsCHS(value.ToString()))
                return Visibility.Visible;
            else
                return Visibility.Collapsed;

        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }



    public class HDVConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {

            if (!Properties.Settings.Default.DisplayStamp) return Visibility.Collapsed;
            if (value == null) return Visibility.Collapsed;


            if (Identify.IsHDV(value.ToString()))
                return Visibility.Visible;
            else
                return Visibility.Collapsed;

        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    public class FontFamilyConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {

            if (value == null || value.ToString() == "") return "宋体";

            return value.ToString();

        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }


}
