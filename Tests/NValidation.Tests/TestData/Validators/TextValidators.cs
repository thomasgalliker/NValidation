using System.Text.RegularExpressions;

namespace NValidation.TestData.Validators
{
    internal sealed class NameMinimumLengthValidator : Validator<Manufacturer>
    {
        public NameMinimumLengthValidator(int minimumLength)
        {
            this.Property(m => m.Name).MinimumLength(minimumLength);
        }
    }

    internal sealed class NameMaximumLengthValidator : Validator<Manufacturer>
    {
        public NameMaximumLengthValidator(int maximumLength)
        {
            this.Property(m => m.Name).MaximumLength(maximumLength);
        }
    }

    internal sealed class NameLengthRangeValidator : Validator<Manufacturer>
    {
        public NameLengthRangeValidator(int minimumLength, int maximumLength)
        {
            this.Property(m => m.Name).Length(minimumLength, maximumLength);
        }
    }

    internal sealed class CountryCodeLengthValidator : Validator<Manufacturer>
    {
        public CountryCodeLengthValidator(int length)
        {
            this.Property(m => m.CountryCode).Length(length);
        }
    }

    internal sealed class ContactEmailValidator : Validator<Manufacturer>
    {
        public ContactEmailValidator()
        {
            this.Property(m => m.ContactEmail).EmailAddress();
        }
    }

    internal sealed class WebsitePatternValidator : Validator<Manufacturer>
    {
        public WebsitePatternValidator(string pattern)
        {
            this.Property(m => m.Website).Matches(pattern);
        }
    }

    internal sealed class WebsiteRegexValidator : Validator<Manufacturer>
    {
        public WebsiteRegexValidator(Regex regex)
        {
            this.Property(m => m.Website).Matches(regex);
        }
    }
}
