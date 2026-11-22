using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using okrlab_2;

namespace okrlab_2.Tests;

/// <summary>
/// Допоміжні методи: Form1 — це Windows Forms, тому її треба створювати в STA-потоці,
/// а приватні поля (balance, placedBets ...) читати через reflection.
/// </summary>
internal static class FormHelper
{
    private const BindingFlags Any = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;

    /// <summary>Виконує тест в STA-потоці з готовою формою Form1.</summary>
    public static void Run(Action<Form1> test)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                using var form = new Form1();
                test(form);
            }
            catch (Exception ex) { error = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (error != null) ExceptionDispatchInfo.Capture(error).Throw();
    }

    public static T Get<T>(Form1 f, string field) =>
        (T)typeof(Form1).GetField(field, Any)!.GetValue(f)!;

    public static void Set(Form1 f, string field, object value) =>
        typeof(Form1).GetField(field, Any)!.SetValue(f, value);

    public static int Balance(Form1 f) => Get<int>(f, "balance");
    public static void SetBalance(Form1 f, int v) => Set(f, "balance", v);
    public static void SetChip(Form1 f, int v) => Set(f, "selectedChip", v);

    public static Dictionary<Button, int> PlacedBets(Form1 f) => Get<Dictionary<Button, int>>(f, "placedBets");
    public static Dictionary<Button, string> BetTypes(Form1 f) => Get<Dictionary<Button, string>>(f, "betTypes");

    /// <summary>Знаходить кнопку на столі за типом ставки: "RED", "17", "1st12", "COL_2" ...</summary>
    public static Button ButtonFor(Form1 f, string betType) =>
        BetTypes(f).First(p => p.Value == betType).Key;

    /// <summary>Натискає кнопку ставки (виклик обробника кліку з Form1).</summary>
    public static void ClickBet(Form1 f, string betType)
    {
        var btn = ButtonFor(f, betType);
        typeof(Form1).GetMethod("OnBetButtonClick", Any)!.Invoke(f, new object[] { btn, EventArgs.Empty });
    }

    /// <summary>Сума поставлена на тип ставки (0, якщо ставки немає).</summary>
    public static int BetOn(Form1 f, string betType) =>
        PlacedBets(f).TryGetValue(ButtonFor(f, betType), out var v) ? v : 0;

    // ---- MessageBox блокує тест, тому його автоматично закриває фоновий таймер ----

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string? cls, string? title);
    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr w, IntPtr l);
    private const uint WM_CLOSE = 0x0010;

    /// <summary>Виконує дію, яка може показати MessageBox, і закриває його. Повертає true, якщо вікно з'явилось.</summary>
    public static bool RunAndDismissMessageBox(Action action)
    {
        bool shown = false;
        using var timer = new System.Threading.Timer(_ =>
        {
            var hwnd = FindWindow("#32770", null);   // клас стандартного діалогу
            if (hwnd != IntPtr.Zero)
            {
                shown = true;
                PostMessage(hwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            }
        }, null, 200, 200);
        action();
        return shown;
    }
}
