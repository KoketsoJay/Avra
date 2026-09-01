using System.ComponentModel.DataAnnotations;

namespace PaymentService;

/// <summary>
/// Settings for the simulated payment processor.
/// </summary>
public class PaymentSettings
{
    public const string SectionName = "PaymentSettings";

    /// <summary>
    /// Whether the simulated gateway approves charges. Real deployments replace this class with a
    /// gateway client; the surrounding workflow does not change.
    /// </summary>
    public bool PaymentSucceeded { get; set; } = true;

    /// <summary>Reason reported when a charge is declined.</summary>
    [Required]
    public string DeclineReason { get; set; } = "The payment was declined by the issuer.";
}
