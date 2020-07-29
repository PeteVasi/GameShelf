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
            int gl = 0;
            if (args.Length >= 2 && args[0] == "-u")
            {
                return ProcessUser(args[1]);
            }
            else if (args.Length >= 2 && args[0] == "-gl" && int.TryParse(args[1], out gl) && gl > 0)
            {
                return ProcessGeekList(gl, args.Length >= 3 && args[2] == "-gamelink");
            }
            else
            {
                Console.WriteLine("Try '-u PeteVasi' or '-gl 11205' or '-gl 11205 -gamelink'...");
                return Task.CompletedTask;
            }
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
                    Id = MaybeInt((string)i.Attribute("objectid")) ?? 0,
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
            XElement items = XElement.Parse(xml);
            var title = (string)items.Element("title");
            var games = items
                .Descendants("item")
                .Select(i => new Game
                {
                    Name = (string)i.Attribute("objectname"),
                    Id = MaybeInt((string)i.Attribute("objectid")) ?? 0,
                    Image = "https://cf.geekdo-images.com/thumb/img/BLvY1461ZRxzY_qoatscfR0gQGU=/fit-in/200x150/pic100654.jpg",
                    Url = linkToGame
                          ? ("https://boardgamegeek.com/boardgame/" + (string)i.Attribute("objectid"))
                          : ($"https://www.boardgamegeek.com/geeklist/{listId}/item/" + (string)i.Attribute("id") + "#item" + (string)i.Attribute("id")),
                    Rating = 0
                })
                .ToList();

            var idList = string.Join(",", games.Select(i => i.Id));
            var gamesXml = await client.GetStringAsync($"https://api.geekdo.com/xmlapi2/thing?id={idList}&stats=1&page=1&pagesize=100");
            XElement gameItems = XElement.Parse(gamesXml);
            var gameImages = gameItems
                .Descendants("item")
                .Select(i => new
                {
                    Id = MaybeInt((string)i.Attribute("id")) ?? 0,
                    Image = (string)i.Element("image"),
                    Rating = MaybeDouble((string)i.Element("statistics")?.Element("ratings")?.Element("bayesaverage")?.Attribute("value")) ?? 0
                });

            foreach (var game in games)
            {
                var img = gameImages.FirstOrDefault(i => i.Id == game.Id);
                if (!string.IsNullOrEmpty(img?.Image))
                {
                    game.Image = img.Image;
                }
                game.Rating = img?.Rating ?? 0;
            }
            Console.Write(HtmlHeader(title));
            Console.Write(GameImages(games.OrderByDescending(i => i.Rating)));
            Console.Write(HtmlFooter($"https://www.boardgamegeek.com/geeklist/{listId}"));
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

        private static int? MaybeInt(string str)
        {
            int value;
            if (!int.TryParse(str, out value))
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
