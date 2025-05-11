using OnlineMarket.Core.SellerLibrary.Models;

namespace OnlineMarket.Core.SellerLibrary.DTO;

public record SellerDashboard
(
    // the materialized view
    // the aggregate part
    OrderSellerView sellerView,
    // simple query the seller open order entries
    // the detailed part
    IList<OrderEntry> orderEntries
);
