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
using System.Drawing;
using DynamicData;
using DynamicData.Binding;
using System.Xml;

namespace Jvedio.ViewModel
{
    public class VieModel_Main : ViewModelBase
    {
        public event EventHandler CurrentMovieListHideOrChanged;
        public event EventHandler CurrentActorListHideOrChanged;
        public event EventHandler CurrentMovieListChangedCompleted;
        public event EventHandler FlipOverCompleted;

        public bool IsFlipOvering = false;


        public VedioType CurrentVedioType = VedioType.所有;

        private DispatcherTimer SearchTimer = new DispatcherTimer();

        public VieModel_Main()
        {
            ResetCommand = new RelayCommand(Reset);
            GenreCommand = new RelayCommand(GetGenreList);
            ActorCommand = new RelayCommand(GetActorList);
            LabelCommand = new RelayCommand(GetLabelList);


            FavoritesCommand = new RelayCommand(GetFavoritesMovie);
            RecentWatchCommand = new RelayCommand(GetRecentWatch);
            RecentCommand = new RelayCommand(GetRecentMovie);
            UncensoredCommand = new RelayCommand<VedioType>(t => GetMoviebyVedioType(VedioType.步兵));
            CensoredCommand = new RelayCommand<VedioType>(t => GetMoviebyVedioType(VedioType.骑兵));
            EuropeCommand = new RelayCommand<VedioType>(t => GetMoviebyVedioType(VedioType.欧美));



            SearchTimer = new DispatcherTimer();
            SearchTimer.Interval = TimeSpan.FromSeconds(0.3);
            SearchTimer.Tick += new EventHandler(SearchTimer_Tick);


            VedioType vedioType = VedioType.所有;
            bool result = Enum.TryParse<VedioType>(Properties.Settings.Default.VedioType, out vedioType);
            VedioType = vedioType;


            //获得所有数据库
            LoadDataBaseList();

            DataBase.MovieListChanged += (o, e) => { CurrentPage = 1; };


        }




        #region "RelayCommand"
        public RelayCommand ResetCommand { get; set; }
        public RelayCommand GenreCommand { get; set; }
        public RelayCommand ActorCommand { get; set; }
        public RelayCommand LabelCommand { get; set; }

        public RelayCommand FavoritesCommand { get; set; }
        public RelayCommand RecentCommand { get; set; }

