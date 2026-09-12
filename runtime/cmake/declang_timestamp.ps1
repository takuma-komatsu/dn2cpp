param([string]$Source, [string]$Result)
$ErrorActionPreference = 'Stop'
(Get-Item -LiteralPath $Result).LastWriteTimeUtc = (Get-Item -LiteralPath $Source).LastWriteTimeUtc
