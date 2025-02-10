using HtmlAgilityPack;

namespace SasflixDownloader;

public class VideoChecker(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly static string _lastVideoUrlFilePath = Path.Combine(Directory.GetCurrentDirectory(), "lastVideoUrl.txt");
    private readonly string _getAllVideosUrl = "https://sasflix.ru/";

    public async Task<string> GetLastVideoUrlAsync()
    {
        using HttpResponseMessage response = await _httpClient.GetAsync(_getAllVideosUrl);
        
        if(!response.IsSuccessStatusCode) return "error while processing get last video request";
        if(response.Content is null) return "error while processing get last video request: response content is null";
        
        string html = await response.Content.ReadAsStringAsync();
        
        if(string.IsNullOrEmpty(html)) return "error while processing get last video request: response content is empty";
        HtmlDocument htmlDocument = new();
        htmlDocument.LoadHtml(html);
        var lastVideoNode = htmlDocument.DocumentNode.SelectSingleNode("//div[contains(@class, 'topic') and contains(@class, 'teaser') and contains(@class, 'recent')]//a[@href]");
        
        if(lastVideoNode is null) return "error while processing get last video request: last video node is null\n";
        
        string lastVideoUrl = lastVideoNode.GetAttributeValue("href", string.Empty);
        
        if(lastVideoUrl != string.Empty) lastVideoUrl =  _getAllVideosUrl + lastVideoUrl;

        return lastVideoUrl;
    }

    public static async Task SaveLastVideoUrlToFileAsync(string lastVideoUrl)
    {
        await File.WriteAllTextAsync(_lastVideoUrlFilePath, lastVideoUrl);
    }

    public static async Task<string> GetLastVideoUrlFromFileAsync()
    {
        if (!File.Exists(_lastVideoUrlFilePath)) return "last video url file not found";
        string lastVideoUrl = await File.ReadAllTextAsync(_lastVideoUrlFilePath);
        return lastVideoUrl;
    }
}