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
using static Jvedio.StaticClass;

namespace Jvedio.ViewModel
{
    public class VieModel_Settings : ViewModelBase
    {

        public VieModel_Settings()
        {
            DataBase = Properties.Settings.Default.DataBasePath.Split('\\').Last().Split('.').First();
            DataBases = ((Main)App.Current.Windows[0]).vieModel.DataBases;




        }


        public void Reset()
        {
            //读取配置文件
            ScanPath = new ObservableCollection<string>();
            foreach(var item in ReadScanPathFromConfig(DataBase))
            {
                ScanPath.Add(item);
            }
            if (ScanPath.Count == 0) ScanPath = null;

             Servers = new ObservableCollection<Server>();
            if(Properties.Settings.Default.Bus!="") Servers.Add(new Server() { IsEnable = Properties.Settings.Default.EnableBus, Url = Properties.Settings.Default.Bus, Available = 0, ServerTitle = "JavBus", LastRefreshDate = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") });
            if (Properties.Settings.Default.BusEurope != "") Servers.Add(new Server() { IsEnable = Properties.Settings.Default.EnableBusEu, Url = Properties.Settings.Default.BusEurope, Available = 0, ServerTitle = "JavBus 欧美", LastRefreshDate = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") });
            if (Properties.Settings.Default.DB != "") Servers.Add(new Server() { IsEnable = Properties.Settings.Default.EnableDB, Url = Properties.Settings.Default.DB,Cookie= Properties.Settings.Default.DBCookie, Available = 0, ServerTitle = "JavDB", LastRefreshDate = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") });
            if (Properties.Settings.Default.Library != "") Servers.Add(new Server() { IsEnable = Properties.Settings.Default.EnableLibrary, Url = Properties.Settings.Default.Library, Available = 0, ServerTitle = "JavLibrary", LastRefreshDate = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") });
            
        }


        private ObservableCollection<Server> _Servers;

        public ObservableCollection<Server> Servers
        {
            get { return _Servers; }
            set
            {
                _Servers = value;
                RaisePropertyChanged();
            }
        }


        private ObservableCollection<string> _ScanPath ;

        public ObservableCollection<string> ScanPath
        {
            get { return _ScanPath; }
            set
            {
                _ScanPath = value;
                RaisePropertyChanged();
            }
        }


        private Skin _Themes = (Skin)Enum.Parse(typeof(Skin), Properties.Settings.Default.Themes, true);

        public Skin Themes
        {
            get { return _Themes; }
            set
            {
                _Themes = value;
                RaisePropertyChanged();
            }
        }


        private Language _Language = (Language)Enum.Parse(typeof(Language), Properties.Settings.Default.Language, true);

        public Language Language
        {
            get { return _Language; }
            set
            {
                _Language = value;
                RaisePropertyChanged();
            }
        }

        private string _DataBase  ;

        public string DataBase
        {
            get { return _DataBase; }
            set
            {
                _DataBase = value;
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


        



    }
}
