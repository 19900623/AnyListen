using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using AnyListen.Helper;
using AnyListen.Interface;
using AnyListen.Model;
using Newtonsoft.Json.Linq;

namespace AnyListen.Api.Music
{
    public class XmMusic : IMusic
    {

        //该API包含的歌曲信息十分全面，但是无法获取搜索结果总数目以及无法指定单页数量
        //附带另一个搜索API：http://api.xiami.com/web?v=2.0&app_key=1&key=hello&page=1&limit=30&r=search/songs
        //该API需要指定Refer：http://m.xiami.com/
        //否则提示非法请求
        public static SearchResult Search(string key, int page)
        {
            var url = "http://www.xiami.com/app/xiating/search-song2?key=" + key + "&uid=0&callback=xiami&page=" + page;
            var html = CommonHelper.GetHtmlContent(url);
            var result = new SearchResult
            {
                ErrorCode = 200,
                ErrorMsg = "OK",
                KeyWord = key,
                PageNum = page,
                TotalSize = -1,     //-1表示未知
                Songs = new List<SongResult>()
            };
            if (string.IsNullOrEmpty(html) || html == "null" || html.Contains("无法在虾米资料库中得到结果"))
            {
                result.ErrorCode = 404;
                result.ErrorMsg = "没有找到符合要求的歌曲";
                return result;
            }
            try
            {
                var json = JObject.Parse(html);
                var data = json["data"];
                foreach (var j in data)
                {
                    try
                    {
                        if (j["song_status"].ToString() != "0")
                        {
                            continue;   //滤除下架歌曲
                        }
                        var song = new SongResult
                        {
                            SongId = j["song_id"].ToString(),
                            SongName = j["song_name"].ToString(),
                            SongSubName = j["sub_title"]?.ToString(),
                            SongLink = "http://www.xiami.com/song/" + j["song_id"],

                            ArtistId = j["artist_id"].ToString(),
                            ArtistName = j["singer"].ToString(),
                            ArtistSubName = j["artist_sub_title"]?.ToString(),

                            AlbumId = j["album_id"].ToString(),
                            AlbumName = j["album_name"].ToString(),
                            AlbumSubName = j["album_sub_title"]?.ToString(),
                            AlbumArtist = j["artist_name"].ToString(),

                            Length = j["songtime"].ToString(),
                            Size = "",
                            BitRate = "320K",

                            FlacUrl = "",
                            ApeUrl = "",
                            WavUrl = "",
                            SqUrl = CommonHelper.GetSongUrl("xm", "320", j["song_id"].ToString(), "mp3"),
                            HqUrl = CommonHelper.GetSongUrl("xm", "192", j["song_id"].ToString(), "mp3"),
                            LqUrl = CommonHelper.GetSongUrl("xm", "128", j["song_id"].ToString(), "mp3"),
                            CopyUrl = CommonHelper.GetSongUrl("xm", "320", j["song_id"].ToString(), "mp3"),

                            PicUrl = ("http://img.xiami.net/" + j["album_logo"]).Replace("_1.", "_4."),
                            LrcUrl = j["lyric"].ToString(),
                            TrcUrl = "",
                            KrcUrl = j["lyric_karaok"]?.ToString(),

                            MvId = j["mv_id"]?.ToString(),
                            MvHdUrl = "",
                            MvLdUrl = "",

                            Language = "",
                            Company = "",
                            Year = CommonHelper.UnixTimestampToDateTime(Convert.ToInt64(string.IsNullOrEmpty(j["gmt_publish"].ToString()) ? "0" : j["gmt_publish"].ToString())).ToString("yyyy-MM-dd"),
                            Disc = j["cd_serial"].ToString(),
                            TrackNum = j["track"].ToString(),
                            Type = "xm"
                        };
                        if (!string.IsNullOrEmpty(song.LrcUrl))
                        {
                            if (song.LrcUrl.EndsWith("txt"))
                            {
                                song.LrcUrl = CommonHelper.GetSongUrl("xm", "128", song.SongId, "lrc");
                            }
                        }
                        if (!string.IsNullOrEmpty(song.MvId))
                        {
                            if (song.MvId != "0")
                            {
                                song.MvHdUrl = CommonHelper.GetSongUrl("xm", "hd", song.SongId, "mp4");
                                song.MvLdUrl = CommonHelper.GetSongUrl("xm", "ld", song.SongId, "mp4");
                            }
                        }
                        result.Songs.Add(song);
                    }
                    catch (Exception ex)
                    {
                        CommonHelper.AddLog(ex);
                    }
                }
                return result;
            }
            catch (Exception)
            {
                result.ErrorCode = 500;
                result.ErrorMsg = "解析歌曲时发生错误";
                return result;
            }
        }

