
using Jvedio.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Jvedio
{
    /// <summary>
    /// WindowAdvanceSearch.xaml 的交互逻辑
    /// </summary>
    public partial class WindowAdvanceSearch : Jvedio_BaseWindow
    {
        VieModel_AdvanceSearch advanceSearchModel;
        public WindowAdvanceSearch()
        {
            InitializeComponent();

            advanceSearchModel = new VieModel_AdvanceSearch();
            advanceSearchModel.Reset();
            this.DataContext = advanceSearchModel;

        }

        private void Search(object sender, RoutedEventArgs e)
        {
            

        }

        private void GenerateCommand(object sender, RoutedEventArgs e)
        {
            Main main=null;
            Window window = Jvedio.GetWindow.Get("Main");
            if (window != null) main = (Main)window;

            if (main.DownLoader?.State == DownLoadState.DownLoading) { new Msgbox(this, "请等待下载完成！").ShowDialog(); return; }


            var WrapPanels = FilterStackPanel.Children.OfType<WrapPanel>().ToList(); ;

            List<int> vediotype = new List<int>();
            WrapPanel wrapPanel = WrapPanels[0];
            foreach (var item in wrapPanel.Children)
            {
                if (item.GetType() == typeof(CheckBox))
                {
                    CheckBox cb = item as CheckBox;
                    if (cb != null)
                        if ((bool)cb.IsChecked)
                        {
                            if (cb.Content.ToString() == Properties.Settings.Default.TypeName1)
                                vediotype.Add(1);
                            else if (cb.Content.ToString() == Properties.Settings.Default.TypeName2)
                                vediotype.Add(2);
                            else if (cb.Content.ToString() == Properties.Settings.Default.TypeName3)
                                vediotype.Add(3);
                        }
                }
            }


            //年份
            wrapPanel = WrapPanels[1];
            ItemsControl itemsControl = wrapPanel.Children[0] as ItemsControl;
            List<string> year = GetFilterFromItemsControl(itemsControl);



            //时长
            wrapPanel = WrapPanels[2];
            itemsControl = wrapPanel.Children[0] as ItemsControl;
            List<string> runtime = GetFilterFromItemsControl(itemsControl);

            //文件大小
            wrapPanel = WrapPanels[3];
            itemsControl = wrapPanel.Children[0] as ItemsControl;
            List<string> filesize = GetFilterFromItemsControl(itemsControl);

            //评分
            wrapPanel = WrapPanels[4];
            itemsControl = wrapPanel.Children[0] as ItemsControl;
            List<string> rating = GetFilterFromItemsControl(itemsControl);


            //类别
            wrapPanel = WrapPanels[5];
            itemsControl = wrapPanel.Children[0] as ItemsControl;
            List<string> genre = GetFilterFromItemsControl(itemsControl);

            //演员
            wrapPanel = WrapPanels[6];
            itemsControl = wrapPanel.Children[0] as ItemsControl;
            List<string> actor = GetFilterFromItemsControl(itemsControl);

            //标签
            wrapPanel = WrapPanels[7];
            itemsControl = wrapPanel.Children[0] as ItemsControl;
            List<string> label = GetFilterFromItemsControl(itemsControl);

            string sql = "select * from movie where ";

            string s = "";
            vediotype.ForEach(arg => { s += $"vediotype={arg} or "; });
            if (vediotype.Count >= 1) s = s.Substring(0, s.Length - 4);
            if (s == "" | vediotype.Count==3) s = "vediotype>0";
            sql += "(" + s +  ") and "; s = "";

            year.ForEach(arg => { s += $"releasedate like '%{arg}%' or "; });
            if (year.Count >= 1) s = s.Substring(0, s.Length - 4);
            if(s!="") sql += "(" + s + ") and "; s = "";


            if (runtime.Count > 0 & rating.Count < 4)
            {
                runtime.ForEach(arg => { s += $"(runtime >={arg.Split('-')[0]} and runtime<={arg.Split('-')[1]}) or "; });
                if (runtime.Count >= 1) s = s.Substring(0, s.Length - 4);
                if (s != "") sql += "(" + s + ") and "; s = "";
            }

            if (filesize.Count > 0 & rating.Count < 4)
            {
                filesize.ForEach(arg => { s += $"(filesize >={double.Parse(arg.Split('-')[0])*1024*1024 * 1024} and filesize<={double.Parse(arg.Split('-')[1]) * 1024 * 1024 * 1024}) or "; });
                if (filesize.Count >= 1) s = s.Substring(0, s.Length - 4);
                if (s != "") sql += "(" + s + ") and "; s = "";
            }

            if (rating.Count >0 & rating.Count<5)
            {
                rating.ForEach(arg => { s += $"(rating >={arg.Split('-')[0]} and rating<={arg.Split('-')[1]}) or "; });
                if (rating.Count >= 1) s = s.Substring(0, s.Length - 4);
                if (s != "") sql += "(" + s + ") and "; s = "";
            }


            sql = sql.Substring(0, sql.Length - 5);
            var movies = DataBase.SelectMoviesBySql(sql);
            

            Console.WriteLine(sql);

            if (movies?.Count == 0) { new PopupWindow(this, "无结果").Show();  } else
            {
                List<Movie> filtermovies = movies.Where(m => IsMoviesContainValue(m.genre.Split(' '), genre))
                                                                           .Where(m => IsMoviesContainValue(m.actor.Split(new char[]{' ','/'}), actor))
                                                                           .Where(m => IsMoviesContainValue(m.label.Split(' '), label)).ToList();

                if (filtermovies.Count == 0) { new PopupWindow(this, "无结果").Show(); } else
                {
                    //更新主界面
                    main.vieModel.MovieList = new List<Movie>();
                    filtermovies.ForEach(arg => { main.vieModel.MovieList.Add(arg); });
                    //main.vieModel.Sort();
                }




            }




        }


        private bool IsMoviesContainValue(string[] m,List<string> list)
        {
            if (list.Count == 0) return true;
            bool result = false;
            if (m != null)
            {
                foreach (var item in m)
                {
                    if(!string.IsNullOrEmpty(item) | item.IndexOf(' ') < 0)
                    {
                        foreach (var value in list)
                        {
                            if (value == item) return true;
                        }
                    }
                }
            }
            return result;
        }


        private List<string> GetFilterFromItemsControl(ItemsControl itemsControl)
        {
            List<string> result = new List<string>();
            for (int i = 0; i < itemsControl.Items.Count; i++)
            {

                ContentPresenter c = (ContentPresenter)itemsControl.ItemContainerGenerator.ContainerFromItem(itemsControl.Items[i]);
                CheckBox cb = c.ContentTemplate.FindName("CheckBox", c) as CheckBox;
                if (cb!= null)
                    if ((bool)cb.IsChecked) result.Add(cb.Content.ToString());
            }
            return result;
        }
    }
}
