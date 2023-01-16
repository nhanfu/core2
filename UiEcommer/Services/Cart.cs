using Blazored.LocalStorage;
using UiEcommer.Models;

namespace UiEcommer.Services
{
    public class Cart
    {
        public ILocalStorageService _syncLocalStorageService;
        public Order Order { get; set; }
        public Cart(ILocalStorageService syncLocalStorageService)
        {
            _syncLocalStorageService = syncLocalStorageService;
            Task.Run(async () =>
            {
                Order = await _syncLocalStorageService.GetItemAsync<Order>("Order") ?? new Order();
            });
        }

        public async Task AddProductAsync(Product product)
        {
            var isAdd = false;
            Order.OrderDetail.ForEach(x =>
            {
                if (x.ProductId == product.Id)
                {
                    x.Quantity++;
                    isAdd = true;
                }
            });
            if (!isAdd)
            {
                Order.OrderDetail.Add(new OrderDetail()
                {
                    ProductId = product.Id,
                    MainAvatar = product.Avatar,
                });
            }
            await _syncLocalStorageService.SetItemAsync("Order", Order);
        }
    }
}
