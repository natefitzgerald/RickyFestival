using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace WebScraper
{
    public class CryptidWikiScraper : WebScraper
    {
        private string html = @"http://cryptidz.wikia.com/wiki/Cryptid_Wiki";
        private HttpClient client;
        public CryptidWikiScraper()
        {
            client = new HttpClient();

        }
        public string Scrape()
        {
            return Scrape(html).Result;
        }
        public async Task<string> Scrape(string URL)
        {
            HtmlWeb web = new HtmlWeb();

            var doc = web.Load(URL);
            

            HtmlNodeCollection collection = doc.DocumentNode.SelectNodes("//a");
            foreach (HtmlNode link in collection)
            {
                try
                {
                    string target = link.Attributes["href"].Value;
                }
                catch
                {

                }
            }
            return "";
        }
    }
}
