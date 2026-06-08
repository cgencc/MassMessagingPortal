using MassMessaging.MVC.Services; // AdminService'in bulunduðu namespace

var builder = WebApplication.CreateBuilder(args);

// 1. Servisleri ekle
builder.Services.AddControllersWithViews();

// 2. Session servisini aktif et (Session hatasý için)
builder.Services.AddSession();

// 3. AdminService'i DI container'a kaydet (InvalidOperationException hatasý için)
builder.Services.AddHttpClient<AdminService>(client =>
{
    client.BaseAddress = new Uri("https://localhost:7124/"); // API adresin
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 4. Session'ý kullanýma aç (UseRouting ile UseAuthorization arasýnda olmalý)
app.UseSession();

app.UseAuthentication(); // Varsa
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();