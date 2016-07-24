using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AnyListen.Helper;
using AnyListen.Interface;
using AnyListen.Model;
using Newtonsoft.Json.Linq;

namespace AnyListen.Api.Music
{
    public class BdMusic:IMusic
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
            var url =
                "http://tingapi.ting.baidu.com/v1/restserver/ting?from=android&version=5.6.5.6&method=baidu.ting.search.merge&format=json&query=" +
                key + "&page_no=" + page + "&page_size=" + size + "&type=0&data_source=0&use_cluster=1";
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
                if (json["error_code"].ToString() != "22000" || json["result"]["song_info"] == null)
                {
                    result.ErrorCode = 404;
                    result.ErrorMsg = "没有找到符合要求的歌曲";
                    return result;
                }
                var datas = json["result"]["song_info"]["song_list"];
                result.TotalSize = json["result"]["song_info"]["total"].Value<int>();
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

        private static List<SongResult> GetListByJson(JToken songs)
        {
            if (songs == null)
            {
                return null;
            }
            var list = new List<SongResult>();
            foreach (JToken j in songs)
            {
                var song = new SongResult
                {
                    SongId = j["song_id"].ToString(),
                    SongName = j["title"].ToString(),
                    SongSubName = j["info"]?.ToString(),
                    SongLink = "http://music.baidu.com/song/" + j["song_id"],

                    ArtistId = j["ting_uid"].ToString(),
                    ArtistName = j["author"].ToString(),
                    ArtistSubName = "",

                    AlbumId = j["album_id"].ToString(),
                    AlbumName = j["album_title"].ToString(),
                    AlbumSubName = "",
                    AlbumArtist = j["author"].ToString(),

                    Length = j["file_duration"] == null ? "" : CommonHelper.NumToTime(j["file_duration"].ToString()),
                    Size = "",
                    BitRate = "",

                    FlacUrl = "",
                    ApeUrl = "",
                    WavUrl = "",
                    SqUrl = "",
                    HqUrl = "",
                    LqUrl = "",
                    CopyUrl = "",

                    PicUrl = "",
                    LrcUrl = j["lrclink"]?.ToString() ?? CommonHelper.GetSongUrl("bd", "128", j["song_id"].ToString(), "lrc"),
                    TrcUrl = "",
                    KrcUrl = "",

                    MvId = "",
                    MvHdUrl = "",
                    MvLdUrl = "",

                    Language = j["language"]?.ToString(),
                    Company = "",
                    Year = j["publishtime"]?.ToString(),
                    Disc = "1",
                    TrackNum = j["album_no"]?.ToString(),
                    Type = "bd"
                };
                
                if (song.ArtistId.Contains(","))
                {
                    song.ArtistId = song.ArtistId.Split(',')[0].Trim();
                }
                if (song.AlbumArtist.Contains(","))
                {
                    song.AlbumArtist = song.AlbumArtist.Split(',')[0].Trim();
                }
                var rate = j["all_rate"].ToString();
                song.BitRate = "128K";
                song.LqUrl = CommonHelper.GetSongUrl("bd", "128", song.SongId, "mp3");
                if (rate.Contains("192"))
                {
                    song.BitRate = "192K";
                    song.HqUrl = CommonHelper.GetSongUrl("bd", "192", song.SongId, "mp3");
                }
                if (rate.Contains("256"))
                {
                    song.BitRate = "256K";
                    song.HqUrl = CommonHelper.GetSongUrl("bd", "256", song.SongId, "mp3");
                }
                if (rate.Contains("320"))
                {
                    song.BitRate = "320K";
                    song.SqUrl = CommonHelper.GetSongUrl("bd", "320", song.SongId, "mp3");
                }
                if (rate.Contains("flac"))
                {
                    song.BitRate = "无损";
                    song.FlacUrl = CommonHelper.GetSongUrl("bd", "2000", song.SongId, "flac");
                }
                song.CopyUrl = CommonHelper.GetSongUrl("bd", "320", song.SongId, "mp3");
                if (j["has_mv"].ToString() == "1")
                {
                    song.MvHdUrl = CommonHelper.GetSongUrl("bd", "hd", song.SongId, "flv");
                    song.MvLdUrl = CommonHelper.GetSongUrl("bd", "ld", song.SongId, "flv");
                }
                song.PicUrl = CommonHelper.GetSongUrl("bd", "hd", song.SongId, "jpg");
                list.Add(song);
            }
            return list;
        }