        private static AlbumResult SearchAlbum(string id)
        {
            var result = new AlbumResult
            {
                ErrorCode = 200,
                ErrorMsg = "OK",
                AlbumLink = "http://www.xiami.com/album/" + id,
                Songs = new List<SongResult>()
            };
            var list = GetResultsByIds(id, 1);
            if (list == null || list.Count <= 0)
            {
                result.Songs = GetLostAlbum(id);
            }
            else
            {
                result.Songs = list;
            }
            var url = "http://www.xiami.com/app/xiating/album?spm=0.0.0.0.L6k2wP&id=" + id + "&uid=0";
            var html = CommonHelper.GetHtmlContent(url);
            if (string.IsNullOrEmpty(html))
            {
                return result;
            }
            try
            {
                var match = Regex.Match(html,
                    @"(?<=h1 title="")([^""]+)(?:""[\s\S]+?p class="")([^""]*)(?:""[\s\S]+detail_songer"">)([^<]+)(?:</a>[\s\S]+?<em>)([^<]+)(?:</em>[\s\S]+?<em>)([^<]+)(?:</em>[\s\S]+?<em>)([^<]+)(?:</em>[\s\S]+?<em>)([^<]+)(?:</em>)");
                if (match.Length <= 0)
                {
                    return result;
                }
                foreach (var s in result.Songs)
                {
                    s.AlbumSubName = match.Groups[2].Value;
                    s.AlbumArtist = match.Groups[3].Value;
                    s.Language = match.Groups[4].Value;
                    s.Company = match.Groups[5].Value;
                    s.Year = match.Groups[6].Value.Replace("年", "-").Replace("月", "-").Replace("日", "");
                }
                result.AlbumType = match.Groups[7].Value;
            }
            catch (Exception ex)
            {
                CommonHelper.AddLog(ex);
            }
            return result;
        }

        private static List<SongResult> GetResultsByIds(string ids, int type)
        {
            var albumUrl = "http://www.xiami.com/song/playlist/id/" + ids + "/type/" + type + "/cat/json";
            var html = CommonHelper.GetHtmlContent(albumUrl);
            if (string.IsNullOrEmpty(html) || html.Contains("应版权方要求，没有歌曲可以播放"))
            {
                return null;
            }
            var list = new List<SongResult>();
            try
            {
                var json = JObject.Parse(html);
                var data = json["data"]["trackList"];
                if (string.IsNullOrEmpty(data.ToString()))
                {
                    return null;
                }
                foreach (var j in data)
                {
                    try
                    {
                        var song = new SongResult
                        {
                            SongId = j["songId"].ToString(),
                            SongName = j["songName"].ToString(),
                            SongSubName = j["subName"]?.ToString(),
                            SongLink = "http://www.xiami.com/song/" + j["song_id"],

                            ArtistId = j["artistId"].ToString(),
                            ArtistName = j["singers"].ToString(),
                            ArtistSubName = j["artist_sub_title"]?.ToString(),

                            AlbumId = j["albumId"].ToString(),
                            AlbumName = j["album_name"].ToString(),
                            AlbumSubName = j["album_sub_title"]?.ToString(),
                            AlbumArtist = j["artist"].ToString(),

                            Length = CommonHelper.NumToTime(j["length"].ToString()),
                            Size = "",
                            BitRate = "320K",

                            FlacUrl = "",
                            ApeUrl = "",
                            WavUrl = "",
                            SqUrl = "",
                            HqUrl = "",
                            LqUrl = Jurl(j["location"].ToString()),
                            CopyUrl = CommonHelper.GetSongUrl("xm", "320", j["song_id"].ToString(), "mp3"),

                            PicUrl = ("http://img.xiami.net/" + j["album_logo"]).Replace("_1.", "_4."),
                            LrcUrl = j["lyric"].ToString(),
                            TrcUrl = "",
                            KrcUrl = j["lyric_karaok"]?.ToString(),

                            MvId = j["mvUrl"]?.ToString(),
                            MvHdUrl = "",
                            MvLdUrl = "",

                            Language = "",
                            Company = "",
                            Year = "",
                            Disc = j["cd_serial"].ToString(),
                            TrackNum = j["track"].ToString(),
                            Type = "xm"
                        };
                        if (j["purview"] != null)
                        {
                            song.SqUrl = song.HqUrl = j["purview"]["filePath"]?.ToString();
                        }
                        if (!string.IsNullOrEmpty(song.MvId))
                        {
                            if (song.MvId != "0")
                            {
                                song.MvHdUrl = CommonHelper.GetSongUrl("xm", "hd", song.SongId, "mp4");
                                song.MvLdUrl = CommonHelper.GetSongUrl("xm", "ld", song.SongId, "mp4");
                            }
                        }
                        list.Add(song);
                    }
                    catch (Exception ex)
                    {
                        CommonHelper.AddLog(ex);
                    }
                }
                return list;
            }
            catch (Exception ex)
            {
                CommonHelper.AddLog(ex);
                return null;
            }
        }

