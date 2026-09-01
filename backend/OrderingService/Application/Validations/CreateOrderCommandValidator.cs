using FluentValidation;
using OrderingService.Application.Commands;
using OrderingService.Domain.AggregatesModel.BuyerAggregate;

namespace OrderingService.Application.Validations;

/// <summary>
/// Guards the checkout payload before an Order aggregate is ever constructed.
/// </summary>
public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(command => command.City).NotEmpty();
        RuleFor(command => command.Street).NotEmpty();
        RuleFor(command => command.State).NotEmpty();
        RuleFor(command => command.Country).NotEmpty();
        RuleFor(command => command.ZipCode).NotEmpty();
        RuleFor(command => command.OrderItems).Must(items => items.Any()).WithMessage("No order items found");

        // Bank transfers and cash on delivery are settled out of band, so no card is collected.
        When(command => PaymentMethods.RequiresCard(command.PaymentMethod), () =>
        {
            RuleFor(command => command.CardNumber).NotEmpty().Length(12, 19);
            RuleFor(command => command.CardHolderName).NotEmpty();
            RuleFor(command => command.CardExpiration).NotEmpty().Must(BeInTheFuture).WithMessage("Please specify a valid card expiration date");
            RuleFor(command => command.CardSecurityNumber).NotEmpty().Length(3, 4);
            RuleFor(command => command.CardTypeId).NotEmpty();
        });
    }

    private static bool BeInTheFuture(DateTime dateTime) => dateTime >= DateTime.UtcNow;
}

/// <summary>Requires a real order number before touching the aggregate.</summary>
public class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderCommandValidator() =>
        RuleFor(order => order.OrderNumber).NotEmpty().WithMessage("No orderId found");
}

/// <summary>Requires a real order number and dispatch details before touching the aggregate.</summary>
public class ShipOrderCommandValidator : AbstractValidator<ShipOrderCommand>
{
    public ShipOrderCommandValidator()
    {
        RuleFor(order => order.OrderNumber).NotEmpty().WithMessage("No orderId found");
        RuleFor(order => order.Carrier).NotEmpty().MaximumLength(100).WithMessage("A carrier is required");
        RuleFor(order => order.TrackingNumber).NotEmpty().MaximumLength(100).WithMessage("A tracking number is required");
    }
}

/// <summary>Every idempotent command must carry a non-empty request id.</summary>
public class IdentifiedCommandValidator : AbstractValidator<IdentifiedCommand<CreateOrderCommand, bool>>
{
    public IdentifiedCommandValidator() =>
        RuleFor(command => command.Id).NotEmpty();
}
