// MassMessaging.MVC/Services/AdminService.cs
using System.Net.Http.Headers;
using System.Net.Http.Json;
namespace MassMessaging.MVC.Services { 
public class AdminService
{
    private readonly HttpClient _httpClient;

    public AdminService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // Token'ı her isteğe ekleyen yardımcı metot
    private void SetToken(string token)
    {   
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<List<object>> GetAllUsersAsync(string token)
    {
        SetToken(token);
        return await _httpClient.GetFromJsonAsync<List<object>>("api/Admin/users");
    }

    public async Task<HttpResponseMessage> AssignRoleAsync(string token, string userId, string role)
    {
        SetToken(token);
        return await _httpClient.PostAsync($"api/Admin/assign-role/{userId}/{role}", null);
    }
}

}