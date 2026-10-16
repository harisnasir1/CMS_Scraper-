namespace CMS_Scrappers.Pricing.Policies;

/// <summary>
/// Turns a supplier's base price (in the supplier's own currency) into the figures a
/// destination system receives. One policy instance = one (supplier, destination) rule set.
/// Policies are pure: no I/O, no state, same input always gives the same output.
/// </summary>
public interface IPricingPolicy
{
    /// <summary>Price the destination lists the item at.</summary>
    decimal Sell(decimal basePrice);

    /// <summary>Price the destination records as our cost for the item.</summary>
    decimal Cost(decimal basePrice);
}
