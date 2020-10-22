using Jvedio.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static Jvedio.StaticVariable;
using static Jvedio.StaticClass;
using System.Data;
using System.Windows.Controls.Primitives;
using FontAwesome.WPF;
using System.ComponentModel;
using DynamicData.Annotations;
using System.Runtime.CompilerServices;
using System.Windows.Interop;
using System.Runtime.InteropServices;

namespace Jvedio
{
    /// <summary>
    /// Settings.xaml 的交互逻辑
    /// </summary>
    public partial class Settings : Jvedio_BaseWindow
    {
        //public Point WindowPoint = new Point(100, 100);
        //public Size WindowSize = new Size(1200, 700);
        //public int WinState = 0;\


        public VieModel_Settings vieModel_Settings;
        public Settings()
        {
            InitializeComponent();

            vieModel_Settings = new VieModel_Settings();
            vieModel_Settings.Reset();

            this.DataContext = vieModel_Settings;


            //绑定中文字体
            foreach (FontFamily _f in Fonts.SystemFontFamilies)
            {
                LanguageSpecificStringDictionary _font = _f.FamilyNames;
                if (_font.ContainsKey(System.Windows.Markup.XmlLanguage.GetLanguage("zh-cn")))
                {
                    string _fontName = null;
                    if (_font.TryGetValue(System.Windows.Markup.XmlLanguage.GetLanguage("zh-cn"), out _fontName))
                    {
                        ComboBox_Ttile.Items.Add(_fontName);
                    }
                }
            }

            bool IsMatch = false;
            foreach(var item  in ComboBox_Ttile.Items)
            {
                if (Properties.Settings.Default.Font_Title_Family == item.ToString())
                {
                    ComboBox_Ttile.SelectedItem = item;
                    IsMatch = true;
                    break;
                }
            }

            if (!IsMatch) ComboBox_Ttile.SelectedIndex = 0;

            var childsps = MainGrid.Children.OfType<StackPanel>().ToList();
            foreach (var item in childsps) item.Visibility = Visibility.Hidden;
            childsps[Properties.Settings.Default.SettingsIndex].Visibility = Visibility.Visible;

            var RadioButtons = RadioButtonStackPanel.Children.OfType<RadioButton>().ToList();
            RadioButtons[Properties.Settings.Default.SettingsIndex].IsChecked = true;

            //设置网址
            //List<Server> servers = new List<Server>();
            //servers.Add(new Server() { IsEnable = false, Url = "https://www.fanbus.cam/",  Avaliable = true, ServerTitle = "JavBus", LastRefreshDate = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") });
            //servers.Add(new Server() { IsEnable = true, Url = "http://www.b47w.com/cn/", Avaliable = false, ServerTitle = "JavLibrary", LastRefreshDate = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") });
            //servers.Add(new Server() { IsEnable = true,  Url = "https://javdb5.com/", Avaliable = true, ServerTitle = "JavDB", LastRefreshDate = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") });
            ServersDataGrid.ItemsSource = vieModel_Settings.Servers;


        }



        #region "热键"





        private void hotkeyTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {

            Key currentKey = (e.Key == Key.System ? e.SystemKey : e.Key);

            if (currentKey == Key.LeftCtrl | currentKey == Key.LeftAlt | currentKey == Key.LeftShift)
            {
                if (!funcKeys.Contains(currentKey)) funcKeys.Add(currentKey);
            }
            else if ((currentKey >= Key.A && currentKey <= Key.Z) || (currentKey >= Key.D0 && currentKey <= Key.D9) || (currentKey >= Key.NumPad0 && currentKey <= Key.NumPad9))
            {
                key = currentKey;
            }
            else
            {
                //Console.WriteLine("不支持");
            }

            string singleKey = key.ToString();
            if (key.ToString().Length > 1)
            {
                singleKey = singleKey.ToString().Replace("D", "");
            }

            if (funcKeys.Count > 0)
            {
                if (key == Key.None)
                {
                    hotkeyTextBox.Text = string.Join("+", funcKeys);
                    _funcKeys = new List<Key>();
                    _funcKeys.AddRange(funcKeys);
                    _key = Key.None;
                }
                else
                {
                    hotkeyTextBox.Text = string.Join("+", funcKeys) + "+" + singleKey;
                    _funcKeys = new List<Key>();
                    _funcKeys.AddRange(funcKeys);
                    _key = key;
                }

            }
            else
            {
                if (key != Key.None)
                {
                    hotkeyTextBox.Text = singleKey;
                    _funcKeys = new List<Key>();
                    _key = key;
                }
            }




        }

        private void hotkeyTextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {

            Key currentKey = (e.Key == Key.System ? e.SystemKey : e.Key);

            if (currentKey == Key.LeftCtrl | currentKey == Key.LeftAlt | currentKey == Key.LeftShift)
            {
                if (funcKeys.Contains(currentKey)) funcKeys.Remove(currentKey);
            }
            else if ((currentKey >= Key.A && currentKey <= Key.Z) || (currentKey >= Key.D0 && currentKey <= Key.D9) || (currentKey >= Key.F1 && currentKey <= Key.F12))
            {
                if (currentKey == key)
                {
                    key = Key.None;
                }

            }


        }

