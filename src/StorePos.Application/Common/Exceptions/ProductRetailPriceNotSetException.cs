namespace StorePos.Application.Common.Exceptions;

public sealed class ProductRetailPriceNotSetException(string productName)
    : Exception(
        $"პროდუქტს „{productName}“ საცალო ფასი არ აქვს მითითებული. " +
        "გაყიდვამდე მიუთითეთ ფასი.");
