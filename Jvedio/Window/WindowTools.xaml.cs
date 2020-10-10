using Jvedio.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Xml;
using static Jvedio.StaticClass;


namespace Jvedio
{
    /// <summary>
    /// Settings.xaml 的交互逻辑
    /// </summary>
    public partial class WindowTools : Jvedio_BaseWindow
    {

        public CancellationTokenSource cts;
        public CancellationToken ct;
        public bool Running;
        VieModel_Tools vieModel;

        public WindowTools()
        {
            InitializeComponent();
            WinState = 0;
            vieModel = new VieModel_Tools();
            this.DataContext = vieModel;
            cts = new CancellationTokenSource();
            cts.Token.Register(() => { Console.WriteLine("取消当前下载任务"); });
            ct = cts.Token;
            Running = false;


            var Grids = MainGrid.Children.OfType<Grid>().ToList();
            foreach (var item in Grids) item.Visibility = Visibility.Hidden;
            Grids[Properties.Settings.Default.ToolsIndex].Visibility = Visibility.Visible;

            var RadioButtons = RadioButtonStackPanel.Children.OfType<RadioButton>().ToList();
            RadioButtons[Properties.Settings.Default.ToolsIndex].IsChecked = true;

        }

        public void ShowGrid(object sender, RoutedEventArgs e)
        {

            RadioButton radioButton = (RadioButton)sender;
            StackPanel SP = radioButton.Parent as StackPanel;
            var radioButtons = SP.Children.OfType<RadioButton>().ToList();
            var Grids = MainGrid.Children.OfType<Grid>().ToList();
            foreach (var item in Grids) item.Visibility = Visibility.Hidden;
            Grids[radioButtons.IndexOf(radioButton)].Visibility = Visibility.Visible;

            Properties.Settings.Default.ToolsIndex = radioButtons.IndexOf(radioButton);
            Properties.Settings.Default.Save();

        }

        public void ShowAccessPath(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog OpenFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            OpenFileDialog1.Title = "选择一个Access文件";
            OpenFileDialog1.FileName = "";
            OpenFileDialog1.Filter = "Access文件(*.mdb)| *.mdb";
            OpenFileDialog1.FilterIndex = 1;
            OpenFileDialog1.InitialDirectory = Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "mdb") ? AppDomain.CurrentDomain.BaseDirectory + "mdb" : AppDomain.CurrentDomain.BaseDirectory;
            OpenFileDialog1.RestoreDirectory = true;
            if (OpenFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                AccessPathTextBox.Text = OpenFileDialog1.FileName;
            }
        }

