// Models/PersonelOdemeleri.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using is_takip.Models;

namespace is_takip.Models
{
    [Table("PersonelOdemeleri")]
    public class PersonelOdemeleri
    {
        [Key]
        [Column("odeme_id")]
        [JsonPropertyName("id")]
        public int OdemeId { get; set; }

        [Column("personel_id")]
        [JsonPropertyName("personnelId")]
        public int PersonelId { get; set; }

        [Column("tutar")]
        [JsonPropertyName("amount")]
        public decimal Tutar { get; set; }

        [Column("tarih")]
        [JsonPropertyName("date")]
        public DateTime Tarih { get; set; }

        [Column("musteri_is_id")]
        [JsonPropertyName("customerJobId")]
        public int? MusteriIsId { get; set; }

        [Column("odeyen_kisi")]
        [JsonPropertyName("payer")]
        public OdemeyiYapan OdeyenKisi { get; set; }

        [Column("odeme_yontemi")]
        [JsonPropertyName("paymentMethod")]
        public OdemeYontemi OdemeYontemi { get; set; }
    }
}