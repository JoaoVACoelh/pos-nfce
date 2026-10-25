# UI smoke test: drives the real app with Windows UI Automation, in a throwaway data folder.
# Usage (after dotnet build):
#   powershell -ExecutionPolicy Bypass -File tools\ui-smoke-test.ps1 -Exe src\PosNfce.Desktop\bin\Debug\net10.0-windows\PosNfce.Desktop.exe -Out <folder for data and screenshots>
# Saved with a BOM: Windows PowerShell 5.1 reads files without one as ANSI and breaks the accented menu names.
param([string]$Exe, [string]$Out)
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing, System.Windows.Forms

$data = Join-Path $Out "data"
Remove-Item -Recurse -Force $data -ErrorAction SilentlyContinue
$env:POSNFCE_DATA_FOLDER = $data
$proc = Start-Process -PassThru $Exe
$A = [System.Windows.Automation.AutomationElement]
$C = [System.Windows.Automation.PropertyCondition]
$Tree = [System.Windows.Automation.TreeScope]

function Wait-Window {
    for ($i = 0; $i -lt 40; $i++) {
        $w = $A::RootElement.FindFirst($Tree::Children, (New-Object $C ($A::ProcessIdProperty, $proc.Id)))
        if ($w) { return $w }
        Start-Sleep -Milliseconds 250
    }
    throw "window not found"
}
function Find($id) {
    for ($i = 0; $i -lt 20; $i++) {
        $e = $script:win.FindFirst($Tree::Descendants, (New-Object $C ($A::AutomationIdProperty, $id)))
        if ($e) { return $e }
        Start-Sleep -Milliseconds 200
    }
    throw "element $id not found"
}
function FindByName($name) {
    for ($i = 0; $i -lt 20; $i++) {
        $e = $script:win.FindFirst($Tree::Descendants, (New-Object $C ($A::NameProperty, $name)))
        if ($e) { return $e }
        Start-Sleep -Milliseconds 200
    }
    throw "element '$name' not found"
}
function Fill($id, $text) { (Find $id).GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue($text) }
function Click($e) { $e.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke(); Start-Sleep -Milliseconds 700 }
function Message { (Find "MessageText").Current.Name }
function Shot($name) {
    $r = $script:win.Current.BoundingRectangle
    $bmp = New-Object System.Drawing.Bitmap ([int]$r.Width), ([int]$r.Height)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen([int]$r.X, [int]$r.Y, 0, 0, $bmp.Size)
    $bmp.Save((Join-Path $Out "$name.png"))
    $g.Dispose(); $bmp.Dispose()
}

