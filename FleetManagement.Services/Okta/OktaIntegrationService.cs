using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FleetManagement.Services.Abstractions;
using FleetManagement.Services.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FleetManagement.Services.Okta;

public class OktaIntegrationService : IOktaIntegrationService
{
    private readonly OktaSettings _settings;
    private readonly ILogger<OktaIntegrationService> _logger;
    private readonly HttpClient _httpClient;

    public OktaIntegrationService(HttpClient httpClient, IOptions<OktaSettings> options, ILogger<OktaIntegrationService> logger)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task<string?> ProvisionUserAsync(string email, string password, string firstName, string lastName, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured())
        {
            return null;
        }

        var payload = new
        {
            profile = new
            {
                firstName,
                lastName,
                email,
                login = email
            },
            credentials = new
            {
                password = new { value = password }
            }
        };

        var response = await SendAsync(HttpMethod.Post, "/api/v1/users?activate=true", payload, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Unable to provision Okta user {Email}. Status {StatusCode}", email, response.StatusCode);
            return null;
        }

        var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var id = json.RootElement.GetProperty("id").GetString();
        _logger.LogInformation("Provisioned Okta user {OktaUserId}", id);
        return id;
    }

    public async Task<string?> EnsureOwnerGroupAsync(string companyName, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured())
        {
            return null;
        }

        var normalizedName = $"fleet-{companyName.Trim().ToLowerInvariant()}";

        var searchResponse = await SendAsync(HttpMethod.Get, $"/api/v1/groups?q={normalizedName}&limit=1", null, cancellationToken);
        if (searchResponse.IsSuccessStatusCode)
        {
            var json = await JsonDocument.ParseAsync(await searchResponse.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            if (json.RootElement.ValueKind == JsonValueKind.Array && json.RootElement.GetArrayLength() > 0)
            {
                return json.RootElement[0].GetProperty("id").GetString();
            }
        }

        var payload = new
        {
            profile = new
            {
                name = normalizedName,
                description = $"Fleet owner group for {companyName}"
            }
        };

        var createResponse = await SendAsync(HttpMethod.Post, "/api/v1/groups", payload, cancellationToken);
        if (!createResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to create Okta group for {Company}", companyName);
            return null;
        }

        var created = await JsonDocument.ParseAsync(await createResponse.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        return created.RootElement.GetProperty("id").GetString();
    }

    private bool IsConfigured()
    {
        if (string.IsNullOrWhiteSpace(_settings.Domain) || string.IsNullOrWhiteSpace(_settings.ApiToken))
        {
            _logger.LogWarning("Okta configuration missing. Skipping Okta operations.");
            return false;
        }

        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = new Uri(_settings.Domain);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("SSWS", _settings.ApiToken);
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        return true;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? payload, CancellationToken cancellationToken)
    {
        if (!IsConfigured())
        {
            return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
        }

        var request = new HttpRequestMessage(method, path);
        if (payload != null)
        {
            var json = JsonSerializer.Serialize(payload);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return await _httpClient.SendAsync(request, cancellationToken);
    }
}

