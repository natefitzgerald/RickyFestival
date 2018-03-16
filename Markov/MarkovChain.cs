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
        private static List<string> properNouns = new List<string>();
        private static RNGCryptoServiceProvider random = new RNGCryptoServiceProvider();
        private static Regex punctuation = new Regex(@"\?|\!|\;|\:|\,|\.");

        public MarkovChain(string filename)
        {
            string text;

            if (File.Exists("signal.butts"))
                text = File.ReadAllText(filename);
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
            _occurences.Add("flibbartygibbet", new Dictionary<string, int>());

            foreach (var word in text.Split(' ').Skip(1))
            {
                if (_occurences.ContainsKey(prevWord))
                {
                    var wordDict = _occurences[prevWord];

                    if (wordDict.ContainsKey(word))
                        wordDict[word]++;
                    else
                    {
                        var sanitizedWord = prevWord.Replace(".", "").Replace(",", "").ToLower();
                        if (set.Contains(sanitizedWord))
                        {
                            if (wordDict.ContainsKey("flibbartygibbet")) wordDict["flibbartygibbet"]++;
                            else wordDict.Add("flibbartygibbet", 1);
                        }

                    }
                }
                else
                {
                    var sanitizedWord = punctuation.Replace(word, "").ToLower();
                    if (set.Contains(sanitizedWord) && !properNouns.Contains(word))
                        _occurences.Add(prevWord, new Dictionary<string, int>(new List<KeyValuePair<string, int>> { new KeyValuePair<string, int>(sanitizedWord, 1) }));
                    else
                    {
                        if (sanitizedWord.Length > 1)
                        {
                            properNouns.Add(sanitizedWord);
                            var wordDict = _occurences["flibbartygibbet"];
                            if (wordDict.ContainsKey(word))
                                wordDict[word]++;
                            else
                                wordDict.Add(word, 1);
                        }
                    }
                }
                prevWord = punctuation.Replace(word, "").ToLower();
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
                foreach (var word in _occurences[key])
                    _graph[key].Add(word.Key, _occurences[key][word.Key] / (double)total);

            }
        }
        public string GenerateSentences(int length)
        {
            var bag = new ConcurrentBag<string>();
            var sb = new StringBuilder();
            Parallel.For(0, length, (i) =>
            {
                bag.Add(GenerateSentence());
            });

            foreach (var sentence in bag)
            {
                sb.Append(sentence);
            }
            return sb.ToString();
        }

        private string Sanitize(string str)
        {
            foreach (var regex in sanitizationRegexes) str = regex.Replace(str, " ");
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
            new Regex(@"-"),
            new Regex(@"'"),
            new Regex(@"[^\u0000-\u007F]+"),
        };

        Random r = new Random();
        ConcurrentBag<string> recentProperNouns;
        public string GenerateSentence()
        {
            recentProperNouns = fillProperNouns();
            var sb = new StringBuilder();
            var prevWord = _graph.Keys.ToList()[r.Next(_graph.Count)];
            int prevLength = 0;
            int wordsInSentence = 0;
            bool first = true;
            do
            {
                double total = 0;
                while(!_graph.ContainsKey(prevWord)) prevWord = _graph.Keys.ToList()[GetRandomInt(_graph.Count)];
                var dict = _graph[prevWord];
               
                foreach (var pair in dict)
                {
                    var word = pair.Key;
                    if (word == "i") word = "I";
                    else if (word.Length < 2) continue;
                    var a = GetRandomDouble();
                    if (a <= pair.Value + total + MarkovConstants.ALPHA)
                    {
                        if(word == prevWord)
                        {
                            prevWord = _graph.Keys.ToList()[GetRandomInt(_graph.Count)];
                            continue;
                        }
                        if (pair.Key == "flibbartygibbet")
                        {
                            word = recentProperNouns.Skip(GetRandomInt(recentProperNouns.Count)).Take(1).First();
                            var wordArr = word.ToCharArray();
                            wordArr[0] = char.ToUpper(wordArr[0]);
                            word = new string(wordArr);
                            if(GetRandomDouble() > MarkovConstants.PROPERNOUN_OCCURANCE_MODIFIER)
                            {
                                prevWord = word;
                                continue; ;
                            }
                        }
                        if (first)
                        {
                            var wordArr = word.ToCharArray();
                            wordArr[0] = char.ToUpper(wordArr[0]);
                            first = false;
                            sb.Append(new string(wordArr) + " ");
                        }
                        else sb.Append(word + " ");
                        prevWord = word;
                        break;
                    }
                    else total += pair.Value;
                }
                if(prevLength == sb.Length) prevWord = _graph.Keys.ToList()[GetRandomInt(_graph.Count)];
                prevLength = sb.Length;
                if(wordsInSentence < MarkovConstants.MINIMUM_SENTENCE_LENGTH && (sb.ToString().Contains('.') || sb.ToString().Contains('?') || sb.ToString().Contains('!')))
                    sb.Replace('.', (char)7).Replace('?', (char)7).Replace('!', (char)7);
                wordsInSentence++;
            } while (!sb.ToString().Contains('.') && !sb.ToString().Contains('?') && !sb.ToString().Contains('!') && wordsInSentence < MarkovConstants.MAXIMUM_SENTENCE_LENGTH);
            sb.Remove(sb.Length - 1, 1);
            sb.Append(". ");
            return sb.ToString().Replace(new string(new char[] { (char)7 }), "");
        }
        private static ConcurrentBag<string> fillProperNouns()
        {
            var list = new ConcurrentBag<string>();
            for(int i = 0; i < MarkovConstants.PROPER_NOUN_LIST_LENGTH; i++)
                list.Add(properNouns[GetRandomInt(properNouns.Count)]);
            return list;
        }
        public void Dispose()
        {
            random.Dispose();
        }

        //using a CSPRNG just because native random doesn't seed well with multiple threads
        private static int GetRandomInt(int range = Int32.MaxValue)
        {
            var bytes = new byte[4];
            random.GetBytes(bytes);
            bytes[3] &= 127; //absolute value
            return BitConverter.ToInt32(bytes, 0) % range; //honestly this will bias the result slightly but idgaf
        }
        private static double GetRandomDouble() //this is super super poopy but you can't get uniformly distributed double values from bytes easily
        {
            return GetRandomInt() / (double)Int32.MaxValue;
        }
    }
}
