// Models/IsMalzemeleri.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace is_takip.Models
{
    [Table("IsMalzemeleri")]
    public class IsMalzemeleri
    {
        [Key]
        [Column("is_malzeme_id")]
        [JsonPropertyName("id")]
        public int IsMalzemeId { get; set; }

        [Column("is_id")]
        [ForeignKey("MusteriIsleri")]
        public int IsId { get; set; }

        [Column("malzeme_adi")]
        [JsonPropertyName("MalzemeAdi")] // ✅ Frontend'den gelen alan adı
        [Required(ErrorMessage = "Malzeme adı zorunludur.")]
        public string MalzemeAdi { get; set; }

        [Column("birim")]
        [JsonPropertyName("Birim")] // ✅ Frontend'den gelen alan adı
        public string? Birim { get; set; }

        [Column("miktar")]
        [JsonPropertyName("Miktar")] // ✅ Frontend'den gelen alan adı
        public decimal Miktar { get; set; }

        [Column("birim_fiyat")]
        [JsonPropertyName("BirimFiyat")] // ✅ Frontend'den gelen alan adı
        public decimal BirimFiyat { get; set; }

        // Navigation property
        [JsonIgnore]
        public virtual MusteriIsleri? MusteriIsleri { get; set; }
    }
}