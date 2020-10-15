using Jvedio.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static Jvedio.StaticClass;
using static Jvedio.StaticVariable;

namespace Jvedio
{
    /// <summary>
    /// WindowStartUp.xaml 的交互逻辑
    /// </summary>
    public partial class WindowStartUp : Window
    {

        public CancellationTokenSource cts;
        public CancellationToken ct;

        public VieModel_StartUp vieModel_StartUp;
        public WindowStartUp()
        {

            InitializeComponent();

            vieModel_StartUp = new VieModel_StartUp();
            vieModel_StartUp.ListDatabase();
            this.DataContext = vieModel_StartUp;

            cts = new CancellationTokenSource();
            cts.Token.Register(() =>  Console.WriteLine("取消任务"));
            ct = cts.Token;
        }






        public static string InfoDataBasePath = AppDomain.CurrentDomain.BaseDirectory + "Info.sqlite";
        public static string AIDataBasePath = AppDomain.CurrentDomain.BaseDirectory + "AI.sqlite";
        public static string TranslateDataBasePath = AppDomain.CurrentDomain.BaseDirectory + "Translate.sqlite";


        public async void LoadDataBase(object sender, MouseButtonEventArgs e)
        {
            //加载数据库
            StackPanel stackPanel = sender as StackPanel;
            TextBox TextBox = stackPanel.Children[1] as TextBox;

            string name = TextBox.Text.ToLower();
            if (name == "info")
                Properties.Settings.Default.DataBasePath = AppDomain.CurrentDomain.BaseDirectory + "info.sqlite";
            else if (name == "新建视频库")
            {
                //重命名
                TextBox.IsEnabled = true;
                TextBox.IsReadOnly = false;
                TextBox.Text = "我的视频";
                TextBox.Focus();
                TextBox.SelectAll();
                TextBox.Cursor = Cursors.IBeam;
                return;
            }
            else
                Properties.Settings.Default.DataBasePath = AppDomain.CurrentDomain.BaseDirectory + $"\\DataBase\\{name}.sqlite";


            if (!File.Exists(Properties.Settings.Default.DataBasePath)) return;

            SelectDbBorder.Visibility = Visibility.Hidden;

            if (Properties.Settings.Default.ScanGivenPath)
            {

                await Task.Run(() =>
                {
                    try
                    {
                        this.Dispatcher.BeginInvoke(new Action(() => { statusText.Text = $"扫描指定文件夹"; }), System.Windows.Threading.DispatcherPriority.Render);
                        List<string> filepaths = Scan.ScanPaths(ReadScanPathFromConfig(Properties.Settings.Default.DataBasePath.Split('\\').Last().Split('.').First()), ct);
                        DataBase cdb = new DataBase();
                        Scan.DistinctMovieAndInsert(filepaths, ct);
                        cdb.CloseDB();
                    }
                    catch(Exception ex)
                    {
                        Logger.LogF(ex);
                    }

                }, cts.Token);

            }


            //启动主窗口
            Main main = new Main();
            statusText.Text = "初始化影片";
            main.InitMovie();


            main.Show();
            this.Close();
        }

        private  void Window_Loaded(object sender, RoutedEventArgs e)
        {

            //更新配置文件
            if (Properties.Settings.Default.UpgradeRequired)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpgradeRequired = false;
                Properties.Settings.Default.Save();
            }


            //判断文件是否存在
            CheckFile();


            //修复设置错误
            CheckSettings();


            //Properties.Settings.Default.Reset();
            if (!Directory.Exists(Properties.Settings.Default.BasePicPath)) Properties.Settings.Default.BasePicPath = AppDomain.CurrentDomain.BaseDirectory + "Pic\\";


            //创建 Log文件夹
            if (!Directory.Exists("log")) { Directory.CreateDirectory("log"); }
            //创建 ScanLog 文件夹
            if (!Directory.Exists("log\\scanlog")) { Directory.CreateDirectory("log\\scanlog"); }
            //创建 DataBase 文件夹
            if (!Directory.Exists("DataBase")) { Directory.CreateDirectory("DataBase"); }

            //创建备份文件夹
            if (!Directory.Exists("BackUp")) { Directory.CreateDirectory("BackUp"); }



            SetSkin();
            
            statusText.Text = "启动中……";
            InitDataBase();//初始化数据库
            InitJav321IDConverter();//初始化 jav321，多 30M 内存
            //初始化参数
            Identify.InitFanhaoList();
            Scan.InitSearchPattern();
            StaticVariable.InitVariable();
            Net.Init();

