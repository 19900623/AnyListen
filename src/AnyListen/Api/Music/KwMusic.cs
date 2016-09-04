using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using AnyListen.Helper;
using AnyListen.Interface;
using AnyListen.Model;
using Newtonsoft.Json.Linq;

namespace AnyListen.Api.Music
{
    public class KwMusic : IMusic
    {
        public static SearchResult Search(string key, int page, int size)
        {
            var result = new SearchResult
            {
                ErrorCode = 200,
                ErrorMsg = "OK",
                KeyWord = key,
                PageNum = page,
                TotalSize = -1,
                Songs = new List<SongResult>()
            };
            var url = "http://search.kuwo.cn/r.s?client=kt&all=" + key + "&pn=" + (page - 1) + "&rn=" 
                + size +"&ft=music&plat=pc&cluster=1&result=json&rformat=json&ver=mbox&show_copyright_off=1&vipver=MUSIC_8.1.2.0_W4&encoding=utf8";
            var html = CommonHelper.GetHtmlContent(url, 0, null, false);
            if (string.IsNullOrEmpty(html))
            {
                result.ErrorCode = 300;
                result.ErrorMsg = "获取搜索结果信息失败";
                return result;
            }
            try
            {
                var json = JObject.Parse(html);
                if (json["TOTAL"].ToString() == "0")
                {
                    result.ErrorCode = 404;
                    result.ErrorMsg = "没有找到符合要求的歌曲";
                    return result;
                }
                result.TotalSize = Convert.ToInt32(json["TOTAL"].ToString());
                var datas = json["abslist"];
                result.Songs = GetListByJson(datas);
                return result;
            }
            catch (Exception ex)
            {
                CommonHelper.AddLog(ex);
                result.ErrorCode = 500;
                result.ErrorMsg = "解析歌曲时发生错误";
                return result;
            }
        }

        private static List<SongResult> GetListByJson(JToken datas)
        {
            var list = new List<SongResult>();
            foreach (JToken j in datas)
            {
                var song = new SongResult
                {
                    SongId = j["MUSICRID"].ToString().Replace("MUSIC_", ""),
                    SongName = j["SONGNAME"].ToString(),
                    SongSubName = j["ALIAS"]?.ToString() ?? "",
                    SongLink = "",

                    ArtistId = j["ARTISTID"]?.ToString() ?? "",
                    ArtistName = (j["ARTIST"]?.ToString() ?? "").Replace("&", ";"),
                    ArtistSubName = "",

                    AlbumId = j["ALBUMID"]?.ToString() ?? "0",
                    AlbumName = j["ALBUM"]?.ToString() ?? "",
                    AlbumSubName = "",
                    AlbumArtist = (j["ARTIST"]?.ToString() ?? "").Replace("+", ";"),

                    Length = CommonHelper.NumToTime(j["DURATION"]?.ToString() ?? "0"),
                    Size = "",
                    BitRate = "128K",

                    FlacUrl = "",
                    ApeUrl = "",
                    WavUrl = "",
                    SqUrl = "",
                    HqUrl = "",
                    LqUrl = "",
                    CopyUrl = "",

                    PicUrl = "",
                    LrcUrl = "",
                    TrcUrl = "",
                    KrcUrl = "",

                    MvId = "",
                    MvHdUrl = "",
                    MvLdUrl = "",

                    Language = j["LANGUAGE"]?.ToString(),
                    Company = j["COMPANY"]?.ToString(),
                    Year = j["RELEASEDATE"]?.ToString(),
                    Disc = "1",
                    TrackNum = "",
                    Type = "kw"
                };
                if (string.IsNullOrEmpty(song.Year))
                {
                    try
                    {
                        song.Year = j["TIMESTAMP"].ToString().Substring(0, 10);
                    }
                    catch (Exception)
                    {
                        //
                    }
                }
                song.PicUrl = CommonHelper.GetSongUrl("kw", "320", song.SongId, "jpg");
                song.LrcUrl = CommonHelper.GetSongUrl("kw", "320", song.SongId, "lrc");
                var format = j["FORMATS"]?.ToString() ?? j["formats"].ToString();
                if (format.Contains("MP3128"))
                {
                    song.BitRate = "128K";
                    song.CopyUrl = song.LqUrl = CommonHelper.GetSongUrl("kw", "128", song.SongId, "mp3");
                }
                if (format.Contains("MP3192"))
                {
                    song.BitRate = "192K";
                    song.CopyUrl = song.HqUrl = CommonHelper.GetSongUrl("kw", "192", song.SongId, "mp3");
                }
                if (format.Contains("MP3H"))
                {
                    song.BitRate = "320K";
                    song.CopyUrl = song.SqUrl = CommonHelper.GetSongUrl("kw", "320", song.SongId, "mp3");
                }
                if (format.Contains("AL"))
                {
                    song.BitRate = "无损";
                    song.ApeUrl = CommonHelper.GetSongUrl("kw", "1000", song.SongId, "ape");
                }
                if (format.Contains("MP4"))
                {
                    song.MvHdUrl = song.MvLdUrl = CommonHelper.GetSongUrl("kw", "1000", song.SongId, "mp4");
                }
                if (format.Contains("MV"))
                {
                    song.MvLdUrl = CommonHelper.GetSongUrl("kw", "1000", song.SongId, "mkv");
                }
                
                list.Add(song);
            }
            return list;
        }

