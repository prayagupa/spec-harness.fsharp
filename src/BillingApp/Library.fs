namespace BillingApp

type LineItem =
    { Description: string
      Quantity: int
      UnitPrice: decimal }

module InvoiceTotals =
    let Subtotal (items: LineItem list) : decimal =
        items |> List.sumBy (fun item -> decimal item.Quantity * item.UnitPrice)

    let TaxAmount (subtotal: decimal) (taxRate: decimal) : decimal =
        subtotal * taxRate

    let GrandTotal (subtotal: decimal) (tax: decimal) : decimal =
        subtotal + tax

module InvoiceFormatter =
    let FormatInvoiceNumber (invoiceId: int) : string =
        sprintf "INV-%06d" invoiceId

    let FormatAmount (amount: decimal) : string =
        sprintf "$%.2f" amount
