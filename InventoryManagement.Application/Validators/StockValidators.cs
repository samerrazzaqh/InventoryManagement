using FluentValidation;
using InventoryManagement.Application.Common;
using InventoryManagement.Application.Dtos;

namespace InventoryManagement.Application.Validators;

public sealed class StockMovementRequestValidator : AbstractValidator<StockMovementRequest>
{
    public StockMovementRequestValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.Quantity).InclusiveBetween(1, 1_000_000);
        RuleFor(x => x.ReferenceNumber).MaximumLength(50);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public sealed class StockTransferRequestValidator : AbstractValidator<StockTransferRequest>
{
    public StockTransferRequestValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.SourceWarehouseId).NotEmpty();
        RuleFor(x => x.DestinationWarehouseId).NotEmpty().NotEqual(x => x.SourceWarehouseId)
            .WithMessage("Destination warehouse must differ from the source warehouse.");
        RuleFor(x => x.Quantity).InclusiveBetween(1, 1_000_000);
        RuleFor(x => x.ReferenceNumber).MaximumLength(50);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public sealed class PagedQueryValidator : AbstractValidator<PagedQuery>
{
    public PagedQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, PagedQuery.MaxPageSize);
        RuleFor(x => x.Search).MaximumLength(100);
    }
}