        private static List<SongResult> GetLostAlbum(string id)
        {
            var url = "http://api.xiami.com/web?id=" + id + "&r=album%2Fdetail&app_key=09bef203bfa02bfbe3f1cfd7073cb0f3";
            var html = CommonHelper.GetHtmlContent(url, 1, new Dictionary<string, string>
            {
                {"Referer", "http://m.xiami.com/"}
            });
            if (string.IsNullOrEmpty(html))
            {
                return null;
            }
            var json = JObject.Parse(html);
            var datas = json["data"]["songs"];
            var list = new List<SongResult>();
            var index = 0;
            foreach (JToken j in datas)
            {
                index++;
                var song = new SongResult
                {
                    SongId = j["song_id"].ToString(),
                    SongName = j["song_name"].ToString(),
                    SongSubName = "",

                    ArtistId = j["artist_id"].ToString(),
                    ArtistName = j["singers"].ToString(),
                    ArtistSubName = "",

                    AlbumId = j["album_id"].ToString(),
                    AlbumName = j["album_name"].ToString(),
                    AlbumSubName = "",
                    AlbumArtist = json["data"]["artist_name"].ToString(),

                    Length = j["songtime"]?.ToString(),
                    Size = "",
                    BitRate = "320K",

                    FlacUrl = "",
                    ApeUrl = "",
                    WavUrl = "",
                    SqUrl = CommonHelper.GetSongUrl("xm", "320", j["song_id"].ToString(), "mp3"),
                    HqUrl = CommonHelper.GetSongUrl("xm", "192", j["song_id"].ToString(), "mp3"),
                    LqUrl = CommonHelper.GetSongUrl("xm", "128", j["song_id"].ToString(), "mp3"),
                    CopyUrl = CommonHelper.GetSongUrl("xm", "320", j["song_id"].ToString(), "mp3"),

                    PicUrl = j["album_logo"].ToString().Replace("_1.", "_4."),
                    LrcUrl = CommonHelper.GetSongUrl("xm", "128", j["song_id"].ToString(), "lrc"),
                    TrcUrl = "",
                    KrcUrl = "",

                    MvId = j["mv_id"]?.ToString(),
                    MvHdUrl = "",
                    MvLdUrl = "",

                    Language = "",
                    Company = "",
                    Year = CommonHelper.UnixTimestampToDateTime(Convert.ToInt64(json["data"]["gmt_publish"].ToString())).ToString("yyyy-MM-dd"),
                    Disc = "1",
                    TrackNum = index.ToString(),
                    Type = "xm"
                };
                if (!string.IsNullOrEmpty(song.MvId))
                {
                    if (song.MvId != "0")
                    {
                        song.MvHdUrl = CommonHelper.GetSongUrl("xm", "hd", song.SongId, "mp4");
                        song.MvLdUrl = CommonHelper.GetSongUrl("xm", "ld", song.SongId, "mp4");
                    }
                }
                list.Add(song);
            }
            return list;
        }

