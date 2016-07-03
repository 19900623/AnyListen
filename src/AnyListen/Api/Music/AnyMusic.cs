using System.Collections.Generic;
using System.Threading.Tasks;
using AnyListen.Interface;
using AnyListen.Model;

namespace AnyListen.Api.Music
{
    public class AnyMusic : IMusic
    {
        private static SearchResult Search(string key, int page, int size)
        {
            if (size > 20)
            {
                size = 20;
            }
            var result = new SearchResult
            {
                ErrorCode = 200,
                ErrorMsg = "OK",
                KeyWord = key,
                PageNum = page,
                TotalSize = -1,
                Songs = new List<SongResult>()
            };
            var t1 = Task.Factory.StartNew((() =>
            {
                var r1 = WyMusic.Search(key, page, size);
                if (r1?.Songs != null && r1.Songs.Count > 0)
                {
                    lock (result)
                    {
                        result.Songs.AddRange(r1.Songs);
                    }
                }
            }));
            var t2 = Task.Factory.StartNew((() =>
            {
                var r1 = XmMusic.Search(key, page);
                if (r1?.Songs != null && r1.Songs.Count > 0)
                {
                    lock (result)
                    {
                        result.Songs.AddRange(r1.Songs);
                    }
                }
            }));
            var t3 = Task.Factory.StartNew((() =>
            {
                var r1 = TtMusic.Search(key, page, size);
                if (r1?.Songs != null && r1.Songs.Count > 0)
                {
                    lock (result)
                    {
                        result.Songs.AddRange(r1.Songs);
                    }
                }
            }));
            var t4 = Task.Factory.StartNew((() =>
            {
                var r1 = TxMusic.Search(key, page, size);
                if (r1?.Songs != null && r1.Songs.Count > 0)
                {
                    lock (result)
                    {
                        result.Songs.AddRange(r1.Songs);
                    }
                }
            }));
            Task.WaitAll(t1, t2, t3, t4);
            if (result.Songs.Count > 0) return result;
            result.ErrorCode = 404;
            result.ErrorMsg = "没有找到符合要求的歌曲";
            return result;
        }

        public SearchResult SongSearch(string key, int page, int size)
        {
            return Search(key, page, size);
        }

        public AlbumResult AlbumSearch(string id)
        {
            return null;
        }

        public ArtistResult ArtistSearch(string id, int page, int size)
        {
            return null;
        }

        public CollectResult CollectSearch(string id, int page, int size)
        {
            return null;
        }

        public SongResult GetSingleSong(string id)
        {
            return null;
        }

        public string GetSongUrl(string id, string quality, string format)
        {
            return null;
        }
    }
}