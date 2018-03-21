using System;
using System.IO;

namespace Harness
{
    class Program
    {
        static void Main(string[] args)
        {
            var text = File.ReadAllText("text.txt");
            var markov = new Markov.Markov2(text);
            while (true)
            {
                Console.Clear();
                Console.Write(markov.GenerateSentence() + Environment.NewLine);
                Console.ReadKey();
            }
        }
    }
}
