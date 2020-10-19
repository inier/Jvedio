using DynamicData.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using static Jvedio.StaticClass;
using static Jvedio.StaticVariable;

//https://www.cnblogs.com/leemano/p/6578050.html

namespace Jvedio
{

    //数据库全部异步，加锁判断

    /// <summary>
    /// 该类使用静态类，一次只能访问一个数据库文件的一个表
    /// </summary>

    public class DataBase
    {
        
        public static string Path { get; set; }
        private SQLiteCommand cmd;
        private SQLiteConnection cn;
        public string dbName;
        public object LockObject; 


        public DataBase(string DatabaseName = "", bool absolutPath = false)
        {
            LockObject = new object();
            this.dbName = DatabaseName;
            if (!LockDataBase.Contains(DatabaseName))
            {
                LockDataBase.Add(DatabaseName);
            }
            else
            {
                //IsLock = true;//如果列表包含该数据库，说明数据库并未关闭
            }
                


            if (DatabaseName == "") { Path = Properties.Settings.Default.DataBasePath; }
            else {
                if (absolutPath)
                    Path = DatabaseName;
                else
                    Path = AppDomain.CurrentDomain.BaseDirectory + DatabaseName + ".sqlite";
            }

            cn = new SQLiteConnection("data source=" + Path);
            cn.Open();
            cmd = new SQLiteCommand();
            cmd.Connection = cn;
        }


        public void CloseDB()
        {
            if (LockDataBase.Contains(dbName)) { LockDataBase.Remove(dbName); }
            cn?.Close();
        }


        /// <summary>
        /// VACCUM
        /// </summary>
        public void Vacuum()
        {
            //if (IsLock) return;
            cmd.CommandText = "vacuum;";
            cmd.ExecuteNonQuery();
        }


        /// <summary>
        /// 删除表
        /// </summary>
        /// <param name="tablename"></param>
        public void DeleteTable(string tablename)
        {
            //if (IsLock) return;
            cmd.CommandText = $"DROP TABLE IF EXISTS {tablename}";
            cmd.ExecuteNonQuery();
        }



        /// <summary>
        /// 获得表总行数
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public int GetMaxCountByTable(string table)
        {
            //if (IsLock) return 0;
            int result = 0;
            cmd.CommandText = $"SELECT MAX(_ROWID_) FROM '{table}' LIMIT 1;";
            SQLiteDataReader sr = cmd.ExecuteReader();
            string maxcount = "0";

            while (sr.Read())
            { maxcount = sr[0].ToString(); }

            sr.Close();
            int.TryParse(maxcount, out result);
            return result;
        }


        #region "CREATE"

        /// <summary>
        /// 创建表
        /// </summary>
        /// <param name="sqltext"></param>
        public void CreateTable(string sqltext)
        {
            cmd.CommandText = sqltext;
            cmd.ExecuteNonQuery();
        }


        #endregion

        #region "SELECT"

        public async Task<double> SelectCountBySql(string sql)
        {
            return await Task.Run(() => {
            double result = 0;
            if (sql == "")
            {
                cmd.CommandText = "SELECT count(id) FROM movie";
            }
            else
            {
                cmd.CommandText = "SELECT count(id) FROM movie " + sql;
            }
            SQLiteDataReader sr = cmd.ExecuteReader();
            while (sr.Read())
            {
                double.TryParse(sr[0].ToString(), out result);
            }
            sr.Close();

            return result;
            });
        }





        /// <summary>
        /// 获得标签，如：奇葩( 20 )
        /// </summary>
        /// <param name="vediotype"></param>
        /// <returns></returns>

        public List<string> SelectLabelByVedioType(VedioType vediotype)
        {
            //if (IsLock) return new List<string>();
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

        /// <summary>
        /// 获得演员信息
        /// </summary>
        /// <param name="vediotype"></param>
        /// <returns></returns>

        public List<Actress> SelectActorByVedioType(VedioType vediotype)
        {
            //if (IsLock) return new List<Actress>();
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
                actress = SelectInfoFromActress(actress);//获得演员所有信息
                result.Add(actress);
            }

            return result;
        }


        /// <summary>
        /// 获得类别列表
        /// </summary>
        /// <param name="vediotype"></param>
        /// <returns></returns>
        public async Task< List<Genre>> SelectGenreByVedioType(VedioType vediotype)
        {
            return await Task.Run(() => {

                //if (IsLock) return new List<Genre>();

                if (vediotype == VedioType.所有)
                cmd.CommandText = "SELECT genre FROM movie where vediotype>=0";
            else
                cmd.CommandText = "SELECT genre FROM movie where vediotype=" + (int)vediotype;

            string[] genre;
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
            });
        }


