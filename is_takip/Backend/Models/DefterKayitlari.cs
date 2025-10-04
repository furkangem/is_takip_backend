// Models/DefterKayitlari.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using is_takip.Models;

namespace is_takip.Models
{
    [Table("DefterKayitlari")]
    public class DefterKayitlari
    {
        [Key]
        [Column("kayit_id")]
        [JsonPropertyName("id")]
        public int KayitId { get; set; }

        [Column("islem_tarihi")]
        [JsonPropertyName("date")]
        public DateTime IslemTarihi { get; set; }

        [Column("aciklama")]
        [JsonPropertyName("description")]
        public string Aciklama { get; set; } = string.Empty;

        [Column("tutar")]
        [JsonPropertyName("amount")]
        public decimal Tutar { get; set; }

        [Column("tip")]
        [JsonPropertyName("type")]
        public DefterKayitTipi Tip { get; set; }

        [Column("durum")]
        [JsonPropertyName("status")]
        public OdemeDurumu Durum { get; set; }

        [Column("vade_tarihi")]
        [JsonPropertyName("dueDate")]
        public DateTime? VadeTarihi { get; set; }

        [Column("odenme_tarihi")]
        [JsonPropertyName("paidDate")]
        public DateTime? OdenmeTarihi { get; set; }

        [Column("notlar")]
        [JsonPropertyName("notes")]
        public string? Notlar { get; set; }
    }
}