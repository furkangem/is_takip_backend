// Models/PuantajKayitlari.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using is_takip.Models; // Bu satırı ekle

namespace is_takip.Models
{
    [Table("PuantajKayitlari")]
    public class PuantajKayitlari
    {
        [Key]
        [Column("kayit_id")]
        public int KayitId { get; set; }

        [Column("personel_id")]
        public int PersonelId { get; set; }

        [Column("musteri_is_id")]
        public int MusteriIsId { get; set; }

        [Column("tarih")]
        public DateTime Tarih { get; set; }

        [Column("gunluk_ucret")]
        public decimal GunlukUcret { get; set; }

        [Column("konum")]
        public string? Konum { get; set; }

        [Column("is_tanimi")]
        public string? IsTanimi { get; set; }
    }
}