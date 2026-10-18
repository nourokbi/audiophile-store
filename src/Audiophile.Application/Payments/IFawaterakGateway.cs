using Audiophile.Domain.Entities;

namespace Audiophile.Application.Payments;

// The calls we make to Fawaterak, implemented by FawaterakGateway in Infrastructure
public interface IFawaterakGateway
{
    public const string Provider = "fawaterak";

    // Creates an invoice for the order and returns the payment page the customer is sent to
    Task<FawaterakCheckout> CreateInvoiceAsync(Order order, string customerEmail, CancellationToken ct);

    // Reads Fawaterak's own record of an invoice
    Task<FawaterakInvoice> GetInvoiceAsync(string invoiceId, CancellationToken ct);

    // Checks the webhook signature and returns the paid invoice, or null when there is nothing to apply
    Task<FawaterakInvoice?> ReadPaidInvoiceAsync(string webhookBody, CancellationToken ct);
}

public sealed record FawaterakCheckout(string InvoiceId, string InvoiceKey, string PaymentUrl);

public sealed record FawaterakInvoice(string InvoiceId, Guid? OrderId, decimal Total, bool IsPaid);
