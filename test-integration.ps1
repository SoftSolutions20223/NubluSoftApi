# test-integration.ps1
# Script para probar la comunicación entre microservicios NubluSoft

$ErrorActionPreference = "Continue"

Clear-Host

Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "  PRUEBAS DE INTEGRACION NUBLUSOFT" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

# URLs de los servicios
$gateway = "http://localhost:5008"
$core = "http://localhost:5001"
$storage = "http://localhost:5002"
$navindex = "http://localhost:5003"

# Contadores
$passed = 0
$failed = 0

# ========== 1. Health Checks ==========
Write-Host "1. Verificando Health de cada servicio..." -ForegroundColor Yellow
Write-Host ""

$services = @(
    @{ Name = "Gateway"; Url = "$gateway/health" },
    @{ Name = "Core"; Url = "$core/health" },
    @{ Name = "Storage"; Url = "$storage/health" },
    @{ Name = "NavIndex"; Url = "$navindex/health" }
)

foreach ($service in $services) {
    try {
        $response = Invoke-RestMethod -Uri $service.Url -Method GET -TimeoutSec 5 -ErrorAction Stop
        Write-Host "   [OK] $($service.Name): $($service.Url)" -ForegroundColor Green
        $passed++
    }
    catch {
        Write-Host "   [FAIL] $($service.Name): $($service.Url)" -ForegroundColor Red
        $failed++
    }
}

Write-Host ""

# ========== 2. Pedir Token ==========
Write-Host "2. Token JWT..." -ForegroundColor Yellow
Write-Host ""
Write-Host "   Obtener token de: http://localhost:5008/swagger" -ForegroundColor Cyan
Write-Host "   POST /api/Auth/login con: {""usuario"":""admin"",""contraseña"":""123456""}" -ForegroundColor DarkGray
Write-Host ""

$token = Read-Host "   Pega el token aqui (o ENTER para saltar)"

if ([string]::IsNullOrWhiteSpace($token)) {
    Write-Host "   [SKIP] No se proporciono token" -ForegroundColor Yellow
}
else {
    Write-Host "   [OK] Token recibido" -ForegroundColor Green
    $passed++
    
    $headers = @{
        "Authorization" = "Bearer $token"
    }

    Write-Host ""

    # ========== 3. Proxy Gateway -> Core (Carpetas) ==========
    Write-Host "3. Probando Proxy Gateway -> Core (carpetas)..." -ForegroundColor Yellow
    Write-Host ""

    try {
        $carpetas = Invoke-RestMethod -Uri "$gateway/api/carpetas" `
            -Method GET -Headers $headers -TimeoutSec 10 -ErrorAction Stop
        
        Write-Host "   [OK] Proxy a Core funcionando" -ForegroundColor Green
        $count = if ($carpetas -is [array]) { $carpetas.Count } else { 1 }
        Write-Host "   Carpetas encontradas: $count" -ForegroundColor DarkGray
        $passed++
    }
    catch {
        Write-Host "   [FAIL] Error: $($_.Exception.Message)" -ForegroundColor Red
        $failed++
    }

    Write-Host ""

    # ========== 4. Proxy Gateway -> NavIndex ==========
    Write-Host "4. Probando Proxy Gateway -> NavIndex (estructura)..." -ForegroundColor Yellow
    Write-Host ""

    try {
        $version = Invoke-RestMethod -Uri "$gateway/api/navegacion/estructura/version" `
            -Method GET -Headers $headers -TimeoutSec 10 -ErrorAction Stop
        
        Write-Host "   [OK] Proxy a NavIndex funcionando" -ForegroundColor Green
        Write-Host "   Response: $($version | ConvertTo-Json -Compress)" -ForegroundColor DarkGray
        $passed++
    }
    catch {
        Write-Host "   [FAIL] Error: $($_.Exception.Message)" -ForegroundColor Red
        $failed++
    }

    Write-Host ""

    # ========== 5. Storage Internal ==========
    Write-Host "5. Probando Storage Internal endpoint..." -ForegroundColor Yellow
    Write-Host ""

    try {
        $exists = Invoke-RestMethod -Uri "$storage/internal/Internal/exists?objectName=test.txt" `
            -Method GET -Headers $headers -TimeoutSec 10 -ErrorAction Stop
        
        Write-Host "   [OK] Storage Internal funcionando" -ForegroundColor Green
        Write-Host "   Response: $($exists | ConvertTo-Json -Compress)" -ForegroundColor DarkGray
        $passed++
    }
    catch {
        Write-Host "   [WARN] $($_.Exception.Message)" -ForegroundColor Yellow
    }

    Write-Host ""

    # ========== 6. Usuarios ==========
    Write-Host "6. Probando endpoint de usuarios..." -ForegroundColor Yellow
    Write-Host ""

    try {
        $usuarios = Invoke-RestMethod -Uri "$gateway/api/usuarios" `
            -Method GET -Headers $headers -TimeoutSec 10 -ErrorAction Stop
        
        Write-Host "   [OK] Endpoint usuarios funcionando" -ForegroundColor Green
        $count = if ($usuarios -is [array]) { $usuarios.Count } else { 1 }
        Write-Host "   Usuarios encontrados: $count" -ForegroundColor DarkGray
        $passed++
    }
    catch {
        Write-Host "   [FAIL] Error: $($_.Exception.Message)" -ForegroundColor Red
        $failed++
    }

    Write-Host ""

    # ========== 7. Archivos ==========
    Write-Host "7. Probando endpoint de archivos..." -ForegroundColor Yellow
    Write-Host ""

    try {
        $archivos = Invoke-RestMethod -Uri "$gateway/api/archivos" `
            -Method GET -Headers $headers -TimeoutSec 10 -ErrorAction Stop
        
        Write-Host "   [OK] Endpoint archivos funcionando" -ForegroundColor Green
        $count = if ($archivos -is [array]) { $archivos.Count } else { 0 }
        Write-Host "   Archivos encontrados: $count" -ForegroundColor DarkGray
        $passed++
    }
    catch {
        Write-Host "   [FAIL] Error: $($_.Exception.Message)" -ForegroundColor Red
        $failed++
    }

    Write-Host ""

    # ========== 8. Oficinas ==========
    Write-Host "8. Probando endpoint de oficinas..." -ForegroundColor Yellow
    Write-Host ""

    try {
        $oficinas = Invoke-RestMethod -Uri "$gateway/api/oficinas" `
            -Method GET -Headers $headers -TimeoutSec 10 -ErrorAction Stop
        
        Write-Host "   [OK] Endpoint oficinas funcionando" -ForegroundColor Green
        $count = if ($oficinas -is [array]) { $oficinas.Count } else { 1 }
        Write-Host "   Oficinas encontradas: $count" -ForegroundColor DarkGray
        $passed++
    }
    catch {
        Write-Host "   [FAIL] Error: $($_.Exception.Message)" -ForegroundColor Red
        $failed++
    }
}

Write-Host ""

# ========== Resumen ==========
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "  RESUMEN" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "   Pruebas exitosas: $passed" -ForegroundColor Green
Write-Host "   Pruebas fallidas: $failed" -ForegroundColor $(if ($failed -gt 0) { "Red" } else { "Green" })
Write-Host ""
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Presiona cualquier tecla para cerrar..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")