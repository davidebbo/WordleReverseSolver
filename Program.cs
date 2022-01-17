using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WordleReverseSolver;

Console.WriteLine($"Starting at {DateTime.Now}");

Console.WriteLine("Reading in dictionary...");
string dictionaryJson = File.ReadAllText("dictionary.json");
var jarray = (JArray)JsonConvert.DeserializeObject(dictionaryJson);
var allWords = jarray.ToObject<string[]>();
Console.WriteLine($"Dictionary loaded with {allWords.Length} words.");
Console.WriteLine();

int puzzleNumber = int.Parse(args[0]);
Console.WriteLine($"Searching and parsing tweets for Wordle puzzle {puzzleNumber}...");
var baseScoreManager = await ScoreManager.ReadFromTwitter(puzzleNumber);

//baseScoreManager.SaveToFile("scores.txt");
//var baseScoreManager = await ScoreManager.ReadFromFile("scores.txt");

Console.WriteLine($"Found {baseScoreManager.Count} distinct score lines across {baseScoreManager.TweetCount} tweets.");
Console.WriteLine();


Console.WriteLine("Possible solutions:");

var results = allWords
    .AsParallel()
    .Select(candidateSolution => new Tuple<string, int>(candidateSolution, GetMismatchedScoreCount(candidateSolution)))
    .Where(pair => pair.Item2 < 3)
    .OrderBy(pair => pair.Item2);
foreach (var pair in results)
{
    Console.WriteLine($"{pair.Item1} ({pair.Item2})");
}

Console.WriteLine($"Ending at {DateTime.Now}");

// Get the number of score lines that are impossible for this word
int GetMismatchedScoreCount(string candidateSolution)
{
    var scoreManager = (ScoreManager)baseScoreManager.Clone();
    for (int guess = 0; guess < allWords.Length; guess++)
    {
        var score = ScoreWord(allWords[guess], candidateSolution);
        scoreManager.RemoveScore(score);
    }

    return scoreManager.Count;
}

int[] ScoreWord(string guess, string solution)
{
    var used = new bool[solution.Length];
    var score = new int[solution.Length];

    // Go through letters in the guess
    for (int i = 0; i < solution.Length; i++)
    {
        // Match in correct position
        if (guess[i] == solution[i])
        {
            used[i] = true;
            score[i] = 2;
            continue;
        }

        // Look for match in an incorrect position
        for (int j = 0; j < solution.Length; j++)
        {
            if (!used[j])
            {
                if (guess[i] == solution[j] && guess[j] != solution[j])
                {
                    used[j] = true;
                    score[i] = 1;
                    break;
                }

            }
        }

        // Leave as 0 if no match
    }

    return score;
}

