using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using EasyCoop.ModManager.Models;

namespace EasyCoop.ModManager.Services;

public sealed class PortalApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public PortalApiClient(string baseUrl)
    {
        var baseUri = ValidateBaseUrl(baseUrl);
        _httpClient = new HttpClient { BaseAddress = baseUri, Timeout = TimeSpan.FromMinutes(10) };
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd($"EasyCoop-Mod-Manager/{BuildInfo.Version}");
    }

    public async Task<PortalLoginResponse> LoginAsync(string username, string password, CancellationToken token = default)
    {
        using var response = await _httpClient.PostAsJsonAsync("login", new { username, password }, token);
        await EnsureSuccessAsync(response, token);
        var login = await response.Content.ReadFromJsonAsync<PortalLoginResponse>(_json, token);
        if (login is null || string.IsNullOrWhiteSpace(login.Token)) throw new InvalidDataException("Ungültige Anmeldeantwort des Portalservers.");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
        return login;
    }

    public async Task<IReadOnlyList<PortalMod>> GetModsAsync(string gameVersion, CancellationToken token = default)
    {
        var path = $"mods?game={Uri.EscapeDataString(gameVersion)}";
        using var response = await _httpClient.GetAsync(path, token);
        await EnsureSuccessAsync(response, token);
        return await response.Content.ReadFromJsonAsync<List<PortalMod>>(_json, token) ?? new List<PortalMod>();
    }

    public async Task<string> DownloadAsync(PortalMod mod, CancellationToken token = default)
    {
        using var response = await _httpClient.GetAsync($"mods/{Uri.EscapeDataString(mod.Id)}/download",
            HttpCompletionOption.ResponseHeadersRead, token);
        await EnsureSuccessAsync(response, token);
        var safeName = Path.GetFileName(mod.FileName);
        if (string.IsNullOrWhiteSpace(safeName) || !safeName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Der Server hat keinen gültigen ZIP-Dateinamen geliefert.");

        var temporary = Path.Combine(Path.GetTempPath(), $"easycoop-{Guid.NewGuid():N}-{safeName}");
        await using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
            await response.Content.CopyToAsync(output, token);

        if (!string.IsNullOrWhiteSpace(mod.Sha256))
        {
            await using var input = File.OpenRead(temporary);
            var actual = Convert.ToHexString(await SHA256.HashDataAsync(input, token));
            if (!actual.Equals(mod.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(temporary);
                throw new InvalidDataException("Die Prüfsumme des Downloads stimmt nicht mit dem Serverdienst überein.");
            }
        }
        return temporary;
    }

    public async Task SetFavoriteAsync(PortalMod mod, bool favorite, CancellationToken token = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            $"mods/{Uri.EscapeDataString(mod.Id)}/favorite", new { favorite }, token);
        await EnsureSuccessAsync(response, token);
        mod.IsFavorite = favorite;
    }

    public async Task<IReadOnlyList<ManagedServer>> GetServersAsync(CancellationToken token = default)
    {
        using var response = await _httpClient.GetAsync("servers", token);
        await EnsureSuccessAsync(response, token);
        return await response.Content.ReadFromJsonAsync<List<ManagedServer>>(_json, token) ?? new List<ManagedServer>();
    }

    public async Task<DedicatedServerStatus> GetServerStatusAsync(ManagedServer server, CancellationToken token = default)
    {
        using var response = await _httpClient.GetAsync($"servers/{Uri.EscapeDataString(server.Id)}/status", token);
        await EnsureSuccessAsync(response, token);
        return await response.Content.ReadFromJsonAsync<DedicatedServerStatus>(_json, token)
            ?? throw new InvalidDataException("Ungültige Statusantwort des Portalservers.");
    }

    public async Task<IReadOnlyList<ServerMod>> GetServerModsAsync(ManagedServer server, CancellationToken token = default)
    {
        using var response = await _httpClient.GetAsync($"servers/{Uri.EscapeDataString(server.Id)}/mods", token);
        await EnsureSuccessAsync(response, token);
        return await response.Content.ReadFromJsonAsync<List<ServerMod>>(_json, token) ?? new List<ServerMod>();
    }

    public async Task<string> DownloadServerModAsync(ManagedServer server, ServerMod mod, CancellationToken token = default)
    {
        var path = $"servers/{Uri.EscapeDataString(server.Id)}/mods/{Uri.EscapeDataString(mod.Id)}/download";
        return await DownloadFileAsync(path, mod.FileName, mod.Sha256, token);
    }

    public async Task UploadServerModAsync(ManagedServer server, ModInfo mod, CancellationToken token = default)
    {
        await using var input = File.OpenRead(mod.FullPath);
        using var form = new MultipartFormDataContent();
        using var content = new StreamContent(input);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        form.Add(content, "mod", Path.GetFileName(mod.FileName));
        using var response = await _httpClient.PostAsync(
            $"servers/{Uri.EscapeDataString(server.Id)}/mods", form, token);
        await EnsureSuccessAsync(response, token);
    }

    public async Task RestartServerAsync(ManagedServer server, CancellationToken token = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            $"servers/{Uri.EscapeDataString(server.Id)}/restart", new { confirm = true }, token);
        await EnsureSuccessAsync(response, token);
    }

    private async Task<string> DownloadFileAsync(string path, string fileName, string sha256, CancellationToken token)
    {
        using var response = await _httpClient.GetAsync(path, HttpCompletionOption.ResponseHeadersRead, token);
        await EnsureSuccessAsync(response, token);
        var safeName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeName) || !safeName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Der Server hat keinen gültigen ZIP-Dateinamen geliefert.");
        var temporary = Path.Combine(Path.GetTempPath(), $"easycoop-{Guid.NewGuid():N}-{safeName}");
        await using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
            await response.Content.CopyToAsync(output, token);
        if (!string.IsNullOrWhiteSpace(sha256))
        {
            await using var input = File.OpenRead(temporary);
            var actual = Convert.ToHexString(await SHA256.HashDataAsync(input, token));
            if (!actual.Equals(sha256, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(temporary);
                throw new InvalidDataException("Die Prüfsumme der Server-Mod stimmt nicht überein.");
            }
        }
        return temporary;
    }

    private static Uri ValidateBaseUrl(string value)
    {
        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri))
            throw new ArgumentException("Bitte eine gültige Portal-API-Adresse eingeben.");
        if (uri.Scheme != Uri.UriSchemeHttps && !uri.IsLoopback)
            throw new ArgumentException("Die Portal-API muss HTTPS verwenden. HTTP ist nur für localhost-Tests erlaubt.");
        return new Uri(uri.ToString().TrimEnd('/') + "/");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken token)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync(token);
        string? message = null;
        try { message = JsonDocument.Parse(body).RootElement.GetProperty("message").GetString(); } catch { }
        throw new HttpRequestException(message ?? $"Portalserver-Fehler: {(int)response.StatusCode} {response.ReasonPhrase}");
    }

    public void Dispose() => _httpClient.Dispose();
}