        private static AlbumResult SearchAlbum(string id)
        {
            var result = new AlbumResult
            {
                ErrorCode = 200,
                ErrorMsg = "OK",
                AlbumLink = "",
                Songs = new List<SongResult>()
            };
            var url = "http://search.kuwo.cn/r.s?stype=albuminfo&albumid=" + id + "&client=kt&plat=pc&cluster=1&ver=mbox&show_copyright_off=1&vipver=MUSIC_8.1.2.0_W4&encoding=utf8";
            var html = CommonHelper.GetHtmlContent(url, 0, null, false);
            if (string.IsNullOrEmpty(html) || html == "{}")
            {
                result.ErrorCode = 300;
                result.ErrorMsg = "获取专辑信息失败";
                return result;
            }
            try
            {
                var json = JObject.Parse(html);
                var al = json["name"].ToString();
                var ar = json["artist"].ToString();
                var lu = json["lang"].ToString();
                var pic = "http://img3.kuwo.cn/star/albumcover/" + json["pic"].ToString().Replace("120/", "500/");
                var year = json["pub"].ToString();
                var cmp = json["company"].ToString();
                result.Songs = GetSongsByToken(json["musiclist"]);
                result.AlbumInfo = json["info"].ToString();

                for (var i = 0; i < result.Songs.Count; i++)
                {
                    result.Songs[i].AlbumId = id;
                    result.Songs[i].AlbumName = al;
                    result.Songs[i].AlbumArtist = ar;
                    result.Songs[i].Language = lu;
                    result.Songs[i].PicUrl = pic;
                    result.Songs[i].TrackNum = (i + 1).ToString();
                    result.Songs[i].Year = year;
                    result.Songs[i].Company = cmp;
                }
                return result;
            }
            catch (Exception ex)
            {
                CommonHelper.AddLog(ex);
                result.ErrorCode = 500;
                result.ErrorMsg = "专辑解析失败";
                return result;
            }
        }

        private static List<SongResult> GetSongsByToken(JToken datas)
        {
            var list = new List<SongResult>();
            foreach (JToken j in datas)
            {
                var song = new SongResult
                {
                    SongId = j["id"].ToString(),
                    SongName = j["name"].ToString(),
                    SongSubName = "",
                    SongLink = "",

                    ArtistId = j["artistid"]?.ToString() ?? "",
                    ArtistName = (j["artist"]?.ToString() ?? "").Replace("&", ";"),
                    ArtistSubName = "",

                    AlbumId = j["albumid"]?.ToString() ?? "",
                    AlbumName = j["album"]?.ToString() ?? "",
                    AlbumSubName = "",
                    AlbumArtist = (j["artist"]?.ToString() ?? "").Replace("+", ";"),

                    Length = CommonHelper.NumToTime(j["duration"]?.ToString() ?? "0"),
                    Size = "",
                    BitRate = "128K",

                    FlacUrl = "",
                    ApeUrl = "",
                    WavUrl = "",
                    SqUrl = "",
                    HqUrl = "",
                    LqUrl = "",
                    CopyUrl = "",

                    PicUrl = "",
                    LrcUrl = "",
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
                    Type = "kw"
                };
                song.PicUrl = CommonHelper.GetSongUrl("kw", "320", song.SongId, "jpg");
                song.LrcUrl = CommonHelper.GetSongUrl("kw", "320", song.SongId, "lrc");
                var format = j["FORMATS"]?.ToString() ?? j["formats"].ToString();
                if (format.Contains("MP3128"))
                {
                    song.BitRate = "128K";
                    song.CopyUrl = song.LqUrl = CommonHelper.GetSongUrl("kw", "128", song.SongId, "mp3");
                }
                if (format.Contains("MP3192"))
                {
                    song.BitRate = "192K";
                    song.CopyUrl = song.HqUrl = CommonHelper.GetSongUrl("kw", "192", song.SongId, "mp3");
                }
                if (format.Contains("MP3H"))
                {
                    song.BitRate = "320K";
                    song.CopyUrl = song.SqUrl = CommonHelper.GetSongUrl("kw", "320", song.SongId, "mp3");
                }
                if (format.Contains("AL"))
                {
                    song.BitRate = "无损";
                    song.ApeUrl = CommonHelper.GetSongUrl("kw", "1000", song.SongId, "ape");
                }
                if (format.Contains("MP4"))
                {
                    song.MvHdUrl = song.MvLdUrl = CommonHelper.GetSongUrl("kw", "1000", song.SongId, "mp4");
                }
                if (format.Contains("MV"))
                {
                    song.MvLdUrl = CommonHelper.GetSongUrl("kw", "1000", song.SongId, "mkv");
                }
                list.Add(song);
            }
            return list;
        }

