// Models/Enums.cs
using System.Text.Json.Serialization;

namespace is_takip.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Rol { SUPER_ADMIN, VIEWER, FOREMAN }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum OdemeyiYapan { Omer, Baris, Kasa }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum OdemeYontemi { cash, transfer, card }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum OdemeDurumu { paid, unpaid }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum GelirOdemeYontemi { TRY, USD, EUR, GOLD }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AltinTuru { gram, quarter, full }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DefterKayitTipi { income, expense }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum NotKategorisi { todo, reminder, important }
}