            //创建图片文件夹
            if (!Directory.Exists(StaticVariable.BasePicPath + "ScreenShot\\")) { Directory.CreateDirectory(StaticVariable.BasePicPath + "ScreenShot\\"); }
            if (!Directory.Exists(StaticVariable.BasePicPath + "SmallPic\\")) { Directory.CreateDirectory(StaticVariable.BasePicPath + "SmallPic\\"); }
            if (!Directory.Exists(StaticVariable.BasePicPath + "BigPic\\")) { Directory.CreateDirectory(StaticVariable.BasePicPath + "BigPic\\"); }
            if (!Directory.Exists(StaticVariable.BasePicPath + "ExtraPic\\")) { Directory.CreateDirectory(StaticVariable.BasePicPath + "ExtraPic\\"); }
            if (!Directory.Exists(StaticVariable.BasePicPath + "Actresses\\")) { Directory.CreateDirectory(StaticVariable.BasePicPath + "Actresses\\"); }
            if (!Directory.Exists(StaticVariable.BasePicPath + "Gif\\")) { Directory.CreateDirectory(StaticVariable.BasePicPath + "Gif\\"); }



            if (Properties.Settings.Default.OpenDataBaseDefault)
            {
                OpenDefaultDatabase();//默认打开某个数据库
                //启动主窗口
                Main main = new Main();
                statusText.Text = "初始化影片";
                main.InitMovie();


                main.Show();
                this.Close();
            }
            else
            {
                SelectDbBorder.Visibility = Visibility.Visible;
            }


        }

        public async void OpenDefaultDatabase()
        {
            if (Properties.Settings.Default.ScanGivenPath)
            {

                await Task.Run(() =>
                {
                    this.Dispatcher.BeginInvoke(new Action(() => { statusText.Text = $"扫描指定文件夹"; }), System.Windows.Threading.DispatcherPriority.Render);
                    List<string> filepaths = Scan.ScanPaths(ReadScanPathFromConfig(Properties.Settings.Default.DataBasePath.Split('\\').Last().Split('.').First()), ct);
                    DataBase cdb = new DataBase();
                    Scan.DistinctMovieAndInsert(filepaths, ct);
                }, cts.Token);

            }
        }

        public void CheckSettings()
        {
            if (!Enum.IsDefined(typeof(Skin),Properties.Settings.Default.Themes))
            {
                Properties.Settings.Default.Themes = Skin.黑色.ToString();
                Properties.Settings.Default.Save();
            }

            if (!Enum.IsDefined(typeof(StaticVariable.Language), Properties.Settings.Default.Language))
            {
                Properties.Settings.Default.Language = StaticVariable. Language.中文.ToString();
                Properties.Settings.Default.Save();
            }

        }


        public void CheckFile()
        {
            if (!File.Exists(@"x64\SQLite.Interop.dll") || !File.Exists(@"x86\SQLite.Interop.dll"))
            {
                MessageBox.Show("缺失 SQLite.Interop.dll", "Jvedio");
                this.Close();
            }

            if (!File.Exists("BusActress.sqlite"))
            {
                MessageBox.Show("缺失 BusActress.sqlite", "Jvedio");
                this.Close();
            }




        }
        private void InitDataBase()
        {
            LockDataBase = new List<string>();

            if (!File.Exists(InfoDataBasePath))
            {
                DataBase cdb = new DataBase("Info");
                cdb.CreateTable(StaticVariable.SQLITETABLE_MOVIE);
                cdb.CreateTable(StaticVariable.SQLITETABLE_ACTRESS);
                cdb.CreateTable(StaticVariable.SQLITETABLE_LIBRARY);
                cdb.CreateTable(StaticVariable.SQLITETABLE_JAVDB);
                cdb.CloseDB();
            }
            else
            {
                //是否具有表结构
                DataBase cdb = new DataBase("Info");
                if (!cdb.IsTableExist("movie") | !cdb.IsTableExist("actress") | !cdb.IsTableExist("library") | !cdb.IsTableExist("javdb"))
                {
                    cdb.CreateTable(StaticVariable.SQLITETABLE_MOVIE);
                    cdb.CreateTable(StaticVariable.SQLITETABLE_ACTRESS);
                    cdb.CreateTable(StaticVariable.SQLITETABLE_LIBRARY);
                    cdb.CreateTable(StaticVariable.SQLITETABLE_JAVDB);
                }
                cdb.CloseDB();
            }


            if (!File.Exists(AIDataBasePath))
            {
                DataBase cdb = new DataBase("AI");
                cdb.CreateTable(StaticVariable.SQLITETABLE_BAIDUAI);
                cdb.CloseDB();
            }
            else
            {
                //是否具有表结构
                DataBase cdb = new DataBase("AI");
                if (!cdb.IsTableExist("baidu")) cdb.CreateTable(StaticVariable.SQLITETABLE_BAIDUAI);
                cdb.CloseDB();
            }


            if (!File.Exists(TranslateDataBasePath))
            {
                DataBase cdb = new DataBase("Translate");
                cdb.CreateTable(StaticVariable.SQLITETABLE_YOUDAO);
                cdb.CreateTable(StaticVariable.SQLITETABLE_BAIDUTRANSLATE);
                cdb.CloseDB();
            }
            else
            {
                //是否具有表结构
                DataBase cdb = new DataBase("Translate");
                if (!cdb.IsTableExist("youdao")) cdb.CreateTable(StaticVariable.SQLITETABLE_YOUDAO);
                if (!cdb.IsTableExist("baidu")) cdb.CreateTable(StaticVariable.SQLITETABLE_BAIDUTRANSLATE);
                cdb.CloseDB();
            }

            
        }


