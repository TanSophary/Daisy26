using Microsoft.EntityFrameworkCore;
using OnlineShop.Data;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OnlineShop.Services
{
    /// <summary>
    /// Background service that polls Telegram getUpdates every 2 seconds.
    /// No webhook / ngrok / public domain needed — works on localhost.
    /// When a customer clicks the bot link and types /start {customerId},
    /// this service saves their TelegramChatId to the database automatically.
    /// </summary>
    public class TelegramPollingService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<TelegramPollingService> _logger;
        private readonly HttpClient _http;
        private long _offset = 0;

        public TelegramPollingService(
            IServiceScopeFactory scopeFactory,
            IConfiguration config,
            ILogger<TelegramPollingService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _scopeFactory = scopeFactory;
            _config = config;
            _logger = logger;
            _http = httpClientFactory.CreateClient("telegram");
            _http.Timeout = TimeSpan.FromSeconds(35);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var botToken = _config["Telegram:BotToken"] ?? "";
            if (string.IsNullOrEmpty(botToken))
            {
                _logger.LogWarning("TelegramPollingService: BotToken is empty, polling disabled.");
                return;
            }

            // First: delete any existing webhook so polling works
            try
            {
                await _http.GetAsync(
                    $"https://api.telegram.org/bot{botToken}/deleteWebhook?drop_pending_updates=false",
                    stoppingToken);
                _logger.LogInformation("TelegramPollingService: webhook cleared, polling started.");
            }
            catch { }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var url = $"https://api.telegram.org/bot{botToken}/getUpdates" +
                              $"?offset={_offset}&timeout=30&allowed_updates=[\"message\"]";

                    var response = await _http.GetAsync(url, stoppingToken);
                    if (!response.IsSuccessStatusCode)
                    {
                        await Task.Delay(5000, stoppingToken);
                        continue;
                    }

                    var json = await response.Content.ReadAsStringAsync(stoppingToken);
                    var result = JsonSerializer.Deserialize<TelegramResponse>(json);

                    if (result?.Ok == true && result.Updates?.Length > 0)
                    {
                        foreach (var update in result.Updates)
                        {
                            _offset = update.UpdateId + 1;
                            await HandleUpdate(update, botToken, stoppingToken);
                        }
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "TelegramPollingService error");
                    await Task.Delay(5000, stoppingToken);
                }
            }
        }

        private async Task HandleUpdate(TelegramUpdate update, string botToken, CancellationToken ct)
        {
            var text   = update.Message?.Text ?? "";
            var chatId = update.Message?.Chat?.Id.ToString() ?? "";

            if (string.IsNullOrEmpty(chatId) || !text.StartsWith("/start"))
                return;

            var parts = text.Split(' ');

            // /start {customerId}  — link account
            if (parts.Length >= 2 && int.TryParse(parts[1], out int customerId))
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var customer = await db.Customers.FindAsync(new object[] { customerId }, ct);
                if (customer != null)
                {
                    customer.TelegramChatId = chatId;
                    await db.SaveChangesAsync(ct);
                    _logger.LogInformation("Linked customer {Id} to Telegram chatId {ChatId}", customerId, chatId);

                    await SendMessage(botToken, chatId,
                        $"✅ សួស្ដី <b>{customer.FullName}</b>!\n\nគណនីរបស់អ្នកត្រូវបានភ្ជាប់ជាមួយ Telegram ហើយ។\nអ្នកនឹងទទួលបានការអប់ arn ការបញ្ជាទិញ និងស្ថានភាព ✉️ 🎉");
                }
                else
                {
                    await SendMessage(botToken, chatId,
                        "❌ រកមិនឃើញគណនី។ សូមព្យាយាមម្ដងទៀត។");
                }
            }
            else
            {
                // /start with no ID — generic welcome
                await SendMessage(botToken, chatId,
                    "👋 Welcome to <b>Pink Daisy Shop</b>!\n\nTo receive order updates, please place an order and click <b>'Connect Telegram'</b> on the confirmation page.");
            }
        }

        private async Task SendMessage(string botToken, string chatId, string text)
        {
            try
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("chat_id", chatId),
                    new KeyValuePair<string, string>("text", text),
                    new KeyValuePair<string, string>("parse_mode", "HTML"),
                });
                await _http.PostAsync(
                    $"https://api.telegram.org/bot{botToken}/sendMessage", content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send Telegram message to {ChatId}", chatId);
            }
        }
    }

    // ── JSON models ──────────────────────────────────────────────────────────
    public class TelegramResponse
    {
        [JsonPropertyName("ok")]      public bool Ok { get; set; }
        [JsonPropertyName("result")]  public TelegramUpdate[]? Updates { get; set; }
    }

    public class TelegramUpdate
    {
        [JsonPropertyName("update_id")] public long UpdateId { get; set; }
        [JsonPropertyName("message")]   public TelegramMessage? Message { get; set; }
    }

    public class TelegramMessage
    {
        [JsonPropertyName("text")] public string? Text { get; set; }
        [JsonPropertyName("chat")] public TelegramChat? Chat { get; set; }
    }

    public class TelegramChat
    {
        [JsonPropertyName("id")] public long Id { get; set; }
    }
}
