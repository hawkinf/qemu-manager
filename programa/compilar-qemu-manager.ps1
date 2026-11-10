# Compilar QEMU Manager (C#)
# Requisitos: .NET SDK 6+ ou 8+

$ErrorActionPreference = "Stop"
$proj = "QemuManager.csproj"
$src  = "QemuManager.cs"

if (-not (Test-Path $src)) {
    Write-Host "Arquivo $src não encontrado." -ForegroundColor Red
    exit 1
}

# Cria o projeto se não existir
if (-not (Test-Path $proj)) {
@"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <ApplicationIcon>Resources\app.ico</ApplicationIcon>
    <AssemblyTitle>QEMU Manager</AssemblyTitle>
    <Product>QEMU Manager</Product>
    <Description>Gerenciador gráfico de máquinas virtuais QEMU</Description>
  </PropertyGroup>
  <ItemGroup>
    <EmbeddedResource Include="Resources\*.png" Exclude="Resources\_preview.png" />
    <EmbeddedResource Include="Resources\app.ico" />
  </ItemGroup>
</Project>
"@ | Set-Content $proj -Encoding UTF8
}

Write-Host "Compilando..." -ForegroundColor Cyan
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nPronto! Executável em: publish\QemuManager.exe" -ForegroundColor Green
} else {
    Write-Host "Erro na compilação." -ForegroundColor Red
}