        public RelayCommand RecentWatchCommand { get; set; }
        public RelayCommand<VedioType> CensoredCommand { get; set; }
        public RelayCommand<VedioType> UncensoredCommand { get; set; }
        public RelayCommand<VedioType> EuropeCommand { get; set; }
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
                //if (ClickGridType == 0) GetGenreList();
                //else if (ClickGridType == 1) GetActorList();
                //else GetLabelList();
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
            }
        }


        private MySearchType _AllSearchType = (MySearchType)Enum.Parse(typeof(MySearchType), Properties.Settings.Default.AllSearchType, true);

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

        private ObservableCollection<Movie> currentmovielist;


        public ObservableCollection<Movie> CurrentMovieList
        {
            get { return currentmovielist; }
            set
            {
                currentmovielist = value;
                RaisePropertyChanged();
                CurrentMovieListHideOrChanged?.Invoke(this, EventArgs.Empty);
                if (MovieList != null) TotalCount = MovieList.Count;
                IsFlipOvering = false;
            }
        }


        private ObservableCollection<Movie> _DetailsDataList;


        public ObservableCollection<Movie> DetailsDataList
        {
            get { return _DetailsDataList; }
            set
            {
                _DetailsDataList = value;
                RaisePropertyChanged();
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


        public List<Movie> MovieList;




        private ObservableCollection<Genre> genrelist;
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


        public ObservableCollection<string> _FilePathClassification;


        public ObservableCollection<string> FilePathClassification
        {
            get { return _FilePathClassification; }
            set
            {
                _FilePathClassification = value;
                RaisePropertyChanged();

            }
        }


        public ObservableCollection<string> _CurrentSearchCandidate;


        public ObservableCollection<string> CurrentSearchCandidate
        {
            get { return _CurrentSearchCandidate; }
            set
            {
                _CurrentSearchCandidate = value;
                RaisePropertyChanged();

            }
        }



        public ObservableCollection<Movie> _SearchCandidate;


        public ObservableCollection<Movie> SearchCandidate
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
        private string _SortType = Properties.Settings.Default.SortType;
        public string SortType
        {
            get { return _SortType; }
            set
            {
                _SortType = value;
                RaisePropertyChanged();
            }
        }


        private bool _SortDescending = Properties.Settings.Default.SortDescending;
        public bool SortDescending
        {
            get { return _SortDescending; }
            set
            {
                _SortDescending = value;
                RaisePropertyChanged();
            }
        }



        private double _VedioTypeACount = 0;
        public double VedioTypeACount
        {
            get { return _VedioTypeACount; }
            set
            {
                _VedioTypeACount = value;
                RaisePropertyChanged();
            }
        }



        private double _VedioTypeBCount = 0;
        public double VedioTypeBCount
        {
            get { return _VedioTypeBCount; }
            set
            {
                _VedioTypeBCount = value;
                RaisePropertyChanged();
            }
        }



        private double _VedioTypeCCount = 0;
        public double VedioTypeCCount
        {
            get { return _VedioTypeCCount; }
            set
            {
                _VedioTypeCCount = value;
                RaisePropertyChanged();
            }
        }

        private double _RecentWatchedCount = 0;
        public double RecentWatchedCount
        {
            get { return _RecentWatchedCount; }
            set
            {
                _RecentWatchedCount = value;
                RaisePropertyChanged();
            }
        }


        private double _AllVedioCount = 0;
        public double AllVedioCount
        {
            get { return _AllVedioCount; }
            set
            {
                _AllVedioCount = value;
                RaisePropertyChanged();
            }
        }

        private double _FavoriteVedioCount = 0;
        public double FavoriteVedioCount
        {
            get { return _FavoriteVedioCount; }
            set
            {
                _FavoriteVedioCount = value;
                RaisePropertyChanged();
            }
        }

        private double _RecentVedioCount = 0;
        public double RecentVedioCount
        {
            get { return _RecentVedioCount; }
            set
            {
                _RecentVedioCount = value;
                RaisePropertyChanged();
            }
        }


        public bool _IsScanning = false;
        public bool IsScanning
        {
            get { return _IsScanning; }
            set
            {
                _IsScanning = value;
                RaisePropertyChanged();
            }
        }


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


        public double _CurrentCount = 0;
        public double CurrentCount
        {
            get { return _CurrentCount; }
            set
            {
                _CurrentCount = value;
                RaisePropertyChanged();

            }
        }


        public double _TotalCount = 0;
        public double TotalCount
        {
            get { return _TotalCount; }
            set
            {
                _TotalCount = value;
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

        private bool IsSearching = false;

        public string Search
        {
            get { return search; }
            set
            {
                search = value;
                RaisePropertyChanged();
                if (search == "") Reset();
                else
                {

                    SearchTimer.Stop();
                    SearchTimer.Start();
                }

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



        public async void BeginSearch()
        {
            IsSearching = true;
            bool result = await Query();
            IsSearching = false;
            GetSearchCandidate(Search.ToProperSql());
            FlipOver();
        }

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



        private void SearchTimer_Tick(object sender, EventArgs e)
        {
            if (!IsSearching) BeginSearch();
            SearchTimer.Stop();
        }

        public void GetSearchCandidate(string Search)
        {
            CurrentSearchCandidate = new ObservableCollection<string>();
            if (Search == "") return;
            List<Movie> movies = new List<Movie>();
            if (AllSearchType == MySearchType.名称)
            {
                movies = MovieList.Where(m => m.title.ToUpper().Contains(Search.ToUpper())).ToList();
                foreach (Movie movie in movies)
                {
                    CurrentSearchCandidate.Add(movie.title);
                    if (CurrentSearchCandidate.Count >= Properties.Settings.Default.SearchCandidateMaxCount) break;
                }
            }
            else if (AllSearchType == MySearchType.演员)
            {
                movies = MovieList.Where(m => m.actor.ToUpper().Contains(Search.ToUpper())).ToList();
                foreach (Movie movie in movies)
                {
                    string[] actor = movie.actor.Split(actorSplitDict[movie.vediotype]);
                    foreach (var item in actor)
                    {
                        if (!string.IsNullOrEmpty(item) & item.IndexOf(' ') < 0)
                        {
                            if (!CurrentSearchCandidate.Contains(item) & item.ToUpper().IndexOf(Search.ToUpper()) >= 0) CurrentSearchCandidate.Add(item);
                            if (CurrentSearchCandidate.Count >= Properties.Settings.Default.SearchCandidateMaxCount) break;
                        }
                    }
                    if (CurrentSearchCandidate.Count >= Properties.Settings.Default.SearchCandidateMaxCount) break;
                }
            }
            else if (AllSearchType == MySearchType.识别码)
            {
                movies = MovieList.Where(m => m.id.ToUpper().Contains(Search.ToUpper())).ToList();
                foreach (Movie movie in movies)
                {
                    CurrentSearchCandidate.Add(movie.id);
                    if (CurrentSearchCandidate.Count >= Properties.Settings.Default.SearchCandidateMaxCount) break;
                }
            }
        }



        private void LoadImageAndInfo(ref Movie movie)
        {
            //加载图片
            if (Properties.Settings.Default.ShowImageMode == "海报图")
                movie.bigimage = StaticClass.GetBitmapImage(movie.id, "BigPic");
            else if (Properties.Settings.Default.ShowImageMode == "缩略图")
                movie.smallimage = StaticClass.GetBitmapImage(movie.id, "SmallPic");
            //显示翻译结果
            if (Properties.Settings.Default.TitleShowTranslate)
            {
                DB db = new DB("Translate");
                string translate_title = db.GetInfoBySql($"select translate_title from youdao where id='{movie.id}'");
                db.CloseDB();
                if (translate_title != "") movie.title = translate_title;
            }
        }



        public void Flow()
        {
            if (MovieList != null)
            {
                CurrentMovieListHideOrChanged?.Invoke(this, EventArgs.Empty); //停止下载
                int DisPlayNum = Properties.Settings.Default.DisplayNumber;
                int SetFlowNum = Properties.Settings.Default.FlowNum;
                Movies = new List<Movie>();
                for (int i = (CurrentPage - 1) * DisPlayNum + FlowNum * SetFlowNum; i < (CurrentPage - 1) * DisPlayNum + (FlowNum + 1) * SetFlowNum; i++)
                {
                    if (CurrentMovieList.Count < DisPlayNum)
                    {
                        if (i <= MovieList.Count - 1)
                        {
                            Movie movie = MovieList[i];
                            if (!Properties.Settings.Default.AsyncShowImage) LoadImageAndInfo(ref movie);
                            if (!string.IsNullOrEmpty(movie.id)) Movies.Add(movie);
                        }
                        else { break; }
                    }
                    else
                    {
                        FlowNum = 0;
                    }

                }
                CurrentMovieList.AddRange(Movies);
                CurrentCount = CurrentMovieList.Count;
                App.Current.Dispatcher.Invoke((Action)delegate
                {
                    CurrentMovieListChangedCompleted?.Invoke(this, EventArgs.Empty);
                    Main main = App.Current.Windows[0] as Main;
                    if (Properties.Settings.Default.ShowImageMode == "预览图") main.ImageSlideTimer.Start();//0.5s后开始展示预览图
                    IsFlipOvering = false;
                    main.AsyncLoadImage(); //异步加载图片
                    main.IsFlowing = false;
                    main.SetSelected();
                });
            }
        }


        public void Refresh()
        {
            App.Current.Windows[0].Cursor = Cursors.Wait;
            List<string> CurrentID = new List<string>();
            foreach (Movie movie in CurrentMovieList) CurrentID.Add(movie.id);
            Reset();
            App.Current.Windows[0].Cursor = Cursors.Arrow;
        }

        public void RefreshActor()
        {
            GetActorList();
        }


        public void ActorFlipOver()
        {
            stopwatch.Restart();
            if (ActorList != null)
            {
                TotalActorPage = (int)Math.Ceiling((double)ActorList.Count / (double)Properties.Settings.Default.ActorDisplayNum);

                //只在翻页时加载图片、显示翻译结果
                int ActorDisplayNum = Properties.Settings.Default.ActorDisplayNum;
                List<Actress> actresses = new List<Actress>();
                for (int i = (CurrentActorPage - 1) * ActorDisplayNum; i < CurrentActorPage * ActorDisplayNum; i++)
                {
                    if (i < ActorList.Count)
                    {
                        Actress actress = ActorList[i];
                        actress.smallimage = StaticClass.GetBitmapImage(actress.name, "Actresses");//不加载图片能节约 1s
                        actresses.Add(actress);
                    }
                    else { break; }
                    if (actresses.Count == ActorDisplayNum) { break; }
                }
                CurrentActorList = new ObservableCollection<Actress>();
                CurrentActorList.AddRange(actresses);
            }
            stopwatch.Stop();
            Console.WriteLine($"\n演员翻页用时：{stopwatch.ElapsedMilliseconds} ms");

        }

        public void DisposeMovieList(ObservableCollection<Movie> movies)
        {
            if (movies == null) return;

            for (int i = 0; i < movies.Count; i++)
            {
                movies[i].bigimage = null;
                movies[i].smallimage = null;
            }
            GC.Collect();
        }




        public List<Movie> Movies;

        /// <summary>
        /// 翻页：加载图片以及其他
        /// </summary>
        public void FlipOver()
        {




            if (Properties.Settings.Default.ShowImageMode == "列表模式")
            {
                ShowDetailsData();
            }
            else
            {

                IsFlipOvering = true;
                Task.Run(() =>
                {
                    if (MovieList != null)
                    {
                        TotalPage = (int)Math.Ceiling((double)MovieList.Count / (double)Properties.Settings.Default.DisplayNumber);

                        int DisPlayNum = Properties.Settings.Default.DisplayNumber;
                        int FlowNum = Properties.Settings.Default.FlowNum;
                        DisposeMovieList(CurrentMovieList);

                        Console.WriteLine("CurrentPage=" + CurrentPage);
                        Movies = new List<Movie>();
                        for (int i = (CurrentPage - 1) * DisPlayNum; i < (CurrentPage - 1) * DisPlayNum + FlowNum; i++)
                        {
                            if (i <= MovieList.Count - 1)
                            {
                                Movie movie = MovieList[i];
                                if (!Properties.Settings.Default.AsyncShowImage) LoadImageAndInfo(ref movie);
                                Movies.Add(movie);

                            }
                            else { break; }
                            if (Movies.Count == FlowNum) { break; }

                        }

                        
                        App.Current.Dispatcher.Invoke((Action)delegate
                        {
                            CurrentMovieList = new ObservableCollection<Movie>();
                            CurrentMovieList.AddRange(Movies);
                            CurrentCount = CurrentMovieList.Count;
                            Main main = App.Current.Windows[0] as Main;
                            if (Properties.Settings.Default.ShowImageMode == "预览图") main.ImageSlideTimer.Start();//0.5s后开始展示预览图
                            IsFlipOvering = false;
                            main.AsyncLoadImage(); //异步加载图片
                            main.IsFlowing = false;
                            main.SetSelected();

                            
                                CurrentMovieListChangedCompleted?.Invoke(this, EventArgs.Empty);



                        });

                    }
                });
            }

        }




        //获得标签
        public void GetLabelList()
        {
            TextType = "标签";
            List<string> labels = DataBase.SelectLabelByVedioType(VedioType);
            LabelList = new ObservableCollection<string>();
            LabelList.AddRange(labels);
        }


        //获得演员，信息照片都获取
        public void GetActorList()
        {
            TextType = "演员";
            Statistic();
            stopwatch.Restart();

            List<Actress> Actresses = DataBase.SelectAllActorName(VedioType);
            stopwatch.Stop();
            Console.WriteLine($"\n加载演员用时：{stopwatch.ElapsedMilliseconds} ms");

            if (ActorList != null && Actresses != null && Actresses.Count == ActorList.ToList().Count) { return; }
            ActorList = new ObservableCollection<Actress>();
            ActorList.AddRange(Actresses);

            ActorFlipOver();


        }


        //获得类别
        public void GetGenreList()
        {
            TextType = "类别";
            Statistic();
            List<Genre> Genres = DataBase.SelectGenreByVedioType(VedioType);
            GenreList = new ObservableCollection<Genre>();
            GenreList.AddRange(Genres);

        }


        public void AddToRecentWatch(string ID)
        {
            DateTime dateTime = DateTime.Now.Date;
            if (!string.IsNullOrEmpty(ID))
            {
                if (RecentWatched.ContainsKey(dateTime))
                {
                    if (!RecentWatched[dateTime].Contains(ID))
                        RecentWatched[dateTime].Add(ID);

                }
                else
                {
                    RecentWatched.Add(dateTime, new List<string>() { ID });
                }
            }



            List<string> total = new List<string>();

            foreach (var keyvalue in RecentWatched)
            {
                total = total.Union(keyvalue.Value).ToList();
            }


            RecentWatchedCount = total.Count;

        }


        public void GetRecentWatch()
        {
            List<Movie> movies = new List<Movie>();
            foreach (var keyValuePair in RecentWatched)
            {
                if (keyValuePair.Key <= DateTime.Now && keyValuePair.Key >= DateTime.Now.AddDays(-1 * Properties.Settings.Default.RecentDays))
                {
                    foreach (var item in keyValuePair.Value)
                    {
                        if (DataBase.IsMovieExist(item))
                        {
                            movies.Add(DataBase.SelectMovieByID(item));
                        }

                    }
                }
            }

            TextType = "最近播放";
            Statistic();
            MovieList = new List<Movie>();
            MovieList.AddRange(movies);
            CurrentPage = 1;
            FlipOver();
            if (MovieList.Count == 0 && RecentWatchedCount > 0)
                HandyControl.Controls.Growl.Info("该库中无最近播放，请切换到其他库！");
            else if (MovieList.Count < RecentWatchedCount)
                HandyControl.Controls.Growl.Info($"该库中仅发现 {MovieList.Count} 个最近播放");

        }

        public void GetRecentMovie()
        {
            TextType = "最近创建";
            Statistic();
            string date1 = DateTime.Now.AddDays(-1 * Properties.Settings.Default.RecentDays).Date.ToString("yyyy-MM-dd");
            string date2 = DateTime.Now.ToString("yyyy-MM-dd");
            MovieList = DataBase.SelectPartialInfo($"SELECT * from movie WHERE scandate BETWEEN '{date1}' AND '{date2}'");
            FlipOver();

            if (MovieList.Count == 0 && RecentVedioCount > 0)
                HandyControl.Controls.Growl.Info("该库中无最近创建，请切换到其他库！");
            else if (MovieList.Count < RecentVedioCount)
                HandyControl.Controls.Growl.Info($"该库中仅发现 {MovieList.Count} 个最近创建");

        }

        public void GetSamePathMovie(string path)
        {
            //Bug: 大数据库卡死
            TextType = path;
            List<Movie> movies = DataBase.SelectMoviesBySql($"SELECT * from movie WHERE filepath like '%{path}%'");
            MovieList = new List<Movie>();
            MovieList.AddRange(movies);
            CurrentPage = 1;
            FlipOver();
        }

        public void GetFavoritesMovie()
        {
            TextType = "我的喜爱";
            Statistic();
            MovieList = DataBase.SelectPartialInfo("SELECT * from movie where favorites>0 and favorites<=5");
            FlipOver();
        }

        public void GetMoviebyStudio(string moviestudio)
        {
            Statistic();
            MovieList = DataBase.SelectMoviesBySql($"SELECT * from movie where studio = '{moviestudio}'");
            FlipOver();

        }

        public void GetMoviebyTag(string movietag)
        {
            Statistic();
            MovieList = DataBase.SelectMoviesBySql($"SELECT * from movie where tag like '%{movietag}%'");
            FlipOver();
        }

        public void GetMoviebyDirector(string moviedirector)
        {
            Statistic();
            MovieList = DataBase.SelectMoviesBySql($"SELECT * from movie where director ='{moviedirector}'");
            FlipOver();

        }


        public void GetMoviebyLabel(string movielabel)
        {
            Statistic();
            var movies = DataBase.SelectMoviesBySql("SELECT * from movie where label like '%" + movielabel + "%'");
            MovieList = new List<Movie>();
            movies?.ForEach(arg =>
            {
                if (arg.label.Split(' ').Any(m => m.ToUpper() == movielabel.ToUpper())) MovieList.Add(arg);
            });
            CurrentPage = 1;
            FlipOver();
        }



        public void GetMoviebyActress(Actress actress)
        {
            Statistic();
            int vediotype = (int)VedioType;
            //根据视频类型选择演员

            List<Movie> movies;
            if (actress.id == "")
                movies = DataBase.SelectMoviesBySql($"SELECT * from movie where actor like '%{actress.name}%'");
            else
                movies = DataBase.SelectMoviesBySql($"SELECT * from movie where actorid like '%{actress.id}%'");


            MovieList = new List<Movie>();
            movies?.ForEach(arg =>
            {
                if (arg.actor.Split(actorSplitDict[arg.vediotype]).Any(m => m.ToUpper() == actress.name.ToUpper())) MovieList.Add(arg);
            });
            CurrentPage = 1;
            FlipOver();
        }


        //根据视频类型选择演员
        public void GetMoviebyActressAndVetioType(Actress actress)
        {

            List<Movie> movies;
            if (actress.id == "")
            {
                if (VedioType == 0) { movies = DataBase.SelectMoviesBySql($"SELECT * from movie where actor like '%{actress.name}%'"); }
                else { movies = DataBase.SelectMoviesBySql($"SELECT * from movie where actor like '%{actress.name}%' and vediotype={(int)VedioType}"); }
            }
            else
            {
                if (VedioType == 0) { movies = DataBase.SelectMoviesBySql($"SELECT * from movie where actorid like '%{actress.id}%'"); }
                else { movies = DataBase.SelectMoviesBySql($"SELECT * from movie where actorid like '%{actress.id}%' and vediotype={(int)VedioType}"); }
            }


            MovieList = new List<Movie>();
            if (movies != null || movies.Count > 0)
            {
                movies.ForEach(arg =>
                {
                    try { if (arg.actor.Split(actorSplitDict[arg.vediotype]).Any(m => m.ToUpper() == actress.name.ToUpper())) MovieList.Add(arg); }
                    catch (Exception e)
                    {
                        Logger.LogE(e);
                    }
                });

            }
            CurrentPage = 1;


        }


        public void GetMoviebyGenre(string moviegenre)
        {

            var movies = DataBase.SelectMoviesBySql("SELECT * from movie where genre like '%" + moviegenre + "%'");
            MovieList = new List<Movie>();
            movies?.ForEach(arg =>
            {
                if (arg.genre.Split(' ').Any(m => m.ToUpper() == moviegenre.ToUpper())) MovieList.Add(arg);
            });
            CurrentPage = 1;
        }


        public void GetMoviebyVedioType(VedioType vt)
        {
            CurrentVedioType = vt;
            TextType = vt.ToString();
            Statistic();
            MovieList = DataBase.SelectPartialInfo("SELECT * from movie where vediotype=" + (int)vt);
            FlipOver();
        }




        /// <summary>
        /// 在数据库中搜索影片
        /// </summary>
        public async Task<bool> Query()
        {
            if (!DataBase.IsTableExist("movie")) { return false; }

            IsSearching = true;
            if (Search == "") return false;

            string FormatSearch = Search.ToProperSql();

            if (string.IsNullOrEmpty(FormatSearch)) { return false; }

            string fanhao = "";

            if (CurrentVedioType == VedioType.欧美)
                fanhao = Identify.GetEuFanhao(FormatSearch);
            else
                fanhao = Identify.GetFanhao(FormatSearch);

            string searchContent = "";
            if (fanhao == "") searchContent = FormatSearch;
            else searchContent = fanhao;



            if (Properties.Settings.Default.AllSearchType == "识别码")
            {
                TextType = "搜索识别码：" + searchContent;
                MovieList = DataBase.SelectPartialInfo($"SELECT * FROM movie where id like '%{searchContent}%'");
            }

            else if (Properties.Settings.Default.AllSearchType == "名称")
            {
                TextType = "搜索名称：" + searchContent;
                MovieList = DataBase.SelectPartialInfo($"SELECT * FROM movie where title like '%{searchContent}%'");
            }

            else if (Properties.Settings.Default.AllSearchType == "演员")
            {
                TextType = "搜索演员：" + searchContent;
                MovieList = DataBase.SelectPartialInfo($"SELECT * FROM movie where actor like '%{searchContent}%'");
            }




            return true;
        }


        public void RandomDisplay()
        {
            TextType = "随机展示";
            Statistic();
            MovieList = DataBase.SelectMoviesBySql($"SELECT * FROM movie ORDER BY RANDOM() limit {Properties.Settings.Default.DisplayNumber}");
            FlipOver();
        }




        public void Reset()
        {
            TextType = "所有视频";
            Statistic();
            MovieList = DataBase.SelectPartialInfo("SELECT * FROM movie");
            FlipOver();
        }


        public void ShowDetailsData()
        {
            Task.Run(() =>
            {
                
                TextType = "详情模式";
                Statistic();
                List<Movie> movies = new List<Movie>();

                TotalPage = (int)Math.Ceiling((double)MovieList.Count / (double)Properties.Settings.Default.DisplayNumber);
                if (MovieList != null && MovieList.Count > 0)
                {
                    MovieList.ForEach(arg =>
                    {
                        Movie movie = DataBase.SelectMovieByID(arg.id);
                        if (movie != null) movies.Add(movie);
                    });

                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        //App.Current.Windows[0].Cursor = Cursors.Wait;
                        DetailsDataList = new ObservableCollection<Movie>();
                        DetailsDataList.AddRange(movies);
                    });


                }
                CurrentCount = DetailsDataList.Count;
                TotalCount = MovieList.Count;
                IsFlipOvering = false;
                //App.Current.Windows[0].Cursor = Cursors.Arrow;
            });
        }


        /// <summary>
        /// 统计：加载时间 <70ms (15620个信息)
        /// </summary>
        public void Statistic()
        {
            if (!DataBase.IsTableExist("movie")) { return; }

            stopwatch.Restart();
            AllVedioCount = DataBase.SelectCountBySql("");
            FavoriteVedioCount = DataBase.SelectCountBySql("where favorites>0 and favorites<=5");
            VedioTypeACount = DataBase.SelectCountBySql("where vediotype=1");
            VedioTypeBCount = DataBase.SelectCountBySql("where vediotype=2");
            VedioTypeCCount = DataBase.SelectCountBySql("where vediotype=3");

            string date1 = DateTime.Now.AddDays(-1 * Properties.Settings.Default.RecentDays).Date.ToString("yyyy-MM-dd");
            string date2 = DateTime.Now.ToString("yyyy-MM-dd");
            RecentVedioCount = DataBase.SelectCountBySql($"WHERE scandate BETWEEN '{date1}' AND '{date2}'");
            stopwatch.Stop();
            Console.WriteLine($"\n统计用时：{stopwatch.ElapsedMilliseconds} ms");
        }



        public void LoadFilePathClassfication()
        {
            //加载路经筛选
            FilePathClassification = new ObservableCollection<string>();
            foreach (Movie movie in MovieList)
            {
                string path = GetPathByDepth(movie.filepath, Properties.Settings.Default.FilePathClassificationMaxDepth);
                if (!FilePathClassification.Contains(path)) FilePathClassification.Add(path);
                if (FilePathClassification.Count > Properties.Settings.Default.FilePathClassificationMaxCount) break;
            }
        }

        private string GetPathByDepth(string path, int depth)
        {

            if (string.IsNullOrEmpty(path) || path.IndexOf("\\") < 0) return "";
            string[] paths = path.Split('\\');
            string result = "";
            for (int i = 0; i < paths.Length - 1; i++)
            {
                result += paths[i] + "\\";
                if (i >= depth - 1) break;
            }
            return result;



        }



    }
}
