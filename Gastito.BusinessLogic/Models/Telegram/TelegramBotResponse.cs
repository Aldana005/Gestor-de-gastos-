using System.Text.Json.Serialization;

namespace Gastito.BusinessLogic.Models.Telegram;

public class TelegramInlineButton
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("callback_data")]
    public string CallbackData { get; set; } = string.Empty;

    public TelegramInlineButton() { }

    public TelegramInlineButton(string text, string callbackData)
    {
        Text = text;
        CallbackData = callbackData;
    }
}

public class TelegramTransactionSummary
{
    public int Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public int? CantidadCuotas { get; set; }
    public string? MedioPago { get; set; }
    public int? TarjetaId { get; set; }
}

public class TelegramBotResponse
{
    public long ChatId { get; set; }
    public string Texto { get; set; } = string.Empty;
    public List<List<TelegramInlineButton>>? InlineKeyboard { get; set; }
    public TelegramTransactionSummary? TransaccionCreada { get; set; }
    public bool Ignorado { get; set; } = false;

    public static TelegramBotResponse Ignored() => new() { Ignorado = true };
}
