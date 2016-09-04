using AnyListen.Api.Music;
using AnyListen.Interface;
using AnyListen.Model;

namespace AnyListen.Service
{
    public class MusicService
    {
        public static IMusic GetMusic(string type)
        {
            switch (type)
            {
                case "wy":
                    return new WyMusic();
                case "xm":
                    return new XmMusic();
                case "tt":
                    return new TtMusic();
                case "qq":
                    return new TxMusic();
                case "bd":
                    return new BdMusic();
                case "kw":
                    return new KwMusic();
                case "kg":
                    return new KgMusic();
                default:
                    return null;
            }
        }
    }
}