        public DetailMovie GetDetailMovieFromSQLiteDataReader(SQLiteDataReader sr)
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
        public List<Movie> SelectMoviesBySql(string sqltext)
        {
            //if (IsLock) return new List<Movie>();
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


        /// <summary>
        /// 异步获得影片信息
        /// </summary>
        /// <param name="movieid"></param>
        /// <returns></returns>
        public async Task<List<Movie>> SelectMoviesById(string movieid)
        {
            return await Task.Run(() =>
            {
                List<Movie> result = new List<Movie>();
                //if (IsLock) return result;
                movieid = movieid.Replace("'", "").Replace("%", "");

                if (string.IsNullOrEmpty(movieid))
                    cmd.CommandText = "SELECT * FROM movie";
                else
                    cmd.CommandText = "SELECT * FROM movie where id like '%" + movieid + "%'";

                SQLiteDataReader sr = cmd.ExecuteReader();
                if (sr != null && cn.State == ConnectionState.Open)
                {
                    try
                    {
                        while (sr.Read())
                    {
                            Movie movie = (Movie)GetDetailMovieFromSQLiteDataReader(sr);
                            result.Add(movie);
                    }
                    }
                    catch { }
                }
                if (cn.State == ConnectionState.Open) sr?.Close();
                return result;
            });
        }


        /// <summary>
        /// 异步获得单个影片
        /// </summary>
        /// <param name="movieid"></param>
        /// <returns></returns>
        public async Task<Movie> SelectMovieByID(string movieid)
        {
            return await Task.Run(() => {
                Movie result = new Movie();
                //if (IsLock) return result;
                cmd.CommandText = $"SELECT * FROM movie where id='{movieid}'";
                SQLiteDataReader sr = cmd.ExecuteReader();
                try
                {
                    while (sr.Read())
                {

                        result = (Movie)GetDetailMovieFromSQLiteDataReader(sr);
                    }
                    
                    
                }
                catch { }
                sr.Close();
                return result;
            });
        }

        /// <summary>
        /// 加载影片详细信息
        /// </summary>
        /// <param name="movieid"></param>
        /// <returns></returns>
        public DetailMovie SelectDetailMovieById(string movieid)
        {
            DetailMovie result = new DetailMovie();
            //if (IsLock) return result;
            if (!string.IsNullOrEmpty(movieid))
            {
                //加载信息
                cmd.CommandText = $"SELECT * FROM movie where id ='{movieid}'";
                SQLiteDataReader sr = cmd.ExecuteReader();
                if (sr != null && cn.State == ConnectionState.Open)
                {
                    try
                    {
                        while (sr.Read())
                    {
                            result = GetDetailMovieFromSQLiteDataReader(sr);
                        }
                        
                        
                    }
                    catch { }
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
                        if (!string.IsNullOrEmpty(Name[i])  )
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
                        if (!string.IsNullOrEmpty(item) )
                        {
                            Actress actress = new Actress() { id = "", name = item };
                            result.actorlist.Add(actress);
                        }
                    }
                }
            }

            return result;

        }

        /// <summary>
        /// 读取演员信息
        /// </summary>
        /// <param name="actress"></param>
        /// <returns></returns>