        private void ApplyHotKey(object sender, RoutedEventArgs e)
        {
            bool containsFunKey = _funcKeys.Contains(Key.LeftAlt) | _funcKeys.Contains(Key.LeftCtrl) | _funcKeys.Contains(Key.LeftShift) | _funcKeys.Contains(Key.CapsLock);


            if (!containsFunKey | _key == Key.None)
            {
                new Msgbox(this,"必须为 功能键 + 数字/字母").ShowDialog();
            }
            else
            {
                //注册热键
                if (_key != Key.None & IsProperFuncKey(_funcKeys))
                {
                    uint fsModifiers = (uint)Modifiers.None;
                    foreach (Key key in _funcKeys)
                    {
                        if (key == Key.LeftCtrl) fsModifiers = fsModifiers | (uint)Modifiers.Control;
                        if (key == Key.LeftAlt) fsModifiers = fsModifiers | (uint)Modifiers.Alt;
                        if (key == Key.LeftShift) fsModifiers = fsModifiers | (uint)Modifiers.Shift;
                    }
                    VK = (uint)KeyInterop.VirtualKeyFromKey(_key);

                    
                    UnregisterHotKey(_windowHandle, HOTKEY_ID);//取消之前的热键
                    bool success = RegisterHotKey(_windowHandle, HOTKEY_ID, fsModifiers, VK);
                    if (!success) { MessageBox.Show("热键冲突！", "热键冲突"); }
                    {
                        //保存设置
                        Properties.Settings.Default.HotKey_Modifiers = fsModifiers;
                        Properties.Settings.Default.HotKey_VK = VK;
                        Properties.Settings.Default.HotKey_Enable = true;
                        Properties.Settings.Default.HotKey_String = hotkeyTextBox.Text;
                        Properties.Settings.Default.Save();
                        new PopupWindow(this, "设置成功！").ShowDialog();
                    }
                    
                }



            }
        }

        #endregion



        public void ShowHelp(object sender, RoutedEventArgs e)
        {
            new Msgbox(this, "将【-1,-2,-3…】或【_1,_2,_3…】或【cd1,cd2,cd3…】视为分段视频，暂不支持 【XXX-123A,XXX-123B】等的识别").ShowDialog();
        }


        public void AddPath(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            folderBrowserDialog.Description = "选择文件夹";
            folderBrowserDialog.ShowNewFolderButton = true;


            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK & !string.IsNullOrWhiteSpace(folderBrowserDialog.SelectedPath))
            {
                if (vieModel_Settings.ScanPath == null) { vieModel_Settings.ScanPath = new ObservableCollection<string>(); }
                if (!vieModel_Settings.ScanPath.Contains(folderBrowserDialog.SelectedPath)) { vieModel_Settings.ScanPath.Add(folderBrowserDialog.SelectedPath); }
                //保存
                Jvedio.StaticClass.SavePathToConfig(vieModel_Settings.DataBase, vieModel_Settings.ScanPath?.ToList());
            }





        }

        public async void TestAI(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            StackPanel stackPanel = button.Parent as StackPanel;
            CheckBox checkBox  = stackPanel.Children.OfType<CheckBox>().First();
            ImageAwesome imageAwesome = stackPanel.Children.OfType<ImageAwesome>().First();
            imageAwesome.Icon = FontAwesomeIcon.Refresh;
            imageAwesome.Spin = true;
            imageAwesome.Foreground = (SolidColorBrush)Application.Current.Resources["ForegroundSearch"];
            if (checkBox.Content.ToString() == "百度人脸识别")
            {
                
                string base64 = Resource_String.BaseImage64;
                System.Drawing.Bitmap bitmap = ImageProcess.Base64ToBitmap(base64);
                Dictionary<string, string> result;
                Int32Rect int32Rect;
                (result, int32Rect) = await TestBaiduAI(bitmap);
                if (result != null && int32Rect != Int32Rect.Empty)
                {
                    imageAwesome.Icon = FontAwesomeIcon.CheckCircle;
                    imageAwesome.Spin = false;
                    imageAwesome.Foreground = new SolidColorBrush(Color.FromRgb(32, 183, 89));
                    new PopupWindow(this, "成功！").Show();
                }
                else
                {
                    imageAwesome.Icon = FontAwesomeIcon.TimesCircle;
                    imageAwesome.Spin = false;
                    imageAwesome.Foreground = new SolidColorBrush(Colors.Red);
                    new PopupWindow(this, "失败！").Show();
                }
            }
        }

        public static  Task<(Dictionary<string, string>,Int32Rect)> TestBaiduAI(System.Drawing.Bitmap bitmap)
        {
            return Task.Run(() => {
                string token = AccessToken.getAccessToken();
                string FaceJson = FaceDetect.faceDetect(token, bitmap);
                Dictionary<string, string> result;
                Int32Rect int32Rect;
                (result, int32Rect) = FaceParse.Parse(FaceJson);
                return (result, int32Rect);
            });

        }

