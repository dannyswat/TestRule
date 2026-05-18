# TestRule — NCalcSync Rule Evaluator

A C# console project demonstrating rule evaluation using the [NCalcSync](https://github.com/ncalc/ncalc) expression library. All input is supplied as `Dictionary<string, string>`; the evaluator automatically coerces values to the right runtime type so expressions work without any manual casting.

---

## Quick Start

```bash
dotnet restore
dotnet run
```

---

## RuleEvaluator API

```csharp
object? result = RuleEvaluator.Evaluate(expression, parameters);
```

| Parameter | Type | Description |
|---|---|---|
| `expression` | `string` | NCalc expression. Reference parameters with `[Name]` syntax. |
| `parameters` | `Dictionary<string, string>` | Input data — all values are strings; coercion is automatic. |

**Return value** is the evaluated result (`bool`, `int`, `double`, `DateTime`, `string`) or an `EvaluationError` on parse/runtime failure.

### Checking for errors

```csharp
var result = RuleEvaluator.Evaluate("[X] + [Y]", data);

if (result is EvaluationError err)
    Console.WriteLine($"Failed: {err.Message}");
else
    Console.WriteLine($"Result: {result}");
```

---

## Automatic Type Coercion

String values are converted in priority order before being passed to NCalc:

| Priority | Target type | Example input |
|---|---|---|
| 1 | `bool` | `"true"`, `"false"` |
| 2 | `int` | `"42"`, `"-7"` |
| 3 | `double` | `"3.14"`, `"-0.5"` |
| 4 | `DateTime` | `"2024-03-15"`, `"03/15/2024"` |
| 5 | `string` | `"active"`, `"gold"` |

Supported date input formats: `yyyy-MM-dd`, `yyyy-MM-ddTHH:mm:ss`, `MM/dd/yyyy`, `dd-MM-yyyy`.

---

## Examples

### 1. Numeric Comparison

```csharp
var data = new Dictionary<string, string>
{
    ["Age"]   = "25",
    ["Score"] = "87.5",
    ["MinAge"] = "18",
    ["MaxAge"] = "65",
};

RuleEvaluator.Evaluate("[Age] > 18", data);                          // true
RuleEvaluator.Evaluate("[Score] >= 80.0", data);                     // true
RuleEvaluator.Evaluate("[Age] >= [MinAge] && [Age] <= [MaxAge]", data); // true
RuleEvaluator.Evaluate("[Score] != 100", data);                      // true
```

### 2. Arithmetic

```csharp
var data = new Dictionary<string, string>
{
    ["Price"]    = "100",
    ["Discount"] = "15",
    ["Qty"]      = "3",
    ["TaxRate"]  = "0.07",
};

RuleEvaluator.Evaluate("[Price] - [Discount]", data);                // 85
RuleEvaluator.Evaluate("[Price] * [Qty]", data);                     // 300
RuleEvaluator.Evaluate("([Price] * [Qty]) * (1 + [TaxRate])", data); // 321.0
RuleEvaluator.Evaluate("[Price] - [Discount] > 80", data);           // true
```

NCalc built-in math functions (`Sqrt`, `Pow`, `Abs`, `Floor`, `Ceiling`, `Round`, `Max`, `Min`, `Log`, `Sin`, `Cos`, `Tan`, …) are all available:

```csharp
RuleEvaluator.Evaluate("Sqrt([Price])", data);    // 10.0
RuleEvaluator.Evaluate("Pow([Price], 2)", data);  // 10000.0
```

### 3. String Comparison

String literals in expressions use **single quotes**.

```csharp
var data = new Dictionary<string, string>
{
    ["Status"]   = "active",
    ["Role"]     = "Admin",
    ["Country"]  = "US",
};

RuleEvaluator.Evaluate("[Status] == 'active'", data);    // true
RuleEvaluator.Evaluate("[Status] == 'inactive'", data);  // false
RuleEvaluator.Evaluate("[Role] != 'User'", data);        // true
RuleEvaluator.Evaluate("[Country] == 'US'", data);       // true
```

### 4. Boolean Logic — AND / OR / NOT

```csharp
var data = new Dictionary<string, string>
{
    ["IsVerified"] = "true",
    ["IsActive"]   = "true",
    ["IsBanned"]   = "false",
    ["Age"]        = "22",
    ["Score"]      = "55",
};

RuleEvaluator.Evaluate("[IsVerified] && [IsActive]", data);             // true
RuleEvaluator.Evaluate("[IsVerified] && ![IsBanned]", data);            // true
RuleEvaluator.Evaluate("[IsBanned] || [Score] < 60", data);             // true
RuleEvaluator.Evaluate("![IsBanned] && [Age] >= 18", data);             // true
```

#### Grouping with parentheses

Parentheses control operator precedence:

```csharp
// (A OR B) AND C
RuleEvaluator.Evaluate("([IsBanned] || [Score] < 60) && [IsVerified]", data); // true

// NOT of a grouped expression
RuleEvaluator.Evaluate("!([IsBanned] || [Age] > 30)", data);  // true  → !(false || false)

// NOT of an AND group
RuleEvaluator.Evaluate("!([IsVerified] && [IsBanned])", data); // true  → !(true && false)

// Deep nesting
RuleEvaluator.Evaluate(
    "((([IsVerified] && ![IsBanned]) && ([Age] >= 18 && [Age] <= 65)) && [Score] >= 50)",
    data); // true
```

### 5. Conditional (`if`)

```csharp
var data = new Dictionary<string, string>
{
    ["Score"]   = "75",
    ["Passing"] = "60",
};

RuleEvaluator.Evaluate("if([Score] >= [Passing], 'Pass', 'Fail')", data); // "Pass"
RuleEvaluator.Evaluate("if([Score] >= 90, 'BonusEligible', 'NoBonus')", data); // "NoBonus"
```

