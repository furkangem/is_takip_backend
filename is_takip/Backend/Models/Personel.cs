using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

[Table("personel")]
public class Personel
{
    [Key]
    [Column("personel_id")]
    public int PersonelId { get; set; }

    [Required(ErrorMessage = "Ad Soyad zorunludur.")]
    [Column("ad_soyad")]
    [JsonPropertyName("AdSoyad")]  // ✅ Frontend'in gönderdiği TAM isim
    public string AdSoyad { get; set; } = string.Empty;

    [Column("not_metni")]
    [JsonPropertyName("NotMetni")]  // ✅ Frontend'in gönderdiği TAM isim
    public string? NotMetni { get; set; }

    [Column("not_guncellenme_tarihi")]
    [JsonPropertyName("NotGuncellenmeTarihi")]  // ✅ Frontend'in gönderdiği TAM isim
    public DateTime? NotGuncellenmeTarihi { get; set; }
}