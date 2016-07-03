using System;
using AnyListen.Api.Music;
using AnyListen.Interface;
using AnyListen.Model;
using Microsoft.AspNetCore.Mvc;

namespace AnyListen.Controllers.Music
{
    [Route("api/[controller]")]
    public class SearchController : Controller
    {
        // GET: api/search?k=XXXXX&p=XXX&s=XX
        [HttpGet]
        public SearchResult Get()
        {
            const string t = "any";
            var k = Request.Query["k"];
            var p = Request.Query["p"];
            var s = Request.Query["s"];
            if (k.Count <= 0)
            {
                return new SearchResult
                {
                    ErrorCode = 403,
                    ErrorMsg = "请输入关键词"
                };
            }
            if (p.Count <= 0)
            {
                p = "1";
            }
            if (s.Count <= 0)
            {
                s = "30";
            }
            return Search(t, k[0], p[0], s[0]);
        }

        // GET api/search/xm?key=XXXXX&sign=XXXX&p=XXX
        [HttpGet("{type}")]
        public SearchResult Get(string type)
        {
            var k = Request.Query["k"];
            var p = Request.Query["p"];
            var s = Request.Query["s"];
            if (k.Count <= 0)
            {
                return new SearchResult
                {
                    ErrorCode = 403,
                    ErrorMsg = "请输入关键词"
                };
            }
            if (p.Count <= 0)
            {
                p = "1";
            }
            if (s.Count <= 0)
            {
                s = "30";
            }
            return Search(type, k[0], p[0], s[0]);
        }

        private static SearchResult Search(string type, string key, string page, string size)
        {
            IMusic music;
            switch (type)
            {
                case "wy":
                    music = new WyMusic();
                    break;
                case "xm":
                    music = new XmMusic();
                    break;
                case "tt":
                    music = new TtMusic();
                    break;
                case "qq":
                    music = new TxMusic();
                    break;
                default:
                    music = new AnyMusic();
                    break;
            }
            return music.SongSearch(key, Convert.ToInt32(page), Convert.ToInt32(size));
        }
    }
}
