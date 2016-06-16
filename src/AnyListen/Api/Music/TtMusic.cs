using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using AnyListen.Helper;
using AnyListen.Interface;
using AnyListen.Model;
using Newtonsoft.Json.Linq;

namespace AnyListen.Api.Music
{
    public class TtMusic:IMusic
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
            var url = "http://search.dongting.com/song/search?page=" + page + "&user_id=0&tid=0&app=ttpod&size=" + size + "&q=" + key + "&active=0";
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
                if (json["totalCount"].ToString() == "0")
                {
                    result.ErrorCode = 404;
                    result.ErrorMsg = "没有找到符合要求的歌曲";
                    return result;
                }
                result.TotalSize = json["totalCount"].Value<int>();
                var datas = json["data"];
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
            foreach (var j in datas)
            {
                try
                {
                    var song = new SongResult
                    {
                        SongId = j["songId"].ToString(),
                        SongName = j["name"].ToString(),
                        SongSubName = j["alias"]?.ToString(),
                        SongLink = "http://h.dongting.com/yule/app/music_player_page.html?id="+ j["songId"],

                        ArtistId = j["singerId"].ToString(),
                        ArtistName = j["singerName"].ToString(),
                        ArtistSubName = "",

                        AlbumId = j["albumId"].ToString(),
                        AlbumName = j["albumName"].ToString(),
                        AlbumSubName = "",
                        AlbumArtist = j["singerName"].ToString(),

                        Length = "",
                        Size = "",
                        BitRate = "128K",

                        FlacUrl = "",
                        ApeUrl = "",
                        WavUrl = "",
                        SqUrl = "",
                        HqUrl = "",
                        LqUrl = "",
                        CopyUrl = "",

                        PicUrl = j["picUrl"].ToString(),
                        LrcUrl = CommonHelper.GetSongUrl("tt", "320", j["songId"].ToString(), "lrc"),
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
                        Type = "tt"
                    };
                    if (j["mvList"].First != null && j["mvList"].First.HasValues)
                    {
                        var mvs = j["mvList"];
                        var max = 0;
                        foreach (JToken mv in mvs)
                        {
                            song.MvId = mv["videoId"].ToString();
                            if (max == 0)
                            {
                                song.MvHdUrl = mv["url"].ToString();
                                song.MvLdUrl = mv["url"].ToString();
                                max = mv["bitRate"].Value<int>();
                            }
                            else
                            {
                                if (mv["bitRate"].Value<int>() > max)
                                {
                                    song.MvHdUrl = mv["url"].ToString();
                                }
                                else
                                {
                                    song.MvLdUrl = mv["url"].ToString();
                                }
                            }
                        }
                    }
                    var links = j["urlList"];
                    if (links.ToString() == "[]" || j["urlList"].First == null)
                    {
                        continue;
                    }
                    foreach (JToken link in links)
                    {
                        switch (link["bitRate"].ToString())
                        {
                            case "128":
                                song.LqUrl = link["url"].ToString();
                                song.BitRate = "128K";
                                break;
                            case "192":
                                song.HqUrl = link["url"].ToString();
                                song.BitRate = "192K";
                                break;
                            case "320":
                                if (string.IsNullOrEmpty(song.HqUrl))
                                {
                                    song.HqUrl = link["url"].ToString();
                                }
                                song.SqUrl = link["url"].ToString();
                                song.BitRate = "320K";
                                break;
                        }
                        song.Length = link["duration"].ToString().Contains(":") ? link["duration"].ToString() : CommonHelper.NumToTime((link["duration"].Value<int>() / 1000).ToString());
                    }
                    if (j["llList"] != null && j["llList"].ToString() != "null" && j["llList"].ToString() != "[]")
                    {
                        song.BitRate = "无损";
                        foreach (JToken wsJToken in j["llList"])
                        {
                            switch (wsJToken["suffix"].ToString())
                            {
                                case "wav":
                                    song.WavUrl = wsJToken["url"].ToString();
                                    break;
                                case "ape":
                                    song.ApeUrl = wsJToken["url"].ToString();
                                    break;
                                case "flac":
                                    song.FlacUrl = wsJToken["url"].ToString();
                                    break;
                            }
                        }
                    }
                    song.CopyUrl = CommonHelper.GetSongUrl("tt", "320", song.SongId, "mp3");
                    list.Add(song);
                }
                catch (Exception ex)
                {
                    CommonHelper.AddLog(ex);
                }
            }
            return list;
        }

        private static AlbumResult SearchAlbum(string id)
        {
            var result = new AlbumResult
            {
                ErrorCode = 200,
                ErrorMsg = "OK",
                AlbumLink = "http://h.dongting.com/yule/app/music_album.html?id="+id,
                Songs = new List<SongResult>()
            };
            var url = "http://api.dongting.com/song/album/" + id;
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
                if (string.IsNullOrEmpty(json["data"]?.ToString()) || json["data"].ToString() == "null")
                {
                    result.ErrorCode = 404;
                    result.ErrorMsg = "请检查专辑ID是否正确";
                    return result;
                }
                result.AlbumType = json["data"]["typeName"].ToString();
                result.AlbumInfo = json["data"]["description"].ToString();

                var datas = json["data"]["songList"];
                var year = json["data"]["publishDate"].ToString();
                var cmp = json["data"]["companyName"].ToString();
                var lug = json["data"]["lang"].ToString();
                var ar = json["data"]["singerName"].ToString();
                var pic = json["data"]["picUrl"].ToString();
                var alias = json["data"]["alias"].ToString();
                var songs = json["data"]["songs"].Select(jToken => jToken.ToString()).ToList();
                result.Songs = GetListByJson(datas);
                foreach (var r in result.Songs)
                {
                    r.TrackNum = (songs.IndexOf(r.SongId) + 1).ToString();
                    r.Year = year;
                    r.Company = cmp;
                    r.Language = lug;
                    r.AlbumArtist = ar;
                    r.PicUrl = pic;
                    r.AlbumSubName = alias;
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
            //http://api.dongting.com/song/singer/1766358

            var url = "http://api.dongting.com/song/singer/" + id +
                     "/songs?app=ttpod&from=android&api_version=1.0&size="+size+"&page="+page+"&user_id=0&tid=0";
            var html = CommonHelper.GetHtmlContent(url);
            var result = new ArtistResult
            {
                ErrorCode = 200,
                ErrorMsg = "OK",
                ArtistLink = "http://music.163.com/#/artist?id=" + id,
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
                if (json["totalCount"].ToString() == "0")
                {
                    return null;
                }
                var datas = json["data"];
                result.Songs = GetListByJson(datas);
                try
                {
                    html = CommonHelper.GetHtmlContent("http://api.dongting.com/song/singer/" + id);
                    json = JObject.Parse(html);
                    result.ArtistInfo = json["data"]["brief"].ToString();
                    result.ArtistLogo = json["data"]["picUrl"].ToString();
                    result.TransName = json["data"]["alias"].First?.ToString();
                    result.AlbumSize = json["data"]["albumsCount"].Value<int>();
                    result.SongSize = json["data"]["songsCount"].Value<int>();
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

            var url = "http://api.songlist.ttpod.com/songlists/" + id;
            var html = CommonHelper.GetHtmlContent(url);
            var result = new CollectResult
            {
                ErrorCode = 200,
                ErrorMsg = "OK",
                CollectId = id,
                CollectLink = "http://h.dongting.com/yule/app/music_songlist.html?id="+id,
                Songs = new List<SongResult>()
            };
            if (string.IsNullOrEmpty(html) || html == "null")
            {
                result.ErrorCode = 300;
                result.ErrorMsg = "获取歌单信息失败";
                return result;
            }
            var json = JObject.Parse(html);
            if (string.IsNullOrEmpty(json["songs"]?.ToString()) || json["songs"].ToString() == "null")
            {
                result.ErrorCode = 404;
                result.ErrorMsg = "请检查歌单ID是否正确";
                return result;
            }
            try
            {
                var datas = json["songs"];
                result.Songs = GetListByJson(datas);
                result.CollectName = json["title"].ToString();
                result.CollectLogo = json["image"]["pic"].ToString();
                result.CollectMaker = json["owner"]["nick_name"].ToString();
                result.CollectInfo = json["desc"].ToString();
                var tags = json["tags"].Aggregate("", (current, t) => current + (t["tag_name"].ToString() + ";"));
                result.Tags = tags.Trim(';');
                result.SongSize = json["song_count"].Value<int>();
                result.Date =
                    CommonHelper.UnixTimestampToDateTime(Convert.ToInt64(json["created_time"].ToString()) / 1000)
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
            var html = CommonHelper.GetHtmlContent("http://api.dongting.com/song/song/" + id);
            if (string.IsNullOrEmpty(html))
            {
                return null;
            }
            try
            {
                var json = JObject.Parse(html);
                var str = "[" + json["data"] + "]";
                var datas = JToken.Parse(str);
                var list = GetListByJson(datas);
                return list == null || list.Count <= 0 ? null : list[0];
            }
            catch (Exception ex)
            {
                CommonHelper.AddLog(ex);
                return null;
            }
        }

        private static string GetUrl(string id, string quality, string format)
        {
            var song = SearchSong(id);
            if (song == null && format == "jpg")
            {
                var url = "http://api.dongting.com/song/song/" + id;
                var html = CommonHelper.GetHtmlContent(url);
                if (string.IsNullOrEmpty(html))
                {
                    return "";
                }
                return Regex.Match(html, @"(?<=picUrl"":"")http://img.xiami.net[^""]+").Value;
            }
            if (song == null)
            {
                return "";
            }
            switch (format)
            {
                case "mp4":
                case "flv":
                    return quality == "hd" ? song.MvHdUrl : song.MvLdUrl;
                case "lrc":
                    var url = "http://lp.music.ttpod.com/lrc/down?artist=" + WebUtility.UrlEncode(song.ArtistName) +
                              "&title=" + WebUtility.UrlEncode(song.SongName) + "&song_id=" + song.SongId;
                    var html = CommonHelper.GetHtmlContent(url);
                    if (string.IsNullOrEmpty(html))
                    {
                        return "";
                    }
                    var json = JObject.Parse(html);
                    return json["data"]?["lrc"]?.ToString();
                case "jpg":
                    return song.PicUrl;
                case "flac":
                    return song.FlacUrl;
                case "wav":
                    return song.WavUrl;
                case "ape":
                    return song.ApeUrl;
                case "mp3":
                    if (quality == "128")
                    {
                        return song.LqUrl;
                    }
                    return string.IsNullOrEmpty(song.SqUrl) ? song.LqUrl : song.SqUrl;
            }
            return song.LqUrl;
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

        /// <summary>
        /// 根据艺术家和歌名得到歌曲信息
        /// 用于获取虾米付费歌曲
        /// </summary>
        /// <param name="ar">艺术家</param>
        /// <param name="name">歌名</param>
        /// <returns></returns>
        public static SongResult GetXmSqUrl(string ar, string name)
        {
            var key = ar + " - " + name;
            var list = Search(key, 1, 20);
            if (list == null)
            {
                return null;
            }
            if (list.Songs.Count <= 0)
            {
                return null;
            }
            var songs = list.Songs.Where(s => (s.SongName == name) && (s.ArtistName == ar)).ToList();
            var song = new SongResult();
            if (songs.Count <= 0)
            {
                decimal max = 0;
                var index = 0;
                var stringcompute1 = new StringCompute();
                foreach (var songResult in list.Songs)
                {
                    stringcompute1.SpeedyCompute(key, song.ArtistName + " - " + songResult.SongName);
                    var rate = stringcompute1.ComputeResult.Rate;
                    if (rate < (decimal)0.8)
                    {
                        continue;
                    }
                    if (rate > max)
                    {
                        max = rate;
                        song = list.Songs[index];
                    }
                    index++;
                }
            }
            else
            {
                song = songs[0];
            }
            return song;
        }

    }
}