using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace is_takip.Models
{
    [Table("ishakedisleri")]
    public class IsHakedisleri
    {
        [Key]
        [Column("is_hakedis_id")]
        public int IsHakedisId { get; set; }

        [Column("is_id")]
        public int IsId { get; set; }

        [Column("personel_id")]
        [JsonPropertyName("personnelId")]
        public int PersonelId { get; set; }

        [Column("hakedis_tutari")]
        [JsonPropertyName("payment")]
        public decimal HakedisTutari { get; set; }

        [Column("calisilan_gun_sayisi")]
        [JsonPropertyName("daysWorked")]
        public int CalisilanGunSayisi { get; set; }

        [Column("odeme_yontemi")]
        [JsonPropertyName("paymentMethod")]
        public OdemeYontemi? OdemeYontemi { get; set; }

        [ForeignKey("IsId")]
        public virtual MusteriIsleri? Is { get; set; }
    }
}