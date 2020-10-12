
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace Jvedio
{
    public static class StaticVariable
    {
        //软件的全局变量
        public static string BasePicPath;

        public static rootUrl RootUrl;
        public static enableUrl EnableUrl;
        public static Cookie AllCookies;

        public static List<string> Qibing = new List<string>();
        public static List<string> Bubing = new List<string>();

        public static double MinHDVFileSize = 2;//多少 GB 视为高清


        //数据库
        public static string[] GenreEurope = new string[8];
        public static string[] GenreCensored = new string[7];
        public static string[] GenreUncensored = new string[8];
        public static string InfoDataBasePath = AppDomain.CurrentDomain.BaseDirectory + "Info.sqlite";

        public static List<string> LockDataBase; //用于存放正在读写的数据库，避免锁

        //新建表 sqlite 语句
        public static string SQLITETABLE_MOVIE = "create table if not exists movie (id VARCHAR(50) PRIMARY KEY , title TEXT , filesize DOUBLE DEFAULT 0 , filepath TEXT , subsection TEXT , vediotype INT , scandate VARCHAR(30) , releasedate VARCHAR(10) DEFAULT '1900-01-01', visits INT  DEFAULT 0, director VARCHAR(50) , genre TEXT , tag TEXT , actor TEXT , actorid TEXT ,studio VARCHAR(50) , rating FLOAT  DEFAULT 0, chinesetitle TEXT , favorites INT  DEFAULT 0, label TEXT , plot TEXT , outline TEXT , year INT  DEFAULT 1900, runtime INT  DEFAULT 0, country VARCHAR(50) , countrycode INT DEFAULT 0 ,otherinfo TEXT, sourceurl TEXT, source VARCHAR(10),actressimageurl TEXT,smallimageurl TEXT,bigimageurl TEXT,extraimageurl TEXT)";
        public static string SQLITETABLE_ACTRESS = "create table if not exists actress ( id VARCHAR(50) PRIMARY KEY, name VARCHAR(50) ,birthday VARCHAR(10) ,age INT ,height INT ,cup VARCHAR(1), chest INT ,waist INT ,hipline INT ,birthplace VARCHAR(50) ,hobby TEXT, sourceurl TEXT, source VARCHAR(10),imageurl TEXT)";
        public static string SQLITETABLE_LIBRARY = "create table if not exists library ( id VARCHAR(50) PRIMARY KEY, code VARCHAR(50))";
        public static string SQLITETABLE_JAVDB = "create table if not exists javdb ( id VARCHAR(50) PRIMARY KEY, code VARCHAR(50))";
        public static string SQLITETABLE_BAIDUAI = "create table if not exists baidu (id VARCHAR(50) PRIMARY KEY , age INT DEFAULT 0 , beauty FLOAT DEFAULT 0 , expression VARCHAR(20), face_shape VARCHAR(20), gender VARCHAR(20), glasses VARCHAR(20), race VARCHAR(20), emotion VARCHAR(20), mask VARCHAR(20))";
        public static string SQLITETABLE_BAIDUTRANSLATE = "create table if not exists baidu (id VARCHAR(50) PRIMARY KEY , title TEXT , translate_title TEXT, plot TEXT, translate_plot TEXT)";
        public static string SQLITETABLE_YOUDAO = "create table if not exists youdao (id VARCHAR(50) PRIMARY KEY , title TEXT , translate_title TEXT, plot TEXT, translate_plot TEXT)";

        public static string DataBaseConfigPath = "DataBase\\Config.ini";
        public static string ServersConfigPath = "ServersConfig.ini";

        //禁止的文件名
        public static readonly char[] BANFILECHAR =  {'\\','#','%', '&', '*', '|', ':', '"', '<', '>', '?', '/','.' }; //https://docs.microsoft.com/zh-cn/previous-versions/s6feh8zw(v=vs.110)?redirectedfrom=MSDN
                                                                                                                       //保留名称


        #region "热键"
        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        public const int HOTKEY_ID = 2415;
        public static uint VK;
        public static IntPtr _windowHandle;
        public static HwndSource _source;
        public static bool IsHide = false;
        public static List<string> OpeningWindows = new List<string>();
        public static List<Key>  funcKeys = new List<Key>(); //功能键 [1,3] 个
        public static Key key = Key.None;//基础键 1 个
        public static List<Key> _funcKeys = new List<Key>();
        public static Key _key = Key.None;

        public  enum Modifiers
        {
            None = 0x0000,
            Alt = 0x0001,
            Control = 0x0002,
            Shift = 0x0004,
            Win = 0x0008
        }

        public static bool IsProperFuncKey(List<Key> keyList)
        {
            bool result = true;
            List<Key> keys = new List<Key>() { Key.LeftCtrl, Key.LeftAlt, Key.LeftShift };

            foreach (Key item in keyList)
            {
                if (!keys.Contains(item))
                {
                    result = false;
                    break;
                }
            }
            return result;
        }

        #endregion

        public static void InitVariable()
        {
            if (Directory.Exists(Properties.Settings.Default.BasePicPath))
                BasePicPath = Properties.Settings.Default.BasePicPath;
            else
                BasePicPath = AppDomain.CurrentDomain.BaseDirectory + "Pic\\";


            if (Properties.Settings.Default.Bus == "") Properties.Settings.Default.EnableBus = false;
            if (Properties.Settings.Default.BusEurope == "") Properties.Settings.Default.EnableBusEu = false;
            if (Properties.Settings.Default.DB == "") Properties.Settings.Default.EnableDB = false;
            if (Properties.Settings.Default.Library == "") Properties.Settings.Default.EnableLibrary = false;
            if (Properties.Settings.Default.DMM == "") Properties.Settings.Default.EnableDMM = false;
            if (Properties.Settings.Default.Jav321 == "") Properties.Settings.Default.Enable321 = false;
            Properties.Settings.Default.Save();


            //格式化网址
            FormatUrl();


            RootUrl = new rootUrl
            {
                Bus = Properties.Settings.Default.Bus,
                BusEu = Properties.Settings.Default.BusEurope,
                Library = Properties.Settings.Default.Library,
                FC2Club = Properties.Settings.Default.Fc2Club,
                Jav321 = Properties.Settings.Default.Jav321,
                DMM = Properties.Settings.Default.DMM,
                DB = Properties.Settings.Default.DB
            };

            //fc2 关站了
            EnableUrl = new enableUrl
            {
                Bus = Properties.Settings.Default.EnableBus,
                BusEu = Properties.Settings.Default.EnableBusEu,
                Library = Properties.Settings.Default.EnableLibrary,
                FC2Club = false,
                Jav321 = Properties.Settings.Default.Enable321,
                DMM = Properties.Settings.Default.EnableDMM,
                DB = Properties.Settings.Default.EnableDB
            };

            AllCookies = new Cookie
            {
                Bus = "",
                BusEu = "",
                Library = "",
                FC2Club = "",
                Jav321 = "",
                DMM = "",
                DB = Properties.Settings.Default.DBCookie
            };

            GenreEurope[0] = "非同性戀,撩撥性,迷人,陰道擴張,體液,戀腿癖,工作性幻想,戀足癖,戶外,戀物癖,面紅耳赤,控制,軟色情,乳液,夫妻性幻想,洗澡,FFM,運動,手銬,拙劣的模仿,幕後花絮,狂歡,真實,科幻,歐洲,第一個男孩 / 女孩,公共性,屄,通姦,施工,婚禮,潤滑油,性交,同性戀,出軌,奇聞趣事";
            GenreEurope[1] = "青年人,人妻,競技,媽媽,女商人,護士,女學生,老師,學生,烏木,秘書,施虐者,老闆,繼母,刑事,客戶,女友,警察,女僕,啦啦隊長,脫衣舞女,性治療師,繼女,小姐,特務,軍人,妻子,戰士女孩,角色扮演,奴隸,按摩師,囚犯,商店營業員,浪蕩公子,校長,廚師,機械師,孕婦,追星族,空姐,聖誕老人的助手,水手,超女,律師,救生員,消防員,女童子軍,水管工人,法官,奶女僕,芭蕾舞者,熟女,脫衣舞,素人,醫生 / 護士,少女,保姆,犠母妹";
            GenreEurope[2] = "高跟鞋,比基尼泳裝,長襪,丁字褲,裙子,連衣裙,內褲,緊身短褲,襪子,牛仔褲,制服,戲服,靴子,鞋,連褲襪,平角褲,人字拖,超短裙,眼鏡,手套,打底褲,眼罩,運動鞋,睡衣,黑色絲襪,裸體絲襪,連體衣,彩色絲襪,襯衫,內衣,網襪";
            GenreEurope[3] = "紋身,淺黑膚色的女人,金發女郎,白種人,中等的膚色,深棕色頭髮,褐色眼睛,金色的頭髮,藍眼睛,刺穿,黑髮,長發,小乳,綠色的眼睛,赤腳,大奶崇拜,18 +,拉美裔女人色情,嬌小,曬痕,蓬鬆乳房,駱駝趾,淺色皮膚,中等屁股,美臀,烏木色情,紅頭髮,紅發女郎,灰色眼睛,巨大乳房,多毛,自然陰毛,小乳房,紅色頭髮,性感,亞洲人色情,豐滿的女人,巨大陽具,黑色眼睛,肌肉,深色皮膚,短髮,豐滿,彎曲的女人,無毛,巨乳,自然乳房,巨臀,裸體,苗條,粗大陽具";
            GenreEurope[4] = "自慰,一對一,單人,後背體位,手淫,高潮,指交,舔陰,舔屁股,陰道插入,三人組,玩乳房,肛門插入,法式接吻,69,背後騎乘式,後庭崇拜,多人色情,大陽具崇拜,塞入食物,舔蛋蛋,深操,肛門指交,腳交,足交,射在屁股上,射在腳上,顏面騎乘,服從,射在陰戶,小便,腳塞陰道,站立性交,肛門射精,二對一,後騎式,四人組,拳交,射精交換,吸吮振動棒,肛門塞,性交馬鞍,射在腹部,乳房抖動,拳交肛門,塞入蔬果,擴張器,陰門射精,首先肛,射在腿上,乳房擠壓,肛門拉珠,肛門內射,一對多,舔,舔鞋,鞋交,吸精,打樁機(POV),肛門指法,口交,肛交,深喉,剃毛,乳交,騎乘式,顏射,舔肛,噴射,射在乳房,吸奶,吞精,內射,舔腳,雙陽具插入,後入,傳教士,側入";
            GenreEurope[5] = "振動器,假陰莖,其它玩具,肛門擴張,聊天,情趣用品,油,奴役,打屁股,跨種族,玻璃自慰器,玩奶頭,按摩,調教,吸吮玩具,抽打,人造陰莖,繩索,雙頭假陽具,帶上假陽具,調教項圈,粗暴性愛,性交機器,玩具自慰,瑜伽,鞭,塞嘴球,偷窺,換妻,束縛,肛門玩具,脫衣舞俱樂部,乳頭夾子,肛塞,人體彩繪,鬼鬼祟祟";
            GenreEurope[6] = "匈牙利人,捷克人,俄羅斯人,拉丁人,羅馬尼亞人,法國人,亞洲人,烏克蘭人,斯洛伐克人,美國人,日本人,拉脫維亞人,菲律賓人,印度人,中國人,意大利人,德國人,葡萄牙人,立陶宛人,荷蘭人,希臘人,哥倫比亞人,塞爾維亞人,愛爾蘭人,阿拉伯人,澳大利亞人,加拿大人,蘇格蘭人,奧地利人,越南人,巴西人,瑞典人,白俄羅斯人,蒙古人,高加索人,英國人,西班牙人";
            GenreEurope[7] = "室內,客廳,野外,臥室,床上,花園,學院,浴室,淋浴,汽車,辦公室,診所,船上,健身房,廚房,桑拿,穀倉,聖誕節,俱樂部,醫院,妓院,萬聖節,酒店,車庫,商店,餐廳,餐館,情人節,更衣室,浴缸,復活節,警察局,酒店客房,派對,獨立日,感恩節,海灘,洗手間,五月五日,公園,卡車,聖帕特里克節,豪華轎車,酒吧,泳池,學校,新年,監獄";

            GenreCensored[0] = "折磨,嘔吐,觸手,蠻橫嬌羞,處男,正太控,出軌,瘙癢,運動,女同接吻,性感的,美容院,處女,爛醉如泥的,殘忍畫面,妄想,惡作劇,學校作品,粗暴,通姦,姐妹,雙性人,跳舞,性奴,倒追,性騷擾,其他,戀腿癖,偷窥,花癡,男同性恋,情侶,戀乳癖,亂倫,其他戀物癖,偶像藝人,野外・露出,獵豔,女同性戀,企畫,10枚組,性感的,性感的,科幻,女優ベスト・総集編,温泉,M男,原作コラボ,16時間以上作品,デカチン・巨根,ファン感謝・訪問,動画,巨尻,ハーレム,日焼け,早漏,キス・接吻,汗だく,スマホ専用縦動画,Vシネマ,Don Cipote's choice,アニメ,アクション,イメージビデオ（男性）,孕ませ,ボーイズラブ,ビッチ,特典あり（AVベースボール）,コミック雑誌,時間停止";
            GenreCensored[1] = "童年朋友,公主,亞洲女演員,伴侶,講師,婆婆,格鬥家,女檢察官,明星臉,女主人、女老板,模特兒,秘書,美少女,新娘、年輕妻子,姐姐,格鬥家,車掌小姐,寡婦,千金小姐,白人,已婚婦女,女醫生,各種職業,妓女,賽車女郎,女大學生,展場女孩,女教師,母親,家教,护士,蕩婦,黑人演員,女生,女主播,高中女生,服務生,魔法少女,學生（其他）,動畫人物,遊戲的真人版,超級女英雄";
            GenreCensored[2] = "角色扮演,制服,女戰士,及膝襪,娃娃,女忍者,女裝人妖,內衣,猥褻穿著,兔女郎,貓耳女,女祭司,泡泡襪,制服,緊身衣,裸體圍裙,迷你裙警察,空中小姐,連褲襪,身體意識,OL,和服・喪服,體育服,內衣,水手服,學校泳裝,旗袍,女傭,迷你裙,校服,泳裝,眼鏡,角色扮演,哥德蘿莉,和服・浴衣";
            GenreCensored[3] = "超乳,肌肉,乳房,嬌小的,屁股,高,變性者,無毛,胖女人,苗條,孕婦,成熟的女人,蘿莉塔,貧乳・微乳,巨乳";
            GenreCensored[4] = "顏面騎乘,食糞,足交,母乳,手指插入,按摩,女上位,舔陰,拳交,深喉,69,淫語,潮吹,乳交,排便,飲尿,口交,濫交,放尿,打手槍,吞精,肛交,顏射,自慰,顏射,中出,肛内中出";
            GenreCensored[5] = "立即口交,女優按摩棒,子宮頸,催眠,乳液,羞恥,凌辱,拘束,輪姦,插入異物,鴨嘴,灌腸,監禁,紧缚,強姦,藥物,汽車性愛,SM,糞便,玩具,跳蛋,緊縛,按摩棒,多P,性愛,假陽具,逆強姦";
            GenreCensored[6] = "合作作品,恐怖,給女性觀眾,教學,DMM專屬,R-15,R-18,戲劇,3D,特效,故事集,限時降價,複刻版,戲劇,戀愛,高畫質,主觀視角,介紹影片,4小時以上作品,薄馬賽克,經典,首次亮相,數位馬賽克,投稿,纪录片,國外進口,第一人稱攝影,業餘,局部特寫,獨立製作,DMM獨家,單體作品,合集,高清,字幕,天堂TV,DVD多士爐,AV OPEN 2014 スーパーヘビー,AV OPEN 2014 ヘビー級,AV OPEN 2014 ミドル級,AV OPEN 2015 マニア/フェチ部門,AV OPEN 2015 熟女部門,AV OPEN 2015 企画部門,AV OPEN 2015 乙女部門,AV OPEN 2015 素人部門,AV OPEN 2015 SM/ハード部門,AV OPEN 2015 女優部門,AVOPEN2016人妻・熟女部門,AVOPEN2016企画部門,AVOPEN2016ハード部門,AVOPEN2016マニア・フェチ部門,AVOPEN2016乙女部門,AVOPEN2016女優部門,AVOPEN2016ドラマ・ドキュメンタリー部門,AVOPEN2016素人部門,AVOPEN2016バラエティ部門,VR専用";

            GenreUncensored[0] = "高清,字幕,推薦作品,通姦,淋浴,舌頭,下流,敏感,變態,願望,慾求不滿,服侍,外遇,訪問,性伴侶,保守,購物,誘惑,出差,煩惱,主動,再會,戀物癖,問題,騙奸,鬼混,高手,順從,密會,做家務,秘密,送貨上門,壓力,處女作,淫語,問卷,住一宿,眼淚,跪求,求職,婚禮,第一視角,洗澡,首次,劇情,約會,實拍,同性戀,幻想,淫蕩,旅行,面試,喝酒,尖叫,新年,借款,不忠,檢查,羞恥,勾引,新人,推銷,ブルマ";
            GenreUncensored[1] = "AV女優,情人,丈夫,辣妹,S級女優,白領,偶像,兒子,女僕,老師,夫婦,保健室,朋友,工作人員,明星,同事,面具男,上司,睡眠系,奶奶,播音員,鄰居,親人,店員,魔女,視訊小姐,大學生,寡婦,小姐,秘書,人妖,啦啦隊,美容師,岳母,警察,熟女,素人,人妻,痴女,角色扮演,蘿莉,姐姐,模特,教師,學生,少女,新手,男友,護士,媽媽,主婦,孕婦,女教師,年輕人妻,職員,看護,外觀相似,色狼,醫生,新婚,黑人,空中小姐";
            GenreUncensored[2] = "制服,內衣,休閒裝,水手服,全裸,不穿內褲,和服,不戴胸罩,連衣裙,打底褲,緊身衣,客人,晚禮服,治癒系,大衣,裸體襪子,絲帶,睡衣,面具,牛仔褲,喪服,極小比基尼,混血,毛衣,頸鏈,短褲,美人,連褲襪,裙子,浴衣和服,泳衣,網襪,眼罩,圍裙,比基尼,情趣內衣,迷你裙,套裝,眼鏡,丁字褲,陽具腰帶";
            GenreUncensored[3] = "美肌,屁股,美穴,黑髮,嬌小,曬痕,F罩杯,E罩杯,D罩杯,素顏,貓眼,捲髮,虎牙,C罩杯,I罩杯,小麥色,大陰蒂,美乳,巨乳,豐滿,苗條,美臀,美腿,無毛,美白,微乳,性感,高個子,爆乳,G罩杯,多毛,巨臀,軟體,巨大陽具,長發,H罩杯";
            GenreUncensored[4] = "舔陰,電動陽具,淫亂,射在外陰,猛烈,後入內射,足交,射在胸部,側位內射,射在腹部,騎乘內射,射在頭髮,母乳,站立姿勢,肛射,陰道擴張,內射觀察,射在大腿,精液流出,射在屁股,內射潮吹,首次肛交,射在衣服上,首次內射,早洩,翻白眼,舔腳,喝尿,口交,內射,自慰,後入,騎乘位,顏射,口內射精,手淫,潮吹,輪姦,亂交,乳交,小便,吸精,深膚色,指法,騎在臉上,連續內射,打樁機,肛交,吞精,鴨嘴,打飛機,剃毛,站立位,高潮,二穴同入,舔肛,多人口交,痙攣,玩弄肛門,立即口交,舔蛋蛋,口射,陰屁,失禁,大量潮吹,69";
            GenreUncensored[5] = "振動,淫語,搭訕,奴役,打屁股,潤滑油,按摩,散步,扯破連褲襪,手銬,束縛,調教,假陽具,變態遊戲,注視,蠟燭,電鑽,亂搞,摩擦,項圈,繩子,灌腸,監禁,車震,鞭打,懸掛,喝口水,精液塗抹,舔耳朵,女體盛,便利店,插兩根,開口器,暴露,陰道放入食物,大便,經期,惡作劇,電動按摩器,凌辱,玩具,露出,肛門,拘束,多P,潤滑劑,攝影,野外,陰道觀察,SM,灌入精液,受虐,綁縛,偷拍,異物插入,電話,公寓,遠程操作,偷窺,踩踏";
            GenreUncensored[6] = "企劃物,獨佔動畫,10代,1080p,人氣系列,60fps,超VIP,投稿,VIP,椅子,風格出眾,首次作品,更衣室,下午,KTV,白天,最佳合集";
            GenreUncensored[7] = "酒店,密室,車,床,陽台,公園,家中,公交車,公司,門口,附近,學校,辦公室,樓梯,住宅,公共廁所,旅館,教室,廚房,桌子,大街,農村,和室,地下室,牢籠,屋頂,游泳池,電梯,拍攝現場,別墅,房間,愛情旅館,車內,沙發,浴室,廁所,溫泉,醫院,榻榻米";



        }


        public static void FormatUrl()
        {
            Properties.Settings.Default.Bus = _FormatUrl(Properties.Settings.Default.Bus);
            Properties.Settings.Default.DB = _FormatUrl(Properties.Settings.Default.DB);
            Properties.Settings.Default.Library = _FormatUrl(Properties.Settings.Default.Library);
            Properties.Settings.Default.Jav321 = _FormatUrl(Properties.Settings.Default.Jav321);
            Properties.Settings.Default.Fc2Club = _FormatUrl(Properties.Settings.Default.Fc2Club);
            Properties.Settings.Default.DMM = _FormatUrl(Properties.Settings.Default.DMM);
        }

        private static string _FormatUrl(string url)
        {
            url = url.ToLower();
            if (string.IsNullOrEmpty(url)) return "";
            if(url.IndexOf("http") < 0) url = "https://" + url;
            if (!url.EndsWith("/"))  url += "/";
            return url;
        }


        #region "enum"
        public enum MovieStampType { 无, 高清中字, 无码流出 }

        public enum VedioType { 所有, 步兵, 骑兵, 欧美 }

        public enum ImageType { SmallImage, BigImage, ExtraImage, ActorImage }

        public enum JvedioWindowState { Normal, Minimized, Maximized, FullScreen, None }

        public enum WebSite { Bus, BusEu, Library, DB, FC2Club, Jav321, DMM }

        public enum Skin { 黑色,白色, 蓝色}

        public enum Language { 中文, English, 日本語 }


        #endregion



        #region "struct"



        public struct Cookie
        {
            public string Bus;
            public string BusEu;
            public string Library;
            public string DB;
            public string Jav321;
            public string DMM;
            public string FC2Club;
        }

        public struct rootUrl
        {
            public string Bus;
            public string BusEu;
            public string Library;
            public string DB;
            public string Jav321;
            public string DMM;
            public string FC2Club;
        }

        public struct enableUrl
        {
            public bool Bus;
            public bool BusEu;
            public bool Library;
            public bool DB;
            public bool Jav321;
            public bool DMM;
            public bool FC2Club;
        }

        #endregion





    }
}
