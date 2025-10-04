// Models/OrtakGiderler.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using is_takip.Models;

namespace is_takip.Models
{
    [Table("OrtakGiderler")]
    public class OrtakGiderler
    {
        [Key]
        [Column("gider_id")]
        [JsonPropertyName("id")]
        public int GiderId { get; set; }

        // --- GÜNCELLEME BURADA ---
        [Required(ErrorMessage = "Açıklama alanı zorunludur")] // Bu etiketi ekledik
        [Column("aciklama")]
        [JsonPropertyName("description")]
        public string Aciklama { get; set; } = string.Empty;

        [Column("tutar")]
        [JsonPropertyName("amount")]
        public decimal Tutar { get; set; }

        [Column("tarih")]
        [JsonPropertyName("date")]
        public DateTime Tarih { get; set; }

        [Column("odeme_yontemi")]
        [JsonPropertyName("paymentMethod")]
        public OdemeYontemi OdemeYontemi { get; set; }

        [Column("odeyen_kisi")]
        [JsonPropertyName("payer")]
        public OdemeyiYapan OdeyenKisi { get; set; }

        [Column("durum")]
        [JsonPropertyName("status")]
        public OdemeDurumu Durum { get; set; }

        [Column("silinme_tarihi")]
        [JsonPropertyName("deletedAt")]
        public DateTime? SilinmeTarihi { get; set; }
    }
}