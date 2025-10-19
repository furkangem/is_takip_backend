using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using is_takip.Data;
using is_takip.Models;

namespace is_takip.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PersonelController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PersonelController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GMT+3 dönüştürücü (Türkiye sabit GMT+3)
        private static DateTime ToGmt3(DateTime dt)
        {
            var utc = dt.Kind == DateTimeKind.Utc
                ? dt
                : DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            return utc.AddHours(3);
        }
        // --- TÜM PERSONELLERİ GETİRMEK İÇİN EKLENECEK KOD ---
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Personel>>> GetAllPersonel()
        {
            var personeller = await _context.Personel.ToListAsync();
            return Ok(personeller);
        }
        // --- PERSONEL CRUD İŞLEMLERİ ---
        [HttpPost]
        public async Task<ActionResult<Personel>> CreatePersonel([FromBody] Personel personel)
        {
            // Model validation kontrolü
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Manuel kontrol
            if (string.IsNullOrWhiteSpace(personel.AdSoyad))
                return BadRequest("Ad Soyad zorunludur.");

            // Not güncelleme tarihini ayarla
            personel.NotGuncellenmeTarihi = personel.NotMetni != null
                ? ToGmt3(DateTime.UtcNow)
                : null;

            _context.Personel.Add(personel);
            await _context.SaveChangesAsync();

            // Debug için
            Console.WriteLine($"Personel eklendi: {personel.AdSoyad}, ID: {personel.PersonelId}");

            return Ok(personel);
        }

        // --- GÜNCELLENMİŞ VE DOĞRU HALİ ---
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePersonel(int id, [FromBody] Personel gelen)
        {
            var mevcut = await _context.Personel.FirstOrDefaultAsync(p => p.PersonelId == id);
            if (mevcut == null) return NotFound();

            // Sadece gelen alanları güncelle
            if (!string.IsNullOrWhiteSpace(gelen.AdSoyad))
                mevcut.AdSoyad = gelen.AdSoyad;

            mevcut.NotMetni = gelen.NotMetni; // null olabilir, bilinçli seçim

            // Not güncelleme tarihini ayarla
            mevcut.NotGuncellenmeTarihi = gelen.NotMetni != null
                ? ToGmt3(DateTime.UtcNow)
                : null;

            // Kritik: Değiştirildi olarak işaretle
            _context.Entry(mevcut).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();

                // Debug için
                Console.WriteLine($"Personel güncellendi: {mevcut.AdSoyad}, ID: {mevcut.PersonelId}");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Personel.Any(p => p.PersonelId == id))
                    return NotFound();
                throw;
            }

            // UI hemen güncellesin
            return Ok(mevcut);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePersonel(int id)
        {
            var personel = await _context.Personel.FindAsync(id);
            if (personel == null) return NotFound();

            // Debug için
            Console.WriteLine($"Personel siliniyor: {personel.AdSoyad}, ID: {personel.PersonelId}");

            _context.Personel.Remove(personel);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // --- PERSONEL ÖDEME İŞLEMLERİ ---
        [HttpPost("odemeler")]
        public async Task<ActionResult<PersonelOdemeleri>> CreatePersonelOdemesi([FromBody] PersonelOdemeleri odeme)
        {
            // Debug için
            Console.WriteLine($"Personel ödemesi ekleniyor: {odeme.PersonelId}, Tutar: {odeme.Tutar}");

            // Set payment date same way as other timestamps (GMT+3)
            odeme.Tarih = ToGmt3(DateTime.UtcNow);

            _context.PersonelOdemeleri.Add(odeme);
            await _context.SaveChangesAsync();
            return Ok(odeme);
        }

        [HttpDelete("odemeler/{id}")]
        public async Task<IActionResult> DeletePersonelOdemesi(int id)
        {
            var odeme = await _context.PersonelOdemeleri.FindAsync(id);
            if (odeme == null) return NotFound();

            // Debug için
            Console.WriteLine($"Personel ödemesi siliniyor: ID: {id}");

            _context.PersonelOdemeleri.Remove(odeme);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}