### 6. `in` / `not in`

Test whether a value belongs to a literal set. Use single quotes for string items.

```csharp
var numData = new Dictionary<string, string> { ["Age"] = "22", ["Score"] = "55" };

RuleEvaluator.Evaluate("[Age] in (18, 22, 25, 30)", numData);      // true
RuleEvaluator.Evaluate("[Age] in (30, 35, 40)", numData);           // false
RuleEvaluator.Evaluate("[Age] not in (30, 35, 40)", numData);       // true
RuleEvaluator.Evaluate("[Score] in (50, 55, 60)", numData);         // true

var strData = new Dictionary<string, string> { ["Status"] = "active", ["Role"] = "Admin" };

RuleEvaluator.Evaluate("[Status] in ('active', 'pending', 'review')", strData); // true
RuleEvaluator.Evaluate("[Status] not in ('banned', 'suspended')", strData);     // true
RuleEvaluator.Evaluate("[Role] in ('Admin', 'Manager', 'Owner')", strData);     // true
```

Combine with boolean operators:

```csharp
RuleEvaluator.Evaluate(
    "[Age] in (18, 22, 25) && [Score] in (50, 55, 60)",
    numData); // true

RuleEvaluator.Evaluate(
    "!([Status] in ('banned', 'suspended')) && [Role] in ('Admin', 'Manager', 'Owner')",
    strData); // true
```

### 7. Date Functions

Dates in the dictionary can be in any supported format (`yyyy-MM-dd`, `MM/dd/yyyy`, `dd-MM-yyyy`, `yyyy-MM-ddTHH:mm:ss`). The evaluator coerces them to `DateTime` automatically.

#### Available functions

| Function | Returns | Example result for `"2024-03-15"` |
|---|---|---|
| `DAY(date)` | Day of month (1–31) | `15` |
| `WEEK(date)` | Day of week — 0=Sun … 6=Sat | `5` (Friday) |
| `MONTH(date)` | Month (1–12) | `3` |
| `YEAR(date)` | Four-digit year | `2024` |
| `DATEDIFF(d1, d2)` | `(d1 − d2)` total days (`double`) | — |

```csharp
var data = new Dictionary<string, string>
{
    ["StartDate"] = "2024-03-15",  // Friday
    ["EndDate"]   = "2024-03-20",  // Wednesday
    ["Birthday"]  = "1990-07-04",
};

// Extract parts
RuleEvaluator.Evaluate("DAY([StartDate]) == 15", data);    // true
RuleEvaluator.Evaluate("MONTH([StartDate]) == 3", data);   // true
RuleEvaluator.Evaluate("YEAR([Birthday]) == 1990", data);  // true

// Day-of-week checks (0=Sun, 1=Mon … 5=Fri, 6=Sat)
RuleEvaluator.Evaluate("WEEK([StartDate]) == 5", data);    // true  (Friday)
RuleEvaluator.Evaluate(
    "WEEK([StartDate]) >= 1 && WEEK([StartDate]) <= 5",
    data); // true — is a weekday

// Date subtraction
RuleEvaluator.Evaluate("DATEDIFF([EndDate], [StartDate]) == 5", data);   // true
RuleEvaluator.Evaluate("DATEDIFF([StartDate], [EndDate]) == -5", data);  // true (negative)
RuleEvaluator.Evaluate("DATEDIFF([EndDate], [StartDate]) > 0", data);    // true

// Native DateTime comparison
RuleEvaluator.Evaluate("[StartDate] < [EndDate]", data);   // true
RuleEvaluator.Evaluate("[StartDate] != [EndDate]", data);  // true

// Complex: valid booking window on a weekday
RuleEvaluator.Evaluate(
    "WEEK([StartDate]) >= 1 && WEEK([StartDate]) <= 5 && DATEDIFF([EndDate], [StartDate]) <= 7",
    data); // true
```

### 8. Mixed Rules

Combine any types in a single expression:

```csharp
var data = new Dictionary<string, string>
{
    ["Age"]        = "30",
    ["Status"]     = "active",
    ["Score"]      = "92",
    ["IsVerified"] = "true",
    ["Tier"]       = "gold",
    ["Balance"]    = "1500",
};

// Eligibility check
RuleEvaluator.Evaluate(
    "[Age] >= 21 && [Status] == 'active' && [Score] >= 90",
    data); // true

// High-value customer
RuleEvaluator.Evaluate(
    "[Balance] >= 1000 && [Tier] == 'gold' && [IsVerified]",
    data); // true

// Discount eligible
RuleEvaluator.Evaluate(
    "[Score] > 85 || [Tier] == 'gold'",
    data); // true
```

---

## Error Handling

`RuleEvaluator.Evaluate` never throws. On failure it returns an `EvaluationError`:

| Scenario | Behaviour |
|---|---|
| Missing parameter | Returns `EvaluationError("Parameter X not defined.")` |
| Division by zero | Returns `∞` (double positive infinity) |
| Malformed expression | Returns `EvaluationError("Parse error: …")` |
| Invalid date conversion | Returns `EvaluationError("Cannot convert '…' to DateTime.")` |

```csharp
// Safe pattern
var result = RuleEvaluator.Evaluate(someExpression, data);
bool ok = result is not EvaluationError;
```

---

## Project Structure

```
TestRule/
├── TestRule.csproj      # .NET 8 console app, references NCalcSync 5.12.0
├── RuleEvaluator.cs     # Evaluator — coercion, date functions, error wrapping
├── Program.cs           # 107 test cases across 10 sections
└── README.md
```
