using System.Collections.ObjectModel;
using System.Windows.Input;
using StorePos.Desktop.Common;
using StorePos.Desktop.History.Dialogs;
using StorePos.Desktop.History.Models;

namespace StorePos.Desktop.History.ViewModels;

public sealed class SaleDetailsDialogViewModel : ObservableObject
{
    private readonly IHistoryDialogService _dialogService;
    private readonly CancellationToken _cancellationToken;
    private readonly RelayCommand _payDebtCommand;
    private decimal _paidAmount;
    private decimal _outstandingAmount;
    private bool _hasDebt;

    public SaleDetailsDialogViewModel(
        SaleDetailsDto sale,
        IClipboardService clipboardService,
        IHistoryDialogService dialogService,
        CancellationToken cancellationToken)
    {
        _dialogService = dialogService;
        _cancellationToken = cancellationToken;
        Id = sale.Id;
        SaleNumber = sale.SaleNumber;
        Status = sale.Status;
        CompletionVersion = sale.CompletionVersion;
        CustomerId = sale.CustomerId;
        CustomerName = sale.CustomerName;
        CustomerIdentificationNumber = sale.CustomerIdentificationNumber;
        Comment = sale.Comment;
        TotalAmount = sale.TotalAmount;
        _paidAmount = sale.PaidAmount;
        _outstandingAmount = sale.OutstandingAmount;
        _hasDebt = sale.HasDebt;
        DateCreated = sale.DateCreated;
        DateCompleted = sale.DateCompleted;
        DateCancelled = sale.DateCancelled;
        Items = sale.Items;
        Payments = new ObservableCollection<SaleDetailsPaymentDto>(
            sale.Payments.OrderBy(payment => payment.DateCreated));
        PaymentGroups = CreatePaymentGroups(sale);
        _currentPaymentGroup = PaymentGroups[0];
        CopySaleNumberCommand = new RelayCommand(
            () =>
            {
                if (!string.IsNullOrWhiteSpace(SaleNumber))
                {
                    clipboardService.TrySetText(SaleNumber);
                }
            },
            () => !string.IsNullOrWhiteSpace(SaleNumber));
        _payDebtCommand = new RelayCommand(PayDebt, CanPayDebt);
        PrintCommand = new RelayCommand(
            () => _dialogService.ShowSaleReporting(CreateCurrentSnapshot()));
    }

    public long Id { get; }
    public string SaleNumber { get; }
    public int Status { get; }
    public int CompletionVersion { get; }
    public long? CustomerId { get; }
    public string? CustomerName { get; }
    public string? CustomerIdentificationNumber { get; }
    public string? Comment { get; }
    public decimal TotalAmount { get; }
    public DateTime DateCreated { get; }
    public DateTime? DateCompleted { get; }
    public DateTime? DateCancelled { get; }
    public IReadOnlyList<SaleDetailsItemDto> Items { get; }
    public ObservableCollection<SaleDetailsPaymentDto> Payments { get; }
    public ObservableCollection<SalePaymentDisplayGroup> PaymentGroups { get; }
    public bool HasFinancialChanges { get; private set; }

    public string StatusName => Status switch
    {
        1 => "Draft",
        2 => "დასრულებული",
        3 => "გაუქმებული",
        _ => "უცნობი"
    };

    public decimal PaidAmount
    {
        get => _paidAmount;
        private set => SetProperty(ref _paidAmount, value);
    }

    public decimal OutstandingAmount
    {
        get => _outstandingAmount;
        private set => SetProperty(ref _outstandingAmount, value);
    }

    public bool HasDebt
    {
        get => _hasDebt;
        private set
        {
            if (SetProperty(ref _hasDebt, value))
            {
                _payDebtCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public ICommand CopySaleNumberCommand { get; }

    public ICommand PayDebtCommand => _payDebtCommand;

    public ICommand PrintCommand { get; }

    private bool CanPayDebt() => Status == 2 && HasDebt && OutstandingAmount > 0;

    private void PayDebt()
    {
        var result = _dialogService.ShowDebtPayment(
            Id,
            OutstandingAmount,
            _cancellationToken);
        if (result is null)
        {
            return;
        }

        PaidAmount = result.PaidAmount;
        OutstandingAmount = result.OutstandingAmount;
        HasDebt = result.HasDebt;
        Payments.Add(result.Payment);
        _currentPaymentGroup.Payments.Add(result.Payment);
        HasFinancialChanges = true;
    }

    private SaleDetailsDto CreateCurrentSnapshot()
        => new(
            Id,
            SaleNumber,
            Status,
            CompletionVersion,
            CustomerId,
            CustomerName,
            CustomerIdentificationNumber,
            Comment,
            TotalAmount,
            PaidAmount,
            OutstandingAmount,
            HasDebt,
            DateCreated,
            DateCompleted,
            DateCancelled,
            Items,
            Payments.ToArray());

    private readonly SalePaymentDisplayGroup _currentPaymentGroup;

    private static ObservableCollection<SalePaymentDisplayGroup> CreatePaymentGroups(
        SaleDetailsDto sale)
    {
        var currentPayments = sale.Status == 2
            ? sale.Payments
                .Where(payment => payment.CompletionVersion == sale.CompletionVersion)
                .OrderBy(payment => payment.DateCreated)
                .ToArray()
            : [];

        var groups = new ObservableCollection<SalePaymentDisplayGroup>
        {
            new("მოქმედი გადახდები", currentPayments)
        };

        var previousPayments = sale.Status == 2
            ? sale.Payments.Where(payment =>
                payment.CompletionVersion < sale.CompletionVersion)
            : sale.Payments;

        foreach (var versionGroup in previousPayments
                     .GroupBy(payment => payment.CompletionVersion)
                     .OrderByDescending(group => group.Key))
        {
            groups.Add(new SalePaymentDisplayGroup(
                $"წინა დასრულება #{versionGroup.Key}",
                versionGroup.OrderBy(payment => payment.DateCreated)));
        }

        return groups;
    }
}

public sealed class SalePaymentDisplayGroup
{
    public SalePaymentDisplayGroup(
        string header,
        IEnumerable<SaleDetailsPaymentDto> payments)
    {
        Header = header;
        Payments = new ObservableCollection<SaleDetailsPaymentDto>(payments);
    }

    public string Header { get; }

    public ObservableCollection<SaleDetailsPaymentDto> Payments { get; }
}
