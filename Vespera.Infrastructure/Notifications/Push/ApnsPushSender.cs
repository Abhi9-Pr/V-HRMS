using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Vespera.Application.Abstractions.Services;

namespace Vespera.Infrastructure.Notifications.Push;

/// <summary>Sends one iOS push via APNs' HTTP/2 provider API, authenticating with a per-request
/// ES256 JWT built from the <see cref="ApnsOptions"/> p8 key (token-based auth — no long-lived TLS
/// client certificate to rotate). The JWT is cheap to mint locally (no network round trip, unlike
/// FCM's OAuth2 exchange) but Apple asks that it not be regenerated more than once every ~20
/// minutes, so it's cached the same way <see cref="FcmPushSender"/> caches its access token.</summary>
public sealed class ApnsPushSender : IDisposable
{
    private static readonly Action<ILogger, string, Exception?> LogSendFailed = LoggerMessage.Define<string>(
        LogLevel.Warning, new EventId(1, nameof(LogSendFailed)), "APNs push send failed: {Reason}");

    private readonly HttpClient _httpClient;
    private readonly ApnsOptions _options;
    private readonly ILogger<ApnsPushSender> _logger;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    private string? _cachedJwt;
    private DateTimeOffset _cachedJwtExpiresAt = DateTimeOffset.MinValue;

    public ApnsPushSender(HttpClient httpClient, IOptions<ApnsOptions> options, ILogger<ApnsPushSender> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(PushMessage message, CancellationToken cancellationToken)
    {
        var host = _options.UseSandbox ? "api.sandbox.push.apple.com" : "api.push.apple.com";

        var payload = message.Data.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);
        payload["aps"] = new { alert = new { title = message.Title, body = message.Body }, sound = "default" };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://{host}/3/device/{message.DeviceToken}")
        {
            Version = HttpVersion.Version20,
            VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
            Content = JsonContent.Create(payload),
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", GetJwt());
        request.Headers.Add("apns-topic", _options.BundleId);
        request.Headers.Add("apns-push-type", "alert");
        request.Headers.Add("apns-priority", "10");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            LogSendFailed(_logger, $"{(int)response.StatusCode} {body}", null);
        }
    }

    public void Dispose() => _tokenLock.Dispose();

    private string GetJwt()
    {
        if (_cachedJwt is not null && DateTimeOffset.UtcNow < _cachedJwtExpiresAt)
        {
            return _cachedJwt;
        }

        _tokenLock.Wait();
        try
        {
            if (_cachedJwt is not null && DateTimeOffset.UtcNow < _cachedJwtExpiresAt)
            {
                return _cachedJwt;
            }

            using var ecdsa = ECDsa.Create();
            ecdsa.ImportFromPem(_options.PrivateKeyPem);
            var credentials = new SigningCredentials(new ECDsaSecurityKey(ecdsa) { KeyId = _options.KeyId }, SecurityAlgorithms.EcdsaSha256);

            var now = DateTime.UtcNow;
            var token = new JwtSecurityToken(
                issuer: _options.TeamId,
                claims: null,
                notBefore: now,
                expires: now.AddMinutes(55),
                signingCredentials: credentials);

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            _cachedJwt = jwt;
            _cachedJwtExpiresAt = now.AddMinutes(50);

            return jwt;
        }
        finally
        {
            _tokenLock.Release();
        }
    }
}
