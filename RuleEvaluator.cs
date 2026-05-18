using NCalc;

namespace TestRule;

/// <summary>
/// Evaluates NCalc expression rules against a Dictionary&lt;string, string&gt; input.
/// String values are auto-coerced to their most specific numeric/boolean type
/// so that arithmetic and comparison expressions work transparently.
/// </summary>
public sealed class RuleEvaluator
{
    // Formats tried (in order) when coercing a string to DateTime.
    private static readonly string[] DateFormats =
    [
        "yyyy-MM-dd",
        "yyyy-MM-ddTHH:mm:ss",
        "MM/dd/yyyy",
        "dd-MM-yyyy",
    ];

    /// <summary>
    /// Evaluates the given expression against the supplied parameter dictionary.
    /// All parameter values are coerced from string to the most specific compatible
    /// type (bool → long → double → DateTime → string) so that numeric, date, and
    /// string comparisons work without manual casting in the expression.
    ///
    /// Custom date functions available in expressions:
    /// <list type="bullet">
    ///   <item><c>DAY(date)</c>    – day-of-month (1–31)</item>
    ///   <item><c>WEEK(date)</c>   – day-of-week  (0=Sunday … 6=Saturday)</item>
    ///   <item><c>MONTH(date)</c>  – month number (1–12)</item>
    ///   <item><c>YEAR(date)</c>   – four-digit year</item>
    ///   <item><c>DATEDIFF(d1, d2)</c> – (d1 − d2) as total days (double)</item>
    /// </list>
    /// </summary>
    /// <param name="expression">
    /// NCalc expression string. Reference parameters with square-bracket syntax,
    /// e.g. <c>[Age] &gt; 18</c>, <c>[Status] == 'active'</c>, or
    /// <c>DAY([Invoice]) == 1</c>.
    /// </param>
    /// <param name="parameters">Input data as string key-value pairs.</param>
    /// <returns>The evaluated result, or an <see cref="EvaluationError"/> on failure.</returns>
    public static object? Evaluate(string expression, Dictionary<string, string> parameters)
    {
        var expr = new Expression(expression);

        foreach (var (key, rawValue) in parameters)
            expr.Parameters[key] = CoerceValue(rawValue);

        RegisterDateFunctions(expr);

        if (expr.HasErrors())
            return new EvaluationError($"Parse error: {expr.Error}");

        try
        {
            return expr.Evaluate();
        }
        catch (Exception ex)
        {
            return new EvaluationError(ex.Message);
        }
    }

    // ── Custom date functions ────────────────────────────────────────────────

    private static void RegisterDateFunctions(Expression expr)
    {
        // DAY(date) → day-of-month integer (1–31)
        expr.Functions["DAY"] = (args) =>
        {
            var dt = ToDateTime(args[0].Evaluate());
            return dt.Day;
        };

        // WEEK(date) → day-of-week integer (0=Sunday, 1=Monday … 6=Saturday)
        expr.Functions["WEEK"] = (args) =>
        {
            var dt = ToDateTime(args[0].Evaluate());
            return (int)dt.DayOfWeek;
        };

        // MONTH(date) → month integer (1–12)
        expr.Functions["MONTH"] = (args) =>
        {
            var dt = ToDateTime(args[0].Evaluate());
            return dt.Month;
        };

        // YEAR(date) → four-digit year integer
        expr.Functions["YEAR"] = (args) =>
        {
            var dt = ToDateTime(args[0].Evaluate());
            return dt.Year;
        };

        // DATEDIFF(d1, d2) → (d1 − d2) as total days (double, may be negative)
        expr.Functions["DATEDIFF"] = (args) =>
        {
            var d1 = ToDateTime(args[0].Evaluate());
            var d2 = ToDateTime(args[1].Evaluate());
            return (d1 - d2).TotalDays;
        };
    }

    private static DateTime ToDateTime(object? value) => value switch
    {
        DateTime dt => dt,
        string s when DateTime.TryParseExact(
            s, DateFormats,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out var dt) => dt,
        string s when DateTime.TryParse(
            s,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out var dt) => dt,
        _ => throw new InvalidOperationException($"Cannot convert '{value}' to DateTime.")
    };

    // ── Value coercion ───────────────────────────────────────────────────────

    /// <summary>
    /// Converts a raw string value to bool, long, double, DateTime, or string —
    /// whichever is the most specific type that parses successfully.
    /// </summary>
    private static object CoerceValue(string raw)
    {
        if (bool.TryParse(raw, out bool b)) return b;
        if (int.TryParse(raw, out int i)) return i;
        if (double.TryParse(raw,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out double d)) return d;
        if (DateTime.TryParseExact(raw, DateFormats,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out DateTime dt)) return dt;
        return raw;
    }
}

/// <summary>Represents a rule evaluation failure.</summary>
public sealed record EvaluationError(string Message);
