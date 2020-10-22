

using Jvedio.ViewModel;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static Jvedio.StaticVariable;
using static Jvedio.StaticClass;
using System.Windows.Controls.Primitives;

namespace Jvedio
{
    /// <summary>
    /// WindowDetails.xaml 的交互逻辑
    /// </summary>
    public partial class WindowDetails : Window
    {


        public VieModel_Details vieModel;
        public Point WindowPoint = new Point(100, 100);
        public Size WindowSize = new Size(1200, 700);
        public JvedioWindowState WinState = JvedioWindowState.Normal;

        public DetailDownLoad DetailDownLoad;

        public WindowDetails(string movieid = "")
        {
            //movieid = "IPX-163";
            InitializeComponent();
            if (movieid != "")
            {
                vieModel = new VieModel_Details();
                vieModel.Query(movieid);
                this.DataContext = vieModel;
                vieModel.QueryCompletedHandler += (s, e) =>
                {
                    BigImage.Source = vieModel.DetailMovie.bigimage;
                };

            }
            else { this.DataContext = null; }

            AdjustWindow();

            FatherGrid.Focus();

            SetSkin();

            SetImage(0);

        }





        public void SetSkin()
        {
            switch (Properties.Settings.Default.Themes)
            {
                case "蓝色":
                    //设置渐变
                    LinearGradientBrush myLinearGradientBrush = new LinearGradientBrush();
                    myLinearGradientBrush.StartPoint = new Point(0.5, 0);
                    myLinearGradientBrush.EndPoint = new Point(0.5, 1);
                    myLinearGradientBrush.GradientStops.Add(new GradientStop(Color.FromRgb(62, 191, 223), 1));
                    myLinearGradientBrush.GradientStops.Add(new GradientStop(Color.FromRgb(11, 114, 189), 0));
                    BackBorder.Background = myLinearGradientBrush;

                    break;
            }
        }


        public void ActorMouseMove(object sender, RoutedEventArgs e)
        {
            ActorCanvas.Visibility = Visibility.Visible;

            Point MousePoistion = Mouse.GetPosition(InfoGrid);
            Canvas.SetLeft(ActorGrid, MousePoistion.X - 40);
            Canvas.SetTop(ActorGrid, MousePoistion.Y + 30);
        }

