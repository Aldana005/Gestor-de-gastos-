using Gastito.Entities.Models;

namespace Gastito.DataAccess.Interfaces;

public interface ITelegramConversacionStore
{
    Task<TelegramConversacion> GetOrCreateAsync(long chatId, CancellationToken ct = default);
    Task SaveAsync(TelegramConversacion conversacion, CancellationToken ct = default);
    Task ResetAsync(long chatId, CancellationToken ct = default);
}
