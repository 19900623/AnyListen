using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AnyListen.Helper;
using AnyListen.Interface;
using AnyListen.Model;
using Newtonsoft.Json.Linq;

namespace AnyListen.Api.Music
{
    public class KgMusic : IMusic
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
            var url = "http://ioscdn.kugou.com/api/v3/search/song?keyword=" + key + "&page=" + page + "&pagesize=" + size + "&showtype=10&plat=2&version=7910&tag=1&correct=1&privilege=1&sver=5";
            var html = CommonHelper.GetHtmlContent(url);
            if (string.IsNullOrEmpty(html))
            {
                result.ErrorCode = 300;
                result.ErrorMsg = "获取搜索结果信息失败";
                return result;
            }
            try
            {
                var json = JObject.Parse(html);
                if (!string.IsNullOrEmpty(json["error"].ToString()) || json["data"]["total"].ToString() == "0")
                {
                    result.ErrorCode = 404;
                    result.ErrorMsg = "没有找到符合要求的歌曲";
                    return result;
                }
                result.TotalSize = json["data"]["total"].Value<int>();
                var datas = json["data"]["info"];
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
                    SongId = j["hash"].ToString(),
                    SongName = j["filename"].ToString(),
                    SongSubName = j["alias"]?.ToString(),
                    SongLink = "",

                    ArtistId = "",
                    ArtistName = (j["singername"]?.ToString() ?? "").Replace("+", ";"),
                    ArtistSubName = "",

                    AlbumId = j["album_id"]?.ToString() ?? "",
                    AlbumName = j["album_name"]?.ToString() ?? "",
                    AlbumSubName = "",
                    AlbumArtist = (j["singername"]?.ToString() ?? "").Replace("+", ";"),

                    Length = CommonHelper.NumToTime(j["duration"].ToString()),
                    Size = "",
                    BitRate = "128K",

                    FlacUrl = "",
                    ApeUrl = "",
                    WavUrl = "",
                    SqUrl = "",
                    HqUrl = "",
                    LqUrl = "",
                    CopyUrl = "",

                    PicUrl = CommonHelper.GetSongUrl("kg", "320", j["hash"].ToString(), "jpg"),
                    LrcUrl = CommonHelper.GetSongUrl("kg", "320", j["hash"].ToString(), "lrc"),
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
                    Type = "kg"
                };
                if (!string.IsNullOrEmpty(song.AlbumId))
                {
                    song.PicUrl = CommonHelper.GetSongUrl("kg", "320", song.AlbumId, "jpg");
                }
                if (string.IsNullOrEmpty(song.ArtistName))
                {
                    if (song.SongName.Contains("-"))
                    {
                        song.ArtistName = song.SongName.Substring(0, song.SongName.IndexOf('-')).Trim();
                        song.SongName = song.SongName.Substring(song.SongName.IndexOf('-') + 1).Trim();
                    }
                }
                else
                {
                    var name = song.SongName.Substring(0, song.SongName.IndexOf('-')).Trim();
                    if (song.ArtistName.Trim() == name)
                    {
                        song.SongName = song.SongName.Substring(song.SongName.IndexOf('-') + 1).Trim();
                    }
                }

