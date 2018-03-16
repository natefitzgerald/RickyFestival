using System;
using System.Collections.Generic;
using System.Text;

namespace Markov
{
    static class MarkovConstants
    {
        public const int PROPER_NOUN_LIST_LENGTH = 10;
        public const int MINIMUM_SENTENCE_LENGTH = 4;
        public const int MAXIMUM_SENTENCE_LENGTH = 20;
        public const double ALPHA = .000001f;
    }
}
