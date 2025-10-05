// using sat�rlar�
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using System.Text.Json;
using is_takip.Data;

var builder = WebApplication.CreateBuilder(args);

// ===================================
// B�L�M 1: SERV�S TANIMLAMALARI
// ===================================

// 1) CORS: TANI�MAYAN CORS HATASINI KES�NLE�T�RMEK ���N GE��C� OLARAK HER �EYE �Z�N VER
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy
            .AllowAnyOrigin() // <--- T�M KAYNAKLARA GE��C� �Z�N
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// 2) DbContext: Ba�lant� hatas� durumunda yeniden deneme (NEONDB i�in �nemli)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 10,
            maxRetryDelay: TimeSpan.FromSeconds(60),
            errorCodesToAdd: null);
    }));

// 3) Controllerlar ve JSON ayarlar� (D�ng�sel referanslar� ��zmek i�in)
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
// B�L�M 2: MIDDLEWARE AKI�I
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
    // KOD G�NCELLEMES�N� KONTROL ETMEK ���N YEN� VERS�YON ��ARETLEY�C�
    var version = "v2.3_CORS_TEST_ANYORIGIN";
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

// 7) HTTPS y�nlendirme
app.UseHttpsRedirection();

// 8) KR�T�K ADIM: Y�nlendirmeyi etkinle�tir
app.UseRouting();

// 9) KR�T�K ADIM: CORS politikas�n� uygulamadan hemen sonra �a��r!
app.UseCors("AllowReactApp");

// 10) Yetkilendirme
app.UseAuthorization();

// 11) Controller'lar� e�le
app.MapControllers();

// 12) Uygulamay� �al��t�r
app.Run();