        private static ArtistResult SearchArtist(string id, int page, int size)
        {
            var url = "http://search.kuwo.cn/r.s?ft=music&itemset=newkw&newsearch=1&cluster=0&rn=" + size + "&pn=" +
                      (page - 1) + "&primitive=0&rformat=json&encoding=UTF8&artist=" + id;
            if (Regex.IsMatch(id, @"^\d+$"))
            {
                url = "http://search.kuwo.cn/r.s?ft=music&itemset=newkw&newsearch=1&cluster=0&rn=" + size + "&pn=" +
                      (page - 1) + "&primitive=0&rformat=json&encoding=UTF8&artistid=" + id;
            }
            var html = CommonHelper.GetHtmlContent(url, 0, null, false);
            var result = new ArtistResult
            {
                ErrorCode = 200,
                ErrorMsg = "OK",
                ArtistLink = "",
                Page = page,
                Songs = new List<SongResult>()
            };
            if (string.IsNullOrEmpty(html) || html == "null")
            {
                result.ErrorCode = 300;
                result.ErrorMsg = "获取源代码失败";
                return result;
            }
            try
            {
                var json = JObject.Parse(html);
                if (json["TOTAL"].ToString() == "0")
                {
                    result.ErrorCode = 404;
                    result.ErrorMsg = "请检查艺术家ID是否正确";
                    return null;
                }
                var datas = json["abslist"];
                result.Songs = GetListByJson(datas);
                try
                {
                    html = CommonHelper.GetHtmlContent("http://search.kuwo.cn/r.s?stype=artistinfo&artist=" + id, 0, null, false);
                    if (Regex.IsMatch(id, @"^\d+$"))
                    {
                        html = CommonHelper.GetHtmlContent("http://search.kuwo.cn/r.s?stype=artistinfo&artistid=" + id);
                    }
                    json = JObject.Parse(html);
                    result.ArtistInfo = json["info"].ToString();
                    result.ArtistLogo = "http://star.kuwo.cn/star/starheads/" + json["pic"].ToString().Replace("120/", "500/");
                    result.AlbumSize = Convert.ToInt32(json["data"]["albumnum"]);
                    result.SongSize = Convert.ToInt32(json["data"]["musicnum"]);
                    result.TransName = json["name"].ToString();
                }
                catch (Exception ex)
                {
                    CommonHelper.AddLog(ex);
                }
                return result;
            }
            catch (Exception ex)
            {
                CommonHelper.AddLog(ex);
                result.ErrorCode = 500;
                result.ErrorMsg = "解析歌曲时发生错误";
                return result;
            }
        }

        private static CollectResult SearchCollect(string id, int page, int size)
        {
            var url = "http://nplserver.kuwo.cn/pl.svc?op=getlistinfo&pid=" + id + "&pn=" + (page - 1) + "&rn=" + size +
                      "&encode=utf-8&keyset=pl2012&identity=kuwo";
            var html = CommonHelper.GetHtmlContent(url, 0, null, false);
            var result = new CollectResult
            {
                ErrorCode = 200,
                ErrorMsg = "OK",
                CollectId = id,
                CollectLink = "",
                Songs = new List<SongResult>()
            };
            if (string.IsNullOrEmpty(html) || html == "{}")
            {
                result.ErrorCode = 300;
                result.ErrorMsg = "获取歌单信息失败";
                return result;
            }
            var json = JObject.Parse(html);
            try
            {
                result.Songs = GetSongsByToken(json["musiclist"]);

                result.CollectName = json["title"].ToString();
                result.CollectLogo = json["pic"].ToString();
                result.CollectMaker = json["uname"].ToString();
                result.CollectInfo = json["info"].ToString();
                result.Tags = "";
                result.SongSize = json["total"].Value<int>();
                result.Date = CommonHelper.UnixTimestampToDateTime(Convert.ToInt64(json["abstime"])).ToString("yyyy-MM-dd");
                return result;
            }
            catch (Exception ex)
            {
                CommonHelper.AddLog(ex);
                result.ErrorCode = 500;
                result.ErrorMsg = "解析歌单时发生错误";
                return result;
            }
        }

