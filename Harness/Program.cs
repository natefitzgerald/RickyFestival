using System;
using System.IO;

namespace Harness
{
    class Program
    {
        static void Main(string[] args)
        {


            var markov = new Markov.MarkovChain();
            var text = File.ReadAllText("text.txt");
            text = text.Replace(Environment.NewLine, " ");
            markov.Parse(text);
            Console.Write(markov.Generate(500));
        }
    }
}
