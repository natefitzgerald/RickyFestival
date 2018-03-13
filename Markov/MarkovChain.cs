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
        private List<string> properNouns = new List<string>();
        private static Random random = new Random();

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

            var lines = File.ReadAllLines("dictionary.txt");
            var set = new HashSet<string>(lines);
            var prevWord = text.Substring(0, text.IndexOf(' '));
            _occurences.Add("PROPER_NOUN", new Dictionary<string, int>());
            bool inProperNoun = false;

            foreach (var word in text.Split(' ').Skip(1))
            {
                if (_occurences.ContainsKey(prevWord))
                {

                    var wordDict = _occurences[prevWord];
                    if (wordDict.ContainsKey(word))
                    {
                        inProperNoun = false;
                        wordDict[word]++;
                    }
                    else
                    {
                        if (!inProperNoun)
                        {
                            inProperNoun = true;
                            var sanitizedWord = prevWord.Replace(".", "").Replace(",", "").ToLower();
                            if (set.Contains(sanitizedWord))
                            {
                                if (wordDict.ContainsKey("PROPER_NOUN")) wordDict["PROPER_NOUN"]++;
                                else wordDict.Add("PROPER_NOUN", 1);
                            }
                        }
                    }
                }
                else
                {
                    var sanitizedWord = prevWord.Replace(".", "").Replace(",", "").ToLower();
                    if (set.Contains(sanitizedWord) && !properNouns.Contains(word))
                    {
                        _occurences.Add(prevWord, new Dictionary<string, int>(new List<KeyValuePair<string, int>> { new KeyValuePair<string, int>(word, 1) }));
                        inProperNoun = false;
                    }
                    else
                    {
                        if (!inProperNoun)
                        {
                            inProperNoun = true;
                            properNouns.Add(prevWord);
                            var wordDict = _occurences["PROPER_NOUN"];
                            if (wordDict.ContainsKey(word))
                            {
                                wordDict[word]++;
                            }
                            else
                            {
                                wordDict.Add(word, 1);
                            }
                        }
                    }
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
            foreach (var regex in sanitizationRegexes)
            {
                str = regex.Replace(str, " ");
            }
            return str;//.ToLower();
        }
        private List<Regex> sanitizationRegexes = new List<Regex>
        {
            new Regex(@"[0-9]"),
            new Regex(@"_"),
            new Regex(@"\(|\)|\[|\]|\|"),
            new Regex("\""),
            new Regex("_"),
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
            List<string> recentProperNouns = fillProperNouns();
            bool first = true;
            do
            {
                var r = random.NextDouble();
                double total = 0;
                while(!_graph.ContainsKey(prevWord)) prevWord = _graph.Keys.ToList()[random.Next(_graph.Count)];
                var dict = _graph[prevWord];
               
                foreach (var pair in dict)
                {
                    var word = pair.Key;
                    if (r < pair.Value + total)
                    {
                        if(pair.Key == "PROPER_NOUN")
                        {
                            var randomNo = random.Next(2);
                            if (randomNo == 0)
                            {
                                word = properNouns[0];
                                for (int n = 1; (double)1 / n > (double)1 / recentProperNouns.Count; n++) if (random.NextDouble() < n) word = properNouns[n];
                            }
                            else
                            {
                                string newProperNoun = "";
                                while(newProperNoun.Length == 0) newProperNoun = properNouns[random.Next(properNouns.Count)];
                                recentProperNouns.Insert(0, newProperNoun);
                                word = newProperNoun;
                            }
                            
                        }
                        if (first)
                        {
                            var wordArr = word.ToCharArray();
                            wordArr[0] = char.ToUpper(wordArr[0]);
                            first = false;
                            sb.Append(new string(wordArr) + " ");
                        }
                        else
                        {
                            sb.Append(word + " ");
                        }
                        prevWord = word;
                        break;
                    }
                    else
                    {
                        total += pair.Value;
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
            var str = sb.ToString();
            return str + " ";
        }
        private List<string> fillProperNouns()
        {
            var list = new List<string>();
            do
            {
                var name = properNouns[random.Next(properNouns.Count)];
                if (name.Length >= 2 && ! name.Contains("."))
                {
                    list.Add(name);
                }
            } while (list.Count < 10);
            return list;
        }
        public string GenerateSentences(int length)
        {
            var bag = new ConcurrentBag<string>();
            var sb = new StringBuilder();
            Parallel.For(0, length, (i) =>
            {
                var sentence = "";
                while(sentence.Length < 5) sentence = GenerateSentence();
                bag.Add(sentence);
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
