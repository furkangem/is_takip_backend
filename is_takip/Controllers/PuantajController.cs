// Controllers/PuantajController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using is_takip.Data; // DbContext için
using is_takip.Models; // Modeller için         
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QuestPDF.Fluent;      // QuestPDF için eklendi
using QuestPDF.Helpers;     // QuestPDF için eklendi
using QuestPDF.Infrastructure; // QuestPDF için eklendi

namespace is_takip.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PuantajController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PuantajController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Güvenli olarak DB'ye yazmadan önce DateTime'ı UTC'ye dönüştürür.
        private static DateTime EnsureUtcForWrite(DateTime dt)
        {
            if (dt == default) return DateTime.UtcNow; // Varsayılan tarihse şimdiki UTC zamanı
            if (dt.Kind == DateTimeKind.Utc) return dt; // Zaten UTC ise dokunma
            if (dt.Kind == DateTimeKind.Local) return dt.ToUniversalTime(); // Local ise UTC'ye çevir

            // Unspecified ise:
            // Sadece tarih (saat 00:00:00) ise UTC gece yarısı olarak kabul et.
            // Saat bilgisi de varsa, istemcinin yerel saati olarak kabul edip UTC'ye çevir.
            return dt.TimeOfDay == TimeSpan.Zero
                ? DateTime.SpecifyKind(dt, DateTimeKind.Utc)
                : DateTime.SpecifyKind(dt, DateTimeKind.Local).ToUniversalTime();
        }

        // Helper: Türkiye sabit GMT+3 gösterim (UI için)
        private static DateTime ToGmt3(DateTime dt)
        {
            var utc = dt.Kind == DateTimeKind.Utc ? dt : DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            return utc.AddHours(3);
        }

        // GET: api/puantaj
        // Belirli bir tarih aralığındaki tüm puantaj kayıtlarını getirir
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PuantajKayitlari>>> GetPuantajKayitlari([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                var query = _context.PuantajKayitlari.AsQueryable();

                DateTime startUtc = EnsureUtcForWrite(startDate ?? new DateTime(2023, 1, 1)); // Varsayılan başlangıç
                DateTime endUtc = EnsureUtcForWrite((endDate ?? DateTime.UtcNow).Date.AddDays(1)); // Bitiş gününün sonu

                query = query.Where(p => p.Tarih >= startUtc && p.Tarih < endUtc);

                var kayitlar = await query.OrderByDescending(p => p.Tarih).ToListAsync();
                Console.WriteLine($"📋 {kayitlar.Count} puantaj kaydı listelendi ({startUtc:yyyy-MM-dd} - {endUtc.AddDays(-1):yyyy-MM-dd}).");
                return Ok(kayitlar);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Puantaj kayıtları listeleme hatası: {ex.Message}");
                return StatusCode(500, $"Puantaj kayıtları listelenirken hata: {ex.Message}");
            }
        }

        // GET: api/puantaj/{id}
        // Tek bir puantaj kaydını getirir
        [HttpGet("{id}")]
        public async Task<ActionResult<PuantajKayitlari>> GetPuantajKaydi(int id)
        {
            // İlgili iş ve personel bilgilerini de çekmek istersen Include kullanabilirsin:
            // var kayit = await _context.PuantajKayitlari
            //                        .Include(k => k.Personel) // Navigation property varsa
            //                        .Include(k => k.MusteriIs) // Navigation property varsa
            //                        .FirstOrDefaultAsync(k => k.KayitId == id);
            var kayit = await _context.PuantajKayitlari.FindAsync(id);

            if (kayit == null)
            {
                return NotFound();
            }

            return Ok(kayit);
        }

        // POST: api/puantaj
        // Yeni bir puantaj kaydı ekler
        [HttpPost]
        public async Task<ActionResult<PuantajKayitlari>> CreatePuantajKaydi([FromBody] PuantajKayitlari kayit)
        {
            try
            {
                // Gerekli doğrulamalar
                if (!ModelState.IsValid) return BadRequest(ModelState);
                if (!await _context.Personel.AnyAsync(p => p.PersonelId == kayit.PersonelId))
                    return BadRequest("Geçersiz Personel ID.");
                if (!await _context.MusteriIsleri.AnyAsync(j => j.IsId == kayit.MusteriIsId))
                    return BadRequest("Geçersiz İş ID.");
                if (kayit.GunlukUcret < 0)
                    return BadRequest("Günlük ücret negatif olamaz.");
                if (kayit.Tarih == default) // Tarih gelmemişse hata verilebilir veya varsayılan atanabilir
                    return BadRequest("Tarih zorunludur.");

                // Tarihi UTC'ye çevirerek kaydet
                kayit.Tarih = EnsureUtcForWrite(kayit.Tarih);

                _context.PuantajKayitlari.Add(kayit);
                await _context.SaveChangesAsync();
                Console.WriteLine($"✅ Yeni puantaj kaydı eklendi: Id={kayit.KayitId}, PersonelId={kayit.PersonelId}, Tarih={kayit.Tarih}");

                // Oluşturulan kaynağın Id'si ile birlikte 201 Created döndür
                return CreatedAtAction(nameof(GetPuantajKaydi), new { id = kayit.KayitId }, kayit);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Puantaj kaydı ekleme hatası: {ex.InnerException?.Message ?? ex.Message}");
                return StatusCode(500, $"Puantaj kaydı eklenirken hata: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        // PUT: api/puantaj/{id}
        // Mevcut bir puantaj kaydını günceller
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePuantajKaydi(int id, [FromBody] PuantajKayitlari gelenKayit)
        {
            if (id != gelenKayit.KayitId)
            {
                return BadRequest("ID uyuşmazlığı.");
            }
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var mevcutKayit = await _context.PuantajKayitlari.FindAsync(id);
            if (mevcutKayit == null)
            {
                return NotFound($"ID={id} ile puantaj kaydı bulunamadı.");
            }

            // Gerekli doğrulamalar
            if (!await _context.Personel.AnyAsync(p => p.PersonelId == gelenKayit.PersonelId))
                return BadRequest("Geçersiz Personel ID.");
            if (!await _context.MusteriIsleri.AnyAsync(j => j.IsId == gelenKayit.MusteriIsId))
                return BadRequest("Geçersiz İş ID.");
            if (gelenKayit.GunlukUcret < 0)
                return BadRequest("Günlük ücret negatif olamaz.");
            if (gelenKayit.Tarih == default)
                return BadRequest("Tarih zorunludur.");


            // Mevcut kaydı gelen verilerle güncelle (Sadece izin verilen alanları güncellemek daha güvenli olabilir)
            mevcutKayit.PersonelId = gelenKayit.PersonelId;
            mevcutKayit.MusteriIsId = gelenKayit.MusteriIsId;
            mevcutKayit.Tarih = EnsureUtcForWrite(gelenKayit.Tarih); // Tarihi UTC'ye çevirerek güncelle
            mevcutKayit.GunlukUcret = gelenKayit.GunlukUcret;
            mevcutKayit.Konum = gelenKayit.Konum; // Null olabilir
            mevcutKayit.IsTanimi = gelenKayit.IsTanimi; // Null olabilir

            // Entity Framework'e değişikliği bildir (FindAsync ile çektiğimiz için zaten izleniyor ama emin olmak için)
            _context.Entry(mevcutKayit).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                Console.WriteLine($"✅ Puantaj kaydı güncellendi: Id={id}");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                Console.WriteLine($"❌ Puantaj kaydı güncelleme concurrency hatası: {ex.Message}");
                if (!await PuantajKaydiExists(id))
                {
                    return NotFound();
                }
                else
                {
                    // Concurrency hatasını istemciye bildir (opsiyonel)
                    return Conflict("Kayıt başka bir kullanıcı tarafından değiştirilmiş olabilir. Lütfen sayfayı yenileyip tekrar deneyin.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Puantaj kaydı güncelleme hatası: {ex.InnerException?.Message ?? ex.Message}");
                return StatusCode(500, $"Puantaj kaydı güncellenirken hata: {ex.InnerException?.Message ?? ex.Message}");
            }

            // Başarılı güncelleme sonrası güncellenmiş nesneyi döndür
            return Ok(mevcutKayit);
        }

        // DELETE: api/puantaj/{id}
        // Bir puantaj kaydını siler
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePuantajKaydi(int id)
        {
            try
            {
                var kayit = await _context.PuantajKayitlari.FindAsync(id);
                if (kayit == null)
                {
                    Console.WriteLine($"⚠️ Silinecek puantaj kaydı bulunamadı: Id={id}");
                    return NotFound();
                }

                _context.PuantajKayitlari.Remove(kayit);
                await _context.SaveChangesAsync();
                Console.WriteLine($"✅ Puantaj kaydı silindi: Id={id}");

                return NoContent(); // Başarılı silme sonrası 204 No Content
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Puantaj kaydı silme hatası: {ex.InnerException?.Message ?? ex.Message}");
                return StatusCode(500, $"Puantaj kaydı silinirken hata: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        // Helper method to check if an entry exists
        private async Task<bool> PuantajKaydiExists(int id)
        {
            return await _context.PuantajKayitlari.AnyAsync(e => e.KayitId == id);
        }

        // === PDF RAPOR ENDPOINT'İ (geliştirilmiş: filtreler ve farklı formatlar) ===
        // Query params:
        // - startDate, endDate : tarih aralığı
        // - personelId : sadece belirtilen personele ait kayıtlar
        // - isId : sadece belirtilen işe ait kayıtlar
        // - groupBy : "personel" (varsayılan) veya "is" (işe göre)
        [HttpGet("report/pdf")]
        public async Task<IActionResult> GeneratePuantajPdfReport(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int? personelId,
            [FromQuery] int? isId,
            [FromQuery] string groupBy = "personel")
        {
            try
            {
                var query = _context.PuantajKayitlari.AsNoTracking();

                DateTime start = startDate ?? new DateTime(2023, 1, 1);
                DateTime end = endDate ?? DateTime.UtcNow;
                DateTime startUtc = EnsureUtcForWrite(start.Date);
                DateTime endUtc = EnsureUtcForWrite(end.Date.AddDays(1)); // exclusive end

                query = query.Where(p => p.Tarih >= startUtc && p.Tarih < endUtc);

                if (personelId.HasValue)
                    query = query.Where(p => p.PersonelId == personelId.Value);

                if (isId.HasValue)
                    query = query.Where(p => p.MusteriIsId == isId.Value);

                var puantajKayitlari = await query.ToListAsync();

                if (!puantajKayitlari.Any())
                {
                        return NotFound("Belirtilen filtrelerde puantaj verisi bulunamadı.");
                }

                // Load related names (personel & is) for display
                var personelIds = puantajKayitlari.Select(p => p.PersonelId).Distinct().ToList();
                var personeller = await _context.Personel
                                        .Where(p => personelIds.Contains(p.PersonelId))
                                        .ToDictionaryAsync(p => p.PersonelId, p => p.AdSoyad);

                var isIds = puantajKayitlari.Select(p => p.MusteriIsId).Distinct().ToList();
                var isler = await _context.MusteriIsleri
                                .Where(i => isIds.Contains(i.IsId))
                                .ToDictionaryAsync(i => i.IsId, i => string.IsNullOrWhiteSpace(i.IsAciklamasi) ? $"İş #{i.IsId}" : i.IsAciklamasi);

                // Prepare report data depending on grouping
                if (groupBy?.ToLowerInvariant() == "is")
                {
                    var jobGroups = puantajKayitlari
                        .GroupBy(p => p.MusteriIsId)
                        .Select(g => new
                        {
                            JobId = g.Key,
                            JobName = isler.ContainsKey(g.Key) ? isler[g.Key] : $"İş #{g.Key}",
                            Personnel = g.GroupBy(x => x.PersonelId)
                                          .Select(pg => new
                                          {
                                              PersonnelId = pg.Key,
                                              PersonnelName = personeller.ContainsKey(pg.Key) ? personeller[pg.Key] : "Bilinmeyen",
                                              Days = pg.Count(),
                                              Earnings = pg.Sum(x => x.GunlukUcret)
                                          })
                                          .OrderBy(p => p.PersonnelName)
                                          .ToList(),
                            TotalEarnings = g.Sum(x => x.GunlukUcret),
                            TotalDays = g.Count()
                        })
                        .OrderBy(j => j.JobName)
                        .ToList();

                    // Build PDF grouped by job
                    QuestPDF.Settings.License = LicenseType.Community;

                    var pdfBytes = Document.Create(container =>
                    {
                        container.Page(page =>
                        {
                            page.Size(PageSizes.A4);
                            page.Margin(1.5f, Unit.Centimetre);
                            // Prefer common fonts that support Turkish characters
                            page.DefaultTextStyle(ts => ts.FontSize(10).FontFamily("Arial"));

                            page.Header().Element(headerContainer =>
                            {
                                headerContainer.Row(row =>
                                {
                                    row.RelativeItem().Column(column =>
                                    {
                                        column.Item().Text("İşe Göre Puantaj Raporu").Bold().FontSize(16);
                                        column.Item().Text($"Filtre: {(isId.HasValue ? $"İş: {isId}" : "Tümü")}  {(personelId.HasValue ? $" - Personel: {personelId}" : string.Empty)}").FontSize(9).FontColor(Colors.Grey.Medium);
                                        column.Item().Text($"Tarih Aralığı: {start:dd.MM.yyyy} - {end:dd.MM.yyyy}").FontSize(9).FontColor(Colors.Grey.Medium);
                                        column.Item().Text($"Oluşturma: {ToGmt3(DateTime.UtcNow):dd.MM.yyyy HH:mm}").FontSize(8).FontColor(Colors.Grey.Lighten2);
                                    });
                                });
                                headerContainer.PaddingBottom(1, Unit.Centimetre);
                                headerContainer.BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                            });

                            page.Content().Element(content =>
                            {
                                foreach (var job in jobGroups)
                                {
                                    content.Column(col =>
                                    {
                                        col.Item().Text($"{job.JobName} — Toplam Gün: {job.TotalDays}, Toplam Hakediş: {job.TotalEarnings:C}", TextStyle.Default.FontSize(11).SemiBold());

                                        col.Item().Table(table =>
                                        {
                                            table.ColumnsDefinition(columns =>
                                            {
                                                columns.ConstantColumn(30);
                                                columns.RelativeColumn(3);
                                                columns.ConstantColumn(70);
                                                columns.ConstantColumn(90);
                                            });

                                            table.Header(header =>
                                            {
                                                static IContainer HeadCell(IContainer c) => c.DefaultTextStyle(x => x.Bold()).Padding(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                                                header.Cell().Element(HeadCell).Text("#");
                                                header.Cell().Element(HeadCell).Text("Personel");
                                                header.Cell().Element(HeadCell).AlignRight().Text("Çalışma Günü");
                                                header.Cell().Element(HeadCell).AlignRight().Text("Hakediş");
                                            });

                                            int idx = 1;
                                            foreach (var p in job.Personnel)
                                            {
                                                table.Cell().Text(idx++.ToString());
                                                table.Cell().Text(p.PersonnelName);
                                                table.Cell().AlignRight().Text($"{p.Days} gün");
                                                table.Cell().AlignRight().Text(p.Earnings.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("tr-TR")));
                                            }
                                        });

                                        // small spacer
                                        col.Item().PaddingVertical(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten3);
                                    });
                                }
                            });

                            page.Footer().AlignCenter().Text(text =>
                            {
                                text.Span("Sayfa ").FontSize(8);
                                text.CurrentPageNumber().FontSize(8);
                                text.Span(" / ").FontSize(8);
                                text.TotalPages().FontSize(8);
                            });
                        });
                    }).GeneratePdf();

                    string fileName = $"Puantaj_IseGore_{start:yyyyMMdd}_{end:yyyyMMdd}.pdf";
                    return File(pdfBytes, "application/pdf", fileName);
                }
                else
                {
                    // Default: group by personel
                    var personelGroups = puantajKayitlari
                        .GroupBy(p => p.PersonelId)
                        .Select(g => new
                        {
                            PersonnelId = g.Key,
                            PersonnelName = personeller.ContainsKey(g.Key) ? personeller[g.Key] : "Bilinmeyen Personel",
                            TotalDaysWorked = g.Count(),
                            TotalEarnings = g.Sum(p => p.GunlukUcret),
                            Jobs = g.GroupBy(x => x.MusteriIsId)
                                    .Select(jg => new
                                    {
                                        JobId = jg.Key,
                                        JobName = isler.ContainsKey(jg.Key) ? isler[jg.Key] : $"İş #{jg.Key}",
                                        Days = jg.Count(),
                                        Earnings = jg.Sum(x => x.GunlukUcret)
                                    }).ToList()
                        })
                        .OrderBy(p => p.PersonnelName)
                        .ToList();

                    QuestPDF.Settings.License = LicenseType.Community;

                    var pdfBytes = Document.Create(container =>
                    {
                        container.Page(page =>
                        {
                            page.Size(PageSizes.A4);
                            page.Margin(1.5f, Unit.Centimetre);
                            page.DefaultTextStyle(ts => ts.FontSize(10).FontFamily("Arial"));

                            page.Header().Element(headerContainer =>
                            {
                                headerContainer.Row(row =>
                                {
                                    row.RelativeItem().Column(column =>
                                    {
                                        column.Item().Text("Personel Bazlı Puantaj Raporu").Bold().FontSize(16);
                                        column.Item().Text($"Filtre: {(personelId.HasValue ? $"Personel: {personelId}" : "Tümü")}  {(isId.HasValue ? $" - İş: {isId}" : string.Empty)}").FontSize(9).FontColor(Colors.Grey.Medium);
                                        column.Item().Text($"Tarih Aralığı: {start:dd.MM.yyyy} - {end:dd.MM.yyyy}").FontSize(9).FontColor(Colors.Grey.Medium);
                                        column.Item().Text($"Oluşturma: {ToGmt3(DateTime.UtcNow):dd.MM.yyyy HH:mm}").FontSize(8).FontColor(Colors.Grey.Lighten2);
                                    });
                                });
                                headerContainer.PaddingBottom(1, Unit.Centimetre);
                                headerContainer.BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                            });

                            page.Content().Element(content =>
                            {
                                content.Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(25);
                                        columns.RelativeColumn(3);
                                        columns.ConstantColumn(60);
                                        columns.ConstantColumn(90);
                                    });

                                    table.Header(header =>
                                    {
                                        static IContainer HeadCell(IContainer c) => c.DefaultTextStyle(x => x.Bold()).Padding(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                                        header.Cell().Element(HeadCell).Text("#");
                                        header.Cell().Element(HeadCell).Text("Personel");
                                        header.Cell().Element(HeadCell).AlignRight().Text("Çalışma Günü");
                                        header.Cell().Element(HeadCell).AlignRight().Text("Toplam Hakediş");
                                    });

                                    int idx = 1;
                                    foreach (var p in personelGroups)
                                    {
                                        table.Cell().Text(idx++.ToString());
                                        table.Cell().Text(p.PersonnelName);
                                        table.Cell().AlignRight().Text($"{p.TotalDaysWorked} gün");
                                        table.Cell().AlignRight().Text(p.TotalEarnings.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("tr-TR")));
                                    }
                                });

                                // Optionally, append per-person job breakdowns on subsequent pages
                                foreach (var p in personelGroups)
                                {
                                    content.PageBreak();
                                    content.Column(col =>
                                    {
                                        col.Item().Text($"Detay — {p.PersonnelName}").Bold().FontSize(12);
                                        col.Item().Text($"Toplam Gün: {p.TotalDaysWorked}, Toplam Hakediş: {p.TotalEarnings:C}").FontSize(10);

                                        col.Item().Table(jobTable =>
                                        {
                                            jobTable.ColumnsDefinition(cols =>
                                            {
                                                cols.ConstantColumn(30);
                                                cols.RelativeColumn();
                                                cols.ConstantColumn(70);
                                                cols.ConstantColumn(90);
                                            });

                                            jobTable.Header(h =>
                                            {
                                                static IContainer H(IContainer c) => c.DefaultTextStyle(x => x.Bold()).Padding(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                                                h.Cell().Element(H).Text("#");
                                                h.Cell().Element(H).Text("İş");
                                                h.Cell().Element(H).AlignRight().Text("Gün");
                                                h.Cell().Element(H).AlignRight().Text("Hakediş");
                                            });

                                            int jidx = 1;
                                            foreach (var job in p.Jobs)
                                            {
                                                jobTable.Cell().Text(jidx++.ToString());
                                                jobTable.Cell().Text(job.JobName);
                                                jobTable.Cell().AlignRight().Text($"{job.Days} gün");
                                                jobTable.Cell().AlignRight().Text(job.Earnings.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("tr-TR")));
                                            }
                                        });
                                    });
                                }
                            });

                            page.Footer().AlignCenter().Text(text =>
                            {
                                text.Span("Sayfa ").FontSize(8);
                                text.CurrentPageNumber().FontSize(8);
                                text.Span(" / ").FontSize(8);
                                text.TotalPages().FontSize(8);
                            });
                        });
                    }).GeneratePdf();

                    string fileName = $"Puantaj_Personel_{start:yyyyMMdd}_{end:yyyyMMdd}.pdf";
                    return File(pdfBytes, "application/pdf", fileName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ PDF Rapor oluşturma hatası: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, $"PDF raporu oluşturulurken bir hata oluştu: {ex.Message}");
            }
        }

        // Accept requests coming via frontend proxy at /api/proxy/Puantaj/report/pdf
        [HttpGet("~/api/proxy/Puantaj/report/pdf")]
        public Task<IActionResult> GeneratePuantajPdfReport_Proxy(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int? personelId,
            [FromQuery] int? isId,
            [FromQuery] string groupBy = "personel")
        {
            return GeneratePuantajPdfReport(startDate, endDate, personelId, isId, groupBy);
        }

        // --- Helper Sınıflar ---
        private class PuantajReportItem
        {
            public int PersonnelId { get; set; }
            public string PersonnelName { get; set; } = string.Empty;
            public int TotalDaysWorked { get; set; }
            public decimal TotalEarnings { get; set; }
            // İsterseniz daha detaylı bilgi için:
            // public List<WorkDetail> WorkDetails { get; set; } = new List<WorkDetail>();
        }
    }
}