using System.Globalization;
using System.Text.RegularExpressions;
using Gastito.BusinessLogic.Interfaces;
using Gastito.BusinessLogic.Models.Telegram;
using Gastito.DataAccess.Interfaces;
using Gastito.Entities.Enums;
using Gastito.Entities.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Gastito.BusinessLogic.Services;

public class TelegramBotService : ITelegramBotService
{
    private readonly ITransactionManager _transactionManager;
    private readonly ICategoryStore _categoryStore;
    private readonly ITarjetaStore _tarjetaStore;
    private readonly ITelegramConversacionStore _conversacionStore;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TelegramBotService> _logger;

    public TelegramBotService(
        ITransactionManager transactionManager,
        ICategoryStore categoryStore,
        ITarjetaStore tarjetaStore,
        ITelegramConversacionStore conversacionStore,
        IConfiguration configuration,
        ILogger<TelegramBotService> logger)
    {
        _transactionManager = transactionManager;
        _categoryStore = categoryStore;
        _tarjetaStore = tarjetaStore;
        _conversacionStore = conversacionStore;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<TelegramBotResponse> ProcessUpdateAsync(TelegramUpdate update, CancellationToken ct = default)
    {
        // 1. Determine ChatId and sender
        long chatId;
        string? text = null;
        string? callbackData = null;

        if (update.CallbackQuery != null)
        {
            chatId = update.CallbackQuery.Message?.Chat?.Id ?? update.CallbackQuery.From?.Id ?? 0;
            callbackData = update.CallbackQuery.Data;
        }
        else if (update.Message != null)
        {
            chatId = update.Message.Chat?.Id ?? 0;
            text = update.Message.Text?.Trim();
        }
        else
        {
            return TelegramBotResponse.Ignored();
        }

        if (chatId == 0)
        {
            return TelegramBotResponse.Ignored();
        }

        // 2. Security validation: single authorized user ChatId (Sección 11)
        var authorizedChatIdStr = _configuration["Telegram:AuthorizedChatId"];
        if (long.TryParse(authorizedChatIdStr, out var authorizedChatId) && authorizedChatId > 0)
        {
            if (chatId != authorizedChatId)
            {
                _logger.LogWarning("Telegram update descartado: ChatId {ChatId} no coincide con el autorizado ({AuthorizedChatId}).", chatId, authorizedChatId);
                return TelegramBotResponse.Ignored();
            }
        }

        // 3. Handle Callback Query from buttons
        if (!string.IsNullOrWhiteSpace(callbackData))
        {
            return await HandleCallbackQueryAsync(chatId, callbackData, ct);
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return TelegramBotResponse.Ignored();
        }

        // 4. Handle text commands
        if (text.Equals("/start", StringComparison.OrdinalIgnoreCase) || text.Equals("/help", StringComparison.OrdinalIgnoreCase))
        {
            return BuildHelpResponse(chatId);
        }

        if (text.Equals("/cancelar", StringComparison.OrdinalIgnoreCase) || text.Equals("/reset", StringComparison.OrdinalIgnoreCase))
        {
            await _conversacionStore.ResetAsync(chatId, ct);
            return new TelegramBotResponse
            {
                ChatId = chatId,
                Texto = "❌ Operación cancelada. Escribí /nuevo para iniciar una carga o enviá un texto rápido."
            };
        }

        if (text.Equals("/nuevo", StringComparison.OrdinalIgnoreCase))
        {
            var conv = await _conversacionStore.GetOrCreateAsync(chatId, ct);
            conv.Reset();
            conv.Estado = TelegramEstadoConversacion.EsperandoTipo;
            await _conversacionStore.SaveAsync(conv, ct);

            return new TelegramBotResponse
            {
                ChatId = chatId,
                Texto = "Elija el tipo de movimiento:",
                InlineKeyboard = new List<List<TelegramInlineButton>>
                {
                    new()
                    {
                        new TelegramInlineButton("💵 Ingreso", "tipo:ingreso"),
                        new TelegramInlineButton("💳 Gasto", "tipo:gasto")
                    }
                }
            };
        }

        // 5. Try Direct Text Parsing (Sección 11)
        // Format 1: ingreso | gasto – monto – categoría
        // Format 2: gasto – monto – comentario – cuotas
        var directResult = await TryProcessDirectMessageAsync(chatId, text, ct);
        if (directResult != null)
        {
            return directResult;
        }

        // 6. Conversational Flow (handling text input during active state)
        var conversacion = await _conversacionStore.GetOrCreateAsync(chatId, ct);
        if (conversacion.Estado == TelegramEstadoConversacion.EsperandoMonto)
        {
            if (TryParseMonto(text, out var monto) && monto > 0)
            {
                conversacion.Monto = monto;
                if (conversacion.Tipo == TipoMovimiento.Ingreso)
                {
                    conversacion.Estado = TelegramEstadoConversacion.EsperandoCategoria;
                    await _conversacionStore.SaveAsync(conversacion, ct);
                    return await BuildCategoryButtonsResponseAsync(chatId, TipoMovimiento.Ingreso, ct);
                }
                else
                {
                    conversacion.Estado = TelegramEstadoConversacion.EsperandoMedioPago;
                    await _conversacionStore.SaveAsync(conversacion, ct);
                    return new TelegramBotResponse
                    {
                        ChatId = chatId,
                        Texto = $"Monto: ${monto:N2}.\nSeleccione el medio de pago:",
                        InlineKeyboard = new List<List<TelegramInlineButton>>
                        {
                            new()
                            {
                                new TelegramInlineButton("💵 Efectivo", "medio:efectivo"),
                                new TelegramInlineButton("📲 Transferencia", "medio:transferencia"),
                                new TelegramInlineButton("💳 Tarjeta", "medio:tarjeta")
                            }
                        }
                    };
                }
            }
            else
            {
                return new TelegramBotResponse
                {
                    ChatId = chatId,
                    Texto = "⚠️ Por favor, ingresá un monto numérico válido (ejemplo: 2500 o 15000.50):"
                };
            }
        }
        else if (conversacion.Estado == TelegramEstadoConversacion.EsperandoDescripcion)
        {
            conversacion.Descripcion = text;
            conversacion.Estado = TelegramEstadoConversacion.EsperandoCategoria;
            await _conversacionStore.SaveAsync(conversacion, ct);
            return await BuildCategoryButtonsResponseAsync(chatId, conversacion.Tipo ?? TipoMovimiento.Gasto, ct);
        }

        // Fallback response with help
        return new TelegramBotResponse
        {
            ChatId = chatId,
            Texto = "No entendí el mensaje. Podés escribir /nuevo para iniciar o usar el formato rápido:\n\n" +
                    "• `ingreso 50000 Sueldo`\n" +
                    "• `gasto 12000 Supermercado`\n" +
                    "• `gasto 90000 Zapatillas 3` (en cuotas)"
        };
    }

    private async Task<TelegramBotResponse?> TryProcessDirectMessageAsync(long chatId, string text, CancellationToken ct)
    {
        // Normalize delimiters (replace dashes, pipes with spaces)
        var normalized = Regex.Replace(text, @"[\-–—\|]+", " ").Trim();
        var parts = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 3)
        {
            return null;
        }

        var keyword = parts[0].ToLowerInvariant();
        if (keyword != "ingreso" && keyword != "gasto")
        {
            return null;
        }

        if (!TryParseMonto(parts[1], out var monto) || monto <= 0)
        {
            return null;
        }

        var isGasto = keyword == "gasto";
        var tipo = isGasto ? TipoMovimiento.Gasto : TipoMovimiento.Ingreso;

        // Check format 2 (cuotas): gasto <monto> <comentario> <cuotas>
        // Example: gasto 90000 zapatillas 3
        if (isGasto && parts.Length >= 4 && int.TryParse(parts[^1], out var cuotas) && cuotas > 1)
        {
            var comentario = string.Join(" ", parts.Skip(2).Take(parts.Length - 3));
            var tarjetas = await _tarjetaStore.GetAllAsync(ct);
            var tarjetaCredito = tarjetas.FirstOrDefault(t => t.TipoTarjeta == TipoTarjeta.Credito)
                                 ?? tarjetas.FirstOrDefault();

            if (tarjetaCredito == null)
            {
                // Create a default credit card if none exists
                tarjetaCredito = await _tarjetaStore.CreateAsync(new Tarjeta
                {
                    Nombre = "Tarjeta de Crédito",
                    TipoTarjeta = TipoTarjeta.Credito
                }, ct);
            }

            var categoriasGasto = await _categoryStore.GetAllAsync(TipoMovimiento.Gasto, ct);
            var categoriaMatch = categoriasGasto.FirstOrDefault(c => comentario.Contains(c.Nombre, StringComparison.OrdinalIgnoreCase))
                                 ?? categoriasGasto.FirstOrDefault();

            var txCuotas = new Transaction
            {
                Tipo = TipoMovimiento.Gasto,
                Frecuencia = Frecuencia.Eventual,
                Monto = monto,
                Moneda = Moneda.ARS,
                Fecha = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc),
                Descripcion = comentario,
                CategoryId = categoriaMatch?.Id ?? 1,
                MedioPago = MedioPago.Tarjeta,
                TarjetaId = tarjetaCredito.Id,
                MontoTotal = monto,
                CantidadCuotas = cuotas,
                CuotasPagadas = 0,
                FechaPrimeraCuota = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc)
            };

