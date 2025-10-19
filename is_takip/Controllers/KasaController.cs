using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using is_takip.Data;
using is_takip.Models;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace is_takip.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KasaController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public KasaController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GMT+3 dönüştürücü (PersonelController ile aynı davranış)
        private static DateTime ToGmt3(DateTime dt)
        {
            var utc = dt.Kind == DateTimeKind.Utc
                ? dt
                : DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            return utc.AddHours(3);
        }

        // =================================================================                                
        // --- ORTAK GİDERLER (SharedExpenses) CRUD İŞLEMLERİ ---
        // =================================================================

        [HttpGet("ortakgiderler")]
        public async Task<ActionResult<IEnumerable<OrtakGiderler>>> GetOrtakGiderler()
        {
            var giderler = await _context.OrtakGiderler
                .Where(g => g.SilinmeTarihi == null)
                .OrderByDescending(g => g.Tarih)
                .ToListAsync();

            return Ok(giderler);
        }

        [HttpGet("ortakgiderler/{id}")]
        public async Task<ActionResult<OrtakGiderler>> GetOrtakGider(int id)
        {
            var gider = await _context.OrtakGiderler.FindAsync(id);

            if (gider == null || gider.SilinmeTarihi != null)
            {
                return NotFound();
            }

            return Ok(gider);
        }

        [HttpPost("ortakgiderler")]
        public async Task<ActionResult<OrtakGiderler>> CreateOrtakGider(OrtakGiderler gider)
        {
            try
            {
                // Validation kontrolü
                if (string.IsNullOrWhiteSpace(gider.Aciklama))
                {
                    return BadRequest("Açıklama alanı boş bırakılamaz.");
                }

                if (gider.Tutar <= 0)
                {
                    return BadRequest("Tutar pozitif bir değer olmalıdır.");
                }

                // PersonelController ile aynı davranış: sunucu zamanı (UTC -> GMT+3) kullan
                gider.Tarih = ToGmt3(DateTime.UtcNow);

                _context.OrtakGiderler.Add(gider);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetOrtakGider), new { id = gider.GiderId }, gider);
            }
            catch (Exception ex)
            {
                // Hata detayını sunucu loglarına yazdırmak için
                Console.WriteLine($"Gider ekleme hatası: {ex.InnerException?.Message ?? ex.Message}");
                return StatusCode(500, $"Gider eklenirken hata oluştu: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        [HttpPut("ortakgiderler/{id}")]
        public async Task<IActionResult> UpdateOrtakGider(int id, OrtakGiderler gelenGiderVerisi)
        {
            try
            {
                // Validation kontrolü
                if (string.IsNullOrWhiteSpace(gelenGiderVerisi.Aciklama))
                {
                    return BadRequest("Açıklama alanı boş bırakılamaz.");
                }

                if (gelenGiderVerisi.Tutar <= 0)
                {
                    return BadRequest("Tutar pozitif bir değer olmalıdır.");
                }

                var mevcutGider = await _context.OrtakGiderler.FindAsync(id);
                if (mevcutGider == null || mevcutGider.SilinmeTarihi != null)
                {
                    return NotFound();
                }

                // Güncelleme işlemi
                mevcutGider.Aciklama = gelenGiderVerisi.Aciklama;
                mevcutGider.Tutar = gelenGiderVerisi.Tutar;
                // PersonelController ile aynı mantık: güncellemede sunucu zamanını kullan
                mevcutGider.Tarih = ToGmt3(DateTime.UtcNow);
                mevcutGider.OdemeYontemi = gelenGiderVerisi.OdemeYontemi;
                mevcutGider.OdeyenKisi = gelenGiderVerisi.OdeyenKisi;
                mevcutGider.Durum = gelenGiderVerisi.Durum;

                await _context.SaveChangesAsync();

                // Güncellenmiş veriyi döndür
                return Ok(mevcutGider);
            }
            catch (Exception ex)
            {
                // Hata detayını sunucu loglarına yazdırmak için
                Console.WriteLine($"Gider güncelleme hatası: {ex.InnerException?.Message ?? ex.Message}");
                return StatusCode(500, $"Gider güncellenirken hata oluştu: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        [HttpPatch("ortakgiderler/{id}/status")]
        public async Task<IActionResult> ToggleOrtakGiderStatus(int id)
        {
            try
            {
                var gider = await _context.OrtakGiderler.FindAsync(id);
                if (gider == null || gider.SilinmeTarihi != null)
                {
                    return NotFound();
                }

                gider.Durum = (gider.Durum == OdemeDurumu.paid) ? OdemeDurumu.unpaid : OdemeDurumu.paid;
                await _context.SaveChangesAsync();

                return Ok(gider);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Gider durumu güncellenirken hata oluştu: {ex.Message}");
            }
        }

        [HttpDelete("ortakgiderler/{id}")]
        public async Task<IActionResult> DeleteOrtakGider(int id)
        {
            try
            {
                var gider = await _context.OrtakGiderler.FindAsync(id);
                if (gider == null || gider.SilinmeTarihi != null)
                {
                    return NotFound();
                }

                // Soft delete - silinme tarihini ayarla (PersonelController ile tutarlı GMT+3)
                gider.SilinmeTarihi = ToGmt3(DateTime.UtcNow);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Gider silinirken hata oluştu: {ex.Message}");
            }
        }

        [HttpPost("ortakgiderler/{id}/restore")]
        public async Task<IActionResult> RestoreOrtakGider(int id)
        {
            try
            {
                var gider = await _context.OrtakGiderler.FindAsync(id);
                if (gider == null)
                {
                    return NotFound();
                }

                gider.SilinmeTarihi = null;
                await _context.SaveChangesAsync();

                return Ok(gider);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Gider geri alınırken hata oluştu: {ex.Message}");
            }
        }

        [HttpDelete("ortakgiderler/{id}/permanent")]
        public async Task<IActionResult> PermanentlyDeleteOrtakGider(int id)
        {
            try
            {
                var gider = await _context.OrtakGiderler.FindAsync(id);
                if (gider == null)
                {
                    return NotFound();
                }

                // Hard delete - veritabanından tamamen sil
                _context.OrtakGiderler.Remove(gider);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Gider kalıcı olarak silinirken hata oluştu: {ex.Message}");
            }
        }

        [HttpGet("ortakgiderler/deleted")]
        public async Task<ActionResult<IEnumerable<OrtakGiderler>>> GetDeletedOrtakGiderler()
        {
            var silinenGiderler = await _context.OrtakGiderler
                .Where(g => g.SilinmeTarihi != null)
                .OrderByDescending(g => g.SilinmeTarihi)
                .ToListAsync();

            return Ok(silinenGiderler);
        }

        // =================================================================
        // --- DİĞER KASA İŞLEMLERİ ---
        // =================================================================
        // Buraya diğer kasa işlemlerini ekleyebilirsiniz
        // Örnek: Gelirler, genel giderler, kasa bakiyesi vb.
    }
}