        private AlbumResult SearchAlbum(string id)
        {
            var result = new AlbumResult
            {
                ErrorCode = 200,
                ErrorMsg = "OK",
                AlbumLink = "http://music.baidu.com/album/" + id,
                Songs = new List<SongResult>()
            };
            var url =
                "http://tingapi.ting.baidu.com/v1/restserver/ting?from=android&version=5.6.5.6&method=baidu.ting.album.getAlbumInfo&format=json&album_id=" + id;
            var html = CommonHelper.GetHtmlContent(url);
            if (string.IsNullOrEmpty(html))
            {
                result.ErrorCode = 300;
                result.ErrorMsg = "获取专辑信息失败";
                return result;
            }
            try
            {
                if (html.Contains("error_code"))
                {
                    result.ErrorCode = 404;
                    result.ErrorMsg = "请检查专辑ID是否正确";
                    return result;
                }
                var json = JObject.Parse(html);
                result.AlbumType = json["albumInfo"]["styles"].ToString();
                result.AlbumInfo = json["albumInfo"]["info"].ToString();

                var datas = json["songlist"];
                var year = json["albumInfo"]["publishtime"].ToString();
                var cmp = json["albumInfo"]["publishcompany"].ToString();
                var lug = json["albumInfo"]["language"].ToString();
                var ar = json["albumInfo"]["author"].ToString();
                string pic;
                if (!string.IsNullOrEmpty(json["albumInfo"]["pic_s1000"].ToString()))
                {
                    pic = json["albumInfo"]["pic_s1000"].ToString();
                }
                else if (!string.IsNullOrEmpty(json["albumInfo"]["pic_s500"].ToString()))
                {
                    pic = json["albumInfo"]["pic_s500"].ToString();
                }
                else
                {
                    pic = json["albumInfo"]["pic_radio"].ToString();
                }
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
                    r.PicUrl = pic;
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

        private ArtistResult SearchArtist(string id, int page, int size)
        {
            var url =
                "http://tingapi.ting.baidu.com/v1/restserver/ting?from=android&version=5.6.5.6&method=baidu.ting.artist.getSongList&format=json&order=2&tinguid=" +
                id + "&offset=" + (page - 1)*size + "&limits=" + size;
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
                if (json["error_code"].ToString() != "22000")
                {
                    result.ErrorCode = 404;
                    result.ErrorMsg = "请检查艺术家ID是否正确";
                    return null;
                }
                var datas = json["songlist"];
                try
                {
                    result.Songs = GetListByJson(datas);
                    html = CommonHelper.GetHtmlContent("http://tingapi.ting.baidu.com/v1/restserver/ting?from=android&version=5.6.5.6&method=baidu.ting.artist.getInfo&format=json&tinguid="+id);
                    if (string.IsNullOrEmpty(html))
                    {
                        return result;
                    }
                    json = JObject.Parse(html);
                    result.ArtistInfo = json["intro"].ToString();
                    result.ArtistLink = json["url"].ToString();
                    result.ArtistLogo = json["avatar_s500"]?.ToString() ?? json["avatar_s180"].ToString();
                    try
                    {
                        result.AlbumSize = Convert.ToInt32(json["albums_total"].ToString());
                        result.SongSize = Convert.ToInt32(json["songs_total"].ToString());
                    }
                    catch (Exception ex)
                    {
                        CommonHelper.AddLog(ex);
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

        private static CollectResult SearchCollect(string id)
        {

            var url = "http://tingapi.ting.baidu.com/v1/restserver/ting?from=android&version=5.6.5.6&method=baidu.ting.diy.gedanInfo&format=json&listid=" + id;
            var html = CommonHelper.GetHtmlContent(url);
            var result = new CollectResult
            {
                ErrorCode = 200,
                ErrorMsg = "OK",
                CollectId = id,
                CollectLink = "http://music.baidu.com/songlist/" + id,
                Songs = new List<SongResult>()
            };
            if (string.IsNullOrEmpty(html) || html == "null")
            {
                result.ErrorCode = 300;
                result.ErrorMsg = "获取歌单信息失败";
                return result;
            }
            var json = JObject.Parse(html);
            if (json["error_code"].ToString() != "22000")
            {
                result.ErrorCode = 404;
                result.ErrorMsg = "请检查歌单ID是否正确";
                return result;
            }
            try
            {
                var datas = json["content"];
                result.Songs = GetListByJson(datas);
                result.CollectName = json["title"].ToString();
                result.CollectLogo = json["pic_500"].ToString();
                result.CollectInfo = json["desc"].ToString();
                result.Tags = json["tag"].ToString();
                result.SongSize = result.Songs.Count;
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
            var url = "http://tingapi.ting.baidu.com/v1/restserver/ting?from=android&version=5.6.5.6&method=baidu.ting.song.getInfos&format=json&songid=" + id + "&e=qkJ%2FEqx%2FM1Q12VUMA3sTqFk3lOrwwubVCs1OpoTpolJAznQ9yAszR5iljRoanpAk&baiduid=a1179897ec2bc2c5e1ba56cd19494060&bduss=ZkOEZSN1hSSHFJN2NId1ZxaUl1fmhIS0FpZEZGdDV-RXVDaUlJLX42MnFwVTFXQVFBQUFBJCQAAAAAAAAAAAEAAAC26iZ4AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAKoYJlaqGCZWS3&nw=2&ucf=1&res=1&l2p=0&lpb=&usup=1&lebo=0";
            var html = CommonHelper.GetHtmlContent(url);
            if (string.IsNullOrEmpty(html))
            {
                return null;
            }
            try
            {
                if (html.Contains("buy_url\":"))
                {
                    return GetPaySong(id);
                }
                var json = JObject.Parse(html);
                if (json["error_code"].ToString() != "22000")
                {
                    return null;
                }
                var match = "["+ json["songinfo"] + "]";
                var datas = JToken.Parse(match.Trim());
                var list = GetListByJson(datas);
                var song = list[0];
                var links = json["songurl"]["url"];
                foreach (JToken token in links)
                {
                    var fileBitrate = token["file_bitrate"].ToString();
                    var link = token["file_link"].ToString();
                    if (string.IsNullOrEmpty(link))
                    {
                        continue;
                    }
                    switch (fileBitrate)
                    {
                        case "128":
                            song.LqUrl = link;
                            break;
                        case "192":
                            song.HqUrl = link;
                            break;
                        case "256":
                            song.HqUrl = link;
                            break;
                        case "320":
                            song.SqUrl = link;
                            break;
                        case "flac":
                            break;
                    }
                }
                return song;
            }
            catch (Exception ex)
            {
                CommonHelper.AddLog(ex);
                return null;
            }
        }

        private static SongResult GetPaySong(string id)
        {
            var url = "http://tingapi.ting.baidu.com/v1/restserver/ting?from=webapp_music&method=baidu.ting.song.baseInfos&song_id=" + id;
            var html = CommonHelper.GetHtmlContent(url);
            if (string.IsNullOrEmpty(html))
            {
                return null;
            }
            var json = JObject.Parse(html);
            if (json["error_code"].ToString() != "22000")
            {
                return null;
            }
            var datas = json["result"]["items"];
            var list = GetListByJson(datas);
            return list?[0];
        }

        private static string GetUrl(string id, string quality, string format)
        {
            if (format == "lrc" || format == "jpg")
            {
                var song = SearchSong(id);
                return format == "lrc" ? song.LrcUrl : (string.IsNullOrEmpty(song.PicUrl) ? "http://yyfm.oss-cn-qingdao.aliyuncs.com/img/mspy.jpg" : song.PicUrl);
            }
            if (format == "flv")
            {
                return GetMvUrl(id, quality);
            }
            if (format == "flac")
            {
                var html = CommonHelper.GetHtmlContent("http://play.baidu.com/data/music/songlink?songIds=" + id + "&hq=2&type=flac&rate=2000&pt=0&flag=1&s2p=570&prerate=2000&bwt=359&dur=356000&bat=359&bp=39&pos=6848&auto=0");
                if (string.IsNullOrEmpty(html))
                {
                    return null;
                }
                return
                    Regex.Match(html, @"(?<=songLink"":"")[^""]+(?="",""showLink"":""[\s\S]*?"",""format"":""flac"")")
                        .Value.Replace("\\", "");
            }
            var mHtml = CommonHelper.GetHtmlContent("http://tingapi.ting.baidu.com/v1/restserver/ting?method=baidu.ting.song.down&ts=1458575606&songid=" + id + "&nw=2&l2p=0&lpb=0&ext=mp3&format=json&from=ios&dt=" + quality + "&mul=1&vid=&e=Xw3BvQ9t46Sb%2BTP4RHZaFsqFx7vHFuQEx6t59t5%2FYrwJuXpxxH6A%2BoWQveBUfYG9&version=5.5.5&from=ios&channel=appstore&operator=1");
            if (string.IsNullOrEmpty(mHtml))
            {
                return null;
            }
            var j = JObject.Parse(mHtml);
            try
            {
                var url = j["bitrate"]["file_link"].ToString();
                if (string.IsNullOrEmpty(url) || url.Contains("pan."))
                {
                    url = GetPayUrl(id);
                }
                return url.Replace("http://yinyueshiting.baidu.com", "https://ss0.bdstatic.com/y0s1hSulBw92lNKgpU_Z2jR7b2w6buu");
            }
            catch (Exception)
            {
                return GetPayUrl(id).Replace("http://yinyueshiting.baidu.com", "https://ss0.bdstatic.com/y0s1hSulBw92lNKgpU_Z2jR7b2w6buu");
            }
        }

        private static string GetMvUrl(string id, string quality)
        {
            var url =
                "http://tingapi.ting.baidu.com/v1/restserver/ting?from=android&version=5.6.5.6&provider=11%2C12&method=baidu.ting.mv.playMV&format=json&song_id=" +
                id + "&definition=0";
            var html = CommonHelper.GetHtmlContent(url);
            if (string.IsNullOrEmpty(html))
            {
                return "";
            }
            var json = JObject.Parse(html);
            if (json["error_code"].ToString() != "22000")
            {
                return "";
            }
            var videoId = json["result"]["video_info"]["sourcepath"].ToString();
            videoId = Regex.Match(videoId, @"(?<=video/)\d+").Value;
            if (string.IsNullOrEmpty(videoId))
            {
                if (json["result"]["video_info"]["sourcepath"].ToString().Contains("iqiyi.com"))
                {
                    return GetAqyUrl(json, quality);
                }
                return null;
            }
            url = "http://www.yinyuetai.com/api/info/get-video-urls?videoId=" + videoId;
            html = CommonHelper.GetHtmlContent(url);
            if (string.IsNullOrEmpty(html))
            {
                return "";
            }
            json = JObject.Parse(html);
            if (json["error"].ToString().ToLower() != "false")
            {
                return "";
            }
            if (quality == "hd")
            {
                if (html.Contains("heVideoUrl"))
                {
                    return json["heVideoUrl"].ToString();
                }
                if (html.Contains("hdVideoUrl"))
                {
                    return json["hdVideoUrl"].ToString();
                }
                if (html.Contains("hcVideoUrl"))
                {
                    return json["hcVideoUrl"].ToString();
                }
            }
            else
            {
                if (html.Contains("hdVideoUrl"))
                {
                    return json["hdVideoUrl"].ToString();
                }
                if (html.Contains("hcVideoUrl"))
                {
                    return json["hcVideoUrl"].ToString();
                }
            }
            return "";
        }

        private static string GetAqyUrl(JObject json, string quality)
        {
            var link = json["result"]["files"].First["file_link"].ToString();
            var reg = Regex.Match(link, @"(?<=vid=)(\w+)(?:.tvId=)(\d+)");
            if (reg.Groups.Count != 3)
            {
                return null;
            }
            var videoId = reg.Groups[1].Value;
            var tvId = reg.Groups[2].Value;
            var html = CommonHelper.GetHtmlContent("http://cache.video.qiyi.com/vp/" + tvId + "/" + videoId + "/");
            if (string.IsNullOrEmpty(html))
            {
                return null;
            }
            json = JObject.Parse(html);
            var jArray = json["tkl"].First["vs"];
            var dic = new Dictionary<int,JToken>();
            foreach (JToken jToken in jArray)
            {
                switch (jToken["bid"].ToString())
                {
                    case "10":
                        dic.Add(7,jToken);
                        break;
                    case "5":
                        dic.Add(6, jToken);
                        break;
                    case "4":
                        dic.Add(5, jToken);
                        break;
                    case "3":
                        dic.Add(4, jToken);
                        break;
                    case "2":
                        dic.Add(3, jToken);
                        break;
                    case "1":
                        dic.Add(2, jToken);
                        break;
                    default:
                        dic.Add(1, jToken);
                        break;
                }
            }
            JToken info;
            try
            {
                info = quality == "hd" ? dic[dic.Keys.Max()] : dic[dic.Keys.Max() - 1];
            }
            catch (Exception)
            {
                info = jArray.Last;
            }
            var linkToken = info["fs"];
            if (!info["fs"][0]["l"].ToString().StartsWith("/"))
            {
                linkToken = GetQiyLink(info["fs"][0]["l"].ToString()).EndsWith("mp4") ? info["flvs"] : info["fs"];
            }
            var tmpLink = linkToken[0]["l"].ToString().StartsWith("/")
                ? linkToken[0]["l"].ToString()
                : GetQiyLink(linkToken[0]["l"].ToString());
            //var linkHash = Regex.Match(tmpLink, @"(?<=/)(\w+)(?=\.)").Value;
            //var t = CommonHelper.GetTimeSpan();
            //linkHash = Math.Floor(Convert.ToDouble(t) / 600.0) + ")(*&^flash@#$%a" + linkHash;
            //var dispathKey = CommonHelper.Md5(linkHash);
            tmpLink = "http://data.video.qiyi.com/videos" + tmpLink;
            html = CommonHelper.GetHtmlContent(tmpLink);
            if (string.IsNullOrEmpty(html))
            {
                return null;
            }
            return JObject.Parse(html)["l"].ToString();
        }

        private static string GetPayUrl(string id)
        {
            var html =
                    CommonHelper.GetHtmlContent("http://music.baidu.com/data/music/fmlink?songIds=" + id +
                                                "&type=mp3&rate=320");
            if (string.IsNullOrEmpty(html))
            {
                return null;
            }
            var json = JObject.Parse(html);
            if (json["errorCode"].ToString() != "22000")
            {
                return null;
            }
            return json["data"]["songList"].First["songLink"].ToString();
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

        #region 爱奇艺解码

        public static string GetQiyLink(string param1)
        {
            var loc2 = "";
            var loc3 = param1.Split('-');
            var loc4 = loc3.Length;
            var loc5 = loc4 - 1;
            while (loc5 >= 0)
            {
                var loc6 = GetVrsxorCode(Convert.ToUInt32(loc3[loc4 - loc5 - 1], 16), (uint)loc5);
                loc2 = Convert.ToChar(loc6) + loc2;
                loc5--;
            }
            return loc2;
        }

        private static uint GetVrsxorCode(uint param1, uint param2)
        {
            var loc3 = (int)(param2 % 3);
            if (loc3 == 1)
            {
                return param1 ^ 121;
            }
            if (loc3 == 2)
            {
                return param1 ^ 72;
            }
            return param1 ^ 103;
        }

        public static string AiqiyiDecoder(byte[] param1)
        {
            var loc3 = param1.Length;
            const int loc5 = 20110218;
            const int loc6 = loc5 % 100;
            var loc7 = loc3 % 4;
            var loc2 = new byte[loc3 + loc7];
            var loc4 = 0;
            while (loc4 + 4 <= loc3)
            {
                var temp = param1[loc4] << 24 | param1[loc4 + 1] << 16 | param1[loc4 + 2] << 8 |
                           param1[loc4 + 3];
                var loc8 = temp < 0 ? Convert.ToUInt32(UInt32.MaxValue + temp + 1) : Convert.ToUInt32(temp);
                loc8 = loc8 ^ Convert.ToUInt32(loc5);
                loc8 = rotate_right(loc8, loc6);
                loc2[loc4] = Convert.ToByte((loc8 & 4278190080) >> 24);
                loc2[loc4 + 1] = Convert.ToByte((loc8 & 16711680) >> 16);
                loc2[loc4 + 2] = Convert.ToByte((loc8 & 65280) >> 8);
                loc2[loc4 + 3] = Convert.ToByte(loc8 & 255);
                loc4 = loc4 + 4;
            }
            loc4 = 0;
            while (loc4 < loc7)
            {
                loc2[loc3 - loc7 - 1 + loc4] = param1[loc3 - loc7 - 1 + loc4];
                loc4++;
            }
            return Encoding.UTF8.GetString(loc2);
        }

        private static uint rotate_right(uint param1, int param2)
        {
            var loc4 = 0;
            while (loc4 < param2)
            {
                var loc3 = param1 & 1;
                param1 = param1 >> 1;
                loc3 = loc3 << 31;
                param1 = param1 + loc3;
                loc4++;
            }
            return param1;
        }

        #endregion
    }
}