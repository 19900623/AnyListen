using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using AnyListen.Helper;
using AnyListen.Interface;
using AnyListen.Model;
using Newtonsoft.Json.Linq;

namespace AnyListen.Api.Music
{
    public class TxMusic : IMusic
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
            var url = "http://soso.music.qq.com/fcgi-bin/search_cp?aggr=0&catZhida=0&lossless=1&sem=1&w=" + key + "&n=" + size + "&t=0&p=" + page + "&remoteplace=sizer.yqqlist.song&g_tk=5381&loginUin=0&hostUin=0&format=jsonp&inCharset=GB2312&outCharset=utf-8&notice=0&platform=yqq&needNewCode=0";
            var html = CommonHelper.GetHtmlContent(url);
            if (string.IsNullOrEmpty(html))
            {
                result.ErrorCode = 300;
                result.ErrorMsg = "获取搜索结果信息失败";
                return result;
            }
            try
            {
                var json = JObject.Parse(html.Replace("callback(", "").TrimEnd(')'));
                if (json["data"]["song"]["totalnum"].ToString() == "0")
                {
                    result.ErrorCode = 404;
                    result.ErrorMsg = "没有找到符合要求的歌曲";
                    return result;
                }
                var datas = json["data"]["song"]["list"];
                result.TotalSize = json["data"]["song"]["totalnum"].Value<int>();
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

        private static List<SongResult> GetListByJson(JToken songs, bool isArtist = false)
        {
            if (songs == null)
            {
                return null;
            }
            var list = new List<SongResult>();
            foreach (JToken token in songs)
            {
                var j = isArtist ? token["musicData"] : token;
                var ar =
                    j["singer"].Aggregate("", (current, fJToken) => current + fJToken["name"].ToString() + ";")
                        .TrimEnd(';');
                var song = new SongResult
                {
                    SongId = j["songid"].ToString(),
                    SongName = j["songname"].ToString(),
                    SongSubName = "",
                    SongLink = "http://y.qq.com/#type=song&mid=" + j["songmid"],

                    ArtistId = j["singer"].First?["id"]?.ToString(),
                    ArtistName = ar,
                    ArtistSubName = "",

                    AlbumId = j["albummid"].ToString(),
                    AlbumName = j["albumname"].ToString(),
                    AlbumSubName = "",
                    AlbumArtist = j["singer"].First?["name"].ToString(),

                    Length = CommonHelper.NumToTime(j["interval"].ToString()),
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
                    LrcUrl = CommonHelper.GetSongUrl("qq", "128", j["songid"].ToString(), "lrc"),
                    TrcUrl = "",
                    KrcUrl = "",

                    MvId = j["vid"].ToString(),
                    MvHdUrl = "",
                    MvLdUrl = "",

                    Language = "",
                    Company = "",
                    Year = j["pubtime"] == null ? "" : CommonHelper.UnixTimestampToDateTime(Convert.ToInt64(j["pubtime"].ToString())).ToString("yyyy-MM-dd"),
                    Disc = "1",
                    TrackNum = j["belongCD"]?.ToString(),
                    Type = "qq"
                };
                var mid = j["strMediaMid"]?.ToString() ?? j["songmid"].ToString();
                if (j["size128"].ToString() != "0")
                {
                    song.BitRate = "128K";
                    song.LqUrl = CommonHelper.GetSongUrl("qq", "128", mid, "mp3");
                }
                if (j["sizeogg"].ToString() != "0")
                {
                    song.BitRate = "192K";
                    song.HqUrl = "http://stream.qqmusic.tc.qq.com/" + (Convert.ToInt32(song.SongId) + 40000000) + ".ogg";
                }
                if (j["size320"].ToString() != "0")
                {
                    song.BitRate = "320K";
                    song.SqUrl = CommonHelper.GetSongUrl("qq", "320", mid, "mp3");
                }
                if (j["sizeape"].ToString() != "0")
                {
                    song.BitRate = "无损";
                    song.ApeUrl = "http://stream.qqmusic.tc.qq.com/A000" + mid + ".ape";
                }
                if (j["sizeflac"].ToString() != "0")
                {
                    song.BitRate = "无损";
                    song.FlacUrl = "http://stream.qqmusic.tc.qq.com/F000" + mid + ".flac";
                }
                song.CopyUrl = CommonHelper.GetSongUrl("qq", "320", mid, "mp3");
                if (!string.IsNullOrEmpty(song.MvId))
                {
                    song.MvHdUrl = CommonHelper.GetSongUrl("qq", "hd", song.MvId, "mp4");
                    song.MvLdUrl = CommonHelper.GetSongUrl("qq", "ld", song.MvId, "mp4");
                }
                song.PicUrl = "http://i.gtimg.cn/music/photo/mid_album_500/" + song.AlbumId[song.AlbumId.Length - 2] +
                              "/" + song.AlbumId[song.AlbumId.Length - 1] + "/" + song.AlbumId + ".jpg";
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
                AlbumLink = "http://h.dongting.com/yule/app/music_album.html?id=" + id,
                Songs = new List<SongResult>()
            };
            var str = "albummid=" + id;
            if (Regex.IsMatch(id, @"^\d+$"))
            {
                str = "albumid=" + id;
            }
            var url =
                "http://i.y.qq.com/v8/fcg-bin/fcg_v8_album_info_cp.fcg?" + str +
                "&g_tk=5381&uin=0&format=jsonp&inCharset=utf-8&outCharset=utf-8&notice=0&platform=h5&needNewCode=1";
            var html = CommonHelper.GetHtmlContent(url);
            if (string.IsNullOrEmpty(html))
            {
                result.ErrorCode = 300;
                result.ErrorMsg = "获取专辑信息失败";
                return result;
            }
            try
            {
                var json = JObject.Parse(html.Replace("callback(", "").TrimEnd(')'));
                if (json["message"].ToString() != "succ")
                {
                    result.ErrorCode = 404;
                    result.ErrorMsg = "请检查专辑ID是否正确";
                    return result;
                }
                result.AlbumType = json["data"]["genre"].ToString();
                result.AlbumInfo = json["data"]["desc"].ToString();

                var mid = json["data"]["mid"].ToString();
                result.AlbumLink = "http://y.qq.com/#type=album&mid=" + mid;

                var datas = json["data"]["list"];
                var year = json["data"]["aDate"].ToString();
                var cmp = json["data"]["company"].ToString();
                var lug = json["data"]["lan"].ToString();
                var ar = json["data"]["singername"].ToString();
                result.Songs = GetListByJson(datas);
                var index = 0;
                foreach (var r in result.Songs)
                {
                    index++;
                    r.TrackNum = index.ToString();
                    r.Year = year;
                    r.Company = cmp;
                    r.Language = lug;
                    r.AlbumArtist = ar;
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
            var str = "singermid=" + id;
            if (Regex.IsMatch(id, @"^\d+$"))
            {
                str = "singerid=" + id;
            }
            var url =
                "http://i.y.qq.com/v8/fcg-bin/fcg_v8_singer_track_cp.fcg?order=listen&begin=" + (page - 1)*size +
                "&num=" + size + "&"+str+"&g_tk=5381&uin=0&format=jsonp&inCharset=utf-8&outCharset=utf-8&notice=0&platform=h5page&needNewCode=1&from=h5";
            var html = CommonHelper.GetHtmlContent(url).Replace("callback(", "").TrimEnd(')');
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
                if (json["message"].ToString() != "succ")
                {
                    result.ErrorCode = 404;
                    result.ErrorMsg = "请检查艺术家ID是否正确";
                    return null;
                }
                var datas = json["data"]["list"];
                result.Songs = GetListByJson(datas,true);
                var mid = json["data"]["singer_mid"].ToString();
                try
                {
                    result.ArtistInfo = json["data"]["SingerDesc"].ToString();
                    result.ArtistLink = "http://y.qq.com/#type=singer&mid=" + mid;
                    result.ArtistLogo = "http://i.gtimg.cn/music/photo_new/T001R500x500M000"+ mid + ".jpg";
                    result.AlbumSize = -1;
                    result.SongSize = json["data"]["total"].Value<int>();
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

        private static CollectResult SearchCollect(string id)
        {

            var url = "http://i.y.qq.com/qzone-music/fcg-bin/fcg_ucc_getcdinfo_byids_cp.fcg?type=1&json=1&utf8=1&onlysong=0&nosign=1&disstid=" + id + "&g_tk=5381&loginUin=0&hostUin=0&format=jsonp&inCharset=GB2312&outCharset=utf-8&notice=0&platform=yqq&jsonpCallback=jsonCallback&needNewCode=0";
            var html = CommonHelper.GetHtmlContent(url);
            var result = new CollectResult
            {
                ErrorCode = 200,
                ErrorMsg = "OK",
                CollectId = id,
                CollectLink = "http://y.qq.com/#type=taoge&id=" + id,
                Songs = new List<SongResult>()
            };
            if (string.IsNullOrEmpty(html) || html == "null")
            {
                result.ErrorCode = 300;
                result.ErrorMsg = "获取歌单信息失败";
                return result;
            }
            var json = JObject.Parse(html.Replace("jsonCallback(", "").TrimEnd(')'));
            if (json["cdlist"] == null)
            {
                result.ErrorCode = 404;
                result.ErrorMsg = "请检查歌单ID是否正确";
                return result;
            }
            try
            {
                var datas = json["cdlist"].First["songlist"];
                result.Songs = GetListByJson(datas);
                result.CollectName = json["cdlist"].First["dissname"].ToString();
                result.CollectLogo = json["cdlist"].First["logo"].ToString();
                result.CollectMaker = json["cdlist"].First["nick"].ToString();
                result.CollectInfo = json["cdlist"].First["desc"].ToString();
                var tags = json["cdlist"].First["tags"].Aggregate("", (current, t) => current + (t["name"].ToString() + ";"));
                result.Tags = tags.Trim(';');
                result.SongSize = json["cdlist"].First["cur_song_num"].Value<int>();
                result.Date =
                    CommonHelper.UnixTimestampToDateTime(Convert.ToInt64(json["cdlist"].First["ctime"].ToString()))
                        .ToString("yyyy-MM-dd");
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
            try
            {
                var str = "songmid=" + id;
                if (Regex.IsMatch(id, @"^\d+$"))
                {
                    str = "songid=" + id;
                }
                var url = "http://c.y.qq.com/v8/playsong.html?" + str;
                var html = CommonHelper.GetHtmlContent(url, 2);
                if (string.IsNullOrEmpty(html))
                {
                    return null;
                }
                var j = JObject.Parse(Regex.Match(html, @"(?<=var song = )({[\s\S]+?})(?=,\s*totalTime)").Value);
                var list = SearchAlbum(j["albummid"].ToString());
                return list?.Songs.SingleOrDefault(t => t.SongId == j["songid"].ToString());
            }
            catch (Exception ex)
            {
                CommonHelper.AddLog(ex);
                return null;
            }
        }

        private static string GetUrl(string id, string quality, string format)
        {
            switch (format)
            {
                case "lrc":
                    if (!Regex.IsMatch(id, @"^\d+$"))
                    {
                        id = SearchSong(id).SongId;
                    }
                    var url =
                        "http://lyric.music.qq.com/fcgi-bin/fcg_query_lyric.fcg?nobase64=1&musicid=" + id + "&callback=jsonp1";
                    var html = CommonHelper.GetHtmlContent(url, 1, new Dictionary<string, string>
                    {
                        {"Host", "lyric.music.qq.com"},
                        {"Referer", "http://lyric.music.qq.com"},
                    });
                    if (string.IsNullOrEmpty(html))
                    {
                        return "";
                    }
                    var json = JObject.Parse(html.Replace("jsonp1(", "").TrimEnd(')'));
                    return json["retcode"].ToString() != "0" ? null : json["lyric"].ToString();
                case "mp4":
                case "flv":
                    return GetMvUrl(id,quality);
                case "jpg":
                    return SearchSong(id).PicUrl;
            }

            if (Regex.IsMatch(id, @"^\d+$"))
            {
                switch (format)
                {
                    case "ape":
                        return "http://stream.qqmusic.tc.qq.com/" + (Convert.ToInt32(id) + 80000000) + ".ape";
                    case "flac":
                        return "http://stream.qqmusic.tc.qq.com/" + (Convert.ToInt32(id) + 70000000) + ".flac";
                    case "ogg":
                        return "http://stream.qqmusic.tc.qq.com/" + (Convert.ToInt32(id) + 40000000) + ".ogg";
                    case "mp3":
                        switch (quality)
                        {
                            case "128":
                                return "http://stream.qqmusic.tc.qq.com/" + (Convert.ToInt32(id) + 30000000) + ".mp3";
                            case "192":
                                return "http://stream.qqmusic.tc.qq.com/" + (Convert.ToInt32(id) + 40000000) + ".ogg";
                        }
                        return "http://stream.qqmusic.tc.qq.com/" + id + ".mp3";
                    default:
                        return "http://stream.qqmusic.tc.qq.com/" + id + ".mp3";
                }
            }
            switch (format)
            {
                case "ape":
                    return "http://stream.qqmusic.tc.qq.com/A000" + id + ".ape";
                case "flac":
                    return "http://stream.qqmusic.tc.qq.com/F000" + id + ".flac";
                case "ogg":
                    return "http://stream.qqmusic.tc.qq.com/M800" + id + ".mp3";
                default:
                    var time = new Random(DateTime.Now.Millisecond).NextDouble();
                    var key = GetKey(time.ToString(CultureInfo.CurrentCulture));
                    return quality == "128"
                        ? "http://cc.stream.qqmusic.qq.com/M500" + id + ".mp3?vkey=" + key + "&guid=" + time +
                          "&fromtag=0"
                        : "http://cc.stream.qqmusic.qq.com/M800" + id + ".mp3?vkey=" + key + "&guid=" + time +
                          "&fromtag=0";
            }
        }

        private static string GetMvUrl(string id, string quality)
        {
            //此处使用腾讯视频会员Cookie可获取1080P资源
            var html =
                CommonHelper.GetHtmlContent(
                    "http://vv.video.qq.com/getinfo?vid=" + id + "&platform=11&charge=1&otype=json");
            if (string.IsNullOrEmpty(html))
            {
                return null;
            }
            var json = JObject.Parse(html.Replace("QZOutputJson=", "").TrimEnd(';'));
            if (json["fl"] == null)
            {
                return null;
            }
            var dic = json["fl"]["fi"].ToDictionary(jToken => jToken["name"].ToString(),
                jToken => jToken["id"].Value<int>());
            int info;
            if (quality == "hd")
            {
                switch (dic.Count)
                {
                    case 5:
                        info = dic["fhd"];
                        break;
                    case 4:
                        info = dic["shd"];
                        break;
                    case 3:
                        info = dic["hd"];
                        break;
                    default:
                        info = dic["mp4"];
                        break;
                }
            }
            else
            {
                switch (dic.Count)
                {
                    case 5:
                        info = dic["shd"];
                        break;
                    case 4:
                        info = dic["hd"];
                        break;
                    default:
                        info = dic["mp4"];
                        break;
                }
            }
            var vkey = GetVkey(info, id);
            var fn = id + ".p" + (info - 10000) + ".1.mp4";
            return json["vl"]["vi"].First["ul"]["ui"].First["url"] + fn + "?vkey=" + vkey;
        }

        private static string GetVkey(int id, string videoId)
        {
            var fn = videoId + ".p" + (Convert.ToInt32(id) - 10000) + ".1.mp4";
            var url = "http://vv.video.qq.com/getkey?format=" + id + "&otype=json&vid=" + videoId +
                      "&platform=11&charge=1&filename=" + fn;
            var html = CommonHelper.GetHtmlContent(url);
            if (string.IsNullOrEmpty(html))
            {
                return null;
            }
            return Regex.Match(html, @"(?<=key"":"")[^""]+(?="")").Value;
        }

        private static string GetKey(string time)
        {
            var html =
                CommonHelper.GetHtmlContent("http://base.music.qq.com/fcgi-bin/fcg_musicexpress.fcg?json=3&guid=" + time);
            return Regex.Match(html, @"(?<=key"":\s*"")[^""]+").Value;
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