        private static SongResult SearchSong(string songId)
        {
            var list = GetResultsByIds(songId, 0);
            var song = list?[0] ?? GetLostSong(songId);
            if (song != null)
            {
                GetSongDetials(song);
            }
            return song;
        }

        private static SongResult GetLostSong(string songId)
        {
            var html = CommonHelper.GetHtmlContent("http://api.xiami.com/web?v=2.0&app_key=1&id=" + songId + "&r=song/detail",
                    1, new Dictionary<string, string>
                    {
                        {"Referer", "http://m.xiami.com/"}
                    });
            if (string.IsNullOrEmpty(html))
            {
                return null;
            }
            var json = JObject.Parse(html);
            var j = json["data"]["song"];
            if (j["song_id"].ToString() == "0")
            {
                return null;
            }
            var song = new SongResult
            {
                SongId = j["song_id"].ToString(),
                SongName = j["song_name"].ToString(),
                SongSubName = "",

                ArtistId = j["artist_id"].ToString(),
                ArtistName = j["singers"].ToString(),
                ArtistSubName = "",

                AlbumId = j["album_id"].ToString(),
                AlbumName = j["album_name"].ToString(),
                AlbumSubName = "",
                AlbumArtist = j["artist_name"].ToString(),

                Length = "",
                Size = "",
                BitRate = "320K",

                FlacUrl = "",
                ApeUrl = "",
                WavUrl = "",
                SqUrl = "",
                HqUrl = "",
                LqUrl = j["listen_file"].ToString(),
                CopyUrl = "",

                PicUrl = j["logo"].ToString().Replace("_1.", "_4."),
                LrcUrl = j["lyric"].ToString(),
                TrcUrl = "",
                KrcUrl = "",

                MvId = "",
                MvHdUrl = "",
                MvLdUrl = "",

                Language = "",
                Company = "",
                Year = "",
                Disc = "1",
                TrackNum = "",
                Type = "xm"
            };
            return song;
        }

