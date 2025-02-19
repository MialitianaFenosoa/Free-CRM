﻿using System.ComponentModel;

namespace Domain.Enums;

public enum BudgetStatus
{
    [Description("Draft")]
    Draft = 0,
    [Description("Cancelled")]
    Cancelled = 1,
    [Description("Confirmed")]
    Confirmed = 2,
    [Description("Archived")]
    Archived = 3
}
