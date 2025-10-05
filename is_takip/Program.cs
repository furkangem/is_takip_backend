// using sat�rlar�
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using System.Text.Json;
using is_takip.Data;

var builder = WebApplication.CreateBuilder(args);

// 1) CORS: Canl� Vercel adresini ve yerel geli�tirme adreslerini ekle
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

// 2) DbContext: Ba�lant� hatas� durumunda yeniden deneme (RETRY) mekanizmas�
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 10,
            maxRetryDelay: TimeSpan.FromSeconds(60),
            errorCodesToAdd: null);
    }));

// 3) Controllerlar ve JSON ayarlar�
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

// 6) Health check endpoints (Versiyon ��aretleyici Eklendi)
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow
}));

app.MapGet("/wake-db", async (ApplicationDbContext db) =>
{
    // BU B�R TESTT�R: RENDER'IN G�NCEL KODU �ALI�TIRIP �ALI�TIRMADI�INI ANLAMAK ���N
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

// 7) HTTPS y�nlendirme
app.UseHttpsRedirection();

// 8) CORS
app.UseCors("AllowReactApp");

// 9) Yetkilendirme
app.UseAuthorization();

// 10) Controller'lar� e�le
app.MapControllers();

// 11) Uygulamay� �al��t�r
app.Run();