try {
    $script:win = Wait-Window
    Start-Sleep -Seconds 2
    Shot "1-checkout-empty"

    # Tax rule (fictitious values, test database only)
    Click (FindByName "Regras tributárias")
    Fill "NameBox" "Mercadorias em geral (TESTE)"
    Fill "CfopBox" "5102"; Fill "CsosnBox" "102"
    Fill "PisCstBox" "49"; Fill "CofinsCstBox" "49"
    Fill "IbsCbsCstBox" "000"; Fill "ClassTribBox" "000001"
    Click (FindByName "Salvar")
    "tax rule: $(Message)"
    Shot "2-tax-rule"

    # Product with an invalid GTIN first: must show an error
    Click (FindByName "Produtos")
    Fill "CodeBox" "001"; Fill "GtinBox" "7891234567894"
    Fill "DescriptionBox" "Refrigerante TESTE 350ml"; Fill "PriceBox" "5,49"; Fill "NcmBox" "2202.10.00"
    Click (FindByName "Salvar")
    "invalid gtin: $(Message)"
    Shot "3-product-error"

    # Fix the GTIN and save
    Fill "GtinBox" "7891234567895"
    Click (FindByName "Salvar")
    "product: $(Message)"

    # Search by GTIN
    Fill "SearchBox" "7891234567895"
    Click (FindByName "Buscar")
    $grid = Find "ProductsGrid"
    $rows = $grid.GetCurrentPattern([System.Windows.Automation.GridPattern]::Pattern).Current.RowCount
    "rows found by GTIN: $rows"
    $cells = 0..6 | ForEach-Object { $grid.GetCurrentPattern([System.Windows.Automation.GridPattern]::Pattern).GetItem(0, $_).Current.Name }
    "row: $($cells -join ' | ')"
    Shot "4-found-by-gtin"

    # IBPT table: fictitious sample file used by the tests
    $sample = Join-Path $PSScriptRoot "..\tests\PosNfce.Catalog.Tests\TestData\ibpt-sample.csv"
    Click (FindByName "Tabela IBPT")
    "ibpt before: $((Find 'StatusText').Current.Name)"
    Fill "FilePathBox" (Resolve-Path $sample).Path
    Click (FindByName "Importar")
    Start-Sleep -Seconds 1
    "ibpt import: $(Message)"
    "ibpt after: $((Find 'StatusText').Current.Name)"
    Shot "5-ibpt"

    # Issuer with the development certificate (dotnet run tools/create-dev-certificate.cs), fictitious data
    $certificate = Join-Path $PSScriptRoot "..\data\dev-certificate.pfx"
    Click (FindByName "Emitente")
    Fill "CnpjBox" "11.222.333/0001-81"; Fill "IeBox" "0012345678901"
    Fill "LegalNameBox" "EMPRESA DE DESENVOLVIMENTO LTDA"; Fill "TradeNameBox" "Mercadinho Teste"
    Fill "StreetBox" "Rua A"; Fill "NumberBox" "100"; Fill "DistrictBox" "Centro"
    Fill "CityNameBox" "Belo Horizonte"; Fill "CityCodeBox" "3106200"; Fill "PostalCodeBox" "30110-000"
    Fill "CscTokenIdBox" "000001"; Fill "CscBox" "ABCDEF0123456789"
    Fill "CertificatePathBox" (Resolve-Path $certificate).Path
    Fill "CertificatePasswordBox" "wrong-password"
    Click (FindByName "Salvar")
    "issuer, wrong password: $(Message)"
    Fill "CertificatePasswordBox" "dev-only"
    Click (FindByName "Salvar")
    "issuer: $(Message)"
    "certificate: $((Find 'CertificateStatusText').Current.Name)"
    "csc: $((Find 'CscStatusText').Current.Name)"
    Shot "6-issuer"

    # Leave and come back: data must be reloaded from the database, CSC field empty
    Click (FindByName "Produtos")
    Click (FindByName "Emitente")
    "reloaded trade name: $((Find 'TradeNameBox').GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).Current.Value)"
    "reloaded csc field: '$((Find 'CscBox').GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).Current.Value)'"

    # Checkout with the keyboard only (the scanner is a keyboard that types the code and Enter)
    Click (FindByName "Caixa")
    (Find "ScanBox").SetFocus()
    function Keys($keys) { [System.Windows.Forms.SendKeys]::SendWait($keys); Start-Sleep -Milliseconds 600 }
    Keys "999{ENTER}"                   # unknown code: warning, screen keeps working
    "checkout unknown: $(Message)"
    Keys "2*7891234567895{ENTER}"       # 2 x R$ 5,49 = R$ 10,98
    Keys "{F3}"; Keys "0,98{ENTER}"     # discount -> R$ 10,00
    "checkout total: $((Find 'TotalText').Current.Name)"
    Keys "{F2}"                         # payment panel, amount prefilled with what is left
    Keys "{F7}"; Keys "4{ENTER}"        # R$ 4,00 on the debit card terminal
    Keys "{F5}"; Keys "10{ENTER}"       # R$ 10,00 in cash
    "checkout remaining: $((Find 'RemainingText').Current.Name) / $(Message)"
    Shot "7-checkout-paid"
    Keys "{ENTER}"                      # close the sale
    "checkout result: $((Find 'LastSaleText').Current.Name)"
    Shot "8-checkout-done"

    # Same kind of sale with the mouse: Finish button, PIX button, confirm button
    Keys "7891234567895{ENTER}"
    Click (Find "FinishButton")
    Click (FindByName "PIX")
    Click (Find "ConfirmPaymentButton")     # adds R$ 5,49 in PIX
    "mouse sale button: $((Find 'ConfirmPaymentButton').Current.Name)"
    Click (Find "ConfirmPaymentButton")     # closes the sale
    "mouse sale result: $((Find 'LastSaleText').Current.Name)"

    # Both sales became pending NFC-e documents
    Click (FindByName "Notas fiscais")
    "fiscal documents: $((Find 'SummaryText').Current.Name)"
    Shot "8b-fiscal-documents"
    Click (FindByName "Caixa")

    # Hamburger slides the side menu closed and open (animated: the items stay in the tree, but off screen)
    function MenuVisible { ((FindByName "Produtos").Current.BoundingRectangle.Right - $script:win.Current.BoundingRectangle.Left) -gt 100 }
    Click (Find "MenuToggle")
    Start-Sleep -Milliseconds 400
    $menuHidden = -not (MenuVisible)
    Shot "9-menu-hidden"
    Click (Find "MenuToggle")
    Start-Sleep -Milliseconds 400
    "menu hidden after toggle: $menuHidden; back: $(MenuVisible)"

    # Smallest window size (1000 x 600): the checkout must still be usable
    $script:win.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).SetWindowVisualState("Normal")
    $script:win.GetCurrentPattern([System.Windows.Automation.TransformPattern]::Pattern).Resize(1000, 600)
    Start-Sleep -Milliseconds 800
    Keys "7891234567895{ENTER}"
    Keys "{F2}"
    Shot "10-small-window"
}
finally {
    Stop-Process $proc.Id -Force -ErrorAction SilentlyContinue
}
