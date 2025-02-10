using HtmlAgilityPack;

namespace SasflixDownloader;

class VideoDownloader(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<string> GetDownlaodUrl(string videoUrl, string authToken)
    {
        if(_httpClient.DefaultRequestHeaders.Authorization is null) _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");
        using HttpResponseMessage httpResponse = await _httpClient.GetAsync(videoUrl);
        
        if(!httpResponse.IsSuccessStatusCode) return "error while processing get download url request";

        string html = await httpResponse.Content.ReadAsStringAsync();
        if(html == string.Empty) return "error while processing get download url request: html is empty";

        HtmlDocument htmlDocument = new();
        htmlDocument.LoadHtml(html);
        
        HtmlNode downloadButtonNode = htmlDocument.DocumentNode.SelectSingleNode("//a[contains(@class, 'btn') and contains(@class, 'btn-md') and contains(@class, 'btn-link')]");
        string downloadUrl = downloadButtonNode.GetAttributeValue("href", string.Empty);
        
        return downloadUrl;
    }

    public async Task<string> DownloadVideo(string downloadUrl, string authToken)
    {
        string finalRedirectUrl = await GetFinalRedirectUrlAsync(downloadUrl, authToken);
        string fileName = Path.GetFileName(new Uri(finalRedirectUrl).AbsolutePath);
        string filePath = Path.Combine(Directory.GetCurrentDirectory(), "videos", fileName);
        await DownloadFileWithProgressAsync(downloadUrl, filePath);
        return string.Empty;
    }

    private static async Task<string> GetFinalRedirectUrlAsync(string url, string authToken)
    {
        using HttpClientHandler handler = new() { AllowAutoRedirect = false };
        using HttpClient client = new(handler);
        
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");
        HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

        return response.Headers.Location?.AbsoluteUri ?? url; // Если есть редирект, получаем новый URL из заголовка Location, если редиректов нет, возвращаем сам URL
    }

    private async Task DownloadFileWithProgressAsync(string url, string filePath)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        long? totalBytes = response.Content.Headers.ContentLength;
        Console.WriteLine(response.Headers.Location);
        using Stream contentStream = await response.Content.ReadAsStreamAsync();
        using FileStream fileStream = new(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

        byte[] buffer = new byte[8192];
        long totalRead = 0;
        int read;

        while ((read = await contentStream.ReadAsync(buffer)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, read));
            totalRead += read;

            if (totalBytes.HasValue)
            {
                double progress = (double)totalRead / totalBytes.Value * 100;
                Console.Write($"\rСкачивание: {progress:F2}%");
            }
        }

        Console.WriteLine("\n✅ Скачивание завершено!");
    }
}