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
                Console.Write(markov.GenerateSentences(3));
            }
            Console.ReadKey(true);
        }
    }
}
