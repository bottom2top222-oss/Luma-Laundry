using Microsoft.Extensions.Options;

namespace LaundryApp.Services;

public class ServiceAreaService
{
    private readonly ServiceAreaOptions _options;

    public ServiceAreaService(IOptions<ServiceAreaOptions> options)
    {
        _options = options.Value;
    }

    public bool IsAddressServed(string city, string state, string zipCode, out string message)
    {
        message = _options.OutOfAreaMessage;

        if (!_options.Enabled)
        {
            return true;
        }

        var normalizedCity = (city ?? string.Empty).Trim();
        var normalizedState = (state ?? string.Empty).Trim().ToUpperInvariant();
        var normalizedZip = NormalizeZip(zipCode);

        if (!string.IsNullOrWhiteSpace(_options.AllowedState) && !string.Equals(normalizedState, _options.AllowedState.Trim().ToUpperInvariant(), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (_options.AllowedCities.Count > 0)
        {
            var allowedCities = _options.AllowedCities
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (!allowedCities.Contains(normalizedCity))
            {
                return false;
            }
        }

        if (_options.AllowedZipCodes.Count > 0)
        {
            var allowedZips = _options.AllowedZipCodes
                .Where(z => !string.IsNullOrWhiteSpace(z))
                .Select(NormalizeZip)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(normalizedZip) || !allowedZips.Contains(normalizedZip))
            {
                return false;
            }
        }

        return true;
    }

    private static string NormalizeZip(string zipCode)
    {
        if (string.IsNullOrWhiteSpace(zipCode))
        {
            return string.Empty;
        }

        var digits = new string(zipCode.Where(char.IsDigit).ToArray());
        return digits.Length >= 5 ? digits[..5] : digits;
    }
}
