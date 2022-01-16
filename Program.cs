using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WordleReverseSolver;

Console.WriteLine($"Starting at {DateTime.Now}");

Console.WriteLine("Reading in dictionary...");
string dictionaryJson = File.ReadAllText("dictionary.json");
var jarray = (JArray)JsonConvert.DeserializeObject(dictionaryJson);
var dictionary = jarray.ToObject<string[]>();

//int puzzleNumber = int.Parse(args[0]);
//var baseScoreManager = await ScoreManager.ReadFromTwitter(puzzleNumber);

//baseScoreManager.SaveToFile("scores.txt");
var baseScoreManager = await ScoreManager.ReadFromFile("scores.txt");

Console.WriteLine($"Number of distinct scores: {baseScoreManager.Count}");


Console.WriteLine("Possible solutions:");

for (int candidate = 0; candidate < dictionary.Length; candidate++)
{
    if (candidate % 1000 == 0) Console.WriteLine(candidate);
    var scoreManager = (ScoreManager) baseScoreManager.Clone();
    for (int guess = 0; guess < dictionary.Length; guess++)
    {
        var score = ScoreWord(dictionary[guess], dictionary[candidate]);
        scoreManager.RemoveScore(score);
    }

    if (scoreManager.Count == 0)
    {
        Console.WriteLine($"{dictionary[candidate]}");
    }
}

Console.WriteLine($"Ending at {DateTime.Now}");

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

