using SasflixDownloader;

async Task<string> GetLastVideoUrl() 
{
    using HttpClient httpClient = new();
    VideoChecker videoChecker = new(httpClient);
    string lastVideoUrl = await videoChecker.GetLastVideoUrlAsync();

    Console.WriteLine($"Last video url: {lastVideoUrl}");

    string lastSavedVideoUrl = await VideoChecker.GetLastVideoUrlFromFileAsync();
    if(lastVideoUrl == lastSavedVideoUrl) return string.Empty;

    await VideoChecker.SaveLastVideoUrlToFileAsync(lastVideoUrl);
    return lastVideoUrl;
}

async Task<string> GetAuthToken()
{
    using HttpClient httpClient = new();
    AuthService authTokenService = new(httpClient);

    string authToken = await AuthService.GetTokenFromFileAsync();
    (bool tokenIsValid, string error) = await authTokenService.TokenIsValid(authToken);

    Console.WriteLine($"token is valid: {tokenIsValid}");
    if(error != string.Empty) Console.WriteLine($"error from token is valid method: {error}");

    if(!tokenIsValid)
    {
        (string login, string password) = GetLoginData();
        authToken = await authTokenService.GenerateTokenAsync(login, password);
        Console.WriteLine($"Generated auth token: {authToken}");
        await AuthService.SaveTokenToFileAsync(authToken);
    }

    return authToken;
}

async Task DownloadVideo(string videoUrl, string authToken)
{
    HttpClient httpClient = new();
    VideoDownloader videoDownloader = new(httpClient);
    string downloadUrl = await videoDownloader.GetDownlaodUrl(videoUrl, authToken);

    Console.WriteLine($"Download url: {downloadUrl}");

    await videoDownloader.DownloadVideo(downloadUrl, authToken);
}

(string login, string password) GetLoginData()
{
    string loginData = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "loginData.txt"));
    string[] loginArray = loginData.Split("\n");
    string login = loginArray[0];
    string password = loginArray[1];
    return (login, password);
}

string lastVideoUrl = await GetLastVideoUrl();
if(lastVideoUrl == string.Empty) return;

string authToken = await GetAuthToken();

await DownloadVideo(lastVideoUrl, authToken);