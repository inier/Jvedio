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
using static Jvedio.StaticVariable;
using LiveCharts;
using LiveCharts.Wpf;

namespace Jvedio.ViewModel
{
    public class VieModel_DBManagement : ViewModelBase
    {
        protected List<Movie> Movies;

        public VieModel_DBManagement()
        {
            ListDatabaseCommand = new RelayCommand(ListDatabase);
            StatisticCommand = new RelayCommand(Statistic);
            Page2Command = new RelayCommand(Page2);
            Page3Command = new RelayCommand(Page3);


        }

        #region "RelayCommand"
        public RelayCommand ListDatabaseCommand { get; set; }
        public RelayCommand StatisticCommand { get; set; }
        public RelayCommand Page2Command { get; set; }
        public RelayCommand Page3Command { get; set; }

        #endregion


      


        public void ListDatabase()
        {
            DataBases = new ObservableCollection<string>();
            var fiels = Directory.GetFiles("DataBase","*.sqlite", SearchOption.TopDirectoryOnly).ToList();
            fiels.ForEach(arg => DataBases.Add(arg.Split('\\').Last().Split('.').First().ToLower()));

            if (!DataBases.Contains("info")) DataBases.Add("info");

        }



        public  void Page2()
        {

            //LoadID();
            //LoadGenre();
            //LoadTag();
            //LoadActor();


        }

        public void Page3()
        {

            //LoadID();
            //LoadGenre();
            //LoadTag();
            //LoadActor();


        }




        public async void Statistic()
        {
            Movies = new List<Movie>();
            string name = Properties.Settings.Default.DataBasePath.Split('\\').Last().Split('.').First().ToLower();
            if (name != "info") name = "DataBase\\" + name;
            DB db = new DB(name);
            Movies = await db.SelectMoviesById("");
            

            AllCount = Movies.Count;
            UncensoredCount = Movies.Where(arg => arg.vediotype == 1).Count();
            CensoredCount = Movies.Where(arg => arg.vediotype == 2).Count();
            EuropeCount = Movies.Where(arg => arg.vediotype == 3).Count();

            CensoredCountPercent=(int)(100* CensoredCount / (AllCount == 0 ? 1 : AllCount));
            UncensoredCountPercent = (int)(100 * UncensoredCount / (AllCount == 0 ? 1 : AllCount));
            EuropeCountPercent = (int)(100 * EuropeCount / (AllCount == 0 ? 1 : AllCount));


            ChartValuesCensoredCount = new ChartValues<double>() { CensoredCount };
            ChartValuesUnCensoredCount = new ChartValues<double>() { UncensoredCount };
            ChartValuesEuropeCount = new ChartValues<double>() { EuropeCount };
            PointLabel = chartPoint => string.Format("{0} ({1:P})", chartPoint.Y, chartPoint.Participation);

            //按识别码显示
            LoadID();
            LoadGenre();
            LoadTag();
            LoadActor();


            Formatter = value => value.ToString("N");

        }







