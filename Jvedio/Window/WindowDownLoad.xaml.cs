
using Jvedio.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Jvedio
{
    /// <summary>
    /// WindowDownLoad.xaml 的交互逻辑
    /// </summary>
    public partial class WindowDownLoad : Jvedio_BaseWindow
    {
        public VieMoel_DownLoad vieModel;
        public MultiDownLoader MultiDownLoader;

        private object lockobject;

        public WindowDownLoad()
        {
            InitializeComponent();
            vieModel = new VieMoel_DownLoad();
            //vieModel.Reset();
            this.DataContext = vieModel;

        }






        public void ShowSetBar(object sender, MouseButtonEventArgs e)
        {
            if (vieModel.ShowSetBar)
            {
                vieModel.ShowSetBar = false;
            }
            else { vieModel.ShowSetBar = true; }

        }


        public void ShowSideBar(object sender, MouseButtonEventArgs e)
        {
            if (vieModel.ShowSideBar)
            {
                vieModel.ShowSideBar = false;
            }
            else { vieModel.ShowSideBar = true; }

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
                Properties.Settings.Default.Save();
                MultiDownLoader?.CancelDownload();

        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
                int num = 0;
                bool success = int.TryParse(((TextBox)sender).Text, out num);
                if (!success | num == 0)
                {
                    ((TextBox)sender).Text = Properties.Settings.Default.DLNum.ToString();
                }
                else
                {
                    num = int.Parse(((TextBox)sender).Text);
                    if (num > 100 | num <= 0)
                    {
                        ((TextBox)sender).Text = Properties.Settings.Default.DLNum.ToString();
                    }
                }
        }

        private void BeginDownLoad()
        {
            
            if (MultiDownLoader?.State == DownLoadState.Pause) { MultiDownLoader.ContinueDownload(); return; }
            if(MultiDownLoader?.State == DownLoadState.Completed | MultiDownLoader == null | MultiDownLoader?.State == DownLoadState.Fail) {

                lockobject = new object();

                double total = vieModel.TotalDownloadList.Count;
                List<DownLoadInfo> movies = new List<DownLoadInfo>();
                List<DownLoadInfo> moviesFC2 = new List<DownLoadInfo>();
                foreach (var item in vieModel.TotalDownloadList) { if (item.id.ToUpper().IndexOf("FC2") >= 0) { moviesFC2.Add(item); } else { movies.Add(item); } }
                MultiDownLoader = new MultiDownLoader(movies, moviesFC2);
                MultiDownLoader.StartThread();


                //更新UI
                MultiDownLoader.InfoUpdate += (s, ev) =>
                {
                    DownloadUpdateEventArgs eventArgs = ev as DownloadUpdateEventArgs;
                    lock (lockobject)
                    {
                        for (int i = 0; i < vieModel.TotalDownloadList.Count; i++)
                        {
                            if (vieModel.TotalDownloadList[i].id.ToUpper() == eventArgs.DownLoadInfo.id.ToUpper())
                            {
                                Dispatcher.BeginInvoke((Action)delegate () { 
                                    vieModel.TotalDownloadList[i] = eventArgs.DownLoadInfo;
                                    if (eventArgs.DownLoadInfo.progress >= eventArgs.DownLoadInfo.maximum)  vieModel.TotalProgress += 1; //总进度+1 
                                });
                                break;
                            }
                        }
                    }
                };


            }

        }




        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
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


        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            tb.SelectAll();
        }


        private void PreviousPage(object sender, MouseEventArgs e)
        {
            if (vieModel.CurrentPage - 1 <= 0)
            {
                vieModel.CurrentPage = vieModel.TotalPage;
            }
            else
            {
                vieModel.CurrentPage -= 1;
            }
            vieModel.FlipOver();
        }

        private void NextPage(object sender, MouseEventArgs e)
        {
            if (vieModel.CurrentPage + 1 > vieModel.TotalPage)
            {
                vieModel.CurrentPage = 1;
            }
            else
            {
                vieModel.CurrentPage += 1;
            }

            vieModel.FlipOver();

        }



        private void Button_Start(object sender, RoutedEventArgs e)
        {
            if (vieModel.TotalDownloadList == null | vieModel.TotalDownloadList?.Count==0) { return; }

            LoadButton.IsEnabled = false;
            Button button = sender as Button;
            if (button.Content.ToString() == "开始")
            {
                button.Content = "暂停";
                BeginDownLoad();
            }
            else if (button.Content.ToString() == "暂停")
            {
                MultiDownLoader?.PauseDownload();
                button.Content = "开始";
            }
                

            
        }

        private async void Button_Cancel(object sender, RoutedEventArgs e)
        {
            if (new Msgbox(this, "是否取消所有任务？").ShowDialog() == true)
            {
                LoadingGrid.Visibility = Visibility.Visible;
                await Task.Run(() => {
                    Dispatcher.Invoke((Action)delegate () {
                        MultiDownLoader?.CancelDownload();
                        ButtonBegin.Content = "开始";
                    });

                    Task.Delay(5000).Wait();

                    Dispatcher.Invoke((Action)delegate () {
                        vieModel.TotalDownloadList?.Clear();
                        vieModel.CurrentList?.Clear();
                        vieModel.TotalProgress = 0;
                        vieModel.TotalProgressMaximum = 1;
                        LoadButton.IsEnabled = true;
                    });

                });

                LoadingGrid.Visibility = Visibility.Hidden;

            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            vieModel.Reset();
        }


    }










}
