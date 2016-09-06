using AnyListen.Helper;
using AnyListen.Model;
using AnyListen.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AnyListen.Controllers
{
    [Route("[controller]")]
    public class MusicController : Controller
    {
        [HttpGet("search")]
        public SearchResult Search([FromQuery]string t, [FromQuery]string k, [FromQuery]int p, [FromQuery]int s)
        {
            if (string.IsNullOrEmpty(t))
            {
                return new SearchResult
                {
                    ErrorCode = 403,
                    ErrorMsg = "请输入搜索类型"
                };
            }
            if (string.IsNullOrEmpty(k))
            {
                return new SearchResult
                {
                    ErrorCode = 403,
                    ErrorMsg = "请输入关键词"
                };
            }
            if (p == 0)
            {
                p = 1;
            }
            if (s == 0)
            {
                s = 30;
            }
            return MusicService.GetMusic(t).SongSearch(k,p,s);
        }

        [HttpGet("album")]
        public AlbumResult SearchAlbum([FromQuery]string t, [FromQuery]string id)
        {
            if (string.IsNullOrEmpty(t))
            {
                return new AlbumResult
                {
                    ErrorCode = 403,
                    ErrorMsg = "请输入搜索类型"
                };
            }
            if (string.IsNullOrEmpty(id))
            {
                return new AlbumResult
                {
                    ErrorCode = 403,
                    ErrorMsg = "请输入专辑ID"
                };
            }
            return MusicService.GetMusic(t).AlbumSearch(id);
        }

        [HttpGet("artist")]
        public ArtistResult SearchArtist([FromQuery]string t, [FromQuery]string id, [FromQuery]int p , [FromQuery]int s)
        {
            if (string.IsNullOrEmpty(t))
            {
                return new ArtistResult
                {
                    ErrorCode = 403,
                    ErrorMsg = "请输入搜索类型"
                };
            }
            if (string.IsNullOrEmpty(id))
            {
                return new ArtistResult
                {
                    ErrorCode = 403,
                    ErrorMsg = "请输入艺术家ID"
                };
            }
            if (p == 0)
            {
                p = 1;
            }
            if (s == 0)
            {
                s = 50;
            }
            return MusicService.GetMusic(t).ArtistSearch(id, p ,s);
        }

        [HttpGet("collect")]
        public CollectResult CollectSearch([FromQuery]string t, [FromQuery]string id, [FromQuery]int p, [FromQuery]int s)
        {
            if (string.IsNullOrEmpty(t))
            {
                return new CollectResult
                {
                    ErrorCode = 403,
                    ErrorMsg = "请输入搜索类型"
                };
            }
            if (string.IsNullOrEmpty(id))
            {
                return new CollectResult
                {
                    ErrorCode = 403,
                    ErrorMsg = "请输入歌单ID"
                };
            }
            if (p == 0)
            {
                p = 1;
            }
            if (s == 0)
            {
                s = 1000;
            }
            return MusicService.GetMusic(t).CollectSearch(id, p, s);
        }

        [HttpGet("song")]
        public SongResult SongSearch([FromQuery]string t, [FromQuery]string id)
        {
            if (string.IsNullOrEmpty(t) || string.IsNullOrEmpty(id))
            {
                return null;
            }
            return MusicService.GetMusic(t).GetSingleSong(id);
        }

        [HttpGet("ymusic/{path}")]
        public void Get(string path, [FromQuery]string sign)
        {
            if (CommonHelper.Md5(path+CommonHelper.SignKey) != sign)
            {
                Response.StatusCode = 403;
                return;
            }
            if (string.IsNullOrEmpty(path))
            {
                Response.StatusCode = 403;
                return;
            }
            var paths = path.Split('.');
            if (paths.Length != 2)
            {
                Response.StatusCode = 403;
                return;
            }
            var keys = paths[0].Split('_');
            if (keys.Length != 3)
            {
                Response.StatusCode = 403;
                return;
            }
            var linkInfo = MusicService.GetMusic(keys[0]).GetSongUrl(keys[2], keys[1], paths[1]);
            if (linkInfo == null)
            {
                Response.StatusCode = 404;
                return;
            }
            if (linkInfo.StartsWith("http://"))
            {
                Response.StatusCode = 302;
                Response.Headers.Add("Location", linkInfo);
            }
            else
            {
                if (string.IsNullOrEmpty(linkInfo))
                {
                    Response.StatusCode = 404;
                }
                else
                {
                    Response.StatusCode = 200;
                    Response.WriteAsync(linkInfo);
                }
            }
        }
    }
}
