using System;
using System.IO;

namespace Harness
{
    class Program
    {
        static void Main(string[] args)
        {
            var text = File.ReadAllText("text.txt");
            using (var markov = new Markov.MarkovChain(text))
            {
                text = text.Replace(Environment.NewLine, " ");
                Console.Write(markov.GenerateSentences(10));
            }
        }
    }
}
