using Xunit;

namespace okrlab_2.Tests;

/// <summary>Тести логіки рулетки у Form1 (баланс, ставки, виплати).</summary>
public class Form1Tests
{
    // ---------- 1. Початковий баланс ----------

    [Fact]
    public void NewGame_BalanceIs1000() =>
        FormHelper.Run(f => Assert.Equal(1000, FormHelper.Balance(f)));

    [Fact]
    public void NewGame_BalanceLabelShows1000() =>
        FormHelper.Run(f => Assert.Contains("1000", f.Controls.Find("label1", true)[0].Text));

    [Fact]
    public void NewGame_NoBetsPlaced() =>
        FormHelper.Run(f => Assert.Empty(FormHelper.PlacedBets(f)));

    [Fact]
    public void NewGame_DefaultChipIs10() =>
        FormHelper.Run(f => Assert.Equal(10, FormHelper.Get<int>(f, "selectedChip")));

    [Fact]
    public void BetTable_Has49BettingPositions() =>
        // 0 + числа 1–36 + 3 колонки + 3 дюжини + 6 зовнішніх ставок
        FormHelper.Run(f => Assert.Equal(49, FormHelper.BetTypes(f).Count));

    // ---------- 2. Успішна ставка ----------

    [Fact]
    public void PlaceBet_SubtractsChipFromBalance() => FormHelper.Run(f =>
    {
        FormHelper.ClickBet(f, "RED");
        Assert.Equal(990, FormHelper.Balance(f));
    });

    [Fact]
    public void PlaceBet_StoresBetOnClickedButton() => FormHelper.Run(f =>
    {
        FormHelper.ClickBet(f, "17");
        Assert.Single(FormHelper.PlacedBets(f));
        Assert.Equal(10, FormHelper.BetOn(f, "17"));
    });

