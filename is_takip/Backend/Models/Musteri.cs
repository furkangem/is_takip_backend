// Models/Musteri.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace is_takip.Models
{
    [Table("Musteriler")]
    public class Musteri
    {
        [Key]
        [Column("musteri_id")]
        [JsonPropertyName("id")]
        public int MusteriId { get; set; }

        [Column("musteri_adi")]
        [JsonPropertyName("name")]
        public string MusteriAdi { get; set; } = string.Empty;

        [Column("iletisim_bilgisi")]
        [JsonPropertyName("contactInfo")]
        public string? IletisimBilgisi { get; set; }

        [Column("adres")]
        [JsonPropertyName("address")]
        public string? Adres { get; set; }

        [Column("genel_is_tanimi")]
        [JsonPropertyName("jobDescription")]
        public string? GenelIsTanimi { get; set; }
    }
}