        public void SetSkin()
        {
            switch (Properties.Settings.Default.Themes)
            {
                case "黑色":
                    Application.Current.Resources["BackgroundTitle"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22252A"));
                    Application.Current.Resources["BackgroundMain"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#393D40"));
                    Application.Current.Resources["BackgroundSide"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#323639"));
                    Application.Current.Resources["BackgroundTab"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#383838"));
                    Application.Current.Resources["BackgroundSearch"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#18191B"));
                    Application.Current.Resources["BackgroundMenu"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("Black"));
                    Application.Current.Resources["ForegroundGlobal"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AFAFAF"));
                    Application.Current.Resources["ForegroundSearch"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
                    Application.Current.Resources["BorderBursh"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("Transparent"));
                    break;

                case "白色":
                    Application.Current.Resources["BackgroundTitle"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1D1D1"));
                    Application.Current.Resources["BackgroundMain"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
                    Application.Current.Resources["BackgroundSide"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5E5E5"));
                    Application.Current.Resources["BackgroundTab"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF5EE"));
                    Application.Current.Resources["BackgroundSearch"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EAEAE8"));
                    Application.Current.Resources["BackgroundMenu"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("White"));
                    Application.Current.Resources["ForegroundGlobal"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#555555"));
                    Application.Current.Resources["ForegroundSearch"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#000000"));
                    Application.Current.Resources["BorderBursh"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("Gray"));
                    break;

                case "蓝色":
                    Application.Current.Resources["BackgroundTitle"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0B72BD"));
                    Application.Current.Resources["BackgroundMain"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#87CEFF"));
                    Application.Current.Resources["BackgroundSide"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3DBEDE"));
                    Application.Current.Resources["BackgroundTab"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3DBEDE"));
                    Application.Current.Resources["BackgroundSearch"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#87CEEB"));
                    Application.Current.Resources["BackgroundMenu"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("LightBlue"));
                    Application.Current.Resources["ForegroundGlobal"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("White"));
                    Application.Current.Resources["ForegroundSearch"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("White"));
                    Application.Current.Resources["BorderBursh"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#95DCED"));
                    break;
            }
        }



        private void MoveWindow(object sender, MouseEventArgs e)
        {
            //移动窗口
            if (e.LeftButton == MouseButtonState.Pressed )
            {
                this.DragMove();
            }
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog OpenFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            OpenFileDialog1.Title = "选择数据库";
            OpenFileDialog1.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
            OpenFileDialog1.Filter = "Sqlite 文件|*.sqlite";
            OpenFileDialog1.Multiselect = true;
            OpenFileDialog1.RestoreDirectory = true;
            if (OpenFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string[] names = OpenFileDialog1.FileNames;

                foreach(var item in names)
                {
                    string name = item.Split('\\').Last().Split('.').First().ToLower();
                    if (name == "info" || name == "新建视频库") return;

                    if (!IsProPerSqlite(item)) continue;



                    if (File.Exists($"DataBase\\{name}.sqlite"))
                    {
                        if(new Msgbox(this,$"已存在 {name}，是否覆盖？").ShowDialog() == true)
                        {
                            File.Copy(item, $"DataBase\\{name}.sqlite", true);

                            if(!vieModel_StartUp.DataBases.Contains(name)) vieModel_StartUp.DataBases.Add(name);

                        }
                    }
                    else
                    {
                        File.Copy(item, $"DataBase\\{name}.sqlite", true);
                        if (!vieModel_StartUp.DataBases.Contains(name)) vieModel_StartUp.DataBases.Add(name);

                    }

                }



            }
        }

        private void Border_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        Image optionImage;

        private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            optionImage = (sender as Border).Child as Image;
            OptionPopup.IsOpen = true;
        }

        private void DelSqlite(object sender, RoutedEventArgs e)
        {
            string name = "";
            Border border = optionImage.Parent as Border;
            StackPanel sp = border.Parent as StackPanel;
            StackPanel stackPanel = sp.Children.OfType<StackPanel>().First();
            TextBox TextBox = stackPanel.Children[1] as TextBox;
            name = TextBox.Text.ToLower();

            if (name == "info" || name == "新建视频库") return;


            if (new Msgbox(this, $"是否确认删除{name}?").ShowDialog() == true)
            {
                string dirpath = DateTime.Now.ToString("yyyyMMddHHss");
                Directory.CreateDirectory($"BackUp\\{dirpath}");
                if (File.Exists($"DataBase\\{name}.sqlite"))
                {
                    //备份
                    File.Copy($"DataBase\\{name}.sqlite", $"BackUp\\{dirpath}\\{name}.sqlite", true);
                    //删除

                    File.Delete($"DataBase\\{name}.sqlite");

                    vieModel_StartUp.DataBases.Remove(name);

                }



            }
        }

        public string beforeRename="";

        private void RenameSqlite(object sender, RoutedEventArgs e)
        {
            string name = "";

            Border border = optionImage.Parent as Border;
            StackPanel sp = border.Parent as StackPanel;
            StackPanel stackPanel = sp.Children.OfType<StackPanel>().First();
            TextBox TextBox = stackPanel.Children[1] as TextBox;
            name = TextBox.Text.ToLower();

            if (name == "info") return;

            //重命名
            OptionPopup.IsOpen = false;
            TextBox.IsEnabled = true;
            TextBox.IsReadOnly = false;
            TextBox.Focus();
            TextBox.SelectAll();
            TextBox.Cursor = Cursors.IBeam;
            beforeRename = TextBox.Text;
        }


        private void Rename(TextBox textBox)
        {
            Console.WriteLine("Rename");
            string name = textBox.Text.ToLower();
            if (name == beforeRename) {

                textBox.IsEnabled = false;
                textBox.IsReadOnly = true;
                textBox.Cursor = Cursors.Hand;
                beforeRename = "";
                return; 
            }


            if (beforeRename == "")
            {
                if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrEmpty(name) && !vieModel_StartUp.DataBases.Contains(name) && name.IndexOfAny(Path.GetInvalidFileNameChars()) ==-1 )
                {
                //新建
                DataBase cdb = new DataBase("DataBase\\" + name);
                cdb.CreateTable(StaticVariable.SQLITETABLE_MOVIE);
                cdb.CreateTable(StaticVariable.SQLITETABLE_ACTRESS);
                cdb.CreateTable(StaticVariable.SQLITETABLE_LIBRARY);
                cdb.CreateTable(StaticVariable.SQLITETABLE_JAVDB);
                cdb.CloseDB();
                if (vieModel_StartUp.DataBases.Contains("新建视频库")) vieModel_StartUp.DataBases.Remove("新建视频库");

                textBox.IsEnabled = false;
                textBox.IsReadOnly = true;
                textBox.Cursor = Cursors.Hand;
                
                vieModel_StartUp.DataBases.Add(name);
                vieModel_StartUp.DataBases.Add("新建视频库");
                }
                else
                {
                    textBox.Text = "新建视频库";
                }
            }

            else
            {
                //重命名
                if (vieModel_StartUp.DataBases.Contains(name))
                {
                    textBox.Text = beforeRename; //重复的
                }
                else
                {
                    //重命名
                    if (name.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) == -1)
                    {
                        try
                        {
                            File.Move(AppDomain.CurrentDomain.BaseDirectory + $"DataBase\\{beforeRename}.sqlite",
                                AppDomain.CurrentDomain.BaseDirectory + $"DataBase\\{name}.sqlite");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            Logger.LogE(ex);
                        }
                    }
                    else
                    {
                        textBox.Text = beforeRename;
                    }


                }
                beforeRename = "";
            }

            textBox.IsEnabled = false;
            textBox.IsReadOnly = true;
            textBox.Cursor = Cursors.Hand;
           
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("TextBox_LostFocus");
            TextBox textBox = sender as TextBox;
            Rename(textBox);
        }

        private void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (e.Key == Key.Enter)
            {
                LoadButton.Focus();
            }
            else if (e.Key == Key.Escape)
            {
                textBox.IsEnabled = false;
                textBox.IsReadOnly = true;
                textBox.Cursor = Cursors.Hand;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Border border = optionImage.Parent as Border;
            StackPanel sp = border.Parent as StackPanel;
            StackPanel stackPanel = sp.Children.OfType<StackPanel>().First();
            TextBox TextBox = stackPanel.Children[1] as TextBox;
            string name = TextBox.Text.ToLower();
            Properties.Settings.Default.OpenDataBaseDefault = true;
            Properties.Settings.Default.DataBasePath = AppDomain.CurrentDomain.BaseDirectory + $"\\DataBase\\{name}.sqlite";
            OptionPopup.IsOpen = false;
            LoadDataBase(stackPanel, new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice, 0, MouseButton.Left));
        }
    }
}
