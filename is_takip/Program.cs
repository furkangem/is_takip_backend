// using sat�rlar�
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using System.Text.Json; // ReferenceHandler i�in
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
                "http://localhost:5173",           // Vite (yerel geli�tirme)
                "http://localhost:3000"            // CRA (yerel geli�tirme)
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// 2) DbContext: Ba�lant� hatas� durumunda yeniden deneme (RETRY) mekanizmas� eklendi
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        // Bu k�s�m, veritaban� uyand���nda olu�abilecek ge�ici ba�lant� hatalar�n� y�netir.
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);
    }));

// 3) Controllerlar ve JSON ayarlar�
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Enum'lar string olarak serile�tirilsin (TRY, GOLD, cash, transfer gibi)
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

        // �li�kili tablolarda d�ng�leri k�r (M��teri <-> ��ler vs.)
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
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