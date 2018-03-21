using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Markov
{
    public class Markov2
    {
        Dictionary<string, ChainWord> dict;
        Dictionary<int, int> sentenceLength = new Dictionary<int, int>();
        public Markov2(string text)
        {
            text = Sanitize(text);
            dict = new Dictionary<string, ChainWord>();
            foreach(var sentence in text.Split('.'))
            {
                var words = sentence.Split(' ');
                words = words.Where(q => q.Length >= 2).ToArray();
                for(int i = 0; i < words.Length; i++)
                {
                    var word = words[i];
                    var cap = false;
                    if (i != 0 && char.IsUpper(word[0])) cap = true;
                    word = word.ToLower();
                    ChainWord chainWord;
                    if (dict.ContainsKey(word))
                    {
                        chainWord = dict[word];
                    }
                    else
                    {
                        chainWord = new ChainWord(word);
                    }
                        for(int j = 0; j < words.Length; j++)
                        {
                            if (i == j) continue;
                            if(chainWord.near.ContainsKey(words[j].ToLower()))
                            {
                                chainWord.near[words[j].ToLower()] += Dist(j, i);
                            }
                            else
                            {
                                chainWord.near.Add(words[j].ToLower(), Dist(j, i));
                            }
                        }
                        dict.Add(word, chainWord);
                    
                    chainWord.endTotal++;
                }

                if (sentenceLength.ContainsKey(words.Length)) sentenceLength[words.Length]++;
                else sentenceLength.Add(words.Length, 1);
            }
        }
        Random rand1 = new Random();
        public string GenerateSentence()
        {
            var sb = new StringBuilder();
            bool finished = false;
            var prev = dict.Keys.ToList()[rand1.Next(dict.Count)];
            int count = 12;
            sb.Append(Capitalize(prev) + " ");
            do
            {
                var word = dict[prev];
                while(word.word == prev) word = dict[dict.Keys.ToList()[rand1.Next(dict.Count)]];
                int r = rand1.Next(word.Sum);
                
                foreach(var key in word.near)
                {
                    r -= count < 8  && (double)dict[key.Key].end / dict[key.Key].endTotal > .85f ? key.Value - count * count : key.Value;
                    if(r <= 0)
                    {
                        sb.Append(dict[key.Key].capitalized ? Capitalize(key.Key) + " " : key.Key + " ");
                        prev = key.Key;
                        break;
                    }
                }
                count--;
            } while (!finished && count != 0);
            return sb.ToString().Substring(0, sb.ToString().Length - 1) + ".";
        }

        private int Dist(int j, int i) => j > i ? (j - i) * (j - i) : i > j ? (i - j) * (i - j) : 0;
        private string Capitalize(string word) =>  new string(word.ToCharArray().Take(1).Select(c => char.ToUpper(c)).Concat(word.ToCharArray().Skip(1)).ToArray());

        private class ChainWord
        {
            public string word;
            public Dictionary<string, int> near = new Dictionary<string, int>();
            public bool capitalized = false;
            private int _sum = -1;
            public int end = 0;
            public int endTotal = 0;
            public ChainWord(string word)
            {
                this.word = word;
            }
            public int Sum { get => _sum == -1 ? (_sum = near.Values.Sum()) : _sum; }
        }
        private string Sanitize(string str)
        {
            foreach (var regex in sanitizationRegexes) str = regex.Replace(str, " ");
            return str;
        }
        private List<Regex> sanitizationRegexes = new List<Regex>
        {
            new Regex(@"\s[0-9]+"),
            new Regex(@"_"),
            new Regex(@"\(|\)|\[|\]|\|"),
            new Regex("\""),
            new Regex("_"),
            new Regex("[\r\n]+"),
            new Regex(@"-"),
            new Regex(@"'"),
            new Regex(@"[^\u0000-\u007F]+"),
            new Regex(@"\s\s+"),
        };
    }
}
