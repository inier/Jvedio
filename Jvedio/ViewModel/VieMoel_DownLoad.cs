
using DynamicData.Annotations;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Jvedio.ViewModel
{
    public class VieMoel_DownLoad : ViewModelBase
    {




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


        public int currentpage = 1;
        public int CurrentPage
        {
            get { return currentpage; }
            set
            {
                currentpage = value;
                RaisePropertyChanged();
            }
        }




        public double _TotalProgress=0;

        public double TotalProgress
        {
            get { return _TotalProgress; }

            set {
                _TotalProgress = value;
                RaisePropertyChanged();
                ProgressBarValue = TotalProgress / TotalProgressMaximum * 100;
            }
        }

        public double _TotalProgressMaximum = 1;

        public double TotalProgressMaximum
        {
            get { return _TotalProgressMaximum; }

            set
            {
                _TotalProgressMaximum = value;
                RaisePropertyChanged();
                ProgressBarValue = TotalProgress / TotalProgressMaximum * 100;
            }
        }

        public double _ProgressBarValue = 0;

        public double ProgressBarValue
        {
            get { return _ProgressBarValue; }

            set
            {
                _ProgressBarValue = value;
                RaisePropertyChanged();
            }
        }

        public RelayCommand ResetCommand { get; set; }

        public VieMoel_DownLoad(){
            ResetCommand = new RelayCommand(Reset);

        }



        public ObservableCollection<DownLoadInfo> totalDownloadList;


        public ObservableCollection<DownLoadInfo> TotalDownloadList
        {
            get { return totalDownloadList; }
            set
            {
                totalDownloadList = value;
                RaisePropertyChanged();
            }
        }



        public ObservableCollection<DownLoadInfo> currentList;


        public ObservableCollection<DownLoadInfo> CurrentList
        {
            get { return currentList; }
            set
            {
                currentList = value;
                RaisePropertyChanged();
            }
        }





        public  void Reset()
        {
            
            var models =  DataBase.SelectMoviesById("");
            TotalDownloadList = new ObservableCollection<DownLoadInfo>();
            var filterlist = new List<string>();
            if (Properties.Settings.Default.DLLoadUnCensored) filterlist = filterlist.Union(models.Where(arg => arg.vediotype == 1).Select(g => g.id)).ToList();
            if (Properties.Settings.Default.DLLoadCensored) filterlist = filterlist.Union(models.Where(arg => arg.vediotype == 2).Select(g => g.id)).ToList();
            if (Properties.Settings.Default.DLLoadEurope) filterlist = filterlist.Union(models.Where(arg => arg.vediotype == 3).Select(g => g.id)).ToList();
            if (Properties.Settings.Default.DLLoadFC2) { filterlist = filterlist.Union(models.Where(arg => arg.id.IndexOf("FC2") >= 0).Select(g => g.id)).ToList(); }
            else
            {
                filterlist = filterlist.Except(models.Where(arg => arg.id.IndexOf("FC2") >=0).Select(g => g.id)).ToList();
            }




            filterlist?.ForEach(arg => { TotalDownloadList.Add(new DownLoadInfo() { id = arg ,progress=0,speed=0,progressbarvalue=0,maximum=1}); });
            
            TotalProgressMaximum = TotalDownloadList.Count;
            TotalProgress = 0;
            TotalPage = (int)Math.Ceiling((double)TotalDownloadList.Count / (double)Properties.Settings.Default.DLNum);
            FlipOver();
        }


        public void FlipOver()
        {
            if (TotalDownloadList != null)
            {
                CurrentList = new ObservableCollection<DownLoadInfo>();
                for (int i = (CurrentPage - 1) * Properties.Settings.Default.DLNum; i <= TotalDownloadList.Count - 1; i++)
                {
                    if (i <= TotalDownloadList.Count - 1)
                    {
                            CurrentList.Add(TotalDownloadList[i]); 
                    }
                    else { break; }
                    if (CurrentList.Count == Properties.Settings.Default.DLNum) { break; }
                }
            }
            //回到顶端
            ScrollViewer sv = App.Current.Windows[0].FindName("ScrollViewer") as ScrollViewer;
            if (sv != null) { sv.ScrollToTop(); }
        }




        private bool showSetBar = false;
        public bool ShowSetBar
        {
            get { return showSetBar; }
            set
            {
                showSetBar = value;
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


    }

    public class DownLoadInfo:INotifyPropertyChanged
    {
        public string id { get; set; }
        public double speed { get; set; }


        private double _progressbarvalue=0;
        public double progressbarvalue { 
            
            
            get { return _progressbarvalue; } 
            
            set {
                _progressbarvalue = value;
                OnPropertyChanged();
            } 
        
        }

        private double _progress;



        public double progress { get { return _progress; } 
            set {
                _progress = value;
                progressbarvalue =(int) (value / maximum * 100);
                
                OnPropertyChanged();
            } 
        }
        public double maximum { get; set; }

        private DownLoadState _state;
        public DownLoadState state {
            get { return _state; }
            set
            {
                _state = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }


}
