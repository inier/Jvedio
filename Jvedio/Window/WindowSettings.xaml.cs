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


        }





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
            }

            //保存
            Jvedio.StaticClass.SavePathToConfig(vieModel_Settings.DataBase, vieModel_Settings.ScanPath.ToList());



        }

        public async void TestAI(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            StackPanel stackPanel = button.Parent as StackPanel;
            CheckBox checkBox  = stackPanel.Children.OfType<CheckBox>().First();
            Image image = stackPanel.Children.OfType<Image>().First();

            if (checkBox.Content.ToString() == "百度人脸识别")
            {
                string base64 = "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";
                System.Drawing.Bitmap bitmap = ImageProcess.Base64ToBitmap(base64);

                Dictionary<string, string> result;
                Int32Rect int32Rect;
                (result, int32Rect) = await TestBaiduAI(bitmap);
                if (result != null && int32Rect != Int32Rect.Empty)
                {
                    image.Source = new BitmapImage(new Uri(@"/Resources/Picture/status_success.png", UriKind.Relative));
                    new PopupWindow(this, "成功！").Show();
                }
                else
                {
                    image.Source = new BitmapImage(new Uri(@"/Resources/Picture/status_fail.png", UriKind.Relative));
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
            Image image= stackPanel.Children.OfType<Image>().First();
            if (checkBox.Content.ToString() == "百度翻译")
            {
                
            }else if (checkBox.Content.ToString() == "有道翻译")
            {
                string result = await Translate.Youdao("のマ○コに");
                if (result != "")
                {
                    image.Source = new BitmapImage(new Uri(@"/Resources/Picture/status_success.png", UriKind.Relative));
                    new PopupWindow(this, "成功！").Show();
                }
                else
                {
                    image.Source = new BitmapImage(new Uri(@"/Resources/Picture/status_fail.png", UriKind.Relative));
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
            Jvedio.StaticClass.SavePathToConfig(vieModel_Settings.DataBase, vieModel_Settings.ScanPath.ToList());
        }

        public void ClearPath(object sender, MouseButtonEventArgs e)
        {

            vieModel_Settings.ScanPath.Clear();
            Jvedio.StaticClass.SavePathToConfig(vieModel_Settings.DataBase, new List<string>());
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
            if(new Msgbox(this, "确认还原所有设置（保留网址）？").ShowDialog() == true)
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
            StaticVariable.InitVariable();
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
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.kancloud.cn/hitchao/jvedio/1921271");
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
}
