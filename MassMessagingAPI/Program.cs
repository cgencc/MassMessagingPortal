using MassMessagingAPI.Data; // Kendi namespace'inize göre düzeltin
using MassMessagingAPI.Models; // Kendi namespace'inize göre düzeltin
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Veritabaný Baðlantýsýný (SQL Server) Sisteme Ekle
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Identity (Üyelik Sistemi) Ayarlarý
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    // Test amaçlý þifre kurallarýný esnetiyoruz
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// 3. JWT (Token Bazlý Kimlik Doðrulama) Ayarlarý
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});

// 4. Controller ve Swagger Ayarlarý (Swagger üzerinden Token girebilmek için)
builder.Services.AddSignalR(); // SignalR servisini ekle
// Hocanýn AnketPortal projesindeki gibi Repository ve Service kayýtlarý
builder.Services.AddScoped(typeof(MassMessagingAPI.Repositories.IGenericRepository<>), typeof(MassMessagingAPI.Repositories.GenericRepository<>));
builder.Services.AddScoped<MassMessagingAPI.Services.ITokenService, MassMessagingAPI.Services.TokenService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Toplu Mesajlaþma API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Örnek kullaným: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// 5. CORS Ayarlarý (SignalR ve UI'ýn engellenmemesi için)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        b => b.AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials() // SignalR için þarttýr
              .SetIsOriginAllowed(origin => true));
});

var app = builder.Build();

// HTTP Request Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// MÝDDLEWARE SIRALAMASI KRÝTÝKTÝR!
app.UseCors("AllowAll");
app.UseAuthentication(); // 1. Sen kimsin? (Auth)
app.UseAuthorization();  // 2. Yetkin var mý? (Role)

app.MapControllers();
app.MapHub<MassMessagingAPI.Hubs.ChatHub>("/chathub"); // Hub'ýn dýþarýya açýlacaðý adres

// Uygulama baþlarken Rolleri ve Kurucu Admin'i otomatik oluþturur
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await MassMessagingAPI.Data.SeedData.InitializeAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Veritabaný baþlangýç verileri yüklenirken hata oluþtu.");
    }
}

app.Run();