        public async void TestTranslate(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            StackPanel stackPanel = button.Parent as StackPanel;
            CheckBox  checkBox = stackPanel.Children.OfType<CheckBox>().First();
            ImageAwesome  imageAwesome = stackPanel.Children.OfType<ImageAwesome>().First();
            imageAwesome.Icon = FontAwesomeIcon.Refresh;
            imageAwesome.Spin = true;
            imageAwesome.Foreground = (SolidColorBrush)Application.Current.Resources["ForegroundSearch"];

            if (checkBox.Content.ToString() == "百度翻译")
            {
                
            }else if (checkBox.Content.ToString() == "有道翻译")
            {
                string result = await Translate.Youdao("のマ○コに");
                if (result != "")
                {
                    imageAwesome.Icon = FontAwesomeIcon.CheckCircle;
                    imageAwesome.Spin = false;
                    imageAwesome.Foreground = new SolidColorBrush(Color.FromRgb(32, 183, 89));
                    new PopupWindow(this, "成功！").Show();
                }
                else
                {
                    imageAwesome.Icon = FontAwesomeIcon.TimesCircle;
                    imageAwesome.Spin = false;
                    imageAwesome.Foreground = new SolidColorBrush(Colors.Red);
                    new PopupWindow(this, "失败！").Show();
                }
            }


        }

        public void DelPath(object sender, MouseButtonEventArgs e)
        {
            if (PathListBox.SelectedIndex != -1)
            {
                for (int i = PathListBox.SelectedItems.Count - 1; i >= 0; i--)
                {
                    vieModel_Settings.ScanPath.Remove(PathListBox.SelectedItems[i].ToString());
                }
            }
            if (vieModel_Settings.ScanPath != null)
                SavePathToConfig(vieModel_Settings.DataBase, vieModel_Settings.ScanPath.ToList());
            
        }

        public void ClearPath(object sender, MouseButtonEventArgs e)
        {

            vieModel_Settings.ScanPath?.Clear();
            SavePathToConfig(vieModel_Settings.DataBase, new List<string>());
        }





        public void LabelMouseUp(object sender, RoutedEventArgs e)
        {
            RadioButton radioButton = (RadioButton)sender;
            StackPanel SP = radioButton.Parent as StackPanel;
            var radioButtons = SP.Children.OfType<RadioButton>().ToList();
            var childsps = MainGrid.Children.OfType<StackPanel>().ToList();
            foreach(var item in childsps) item.Visibility = Visibility.Hidden;
            childsps[radioButtons.IndexOf(radioButton)].Visibility = Visibility.Visible;
            Properties.Settings.Default.SettingsIndex = radioButtons.IndexOf(radioButton);
            Properties.Settings.Default.Save();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if(new Msgbox(this, "确认还原所有设置？").ShowDialog() == true)
            {
                //保存网址
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

                Properties.Settings.Default.Reset();

                Properties.Settings.Default.Bus = urlList[0];
                Properties.Settings.Default.BusEurope = urlList[1];
                Properties.Settings.Default.Library = urlList[2];
                Properties.Settings.Default.DB = urlList[3];
                Properties.Settings.Default.Fc2Club = urlList[4];
                Properties.Settings.Default.Jav321 = urlList[5];
                Properties.Settings.Default.DMM = urlList[6];

                Properties.Settings.Default.EnableBus = enableList[0];
                Properties.Settings.Default.EnableBusEu = enableList[1];
                Properties.Settings.Default.EnableLibrary = enableList[2];
                Properties.Settings.Default.EnableDB = enableList[3];
                Properties.Settings.Default.EnableFC2 = enableList[4];
                Properties.Settings.Default.Enable321 = enableList[5];
                Properties.Settings.Default.EnableDMM = enableList[6];



                Properties.Settings.Default.Save();

                new PopupWindow(this, "已恢复默认设置").Show();
            }

    }

