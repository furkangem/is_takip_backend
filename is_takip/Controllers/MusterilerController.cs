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

        // Yardımcı: DateTime'ı UTC'ye çevir (Kind=Unspecified -> UTC varsay)
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
            _context.Musteriler.Add(musteri);
            await _context.SaveChangesAsync();
            return Ok(musteri);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMusteri(int id, [FromBody] Musteri musteri)
        {
            if (id != musteri.MusteriId) return BadRequest("Route id ve body id aynı olmalı.");
            _context.Entry(musteri).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return Ok(musteri);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMusteri(int id)
        {
            var musteri = await _context.Musteriler.FindAsync(id);
            if (musteri == null) return NotFound();
            _context.Musteriler.Remove(musteri);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ============== MÜŞTERİ İŞLERİ (JOBS) CRUD ==============

        [HttpGet("isler")]
        public async Task<ActionResult<IEnumerable<MusteriIsleri>>> GetIsler()
        {
            var jobs = await _context.MusteriIsleri
                .AsNoTracking()
                .Include(j => j.IsHakedisleri)
                .Include(j => j.IsMalzemeleri)
                .ToListAsync();
            return Ok(jobs);
        }

        [HttpGet("isler/{id}")]
        public async Task<ActionResult<MusteriIsleri>> GetIs(int id)
        {
            var job = await _context.MusteriIsleri
                .AsNoTracking()
                .Include(j => j.IsHakedisleri)
                .Include(j => j.IsMalzemeleri)
                .FirstOrDefaultAsync(j => j.IsId == id);
            if (job == null) return NotFound();
            return Ok(job);
        }

        [HttpPost("isler")]
        public async Task<ActionResult<MusteriIsleri>> CreateMusteriIsi([FromBody] MusteriIsleri musteriIsi)
        {
            var musteriVarMi = await _context.Musteriler.AnyAsync(m => m.MusteriId == musteriIsi.MusteriId);
            if (!musteriVarMi)
                return BadRequest("Geçersiz customerId (musteri bulunamadı).");

            if (string.IsNullOrWhiteSpace(musteriIsi.Konum))
                return BadRequest("Konum zorunludur.");
            if (string.IsNullOrWhiteSpace(musteriIsi.IsAciklamasi))
                return BadRequest("İş açıklaması zorunludur.");

            musteriIsi.Tarih = ToUtc(musteriIsi.Tarih);

            if (musteriIsi.GelirOdemeYontemi == GelirOdemeYontemi.GOLD && musteriIsi.GelirAltinTuru == null)
            {
                return BadRequest("Altın seçildi ise incomeGoldType zorunludur.");
            }

            try
            {
                _context.MusteriIsleri.Add(musteriIsi);
                await _context.SaveChangesAsync();
                return Ok(musteriIsi);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPut("isler/{id}")]
        public async Task<IActionResult> UpdateMusteriIsi(int id, [FromBody] MusteriIsleri dto)
        {
            if (id != dto.IsId) return BadRequest("Route id ve body id aynı olmalı.");

            var mevcut = await _context.MusteriIsleri.FirstOrDefaultAsync(j => j.IsId == id);
            if (mevcut == null) return NotFound();

            if (string.IsNullOrWhiteSpace(dto.Konum))
                return BadRequest("Konum zorunludur.");
            if (string.IsNullOrWhiteSpace(dto.IsAciklamasi))
                return BadRequest("İş açıklaması zorunludur.");

            mevcut.MusteriId = dto.MusteriId;
            mevcut.Konum = dto.Konum;
            mevcut.IsAciklamasi = dto.IsAciklamasi;
            mevcut.Tarih = ToUtc(dto.Tarih);
            mevcut.GelirTutari = dto.GelirTutari;
            mevcut.GelirOdemeYontemi = dto.GelirOdemeYontemi;
            mevcut.GelirAltinTuru = dto.GelirAltinTuru;

            if (mevcut.GelirOdemeYontemi == GelirOdemeYontemi.GOLD && mevcut.GelirAltinTuru == null)
            {
                return BadRequest("Altın seçildi ise incomeGoldType zorunludur.");
            }

            await _context.SaveChangesAsync();
            return Ok(mevcut);
        }

        [HttpDelete("isler/{id}")]
        public async Task<IActionResult> DeleteMusteriIsi(int id)
        {
            var musteriIsi = await _context.MusteriIsleri.FindAsync(id);
            if (musteriIsi == null) return NotFound();
            _context.MusteriIsleri.Remove(musteriIsi);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ============== İŞ HAKEDİŞLERİ (EARNINGS) ==============

        [HttpPost("isler/{isId}/hakedisler/bulk")]
        public async Task<IActionResult> UpsertHakedislerBulk(int isId, [FromBody] List<IsHakedisleri> list)
        {
            var isVarMi = await _context.MusteriIsleri.AnyAsync(j => j.IsId == isId);
            if (!isVarMi) return BadRequest("Geçersiz isId.");

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var eskiler = await _context.IsHakedisleri.Where(h => h.IsId == isId).ToListAsync();
                if (eskiler.Any())
                {
                    _context.IsHakedisleri.RemoveRange(eskiler);
                }

                foreach (var item in list)
                {
                    item.IsId = isId;
                    item.IsHakedisId = 0;
                }
                await _context.IsHakedisleri.AddRangeAsync(list);

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                return Ok(list);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                Console.WriteLine($"HATA: UpsertHakedislerBulk - {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"INNER EXCEPTION: {ex.InnerException.Message}");
                }
                return StatusCode(500, $"Hakedişler kaydedilirken bir sunucu hatası oluştu: {ex.Message}");
            }
        }

        [HttpGet("isler/{isId}/hakedisler")]
        public async Task<IActionResult> GetHakedislerForIs(int isId)
        {
            var rows = await _context.IsHakedisleri
                .AsNoTracking()
                .Where(h => h.IsId == isId)
                .ToListAsync();
            return Ok(rows);
        }

        // ============== İŞ MALZEMELERİ (MATERIALS) ==============

        [HttpPost("isler/{isId}/malzemeler/bulk")]
        public async Task<IActionResult> UpsertMalzemelerBulk(int isId, [FromBody] List<IsMalzemeleri> list)
        {
            var isVarMi = await _context.MusteriIsleri.AnyAsync(j => j.IsId == isId);
            if (!isVarMi) return BadRequest("Geçersiz isId.");

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var eskiler = await _context.IsMalzemeleri.Where(m => m.IsId == isId).ToListAsync();
                if (eskiler.Any())
                {
                    _context.IsMalzemeleri.RemoveRange(eskiler);
                }

                foreach (var item in list)
                {
                    item.IsId = isId;
                    item.IsMalzemeId = 0;
                }
                await _context.IsMalzemeleri.AddRangeAsync(list);

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                return Ok(list);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                Console.WriteLine($"HATA: UpsertMalzemelerBulk - {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"INNER EXCEPTION: {ex.InnerException.Message}");
                }
                return StatusCode(500, $"Malzemeler kaydedilirken bir sunucu hatası oluştu: {ex.Message}");
            }
        }

        [HttpGet("isler/{isId}/malzemeler")]
        public async Task<IActionResult> GetMalzemelerForIs(int isId)
        {
            var rows = await _context.IsMalzemeleri
                .AsNoTracking()
                .Where(m => m.IsId == isId)
                .ToListAsync();
            return Ok(rows);
        }

        [HttpPost("malzemeler")]
        public async Task<ActionResult<IsMalzemeleri>> CreateMalzeme([FromBody] IsMalzemeleri malzeme)
        {
            var isVarMi = await _context.MusteriIsleri.AnyAsync(j => j.IsId == malzeme.IsId);
            if (!isVarMi) return BadRequest("Geçersiz IsId.");

            if (string.IsNullOrWhiteSpace(malzeme.MalzemeAdi))
                return BadRequest("Malzeme adı zorunludur.");

            _context.IsMalzemeleri.Add(malzeme);
            await _context.SaveChangesAsync();
            return Ok(malzeme);
        }

        [HttpPut("malzemeler/{id}")]
        public async Task<IActionResult> UpdateMalzeme(int id, [FromBody] IsMalzemeleri malzeme)
        {
            if (id != malzeme.IsMalzemeId) return BadRequest("Route id ve body id aynı olmalı.");

            var mevcut = await _context.IsMalzemeleri.FirstOrDefaultAsync(m => m.IsMalzemeId == id);
            if (mevcut == null) return NotFound();

            if (string.IsNullOrWhiteSpace(malzeme.MalzemeAdi))
                return BadRequest("Malzeme adı zorunludur.");

            mevcut.IsId = malzeme.IsId;
            mevcut.MalzemeAdi = malzeme.MalzemeAdi;
            mevcut.Birim = malzeme.Birim;
            mevcut.Miktar = malzeme.Miktar;
            mevcut.BirimFiyat = malzeme.BirimFiyat;

            await _context.SaveChangesAsync();
            return Ok(mevcut);
        }

        [HttpDelete("malzemeler/{id}")]
        public async Task<IActionResult> DeleteMalzeme(int id)
        {
            var malzeme = await _context.IsMalzemeleri.FindAsync(id);
            if (malzeme == null) return NotFound();
            _context.IsMalzemeleri.Remove(malzeme);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}