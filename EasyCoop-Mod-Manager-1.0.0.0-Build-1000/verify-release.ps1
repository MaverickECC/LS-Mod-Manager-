$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $projectRoot "EasyCoop.ModManager.csproj"

Write-Host "Pruefe EasyCoop Mod Manager 1.0.0.0 - Build 1000 ..." -ForegroundColor Cyan
dotnet restore $project
if ($LASTEXITCODE -ne 0) { throw "dotnet restore ist fehlgeschlagen (Exitcode $LASTEXITCODE)." }
dotnet build $project -c Release --no-restore
if ($LASTEXITCODE -ne 0) { throw "dotnet build ist fehlgeschlagen (Exitcode $LASTEXITCODE)." }

function Assert-Contains {
    param([string]$Path, [string]$Text, [string]$Label)
    if (-not (Select-String -Path $Path -SimpleMatch -Pattern $Text -Quiet)) {
        throw "Release-Pruefung fehlgeschlagen: $Label ($Path)"
    }
    Write-Host "  OK: $Label" -ForegroundColor DarkGreen
}

$installer = Join-Path $projectRoot 'installer\EasyCoop-Mod-Manager.iss'
$buildInfo = Join-Path $projectRoot 'Services\BuildInfo.cs'
Assert-Contains $project '<Version>1.0.0.0</Version>' 'Projektversion 1.0.0.0'
Assert-Contains $project '<FileVersion>1.0.0.0</FileVersion>' 'Windows-Dateiversion 1.0.0.0'
Assert-Contains $project '<InformationalVersion>1.0.0.0+build.1000</InformationalVersion>' 'Informationsversion Build 1000'
Assert-Contains $installer 'MyAppVersion "1.0.0.0"' 'Installerversion 1.0.0.0'
Assert-Contains (Join-Path $projectRoot 'MainWindow.xaml') 'Offizielle Version 1.0.0.0' 'sichtbare Programmversion'
Assert-Contains (Join-Path $projectRoot 'MainWindow.xaml') 'Build 1000' 'sichtbare Buildnummer'
Assert-Contains $installer 'InfoBeforeFile=Vorinstallation.txt' 'Vorinstallationstext'
Assert-Contains $installer 'WizardImageFile=..\Assets\EasyCoop-Installer-Background.png' 'Installer-Hintergrund'
Assert-Contains $project '<Resource Include="Assets\EasyCoop-Background-Main.png" />' 'WPF-Ressource Hauptfenster'
Assert-Contains $project '<Resource Include="Assets\EasyCoop-Background-About.png" />' 'WPF-Ressource Ueber-Fenster'
Assert-Contains $project '<Resource Include="Assets\EasyCoop-Background-Options.png" />' 'WPF-Ressource Optionen'
Assert-Contains $project '<Resource Include="Assets\EasyCoop-Background-GameLaunch.png" />' 'WPF-Ressource Spielstart'
Assert-Contains $project '<Resource Include="Assets\EasyCoop-Background-Servers.png" />' 'WPF-Ressource Serververwaltung'
Assert-Contains $project '<Resource Include="Assets\EasyCoop-Background-ModFolder.png" />' 'WPF-Ressource Modordner'
Assert-Contains $project '<Resource Include="Assets\EasyCoop-Background-Profile.png" />' 'WPF-Ressource Modprofil'
Assert-Contains $project '<Resource Include="Assets\LS19-Official-Logo.png" />' 'WPF-Ressource LS19-Logo'
Assert-Contains $project '<Resource Include="Assets\LS22-Official-Logo.png" />' 'WPF-Ressource LS22-Logo'
Assert-Contains $project '<Resource Include="Assets\LS25-Official-Logo.png" />' 'WPF-Ressource LS25-Logo'
Assert-Contains (Join-Path $projectRoot 'MainWindow.xaml') 'LogoSource' 'Logos in Spielversionsauswahl'
Assert-Contains $project '<Resource Include="Assets\EasyCoop-Background-Savegames.png" />' 'WPF-Ressource Spielstaende-Hintergrund'
Assert-Contains (Join-Path $projectRoot 'MainWindow.xaml') 'OpenSavegames_Click' 'Spielstaende-Navigation'
Assert-Contains (Join-Path $projectRoot 'SavegamesWindow.xaml') 'EasyCoop-Background-Savegames.png' 'Spielstaende-Fenster'
Assert-Contains (Join-Path $projectRoot 'Services\SavegameService.cs') 'CreateBackup(gameShortName, savegame, "vor-wiederherstellung")' 'Sicherheitsbackup vor Wiederherstellung'
Assert-Contains (Join-Path $projectRoot 'Services\SavegameService.cs') 'EnsureGameIsNotRunning' 'Spielprozess-Sperre fuer Spielstaende'
Assert-Contains (Join-Path $projectRoot 'SavegamesWindow.xaml') 'SaveValues_Click' 'Grundwerte-Editor'
Assert-Contains (Join-Path $projectRoot 'Services\SavegameService.cs') 'UpdateBasicValues' 'Grundwerte speichern'
Assert-Contains (Join-Path $projectRoot 'Services\SavegameService.cs') 'FindCareerMoney' 'Geldwert in careerSavegame'
Assert-Contains (Join-Path $projectRoot 'Services\SavegameService.cs') 'moneyAttribute.Value = moneyText' 'Geldwert in farms.xml'
Assert-Contains (Join-Path $projectRoot 'Services\SavegameService.cs') 'vor-bearbeitung' 'Sicherheitsbackup vor Bearbeitung'
Assert-Contains $project '<Resource Include="Assets\EasyCoop-Background-AdvancedSavegame.png" />' 'WPF-Ressource erweiterter Spielstand-Editor'
Assert-Contains (Join-Path $projectRoot 'SavegamesWindow.xaml') 'AdvancedEditor_Click' 'Navigation erweiterter Editor'
Assert-Contains (Join-Path $projectRoot 'AdvancedSavegameWindow.xaml') 'Fahrzeuge' 'Editor fuer Fahrzeuge'
Assert-Contains (Join-Path $projectRoot 'AdvancedSavegameWindow.xaml') 'Tiere' 'Editor fuer Tiere'
Assert-Contains (Join-Path $projectRoot 'AdvancedSavegameWindow.xaml') 'Felder' 'Editor fuer Felder'
Assert-Contains (Join-Path $projectRoot 'AdvancedSavegameWindow.xaml') 'Produktionen' 'Editor fuer Produktionen'
Assert-Contains (Join-Path $projectRoot 'Services\AdvancedSavegameService.cs') 'vor-erweitert' 'Backup vor erweiterten Aenderungen'
Assert-Contains (Join-Path $projectRoot 'build-installer.ps1') 'dotnet publish ist fehlgeschlagen' 'Abbruch bei Publishfehler'
Assert-Contains (Join-Path $projectRoot 'build-installer.ps1') 'EasyCoop Mod Manager.exe' 'Pruefung der veroeffentlichten EXE'
Assert-Contains (Join-Path $projectRoot 'MainWindow.xaml') 'EasyCoop-Background-Main.png' 'Hintergrund Hauptfenster'
Assert-Contains (Join-Path $projectRoot 'AboutWindow.xaml') 'EasyCoop-Background-About.png' 'Hintergrund Ueber-Fenster'
Assert-Contains (Join-Path $projectRoot 'OptionsWindow.xaml') 'EasyCoop-Background-Options.png' 'Hintergrund Optionen'
Assert-Contains (Join-Path $projectRoot 'GameLaunchDialog.xaml') 'EasyCoop-Background-GameLaunch.png' 'Hintergrund Spielstart'
Assert-Contains (Join-Path $projectRoot 'DedicatedServersWindow.xaml') 'EasyCoop-Background-Servers.png' 'Hintergrund Serververwaltung'
Assert-Contains (Join-Path $projectRoot 'ModFolderNameDialog.xaml') 'EasyCoop-Background-ModFolder.png' 'Hintergrund Modordner'
Assert-Contains (Join-Path $projectRoot 'ProfileNameDialog.xaml') 'EasyCoop-Background-Profile.png' 'Hintergrund Modprofil'
Assert-Contains (Join-Path $projectRoot 'OptionsWindow.xaml.cs') 'SettingsSaved?.Invoke' 'Optionsuebernahme'
Assert-Contains (Join-Path $projectRoot 'MainWindow.xaml.cs') 'ApplySettings(current.Preferences)' 'sofortige Optionsanwendung'
Assert-Contains $buildInfo 'Version = "1.0.0.0"' 'zentrale Programmversion'
Assert-Contains $buildInfo 'BuildNumber = "1000"' 'zentrale Buildnummer'
Assert-Contains $buildInfo 'discord.gg/JVjRhghfME' 'Discord-Support'
Assert-Contains $buildInfo 'Easycoopclantv@gmail.com' 'E-Mail-Support'
Assert-Contains $buildInfo 'https://www.easycoopclan.de' 'Website'
Assert-Contains (Join-Path $projectRoot 'AboutWindow.xaml') 'x:Name="DatabaseVersionText"' 'Anzeige der Mod-Datenbankversion'

Write-Host "Build und Versionspruefung erfolgreich." -ForegroundColor Green
