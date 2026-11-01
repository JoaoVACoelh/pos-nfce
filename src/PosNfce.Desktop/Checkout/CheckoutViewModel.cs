using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosNfce.Checkout.Application.Sales;
using PosNfce.Checkout.Domain.Sales;
using PosNfce.Desktop.Fiscal.Documents;
using PosNfce.Desktop.Shared;
using PosNfce.SharedKernel;

namespace PosNfce.Desktop.Checkout;

/// <summary>Which field should have the keyboard after an action. The view moves the focus.</summary>
public enum CheckoutFocus
{
    Scan,
    Discount,
    PaymentAmount,
    Cpf,
    Quantity,
}

/// <summary>The option Enter runs when the code field is empty. ← → move it; the highlighted button shows which one.</summary>
public enum CheckoutAction
{
    None,
    Finish,
    Discount,
    Cpf,
    Cancel,
    PrintNfce,
}

/// <summary>
/// Checkout screen, made to be used with the keyboard and the scanner only:
/// scan → F2 to pay → F5–F8 picks the method → Enter adds the payment; the payment that settles the total closes the sale.
/// </summary>
public sealed partial class CheckoutViewModel(UseCaseRunner useCases, SaleNfceViewModel saleNfce) : FormViewModel
{
    /// <summary>NFC-e situation and "Imprimir nota" in the card shown after a sale.</summary>
    public SaleNfceViewModel SaleNfce { get; } = saleNfce;

    public event Action<CheckoutFocus>? FocusRequested;

    /// <summary>"Cancelar venda" picked with the keyboard: the view asks for confirmation, as with F4.</summary>
    public event Action? CancelRequested;

    // Same order as the buttons on screen, so ← → follow what the cashier sees.
    private static readonly CheckoutAction[] ScanActions =
        [CheckoutAction.Finish, CheckoutAction.Discount, CheckoutAction.Cpf, CheckoutAction.Cancel];

    // The payment buttons, top to bottom as on screen.
    private static readonly PaymentMethod[] PaymentMethods =
        [PaymentMethod.Cash, PaymentMethod.CreditCard, PaymentMethod.DebitCard, PaymentMethod.Pix];

    public ObservableCollection<SaleItemView> Items { get; } = [];
    public ObservableCollection<PaymentView> Payments { get; } = [];

    [ObservableProperty] public partial string Scan { get; set; } = "";

    /// <summary>Starts on "Finalizar venda" while scanning; after a sale, nothing until → highlights "Imprimir nota".</summary>
    [ObservableProperty] public partial CheckoutAction HighlightedAction { get; set; } = CheckoutAction.Finish;

    /// <summary>Quantity of the next scanned item. Back to 1 after each item, like a cashier expects.</summary>
    [ObservableProperty] public partial string Quantity { get; set; } = "1";
    [ObservableProperty] public partial SaleItemView? SelectedItem { get; set; }

