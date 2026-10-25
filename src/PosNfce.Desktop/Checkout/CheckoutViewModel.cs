using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosNfce.Checkout.Application.Sales;
using PosNfce.Checkout.Domain.Sales;
using PosNfce.Desktop.Shared;
using PosNfce.SharedKernel;

namespace PosNfce.Desktop.Checkout;

/// <summary>Which field should have the keyboard after an action. The view moves the focus.</summary>
public enum CheckoutFocus
{
    Scan,
    Discount,
    PaymentAmount,
}

/// <summary>
/// Checkout screen, made to be used with the keyboard and the scanner only:
/// scan → F2 to pay → F5–F8 picks the method → Enter adds the payment → Enter again closes the sale.
/// </summary>
public sealed partial class CheckoutViewModel(UseCaseRunner useCases) : FormViewModel
{
    public event Action<CheckoutFocus>? FocusRequested;

    public ObservableCollection<SaleItemView> Items { get; } = [];
    public ObservableCollection<PaymentView> Payments { get; } = [];

    [ObservableProperty] public partial string Scan { get; set; } = "";
    [ObservableProperty] public partial SaleItemView? SelectedItem { get; set; }

    [ObservableProperty] public partial decimal Subtotal { get; set; }
    [ObservableProperty] public partial decimal Discount { get; set; }
    [ObservableProperty] public partial decimal Total { get; set; }
    [ObservableProperty] public partial decimal Paid { get; set; }
    [ObservableProperty] public partial decimal Remaining { get; set; }
    [ObservableProperty] public partial decimal Change { get; set; }

    [ObservableProperty] public partial bool IsPaying { get; set; }
    [ObservableProperty] public partial bool IsEditingDiscount { get; set; }
    [ObservableProperty] public partial string DiscountText { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PaymentMethodLabel))]
    public partial PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

    [ObservableProperty] public partial string PaymentAmount { get; set; } = "";

    /// <summary>Big message after a sale is closed ("Troco R$ 3,00").</summary>
    [ObservableProperty] public partial string LastSale { get; set; } = "";

    public string PaymentMethodLabel => Label(PaymentMethod);

    public bool HasItems => Items.Count > 0;

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
        FocusRequested?.Invoke(IsPaying ? CheckoutFocus.PaymentAmount : CheckoutFocus.Scan);
    }

    [RelayCommand]
    private Task AddItemAsync() => RunAsync(async () =>
    {
        if (IsPaying)
        {
            throw new DomainException("A venda está em pagamento. Tecle Esc para voltar aos itens.");
        }

        var typed = Scan;
        Scan = "";
        LastSale = "";
        Show(await useCases.RunAsync<AddItemToSale, SaleView>(u => u.ExecuteAsync(typed)));
        SelectedItem = Items.LastOrDefault();
        Message = "";
    }, CheckoutFocus.Scan);

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

    [RelayCommand]
    private Task ApplyDiscountAsync() => RunAsync(async () =>
    {
        var amount = DiscountText.Trim().Length == 0 ? 0m : ParseDecimal(DiscountText, "Desconto");
        Show(await useCases.RunAsync<ApplySaleDiscount, SaleView>(u => u.ExecuteAsync(amount)));
        IsEditingDiscount = false;
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

    /// <summary>Enter in the amount field: adds the payment, or closes the sale when nothing is left to pay.</summary>
    [RelayCommand]
    private Task ConfirmPaymentAsync() => RunAsync(async () =>
    {
        if (Remaining == 0)
        {
            await CompleteAsync();
            return;
        }

        var amount = ParseDecimal(PaymentAmount, "Valor");
        Show(await useCases.RunAsync<AddSalePayment, SaleView>(u => u.ExecuteAsync(PaymentMethod, amount)));
        PaymentMethod = PaymentMethod.Cash;
        PaymentAmount = Remaining > 0 ? Remaining.ToString("0.00", Brazilian) : "";
        Message = Remaining > 0 ? "" : "Pago. Tecle Enter para concluir a venda.";
        IsError = false;
    }, CheckoutFocus.PaymentAmount);

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

        Message = "";
        FocusRequested?.Invoke(CheckoutFocus.Scan);
    }

    /// <summary>Called by the view after the cashier confirms the cancellation.</summary>
    public Task CancelSaleAsync() => RunAsync(async () =>
    {
        await useCases.RunAsync<CancelSale>(u => u.ExecuteAsync());
        Show(SaleView.Empty);
        IsPaying = IsEditingDiscount = false;
        ShowSuccess("Venda cancelada.");
    }, CheckoutFocus.Scan);

    private async Task CompleteAsync()
    {
        var completed = await useCases.RunAsync<CompleteSale, CompletedSale>(u => u.ExecuteAsync());
        Show(SaleView.Empty);
        IsPaying = false;
        LastSale = completed.Change > 0
            ? $"Venda concluída: {new Money(completed.Total)}. TROCO {new Money(completed.Change)}"
            : $"Venda concluída: {new Money(completed.Total)}.";
        Message = "";
        FocusRequested?.Invoke(CheckoutFocus.Scan);
    }

    private async Task RunAsync(Func<Task> action, CheckoutFocus focusAfter)
    {
        try
        {
            await action();
        }
        catch (DomainException error)
        {
            ShowError(error.Message);
        }

        FocusRequested?.Invoke(IsEditingDiscount ? CheckoutFocus.Discount : focusAfter);
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
    }
}
