using MassMessaging.MVC.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Session Servisini Ekle
builder.Services.AddControllersWithViews();
builder.Services.AddSession();

builder.Services.AddHttpClient<AdminService>(client =>
{
    client.BaseAddress = new Uri("https://localhost:7124/");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 2. Session Middleware'ini Ekle (UseRouting'den SONRA, MapControllerRoute'dan ÖNCE)
app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();