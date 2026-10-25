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
    $e = $script:win.FindFirst($Tree::Descendants, (New-Object $C ($A::NameProperty, $name)))
    if (-not $e) { throw "element '$name' not found" }
    return $e
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
    Shot "1-products-empty"

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
}
finally {
    Stop-Process $proc.Id -Force -ErrorAction SilentlyContinue
}
