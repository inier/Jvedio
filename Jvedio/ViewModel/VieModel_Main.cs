using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;

using System.Collections.ObjectModel;
using System.Windows.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Threading;
using System.IO;
using System.Windows.Threading;
using System.Diagnostics;
using static Jvedio.StaticVariable;
using System.Windows.Input;

namespace Jvedio.ViewModel
{
    public  class VieModel_Main:ViewModelBase
    {
        public event EventHandler CurrentMovieListHideOrChanged;
        public event EventHandler CurrentActorListHideOrChanged;
        public event EventHandler CurrentMovieListChanged;
        public event EventHandler FlipOverCompleted;
        private DispatcherTimer DispatcherTimer;
        protected DataBase cdb;


        public VedioType CurrentVedioType = VedioType.所有;

        public VieModel_Main()
        {
            QueryCommand = new RelayCommand(Query);
            ResetCommand = new RelayCommand( Reset);
            GenreCommand = new RelayCommand(GetGenreList);
            ActorCommand = new RelayCommand(GetActorList);
            LabelCommand = new RelayCommand(GetLabelList);


            FavoritesCommand = new RelayCommand(GetFavoritesMovie);
            UncensoredCommand = new RelayCommand<int>(t => Getmoviebysql(1));
            CensoredCommand = new RelayCommand<int>(t => Getmoviebysql(2));
            EuropeCommand = new RelayCommand<int>(t => Getmoviebysql(3));

            DispatcherTimer = new DispatcherTimer();
            DispatcherTimer.Interval = TimeSpan.FromSeconds(0.5);
            DispatcherTimer.Tick += new EventHandler(DispatcherTimer_Tick);

            VedioType vedioType = VedioType.所有;
            bool result = Enum.TryParse<VedioType>(Properties.Settings.Default.VedioType, out vedioType);
            VedioType = vedioType;


            //获得所有数据库
            LoadDataBaseList();


        }


        #region "RelayCommand"
        public RelayCommand QueryCommand { get; set; }
        public RelayCommand ResetCommand { get; set; }
        public RelayCommand GenreCommand { get; set; }
        public RelayCommand ActorCommand { get; set; }
        public RelayCommand LabelCommand { get; set; }

        public RelayCommand FavoritesCommand { get; set; }
        public RelayCommand<int> CensoredCommand { get; set; }
        public RelayCommand<int> UncensoredCommand { get; set; }
        public RelayCommand<int> EuropeCommand { get; set; }
        #endregion


        #region "enum"
        private VedioType vedioType;

        public VedioType VedioType
        {
            get { return vedioType; }
            set
            {
                vedioType = value;
                RaisePropertyChanged();
                if (ClickGridType == 0) GetGenreList();
                else if (ClickGridType == 1) GetActorList();
                else GetLabelList();

            }
        }



        private MyImageType _ShowImageMode = (MyImageType)Enum.Parse(typeof(MyImageType), Properties.Settings.Default.ShowImageMode, true);

        public MyImageType ShowImageMode
        {
            get { return _ShowImageMode; }
            set
            {
                _ShowImageMode = value;
                RaisePropertyChanged();
                Properties.Settings.Default.ShowImageMode = value.ToString();
            }
        }



        private MyViewType _ShowViewMode = (MyViewType)Enum.Parse(typeof(MyViewType), Properties.Settings.Default.ShowViewMode, true);

        public MyViewType ShowViewMode
        {
            get { return _ShowViewMode; }
            set
            {
                _ShowViewMode = value;
                RaisePropertyChanged();
            }
        }


        private MySearchType _SearchType = (MySearchType)Enum.Parse(typeof(MySearchType), Properties.Settings.Default.SearchType, true);

        public MySearchType SearchType
        {
            get { return _SearchType; }
            set
            {
                _SearchType = value;
                RaisePropertyChanged();
                Console.WriteLine(SearchType.ToString());
            }
        }


        private MySearchType _AllSearchType = (MySearchType)Enum.Parse(typeof(MySearchType),Properties.Settings.Default.AllSearchType,true);

        public MySearchType AllSearchType
        {
            get { return _AllSearchType; }
            set
            {
                _AllSearchType = value;
                RaisePropertyChanged();
            }
        }

        #endregion


