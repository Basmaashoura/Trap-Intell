using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Shared.ValueObjects;

/// <summary>
/// Value object representing a monetary amount with currency.
/// Provides proper handling of financial calculations and display.
/// </summary>
public record Money : IComparable<Money>
{
    /// <summary>
    /// The monetary amount.
    /// </summary>
    public decimal Amount { get; }

    /// <summary>
    /// The currency code (ISO 4217).
    /// </summary>
    public Currency Currency { get; }

    private Money(decimal amount, Currency currency)
    {
        Amount = amount;
        Currency = currency ?? throw new ArgumentNullException(nameof(currency));
    }

    #region Factory Methods

    /// <summary>
    /// Create a Money value with validation.
    /// </summary>
    public static Result<Money> Create(decimal amount, string currencyCode)
    {
        var currencyResult = ValueObjects.Currency.Create(currencyCode);
        if (currencyResult.IsFailure)
            return Result.Failure<Money>(currencyResult.Errors[0]);

        return Result.Success(new Money(Math.Round(amount, currencyResult.Value.DecimalPlaces), currencyResult.Value));
    }

    /// <summary>
    /// Create a Money value with a Currency object.
    /// </summary>
    public static Result<Money> Create(decimal amount, Currency currency)
    {
        if (currency == null)
            return Result.Failure<Money>(Error.Custom("Money.InvalidCurrency", "Currency cannot be null"));

        return Result.Success(new Money(Math.Round(amount, currency.DecimalPlaces), currency));
    }

    /// <summary>
    /// Create USD money.
    /// </summary>
    public static Money USD(decimal amount) => new(Math.Round(amount, 2), ValueObjects.Currency.USD);

    /// <summary>
    /// Create EUR money.
    /// </summary>
    public static Money EUR(decimal amount) => new(Math.Round(amount, 2), ValueObjects.Currency.EUR);

    /// <summary>
    /// Create GBP money.
    /// </summary>
    public static Money GBP(decimal amount) => new(Math.Round(amount, 2), ValueObjects.Currency.GBP);

    /// <summary>
    /// Create zero money in a currency.
    /// </summary>
    public static Money Zero(Currency currency) => new(0, currency);

    /// <summary>
    /// Create zero USD.
    /// </summary>
    public static Money ZeroUSD => new(0, ValueObjects.Currency.USD);

    #endregion

    #region Arithmetic Operations

    /// <summary>
    /// Add two money values (must be same currency).
    /// </summary>
    public Result<Money> Add(Money other)
    {
        if (!Currency.Equals(other.Currency))
            return Result.Failure<Money>(Error.Custom("Money.CurrencyMismatch", 
                $"Cannot add {Currency.Code} to {other.Currency.Code}"));

        return Result.Success(new Money(Amount + other.Amount, Currency));
    }

    /// <summary>
    /// Subtract money (must be same currency).
    /// </summary>
    public Result<Money> Subtract(Money other)
    {
        if (!Currency.Equals(other.Currency))
            return Result.Failure<Money>(Error.Custom("Money.CurrencyMismatch", 
                $"Cannot subtract {other.Currency.Code} from {Currency.Code}"));

        return Result.Success(new Money(Amount - other.Amount, Currency));
    }

    /// <summary>
    /// Multiply by a factor.
    /// </summary>
    public Money Multiply(decimal factor)
    {
        return new Money(Math.Round(Amount * factor, Currency.DecimalPlaces), Currency);
    }

    /// <summary>
    /// Divide by a factor.
    /// </summary>
    public Result<Money> Divide(decimal divisor)
    {
        if (divisor == 0)
            return Result.Failure<Money>(Error.Custom("Money.DivisionByZero", "Cannot divide by zero"));

        return Result.Success(new Money(Math.Round(Amount / divisor, Currency.DecimalPlaces), Currency));
    }

    /// <summary>
    /// Calculate percentage of this amount.
    /// </summary>
    public Money Percentage(decimal percent)
    {
        return new Money(Math.Round(Amount * percent / 100, Currency.DecimalPlaces), Currency);
    }

    /// <summary>
    /// Negate the amount.
    /// </summary>
    public Money Negate() => new(-Amount, Currency);

    /// <summary>
    /// Get absolute value.
    /// </summary>
    public Money Abs() => new(Math.Abs(Amount), Currency);

    #endregion

    #region Operators

