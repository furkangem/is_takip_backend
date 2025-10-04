// Models/DefterNotlari.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using is_takip.Models;

namespace is_takip.Models
{
    [Table("DefterNotlari")]
    public class DefterNotlari
    {
        [Key]
        [Column("not_id")]
        [JsonPropertyName("id")]
        public int NotId { get; set; }

        [Column("baslik")]
        [JsonPropertyName("title")]
        public string Baslik { get; set; } = string.Empty;

        [Column("aciklama")]
        [JsonPropertyName("description")]
        public string? Aciklama { get; set; }

        [Column("kategori")]
        [JsonPropertyName("category")]
        public NotKategorisi Kategori { get; set; }

        [Column("olusturma_tarihi")]
        [JsonPropertyName("createdAt")]
        public DateTime OlusturmaTarihi { get; set; }

        [Column("vade_tarihi")]
        [JsonPropertyName("dueDate")]
        public DateTime? VadeTarihi { get; set; }

        [Column("tamamlandi_mi")]
        [JsonPropertyName("completed")]
        public bool TamamlandiMi { get; set; }
    }
}