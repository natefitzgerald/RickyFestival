using System;
using System.IO;

namespace Harness
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var markov = new Markov.MarkovChain("text.txt"))
            {
                while (true)
                {
                    Console.Write(markov.GenerateSentences(2));
                    Console.ReadKey(true);
                    Console.Clear();
                }
            }
        }
    }
}
