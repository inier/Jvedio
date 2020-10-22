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
    public class DB
    {

        public static string Path { get; set; }
        private SQLiteCommand cmd;
        private SQLiteConnection cn;
        public string dbName;
        public object LockObject;


        public DB(string DatabaseName = "", bool absolutPath = false)
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
            else
            {
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
        public string SelectInfoByID(string info, string table, string id)
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




        #endregion

        #region "Other Command"


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
}
