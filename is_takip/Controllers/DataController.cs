using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using is_takip.Data;
using is_takip.Models;

namespace is_takip.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DataController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DataController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/data/all
        // Bu endpoint, React uygulamasının başlangıçta ihtiyaç duyduğu tüm verileri
        // tek bir paket halinde ve React'in beklediği İNGİLİZCE isimlerle döndürür.
        [HttpGet("all")]
        public async Task<IActionResult> GetAllData()
        {
            var data = new
            {
                // React kodundaki "App.tsx" ve "mockData.ts" dosyalarındaki
                // isimlendirmelerle aynı isimleri kullanıyoruz.
                users = await _context.Kullanicilar.ToListAsync(),
                personnel = await _context.Personel.ToListAsync(),
                customers = await _context.Musteriler.ToListAsync(),
                customerJobs = await _context.MusteriIsleri.ToListAsync(),
                personnelPayments = await _context.PersonelOdemeleri.ToListAsync(),
                // TÜM GİDERLERİ GETİR (hem aktif hem silinmiş)
                sharedExpenses = await _context.OrtakGiderler.ToListAsync(),
                defterEntries = await _context.DefterKayitlari.ToListAsync(),
                defterNotes = await _context.DefterNotlari.ToListAsync(),
                workDays = await _context.PuantajKayitlari.ToListAsync(),
                // Diğer modeller de React tarafındaki isimlendirmeyle eşleşmeli
                materials = await _context.IsMalzemeleri.ToListAsync(),
                jobEarnings = await _context.IsHakedisleri.ToListAsync()
            };

            return Ok(data);
        }
    }
}