using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace V275_REST_Lib;

public partial class Connection : ObservableObject
{
    [ObservableProperty] private bool isException;
    [ObservableProperty] private Exception? exception;
    partial void OnExceptionChanged(Exception? value)
    {
        if (value != null)
        {
            IsException = true;
            LogError(value);
        }
        else
            IsException = false;
    }
    [ObservableProperty] private HttpResponseMessage? httpResponseMessage;

    private void Reset()
    {
        Exception = null;
        HttpResponseMessage = null;
    }

    public void CertOverride() => ServicePointManager.ServerCertificateValidationCallback += (sender1, certificate, chain, sslPolicyErrors) => true;
    public void CertNormal() => ServicePointManager.ServerCertificateValidationCallback -= (sender1, certificate, chain, sslPolicyErrors) => true;

    public async Task<string> Get_Token(string url, string user, string pass)
    {
        LogDebug($"GET TOKEN: {url}");

        Reset();

        try
        {
            using HttpClient client = new();
            client.BaseAddress = new System.Uri(url);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes($"{user}:{pass}")));
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"));

            HttpContent content = new StringContent("", UTF8Encoding.UTF8, "*/*");
            HttpResponseMessage = await client.PutAsync(url, content);

            return HttpResponseMessage.IsSuccessStatusCode ? HttpResponseMessage.Headers.GetValues("Authorization").FirstOrDefault() : null;
        }
        catch (Exception ex)
        {
            Exception = ex;
            return null;
        }
    }
    public async Task<bool> Post(string url, string data, string token)
    {
        LogDebug($"POST: {url}");

        Reset();

        try
        {
            using HttpClient client = new();
            client.BaseAddress = new System.Uri(url);
            if (!string.IsNullOrEmpty(token))
                _ = client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", token);
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"));

            HttpContent content = new StringContent(data, UTF8Encoding.UTF8, "*/*");
            HttpResponseMessage = await client.PostAsync(url, content);

            return HttpResponseMessage.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Exception = ex;
            return false;
        }
    }
    public async Task<bool> Put(string url, string data, string token)
    {
        LogDebug($"PUT: {url}");

        Reset();

        try
        {
            using HttpClient client = new();
            client.BaseAddress = new System.Uri(url);
            if (!string.IsNullOrEmpty(token))
                _ = client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", token);
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"));

            //This sets Content-Type: text/plain; charset=utf-8
            HttpContent content = new StringContent(data);

            //This is required for the server to accept the data as plain text
            //This sets Content-Type: text/plain
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
            HttpResponseMessage = await client.PutAsync(url, content);

            return HttpResponseMessage.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Exception = ex;
            return false;
        }
    }
    public async Task<bool> Put(string url, byte[] data, string token)
    {
        LogDebug($"PUT: {url}");

        Reset();

        try
        {
            using HttpClient client = new();
            client.BaseAddress = new System.Uri(url);
            if (!string.IsNullOrEmpty(token))
                _ = client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", token);
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"));

            HttpContent content = new ByteArrayContent(data);
            HttpResponseMessage = await client.PutAsync(url, content);

            return HttpResponseMessage.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Exception = ex;
            return false;
        }
    }
    public async Task<bool> Patch(string url, string data, string token)
    {
        LogDebug($"PATCH: {url}");

        try
        {
            using HttpClient client = new();
            client.BaseAddress = new System.Uri(url);
            if (!string.IsNullOrEmpty(token))
                _ = client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", token);
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"));

            HttpContent content = new StringContent(data, UTF8Encoding.UTF8, "*/*");
            HttpRequestMessage request = new(new HttpMethod("PATCH"), url)
            { Content = content };

            HttpResponseMessage = await client.SendAsync(request);

            return HttpResponseMessage.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Exception = ex;
            return false;
        }
    }
    public async Task<bool> Delete(string url, string token)
    {
        LogDebug($"DELETE: {url}");

        Reset();

        try
        {
            using HttpClient client = new();
            client.BaseAddress = new System.Uri(url);
            if (!string.IsNullOrEmpty(token))
                _ = client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", token);
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"));

            HttpResponseMessage = await client.DeleteAsync(url);

            return HttpResponseMessage.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Exception = ex;
            return false;
        }
    }
    public async Task<string> Get(string url, string token)
    {
        LogDebug($"GET: {url}");

        Reset();

        try
        {
            using HttpClient client = new();
            client.BaseAddress = new System.Uri(url);
            if (!string.IsNullOrEmpty(token))
                _ = client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", token);
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"));

            HttpResponseMessage = await client.GetAsync(url);

            return HttpResponseMessage.IsSuccessStatusCode ? HttpResponseMessage.Content.ReadAsStringAsync().Result : null;
        }
        catch (Exception ex)
        {
            Exception = ex;
            return null;
        }
    }
    public async Task<byte[]> GetBytes(string url, string token)
    {
        LogDebug($"GET: {url}");

        Reset();

        try
        {
            using HttpClient client = new();
            client.BaseAddress = new System.Uri(url);
            if (!string.IsNullOrEmpty(token))
                _ = client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", token);
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("image/bmp"));

            HttpResponseMessage = await client.GetAsync(url);

            return HttpResponseMessage.IsSuccessStatusCode ? HttpResponseMessage.Content.ReadAsByteArrayAsync().Result : null;
        }
        catch (Exception ex)
        {
            Exception = ex;
            return null;
        }
    }
    public async Task<Stream> Stream(string url, string token)
    {
        LogDebug($"STREAM: {url}");

        Reset();

        try
        {
            using HttpClient client = new();
            client.BaseAddress = new System.Uri(url);
            if (!string.IsNullOrEmpty(token))
                _ = client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", token);
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));

            return await client.GetStreamAsync(url);
        }
        catch (Exception ex)
        {
            Exception = ex;
            return null;
        }
    }

    #region Logging
    private static void LogInfo(string message) => Logging.lib.Logger.LogInfo(typeof(Controller), message);
#if DEBUG
    private static void LogDebug(string message) => Logging.lib.Logger.LogDebug(typeof(Controller), message);
#else
    private void LogDebug(string message) { }
#endif
    private static void LogWarning(string message) => Logging.lib.Logger.LogInfo(typeof(Controller), message);
    private static void LogError(string message) => Logging.lib.Logger.LogError(typeof(Controller), message);
    private static void LogError(Exception ex) => Logging.lib.Logger.LogError(typeof(Controller), ex);
    private static void LogError(Exception ex, string message) => Logging.lib.Logger.LogError(typeof(Controller), ex, message);

    #endregion
}
