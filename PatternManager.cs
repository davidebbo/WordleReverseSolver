using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace WordleReverseSolver
{
    internal class PatternData
    {
        // Number of time it was found in tweets
        public int IncidenceCount { get; set; }

        // ID of some tweet that contains this pattern
        public string SampleTweetId { get; set; }
    }

    internal class PatternManager: ICloneable
    {
        const int WordLength = 5;

        // Black and gray are alternative that mean the same thing
        const char black = (char)0x2B1B;
        const char gray = (char)0x2B1C;

        const char yellowGreenFirstChar = (char)0xD83D;
        const char yellowSecondChar = (char)0xDFE8;
        const char greenSecondChar = (char)0xDFE9;

        const int allCorrectPattern = 242;    // All 2s, so 3^5-1

        Dictionary<int, PatternData> _patterns = new Dictionary<int, PatternData>();
        public int TweetCount { get; private set; }

        private PatternManager() { }

        async public static Task<PatternManager> ReadFromTwitter(int puzzleNumber)
        {
            var manager = new PatternManager();

            var twitterSearch = new TwitterSearchService();
            string? nextToken = null;

            for (int page = 0; page < 100; page++)
            {
                var query = $"?query=%22Wordle {puzzleNumber}%22 -RT";
                if (nextToken != null) query += $"&next_token={nextToken}";

                var res = await twitterSearch.GetData(query);

                dynamic twitterResponse = JsonConvert.DeserializeObject(res);

                nextToken = twitterResponse.meta.next_token;

                foreach (var tweet in twitterResponse.data)
                {
                    //Console.WriteLine(tweet.id);
                    try
                    {
                        manager.ParseTweetText((string)tweet.id, (string)tweet.text, puzzleNumber);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to process tweet {tweet.id}: { ex.Message}");
                    }

                    manager.TweetCount++;
                }

                if (nextToken == null || manager._patterns.Count > 100) break;
            }

            return manager;
        }

        public bool RemovePattern(int[] patternArray)
        {
            int patternInteger = PatternArrayToSingleInteger(patternArray);
            return _patterns.Remove(patternInteger);
        }

        public int Count { get { return _patterns.Count; } }

        public int GetTotalPatternIncidenceCount()
        {
            int totalIncidenceCount = 0;
            foreach (var pattern in _patterns)
            {
                totalIncidenceCount += pattern.Value.IncidenceCount;
            }

            return totalIncidenceCount;
        }

        void ParseTweetText(string tweetId, string tweetText, int puzzleNumber)
        {
            int tweetTextIndex = tweetText.IndexOf($"Wordle {puzzleNumber} ", StringComparison.InvariantCultureIgnoreCase);

            // Bail out if it doesn't seem to relate to this puzzle number
            if (tweetTextIndex < 0)
            {
                //Console.WriteLine($"Not a tweet for this puzzle: {tweetId}");
                return;
            };

            var bannedRegexes = new string[] {
                "http://",  // Links are often a sign of some non-English Wordle, e.g. wordle.at
                "https://",
                "[\u3040-\u30ff\u3400-\u4dbf\u4e00-\u9fff\uf900-\ufaff\uff66-\uff9f]",  // Japanese/Chinese characters
                "[\u0600-\u06ff]|[\u0750-\u077f]|[\ufb50-\ufc3f]|[\ufe70-\ufefc]",      // Arabic characters
                @"\p{IsCyrillic}"   // Russian characters
            };
            foreach (var bannedRegex in bannedRegexes)
            {
                if (Regex.IsMatch(tweetText, bannedRegex))
                {
                    Console.WriteLine($"Ignoring tweet {tweetId} which contains banned regex '{bannedRegex}'");
                    return;
                }
            }

            //tweetText.IndexOfAny()

            for (; ; )
            {
                int[] pattern = new int[WordLength];
                int patternIndex = 0;

                tweetTextIndex = tweetText.IndexOfAny(new char[] { black, gray, yellowGreenFirstChar }, tweetTextIndex);
                if (tweetTextIndex < 0) return;

                for (; patternIndex < WordLength; tweetTextIndex++)
                {
                    if (tweetText[tweetTextIndex] == black || tweetText[tweetTextIndex] == gray)
                    {
                        pattern[patternIndex++] = 0;
                    }
                    else if (tweetText[tweetTextIndex] == yellowGreenFirstChar)
                    {
                        tweetTextIndex++;
                        if (tweetText[tweetTextIndex] == yellowSecondChar)
                        {
                            pattern[patternIndex++] = 1;
                        }
                        else if (tweetText[tweetTextIndex] == greenSecondChar)
                        {
                            pattern[patternIndex++] = 2;
                        }
                        else
                        {
                            //Console.WriteLine($"Invalid pattern char in tweet {tweetId} at index {tweetTextIndex}: {(int)tweetText[tweetTextIndex]}");
                            return;
                        }
                    }
                    else if (tweetText[tweetTextIndex] == 0xFE0F)
                    {
                        // Sometimes we get this character between real ones. Maybe some kind of space? Ignore it
                    }
                    else
                    {
                        //Console.WriteLine($"Invalid pattern char in tweet {tweetId} at index {tweetTextIndex}: {(int)tweetText[tweetTextIndex]}");
                        return;
                    }
                }

                // Last line with all correct, so no need to look further
                if (AddPattern(pattern, tweetId) == allCorrectPattern) return;
            }
        }

        private int AddPattern(int[] patternArray, string tweetId)
        {
            int integerPattern = PatternArrayToSingleInteger(patternArray);
            if (!_patterns.ContainsKey(integerPattern))
            {
                _patterns[integerPattern] = new PatternData { SampleTweetId = tweetId };
            }

            _patterns[integerPattern].IncidenceCount++;
            return integerPattern;
        }

        internal void DumpAllPatterns()
        {
            foreach (var entry in _patterns)
            {
                Console.Write($"{entry.Value.SampleTweetId} ({entry.Value.IncidenceCount}): ");
                DumpPattern(SingleIntegerToArrayPattern(entry.Key));
            }
        }

        static int PatternArrayToSingleInteger(int[] patternArray)
        {
            int num = 0;
            for (int i = 0; i < patternArray.Length; i++)
            {
                num *= 3;
                num += patternArray[i];
            }

            return num;
        }

        static int[] SingleIntegerToArrayPattern(int integerPattern)
        {
            var patternArray = new int[WordLength];

            for (int i = WordLength-1; i >= 0; i--)
            {
                patternArray[i] = integerPattern % 3;
                integerPattern /= 3;
            }

            return patternArray;
        }

        static void DumpPattern(int[] patternArray)
        {
            for (int i = 0; i < patternArray.Length; i++)
            {
                Console.Write(patternArray[i]);
            }
            Console.WriteLine();
        }

        public object Clone()
        {
            var manager = new PatternManager();
            manager._patterns = new Dictionary<int, PatternData>(_patterns);
            return manager;
        }
    }
}
