using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Audiophile.Application.Common;
using Audiophile.Application.Payments;
using Audiophile.Domain.Enums;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Audiophile.Infrastructure.Payments.Fawaterak;

// Everything that talks to Fawaterak. Fawaterak calls a payment an "invoice": we
// create one per order, send the customer to its hosted page, and learn the result
// from a signed webhook. Nothing outside this folder knows Fawaterak's field names.
public sealed class FawaterakGateway(
    HttpClient http,
    IOptions<FawaterakOptions> options,
    ILogger<FawaterakGateway> logger) : IFawaterakGateway
{
    private FawaterakOptions Options => options.Value;

    // ---------------------------------------------------------------- checkout

    public async Task<CheckoutSession> CreateCheckoutAsync(CheckoutRequest request, CancellationToken ct)
    {
        EnsureConfigured();
        var orderId = request.MerchantReference;
        var publicBase = Options.PublicBaseUrl.TrimEnd('/');
        string ReturnUrl(string result) => $"{publicBase}/payments/fawaterak/return?orderId={orderId}&result={result}";

        var body = new Dictionary<string, object?>
        {
            ["payment_method_id"] = PaymentMethodIdFor(request.Method),
            ["cartTotal"] = Money(request.AmountMinor),     // Fawaterak takes pounds, not piasters
            ["currency"] = request.Currency,
            ["invoice_number"] = orderId,
            ["customer"] = new
            {
                first_name = request.Customer.FirstName,
                last_name = request.Customer.LastName,
                email = request.Customer.Email,
                phone = LocalPhone(request.Customer.Phone),
                address = $"{request.Customer.Street}, {request.Customer.City}",
            },
            // One item per order line at quantity 1, so the items always add up to cartTotal.
            ["cartItems"] = request.Items.Select(i => new
            {
                name = i.Quantity == 1 ? i.Name : $"{i.Quantity} x {i.Name}",
                price = Money(i.UnitAmountMinor * i.Quantity),
                quantity = "1",
            }),
            ["redirectionUrls"] = new
            {
                successUrl = ReturnUrl("success"),
                failUrl = ReturnUrl("fail"),
                pendingUrl = ReturnUrl("pending"),
                // "_json" in the path tells Fawaterak to send JSON, not form data.
                webhookUrl = $"{publicBase}/payments/fawaterak/webhook_json",
            },
            // Echoed back in the webhook as pay_load: how the webhook finds our order.
            ["payLoad"] = new { order_id = orderId },
            // Always return a hosted page, even for Aman / Fawry, where it shows the code.
            ["redirectOption"] = true,
            ["lang"] = "en",
        };

        if (request.Method == PaymentMethod.VodafoneCash)
            body["mobileWalletNumber"] = LocalPhone(request.Customer.Phone);
        if (request.Method == PaymentMethod.Kiosk)
            body["customExpireDate"] = "+2 days";           // matches our 48-hour kiosk window

        using var message = new HttpRequestMessage(HttpMethod.Post, "invoiceInitPay") { Content = JsonContent.Create(body) };
        using var doc = await SendAsync(message, "create invoice", ct);
        var data = Data(doc.RootElement, "create invoice");

        var invoiceId = Str(data, "invoice_id") ?? throw new GatewayException("Fawaterak did not return an invoice id.");
        var invoiceKey = Str(data, "invoice_key") ?? invoiceId;

        data.TryGetProperty("payment_data", out var paymentData);
        var code = Str(paymentData, "amanCode") ?? Str(paymentData, "fawryCode")
                   ?? Str(paymentData, "masaryCode") ?? Str(paymentData, "meezaReference");
        var checkoutUrl = Str(paymentData, "redirectTo") ?? ReturnUrl("pending");

        logger.LogInformation("Fawaterak invoice {InvoiceId} created for order {OrderId}", invoiceId, orderId);
        return new CheckoutSession(invoiceKey, invoiceId, checkoutUrl, code);
    }

    // ---------------------------------------------------------------- inquiry

    public async Task<GatewayTransaction> GetInvoiceAsync(string invoiceId, CancellationToken ct)
    {
        EnsureConfigured();
        using var message = new HttpRequestMessage(HttpMethod.Get, $"getInvoiceData/{Uri.EscapeDataString(invoiceId)}");
        using var doc = await SendAsync(message, "invoice lookup", ct);
        var data = Data(doc.RootElement, "invoice lookup");

        var paid = Str(data, "paid") is "1" or "true";
        var total = decimal.TryParse(Str(data, "total"), NumberStyles.Number, CultureInfo.InvariantCulture, out var t) ? t : 0m;

        return new GatewayTransaction(
            TransactionId: Str(data, "invoice_id") ?? invoiceId,
            ProviderOrderId: Str(data, "invoice_id") ?? invoiceId,
            MerchantReference: OrderIdFromPayload(data),
            AmountMinor: (long)Math.Round(total * 100m),
            Currency: Str(data, "currency") ?? "",
            Success: paid,
            Pending: !paid,
            SourceType: Str(data, "payment_method"));
    }

    // -------------------------------------------------------------- callbacks

    public async Task<GatewayCallback> ParseWebhookAsync(string rawBody, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(Options.VendorKey))
        {
            logger.LogError("Fawaterak:VendorKey is not configured; rejecting every webhook");
            return GatewayCallback.Rejected;
        }

        using var doc = ToJson(rawBody);
        if (doc is null)
            return GatewayCallback.Rejected;
        var root = doc.RootElement;

        // 1. Paid. The body says which invoice was paid but not how much, so after the
        //    signature checks out we ask Fawaterak for the invoice and use its numbers.
        if (string.Equals(Str(root, "invoice_status"), "paid", StringComparison.OrdinalIgnoreCase))
        {
            var invoiceId = Str(root, "invoice_id") ?? "";
            var expected = FawaterakHash.ForInvoice(
                invoiceId, Str(root, "invoice_key") ?? "", Str(root, "payment_method") ?? "", Options.VendorKey);
            if (!FawaterakHash.Matches(expected, Str(root, "hashKey")))
                return Reject("paid");

            var confirmed = await GetInvoiceAsync(invoiceId, ct);
            if (!confirmed.Success)
            {
                logger.LogWarning("Webhook says invoice {InvoiceId} is paid but Fawaterak's lookup disagrees", invoiceId);
                return new GatewayCallback(true, "paid-unconfirmed", null);
            }

            return new GatewayCallback(true, "paid",
                confirmed with { MerchantReference = confirmed.MerchantReference ?? OrderIdFromPayload(root) });
        }

        // 2. An Aman / Fawry / Masary code expired unpaid.
        if (string.Equals(Str(root, "status"), "EXPIRED", StringComparison.OrdinalIgnoreCase))
        {
            var expected = FawaterakHash.ForExpiredReference(
                Str(root, "referenceId") ?? "", Str(root, "paymentMethod") ?? "", Options.VendorKey);
            if (!FawaterakHash.Matches(expected, Str(root, "hashKey")))
                return Reject("expired");

            return new GatewayCallback(true, "expired", new GatewayTransaction(
                TransactionId: Str(root, "transactionId") ?? Str(root, "referenceId") ?? "",
                ProviderOrderId: null,
                MerchantReference: OrderIdFromPayload(root),
                AmountMinor: 0, Currency: "", Success: false, Pending: false,
                SourceType: Str(root, "paymentMethod")));
        }

        // 3. A failed attempt. Informational only: the customer can try again.
        if (Str(root, "errorMessage") is { } error)
        {
            logger.LogInformation("Fawaterak payment attempt failed for invoice {InvoiceId}: {Error}",
                Str(root, "invoice_id"), error);
            return new GatewayCallback(true, "failed", null);
        }

        // 4. Refund notice. The documented body carries no signature, so it cannot be
        //    trusted to change anything; log it for reconciliation.
        if (Str(root, "transactionId") is not null && Str(root, "reason") is not null)
        {
            logger.LogWarning("Unsigned Fawaterak refund notice for transaction {TransactionId}, amount {Amount}. " +
                              "Not applied automatically; reconcile against the dashboard.",
                Str(root, "transactionId"), Str(root, "amount"));
            return new GatewayCallback(true, "refund-notice", null);
        }

        return Reject("unknown");
    }

    // ---------------------------------------------------------------- helpers

    private GatewayCallback Reject(string kind)
    {
        logger.LogWarning("Fawaterak {Kind} webhook failed signature verification", kind);
        return GatewayCallback.Rejected;
    }

    // Webhooks arrive as JSON when the URL contains "_json", otherwise as form fields.
    private static JsonDocument? ToJson(string rawBody)
    {
        try
        {
            return JsonDocument.Parse(rawBody);
        }
        catch (JsonException)
        {
            var form = QueryHelpers.ParseQuery(rawBody);
            if (form.Count == 0)
                return null;
            return JsonSerializer.SerializeToDocument(form.ToDictionary(f => f.Key, f => f.Value.ToString()));
        }
    }

    // pay_load is our { order_id } object, echoed back either as an object or as a JSON string.
    private static string? OrderIdFromPayload(JsonElement element)
    {
        if (!element.TryGetProperty("pay_load", out var payload))
            return null;

        if (payload.ValueKind == JsonValueKind.String)
        {
            try
            {
                using var inner = JsonDocument.Parse(payload.GetString() ?? "");
                return Str(inner.RootElement, "order_id");
            }
            catch (JsonException)
            {
                return null;
            }
        }

        return payload.ValueKind == JsonValueKind.Object ? Str(payload, "order_id") : null;
    }

    private static JsonElement Data(JsonElement root, string operation)
    {
        if (Str(root, "status") != "success" || !root.TryGetProperty("data", out var data))
            throw new GatewayException(
                $"Fawaterak refused the {operation} request: {Truncate(root.GetRawText(), 300)}");
        return data;
    }

    // Turns every failure into one of two answers: a clear "no" (GatewayException) or "outcome unknown" (GatewayTimeoutException).
    private async Task<JsonDocument> SendAsync(HttpRequestMessage message, string operation, CancellationToken ct)
    {
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Options.ApiKey);

        // Fawaterak rejects any request without a Content-Type header, GETs included
        // ("The content-type field is required"). .NET only sends Content-Type along
        // with a body, so GETs carry an empty JSON body to get the header out.
        message.Content ??= new StringContent("", System.Text.Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(message, ct);
        }
        catch (Exception ex) when (ex is TimeoutRejectedException
                                   || (ex is TaskCanceledException && !ct.IsCancellationRequested))
        {
            logger.LogWarning(ex, "Fawaterak {Operation} timed out", operation);
            throw new GatewayTimeoutException(
                $"Fawaterak did not answer the {operation} request in time. The outcome is unknown; " +
                "check the payment status before trying again.");
        }
        catch (BrokenCircuitException ex)
        {
            throw new GatewayException($"Fawaterak is failing repeatedly; {operation} was not attempted. {ex.Message}");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Fawaterak {Operation} could not be sent", operation);
            throw new GatewayException($"Could not reach Fawaterak for {operation}.");
        }

        using (response)
        {
            var content = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Fawaterak {Operation} failed with {Status}: {Body}",
                    operation, (int)response.StatusCode, Truncate(content, 1000));
                throw new GatewayException(
                    $"Fawaterak rejected the {operation} request ({(int)response.StatusCode}): {Truncate(content, 300)}");
            }

            try
            {
                return JsonDocument.Parse(content);
            }
            catch (JsonException)
            {
                throw new GatewayException($"Fawaterak returned an unreadable {operation} response.");
            }
        }
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(Options.ApiKey))
            throw new GatewayException("Fawaterak is not configured. Set Fawaterak:ApiKey with dotnet user-secrets.");
        if (string.IsNullOrWhiteSpace(Options.PublicBaseUrl))
            throw new GatewayException("Set Fawaterak:PublicBaseUrl to the ngrok address so Fawaterak can reach the webhook.");
    }

    private int PaymentMethodIdFor(PaymentMethod method)
    {
        var id = method switch
        {
            PaymentMethod.Card => Options.CardPaymentMethodId,
            PaymentMethod.VodafoneCash => Options.WalletPaymentMethodId,
            PaymentMethod.Kiosk => Options.KioskPaymentMethodId,
            _ => 0,
        };

        return id > 0 ? id : throw new GatewayException($"No Fawaterak payment method id is configured for {method}.");
    }

    private static string Money(long minor) => (minor / 100m).ToString("0.00", CultureInfo.InvariantCulture);

    // +201012345678 → 01012345678, the local format Fawaterak expects.
    private static string LocalPhone(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        return digits.StartsWith("20") && digits.Length == 12 ? "0" + digits[2..] : digits;
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max] + "…";

    private static string? Str(JsonElement element, string name)
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
}
