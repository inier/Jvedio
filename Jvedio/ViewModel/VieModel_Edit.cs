using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;

using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using System.IO;


namespace Jvedio.ViewModel
{
    class VieModel_Edit : ViewModelBase
    {

        public string id;//用于判断是否更改了 id

        public DetailMovie detailmovie;

        public DetailMovie DetailMovie
        {
            get { return detailmovie; }
            set
            {
                detailmovie = value;
                RaisePropertyChanged();
                id = DetailMovie.id;
            }
        }



        public ObservableCollection<string> movieIDList;

        public ObservableCollection<string> MovieIDList
        {
            get { return movieIDList; }
            set
            {
                movieIDList = value;
                RaisePropertyChanged();
            }
        }


        public void Refresh(string filepath)
        {
            DetailMovie models = new DetailMovie();
            models.filepath = filepath;
            FileInfo fileInfo = new FileInfo(filepath);

            //获取创建日期
            string createDate = "";
            try { createDate = fileInfo.CreationTime.ToString("yyyy-MM-dd HH:mm:ss"); }
            catch { }
            if (createDate == "") createDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            models.id = Identify.GetFanhao(fileInfo.Name);
            models.vediotype =(int) Identify.GetVedioType(models.id);
            models.scandate = createDate;
            models.filesize = fileInfo.Length;
            if (models != null) { DetailMovie = models; }
        }




        public RelayCommand<string> QueryCommand { get; set; }

        public void Query(string movieid)
        {
            
            DetailMovie models = DataBase.SelectDetailMovieById(movieid);
            
            DetailMovie = new DetailMovie();
            if (models != null) { DetailMovie = models; }
        }


        public  void Reset()
        {
            Main main = App.Current.Windows[0] as Main;
            var models = main.vieModel.CurrentMovieList.Select(arg => arg.id).ToList();
            MovieIDList = new ObservableCollection<string>();
            models?.ForEach(arg => { MovieIDList.Add(arg.ToUpper()); });
        }


        public void SaveModel()
        {
            
            if (MovieIDList == null ) id = DetailMovie.id; //是否导入单个视频
            
            if (DetailMovie != null)
            {
                
                if (DetailMovie.id.ToUpper() != id.ToUpper())
                {
                    //先删除原来的
                    DataBase.DelInfoByType("movie", "id",id);
                    DataBase.InsertFullMovie(DetailMovie);
                }
                else { DataBase.InsertFullMovie(DetailMovie); }
                
            }
           
        }










    }
}
