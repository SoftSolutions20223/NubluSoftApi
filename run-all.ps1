# Script para ejecutar todos los servicios de NubluSoft
# Ejecutar desde la raiz del proyecto: .\run-all.ps1

Write-Host "Compilando solucion..." -ForegroundColor Cyan
dotnet build NubluSoft.sln

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error en la compilacion" -ForegroundColor Red
    exit 1
}

Write-Host "`nIniciando servicios..." -ForegroundColor Green

# Iniciar cada servicio en una nueva ventana de PowerShell
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PSScriptRoot\NubluSoft'; Write-Host 'GATEWAY (5008)' -ForegroundColor Yellow; dotnet run"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PSScriptRoot\NubluSoft_Core'; Write-Host 'CORE (5001)' -ForegroundColor Yellow; dotnet run"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PSScriptRoot\NubluSoft_Storage'; Write-Host 'STORAGE (5002)' -ForegroundColor Yellow; dotnet run"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PSScriptRoot\NubluSoft_NavIndex'; Write-Host 'NAVINDEX (5003)' -ForegroundColor Yellow; dotnet run"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PSScriptRoot\NubluSoft_Signature'; Write-Host 'SIGNATURE (5004)' -ForegroundColor Yellow; dotnet run"

Write-Host "`nServicios iniciandose en ventanas separadas:" -ForegroundColor Green
Write-Host "  - Gateway:   http://localhost:5008/swagger" -ForegroundColor White
Write-Host "  - Core:      http://localhost:5001/swagger" -ForegroundColor White
Write-Host "  - Storage:   http://localhost:5002/swagger" -ForegroundColor White
Write-Host "  - NavIndex:  http://localhost:5003/swagger" -ForegroundColor White
Write-Host "  - Signature: http://localhost:5004/swagger" -ForegroundColor White

# Funcion para verificar si un servicio esta listo
function Wait-ForService {
    param (
        [string]$Name,
        [string]$Url,
        [int]$MaxAttempts = 30
    )

    $attempt = 0
    while ($attempt -lt $MaxAttempts) {
        try {
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 2 -ErrorAction SilentlyContinue
            if ($response.StatusCode -eq 200) {
                Write-Host "  [OK] $Name listo" -ForegroundColor Green
                return $true
            }
        } catch {
            # Servicio aun no responde
        }
        $attempt++
        Start-Sleep -Milliseconds 500
    }
    Write-Host "  [!] $Name no responde (timeout)" -ForegroundColor Yellow
    return $false
}

Write-Host "`nEsperando que los servicios esten listos..." -ForegroundColor Cyan

Wait-ForService -Name "Gateway" -Url "http://localhost:5008/health"
Wait-ForService -Name "Core" -Url "http://localhost:5001/health"
Wait-ForService -Name "Storage" -Url "http://localhost:5002/health"
Wait-ForService -Name "NavIndex" -Url "http://localhost:5003/health"
Wait-ForService -Name "Signature" -Url "http://localhost:5004/health"

Write-Host "`nAbriendo Swagger en el navegador..." -ForegroundColor Cyan
Start-Process "http://localhost:5008/swagger"
Start-Process "http://localhost:5001/swagger"
Start-Process "http://localhost:5002/swagger"
Start-Process "http://localhost:5003/swagger"
Start-Process "http://localhost:5004/swagger"

Write-Host "`nTodos los servicios iniciados y Swagger abiertos!" -ForegroundColor Green
