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

        public event EventHandler QueryCompletedHandler;
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
            DetailMovie detailMovie = cdb.SelectDetailMovieById(movieid);
            //访问次数+1
            detailMovie.visits += 1;
            cdb.UpdateMovieByID(movieid, "visits", detailMovie.visits);
            cdb.CloseDB();

            //扫描目录
            List<string> imagePathList = new List<string>();
            if(Directory.Exists(StaticVariable.BasePicPath + $"ExtraPic\\{detailMovie.id}\\"))
            {
                try
                {
                    foreach (var path in Directory.GetFiles(StaticVariable.BasePicPath + $"ExtraPic\\{detailMovie.id}\\")) imagePathList.Add(path);
                }
                catch { }
                if (imagePathList.Count > 0) imagePathList = imagePathList.CustomSort().ToList();
            }
            //释放图片内存
            if (DetailMovie != null)
            {
                DetailMovie.smallimage = null;
                DetailMovie.bigimage = null;
                for (int i = 0; i < DetailMovie.extraimagelist.Count; i++)
                {
                    DetailMovie.extraimagelist[i] = null;
                }

                for (int i = 0; i < DetailMovie.actorlist.Count; i++)
                {
                    DetailMovie.actorlist[i].bigimage = null;
                    DetailMovie.actorlist[i].smallimage = null;
                }
            }
            GC.Collect();
            DetailMovie = new DetailMovie();
            if (detailMovie != null)
            {
                foreach (var path in imagePathList) { 
                    detailMovie.extraimagelist.Add(StaticClass.GetExtraImage(path));
                    detailMovie.extraimagePath.Add(path);
                }//加载预览图
                detailMovie.bigimage = StaticClass.GetBitmapImage(detailMovie.id, "BigPic");

                DataBase dataBase = new DataBase("Translate");
                //加载翻译结果
                if (Properties.Settings.Default.TitleShowTranslate)
                {
                    string translate_title = dataBase.GetInfoBySql($"select translate_title from youdao where id='{detailMovie.id}'");
                    if (translate_title != "") detailMovie.title = translate_title;
                }

                if (Properties.Settings.Default.PlotShowTranslate)
                {
                    string translate_plot = dataBase.GetInfoBySql($"select translate_plot from youdao where id='{detailMovie.id}'");
                    if (translate_plot != "") detailMovie.plot = translate_plot;
                }
                dataBase.CloseDB();



                DetailMovie = detailMovie;
                //QueryCompletedHandler?.Invoke(null, EventArgs.Empty);
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
