// using sat�rlar�
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using System.Text.Json; // ReferenceHandler i�in
using is_takip.Data;

var builder = WebApplication.CreateBuilder(args);

// 1) CORS: React (Vite) ve CRA i�in izin ver
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173", // Vite
                "http://localhost:3000"  // CRA
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// 2) DbContext: appsettings.json�daki DefaultConnection�� kullan
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// 3) Controllerlar ve JSON ayarlar�
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Enum'lar string olarak serile�tirilsin (TRY, GOLD, cash, transfer gibi)
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

        // �li�kili tablolarda d�ng�leri k�r (M��teri <-> ��ler vs.)
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;

        // �ste�e ba�l�: tarih formatlar�n� standartla�t�rmak istersen
        // options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

// 4) Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 5) Swagger sadece Development�ta
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 6) HTTPS y�nlendirme (gerekli ise)
app.UseHttpsRedirection();

// 7) CORS
app.UseCors("AllowReactApp");

// 8) (Varsa) Yetkilendirme
app.UseAuthorization();

// 9) Controller�lar� e�le
app.MapControllers();

// 10) Uygulamay� �al��t�r
app.Run();