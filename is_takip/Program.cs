// using satýrlarý
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using System.Text.Json;
using is_takip.Data;

var builder = WebApplication.CreateBuilder(args);

// ===================================
// BÖLÜM 1: SERVÝS TANIMLAMALARI
// ===================================

// 1) CORS: Canlý Vercel adresini ve yerel geliþtirme adreslerini ekle
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy
            .WithOrigins(
                "https://is-takip-theta.vercel.app", // Yayýndaki frontend
                "http://localhost:5173",            // Local Vite portu
                "http://localhost:3000"             // Local CRA portu
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
        // .AllowCredentials(); // Eðer çerez kullanýyorsanýz bu satýrý açýn
    });
});

// 2) DbContext: Baðlantý hatasý durumunda yeniden deneme (NEONDB için önemli)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 10,
            maxRetryDelay: TimeSpan.FromSeconds(60),
            errorCodesToAdd: null);
    }));

// 3) Controllerlar ve JSON ayarlarý (Döngüsel referanslarý çözmek için)
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

// 4) Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ===================================
// BÖLÜM 2: MIDDLEWARE AKIÞI
// ===================================

// 5) Swagger sadece Development'ta
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 6) Health check endpoints
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow
}));

app.MapGet("/wake-db", async (ApplicationDbContext db) =>
{
    // KOD GÜNCELLEMESÝNÝ KONTROL ETMEK ÝÇÝN VERSÝYON ÝÞARETLEYÝCÝ
    var version = "v2.2_CORS_FIX_APPLIED";
    try
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(25));
        var canConnect = await db.Database.CanConnectAsync(cts.Token);
        return Results.Ok(new
        {
            version,
            database = canConnect ? "connected" : "disconnected",
            timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        return Results.Ok(new
        {
            version,
            database = "error",
            message = ex.Message,
            timestamp = DateTime.UtcNow
        });
    }
});

// 7) HTTPS yönlendirme
app.UseHttpsRedirection();

// 8) KRÝTÝK ADIM: Yönlendirmeyi etkinleþtir
app.UseRouting();

// 9) KRÝTÝK ADIM: CORS politikasýný uygulamadan hemen sonra çaðýr!
app.UseCors("AllowReactApp");

// 10) Yetkilendirme
app.UseAuthorization();

// 11) Controller'larý eþle
app.MapControllers();

// 12) Uygulamayý çalýþtýr
app.Run();