        private void LoadActor()
        {
            Dictionary<string, double> dic = new Dictionary<string, double>();
            Movies.ForEach(arg => {
                arg.actor.Split(new char[] { ' ','/'}).ToList().ForEach(item => {
                    if (!string.IsNullOrEmpty(item))
                    {
                        if (!dic.ContainsKey(item))
                            dic.Add(item, 1);
                        else
                            dic[item] += 1;
                    }

                });



            });

            var dicSort = dic.OrderByDescending(arg => arg.Value).ToDictionary(x => x.Key, y => y.Value);



            ActorLabels = dicSort.Keys.ToArray();

            ChartValues<double> cv = new ChartValues<double>();
            dicSort.Values.ToList().ForEach(arg => cv.Add(arg));


            ActorSeriesCollection = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title="数目",
                    Values = cv
                }
            };
        }

        private void LoadTag()
        {
            Dictionary<string, double> dic = new Dictionary<string, double>();
            Movies.ForEach(arg => {
                arg.tag.Split(' ').ToList().ForEach(item => {
                    if (!string.IsNullOrEmpty(item))
                    {
                        if (!dic.ContainsKey(item))
                            dic.Add(item, 1);
                        else
                            dic[item] += 1;
                    }

                });



            });

            var dicSort = dic.OrderByDescending(arg => arg.Value).ToDictionary(x => x.Key, y => y.Value);



            TagLabels = dicSort.Keys.ToArray();

            ChartValues<double> cv = new ChartValues<double>();
            dicSort.Values.ToList().ForEach(arg => cv.Add(arg));


            TagSeriesCollection = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title="数目",
                    Values = cv
                }
            };
        }

        private void LoadGenre()
        {
            Dictionary<string, double> dic = new Dictionary<string, double>();
            Movies.ForEach(arg => {
                arg.genre.Split(' ').ToList().ForEach(item => {
                    if (!string.IsNullOrEmpty(item))
                    {
                        if (!dic.ContainsKey(item))
                            dic.Add(item, 1);
                        else
                            dic[item] += 1;
                    }

                });


               
            });

            var dicSort = dic.OrderByDescending(arg => arg.Value).ToDictionary(x => x.Key, y => y.Value);



            GenreLabels = dicSort.Keys.ToArray();

            ChartValues<double> cv = new ChartValues<double>();
            dicSort.Values.ToList().ForEach(arg => cv.Add(arg));


            GenreSeriesCollection = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title="数目",
                    Values = cv
                }
            };
        }

        private void LoadID()
        {
            Dictionary<string, double> dic = new Dictionary<string, double>();
            Movies.ForEach(arg => {
                string id = "";
                if (arg.vediotype == 3)
                    id = Identify.GetEuFanhao(arg.id).Split('.')[0];
                else
                    id = Identify.GetFanhao(arg.id).Split('-')[0];
                if (!dic.ContainsKey(id))
                    dic.Add(id, 1);
                else
                    dic[id] += 1;
            });

            var dicSort = dic.OrderByDescending(arg => arg.Value).ToDictionary(x => x.Key, y => y.Value);



            Labels = dicSort.Keys.ToArray();

            ChartValues<double> cv = new ChartValues<double>();
            dicSort.Values.ToList().ForEach(arg => cv.Add(arg));


            SeriesCollection = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title="数目",
                    Values = cv
                }
            };
        }




        public SeriesCollection _ActorSeriesCollection;
        public SeriesCollection ActorSeriesCollection
        {
            get { return _ActorSeriesCollection; }
            set
            {
                _ActorSeriesCollection = value;
                RaisePropertyChanged();
            }
        }



        public string[] _ActorLabels;

        public string[] ActorLabels
        {

            get { return _ActorLabels; }
            set
            {
                _ActorLabels = value;
                RaisePropertyChanged();
            }
        }






        public SeriesCollection _TagSeriesCollection;
        public SeriesCollection TagSeriesCollection
        {
            get { return _TagSeriesCollection; }
            set
            {
                _TagSeriesCollection = value;
                RaisePropertyChanged();
            }
        }



        public string[] _TagLabels;

        public string[] TagLabels
        {

            get { return _TagLabels; }
            set
            {
                _TagLabels = value;
                RaisePropertyChanged();
            }
        }


        public SeriesCollection _GenreSeriesCollection;
        public SeriesCollection GenreSeriesCollection
        {
            get { return _GenreSeriesCollection; }
            set
            {
                _GenreSeriesCollection = value;
                RaisePropertyChanged();
            }
        }



        public string[] _GenreLabels;

        public string[] GenreLabels
        {

            get { return _GenreLabels; }
            set
            {
                _GenreLabels = value;
                RaisePropertyChanged();
            }
        }


        public SeriesCollection _SeriesCollection;
        public SeriesCollection SeriesCollection {
            get { return _SeriesCollection; }
            set
            {
                _SeriesCollection = value;
                RaisePropertyChanged();
            }
        }



        public string[] _Labels;

        public string[] Labels
        {

            get { return _Labels; }
            set
            {
                _Labels = value;
                RaisePropertyChanged();
            }
        }

        public Func<double, string> Formatter { get; set; }





        private ChartValues<double> _ChartValuesCensoredCount;

        public ChartValues<double> ChartValuesCensoredCount
        {
            get { return _ChartValuesCensoredCount; }
            set
            {
                _ChartValuesCensoredCount = value;
                RaisePropertyChanged();
            }
        }
        private ChartValues<double> _ChartValuesUnCensoredCount;


        public ChartValues<double> ChartValuesUnCensoredCount
        {
            get { return _ChartValuesUnCensoredCount; }
            set
            {
                _ChartValuesUnCensoredCount = value;
                RaisePropertyChanged();
            }
        }
        public ChartValues<double> _ChartValuesEuropeCount { get; set; }


        public ChartValues<double> ChartValuesEuropeCount
        {
            get { return _ChartValuesEuropeCount; }
            set
            {
                _ChartValuesEuropeCount = value;
                RaisePropertyChanged();
            }
        }



        private Func<ChartPoint, string> _PointLabel;

        public Func<ChartPoint, string> PointLabel
        {
            get { return _PointLabel; }
            set
            {
                _PointLabel = value;
                RaisePropertyChanged();
            }


        }


        private double _CensoredCountPercent;

        public double CensoredCountPercent
        {
            get { return _CensoredCountPercent; }
            set
            {
                _CensoredCountPercent = value;
                RaisePropertyChanged();
            }


        }

        private double _UncensoredCountPercent;

        public double UncensoredCountPercent
        {
            get { return _UncensoredCountPercent; }
            set
            {
                _UncensoredCountPercent = value;
                RaisePropertyChanged();
            }


        }

        private double _EuropeCountPercent;

        public double EuropeCountPercent
        {
            get { return _EuropeCountPercent; }
            set
            {
                _EuropeCountPercent = value;
                RaisePropertyChanged();
            }


        }


        private double _AllCount;

        public double AllCount
        {
            get { return _AllCount; }
            set
            {
                _AllCount = value;
                RaisePropertyChanged();
            }


        }

        private double _censoredCount;

        public double CensoredCount
        {
            get { return _censoredCount; }
            set
            {
                _censoredCount = value;
                RaisePropertyChanged();
            }


        }

        private double _UncensoredCount;

        public double UncensoredCount
        {
            get { return _UncensoredCount; }
            set
            {
                _UncensoredCount = value;
                RaisePropertyChanged();
            }


        }

        private double _EuropeCount;

        public double EuropeCount
        {
            get { return _EuropeCount; }
            set
            {
                _EuropeCount = value;
                RaisePropertyChanged();
            }


        }


        private ObservableCollection<string> _DataBases;



        public ObservableCollection<string> DataBases
        {
            get { return _DataBases; }
            set
            {
                _DataBases = value;
                RaisePropertyChanged();
            }


        }

        private string _CurrentDataBase;

        public string CurrentDataBase
        {
            get { return _CurrentDataBase; }
            set
            {
                _CurrentDataBase = value;
                RaisePropertyChanged();
            }


        }





    }
}
