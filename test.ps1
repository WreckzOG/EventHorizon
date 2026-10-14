$ErrorActionPreference='Stop'
$ehCompiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
Push-Location $PSScriptRoot
try {
    foreach($ehSet in @(
        @{Name='HorizonHarness';Files=@('HorizonCombat','HorizonBurnTimer','HorizonProjectiles','HorizonRules','DomainRules')},
        @{Name='HorizonSurfaceHarness';Files=@('HorizonSurfaces','HorizonClutterNames')},
        @{Name='HorizonMeshHarness';Files=@('HorizonBlackHole')},
        @{Name='HorizonMusicHarness';Files=@('HorizonAudio','HorizonMusicMix')}
    )) {
        $ehSources=(@('tests/'+$ehSet.Name+'.cs')+@($ehSet.Files|ForEach-Object{'src/'+$_+'.cs'}))|ForEach-Object{Join-Path $PSScriptRoot $_}
        & $ehCompiler /nologo /nowarn:0649,0108 ('/out:tests/'+$ehSet.Name+'.exe') $ehSources
        if($LASTEXITCODE -ne 0){throw 'Harness compilation failed'}
        & ('./tests/'+$ehSet.Name+'.exe')
        if($LASTEXITCODE -ne 0){throw 'Behavior check failed'}
    }
    Add-Type -Path "$env:APPDATA\r2modmanPlus-local\RiskOfRain2\profiles\Classic Route - Current Game\BepInEx\core\Mono.Cecil.dll"
    $ehModule=[Mono.Cecil.ModuleDefinition]::ReadModule((Join-Path $PSScriptRoot 'src/EventHorizon.dll'))
    try {
        $ehTypes=@($ehModule.Types)
        $ehDomain=$ehTypes|Where-Object Name -eq 'VoidFiendDomain'
        $ehActivate=$ehDomain.Methods|Where-Object Name -eq 'ActivateServer'
        if($ehActivate.Parameters.Count -ne 0){throw 'Activation bypass parameter remains'}
        if(($ehTypes|Where-Object Name -eq 'DomainMessage').Fields|Where-Object Name -eq 'Test'){throw 'Network test flag remains'}
        if($ehTypes|Where-Object Name -match '^(Acrid|Zero|Facility|Huntress|Jackpot|Garden|Orbital|Severance|Automation|Mist|Storm|Supremacy|Overload)'){throw 'Other domains present'}
        $ehText=(Get-ChildItem src/*.cs|Get-Content -Raw)-join "`n"
        if($ehText -match 'TestCastKey|TestKey|TestingEnabled|KeyCode.F6|bool test|TEST CAST|GUI.Button'){throw 'Free cast route remains'}
        foreach($ehGuard in 'get_active','get_Alive','Account','get_unscaledTime'){
            if(-not($ehActivate.Body.Instructions|Where-Object{$_.Operand -is [Mono.Cecil.MethodReference] -and $_.Operand.Name -eq $ehGuard})){throw "Missing activation guard $ehGuard"}
        }
        foreach($ehField in 'cast','Fraction'){
            if(-not($ehActivate.Body.Instructions|Where-Object{$_.OpCode.Name -eq 'ldfld' -and $_.Operand.Name -eq $ehField})){throw "Missing charge/active guard $ehField"}
        }
        if($ehText -notmatch 'if\(cast!=null\|\|account.Fraction<1\)return;' -or $ehText -notmatch 'account.Fraction=0;'){throw 'Full charge consumption missing'}
        if($ehText -notmatch 'user.connectionToClient != message.conn' -or $ehText -notmatch 'body.netId != request.Body'){throw 'Client ownership missing'}
        if($ehText -notmatch 'Duration=F\(c,"Duration",22,' -or $ehText -notmatch 'Windup=F\(c,"WindupDuration",0,'){throw 'Timing defaults incorrect'}
        foreach($ehName in 'HorizonProjection','HorizonSceneryMask','HorizonHit','HorizonLoop'){
            if(-not($ehModule.Resources|Where-Object Name -eq ('SurvivorDomains.'+$ehName))){throw "Missing resource $ehName"}
        }
        if($ehModule.Resources.Count -ne 4){throw 'Unexpected embedded resources'}
        if($ehModule.AssemblyReferences|Where-Object Name -eq 'SurvivorDomains'){throw 'Depends on private pack'}
        $ehMessage=$ehTypes|Where-Object Name -eq 'DomainMessage'
        $ehWritten=@(($ehMessage.Methods|Where-Object Name -eq Serialize).Body.Instructions|Where-Object{$_.OpCode.Name -eq 'ldfld'}|ForEach-Object{$_.Operand.Name})
        $ehRead=@(($ehMessage.Methods|Where-Object Name -eq Deserialize).Body.Instructions|Where-Object{$_.OpCode.Name -eq 'stfld'}|ForEach-Object{$_.Operand.Name})
        if(($ehWritten -join ',') -ne ($ehRead -join ',')){throw 'Asymmetric cast message'}
        Write-Output 'PASS: Void Fiend-only binary; 22s/instant defaults; no input, HUD, config or network free-cast route; charge and ownership guards; symmetric wire format; all four shader/audio resources.'
    } finally {$ehModule.Dispose()}
} finally {Pop-Location}
