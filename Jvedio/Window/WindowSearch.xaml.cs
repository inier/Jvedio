using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Jvedio
{
    /// <summary>
    /// WindowSearch.xaml 的交互逻辑
    /// </summary>
    public partial class WindowSearch : Jvedio_BaseWindow
    {
        public WindowSearch()
        {
            InitializeComponent();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.Search_Content = SearchTextBox.Text;
            Properties.Settings.Default.Save();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //计数
            string sqlText;
            string searchType = Properties.Settings.Default.Search_Type;
            string searchContent = Properties.Settings.Default.Search_Content.ToUpper();

            if (searchType == "识别码") searchType = "id";
            else if (searchType == "名称") searchType = "title";
            else if (searchType == "演员") searchType = "actor";


            if (Properties.Settings.Default.Search_Pattern == "SQL语句")
            {
                sqlText = searchContent;
            }
            else
            {
                if (Properties.Settings.Default.Search_MatchAllWord)
                    sqlText = $"select * from movie where {searchType} ='{searchContent}'";
                else
                    sqlText = $"select * from movie where {searchType} like '%{searchContent}%'";
            }




            


            Console.WriteLine(sqlText);
            DataBase cdb = new DataBase();
            List<Movie> movies = null;

            try { movies = cdb.SelectMoviesBySql(sqlText); }
            catch { }
             
            cdb.CloseDB();
            if (movies != null) { SearchResult.Text = $"计数：{movies.Count} 次匹配"; SearchResult.Foreground = Brushes.LightBlue; }
            else { SearchResult.Text = $"查找：无法找到文本 {searchContent}";SearchResult.Foreground = Brushes.OrangeRed; }
        }

        private void Search(object sender, RoutedEventArgs e)
        {
            string sqlText;
            string searchType = Properties.Settings.Default.Search_Type;
            string searchContent = Properties.Settings.Default.Search_Content.ToUpper();

            if (searchType == "识别码") searchType = "id";
            else if (searchType == "名称") searchType = "title";
            else if (searchType == "演员") searchType = "actor";


            if (Properties.Settings.Default.Search_Pattern == "SQL语句")
                sqlText = searchContent;
            else
            {
                if (Properties.Settings.Default.Search_MatchAllWord)
                    sqlText = $"select * from movie where {searchType} ='{searchContent}'";
                else
                    sqlText = $"select * from movie where {searchType} like '%{searchContent}%'";
            }

            Console.WriteLine(sqlText);
            DataBase cdb = new DataBase();
            List<Movie> movies = null;

            try { movies = cdb.SelectMoviesBySql(sqlText); }
            catch(Exception ex) { Logger.LogD(ex); }

            cdb.CloseDB();
            if (movies != null) { 
                SearchResult.Text = $"计数：{movies.Count} 次匹配"; SearchResult.Foreground = Brushes.LightBlue;

                Main main = App.Current.Windows[0] as Main;
                main.vieModel.MovieList = new System.Collections.ObjectModel.ObservableCollection<Movie>();
                main.vieModel.CurrentMovieList = new System.Collections.ObjectModel.ObservableCollection<Movie>();
                movies.ForEach(arg => main.vieModel.MovieList.Add(arg));
                main.vieModel.Sort();
            }
            else { 
                SearchResult.Text = $"查找无结果： {searchContent}"; SearchResult.Foreground = Brushes.OrangeRed; 
            }
        }

        private void RadioButton_Click(object sender, RoutedEventArgs e)
        {
            RadioButton radioButton = sender as RadioButton;
            if (radioButton.Content.ToString() == "普通")
            {
                AllWordCheckBox.IsEnabled = true;
                
            }
            else
            {
                AllWordCheckBox.IsEnabled = false;
                AllWordCheckBox.IsChecked = false;
            }

            Properties.Settings.Default.Search_Pattern = radioButton.Content.ToString();
            Properties.Settings.Default.Save();
        }

        private void RadioButton_Click_1(object sender, RoutedEventArgs e)
        {
            RadioButton radioButton = sender as RadioButton;
            Properties.Settings.Default.Search_Type = radioButton.Content.ToString();
            Properties.Settings.Default.Save();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            WindowAdvanceSearch windowAdvanceSearch;
            Window window = Jvedio.GetWindow.Get("WindowAdvanceSearch");
            if (window != null) { windowAdvanceSearch = (WindowAdvanceSearch)window; windowAdvanceSearch.Close(); }
            windowAdvanceSearch = new WindowAdvanceSearch();
            windowAdvanceSearch.Show();
        }

        private void SearchTextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Search(sender, new RoutedEventArgs());
            else if (e.Key == Key.Escape)
                this.Close();
            else if (e.Key == Key.Delete)
                SearchTextBox.Text = "";
        }
    }



}
