using OnlineMarket.Core.CartLibrary.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace OnlineMarket.Core.CartLibrary.Repositories;

public interface ICartRepository
{
    CartModel? GetCart(int customerId);

    IList<CartItemModel> GetItems(int customerId);

    IList<CartItemModel> GetItemsByProduct(int sellerId, int productId, string version);

    CartItemModel AddItem(CartItemModel item);

    CartModel? Delete(int customerId);

    void Update(CartModel cart);

    void Insert(CartModel cart);

    CartItemModel UpdateItem(CartItemModel item);

    // APIs for CartService
    IDbContextTransaction BeginTransaction();
    void FlushUpdates();
    void Cleanup();
    void Reset();
}