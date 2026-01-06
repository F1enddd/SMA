using SteamAuth;
using System.Net.Http.Json;
using System.Text.Json;

public static class SteamWebAuth
{
    private static readonly HttpClient http = new HttpClient();

    public class BeginAuthResponse
    {
        public Response response { get; set; }
        public class Response
        {
            public string client_id { get; set; }
            public string request_id { get; set; }
            public string interval { get; set; }
        }
    }

    public class PollAuthResponse
    {
        public Response response { get; set; }
        public class Response
        {
            public string status { get; set; }
            public string access_token { get; set; }
            public string refresh_token { get; set; }
            public ulong steamid { get; set; }
        }
    }

    public static async Task<BeginAuthResponse> BeginAuth(string username, string password, SteamGuardAccount account)
    {
        string deviceCode = null;

        if (account != null)
        {
            deviceCode = await account.GenerateSteamGuardCodeAsync();
        }

        var data = new
        {
            username = username,
            password = password,
            two_factor_code = deviceCode,
            platform_type = 1,            // Steam Client
            device_friendly_name = "Windows",
            device_os_type = 14           // Windows 10
        };

        var resp = await http.PostAsJsonAsync(
            "https://api.steampowered.com/IAuthenticationService/BeginAuthSessionViaCredentials/v1/",
            data
        );

        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<BeginAuthResponse>();
    }


    public static async Task<PollAuthResponse?> PollAuth(string clientId, string requestId)
    {
        var data = new
        {
            client_id = clientId,
            request_id = requestId
        };

        var resp = await http.PostAsJsonAsync(
            "https://api.steampowered.com/IAuthenticationService/PollAuthSessionStatus/v1/",
            data
        );

        string raw = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
        {
            // Возвращаем null, чтобы код выше понял, что Steam требует доп. действия
            return new PollAuthResponse
            {
                response = new PollAuthResponse.Response
                {
                    status = "ERROR",
                }
            };
        }

        return JsonSerializer.Deserialize<PollAuthResponse>(raw);
    }

}
