using TestRule;

// ─────────────────────────────────────────────────────────────────────────────
// Helpers
// ─────────────────────────────────────────────────────────────────────────────

static void RunTest(string label, string expression, Dictionary<string, string> input, object? expected = null)
{
    var result = RuleEvaluator.Evaluate(expression, input);
    bool passed = expected is null
        ? result is not EvaluationError
        : Equals(result, expected);

    string status = passed ? "PASS" : "FAIL";
    string expectedStr = expected is null ? "(any non-error)" : expected.ToString()!;
    Console.WriteLine($"  [{status}] {label}");
    Console.WriteLine($"         expr   : {expression}");
    Console.WriteLine($"         result : {result}  |  expected : {expectedStr}");
    Console.WriteLine();
}

static void Section(string title)
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 60));
    Console.WriteLine($"  {title}");
    Console.WriteLine(new string('=', 60));
    Console.WriteLine();
}

// ─────────────────────────────────────────────────────────────────────────────
// 1. Numeric Comparison Rules
// ─────────────────────────────────────────────────────────────────────────────
Section("1. Numeric Comparison Rules");

var numericData = new Dictionary<string, string>
{
    ["Age"]         = "25",
    ["Score"]       = "87.5",
    ["Threshold"]   = "80.0",
    ["MinAge"]      = "18",
    ["MaxAge"]      = "65",
    ["Balance"]     = "-50.75",
    ["Zero"]        = "0",
};

RunTest("Age > 18",                "[Age] > 18",                       numericData, expected: true);
RunTest("Age < MinAge",            "[Age] < [MinAge]",                 numericData, expected: false);
RunTest("Score >= Threshold",      "[Score] >= [Threshold]",           numericData, expected: true);
RunTest("Score > Threshold",       "[Score] > [Threshold]",            numericData, expected: true);
RunTest("Balance < Zero",          "[Balance] < [Zero]",               numericData, expected: true);
RunTest("Age in range [18, 65]",   "[Age] >= [MinAge] && [Age] <= [MaxAge]", numericData, expected: true);
RunTest("Score != 100",            "[Score] != 100",                   numericData, expected: true);
RunTest("Age == 25",               "[Age] == 25",                      numericData, expected: true);

// ─────────────────────────────────────────────────────────────────────────────
// 2. Arithmetic Expressions
// ─────────────────────────────────────────────────────────────────────────────
Section("2. Arithmetic Expressions");

var arithData = new Dictionary<string, string>
{
    ["Price"]    = "100",
    ["Discount"] = "15",
    ["Qty"]      = "3",
    ["TaxRate"]  = "0.07",
};

RunTest("Price after discount",        "[Price] - [Discount]",                          arithData, expected: 85L);
RunTest("Total before tax",            "[Price] * [Qty]",                               arithData, expected: 300L);
RunTest("Total with tax (double)",     "([Price] * [Qty]) * (1 + [TaxRate])",           arithData, expected: 321.0);
RunTest("Discount %",                  "[Discount] / [Price] * 100",                    arithData, expected: 15.0);
RunTest("Price after discount > 80",   "[Price] - [Discount] > 80",                     arithData, expected: true);
RunTest("Math: Pow(Price, 2)",         "Pow([Price], 2) == 10000",                      arithData, expected: true);
RunTest("Math: Sqrt(Qty * 3)",         "Sqrt([Qty] * 3)",                               arithData, expected: 3.0);
RunTest("Math: Abs(Balance)",
        "Abs(-50) == 50",                     arithData, expected: true);   // returns int 50 (no expected check)

// ─────────────────────────────────────────────────────────────────────────────
// 3. String Comparison Rules
// ─────────────────────────────────────────────────────────────────────────────
Section("3. String Comparison Rules");

var stringData = new Dictionary<string, string>
{
    ["Status"]   = "active",
    ["Role"]     = "Admin",
    ["Country"]  = "US",
    ["Category"] = "Electronics",
    ["Tag"]      = "sale",
};

RunTest("Status == 'active'",          "[Status] == 'active'",          stringData, expected: true);
RunTest("Status == 'inactive'",        "[Status] == 'inactive'",        stringData, expected: false);
RunTest("Role != 'User'",              "[Role] != 'User'",              stringData, expected: true);
RunTest("Country == 'US'",             "[Country] == 'US'",             stringData, expected: true);
RunTest("Category == 'Electronics'",   "[Category] == 'Electronics'",   stringData, expected: true);
RunTest("Tag == 'sale'",               "[Tag] == 'sale'",               stringData, expected: true);

// ─────────────────────────────────────────────────────────────────────────────
// 4. Boolean Logic Rules
// ─────────────────────────────────────────────────────────────────────────────
Section("4. Boolean Logic (AND / OR / NOT)");