        private static void GetSongDetials(SongResult song)
        {
            var url = "http://www.xiami.com/app/xiating/album?spm=0.0.0.0.L6k2wP&id=" + song.AlbumId + "&uid=0";
            var html = CommonHelper.GetHtmlContent(url);
            if (string.IsNullOrEmpty(html))
            {
                return;
            }
            var match = Regex.Match(html,
                @"(?<=h1 title="")([^""]+)(?:""[\s\S]+?p class="")([^""]*)(?:""[\s\S]+detail_songer"">)([^<]+)(?:</a>[\s\S]+?<em>)([^<]+)(?:</em>[\s\S]+?<em>)([^<]+)(?:</em>[\s\S]+?<em>)([^<]+)(?:</em>[\s\S]+?<em>)([^<]+)(?:</em>)");
            if (match.Length <= 0)
            {
                return;
            }
            song.AlbumName = match.Groups[1].Value;
            song.AlbumSubName = match.Groups[2].Value;
            song.AlbumArtist = match.Groups[3].Value;
            song.Language = match.Groups[4].Value;
            song.Company = match.Groups[5].Value;
            song.Year = match.Groups[6].Value.Replace("年", "-").Replace("月", "-").Replace("日", "");
            var discs = Regex.Matches(html, @"(?<=<ul class=""playlist)[\s\S]+?(?=</ul>)");
            for (int i = 0; i < discs.Count; i++)
            {
                match = Regex.Match(discs[i].Value, @"(?<=rel=""" + song.SongId + @"""[\s\S]+?list_index"">)\d+(?=[\s\S]+?id=" + song.SongId + ")");
                if (!string.IsNullOrEmpty(match.Value))
                {
                    song.TrackNum = match.Value;
                    song.Disc = (i + 1).ToString();
                }
            }
        }

        private ArtistResult SearchArtist(string id,int page)
        {
            var result = new ArtistResult
            {
                ErrorCode = 200,
                ErrorMsg = "OK",
                ArtistLink = "http://www.xiami.com/artist/"+id,
                Page = page,
                Songs = new List<SongResult>()
            };
            var html = CommonHelper.PostData("http://www.xiami.com/app/xiating/artist-song",
                new Dictionary<string, string>
                {
                    {"id", id},
                    {"uid", "0"},
                    {"callback", "xiami"},
                    {"page", page.ToString()},
                });
            if (string.IsNullOrEmpty(html))
            {
                result.ErrorCode = 300;
                result.ErrorMsg = "获取热门歌曲信息失败";
                return result;
            }
            try
            {
                var json = JObject.Parse(html);
                var data = json["data"];
                foreach (var j in data)
                {
                    try
                    {
                        if (j["song_status"].ToString() != "0")
                        {
                            continue;   //滤除下架歌曲
                        }
                        var song = new SongResult
                        {
                            SongId = j["song_id"].ToString(),
                            SongName = j["name"].ToString(),
                            SongSubName = j["sub_title"]?.ToString(),
                            SongLink = "http://www.xiami.com/song/" + j["song_id"],

                            ArtistId = j["artist_id"].ToString(),
                            ArtistName = j["singers"].ToString(),
                            ArtistSubName = j["artist_sub_title"]?.ToString(),

                            AlbumId = j["album_id"].ToString(),
                            AlbumName = j["album_name"]?.ToString(),
                            AlbumSubName = j["album_sub_title"]?.ToString(),
                            AlbumArtist = j["artist_name"].ToString(),

                            Length = j["songtime"].ToString(),
                            Size = "",
                            BitRate = "320K",

                            FlacUrl = "",
                            ApeUrl = "",
                            WavUrl = "",
                            SqUrl = CommonHelper.GetSongUrl("xm", "320", j["song_id"].ToString(), "mp3"),
                            HqUrl = CommonHelper.GetSongUrl("xm", "192", j["song_id"].ToString(), "mp3"),
                            LqUrl = CommonHelper.GetSongUrl("xm", "128", j["song_id"].ToString(), "mp3"),
                            CopyUrl = CommonHelper.GetSongUrl("xm", "320", j["song_id"].ToString(), "mp3"),

                            PicUrl = ("http://img.xiami.net/" + j["album_logo"]).Replace("_1.", "_4."),
                            LrcUrl = CommonHelper.GetSongUrl("xm", "320", j["song_id"].ToString(), "lrc"),
                            TrcUrl = "",
                            KrcUrl = "",

                            MvId = j["mv_id"]?.ToString(),
                            MvHdUrl = "",
                            MvLdUrl = "",

                            Language = "",
                            Company = "",
                            Year = "",
                            Disc = j["cd_serial"]?.ToString(),
                            TrackNum = j["track"]?.ToString(),
                            Type = "xm"
                        };
                        if (!string.IsNullOrEmpty(song.MvId))
                        {
                            if (song.MvId != "0")
                            {
                                song.MvHdUrl = CommonHelper.GetSongUrl("xm", "hd", song.SongId, "mp4");
                                song.MvLdUrl = CommonHelper.GetSongUrl("xm", "ld", song.SongId, "mp4");
                            }
                        }
                        result.Songs.Add(song);
                    }
                    catch (Exception ex)
                    {
                        CommonHelper.AddLog(ex);
                    }
                }
            }
            catch (Exception)
            {
                result.ErrorCode = 500;
                result.ErrorMsg = "解析歌曲时发生错误";
            }

            html =
                CommonHelper.GetHtmlContent(
                    "http://api.xiami.com/web?id=" + id + "&r=artist%2Fdetail&app_key=09bef203bfa02bfbe3f1cfd7073cb0f3",
                    1, new Dictionary<string, string>
                    {
                        {"Referer", "http://m.xiami.com/"}
                    });
            if (string.IsNullOrEmpty(html))
            {
                return result;
            }
            try
            {
                var json = JObject.Parse(html);
                result.AlbumSize = json["data"]["albums_count"].Value<int>();
                result.SongSize = -1;
                result.ArtistLogo = json["data"]["logo"].ToString().Replace("_1.", "_4.");
                result.TransName = json["data"]["english_name"]?.ToString();
                result.ArtistInfo = string.Format("性别：{0};地区：{1};唱片公司：{3}",
                    json["data"]["gender"].ToString() == "F" ? "女" : "男", json["data"]["area"].ToString(),
                    json["data"]["company"].ToString());
            }
            catch (Exception ex)
            {
                CommonHelper.AddLog(ex);
            }
            return result;
        }

        private CollectResult SearchCollect(string id)
        {
            var url = "http://api.xiami.com/web?v=2.0&app_key=1&id=188126468&type=collectId&r=collect/detail";
            var html = CommonHelper.GetHtmlContent(url, 1, new Dictionary<string, string>
            {
                {"Referer", "http://m.xiami.com/"}
            });
            var result = new CollectResult
            {
                ErrorCode = 200,
                ErrorMsg = "OK",
                CollectId = id,
                CollectLink = "http://www.xiami.com/collect/"+id,
                Page = 1,
                Songs = new List<SongResult>()
            };
            if (string.IsNullOrEmpty(html))
            {
                result.ErrorCode = 300;
                result.ErrorMsg = "获取歌单信息失败";
                return result;
            }
            try
            {
                var json = JObject.Parse(html);
                var datas = json["data"]["songs"];
                result.CollectName = json["data"]["collect_name"].ToString();
                result.CollectLogo = json["data"]["logo"].ToString().Replace("_1.", "_4.");
                result.SongSize = json["data"]["songs_count"].Value<int>();
                result.Date = CommonHelper.UnixTimestampToDateTime(Convert.ToInt64(json["data"]["gmt_create"].ToString())).ToString("yyyy-MM-dd");
                result.CollectMaker = json["data"]["user_name"].ToString();
                result.Tags = json["data"]["tags"].First?.ToString();
                foreach (JToken j in datas)
                {
                    if (j["song_id"].ToString() == "0")
                    {
                        return null;
                    }
                    var song = new SongResult
                    {
                        SongId = j["song_id"].ToString(),
                        SongName = j["song_name"].ToString(),
                        SongSubName = "",

                        ArtistId = j["artist_id"].ToString(),
                        ArtistName = j["singers"].ToString(),
                        ArtistSubName = "",

                        AlbumId = j["album_id"].ToString(),
                        AlbumName = j["album_name"].ToString(),
                        AlbumSubName = "",
                        AlbumArtist = j["artist_name"].ToString(),

                        Length = CommonHelper.NumToTime(j["length"].ToString()),
                        Size = "",
                        BitRate = "320K",

                        FlacUrl = "",
                        ApeUrl = "",
                        WavUrl = "",
                        SqUrl = CommonHelper.GetSongUrl("xm", "320", j["song_id"].ToString(), "mp3"),
                        HqUrl = CommonHelper.GetSongUrl("xm", "192", j["song_id"].ToString(), "mp3"),
                        LqUrl = j["listen_file"].ToString(),
                        CopyUrl = CommonHelper.GetSongUrl("xm", "320", j["song_id"].ToString(), "mp3"),

                        PicUrl = j["album_logo"].ToString().Replace("_1.", "_4."),
                        LrcUrl = CommonHelper.GetSongUrl("xm", "128", j["song_id"].ToString(), "lrc"),
                        TrcUrl = "",
                        KrcUrl = "",

                        MvId = j["mv_id"]?.ToString(),
                        MvHdUrl = "",
                        MvLdUrl = "",

                        Language = "",
                        Company = "",
                        Year = "",
                        Disc = "1",
                        TrackNum = "",
                        Type = "xm"
                    };
                    if (string.IsNullOrEmpty(song.LqUrl))
                    {
                        song.LqUrl = CommonHelper.GetSongUrl("xm", "128", j["song_id"].ToString(), "mp3");
                    }
                    if (!string.IsNullOrEmpty(song.MvId))
                    {
                        if (song.MvId != "0")
                        {
                            song.MvHdUrl = CommonHelper.GetSongUrl("xm", "hd", song.SongId, "mp4");
                            song.MvLdUrl = CommonHelper.GetSongUrl("xm", "ld", song.SongId, "mp4");
                        }
                    }
                    result.Songs.Add(song);
                }
                return result;
            }
            catch (Exception ex)
            {
                CommonHelper.AddLog(ex);
                result.ErrorCode = 500;
                result.ErrorMsg = "解析歌单出现异常";
                return result;
            }
        }

        private static string Jurl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return "";
            }
            var num = Convert.ToInt32(url.Substring(0, 1));
            var newurl = url.Substring(1);
            var yushu = newurl.Length % num;
            var colunms = (int)Math.Ceiling((double)newurl.Length / num);
            var arrList = new string[num];
            var a = 0;
            for (var i = 0; i < num; i++)
            {
                if (i < yushu)
                {
                    arrList[i] = newurl.Substring(a, colunms);
                    a += colunms;
                }
                else
                {
                    if (yushu == 0)
                    {
                        arrList[i] = newurl.Substring(a, colunms);
                        a += colunms;
                    }
                    else
                    {
                        arrList[i] = newurl.Substring(a, colunms - 1);
                        a += (colunms - 1);
                    }
                }
            }
            var sb = new StringBuilder();
            if (yushu == 0)
            {
                for (var i = 0; i < colunms; i++)
                {
                    for (var j = 0; j < num; j++)
                    {
                        sb.Append(arrList[j].Substring(i, 1));
                    }
                }
            }
            else
            {
                for (var i = 0; i < colunms; i++)
                {
                    if (i == colunms - 1)
                    {
                        for (var j = 0; j < yushu; j++)
                        {
                            sb.Append(arrList[j].Substring(i, 1));
                        }
                    }
                    else
                    {
                        for (var j = 0; j < num; j++)
                        {
                            sb.Append(arrList[j].Substring(i, 1));
                        }
                    }
                }
            }
            var str = WebUtility.UrlDecode(sb.ToString());
            return str?.Replace("^", "0").Replace("+", " ").Replace(".mp$", "mp3");
        }

        private static string GetUrl(string id, string quality, string format)
        {
            if (format == "mp4" || format == "flv")
            {
                string mvId;
                string html;
                if (Regex.IsMatch(id, @"^\d+$"))
                {
                    var url = "http://www.xiami.com/song/" + id;
                    html = CommonHelper.GetHtmlContent(url);
                    if (string.IsNullOrEmpty(html))
                    {
                        return "";
                    }
                    mvId = Regex.Match(html, @"(?<=href=""/mv/)\w+(?="")").Value;
                }
                else
                {
                    mvId = id;
                }
                if (string.IsNullOrEmpty(mvId))
                {
                    return null;
                }
                html = CommonHelper.GetHtmlContent("http://m.xiami.com/mv/" + mvId, 2);
                return string.IsNullOrEmpty(html) ? "" : Regex.Match(html, @"(?<=<video src="")[^""]+(?=""\s*poster=)").Value;
            }
            var song = SearchSong(id);
            if (song == null)
            {
                return null;
            }
            if (format == "lrc")
            {
                return song.LrcUrl;
            }
            if (format == "jpg")
            {
                return song.PicUrl;
            }
            if (string.IsNullOrEmpty(song.LqUrl) && string.IsNullOrEmpty(song.HqUrl))
            {
                song = TtMusic.GetXmSqUrl(song.ArtistName, song.SongName);
            }
            return quality != "128" ? song.SqUrl : song.LqUrl;
        }

        public SearchResult SongSearch(string key, int page, int size)
        {
            return Search(key, page);
        }

        public AlbumResult AlbumSearch(string id)
        {
            return SearchAlbum(id);
        }

        public ArtistResult ArtistSearch(string id, int page, int size)
        {
            return SearchArtist(id, page);
        }

        public CollectResult CollectSearch(string id, int page, int size)
        {
            return SearchCollect(id);
        }

        public SongResult GetSingleSong(string id)
        {
            return SearchSong(id);
        }

        public string GetSongUrl(string id, string quality, string format)
        {
            return GetUrl(id, quality, format);
        }
    }
}