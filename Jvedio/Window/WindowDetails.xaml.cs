

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
using System.Windows.Threading;
using static Jvedio.StaticVariable;

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
            }
            else { this.DataContext = null; }

            AdjustWindow();

            FatherGrid.Focus();

            SetSkin();



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

        public void DownLoad(object sender, MouseButtonEventArgs e)
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

        public void GetScreenShot(object sender, RoutedEventArgs e)
        {

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
                    if (vieModel.DetailMovie.id.ToUpper() == eventArgs.DetailMovie.id.ToUpper()) vieModel.Query(eventArgs.DetailMovie.id);
                });
            };


        }

        public void StopDownLoad()
        {
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
            if (LikePopup.IsOpen)
                LikePopup.IsOpen = false;
            else
                LikePopup.IsOpen = true;
            LikePopup.Focus();
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
            string genretext = ((Label)sender).Content.ToString();
            if (string.IsNullOrEmpty(genretext)) return;
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
            string tagtext = ((Label)sender).Content.ToString();
            if (string.IsNullOrEmpty(tagtext)) return;
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
        public void EditInfo(object sender, MouseButtonEventArgs e)
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


        public void HeartMouseUp(object sender, RoutedEventArgs e)
        {
            RadioButton radioButton = sender as RadioButton;
            this.vieModel.DetailMovie.favorites = int.Parse(radioButton.Content.ToString());
            //保存到数据库
            vieModel.SaveLove();
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
                    //使用默认播放器
                    if (!string.IsNullOrEmpty(Properties.Settings.Default.VedioPlayerPath) && File.Exists(Properties.Settings.Default.VedioPlayerPath))
                    {
                        try
                        {
                            Process.Start(Properties.Settings.Default.VedioPlayerPath, filepath);
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
                    }
                }
                else
                    new Msgbox(this, "无法打开 " + filepath).ShowDialog();
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
        }

        public void NextMovie(object sender, MouseButtonEventArgs e)
        {
            StopDownLoad();

            windowMain = App.Current.Windows[0] as Main;


            string id = "";
            for (int i = 0; i < windowMain.vieModel.CurrentMovieList.Count; i++)
            {
                if (vieModel.DetailMovie.id.ToUpper() == windowMain.vieModel.CurrentMovieList[i].id.ToUpper())
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
        }



        public void OpenImagePath(object sender, RoutedEventArgs e)
        {
            MenuItem _mnu = sender as MenuItem;
            MenuItem mnu = _mnu.Parent as MenuItem;

            if (mnu != null)
            {
                int index = mnu.Items.IndexOf(_mnu);
                string filepath = vieModel.DetailMovie.filepath;
                if (index == 0) { filepath = vieModel.DetailMovie.filepath; }
                else if (index == 1) { filepath = BasePicPath + $"BigPic\\{vieModel.DetailMovie.id}.jpg"; }
                else if (index == 2) { filepath = BasePicPath + $"SmallPic\\{vieModel.DetailMovie.id}.jpg"; }
                else if (index == 3) { filepath = BasePicPath + $"ScreenShot\\{vieModel.DetailMovie.id}\\"; }
                else if (index == 4) { if (vieModel.DetailMovie.actor.Length > 0) filepath = BasePicPath + $"Actresses\\{vieModel.DetailMovie.actor.Split(new char[] { ' ', '/' })[0]}.jpg"; else filepath = ""; }



                if (index == 3)
                {
                    if (Directory.Exists(filepath))
                        Process.Start("explorer.exe", "\"" + filepath + "\"");
                    else
                        new PopupWindow(this, "文件夹不存在").Show();
                }
                else
                {
                    if (File.Exists(filepath))
                        Process.Start("explorer.exe", "/select, \"" + filepath + "\"");
                    else
                        new PopupWindow(this, "文件夹不存在").Show();
                }

            }


        }


        public void OpenExtraImagePath(object sender, RoutedEventArgs e)
        {
            string filepath = BasePicPath + $"ExtraPic\\{vieModel.DetailMovie.id}\\";
            if (Directory.Exists(filepath)) { Process.Start("explorer.exe", filepath); }
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
                    new Msgbox(this, $"已复制 {filepath}").ShowDialog();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

            }
        }

        public void DeleteFile(object sender, RoutedEventArgs e)
        {
            string filepath = vieModel.DetailMovie.filepath;
            if (File.Exists(filepath))
            {
                if (new Msgbox(this, $"是否确认删除 {filepath} 到回收站？").ShowDialog() == true)
                {
                    FileSystem.DeleteFile(filepath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    new Msgbox(this, $"已删除 {filepath} 到回收站").ShowDialog();
                }
            }
        }

        public void DeleteID(object sender, RoutedEventArgs e)
        {
            if (new Msgbox(this, $"是否确认从数据库删除 {vieModel.DetailMovie.id} （不删除文件）？").ShowDialog() == true)
            {

                DataBase cdb = new DataBase();
                cdb.DelInfoByType("movie", "id", vieModel.DetailMovie.id);
                cdb.CloseDB();




               
                windowMain = App.Current.Windows[0] as Main;
                var movie = windowMain.vieModel.CurrentMovieList.Where(arg => arg.id.ToUpper() == vieModel.DetailMovie.id.ToUpper()).First();

                if (windowMain.vieModel.CurrentMovieList.Count > 1)
                {
                    NextMovie(sender, new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice, 0, MouseButton.Left));
                    new PopupWindow(this, $"已从数据库删除 {movie.id} ").Show();
                }

                //从主界面删除
                windowMain.vieModel.CurrentMovieList.Remove(movie); 
                windowMain.vieModel.MovieList.Remove(movie);
                windowMain.vieModel.MovieCount = $"本页有 {windowMain.vieModel.CurrentMovieList.Count} 个，总计{windowMain.vieModel.MovieList.Count} 个";

                if (windowMain.vieModel.CurrentMovieList.Count ==0)
                {
                    this.Close();
                }


                }
        }

        private async void OpenWeb(object sender, RoutedEventArgs e)
        {
            try
            {
                string id = this.vieModel.DetailMovie.id;
                DataBase cdb = new DataBase("");
                Movie movie = await cdb.SelectMovieByID(id);
                string sourceurl = movie.sourceurl;
                cdb.CloseDB();
                if (sourceurl != null)
                {
                    Process.Start(sourceurl);
                }

            }
            catch (Exception ex) { Console.WriteLine(ex.StackTrace); Console.WriteLine(ex.Message); }
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

        public void ShowSubsection(object sender, MouseButtonEventArgs e)
        {
            var grid = SubsectionImage.Parent as Grid;
            Canvas canvas = grid.Children.OfType<Canvas>().First();
            if (canvas.Visibility == Visibility.Hidden)
                canvas.Visibility = Visibility.Visible;
            else
                canvas.Visibility = Visibility.Hidden;

        }



        private void Grid_PreviewKeyDown(object sender, KeyEventArgs e)
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
                DownLoad(sender, new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice, 0, MouseButton.Left));

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
                    if (windowMain.vieModel.CurrentMovieList[i].id.ToUpper() == movieid.ToUpper())
                    {
                        windowMain.vieModel.CurrentMovieList[i] = (Movie)vieModel.DetailMovie;
                        break;
                    }
                }
            }
        }








        //拖动图片
        //Point scrollMousePoint = new Point();
        //double hOff = 1;
        //private void scrollViewer_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        //{
        //    scrollMousePoint = e.GetPosition(scrollViewer);
        //    hOff = scrollViewer.HorizontalOffset;
        //    scrollViewer.CaptureMouse();
        //}

        //private void scrollViewer_PreviewMouseMove(object sender, MouseEventArgs e)
        //{
        //    if (scrollViewer.IsMouseCaptured)
        //    {
        //        scrollViewer.ScrollToHorizontalOffset(hOff + (scrollMousePoint.X - e.GetPosition(scrollViewer).X));
        //    }
        //}

        //private void scrollViewer_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        //{
        //    scrollViewer.ReleaseMouseCapture();
        //}
        WindowImageViewer windowImageViewer;

        private void Image_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Image image = sender as Image;
            TextBlock tb = ((StackPanel)image.Parent).Children.OfType<TextBlock>().First();
            if (windowImageViewer != null) { windowImageViewer.Close(); }
            windowImageViewer = new WindowImageViewer(this.vieModel.DetailMovie.id, int.Parse(tb.Text));
            windowImageViewer.ShowDialog();
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
            cts.Token.Register(() => { Console.WriteLine("取消当前下载任务"); });
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

            DetailMovie dm = new DetailMovie(); DataBase cdb = new DataBase("");
            dm = cdb.SelectDetailMovieById(DetailMovie.id);
            cdb.CloseDB();
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
