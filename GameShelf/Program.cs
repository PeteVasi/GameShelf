using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GameShelf
{
    class Program
    {
        private static readonly HttpClient client = new HttpClient();

        public static void Main(string[] args) => new Program().MainAsync(args).GetAwaiter().GetResult();

        public Task MainAsync(string[] args)
        {
            // TODO: process args
            return ProcessUser("PeteVasi");
            // else return ProcessGeekList(11205);
        }

        private async Task ProcessUser(string username)
        {
            var xml = await client.GetStringAsync($"https://boardgamegeek.com/xmlapi2/collection?username={username}&excludesubtype=boardgameexpansion&own=1&stats=1");
            XElement items = XElement.Parse(xml);
            var games = items
                .Descendants("item")
                .Select(i => new Game
                {
                    Name = (string)i.Element("name"),
                    Image = (string)i.Element("image"),
                    Url = "https://boardgamegeek.com/boardgame/" + (string)i.Attribute("objectid"),
                    Rating = MaybeDouble((string)i.Element("stats").Element("rating").Attribute("value")) ?? 0
                })
                .OrderByDescending(i => i.Rating);

            Console.Write(HtmlHeader(username));
            Console.Write(GameImages(games));
            Console.Write(HtmlFooter($"https://boardgamegeek.com/collection/user/{username}?sort=rating&sortdir=desc&own=1&ff=1&excludesubtype=boardgameexpansion&gallery=large"));
        }

        private async Task ProcessGeekList(int listId, bool linkToGame)
        {
            var xml = await client.GetStringAsync($"https://www.boardgamegeek.com/xmlapi/geeklist/{listId}");
            var gamesXml = await client.GetStringAsync("https://api.geekdo.com/xmlapi2/thing?id=5867,7866&page=1&pagesize=100");

            Console.Write(xml);
            Console.Write(gamesXml);
        }

        private static double? MaybeDouble(string str)
        {
            double value;
            if (!double.TryParse(str, out value))
            {
                return null;
            }
            else
            {
                return value;
            }
        }

        private string HtmlHeader(string extraTitle)
        {
            var str = new StringBuilder();
            str.AppendLine("<!DOCTYPE html>");
            str.AppendLine("<html lang=\"en\">");
            str.Append("<head>");
            str.Append("<meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1,shrink-to-fit=no\">");
            str.Append($"<title>GameShelf - {extraTitle}</title>");
            str.Append("<link rel=\"stylesheet\" type=\"text/css\" href=\"gameshelf.css\">");
            str.AppendLine("</head>");
            str.AppendLine("<body>");
            return str.ToString();
        }

        private string HtmlFooter(string extraHref)
        {
            var str = new StringBuilder();
            if (!string.IsNullOrEmpty(extraHref))
            {
                str.AppendLine($"<div class=\"extra\"><a href=\"{extraHref}\">{extraHref}</a></div>");
            }
            str.AppendLine("<div class=\"git\"><a href=\"https://github.com/PeteVasi/GameShelf\">GameShelf GitHub</a></div>");
            str.AppendLine("</body>");
            str.AppendLine("</html>");
            return str.ToString();
        }

        private string GameImages(IEnumerable<Game> games)
        {
            var str = new StringBuilder();
            foreach (var game in games)
            {
                str.AppendLine($"<div class=\"game\"><a href=\"{game.Url}\"><img src=\"{game.Image}\" alt=\"{game.Name}\"></a></div>");
            }
            return str.ToString();
        }
    }
}
