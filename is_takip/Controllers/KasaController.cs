using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using is_takip.Data;
using is_takip.Models;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

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

        // Güvenli olarak DB'ye yazmadan önce DateTime'ı UTC'ye dönüştürür.
        // - Utc -> olduğu gibi döner
        // - Local -> ToUniversalTime()
        // - Unspecified:
        //    * eğer sadece tarih (time = 00:00) ise seçilen günü korumak için UTC olarak işaretler (preserve date)
        //    * aksi halde istemcinin local zamanı varsayılarak UTC'ye çevirir.
        private static DateTime EnsureUtcForWrite(DateTime dt)
        {
            if (dt == default) return DateTime.UtcNow;

            if (dt.Kind == DateTimeKind.Utc) return dt;
            if (dt.Kind == DateTimeKind.Local) return dt.ToUniversalTime();

            // Unspecified
            if (dt.TimeOfDay == TimeSpan.Zero)
            {
                // Date-only from client (e.g. "2025-10-19") — preserve selected day by treating as UTC date
                return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            }

            // Otherwise treat unspecified as client-local and convert to UTC
            return DateTime.SpecifyKind(dt, DateTimeKind.Local).ToUniversalTime();
        }

        private static DateTime? EnsureUtcForWrite(DateTime? dt)
        {
            if (!dt.HasValue) return null;
            return EnsureUtcForWrite(dt.Value);
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

                // Frontend'den gelen tarihi kullanıyoruz
                _context.OrtakGiderler.Add(gider);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetOrtakGider), new { id = gider.GiderId }, gider);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Gider ekleme hatası: {ex.InnerException?.Message ?? ex.Message}");
                return StatusCode(500, $"Gider eklenirken hata oluştu: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        [HttpPut("ortakgiderler/{id}")]
        public async Task<IActionResult> UpdateOrtakGider(int id, OrtakGiderler gelenGiderVerisi)
        {
            try
            {
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

                mevcutGider.Aciklama = gelenGiderVerisi.Aciklama;
                mevcutGider.Tutar = gelenGiderVerisi.Tutar;
                mevcutGider.Tarih = gelenGiderVerisi.Tarih; // Frontend'den gelen tarih
                mevcutGider.OdemeYontemi = gelenGiderVerisi.OdemeYontemi;
                mevcutGider.OdeyenKisi = gelenGiderVerisi.OdeyenKisi;
                mevcutGider.Durum = gelenGiderVerisi.Durum;

                await _context.SaveChangesAsync();

                return Ok(mevcutGider);
            }
            catch (Exception ex)
            {
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

                // Soft delete - silinme tarihini ayarla (UTC olarak)
                gider.SilinmeTarihi = DateTime.UtcNow;
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
        // --- DEFTER KAYITLARI (DefterEntry) CRUD İŞLEMLERİ ---
        // =================================================================

        [HttpGet("defterkayitlari")]
        public async Task<ActionResult<IEnumerable<DefterKayitlari>>> GetDefterKayitlari()
        {
            try
            {
                var kayitlar = await _context.DefterKayitlari
                    .OrderByDescending(k => k.IslemTarihi)
                    .ToListAsync();
                Console.WriteLine($"📋 {kayitlar.Count} defter kaydı listelendi");
                return Ok(kayitlar);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Defter kayıtları listeleme hatası: {ex.Message}");
                return StatusCode(500, $"Defter kayıtları listelenirken hata oluştu: {ex.Message}");
            }
        }

        [HttpPost("defterkayitlari")]
        public async Task<ActionResult<DefterKayitlari>> CreateDefterKaydi([FromBody] DefterKayitlari kayit)
        {
            try
            {
                Console.WriteLine($"📥 Yeni defter kaydı ekleme isteği: {kayit.Aciklama}");

                // Gelen tarih alanlarını DB yazımı için UTC'ye normalize et
                kayit.IslemTarihi = EnsureUtcForWrite(kayit.IslemTarihi);
                kayit.VadeTarihi = EnsureUtcForWrite(kayit.VadeTarihi);
                kayit.OdenmeTarihi = EnsureUtcForWrite(kayit.OdenmeTarihi);

                _context.DefterKayitlari.Add(kayit);
                await _context.SaveChangesAsync();
                Console.WriteLine($"✅ Defter kaydı eklendi: Id={kayit.KayitId}");
                return Ok(kayit);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Defter kaydı ekleme hatası: {ex.InnerException?.Message ?? ex.Message}");
                return StatusCode(500, $"Defter kaydı eklenirken hata oluştu: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        [HttpPut("defterkayitlari/{id}")]
        public async Task<IActionResult> UpdateDefterKaydi(int id, [FromBody] DefterKayitlari gelenKayit)
        {
            if (id != gelenKayit.KayitId)
            {
                return BadRequest("ID uyuşmazlığı");
            }

            try
            {
                Console.WriteLine($"📥 Defter kaydı güncelleme: Id={id}");
                var mevcutKayit = await _context.DefterKayitlari.FindAsync(id);
                if (mevcutKayit == null)
                {
                    Console.WriteLine($"⚠️ Güncellenecek defter kaydı bulunamadı: Id={id}");
                    return NotFound();
                }

                // Normalize ve güncelle (DB'ye yazmadan önce UTC yap)
                mevcutKayit.IslemTarihi = EnsureUtcForWrite(gelenKayit.IslemTarihi);
                mevcutKayit.Aciklama = gelenKayit.Aciklama;
                mevcutKayit.Tutar = gelenKayit.Tutar;
                mevcutKayit.Tip = gelenKayit.Tip;
                mevcutKayit.Durum = gelenKayit.Durum;
                mevcutKayit.VadeTarihi = EnsureUtcForWrite(gelenKayit.VadeTarihi);
                mevcutKayit.OdenmeTarihi = EnsureUtcForWrite(gelenKayit.OdenmeTarihi);
                mevcutKayit.Notlar = gelenKayit.Notlar;

                await _context.SaveChangesAsync();
                Console.WriteLine($"✅ Defter kaydı güncellendi: Id={id}");
                return Ok(mevcutKayit);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Defter kaydı güncelleme hatası: {ex.InnerException?.Message ?? ex.Message}");
                return StatusCode(500, $"Defter kaydı güncellenirken hata oluştu: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        [HttpDelete("defterkayitlari/{id}")]
        public async Task<IActionResult> DeleteDefterKaydi(int id)
        {
            try
            {
                Console.WriteLine($"📥 Defter kaydı silme: Id={id}");
                var kayit = await _context.DefterKayitlari.FindAsync(id);
                if (kayit == null)
                {
                    Console.WriteLine($"⚠️ Silinecek defter kaydı bulunamadı: Id={id}");
                    return NotFound();
                }

                _context.DefterKayitlari.Remove(kayit);
                await _context.SaveChangesAsync();
                Console.WriteLine($"✅ Defter kaydı silindi: Id={id}");
                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Defter kaydı silme hatası: {ex.InnerException?.Message ?? ex.Message}");
                return StatusCode(500, $"Defter kaydı silinirken hata oluştu: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        // =================================================================
        // --- DEFTER NOTLARI (mevcut kodunuz) ---
        // =================================================================
        [HttpGet("defternotlari")]
        public async Task<ActionResult<IEnumerable<DefterNotlari>>> GetDefterNotlari()
        {
            try
            {
                var notlar = await _context.DefterNotlari
                    .OrderByDescending(n => n.OlusturmaTarihi)
                    .ToListAsync();
                Console.WriteLine($"📋 {notlar.Count} defter notu listelendi");
                return Ok(notlar);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Defter notları listeleme hatası: {ex.Message}");
                return StatusCode(500, $"Defter notları listelenirken hata oluştu: {ex.Message}");
            }
        }

        [HttpPost("defternotlari")]
        public async Task<ActionResult<DefterNotlari>> CreateDefterNotu([FromBody] DefterNotlari not)
        {
            try
            {
                Console.WriteLine($"📥 Yeni defter notu ekleme isteği: {not.Baslik}");

                not.OlusturmaTarihi = ToGmt3(DateTime.UtcNow);
                not.TamamlandiMi = false;

                _context.DefterNotlari.Add(not);
                await _context.SaveChangesAsync();
                Console.WriteLine($"✅ Defter notu eklendi: Id={not.NotId}");
                return Ok(not);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Defter notu ekleme hatası: {ex.InnerException?.Message ?? ex.Message}");
                return StatusCode(500, $"Defter notu eklenirken hata oluştu: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        [HttpPut("defternotlari/{id}")]
        public async Task<IActionResult> UpdateDefterNotu(int id, [FromBody] DefterNotlari gelenNot)
        {
            if (id != gelenNot.NotId)
            {
                return BadRequest("ID uyuşmazlığı");
            }

            try
            {
                Console.WriteLine($"📥 Defter notu güncelleme: Id={id}");
                var mevcutNot = await _context.DefterNotlari.FindAsync(id);
                if (mevcutNot == null)
                {
                    Console.WriteLine($"⚠️ Güncellenecek defter notu bulunamadı: Id={id}");
                    return NotFound();
                }

                mevcutNot.Baslik = gelenNot.Baslik;
                mevcutNot.Aciklama = gelenNot.Aciklama;
                mevcutNot.Kategori = gelenNot.Kategori;
                mevcutNot.VadeTarihi = gelenNot.VadeTarihi;
                mevcutNot.TamamlandiMi = gelenNot.TamamlandiMi;

                await _context.SaveChangesAsync();
                Console.WriteLine($"✅ Defter notu güncellendi: Id={id}");
                return Ok(mevcutNot);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Defter notu güncelleme hatası: {ex.InnerException?.Message ?? ex.Message}");
                return StatusCode(500, $"Defter notu güncellenirken hata oluştu: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        [HttpDelete("defternotlari/{id}")]
        public async Task<IActionResult> DeleteDefterNotu(int id)
        {
            try
            {
                Console.WriteLine($"📥 Defter notu silme: Id={id}");
                var not = await _context.DefterNotlari.FindAsync(id);
                if (not == null)
                {
                    Console.WriteLine($"⚠️ Silinecek defter notu bulunamadı: Id={id}");
                    return NotFound();
                }

                _context.DefterNotlari.Remove(not);
                await _context.SaveChangesAsync();
                Console.WriteLine($"✅ Defter notu silindi: Id={id}");
                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Defter notu silme hatası: {ex.InnerException?.Message ?? ex.Message}");
                return StatusCode(500, $"Defter notu silinirken hata oluştu: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        // Diğer metodlar...
    }
}