using InventoryManagement.Application.Dtos;
using InventoryManagement.Domain.Entities;
using Mapster;

namespace InventoryManagement.Application.Mapping;

/// <summary>Discovered via TypeAdapterConfig.Scan(). Mapster is source-gen/compile-friendly and MIT-licensed.</summary>
public sealed class MappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Category, CategoryDto>();

        config.NewConfig<Product, ProductDto>()
            .Map(d => d.CategoryName, s => s.Category.Name)
            .Map(d => d.SupplierName, s => s.Supplier.Name);

        config.NewConfig<StockTransaction, StockTransactionDto>();
    }
}
