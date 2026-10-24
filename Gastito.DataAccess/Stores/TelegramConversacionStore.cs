using System.Collections.Concurrent;
using Gastito.DataAccess.Interfaces;
using Gastito.Entities.Models;

namespace Gastito.DataAccess.Stores;

public class TelegramConversacionStore : ITelegramConversacionStore
{
    private static readonly ConcurrentDictionary<long, TelegramConversacion> _conversaciones = new();

    public Task<TelegramConversacion> GetOrCreateAsync(long chatId, CancellationToken ct = default)
    {
        var conv = _conversaciones.GetOrAdd(chatId, id => new TelegramConversacion { ChatId = id });
        return Task.FromResult(conv);
    }

    public Task SaveAsync(TelegramConversacion conversacion, CancellationToken ct = default)
    {
        conversacion.UltimaActualizacionUtc = DateTime.UtcNow;
        _conversaciones[conversacion.ChatId] = conversacion;
        return Task.CompletedTask;
    }

    public Task ResetAsync(long chatId, CancellationToken ct = default)
    {
        if (_conversaciones.TryGetValue(chatId, out var conv))
        {
            conv.Reset();
        }
        return Task.CompletedTask;
    }
}
