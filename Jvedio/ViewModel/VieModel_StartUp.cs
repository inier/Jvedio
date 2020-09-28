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
    public class VieModel_StartUp : ViewModelBase
    {
        protected DataBase cdb;
        protected List<Movie> Movies;

        public VieModel_StartUp()
        {
            ListDatabaseCommand = new RelayCommand(ListDatabase);


        }

        #region "RelayCommand"
        public RelayCommand ListDatabaseCommand { get; set; }

        #endregion





        public void ListDatabase()
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






    }
}
