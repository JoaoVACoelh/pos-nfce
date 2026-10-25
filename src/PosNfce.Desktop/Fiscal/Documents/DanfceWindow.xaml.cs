using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using PosNfce.Fiscal.Application.Documents;
using QRCoder;

namespace PosNfce.Desktop.Fiscal.Documents;

/// <summary>
/// Shows the DANFCE as it will come out of the receipt printer, and prints it as an image on any other Windows printer.
/// The receipt printer itself gets ESC/POS commands ("Imprimir DANFCE" in "Notas fiscais").
/// </summary>
public partial class DanfceWindow : Window
{
    private readonly Danfce _danfce;

    public DanfceWindow(Danfce danfce)
    {
        InitializeComponent();
        _danfce = danfce;
        Render();
    }

    private void Render()
    {
        var font = new FontFamily("Consolas");
        foreach (var line in _danfce.Lines)
        {
            if (line.Text == Danfce.QrCodePlaceholder)
            {
                ReceiptLines.Children.Add(QrImage());
                continue;
            }

            var centered = line.Style is DanfceStyle.Centered or DanfceStyle.CenteredBold;
            ReceiptLines.Children.Add(new TextBlock
            {
                Text = line.Text,
                FontFamily = font,
                // Same size for every line: a smaller font would break the 48-column alignment, as on the printer.
                FontSize = 12,
                FontWeight = line.Style is DanfceStyle.Bold or DanfceStyle.CenteredBold ? FontWeights.Bold : FontWeights.Normal,
                Foreground = line.Style is DanfceStyle.Rule or DanfceStyle.Small ? Brushes.DimGray : Brushes.Black,
                TextAlignment = centered ? TextAlignment.Center : TextAlignment.Left,
                TextWrapping = centered ? TextWrapping.Wrap : TextWrapping.NoWrap,
                // One Consolas 12 character is about 6.6 px: 48 of them ≈ 80 mm paper.
                Width = _danfce.Width * 6.6,
            });
        }
    }

    private Image QrImage()
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(_danfce.QrCode, QRCodeGenerator.ECCLevel.M);
        var png = new PngByteQRCode(data).GetGraphic(pixelsPerModule: 6);

        var bitmap = new BitmapImage();
        using (var stream = new MemoryStream(png))
        {
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
        }

        return new Image { Source = bitmap, Width = 160, Height = 160, Margin = new Thickness(0, 8, 0, 8) };
    }

    private void OnPrint(object sender, RoutedEventArgs e)
    {
        var dialog = new PrintDialog();
        if (dialog.ShowDialog() == true)
        {
            dialog.PrintVisual(Receipt, "DANFCE");
        }
    }

    private void OnSaveImage(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { Title = "Salvar DANFCE", Filter = "Imagem PNG (*.png)|*.png", FileName = "danfce.png" };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var bitmap = new RenderTargetBitmap((int)Receipt.ActualWidth * 2, (int)Receipt.ActualHeight * 2, 192, 192, PixelFormats.Pbgra32);
        bitmap.Render(Receipt);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(dialog.FileName);
        encoder.Save(file);
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