                if (!string.IsNullOrEmpty(j["mvhash"].ToString()))
                {
                    song.MvHdUrl = CommonHelper.GetSongUrl("kg", "hd", j["mvhash"].ToString(), "mp4");
                    song.MvLdUrl = CommonHelper.GetSongUrl("kg", "ld", j["mvhash"].ToString(), "mp4");
                }
                if (!string.IsNullOrEmpty(j["hash"].ToString()))
                {
                    song.BitRate = "128K";
                    song.CopyUrl = song.LqUrl = CommonHelper.GetSongUrl("kg", "128", j["hash"].ToString(), "mp3");
                }
                if (!string.IsNullOrEmpty(j["320hash"].ToString()))
                {
                    song.BitRate = "320K";
                    song.CopyUrl = song.SqUrl = CommonHelper.GetSongUrl("kg", "320", j["320hash"].ToString(), "mp3");
                }
                if (!string.IsNullOrEmpty(j["sqhash"].ToString()))
                {
                    song.BitRate = "无损";
                    song.FlacUrl = CommonHelper.GetSongUrl("kg", "1000", j["sqhash"].ToString(), "flac");
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
            var url = "http://ioscdn.kugou.com/api/v3/album/song?albumid=" + id + "&page=1&pagesize=-1&plat=2&version=7910";
            var html = CommonHelper.GetHtmlContent(url);
            if (string.IsNullOrEmpty(html))
            {
                result.ErrorCode = 300;
                result.ErrorMsg = "获取专辑信息失败";
                return result;
            }
            try
            {
                var json = JObject.Parse(html);
                if (json["data"]["total"].ToString() == "0")
                {
                    result.ErrorCode = 404;
                    result.ErrorMsg = "请检查专辑ID是否正确";
                    return result;
                }
                var datas = json["data"]["info"];
                result.Songs = GetListByJson(datas);
                
                html = CommonHelper.GetHtmlContent("http://ioscdn.kugou.com/api/v3/album/info?albumid=" + id + "&version=7910");
                if (string.IsNullOrEmpty(html))
                {
                    return result;
                }
                json = JObject.Parse(html);
                result.AlbumInfo = json["data"]["intro"].ToString();

                var time = json["data"]["publishtime"].ToString().Substring(0, 10);
                var al = json["data"]["albumname"].ToString();
                var singerId = json["data"]["singerid"].ToString();
                var singerName = json["data"]["singername"].ToString();
                var pic = json["data"]["imgurl"].ToString().Replace("{size}", "480");

                for (var i = 0; i < result.Songs.Count; i++)
                {
                    result.Songs[i].ArtistId = singerId;
                    result.Songs[i].ArtistName = singerName;
                    result.Songs[i].AlbumName = al;
                    result.Songs[i].AlbumArtist = singerName;
                    result.Songs[i].TrackNum = (i + 1).ToString();
                    result.Songs[i].Year = time;
                    result.Songs[i].PicUrl = pic;
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

        private static ArtistResult SearchArtist(string id, int page, int size)
        {
            var url = "http://ioscdn.kugou.com/api/v3/singer/song?singerid=" + id + "&page=" + page + "&pagesize=" +
                      size + "&sorttype=2&plat=2&version=7910";
            var html = CommonHelper.GetHtmlContent(url);
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
                if (json["data"]["total"].ToString() == "0")
                {
                    result.ErrorCode = 404;
                    result.ErrorMsg = "请检查艺术家ID是否正确";
                    return null;
                }
                var datas = json["data"]["info"];
                result.Songs = GetListByJson(datas);
                try
                {
                    html = CommonHelper.GetHtmlContent("http://mobilecdn.kugou.com/api/v3/singer/info?singerid="+ id + "&with_res_tag=1");
                    json = JObject.Parse(html);
                    result.ArtistInfo = json["data"]["intro"].ToString();
                    result.ArtistLogo = json["data"]["imgurl"].ToString().Replace("{size}", "480");
                    result.AlbumSize = json["data"]["albumcount"].Value<int>();
                    result.SongSize = json["data"]["songcount"].Value<int>();
                    result.TransName = json["data"]["singername"].ToString();
                    foreach (var song in result.Songs)
                    {
                        song.ArtistName = result.TransName;
                        song.ArtistId = id;
                        song.AlbumArtist = result.TransName;
                    }
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

        private CollectResult SearchCollect(string id, int page, int size)
        {
            var url = "http://m.kugou.com/plist/list/?specialid=" + id + "&page=" + page + "&plat=2&json=true";
            var html = CommonHelper.GetHtmlContent(url);
            var result = new CollectResult
            {
                ErrorCode = 200,
                ErrorMsg = "OK",
                CollectId = id,
                CollectLink = "",
                Songs = new List<SongResult>()
            };
            if (string.IsNullOrEmpty(html) || html == "null")
            {
                result.ErrorCode = 300;
                result.ErrorMsg = "获取歌单信息失败";
                return result;
            }
            var json = JObject.Parse(html);
            if (json["list"]["list"]["total"].ToString() == "0")
            {
                result.ErrorCode = 404;
                result.ErrorMsg = "请检查歌单ID是否正确";
                return result;
            }
            try
            {
                var datas = json["list"]["list"]["info"];
                result.Songs = GetListByJson(datas);

                result.CollectName = json["info"]["list"]["specialname"].ToString();
                result.CollectLogo = json["info"]["list"]["imgurl"].ToString().Replace("{size}","480");
                result.CollectMaker = json["info"]["list"]["nickname"].ToString();
                result.CollectInfo = json["info"]["list"]["intro"].ToString();
                var tags = json["info"]["list"]["tags"].Aggregate("", (current, t) => current + (t["tagname"].ToString() + ";"));
                result.Tags = tags.Trim(';');
                result.SongSize = json["info"]["list"]["songcount"].Value<int>();
                result.Date = json["info"]["list"]["publishtime"].ToString().Substring(0,10);
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

        private SongResult SearchSong(string id)
        {
            var html = CommonHelper.GetHtmlContent("http://m.kugou.com/app/i/getSongInfo.php?hash=" + id + "&album_id=&cmd=playInfo");
            if (string.IsNullOrEmpty(html))
            {
                return null;
            }
            try
            {
                var json = JObject.Parse(html);
                var key = json["fileName"].ToString();
                var list = SongSearch(key, 1, 30);
                if (list == null)
                {
                    return null;
                }
                var song = list.Songs.SingleOrDefault(t => t.SongId == id);
                return song ?? list.Songs[0];
            }
            catch (Exception ex)
            {
                CommonHelper.AddLog(ex);
                return null;
            }
        }

        private string GetUrl(string id, string quality, string format)
        {
            if (format == "jpg" && Regex.IsMatch(id, @"^\d+$"))
            {
                var html = CommonHelper.GetHtmlContent("http://ioscdn.kugou.com/api/v3/album/info?albumid=" + id + "&version=7910");
                if (string.IsNullOrEmpty(html))
                {
                    return "http://yyfm.oss-cn-qingdao.aliyuncs.com/img/mspy.jpg";
                }
                html = CommonHelper.UnicodeToString(html);
                var json = JObject.Parse(html);
                if (string.IsNullOrEmpty(json["data"]?.ToString()))
                {
                    return "http://yyfm.oss-cn-qingdao.aliyuncs.com/img/mspy.jpg";
                }
                return json["data"]["imgurl"].ToString().Replace("{size}", "480");
            }

            if (format == "lrc" || format == "jpg")
            {
                var html =
                    CommonHelper.GetHtmlContent("http://m.kugou.com/app/i/getSongInfo.php?cmd=playInfo&hash=" + id);
                if (string.IsNullOrEmpty(html) || html.Contains("hash error"))
                {
                    return null;
                }
                var json = JObject.Parse(html);
                var songName = json["fileName"].ToString();
                var len = json["timeLength"] + "000";
                if (format == "lrc")
                {
                    html =
                        CommonHelper.GetHtmlContent("http://m.kugou.com/app/i/krc.php?cmd=100&keyword=" + songName +
                                                    "&hash=" + id + "&timelength=" + len + "&d=0.38664927426725626");
                    if (string.IsNullOrEmpty(html))
                    {
                        return "";
                    }
                    return "[ti:" + songName + "]\n[by: 雅音FM]\n" + html;
                }
                html =
                    CommonHelper.GetHtmlContent("http://m.kugou.com/app/i/getSingerHead_new.php?singerName=" +
                                                songName.Split('-')[0].Trim() + "&size=480");
                if (string.IsNullOrEmpty(html) || html.Contains("未能找到"))
                {
                    return "http://yyfm.oss-cn-qingdao.aliyuncs.com/img/mspy.jpg";
                }
                return Regex.Match(html, @"(?<=url"":"")[^""]+").Value.Replace("\\", "");
            }

            if (format == "mp4" || format == "flv")
            {
                var key = CommonHelper.Md5(id + "kugoumvcloud");
                var html =
                    CommonHelper.GetHtmlContent("http://trackermv.kugou.com/interface/index/cmd=100&hash=" + id +
                                                "&key=" + key + "&pid=6&ext=mp4");
                if (string.IsNullOrEmpty(html))
                {
                    return "";
                }
                var json = JObject.Parse(html);
                if (quality == "hd")
                {
                    if (json["mvdata"]["rq"]["downurl"] != null)
                    {
                        return json["mvdata"]["rq"]["downurl"].ToString();
                    }
                    if (json["mvdata"]["sq"]["downurl"] != null)
                    {
                        return json["mvdata"]["sq"]["downurl"].ToString();
                    }
                    if (json["mvdata"]["hd"]["downurl"] != null)
                    {
                        return json["mvdata"]["hd"]["downurl"].ToString();
                    }
                    if (json["mvdata"]["sd"]["downurl"] != null)
                    {
                        return json["mvdata"]["sd"]["downurl"].ToString();
                    }
                }
                else
                {
                    if (json["mvdata"]["sq"]["downurl"] != null)
                    {
                        return json["mvdata"]["sq"]["downurl"].ToString();
                    }
                    if (json["mvdata"]["hd"]["downurl"] != null)
                    {
                        return json["mvdata"]["hd"]["downurl"].ToString();
                    }
                    if (json["mvdata"]["sd"]["downurl"] != null)
                    {
                        return json["mvdata"]["sd"]["downurl"].ToString();
                    }
                }
            }
            var url = "http://trackercdn.kugou.com/i/?key=" + CommonHelper.Md5(id + "kgcloud") + "&cmd=4&acceptMp3=1&hash=" + id + "&pid=1";
            var html1 = CommonHelper.GetHtmlContent(url);
            if (string.IsNullOrEmpty(html1))
            {
                return "";
            }
            return Regex.Match(html1, @"(?<=url"":"")[^""]+").Value.Replace("\\", "");
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