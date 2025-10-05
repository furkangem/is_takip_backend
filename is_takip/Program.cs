// using satýrlarý
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using System.Text.Json;
using is_takip.Data;

var builder = WebApplication.CreateBuilder(args);

// 1) CORS: Canlý Vercel adresini ve yerel geliþtirme adreslerini ekle
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy
            .WithOrigins(
                "https://is-takip-theta.vercel.app",
                "http://localhost:5173",
                "http://localhost:3000"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// 2) DbContext: Baðlantý hatasý durumunda yeniden deneme (RETRY) mekanizmasý
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 10,
            maxRetryDelay: TimeSpan.FromSeconds(60),
            errorCodesToAdd: null);
    }));

// 3) Controllerlar ve JSON ayarlarý
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

// 5) Swagger sadece Development'ta
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 6) Health check endpoints (Versiyon Ýþaretleyici Eklendi)
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow
}));

app.MapGet("/wake-db", async (ApplicationDbContext db) =>
{
    // BU BÝR TESTTÝR: RENDER'IN GÜNCEL KODU ÇALIÞTIRIP ÇALIÞTIRMADIÐINI ANLAMAK ÝÇÝN
    var version = "v2.1_FINAL_CORS_CHECK";
    try
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(25));
        var canConnect = await db.Database.CanConnectAsync(cts.Token);
        return Results.Ok(new
        {
            version, // Cevaba versiyonu ekliyoruz
            database = canConnect ? "connected" : "disconnected",
            timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        return Results.Ok(new
        {
            version, // Cevaba versiyonu ekliyoruz
            database = "error",
            message = ex.Message,
            timestamp = DateTime.UtcNow
        });
    }
});

// 7) HTTPS yönlendirme
app.UseHttpsRedirection();

// 8) CORS
app.UseCors("AllowReactApp");

// 9) Yetkilendirme
app.UseAuthorization();

// 10) Controller'larý eþle
app.MapControllers();

// 11) Uygulamayý çalýþtýr
app.Run();