        #region "ObservableCollection"


        public ObservableCollection<string> _DataBases;


        public ObservableCollection<string> DataBases
        {
            get { return _DataBases; }
            set
            {
                _DataBases = value;
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<Movie> currentmovielist;


        public ObservableCollection<Movie> CurrentMovieList
        {
            get { return currentmovielist; }
            set
            {
                currentmovielist = value;
                RaisePropertyChanged();
                CurrentMovieListHideOrChanged?.Invoke(this, EventArgs.Empty); 
                
            }
        }


        public ObservableCollection<Movie> selectedMovie = new ObservableCollection<Movie>();

        public ObservableCollection<Movie> SelectedMovie
        {
            get { return selectedMovie; }
            set
            {
                selectedMovie = value;
                RaisePropertyChanged();
            }
        }


        public ObservableCollection<Movie> movielist;


        public ObservableCollection<Movie> MovieList
        {
            get { return movielist; }
            set
            {
                movielist = value;
                RaisePropertyChanged();
                CurrentPage = 1;
                FlowNum = 0;
                Properties.Settings.Default.EditMode = false;
                Properties.Settings.Default.Save();
            }
        }

        public ObservableCollection<Genre> genrelist;
        public ObservableCollection<Genre> GenreList
        {
            get { return genrelist; }
            set
            {
                genrelist = value;
                RaisePropertyChanged();
            }
        }


        public ObservableCollection<Actress> actorlist;
        public ObservableCollection<Actress> ActorList
        {
            get { return actorlist; }
            set
            {
                actorlist = value;
                RaisePropertyChanged();
            }
        }


        public ObservableCollection<Actress> _CurrentActorList;


        public ObservableCollection<Actress> CurrentActorList
        {
            get { return _CurrentActorList; }
            set
            {
                _CurrentActorList = value;
                RaisePropertyChanged();
                CurrentActorListHideOrChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public ObservableCollection<string> labellist;
        public ObservableCollection<string> LabelList
        {
            get { return labellist; }
            set
            {
                labellist = value;
                RaisePropertyChanged();
            }
        }




        public ObservableCollection<string> _AllSearchCandidate;


        public ObservableCollection<string> AllSearchCandidate
        {
            get { return _AllSearchCandidate; }
            set
            {
                _AllSearchCandidate = value;
                RaisePropertyChanged();

            }
        }



        public ObservableCollection<string> _SearchCandidate;


        public ObservableCollection<string> SearchCandidate
        {
            get { return _SearchCandidate; }
            set
            {
                _SearchCandidate = value;
                RaisePropertyChanged();

            }
        }

        #endregion



        #region "Variable"

        public bool _EnableEditActress = false;

        public bool EnableEditActress
        {
            get { return _EnableEditActress; }
            set
            {
                _EnableEditActress = value;
                RaisePropertyChanged();
            }
        }


        public string movieCount = "总计 0 个";

        public string MovieCount
        {
            get { return movieCount; }
            set
            {
                movieCount = value;
                RaisePropertyChanged();
            }
        }

        public int currentpage = 1;
        public int CurrentPage
        {
            get { return currentpage; }
            set
            {
                currentpage = value;
                FlowNum = 0;
                RaisePropertyChanged();
            }
        }

        public int totalpage = 1;
        public int TotalPage
        {
            get { return totalpage; }
            set
            {
                totalpage = value;
                RaisePropertyChanged();

            }
        }


        public int currentactorpage = 1;
        public int CurrentActorPage
        {
            get { return currentactorpage; }
            set
            {
                currentactorpage = value;
                FlowNum = 0;
                RaisePropertyChanged();
            }
        }


        public int totalactorpage = 1;
        public int TotalActorPage
        {
            get { return totalactorpage; }
            set
            {
                totalactorpage = value;
                RaisePropertyChanged();
            }
        }






        public int _FlowNum = 0;
        public int FlowNum
        {
            get { return _FlowNum; }
            set
            {
                _FlowNum = value;
                RaisePropertyChanged();
            }
        }








        public string textType = "所有视频";

        public string TextType
        {
            get { return textType; }
            set
            {
                textType = value;
                RaisePropertyChanged();
            }
        }

        public int ClickGridType { get; set; }

        private string search = string.Empty;

        public string Search
        {
            get { return search; }
            set
            {
                search = value;
                RaisePropertyChanged();
                if (search == "")
                    Reset();
                else
                    Query();
            }
        }


        public Actress actress;
        public Actress Actress
        {
            get { return actress; }
            set
            {
                actress = value;
                RaisePropertyChanged();
            }
        }

        private bool showSideBar = false;

        public bool ShowSideBar
        {
            get { return showSideBar; }
            set
            {
                showSideBar = value;
                RaisePropertyChanged();
            }
        }



        private bool Checkingurl = false;

        public bool CheckingUrl
        {
            get { return Checkingurl; }
            set
            {
                Checkingurl = value;
                RaisePropertyChanged();
            }
        }

        private bool searchAll = true;

        public bool SearchAll
        {
            get { return searchAll; }
            set
            {
                searchAll = value;
            }
        }

        #endregion




        #region "Method"

        public void LoadDataBaseList()
        {
            DataBases = new ObservableCollection<string>();
            try
            {
                var fiels = Directory.GetFiles("DataBase", "*.sqlite", SearchOption.TopDirectoryOnly).ToList();
                fiels.ForEach(arg => DataBases.Add(arg.Split('\\').Last().Split('.').First().ToLower()));
            }
            catch { }
            
            if (!DataBases.Contains("info")) DataBases.Add("info");
        }


        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            Main main = App.Current.Windows[0] as Main;
            main.Loadslide();
            DispatcherTimer.Stop();
        }

        public void GetSearchCandidate()
        {
            SearchCandidate = new ObservableCollection<string>();
            List<Movie> model = new List<Movie>();
            if (SearchType == MySearchType.名称)
            {
                model = MovieList.Where(m => m.title.ToUpper().Contains(Search.ToUpper())).ToList();
                model.ForEach(arg => { SearchCandidate.Add(arg.title); });
            }
            else if (SearchType == MySearchType.演员)
            {
                model = MovieList.Where(m => m.actor.ToUpper().Contains(Search.ToUpper())).ToList();
                List<string> actors = new List<string>();
                model.ForEach(arg => {
                    string[] actor = arg.actor.Split(new char[]{' ','/'});
                    foreach (var item in actor)
                    {
                        if (!string.IsNullOrEmpty(item) & item.IndexOf(' ') < 0)
                        {
                            if (!actors.Contains(item) & item.ToUpper().IndexOf(Search.ToUpper()) >= 0) actors.Add(item);
                        }
                    }
                });
                actors.ForEach(arg => { SearchCandidate.Add(arg); });
            }
            else if (SearchType == MySearchType.识别码)
            {
                model = MovieList.Where(m => m.id.ToUpper().Contains(Search.ToUpper())).ToList();
                model.ForEach(arg => { SearchCandidate.Add(arg.id); });
            }





        }




       

        public void Flow()
        {
            if (MovieList != null)
            {
                CurrentMovieListHideOrChanged?.Invoke(this, EventArgs.Empty); //停止下载
                int DisPlayNum = Properties.Settings.Default.DisplayNumber;
                int SetFlowNum = Properties.Settings.Default.FlowNum;
                for (int i = (CurrentPage - 1) * DisPlayNum + FlowNum * SetFlowNum; i < (CurrentPage - 1) * DisPlayNum + (FlowNum + 1) * SetFlowNum; i++)
                {
                    if (CurrentMovieList.Count < DisPlayNum)
                    {
                        if (i <= MovieList.Count - 1)
                        {
                            Movie movie = MovieList[i];

                            //加载图片
                            if (Properties.Settings.Default.ShowImageMode == "海报图")
                            {
                                movie.bigimage = StaticClass.GetBitmapImage(movie.id, "BigPic");
                            }
                            else if (Properties.Settings.Default.ShowImageMode == "缩略图")
                            {
                                movie.smallimage = StaticClass.GetBitmapImage(movie.id, "SmallPic");
                            }
                            CurrentMovieList.Add(movie);
                        }
                        else { break; }
                    }
                    else
                    {
                        //Main main = App.Current.Windows[0] as Main;
                        //main.FLowNum = 0;
                        FlowNum = 0;
                    }

                }
            }
            MovieCount = $"本页有 {CurrentMovieList.Count} 个，总计{MovieList.Count} 个";
            CurrentMovieListChanged?.Invoke(this, EventArgs.Empty);

        }


        public async  void Refresh()
        {
            App.Current.Windows[0].Cursor = Cursors.Wait;
            List<string> CurrentMovieIDList = new List<string>();
            foreach (Movie movie in CurrentMovieList)
                CurrentMovieIDList.Add(movie.id);
            DataBase dataBase = new DataBase();
            //CurrentMovieList = new ObservableCollection<Movie>();

            foreach(var item in CurrentMovieIDList)
            {
                Movie movie = await dataBase.SelectMovieByID(item);

                //加载图片
                if (Properties.Settings.Default.ShowImageMode == "海报图")
                    movie.bigimage = StaticClass.GetBitmapImage(movie.id, "BigPic");
                else if (Properties.Settings.Default.ShowImageMode == "缩略图")
                    movie.smallimage = StaticClass.GetBitmapImage(movie.id, "SmallPic");

                //显示翻译结果
                if (Properties.Settings.Default.TitleShowTranslate)
                {
                    DataBase cdb = new DataBase("Translate");
                    string translate_title = cdb.GetInfoBySql($"select translate_title from youdao where id='{movie.id}'");
                    cdb.CloseDB();
                    if (translate_title != "") movie.title = translate_title;
                }


                //更新所有
                for (int i = 0; i < MovieList.Count; i++)
                {
                    if (MovieList[i].id == item)
                    {
                        MovieList[i] = null;
                        MovieList[i] = movie;
                        break;
                    }
                }

                //更新当前
                for (int i = 0; i < CurrentMovieList.Count; i++)
                {
                    if (CurrentMovieList[i].id == item)
                    {
                        CurrentMovieList[i] = null;
                        CurrentMovieList[i] = movie;
                        break;
                    }
                }
            }


            dataBase.CloseDB();

            App.Current.Windows[0].Cursor = Cursors.Arrow;
        }

        public  void RefreshActor()
        {
            int Page = CurrentActorPage;
            cdb = new DataBase();
            List<Actress> models = null;
            models = cdb.SelectActorByVedioType(VedioType);
            cdb.CloseDB();
            ActorList = new ObservableCollection<Actress>();
            models?.ForEach(arg => { ActorList.Add(arg); });
            TotalActorPage = (int)Math.Ceiling((double)ActorList.Count / (double)Properties.Settings.Default.ActorDisplayNum);
            CurrentActorPage = Page;
            ActorFlipOver();
        }


        public void ActorFlipOver()
        {
            if (ActorList != null)
            {
                //只在翻页时加载图片、显示翻译结果
                int ActorDisplayNum = Properties.Settings.Default.ActorDisplayNum;
                CurrentActorList = new ObservableCollection<Actress>();
                for (int i = (CurrentActorPage - 1) * ActorDisplayNum; i < CurrentActorPage* ActorDisplayNum; i++)
                {
                    if (i < ActorList.Count )
                    {
                        Actress actress = ActorList[i];
                        actress.smallimage = StaticClass.GetBitmapImage(actress.name, "Actresses");
                        App.Current.Dispatcher.BeginInvoke((Action)delegate { CurrentActorList.Add(actress); });
                    }
                    else { break; }
                    if (CurrentActorList.Count == ActorDisplayNum) { break; }
                }
            }


        }


        //翻页
        public void FlipOver()
        {
            if (MovieList != null)
            {
                //只在翻页时加载图片、显示翻译结果
                int DisPlayNum = Properties.Settings.Default.DisplayNumber;
                int FlowNum = Properties.Settings.Default.FlowNum;
                CurrentMovieList = new ObservableCollection<Movie>();
                for (int i = (CurrentPage - 1) * DisPlayNum; i < (CurrentPage - 1) * DisPlayNum + FlowNum; i++)
                {
                    if (i <= MovieList.Count - 1)
                    {
                        Movie movie = MovieList[i];

                        //加载图片
                        if (Properties.Settings.Default.ShowImageMode == "海报图")
                            movie.bigimage = StaticClass.GetBitmapImage(movie.id, "BigPic");
                        else if (Properties.Settings.Default.ShowImageMode == "缩略图")
                            movie.smallimage = StaticClass.GetBitmapImage(movie.id, "SmallPic");

                        //显示翻译结果
                        if (Properties.Settings.Default.TitleShowTranslate)
                        {
                            DataBase dataBase = new DataBase("Translate");
                            string translate_title = dataBase.GetInfoBySql($"select translate_title from youdao where id='{movie.id}'");
                            dataBase.CloseDB();
                            if (translate_title != "") movie.title = translate_title;
                        }
                        App.Current.Dispatcher.BeginInvoke((Action)delegate { CurrentMovieList.Add(movie); MovieCount = $"本页有 {CurrentMovieList.Count} 个，总计{MovieList.Count} 个"; }); 

                    }
                    else { break; }
                    if (CurrentMovieList.Count == FlowNum) { break; }
                }
            }
            
            //0.5s后开始展示预览图
            if (Properties.Settings.Default.ShowImageMode == "预览图") DispatcherTimer.Start();
            FlipOverCompleted?.Invoke(this, EventArgs.Empty);
        }




        //保存信息
        public void SaveModel(string id ,string content,object value, string savetype = "Int")
        {
            cdb = new DataBase();
            cdb.UpdateMovieByID( id,  content,  value, savetype);
            cdb.CloseDB();
        }



        //获得标签
        public async void GetLabelList( )
        {
            TextType = "标签";
            cdb = new DataBase();
            List<string> models = null;
            await Task.Run(() => { 
                models = cdb.SelectLabelByVedioType(VedioType);
            });
            await Task.Run(() => {
                cdb.CloseDB();
                LabelList = new ObservableCollection<string>();
                if (models != null) models.ForEach(arg => { LabelList.Add(arg); });
            });
        } 


        //获得演员，信息照片都获取
        public async void GetActorList()
        {
            CurrentActorPage = 1;
            TextType = "演员";
            cdb = new DataBase();
            List<Actress> models = null;
            await Task.Run(() => { 
                 models = cdb.SelectActorByVedioType(VedioType);
            });

            await Task.Run(() => {
                cdb.CloseDB();
                if (ActorList != null && models != null && models.Count == ActorList.ToList().Count) { return; }
                
                ActorList = new ObservableCollection<Actress>();

                models?.ForEach(arg => { App.Current.Dispatcher.Invoke((Action)delegate { ActorList.Add(arg); }); });

                TotalActorPage = (int)Math.Ceiling((double)ActorList.Count / (double)Properties.Settings.Default.ActorDisplayNum);
                ActorFlipOver();
            });
        }


        //获得类别
        public async void GetGenreList()
        {
            TextType = "类别";
            cdb = new DataBase();
            List<Genre> models = null;
            models = await cdb.SelectGenreByVedioType(VedioType); 
            cdb.CloseDB();
            GenreList = new ObservableCollection<Genre>();
            models?.ForEach(arg => { GenreList.Add(arg); });

        }


        public async void GetFavoritesMovie()
        {
            TextType = "我的喜爱";
            cdb = new DataBase();
            List<Movie> models = null;
            await Task.Run(() => { models = cdb.SelectMoviesBySql("SELECT * from movie where favorites>0"); });

            await Task.Run(() =>
            {
                cdb.CloseDB();
                MovieList = new ObservableCollection<Movie>();
                models?.ForEach(arg => { MovieList.Add(arg); });
                Sort();
            });
        }

        public void GetMoviebyStudio(string moviestudio)
        {
            cdb = new DataBase();
            var models = cdb.SelectMoviesBySql($"SELECT * from movie where studio = '{moviestudio}'");
            cdb.CloseDB();
            MovieList = new ObservableCollection<Movie>();
            models?.ForEach(arg => { MovieList.Add(arg); });
            Sort();
        }

        public void GetMoviebyTag(string movietag)
        {
            cdb = new DataBase();
            var models = cdb.SelectMoviesBySql($"SELECT * from movie where tag = '{movietag}'");
            cdb.CloseDB();
            MovieList = new ObservableCollection<Movie>();
            models?.ForEach(arg => { MovieList.Add(arg); });
            Sort();
        }

        public void GetMoviebyDirector(string moviedirector)
        {
            cdb = new DataBase();
            var models = cdb.SelectMoviesBySql($"SELECT * from movie where director ='{moviedirector}'");
            cdb.CloseDB();
            MovieList = new ObservableCollection<Movie>();
            models?.ForEach(arg => { MovieList.Add(arg); });
            Sort();
        }


        public void GetMoviebyLabel(string movielabel)
        {
            cdb = new DataBase();
            var models = cdb.SelectMoviesBySql("SELECT * from movie where label like '%" + movielabel + "%'");
            cdb.CloseDB();
            MovieList = new ObservableCollection<Movie>();
            models?.ForEach(arg =>
            {
                if (arg.label.Split(' ').Any(m => m.ToUpper() == movielabel.ToUpper())) MovieList.Add(arg);
            });
            Sort();
        }



        public void GetMoviebyActress(Actress actress)
        {
            int vediotype = (int)VedioType;
            //根据视频类型选择演员
            cdb = new DataBase();
            List<Movie> models;
            if (actress.id == "")
                models = cdb.SelectMoviesBySql($"SELECT * from movie where actor like '%{actress.name}%'"); 
            else
               models = cdb.SelectMoviesBySql($"SELECT * from movie where actorid like '%{actress.id}%'");

            cdb.CloseDB();
            MovieList = new ObservableCollection<Movie>();
            models?.ForEach(arg =>
            {
                if (arg.actor.Split(new char[] { ' ', '/' }).Any(m => m.ToUpper() == actress.name.ToUpper())) MovieList.Add(arg);
            });
            Sort();
            
        }


        //根据视频类型选择演员
        public void GetMoviebyActressAndVetioType(Actress actress)
        {
            cdb = new DataBase();
            List<Movie> models;
            if (actress.id == "")
            {
                if (VedioType == 0) { models = cdb.SelectMoviesBySql($"SELECT * from movie where actor like '%{actress.name}%'"); }
                else { models = cdb.SelectMoviesBySql($"SELECT * from movie where actor like '%{actress.name}%' and vediotype={(int)VedioType}"); }
            }
            else
            {
                if (VedioType == 0) { models = cdb.SelectMoviesBySql($"SELECT * from movie where actorid like '%{actress.id}%'"); }
                else { models = cdb.SelectMoviesBySql($"SELECT * from movie where actorid like '%{actress.id}%' and vediotype={(int)VedioType}"); }
            }
            cdb.CloseDB();

            MovieList = new ObservableCollection<Movie>();
            models?.ForEach(arg =>
            {
                if (arg.actor.Split(new char[] { ' ', '/' }).Any(m => m.ToUpper() == actress.name.ToUpper())) MovieList.Add(arg);
            });
            Sort();
           
        }


        public void GetMoviebyGenre(string moviegenre)
        {
            cdb = new DataBase();
            var models = cdb.SelectMoviesBySql("SELECT * from movie where genre like '%" + moviegenre + "%'");
            cdb.CloseDB();
            MovieList = new ObservableCollection<Movie>();
            models?.ForEach(arg =>
            {
                if(arg.genre.Split(' ').Any(m => m.ToUpper() == moviegenre.ToUpper())) MovieList.Add(arg);
            });
            Sort();
        }


        public async void Getmoviebysql(int t)
        {
            if(t==0) TextType = "所有";
            else if(t==1) TextType = "步兵";
            else if (t == 2) TextType = "骑兵";
            else if (t == 3) TextType = "欧美";
            else TextType = "所有";

            CurrentVedioType = (VedioType)VedioType.Parse(typeof(VedioType), textType);

            cdb = new DataBase();
            List<Movie> models = null;

            await Task.Run(() => { 
                models = cdb.SelectMoviesBySql("SELECT * from movie where vediotype=" + t); 
            });

            await Task.Run(() => {
                cdb.CloseDB();
                MovieList = new ObservableCollection<Movie>();
                models?.ForEach(arg => { MovieList.Add(arg); });
                Sort();
            });
        }



        /// <summary>
        /// 在数据库中搜索影片
        /// </summary>
        public async void Query()
        {
            if (Search == "") return;
            //对输入的内容进行格式化
            string FormatSearch = Search.Replace(" ", "").Replace("%", "").Replace("'", "");
            FormatSearch = FormatSearch.ToUpper();

            if (string.IsNullOrEmpty(FormatSearch)) return;

            string fanhao = "";
            if (CurrentVedioType == VedioType.欧美)
                fanhao = Identify.GetEuFanhao(FormatSearch);
            else
                fanhao = Identify.GetFanhao(FormatSearch);

            string searchContent = "";
            if (fanhao == "") searchContent = FormatSearch;
            else searchContent = fanhao;
            


            TextType = searchContent;//当前显示内容
            cdb = new DataBase();
            List<Movie> models = null ;

            try {
            if(Properties.Settings.Default.AllSearchType== "识别码")
                models = await cdb.SelectMoviesById(searchContent);
            else if (Properties.Settings.Default.AllSearchType == "名称")
                models = cdb.SelectMoviesBySql($"SELECT * FROM movie where title like '%{searchContent}%'");
            else if (Properties.Settings.Default.AllSearchType == "演员")
                models = cdb.SelectMoviesBySql($"SELECT * FROM movie where actor like '%{searchContent}%'");
            }
            finally { cdb.CloseDB(); }


            MovieList = new ObservableCollection<Movie>();
            models?.ForEach(arg => { MovieList.Add(arg); });
            Sort();
            }


        public  void RandomDisplay()
        {
            TextType = "随机展示";
            cdb = new DataBase();
            var models =  cdb.SelectMoviesBySql($"SELECT * FROM movie ORDER BY RANDOM() limit {Properties.Settings.Default.DisplayNumber}");
            cdb.CloseDB();
            MovieList = new ObservableCollection<Movie>();
            models?.ForEach(arg => { MovieList.Add(arg); });
            Sort();
        }


        public async  void Reset()
        {
                TextType = "所有视频";
                List<Movie> models = null;
               cdb = new DataBase(); 
                models =  await cdb.SelectMoviesById("");
                cdb.CloseDB();
                if(models.Count==0) MovieCount = $"本页有 0 个，总计 0 个";
                if (models != null && MovieList != null && models.Count == MovieList.ToList().Count  ) { return; }
                    MovieList = new ObservableCollection<Movie>();
                    models?.ForEach(arg => { MovieList.Add(arg); });
                    Sort();
                cdb.CloseDB();
        }

        public void Sort()
        {
            if (MovieList != null)
            {
                List<Movie> sortMovieList = new List<Movie>();
                bool SortDescending = Properties.Settings.Default.SortDescending;
                switch (Properties.Settings.Default.SortType)
                {
                    case "识别码":
                        if (SortDescending) { sortMovieList = MovieList.OrderByDescending(o => o.id).ToList(); } else { sortMovieList = MovieList.OrderBy(o => o.id).ToList(); }
                        break;
                    case "文件大小":
                        if (SortDescending) { sortMovieList = MovieList.OrderByDescending(o => o.filesize).ToList(); } else { sortMovieList = MovieList.OrderBy(o => o.filesize).ToList(); }
                        break;
                    case "创建时间":
                        if (SortDescending) { sortMovieList = MovieList.OrderByDescending(o => o.scandate).ToList(); } else { sortMovieList = MovieList.OrderBy(o => o.scandate).ToList(); }
                        break;
                    case "喜爱程度":
                        if (SortDescending) { sortMovieList = MovieList.OrderByDescending(o => o.favorites).ToList(); } else { sortMovieList = MovieList.OrderBy(o => o.favorites).ToList(); }
                        break;
                    case "名称":
                        if (SortDescending) { sortMovieList = MovieList.OrderByDescending(o => o.title).ToList(); } else { sortMovieList = MovieList.OrderBy(o => o.title).ToList(); }
                        break;
                    case "访问次数":
                        if (SortDescending) { sortMovieList = MovieList.OrderByDescending(o => o.visits).ToList(); } else { sortMovieList = MovieList.OrderBy(o => o.visits).ToList(); }
                        break;
                    case "发行日期":
                        if (SortDescending) { sortMovieList = MovieList.OrderByDescending(o => o.releasedate).ToList(); } else { sortMovieList = MovieList.OrderBy(o => o.releasedate).ToList(); }
                        break;
                    case "评分":
                        if (SortDescending) { sortMovieList = MovieList.OrderByDescending(o => o.rating).ToList(); } else { sortMovieList = MovieList.OrderBy(o => o.rating).ToList(); }
                        break;
                    case "时长":
                        if (SortDescending) { sortMovieList = MovieList.OrderByDescending(o => o.runtime).ToList(); } else { sortMovieList = MovieList.OrderBy(o => o.runtime).ToList(); }
                        break;
                    case "演员":
                        if (SortDescending) { sortMovieList = MovieList.OrderByDescending(o => o.actor.Split(new char[] { ' ', '/' })[0]).ToList(); } else { sortMovieList = MovieList.OrderBy(o => o.actor.Split(new char[] { ' ', '/' })[0]).ToList(); }
                        break;
                    default:
                        if (SortDescending) { sortMovieList = MovieList.OrderByDescending(o => o.id).ToList(); } else { sortMovieList = MovieList.OrderBy(o => o.id).ToList(); }
                        break;
                }
                MovieList = new ObservableCollection<Movie>();
                sortMovieList.ForEach(arg =>
                {
                    MovieList.Add(arg);
                    if (Properties.Settings.Default.OnlyShowPlay) { if (!File.Exists(arg.filepath)) { MovieList.Remove(arg); } }//仅显示可播放
                    //if (!Properties.Settings.Default.DisPlayHDV) { if (Identify.IsHDV(arg.filepath)) { MovieList.Remove(arg); } }//去掉高清
                    //if (!Properties.Settings.Default.DisPlayCHS) { if (Identify.IsCHS(arg.filepath)) { MovieList.Remove(arg); } }//去掉中字
                    //if (!Properties.Settings.Default.DisPlayOutFlow) { if (Identify.IsFlowOut(arg.filepath)) { MovieList.Remove(arg); } }//去掉流出
                    //if (!Properties.Settings.Default.DisPlayNoStamp) { if (!Identify.IsFlowOut(arg.filepath) & !Identify.IsHDV(arg.filepath) & !Identify.IsCHS(arg.filepath) ) { MovieList.Remove(arg); } }//去掉无标签戳

                    if (Properties.Settings.Default.ShowViewMode == "有图")
                    {
                        if (Properties.Settings.Default.ShowImageMode == "缩略图")
                            if (!File.Exists(StaticVariable.BasePicPath + $"SmallPic\\{arg.id}.jpg")) { MovieList.Remove(arg); }
                        else if (Properties.Settings.Default.ShowImageMode == "海报图")
                            if (!File.Exists(StaticVariable.BasePicPath + $"BigPic\\{arg.id}.jpg")) { MovieList.Remove(arg); }
                        else if (Properties.Settings.Default.ShowImageMode == "预览图")
                        {
                            if (!Directory.Exists(StaticVariable.BasePicPath + $"ExtraPic\\{arg.id}\\")) { MovieList.Remove(arg); }
                            else
                            {
                                try { if (Directory.GetFiles(StaticVariable.BasePicPath + $"ExtraPic\\{arg.id}\\", "*.*", SearchOption.TopDirectoryOnly).Count() == 0) MovieList.Remove(arg); }
                                catch { }
                            }
                        }


                    }
                    else if (Properties.Settings.Default.ShowViewMode == "无图")
                    {
                        if (Properties.Settings.Default.ShowImageMode == "缩略图")
                            if (File.Exists(StaticVariable.BasePicPath + $"SmallPic\\{arg.id}.jpg")) { MovieList.Remove(arg); }
                        else if (Properties.Settings.Default.ShowImageMode == "海报图")
                            if (File.Exists(StaticVariable.BasePicPath + $"BigPic\\{arg.id}.jpg")) { MovieList.Remove(arg); }
                        else if (Properties.Settings.Default.ShowImageMode == "预览图")
                        {
                            if (Directory.Exists(StaticVariable.BasePicPath + $"ExtraPic\\{arg.id}\\"))
                            {
                                try { if (Directory.GetFiles(StaticVariable.BasePicPath + $"ExtraPic\\{arg.id}\\", "*.*", SearchOption.TopDirectoryOnly).Count() > 0) MovieList.Remove(arg); }
                                catch { }
                            }
                        }
                    }

                });
                TotalPage = (int)Math.Ceiling((double)MovieList.Count / (double)Properties.Settings.Default.DisplayNumber);
                FlipOver();
            }
        }


        #endregion

    }

}
