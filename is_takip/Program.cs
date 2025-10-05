// using satýrlarý
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using System.Text.Json; // ReferenceHandler için
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
                "http://localhost:5173",           // Vite (yerel geliþtirme)
                "http://localhost:3000"            // CRA (yerel geliþtirme)
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// 2) DbContext: Baðlantý hatasý durumunda yeniden deneme (RETRY) mekanizmasý eklendi
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        // Bu kýsým, veritabaný uyandýðýnda oluþabilecek geçici baðlantý hatalarýný yönetir.
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);
    }));

// 3) Controllerlar ve JSON ayarlarý
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Enum'lar string olarak serileþtirilsin (TRY, GOLD, cash, transfer gibi)
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

        // Ýliþkili tablolarda döngüleri kýr (Müþteri <-> Ýþler vs.)
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

// 4) Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 5) Swagger sadece Development’ta
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 6) HTTPS yönlendirme (gerekli ise)
app.UseHttpsRedirection();

// 7) CORS
app.UseCors("AllowReactApp");

// 8) (Varsa) Yetkilendirme
app.UseAuthorization();

// 9) Controller’larý eþle
app.MapControllers();

// 10) Uygulamayý çalýþtýr
app.Run();