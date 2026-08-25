using StorePos.Desktop.Sales.Models;

namespace StorePos.Desktop.Products.ViewModels;

public sealed class ProductAddedEventArgs(AddProductSaleItemResponse result) : EventArgs
{
    public AddProductSaleItemResponse Result { get; } = result;
}