    public static Money operator +(Money left, Money right)
    {
        if (!left.Currency.Equals(right.Currency))
            throw new InvalidOperationException($"Cannot add {left.Currency.Code} to {right.Currency.Code}");
        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public static Money operator -(Money left, Money right)
    {
        if (!left.Currency.Equals(right.Currency))
            throw new InvalidOperationException($"Cannot subtract {right.Currency.Code} from {left.Currency.Code}");
        return new Money(left.Amount - right.Amount, left.Currency);
    }

    public static Money operator *(Money money, decimal factor)
    {
        return money.Multiply(factor);
    }

    public static Money operator *(decimal factor, Money money)
    {
        return money.Multiply(factor);
    }

    public static Money operator /(Money money, decimal divisor)
    {
        if (divisor == 0)
            throw new DivideByZeroException("Cannot divide money by zero");
        return new Money(Math.Round(money.Amount / divisor, money.Currency.DecimalPlaces), money.Currency);
    }

    public static Money operator -(Money money)
    {
        return money.Negate();
    }

    public static bool operator <(Money left, Money right) => left.CompareTo(right) < 0;
    public static bool operator >(Money left, Money right) => left.CompareTo(right) > 0;
    public static bool operator <=(Money left, Money right) => left.CompareTo(right) <= 0;
    public static bool operator >=(Money left, Money right) => left.CompareTo(right) >= 0;

    #endregion

    #region Comparison

    public int CompareTo(Money? other)
    {
        if (other == null) return 1;
        if (!Currency.Equals(other.Currency))
            throw new InvalidOperationException($"Cannot compare {Currency.Code} with {other.Currency.Code}");
        return Amount.CompareTo(other.Amount);
    }

    #endregion

    #region Query Methods

    /// <summary>
    /// Check if amount is zero.
    /// </summary>
    public bool IsZero => Amount == 0;

    /// <summary>
    /// Check if amount is positive.
    /// </summary>
    public bool IsPositive => Amount > 0;

    /// <summary>
    /// Check if amount is negative.
    /// </summary>
    public bool IsNegative => Amount < 0;

    /// <summary>
    /// Check if this money has the same currency as another.
    /// </summary>
    public bool HasSameCurrency(Money other) => Currency.Equals(other?.Currency);

    #endregion

    #region Formatting

    /// <summary>
    /// Format as string with currency symbol.
    /// </summary>
    public override string ToString()
    {
        var format = "N" + Currency.DecimalPlaces;
        return $"{Currency.Symbol}{Amount.ToString(format)}";
    }

    /// <summary>
    /// Format as string with currency code.
    /// </summary>
    public string ToStringWithCode()
    {
        var format = "N" + Currency.DecimalPlaces;
        return $"{Amount.ToString(format)} {Currency.Code}";
    }

    /// <summary>
    /// Format for display (e.g., "$1,234.56").
    /// </summary>
    public string ToDisplayString()
    {
        var format = "N" + Currency.DecimalPlaces;
        return $"{Currency.Symbol}{Amount.ToString(format)}";
    }

    /// <summary>
    /// Format for accounting (negative in parentheses).
    /// </summary>
    public string ToAccountingString()
    {
        var format = "N" + Currency.DecimalPlaces;
        if (Amount < 0)
            return $"({Currency.Symbol}{Math.Abs(Amount).ToString(format)})";
        return ToDisplayString();
    }

    #endregion
}

/// <summary>
/// Value object representing a currency.
/// </summary>
public record Currency
{
    /// <summary>
    /// ISO 4217 currency code.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Currency name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Currency symbol.
    /// </summary>
    public string Symbol { get; }

    /// <summary>
    /// Number of decimal places.
    /// </summary>
    public int DecimalPlaces { get; }

    private Currency(string code, string name, string symbol, int decimalPlaces)
    {
        Code = code;
        Name = name;
        Symbol = symbol;
        DecimalPlaces = decimalPlaces;
    }

    #region Factory Methods

    /// <summary>
    /// Create a currency from ISO code.
    /// </summary>
    public static Result<Currency> Create(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Result.Failure<Currency>(Error.Custom("Currency.InvalidCode", "Currency code cannot be empty"));

        var upperCode = code.ToUpperInvariant().Trim();

        return upperCode switch
        {
            "USD" => Result.Success(USD),
            "EUR" => Result.Success(EUR),
            "GBP" => Result.Success(GBP),
            "JPY" => Result.Success(JPY),
            "CAD" => Result.Success(CAD),
            "AUD" => Result.Success(AUD),
            "CHF" => Result.Success(CHF),
            "CNY" => Result.Success(CNY),
            "INR" => Result.Success(INR),
            "BRL" => Result.Success(BRL),
            "MXN" => Result.Success(MXN),
            "SGD" => Result.Success(SGD),
            "HKD" => Result.Success(HKD),
            "SEK" => Result.Success(SEK),
            "NOK" => Result.Success(NOK),
            "DKK" => Result.Success(DKK),
            "NZD" => Result.Success(NZD),
            "ZAR" => Result.Success(ZAR),
            "AED" => Result.Success(AED),
            "SAR" => Result.Success(SAR),
            _ => Result.Failure<Currency>(Error.Custom("Currency.Unsupported", $"Currency '{code}' is not supported"))
        };
    }

    #endregion

    #region Standard Currencies

