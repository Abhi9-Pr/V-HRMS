using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Vespera.Application.Abstractions.Services;

namespace Vespera.Infrastructure.Notifications.Push;

/// <summary>Sends one Android/Web push via FCM's HTTP v1 API. Authenticates as the service account
/// in <see cref="FcmOptions"/> via the standard Google OAuth2 JWT-bearer flow (RFC 7523): mint a
/// short-lived RS256-signed JWT asserting the service account's identity, exchange it at Google's
/// token endpoint for an access token, cache that token until shortly before it expires. No
/// Google client library — this is the entire flow in about thirty lines, not worth a dependency
/// for.</summary>
public sealed class FcmPushSender : IDisposable
{
    private static readonly Action<ILogger, string, Exception?> LogSendFailed = LoggerMessage.Define<string>(
        LogLevel.Warning, new EventId(1, nameof(LogSendFailed)), "FCM push send failed: {Reason}");

    private readonly HttpClient _httpClient;
    private readonly FcmOptions _options;
    private readonly ILogger<FcmPushSender> _logger;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    private string? _cachedAccessToken;
    private DateTimeOffset _cachedAccessTokenExpiresAt = DateTimeOffset.MinValue;

    public FcmPushSender(HttpClient httpClient, IOptions<FcmOptions> options, ILogger<FcmPushSender> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(PushMessage message, CancellationToken cancellationToken)
    {
        var accessToken = await GetAccessTokenAsync(cancellationToken);

        var payload = new
        {
            message = new
            {
                token = message.DeviceToken,
                notification = new { title = message.Title, body = message.Body },
                data = message.Data,
            },
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Post, $"https://fcm.googleapis.com/v1/projects/{_options.ProjectId}/messages:send")
        {
            Content = JsonContent.Create(payload),
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            LogSendFailed(_logger, $"{(int)response.StatusCode} {body}", null);
        }
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (_cachedAccessToken is not null && DateTimeOffset.UtcNow < _cachedAccessTokenExpiresAt)
        {
            return _cachedAccessToken;
        }

        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            if (_cachedAccessToken is not null && DateTimeOffset.UtcNow < _cachedAccessTokenExpiresAt)
            {
                return _cachedAccessToken;
            }

            var assertion = BuildAssertionJwt();

            using var response = await _httpClient.PostAsync(
                "https://oauth2.googleapis.com/token",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
                    ["assertion"] = assertion,
                }),
                cancellationToken);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var body = await JsonSerializer.DeserializeAsync<JsonElement>(stream, cancellationToken: cancellationToken);

            var accessToken = body.GetProperty("access_token").GetString()!;
            var expiresInSeconds = body.GetProperty("expires_in").GetInt32();

            _cachedAccessToken = accessToken;
            _cachedAccessTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds - 60);

            return accessToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    public void Dispose() => _tokenLock.Dispose();

    private string BuildAssertionJwt()
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(_options.PrivateKeyPem);
        var credentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256);

        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: _options.ClientEmail,
            audience: "https://oauth2.googleapis.com/token",
            claims: [new Claim("scope", "https://www.googleapis.com/auth/firebase.messaging")],
            notBefore: now,
            expires: now.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
