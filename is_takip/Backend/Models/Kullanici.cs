// Models/Kullanici.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using is_takip.Models;

namespace is_takip.Models
{
    [Table("Kullanicilar")]
    public class Kullanici
    {
        [Key]
        [Column("kullanici_id")]
        [JsonPropertyName("id")]
        public int KullaniciId { get; set; }

        [Column("ad_soyad")]
        [JsonPropertyName("name")]
        public string AdSoyad { get; set; } = string.Empty;

        [Column("kullanici_adi")]
        [JsonPropertyName("email")] // React'te 'email' olarak kullanılıyor
        public string KullaniciAdi { get; set; } = string.Empty;

        [Column("sifre")]
        [JsonPropertyName("password")]
        public string Sifre { get; set; } = string.Empty;

        [Column("rol")]
        [JsonPropertyName("role")]
        public Rol Rol { get; set; }
    }
}