    [ObservableProperty] public partial decimal Subtotal { get; set; }
    [ObservableProperty] public partial decimal Discount { get; set; }
    [ObservableProperty] public partial decimal Total { get; set; }
    [ObservableProperty] public partial decimal Paid { get; set; }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ConfirmPaymentText), nameof(IsPaid), nameof(ShowsCard))]
    public partial decimal Remaining { get; set; }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ChangeText))]
    public partial decimal Change { get; set; }

    /// <summary>A product sold by KG was typed without weight: the code waits in its field while the cashier types the weight.</summary>
    private bool _askingWeight;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanFinish), nameof(IsPaid), nameof(ShowsCard))]
    public partial bool IsPaying { get; set; }
    [ObservableProperty] public partial bool IsEditingDiscount { get; set; }
    [ObservableProperty] public partial string DiscountText { get; set; } = "";

    /// <summary>"CPF na nota" already set on the sale, formatted; empty when none.</summary>
    [ObservableProperty] public partial string CustomerCpf { get; set; } = "";
    [ObservableProperty] public partial bool IsEditingCpf { get; set; }
    [ObservableProperty] public partial string CpfText { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PaymentMethodLabel))]
    public partial PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

    [ObservableProperty] public partial string PaymentAmount { get; set; } = "";

    /// <summary>Title of the "venda concluída" card ("Venda concluída: R$ 53,20."); empty hides the card.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLastSale), nameof(ShowsCard))]
    public partial string LastSale { get; set; } = "";

    /// <summary>"TROCO R$ 6,80" in the card; empty when there is no change.</summary>
    [ObservableProperty] public partial string LastChange { get; set; } = "";

    public bool HasLastSale => LastSale.Length > 0;

    /// <summary>
    /// Paid but still open: the last payment normally closes the sale, so this only happens when closing failed (CPF
    /// required) or the app reopened. The "Pago" card replaces the message line until Enter closes the sale.
    /// </summary>
    public bool IsPaid => IsPaying && HasPayments && Remaining == 0;

    /// <summary>"TROCO R$ 4,00" while the sale is paid; empty without change.</summary>
    public string ChangeText => Change > 0 ? $"TROCO {new Money(Change)}" : "";

    /// <summary>A card ("Pago" or "Venda concluída") takes the place of the buttons and the message line.</summary>
    public bool ShowsCard => IsPaid || HasLastSale;

    public string PaymentMethodLabel => Label(PaymentMethod);

    public bool HasItems => Items.Count > 0;

    public bool HasPayments => Payments.Count > 0;

    /// <summary>Shows the Finish / Discount / Cancel buttons while items are being scanned.</summary>
    public bool CanFinish => HasItems && !IsPaying;

    public string ConfirmPaymentText => Remaining > 0 ? "Lançar pagamento  (Enter)" : "Concluir venda  (Enter)";

    public static string Label(PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "Dinheiro",
        PaymentMethod.CreditCard => "Cartão de crédito",
        PaymentMethod.DebitCard => "Cartão de débito",
        PaymentMethod.Pix => "PIX",
        _ => method.ToString(),
    };

    public async Task LoadAsync()
    {
        Show(await useCases.RunAsync<GetCurrentSale, SaleView>(u => u.ExecuteAsync()));
        IsPaying = Payments.Count > 0;
        await SaleNfce.RefreshUnsentAsync();
        FocusRequested?.Invoke(IsPaying ? CheckoutFocus.PaymentAmount : CheckoutFocus.Scan);
    }

    /// <summary>Enter in the code field: adds the item; with nothing typed, runs the highlighted option instead.</summary>
    [RelayCommand]
    private Task AddItemAsync() =>
        Scan.Trim().Length == 0 && (CanFinish || HasLastSale) ? RunHighlightedActionAsync() : AddScannedItemAsync();

    private Task AddScannedItemAsync() => RunAsync(async () =>
    {
        if (IsPaying)
        {
            throw new DomainException("A venda está em pagamento. Tecle Esc para voltar aos itens.");
        }

        // "3*code" typed in the scan field still works and wins over the quantity field.
        var quantity = Quantity.Trim();
        var plainCode = Scan.Contains('*') || quantity.Length == 0 || (quantity == "1" && !_askingWeight);
        var typed = plainCode ? Scan : $"{quantity}*{Scan}";
        var code = Scan;
        Scan = "";
        ClearLastSale();
        try
        {
            Show(await useCases.RunAsync<AddItemToSale, SaleView>(u => u.ExecuteAsync(typed)));
        }
        catch (WeightRequiredException error)
        {
            Scan = code;
            Quantity = "";
            _askingWeight = true;
            ShowError(error.Message);
            return;
        }

        _askingWeight = false;
        SelectedItem = Items.LastOrDefault();
        Quantity = "1";
        HighlightedAction = CheckoutAction.Finish;
        Message = "";
    });

    /// <summary>← → in the empty code field: moves the highlight among the options of the current step.</summary>
    public void MoveHighlight(int step)
    {
        if (CanFinish)
        {
            var index = Array.IndexOf(ScanActions, HighlightedAction);
            var next = index < 0 ? 0 : (index + step + ScanActions.Length) % ScanActions.Length;
            HighlightedAction = ScanActions[next];
        }
        else if (HasLastSale)
        {
            // The only option after a sale. Having to highlight it first keeps a stray Enter from printing twice.
            HighlightedAction = CheckoutAction.PrintNfce;
        }
    }

    /// <summary>↑ ↓ in the amount field: picks the payment method in the list, like F5–F8.</summary>
    public void MovePaymentMethod(int step)
    {
        if (!IsPaying || IsPaid)
        {
            return;
        }

        var index = Array.IndexOf(PaymentMethods, PaymentMethod);
        PaymentMethod = PaymentMethods[Math.Clamp(index + step, 0, PaymentMethods.Length - 1)];
    }

    private async Task RunHighlightedActionAsync()
    {
        switch (HighlightedAction)
        {
            case CheckoutAction.Finish when CanFinish:
                await StartPaymentAsync();
                break;
            case CheckoutAction.Discount when CanFinish:
                EditDiscount();
                break;
            case CheckoutAction.Cpf when CanFinish:
                EditCpf();
                break;
            case CheckoutAction.Cancel when CanFinish:
                CancelRequested?.Invoke();
                break;
            case CheckoutAction.PrintNfce when HasLastSale:
                // One Enter, one print: the next print needs → again.
                HighlightedAction = CheckoutAction.None;
                if (SaleNfce.PrintCommand.CanExecute(null))
                {
                    await SaleNfce.PrintCommand.ExecuteAsync(null);
                }

                break;
        }
    }

    /// <summary>F11: jumps to the quantity field.</summary>
    [RelayCommand]
    private void EditQuantity()
    {
        if (!IsPaying)
        {
            FocusRequested?.Invoke(CheckoutFocus.Quantity);
        }
    }

    /// <summary>Enter in the quantity field: adds the item waiting for its weight, or goes on to the scan field.</summary>
    [RelayCommand]
    private Task ConfirmQuantityAsync()
    {
        if (_askingWeight)
        {
            return AddScannedItemAsync();
        }

        FocusRequested?.Invoke(CheckoutFocus.Scan);
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task RemoveSelectedItemAsync() => RunAsync(async () =>
    {
        var item = SelectedItem ?? throw new DomainException("Selecione o item com as setas ↑ ↓ antes de remover.");
        Show(await useCases.RunAsync<RemoveItemFromSale, SaleView>(u => u.ExecuteAsync(item.Id)));
        SelectedItem = Items.LastOrDefault();
        ShowSuccess($"Item {item.Line} removido: {item.Description}.");
    }, CheckoutFocus.Scan);

    public void MoveSelection(int delta)
    {
        if (Items.Count == 0)
        {
            return;
        }

        var index = SelectedItem is null ? Items.Count - 1 : Items.IndexOf(SelectedItem) + delta;
        SelectedItem = Items[Math.Clamp(index, 0, Items.Count - 1)];
    }

    [RelayCommand]
    private void EditDiscount()
    {
        if (!HasItems || IsPaying)
        {
            return;
        }

        IsEditingDiscount = true;
        DiscountText = Discount > 0 ? Discount.ToString("0.00", Brazilian) : "";
        FocusRequested?.Invoke(CheckoutFocus.Discount);
    }

    /// <summary>F10: asks for the customer's CPF. Can be done at any moment before the sale closes, even while paying.</summary>
    [RelayCommand]
    private void EditCpf()
    {
        if (!HasItems)
        {
            return;
        }

        IsEditingCpf = true;
        CpfText = CustomerCpf;
        FocusRequested?.Invoke(CheckoutFocus.Cpf);
    }

    [RelayCommand]
    private Task ApplyCpfAsync() => RunAsync(async () =>
    {
        var typed = CpfText;
        Show(await useCases.RunAsync<IdentifySaleCustomer, SaleView>(u => u.ExecuteAsync(typed)));
        IsEditingCpf = false;
        HighlightedAction = CheckoutAction.Finish;
        Message = "";
    }, IsPaying ? CheckoutFocus.PaymentAmount : CheckoutFocus.Scan);

    [RelayCommand]
    private Task ApplyDiscountAsync() => RunAsync(async () =>
    {
        var amount = DiscountText.Trim().Length == 0 ? 0m : ParseDecimal(DiscountText, "Desconto");
        Show(await useCases.RunAsync<ApplySaleDiscount, SaleView>(u => u.ExecuteAsync(amount)));
        IsEditingDiscount = false;
        HighlightedAction = CheckoutAction.Finish;
        Message = "";
    }, CheckoutFocus.Scan);

    /// <summary>F2: opens the payment panel; pressed again when everything is paid, closes the sale.</summary>
    [RelayCommand]
    private async Task StartPaymentAsync()
    {
        if (!HasItems)
        {
            ShowError("Leia pelo menos um produto antes de finalizar.");
            return;
        }

        if (IsPaying && Remaining == 0)
        {
            await RunAsync(CompleteAsync, CheckoutFocus.Scan);
            return;
        }

        OpenPayment();
    }

    private void OpenPayment()
    {
        IsPaying = true;
        IsEditingDiscount = false;
        PaymentAmount = Remaining.ToString("0.00", Brazilian);
        Message = "";
        FocusRequested?.Invoke(CheckoutFocus.PaymentAmount);
    }

    [RelayCommand]
    private void SelectPaymentMethod(PaymentMethod method)
    {
        if (!HasItems)
        {
            return;
        }

        if (!IsPaying)
        {
            OpenPayment();
        }

        PaymentMethod = method;
        FocusRequested?.Invoke(CheckoutFocus.PaymentAmount);
    }

    /// <summary>Enter in the amount field: adds the payment; the one that settles the total also closes the sale.</summary>
    [RelayCommand]
    private Task ConfirmPaymentAsync() => RunAsync(async () =>
    {
        if (Remaining == 0)
        {
            // A sale left paid (see IsPaid): a code read here is the next customer's first item.
            var nextCode = PaymentAmount.Trim();
            await CompleteAsync();
            if (nextCode.Length > 0)
            {
                Scan = nextCode;
                await AddScannedItemAsync();
            }

            return;
        }

        var amount = ParseDecimal(PaymentAmount, "Valor");
        try
        {
            Show(await useCases.RunAsync<AddSalePayment, SaleView>(u => u.ExecuteAsync(PaymentMethod, amount)));
        }
        catch (DomainException)
        {
            PaymentAmount = Remaining.ToString("0.00", Brazilian);
            throw;
        }

        PaymentMethod = PaymentMethod.Cash;
        PaymentAmount = Remaining > 0 ? Remaining.ToString("0.00", Brazilian) : "";
        Message = "";
        IsError = false;
        if (Remaining == 0)
        {
            await CompleteAsync();
        }
    });

    [RelayCommand]
    private Task RemoveLastPaymentAsync() => RunAsync(async () =>
    {
        var payment = Payments.LastOrDefault() ?? throw new DomainException("Não há pagamentos para remover.");
        Show(await useCases.RunAsync<RemoveSalePayment, SaleView>(u => u.ExecuteAsync(payment.Id)));
        PaymentAmount = Remaining.ToString("0.00", Brazilian);
        ShowSuccess($"Pagamento de {new Money(payment.Amount)} em {Label(payment.Method)} removido.");
    }, CheckoutFocus.PaymentAmount);

    /// <summary>Esc: leaves discount or payment mode. Payments must be removed first (F9) to change items.</summary>
    [RelayCommand]
    private void Back()
    {
        if (_askingWeight)
        {
            _askingWeight = false;
            Scan = "";
            Quantity = "1";
            Message = "";
            FocusRequested?.Invoke(CheckoutFocus.Scan);
            return;
        }

        if (IsEditingCpf)
        {
            IsEditingCpf = false;
            FocusRequested?.Invoke(IsPaying ? CheckoutFocus.PaymentAmount : CheckoutFocus.Scan);
            return;
        }

        if (IsEditingDiscount)
        {
            IsEditingDiscount = false;
        }
        else if (IsPaying && Payments.Count == 0)
        {
            IsPaying = false;
        }
        else if (IsPaying)
        {
            ShowError("Há pagamentos lançados. Remova-os com F9 para voltar aos itens.");
            return;
        }
        else if (HasLastSale)
        {
            HighlightedAction = CheckoutAction.None;
        }

        Message = "";
        FocusRequested?.Invoke(CheckoutFocus.Scan);
    }

    /// <summary>Called by the view after the cashier confirms the cancellation.</summary>
    public Task CancelSaleAsync() => RunAsync(async () =>
    {
        await useCases.RunAsync<CancelSale>(u => u.ExecuteAsync());
        Show(SaleView.Empty);
        IsPaying = IsEditingDiscount = IsEditingCpf = false;
        Quantity = "1";
        HighlightedAction = CheckoutAction.Finish;
        ShowSuccess("Venda cancelada.");
    }, CheckoutFocus.Scan);

    private async Task CompleteAsync()
    {
        var completed = await useCases.RunAsync<CompleteSale, CompletedSale>(u => u.ExecuteAsync());
        Show(SaleView.Empty);
        IsPaying = false;
        LastSale = $"Venda concluída: {new Money(completed.Total)}.";
        LastChange = completed.Change > 0 ? $"TROCO {new Money(completed.Change)}" : "";
        HighlightedAction = CheckoutAction.None;
        Message = "";
        // A failed NFC-e or print shows in the card, next to the button that retries it.
        await SaleNfce.LoadAsync(completed.SaleId, completed.AfterSaleWarning);

        FocusRequested?.Invoke(CheckoutFocus.Scan);
    }

    private void ClearLastSale()
    {
        LastSale = LastChange = "";
        SaleNfce.Clear();
    }

    private async Task RunAsync(Func<Task> action, CheckoutFocus? focusAfter = null)
    {
        try
        {
            await action();
        }
        catch (DomainException error)
        {
            ShowError(error.Message);
        }

        FocusRequested?.Invoke(
            _askingWeight ? CheckoutFocus.Quantity
            : IsEditingCpf ? CheckoutFocus.Cpf
            : IsEditingDiscount ? CheckoutFocus.Discount
            : focusAfter ?? (IsPaying ? CheckoutFocus.PaymentAmount : CheckoutFocus.Scan));
    }

    private void Show(SaleView sale)
    {
        Items.Clear();
        foreach (var item in sale.Items)
        {
            Items.Add(item);
        }

        Payments.Clear();
        foreach (var payment in sale.Payments)
        {
            Payments.Add(payment);
        }

        (Subtotal, Discount, Total, Paid, Remaining, Change) =
            (sale.Subtotal, sale.Discount, sale.Total, sale.Paid, sale.Remaining, sale.Change);
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(HasPayments));
        OnPropertyChanged(nameof(IsPaid));
        OnPropertyChanged(nameof(ShowsCard));
        OnPropertyChanged(nameof(CanFinish));
        CustomerCpf = sale.CustomerCpf ?? "";
    }
}
