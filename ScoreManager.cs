using Newtonsoft.Json;

namespace WordleReverseSolver
{
    internal class ScoreManager: ICloneable
    {
        const int WordLength = 5;

        // Black and gray are alternative that mean the same thing
        const char black = (char)0x2B1B;
        const char gray = (char)0x2B1C;

        const char yellowGreenFirstChar = (char)0xD83D;
        const char yellowSecondChar = (char)0xDFE8;
        const char greenSecondChar = (char)0xDFE9;

        const int allCorrectScore = 242;    // All 2s, so 3^5-1

        Dictionary<int,string> scores = new Dictionary<int,string>();
        public int TweetCount { get; private set; }

        private ScoreManager() { }

        async public static Task<ScoreManager> ReadFromTwitter(int puzzleNumber)
        {
            var manager = new ScoreManager();

            var twitterSearch = new TwitterSearchService();
            string? nextToken = null;

            for (; ; )
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
                }

                if (nextToken == null || manager.scores.Count > 100) break;
            }

            return manager;
        }

        async public static Task<ScoreManager> ReadFromFile(string fileName)
        {
            var manager = new ScoreManager();

            foreach (var line in await File.ReadAllLinesAsync(fileName))
            {
                manager.scores[int.Parse(line)] = String.Empty;
            }

            return manager;
        }

        public void SaveToFile(string fileName)
        {
            using (var writer = new StreamWriter(fileName))
            {
                foreach (var integerScore in scores)
                {
                    writer.WriteLine(integerScore.ToString());
                }
            }
        }

        public bool RemoveScore(int[] scoreArray)
        {
            int scoreInteger = ScoreArrayToSingleInteger(scoreArray);
            return scores.Remove(scoreInteger);
        }

        public int Count { get { return scores.Count; } }

        void ParseTweetText(string tweetId, string tweetText, int puzzleNumber)
        {
            int tweetTextIndex = tweetText.IndexOf($"Wordle {puzzleNumber} ", StringComparison.InvariantCultureIgnoreCase);

            // Bail out if it doesn't seem to relate to this puzzle number
            if (tweetTextIndex < 0)
            {
                //Console.WriteLine($"Not a tweet for this puzzle: {tweetId}");
                return;
            };

            for (; ; )
            {
                int[] score = new int[WordLength];
                int scoreIndex = 0;

                tweetTextIndex = tweetText.IndexOfAny(new char[] { black, gray, yellowGreenFirstChar }, tweetTextIndex);
                if (tweetTextIndex < 0) return;

                for (; scoreIndex < WordLength; tweetTextIndex++)
                {
                    if (tweetText[tweetTextIndex] == black || tweetText[tweetTextIndex] == gray)
                    {
                        score[scoreIndex++] = 0;
                    }
                    else if (tweetText[tweetTextIndex] == yellowGreenFirstChar)
                    {
                        tweetTextIndex++;
                        if (tweetText[tweetTextIndex] == yellowSecondChar)
                        {
                            score[scoreIndex++] = 1;
                        }
                        else if (tweetText[tweetTextIndex] == greenSecondChar)
                        {
                            score[scoreIndex++] = 2;
                        }
                        else
                        {
                            //Console.WriteLine($"Invalid score char in tweet {tweetId} at index {tweetTextIndex}: {(int)tweetText[tweetTextIndex]}");
                            return;
                        }
                    }
                    else if (tweetText[tweetTextIndex] == 0xFE0F)
                    {
                        // Sometimes we get this character between real ones. Maybe some kind of space? Ignore it
                    }
                    else
                    {
                        //Console.WriteLine($"Invalid score char in tweet {tweetId} at index {tweetTextIndex}: {(int)tweetText[tweetTextIndex]}");
                        return;
                    }
                }

                TweetCount++;

                // Last line with all correct, so no need to look further
                if (AddScore(score, tweetId) == allCorrectScore) return;
            }
        }

        private int AddScore(int[] scoreArray, string tweetId)
        {
            int integerScore = ScoreArrayToSingleInteger(scoreArray);
            scores[integerScore] = tweetId;
            return integerScore;
        }

        internal void DumpAllScoreItems()
        {
            foreach (var entry in scores)
            {
                Console.Write($"{entry.Value}: ");
                DumpScore(SingleIntegerToArrayScore(entry.Key));
            }
        }

        static int ScoreArrayToSingleInteger(int[] scoreArray)
        {
            int num = 0;
            for (int i = 0; i < scoreArray.Length; i++)
            {
                num *= 3;
                num += scoreArray[i];
            }

            return num;
        }

        static int[] SingleIntegerToArrayScore(int integerScore)
        {
            var scoreArray = new int[WordLength];

            for (int i = WordLength-1; i >= 0; i--)
            {
                scoreArray[i] = integerScore % 3;
                integerScore /= 3;
            }

            return scoreArray;
        }

        static void DumpScore(int[] scoreArray)
        {
            for (int i = 0; i < scoreArray.Length; i++)
            {
                Console.Write(scoreArray[i]);
            }
            Console.WriteLine();
        }

        public object Clone()
        {
            var manager = new ScoreManager();
            manager.scores = new Dictionary<int, string>(scores);
            return manager;
        }
    }
}
