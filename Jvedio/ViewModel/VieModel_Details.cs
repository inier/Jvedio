using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;

using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.IO;
using System.Text.RegularExpressions;


namespace Jvedio.ViewModel
{
    public class VieModel_Details : ViewModelBase
    {


        public VieModel_Details()
        {
            QueryCommand = new RelayCommand<string>(Query);
        }


        public DetailMovie detailmovie;

        public DetailMovie DetailMovie
        {
            get { return detailmovie; }
            set
            {
                detailmovie = value;
                RaisePropertyChanged();
            }
        }

        public void CleanUp()
        {
            MessengerInstance.Unregister(this);
        }

        DataBase cdb;

        public RelayCommand<string> QueryCommand { get; set; }


        public void SaveLove()
        {
            cdb = new DataBase();
            cdb.UpdateMovieByID(DetailMovie.id, "favorites",DetailMovie.favorites, "string");
            cdb.CloseDB();
        }


        public void Query(string movieid)
        {

            cdb = new DataBase();
            DetailMovie models = cdb.SelectDetailMovieById(movieid);
            //访问次数+1
            models.visits += 1;
            cdb.UpdateMovieByID(movieid, "visits", models.visits);
            cdb.CloseDB();

            //扫描目录
            List<string> imagePathList = new List<string>();
            if(Directory.Exists(StaticVariable.BasePicPath + $"ExtraPic\\{models.id}\\"))
            {
                try
                {
                    foreach (var path in Directory.GetFiles(StaticVariable.BasePicPath + $"ExtraPic\\{models.id}\\")) imagePathList.Add(path);
                }
                catch { }
                if (imagePathList.Count > 0) imagePathList = imagePathList.CustomSort().ToList();
            }

            DetailMovie = new DetailMovie();
            if (models != null)
            {
                foreach (var path in imagePathList) models.extraimagelist.Add(StaticClass.GetExtraImage(path));//加载预览图
                models.bigimage = StaticClass.GetBitmapImage(models.id, "BigPic");

                DataBase dataBase = new DataBase("Translate");
                //加载翻译结果
                if (Properties.Settings.Default.TitleShowTranslate)
                {
                    string translate_title = dataBase.GetInfoBySql($"select translate_title from youdao where id='{models.id}'");
                    if (translate_title != "") models.title = translate_title;
                }

                if (Properties.Settings.Default.PlotShowTranslate)
                {
                    string translate_plot = dataBase.GetInfoBySql($"select translate_plot from youdao where id='{models.id}'");
                    if (translate_plot != "") models.plot = translate_plot;
                }
                dataBase.CloseDB();



                DetailMovie = models;
            }
        }


    }

    public static class MyExtensions
    {
        public static IEnumerable<string> CustomSort(this IEnumerable<string> list)
        {
            int maxLen = list.Select(s => s.Length).Max();

            return list.Select(s => new
            {
                OrgStr = s,
                SortStr = Regex.Replace(s, @"(\d+)|(\D+)", m => m.Value.PadLeft(maxLen, char.IsDigit(m.Value[0]) ? ' ' : '\xffff'))
            })
            .OrderBy(x => x.SortStr)
            .Select(x => x.OrgStr);
        }

    }
}
