using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Markov
{
    public class MarkovChain : IDisposable
    {
        private Dictionary<string, Dictionary<string, int>> _occurences = new Dictionary<string, Dictionary<string, int>>();
        private Dictionary<string, Dictionary<string, double>> _graph;
        private static Random random = new Random();
        string text;

        public MarkovChain(string filename)
        {
            string text;
            if (File.Exists("signal.butts"))
            {
                text = File.ReadAllText(filename);
            }
            else
            {
                text = File.ReadAllText(filename);
                text = Sanitize(text);
                File.WriteAllText(filename, text);
                File.Create("signal.butts");
            }

            var prevWord = text.Substring(0, text.IndexOf(' '));
            foreach (var word in text.Split(' ').Skip(1))
            {
                if (_occurences.ContainsKey(prevWord))
                {

                    var wordDict = _occurences[prevWord];
                    if (wordDict.ContainsKey(word))
                    {
                        wordDict[word]++;
                    }
                    else
                    {
                        wordDict.Add(word, 1);
                    }
                }
                else
                {
                    _occurences.Add(prevWord, new Dictionary<string, int>(new List<KeyValuePair<string, int>> { new KeyValuePair<string, int>(word, 1) }));
                }
                prevWord = word;
            }

            ConstructGraph();
        }
        

        private void ConstructGraph()
        {
            _graph = new Dictionary<string, Dictionary<string, double>>();
            foreach (var key in _occurences.Keys)
            {
                int total = _occurences[key].Values.Sum();
                _graph[key] = new Dictionary<string, double>();
                foreach(var word in _occurences[key])
                {
                    _graph[key].Add(word.Key, _occurences[key][word.Key] / (double)total);
                }
            }//sort the graph here later for substantial speedup
        }

        private string Sanitize(string str)
        {
            foreach(var regex in sanitizationRegexes)
            {
                str = regex.Replace(str, String.Empty);
            }
            return str.ToLower();
        }
        private List<Regex> sanitizationRegexes = new List<Regex>
        {
            new Regex(@"[0-9]*"),
            new Regex(@"_"),
            new Regex(@"\(|\)"),
            new Regex("\""),
            new Regex("[\r\n]+"),
            new Regex(@"\s\s+")
        };

        Random r = new Random();
        public string GenerateSentence()
        {
            string seedWord = _graph.Keys.ToList()[r.Next(_graph.Count)];
            var sb = new StringBuilder();
            var prevWord = seedWord;
            int prevLength = 0;
            int wordsInSentence = 0;
            bool first = true;
            do
            {
                var r = random.NextDouble();
                double total = 0;
                var dict = _graph[prevWord];
                foreach (var word in dict)
                {
                    if (r < word.Value + total)
                    {
                        if (first)
                        {
                            var wordArr = word.Key.ToCharArray();
                            wordArr[0] = char.ToUpper(wordArr[0]);
                            first = false;
                            sb.Append(new string(wordArr) + " ");
                        }
                        else
                        {
                            sb.Append(word.Key + " ");
                        }
                        prevWord = word.Key;
                        break;
                    }
                    else
                    {
                        total += word.Value;
                    }
                }
                if(prevLength == sb.Length)
                {
                    prevWord = _graph.Keys.ToList()[random.Next(_graph.Count)];
                }
                prevLength = sb.Length;
                if(wordsInSentence < 4 && sb.ToString().Contains('.'))
                {
                    sb.Replace('.', ' ');
                }
                wordsInSentence++;
            } while (!sb.ToString().Contains('.') && !sb.ToString().Contains('?') && !sb.ToString().Contains('!'));
            return sb.ToString();
        }
        public string GenerateSentences(int length)
        {
            var bag = new ConcurrentBag<string>();
            var sb = new StringBuilder();
            Parallel.For(0, length, (i) =>
            {
                bag.Add(GenerateSentence());
            });

            foreach(var sentence in bag)
            {
                sb.Append(sentence);
            }
            return sb.ToString();
        }


        public void Dispose()
        {

        }
    }
}
