using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;

namespace Markov
{
    public class MarkovChain : IDisposable
    {
        private Dictionary<string, Dictionary<string, int>> _occurences = new Dictionary<string, Dictionary<string, int>>();
        private Dictionary<string, Dictionary<string, double>> _graph;
        private static Random random = new Random();
        private string persistenceFilepath = "graph.bin";
        private SHA256 hash;

        public MarkovChain(string text)
        {
            bool flag = true;
            var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.ASCII.GetBytes(text));
            if (!File.Exists("hash.dat"))
            {
                flag = false;
            }
            else
            {
                var oldHash = File.ReadAllBytes("hash.dat");
                for (int i = 0; i < hash.Length; i++) if (hash[i] != oldHash[i]) flag = false;
            }
            if (flag)
            {
                BinaryFormatter binform = new BinaryFormatter();
                var graph = binform.Deserialize(new FileStream(persistenceFilepath, FileMode.Open)) as Dictionary<string, Dictionary<string, double>>;
                if (graph != null)
                {
                    _graph = graph;
                }
            }
            else
            {
                File.WriteAllBytes("hash.dat", hash);
                var t = Sanitize(text);

                var prevWord = t.Substring(0, t.IndexOf(' '));
                foreach (var word in t.Split(' ').Skip(1))
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
            return str;
        }
        private List<Regex> sanitizationRegexes = new List<Regex>
        {
            new Regex(@"[0-9]*"),
            new Regex(@"_"),
            new Regex(@"\(|\)")
        };

        Random r = new Random();
        public string GenerateSentence()
        {
            string seedWord = _graph.Keys.ToList()[r.Next(_graph.Count)];
            var sb = new StringBuilder();
            var prevWord = seedWord;
            int prevLength = 0;
            int wordsInSentence = 0;
            do
            {
                var r = random.NextDouble();
                double total = 0;
                var dict = _graph[prevWord];
                foreach (var word in dict)
                {
                    if (r < word.Value + total)
                    {
                        sb.Append(word.Key + " ");
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
            } while (!sb.ToString().Contains('.'));
            return sb.ToString();
        }
        public string GenerateSentences(int length)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                sb.Append(GenerateSentence());
            }
            return sb.ToString();
        }

        public void SaveState(string filepath)
        {
            try
            {
                BinaryFormatter binform = new BinaryFormatter();
                var stream = new FileStream(filepath, FileMode.OpenOrCreate);
                binform.Serialize(stream, _graph);
            }
            catch { }
        }

        public void Dispose()
        {
            SaveState(persistenceFilepath);
        }
    }
}