    public static readonly Currency USD = new("USD", "US Dollar", "$", 2);
    public static readonly Currency EUR = new("EUR", "Euro", "€", 2);
    public static readonly Currency GBP = new("GBP", "British Pound", "£", 2);
    public static readonly Currency JPY = new("JPY", "Japanese Yen", "¥", 0);
    public static readonly Currency CAD = new("CAD", "Canadian Dollar", "CA$", 2);
    public static readonly Currency AUD = new("AUD", "Australian Dollar", "A$", 2);
    public static readonly Currency CHF = new("CHF", "Swiss Franc", "CHF", 2);
    public static readonly Currency CNY = new("CNY", "Chinese Yuan", "¥", 2);
    public static readonly Currency INR = new("INR", "Indian Rupee", "?", 2);
    public static readonly Currency BRL = new("BRL", "Brazilian Real", "R$", 2);
    public static readonly Currency MXN = new("MXN", "Mexican Peso", "MX$", 2);
    public static readonly Currency SGD = new("SGD", "Singapore Dollar", "S$", 2);
    public static readonly Currency HKD = new("HKD", "Hong Kong Dollar", "HK$", 2);
    public static readonly Currency SEK = new("SEK", "Swedish Krona", "kr", 2);
    public static readonly Currency NOK = new("NOK", "Norwegian Krone", "kr", 2);
    public static readonly Currency DKK = new("DKK", "Danish Krone", "kr", 2);
    public static readonly Currency NZD = new("NZD", "New Zealand Dollar", "NZ$", 2);
    public static readonly Currency ZAR = new("ZAR", "South African Rand", "R", 2);
    public static readonly Currency AED = new("AED", "UAE Dirham", "Ï.Å", 2);
    public static readonly Currency SAR = new("SAR", "Saudi Riyal", "?", 2);

    /// <summary>
    /// Get all supported currencies.
    /// </summary>
    public static IReadOnlyList<Currency> All => new[]
    {
        USD, EUR, GBP, JPY, CAD, AUD, CHF, CNY, INR, BRL,
        MXN, SGD, HKD, SEK, NOK, DKK, NZD, ZAR, AED, SAR
    };

    #endregion

    public override string ToString() => Code;
}

/// <summary>
/// Tax calculation result.
/// </summary>
public record TaxAmount
{
    /// <summary>
    /// Net amount (before tax).
    /// </summary>
    public Money NetAmount { get; }

    /// <summary>
    /// Tax amount.
    /// </summary>
    public Money Tax { get; }

    /// <summary>
    /// Gross amount (after tax).
    /// </summary>
    public Money GrossAmount { get; }

    /// <summary>
    /// Tax rate as percentage.
    /// </summary>
    public decimal TaxRate { get; }

    /// <summary>
    /// Tax name (e.g., "VAT", "Sales Tax").
    /// </summary>
    public string TaxName { get; }

    public TaxAmount(Money netAmount, decimal taxRatePercent, string taxName = "Tax")
    {
        NetAmount = netAmount;
        TaxRate = taxRatePercent;
        TaxName = taxName;
        Tax = netAmount.Percentage(taxRatePercent);
        GrossAmount = netAmount + Tax;
    }

    /// <summary>
    /// Calculate from gross amount (tax inclusive).
    /// </summary>
    public static TaxAmount FromGross(Money grossAmount, decimal taxRatePercent, string taxName = "Tax")
    {
        var netAmount = grossAmount / (1 + taxRatePercent / 100);
        return new TaxAmount(netAmount, taxRatePercent, taxName);
    }

    public override string ToString() =>
        $"{NetAmount} + {TaxName} ({TaxRate}%: {Tax}) = {GrossAmount}";
}

/// <summary>
/// Price with breakdown.
/// </summary>
public record PriceBreakdown
{
    /// <summary>
    /// Base price.
    /// </summary>
    public Money BasePrice { get; init; }

    /// <summary>
    /// Discount amount.
    /// </summary>
    public Money Discount { get; init; }

    /// <summary>
    /// Subtotal after discount.
    /// </summary>
    public Money Subtotal { get; init; }

    /// <summary>
    /// Tax amount.
    /// </summary>
    public Money Tax { get; init; }

    /// <summary>
    /// Total amount.
    /// </summary>
    public Money Total { get; init; }

    /// <summary>
    /// Discount percentage applied.
    /// </summary>
    public decimal DiscountPercent { get; init; }

    /// <summary>
    /// Tax rate applied.
    /// </summary>
    public decimal TaxRate { get; init; }

    public PriceBreakdown(
        Money basePrice,
        decimal discountPercent = 0,
        decimal taxRate = 0)
    {
        BasePrice = basePrice;
        DiscountPercent = discountPercent;
        TaxRate = taxRate;
        Discount = basePrice.Percentage(discountPercent);
        Subtotal = basePrice - Discount;
        Tax = Subtotal.Percentage(taxRate);
        Total = Subtotal + Tax;
    }

    public static PriceBreakdown Simple(Money total) => new(total);
}
