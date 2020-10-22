
using GalaSoft.MvvmLight;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jvedio.ViewModel
{
    class VieModel_ImageViewer : ViewModelBase
    {



        private DetailMovie detailMovie;

        public VieModel_ImageViewer(string movieid)
        {
            DetailMovie models = DataBase.SelectDetailMovieById(movieid);

            //扫描目录
            List<string> imagePathList = new List<string>();
            try
            {
                foreach (var path in Directory.GetFiles(StaticVariable.BasePicPath + $"ExtraPic\\{models.id}\\")) imagePathList.Add(path);
            }
            catch (Exception e) { Logger.LogE(e); }
            if (imagePathList.Count > 0) imagePathList = imagePathList.CustomSort().ToList();
            DetailMovie = new DetailMovie();
            if (models != null)
            {
                foreach (var path in imagePathList) models.extraimagelist.Add(StaticClass.GetExtraImage(path));//加载预览图
                DetailMovie = models;
            }
        }

        public DetailMovie DetailMovie { get => detailMovie; set => detailMovie = value; }
    }
}
