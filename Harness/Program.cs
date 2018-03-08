using System;
using System.IO;
using WebScraper;

namespace Harness
{
    class Program
    {
        static void Main(string[] args)
        {
            var scraper = new CryptidWikiScraper();
            var x = scraper.Scrape();


            var markov = new Markov.MarkovChain();
            var text = File.ReadAllText("text.txt");
            text = text.Replace(Environment.NewLine, " ");
            markov.Parse(text);
            Console.Write(markov.Generate(100, "The"));
        }
    }
}