        private static SongResult SearchSong(string id)
        {
            var html = CommonHelper.GetHtmlContent("http://search.kuwo.cn/r.s?rformat=json&RID=MUSIC_" + id, 0, null, false);
            if (string.IsNullOrEmpty(html))
            {
                return null;
            }
            var json = JObject.Parse(html);
            var list = GetSongsByToken(json["abslist"]);
            var song = list?[0];
            if (song == null) return null;
            html = CommonHelper.GetHtmlContent("http://www.kuwo.cn/webmusic/st/getMuiseByRid?rid=MUSIC_" + id + "&flag=1");
            if (string.IsNullOrEmpty(html)) return song;
            json = JObject.Parse(html);
            song.SongName = json["songName"].ToString();
            song.ArtistName = json["artist"].ToString();
            song.AlbumName = json["album"].ToString();
            song.AlbumArtist = json["songName"].ToString();
            song.Length = CommonHelper.NumToTime(json["duration"].ToString());
            return song;
        }

        private static string GetUrl(string id, string quality, string format)
        {
            if (format == "lrc")
            {
                var html =
                    CommonHelper.GetHtmlContent("http://mobile.kuwo.cn/mpage/html5/songinfoandlrc?mid=" + id + "&flag=0");
                if (string.IsNullOrEmpty(html))
                {
                    return null;
                }
                var j = JObject.Parse(html);
                if (string.IsNullOrEmpty(j["lrclist"]?.ToString()))
                {
                    return null;
                }
                var name = j["songinfo"]["name"]?.ToString();
                var ar = j["songinfo"]["artist"]?.ToString();
                var sb = new StringBuilder();
                foreach (JToken jToken in j["lrclist"])
                {
                    sb.AppendLine("[" + CommonHelper.NumToTime(jToken["timeId"].ToString()) + ".00]" + jToken["text"]);
                }
                if (string.IsNullOrEmpty(sb.ToString()))
                {
                    return null;
                }
                return "[ti:" + name + "]\n[ar: " + ar + "]\n[by: 雅音FM]\n" + sb;
            }
            if (format == "jpg")
            {
                var html =
                    CommonHelper.GetHtmlContent("http://player.kuwo.cn/webmusic/sj/dtflagdate?flag=6&rid=MUSIC_" + id);
                if (string.IsNullOrEmpty(html))
                {
                    return "http://yyfm.oss-cn-qingdao.aliyuncs.com/img/mspy.jpg";
                }
                var strs = html.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (html.Contains("star/albumcover"))
                {
                    return strs[strs.Length - 1].Replace("albumcover/120", "albumcover/500");
                }
                return strs[1].Replace("starheads/120", "starheads/500");
            }
            if (format == "mkv")
            {
                return "http://antiserver.kuwo.cn/anti.s?rid=MUSIC_" + id + "&response=res&format=mkv&type=convert_url";
            }
            if (format == "mp4")
            {
                return "http://antiserver.kuwo.cn/anti.s?rid=MUSIC_" + id + "&response=res&format=mp4&type=convert_url";
            }
            if (format == "ape")
            {
                return "http://antiserver.kuwo.cn/anti.s?rid=MUSIC_" + id + "&response=res&format=ape&type=convert_url";
            }
            return
                    "http://antiserver.kuwo.cn/anti.s?type=convert_url&br=" + quality + "kmp3&format=mp3&rid=MUSIC_" + id + "&response=res";
        }

        public SearchResult SongSearch(string key, int page, int size)
        {
            return Search(key, page, size);
        }

        public AlbumResult AlbumSearch(string id)
        {
            return SearchAlbum(id);
        }

        public ArtistResult ArtistSearch(string id, int page, int size)
        {
            return SearchArtist(id, page, size);
        }

        public CollectResult CollectSearch(string id, int page, int size)
        {
            return SearchCollect(id, page, size);
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