var boolData = new Dictionary<string, string>
{
    ["IsVerified"] = "true",
    ["IsActive"]   = "true",
    ["IsBanned"]   = "false",
    ["Age"]        = "22",
    ["Score"]      = "55",
};

// Basic
RunTest("Verified AND Active",           "[IsVerified] && [IsActive]",                    boolData, expected: true);
RunTest("Verified AND NOT Banned",       "[IsVerified] && ![IsBanned]",                   boolData, expected: true);
RunTest("Banned OR Score < 60",          "[IsBanned] || [Score] < 60",                    boolData, expected: true);
RunTest("NOT Banned AND Age >= 18",      "![IsBanned] && [Age] >= 18",                    boolData, expected: true);
RunTest("All conditions met",            "[IsVerified] && [IsActive] && ![IsBanned] && [Age] >= 18", boolData, expected: true);
RunTest("Score > 90 (should fail)",      "[Score] > 90",                                  boolData, expected: false);

// Grouped with parentheses — precedence control
RunTest("(A OR B) AND C",
    "([IsBanned] || [Score] < 60) && [IsVerified]",
    boolData, expected: true);

RunTest("A AND (B OR C) — parentheses change result",
    "[IsVerified] && ([IsBanned] || [Age] > 30)",
    boolData, expected: false);   // Age=22, Banned=false → inner is false

RunTest("A AND (B OR C) — without parens different precedence",
    "[IsVerified] && [IsBanned] || [Age] > 30",
    boolData, expected: false);   // (Verified && Banned) || Age>30 → false||false

RunTest("NOT of a grouped expression",
    "!([IsBanned] || [Age] > 30)",
    boolData, expected: true);    // !(false || false)

RunTest("NOT of AND group",
    "!([IsVerified] && [IsBanned])",
    boolData, expected: true);    // !(true && false)

// Deeply nested parentheses
RunTest("Triple-nested grouping",
    "((([IsVerified] && ![IsBanned]) && ([Age] >= 18 && [Age] <= 65)) && [Score] >= 50)",
    boolData, expected: true);

RunTest("Triple-nested — one inner fails",
    "((([IsVerified] && ![IsBanned]) && ([Age] >= 18 && [Age] <= 20)) && [Score] >= 90)",
    boolData, expected: false);   // Score=55 < 90

// Complex OR/AND mixing with NOT brackets
RunTest("(NOT A OR B) AND (C OR NOT D)",
    "(!([IsVerified]) || [IsActive]) && ([IsBanned] || ![IsBanned])",
    boolData, expected: true);    // (false||true) && (true) = true

RunTest("NOT both conditions",
    "!([Score] > 90 && [Age] < 18)",
    boolData, expected: true);    // !(false && false)

RunTest("NOT either condition",
    "!([Score] > 90 || [Age] < 18)",
    boolData, expected: true);    // !(false || false)

RunTest("NOT either condition (one true — should fail)",
    "!([Score] < 90 || [Age] < 18)",
    boolData, expected: false);   // !(true || false) = false

// Parentheses around arithmetic inside boolean
RunTest("Arithmetic in grouped condition",
    "([Age] * 2) > 40 && ([Score] + 10) < 70",
    boolData, expected: true);    // 44>40 && 65<70

RunTest("Mixed grouping: OR of two AND groups",
    "([IsVerified] && [Age] >= 21) || ([IsBanned] && [Score] > 50)",
    boolData, expected: true);    // (true&&true) || (false&&true)

RunTest("Mixed grouping: both AND groups false",
    "([Score] > 90 && [Age] >= 21) || ([IsBanned] && [IsActive])",
    boolData, expected: false);   // (false&&true) || (false&&true)

// ─────────────────────────────────────────────────────────────────────────────
// 5. Mixed Numeric + String + Boolean
// ─────────────────────────────────────────────────────────────────────────────
Section("5. Mixed Rules");

var mixedData = new Dictionary<string, string>
{
    ["Age"]        = "30",
    ["Status"]     = "active",
    ["Score"]      = "92",
    ["IsVerified"] = "true",
    ["Tier"]       = "gold",
    ["Balance"]    = "1500",
};

RunTest("Eligible for premium",
    "[Age] >= 21 && [Status] == 'active' && [Score] >= 90",
    mixedData, expected: true);

RunTest("High-value customer",
    "[Balance] >= 1000 && [Tier] == 'gold' && [IsVerified]",
    mixedData, expected: true);

RunTest("Discount eligible: score > 85 OR tier == 'gold'",
    "[Score] > 85 || [Tier] == 'gold'",
    mixedData, expected: true);

