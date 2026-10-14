param(
    [string]$GameDirectory='C:\Program Files (x86)\Steam\steamapps\common\Risk of Rain 2',
    [string]$BepInExDirectory="$env:APPDATA\r2modmanPlus-local\RiskOfRain2\profiles\Classic Route - Current Game\BepInEx",
    [string]$ReferenceDirectory=(Join-Path $PSScriptRoot '../ClassicRoute/build-tools/references/ref/netstandard2.1')
)
$ErrorActionPreference = 'Stop'
$horizonManaged = Join-Path $GameDirectory 'Risk of Rain 2_Data\Managed'
$horizonCore = Join-Path $BepInExDirectory 'core'
$horizonArgs = @('/nologo','/nostdlib+','/target:library','/optimize+','/nowarn:1684,0618', ('/out:' + (Join-Path $PSScriptRoot 'src/EventHorizon.dll')))
$horizonArgs += Get-ChildItem -LiteralPath $ReferenceDirectory -Filter '*.dll' | ForEach-Object { '/reference:' + $_.FullName }
$horizonArgs += @('LegacyResourcesAPI.dll','RoR2.dll','UnityEngine.UI.dll','KinematicCharacterController.dll','UnityEngine.dll','UnityEngine.CoreModule.dll','UnityEngine.AssetBundleModule.dll','UnityEngine.InputLegacyModule.dll','UnityEngine.IMGUIModule.dll','UnityEngine.TextRenderingModule.dll','UnityEngine.ParticleSystemModule.dll','UnityEngine.PhysicsModule.dll','UnityEngine.AnimationModule.dll','Unity.Addressables.dll','Unity.ResourceManager.dll','Unity.Postprocessing.Runtime.dll','HGUnityUtils.dll','HGCSharpUtils.dll','com.unity.multiplayer-hlapi.Runtime.dll','AK.Wwise.Unity.API.dll','AK.Wwise.Unity.MonoBehaviour.dll') | ForEach-Object { '/reference:' + (Join-Path $horizonManaged $_) }
$horizonArgs += @("/reference:$horizonCore\BepInEx.dll", "/reference:$horizonCore\0Harmony.dll")
$horizonArgs += ('/resource:' + (Join-Path $PSScriptRoot 'shaders/horizonprojection.bundle') + ',SurvivorDomains.HorizonProjection')
$horizonArgs += ('/resource:' + (Join-Path $PSScriptRoot 'shaders/horizonscenerymask.bundle') + ',SurvivorDomains.HorizonSceneryMask')
$horizonArgs += ('/resource:' + (Join-Path $PSScriptRoot 'audio/horizon-hit.wav') + ',SurvivorDomains.HorizonHit')
$horizonArgs += ('/resource:' + (Join-Path $PSScriptRoot 'audio/horizon-loop.wav') + ',SurvivorDomains.HorizonLoop')
$horizonArgs += Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.cs' | ForEach-Object { $_.FullName }
$horizonRsp = Join-Path $PSScriptRoot 'build.rsp'
[IO.File]::WriteAllLines($horizonRsp, [string[]]@($horizonArgs | ForEach-Object { '"' + $_ + '"' }))
& 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' /noconfig ('@' + $horizonRsp)
if ($LASTEXITCODE -ne 0) { throw 'Event Horizon release compilation failed' }