    [Theory]
    [InlineData(10)]
    [InlineData(25)]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(500)]
    public void PlaceBet_AllChipValues_SubtractCorrectly(int chip) => FormHelper.Run(f =>
    {
        FormHelper.SetChip(f, chip);
        FormHelper.ClickBet(f, "BLACK");
        Assert.Equal(1000 - chip, FormHelper.Balance(f));
        Assert.Equal(chip, FormHelper.BetOn(f, "BLACK"));
    });

    [Fact]
    public void PlaceBet_SamePositionTwice_AddsUp() => FormHelper.Run(f =>
    {
        FormHelper.ClickBet(f, "RED");
        FormHelper.SetChip(f, 25);
        FormHelper.ClickBet(f, "RED");
        Assert.Single(FormHelper.PlacedBets(f));
        Assert.Equal(35, FormHelper.BetOn(f, "RED"));
        Assert.Equal(965, FormHelper.Balance(f));
    });

    [Fact]
    public void PlaceBet_DifferentPositions_AreSeparate() => FormHelper.Run(f =>
    {
        FormHelper.ClickBet(f, "RED");
        FormHelper.ClickBet(f, "ODD");
        FormHelper.ClickBet(f, "5");
        Assert.Equal(3, FormHelper.PlacedBets(f).Count);
        Assert.Equal(970, FormHelper.Balance(f));
    });

    // ---------- 3. Граничні випадки ----------

    [Fact]
    public void PlaceBet_ChipEqualsBalance_IsAllowed() => FormHelper.Run(f =>
    {
        FormHelper.SetBalance(f, 100);
        FormHelper.SetChip(f, 100);
        FormHelper.ClickBet(f, "RED");
        Assert.Equal(0, FormHelper.Balance(f));
        Assert.Equal(100, FormHelper.BetOn(f, "RED"));
    });

    [Fact]
    public void PlaceBet_ChipGreaterThanBalance_IsRejected() => FormHelper.Run(f =>
    {
        FormHelper.SetBalance(f, 50);
        FormHelper.SetChip(f, 100);
        bool warned = FormHelper.RunAndDismissMessageBox(() => FormHelper.ClickBet(f, "RED"));
        Assert.True(warned, "Має з'явитись повідомлення про нестачу коштів");
        Assert.Equal(50, FormHelper.Balance(f));
        Assert.Empty(FormHelper.PlacedBets(f));
    });

    [Fact]
    public void PlaceBet_BalanceZero_IsRejected() => FormHelper.Run(f =>
    {
        FormHelper.SetBalance(f, 0);
        bool warned = FormHelper.RunAndDismissMessageBox(() => FormHelper.ClickBet(f, "RED"));
        Assert.True(warned);
        Assert.Equal(0, FormHelper.Balance(f));
        Assert.Empty(FormHelper.PlacedBets(f));
    });

    [Fact]
    public void PlaceBet_SecondBetExceedsRemainingBalance_IsRejected() => FormHelper.Run(f =>
    {
        FormHelper.SetBalance(f, 100);
        FormHelper.SetChip(f, 100);
        FormHelper.ClickBet(f, "RED");                 // баланс 0
        FormHelper.SetChip(f, 10);
        FormHelper.RunAndDismissMessageBox(() => FormHelper.ClickBet(f, "BLACK"));
        Assert.Equal(0, FormHelper.Balance(f));
        Assert.Equal(0, FormHelper.BetOn(f, "BLACK"));
    });

    [Fact]
    public void PlaceBet_WhileSpinning_IsIgnored() => FormHelper.Run(f =>
    {
        FormHelper.Set(f, "isSpinning", true);
        FormHelper.ClickBet(f, "RED");
        Assert.Equal(1000, FormHelper.Balance(f));
        Assert.Empty(FormHelper.PlacedBets(f));
    });

    // Ставка 0 або від'ємна в інтерфейсі неможлива: розмір ставки — це номінал фішки (10/25/50/100/500).
    [Fact]
    public void Chips_AreAlwaysPositive() => FormHelper.Run(f =>
    {
        var select = typeof(Form1).GetMethod("SelectChip",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var pb = f.Controls.Find("pictureBox2", true)[0];
        foreach (int chip in new[] { 10, 25, 50, 100, 500 })
        {
            select.Invoke(f, new object[] { chip, pb });
            Assert.True(FormHelper.Get<int>(f, "selectedChip") > 0);
        }
    });

    // ---------- 4. Виграш / програш ----------

    [Fact]
    public void Payout_StraightWin_Returns36xBet() => FormHelper.Run(f =>
    {
        FormHelper.ClickBet(f, "17");
        int payout = f.CalculateSpinPayout(17);
        Assert.Equal(360, payout);
        Assert.Equal(1350, FormHelper.Balance(f));   // 1000 - 10 + 360
    });

    [Fact]
    public void Payout_StraightLose_Returns0_BetLost() => FormHelper.Run(f =>
    {
        FormHelper.ClickBet(f, "17");
        int payout = f.CalculateSpinPayout(18);
        Assert.Equal(0, payout);
        Assert.Equal(990, FormHelper.Balance(f));
    });

    [Fact]
    public void Payout_BetOnZero_WinsOnZero() => FormHelper.Run(f =>
    {
        FormHelper.ClickBet(f, "0");
        Assert.Equal(360, f.CalculateSpinPayout(0));
    });

    // тип ставки, випале число, очікувана виплата при ставці 10
    [Theory]
    [InlineData("RED", 1, 20)]
    [InlineData("RED", 2, 0)]
    [InlineData("BLACK", 2, 20)]
    [InlineData("BLACK", 1, 0)]
    [InlineData("EVEN", 2, 20)]
    [InlineData("EVEN", 3, 0)]
    [InlineData("ODD", 3, 20)]
    [InlineData("ODD", 4, 0)]
    [InlineData("1to18", 1, 20)]
    [InlineData("1to18", 18, 20)]
    [InlineData("1to18", 19, 0)]
    [InlineData("19to36", 19, 20)]
    [InlineData("19to36", 36, 20)]
    [InlineData("19to36", 18, 0)]
    [InlineData("1st12", 1, 30)]
    [InlineData("1st12", 12, 30)]
    [InlineData("1st12", 13, 0)]
    [InlineData("2st12", 13, 30)]
    [InlineData("2st12", 24, 30)]
    [InlineData("2st12", 25, 0)]
    [InlineData("3st12", 25, 30)]
    [InlineData("3st12", 36, 30)]
    [InlineData("3st12", 24, 0)]
    [InlineData("COL_1", 1, 30)]
    [InlineData("COL_1", 34, 30)]
    [InlineData("COL_1", 2, 0)]
    [InlineData("COL_2", 2, 30)]
    [InlineData("COL_2", 35, 30)]
    [InlineData("COL_2", 3, 0)]
    [InlineData("COL_3", 3, 30)]
    [InlineData("COL_3", 36, 30)]
    [InlineData("COL_3", 1, 0)]
    public void Payout_Table(string betType, int winning, int expected) => FormHelper.Run(f =>
    {
        FormHelper.ClickBet(f, betType);
        Assert.Equal(expected, f.CalculateSpinPayout(winning));
    });

    [Theory]
    [InlineData("RED")]
    [InlineData("BLACK")]
    [InlineData("EVEN")]
    [InlineData("ODD")]
    [InlineData("1to18")]
    [InlineData("19to36")]
    [InlineData("1st12")]
    [InlineData("2st12")]
    [InlineData("3st12")]
    [InlineData("COL_1")]
    [InlineData("COL_2")]
    [InlineData("COL_3")]
    public void Payout_Zero_LosesEveryOutsideBet(string betType) => FormHelper.Run(f =>
    {
        FormHelper.ClickBet(f, betType);
        Assert.Equal(0, f.CalculateSpinPayout(0));
        Assert.Equal(990, FormHelper.Balance(f));
    });

    [Fact]
    public void Payout_WinAddsToBalance() => FormHelper.Run(f =>
    {
        FormHelper.SetChip(f, 100);
        FormHelper.ClickBet(f, "RED");               // баланс 900
        f.CalculateSpinPayout(1);                    // 1 — червоне, +200
        Assert.Equal(1100, FormHelper.Balance(f));
    });

    [Fact]
    public void Payout_LoseKeepsBalanceReduced() => FormHelper.Run(f =>
    {
        FormHelper.SetChip(f, 100);
        FormHelper.ClickBet(f, "RED");
        f.CalculateSpinPayout(2);                    // 2 — чорне
        Assert.Equal(900, FormHelper.Balance(f));
    });

    [Fact]
    public void Payout_MixedBets_SumsCorrectly() => FormHelper.Run(f =>
    {
        // випаде 7: червоне, непарне, 1–18, 1st12, колонка 1
        FormHelper.SetChip(f, 100); FormHelper.ClickBet(f, "RED");     // виграш 200
        FormHelper.SetChip(f, 50);  FormHelper.ClickBet(f, "EVEN");    // програш
        FormHelper.SetChip(f, 10);  FormHelper.ClickBet(f, "7");       // виграш 360
        int payout = f.CalculateSpinPayout(7);
        Assert.Equal(560, payout);
        Assert.Equal(1400, FormHelper.Balance(f));                      // 1000 - 160 + 560
    });

    [Fact]
    public void Payout_StackedBet_UsesTotalAmount() => FormHelper.Run(f =>
    {
        FormHelper.ClickBet(f, "RED");
        FormHelper.SetChip(f, 25);
        FormHelper.ClickBet(f, "RED");
        Assert.Equal(70, f.CalculateSpinPayout(1));  // 35 * 2
    });

    [Fact]
    public void Payout_ClearsBetsAfterSpin() => FormHelper.Run(f =>
    {
        FormHelper.ClickBet(f, "RED");
        f.CalculateSpinPayout(1);
        Assert.Empty(FormHelper.PlacedBets(f));
    });

    [Fact]
    public void Payout_NoBets_ReturnsZero() => FormHelper.Run(f =>
    {
        Assert.Equal(0, f.CalculateSpinPayout(5));
        Assert.Equal(1000, FormHelper.Balance(f));
    });

    [Fact]
    public void Payout_TwoRounds_BalanceCarriesOver() => FormHelper.Run(f =>
    {
        FormHelper.SetChip(f, 100);
        FormHelper.ClickBet(f, "RED"); f.CalculateSpinPayout(1);   // 1100
        FormHelper.ClickBet(f, "RED"); f.CalculateSpinPayout(2);   // 1000
        Assert.Equal(1000, FormHelper.Balance(f));
    });

    [Fact]
    public void Payout_AllInWin_DoublesBalance() => FormHelper.Run(f =>
    {
        FormHelper.SetBalance(f, 500);
        FormHelper.SetChip(f, 500);
        FormHelper.ClickBet(f, "RED");
        f.CalculateSpinPayout(1);
        Assert.Equal(1000, FormHelper.Balance(f));
    });

    // ---------- Колесо ----------

    [Fact]
    public void RedNumbers_Are18_AndIncludeKnownValues() => FormHelper.Run(f =>
    {
        var red = FormHelper.Get<HashSet<int>>(f, "redNumbers");
        Assert.Equal(18, red.Count);
        Assert.Contains(1, red);
        Assert.Contains(36, red);
        Assert.DoesNotContain(0, red);
        Assert.DoesNotContain(2, red);
    });

    [Fact]
    public void WheelNumbers_Contain37UniqueNumbers0To36() => FormHelper.Run(f =>
    {
        var wheel = FormHelper.Get<int[]>(f, "wheelNumbers");
        Assert.Equal(37, wheel.Length);
        Assert.Equal(Enumerable.Range(0, 37), wheel.OrderBy(x => x));
    });
}