RunTest("KYC passed: verified AND age >= 18",
    "[IsVerified] == true && [Age] >= 18",
    mixedData, expected: true);

// ─────────────────────────────────────────────────────────────────────────────
// 6. Ternary-style with if()
// ─────────────────────────────────────────────────────────────────────────────
Section("6. Conditional (if) Expressions");

var condData = new Dictionary<string, string>
{
    ["Score"]   = "75",
    ["Passing"] = "60",
};

RunTest("Grade label (Pass)",   "if([Score] >= [Passing], 'Pass', 'Fail')", condData, expected: "Pass");
RunTest("Grade label (Fail)",   "if([Score] < [Passing],  'Pass', 'Fail')", condData, expected: "Fail");
RunTest("Bonus eligible",       "if([Score] >= 90, 'BonusEligible', 'NoBonus')", condData, expected: "NoBonus");

// ─────────────────────────────────────────────────────────────────────────────
// 7. Built-in Math Functions
// ─────────────────────────────────────────────────────────────────────────────
Section("7. Built-in Math Functions");

var mathData = new Dictionary<string, string>
{
    ["X"] = "9",
    ["Y"] = "2",
    ["Z"] = "-4.5",
};

RunTest("Sqrt(X)",       "Sqrt([X])",              mathData, expected: 3.0);
RunTest("Pow(X, Y)",     "Pow([X], [Y])",          mathData, expected: 81.0);
RunTest("Abs(Z)",        "Abs([Z])",               mathData, expected: 4.5);
RunTest("Floor(Z)",      "Floor([Z])",             mathData, expected: -5.0);
RunTest("Ceiling(Z)",    "Ceiling([Z])",           mathData, expected: -4.0);
RunTest("Round(Z, 0)",   "Round([Z], 0)",          mathData, expected: -4.0);
RunTest("Max(X, Y)",     "Max([X], [Y])",          mathData, expected: 9L);
RunTest("Min(X, Y)",     "Min([X], [Y])",          mathData, expected: 2L);
RunTest("Log(X, 10)",    "Log([X], 10)",           mathData);       // Log(value, base)

// ─────────────────────────────────────────────────────────────────────────────
// 8. Date Functions & Subtraction
//    Dates in the dictionary use ISO-8601 format (yyyy-MM-dd).
//    WEEK(): 0=Sunday, 1=Monday, 2=Tuesday, 3=Wednesday,
//            4=Thursday, 5=Friday, 6=Saturday
// ─────────────────────────────────────────────────────────────────────────────
Section("8. Date Functions & Subtraction");

var dateData = new Dictionary<string, string>
{
    ["StartDate"]   = "2024-03-15",   // Friday  → WEEK=5, DAY=15, MONTH=3, YEAR=2024
    ["EndDate"]     = "2024-03-20",   // Wednesday → WEEK=3, DAY=20
    ["Birthday"]    = "1990-07-04",   // Wednesday → WEEK=3, DAY=4,  MONTH=7, YEAR=1990
    ["NewYear"]     = "2024-01-01",   // Monday  → WEEK=1, DAY=1,  MONTH=1
    ["AltFormat"]   = "03/15/2024",   // MM/dd/yyyy — same day as StartDate
};

// --- DAY() ---
RunTest("DAY(StartDate) == 15",      "DAY([StartDate]) == 15",      dateData, expected: true);
RunTest("DAY(NewYear) == 1",         "DAY([NewYear]) == 1",         dateData, expected: true);
RunTest("DAY(Birthday) == 4",        "DAY([Birthday]) == 4",        dateData, expected: true);
RunTest("DAY(EndDate) > DAY(StartDate)",
    "DAY([EndDate]) > DAY([StartDate])",                             dateData, expected: true);

// --- WEEK() ---
RunTest("WEEK(StartDate) == 5 (Friday)",    "WEEK([StartDate]) == 5",  dateData, expected: true);
RunTest("WEEK(EndDate) == 3 (Wednesday)",   "WEEK([EndDate]) == 3",    dateData, expected: true);
RunTest("WEEK(NewYear) == 1 (Monday)",      "WEEK([NewYear]) == 1",    dateData, expected: true);
RunTest("WEEK(Birthday) == 3 (Wednesday)",  "WEEK([Birthday]) == 3",   dateData, expected: true);
RunTest("StartDate is a weekday (Mon–Fri)",
    "WEEK([StartDate]) >= 1 && WEEK([StartDate]) <= 5",              dateData, expected: true);
RunTest("EndDate is NOT a weekend",
    "!(WEEK([EndDate]) == 0 || WEEK([EndDate]) == 6)",               dateData, expected: true);

