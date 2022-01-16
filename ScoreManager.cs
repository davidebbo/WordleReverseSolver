﻿using Newtonsoft.Json;

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

        HashSet<int> scores = new HashSet<int>();

        private ScoreManager() { }

        async public static Task<ScoreManager> ReadFromTwitter(int puzzleNumber)
        {
            var manager = new ScoreManager();

            var twitterSearch = new TwitterSearchService();
            string? nextToken = null;

            for (; ; )
            {
                //var query = $"?query=wordle {puzzleNumber}";
                var query = $"?query=%22Wordle {puzzleNumber}%22";
                if (nextToken != null) query += $"&next_token={nextToken}";

                var res = await twitterSearch.GetData(query);

                dynamic twitterResponse = JsonConvert.DeserializeObject(res);

                nextToken = twitterResponse.meta.next_token;

                foreach (var tweet in twitterResponse.data)
                {
                    //Console.WriteLine(tweet.id);

                    manager.ParseTweetText((string)tweet.text, puzzleNumber);
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
                manager.scores.Add(int.Parse(line));
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

        void ParseTweetText(string tweetText, int puzzleNumber)
        {
            int tweetTextIndex = tweetText.IndexOf($"Wordle {puzzleNumber} ");

            // Bail out if it doesn't seem to relate to this puzzle number
            if (tweetTextIndex < 0)
            {
                Console.WriteLine($"Not a tweet for this puzzle: {tweetText}");
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
                            Console.WriteLine($"Invalid second char at {tweetTextIndex}: {(int)tweetText[tweetTextIndex]}: {tweetText}");
                            return;
                        }
                    }
                    else if (tweetText[tweetTextIndex] == 0xFE0F) {
                        // Sometimes we get this character between real ones. Maybe some kind of space? Ignore it
                    }
                    else
                    {
                        Console.WriteLine($"Invalid solution char at {tweetTextIndex}: {(int)tweetText[tweetTextIndex]}: {tweetText}");
                        return;
                    }
                }

                // Last line with all correct, so no need to look further
                if (AddScore(score) == allCorrectScore) return;
            }
        }

        private int AddScore(int[] scoreArray)
        {
            int integerScore = ScoreArrayToSingleInteger(scoreArray);
            scores.Add(integerScore);
            return integerScore;
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
            manager.scores = new HashSet<int>(scores);
            return manager;
        }
    }
}