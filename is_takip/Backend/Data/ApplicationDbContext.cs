// Data/ApplicationDbContext.cs
using Microsoft.EntityFrameworkCore;
using is_takip.Models;

namespace is_takip.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        // VERİTABANINDAKİ HER TABLO İÇİN BİR DbSet TANIMLAMASI
        public DbSet<Kullanici> Kullanicilar { get; set; }
        public DbSet<Personel> Personel { get; set; }
        public DbSet<Musteri> Musteriler { get; set; }
        public DbSet<MusteriIsleri> MusteriIsleri { get; set; }
        public DbSet<IsMalzemeleri> IsMalzemeleri { get; set; }
        public DbSet<IsHakedisleri> IsHakedisleri { get; set; }
        public DbSet<PuantajKayitlari> PuantajKayitlari { get; set; }
        public DbSet<PersonelOdemeleri> PersonelOdemeleri { get; set; }
        public DbSet<OrtakGiderler> OrtakGiderler { get; set; }
        public DbSet<DefterKayitlari> DefterKayitlari { get; set; }
        public DbSet<DefterNotlari> DefterNotlari { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Tablo isimlerini Supabase'deki küçük harfli isimlerle eşleştir
            modelBuilder.Entity<Kullanici>().ToTable("kullanicilar");
            modelBuilder.Entity<Personel>().ToTable("personel");
            modelBuilder.Entity<Musteri>().ToTable("musteriler");
            modelBuilder.Entity<MusteriIsleri>().ToTable("musteriisleri");
            modelBuilder.Entity<IsMalzemeleri>().ToTable("ismalzemeleri");
            modelBuilder.Entity<IsHakedisleri>().ToTable("ishakedisleri");
            modelBuilder.Entity<PuantajKayitlari>().ToTable("puantajkayitlari");
            modelBuilder.Entity<PersonelOdemeleri>().ToTable("personelodemeleri");
            modelBuilder.Entity<OrtakGiderler>().ToTable("ortakgiderler");
            modelBuilder.Entity<DefterKayitlari>().ToTable("defterkayitlari");
            modelBuilder.Entity<DefterNotlari>().ToTable("defternotlari");

            // Enum dönüşümleri
            modelBuilder.Entity<Kullanici>()
                .Property(e => e.Rol)
                .HasConversion<string>();

            modelBuilder.Entity<MusteriIsleri>()
                .Property(e => e.GelirOdemeYontemi)
                .HasConversion<string>();

            modelBuilder.Entity<MusteriIsleri>()
                .Property(e => e.GelirAltinTuru)
                .HasConversion<string>();

            modelBuilder.Entity<IsHakedisleri>()
                .Property(e => e.OdemeYontemi)
                .HasConversion<string>();

            modelBuilder.Entity<PersonelOdemeleri>()
                .Property(e => e.OdeyenKisi)
                .HasConversion<string>();

            modelBuilder.Entity<PersonelOdemeleri>()
                .Property(e => e.OdemeYontemi)
                .HasConversion<string>();

            modelBuilder.Entity<OrtakGiderler>()
                .Property(e => e.OdemeYontemi)
                .HasConversion<string>();

            modelBuilder.Entity<OrtakGiderler>()
                .Property(e => e.OdeyenKisi)
                .HasConversion<string>();

            modelBuilder.Entity<OrtakGiderler>()
                .Property(e => e.Durum)
                .HasConversion<string>();

            modelBuilder.Entity<DefterKayitlari>()
                .Property(e => e.Tip)
                .HasConversion<string>();

            modelBuilder.Entity<DefterKayitlari>()
                .Property(e => e.Durum)
                .HasConversion<string>();

            modelBuilder.Entity<DefterNotlari>()
                .Property(e => e.Kategori)
                .HasConversion<string>();
            modelBuilder.Entity<MusteriIsleri>()
                .HasMany(j => j.IsHakedisleri)
                .WithOne(h => h.Is)
                .HasForeignKey(h => h.IsId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}