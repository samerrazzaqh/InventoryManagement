using FluentValidation;
using InventoryManagement.Application.Common;
using InventoryManagement.Application.Dtos;

namespace InventoryManagement.Application.Validators;

public sealed class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public sealed class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public sealed class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(50).Matches("^[A-Za-z0-9._-]+$")
            .WithMessage("SKU may contain letters, digits, '.', '_' and '-' only.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0).PrecisionScale(18, 2, false);
        RuleFor(x => x.ReorderLevel).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0).PrecisionScale(18, 2, false);
        RuleFor(x => x.ReorderLevel).GreaterThanOrEqualTo(0);
    }
}

public sealed class ProductFilterValidator : AbstractValidator<ProductFilter>
{
    private static readonly string[] SortFields = ["name", "sku", "price", "createdat"];

    public ProductFilterValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, PagedQuery.MaxPageSize);
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.SortBy).Must(s => s is null || SortFields.Contains(s.ToLowerInvariant()))
            .WithMessage($"SortBy must be one of: {string.Join(", ", SortFields)}.");
        RuleFor(x => x.MinPrice).GreaterThanOrEqualTo(0).When(x => x.MinPrice.HasValue);
        RuleFor(x => x.MaxPrice).GreaterThanOrEqualTo(x => x.MinPrice!.Value)
            .When(x => x.MinPrice.HasValue && x.MaxPrice.HasValue);
    }
}