        public Actress SelectInfoFromActress(Actress actress)
        {
            //if (IsLock) return actress;
            if (actress.name == "") { return actress; }
            cmd.CommandText = $"select * from actress where name='{actress.name}'";
            SQLiteDataReader sr = cmd.ExecuteReader();
            while (sr.Read())
            {
                int v1, v2,v3, v4, v5 = 0;
                actress.birthday = sr["birthday"].ToString();
                int.TryParse(sr["age"].ToString(),out v1); actress.age = v1;
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

        /// <summary>
        /// 表是否存在
        /// </summary>
        /// <param name="table">表</param>
        /// <returns></returns>
        public bool IsTableExist(string table)
        {
            bool result = true;
            try
            {
                string sqltext = $"select * from {table}";
                cmd.CommandText = sqltext;
                cmd.ExecuteNonQuery();
            }
            catch { result = false; }
            return result;
        }

        /// <summary>
        /// 执行数据库命令：select from where
        /// </summary>
        /// <param name="info"></param>
        /// <param name="table"></param>
        /// <param name="field"></param>
        /// <param name="fieldvalue"></param>
        /// <returns></returns>
        public string SelectInfoByID(string info, string table,string id)
        {
            //if (IsLock) return "";
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


        #endregion

        #region "DELETE"





        //删除
        public void DelInfoByType(string table, string type, string value)
        {
            //if (IsLock) return ;
            cmd.CommandText = $"delete from {table} where {type} = '{value}'"; ;
            cmd.ExecuteNonQuery();
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
        public void UpdateMovieByID(string id, string content, object value, string savetype = "Int")
        {
            //if (IsLock) return;
            string sqltext;
            if (savetype == "Int") { sqltext = $"UPDATE movie SET {content} = {value} WHERE id = '{id}'"; }
            else { sqltext = $"UPDATE movie SET {content} = '{value}' WHERE id = '{id}'"; }
            cmd.CommandText = sqltext;
            cmd.ExecuteNonQuery();
        }


        /// <summary>
        /// 插入下载的信息
        /// </summary>
        /// <param name="info"></param>
        /// <param name="webSite"></param>
        public void UpdateInfoFromNet(Dictionary<string, string> info, WebSite webSite)
        {
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

        #endregion


        #region "INSERT"


        /// <summary>
        /// 插入 完整 的数据
        /// </summary>
        /// <param name="movie"></param>
        public void InsertFullMovie(Movie movie)
        {
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

        /// <summary>
        /// 插入扫描的数据
        /// </summary>
        /// <param name="movie"></param>
        public void InsertScanMovie(Movie movie)
        {
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

        public void SaveMovieCodeByID(string id, string table, string code)
        {
            cmd.CommandText = $"insert into  {table}(id,code) values(@id,@code) ON CONFLICT(id) DO UPDATE SET code=@code";
            cmd.Parameters.Add("id", DbType.String).Value = id;
            cmd.Parameters.Add("code", DbType.String).Value = code;
            cmd.ExecuteNonQuery();
        }

        public void SaveBaiduAIByID(string id, Dictionary<string, string> dic)
        {
            cmd.CommandText = $"insert into  baidu (id,age,beauty,expression,face_shape,gender,glasses,race,emotion,mask) values(@id,@age,@beauty,@expression,@face_shape,@gender,@glasses,@race,@emotion,@mask) ON CONFLICT(id) DO UPDATE SET age=@age,beauty=@beauty,expression=@expression,face_shape=@face_shape,gender=@gender,glasses=@glasses,race=@race,emotion=@emotion,mask=@mask";
            cmd.Parameters.Add("id", DbType.String).Value = id;
            cmd.Parameters.Add("age", DbType.Int32).Value = dic["age"];
            cmd.Parameters.Add("beauty", DbType.Single).Value = dic["beauty"];
            cmd.Parameters.Add("expression", DbType.String).Value = dic["expression"];
            cmd.Parameters.Add("face_shape", DbType.String).Value = dic["face_shape"];
            cmd.Parameters.Add("gender", DbType.String).Value = dic["gender"];
            cmd.Parameters.Add("glasses", DbType.String).Value = dic["glasses"];
            cmd.Parameters.Add("race", DbType.String).Value = dic["race"];
            cmd.Parameters.Add("emotion", DbType.String).Value = dic["emotion"];
            cmd.Parameters.Add("mask", DbType.Boolean).Value = int.Parse(dic["mask"]) == 0 ? false : true;
            cmd.ExecuteNonQuery();
        }

        public void SaveYoudaoTranslateByID(string id, string value1, string value2, string type)
        {
            cmd.CommandText = $"insert into  youdao (id,{type},translate_{type}) values(@id,@{type},@translate_{type}) ON CONFLICT(id) DO UPDATE SET {type}=@{type},translate_{type}=@translate_{type}";
            cmd.Parameters.Add("id", DbType.String).Value = id;
            cmd.Parameters.Add(type, DbType.String).Value = value1;
            cmd.Parameters.Add($"translate_{type}", DbType.String).Value = value2;
            cmd.ExecuteNonQuery();
        }


        /// <summary>
        /// 插入从网上下载的演员信息
        /// </summary>
        /// <param name="info"></param>

        //public void InsertActressFromNet(Dictionary<string, string> info)
        //{
        //    if (info == null) { return; }
        //    if (info["name"] == null | info["id"] == null) { return; }
        //    string sqltext = $"insert into actress(id,name ,  birthday , age ,height, cup, chest  , waist , hipline ,birthplace  , hobby,sourceurl ,source,imageurl) values(@id,@name ,  @birthday , @age ,@height, @cup, @chest  , @waist , @hipline ,@birthplace  , @hobby,@sourceurl ,@source,@imageurl) on conflict(id) do update set name=@name ,  birthday=@birthday , age=@age  ,height=@height, cup=@cup, chest=@chest  , waist=@waist , hipline=@hipline ,birthplace=@birthplace  , hobby=@hobby,sourceurl=@sourceurl ,source=@source,imageurl=@imageurl";
        //    cmd.CommandText = sqltext;
        //    cmd.Parameters.Add("id", DbType.String).Value = info["id"];
        //    cmd.Parameters.Add("name", DbType.String).Value = info["name"];
        //    cmd.Parameters.Add("birthday", DbType.String).Value = info.ContainsKey("birthday") ? info["birthday"] : "";
        //    cmd.Parameters.Add("age", DbType.Int32).Value = int.Parse(info.ContainsKey("age") ? info["age"] : "0");
        //    cmd.Parameters.Add("height", DbType.Int32).Value = int.Parse(info.ContainsKey("height") ? info["height"] : "0");
        //    cmd.Parameters.Add("cup", DbType.String).Value = info.ContainsKey("cup") ? info["cup"] : "";
        //    cmd.Parameters.Add("chest", DbType.Int32).Value = int.Parse(info.ContainsKey("chest") ? info["chest"] : "0");
        //    cmd.Parameters.Add("waist", DbType.Int32).Value = int.Parse(info.ContainsKey("waist") ? info["waist"] : "0");
        //    cmd.Parameters.Add("hipline", DbType.Int32).Value = int.Parse(info.ContainsKey("hipline") ? info["hipline"] : "0");
        //    cmd.Parameters.Add("birthplace", DbType.String).Value = info.ContainsKey("birthplace") ? info["birthplace"] : "";
        //    cmd.Parameters.Add("hobby", DbType.String).Value = info.ContainsKey("hobby") ? info["hobby"] : "";
        //    cmd.Parameters.Add("sourceurl", DbType.String).Value = info.ContainsKey("sourceurl") ? info["sourceurl"] : "";
        //    cmd.Parameters.Add("source", DbType.String).Value = info.ContainsKey("source") ? info["source"] : "";
        //    cmd.Parameters.Add("imageurl", DbType.String).Value = info.ContainsKey("imageurl") ? info["imageurl"] : "";
        //    cmd.ExecuteNonQuery();
        //}


        public void InsertActress(Actress actress)
        {
            if (actress == null) { return; }
            if (string.IsNullOrEmpty( actress.name) ) { return; }
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




        #endregion

        #region "Other Command"


        /// <summary>
        /// 组合搜索，获得所有筛选值
        /// </summary>
        /// <returns></returns>
        public List<List<string>> GetAllFilter()
        {
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

        /// <summary>
        /// 读取 Access 到 SQlite
        /// </summary>
        /// <param name="path"></param>
        public void InsertFromAccess(string path = "")
        {
            //从Access中读取
            string dbpath = AppDomain.CurrentDomain.BaseDirectory + "\\mdb\\MainDatabase.mdb";
            if (path != "") { dbpath = path; }
            if (!File.Exists(dbpath)) { return; }
            OleDbConnection con = new OleDbConnection();
            OleDbCommand cmd = new OleDbCommand();
            OleDbDataReader sr;
            con.ConnectionString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + dbpath + ";Mode=Read";
            con.Open();
            cmd.Connection = con;
            cmd.CommandText = "select * from Main";
            sr = cmd.ExecuteReader();

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
            con.Close();
        }

        public string GetInfoBySql(string sql)
        {
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

        #endregion


    }

    public class Movie
    {
        private string _id;
        public string id { get { return _id; } set {
                _id = value.ToUpper();
            }}
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
                        if (!string.IsNullOrEmpty(item) & item != "")
                            if (File.Exists(item)) subsectionlist.Add(item);
                    }
                }
                OnPropertyChanged();
            }
        }

        public List<string> subsectionlist { get; set; }


        public int vediotype { get; set; }
        public string scandate { get; set; }


        private string _releasedate;
        public string releasedate { get { return _releasedate; } set {
                DateTime dateTime = new DateTime(1900, 01, 01);
                DateTime.TryParse(value.ToString(), out dateTime);
                _releasedate = dateTime.ToString("yyyy-MM-dd");
            } }
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
        private MemoryStream _gif;


        public BitmapSource smallimage { get { return _smallimage; } set { _smallimage = value; OnPropertyChanged(); } }
        public BitmapSource bigimage { get { return _bigimage; } set { _bigimage = value; OnPropertyChanged(); } }
        public MemoryStream gif { get { return _gif; } set { _gif = value; OnPropertyChanged(); } }


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
        public string birthday { get { return _birthday; } 
            set {
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
        public int age { get { return _age; } set {
                int a = 0;
                int.TryParse(value.ToString(), out a);
                if (a < 0 || a > 200) a = 0;
                _age = a;
            } }

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
                if (a < 0 || a>500) a = 0;
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
