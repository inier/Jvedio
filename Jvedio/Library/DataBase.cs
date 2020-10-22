using DynamicData.Annotations;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using static Jvedio.StaticClass;
using static Jvedio.StaticVariable;

//https://www.cnblogs.com/leemano/p/6578050.html

namespace Jvedio
{


    public static  class DataBase
    {
        
        public static StringBuilder Path { get; set; }

        public static event EventHandler MovieListChanged;

        public static  void Init()
        {
            Path = File.Exists(Properties.Settings.Default.DataBasePath) ?   new StringBuilder(Properties.Settings.Default.DataBasePath): new StringBuilder("info.sqlite");
        }


        /// <summary>
        /// 获得表总行数
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public static int GetMaxCountByTable(string table)
        {
            Init();
            using (SQLiteConnection cn = new SQLiteConnection("data source=" + Path))
            {
                cn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    cmd.Connection = cn;

                    int result = 0;
                    cmd.CommandText = $"SELECT MAX(_ROWID_) FROM '{table}' LIMIT 1;";

                    using(SQLiteDataReader sr = cmd.ExecuteReader())
                    {
                        while (sr.Read()) int.TryParse(sr[0].ToString(), out result);
                        return result;
                    }
                }
            }


            
        }


        #region "SELECT"

        public static double SelectCountBySql(string sql)
        {
            Init();
            using (SQLiteConnection cn = new SQLiteConnection("data source=" + Path))
            {
                cn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    cmd.Connection = cn;
                    double result = 0;
                    if (sql == "")
                        cmd.CommandText = "SELECT count(id) FROM movie";
                    else
                        cmd.CommandText = "SELECT count(id) FROM movie " + sql;

                    using(SQLiteDataReader sr = cmd.ExecuteReader())
                    {
                        while (sr.Read())
                        {
                            double.TryParse(sr[0].ToString(), out result);
                        }
                        return result;
                    }


                }
            }
        }


        public static List<Actress> SelectAllActorName(VedioType vediotype)
        {
            Init();
            using (SQLiteConnection cn = new SQLiteConnection("data source=" + Path))
            {
                cn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    cmd.Connection = cn;

                    if (vediotype == 0)
                        cmd.CommandText = "SELECT actor FROM movie where vediotype>=0";
                    else
                        cmd.CommandText = "SELECT actor FROM movie where vediotype=" + (int)vediotype;

                    SQLiteDataReader sr = cmd.ExecuteReader();
                    Dictionary<string, int> dicresult = new Dictionary<string, int>();
                    List<string> actors = new List<string>();
                    while (sr.Read())
                    {
                        if (sr[0].ToString() != "")
                        {
                            foreach (var actor in sr[0].ToString().Split(actorSplitDict[(int)vediotype]))
                            {
                                if (actor != "")
                                {
                                    if (!dicresult.ContainsKey(actor))
                                        dicresult.Add(actor, 1);
                                    else
                                        dicresult[actor] += 1;
                                }
                            }
                        }

                    }
                    sr.Close();
                    //降序
                    var dicSort = from objDic in dicresult orderby objDic.Value descending select objDic;

                    List<Actress> result = new List<Actress>();
                    foreach (var item in dicSort)
                    {
                        Actress actress = new Actress()
                        {
                            id = "",
                            num = item.Value,
                            name = (item.Key.IndexOf(";") > 0 ? item.Key.Split(';')[0] : item.Key)
                        };
                        result.Add(actress);
                    }

                    return result;

                }
            }



        }

        public static string GenerateSort()
        {
            Init();
            string result = "";
            string SortType = Properties.Settings.Default.SortType;
            bool SortDescending = Properties.Settings.Default.SortDescending;
            if (SortType == Sort.识别码.ToString())
            {
                result = $"ORDER BY id {(SortDescending ? "DESC" : "ASC")}";
            }
            else if (SortType == Sort.文件大小.ToString())
            {
                result = $"ORDER BY filesize {(SortDescending ? "DESC" : "ASC")}";
            }
            else if (SortType == Sort.创建时间.ToString())
            {
                result = $"ORDER BY scandate {(SortDescending ? "DESC" : "ASC")}";
            }
            else if (SortType == Sort.喜爱程度.ToString())
            {
                result = $"ORDER BY favorites {(SortDescending ? "DESC" : "ASC")}";
            }
            else if (SortType == Sort.名称.ToString())
            {
                result = $"ORDER BY title {(SortDescending ? "DESC" : "ASC")}";
            }
            else if (SortType == Sort.访问次数.ToString())
            {
                result = $"ORDER BY visits {(SortDescending ? "DESC" : "ASC")}";
            }
            else if (SortType == Sort.发行日期.ToString())
            {
                result = $"ORDER BY releasedate {(SortDescending ? "DESC" : "ASC")}";
            }
            else if (SortType == Sort.评分.ToString())
            {
                result = $"ORDER BY rating {(SortDescending ? "DESC" : "ASC")}";
            }
            else if (SortType == Sort.时长.ToString())
            {
                result = $"ORDER BY runtime {(SortDescending ? "DESC" : "ASC")}";
            }
            else if (SortType == Sort.演员.ToString())
            {
                result = $"ORDER BY actor {(SortDescending ? "DESC" : "ASC")}";
            }
            else
            {
                result = $"ORDER BY id {(SortDescending ? "DESC" : "ASC")}";
            }
            return result;


        }

        public static List<Movie> SelectPartialInfo(string sql)
        {

            stopwatch.Restart();
            Init();
            string order = GenerateSort();
            using (SQLiteConnection cn = new SQLiteConnection("data source=" + Path))
            {
                cn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    cmd.Connection = cn;
                    List<Movie> result = new List<Movie>();
                    cmd.CommandText = $"{sql} {order}";
                    SQLiteDataReader sr = cmd.ExecuteReader();
                    while (sr.Read())
                    {
                        int vt = 1;
                        Movie movie = new Movie()
                        {
                            id = sr["id"].ToString(),
                            filepath = sr["filepath"].ToString(),
                            title = sr["title"].ToString(),
                            actor = sr["actor"].ToString(),
                            releasedate = sr["releasedate"].ToString(),
                            subsection=sr["subsection"].ToString()
                        };
                        int.TryParse(sr["releasedate"].ToString(), out vt);
                        movie.vediotype = vt;
                        result.Add(movie);
                    }
                    sr.Close();

                    List<Movie> movies = new List<Movie>();
                    movies.AddRange(result);
                    //可播放/不可播放
                    if (Properties.Settings.Default.OnlyShowPlay)
                    {

                        foreach (var item in movies)
                        {
                            if (!File.Exists((item.filepath))) result.Remove(item);
                        }
                    }
                    movies.Clear();
                    movies.AddRange(result);
                    //有图/无图
                    FilterImage(movies,ref result);



                    stopwatch.Stop();
                    Console.WriteLine($"\nSelectPartialInfo 用时：{stopwatch.ElapsedMilliseconds} ms");
                    MovieListChanged?.Invoke(null, EventArgs.Empty);
                    return result;
                }
            }


        }


        private static void FilterImage(List<Movie> allMovies, ref List<Movie>  movies)
        {
            MyViewType ShowViewMode = MyViewType.默认;
            Enum.TryParse<MyViewType>(Properties.Settings.Default.ShowViewMode,out ShowViewMode);

            MyImageType ShowImageMode = MyImageType.缩略图;
            Enum.TryParse<MyImageType>(Properties.Settings.Default.ShowImageMode, out ShowImageMode);

            Console.WriteLine(ShowViewMode.ToString());


            if (ShowViewMode == MyViewType.有图)
            {
                foreach (var item in allMovies)
                {
                    if (ShowImageMode == MyImageType.缩略图)
                    {
                        if (!File.Exists(BasePicPath + $"SmallPic\\{item.id}.jpg")) { movies.Remove(item); }
                    }
                        
                    else if (ShowImageMode == MyImageType.海报图)
                    {
                        if (!File.Exists(BasePicPath + $"BigPic\\{item.id}.jpg")) { movies.Remove(item); }
                    }
                        
                    else if (ShowImageMode == MyImageType.动态图)
                    {
                        if (!File.Exists(BasePicPath + $"Gif\\{item.id}.gif")) { movies.Remove(item); }
                    }
                        
                    else if (ShowImageMode == MyImageType.预览图)
                    {
                        if (!Directory.Exists(BasePicPath + $"ExtraPic\\{item.id}\\")) { movies.Remove(item); }
                        else
                        {
                            try { if (Directory.GetFiles(BasePicPath + $"ExtraPic\\{item.id}\\", "*.*", SearchOption.TopDirectoryOnly).Count() == 0) movies.Remove(item); }
                            catch { }
                        }
                    }
                }


            }
            else if (ShowViewMode == MyViewType.无图)
            {
                foreach (var item in allMovies)
                {
                    if (ShowImageMode == MyImageType.缩略图)
                    {
                        if (File.Exists(BasePicPath + $"SmallPic\\{item.id}.jpg")) { movies.Remove(item); }
                    }
                        
                    else if (ShowImageMode == MyImageType.海报图)
                    {
                        if (File.Exists(BasePicPath + $"BigPic\\{item.id}.jpg")) { movies.Remove(item); }
                    }
                        
                    else if (ShowImageMode == MyImageType.动态图)
                    {
                        if (File.Exists(BasePicPath + $"Gif\\{item.id}.gif")) { movies.Remove(item); }
                    }
                        
                    else if (ShowImageMode == MyImageType.预览图)
                    {
                        if (Directory.Exists(BasePicPath + $"ExtraPic\\{item.id}\\"))
                        {
                            try { if (Directory.GetFiles(BasePicPath + $"ExtraPic\\{item.id}\\", "*.*", SearchOption.TopDirectoryOnly).Count() > 0) movies.Remove(item); }
                            catch { }
                        }
                    }
                }
            }
        }


        /// <summary>
        /// 获得标签，如：奇葩( 20 )
        /// </summary>
        /// <param name="vediotype"></param>
        /// <returns></returns>

        public static List<string> SelectLabelByVedioType(VedioType vediotype)
        {
            Init();
            using (SQLiteConnection cn = new SQLiteConnection("data source=" + Path))
            {
                cn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    cmd.Connection = cn;

                    if (vediotype == VedioType.所有)
                        cmd.CommandText = "SELECT label FROM movie where vediotype>=0";
                    else
                        cmd.CommandText = "SELECT label FROM movie where vediotype=" + (int)vediotype;

                    string[] label;
                    SQLiteDataReader sr = cmd.ExecuteReader();
                    Dictionary<string, int> dicresult = new Dictionary<string, int>();
                    while (sr.Read())
                    {
                        label = sr["label"].ToString().Split(' ');
                        for (int i = 0; i < label.Count(); i++)
                        {
                            if (label[i].Length > 0 && label[i].IndexOf(' ') < 0)
                            {
                                if (dicresult.ContainsKey(label[i]))
                                    dicresult[label[i]] += 1;
                                else
                                    dicresult.Add(label[i], 1);
                            }
                        }
                    }
                    sr.Close();
                    //降序
                    var dicSort = from objDic in dicresult orderby objDic.Value descending select objDic;

                    List<string> result = new List<string>();
                    foreach (var item in dicSort)
                        result.Add(item.Key + "( " + item.Value + " )");
                    return result;
                }
            }


        }



        /// <summary>
        /// 获得类别列表
        /// </summary>
        /// <param name="vediotype"></param>
        /// <returns></returns>
        public static  List<Genre> SelectGenreByVedioType(VedioType vediotype)
        {
            Init();
            using (SQLiteConnection cn = new SQLiteConnection("data source=" + Path))
            {
                cn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    cmd.Connection = cn;
                    if (vediotype == VedioType.所有)
                        cmd.CommandText = "SELECT genre FROM movie where vediotype>=0";
                    else
                        cmd.CommandText = "SELECT genre FROM movie where vediotype=" + (int)vediotype;

                    string[] genre;
                    if (cn.State != ConnectionState.Open) return new List<Genre>();
                    SQLiteDataReader sr = cmd.ExecuteReader();
                    Dictionary<string, int> dicresult = new Dictionary<string, int>();
                    while (sr.Read())
                    {
                        genre = sr["genre"].ToString().Split(' ');
                        for (int i = 0; i < genre.Count(); i++)
                        {
                            if (genre[i].Length > 0 & genre[i].IndexOf(' ') < 0)
                            {
                                if (dicresult.ContainsKey(genre[i]))
                                {
                                    dicresult[genre[i]] += 1;
                                }
                                else { dicresult.Add(genre[i], 1); }
                            }

                        }
                    }
                    sr.Close();
                    //降序
                    var dicSort = from objDic in dicresult orderby objDic.Value descending select objDic;
                    List<Genre> result = new List<Genre>();

                    Genre newgenre = new Genre();
                    foreach (var item in dicSort)
                    {
                        if (vediotype == VedioType.欧美)
                        {
                            if (GenreEurope[0].IndexOf(item.Key) > 0) { newgenre.theme.Add(item.Key + "( " + item.Value + " )"); }
                            if (GenreEurope[1].IndexOf(item.Key) > 0) { newgenre.role.Add(item.Key + "( " + item.Value + " )"); }
                            if (GenreEurope[2].IndexOf(item.Key) > 0) { newgenre.clothing.Add(item.Key + "( " + item.Value + " )"); }
                            if (GenreEurope[3].IndexOf(item.Key) > 0) { newgenre.body.Add(item.Key + "( " + item.Value + " )"); }
                            if (GenreEurope[4].IndexOf(item.Key) > 0) { newgenre.behavior.Add(item.Key + "( " + item.Value + " )"); }
                            if (GenreEurope[5].IndexOf(item.Key) > 0) { newgenre.playmethod.Add(item.Key + "( " + item.Value + " )"); }
                            if (GenreEurope[6].IndexOf(item.Key) > 0) { newgenre.other.Add(item.Key + "( " + item.Value + " )"); }
                            if (GenreEurope[7].IndexOf(item.Key) > 0) { newgenre.scene.Add(item.Key + "( " + item.Value + " )"); }
                        }
                        else if (vediotype == VedioType.骑兵)
                        {
                            if (GenreCensored[0].IndexOf(item.Key) > 0) { newgenre.theme.Add(item.Key + "( " + item.Value + " )"); }
                            if (GenreCensored[1].IndexOf(item.Key) > 0) { newgenre.role.Add(item.Key + "( " + item.Value + " )"); }
                            if (GenreCensored[2].IndexOf(item.Key) > 0) { newgenre.clothing.Add(item.Key + "( " + item.Value + " )"); }
                            if (GenreCensored[3].IndexOf(item.Key) > 0) { newgenre.body.Add(item.Key + "( " + item.Value + " )"); }
                            if (GenreCensored[4].IndexOf(item.Key) > 0) { newgenre.behavior.Add(item.Key + "( " + item.Value + " )"); }
                            if (GenreCensored[5].IndexOf(item.Key) > 0) { newgenre.playmethod.Add(item.Key + "( " + item.Value + " )"); }
                            if (GenreCensored[6].IndexOf(item.Key) > 0) { newgenre.other.Add(item.Key + "( " + item.Value + " )"); }
                        }
                        else if (vediotype == VedioType.步兵)
                        {
                            if (GenreUncensored[0].IndexOf(item.Key) > 0) { newgenre.theme.Add(item.Key + "( " + item.Value + " )"); }
                            if (GenreUncensored[1].IndexOf(item.Key) > 0) { newgenre.role.Add(item.Key + "( " + item.Value + " )"); }
                            if (GenreUncensored[2].IndexOf(item.Key) > 0) { newgenre.clothing.Add(item.Key + "( " + item.Value + " )"); }
                            if (GenreUncensored[3].IndexOf(item.Key) > 0) { newgenre.body.Add(item.Key + "( " + item.Value + " )"); }
                            if (GenreUncensored[4].IndexOf(item.Key) > 0) { newgenre.behavior.Add(item.Key + "( " + item.Value + " )"); }
                            if (GenreUncensored[5].IndexOf(item.Key) > 0) { newgenre.playmethod.Add(item.Key + "( " + item.Value + " )"); }
                            if (GenreUncensored[6].IndexOf(item.Key) > 0) { newgenre.other.Add(item.Key + "( " + item.Value + " )"); }
                            if (GenreUncensored[7].IndexOf(item.Key) > 0) { newgenre.scene.Add(item.Key + "( " + item.Value + " )"); }
                        }
                        else if (vediotype == VedioType.所有)
                        {
                            if (GenreUncensored[0].IndexOf(item.Key) > 0 | GenreCensored[0].IndexOf(item.Key) > 0) { newgenre.theme.Add(item.Key + "( " + item.Value + " )"); }
                            if (GenreUncensored[1].IndexOf(item.Key) > 0 | GenreCensored[1].IndexOf(item.Key) > 0) { newgenre.role.Add(item.Key + "( " + item.Value + " )"); }
                            if (GenreUncensored[2].IndexOf(item.Key) > 0 | GenreCensored[2].IndexOf(item.Key) > 0) { newgenre.clothing.Add(item.Key + "( " + item.Value + " )"); }
                            if (GenreUncensored[3].IndexOf(item.Key) > 0 | GenreCensored[3].IndexOf(item.Key) > 0) { newgenre.body.Add(item.Key + "( " + item.Value + " )"); }
                            if (GenreUncensored[4].IndexOf(item.Key) > 0 | GenreCensored[4].IndexOf(item.Key) > 0) { newgenre.behavior.Add(item.Key + "( " + item.Value + " )"); }
                            if (GenreUncensored[5].IndexOf(item.Key) > 0 | GenreCensored[5].IndexOf(item.Key) > 0) { newgenre.playmethod.Add(item.Key + "( " + item.Value + " )"); }
                            if (GenreUncensored[6].IndexOf(item.Key) > 0 | GenreCensored[6].IndexOf(item.Key) > 0) { newgenre.other.Add(item.Key + "( " + item.Value + " )"); }
                            if (GenreUncensored[7].IndexOf(item.Key) > 0) { newgenre.scene.Add(item.Key + "( " + item.Value + " )"); }
                        }
                    }
                    result.Add(newgenre);
                    return result;
                }
            }



        }


        public static DetailMovie GetDetailMovieFromSQLiteDataReader(SQLiteDataReader sr)
        {
            DetailMovie detailMovie = new DetailMovie()
            {
                id = sr["id"].ToString(),
                title = sr["title"].ToString(),
                filesize = double.Parse(sr["filesize"].ToString()),
                filepath = sr["filepath"].ToString(),
                subsection = sr["subsection"].ToString(),
                vediotype = int.Parse(sr["vediotype"].ToString()),
                scandate = sr["scandate"].ToString(),
                releasedate = sr["releasedate"].ToString(),
                visits = int.Parse(sr["visits"].ToString()),
                director = sr["director"].ToString(),
                genre = sr["genre"].ToString(),
                tag = sr["tag"].ToString(),
                actor = sr["actor"].ToString(),
                actorid = sr["actorid"].ToString(),
                studio = sr["studio"].ToString(),
                rating = float.Parse(sr["rating"].ToString()),
                chinesetitle = sr["chinesetitle"].ToString(),
                favorites = int.Parse(sr["favorites"].ToString()),
                label = sr["label"].ToString(),
                plot = sr["plot"].ToString(),
                outline = sr["outline"].ToString(),
                year = int.Parse(sr["year"].ToString()),
                runtime = int.Parse(sr["runtime"].ToString()),
                country = sr["country"].ToString(),
                countrycode = int.Parse(sr["countrycode"].ToString()),
                otherinfo = sr["otherinfo"].ToString(),
                actressimageurl = sr["actressimageurl"].ToString(),
                smallimageurl = sr["smallimageurl"].ToString(),
                bigimageurl = sr["bigimageurl"].ToString(),
                extraimageurl = sr["extraimageurl"].ToString(),
                sourceurl = sr["sourceurl"].ToString(),
                source = sr["source"].ToString()
            };
            return detailMovie;
        }


        /// <summary>
        /// 通过 sql 获得影片列表
        /// </summary>
        /// <param name="sqltext"></param>
        /// <returns></returns>
        public static List<Movie> SelectMoviesBySql(string sqltext)
        {
            Init();
            using (SQLiteConnection cn = new SQLiteConnection("data source=" + Path))
            {
                cn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    cmd.Connection = cn;
                    List<Movie> result = new List<Movie>();
                    if (string.IsNullOrEmpty(sqltext)) return result;
                    else cmd.CommandText = sqltext;


                    SQLiteDataReader sr = cmd.ExecuteReader();
                    try
                    {
                        while (sr.Read())
                        {

                            Movie movie = (Movie)GetDetailMovieFromSQLiteDataReader(sr);
                            result.Add(movie);
                        }

                    }
                    catch { }
                    sr.Close();
                    return result;

                }
            }


            
        }


        /// <summary>
        /// 异步获得影片信息
        /// </summary>
        /// <param name="movieid"></param>
        /// <returns></returns>
        public static  List<Movie> SelectMoviesById(string movieid)
        {
            Init();
            using (SQLiteConnection cn = new SQLiteConnection("data source=" + Path))
            {
                cn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    cmd.Connection = cn;

                    List<Movie> result = new List<Movie>();
                    //if (IsLock) return result;
                    movieid = movieid.Replace("'", "").Replace("%", "");

                    if (string.IsNullOrEmpty(movieid))
                        cmd.CommandText = "SELECT * FROM movie";
                    else
                        cmd.CommandText = "SELECT * FROM movie where id like '%" + movieid + "%'";

                    SQLiteDataReader sr = cmd.ExecuteReader();
                    while (sr.Read())
                    {
                        Movie movie = (Movie)GetDetailMovieFromSQLiteDataReader(sr);
                        result.Add(movie);
                    }
                    sr?.Close(); 
                    
                    return result;

                }
            }
        }


        /// <summary>
        /// 异步获得单个影片
        /// </summary>
        /// <param name="movieid"></param>
        /// <returns></returns>
        public static  Movie SelectMovieByID(string movieid)
        {
            if (movieid == "") return null;

            Init();
            using (SQLiteConnection cn = new SQLiteConnection("data source=" + Path))
            {
                cn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    cmd.Connection = cn;


                    Movie result = new Movie();
                    cmd.CommandText = $"SELECT * FROM movie where id='{movieid}'";
                    SQLiteDataReader sr = cmd.ExecuteReader();
                    if (!sr.HasRows) return null;
                    try
                    {
                        while (sr.Read())
                        {
                            result = (Movie)GetDetailMovieFromSQLiteDataReader(sr);
                        }
                    }
                    catch(Exception e) { Console.WriteLine(e.Message); }
                    finally
                    { sr.Close(); }
                    
                    return result;
                }
            }


        }

        /// <summary>
        /// 加载影片详细信息
        /// </summary>
        /// <param name="movieid"></param>
        /// <returns></returns>
        public static DetailMovie SelectDetailMovieById(string movieid)
        {
            Init();
            using (SQLiteConnection cn = new SQLiteConnection("data source=" + Path))
            {
                cn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    cmd.Connection = cn;
                    DetailMovie result = new DetailMovie();
                    if (!string.IsNullOrEmpty(movieid))
                    {
                        //加载信息
                        cmd.CommandText = $"SELECT * FROM movie where id ='{movieid}'";
                        SQLiteDataReader sr = cmd.ExecuteReader();
                        if (sr != null && cn.State == ConnectionState.Open)
                        {
                                while (sr.Read())
                                {
                                    result = GetDetailMovieFromSQLiteDataReader(sr);
                                }

                            sr.Close();

                        }
                    }

                    if (!string.IsNullOrEmpty(result.id))
                    {
                        foreach (var item in result.genre.Split(' ')) { if (!string.IsNullOrEmpty(item) && item.IndexOf(' ') < 0) { result.genrelist.Add(item); } }
                        foreach (var item in result.label.Split(' ')) { if (!string.IsNullOrEmpty(item) && item.IndexOf(' ') < 0) { result.labellist.Add(item); } }

                        if (result.actor.Split(actorSplitDict[result.vediotype]).Count() == result.actorid.Split(actorSplitDict[result.vediotype]).Count() && result.actor.Split(actorSplitDict[result.vediotype]).Count() > 1)
                        {
                            //演员数目>1
                            string[] Name = result.actor.Split(actorSplitDict[result.vediotype]);
                            for (int i = 0; i < Name.Count(); i++)
                            {
                                if (!string.IsNullOrEmpty(Name[i]))
                                {
                                    Actress actress = new Actress() { id = "", name = Name[i] };
                                    result.actorlist.Add(actress);
                                }
                            }
                        }
                        else
                        {
                            //演员数目<=1
                            foreach (var item in result.actor.Split(actorSplitDict[result.vediotype]))
                            {
                                if (!string.IsNullOrEmpty(item))
                                {
                                    Actress actress = new Actress() { id = "", name = item };
                                    result.actorlist.Add(actress);
                                }
                            }
                        }
                    }
                    return result;


                }
            }

            
        }

        /// <summary>
        /// 读取演员信息
        /// </summary>
        /// <param name="actress"></param>
        /// <returns></returns>

        public static Actress SelectInfoFromActress(Actress actress)
        {
            Init();
            using (SQLiteConnection cn = new SQLiteConnection("data source=" + Path))
            {
                cn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    cmd.Connection = cn;

                    if (actress.name == "") { return actress; }
                    cmd.CommandText = $"select * from actress where name='{actress.name}'";
                    SQLiteDataReader sr = cmd.ExecuteReader();
                    while (sr.Read())
                    {
                        int v1, v2, v3, v4, v5 = 0;
                        actress.birthday = sr["birthday"].ToString();
                        int.TryParse(sr["age"].ToString(), out v1); actress.age = v1;
                        int.TryParse(sr["height"].ToString(), out v2); actress.height = v2;
                        int.TryParse(sr["chest"].ToString(), out v3); actress.chest = v3;
                        int.TryParse(sr["waist"].ToString(), out v4); actress.waist = v4;
                        int.TryParse(sr["hipline"].ToString(), out v5); actress.hipline = v5;

                        actress.cup = sr["cup"].ToString();
                        actress.birthplace = sr["birthplace"].ToString();
                        actress.hobby = sr["hobby"].ToString();
                        actress.source = sr["source"].ToString();
                        actress.sourceurl = sr["sourceurl"].ToString();
                        actress.imageurl = sr["imageurl"].ToString();
                        break;
                    }
                    sr.Close();
                    return actress;
                }
            }


            
        }

        /// <summary>
        /// 表是否存在
        /// </summary>
        /// <param name="table">表</param>
        /// <returns></returns>
        public static bool IsTableExist(string table)
        {
            Init();
            using (SQLiteConnection cn = new SQLiteConnection("data source=" + Path))
            {
                cn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    cmd.Connection = cn;

                    bool result = true;
                    try
                    {
                        string sqltext = $"select * from {table}";
                        cmd.CommandText = sqltext;
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception e) { Console.WriteLine(e.Message); result = false; }
                    return result;
                }
            }


            
        }

        public static bool IsMovieExist(string id)
        {
            Init();
            using (SQLiteConnection cn = new SQLiteConnection("data source=" + Path))
            {
                cn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    cmd.Connection = cn;

                    bool result = true;
                    try
                    {
                        string sqltext = $"select * from movie where id='{id.ToUpper()}'";
                        cmd.CommandText = sqltext;
                        SQLiteDataReader sr = cmd.ExecuteReader();
                        if (sr.HasRows) 
                            result = true;
                        else
                            result = false;
                        sr.Close();
                    }
                    catch (Exception e) { Console.WriteLine(e.Message); result = false; }
                    return result;
                }
            }



        }

        /// <summary>
        /// 执行数据库命令：select from where
        /// </summary>
        /// <param name="info"></param>
        /// <param name="table"></param>
        /// <param name="field"></param>
        /// <param name="fieldvalue"></param>
        /// <returns></returns>
        public static string SelectInfoByID(string info, string table,string id)
        {
            Init();
            using (SQLiteConnection cn = new SQLiteConnection("data source=" + Path))
            {
                cn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    cmd.Connection = cn;

                    string result = "";
                    string sqltext = $"select {info} from {table} where id ='{id}'";
                    cmd.CommandText = sqltext;
                    SQLiteDataReader sr = cmd.ExecuteReader();
                    if (sr != null)
                    {
                        while (sr.Read())
                        {
                            result = sr[0].ToString();
                        }
                    }
                    sr?.Close();
                    return result;
                }
            }



            

        }


        #endregion

        #region "DELETE"





        //删除
        public static void DelInfoByType(string table, string type, string value)
        {
            Init();
            using (SQLiteConnection cn = new SQLiteConnection("data source=" + Path))
            {
                cn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    cmd.Connection = cn;
                    cmd.CommandText = $"delete from {table} where {type} = '{value}'"; ;
                    cmd.ExecuteNonQuery();
                }
            }



            
        }

        #endregion


        #region "UPDATE"

        /// <summary>
        /// 保存单个信息到数据库
        /// </summary>
        /// <param name="id"></param>
        /// <param name="content"></param>
        /// <param name="value"></param>
        /// <param name="savetype"></param>
        public static void UpdateMovieByID(string id, string content, object value, string savetype = "Int")
        {
            Init();
            using (SQLiteConnection cn = new SQLiteConnection("data source=" + Path))
            {
                cn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    cmd.Connection = cn;
                    string sqltext;
                    if (savetype == "Int") { sqltext = $"UPDATE movie SET {content} = {value} WHERE id = '{id}'"; }
                    else { sqltext = $"UPDATE movie SET {content} = '{value}' WHERE id = '{id}'"; }
                    cmd.CommandText = sqltext;
                    cmd.ExecuteNonQuery();

                }
            }



            
        }


        /// <summary>
        /// 插入下载的信息
        /// </summary>
        /// <param name="info"></param>
        /// <param name="webSite"></param>
        public static void UpdateInfoFromNet(Dictionary<string, string> info, WebSite webSite)
        {
            Init();
            using (SQLiteConnection cn = new SQLiteConnection("data source=" + Path))
            {
                cn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    cmd.Connection = cn;
                    if (info == null) { return; }
                    if (info.ContainsKey("id") && info["id"] == null) { return; }
                    string sqltext;
                    if (webSite == WebSite.FC2Club)
                    {
                        sqltext = "update movie SET title=@title,releasedate=@releasedate,director=@director,genre=@genre,actor=@actor,studio=@studio," +
                           $"year=@year,bigimageurl=@bigimageurl ,smallimageurl=@smallimageurl,extraimageurl=@extraimageurl,rating=@rating,vediotype=@vediotype,otherinfo=@otherinfo,sourceurl=@sourceurl,source=@source where id ='{info["id"] }'";
                        cmd.CommandText = sqltext;
                        cmd.Parameters.Add("rating", DbType.Int32).Value = int.Parse(info.ContainsKey("rating") ? info["rating"] : "0");
                        cmd.Parameters.Add("vediotype", DbType.Int32).Value = int.Parse(info.ContainsKey("vediotype") ? info["vediotype"] : "1");
                        cmd.Parameters.Add("otherinfo", DbType.String).Value = info.ContainsKey("otherinfo") ? info["otherinfo"] : "";

                    }
                    else if (webSite == WebSite.DB)
                    {
                        sqltext = $"update movie SET title=@title ,  releasedate=@releasedate , director=@director  , genre=@genre  , tag=@tag , actor=@actor , studio=@studio ,year=@year  , runtime=@runtime,bigimageurl=@bigimageurl ,smallimageurl=@smallimageurl ,extraimageurl=@extraimageurl,rating=@rating,sourceurl=@sourceurl,source=@source where id ='{info["id"] }'";
                        cmd.CommandText = sqltext;
                        cmd.Parameters.Add("tag", DbType.String).Value = info.ContainsKey("tag") ? info["tag"] : "";
                        cmd.Parameters.Add("runtime", DbType.Int32).Value = int.Parse(info.ContainsKey("runtime") ? info["runtime"] : "0");
                        cmd.Parameters.Add("rating", DbType.Int32).Value = int.Parse(info.ContainsKey("rating") ? info["rating"] : "0");
                    }
                    else if (webSite == WebSite.Library)
                    {
                        sqltext = $"update movie SET title=@title ,  releasedate=@releasedate , director=@director  , genre=@genre  , actor=@actor , studio=@studio ,year=@year  , runtime=@runtime,smallimageurl=@smallimageurl,bigimageurl=@bigimageurl ,extraimageurl=@extraimageurl,sourceurl=@sourceurl,source=@source where id ='{info["id"] }'";
                        cmd.CommandText = sqltext;
                        cmd.Parameters.Add("runtime", DbType.Int32).Value = int.Parse(info.ContainsKey("runtime") ? info["runtime"] : "0");
                        cmd.Parameters.Add("rating", DbType.Int32).Value = int.Parse(info.ContainsKey("rating") ? info["rating"] : "0");
                    }
                    else if (webSite == WebSite.Bus)
                    {
                        //Bus
                        sqltext = $"update movie SET title=@title ,  releasedate=@releasedate , director=@director  ,actorid=@actorid, tag=@tag, genre=@genre  , actor=@actor , studio=@studio ,year=@year  , runtime=@runtime,bigimageurl=@bigimageurl ,smallimageurl=@smallimageurl ,extraimageurl=@extraimageurl,actressimageurl=@actressimageurl,sourceurl=@sourceurl,source=@source where id ='{info["id"] }'";
                        cmd.CommandText = sqltext;
                        cmd.Parameters.Add("actorid", DbType.String).Value = info.ContainsKey("actorid") ? info["actorid"] : "";
                        cmd.Parameters.Add("tag", DbType.String).Value = info.ContainsKey("tag") ? info["tag"] : "";
                        cmd.Parameters.Add("runtime", DbType.Int32).Value = int.Parse(info.ContainsKey("runtime") ? info["runtime"] : "0");
                        cmd.Parameters.Add("rating", DbType.Int32).Value = int.Parse(info.ContainsKey("rating") ? info["rating"] : "0");
                        cmd.Parameters.Add("actressimageurl", DbType.String).Value = info.ContainsKey("actressimageurl") ? info["actressimageurl"] : "";
                    }
                    else if (webSite == WebSite.BusEu)
                    {
                        //BusEu
                        sqltext = $"update movie SET title=@title ,  releasedate=@releasedate , director=@director  ,actorid=@actorid, tag=@tag, genre=@genre  , actor=@actor , studio=@studio ,year=@year  , runtime=@runtime,bigimageurl=@bigimageurl ,smallimageurl=@smallimageurl ,extraimageurl=@extraimageurl,actressimageurl=@actressimageurl,sourceurl=@sourceurl,source=@source where id ='{info["id"] }'";
                        cmd.CommandText = sqltext;
                        cmd.Parameters.Add("actorid", DbType.String).Value = info.ContainsKey("actorid") ? info["actorid"] : "";
                        cmd.Parameters.Add("tag", DbType.String).Value = info.ContainsKey("tag") ? info["tag"] : "";
                        cmd.Parameters.Add("runtime", DbType.Int32).Value = int.Parse(info.ContainsKey("runtime") ? info["runtime"] : "0");
                        cmd.Parameters.Add("rating", DbType.Int32).Value = int.Parse(info.ContainsKey("rating") ? info["rating"] : "0");
                        cmd.Parameters.Add("actressimageurl", DbType.String).Value = info.ContainsKey("actressimageurl") ? info["actressimageurl"] : "";
                    }
                    cmd.Parameters.Add("title", DbType.String).Value = info.ContainsKey("title") ? info["title"] : "";
                    cmd.Parameters.Add("releasedate", DbType.String).Value = info.ContainsKey("releasedate") ? info["releasedate"] : "1970-01-01";
                    cmd.Parameters.Add("director", DbType.String).Value = info.ContainsKey("director") ? info["director"] : "";
                    cmd.Parameters.Add("genre", DbType.String).Value = info.ContainsKey("genre") ? info["genre"] : "";
                    cmd.Parameters.Add("actor", DbType.String).Value = info.ContainsKey("actor") ? info["actor"] : "";
                    cmd.Parameters.Add("studio", DbType.String).Value = info.ContainsKey("studio") ? info["studio"] : "";
                    cmd.Parameters.Add("year", DbType.Int32).Value = int.Parse(info.ContainsKey("year") ? info["year"] : "1970");
                    cmd.Parameters.Add("extraimageurl", DbType.String).Value = info.ContainsKey("extraimageurl") ? info["extraimageurl"] : "";
                    cmd.Parameters.Add("sourceurl", DbType.String).Value = info.ContainsKey("sourceurl") ? info["sourceurl"] : "";
                    cmd.Parameters.Add("source", DbType.String).Value = info.ContainsKey("source") ? info["source"] : "";
                    cmd.Parameters.Add("bigimageurl", DbType.String).Value = info.ContainsKey("bigimageurl") ? info["bigimageurl"] : "";
                    cmd.Parameters.Add("smallimageurl", DbType.String).Value = info.ContainsKey("smallimageurl") ? info["smallimageurl"] : "";

                    cmd.ExecuteNonQuery();

                }
            }



           
        }

        #endregion


        #region "INSERT"


        /// <summary>
        /// 插入 完整 的数据
        /// </summary>
        /// <param name="movie"></param>
        public static void InsertFullMovie(Movie movie)
        {
            Init();
            using (SQLiteConnection cn = new SQLiteConnection("data source=" + Path))
            {
                cn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    cmd.Connection = cn;
                    if (movie.vediotype == 0) return;
                    //将 id中的 FC2PPV 替换成 FC2
                    movie.id = movie.id.Replace("FC2PPV", "FC2");

                    string sqltext = "INSERT INTO movie(id , title,filesize ,filepath ,subsection , vediotype  ,scandate,releasedate,visits,director,genre,tag, actor, studio ,rating,chinesetitle,favorites, label ,plot,outline,year ,runtime  ,country,countrycode,otherinfo,extraimageurl) " +
                        "values(@id , @title ,@filesize ,@filepath ,@subsection, @vediotype  ,@scandate,@releasedate,@visits,@director,@genre,@tag,@actor,@studio,@rating,@chinesetitle,@favorites,@label,@plot,@outline,@year,@runtime ,@country,@countrycode,@otherinfo,@extraimageurl) " +
                        "ON CONFLICT(id) DO UPDATE SET title=@title,filesize=@filesize,filepath=@filepath,subsection=@subsection,vediotype=@vediotype,scandate=@scandate,releasedate=@releasedate,visits=@visits,director=@director,genre=@genre,tag=@tag," +
                        "actor=@actor,studio=@studio,rating=@rating,chinesetitle=@chinesetitle,favorites=@favorites,label=@label,plot=@plot,outline=@outline,year=@year,runtime=@runtime,country=@country,countrycode=@countrycode,otherinfo=@otherinfo,extraimageurl=@extraimageurl";
                    cmd.CommandText = sqltext;
                    cmd.Parameters.Add("id", DbType.String).Value = movie.id;
                    cmd.Parameters.Add("title", DbType.String).Value = movie.title;
                    cmd.Parameters.Add("filesize", DbType.Double).Value = movie.filesize;
                    cmd.Parameters.Add("filepath", DbType.String).Value = movie.filepath;
                    cmd.Parameters.Add("subsection", DbType.String).Value = movie.subsection;
                    cmd.Parameters.Add("vediotype", DbType.Int16).Value = movie.vediotype;
                    cmd.Parameters.Add("scandate", DbType.String).Value = movie.scandate;
                    cmd.Parameters.Add("releasedate", DbType.String).Value = movie.releasedate;
                    cmd.Parameters.Add("visits", DbType.Int16).Value = movie.visits;
                    cmd.Parameters.Add("director", DbType.String).Value = movie.director;
                    cmd.Parameters.Add("genre", DbType.String).Value = movie.genre;
                    cmd.Parameters.Add("tag", DbType.String).Value = movie.tag;
                    cmd.Parameters.Add("actor", DbType.String).Value = movie.actor;
                    cmd.Parameters.Add("actorid", DbType.String).Value = movie.actorid;
                    cmd.Parameters.Add("studio", DbType.String).Value = movie.studio;
                    cmd.Parameters.Add("rating", DbType.Double).Value = movie.rating;
                    cmd.Parameters.Add("chinesetitle", DbType.String).Value = movie.chinesetitle;
                    cmd.Parameters.Add("favorites", DbType.Int16).Value = movie.favorites;
                    cmd.Parameters.Add("label", DbType.String).Value = movie.label;
                    cmd.Parameters.Add("plot", DbType.String).Value = movie.plot;
                    cmd.Parameters.Add("outline", DbType.String).Value = movie.outline;
                    cmd.Parameters.Add("year", DbType.Int16).Value = movie.year;
                    cmd.Parameters.Add("runtime", DbType.Int16).Value = movie.runtime;
                    cmd.Parameters.Add("country", DbType.String).Value = movie.country;
                    cmd.Parameters.Add("countrycode", DbType.Int16).Value = movie.countrycode;
                    cmd.Parameters.Add("otherinfo", DbType.String).Value = movie.otherinfo;
                    cmd.Parameters.Add("sourceurl", DbType.String).Value = movie.sourceurl;
                    cmd.Parameters.Add("source", DbType.String).Value = movie.source;
                    cmd.Parameters.Add("extraimageurl", DbType.String).Value = movie.extraimageurl;
                    cmd.ExecuteNonQuery();

                }
            }



            
        }

        /// <summary>
        /// 插入扫描的数据
        /// </summary>
        /// <param name="movie"></param>
        public static void InsertScanMovie(Movie movie)
        {
            Init();
            using (SQLiteConnection cn = new SQLiteConnection("data source=" + Path))
            {
                cn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    cmd.Connection = cn;

                    string sqltext = "INSERT INTO movie(id , filesize ,filepath  , vediotype  ,scandate,subsection) values(@id , @filesize ,@filepath  , @vediotype  ,@scandate,@subsection) ON CONFLICT(id) DO UPDATE SET filesize=@filesize,filepath=@filepath,scandate=@scandate,vediotype=@vediotype,subsection=@subsection";
                    cmd.CommandText = sqltext;
                    cmd.Parameters.Add("id", DbType.String).Value = movie.id.ToUpper();
                    cmd.Parameters.Add("filesize", DbType.Double).Value = movie.filesize;
                    cmd.Parameters.Add("filepath", DbType.String).Value = movie.filepath;
                    cmd.Parameters.Add("vediotype", DbType.Int16).Value = movie.vediotype;
                    cmd.Parameters.Add("scandate", DbType.String).Value = movie.scandate;
                    cmd.Parameters.Add("subsection", DbType.String).Value = movie.subsection;
                    //Console.WriteLine(movie.subsection);
                    cmd.ExecuteNonQuery();
                }
            }



         
        }

        public static void SaveMovieCodeByID(string id, string table, string code)
        {
            Init();
            using (SQLiteConnection cn = new SQLiteConnection("data source=" + Path))
            {
                cn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    cmd.Connection = cn;

                    cmd.CommandText = $"insert into  {table}(id,code) values(@id,@code) ON CONFLICT(id) DO UPDATE SET code=@code";
                    cmd.Parameters.Add("id", DbType.String).Value = id;
                    cmd.Parameters.Add("code", DbType.String).Value = code;
                    cmd.ExecuteNonQuery();
                }
            }



         
        }


        public static void InsertActress(Actress actress)
        {
            Init();
            using (SQLiteConnection cn = new SQLiteConnection("data source=" + Path))
            {
                cn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    cmd.Connection = cn;
                    if (actress == null) { return; }
                    if (string.IsNullOrEmpty(actress.name)) { return; }
                    string sqltext = $"insert into actress(id,name ,  birthday , age ,height, cup, chest  , waist , hipline ,birthplace  , hobby,sourceurl ,source,imageurl) values(@id,@name ,  @birthday , @age ,@height, @cup, @chest  , @waist , @hipline ,@birthplace  , @hobby,@sourceurl ,@source,@imageurl) on conflict(id) do update set name=@name ,  birthday=@birthday , age=@age  ,height=@height, cup=@cup, chest=@chest  , waist=@waist , hipline=@hipline ,birthplace=@birthplace  , hobby=@hobby,sourceurl=@sourceurl ,source=@source,imageurl=@imageurl";
                    cmd.CommandText = sqltext;
                    cmd.Parameters.Add("id", DbType.String).Value = actress.id;
                    cmd.Parameters.Add("name", DbType.String).Value = actress.name;
                    cmd.Parameters.Add("birthday", DbType.String).Value = actress.birthday;
                    cmd.Parameters.Add("age", DbType.Int32).Value = actress.age;
                    cmd.Parameters.Add("height", DbType.Int32).Value = actress.height;
                    cmd.Parameters.Add("cup", DbType.String).Value = actress.cup;
                    cmd.Parameters.Add("chest", DbType.Int32).Value = actress.chest;
                    cmd.Parameters.Add("waist", DbType.Int32).Value = actress.waist;
                    cmd.Parameters.Add("hipline", DbType.Int32).Value = actress.hipline;
                    cmd.Parameters.Add("birthplace", DbType.String).Value = actress.birthplace;
                    cmd.Parameters.Add("hobby", DbType.String).Value = actress.hobby;
                    cmd.Parameters.Add("sourceurl", DbType.String).Value = actress.sourceurl;
                    cmd.Parameters.Add("source", DbType.String).Value = actress.source;
                    cmd.Parameters.Add("imageurl", DbType.String).Value = actress.imageurl;
                    cmd.ExecuteNonQuery();

                }
            }



           
        }




        #endregion

        #region "Other Command"


        /// <summary>
        /// 组合搜索，获得所有筛选值
        /// </summary>
        /// <returns></returns>
        public static List<List<string>> GetAllFilter()
        {
            Init();
            using (SQLiteConnection cn = new SQLiteConnection("data source=" + Path))
            {
                cn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    cmd.Connection = cn;

                    //年份
                    cmd.CommandText = "SELECT releasedate FROM movie";
                    string year = "1900";
                    SQLiteDataReader sr = cmd.ExecuteReader();
                    List<string> Year = new List<string>();
                    while (sr.Read())
                    {
                        string date = sr[0].ToString();
                        try { year = Regex.Match(date, @"\d{4}").Value; }
                        catch { }
                        if (year == "0000") year = "1900";
                        if (!Year.Contains(year)) Year.Add(year);
                    }
                    sr.Close();
                    Year.Sort();

                    //类别
                    cmd.CommandText = "SELECT genre FROM movie";
                    sr = cmd.ExecuteReader();
                    List<string> Genre = new List<string>();
                    while (sr.Read())
                    {
                        sr[0].ToString().Split(' ').ToList().ForEach(arg =>
                        {
                            if (arg.Length > 0 & arg.IndexOf(' ') < 0)
                                if (!Genre.Contains(arg)) Genre.Add(arg);
                        });
                    }
                    sr.Close();
                    Genre.Sort();

                    //演员
                    cmd.CommandText = "SELECT actor,vediotype FROM movie";
                    sr = cmd.ExecuteReader();
                    List<string> Actor = new List<string>();
                    while (sr.Read())
                    {
                        int vt = 0;
                        int.TryParse(sr[1].ToString(), out vt);
                        sr[0].ToString().Split(actorSplitDict[vt]).ToList().ForEach(arg =>
                        {
                            if (arg.Length > 0 & arg.IndexOf(' ') < 0)
                                if (!Actor.Contains(arg)) Actor.Add(arg);
                        });
                    }
                    sr.Close();
                    Actor.Sort();

                    //标签
                    cmd.CommandText = "SELECT label FROM movie";
                    sr = cmd.ExecuteReader();
                    List<string> Label = new List<string>();
                    while (sr.Read())
                    {
                        sr[0].ToString().Split(' ').ToList().ForEach(arg =>
                        {
                            if (arg.Length > 0 & arg.IndexOf(' ') < 0)
                                if (!Label.Contains(arg)) Label.Add(arg);
                        });
                    }
                    sr.Close();
                    Label.Sort();

                    //时长
                    //45 90 120
                    cmd.CommandText = "SELECT runtime FROM movie";
                    sr = cmd.ExecuteReader();
                    int runtime = 0;
                    List<string> Runtime = new List<string>();
                    while (sr.Read())
                    {
                        if (Runtime.Count >= 4) break;
                        int.TryParse(sr[0].ToString(), out runtime);
                        if (runtime <= 45)
                        {
                            if (!Runtime.Contains("0-45")) Runtime.Add("0-45");
                        }

                        else if (runtime >= 45 & runtime <= 90)
                        {
                            if (!Runtime.Contains("45-90")) Runtime.Add("45-90");
                        }

                        else if (runtime >= 90 & runtime <= 120)
                        {
                            if (!Runtime.Contains("90-120")) Runtime.Add("90-120");
                        }

                        else if (runtime >= 120)
                        {
                            if (!Runtime.Contains("120-999")) Runtime.Add("120-999");
                        }

                    }
                    sr.Close();
                    Runtime.Sort();

                    //文件大小
                    //1 2 3 
                    cmd.CommandText = "SELECT filesize FROM movie";
                    sr = cmd.ExecuteReader();
                    List<string> FileSize = new List<string>();
                    while (sr.Read())
                    {
                        if (FileSize.Count >= 4) break;
                        double filesize = 0;
                        double.TryParse(sr[0].ToString(), out filesize);
                        filesize = filesize / 1073741824D;// B to GB
                        if (filesize <= 1)
                        {
                            if (!FileSize.Contains("0-1")) FileSize.Add("0-1");
                        }

                        else if (filesize >= 1 & filesize <= 2)
                        {
                            if (!FileSize.Contains("1-2")) FileSize.Add("1-2");
                        }

                        else if (filesize >= 2 & filesize <= 3)
                        {
                            if (!FileSize.Contains("2-3")) FileSize.Add("2-3");
                        }

                        else if (filesize >= 3)
                        {
                            if (!FileSize.Contains("3-999")) FileSize.Add("3-999");
                        }

                    }
                    sr.Close();
                    FileSize.Sort();


                    //评分
                    //20 40 60 80 
                    cmd.CommandText = "SELECT rating FROM movie";
                    sr = cmd.ExecuteReader();
                    List<string> Rating = new List<string>();
                    while (sr.Read())
                    {
                        if (Rating.Count >= 4) break;
                        double rating = 0;
                        double.TryParse(sr[0].ToString(), out rating);
                        if (rating <= 20)
                        {
                            if (!Rating.Contains("0-20")) Rating.Add("0-20");
                        }

                        else if (rating >= 20 & rating <= 40)
                        {
                            if (!Rating.Contains("20-40")) Rating.Add("20-40");
                        }

                        else if (rating >= 40 & rating <= 60)
                        {
                            if (!Rating.Contains("40-60")) Rating.Add("40-60");
                        }

                        else if (rating >= 60 & rating <= 80)
                        {
                            if (!Rating.Contains("60-80")) Rating.Add("60-80");
                        }

                        else if (rating >= 80)
                        {
                            Console.WriteLine(rating);
                            if (!Rating.Contains("80-100")) Rating.Add("80-100");
                        }

                    }
                    sr.Close();
                    Rating.Sort();

                    List<List<string>> result = new List<List<string>>();

                    result.Add(Year);
                    result.Add(Genre);
                    result.Add(Actor);
                    result.Add(Label);
                    result.Add(Runtime);
                    result.Add(FileSize);
                    result.Add(Rating);
                    return result;

                }
            }



         
        }

        /// <summary>
        /// 读取 Access 到 SQlite
        /// </summary>
        /// <param name="path"></param>
        public static void InsertFromAccess(string path = "")
        {
            Init();
            using (SQLiteConnection cn = new SQLiteConnection("data source=" + Path))
            {
                cn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    cmd.Connection = cn;
                    //从Access中读取
                    string dbpath = AppDomain.CurrentDomain.BaseDirectory + "\\mdb\\MainDatabase.mdb";
                    if (path != "") { dbpath = path; }
                    if (!File.Exists(dbpath)) { return; }
                    OleDbConnection con = new OleDbConnection();
                    OleDbCommand OleDbcmd = new OleDbCommand();
                    OleDbDataReader sr;
                    con.ConnectionString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + dbpath + ";Mode=Read";
                    con.Open();
                    OleDbcmd.Connection = con;
                    OleDbcmd.CommandText = "select * from Main";
                    sr = OleDbcmd.ExecuteReader();

                    while (sr.Read())
                    {
                        try
                        {
                            Movie AccessMovie = new Movie()
                            {
                                id = sr["fanhao"].ToString(),
                                title = Unicode2String(sr["mingcheng"].ToString()),
                                filesize = string.IsNullOrEmpty(sr["wenjiandaxiao"].ToString()) ? 0 : double.Parse(sr["wenjiandaxiao"].ToString()),
                                filepath = sr["weizhi"].ToString(),
                                vediotype = string.IsNullOrEmpty(sr["shipinleixing"].ToString()) ? 0 : int.Parse(sr["shipinleixing"].ToString()),
                                scandate = string.IsNullOrEmpty(sr["daorushijian"].ToString()) ? "1900-01-01 01:01:01" : sr["daorushijian"].ToString(),
                                releasedate = string.IsNullOrEmpty(sr["faxingriqi"].ToString()) ? "1900-01-01" : sr["faxingriqi"].ToString(),
                                visits = string.IsNullOrEmpty(sr["fangwencishu"].ToString()) ? 0 : int.Parse(sr["fangwencishu"].ToString()),
                                director = Unicode2String(sr["daoyan"].ToString()),
                                genre = Unicode2String(sr["leibie"].ToString()),
                                tag = Unicode2String(sr["xilie"].ToString()),
                                actor = Unicode2String(sr["yanyuan"].ToString()),
                                actorid = "",
                                sourceurl = "",
                                source = "",
                                studio = Unicode2String(sr["faxingshang"].ToString()),
                                favorites = string.IsNullOrEmpty(sr["love"].ToString()) ? 0 : int.Parse(sr["love"].ToString()),
                                label = Unicode2String(sr["biaoqian"].ToString()),
                                runtime = string.IsNullOrEmpty(sr["changdu"].ToString()) ? 0 : int.Parse(sr["changdu"].ToString()),
                                rating = 0,
                                year = string.IsNullOrEmpty(sr["faxingriqi"].ToString()) ? 1900 : int.Parse(sr["faxingriqi"].ToString().Split('-')[0]),
                                countrycode = 0,
                                otherinfo = "",
                                subsection = "",
                                chinesetitle = "",
                                plot = "",
                                outline = "",
                                country = ""
                            };
                            InsertFullMovie(AccessMovie); //导入到 Sqlite 中
                        }
                        catch (Exception e)
                        {
                            Logger.LogD(e);
                            continue;
                        }
                    }
                    sr.Close();

                }
            }



       
        }

        public static string GetInfoBySql(string sql)
        {
            Init();
            using (SQLiteConnection cn = new SQLiteConnection("data source=" + Path))
            {
                cn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    cmd.Connection = cn;
                    string result = "";
                    cmd.CommandText = sql;
                    SQLiteDataReader sr = cmd.ExecuteReader();
                    while (sr.Read())
                    {
                        if (sr[0] != null) result = sr[0].ToString();
                        if (result != "") { break; }
                    }
                    sr.Close();
                    return result;

                }
            }



          
        }

        #endregion


    }



}
