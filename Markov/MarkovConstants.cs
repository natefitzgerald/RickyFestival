using System;
using System.Collections.Generic;
using System.Text;

namespace Markov
{
    static class MarkovConstants
    {
        public const int PROPER_NOUN_LIST_LENGTH = 100;
        public const int MINIMUM_SENTENCE_LENGTH = 4;
        public const int MAXIMUM_SENTENCE_LENGTH = 20;
        public const double PROPERNOUN_OCCURANCE_MODIFIER = .4f;
        public const double ALPHA = .000001f;
    }
}