        public void ShowActor(object sender, RoutedEventArgs e)
        {
            Border border = sender as Border;
            StackPanel stackPanel = border.Child as StackPanel;
            TextBlock textBlock = stackPanel.Children[0] as TextBlock;

            string imagePath = BasePicPath + $"Actresses\\{textBlock.Text}.jpg";
            if (File.Exists(imagePath))
                ActorImage.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(imagePath, UriKind.Absolute));
            else
                ActorImage.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("/Resources/Picture/NoPrinting_A.jpg", UriKind.Relative));

        }

        public void HideActor(object sender, RoutedEventArgs e)
        {
            ActorCanvas.Visibility = Visibility.Hidden;
        }

        public void DownLoad(object sender, RoutedEventArgs e)
        {
            if (!CheckBeforeDownload())
            {
                HandyControl.Controls.Growl.Error("请在设置【同步信息】中添加服务器源并启用！", "DetailsGrowl");

            }
            else
            {
                if (DetailDownLoad == null)
                {
                    Task.Run(() => { StartDownload(); });
                }
                else
                {
                    if (!DetailDownLoad.IsDownLoading)
                    {
                        Task.Run(() => { StartDownload(); });
                    }
                }
            }
        }

        public void GetScreenShot(object sender, RoutedEventArgs e)
        {

            if (!File.Exists(Properties.Settings.Default.FFMPEG_Path)) { HandyControl.Controls.Growl.Info("请设置 ffmpeg.exe 的路径 ", "DetailsGrowl"); return; }
            MenuItem mnu = ((MenuItem)(sender)).Parent as MenuItem;
            StackPanel sp = null;

            if (mnu != null)
            {
                DetailMovie movie = vieModel.DetailMovie;
                
                if (!File.Exists(movie.filepath)) { HandyControl.Controls.Growl.Error("视频不存在", "DetailsGrowl"); return; }

                try { ScreenShot(movie); } catch (Exception ex) { Logger.LogF(ex); }
            }
           
            if (Properties.Settings.Default.ScreenShotToExtraPicPath)
                HandyControl.Controls.Growl.Info("开始截图到【预览图】", "DetailsGrowl");
            else
                HandyControl.Controls.Growl.Warning("开始截图到【影片截图】", "DetailsGrowl");

        }

        public Semaphore SemaphoreScreenShot;
        public int TotalSSNum = 0;
        public int CurrentSSNum = 0;
        public void ScreenShot(Movie movie)
        {
            // n 个线程截图
            if (!File.Exists(Properties.Settings.Default.FFMPEG_Path)) return;

            int num = Properties.Settings.Default.ScreenShot_ThreadNum;
            string ScreenShotPath = "";
            if (Properties.Settings.Default.ScreenShotToExtraPicPath)
                ScreenShotPath = BasePicPath + "ExtraPic\\" + movie.id;
            else
                ScreenShotPath = BasePicPath + "ScreenShot\\" + movie.id;

            if (!Directory.Exists(ScreenShotPath)) Directory.CreateDirectory(ScreenShotPath);



            string[] cutoffArray = MediaParse.GetCutOffArray(movie.filepath); //获得影片长度数组
            SemaphoreScreenShot = new Semaphore(cutoffArray.Count(), cutoffArray.Count());
            TotalSSNum = cutoffArray.Count();
            CurrentSSNum = 0;
            for (int i = 0; i < cutoffArray.Count(); i++)
            {
                List<object> list = new List<object>() { cutoffArray[i], i.ToString(), movie.filepath, ScreenShotPath };
                Thread threadObject = new Thread(BeginScreenShot);
                threadObject.Start(list);
            }
            HandyControl.Controls.Growl.Info($"已启用 {cutoffArray.Count()} 个线程， 3-10S 后即可截图成功\n", "DetailsGrowl");
        }

        public void BeginScreenShot(object o)
        {
            App.Current.Dispatcher.Invoke((Action)delegate { this.Cursor = Cursors.Wait; });
            
            List<object> list = o as List<object>;
            string cutoffTime = list[0] as string;
            string i = list[1] as string;
            string filePath = list[2] as string;
            string ScreenShotPath = list[3] as string;

            if (string.IsNullOrEmpty(cutoffTime)) return;
            SemaphoreScreenShot.WaitOne();
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.UseShellExecute = false;    //是否使用操作系统shell启动
            p.StartInfo.RedirectStandardInput = true;//接受来自调用程序的输入信息
            p.StartInfo.RedirectStandardOutput = true;//由调用程序获取输出信息
            p.StartInfo.RedirectStandardError = true;//重定向标准错误输出
            p.StartInfo.CreateNoWindow = true;//不显示程序窗口
            p.Start();//启动程序

            string outputfile= $"{ ScreenShotPath }\\ScreenShot -{ i.PadLeft(2, '0')}.jpg";
            string str = $"\"{Properties.Settings.Default.FFMPEG_Path}\" -y -threads 1 -ss {cutoffTime} -i \"{filePath}\" -f image2 -frames:v 1 \"{outputfile}\"";

            p.StandardInput.WriteLine(str + "&exit");
            p.StandardInput.AutoFlush = true;
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();//等待程序执行完退出进程
            p.Close();
            SemaphoreScreenShot.Release();
            CurrentSSNum++;
            App.Current.Dispatcher.Invoke((Action)delegate { this.Cursor = Cursors.Arrow;  });

            if (CurrentSSNum == TotalSSNum)
            {
                try
                {
                    App.Current.Dispatcher.Invoke((Action)delegate {
                        vieModel.Query(vieModel.DetailMovie.id);
                        SetImage(0);
                    });
                }
                catch { }

            }


        }


        public void StartDownload()
        {
            List<string> urlList = new List<string>();
            foreach (var item in vieModel.DetailMovie.extraimageurl?.Split(';')) { if (!string.IsNullOrEmpty(item)) urlList.Add(item); }
            if (vieModel.DetailMovie.extraimagelist.Count >= urlList.Count & vieModel.DetailMovie.bigimage != null & vieModel.DetailMovie.title != "") return;



            //添加到下载列表
            DetailDownLoad = new DetailDownLoad(vieModel.DetailMovie);
            DetailDownLoad.DownLoad();
            Dispatcher.Invoke((Action)delegate () { ProgressBar.Value = 0; ProgressBar.Visibility = Visibility.Visible; });

            //监听取消下载：
            DetailDownLoad.CancelEvent += (s, e) => { Dispatcher.Invoke((Action)delegate () { ProgressBar.Visibility = Visibility.Hidden; }); };

            //更新 UI
            DetailDownLoad.InfoUpdate += (s, e) =>
            {
                Dispatcher.Invoke((Action)delegate ()
                {
                    DetailMovieEventArgs eventArgs = e as DetailMovieEventArgs;
                    ProgressBar.Value = (eventArgs.value / eventArgs.maximum) * 100; ProgressBar.Visibility = Visibility.Visible;
                    if (ProgressBar.Value == ProgressBar.Maximum) ProgressBar.Visibility = Visibility.Hidden;
                    //判断是否是当前番号
                    if (vieModel.DetailMovie.id == eventArgs.DetailMovie.id) vieModel.Query(eventArgs.DetailMovie.id);
                });
            };


        }

        public void StopDownLoad()
        {
            if (DetailDownLoad != null && DetailDownLoad.IsDownLoading == true) HandyControl.Controls.Growl.Warning("已取消同步！", "DetailsGrowl");
            DetailDownLoad?.CancelDownload();
        }

        public void AdjustWindow()
        {
            //读取窗体设置
            WindowConfig cj = new WindowConfig(this.GetType().Name);
            Rect rect;
            (rect, WinState) = cj.GetValue();
            if (rect.X != -1 && rect.Y != -1)
            {
                this.WindowState = WindowState.Normal;
                this.Left = rect.X > 0 ? rect.X : 0;
                this.Top = rect.Y > 0 ? rect.Y : 0;
                this.Height = rect.Height <= SystemParameters.PrimaryScreenHeight ? rect.Height : SystemParameters.PrimaryScreenHeight / 2;
                this.Width = rect.Width <= SystemParameters.PrimaryScreenWidth ? rect.Width : SystemParameters.PrimaryScreenWidth / 2;
                if (this.Width == SystemParameters.WorkArea.Width | this.Height == SystemParameters.WorkArea.Height) { WinState = JvedioWindowState.Maximized; }
            }
            else
            {
                WinState = JvedioWindowState.Normal;
                this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }


        }



        public void ShowFavorites(object sender, MouseButtonEventArgs e)
        {
            //if (LikePopup.IsOpen)
            //    LikePopup.IsOpen = false;
            //else
            //    LikePopup.IsOpen = true;
            //LikePopup.Focus();
            //var siblings = HeartCanvas.Children;
            //var paths = siblings.OfType<System.Windows.Shapes.Path>().ToList();
            //for (int i = 1; i <= vieModel.DetailMovie.favorites; i++)
            //{
            //    paths[i].Fill = Brushes.Orange;
            //}
        }

        //显示预览图


        public void HeartEnter(object sender, RoutedEventArgs e)
        {

            System.Windows.Shapes.Path path = (System.Windows.Shapes.Path)sender;
            var siblings = ((sender as FrameworkElement).Parent as Canvas).Children;
            var paths = siblings.OfType<System.Windows.Shapes.Path>().ToList();

            if (Mouse.DirectlyOver != null)
            {
                if (paths.IndexOf(path) == 0) { path.Fill = Brushes.DarkGray; } else { paths[0].Fill = Brushes.Gray; }
                for (int i = 1; i <= paths.IndexOf(path); i++)
                {
                    paths[i].Fill = Brushes.Red;
                }
                for (int i = paths.IndexOf(path) + 1; i <= 5; i++)
                {
                    paths[i].Fill = Brushes.White;
                }
            }

        }

        public void CloseWindow(object sender, MouseEventArgs e)
        {
            this.Close();
        }



        Main windowMain;


        //显示类别
        public void ShowSameGenre(object sender, MouseButtonEventArgs e)
        {
            string genretext = ((Label)sender).Content.ToString();
            if (string.IsNullOrEmpty(genretext)) return;
            foreach (Window window in App.Current.Windows)
            {
                if (window.GetType().Name == "Main") { windowMain = (Main)window; break; }
            }
            if (windowMain != null)
            {
                windowMain.Genre_MouseDown(sender, e);
                this.Close();
            }

        }

        //显示演员
        public void ShowSameActor(object sender, MouseButtonEventArgs e)
        {
            Border border = sender as Border;
            StackPanel sp = border.Child as StackPanel;
            TextBlock textBlock = sp.Children.OfType<TextBlock>().First();
            string name = textBlock.Text.Split('(')[0];
            if (string.IsNullOrEmpty(name)) return;
            foreach (Window window in App.Current.Windows)
            {
                if (window.GetType().Name == "Main") { windowMain = (Main)window; break; }
            }
            if (windowMain != null)
            {
                Actress actress = null;
                foreach (Actress item in vieModel.DetailMovie.actorlist)
                {
                    if (name == item.name)
                    {
                        actress = item; break;
                    }
                }
                if (actress != null)
                {
                    windowMain.ShowActorMovieFromDetailWindow(actress);
                    this.Close();
                }

            }

        }

        //显示标签
        public void ShowSameLabel(object sender, MouseButtonEventArgs e)
        {

            string tagtext = ((HandyControl.Controls.Tag)sender).Content.ToString();

            if (string.IsNullOrEmpty(tagtext)) return;

            if (tagtext == "+")
            {
                //新增
                var r = new DialogInput(this, "请输入标签");
                if (r.ShowDialog() == true)
                {
                    string text = r.Text;
                    if (text != "" & text != "+" & text.IndexOf(" ") < 0)
                    {
                        if (!vieModel.DetailMovie.labellist.Contains(text))
                        {
                            vieModel.DetailMovie.labellist.Add(text);
                            vieModel.SaveLabel();
                            vieModel.Query(vieModel.DetailMovie.id);
                        }

                    }

                }
            }
            else
            {
                foreach (Window window in App.Current.Windows)
                {
                    if (window.GetType().Name == "Main") { windowMain = (Main)window; break; }
                }
                if (windowMain != null)
                {
                    windowMain.Label_MouseDown(sender, e);
                    this.Close();
                }
            }
        }

        //显示导演
        public void ShowSameDirector(object sender, MouseButtonEventArgs e)
        {
            string genretext = ((Label)sender).Content.ToString();
            if (string.IsNullOrEmpty(genretext)) return;
            foreach (Window window in App.Current.Windows)
            {
                if (window.GetType().Name == "Main") { windowMain = (Main)window; break; }
            }
            if (windowMain != null)
            {
                windowMain.Director_MouseDown(sender, e);
                this.Close();
            }

        }

        //显示发行商
        public void ShowSameStudio(object sender, MouseButtonEventArgs e)
        {
            string genretext = ((Label)sender).Content.ToString();
            if (string.IsNullOrEmpty(genretext)) return;
            foreach (Window window in App.Current.Windows)
            {
                if (window.GetType().Name == "Main") { windowMain = (Main)window; break; }
            }
            if (windowMain != null)
            {
                windowMain.Studio_MouseDown(sender, e);
                this.Close();
            }

        }

        //显示系列
        public void ShowSameTag(object sender, MouseButtonEventArgs e)
        {
            string genretext = ((Label)sender).Content.ToString();
            if (string.IsNullOrEmpty(genretext)) return;
            foreach (Window window in App.Current.Windows)
            {
                if (window.GetType().Name == "Main") { windowMain = (Main)window; break; }
            }
            if (windowMain != null)
            {
                windowMain.Tag_MouseDown(sender, e);
                this.Close();
            }
        }


        public WindowEdit WindowEdit;
        public void EditInfo(object sender, RoutedEventArgs e)
        {
            if (WindowEdit != null) { WindowEdit.Close(); }
            string id = vieModel.DetailMovie.id;
            Console.WriteLine(id);
            WindowEdit = new WindowEdit(id);


            WindowEdit.Loaded +=
                delegate (object _sender, RoutedEventArgs args)
                {
                    if (WindowEdit.IdListBox.Items.Count > 0)
                    {
                        for (int i = 0; i <= WindowEdit.IdListBox.Items.Count - 1; i++)
                        {
                            string movieid = WindowEdit.IdListBox.Items[i].ToString();
                            if (movieid.ToLower() == id.ToLower())
                            {
                                WindowEdit.IdListBox.SelectedItem = WindowEdit.IdListBox.Items[i];
                                WindowEdit.IdListBox.ScrollIntoView(WindowEdit.IdListBox.Items[i]);
                                break;
                            }
                        }
                    }
                };
            WindowEdit.Show();
        }



        public void PlayVedio(object sender, MouseEventArgs e)
        {

            if (vieModel.DetailMovie.hassubsection)
            {
                ShowSubsection(this, new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice, 0, MouseButton.Left));
            }
            else
            {
                string filepath = vieModel.DetailMovie.filepath;
                if (File.Exists(filepath))
                {
                    Main main = App.Current.Windows[0] as Main;
                    //使用默认播放器
                    if (!string.IsNullOrEmpty(Properties.Settings.Default.VedioPlayerPath) && File.Exists(Properties.Settings.Default.VedioPlayerPath))
                    {
                        try
                        {
                            Process.Start(Properties.Settings.Default.VedioPlayerPath, filepath);
                            main.vieModel.AddToRecentWatch(vieModel.DetailMovie.id);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogE(ex);
                            Process.Start(filepath);
                        }

                    }
                    else
                    {
                        Process.Start(filepath);
                        main.vieModel.AddToRecentWatch(vieModel.DetailMovie.id);
                    }
                }
                else
                    HandyControl.Controls.Growl.Info("无法打开 " + filepath, "DetailsGrowl");
            }
        }

        private void MoveWindow(object sender, MouseEventArgs e)
        {
            //移动窗口
            if (e.LeftButton == MouseButtonState.Pressed && WinState == JvedioWindowState.Normal)
            {
                this.DragMove();
            }

            //FatherGrid.Focus();
        }

        private void scrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - e.Delta);
            e.Handled = true;
        }

        public void PreviewMovie(object sender, MouseButtonEventArgs e)
        {
            StopDownLoad();
            foreach (Window window in App.Current.Windows)
            {
                if (window.GetType().Name == "Main") { windowMain = (Main)window; break; }
            }
            string id = "";
            for (int i = 0; i < windowMain.vieModel.CurrentMovieList.Count; i++)
            {
                if (vieModel.DetailMovie.id.ToLower() == windowMain.vieModel.CurrentMovieList[i].id.ToLower())
                {
                    if (i == 0) { id = windowMain.vieModel.CurrentMovieList[windowMain.vieModel.CurrentMovieList.Count - 1].id; }
                    else { id = windowMain.vieModel.CurrentMovieList[i - 1].id; }
                    break;
                }
            }
            if (id != "")
            {
                vieModel.CleanUp();
                vieModel.Query(id);
            }

            vieModel.SelectImageIndex = 0;
            SetImage(0);
        }

        public void NextMovie(object sender, MouseButtonEventArgs e)
        {
            StopDownLoad();

            windowMain = App.Current.Windows[0] as Main;


            string id = "";
            for (int i = 0; i < windowMain.vieModel.CurrentMovieList.Count; i++)
            {
                if (vieModel.DetailMovie.id == windowMain.vieModel.CurrentMovieList[i].id)
                {
                    if (i == windowMain.vieModel.CurrentMovieList.Count - 1) { id = windowMain.vieModel.CurrentMovieList[0].id; }
                    else { id = windowMain.vieModel.CurrentMovieList[i + 1].id; }
                    break;
                }
            }
            if (id != "")
            {
                vieModel.CleanUp();
                vieModel.Query(id);
            }

            vieModel.SelectImageIndex = 0;
            SetImage(0);
        }




        public void OpenImagePath(object sender, RoutedEventArgs e)
        {
            try
            {
                MenuItem _mnu = sender as MenuItem;
                MenuItem mnu = _mnu.Parent as MenuItem;
                StackPanel sp = null;
                if (mnu != null)
                {
                    DetailMovie detailMovie = vieModel.DetailMovie;
                    int index = mnu.Items.IndexOf(_mnu);
                    string filepath = detailMovie.filepath;
                    if (index == 0) { filepath = detailMovie.filepath; }
                    else if (index == 1) { filepath = BasePicPath + $"BigPic\\{detailMovie.id}.jpg"; }
                    else if (index == 2) { filepath = BasePicPath + $"SmallPic\\{detailMovie.id}.jpg"; }
                    else if (index == 3) { filepath = BasePicPath + $"Gif\\{detailMovie.id}.gif"; }
                    else if (index == 4) { filepath = BasePicPath + $"ExtraPic\\{detailMovie.id}\\"; }
                    else if (index == 5) { filepath = BasePicPath + $"ScreenShot\\{detailMovie.id}\\"; }
                    else if (index == 6) { if (detailMovie.actor.Length > 0) filepath = BasePicPath + $"Actresses\\{detailMovie.actor.Split(actorSplitDict[detailMovie.vediotype])[0]}.jpg"; else filepath = ""; }

                    if (index == 4 | index == 5)
                    {
                        if (Directory.Exists(filepath)) { Process.Start("explorer.exe", "\"" + filepath + "\""); }
                        else
                        {
                            HandyControl.Controls.Growl.Info($"打开失败，不存在 ：{filepath}", "DetailsGrowl");
                        }
                    }
                    else
                    {
                        if (File.Exists(filepath)) { Process.Start("explorer.exe", "/select, \"" + filepath + "\""); }
                        else
                        {
                            HandyControl.Controls.Growl.Info($"打开失败，不存在 ：{filepath}", "DetailsGrowl");
                        }
                    }

                }

            }
            catch (Exception ex) { Console.WriteLine(ex.StackTrace); Console.WriteLine(ex.Message); }
        }

        public void OpenExtraImagePath(object sender, RoutedEventArgs e)
        {
            string filepath = BasePicPath + $"ExtraPic\\{vieModel.DetailMovie.id}\\";
            if (Directory.Exists(filepath)) { Process.Start("explorer.exe", filepath); }
            else
            {
                HandyControl.Controls.Growl.Info($"打开失败，不存在 ：{filepath}", "DetailsGrowl");
            }
        }

        public void OpenFilePath(object sender, RoutedEventArgs e)
        {
            try
            {
                MenuItem _mnu = sender as MenuItem;
                MenuItem mnu = _mnu.Parent as MenuItem;
                DetailMovie detailMovie = vieModel.DetailMovie;
                if (mnu != null)
                {
                    if (File.Exists(detailMovie.filepath)) { Process.Start("explorer.exe", "/select, \"" + detailMovie.filepath + "\""); }
                    else
                    {
                        HandyControl.Controls.Growl.Info($"打开失败，不存在 ：\n{detailMovie.filepath}", "DetailsGrowl");
                    }

                }

            }
            catch (Exception ex) { Console.WriteLine(ex.StackTrace); Console.WriteLine(ex.Message); }
        }


        private void SetToSmallPic(object sender, RoutedEventArgs e)
        {
            MenuItem m1 = sender as MenuItem;
            MenuItem m2 = m1.Parent as MenuItem;

            ContextMenu contextMenu = m2.Parent as ContextMenu;

            Border border = contextMenu.PlacementTarget as Border;
            Grid grid = border.Parent as Grid;
            TextBlock textBox = grid.Children.OfType<TextBlock>().First();

            int idx = int.Parse(textBox.Text);

            string path = vieModel.DetailMovie.extraimagePath[idx];

            try
            {
                File.Copy(path, BasePicPath + $"SmallPic\\{vieModel.DetailMovie.id}.jpg", true);
                //更新到 UI
                RefreshUI(path);
                HandyControl.Controls.Growl.Info("已成功设置！", "DetailsGrowl");
            }
            catch (Exception ex)
            {
                HandyControl.Controls.Growl.Error(ex.Message, "DetailsGrowl");
            }
        }






        private void SetToBigPic(object sender, RoutedEventArgs e)
        {
            MenuItem m1 = sender as MenuItem;
            MenuItem m2 = m1.Parent as MenuItem;

            ContextMenu contextMenu = m2.Parent as ContextMenu;

            Border border = contextMenu.PlacementTarget as Border;
            Grid grid = border.Parent as Grid;
            TextBlock textBox = grid.Children.OfType<TextBlock>().First();

            int idx = int.Parse(textBox.Text);

            string path = vieModel.DetailMovie.extraimagePath[idx];
            if (!File.Exists(path)) { return; }

            try
            {
                File.Copy(path, BasePicPath + $"BigPic\\{vieModel.DetailMovie.id}.jpg", true);
                //更新到 UI

                //BigImage.Source = new BitmapImage(new Uri(path));
                DetailMovie detailMovie = vieModel.DetailMovie;
                detailMovie.bigimage = null;

                vieModel.DetailMovie = null;

                detailMovie.bigimage = StaticClass.BitmapImageFromFile(path);
                vieModel.DetailMovie = detailMovie;

                RefreshUI("", path);
                HandyControl.Controls.Growl.Info("已成功设置！", "DetailsGrowl");
            }
            catch (Exception ex)
            {
                HandyControl.Controls.Growl.Error(ex.Message, "DetailsGrowl");
            }
        }


        private void RefreshUI(string smallPicPath, string BigPicPath = "")
        {
            windowMain = App.Current.Windows[0] as Main;
            for (int i = 0; i < windowMain.vieModel.CurrentMovieList.Count; i++)
            {
                try
                {
                    if (windowMain.vieModel.CurrentMovieList[i]?.id == vieModel.DetailMovie.id)
                    {
                        Movie movie = windowMain.vieModel.CurrentMovieList[i];
                        if (smallPicPath != "") movie.bigimage = null;
                        if (BigPicPath != "") movie.smallimage = null;
                        windowMain.vieModel.CurrentMovieList[i] = null;
                        if (smallPicPath != "") movie.smallimage = StaticClass.BitmapImageFromFile(smallPicPath);
                        if (BigPicPath != "") movie.bigimage = StaticClass.BitmapImageFromFile(BigPicPath);
                        windowMain.vieModel.CurrentMovieList[i] = movie;
                    }
                }
                catch (Exception ex1)
                {
                    Console.WriteLine(ex1.StackTrace);
                    Console.WriteLine(ex1.Message);
                }
            }
        }


        public void CopyFile(object sender, RoutedEventArgs e)
        {
            string filepath = vieModel.DetailMovie.filepath;
            if (File.Exists(filepath))
            {
                StringCollection paths = new StringCollection();
                paths.Add(filepath);
                try
                {
                    Clipboard.SetFileDropList(paths);
                    HandyControl.Controls.Growl.Info($"已复制 {filepath}", "DetailsGrowl");
                }
                catch (Exception ex) { HandyControl.Controls.Growl.Error(ex.Message, "DetailsGrowl"); }

            }
        }

        public void DeleteFile(object sender, RoutedEventArgs e)
        {
            string filepath = vieModel.DetailMovie.filepath;
            if (File.Exists(filepath))
            {
                if (new Msgbox(this, $"是否确认删除 {filepath} 到回收站？").ShowDialog() == true)
                {
                    try
                    {
                        FileSystem.DeleteFile(filepath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                        HandyControl.Controls.Growl.Info($"已删除 {filepath} 到回收站", "DetailsGrowl");
                        DeleteID(sender, e);
                    }
                    catch (Exception ex) { HandyControl.Controls.Growl.Error(ex.Message, "DetailsGrowl"); }

                }
            }
            else
            {
                HandyControl.Controls.Growl.Warning($"删除文件失败，不存在： {filepath} ", "DetailsGrowl");
                DeleteID(sender, e);
            }
        }

        public void DeleteID(object sender, RoutedEventArgs e)
        {
            DataBase.DelInfoByType("movie", "id", vieModel.DetailMovie.id);
            windowMain = App.Current.Windows[0] as Main;
            var movie = windowMain.vieModel.CurrentMovieList.Where(arg => arg.id == vieModel.DetailMovie.id).First();

            if (windowMain.vieModel.CurrentMovieList.Count > 1)
            {
                NextMovie(sender, new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice, 0, MouseButton.Left));
                HandyControl.Controls.Growl.Info($"已从数据库删除 {movie.id} ", "DetailsGrowl");
            }

            //从主界面删除
            windowMain.vieModel.CurrentMovieList.Remove(movie);
            windowMain.vieModel.MovieList.Remove(movie);
            windowMain.vieModel.Statistic();

            if (windowMain.vieModel.CurrentMovieList.Count == 0)
            {
                this.Close();
            }
        }

        private void OpenWeb(object sender, RoutedEventArgs e)
        {

            DetailMovie detailMovie = vieModel.DetailMovie;

            if (!string.IsNullOrEmpty(detailMovie.sourceurl) && detailMovie.sourceurl.IndexOf("http") >= 0)
            {
                try
                {
                    Process.Start(detailMovie.sourceurl);
                }
                catch (Exception ex) { HandyControl.Controls.Growl.Error(ex.Message, "DetailsGrowl"); }

            }
            else
            {
                //为空则使用 bus 打开
                if (!string.IsNullOrEmpty(Properties.Settings.Default.Bus) && Properties.Settings.Default.Bus.IndexOf("http") >= 0)
                {
                    try
                    {
                        Process.Start(Properties.Settings.Default.Bus + detailMovie.id);
                    }
                    catch (Exception ex) { HandyControl.Controls.Growl.Error(ex.Message, "DetailsGrowl"); }
                }
                else
                {
                    HandyControl.Controls.Growl.Error("同步信息的服务器源未设置！", "DetailsGrowl");
                }

            }
        }


        private void UpdateInfo(DetailMovie movie)
        {
            //显示到主界面
            Main main = App.Current.Windows[0] as Main;

            int index1 = main.vieModel.CurrentMovieList.IndexOf(main.vieModel.CurrentMovieList.Where(arg => arg.id == movie.id).First()); ;
            int index2 = main.vieModel.MovieList.IndexOf(main.vieModel.MovieList.Where(arg => arg.id == movie.id).First());

            try
            {
                main.vieModel.CurrentMovieList[index1] = null;
                main.vieModel.MovieList[index2] = null;
                main.vieModel.CurrentMovieList[index1] = movie;
                main.vieModel.MovieList[index2] = movie;
            }
            catch (ArgumentNullException) { }

            //显示到当前页面
            vieModel.DetailMovie = null;
            vieModel.DetailMovie = movie;
            
        }

        public async void TranslateMovie(object sender, RoutedEventArgs e)
        {
            if (!Properties.Settings.Default.Enable_TL_BAIDU & !Properties.Settings.Default.Enable_TL_YOUDAO) { HandyControl.Controls.Growl.Warning("请设置【有道翻译】并测试", "DetailsGrowl"); return; }
            MenuItem mnu = ((MenuItem)(sender)).Parent as MenuItem;
            StackPanel sp = null;
            if (mnu != null)
            {

                string result = "";
                DB dataBase = new DB("Translate");

                DetailMovie movie = vieModel.DetailMovie;

                //检查是否已经翻译过，如有则跳过
                if (!string.IsNullOrEmpty(dataBase.SelectInfoByID("translate_title", "youdao", movie.id))) { HandyControl.Controls.Growl.Warning("影片已经翻译过！", "DetailsGrowl"); return; }
                if (movie.title != "")
                {

                    if (Properties.Settings.Default.Enable_TL_YOUDAO) result = await Translate.Youdao(movie.title);
                    //保存
                    if (result != "")
                    {

                        dataBase.SaveYoudaoTranslateByID(movie.id, movie.title, result, "title");
                        movie.title = result;
                        UpdateInfo(movie);
                        HandyControl.Controls.Growl.Info($"翻译成功！", "DetailsGrowl");
                    }
                    else
                    {
                        HandyControl.Controls.Growl.Info("翻译失败！", "DetailsGrowl");
                    }

                }

                if (movie.plot != "")
                {
                    if (Properties.Settings.Default.Enable_TL_YOUDAO) result = await Translate.Youdao(movie.plot);
                    //保存
                    if (result != "")
                    {
                        dataBase.SaveYoudaoTranslateByID(movie.id, movie.plot, result, "plot");
                        movie.plot = result;
                        UpdateInfo(movie);
                        HandyControl.Controls.Growl.Info($"翻译成功！", "DetailsGrowl");
                    }

                }


                dataBase.CloseDB();
            }
        }



        public async void GenerateSmallImage(object sender, RoutedEventArgs e)
        {
            if (!Properties.Settings.Default.Enable_BaiduAI) { HandyControl.Controls.Growl.Info("请设置【百度 AI】并测试", "DetailsGrowl"); return; }
            MenuItem _mnu = sender as MenuItem;
            MenuItem mnu = _mnu.Parent as MenuItem;
            if (mnu != null)
            {
                this.Cursor = Cursors.Wait;

                DetailMovie movie = vieModel.DetailMovie;
                    string BigPicPath = Properties.Settings.Default.BasePicPath + $"BigPic\\{movie.id}.jpg";
                    string SmallPicPath = Properties.Settings.Default.BasePicPath + $"SmallPic\\{movie.id}.jpg";
                    if (File.Exists(BigPicPath))
                    {
                        Int32Rect int32Rect = await FaceDetect.GetAIResult(movie, BigPicPath);

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
                                bitmap.Save(SmallPicPath, System.Drawing.Imaging.ImageFormat.Jpeg); 
                            }
                            catch (Exception ex) { HandyControl.Controls.Growl.Error(ex.Message, "DetailsGrowl"); }

                            movie.smallimage = StaticClass.GetBitmapImage(movie.id, "SmallPic");
                            UpdateInfo(movie);
                        HandyControl.Controls.Growl.Info($"成功切割缩略图", "DetailsGrowl");
                    }
                    else
                    {
                        HandyControl.Controls.Growl.Warning($"失败：人工智能识别失败！", "DetailsGrowl");
                    }

                    }
                    else
                    {
                        HandyControl.Controls.Growl.Error($"海报图必须存在才能切割！", "DetailsGrowl");
                    }

                


            }
            this.Cursor = Cursors.Arrow;
        }


        public async void GenerateActor(object sender, RoutedEventArgs e)
        {
            if (!Properties.Settings.Default.Enable_BaiduAI) { HandyControl.Controls.Growl.Info("请设置【百度 AI】并测试", "DetailsGrowl"); return; }
            MenuItem _mnu = sender as MenuItem;
            MenuItem mnu = _mnu.Parent as MenuItem;
            StackPanel sp = null;
            if (mnu != null)
            {

                DetailMovie movie = vieModel.DetailMovie;

                

                    if (movie.actor == "") { HandyControl.Controls.Growl.Error("该影片无演员", "DetailsGrowl"); return; }
                    string BigPicPath = Properties.Settings.Default.BasePicPath + $"BigPic\\{movie.id}.jpg";
                    string name = movie.actor.Split(actorSplitDict[movie.vediotype])[0];
                this.Cursor = Cursors.Wait;
                string ActressesPicPath = Properties.Settings.Default.BasePicPath + $"Actresses\\{name}.jpg";
                    if (File.Exists(BigPicPath))
                    {
                        Int32Rect int32Rect = await FaceDetect.GetAIResult(movie, BigPicPath);
                        if (int32Rect != Int32Rect.Empty)
                        {
                            await Task.Delay(500);
                            //切割演员头像
                            System.Drawing.Bitmap SourceBitmap = new System.Drawing.Bitmap(BigPicPath);
                            BitmapImage bitmapImage = ImageProcess.BitmapToBitmapImage(SourceBitmap);
                            ImageSource actressImage = ImageProcess.CutImage(bitmapImage, ImageProcess.GetActressRect(bitmapImage, int32Rect));
                            System.Drawing.Bitmap bitmap = ImageProcess.ImageSourceToBitmap(actressImage);
                            try { bitmap.Save(ActressesPicPath, System.Drawing.Imaging.ImageFormat.Jpeg); }
                            catch (Exception ex) { HandyControl.Controls.Growl.Error(ex.Message, "DetailsGrowl"); }
                        HandyControl.Controls.Growl.Info($"成功切割【{name}】头像", "DetailsGrowl");
                    }
                    else
                    {
                        HandyControl.Controls.Growl.Warning($"失败：人工智能识别失败！", "DetailsGrowl");
                    }
                    }
                    else
                    {
                        HandyControl.Controls.Growl.Error($"海报图必须存在才能切割！", "DetailsGrowl");
                    }

            }
            this.Cursor = Cursors.Arrow;
        }
        public void OpenSubSuctionVedio(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            StackPanel stackPanel = button.Parent as StackPanel;
            TextBlock textBlock = stackPanel.Children.OfType<TextBlock>().Last();
            string filepath = textBlock.Text;
            if (File.Exists(filepath))
            {
                if (File.Exists(filepath)) { Process.Start(filepath); } else { HandyControl.Controls.Growl.Info("无法打开 " + filepath, "DetailsGrowl"); }
            }

        }

        public void ShowSubsection(object sender, MouseButtonEventArgs e)
        {
            SubSectionPopup.IsOpen = true;
            Console.WriteLine(SubSectionPopup.IsOpen);
        }



        private void Grid_PreviewKeyUp(object sender, KeyEventArgs e)
        {

            if (e.Key == Key.Escape)
                this.Close();
            else if (e.Key == Key.Left)
                PreviewMovie(sender, new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice, 0, MouseButton.Left));
            else if (e.Key == Key.Right)
                NextMovie(sender, new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice, 0, MouseButton.Left));
            else if (e.Key == Key.Space || e.Key == Key.Enter || e.Key == Key.P)
                PlayVedio(sender, new MouseEventArgs(InputManager.Current.PrimaryMouseDevice, 0));
            else if (e.Key == Key.L)
                ShowFavorites(sender, new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice, 0, MouseButton.Left));
            else if (e.Key == Key.E)
                EditInfo(sender, new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice, 0, MouseButton.Left));
            else if (e.Key == Key.D)
                DownLoad(sender, new RoutedEventArgs());

        }



        private void RefreshMain(string movieid)
        {
            foreach (Window window in App.Current.Windows)
            {
                if (window.GetType().Name == "Main") { windowMain = (Main)window; break; }
            }
            if (windowMain != null)
            {
                for (int i = 0; i < windowMain.vieModel.CurrentMovieList.Count; i++)
                {
                    if (windowMain.vieModel.CurrentMovieList[i].id == movieid)
                    {
                        windowMain.vieModel.CurrentMovieList[i] = (Movie)vieModel.DetailMovie;
                        break;
                    }
                }
            }
        }




        private void SetImage(int idx)
        {
            if (vieModel.DetailMovie.extraimagelist.Count == 0)
            {
                //设置为默认图片
                BigImage.Source = new BitmapImage(new Uri("/Resources/Picture/NoPrinting_B.jpg", UriKind.Relative));
            }
            else
            {


                BigImage.Source = vieModel.DetailMovie.extraimagelist[idx];
                for (int i = 0; i < imageItemsControl.Items.Count; i++)
                {
                    ContentPresenter c = (ContentPresenter)imageItemsControl.ItemContainerGenerator.ContainerFromItem(imageItemsControl.Items[i]);
                    StackPanel stackPanel = FindElementByName<StackPanel>(c, "ImageStackPanel");
                    if (stackPanel != null)
                    {
                        Grid grid = stackPanel.Children[0] as Grid;
                        Border border = grid.Children[0] as Border;
                        TextBlock tb = grid.Children.OfType<TextBlock>().First();

                        if (border != null & tb != null)
                        {
                            if (int.Parse(tb.Text) == idx)
                                border.Opacity = 0;
                            else
                                border.Opacity = 0.5;
                        }
                    }
                }
            }
        }



        private void ShowExtraImage(object sender, MouseButtonEventArgs e)
        {
            Border border = sender as Border;
            TextBlock tb = ((Grid)border.Parent).Children.OfType<TextBlock>().First();
            vieModel.SelectImageIndex = int.Parse(tb.Text);
            SetImage(vieModel.SelectImageIndex);
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




        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StopDownLoad();
            if (this.WindowState != WindowState.Minimized)
            {
                if (this.WindowState == WindowState.Normal) WinState = JvedioWindowState.Normal;
                else if (this.WindowState == WindowState.Maximized) WinState = JvedioWindowState.FullScreen;
                else if (this.Width == SystemParameters.WorkArea.Width & this.Height == SystemParameters.WorkArea.Height) WinState = JvedioWindowState.Maximized;


                WindowConfig cj = new WindowConfig(this.GetType().Name);
                Rect rect = new Rect(this.Left, this.Top, this.Width, this.Height);
                cj.Save(rect, WinState);
            }

        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Console.WriteLine(this.WindowState.ToString());
        }

        private void BigImage_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Link;
            e.Handled = true;//必须加
        }

        private void BigImage_Drop(object sender, DragEventArgs e)
        {
            //分为文件夹和文件
            string[] dragdropFiles = (string[])e.Data.GetData(DataFormats.FileDrop);
            string file = dragdropFiles[0];

            if (StaticClass.IsFile(file))
            {
                FileInfo fileInfo = new FileInfo(file);
                if (fileInfo.Extension.ToLower() == ".jpg")
                {
                    try
                    {
                        File.Copy(fileInfo.FullName, BasePicPath + $"BigPic\\{vieModel.DetailMovie.id}.jpg", true);
                        DetailMovie detailMovie = vieModel.DetailMovie;
                        detailMovie.bigimage = null;
                        vieModel.DetailMovie = null;
                        detailMovie.bigimage = StaticClass.BitmapImageFromFile(fileInfo.FullName);
                        vieModel.DetailMovie = detailMovie;

                        RefreshUI("", fileInfo.FullName);


                    }
                    catch (Exception ex)
                    {
                        HandyControl.Controls.Growl.Error(ex.Message, "DetailsGrowl");
                    }
                }
                else
                {
                    HandyControl.Controls.Growl.Info("仅支持 jpg", "DetailsGrowl");
                }
            }

        }

        private void Border_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Link;
            e.Handled = true;//必须加
        }

        private void Border_Drop(object sender, DragEventArgs e)
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
            foreach (var item in stringCollection)
            {
                try { filepaths.AddRange(Directory.GetFiles(item, "*.jpg").ToList<string>()); }
                catch (Exception ex) { Console.WriteLine(ex.Message); continue; }
            }
            if (files.Count > 0) filepaths.AddRange(files);

            //复制文件
            string path = BasePicPath + $"ExtraPic\\{vieModel.DetailMovie.id}\\";
            if (!Directory.Exists(path)) { Directory.CreateDirectory(path); }
            bool success = false;
            foreach (var item in filepaths)
            {
                try
                {
                    File.Copy(item, path + item.Split('\\').Last());
                    success = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    continue;
                }

            }
            if (success)
            {
                //更新UI
                DetailMovie detailMovie = vieModel.DetailMovie;
                List<BitmapSource> oldImageList = detailMovie.extraimagelist;
                List<string> oldImagePath = detailMovie.extraimagePath;

                detailMovie.extraimagelist = new List<BitmapSource>();
                detailMovie.extraimagePath = new List<string>();
                vieModel.DetailMovie = null;

                //载入默认的和新的
                detailMovie.extraimagelist.AddRange(oldImageList);
                detailMovie.extraimagePath.AddRange(oldImagePath);


                foreach (var item in filepaths)
                {
                    detailMovie.extraimagelist.Add(StaticClass.GetExtraImage(item));
                    detailMovie.extraimagePath.Add(path + item.Split('\\').Last());
                }


                vieModel.DetailMovie = detailMovie;

            }

        }

        private void Rate_ValueChanged(object sender, HandyControl.Data.FunctionEventArgs<double> e)
        {
            vieModel.SaveLove();
        }

        private void Tag_Closing(object sender, EventArgs e)
        {
            HandyControl.Controls.Tag Tag = sender as HandyControl.Controls.Tag;
            string text = Tag.Content.ToString();
            //删除
            if (vieModel.DetailMovie.labellist.Contains(text))
            {
                vieModel.DetailMovie.labellist.Remove(text);
                vieModel.SaveLabel();
            }

            if (text == "+")
            {
                //显示新增按钮
                List<string> labels = vieModel.DetailMovie.labellist;
                vieModel.DetailMovie.labellist = new List<string>();
                vieModel.DetailMovie.labellist.Add("+");
                vieModel.DetailMovie.labellist.AddRange(labels);
                vieModel.Query(vieModel.DetailMovie.id);
            }


        }

        private void Border_MouseEnter(object sender, MouseEventArgs e)
        {
            Border border = sender as Border;
            border.Opacity = 0;



        }

        private void Border_MouseLeave(object sender, MouseEventArgs e)
        {

            Border border = sender as Border;

            Grid grid = border.Parent as Grid;
            TextBlock textBlock = grid.Children.OfType<TextBlock>().First();

            int idx = int.Parse(textBlock.Text);
            if (idx != vieModel.SelectImageIndex)
                border.Opacity = 0.4;
            else
                border.Opacity = 0;




        }

        private void Grid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (vieModel.DetailMovie.extraimagelist.Count == 0) return;
            if (e.Delta > 0)
            {
                vieModel.SelectImageIndex -= 1;
            }
            else
            {
                vieModel.SelectImageIndex += 1;
            }

            if (vieModel.SelectImageIndex < 0) { vieModel.SelectImageIndex = 0; } else if (vieModel.SelectImageIndex >= imageItemsControl.Items.Count) { vieModel.SelectImageIndex = imageItemsControl.Items.Count - 1; }
            SetImage(vieModel.SelectImageIndex);
            //滚动到指定的
            ContentPresenter c = (ContentPresenter)imageItemsControl.ItemContainerGenerator.ContainerFromItem(imageItemsControl.Items[vieModel.SelectImageIndex]);
            c.BringIntoView();


        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                Main main = App.Current.Windows[0] as Main;
                main.Resizing = true;
                main.ResizingTimer.Start();
                this.Close();
            }
        }

        private void Border_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta < 0)
            {
                NextMovie(sender, new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice, 0, MouseButton.Left));
            }
            else
            {
                PreviewMovie(sender, new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice, 0, MouseButton.Left));
            }
        }

        private MenuItem GetMenuItem(ContextMenu contextMenu, string header)
        {
            foreach (MenuItem item in contextMenu.Items)
            {
                if (item.Header.ToString() == header)
                {
                    return item;
                }
            }
            return null;
        }

        private void ContextMenu_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            ContextMenu contextMenu = sender as ContextMenu;
            if (e.Key == Key.D)
            {
                MenuItem menuItem = GetMenuItem(contextMenu, "删除信息(D)");
                if (menuItem != null) DeleteID(menuItem, new RoutedEventArgs());
            }
            else if (e.Key == Key.T)
            {
                MenuItem menuItem = GetMenuItem(contextMenu, "删除文件(T)");
                if (menuItem != null) DeleteFile(menuItem, new RoutedEventArgs());

            }
            else if (e.Key == Key.S)
            {
                MenuItem menuItem = GetMenuItem(contextMenu, "立即同步(S)");
                if (menuItem != null) DownLoad(menuItem, new RoutedEventArgs());

            }
            else if (e.Key == Key.E)
            {
                MenuItem menuItem = GetMenuItem(contextMenu, "修改信息(E)");
                if (menuItem != null) EditInfo(menuItem, new RoutedEventArgs());

            }
            else if (e.Key == Key.W)
            {
                MenuItem menuItem = GetMenuItem(contextMenu, "打开网址(W)");
                if (menuItem != null) OpenWeb(menuItem, new RoutedEventArgs());

            }
            else if (e.Key == Key.C)
            {
                MenuItem menuItem = GetMenuItem(contextMenu, "复制文件(C)");
                if (menuItem != null) CopyFile(menuItem, new RoutedEventArgs());

            }
            contextMenu.IsOpen = false;
        }
    }



    public class DetailDownLoad
    {
        public event EventHandler InfoUpdate;
        public event EventHandler CancelEvent;
        private object lockobject;
        private double Maximum;
        private double Value;
        public bool IsDownLoading = false;

        //线程 Token
        CancellationTokenSource cts;

        public DetailMovie DetailMovie { get; set; }

        public DetailDownLoad(DetailMovie detailMovie)
        {
            Value = 0;
            Maximum = 1;
            DetailMovie = detailMovie;
            cts = new CancellationTokenSource();
            cts.Token.Register(() => { Console.WriteLine("取消当前同步任务"); });
            lockobject = new object();
            IsDownLoading = false;
        }

        public void CancelDownload()
        {
            cts.Cancel();
            IsDownLoading = false;
        }


        public async void DownLoad()
        {
            IsDownLoading = true;
            //下载信息
            if (DetailMovie.title == "" | DetailMovie.bigimageurl == "" | DetailMovie.extraimageurl == "" | DetailMovie.sourceurl == "")
            {
                string[] url = new string[] { Properties.Settings.Default.Bus, Properties.Settings.Default.BusEurope, Properties.Settings.Default.DB, Properties.Settings.Default.Library };
                bool[] enableurl = new bool[] { Properties.Settings.Default.EnableBus, Properties.Settings.Default.EnableBusEu, Properties.Settings.Default.EnableDB, Properties.Settings.Default.EnableLibrary, Properties.Settings.Default.EnableFC2 };
                string[] cookies = new string[] { Properties.Settings.Default.DBCookie };

                bool success; string resultMessage;
                (success, resultMessage) = await Task.Run(() =>
                {
                    return Net.DownLoadFromNet((Movie)DetailMovie);
                });
            }

            DetailMovie dm = new DetailMovie();
            dm = DataBase.SelectDetailMovieById(DetailMovie.id);

            InfoUpdate?.Invoke(this, new DetailMovieEventArgs() { DetailMovie = dm, value = Value, maximum = Maximum });

            if (!File.Exists(StaticVariable.BasePicPath + $"BigPic\\{dm.id}.jpg")) DownLoadBigPic(dm); //下载大图
            if (!File.Exists(StaticVariable.BasePicPath + $"SmallPic\\{dm.id}.jpg")) DownLoadSmallPic(dm); //下载小图

            List<string> urlList = new List<string>();
            foreach (var item in dm.extraimageurl?.Split(';')) { if (!string.IsNullOrEmpty(item)) urlList.Add(item); }
            Maximum = urlList.Count() == 0 ? 1 : urlList.Count();

            //下载预览图
            DownLoadExtraPic(dm);



        }

        private async void DownLoadSmallPic(DetailMovie dm)
        {
            if (dm.smallimageurl != "")
            {
                await Task.Run(() =>
               {

                   return Net.DownLoadImage(dm.smallimageurl, ImageType.SmallImage, dm.id);
               });
            }
        }


        private async void DownLoadBigPic(DetailMovie dm)
        {
            if (dm.bigimageurl != "")
            {
                await Task.Run(() =>
                {

                    return Net.DownLoadImage(dm.bigimageurl, ImageType.BigImage, dm.id);
                });
            }
            InfoUpdate?.Invoke(this, new DetailMovieEventArgs() { DetailMovie = dm, value = Value, maximum = Maximum });
        }




        private async void DownLoadExtraPic(DetailMovie dm)
        {
            List<string> urlList = new List<string>();
            foreach (var item in dm.extraimageurl?.Split(';'))
            {
                if (!string.IsNullOrEmpty(item)) urlList.Add(item);
            }
            bool dlimageSuccess = false; string cookies = "";
            for (int i = 0; i < urlList.Count(); i++)
            {
                if (cts.IsCancellationRequested) { CancelEvent?.Invoke(this, EventArgs.Empty); break; }
                string filepath = "";
                if (urlList[i].Length > 0)
                {
                    filepath = StaticVariable.BasePicPath + "ExtraPic\\" + dm.id + "\\" + Path.GetFileName(new Uri(urlList[i]).LocalPath);
                    if (!File.Exists(filepath))
                    {
                        (dlimageSuccess, cookies) = await Task.Run(() => { return Net.DownLoadImage(urlList[i], ImageType.ExtraImage, dm.id, Cookie: cookies); });
                        if (dlimageSuccess) Thread.Sleep(1500);
                    }
                }
                lock (lockobject) Value += 1;
                InfoUpdate?.Invoke(this, new DetailMovieEventArgs() { DetailMovie = dm, value = Value, maximum = Maximum });
            }
            lock (lockobject) Value = Maximum;
            InfoUpdate?.Invoke(this, new DetailMovieEventArgs() { DetailMovie = dm, value = Value, maximum = Maximum });
            IsDownLoading = false;
        }
    }

    public class DetailMovieEventArgs : EventArgs
    {
        public DetailMovie DetailMovie;
        public double value = 0;
        public double maximum = 1;
    }

    public class PlotToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is null) return Visibility.Hidden;
            if (value.ToString() == "" | string.IsNullOrEmpty(value.ToString()))
                return Visibility.Hidden;
            else
                return Visibility.Visible;
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }



}