// --- MONTH() / YEAR() ---
RunTest("MONTH(StartDate) == 3",       "MONTH([StartDate]) == 3",    dateData, expected: true);
RunTest("YEAR(StartDate) == 2024",     "YEAR([StartDate]) == 2024",  dateData, expected: true);
RunTest("YEAR(Birthday) == 1990",      "YEAR([Birthday]) == 1990",   dateData, expected: true);
RunTest("Birthday is in summer (Jun–Aug)",
    "MONTH([Birthday]) >= 6 && MONTH([Birthday]) <= 8",              dateData, expected: true);
RunTest("StartDate and EndDate same year",
    "YEAR([StartDate]) == YEAR([EndDate])",                          dateData, expected: true);
RunTest("StartDate and EndDate same month",
    "MONTH([StartDate]) == MONTH([EndDate])",                        dateData, expected: true);

// --- DATEDIFF(d1, d2) → (d1 − d2) total days ---
RunTest("DATEDIFF(EndDate, StartDate) == 5",
    "DATEDIFF([EndDate], [StartDate]) == 5",                         dateData, expected: true);
RunTest("DATEDIFF(StartDate, EndDate) == -5 (negative)",
    "DATEDIFF([StartDate], [EndDate]) == -5",                        dateData, expected: true);
RunTest("DATEDIFF > 0 (forward range)",
    "DATEDIFF([EndDate], [StartDate]) > 0",                          dateData, expected: true);
RunTest("DATEDIFF within 30-day window",
    "DATEDIFF([EndDate], [StartDate]) <= 30 && DATEDIFF([EndDate], [StartDate]) >= 0",
    dateData, expected: true);
RunTest("Birthday gap > 10000 days from StartDate",
    "DATEDIFF([StartDate], [Birthday]) > 10000",                     dateData, expected: true);

// --- Native DateTime comparison (<, >, ==) ---
RunTest("StartDate < EndDate",         "[StartDate] < [EndDate]",    dateData, expected: true);
RunTest("EndDate > StartDate",         "[EndDate] > [StartDate]",    dateData, expected: true);
RunTest("StartDate != EndDate",        "[StartDate] != [EndDate]",   dateData, expected: true);
RunTest("AltFormat same day as StartDate (cross-format equality)",
    "[AltFormat] == [StartDate]",                                    dateData, expected: true);

// --- Combined / complex ---
RunTest("Valid booking: weekday AND within range",
    "WEEK([StartDate]) >= 1 && WEEK([StartDate]) <= 5 && DATEDIFF([EndDate], [StartDate]) <= 7",
    dateData, expected: true);
RunTest("Anniversary check: same month AND day",
    "MONTH([StartDate]) == MONTH([AltFormat]) && DAY([StartDate]) == DAY([AltFormat])",
    dateData, expected: true);
RunTest("Expiry check: DATEDIFF > 0 AND end is a weekday",
    "DATEDIFF([EndDate], [StartDate]) > 0 && WEEK([EndDate]) >= 1 && WEEK([EndDate]) <= 5",
    dateData, expected: true);

// ─────────────────────────────────────────────────────────────────────────────
// 9. Error / Edge-Case Handling
// ─────────────────────────────────────────────────────────────────────────────
Section("9. Error & Edge-Case Handling");

// Missing parameter — NCalc returns null for an unset parameter
var errorData = new Dictionary<string, string> { ["X"] = "10" };
var missing = RuleEvaluator.Evaluate("[X] + [Y]", errorData);
Console.WriteLine($"  Missing param [Y]: result = {missing ?? "null"} (EvaluationError expected)");
Console.WriteLine();

// Division by zero
var divData = new Dictionary<string, string> { ["A"] = "10", ["B"] = "0" };
var divResult = RuleEvaluator.Evaluate("[A] / [B]", divData);
Console.WriteLine($"  Division by zero: result = {divResult}");
Console.WriteLine();

// Malformed expression
var badResult = RuleEvaluator.Evaluate("[A] >>>< [B]", divData);
string badKind = badResult is EvaluationError e ? $"EvaluationError: {e.Message}" : badResult?.ToString()!;
Console.WriteLine($"  Malformed expression: {badKind}");
Console.WriteLine();

// Empty dictionary
var emptyResult = RuleEvaluator.Evaluate("1 + 1", new Dictionary<string, string>());
Console.WriteLine($"  No params (1 + 1): result = {emptyResult}  (expected 2)");
Console.WriteLine();

// ─────────────────────────────────────────────────────────────────────────────
Console.WriteLine(new string('=', 60));
Console.WriteLine("  All tests complete.");
Console.WriteLine(new string('=', 60));
