using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

namespace Markov
{
    public class MarkovChain
    {
        private Dictionary<string, Dictionary<string, int>> _occurences = new Dictionary<string, Dictionary<string, int>>();
        private Dictionary<string, Dictionary<string, double>> _graph;

        public MarkovChain()
        {
           
        }
        public void Parse(params string[] text)
        {
            foreach(var str in text)
            {
                if (str.Length == 0) continue;
                var t = Sanitize(str);
                var prevWord = t.Substring(0, t.IndexOf(' '));
                foreach(var word in str.Split(' ').Skip(1))
                {
                    if(_occurences.ContainsKey(prevWord))
                    {

                        var wordDict = _occurences[prevWord];
                        if(wordDict.ContainsKey(word))
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
            return str.Replace(@"""", "");
        }

        Random r = new Random();
        public string Generate(int length)
        {
            int rand = r.Next(_graph.Count);
            string seedWord = _graph.Keys.ToList()[rand];
            var sb = new StringBuilder();
            var random = new Random();
            var prevWord = seedWord;
            bool beginSentence = true;
            for(int i = 0; i < length; i++)
            {
                var r = random.NextDouble();
                double total = 0;
                var dict = _graph[prevWord];
                foreach(var word in dict)
                {
                    if(r < word.Value + total)
                    {
                        if (word.Key.Length == 0) continue;
                        if (beginSentence)
                        {
                            var wordArray = word.Key.ToCharArray();
                            wordArray[0] = char.ToUpper(wordArray[0]);
                            sb.Append(new string(wordArray) + " ");
                            beginSentence = false;
                        }
                        else
                        {
                            sb.Append(word.Key + " ");
                            if (word.Key.Contains('.')) beginSentence = true;
                        }
                        prevWord = word.Key;
                        break;
                    }
                    else
                    {
                        total += word.Value;
                    }
                }
            }
            return sb.ToString();
        }

        public void SaveState(string filepath)
        {
            var json = JsonConvert.SerializeObject(_occurences);
            File.WriteAllText(filepath, json);
        }
    }
}