        private void DisplayNumberTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            int num = 0;
            bool success = int.TryParse(textBox.Text, out num);
            if (success)
            {
                num = int.Parse(textBox.Text);
                if (num > 0 & num <=500)
                {
                    Properties.Settings.Default.DisplayNumber = num;
                    Properties.Settings.Default.Save();
                }
            }

        }

        private void FlowTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            int num = 0;
            bool success = int.TryParse(textBox.Text, out num);
            if (success ) 
            {
                num = int.Parse(textBox.Text);
                if (num > 0 & num <=30)
                {
                    Properties.Settings.Default.FlowNum = num;
                    Properties.Settings.Default.Save();
                }
            }
               
        }

        private void ActorTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            int num = 0;
            bool success = int.TryParse(textBox.Text, out num);
            if (success)
            {
                num = int.Parse(textBox.Text);
                if (num > 0 & num <= 50)
                {
                    Properties.Settings.Default.ActorDisplayNum = num;
                    Properties.Settings.Default.Save();
                }
            }

        }

        private void ScreenShotNumTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            int num = 0;
            bool success = int.TryParse(textBox.Text, out num);
            if (success)
            {
                num = int.Parse(textBox.Text);
                if (num > 0 & num <= 20)
                {
                    Properties.Settings.Default.ScreenShotNum = num;
                    Properties.Settings.Default.Save();
                }
            }

        }

        private void ScanMinFileSizeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            int num = 0;
            bool success = int.TryParse(textBox.Text, out num);
            if (success)
            {
                num = int.Parse(textBox.Text);
                if (num >= 0 & num <= 2000)
                {
                    Properties.Settings.Default.ScanMinFileSize = num;
                    Properties.Settings.Default.Save();
                }
            }

        }





        public async  void Test(object sender, RoutedEventArgs e)
        {
           
           
            Button btn = sender as Button;
            Grid grid = btn.Parent as Grid;
            TextBox tb = grid.Children.OfType<TextBox>().First();
            Image image= grid.Children.OfType<Image>().First();
            Label label = grid.Children.OfType<Label>().First();
            string url = tb.Text.ToLower();
            string cookie="";bool enablecookie = false;
            string labelcontent = label.Content.ToString();

            if (url == "default" & labelcontent == "FC2") url = Properties.Settings.Default.Fc2Club;
            if (url == "default" & labelcontent == "321") url = Properties.Settings.Default.Jav321;
            if (url == "default" & labelcontent == "DMM") url = Properties.Settings.Default.DMM;
            if (labelcontent == "DB") { enablecookie = true; cookie = Properties.Settings.Default.DBCookie; }
            if (enablecookie & cookie == "") { new PopupWindow(this, "勾选后输入 Cookie 再尝试！").Show(); return; }

            if (url.IndexOf("http") < 0) { url = "https://" + url; }
            if (url.Substring(url.Length - 1, 1) != "/") { url=url+"/"; }
            
            bool result = await Net.TestUrl(url, enablecookie, cookie, labelcontent);
            if (result)
            {
                image.Source = new BitmapImage(new Uri(@"/Resources/Picture/status_success.png", UriKind.Relative));
                if (label.Content.ToString() != "FC2" & label.Content.ToString() != "321" & label.Content.ToString() != "DMM") { tb.Text = url; }
            }
            else { image.Source = new BitmapImage(new Uri(@"/Resources/Picture/status_fail.png", UriKind.Relative)); }

            Main main = App.Current.Windows[0] as Main;
            main.BeginCheckurlThread();
        }

        private void ListenCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            if (checkBox.IsVisible == false) return;
            if ((bool)checkBox.IsChecked)
            {
                //测试是否能监听
                if (!TestListen())
                    checkBox.IsChecked = false;
                else
                    new PopupWindow(this, "重启后生效！").Show();
            }
        }


        FileSystemWatcher[] watchers;

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public bool TestListen()
        {
            string[] drives = Environment.GetLogicalDrives();
            watchers = new FileSystemWatcher[drives.Count()];
            for (int i = 0; i < drives.Count(); i++)
            {
                try
                {

                    if (drives[i] == @"C:\") { continue; }
                    FileSystemWatcher watcher = new FileSystemWatcher();
                    watcher.Path = drives[i];
                    watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                    watcher.Filter = "*.*";
                    watcher.EnableRaisingEvents = true;
                    watchers[i] = watcher;
                    watcher.Dispose();
                }
                catch
                {
                    new PopupWindow(this, $"无权限监听{drives[i]}").Show();
                    return false;
                }
            }
            return true;
        }

        private void SetVediaPlaterPath(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog OpenFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            OpenFileDialog1.Title = "选择播放器所在路径";
            OpenFileDialog1.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
            OpenFileDialog1.Filter = "可执行文件|*.exe";
            OpenFileDialog1.FilterIndex = 1;
            OpenFileDialog1.RestoreDirectory = true;
            if (OpenFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string exePath = OpenFileDialog1.FileName;
                if (File.Exists(exePath))
                    Properties.Settings.Default.VedioPlayerPath = exePath;
                       
            }
        }

        private void SetBasePicPath(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "请选择保存图片的路径";
            if (Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "Pic\\")) dialog.SelectedPath = AppDomain.CurrentDomain.BaseDirectory + "Pic\\";
            dialog.ShowNewFolderButton = true;
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (string.IsNullOrEmpty(dialog.SelectedPath))
                {
                    System.Windows.MessageBox.Show(this, "文件夹路径不能为空", "提示");
                    return;
                }
                else
                {
                    string path= dialog.SelectedPath; 
                    if (path.Substring(path.Length - 1, 1) != "\\") { path = path + "\\"; }
                    Properties.Settings.Default.BasePicPath = path;
                    
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (Properties.Settings.Default.Opacity_Main >= 0.5)
                App.Current.Windows[0].Opacity = Properties.Settings.Default.Opacity_Main;
            else
                App.Current.Windows[0].Opacity = 1;

            ////UpdateServersEnable();

            StaticVariable.InitVariable();
            Scan.InitSearchPattern();
            Net.Init();
            new PopupWindow(this, "保存成功").Show();
        }

        private void SetFFMPEGPath(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog OpenFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            OpenFileDialog1.Title = "选择 ffmpeg.exe 所在路径";
            OpenFileDialog1.FileName = "ffmpeg.exe";
            OpenFileDialog1.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
            OpenFileDialog1.Filter = "ffmpeg.exe|*.exe";
            OpenFileDialog1.FilterIndex = 1;
            OpenFileDialog1.RestoreDirectory = true;
            if (OpenFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string exePath = OpenFileDialog1.FileName;
                if (File.Exists(exePath))
                {
                    if(new FileInfo(exePath).Name.ToLower()== "ffmpeg.exe")
                        Properties.Settings.Default.FFMPEG_Path = exePath;
                }
            }
        }

        private void SetSkin(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Themes = (sender as RadioButton).Content.ToString();
            Properties.Settings.Default.Save();
            Main main = App.Current.Windows[0] as Main;
            main?.SetSkin();
            main?.SetSelected();
            main?.ActorSetSelected();
        }

        

                    private void SetLanguage(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Language = (sender as RadioButton).Content.ToString();
            Properties.Settings.Default.Save();
        }

        private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if(SubsectionMenu.Visibility == Visibility.Hidden)
                SubsectionMenu.Visibility = Visibility.Visible;
            else
                SubsectionMenu.Visibility = Visibility.Hidden;
        }

        private void Border_MouseLeftButtonUp1(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Forms.ColorDialog colorDialog = new System.Windows.Forms.ColorDialog();
            colorDialog.Color = System.Drawing.ColorTranslator.FromHtml(Properties.Settings.Default.Selected_BorderBrush);
            if(colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Properties.Settings.Default.Selected_BorderBrush = System.Drawing.ColorTranslator.ToHtml(colorDialog.Color);
                Properties.Settings.Default.Save();
            }


        }

        private void Border_MouseLeftButtonUp2(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Forms.ColorDialog colorDialog = new System.Windows.Forms.ColorDialog();
            colorDialog.Color = System.Drawing.ColorTranslator.FromHtml(Properties.Settings.Default.Selected_BorderBrush);
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Properties.Settings.Default.Selected_Background = System.Drawing.ColorTranslator.ToHtml(colorDialog.Color);
                Properties.Settings.Default.Save();
            }

        }

        private void ComboBox_Ttile_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
            if (e.RemovedItems.Count > 0)
            {
                if (ComboBox_Ttile.Text != "")
                {
                    Properties.Settings.Default.Font_Title_Family = (sender as ComboBox).SelectedItem.ToString();
                    Properties.Settings.Default.Save();
                }
            }



        }

        private void DatabaseComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0) return;
            vieModel_Settings.DataBase = e.AddedItems[0].ToString();
            vieModel_Settings.Reset();




        }

        private void Jvedio_BaseWindow_ContentRendered(object sender, EventArgs e)
        {
            //设置当前数据库
            for (int i = 0; i < vieModel_Settings.DataBases.Count; i++)
            {
                if (vieModel_Settings.DataBases[i].ToLower() == Properties.Settings.Default.DataBasePath.Split('\\').Last().Split('.').First().ToLower())
                {
                    DatabaseComboBox.SelectedIndex = i;
                    break;
                }
            }

            if (vieModel_Settings.DataBases.Count == 1) DatabaseComboBox.Visibility = Visibility.Hidden;

            //ServersDataGrid.Columns[0].Header = "是否启用";
            //ServersDataGrid.Columns[1].Header = "服务器地址";
            //ServersDataGrid.Columns[2].Header = "是否可用";
            //ServersDataGrid.Columns[3].Header = "服务器名称";
            //ServersDataGrid.Columns[4].Header = "上一次更新时间";
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.kancloud.cn/hitchao/jvedio/1921335");
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.kancloud.cn/hitchao/jvedio/1921336");
        }

        private void PathListBox_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Link;
            e.Handled = true;//必须加
        }

        private void PathListBox_Drop(object sender, DragEventArgs e)
        {
            if (vieModel_Settings.ScanPath == null) { vieModel_Settings.ScanPath = new ObservableCollection<string>(); }
            string[] dragdropFiles = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var item in dragdropFiles)
            {
                if (!IsFile(item))
                {
                    if (!vieModel_Settings.ScanPath.Contains(item)) { vieModel_Settings.ScanPath.Add(item); }
                }

            }
            //保存
            Jvedio.StaticClass.SavePathToConfig(vieModel_Settings.DataBase, vieModel_Settings.ScanPath.ToList());

        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            //选择NFO存放位置
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "请选择保存 NFO 的路径";
            dialog.ShowNewFolderButton = true;
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (string.IsNullOrEmpty(dialog.SelectedPath))
                {
                    System.Windows.MessageBox.Show(this, "文件夹路径不能为空", "提示");
                    return;
                }
                else
                {
                    string path = dialog.SelectedPath;
                    if (path.Substring(path.Length - 1, 1) != "\\") { path = path + "\\"; }
                    Properties.Settings.Default.NFOSavePath = path;
                }
            }

        }

        private void ServersDataGrid_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {

        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            if (vieModel_Settings.Servers.Count >= 10) return;

            //ServersDataGrid.ItemsSource = null;
            vieModel_Settings.Servers.Add(new Server()
            {
                IsEnable = true,
                Url = "请输入网址",
                Cookie = "无",
                Available = 0,
                ServerTitle = "",
                LastRefreshDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            }); ;
            ServersDataGrid.ItemsSource = vieModel_Settings.Servers;

            //TextBox tb = GetVisualChild<TextBox>(GetCell(vieModel_Settings.Servers.Count-1, 1));
            //tb.Focus();
            //tb.SelectAll();

        }


        private int  ServersDataGrid_RowIndex=0;
        private void test_Click(object sender, RoutedEventArgs e)
        {
            FocusTextBox.Focus();
            //UpdateServersEnable();
            var button = (FrameworkElement)sender;
            var row = (DataGridRow)button.Tag;
            int rowIndex = ServersDataGrid_RowIndex;
            Server server = vieModel_Settings.Servers[rowIndex];
            CheckBox cb = GetVisualChild<CheckBox>(GetCell(rowIndex, 0));

            CheckUrl( server,cb);
        }

        private void delete_Click(object sender, RoutedEventArgs e)
        {
            //if(new Msgbox(this, "删除后不可恢复，是否删除？").ShowDialog() == false) { return; }
            FocusTextBox.Focus();

            Server server = vieModel_Settings.Servers[ServersDataGrid_RowIndex];

            //保存到文件
            if (server.ServerTitle == "JavBus" | server.Url.ToLower()==Properties.Settings.Default.Bus.ToLower())
            {
                Properties.Settings.Default.Bus = "";
                DeleteServerInfoFromConfig(WebSite.Bus);
            }
            else if (server.ServerTitle == "JavBus Europe" | server.Url.ToLower() == Properties.Settings.Default.BusEurope.ToLower())
            {
                Properties.Settings.Default.BusEurope = "";
                DeleteServerInfoFromConfig(WebSite.BusEu);
            }
                
            else if (server.ServerTitle == "JavDB" | server.Url.ToLower() == Properties.Settings.Default.DB.ToLower())
            {
                Properties.Settings.Default.DB = "";
                DeleteServerInfoFromConfig(WebSite.DB);
            }
                
            else if (server.ServerTitle == "JavLibrary" | server.Url.ToLower() == Properties.Settings.Default.Library.ToLower())
            {
                Properties.Settings.Default.Library = "";
                DeleteServerInfoFromConfig(WebSite.Library);
            }
               
            else if (server.ServerTitle == "FANZA" | server.Url.ToLower() == Properties.Settings.Default.DMM.ToLower())
            {
                Properties.Settings.Default.DMM = "";
                DeleteServerInfoFromConfig(WebSite.DMM);
            }
                
            else if (server.ServerTitle == "JAV321" | server.Url.ToLower() == Properties.Settings.Default.Jav321.ToLower())
            {
                Properties.Settings.Default.Jav321 = "";
                DeleteServerInfoFromConfig(WebSite.Jav321);
            }

            //如果本地没有，则判断 properties



            vieModel_Settings.Servers.RemoveAt(ServersDataGrid_RowIndex);
            ServersDataGrid.ItemsSource = vieModel_Settings.Servers;
            ServersDataGrid.Items.Refresh();










        }


        private void Previe_Mouse_LBtnDown(object sender, MouseButtonEventArgs e)
        {
            DataGridRow dgr = null;
            var visParent = VisualTreeHelper.GetParent(e.OriginalSource as FrameworkElement);
            while (dgr == null && visParent != null)
            {
                dgr = visParent as DataGridRow;
                visParent = VisualTreeHelper.GetParent(visParent);
            }
            if (dgr == null) { return; }

            ServersDataGrid_RowIndex = dgr.GetIndex();
        }


        private async void CheckUrl(Server server, CheckBox checkBox)
        {
            ServersDataGrid.ItemsSource = vieModel_Settings.Servers;
            server.LastRefreshDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            server.Available = 2;
            ServersDataGrid.Items.Refresh();
            if (server.Url.IndexOf("http") < 0) server.Url = "https://" + server.Url; 
            if (!server.Url.EndsWith("/")) server.Url = server.Url + "/";

            bool enablecookie = false;
            string label = "";

            (bool result,string title) = await Net.TestAndGetTitle(server.Url, enablecookie, server.Cookie, label);
            if (result && title!="")
            {
                server.Available = 1;

                if(title.IndexOf("JavBus")>=0 && title.IndexOf("歐美") < 0)
                {
                    server.ServerTitle = "JavBus";
                }
                else if (title.IndexOf("JavBus") >= 0 && title.IndexOf("歐美") >= 0)
                {
                    server.ServerTitle = "JavBus Europe";
                }
                else if (title.IndexOf("JavDB") >= 0)
                {
                    server.ServerTitle = "JavDB";
                }
                else if (title.IndexOf("JavLibrary") >= 0)
                {
                    server.ServerTitle = "JavLibrary";
                }
                else if (title.IndexOf("FANZA") >= 0)
                {
                    server.ServerTitle = "FANZA";
                }
                else if (title.IndexOf("FANZA") >= 0)
                {
                    server.ServerTitle = "FANZA";
                }
                else if (title.IndexOf("JAV321") >= 0)
                {
                    server.ServerTitle = "JAV321";
                }
                else
                {
                    server.ServerTitle = title;
                }


            }
            else {
                server.Available = -1;
            }
            ServersDataGrid.Items.Refresh();

            //保存，重复的会覆盖
            if (server.ServerTitle== "JavBus")
            {
                Properties.Settings.Default.Bus = server.Url;
                Properties.Settings.Default.EnableBus = (bool)checkBox.IsChecked;

                SaveServersInfoToConfig(WebSite.Bus, new List<string>() { server.Url, server.ServerTitle, server.LastRefreshDate });
            }
            else if (server.ServerTitle == "JavBus Europe")
            {
                Properties.Settings.Default.BusEurope = server.Url;
                Properties.Settings.Default.EnableBusEu = (bool)checkBox.IsChecked;
                SaveServersInfoToConfig(WebSite.BusEu, new List<string>() { server.Url, server.ServerTitle, server.LastRefreshDate });
            }
            else if (server.ServerTitle == "JavDB")
            {
                //是否包含 cookie
                if(server.Cookie=="无" || server.Cookie == "")
                {
                    new Msgbox(this, "该网址需要填入 Cookie !，填入后在测试一次！").ShowDialog();
                }
                else
                {
                    Properties.Settings.Default.DB = server.Url;
                    Properties.Settings.Default.EnableDB = (bool)checkBox.IsChecked;
                    Properties.Settings.Default.DBCookie = server.Cookie;
                    SaveServersInfoToConfig(WebSite.DB, new List<string>() { server.Url, server.ServerTitle, server.LastRefreshDate });
                }
                
            }
            else if (server.ServerTitle == "JavLibrary")
            {
                Properties.Settings.Default.Library = server.Url;
                Properties.Settings.Default.EnableLibrary = (bool)checkBox.IsChecked;
                SaveServersInfoToConfig(WebSite.Library, new List<string>() { server.Url, server.ServerTitle, server.LastRefreshDate });
            }
            else if (server.ServerTitle == "FANZA")
            {
                Properties.Settings.Default.DMM = server.Url;
                Properties.Settings.Default.EnableDMM = (bool)checkBox.IsChecked;
                SaveServersInfoToConfig(WebSite.DMM, new List<string>() { server.Url, server.ServerTitle, server.LastRefreshDate });
            }
            else if (server.ServerTitle == "JAV321")
            {
                Properties.Settings.Default.Jav321 = server.Url;
                Properties.Settings.Default.Enable321 = (bool)checkBox.IsChecked;
                SaveServersInfoToConfig(WebSite.Jav321, new List<string>() { server.Url, server.ServerTitle, server.LastRefreshDate });
            }
                Properties.Settings.Default.Save();

        }

        


        public static T GetVisualChild<T>(Visual parent) where T : Visual

        {

            T child = default(T);

            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < numVisuals; i++)

            {

                Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);

                child = v as T;

                if (child == null)

                {

                    child = GetVisualChild<T>

                    (v);

                }

                if (child != null)

                {

                    break;

                }

            }

            return child;

        }

        public DataGridCell GetCell(int row, int column)

        {

            DataGridRow rowContainer = GetRow(row);

            if (rowContainer != null)

            {

                DataGridCellsPresenter presenter = GetVisualChild<DataGridCellsPresenter>(rowContainer);

                if (presenter == null)

                {

                    ServersDataGrid.ScrollIntoView(rowContainer, ServersDataGrid.Columns[column]);

                    presenter = GetVisualChild<DataGridCellsPresenter>(rowContainer);

                }

                DataGridCell cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(column);

                return cell;

            }

            return null;

        }

        public DataGridRow GetRow(int index)

        {

            DataGridRow row = (DataGridRow)ServersDataGrid.ItemContainerGenerator.ContainerFromIndex(index);

            if (row == null)

            {

                ServersDataGrid.UpdateLayout();

                ServersDataGrid.ScrollIntoView(ServersDataGrid.Items[index]);

                row = (DataGridRow)ServersDataGrid.ItemContainerGenerator.ContainerFromIndex(index);

            }

            return row;

        }

        private void CheckBox_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            bool enable = !(bool)((CheckBox)sender).IsChecked;
            vieModel_Settings.Servers[ServersDataGrid_RowIndex].IsEnable = enable;

            if (vieModel_Settings.Servers[ServersDataGrid_RowIndex].ServerTitle == "JavBus")
                Properties.Settings.Default.EnableBus = enable;
            else if (vieModel_Settings.Servers[ServersDataGrid_RowIndex].ServerTitle == "JavBus Europe") 
                Properties.Settings.Default.EnableBusEu = enable;
            else if (vieModel_Settings.Servers[ServersDataGrid_RowIndex].ServerTitle == "JavDB")
                Properties.Settings.Default.EnableDB = enable;
            else if (vieModel_Settings.Servers[ServersDataGrid_RowIndex].ServerTitle == "JavLibrary")
                Properties.Settings.Default.EnableLibrary = enable;
            else if (vieModel_Settings.Servers[ServersDataGrid_RowIndex].ServerTitle == "FANZA")
                Properties.Settings.Default.EnableDMM = enable;
            else if (vieModel_Settings.Servers[ServersDataGrid_RowIndex].ServerTitle == "JAV321")
                Properties.Settings.Default.Enable321 = enable;

            Properties.Settings.Default.Save();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            //注册热键
            uint modifier = Properties.Settings.Default.HotKey_Modifiers;
            uint vk = Properties.Settings.Default.HotKey_VK;

            if ( modifier != 0 && vk != 0)
            {
                UnregisterHotKey(_windowHandle, HOTKEY_ID);//取消之前的热键
                bool success = RegisterHotKey(_windowHandle, HOTKEY_ID, modifier, vk);
                if (!success) { MessageBox.Show("热键冲突！", "热键冲突");
                    Properties.Settings.Default.HotKey_Enable = false;
                }
            }
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            UnregisterHotKey(_windowHandle, HOTKEY_ID);//取消之前的热键
        }
    }



    public class SkinStringToCheckedConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if(value ==null || parameter == null)  return false;


            if (value.ToString() == parameter.ToString())
                return true;
            else
                return false;
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null || parameter == null) return null;
            if ((bool)value)
                return parameter.ToString();
            else
                return null;
        }
    }



    public class SkinTypeEnumConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return false;

            return (((Skin)value).ToString() == parameter.ToString());
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (bool)value ? Enum.Parse(typeof(Skin), parameter.ToString(), true) : null;
        }
    }


    //public class VedioTypeEnumConverter : IValueConverter
    //{
    //    public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
    //    {
    //        if (value == null)
    //            return false;

    //        return (((VedioType)value).ToString() == parameter.ToString());
    //    }

    //    public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
    //    {
    //        return (bool)value ? Enum.Parse(typeof(VedioType), parameter.ToString(), true) : null;
    //    }
    //}


    public class VedioTypeEnumConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            string typename = values[0].ToString();
            if (typename == Properties.Settings.Default.TypeName1)
                typename = "步兵";
            else if (typename == Properties.Settings.Default.TypeName2)
                typename = "骑兵";
            else if (typename == Properties.Settings.Default.TypeName3)
                typename = "欧美";
            else
                typename = "所有";
            string vediotype = values[1].ToString();

            if (typename == vediotype) 
                return true;
            else
                return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class LanguageTypeEnumConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return false;
            return (((Language)value).ToString() == parameter.ToString());
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (bool)value ? Enum.Parse(typeof(Language), parameter.ToString(), true) : null;
        }
    }


    public class MultiIntToMarginConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            int padUD = 5;
            int padLR = 5;
            int.TryParse(values[0].ToString(), out padUD);
            int.TryParse(values[1].ToString(), out padLR);

            return new Thickness(padLR, padUD, padLR, padUD);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class ColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {

            if (value == null )
            {
                if (parameter.ToString() == "BorderBrush")
                    return Brushes.Gold;
                else if (parameter.ToString() == "Background")
                    return Brushes.LightGreen;
            }

            if (parameter.ToString() == "BorderBrush")
                return Brushes.Gold;
            else 
                return Brushes.LightGreen;
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
            {
                if (parameter.ToString() == "BorderBrush")
                    return System.Drawing.Color.Gold;
                else if (parameter.ToString() == "Background")
                    return System.Drawing.Color.LightGreen;
            }

            return "#123455";



        }
    }


    public class FontFamilyToSelectedIndexConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {

            if (value == null)
                return 0;

            int i = -1;
            foreach(FontFamily item in Fonts.SystemFontFamilies)
            {
                i++;
                if (item.ToString() == value.ToString()) return i;
            }
            return 0;
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }


    public class BoolToImageStretchConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {

            if (value == null)
                return Stretch.Uniform;

            if ((bool)value)
                return Stretch.UniformToFill;
            else
                return Stretch.Uniform;


        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }

    }



    public class BoolToFontBoldConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {

            if (value == null)
                return FontWeights.Normal;

            if ((bool)value)
                return FontWeights.Bold;
            else
                return FontWeights.Normal;


        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }

    }

    

                    public class BoolToFontItalicConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {

            if (value == null)
                return FontStyles.Normal;

            if ((bool)value)
                return FontStyles.Italic;
            else
                return FontStyles.Normal;


        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }

    }


    public class BoolToUnderLineConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {

            if (value == null)
                return "";

            if ((bool)value)
                return "Underline";
            else
                return "";


        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }

    }

    public class Server
    {
        private bool isEnable;
        private string url;
        private string cookie;
        private int available;
        private string serverTitle;
        private string lastRefreshDate;

        public bool IsEnable { get => isEnable; set { isEnable = value; OnPropertyChanged(); } }


        public string Url { get => url; set { url = value; OnPropertyChanged(); } }
        public string Cookie { get => cookie; set { cookie = value; OnPropertyChanged(); } }

        public int Available { get => available; set { available = value; OnPropertyChanged(); } }
        public string ServerTitle { get => serverTitle; set { serverTitle = value; OnPropertyChanged(); } }
        public string LastRefreshDate { get => lastRefreshDate; set { lastRefreshDate = value; OnPropertyChanged(); } }


        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


    }
}
