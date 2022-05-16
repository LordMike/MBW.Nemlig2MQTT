using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MBW.Client.NemligCom.Extensions;

internal static class HttpClientExtensons 
{
    private static readonly Encoding Utf8Encoding = new UTF8Encoding(false);

    public static async Task<HttpResponseMessage> PostJson<T>(this HttpClient http, string resource, T obj, HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead, CancellationToken token = default) where T : notnull
    {
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, resource);
        request.SetJsonContent(JsonSerializer.Create(), obj);

        HttpResponseMessage response = await http.SendAsync(request, completionOption, token);

        return response;
    }

    public static Task<T> ReadAsJson<T>(this HttpContent content, CancellationToken token = default)
    {
        return ReadAsJson<T>(content, JsonSerializer.Create(), token);
    }

    public static async Task<T> ReadAsJson<T>(this HttpContent content, JsonSerializer serializer, CancellationToken token = default)
    {
        using Stream stream = await content.ReadAsStreamAsync();

        using StreamReader sr = new StreamReader(stream);
        using JsonTextReader jr = new JsonTextReader(sr);

        return serializer.Deserialize<T>(jr);
    }

    private static void SetJsonContent<T>(this HttpRequestMessage request, JsonSerializer serializer, T obj)
    {
        var stream = new MemoryStream();

        using (StreamWriter sw = new StreamWriter(stream, Utf8Encoding, 4096, true))
        using (JsonTextWriter jw = new JsonTextWriter(sw))
        {
            serializer.Serialize(jw, obj);
        }

        stream.Seek(0, SeekOrigin.Begin);

        request.Content = new StreamContent(stream);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json")
        {
            CharSet = "UTF-8"
        };
    }
}