namespace SasflixDownloader;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public class AuthService(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly string _loginRequestUrl = "https://sasflix.ru/api/security/login";
    private readonly string _getAccountInfoRequestUrl = "https://sasflix.ru/api/user/profile";
    private static readonly string _tokenFilePath = Path.Combine(Directory.GetCurrentDirectory(), "token.txt");

    public async Task<string> GenerateTokenAsync(string username, string password)
    {
        Dictionary<string, string> body = new() 
        {
            { "username", username },
            { "password", password }
        };
        using HttpContent content = new FormUrlEncodedContent(body);
        HttpResponseMessage response = await _httpClient.PostAsync(_loginRequestUrl, content);
        
        if(!response.IsSuccessStatusCode) return "error while processing get token request";
        
        string responseData = await response.Content.ReadAsStringAsync();
        Dictionary<string, string>? responseBody = JsonSerializer.Deserialize<Dictionary<string, string>>(responseData);
        
        if(responseBody is null) return $"error while processing get token request: responseBody is null\nResponse data: {responseData}";
        if(!responseBody.TryGetValue("token", out string? tokenValue)) return $"error while processing get token request: responseBody does not contains token\nResponse data: {responseData}\n\nResponse body: {responseBody}";
        
        string token = tokenValue.ToString();
        return token;
    }

    public async Task<(bool valid, string error)> TokenIsValid(string authToken)
    {
        if(_httpClient.DefaultRequestHeaders.Authorization is null) _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");
        HttpResponseMessage response = await _httpClient.GetAsync(_getAccountInfoRequestUrl);

        if(!response.IsSuccessStatusCode) return (false, "error while processing token is valid request");
        
        string responseData = await response.Content.ReadAsStringAsync();
        Dictionary<string, object>? responseBody = JsonSerializer.Deserialize<Dictionary<string, object>>(responseData);

        if(responseBody is null) return (false, $"error while processing token is valid request: responseBody is null\nResponse data: {responseData}");
        if(!responseBody.ContainsKey("user")) return (false, $"error while processing token is valid request: responseBody does not contains user\nResponse data: {responseData}\n\nResponse body: {responseBody}");
        
        return (true, string.Empty);
    }

    public static async Task SaveTokenToFileAsync(string token)
    {
        await File.WriteAllTextAsync(_tokenFilePath, token);
    }

    public static async Task<string> GetTokenFromFileAsync()
    {
        if(!File.Exists(_tokenFilePath)) return "token file not found";
        string token = await File.ReadAllTextAsync(_tokenFilePath);
        return token;
    }
}