// Controllers/MusterilerController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using is_takip.Data;
using is_takip.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace is_takip.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MusterilerController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MusterilerController(ApplicationDbContext context)
        {
            _context = context;
        }

        private static DateTime ToUtc(DateTime value)
        {
            if (value == default) return DateTime.UtcNow;
            if (value.Kind == DateTimeKind.Utc) return value;
            return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        // ===================== MÜŞTERİ CRUD =====================
        [HttpPost]
        public async Task<ActionResult<Musteri>> CreateMusteri([FromBody] Musteri musteri)
        {
            try
            {
                _context.Musteriler.Add(musteri);
                await _context.SaveChangesAsync();
                Console.WriteLine($"✅ Müşteri eklendi: Id={musteri.MusteriId}");
                return Ok(musteri);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Müşteri ekleme hatası: {ex.Message}");
                return StatusCode(500, $"Müşteri eklenirken hata: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Musteri>>> GetMusteriler()
        {
            try
            {
                var musteriler = await _context.Musteriler
                    .AsNoTracking()
                    .ToListAsync();
                Console.WriteLine($"📋 {musteriler.Count} müşteri listelendi");
                return Ok(musteriler);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Müşteri listeleme hatası: {ex.Message}");
                return StatusCode(500, $"Müşteriler listelenirken hata: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Musteri>> GetMusteri(int id)
        {
            try
            {
                var musteri = await _context.Musteriler.FindAsync(id);
                if (musteri == null)
                {
                    Console.WriteLine($"⚠️ Müşteri bulunamadı: Id={id}");
                    return NotFound();
                }
                return Ok(musteri);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Müşteri getirme hatası: {ex.Message}");
                return StatusCode(500, $"Müşteri getirilirken hata: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMusteri(int id, [FromBody] Musteri musteri)
        {
            if (id != musteri.MusteriId)
            {
                Console.WriteLine($"⚠️ ID uyumsuzluğu: Route={id}, Body={musteri.MusteriId}");
                return BadRequest("Route id ve body id aynı olmalı.");
            }

            try
            {
                _context.Entry(musteri).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                Console.WriteLine($"✅ Müşteri güncellendi: Id={id}");
                return Ok(musteri);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                Console.WriteLine($"❌ Müşteri güncelleme hatası (Concurrency): {ex.Message}");
                return StatusCode(409, "Kayıt başka bir işlem tarafından değiştirilmiş.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Müşteri güncelleme hatası: {ex.Message}");
                return StatusCode(500, $"Müşteri güncellenirken hata: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMusteri(int id)
        {
            try
            {
                var musteri = await _context.Musteriler.FindAsync(id);
                if (musteri == null)
                {
                    Console.WriteLine($"⚠️ Silinecek müşteri bulunamadı: Id={id}");
                    return NotFound();
                }

                _context.Musteriler.Remove(musteri);
                await _context.SaveChangesAsync();
                Console.WriteLine($"✅ Müşteri silindi: Id={id}");
                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Müşteri silme hatası: {ex.Message}");
                return StatusCode(500, $"Müşteri silinirken hata: {ex.Message}");
            }
        }

        // ============== MÜŞTERİ İŞLERİ (JOBS) CRUD ==============
        [HttpGet("isler")]
        public async Task<ActionResult<IEnumerable<MusteriIsleri>>> GetIsler()
        {
            try
            {
                var jobs = await _context.MusteriIsleri
                    .AsNoTracking()
                    .Include(j => j.IsHakedisleri)
                    .Include(j => j.IsMalzemeleri)
                    .ToListAsync();

                Console.WriteLine($"📋 {jobs.Count} iş listelendi");
                foreach (var job in jobs)
                {
                    Console.WriteLine($"  - İş {job.IsId}: {job.IsHakedisleri.Count} hakediş, {job.IsMalzemeleri.Count} malzeme");
                }

                return Ok(jobs);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ İş listeleme hatası: {ex.Message}");
                return StatusCode(500, $"İşler listelenirken hata: {ex.Message}");
            }
        }

        [HttpGet("isler/{id}")]
        public async Task<ActionResult<MusteriIsleri>> GetIs(int id)
        {
            try
            {
                var job = await _context.MusteriIsleri
                    .AsNoTracking()
                    .Include(j => j.IsHakedisleri)
                    .Include(j => j.IsMalzemeleri)
                    .FirstOrDefaultAsync(j => j.IsId == id);

                if (job == null)
                {
                    Console.WriteLine($"⚠️ İş bulunamadı: Id={id}");
                    return NotFound();
                }

                Console.WriteLine($"📄 İş detayı getirildi: Id={id}, Hakediş={job.IsHakedisleri.Count}, Malzeme={job.IsMalzemeleri.Count}");
                return Ok(job);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ İş getirme hatası: {ex.Message}");
                return StatusCode(500, $"İş getirilirken hata: {ex.Message}");
            }
        }

        [HttpPost("isler")]
        public async Task<ActionResult<MusteriIsleri>> CreateMusteriIsi([FromBody] MusteriIsleri musteriIsi)
        {
            Console.WriteLine($"📥 Yeni iş ekleme isteği: MusteriId={musteriIsi.MusteriId}, Konum={musteriIsi.Konum}");

            // 1) Müşteri var mı? (FK)
            var musteriVarMi = await _context.Musteriler.AnyAsync(m => m.MusteriId == musteriIsi.MusteriId);
            if (!musteriVarMi)
            {
                Console.WriteLine($"⚠️ Geçersiz müşteri ID: {musteriIsi.MusteriId}");
                return BadRequest("Geçersiz customerId (musteri bulunamadı).");
            }

            // 2) Zorunlu alanlar
            if (string.IsNullOrWhiteSpace(musteriIsi.Konum))
            {
                Console.WriteLine("⚠️ Konum boş");
                return BadRequest("Konum zorunludur.");
            }
            if (string.IsNullOrWhiteSpace(musteriIsi.IsAciklamasi))
            {
                Console.WriteLine("⚠️ İş açıklaması boş");
                return BadRequest("İş açıklaması zorunludur.");
            }

            // 3) Tarih -> UTC
            musteriIsi.Tarih = ToUtc(musteriIsi.Tarih);

            // 4) Enum tutarlılığı (GOLD ise altın türü zorunlu)
            if (musteriIsi.GelirOdemeYontemi != null &&
                musteriIsi.GelirOdemeYontemi == GelirOdemeYontemi.GOLD &&
                musteriIsi.GelirAltinTuru == null)
            {
                Console.WriteLine("⚠️ Altın türü eksik");
                return BadRequest("Altın seçildi ise incomeGoldType zorunludur.");
            }

            try
            {
                _context.MusteriIsleri.Add(musteriIsi);
                await _context.SaveChangesAsync();

                Console.WriteLine($"✅ İş eklendi: Id={musteriIsi.IsId}");

                // ✅ KRİTİK: Yeni eklenen işi ilişkileriyle birlikte tekrar yükle
                var savedJob = await _context.MusteriIsleri
                    .Include(j => j.IsHakedisleri)
                    .Include(j => j.IsMalzemeleri)
                    .FirstOrDefaultAsync(j => j.IsId == musteriIsi.IsId);

                if (savedJob != null)
                {
                    Console.WriteLine($"📤 İş döndürülüyor: Id={savedJob.IsId}, Hakediş={savedJob.IsHakedisleri.Count}, Malzeme={savedJob.IsMalzemeleri.Count}");
                }

                return Ok(savedJob);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ İş ekleme hatası: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, $"İş eklenirken hata: {ex.Message}");
            }
        }

        [HttpPut("isler/{id}")]
        public async Task<IActionResult> UpdateMusteriIsi(int id, [FromBody] MusteriIsleri dto)
        {
            Console.WriteLine($"📥 İş güncelleme isteği: Id={id}");

            if (id != dto.IsId)
            {
                Console.WriteLine($"⚠️ ID uyumsuzluğu: Route={id}, Body={dto.IsId}");
                return BadRequest("Route id ve body id aynı olmalı.");
            }

            var mevcut = await _context.MusteriIsleri.FirstOrDefaultAsync(j => j.IsId == id);
            if (mevcut == null)
            {
                Console.WriteLine($"⚠️ Güncellenecek iş bulunamadı: Id={id}");
                return NotFound();
            }

            // Zorunlu alanlar
            if (string.IsNullOrWhiteSpace(dto.Konum))
            {
                Console.WriteLine("⚠️ Konum boş");
                return BadRequest("Konum zorunludur.");
            }
            if (string.IsNullOrWhiteSpace(dto.IsAciklamasi))
            {
                Console.WriteLine("⚠️ İş açıklaması boş");
                return BadRequest("İş açıklaması zorunludur.");
            }

            try
            {
                // Güncelleme
                mevcut.MusteriId = dto.MusteriId;
                mevcut.Konum = dto.Konum;
                mevcut.IsAciklamasi = dto.IsAciklamasi;
                mevcut.Tarih = ToUtc(dto.Tarih);
                mevcut.GelirTutari = dto.GelirTutari;
                mevcut.GelirOdemeYontemi = dto.GelirOdemeYontemi;
                mevcut.GelirAltinTuru = dto.GelirAltinTuru;

                // Enum tutarlılığı
                if (mevcut.GelirOdemeYontemi != null &&
                    mevcut.GelirOdemeYontemi == GelirOdemeYontemi.GOLD &&
                    mevcut.GelirAltinTuru == null)
                {
                    Console.WriteLine("⚠️ Altın türü eksik");
                    return BadRequest("Altın seçildi ise incomeGoldType zorunludur.");
                }

                await _context.SaveChangesAsync();
                Console.WriteLine($"✅ İş güncellendi: Id={id}");

                // ✅ KRİTİK: Güncellenen işi ilişkileriyle birlikte tekrar yükle
                var updatedJob = await _context.MusteriIsleri
                    .Include(j => j.IsHakedisleri)
                    .Include(j => j.IsMalzemeleri)
                    .FirstOrDefaultAsync(j => j.IsId == id);

                if (updatedJob != null)
                {
                    Console.WriteLine($"📤 Güncellenmiş iş döndürülüyor: Id={updatedJob.IsId}, Hakediş={updatedJob.IsHakedisleri.Count}, Malzeme={updatedJob.IsMalzemeleri.Count}");
                }

                return Ok(updatedJob);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ İş güncelleme hatası: {ex.Message}");
                return StatusCode(500, $"İş güncellenirken hata: {ex.Message}");
            }
        }

        [HttpDelete("isler/{id}")]
        public async Task<IActionResult> DeleteMusteriIsi(int id)
        {
            try
            {
                Console.WriteLine($"📥 İş silme isteği: Id={id}");

                var musteriIsi = await _context.MusteriIsleri.FindAsync(id);
                if (musteriIsi == null)
                {
                    Console.WriteLine($"⚠️ Silinecek iş bulunamadı: Id={id}");
                    return NotFound();
                }

                _context.MusteriIsleri.Remove(musteriIsi);
                await _context.SaveChangesAsync();

                Console.WriteLine($"✅ İş silindi: Id={id}");
                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ İş silme hatası: {ex.Message}");
                return StatusCode(500, $"İş silinirken hata: {ex.Message}");
            }
        }

        // ============== İŞ HAKEDİŞLERİ (EARNINGS) ==============

        [HttpPost("isler/{isId}/hakedisler")]
        public async Task<ActionResult<IsHakedisleri>> CreateHakedis(int isId, [FromBody] IsHakedisleri hakedis)
        {
            Console.WriteLine($"📥 Tekil hakediş ekleme isteği: IsId={isId}, PersonelId={hakedis.PersonelId}");

            // İş mevcut mu?
            var isVarMi = await _context.MusteriIsleri.AnyAsync(j => j.IsId == isId);
            if (!isVarMi)
            {
                Console.WriteLine($"⚠️ Geçersiz iş ID: {isId}");
                return BadRequest("Geçersiz isId.");
            }

            // Zorunlu alanlar
            if (hakedis.PersonelId <= 0)
            {
                Console.WriteLine("⚠️ Personel ID geçersiz");
                return BadRequest("Personel ID zorunludur.");
            }

            // IsId'yi set et
            hakedis.IsId = isId;

            try
            {
                _context.IsHakedisleri.Add(hakedis);
                await _context.SaveChangesAsync();

                Console.WriteLine($"✅ Hakediş eklendi: Id={hakedis.IsHakedisId}, IsId={isId}, PersonelId={hakedis.PersonelId}, Tutar={hakedis.HakedisTutari}, Gün={hakedis.CalisilanGunSayisi}");

                return Ok(hakedis);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Hakediş ekleme hatası: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, $"Hakediş eklenirken hata: {ex.Message}");
            }
        }

        [HttpPost("isler/{isId}/hakedisler/bulk")]
        public async Task<IActionResult> UpsertHakedislerBulk(int isId, [FromBody] List<IsHakedisleri> list)
        {
            Console.WriteLine($"\n{'=' * 60}");
            Console.WriteLine($"📥 BULK HAKEDİŞ İSTEĞİ ALINDI");
            Console.WriteLine($"{'=' * 60}");
            Console.WriteLine($"IsId: {isId}");
            Console.WriteLine($"Gelen kayıt sayısı: {list.Count}");

            // İş mevcut mu?
            var isVarMi = await _context.MusteriIsleri.AnyAsync(j => j.IsId == isId);
            if (!isVarMi)
            {
                Console.WriteLine($"⚠️ Geçersiz iş ID: {isId}");
                return BadRequest("Geçersiz isId.");
            }

            try
            {
                // Gelen veriyi logla
                for (int i = 0; i < list.Count; i++)
                {
                    var item = list[i];
                    Console.WriteLine($"  [{i + 1}] PersonelId={item.PersonelId}, Tutar={item.HakedisTutari}, Gün={item.CalisilanGunSayisi}, Yöntem={item.OdemeYontemi}");
                }

                // Eski hakedişleri sil
                var eskiler = await _context.IsHakedisleri.Where(h => h.IsId == isId).ToListAsync();
                Console.WriteLine($"\n🗑️ Silinecek eski hakediş sayısı: {eskiler.Count}");

                if (eskiler.Count > 0)
                {
                    _context.IsHakedisleri.RemoveRange(eskiler);
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"✅ Eski hakedişler silindi");
                }

                // Yeni hakedişleri ekle
                Console.WriteLine($"\n➕ Yeni hakedişler ekleniyor...");
                foreach (var item in list)
                {
                    item.IsId = isId;
                    item.IsHakedisId = 0; // Yeni kayıt için ID sıfırla
                    Console.WriteLine($"  + Ekleniyor: PersonelId={item.PersonelId}, Tutar={item.HakedisTutari}");
                }

                if (list.Count > 0)
                {
                    _context.IsHakedisleri.AddRange(list);
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"✅ {list.Count} yeni hakediş eklendi");
                }

                // Kaydedilen hakedişleri geri döndür
                var savedEarnings = await _context.IsHakedisleri
                    .Where(h => h.IsId == isId)
                    .ToListAsync();

                Console.WriteLine($"\n📤 DÖNDÜRÜLEN HAKEDİŞLER:");
                foreach (var saved in savedEarnings)
                {
                    Console.WriteLine($"  - Id={saved.IsHakedisId}, PersonelId={saved.PersonelId}, Tutar={saved.HakedisTutari}, Gün={saved.CalisilanGunSayisi}");
                }
                Console.WriteLine($"{'=' * 60}\n");

                return Ok(savedEarnings);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ BULK HAKEDİŞ HATASI:");
                Console.WriteLine($"Mesaj: {ex.Message}");
                Console.WriteLine($"Stack Trace:\n{ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                Console.WriteLine($"{'=' * 60}\n");

                return StatusCode(500, $"Hakedişler kaydedilirken hata: {ex.Message}");
            }
        }

        [HttpGet("isler/{isId}/hakedisler")]
        public async Task<IActionResult> GetHakedislerForIs(int isId)
        {
            try
            {
                var rows = await _context.IsHakedisleri
                    .AsNoTracking()
                    .Where(h => h.IsId == isId)
                    .ToListAsync();

                Console.WriteLine($"📊 IsId={isId} için {rows.Count} hakediş bulundu");
                foreach (var row in rows)
                {
                    Console.WriteLine($"  - Id={row.IsHakedisId}, PersonelId={row.PersonelId}, Tutar={row.HakedisTutari}");
                }

                return Ok(rows);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Hakediş listeleme hatası: {ex.Message}");
                return StatusCode(500, $"Hakedişler listelenirken hata: {ex.Message}");
            }
        }

        [HttpPut("hakedisler/{id}")]
        public async Task<IActionResult> UpdateHakedis(int id, [FromBody] IsHakedisleri hakedis)
        {
            Console.WriteLine($"📥 Hakediş güncelleme isteği: Id={id}");

            if (id != hakedis.IsHakedisId)
            {
                Console.WriteLine($"⚠️ ID uyumsuzluğu: Route={id}, Body={hakedis.IsHakedisId}");
                return BadRequest("Route id ve body id aynı olmalı.");
            }

            try
            {
                var mevcut = await _context.IsHakedisleri.FirstOrDefaultAsync(h => h.IsHakedisId == id);
                if (mevcut == null)
                {
                    Console.WriteLine($"⚠️ Güncellenecek hakediş bulunamadı: Id={id}");
                    return NotFound();
                }

                mevcut.PersonelId = hakedis.PersonelId;
                mevcut.HakedisTutari = hakedis.HakedisTutari;
                mevcut.CalisilanGunSayisi = hakedis.CalisilanGunSayisi;
                mevcut.OdemeYontemi = hakedis.OdemeYontemi;

                await _context.SaveChangesAsync();
                Console.WriteLine($"✅ Hakediş güncellendi: Id={id}");

                return Ok(mevcut);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Hakediş güncelleme hatası: {ex.Message}");
                return StatusCode(500, $"Hakediş güncellenirken hata: {ex.Message}");
            }
        }

        [HttpDelete("hakedisler/{id}")]
        public async Task<IActionResult> DeleteHakedis(int id)
        {
            try
            {
                Console.WriteLine($"📥 Hakediş silme isteği: Id={id}");

                var hakedis = await _context.IsHakedisleri.FindAsync(id);
                if (hakedis == null)
                {
                    Console.WriteLine($"⚠️ Silinecek hakediş bulunamadı: Id={id}");
                    return NotFound();
                }

                _context.IsHakedisleri.Remove(hakedis);
                await _context.SaveChangesAsync();

                Console.WriteLine($"✅ Hakediş silindi: Id={id}");
                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Hakediş silme hatası: {ex.Message}");
                return StatusCode(500, $"Hakediş silinirken hata: {ex.Message}");
            }
        }

        // ============== İŞ MALZEMELERİ (MATERIALS) ==============

        [HttpPost("isler/{isId}/malzemeler/bulk")]
        public async Task<IActionResult> UpsertMalzemelerBulk(int isId, [FromBody] List<IsMalzemeleri> list)
        {
            Console.WriteLine($"\n📥 BULK MALZEME İSTEĞİ: IsId={isId}, Kayıt sayısı={list.Count}");

            // İş mevcut mu?
            var isVarMi = await _context.MusteriIsleri.AnyAsync(j => j.IsId == isId);
            if (!isVarMi)
            {
                Console.WriteLine($"⚠️ Geçersiz iş ID: {isId}");
                return BadRequest("Geçersiz isId.");
            }

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Eski kayıtları kaldır
                    var eskiler = await _context.IsMalzemeleri.Where(m => m.IsId == isId).ToListAsync();
                    Console.WriteLine($"🗑️ Silinecek eski malzeme sayısı: {eskiler.Count}");

                    if (eskiler.Any())
                    {
                        _context.IsMalzemeleri.RemoveRange(eskiler);
                        await _context.SaveChangesAsync();
                    }

                    // Yeni listeyi ekle
                    foreach (var item in list)
                    {
                        item.IsId = isId;
                        item.IsMalzemeId = 0; // Yeni kayıt için ID sıfırla
                        Console.WriteLine($"  + Ekleniyor: {item.MalzemeAdi}, Miktar={item.Miktar}, BirimFiyat={item.BirimFiyat}");
                    }

                    await _context.IsMalzemeleri.AddRangeAsync(list);
                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();

                    Console.WriteLine($"✅ {list.Count} malzeme eklendi");
                    return Ok(list);
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync();
                    Console.WriteLine($"❌ BULK MALZEME HATASI: {ex.Message}\nINNER: {ex.InnerException?.Message}");
                    return StatusCode(500, $"Malzemeler kaydedilirken bir sunucu hatası oluştu: {ex.Message}");
                }
            });
        }

        [HttpGet("isler/{isId}/malzemeler")]
        public async Task<IActionResult> GetMalzemelerForIs(int isId)
        {
            try
            {
                var rows = await _context.IsMalzemeleri
                    .AsNoTracking()
                    .Where(m => m.IsId == isId)
                    .ToListAsync();

                Console.WriteLine($"📊 IsId={isId} için {rows.Count} malzeme bulundu");
                return Ok(rows);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Malzeme listeleme hatası: {ex.Message}");
                return StatusCode(500, $"Malzemeler listelenirken hata: {ex.Message}");
            }
        }

        [HttpPost("malzemeler")]
        public async Task<ActionResult<IsMalzemeleri>> CreateMalzeme([FromBody] IsMalzemeleri malzeme)
        {
            Console.WriteLine($"📥 Tekil malzeme ekleme: IsId={malzeme.IsId}, Ad={malzeme.MalzemeAdi}");

            // İş mevcut mu?
            var isVarMi = await _context.MusteriIsleri.AnyAsync(j => j.IsId == malzeme.IsId);
            if (!isVarMi)
            {
                Console.WriteLine($"⚠️ Geçersiz iş ID: {malzeme.IsId}");
                return BadRequest("Geçersiz IsId.");
            }

            // Zorunlu alanlar
            if (string.IsNullOrWhiteSpace(malzeme.MalzemeAdi))
            {
                Console.WriteLine("⚠️ Malzeme adı boş");
                return BadRequest("Malzeme adı zorunludur.");
            }

            try
            {
                _context.IsMalzemeleri.Add(malzeme);
                await _context.SaveChangesAsync();

                Console.WriteLine($"✅ Malzeme eklendi: Id={malzeme.IsMalzemeId}");
                return Ok(malzeme);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Malzeme ekleme hatası: {ex.Message}");
                return StatusCode(500, $"Malzeme eklenirken hata: {ex.Message}");
            }
        }

        [HttpPut("malzemeler/{id}")]
        public async Task<IActionResult> UpdateMalzeme(int id, [FromBody] IsMalzemeleri malzeme)
        {
            Console.WriteLine($"📥 Malzeme güncelleme isteği: Id={id}");

            if (id != malzeme.IsMalzemeId)
            {
                Console.WriteLine($"⚠️ ID uyumsuzluğu: Route={id}, Body={malzeme.IsMalzemeId}");
                return BadRequest("Route id ve body id aynı olmalı.");
            }

            try
            {
                var mevcut = await _context.IsMalzemeleri.FirstOrDefaultAsync(m => m.IsMalzemeId == id);
                if (mevcut == null)
                {
                    Console.WriteLine($"⚠️ Güncellenecek malzeme bulunamadı: Id={id}");
                    return NotFound();
                }

                // Zorunlu alanlar
                if (string.IsNullOrWhiteSpace(malzeme.MalzemeAdi))
                {
                    Console.WriteLine("⚠️ Malzeme adı boş");
                    return BadRequest("Malzeme adı zorunludur.");
                }

                // Güncelleme
                mevcut.IsId = malzeme.IsId;
                mevcut.MalzemeAdi = malzeme.MalzemeAdi;
                mevcut.Birim = malzeme.Birim;
                mevcut.Miktar = malzeme.Miktar;
                mevcut.BirimFiyat = malzeme.BirimFiyat;

                await _context.SaveChangesAsync();
                Console.WriteLine($"✅ Malzeme güncellendi: Id={id}");

                return Ok(mevcut);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Malzeme güncelleme hatası: {ex.Message}");
                return StatusCode(500, $"Malzeme güncellenirken hata: {ex.Message}");
            }
        }

        [HttpDelete("malzemeler/{id}")]
        public async Task<IActionResult> DeleteMalzeme(int id)
        {
            try
            {
                Console.WriteLine($"📥 Malzeme silme isteği: Id={id}");

                var malzeme = await _context.IsMalzemeleri.FindAsync(id);
                if (malzeme == null)
                {
                    Console.WriteLine($"⚠️ Silinecek malzeme bulunamadı: Id={id}");
                    return NotFound();
                }

                _context.IsMalzemeleri.Remove(malzeme);
                await _context.SaveChangesAsync();

                Console.WriteLine($"✅ Malzeme silindi: Id={id}");
                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Malzeme silme hatası: {ex.Message}");
                return StatusCode(500, $"Malzeme silinirken hata: {ex.Message}");
            }
        }
    }
}