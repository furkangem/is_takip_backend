using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using is_takip.Data;
using is_takip.Models;
using System;
using System.Threading.Tasks;

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

        // =================================================================
        // --- ORTAK GİDERLER (SharedExpenses) CRUD İŞLEMLERİ ---
        // =================================================================

        [HttpPost("ortakgiderler")]
        public async Task<ActionResult<OrtakGiderler>> CreateOrtakGider(OrtakGiderler gider)
        {
            // Tarihi her zaman standart UTC saati olarak ayarla.
            gider.Tarih = DateTime.UtcNow;

            _context.OrtakGiderler.Add(gider);
            await _context.SaveChangesAsync();
            return Ok(gider);
        }

        [HttpPut("ortakgiderler/{id}")]
        public async Task<IActionResult> UpdateOrtakGider(int id, OrtakGiderler gelenGiderVerisi)
        {
            var mevcutGider = await _context.OrtakGiderler.FindAsync(id);
            if (mevcutGider == null) return NotFound();

            mevcutGider.Aciklama = gelenGiderVerisi.Aciklama;
            mevcutGider.Tutar = gelenGiderVerisi.Tutar;
            mevcutGider.OdemeYontemi = gelenGiderVerisi.OdemeYontemi;
            mevcutGider.OdeyenKisi = gelenGiderVerisi.OdeyenKisi;
            mevcutGider.Durum = gelenGiderVerisi.Durum;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPatch("ortakgiderler/{id}/status")]
        public async Task<IActionResult> ToggleOrtakGiderStatus(int id)
        {
            var gider = await _context.OrtakGiderler.FindAsync(id);
            if (gider == null) return NotFound();

            gider.Durum = (gider.Durum == OdemeDurumu.paid) ? OdemeDurumu.unpaid : OdemeDurumu.paid;
            await _context.SaveChangesAsync();
            return Ok(gider);
        }

        [HttpDelete("ortakgiderler/{id}")]
        public async Task<IActionResult> DeleteOrtakGider(int id)
        {
            var gider = await _context.OrtakGiderler.FindAsync(id);
            if (gider == null) return NotFound();

            gider.SilinmeTarihi = DateTime.UtcNow; // Silinme tarihini UTC olarak ayarla
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("ortakgiderler/{id}/restore")]
        public async Task<IActionResult> RestoreOrtakGider(int id)
        {
            var gider = await _context.OrtakGiderler.FindAsync(id);
            if (gider == null) return NotFound();

            gider.SilinmeTarihi = null;
            await _context.SaveChangesAsync();
            return Ok(gider);
        }

        // (Diğer metotlar aynı kalabilir)
    }
}