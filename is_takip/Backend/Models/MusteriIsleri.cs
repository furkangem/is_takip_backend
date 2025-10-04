using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace is_takip.Models
{
    [Table("musteriisleri")] // DbContext'te ToTable ile uyumlu
    public class MusteriIsleri
    {
        [Key]
        [Column("is_id")]
        [JsonPropertyName("id")]
        public int IsId { get; set; }

        [Column("musteri_id")]
        [JsonPropertyName("customerId")]
        public int MusteriId { get; set; }

        [Column("konum")]
        [JsonPropertyName("location")]
        public string Konum { get; set; } = string.Empty;

        [Column("is_aciklamasi")]
        [JsonPropertyName("description")]
        public string IsAciklamasi { get; set; } = string.Empty;

        [Column("tarih")]
        [JsonPropertyName("date")]
        public DateTime Tarih { get; set; }

        [Column("gelir_tutari")]
        [JsonPropertyName("income")]
        public decimal GelirTutari { get; set; }

        [Column("gelir_odeme_yontemi")]
        [JsonPropertyName("incomePaymentMethod")]
        public GelirOdemeYontemi? GelirOdemeYontemi { get; set; }

        [Column("gelir_altin_turu")]
        [JsonPropertyName("incomeGoldType")]
        public AltinTuru? GelirAltinTuru { get; set; }

        // KRİTİK: İşin hakedişleri
        [JsonPropertyName("personnelPayments")]
        public virtual ICollection<IsHakedisleri> IsHakedisleri { get; set; } = new List<IsHakedisleri>();

        [JsonPropertyName("materials")]
        public virtual ICollection<IsMalzemeleri> IsMalzemeleri { get; set; } = new List<IsMalzemeleri>();

    }
}