using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace OnlineMarket.Core.PaymentLibrary.Models;

[Table("order_payment_cards", Schema = "payment")]
[PrimaryKey(nameof(customer_id), nameof(order_id), nameof(sequential))]
public class OrderPaymentCardModel
{
    public int customer_id { get; set; }

    public int order_id { get; set; }

    public int sequential { get; set; }

    // card info coming from customer checkout
    public string card_number { get; set; } = "";

    public string card_holder_name { get; set; } = "";

    public DateTime card_expiration { get; set; }

    // for privacy issues, we assume this is not stored, only carried out in the event
    // public string card_security_number { get; set; }

    public string card_brand { get; set; } = "";

    [ForeignKey("customer_id, order_id, sequential")]
    public virtual OrderPaymentModel orderPayment { get; set; }

    public OrderPaymentCardModel(){ }

}