            var createdCuotas = await _transactionManager.CreateAsync(txCuotas, ct);

            return new TelegramBotResponse
            {
                ChatId = chatId,
                Texto = $"✅ Compra en cuotas registrada con éxito!\n\n" +
                        $"• Tipo: Gasto en cuotas\n" +
                        $"• Monto Total: ${monto:N2}\n" +
                        $"• Cuotas: {cuotas} pagos de ${(monto / cuotas):N2}\n" +
                        $"• Tarjeta: {tarjetaCredito.Nombre}\n" +
                        $"• Categoría: {categoriaMatch?.Nombre ?? "Gasto"}\n" +
                        $"• Descripción: {comentario}",
                TransaccionCreada = ToSummary(createdCuotas)
            };
        }

        // Format 1: ingreso | gasto - monto - categoría
        var categoryOrDesc = string.Join(" ", parts.Skip(2));
        var availableCategories = await _categoryStore.GetAllAsync(tipo, ct);
        var matchedCat = availableCategories.FirstOrDefault(c => c.Nombre.Equals(categoryOrDesc, StringComparison.OrdinalIgnoreCase))
                         ?? availableCategories.FirstOrDefault(c => categoryOrDesc.Contains(c.Nombre, StringComparison.OrdinalIgnoreCase));

        int categoryId;
        string descripcion;

        if (matchedCat != null)
        {
            categoryId = matchedCat.Id;
            descripcion = categoryOrDesc;
        }
        else
        {
            // If category doesn't exist, pick the first of that type and put text in description
            var defaultCat = availableCategories.FirstOrDefault();
            if (defaultCat == null)
            {
                defaultCat = await _categoryStore.CreateAsync(new Category
                {
                    Nombre = tipo == TipoMovimiento.Ingreso ? "Ingreso General" : "Gasto General",
                    Tipo = tipo,
                    EsSistema = false
                }, ct);
            }
            categoryId = defaultCat.Id;
            descripcion = categoryOrDesc;
        }

        var tx = new Transaction
        {
            Tipo = tipo,
            Frecuencia = Frecuencia.Eventual,
            Monto = monto,
            Moneda = Moneda.ARS,
            Fecha = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc),
            Descripcion = descripcion,
            CategoryId = categoryId,
            MedioPago = isGasto ? MedioPago.Efectivo : null
        };

        var created = await _transactionManager.CreateAsync(tx, ct);

        return new TelegramBotResponse
        {
            ChatId = chatId,
            Texto = $"✅ Movimiento registrado con éxito!\n\n" +
                    $"• Tipo: {(tipo == TipoMovimiento.Ingreso ? "Ingreso" : "Gasto")}\n" +
                    $"• Monto: ${monto:N2}\n" +
                    $"• Categoría: {matchedCat?.Nombre ?? "General"}\n" +
                    $"• Descripción: {descripcion}" +
                    (isGasto ? "\n• Medio de pago: Efectivo" : ""),
            TransaccionCreada = ToSummary(created)
        };
    }

    private async Task<TelegramBotResponse> HandleCallbackQueryAsync(long chatId, string data, CancellationToken ct)
    {
        var conv = await _conversacionStore.GetOrCreateAsync(chatId, ct);

        if (data.StartsWith("tipo:"))
        {
            var tipoStr = data["tipo:".Length..];
            conv.Tipo = tipoStr == "ingreso" ? TipoMovimiento.Ingreso : TipoMovimiento.Gasto;
            conv.Estado = TelegramEstadoConversacion.EsperandoMonto;
            await _conversacionStore.SaveAsync(conv, ct);

            return new TelegramBotResponse
            {
                ChatId = chatId,
                Texto = $"Seleccionaste {(conv.Tipo == TipoMovimiento.Ingreso ? "Ingreso" : "Gasto")}.\nPor favor, escribí el monto:"
            };
        }

        if (data.StartsWith("medio:"))
        {
            var medioStr = data["medio:".Length..];
            if (medioStr == "efectivo")
            {
                conv.MedioPago = MedioPago.Efectivo;
                conv.Estado = TelegramEstadoConversacion.EsperandoCategoria;
                await _conversacionStore.SaveAsync(conv, ct);
                return await BuildCategoryButtonsResponseAsync(chatId, TipoMovimiento.Gasto, ct);
            }
            else if (medioStr == "transferencia")
            {
                conv.MedioPago = MedioPago.Transferencia;
                conv.Estado = TelegramEstadoConversacion.EsperandoCategoria;
                await _conversacionStore.SaveAsync(conv, ct);
                return await BuildCategoryButtonsResponseAsync(chatId, TipoMovimiento.Gasto, ct);
            }
            else if (medioStr == "tarjeta")
            {
                conv.MedioPago = MedioPago.Tarjeta;
                conv.Estado = TelegramEstadoConversacion.EsperandoTarjeta;
                await _conversacionStore.SaveAsync(conv, ct);

                var tarjetas = await _tarjetaStore.GetAllAsync(ct);
                if (tarjetas.Count == 0)
                {
                    // Create a default card
                    var defaultCard = await _tarjetaStore.CreateAsync(new Tarjeta
                    {
                        Nombre = "Tarjeta Principal",
                        TipoTarjeta = TipoTarjeta.Debito
                    }, ct);
                    tarjetas.Add(defaultCard);
                }

                var buttons = tarjetas.Select(t => new List<TelegramInlineButton>
                {
                    new($"{t.Nombre} ({t.TipoTarjeta})", $"tarjeta:{t.Id}")
                }).ToList();

                return new TelegramBotResponse
                {
                    ChatId = chatId,
                    Texto = "Seleccione la tarjeta:",
                    InlineKeyboard = buttons
                };
            }
        }

        if (data.StartsWith("tarjeta:"))
        {
            var tarjetaId = int.Parse(data["tarjeta:".Length..]);
            conv.TarjetaId = tarjetaId;
            var tarjeta = await _tarjetaStore.GetByIdAsync(tarjetaId, ct);

            if (tarjeta != null && tarjeta.TipoTarjeta == TipoTarjeta.Credito)
            {
                conv.Estado = TelegramEstadoConversacion.EsperandoCuotas;
                await _conversacionStore.SaveAsync(conv, ct);

                return new TelegramBotResponse
                {
                    ChatId = chatId,
                    Texto = "¿En cuántas cuotas se realizó la compra?",
                    InlineKeyboard = new List<List<TelegramInlineButton>>
                    {
                        new()
                        {
                            new TelegramInlineButton("1 cuota", "cuotas:1"),
                            new TelegramInlineButton("3 cuotas", "cuotas:3")
                        },
                        new()
                        {
                            new TelegramInlineButton("6 cuotas", "cuotas:6"),
                            new TelegramInlineButton("12 cuotas", "cuotas:12")
                        }
                    }
                };
            }
            else
            {
                conv.Estado = TelegramEstadoConversacion.EsperandoCategoria;
                await _conversacionStore.SaveAsync(conv, ct);
                return await BuildCategoryButtonsResponseAsync(chatId, TipoMovimiento.Gasto, ct);
            }
        }

        if (data.StartsWith("cuotas:"))
        {
            var cuotas = int.Parse(data["cuotas:".Length..]);
            conv.CantidadCuotas = cuotas;
            conv.Estado = TelegramEstadoConversacion.EsperandoCategoria;
            await _conversacionStore.SaveAsync(conv, ct);
            return await BuildCategoryButtonsResponseAsync(chatId, TipoMovimiento.Gasto, ct);
        }

        if (data.StartsWith("cat:"))
        {
            var catId = int.Parse(data["cat:".Length..]);
            conv.CategoryId = catId;

            // Finalize transaction
            var category = await _categoryStore.GetByIdAsync(catId, ct);
            var isCuotas = (conv.CantidadCuotas ?? 1) > 1;

            var tx = new Transaction
            {
                Tipo = conv.Tipo ?? TipoMovimiento.Gasto,
                Frecuencia = Frecuencia.Eventual,
                Monto = conv.Monto ?? 0,
                Moneda = Moneda.ARS,
                Fecha = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc),
                Descripcion = conv.Descripcion ?? $"{conv.Tipo} en {category?.Nombre}",
                CategoryId = catId,
                MedioPago = conv.Tipo == TipoMovimiento.Gasto ? (conv.MedioPago ?? MedioPago.Efectivo) : null,
                TarjetaId = conv.TarjetaId
            };

            if (isCuotas)
            {
                tx.MontoTotal = conv.Monto;
                tx.CantidadCuotas = conv.CantidadCuotas;
                tx.CuotasPagadas = 0;
                tx.FechaPrimeraCuota = tx.Fecha;
            }

            var created = await _transactionManager.CreateAsync(tx, ct);
            await _conversacionStore.ResetAsync(chatId, ct);

            return new TelegramBotResponse
            {
                ChatId = chatId,
                Texto = $"🎉 Movimiento guardado exitosamente!\n\n" +
                        $"• Tipo: {created.Tipo}\n" +
                        $"• Monto: ${created.Monto:N2}\n" +
                        $"• Categoría: {category?.Nombre}\n" +
                        (created.MedioPago != null ? $"• Medio de pago: {created.MedioPago}\n" : "") +
                        (isCuotas ? $"• Cuotas: {created.CantidadCuotas}\n" : ""),
                TransaccionCreada = ToSummary(created)
            };
        }

        return TelegramBotResponse.Ignored();
    }

    private async Task<TelegramBotResponse> BuildCategoryButtonsResponseAsync(long chatId, TipoMovimiento tipo, CancellationToken ct)
    {
        var categories = await _categoryStore.GetAllAsync(tipo, ct);
        var buttons = categories.Select(c => new List<TelegramInlineButton>
        {
            new(c.Nombre, $"cat:{c.Id}")
        }).ToList();

        return new TelegramBotResponse
        {
            ChatId = chatId,
            Texto = "Seleccione la categoría:",
            InlineKeyboard = buttons
        };
    }

    private static TelegramBotResponse BuildHelpResponse(long chatId)
    {
        return new TelegramBotResponse
        {
            ChatId = chatId,
            Texto = "🤖 *¡Hola! Soy GASTITO Bot.*\n\n" +
                    "Podés registrar tus movimientos rápidamente enviando un mensaje con estos formatos:\n\n" +
                    "💰 *Ingreso:*\n" +
                    "`ingreso 50000 Sueldo`\n" +
                    "`ingreso - 20000 - Venta`\n\n" +
                    "🛒 *Gasto:* \n" +
                    "`gasto 1500 Supermercado`\n" +
                    "`gasto - 4500 - Almuerzo`\n\n" +
                    "💳 *Gasto en cuotas (tarjeta):*\n" +
                    "`gasto 120000 Heladera 6`\n\n" +
                    "O enviá /nuevo para registrar con botones paso a paso."
        };
    }

    private static bool TryParseMonto(string str, out decimal monto)
    {
        // Clean currency symbols and whitespace
        var clean = str.Replace("$", "").Replace(" ", "").Trim();
        // Support either comma or dot
        clean = clean.Replace(',', '.');

        return decimal.TryParse(clean, NumberStyles.Number | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out monto);
    }

    private static TelegramTransactionSummary ToSummary(Transaction tx)
    {
        return new TelegramTransactionSummary
        {
            Id = tx.Id,
            Tipo = tx.Tipo.ToString(),
            Monto = tx.Monto,
            Descripcion = tx.Descripcion,
            CategoryId = tx.CategoryId,
            CantidadCuotas = tx.CantidadCuotas,
            MedioPago = tx.MedioPago?.ToString(),
            TarjetaId = tx.TarjetaId
        };
    }
}
