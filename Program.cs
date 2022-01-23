using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WordleReverseSolver;

//Console.WriteLine($"Starting at {DateTime.Now}");

Console.WriteLine("Reading in dictionary...");
string dictionaryJson = File.ReadAllText("dictionary.json");
var jarray = (JArray)JsonConvert.DeserializeObject(dictionaryJson);
var allWords = jarray.ToObject<string[]>();
Console.WriteLine($"Dictionary loaded with {allWords.Length} words.");
Console.WriteLine();

int puzzleNumber = int.Parse(args[0]);
Console.WriteLine($"Searching and parsing tweets for Wordle puzzle {puzzleNumber}...");
var mainPatternManager = await PatternManager.ReadFromTwitter(puzzleNumber);

//basePatternManager.SaveToFile("patterns.txt");
//var basePatternManager = await PatternManager.ReadFromFile("patterns.txt");

Console.WriteLine($"Found {mainPatternManager.Count} distinct patterns across {mainPatternManager.TweetCount} tweets.");
Console.WriteLine();


//DumpMismatchedPatternItems("hello");
//return;


Console.WriteLine("Searching for solutions:");

var results = allWords
    .AsParallel()
    .Select(candidateSolution => new Tuple<string, int>(candidateSolution, GetMismatchedPatternCount(candidateSolution)))
    .Where(pair => pair.Item2 == 0)
    .OrderBy(pair => pair.Item2);
foreach (var pair in results)
{
    //Console.WriteLine($"{pair.Item1} ({pair.Item2})");
    Console.WriteLine($"{pair.Item1}");
}

//Console.WriteLine($"Ending at {DateTime.Now}");

// Get the number of patterns that are impossible for this word
int GetMismatchedPatternCount(string candidateSolution)
{
    var patternManager = (PatternManager)mainPatternManager.Clone();
    for (int guess = 0; guess < allWords.Length; guess++)
    {
        var pattern = ScoreWord(allWords[guess], candidateSolution);
        patternManager.RemovePattern(pattern);
    }

    return patternManager.Count;
}

// For debugging purpose
void DumpMismatchedPatternItems(string candidateSolution)
{
    var patternManager = (PatternManager)mainPatternManager.Clone();
    for (int guess = 0; guess < allWords.Length; guess++)
    {
        var pattern = ScoreWord(allWords[guess], candidateSolution);
        patternManager.RemovePattern(pattern);
    }

    patternManager.DumpAllPatterns();
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

