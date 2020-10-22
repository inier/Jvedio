using DynamicData.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Jvedio
{

    public class Movie
    {
        private string _id;
        public string id
        {
            get { return _id; }
            set
            {
                _id = value;
            }
        }
        private string _title;
        public string title { get { return _title; } set { _title = value; OnPropertyChanged(); } }
        public double filesize { get; set; }

        private string _filepath;
        public string filepath
        {
            get { return _filepath; }

            set
            {
                _filepath = value;
                OnPropertyChanged();
            }
        }
        public bool hassubsection { get; set; }

        private string _subsection;
        public string subsection
        {
            get { return _subsection; }
            set
            {
                _subsection = value;
                string[] t = value.Split(';');
                if (t.Count() > 2)
                {
                    hassubsection = true;
                    foreach (var item in t)
                    {
                        if (!string.IsNullOrEmpty(item) & item != "") subsectionlist.Add(item);
                    }
                }
                OnPropertyChanged();
            }
        }

        public List<string> subsectionlist { get; set; }


        public int vediotype { get; set; }
        public string scandate { get; set; }


        private string _releasedate;
        public string releasedate
        {
            get { return _releasedate; }
            set
            {
                DateTime dateTime = new DateTime(1900, 01, 01);
                DateTime.TryParse(value.ToString(), out dateTime);
                _releasedate = dateTime.ToString("yyyy-MM-dd");
            }
        }
        public int visits { get; set; }
        public string director { get; set; }
        public string genre { get; set; }
        public string tag { get; set; }
        public string actor { get; set; }
        public string actorid { get; set; }
        public string studio { get; set; }
        public float rating { get; set; }
        public string chinesetitle { get; set; }
        public int favorites { get; set; }
        public string label { get; set; }
        public string plot { get; set; }
        public string outline { get; set; }
        public int year { get; set; }
        public int runtime { get; set; }
        public string country { get; set; }
        public int countrycode { get; set; }
        public string otherinfo { get; set; }
        public string sourceurl { get; set; }
        public string source { get; set; }

        public string actressimageurl { get; set; }
        public string smallimageurl { get; set; }
        public string bigimageurl { get; set; }
        public string extraimageurl { get; set; }


        private BitmapSource _smallimage;
        private BitmapSource _bigimage;
        private Uri _gif;


        public BitmapSource smallimage { get { return _smallimage; } set { _smallimage = value; OnPropertyChanged(); } }
        public BitmapSource bigimage { get { return _bigimage; } set { _bigimage = value; OnPropertyChanged(); } }
        public Uri gif { get { return _gif; } set { _gif = value; OnPropertyChanged(); } }


        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Movie()
        {
            subsectionlist = new List<string>();
        }

    }


    public class Genre
    {
        public List<string> theme { get; set; }
        public List<string> role { get; set; }
        public List<string> clothing { get; set; }
        public List<string> body { get; set; }
        public List<string> behavior { get; set; }
        public List<string> playmethod { get; set; }
        public List<string> other { get; set; }
        public List<string> scene { get; set; }

        public Genre()
        {
            theme = new List<string>();
            role = new List<string>();
            clothing = new List<string>();
            body = new List<string>();
            behavior = new List<string>();
            playmethod = new List<string>();
            other = new List<string>();
            scene = new List<string>();
        }

    }

    public class DetailMovie : Movie
    {
        public List<string> genrelist { get; set; }
        public List<Actress> actorlist { get; set; }
        public List<string> labellist { get; set; }

        public List<BitmapSource> extraimagelist { get; set; }
        public List<string> extraimagePath { get; set; }

        public DetailMovie()
        {
            genrelist = new List<string>();
            actorlist = new List<Actress>();
            labellist = new List<string>();
            extraimagelist = new List<BitmapSource>();
            extraimagePath = new List<string>();
        }


    }

    public class Actress : INotifyPropertyChanged
    {
        public int num { get; set; }//仅仅用于计数
        public string id { get; set; }
        public string name { get; set; }
        public string actressimageurl { get; set; }
        private BitmapSource _smallimage;
        public BitmapSource smallimage { get { return _smallimage; } set { _smallimage = value; OnPropertyChanged(); } }
        public BitmapSource bigimage { get; set; }


        private string _birthday;
        public string birthday
        {
            get { return _birthday; }
            set
            {
                //验证数据
                DateTime dateTime;
                DateTime.TryParse(value, out dateTime);
                try
                {
                    _birthday = dateTime.ToString("yyyy-MM-dd");
                }
                catch { _birthday = ""; }

            }
        }

        private int _age;
        public int age
        {
            get { return _age; }
            set
            {
                int a = 0;
                int.TryParse(value.ToString(), out a);
                if (a < 0 || a > 200) a = 0;
                _age = a;
            }
        }

        private int _height;
        public int height
        {
            get { return _height; }
            set
            {
                int a = 0;
                int.TryParse(value.ToString(), out a);
                if (a < 0 || a > 300) a = 0;
                _height = a;
            }
        }

        private string _cup;
        public string cup { get { return _cup; } set { if (value == "") _cup = ""; else _cup = value[0].ToString().ToUpper(); } }


        private int _hipline;
        public int hipline
        {
            get { return _hipline; }
            set
            {
                int a = 0;
                int.TryParse(value.ToString(), out a);
                if (a < 0 || a > 500) a = 0;
                _hipline = a;
            }
        }


        private int _waist;
        public int waist
        {
            get { return _waist; }
            set
            {
                int a = 0;
                int.TryParse(value.ToString(), out a);
                if (a < 0 || a > 500) a = 0;
                _waist = a;
            }
        }


        private int _chest;
        public int chest
        {
            get { return _chest; }
            set
            {
                int a = 0;
                int.TryParse(value.ToString(), out a);
                if (a < 0 || a > 500) a = 0;
                _chest = a;
            }
        }

        public string birthplace { get; set; }
        public string hobby { get; set; }

        public string sourceurl { get; set; }
        public string source { get; set; }
        public string imageurl { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
