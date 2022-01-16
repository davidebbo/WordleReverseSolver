namespace WordleReverseSolver
{
    internal class TwitterSearchService
    {
        private readonly HttpClient _client;
        public TwitterSearchService()
        {
            string? bearerToken = Environment.GetEnvironmentVariable("TWITTER_BEARER_TOKEN");
            if (bearerToken == null)
            {
                throw new Exception("Please set the TWITTER_BEARER_TOKEN env variable before running");
            }

            _client = new HttpClient
            {
                BaseAddress = new Uri("https://api.twitter.com/2/tweets/search/recent"),
            };
            _client.DefaultRequestHeaders.Clear();
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {bearerToken}");
        }

        public async Task<string> GetData(string q)
        {
            var result = await _client.GetStringAsync(q);
            return result;
        }
    }
}
