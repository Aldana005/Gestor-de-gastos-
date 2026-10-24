using Gastito.BusinessLogic.Models.Telegram;

namespace Gastito.BusinessLogic.Interfaces;

public interface ITelegramBotService
{
    Task<TelegramBotResponse> ProcessUpdateAsync(TelegramUpdate update, CancellationToken ct = default);
}
