using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Audiophile.Application.Common;
using Audiophile.Application.Payments;
using Audiophile.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Audiophile.Infrastructure.Payments.Fawaterak;

// Every HTTP call to Fawaterak, which calls a payment an invoice
public sealed class FawaterakGateway(
    HttpClient http,
    IOptions<FawaterakOptions> options,
    ILogger<FawaterakGateway> logger) : IFawaterakGateway
{
    private const string Currency = "EGP";

    private FawaterakOptions Settings => options.Value;

    public async Task<FawaterakCheckout> CreateInvoiceAsync(Order order, string customerEmail, CancellationToken ct)
    {
        EnsureApiKey();
        if (string.IsNullOrWhiteSpace(Settings.PublicBaseUrl))
            throw new GatewayException("Set Fawaterak:PublicBaseUrl to the ngrok address so Fawaterak can reach the webhook.");
        if (Settings.CardPaymentMethodId <= 0)
            throw new GatewayException("Set Fawaterak:CardPaymentMethodId from GET getPaymentmethods.");

        var publicUrl = Settings.PublicBaseUrl.TrimEnd('/');
        var returnUrl = $"{publicUrl}/payments/fawaterak/return?orderId={order.Id}&result=";
        var nameParts = order.ShippingFullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

        // Each product goes as one line priced at its line total, and shipping as its own line, so the lines add up to the cart total
        var cartItems = order.OrderItems
            .Select(i => new
            {
                name = i.Quantity == 1 ? i.ProductName : $"{i.Quantity} x {i.ProductName}",
                price = FormatAmount(i.LineTotal),
                quantity = "1",
            })
            .ToList();
        if (order.ShippingFee > 0)
            cartItems.Add(new { name = "Shipping", price = FormatAmount(order.ShippingFee), quantity = "1" });

        var body = new
        {
            payment_method_id = Settings.CardPaymentMethodId,
            cartTotal = FormatAmount(order.TotalAmount),
            currency = Currency,
            invoice_number = order.Id.ToString(),
            customer = new
            {
                first_name = nameParts[0],
                last_name = nameParts.Length > 1 ? nameParts[1] : nameParts[0],
                email = customerEmail,
                phone = LocalPhone(order.ShippingPhone),
                address = $"{order.ShippingStreet}, {order.ShippingCity}",
            },
            cartItems,
            redirectionUrls = new
            {
                successUrl = returnUrl + "success",
                failUrl = returnUrl + "fail",
                pendingUrl = returnUrl + "pending",
                webhookUrl = $"{publicUrl}/payments/fawaterak/webhook_json",
            },
            // Fawaterak sends this back in the webhook as pay_load, which is how the webhook finds our order
            payLoad = new { order_id = order.Id.ToString() },
            redirectOption = true,
            lang = "en",
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "invoiceInitPay") { Content = JsonContent.Create(body) };
        var data = await SendAsync(request, "create invoice", ct);

        var invoiceId = ReadString(data, "invoice_id")
            ?? throw new GatewayException("Fawaterak did not return an invoice id.");
        var paymentUrl = data.TryGetProperty("payment_data", out var paymentData) ? ReadString(paymentData, "redirectTo") : null;
        if (paymentUrl is null)
            throw new GatewayException("Fawaterak did not return a payment page URL.");

        logger.LogInformation("Fawaterak invoice {InvoiceId} created for order {OrderId}", invoiceId, order.Id);
        return new FawaterakCheckout(invoiceId, ReadString(data, "invoice_key") ?? invoiceId, paymentUrl);
    }

    public async Task<FawaterakInvoice> GetInvoiceAsync(string invoiceId, CancellationToken ct)
    {
        EnsureApiKey();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"getInvoiceData/{Uri.EscapeDataString(invoiceId)}");
        var data = await SendAsync(request, "invoice lookup", ct);

        decimal.TryParse(ReadString(data, "total"), NumberStyles.Number, CultureInfo.InvariantCulture, out var total);
        return new FawaterakInvoice(
            InvoiceId: ReadString(data, "invoice_id") ?? invoiceId,
            OrderId: ReadOrderId(data),
            Total: total,
            IsPaid: ReadString(data, "paid") is "1" or "true");
    }

    public async Task<FawaterakInvoice?> ReadPaidInvoiceAsync(string webhookBody, CancellationToken ct)
    {
        var webhook = ParseWebhook(webhookBody);
        var invoiceId = ReadString(webhook, "invoice_id") ?? "";

        // Only a paid notice can change an order, failed attempts and refund notices are just acknowledged
        var status = ReadString(webhook, "invoice_status");
        if (!string.Equals(status, "paid", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("Fawaterak webhook for invoice {InvoiceId} with status {Status} needs no action", invoiceId, status);
            return null;
        }

        var expectedHash = FawaterakHash.ForInvoice(
            invoiceId, ReadString(webhook, "invoice_key") ?? "", ReadString(webhook, "payment_method") ?? "", Settings.VendorKey);
        if (string.IsNullOrEmpty(Settings.VendorKey) || !FawaterakHash.Matches(expectedHash, ReadString(webhook, "hashKey")))
        {
            logger.LogWarning("Fawaterak webhook for invoice {InvoiceId} failed the signature check", invoiceId);
            throw new UnauthorizedException("The webhook signature is not valid.");
        }

        // The signature does not cover the amount, so the amount comes from Fawaterak's own record of the invoice
        var invoice = await GetInvoiceAsync(invoiceId, ct);
        if (!invoice.IsPaid)
        {
            logger.LogWarning("The webhook says invoice {InvoiceId} is paid but Fawaterak's record says it is not", invoiceId);
            return null;
        }

        return invoice with { OrderId = invoice.OrderId ?? ReadOrderId(webhook) };
    }

    // Sends the request with the API key and returns the data part of a successful answer
    private async Task<JsonElement> SendAsync(HttpRequestMessage request, string operation, CancellationToken ct)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Settings.ApiKey);

        // Fawaterak rejects requests without a Content-Type header, even GETs, so they get an empty JSON body
        request.Content ??= new StringContent("", Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, ct);
        }
        catch (Exception ex) when (ex is TimeoutRejectedException || (ex is TaskCanceledException && !ct.IsCancellationRequested))
        {
            logger.LogWarning(ex, "Fawaterak {Operation} timed out", operation);
            throw new GatewayTimeoutException(
                $"Fawaterak did not answer the {operation} request in time, check the payment status before trying again.");
        }
        catch (Exception ex) when (ex is HttpRequestException or BrokenCircuitException)
        {
            logger.LogWarning(ex, "Could not reach Fawaterak for {Operation}", operation);
            throw new GatewayException($"Could not reach Fawaterak for {operation}.");
        }

        using (response)
        {
            var content = await response.Content.ReadAsStringAsync(ct);
            var root = TryParseJson(content);

            if (!response.IsSuccessStatusCode
                || ReadString(root, "status") != "success"
                || !root.TryGetProperty("data", out var data))
            {
                logger.LogWarning("Fawaterak {Operation} failed with {Status}: {Body}",
                    operation, (int)response.StatusCode, Truncate(content, 1000));
                throw new GatewayException(
                    $"Fawaterak refused the {operation} request ({(int)response.StatusCode}): {Truncate(content, 300)}");
            }

            return data;
        }
    }

    private void EnsureApiKey()
    {
        if (string.IsNullOrWhiteSpace(Settings.ApiKey))
            throw new GatewayException("Fawaterak is not configured, set Fawaterak:ApiKey with dotnet user-secrets.");
    }

    private static JsonElement ParseWebhook(string body) =>
        TryParseJson(body) is { ValueKind: JsonValueKind.Object } webhook
            ? webhook
            : throw new ArgumentException("The webhook body is not a JSON object.");

    // Returns an element that stays valid after the parsed document is gone, or an undefined element for bad JSON
    private static JsonElement TryParseJson(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return default;
        }
    }

    // Our order id comes back inside pay_load, either as an object or as a JSON string
    private static Guid? ReadOrderId(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty("pay_load", out var payload))
            return null;

        if (payload.ValueKind == JsonValueKind.String)
            payload = TryParseJson(payload.GetString() ?? "");

        return Guid.TryParse(ReadString(payload, "order_id"), out var orderId) ? orderId : null;
    }

    private static string? ReadString(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var value))
            return null;

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null,
        };
    }

    // Fawaterak takes amounts in pounds with two decimals
    private static string FormatAmount(decimal amount) => amount.ToString("0.00", CultureInfo.InvariantCulture);

    // Turns +201012345678 into 01012345678, the local format Fawaterak expects
    private static string LocalPhone(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        return digits.StartsWith("20") && digits.Length == 12 ? "0" + digits[2..] : digits;
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength] + "...";
}