        public void ShowNFOPath(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog OpenFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            OpenFileDialog1.Title = "选择一个NFO文件";
            OpenFileDialog1.FileName = "";
            OpenFileDialog1.Filter = "NFO文件(*.nfo)| *.nfo";
            OpenFileDialog1.FilterIndex = 1;
            OpenFileDialog1.InitialDirectory = Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "Download\\NFO") ? AppDomain.CurrentDomain.BaseDirectory + "Download\\NFO" : AppDomain.CurrentDomain.BaseDirectory;
            OpenFileDialog1.RestoreDirectory = true;
            if (OpenFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                NFOPathTextBox.Text = OpenFileDialog1.FileName;
            }
        }



        public void AddPath(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            folderBrowserDialog.Description = "选择文件夹";
            folderBrowserDialog.ShowNewFolderButton = false;
            //folderBrowserDialog.SelectedPath = @"D:\2020\VS Project\Jvedio\Jvedio(WPF)\Jvedio\Jvedio\bin\番号测试";
            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK & !string.IsNullOrWhiteSpace(folderBrowserDialog.SelectedPath))
            {
                bool PathConflict = false;
                foreach (var item in vieModel.ScanPath)
                {
                    if (folderBrowserDialog.SelectedPath.IndexOf(item) >= 0 | item.IndexOf(folderBrowserDialog.SelectedPath) >= 0)
                    {
                        PathConflict = true;
                        break;
                    }
                }
                if (!PathConflict) { vieModel.ScanPath.Add(folderBrowserDialog.SelectedPath); } else { new Msgbox(this, "路径冲突！").ShowDialog(); }

            }




        }

        public void DelPath(object sender, MouseButtonEventArgs e)
        {
            if (PathListBox.SelectedIndex != -1)
            {
                for (int i = PathListBox.SelectedItems.Count - 1; i >= 0; i--)
                {
                    vieModel.ScanPath.Remove(PathListBox.SelectedItems[i].ToString());
                }
            }
        }

        public void ClearPath(object sender, MouseButtonEventArgs e)
        {
            vieModel.ScanPath.Clear();
        }



        public void AddEuPath(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            folderBrowserDialog.Description = "选择文件夹";
            folderBrowserDialog.SelectedPath = @"D:\2020\VS Project\Jvedio\Jvedio(WPF)\Jvedio\Jvedio\资料\欧美测试";
            folderBrowserDialog.ShowNewFolderButton = true;
            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK & !string.IsNullOrWhiteSpace(folderBrowserDialog.SelectedPath))
            {
                bool PathConflict = false;
                foreach (var item in vieModel.ScanEuPath)
                {
                    if (folderBrowserDialog.SelectedPath.IndexOf(item) >= 0 | item.IndexOf(folderBrowserDialog.SelectedPath) >= 0)
                    {
                        PathConflict = true;
                        break;
                    }
                }
                if (!PathConflict) { vieModel.ScanEuPath.Add(folderBrowserDialog.SelectedPath); } else { new Msgbox(this, "路径冲突！").ShowDialog(); }

            }




        }

        public void DelEuPath(object sender, MouseButtonEventArgs e)
        {
            if (EuropePathListBox.SelectedIndex != -1)
            {
                for (int i = EuropePathListBox.SelectedItems.Count - 1; i >= 0; i--)
                {
                    vieModel.ScanEuPath.Remove(EuropePathListBox.SelectedItems[i].ToString());
                }
            }
        }

        public void ClearEuPath(object sender, MouseButtonEventArgs e)
        {
            vieModel.ScanEuPath.Clear();
        }





        public void AddSingleNFOPath(object sender, RoutedEventArgs e)
        {
            string path = NFODirPathTextBox.Text;
            if (Directory.Exists(path))
            {
                bool PathConflict = false;
                foreach (var item in vieModel.NFOScanPath)
                {
                    if (path.IndexOf(item) >= 0 | item.IndexOf(path) >= 0)
                    {
                        PathConflict = true;
                        break;
                    }
                }
                if (!PathConflict) { vieModel.NFOScanPath.Add(path); NFODirPathTextBox.Text = ""; } else { new Msgbox(this, "路径冲突！").ShowDialog(); }

            }




        }



        public void AddNFOPath(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            folderBrowserDialog.Description = "选择文件夹";
            folderBrowserDialog.ShowNewFolderButton = true;
            folderBrowserDialog.SelectedPath = Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "DownLoad\\NFO") ? AppDomain.CurrentDomain.BaseDirectory + "DownLoad\\NFO" : AppDomain.CurrentDomain.BaseDirectory;
            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK & !string.IsNullOrWhiteSpace(folderBrowserDialog.SelectedPath))
            {
                bool PathConflict = false;
                foreach (var item in vieModel.NFOScanPath)
                {
                    if (folderBrowserDialog.SelectedPath.IndexOf(item) >= 0 | item.IndexOf(folderBrowserDialog.SelectedPath) >= 0)
                    {
                        PathConflict = true;
                        break;
                    }
                }
                if (!PathConflict) { vieModel.NFOScanPath.Add(folderBrowserDialog.SelectedPath); } else { new Msgbox(this, "路径冲突！").ShowDialog(); }

            }




        }


        public async void StartRun(object sender, RoutedEventArgs e)
        {

            if (Running) { new Msgbox(this, "其他任务正在进行！").ShowDialog(); return; }

            cts = new CancellationTokenSource();
            cts.Token.Register(() => { Console.WriteLine("取消当前下载任务"); });
            ct = cts.Token;

            var grids = MainGrid.Children.OfType<Grid>().ToList();
            int index = 0;
            for (int i = 0; i < grids.Count; i++) { if (grids[i].Visibility == Visibility.Visible) { index = i; break; } }
            Running = true;
            switch (index)
            {
                case 0:
                    //扫描
                    double totalnum = 0;//扫描出的视频总数
                    double insertnum = 0;//导入的视频总数
                    try
                    {
                        //全盘扫描
                        if ((bool)ScanAll.IsChecked)
                        {
                            LoadingStackPanel.Visibility = Visibility.Visible;
                            await Task.Run(() =>
                            {
                                ct.ThrowIfCancellationRequested();

                                List<string> filepaths = Scan.ScanAllDrives();
                                totalnum = filepaths.Count;
                                insertnum=Scan.DistinctMovieAndInsert(filepaths, ct);
                            });
                        }
                        else
                        {
                            if (vieModel.ScanPath.Count == 0) { break; }
                            LoadingStackPanel.Visibility = Visibility.Visible;



                            await Task.Run(() =>
                        {
                            ct.ThrowIfCancellationRequested();

                            StringCollection stringCollection = new StringCollection();
                            foreach (var item in vieModel.ScanPath)
                            {
                                if (Directory.Exists(item)) { stringCollection.Add(item); }
                            }
                            List<string> filepaths = Scan.ScanPaths(stringCollection, ct);
                            totalnum = filepaths.Count;
                            insertnum = Scan.DistinctMovieAndInsert(filepaths, ct);
                        }, cts.Token);

                        }

                        LoadingStackPanel.Visibility = Visibility.Hidden;
                        if (!cts.IsCancellationRequested) new PopupWindow(this, $"扫描出 {totalnum} 个，导入 {insertnum} 个",true).Show();
                    }
                    catch (OperationCanceledException ex)
                    {
                        Console.WriteLine($"{nameof(OperationCanceledException)} thrown with message: {ex.Message}");
                    }
                    finally
                    {
                        cts.Dispose();
                        Running = false;
                    }


                    break;
                case 1:
                    //Access
                    LoadingStackPanel.Visibility = Visibility.Visible;
                    string AccessPath = AccessPathTextBox.Text;
                    if (!File.Exists(AccessPath)) { new Msgbox(this, $"不存在 ：{AccessPath}").ShowDialog(); break; }
                    try
                    {
                        await Task.Run(() =>
                    {
                        DataBase cdb = new DataBase();
                        cdb.InsertFromAccess(AccessPath);
                        cdb.CloseDB();
                    });
                        LoadingStackPanel.Visibility = Visibility.Hidden;
                        if (!cts.IsCancellationRequested) new PopupWindow(this, "成功！").Show();

                    }
                    finally
                    {
                        cts.Dispose();
                        Running = false;
                    }
                    break;
                case 2:
                    //NFO
                    if ((bool)NfoRB1.IsChecked)
                    {
                        if (vieModel.NFOScanPath.Count == 0) { new Msgbox(this, "路径为空！").ShowDialog(); }
                    }
                    else { if (!File.Exists(NFOPathTextBox.Text)) { new Msgbox(this, $"文件不存在{NFOPathTextBox.Text}").ShowDialog(); } }


                    Running = true;

                    try
                    {
                        List<string> nfoFiles = new List<string>();
                        if (!(bool)NfoRB1.IsChecked) { nfoFiles.Add(NFOPathTextBox.Text); }
                        else
                        {
                            //扫描所有nfo文件
                            await Task.Run(() =>
                            {
                                this.Dispatcher.Invoke((Action)delegate {
                                    StatusTextBlock.Visibility = Visibility.Visible;
                                    StatusTextBlock.Text = "开始扫描";
                                });

                                StringCollection stringCollection = new StringCollection();
                                foreach (var item in vieModel.NFOScanPath)
                                {
                                    if (Directory.Exists(item)) { stringCollection.Add(item); }
                                }
                                nfoFiles = Scan.ScanNFO(stringCollection, ct,(filepath)=> {
                                    this.Dispatcher.Invoke((Action)delegate { StatusTextBlock.Text = filepath; });
                                });
                            }, cts.Token);
                        }


                        //记录日志
                        Logger.LogScanInfo("\n-----【" + DateTime.Now.ToString() + "】NFO扫描-----");
                        Logger.LogScanInfo($"\n扫描出 => {nfoFiles.Count}  个 ");


                        //导入所有 nfo 文件信息
                        double total = 0;
                        await Task.Run(() =>
                        {
                            DataBase cdb = new DataBase();
                            
                            nfoFiles.ForEach(item =>
                            {
                                if (File.Exists(item))
                                {
                                    Movie movie = GetInfoFromNfo(item);
                                    if (movie != null)
                                    {

                                        cdb.InsertFullMovie(movie);
                                        //复制并覆盖所有图片
                                        CopyPicToPath(movie.id, item);
                                        total += 1;
                                        Logger.LogScanInfo($"\n成功导入数据库 => {item}  ");
                                    }

                                }
                            });
                            cdb.CloseDB();

                        });
                        LoadingStackPanel.Visibility = Visibility.Hidden;
                        if (!cts.IsCancellationRequested) {
                            Logger.LogScanInfo($"\n成功导入 {total} 个");
                            new PopupWindow(this, "成功！").Show(); }
                    }
                    finally
                    {
                        cts.Dispose();
                        Running = false;
                    }


                    break;

                case 3:
                    //欧美扫描
                    if (vieModel.ScanEuPath.Count == 0) { break; }
                    LoadingStackPanel.Visibility = Visibility.Visible;
                    totalnum = 0;
                    insertnum = 0;

                    try
                    {
                        await Task.Run(() =>
                        {
                            StringCollection stringCollection = new StringCollection();
                            foreach (var item in vieModel.ScanEuPath) if (Directory.Exists(item)) { stringCollection.Add(item); }
                            List<string> filepaths = Scan.ScanPaths(stringCollection, ct);
                            totalnum = filepaths.Count;
                            insertnum=Scan.DistinctMovieAndInsert(filepaths, ct, true);
                        });

                        LoadingStackPanel.Visibility = Visibility.Hidden;
                        if (!cts.IsCancellationRequested) new PopupWindow(this, $"扫描出 {totalnum} 个，导入 {insertnum} 个", true).Show();
                    }
                    finally

                    {
                        cts.Dispose();
                        Running = false;
                    }

                    break;

                case 4:

                    //if (IsDownLoading()) { new PopupWindow(this, "请等待下载结束！").Show(); break; }

                    //if (new Msgbox(this, "删除不可逆，是否继续？").ShowDialog() == false) { break; }



                    //string InfoDataBasePath = AppDomain.CurrentDomain.BaseDirectory + "Info.sqlite";
                    //try
                    //{

                    //    //数据库管理
                    //    var cb = CheckBoxStackPanel.Children.OfType<CheckBox>().ToList();

                    //    if ((bool)cb[0].IsChecked)
                    //    {
                    //        //重置信息
                    //        DataBase cdb = new DataBase();
                    //        cdb.DeleteTable("movie");
                    //        cdb.CreateTable(StaticVariable.SQLITETABLE_MOVIE);
                    //        cdb.CloseDB();
                    //    }

                    //    if ((bool)cb[1].IsChecked)
                    //    {
                    //        //删除不存在影片
                    //        DataBase cdb = new DataBase("");
                    //        var movies = cdb.SelectMoviesBySql("select * from movie");
                    //        movies.ForEach(movie =>
                    //        {
                    //            if (!File.Exists(movie.filepath))
                    //            {
                    //                cdb.DelInfoByType("movie", "id", movie.id);
                    //            }
                    //        });
                    //        cdb.CloseDB();



                    //    }

                    //    if ((bool)cb[2].IsChecked)
                    //    {
                    //        //Vaccum
                    //        DataBase cdb = new DataBase();
                    //        cdb.Vaccum();
                    //        cdb.CloseDB();
                    //        cdb = new DataBase("Image");
                    //        cdb.Vaccum();
                    //        cdb.CloseDB();
                    //    }

                    //    if (!cts.IsCancellationRequested) new PopupWindow(this, "成功！").Show();
                    //}
                    //finally 
                    //{
                    //    cts.Dispose();
                    //    Running = false;
                    //}

                    //Main main = null;
                    //Window window = Jvedio.GetWindow.Get("Main");
                    //if (window != null) main = (Main)window;
                    //main?.vieModel.Reset();

                    break;

                case 5:
                    //网络驱动器
                    LoadingStackPanel.Visibility = Visibility.Visible;

                    string path = UNCPathTextBox.Text;
                    if (path == "") { break; }

                    bool CanScan = true;
                    //检查权限
                    await Task.Run(() =>
                    {
                        try { var tl = Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly); }
                        catch { CanScan = false; }
                    });

                    if (!CanScan) { LoadingStackPanel.Visibility = Visibility.Hidden; new Msgbox(this, "权限不够！").ShowDialog(); break; }


                    bool IsEurope = (bool)ScanTypeRadioButton.IsChecked ? false : true;

                    totalnum = 0;
                    insertnum = 0;
                    try
                    {
                        await Task.Run(() =>
                        {
                            StringCollection stringCollection = new StringCollection();
                            stringCollection.Add(path);
                            List<string> filepaths = Scan.ScanPaths(stringCollection, ct);
                            totalnum = filepaths.Count;
                            insertnum=Scan.DistinctMovieAndInsert(filepaths, ct, IsEurope);
                        });

                        LoadingStackPanel.Visibility = Visibility.Hidden;
                        if (!cts.IsCancellationRequested) new PopupWindow(this, $"扫描出 {totalnum} 个，导入 {insertnum} 个", true).Show();
                    }
                    finally
                    {
                        cts.Dispose();
                        Running = false;
                    }
                    break;

                default:

                    break;

            }
            Running = false;

        }



        public bool IsDownLoading()
        {
            bool result = false;
            Main main = null;
            Window window = Jvedio.GetWindow.Get("Main");
            if (window != null) main = (Main)window;

            WindowDownLoad WindowDownLoad = null;
            window = Jvedio.GetWindow.Get("WindowDownLoad");
            if (window != null) WindowDownLoad = (WindowDownLoad)window;


            if (main?.DownLoader != null)
            {
                if (main.DownLoader.State == DownLoadState.DownLoading | main.DownLoader.State == DownLoadState.Pause)
                {
                    Console.WriteLine("main.DownLoader.State   " + main.DownLoader.State);
                    result = true;
                }


            }

            if (WindowDownLoad?.MultiDownLoader != null)
            {
                if (WindowDownLoad.MultiDownLoader.State == DownLoadState.DownLoading | WindowDownLoad.MultiDownLoader.State == DownLoadState.Pause)
                {
                    Console.WriteLine("WindowDownLoad.MultiDownLoader.State   " + WindowDownLoad.MultiDownLoader.State);
                    result = true;
                }
            }

            return result;
        }




        public void ShowRunInfo(object sender, RoutedEventArgs e)
        {
            try
            {

                var grids = MainGrid.Children.OfType<Grid>().ToList();
                int index = 0;
                for (int i = 0; i < grids.Count; i++) { if (grids[i].Visibility == Visibility.Visible) { index = i; break; } }
                string filepath = "";
                switch (index)
                {
                    case 0:
                        filepath = AppDomain.CurrentDomain.BaseDirectory + $"Log\\ScanLog\\{DateTime.Now.ToString("yyyy-MM-dd")}.log";
                        if (File.Exists(filepath)) Process.Start(filepath); else new PopupWindow(this, "不存在").Show();
                        break;

                    case 1:
                        filepath = AppDomain.CurrentDomain.BaseDirectory + $"Log\\DataBase\\{DateTime.Now.ToString("yyyy -MM-dd")}.log";
                        if (File.Exists(filepath)) Process.Start(filepath); else new PopupWindow(this, "不存在").Show();
                        break;
                    case 2:
                        filepath = AppDomain.CurrentDomain.BaseDirectory + $"Log\\ScanLog\\{DateTime.Now.ToString("yyyy-MM-dd")}.log";
                        if (File.Exists(filepath)) Process.Start(filepath); else new PopupWindow(this, "不存在").Show();
                        break;

                    case 3:
                        filepath = AppDomain.CurrentDomain.BaseDirectory + $"Log\\ScanLog\\{DateTime.Now.ToString("yyyy-MM-dd")}.log";
                        if (File.Exists(filepath)) Process.Start(filepath); else new PopupWindow(this, "不存在").Show();
                        break;

                    case 4:
                        new PopupWindow(this, "无报告").Show();
                        break;

                    case 5:
                        filepath = AppDomain.CurrentDomain.BaseDirectory + $"Log\\ScanLog\\{DateTime.Now.ToString("yyyy-MM-dd")}.log";
                        if (File.Exists(filepath)) Process.Start(filepath); else new PopupWindow(this, "不存在").Show();
                        break;

                    default:

                        break;
                }



            }
            catch { }
        }

        public void DelNFOPath(object sender, MouseButtonEventArgs e)
        {
            if (NFOPathListBox.SelectedIndex != -1)
            {
                for (int i = NFOPathListBox.SelectedItems.Count - 1; i >= 0; i--)
                {
                    vieModel.NFOScanPath.Remove(NFOPathListBox.SelectedItems[i].ToString());
                }
            }
        }

        public void ClearNFOPath(object sender, MouseButtonEventArgs e)
        {
            vieModel.NFOScanPath.Clear();
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Save();
            this.Hide();
        }


        public void StartScan(object sender, RoutedEventArgs e)
        {

        }

        public void CopyPicToPath(string id, string path)
        {
            string fatherpath = new FileInfo(path).DirectoryName;
            string[] files = null;
            try
            {
                files = Directory.GetFiles(fatherpath, "*.*", SearchOption.TopDirectoryOnly);
            }
            catch (Exception e)
            {
                Logger.LogE(e);
            }

            string ImageExt = "bmp;gif;ico;jpe;jpeg;jpg;png";
            List<string> ImageExtList = new List<string>(); foreach (var item in ImageExt.Split(';')) { ImageExtList.Add('.' + item); }

            //识别图片
            if (files != null)
            {
                var piclist = files.Where(s => ImageExtList.Contains(Path.GetExtension(s))).ToList();

                piclist.ForEach(arg =>
                {

                    if (arg.ToLower().IndexOf("poster") >= 0 || arg.ToLower().IndexOf($"{id.ToLower()}_s") >= 0)
                    {
                        try { File.Copy(arg, StaticVariable.BasePicPath + $"SmallPic\\{id}.jpg", true); }
                        catch { }

                    }
                    else if (arg.ToLower().IndexOf("fanart") >= 0 || arg.ToLower().IndexOf($"{id.ToLower()}_b") >= 0)
                    {
                        try { File.Copy(arg, StaticVariable.BasePicPath + $"BigPic\\{id}.jpg", true); }
                        catch { }
                    }
                });


            }
        }

        public Movie GetInfoFromNfo(string path)
        {
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(path);
            }
            catch { return null; }
            XmlNode rootNode = doc.SelectSingleNode("movie");
            if (rootNode == null) return null;
            Movie movie = new Movie();
            foreach (XmlNode node in rootNode.ChildNodes)
            {
                try
                {
                    switch (node.Name)
                    {
                        case "id": movie.id = node.InnerText.ToUpper(); break;
                        case "title": movie.title = node.InnerText; break;
                        case "release": movie.releasedate = node.InnerText; break;
                        case "director": movie.director = node.InnerText; break;
                        case "studio": movie.studio = node.InnerText; break;
                        case "rating": movie.rating = node.InnerText == "" ? 0 : float.Parse(node.InnerText); break;
                        case "plot": movie.plot = node.InnerText; break;
                        case "outline": movie.outline = node.InnerText; break;
                        case "year": movie.year = node.InnerText == "" ? 1970 : int.Parse(node.InnerText); break;
                        case "runtime": movie.runtime = node.InnerText == "" ? 0 : int.Parse(node.InnerText); break;
                        case "country": movie.country = node.InnerText; break;
                        case "source": movie.sourceurl = node.InnerText; break;
                        default: break;

                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                    Console.WriteLine(ex.Message);
                    continue;
                }
            }
            if (movie.id == "") { return null; }
            //视频类型

            movie.vediotype = (int)Identify.GetVedioType(movie.id);

            //扫描视频获得文件大小
            if (File.Exists(path))
            {
                string fatherpath = new FileInfo(path).DirectoryName;
                string[] files = null;
                try
                {
                    files = Directory.GetFiles(fatherpath, "*.*", SearchOption.TopDirectoryOnly);
                }
                catch (Exception e)
                {
                    Logger.LogE(e);
                }

                if (files != null)
                {

                    var movielist = Scan.FirstFilter(files.ToList(), movie.id);
                    if (movielist.Count == 1)
                    {
                        movie.filepath = movielist[0];
                    }
                    else if (movielist.Count > 1)
                    {
                        //分段视频
                        movie.filepath = movielist[0];
                        string subsection = "";
                        movielist.ForEach(arg => { subsection += arg + ";"; });
                        movie.subsection = subsection;
                    }
                }



            }

            //tag
            XmlNodeList tagNodes = doc.SelectNodes("/movie/tag");
            if (tagNodes != null)
            {
                string tags = "";
                foreach (XmlNode item in tagNodes)
                {
                    if (item.InnerText != "") { tags += item.InnerText + " "; }

                }
                if (tags.Length > 0)
                {

                    if (movie.id.IndexOf("FC2") >= 0)
                    {
                        movie.genre = tags.Substring(0, tags.Length - 1);
                    }
                    else
                    {
                        movie.tag = tags.Substring(0, tags.Length - 1);
                    }


                }
            }

            //genre
            XmlNodeList genreNodes = doc.SelectNodes("/movie/genre");
            if (genreNodes != null)
            {
                string genres = "";
                foreach (XmlNode item in genreNodes)
                {
                    if (item.InnerText != "") { genres += item.InnerText + " "; }

                }
                if (genres.Length > 0) { movie.genre = genres.Substring(0, genres.Length - 1); }
            }

            //actor
            XmlNodeList actorNodes = doc.SelectNodes("/movie/actor/name");
            if (actorNodes != null)
            {
                string actors = "";
                foreach (XmlNode item in actorNodes)
                {
                    if (item.InnerText != "") { actors += item.InnerText + " "; }
                }
                if (actors.Length > 0) { movie.actor = actors.Substring(0, actors.Length - 1); }
            }

            //fanart
            XmlNodeList fanartNodes = doc.SelectNodes("/movie/fanart/thumb");
            if (fanartNodes != null)
            {
                string extraimageurl = "";
                foreach (XmlNode item in fanartNodes)
                {
                    if (item.InnerText != "") { extraimageurl += item.InnerText + ";"; }
                }
                if (extraimageurl.Length > 0) { movie.extraimageurl = extraimageurl.Substring(0, extraimageurl.Length - 1); }
            }


            return movie;
        }

        private void DownloadMany(object sender, RoutedEventArgs e)
        {
            if (Running) { new Msgbox(this, "其他任务正在进行！").ShowDialog(); return; }
            if (IsDownLoading()) { new PopupWindow(this, "请等待下载结束！").Show(); return; }

            WindowDownLoad WindowDownLoad = null;
            Window window = Jvedio.GetWindow.Get("WindowDownLoad");
            if (window != null) { WindowDownLoad = (WindowDownLoad)window; WindowDownLoad.Close(); }
            WindowDownLoad = new WindowDownLoad();
            WindowDownLoad.Show();



        }



        private void InsertOneMovie(object sender, RoutedEventArgs e)
        {
            WindowEdit windowEdit = null;
            Window window = Jvedio.GetWindow.Get("WindowEdit");
            if (window != null) { windowEdit = (WindowEdit)window; windowEdit.Close(); }
            windowEdit = new WindowEdit();
            windowEdit.Show();

        }

        private void CancelRun(object sender, RoutedEventArgs e)
        {
            cts.Cancel();
            LoadingStackPanel.Visibility = Visibility.Hidden;
            new PopupWindow(this, "已取消！").Show();
            Running = false;
        }

        private void PathListBox_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Link;
            e.Handled = true;//必须加
        }

        private void PathListBox_Drop(object sender, DragEventArgs e)
        {
            string[] dragdropFiles = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var dragdropFile in dragdropFiles)
            {
                if (!IsFile(dragdropFile))
                {
                    bool PathConflict = false;
                    foreach (var path in vieModel.ScanPath)
                    {
                        if (dragdropFile.IndexOf(path) >= 0 | path.IndexOf(dragdropFile) >= 0)
                        {
                            PathConflict = true;
                            break;
                        }
                    }
                    if (!PathConflict) { vieModel.ScanPath.Add(dragdropFile); } 
                }
            }
        }

        private void AccessPathTextBox_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Link;
            e.Handled = true;//必须加
        }

        private void AccessPathTextBox_Drop(object sender, DragEventArgs e)
        {
            string[] dragdropFiles = (string[])e.Data.GetData(DataFormats.FileDrop);

            foreach (var dragdropFile in dragdropFiles)
            {
                if (IsFile(dragdropFile))
                {
                    if(new FileInfo(dragdropFile).Extension == ".mdb")
                    {
                        AccessPathTextBox.Text = dragdropFile;
                        break;
                    }
                }
            }
        }

        private void NFOPathListBox_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Link;
            e.Handled = true;//必须加
        }

        private void NFOPathListBox_Drop(object sender, DragEventArgs e)
        {
            string[] dragdropFiles = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var dragdropFile in dragdropFiles)
            {
                if (!IsFile(dragdropFile))
                {
                    bool PathConflict = false;
                    foreach (var path in vieModel.NFOScanPath)
                    {
                        if (dragdropFile.IndexOf(path) >= 0 | path.IndexOf(dragdropFile) >= 0)
                        {
                            PathConflict = true;
                            break;
                        }
                    }
                    if (!PathConflict) { vieModel.NFOScanPath.Add(dragdropFile); }
                }
            }
        }

        private void SingleNFOBorder_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Link;
            e.Handled = true;//必须加
        }

        private void SingleNFOBorder_Drop(object sender, DragEventArgs e)
        {
            string[] dragdropFiles = (string[])e.Data.GetData(DataFormats.FileDrop);

            foreach (var dragdropFile in dragdropFiles)
            {
                if (IsFile(dragdropFile))
                {
                    if (new FileInfo(dragdropFile).Extension == ".nfo")
                    {
                        NFOPathTextBox.Text = dragdropFile;
                        break;
                    }
                }
            }
        }

        private void EuropePathListBox_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Link;
            e.Handled = true;//必须加
        }

        private void EuropePathListBox_Drop(object sender, DragEventArgs e)
        {
            string[] dragdropFiles = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var dragdropFile in dragdropFiles)
            {
                if (!IsFile(dragdropFile))
                {
                    bool PathConflict = false;
                    foreach (var path in vieModel.ScanEuPath)
                    {
                        if (dragdropFile.IndexOf(path) >= 0 | path.IndexOf(dragdropFile) >= 0)
                        {
                            PathConflict = true;
                            break;
                        }
                    }
                    if (!PathConflict) { vieModel.ScanEuPath.Add(dragdropFile); }
                }
            }
        }

        private void UNCPathBorder_Drop(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Link;
            e.Handled = true;//必须加
        }

        private void UNCPathBorder_DragOver(object sender, DragEventArgs e)
        {
            string[] dragdropFiles = (string[])e.Data.GetData(DataFormats.FileDrop);

            foreach (var dragdropFile in dragdropFiles)
            {
                if (!IsFile(dragdropFile))
                {
                    UNCPathTextBox.Text = dragdropFile;
                    break;
                }
            }
        }
    }

    public class IntToVisibility : IValueConverter
    {
        //数字转换为选中项的地址
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int v = int.Parse(value.ToString());
            if (v <= 0)
            {
                return Visibility.Visible;
            }
            else
            {
                return Visibility.Hidden;
            }
        }

        //选中项地址转换为数字

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }


    }
}
