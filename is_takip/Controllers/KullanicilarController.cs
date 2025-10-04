// Gerekli kütüphaneleri ve diğer namespace'leri içeri aktarır
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using is_takip.Data;
using is_takip.Models;

// Bu controller'ın "is_takip.Controllers" isim alanına ait olduğunu belirtir
namespace is_takip.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KullanicilarController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public KullanicilarController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("login")]
        public async Task<ActionResult<Kullanici>> Login(LoginRequest loginRequest)
        {
            var kullanici = await _context.Kullanicilar
                .FirstOrDefaultAsync(k => k.KullaniciAdi == loginRequest.KullaniciAdi && k.Sifre == loginRequest.Sifre);

            if (kullanici == null)
            {
                return Unauthorized(new { message = "Geçersiz kullanıcı adı veya şifre." });
            }

            return Ok(kullanici);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Kullanici>>> GetKullanicilar()
        {
            return await _context.Kullanicilar.ToListAsync();
        }
    }

    public class LoginRequest
    {
        public string KullaniciAdi { get; set; } = string.Empty;
        public string Sifre { get; set; } = string.Empty;
    }
}