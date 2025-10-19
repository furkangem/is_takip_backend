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

                // Frontend'den gelen tarihi kullanıyoruz (önceki düzeltmemiz)
                // gider.Tarih = ToGmt3(DateTime.UtcNow); // Bu satır kaldırılmıştı, doğru.

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
                mevcutGider.Tarih = gelenGiderVerisi.Tarih; // Frontend'den gelen tarih
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

                // Soft delete - silinme tarihini ayarla (UTC olarak)
                gider.SilinmeTarihi = DateTime.UtcNow; // STANDART YÖNTEM
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
        // --- YENİ EKLENEN BÖLÜM: DEFTER KAYITLARI (DefterEntry) ---
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
                // Frontend'den gelen tarih 'YYYY-MM-DD' formatındadır ve
                // .NET bunu T00:00:00 olarak ve Kind=Unspecified olarak alır.
                // 'timestamp without time zone' için bu doğrudur, olduğu gibi kaydedilir.

                _context.DefterKayitlari.Add(kayit);
                await _context.SaveChangesAsync();
                Console.WriteLine($"✅ Defter kaydı eklendi: Id={kayit.KayitId}");
                return Ok(kayit); // Yeni oluşturulan kaydı ID'si ile birlikte döndür
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

                // Değerleri güncelle
                mevcutKayit.IslemTarihi = gelenKayit.IslemTarihi;
                mevcutKayit.Aciklama = gelenKayit.Aciklama;
                mevcutKayit.Tutar = gelenKayit.Tutar;
                mevcutKayit.Tip = gelenKayit.Tip;
                mevcutKayit.Durum = gelenKayit.Durum;
                mevcutKayit.VadeTarihi = gelenKayit.VadeTarihi;
                mevcutKayit.OdenmeTarihi = gelenKayit.OdenmeTarihi;
                mevcutKayit.Notlar = gelenKayit.Notlar;

                await _context.SaveChangesAsync();
                Console.WriteLine($"✅ Defter kaydı güncellendi: Id={id}");
                return Ok(mevcutKayit); // Güncellenmiş nesneyi döndür
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
        // --- YENİ EKLENEN BÖLÜM: DEFTER NOTLARI (DefterNote) ---
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

                // Oluşturma tarihini sunucuda ayarla (Koddaki ToGmt3 standardına uyarak)
                not.OlusturmaTarihi = ToGmt3(DateTime.UtcNow);
                not.TamamlandiMi = false; // Yeni not varsayılan olarak tamamlanmadı

                _context.DefterNotlari.Add(not);
                await _context.SaveChangesAsync();
                Console.WriteLine($"✅ Defter notu eklendi: Id={not.NotId}");
                return Ok(not); // Yeni oluşturulan nesneyi ID'si ile birlikte döndür
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

                // Değerleri güncelle
                mevcutNot.Baslik = gelenNot.Baslik;
                mevcutNot.Aciklama = gelenNot.Aciklama;
                mevcutNot.Kategori = gelenNot.Kategori;
                mevcutNot.VadeTarihi = gelenNot.VadeTarihi;
                mevcutNot.TamamlandiMi = gelenNot.TamamlandiMi;
                // OlusturmaTarihi güncellenmemeli, orijinali korunmalı

                await _context.SaveChangesAsync();
                Console.WriteLine($"✅ Defter notu güncellendi: Id={id}");
                return Ok(mevcutNot); // Güncellenmiş nesneyi döndür
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

        // =================================================================
        // --- DİĞER KASA İŞLEMLERİ ---
        // =================================================================
        // Buraya diğer kasa işlemlerini ekleyebilirsiniz
        // Örnek: Gelirler, genel giderler, kasa bakiyesi vb.


    }
}