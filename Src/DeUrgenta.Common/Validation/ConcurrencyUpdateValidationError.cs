namespace DeUrgenta.Common.Validation
{
    using System.Collections.Immutable;

    public record ConcurrencyUpdateValidationError : ValidationResult
    {
        public ConcurrencyUpdateValidationError() : base(false, ImmutableDictionary<string, string>.Empty)
        {
        }
    }
}