using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Leave;
using Vespera.Domain.Services;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.Services;

public class LeaveAccrualCalculatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CalculateMonthlyAccrual_Should_Use_The_Flat_Rate_When_No_Tenure_Tiers_Are_Configured()
    {
        var policy = CreatePolicy(accrualRatePerMonth: 1.5m);
        var employee = CreateEmployee(new DateOnly(2020, 1, 1));
        var calculator = new LeaveAccrualCalculator();

        var accrued = calculator.CalculateMonthlyAccrual(policy, employee, new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31));

        accrued.Should().Be(1.5m);
    }

    [Fact]
    public void CalculateMonthlyAccrual_Should_ProRate_A_MidMonth_Joiner()
    {
        var policy = CreatePolicy(accrualRatePerMonth: 3m);
        var employee = CreateEmployee(new DateOnly(2026, 3, 16));
        var calculator = new LeaveAccrualCalculator();

        var accrued = calculator.CalculateMonthlyAccrual(policy, employee, new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31));

        accrued.Should().Be(decimal.Round(3m * 16 / 31, 4));
    }

    [Fact]
    public void CalculateMonthlyAccrual_Should_Be_Zero_Before_The_Minimum_Tenure_Gate()
    {
        var policy = CreatePolicy(accrualRatePerMonth: 1m);
        policy.ConfigureAccrual(AccrualFrequency.Monthly, minimumTenureMonthsForAccrual: 6, tenureAccrualTiers: []);
        var employee = CreateEmployee(new DateOnly(2026, 1, 1));
        var calculator = new LeaveAccrualCalculator();

        var accrued = calculator.CalculateMonthlyAccrual(policy, employee, new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31));

        accrued.Should().Be(0m);
    }

    [Fact]
    public void CalculateMonthlyAccrual_Should_Use_The_Highest_Applicable_Tenure_Tier()
    {
        var policy = CreatePolicy(accrualRatePerMonth: 1m);
        policy.ConfigureAccrual(
            AccrualFrequency.Monthly, minimumTenureMonthsForAccrual: 0,
            tenureAccrualTiers: [new TenureAccrualTier(0, 1m), new TenureAccrualTier(24, 2m)]);
        var employee = CreateEmployee(new DateOnly(2023, 1, 1));
        var calculator = new LeaveAccrualCalculator();

        var accrued = calculator.CalculateMonthlyAccrual(policy, employee, new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31));

        accrued.Should().Be(2m);
    }

    [Fact]
    public void CalculateMonthlyAccrual_Should_Be_Zero_When_The_Range_Is_Inverted()
    {
        var policy = CreatePolicy(accrualRatePerMonth: 1.5m);
        var employee = CreateEmployee(new DateOnly(2020, 1, 1));
        var calculator = new LeaveAccrualCalculator();

        var accrued = calculator.CalculateMonthlyAccrual(policy, employee, new DateOnly(2026, 3, 31), new DateOnly(2026, 3, 1));

        accrued.Should().Be(0m);
    }

    [Fact]
    public void CalculateAnnualAccrual_Should_ProRate_A_MidYear_Joiner()
    {
        var policy = CreatePolicy(accrualRatePerMonth: 0m, annualEntitlementDays: 365m);
        var employee = CreateEmployee(new DateOnly(2026, 7, 2));
        var calculator = new LeaveAccrualCalculator();

        var accrued = calculator.CalculateAnnualAccrual(policy, employee, 2026);

        accrued.Should().Be(183m);
    }

    private static LeavePolicy CreatePolicy(decimal accrualRatePerMonth, decimal annualEntitlementDays = 18m) =>
        LeavePolicy.Create(
            TenantId.New(), LeaveTypeId.New(), annualEntitlementDays, accrualRatePerMonth,
            maxCarryForwardDays: 5m, new DateOnly(2020, 1, 1), null).Value;

    private static Employee CreateEmployee(DateOnly dateOfJoining)
    {
        var code = EmployeeCode.Create("EMP-100").Value;
        var email = EmailAddress.Create("employee@vespera.test").Value;
        var phone = PhoneNumber.Create("+14155552671").Value;

        return Employee.Onboard(
            TenantId.New(), code, "Ada", "Lovelace", email, phone,
            new DateOnly(1990, 1, 1), dateOfJoining,
            DepartmentId.New(), DesignationId.New(), LocationId.New(),
            Now, "hr@vespera.test").Value;
    }
}
