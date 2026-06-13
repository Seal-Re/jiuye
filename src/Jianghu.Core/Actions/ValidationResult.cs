namespace Jianghu.Actions
{
    public readonly record struct ValidationResult(bool Ok, string Reason)
    {
        public static ValidationResult Valid => new ValidationResult(true, "");
        public static ValidationResult Invalid(string reason) => new ValidationResult(false, reason);
    }
}
